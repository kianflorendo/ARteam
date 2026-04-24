using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Profile Screen - matches Terra Figma design: user_profile_achievements
/// Shows:
/// - User avatar with level badge
/// - User name and title (VANGUARD SCOUT)
/// - Bataan Division affiliation
/// - Stats: 42 Artifacts Found, 08 Divisions Completed, 1,240 Tokens Earned
/// - Recent Achievements list (badges with completion status and dates)
/// - Active Mission card at bottom
/// </summary>
public class ProfileScreen : MonoBehaviour
{
    [Header("User Profile")]
    public Image userAvatarImage;
    public TextMeshProUGUI userLevelBadge;          // "LVL 24"
    public TextMeshProUGUI userNameText;            // "Mateo Santos"
    public TextMeshProUGUI userTitleText;           // "VANGUARD SCOUT"
    public TextMeshProUGUI userAffiliationText;     // "Bataan Division"

    [Header("Stats Cards")]
    public TextMeshProUGUI artifactsFoundCount;     // "42"
    public TextMeshProUGUI artifactsFoundLabel;     // "ARTIFACTS FOUND"
    public TextMeshProUGUI divisionsCompletedCount; // "08"
    public TextMeshProUGUI divisionsCompletedLabel; // "DIVISIONS COMPLETED"
    public TextMeshProUGUI tokensEarnedCount;       // "1,240"
    public TextMeshProUGUI tokensEarnedLabel;       // "TOKENS EARNED"

    [Header("Recent Achievements")]
    public TextMeshProUGUI achievementsTitle;       // "Recent Achievements"
    public TextMeshProUGUI viewGalleryButton;       // "View Gallery"
    public Transform achievementsContainer;         // Parent for achievement cards
    public GameObject achievementCardPrefab;        // Prefab for each achievement

    [Header("Active Mission Card")]
    public TextMeshProUGUI activeMissionBadge;      // "ACTIVE MISSION"
    public TextMeshProUGUI activeMissionTitle;      // "The Echoes of Bataan"
    public TextMeshProUGUI activeMissionProgress;   // "Progress: 6/9 Artifacts Found"
    public Button resumeJourneyButton;              // "Resume Journey"

    // ───────────────────────────────────────────────────────────────────
    // Lifecycle
    // ───────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        PopulateScreen();
    }

    // ───────────────────────────────────────────────────────────────────
    // Screen Population
    // ───────────────────────────────────────────────────────────────────

    public void PopulateScreen()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[ProfileScreen] InventoryManager not ready");
            return;
        }

        // User Profile
        if (userLevelBadge != null)
            userLevelBadge.text = $"LVL {InventoryManager.Instance.GetLevel()}";

        if (userNameText != null)
            userNameText.text = InventoryManager.Instance.GetPlayerName();

        if (userTitleText != null)
            userTitleText.text = "VANGUARD SCOUT"; // TODO: Get from player progression system

        if (userAffiliationText != null)
            userAffiliationText.text = "Bataan Division"; // TODO: Get from player data

        // Stats Cards
        int artifactsCount = InventoryManager.Instance.GetAllBadges().Count; // Using badges as proxy for collected artifacts

        int divisionsCount = 0;
        var allDivisions = ManifestLoader.Instance?.GetAllDivisions();
        if (allDivisions != null)
        {
            foreach (var division in allDivisions)
            {
                var progress = InventoryManager.Instance.GetDivisionProgress(division.id);
                if (progress.completed)
                    divisionsCount++;
            }
        }

        if (artifactsFoundCount != null)
            artifactsFoundCount.text = artifactsCount.ToString();

        if (artifactsFoundLabel != null)
            artifactsFoundLabel.text = "ARTIFACTS FOUND";

        if (divisionsCompletedCount != null)
            divisionsCompletedCount.text = divisionsCount.ToString("D2"); // "08" format

        if (divisionsCompletedLabel != null)
            divisionsCompletedLabel.text = "DIVISIONS COMPLETED";

        if (tokensEarnedCount != null)
            tokensEarnedCount.text = InventoryManager.Instance.GetTokenCount().ToString("N0"); // "1,240" format

        if (tokensEarnedLabel != null)
            tokensEarnedLabel.text = "TOKENS EARNED";

        // Recent Achievements
        if (achievementsTitle != null)
            achievementsTitle.text = "Recent Achievements";

        if (viewGalleryButton != null)
            viewGalleryButton.text = "View Gallery";

        PopulateAchievements();

        // Active Mission Card
        if (activeMissionBadge != null)
            activeMissionBadge.text = "ACTIVE MISSION";

        if (activeMissionTitle != null)
            activeMissionTitle.text = "The Echoes of Bataan";

        if (ManifestLoader.Instance != null)
        {
            // TODO: Get total artifacts count - for now using a placeholder
            int totalArtifacts = 50; // Placeholder
            if (activeMissionProgress != null)
                activeMissionProgress.text = $"Progress: {artifactsCount}/{totalArtifacts} Artifacts Found";
        }

        Debug.Log($"[ProfileScreen] Populated profile - {artifactsCount} artifacts, {divisionsCount} divisions, {InventoryManager.Instance.GetTokenCount()} tokens");
    }

    private void PopulateAchievements()
    {
        // Clear existing cards
        foreach (Transform child in achievementsContainer)
        {
            Destroy(child.gameObject);
        }

        var badges = InventoryManager.Instance.GetAllBadges();

        // Show recent badges (limit to 3 most recent)
        int count = 0;
        foreach (var badge in badges)
        {
            if (count >= 3) break;

            CreateAchievementCard(badge);
            count++;
        }
    }

    private void CreateAchievementCard(AFPTokenBadge badge)
    {
        if (achievementCardPrefab == null) return;

        GameObject card = Instantiate(achievementCardPrefab, achievementsContainer);
        var cardComponent = card.GetComponent<AchievementCard>();
        if (cardComponent != null)
        {
            cardComponent.Setup(badge);
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // Button Callbacks
    // ───────────────────────────────────────────────────────────────────

    public void OnResumeJourneyClicked()
    {
        AudioManager.Instance?.PlayUITapSFX();
        // TODO: Navigate to Home/Camera screen
        Debug.Log("[ProfileScreen] Resume Journey clicked");
    }

    public void OnViewGalleryClicked()
    {
        AudioManager.Instance?.PlayUITapSFX();
        // TODO: Navigate to full badges gallery
        Debug.Log("[ProfileScreen] View Gallery clicked");
    }
}