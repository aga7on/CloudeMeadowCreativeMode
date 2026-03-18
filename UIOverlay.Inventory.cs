using UnityEngine;

namespace CloudMeadow.CreativeMode
{
    internal partial class UIOverlay
    {
        private Vector2 _invScroll;
        private bool _showAddItem;
        private Vector2 _addItemScroll;
        private object[] _allItemDefs;
        private string _addItemFilter = "";
        private string _itemCategoryFilter = "All";
        private bool _inventoryCacheDirty = true;
        private float _inventoryCacheAt = -999f;
        private const float InventoryCacheSeconds = 0.75f;
        private System.Collections.Generic.List<InventoryRowCache> _inventoryRowsCache = new System.Collections.Generic.List<InventoryRowCache>();
        private string[] _itemCategoryOptions;

        private class InventoryRowCache
        {
            public object Entry;
            public object Def;
            public string Name;
            public string Category;
            public int Quantity;
        }

        private void DrawInventoryUI()
        {
            try
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh", GUILayout.Width(80))) { _allItemDefs = null; MarkInventoryCacheDirty(); }
                GUILayout.Label("Filter:", GUILayout.Width(50));
                _addItemFilter = GUILayout.TextField(_addItemFilter ?? "", GUILayout.Width(200));
                if (GUILayout.Button("Apply", GUILayout.Width(70))) { /* filter is reactive */ }
                if (GUILayout.Button("Add Item", GUILayout.Width(100))) { _showAddItem = true; if (_allItemDefs == null) _allItemDefs = GameApi.GetAllItemDefinitions(); }
                if (GUILayout.Button("Get All Items", GUILayout.Width(140))) { GameApi.AddAllItems(1, 1); MarkInventoryCacheDirty(); }
                if (GUILayout.Button("MAX ALL QUALITY", GUILayout.Width(150))) { GameApi.SetAllInventoryEntriesMaxQuality(); MarkInventoryCacheDirty(); }
                GUILayout.EndHorizontal();

                DrawCategoryFilterBar();

                EnsureInventoryCache();
                _invScroll = GUILayout.BeginScrollView(_invScroll);
                for (int i = 0; i < _inventoryRowsCache.Count; i++)
                {
                    var row = _inventoryRowsCache[i];
                    if (row == null || row.Entry == null) continue;
                    string name = row.Name;
                    string category = row.Category;
                    if (!string.IsNullOrEmpty(_addItemFilter))
                    {
                        if (name.IndexOf(_addItemFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    }
                    if (!string.Equals(_itemCategoryFilter, "All", System.StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(category, _itemCategoryFilter, System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label((i + 1) + ". " + name, GUILayout.Width(300));
                    GUILayout.Label("[" + (string.IsNullOrEmpty(category) ? "Unknown" : category) + "]", GUILayout.Width(120));
                    GUILayout.Label("x" + row.Quantity, GUILayout.Width(60));
                    if (GUILayout.Button("+1", GUILayout.Width(40))) { GameApi.AdjustEntryQuantity(row.Entry, 1); MarkInventoryCacheDirty(); }
                    if (GUILayout.Button("+10", GUILayout.Width(50))) { GameApi.AdjustEntryQuantity(row.Entry, 10); MarkInventoryCacheDirty(); }
                    if (GUILayout.Button("Set Max Quality", GUILayout.Width(120))) { GameApi.SetEntryMaxQuality(row.Entry); MarkInventoryCacheDirty(); }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();

                if (_showAddItem) DrawAddItemWindow();
            }
            catch (System.Exception ex)
            {
                GUILayout.Label("Inventory UI error: " + ex.Message);
            }
        }

        private object ReadDef(object entry)
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

        private void MarkInventoryCacheDirty()
        {
            _inventoryCacheDirty = true;
        }

        private void EnsureInventoryCache()
        {
            float now = Time.realtimeSinceStartup;
            if (!_inventoryCacheDirty && (now - _inventoryCacheAt) < InventoryCacheSeconds) return;

            _inventoryCacheDirty = false;
            _inventoryCacheAt = now;
            _inventoryRowsCache.Clear();

            try
            {
                var entries = GameApi.GetInventoryEntries();
                for (int i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];
                    if (e == null) continue;
                    var def = ReadDef(e);
                    string name = ReadString(def, new string[] { "Name", "DisplayName", "Code" });
                    if (string.IsNullOrEmpty(name)) name = e.GetType().Name;
                    string category = GameApi.GetItemCategoryName(def);
                    int qty = ReadInt(e, new string[] { "Quantity", "Count", "Stack", "Amount" });
                    _inventoryRowsCache.Add(new InventoryRowCache { Entry = e, Def = def, Name = name, Category = category, Quantity = qty });
                }
            }
            catch { }
        }

        private void DrawCategoryFilterBar()
        {
            try
            {
                if (_itemCategoryOptions == null) _itemCategoryOptions = GameApi.GetAllItemCategoryNames();
                if (_itemCategoryOptions == null || _itemCategoryOptions.Length == 0) return;

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label("Category:", GUILayout.Width(70));
                for (int i = 0; i < _itemCategoryOptions.Length; i++)
                {
                    string cat = _itemCategoryOptions[i];
                    bool active = string.Equals(cat, _itemCategoryFilter, System.StringComparison.OrdinalIgnoreCase);
                    bool newActive = GUILayout.Toggle(active, cat, GUI.skin.button, GUILayout.Width(90));
                    if (newActive && !active) _itemCategoryFilter = cat;
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset", GUILayout.Width(60))) _itemCategoryFilter = "All";
                GUILayout.EndHorizontal();
            }
            catch { }
        }

        private void DrawAddItemWindow()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Add Item", GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(70))) _showAddItem = false;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter:", GUILayout.Width(50));
            _addItemFilter = GUILayout.TextField(_addItemFilter ?? "", GUILayout.Width(200));
            if (GUILayout.Button("Reload", GUILayout.Width(70))) _allItemDefs = GameApi.GetAllItemDefinitions();
            GUILayout.EndHorizontal();
            DrawCategoryFilterBar();

            _addItemScroll = GUILayout.BeginScrollView(_addItemScroll, GUILayout.Height(300));
            if (_allItemDefs != null)
            {
                for (int i = 0; i < _allItemDefs.Length; i++)
                {
                    var def = _allItemDefs[i]; if (def == null) continue;
                    string name = ReadString(def, new string[] { "Name", "DisplayName", "Code" });
                    string category = GameApi.GetItemCategoryName(def);
                    if (!string.IsNullOrEmpty(_addItemFilter))
                    {
                        if (name.IndexOf(_addItemFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    }
                    if (!string.Equals(_itemCategoryFilter, "All", System.StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(category, _itemCategoryFilter, System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label(name, GUILayout.Width(300));
                    GUILayout.Label("[" + (string.IsNullOrEmpty(category) ? "Unknown" : category) + "]", GUILayout.Width(120));
                    if (GUILayout.Button("+1", GUILayout.Width(40))) { GameApi.AddItemByDefinition(def, 1, 1); MarkInventoryCacheDirty(); }
                    if (GUILayout.Button("+10", GUILayout.Width(50))) { GameApi.AddItemByDefinition(def, 10, 1); MarkInventoryCacheDirty(); }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}
