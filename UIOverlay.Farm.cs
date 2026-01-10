using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal partial class UIOverlay
    {
        private string _timeHH = "07";
        private string _timeMM = "00";
        private System.Collections.Generic.Dictionary<object, bool> _monsterTypeWindow = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, bool> _monsterTraitsWindow = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, bool> _monsterAddTraitWindow = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, string> _monsterSelectedSpecies = new System.Collections.Generic.Dictionary<object, string>();
        private System.Collections.Generic.Dictionary<object, string> _traitLevelEdits = new System.Collections.Generic.Dictionary<object, string>();
        private System.Collections.Generic.Dictionary<object, string> _addTraitFilter = new System.Collections.Generic.Dictionary<object, string>();
        private System.Collections.Generic.Dictionary<object, Rect> _addTraitPopupRect = new System.Collections.Generic.Dictionary<object, Rect>();
        private System.Collections.Generic.Dictionary<object, Vector2> _addTraitScroll = new System.Collections.Generic.Dictionary<object, Vector2>();
        private object[] _allTraitDefsCache;

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
                    int hh = 7, mm = 0; int.TryParse(_timeHH, out hh); int.TryParse(_timeMM, out mm);
                    GameApi.SetTime(hh, mm);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.Label("Monsters");
                var mons = GameApi.GetActiveMonsters();
                for (int i = 0; i < mons.Length; i++)
                {
                    var m = mons[i]; if (m == null) continue;
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label((i + 1) + ". " + m.Name, GUILayout.Width(200));
                    var species = GameApi.GetMonsterSpecies(m);
                    GUILayout.Label("Type: " + species, GUILayout.Width(160));
                    if (GUILayout.Button("Change", GUILayout.Width(70))) { _monsterTypeWindow[m] = true; _monsterSelectedSpecies[m] = species; }
                    GUILayout.Label("Gender:", GUILayout.Width(60));
                    if (GUILayout.Button("Male", GUILayout.Width(60))) GameApi.SetMonsterGender(m, "Male");
                    if (GUILayout.Button("Female", GUILayout.Width(60))) GameApi.SetMonsterGender(m, "Female");
                    if (GUILayout.Button("Swap", GUILayout.Width(60))) GameApi.SwapMonsterGender(m);
                    if (GUILayout.Button("Delete", GUILayout.Width(70))) { GameApi.RemoveMonster(m); GUILayout.EndHorizontal(); GUILayout.EndVertical(); continue; }
                    if (GUILayout.Button("Traits", GUILayout.Width(70))) { _monsterTraitsWindow[m] = true; }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginVertical(GUI.skin.box);
                    StatRow("Physique", m, "Physique");
                    StatRow("Stamina", m, "Stamina");
                    StatRow("Intuition", m, "Intuition");
                    StatRow("Swiftness", m, "Swiftness");
                    GUILayout.EndVertical();

                    if (_monsterTypeWindow.ContainsKey(m) && _monsterTypeWindow[m]) DrawMonsterTypeWindow(m);
                    if (_monsterTraitsWindow.ContainsKey(m) && _monsterTraitsWindow[m]) DrawMonsterTraitsWindow(m);

                    GUILayout.EndVertical();
                }
            }
            catch (System.Exception e)
            {
                GUILayout.Label("Farm UI error: " + e.Message);
            }
        }

        private void DrawMonsterTypeWindow(object monster)
        {
            GUILayout.BeginVertical(GUI.skin.window);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Change Type");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(60))) { _monsterTypeWindow[monster] = false; GUILayout.EndHorizontal(); GUILayout.EndVertical(); return; }
            GUILayout.EndHorizontal();

            var speciesArr = GameApi.GetAllSpecies();
            string cur = _monsterSelectedSpecies.ContainsKey(monster) ? _monsterSelectedSpecies[monster] : GameApi.GetMonsterSpecies(monster);
            for (int i = 0; i < speciesArr.Length; i++)
            {
                var s = speciesArr.GetValue(i).ToString();
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label((s == cur ? "* " : "") + s, GUILayout.Width(220));
                if (GUILayout.Button("Select", GUILayout.Width(60))) { GameApi.SetMonsterSpecies(monster, s); _monsterTypeWindow[monster] = false; }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawMonsterTraitsWindow(object monster)
        {
            GUILayout.BeginVertical(GUI.skin.window);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Traits");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Trait", GUILayout.Width(80))) { _monsterAddTraitWindow[monster] = true; }
            if (GUILayout.Button("Close", GUILayout.Width(60))) { _monsterTraitsWindow[monster] = false; GUILayout.EndHorizontal(); GUILayout.EndVertical(); return; }
            GUILayout.EndHorizontal();

            var traits = GameApi.GetMonsterTraits(monster);
            for (int i = 0; i < traits.Length; i++)
            {
                var tr = traits[i];
                string name = ReadString(tr, new string[] { "Name", "TraitName", "TraitCode", "Code" });
                int lvl = ReadInt(tr, new string[] { "Grade", "Level", "CurrentLevel" });
                int max = GameApi.GetTraitMaxGrade(tr);
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label((i + 1) + ". " + name + " Lvl:" + lvl + (max > 0 ? "/" + max : ""), GUILayout.Width(280));
                if (GUILayout.Button("Del", GUILayout.Width(40))) { GameApi.RemoveTraitFromMonster(monster, tr); GUILayout.EndHorizontal(); continue; }
                if (GUILayout.Button("Max", GUILayout.Width(50))) GameApi.MaxTraitGrade(tr);
                GUILayout.Label("(Experimental: Set lvl)", GUILayout.Width(150));
                string edit;
                if (!_traitLevelEdits.TryGetValue(tr, out edit)) edit = lvl.ToString();
                edit = GUILayout.TextField(edit, GUILayout.Width(40));
                _traitLevelEdits[tr] = edit;
                if (GUILayout.Button("Set", GUILayout.Width(40))) { int nv; if (int.TryParse(edit, out nv)) GameApi.SetTraitGrade(tr, nv); }
                GUILayout.EndHorizontal();
            }

            if (_monsterAddTraitWindow.ContainsKey(monster) && _monsterAddTraitWindow[monster]) DrawAddTraitPopup(monster);
            GUILayout.EndVertical();
        }

        private void DrawAddTraitPopup(object monster)
        {
            GUILayout.BeginVertical(GUI.skin.window);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Add Trait");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(60))) { _monsterAddTraitWindow[monster] = false; GUIUtility.ExitGUI(); return; }
            GUILayout.EndHorizontal();

            if (_allTraitDefsCache == null) _allTraitDefsCache = GameApi.GetAllTraitDefinitions();
            _traitLevelEdits[monster] = _traitLevelEdits.ContainsKey(monster) ? _traitLevelEdits[monster] : "1";
            if (!_addTraitScroll.ContainsKey(monster)) _addTraitScroll[monster] = Vector2.zero;
            if (!_addTraitFilter.ContainsKey(monster)) _addTraitFilter[monster] = "";

            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(50));
            string filter = _addTraitFilter[monster];
            filter = GUILayout.TextField(filter, GUILayout.Width(200));
            _addTraitFilter[monster] = filter;
            GUILayout.Label("Grade:", GUILayout.Width(50));
            string gradeEdit = _traitLevelEdits[monster];
            gradeEdit = GUILayout.TextField(gradeEdit, GUILayout.Width(40));
            _traitLevelEdits[monster] = gradeEdit;
            if (GUILayout.Button("Set 1", GUILayout.Width(50))) _traitLevelEdits[monster] = "1";
            if (GUILayout.Button("Set 5", GUILayout.Width(50))) _traitLevelEdits[monster] = "5";
            GUILayout.EndHorizontal();

            _addTraitScroll[monster] = GUILayout.BeginScrollView(_addTraitScroll[monster], GUILayout.Height(480));
            if (_allTraitDefsCache != null && _allTraitDefsCache.Length > 0)
            {
                for (int i = 0; i < _allTraitDefsCache.Length; i++)
                {
                    var d = _allTraitDefsCache[i]; if (d == null) continue;
                    var name = ReadString(d, new string[] { "Name", "DisplayName", "Code" });
                    if (!string.IsNullOrEmpty(filter))
                    {
                        if (name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    }
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label(name, GUILayout.Width(740));
                    if (GUILayout.Button("Add", GUILayout.Width(80)))
                    {
                        int g = 1; int.TryParse(_traitLevelEdits[monster], out g); if (g < 1) g = 1; if (g > 5) g = 5;
                        GameApi.AddTraitToMonster(monster, d, g);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No trait definitions found.");
            }
            GUILayout.EndScrollView();

            GUILayout.Space(5);
            if (GUILayout.Button("Close", GUILayout.Width(100))) { _monsterAddTraitWindow[monster] = false; }
            GUILayout.EndVertical();
        }

        private System.Collections.Generic.List<object> FindTraitsCollection(object monster)
        {
            try
            {
                var t = monster.GetType();
                var list = new System.Collections.Generic.List<object>();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    if (p.Name.IndexOf("trait", System.StringComparison.OrdinalIgnoreCase) >= 0)
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
                    if (f.Name.IndexOf("trait", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        object col = null; try { col = f.GetValue(monster); } catch { }
                        AppendEnumerable(list, col);
                        if (list.Count > 0) return list;
                    }
                }
            }
            catch { }
            return new System.Collections.Generic.List<object>();
        }

        private void AppendEnumerable(System.Collections.Generic.List<object> list, object col)
        {
            if (col == null) return;
            var en = col as System.Collections.IEnumerable;
            if (en == null || col is string) return;
            foreach (var item in en)
            {
                if (item != null) list.Add(item);
            }
        }

    }
}
