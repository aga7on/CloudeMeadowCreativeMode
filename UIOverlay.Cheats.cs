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
            if (GUILayout.Button("+1,000 Korona")) GameApi.AddKorona(1000);
            if (GUILayout.Button("+100,000 Korona")) GameApi.AddKorona(100000);
            if (GUILayout.Button("+1,000,000 Korona")) GameApi.AddKorona(1000000);
            if (GUILayout.Button("+100 Upgrade Shards")) GameApi.AddShards(100);
            if (GUILayout.Button("All harvest & groceries")) GameApi.AddHarvestAndGroceries();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Farm Upgrades");
            if (GUILayout.Button("Upgrade Farm (unlock all)")) GameApi.UpgradeFarm();
            if (GUILayout.Button("Water all crops")) GameApi.WaterAllCrops();
            if (GUILayout.Button("Grow all crops")) GameApi.GrowAllCrops();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
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
            if (GUILayout.Button("Level ALL to 60")) GameApi.LevelAll(60);
            if (GUILayout.Button("Upgrade all abilities (party)")) GameApi.UpgradeAllAbilitiesForParty();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Monster QoL");
            if (GUILayout.Button("Max all monsters loyalty")) GameApi.MaxAllMonstersLoyalty();
            if (GUILayout.Button("Set 99 Harvest Times Available")) GameApi.SetExtraHarvestTimesForAll(99);
            bool toggleUltra = GUILayout.Toggle(GameApi.UltraBreadEnabled, "Make Ultra Bread?!");
            if (toggleUltra != GameApi.UltraBreadEnabled) GameApi.ToggleUltraBread();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Misc");
            if (GUILayout.Button("Unlock Gallery (All)")) GameApi.UnlockAllGallery();
            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }
    }
}
