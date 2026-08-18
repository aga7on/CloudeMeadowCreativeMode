using System;
using TeamNimbus.CloudMeadow; // Season, Weather enums
using TeamNimbus.CloudMeadow.Managers;
using TeamNimbus.CloudMeadow.Persistence;
using TeamNimbus.CloudMeadow.Monsters;
using TeamNimbus.CloudMeadow.UI;
using TeamNimbus.CloudMeadow.Items;
using TeamNimbus.CloudMeadow.Inventory;
using TeamNimbus.CloudMeadow.Utilities;
using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal static class GameApi
    {
        public static void SetProtagonistGender(string desired)
        {
            try
            {
                var p = GameManager.Status.ProtagonistStats;
                Gender target = (Gender)Enum.Parse(typeof(Gender), desired, true);
                if (p.Gender == target) return;

                // Gender is getter-only: the old generic property setter never reached
                // the backing field. The game method also refreshes sprites/appearance.
                var canSwap = p.GetType().GetMethod("CanSwapGender", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var swap = p.GetType().GetMethod("SwapGender", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                bool allowed = canSwap == null || (bool)canSwap.Invoke(p, null);
                if (!allowed || swap == null) throw new MissingMethodException(p.GetType().FullName, "SwapGender");
                swap.Invoke(p, null);
                LogBuffer.Add("Protagonist gender -> " + p.Gender);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetProtagonistGender failed: " + e); }
        }

        public static void SetProtagonistLevel(int level)
        {
            try
            {
                if (level < 1) level = 1;
                if (level > GameManager.MaxLevel) level = GameManager.MaxLevel;
                var p = GameManager.Status.ProtagonistStats;
                var field = FindInstanceField(p.GetType(), "level");
                if (field == null) throw new MissingFieldException(p.GetType().FullName, "level");
                field.SetValue(p, level);
                LogBuffer.Add("Protagonist level -> " + level);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetProtagonistLevel failed: " + e.Message); }
        }

        public static void RenameProtagonist(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return;
                GameManager.Status.ProtagonistStats.RenameCharacter(name.Trim());
                LogBuffer.Add("Protagonist renamed -> " + name.Trim());
            }
            catch (Exception e) { Plugin.Log.LogWarning("RenameProtagonist failed: " + e.Message); }
        }

        public static string GetProtagonistPrimaryStatsSummary()
        {
            try
            {
                var p = GameManager.Status.ProtagonistStats;
                return "Physique " + p.GetPrimaryStatData(PrimaryStat.Physique).BaseValue +
                       " | Stamina " + p.GetPrimaryStatData(PrimaryStat.Stamina).BaseValue +
                       " | Intuition " + p.GetPrimaryStatData(PrimaryStat.Intuition).BaseValue +
                       " | Swiftness " + p.GetPrimaryStatData(PrimaryStat.Swiftness).BaseValue;
            }
            catch { return "Primary stats unavailable"; }
        }

        public static void SetProtagonistPrimaryStat(string statName, int targetValue)
        {
            try
            {
                var stat = (PrimaryStat)Enum.Parse(typeof(PrimaryStat), statName, true);
                var p = GameManager.Status.ProtagonistStats;
                var data = p.GetPrimaryStatData(stat);
                int growth = Mathf.RoundToInt(data.GrowthValue);
                int desiredCustom = Mathf.Clamp(targetValue - growth, 0, data.MaxCustomValue);
                p.IncreasePrimaryStatCustomValue(stat, desiredCustom - data.CustomValue);
                ReportPrimaryStatResult(p, stat, targetValue);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetProtagonistPrimaryStat failed: " + e.Message); }
        }

        public static string GetPrimaryStatsSummary(PartyCharacterStats stats)
        {
            try
            {
                return "Physique " + stats.GetPrimaryStatData(PrimaryStat.Physique).BaseValue +
                       " | Stamina " + stats.GetPrimaryStatData(PrimaryStat.Stamina).BaseValue +
                       " | Intuition " + stats.GetPrimaryStatData(PrimaryStat.Intuition).BaseValue +
                       " | Swiftness " + stats.GetPrimaryStatData(PrimaryStat.Swiftness).BaseValue;
            }
            catch { return "Primary stats unavailable"; }
        }

        public static void SetPrimaryStat(PartyCharacterStats stats, string statName, int targetValue)
        {
            try
            {
                if (stats == null) return; var stat = (PrimaryStat)Enum.Parse(typeof(PrimaryStat), statName, true);
                var data = stats.GetPrimaryStatData(stat); int growth = Mathf.RoundToInt(data.GrowthValue);
                int desiredCustom = Mathf.Clamp(targetValue - growth, 0, data.MaxCustomValue);
                stats.IncreasePrimaryStatCustomValue(stat, desiredCustom - data.CustomValue);
                ReportPrimaryStatResult(stats, stat, targetValue);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetPrimaryStat failed: " + e.Message); }
        }

        public static void HealProtagonist()
        {
            try
            {
                var p = GameManager.Status.ProtagonistStats;
                p.UpdateCurrentHP(p.GetMaxHP());
                LogBuffer.Add("Protagonist fully healed");
            }
            catch (Exception e) { Plugin.Log.LogWarning("HealProtagonist failed: " + e.Message); }
        }

        public static void SetProtagonistXP(float value)
        {
            try
            {
                if (value < 0f) value = 0f; var p = GameManager.Status.ProtagonistStats;
                var field = FindInstanceField(p.GetType(), "experienceSinceLastLevel");
                if (field == null) throw new MissingFieldException("experienceSinceLastLevel"); field.SetValue(p, value);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetProtagonistXP failed: " + e.Message); }
        }

        public static void SetProtagonistStatPoints(int value)
        {
            try
            {
                if (value < 0) value = 0; var p = GameManager.Status.ProtagonistStats;
                var field = FindInstanceField(p.GetType(), "statPoints");
                if (field == null) throw new MissingFieldException("statPoints"); field.SetValue(p, value);
                Banner("Player stat points -> " + value);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetProtagonistStatPoints failed: " + e.Message); }
        }

        public static void SetProtagonistPronoun(string value)
        {
            try
            {
                var parsed = (Pronoun)Enum.Parse(typeof(Pronoun), value, true);
                GameManager.Status.ProtagonistStats.UpdatePronoun(parsed);
                LogBuffer.Add("Protagonist pronoun -> " + parsed);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetProtagonistPronoun failed: " + e.Message); }
        }

        public static void SyncProtagonistPronoun()
        {
            try
            {
                var p = GameManager.Status.ProtagonistStats;
                SetProtagonistPronoun(p.Gender == Gender.Male ? "He" : "She");
            }
            catch (Exception e) { Plugin.Log.LogWarning("SyncProtagonistPronoun failed: " + e.Message); }
        }

        public static string[] GetProtagonistAbilitySummary()
        {
            var lines = new System.Collections.Generic.List<string>();
            try
            {
                int index = 0; foreach (var state in GameManager.Status.ProtagonistStats.EnumerateAbilityStates())
                    lines.Add("Slot " + (index++) + ": " + (state.Asset != null ? state.Asset.name : "(missing)") + " | State " + state.ActiveStateIndex + " | Cooldown " + state.TurnsRemainingOnCooldown);
            }
            catch (Exception e) { lines.Add("Ability scan failed: " + e.Message); }
            return lines.ToArray();
        }

        public static int RepairProtagonistAbilityStates()
        {
            int repaired = 0;
            try
            {
                var protagonist = GameManager.Status.ProtagonistStats;
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                System.Reflection.FieldInfo dataField = null; Type scan = protagonist.GetType();
                while (scan != null && dataField == null) { dataField = scan.GetField("dataForAbilities", flags); scan = scan.BaseType; }
                var data = dataField != null ? dataField.GetValue(protagonist) as System.Collections.IList : null;
                if (data == null) return 0;
                for (int slot = 0; slot < data.Count; slot++)
                {
                    object abilityData = data[slot]; if (abilityData == null) continue;
                    var stateProp = abilityData.GetType().GetProperty("StateIndex", flags);
                    if (stateProp == null) continue;
                    int current = Convert.ToInt32(stateProp.GetValue(abilityData, null));
                    int count = GetProtagonistAbilityStateCount(protagonist, slot);
                    // A missing count means the current game version exposes no safe
                    // upper bound. Never rewrite the save in that case.
                    if (count <= 0) continue;
                    int safe = Mathf.Clamp(current, 0, count - 1);
                    if (current != safe) { protagonist.ChangeAbilityState(slot, safe); repaired++; LogBuffer.Add("Repaired player ability slot " + slot + ": " + current + " -> " + safe); }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("RepairProtagonistAbilityStates failed: " + e.Message); }
            return repaired;
        }

        private static System.Reflection.FieldInfo FindInstanceField(Type type, string name)
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
            while (type != null)
            {
                var field = type.GetField(name, flags);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }

        private static void ReportPrimaryStatResult(PartyCharacterStats stats, PrimaryStat stat, int requested)
        {
            try
            {
                var data = stats.GetPrimaryStatData(stat);
                int actual = Mathf.RoundToInt(data.BaseValue);
                int growth = Mathf.RoundToInt(data.GrowthValue);
                int maximum = growth + data.MaxCustomValue;
                LogBuffer.Add(stat + " -> " + actual + " (requested " + requested + ", allowed " + growth + "-" + maximum + ")");
                if (actual != requested)
                    Banner(stat + " set to " + actual + "; this character's allowed range is " + growth + "-" + maximum);
            }
            catch { }
        }

        private static int GetProtagonistAbilityStateCount(object protagonist, int slot)
        {
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                var assetProp = protagonist.GetType().GetProperty("CharacterAsset", flags);
                object asset = assetProp != null ? assetProp.GetValue(protagonist, null) : null; if (asset == null) return 0;
                var slotsProp = asset.GetType().GetProperty("AbilitySlots", flags);
                object slots = slotsProp != null ? slotsProp.GetValue(asset, null) : null;
                var slotsList = slots as System.Collections.IList; if (slotsList == null || slot < 0 || slot >= slotsList.Count) return 0;
                object abilitySlot = slotsList[slot]; if (abilitySlot == null) return 0;
                var countProp = abilitySlot.GetType().GetProperty("Count", flags) ?? abilitySlot.GetType().GetProperty("Length", flags);
                if (countProp != null) return Convert.ToInt32(countProp.GetValue(abilitySlot, null));
                var collection = abilitySlot as System.Collections.ICollection; if (collection != null) return collection.Count;
            }
            catch { }
            return 0;
        }

        public static void SetProtagonistAbilityState(int slot, int state)
        {
            try
            {
                var protagonist = GameManager.Status.ProtagonistStats; int count = GetProtagonistAbilityStateCount(protagonist, slot);
                if (count <= 0) throw new ArgumentOutOfRangeException("slot", "Ability slot is unavailable");
                int safe = Mathf.Clamp(state, 0, count - 1); protagonist.ChangeAbilityState(slot, safe);
                LogBuffer.Add("Player ability slot " + slot + " -> state " + safe + (safe != state ? " (clamped from " + state + ")" : string.Empty));
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetProtagonistAbilityState failed: " + e.Message); }
        }

        public static void ClearProtagonistCooldowns()
        { try { GameManager.Status.ProtagonistStats.ClearAllCooldowns(); LogBuffer.Add("Player cooldowns cleared"); } catch (Exception e) { Plugin.Log.LogWarning(e.Message); } }

        public static void ResetProtagonistLifecycleStatus()
        {
            try
            {
                var p = GameManager.Status.ProtagonistStats; var type = p.GetType(); System.Reflection.MethodInfo method = null;
                while (type != null && method == null) { method = type.GetMethod("UpdateStatus", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly); type = type.BaseType; }
                if (method == null) throw new MissingMethodException("UpdateStatus");
                object date = Activator.CreateInstance(method.GetParameters()[1].ParameterType);
                method.Invoke(p, new object[] { TeamNimbus.CloudMeadow.Monsters.PartyCharacterStatus.Idle, date });
                LogBuffer.Add("Player lifecycle status -> Idle");
            }
            catch (Exception e) { Plugin.Log.LogWarning("ResetProtagonistLifecycleStatus failed: " + e.Message); }
        }
        public static EquipmentItemEntry[] GetPlayerEquipmentInventory()
        {
            var list = new System.Collections.Generic.List<EquipmentItemEntry>(); try { foreach (var entry in GameManager.Status.Inventory.EnumerateEquipmentEntrys()) { var equipment = entry as EquipmentItemEntry; if (equipment != null) list.Add(equipment); } } catch { } return list.ToArray();
        }
        public static void EquipProtagonist(EquipmentItemEntry item) { try { if (item != null) { GameManager.Status.ProtagonistStats.EquipItem(item); LogBuffer.Add("Player equipped: " + item); } } catch (Exception e) { Plugin.Log.LogWarning("EquipProtagonist failed: " + e.Message); } }
        public static void UnequipProtagonist(string category) { try { var c = (ItemCategory)Enum.Parse(typeof(ItemCategory), category, true); GameManager.Status.ProtagonistStats.UnequipItem(c); LogBuffer.Add("Player unequipped: " + c); } catch (Exception e) { Plugin.Log.LogWarning("UnequipProtagonist failed: " + e.Message); } }

        public static string BackupAllSaves()
        {
            try
            {
                string source = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..\\LocalLow\\Team Nimbus\\Cloud Meadow");
                source = System.IO.Path.GetFullPath(source);
                string root = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx\\plugins\\CloudMeadowCreativeMode\\backups");
                string target = System.IO.Path.Combine(root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                System.IO.Directory.CreateDirectory(target);
                string[] files = System.IO.Directory.GetFiles(source, "*", System.IO.SearchOption.AllDirectories);
                int copied = 0;
                for (int i = 0; i < files.Length; i++)
                {
                    string ext = System.IO.Path.GetExtension(files[i]).ToLowerInvariant();
                    string name = System.IO.Path.GetFileName(files[i]).ToLowerInvariant();
                    if (ext != ".json" && ext != ".sav" && ext != ".meta" && name != "steam_autocloud.vdf") continue;
                    string rel = files[i].Substring(source.Length).TrimStart('\\', '/');
                    string dest = System.IO.Path.Combine(target, rel);
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));
                    System.IO.File.Copy(files[i], dest, true); copied++;
                }
                LogBuffer.Add("Save backup: " + copied + " files -> " + target);
                try
                {
                    var dirs = new System.Collections.Generic.List<System.IO.DirectoryInfo>(new System.IO.DirectoryInfo(root).GetDirectories());
                    dirs.Sort(delegate(System.IO.DirectoryInfo a, System.IO.DirectoryInfo b) { return b.CreationTimeUtc.CompareTo(a.CreationTimeUtc); });
                    for (int i = 10; i < dirs.Count; i++) dirs[i].Delete(true);
                }
                catch { }
                return target;
            }
            catch (Exception e) { Plugin.Log.LogWarning("BackupAllSaves failed: " + e); return "FAILED: " + e.Message; }
        }

        public static string[] GetSaveBackupSummary()
        {
            try
            {
                string root = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx\\plugins\\CloudMeadowCreativeMode\\backups");
                if (!System.IO.Directory.Exists(root)) return new string[0];
                var dirs = new System.Collections.Generic.List<System.IO.DirectoryInfo>(new System.IO.DirectoryInfo(root).GetDirectories());
                dirs.Sort(delegate(System.IO.DirectoryInfo a, System.IO.DirectoryInfo b) { return b.CreationTimeUtc.CompareTo(a.CreationTimeUtc); });
                var lines = new System.Collections.Generic.List<string>();
                for (int i = 0; i < dirs.Count && i < 10; i++) lines.Add(dirs[i].Name + " | " + dirs[i].GetFiles("*", System.IO.SearchOption.AllDirectories).Length + " files");
                return lines.ToArray();
            }
            catch { return new string[0]; }
        }
        public static string RestoreLatestSaveBackup()
        {
            try
            {
                if (Ready) return "REFUSED: return to main menu before restoring";
                string root = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx\\plugins\\CloudMeadowCreativeMode\\backups"); if (!System.IO.Directory.Exists(root)) return "No backups";
                var dirs = new System.Collections.Generic.List<System.IO.DirectoryInfo>(new System.IO.DirectoryInfo(root).GetDirectories()); dirs.Sort(delegate(System.IO.DirectoryInfo a, System.IO.DirectoryInfo b) { return b.CreationTimeUtc.CompareTo(a.CreationTimeUtc); }); if (dirs.Count == 0) return "No backups";
                string target = System.IO.Path.GetFullPath(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "..\\LocalLow\\Team Nimbus\\Cloud Meadow")); int copied = 0;
                foreach (var file in dirs[0].GetFiles("*", System.IO.SearchOption.AllDirectories)) { string rel = file.FullName.Substring(dirs[0].FullName.Length).TrimStart('\\', '/'); string dest = System.IO.Path.Combine(target, rel); System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest)); file.CopyTo(dest, true); copied++; }
                string result = "Restored " + copied + " files from " + dirs[0].Name; LogBuffer.Add(result); return result;
            }
            catch (Exception e) { return "FAILED: " + e.Message; }
        }

        public static string GetCompatibilitySummary()
        {
            try
            {
                string gameDll = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "Cloud Meadow_Data\\Managed\\Game.dll");
                var info = new System.IO.FileInfo(gameDll);
                string hash;
                using (var stream = System.IO.File.OpenRead(gameDll))
                using (var sha = new System.Security.Cryptography.SHA256Managed())
                    hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).Substring(0, 16);
                return "Game.dll " + info.Length + " bytes | " + info.LastWriteTime.ToString("u") + " | SHA256 " + hash + "... | API ready=" + Ready;
            }
            catch (Exception e) { return "Compatibility scan failed: " + e.Message; }
        }

        public static string GetTraitDisplayName(object value)
        {
            try
            {
                var instance = value as TeamNimbus.CloudMeadow.Traits.TraitInstance;
                if (instance != null) return instance.DisplayName;
                var definition = value as TeamNimbus.CloudMeadow.Traits.BaseTraitDefinition;
                if (definition != null) return definition.DisplayName;
            }
            catch { }
            return value != null ? value.ToString() : "Unknown trait";
        }

        public static string GetTraitDescription(object value)
        {
            try
            {
                var instance = value as TeamNimbus.CloudMeadow.Traits.TraitInstance;
                if (instance != null) return instance.TraitDefinition.Description(instance);
                var definition = value as TeamNimbus.CloudMeadow.Traits.BaseTraitDefinition;
                if (definition != null) return definition.Description(new TeamNimbus.CloudMeadow.Traits.TraitInstance(definition, 1));
            }
            catch { }
            return "Description unavailable";
        }

        public static string GetTraitQuality(object value)
        {
            try
            {
                var instance = value as TeamNimbus.CloudMeadow.Traits.TraitInstance;
                if (instance != null) return instance.Quality.ToString();
                var definition = value as TeamNimbus.CloudMeadow.Traits.BaseTraitDefinition;
                if (definition != null) return definition.Quality.ToString();
            }
            catch { }
            return "Common";
        }

        public static string GetMigrationSummary()
        {
            try
            {
                var data = GameManager.Status.MigrationSaveData;
                return "Unlocked: " + data.IsMigrationUnlocked + " | Seed: " + data.Seed +
                       " | Savannah: " + data.ActiveSavannahMigrationDate +
                       " | Forest: " + data.ActiveForestMigrationDate +
                       " | Migrated species: " + data.AllMigratedSpecies.Count;
            }
            catch (Exception e) { return "Migration data unavailable: " + e.Message; }
        }

        public static void UnlockMigrations()
        {
            try
            {
                var data = GameManager.Status.MigrationSaveData;
                var field = data.GetType().GetField("migrationsUnlocked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field == null) throw new MissingFieldException(data.GetType().FullName, "migrationsUnlocked");
                field.SetValue(data, true); data.UpdateActiveMigrationDate();
                LogBuffer.Add("Migrations unlocked");
            }
            catch (Exception e) { Plugin.Log.LogWarning("UnlockMigrations failed: " + e.Message); }
        }

        public static void RerollMigrations()
        {
            try
            {
                var data = GameManager.Status.MigrationSaveData;
                data.SetSeed(UnityEngine.Random.Range(100000, 999999));
                data.UpdateActiveMigrationDate(); data.RegenerateRuntimeData();
                LogBuffer.Add("Migration seed -> " + data.Seed);
            }
            catch (Exception e) { Plugin.Log.LogWarning("RerollMigrations failed: " + e.Message); }
        }
        public static void LockMigrations() { try { GameManager.Status.MigrationSaveData.SetMigrationsLocked(); LogBuffer.Add("Migrations locked"); } catch (Exception e) { Plugin.Log.LogWarning(e.Message); } }
        public static void SyncMigrationDungeonProgress() { try { var data = GameManager.Status.MigrationSaveData; data.UpdateWithPersistentData(); data.RegenerateRuntimeData(); LogBuffer.Add("Migration progress synchronized"); } catch (Exception e) { Plugin.Log.LogWarning(e.Message); } }
        public static void ClearMigrationDiscoveries() { try { var data = GameManager.Status.MigrationSaveData; data.ClearDiscoveriesFromDungeons(); data.RegenerateRuntimeData(); LogBuffer.Add("Migration discoveries cleared"); } catch (Exception e) { Plugin.Log.LogWarning(e.Message); } }

        public static string GetDungeonSummary()
        {
            try
            {
                var s = GameManager.Status;
                return "Current zone: " + s.CurrentZone + " | Savannah floor: " + s.SavannahPersistentDungeonData.CurrentFloor +
                       " | Forest floor: " + s.ForestPersistentDungeonData.CurrentFloor;
            }
            catch (Exception e) { return "Dungeon data unavailable: " + e.Message; }
        }

        public static void SetDungeonFloor(string zoneName, int floor)
        {
            try
            {
                if (floor < 1) floor = 1;
                TeamNimbus.CloudMeadow.Persistence.PersistentDungeonData data;
                TeamNimbus.CloudMeadow.Dungeon.DungeonDescription.DungeonZone zone;
                if (string.Equals(zoneName, "Forest", StringComparison.OrdinalIgnoreCase))
                { data = GameManager.Status.ForestPersistentDungeonData; zone = TeamNimbus.CloudMeadow.Dungeon.DungeonDescription.DungeonZone.Forest; }
                else
                { data = GameManager.Status.SavannahPersistentDungeonData; zone = TeamNimbus.CloudMeadow.Dungeon.DungeonDescription.DungeonZone.Savannah; }
                if (TeamNimbus.CloudMeadow.Persistence.PersistentDungeonData.GetDungeonDescriptionResourceBy(zone, floor) == null)
                    throw new ArgumentOutOfRangeException("floor", "Dungeon floor resource does not exist");
                data.SetTargetDungeonFloor(floor);
                LogBuffer.Add(zone + " target floor -> " + floor);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetDungeonFloor failed: " + e.Message); }
        }
        public static void UnlockDungeonProgress(string zoneName)
        {
            try
            {
                var data = string.Equals(zoneName, "Forest", StringComparison.OrdinalIgnoreCase) ? GameManager.Status.ForestPersistentDungeonData : GameManager.Status.SavannahPersistentDungeonData;
                int max = string.Equals(zoneName, "Forest", StringComparison.OrdinalIgnoreCase) ? 3 : 8;
                for (int i = 1; i <= max; i++) { data.SetDiscoveredFloor(i); data.SetUnlockedFastTravelFloor(i); }
                GameManager.Status.MigrationSaveData.UpdateWithPersistentData(); LogBuffer.Add(zoneName + " dungeon floors/fast travel unlocked");
            }
            catch (Exception e) { Plugin.Log.LogWarning("UnlockDungeonProgress failed: " + e.Message); }
        }
        public static string[] GetSceneNames() { try { return Enum.GetNames(typeof(TeamNimbus.CloudMeadow.SceneIDs)); } catch { return new string[0]; } }
        public static void LoadSceneByName(string sceneName)
        {
            try { var scene = (TeamNimbus.CloudMeadow.SceneIDs)Enum.Parse(typeof(TeamNimbus.CloudMeadow.SceneIDs), sceneName, true); GameManager.Instance.LoadScene(scene); LogBuffer.Add("Scene load requested: " + scene); }
            catch (Exception e) { Plugin.Log.LogWarning("LoadSceneByName failed: " + e.Message); }
        }
        public static void RestartCurrentScene() { try { string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; GameManager.Instance.LoadSceneName(scene); LogBuffer.Add("Scene restart requested: " + scene); } catch (Exception e) { Plugin.Log.LogWarning("RestartCurrentScene failed: " + e.Message); } }

        public static TeamNimbus.CloudMeadow.Combat.CombatUnit[] GetCombatUnits()
        { try { return UnityEngine.Object.FindObjectsOfType<TeamNimbus.CloudMeadow.Combat.CombatUnit>(); } catch { return new TeamNimbus.CloudMeadow.Combat.CombatUnit[0]; } }

        public static void HealCombatUnit(TeamNimbus.CloudMeadow.Combat.CombatUnit unit)
        {
            try
            {
                if (unit == null) return;
                var stats = unit.CharacterStats; Type t = stats.GetType(); System.Reflection.FieldInfo hp = null;
                while (t != null && hp == null) { hp = t.GetField("currentHP", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); t = t.BaseType; }
                if (hp == null) throw new MissingFieldException("currentHP"); hp.SetValue(stats, stats.GetMaxHP());
                LogBuffer.Add("Healed combat unit: " + unit.DisplayName);
            }
            catch (Exception e) { Plugin.Log.LogWarning("HealCombatUnit failed: " + e.Message); }
        }

        public static void KillCombatUnit(TeamNimbus.CloudMeadow.Combat.CombatUnit unit)
        { try { if (unit != null) unit.CharacterStats.DamageCurrentHP(unit.CharacterStats.GetCurrentHP() + unit.GetMaxHP()); } catch (Exception e) { Plugin.Log.LogWarning("KillCombatUnit failed: " + e.Message); } }

        public static void ClearCombatStatuses(TeamNimbus.CloudMeadow.Combat.CombatUnit unit)
        {
            try
            {
                if (unit == null) return; var statuses = new System.Collections.Generic.List<TeamNimbus.CloudMeadow.Combat.CombatStatus>(unit.ActiveCombatStatuses);
                for (int i = 0; i < statuses.Count; i++) unit.RemoveActiveCombatStatus(statuses[i]);
            }
            catch (Exception e) { Plugin.Log.LogWarning("ClearCombatStatuses failed: " + e.Message); }
        }
        public static void ClearCombatCooldowns(TeamNimbus.CloudMeadow.Combat.CombatUnit unit)
        { try { if (unit != null && unit.CharacterStats != null) { unit.CharacterStats.ClearAllCooldowns(); LogBuffer.Add("Cooldowns cleared: " + unit.DisplayName); } } catch (Exception e) { Plugin.Log.LogWarning("ClearCombatCooldowns failed: " + e.Message); } }
        public static string[] GetCombatUnitDetails(TeamNimbus.CloudMeadow.Combat.CombatUnit unit)
        {
            var lines = new System.Collections.Generic.List<string>(); if (unit == null) return lines.ToArray();
            try { int i = 0; foreach (var state in unit.CharacterStats.EnumerateAbilityStates()) lines.Add("Ability " + (i++) + ": " + (state.Asset != null ? state.Asset.name : "missing") + " | state " + state.ActiveStateIndex + " | cooldown " + state.TurnsRemainingOnCooldown); foreach (var status in unit.ActiveCombatStatuses) lines.Add("Status: " + status); }
            catch (Exception e) { lines.Add("Combat detail scan failed: " + e.Message); } return lines.ToArray();
        }

        public static string[] GetCalendarEventSummary()
        {
            var lines = new System.Collections.Generic.List<string>();
            try
            {
                var s = GameManager.Status; var scheduler = s.EventScheduler; var date = s.CurrentDateTime;
                foreach (var e in scheduler.GetActiveEventTypesForDay(date)) lines.Add("Event: " + e);
                foreach (var b in scheduler.GetBirthdaysForDay(date)) lines.Add("Birthday: " + b);
                var merchant = scheduler.GetActiveMerchant(date); if (merchant != TeamNimbus.CloudMeadow.Managers.CalendarEvents.ClovertonMerchant.None) lines.Add("Merchant: " + merchant);
                if (lines.Count == 0) lines.Add("No scheduled events today");
            }
            catch (Exception e) { lines.Add("Calendar events unavailable: " + e.Message); }
            return lines.ToArray();
        }
        public static string[] GetCalendarSchedule(int days)
        {
            var lines = new System.Collections.Generic.List<string>();
            try { var s = GameManager.Status; for (int i = 0; i < days; i++) { var date = s.CurrentDateTime.CreateFutureDate(new GameTime.Duration(0, 0, i, 0, 0)); var events = new System.Collections.Generic.List<string>(); foreach (var e in s.EventScheduler.GetActiveEventTypesForDay(date)) events.Add(e.ToString()); foreach (var b in s.EventScheduler.GetBirthdaysForDay(date)) events.Add("Birthday " + b); var merchant = s.EventScheduler.GetActiveMerchant(date); if (merchant != TeamNimbus.CloudMeadow.Managers.CalendarEvents.ClovertonMerchant.None) events.Add("Merchant " + merchant); if (events.Count > 0) lines.Add(date + " | " + string.Join(", ", events.ToArray())); } if (lines.Count == 0) lines.Add("No scheduled events in the next " + days + " days"); }
            catch (Exception e) { lines.Add("Calendar scan failed: " + e.Message); } return lines.ToArray();
        }
        public static string[] GetWeatherForecastSummary()
        {
            var lines = new System.Collections.Generic.List<string>();
            try { var status = GameManager.Status; lines.Add("Forecast days: " + status.DaysOfWeatherPredicted + "/14 | Balloon progress: " + (status.FarmStatus.WeatherBalloon.ResolveProgressToNextPrediction() * 100f).ToString("0") + "%"); var f = status.GetType().GetField("_predictedWeatherCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); var en = f != null ? f.GetValue(status) as System.Collections.IEnumerable : null; int i = 1; if (en != null) foreach (var weather in en) lines.Add("Day +" + (i++) + ": " + weather); }
            catch (Exception e) { lines.Add("Forecast unavailable: " + e.Message); } return lines.ToArray();
        }
        public static void PredictWeather(int days) { try { GameManager.Status.PredictWeather(days); LogBuffer.Add("Weather forecast extended by " + days + " days"); } catch (Exception e) { Plugin.Log.LogWarning(e.Message); } }

        public static bool SetMemberFromString(object target, string memberName, string text)
        {
            try
            {
                if (target == null) return false; var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                Type t = target.GetType(); var prop = t.GetProperty(memberName, flags);
                if (prop != null && prop.CanWrite) { prop.SetValue(target, ConvertText(text, prop.PropertyType), null); return true; }
                var field = t.GetField(memberName, flags); if (field != null && !field.IsInitOnly) { field.SetValue(target, ConvertText(text, field.FieldType)); return true; }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMemberFromString failed: " + e.Message); }
            return false;
        }

        public static string ExportObjectJson(object target, string fileName)
        {
            try
            {
                if (target == null) return "FAILED: target missing"; string root = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx\\plugins\\CloudMeadowCreativeMode\\exports"); System.IO.Directory.CreateDirectory(root);
                foreach (char c in System.IO.Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c, '_'); string path = System.IO.Path.Combine(root, fileName + ".json");
                System.IO.File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(target, true)); LogBuffer.Add("Exported JSON: " + path); return path;
            }
            catch (Exception e) { return "FAILED: " + e.Message; }
        }
        public static bool ImportObjectJson(object target, string path)
        {
            try { if (target == null || !System.IO.File.Exists(path)) return false; UnityEngine.JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(path), target); LogBuffer.Add("Imported JSON: " + path); return true; }
            catch (Exception e) { Plugin.Log.LogWarning("ImportObjectJson failed: " + e.Message); return false; }
        }

        private static object ConvertText(string text, Type type)
        {
            var nullable = Nullable.GetUnderlyingType(type); if (nullable != null) type = nullable;
            if (type == typeof(string)) return text;
            if (type == typeof(bool)) return string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) || text == "1";
            if (type.IsEnum) return Enum.Parse(type, text, true);
            return Convert.ChangeType(text, type, System.Globalization.CultureInfo.InvariantCulture);
        }

        // Movement tweaks
        private static float _speedMultiplier = 1f;
        public static float SpeedMultiplier { get { return _speedMultiplier; } }
        public static void SetSpeedMultiplier(float mult)
        {
            if (mult < 0.1f) mult = 0.1f; if (mult > 50f) mult = 50f;
            _speedMultiplier = mult;
            Banner("Speed x" + mult.ToString("0.##"));
        }
        private static bool _noClip;
        public static bool NoClipEnabled { get { return _noClip; } }
        public static void ToggleNoClip()
        {
            try
            {
                _noClip = !_noClip;
                var pc = UnityEngine.Object.FindObjectOfType<TeamNimbus.CloudMeadow.Controllers.PlayerController>();
                if (pc != null)
                {
                    var colProp = pc.GetType().GetProperty("MovementCollider");
                    var col = colProp != null ? (UnityEngine.CircleCollider2D)colProp.GetValue(pc, null) : null;
                    if (col != null) col.gameObject.SetActive(!_noClip);
                }
                Banner("No Clip: " + (_noClip ? "ON" : "OFF"));
            }
            catch (Exception e) { Plugin.Log.LogWarning("ToggleNoClip failed: " + e.Message); }
        }
        public static void SetMonsterLoyalty(object monster, int loyalty)
        {
            try
            {
                var t = monster.GetType();
                var f = t.GetField("loyalty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null)
                {
                    if (loyalty < -30) loyalty = -30; if (loyalty > 110) loyalty = 110;
                    f.SetValue(monster, loyalty);
                    var fIsLoyal = t.GetField("isLoyal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (fIsLoyal != null) fIsLoyal.SetValue(monster, loyalty >= 100);
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterLoyalty failed: " + e.Message); }
        }

        public static void SetMonsterDry(object monster, bool isDry)
        {
            try
            {
                var baseType = monster.GetType().BaseType; // PartyCharacterStats
                var f = baseType.GetField("tempPassiveBuffs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                // Fallback: try SetBaseStat Dry stat
                var statEnum = Type.GetType("TeamNimbus.CloudMeadow.StatModifiers, Game");
                var dryVal = statEnum != null ? Enum.Parse(statEnum, "Dry") : null;
                var setBase = baseType.GetMethod("SetBaseStat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (setBase != null && dryVal != null)
                {
                    setBase.Invoke(monster, new object[] { dryVal, isDry ? 1f : 0f });
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterDry failed: " + e.Message); }
        }

        public static void SetMonsterInfertile(object monster, bool infertile)
        {
            try
            {
                var baseType = monster.GetType().BaseType;
                var statEnum = Type.GetType("TeamNimbus.CloudMeadow.StatModifiers, Game");
                var infVal = statEnum != null ? Enum.Parse(statEnum, "Infertile") : null;
                var setBase = baseType.GetMethod("SetBaseStat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (setBase != null && infVal != null)
                {
                    setBase.Invoke(monster, new object[] { infVal, infertile ? 1f : 0f });
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterInfertile failed: " + e.Message); }
        }

        public static void SetMonsterIsLoyal(object monster, bool isLoyal)
        {
            try
            {
                var t = monster.GetType();
                var f = t.GetField("isLoyal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null) f.SetValue(monster, isLoyal);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterIsLoyal failed: " + e.Message); }
        }

        public static void SetYear(int year)
        {
            try
            {
                if (year < 1) year = 1;
                var cal = GameManager.Status.GetCalendarDate;
                var cur = cal.DateTime;
                if (year <= cur.Year)
                {
                    // Only support moving forward in time to avoid engine edge cases
                    Banner("Year change supports forward only");
                    return;
                }
                int delta = year - cur.Year;
                var future = cur.CreateFutureDate(GameTime.CreateNewDuration(numYears: delta));
                int minutes = GameTime.CreateDurationBetweenDates(cur, future).AsMinutes;
                cal.TickMinutes(minutes);
                LogBuffer.Add("Year -> " + future.Year);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetYear failed: " + e.Message); }
        }

        public static void WinCombat()
        {
            try
            {
                var csm = UnityEngine.Object.FindObjectOfType<TeamNimbus.CloudMeadow.Combat.CombatSceneManager>();
                if (csm != null)
                {
                    // Brutal method: kill all enemies
                    var dbg = typeof(TeamNimbus.CloudMeadow.Combat.DebugCheats);
                    var kill = dbg.GetMethod("Kill");
                    var targetMode = dbg.GetNestedType("TargetMode");
                    if (kill != null && targetMode != null)
                    {
                        var enemies = Enum.Parse(targetMode, "Enemies");
                        kill.Invoke(null, new object[] { enemies });
                        Banner("Combat: Victory forced");
                        return;
                    }
                }
                Banner("Combat: Not in combat or API missing");
            }
            catch (Exception e) { Plugin.Log.LogWarning("WinCombat failed: " + e.Message); }
        }

        public static void SetMonsterLevel(MonsterCharacterStats monster, int level)
        {
            try
            {
                if (monster == null) return; if (level < 1) level = 1; if (level > GameManager.MaxLevel) level = GameManager.MaxLevel;
                Type t = monster.GetType(); System.Reflection.FieldInfo field = null;
                while (t != null && field == null) { field = t.GetField("level", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); t = t.BaseType; }
                if (field == null) throw new MissingFieldException("level"); field.SetValue(monster, level);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterLevel failed: " + e.Message); }
        }
        public static string[] GetMonsterFamilySummary(MonsterCharacterStats monster)
        {
            var lines = new System.Collections.Generic.List<string>(); if (monster == null) return lines.ToArray();
            try
            {
                lines.Add("ID: " + monster.PartyCharacterID + " | Birth: " + monster.BirthDate + " | Status: " + monster.CurrentStatus + " | HP " + monster.GetCurrentHP().ToString("0") + "/" + monster.GetMaxHP().ToString("0"));
                lines.Add("Parents: " + (monster.ParentsUnknown ? "unknown" : monster.FirstParentID + " + " + monster.SecondParentID) + " | Home group: " + monster.HomeGroupID + " | Barn: " + monster.IsAssignedToTheBarn);
                lines.Add("Job: " + monster.ActiveJob + " | Pregnant/incubating: " + monster.IsPregnant);
                var all = GetActiveMonsters(); var children = new System.Collections.Generic.List<string>();
                for (int i = 0; i < all.Length; i++) if (all[i] != null && (all[i].FirstParentID == monster.PartyCharacterID || all[i].SecondParentID == monster.PartyCharacterID)) children.Add(all[i].Name + " (#" + all[i].PartyCharacterID + ")");
                lines.Add("Children: " + (children.Count == 0 ? "none" : string.Join(", ", children.ToArray())));
            }
            catch (Exception e) { lines.Add("Family scan failed: " + e.Message); }
            return lines.ToArray();
        }
        public static bool SetMonsterParents(MonsterCharacterStats monster, int first, int second)
        {
            try
            {
                if (monster == null || first == monster.PartyCharacterID || second == monster.PartyCharacterID || first == second) return false;
                if (WouldCreateFamilyCycle(monster.PartyCharacterID, first) || WouldCreateFamilyCycle(monster.PartyCharacterID, second)) return false;
                SetMonsterParentsUnchecked(monster, first, second); LogBuffer.Add("Parents updated for " + monster.Name + ": " + first + ", " + second); return true;
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterParents failed: " + e.Message); return false; }
        }
        public static void ClearMonsterParents(MonsterCharacterStats monster) { if (monster != null) { SetMonsterParentsUnchecked(monster, -1, -1); LogBuffer.Add("Parents cleared for " + monster.Name); } }
        private static void SetMonsterParentsUnchecked(MonsterCharacterStats monster, int first, int second)
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var f1 = typeof(MonsterCharacterStats).GetField("firstParentID", flags); var f2 = typeof(MonsterCharacterStats).GetField("secondParentID", flags);
            if (f1 == null || f2 == null) throw new MissingFieldException("monster parent IDs"); f1.SetValue(monster, first); f2.SetValue(monster, second);
        }
        private static bool WouldCreateFamilyCycle(int childId, int proposedParent)
        {
            if (proposedParent < 0) return false; var all = GetActiveMonsters(); var stack = new System.Collections.Generic.Stack<int>(); var seen = new System.Collections.Generic.HashSet<int>(); stack.Push(proposedParent);
            while (stack.Count > 0) { int id = stack.Pop(); if (id == childId) return true; if (!seen.Add(id)) continue; for (int i = 0; i < all.Length; i++) if (all[i] != null && all[i].PartyCharacterID == id) { if (all[i].FirstParentID >= 0) stack.Push(all[i].FirstParentID); if (all[i].SecondParentID >= 0) stack.Push(all[i].SecondParentID); break; } }
            return false;
        }
        public static string ExportFamilyTree(string path)
        {
            try { var lines = new System.Collections.Generic.List<string>(); lines.Add("ID\tName\tSpecies\tFirstParent\tSecondParent"); foreach (var m in GetActiveMonsters()) if (m != null) lines.Add(m.PartyCharacterID + "\t" + m.Name + "\t" + m.FarmableSpecies + "\t" + m.FirstParentID + "\t" + m.SecondParentID); System.IO.File.WriteAllLines(path, lines.ToArray()); return path; }
            catch (Exception e) { return "FAILED: " + e.Message; }
        }
        public static string[] GetFarmBuildingSummary()
        {
            var lines = new System.Collections.Generic.List<string>();
            try { var buildings = GameManager.Status.FarmStatus.SpecialBuildings; for (int i = 0; i < buildings.Count; i++) lines.Add("Slot " + i + ": " + buildings[i].BuildingType + " | Built: " + buildings[i].HasBuilding + " | Name: " + buildings[i].Name); }
            catch (Exception e) { lines.Add("Building scan failed: " + e.Message); } return lines.ToArray();
        }
        public static void SetFarmBuildingType(int index, string typeName)
        {
            try { var buildings = GameManager.Status.FarmStatus.SpecialBuildings; if (index < 0 || index >= buildings.Count) return; var type = (TeamNimbus.CloudMeadow.Persistence.FarmBuildingTypes)Enum.Parse(typeof(TeamNimbus.CloudMeadow.Persistence.FarmBuildingTypes), typeName, true); buildings[index].UpdateBuildingType(type); MarkPendingFarmLayoutRefresh(); LogBuffer.Add("Farm building slot " + index + " -> " + type); }
            catch (Exception e) { Plugin.Log.LogWarning("SetFarmBuildingType failed: " + e.Message); }
        }
        public static string[] AuditFarmIntegrity()
        {
            var lines = new System.Collections.Generic.List<string>(); int issues = 0;
            try
            {
                var all = GetActiveMonsters(); var ids = new System.Collections.Generic.HashSet<int>();
                for (int i = 0; i < all.Length; i++) if (all[i] != null) { if (!ids.Add(all[i].PartyCharacterID)) { lines.Add("Duplicate monster ID: " + all[i].PartyCharacterID); issues++; } }
                for (int i = 0; i < all.Length; i++) if (all[i] != null && !all[i].ParentsUnknown)
                {
                    if (all[i].FirstParentID == all[i].PartyCharacterID || all[i].SecondParentID == all[i].PartyCharacterID) { lines.Add("Self-parent link: " + all[i].Name); issues++; }
                    if (all[i].FirstParentID >= 0 && !ids.Contains(all[i].FirstParentID)) { lines.Add("Missing first parent: " + all[i].Name + " -> " + all[i].FirstParentID); issues++; }
                    if (all[i].SecondParentID >= 0 && !ids.Contains(all[i].SecondParentID)) { lines.Add("Missing second parent: " + all[i].Name + " -> " + all[i].SecondParentID); issues++; }
                }
                var fs = GameManager.Status.FarmStatus; lines.Add("Monsters reported: " + GameManager.Status.NumMonstersOnTheFarm + " | Enumerated: " + all.Length + " | Breeding couples: " + GameManager.Status.BreedingCouples.Count + " | Buildings: " + fs.SpecialBuildings.Count + " | Eggs: " + GetIncubatorEggs().Length);
                if (GameManager.Status.NumMonstersOnTheFarm != all.Length) { lines.Add("Monster count mismatch detected"); issues++; }
            }
            catch (Exception e) { lines.Add("Farm audit failed: " + e.Message); issues++; }
            lines.Insert(0, issues == 0 ? "Farm integrity: OK" : "Farm integrity issues: " + issues); return lines.ToArray();
        }
        public static string GetFarmCapacitySummary()
        { try { int used = GameManager.Status.NumMonstersOnTheFarm, max = GameManager.Status.FarmStatus.ResolveNumberOfMonsterSpotsOnFarm(), eggs = GetIncubatorEggs().Length; return "Farm capacity: " + used + "/" + max + " | Incubator eggs: " + eggs + " | Free after all hatch: " + (max - used - eggs); } catch (Exception e) { return "Farm capacity unavailable: " + e.Message; } }
        public static string[] GetFarmPlotSummary()
        {
            var lines = new System.Collections.Generic.List<string>();
            try { var plots = GameManager.Status.FarmStatus.Plots; for (int i = 0; i < plots.Length; i++) { var p = plots[i]; int planted = 0, watered = 0; for (int j = 0; j < p.Plants.Length; j++) { if (p.Plants[j].CurrentStatus != TeamNimbus.CloudMeadow.Farm.CropStatus.Dirt) planted++; if (p.Plants[j].IsWatered) watered++; } lines.Add("Plot " + i + " | " + p.BuildingType + " | Lv " + p.UpgradeLevel + " | Active " + p.GetNumActivePlantingSpots() + " | Planted " + planted + " | Watered " + watered); } }
            catch (Exception e) { lines.Add("Plot scan failed: " + e.Message); } return lines.ToArray();
        }
        public static void UpgradeFarmPlot(int index) { try { var p = GameManager.Status.FarmStatus.Plots[index]; p.UpgradeField(); MarkPendingFarmLayoutRefresh(); LogBuffer.Add("Farm plot upgraded: " + index); } catch (Exception e) { Plugin.Log.LogWarning(e.Message); } }
        public static void WaterFarmPlot(int index) { try { var p = GameManager.Status.FarmStatus.Plots[index]; for (int i = 0; i < p.Plants.Length; i++) p.Plants[i].Water(); LogBuffer.Add("Farm plot watered: " + index); } catch (Exception e) { Plugin.Log.LogWarning(e.Message); } }
        public static void GrowFarmPlot(int index) { try { var p = GameManager.Status.FarmStatus.Plots[index]; for (int i = 0; i < p.Plants.Length; i++) if (p.Plants[i].CurrentStatus != TeamNimbus.CloudMeadow.Farm.CropStatus.Dirt) p.Plants[i].ForceCropChange(p.Plants[i].CropType, TeamNimbus.CloudMeadow.Farm.CropStatus.Harvestable); LogBuffer.Add("Farm plot grown: " + index); } catch (Exception e) { Plugin.Log.LogWarning(e.Message); } }
        public static void ResetFarmPlot(int index) { try { var p = GameManager.Status.FarmStatus.Plots[index]; for (int i = 0; i < p.Plants.Length; i++) p.Plants[i].ResetToDirt(true); LogBuffer.Add("Farm plot reset: " + index); } catch (Exception e) { Plugin.Log.LogWarning(e.Message); } }
        public static void AssignMonsterToPlot(MonsterCharacterStats monster, int plotIndex, string roleName)
        {
            try
            {
                if (monster == null) return; var plot = GameManager.Status.FarmStatus.Plots[plotIndex]; var role = (TeamNimbus.CloudMeadow.Monsters.JobRole)Enum.Parse(typeof(TeamNimbus.CloudMeadow.Monsters.JobRole), roleName, true);
                foreach (var existing in GameManager.Status.EnumerateWorkersAssignedToWorkable(plot.JobID)) if (existing != null && existing != monster && existing.ActiveJob.AssignedRole == role) existing.QuitJob();
                if (monster.ActiveJob.IsWorking) monster.QuitJob(); monster.BeginJob(plot, role); LogBuffer.Add(monster.Name + " assigned to plot " + plotIndex + " / " + role);
            }
            catch (Exception e) { Plugin.Log.LogWarning("AssignMonsterToPlot failed: " + e.Message); }
        }
        public static void QuitMonsterJob(MonsterCharacterStats monster) { try { if (monster != null) { monster.QuitJob(); LogBuffer.Add(monster.Name + " quit job"); } } catch (Exception e) { Plugin.Log.LogWarning(e.Message); } }
        public static bool Ready { get { return Application.isPlaying && GameManager.Instance != null && GameManager.IsGameStatusLoaded; } }
        public static bool VerboseDiagnosticsEnabled
        {
            get { return Plugin.VerboseDiagnostics != null && Plugin.VerboseDiagnostics.Value; }
        }

        public static void SetVerboseDiagnostics(bool enabled)
        {
            if (Plugin.VerboseDiagnostics != null) Plugin.VerboseDiagnostics.Value = enabled;
            Banner("Verbose diagnostics: " + (enabled ? "ON" : "OFF"));
        }

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

        public static SexSceneData[] GetGalleryScenes()
        {
            try
            {
                if (GameManager.SexSceneLibrary == null) return new SexSceneData[0];
                var list = new System.Collections.Generic.List<SexSceneData>();
                foreach (var scene in GameManager.SexSceneLibrary.EnumerateAllScenes()) if (scene != null) list.Add(scene);
                return list.ToArray();
            }
            catch { return new SexSceneData[0]; }
        }

        public static bool IsGallerySceneUnlocked(SexSceneData scene)
        { try { return scene != null && SaveGameManager.IsSceneUnlocked(scene); } catch { return false; } }

        public static void UnlockGalleryScene(SexSceneData scene)
        {
            if (scene == null) return;
            try
            {
                var field = typeof(SaveGameManager).GetField("s_globalSettings", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                object settings = field != null ? field.GetValue(null) : null;
                if (settings == null) throw new InvalidOperationException("Global gallery settings are not loaded");
                var method = settings.GetType().GetMethod("UnlockSexScene", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (method == null) throw new MissingMethodException(settings.GetType().FullName, "UnlockSexScene");
                method.Invoke(settings, new object[] { scene });
                var save = typeof(SaveGameManager).GetMethod("SaveGlobalSettings", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (save != null) save.Invoke(null, null);
                LogBuffer.Add("Gallery scene unlocked: " + GetGallerySceneName(scene));
            }
            catch (Exception e) { Plugin.Log.LogWarning("Unlock gallery scene failed: " + e); }
        }

        public static void LockGalleryScene(SexSceneData scene)
        {
            if (scene == null) return;
            try
            {
                var managerField = typeof(SaveGameManager).GetField("s_globalSettings", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                object settings = managerField != null ? managerField.GetValue(null) : null; if (settings == null) throw new InvalidOperationException("Global settings unavailable");
                var listField = settings.GetType().GetField("unlockedSexScenes", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var list = listField != null ? listField.GetValue(settings) as System.Collections.IList : null; if (list == null) throw new MissingFieldException("unlockedSexScenes");
                for (int i = list.Count - 1; i >= 0; i--) if (object.Equals(list[i], scene.UniqueID)) list.RemoveAt(i);
                var save = typeof(SaveGameManager).GetMethod("SaveGlobalSettings", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic); if (save != null) save.Invoke(null, null);
                LogBuffer.Add("Gallery scene locked: " + GetGallerySceneName(scene));
            }
            catch (Exception e) { Plugin.Log.LogWarning("LockGalleryScene failed: " + e.Message); }
        }

        public static string GetGallerySceneName(SexSceneData scene)
        {
            if (scene == null) return "(null)";
            try
            {
                string asset = scene.AssetNameInBundle;
                if (!string.IsNullOrEmpty(asset)) return asset;
                return scene.FirstCharacterFilterFlags + " + " + scene.SecondCharacterFilterFlags;
            }
            catch { return scene.name; }
        }
        public static string GetGallerySceneDiagnostics(SexSceneData scene)
        {
            if (scene == null) return "Scene missing"; try { string bundle = scene.ResolveBundleName(); string root = UnityEngine.Application.streamingAssetsPath; string[] candidates = { System.IO.Path.Combine(root, bundle), System.IO.Path.Combine(root, bundle + ".bundle"), System.IO.Path.Combine(root, bundle + ".assetbundle") }; bool found = false; for (int i = 0; i < candidates.Length; i++) if (System.IO.File.Exists(candidates[i])) found = true; return "Characters: " + scene.FirstCharacterFilterFlags + " + " + scene.SecondCharacterFilterFlags + " | Bundle: " + bundle + " (" + (found ? "file found" : "managed/packed") + ") | Asset: " + scene.AssetNameInBundle; } catch (Exception e) { return "Scene diagnostics failed: " + e.Message; }
        }
        public static void PreviewGalleryScene(SexSceneData scene)
        {
            try { var managers = UnityEngine.Object.FindObjectsOfType<TeamNimbus.CloudMeadow.UI.SexSceneWindowManager>(); if (managers == null || managers.Length == 0) throw new InvalidOperationException("SexSceneWindowManager is not loaded in this scene"); managers[0].ShowDefaultSexScene(scene); LogBuffer.Add("Gallery preview: " + GetGallerySceneName(scene)); }
            catch (Exception e) { Plugin.Log.LogWarning("PreviewGalleryScene failed: " + e.Message); LogBuffer.AddError("Gallery preview", e.Message, e.StackTrace); }
        }

        public static string[] GetCompatibilityChecks()
        {
            var result = new System.Collections.Generic.List<string>();
            int missing = 0;
            string[] types = {
                "TeamNimbus.CloudMeadow.Managers.GameManager", "TeamNimbus.CloudMeadow.Persistence.SaveGameManager",
                "TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats", "TeamNimbus.CloudMeadow.Story.QuestSystem.QuestInfo",
                "TeamNimbus.CloudMeadow.UI.SexSceneDataLibrary"
            };
            var asm = typeof(GameManager).Assembly;
            for (int i = 0; i < types.Length; i++) { bool ok = asm.GetType(types[i], false) != null; if (!ok) missing++; result.Add((ok ? "OK  " : "MISS  ") + types[i]); }
            bool statusOk = typeof(GameManager).GetProperty("Status", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public) != null; if (!statusOk) missing++; result.Add((statusOk ? "OK" : "MISS") + "  GameManager.Status");
            bool unlockOk = typeof(SaveGameManager).GetMethod("UnlockEverything", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public) != null; if (!unlockOk) missing++; result.Add((unlockOk ? "OK" : "MISS") + "  SaveGameManager.UnlockEverything");
            var questStart = typeof(TeamNimbus.CloudMeadow.Story.QuestSystem.QuestManager).GetMethod("StartQuest", new[] { typeof(TeamNimbus.CloudMeadow.Story.QuestSystem.QuestInfo) }); bool questOk = questStart != null; if (!questOk) missing++; result.Add((questOk ? "OK" : "MISS") + "  QuestManager.StartQuest(QuestInfo)");
            result.Insert(0, "Compatibility: " + (missing == 0 ? "COMPATIBLE" : missing <= 2 ? "PARTIAL" : "BROKEN") + " | Missing signatures: " + missing);
            return result.ToArray();
        }

        public static string[] GetContentDiscoverySummary()
        {
            var lines = new System.Collections.Generic.List<string>();
            try { lines.Add("Species enum values: " + Enum.GetNames(typeof(FarmableSpecies)).Length); } catch { }
            try { lines.Add("Item definitions: " + GetAllItemDefinitions().Length); } catch { }
            try { lines.Add("Trait definitions: " + GetAllTraitDefinitions().Length); } catch { }
            try { lines.Add("Quest definitions: " + GameApiQuest.GetAllQuests().Length); } catch { }
            try { lines.Add("Gallery scenes: " + GetGalleryScenes().Length); } catch { }
            return lines.ToArray();
        }
        public static string WriteCompatibilityReport(string path)
        {
            try
            {
                var lines = new System.Collections.Generic.List<string>(); lines.Add(GetCompatibilitySummary()); lines.AddRange(GetCompatibilityChecks());
                lines.Add("FarmableSpecies=" + string.Join(",", Enum.GetNames(typeof(FarmableSpecies)))); lines.Add("SceneIDs=" + string.Join(",", Enum.GetNames(typeof(TeamNimbus.CloudMeadow.SceneIDs)))); lines.Add("Weather=" + string.Join(",", Enum.GetNames(typeof(Weather))));
                if (Ready) { lines.Add("Quests=" + GameApiQuest.GetAllQuests().Length); lines.Add("Items=" + GetAllItemDefinitions().Length); lines.Add("Traits=" + GetAllTraitDefinitions().Length); lines.Add("Gallery=" + GetGalleryScenes().Length); }
                string current = string.Join("\r\n", lines.ToArray()); string previousPath = path + ".previous"; string deltaPath = path + ".changes.txt";
                if (System.IO.File.Exists(path)) { string old = System.IO.File.ReadAllText(path); if (!string.Equals(old, current, StringComparison.Ordinal)) { System.IO.File.WriteAllText(previousPath, old); var oldSet = new System.Collections.Generic.HashSet<string>(old.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)); var changes = new System.Collections.Generic.List<string>(); foreach (string line in lines) if (!oldSet.Contains(line)) changes.Add("NEW/CHANGED: " + line); System.IO.File.WriteAllLines(deltaPath, changes.ToArray()); } }
                System.IO.File.WriteAllText(path, current); LogBuffer.Add("Compatibility report: " + path); return path;
            }
            catch (Exception e) { return "FAILED: " + e.Message; }
        }

        public static void AddMonster(string speciesName, int level)
        {
            try
            {
                if (string.IsNullOrEmpty(speciesName)) speciesName = "Chimera";
                if (level < 1) level = 1;

                FarmableSpecies species = (FarmableSpecies)Enum.Parse(typeof(FarmableSpecies), speciesName, true);
                TeamNimbus.CloudMeadow.Combat.DebugCheats.AddMonster(species, level, Gender.Other);
                LogBuffer.Add("Added monster: " + species + " Lv" + level);
                Banner("Added monster: " + species + " Lv" + level);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("AddMonster failed: " + e.Message);
            }
        }

        public static string[] GetSpeciesTraitNamesForSpecies(string speciesName)
        {
            var defs = GetSpeciesTraitDefinitionsForSpecies(speciesName);
            var names = new System.Collections.Generic.List<string>();
            int i;
            for (i = 0; i < defs.Length; i++)
            {
                var def = defs[i];
                string name = ReadStringFromTraitDefinition(def);
                if (!string.IsNullOrEmpty(name) && names.IndexOf(name) < 0) names.Add(name);
            }
            if (names.Count == 0 && string.Equals(speciesName, "Chimera", StringComparison.OrdinalIgnoreCase))
            {
                names.Add("QuickOnTheWing");
                names.Add("AlchemicalGenes");
            }
            return names.ToArray();
        }

        public static object[] GetSpeciesTraitDefinitionsForSpeciesUI(string speciesName)
        {
            return GetSpeciesTraitDefinitionsForSpecies(speciesName);
        }

        public static object[] GetStatLimitTraitDefinitionsForSpeciesUI(string speciesName)
        {
            try
            {
                var statLimitType = Type.GetType("TeamNimbus.CloudMeadow.Traits.BaseStatLimitTraitDefinition, Game");
                var list = new System.Collections.Generic.List<object>();
                object[] source;
                if (string.Equals(speciesName, "Chimera", StringComparison.OrdinalIgnoreCase))
                {
                    source = GetAllBloodlineTraitDefinitionsForAllSpecies();
                }
                else
                {
                    var grouped = GroupBloodlineTraitsBySpecies(GetAllBloodlineTraitDefinitionsForAllSpecies());
                    source = grouped.ContainsKey(speciesName) ? grouped[speciesName].ToArray() : new object[0];
                }

                int i;
                for (i = 0; i < source.Length; i++)
                {
                    var d = source[i];
                    if (d != null && statLimitType != null && statLimitType.IsInstanceOfType(d)) list.Add(d);
                }
                return DedupTraitDefinitionsByCode(list.ToArray());
            }
            catch { }
            return new object[0];
        }

        public static object[] GetBloodlineTraitDefinitionsForSpeciesUI(string speciesName)
        {
            try
            {
                var grouped = GroupBloodlineTraitsBySpecies(GetAllBloodlineTraitDefinitionsForAllSpecies());
                var source = grouped.ContainsKey(speciesName) ? grouped[speciesName].ToArray() : new object[0];
                var statLimitType = Type.GetType("TeamNimbus.CloudMeadow.Traits.BaseStatLimitTraitDefinition, Game");
                var list = new System.Collections.Generic.List<object>();
                int i;
                for (i = 0; i < source.Length; i++)
                {
                    var d = source[i];
                    if (d == null) continue;
                    if (statLimitType != null && statLimitType.IsInstanceOfType(d)) continue;
                    list.Add(d);
                }
                return DedupTraitDefinitionsByCode(list.ToArray());
            }
            catch { }
            return new object[0];
        }

        public static void SpawnChimeraVariant(string variantName, int level)
        {
            try
            {
                Diag("SpawnChimeraVariant request: " + variantName + " Lv" + level);
                var monster = SpawnMonsterAndReturnRef(FarmableSpecies.Chimera, level);
                if (monster == null)
                {
                    Banner("Chimera spawn failed");
                    return;
                }

                var defs = GetSpeciesTraitDefinitionsForSpecies("Chimera");
                object selected = FindTraitDefinitionByName(defs, variantName);
                if (selected == null && defs.Length > 0) selected = defs[0];

                if (selected != null)
                {
                    SetMonsterSpeciesTrait(monster, selected, 1);
                    Diag("SpawnChimeraVariant applied species trait: " + ReadStringFromTraitDefinition(selected));
                    Banner("Spawned Chimera: " + ReadStringFromTraitDefinition(selected) + " Lv" + level);
                    return;
                }

                Banner("Spawned Chimera Lv" + level);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("SpawnChimeraVariant failed: " + e.Message);
            }
        }

        public static bool SetChimeraVariant(object monster, string variantName, int grade)
        {
            try
            {
                if (monster == null || string.IsNullOrEmpty(variantName)) return false;
                if (!string.Equals(GetMonsterSpecies(monster), "Chimera", StringComparison.OrdinalIgnoreCase)) return false;

                var defs = GetSpeciesTraitDefinitionsForSpecies("Chimera");
                var selected = FindTraitDefinitionByName(defs, variantName);
                if (selected == null) return false;

                Diag("SetChimeraVariant -> " + variantName + " grade " + grade);
                return SetMonsterSpeciesTrait(monster, selected, grade < 1 ? 1 : grade);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("SetChimeraVariant failed: " + e.Message);
            }
            return false;
        }

        private static MonsterCharacterStats SpawnMonsterAndReturnRef(FarmableSpecies species, int level)
        {
            try
            {
                var before = new System.Collections.Generic.HashSet<MonsterCharacterStats>();
                foreach (var m in GameManager.Status.EnumerateActiveMonsters())
                {
                    if (m != null) before.Add(m);
                }

                TeamNimbus.CloudMeadow.Combat.DebugCheats.AddMonster(species, level, Gender.Other);

                foreach (var m2 in GameManager.Status.EnumerateActiveMonsters())
                {
                    if (m2 != null && !before.Contains(m2)) return m2;
                }
            }
            catch { }
            return null;
        }

        private static object[] GetSpeciesTraitDefinitionsForSpecies(string speciesName)
        {
            try
            {
                var lib = GameManager.MonsterTraitLibrary;
                if (lib == null) return new object[0];

                FarmableSpecies species = (FarmableSpecies)Enum.Parse(typeof(FarmableSpecies), speciesName, true);
                var traitsByType = ReflectionUtil.GetPrivateMethod(lib, "ResolveMonsterTraitsByType").Invoke(lib, new object[] { species });
                if (traitsByType == null) return new object[0];

                // Special species traits can be stored separately from OtherSpeciesTraits.
                var list = new System.Collections.Generic.List<object>();
                var fields = traitsByType.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].Name.IndexOf("Species", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    object value = fields[i].GetValue(traitsByType);
                    var collection = value as System.Collections.IEnumerable;
                    if (collection != null && !(value is string))
                    {
                        foreach (var item in collection) if (item != null) list.Add(item);
                    }
                    else if (value != null) list.Add(value);
                }
                return DedupTraitDefinitionsByCode(FilterToDefinitionLike(list.ToArray()));
            }
            catch { }
            return new object[0];
        }

        private static object FindTraitDefinitionByName(object[] defs, string name)
        {
            if (defs == null || defs.Length == 0) return null;
            int i;
            for (i = 0; i < defs.Length; i++)
            {
                string cur = ReadStringFromTraitDefinition(defs[i]);
                if (string.Equals(cur, name, StringComparison.OrdinalIgnoreCase)) return defs[i];
            }
            for (i = 0; i < defs.Length; i++)
            {
                string cur2 = ReadStringFromTraitDefinition(defs[i]);
                if (!string.IsNullOrEmpty(cur2) && !string.IsNullOrEmpty(name) && cur2.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return defs[i];
            }
            return null;
        }

        private static string ReadStringFromTraitDefinition(object def)
        {
            if (def == null) return string.Empty;
            var name = SafeProp(def, "DisplayName") ?? SafeProp(def, "Name") ?? SafeProp(def, "Code") ?? SafeProp(def, "TraitCode");
            return name != null ? name.ToString() : def.ToString();
        }

        private static bool SetMonsterSpeciesTrait(object monster, object traitDefinition, int grade)
        {
            try
            {
                var def = UnwrapTraitDefinition(traitDefinition) as TeamNimbus.CloudMeadow.Traits.BaseTraitDefinition;
                if (def == null || monster == null) return false;

                var trait = new TeamNimbus.CloudMeadow.Traits.TraitInstance(def, Mathf.Clamp(grade, 1, 5));
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var field = monster.GetType().GetField("_speciesTrait", flags);
                if (field == null) return false;

                field.SetValue(monster, trait);
                SyncSpecialSpeciesTraitState(monster, def);
                RefreshMonsterAfterTrait(monster);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("SetMonsterSpeciesTrait failed: " + e.Message);
            }
            return false;
        }

        private static void SyncSpecialSpeciesTraitState(object monster, TeamNimbus.CloudMeadow.Traits.BaseTraitDefinition def)
        {
            try
            {
                if (monster == null) return;

                var m = monster as MonsterCharacterStats;
                if (m == null) return;

                int abilityIndex = -1;
                int stateIndex = 0;
                if (m.FarmableSpecies == FarmableSpecies.Chimera)
                {
                    abilityIndex = 3;
                    stateIndex = (def != null && object.ReferenceEquals(def, GameManager.MonsterTraitLibrary.LeechingCompoundTrait)) ? 1 : 0;
                }
                else if (m.FarmableSpecies == FarmableSpecies.Holstaur)
                {
                    abilityIndex = 2;
                    stateIndex = (def != null && object.ReferenceEquals(def, GameManager.MonsterTraitLibrary.MadCowTrait)) ? 1 : 0;
                }

                if (abilityIndex >= 0)
                {
                    var changeState = m.GetType().BaseType.GetMethod("ChangeAbilityState", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (changeState != null) changeState.Invoke(m, new object[] { abilityIndex, stateIndex });
                }
            }
            catch { }
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

        public static void UpgradeAllAbilitiesForParty()
        {
            try
            {
                // Try direct cheat/API
                var dbg = typeof(TeamNimbus.CloudMeadow.Combat.DebugCheats);
                var m = dbg.GetMethod("UpgradeAllAbilitiesForParty") ?? dbg.GetMethod("UpgradePartyAbilities") ?? dbg.GetMethod("MaxAbilitiesForParty");
                if (m != null) { try { m.Invoke(null, null); Banner("Upgraded party abilities"); return; } catch { } }

                // Fallback: reflect party and upgrade per member
                var gm = TeamNimbus.CloudMeadow.Managers.GameManager.Instance;
                object party = null;
                var gmType = gm.GetType();
                var pProp = gmType.GetProperty("PartyManager") ?? gmType.GetProperty("Party");
                if (pProp != null) party = pProp.GetValue(gm, null);
                if (party != null)
                {
                    var membersProp = party.GetType().GetProperty("Members");
                    var list = membersProp != null ? membersProp.GetValue(party, null) as System.Collections.IEnumerable : null;
                    if (list != null)
                    {
                        foreach (var member in list)
                        {
                            if (member == null) continue;
                            var abProp = member.GetType().GetProperty("Abilities");
                            var abHolder = abProp != null ? abProp.GetValue(member, null) : null;
                            if (abHolder == null) continue;
                            var upg = abHolder.GetType().GetMethod("UpgradeAll", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                   ?? abHolder.GetType().GetMethod("MaxAll")
                                   ?? abHolder.GetType().GetMethod("UpgradeEverything");
                            if (upg != null) { try { upg.Invoke(abHolder, null); } catch { } }
                        }
                        Banner("Upgraded party abilities");
                        return;
                    }
                }
                Banner("Upgrade abilities: attempted");
            }
            catch (Exception e) { Plugin.Log.LogWarning("UpgradeAllAbilitiesForParty failed: " + e.Message); }
        }

        // ===== Inventory helpers =====
        public static object[] GetInventoryEntries()
        { return GetInventoryEntriesFrom(GameManager.Status.Inventory); }

        public static object[] GetInventoryEntriesFrom(object inv)
        {
            try
            {
                if (inv == null) return new object[0];
                var list = new System.Collections.Generic.List<object>();
                var seen = new System.Collections.Generic.HashSet<object>();
                var t = inv.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    object val = null; try { val = p.GetValue(inv, null); } catch { }
                    AppendEntryEnumerable(list, seen, val);
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    object val = null; try { val = f.GetValue(inv); } catch { }
                    AppendEntryEnumerable(list, seen, val);
                }
                return list.ToArray();
            }
            catch (Exception e) { Plugin.Log.LogWarning("GetInventoryEntries failed: " + e.Message); }
            return new object[0];
        }

        public static object[] GetInventoryContainers()
        {
            try
            {
                var list = new System.Collections.Generic.List<object>(); var status = GameManager.Status;
                if (status.Inventory != null) list.Add(status.Inventory); if (status.Storage != null && !list.Contains(status.Storage)) list.Add(status.Storage);
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                var fields = status.GetType().GetFields(flags); for (int i = 0; i < fields.Length; i++) { object v = null; try { v = fields[i].GetValue(status); } catch { } if (v is Inventory && !list.Contains(v)) list.Add(v); }
                var sceneInventories = UnityEngine.Object.FindObjectsOfType<TeamNimbus.CloudMeadow.Inventory.InventoryMerchantBehaviour>();
                for (int i = 0; i < sceneInventories.Length; i++) { object stock = null; try { stock = status.ResolveInventoryStock(sceneInventories[i]); } catch { } if (stock != null && !list.Contains(stock)) list.Add(stock); }
                return list.ToArray();
            }
            catch { return new object[0]; }
        }
        public static string GetInventoryContainerName(object container)
        {
            try { if (object.ReferenceEquals(container, GameManager.Status.Inventory)) return "Player Inventory"; if (object.ReferenceEquals(container, GameManager.Status.Storage)) return "Farm Storage"; return container != null ? container.GetType().Name : "(missing)"; }
            catch { return container != null ? container.GetType().Name : "(missing)"; }
        }

        private static void AppendEntryEnumerable(System.Collections.Generic.List<object> list, System.Collections.Generic.HashSet<object> seen, object val)
        {
            if (val == null) return;
            var en = val as System.Collections.IEnumerable; if (en == null || val is string) return;
            foreach (var it in en)
            {
                if (it == null) continue;
                var tn = it.GetType().FullName;
                if (tn != null && (tn.IndexOf("Entry") >= 0 || tn.IndexOf("ItemEntry") >= 0) && seen.Add(it)) list.Add(it);
            }
        }

        public static string GetEntryInspectorSummary(object entry)
        {
            try
            {
                var def = GetEntryDefinition(entry) as BaseItemDefinition; object code = def != null ? (SafeProp(def, "Code") ?? SafeProp(def, "name")) : null;
                object sell = entry != null ? (SafeProp(entry, "KoronaValueOfOne") ?? SafeProp(entry, "SellValue") ?? SafeProp(def, "SellValue")) : null;
                return "Type: " + (entry != null ? entry.GetType().Name : "missing") + " | ID: " + (code ?? "unknown") + " | Qty: " + GetEntryQuantity(entry) + " | Quality: " + GetEntryQuality(entry) + " | Value: " + (sell ?? "n/a");
            }
            catch (Exception e) { return "Item inspection failed: " + e.Message; }
        }

        private static object CreateEntry(object def, int amount, int qualityIndex)
        {
            try
            {
                var baseDef = def as BaseItemDefinition;
                if (baseDef == null) return null;

                ItemQuality quality = ResolveNearestAllowedQuality(baseDef, qualityIndex);
                return InventoryEntryExtensions.CreateItemEntry(baseDef, amount, quality);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("CreateEntry failed: " + e.Message);
                return null;
            }
        }

        private static ItemQuality ResolveNearestAllowedQuality(BaseItemDefinition def, int preferredQualityIndex)
        {
            preferredQualityIndex = ClampQualityIndex(preferredQualityIndex);
            ItemQuality preferred = (ItemQuality)preferredQualityIndex;
            if (def == null) return preferred;
            if (def.ItemAvailableWithQuality(preferred)) return preferred;

            int q;
            for (q = preferredQualityIndex; q >= (int)ItemQuality.OneStar; q--)
            {
                ItemQuality candidate = (ItemQuality)q;
                if (def.ItemAvailableWithQuality(candidate)) return candidate;
            }

            for (q = preferredQualityIndex + 1; q <= (int)ItemQuality.FiveStar; q++)
            {
                ItemQuality candidate2 = (ItemQuality)q;
                if (def.ItemAvailableWithQuality(candidate2)) return candidate2;
            }

            return def.ResolveMinQuality();
        }

        private static ItemQuality ResolveMaxAllowedQuality(BaseItemDefinition def)
        {
            if (def == null) return ItemQuality.OneStar;

            int q;
            for (q = (int)ItemQuality.FiveStar; q >= (int)ItemQuality.OneStar; q--)
            {
                ItemQuality candidate = (ItemQuality)q;
                if (def.ItemAvailableWithQuality(candidate)) return candidate;
            }

            return def.ResolveMinQuality();
        }

        private static int ClampQualityIndex(int q)
        {
            try
            {
                var vals = System.Enum.GetValues(typeof(TeamNimbus.CloudMeadow.Items.ItemQuality));
                int min = (int)vals.GetValue(0); // OneStar = 1
                int max = (int)vals.GetValue(vals.Length - 1);
                if (q < min) q = min;
                if (q > max) q = max;
                return q;
            }
            catch { return 1; }
        }

        private static bool IsSafeItemDefinition(System.Type tDef)
        {
            try
            {
                var simple = typeof(TeamNimbus.CloudMeadow.Items.SimpleItemDefinition);
                var equip = typeof(TeamNimbus.CloudMeadow.Items.EquippableItemDefinition);
                var usable = typeof(TeamNimbus.CloudMeadow.Items.UsableDefinition);
                // safe: Simple/Equippable/Usable; skip Eggs, Quest-only, etc.
                return simple.IsAssignableFrom(tDef) || equip.IsAssignableFrom(tDef) || usable.IsAssignableFrom(tDef);
            }
            catch { return false; }
        }

        // Previously filtered to crops/ingredients/food/seeds. Reverting to enumerate ALL categories as requested.
        private static bool IsAllowedCategory(object cat) { return true; }

        public static object[] GetAllItemDefinitions()
        {
            try
            {
                var lib = GameManager.ItemLibrary;
                var values = System.Enum.GetValues(typeof(TeamNimbus.CloudMeadow.Items.ItemCategory));
                var list = new System.Collections.Generic.List<object>();
                var t = lib.GetType();
                var mEnum = t.GetMethod("EnumerateItemsInCategory");
                if (mEnum != null)
                {
                    var en = values.GetEnumerator();
                    try
                    {
                        while (en.MoveNext())
                        {
                            object cat = en.Current;
                            var ienum = mEnum.Invoke(lib, new object[] { cat }) as System.Collections.IEnumerable;
                            if (ienum != null)
                            {
                                foreach (var def in ienum) list.Add(def);
                            }
                        }
                    }
                    finally { var disp = en as System.IDisposable; if (disp != null) disp.Dispose(); }
                }
                return list.ToArray();
            }
            catch (Exception e) { Plugin.Log.LogWarning("GetAllItemDefinitions failed: " + e.Message); }
            return new object[0];
        }

        public static string[] GetAllItemCategoryNames()
        {
            try
            {
                var values = System.Enum.GetValues(typeof(TeamNimbus.CloudMeadow.Items.ItemCategory));
                var arr = new string[values.Length + 1];
                arr[0] = "All";
                for (int i = 0; i < values.Length; i++) arr[i + 1] = values.GetValue(i).ToString();
                return arr;
            }
            catch
            {
                return new string[] { "All" };
            }
        }

        public static string GetItemCategoryName(object def)
        {
            try
            {
                if (def == null) return string.Empty;
                var cat = SafeProp(def, "Category");
                return cat != null ? cat.ToString() : string.Empty;
            }
            catch { }
            return string.Empty;
        }

        public static void AddItemByDefinition(object def, int amount, int qualityIndex)
        {
            try
            {
                var entry = CreateEntry(def, amount, qualityIndex);
                var ientry = entry as TeamNimbus.CloudMeadow.Inventory.IItemEntry;
                if (ientry != null) GameManager.Status.Inventory.AddItemEntry(ientry);
            }
            catch (Exception e) { Plugin.Log.LogWarning("AddItemByDefinition failed: " + e.Message); }
        }

        public static void AddAllItems(int amount, int qualityIndex)
        {
            var defs = GetAllItemDefinitions();
            int added = 0, skipped = 0;
            for (int i = 0; i < defs.Length; i++)
            {
                try { AddItemByDefinition(defs[i], amount, qualityIndex); added++; }
                catch { skipped++; }
            }
            LogBuffer.Add("AddAllItems: added=" + added + " skipped=" + skipped);
        }

        public static void AdjustEntryQuantity(object entry, int delta)
        {
            try
            {
                var t = entry.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
                var prop = t.GetProperty("Quantity", flags) ?? t.GetProperty("Count", flags) ?? t.GetProperty("Stack", flags) ?? t.GetProperty("Amount", flags);
                if (prop != null && prop.CanRead && prop.CanWrite)
                {
                    var v = prop.GetValue(entry, null); int cur = 0; try { cur = System.Convert.ToInt32(v); } catch { }
                    prop.SetValue(entry, cur + delta, null);
                    return;
                }
                var field = t.GetField("Quantity", flags) ?? t.GetField("Count", flags) ?? t.GetField("Stack", flags) ?? t.GetField("Amount", flags);
                if (field != null)
                {
                    var v = field.GetValue(entry); int cur = 0; try { cur = System.Convert.ToInt32(v); } catch { }
                    field.SetValue(entry, cur + delta);
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("AdjustEntryQuantity failed: " + e.Message); }
        }

        public static void SetEntryMaxQuality(object entry)
        {
            try
            {
                UpgradeEntryToMaxQuality(entry, true);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetEntryMaxQuality failed: " + e.Message); }
        }

        public static void SetEntryQuantity(object entry, int quantity)
        {
            try
            {
                if (entry == null) return; if (quantity < 0) quantity = 0;
                var t = entry.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
                var prop = t.GetProperty("Quantity", flags) ?? t.GetProperty("Count", flags) ?? t.GetProperty("Stack", flags) ?? t.GetProperty("Amount", flags);
                if (prop != null && prop.CanWrite) { prop.SetValue(entry, Convert.ChangeType(quantity, prop.PropertyType), null); return; }
                var field = t.GetField("Quantity", flags) ?? t.GetField("Count", flags) ?? t.GetField("Stack", flags) ?? t.GetField("Amount", flags);
                if (field != null) field.SetValue(entry, Convert.ChangeType(quantity, field.FieldType));
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetEntryQuantity failed: " + e.Message); }
        }

        public static void SetAllInventoryEntriesMaxQuality()
        {
            try
            {
                var entries = GetInventoryEntries();
                int upgraded = 0;
                int i;
                for (i = 0; i < entries.Length; i++)
                {
                    if (UpgradeEntryToMaxQuality(entries[i], false)) upgraded++;
                }
                Banner("Max quality applied: " + upgraded);
                LogBuffer.Add("Inventory max quality upgraded: " + upgraded);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetAllInventoryEntriesMaxQuality failed: " + e.Message); }
        }

        private static bool UpgradeEntryToMaxQuality(object entry, bool showBanner)
        {
            try
            {
                var def = GetEntryDefinition(entry);
                var baseDef = def as BaseItemDefinition;
                if (baseDef == null) return false;

                int qty = GetEntryQuantity(entry);
                if (qty <= 0) qty = 1;
                ItemQuality targetQuality = ResolveMaxAllowedQuality(baseDef);
                if (GetEntryQuality(entry) == targetQuality)
                {
                    var itemKey = SafeProp(baseDef, "Code") ?? SafeProp(baseDef, "Name") ?? baseDef.ToString();
                    Diag("UpgradeEntryToMaxQuality skipped (already max): " + itemKey);
                    if (showBanner) Banner("Item already at max quality");
                    return false;
                }

                var newEntry = CreateEntry(def, qty, (int)targetQuality) as TeamNimbus.CloudMeadow.Inventory.IItemEntry;
                if (newEntry == null)
                {
                    Plugin.Log.LogWarning("UpgradeEntryToMaxQuality: could not create new entry");
                    return false;
                }

                GameManager.Status.Inventory.AddItemEntry(newEntry);
                TryRemoveEntry(entry);
                var itemKey2 = SafeProp(baseDef, "Code") ?? SafeProp(baseDef, "Name") ?? baseDef.ToString();
                Diag("UpgradeEntryToMaxQuality success: " + itemKey2 + " -> " + targetQuality);
                if (showBanner) Banner("Item upgraded to " + targetQuality);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("UpgradeEntryToMaxQuality failed: " + e.Message);
            }
            return false;
        }

        private static ItemQuality GetEntryQuality(object entry)
        {
            try
            {
                var t = entry.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
                var prop = t.GetProperty("Quality", flags);
                if (prop != null)
                {
                    var val = prop.GetValue(entry, null);
                    if (val is ItemQuality) return (ItemQuality)val;
                    if (val != null) return (ItemQuality)Convert.ToInt32(val);
                }

                var field = t.GetField("itemQuality", flags) ?? t.GetField("Quality", flags);
                if (field != null)
                {
                    var val2 = field.GetValue(entry);
                    if (val2 is ItemQuality) return (ItemQuality)val2;
                    if (val2 != null) return (ItemQuality)Convert.ToInt32(val2);
                }
            }
            catch { }
            return ItemQuality.OneStar;
        }

        private static object GetEntryDefinition(object entry)
        {
            try
            {
                var t = entry.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
                var prop = t.GetProperty("Definition", flags) ?? t.GetProperty("ItemDefinition", flags) ?? t.GetProperty("Def", flags);
                if (prop != null) return prop.GetValue(entry, null);
                var field = t.GetField("Definition", flags) ?? t.GetField("ItemDefinition", flags) ?? t.GetField("Def", flags);
                if (field != null) return field.GetValue(entry);
            }
            catch { }
            return null;
        }

        private static int GetEntryQuantity(object entry)
        {
            try
            {
                var t = entry.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
                var prop = t.GetProperty("Quantity", flags) ?? t.GetProperty("Count", flags) ?? t.GetProperty("Stack", flags) ?? t.GetProperty("Amount", flags);
                if (prop != null)
                { var v = prop.GetValue(entry, null); return System.Convert.ToInt32(v); }
                var field = t.GetField("Quantity", flags) ?? t.GetField("Count", flags) ?? t.GetField("Stack", flags) ?? t.GetField("Amount", flags);
                if (field != null)
                { var v = field.GetValue(entry); return System.Convert.ToInt32(v); }
            }
            catch { }
            return 1;
        }

        private static void TryRemoveEntry(object entry)
        {
            try
            {
                var inv = GameManager.Status.Inventory;
                var tInv = inv.GetType();
                // Prefer Remove by entry
                var mByEntry = tInv.GetMethod("RemoveEntry", new System.Type[] { typeof(TeamNimbus.CloudMeadow.Inventory.IItemEntry) })
                               ?? tInv.GetMethod("RemoveItemEntry", new System.Type[] { typeof(TeamNimbus.CloudMeadow.Inventory.IItemEntry) })
                               ?? tInv.GetMethod("Remove", new System.Type[] { typeof(TeamNimbus.CloudMeadow.Inventory.IItemEntry) });
                if (mByEntry != null) { mByEntry.Invoke(inv, new object[] { (TeamNimbus.CloudMeadow.Inventory.IItemEntry)entry }); return; }
                // Fallback: Remove by definition
                var def = GetEntryDefinition(entry);
                if (def != null)
                {
                    var mByDef = tInv.GetMethod("RemoveByDefinition") ?? tInv.GetMethod("RemoveItemByDefinition");
                    if (mByDef != null) { mByDef.Invoke(inv, new object[] { def }); return; }
                }
                // Last resort: set quantity to 0
                AdjustEntryQuantity(entry, -GetEntryQuantity(entry));
            }
            catch { }
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

        // ==== Farm helpers ====
        public static void HatchAllEggs()
        {
            try
            {
                int cnt = 0;
                var farmStatus = GameManager.Status.FarmStatus;
                int shelfIndex;
                for (shelfIndex = (int)TeamNimbus.CloudMeadow.UI.Farm.IncubatorShelfID.TopLeft; shelfIndex < (int)TeamNimbus.CloudMeadow.UI.Farm.IncubatorShelfID.COUNT; shelfIndex++)
                {
                    var shelf = farmStatus.GetIncubatorShelfData((TeamNimbus.CloudMeadow.UI.Farm.IncubatorShelfID)shelfIndex);
                    if (shelf == null) continue;

                    var eggsField = shelf.GetType().GetField("eggsIncubating", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var rawList = eggsField != null ? eggsField.GetValue(shelf) as System.Collections.IList : null;
                    if (rawList == null) continue;

                    int i;
                    for (i = 0; i < rawList.Count; i++)
                    {
                        object record = rawList[i];
                        if (record == null) continue;
                        if (SetEggReady(record))
                        {
                            rawList[i] = record;
                            cnt++;
                        }
                    }
                }

                RefreshIncubatorVisuals();
                Banner("HATCH ALL EGGS: " + cnt + " ready");
            }
            catch (Exception ex) { Plugin.Log.LogWarning("HatchAllEggs failed: " + ex.Message); }
        }
        public static object GetEntryDefinitionForUI(object entry) { return GetEntryDefinition(entry); }
        public static int GetEntryQuantityForUI(object entry) { return GetEntryQuantity(entry); }

        private static bool SetEggReady(object record)
        {
            try
            {
                if (record == null) return false;

                object readyDate = GameManager.Status.CurrentDateTime;
                TryWriteIntField(readyDate, "year", "Year", 1);
                TryWriteIntField(readyDate, "season", "Season", 0);
                TryWriteIntField(readyDate, "day", "Day", 1);
                TryWriteIntField(readyDate, "hour", "Hour", 0);
                TryWriteIntField(readyDate, "minute", "Minute", 0);

                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var hdProp = record.GetType().GetProperty("hatchingDate", flags)
                          ?? record.GetType().GetProperty("HatchingDate", flags);
                if (hdProp != null && hdProp.CanWrite)
                {
                    hdProp.SetValue(record, readyDate, null);
                    return true;
                }

                var hdField = record.GetType().GetField("hatchingDate", flags)
                           ?? record.GetType().GetField("HatchingDate", flags);
                if (hdField != null)
                {
                    hdField.SetValue(record, readyDate);
                    return true;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("SetEggReady failed: " + e.Message);
            }
            return false;
        }

        private static void RefreshIncubatorVisuals()
        {
            try
            {
                var incubators = UnityEngine.Object.FindObjectsOfType<TeamNimbus.CloudMeadow.Farm.MonsterIncubator>();
                int i;
                for (i = 0; i < incubators.Length; i++)
                {
                    if (incubators[i] != null) incubators[i].RefreshEggs();
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("RefreshIncubatorVisuals failed: " + e.Message);
            }
        }

        public static object[] GetIncubatorEggs()
        {
            try
            {
                var s = GameManager.Status;
                var fs = s.FarmStatus;
                var list = new System.Collections.Generic.List<object>();
                if (fs != null)
                {
                    // Try Incubators collection
                    var incProp = fs.GetType().GetProperty("Incubators", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?? fs.GetType().GetProperty("Incubator", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?? fs.GetType().GetProperty("Hatchery", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                ?? fs.GetType().GetProperty("Nursery", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var incVal = incProp != null ? incProp.GetValue(fs, null) : null;
                    var incEnum = incVal as System.Collections.IEnumerable;
                    if (incEnum == null && incVal != null)
                    {
                        // single incubator object
                        CollectEggsFromIncubator(list, incVal);
                    }
                    if (incEnum != null)
                    {
                        foreach (var inc in incEnum) CollectEggsFromIncubator(list, inc);
                    }
                }
                if (list.Count == 0)
                {
                    // Fallback: scan scene behaviours by name tokens and collect egg-like items
                    var mbs = ReflectionUtil.FindMonoBehaviours("incub", "hatch", "egg", "breed", "nursery");
                    for (int i = 0; i < mbs.Count; i++)
                    {
                        CollectEggsFromUnknown(list, mbs[i]);
                    }
                }
                return list.ToArray();
            }
            catch { }
            return new object[0];
        }

        private static void CollectEggsFromIncubator(System.Collections.Generic.List<object> list, object inc)
        {
            if (inc == null) return;
            try
            {
                var t = inc.GetType();
                string[] eggCollections = { "Eggs", "EggQueue", "IncubatingEggs", "Slots", "Queue", "incubatorShelves", "IncubatorShelves" };
                for (int i = 0; i < eggCollections.Length; i++)
                {
                    var p = t.GetProperty(eggCollections[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    object col = null; try { if (p != null) col = p.GetValue(inc, null); } catch { col = null; }
                    var en = col as System.Collections.IEnumerable; if (en == null) continue;
                    foreach (var slot in en)
                    {
                        // Keep the slot object; UI/helpers will unwrap egg info when needed
                        list.Add(slot);
                    }
                }
            }
            catch { }
        }

        private static void CollectEggsFromUnknown(System.Collections.Generic.List<object> list, object host)
        {
            if (host == null) return;
            try
            {
                var t = host.GetType();
                // search breadth-first for properties/fields containing collections with Egg-like elements
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    object val = null; try { val = props[i].GetValue(host, null); } catch { val = null; }
                    AppendEggEnumerable(list, val);
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    object val = null; try { val = fields[i].GetValue(host); } catch { val = null; }
                    AppendEggEnumerable(list, val);
                }
            }
            catch { }
        }

        private static void AppendEggEnumerable(System.Collections.Generic.List<object> list, object val)
        {
            if (val == null) return;
            var en = val as System.Collections.IEnumerable; if (en == null || val is string) return;
            foreach (var it in en)
            {
                if (it == null) continue;
                var tn = it.GetType().FullName ?? it.GetType().Name;
                // Shelves -> eggsIncubating -> egg/hatchingDate
                var eggsIncubatingProp = it.GetType().GetProperty("eggsIncubating", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (eggsIncubatingProp != null)
                {
                    try
                    {
                        var eggs = eggsIncubatingProp.GetValue(it, null) as System.Collections.IEnumerable;
                        if (eggs != null)
                        {
                            foreach (var rec in eggs)
                            {
                                list.Add(rec); // record holds egg + hatchingDate
                            }
                        }
                    }
                    catch { }
                    continue;
                }
                // Anything that looks like an egg or egg-entry
                if (tn.IndexOf("egg", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    list.Add(it);
                    continue;
                }
                // unwrap common wrappers
                var pEgg = it.GetType().GetProperty("Egg") ?? it.GetType().GetProperty("egg") ?? it.GetType().GetProperty("Entry") ?? it.GetType().GetProperty("Value") ?? it.GetType().GetProperty("Item");
                if (pEgg != null)
                {
                    try
                    {
                        var inner = pEgg.GetValue(it, null);
                        if (inner != null)
                        {
                            var tni = inner.GetType().FullName ?? inner.GetType().Name;
                            if (tni.IndexOf("egg", System.StringComparison.OrdinalIgnoreCase) >= 0) list.Add(inner); else list.Add(inner);
                        }
                    }
                    catch { }
                }
            }
        }

        public static string GetEggDisplayName(object obj)
        {
            var egg = UnwrapEgg(obj);
            var n = SafeProp(egg, "Name") ?? SafeProp(egg, "DisplayName") ?? SafeProp(egg, "Code");
            return n != null ? n.ToString() : egg != null ? egg.GetType().Name : "(egg)";
        }

        public static string GetEggTimerString(object obj)
        {
            try
            {
                // First, try on the host (slot) itself
                int seconds = TryReadTimerSeconds(obj);
                if (seconds <= 0)
                {
                    var egg = UnwrapEgg(obj);
                    seconds = TryReadTimerSeconds(egg);
                }
                if (seconds < 0) seconds = 0;
                int mm = seconds / 60; int ss = seconds % 60;
                return mm.ToString("00") + ":" + ss.ToString("00");
            }
            catch { }
            return "--:--";
        }

        private static int TryReadTimerSeconds(object o)
        {
            try
            {
                if (o == null) return 0;
                // Seconds properties
                string[] secProps = { "SecondsRemaining", "RemainingSeconds", "TimeRemainingSeconds", "SecondsToHatch", "SecondsLeft", "TimeLeftSeconds" };
                for (int i = 0; i < secProps.Length; i++)
                {
                    var p = o.GetType().GetProperty(secProps[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        try { var v = p.GetValue(o, null); return System.Convert.ToInt32(v); } catch { }
                    }
                }
                // Minutes properties
                string[] minProps = { "MinutesRemaining", "RemainingMinutes", "TimeRemainingMinutes", "MinutesLeft" };
                for (int i = 0; i < minProps.Length; i++)
                {
                    var p = o.GetType().GetProperty(minProps[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        try { var v = p.GetValue(o, null); return System.Convert.ToInt32(v) * 60; } catch { }
                    }
                }
                // Absolute hatchingDate { year, season, day, hour, minute }
                var hd = o.GetType().GetProperty("hatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                      ?? o.GetType().GetProperty("HatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (hd != null)
                {
                    try
                    {
                        var dateObj = hd.GetValue(o, null);
                        int y = ReadIntField(dateObj, "year", "Year");
                        int s = ReadIntField(dateObj, "season", "Season");
                        int d = ReadIntField(dateObj, "day", "Day");
                        int h = ReadIntField(dateObj, "hour", "Hour");
                        int m = ReadIntField(dateObj, "minute", "Minute");
                        // Build comparable minute stamps (relative ordering OK)
                        int targetStamp = MakeMinuteStamp(y, s, d, h, m);
                        var cal = GameManager.Status.GetCalendarDate;
                        int curStamp = MakeMinuteStamp(cal.DateTime.Year, (int)cal.DateTime.Season, cal.DateTime.Day, cal.DateTime.Hour, cal.DateTime.Minute);
                        int diff = targetStamp - curStamp;
                        if (diff > 0) return diff * 60; // assume stamp in hours? we used minutes already; keep in seconds
                        if (diff == 0) return 60; // will hatch soon
                    }
                    catch { }
                }
                // Progress with duration
                string[] progProps = { "Progress", "Progress01", "NormalizedProgress" };
                float prog = -1f;
                for (int i = 0; i < progProps.Length; i++)
                {
                    var p = o.GetType().GetProperty(progProps[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        try { var v = p.GetValue(o, null); prog = System.Convert.ToSingle(v); break; } catch { }
                    }
                }
                if (prog >= 0f)
                {
                    int total = 0;
                    string[] totalProps = { "TotalSeconds", "DurationSeconds", "SecondsTotal", "Duration", "TotalTimeSeconds" };
                    for (int i = 0; i < totalProps.Length; i++)
                    {
                        var p = o.GetType().GetProperty(totalProps[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (p != null)
                        {
                            try { var v = p.GetValue(o, null); total = System.Convert.ToInt32(v); break; } catch { }
                        }
                    }
                    if (total > 0) return System.Math.Max(0, (int)System.Math.Round((1f - prog) * total));
                }
            }
            catch { }
            return 0;
        }

        private static int ReadIntField(object o, string lower, string upper)
        {
            if (o == null) return 0;
            var p1 = o.GetType().GetProperty(lower, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p1 != null) { try { return System.Convert.ToInt32(p1.GetValue(o, null)); } catch { } }
            var p2 = o.GetType().GetProperty(upper, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p2 != null) { try { return System.Convert.ToInt32(p2.GetValue(o, null)); } catch { } }
            var f1 = o.GetType().GetField(lower, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f1 != null) { try { return System.Convert.ToInt32(f1.GetValue(o)); } catch { } }
            var f2 = o.GetType().GetField(upper, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f2 != null) { try { return System.Convert.ToInt32(f2.GetValue(o)); } catch { } }
            return 0;
        }

        private static int MakeMinuteStamp(int year, int season, int day, int hour, int minute)
        {
            // season: 0..3, day: starts at 1?
            int seasonsPerYear = 4;
            int daysPerSeasonApprox = 30; // approximate; only relative ordering matters for difference
            int stampDays = year * seasonsPerYear * daysPerSeasonApprox + season * daysPerSeasonApprox + (day - 1);
            return stampDays * 24 * 60 + hour * 60 + minute;
        }

        public static void HatchEgg(object obj)
        {
            try
            {
                var egg = UnwrapEgg(obj);
                // Try direct methods on egg instance
                if (egg != null)
                {
                    string[] hatchMethods = { "CHEAT_Hatch", "HatchNow", "ForceHatch", "Finish", "Complete" };
                    for (int i = 0; i < hatchMethods.Length; i++)
                    {
                        var m = egg.GetType().GetMethod(hatchMethods[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (m != null && m.GetParameters().Length == 0) { try { m.Invoke(egg, null); Banner("Egg hatched"); return; } catch { } }
                    }
                    // Set remaining on egg
                    var setSec = egg.GetType().GetMethod("SetRemainingSeconds") ?? egg.GetType().GetMethod("SetSecondsRemaining");
                    if (setSec != null) { try { setSec.Invoke(egg, new object[] { 60 }); Banner("Egg hatch timer set to 00:01"); return; } catch { } }
                    var setMin = egg.GetType().GetMethod("SetRemainingMinutes") ?? egg.GetType().GetMethod("SetMinutesRemaining");
                    if (setMin != null) { try { setMin.Invoke(egg, new object[] { 1 }); Banner("Egg hatch timer set to 00:01"); return; } catch { } }
                    var pSec = egg.GetType().GetProperty("SecondsRemaining", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                             ?? egg.GetType().GetProperty("RemainingSeconds")
                             ?? egg.GetType().GetProperty("TimeRemainingSeconds");
                    if (pSec != null && pSec.CanWrite) { try { pSec.SetValue(egg, 60, null); Banner("Egg hatch timer set to 00:01"); return; } catch { } }
                    var pMin = egg.GetType().GetProperty("MinutesRemaining", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                             ?? egg.GetType().GetProperty("RemainingMinutes")
                             ?? egg.GetType().GetProperty("TimeRemainingMinutes");
                    if (pMin != null && pMin.CanWrite) { try { pMin.SetValue(egg, 1, null); Banner("Egg hatch timer set to 00:01"); return; } catch { } }
                }
                // Try set hatchingDate on record/slot
                object rec = obj;
                // Try set hatchingDate property or field on record
                var hdProp2 = rec.GetType().GetProperty("hatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?? rec.GetType().GetProperty("HatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var hdField2 = (hdProp2 == null) ? (rec.GetType().GetField("hatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?? rec.GetType().GetField("HatchingDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)) : null;
                if (hdProp2 != null || hdField2 != null)
                {
                    try
                    {
                        var cal = GameManager.Status.GetCalendarDate;
                        // current time + 1 minute
                        int y = cal.DateTime.Year;
                        int s = (int)cal.DateTime.Season;
                        int d = cal.DateTime.Day;
                        int h = cal.DateTime.Hour;
                        int m = cal.DateTime.Minute + 1;
                        if (m >= 60) { m -= 60; h += 1; }
                        if (h >= 24) { h -= 24; d += 1; }
                        object dateObj = null;
                        if (hdProp2 != null) dateObj = hdProp2.GetValue(rec, null);
                        else if (hdField2 != null) dateObj = hdField2.GetValue(rec);
                        if (dateObj != null)
                        {
                            TryWriteIntField(dateObj, "year", "Year", y);
                            TryWriteIntField(dateObj, "season", "Season", s);
                            TryWriteIntField(dateObj, "day", "Day", d);
                            TryWriteIntField(dateObj, "hour", "Hour", h);
                            TryWriteIntField(dateObj, "minute", "Minute", m);
                            try
                            {
                                if (hdProp2 != null) hdProp2.SetValue(rec, dateObj, null);
                                else if (hdField2 != null) hdField2.SetValue(rec, dateObj);
                            }
                            catch { }
                            Banner("Egg hatch date set to +1 minute");
                            return;
                        }
                    }
                    catch { }
                }
                Banner("Egg hatch: attempted");
            }
            catch (Exception e) { Plugin.Log.LogWarning("HatchEgg failed: " + e.Message); }
        }

        private static void TryWriteIntField(object o, string lower, string upper, int value)
        {
            if (o == null) return;
            var p1 = o.GetType().GetProperty(lower, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p1 != null && p1.CanWrite) { try { p1.SetValue(o, value, null); return; } catch { } }
            var p2 = o.GetType().GetProperty(upper, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p2 != null && p2.CanWrite) { try { p2.SetValue(o, value, null); return; } catch { } }
            var f1 = o.GetType().GetField(lower, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f1 != null) { try { f1.SetValue(o, value); return; } catch { } }
            var f2 = o.GetType().GetField(upper, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f2 != null) { try { f2.SetValue(o, value); return; } catch { } }
        }

        private static object UnwrapEgg(object host)
        {
            object cur = host;
            for (int depth = 0; depth < 3 && cur != null; depth++)
            {
                // if this looks like an egg (has timer/name), return
                if (SafeProp(cur, "SecondsRemaining") != null || SafeProp(cur, "MinutesRemaining") != null || SafeProp(cur, "RemainingSeconds") != null || SafeProp(cur, "RemainingMinutes") != null)
                    return cur;
                if (SafeProp(cur, "Name") != null || SafeProp(cur, "DisplayName") != null || SafeProp(cur, "Code") != null)
                {
                    // might still be an entry that holds timers deeper; continue
                }
                var p = cur.GetType().GetProperty("Egg") ?? cur.GetType().GetProperty("Entry") ?? cur.GetType().GetProperty("Value") ?? cur.GetType().GetProperty("Item");
                if (p != null)
                {
                    try { cur = p.GetValue(cur, null); continue; } catch { return cur; }
                }
                break;
            }
            return cur;
        }

        private static bool _pendingFarmLayoutRefresh;
        public static bool PendingFarmLayoutRefresh { get { return _pendingFarmLayoutRefresh; } }
        public static void MarkPendingFarmLayoutRefresh() { _pendingFarmLayoutRefresh = true; }
        public static void ClearPendingFarmLayoutRefresh() { _pendingFarmLayoutRefresh = false; }
        public static void TryRefreshFarmLayout(TeamNimbus.CloudMeadow.Farm.FarmSceneManager fsm)
        {
            try
            {
                var gs = GameManager.Status;
                var fsmType = typeof(TeamNimbus.CloudMeadow.Farm.FarmSceneManager);
                var segField = fsmType.GetField("_segments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var segs = segField != null ? segField.GetValue(fsm) as Array : null;
                var initMethod = fsmType.GetMethod("InitUpgradeLevelLayout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (segs != null && initMethod != null)
                {
                    int farmLevel = gs.FarmLevel; // Flags.FarmUpgrade + 1
                    for (int i = 0; i < segs.Length; i++)
                    {
                        bool isActivated = (i < farmLevel);
                        initMethod.Invoke(fsm, new object[] { i, isActivated });
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Farm layout refresh failed: " + e.Message);
            }
        }

        public static void UpgradeFarm()
        {
            try
            {
                var gs = GameManager.Status;
                // Max out farm level
                var flags = gs.Flags;
                try { var f = flags.GetType().GetField("FarmUpgrade"); if (f != null) f.SetValue(flags, GameStatus.MaxFarmLevel); } catch { flags.FarmUpgrade = GameStatus.MaxFarmLevel; }
                // Unlock all buildings
                foreach (FarmBuildingTypes bt in Enum.GetValues(typeof(FarmBuildingTypes)))
                {
                    try { gs.SetFarmBuildingUnlocked(bt, false); } catch { }
                }
                // Refresh farm scene segments if present
                var fsm = UnityEngine.Object.FindObjectOfType<TeamNimbus.CloudMeadow.Farm.FarmSceneManager>();
                // Only attempt in Farm scene; if not present, mark pending to refresh on scene init
                if (fsm != null)
                {
                    TryRefreshFarmLayout(fsm);
                }
                else
                {
                    MarkPendingFarmLayoutRefresh();
                }
                Banner("Farm: All upgrades unlocked");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("UpgradeFarm failed: " + e.Message);
            }
            return;
        }

        public static void WaterAllCrops()
        {
            try
            {
                var s = GameManager.Status;
                var fs = s.FarmStatus;
                if (fs != null)
                {
                    // Try method
                    var m = fs.GetType().GetMethod("CHEAT_WaterAllCrops", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                          ?? fs.GetType().GetMethod("WaterAllCrops");
                    if (m != null) { try { m.Invoke(fs, null); Banner("All crops watered"); return; } catch { } }
                    // Try iterate plots
                    var plotsProp = fs.GetType().GetProperty("CropPlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var plots = plotsProp != null ? plotsProp.GetValue(fs, null) as System.Collections.IEnumerable : null;
                    if (plots != null)
                    {
                        foreach (var p in plots)
                        {
                            var water = p.GetType().GetMethod("Water", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                      ?? p.GetType().GetMethod("SetWatered");
                            if (water != null) { try { water.Invoke(p, null); } catch { } }
                        }
                        Banner("All crops watered");
                        return;
                    }
                }
                Banner("Water crops: attempted");
            }
            catch (Exception e) { Plugin.Log.LogWarning("WaterAllCrops failed: " + e.Message); }
        }

        public static void GrowAllCrops()
        {
            try
            {
                var s = GameManager.Status;
                var fs = s.FarmStatus;
                if (fs != null)
                {
                    // Try method
                    var m = fs.GetType().GetMethod("CHEAT_GrowAllCrops", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                          ?? fs.GetType().GetMethod("GrowAllCrops")
                          ?? fs.GetType().GetMethod("AdvanceGrowthAllCrops");
                    if (m != null) { try { m.Invoke(fs, null); Banner("All crops grown"); return; } catch { } }
                    // Try iterate plots
                    var plotsProp = fs.GetType().GetProperty("CropPlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var plots = plotsProp != null ? plotsProp.GetValue(fs, null) as System.Collections.IEnumerable : null;
                    if (plots != null)
                    {
                        foreach (var p in plots)
                        {
                            var grow = p.GetType().GetMethod("Grow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                     ?? p.GetType().GetMethod("AdvanceGrowth");
                            if (grow != null) { try { grow.Invoke(p, null); } catch { } }
                        }
                        Banner("All crops grown");
                        return;
                    }
                }
                Banner("Grow crops: attempted");
            }
            catch (Exception e) { Plugin.Log.LogWarning("GrowAllCrops failed: " + e.Message); }
        }

        public static void SetTime(int hour, int minute)
        {
            try
            {
                var cal = GameManager.Status.GetCalendarDate;
                int h = ((hour % 24) + 24) % 24;
                int m = ((minute % 60) + 60) % 60;
                // reset to start of day, then tick to target absolute time
                int cur = cal.DateTime.Hour * 60 + cal.DateTime.Minute;
                if (cur != 0) cal.TickMinutes(-cur);
                int target = h * 60 + m;
                if (target != 0) cal.TickMinutes(target);
                Banner("Time set to " + h + ":" + m.ToString("00"));
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetTime failed: " + e.Message); }
        }

        public static void SetDayAndTime(int day, int hour, int minute)
        {
            try
            {
                var cal = GameManager.Status.GetCalendarDate;
                // Normalize time to 00:00 of current day first
                int cur = cal.DateTime.Hour * 60 + cal.DateTime.Minute;
                if (cur != 0) cal.TickMinutes(-cur);
                // Compute absolute delta in days from current day to target day
                int currentDay = cal.DateTime.Day;
                int deltaDays = day - currentDay;
                if (deltaDays != 0) cal.TickMinutes(deltaDays * 24 * 60);
                // Set exact time within day
                int target = ((hour % 24 + 24) % 24) * 60 + ((minute % 60 + 60) % 60);
                if (target != 0) cal.TickMinutes(target);
                Banner("Day set to " + day + ", time set to " + hour + ":" + minute.ToString("00"));
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetDayAndTime failed: " + e.Message); }
        }

        // ===== Monsters & Traits helpers =====
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

        public static void GenerateFarmTraitsReport(string path)
        {
            try
            {
                var mons = GetActiveMonsters();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== Farm Traits Report ===");
                sb.AppendLine(System.DateTime.Now.ToString("u"));
                for (int i = 0; i < mons.Length; i++)
                {
                    var m = mons[i]; if (m == null) continue;
                    sb.AppendLine("# " + (i+1) + ". " + m.Name + " (" + m.FarmableSpecies + ", " + m.Gender + ")");
                    foreach (var tr in m.EnumerateTraitInstances())
                    {
                        if (tr == null) continue;
                        string tname = SafeTraitName(tr);
                        int grade = SafeIntProp(tr, "Grade");
                        sb.AppendLine("- " + tname + " [Grade:" + grade + "]");
                        // Effects/Details from definition
                        var def = SafeProp(tr, "TraitDefinition");
                        if (def != null)
                        {
                            string src = SafeProp(def, "TraitSource") != null ? SafeProp(def, "TraitSource").ToString() : "";
                            string targeted = SafeProp(def, "TargetedStat") != null ? SafeProp(def, "TargetedStat").ToString() : "";
                            if (!string.IsNullOrEmpty(src)) sb.AppendLine("  Source: " + src);
                            if (!string.IsNullOrEmpty(targeted)) sb.AppendLine("  TargetedStat: " + targeted);
                            // Try enumerate stat mods if any
                            DumpMods(def, sb, "StatModifiers");
                            DumpMods(def, sb, "PassiveStatMods");
                            DumpMods(def, sb, "Mods");
                            // Description if available
                            var desc = SafeProp(def, "Description"); if (desc != null) sb.AppendLine("  Desc: " + desc);
                        }
                    }
                    sb.AppendLine();
                }
                System.IO.File.WriteAllText(path, sb.ToString());
                Banner("Traits report saved: " + path);
            }
            catch (Exception e) { Plugin.Log.LogWarning("GenerateFarmTraitsReport failed: " + e.Message); }
        }

        private static void DumpMods(object def, System.Text.StringBuilder sb, string fieldOrProp)
        {
            try
            {
                var t = def.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
                object val = null;
                var p = t.GetProperty(fieldOrProp, flags); if (p != null) val = p.GetValue(def, null);
                if (val == null) { var f = t.GetField(fieldOrProp, flags); if (f != null) val = f.GetValue(def); }
                var en = val as System.Collections.IEnumerable; if (en == null || val is string) return;
                int count = 0;
                foreach (var mod in en)
                {
                    if (mod == null) continue;
                    count++;
                    sb.AppendLine("  * Mod: " + mod.ToString());
                }
                if (count == 0) return;
            }
            catch { }
        }

        private static string SafeTraitName(object traitInstance)
        {
            try
            {
                var def = SafeProp(traitInstance, "TraitDefinition");
                if (def != null)
                {
                    var name = SafeProp(def, "Name") ?? SafeProp(def, "DisplayName") ?? SafeProp(def, "Code");
                    if (name != null) return name.ToString();
                }
                var code = SafeProp(traitInstance, "TraitCode") ?? SafeProp(traitInstance, "Code");
                if (code != null) return code.ToString();
            }
            catch { }
            return traitInstance != null ? traitInstance.ToString() : "(null)";
        }

        private static object SafeProp(object o, string name)
        {
            try
            {
                if (o == null) return null;
                var p = o.GetType().GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (p != null && p.CanRead) return p.GetValue(o, null);
            }
            catch { }
            return null;
        }

        private static int SafeIntProp(object o, string name)
        {
            try
            {
                var v = SafeProp(o, name); if (v == null) return 0; return Convert.ToInt32(v);
            }
            catch { return 0; }
        }

        public static void GenerateAllTraitsCatalog(string path)
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                var enumType = Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies, Game");
                if (libType == null || enumType == null) { Plugin.Log.LogWarning("Trait library or enum not found"); return; }
                var lib = UnityEngine.Object.FindObjectOfType(libType);
                if (lib == null) { Plugin.Log.LogWarning("MonsterTraitLibrary instance not found"); return; }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== All Traits Catalog ===");
                sb.AppendLine(System.DateTime.Now.ToString("u"));

                var resolveByType = libType.GetMethod("ResolveMonsterTraitsByType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var all = new System.Collections.Generic.HashSet<object>();

                // by species
                Array speciesValues = Enum.GetValues(enumType);
                for (int i = 0; i < speciesValues.Length; i++)
                {
                    var species = speciesValues.GetValue(i);
                    object traitsByType = null;
                    try { traitsByType = resolveByType.Invoke(lib, new object[] { species }); } catch { traitsByType = null; }
                    if (traitsByType == null) continue;
                    var tbt = traitsByType.GetType();
                    string[] fields = { "OtherSpeciesTraits", "StatLimitTraits", "OtherBloodlineTraits" };
                    for (int f = 0; f < fields.Length; f++)
                    {
                        var fld = tbt.GetField(fields[f], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (fld == null) continue;
                        var arr = fld.GetValue(traitsByType) as System.Collections.IEnumerable;
                        if (arr == null) continue;
                        foreach (var d in arr) if (d != null) all.Add(d);
                    }
                }
                // universal
                var uniFld = libType.GetField("universalTraits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var uniArr = uniFld != null ? uniFld.GetValue(lib) as System.Collections.IEnumerable : null;
                if (uniArr != null) foreach (var d in uniArr) if (d != null) all.Add(d);

                // Write details
                int idx = 1;
                foreach (var def in all)
                {
                    if (def == null) continue;
                    var name = SafeProp(def, "Name") ?? SafeProp(def, "DisplayName") ?? SafeProp(def, "Code");
                    var src = SafeProp(def, "TraitSource");
                    var targeted = SafeProp(def, "TargetedStat");
                    var desc = SafeProp(def, "Description");
                    var max = SafeProp(def, "MaxGrade") ?? SafeProp(def, "MaxLevel") ?? SafeProp(def, "Cap");
                    sb.AppendLine(idx + ". " + (name != null ? name.ToString() : def.ToString()));
                    if (src != null) sb.AppendLine("  Source: " + src);
                    if (targeted != null) sb.AppendLine("  TargetedStat: " + targeted);
                    if (desc != null) sb.AppendLine("  Desc: " + desc);
                    if (max != null) sb.AppendLine("  MaxLevel: " + max);
                    DumpMods(def, sb, "StatModifiers");
                    DumpMods(def, sb, "PassiveStatMods");
                    DumpMods(def, sb, "Mods");
                    sb.AppendLine();
                    idx++;
                }

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                System.IO.File.WriteAllText(path, sb.ToString());
                Banner("All traits catalog saved: " + path);
            }
            catch (Exception e) { Plugin.Log.LogWarning("GenerateAllTraitsCatalog failed: " + e.Message); }
        }

        public static Array GetAllSpecies()
        {
            try
            {
                var t = Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies, Game");
                if (t != null && t.IsEnum) return Enum.GetValues(t);
            }
            catch { }
            return new object[0];
        }

        public static string GetMonsterSpecies(object monster)
        {
            try
            {
                var t = monster.GetType();
                var prop = t.GetProperty("Species") ?? t.GetProperty("FarmableSpecies") ?? t.GetProperty("Type");
                if (prop != null)
                {
                    var v = prop.GetValue(monster, null); return v != null ? v.ToString() : "-";
                }
            }
            catch { }
            return "-";
        }

        public static void SetMonsterSpecies(object monster, string speciesName)
        {
            try
            {
                var m = monster as MonsterCharacterStats;
                if (m == null) throw new InvalidOperationException("MonsterCharacterStats expected");

                var t = monster.GetType();
                // Resolve FarmableSpecies enum from Game.dll
                var speciesEnum = Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies, Game", false)
                                  ?? Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies", false);
                if (speciesEnum == null || !speciesEnum.IsEnum)
                    throw new InvalidOperationException("FarmableSpecies enum not found");

                object enumVal = null;
                try { enumVal = Enum.Parse(speciesEnum, speciesName, true); } catch { }
                if (enumVal == null)
                {
                    var names = Enum.GetNames(speciesEnum);
                    for (int i = 0; i < names.Length; i++)
                    {
                        if (names[i].IndexOf(speciesName, StringComparison.OrdinalIgnoreCase) >= 0) { enumVal = Enum.Parse(speciesEnum, names[i]); break; }
                    }
                }
                if (enumVal == null) throw new ArgumentException("Unknown species: " + speciesName);

                var targetSpecies = (FarmableSpecies)enumVal;
                var oldSpecies = m.FarmableSpecies;
                var oldGender = m.Gender;
                if (oldSpecies == targetSpecies)
                {
                    Diag("SetMonsterSpecies no-op: " + oldSpecies);
                    return;
                }

                Diag("SetMonsterSpecies " + oldSpecies + " -> " + targetSpecies + " (gender " + oldGender + ")");

                // 1) Preferred: if there is a dedicated method (rare), use it
                var changeMethod = t.GetMethod("ChangeMonsterSpecies", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (changeMethod != null && changeMethod.GetParameters().Length == 1)
                {
                    changeMethod.Invoke(monster, new object[] { enumVal });
                }
                else
                {
                    // 2) Directly set private field 'species'
                    var fSpecies = t.GetField("species", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (fSpecies == null) throw new MissingFieldException("species field not found on MonsterCharacterStats");
                    fSpecies.SetValue(monster, enumVal);

                    // 2a) Update monsterAsset using GameManager.CombatCharacterLibrary.ResolveMonsterCharacterAsset(FarmableSpecies)
                    try
                    {
                        var gmType = Type.GetType("TeamNimbus.CloudMeadow.Managers.GameManager, Game", false)
                                   ?? Type.GetType("TeamNimbus.CloudMeadow.Managers.GameManager", false);
                        if (gmType != null)
                        {
                            var pCombatLib = gmType.GetProperty("CombatCharacterLibrary", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            var combatLib = pCombatLib != null ? pCombatLib.GetValue(null, null) : null;
                            if (combatLib != null)
                            {
                                var resolve = combatLib.GetType().GetMethod("ResolveMonsterCharacterAsset", new Type[] { speciesEnum });
                                if (resolve != null)
                                {
                                    var asset = resolve.Invoke(combatLib, new object[] { enumVal });
                                    var fAsset = t.GetField("monsterAsset", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                    if (fAsset != null) fAsset.SetValue(monster, asset);
                                }
                            }
                        }
                    }
                    catch { }

                    // 2c) Reinitialize data assets and definitions to be safe
                    try
                    {
                        var init = t.GetMethod("InitializeDataAssets", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (init != null) init.Invoke(monster, null);
                    }
                    catch { }

                    // 2d) Trigger sprite refresh
                    try
                    {
                        var pEvent = t.GetProperty("SpriteModifiedEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        var spriteEvt = pEvent != null ? pEvent.GetValue(monster, null) : null;
                        if (spriteEvt == null)
                        {
                            var fEvent = t.GetField("SpriteModifiedEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (fEvent != null) spriteEvt = fEvent.GetValue(monster);
                        }
                        if (spriteEvt != null)
                        {
                            var trig = spriteEvt.GetType().GetMethod("Trigger", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (trig != null) trig.Invoke(spriteEvt, null);
                        }
                    }
                    catch { }
                }

                NormalizeMonsterAfterSpeciesSwap(m, oldSpecies, oldGender, targetSpecies);
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterSpecies failed: " + e.Message); }
        }

        public static string[] GetEggInspectorLines(object obj)
        {
            var lines = new System.Collections.Generic.List<string>();
            try
            {
                object egg = UnwrapEgg(obj); if (egg == null) return new[] { "Egg data missing" };
                lines.Add("Runtime: " + egg.GetType().FullName + " | Host: " + (obj != null ? obj.GetType().Name : "none"));
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic; int shown = 0;
                var props = egg.GetType().GetProperties(flags);
                for (int i = 0; i < props.Length && shown < 14; i++) if (props[i].GetIndexParameters().Length == 0 && IsSimpleValueType(props[i].PropertyType)) { try { lines.Add(props[i].Name + ": " + props[i].GetValue(egg, null)); shown++; } catch { } }
                var fields = egg.GetType().GetFields(flags);
                for (int i = 0; i < fields.Length && shown < 20; i++) if (IsSimpleValueType(fields[i].FieldType)) { try { lines.Add(fields[i].Name + ": " + fields[i].GetValue(egg)); shown++; } catch { } }
            }
            catch (Exception e) { lines.Add("Egg inspection failed: " + e.Message); }
            return lines.ToArray();
        }
        public static EggItemEntry ResolveEggEntry(object obj)
        {
            object current = obj; var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            for (int depth = 0; depth < 5 && current != null; depth++)
            {
                var typed = current as EggItemEntry; if (typed != null) return typed;
                string[] names = { "Egg", "Entry", "Item", "Value", "egg", "entry", "item" }; object next = null;
                for (int i = 0; i < names.Length && next == null; i++) { var p = current.GetType().GetProperty(names[i], flags); try { if (p != null) next = p.GetValue(current, null); } catch { } var f = current.GetType().GetField(names[i], flags); try { if (next == null && f != null) next = f.GetValue(current); } catch { } }
                if (object.ReferenceEquals(next, current)) break; current = next;
            }
            return null;
        }
        public static void SetEggParents(object obj, int first, int second)
        {
            try { var egg = ResolveEggEntry(obj); if (egg == null || first == second) return; var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic; typeof(EggItemEntry).GetField("firstParentID", flags).SetValue(egg, first); typeof(EggItemEntry).GetField("secondParentID", flags).SetValue(egg, second); LogBuffer.Add("Egg parents -> " + first + ", " + second); }
            catch (Exception e) { Plugin.Log.LogWarning("SetEggParents failed: " + e.Message); }
        }
        public static void CopyEggToInventory(object obj)
        {
            try { var egg = ResolveEggEntry(obj); if (egg == null) return; var clone = new EggItemEntry((EggItemDefinition)egg.Definition, egg.MagicalSaturationAtCreation); SetEggParents(clone, egg.FirstParentID, egg.SecondParentID); GameManager.Status.Inventory.AddItemEntry(clone); LogBuffer.Add("Egg copied to player inventory: " + egg.DisplayName); }
            catch (Exception e) { Plugin.Log.LogWarning("CopyEggToInventory failed: " + e.Message); }
        }
        public static void RerollEggCopyToInventory(object obj)
        {
            try { var egg = ResolveEggEntry(obj); if (egg == null) return; var clone = new EggItemEntry((EggItemDefinition)egg.Definition, UnityEngine.Random.Range(0, 101)); SetEggParents(clone, egg.FirstParentID, egg.SecondParentID); GameManager.Status.Inventory.AddItemEntry(clone); LogBuffer.Add("Rerolled egg copy added: " + egg.DisplayName); }
            catch (Exception e) { Plugin.Log.LogWarning("RerollEggCopyToInventory failed: " + e.Message); }
        }
        private static bool IsSimpleValueType(Type type) { type = Nullable.GetUnderlyingType(type) ?? type; return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal); }

        private static void NormalizeMonsterAfterSpeciesSwap(MonsterCharacterStats monster, FarmableSpecies oldSpecies, Gender oldGender, FarmableSpecies targetSpecies)
        {
            try
            {
                if (monster == null) return;

                SetMonsterGenderForSpeciesSwap(monster, oldGender, targetSpecies);
                SetMonsterPaletteToSpeciesDefault(monster, targetSpecies);
                NormalizeSpeciesTraitForSpecies(monster, targetSpecies);
                ReapplyDefaultStatLimitTraitsForSpecies(monster, targetSpecies);
                ReinitializeMonsterDataAssets(monster);
                ResetMonsterCombatStateAfterSpeciesSwap(monster);
                RefreshMonsterAfterTrait(monster);

                Banner("Type -> " + targetSpecies);
                LogBuffer.Add("Monster type " + oldSpecies + " -> " + targetSpecies);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("NormalizeMonsterAfterSpeciesSwap failed: " + e.Message);
            }
        }

        private static void SetMonsterGenderForSpeciesSwap(MonsterCharacterStats monster, Gender oldGender, FarmableSpecies targetSpecies)
        {
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
                var fGender = monster.GetType().GetField("gender", flags);
                if (fGender == null) return;

                Gender newGender = oldGender;
                if (targetSpecies == FarmableSpecies.Chimera || targetSpecies == FarmableSpecies.Crab)
                {
                    newGender = Gender.Other;
                }
                else if (oldGender == Gender.Other)
                {
                    try { newGender = GameManager.Status.ResolveNextGender(targetSpecies); }
                    catch { newGender = Gender.Female; }
                }

                fGender.SetValue(monster, newGender);
                Diag("SetMonsterGenderForSpeciesSwap -> " + newGender);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("SetMonsterGenderForSpeciesSwap failed: " + e.Message);
            }
        }

        private static void SetMonsterPaletteToSpeciesDefault(MonsterCharacterStats monster, FarmableSpecies targetSpecies)
        {
            try
            {
                var t = monster.GetType();
                var paletteEnum = Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette, Game", false)
                                 ?? Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette", false);
                if (paletteEnum == null || !paletteEnum.IsEnum) return;

                var paletteVal = Enum.ToObject(paletteEnum, (int)targetSpecies);
                var changePalette = t.GetMethod("ChangeMonsterPalette", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (changePalette != null && changePalette.GetParameters().Length == 1)
                {
                    changePalette.Invoke(monster, new object[] { paletteVal });
                }
                else
                {
                    var fPalette = t.GetField("palette", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (fPalette != null) fPalette.SetValue(monster, paletteVal);
                }
                Diag("SetMonsterPaletteToSpeciesDefault -> " + targetSpecies);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("SetMonsterPaletteToSpeciesDefault failed: " + e.Message);
            }
        }

        private static void NormalizeSpeciesTraitForSpecies(MonsterCharacterStats monster, FarmableSpecies targetSpecies)
        {
            try
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
                var speciesField = monster.GetType().GetField("_speciesTrait", flags);
                if (speciesField == null) return;

                object currentTrait = speciesField.GetValue(monster);
                bool keepTrait = false;
                if (currentTrait != null)
                {
                    var def = GetTraitDefinitionFromInstance(currentTrait);
                    var defs = GetSpeciesTraitDefinitionsForSpecies(targetSpecies.ToString());
                    for (int i = 0; i < defs.Length; i++)
                    {
                        if (TraitDefinitionsEqual(def, defs[i]))
                        {
                            keepTrait = true;
                            break;
                        }
                    }
                }

                if (!keepTrait)
                {
                    speciesField.SetValue(monster, null);
                    Diag("NormalizeSpeciesTraitForSpecies -> cleared invalid species trait");
                }

                if (targetSpecies == FarmableSpecies.Chimera && speciesField.GetValue(monster) == null)
                {
                    var chimeraDefs = GetSpeciesTraitDefinitionsForSpecies("Chimera");
                    if (chimeraDefs.Length > 0)
                    {
                        SetMonsterSpeciesTrait(monster, chimeraDefs[0], 1);
                        Diag("NormalizeSpeciesTraitForSpecies -> applied default Chimera variant");
                        return;
                    }
                }

                SyncSpecialSpeciesTraitState(monster, GetTraitDefinitionFromInstance(speciesField.GetValue(monster)) as TeamNimbus.CloudMeadow.Traits.BaseTraitDefinition);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("NormalizeSpeciesTraitForSpecies failed: " + e.Message);
            }
        }

        private static void ReapplyDefaultStatLimitTraitsForSpecies(MonsterCharacterStats monster, FarmableSpecies targetSpecies)
        {
            try
            {
                if (monster == null) return;
                var defs = GameManager.MonsterTraitLibrary.EnumerateDefaultStatLimitTraitsForSpecies(targetSpecies);
                foreach (var def in defs)
                {
                    if (def != null) SetMonsterStatLimitTrait(monster, def, 1);
                }
                Diag("ReapplyDefaultStatLimitTraitsForSpecies -> " + targetSpecies);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("ReapplyDefaultStatLimitTraitsForSpecies failed: " + e.Message);
            }
        }

        private static void ReinitializeMonsterDataAssets(MonsterCharacterStats monster)
        {
            try
            {
                var init = monster.GetType().GetMethod("InitializeDataAssets", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (init != null) init.Invoke(monster, null);
                Diag("ReinitializeMonsterDataAssets done");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("ReinitializeMonsterDataAssets failed: " + e.Message);
            }
        }

        private static void ResetMonsterCombatStateAfterSpeciesSwap(MonsterCharacterStats monster)
        {
            try
            {
                var reset = monster.GetType().BaseType.GetMethod("ResetCombatStateData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (reset != null) reset.Invoke(monster, null);
                Diag("ResetMonsterCombatStateAfterSpeciesSwap done");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("ResetMonsterCombatStateAfterSpeciesSwap failed: " + e.Message);
            }
        }

        public static void SwapMonsterGender(TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats m)
        {
            try
            {
                if (m != null)
                {
                    var canSwap = m.GetType().GetMethod("CanSwapGender", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var swap = m.GetType().GetMethod("SwapGender", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    bool ok = true;
                    if (canSwap != null)
                    {
                        object r = canSwap.Invoke(m, null);
                        ok = (r is bool) ? (bool)r : true;
                    }
                    if (ok && swap != null) swap.Invoke(m, null);
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SwapMonsterGender failed: " + e.Message); }
        }

        public static void SetMonsterGender(TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats m, string desired)
        {
            try
            {
                if (m == null) return;
                var genderProp = m.GetType().GetProperty("Gender");
                if (genderProp == null) { SwapMonsterGender(m); return; }
                var cur = genderProp.GetValue(m, null);
                var enumType = cur.GetType();
                var target = Enum.Parse(enumType, desired, true);
                if (!cur.Equals(target))
                {
                    SwapMonsterGender(m);
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterGender failed: " + e.Message); }
        }

        public static object[] GetBloodlineTraitDefinitionsForSpecies(string speciesName)
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                var lib = ResolveMonsterTraitLibrary();
                if (lib != null)
                {
                    var resolve = libType.GetMethod("ResolveMonsterTraitsByType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var enumType = Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies, Game");
                    var species = Enum.Parse(enumType, speciesName, true);
                    var traitsByType = resolve.Invoke(lib, new object[] { species });
                    var tbt = traitsByType.GetType();

                    string[] fields = { "OtherBloodlineTraits", "OtherSpeciesTraits", "StatLimitTraits", "BloodlineTraits", "SpeciesTraits" };
                    var list = new System.Collections.Generic.List<object>();
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var fld = tbt.GetField(fields[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (fld == null) continue;
                        object val = null; try { val = fld.GetValue(traitsByType); } catch { }
                        var arr = val as System.Collections.IEnumerable; if (arr == null) continue;
                        foreach (var d in arr) if (d != null) list.Add(d);
                    }
                    return FilterToDefinitionLike(list.ToArray());
                }
            }
            catch { }
            return new object[0];
        }

        public static object[] GetMonsterTraits(object monster)
        {
            try
            {
                var traits = new System.Collections.Generic.List<object>();

                var enumTraits = monster.GetType().GetMethod("EnumerateTraitInstances", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (enumTraits != null)
                {
                    var en = enumTraits.Invoke(monster, null) as System.Collections.IEnumerable;
                    if (en != null)
                    {
                        foreach (var item in en) if (item != null) traits.Add(item);
                    }
                }

                if (traits.Count == 0)
                {
                    var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    var t = monster.GetType();
                    var props = t.GetProperties(flags);
                    for (int i = 0; i < props.Length; i++)
                    {
                        var p = props[i];
                        if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            object col = null; try { col = p.GetValue(monster, null); } catch { }
                            AppendTraits(traits, col);
                            if (IsTraitInstanceLike(col)) traits.Add(col);
                        }
                    }
                    var fields = t.GetFields(flags);
                    for (int j = 0; j < fields.Length; j++)
                    {
                        var f = fields[j];
                        if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            object col2 = null; try { col2 = f.GetValue(monster); } catch { }
                            AppendTraits(traits, col2);
                            if (IsTraitInstanceLike(col2)) traits.Add(col2);
                        }
                    }
                }
                // Deduplicate by reference and keep only instance-like entries
                var uniq = new System.Collections.Generic.List<object>();
                var seen = new System.Collections.Generic.HashSet<object>(new RefEqComparer());
                for (int i = 0; i < traits.Count; i++)
                {
                    var it = traits[i]; if (it == null) continue;
                    if (!IsTraitInstanceLike(it)) continue;
                    if (seen.Add(it)) uniq.Add(it);
                }
                return uniq.ToArray();
            }
            catch { }
            return new object[0];
        }

        private static void AppendTraits(System.Collections.Generic.List<object> list, object col)
        {
            if (col == null) return;
            var en = col as System.Collections.IEnumerable; if (en == null || col is string) return;
            foreach (var item in en) if (item != null) list.Add(item);
        }

        public static string GetTraitBucketForMonster(object monster, object traitInstance)
        {
            try
            {
                if (monster == null || traitInstance == null) return "Unknown";

                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var t = monster.GetType();

                var speciesField = t.GetField("_speciesTrait", flags);
                if (speciesField != null)
                {
                    var speciesTrait = speciesField.GetValue(monster);
                    if (speciesTrait != null && object.ReferenceEquals(speciesTrait, traitInstance)) return "Species";
                }

                string[] statFields = { "physiqueLimitTrait", "staminaLimitTrait", "intuitionLimitTrait", "swiftnessLimitTrait" };
                for (int i = 0; i < statFields.Length; i++)
                {
                    var f = t.GetField(statFields[i], flags);
                    if (f != null)
                    {
                        var v = f.GetValue(monster);
                        if (v != null && object.ReferenceEquals(v, traitInstance)) return "StatLimit";
                    }
                }

                var src = GetTraitSourceString(traitInstance);
                if (src.IndexOf("universal", StringComparison.OrdinalIgnoreCase) >= 0) return "Universal";
                return "Bloodline";
            }
            catch { }
            return "Unknown";
        }

        public static object[] GetTraitDefinitionsForSpecies(string speciesName)
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                if (libType != null)
                {
                    var lib = UnityEngine.Object.FindObjectOfType(libType);
                    if (lib != null)
                    {
                        var list = new System.Collections.Generic.List<object>();
                        var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                        var methods = libType.GetMethods(flags);
                        for (int i = 0; i < methods.Length; i++)
                        {
                            var m = methods[i];
                            if (m.Name.IndexOf("Enumerate", StringComparison.OrdinalIgnoreCase) >= 0 && m.GetParameters().Length == 1)
                            {
                                var p = m.GetParameters()[0];
                                if (p.ParameterType.IsEnum && p.ParameterType.FullName.IndexOf("FarmableSpecies") >= 0)
                                {
                                    var enumVal = Enum.Parse(p.ParameterType, speciesName, true);
                                    var en = m.Invoke(lib, new object[] { enumVal }) as System.Collections.IEnumerable;
                                    if (en != null) foreach (var def in en) list.Add(def);
                                }
                            }
                        }
                        if (list.Count > 0) return list.ToArray();
                    }
                }
            }
            catch { }
            return new object[0];
        }

        private static object ResolveMonsterTraitLibrary()
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                if (libType == null) return null;
                // 1) Find active scene object
                var lib = UnityEngine.Object.FindObjectOfType(libType);
                if (lib != null) return lib;
                // 2) Resources (inactive assets, ScriptableObjects)
                var all = Resources.FindObjectsOfTypeAll(libType);
                if (all != null && all.Length > 0) return all.GetValue(0);
                // 3) Static Instance/Singleton property
                var instProp = libType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) ??
                               libType.GetProperty("Singleton", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (instProp != null)
                {
                    try { var inst = instProp.GetValue(null, null); if (inst != null) return inst; } catch { }
                }
                // 4) Static fields that might hold instance
                var sfields = libType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                for (int i = 0; i < sfields.Length; i++)
                {
                    try { var v = sfields[i].GetValue(null); if (v != null && libType.IsInstanceOfType(v)) return v; } catch { }
                }
                return null;
            }
            catch { return null; }
        }

        public static object[] GetAllTraitDefinitions()
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                var lib = ResolveMonsterTraitLibrary();
                var res = new System.Collections.Generic.List<object>();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
                var fields = libType.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    object col = null;
                    try { col = f.IsStatic ? f.GetValue(null) : (lib != null ? f.GetValue(lib) : null); } catch { col = null; }
                    var en = col as System.Collections.IEnumerable;
                    if (en != null && !(col is string)) foreach (var d in en) if (d != null) res.Add(d);
                    else if (col != null) res.Add(col);
                }
                // Methods that enumerate all
                var methods = libType.GetMethods(flags);
                for (int i = 0; i < methods.Length; i++)
                {
                    var m = methods[i];
                    if (m.GetParameters().Length == 0 && m.ReturnType != typeof(void) && (m.Name.ToLowerInvariant().IndexOf("enumerate") >= 0 || m.Name.ToLowerInvariant().IndexOf("getall") >= 0))
                    {
                        try
                        {
                            var col = m.IsStatic ? m.Invoke(null, null) : (lib != null ? m.Invoke(lib, null) : null);
                            var en = col as System.Collections.IEnumerable; if (en == null || col is string) continue;
                            foreach (var d in en) if (d != null) res.Add(d);
                        }
                        catch { }
                    }
                }
                return DedupTraitDefinitionsByCode(FilterToDefinitionLike(res.ToArray()));
            }
            catch { }
            return new object[0];
        }

        public static object[] GetAllBloodlineTraitDefinitionsForAllSpecies()
        {
            try
            {
                var lib = TeamNimbus.CloudMeadow.Managers.GameManager.MonsterTraitLibrary;
                if (lib == null)
                {
                    LogBuffer.Add("Bloodline: MonsterTraitLibrary is null");
                    return new object[0];
                }

                var list = new System.Collections.Generic.List<object>(256);
                var speciesValues = System.Enum.GetValues(typeof(TeamNimbus.CloudMeadow.Monsters.FarmableSpecies));
                int speciesWithTraits = 0;
                
                for (int i = 0; i < speciesValues.Length; i++)
                {
                    var s = (TeamNimbus.CloudMeadow.Monsters.FarmableSpecies)speciesValues.GetValue(i);
                    try
                    {
                        var resolveMethod = ReflectionUtil.GetPrivateMethod(lib, "ResolveMonsterTraitsByType");
                        if (resolveMethod == null)
                        {
                            LogBuffer.Add("Bloodline: ResolveMonsterTraitsByType method not found");
                            break;
                        }

                        var traitsByType = resolveMethod.Invoke(lib, new object[] { s });
                        if (traitsByType != null)
                        {
                            var tType = traitsByType.GetType();
                            var fields = tType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                            // Try multiple field name variations
                            System.Array statLimit = null;
                            System.Array otherBlood = null;
                            
                            foreach (var field in fields)
                            {
                                if (field.Name.IndexOf("StatLimit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    statLimit = field.GetValue(traitsByType) as System.Array;
                                }
                                else if (field.Name.IndexOf("Bloodline", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                                         field.Name.IndexOf("OtherBlood", System.StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    otherBlood = field.GetValue(traitsByType) as System.Array;
                                }
                            }
                            
                            int beforeCount = list.Count;
                            AppendTraitArray(list, statLimit);
                            AppendTraitArray(list, otherBlood);
                            
                            if (list.Count > beforeCount)
                            {
                                speciesWithTraits++;
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Plugin.Log.LogWarning("GetAllBloodlineTraitDefinitionsForAllSpecies failed for " + s + ": " + e.Message);
                    }
                }
                
                // de-dup by reference
                var uniq = new System.Collections.Generic.List<object>(list.Count);
                var seen = new System.Collections.Generic.HashSet<object>();
                for (int i = 0; i < list.Count; i++) { var o = list[i]; if (o != null && !seen.Contains(o)) { seen.Add(o); uniq.Add(o); } }
                
                LogBuffer.Add("Bloodline traits found: " + uniq.Count + " from " + speciesWithTraits + " species");
                return uniq.ToArray();
            }
            catch (System.Exception e) 
            { 
                Plugin.Log.LogWarning("GetAllBloodlineTraitDefinitionsForAllSpecies failed: " + e.Message);
                LogBuffer.Add("Bloodline error: " + e.Message);
            }
            return new object[0];
        }

        private static void AppendTraitArray(System.Collections.Generic.List<object> list, System.Array arr)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++) { var v = arr.GetValue(i); if (v != null) list.Add(v); }
        }

        public static System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<object>> GroupBloodlineTraitsBySpecies(object[] allBloodline)
        {
            var map = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<object>>(16, System.StringComparer.OrdinalIgnoreCase);
            try
            {
                var lib = TeamNimbus.CloudMeadow.Managers.GameManager.MonsterTraitLibrary;
                var speciesValues = System.Enum.GetValues(typeof(TeamNimbus.CloudMeadow.Monsters.FarmableSpecies));
                for (int i = 0; i < speciesValues.Length; i++)
                {
                    var s = (TeamNimbus.CloudMeadow.Monsters.FarmableSpecies)speciesValues.GetValue(i);
                    string speciesName = s.ToString();
                    var list = new System.Collections.Generic.List<object>();
                    try
                    {
                        var traitsByType = ReflectionUtil.GetPrivateMethod(lib, "ResolveMonsterTraitsByType").Invoke(lib, new object[] { s });
                        if (traitsByType != null)
                        {
                            var tType = traitsByType.GetType();
                            var statLimit = tType.GetField("StatLimitTraits").GetValue(traitsByType) as System.Array;
                            var otherBlood = tType.GetField("OtherBloodlineTraits").GetValue(traitsByType) as System.Array;
                            AppendTraitArray(list, statLimit); AppendTraitArray(list, otherBlood);
                        }
                    }
                    catch { }
                    map[speciesName] = list;
                }
                return map;
            }
            catch { }
            return map;
        }
        

        public static object[] GetUniversalTraitDefinitions()
        {
            var all = GetAllTraitDefinitions();
            var uni = FilterTraitDefinitionsBySource(all, "Universal");
            return DedupTraitDefinitionsByCode(uni);
        }

        public static object[] GetSpeciesTraitDefinitionsForAllSpeciesUI()
        {
            return GetTraitDefinitionsByKindForUI(MonsterTraitKind.Species);
        }

        public static object[] GetStatLimitTraitDefinitionsForAllSpeciesUI()
        {
            return GetTraitDefinitionsByKindForUI(MonsterTraitKind.StatLimit);
        }

        public static object[] GetBloodlineTraitDefinitionsForAllSpeciesUI()
        {
            // Use the dedicated method that properly collects bloodline traits from all species
            var all = GetAllBloodlineTraitDefinitionsForAllSpecies();
            return DedupTraitDefinitionsByCode(all);
        }

        private static object[] GetTraitDefinitionsByKindForUI(MonsterTraitKind kind)
        {
            try
            {
                var all = GetAllTraitDefinitions();
                var list = new System.Collections.Generic.List<object>();
                for (int i = 0; i < all.Length; i++)
                {
                    var d = all[i];
                    if (d == null) continue;
                    if (ResolveMonsterTraitKind(d) == kind) list.Add(d);
                }
                return DedupTraitDefinitionsByCode(list.ToArray());
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("GetTraitDefinitionsByKindForUI failed: " + e.Message);
            }
            return new object[0];
        }

        private static object[] DedupTraitDefinitionsByCode(object[] defs)
        {
            try
            {
                var list = new System.Collections.Generic.List<object>();
                var seen = new System.Collections.Generic.HashSet<string>();
                for (int i = 0; i < defs.Length; i++)
                {
                    var d = defs[i]; if (d == null) continue;
                    var codeObj = SafeProp(d, "Code") ?? SafeProp(d, "TraitCode");
                    var nameObj = SafeProp(d, "Name") ?? SafeProp(d, "DisplayName");
                    string key = codeObj != null ? codeObj.ToString() : (nameObj != null ? nameObj.ToString() : d.ToString());
                    if (string.IsNullOrEmpty(key)) continue;
                    if (seen.Add(key)) list.Add(d);
                }
                return list.ToArray();
            }
            catch { }
            return defs;
        }

        public static object[] GetTraitDefinitionsForMonster(object monster)
        {
            try
            {
                string speciesName = GetMonsterSpecies(monster);
                var bloodline = GetTraitDefinitionsForSpecies(speciesName);
                var speciesTraits = GetSpeciesTraitDefinitionsForSpecies(speciesName);
                var universal = GetUniversalTraitDefinitions();
                var allBloodline = GetAllBloodlineTraitDefinitionsForAllSpecies();
                // Merge unique by reference
                var list = new System.Collections.Generic.List<object>();
                for (int i = 0; i < bloodline.Length; i++) if (bloodline[i] != null && list.IndexOf(bloodline[i]) < 0) list.Add(bloodline[i]);
                for (int i = 0; i < speciesTraits.Length; i++) if (speciesTraits[i] != null && list.IndexOf(speciesTraits[i]) < 0) list.Add(speciesTraits[i]);
                for (int i = 0; i < universal.Length; i++) if (universal[i] != null && list.IndexOf(universal[i]) < 0) list.Add(universal[i]);
                for (int i = 0; i < allBloodline.Length; i++) if (allBloodline[i] != null && list.IndexOf(allBloodline[i]) < 0) list.Add(allBloodline[i]);
                // Include singleton definitions from the library. This covers special
                // bloodline traits that are not stored in the per-species arrays.
                var all = GetAllTraitDefinitions();
                for (int i = 0; i < all.Length; i++)
                {
                    var def = all[i];
                    if (def == null || list.IndexOf(def) >= 0) continue;
                    var kind = ResolveMonsterTraitKind(def);
                    if (kind == MonsterTraitKind.Universal || kind == MonsterTraitKind.Bloodline) list.Add(def);
                }
                return DedupTraitDefinitionsByCode(list.ToArray());
            }
            catch { }
            return new object[0];
        }

        public static bool IsBloodlineTrait(object traitDefinition)
        {
            // Use the existing kind resolver which properly identifies bloodline traits
            return ResolveMonsterTraitKind(traitDefinition) == MonsterTraitKind.Bloodline;
        }

        public static object[] SortTraitsByPriority(object[] traits)
        {
            if (traits == null || traits.Length <= 1) return traits;
            var list = new System.Collections.Generic.List<object>(traits);
            list.Sort((a, b) =>
            {
                int priorityA = GetTraitPriority(a);
                int priorityB = GetTraitPriority(b);
                if (priorityA != priorityB) return priorityA.CompareTo(priorityB);
                // Same priority: sort by name
                string nameA = GetTraitDisplayName(a) ?? string.Empty;
                string nameB = GetTraitDisplayName(b) ?? string.Empty;
                return string.Compare(nameA, nameB, System.StringComparison.OrdinalIgnoreCase);
            });
            return list.ToArray();
        }

        private static int GetTraitPriority(object trait)
        {
            if (trait == null) return 999;
            // Bloodline first
            if (IsBloodlineTrait(trait)) return 0;
            // Then by quality: Rare, Uncommon, Negative, others
            string quality = GetTraitQuality(trait);
            if (string.IsNullOrEmpty(quality)) return 999;
            string q = quality.ToLowerInvariant();
            if (q.Contains("rare")) return 1;
            if (q.Contains("uncommon")) return 2;
            if (q == "common") return 3;
            if (q.Contains("negative")) return 4;
            return 5;
        }

        private static object GetTraitDefinitionFromInstance(object traitInstance)
        {
            var def = SafeProp(traitInstance, "TraitDefinition");
            if (def != null) return def;
            // Some instances may expose Code or inner definition differently
            return traitInstance;
        }

        private static bool IsTraitInstanceLike(object o)
        {
            try
            {
                var t = o.GetType();
                // Instance-like: has TraitDefinition and Grade/Level but is not a definition type
                bool hasDef = t.GetProperty("TraitDefinition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null;
                bool hasGrade = t.GetProperty("Grade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null ||
                                t.GetProperty("Level", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null;
                bool nameLooksLikeDef = (t.FullName ?? t.Name).IndexOf("Definition", System.StringComparison.OrdinalIgnoreCase) >= 0;
                return hasDef && hasGrade && !nameLooksLikeDef;
            }
            catch { return true; }
        }

        private static string GetTraitSourceString(object obj)
        {
            try
            {
                var def = UnwrapTraitDefinition(obj);
                var src = SafeProp(def, "TraitSource");
                return src != null ? src.ToString() : string.Empty;
            }
            catch { return string.Empty; }
        }

        // Public wrapper for UI usage
        public static string GetTraitSourceForUI(object obj)
        {
            if (IsBloodlineTrait(obj)) return "Bloodline";
            return GetTraitSourceString(obj);
        }

        public static string GetTraitBadgeForUI(object obj)
        {
            try
            {
                string grade = GetTraitGradeLabelForUI(obj);
                string quality = GetTraitQualityLabelForUI(obj);
                string result = string.Empty;
                if (!string.IsNullOrEmpty(grade) && !string.IsNullOrEmpty(quality)) result = grade + " / " + quality;
                else if (!string.IsNullOrEmpty(grade)) result = grade;
                else if (!string.IsNullOrEmpty(quality)) result = quality;
                
                // Add [Bloodline] tag for bloodline traits
                if (IsBloodlineTrait(obj))
                {
                    result = string.IsNullOrEmpty(result) ? "[Bloodline]" : result + " / [Bloodline]";
                }
                return result;
            }
            catch { }
            return string.Empty;
        }

        private static string GetTraitGradeLabelForUI(object obj)
        {
            try
            {
                var def = UnwrapTraitDefinition(obj);
                int grade = SafeIntAny(obj, "Grade", "Level", "CurrentLevel");
                if (grade <= 0) grade = SafeIntAny(def, "Grade", "Level", "CurrentLevel");
                if (grade <= 0) grade = SafeIntAny(obj, "MaxGrade", "MaxLevel", "Cap");
                if (grade <= 0) grade = SafeIntAny(def, "MaxGrade", "MaxLevel", "Cap");
                if (grade <= 0) return string.Empty;
                return ToRomanNumeral(grade);
            }
            catch { return string.Empty; }
        }

        private static string GetTraitQualityLabelForUI(object obj)
        {
            try
            {
                var def = UnwrapTraitDefinition(obj);
                object quality = SafeProp(def, "TraitQuality") ?? SafeProp(def, "Quality") ?? SafeProp(def, "Rarity") ?? SafeProp(def, "Tier")
                               ?? SafeProp(obj, "TraitQuality") ?? SafeProp(obj, "Quality") ?? SafeProp(obj, "Rarity") ?? SafeProp(obj, "Tier");
                if (quality == null) return string.Empty;

                string text = quality.ToString();
                if (string.IsNullOrEmpty(text)) return string.Empty;

                int numeric;
                if (int.TryParse(text, out numeric))
                {
                    if (numeric <= 0) return "Gray";
                    if (numeric == 1) return "Silver";
                    return "Gold";
                }

                if (text.IndexOf("Rare", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Legend", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Epic", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Gold";
                }

                if (text.IndexOf("Uncommon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Silver", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Silver";
                }

                if (text.IndexOf("Common", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Negative", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Gray", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Grey", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "Gray";
                }

                return text;
            }
            catch { return string.Empty; }
        }

        private static int SafeIntAny(object o, params string[] names)
        {
            try
            {
                for (int i = 0; i < names.Length; i++)
                {
                    var v = SafeProp(o, names[i]);
                    if (v == null) continue;
                    try { return Convert.ToInt32(v); } catch { }
                }
            }
            catch { }
            return 0;
        }

        private static string ToRomanNumeral(int value)
        {
            if (value <= 0) return string.Empty;
            if (value == 1) return "I";
            if (value == 2) return "II";
            if (value == 3) return "III";
            if (value == 4) return "IV";
            if (value == 5) return "V";
            if (value == 6) return "VI";
            if (value == 7) return "VII";
            if (value == 8) return "VIII";
            if (value == 9) return "IX";
            if (value == 10) return "X";
            return value.ToString();
        }

        private static object UnwrapTraitDefinition(object obj)
        {
            var def = obj;
            try
            {
                var wrapProp = def != null ? def.GetType().GetProperty("TraitDefinition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) : null;
                if (wrapProp != null)
                {
                    var inner = wrapProp.GetValue(def, null);
                    if (inner != null) def = inner;
                }
            }
            catch { }
            return def;
        }

        private static bool TryAddToNamedCollection(object monster, object inst, bool isUniversal)
        {
            try
            {
                var mType = monster.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = mType.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    var n = p.Name.ToLowerInvariant();
                    if (n.IndexOf("trait") < 0) continue;
                    bool universalLike = n.IndexOf("universal") >= 0 || n.IndexOf("generic") >= 0;
                    bool bloodlineLike = n.IndexOf("bloodline") >= 0 || n.IndexOf("species") >= 0 || n.IndexOf("lineage") >= 0;
                    if ((isUniversal && universalLike) || (!isUniversal && bloodlineLike))
                    {
                        var col = p.GetValue(monster, null);
                        if (col == null || col is string) continue;
                        var add = col.GetType().GetMethod("Add");
                        if (add != null && add.GetParameters().Length == 1)
                        { add.Invoke(col, new object[] { inst }); Diag("TryAddToNamedCollection property: " + p.Name); return true; }
                    }
                }
                var fields = mType.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    var n = f.Name.ToLowerInvariant();
                    if (n.IndexOf("trait") < 0) continue;
                    bool universalLike = n.IndexOf("universal") >= 0 || n.IndexOf("generic") >= 0;
                    bool bloodlineLike = n.IndexOf("bloodline") >= 0 || n.IndexOf("species") >= 0 || n.IndexOf("lineage") >= 0;
                    if ((isUniversal && universalLike) || (!isUniversal && bloodlineLike))
                    {
                        var col = f.GetValue(monster);
                        if (col == null || col is string) continue;
                        var add = col.GetType().GetMethod("Add");
                        if (add != null && add.GetParameters().Length == 1)
                        { add.Invoke(col, new object[] { inst }); Diag("TryAddToNamedCollection field: " + f.Name); return true; }
                    }
                }
            }
            catch { }
            return false;
        }

        public static string[] GetAvailablePigments()
        {
            // Use MonsterPalette enum from Game.dll (actual pigment selector), not any Palette UI enums
            try
            {
                var t = Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette, Game", false) ?? Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette", false);
                if (t != null && t.IsEnum)
                {
                    return Enum.GetNames(t);
                }
            }
            catch { }
            // Fallback: attempt older names
            try
            {
                string[] typeNames = { "TeamNimbus.CloudMeadow.Monsters.Pigment", "Pigment" };
                for (int i = 0; i < typeNames.Length; i++)
                {
                    var t2 = Type.GetType(typeNames[i] + ", Game", false) ?? Type.GetType(typeNames[i], false);
                    if (t2 != null && t2.IsEnum) return Enum.GetNames(t2);
                }
            }
            catch { }
            // Minimal safe fallback
            return new string[] { "Cat", "Holstaur", "Centaur", "Harpy", "Wolf", "Demon", "Dragon", "Lamia", "Chimera", "Cyclops", "Crab", "Mermaid" };
        }

        public static string GetMonsterPigment(object monster)
        {
            // Prefer MonsterCharacterStats.Palette (MonsterPalette enum)
            try
            {
                var t = monster.GetType();
                var pPalette = t.GetProperty("Palette", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pPalette != null)
                {
                    var v = pPalette.GetValue(monster, null);
                    return v != null ? v.ToString() : "-";
                }
            }
            catch { }
            // Fallback legacy names
            try
            {
                var t = monster.GetType();
                string[] names = { "Pattern", "ColorPattern", "Skin", "Variant" };
                for (int i = 0; i < names.Length; i++)
                {
                    var p = t.GetProperty(names[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        var v = p.GetValue(monster, null);
                        return v != null ? v.ToString() : "-";
                    }
                }
            }
            catch { }
            return "-";
        }

        public static void SetMonsterPigment(object monster, string pigmentName)
        {
            // Correct way: call ChangeMonsterPalette(MonsterPalette) so SpriteModifiedEvent is triggered
            try
            {
                var t = monster.GetType();
                var paletteEnum = Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette, Game", false)
                                   ?? Type.GetType("TeamNimbus.CloudMeadow.Monsters.MonsterPalette", false);
                if (paletteEnum != null && paletteEnum.IsEnum)
                {
                    object enumVal = null;
                    try { enumVal = Enum.Parse(paletteEnum, pigmentName, true); } catch { }
                    if (enumVal == null)
                    {
                        var namesEnum = Enum.GetNames(paletteEnum);
                        for (int n = 0; n < namesEnum.Length; n++)
                        {
                            if (namesEnum[n].IndexOf(pigmentName, StringComparison.OrdinalIgnoreCase) >= 0) { enumVal = Enum.Parse(paletteEnum, namesEnum[n]); break; }
                        }
                    }
                    if (enumVal != null)
                    {
                        // Preferred: call ChangeMonsterPalette(newPalette)
                        var change = t.GetMethod("ChangeMonsterPalette", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (change != null && change.GetParameters().Length == 1)
                        {
                            change.Invoke(monster, new object[] { enumVal });
                            RefreshMonsterAfterTrait(monster);
                            return;
                        }
                        // Fallback: set private field 'palette' directly and trigger SpriteModifiedEvent if present
                        var fPalette = t.GetField("palette", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fPalette != null)
                        {
                            fPalette.SetValue(monster, enumVal);
                            // Try to trigger visual refresh
                            var pEvent = t.GetProperty("SpriteModifiedEvent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            var spriteEvt = pEvent != null ? pEvent.GetValue(monster, null) : null;
                            if (spriteEvt == null)
                            {
                                var fEvent = t.GetField("SpriteModifiedEvent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (fEvent != null) spriteEvt = fEvent.GetValue(monster);
                            }
                            if (spriteEvt != null)
                            {
                                var trig = spriteEvt.GetType().GetMethod("Trigger", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (trig != null) { try { trig.Invoke(spriteEvt, null); } catch { } }
                            }
                            RefreshMonsterAfterTrait(monster);
                            return;
                        }
                    }
                }
            }
            catch { }

            // Legacy fallbacks (older builds)
            try
            {
                var t = monster.GetType();
                string[] names = { "Pattern", "ColorPattern", "Skin", "Variant" };
                for (int i = 0; i < names.Length; i++)
                {
                    var p = t.GetProperty(names[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        var pt = p.PropertyType;
                        if (pt.IsEnum)
                        {
                            object val = null;
                            try { val = Enum.Parse(pt, pigmentName, true); } catch { }
                            if (val == null)
                            {
                                var namesEnum = Enum.GetNames(pt);
                                for (int n = 0; n < namesEnum.Length; n++)
                                {
                                    if (namesEnum[n].IndexOf(pigmentName, StringComparison.OrdinalIgnoreCase) >= 0) { val = Enum.Parse(pt, namesEnum[n]); break; }
                                }
                            }
                            if (val != null) { p.SetValue(monster, val, null); RefreshMonsterAfterTrait(monster); return; }
                        }
                        else if (pt == typeof(string))
                        {
                            p.SetValue(monster, pigmentName, null); RefreshMonsterAfterTrait(monster); return;
                        }
                    }
                }
            }
            catch { }
        }

        public static void DumpMonstersDebug()
        {
            try
            {
                var dir = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx");
                dir = System.IO.Path.Combine(dir, "plugins");
                dir = System.IO.Path.Combine(dir, "CloudMeadowCreativeMode");
                var path = System.IO.Path.Combine(dir, "tmp_rovodev_monsters_dump.log");
                System.IO.File.WriteAllText(path, "=== Monster Debug Dump (manual) ===\n");
                var s = GameManager.Status;
                var list = s != null ? s.EnumerateActiveMonsters() : null;
                if (list == null) { System.IO.File.AppendAllText(path, "No active monsters.\n"); Banner("No monsters to dump"); return; }
                int idx = 1;
                foreach (var m in list)
                {
                    if (m == null) continue;
                    System.IO.File.AppendAllText(path, "# Monster " + (idx++) + ": " + m.Name + " (" + m.FarmableSpecies + ")\n");
                    ReflectionUtil.DumpObject(m, (l) => { try { System.IO.File.AppendAllText(path, l + "\n"); } catch { } }, 1, 300);
                    TryDumpAppearanceTo(path, m);
                }
                System.IO.File.AppendAllText(path, "=== End of dump ===\n");
                Banner("Monsters dumped to log file");
            }
            catch (Exception e) { Plugin.Log.LogWarning("DumpMonstersDebug failed: " + e.Message); }
        }

        private static void TryDumpAppearanceTo(string path, object monster)
        {
            try
            {
                var t = monster.GetType();
                string[] keys = { "Pigment", "Pigments", "Palette", "Color", "ColorPattern", "Variant", "Skin", "Appearance", "Visual" };
                for (int i = 0; i < keys.Length; i++)
                {
                    var p = t.GetProperty(keys[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (p != null)
                    {
                        object v = null; try { v = p.GetValue(monster, null); } catch { }
                        try { System.IO.File.AppendAllText(path, "  * " + keys[i] + ": " + (v != null ? v.ToString() : "null") + "\n"); } catch { }
                        if (v != null && !(v is string) && !v.GetType().IsPrimitive)
                        {
                            ReflectionUtil.DumpObject(v, (l) => { try { System.IO.File.AppendAllText(path, "    " + l + "\n"); } catch { } }, 1, 80);
                        }
                    }
                }
            }
            catch { }
        }

        private static void TryAddToAnyCollection(object monster, object inst)
        {
            try
            {
                var mType = monster.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = mType.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i]; if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var col = p.GetValue(monster, null);
                    if (col == null || col is string) continue;
                    var add = col.GetType().GetMethod("Add");
                    if (add != null && add.GetParameters().Length == 1)
                    { add.Invoke(col, new object[] { inst }); return; }
                }
                var fields = mType.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i]; if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var col = f.GetValue(monster);
                    if (col == null || col is string) continue;
                    var add = col.GetType().GetMethod("Add");
                    if (add != null && add.GetParameters().Length == 1)
                    { add.Invoke(col, new object[] { inst }); return; }
                }
            }
            catch { }
        }

        private static int GetTraitCapacity(object monster, bool universal)
        {
            try
            {
                // Heuristics: try fields/properties that look like capacity/slots
                var mType = monster.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                string[] keys = universal ? new[] { "UniversalTraitSlots", "UniversalTraitsCap", "MaxUniversalTraits", "GenericTraitSlots" }
                                          : new[] { "BloodlineTraitSlots", "SpeciesTraitSlots", "BloodlineTraitsCap", "MaxBloodlineTraits" };
                for (int k = 0; k < keys.Length; k++)
                {
                    var p = mType.GetProperty(keys[k], flags); if (p != null) { var v = p.GetValue(monster, null); return Convert.ToInt32(v); }
                    var f = mType.GetField(keys[k], flags); if (f != null) { var v = f.GetValue(monster); return Convert.ToInt32(v); }
                }
            }
            catch { }
            // Fallback defaults (from your description): bloodline ~8 (4 preset + 4 earned), universal ~10
            return universal ? 10 : 8;
        }

        private static int CountMonsterTraits(object monster, bool universal)
        {
            try
            {
                var traits = GetMonsterTraits(monster);
                int count = 0;
                for (int i = 0; i < traits.Length; i++)
                {
                    var inst = traits[i];
                    var bucket = GetTraitBucketForMonster(monster, inst);
                    bool isUni = string.Equals(bucket, "Universal", StringComparison.OrdinalIgnoreCase);
                    bool isBloodline = string.Equals(bucket, "Bloodline", StringComparison.OrdinalIgnoreCase);
                    if (universal && isUni) count++;
                    if (!universal && isBloodline) count++;
                }
                return count;
            }
            catch { return 0; }
        }

        private static bool TraitDefinitionsEqual(object a, object b)
        {
            if (a == null || b == null) return false;
            if (object.ReferenceEquals(a, b)) return true;
            // fallback: compare Code/Name
            var ac = SafeProp(a, "Code") ?? SafeProp(a, "Name") ?? SafeProp(a, "DisplayName");
            var bc = SafeProp(b, "Code") ?? SafeProp(b, "Name") ?? SafeProp(b, "DisplayName");
            if (ac != null && bc != null) return ac.ToString() == bc.ToString();
            return false;
        }

        public static bool MonsterHasTrait(object monster, object traitDefinition)
        {
            try
            {
                // Unwrap wrapper to actual definition if needed
                var td = traitDefinition;
                var wrapProp = td != null ? td.GetType().GetProperty("TraitDefinition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) : null;
                if (wrapProp != null)
                {
                    try { var inner = wrapProp.GetValue(td, null); if (inner != null) td = inner; } catch { }
                }
                var traits = GetMonsterTraits(monster);
                for (int i = 0; i < traits.Length; i++)
                {
                    var inst = traits[i]; if (inst == null) continue;
                    var def = GetTraitDefinitionFromInstance(inst);
                    if (TraitDefinitionsEqual(def, td)) return true;
                }
            }
            catch { }
            return false;
        }

        public static bool TryAddTraitToMonster(object monster, object traitDefinition, int grade)
        {
            try
            {
                if (MonsterHasTrait(monster, traitDefinition)) { Banner("Trait already present"); return false; }
                AddTraitToMonster(monster, traitDefinition, grade);
                Banner("Trait added");
                return true;
            }
            catch (Exception e) { Plugin.Log.LogWarning("TryAddTraitToMonster failed: " + e.Message); }
            return false;
        }

        public static int GetTraitMaxGrade(object traitInstance)
        {
            try
            {
                var t = traitInstance.GetType();
                var p = t.GetProperty("MaxGrade") ?? t.GetProperty("MaxLevel") ?? t.GetProperty("Cap");
                if (p != null) { var v = p.GetValue(traitInstance, null); return Convert.ToInt32(v); }
            }
            catch { }
            return 5; // default
        }

        public static void SetTraitGrade(object traitInstance, int grade)
        {
            try
            {
                if (traitInstance == null) return;
                int target = Mathf.Clamp(grade, 1, 5);
                var t = traitInstance.GetType();

                // Preferred path: Increase/Reduce loops using readable Grade/Level
                var pGet = t.GetProperty("Grade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                           ?? t.GetProperty("Level", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var miInc = t.GetMethod("IncreaseGrade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                           ?? t.GetMethod("LevelUp", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var miDec = t.GetMethod("ReduceGrade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                           ?? t.GetMethod("LevelDown", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pGet != null && (miInc != null || miDec != null))
                {
                    int cur = Convert.ToInt32(pGet.GetValue(traitInstance, null));
                    int safety = 20;
                    while (cur < target && miInc != null && safety-- > 0)
                    {
                        miInc.Invoke(traitInstance, null);
                        cur = Convert.ToInt32(pGet.GetValue(traitInstance, null));
                    }
                    while (cur > target && miDec != null && safety-- > 0)
                    {
                        miDec.Invoke(traitInstance, null);
                        cur = Convert.ToInt32(pGet.GetValue(traitInstance, null));
                    }
                    return;
                }

                // Direct setter methods
                string[] setterNames = { "SetGrade", "SetLevel", "ApplyGrade", "SetToLevel", "ForceSetGrade", "ForceSetLevel" };
                for (int i = 0; i < setterNames.Length; i++)
                {
                    var m = t.GetMethod(setterNames[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (m != null && m.GetParameters().Length == 1)
                    {
                        try { m.Invoke(traitInstance, new object[] { target }); return; } catch { }
                    }
                }

                // Try writable property Grade/Level
                var pSet = t.GetProperty("Grade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                         ?? t.GetProperty("Level", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pSet != null && pSet.CanWrite)
                {
                    try { pSet.SetValue(traitInstance, target, null); return; } catch { }
                }

                // Try backing fields
                string[] fldNames = { "grade", "_grade", "m_Grade", "level", "_level", "m_Level" };
                for (int i = 0; i < fldNames.Length; i++)
                {
                    var f = t.GetField(fldNames[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (f != null)
                    {
                        try { f.SetValue(traitInstance, target); return; } catch { }
                    }
                }

                Plugin.Log.LogWarning("SetTraitGrade: no applicable method/property found");
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetTraitGrade failed: " + e.Message); }
        }

        private static void RefreshMonsterAfterTrait(object monster)
        {
            try
            {
                if (monster == null) return;
                Diag("RefreshMonsterAfterTrait for " + monster);
                var t = monster.GetType();
                string[] mnames = {
                    "RecalculateStats", "RefreshStats", "RefreshDerivedStats", "ApplyTraits", "RebuildTraitEffects",
                    "ReapplyTraits", "Recompute", "OnTraitsChanged", "UpdateModifiers", "Recalculate"
                };
                for (int i = 0; i < mnames.Length; i++)
                {
                    var m = t.GetMethod(mnames[i], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (m != null && m.GetParameters().Length == 0)
                    {
                        try { m.Invoke(monster, null); } catch { }
                    }
                }
            }
            catch { }
        }

        public static void MaxTraitGrade(object traitInstance)
        {
            try
            {
                if (traitInstance == null) return;
                int max = GetTraitMaxGrade(traitInstance);
                SetTraitGrade(traitInstance, max);
            }
            catch (Exception e) { Plugin.Log.LogWarning("MaxTraitGrade failed: " + e.Message); }
        }

        public static object[] FilterTraitDefinitionsBySource(object[] defs, string sourceContains)
        {
            var list = new System.Collections.Generic.List<object>();
            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i]; if (d == null) continue;
                try
                {
                    var t = d.GetType();
                    // Prefer property on definition type
                    var p = t.GetProperty("TraitSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    object val = null; if (p != null) val = p.GetValue(d, null);
                    // Some libraries hold a nested Definition object
                    if (val == null)
                    {
                        var defP = t.GetProperty("TraitDefinition", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (defP != null)
                        {
                            var inner = defP.GetValue(d, null);
                            if (inner != null)
                            {
                                var innerSrcP = inner.GetType().GetProperty("TraitSource", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (innerSrcP != null) val = innerSrcP.GetValue(inner, null);
                            }
                        }
                    }
                    var s = val != null ? val.ToString() : string.Empty;
                    if (s.IndexOf(sourceContains, StringComparison.OrdinalIgnoreCase) >= 0) list.Add(d);
                }
                catch { }
            }
            return list.ToArray();
        }

        private static object[] FilterToDefinitionLike(object[] arr)
        {
            var list = new System.Collections.Generic.List<object>();
            for (int i = 0; i < arr.Length; i++)
            {
                var d = arr[i]; if (d == null) continue;
                try
                {
                    var t = d.GetType();
                    string tn = t.FullName != null ? t.FullName : t.Name;
                    // Exclude obvious instances with Grade/Level
                    if (SafeProp(d, "Grade") != null || SafeProp(d, "Level") != null) continue;
                    // Resolve TraitSource directly or via inner TraitDefinition
                    object src = SafeProp(d, "TraitSource");
                    if (src == null)
                    {
                        var inner = SafeProp(d, "TraitDefinition");
                        if (inner != null) src = SafeProp(inner, "TraitSource");
                    }
                    // Require name/code field
                    object name = SafeProp(d, "Name") ?? SafeProp(d, "DisplayName") ?? SafeProp(d, "Code");
                    if (name == null && SafeProp(d, "TraitDefinition") != null)
                    {
                        var inner = SafeProp(d, "TraitDefinition");
                        name = SafeProp(inner, "Name") ?? SafeProp(inner, "DisplayName") ?? SafeProp(inner, "Code");
                    }
                    bool typeOk = (tn != null && tn.IndexOf("Definition", System.StringComparison.OrdinalIgnoreCase) >= 0) || src != null;
                    if (typeOk && name != null)
                    {
                        list.Add(d);
                    }
                }
                catch { }
            }
            return list.ToArray();
        }

        private sealed class RefEqComparer : System.Collections.Generic.IEqualityComparer<object>
        {
            public new bool Equals(object x, object y) { return object.ReferenceEquals(x, y); }
            public int GetHashCode(object obj) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
        }

        private enum MonsterTraitKind
        {
            Unknown,
            Species,
            StatLimit,
            Bloodline,
            Universal
        }

        private static MonsterTraitKind ResolveMonsterTraitKind(object traitDefinition)
        {
            try
            {
                var def = UnwrapTraitDefinition(traitDefinition);
                if (def == null) return MonsterTraitKind.Unknown;

                var lib = GameManager.MonsterTraitLibrary;
                if (lib != null)
                {
                    var isSpecies = lib.GetType().GetMethod("IsSpeciesTrait", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (isSpecies != null)
                    {
                        try
                        {
                            var result = isSpecies.Invoke(lib, new object[] { def });
                            if (result is bool && (bool)result) return MonsterTraitKind.Species;
                        }
                        catch { }
                    }
                }

                var statLimitType = Type.GetType("TeamNimbus.CloudMeadow.Traits.BaseStatLimitTraitDefinition, Game");
                if (statLimitType != null && statLimitType.IsInstanceOfType(def)) return MonsterTraitKind.StatLimit;

                var src = GetTraitSourceString(def);
                if (src.IndexOf("universal", StringComparison.OrdinalIgnoreCase) >= 0) return MonsterTraitKind.Universal;
                return MonsterTraitKind.Bloodline;
            }
            catch { }
            return MonsterTraitKind.Unknown;
        }

        private static bool SetMonsterStatLimitTrait(object monster, object traitDefinition, int grade)
        {
            try
            {
                var def = UnwrapTraitDefinition(traitDefinition) as TeamNimbus.CloudMeadow.Traits.BaseTraitDefinition;
                if (def == null || monster == null) return false;

                object targetedStat = SafeProp(def, "TargetedStat");
                if (targetedStat == null) return false;

                string fieldName = null;
                string statName = targetedStat.ToString();
                if (string.Equals(statName, "Physique", StringComparison.OrdinalIgnoreCase)) fieldName = "physiqueLimitTrait";
                else if (string.Equals(statName, "Stamina", StringComparison.OrdinalIgnoreCase)) fieldName = "staminaLimitTrait";
                else if (string.Equals(statName, "Intuition", StringComparison.OrdinalIgnoreCase)) fieldName = "intuitionLimitTrait";
                else if (string.Equals(statName, "Swiftness", StringComparison.OrdinalIgnoreCase)) fieldName = "swiftnessLimitTrait";
                if (fieldName == null) return false;

                var trait = new TeamNimbus.CloudMeadow.Traits.TraitInstance(def, Mathf.Clamp(grade, 1, 5));
                var field = monster.GetType().GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field == null) return false;

                field.SetValue(monster, trait);
                RefreshMonsterAfterTrait(monster);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("SetMonsterStatLimitTrait failed: " + e.Message);
            }
            return false;
        }

        private static bool ResetMonsterStatLimitTrait(object monster, object traitInstance)
        {
            try
            {
                var m = monster as MonsterCharacterStats;
                if (m == null || traitInstance == null) return false;

                var def = GetTraitDefinitionFromInstance(traitInstance);
                var targetedStat = SafeProp(def, "TargetedStat");
                if (targetedStat == null) return false;

                foreach (var defaultDef in GameManager.MonsterTraitLibrary.EnumerateDefaultStatLimitTraitsForSpecies(m.FarmableSpecies))
                {
                    if (defaultDef != null && string.Equals(defaultDef.TargetedStat.ToString(), targetedStat.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return SetMonsterStatLimitTrait(monster, defaultDef, 1);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("ResetMonsterStatLimitTrait failed: " + e.Message);
            }
            return false;
        }

        public static void AddTraitToMonster(object monster, object traitDefinition, int grade)
        {
            try
            {
                var instType = Type.GetType("TeamNimbus.CloudMeadow.Traits.TraitInstance, Game");
                if (instType == null) { Plugin.Log.LogWarning("TraitInstance type not found"); return; }

                MonsterTraitKind kind = ResolveMonsterTraitKind(traitDefinition);
                bool isUniversal = kind == MonsterTraitKind.Universal;

                if (kind == MonsterTraitKind.Species)
                {
                    SetMonsterSpeciesTrait(monster, traitDefinition, grade);
                    return;
                }

                if (kind == MonsterTraitKind.StatLimit)
                {
                    SetMonsterStatLimitTrait(monster, traitDefinition, grade);
                    return;
                }

                // Capacity check
                int cur = CountMonsterTraits(monster, isUniversal);
                if (isUniversal)
                {
                    int capU = GetTraitCapacity(monster, true); // default 10 if unknown
                    if (capU > 0 && cur >= capU) { Banner("Universal trait slots full"); return; }
                }
                else
                {
                    // Bloodline: game limit is 4 extra bloodline traits
                    int totalBloodline = CountMonsterTraits(monster, false);
                    if (totalBloodline >= 4) { Banner("Bloodline trait slots full"); return; }
                }

                // Try factory first on definition or library
                object inst = null;
                var def = UnwrapTraitDefinition(traitDefinition);

                var defType = def != null ? def.GetType() : null;
                var createOnDef = defType != null ? (defType.GetMethod("CreateTraitInstance") ?? defType.GetMethod("CreateInstance")) : null;
                if (createOnDef != null)
                {
                    try { inst = createOnDef.GetParameters().Length == 0 ? createOnDef.Invoke(def, null) : createOnDef.Invoke(def, new object[] { grade }); } catch { inst = null; }
                }
                if (inst == null)
                {
                    var lib = ResolveMonsterTraitLibrary();
                    if (lib != null)
                    {
                        var m = lib.GetType().GetMethod("CreateTraitInstance") ?? lib.GetType().GetMethod("CreateInstance");
                        if (m != null)
                        {
                            try { inst = m.GetParameters().Length == 1 ? m.Invoke(lib, new object[] { def }) : m.Invoke(lib, new object[] { def, grade }); } catch { inst = null; }
                        }
                    }
                }

                // Fallback: create without calling ctor
                if (inst == null)
                {
                    inst = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(instType);
                    var init = instType.GetMethod("InitializeTraitDefinition");
                    if (init != null) init.Invoke(inst, new object[] { def });
                }

                // set grade/level using robust method (no direct setter required)
                try { SetTraitGrade(inst, grade); } catch { }

                // Prefer adding into the correct collection by source
                if (TryAddToNamedCollection(monster, inst, isUniversal)) { Diag("AddTraitToMonster path: named collection"); return; }

                // Otherwise fallback to dedicated API
                var mType = monster.GetType();
                var addTraitInst = mType.GetMethod("AddTraitInstance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (addTraitInst != null && addTraitInst.GetParameters().Length == 1)
                {
                    try { addTraitInst.Invoke(monster, new object[] { inst }); Diag("AddTraitToMonster path: AddTraitInstance"); return; } catch { }
                }
                var addTraitDef = mType.GetMethod("AddTrait", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (addTraitDef != null)
                {
                    var ps = addTraitDef.GetParameters();
                    try
                    {
                        if (ps.Length == 1) { addTraitDef.Invoke(monster, new object[] { def }); Diag("AddTraitToMonster path: AddTrait(def)"); return; }
                        if (ps.Length == 2) { addTraitDef.Invoke(monster, new object[] { def, grade }); Diag("AddTraitToMonster path: AddTrait(def, grade)"); return; }
                    }
                    catch { }
                }

                // As last resort: add to any trait collection
                Diag("AddTraitToMonster path: any trait collection");
                TryAddToAnyCollection(monster, inst);
            }
            catch (Exception e) { Plugin.Log.LogWarning("AddTraitToMonster failed: " + e.Message); }
        }

        public static void RemoveTraitFromMonster(object monster, object traitInstance)
        {
            try
            {
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var t = monster.GetType();
                var bucket = GetTraitBucketForMonster(monster, traitInstance);

                if (string.Equals(bucket, "Species", StringComparison.OrdinalIgnoreCase))
                {
                    var speciesField = t.GetField("_speciesTrait", flags);
                    if (speciesField != null)
                    {
                        Diag("RemoveTraitFromMonster path: species field");
                        speciesField.SetValue(monster, null);
                        SyncSpecialSpeciesTraitState(monster, null);
                        RefreshMonsterAfterTrait(monster);
                        return;
                    }
                }

                if (string.Equals(bucket, "StatLimit", StringComparison.OrdinalIgnoreCase))
                {
                    if (ResetMonsterStatLimitTrait(monster, traitInstance))
                    {
                        Diag("RemoveTraitFromMonster path: stat limit reset");
                        Banner("Stat limit reset");
                        return;
                    }
                    Banner("Stat limit reset failed");
                    return;
                }

                // Pre-pass: if Universal trait, remove directly from universalTraits field for reliability
                var defInitial = GetTraitDefinitionFromInstance(traitInstance);
                var src = GetTraitSourceString(defInitial);
                if (src.IndexOf("universal", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (TryRemoveFromListField(monster, "universalTraits", traitInstance, defInitial)) { Diag("RemoveTraitFromMonster path: universalTraits field"); RefreshMonsterAfterTrait(monster); return; }
                }

                // 1) Try dedicated API on monster
                var rmInst = t.GetMethod("RemoveTraitInstance", flags);
                if (rmInst != null && rmInst.GetParameters().Length == 1)
                {
                    try { rmInst.Invoke(monster, new object[] { traitInstance }); Diag("RemoveTraitFromMonster path: RemoveTraitInstance"); RefreshMonsterAfterTrait(monster); return; } catch { }
                }
                var rmDef = t.GetMethod("RemoveTrait", flags);
                if (rmDef != null)
                {
                    var def = defInitial;
                    var ps = rmDef.GetParameters();
                    try
                    {
                        if (ps.Length == 1) { rmDef.Invoke(monster, new object[] { def }); Diag("RemoveTraitFromMonster path: RemoveTrait(def)"); RefreshMonsterAfterTrait(monster); return; }
                        if (ps.Length == 2) { rmDef.Invoke(monster, new object[] { def, 0 }); Diag("RemoveTraitFromMonster path: RemoveTrait(def,0)"); RefreshMonsterAfterTrait(monster); return; }
                    }
                    catch { }
                }

                // 2) Try collections; remove exact instance
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i]; if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var col = p.GetValue(monster, null);
                    var rem = col != null ? col.GetType().GetMethod("Remove") : null;
                    if (rem != null && rem.GetParameters().Length == 1)
                    { try { rem.Invoke(col, new object[] { traitInstance }); RefreshMonsterAfterTrait(monster); return; } catch { } }
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i]; if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var col = f.GetValue(monster);
                    var rem = col != null ? col.GetType().GetMethod("Remove") : null;
                    if (rem != null && rem.GetParameters().Length == 1)
                    { try { rem.Invoke(col, new object[] { traitInstance }); RefreshMonsterAfterTrait(monster); return; } catch { } }
                }

                // 3) Remove by definition match
                var defMatch = defInitial;
                // scan collections and find item with same def
                for (int pass = 0; pass < 2; pass++)
                {
                    props = t.GetProperties(flags);
                    for (int i = 0; i < props.Length; i++)
                    {
                        var p = props[i]; if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        var col = p.GetValue(monster, null) as System.Collections.IEnumerable; if (col == null) continue;
                        object toRemove = null; var colObj = p.GetValue(monster, null);
                        foreach (var it in col)
                        {
                            if (TraitDefinitionsEqual(GetTraitDefinitionFromInstance(it), defMatch)) { toRemove = it; break; }
                        }
                        if (toRemove != null)
                        {
                            var rem = colObj.GetType().GetMethod("Remove");
                            if (rem != null) { try { rem.Invoke(colObj, new object[] { toRemove }); RefreshMonsterAfterTrait(monster); return; } catch { } }
                        }
                    }
                    fields = t.GetFields(flags);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var f = fields[i]; if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        var col = f.GetValue(monster) as System.Collections.IEnumerable; if (col == null) continue;
                        object toRemove = null; var colObj = f.GetValue(monster);
                        foreach (var it in col)
                        {
                            if (TraitDefinitionsEqual(GetTraitDefinitionFromInstance(it), defMatch)) { toRemove = it; break; }
                        }
                        if (toRemove != null)
                        {
                            var rem = colObj.GetType().GetMethod("Remove");
                            if (rem != null) { try { rem.Invoke(colObj, new object[] { toRemove }); RefreshMonsterAfterTrait(monster); return; } catch { } }
                        }
                    }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("RemoveTraitFromMonster failed: " + e.Message); }
        }

        // Remove from a private List<TraitInstance> field by name (e.g., "universalTraits")
        private static bool TryRemoveFromListField(object monster, string fieldName, object traitInstance, object defMatch)
        {
            try
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var f = monster.GetType().GetField(fieldName, flags);
                if (f == null) return false;
                var list = f.GetValue(monster);
                if (list == null) return false;
                var listType = list.GetType();
                var asEnumerable = list as System.Collections.IEnumerable;
                object toRemove = null;
                foreach (var it in asEnumerable)
                {
                    if (traitInstance != null && object.ReferenceEquals(it, traitInstance)) { toRemove = it; break; }
                    if (TraitDefinitionsEqual(GetTraitDefinitionFromInstance(it), defMatch)) { toRemove = it; break; }
                }
                if (toRemove != null)
                {
                    var rem = listType.GetMethod("Remove", new Type[] { toRemove.GetType() });
                    if (rem == null) rem = listType.GetMethod("Remove");
                    if (rem != null)
                    {
                        rem.Invoke(list, new object[] { toRemove });
                        Diag("TryRemoveFromListField success: " + fieldName);
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        // Cheats: Max all monsters loyalty
        public static void MaxAllMonstersLoyalty()
        {
            try
            {
                var list = GameManager.Status.EnumerateActiveMonsters();
                int cnt = 0;
                foreach (var m in list)
                {
                    try
                    {
                        var t = m.GetType();
                        var fLoyalty = t.GetField("loyalty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var fIsLoyal = t.GetField("isLoyal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var fDaysNotFed = t.GetField("daysNotFed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fLoyalty != null) fLoyalty.SetValue(m, 110);
                        if (fIsLoyal != null) fIsLoyal.SetValue(m, true);
                        if (fDaysNotFed != null) fDaysNotFed.SetValue(m, 0);
                        cnt++;
                    }
                    catch { }
                }
                Banner("Max Loyalty for " + cnt + " monsters");
            }
            catch (Exception e) { Plugin.Log.LogWarning("MaxAllMonstersLoyalty failed: " + e.Message); }
        }

        public static void RunSafeConsistencyAuditAndFix()
        {
            try
            {
                var dir = System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx");
                dir = System.IO.Path.Combine(dir, "plugins");
                dir = System.IO.Path.Combine(dir, "CloudMeadowCreativeMode");
                System.IO.Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, "consistency_report.txt");
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== Safe Consistency Audit ===");
                sb.AppendLine(DateTime.Now.ToString("u"));

                int issues = 0;
                int fixes = 0;
                var monsters = GameManager.Status != null ? GameManager.Status.EnumerateActiveMonsters() : null;
                if (monsters != null)
                {
                    foreach (var monster in monsters)
                    {
                        if (monster == null) continue;
                        int speciesCount = 0;
                        int statCount = 0;
                        int bloodCount = 0;
                        int uniCount = 0;
                        var traits = GetMonsterTraits(monster);
                        for (int i = 0; i < traits.Length; i++)
                        {
                            var bucket = GetTraitBucketForMonster(monster, traits[i]);
                            if (string.Equals(bucket, "Species", StringComparison.OrdinalIgnoreCase)) speciesCount++;
                            else if (string.Equals(bucket, "StatLimit", StringComparison.OrdinalIgnoreCase)) statCount++;
                            else if (string.Equals(bucket, "Universal", StringComparison.OrdinalIgnoreCase)) uniCount++;
                            else if (string.Equals(bucket, "Bloodline", StringComparison.OrdinalIgnoreCase)) bloodCount++;
                        }

                        sb.AppendLine(string.Format("{0} ({1}) -> species:{2} stat:{3} blood:{4} uni:{5}", monster.Name, monster.FarmableSpecies, speciesCount, statCount, bloodCount, uniCount));

                        if (monster.FarmableSpecies == FarmableSpecies.Chimera && speciesCount == 0)
                        {
                            var chimeraDefs = GetSpeciesTraitDefinitionsForSpecies("Chimera");
                            if (chimeraDefs.Length > 0 && SetMonsterSpeciesTrait(monster, chimeraDefs[0], 1))
                            {
                                fixes++;
                                issues++;
                                sb.AppendLine("  FIX: restored missing Chimera variant -> " + ReadStringFromTraitDefinition(chimeraDefs[0]));
                            }
                        }

                        if (statCount != 4)
                        {
                            issues++;
                            sb.AppendLine("  ISSUE: invalid stat-limit count");
                            foreach (var defaultDef in GameManager.MonsterTraitLibrary.EnumerateDefaultStatLimitTraitsForSpecies(monster.FarmableSpecies))
                            {
                                if (defaultDef != null) SetMonsterStatLimitTrait(monster, defaultDef, 1);
                            }
                            fixes++;
                            sb.AppendLine("  FIX: reapplied default stat-limit traits");
                        }

                        if (bloodCount > 4) sb.AppendLine("  WARN: bloodline count above limit");
                        if (uniCount > 10) sb.AppendLine("  WARN: universal count above limit");
                    }
                }

                var entries = GetInventoryEntries();
                sb.AppendLine("Inventory entries: " + entries.Length);
                for (int j = 0; j < entries.Length; j++)
                {
                    var entry = entries[j];
                    var def = GetEntryDefinition(entry) as BaseItemDefinition;
                    if (def == null) continue;
                    var q = GetEntryQuality(entry);
                    if (!def.ItemAvailableWithQuality(q))
                    {
                        issues++;
                        if (UpgradeEntryToMaxQuality(entry, false))
                        {
                            fixes++;
                            sb.AppendLine("  FIX ITEM: " + (SafeProp(def, "Code") ?? SafeProp(def, "Name") ?? def.ToString()) + " quality normalized");
                        }
                    }
                }

                sb.AppendLine(string.Format("Summary -> issues:{0} fixes:{1}", issues, fixes));
                System.IO.File.WriteAllText(path, sb.ToString());
                Banner("Audit complete. Fixed: " + fixes);
                LogBuffer.Add("Consistency audit written: " + path);
            }
            catch (Exception e) { Plugin.Log.LogWarning("RunSafeConsistencyAuditAndFix failed: " + e.Message); }
        }

        // Cheats: Set Extra Harvest Times charges to a fixed value for all monsters
        public static void SetExtraHarvestTimesForAll(int charges)
        {
            try
            {
                var list = GameManager.Status.EnumerateActiveMonsters();
                int cnt = 0;
                var statChargesEnum = Type.GetType("TeamNimbus.CloudMeadow.Traits.StatModifiersThatUseCharges, Game");
                var traitInstanceType = Type.GetType("TeamNimbus.CloudMeadow.Traits.TraitInstance, Game");
                foreach (var m in list)
                {
                    try
                    {
                        var t = m.GetType();
                        var meth = t.GetMethod("EnumerateTraitsWithCharges", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (meth != null && statChargesEnum != null)
                        {
                            var val = Enum.Parse(statChargesEnum, "ExtraHarvestTimes");
                            var enumerable = meth.Invoke(m, new object[] { val }) as System.Collections.IEnumerable;
                            if (enumerable != null)
                            {
                                foreach (var ti in enumerable)
                                {
                                    try
                                    {
                                        var f = traitInstanceType.GetField("charges", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        if (f != null) { f.SetValue(ti, charges); cnt++; }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                    catch { }
                }
                Banner("Harvest times set to " + charges + " (" + cnt + " traits)");
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetExtraHarvestTimesForAll failed: " + e.Message); }
        }

        // Cheats: Ultra Bread toggle (makes Bread give +999 to six stats)
        private static bool _ultraBreadEnabled = false;
        public static bool UltraBreadEnabled { get { return _ultraBreadEnabled; } }
        private static System.Collections.Generic.Dictionary<object, object> _savedBreadData = new System.Collections.Generic.Dictionary<object, object>();

        public static void ToggleUltraBread()
        {
            try
            {
                _ultraBreadEnabled = !_ultraBreadEnabled;
                ApplyUltraBread(_ultraBreadEnabled);
                Banner("Ultra Bread: " + (_ultraBreadEnabled ? "ON" : "OFF"));
            }
            catch (Exception e) { Plugin.Log.LogWarning("ToggleUltraBread failed: " + e.Message); }
        }

        private static void ApplyUltraBread(bool enable)
        {
            var itemLib = GameManager.ItemLibrary;
            if (itemLib == null) return;
            var libType = itemLib.GetType();
            var fAll = libType.GetField("allItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var all = fAll != null ? fAll.GetValue(itemLib) as System.Collections.IList : null;
            if (all == null) return;

            var otherFoodType = Type.GetType("TeamNimbus.CloudMeadow.Items.OtherFoodItemDefinition, Game");
            var cookedType = Type.GetType("TeamNimbus.CloudMeadow.Items.CookedItemDefinition, Game");
            var passiveStatModType = Type.GetType("TeamNimbus.CloudMeadow.PassiveStatMod, Game");
            var statModifiersEnum = Type.GetType("TeamNimbus.CloudMeadow.StatModifiers, Game");

            foreach (var def in all)
            {
                try
                {
                    var asIEdible = def.GetType().GetInterface("TeamNimbus.CloudMeadow.Items.IEdibleItemDefinition");
                    if (asIEdible == null) continue;

                    var foodTagsProp = def.GetType().GetProperty("FoodTags");
                    if (foodTagsProp == null) continue;
                    var tagsVal = foodTagsProp.GetValue(def, null);
                    if (tagsVal == null) continue;
                    int tagsInt = Convert.ToInt32(tagsVal);
                    if ((tagsInt & 16384) == 0) continue; // FoodTags.Bread

                    if (otherFoodType != null && otherFoodType.IsInstanceOfType(def))
                    {
                        var fMods = otherFoodType.GetField("statModifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fMods == null || passiveStatModType == null || statModifiersEnum == null) continue;
                        var current = fMods.GetValue(def);
                        if (enable)
                        {
                            if (!_savedBreadData.ContainsKey(def)) _savedBreadData[def] = current;
                            var arr = Array.CreateInstance(passiveStatModType, 6);
                            string[] stats = new[] { "Physique", "Stamina", "Intuition", "Swiftness", "Experience", "HealingFromFood" };
                            var modTypeEnum = Type.GetType("TeamNimbus.Common.Utility.ModifierType, Common") ?? Type.GetType("TeamNimbus.Common.Utility.ModifierType");
                            for (int i = 0; i < stats.Length; i++)
                            {
                                var stat = Enum.Parse(statModifiersEnum, stats[i]);
                                object modTypeVal = (stats[i] == "Experience" || stats[i] == "HealingFromFood")
                                    ? Enum.Parse(modTypeEnum, "IndependentScalarWithoutBaseValue")
                                    : Enum.Parse(modTypeEnum, "AddedModifier");
                                var ctor = passiveStatModType.GetConstructor(new Type[] { statModifiersEnum, typeof(float), modTypeEnum });
                                var psm = ctor.Invoke(new object[] { stat, 999f, modTypeVal });
                                arr.SetValue(psm, i);
                            }
                            fMods.SetValue(def, arr);
                        }
                        else
                        {
                            object saved;
                            if (_savedBreadData.TryGetValue(def, out saved))
                            {
                                fMods.SetValue(def, saved);
                            }
                        }
                    }
                    else if (cookedType != null && cookedType.IsInstanceOfType(def))
                    {
                        var fBase = cookedType.GetField("baseFoodEffectsData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (fBase == null) continue;
                        var boostStruct = fBase.GetValue(def);
                        if (boostStruct == null) continue;
                        var boostType = boostStruct.GetType();
                        if (enable)
                        {
                            if (!_savedBreadData.ContainsKey(def)) _savedBreadData[def] = boostStruct;
                            var newBoost = boostStruct;
                            SetBoostField(newBoost, boostType, "physiqueBoost", 999f);
                            SetBoostField(newBoost, boostType, "staminaBoost", 999f);
                            SetBoostField(newBoost, boostType, "intuitionBoost", 999f);
                            SetBoostField(newBoost, boostType, "swiftnessBoost", 999f);
                            SetBoostField(newBoost, boostType, "experienceBoost", 999f);
                            SetBoostField(newBoost, boostType, "healingBoost", 999f);
                            fBase.SetValue(def, newBoost);
                        }
                        else
                        {
                            object saved;
                            if (_savedBreadData.TryGetValue(def, out saved))
                            {
                                fBase.SetValue(def, saved);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private static void SetBoostField(object boostStruct, Type boostType, string fieldName, float value)
        {
            try
            {
                var f = boostType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f == null) return;
                var simple = f.GetValue(boostStruct);
                var simpleType = simple.GetType();
                var fVal = simpleType.GetField("value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (fVal != null)
                {
                    object boxed = simple;
                    fVal.SetValue(boxed, value);
                    f.SetValue(boostStruct, boxed);
                }
            }
            catch { }
        }

        private static void Banner(string msg)
        {
            try { TeamNimbus.CloudMeadow.Utilities.BannerMessage.ShowMessage(msg, 1.2f, null); } catch { }
        }

        private static void Diag(string msg)
        {
            try
            {
                if (!VerboseDiagnosticsEnabled) return;
                Plugin.Log.LogInfo("[CM-DIAG] " + msg);
                LogBuffer.Add("[DIAG] " + msg);
            }
            catch { }
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
