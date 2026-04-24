using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages bottom navigation bar and screen switching.
/// Phase 10 - Navigation + Profile
/// </summary>
public class NavigationManager : MonoBehaviour
{
    [Header("Screen References")]
    [SerializeField] private GameObject aboutScreen;
    [SerializeField] private GameObject soldierScreen;
    [SerializeField] private GameObject homeScreen;
    [SerializeField] private GameObject cameraScreen;
    [SerializeField] private GameObject emblemScreen; // DivisionsListScreen
    [SerializeField] private GameObject profileScreen;

    [Header("Tab Button References")]
    [SerializeField] private Button aboutTabButton;
    [SerializeField] private Button soldierTabButton;
    [SerializeField] private Button homeTabButton;
    [SerializeField] private Button cameraTabButton;
    [SerializeField] private Button emblemTabButton;
    [SerializeField] private Button profileTabButton;

    [Header("Settings")]
    [SerializeField] private bool startWithCameraView = true;

    private GameObject currentScreen;

    // ────────────────────────────────────────────────────────────────────────
    // Initialization
    // ────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Wire up button listeners
        if (aboutTabButton != null)
            aboutTabButton.onClick.AddListener(() => ShowScreen(aboutScreen));

        if (soldierTabButton != null)
            soldierTabButton.onClick.AddListener(() => ShowScreen(soldierScreen));

        if (homeTabButton != null)
            homeTabButton.onClick.AddListener(() => ShowScreen(homeScreen));

        if (cameraTabButton != null)
            cameraTabButton.onClick.AddListener(() => ShowScreen(cameraScreen));

        if (emblemTabButton != null)
            emblemTabButton.onClick.AddListener(() => ShowScreen(emblemScreen));

        if (profileTabButton != null)
            profileTabButton.onClick.AddListener(() => ShowScreen(profileScreen));

        // Show default screen
        if (startWithCameraView && cameraScreen != null)
        {
            ShowScreen(cameraScreen);
        }
        else if (homeScreen != null)
        {
            ShowScreen(homeScreen);
        }

        Debug.Log("[NavigationManager] Initialized with " + GetActiveTabCount() + " tabs");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Screen Switching
    // ────────────────────────────────────────────────────────────────────────

    private void ShowScreen(GameObject targetScreen)
    {
        if (targetScreen == null)
        {
            Debug.LogWarning("[NavigationManager] Target screen is null!");
            return;
        }

        // Play UI tap sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUITapSFX();
        }

        // Hide all screens
        if (aboutScreen != null) aboutScreen.SetActive(false);
        if (soldierScreen != null) soldierScreen.SetActive(false);
        if (homeScreen != null) homeScreen.SetActive(false);
        if (cameraScreen != null) cameraScreen.SetActive(false);
        if (emblemScreen != null) emblemScreen.SetActive(false);
        if (profileScreen != null) profileScreen.SetActive(false);

        // Show target screen
        targetScreen.SetActive(true);
        currentScreen = targetScreen;

        Debug.Log($"[NavigationManager] Switched to {targetScreen.name}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Public Methods
    // ────────────────────────────────────────────────────────────────────────

    public void ShowAboutScreen() => ShowScreen(aboutScreen);
    public void ShowSoldierScreen() => ShowScreen(soldierScreen);
    public void ShowHomeScreen() => ShowScreen(homeScreen);
    public void ShowCameraScreen() => ShowScreen(cameraScreen);
    public void ShowEmblemScreen() => ShowScreen(emblemScreen);
    public void ShowProfileScreen() => ShowScreen(profileScreen);

    // ────────────────────────────────────────────────────────────────────────
    // Utility
    // ────────────────────────────────────────────────────────────────────────

    private int GetActiveTabCount()
    {
        int count = 0;
        if (aboutTabButton != null) count++;
        if (soldierTabButton != null) count++;
        if (homeTabButton != null) count++;
        if (cameraTabButton != null) count++;
        if (emblemTabButton != null) count++;
        if (profileTabButton != null) count++;
        return count;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-find screens if not assigned
        if (aboutScreen == null)
            aboutScreen = GameObject.Find("AboutScreen");
        if (soldierScreen == null)
            soldierScreen = GameObject.Find("SoldierInventoryScreen");
        if (homeScreen == null)
            homeScreen = GameObject.Find("HomeScreen");
        if (cameraScreen == null)
            cameraScreen = GameObject.Find("CameraScreen");
        if (emblemScreen == null)
            emblemScreen = GameObject.Find("DivisionsListScreen");
        if (profileScreen == null)
            profileScreen = GameObject.Find("ProfileScreen");
    }
#endif
}