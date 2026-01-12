using System;
using TeamNimbus.CloudMeadow; // Season, Weather enums
using TeamNimbus.CloudMeadow.Managers;
using TeamNimbus.CloudMeadow.Persistence;
using TeamNimbus.CloudMeadow.Monsters;
using TeamNimbus.CloudMeadow.UI;
using TeamNimbus.CloudMeadow.Items;
using TeamNimbus.CloudMeadow.Inventory;
using TeamNimbus.CloudMeadow.Utilities;
using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal static class GameApi
    {
        public static bool Ready { get { return Application.isPlaying && GameManager.Instance != null && GameManager.IsGameStatusLoaded; } }

        public static void UnlockAllGallery()
        {
            try {
                SaveGameManager.UnlockEverything();
                // Try refresh album window if open
                try {
                    var managers = UnityEngine.Object.FindObjectsOfType<TeamNimbus.CloudMeadow.UI.AlbumWindowManager>();
                    if (managers != null && managers.Length > 0)
                    {
                        for (int i = 0; i < managers.Length; i++) managers[i].UpdateLockedStatus();
                    }
                } catch { }
                LogBuffer.Add("Unlock Gallery: all content");
                Plugin.Log.LogInfo("Gallery unlock invoked.");
                Banner("Album: All scenes unlocked");
            } catch (Exception e) { Plugin.Log.LogWarning("UnlockEverything failed: " + e.Message); }
        }

        public static void AddKorona(int amount)
        {
            try { GameManager.Status.UpdateKorona(amount, TransactionSource.Cheating); LogBuffer.Add("Korona +" + amount); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void AddShards(int amount)
        {
            try { GameManager.Status.UpdateUpgradeShards(amount); LogBuffer.Add("Shards +" + amount); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void AdvanceToEndOfDay()
        {
            try { GameManager.Status.GetCalendarDate.CHEAT_AdvanceTimeToEndOfDay(); LogBuffer.Add("Advance to end of day"); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void ToggleGodMode()
        {
            try { var s = GameManager.Status; s.GodMode = !s.GodMode; var on = s.GodMode; LogBuffer.Add("GodMode: " + (on ? "ON" : "OFF")); Banner("GodMode: " + (on ? "ON" : "OFF")); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void SetSeason(Season season)
        {
            try { GameManager.Status.GetCalendarDate.CHEAT_AdvanceTimeToSeason(season); LogBuffer.Add("Season -> " + season); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void SetWeather(Weather weather)
        {
            try { GameManager.Status.CHEAT_ForceWeatherChange(weather); LogBuffer.Add("Weather -> " + weather); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void ClearBarn()
        {
            try { GameManager.Status.CHEAT_ClearAllActiveMonsters(); LogBuffer.Add("Cleared barn"); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void GiveEveryMonster()
        {
            try { TeamNimbus.CloudMeadow.Combat.DebugCheats.AddAllMonsters(Mathf.Max(GameManager.Status.ProtagonistStats.Level, 15)); LogBuffer.Add("Give all monsters"); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void RecruitAllCompanions(int level)
        {
            try { TeamNimbus.CloudMeadow.Combat.DebugCheats.RecruitCompanions(level); LogBuffer.Add("Recruit companions to ~L" + level); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void LevelCompanions(int level)
        {
            try { TeamNimbus.CloudMeadow.Combat.DebugCheats.LevelCompanions(level); LogBuffer.Add("Level companions -> " + level); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void LevelMonsters(int level)
        {
            try { TeamNimbus.CloudMeadow.Combat.DebugCheats.LevelMonsters(level); LogBuffer.Add("Level monsters -> " + level); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void LevelAll(int level)
        {
            try { TeamNimbus.CloudMeadow.Combat.DebugCheats.LevelAll(level); LogBuffer.Add("Level ALL -> " + level); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void UpgradeAllAbilitiesForParty()
        {
            try
            {
                // Try direct cheat/API
                var dbg = typeof(TeamNimbus.CloudMeadow.Combat.DebugCheats);
                var m = dbg.GetMethod("UpgradeAllAbilitiesForParty") ?? dbg.GetMethod("UpgradePartyAbilities") ?? dbg.GetMethod("MaxAbilitiesForParty");
                if (m != null) { try { m.Invoke(null, null); Banner("Upgraded party abilities"); return; } catch { } }

                // Fallback: reflect party and upgrade per member
                var gm = TeamNimbus.CloudMeadow.Managers.GameManager.Instance;
                object party = null;
                var gmType = gm.GetType();
                var pProp = gmType.GetProperty("PartyManager") ?? gmType.GetProperty("Party");
                if (pProp != null) party = pProp.GetValue(gm, null);
                if (party != null)
                {
                    var membersProp = party.GetType().GetProperty("Members");
                    var list = membersProp != null ? membersProp.GetValue(party, null) as System.Collections.IEnumerable : null;
                    if (list != null)
                    {
                        foreach (var member in list)
                        {
                            if (member == null) continue;
                            var abProp = member.GetType().GetProperty("Abilities");
                            var abHolder = abProp != null ? abProp.GetValue(member, null) : null;
                            if (abHolder == null) continue;
                            var upg = abHolder.GetType().GetMethod("UpgradeAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                   ?? abHolder.GetType().GetMethod("MaxAll")
                                   ?? abHolder.GetType().GetMethod("UpgradeEverything");
                            if (upg != null) { try { upg.Invoke(abHolder, null); } catch { } }
                        }
                        Banner("Upgraded party abilities");
                        return;
                    }
                }
                Banner("Upgrade abilities: attempted");
            }
            catch (Exception e) { Plugin.Log.LogWarning("UpgradeAllAbilitiesForParty failed: " + e.Message); }
        }

        // ===== Inventory helpers =====
        public static object[] GetInventoryEntries()
        {
            try
            {
                var inv = GameManager.Status.Inventory;
                var list = new System.Collections.Generic.List<object>();
                var t = inv.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    object val = null; try { val = p.GetValue(inv, null); } catch { }
                    AppendEntryEnumerable(list, val);
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    object val = null; try { val = f.GetValue(inv); } catch { }
                    AppendEntryEnumerable(list, val);
                }
                return list.ToArray();
            }
            catch (Exception e) { Plugin.Log.LogWarning("GetInventoryEntries failed: " + e.Message); }
            return new object[0];
        }

        private static void AppendEntryEnumerable(System.Collections.Generic.List<object> list, object val)
        {
            if (val == null) return;
            var en = val as System.Collections.IEnumerable; if (en == null || val is string) return;
            foreach (var it in en)
            {
                if (it == null) continue;
                var tn = it.GetType().FullName;
                if (tn != null && (tn.IndexOf("Entry") >= 0 || tn.IndexOf("ItemEntry") >= 0)) list.Add(it);
            }
        }

        private static object CreateEntry(object def, int amount, int qualityIndex)
        {
            try
            {
                var tDef = def.GetType();
                qualityIndex = ClampQualityIndex(qualityIndex);
                var itemQualityType = typeof(TeamNimbus.CloudMeadow.Items.ItemQuality);
                object quality = System.Enum.ToObject(itemQualityType, qualityIndex);

                // Prefer CreateItemEntry(count, quality)
                var mi = tDef.GetMethod("CreateItemEntry", new System.Type[] { typeof(int), itemQualityType });
                if (mi != null)
                {
                    return mi.Invoke(def, new object[] { amount, quality });
                }
                // Special-case: EggItemDefinition -> EggItemEntry(def, true)
                var eggType = typeof(TeamNimbus.CloudMeadow.Items.EggItemDefinition);
                if (eggType.IsAssignableFrom(tDef))
                {
                    var eggEntryType = typeof(TeamNimbus.CloudMeadow.Inventory.EggItemEntry);
                    return System.Activator.CreateInstance(eggEntryType, new object[] { def, true });
                }
                // Fallback: StandardItemEntry(def, quality, amount)
                var sei = typeof(TeamNimbus.CloudMeadow.Inventory.StandardItemEntry);
                return System.Activator.CreateInstance(sei, new object[] { def, quality, amount });
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("CreateEntry failed: " + e.Message);
                return null;
            }
        }

        private static int ClampQualityIndex(int q)
        {
            try
            {
                var vals = System.Enum.GetValues(typeof(TeamNimbus.CloudMeadow.Items.ItemQuality));
                int min = (int)vals.GetValue(0); // OneStar = 1
                int max = (int)vals.GetValue(vals.Length - 1);
                if (q < min) q = min;
                if (q > max) q = max;
                return q;
            }
            catch { return 1; }
        }

        private static bool IsSafeItemDefinition(System.Type tDef)
        {
            try
            {
                var simple = typeof(TeamNimbus.CloudMeadow.Items.SimpleItemDefinition);
                var equip = typeof(TeamNimbus.CloudMeadow.Items.EquippableItemDefinition);
                var usable = typeof(TeamNimbus.CloudMeadow.Items.UsableDefinition);
                // safe: Simple/Equippable/Usable; skip Eggs, Quest-only, etc.
                return simple.IsAssignableFrom(tDef) || equip.IsAssignableFrom(tDef) || usable.IsAssignableFrom(tDef);
            }
            catch { return false; }
        }

        // Previously filtered to crops/ingredients/food/seeds. Reverting to enumerate ALL categories as requested.
        private static bool IsAllowedCategory(object cat) { return true; }

        public static object[] GetAllItemDefinitions()
        {
            try
            {
                var lib = GameManager.ItemLibrary;
                var values = System.Enum.GetValues(typeof(TeamNimbus.CloudMeadow.Items.ItemCategory));
                var list = new System.Collections.Generic.List<object>();
                var t = lib.GetType();
                var mEnum = t.GetMethod("EnumerateItemsInCategory");
                if (mEnum != null)
                {
                    var en = values.GetEnumerator();
                    try
                    {
                        while (en.MoveNext())
                        {
                            object cat = en.Current;
                            var ienum = mEnum.Invoke(lib, new object[] { cat }) as System.Collections.IEnumerable;
                            if (ienum != null)
                            {
                                foreach (var def in ienum) list.Add(def);
                            }
                        }
                    }
                    finally { var disp = en as System.IDisposable; if (disp != null) disp.Dispose(); }
                }
                return list.ToArray();
            }
            catch (Exception e) { Plugin.Log.LogWarning("GetAllItemDefinitions failed: " + e.Message); }
            return new object[0];
        }

        public static void AddItemByDefinition(object def, int amount, int qualityIndex)
        {
            try
            {
                var entry = CreateEntry(def, amount, qualityIndex);
                var ientry = entry as TeamNimbus.CloudMeadow.Inventory.IItemEntry;
                if (ientry != null) GameManager.Status.Inventory.AddItemEntry(ientry);
            }
            catch (Exception e) { Plugin.Log.LogWarning("AddItemByDefinition failed: " + e.Message); }
        }

        public static void AddAllItems(int amount, int qualityIndex)
        {
            var defs = GetAllItemDefinitions();
            int added = 0, skipped = 0;
            for (int i = 0; i < defs.Length; i++)
            {
                try { AddItemByDefinition(defs[i], amount, qualityIndex); added++; }
                catch { skipped++; }
            }
            LogBuffer.Add("AddAllItems: added=" + added + " skipped=" + skipped);
        }

        public static void AdjustEntryQuantity(object entry, int delta)
        {
            try
            {
                var t = entry.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
                var prop = t.GetProperty("Quantity", flags) ?? t.GetProperty("Count", flags) ?? t.GetProperty("Stack", flags) ?? t.GetProperty("Amount", flags);
                if (prop != null && prop.CanRead && prop.CanWrite)
                {
                    var v = prop.GetValue(entry, null); int cur = 0; try { cur = System.Convert.ToInt32(v); } catch { }
                    prop.SetValue(entry, cur + delta, null);
                    return;
                }
                var field = t.GetField("Quantity", flags) ?? t.GetField("Count", flags) ?? t.GetField("Stack", flags) ?? t.GetField("Amount", flags);
                if (field != null)
                {
                    var v = field.GetValue(entry); int cur = 0; try { cur = System.Convert.ToInt32(v); } catch { }
                    field.SetValue(entry, cur + delta);
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("AdjustEntryQuantity failed: " + e.Message); }
        }

        public static void SetEntryMaxQuality(object entry)
        {
            try
            {
                // Replace by: add new max-quality entry FIRST, then remove old entry
                var def = GetEntryDefinition(entry);
                int qty = GetEntryQuantity(entry);
                if (qty <= 0) qty = 1;
                var maxQ = (int)TeamNimbus.CloudMeadow.Items.ItemQuality.FiveStar;
                var newEntry = CreateEntry(def, qty, maxQ) as TeamNimbus.CloudMeadow.Inventory.IItemEntry;
                if (newEntry != null)
                {
                    GameManager.Status.Inventory.AddItemEntry(newEntry);
                    TryRemoveEntry(entry);
                }
                else
                {
                    Plugin.Log.LogWarning("SetEntryMaxQuality: could not create new entry");
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetEntryMaxQuality failed: " + e.Message); }
        }

        private static object GetEntryDefinition(object entry)
        {
            try
            {
                var t = entry.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
                var prop = t.GetProperty("Definition", flags) ?? t.GetProperty("ItemDefinition", flags) ?? t.GetProperty("Def", flags);
                if (prop != null) return prop.GetValue(entry, null);
                var field = t.GetField("Definition", flags) ?? t.GetField("ItemDefinition", flags) ?? t.GetField("Def", flags);
                if (field != null) return field.GetValue(entry);
            }
            catch { }
            return null;
        }

        private static int GetEntryQuantity(object entry)
        {
            try
            {
                var t = entry.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
                var prop = t.GetProperty("Quantity", flags) ?? t.GetProperty("Count", flags) ?? t.GetProperty("Stack", flags) ?? t.GetProperty("Amount", flags);
                if (prop != null)
                { var v = prop.GetValue(entry, null); return System.Convert.ToInt32(v); }
                var field = t.GetField("Quantity", flags) ?? t.GetField("Count", flags) ?? t.GetField("Stack", flags) ?? t.GetField("Amount", flags);
                if (field != null)
                { var v = field.GetValue(entry); return System.Convert.ToInt32(v); }
            }
            catch { }
            return 1;
        }

        private static void TryRemoveEntry(object entry)
        {
            try
            {
                var inv = GameManager.Status.Inventory;
                var tInv = inv.GetType();
                // Prefer Remove by entry
                var mByEntry = tInv.GetMethod("RemoveEntry", new System.Type[] { typeof(TeamNimbus.CloudMeadow.Inventory.IItemEntry) })
                               ?? tInv.GetMethod("RemoveItemEntry", new System.Type[] { typeof(TeamNimbus.CloudMeadow.Inventory.IItemEntry) })
                               ?? tInv.GetMethod("Remove", new System.Type[] { typeof(TeamNimbus.CloudMeadow.Inventory.IItemEntry) });
                if (mByEntry != null) { mByEntry.Invoke(inv, new object[] { (TeamNimbus.CloudMeadow.Inventory.IItemEntry)entry }); return; }
                // Fallback: Remove by definition
                var def = GetEntryDefinition(entry);
                if (def != null)
                {
                    var mByDef = tInv.GetMethod("RemoveByDefinition") ?? tInv.GetMethod("RemoveItemByDefinition");
                    if (mByDef != null) { mByDef.Invoke(inv, new object[] { def }); return; }
                }
                // Last resort: set quantity to 0
                AdjustEntryQuantity(entry, -GetEntryQuantity(entry));
            }
            catch { }
        }
        public static void AddHarvestAndGroceries()
        {
            try
            {
                var itemLib = GameManager.ItemLibrary;
                var inventory = GameManager.Status.Inventory;
                int added = 0;
                foreach (var def in itemLib.EnumerateItemsInCategory(ItemCategory.Crop))
                {
                    var entry = new StandardItemEntry(def, ItemQuality.OneStar, 99);
                    inventory.AddItemEntry(entry);
                    added++;
                }
                foreach (var def in itemLib.EnumerateItemsInCategory(ItemCategory.Ingredient))
                {
                    var entry = new StandardItemEntry(def, ItemQuality.OneStar, 99);
                    inventory.AddItemEntry(entry);
                    added++;
                }
                LogBuffer.Add("Added harvest & groceries: " + added + " entries");
            }
            catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        // ==== Farm helpers ====
        public static void HatchAllEggs()
        {
            try
            {
                var eggs = GetIncubatorEggs();
                int cnt = 0;
                if (eggs != null)
                {
                    for (int i = 0; i < eggs.Length; i++)
                    {
                        var e = eggs[i]; if (e == null) continue;
                        try { HatchEgg(e); cnt++; } catch { }
                    }
                }
                Banner("Hatch All: processed " + cnt + " eggs");
            }
            catch (Exception ex) { Plugin.Log.LogWarning("HatchAllEggs failed: " + ex.Message); }
        }

        public static object[] GetIncubatorEggs()
        {
            try
            {
                var s = GameManager.Status;
                var fs = s.FarmStatus;
                var list = new System.Collections.Generic.List<object>();
                if (fs != null)
                {
                    // Try Incubators collection
                    var incProp = fs.GetType().GetProperty("Incubators", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?? fs.GetType().GetProperty("Incubator", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?? fs.GetType().GetProperty("Hatchery", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?? fs.GetType().GetProperty("Nursery", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var incVal = incProp != null ? incProp.GetValue(fs, null) : null;
                    var incEnum = incVal as System.Collections.IEnumerable;
                    if (incEnum == null && incVal != null)
                    {
                        // single incubator object
                        CollectEggsFromIncubator(list, incVal);
                    }
                    if (incEnum != null)
                    {
                        foreach (var inc in incEnum) CollectEggsFromIncubator(list, inc);
                    }
                }
                if (list.Count == 0)
                {
                    // Fallback: scan scene behaviours by name tokens and collect egg-like items
                    var mbs = ReflectionUtil.FindMonoBehaviours("incub", "hatch", "egg", "breed", "nursery");
                    for (int i = 0; i < mbs.Count; i++)
                    {
                        CollectEggsFromUnknown(list, mbs[i]);
                    }
                }
                return list.ToArray();
            }
            catch { }
            return new object[0];
        }

        private static void CollectEggsFromIncubator(System.Collections.Generic.List<object> list, object inc)
        {
            if (inc == null) return;
            try
            {
                var t = inc.GetType();
                string[] eggCollections = { "Eggs", "EggQueue", "IncubatingEggs", "Slots", "Queue", "incubatorShelves", "IncubatorShelves" };
                for (int i = 0; i < eggCollections.Length; i++)
                {
                    var p = t.GetProperty(eggCollections[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    object col = null; try { if (p != null) col = p.GetValue(inc, null); } catch { col = null; }
                    var en = col as System.Collections.IEnumerable; if (en == null) continue;
                    foreach (var slot in en)
                    {
                        // Keep the slot object; UI/helpers will unwrap egg info when needed
                        list.Add(slot);
                    }
                }
            }
            catch { }
        }

        private static void CollectEggsFromUnknown(System.Collections.Generic.List<object> list, object host)
        {
            if (host == null) return;
            try
            {
                var t = host.GetType();
                // search breadth-first for properties/fields containing collections with Egg-like elements
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    object val = null; try { val = props[i].GetValue(host, null); } catch { val = null; }
                    AppendEggEnumerable(list, val);
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    object val = null; try { val = fields[i].GetValue(host); } catch { val = null; }
                    AppendEggEnumerable(list, val);
                }
            }
            catch { }
        }

        private static void AppendEggEnumerable(System.Collections.Generic.List<object> list, object val)
        {
            if (val == null) return;
            var en = val as System.Collections.IEnumerable; if (en == null || val is string) return;
            foreach (var it in en)
            {
                if (it == null) continue;
                var tn = it.GetType().FullName ?? it.GetType().Name;
                // Shelves -> eggsIncubating -> egg/hatchingDate
                var eggsIncubatingProp = it.GetType().GetProperty("eggsIncubating", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (eggsIncubatingProp != null)
                {
                    try
                    {
                        var eggs = eggsIncubatingProp.GetValue(it, null) as System.Collections.IEnumerable;
                        if (eggs != null)
                        {
                            foreach (var rec in eggs)
                            {
                                list.Add(rec); // record holds egg + hatchingDate
                            }
                        }
                    }
                    catch { }
                    continue;
                }
                // Anything that looks like an egg or egg-entry
                if (tn.IndexOf("egg", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    list.Add(it);
                    continue;
                }
                // unwrap common wrappers
                var pEgg = it.GetType().GetProperty("Egg") ?? it.GetType().GetProperty("egg") ?? it.GetType().GetProperty("Entry") ?? it.GetType().GetProperty("Value") ?? it.GetType().GetProperty("Item");
                if (pEgg != null)
                {
                    try
                    {
                        var inner = pEgg.GetValue(it, null);
                        if (inner != null)
                        {
                            var tni = inner.GetType().FullName ?? inner.GetType().Name;
                            if (tni.IndexOf("egg", System.StringComparison.OrdinalIgnoreCase) >= 0) list.Add(inner); else list.Add(inner);
                        }
                    }
                    catch { }
                }
            }
        }

        public static string GetEggDisplayName(object obj)
        {
            var egg = UnwrapEgg(obj);
            var n = SafeProp(egg, "Name") ?? SafeProp(egg, "DisplayName") ?? SafeProp(egg, "Code");
            return n != null ? n.ToString() : egg != null ? egg.GetType().Name : "(egg)";
        }

        public static string GetEggTimerString(object obj)
        {
            try
            {
                // First, try on the host (slot) itself
                int seconds = TryReadTimerSeconds(obj);
                if (seconds <= 0)
                {
                    var egg = UnwrapEgg(obj);
                    seconds = TryReadTimerSeconds(egg);
                }
                if (seconds < 0) seconds = 0;
                int mm = seconds / 60; int ss = seconds % 60;
                return mm.ToString("00") + ":" + ss.ToString("00");
            }
            catch { }
            return "--:--";
        }

        private static int TryReadTimerSeconds(object o)
        {
            try
            {
                if (o == null) return 0;
                // Seconds properties
                string[] secProps = { "SecondsRemaining", "RemainingSeconds", "TimeRemainingSeconds", "SecondsToHatch", "SecondsLeft", "TimeLeftSeconds" };
                for (int i = 0; i < secProps.Length; i++)
                {
                    var p = o.GetType().GetProperty(secProps[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        try { var v = p.GetValue(o, null); return System.Convert.ToInt32(v); } catch { }
                    }
                }
                // Minutes properties
                string[] minProps = { "MinutesRemaining", "RemainingMinutes", "TimeRemainingMinutes", "MinutesLeft" };
                for (int i = 0; i < minProps.Length; i++)
                {
                    var p = o.GetType().GetProperty(minProps[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        try { var v = p.GetValue(o, null); return System.Convert.ToInt32(v) * 60; } catch { }
                    }
                }
                // Absolute hatchingDate { year, season, day, hour, minute }
                var hd = o.GetType().GetProperty("hatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                      ?? o.GetType().GetProperty("HatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (hd != null)
                {
                    try
                    {
                        var dateObj = hd.GetValue(o, null);
                        int y = ReadIntField(dateObj, "year", "Year");
                        int s = ReadIntField(dateObj, "season", "Season");
                        int d = ReadIntField(dateObj, "day", "Day");
                        int h = ReadIntField(dateObj, "hour", "Hour");
                        int m = ReadIntField(dateObj, "minute", "Minute");
                        // Build comparable minute stamps (relative ordering OK)
                        int targetStamp = MakeMinuteStamp(y, s, d, h, m);
                        var cal = GameManager.Status.GetCalendarDate;
                        int curStamp = MakeMinuteStamp(cal.DateTime.Year, (int)cal.DateTime.Season, cal.DateTime.Day, cal.DateTime.Hour, cal.DateTime.Minute);
                        int diff = targetStamp - curStamp;
                        if (diff > 0) return diff * 60; // assume stamp in hours? we used minutes already; keep in seconds
                        if (diff == 0) return 60; // will hatch soon
                    }
                    catch { }
                }
                // Progress with duration
                string[] progProps = { "Progress", "Progress01", "NormalizedProgress" };
                float prog = -1f;
                for (int i = 0; i < progProps.Length; i++)
                {
                    var p = o.GetType().GetProperty(progProps[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        try { var v = p.GetValue(o, null); prog = System.Convert.ToSingle(v); break; } catch { }
                    }
                }
                if (prog >= 0f)
                {
                    int total = 0;
                    string[] totalProps = { "TotalSeconds", "DurationSeconds", "SecondsTotal", "Duration", "TotalTimeSeconds" };
                    for (int i = 0; i < totalProps.Length; i++)
                    {
                        var p = o.GetType().GetProperty(totalProps[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (p != null)
                        {
                            try { var v = p.GetValue(o, null); total = System.Convert.ToInt32(v); break; } catch { }
                        }
                    }
                    if (total > 0) return System.Math.Max(0, (int)System.Math.Round((1f - prog) * total));
                }
            }
            catch { }
            return 0;
        }

        private static int ReadIntField(object o, string lower, string upper)
        {
            if (o == null) return 0;
            var p1 = o.GetType().GetProperty(lower, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p1 != null) { try { return System.Convert.ToInt32(p1.GetValue(o, null)); } catch { } }
            var p2 = o.GetType().GetProperty(upper, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p2 != null) { try { return System.Convert.ToInt32(p2.GetValue(o, null)); } catch { } }
            var f1 = o.GetType().GetField(lower, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f1 != null) { try { return System.Convert.ToInt32(f1.GetValue(o)); } catch { } }
            var f2 = o.GetType().GetField(upper, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f2 != null) { try { return System.Convert.ToInt32(f2.GetValue(o)); } catch { } }
            return 0;
        }

        private static int MakeMinuteStamp(int year, int season, int day, int hour, int minute)
        {
            // season: 0..3, day: starts at 1?
            int seasonsPerYear = 4;
            int daysPerSeasonApprox = 30; // approximate; only relative ordering matters for difference
            int stampDays = year * seasonsPerYear * daysPerSeasonApprox + season * daysPerSeasonApprox + (day - 1);
            return stampDays * 24 * 60 + hour * 60 + minute;
        }

        public static void HatchEgg(object obj)
        {
            try
            {
                var egg = UnwrapEgg(obj);
                // Try direct methods on egg instance
                if (egg != null)
                {
                    string[] hatchMethods = { "CHEAT_Hatch", "HatchNow", "ForceHatch", "Finish", "Complete" };
                    for (int i = 0; i < hatchMethods.Length; i++)
                    {
                        var m = egg.GetType().GetMethod(hatchMethods[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (m != null && m.GetParameters().Length == 0) { try { m.Invoke(egg, null); Banner("Egg hatched"); return; } catch { } }
                    }
                    // Set remaining on egg
                    var setSec = egg.GetType().GetMethod("SetRemainingSeconds") ?? egg.GetType().GetMethod("SetSecondsRemaining");
                    if (setSec != null) { try { setSec.Invoke(egg, new object[] { 60 }); Banner("Egg hatch timer set to 00:01"); return; } catch { } }
                    var setMin = egg.GetType().GetMethod("SetRemainingMinutes") ?? egg.GetType().GetMethod("SetMinutesRemaining");
                    if (setMin != null) { try { setMin.Invoke(egg, new object[] { 1 }); Banner("Egg hatch timer set to 00:01"); return; } catch { } }
                    var pSec = egg.GetType().GetProperty("SecondsRemaining", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                             ?? egg.GetType().GetProperty("RemainingSeconds")
                             ?? egg.GetType().GetProperty("TimeRemainingSeconds");
                    if (pSec != null && pSec.CanWrite) { try { pSec.SetValue(egg, 60, null); Banner("Egg hatch timer set to 00:01"); return; } catch { } }
                    var pMin = egg.GetType().GetProperty("MinutesRemaining", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                             ?? egg.GetType().GetProperty("RemainingMinutes")
                             ?? egg.GetType().GetProperty("TimeRemainingMinutes");
                    if (pMin != null && pMin.CanWrite) { try { pMin.SetValue(egg, 1, null); Banner("Egg hatch timer set to 00:01"); return; } catch { } }
                }
                // Try set hatchingDate on record/slot
                object rec = obj;
                // Try set hatchingDate property or field on record
                var hdProp2 = rec.GetType().GetProperty("hatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?? rec.GetType().GetProperty("HatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hdField2 = (hdProp2 == null) ? (rec.GetType().GetField("hatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?? rec.GetType().GetField("HatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)) : null;
                if (hdProp2 != null || hdField2 != null)
                {
                    try
                    {
                        var cal = GameManager.Status.GetCalendarDate;
                        // current time + 1 minute
                        int y = cal.DateTime.Year;
                        int s = (int)cal.DateTime.Season;
                        int d = cal.DateTime.Day;
                        int h = cal.DateTime.Hour;
                        int m = cal.DateTime.Minute + 1;
                        if (m >= 60) { m -= 60; h += 1; }
                        if (h >= 24) { h -= 24; d += 1; }
                        object dateObj = null;
                        if (hdProp2 != null) dateObj = hdProp2.GetValue(rec, null);
                        else if (hdField2 != null) dateObj = hdField2.GetValue(rec);
                        if (dateObj != null)
                        {
                            TryWriteIntField(dateObj, "year", "Year", y);
                            TryWriteIntField(dateObj, "season", "Season", s);
                            TryWriteIntField(dateObj, "day", "Day", d);
                            TryWriteIntField(dateObj, "hour", "Hour", h);
                            TryWriteIntField(dateObj, "minute", "Minute", m);
                            try
                            {
                                if (hdProp2 != null) hdProp2.SetValue(rec, dateObj, null);
                                else if (hdField2 != null) hdField2.SetValue(rec, dateObj);
                            }
                            catch { }
                            Banner("Egg hatch date set to +1 minute");
                            return;
                        }
                    }
                    catch { }
                }
                Banner("Egg hatch: attempted");
            }
            catch (Exception e) { Plugin.Log.LogWarning("HatchEgg failed: " + e.Message); }
        }

        private static void TryWriteIntField(object o, string lower, string upper, int value)
        {
            if (o == null) return;
            var p1 = o.GetType().GetProperty(lower, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p1 != null && p1.CanWrite) { try { p1.SetValue(o, value, null); return; } catch { } }
            var p2 = o.GetType().GetProperty(upper, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p2 != null && p2.CanWrite) { try { p2.SetValue(o, value, null); return; } catch { } }
            var f1 = o.GetType().GetField(lower, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f1 != null) { try { f1.SetValue(o, value); return; } catch { } }
            var f2 = o.GetType().GetField(upper, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f2 != null) { try { f2.SetValue(o, value); return; } catch { } }
        }

        private static object UnwrapEgg(object host)
        {
            object cur = host;
            for (int depth = 0; depth < 3 && cur != null; depth++)
            {
                // if this looks like an egg (has timer/name), return
                if (SafeProp(cur, "SecondsRemaining") != null || SafeProp(cur, "MinutesRemaining") != null || SafeProp(cur, "RemainingSeconds") != null || SafeProp(cur, "RemainingMinutes") != null)
                    return cur;
                if (SafeProp(cur, "Name") != null || SafeProp(cur, "DisplayName") != null || SafeProp(cur, "Code") != null)
                {
                    // might still be an entry that holds timers deeper; continue
                }
                var p = cur.GetType().GetProperty("Egg") ?? cur.GetType().GetProperty("Entry") ?? cur.GetType().GetProperty("Value") ?? cur.GetType().GetProperty("Item");
                if (p != null)
                {
                    try { cur = p.GetValue(cur, null); continue; } catch { return cur; }
                }
                break;
            }
            return cur;
        }

        public static void UpgradeFarm()
        {
            try
            {
                var s = GameManager.Status;
                // Try obvious methods
                string[] mnames = { "CHEAT_UnlockAllUpgrades", "UnlockAllUpgrades", "UnlockAllFarmUpgrades", "UnlockEverything" };
                for (int i = 0; i < mnames.Length; i++)
                {
                    var m = s.GetType().GetMethod(mnames[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (m != null && m.GetParameters().Length == 0) { try { m.Invoke(s, null); Banner("Farm upgrades unlocked"); return; } catch { } }
                }
                // Try FarmStatus / Managers
                var fs = s.FarmStatus;
                if (fs != null)
                {
                    var m2 = fs.GetType().GetMethod("CHEAT_UnlockAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                           ?? fs.GetType().GetMethod("UnlockAll");
                    if (m2 != null && m2.GetParameters().Length == 0) { try { m2.Invoke(fs, null); Banner("Farm upgrades unlocked"); return; } catch { } }
                    // Expand island / regions
                    var exp = fs.GetType().GetMethod("CHEAT_ExpandIsland", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                           ?? fs.GetType().GetMethod("ExpandIsland");
                    if (exp != null && exp.GetParameters().Length == 0) { try { exp.Invoke(fs, null); } catch { } }
                }
                Banner("Farm upgrade: attempted unlock");
            }
            catch (Exception e) { Plugin.Log.LogWarning("UpgradeFarm failed: " + e.Message); }
        }

        public static void WaterAllCrops()
        {
            try
            {
                var s = GameManager.Status;
                var fs = s.FarmStatus;
                if (fs != null)
                {
                    // Try method
                    var m = fs.GetType().GetMethod("CHEAT_WaterAllCrops", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                          ?? fs.GetType().GetMethod("WaterAllCrops");
                    if (m != null) { try { m.Invoke(fs, null); Banner("All crops watered"); return; } catch { } }
                    // Try iterate plots
                    var plotsProp = fs.GetType().GetProperty("CropPlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var plots = plotsProp != null ? plotsProp.GetValue(fs, null) as System.Collections.IEnumerable : null;
                    if (plots != null)
                    {
                        foreach (var p in plots)
                        {
                            var water = p.GetType().GetMethod("Water", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                      ?? p.GetType().GetMethod("SetWatered");
                            if (water != null) { try { water.Invoke(p, null); } catch { } }
                        }
                        Banner("All crops watered");
                        return;
                    }
                }
                Banner("Water crops: attempted");
            }
            catch (Exception e) { Plugin.Log.LogWarning("WaterAllCrops failed: " + e.Message); }
        }

        public static void GrowAllCrops()
        {
            try
            {
                var s = GameManager.Status;
                var fs = s.FarmStatus;
                if (fs != null)
                {
                    // Try method
                    var m = fs.GetType().GetMethod("CHEAT_GrowAllCrops", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                          ?? fs.GetType().GetMethod("GrowAllCrops")
                          ?? fs.GetType().GetMethod("AdvanceGrowthAllCrops");
                    if (m != null) { try { m.Invoke(fs, null); Banner("All crops grown"); return; } catch { } }
                    // Try iterate plots
                    var plotsProp = fs.GetType().GetProperty("CropPlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var plots = plotsProp != null ? plotsProp.GetValue(fs, null) as System.Collections.IEnumerable : null;
                    if (plots != null)
                    {
                        foreach (var p in plots)
                        {
                            var grow = p.GetType().GetMethod("Grow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                     ?? p.GetType().GetMethod("AdvanceGrowth");
                            if (grow != null) { try { grow.Invoke(p, null); } catch { } }
                        }
                        Banner("All crops grown");
                        return;
                    }
                }
                Banner("Grow crops: attempted");
            }
            catch (Exception e) { Plugin.Log.LogWarning("GrowAllCrops failed: " + e.Message); }
        }

        public static void SetTime(int hour, int minute)
        {
            try
            {
                var cal = GameManager.Status.GetCalendarDate;
                int h = ((hour % 24) + 24) % 24;
                int m = ((minute % 60) + 60) % 60;
                // reset to start of day, then tick to target absolute time
                int cur = cal.DateTime.Hour * 60 + cal.DateTime.Minute;
                if (cur != 0) cal.TickMinutes(-cur);
                int target = h * 60 + m;
                if (target != 0) cal.TickMinutes(target);
                Banner("Time set to " + h + ":" + m.ToString("00"));
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetTime failed: " + e.Message); }
        }

        public static void SetDayAndTime(int day, int hour, int minute)
        {
            try
            {
                var cal = GameManager.Status.GetCalendarDate;
                // Normalize time to 00:00 of current day first
                int cur = cal.DateTime.Hour * 60 + cal.DateTime.Minute;
                if (cur != 0) cal.TickMinutes(-cur);
                // Compute absolute delta in days from current day to target day
                int currentDay = cal.DateTime.Day;
                int deltaDays = day - currentDay;
                if (deltaDays != 0) cal.TickMinutes(deltaDays * 24 * 60);
                // Set exact time within day
                int target = ((hour % 24 + 24) % 24) * 60 + ((minute % 60 + 60) % 60);
                if (target != 0) cal.TickMinutes(target);
                Banner("Day set to " + day + ", time set to " + hour + ":" + minute.ToString("00"));
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetDayAndTime failed: " + e.Message); }
        }

        // ===== Monsters & Traits helpers =====
        public static MonsterCharacterStats[] GetActiveMonsters()
        {
            try
            {
                var en = GameManager.Status.EnumerateActiveMonsters();
                var list = new System.Collections.Generic.List<MonsterCharacterStats>();
                foreach (var m in en) list.Add(m);
                return list.ToArray();
            }
            catch (Exception e) { Plugin.Log.LogWarning("GetActiveMonsters failed: " + e.Message); }
            return new MonsterCharacterStats[0];
        }

        public static void RemoveMonster(MonsterCharacterStats m)
        {
            try { GameManager.Status.RemoveActiveMonster(m); Banner("Removed monster: " + m.Name); }
            catch (Exception e) { Plugin.Log.LogWarning("RemoveMonster failed: " + e.Message); }
        }

        public static void GenerateFarmTraitsReport(string path)
        {
            try
            {
                var mons = GetActiveMonsters();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== Farm Traits Report ===");
                sb.AppendLine(System.DateTime.Now.ToString("u"));
                for (int i = 0; i < mons.Length; i++)
                {
                    var m = mons[i]; if (m == null) continue;
                    sb.AppendLine("# " + (i+1) + ". " + m.Name + " (" + m.FarmableSpecies + ", " + m.Gender + ")");
                    foreach (var tr in m.EnumerateTraitInstances())
                    {
                        if (tr == null) continue;
                        string tname = SafeTraitName(tr);
                        int grade = SafeIntProp(tr, "Grade");
                        sb.AppendLine("- " + tname + " [Grade:" + grade + "]");
                        // Effects/Details from definition
                        var def = SafeProp(tr, "TraitDefinition");
                        if (def != null)
                        {
                            string src = SafeProp(def, "TraitSource") != null ? SafeProp(def, "TraitSource").ToString() : "";
                            string targeted = SafeProp(def, "TargetedStat") != null ? SafeProp(def, "TargetedStat").ToString() : "";
                            if (!string.IsNullOrEmpty(src)) sb.AppendLine("  Source: " + src);
                            if (!string.IsNullOrEmpty(targeted)) sb.AppendLine("  TargetedStat: " + targeted);
                            // Try enumerate stat mods if any
                            DumpMods(def, sb, "StatModifiers");
                            DumpMods(def, sb, "PassiveStatMods");
                            DumpMods(def, sb, "Mods");
                            // Description if available
                            var desc = SafeProp(def, "Description"); if (desc != null) sb.AppendLine("  Desc: " + desc);
                        }
                    }
                    sb.AppendLine();
                }
                System.IO.File.WriteAllText(path, sb.ToString());
                Banner("Traits report saved: " + path);
            }
            catch (Exception e) { Plugin.Log.LogWarning("GenerateFarmTraitsReport failed: " + e.Message); }
        }

        private static void DumpMods(object def, System.Text.StringBuilder sb, string fieldOrProp)
        {
            try
            {
                var t = def.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
                object val = null;
                var p = t.GetProperty(fieldOrProp, flags); if (p != null) val = p.GetValue(def, null);
                if (val == null) { var f = t.GetField(fieldOrProp, flags); if (f != null) val = f.GetValue(def); }
                var en = val as System.Collections.IEnumerable; if (en == null || val is string) return;
                int count = 0;
                foreach (var mod in en)
                {
                    if (mod == null) continue;
                    count++;
                    sb.AppendLine("  * Mod: " + mod.ToString());
                }
                if (count == 0) return;
            }
            catch { }
        }

        private static string SafeTraitName(object traitInstance)
        {
            try
            {
                var def = SafeProp(traitInstance, "TraitDefinition");
                if (def != null)
                {
                    var name = SafeProp(def, "Name") ?? SafeProp(def, "DisplayName") ?? SafeProp(def, "Code");
                    if (name != null) return name.ToString();
                }
                var code = SafeProp(traitInstance, "TraitCode") ?? SafeProp(traitInstance, "Code");
                if (code != null) return code.ToString();
            }
            catch { }
            return traitInstance != null ? traitInstance.ToString() : "(null)";
        }

        private static object SafeProp(object o, string name)
        {
            try
            {
                if (o == null) return null;
                var p = o.GetType().GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (p != null && p.CanRead) return p.GetValue(o, null);
            }
            catch { }
            return null;
        }

        private static int SafeIntProp(object o, string name)
        {
            try
            {
                var v = SafeProp(o, name); if (v == null) return 0; return Convert.ToInt32(v);
            }
            catch { return 0; }
        }

        public static void GenerateAllTraitsCatalog(string path)
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                var enumType = Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies, Game");
                if (libType == null || enumType == null) { Plugin.Log.LogWarning("Trait library or enum not found"); return; }
                var lib = UnityEngine.Object.FindObjectOfType(libType);
                if (lib == null) { Plugin.Log.LogWarning("MonsterTraitLibrary instance not found"); return; }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== All Traits Catalog ===");
                sb.AppendLine(System.DateTime.Now.ToString("u"));

                var resolveByType = libType.GetMethod("ResolveMonsterTraitsByType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var all = new System.Collections.Generic.HashSet<object>();

                // by species
                Array speciesValues = Enum.GetValues(enumType);
                for (int i = 0; i < speciesValues.Length; i++)
                {
                    var species = speciesValues.GetValue(i);
                    object traitsByType = null;
                    try { traitsByType = resolveByType.Invoke(lib, new object[] { species }); } catch { traitsByType = null; }
                    if (traitsByType == null) continue;
                    var tbt = traitsByType.GetType();
                    string[] fields = { "OtherSpeciesTraits", "StatLimitTraits", "OtherBloodlineTraits" };
                    for (int f = 0; f < fields.Length; f++)
                    {
                        var fld = tbt.GetField(fields[f], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (fld == null) continue;
                        var arr = fld.GetValue(traitsByType) as System.Collections.IEnumerable;
                        if (arr == null) continue;
                        foreach (var d in arr) if (d != null) all.Add(d);
                    }
                }
                // universal
                var uniFld = libType.GetField("universalTraits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var uniArr = uniFld != null ? uniFld.GetValue(lib) as System.Collections.IEnumerable : null;
                if (uniArr != null) foreach (var d in uniArr) if (d != null) all.Add(d);

                // Write details
                int idx = 1;
                foreach (var def in all)
                {
                    if (def == null) continue;
                    var name = SafeProp(def, "Name") ?? SafeProp(def, "DisplayName") ?? SafeProp(def, "Code");
                    var src = SafeProp(def, "TraitSource");
                    var targeted = SafeProp(def, "TargetedStat");
                    var desc = SafeProp(def, "Description");
                    var max = SafeProp(def, "MaxGrade") ?? SafeProp(def, "MaxLevel") ?? SafeProp(def, "Cap");
                    sb.AppendLine(idx + ". " + (name != null ? name.ToString() : def.ToString()));
                    if (src != null) sb.AppendLine("  Source: " + src);
                    if (targeted != null) sb.AppendLine("  TargetedStat: " + targeted);
                    if (desc != null) sb.AppendLine("  Desc: " + desc);
                    if (max != null) sb.AppendLine("  MaxLevel: " + max);
                    DumpMods(def, sb, "StatModifiers");
                    DumpMods(def, sb, "PassiveStatMods");
                    DumpMods(def, sb, "Mods");
                    sb.AppendLine();
                    idx++;
                }

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                System.IO.File.WriteAllText(path, sb.ToString());
                Banner("All traits catalog saved: " + path);
            }
            catch (Exception e) { Plugin.Log.LogWarning("GenerateAllTraitsCatalog failed: " + e.Message); }
        }

        public static Array GetAllSpecies()
        {
            try
            {
                var t = Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies, Game");
                if (t != null && t.IsEnum) return Enum.GetValues(t);
            }
            catch { }
            return new object[0];
        }

        public static string GetMonsterSpecies(object monster)
        {
            try
            {
                var t = monster.GetType();
                var prop = t.GetProperty("Species") ?? t.GetProperty("FarmableSpecies") ?? t.GetProperty("Type");
                if (prop != null)
                {
                    var v = prop.GetValue(monster, null); return v != null ? v.ToString() : "-";
                }
            }
            catch { }
            return "-";
        }

        public static void SetMonsterSpecies(object monster, string speciesName)
        {
            try
            {
                var t = monster.GetType();
                // Resolve FarmableSpecies enum from Game.dll
                var speciesEnum = Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies, Game", false)
                                  ?? Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies", false);
                if (speciesEnum == null || !speciesEnum.IsEnum)
                    throw new InvalidOperationException("FarmableSpecies enum not found");

                object enumVal = null;
                try { enumVal = Enum.Parse(speciesEnum, speciesName, true); } catch { }
                if (enumVal == null)
                {
                    var names = Enum.GetNames(speciesEnum);
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (names[i].IndexOf(speciesName, StringComparison.OrdinalIgnoreCase) >= 0) { enumVal = Enum.Parse(speciesEnum, names[i]); break; }
                    }
                }
                if (enumVal == null) throw new ArgumentException("Unknown species: " + speciesName);

                // 1) Preferred: if there is a dedicated method (rare), use it
                var changeMethod = t.GetMethod("ChangeMonsterSpecies", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (changeMethod != null && changeMethod.GetParameters().Length == 1)
                {
                    changeMethod.Invoke(monster, new object[] { enumVal });
                }
                else
                {
                    // 2) Directly set private field 'species'
                    var fSpecies = t.GetField("species", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (fSpecies == null) throw new MissingFieldException("species field not found on MonsterCharacterStats");
                    fSpecies.SetValue(monster, enumVal);

                    // 2a) Update monsterAsset using GameManager.CombatCharacterLibrary.ResolveMonsterCharacterAsset(FarmableSpecies)
                    try
                    {
                        var gmType = Type.GetType("TeamNimbus.CloudMeadow.Managers.GameManager, Game", false)
                                   ?? Type.GetType("TeamNimbus.CloudMeadow.Managers.GameManager", false);
                        if (gmType != null)
                        {
                            var pCombatLib = gmType.GetProperty("CombatCharacterLibrary", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            var combatLib = pCombatLib != null ? pCombatLib.GetValue(null, null) : null;
                            if (combatLib != null)
                            {
                                var resolve = combatLib.GetType().GetMethod("ResolveMonsterCharacterAsset", new Type[] { speciesEnum });
                                if (resolve != null)
                                {
                                    var asset = resolve.Invoke(combatLib, new object[] { enumVal });
                                    var fAsset = t.GetField("monsterAsset", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                    if (fAsset != null) fAsset.SetValue(monster, asset);
                                }
                            }
                        }
                    }
                    catch { }

                    // 2b) Try to align palette to species by casting to MonsterPalette and calling ChangeMonsterPalette
                    try
                    {
                        var paletteEnum = Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette, Game", false)
                                         ?? Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette", false);
                        if (paletteEnum != null && paletteEnum.IsEnum)
                        {
                            // cast enum by underlying value
                            var underlying = Convert.ChangeType(enumVal, Enum.GetUnderlyingType(speciesEnum));
                            var paletteVal = Enum.ToObject(paletteEnum, Convert.ChangeType(underlying, Enum.GetUnderlyingType(paletteEnum)));
                            var changePalette = t.GetMethod("ChangeMonsterPalette", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (changePalette != null && changePalette.GetParameters().Length == 1)
                            {
                                changePalette.Invoke(monster, new object[] { paletteVal });
                            }
                            else
                            {
                                var fPalette = t.GetField("palette", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                if (fPalette != null) fPalette.SetValue(monster, paletteVal);
                            }
                        }
                    }
                    catch { }

                    // 2c) Reinitialize data assets and definitions to be safe
                    try
                    {
                        var init = t.GetMethod("InitializeDataAssets", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (init != null) init.Invoke(monster, null);
                    }
                    catch { }

                    // 2d) Trigger sprite refresh
                    try
                    {
                        var pEvent = t.GetProperty("SpriteModifiedEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        var spriteEvt = pEvent != null ? pEvent.GetValue(monster, null) : null;
                        if (spriteEvt == null)
                        {
                            var fEvent = t.GetField("SpriteModifiedEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (fEvent != null) spriteEvt = fEvent.GetValue(monster);
                        }
                        if (spriteEvt != null)
                        {
                            var trig = spriteEvt.GetType().GetMethod("Trigger", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (trig != null) trig.Invoke(spriteEvt, null);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterSpecies failed: " + e.Message); }
        }

        public static void SwapMonsterGender(TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats m)
        {
            try
            {
                if (m != null)
                {
                    var canSwap = m.GetType().GetMethod("CanSwapGender", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var swap = m.GetType().GetMethod("SwapGender", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    bool ok = true;
                    if (canSwap != null)
                    {
                        object r = canSwap.Invoke(m, null);
                        ok = (r is bool) ? (bool)r : true;
                    }
                    if (ok && swap != null) swap.Invoke(m, null);
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SwapMonsterGender failed: " + e.Message); }
        }

        public static void SetMonsterGender(TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats m, string desired)
        {
            try
            {
                if (m == null) return;
                var genderProp = m.GetType().GetProperty("Gender");
                if (genderProp == null) { SwapMonsterGender(m); return; }
                var cur = genderProp.GetValue(m, null);
                var enumType = cur.GetType();
                var target = Enum.Parse(enumType, desired, true);
                if (!cur.Equals(target))
                {
                    SwapMonsterGender(m);
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterGender failed: " + e.Message); }
        }

        public static object[] GetBloodlineTraitDefinitionsForSpecies(string speciesName)
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                var lib = ResolveMonsterTraitLibrary();
                if (lib != null)
                {
                    var resolve = libType.GetMethod("ResolveMonsterTraitsByType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var enumType = Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies, Game");
                    var species = Enum.Parse(enumType, speciesName, true);
                    var traitsByType = resolve.Invoke(lib, new object[] { species });
                    var tbt = traitsByType.GetType();

                    string[] fields = { "OtherBloodlineTraits", "OtherSpeciesTraits", "StatLimitTraits", "BloodlineTraits", "SpeciesTraits" };
                    var list = new System.Collections.Generic.List<object>();
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var fld = tbt.GetField(fields[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (fld == null) continue;
                        object val = null; try { val = fld.GetValue(traitsByType); } catch { }
                        var arr = val as System.Collections.IEnumerable; if (arr == null) continue;
                        foreach (var d in arr) if (d != null) list.Add(d);
                    }
                    return FilterToDefinitionLike(list.ToArray());
                }
            }
            catch { }
            return new object[0];
        }

        public static object[] GetMonsterTraits(object monster)
        {
            try
            {
                var traits = new System.Collections.Generic.List<object>();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var t = monster.GetType();
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        object col = null; try { col = p.GetValue(monster, null); } catch { }
                        AppendTraits(traits, col);
                    }
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        object col = null; try { col = f.GetValue(monster); } catch { }
                        AppendTraits(traits, col);
                    }
                }
                // Deduplicate by reference and keep only instance-like entries
                var uniq = new System.Collections.Generic.List<object>();
                var seen = new System.Collections.Generic.HashSet<object>(new RefEqComparer());
                for (int i = 0; i < traits.Count; i++)
                {
                    var it = traits[i]; if (it == null) continue;
                    if (!IsTraitInstanceLike(it)) continue;
                    if (seen.Add(it)) uniq.Add(it);
                }
                return uniq.ToArray();
            }
            catch { }
            return new object[0];
        }

        private static void AppendTraits(System.Collections.Generic.List<object> list, object col)
        {
            if (col == null) return;
            var en = col as System.Collections.IEnumerable; if (en == null || col is string) return;
            foreach (var item in en) if (item != null) list.Add(item);
        }

        public static object[] GetTraitDefinitionsForSpecies(string speciesName)
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                if (libType != null)
                {
                    var lib = UnityEngine.Object.FindObjectOfType(libType);
                    if (lib != null)
                    {
                        var list = new System.Collections.Generic.List<object>();
                        var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                        var methods = libType.GetMethods(flags);
                        for (int i = 0; i < methods.Length; i++)
                        {
                            var m = methods[i];
                            if (m.Name.IndexOf("Enumerate", StringComparison.OrdinalIgnoreCase) >= 0 && m.GetParameters().Length == 1)
                            {
                                var p = m.GetParameters()[0];
                                if (p.ParameterType.IsEnum && p.ParameterType.FullName.IndexOf("FarmableSpecies") >= 0)
                                {
                                    var enumVal = Enum.Parse(p.ParameterType, speciesName, true);
                                    var en = m.Invoke(lib, new object[] { enumVal }) as System.Collections.IEnumerable;
                                    if (en != null) foreach (var def in en) list.Add(def);
                                }
                            }
                        }
                        if (list.Count > 0) return list.ToArray();
                    }
                }
            }
            catch { }
            return new object[0];
        }

        private static object ResolveMonsterTraitLibrary()
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                if (libType == null) return null;
                // 1) Find active scene object
                var lib = UnityEngine.Object.FindObjectOfType(libType);
                if (lib != null) return lib;
                // 2) Resources (inactive assets, ScriptableObjects)
                var all = Resources.FindObjectsOfTypeAll(libType);
                if (all != null && all.Length > 0) return all.GetValue(0);
                // 3) Static Instance/Singleton property
                var instProp = libType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) ??
                               libType.GetProperty("Singleton", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (instProp != null)
                {
                    try { var inst = instProp.GetValue(null, null); if (inst != null) return inst; } catch { }
                }
                // 4) Static fields that might hold instance
                var sfields = libType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                for (int i = 0; i < sfields.Length; i++)
                {
                    try { var v = sfields[i].GetValue(null); if (v != null && libType.IsInstanceOfType(v)) return v; } catch { }
                }
                return null;
            }
            catch { return null; }
        }

        public static object[] GetAllTraitDefinitions()
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                var lib = ResolveMonsterTraitLibrary();
                var res = new System.Collections.Generic.List<object>();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
                var fields = libType.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    object col = null;
                    try { col = f.IsStatic ? f.GetValue(null) : (lib != null ? f.GetValue(lib) : null); } catch { col = null; }
                    var en = col as System.Collections.IEnumerable; if (en == null || col is string) continue;
                    foreach (var d in en) if (d != null) res.Add(d);
                }
                // Methods that enumerate all
                var methods = libType.GetMethods(flags);
                for (int i = 0; i < methods.Length; i++)
                {
                    var m = methods[i];
                    if (m.GetParameters().Length == 0 && m.ReturnType != typeof(void) && (m.Name.ToLowerInvariant().IndexOf("enumerate") >= 0 || m.Name.ToLowerInvariant().IndexOf("getall") >= 0))
                    {
                        try
                        {
                            var col = m.IsStatic ? m.Invoke(null, null) : (lib != null ? m.Invoke(lib, null) : null);
                            var en = col as System.Collections.IEnumerable; if (en == null || col is string) continue;
                            foreach (var d in en) if (d != null) res.Add(d);
                        }
                        catch { }
                    }
                }
                return FilterToDefinitionLike(res.ToArray());
            }
            catch { }
            return new object[0];
        }

        public static object[] GetUniversalTraitDefinitions()
        {
            var all = GetAllTraitDefinitions();
            var uni = FilterTraitDefinitionsBySource(all, "Universal");
            return DedupTraitDefinitionsByCode(uni);
        }

        private static object[] DedupTraitDefinitionsByCode(object[] defs)
        {
            try
            {
                var list = new System.Collections.Generic.List<object>();
                var seen = new System.Collections.Generic.HashSet<string>();
                for (int i = 0; i < defs.Length; i++)
                {
                    var d = defs[i]; if (d == null) continue;
                    var codeObj = SafeProp(d, "Code") ?? SafeProp(d, "TraitCode");
                    var nameObj = SafeProp(d, "Name") ?? SafeProp(d, "DisplayName");
                    string key = codeObj != null ? codeObj.ToString() : (nameObj != null ? nameObj.ToString() : d.ToString());
                    if (string.IsNullOrEmpty(key)) continue;
                    if (seen.Add(key)) list.Add(d);
                }
                return list.ToArray();
            }
            catch { }
            return defs;
        }

        public static object[] GetTraitDefinitionsForMonster(object monster)
        {
            try
            {
                string speciesName = GetMonsterSpecies(monster);
                var bloodline = GetTraitDefinitionsForSpecies(speciesName);
                var universal = GetUniversalTraitDefinitions();
                // Merge unique by reference
                var list = new System.Collections.Generic.List<object>();
                for (int i = 0; i < bloodline.Length; i++) if (bloodline[i] != null && list.IndexOf(bloodline[i]) < 0) list.Add(bloodline[i]);
                for (int i = 0; i < universal.Length; i++) if (universal[i] != null && list.IndexOf(universal[i]) < 0) list.Add(universal[i]);
                return list.ToArray();
            }
            catch { }
            return new object[0];
        }

        private static object GetTraitDefinitionFromInstance(object traitInstance)
        {
            var def = SafeProp(traitInstance, "TraitDefinition");
            if (def != null) return def;
            // Some instances may expose Code or inner definition differently
            return traitInstance;
        }

        private static bool IsTraitInstanceLike(object o)
        {
            try
            {
                var t = o.GetType();
                // Instance-like: has TraitDefinition and Grade/Level but is not a definition type
                bool hasDef = t.GetProperty("TraitDefinition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null;
                bool hasGrade = t.GetProperty("Grade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null ||
                                t.GetProperty("Level", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null;
                bool nameLooksLikeDef = (t.FullName ?? t.Name).IndexOf("Definition", System.StringComparison.OrdinalIgnoreCase) >= 0;
                return hasDef && hasGrade && !nameLooksLikeDef;
            }
            catch { return true; }
        }

        private static string GetTraitSourceString(object obj)
        {
            try
            {
                var def = UnwrapTraitDefinition(obj);
                var src = SafeProp(def, "TraitSource");
                return src != null ? src.ToString() : string.Empty;
            }
            catch { return string.Empty; }
        }

        // Public wrapper for UI usage
        public static string GetTraitSourceForUI(object obj)
        {
            return GetTraitSourceString(obj);
        }

        private static object UnwrapTraitDefinition(object obj)
        {
            var def = obj;
            try
            {
                var wrapProp = def != null ? def.GetType().GetProperty("TraitDefinition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) : null;
                if (wrapProp != null)
                {
                    var inner = wrapProp.GetValue(def, null);
                    if (inner != null) def = inner;
                }
            }
            catch { }
            return def;
        }

        private static bool TryAddToNamedCollection(object monster, object inst, bool isUniversal)
        {
            try
            {
                var mType = monster.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = mType.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    var n = p.Name.ToLowerInvariant();
                    if (n.IndexOf("trait") < 0) continue;
                    bool universalLike = n.IndexOf("universal") >= 0 || n.IndexOf("generic") >= 0;
                    bool bloodlineLike = n.IndexOf("bloodline") >= 0 || n.IndexOf("species") >= 0 || n.IndexOf("lineage") >= 0;
                    if ((isUniversal && universalLike) || (!isUniversal && bloodlineLike))
                    {
                        var col = p.GetValue(monster, null);
                        if (col == null || col is string) continue;
                        var add = col.GetType().GetMethod("Add");
                        if (add != null && add.GetParameters().Length == 1)
                        { add.Invoke(col, new object[] { inst }); return true; }
                    }
                }
                var fields = mType.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    var n = f.Name.ToLowerInvariant();
                    if (n.IndexOf("trait") < 0) continue;
                    bool universalLike = n.IndexOf("universal") >= 0 || n.IndexOf("generic") >= 0;
                    bool bloodlineLike = n.IndexOf("bloodline") >= 0 || n.IndexOf("species") >= 0 || n.IndexOf("lineage") >= 0;
                    if ((isUniversal && universalLike) || (!isUniversal && bloodlineLike))
                    {
                        var col = f.GetValue(monster);
                        if (col == null || col is string) continue;
                        var add = col.GetType().GetMethod("Add");
                        if (add != null && add.GetParameters().Length == 1)
                        { add.Invoke(col, new object[] { inst }); return true; }
                    }
                }
            }
            catch { }
            return false;
        }

        public static string[] GetAvailablePigments()
        {
            // Use MonsterPalette enum from Game.dll (actual pigment selector), not any Palette UI enums
            try
            {
                var t = Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette, Game", false) ?? Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette", false);
                if (t != null && t.IsEnum)
                {
                    return Enum.GetNames(t);
                }
            }
            catch { }
            // Fallback: attempt older names
            try
            {
                string[] typeNames = { "TeamNimbus.CloudMeadow.Monsters.Pigment", "Pigment" };
                for (int i = 0; i < typeNames.Length; i++)
                {
                    var t2 = Type.GetType(typeNames[i] + ", Game", false) ?? Type.GetType(typeNames[i], false);
                    if (t2 != null && t2.IsEnum) return Enum.GetNames(t2);
                }
            }
            catch { }
            // Minimal safe fallback
            return new string[] { "Cat", "Holstaur", "Centaur", "Harpy", "Wolf", "Demon", "Dragon", "Lamia", "Chimera", "Cyclops", "Crab", "Mermaid" };
        }

        public static string GetMonsterPigment(object monster)
        {
            // Prefer MonsterCharacterStats.Palette (MonsterPalette enum)
            try
            {
                var t = monster.GetType();
                var pPalette = t.GetProperty("Palette", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pPalette != null)
                {
                    var v = pPalette.GetValue(monster, null);
                    return v != null ? v.ToString() : "-";
                }
            }
            catch { }
            // Fallback legacy names
            try
            {
                var t = monster.GetType();
                string[] names = { "Pattern", "ColorPattern", "Skin", "Variant" };
                for (int i = 0; i < names.Length; i++)
                {
                    var p = t.GetProperty(names[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        var v = p.GetValue(monster, null);
                        return v != null ? v.ToString() : "-";
                    }
                }
            }
            catch { }
            return "-";
        }

        public static void SetMonsterPigment(object monster, string pigmentName)
        {
            // Correct way: call ChangeMonsterPalette(MonsterPalette) so SpriteModifiedEvent is triggered
            try
            {
                var t = monster.GetType();
                var paletteEnum = Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette, Game", false)
                                   ?? Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette", false);
                if (paletteEnum != null && paletteEnum.IsEnum)
                {
                    object enumVal = null;
                    try { enumVal = Enum.Parse(paletteEnum, pigmentName, true); } catch { }
                    if (enumVal == null)
                    {
                        var namesEnum = Enum.GetNames(paletteEnum);
                        for (int n = 0; n < namesEnum.Length; n++)
                        {
                            if (namesEnum[n].IndexOf(pigmentName, StringComparison.OrdinalIgnoreCase) >= 0) { enumVal = Enum.Parse(paletteEnum, namesEnum[n]); break; }
                        }
                    }
                    if (enumVal != null)
                    {
                        // Preferred: call ChangeMonsterPalette(newPalette)
                        var change = t.GetMethod("ChangeMonsterPalette", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (change != null && change.GetParameters().Length == 1)
                        {
                            change.Invoke(monster, new object[] { enumVal });
                            RefreshMonsterAfterTrait(monster);
                            return;
                        }
                        // Fallback: set private field 'palette' directly and trigger SpriteModifiedEvent if present
                        var fPalette = t.GetField("palette", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fPalette != null)
                        {
                            fPalette.SetValue(monster, enumVal);
                            // Try to trigger visual refresh
                            var pEvent = t.GetProperty("SpriteModifiedEvent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            var spriteEvt = pEvent != null ? pEvent.GetValue(monster, null) : null;
                            if (spriteEvt == null)
                            {
                                var fEvent = t.GetField("SpriteModifiedEvent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (fEvent != null) spriteEvt = fEvent.GetValue(monster);
                            }
                            if (spriteEvt != null)
                            {
                                var trig = spriteEvt.GetType().GetMethod("Trigger", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (trig != null) { try { trig.Invoke(spriteEvt, null); } catch { } }
                            }
                            RefreshMonsterAfterTrait(monster);
                            return;
                        }
                    }
                }
            }
            catch { }

            // Legacy fallbacks (older builds)
            try
            {
                var t = monster.GetType();
                string[] names = { "Pattern", "ColorPattern", "Skin", "Variant" };
                for (int i = 0; i < names.Length; i++)
                {
                    var p = t.GetProperty(names[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        var pt = p.PropertyType;
                        if (pt.IsEnum)
                        {
                            object val = null;
                            try { val = Enum.Parse(pt, pigmentName, true); } catch { }
                            if (val == null)
                            {
                                var namesEnum = Enum.GetNames(pt);
                                for (int n = 0; n < namesEnum.Length; n++)
                                {
                                    if (namesEnum[n].IndexOf(pigmentName, StringComparison.OrdinalIgnoreCase) >= 0) { val = Enum.Parse(pt, namesEnum[n]); break; }
                                }
                            }
                            if (val != null) { p.SetValue(monster, val, null); RefreshMonsterAfterTrait(monster); return; }
                        }
                        else if (pt == typeof(string))
                        {
                            p.SetValue(monster, pigmentName, null); RefreshMonsterAfterTrait(monster); return;
                        }
                    }
                }
            }
            catch { }
        }

        public static void DumpMonstersDebug()
        {
            try
            {
                var dir = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx");
                dir = System.IO.Path.Combine(dir, "plugins");
                dir = System.IO.Path.Combine(dir, "CloudMeadowCreativeMode");
                var path = System.IO.Path.Combine(dir, "tmp_rovodev_monsters_dump.log");
                System.IO.File.WriteAllText(path, "=== Monster Debug Dump (manual) ===\n");
                var s = GameManager.Status;
                var list = s != null ? s.EnumerateActiveMonsters() : null;
                if (list == null) { System.IO.File.AppendAllText(path, "No active monsters.\n"); Banner("No monsters to dump"); return; }
                int idx = 1;
                foreach (var m in list)
                {
                    if (m == null) continue;
                    System.IO.File.AppendAllText(path, "# Monster " + (idx++) + ": " + m.Name + " (" + m.FarmableSpecies + ")\n");
                    ReflectionUtil.DumpObject(m, (l) => { try { System.IO.File.AppendAllText(path, l + "\n"); } catch { } }, 1, 300);
                    TryDumpAppearanceTo(path, m);
                }
                System.IO.File.AppendAllText(path, "=== End of dump ===\n");
                Banner("Monsters dumped to log file");
            }
            catch (Exception e) { Plugin.Log.LogWarning("DumpMonstersDebug failed: " + e.Message); }
        }

        private static void TryDumpAppearanceTo(string path, object monster)
        {
            try
            {
                var t = monster.GetType();
                string[] keys = { "Pigment", "Pigments", "Palette", "Color", "ColorPattern", "Variant", "Skin", "Appearance", "Visual" };
                for (int i = 0; i < keys.Length; i++)
                {
                    var p = t.GetProperty(keys[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        object v = null; try { v = p.GetValue(monster, null); } catch { }
                        try { System.IO.File.AppendAllText(path, "  * " + keys[i] + ": " + (v != null ? v.ToString() : "null") + "\n"); } catch { }
                        if (v != null && !(v is string) && !v.GetType().IsPrimitive)
                        {
                            ReflectionUtil.DumpObject(v, (l) => { try { System.IO.File.AppendAllText(path, "    " + l + "\n"); } catch { } }, 1, 80);
                        }
                    }
                }
            }
            catch { }
        }

        private static void TryAddToAnyCollection(object monster, object inst)
        {
            try
            {
                var mType = monster.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = mType.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i]; if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var col = p.GetValue(monster, null);
                    if (col == null || col is string) continue;
                    var add = col.GetType().GetMethod("Add");
                    if (add != null && add.GetParameters().Length == 1)
                    { add.Invoke(col, new object[] { inst }); return; }
                }
                var fields = mType.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i]; if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var col = f.GetValue(monster);
                    if (col == null || col is string) continue;
                    var add = col.GetType().GetMethod("Add");
                    if (add != null && add.GetParameters().Length == 1)
                    { add.Invoke(col, new object[] { inst }); return; }
                }
            }
            catch { }
        }

        private static int GetTraitCapacity(object monster, bool universal)
        {
            try
            {
                // Heuristics: try fields/properties that look like capacity/slots
                var mType = monster.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                string[] keys = universal ? new[] { "UniversalTraitSlots", "UniversalTraitsCap", "MaxUniversalTraits", "GenericTraitSlots" }
                                          : new[] { "BloodlineTraitSlots", "SpeciesTraitSlots", "BloodlineTraitsCap", "MaxBloodlineTraits" };
                for (int k = 0; k < keys.Length; k++)
                {
                    var p = mType.GetProperty(keys[k], flags); if (p != null) { var v = p.GetValue(monster, null); return Convert.ToInt32(v); }
                    var f = mType.GetField(keys[k], flags); if (f != null) { var v = f.GetValue(monster); return Convert.ToInt32(v); }
                }
            }
            catch { }
            // Fallback defaults (from your description): bloodline ~8 (4 preset + 4 earned), universal ~10
            return universal ? 10 : 8;
        }

        private static int CountMonsterTraits(object monster, bool universal)
        {
            try
            {
                var mType = monster.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var traits = GetMonsterTraits(monster);
                int count = 0;
                for (int i = 0; i < traits.Length; i++)
                {
                    var inst = traits[i];
                    var src = GetTraitSourceString(inst);
                    bool isUni = src.IndexOf("universal", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (isUni == universal) count++;
                }
                return count;
            }
            catch { return 0; }
        }

        private static bool TraitDefinitionsEqual(object a, object b)
        {
            if (a == null || b == null) return false;
            if (object.ReferenceEquals(a, b)) return true;
            // fallback: compare Code/Name
            var ac = SafeProp(a, "Code") ?? SafeProp(a, "Name") ?? SafeProp(a, "DisplayName");
            var bc = SafeProp(b, "Code") ?? SafeProp(b, "Name") ?? SafeProp(b, "DisplayName");
            if (ac != null && bc != null) return ac.ToString() == bc.ToString();
            return false;
        }

        public static bool MonsterHasTrait(object monster, object traitDefinition)
        {
            try
            {
                // Unwrap wrapper to actual definition if needed
                var td = traitDefinition;
                var wrapProp = td != null ? td.GetType().GetProperty("TraitDefinition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) : null;
                if (wrapProp != null)
                {
                    try { var inner = wrapProp.GetValue(td, null); if (inner != null) td = inner; } catch { }
                }
                var traits = GetMonsterTraits(monster);
                for (int i = 0; i < traits.Length; i++)
                {
                    var inst = traits[i]; if (inst == null) continue;
                    var def = GetTraitDefinitionFromInstance(inst);
                    if (TraitDefinitionsEqual(def, td)) return true;
                }
            }
            catch { }
            return false;
        }

        public static bool TryAddTraitToMonster(object monster, object traitDefinition, int grade)
        {
            try
            {
                if (MonsterHasTrait(monster, traitDefinition)) { Banner("Trait already present"); return false; }
                AddTraitToMonster(monster, traitDefinition, grade);
                Banner("Trait added");
                return true;
            }
            catch (Exception e) { Plugin.Log.LogWarning("TryAddTraitToMonster failed: " + e.Message); }
            return false;
        }

        public static int GetTraitMaxGrade(object traitInstance)
        {
            try
            {
                var t = traitInstance.GetType();
                var p = t.GetProperty("MaxGrade") ?? t.GetProperty("MaxLevel") ?? t.GetProperty("Cap");
                if (p != null) { var v = p.GetValue(traitInstance, null); return Convert.ToInt32(v); }
            }
            catch { }
            return 5; // default
        }

        public static void SetTraitGrade(object traitInstance, int grade)
        {
            try
            {
                if (traitInstance == null) return;
                int target = Mathf.Clamp(grade, 1, 5);
                var t = traitInstance.GetType();

                // Preferred path: Increase/Reduce loops using readable Grade/Level
                var pGet = t.GetProperty("Grade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                           ?? t.GetProperty("Level", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var miInc = t.GetMethod("IncreaseGrade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                           ?? t.GetMethod("LevelUp", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var miDec = t.GetMethod("ReduceGrade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                           ?? t.GetMethod("LevelDown", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pGet != null && (miInc != null || miDec != null))
                {
                    int cur = Convert.ToInt32(pGet.GetValue(traitInstance, null));
                    int safety = 20;
                    while (cur < target && miInc != null && safety-- > 0)
                    {
                        miInc.Invoke(traitInstance, null);
                        cur = Convert.ToInt32(pGet.GetValue(traitInstance, null));
                    }
                    while (cur > target && miDec != null && safety-- > 0)
                    {
                        miDec.Invoke(traitInstance, null);
                        cur = Convert.ToInt32(pGet.GetValue(traitInstance, null));
                    }
                    return;
                }

                // Direct setter methods
                string[] setterNames = { "SetGrade", "SetLevel", "ApplyGrade", "SetToLevel", "ForceSetGrade", "ForceSetLevel" };
                for (int i = 0; i < setterNames.Length; i++)
                {
                    var m = t.GetMethod(setterNames[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (m != null && m.GetParameters().Length == 1)
                    {
                        try { m.Invoke(traitInstance, new object[] { target }); return; } catch { }
                    }
                }

                // Try writable property Grade/Level
                var pSet = t.GetProperty("Grade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                         ?? t.GetProperty("Level", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pSet != null && pSet.CanWrite)
                {
                    try { pSet.SetValue(traitInstance, target, null); return; } catch { }
                }

                // Try backing fields
                string[] fldNames = { "grade", "_grade", "m_Grade", "level", "_level", "m_Level" };
                for (int i = 0; i < fldNames.Length; i++)
                {
                    var f = t.GetField(fldNames[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (f != null)
                    {
                        try { f.SetValue(traitInstance, target); return; } catch { }
                    }
                }

                Plugin.Log.LogWarning("SetTraitGrade: no applicable method/property found");
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetTraitGrade failed: " + e.Message); }
        }

        private static void RefreshMonsterAfterTrait(object monster)
        {
            try
            {
                if (monster == null) return;
                var t = monster.GetType();
                string[] mnames = {
                    "RecalculateStats", "RefreshStats", "RefreshDerivedStats", "ApplyTraits", "RebuildTraitEffects",
                    "ReapplyTraits", "Recompute", "OnTraitsChanged", "UpdateModifiers", "Recalculate"
                };
                for (int i = 0; i < mnames.Length; i++)
                {
                    var m = t.GetMethod(mnames[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (m != null && m.GetParameters().Length == 0)
                    {
                        try { m.Invoke(monster, null); } catch { }
                    }
                }
            }
            catch { }
        }

        public static void MaxTraitGrade(object traitInstance)
        {
            try
            {
                if (traitInstance == null) return;
                int max = GetTraitMaxGrade(traitInstance);
                SetTraitGrade(traitInstance, max);
            }
            catch (Exception e) { Plugin.Log.LogWarning("MaxTraitGrade failed: " + e.Message); }
        }

        public static object[] FilterTraitDefinitionsBySource(object[] defs, string sourceContains)
        {
            var list = new System.Collections.Generic.List<object>();
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i]; if (d == null) continue;
                try
                {
                    var t = d.GetType();
                    // Prefer property on definition type
                    var p = t.GetProperty("TraitSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    object val = null; if (p != null) val = p.GetValue(d, null);
                    // Some libraries hold a nested Definition object
                    if (val == null)
                    {
                        var defP = t.GetProperty("TraitDefinition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (defP != null)
                        {
                            var inner = defP.GetValue(d, null);
                            if (inner != null)
                            {
                                var innerSrcP = inner.GetType().GetProperty("TraitSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (innerSrcP != null) val = innerSrcP.GetValue(inner, null);
                            }
                        }
                    }
                    var s = val != null ? val.ToString() : string.Empty;
                    if (s.IndexOf(sourceContains, StringComparison.OrdinalIgnoreCase) >= 0) list.Add(d);
                }
                catch { }
            }
            return list.ToArray();
        }

        private static object[] FilterToDefinitionLike(object[] arr)
        {
            var list = new System.Collections.Generic.List<object>();
            for (int i = 0; i < arr.Length; i++)
            {
                var d = arr[i]; if (d == null) continue;
                try
                {
                    var t = d.GetType();
                    string tn = t.FullName != null ? t.FullName : t.Name;
                    // Exclude obvious instances with Grade/Level
                    if (SafeProp(d, "Grade") != null || SafeProp(d, "Level") != null) continue;
                    // Resolve TraitSource directly or via inner TraitDefinition
                    object src = SafeProp(d, "TraitSource");
                    if (src == null)
                    {
                        var inner = SafeProp(d, "TraitDefinition");
                        if (inner != null) src = SafeProp(inner, "TraitSource");
                    }
                    // Require name/code field
                    object name = SafeProp(d, "Name") ?? SafeProp(d, "DisplayName") ?? SafeProp(d, "Code");
                    if (name == null && SafeProp(d, "TraitDefinition") != null)
                    {
                        var inner = SafeProp(d, "TraitDefinition");
                        name = SafeProp(inner, "Name") ?? SafeProp(inner, "DisplayName") ?? SafeProp(inner, "Code");
                    }
                    bool typeOk = (tn != null && tn.IndexOf("Definition", System.StringComparison.OrdinalIgnoreCase) >= 0) || src != null;
                    if (typeOk && name != null)
                    {
                        list.Add(d);
                    }
                }
                catch { }
            }
            return list.ToArray();
        }

        private sealed class RefEqComparer : System.Collections.Generic.IEqualityComparer<object>
        {
            public new bool Equals(object x, object y) { return object.ReferenceEquals(x, y); }
            public int GetHashCode(object obj) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
        }

        public static void AddTraitToMonster(object monster, object traitDefinition, int grade)
        {
            try
            {
                var instType = Type.GetType("TeamNimbus.CloudMeadow.Traits.TraitInstance, Game");
                if (instType == null) { Plugin.Log.LogWarning("TraitInstance type not found"); return; }

                // Determine trait source
                var source = GetTraitSourceString(traitDefinition);
                bool isUniversal = source.IndexOf("universal", StringComparison.OrdinalIgnoreCase) >= 0;

                // Capacity check
                int cur = CountMonsterTraits(monster, isUniversal);
                if (isUniversal)
                {
                    int capU = GetTraitCapacity(monster, true); // default 10 if unknown
                    if (capU > 0 && cur >= capU) { Banner("Universal trait slots full"); return; }
                }
                else
                {
                    // Bloodline: strictly allow max 4 total (per request)
                    int totalBloodline = CountMonsterTraits(monster, false);
                    if (totalBloodline >= 4) { Banner("Bloodline trait slots full"); return; }
                }

                // Try factory first on definition or library
                object inst = null;
                var def = UnwrapTraitDefinition(traitDefinition);

                var defType = def != null ? def.GetType() : null;
                var createOnDef = defType != null ? (defType.GetMethod("CreateTraitInstance") ?? defType.GetMethod("CreateInstance")) : null;
                if (createOnDef != null)
                {
                    try { inst = createOnDef.GetParameters().Length == 0 ? createOnDef.Invoke(def, null) : createOnDef.Invoke(def, new object[] { grade }); } catch { inst = null; }
                }
                if (inst == null)
                {
                    var lib = ResolveMonsterTraitLibrary();
                    if (lib != null)
                    {
                        var m = lib.GetType().GetMethod("CreateTraitInstance") ?? lib.GetType().GetMethod("CreateInstance");
                        if (m != null)
                        {
                            try { inst = m.GetParameters().Length == 1 ? m.Invoke(lib, new object[] { def }) : m.Invoke(lib, new object[] { def, grade }); } catch { inst = null; }
                        }
                    }
                }

                // Fallback: create without calling ctor
                if (inst == null)
                {
                    inst = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(instType);
                    var init = instType.GetMethod("InitializeTraitDefinition");
                    if (init != null) init.Invoke(inst, new object[] { def });
                }

                // set grade/level using robust method (no direct setter required)
                try { SetTraitGrade(inst, grade); } catch { }

                // Prefer adding into the correct collection by source
                if (TryAddToNamedCollection(monster, inst, isUniversal)) return;

                // Otherwise fallback to dedicated API
                var mType = monster.GetType();
                var addTraitInst = mType.GetMethod("AddTraitInstance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (addTraitInst != null && addTraitInst.GetParameters().Length == 1)
                {
                    try { addTraitInst.Invoke(monster, new object[] { inst }); return; } catch { }
                }
                var addTraitDef = mType.GetMethod("AddTrait", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (addTraitDef != null)
                {
                    var ps = addTraitDef.GetParameters();
                    try
                    {
                        if (ps.Length == 1) { addTraitDef.Invoke(monster, new object[] { def }); return; }
                        if (ps.Length == 2) { addTraitDef.Invoke(monster, new object[] { def, grade }); return; }
                    }
                    catch { }
                }

                // As last resort: add to any trait collection
                TryAddToAnyCollection(monster, inst);
            }
            catch (Exception e) { Plugin.Log.LogWarning("AddTraitToMonster failed: " + e.Message); }
        }

        public static void RemoveTraitFromMonster(object monster, object traitInstance)
        {
            try
            {
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var t = monster.GetType();

                // Pre-pass: if Universal trait, remove directly from universalTraits field for reliability
                var defInitial = GetTraitDefinitionFromInstance(traitInstance);
                var src = GetTraitSourceString(defInitial);
                if (src.IndexOf("universal", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (TryRemoveFromListField(monster, "universalTraits", traitInstance, defInitial)) { RefreshMonsterAfterTrait(monster); return; }
                }

                // 1) Try dedicated API on monster
                var rmInst = t.GetMethod("RemoveTraitInstance", flags);
                if (rmInst != null && rmInst.GetParameters().Length == 1)
                {
                    try { rmInst.Invoke(monster, new object[] { traitInstance }); RefreshMonsterAfterTrait(monster); return; } catch { }
                }
                var rmDef = t.GetMethod("RemoveTrait", flags);
                if (rmDef != null)
                {
                    var def = defInitial;
                    var ps = rmDef.GetParameters();
                    try
                    {
                        if (ps.Length == 1) { rmDef.Invoke(monster, new object[] { def }); RefreshMonsterAfterTrait(monster); return; }
                        if (ps.Length == 2) { rmDef.Invoke(monster, new object[] { def, 0 }); RefreshMonsterAfterTrait(monster); return; }
                    }
                    catch { }
                }

                // 2) Try collections; remove exact instance
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i]; if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var col = p.GetValue(monster, null);
                    var rem = col != null ? col.GetType().GetMethod("Remove") : null;
                    if (rem != null && rem.GetParameters().Length == 1)
                    { try { rem.Invoke(col, new object[] { traitInstance }); RefreshMonsterAfterTrait(monster); return; } catch { } }
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i]; if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var col = f.GetValue(monster);
                    var rem = col != null ? col.GetType().GetMethod("Remove") : null;
                    if (rem != null && rem.GetParameters().Length == 1)
                    { try { rem.Invoke(col, new object[] { traitInstance }); RefreshMonsterAfterTrait(monster); return; } catch { } }
                }

                // 3) Remove by definition match
                var defMatch = defInitial;
                // scan collections and find item with same def
                for (int pass = 0; pass < 2; pass++)
                {
                    props = t.GetProperties(flags);
                    for (int i = 0; i < props.Length; i++)
                    {
                        var p = props[i]; if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        var col = p.GetValue(monster, null) as System.Collections.IEnumerable; if (col == null) continue;
                        object toRemove = null; var colObj = p.GetValue(monster, null);
                        foreach (var it in col)
                        {
                            if (TraitDefinitionsEqual(GetTraitDefinitionFromInstance(it), defMatch)) { toRemove = it; break; }
                        }
                        if (toRemove != null)
                        {
                            var rem = colObj.GetType().GetMethod("Remove");
                            if (rem != null) { try { rem.Invoke(colObj, new object[] { toRemove }); RefreshMonsterAfterTrait(monster); return; } catch { } }
                        }
                    }
                    fields = t.GetFields(flags);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var f = fields[i]; if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        var col = f.GetValue(monster) as System.Collections.IEnumerable; if (col == null) continue;
                        object toRemove = null; var colObj = f.GetValue(monster);
                        foreach (var it in col)
                        {
                            if (TraitDefinitionsEqual(GetTraitDefinitionFromInstance(it), defMatch)) { toRemove = it; break; }
                        }
                        if (toRemove != null)
                        {
                            var rem = colObj.GetType().GetMethod("Remove");
                            if (rem != null) { try { rem.Invoke(colObj, new object[] { toRemove }); RefreshMonsterAfterTrait(monster); return; } catch { } }
                        }
                    }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("RemoveTraitFromMonster failed: " + e.Message); }
        }

        // Remove from a private List<TraitInstance> field by name (e.g., "universalTraits")
        private static bool TryRemoveFromListField(object monster, string fieldName, object traitInstance, object defMatch)
        {
            try
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var f = monster.GetType().GetField(fieldName, flags);
                if (f == null) return false;
                var list = f.GetValue(monster);
                if (list == null) return false;
                var listType = list.GetType();
                var asEnumerable = list as System.Collections.IEnumerable;
                object toRemove = null;
                foreach (var it in asEnumerable)
                {
                    if (traitInstance != null && object.ReferenceEquals(it, traitInstance)) { toRemove = it; break; }
                    if (TraitDefinitionsEqual(GetTraitDefinitionFromInstance(it), defMatch)) { toRemove = it; break; }
                }
                if (toRemove != null)
                {
                    var rem = listType.GetMethod("Remove", new Type[] { toRemove.GetType() });
                    if (rem == null) rem = listType.GetMethod("Remove");
                    if (rem != null)
                    {
                        rem.Invoke(list, new object[] { toRemove });
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        // Cheats: Max all monsters loyalty
        public static void MaxAllMonstersLoyalty()
        {
            try
            {
                var list = GameManager.Status.EnumerateActiveMonsters();
                int cnt = 0;
                foreach (var m in list)
                {
                    try
                    {
                        var t = m.GetType();
                        var fLoyalty = t.GetField("loyalty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var fIsLoyal = t.GetField("isLoyal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var fDaysNotFed = t.GetField("daysNotFed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fLoyalty != null) fLoyalty.SetValue(m, 110);
                        if (fIsLoyal != null) fIsLoyal.SetValue(m, true);
                        if (fDaysNotFed != null) fDaysNotFed.SetValue(m, 0);
                        cnt++;
                    }
                    catch { }
                }
                Banner("Max Loyalty for " + cnt + " monsters");
            }
            catch (Exception e) { Plugin.Log.LogWarning("MaxAllMonstersLoyalty failed: " + e.Message); }
        }

        // Cheats: Set Extra Harvest Times charges to a fixed value for all monsters
        public static void SetExtraHarvestTimesForAll(int charges)
        {
            try
            {
                var list = GameManager.Status.EnumerateActiveMonsters();
                int cnt = 0;
                var statChargesEnum = Type.GetType("TeamNimbus.CloudMeadow.Traits.StatModifiersThatUseCharges, Game");
                var traitInstanceType = Type.GetType("TeamNimbus.CloudMeadow.Traits.TraitInstance, Game");
                foreach (var m in list)
                {
                    try
                    {
                        var t = m.GetType();
                        var meth = t.GetMethod("EnumerateTraitsWithCharges", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (meth != null && statChargesEnum != null)
                        {
                            var val = Enum.Parse(statChargesEnum, "ExtraHarvestTimes");
                            var enumerable = meth.Invoke(m, new object[] { val }) as System.Collections.IEnumerable;
                            if (enumerable != null)
                            {
                                foreach (var ti in enumerable)
                                {
                                    try
                                    {
                                        var f = traitInstanceType.GetField("charges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        if (f != null) { f.SetValue(ti, charges); cnt++; }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }
                }
                Banner("Harvest times set to " + charges + " (" + cnt + " traits)");
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetExtraHarvestTimesForAll failed: " + e.Message); }
        }

        // Cheats: Ultra Bread toggle (makes Bread give +999 to six stats)
        private static bool _ultraBreadEnabled = false;
        public static bool UltraBreadEnabled { get { return _ultraBreadEnabled; } }
        private static System.Collections.Generic.Dictionary<object, object> _savedBreadData = new System.Collections.Generic.Dictionary<object, object>();

        public static void ToggleUltraBread()
        {
            try
            {
                _ultraBreadEnabled = !_ultraBreadEnabled;
                ApplyUltraBread(_ultraBreadEnabled);
                Banner("Ultra Bread: " + (_ultraBreadEnabled ? "ON" : "OFF"));
            }
            catch (Exception e) { Plugin.Log.LogWarning("ToggleUltraBread failed: " + e.Message); }
        }

        private static void ApplyUltraBread(bool enable)
        {
            var itemLib = GameManager.ItemLibrary;
            if (itemLib == null) return;
            var libType = itemLib.GetType();
            var fAll = libType.GetField("allItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var all = fAll != null ? fAll.GetValue(itemLib) as System.Collections.IList : null;
            if (all == null) return;

            var otherFoodType = Type.GetType("TeamNimbus.CloudMeadow.Items.OtherFoodItemDefinition, Game");
            var cookedType = Type.GetType("TeamNimbus.CloudMeadow.Items.CookedItemDefinition, Game");
            var passiveStatModType = Type.GetType("TeamNimbus.CloudMeadow.PassiveStatMod, Game");
            var statModifiersEnum = Type.GetType("TeamNimbus.CloudMeadow.StatModifiers, Game");

            foreach (var def in all)
            {
                try
                {
                    var asIEdible = def.GetType().GetInterface("TeamNimbus.CloudMeadow.Items.IEdibleItemDefinition");
                    if (asIEdible == null) continue;

                    var foodTagsProp = def.GetType().GetProperty("FoodTags");
                    if (foodTagsProp == null) continue;
                    var tagsVal = foodTagsProp.GetValue(def, null);
                    if (tagsVal == null) continue;
                    int tagsInt = Convert.ToInt32(tagsVal);
                    if ((tagsInt & 16384) == 0) continue; // FoodTags.Bread

                    if (otherFoodType != null && otherFoodType.IsInstanceOfType(def))
                    {
                        var fMods = otherFoodType.GetField("statModifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fMods == null || passiveStatModType == null || statModifiersEnum == null) continue;
                        var current = fMods.GetValue(def);
                        if (enable)
                        {
                            if (!_savedBreadData.ContainsKey(def)) _savedBreadData[def] = current;
                            var arr = Array.CreateInstance(passiveStatModType, 6);
                            string[] stats = new[] { "Physique", "Stamina", "Intuition", "Swiftness", "Experience", "HealingFromFood" };
                            var modTypeEnum = Type.GetType("TeamNimbus.Common.Utility.ModifierType, Common") ?? Type.GetType("TeamNimbus.Common.Utility.ModifierType");
                            for (int i = 0; i < stats.Length; i++)
                            {
                                var stat = Enum.Parse(statModifiersEnum, stats[i]);
                                object modTypeVal = (stats[i] == "Experience" || stats[i] == "HealingFromFood")
                                    ? Enum.Parse(modTypeEnum, "IndependentScalarWithoutBaseValue")
                                    : Enum.Parse(modTypeEnum, "AddedModifier");
                                var ctor = passiveStatModType.GetConstructor(new Type[] { statModifiersEnum, typeof(float), modTypeEnum });
                                var psm = ctor.Invoke(new object[] { stat, 999f, modTypeVal });
                                arr.SetValue(psm, i);
                            }
                            fMods.SetValue(def, arr);
                        }
                        else
                        {
                            object saved;
                            if (_savedBreadData.TryGetValue(def, out saved))
                            {
                                fMods.SetValue(def, saved);
                            }
                        }
                    }
                    else if (cookedType != null && cookedType.IsInstanceOfType(def))
                    {
                        var fBase = cookedType.GetField("baseFoodEffectsData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fBase == null) continue;
                        var boostStruct = fBase.GetValue(def);
                        if (boostStruct == null) continue;
                        var boostType = boostStruct.GetType();
                        if (enable)
                        {
                            if (!_savedBreadData.ContainsKey(def)) _savedBreadData[def] = boostStruct;
                            var newBoost = boostStruct;
                            SetBoostField(newBoost, boostType, "physiqueBoost", 999f);
                            SetBoostField(newBoost, boostType, "staminaBoost", 999f);
                            SetBoostField(newBoost, boostType, "intuitionBoost", 999f);
                            SetBoostField(newBoost, boostType, "swiftnessBoost", 999f);
                            SetBoostField(newBoost, boostType, "experienceBoost", 999f);
                            SetBoostField(newBoost, boostType, "healingBoost", 999f);
                            fBase.SetValue(def, newBoost);
                        }
                        else
                        {
                            object saved;
                            if (_savedBreadData.TryGetValue(def, out saved))
                            {
                                fBase.SetValue(def, saved);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private static void SetBoostField(object boostStruct, Type boostType, string fieldName, float value)
        {
            try
            {
                var f = boostType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f == null) return;
                var simple = f.GetValue(boostStruct);
                var simpleType = simple.GetType();
                var fVal = simpleType.GetField("value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fVal != null)
                {
                    object boxed = simple;
                    fVal.SetValue(boxed, value);
                    f.SetValue(boostStruct, boxed);
                }
            }
            catch { }
        }

        private static void Banner(string msg)
        {
            try { TeamNimbus.CloudMeadow.Utilities.BannerMessage.ShowMessage(msg, 1.2f, null); } catch { }
        }

        public static string BuildQuickStatus()
        {
            try
            {
                var s = GameManager.Status;
                var dt = s.GetCalendarDate;
                return string.Format("Korona: {0} | Shards: {1} | Monsters: {2}/{3} | Protag Lv {4} {5} | Season: {6} Day: {7} Hour: {8}", s.KoronaBalance, s.NumUpgradeShards, s.NumMonstersOnTheFarm, s.FarmStatus.ResolveNumberOfMonsterSpotsOnFarm(), s.ProtagonistStats.Level, s.ProtagonistStats.Gender, dt.DateTime.Season, dt.DateTime.Day, dt.DateTime.Hour);
            }
            catch (Exception e)
            {
                return "Status unavailable: " + e.Message;
            }
        }
    }
}
