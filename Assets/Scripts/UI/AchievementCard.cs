using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Achievement card used in Profile Screen
/// Shows: badge icon, title, description, completion status, completion date
/// Matches Terra Figma design: horizontal card with badge icon on left
/// States: COMPLETED (green badge), IN PROGRESS (gray badge with percentage)
/// </summary>
public class AchievementCard : MonoBehaviour
{
    [Header("UI Elements")]
    public Image badgeIconImage;
    public TextMeshProUGUI badgeTitleText;          // "The Valor Trail"
    public TextMeshProUGUI badgeDescriptionText;    // "Reached the Shrine of Valor at sunrise."
    public TextMeshProUGUI statusBadgeText;         // "COMPLETED" or "IN PROGRESS"
    public TextMeshProUGUI completionDateText;      // "Oct 12, 2023" or "80% Done"

    [Header("Card States")]
    public GameObject completedBadge;               // Green "COMPLETED" badge
    public GameObject inProgressBadge;              // Gray badge with percentage

    private AFPTokenBadge _badge;

    public void Setup(AFPTokenBadge badge)
    {
        _badge = badge;

        if (badgeTitleText != null)
            badgeTitleText.text = badge.badge_name;

        if (badgeDescriptionText != null)
            badgeDescriptionText.text = badge.badge_description;

        bool isCompleted = (badge.status == BadgeStatus.Approved || badge.status == BadgeStatus.Issued);

        if (completedBadge != null)
            completedBadge.SetActive(isCompleted);

        if (inProgressBadge != null)
            inProgressBadge.SetActive(!isCompleted);

        if (statusBadgeText != null)
        {
            statusBadgeText.text = isCompleted ? "COMPLETED" : "IN PROGRESS";
        }

        if (completionDateText != null)
        {
            if (isCompleted && !string.IsNullOrEmpty(badge.approved_at))
            {
                // Format date from ISO8601 to "Oct 12, 2023"
                if (System.DateTime.TryParse(badge.approved_at, out System.DateTime date))
                {
                    completionDateText.text = date.ToString("MMM dd, yyyy");
                }
                else
                {
                    completionDateText.text = badge.approved_at;
                }
            }
            else
            {
                completionDateText.text = "In Progress"; // TODO: Show percentage when progress tracking is implemented
            }
        }

        // TODO: Load badgeIconImage from Addressables using badge.badge_bundle_key
    }

    public void OnCardClicked()
    {
        AudioManager.Instance?.PlayUITapSFX();
        Debug.Log($"[AchievementCard] Clicked: {_badge.badge_id}");
        // TODO: Show achievement detail popup
    }
}