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
            if (GUILayout.Button("Spring")) GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Spring);
            if (GUILayout.Button("Summer")) GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Summer);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Autumn")) GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Autumn);
            if (GUILayout.Button("Winter")) GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Winter);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250));
            GUILayout.Label("Weather");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Clear);
            if (GUILayout.Button("Rain")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Rain);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Storm")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Storm);
            if (GUILayout.Button("Snow")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Snow);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Blazing Heat")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.BlazingHeat);
            if (GUILayout.Button("Falling Leaves")) GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Leafs);
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
