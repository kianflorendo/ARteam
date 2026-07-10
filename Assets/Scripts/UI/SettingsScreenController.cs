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
    }
}
