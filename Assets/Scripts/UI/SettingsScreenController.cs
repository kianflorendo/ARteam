// ============================================================
// SettingsScreenController.cs
// Location: Assets/Scripts/UI/SettingsScreenController.cs
// Mt. Samat AR — Settings / About screen.
// Figma node: 301:167 (settings, 390x2034 scrollable)
//
// Sections:
//   App logo + "MT. SAMAT AR"
//   Description placeholder
//   App info cards
//   MEET THE DEVELOPERS
//   Our Partners & Supporters (AFP, DOT, NHCP)
//   Have questions or feedback? + Contact us
//
// Accessed from Profile screen via gear icon.
// All references are [SerializeField] — wire in Prefab Editor.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class SettingsScreenController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button _backBtn;
    [SerializeField] private Button _contactUsBtn;

    private void Start()
    {
        _backBtn?.onClick.AddListener(OnBack);
        _contactUsBtn?.onClick.AddListener(OnContactUs);
    }

    private void OnBack()
    {
        NavigationManager.Instance?.ShowScreen("Profile");
        AudioManager.Instance?.PlayUITapSFX();
    }

    private void OnContactUs()
    {
        Debug.Log("[SettingsScreenController] Contact us tapped.");
        AudioManager.Instance?.PlayUITapSFX();
        // TODO: Open email/contact link via Application.OpenURL()
    }
}
