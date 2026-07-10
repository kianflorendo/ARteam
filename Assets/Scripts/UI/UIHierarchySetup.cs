using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteInEditMode]
public class UIHierarchySetup : MonoBehaviour
{
    [Header("Full Generate (deletes existing UI_Canvas first)")]
    [Tooltip("Tick to generate the full UI hierarchy from scratch.")]
    public bool generateHierarchy = false;

    [Header("Partial Rebuild — safe to use on existing prefab")]
    [Tooltip("Tick to destroy and rebuild just SoldierScreen in place.")]
    public bool rebuildSoldierScreen  = false;
    [Tooltip("Tick to destroy and rebuild just SettingsScreen in place.")]
    public bool rebuildSettingsScreen = false;
    [Tooltip("Tick to destroy and rebuild just ProfileScreen in place.")]
    public bool rebuildProfileScreen  = false;
    [Tooltip("Tick to destroy and rebuild just HomeScreen in place.")]
    public bool rebuildHomeScreen       = false;
    [Tooltip("Tick to destroy and rebuild just MainMenuScreen in place.")]
    public bool rebuildMainMenuScreen   = false;
    [Tooltip("Tick to destroy and rebuild just HowToPlayScreen in place.")]
    public bool rebuildHowToPlayScreen  = false;
    [Tooltip("Tick to destroy and rebuild just RegisterScreen in place.")]
    public bool rebuildRegisterScreen   = false;
    [Tooltip("Tick to destroy and rebuild just AwardsScreen in place.")]
    public bool rebuildAwardsScreen     = false;
    [Tooltip("Tick to destroy and rebuild just ARScanScreen in place.")]
    public bool rebuildARScanScreen     = false;
    [Tooltip("Tick to destroy and rebuild BottomNavBar in place.")]
    public bool rebuildBottomNavBar     = false;
    [Tooltip("Tick to destroy and rebuild AR_DebugPanel, AR_ActionPanel, and AR_CollectBanner in place. " +
             "After rebuild, wire the new GameObjects to NavigationManager in the Inspector.")]
    public bool rebuildAROverlayPanels  = false;

    private static readonly Color C_BLACK     = new Color(0.102f, 0.102f, 0.102f);
    private static readonly Color C_GRAY_88   = new Color(0.533f, 0.533f, 0.533f);
    private static readonly Color C_GRAY_AA   = new Color(0.667f, 0.667f, 0.667f);
    private static readonly Color C_GRAY_CC   = new Color(0.800f, 0.800f, 0.800f);
    private static readonly Color C_GRAY_D9   = new Color(0.851f, 0.851f, 0.851f);
    private static readonly Color C_GRAY_E8   = new Color(0.910f, 0.910f, 0.910f);
    private static readonly Color C_GRAY_F8   = new Color(0.973f, 0.973f, 0.973f);
    private static readonly Color C_WHITE     = Color.white;
    private static readonly Color C_CLEAR     = Color.clear;

    private void Update()
    {
        if (rebuildSoldierScreen)
        {
            rebuildSoldierScreen = false;
            DoRebuildSoldierScreen();
            return;
        }

        if (rebuildSettingsScreen)
        {
            rebuildSettingsScreen = false;
            DoRebuildScreen("SettingsScreen", BuildSettingsScreen, 5);
            return;
        }

        if (rebuildProfileScreen)
        {
            rebuildProfileScreen = false;
            DoRebuildScreen("ProfileScreen", BuildProfileScreen, 4);
            return;
        }

        if (rebuildHomeScreen)
        {
            rebuildHomeScreen = false;
            DoRebuildScreen("HomeScreen", BuildHomeScreen, 0);
            return;
        }

        if (rebuildMainMenuScreen)
        {
            rebuildMainMenuScreen = false;
            DoRebuildPreLoginScreen("MainMenuScreen", BuildMainMenuScreen, 0);
            return;
        }

        if (rebuildHowToPlayScreen)
        {
            rebuildHowToPlayScreen = false;
            DoRebuildPreLoginScreen("HowToPlayScreen", BuildHowToPlayScreen, 2);
            return;
        }

        if (rebuildRegisterScreen)
        {
            rebuildRegisterScreen = false;
            DoRebuildPreLoginScreen("RegisterScreen", BuildRegisterScreen, 1);
            return;
        }

        if (rebuildAwardsScreen)
        {
            rebuildAwardsScreen = false;
            DoRebuildScreen("AwardsScreen", BuildAwardsScreen, 3);
            return;
        }

        if (rebuildARScanScreen)
        {
            rebuildARScanScreen = false;
            DoRebuildScreen("ARScanScreen", BuildARScanScreen, 2);
            return;
        }

        if (rebuildBottomNavBar)
        {
            rebuildBottomNavBar = false;
            var canvas  = transform.Find("UI_Canvas");
            if (canvas == null) return;
            var mainApp = canvas.Find("MainAppGroup");
            if (mainApp == null) return;
            var existing = mainApp.Find("BottomNavBar");
            if (existing != null) DestroyImmediate(existing.gameObject);
            BuildBottomNavBar(mainApp);
            return;
        }

        if (rebuildAROverlayPanels)
        {
            rebuildAROverlayPanels = false;
            var canvas = transform.Find("UI_Canvas");
            if (canvas == null) { Debug.LogError("[UIHierarchySetup] UI_Canvas not found."); return; }
            var dp = canvas.Find("AR_DebugPanel");    if (dp != null) DestroyImmediate(dp.gameObject);
            var ap = canvas.Find("AR_ActionPanel");   if (ap != null) DestroyImmediate(ap.gameObject);
            var cb = canvas.Find("AR_CollectBanner"); if (cb != null) DestroyImmediate(cb.gameObject);
            BuildDebugPanel(canvas);
            BuildARActionPanel(canvas);
            BuildARCollectBanner(canvas);
            Debug.Log("[UIHierarchySetup] AR overlay panels rebuilt. Wire AR_ActionPanel and AR_CollectBanner to NavigationManager in the Inspector.");
            return;
        }

        if (!generateHierarchy) return;
        generateHierarchy = false;

        if (transform.Find("UI_Canvas") != null)
        {
            Debug.LogWarning("[UIHierarchySetup] UI_Canvas already exists. " +
                             "Delete it first, then tick Generate Hierarchy again. " +
                             "To edit visually, open Assets/Prefabs/UI/UI_Canvas.prefab.");
            return;
        }

        Generate();
    }

    private void DoRebuildSoldierScreen()
    {
        var canvas = transform.Find("UI_Canvas");
        if (canvas == null) { Debug.LogError("[UIHierarchySetup] UI_Canvas not found on UIGenerator."); return; }

        var screens = canvas.Find("MainAppGroup/Screens");
        if (screens == null) { Debug.LogError("[UIHierarchySetup] MainAppGroup/Screens not found."); return; }

        // Remember sibling index so SoldierScreen stays between HomeScreen and ARScanScreen
        int siblingIndex = 1;
        var existing = screens.Find("SoldierScreen");
        if (existing != null)
        {
            siblingIndex = existing.GetSiblingIndex();
            DestroyImmediate(existing.gameObject);
        }

        BuildSoldierScreen(screens);

        var rebuilt = screens.Find("SoldierScreen");
        if (rebuilt != null) rebuilt.SetSiblingIndex(siblingIndex);

        Debug.Log("[UIHierarchySetup] ✅ SoldierScreen rebuilt. " +
                  "Select UI_Canvas → Inspector → Overrides → Apply All to save to prefab.");
    }

    private void DoRebuildScreen(string screenName, System.Action<Transform> builder, int defaultSiblingIndex)
    {
        var canvas = transform.Find("UI_Canvas");
        if (canvas == null) { Debug.LogError("[UIHierarchySetup] UI_Canvas not found."); return; }

        var screens = canvas.Find("MainAppGroup/Screens");
        if (screens == null) { Debug.LogError("[UIHierarchySetup] MainAppGroup/Screens not found."); return; }

        int sibling = defaultSiblingIndex;
        var existing = screens.Find(screenName);
        if (existing != null)
        {
            sibling = existing.GetSiblingIndex();
            DestroyImmediate(existing.gameObject);
        }

        builder(screens);

        var rebuilt = screens.Find(screenName);
        if (rebuilt != null) rebuilt.SetSiblingIndex(sibling);

        Debug.Log($"[UIHierarchySetup] ✅ {screenName} rebuilt. " +
                  "Select UI_Canvas → Overrides → Apply All to save to prefab.");
    }

    private void DoRebuildPreLoginScreen(string screenName, System.Action<Transform> builder, int defaultSiblingIndex)
    {
        var canvas = transform.Find("UI_Canvas");
        if (canvas == null) { Debug.LogError("[UIHierarchySetup] UI_Canvas not found."); return; }

        var preLogin = canvas.Find("PreLoginGroup");
        if (preLogin == null) { Debug.LogError("[UIHierarchySetup] PreLoginGroup not found."); return; }

        int sibling = defaultSiblingIndex;
        var existing = preLogin.Find(screenName);
        if (existing != null)
        {
            sibling = existing.GetSiblingIndex();
            DestroyImmediate(existing.gameObject);
        }

        builder(preLogin);

        var rebuilt = preLogin.Find(screenName);
        if (rebuilt != null) rebuilt.SetSiblingIndex(sibling);

        Debug.Log($"[UIHierarchySetup] ✅ {screenName} rebuilt. " +
                  "Select UI_Canvas → Overrides → Apply All to save to prefab.");
    }

    public void Generate()
    {
        Debug.Log("[UIHierarchySetup] Generating Figma UI hierarchy...");

        EnsureComponent<NavigationManager>(gameObject);
        EnsureComponent<ActiveSoldierManager>(gameObject);
        EnsureComponent<PlayerProfileManager>(gameObject);

        var canvas = BuildCanvas();

        var preLogin = MakeGroup("PreLoginGroup", canvas.transform, C_CLEAR);
        BuildMainMenuScreen(preLogin.transform);
        BuildRegisterScreen(preLogin.transform);
        BuildHowToPlayScreen(preLogin.transform);

        var mainApp = MakeGroup("MainAppGroup", canvas.transform, C_CLEAR);
        mainApp.SetActive(false);

        var screens = MakeGroup("Screens", mainApp.transform, C_CLEAR);
        SetFullScreen(screens);

        BuildHomeScreen(screens.transform);
        BuildSoldierScreen(screens.transform);
        BuildARScanScreen(screens.transform);
        BuildAwardsScreen(screens.transform);
        try { BuildProfileScreen(screens.transform); }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIHierarchySetup] BuildProfileScreen failed: {e.Message} — profile may be incomplete.");
        }

        BuildSettingsScreen(screens.transform);

        // TopBar and BottomNavBar must render ON TOP of screens — add them last (later sibling = rendered on top)
        BuildTopBar(mainApp.transform);
        BuildBottomNavBar(mainApp.transform);

        BuildDebugPanel(canvas.transform);
        BuildARActionPanel(canvas.transform);
        BuildARCollectBanner(canvas.transform);

        EnsureEventSystem();

        Debug.Log("[UIHierarchySetup] ✅ Done! " +
                  "Now drag UI_Canvas → Assets/Prefabs/UI/UI_Canvas.prefab then save the scene.");
    }

    private Canvas BuildCanvas()
    {
        var go     = new GameObject("UI_Canvas");
        go.transform.SetParent(transform, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390, 844);
        scaler.matchWidthOrHeight  = 1f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void BuildTopBar(Transform parent)
    {
        var bar = MakeRect("TopBar", parent);
        Anchor(bar, 0, 1, 1, 1);
        SetPivot(bar, 0.5f, 1f);
        bar.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 65);

        var bg = bar.AddComponent<Image>();
        bg.color = C_WHITE;

        var div = MakeRect("Divider", bar.transform);
        Anchor(div, 0, 0, 1, 0);
        SetPivot(div, 0.5f, 0f);
        div.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 2);
        div.AddComponent<Image>().color = C_GRAY_D9;

        var avatar = MakeRect("AvatarCircle", bar.transform);
        Anchor(avatar, 0, 0.5f, 0, 0.5f);
        SetPivot(avatar, 0f, 0.5f);
        var avatarRT = avatar.GetComponent<RectTransform>();
        avatarRT.sizeDelta        = new Vector2(35, 35);
        avatarRT.anchoredPosition = new Vector2(20, 0);
        avatar.AddComponent<Image>().color = C_GRAY_E8;

        var userLbl = MakeRect("UsernameLabel", bar.transform);
        Anchor(userLbl, 0, 0.5f, 0, 0.5f);
        SetPivot(userLbl, 0f, 0.5f);
        var userRT = userLbl.GetComponent<RectTransform>();
        userRT.sizeDelta        = new Vector2(160, 22);
        userRT.anchoredPosition = new Vector2(62, 0);
        var userTmp = userLbl.AddComponent<TextMeshProUGUI>();
        userTmp.text      = "USERNAME";
        userTmp.fontSize  = 14;
        userTmp.fontStyle = FontStyles.Bold;
        userTmp.color     = C_BLACK;
        userTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var xpPill = MakeRect("XPPill", bar.transform);
        Anchor(xpPill, 1, 0.5f, 1, 0.5f);
        SetPivot(xpPill, 1f, 0.5f);
        var xpRT = xpPill.GetComponent<RectTransform>();
        xpRT.sizeDelta        = new Vector2(68, 22);
        xpRT.anchoredPosition = new Vector2(-20, 0);
        var xpBg = xpPill.AddComponent<Image>();
        xpBg.color = C_GRAY_F8;

        var xpLbl = MakeRect("XPLabel", xpPill.transform);
        SetFullScreen(xpLbl);
        var xpTmp = xpLbl.AddComponent<TextMeshProUGUI>();
        xpTmp.text      = "0 XP";
        xpTmp.fontSize  = 13;
        xpTmp.fontStyle = FontStyles.Bold;
        xpTmp.color     = C_GRAY_88;
        xpTmp.alignment = TextAlignmentOptions.Center;
    }

    private void BuildBottomNavBar(Transform parent)
    {
        var nav = MakeRect("BottomNavBar", parent);
        Anchor(nav, 0, 0, 1, 0);
        SetPivot(nav, 0.5f, 0f);
        nav.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 65);

        var bg = nav.AddComponent<Image>();
        bg.color = C_WHITE;

        var hlg = nav.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = true;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.padding               = new RectOffset(8, 8, 8, 8);
        hlg.spacing               = 0;

        BuildNavTab(nav.transform, "HomeTab",    "Home");
        BuildNavTab(nav.transform, "SoldierTab", "Soldier");
        BuildNavTab(nav.transform, "ARScanTab",  "AR Scan");
        BuildNavTab(nav.transform, "AwardsTab",  "Awards");
        BuildNavTab(nav.transform, "ProfileTab", "Profile");
    }

    private void BuildNavTab(Transform parent, string tabName, string labelText)
    {
        var tab = MakeRect(tabName, parent);
        tab.AddComponent<Image>().color = C_CLEAR;
        tab.AddComponent<Button>();

        // childControlWidth/Height = false so VLG never overrides the explicit sizeDelta we set below.
        // childForceExpandWidth = false so the icon is not stretched to the full tab width.
        var vlg = tab.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.MiddleCenter;
        vlg.childControlWidth      = false;
        vlg.childControlHeight     = false;
        vlg.childForceExpandWidth  = false;
        vlg.childForceExpandHeight = false;
        vlg.spacing                = 2;
        vlg.padding                = new RectOffset(0, 0, 6, 6);

        // Icon: explicit 24×24 — immune to sprite native size
        var iconGo = MakeRect("Icon", tab.transform);
        iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 24f);
        iconGo.AddComponent<Image>().color = C_GRAY_88;

        var lblGo = MakeRect("Label", tab.transform);
        lblGo.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 13f);
        var lbl = lblGo.AddComponent<TextMeshProUGUI>();
        lbl.text      = labelText;
        lbl.fontSize  = 10;
        lbl.color     = C_GRAY_88;
        lbl.alignment = TextAlignmentOptions.Center;
    }

    private void BuildMainMenuScreen(Transform parent)
    {
        var screen = MakeScreen("MainMenuScreen", parent, C_WHITE);
        screen.AddComponent<MainMenuController>();

        var title = MakeRect("Title", screen.transform);
        AbsCenterH(title, 3.5f, 203f, 285f, 68f);
        var tt = title.AddComponent<TextMeshProUGUI>();
        tt.text = "MT. SAMAT"; tt.fontSize = 50; tt.fontStyle = FontStyles.Bold;
        tt.color = C_BLACK; tt.alignment = TextAlignmentOptions.Center;
        tt.enableWordWrapping = false;

        var sub = MakeRect("Subtitle", screen.transform);
        AbsCenterH(sub, 0f, 268f, 220f, 22f);
        var st = sub.AddComponent<TextMeshProUGUI>();
        st.text = "MT. SAMAT AR QUEST"; st.fontSize = 15;
        st.color = C_GRAY_88; st.alignment = TextAlignmentOptions.Center;
        st.enableWordWrapping = false;

        var div = MakeRect("Divider", screen.transform);
        AbsPos(div, 131f, 300f, 128f, 4f);
        div.AddComponent<Image>().color = C_GRAY_D9;

        var startBtn = MakeRect("StartMissionBtn", screen.transform);
        AbsStretchH(startBtn, 41f, 40f, 349f, 57f);
        startBtn.AddComponent<Image>().color = C_BLACK;
        startBtn.AddComponent<Button>();
        var startTxt = MakeRect("Text", startBtn.transform);
        SetFullScreen(startTxt);
        var smt = startTxt.AddComponent<TextMeshProUGUI>();
        smt.text = "START MISSION"; smt.fontSize = 17; smt.fontStyle = FontStyles.Bold;
        smt.color = C_WHITE; smt.alignment = TextAlignmentOptions.Center;

        BuildAbsFigmaBtn("HowToPlayBtn", screen.transform, "HOW TO PLAY", 40f, 41f, 412f, 57f);

        BuildAbsFigmaBtn("SettingsBtn",  screen.transform, "SETTINGS",    40f, 41f, 475f, 57f);

        var exitBtn = MakeRect("ExitBtn", screen.transform);
        AbsCenterH(exitBtn, 0f, 555f, 100f, 24f);
        exitBtn.AddComponent<Image>().color = C_CLEAR;
        exitBtn.AddComponent<Button>();
        var exitTxtGo = MakeRect("Text", exitBtn.transform);
        SetFullScreen(exitTxtGo);
        var et = exitTxtGo.AddComponent<TextMeshProUGUI>();
        et.text = "EXIT"; et.fontSize = 15;
        et.color = C_GRAY_AA; et.alignment = TextAlignmentOptions.Center;

        var ver = MakeRect("VersionLabel", screen.transform);
        AbsBottom(ver, 0f, 54f, 166f, 14f);
        var vt = ver.AddComponent<TextMeshProUGUI>();
        vt.text = "v1.2.0 · CLASSIFIED"; vt.fontSize = 10;
        vt.color = C_GRAY_AA; vt.alignment = TextAlignmentOptions.Center;
    }

    // Outline button with exact Figma absolute position + 1px border effect
    private void BuildAbsFigmaBtn(string name, Transform parent, string label,
                                   float left, float right, float figmaY, float h)
    {
        var go = MakeRect(name, parent);
        AbsStretchH(go, left, right, figmaY, h);
        go.AddComponent<Image>().color = C_GRAY_E8;
        go.AddComponent<Button>();

        // Inner fill (1px inset = border effect)
        var inner = MakeRect("Fill", go.transform);
        Anchor(inner, 0f, 0f, 1f, 1f);
        inner.GetComponent<RectTransform>().offsetMin = new Vector2( 1f,  1f);
        inner.GetComponent<RectTransform>().offsetMax = new Vector2(-1f, -1f);
        inner.AddComponent<Image>().color = C_GRAY_F8;

        var txtGo = MakeRect("Text", go.transform);
        SetFullScreen(txtGo);
        var t = txtGo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 15; t.fontStyle = FontStyles.Bold;
        t.color = C_BLACK; t.alignment = TextAlignmentOptions.Center;
    }

    private void BuildRegisterScreen(Transform parent)
    {
        var screen = MakeScreen("RegisterScreen", parent, C_WHITE);
        screen.SetActive(false);
        screen.AddComponent<RegisterController>();

        var backBtn = MakeRect("BackBtn", screen.transform);
        AbsPos(backBtn, 20f, 12f, 120f, 44f);
        backBtn.AddComponent<Image>().color = C_CLEAR;
        backBtn.AddComponent<Button>();
        var backTxtGo = MakeRect("Text", backBtn.transform);
        SetFullScreen(backTxtGo);
        var bt = backTxtGo.AddComponent<TextMeshProUGUI>();
        bt.text = "← Back"; bt.fontSize = 15; bt.color = C_GRAY_88;
        bt.alignment = TextAlignmentOptions.MidlineLeft;

        var divider = MakeRect("Divider", screen.transform);
        AbsStretchH(divider, 0f, 0f, 64f, 2f);
        divider.AddComponent<Image>().color = C_GRAY_D9;

        var title = MakeRect("Title", screen.transform);
        AbsCenterH(title, -2f, 89f, 320f, 36f);
        var tt = title.AddComponent<TextMeshProUGUI>();
        tt.text = "PERSONAL INFORMATION"; tt.fontSize = 25; tt.fontStyle = FontStyles.Bold;
        tt.color = C_BLACK; tt.alignment = TextAlignmentOptions.Center;
        tt.enableWordWrapping = false;

        var sub = MakeRect("Subtitle", screen.transform);
        AbsCenterH(sub, -50.5f, 133f, 270f, 22f);
        var st = sub.AddComponent<TextMeshProUGUI>();
        st.text = "Register to begin your mission"; st.fontSize = 15;
        st.color = C_GRAY_88; st.alignment = TextAlignmentOptions.Center;
        st.enableWordWrapping = false;

        var fullNameLbl = MakeRect("FullNameLabel", screen.transform);
        AbsPos(fullNameLbl, 41f, 179f, 100f, 20f);
        var fnl = fullNameLbl.AddComponent<TextMeshProUGUI>();
        fnl.text = "FULL NAME"; fnl.fontSize = 15; fnl.fontStyle = FontStyles.Bold;
        fnl.color = C_GRAY_88; fnl.alignment = TextAlignmentOptions.MidlineLeft;

        BuildAbsInputField("FullNameInput", screen.transform, 38f, 204f, 309f, 44f, "Juan dela Cruz");

        var fnHint = MakeRect("FullNameHint", screen.transform);
        AbsPos(fnHint, 38f, 255f, 180f, 18f);
        var fnh = fnHint.AddComponent<TextMeshProUGUI>();
        fnh.text = "// used for mission log"; fnh.fontSize = 12;
        fnh.color = C_GRAY_CC; fnh.alignment = TextAlignmentOptions.MidlineLeft;

        var userLbl = MakeRect("UsernameLabel", screen.transform);
        AbsPos(userLbl, 41f, 289f, 100f, 20f);
        var ul = userLbl.AddComponent<TextMeshProUGUI>();
        ul.text = "USERNAME"; ul.fontSize = 15; ul.fontStyle = FontStyles.Bold;
        ul.color = C_GRAY_88; ul.alignment = TextAlignmentOptions.MidlineLeft;

        BuildAbsInputField("UsernameInput", screen.transform, 38f, 314f, 309f, 44f, "Cutie_JDC");

        var unHint = MakeRect("UsernameHint", screen.transform);
        AbsPos(unHint, 38f, 365f, 200f, 18f);
        var unh = unHint.AddComponent<TextMeshProUGUI>();
        unh.text = "// shown on leaderboard"; unh.fontSize = 12;
        unh.color = C_GRAY_CC; unh.alignment = TextAlignmentOptions.MidlineLeft;

        var avLbl = MakeRect("AvatarLabel", screen.transform);
        AbsPos(avLbl, 41f, 407f, 150f, 20f);
        var avl = avLbl.AddComponent<TextMeshProUGUI>();
        avl.text = "CHOOSE AVATAR"; avl.fontSize = 15; avl.fontStyle = FontStyles.Bold;
        avl.color = C_GRAY_88; avl.alignment = TextAlignmentOptions.MidlineLeft;

        float[] avCols = { 37f, 115f, 197f, 277f };
        float[] avRows = { 444f, 528f };
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int idx = row * 4 + col;
                var av = MakeRect($"Avatar_{idx}", screen.transform);
                AbsPos(av, avCols[col], avRows[row], 70f, 70f);
                av.AddComponent<Image>().color = (idx == 0) ? C_BLACK : C_GRAY_E8;
                av.AddComponent<Button>();
            }
        }

        var saveBtn = MakeRect("SaveBtn", screen.transform);
        AbsStretchH(saveBtn, 38f, 41f, 635f, 53f);
        saveBtn.AddComponent<Image>().color = C_BLACK;
        saveBtn.AddComponent<Button>();
        var saveTxtGo = MakeRect("Text", saveBtn.transform);
        SetFullScreen(saveTxtGo);
        var saveTmp = saveTxtGo.AddComponent<TextMeshProUGUI>();
        saveTmp.text = "SAVE"; saveTmp.fontSize = 17; saveTmp.fontStyle = FontStyles.Bold;
        saveTmp.color = C_WHITE; saveTmp.alignment = TextAlignmentOptions.Center;

        var disc = MakeRect("Disclaimer", screen.transform);
        AbsCenterH(disc, 0.5f, 717f, 280f, 44f);
        var dt = disc.AddComponent<TextMeshProUGUI>();
        dt.text = "Your name will appear on badges & division discoveries";
        dt.fontSize = 12; dt.color = C_GRAY_CC;
        dt.alignment = TextAlignmentOptions.Center; dt.enableWordWrapping = true;
    }

    private void BuildAbsInputField(string name, Transform parent,
        float x, float y, float w, float h, string placeholder)
    {
        var outer = MakeRect(name, parent);
        AbsPos(outer, x, y, w, h);
        outer.AddComponent<Image>().color = C_GRAY_E8;

        // White inner fill (1px inset for border effect)
        var inner = MakeRect("InputBg", outer.transform);
        Anchor(inner, 0f, 0f, 1f, 1f);
        inner.GetComponent<RectTransform>().offsetMin = new Vector2(1f,  1f);
        inner.GetComponent<RectTransform>().offsetMax = new Vector2(-1f, -1f);
        inner.AddComponent<Image>().color = C_WHITE;

        var viewport = MakeRect("TextViewport", outer.transform);
        Anchor(viewport, 0f, 0f, 1f, 1f);
        viewport.GetComponent<RectTransform>().offsetMin = new Vector2(12f, 4f);
        viewport.GetComponent<RectTransform>().offsetMax = new Vector2(-12f, -4f);
        viewport.AddComponent<RectMask2D>();

        var textGo = MakeRect("Text", viewport.transform);
        SetFullScreen(textGo);
        var inputTmp = textGo.AddComponent<TextMeshProUGUI>();
        inputTmp.fontSize = 15; inputTmp.color = C_BLACK;
        inputTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var phGo = MakeRect("Placeholder", viewport.transform);
        SetFullScreen(phGo);
        var phTmp = phGo.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder; phTmp.fontSize = 15;
        phTmp.color = C_GRAY_AA; phTmp.fontStyle = FontStyles.Italic;
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var inputField = outer.AddComponent<TMP_InputField>();
        inputField.textViewport  = viewport.GetComponent<RectTransform>();
        inputField.textComponent = inputTmp;
        inputField.placeholder   = phTmp;
        inputField.characterLimit = 50;
    }

    private void BuildHowToPlayScreen(Transform parent)
    {
        var screen = MakeScreen("HowToPlayScreen", parent, C_WHITE);
        screen.SetActive(false);
        screen.AddComponent<HowToPlayController>();

        var statusBar = MakeRect("StatusBar", screen.transform);
        AbsStretchH(statusBar, 0f, 0f, 0f, 44f);
        statusBar.AddComponent<Image>().color = C_GRAY_F8;

        // Back button (not in Figma, required for navigation)
        var backBtn = MakeRect("BackBtn", screen.transform);
        AbsPos(backBtn, 16f, 8f, 80f, 30f);
        backBtn.AddComponent<Image>().color = C_CLEAR;
        backBtn.AddComponent<Button>();
        var backTxtGo = MakeRect("Text", backBtn.transform);
        SetFullScreen(backTxtGo);
        var bt = backTxtGo.AddComponent<TextMeshProUGUI>();
        bt.text = "← Back"; bt.fontSize = 14; bt.color = C_GRAY_88;
        bt.alignment = TextAlignmentOptions.MidlineLeft;

        var logoCircle = MakeRect("LogoCircleBg", screen.transform);
        AbsCenterH(logoCircle, 0f, 84f, 120f, 120f);
        logoCircle.AddComponent<Image>().color = new Color(0.851f, 0.851f, 0.851f, 0.1f);

        var logoBox = MakeRect("LogoPlaceholder", screen.transform);
        AbsPos(logoBox, 155f, 104f, 80f, 80f);
        logoBox.AddComponent<Image>().color = C_GRAY_E8;

        var title = MakeRect("Title", screen.transform);
        AbsCenterH(title, 0.5f, 222f, 285f, 40f);
        var tt = title.AddComponent<TextMeshProUGUI>();
        tt.text = "HOW TO PLAY"; tt.fontSize = 30; tt.fontStyle = FontStyles.Bold;
        tt.color = C_BLACK; tt.alignment = TextAlignmentOptions.Center;
        tt.enableWordWrapping = false;

        var appLabel = MakeRect("AppLabel", screen.transform);
        AbsCenterH(appLabel, 0f, 264f, 220f, 28f);
        var al = appLabel.AddComponent<TextMeshProUGUI>();
        al.text = "MT. SAMAT AR"; al.fontSize = 20; al.fontStyle = FontStyles.Bold;
        al.color = C_BLACK; al.alignment = TextAlignmentOptions.Center;
        al.enableWordWrapping = false;

        var cardBorder = MakeRect("StepsCard", screen.transform);
        AbsPos(cardBorder, 37f, 315f, 319f, 389f);
        cardBorder.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.1f);
        var cardFill = MakeRect("CardFill", cardBorder.transform);
        Anchor(cardFill, 0f, 0f, 1f, 1f);
        cardFill.GetComponent<RectTransform>().offsetMin = new Vector2(1f,  1f);
        cardFill.GetComponent<RectTransform>().offsetMax = new Vector2(-1f, -1f);
        cardFill.AddComponent<Image>().color = C_WHITE;

        BuildHowToStepAbs(screen.transform, "1",
            60f, 359f, 92f, 360f,
            115f, 359f, 189f, 92f, 386f, 269f,
            "Point camera at artifacts",
            "Look for circular markers near the monument. Use your AR lens to reveal hidden historical items.");

        BuildHowToStepAbs(screen.transform, "2",
            60f, 470f, 92f, 468f,
            119f, 468f, 185f, 92f, 495f, 241f,
            "Read the historical scroll",
            "Tap the artifact to open its ancient scroll and learn about its role in the 1942 defense.");

        BuildHowToStepAbs(screen.transform, "3",
            60f, 582f, 94f, 582f,
            112f, 582f, 229f, 91f, 609f, 236f,
            "Complete sets, earn rewards",
            "Collecting all items in a category unlocks special commemorative badges and physical rewards.");

        var ver = MakeRect("VersionLabel", screen.transform);
        AbsBottom(ver, 0f, 54f, 166f, 14f);
        var vt = ver.AddComponent<TextMeshProUGUI>();
        vt.text = "v1.2.0 · CLASSIFIED"; vt.fontSize = 10;
        vt.color = C_GRAY_AA; vt.alignment = TextAlignmentOptions.Center;
    }

    private void BuildHowToStepAbs(Transform parent, string num,
        float circleX, float circleY, float iconX, float iconY,
        float titleX,  float titleY,  float titleW,
        float descX,   float descY,   float descW,
        string titleText, string descText)
    {
        var circle = MakeRect($"Step{num}Circle", parent);
        AbsPos(circle, circleX, circleY, 21f, 21f);
        circle.AddComponent<Image>().color = C_BLACK;
        var numTxtGo = MakeRect("Num", circle.transform);
        SetFullScreen(numTxtGo);
        var nt = numTxtGo.AddComponent<TextMeshProUGUI>();
        nt.text = num; nt.fontSize = 13; nt.fontStyle = FontStyles.Bold;
        nt.color = C_WHITE; nt.alignment = TextAlignmentOptions.Center;

        var icon = MakeRect($"Step{num}Icon", parent);
        AbsPos(icon, iconX, iconY, 20f, 20f);
        icon.AddComponent<Image>().color = C_GRAY_E8;

        var titleGo = MakeRect($"Step{num}Title", parent);
        AbsPos(titleGo, titleX, titleY, titleW, 21f);
        var titTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titTmp.text = titleText; titTmp.fontSize = 14; titTmp.fontStyle = FontStyles.Bold;
        titTmp.color = C_BLACK; titTmp.alignment = TextAlignmentOptions.MidlineLeft;
        titTmp.enableWordWrapping = false;

        var descGo = MakeRect($"Step{num}Desc", parent);
        AbsPos(descGo, descX, descY, descW, 54f);
        var descTmp = descGo.AddComponent<TextMeshProUGUI>();
        descTmp.text = descText; descTmp.fontSize = 12;
        descTmp.color = new Color(0.471f, 0.443f, 0.424f, 1f);
        descTmp.alignment = TextAlignmentOptions.TopLeft;
        descTmp.enableWordWrapping = true;
    }

    private void BuildHomeScreen(Transform parent)
    {
        var screen = MakeScreen("HomeScreen", parent, C_WHITE);
        screen.SetActive(false);
        screen.AddComponent<HomeScreenController>();

        var progLbl = MakeRect("ProgressSectionLabel", screen.transform);
        AbsPos(progLbl, 41f, 86f, 165f, 24f);
        var plt = progLbl.AddComponent<TextMeshProUGUI>();
        plt.text = "CURRENT PROGRESS"; plt.fontSize = 18; plt.fontStyle = FontStyles.Bold;
        plt.color = C_GRAY_88; plt.enableWordWrapping = false;

        var pcOuter = MakeRect("ProgressCard", screen.transform);
        AbsPos(pcOuter, 30f, 119f, 329f, 84f);
        pcOuter.AddComponent<Image>().color = C_GRAY_E8;
        var pcInner = MakeRect("Fill", pcOuter.transform);
        Anchor(pcInner, 0f, 0f, 1f, 1f);
        pcInner.GetComponent<RectTransform>().offsetMin = new Vector2(1f,  1f);
        pcInner.GetComponent<RectTransform>().offsetMax = new Vector2(-1f, -1f);
        pcInner.AddComponent<Image>().color = C_GRAY_F8;

        var missionLbl = MakeRect("MissionLabel", screen.transform);
        AbsPos(missionLbl, 41f, 135f, 248f, 20f);
        var ml = missionLbl.AddComponent<TextMeshProUGUI>();
        ml.text = "Mission Progress: Gear Collection"; ml.fontSize = 14;
        ml.color = C_GRAY_88; ml.enableWordWrapping = false;

        var countLbl = MakeRect("ProgressCountLabel", screen.transform);
        AbsPos(countLbl, 305f, 138f, 54f, 22f);
        var pct = countLbl.AddComponent<TextMeshProUGUI>();
        pct.text = "0/0"; pct.fontSize = 13; pct.fontStyle = FontStyles.Bold;
        pct.color = C_GRAY_88; pct.alignment = TextAlignmentOptions.MidlineRight;

        var barBG = MakeRect("ProgressBarBG", screen.transform);
        AbsPos(barBG, 47f, 173f, 297f, 9f);
        barBG.AddComponent<Image>().color = C_GRAY_D9;

        var barFill = MakeRect("ProgressBarFill", barBG.transform);
        SetFullScreen(barFill);
        var fi = barFill.AddComponent<Image>();
        fi.color = C_BLACK; fi.type = Image.Type.Filled;
        fi.fillMethod = Image.FillMethod.Horizontal;
        fi.fillOrigin = (int)Image.OriginHorizontal.Left;
        fi.fillAmount = 0f;

        var awardLbl = MakeRect("AwardSectionLabel", screen.transform);
        AbsPos(awardLbl, 30f, 220f, 135f, 24f);
        var alt = awardLbl.AddComponent<TextMeshProUGUI>();
        alt.text = "AWARD PANEL"; alt.fontSize = 18; alt.fontStyle = FontStyles.Bold;
        alt.color = C_GRAY_88; alt.enableWordWrapping = false;

        var awardOuter = MakeRect("AwardPanelCard", screen.transform);
        AbsPos(awardOuter, 29f, 273f, 330f, 225f);
        awardOuter.AddComponent<Image>().color = C_GRAY_E8;
        var awardInner = MakeRect("Fill", awardOuter.transform);
        Anchor(awardInner, 0f, 0f, 1f, 1f);
        awardInner.GetComponent<RectTransform>().offsetMin = new Vector2(2f,  2f);
        awardInner.GetComponent<RectTransform>().offsetMax = new Vector2(-2f, -2f);
        awardInner.AddComponent<Image>().color = C_WHITE;

        float[] bCols  = { 55f,  129f, 203f, 277f };
        float[] bRows  = { 287f, 387f };
        float[] nmX1   = { 57f,  131f, 205f, 280f };
        float[] nmX2   = { 58f,  131f, 207f, 280f };
        float[] sbX1   = { 65f,  139f, 213f, 288f };
        float[] sbX2   = { 66f,  139f, 215f, 288f };
        var C_D8       = new Color(0.847f, 0.847f, 0.847f, 1f);

        for (int r = 0; r < 2; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                int idx = r * 4 + c;
                float slotY = bRows[r];

                var slot = MakeRect($"BadgeSlot_{idx}", screen.transform);
                AbsPos(slot, bCols[c], slotY, 58f, 62f);
                slot.AddComponent<Image>().color = C_GRAY_E8;
                var slotFill = MakeRect("Fill", slot.transform);
                Anchor(slotFill, 0f, 0f, 1f, 1f);
                slotFill.GetComponent<RectTransform>().offsetMin = new Vector2(1f,  1f);
                slotFill.GetComponent<RectTransform>().offsetMax = new Vector2(-1f, -1f);
                slotFill.AddComponent<Image>().color = new Color(0.851f, 0.851f, 0.851f, 0.1f);

                var nameBar = MakeRect($"NameBar_{idx}", screen.transform);
                float[] nmX = r == 0 ? nmX1 : nmX2;
                AbsPos(nameBar, nmX[c], slotY + 69f, 53f, 8f);
                nameBar.AddComponent<Image>().color = C_D8;

                var subBar = MakeRect($"SubBar_{idx}", screen.transform);
                float[] sbX = r == 0 ? sbX1 : sbX2;
                AbsPos(subBar, sbX[c], slotY + 81f, 38f, 8f);
                subBar.AddComponent<Image>().color = C_GRAY_E8;
            }
        }

        var soldierLbl = MakeRect("SoldierSectionLabel", screen.transform);
        AbsPos(soldierLbl, 30f, 511f, 220f, 24f);
        var slt = soldierLbl.AddComponent<TextMeshProUGUI>();
        slt.text = "CHOOSE YOUR SOLDIER"; slt.fontSize = 18; slt.fontStyle = FontStyles.Bold;
        slt.color = C_GRAY_88; slt.enableWordWrapping = false;

        var sb1 = MakeRect("SoldierInfoBar1", screen.transform);
        AbsPos(sb1, 30f, 539f, 327f, 13f);
        sb1.AddComponent<Image>().color = C_GRAY_E8;
        var sb2 = MakeRect("SoldierInfoBar2", screen.transform);
        AbsPos(sb2, 30f, 558f, 149f, 13f);
        sb2.AddComponent<Image>().color = C_GRAY_E8;

        BuildHomeSoldierCard(screen.transform, "FilipinoCard",
            32f, 583f, 103f, 159f, true,
            24f, 20f, 53f,
            51f, 81f,  "PH",
            51f, 98f,  "Filipino",
            51f, 117f, "Soldier");

        BuildHomeSoldierCard(screen.transform, "JapaneseCard",
            145f, 584f, 103f, 158f, false,
            26f, 19f, 53f,
            51.5f, 80f, "JP",
            52f,   97f, "Japanese",
            51f,   116f, "Soldier");

        BuildHomeSoldierCard(screen.transform, "AmericanCard",
            255f, 583f, 103f, 158f, false,
            26f, 20f, 53f,
            51f,   81f, "US",
            52.5f, 98f, "American",
            51f,   117f, "Soldier");
    }

    private void BuildHomeSoldierCard(Transform parent, string cardName,
        float cx, float cy, float cw, float ch, bool active,
        float avRelX, float avRelY, float avSize,
        float codeRelCX, float codeRelY, string code,
        float natRelCX,  float natRelY,  string nationality,
        float typeRelCX, float typeRelY, string soldierType)
    {
        var card = MakeRect(cardName, parent);
        AbsPos(card, cx, cy, cw, ch);
        card.AddComponent<Image>().color = active ? C_BLACK : C_GRAY_D9;
        card.AddComponent<Button>();
        if (!active) card.AddComponent<CanvasGroup>().alpha = 0.5f;

        var av = MakeRect("AvatarCircle", card.transform);
        AbsPos(av, avRelX, avRelY, avSize, avSize);
        av.AddComponent<Image>().color = C_GRAY_E8;

        var codeGo = MakeRect("CodeLabel", card.transform);
        AbsCenterH(codeGo, codeRelCX - cw * 0.5f, codeRelY, 40f, 16f);
        var ct = codeGo.AddComponent<TextMeshProUGUI>();
        ct.text = code; ct.fontSize = 10; ct.fontStyle = FontStyles.Bold;
        ct.color = C_GRAY_88; ct.alignment = TextAlignmentOptions.Center;

        var natGo = MakeRect("NatLabel", card.transform);
        AbsCenterH(natGo, natRelCX - cw * 0.5f, natRelY, 90f, 20f);
        var nt = natGo.AddComponent<TextMeshProUGUI>();
        nt.text = nationality; nt.fontSize = 15; nt.fontStyle = FontStyles.Bold;
        nt.color = active ? C_WHITE : C_BLACK; nt.alignment = TextAlignmentOptions.Center;

        var typeGo = MakeRect("TypeLabel", card.transform);
        AbsCenterH(typeGo, typeRelCX - cw * 0.5f, typeRelY, 90f, 20f);
        var tyt = typeGo.AddComponent<TextMeshProUGUI>();
        tyt.text = soldierType; tyt.fontSize = 15; tyt.fontStyle = FontStyles.Bold;
        tyt.color = active ? C_WHITE : C_BLACK; tyt.alignment = TextAlignmentOptions.Center;
    }

    private void BuildSoldierScreen(Transform parent)
    {
        const float HEADER_H = 280f;
        const float BTN_Y    = 716f;
        const float BTN_H    =  47f;
        const float CANVAS_H = 844f;

        var screen = MakeScreen("SoldierScreen", parent, C_WHITE);
        screen.SetActive(false);
        screen.AddComponent<SoldierScreenController>();

        var statusBar = MakeRect("StatusBar", screen.transform);
        AbsStretchH(statusBar, 0f, 0f, 0f, 27f);
        statusBar.AddComponent<Image>().color = C_GRAY_F8;

        var leftAv = MakeRect("LeftAvatar", screen.transform);
        AbsPos(leftAv, 98f, 67f, 65f, 65f);
        leftAv.AddComponent<Image>().color = C_GRAY_E8;

        var rightAv = MakeRect("RightAvatar", screen.transform);
        AbsPos(rightAv, 227f, 67f, 65f, 65f);
        rightAv.AddComponent<Image>().color = C_GRAY_E8;

        var centerAv = MakeRect("CenterAvatar", screen.transform);
        AbsCenterH(centerAv, 0f, 62f, 80f, 80f);
        centerAv.AddComponent<Image>().color = C_BLACK;

        var prevBtn = MakeRect("PrevBtn", screen.transform);
        AbsPos(prevBtn, 118f, 79f, 36f, 36f);
        prevBtn.AddComponent<Image>().color = C_CLEAR;
        prevBtn.AddComponent<Button>();
        var prevTxt = MakeRect("Text", prevBtn.transform);
        SetFullScreen(prevTxt);
        var pt = prevTxt.AddComponent<TextMeshProUGUI>();
        pt.text = "<"; pt.fontSize = 18; pt.fontStyle = FontStyles.Bold;
        pt.color = C_GRAY_88; pt.alignment = TextAlignmentOptions.Center;

        var nextBtn = MakeRect("NextBtn", screen.transform);
        AbsPos(nextBtn, 236f, 79f, 36f, 36f);
        nextBtn.AddComponent<Image>().color = C_CLEAR;
        nextBtn.AddComponent<Button>();
        var nextTxt = MakeRect("Text", nextBtn.transform);
        SetFullScreen(nextTxt);
        var nt2 = nextTxt.AddComponent<TextMeshProUGUI>();
        nt2.text = ">"; nt2.fontSize = 18; nt2.fontStyle = FontStyles.Bold;
        nt2.color = C_GRAY_88; nt2.alignment = TextAlignmentOptions.Center;

        var nameLabel = MakeRect("SoldierNameLabel", screen.transform);
        AbsCenterH(nameLabel, 0f, 159f, 280f, 28f);
        var nlt = nameLabel.AddComponent<TextMeshProUGUI>();
        nlt.text = "FILIPINO SOLDIER"; nlt.fontSize = 20; nlt.fontStyle = FontStyles.Bold;
        nlt.color = C_BLACK; nlt.alignment = TextAlignmentOptions.Center;
        nlt.enableWordWrapping = false;

        var progLblGo = MakeRect("ProgressLabel", screen.transform);
        AbsPos(progLblGo, 23f, 187f, 80f, 18f);
        var plt = progLblGo.AddComponent<TextMeshProUGUI>();
        plt.text = "Progress"; plt.fontSize = 12; plt.color = C_GRAY_88;
        plt.alignment = TextAlignmentOptions.MidlineLeft;

        var progCountGo = MakeRect("ProgressCountLabel", screen.transform);
        AbsPos(progCountGo, 316f, 187f, 54f, 18f);
        var pct = progCountGo.AddComponent<TextMeshProUGUI>();
        pct.text = "0/0"; pct.fontSize = 12; pct.color = C_GRAY_88;
        pct.alignment = TextAlignmentOptions.MidlineRight;

        var barBG = MakeRect("ProgressBarBG", screen.transform);
        AbsPos(barBG, 23f, 212f, 337f, 9f);
        barBG.AddComponent<Image>().color = C_GRAY_D9;
        var barFill = MakeRect("ProgressBarFill", barBG.transform);
        SetFullScreen(barFill);
        var fi = barFill.AddComponent<Image>();
        fi.color = C_BLACK; fi.type = Image.Type.Filled;
        fi.fillMethod = Image.FillMethod.Horizontal;
        fi.fillOrigin = (int)Image.OriginHorizontal.Left;
        fi.fillAmount = 0f;

        var div1 = MakeRect("Divider1", screen.transform);
        AbsStretchH(div1, 0f, 0f, 240f, 1f);
        div1.AddComponent<Image>().color = new Color(0.910f, 0.910f, 0.910f, 0.91f);

        var itemsLbl = MakeRect("ItemsToFindLabel", screen.transform);
        AbsPos(itemsLbl, 23f, 244f, 130f, 20f);
        var ilt = itemsLbl.AddComponent<TextMeshProUGUI>();
        ilt.text = "ITEMS TO FIND"; ilt.fontSize = 14; ilt.fontStyle = FontStyles.Bold;
        ilt.color = C_GRAY_88; ilt.enableWordWrapping = false;

        var div2 = MakeRect("Divider2", screen.transform);
        AbsStretchH(div2, 0f, 0f, 272f, 1f);
        div2.AddComponent<Image>().color = new Color(0.910f, 0.910f, 0.910f, 0.91f);

        var scanBtn = MakeRect("ScanNextItemBtn", screen.transform);
        AbsStretchH(scanBtn, 40f, 39f, BTN_Y, BTN_H);
        scanBtn.AddComponent<Image>().color = C_BLACK;
        scanBtn.AddComponent<Button>();
        var scanTxt = MakeRect("Text", scanBtn.transform);
        SetFullScreen(scanTxt);
        var st = scanTxt.AddComponent<TextMeshProUGUI>();
        st.text = "Scan next item"; st.fontSize = 17; st.fontStyle = FontStyles.Bold;
        st.color = C_WHITE; st.alignment = TextAlignmentOptions.Center;

        // Fills from y=280 to y=716 (between header and scan button)
        var sv = MakeRect("ArtifactScrollView", screen.transform);
        Anchor(sv, 0f, 0f, 1f, 1f);
        var svRT = sv.GetComponent<RectTransform>();
        svRT.offsetMin = new Vector2(0f, CANVAS_H - BTN_Y);
        svRT.offsetMax = new Vector2(0f, -HEADER_H);

        var sr = sv.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true;

        var vp = MakeRect("Viewport", sv.transform);
        SetFullScreen(vp);
        vp.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        var mask = vp.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var content = MakeRect("ArtifactList", vp.transform);
        Anchor(content, 0f, 1f, 1f, 1f); SetPivot(content, 0.5f, 1f);
        content.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        var cVlg = content.AddComponent<VerticalLayoutGroup>();
        cVlg.childAlignment = TextAnchor.UpperCenter;
        cVlg.childControlWidth = true; cVlg.childControlHeight = false;
        cVlg.childForceExpandWidth = true; cVlg.spacing = 0;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content  = content.GetComponent<RectTransform>();
        sr.viewport = vp.GetComponent<RectTransform>();
    }

    private void BuildCarouselBtn(string name, Transform parent, string label, Vector2 offset)
    {
        var btn = MakeRect(name, parent);
        Anchor(btn, 0.5f, 0.5f, 0.5f, 0.5f); SetPivot(btn, 0.5f, 0.5f);
        btn.GetComponent<RectTransform>().sizeDelta        = new Vector2(30, 30);
        btn.GetComponent<RectTransform>().anchoredPosition = offset;
        btn.AddComponent<Image>().color = C_CLEAR;
        btn.AddComponent<Button>();
        var txtGo = MakeRect("Text", btn.transform);
        SetFullScreen(txtGo);
        var t = txtGo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 18; t.fontStyle = FontStyles.Bold;
        t.color = C_GRAY_88; t.alignment = TextAlignmentOptions.Center;
    }

    private void BuildFullWidthDivider(string name, Transform parent)
    {
        var div = MakeRect(name, parent);
        div.AddComponent<Image>().color = C_GRAY_E8;
        div.AddComponent<LayoutElement>().preferredHeight = 1;
    }

    private void BuildARScanScreen(Transform parent)
    {
        var screen = MakeScreen("ARScanScreen", parent, C_CLEAR);
        screen.SetActive(false);
        screen.AddComponent<ARScanOverlayController>();

        // Remove the white background image so camera shows through
        var bg = screen.GetComponent<Image>();
        if (bg != null) bg.color = C_CLEAR;

        // Scan header (replaces TopBar on this screen)
        var header = MakeRect("ScanHeader", screen.transform);
        Anchor(header, 0, 1, 1, 1);
        SetPivot(header, 0.5f, 1f);
        header.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 65);
        header.AddComponent<Image>().color = C_WHITE;

        var pill = MakeRect("ScanningPill", header.transform);
        Anchor(pill, 0, 0.5f, 0, 0.5f);
        SetPivot(pill, 0f, 0.5f);
        var pillRT = pill.GetComponent<RectTransform>();
        pillRT.sizeDelta = new Vector2(108, 22); pillRT.anchoredPosition = new Vector2(20, 0);
        pill.AddComponent<Image>().color = C_GRAY_F8;

        var scanHlg = pill.AddComponent<HorizontalLayoutGroup>();
        scanHlg.childAlignment = TextAnchor.MiddleCenter;
        scanHlg.padding = new RectOffset(8, 8, 0, 0); scanHlg.spacing = 4;
        scanHlg.childControlHeight = false; scanHlg.childControlWidth = false;

        var dot = MakeRect("ScanDot", pill.transform);
        dot.GetComponent<RectTransform>().sizeDelta = new Vector2(7, 7);
        dot.AddComponent<Image>().color = C_GRAY_88;

        var scanLblGo = MakeRect("ScanLabel", pill.transform);
        scanLblGo.GetComponent<RectTransform>().sizeDelta = new Vector2(76, 15);
        var st = scanLblGo.AddComponent<TextMeshProUGUI>();
        st.text = "SCANNING"; st.fontSize = 13; st.fontStyle = FontStyles.Bold; st.color = C_GRAY_88; st.alignment = TextAlignmentOptions.MidlineLeft;

        var targetGo = MakeRect("TargetLabel", header.transform);
        Anchor(targetGo, 0, 0.5f, 0, 0.5f);
        SetPivot(targetGo, 0f, 0.5f);
        var tRT = targetGo.GetComponent<RectTransform>();
        tRT.sizeDelta = new Vector2(100, 15); tRT.anchoredPosition = new Vector2(140, 0);
        var tTmp = targetGo.AddComponent<TextMeshProUGUI>();
        tTmp.text = "Target 1 of 6"; tTmp.fontSize = 12; tTmp.fontStyle = FontStyles.Normal; tTmp.color = C_GRAY_88; tTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var gear = MakeRect("SettingsBtn", header.transform);
        Anchor(gear, 1, 0.5f, 1, 0.5f);
        SetPivot(gear, 1f, 0.5f);
        var gRT = gear.GetComponent<RectTransform>();
        gRT.sizeDelta = new Vector2(30, 30); gRT.anchoredPosition = new Vector2(-20, 0);
        gear.AddComponent<Image>().color = C_GRAY_E8;
        gear.AddComponent<Button>();

        var div = MakeRect("Divider", screen.transform);
        Anchor(div, 0, 1, 1, 1);
        SetPivot(div, 0.5f, 1f);
        div.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 2);
        div.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -64);
        div.AddComponent<Image>().color = C_GRAY_D9;

        BuildViewfinderCorners(screen.transform);

    }

    private void BuildViewfinderCorners(Transform parent)
    {
        float arm = 26f;
        float thick = 2f;
        float margin = 30f;

        // Top corners: anchored to top — negative Y moves down from top edge
        BuildLCorner("VF_TL", parent, new Vector2( margin, -90), arm, thick, true,  true);
        BuildLCorner("VF_TR", parent, new Vector2(-margin, -90), arm, thick, false, true);
        // Bottom corners: anchored to bottom — POSITIVE Y moves up from bottom edge
        // 80px clears the BottomNavBar (~65px) with a small gap
        BuildLCorner("VF_BL", parent, new Vector2( margin, 80),  arm, thick, true,  false);
        BuildLCorner("VF_BR", parent, new Vector2(-margin, 80),  arm, thick, false, false);
    }

    private void BuildLCorner(string name, Transform parent, Vector2 anchoredPos, float arm, float thick, bool isLeft, bool isTop)
    {
        var corner = MakeRect(name, parent);
        float anchorX = isLeft ? 0f : 1f;
        float anchorY = isTop  ? 1f : 0f;
        Anchor(corner, anchorX, anchorY, anchorX, anchorY);
        SetPivot(corner, isLeft ? 0f : 1f, isTop ? 1f : 0f);
        corner.GetComponent<RectTransform>().sizeDelta = new Vector2(arm, arm);
        corner.GetComponent<RectTransform>().anchoredPosition = anchoredPos;

        var h = MakeRect("H", corner.transform);
        Anchor(h, 0, isTop ? 1f : 0f, 1, isTop ? 1f : 0f);
        SetPivot(h, 0.5f, isTop ? 1f : 0f);
        h.GetComponent<RectTransform>().sizeDelta = new Vector2(0, thick);
        h.AddComponent<Image>().color = C_BLACK;

        var v = MakeRect("V", corner.transform);
        Anchor(v, isLeft ? 0f : 1f, 0, isLeft ? 0f : 1f, 1);
        SetPivot(v, isLeft ? 0f : 1f, 0.5f);
        v.GetComponent<RectTransform>().sizeDelta = new Vector2(thick, 0);
        v.AddComponent<Image>().color = C_BLACK;
    }

    // Absolute positioning. Figma origin → content_y = figma_y − 86
    // (subtracts status-bar 44 px + Figma header 42 px; Unity header is 50 px fixed above scroll).
    private void BuildSettingsScreen(Transform parent)
    {
        const float CONTENT_H = 1870f;

        var screen = MakeScreen("SettingsScreen", parent, C_WHITE);
        screen.SetActive(false);
        screen.AddComponent<SettingsScreenController>();

        var header = MakeRect("SettingsHeader", screen.transform);
        Anchor(header, 0, 1, 1, 1); SetPivot(header, 0.5f, 1f);
        header.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
        header.AddComponent<Image>().color = C_WHITE;
        var headerDiv = MakeRect("Divider", header.transform);
        Anchor(headerDiv, 0, 0, 1, 0); SetPivot(headerDiv, 0.5f, 0f);
        headerDiv.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 1);
        headerDiv.AddComponent<Image>().color = C_GRAY_E8;
        var backBtn = MakeRect("BackBtn", header.transform);
        Anchor(backBtn, 0, 0, 0, 1); SetPivot(backBtn, 0f, 0.5f);
        backBtn.GetComponent<RectTransform>().sizeDelta        = new Vector2(90, 0);
        backBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(16, 0);
        backBtn.AddComponent<Image>().color = C_CLEAR;
        backBtn.AddComponent<Button>();
        var backTxtGo = MakeRect("Text", backBtn.transform);
        SetFullScreen(backTxtGo);
        var backTmp = backTxtGo.AddComponent<TextMeshProUGUI>();
        backTmp.text = "← Back"; backTmp.fontSize = 14;
        backTmp.color = C_GRAY_88; backTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var sv = MakeRect("ScrollView", screen.transform);
        Anchor(sv, 0, 0, 1, 1);
        sv.GetComponent<RectTransform>().offsetMin = new Vector2(0, 65);
        sv.GetComponent<RectTransform>().offsetMax = new Vector2(0, -50);
        var sr = sv.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true;
        var vp = MakeRect("Viewport", sv.transform);
        SetFullScreen(vp);
        vp.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        vp.AddComponent<Mask>().showMaskGraphic = false;
        var content = MakeRect("Content", vp.transform);
        Anchor(content, 0, 1, 1, 1); SetPivot(content, 0.5f, 1f);
        content.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, CONTENT_H);
        sr.content  = content.GetComponent<RectTransform>();
        sr.viewport = vp.GetComponent<RectTransform>();

        var logoCircle = MakeRect("LogoCircleBg", content.transform);
        AbsCenterH(logoCircle, 0f, 0f, 120f, 120f);
        logoCircle.AddComponent<Image>().color = C_GRAY_E8;
        var appLogo = MakeRect("AppLogo", logoCircle.transform);
        Anchor(appLogo, 0.5f, 0.5f, 0.5f, 0.5f); SetPivot(appLogo, 0.5f, 0.5f);
        appLogo.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 80f);
        appLogo.AddComponent<Image>().color = C_GRAY_D9;

        var appNameGo = MakeRect("AppNameLabel", content.transform);
        AbsCenterH(appNameGo, 0f, 138f, 290f, 42f);
        var anTmp = appNameGo.AddComponent<TextMeshProUGUI>();
        anTmp.text = "MT. SAMAT AR"; anTmp.fontSize = 30; anTmp.fontStyle = FontStyles.Bold;
        anTmp.color = C_BLACK; anTmp.alignment = TextAlignmentOptions.Center;

        float[] descYs = { 219f, 243f, 267f, 291f };
        float[] descWs = { 295f, 295f, 295f, 186f };
        for (int i = 0; i < descYs.Length; i++)
        {
            var bar = MakeRect($"DescBar_{i}", content.transform);
            AbsPos(bar, 48f, descYs[i], descWs[i], 16f);
            bar.AddComponent<Image>().color = C_GRAY_D9;
        }

        var card1 = MakeRect("AppInfoCard1", content.transform);
        AbsPos(card1, 48f, 369f, 295f, 205f);
        card1.AddComponent<Image>().color = C_GRAY_F8;

        var card2 = MakeRect("AppInfoCard2", content.transform);
        AbsPos(card2, 48f, 604f, 295f, 205f);
        card2.AddComponent<Image>().color = C_GRAY_F8;

        var devTitleGo = MakeRect("DevelopersLabel", content.transform);
        AbsCenterH(devTitleGo, 0f, 845f, 300f, 40f);
        var dtTmp = devTitleGo.AddComponent<TextMeshProUGUI>();
        dtTmp.text = "MEET THE DEVELOPERS"; dtTmp.fontSize = 20; dtTmp.fontStyle = FontStyles.Bold;
        dtTmp.color = C_BLACK; dtTmp.alignment = TextAlignmentOptions.Center;

        var devCard = MakeRect("DeveloperCard", content.transform);
        AbsCenterH(devCard, 0f, 949f, 292f, 177f);
        devCard.AddComponent<Image>().color = C_GRAY_F8;
        var devAv = MakeRect("DevAvatar", devCard.transform);
        AbsPos(devAv, 27f, 23f, 60f, 60f);
        devAv.AddComponent<Image>().color = C_GRAY_D9;
        var devNameBar = MakeRect("DevNameBar", devCard.transform);
        AbsCenterH(devNameBar, 0f, 105f, 237f, 16f);
        devNameBar.AddComponent<Image>().color = C_GRAY_D9;
        var devRoleBar = MakeRect("DevRoleBar", devCard.transform);
        AbsCenterH(devRoleBar, 0f, 130f, 102f, 13f);
        devRoleBar.AddComponent<Image>().color = C_GRAY_E8;

        var partnersBg = MakeRect("PartnersBg", content.transform);
        AbsStretchH(partnersBg, 0f, 0f, 1200f, 290f);
        partnersBg.AddComponent<Image>().color = new Color(0.910f, 0.910f, 0.910f, 0.8f);

        var partnersTitle = MakeRect("PartnersTitle", content.transform);
        AbsCenterH(partnersTitle, 0f, 1224f, 310f, 56f);
        var ptTmp = partnersTitle.AddComponent<TextMeshProUGUI>();
        ptTmp.text = "Our Partners &\nSupporters"; ptTmp.fontSize = 20; ptTmp.fontStyle = FontStyles.Bold;
        ptTmp.color = C_BLACK; ptTmp.alignment = TextAlignmentOptions.Center;

        var afpLogo = MakeRect("AFP_Logo", content.transform);
        AbsPos(afpLogo, 103f, 1291f, 70f, 70f);
        afpLogo.AddComponent<Image>().color = C_GRAY_D9;
        {
            var lbl = MakeRect("Label", afpLogo.transform);
            Anchor(lbl, 0f, 0f, 1f, 0f); SetPivot(lbl, 0.5f, 1f);
            lbl.GetComponent<RectTransform>().sizeDelta        = new Vector2(0, 18f);
            lbl.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -4f);
            var lt = lbl.AddComponent<TextMeshProUGUI>();
            lt.text = "AFP"; lt.fontSize = 10; lt.fontStyle = FontStyles.Bold;
            lt.color = C_GRAY_88; lt.alignment = TextAlignmentOptions.Center;
        }

        var dotLogo = MakeRect("DOT_Logo", content.transform);
        AbsPos(dotLogo, 222f, 1291f, 70f, 70f);
        dotLogo.AddComponent<Image>().color = C_GRAY_D9;
        {
            var lbl = MakeRect("Label", dotLogo.transform);
            Anchor(lbl, 0f, 0f, 1f, 0f); SetPivot(lbl, 0.5f, 1f);
            lbl.GetComponent<RectTransform>().sizeDelta        = new Vector2(0, 18f);
            lbl.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -4f);
            var lt = lbl.AddComponent<TextMeshProUGUI>();
            lt.text = "DOT"; lt.fontSize = 10; lt.fontStyle = FontStyles.Bold;
            lt.color = C_GRAY_88; lt.alignment = TextAlignmentOptions.Center;
        }

        var nhcpLogo = MakeRect("NHCP_Logo", content.transform);
        AbsCenterH(nhcpLogo, 0f, 1398f, 70f, 70f);
        nhcpLogo.AddComponent<Image>().color = C_GRAY_D9;
        {
            var lbl = MakeRect("Label", nhcpLogo.transform);
            Anchor(lbl, 0f, 0f, 1f, 0f); SetPivot(lbl, 0.5f, 1f);
            lbl.GetComponent<RectTransform>().sizeDelta        = new Vector2(0, 18f);
            lbl.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -4f);
            var lt = lbl.AddComponent<TextMeshProUGUI>();
            lt.text = "NHCP"; lt.fontSize = 10; lt.fontStyle = FontStyles.Bold;
            lt.color = C_GRAY_88; lt.alignment = TextAlignmentOptions.Center;
        }

        var feedbackBg = MakeRect("FeedbackBg", content.transform);
        AbsStretchH(feedbackBg, 0f, 0f, 1580f, 220f);
        feedbackBg.AddComponent<Image>().color = new Color(0.910f, 0.910f, 0.910f, 0.8f);

        var feedbackTitle = MakeRect("FeedbackTitle", content.transform);
        AbsCenterH(feedbackTitle, 0f, 1602f, 310f, 56f);
        var ftTmp = feedbackTitle.AddComponent<TextMeshProUGUI>();
        ftTmp.text = "Have questions or\nfeedback?"; ftTmp.fontSize = 20; ftTmp.fontStyle = FontStyles.Bold;
        ftTmp.color = C_BLACK; ftTmp.alignment = TextAlignmentOptions.Center;

        var feedbackDesc = MakeRect("FeedbackDesc", content.transform);
        AbsCenterH(feedbackDesc, 0f, 1660f, 310f, 64f);
        var fdTmp = feedbackDesc.AddComponent<TextMeshProUGUI>();
        fdTmp.text = "We're always looking for ways to improve the scavenger hunt experience.";
        fdTmp.fontSize = 14; fdTmp.fontStyle = FontStyles.Normal;
        fdTmp.color = C_GRAY_88; fdTmp.alignment = TextAlignmentOptions.Center;

        var contactBtn = MakeRect("ContactUsBtn", content.transform);
        AbsCenterH(contactBtn, 0f, 1742f, 143f, 32f);
        contactBtn.AddComponent<Image>().color = C_WHITE;
        contactBtn.AddComponent<Button>();
        var contactTxtGo = MakeRect("Text", contactBtn.transform);
        SetFullScreen(contactTxtGo);
        var contactTmp = contactTxtGo.AddComponent<TextMeshProUGUI>();
        contactTmp.text = "Contact us →"; contactTmp.fontSize = 13; contactTmp.fontStyle = FontStyles.Bold;
        contactTmp.color = C_GRAY_CC; contactTmp.alignment = TextAlignmentOptions.Center;

        var verGo = MakeRect("VersionLabel", content.transform);
        AbsCenterH(verGo, 0f, 1800f, 300f, 24f);
        var vt = verGo.AddComponent<TextMeshProUGUI>();
        vt.text = "v1.0.0 · CLASSIFIED"; vt.fontSize = 10;
        vt.color = C_GRAY_AA; vt.alignment = TextAlignmentOptions.Center;
    }

    private void BuildAwardsScreen(Transform parent)
    {
        var screen = MakeScreen("AwardsScreen", parent, C_WHITE);
        screen.SetActive(false);
        screen.AddComponent<AwardsScreenController>();

        var sv = BuildScrollView("ScrollView", screen.transform);
        var content = sv.transform.Find("Viewport/Content");

        // Spacer to clear TopBar → title at screen y=110
        AddSpacer(content, 110);

        var titleGo = MakeRect("AwardsTitle", content);
        titleGo.AddComponent<LayoutElement>().preferredHeight = 28;
        var tt = titleGo.AddComponent<TextMeshProUGUI>();
        tt.text = "AWARDS"; tt.fontSize = 20; tt.fontStyle = FontStyles.Bold;
        tt.color = C_BLACK; tt.alignment = TextAlignmentOptions.MidlineLeft;
        tt.enableWordWrapping = false;

        AddSpacer(content, 7);
        AddDescBar(content, 328, 13, C_GRAY_D9);
        AddSpacer(content, 8);
        AddDescBar(content, 328, 13, C_GRAY_D9);
        AddSpacer(content, 8);
        AddDescBar(content, 225, 13, C_GRAY_E8);

        // Spacer to reach list start at y=242
        // 110(spacer)+28(title)+7+13+8+13+8+13 = 200; need 242 → add 42px
        AddSpacer(content, 42);

        var list = MakeRect("AwardsList", content);
        list.AddComponent<LayoutElement>().preferredHeight = 860; // 10×68 + 9×20
        var lVlg = list.AddComponent<VerticalLayoutGroup>();
        lVlg.spacing = 20;
        lVlg.childControlWidth = true; lVlg.childControlHeight = false;
        lVlg.childForceExpandWidth = true;
        lVlg.padding = new RectOffset(29, 29, 0, 0);

        for (int i = 0; i < 10; i++)
        {
            var row = MakeRect($"AwardRow_{i}", list.transform);
            row.AddComponent<LayoutElement>().preferredHeight = 68;
            row.AddComponent<Image>().color = new Color(0.910f, 0.910f, 0.910f, 0.1f);

            var rHlg = row.AddComponent<HorizontalLayoutGroup>();
            rHlg.padding = new RectOffset(15, 18, 9, 9);
            rHlg.spacing = 12;
            rHlg.childControlHeight = false; rHlg.childControlWidth = false;
            rHlg.childAlignment = TextAnchor.MiddleLeft;

            var badge = MakeRect("BadgeIcon", row.transform);
            badge.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 50);
            badge.AddComponent<Image>().color = (i == 0) ? C_BLACK : C_GRAY_F8;

            var info = MakeRect("InfoCol", row.transform);
            info.GetComponent<RectTransform>().sizeDelta = new Vector2(173, 50);
            var iVlg = info.AddComponent<VerticalLayoutGroup>();
            iVlg.childControlWidth = true; iVlg.childControlHeight = false;
            iVlg.childForceExpandWidth = true; iVlg.spacing = 4;

            var titleRow = MakeRect("AwardTitle", info.transform);
            titleRow.AddComponent<LayoutElement>().preferredHeight = 18;
            var trt = titleRow.AddComponent<TextMeshProUGUI>();
            trt.text = i == 0 ? "First Award" : "— Locked —";
            trt.fontSize = 13; trt.fontStyle = FontStyles.Bold;
            trt.color = C_BLACK; trt.alignment = TextAlignmentOptions.MidlineLeft;

            var descRow = MakeRect("AwardDesc", info.transform);
            descRow.AddComponent<LayoutElement>().preferredHeight = 28;
            var drt = descRow.AddComponent<TextMeshProUGUI>();
            drt.text = i == 0 ? "Collect your first artifact."
                               : "Collect more artifacts to unlock.";
            drt.fontSize = 11; drt.color = C_GRAY_88;
            drt.alignment = TextAlignmentOptions.TopLeft; drt.enableWordWrapping = true;

            var statusGo = MakeRect("StatusIcon", row.transform);
            statusGo.GetComponent<RectTransform>().sizeDelta = new Vector2(18, 18);
            statusGo.AddComponent<Image>().color = C_GRAY_E8;
        }

        AddSpacer(content, 75); // NavBar clearance
    }

    private void AddDescBar(Transform parent, float w, float h, Color color)
    {
        var go = MakeRect("DescBar", parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = h; le.preferredWidth = w;
        go.AddComponent<Image>().color = color;
    }

    private void BuildProfileScreen(Transform parent)
    {
        var screen = MakeScreen("ProfileScreen", parent, C_WHITE);
        screen.SetActive(false);
        screen.AddComponent<ProfileScreenController>();

        var header = MakeRect("ProfileHeader", screen.transform);
        AbsStretchH(header, 0f, 0f, 0f, 65f);
        header.AddComponent<Image>().color = C_WHITE;

        var headerDiv = MakeRect("Divider", header.transform);
        Anchor(headerDiv, 0f, 0f, 1f, 0f); SetPivot(headerDiv, 0.5f, 0f);
        headerDiv.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 2f);
        headerDiv.AddComponent<Image>().color = C_GRAY_D9;

        var headerTitleGo = MakeRect("HeaderTitle", header.transform);
        AbsPos(headerTitleGo, 20f, 17f, 140f, 28f);
        var htTmp = headerTitleGo.AddComponent<TextMeshProUGUI>();
        htTmp.text = "MT. SAMAT AR"; htTmp.fontSize = 14; htTmp.fontStyle = FontStyles.Bold;
        htTmp.color = C_BLACK; htTmp.alignment = TextAlignmentOptions.MidlineLeft;
        htTmp.enableWordWrapping = false;

        var gearBtn = MakeRect("SettingsBtn", header.transform);
        AbsPos(gearBtn, 329f, 20f, 34f, 34f);
        gearBtn.AddComponent<Image>().color = C_CLEAR;
        gearBtn.AddComponent<Button>();
        var gearTxtGo = MakeRect("Icon", gearBtn.transform);
        SetFullScreen(gearTxtGo);
        var gearTmp = gearTxtGo.AddComponent<TextMeshProUGUI>();
        gearTmp.text = "⚙"; gearTmp.fontSize = 20;
        gearTmp.color = C_GRAY_88; gearTmp.alignment = TextAlignmentOptions.Center;

        var sv = BuildScrollView("ScrollView", screen.transform);
        Anchor(sv, 0f, 0f, 1f, 1f);
        sv.GetComponent<RectTransform>().offsetMin = new Vector2(0f,  65f);
        sv.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -65f);

        var content = sv.transform.Find("Viewport/Content");

        var area = MakeRect("ProfileContentArea", content);
        area.AddComponent<LayoutElement>().preferredHeight = 1071f;

        var avCircle = MakeRect("AvatarCircle", area.transform);
        AbsCenterH(avCircle, -2f, 35f, 110f, 110f);
        avCircle.AddComponent<Image>().color = C_GRAY_E8;

        var xpBadge = MakeRect("XPBadge", area.transform);
        AbsPos(xpBadge, 190f, 118f, 58f, 22f);
        xpBadge.AddComponent<Image>().color = C_GRAY_D9;

        var nameUserGo = MakeRect("NameAndUsernameLabel", area.transform);
        AbsCenterH(nameUserGo, 0f, 162f, 320f, 26f);
        var nuTmp = nameUserGo.AddComponent<TextMeshProUGUI>();
        nuTmp.text = "<b>PLAYER NAME</b> <color=#888888>• @username</color>";
        nuTmp.fontSize = 16; nuTmp.color = C_BLACK;
        nuTmp.alignment = TextAlignmentOptions.Center; nuTmp.richText = true;
        nuTmp.enableWordWrapping = false;

        var leftStat = MakeRect("ArtifactsCard", area.transform);
        AbsPos(leftStat, 38f, 272f, 150f, 110f);
        leftStat.AddComponent<Image>().color = new Color(0.851f, 0.851f, 0.851f, 0.25f);
        BuildProfileStatContent(leftStat.transform, "—", "ARTIFACTS FOUND");

        var rightStat = MakeRect("BadgesCard", area.transform);
        AbsPos(rightStat, 202f, 272f, 150f, 110f);
        rightStat.AddComponent<Image>().color = new Color(0.851f, 0.851f, 0.851f, 0.25f);
        BuildProfileStatContent(rightStat.transform, "—", "BADGES EARNED");

        var wideStat = MakeRect("WideStatCard", area.transform);
        AbsPos(wideStat, 38f, 393f, 314f, 110f);
        wideStat.AddComponent<Image>().color = new Color(0.851f, 0.851f, 0.851f, 0.25f);
        BuildProfileStatContent(wideStat.transform, "—", "OVERALL JOURNEY");

        var recentLblGo = MakeRect("RecentAchLabel", area.transform);
        AbsPos(recentLblGo, 38f, 530f, 200f, 24f);
        var rl = recentLblGo.AddComponent<TextMeshProUGUI>();
        rl.text = "RECENT ACHIEVEMENTS"; rl.fontSize = 17; rl.fontStyle = FontStyles.Bold;
        rl.color = C_GRAY_88; rl.enableWordWrapping = false;

        var galleryBtnGo = MakeRect("ViewGalleryBtn", area.transform);
        AbsPos(galleryBtnGo, 272f, 532f, 80f, 20f);
        galleryBtnGo.AddComponent<Image>().color = C_CLEAR;
        galleryBtnGo.AddComponent<Button>();
        var galleryTxt = MakeRect("Text", galleryBtnGo.transform);
        SetFullScreen(galleryTxt);
        var gt = galleryTxt.AddComponent<TextMeshProUGUI>();
        gt.text = "View Gallery"; gt.fontSize = 11; gt.color = C_GRAY_88;
        gt.alignment = TextAlignmentOptions.MidlineRight;

        BuildAbsAchievementRow(area.transform, "Achievement_1", 572f, "COMPLETED",   "May 15, 2026");
        BuildAbsAchievementRow(area.transform, "Achievement_2", 654f, "COMPLETED",   "May 15, 2026");
        BuildAbsAchievementRow(area.transform, "Achievement_3", 739f, "IN PROGRESS", "80% Done");

        var progressCard = MakeRect("ProgressCard", area.transform);
        AbsCenterH(progressCard, -0.5f, 852f, 319f, 154f);
        progressCard.AddComponent<Image>().color = C_GRAY_F8;

        var progStub = MakeRect("ProgressStub", progressCard.transform);
        AbsPos(progStub, 19f, 46f, 81f, 18f);
        progStub.AddComponent<Image>().color = C_GRAY_D9;

        var barBG = MakeRect("ProgressBarBG", progressCard.transform);
        AbsPos(barBG, 20f, 103f, 278f, 5f);
        barBG.AddComponent<Image>().color = C_GRAY_D9;
        var barFill = MakeRect("ProgressBarFill", barBG.transform);
        SetFullScreen(barFill);
        var fi = barFill.AddComponent<Image>();
        fi.color = C_BLACK; fi.type = Image.Type.Filled;
        fi.fillMethod = Image.FillMethod.Horizontal;
        fi.fillOrigin = (int)Image.OriginHorizontal.Left;
        fi.fillAmount = 0f;

        var progressLblGo = MakeRect("ProgressLabel", progressCard.transform);
        AbsPos(progressLblGo, 19f, 112f, 220f, 22f);
        var plt = progressLblGo.AddComponent<TextMeshProUGUI>();
        plt.text = "Progress: 0/0 Artifacts Found"; plt.fontSize = 11;
        plt.color = C_GRAY_88; plt.alignment = TextAlignmentOptions.MidlineLeft;

        var resumeBtnGo = MakeRect("ResumeBtn", progressCard.transform);
        AbsPos(resumeBtnGo, 210f, 111f, 88f, 24f);
        resumeBtnGo.AddComponent<Image>().color = C_CLEAR;
        resumeBtnGo.AddComponent<Button>();
        var resumeTxtGo = MakeRect("Text", resumeBtnGo.transform);
        SetFullScreen(resumeTxtGo);
        var resumeTmp = resumeTxtGo.AddComponent<TextMeshProUGUI>();
        resumeTmp.text = "Resume Journey"; resumeTmp.fontSize = 10;
        resumeTmp.color = C_GRAY_88; resumeTmp.fontStyle = FontStyles.Underline;
        resumeTmp.alignment = TextAlignmentOptions.MidlineRight;
    }

    private void BuildProfileStatContent(Transform card, string value, string label)
    {
        var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter; vlg.spacing = 4;
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true; vlg.padding = new RectOffset(8, 8, 12, 12);

        var valGo = MakeRect("Value", card.transform);
        valGo.AddComponent<LayoutElement>().preferredHeight = 32;
        var vt = valGo.AddComponent<TextMeshProUGUI>();
        vt.text = value; vt.fontSize = 24; vt.fontStyle = FontStyles.Bold;
        vt.color = C_BLACK; vt.alignment = TextAlignmentOptions.Center;

        var lGo = MakeRect("CardLabel", card.transform);
        lGo.AddComponent<LayoutElement>().preferredHeight = 16;
        var lt = lGo.AddComponent<TextMeshProUGUI>();
        lt.text = label; lt.fontSize = 11; lt.color = C_GRAY_88;
        lt.alignment = TextAlignmentOptions.Center;
    }

    private void BuildAbsAchievementRow(Transform parent, string name, float contentY,
        string status, string date)
    {
        var row = MakeRect(name, parent);
        AbsStretchH(row, 38f, 38f, contentY, 73f);
        row.AddComponent<Image>().color = new Color(0.910f, 0.910f, 0.910f, 0.6f);

        var icon = MakeRect("AchIcon", row.transform);
        AbsPos(icon, 21f, 10f, 55f, 55f);
        icon.AddComponent<Image>().color = C_GRAY_F8;

        var titleBar = MakeRect("AchTitle", row.transform);
        AbsPos(titleBar, 88f, 20f, 135f, 13f);
        titleBar.AddComponent<Image>().color = C_GRAY_D9;

        var subBar = MakeRect("AchSub", row.transform);
        AbsPos(subBar, 88f, 39f, 79f, 13f);
        subBar.AddComponent<Image>().color = C_GRAY_E8;

        var stGo = MakeRect("StatusLabel", row.transform);
        AbsPos(stGo, 220f, 18f, 88f, 18f);
        var st = stGo.AddComponent<TextMeshProUGUI>();
        st.text = status; st.fontSize = 10; st.color = C_GRAY_88;
        st.alignment = TextAlignmentOptions.MidlineRight;

        var dtGo = MakeRect("DateLabel", row.transform);
        AbsPos(dtGo, 220f, 39f, 88f, 16f);
        var dt = dtGo.AddComponent<TextMeshProUGUI>();
        dt.text = date; dt.fontSize = 8; dt.color = C_GRAY_AA;
        dt.alignment = TextAlignmentOptions.MidlineRight;
    }

    private void BuildStatCard(Transform parent, string name, string count, string label)
    {
        var card = MakeRect(name, parent);
        card.AddComponent<Image>().color = new Color(0.851f, 0.851f, 0.851f, 0.25f);
        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter; vlg.spacing = 4;
        vlg.childControlWidth = true; vlg.childControlHeight = false; vlg.childForceExpandWidth = true;
        vlg.padding = new RectOffset(8, 8, 12, 12);

        var countGo = MakeRect("Count", card.transform);
        countGo.AddComponent<LayoutElement>().preferredHeight = 32;
        var ct = countGo.AddComponent<TextMeshProUGUI>();
        ct.text = count; ct.fontSize = 24; ct.fontStyle = FontStyles.Bold; ct.color = C_BLACK; ct.alignment = TextAlignmentOptions.Center;

        var lGo = MakeRect("CardLabel", card.transform);
        lGo.AddComponent<LayoutElement>().preferredHeight = 16;
        var lt = lGo.AddComponent<TextMeshProUGUI>();
        lt.text = label; lt.fontSize = 11; lt.color = C_GRAY_88; lt.alignment = TextAlignmentOptions.Center;
    }

    private void BuildWideStatCard(Transform parent)
    {
        var card = MakeRect("WideStatCard", parent);
        card.AddComponent<LayoutElement>().preferredHeight = 110;
        card.AddComponent<Image>().color = new Color(0.851f, 0.851f, 0.851f, 0.25f);
        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter; vlg.spacing = 4;
        vlg.padding = new RectOffset(16, 16, 12, 12);
        vlg.childControlWidth = true; vlg.childControlHeight = false; vlg.childForceExpandWidth = true;

        AddTMP("WideLabel", card.transform, "OVERALL JOURNEY", 13, FontStyles.Bold, C_GRAY_88, 20, TextAlignmentOptions.Center);
        AddTMP("WideValue", card.transform, "—", 28, FontStyles.Bold, C_BLACK, 36, TextAlignmentOptions.Center);
    }

    private void BuildAchievementRow(Transform parent, string name, string status, string date)
    {
        var row = MakeRect(name, parent);
        row.AddComponent<LayoutElement>().preferredHeight = 73;
        row.AddComponent<Image>().color = new Color(0.910f, 0.910f, 0.910f, 0.6f);

        var icon = MakeRect("AchIcon", row.transform);
        Anchor(icon, 0, 0, 0, 1); SetPivot(icon, 0f, 0.5f);
        icon.GetComponent<RectTransform>().sizeDelta        = new Vector2(55, -16);
        icon.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 0);
        icon.AddComponent<Image>().color = C_GRAY_F8;

        var titleGo = MakeRect("AchTitle", row.transform);
        Anchor(titleGo, 0, 0.5f, 1, 1); SetPivot(titleGo, 0f, 1f);
        titleGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(88, -8);
        titleGo.GetComponent<RectTransform>().sizeDelta        = new Vector2(-180, 0);
        var tTmp = titleGo.AddComponent<TextMeshProUGUI>();
        tTmp.text = "— Achievement —"; tTmp.fontSize = 13;
        tTmp.fontStyle = FontStyles.Bold; tTmp.color = C_GRAY_88;
        tTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var stGo = MakeRect("StatusLabel", row.transform);
        Anchor(stGo, 1, 0.5f, 1, 1); SetPivot(stGo, 1f, 1f);
        stGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-12, -8);
        stGo.GetComponent<RectTransform>().sizeDelta        = new Vector2(80, 16);
        var stTmp = stGo.AddComponent<TextMeshProUGUI>();
        stTmp.text = status; stTmp.fontSize = 10; stTmp.color = C_GRAY_88;
        stTmp.alignment = TextAlignmentOptions.MidlineRight;

        var dtGo = MakeRect("DateLabel", row.transform);
        Anchor(dtGo, 1, 0, 1, 0.5f); SetPivot(dtGo, 1f, 0f);
        dtGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-12, 8);
        dtGo.GetComponent<RectTransform>().sizeDelta        = new Vector2(80, 14);
        var dtTmp = dtGo.AddComponent<TextMeshProUGUI>();
        dtTmp.text = date; dtTmp.fontSize = 8; dtTmp.color = C_GRAY_AA;
        dtTmp.alignment = TextAlignmentOptions.MidlineRight;
    }

    private void BuildDebugPanel(Transform parent)
    {
        var panel = MakeRect("AR_DebugPanel", parent);
        // Anchored to bottom, full width, sits 75px above bottom (10px clear of 65px BottomNavBar)
        Anchor(panel, 0, 0, 1, 0);
        SetPivot(panel, 0.5f, 0f);
        var rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(-20, 220);
        rt.anchoredPosition = new Vector2(0, 145);   // 145px from bottom: above ActionPanel (70+60+15)

        // Background is now managed at runtime by ARDebugPanel.SetVisible()
        panel.AddComponent<Image>().color = Color.clear;
        var arDebugPanel = panel.AddComponent<ARDebugPanel>();

        var debugTextGo = MakeRect("DebugInfo", panel.transform);
        SetFullScreen(debugTextGo);
        var tmp = debugTextGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize            = 13;
        tmp.color               = Color.white;
        tmp.alignment           = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping  = true;
        tmp.overflowMode        = TextOverflowModes.Ellipsis;
        tmp.margin              = new Vector4(12, 12, 12, 12);

        // Wire the reference so ARDebugPanel.ShowInfo() can write to this TMP.
        arDebugPanel.debugText = tmp;

        panel.SetActive(false);
    }

    private void BuildARActionPanel(Transform parent)
    {
        // Bottom-anchored panel with COLLECT and SHOW INFO buttons.
        // ArtifactActionPanel.cs resolves children by name at runtime.
        var panel = MakeRect("AR_ActionPanel", parent);
        Anchor(panel, 0, 0, 1, 0);
        SetPivot(panel, 0.5f, 0f);
        var rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(-20, 60);
        rt.anchoredPosition = new Vector2(0f, 70f);  // 70px from bottom: 65px NavBar + 5px gap

        panel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.06f, 0.85f);
        panel.AddComponent<ArtifactActionPanel>();

        // ButtonRow — resolved by ArtifactActionPanel via transform.Find("ButtonRow")
        var row = MakeRect("ButtonRow", panel.transform);
        SetFullScreen(row);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing               = 10;
        hlg.padding               = new RectOffset(12, 12, 8, 8);
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childControlWidth     = true;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth = true;

        // CollectButton — resolved via "ButtonRow/CollectButton"
        var collectGo = MakeRect("CollectButton", row.transform);
        collectGo.AddComponent<Image>().color = new Color(0.290f, 0.486f, 0.349f);
        collectGo.AddComponent<Button>();
        var collectText = MakeRect("Text", collectGo.transform);
        SetFullScreen(collectText);
        var cTmp = collectText.AddComponent<TextMeshProUGUI>();
        cTmp.text      = "COLLECT";
        cTmp.fontSize  = 13;
        cTmp.fontStyle = FontStyles.Bold;
        cTmp.color     = Color.white;
        cTmp.alignment = TextAlignmentOptions.Center;
        cTmp.enableWordWrapping = false;

        // ShowInfoButton — resolved via "ButtonRow/ShowInfoButton"
        var infoGo = MakeRect("ShowInfoButton", row.transform);
        infoGo.AddComponent<Image>().color = new Color(0.910f, 0.910f, 0.910f);
        infoGo.AddComponent<Button>();
        var infoText = MakeRect("Text", infoGo.transform);
        SetFullScreen(infoText);
        var iTmp = infoText.AddComponent<TextMeshProUGUI>();
        iTmp.text      = "SHOW INFO";
        iTmp.fontSize  = 13;
        iTmp.fontStyle = FontStyles.Bold;
        iTmp.color     = new Color(0.102f, 0.102f, 0.102f);
        iTmp.alignment = TextAlignmentOptions.Center;
        iTmp.enableWordWrapping = false;

        // Hidden by default; NavigationManager.ShowScreen controls SetActive
        panel.SetActive(false);
    }

    private void BuildARCollectBanner(Transform parent)
    {
        // Top-anchored slide-in banner shown after artifact collection.
        // CollectNotificationBanner.cs resolves children by name at runtime
        // and animates anchoredPosition.y to slide in/out.
        var panel = MakeRect("AR_CollectBanner", parent);
        Anchor(panel, 0, 1, 1, 1);
        SetPivot(panel, 0.5f, 1f);
        var rt = panel.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(0f, 60f);
        rt.anchoredPosition = new Vector2(0f, 70f);   // starts off-screen above (script sets HIDDEN_Y=70)

        panel.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.06f, 0.88f);
        panel.AddComponent<CollectNotificationBanner>();

        // BannerContent — resolved via "BannerContent"
        var content = MakeRect("BannerContent", panel.transform);
        SetFullScreen(content);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.MiddleCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = false;
        vlg.childForceExpandWidth = true;
        vlg.padding               = new RectOffset(16, 16, 6, 6);
        vlg.spacing               = 2;

        var collectedGo = MakeRect("CollectedLabel", content.transform);
        collectedGo.AddComponent<LayoutElement>().preferredHeight = 22;
        var cLabel = collectedGo.AddComponent<TextMeshProUGUI>();
        cLabel.text      = "Artifact collected!";
        cLabel.fontSize  = 14;
        cLabel.fontStyle = FontStyles.Bold;
        cLabel.color     = Color.white;
        cLabel.alignment = TextAlignmentOptions.Center;
        cLabel.enableWordWrapping = false;

        var remainingGo = MakeRect("RemainingLabel", content.transform);
        remainingGo.AddComponent<LayoutElement>().preferredHeight = 18;
        var rLabel = remainingGo.AddComponent<TextMeshProUGUI>();
        rLabel.text      = "artifacts remaining";
        rLabel.fontSize  = 11;
        rLabel.color     = new Color(0.800f, 0.800f, 0.800f);
        rLabel.alignment = TextAlignmentOptions.Center;
        rLabel.enableWordWrapping = false;

        // Hidden by default; NavigationManager.ShowScreen controls SetActive
        panel.SetActive(false);
    }

    private GameObject BuildScrollView(string name, Transform parent)
    {
        var sv = MakeRect(name, parent);
        SetFullScreen(sv);

        var sr = sv.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical   = true;

        var vp = MakeRect("Viewport", sv.transform);
        SetFullScreen(vp);
        var vpImg  = vp.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);
        var mask   = vp.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var content = MakeRect("Content", vp.transform);
        Anchor(content, 0, 1, 1, 1);
        SetPivot(content, 0.5f, 1f);
        content.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = false;
        vlg.childForceExpandWidth = true;
        vlg.padding               = new RectOffset(30, 30, 0, 0);
        vlg.spacing               = 10;

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.content  = content.GetComponent<RectTransform>();
        sr.viewport = vp.GetComponent<RectTransform>();

        return sv;
    }

    private void BuildInputField(string name, Transform parent, string placeholder)
    {
        var fieldGo = MakeRect(name, parent);
        fieldGo.AddComponent<LayoutElement>().preferredHeight = 44;
        fieldGo.AddComponent<Image>().color = C_WHITE;

        var inputField = fieldGo.AddComponent<TMP_InputField>();

        var textArea = MakeRect("Text Area", fieldGo.transform);
        SetFullScreen(textArea);
        textArea.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0);
        textArea.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);

        var ph = MakeRect("Placeholder", textArea.transform);
        SetFullScreen(ph);
        var phTmp = ph.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder; phTmp.fontSize = 15; phTmp.color = C_GRAY_CC; phTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var textComp = MakeRect("Text", textArea.transform);
        SetFullScreen(textComp);
        var textTmp = textComp.AddComponent<TextMeshProUGUI>();
        textTmp.fontSize = 15; textTmp.color = C_BLACK; textTmp.alignment = TextAlignmentOptions.MidlineLeft;

        inputField.textViewport   = textArea.GetComponent<RectTransform>();
        inputField.textComponent  = textTmp;
        inputField.placeholder    = phTmp;
        inputField.characterLimit = 50;
    }

    private void BuildAvatarGrid(Transform parent)
    {
        var grid = MakeRect("AvatarGrid", parent);
        grid.AddComponent<LayoutElement>().preferredHeight = 164;

        var glg = grid.AddComponent<GridLayoutGroup>();
        glg.cellSize       = new Vector2(66, 66);
        glg.spacing        = new Vector2(10, 10);
        glg.padding        = new RectOffset(0, 0, 8, 8);
        glg.constraint     = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;

        for (int i = 0; i < 8; i++)
        {
            var av = MakeRect($"Avatar_{i}", grid.transform);
            var img = av.AddComponent<Image>();
            img.color = (i == 0) ? C_BLACK : C_GRAY_E8;
            av.AddComponent<Button>();
        }
    }

    private void BuildColorBtn(string name, Transform parent, string label, Color bg, Color txt, float height, float fontSize)
    {
        var go = MakeRect(name, parent);
        go.AddComponent<Image>().color = bg;
        go.AddComponent<Button>();
        go.AddComponent<LayoutElement>().preferredHeight = height;

        var lblGo = MakeRect("Text", go.transform);
        SetFullScreen(lblGo);
        var t = lblGo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = fontSize; t.fontStyle = FontStyles.Bold; t.color = txt; t.alignment = TextAlignmentOptions.Center;
    }

    private void BuildOutlineBtn(string name, Transform parent, string label, float height)
    {
        var go = MakeRect(name, parent);
        go.AddComponent<Image>().color = C_GRAY_F8;
        go.AddComponent<Button>();
        go.AddComponent<LayoutElement>().preferredHeight = height;

        var lblGo = MakeRect("Text", go.transform);
        SetFullScreen(lblGo);
        var t = lblGo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 15; t.fontStyle = FontStyles.Normal; t.color = C_BLACK; t.alignment = TextAlignmentOptions.Center;
    }

    private void AddFieldLabel(Transform parent, string text)
    {
        var go = MakeRect("FieldLabel_" + text, parent);
        go.AddComponent<LayoutElement>().preferredHeight = 22;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = 15; t.fontStyle = FontStyles.Bold; t.color = C_GRAY_88; t.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private GameObject MakeScreen(string name, Transform parent, Color bg)
    {
        var go = MakeRect(name, parent);
        SetFullScreen(go);
        go.AddComponent<Image>().color = bg;
        return go;
    }

    private GameObject MakeGroup(string name, Transform parent, Color bg)
    {
        var go = MakeRect(name, parent);
        SetFullScreen(go);
        if (bg != C_CLEAR)
            go.AddComponent<Image>().color = bg;
        return go;
    }

    private GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetFullScreen(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin       = Vector2.zero;
        rt.anchorMax       = Vector2.one;
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.sizeDelta       = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    // AbsPos: anchor top-left, exact Figma x/y/w/h
    private static void AbsPos(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, -y);
    }

    // AbsCenterH: anchor top-center, centered horizontally with optional x offset
    private static void AbsCenterH(GameObject go, float xOffset, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(xOffset, -y);
    }

    // AbsStretchH: anchors 0→1 horizontally, positioned from top with Figma left/right padding
    private static void AbsStretchH(GameObject go, float left, float right, float y, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(left,   -(y + h));
        rt.offsetMax = new Vector2(-right, -y);
    }

    // AbsBottom: anchor bottom-center, positioned from bottom edge
    private static void AbsBottom(GameObject go, float xOffset, float fromBottom, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(xOffset, fromBottom);
    }

    private static void Anchor(GameObject go, float minX, float minY, float maxX, float maxY)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
    }

    private static void SetPivot(GameObject go, float x, float y)
    {
        go.GetComponent<RectTransform>().pivot = new Vector2(x, y);
    }

    private void AddTMP(string name, Transform parent, string text, float size, FontStyles style, Color color, float prefHeight, TextAlignmentOptions align)
    {
        var go = MakeRect(name, parent);
        go.AddComponent<LayoutElement>().preferredHeight = prefHeight;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color; t.alignment = align; t.enableWordWrapping = true;
    }

    private GameObject AddSpacer(Transform parent, float height)
    {
        var go = MakeRect("Spacer", parent);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        go.AddComponent<Image>().color = C_CLEAR;
        return go;
    }

    private static void EnsureComponent<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() == null) go.AddComponent<T>();
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
}
