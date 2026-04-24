using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Artifact grid card used in Division Detail Screen
/// Shows: artifact icon, name, COLLECTED or MISSING state
/// Matches Terra Figma design: square card with solid border (collected) or dashed border (missing)
/// </summary>
public class DivisionArtifactGridCard : MonoBehaviour
{
    [Header("UI Elements")]
    public Image artifactIconImage;
    public TextMeshProUGUI artifactNameText;        // "Enfield Rifle"
    public TextMeshProUGUI statusText;              // "COLLECTED" (green) or "MISSING" (gray)

    [Header("Card States")]
    public Image cardBorder;
    public GameObject collectedState;               // Green icon background, solid border
    public GameObject missingState;                 // Gray icon background, dashed border

    private ArtifactData _artifact;
    private bool _isCollected;

    public void Setup(ArtifactData artifact, bool isCollected)
    {
        _artifact = artifact;
        _isCollected = isCollected;

        if (artifactNameText != null)
            artifactNameText.text = artifact.scroll.title;

        if (statusText != null)
        {
            statusText.text = isCollected ? "COLLECTED" : "MISSING";
            statusText.color = isCollected ? new Color(0.29f, 0.49f, 0.35f) : new Color(0.6f, 0.6f, 0.6f);
        }

        if (collectedState != null)
            collectedState.SetActive(isCollected);

        if (missingState != null)
            missingState.SetActive(!isCollected);

        // TODO: Load artifactIconImage from Addressables using artifact.bundle_key
        // TODO: Apply dashed border effect for missing state
    }

    public void OnCardClicked()
    {
        AudioManager.Instance?.PlayUITapSFX();
        Debug.Log($"[DivisionArtifactGridCard] Clicked: {_artifact.id} - Collected: {_isCollected}");
        // TODO: Show artifact detail popup if collected, or show location hint if missing
    }
}