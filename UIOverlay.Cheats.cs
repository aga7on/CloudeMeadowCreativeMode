using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal partial class UIOverlay
    {
        private string _spawnMonsterSpecies = "Chimera";
        private string _spawnMonsterLevel = "15";
        private bool _showChimeraSpawnWindow;
        private bool _chimeraVariantDropdownOpen;
        private bool _chimeraLevelDropdownOpen;
        private string _chimeraVariantName = "";
        private int _chimeraLevelValue = 15;

        private void DrawCheats()
        {
            if (!GameApi.Ready)
            {
                GUILayout.Label("Game not ready or save not loaded.");
                return;
            }
            GUILayout.Label(GameApi.BuildQuickStatus());
            GUILayout.Space(5);

            // Compact vertical layout: sections stacked top-to-bottom
            GUILayout.BeginVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Money & Resources");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1k Korona", GUILayout.Width(110))) GameApi.AddKorona(1000);
            if (GUILayout.Button("+100k", GUILayout.Width(90))) GameApi.AddKorona(100000);
            if (GUILayout.Button("+1M", GUILayout.Width(70))) GameApi.AddKorona(1000000);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+100 Shards", GUILayout.Width(120))) GameApi.AddShards(100);
            if (GUILayout.Button("All harvest & groceries", GUILayout.Width(220))) GameApi.AddHarvestAndGroceries();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Farm Upgrades");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock farm upgrades", GUILayout.Width(200))) GameApi.UpgradeFarm();
            if (GUILayout.Button("Water all crops", GUILayout.Width(160))) GameApi.WaterAllCrops();
            if (GUILayout.Button("Grow all crops", GUILayout.Width(150))) GameApi.GrowAllCrops();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("HATCH ALL EGGS", GUILayout.Width(200))) GameApi.HatchAllEggs();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Monsters & Companions");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Barn", GUILayout.Width(120))) GameApi.ClearBarn();
            if (GUILayout.Button("Give every monster (auto level)", GUILayout.Width(260))) GameApi.GiveEveryMonster();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Recruit companions L10", GUILayout.Width(220))) GameApi.RecruitAllCompanions(10);
            if (GUILayout.Button("L15", GUILayout.Width(60))) GameApi.RecruitAllCompanions(15);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Level companions to 20", GUILayout.Width(230))) GameApi.LevelCompanions(20);
            if (GUILayout.Button("Level monsters to 20", GUILayout.Width(220))) GameApi.LevelMonsters(20);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Level ALL to 30", GUILayout.Width(170))) GameApi.LevelAll(30);
            if (GUILayout.Button("Level ALL to 60", GUILayout.Width(170))) GameApi.LevelAll(60);
            if (GUILayout.Button("Upgrade abilities (party)", GUILayout.Width(200))) GameApi.UpgradeAllAbilitiesForParty();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Spawn:", GUILayout.Width(50));
            _spawnMonsterSpecies = GUILayout.TextField(_spawnMonsterSpecies ?? "Chimera", GUILayout.Width(120));
            GUILayout.Label("Lvl", GUILayout.Width(24));
            _spawnMonsterLevel = GUILayout.TextField(_spawnMonsterLevel ?? "15", GUILayout.Width(40));
            if (GUILayout.Button("Add monster", GUILayout.Width(120)))
            {
                int spawnLevel = 15;
                int.TryParse(_spawnMonsterLevel, out spawnLevel);
                GameApi.AddMonster(_spawnMonsterSpecies, spawnLevel);
            }
            if (GUILayout.Button("Add Chimera", GUILayout.Width(120)))
            {
                _showChimeraSpawnWindow = !_showChimeraSpawnWindow;
            }
            GUILayout.EndHorizontal();
            if (_showChimeraSpawnWindow) DrawChimeraSpawnWindow();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Monsters: Quality of Life");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Max loyalty (all)")) GameApi.MaxAllMonstersLoyalty();
            if (GUILayout.Button("Safe audit + fix")) GameApi.RunSafeConsistencyAuditAndFix();
            // Disabled by request: harvest times spam causes perf issues
            // if (GUILayout.Button("Set 99 Harvest Times Available")) GameApi.SetExtraHarvestTimesForAll(99);
            GUILayout.EndHorizontal();
            bool toggleUltra = GUILayout.Toggle(GameApi.UltraBreadEnabled, "Ultra Bread (toggle)");
            if (toggleUltra != GameApi.UltraBreadEnabled) GameApi.ToggleUltraBread();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Combat & Movement");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Win Combat", GUILayout.Width(140))) GameApi.WinCombat();
            if (GUILayout.Button("God Mode (toggle)", GUILayout.Width(180))) GameApi.ToggleGodMode();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(GameApi.NoClipEnabled ? "No Clip: ON" : "No Clip: OFF", GUILayout.Width(160))) GameApi.ToggleNoClip();
            if (GUILayout.Button("Speed x10", GUILayout.Width(110))) GameApi.SetSpeedMultiplier(10f);
            if (GUILayout.Button("Speed x1", GUILayout.Width(100))) GameApi.SetSpeedMultiplier(1f);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Misc");
            if (GUILayout.Button("Unlock Gallery (All)")) GameApi.UnlockAllGallery();
            bool verboseDiag = GameApi.VerboseDiagnosticsEnabled;
            bool verboseDiagNew = GUILayout.Toggle(verboseDiag, "Verbose diagnostics");
            if (verboseDiagNew != verboseDiag) GameApi.SetVerboseDiagnostics(verboseDiagNew);
            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }

        private void DrawChimeraSpawnWindow()
        {
            GUILayout.BeginVertical(GUI.skin.window);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Chimera Spawn");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(60)))
            {
                _showChimeraSpawnWindow = false;
                _chimeraVariantDropdownOpen = false;
                _chimeraLevelDropdownOpen = false;
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }
            GUILayout.EndHorizontal();

            var variants = GameApi.GetSpeciesTraitNamesForSpecies("Chimera");
            if ((variants == null || variants.Length == 0) && string.IsNullOrEmpty(_chimeraVariantName)) _chimeraVariantName = "QuickOnTheWing";
            if (variants != null && variants.Length > 0)
            {
                bool found = false;
                for (int i = 0; i < variants.Length; i++) if (variants[i] == _chimeraVariantName) found = true;
                if (!found) _chimeraVariantName = variants[0];
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Variant", GUILayout.Width(60));
            if (GUILayout.Button(string.IsNullOrEmpty(_chimeraVariantName) ? "Select" : _chimeraVariantName, GUILayout.Width(180)))
            {
                _chimeraVariantDropdownOpen = !_chimeraVariantDropdownOpen;
                _chimeraLevelDropdownOpen = false;
            }
            GUILayout.Label("Level", GUILayout.Width(40));
            if (GUILayout.Button(_chimeraLevelValue.ToString(), GUILayout.Width(80)))
            {
                _chimeraLevelDropdownOpen = !_chimeraLevelDropdownOpen;
                _chimeraVariantDropdownOpen = false;
            }
            GUILayout.EndHorizontal();

            if (_chimeraVariantDropdownOpen && variants != null)
            {
                for (int i = 0; i < variants.Length; i++)
                {
                    if (GUILayout.Button(variants[i], GUILayout.Width(250)))
                    {
                        _chimeraVariantName = variants[i];
                        _chimeraVariantDropdownOpen = false;
                    }
                }
            }

            if (_chimeraLevelDropdownOpen)
            {
                int[] levels = new int[] { 1, 5, 10, 15, 20, 30, 60 };
                for (int j = 0; j < levels.Length; j++)
                {
                    if (GUILayout.Button(levels[j].ToString(), GUILayout.Width(100)))
                    {
                        _chimeraLevelValue = levels[j];
                        _chimeraLevelDropdownOpen = false;
                    }
                }
            }

            GUILayout.Space(4);
            if (GUILayout.Button("Spawn", GUILayout.Width(120)))
            {
                GameApi.SpawnChimeraVariant(_chimeraVariantName, _chimeraLevelValue);
                _showChimeraSpawnWindow = false;
                _chimeraVariantDropdownOpen = false;
                _chimeraLevelDropdownOpen = false;
            }
            GUILayout.EndVertical();
        }
    }
}
