using System;
using System.Collections.Generic;
// using System.Linq;
using UnityEngine;
using TeamNimbus.CloudMeadow;

namespace CloudMeadow.CreativeMode
{
    internal class UIOverlay : MonoBehaviour
    {
        private bool _visible;
        private Rect _windowRect = new Rect(50, 50, 900, 600);
        private Vector2 _scroll;
        private Vector2 _logScroll;
        private bool _logCollapsed = false;
        private string _activeTab = "Overview";
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
            if (GUILayout.Toggle(_activeTab == "Party", "Party", GUI.skin.button)) _activeTab = "Party";
            if (GUILayout.Toggle(_activeTab == "Farm", "Farm", GUI.skin.button)) _activeTab = "Farm";
            if (GUILayout.Toggle(_activeTab == "Traits", "Traits", GUI.skin.button)) _activeTab = "Traits";
            if (GUILayout.Toggle(_activeTab == "All Roots", "All Roots", GUI.skin.button)) _activeTab = "All Roots";
            if (GUILayout.Toggle(_activeTab == "Cheats", "Cheats", GUI.skin.button)) _activeTab = "Cheats";
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Rescan (F8)", GUILayout.Width(120))) Rescan();
            if (GUILayout.Button("Unlock Gallery (F7)", GUILayout.Width(170))) GameApi.UnlockAllGallery();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter contains:", GUILayout.Width(110));
            _filter = GUILayout.TextField(_filter ?? string.Empty);
            GUILayout.Label("Max depth:", GUILayout.Width(80));
            int.TryParse(GUILayout.TextField(_maxDepth.ToString(), GUILayout.Width(40)), out _maxDepth);
            _maxDepth = Mathf.Clamp(_maxDepth, 1, 6);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            _scroll = GUILayout.BeginScrollView(_scroll);
            switch (_activeTab)
            {
                case "Overview": DrawOverview(); break;
                case "Player": DrawPlayerUI(); break;
                case "Party": DrawPartyUI(); break;
                case "Farm": DrawFarmUI(); break;
                case "Traits": DrawTraitsUI(); break;
                case "All Roots": DrawAllRoots(); break;
                case "Cheats": DrawCheats(); break;
            }
            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0, 0, 10000, 25));
        }

        private void DrawOverview()
        {
            GUILayout.Label("Detected roots: " + _roots.Count);
            if (GameApi.Ready)
            {
                GUILayout.Label(GameApi.BuildQuickStatus());
            }
            for (int i = 0; i < _roots.Count; i++)
            {
                var r = _roots[i];
                if (!PassesFilter(r)) continue;
                GUILayout.Label("- " + r.GetType().FullName);
            }
        }

        private bool PassesFilter(object obj)
        {
            if (string.IsNullOrEmpty(_filter)) return true;
            var name = (obj != null && obj.GetType() != null && obj.GetType().FullName != null) ? obj.GetType().FullName : "";
            return name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ==== Structured UIs ====
        private string _editName = null;
        private string _editLevel = null;
        private Dictionary<string, string> _editStats = new Dictionary<string, string>();

        private void DrawPlayerUI()
        {
            try
            {
                var s = TeamNimbus.CloudMeadow.Managers.GameManager.Status;
                var p = s.ProtagonistStats;
                GUILayout.Label("Protagonist");
                GUILayout.BeginVertical(GUI.skin.box);
                // Name
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name:", GUILayout.Width(60));
                if (_editName == null) _editName = p.Name;
                _editName = GUILayout.TextField(_editName, GUILayout.Width(180));
                if (GUILayout.Button("Set", GUILayout.Width(50))) TrySetMember(p, "Name", _editName);
                GUILayout.EndHorizontal();
                // Level
                GUILayout.BeginHorizontal();
                GUILayout.Label("Level:", GUILayout.Width(60));
                if (_editLevel == null) _editLevel = p.Level.ToString();
                _editLevel = GUILayout.TextField(_editLevel, GUILayout.Width(80));
                if (GUILayout.Button("Set", GUILayout.Width(50)))
                {
                    int lvl;
                    if (int.TryParse(_editLevel, out lvl)) TrySetMember(p, "Level", lvl);
                }
                GUILayout.Label(p.IsMaxLevel ? "(Max)" : "", GUILayout.Width(60));
                GUILayout.EndHorizontal();
                // Gender
                GUILayout.BeginHorizontal();
                GUILayout.Label("Gender:", GUILayout.Width(60));
                if (GUILayout.Button("Male", GUILayout.Width(60))) TrySetEnum(p, "Gender", "Male");
                if (GUILayout.Button("Female", GUILayout.Width(60))) TrySetEnum(p, "Gender", "Female");
                GUILayout.Label("Current: " + p.Gender, GUILayout.Width(120));
                GUILayout.EndHorizontal();
                // LVL/HP/XP line (best effort via reflection)
                GUILayout.BeginHorizontal();
                GUILayout.Label("LVL " + p.Level, GUILayout.Width(70));
                var hpCur = ReadStat(p, new string[] { "HPCurrent", "CurrentHP", "HP" });
                var hpMax = ReadStat(p, new string[] { "HPMax", "MaxHP", "MaxHealth" });
                GUILayout.Label("HP: " + (hpCur != null ? hpCur.ToString() : "-") + "/" + (hpMax != null ? hpMax.ToString() : "-"), GUILayout.Width(180));
                var xp = ReadStat(p, new string[] { "CurrentXP", "XP", "Experience" });
                GUILayout.Label("XP: " + (xp != null ? xp.ToString() : "-"), GUILayout.Width(120));
                GUILayout.Label("XP Next: " + p.XPNeededForNextLevel, GUILayout.Width(120));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.Space(5);
                GUILayout.Label("Primary Stats");
                GUILayout.BeginVertical(GUI.skin.box);
                // Primary group per требованию: Physique, Stamina, Intuition, Swiftness
                StatRow("Physique", p, "Physique");
                StatRow("Stamina", p, "Stamina");
                StatRow("Intuition", p, "Intuition");
                StatRow("Swiftness", p, "Swiftness");
                GUILayout.EndVertical();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label("Korona: " + s.KoronaBalance, GUILayout.Width(160));
                if (GUILayout.Button("+1000", GUILayout.Width(60))) GameApi.AddKorona(1000);
                if (GUILayout.Button("+100000", GUILayout.Width(80))) GameApi.AddKorona(100000);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label("Shards: " + s.NumUpgradeShards, GUILayout.Width(160));
                if (GUILayout.Button("+10", GUILayout.Width(60))) GameApi.AddShards(10);
                if (GUILayout.Button("+100", GUILayout.Width(60))) GameApi.AddShards(100);
                GUILayout.EndHorizontal();
            }
            catch (Exception e)
            {
                GUILayout.Label("Player UI error: " + e.Message);
            }
        }

        private void DrawPartyUI()
        {
            try
            {
                GUILayout.Label("Party Members");
                GUILayout.BeginVertical(GUI.skin.box);
                int shown = 0;
                for (int i = 0; i < _roots.Count; i++)
                {
                    var obj = _roots[i]; if (obj == null) continue;
                    var tn = obj.GetType().FullName;
                    if (tn != null && tn.IndexOf("Party", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        GUILayout.Label("== " + tn + " ==");
                        int budget = 80;
                        ReflectionUtil.DumpObject(obj, line => GUILayout.Label(line), 2, budget);
                        GUILayout.Space(5);
                        shown++;
                        if (shown > 5) break;
                    }
                }
                GUILayout.EndVertical();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Button("Level party to 20")) GameApi.LevelCompanions(20);
                if (GUILayout.Button("Recruit all L15")) GameApi.RecruitAllCompanions(15);
                GUILayout.EndHorizontal();
            }
            catch (Exception e)
            {
                GUILayout.Label("Party UI error: " + e.Message);
            }
        }

        private string _timeHH = "07";
        private string _timeMM = "00";
        private System.Collections.Generic.Dictionary<object, bool> _monsterTraitsOpen = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, string> _traitLevelEdits = new System.Collections.Generic.Dictionary<object, string>();

        // Helpers for reading values
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

        private void DrawMonsterTraits(object monster)
        {
            try
            {
                GUILayout.Label("Traits:");
                var traits = FindTraitsCollection(monster);
                if (traits == null || traits.Count == 0)
                {
                    GUILayout.Label("(no traits found)");
                    return;
                }
                for (int i = 0; i < traits.Count; i++)
                {
                    var tr = traits[i]; if (tr == null) continue;
                    string name = ReadString(tr, new string[] { "Name", "TraitName", "Id", "Code" });
                    int lvl = ReadInt(tr, new string[] { "Level", "CurrentLevel" });
                    int max = ReadInt(tr, new string[] { "MaxLevel", "Cap", "Max" });
                    GUILayout.BeginHorizontal();
                    GUILayout.Label((i+1) + ". " + name, GUILayout.Width(240));
                    // Enabled toggle best-effort
                    bool enabled = ReadInt(tr, new string[] { "Enabled", "IsEnabled", "Active" }) != 0;
                    bool newEnabled = GUILayout.Toggle(enabled, "Enabled", GUILayout.Width(80));
                    if (newEnabled != enabled)
                    {
                        TrySetMember(tr, enabled ? "Enabled" : "IsEnabled", newEnabled);
                        TrySetMember(tr, "Active", newEnabled);
                    }
                    // Level editor
                    string edit;
                    if (!_traitLevelEdits.TryGetValue(tr, out edit)) edit = lvl.ToString();
                    edit = GUILayout.TextField(edit, GUILayout.Width(40));
                    _traitLevelEdits[tr] = edit;
                    if (GUILayout.Button("Set", GUILayout.Width(40)))
                    {
                        int nv; if (int.TryParse(edit, out nv)) TrySetMember(tr, "Level", nv);
                    }
                    if (max > 0 && lvl < max)
                    {
                        if (GUILayout.Button("Max", GUILayout.Width(50))) TrySetMember(tr, "Level", max);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            catch (Exception e)
            {
                GUILayout.Label("Traits error: " + e.Message);
            }
        }

        private List<object> FindTraitsCollection(object monster)
        {
            try
            {
                var t = monster.GetType();
                // Look for properties/fields containing "trait" that are IEnumerable
                var list = new List<object>();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        object col = null; try { col = p.GetValue(monster, null); } catch { }
                        AppendEnumerable(list, col);
                        if (list.Count > 0) return list;
                    }
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        object col = null; try { col = f.GetValue(monster); } catch { }
                        AppendEnumerable(list, col);
                        if (list.Count > 0) return list;
                    }
                }
            }
            catch { }
            return new List<object>();
        }

        private void AppendEnumerable(List<object> list, object col)
        {
            if (col == null) return;
            var en = col as System.Collections.IEnumerable;
            if (en == null || col is string) return;
            foreach (var item in en)
            {
                if (item != null) list.Add(item);
            }
        }

        private void DrawFarmUI()
        {
            try
            {
                var s = TeamNimbus.CloudMeadow.Managers.GameManager.Status;
                GUILayout.Label("Farm Overview");
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Monsters on farm: " + s.NumMonstersOnTheFarm + "/" + s.FarmStatus.ResolveNumberOfMonsterSpotsOnFarm());
                GUILayout.EndVertical();
                GUILayout.Space(5);

                // Time presets
                GUILayout.Label("Time presets");
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Button("07:00", GUILayout.Width(60))) GameApi.SetTime(7, 0);
                if (GUILayout.Button("14:00", GUILayout.Width(60))) GameApi.SetTime(14, 0);
                if (GUILayout.Button("20:00", GUILayout.Width(60))) GameApi.SetTime(20, 0);
                GUILayout.Label("Set:", GUILayout.Width(30));
                _timeHH = GUILayout.TextField(_timeHH, GUILayout.Width(30));
                GUILayout.Label(":", GUILayout.Width(8));
                _timeMM = GUILayout.TextField(_timeMM, GUILayout.Width(30));
                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                {
                    int hh=7, mm=0; int.TryParse(_timeHH, out hh); int.TryParse(_timeMM, out mm);
                    GameApi.SetTime(hh, mm);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Button("Clear Barn")) GameApi.ClearBarn();
                if (GUILayout.Button("Give every monster")) GameApi.GiveEveryMonster();
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.Label("Season & Weather");
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Button("Spring")) GameApi.SetSeason(Season.Spring);
                if (GUILayout.Button("Summer")) GameApi.SetSeason(Season.Summer);
                if (GUILayout.Button("Autumn")) GameApi.SetSeason(Season.Autumn);
                if (GUILayout.Button("Winter")) GameApi.SetSeason(Season.Winter);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Button("Clear")) GameApi.SetWeather(Weather.Clear);
                if (GUILayout.Button("Rain")) GameApi.SetWeather(Weather.Rain);
                if (GUILayout.Button("Storm")) GameApi.SetWeather(Weather.Storm);
                if (GUILayout.Button("Snow")) GameApi.SetWeather(Weather.Snow);
                GUILayout.EndHorizontal();

                // Monsters list
                GUILayout.Space(5);
                GUILayout.Label("Monsters");
                var mons = GameApi.GetActiveMonsters();
                for (int i = 0; i < mons.Length; i++)
                {
                    var m = mons[i]; if (m == null) continue;
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label((i+1) + ". " + m.Name, GUILayout.Width(200));
                    GUILayout.Label("Gender: " + ReadEnum(m, "Gender"), GUILayout.Width(120));
                    if (GUILayout.Button("Delete", GUILayout.Width(70))) { GameApi.RemoveMonster(m); continue; }
                    bool open = false; _monsterTraitsOpen.TryGetValue(m, out open);
                    if (GUILayout.Button(open ? "Hide Traits" : "Traits", GUILayout.Width(90))) { _monsterTraitsOpen[m] = !open; open = !open; }
                    GUILayout.EndHorizontal();
                    // Show monster primary stats
                    GUILayout.BeginVertical(GUI.skin.box);
                    StatRow("Physique", m, "Physique");
                    StatRow("Stamina", m, "Stamina");
                    StatRow("Intuition", m, "Intuition");
                    StatRow("Swiftness", m, "Swiftness");
                    GUILayout.EndVertical();
                    if (open) DrawMonsterTraits(m);
                    GUILayout.EndVertical();
                }
            }
            catch (Exception e)
            {
                GUILayout.Label("Farm UI error: " + e.Message);
            }
        }

        private void DrawTraitsUI()
        {
            try
            {
                GUILayout.Label("Traits & Status");
                GUILayout.BeginVertical(GUI.skin.box);
                int shown = 0;
                for (int i = 0; i < _roots.Count; i++)
                {
                    var obj = _roots[i]; if (obj == null) continue;
                    var n = obj.GetType().FullName;
                    if (n != null && (n.IndexOf("Trait", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        GUILayout.Label("== " + n + " ==");
                        int budget = 80;
                        ReflectionUtil.DumpObject(obj, line => GUILayout.Label(line), 2, budget);
                        GUILayout.Space(4);
                        shown++;
                        if (shown > 6) break;
                    }
                }
                GUILayout.EndVertical();
            }
            catch (Exception e)
            {
                GUILayout.Label("Traits UI error: " + e.Message);
            }
        }

        private void TryStat(string label, object statsObj, string statKey)
        {
            try
            {
                var t = statsObj.GetType();
                var prop = t.GetProperty(statKey, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var val = prop.GetValue(statsObj, null);
                    GUILayout.Label(label + ": " + (val != null ? val.ToString() : "-"));
                    return;
                }
                var field = t.GetField(statKey, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var val = field.GetValue(statsObj);
                    GUILayout.Label(label + ": " + (val != null ? val.ToString() : "-"));
                    return;
                }
                var m = t.GetMethod("GetStatValue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (m != null && m.GetParameters().Length == 1)
                {
                    var v = m.Invoke(statsObj, new object[] { (object)statKey });
                    GUILayout.Label(label + ": " + (v != null ? v.ToString() : "-"));
                    return;
                }
                GUILayout.Label(label + ": (n/a)");
            }
            catch { GUILayout.Label(label + ": (err)"); }
        }

        private void StatRow(string label, object statsObj, string statKey)
        {
            GUILayout.BeginHorizontal();
            TryStat(label, statsObj, statKey);
            string key = label;
            string cur;
            if (!_editStats.TryGetValue(key, out cur)) cur = "";
            cur = GUILayout.TextField(cur, GUILayout.Width(80));
            _editStats[key] = cur;
            if (GUILayout.Button("Set", GUILayout.Width(50)))
            {
                float fv;
                int iv;
                if (int.TryParse(cur, out iv)) TrySetStat(statsObj, statKey, iv);
                else if (float.TryParse(cur, out fv)) TrySetStat(statsObj, statKey, fv);
            }
            GUILayout.EndHorizontal();
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
                    // Try common enum types
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

        private bool InvokeSetStatValue(object target, string key, object value)
        {
            try
            {
                var t = target.GetType();
                var m = t.GetMethod("SetStatValue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (m != null && m.GetParameters().Length == 2)
                {
                    var p1 = m.GetParameters()[0]; var p2 = m.GetParameters()[1];
                    object val = value;
                    if (p2.ParameterType == typeof(int)) { try { val = Convert.ToInt32(value); } catch { } }
                    if (p2.ParameterType == typeof(float)) { try { val = Convert.ToSingle(value); } catch { } }
                    if (p1.ParameterType == typeof(string)) { m.Invoke(target, new object[] { (object)key, val }); return true; }
                    var ev = ResolveEnumValue("TeamNimbus.CloudMeadow.Monsters.StatModifiers", key) ?? ResolveEnumValue("StatModifiers", key);
                    if (ev != null && p1.ParameterType.IsEnum && ev.GetType() == p1.ParameterType)
                    { m.Invoke(target, new object[] { ev, val }); return true; }
                }
            }
            catch { }
            return false;
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

        private string ReadEnum(object target, string memberName)
        {
            try
            {
                var t = target.GetType();
                var prop = t.GetProperty(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var v = prop.GetValue(target, null); return v != null ? v.ToString() : "-";
                }
                var field = t.GetField(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var v = field.GetValue(target); return v != null ? v.ToString() : "-";
                }
            }
            catch { }
            return "-";
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
                    var val = System.Enum.Parse(et, enumName, true);
                    prop.SetValue(target, val, null);
                    return;
                }
                var field = t.GetField(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (field != null)
                {
                    var et = field.FieldType;
                    var val = System.Enum.Parse(et, enumName, true);
                    field.SetValue(target, val);
                    return;
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("TrySetEnum failed: " + e.Message); }
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
                    m2.Invoke(target, new object[] { (object)statKey, value });
                    return;
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("TrySetStat failed: " + e.Message); }
        }

        private void DrawAllRoots()
        {
            for (int i = 0; i < _rootsAll.Count; i++)
            {
                var obj = _rootsAll[i];
                if (!PassesFilter(obj)) continue;
                GUILayout.Label("== " + obj.GetType().FullName + " ==");
                int budget = 800;
                ReflectionUtil.DumpObject(obj, line => GUILayout.Label(line), _maxDepth, budget);
                GUILayout.Space(10);
            }
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

        private void DrawCheats()
        {
            if (!GameApi.Ready)
            {
                GUILayout.Label("Game not ready or save not loaded.");
                return;
            }
            GUILayout.Label(GameApi.BuildQuickStatus());
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(280));
            GUILayout.Label("Money & Resources");
            if (GUILayout.Button("+1,000 Korona")) GameApi.AddKorona(1000);
            if (GUILayout.Button("+100,000 Korona")) GameApi.AddKorona(100000);
            if (GUILayout.Button("+1,000,000 Korona")) GameApi.AddKorona(1000000);
            if (GUILayout.Button("+100 Upgrade Shards")) GameApi.AddShards(100);
            if (GUILayout.Button("All harvest & groceries")) GameApi.AddHarvestAndGroceries();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250));
            GUILayout.Label("Time & Season");
            if (GUILayout.Button("Toggle God Mode")) GameApi.ToggleGodMode();
            if (GUILayout.Button("Advance to end of day")) GameApi.AdvanceToEndOfDay();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spring")) GameApi.SetSeason(Season.Spring);
            if (GUILayout.Button("Summer")) GameApi.SetSeason(Season.Summer);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Autumn")) GameApi.SetSeason(Season.Autumn);
            if (GUILayout.Button("Winter")) GameApi.SetSeason(Season.Winter);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250));
            GUILayout.Label("Weather");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear")) GameApi.SetWeather(Weather.Clear);
            if (GUILayout.Button("Rain")) GameApi.SetWeather(Weather.Rain);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Storm")) GameApi.SetWeather(Weather.Storm);
            if (GUILayout.Button("Snow")) GameApi.SetWeather(Weather.Snow);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Blazing Heat")) GameApi.SetWeather(Weather.BlazingHeat);
            if (GUILayout.Button("Falling Leaves")) GameApi.SetWeather(Weather.Leafs);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250));
            GUILayout.Label("Monsters & Companions");
            if (GUILayout.Button("Clear Barn")) GameApi.ClearBarn();
            if (GUILayout.Button("Give every monster (auto level)")) GameApi.GiveEveryMonster();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Recruit companions L10")) GameApi.RecruitAllCompanions(10);
            if (GUILayout.Button("L15")) GameApi.RecruitAllCompanions(15);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Level companions to 20")) GameApi.LevelCompanions(20);
            if (GUILayout.Button("Level monsters to 20")) GameApi.LevelMonsters(20);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Level ALL to 30")) GameApi.LevelAll(30);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(200));
            GUILayout.Label("Misc");
            if (GUILayout.Button("Unlock Gallery (All)")) GameApi.UnlockAllGallery();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
    }
}
