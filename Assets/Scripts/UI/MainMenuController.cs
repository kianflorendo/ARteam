// ============================================================
// MainMenuController.cs
// Location: Assets/Scripts/UI/MainMenuController.cs
// Mt. Samat AR — handles Main Menu screen buttons.
// All references are [SerializeField] — wire in Prefab Editor.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _startMissionBtn;
    [SerializeField] private Button _howToPlayBtn;
    [SerializeField] private Button _settingsBtn;
    [SerializeField] private Button _exitBtn;

    private void Start()
    {
        _startMissionBtn?.onClick.AddListener(OnStartMission);
        _howToPlayBtn?.onClick.AddListener(OnHowToPlay);
        _settingsBtn?.onClick.AddListener(OnSettings);
        _exitBtn?.onClick.AddListener(OnExit);
    }

    private void OnStartMission()
    {
        if (PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.IsRegistered)
            NavigationManager.Instance?.SwitchToMainApp();
        else
            NavigationManager.Instance?.ShowPreLoginScreen("Register");
    }

    private void OnHowToPlay()
    {
        NavigationManager.Instance?.ShowPreLoginScreen("HowToPlay");
    }

    private void OnSettings()
    {
        Debug.Log("[MainMenuController] Settings — coming soon.");
    }

    private void OnExit()
    {
        Application.Quit();
    }
}
