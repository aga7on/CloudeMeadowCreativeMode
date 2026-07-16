using System;
using System.Collections.Generic;
using System.Reflection;
using TeamNimbus.CloudMeadow.Managers;

namespace CloudMeadow.CreativeMode
{
    internal static class TransactionManager
    {
        private sealed class Slot { public object Target; public FieldInfo Field; public object Before; public object After; }
        private sealed class CollectionSlot { public System.Collections.IList Target; public object[] Before; public object[] After; }
        private sealed class Transaction { public string Label; public DateTime Time; public readonly List<Slot> Slots = new List<Slot>(); public readonly List<CollectionSlot> Collections = new List<CollectionSlot>(); }
        private static Dictionary<string, Slot> _pending;
        private static List<CollectionSlot> _pendingCollections;
        private static readonly List<Transaction> _history = new List<Transaction>();
        private static string[] _lastValidation = new string[0];
        public static bool IsRestoring { get; private set; }

        public static void Begin(string label)
        {
            if (IsRestoring || !GameApi.Ready) { _pending = null; return; }
            var map = new Dictionary<string, Slot>(); int objectIndex = 0;
            foreach (object root in Roots())
            {
                if (root == null) continue; string prefix = (++objectIndex) + ":" + root.GetType().FullName + ":" + System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(root);
                Type type = root.GetType();
                while (type != null)
                {
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var f = fields[i]; if (f.IsInitOnly || !Simple(f.FieldType)) continue;
                        try { map[prefix + ":" + type.FullName + ":" + f.Name] = new Slot { Target = root, Field = f, Before = f.GetValue(root) }; } catch { }
                    }
                    type = type.BaseType;
                }
            }
            _pending = map;
            _pendingCollections = CaptureCollections();
        }

        public static void Commit(string label)
        {
            var pending = _pending; var pendingCollections = _pendingCollections; _pending = null; _pendingCollections = null; if (pending == null) return;
            var tx = new Transaction { Label = label, Time = DateTime.Now };
            foreach (var pair in pending)
            {
                var slot = pair.Value; try { slot.After = slot.Field.GetValue(slot.Target); if (!object.Equals(slot.Before, slot.After)) tx.Slots.Add(slot); } catch { }
            }
            if (pendingCollections != null) for (int i = 0; i < pendingCollections.Count; i++)
            {
                var c = pendingCollections[i]; c.After = Snapshot(c.Target); if (!SameSequence(c.Before, c.After)) tx.Collections.Add(c);
            }
            if (tx.Slots.Count == 0 && tx.Collections.Count == 0) return;
            _history.Add(tx); while (_history.Count > 20) _history.RemoveAt(0);
            var validation = new List<string>(); validation.Add("Requested: " + label); validation.Add("Runtime validation: " + tx.Slots.Count + " scalar fields, " + tx.Collections.Count + " collections changed");
            for (int i = 0; i < tx.Slots.Count && i < 8; i++) validation.Add(tx.Slots[i].Target.GetType().Name + "." + tx.Slots[i].Field.Name + ": " + (tx.Slots[i].Before ?? "null") + " -> " + (tx.Slots[i].After ?? "null"));
            _lastValidation = validation.ToArray();
            LogBuffer.Add("Transaction: " + label + " | fields=" + tx.Slots.Count + " collections=" + tx.Collections.Count);
        }

        public static string UndoLast()
        {
            if (_history.Count == 0) return "Nothing to undo";
            var tx = _history[_history.Count - 1]; _history.RemoveAt(_history.Count - 1); int restored = 0; IsRestoring = true;
            try
            {
                for (int i = tx.Collections.Count - 1; i >= 0; i--) { try { Restore(tx.Collections[i]); restored++; } catch { } }
                for (int i = tx.Slots.Count - 1; i >= 0; i--) { try { tx.Slots[i].Field.SetValue(tx.Slots[i].Target, tx.Slots[i].Before); restored++; } catch { } }
            }
            finally { IsRestoring = false; }
            int questsSynced = 0; try { if (tx.Collections.Count > 0) questsSynced = GameApiQuest.ResyncQuestRuntimeFromSaveLog(); } catch { }
            string result = "Undo: " + tx.Label + " | restored=" + restored + (questsSynced > 0 ? " | quests synced=" + questsSynced : string.Empty); LogBuffer.Add(result); return result;
        }

        public static string[] History()
        {
            var result = new string[_history.Count]; for (int i = 0; i < _history.Count; i++) { var tx = _history[_history.Count - 1 - i]; result[i] = tx.Time.ToString("HH:mm:ss") + " | " + tx.Label + " | " + tx.Slots.Count + " fields / " + tx.Collections.Count + " lists"; } return result;
        }
        public static string[] LastValidation() { return (string[])_lastValidation.Clone(); }


        private static List<CollectionSlot> CaptureCollections()
        {
            var result = new List<CollectionSlot>();
            try
            {
                var log = GameManager.Status.QuestsDataLog as System.Collections.IList;
                AddCollection(result, log);
                if (log != null) foreach (object value in log)
                {
                    if (value == null) continue;
                    object steps = null; var type = value.GetType();
                    var prop = type.GetProperty("QuestStepDataStorage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null) steps = prop.GetValue(value, null);
                    else { var field = type.GetField("QuestStepDataStorage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); if (field != null) steps = field.GetValue(value); }
                    AddCollection(result, steps as System.Collections.IList);
                }
            }
            catch { }
            return result;
        }
        private static void AddCollection(List<CollectionSlot> list, System.Collections.IList target)
        { if (target != null && !target.IsReadOnly && !target.IsFixedSize) list.Add(new CollectionSlot { Target = target, Before = Snapshot(target) }); }
        private static object[] Snapshot(System.Collections.IList list)
        { var result = new object[list != null ? list.Count : 0]; if (list != null) list.CopyTo(result, 0); return result; }
        private static bool SameSequence(object[] a, object[] b)
        { if (a == null || b == null || a.Length != b.Length) return false; for (int i = 0; i < a.Length; i++) if (!object.ReferenceEquals(a[i], b[i])) return false; return true; }
        private static void Restore(CollectionSlot slot)
        { slot.Target.Clear(); for (int i = 0; i < slot.Before.Length; i++) slot.Target.Add(slot.Before[i]); }

        private static IEnumerable<object> Roots()
        {
            object status = null; try { status = GameManager.Status; } catch { }
            if (status == null) yield break; yield return status;
            object farm = null, migration = null, savannah = null, forest = null;
            try { farm = GameManager.Status.FarmStatus; migration = GameManager.Status.MigrationSaveData; savannah = GameManager.Status.SavannahPersistentDungeonData; forest = GameManager.Status.ForestPersistentDungeonData; } catch { }
            if (farm != null) yield return farm; if (migration != null) yield return migration; if (savannah != null) yield return savannah; if (forest != null) yield return forest;
            object protagonist = null; try { protagonist = GameManager.Status.ProtagonistStats; } catch { } if (protagonist != null) yield return protagonist;
            TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats[] monsters = null; try { monsters = GameApi.GetActiveMonsters(); } catch { }
            if (monsters != null) for (int i = 0; i < monsters.Length; i++) if (monsters[i] != null) yield return monsters[i];
            // Capture mutable leaf records too: quantities/quality, quest status and
            // step progress otherwise would not participate in Undo.
            object[] entries = null; try { entries = GameApi.GetInventoryEntries(); } catch { }
            if (entries != null) for (int i = 0; i < entries.Length; i++) if (entries[i] != null) yield return entries[i];
            try
            {
                var questLog = GameManager.Status.QuestsDataLog;
                if (questLog != null) for (int i = 0; i < questLog.Count; i++)
                {
                    var q = questLog[i]; if (q == null) continue; yield return q;
                    if (q.QuestStepDataStorage != null) for (int j = 0; j < q.QuestStepDataStorage.Count; j++) if (q.QuestStepDataStorage[j] != null) yield return q.QuestStepDataStorage[j];
                }
            }
            finally { }
        }
        private static bool Simple(Type t) { t = Nullable.GetUnderlyingType(t) ?? t; return t.IsEnum || t.IsPrimitive || t == typeof(string) || t == typeof(decimal); }
    }
}

