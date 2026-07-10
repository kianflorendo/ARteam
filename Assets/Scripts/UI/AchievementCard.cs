using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementCard : MonoBehaviour
{
    [Header("UI Elements")]
    public Image badgeIconImage;
    public TextMeshProUGUI badgeTitleText;
    public TextMeshProUGUI badgeDescriptionText;
    public TextMeshProUGUI statusBadgeText;
    public TextMeshProUGUI completionDateText;

    [Header("Card States")]
    public GameObject completedBadge;
    public GameObject inProgressBadge;

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
            statusBadgeText.text = isCompleted ? "COMPLETED" : "IN PROGRESS";

        if (completionDateText != null)
        {
            if (isCompleted && !string.IsNullOrEmpty(badge.approved_at))
            {
                if (System.DateTime.TryParse(badge.approved_at, out System.DateTime date))
                    completionDateText.text = date.ToString("MMM dd, yyyy");
                else
                    completionDateText.text = badge.approved_at;
            }
            else
            {
                completionDateText.text = "In Progress";
            }
        }
    }

    public void OnCardClicked()
    {
        AudioManager.Instance?.PlayUITapSFX();
        Debug.Log($"[AchievementCard] Clicked: {_badge.badge_id}");
    }
}
