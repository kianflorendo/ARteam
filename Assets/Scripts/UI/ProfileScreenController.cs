// ============================================================
// ProfileScreenController.cs
// Location: Assets/Scripts/UI/ProfileScreenController.cs
// Mt. Samat AR — Profile screen.
//
// Figma (302:110):
//   - Custom header "MT. SAMAT AR" + gear icon → Settings
//   - Single combined label: "BOLD NAME • @username"
//   - Progress bar + Resume Journey
//
// All references are [SerializeField] — wire in Prefab Editor.
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileScreenController : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Button _settingsBtn;

    [Header("Avatar Section")]
    [SerializeField] private TextMeshProUGUI _nameAndUsernameLabel;

    [Header("Progress Card")]
    [SerializeField] private TextMeshProUGUI _progressLabel;
    [SerializeField] private Image           _progressBarFill;
    [SerializeField] private Button          _resumeBtn;

    private void Start()
    {
        _settingsBtn?.onClick.AddListener(() => NavigationManager.Instance?.ShowScreen("Settings"));
        _resumeBtn?.onClick.AddListener(() => NavigationManager.Instance?.ShowScreen("ARScan"));
    }

    private void OnEnable() => Refresh();

    private void Refresh()
    {
        RefreshNameAndUsername();
        RefreshProgress();
    }

    private void RefreshNameAndUsername()
    {
        if (_nameAndUsernameLabel == null || PlayerProfileManager.Instance == null) return;

        string name = PlayerProfileManager.Instance.PlayerName;
        string user = PlayerProfileManager.Instance.Username;

        string displayName = string.IsNullOrEmpty(name) ? "PLAYER" : name.ToUpper();
        string displayUser = string.IsNullOrEmpty(user) ? "@player" : "@" + user.ToLower();

        // Figma: bold name + lighter username on same line
        _nameAndUsernameLabel.text =
            $"<b>{displayName}</b> <color=#888888>• {displayUser}</color>";
    }

    private void RefreshProgress()
    {
        if (InventoryManager.Instance == null || ManifestLoader.Instance == null) return;

        var route     = ManifestLoader.Instance.GetGPSRouteArtifacts();
        int total     = route.Count;
        int collected = 0;
        foreach (var a in route)
            if (InventoryManager.Instance.IsCollected(a.id)) collected++;

        if (_progressLabel != null)
            _progressLabel.text = $"Progress: {collected}/{total} Artifacts Found";
        if (_progressBarFill != null)
            _progressBarFill.fillAmount = total > 0 ? (float)collected / total : 0f;
    }
}
