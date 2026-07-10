using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance { get; private set; }

    [Header("Groups")]
    [SerializeField] private GameObject _preLoginGroup;
    [SerializeField] private GameObject _mainAppGroup;

    [Header("Pre-Login Screens")]
    [SerializeField] private GameObject _mainMenuScreen;
    [SerializeField] private GameObject _registerScreen;
    [SerializeField] private GameObject _howToPlayScreen;

    [Header("Main App Screens")]
    [SerializeField] private GameObject _topBar;
    [SerializeField] private GameObject _homeScreen;
    [SerializeField] private GameObject _soldierScreen;
    [SerializeField] private GameObject _arScanScreen;
    [SerializeField] private GameObject _awardsScreen;
    [SerializeField] private GameObject _profileScreen;
    [SerializeField] private GameObject _settingsScreen;

    [Header("Debug Panel")]
    [SerializeField] private GameObject _arDebugPanel;

    [Header("AR Action Panels")]
    [SerializeField] private GameObject _artifactActionPanel;
    [SerializeField] private GameObject _collectNotificationBanner;

    [Header("Bottom Nav Bar")]
    [SerializeField] private GameObject _bottomNavBar;

    [Header("Nav Tab Buttons")]
    [SerializeField] private Button _homeTabBtn;
    [SerializeField] private Button _soldierTabBtn;
    [SerializeField] private Button _arScanTabBtn;
    [SerializeField] private Button _awardsTabBtn;
    [SerializeField] private Button _profileTabBtn;

    [Header("Nav Tab Labels")]
    [SerializeField] private TextMeshProUGUI _homeTabLabel;
    [SerializeField] private TextMeshProUGUI _soldierTabLabel;
    [SerializeField] private TextMeshProUGUI _arScanTabLabel;
    [SerializeField] private TextMeshProUGUI _awardsTabLabel;
    [SerializeField] private TextMeshProUGUI _profileTabLabel;

    private static readonly Color TAB_ACTIVE   = new Color(0.102f, 0.102f, 0.102f);
    private static readonly Color TAB_INACTIVE = new Color(0.471f, 0.443f, 0.424f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        WireNavButtons();

        bool registered = PlayerProfileManager.Instance != null
                          && PlayerProfileManager.Instance.IsRegistered;

        if (registered)
            SwitchToMainApp();
        else
            ShowPreLoginScreen("MainMenu");
    }

    private void WireNavButtons()
    {
        _homeTabBtn?.onClick.AddListener(() => ShowScreen("Home"));
        _soldierTabBtn?.onClick.AddListener(() => ShowScreen("Soldier"));
        _arScanTabBtn?.onClick.AddListener(() => ShowScreen("ARScan"));
        _awardsTabBtn?.onClick.AddListener(() => ShowScreen("Awards"));
        _profileTabBtn?.onClick.AddListener(() => ShowScreen("Profile"));
    }

    public void ShowPreLoginScreen(string screenName)
    {
        _preLoginGroup?.SetActive(true);
        _mainAppGroup?.SetActive(false);

        // Camera feed must never show on pre-login screens.
        ARCameraDisplay.SetSuppressed(true);

        if (_arDebugPanel             != null) _arDebugPanel.SetActive(false);
        if (_artifactActionPanel      != null) _artifactActionPanel.SetActive(false);
        if (_collectNotificationBanner != null) _collectNotificationBanner.SetActive(false);

        Show(_mainMenuScreen,  screenName == "MainMenu");
        Show(_registerScreen,  screenName == "Register");
        Show(_howToPlayScreen, screenName == "HowToPlay");

        Debug.Log($"[NavigationManager] PreLogin → {screenName}");
    }

    public void SwitchToMainApp()
    {
        _preLoginGroup?.SetActive(false);
        _mainAppGroup?.SetActive(true);
        ShowScreen("Home");
    }

    public void ShowScreen(string screenName)
    {
        Show(_homeScreen,     screenName == "Home");
        Show(_soldierScreen,  screenName == "Soldier");
        Show(_arScanScreen,   screenName == "ARScan");
        Show(_awardsScreen,   screenName == "Awards");
        Show(_profileScreen,  screenName == "Profile");
        Show(_settingsScreen, screenName == "Settings");

        // Camera feed only on ARScan; suppressed elsewhere.
        ARCameraDisplay.SetSuppressed(screenName != "ARScan");

        if (_arDebugPanel             != null) _arDebugPanel.SetActive(screenName == "ARScan");
        if (_artifactActionPanel      != null) _artifactActionPanel.SetActive(screenName == "ARScan");
        if (_collectNotificationBanner != null) _collectNotificationBanner.SetActive(screenName == "ARScan");

        // TopBar hidden on screens that have their own custom header.
        if (_topBar != null) _topBar.SetActive(
            screenName != "ARScan" &&
            screenName != "Soldier" &&
            screenName != "Settings" &&
            screenName != "Profile");

        SetActiveTab(screenName);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUITapSFX();

        Debug.Log($"[NavigationManager] MainApp → {screenName}");
    }

    private void SetActiveTab(string screenName)
    {
        SetTabColor(_homeTabLabel,    screenName == "Home");
        SetTabColor(_soldierTabLabel, screenName == "Soldier");
        SetTabColor(_arScanTabLabel,  screenName == "ARScan");
        SetTabColor(_awardsTabLabel,  screenName == "Awards");
        SetTabColor(_profileTabLabel, screenName == "Profile");
    }

    private static void SetTabColor(TextMeshProUGUI label, bool active)
    {
        if (label != null) label.color = active ? TAB_ACTIVE : TAB_INACTIVE;
    }

    private static void Show(GameObject go, bool visible)
    {
        if (go != null) go.SetActive(visible);
    }
}
