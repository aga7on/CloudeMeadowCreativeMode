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

        // ===== Inventory helpers =====
        public static object[] GetInventoryEntries()
        {
            try
            {
                var inv = GameManager.Status.Inventory;
                var list = new System.Collections.Generic.List<object>();
                var t = inv.GetType();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    object val = null; try { val = p.GetValue(inv, null); } catch { }
                    AppendEntryEnumerable(list, val);
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    object val = null; try { val = f.GetValue(inv); } catch { }
                    AppendEntryEnumerable(list, val);
                }
                return list.ToArray();
            }
            catch (Exception e) { Plugin.Log.LogWarning("GetInventoryEntries failed: " + e.Message); }
            return new object[0];
        }

        private static void AppendEntryEnumerable(System.Collections.Generic.List<object> list, object val)
        {
            if (val == null) return;
            var en = val as System.Collections.IEnumerable; if (en == null || val is string) return;
            foreach (var it in en)
            {
                if (it == null) continue;
                var tn = it.GetType().FullName;
                if (tn != null && (tn.IndexOf("Entry") >= 0 || tn.IndexOf("ItemEntry") >= 0)) list.Add(it);
            }
        }

        private static object CreateEntry(object def, int amount, int qualityIndex)
        {
            try
            {
                var tDef = def.GetType();
                qualityIndex = ClampQualityIndex(qualityIndex);
                var itemQualityType = typeof(TeamNimbus.CloudMeadow.Items.ItemQuality);
                object quality = System.Enum.ToObject(itemQualityType, qualityIndex);

                // Prefer CreateItemEntry(count, quality)
                var mi = tDef.GetMethod("CreateItemEntry", new System.Type[] { typeof(int), itemQualityType });
                if (mi != null)
                {
                    return mi.Invoke(def, new object[] { amount, quality });
                }
                // Special-case: EggItemDefinition -> EggItemEntry(def, true)
                var eggType = typeof(TeamNimbus.CloudMeadow.Items.EggItemDefinition);
                if (eggType.IsAssignableFrom(tDef))
                {
                    var eggEntryType = typeof(TeamNimbus.CloudMeadow.Inventory.EggItemEntry);
                    return System.Activator.CreateInstance(eggEntryType, new object[] { def, true });
                }
                // Fallback: StandardItemEntry(def, quality, amount)
                var sei = typeof(TeamNimbus.CloudMeadow.Inventory.StandardItemEntry);
                return System.Activator.CreateInstance(sei, new object[] { def, quality, amount });
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("CreateEntry failed: " + e.Message);
                return null;
            }
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
                // Replace by: add new max-quality entry FIRST, then remove old entry
                var def = GetEntryDefinition(entry);
                int qty = GetEntryQuantity(entry);
                if (qty <= 0) qty = 1;
                var maxQ = (int)TeamNimbus.CloudMeadow.Items.ItemQuality.FiveStar;
                var newEntry = CreateEntry(def, qty, maxQ) as TeamNimbus.CloudMeadow.Inventory.IItemEntry;
                if (newEntry != null)
                {
                    GameManager.Status.Inventory.AddItemEntry(newEntry);
                    TryRemoveEntry(entry);
                }
                else
                {
                    Plugin.Log.LogWarning("SetEntryMaxQuality: could not create new entry");
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetEntryMaxQuality failed: " + e.Message); }
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
                var t = monster.GetType();
                var prop = t.GetProperty("Species") ?? t.GetProperty("FarmableSpecies") ?? t.GetProperty("Type");
                if (prop != null)
                {
                    var enumType = prop.PropertyType;
                    if (enumType.IsEnum)
                    {
                        var val = Enum.Parse(enumType, speciesName, true);
                        prop.SetValue(monster, val, null);
                    }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetMonsterSpecies failed: " + e.Message); }
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
                var lib = UnityEngine.Object.FindObjectOfType(libType);
                if (lib != null)
                {
                    var resolve = libType.GetMethod("ResolveMonsterTraitsByType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var enumType = Type.GetType("TeamNimbus.CloudMeadow.Monsters.FarmableSpecies, Game");
                    var species = Enum.Parse(enumType, speciesName, true);
                    var traitsByType = resolve.Invoke(lib, new object[] { species });
                    var tbt = traitsByType.GetType();
                    var fld = tbt.GetField("OtherBloodlineTraits", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    object val = null; try { if (fld != null) val = fld.GetValue(traitsByType); } catch { }
                    var arr = val as System.Collections.IEnumerable;
                    var list = new System.Collections.Generic.List<object>();
                    if (arr != null) foreach (var d in arr) if (d != null) list.Add(d);
                    return list.ToArray();
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
                        if (traits.Count > 0) return traits.ToArray();
                    }
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i];
                    if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        object col = null; try { col = f.GetValue(monster); } catch { }
                        AppendTraits(traits, col);
                        if (traits.Count > 0) return traits.ToArray();
                    }
                }
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

        public static object[] GetAllTraitDefinitions()
        {
            try
            {
                var libType = Type.GetType("TeamNimbus.CloudMeadow.Traits.MonsterTraitLibrary, Game");
                var lib = UnityEngine.Object.FindObjectOfType(libType);
                var res = new System.Collections.Generic.List<object>();
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var fields = libType.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i]; object col = null; try { col = f.GetValue(lib); } catch { }
                    var en = col as System.Collections.IEnumerable; if (en == null || col is string) continue;
                    foreach (var d in en) if (d != null) res.Add(d);
                }
                return res.ToArray();
            }
            catch { }
            return new object[0];
        }

        public static object[] GetUniversalTraitDefinitions()
        {
            var all = GetAllTraitDefinitions();
            return FilterTraitDefinitionsBySource(all, "Universal");
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
                var t = traitInstance.GetType();
                var pGet = t.GetProperty("Grade");
                var miInc = t.GetMethod("IncreaseGrade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var miDec = t.GetMethod("ReduceGrade", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (pGet != null && miInc != null && miDec != null)
                {
                    int cur = Convert.ToInt32(pGet.GetValue(traitInstance, null));
                    int target = Math.Max(cur, grade);
                    int safety = 10;
                    while (cur < target && safety-- > 0)
                    {
                        miInc.Invoke(traitInstance, null);
                        cur = Convert.ToInt32(pGet.GetValue(traitInstance, null));
                    }
                    while (cur > target && safety-- > 0)
                    {
                        miDec.Invoke(traitInstance, null);
                        cur = Convert.ToInt32(pGet.GetValue(traitInstance, null));
                    }
                    return;
                }
                Plugin.Log.LogWarning("SetTraitGrade failed: methods not found");
            }
            catch (Exception e) { Plugin.Log.LogWarning("SetTraitGrade failed: " + e.Message); }
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
                    var def = t.GetProperty("TraitSource") != null ? t.GetProperty("TraitSource").GetValue(d, null) : null;
                    var s = def != null ? def.ToString() : string.Empty;
                    if (s.IndexOf(sourceContains, StringComparison.OrdinalIgnoreCase) >= 0) list.Add(d);
                }
                catch { }
            }
            return list.ToArray();
        }

        public static void AddTraitToMonster(object monster, object traitDefinition, int grade)
        {
            try
            {
                var instType = Type.GetType("TeamNimbus.CloudMeadow.Traits.TraitInstance, Game");
                var inst = Activator.CreateInstance(instType);
                var init = instType.GetMethod("InitializeTraitDefinition");
                if (init != null) init.Invoke(inst, new object[] { traitDefinition });
                // set grade/level
                var gradeProp = instType.GetProperty("Grade") ?? instType.GetProperty("Level");
                if (gradeProp != null) gradeProp.SetValue(inst, grade, null);
                // add to monster's trait collection
                var traits = GetMonsterTraits(monster);
                if (traits.Length >= 0)
                {
                    // find backing list object (IEnumerable); assume first collection's owner is on monster as List<TraitInstance>
                    var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    var t = monster.GetType();
                    var props = t.GetProperties(flags);
                    for (int i = 0; i < props.Length; i++)
                    {
                        var p = props[i]; if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        var col = p.GetValue(monster, null);
                        if (col == null || col is string) continue;
                        var add = col.GetType().GetMethod("Add");
                        if (add != null && add.GetParameters().Length == 1)
                        { add.Invoke(col, new object[] { inst }); return; }
                    }
                    var fields = t.GetFields(flags);
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
            }
            catch (Exception e) { Plugin.Log.LogWarning("AddTraitToMonster failed: " + e.Message); }
        }

        public static void RemoveTraitFromMonster(object monster, object traitInstance)
        {
            try
            {
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var t = monster.GetType();
                var props = t.GetProperties(flags);
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i]; if (p.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var col = p.GetValue(monster, null);
                    var rem = col != null ? col.GetType().GetMethod("Remove") : null;
                    if (rem != null && rem.GetParameters().Length == 1)
                    { rem.Invoke(col, new object[] { traitInstance }); return; }
                }
                var fields = t.GetFields(flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i]; if (f.Name.IndexOf("trait", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var col = f.GetValue(monster);
                    var rem = col != null ? col.GetType().GetMethod("Remove") : null;
                    if (rem != null && rem.GetParameters().Length == 1)
                    { rem.Invoke(col, new object[] { traitInstance }); return; }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("RemoveTraitFromMonster failed: " + e.Message); }
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
