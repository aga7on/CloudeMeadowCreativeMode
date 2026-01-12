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

        private void DrawInventoryUI()
        {
            try
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh", GUILayout.Width(80))) { }
                GUILayout.Label("Filter:", GUILayout.Width(50));
                _addItemFilter = GUILayout.TextField(_addItemFilter ?? "", GUILayout.Width(200));
                if (GUILayout.Button("Apply", GUILayout.Width(70))) { /* filter is reactive */ }
                if (GUILayout.Button("Add Item", GUILayout.Width(100))) { _showAddItem = true; if (_allItemDefs == null) _allItemDefs = GameApi.GetAllItemDefinitions(); }
                if (GUILayout.Button("Get All Items", GUILayout.Width(140))) { GameApi.AddAllItems(1, 1); }
                GUILayout.EndHorizontal();

                var entries = GameApi.GetInventoryEntries();
                _invScroll = GUILayout.BeginScrollView(_invScroll);
                for (int i = 0; i < entries.Length; i++)
                {
                    var e = entries[i]; if (e == null) continue;
                    var def = ReadDef(e);
                    string name = ReadString(def, new string[] { "Name", "DisplayName", "Code" });
                    if (!string.IsNullOrEmpty(_addItemFilter))
                    {
                        if (name.IndexOf(_addItemFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    }
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label((i + 1) + ". " + name, GUILayout.Width(300));
                    int qty = ReadInt(e, new string[] { "Quantity", "Count", "Stack", "Amount" });
                    GUILayout.Label("x" + qty, GUILayout.Width(60));
                    if (GUILayout.Button("+1", GUILayout.Width(40))) GameApi.AdjustEntryQuantity(e, 1);
                    if (GUILayout.Button("+10", GUILayout.Width(50))) GameApi.AdjustEntryQuantity(e, 10);
                    if (GUILayout.Button("Set Max Quality", GUILayout.Width(120))) GameApi.SetEntryMaxQuality(e);
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

            _addItemScroll = GUILayout.BeginScrollView(_addItemScroll, GUILayout.Height(300));
            if (_allItemDefs != null)
            {
                for (int i = 0; i < _allItemDefs.Length; i++)
                {
                    var def = _allItemDefs[i]; if (def == null) continue;
                    string name = ReadString(def, new string[] { "Name", "DisplayName", "Code" });
                    if (!string.IsNullOrEmpty(_addItemFilter))
                    {
                        if (name.IndexOf(_addItemFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    }
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label(name, GUILayout.Width(400));
                    if (GUILayout.Button("+1", GUILayout.Width(40))) GameApi.AddItemByDefinition(def, 1, 1);
                    if (GUILayout.Button("+10", GUILayout.Width(50))) GameApi.AddItemByDefinition(def, 10, 1);
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
    }
}
