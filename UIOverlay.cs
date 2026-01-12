using System;
using System.Collections.Generic;
using UnityEngine;
using TeamNimbus.CloudMeadow;

namespace CloudMeadow.CreativeMode
{
    internal partial class UIOverlay : MonoBehaviour
    {
        // Core state
        private bool _visible;
        private Rect _windowRect = new Rect(50, 50, 1000, 700);
        private Vector2 _scroll;
        private Vector2 _logScroll;
        private bool _logCollapsed = false;
        private string _activeTab = "Overview";

        // Roots for diagnostics/overview
        private List<object> _rootsAll = new List<object>();
        private List<object> _roots = new List<object>();
        private string _filter = string.Empty;
        private int _maxDepth = 2;

        private void Start()
        {
            Rescan();
        }

        private void Update()
        {
            if (Input.GetKeyDown(Plugin.ToggleOverlayKey.Value))
            {
                _visible = !_visible;
                if (_visible) Rescan();
            }
            if (Input.GetKeyDown(Plugin.RefreshScanKey.Value))
            {
                Rescan();
            }
            if (Input.GetKeyDown(Plugin.UnlockGalleryKey.Value))
            {
                GameApi.UnlockAllGallery();
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;
            GUI.depth = 0;
            _windowRect = GUILayout.Window(0xC10AD, _windowRect, DrawWindow, "Creative Mode (F6)");
            DrawLogOverlay();
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_activeTab == "Overview", "Overview", GUI.skin.button)) _activeTab = "Overview";
            if (GUILayout.Toggle(_activeTab == "Player", "Player", GUI.skin.button)) _activeTab = "Player";
            if (GUILayout.Toggle(_activeTab == "Farm", "Farm", GUI.skin.button)) _activeTab = "Farm";
            if (GUILayout.Toggle(_activeTab == "Inventory", "Inventory", GUI.skin.button)) _activeTab = "Inventory";
            if (GUILayout.Toggle(_activeTab == "Cheats", "Cheats", GUI.skin.button)) _activeTab = "Cheats";
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Unlock Gallery (F7)", GUILayout.Width(170))) GameApi.UnlockAllGallery();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (_activeTab == "Overview") DrawOverview();
            else if (_activeTab == "Player") DrawPlayerUI();
            else if (_activeTab == "Party") DrawPartyUI();
            else if (_activeTab == "Farm") DrawFarmUI();
            else if (_activeTab == "Inventory") DrawInventoryUI();
            else if (_activeTab == "Cheats") DrawCheats();
            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0, 0, 10000, 25));
        }

        private void DrawLogOverlay()
        {
            const int width = 360;
            const int height = 220;
            var rect = new Rect(Screen.width - width - 10, 10, width, height);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Log", GUILayout.Width(40));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_logCollapsed ? "Expand" : "Collapse", GUILayout.Width(70))) _logCollapsed = !_logCollapsed;
            GUILayout.EndHorizontal();
            if (!_logCollapsed)
            {
                _logScroll = GUILayout.BeginScrollView(_logScroll);
                var lines = LogBuffer.Snapshot();
                for (int i = 0; i < lines.Length; i++)
                {
                    GUILayout.Label(lines[i]);
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }

        private void Rescan()
        {
            _rootsAll = ReflectionUtil.CollectGameRoots();
            var byType = new Dictionary<Type, object>();
            for (int i = 0; i < _rootsAll.Count; i++)
            {
                var o = _rootsAll[i];
                if (o == null) continue;
                var t = o.GetType();
                if (!byType.ContainsKey(t)) byType[t] = o;
            }
            _roots = new List<object>(byType.Values);
            Plugin.Log.LogInfo("CreativeMode rescan: " + _roots.Count + " roots found");
            LogBuffer.Add("Rescanned scene objects");
        }

        // ===== Shared helpers (used across partial tabs) =====
        // Inline stat editor storage (shared for Player/Farm)
        private Dictionary<string, string> _statEdits = new Dictionary<string, string>();
        
        private void StatRow(string label, object statsObj, string statKey)
        {
            GUILayout.BeginHorizontal();
            object cur = ReadStat(statsObj, new string[] { statKey });
            GUILayout.Label(label + ": " + (cur != null ? cur.ToString() : "n/a"), GUILayout.Width(200));
            string key = label;
            string edit;
            if (!_statEdits.TryGetValue(key, out edit)) edit = "";
            edit = GUILayout.TextField(edit, GUILayout.Width(80));
            _statEdits[key] = edit;
            if (GUILayout.Button("Set", GUILayout.Width(50)))
            {
                int iv; float fv;
                if (int.TryParse(edit, out iv)) TrySetStat(statsObj, statKey, iv);
                else if (float.TryParse(edit, out fv)) TrySetStat(statsObj, statKey, fv);
            }
            GUILayout.EndHorizontal();
        }

        private object ReadStat(object target, string[] keys)
        {
            try
            {
                var t = target.GetType();
                for (int i = 0; i < keys.Length; i++)
                {
                    var k = keys[i];
                    var prop = t.GetProperty(k, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (prop != null) return prop.GetValue(target, null);
                    var field = t.GetField(k, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (field != null) return field.GetValue(target);
                    var v = InvokeGetStatValue(target, k);
                    if (v != null) return v;
                }
            }
            catch { }
            return null;
        }

        private object InvokeGetStatValue(object target, string key)
        {
            try
            {
                var t = target.GetType();
                var m = t.GetMethod("GetStatValue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (m != null && m.GetParameters().Length == 1)
                {
                    var p = m.GetParameters()[0];
                    if (p.ParameterType == typeof(string)) return m.Invoke(target, new object[] { (object)key });
                    var ev = ResolveEnumValue("TeamNimbus.CloudMeadow.Monsters.StatModifiers", key) ?? ResolveEnumValue("StatModifiers", key);
                    if (ev != null && p.ParameterType.IsEnum && ev.GetType() == p.ParameterType)
                    {
                        return m.Invoke(target, new object[] { ev });
                    }
                }
            }
            catch { }
            return null;
        }

        private object ResolveEnumValue(string typeName, string valueName)
        {
            try
            {
                var asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < asms.Length; a++)
                {
                    var t = asms[a].GetType(typeName, false, true);
                    if (t != null && t.IsEnum)
                    {
                        try { return Enum.Parse(t, valueName, true); } catch { }
                    }
                }
            }
            catch { }
            return null;
        }

        private int ReadInt(object target, string[] keys)
        {
            object v = ReadStat(target, keys);
            try { if (v == null) return 0; return Convert.ToInt32(v); } catch { return 0; }
        }
        private string ReadString(object target, string[] keys)
        {
            object v = ReadStat(target, keys);
            return v != null ? v.ToString() : "-";
        }

        private void TrySetStat(object target, string statKey, object value)
        {
            try
            {
                var t = target.GetType();
                var prop = t.GetProperty(statKey, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite) { prop.SetValue(target, value, null); return; }
                var field = t.GetField(statKey, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (field != null) { field.SetValue(target, value); return; }
                var m2 = t.GetMethod("SetStatValue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (m2 != null && m2.GetParameters().Length == 2)
                {
                    var p1 = m2.GetParameters()[0]; var p2 = m2.GetParameters()[1];
                    object val = value;
                    if (p2.ParameterType == typeof(int)) { try { val = Convert.ToInt32(value); } catch { } }
                    if (p2.ParameterType == typeof(float)) { try { val = Convert.ToSingle(value); } catch { } }
                    if (p1.ParameterType == typeof(string)) { m2.Invoke(target, new object[] { (object)statKey, val }); return; }
                    var ev = ResolveEnumValue("TeamNimbus.CloudMeadow.Monsters.StatModifiers", statKey) ?? ResolveEnumValue("StatModifiers", statKey);
                    if (ev != null && p1.ParameterType.IsEnum && ev.GetType() == p1.ParameterType)
                    { m2.Invoke(target, new object[] { ev, val }); return; }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("TrySetStat failed: " + e.Message); }
        }

        private void TrySetMember(object target, string memberName, object value)
        {
            try
            {
                var t = target.GetType();
                var prop = t.GetProperty(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(target, value, null);
                    return;
                }
                var field = t.GetField(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("TrySetMember failed: " + e.Message); }
        }

        private void TrySetEnum(object target, string memberName, string enumName)
        {
            try
            {
                var t = target.GetType();
                var prop = t.GetProperty(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var et = prop.PropertyType;
                    var val = Enum.Parse(et, enumName, true);
                    prop.SetValue(target, val, null);
                    return;
                }
                var field = t.GetField(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var et = field.FieldType;
                    var val = Enum.Parse(et, enumName, true);
                    field.SetValue(target, val);
                    return;
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("TrySetEnum failed: " + e.Message); }
        }
    }
}
