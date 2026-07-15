using System;
using UnityEngine;
using UnityEngine.UI;

namespace CloudMeadow.CreativeMode
{
    internal sealed class CreativeEditorUGUI : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _content;
        private RectTransform _contentViewport;
        private Text _title;
        private RectTransform _softwareCursor;
        private RectTransform _canvasRect;
        private Texture2D _cursorTexture;
        private readonly System.Collections.Generic.List<Button> _manualButtons = new System.Collections.Generic.List<Button>();
        private int _inventoryPage, _itemCatalogPage, _monsterPage, _traitPage, _questPage, _eggPage, _galleryPage;
        private TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats _selectedMonster;
        private readonly System.Collections.Generic.HashSet<TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats> _selectedMonsters = new System.Collections.Generic.HashSet<TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats>();
        private string _monsterFilter = string.Empty;
        private TeamNimbus.CloudMeadow.Story.QuestSystem.QuestInfo _selectedQuest;
        private string _inventoryFilter = string.Empty;
        private string _inventoryCategory = "All";
        private string _questFilter = string.Empty;
        private string _galleryFilter = string.Empty;
        private readonly System.Collections.Generic.HashSet<object> _selectedInventory = new System.Collections.Generic.HashSet<object>();
        private object _inventoryContainer;
        private const int PageSize = 12;
        private readonly System.Collections.Generic.Dictionary<string, float> _scrollPositions = new System.Collections.Generic.Dictionary<string, float>();
        private string _renderedModule;
        private float _scrollTarget = 1f;
        private int _restoreScrollFrames;
        private CanvasGroup _activeBodyGroup;
        private bool _showSpeciesCatalog;
        private bool _showItemCatalog;
        private bool _showInventoryCategories;
        private bool _showAvailableTraits;
        private bool _showSpeciesEditor;
        private bool _showChimeraEditor;
        private bool _showPigments;
        private bool _showSceneBrowser;
        private bool _showJobs;
        private bool _showEquipment;
        private string _questStatusFilter = "All";
        private readonly System.Collections.Generic.Dictionary<string, Button> _sidebarButtons = new System.Collections.Generic.Dictionary<string, Button>();
        private bool _promptOpen;
        private string _promptTitle, _promptValue;
        private Action<string> _promptApply;
        private GameObject _promptLayer;
        private Text _promptHeading;
        private InputField _promptInput;
        private readonly System.Collections.Generic.Dictionary<Transform, Transform> _actionRows = new System.Collections.Generic.Dictionary<Transform, Transform>();
        private readonly System.Collections.Generic.Dictionary<Transform, int> _actionRowCounts = new System.Collections.Generic.Dictionary<Transform, int>();
        private object _advancedSelected;
        private int _advancedPage;
        private string _advancedFilter = string.Empty;
        private string _module = "Overview";
        private static readonly string[] Modules = { "Overview", "Player", "Party", "Monsters", "Inventory", "Eggs", "Farm", "Quests", "World", "Dungeons", "Combat", "Cheats", "Gallery", "Relationships", "Advanced", "Diagnostics", "Errors" };

        private void Start()
        {
            Build();
            SetVisible(false);
        }

        private void Update()
        {
            if (_promptOpen && Input.GetKeyDown(KeyCode.Escape)) { ClosePrompt(); return; }
            if (Plugin.ToggleOverlayKey != null && Input.GetKeyDown(Plugin.ToggleOverlayKey.Value))
                SetVisible(!_canvas.gameObject.activeSelf);
            if (Plugin.UnlockGalleryKey != null && Input.GetKeyDown(Plugin.UnlockGalleryKey.Value)) GameApi.UnlockAllGallery();
            if (Plugin.RefreshScanKey != null && Input.GetKeyDown(Plugin.RefreshScanKey.Value) && _canvas != null && _canvas.gameObject.activeSelf) RefreshModule();
            if (_canvas != null && _canvas.gameObject.activeSelf && _softwareCursor != null)
            {
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, Input.mousePosition, null, out local))
                    _softwareCursor.anchoredPosition = local;
            }
            if (_canvas != null && _canvas.gameObject.activeSelf && _content != null)
            {
                var scroll = _content.GetComponent<ScrollRect>();
                if (scroll != null)
                {
                    if (_restoreScrollFrames > 0) _restoreScrollFrames--;
                    else scroll.verticalNormalizedPosition = Mathf.Lerp(scroll.verticalNormalizedPosition, _scrollTarget, Mathf.Clamp01(Time.unscaledDeltaTime * 14f));
                }
                if (_activeBodyGroup != null && _activeBodyGroup.alpha < 1f)
                    _activeBodyGroup.alpha = Mathf.MoveTowards(_activeBodyGroup.alpha, 1f, Time.unscaledDeltaTime * 5f);
            }
        }

        private void OnGUI()
        {
            if (_canvas == null || !_canvas.gameObject.activeSelf || Event.current == null) return;
            // Unity 2017's StandaloneInputModule applies a vertical backbuffer offset
            // in this game. Handle editor clicks in the same GUI coordinate system in
            // which the controls are rendered, avoiding shifted uGUI raycasts.
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Vector2 mouse = Event.current.mousePosition;
                for (int i = _manualButtons.Count - 1; i >= 0; i--)
                {
                    var button = _manualButtons[i];
                    if (button == null) { _manualButtons.RemoveAt(i); continue; }
                    var rt = button.GetComponent<RectTransform>();
                    Vector3[] corners = new Vector3[4]; rt.GetWorldCorners(corners);
                    Rect guiRect = new Rect(corners[0].x, Screen.height - corners[1].y, corners[2].x - corners[0].x, corners[1].y - corners[0].y);
                    if (_promptOpen && !button.transform.IsChildOf(_promptLayer.transform)) continue;
                    if (guiRect.Contains(mouse)) { button.onClick.Invoke(); Event.current.Use(); break; }
                }
            }
            else if (!_promptOpen && Event.current.type == EventType.ScrollWheel && _content != null)
            {
                var scroll = _content.GetComponent<ScrollRect>();
                if (scroll != null)
                {
                    _scrollTarget = Mathf.Clamp01(_scrollTarget - Event.current.delta.y * 0.055f);
                    _scrollPositions[_module] = _scrollTarget;
                }
                Event.current.Use();
            }
            if (_cursorTexture == null)
            {
                _cursorTexture = new Texture2D(1, 1); _cursorTexture.SetPixel(0, 0, new Color(1f, 0.92f, 0.2f, 1f)); _cursorTexture.Apply();
            }
            Vector2 cursorPoint = Event.current.mousePosition;
            GUI.depth = -32768;
            GUI.DrawTexture(new Rect(cursorPoint.x, cursorPoint.y, 12, 12), _cursorTexture);
        }

        private void SetVisible(bool value)
        {
            if (_canvas != null) _canvas.gameObject.SetActive(value);
            if (value) { Cursor.visible = true; RefreshModule(); }
        }

        private void Build()
        {
            var root = new GameObject("CreativeEditorCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            _canvas = root.GetComponent<Canvas>();
            _canvasRect = root.GetComponent<RectTransform>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Stay above normal HUD windows but below the game's custom cursor layer.
            _canvas.sortingOrder = 100;
            root.GetComponent<GraphicRaycaster>().enabled = false;
            var scaler = root.GetComponent<CanvasScaler>();
            // Unity 2017 has incorrect pointer mapping with ScaleWithScreenSize on
            // some DPI/resolution combinations. Percentage anchors already provide
            // adaptive sizing, so keep pixels and raycasts in the same coordinate space.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var window = Panel("Window", root.transform, new Color(0.055f, 0.075f, 0.10f, 0.98f));
            SetRect(window, new Vector2(0.16f, 0.12f), new Vector2(0.84f, 0.88f), Vector2.zero, Vector2.zero);

            var header = Panel("Header", window, new Color(0.10f, 0.16f, 0.20f, 1f));
            SetRect(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -68), Vector2.zero);
            _title = Label("Cloud Meadow Creative Editor", header, 26, TextAnchor.MiddleLeft);
            SetRect(_title.rectTransform, Vector2.zero, Vector2.one, new Vector2(24, 0), new Vector2(-160, 0));
            var close = MakeButton("Close", header, delegate { SetVisible(false); });
            SetRect(close.GetComponent<RectTransform>(), new Vector2(1, 0), Vector2.one, new Vector2(-130, 10), new Vector2(-18, -10));

            var sidebar = Panel("Sidebar", window, new Color(0.075f, 0.11f, 0.14f, 1f));
            SetRect(sidebar, Vector2.zero, new Vector2(0, 1), new Vector2(0, 0), new Vector2(185, -68));
            var layout = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8); layout.spacing = 2; layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            for (int i = 0; i < Modules.Length; i++)
            {
                string captured = Modules[i];
                var b = MakeButton(captured, sidebar, delegate { _module = captured; RefreshModule(); });
                b.GetComponent<LayoutElement>().preferredHeight = 24;
                _sidebarButtons[captured] = b;
            }

            _content = Panel("Content", window, new Color(0.035f, 0.05f, 0.07f, 0.25f));
            SetRect(_content, Vector2.zero, Vector2.one, new Vector2(185, 0), new Vector2(0, -68));
            var scroll = _content.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 35f;
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(_content, false);
            _contentViewport = viewport.GetComponent<RectTransform>();
            SetRect(_contentViewport, Vector2.zero, Vector2.one, new Vector2(20, 16), new Vector2(-20, -16));
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = _contentViewport;
            RefreshModule();

            BuildPrompt(root.transform);

            // Cloud Meadow renders its own cursor below high-order overlay canvases.
            // Mirror the pointer on a nested override-sorting canvas so it stays visible.
            var cursorLayer = new GameObject("CreativeCursorLayer", typeof(RectTransform), typeof(Canvas));
            cursorLayer.transform.SetParent(root.transform, false);
            var cursorCanvas = cursorLayer.GetComponent<Canvas>(); cursorCanvas.overrideSorting = true; cursorCanvas.sortingOrder = 32767;
            SetRect(cursorLayer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var cursor = new GameObject("CreativeCursor", typeof(RectTransform), typeof(Image));
            cursor.transform.SetParent(cursorLayer.transform, false);
            _softwareCursor = cursor.GetComponent<RectTransform>();
            _softwareCursor.anchorMin = _softwareCursor.anchorMax = new Vector2(0.5f, 0.5f);
            _softwareCursor.pivot = new Vector2(0.15f, 0.85f); _softwareCursor.sizeDelta = new Vector2(18, 18);
            var cursorImage = cursor.GetComponent<Image>(); cursorImage.color = new Color(1f, 0.92f, 0.35f, 1f); cursorImage.raycastTarget = false; cursorImage.enabled = false;
        }

        private void RefreshModule()
        {
            if (_content == null) return;
            if (_contentViewport == null) return;
            var activeScroll = _content.GetComponent<ScrollRect>();
            if (activeScroll != null && !string.IsNullOrEmpty(_renderedModule))
                _scrollPositions[_renderedModule] = activeScroll.verticalNormalizedPosition;
            for (int i = _contentViewport.childCount - 1; i >= 0; i--) Destroy(_contentViewport.GetChild(i).gameObject);
            _actionRows.Clear(); _actionRowCounts.Clear();
            _title.text = "Creative Editor  /  " + _module;
            foreach (var pair in _sidebarButtons)
                if (pair.Value != null) pair.Value.GetComponent<Image>().color = pair.Key == _module ? new Color(0.20f, 0.48f, 0.53f, 1f) : new Color(0.14f, 0.25f, 0.29f, 1f);
            var body = new GameObject("Body", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(CanvasGroup));
            body.transform.SetParent(_contentViewport, false);
            var bodyRect = body.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0, 1); bodyRect.anchorMax = new Vector2(1, 1); bodyRect.pivot = new Vector2(0.5f, 1);
            bodyRect.offsetMin = new Vector2(8, 0); bodyRect.offsetMax = new Vector2(-8, 0);
            var vg = body.GetComponent<VerticalLayoutGroup>(); vg.spacing = 12; vg.childControlHeight = true; vg.childForceExpandWidth = true; vg.childForceExpandHeight = false;
            var fitter = body.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _content.GetComponent<ScrollRect>().content = bodyRect;
            _activeBodyGroup = body.GetComponent<CanvasGroup>(); _activeBodyGroup.alpha = 0.25f;
            float savedScroll;
            _scrollTarget = _scrollPositions.TryGetValue(_module, out savedScroll) ? savedScroll : 1f;
            _renderedModule = _module;
            _restoreScrollFrames = 2;

            AddHeading(body.transform, _module);
            if (_module == "Overview")
            {
                AddLine(body.transform, GameApi.Ready ? GameApi.BuildQuickStatus() : "Load a save/world to enable editing.");
                AddAction(body.transform, "Unlock gallery", delegate { GameApi.UnlockAllGallery(); RefreshModule(); });
                AddAction(body.transform, "Advance to end of day", delegate { GameApi.AdvanceToEndOfDay(); RefreshModule(); });
                AddAction(body.transform, "Refresh data", delegate { GameApiQuest.MarkQuestCacheDirty(); RefreshModule(); });
            }
            else if (_module == "Player")
            {
                if (GameApi.Ready)
                {
                    var p = TeamNimbus.CloudMeadow.Managers.GameManager.Status.ProtagonistStats;
                    AddLine(body.transform, "Name: " + p.Name + "    Level: " + p.Level + "    Gender: " + p.Gender);
                    AddLine(body.transform, "HP: " + p.GetCurrentHP().ToString("0") + "/" + p.GetMaxHP().ToString("0") + " | XP: " + p.XPSinceLastLevel.ToString("0") + "/" + p.XPNeededForNextLevel + " | Stat points: " + p.NumStatPoints);
                    AddAction(body.transform, "Edit player name...", delegate { OpenPrompt("Player name", p.Name, delegate(string v) { GameApi.RenameProtagonist(v); }); });
                    AddAction(body.transform, "Set exact player level...", delegate { OpenPrompt("Player level", p.Level.ToString(), delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetProtagonistLevel(n); }); });
                    AddAction(body.transform, "Set Male", delegate { GameApi.SetProtagonistGender("Male"); RefreshModule(); });
                    AddAction(body.transform, "Set Female", delegate { GameApi.SetProtagonistGender("Female"); RefreshModule(); });
                    AddLine(body.transform, "Pronoun: " + p.Pronoun);
                    AddAction(body.transform, "Pronoun: He", delegate { GameApi.SetProtagonistPronoun("He"); RefreshModule(); });
                    AddAction(body.transform, "Pronoun: She", delegate { GameApi.SetProtagonistPronoun("She"); RefreshModule(); });
                    AddAction(body.transform, "Pronoun: They", delegate { GameApi.SetProtagonistPronoun("They"); RefreshModule(); });
                    AddAction(body.transform, "Sync pronoun with gender", delegate { GameApi.SyncProtagonistPronoun(); RefreshModule(); });
                    AddAction(body.transform, "Player level 1", delegate { GameApi.SetProtagonistLevel(1); RefreshModule(); });
                    AddAction(body.transform, "Player level 10", delegate { GameApi.SetProtagonistLevel(10); RefreshModule(); });
                    AddAction(body.transform, "Player level 30", delegate { GameApi.SetProtagonistLevel(30); RefreshModule(); });
                    AddAction(body.transform, "Player max level", delegate { GameApi.SetProtagonistLevel(TeamNimbus.CloudMeadow.Managers.GameManager.MaxLevel); RefreshModule(); });
                    AddAction(body.transform, "Fully heal player", delegate { GameApi.HealProtagonist(); RefreshModule(); });
                    AddAction(body.transform, "Set exact XP...", delegate { OpenPrompt("Player XP", p.XPSinceLastLevel.ToString("0"), delegate(string v) { float n; if (float.TryParse(v, out n)) GameApi.SetProtagonistXP(n); }); });
                    AddAction(body.transform, "Set stat points...", delegate { OpenPrompt("Stat points", p.NumStatPoints.ToString(), delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetProtagonistStatPoints(n); }); });
                    AddLine(body.transform, GameApi.GetProtagonistPrimaryStatsSummary());
                    string[] primaryNames = { "Physique", "Stamina", "Intuition", "Swiftness" };
                    for (int psi = 0; psi < primaryNames.Length; psi++)
                    {
                        string capturedPrimary = primaryNames[psi];
                        AddAction(body.transform, "Set " + capturedPrimary + "...", delegate { OpenPrompt(capturedPrimary, "10", delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetProtagonistPrimaryStat(capturedPrimary, n); }); });
                    }
                    AddAction(body.transform, "Add 1,000 Korona", delegate { GameApi.AddKorona(1000); RefreshModule(); });
                    AddAction(body.transform, "Add 100,000 Korona", delegate { GameApi.AddKorona(100000); RefreshModule(); });
                    AddAction(body.transform, "Add 10 upgrade shards", delegate { GameApi.AddShards(10); RefreshModule(); });
                    AddAction(body.transform, "Upgrade party abilities", delegate { GameApi.UpgradeAllAbilitiesForParty(); RefreshModule(); });
                    AddAction(body.transform, "Level player, companions and monsters to 30", delegate { GameApi.LevelAll(30); RefreshModule(); });
                    AddHeading(body.transform, "Genodriver");
                    AddLine(body.transform, "Unlocked: " + p.UnlockedGenodriverCells + " | Active: " + p.ActiveGenodriverIndex);
                    AddLine(body.transform, "Lifecycle: " + p.CurrentStatus + " | Status changes at: " + p.DateTimeStatusChanges + " | Pregnant/incubating: " + p.IsPregnant + " | Birthday: " + p.BirthDate);
                    AddAction(body.transform, "Reset lifecycle status to Idle", delegate { ConfirmAction("Reset player lifecycle status", delegate { GameApi.ResetProtagonistLifecycleStatus(); }); });
                    AddAction(body.transform, "Unlock Lizard", delegate { p.UnlockGenodriverForm(TeamNimbus.CloudMeadow.Monsters.GenodriverFlags.Lizard, false); RefreshModule(); });
                    AddAction(body.transform, "Unlock Bushy", delegate { p.UnlockGenodriverForm(TeamNimbus.CloudMeadow.Monsters.GenodriverFlags.Bushy, false); RefreshModule(); });
                    AddAction(body.transform, "Unlock Bird", delegate { p.UnlockGenodriverForm(TeamNimbus.CloudMeadow.Monsters.GenodriverFlags.Bird, false); RefreshModule(); });
                    AddAction(body.transform, "Activate Lizard", delegate { p.UnlockGenodriverForm(TeamNimbus.CloudMeadow.Monsters.GenodriverFlags.Lizard, true); RefreshModule(); });
                    AddAction(body.transform, "Activate Bushy", delegate { p.UnlockGenodriverForm(TeamNimbus.CloudMeadow.Monsters.GenodriverFlags.Bushy, true); RefreshModule(); });
                    AddAction(body.transform, "Activate Bird", delegate { p.UnlockGenodriverForm(TeamNimbus.CloudMeadow.Monsters.GenodriverFlags.Bird, true); RefreshModule(); });
                    AddHeading(body.transform, "Ability states");
                    string[] abilityLines = GameApi.GetProtagonistAbilitySummary();
                    for (int ali = 0; ali < abilityLines.Length; ali++) AddLine(body.transform, abilityLines[ali]);
                    AddAction(body.transform, "Clear ability cooldowns", delegate { GameApi.ClearProtagonistCooldowns(); RefreshModule(); });
                    for (int asi = 0; asi < abilityLines.Length; asi++)
                    {
                        int capturedSlot = asi;
                        AddAction(body.transform, "Set slot " + asi + " state...", delegate { OpenPrompt("Ability state index", "0", delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetProtagonistAbilityState(capturedSlot, n); }); });
                    }
                    TeamNimbus.CloudMeadow.Inventory.EquipmentItemEntry held, worn;
                    AddHeading(body.transform, "Equipment");
                    AddLine(body.transform, "Held: " + (p.TryToGetHeldItem(out held) && held != null ? held.ToString() : "(none)"));
                    AddLine(body.transform, "Worn: " + (p.TryToGetWornItem(out worn) && worn != null ? worn.ToString() : "(none)"));
                    AddAction(body.transform, "Unequip held", delegate { GameApi.UnequipProtagonist("Held"); RefreshModule(); });
                    AddAction(body.transform, "Unequip worn", delegate { GameApi.UnequipProtagonist("Worn"); RefreshModule(); });
                    var equipmentEntries = GameApi.GetPlayerEquipmentInventory();
                    AddAction(body.transform, _showEquipment ? "▼ Hide equipment inventory" : "▶ Equip from inventory (" + equipmentEntries.Length + ")", delegate { _showEquipment = !_showEquipment; RefreshModule(); });
                    if (_showEquipment) for (int eqi = 0; eqi < equipmentEntries.Length; eqi++) { var equipment = equipmentEntries[eqi]; AddAction(body.transform, "Equip: " + equipment, delegate { GameApi.EquipProtagonist(equipment); RefreshModule(); }); }
                }
                else AddLine(body.transform, "Game status is not loaded.");
            }
            else if (_module == "Advanced")
            {
                var allRoots = ReflectionUtil.CollectGameRoots(); var roots = new System.Collections.Generic.List<object>();
                for (int ari = 0; ari < allRoots.Count; ari++) if (allRoots[ari] != null && (string.IsNullOrEmpty(_advancedFilter) || allRoots[ari].GetType().FullName.IndexOf(_advancedFilter, StringComparison.OrdinalIgnoreCase) >= 0)) roots.Add(allRoots[ari]);
                AddLine(body.transform, "Runtime roots: " + roots.Count + " | Page " + (_advancedPage + 1));
                AddAction(body.transform, "Search runtime types...", delegate { OpenPrompt("Runtime type search", _advancedFilter, delegate(string v) { _advancedFilter = v ?? string.Empty; _advancedPage = 0; _advancedSelected = null; }); });
                AddAction(body.transform, "Previous page", delegate { if (_advancedPage > 0) _advancedPage--; RefreshModule(); });
                AddAction(body.transform, "Next page", delegate { if ((_advancedPage + 1) * PageSize < roots.Count) _advancedPage++; RefreshModule(); });
                int arStart = _advancedPage * PageSize, arEnd = Mathf.Min(roots.Count, arStart + PageSize);
                for (int ari = arStart; ari < arEnd; ari++)
                {
                    object root = roots[ari]; string typeName = root.GetType().FullName; object capturedRoot = root;
                    AddAction(body.transform, "Inspect " + typeName, delegate { _advancedSelected = capturedRoot; RefreshModule(); });
                }
                if (_advancedSelected != null)
                {
                    AddHeading(body.transform, _advancedSelected.GetType().FullName);
                    object jsonTarget = _advancedSelected;
                    AddAction(body.transform, "Export selected to JSON", delegate { GameApi.ExportObjectJson(jsonTarget, jsonTarget.GetType().Name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")); RefreshModule(); });
                    AddAction(body.transform, "Import JSON into selected...", delegate { OpenPrompt("Absolute JSON path", "", delegate(string v) { ConfirmAction("Import JSON into runtime object", delegate { GameApi.ImportObjectJson(jsonTarget, v); }); }); });
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                    var fields = _advancedSelected.GetType().GetFields(flags); int shown = 0;
                    for (int afi = 0; afi < fields.Length && shown < 60; afi++)
                    {
                        var field = fields[afi]; if (!IsSimpleEditorType(field.FieldType)) continue; object val = null; try { val = field.GetValue(_advancedSelected); } catch { }
                        string member = field.Name; object selected = _advancedSelected; string initial = val != null ? val.ToString() : string.Empty;
                        AddAction(body.transform, member + " = " + initial, delegate { OpenPrompt(member, initial, delegate(string v) { GameApi.SetMemberFromString(selected, member, v); }); }); shown++;
                    }
                    var props = _advancedSelected.GetType().GetProperties(flags);
                    for (int api = 0; api < props.Length && shown < 60; api++)
                    {
                        var prop = props[api]; if (!prop.CanRead || !prop.CanWrite || prop.GetIndexParameters().Length != 0 || !IsSimpleEditorType(prop.PropertyType)) continue; object val = null; try { val = prop.GetValue(_advancedSelected, null); } catch { }
                        string member = prop.Name; object selected = _advancedSelected; string initial = val != null ? val.ToString() : string.Empty;
                        AddAction(body.transform, member + " = " + initial, delegate { OpenPrompt(member, initial, delegate(string v) { GameApi.SetMemberFromString(selected, member, v); }); }); shown++;
                    }
                }
            }
            else if (_module == "Diagnostics")
            {
                AddLine(body.transform, "Unity: " + Application.unityVersion);
                AddLine(body.transform, GameApi.GetCompatibilitySummary());
                AddLine(body.transform, "Game API ready: " + GameApi.Ready);
                AddLine(body.transform, "Loaded quests discovered: " + (GameApi.Ready ? GameApiQuest.GetAllQuests().Length.ToString() : "n/a"));
                AddAction(body.transform, "Run consistency audit", delegate { GameApi.RunSafeConsistencyAuditAndFix(); RefreshModule(); });
                AddAction(body.transform, "Backup all saves now", delegate { GameApi.BackupAllSaves(); RefreshModule(); });
                AddAction(body.transform, "Restore latest backup (main menu only)", delegate { ConfirmWithoutBackup("Restore latest save backup", delegate { GameApi.RestoreLatestSaveBackup(); }); });
                string[] backups = GameApi.GetSaveBackupSummary();
                AddHeading(body.transform, "Save backups: " + backups.Length + "/10");
                for (int bsi = 0; bsi < backups.Length; bsi++) AddLine(body.transform, backups[bsi]);
                AddAction(body.transform, GameApi.VerboseDiagnosticsEnabled ? "Verbose diagnostics: ON" : "Verbose diagnostics: OFF", delegate { GameApi.SetVerboseDiagnostics(!GameApi.VerboseDiagnosticsEnabled); RefreshModule(); });
                AddAction(body.transform, "Export farm traits report", delegate { GameApi.GenerateFarmTraitsReport(System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx\\plugins\\CloudMeadowCreativeMode\\farm_traits_report.txt")); RefreshModule(); });
                AddAction(body.transform, "Export all traits catalog", delegate { GameApi.GenerateAllTraitsCatalog(System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx\\plugins\\CloudMeadowCreativeMode\\all_traits_catalog.txt")); RefreshModule(); });
                AddAction(body.transform, "Write compatibility snapshot + diff", delegate { GameApi.WriteCompatibilityReport(System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx\\plugins\\CloudMeadowCreativeMode\\compatibility_report.txt")); RefreshModule(); });
                AddHeading(body.transform, "Recent operations");
                string[] validationLines = TransactionManager.LastValidation();
                for (int vli = 0; vli < validationLines.Length; vli++) AddLine(body.transform, validationLines[vli]);
                string[] recent = LogBuffer.Snapshot(); int logStart = Mathf.Max(0, recent.Length - 30);
                for (int li = logStart; li < recent.Length; li++) AddLine(body.transform, recent[li]);
                AddHeading(body.transform, "Compatibility checks");
                string[] checks = GameApi.GetCompatibilityChecks();
                for (int cci = 0; cci < checks.Length; cci++) AddLine(body.transform, checks[cci]);
                string[] discovery = GameApi.GetContentDiscoverySummary();
                for (int dci = 0; dci < discovery.Length; dci++) AddLine(body.transform, discovery[dci]);
            }
            else if (_module == "Party")
            {
                if (!GameApi.Ready) AddLine(body.transform, "Game status is not loaded.");
                else
                {
                    var companions = TeamNimbus.CloudMeadow.Managers.GameManager.Status.Companions;
                    AddLine(body.transform, "Companions: " + companions.Count);
                    AddAction(body.transform, "Recruit all companions (Lv 10)", delegate { GameApi.RecruitAllCompanions(10); RefreshModule(); });
                    AddAction(body.transform, "Level companions to 20", delegate { GameApi.LevelCompanions(20); RefreshModule(); });
                    AddAction(body.transform, "Upgrade party abilities", delegate { GameApi.UpgradeAllAbilitiesForParty(); RefreshModule(); });
                    for (int ci = 0; ci < companions.Count; ci++)
                    {
                        var companion = companions[ci]; if (companion == null) continue;
                        AddLine(body.transform, companion.Name + " | Lv " + companion.Level + " | " + companion.Gender);
                    }
                }
            }
            else if (_module == "Monsters")
            {
                var rawMonsters = GameApi.Ready ? GameApi.GetActiveMonsters() : new TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats[0];
                var monsterView = new System.Collections.Generic.List<TeamNimbus.CloudMeadow.Monsters.MonsterCharacterStats>();
                for (int mvi = 0; mvi < rawMonsters.Length; mvi++)
                {
                    var candidate = rawMonsters[mvi]; if (candidate == null) continue; bool match = string.IsNullOrEmpty(_monsterFilter) || candidate.Name.IndexOf(_monsterFilter, StringComparison.OrdinalIgnoreCase) >= 0 || candidate.FarmableSpecies.ToString().IndexOf(_monsterFilter, StringComparison.OrdinalIgnoreCase) >= 0 || candidate.Gender.ToString().IndexOf(_monsterFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!match) { object[] candidateTraits = GameApi.GetMonsterTraits(candidate); for (int cti = 0; cti < candidateTraits.Length && !match; cti++) match = GameApi.GetTraitDisplayName(candidateTraits[cti]).IndexOf(_monsterFilter, StringComparison.OrdinalIgnoreCase) >= 0; }
                    if (match) monsterView.Add(candidate);
                }
                var monsters = monsterView.ToArray();
                AddLine(body.transform, "Farm monsters: " + monsters.Length + "    Page: " + (_monsterPage + 1));
                AddLine(body.transform, "Batch selected: " + _selectedMonsters.Count + " | Filter: " + (string.IsNullOrEmpty(_monsterFilter) ? "none" : _monsterFilter));
                AddAction(body.transform, "Search name/species/gender/trait...", delegate { OpenPrompt("Monster filter", _monsterFilter, delegate(string v) { _monsterFilter = v ?? string.Empty; _monsterPage = 0; }); });
                AddAction(body.transform, "Clear filter", delegate { _monsterFilter = string.Empty; _monsterPage = 0; RefreshModule(); });
                AddAction(body.transform, "Select filtered", delegate { for (int smi = 0; smi < monsters.Length; smi++) _selectedMonsters.Add(monsters[smi]); RefreshModule(); });
                AddAction(body.transform, "Clear batch selection", delegate { _selectedMonsters.Clear(); RefreshModule(); });
                AddAction(body.transform, "Selected level 30", delegate { foreach (var m in _selectedMonsters) GameApi.SetMonsterLevel(m, 30); RefreshModule(); });
                AddAction(body.transform, "Selected max loyalty", delegate { foreach (var m in _selectedMonsters) GameApi.SetMonsterLoyalty(m, 110); RefreshModule(); });
                AddAction(body.transform, "Max all loyalty", delegate { GameApi.MaxAllMonstersLoyalty(); RefreshModule(); });
                AddAction(body.transform, "Give every monster", delegate { GameApi.GiveEveryMonster(); RefreshModule(); });
                AddAction(body.transform, "Level monsters to 10", delegate { GameApi.LevelMonsters(10); RefreshModule(); });
                AddAction(body.transform, "Level monsters to 30", delegate { GameApi.LevelMonsters(30); RefreshModule(); });
                AddAction(body.transform, "Recruit companions (Lv 15)", delegate { GameApi.RecruitAllCompanions(15); RefreshModule(); });
                AddAction(body.transform, "Dump monster diagnostics", delegate { GameApi.DumpMonstersDebug(); RefreshModule(); });
                AddAction(body.transform, _showSpeciesCatalog ? "▼ Hide species catalog" : "▶ Add monster by species", delegate { _showSpeciesCatalog = !_showSpeciesCatalog; RefreshModule(); });
                string[] speciesNames = Enum.GetNames(typeof(TeamNimbus.CloudMeadow.Monsters.FarmableSpecies));
                if (_showSpeciesCatalog) for (int si = 0; si < speciesNames.Length; si++)
                {
                    string capturedSpecies = speciesNames[si];
                    AddAction(body.transform, "Add " + capturedSpecies + " (Lv 15)", delegate { GameApi.AddMonster(capturedSpecies, 15); RefreshModule(); });
                }
                AddAction(body.transform, "Spawn Chimera variants (Lv 15)", delegate {
                    string[] variants = GameApi.GetSpeciesTraitNamesForSpecies("Chimera");
                    for (int vi = 0; vi < variants.Length; vi++) GameApi.SpawnChimeraVariant(variants[vi], 15);
                    RefreshModule();
                });
                AddAction(body.transform, "Previous page", delegate { if (_monsterPage > 0) _monsterPage--; RefreshModule(); });
                AddAction(body.transform, "Next page", delegate { if ((_monsterPage + 1) * PageSize < monsters.Length) _monsterPage++; RefreshModule(); });
                var monsterColumns = new GameObject("MonsterColumns", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
                monsterColumns.transform.SetParent(body.transform, false);
                var monsterHorizontal = monsterColumns.GetComponent<HorizontalLayoutGroup>();
                monsterHorizontal.spacing = 14; monsterHorizontal.childControlWidth = true; monsterHorizontal.childForceExpandWidth = true; monsterHorizontal.childControlHeight = true; monsterHorizontal.childForceExpandHeight = false;
                monsterColumns.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Transform monsterList = CreateColumn(monsterColumns.transform, 0.34f, new Color(0.06f, 0.10f, 0.13f, 0.9f));
                Transform monsterInspector = CreateColumn(monsterColumns.transform, 0.66f, new Color(0.045f, 0.075f, 0.095f, 0.9f));
                if (_selectedMonster != null)
                {
                    AddHeading(monsterInspector, "Selected: " + _selectedMonster.Name);
                    AddAction(monsterInspector, "Collapse monster inspector", delegate { _selectedMonster = null; RefreshModule(); });
                    AddLine(monsterInspector, "Species: " + _selectedMonster.FarmableSpecies + " | Gender: " + _selectedMonster.Gender + " | Pigment: " + GameApi.GetMonsterPigment(_selectedMonster));
                    AddHeading(monsterInspector, "Lifecycle and genealogy");
                    string[] familyLines = GameApi.GetMonsterFamilySummary(_selectedMonster);
                    for (int fli = 0; fli < familyLines.Length; fli++) AddLine(monsterInspector, familyLines[fli]);
                    var familyMonster = _selectedMonster;
                    AddAction(monsterInspector, "Set parent IDs...", delegate { OpenPrompt("Parent IDs: first,second", familyMonster.FirstParentID + "," + familyMonster.SecondParentID, delegate(string v) { string[] parts = (v ?? "").Split(','); int a, b; if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out a) && int.TryParse(parts[1].Trim(), out b)) GameApi.SetMonsterParents(familyMonster, a, b); }); });
                    AddAction(monsterInspector, "Clear parent links", delegate { ConfirmAction("Clear monster parent links", delegate { GameApi.ClearMonsterParents(familyMonster); }); });
                    AddAction(monsterInspector, "Export family tree", delegate { GameApi.ExportFamilyTree(System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx\\plugins\\CloudMeadowCreativeMode\\family_tree.tsv")); RefreshModule(); });
                    AddAction(monsterInspector, "Export monster JSON", delegate { GameApi.ExportObjectJson(familyMonster, familyMonster.Name + "_" + familyMonster.PartyCharacterID); RefreshModule(); });
                    AddAction(monsterInspector, _showJobs ? "▼ Hide job assignment" : "▶ Job assignment", delegate { _showJobs = !_showJobs; RefreshModule(); });
                    if (_showJobs)
                    {
                        AddAction(monsterInspector, "Quit current job", delegate { GameApi.QuitMonsterJob(familyMonster); RefreshModule(); });
                        int plotCount = TeamNimbus.CloudMeadow.Managers.GameManager.Status.FarmStatus.Plots.Length;
                        string[] roles = Enum.GetNames(typeof(TeamNimbus.CloudMeadow.Monsters.JobRole));
                        for (int jpi = 0; jpi < plotCount; jpi++) for (int jri = 0; jri < roles.Length; jri++)
                        {
                            if (roles[jri] == "NotWorking") continue; int capturedPlot = jpi; string capturedRole = roles[jri];
                            AddAction(monsterInspector, "Plot " + jpi + " / " + capturedRole, delegate { GameApi.AssignMonsterToPlot(familyMonster, capturedPlot, capturedRole); RefreshModule(); });
                        }
                    }
                    var stateMonster = _selectedMonster;
                    AddAction(monsterInspector, "Set exact level...", delegate { OpenPrompt("Monster level", stateMonster.Level.ToString(), delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetMonsterLevel(stateMonster, n); }); });
                    AddLine(monsterInspector, GameApi.GetPrimaryStatsSummary(stateMonster));
                    string[] monsterPrimary = { "Physique", "Stamina", "Intuition", "Swiftness" };
                    for (int mpsi = 0; mpsi < monsterPrimary.Length; mpsi++)
                    {
                        string statName = monsterPrimary[mpsi]; var statMonster = stateMonster;
                        AddAction(monsterInspector, "Set " + statName + "...", delegate { OpenPrompt(statName, "10", delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetPrimaryStat(statMonster, statName, n); }); });
                    }
                    AddAction(monsterInspector, _showSpeciesEditor ? "▼ Hide species editor" : "▶ Change species", delegate { _showSpeciesEditor = !_showSpeciesEditor; RefreshModule(); });
                    if (_showSpeciesEditor)
                    {
                        string[] targetSpecies = Enum.GetNames(typeof(TeamNimbus.CloudMeadow.Monsters.FarmableSpecies));
                        for (int tsi = 0; tsi < targetSpecies.Length; tsi++)
                        {
                            string speciesTarget = targetSpecies[tsi]; var speciesMonster = stateMonster;
                            AddAction(monsterInspector, "Change to " + speciesTarget, delegate { GameApi.SetMonsterSpecies(speciesMonster, speciesTarget); RefreshModule(); });
                        }
                    }
                    if (string.Equals(stateMonster.FarmableSpecies.ToString(), "Chimera", StringComparison.OrdinalIgnoreCase))
                    {
                        AddAction(monsterInspector, _showChimeraEditor ? "▼ Hide Chimera variants" : "▶ Chimera variant", delegate { _showChimeraEditor = !_showChimeraEditor; RefreshModule(); });
                        if (_showChimeraEditor)
                        {
                            string[] variants = GameApi.GetSpeciesTraitNamesForSpecies("Chimera");
                            for (int cvi = 0; cvi < variants.Length; cvi++)
                            {
                                string variant = variants[cvi]; var chimera = stateMonster;
                                AddAction(monsterInspector, "Variant: " + variant + " (Grade 1)", delegate { GameApi.SetChimeraVariant(chimera, variant, 1); RefreshModule(); });
                                AddAction(monsterInspector, "Variant: " + variant + " (Grade 5)", delegate { GameApi.SetChimeraVariant(chimera, variant, 5); RefreshModule(); });
                            }
                        }
                    }
                    AddAction(monsterInspector, "Loyalty: maximum", delegate { GameApi.SetMonsterLoyalty(stateMonster, 110); RefreshModule(); });
                    AddAction(monsterInspector, "Loyalty: zero", delegate { GameApi.SetMonsterLoyalty(stateMonster, 0); RefreshModule(); });
                    AddAction(monsterInspector, "Loyal flag: ON", delegate { GameApi.SetMonsterIsLoyal(stateMonster, true); RefreshModule(); });
                    AddAction(monsterInspector, "Loyal flag: OFF", delegate { GameApi.SetMonsterIsLoyal(stateMonster, false); RefreshModule(); });
                    AddAction(monsterInspector, "Set fertile", delegate { GameApi.SetMonsterInfertile(stateMonster, false); RefreshModule(); });
                    AddAction(monsterInspector, "Set infertile", delegate { GameApi.SetMonsterInfertile(stateMonster, true); RefreshModule(); });
                    AddAction(monsterInspector, "Set dry", delegate { GameApi.SetMonsterDry(stateMonster, true); RefreshModule(); });
                    AddAction(monsterInspector, "Set not dry", delegate { GameApi.SetMonsterDry(stateMonster, false); RefreshModule(); });
                    string[] pigments = GameApi.GetAvailablePigments();
                    AddAction(monsterInspector, _showPigments ? "▼ Hide pigments" : "▶ Pigments (" + pigments.Length + ")", delegate { _showPigments = !_showPigments; RefreshModule(); });
                    if (_showPigments) for (int pi = 0; pi < pigments.Length; pi++)
                    {
                        string capturedPigment = pigments[pi]; var pigmentMonster = _selectedMonster;
                        AddAction(monsterInspector, "Pigment: " + capturedPigment, delegate { GameApi.SetMonsterPigment(pigmentMonster, capturedPigment); RefreshModule(); });
                    }
                    object[] traits = GameApi.GetMonsterTraits(_selectedMonster);
                    AddLine(monsterInspector, "Traits: " + traits.Length);
                    for (int ti = 0; ti < traits.Length; ti++)
                    {
                        object trait = traits[ti]; if (trait == null) continue;
                        AddLine(monsterInspector, GameApi.GetTraitDisplayName(trait) + "  [" + GameApi.GetTraitQuality(trait) + "]");
                        AddLine(monsterInspector, GameApi.GetTraitDescription(trait));
                        object capturedTrait = trait; var traitMonster = _selectedMonster;
                        AddAction(monsterInspector, "Set trait #" + (ti + 1) + " grade...", delegate { OpenPrompt("Trait grade", "1", delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetTraitGrade(capturedTrait, n); }); });
                        AddAction(monsterInspector, "Max trait #" + (ti + 1), delegate { GameApi.MaxTraitGrade(capturedTrait); RefreshModule(); });
                        AddAction(monsterInspector, "Remove trait #" + (ti + 1), delegate { GameApi.RemoveTraitFromMonster(traitMonster, capturedTrait); RefreshModule(); });
                    }
                    object[] availableTraits = GameApi.GetTraitDefinitionsForMonster(_selectedMonster);
                    AddAction(monsterInspector, _showAvailableTraits ? "▼ Hide available traits" : "▶ Add trait (" + availableTraits.Length + ")", delegate { _showAvailableTraits = !_showAvailableTraits; RefreshModule(); });
                    int availableStart = _traitPage * 18, availableLimit = Mathf.Min(availableTraits.Length, availableStart + 18);
                    Transform traitGrid = null;
                    if (_showAvailableTraits)
                    {
                        AddLine(monsterInspector, "Trait catalog page " + (_traitPage + 1) + " / " + Mathf.Max(1, Mathf.CeilToInt(availableTraits.Length / 18f)));
                        AddAction(monsterInspector, "Previous trait page", delegate { if (_traitPage > 0) _traitPage--; RefreshModule(); });
                        AddAction(monsterInspector, "Next trait page", delegate { if ((_traitPage + 1) * 18 < availableTraits.Length) _traitPage++; RefreshModule(); });
                        var gridObject = new GameObject("TraitGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
                        gridObject.transform.SetParent(monsterInspector, false);
                        var grid = gridObject.GetComponent<GridLayoutGroup>();
                        grid.cellSize = new Vector2(285, 92); grid.spacing = new Vector2(10, 10); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
                        var gridFit = gridObject.GetComponent<ContentSizeFitter>(); gridFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        traitGrid = gridObject.transform;
                    }
                    if (_showAvailableTraits) for (int ati = availableStart; ati < availableLimit; ati++)
                    {
                        object traitDef = availableTraits[ati]; if (traitDef == null || GameApi.MonsterHasTrait(_selectedMonster, traitDef)) continue;
                        object capturedDef = traitDef; var addTraitMonster = _selectedMonster;
                        AddTraitAction(traitGrid, GameApi.GetTraitDisplayName(traitDef), GameApi.GetTraitDescription(traitDef), GameApi.GetTraitQuality(traitDef), delegate { GameApi.AddTraitToMonster(addTraitMonster, capturedDef, 1); RefreshModule(); });
                    }
                }
                else
                {
                    AddHeading(monsterInspector, "Monster inspector");
                    AddLine(monsterInspector, "Select a monster from the list on the left.");
                }
                int mStart = _monsterPage * PageSize, mEnd = Mathf.Min(monsters.Length, mStart + PageSize);
                for (int mi = mStart; mi < mEnd; mi++)
                {
                    var monster = monsters[mi]; if (monster == null) continue;
                    AddLine(monsterList, "#" + (mi + 1) + "  " + monster.Name + "  | " + monster.FarmableSpecies + " | Lv " + monster.Level + " | " + monster.Gender);
                    var capturedMonster = monster;
                    AddAction(monsterList, (_selectedMonsters.Contains(monster) ? "✓ Batch: " : "Batch select: ") + monster.Name, delegate { if (!_selectedMonsters.Add(capturedMonster)) _selectedMonsters.Remove(capturedMonster); RefreshModule(); });
                    AddAction(monsterList, "Inspect: " + monster.Name, delegate { _selectedMonster = capturedMonster; RefreshModule(); });
                    AddAction(monsterList, "Swap gender: " + monster.Name, delegate { GameApi.SwapMonsterGender(capturedMonster); RefreshModule(); });
                    AddAction(monsterList, "Remove: " + monster.Name, delegate { ConfirmAction("Remove " + capturedMonster.Name, delegate { GameApi.RemoveMonster(capturedMonster); }); });
                }
            }
            else if (_module == "Inventory")
            {
                object[] containers = GameApi.Ready ? GameApi.GetInventoryContainers() : new object[0];
                if (_inventoryContainer == null && containers.Length > 0) _inventoryContainer = containers[0];
                AddHeading(body.transform, "Containers");
                for (int ici = 0; ici < containers.Length; ici++)
                {
                    object capturedContainer = containers[ici]; string containerName = GameApi.GetInventoryContainerName(capturedContainer);
                    AddAction(body.transform, (object.ReferenceEquals(_inventoryContainer, capturedContainer) ? "✓ " : "") + containerName, delegate { _inventoryContainer = capturedContainer; _inventoryPage = 0; _selectedInventory.Clear(); RefreshModule(); });
                }
                var rawEntries = GameApi.Ready ? GameApi.GetInventoryEntriesFrom(_inventoryContainer) : new object[0];
                var entryView = new System.Collections.Generic.List<object>();
                var categorySet = new System.Collections.Generic.List<string>();
                for (int evi = 0; evi < rawEntries.Length; evi++)
                {
                    object candidateEntry = rawEntries[evi]; if (candidateEntry == null) continue;
                    object candidateDef = GameApi.GetEntryDefinitionForUI(candidateEntry);
                    string candidateCategory = GameApi.GetItemCategoryName(candidateDef); if (!string.IsNullOrEmpty(candidateCategory) && !categorySet.Contains(candidateCategory)) categorySet.Add(candidateCategory);
                    bool categoryMatch = _inventoryCategory == "All" || string.Equals(candidateCategory, _inventoryCategory, StringComparison.OrdinalIgnoreCase);
                    string searchText = candidateEntry + " " + GameApi.GetEntryInspectorSummary(candidateEntry) + " " + candidateCategory;
                    bool searchMatch = string.IsNullOrEmpty(_inventoryFilter) || searchText.IndexOf(_inventoryFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (categoryMatch && searchMatch) entryView.Add(candidateEntry);
                }
                var entries = entryView.ToArray();
                AddLine(body.transform, GameApi.GetInventoryContainerName(_inventoryContainer) + " entries: " + entries.Length + "    Page: " + (_inventoryPage + 1));
                AddLine(body.transform, "Selected: " + _selectedInventory.Count);
                AddLine(body.transform, "Filter: " + (string.IsNullOrEmpty(_inventoryFilter) ? "(none)" : _inventoryFilter));
                AddLine(body.transform, "Category: " + _inventoryCategory);
                AddAction(body.transform, _showInventoryCategories ? "▼ Hide category filters" : "▶ Category filters (" + categorySet.Count + ")", delegate { _showInventoryCategories = !_showInventoryCategories; RefreshModule(); });
                if (_showInventoryCategories)
                {
                    AddAction(body.transform, (_inventoryCategory == "All" ? "✓ " : "") + "Category: All", delegate { _inventoryCategory = "All"; _inventoryPage = 0; RefreshModule(); });
                    foreach (string discoveredCategory in categorySet)
                    {
                        string capturedCategory = discoveredCategory;
                        AddAction(body.transform, (string.Equals(_inventoryCategory, capturedCategory, StringComparison.OrdinalIgnoreCase) ? "✓ " : "") + "Category: " + capturedCategory, delegate { _inventoryCategory = capturedCategory; _inventoryPage = 0; RefreshModule(); });
                    }
                }
                AddAction(body.transform, "Search inventory...", delegate { OpenPrompt("Inventory search", _inventoryFilter, delegate(string v) { _inventoryFilter = v ?? string.Empty; _inventoryPage = 0; }); });
                AddAction(body.transform, "Clear search", delegate { _inventoryFilter = string.Empty; _inventoryPage = 0; RefreshModule(); });
                AddAction(body.transform, "Select all filtered", delegate { for (int si = 0; si < entries.Length; si++) if (entries[si] != null) _selectedInventory.Add(entries[si]); RefreshModule(); });
                AddAction(body.transform, "Invert filtered selection", delegate { for (int si = 0; si < entries.Length; si++) if (entries[si] != null && !_selectedInventory.Add(entries[si])) _selectedInventory.Remove(entries[si]); RefreshModule(); });
                AddAction(body.transform, "Clear selection", delegate { _selectedInventory.Clear(); RefreshModule(); });
                AddAction(body.transform, "Selected: +1", delegate { foreach (object x in _selectedInventory) GameApi.AdjustEntryQuantity(x, 1); RefreshModule(); });
                AddAction(body.transform, "Selected: -1", delegate { foreach (object x in _selectedInventory) GameApi.AdjustEntryQuantity(x, -1); RefreshModule(); });
                AddAction(body.transform, "Selected: max quality", delegate { foreach (object x in _selectedInventory) GameApi.SetEntryMaxQuality(x); RefreshModule(); });
                AddAction(body.transform, "Max quality of inventory", delegate { GameApi.SetAllInventoryEntriesMaxQuality(); RefreshModule(); });
                AddAction(body.transform, "Add harvest and groceries", delegate { GameApi.AddHarvestAndGroceries(); RefreshModule(); });
                AddAction(body.transform, "Add all items x1", delegate { ConfirmAction("Add every item", delegate { GameApi.AddAllItems(1, 0); }); });
                AddAction(body.transform, "Make all incubator eggs ready", delegate { GameApi.HatchAllEggs(); RefreshModule(); });
                AddAction(body.transform, _showItemCatalog ? "▼ Hide item catalog" : "▶ Add items from catalog", delegate { _showItemCatalog = !_showItemCatalog; RefreshModule(); });
                if (_showItemCatalog)
                {
                    object[] definitions = GameApi.GetAllItemDefinitions();
                    AddLine(body.transform, "Item definitions: " + definitions.Length + " | Page " + (_itemCatalogPage + 1));
                    AddAction(body.transform, "Previous catalog page", delegate { if (_itemCatalogPage > 0) _itemCatalogPage--; RefreshModule(); });
                    AddAction(body.transform, "Next catalog page", delegate { if ((_itemCatalogPage + 1) * PageSize < definitions.Length) _itemCatalogPage++; RefreshModule(); });
                    int dcStart = _itemCatalogPage * PageSize, dcEnd = Mathf.Min(definitions.Length, dcStart + PageSize);
                    for (int di = dcStart; di < dcEnd; di++)
                    {
                        object definition = definitions[di]; if (definition == null) continue; object capturedDefinition = definition;
                        AddLine(body.transform, definition + " | " + GameApi.GetItemCategoryName(definition));
                        AddAction(body.transform, "Add 1: " + definition, delegate { GameApi.AddItemByDefinition(capturedDefinition, 1, 0); RefreshModule(); });
                        AddAction(body.transform, "Add 99 max quality: " + definition, delegate { GameApi.AddItemByDefinition(capturedDefinition, 99, 4); RefreshModule(); });
                    }
                }
                AddAction(body.transform, "Previous page", delegate { if (_inventoryPage > 0) _inventoryPage--; RefreshModule(); });
                AddAction(body.transform, "Next page", delegate { if ((_inventoryPage + 1) * PageSize < entries.Length) _inventoryPage++; RefreshModule(); });
                int iStart = _inventoryPage * PageSize, iEnd = Mathf.Min(entries.Length, iStart + PageSize);
                for (int ii = iStart; ii < iEnd; ii++)
                {
                    object entry = entries[ii]; if (entry == null) continue;
                    AddLine(body.transform, "#" + (ii + 1) + "  " + entry);
                    AddLine(body.transform, GameApi.GetEntryInspectorSummary(entry));
                    if (GameApiQuest.IsActiveQuestItem(entry)) AddLine(body.transform, "⚠ Active quest item — destructive quantity changes require care");
                    object capturedEntry = entry;
                    AddAction(body.transform, (_selectedInventory.Contains(entry) ? "✓ Selected #" : "Select #") + (ii + 1), delegate { if (!_selectedInventory.Add(capturedEntry)) _selectedInventory.Remove(capturedEntry); RefreshModule(); });
                    AddAction(body.transform, "Set quantity item #" + (ii + 1) + "...", delegate { OpenPrompt("Item quantity", GameApi.GetEntryQuantityForUI(capturedEntry).ToString(), delegate(string v) { int n; if (int.TryParse(v, out n)) { if (GameApiQuest.IsActiveQuestItem(capturedEntry) && n < GameApi.GetEntryQuantityForUI(capturedEntry)) ConfirmAction("Reduce active quest item", delegate { GameApi.SetEntryQuantity(capturedEntry, n); }); else GameApi.SetEntryQuantity(capturedEntry, n); } }); });
                    AddAction(body.transform, "+1 to item #" + (ii + 1), delegate { GameApi.AdjustEntryQuantity(capturedEntry, 1); RefreshModule(); });
                    AddAction(body.transform, "-1 from item #" + (ii + 1), delegate { if (GameApiQuest.IsActiveQuestItem(capturedEntry)) ConfirmAction("Reduce active quest item", delegate { GameApi.AdjustEntryQuantity(capturedEntry, -1); }); else { GameApi.AdjustEntryQuantity(capturedEntry, -1); RefreshModule(); } });
                    AddAction(body.transform, "Max quality item #" + (ii + 1), delegate { GameApi.SetEntryMaxQuality(capturedEntry); RefreshModule(); });
                }
            }
            else if (_module == "Eggs")
            {
                object[] eggs = GameApi.Ready ? GameApi.GetIncubatorEggs() : new object[0];
                AddLine(body.transform, "Incubator eggs: " + eggs.Length + "    Page: " + (_eggPage + 1));
                AddLine(body.transform, GameApi.GetFarmCapacitySummary());
                AddAction(body.transform, "Make all eggs ready", delegate { GameApi.HatchAllEggs(); RefreshModule(); });
                AddAction(body.transform, "Previous page", delegate { if (_eggPage > 0) _eggPage--; RefreshModule(); });
                AddAction(body.transform, "Next page", delegate { if ((_eggPage + 1) * PageSize < eggs.Length) _eggPage++; RefreshModule(); });
                int eggStart = _eggPage * PageSize, eggEnd = Mathf.Min(eggs.Length, eggStart + PageSize);
                for (int ei = eggStart; ei < eggEnd; ei++)
                {
                    object egg = eggs[ei]; if (egg == null) continue;
                    AddHeading(body.transform, GameApi.GetEggDisplayName(egg));
                    AddLine(body.transform, "Timer: " + GameApi.GetEggTimerString(egg));
                    string[] eggLines = GameApi.GetEggInspectorLines(egg);
                    for (int eli = 0; eli < eggLines.Length; eli++) AddLine(body.transform, eggLines[eli]);
                    object capturedEgg = egg;
                    var typedEgg = GameApi.ResolveEggEntry(egg);
                    if (typedEgg != null)
                    {
                        AddLine(body.transform, "Species: " + typedEgg.Species + " | Parents: " + (typedEgg.HasParents ? typedEgg.FirstParentID + ", " + typedEgg.SecondParentID : "unknown") + " | Saturation: " + typedEgg.MagicalSaturationAtCreation);
                        AddAction(body.transform, "Set egg parent IDs...", delegate { OpenPrompt("Egg parents: first,second", typedEgg.FirstParentID + "," + typedEgg.SecondParentID, delegate(string v) { string[] parts = (v ?? "").Split(','); int a, b; if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out a) && int.TryParse(parts[1].Trim(), out b)) GameApi.SetEggParents(capturedEgg, a, b); }); });
                        AddAction(body.transform, "Copy egg to inventory", delegate { GameApi.CopyEggToInventory(capturedEgg); RefreshModule(); });
                        AddAction(body.transform, "Reroll genetics as inventory copy", delegate { GameApi.RerollEggCopyToInventory(capturedEgg); RefreshModule(); });
                    }
                    AddAction(body.transform, "Make this egg ready", delegate { GameApi.HatchEgg(capturedEgg); RefreshModule(); });
                }
            }
            else if (_module == "Farm")
            {
                if (GameApi.Ready)
                {
                    var farmStatus = TeamNimbus.CloudMeadow.Managers.GameManager.Status;
                    AddLine(body.transform, "Farm level: " + farmStatus.FarmLevel + " | Monsters: " + farmStatus.NumMonstersOnTheFarm + " | Breeding couples: " + farmStatus.BreedingCouples.Count);
                    for (int bci = 0; bci < farmStatus.BreedingCouples.Count; bci++) AddLine(body.transform, "Breeding #" + (bci + 1) + ": " + farmStatus.BreedingCouples[bci]);
                }
                AddAction(body.transform, "Upgrade farm", delegate { GameApi.UpgradeFarm(); RefreshModule(); });
                AddAction(body.transform, "Water all crops", delegate { GameApi.WaterAllCrops(); RefreshModule(); });
                AddAction(body.transform, "Grow all crops", delegate { GameApi.GrowAllCrops(); RefreshModule(); });
                AddAction(body.transform, "Extra harvest charges: 10", delegate { GameApi.SetExtraHarvestTimesForAll(10); RefreshModule(); });
                AddAction(body.transform, "Ultra Bread: " + (GameApi.UltraBreadEnabled ? "ON" : "OFF"), delegate { GameApi.ToggleUltraBread(); RefreshModule(); });
                AddAction(body.transform, "Clear barn", delegate { ConfirmAction("Clear barn", delegate { GameApi.ClearBarn(); }); });
                AddHeading(body.transform, "Integrity audit");
                string[] farmAudit = GameApi.AuditFarmIntegrity(); for (int fai = 0; fai < farmAudit.Length; fai++) AddLine(body.transform, farmAudit[fai]);
                AddHeading(body.transform, "Buildings");
                string[] buildingLines = GameApi.GetFarmBuildingSummary();
                for (int bli = 0; bli < buildingLines.Length; bli++)
                {
                    AddLine(body.transform, buildingLines[bli]); int buildingIndex = bli;
                    AddAction(body.transform, "Change building slot " + bli + "...", delegate { OpenPrompt("Building type", "Field", delegate(string v) { GameApi.SetFarmBuildingType(buildingIndex, v); }); });
                }
                AddHeading(body.transform, "Crop plots");
                string[] plotLines = GameApi.GetFarmPlotSummary();
                for (int pli = 0; pli < plotLines.Length; pli++)
                {
                    int plotIndex = pli; AddLine(body.transform, plotLines[pli]);
                    AddAction(body.transform, "Upgrade plot " + pli, delegate { GameApi.UpgradeFarmPlot(plotIndex); RefreshModule(); });
                    AddAction(body.transform, "Water plot " + pli, delegate { GameApi.WaterFarmPlot(plotIndex); RefreshModule(); });
                    AddAction(body.transform, "Grow plot " + pli, delegate { GameApi.GrowFarmPlot(plotIndex); RefreshModule(); });
                    AddAction(body.transform, "Reset plot " + pli, delegate { ConfirmAction("Reset crop plot", delegate { GameApi.ResetFarmPlot(plotIndex); }); });
                }
                if (GameApi.Ready) AddSimpleObjectEditor(body.transform, TeamNimbus.CloudMeadow.Managers.GameManager.Status.FarmStatus, 36, "Farm persistent fields");
            }
            else if (_module == "Quests")
            {
                var rawQuests = GameApi.Ready ? GameApiQuest.GetAllQuests() : new TeamNimbus.CloudMeadow.Story.QuestSystem.QuestInfo[0];
                var questView = new System.Collections.Generic.List<TeamNimbus.CloudMeadow.Story.QuestSystem.QuestInfo>();
                for (int qvi = 0; qvi < rawQuests.Length; qvi++)
                {
                    var candidate = rawQuests[qvi]; if (candidate == null) continue;
                    string candidateStatus = GameApiQuest.GetQuestStatus(candidate);
                    bool statusMatch = _questStatusFilter == "All" || string.Equals(candidateStatus, _questStatusFilter, StringComparison.OrdinalIgnoreCase);
                    string candidateName = candidate.Name ?? string.Empty; string candidateId = GameApiQuest.GetQuestId(candidate);
                    string candidateText = GameApiQuest.GetQuestSearchText(candidate);
                    bool searchMatch = string.IsNullOrEmpty(_questFilter) || candidateName.IndexOf(_questFilter, StringComparison.OrdinalIgnoreCase) >= 0 || candidateId.IndexOf(_questFilter, StringComparison.OrdinalIgnoreCase) >= 0 || candidateText.IndexOf(_questFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                    if (statusMatch && searchMatch) questView.Add(candidate);
                }
                var allQuests = questView.ToArray();
                int maxQuestPage = Mathf.Max(0, (allQuests.Length - 1) / PageSize); if (_questPage > maxQuestPage) _questPage = maxQuestPage;
                AddLine(body.transform, "Showing: " + allQuests.Length + " / " + rawQuests.Length + " canonical quests    Page: " + (_questPage + 1));
                AddLine(body.transform, "Active log: " + (GameApi.Ready ? GameApiQuest.GetActiveQuestLog().Count.ToString() : "n/a"));
                string[] questAudit = GameApiQuest.AuditQuestIntegrity();
                for (int qai = 0; qai < questAudit.Length; qai++) AddLine(body.transform, questAudit[qai]);
                AddAction(body.transform, "Refresh quest database", delegate { GameApiQuest.MarkQuestCacheDirty(); RefreshModule(); });
                AddAction(body.transform, "Repair quest log", delegate { ConfirmAction("Repair quest log", delegate { GameApiQuest.RepairQuestLog(); }); });
                string[] missingQuestItems = GameApiQuest.GetMissingQuestItems(false); for (int mqi = 0; mqi < missingQuestItems.Length; mqi++) AddLine(body.transform, missingQuestItems[mqi]);
                AddAction(body.transform, "Repair missing active quest items", delegate { ConfirmAction("Repair missing quest items", delegate { GameApiQuest.GetMissingQuestItems(true); }); });
                AddLine(body.transform, "Search: " + (string.IsNullOrEmpty(_questFilter) ? "(none)" : _questFilter));
                AddAction(body.transform, "Search quests...", delegate { OpenPrompt("Quest search", _questFilter, delegate(string v) { _questFilter = v ?? string.Empty; _questPage = 0; _selectedQuest = null; }); });
                AddAction(body.transform, "Clear quest search", delegate { _questFilter = string.Empty; _questPage = 0; RefreshModule(); });
                AddAction(body.transform, "All", delegate { _questStatusFilter = "All"; _questPage = 0; _selectedQuest = null; RefreshModule(); });
                AddAction(body.transform, "Active", delegate { _questStatusFilter = "Active"; _questPage = 0; _selectedQuest = null; RefreshModule(); });
                AddAction(body.transform, "Inactive", delegate { _questStatusFilter = "Inactive"; _questPage = 0; _selectedQuest = null; RefreshModule(); });
                AddAction(body.transform, "Completed", delegate { _questStatusFilter = "Completed"; _questPage = 0; _selectedQuest = null; RefreshModule(); });
                AddAction(body.transform, "Previous page", delegate { if (_questPage > 0) _questPage--; RefreshModule(); });
                AddAction(body.transform, "Next page", delegate { if ((_questPage + 1) * PageSize < allQuests.Length) _questPage++; RefreshModule(); });
                var questColumns = new GameObject("QuestColumns", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
                questColumns.transform.SetParent(body.transform, false);
                var questHorizontal = questColumns.GetComponent<HorizontalLayoutGroup>();
                questHorizontal.spacing = 14; questHorizontal.childControlWidth = true; questHorizontal.childForceExpandWidth = true; questHorizontal.childControlHeight = true; questHorizontal.childForceExpandHeight = false;
                questColumns.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                Transform questList = CreateColumn(questColumns.transform, 0.40f, new Color(0.06f, 0.10f, 0.13f, 0.9f));
                Transform questInspector = CreateColumn(questColumns.transform, 0.60f, new Color(0.045f, 0.075f, 0.095f, 0.9f));
                if (_selectedQuest != null)
                {
                    AddHeading(questInspector, "Quest: " + _selectedQuest.Name);
                    AddLine(questInspector, "ID: " + GameApiQuest.GetQuestId(_selectedQuest));
                    AddLine(questInspector, GameApiQuest.GetQuestRuntimeSummary(_selectedQuest));
                    AddLine(questInspector, "Priority: " + _selectedQuest.Priority + " | Repeatable: " + _selectedQuest.Repeatable + " | Rewards: " + _selectedQuest.QuestRewards.Count);
                    AddAction(questInspector, "Collapse quest steps", delegate { _selectedQuest = null; RefreshModule(); });
                    var restartQuest = _selectedQuest;
                    if (GameApiQuest.HasFullRestartProfile(restartQuest)) AddAction(questInspector, "Full restart (profiled)", delegate { ConfirmAction("Full restart " + restartQuest.Name, delegate { GameApiQuest.RestartQuest(restartQuest); }); });
                    else AddLine(questInspector, "Full restart disabled: no verified repair profile. Soft Restart remains available per step.");
                    var selectedSteps = GameApiQuest.GetQuestSteps(_selectedQuest);
                    AddLine(questInspector, "Steps: " + selectedSteps.Length);
                    for (int si2 = 0; si2 < selectedSteps.Length; si2++)
                    {
                        var step = selectedSteps[si2]; if (step == null) continue;
                        AddLine(questInspector, (si2 + 1) + ". " + step.Description + " | " + step.StepType + " | " + step.StepTrigger);
                        AddLine(questInspector, GameApiQuest.GetQuestStepInspectorSummary(_selectedQuest, step));
                        var capturedStep = step; var stepQuest = _selectedQuest;
                        var jumpPlan = GameApiQuest.PlanSafeJump(stepQuest, capturedStep);
                        if (jumpPlan.Length > 0)
                        {
                            AddLine(questInspector, "Dependency plan: " + jumpPlan.Length + " ordered steps");
                            for (int dpi = 0; dpi < jumpPlan.Length && dpi < 8; dpi++) AddLine(questInspector, "  " + (dpi + 1) + ". " + jumpPlan[dpi]);
                            if (jumpPlan.Length > 8) AddLine(questInspector, "  ... and " + (jumpPlan.Length - 8) + " more");
                        }
                        AddAction(questInspector, "Soft restart step " + (si2 + 1), delegate { ConfirmAction("Soft restart quest step", delegate { GameApiQuest.SoftRestartStep(stepQuest, capturedStep); }); });
                        AddAction(questInspector, "Safe Jump to step " + (si2 + 1), delegate { GameApiQuest.SafeJumpTo(stepQuest, capturedStep); RefreshModule(); });
                        AddAction(questInspector, "Complete step " + (si2 + 1), delegate { ConfirmAction("Complete quest step", delegate { GameApiQuest.SetQuestStage(stepQuest, capturedStep); }); });
                    }
                }
                else
                {
                    AddHeading(questInspector, "Quest inspector");
                    AddLine(questInspector, "Select a quest from the list on the left.");
                }
                int qStart = _questPage * PageSize, qEnd = Mathf.Min(allQuests.Length, qStart + PageSize);
                for (int qi = qStart; qi < qEnd; qi++)
                {
                    var quest = allQuests[qi]; if (quest == null) continue;
                    AddLine(questList, quest.Name + " | " + GameApiQuest.GetQuestStatus(quest) + " | " + GameApiQuest.GetQuestId(quest));
                    var capturedQuest = quest;
                    AddAction(questList, "Inspect steps: " + quest.Name, delegate { _selectedQuest = capturedQuest; RefreshModule(); });
                    AddAction(questList, "Safe start: " + quest.Name, delegate { GameApiQuest.SafeJumpTo(capturedQuest); RefreshModule(); });
                    AddAction(questList, "Complete: " + quest.Name, delegate { ConfirmAction("Complete quest " + capturedQuest.Name, delegate { GameApiQuest.SetQuestStage(capturedQuest); }); });
                }
            }
            else if (_module == "World")
            {
                if (GameApi.Ready) AddLine(body.transform, "Date: " + TeamNimbus.CloudMeadow.Managers.GameManager.Status.CurrentDateTime + "    Weather: " + TeamNimbus.CloudMeadow.Managers.GameManager.Status.CurrentWeather);
                else AddLine(body.transform, "Game status is not loaded.");
                AddAction(body.transform, "Season: Spring", delegate { GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Spring); RefreshModule(); });
                AddAction(body.transform, "Season: Summer", delegate { GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Summer); RefreshModule(); });
                AddAction(body.transform, "Season: Autumn", delegate { GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Autumn); RefreshModule(); });
                AddAction(body.transform, "Season: Winter", delegate { GameApi.SetSeason(TeamNimbus.CloudMeadow.Season.Winter); RefreshModule(); });
                AddAction(body.transform, "Advance to end of day", delegate { GameApi.AdvanceToEndOfDay(); RefreshModule(); });
                AddAction(body.transform, "Weather: Clear", delegate { GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Clear); RefreshModule(); });
                AddAction(body.transform, "Weather: Rain", delegate { GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Rain); RefreshModule(); });
                AddAction(body.transform, "Weather: Storm", delegate { GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Storm); RefreshModule(); });
                AddAction(body.transform, "Weather: Blazing Heat", delegate { GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.BlazingHeat); RefreshModule(); });
                AddAction(body.transform, "Weather: Snow", delegate { GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Snow); RefreshModule(); });
                AddAction(body.transform, "Weather: Falling Leaves", delegate { GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Leafs); RefreshModule(); });
                AddAction(body.transform, "Weather: Haunting", delegate { GameApi.SetWeather(TeamNimbus.CloudMeadow.Weather.Haunting); RefreshModule(); });
                AddHeading(body.transform, "Weather forecast");
                string[] forecastLines = GameApi.GetWeatherForecastSummary(); for (int wfi = 0; wfi < forecastLines.Length; wfi++) AddLine(body.transform, forecastLines[wfi]);
                AddAction(body.transform, "Predict +1 day", delegate { GameApi.PredictWeather(1); RefreshModule(); });
                AddAction(body.transform, "Predict to 14 days", delegate { GameApi.PredictWeather(14); RefreshModule(); });
                AddAction(body.transform, "Time: 06:00", delegate { GameApi.SetTime(6, 0); RefreshModule(); });
                AddAction(body.transform, "Time: 12:00", delegate { GameApi.SetTime(12, 0); RefreshModule(); });
                AddAction(body.transform, "Time: 18:00", delegate { GameApi.SetTime(18, 0); RefreshModule(); });
                AddAction(body.transform, "Time: 23:00", delegate { GameApi.SetTime(23, 0); RefreshModule(); });
                AddAction(body.transform, "Set exact hour...", delegate { OpenPrompt("Hour (0-23)", "12", delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetTime(n, 0); }); });
                AddAction(body.transform, "Set day...", delegate { OpenPrompt("Day", "1", delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetDayAndTime(n, 8, 0); }); });
                AddAction(body.transform, "Set year...", delegate { OpenPrompt("Year", "1", delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetYear(n); }); });
                AddHeading(body.transform, "Migrations");
                AddLine(body.transform, GameApi.GetMigrationSummary());
                AddAction(body.transform, "Unlock migrations", delegate { GameApi.UnlockMigrations(); RefreshModule(); });
                AddAction(body.transform, "Lock migrations", delegate { ConfirmAction("Lock migrations", delegate { GameApi.LockMigrations(); }); });
                AddAction(body.transform, "Reroll migration seed", delegate { GameApi.RerollMigrations(); RefreshModule(); });
                AddAction(body.transform, "Sync dungeon discoveries", delegate { GameApi.SyncMigrationDungeonProgress(); RefreshModule(); });
                AddAction(body.transform, "Clear migration discoveries", delegate { ConfirmAction("Clear migration discoveries", delegate { GameApi.ClearMigrationDiscoveries(); }); });
                if (GameApi.Ready) AddSimpleObjectEditor(body.transform, TeamNimbus.CloudMeadow.Managers.GameManager.Status.MigrationSaveData, 24, "Migration persistent fields");
                AddHeading(body.transform, "Calendar events today");
                string[] calendarLines = GameApi.GetCalendarEventSummary();
                for (int cli = 0; cli < calendarLines.Length; cli++) AddLine(body.transform, calendarLines[cli]);
                AddHeading(body.transform, "Upcoming 14 days");
                string[] scheduleLines = GameApi.GetCalendarSchedule(14); for (int csi = 0; csi < scheduleLines.Length; csi++) AddLine(body.transform, scheduleLines[csi]);
                AddHeading(body.transform, "Location browser");
                AddAction(body.transform, _showSceneBrowser ? "▼ Hide scenes" : "▶ Show scenes", delegate { _showSceneBrowser = !_showSceneBrowser; RefreshModule(); });
                if (_showSceneBrowser)
                {
                    string[] scenes = GameApi.GetSceneNames();
                    for (int sci = 0; sci < scenes.Length; sci++) { string sceneName = scenes[sci]; AddAction(body.transform, sceneName, delegate { ConfirmAction("Load scene " + sceneName, delegate { GameApi.LoadSceneByName(sceneName); }); }); }
                }
            }
            else if (_module == "Dungeons")
            {
                AddLine(body.transform, GameApi.Ready ? GameApi.GetDungeonSummary() : "Game status is not loaded.");
                AddAction(body.transform, "Set Savannah floor...", delegate { OpenPrompt("Savannah floor", "1", delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetDungeonFloor("Savannah", n); }); });
                AddAction(body.transform, "Set Forest floor...", delegate { OpenPrompt("Forest floor", "1", delegate(string v) { int n; if (int.TryParse(v, out n)) GameApi.SetDungeonFloor("Forest", n); }); });
                AddAction(body.transform, "Unlock Savannah floors + fast travel", delegate { GameApi.UnlockDungeonProgress("Savannah"); RefreshModule(); });
                AddAction(body.transform, "Unlock Forest floors + fast travel", delegate { GameApi.UnlockDungeonProgress("Forest"); RefreshModule(); });
                if (GameApi.Ready)
                {
                    AddSimpleObjectEditor(body.transform, TeamNimbus.CloudMeadow.Managers.GameManager.Status.SavannahPersistentDungeonData, 30, "Savannah persistent state");
                    AddSimpleObjectEditor(body.transform, TeamNimbus.CloudMeadow.Managers.GameManager.Status.ForestPersistentDungeonData, 30, "Forest persistent state");
                }
            }
            else if (_module == "Combat")
            {
                AddAction(body.transform, "Toggle God Mode", delegate { GameApi.ToggleGodMode(); });
                AddAction(body.transform, "Win current combat", delegate { GameApi.WinCombat(); });
                AddAction(body.transform, "Upgrade all party abilities", delegate { GameApi.UpgradeAllAbilitiesForParty(); RefreshModule(); });
                AddAction(body.transform, "Restart combat scene", delegate { ConfirmAction("Restart current combat scene", delegate { GameApi.RestartCurrentScene(); }); });
                var combatUnits = GameApi.GetCombatUnits();
                AddHeading(body.transform, "Combat units: " + combatUnits.Length);
                for (int cui = 0; cui < combatUnits.Length; cui++)
                {
                    var unit = combatUnits[cui]; if (unit == null) continue;
                    AddLine(body.transform, unit.DisplayName + " | " + (unit.IsEnemy ? "Enemy" : "Ally") + " | HP " + unit.GetCurrentHP().ToString("0") + "/" + unit.GetMaxHP().ToString("0") + " | Statuses " + unit.ActiveCombatStatuses.Count);
                    string[] unitDetails = GameApi.GetCombatUnitDetails(unit); for (int udi = 0; udi < unitDetails.Length; udi++) AddLine(body.transform, unitDetails[udi]);
                    var capturedUnit = unit;
                    AddAction(body.transform, "Heal " + unit.DisplayName, delegate { GameApi.HealCombatUnit(capturedUnit); RefreshModule(); });
                    AddAction(body.transform, "Clear statuses: " + unit.DisplayName, delegate { GameApi.ClearCombatStatuses(capturedUnit); RefreshModule(); });
                    AddAction(body.transform, "Reset cooldowns: " + unit.DisplayName, delegate { GameApi.ClearCombatCooldowns(capturedUnit); RefreshModule(); });
                    if (unit.IsEnemy) AddAction(body.transform, "Remove enemy: " + unit.DisplayName, delegate { GameApi.KillCombatUnit(capturedUnit); RefreshModule(); });
                }
            }
            else if (_module == "Errors")
            {
                string[] errors = LogBuffer.ErrorSnapshot();
                AddLine(body.transform, "Captured errors: " + errors.Length + " (latest 50 retained)");
                AddAction(body.transform, "Export diagnostics", delegate { LogBuffer.ExportErrors(System.IO.Path.Combine(BepInEx.Paths.GameRootPath, "BepInEx\\plugins\\CloudMeadowCreativeMode\\error_report.txt")); RefreshModule(); });
                AddAction(body.transform, "Copy errors", delegate { GUIUtility.systemCopyBuffer = string.Join("\n\n", errors); });
                AddAction(body.transform, "Clear errors", delegate { LogBuffer.ClearErrors(); RefreshModule(); });
                AddAction(body.transform, "Refresh", RefreshModule);
                int errorStart = Mathf.Max(0, errors.Length - 20);
                for (int eri = errors.Length - 1; eri >= errorStart; eri--) AddLine(body.transform, errors[eri]);
            }
            else if (_module == "Cheats")
            {
                AddHeading(body.transform, "Presets");
                AddAction(body.transform, "Creative starter preset", delegate { ConfirmAction("Apply Creative starter preset", delegate { GameApi.AddKorona(100000); GameApi.AddShards(100); GameApi.LevelAll(30); GameApi.MaxAllMonstersLoyalty(); GameApi.UnlockAllGallery(); GameApi.SetAllInventoryEntriesMaxQuality(); }); });
                AddAction(body.transform, "Exploration preset", delegate { GameApi.SetSpeedMultiplier(5f); if (!GameApi.NoClipEnabled) GameApi.ToggleNoClip(); RefreshModule(); });
                AddAction(body.transform, "Normal movement preset", delegate { GameApi.SetSpeedMultiplier(1f); if (GameApi.NoClipEnabled) GameApi.ToggleNoClip(); RefreshModule(); });
                AddHeading(body.transform, "Transaction / Undo");
                AddAction(body.transform, "Undo last field transaction", delegate { TransactionManager.UndoLast(); RefreshModule(); });
                string[] txHistory = TransactionManager.History();
                for (int thi = 0; thi < txHistory.Length && thi < 8; thi++) AddLine(body.transform, txHistory[thi]);
                AddHeading(body.transform, "Exploration");
                AddLine(body.transform, "Map movement tools. These settings apply outside combat while exploring locations.");
                AddAction(body.transform, GameApi.NoClipEnabled ? "No Clip: ON" : "No Clip: OFF", delegate { GameApi.ToggleNoClip(); RefreshModule(); });
                AddAction(body.transform, "Speed x1", delegate { GameApi.SetSpeedMultiplier(1f); RefreshModule(); });
                AddAction(body.transform, "Speed x2", delegate { GameApi.SetSpeedMultiplier(2f); RefreshModule(); });
                AddAction(body.transform, "Speed x5", delegate { GameApi.SetSpeedMultiplier(5f); RefreshModule(); });
                AddAction(body.transform, "Speed x10", delegate { GameApi.SetSpeedMultiplier(10f); RefreshModule(); });
                AddHeading(body.transform, "Quick resources");
                AddAction(body.transform, "Add 1,000 Korona", delegate { GameApi.AddKorona(1000); RefreshModule(); });
                AddAction(body.transform, "Add 100,000 Korona", delegate { GameApi.AddKorona(100000); RefreshModule(); });
                AddAction(body.transform, "Add 10 shards", delegate { GameApi.AddShards(10); RefreshModule(); });
                AddAction(body.transform, "Heal protagonist", delegate { GameApi.HealProtagonist(); RefreshModule(); });
                AddAction(body.transform, "Max monster loyalty", delegate { GameApi.MaxAllMonstersLoyalty(); RefreshModule(); });
                AddAction(body.transform, "Unlock gallery", delegate { GameApi.UnlockAllGallery(); RefreshModule(); });
            }
            else if (_module == "Gallery")
            {
                if (GameApi.Ready)
                {
                    var galleryStatus = TeamNimbus.CloudMeadow.Managers.GameManager.Status;
                    AddHeading(body.transform, "Content settings");
                    string[] contentFields = { "AdultContent", "HeteroScenes", "HomoScenes", "IntersexScenes", "GuyGuyScenes", "GirlGirlScenes" };
                    for (int cfi = 0; cfi < contentFields.Length; cfi++)
                    {
                        string fieldName = contentFields[cfi]; var field = galleryStatus.GetType().GetField(fieldName); bool current = field != null && (bool)field.GetValue(galleryStatus);
                        AddAction(body.transform, fieldName + ": " + (current ? "ON" : "OFF"), delegate { GameApi.SetMemberFromString(galleryStatus, fieldName, (!current).ToString()); RefreshModule(); });
                    }
                }
                var rawScenes = GameApi.GetGalleryScenes();
                var sceneView = new System.Collections.Generic.List<TeamNimbus.CloudMeadow.UI.SexSceneData>();
                for (int gsi = 0; gsi < rawScenes.Length; gsi++)
                {
                    string sceneName = GameApi.GetGallerySceneName(rawScenes[gsi]);
                    if (string.IsNullOrEmpty(_galleryFilter) || sceneName.IndexOf(_galleryFilter, StringComparison.OrdinalIgnoreCase) >= 0) sceneView.Add(rawScenes[gsi]);
                }
                AddLine(body.transform, "Gallery entries: " + sceneView.Count + " / " + rawScenes.Length + "    Page: " + (_galleryPage + 1));
                AddAction(body.transform, "Unlock all gallery entries", delegate { GameApi.UnlockAllGallery(); RefreshModule(); });
                AddAction(body.transform, "Search scenes...", delegate { OpenPrompt("Gallery search", _galleryFilter, delegate(string v) { _galleryFilter = v ?? string.Empty; _galleryPage = 0; }); });
                AddAction(body.transform, "Clear search", delegate { _galleryFilter = string.Empty; _galleryPage = 0; RefreshModule(); });
                AddAction(body.transform, "Previous page", delegate { if (_galleryPage > 0) _galleryPage--; RefreshModule(); });
                AddAction(body.transform, "Next page", delegate { if ((_galleryPage + 1) * PageSize < sceneView.Count) _galleryPage++; RefreshModule(); });
                int gsStart = _galleryPage * PageSize, gsEnd = Mathf.Min(sceneView.Count, gsStart + PageSize);
                for (int gsi = gsStart; gsi < gsEnd; gsi++)
                {
                    var scene = sceneView[gsi]; bool unlocked = GameApi.IsGallerySceneUnlocked(scene);
                    AddLine(body.transform, GameApi.GetGallerySceneName(scene) + " | " + scene.Pairing + " | " + (scene.IsSceneHD ? "HD" : "SD") + " | " + (unlocked ? "UNLOCKED" : "LOCKED"));
                    AddLine(body.transform, GameApi.GetGallerySceneDiagnostics(scene));
                    var capturedScene = scene;
                    if (!unlocked) AddAction(body.transform, "Unlock: " + GameApi.GetGallerySceneName(scene), delegate { GameApi.UnlockGalleryScene(capturedScene); RefreshModule(); });
                    else AddAction(body.transform, "Lock: " + GameApi.GetGallerySceneName(scene), delegate { ConfirmAction("Lock gallery scene", delegate { GameApi.LockGalleryScene(capturedScene); }); });
                    AddAction(body.transform, "Preview: " + GameApi.GetGallerySceneName(scene), delegate { GameApi.PreviewGalleryScene(capturedScene); SetVisible(false); });
                }
            }
            else if (_module == "Relationships")
            {
                AddLine(body.transform, "Story continuity and relationship flags discovered from the current Game.dll runtime.");
                if (GameApi.Ready) AddFilteredObjectEditor(body.transform, TeamNimbus.CloudMeadow.Managers.GameManager.Status.Flags, 120, "Relationship / continuity flags", new[] { "fio", "yonten", "romance", "relationship", "continuity", "match", "date", "kidnap", "rook", "ev" });
            }
        }

        private void BreakActionRow(Transform p) { _actionRows.Remove(p); _actionRowCounts.Remove(p); }
        private void AddHeading(Transform p, string text) { BreakActionRow(p); var t = Label(text, p, 27, TextAnchor.MiddleLeft); t.gameObject.AddComponent<LayoutElement>().preferredHeight = 44; }
        private void AddLine(Transform p, string text) { BreakActionRow(p); var card = Panel("InfoCard", p, new Color(0.07f, 0.105f, 0.13f, 0.88f)); card.gameObject.AddComponent<LayoutElement>().preferredHeight = 38; var t = Label(text, card, 16, TextAnchor.MiddleLeft); SetRect(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-10, 0)); }
        private void AddAction(Transform p, string text, UnityEngine.Events.UnityAction action)
        {
            Transform row; int count;
            int maxPerRow = p.name == "Column" ? 2 : 3;
            if (!_actionRows.TryGetValue(p, out row) || row == null || !_actionRowCounts.TryGetValue(p, out count) || count >= maxPerRow)
            {
                var rowObject = new GameObject("ActionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                rowObject.transform.SetParent(p, false); row = rowObject.transform;
                var rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>(); rowLayout.spacing = 8; rowLayout.childControlWidth = true; rowLayout.childForceExpandWidth = true; rowLayout.childControlHeight = true; rowLayout.childForceExpandHeight = true;
                rowObject.GetComponent<LayoutElement>().preferredHeight = 44;
                _actionRows[p] = row; _actionRowCounts[p] = 0; count = 0;
            }
            var b = MakeButton(text, row, action); var element = b.GetComponent<LayoutElement>(); element.preferredHeight = 44; element.flexibleWidth = 1; element.minWidth = 120;
            _actionRowCounts[p] = count + 1;
        }
        private Transform CreateColumn(Transform parent, float flexibleWidth, Color color)
        {
            var panel = Panel("Column", parent, color);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10); layout.spacing = 8; layout.childControlHeight = true; layout.childForceExpandHeight = false; layout.childControlWidth = true; layout.childForceExpandWidth = true;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var element = panel.gameObject.AddComponent<LayoutElement>(); element.flexibleWidth = flexibleWidth; element.minWidth = 220;
            return panel.transform;
        }
        private static bool IsSimpleEditorType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsEnum || type == typeof(string) || type == typeof(bool) || type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }
        private void AddSimpleObjectEditor(Transform parent, object target, int maxMembers, string heading)
        {
            if (target == null) return; AddHeading(parent, heading); int shown = 0;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var fields = target.GetType().GetFields(flags);
            for (int i = 0; i < fields.Length && shown < maxMembers; i++)
            {
                var field = fields[i]; if (field.IsInitOnly || !IsSimpleEditorType(field.FieldType)) continue; object value = null; try { value = field.GetValue(target); } catch { }
                string name = field.Name, initial = value != null ? value.ToString() : string.Empty; object capturedTarget = target;
                AddAction(parent, name + " = " + initial, delegate { OpenPrompt(name, initial, delegate(string v) { GameApi.SetMemberFromString(capturedTarget, name, v); }); }); shown++;
            }
            var props = target.GetType().GetProperties(flags);
            for (int i = 0; i < props.Length && shown < maxMembers; i++)
            {
                var prop = props[i]; if (!prop.CanRead || !prop.CanWrite || prop.GetIndexParameters().Length != 0 || !IsSimpleEditorType(prop.PropertyType)) continue; object value = null; try { value = prop.GetValue(target, null); } catch { }
                string name = prop.Name, initial = value != null ? value.ToString() : string.Empty; object capturedTarget = target;
                AddAction(parent, name + " = " + initial, delegate { OpenPrompt(name, initial, delegate(string v) { GameApi.SetMemberFromString(capturedTarget, name, v); }); }); shown++;
            }
            if (shown == 0) AddLine(parent, "No safely editable scalar fields detected.");
        }
        private void AddFilteredObjectEditor(Transform parent, object target, int maxMembers, string heading, string[] tokens)
        {
            if (target == null) return; AddHeading(parent, heading); int shown = 0; var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var fields = target.GetType().GetFields(flags);
            for (int i = 0; i < fields.Length && shown < maxMembers; i++)
            {
                var field = fields[i]; if (field.IsInitOnly || !IsSimpleEditorType(field.FieldType) || !NameMatches(field.Name, tokens)) continue; object value = null; try { value = field.GetValue(target); } catch { }
                string name = field.Name, initial = value != null ? value.ToString() : string.Empty; object captured = target; AddAction(parent, name + " = " + initial, delegate { OpenPrompt(name, initial, delegate(string v) { GameApi.SetMemberFromString(captured, name, v); }); }); shown++;
            }
            if (shown == 0) AddLine(parent, "No matching runtime flags found in this game version.");
        }
        private static bool NameMatches(string name, string[] tokens) { for (int i = 0; i < tokens.Length; i++) if (name.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0) return true; return false; }
        private void OpenPrompt(string title, string initial, Action<string> apply)
        {
            _promptTitle = title; _promptValue = initial ?? string.Empty; _promptApply = apply; _promptOpen = true;
            if (_promptLayer != null) _promptLayer.SetActive(true);
            if (_promptHeading != null) _promptHeading.text = title;
            if (_promptInput != null) { _promptInput.text = _promptValue; _promptInput.Select(); _promptInput.ActivateInputField(); }
        }
        private void ApplyPrompt()
        {
            var apply = _promptApply; string value = _promptInput != null ? _promptInput.text : _promptValue;
            ClosePrompt(); if (apply != null) apply(value); RefreshModule();
        }
        private void ClosePrompt() { _promptOpen = false; _promptApply = null; if (_promptLayer != null) _promptLayer.SetActive(false); }
        private void BuildPrompt(Transform root)
        {
            _promptLayer = new GameObject("PromptLayer", typeof(RectTransform), typeof(Image)); _promptLayer.transform.SetParent(root, false);
            SetRect(_promptLayer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _promptLayer.GetComponent<Image>().color = new Color(0, 0, 0, 0.68f);
            var modal = Panel("PromptCard", _promptLayer.transform, new Color(0.075f, 0.11f, 0.14f, 1f));
            SetRect(modal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-310, -120), new Vector2(310, 120));
            _promptHeading = Label("Input", modal, 24, TextAnchor.MiddleLeft); SetRect(_promptHeading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(24, -62), new Vector2(-24, -12));
            var inputObject = new GameObject("PromptInput", typeof(RectTransform), typeof(Image), typeof(InputField)); inputObject.transform.SetParent(modal, false);
            SetRect(inputObject.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(24, -20), new Vector2(-24, 28));
            inputObject.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.07f, 1f);
            var inputText = Label(string.Empty, inputObject.transform, 20, TextAnchor.MiddleLeft); SetRect(inputText.rectTransform, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4));
            _promptInput = inputObject.GetComponent<InputField>(); _promptInput.textComponent = inputText; _promptInput.lineType = InputField.LineType.SingleLine;
            _promptInput.onEndEdit.AddListener(delegate(string value) { if (_promptOpen && (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))) ApplyPrompt(); });
            var apply = MakeButton("Apply", modal, ApplyPrompt); SetRect(apply.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(24, 18), new Vector2(-6, 62));
            var cancel = MakeButton("Cancel", modal, ClosePrompt); SetRect(cancel.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(6, 18), new Vector2(-24, 62));
            _promptLayer.SetActive(false);
        }
        private void ConfirmAction(string title, UnityEngine.Events.UnityAction action)
        { OpenPrompt(title + " — type YES", string.Empty, delegate(string v) { if (string.Equals(v, "YES", StringComparison.OrdinalIgnoreCase) && action != null) { GameApi.BackupAllSaves(); action(); } }); }
        private void ConfirmWithoutBackup(string title, UnityEngine.Events.UnityAction action)
        { OpenPrompt(title + " — type YES", string.Empty, delegate(string v) { if (string.Equals(v, "YES", StringComparison.OrdinalIgnoreCase) && action != null) action(); }); }
        private void AddTraitAction(Transform p, string name, string description, string quality, UnityEngine.Events.UnityAction action)
        {
            string shortDescription = description ?? string.Empty;
            if (shortDescription.Length > 105) shortDescription = shortDescription.Substring(0, 102) + "...";
            var b = MakeButton(name + "  [" + quality + "]\n" + shortDescription, p, action);
            Color color = new Color(0.28f, 0.32f, 0.35f, 1f);
            if (quality == "Uncommon") color = new Color(0.12f, 0.38f, 0.22f, 1f);
            else if (quality == "Rare") color = new Color(0.55f, 0.38f, 0.08f, 1f);
            else if (quality == "Negative") color = new Color(0.48f, 0.15f, 0.16f, 1f);
            b.GetComponent<Image>().color = color;
        }

        private static RectTransform Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color; return go.GetComponent<RectTransform>();
        }
        private static Text Label(string value, Transform parent, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false); var t = go.GetComponent<Text>();
            t.text = value; t.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); t.fontSize = size; t.color = new Color(0.92f, 0.94f, 0.94f); t.alignment = anchor; return t;
        }
        private Button MakeButton(string text, Transform parent, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement)); go.transform.SetParent(parent, false);
            Color baseColor = new Color(0.14f, 0.25f, 0.29f, 1f);
            string lower = text.ToLowerInvariant();
            if (lower.Contains("remove") || lower.Contains("delete") || lower.Contains("clear barn")) baseColor = new Color(0.42f, 0.16f, 0.17f, 1f);
            else if (lower.Contains("add ") || lower.Contains("recruit") || lower.Contains("unlock")) baseColor = new Color(0.12f, 0.34f, 0.25f, 1f);
            else if (lower.Contains("inspect") || lower.Contains("safe") || lower.Contains("refresh")) baseColor = new Color(0.12f, 0.28f, 0.43f, 1f);
            else if (lower.Contains("complete") || lower.Contains("win current")) baseColor = new Color(0.43f, 0.31f, 0.11f, 1f);
            go.GetComponent<Image>().color = baseColor; var b = go.GetComponent<Button>();
            b.onClick.AddListener(delegate {
                bool transactional = parent != null && (parent.name == "ActionRow" || parent.name == "TraitGrid" || parent.name == "PromptCard") && !IsUiOnlyAction(text);
                if (!transactional || text.StartsWith("Undo", StringComparison.OrdinalIgnoreCase)) { if (action != null) action(); return; }
                TransactionManager.Begin(text); try { if (action != null) action(); } finally { TransactionManager.Commit(text); }
            });
            var colors = b.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f); colors.pressedColor = new Color(0.72f, 0.78f, 0.80f, 1f); b.colors = colors;
            var t = Label(text, go.transform, 18, TextAnchor.MiddleCenter); t.resizeTextForBestFit = true; t.resizeTextMinSize = 12; t.resizeTextMaxSize = 18;
            SetRect(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 3), new Vector2(-8, -3)); _manualButtons.Add(b); return b;
        }
        private static bool IsUiOnlyAction(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            string value = text.ToLowerInvariant();
            string[] exact = { "overview", "player", "party", "monsters", "inventory", "eggs", "farm", "quests", "world", "dungeons", "combat", "cheats", "gallery", "relationships", "advanced", "diagnostics", "errors", "all", "active", "inactive", "completed", "close" };
            for (int i = 0; i < exact.Length; i++) if (value == exact[i]) return true;
            string[] prefixes = { "▶", "▼", "previous ", "next ", "search ", "clear search", "select #", "✓ selected #", "batch select:", "✓ batch:", "inspect:", "collapse ", "refresh ", "copy error", "export " };
            for (int i = 0; i < prefixes.Length; i++) if (value.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
        private static void SetRect(RectTransform r, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax) { r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = omin; r.offsetMax = omax; }
    }
}

