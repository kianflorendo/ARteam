using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UIHierarchySetup - Auto-generates the complete UI hierarchy for Phase 8 testing
/// 
/// USAGE:
/// 1. Create empty GameObject in scene, name it "UI_ROOT"
/// 2. Add this script as component
/// 3. Press Play
/// 4. Click "Generate UI Hierarchy" button in Inspector
/// 5. Stop Play mode - the hierarchy persists!
/// 
/// This creates:
/// - Canvas with proper settings
/// - TopBar with logo and token counter
/// - All 4 screen containers with required Transform references
/// - BottomNavBar with 6 tabs
/// - AR Debug Panel for testing
/// - EventSystem if missing
/// </summary>
[ExecuteInEditMode]
public class UIHierarchySetup : MonoBehaviour
{
    [Header("Setup Control")]
    [Tooltip("Click this in Inspector to generate the UI hierarchy")]
    public bool generateHierarchy = false;

    [Header("Terra Design Colors")]
    public Color primaryGreen = new Color(0.29f, 0.49f, 0.35f); // #4a7c59
    public Color backgroundCream = new Color(0.98f, 0.96f, 0.94f); // #faf6f0
    public Color textDark = new Color(0.2f, 0.2f, 0.2f);
    public Color textLight = new Color(0.95f, 0.95f, 0.95f);

    private Canvas _mainCanvas;
    private GameObject _topBar;
    private GameObject _screensContainer;
    private GameObject _bottomNavBar;
    private GameObject _debugPanel;

    // ───────────────────────────────────────────────────────────────────
    // Inspector Button Trigger
    // ───────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (generateHierarchy)
        {
            generateHierarchy = false;
            GenerateCompleteHierarchy();
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // Main Generation Method
    // ───────────────────────────────────────────────────────────────────

    public void GenerateCompleteHierarchy()
    {
        Debug.Log("[UIHierarchySetup] Starting UI generation...");

        // Create main canvas
        _mainCanvas = CreateMainCanvas();

        // Create top bar
        _topBar = CreateTopBar(_mainCanvas.transform);

        // Create screens container
        _screensContainer = CreateScreensContainer(_mainCanvas.transform);

        // Create individual screens
        CreateAboutScreen(_screensContainer.transform);
        CreateSoldierInventoryScreen(_screensContainer.transform);
        CreateHomeScreen(_screensContainer.transform);
        CreateDivisionsListScreen(_screensContainer.transform);
        CreateDivisionDetailScreen(_screensContainer.transform);
        CreateProfileScreen(_screensContainer.transform);
        CreateCameraScreen(_screensContainer.transform);

        // Create bottom navigation bar
        _bottomNavBar = CreateBottomNavBar(_mainCanvas.transform);

        // Create AR debug panel
        _debugPanel = CreateDebugPanel(_mainCanvas.transform);

        // Ensure EventSystem exists
        CreateEventSystem();

        Debug.Log("[UIHierarchySetup] ✅ UI hierarchy generated successfully!");
        Debug.Log("[UIHierarchySetup] Next steps:");
        Debug.Log("  1. Stop Play mode (hierarchy will persist)");
        Debug.Log("  2. Wire up screen references in TestPhase8Controller");
        Debug.Log("  3. Create prefabs for artifact cards (or use placeholders)");
        Debug.Log("  4. Press Play and test with buttons!");
    }

    // ───────────────────────────────────────────────────────────────────
    // Canvas Creation
    // ───────────────────────────────────────────────────────────────────

    private Canvas CreateMainCanvas()
    {
        GameObject canvasObj = new GameObject("UI_Canvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // Mobile portrait
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        Debug.Log("[UIHierarchySetup] Created main canvas");
        return canvas;
    }

    // ───────────────────────────────────────────────────────────────────
    // Top Bar
    // ───────────────────────────────────────────────────────────────────

    private GameObject CreateTopBar(Transform parent)
    {
        GameObject topBar = CreateUIObject("TopBar", parent);
        RectTransform rt = topBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 120);
        rt.anchoredPosition = Vector2.zero;

        // Background
        Image bg = topBar.AddComponent<Image>();
        bg.color = primaryGreen;

        // Logo (placeholder text for now)
        GameObject logo = CreateText("Logo", topBar.transform, "MT. SAMAT AR");
        RectTransform logoRT = logo.GetComponent<RectTransform>();
        logoRT.anchorMin = new Vector2(0, 0.5f);
        logoRT.anchorMax = new Vector2(0, 0.5f);
        logoRT.pivot = new Vector2(0, 0.5f);
        logoRT.anchoredPosition = new Vector2(30, 0);
        logoRT.sizeDelta = new Vector2(300, 60);

        TextMeshProUGUI logoText = logo.GetComponent<TextMeshProUGUI>();
        logoText.fontSize = 24;
        logoText.fontStyle = FontStyles.Bold;
        logoText.color = textLight;
        logoText.alignment = TextAlignmentOptions.MidlineLeft;

        // Token Counter
        GameObject tokenCounter = CreateText("TokenCounter", topBar.transform, "0/19");
        RectTransform tokenRT = tokenCounter.GetComponent<RectTransform>();
        tokenRT.anchorMin = new Vector2(1, 0.5f);
        tokenRT.anchorMax = new Vector2(1, 0.5f);
        tokenRT.pivot = new Vector2(1, 0.5f);
        tokenRT.anchoredPosition = new Vector2(-30, 0);
        tokenRT.sizeDelta = new Vector2(150, 60);

        TextMeshProUGUI tokenText = tokenCounter.GetComponent<TextMeshProUGUI>();
        tokenText.fontSize = 28;
        tokenText.fontStyle = FontStyles.Bold;
        tokenText.color = textLight;
        tokenText.alignment = TextAlignmentOptions.MidlineRight;

        Debug.Log("[UIHierarchySetup] Created top bar");
        return topBar;
    }

    // ───────────────────────────────────────────────────────────────────
    // Screens Container
    // ───────────────────────────────────────────────────────────────────

    private GameObject CreateScreensContainer(Transform parent)
    {
        GameObject container = CreateUIObject("Screens", parent);
        RectTransform rt = container.GetComponent<RectTransform>();

        // Fill space between top bar and bottom nav
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(0, 160); // Bottom offset for nav bar
        rt.offsetMax = new Vector2(0, -120); // Top offset for top bar

        Debug.Log("[UIHierarchySetup] Created screens container");
        return container;
    }

    // ───────────────────────────────────────────────────────────────────
    // Individual Screens
    // ───────────────────────────────────────────────────────────────────

    private void CreateSoldierInventoryScreen(Transform parent)
    {
        GameObject screen = CreateUIObject("SoldierInventoryScreen", parent);
        SetFullScreenRect(screen);
        screen.SetActive(false); // Hidden by default

        // Add the actual script component
        SoldierInventoryScreen script = screen.AddComponent<SoldierInventoryScreen>();

        // Create ScrollView for content
        GameObject scrollView = CreateScrollView("ScrollView", screen.transform);

        // Content container inside ScrollView
        GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;

        // Mission Progress Card
        GameObject progressCard = CreateUIObject("MissionProgressCard", content.transform);
        RectTransform progressRT = progressCard.GetComponent<RectTransform>();
        progressRT.anchorMin = new Vector2(0, 1);
        progressRT.anchorMax = new Vector2(1, 1);
        progressRT.pivot = new Vector2(0.5f, 1);
        progressRT.anchoredPosition = new Vector2(0, -20);
        progressRT.sizeDelta = new Vector2(-40, 150);

        Image progressBG = progressCard.AddComponent<Image>();
        progressBG.color = Color.white;

        script.missionProgressTitle = CreateText("Title", progressCard.transform, "Mission Progress").GetComponent<TextMeshProUGUI>();
        script.missionProgressDescription = CreateText("Description", progressCard.transform, "Collect artifacts...").GetComponent<TextMeshProUGUI>();
        script.missionProgressPercent = CreateText("Percent", progressCard.transform, "0%").GetComponent<TextMeshProUGUI>();

        // Progress bar
        GameObject barBG = CreateUIObject("ProgressBarBG", progressCard.transform);
        RectTransform barRT = barBG.GetComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0.1f, 0.2f);
        barRT.anchorMax = new Vector2(0.9f, 0.3f);
        barRT.sizeDelta = Vector2.zero;
        Image barBGImg = barBG.AddComponent<Image>();
        barBGImg.color = new Color(0.8f, 0.8f, 0.8f);

        GameObject barFill = CreateUIObject("ProgressBarFill", barBG.transform);
        RectTransform fillRT = barFill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.pivot = new Vector2(0, 0.5f);
        fillRT.sizeDelta = Vector2.zero;
        script.missionProgressBar = barFill.AddComponent<Image>();
        script.missionProgressBar.color = primaryGreen;
        script.missionProgressBar.type = Image.Type.Filled;
        script.missionProgressBar.fillMethod = Image.FillMethod.Horizontal;

        // Soldier Card
        GameObject soldierCard = CreateUIObject("SoldierCard", content.transform);
        RectTransform soldierRT = soldierCard.GetComponent<RectTransform>();
        soldierRT.anchorMin = new Vector2(0, 1);
        soldierRT.anchorMax = new Vector2(1, 1);
        soldierRT.pivot = new Vector2(0.5f, 1);
        soldierRT.anchoredPosition = new Vector2(0, -190);
        soldierRT.sizeDelta = new Vector2(-40, 200);

        Image soldierBG = soldierCard.AddComponent<Image>();
        soldierBG.color = Color.white;

        script.soldierBadgeText = CreateText("BadgeText", soldierCard.transform, "PHILIPPINE WORK").GetComponent<TextMeshProUGUI>();
        script.soldierInventoryStatus = CreateText("StatusText", soldierCard.transform, "INVENTORY STATUS").GetComponent<TextMeshProUGUI>();
        script.soldierNameText = CreateText("NameText", soldierCard.transform, "Filipino Soldier").GetComponent<TextMeshProUGUI>();

        // Emblem placeholder
        GameObject emblem = CreateUIObject("EmblemImage", soldierCard.transform);
        RectTransform emblemRT = emblem.GetComponent<RectTransform>();
        emblemRT.anchorMin = new Vector2(0.5f, 0.5f);
        emblemRT.anchorMax = new Vector2(0.5f, 0.5f);
        emblemRT.sizeDelta = new Vector2(100, 100);
        script.soldierEmblemImage = emblem.AddComponent<Image>();
        script.soldierEmblemImage.color = new Color(0.7f, 0.7f, 0.7f);

        // Collected Artifacts Section
        GameObject artifactsSection = CreateUIObject("CollectedArtifactsSection", content.transform);
        RectTransform artifactsRT = artifactsSection.GetComponent<RectTransform>();
        artifactsRT.anchorMin = new Vector2(0, 1);
        artifactsRT.anchorMax = new Vector2(1, 1);
        artifactsRT.pivot = new Vector2(0.5f, 1);
        artifactsRT.anchoredPosition = new Vector2(0, -410);
        artifactsRT.sizeDelta = new Vector2(-40, 600);

        script.collectedArtifactsTitle = CreateText("Title", artifactsSection.transform, "Collected Artifacts").GetComponent<TextMeshProUGUI>();

        // Artifacts Container - THIS IS CRITICAL
        GameObject artifactsContainer = CreateUIObject("ArtifactsContainer", artifactsSection.transform);
        RectTransform containerRT = artifactsContainer.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0, 0);
        containerRT.anchorMax = new Vector2(1, 1);
        containerRT.offsetMin = new Vector2(0, 0);
        containerRT.offsetMax = new Vector2(0, -50); // Leave space for title

        VerticalLayoutGroup vlg = artifactsContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        script.artifactsContainer = artifactsContainer.transform;

        Debug.Log("[UIHierarchySetup] Created SoldierInventoryScreen with artifactsContainer");
    }

    private void CreateDivisionsListScreen(Transform parent)
    {
        GameObject screen = CreateUIObject("DivisionsListScreen", parent);
        SetFullScreenRect(screen);
        screen.SetActive(false);

        DivisionsListScreen script = screen.AddComponent<DivisionsListScreen>();

        // Background
        Image bg = screen.AddComponent<Image>();
        bg.color = backgroundCream;

        // ScrollView
        GameObject scrollView = CreateScrollView("ScrollView", screen.transform);
        GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;

        // Header
        script.titleText = CreateText("Title", content.transform, "Philippine Army Divisions").GetComponent<TextMeshProUGUI>();
        script.descriptionText = CreateText("Description", content.transform, "Browse divisions...").GetComponent<TextMeshProUGUI>();

        // Highlights
        GameObject highlights = CreateUIObject("InventoryHighlights", content.transform);
        script.highlightsLabel = CreateText("Label", highlights.transform, "INVENTORY HIGHLIGHTS").GetComponent<TextMeshProUGUI>();
        script.highlightsProgress = CreateText("Progress", highlights.transform, "0/16").GetComponent<TextMeshProUGUI>();

        // Divisions Container - CRITICAL
        GameObject divisionsContainer = CreateUIObject("DivisionsContainer", content.transform);
        RectTransform containerRT = divisionsContainer.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0, 1);
        containerRT.anchorMax = new Vector2(1, 1);
        containerRT.pivot = new Vector2(0.5f, 1);
        containerRT.sizeDelta = new Vector2(-40, 1000);

        VerticalLayoutGroup vlg = divisionsContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;

        script.divisionsContainer = divisionsContainer.transform;

        Debug.Log("[UIHierarchySetup] Created DivisionsListScreen with divisionsContainer");
    }

    private void CreateDivisionDetailScreen(Transform parent)
    {
        GameObject screen = CreateUIObject("DivisionDetailScreen", parent);
        SetFullScreenRect(screen);
        screen.SetActive(false);

        DivisionDetailScreen script = screen.AddComponent<DivisionDetailScreen>();

        // Background
        Image bg = screen.AddComponent<Image>();
        bg.color = backgroundCream;

        // ScrollView
        GameObject scrollView = CreateScrollView("ScrollView", screen.transform);
        GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;

        // Hero Section
        GameObject hero = CreateUIObject("HeroSection", content.transform);
        script.divisionNameText = CreateText("NameText", hero.transform, "21st Division").GetComponent<TextMeshProUGUI>();
        script.divisionMottoText = CreateText("MottoText", hero.transform, "MOUNTAIN WATCHERS").GetComponent<TextMeshProUGUI>();

        GameObject emblem = CreateUIObject("EmblemImage", hero.transform);
        script.divisionEmblemImage = emblem.AddComponent<Image>();
        script.divisionEmblemImage.color = new Color(0.7f, 0.7f, 0.7f);

        // Mission Progress Card
        GameObject progress = CreateUIObject("MissionProgressCard", content.transform);
        script.missionProgressTitle = CreateText("Title", progress.transform, "Mission Progress").GetComponent<TextMeshProUGUI>();
        script.missionProgressDescription = CreateText("Desc", progress.transform, "Finding Lost History").GetComponent<TextMeshProUGUI>();
        script.missionProgressCount = CreateText("Count", progress.transform, "0/6 Artifacts Found").GetComponent<TextMeshProUGUI>();
        script.activeMissionBadge = CreateText("Badge", progress.transform, "Active Mission").GetComponent<TextMeshProUGUI>();

        GameObject barBG = CreateUIObject("ProgressBar", progress.transform);
        script.missionProgressBar = barBG.AddComponent<Image>();
        script.missionProgressBar.type = Image.Type.Filled;
        script.missionProgressBar.fillMethod = Image.FillMethod.Horizontal;
        script.missionProgressBar.color = primaryGreen;

        // Artifact Collection Section
        script.artifactCollectionTitle = CreateText("ArtifactCollectionTitle", content.transform, "Artifact Collection").GetComponent<TextMeshProUGUI>();

        // Artifacts Grid Container - CRITICAL
        GameObject gridContainer = CreateUIObject("ArtifactsGridContainer", content.transform);
        RectTransform gridRT = gridContainer.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0, 1);
        gridRT.anchorMax = new Vector2(1, 1);
        gridRT.pivot = new Vector2(0.5f, 1);
        gridRT.sizeDelta = new Vector2(-40, 800);

        GridLayoutGroup grid = gridContainer.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(500, 120);
        grid.spacing = new Vector2(20, 20);
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2; // 2-column grid

        script.artifactsGridContainer = gridContainer.transform;

        // Historical Log
        script.historicalLogTitle = CreateText("HistoricalLogTitle", content.transform, "Historical Log").GetComponent<TextMeshProUGUI>();
        script.historicalLogText = CreateText("HistoricalLogText", content.transform, "Division history...").GetComponent<TextMeshProUGUI>();

        Debug.Log("[UIHierarchySetup] Created DivisionDetailScreen with artifactsGridContainer");
    }

    private void CreateProfileScreen(Transform parent)
    {
        GameObject screen = CreateUIObject("ProfileScreen", parent);
        SetFullScreenRect(screen);
        screen.SetActive(false);

        ProfileScreen script = screen.AddComponent<ProfileScreen>();

        // Background
        Image bg = screen.AddComponent<Image>();
        bg.color = backgroundCream;

        // ScrollView
        GameObject scrollView = CreateScrollView("ScrollView", screen.transform);
        GameObject content = scrollView.transform.Find("Viewport/Content").gameObject;

        // User Profile Section
        GameObject profile = CreateUIObject("UserProfile", content.transform);

        GameObject avatar = CreateUIObject("Avatar", profile.transform);
        script.userAvatarImage = avatar.AddComponent<Image>();
        script.userAvatarImage.color = new Color(0.5f, 0.5f, 0.5f);

        script.userLevelBadge = CreateText("LevelBadge", profile.transform, "LVL 1").GetComponent<TextMeshProUGUI>();
        script.userNameText = CreateText("NameText", profile.transform, "Player Name").GetComponent<TextMeshProUGUI>();
        script.userTitleText = CreateText("TitleText", profile.transform, "VANGUARD SCOUT").GetComponent<TextMeshProUGUI>();
        script.userAffiliationText = CreateText("Affiliation", profile.transform, "Bataan Division").GetComponent<TextMeshProUGUI>();

        // Stats Cards
        GameObject stats = CreateUIObject("StatsCards", content.transform);

        GameObject artifactsCard = CreateUIObject("ArtifactsCard", stats.transform);
        script.artifactsFoundCount = CreateText("Count", artifactsCard.transform, "0").GetComponent<TextMeshProUGUI>();
        script.artifactsFoundLabel = CreateText("Label", artifactsCard.transform, "ARTIFACTS FOUND").GetComponent<TextMeshProUGUI>();

        GameObject divisionsCard = CreateUIObject("DivisionsCard", stats.transform);
        script.divisionsCompletedCount = CreateText("Count", divisionsCard.transform, "0").GetComponent<TextMeshProUGUI>();
        script.divisionsCompletedLabel = CreateText("Label", divisionsCard.transform, "DIVISIONS COMPLETED").GetComponent<TextMeshProUGUI>();

        GameObject tokensCard = CreateUIObject("TokensCard", stats.transform);
        script.tokensEarnedCount = CreateText("Count", tokensCard.transform, "0").GetComponent<TextMeshProUGUI>();
        script.tokensEarnedLabel = CreateText("Label", tokensCard.transform, "TOKENS EARNED").GetComponent<TextMeshProUGUI>();

        // Recent Achievements
        script.achievementsTitle = CreateText("AchievementsTitle", content.transform, "Recent Achievements").GetComponent<TextMeshProUGUI>();

        GameObject viewGalleryBtn = CreateButton("ViewGalleryButton", content.transform, "View Gallery");
        script.viewGalleryButton = viewGalleryBtn.GetComponentInChildren<TextMeshProUGUI>();

        // Achievements Container - CRITICAL
        GameObject achievementsContainer = CreateUIObject("AchievementsContainer", content.transform);
        RectTransform achRT = achievementsContainer.GetComponent<RectTransform>();
        achRT.anchorMin = new Vector2(0, 1);
        achRT.anchorMax = new Vector2(1, 1);
        achRT.pivot = new Vector2(0.5f, 1);
        achRT.sizeDelta = new Vector2(-40, 400);

        VerticalLayoutGroup vlg = achievementsContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;

        script.achievementsContainer = achievementsContainer.transform;

        // Active Mission Card
        GameObject mission = CreateUIObject("ActiveMissionCard", content.transform);
        script.activeMissionBadge = CreateText("Badge", mission.transform, "ACTIVE MISSION").GetComponent<TextMeshProUGUI>();
        script.activeMissionTitle = CreateText("Title", mission.transform, "The Echoes of Bataan").GetComponent<TextMeshProUGUI>();
        script.activeMissionProgress = CreateText("Progress", mission.transform, "Progress: 0/50").GetComponent<TextMeshProUGUI>();

        GameObject resumeBtn = CreateButton("ResumeButton", mission.transform, "Resume Journey");
        script.resumeJourneyButton = resumeBtn.GetComponent<Button>();

        Debug.Log("[UIHierarchySetup] Created ProfileScreen with achievementsContainer");
    }

    private void CreateCameraScreen(Transform parent)
    {
        GameObject screen = CreateUIObject("CameraScreen", parent);
        SetFullScreenRect(screen);
        screen.SetActive(true); // Active by default for AR testing

        // This screen is mostly transparent - just shows AR camera passthrough
        // Add a background panel for the label
        GameObject labelBG = CreateUIObject("CameraLabelBG", screen.transform);
        RectTransform labelBGRT = labelBG.GetComponent<RectTransform>();
        labelBGRT.anchorMin = new Vector2(0, 1);
        labelBGRT.anchorMax = new Vector2(1, 1);
        labelBGRT.pivot = new Vector2(0.5f, 1);
        labelBGRT.anchoredPosition = new Vector2(0, -140);
        labelBGRT.sizeDelta = new Vector2(-40, 60);

        // Semi-transparent background for visibility
        Image bg = labelBG.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);

        // Label text as child of background
        GameObject label = CreateText("CameraLabel", labelBG.transform, "AR CAMERA VIEW - Point at exhibits");
        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
        labelText.fontSize = 18;
        labelText.color = textLight;
        labelText.alignment = TextAlignmentOptions.Center;

        Debug.Log("[UIHierarchySetup] Created CameraScreen (AR view)");
    }

    // ───────────────────────────────────────────────────────────────────
    // Bottom Navigation Bar
    // ───────────────────────────────────────────────────────────────────

    private GameObject CreateBottomNavBar(Transform parent)
    {
        GameObject navBar = CreateUIObject("BottomNavBar", parent);
        RectTransform rt = navBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.sizeDelta = new Vector2(0, 160);
        rt.anchoredPosition = Vector2.zero;

        // Background
        Image bg = navBar.AddComponent<Image>();
        bg.color = Color.white;

        // Horizontal layout for 6 tabs
        HorizontalLayoutGroup hlg = navBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.padding = new RectOffset(20, 20, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // Create 6 tab buttons
        CreateNavTab(navBar.transform, "About", "📖");
        CreateNavTab(navBar.transform, "Soldier", "⚔️");
        CreateNavTab(navBar.transform, "Home", "🏠");
        CreateNavTab(navBar.transform, "Camera", "📷"); // Center, main tab
        CreateNavTab(navBar.transform, "Emblem", "🛡️");
        CreateNavTab(navBar.transform, "Profile", "👤");

        Debug.Log("[UIHierarchySetup] Created bottom nav bar with 6 tabs");
        return navBar;
    }

    private void CreateNavTab(Transform parent, string tabName, string icon)
    {
        GameObject tab = CreateButton($"{tabName}Tab", parent, $"{icon}\n{tabName}");

        TextMeshProUGUI text = tab.GetComponentInChildren<TextMeshProUGUI>();
        text.fontSize = 14;
        text.alignment = TextAlignmentOptions.Center;

        // Special styling for Camera tab (center)
        if (tabName == "Camera")
        {
            Image img = tab.GetComponent<Image>();
            img.color = primaryGreen;
            text.color = textLight;
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // Debug Panel (for AR testing)
    // ───────────────────────────────────────────────────────────────────

    private GameObject CreateDebugPanel(Transform parent)
    {
        GameObject panel = CreateUIObject("AR_DebugPanel", parent);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 170);
        rt.sizeDelta = new Vector2(-40, 200);

        // Semi-transparent dark background
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);

        // Debug info text
        GameObject debugText = CreateText("DebugInfo", panel.transform,
            "AR DEBUG INFO:\n" +
            "GPS: 0.000000, 0.000000\n" +
            "Tracking State: Not Tracking\n" +
            "Spawned Objects: 0\n" +
            "Tracked Images: 0");

        RectTransform debugRT = debugText.GetComponent<RectTransform>();
        debugRT.anchorMin = Vector2.zero;
        debugRT.anchorMax = Vector2.one;
        debugRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI debugTMP = debugText.GetComponent<TextMeshProUGUI>();
        debugTMP.fontSize = 14;
        debugTMP.color = Color.green;
        debugTMP.alignment = TextAlignmentOptions.TopLeft;
        debugTMP.margin = new Vector4(10, 10, 10, 10);

        Debug.Log("[UIHierarchySetup] Created AR debug panel");
        return panel;
    }

    // ───────────────────────────────────────────────────────────────────
    // Helper Methods
    // ───────────────────────────────────────────────────────────────────

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        return obj;
    }

    private GameObject CreateText(string name, Transform parent, string text)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = textDark;
        tmp.alignment = TextAlignmentOptions.Center;
        return obj;
    }

    private GameObject CreateButton(string name, Transform parent, string text)
    {
        GameObject btnObj = CreateUIObject(name, parent);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f);

        Button btn = btnObj.AddComponent<Button>();

        GameObject textObj = CreateText("Text", btnObj.transform, text);
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        return btnObj;
    }

    private GameObject CreateScrollView(string name, Transform parent)
    {
        GameObject scrollView = CreateUIObject(name, parent);
        SetFullScreenRect(scrollView);

        // Scroll Rect component
        ScrollRect sr = scrollView.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.sizeDelta = Vector2.zero;

        Image viewportMask = viewport.AddComponent<Image>();
        viewportMask.color = new Color(1, 1, 1, 0.01f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 2000); // Tall scrollable content

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Wire up ScrollRect
        sr.content = contentRT;
        sr.viewport = viewportRT;
        sr.horizontal = false;
        sr.vertical = true;

        return scrollView;
    }

    private void SetFullScreenRect(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private void CreateEventSystem()
    {
        // Check if EventSystem already exists
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            Debug.Log("[UIHierarchySetup] EventSystem already exists");
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        Debug.Log("[UIHierarchySetup] Created EventSystem");
    }

    // ───────────────────────────────────────────────────────────────────
    // About Screen
    // ───────────────────────────────────────────────────────────────────

    private void CreateAboutScreen(Transform parent)
    {
        GameObject screen = new GameObject("AboutScreen");
        screen.transform.SetParent(parent, false);
        RectTransform screenRT = screen.AddComponent<RectTransform>();
        SetFullScreenRect(screen);

        // Add AboutScreen component
        screen.AddComponent<AboutScreen>();

        // Create ScrollView
        GameObject scrollView = CreateScrollView("AboutScrollView", screen.transform);

        // Get content area
        Transform content = scrollView.transform.Find("Viewport/Content");

        // App Header Section
        GameObject header = CreateTextElement(content, "HeaderSection", "MT. SAMAT AR", 32, FontStyles.Bold);
        AddVerticalSpacing(header.transform, 30);

        GameObject version = CreateTextElement(content, "VersionText", "Version 1.0.0", 18, FontStyles.Normal);
        AddVerticalSpacing(version.transform, 20);

        // Shrine Description
        GameObject shrineTitle = CreateTextElement(content, "ShrineTitleText", "Mt. Samat National Shrine", 24, FontStyles.Bold);
        AddVerticalSpacing(shrineTitle.transform, 10);

        GameObject shrineDesc = CreateTextElement(content, "ShrineDescText",
            "Historical shrine honoring WWII heroes.", 16, FontStyles.Normal);
        AddVerticalSpacing(shrineDesc.transform, 20);

        // Mission
        GameObject missionTitle = CreateTextElement(content, "MissionTitleText", "About This Project", 24, FontStyles.Bold);
        AddVerticalSpacing(missionTitle.transform, 10);

        GameObject missionDesc = CreateTextElement(content, "MissionDescText",
            "AR scavenger hunt to educate visitors.", 16, FontStyles.Normal);
        AddVerticalSpacing(missionDesc.transform, 20);

        // Credits
        GameObject creditsTitle = CreateTextElement(content, "CreditsTitleText", "Credits", 24, FontStyles.Bold);
        AddVerticalSpacing(creditsTitle.transform, 10);

        GameObject credits = CreateTextElement(content, "CreditsText",
            "Terra App · AFP Partnership", 16, FontStyles.Normal);
        AddVerticalSpacing(credits.transform, 20);

        // Legal
        GameObject legalTitle = CreateTextElement(content, "LegalTitleText", "Legal", 24, FontStyles.Bold);
        AddVerticalSpacing(legalTitle.transform, 10);

        GameObject legal = CreateTextElement(content, "LegalText",
            "Privacy policy · Camera & GPS permissions", 16, FontStyles.Normal);

        screen.SetActive(false);
        Debug.Log("[UIHierarchySetup] Created AboutScreen");
    }

    // ───────────────────────────────────────────────────────────────────
    // Home Screen
    // ───────────────────────────────────────────────────────────────────

    private void CreateHomeScreen(Transform parent)
    {
        GameObject screen = new GameObject("HomeScreen");
        screen.transform.SetParent(parent, false);
        RectTransform screenRT = screen.AddComponent<RectTransform>();
        SetFullScreenRect(screen);

        // Add HomeScreen component
        screen.AddComponent<HomeScreen>();

        // Create ScrollView
        GameObject scrollView = CreateScrollView("HomeScrollView", screen.transform);

        // Get content area
        Transform content = scrollView.transform.Find("Viewport/Content");

        // Welcome Section
        GameObject welcome = CreateTextElement(content, "WelcomeText",
            "Welcome to Mt. Samat AR", 28, FontStyles.Bold);
        AddVerticalSpacing(welcome.transform, 10);

        GameObject playerName = CreateTextElement(content, "PlayerNameText",
            "Hello, Visitor!", 20, FontStyles.Normal);
        AddVerticalSpacing(playerName.transform, 30);

        // How to Play Section
        GameObject howToTitle = CreateTextElement(content, "HowToPlayTitle",
            "How to Play", 24, FontStyles.Bold);
        AddVerticalSpacing(howToTitle.transform, 15);

        GameObject howToText = CreateTextElement(content, "HowToPlayText",
            "Point your camera at exhibits or walk to GPS locations to discover artifacts!",
            16, FontStyles.Normal);
        AddVerticalSpacing(howToText.transform, 30);

        // Quick Stats
        GameObject statsText = CreateTextElement(content, "QuickStatsText",
            "Artifacts: 0 | Badges: 0/19", 18, FontStyles.Bold);
        AddVerticalSpacing(statsText.transform, 30);

        // Start AR Button
        GameObject startButton = CreateButton(content, "StartARButton", "Start AR Mode", primaryGreen);
        AddVerticalSpacing(startButton.transform, 15);

        // View Progress Button
        GameObject progressButton = CreateButton(content, "ViewProgressButton", "View Progress", primaryGreen);

        screen.SetActive(false);
        Debug.Log("[UIHierarchySetup] Created HomeScreen");
    }

    private void AddVerticalSpacing(Transform element, float spacing)
    {
        LayoutElement le = element.gameObject.GetComponent<LayoutElement>();
        if (le == null)
        {
            le = element.gameObject.AddComponent<LayoutElement>();
        }
        le.preferredHeight = spacing;
    }

    private GameObject CreateTextElement(Transform parent, string name, string text, int fontSize, FontStyles fontStyle)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.color = textDark;
        tmp.alignment = TextAlignmentOptions.Left;

        RectTransform rt = textObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, fontSize * 2);

        return textObj;
    }

    private GameObject CreateButton(Transform parent, string name, string buttonText, Color buttonColor)
    {
        GameObject button = new GameObject(name);
        button.transform.SetParent(parent, false);

        RectTransform rt = button.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 50);

        Image img = button.AddComponent<Image>();
        img.color = buttonColor;

        Button btn = button.AddComponent<Button>();

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(button.transform, false);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = buttonText;
        text.fontSize = 18;
        text.color = textLight;
        text.alignment = TextAlignmentOptions.Center;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        return button;
    }
}