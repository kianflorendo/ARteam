using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoldierArtifactCard : MonoBehaviour
{
    [Header("UI Elements")]
    public Image artifactImage;
    public TextMeshProUGUI artifactNameText;
    public TextMeshProUGUI acquiredBadge;
    public GameObject checkmarkIcon;
    public TextMeshProUGUI descriptionText;

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
    }

    public void OnCardClicked()
    {
        AudioManager.Instance?.PlayUITapSFX();
        Debug.Log($"[SoldierArtifactCard] Clicked: {_artifact.id}");
    }
}
