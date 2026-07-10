using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AwardsScreenController : MonoBehaviour
{
    [Header("Awards List")]
    [SerializeField] private Transform _listContainer;

    private static readonly Color EARNED_ICON = new Color(0.102f, 0.102f, 0.102f);
    private static readonly Color LOCKED_ICON = new Color(0.973f, 0.973f, 0.973f);

    private void OnEnable() => Refresh();

    private void Refresh()
    {
        if (_listContainer == null || InventoryManager.Instance == null) return;

        var badges = InventoryManager.Instance.GetAllBadges();

        for (int i = 0; i < _listContainer.childCount; i++)
        {
            var row   = _listContainer.GetChild(i);
            bool earned = i < badges.Count;

            var badgeIcon = row.Find("BadgeIcon")?.GetComponent<Image>();
            if (badgeIcon != null)
                badgeIcon.color = earned ? EARNED_ICON : LOCKED_ICON;

            var title = row.Find("InfoCol/AwardTitle")?.GetComponent<TextMeshProUGUI>();
            var desc  = row.Find("InfoCol/AwardDesc")?.GetComponent<TextMeshProUGUI>();

            if (earned && i < badges.Count)
            {
                if (title != null) title.text = badges[i].badge_name;
                if (desc  != null) desc.text  = badges[i].badge_description;
            }
            else
            {
                if (title != null) title.text = "— Locked —";
                if (desc  != null) desc.text  = "Collect more artifacts to unlock.";
            }
        }
    }
}
