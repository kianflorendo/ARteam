using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Division card used in Divisions List Screen
/// Shows: emblem, name, motto, progress (X/6 Artifacts Found), "COMPLETED" badge, chevron
/// Matches Terra Figma design: horizontal card with different states (highlighted, completed, incomplete)
/// </summary>
public class DivisionListCard : MonoBehaviour
{
    [Header("UI Elements")]
    public Image emblemImage;
    public TextMeshProUGUI divisionNameText;        // "21st Division"
    public TextMeshProUGUI divisionMottoText;       // "Victory through Valor"
    public TextMeshProUGUI progressText;            // "6/6 Artifacts Found"
    public GameObject completedBadge;               // "COMPLETED" badge (tan pill)
    public GameObject checkmarkIcon;                // Green checkmark (shows when completed)
    public GameObject chevronIcon;                  // Right chevron for navigation

    [Header("Card States")]
    public Image cardBackground;
    public Color highlightedColor = new Color(0.29f, 0.49f, 0.35f); // Green #4a7c59
    public Color completedColor = new Color(0.98f, 0.96f, 0.94f);    // Cream #faf6f0
    public Color incompleteColor = new Color(0.98f, 0.96f, 0.94f);   // Cream #faf6f0

    private DivisionData _division;
    private DivisionProgress _progress;

    public void Setup(DivisionData division, DivisionProgress progress)
    {
        _division = division;
        _progress = progress;

        if (divisionNameText != null)
            divisionNameText.text = division.name;

        if (divisionMottoText != null)
            divisionMottoText.text = $"\"{division.motto}\"";

        int collected = progress.collected.Count;
        int total = division.required_artifacts.Count;

        if (progressText != null)
            progressText.text = $"{collected}/{total} Artifacts Found";

        bool isCompleted = progress.completed;

        if (completedBadge != null)
            completedBadge.SetActive(isCompleted);

        if (checkmarkIcon != null)
            checkmarkIcon.SetActive(isCompleted);

        if (chevronIcon != null)
            chevronIcon.SetActive(true);

        // Set card state color
        // TODO: Implement "highlighted" state logic (first incomplete division?)
        if (cardBackground != null)
        {
            if (isCompleted)
                cardBackground.color = completedColor;
            else
                cardBackground.color = incompleteColor;
        }

        // TODO: Load emblemImage from Addressables using division.emblem_key
    }

    public void OnCardClicked()
    {
        AudioManager.Instance?.PlayUITapSFX();
        Debug.Log($"[DivisionListCard] Clicked: {_division.id}");
        // TODO: Navigate to DivisionDetailScreen and show this division
    }
}