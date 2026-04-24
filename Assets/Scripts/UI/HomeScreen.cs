using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Home screen - Welcome message and How to Play instructions.
/// Phase 10 - Navigation + Profile
/// </summary>
public class HomeScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI howToPlayText;
    [SerializeField] private TextMeshProUGUI quickStatsText;

    [Header("Action Buttons")]
    [SerializeField] private Button startARButton;
    [SerializeField] private Button viewProgressButton;

    [Header("Welcome Image")]
    [SerializeField] private Image heroImage;

    // ────────────────────────────────────────────────────────────────────────
    // Initialization
    // ────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        PopulateHomeScreen();

        // Wire up buttons
        if (startARButton != null)
        {
            startARButton.onClick.AddListener(StartARMode);
        }

        if (viewProgressButton != null)
        {
            viewProgressButton.onClick.AddListener(ViewProgress);
        }

        Debug.Log("[HomeScreen] Initialized");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Content Population
    // ────────────────────────────────────────────────────────────────────────

    private void PopulateHomeScreen()
    {
        // Welcome message
        if (welcomeText != null)
        {
            welcomeText.text = "Welcome to Mt. Samat AR";
        }

        // Player name (from inventory if available)
        if (playerNameText != null)
        {
            playerNameText.text = "Hello, Visitor!";
        }

        // How to Play instructions
        if (howToPlayText != null)
        {
            howToPlayText.text = GetHowToPlayText();
        }

        // Quick stats
        if (quickStatsText != null)
        {
            quickStatsText.text = GetQuickStats();
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // How to Play Text
    // ────────────────────────────────────────────────────────────────────────

    private string GetHowToPlayText()
    {
        return "How to Play:\n\n" +
               "📷 Image Tracking Mode\n" +
               "Point your camera at exhibits, emblems, weapons, and artifacts around the shrine. " +
               "When detected, a 3D model and information scroll will appear.\n\n" +
               "🗺️ GPS Mode\n" +
               "On your first run, the app saves your starting GPS location offline. " +
               "From there, walk the required route distance to unlock the next artifact. " +
               "The Bolo Knife appears after 1 meter, the Mortar after 5 more meters, and the Thompson after 10 more meters. " +
               "Each unlocked artifact appears locally within about 1 meter of you in AR.\n\n" +
               "⚔️ Collect Artifacts\n" +
               "Tap the 'Collect' button on collectible artifacts to add them to your inventory. " +
               "Each artifact belongs to a soldier and a division.\n\n" +
               "🏆 Earn Badges\n" +
               "Complete soldier sets (Filipino, Japanese, American) or division sets to earn " +
               "AFP Token Badges. Collect all 19 badges to complete the experience!\n\n" +
               "Tap the Camera tab below to start exploring!";
    }

    // ────────────────────────────────────────────────────────────────────────
    // Quick Stats
    // ────────────────────────────────────────────────────────────────────────

    private string GetQuickStats()
    {
        // Simple hardcoded stats for now - will be dynamic after InventoryManager is updated
        return "Artifacts: 0 | Badges: 0/19";
    }

    // ────────────────────────────────────────────────────────────────────────
    // Button Actions
    // ────────────────────────────────────────────────────────────────────────

    private void StartARMode()
    {
        // Play UI tap sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUITapSFX();
        }

        // Switch to Camera screen via NavigationManager
        NavigationManager navManager = FindAnyObjectByType<NavigationManager>();
        if (navManager != null)
        {
            navManager.ShowCameraScreen();
        }
        else
        {
            Debug.LogWarning("[HomeScreen] NavigationManager not found!");
        }
    }

    private void ViewProgress()
    {
        // Play UI tap sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUITapSFX();
        }

        // Switch to Soldier screen to view progress
        NavigationManager navManager = FindAnyObjectByType<NavigationManager>();
        if (navManager != null)
        {
            navManager.ShowSoldierScreen();
        }
        else
        {
            Debug.LogWarning("[HomeScreen] NavigationManager not found!");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Public Methods
    // ────────────────────────────────────────────────────────────────────────

    public void RefreshStats()
    {
        if (quickStatsText != null)
        {
            quickStatsText.text = GetQuickStats();
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Refresh content when screen becomes active
        PopulateHomeScreen();
    }
}
