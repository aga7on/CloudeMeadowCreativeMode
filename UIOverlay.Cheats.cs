using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal partial class UIOverlay
    {
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
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Monsters: Quality of Life");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Max loyalty (all)")) GameApi.MaxAllMonstersLoyalty();
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
            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }
    }
}
