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
        private System.Collections.Generic.Dictionary<object, bool> _monsterChimeraWindow = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, string> _traitLevelEdits = new System.Collections.Generic.Dictionary<object, string>();
        private System.Collections.Generic.Dictionary<object, bool> _monsterPatternWindow = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, string> _addTraitFilter = new System.Collections.Generic.Dictionary<object, string>();
        private System.Collections.Generic.Dictionary<object, Rect> _addTraitPopupRect = new System.Collections.Generic.Dictionary<object, Rect>();
        private System.Collections.Generic.Dictionary<object, Vector2> _addTraitScroll = new System.Collections.Generic.Dictionary<object, Vector2>();
        private System.Collections.Generic.Dictionary<object, bool> _addTraitDropdownOpen = new System.Collections.Generic.Dictionary<object, bool>();
        private System.Collections.Generic.Dictionary<object, int> _addTraitSelectedIndex = new System.Collections.Generic.Dictionary<object, int>();
        private System.Collections.Generic.Dictionary<object, string> _addTraitCategoryTab = new System.Collections.Generic.Dictionary<object, string>();
        private System.Collections.Generic.Dictionary<object, string> _addTraitSourceFilter = new System.Collections.Generic.Dictionary<object, string>();
        private object[] _allTraitDefsCache;
        private bool _farmMonstersCacheDirty = true;
        private float _farmMonstersCacheAt = -999f;
        private const float FarmMonstersCacheSeconds = 0.5f;
        private System.Collections.Generic.List<MonsterRowCache> _farmMonstersCache = new System.Collections.Generic.List<MonsterRowCache>();
        private System.Collections.Generic.Dictionary<object, object[]> _monsterTraitsSnapshotCache = new System.Collections.Generic.Dictionary<object, object[]>();
        private System.Collections.Generic.Dictionary<object, float> _monsterTraitsSnapshotAt = new System.Collections.Generic.Dictionary<object, float>();
        private const float MonsterTraitsCacheSeconds = 0.75f;

        private class MonsterRowCache
        {
            public object Monster;
            public string Name;
            public string Species;
            public string Pigment;
        }

        // Performance: cache Add Trait popup data per-monster while popup is open
        private System.Collections.Generic.Dictionary<object, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<object>>> _cacheBloodlineBySpecies = new System.Collections.Generic.Dictionary<object, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<object>>>();
        private System.Collections.Generic.Dictionary<object, object[]> _cacheUniversalDefs = new System.Collections.Generic.Dictionary<object, object[]>();
        private System.Collections.Generic.Dictionary<object, System.Collections.Generic.HashSet<string>> _cacheOwnedTraitCodes = new System.Collections.Generic.Dictionary<object, System.Collections.Generic.HashSet<string>>();
        private System.Collections.Generic.Dictionary<string, object[]> _cacheSpeciesTraitDefsStatic = new System.Collections.Generic.Dictionary<string, object[]>(System.StringComparer.OrdinalIgnoreCase);
        private System.Collections.Generic.Dictionary<string, object[]> _cacheStatLimitTraitDefsStatic = new System.Collections.Generic.Dictionary<string, object[]>(System.StringComparer.OrdinalIgnoreCase);
        private System.Collections.Generic.Dictionary<string, object[]> _cacheBloodlineTraitDefsStatic = new System.Collections.Generic.Dictionary<string, object[]>(System.StringComparer.OrdinalIgnoreCase);
        private object[] _cacheUniversalTraitDefsStatic;
        private object[] _cacheAllSpeciesTraitDefsStatic;
        private object[] _cacheAllStatLimitTraitDefsStatic;
        private object[] _cacheAllBloodlineTraitDefsStatic;

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
                EnsureFarmMonstersCache();
                for (int i = 0; i < _farmMonstersCache.Count; i++)
                {
                    var row = _farmMonstersCache[i];
                    var m = row != null ? row.Monster as TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats : null; if (m == null) continue;
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label((i + 1) + ". " + row.Name, GUILayout.Width(200));
                    GUILayout.Label("Type: " + row.Species, GUILayout.Width(160));
                    if (GUILayout.Button("Change", GUILayout.Width(70))) { _monsterTypeWindow[m] = true; _monsterSelectedSpecies[m] = row.Species; }
                    if (string.Equals(row.Species, "Chimera", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (GUILayout.Button("Variant", GUILayout.Width(70))) { _monsterChimeraWindow[m] = !_monsterChimeraWindow.ContainsKey(m) || !_monsterChimeraWindow[m]; }
                    }
                    GUILayout.Label("Gender:", GUILayout.Width(60));
                    if (GUILayout.Button("Swap", GUILayout.Width(60))) GameApi.SwapMonsterGender(m);
                    if (GUILayout.Button("Delete", GUILayout.Width(70))) { GameApi.RemoveMonster(m); MarkFarmCacheDirty(); GUILayout.EndHorizontal(); GUILayout.EndVertical(); continue; }
                    if (GUILayout.Button("Traits", GUILayout.Width(70))) { _monsterTraitsWindow[m] = _monsterTraitsWindow.ContainsKey(m) && _monsterTraitsWindow[m] ? false : true; if (!_monsterTraitsWindow[m]) InvalidateMonsterTraitPopupCache(m); }
                    // Pigment UI
                    GUILayout.Label("Pigment: " + row.Pigment, GUILayout.Width(180));
                    if (GUILayout.Button("Change", GUILayout.Width(70))) { _monsterPatternWindow[m] = true; }
                    GUILayout.EndHorizontal();

                    if (_monsterPatternWindow.ContainsKey(m) && _monsterPatternWindow[m]) DrawMonsterPatternWindow(m);
                    if (_monsterChimeraWindow.ContainsKey(m) && _monsterChimeraWindow[m]) DrawMonsterChimeraWindow(m);

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
                    if (GUILayout.Button("Select", GUILayout.Width(60))) { GameApi.SetMonsterPigment(monster, name); _monsterPatternWindow[monster] = false; MarkFarmCacheDirty(); }
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
                if (GUILayout.Button("Select", GUILayout.Width(60))) { GameApi.SetMonsterSpecies(monster, s); InvalidateMonsterTraitPopupCache(monster); MarkFarmCacheDirty(); _monsterTypeWindow[monster] = false; }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawMonsterChimeraWindow(object monster)
        {
            GUILayout.BeginVertical(GUI.skin.window);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Chimera Variant");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(60))) { _monsterChimeraWindow[monster] = false; GUILayout.EndHorizontal(); GUILayout.EndVertical(); return; }
            GUILayout.EndHorizontal();

            var defs = GetCachedSpeciesTraitDefs("Chimera");
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                if (d == null) continue;
                string name = ReadString(d, new string[] { "Name", "DisplayName", "Code" });
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(name, GUILayout.Width(220));
                if (GUILayout.Button("Apply", GUILayout.Width(70)))
                {
                    if (GameApi.SetChimeraVariant(monster, name, 1))
                    {
                        InvalidateMonsterTraitPopupCache(monster);
                        MarkFarmCacheDirty();
                        _monsterChimeraWindow[monster] = false;
                    }
                }
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
            if (GUILayout.Button("Add Trait", GUILayout.Width(80))) { _monsterAddTraitWindow[monster] = true; RefreshMonsterTraitPopupCache(monster); }
            if (GUILayout.Button("Close", GUILayout.Width(60))) { _monsterTraitsWindow[monster] = false; InvalidateMonsterTraitPopupCache(monster); GUILayout.EndHorizontal(); GUILayout.EndVertical(); return; }
            GUILayout.EndHorizontal();

            var traits = GetCachedMonsterTraits(monster);
            var species = new System.Collections.Generic.List<object>();
            var statLimits = new System.Collections.Generic.List<object>();
            var blood = new System.Collections.Generic.List<object>();
            var uni = new System.Collections.Generic.List<object>();
            for (int i = 0; i < traits.Length; i++)
            {
                var tr = traits[i]; if (tr == null) continue;
                var bucket = GameApi.GetTraitBucketForMonster(monster, tr);
                if (string.Equals(bucket, "Species", System.StringComparison.OrdinalIgnoreCase)) species.Add(tr);
                else if (string.Equals(bucket, "StatLimit", System.StringComparison.OrdinalIgnoreCase)) statLimits.Add(tr);
                else if (string.Equals(bucket, "Universal", System.StringComparison.OrdinalIgnoreCase)) uni.Add(tr);
                else blood.Add(tr);
            }

            DrawTraitSection(monster, "Species", species, true);
            GUILayout.Space(6);
            DrawTraitSection(monster, "Stat Limits", statLimits, true);
            GUILayout.Space(6);
            DrawTraitSection(monster, "Bloodlines", blood, true);
            GUILayout.Space(6);
            DrawTraitSection(monster, "Universal", uni, true);

            if (_monsterAddTraitWindow.ContainsKey(monster) && _monsterAddTraitWindow[monster]) DrawAddTraitPopup(monster);
            GUILayout.EndVertical();
        }

        private void DrawTraitSection(object monster, string title, System.Collections.Generic.List<object> items, bool allowDelete)
        {
            GUILayout.Label(title, GUI.skin.label);
            if (items == null || items.Count == 0)
            {
                GUILayout.Label("—", GUI.skin.box);
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var tr = items[i];
                string name = ReadString(tr, new string[] { "Name", "DisplayName", "TraitName", "TraitCode", "Code" });
                int lvl = ReadInt(tr, new string[] { "Grade", "Level", "CurrentLevel" });
                int max = GameApi.GetTraitMaxGrade(tr);
                string badge = GameApi.GetTraitBadgeForUI(tr);
                string displayName = (i + 1) + ". " + name;
                if (!string.IsNullOrEmpty(badge)) displayName += " [" + badge + "]";
                displayName += " Lvl:" + lvl + (max > 0 ? "/" + max : "");
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(displayName, GUILayout.Width(400));
                if (allowDelete)
                {
                    if (GUILayout.Button("Del", GUILayout.Width(40))) { GameApi.RemoveTraitFromMonster(monster, tr); InvalidateMonsterTraitPopupCache(monster); MarkFarmCacheDirty(); GUILayout.EndHorizontal(); continue; }
                }
                else
                {
                    GUILayout.Label("", GUILayout.Width(40));
                }
                if (GUILayout.Button("Max", GUILayout.Width(50))) { GameApi.MaxTraitGrade(tr); InvalidateMonsterTraitPopupCache(monster); MarkFarmCacheDirty(); }
                GUILayout.Label("Set lvl", GUILayout.Width(50));
                string edit;
                if (!_traitLevelEdits.TryGetValue(tr, out edit)) edit = lvl.ToString();
                edit = GUILayout.TextField(edit, GUILayout.Width(40));
                _traitLevelEdits[tr] = edit;
                if (GUILayout.Button("Set", GUILayout.Width(40))) { int nv; if (int.TryParse(edit, out nv)) { GameApi.SetTraitGrade(tr, nv); InvalidateMonsterTraitPopupCache(monster); MarkFarmCacheDirty(); } }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawAddTraitPopup(object monster)
        {
            GUILayout.BeginVertical(GUI.skin.window);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Add Trait");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(60))) { _monsterAddTraitWindow[monster] = false; InvalidateMonsterTraitPopupCache(monster); GUIUtility.ExitGUI(); return; }
            GUILayout.EndHorizontal();

            // Build per-monster trait list once per popup (cache while open)
            _traitLevelEdits[monster] = _traitLevelEdits.ContainsKey(monster) ? _traitLevelEdits[monster] : "1";
            if (!_addTraitScroll.ContainsKey(monster)) _addTraitScroll[monster] = Vector2.zero;
            if (!_addTraitFilter.ContainsKey(monster)) _addTraitFilter[monster] = "";
            if (!_addTraitCategoryTab.ContainsKey(monster)) _addTraitCategoryTab[monster] = "All";
            if (!_addTraitSourceFilter.ContainsKey(monster)) _addTraitSourceFilter[monster] = "All";

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

            DrawAddTraitCategoryBar(monster);

            _addTraitScroll[monster] = GUILayout.BeginScrollView(_addTraitScroll[monster], GUILayout.Height(480));

            var speciesDefs = GetCachedAllSpeciesTraitDefs();
            var statLimitDefs = GetCachedAllStatLimitTraitDefs();
            var bloodlineDefs = GetCachedAllBloodlineTraitDefs();
            var universalDefs = GetCachedUniversalTraitDefs();
            var ownedCodes = GetOwnedTraitCodeCache(monster);
            string selectedTab = _addTraitCategoryTab[monster];
            string sourceFilter = _addTraitSourceFilter[monster];

            if (string.Equals(selectedTab, "All", System.StringComparison.OrdinalIgnoreCase) || string.Equals(selectedTab, "Species", System.StringComparison.OrdinalIgnoreCase))
            {
                DrawAddableTraitSection(monster, "Species", speciesDefs, filter, sourceFilter, true, ownedCodes);
                GUILayout.Space(8);
            }
            if (string.Equals(selectedTab, "All", System.StringComparison.OrdinalIgnoreCase) || string.Equals(selectedTab, "Stat Limits", System.StringComparison.OrdinalIgnoreCase))
            {
                DrawAddableTraitSection(monster, "Stat Limits", statLimitDefs, filter, sourceFilter, true, ownedCodes);
                GUILayout.Space(8);
            }
            if (string.Equals(selectedTab, "All", System.StringComparison.OrdinalIgnoreCase) || string.Equals(selectedTab, "Bloodlines", System.StringComparison.OrdinalIgnoreCase))
            {
                DrawAddableTraitSection(monster, "Bloodlines", bloodlineDefs, filter, sourceFilter, false, ownedCodes);
                GUILayout.Space(8);
            }
            if (string.Equals(selectedTab, "All", System.StringComparison.OrdinalIgnoreCase) || string.Equals(selectedTab, "Universal", System.StringComparison.OrdinalIgnoreCase))
            {
                DrawAddableTraitSection(monster, "Universal", universalDefs, filter, sourceFilter, false, ownedCodes);
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
            if (GUILayout.Button("Close", GUILayout.Width(100))) { _monsterAddTraitWindow[monster] = false; InvalidateMonsterTraitPopupCache(monster); }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawAddableTraitSection(object monster, string title, object[] defs, string filter, string sourceFilter, bool replaceMode, System.Collections.Generic.HashSet<string> ownedCodes)
        {
            GUILayout.Label(title, GUI.skin.label);
            if (defs == null || defs.Length == 0)
            {
                GUILayout.Label("No traits found.", GUI.skin.box);
                return;
            }

            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i]; if (d == null) continue;
                var name = ReadString(d, new string[] { "Name", "DisplayName", "Code" });
                var badge = GameApi.GetTraitBadgeForUI(d);
                var src = GameApi.GetTraitSourceForUI(d);
                string displayName = name;
                if (!string.IsNullOrEmpty(badge)) displayName += " [" + badge + "]";
                if (!string.IsNullOrEmpty(src)) displayName += " [" + src + "]";
                if (!string.IsNullOrEmpty(filter) && displayName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (!string.Equals(sourceFilter, "All", System.StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(src, sourceFilter, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bool owned = ownedCodes != null && ownedCodes.Contains(GetTraitCacheKey(d));
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(displayName + (owned ? " (current)" : ""), GUILayout.Width(860));
                GUI.enabled = !owned;
                if (GUILayout.Button(replaceMode ? "Set" : "Add", GUILayout.Width(80)))
                {
                    int g = 1; int.TryParse(_traitLevelEdits[monster], out g); if (g < 1) g = 1; if (g > 5) g = 5;
                    GameApi.TryAddTraitToMonster(monster, d, g);
                    InvalidateMonsterTraitPopupCache(monster);
                    MarkFarmCacheDirty();
                    _monsterAddTraitWindow[monster] = false;
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        private void RefreshMonsterTraitPopupCache(object monster)
        {
            try
            {
            GetCachedAllSpeciesTraitDefs();
            GetCachedAllStatLimitTraitDefs();
            GetCachedAllBloodlineTraitDefs();
            GetCachedUniversalTraitDefs();
            _cacheOwnedTraitCodes[monster] = BuildOwnedTraitCodeCache(monster);
            GetCachedMonsterTraits(monster);
            _addTraitCategoryTab[monster] = _addTraitCategoryTab.ContainsKey(monster) ? _addTraitCategoryTab[monster] : "All";
            _addTraitSourceFilter[monster] = _addTraitSourceFilter.ContainsKey(monster) ? _addTraitSourceFilter[monster] : "All";
        }
        catch { }
    }

        private void InvalidateMonsterTraitPopupCache(object monster)
        {
            if (monster == null) return;
            _cacheOwnedTraitCodes.Remove(monster);
            _monsterTraitsSnapshotCache.Remove(monster);
            _monsterTraitsSnapshotAt.Remove(monster);
            _addTraitCategoryTab.Remove(monster);
            _addTraitSourceFilter.Remove(monster);
        }

        private void DrawAddTraitCategoryBar(object monster)
        {
            try
            {
                string[] tabs = { "All", "Species", "Stat Limits", "Bloodlines", "Universal" };
                if (!_addTraitCategoryTab.ContainsKey(monster)) _addTraitCategoryTab[monster] = "All";
                if (!_addTraitSourceFilter.ContainsKey(monster)) _addTraitSourceFilter[monster] = "All";

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label("Category:", GUILayout.Width(70));
                for (int i = 0; i < tabs.Length; i++)
                {
                    string tab = tabs[i];
                    bool active = string.Equals(_addTraitCategoryTab[monster], tab, System.StringComparison.OrdinalIgnoreCase);
                    bool newActive = GUILayout.Toggle(active, tab, GUI.skin.button, GUILayout.Width(95));
                    if (newActive && !active) _addTraitCategoryTab[monster] = tab;
                }
                GUILayout.EndHorizontal();

                string[] sources = BuildTraitSourceButtonsForPopup();
                if (sources != null && sources.Length > 0)
                {
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label("Source:", GUILayout.Width(70));
                    for (int j = 0; j < sources.Length; j++)
                    {
                        string src = sources[j];
                        bool active2 = string.Equals(_addTraitSourceFilter[monster], src, System.StringComparison.OrdinalIgnoreCase);
                        bool newActive2 = GUILayout.Toggle(active2, src, GUI.skin.button, GUILayout.Width(90));
                        if (newActive2 && !active2) _addTraitSourceFilter[monster] = src;
                    }
                    if (GUILayout.Button("Reset", GUILayout.Width(60))) _addTraitSourceFilter[monster] = "All";
                    GUILayout.EndHorizontal();
                }
            }
            catch { }
        }

        private string[] BuildTraitSourceButtonsForPopup()
        {
            try
            {
                var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                var list = new System.Collections.Generic.List<string>();
                list.Add("All");

                AppendTraitSources(list, seen, GetCachedAllSpeciesTraitDefs());
                AppendTraitSources(list, seen, GetCachedAllStatLimitTraitDefs());
                AppendTraitSources(list, seen, GetCachedAllBloodlineTraitDefs());
                AppendTraitSources(list, seen, GetCachedUniversalTraitDefs());
                return list.ToArray();
            }
            catch { }
            return new string[] { "All" };
        }

        private void AppendTraitSources(System.Collections.Generic.List<string> list, System.Collections.Generic.HashSet<string> seen, object[] defs)
        {
            if (defs == null) return;
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                if (d == null) continue;
                string src = GameApi.GetTraitSourceForUI(d);
                if (string.IsNullOrEmpty(src)) continue;
                if (seen.Add(src)) list.Add(src);
            }
        }

        private void MarkFarmCacheDirty()
        {
            _farmMonstersCacheDirty = true;
        }

        private void EnsureFarmMonstersCache()
        {
            float now = Time.realtimeSinceStartup;
            if (!_farmMonstersCacheDirty && (now - _farmMonstersCacheAt) < FarmMonstersCacheSeconds) return;

            _farmMonstersCacheDirty = false;
            _farmMonstersCacheAt = now;
            _farmMonstersCache.Clear();

            try
            {
                var mons = GameApi.GetActiveMonsters();
                for (int i = 0; i < mons.Length; i++)
                {
                    var m = mons[i];
                    if (m == null) continue;
                    _farmMonstersCache.Add(new MonsterRowCache
                    {
                        Monster = m,
                        Name = m.Name,
                        Species = GameApi.GetMonsterSpecies(m),
                        Pigment = GameApi.GetMonsterPigment(m)
                    });
                }
            }
            catch { }
        }

        private object[] GetCachedMonsterTraits(object monster)
        {
            if (monster == null) return new object[0];
            float now = Time.realtimeSinceStartup;
            object[] traits;
            float at;
            if (_monsterTraitsSnapshotCache.TryGetValue(monster, out traits) &&
                _monsterTraitsSnapshotAt.TryGetValue(monster, out at) &&
                (now - at) < MonsterTraitsCacheSeconds)
            {
                return traits;
            }

            traits = GameApi.GetMonsterTraits(monster);
            _monsterTraitsSnapshotCache[monster] = traits;
            _monsterTraitsSnapshotAt[monster] = now;
            return traits;
        }

        private object[] GetCachedSpeciesTraitDefs(string speciesName)
        {
            object[] defs;
            if (!_cacheSpeciesTraitDefsStatic.TryGetValue(speciesName, out defs))
            {
                defs = SortTraitDefinitions(GameApi.GetSpeciesTraitDefinitionsForSpeciesUI(speciesName));
                _cacheSpeciesTraitDefsStatic[speciesName] = defs;
            }
            return defs;
        }

        private object[] GetCachedAllSpeciesTraitDefs()
        {
            if (_cacheAllSpeciesTraitDefsStatic == null)
            {
                _cacheAllSpeciesTraitDefsStatic = SortTraitDefinitions(GameApi.GetSpeciesTraitDefinitionsForAllSpeciesUI());
            }
            return _cacheAllSpeciesTraitDefsStatic;
        }

        private object[] GetCachedStatLimitTraitDefs(string speciesName)
        {
            object[] defs;
            if (!_cacheStatLimitTraitDefsStatic.TryGetValue(speciesName, out defs))
            {
                defs = SortTraitDefinitions(GameApi.GetStatLimitTraitDefinitionsForSpeciesUI(speciesName));
                _cacheStatLimitTraitDefsStatic[speciesName] = defs;
            }
            return defs;
        }

        private object[] GetCachedAllStatLimitTraitDefs()
        {
            if (_cacheAllStatLimitTraitDefsStatic == null)
            {
                _cacheAllStatLimitTraitDefsStatic = SortTraitDefinitions(GameApi.GetStatLimitTraitDefinitionsForAllSpeciesUI());
            }
            return _cacheAllStatLimitTraitDefsStatic;
        }

        private object[] GetCachedBloodlineTraitDefs(string speciesName)
        {
            object[] defs;
            if (!_cacheBloodlineTraitDefsStatic.TryGetValue(speciesName, out defs))
            {
                defs = SortTraitDefinitions(GameApi.GetBloodlineTraitDefinitionsForSpeciesUI(speciesName));
                _cacheBloodlineTraitDefsStatic[speciesName] = defs;
            }
            return defs;
        }

        private object[] GetCachedAllBloodlineTraitDefs()
        {
            if (_cacheAllBloodlineTraitDefsStatic == null)
            {
                _cacheAllBloodlineTraitDefsStatic = SortTraitDefinitions(GameApi.GetBloodlineTraitDefinitionsForAllSpeciesUI());
            }
            return _cacheAllBloodlineTraitDefsStatic;
        }

        private object[] GetCachedUniversalTraitDefs()
        {
            if (_cacheUniversalTraitDefsStatic == null)
            {
                _cacheUniversalTraitDefsStatic = SortTraitDefinitions(GameApi.GetUniversalTraitDefinitions());
            }
            return _cacheUniversalTraitDefsStatic;
        }

        private object[] SortTraitDefinitions(object[] defs)
        {
            if (defs == null || defs.Length == 0) return new object[0];
            var list = new System.Collections.Generic.List<object>(defs);
            list.Sort((a, b) =>
            {
                string an = ReadString(a, new string[] { "Name", "DisplayName", "Code" }) ?? "";
                string bn = ReadString(b, new string[] { "Name", "DisplayName", "Code" }) ?? "";
                return string.Compare(an, bn, System.StringComparison.OrdinalIgnoreCase);
            });
            return list.ToArray();
        }

        private System.Collections.Generic.HashSet<string> GetOwnedTraitCodeCache(object monster)
        {
            System.Collections.Generic.HashSet<string> set;
            if (!_cacheOwnedTraitCodes.TryGetValue(monster, out set))
            {
                set = BuildOwnedTraitCodeCache(monster);
                _cacheOwnedTraitCodes[monster] = set;
            }
            return set;
        }

        private System.Collections.Generic.HashSet<string> BuildOwnedTraitCodeCache(object monster)
        {
            var set = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            try
            {
                var traits = GetCachedMonsterTraits(monster);
                int i;
                for (i = 0; i < traits.Length; i++)
                {
                    var tr = traits[i];
                    if (tr == null) continue;
                    string key = GetTraitCacheKey(ReadStat(tr, new string[] { "TraitDefinition" }) ?? tr);
                    if (!string.IsNullOrEmpty(key)) set.Add(key);
                }
            }
            catch { }
            return set;
        }

        private string GetTraitCacheKey(object traitDefinitionOrInstance)
        {
            if (traitDefinitionOrInstance == null) return string.Empty;
            string key = ReadString(traitDefinitionOrInstance, new string[] { "Code", "TraitCode", "Name", "DisplayName" });
            return key ?? string.Empty;
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
