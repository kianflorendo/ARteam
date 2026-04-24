using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Artifact card used in Soldier Inventory Screen
/// Shows: artifact image, name, "ACQUIRED" badge, checkmark, description
/// Matches Terra Figma design: horizontal card with image on left
/// </summary>
public class SoldierArtifactCard : MonoBehaviour
{
    [Header("UI Elements")]
    public Image artifactImage;
    public TextMeshProUGUI artifactNameText;        // "M1 Garand"
    public TextMeshProUGUI acquiredBadge;           // "ACQUIRED" badge (green pill)
    public GameObject checkmarkIcon;                // Green checkmark
    public TextMeshProUGUI descriptionText;         // Short description

    private ArtifactData _artifact;
    private bool _isCollected;

    public void Setup(ArtifactData artifact, bool isCollected)
    {
        _artifact = artifact;
        _isCollected = isCollected;

        if (artifactNameText != null)
            artifactNameText.text = artifact.scroll.title;

        if (descriptionText != null)
            descriptionText.text = artifact.scroll.description;

        if (acquiredBadge != null)
            acquiredBadge.gameObject.SetActive(isCollected);

        if (checkmarkIcon != null)
            checkmarkIcon.SetActive(isCollected);

        // TODO: Load artifactImage from Addressables using artifact.bundle_key
    }

    public void OnCardClicked()
    {
        AudioManager.Instance?.PlayUITapSFX();
        Debug.Log($"[SoldierArtifactCard] Clicked: {_artifact.id}");
        // TODO: Show artifact detail popup
    }
}