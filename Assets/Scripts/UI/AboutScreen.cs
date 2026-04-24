using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// About screen - Static info display (no AR, no network).
/// Phase 10 - About Screen
/// Implementation Plan Section 11 - Phase 10 About Screen Full Definition
/// </summary>
public class AboutScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI appVersionText;
    [SerializeField] private TextMeshProUGUI appTitleText;

    [Header("Content Sections")]
    [SerializeField] private TextMeshProUGUI shrineDescriptionText;
    [SerializeField] private TextMeshProUGUI missionDescriptionText;
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private TextMeshProUGUI legalText;

    [Header("Images")]
    [SerializeField] private Image appLogoImage;
    [SerializeField] private Image afpLogoImage;

    [Header("Privacy Policy")]
    [SerializeField] private Button privacyPolicyButton;
    [SerializeField] private string privacyPolicyURL = "https://terra-app.com/privacy";

    // ────────────────────────────────────────────────────────────────────────
    // Initialization
    // ────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        PopulateAboutScreen();

        // Wire privacy policy button
        if (privacyPolicyButton != null)
        {
            privacyPolicyButton.onClick.AddListener(OpenPrivacyPolicy);
        }

        Debug.Log("[AboutScreen] Initialized");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Content Population
    // ────────────────────────────────────────────────────────────────────────

    private void PopulateAboutScreen()
    {
        // App Version - Runtime from Application.version
        if (appVersionText != null)
        {
            appVersionText.text = $"Version {Application.version}";
        }

        // App Title
        if (appTitleText != null)
        {
            appTitleText.text = "Mt. Samat AR";
        }

        // About the Shrine
        if (shrineDescriptionText != null)
        {
            shrineDescriptionText.text = GetShrineDescription();
        }

        // Our Mission
        if (missionDescriptionText != null)
        {
            missionDescriptionText.text = GetMissionDescription();
        }

        // Credits
        if (creditsText != null)
        {
            creditsText.text = GetCredits();
        }

        // Legal
        if (legalText != null)
        {
            legalText.text = GetLegalText();
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Content Text
    // ────────────────────────────────────────────────────────────────────────

    private string GetShrineDescription()
    {
        return "Mt. Samat National Shrine, also known as Dambana ng Kagitingan (Shrine of Valor), " +
               "stands as a memorial to the Filipino, American, and Japanese soldiers who fought during " +
               "the Battle of Bataan in 1942. This historic site honors the courage and sacrifice of those " +
               "who defended freedom during World War II.";
    }

    private string GetMissionDescription()
    {
        return "This AR scavenger hunt uses augmented reality to bring history to life. " +
               "By pointing your camera at exhibits and following an offline GPS-guided walking route from your starting point, " +
               "you can discover artifacts, learn about the soldiers and divisions of the Battle of Bataan, " +
               "and gain a deeper understanding of this pivotal moment in Philippine history—all without " +
               "any physical changes to the memorial.";
    }

    private string GetCredits()
    {
        return "Development Team\n" +
               "Terra App\n\n" +
               "In Partnership With\n" +
               "Armed Forces of the Philippines (AFP)\n\n" +
               "Historical Advisors\n" +
               "AFP Historical Review Committee\n\n" +
               "Content Reviewers\n" +
               "Mt. Samat National Shrine Staff";
    }

    private string GetLegalText()
    {
        return "Camera permission is used for AR image tracking and scene presentation. " +
               "Location permission is used to save the player's starting point and unlock the offline walking route. " +
               "No personal data is collected or transmitted without your consent.\n\n" +
               "© 2026 Terra App · Built with Unity 6";
    }

    // ────────────────────────────────────────────────────────────────────────
    // Privacy Policy
    // ────────────────────────────────────────────────────────────────────────

    private void OpenPrivacyPolicy()
    {
        // Play UI tap sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUITapSFX();
        }

        // Open URL in browser
        Application.OpenURL(privacyPolicyURL);

        Debug.Log($"[AboutScreen] Opening privacy policy: {privacyPolicyURL}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Public Methods
    // ────────────────────────────────────────────────────────────────────────

    public void RefreshContent()
    {
        PopulateAboutScreen();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Refresh content when screen becomes active
        PopulateAboutScreen();
    }
}
