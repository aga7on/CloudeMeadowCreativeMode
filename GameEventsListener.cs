using System;
using System.Reflection;
using TeamNimbus.CloudMeadow.Managers;
using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal class GameEventsListener : MonoBehaviour
    {
        private Delegate _seasonChangedHandler;
        private Delegate _startOfNewDayHandler;
        private Delegate _hourChangedHandler;

        private bool _monsterDebugDumped;
        private int _frames;
        private string _debugLogPath;

        private void OnEnable()
        {
            TryRegisterEvents();
            try
            {
                var dir = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx");
                dir = System.IO.Path.Combine(dir, "plugins");
                dir = System.IO.Path.Combine(dir, "CloudMeadowCreativeMode");
                _debugLogPath = System.IO.Path.Combine(dir, "tmp_rovodev_monsters_dump.log");
                System.IO.File.WriteAllText(_debugLogPath, "=== Monster Debug Dump ===\n");
                _monsterDebugDumped = false;
                _frames = 0;
            }
            catch { }
        }

        private void OnDisable()
        {
            TryUnregisterEvents();
        }

        private void Update()
        {
            // Wait a few frames after load, then attempt dump once
            if (_monsterDebugDumped) return;
            _frames++;
            if (_frames < 180) return; // ~3s at 60fps
            TryDumpMonsters();
            _monsterDebugDumped = true;
        }

        private void TryRegisterEvents()
        {
            try
            {
                var gmType = typeof(GameManager);

                // SeasonChangedEvent (no args)
                var seasonField = gmType.GetField("SeasonChangedEvent", BindingFlags.Public | BindingFlags.Static);
                var seasonEvt = (seasonField != null) ? seasonField.GetValue(null) : null;
                if (seasonEvt != null)
                {
                    var mi = seasonEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Func<bool>) });
                    if (mi == null) mi = seasonEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Action) });
                    if (mi != null)
                    {
                        Action a = () => LogBuffer.Add("Season changed");
                        _seasonChangedHandler = a;
                        mi.Invoke(seasonEvt, new object[] { a });
                    }
                }

                // StartOfNewDayEvent (no args)
                var newDayField = gmType.GetField("StartOfNewDayEvent", BindingFlags.Public | BindingFlags.Static);
                var newDayEvt = (newDayField != null) ? newDayField.GetValue(null) : null;
                if (newDayEvt != null)
                {
                    var mi = newDayEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Func<bool>) });
                    if (mi == null) mi = newDayEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Action) });
                    if (mi != null)
                    {
                        Action a = () => LogBuffer.Add("Start of new day");
                        _startOfNewDayHandler = a;
                        mi.Invoke(newDayEvt, new object[] { a });
                    }
                }

                // HourChangedEvent (int hours)
                var hourField = gmType.GetField("HourChangedEvent", BindingFlags.Public | BindingFlags.Static);
                var hourEvt = (hourField != null) ? hourField.GetValue(null) : null;
                if (hourEvt != null)
                {
                    var mi = hourEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Func<int, bool>) });
                    if (mi == null) mi = hourEvt.GetType().GetMethod("RegisterHandler", new[] { typeof(Action<int>) });
                    if (mi != null)
                    {
                        Action<int> a = (h) => LogBuffer.Add("Hours passed: " + h);
                        _hourChangedHandler = a;
                        mi.Invoke(hourEvt, new object[] { a });
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("GameEventsListener register failed: " + e.Message);
            }
        }

        private void AppendDebug(string line)
        {
            try { System.IO.File.AppendAllText(_debugLogPath, line + "\n"); } catch { }
        }

        private void TryDumpMonsters()
        {
            try
            {
                var s = GameManager.Status;
                var list = s != null ? s.EnumerateActiveMonsters() : null;
                if (list == null) { AppendDebug("No active monsters."); return; }
                int idx = 1;
                foreach (var m in list)
                {
                    if (m == null) continue;
                    AppendDebug("# Monster " + (idx++) + ": " + m.Name + " (" + m.FarmableSpecies + ")");
                    // dump core fields
                    ReflectionUtil.DumpObject(m, AppendDebug, 1, 300);
                    // try visual/appearance-related properties
                    TryDumpAppearance(m);
                }
                AppendDebug("=== End of dump ===");
            }
            catch (Exception e)
            {
                AppendDebug("Dump error: " + e.Message);
            }
        }

        private void TryDumpAppearance(object monster)
        {
            try
            {
                var t = monster.GetType();
                string[] keys = { "Pigment", "Pigments", "Palette", "Color", "ColorPattern", "Variant", "Skin", "Appearance", "Visual" };
                for (int i = 0; i < keys.Length; i++)
                {
                    var p = t.GetProperty(keys[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (p != null)
                    {
                        object v = null; try { v = p.GetValue(monster, null); } catch { }
                        AppendDebug("  * " + keys[i] + ": " + (v != null ? v.ToString() : "null"));
                        // If nested appearance object, dump shallow
                        if (v != null && !(v is string) && !v.GetType().IsPrimitive)
                        {
                            ReflectionUtil.DumpObject(v, (l) => AppendDebug("    " + l), 1, 80);
                        }
                    }
                }
            }
            catch (Exception e) { AppendDebug("Appearance dump error: " + e.Message); }
        }

        private void TryUnregisterEvents()
        {
            try
            {
                var gmType = typeof(GameManager);

                var seasonField = gmType.GetField("SeasonChangedEvent", BindingFlags.Public | BindingFlags.Static);
                var seasonEvt = (seasonField != null) ? seasonField.GetValue(null) : null;

                var newDayField = gmType.GetField("StartOfNewDayEvent", BindingFlags.Public | BindingFlags.Static);
                var newDayEvt = (newDayField != null) ? newDayField.GetValue(null) : null;

                var hourField = gmType.GetField("HourChangedEvent", BindingFlags.Public | BindingFlags.Static);
                var hourEvt = (hourField != null) ? hourField.GetValue(null) : null;

                if (seasonEvt != null && _seasonChangedHandler != null)
                {
                    var unregS = seasonEvt.GetType().GetMethod("UnregisterHandler");
                    if (unregS != null) unregS.Invoke(seasonEvt, new object[] { _seasonChangedHandler });
                }
                if (newDayEvt != null && _startOfNewDayHandler != null)
                {
                    var unregD = newDayEvt.GetType().GetMethod("UnregisterHandler");
                    if (unregD != null) unregD.Invoke(newDayEvt, new object[] { _startOfNewDayHandler });
                }
                if (hourEvt != null && _hourChangedHandler != null)
                {
                    var unregH = hourEvt.GetType().GetMethod("UnregisterHandler");
                    if (unregH != null) unregH.Invoke(hourEvt, new object[] { _hourChangedHandler });
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("GameEventsListener unregister failed: " + e.Message);
            }
        }
    }
}
