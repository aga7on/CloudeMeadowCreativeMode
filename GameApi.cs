using System;
using TeamNimbus.CloudMeadow; // Season, Weather enums
using TeamNimbus.CloudMeadow.Managers;
using TeamNimbus.CloudMeadow.Persistence;
using TeamNimbus.CloudMeadow.Monsters;
using TeamNimbus.CloudMeadow.UI;
using TeamNimbus.CloudMeadow.Items;
using TeamNimbus.CloudMeadow.Inventory;
using TeamNimbus.CloudMeadow.Utilities;
using TeamNimbus.CloudMeadow.Monsters;
using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal static class GameApi
    {
        public static bool Ready { get { return Application.isPlaying && GameManager.Instance != null && GameManager.IsGameStatusLoaded; } }

        public static void UnlockAllGallery()
        {
            try {
                SaveGameManager.UnlockEverything();
                // Try refresh album window if open
                try {
                    var managers = UnityEngine.Object.FindObjectsOfType<TeamNimbus.CloudMeadow.UI.AlbumWindowManager>();
                    if (managers != null && managers.Length > 0)
                    {
                        for (int i = 0; i < managers.Length; i++) managers[i].UpdateLockedStatus();
                    }
                } catch { }
                LogBuffer.Add("Unlock Gallery: all content");
                Plugin.Log.LogInfo("Gallery unlock invoked.");
                Banner("Album: All scenes unlocked");
            } catch (Exception e) { Plugin.Log.LogWarning("UnlockEverything failed: " + e.Message); }
        }

        public static void AddKorona(int amount)
        {
            try { GameManager.Status.UpdateKorona(amount, TransactionSource.Cheating); LogBuffer.Add("Korona +" + amount); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void AddShards(int amount)
        {
            try { GameManager.Status.UpdateUpgradeShards(amount); LogBuffer.Add("Shards +" + amount); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void AdvanceToEndOfDay()
        {
            try { GameManager.Status.GetCalendarDate.CHEAT_AdvanceTimeToEndOfDay(); LogBuffer.Add("Advance to end of day"); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void ToggleGodMode()
        {
            try { var s = GameManager.Status; s.GodMode = !s.GodMode; var on = s.GodMode; LogBuffer.Add("GodMode: " + (on ? "ON" : "OFF")); Banner("GodMode: " + (on ? "ON" : "OFF")); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void SetSeason(Season season)
        {
            try { GameManager.Status.GetCalendarDate.CHEAT_AdvanceTimeToSeason(season); LogBuffer.Add("Season -> " + season); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void SetWeather(Weather weather)
        {
            try { GameManager.Status.CHEAT_ForceWeatherChange(weather); LogBuffer.Add("Weather -> " + weather); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void ClearBarn()
        {
            try { GameManager.Status.CHEAT_ClearAllActiveMonsters(); LogBuffer.Add("Cleared barn"); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void GiveEveryMonster()
        {
            try { TeamNimbus.CloudMeadow.Combat.DebugCheats.AddAllMonsters(Mathf.Max(GameManager.Status.ProtagonistStats.Level, 15)); LogBuffer.Add("Give all monsters"); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void RecruitAllCompanions(int level)
        {
            try { TeamNimbus.CloudMeadow.Combat.DebugCheats.RecruitCompanions(level); LogBuffer.Add("Recruit companions to ~L" + level); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void LevelCompanions(int level)
        {
            try { TeamNimbus.CloudMeadow.Combat.DebugCheats.LevelCompanions(level); LogBuffer.Add("Level companions -> " + level); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void LevelMonsters(int level)
        {
            try { TeamNimbus.CloudMeadow.Combat.DebugCheats.LevelMonsters(level); LogBuffer.Add("Level monsters -> " + level); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void LevelAll(int level)
        {
            try { TeamNimbus.CloudMeadow.Combat.DebugCheats.LevelAll(level); LogBuffer.Add("Level ALL -> " + level); } catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void AddHarvestAndGroceries()
        {
            try
            {
                var itemLib = GameManager.ItemLibrary;
                var inventory = GameManager.Status.Inventory;
                int added = 0;
                foreach (var def in itemLib.EnumerateItemsInCategory(ItemCategory.Crop))
                {
                    var entry = new StandardItemEntry(def, ItemQuality.OneStar, 99);
                    inventory.AddItemEntry(entry);
                    added++;
                }
                foreach (var def in itemLib.EnumerateItemsInCategory(ItemCategory.Ingredient))
                {
                    var entry = new StandardItemEntry(def, ItemQuality.OneStar, 99);
                    inventory.AddItemEntry(entry);
                    added++;
                }
                LogBuffer.Add("Added harvest & groceries: " + added + " entries");
            }
            catch (Exception e) { Plugin.Log.LogWarning(e.ToString()); }
        }

        public static void SetTime(int hour, int minute)
        {
            try
            {
                var cal = GameManager.Status.GetCalendarDate;
                // compute total minutes today
                int target = (hour % 24) * 60 + (minute % 60);
                int cur = cal.DateTime.Hour * 60 + cal.DateTime.Minute;
                int diff = target - cur;
                cal.TickMinutes(diff);
                Banner("Time set to " + hour + ":" + minute.ToString("00"));
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetTime failed: " + e.Message); }
        }

        public static MonsterCharacterStats[] GetActiveMonsters()
        {
            try
            {
                var en = GameManager.Status.EnumerateActiveMonsters();
                var list = new System.Collections.Generic.List<MonsterCharacterStats>();
                foreach (var m in en) list.Add(m);
                return list.ToArray();
            }
            catch (Exception e) { Plugin.Log.LogWarning("GetActiveMonsters failed: " + e.Message); }
            return new MonsterCharacterStats[0];
        }

        public static void RemoveMonster(MonsterCharacterStats m)
        {
            try { GameManager.Status.RemoveActiveMonster(m); Banner("Removed monster: " + m.Name); }
            catch (Exception e) { Plugin.Log.LogWarning("RemoveMonster failed: " + e.Message); }
        }

        private static void Banner(string msg)
        {
            try { TeamNimbus.CloudMeadow.Utilities.BannerMessage.ShowMessage(msg, 1.2f, null); } catch { }
        }

        public static string BuildQuickStatus()
        {
            try
            {
                var s = GameManager.Status;
                var dt = s.GetCalendarDate;
                return string.Format("Korona: {0} | Shards: {1} | Monsters: {2}/{3} | Protag Lv {4} {5} | Season: {6} Day: {7} Hour: {8}", s.KoronaBalance, s.NumUpgradeShards, s.NumMonstersOnTheFarm, s.FarmStatus.ResolveNumberOfMonsterSpotsOnFarm(), s.ProtagonistStats.Level, s.ProtagonistStats.Gender, dt.DateTime.Season, dt.DateTime.Day, dt.DateTime.Hour);
            }
            catch (Exception e)
            {
                return "Status unavailable: " + e.Message;
            }
        }
    }
}
