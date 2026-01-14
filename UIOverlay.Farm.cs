using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal partial class UIOverlay
    {
        private string _timeHH = "07";
        private string _timeMM = "00";
        private string _timeDay = "1";
        private string _timeYear = "1";
        private System.Collections.Generic.Dictionary<object, bool> _monsterTypeWindow = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, bool> _monsterStatsFoldout = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, bool> _monsterTraitsWindow = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, bool> _monsterAddTraitWindow = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, string> _monsterSelectedSpecies = new System.Collections.Generic.Dictionary<object, string>();
        private System.Collections.Generic.Dictionary<object, string> _traitLevelEdits = new System.Collections.Generic.Dictionary<object, string>();
        private System.Collections.Generic.Dictionary<object, bool> _monsterPatternWindow = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, string> _addTraitFilter = new System.Collections.Generic.Dictionary<object, string>();
        private System.Collections.Generic.Dictionary<object, Rect> _addTraitPopupRect = new System.Collections.Generic.Dictionary<object, Rect>();
        private System.Collections.Generic.Dictionary<object, Vector2> _addTraitScroll = new System.Collections.Generic.Dictionary<object, Vector2>();
        private System.Collections.Generic.Dictionary<object, bool> _addTraitDropdownOpen = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, int> _addTraitSelectedIndex = new System.Collections.Generic.Dictionary<object, int>();
        private object[] _allTraitDefsCache;

        // Performance: cache Add Trait popup data per-monster while popup is open
        private System.Collections.Generic.Dictionary<object, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<object>>> _cacheBloodlineBySpecies = new System.Collections.Generic.Dictionary<object, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<object>>>();
        private System.Collections.Generic.Dictionary<object, object[]> _cacheUniversalDefs = new System.Collections.Generic.Dictionary<object, object[]>();
        private System.Collections.Generic.Dictionary<object, System.Collections.Generic.HashSet<string>> _cacheOwnedTraitCodes = new System.Collections.Generic.Dictionary<object, System.Collections.Generic.HashSet<string>>();

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
                GUILayout.EndHorizontal();

                // Current date/time display
                var cur = TeamNimbus.CloudMeadow.Managers.GameManager.Status.CurrentDateTime;
                GUILayout.Label(string.Format("Current: Year {0}, {1} Day {2} {3:D2}:{4:D2}", cur.Year, cur.Season, cur.Day, cur.Hour, cur.Minute));
                GUILayout.Label("Set exact Year/Day & Time (time may be unstable; to reset the date pick the current season)");
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label("Year:", GUILayout.Width(40));
                _timeYear = GUILayout.TextField(_timeYear ?? "1", GUILayout.Width(50));
                GUILayout.Space(10);
                GUILayout.Label("Day:", GUILayout.Width(30));
                _timeDay = GUILayout.TextField(_timeDay, GUILayout.Width(40));
                GUILayout.Label("Time:", GUILayout.Width(40));
                _timeHH = GUILayout.TextField(_timeHH, GUILayout.Width(30));
                GUILayout.Label(":", GUILayout.Width(8));
                _timeMM = GUILayout.TextField(_timeMM, GUILayout.Width(30));
                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                {
                    int year = 1, day = 1, hh = 7, mm = 0; int.TryParse(_timeYear, out year); int.TryParse(_timeDay, out day); int.TryParse(_timeHH, out hh); int.TryParse(_timeMM, out mm);
                    GameApi.SetYear(year);
                    GameApi.SetDayAndTime(day, hh, mm);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.Label("Season & Weather");
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Button("Spring")) GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Spring);
                if (GUILayout.Button("Summer")) GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Summer);
                if (GUILayout.Button("Autumn")) GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Autumn);
                if (GUILayout.Button("Winter")) GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Winter);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(GUI.skin.box);
                if (GUILayout.Button("Clear")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Clear);
                if (GUILayout.Button("Rain")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Rain);
                if (GUILayout.Button("Storm")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Storm);
                if (GUILayout.Button("Snow")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Snow);
                if (GUILayout.Button("Blazing Heat")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.BlazingHeat);
                if (GUILayout.Button("Falling Leaves")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Leafs);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.Label("Hint: To quickly hatch eggs or accelerate monster births, click the current Season and add time.");

                GUILayout.Space(5);
                // Debug tools hidden per request

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
                    if (GUILayout.Button("Swap", GUILayout.Width(60))) GameApi.SwapMonsterGender(m);
                    if (GUILayout.Button("Delete", GUILayout.Width(70))) { GameApi.RemoveMonster(m); GUILayout.EndHorizontal(); GUILayout.EndVertical(); continue; }
                    if (GUILayout.Button("Traits", GUILayout.Width(70))) { _monsterTraitsWindow[m] = _monsterTraitsWindow.ContainsKey(m) && _monsterTraitsWindow[m] ? false : true; }
                    // Pigment UI
                    string curPigment = GameApi.GetMonsterPigment(m);
                    GUILayout.Label("Pigment: " + curPigment, GUILayout.Width(180));
                    if (GUILayout.Button("Change", GUILayout.Width(70))) { _monsterPatternWindow[m] = true; }
                    GUILayout.EndHorizontal();

                    if (_monsterPatternWindow.ContainsKey(m) && _monsterPatternWindow[m]) DrawMonsterPatternWindow(m);

                    // Stats foldout per monster
                    if (!_monsterStatsFoldout.ContainsKey(m)) _monsterStatsFoldout[m] = false;
                    GUILayout.BeginHorizontal();
                    bool newFold = GUILayout.Toggle(_monsterStatsFoldout[m], "Stats", GUILayout.Width(60));
                    if (newFold != _monsterStatsFoldout[m]) _monsterStatsFoldout[m] = newFold;
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    if (_monsterStatsFoldout[m])
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        GUILayout.Label("Primary Stats (safe-clamped)", GUI.skin.label);
                        StatRow("Physique", m, "Physique");
                        StatRow("Stamina", m, "Stamina");
                        StatRow("Intuition", m, "Intuition");
                        StatRow("Swiftness", m, "Swiftness");
                        GUILayout.Space(4);
                        GUILayout.Label("Hidden/State", GUI.skin.label);
                    GUILayout.Label("Note: Primary stats are limited by Growth + Max Custom. Edits are clamped for safety.", GUI.skin.box);
                        // Loyalty
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Loyalty: " + ReadInt(m, new string[]{"Loyalty"}), GUILayout.Width(200));
                        if (GUILayout.Button("Set 110", GUILayout.Width(70))) { GameApi.SetMonsterLoyalty(m, 110); }
                        if (GUILayout.Button("Set 50", GUILayout.Width(70))) { GameApi.SetMonsterLoyalty(m, 50); }
                        GUILayout.EndHorizontal();
                        // Toggles
                        GUILayout.BeginHorizontal();
                        bool isDry = System.Convert.ToInt32(ReadStat(m, new string[]{"IsDry"})) != 0;
                        bool newIsDry = GUILayout.Toggle(isDry, "Dry", GUILayout.Width(80));
                        if (newIsDry != isDry) { GameApi.SetMonsterDry(m, newIsDry); }
                        bool infertile = System.Convert.ToInt32(ReadStat(m, new string[]{"IsInfertile"})) != 0;
                        bool newInf = GUILayout.Toggle(infertile, "Infertile", GUILayout.Width(100));
                        if (newInf != infertile) { GameApi.SetMonsterInfertile(m, newInf); }
                        bool isLoyal = System.Convert.ToInt32(ReadStat(m, new string[]{"IsLoyal"})) != 0;
                        bool newIsLoyal = GUILayout.Toggle(isLoyal, "Is Loyal", GUILayout.Width(100));
                        if (newIsLoyal != isLoyal) { GameApi.SetMonsterIsLoyal(m, newIsLoyal); }
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }

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

        private void DrawEggsUI()
        {
            try
            {
                var eggs = GameApi.GetIncubatorEggs();
                if (eggs == null || eggs.Length == 0)
                {
                    GUILayout.Label("No eggs found in incubators.");
                    return;
                }
                // Hatch All hidden per request
                for (int i = 0; i < eggs.Length; i++)
                {
                    var e = eggs[i]; if (e == null) continue;
                    string name = GameApi.GetEggDisplayName(e);
                    string timer = GameApi.GetEggTimerString(e);
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label((i + 1) + ". " + name + "  [" + timer + "]", GUILayout.Width(500));
                    // Individual Hatch hidden per request
                    GUILayout.EndHorizontal();
                }
            }
            catch (System.Exception ex)
            {
                GUILayout.Label("Eggs UI error: " + ex.Message);
            }
        }

        private void DrawMonsterPatternWindow(object monster)
        {
            // Rename to Pigment selection
            GUILayout.BeginVertical(GUI.skin.window);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Change Pigment");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(60))) { _monsterPatternWindow[monster] = false; GUILayout.EndHorizontal(); GUILayout.EndVertical(); return; }
            GUILayout.EndHorizontal();

            var pigments = GameApi.GetAvailablePigments();
            if (pigments != null && pigments.Length > 0)
            {
                for (int i = 0; i < pigments.Length; i++)
                {
                    string name = pigments[i];
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label(name, GUILayout.Width(220));
                    if (GUILayout.Button("Select", GUILayout.Width(60))) { GameApi.SetMonsterPigment(monster, name); _monsterPatternWindow[monster] = false; }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No pigments found.");
            }
            GUILayout.EndVertical();
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
                if (string.Equals(s, "Chimera", System.StringComparison.OrdinalIgnoreCase)) continue; // hide Chimera
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
            // Partition traits by source
            var blood = new System.Collections.Generic.List<object>();
            var uni = new System.Collections.Generic.List<object>();
            for (int i = 0; i < traits.Length; i++)
            {
                var tr = traits[i]; if (tr == null) continue;
                var src = GameApi.GetTraitSourceForUI(tr);
                if (!string.IsNullOrEmpty(src) && src.ToLower().IndexOf("universal") >= 0) uni.Add(tr); else blood.Add(tr);
            }

            GUILayout.Label("Bloodlines", GUI.skin.label);
            for (int i = 0; i < blood.Count; i++)
            {
                var tr = blood[i];
                string name = ReadString(tr, new string[] { "Name", "TraitName", "TraitCode", "Code" });
                int lvl = ReadInt(tr, new string[] { "Grade", "Level", "CurrentLevel" });
                int max = GameApi.GetTraitMaxGrade(tr);
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label((i + 1) + ". " + name + " Lvl:" + lvl + (max > 0 ? "/" + max : ""), GUILayout.Width(320));
                if (GUILayout.Button("Del", GUILayout.Width(40))) { GameApi.RemoveTraitFromMonster(monster, tr); GUILayout.EndHorizontal(); continue; }
                if (GUILayout.Button("Max", GUILayout.Width(50))) GameApi.MaxTraitGrade(tr);
                GUILayout.Label("Set lvl", GUILayout.Width(50));
                string edit;
                if (!_traitLevelEdits.TryGetValue(tr, out edit)) edit = lvl.ToString();
                edit = GUILayout.TextField(edit, GUILayout.Width(40));
                _traitLevelEdits[tr] = edit;
                if (GUILayout.Button("Set", GUILayout.Width(40))) { int nv; if (int.TryParse(edit, out nv)) GameApi.SetTraitGrade(tr, nv); }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label("Universal", GUI.skin.label);
            for (int i = 0; i < uni.Count; i++)
            {
                var tr = uni[i];
                string name = ReadString(tr, new string[] { "Name", "TraitName", "TraitCode", "Code" });
                int lvl = ReadInt(tr, new string[] { "Grade", "Level", "CurrentLevel" });
                int max = GameApi.GetTraitMaxGrade(tr);
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label((i + 1) + ". " + name + " Lvl:" + lvl + (max > 0 ? "/" + max : ""), GUILayout.Width(320));
                if (GUILayout.Button("Del", GUILayout.Width(40))) { GameApi.RemoveTraitFromMonster(monster, tr); GUILayout.EndHorizontal(); continue; }
                if (GUILayout.Button("Max", GUILayout.Width(50))) GameApi.MaxTraitGrade(tr);
                GUILayout.Label("Set lvl", GUILayout.Width(50));
                string edit2;
                if (!_traitLevelEdits.TryGetValue(tr, out edit2)) edit2 = lvl.ToString();
                edit2 = GUILayout.TextField(edit2, GUILayout.Width(40));
                _traitLevelEdits[tr] = edit2;
                if (GUILayout.Button("Set", GUILayout.Width(40))) { int nv; if (int.TryParse(edit2, out nv)) GameApi.SetTraitGrade(tr, nv); }
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

            // Build per-monster trait list once per popup (cache while open)
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

            string speciesName = GameApi.GetMonsterSpecies(monster);
            // Build cache on first draw for this popup/monster
            if (!_cacheBloodlineBySpecies.ContainsKey(monster))
            {
                var bloodlineDefs = GameApi.GetAllBloodlineTraitDefinitionsForAllSpecies();
                _cacheBloodlineBySpecies[monster] = GameApi.GroupBloodlineTraitsBySpecies(bloodlineDefs);
            }
            if (!_cacheUniversalDefs.ContainsKey(monster))
            {
                _cacheUniversalDefs[monster] = GameApi.GetUniversalTraitDefinitions();
            }
            var grouped = _cacheBloodlineBySpecies[monster];
            var universalDefs = _cacheUniversalDefs[monster];

            // int addedCount = 0;

            // Section: Bloodlines (grouped by species)
            GUILayout.Label("Bloodlines", GUI.skin.label);
            if (grouped != null && grouped.Count > 0)
            {
                var speciesKeys = new System.Collections.Generic.List<string>(grouped.Keys);
                speciesKeys.Sort(System.StringComparer.OrdinalIgnoreCase);
                for (int si = 0; si < speciesKeys.Count; si++)
                {
                    string speciesKey = speciesKeys[si];
                    var list = grouped[speciesKey]; if (list == null || list.Count == 0) continue;
                    GUILayout.Label(speciesKey, GUI.skin.box);
                    // sort traits by name inside the group
                    list.Sort((a,b)=>{
                        string an = ReadString(a, new string[]{"Name","DisplayName","Code"}) ?? "";
                        string bn = ReadString(b, new string[]{"Name","DisplayName","Code"}) ?? "";
                        return string.Compare(an,bn,System.StringComparison.OrdinalIgnoreCase);
                    });
                    for (int i = 0; i < list.Count; i++)
                    {
                        var d = list[i]; if (d == null) continue;
                        var name = ReadString(d, new string[] { "Name", "DisplayName", "Code" });
                        if (!string.IsNullOrEmpty(filter) && name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                        bool owned = GameApi.MonsterHasTrait(monster, d);
                        GUILayout.BeginHorizontal(GUI.skin.box);
                        GUILayout.Label(name + (owned ? " (owned)" : ""), GUILayout.Width(740));
                        GUI.enabled = !owned;
                        if (GUILayout.Button("Add", GUILayout.Width(80)))
                        {
                            int g = 1; int.TryParse(_traitLevelEdits[monster], out g); if (g < 1) g = 1; if (g > 5) g = 5;
                            GameApi.TryAddTraitToMonster(monster, d, g);
                            _monsterAddTraitWindow[monster] = false;
                        }
                        GUI.enabled = true;
                        GUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                GUILayout.Label("No bloodline traits available.");
            }

            GUILayout.Space(8);

            // Section: Universal
            GUILayout.Label("Universal", GUI.skin.label);
            if (universalDefs != null && universalDefs.Length > 0)
            {
                for (int i = 0; i < universalDefs.Length; i++)
                {
                    var d = universalDefs[i]; if (d == null) continue;
                    var name = ReadString(d, new string[] { "Name", "DisplayName", "Code" });
                    if (!string.IsNullOrEmpty(filter) && name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                    bool owned = GameApi.MonsterHasTrait(monster, d);
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label(name + (owned ? " (owned)" : ""), GUILayout.Width(740));
                    GUI.enabled = !owned;
                    if (GUILayout.Button("Add", GUILayout.Width(80)))
                    {
                        int g = 1; int.TryParse(_traitLevelEdits[monster], out g); if (g < 1) g = 1; if (g > 5) g = 5;
                        GameApi.TryAddTraitToMonster(monster, d, g);
                        _monsterAddTraitWindow[monster] = false;
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No universal traits found.");
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Report", GUILayout.Width(140)))
            {
                string dir = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx");
                dir = System.IO.Path.Combine(dir, "plugins");
                dir = System.IO.Path.Combine(dir, "CloudMeadowCreativeMode");
                string path = System.IO.Path.Combine(dir, "traits_report.txt");
                GameApi.GenerateFarmTraitsReport(path);
            }
            if (GUILayout.Button("Close", GUILayout.Width(100))) { _monsterAddTraitWindow[monster] = false; }
            GUILayout.EndHorizontal();
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
