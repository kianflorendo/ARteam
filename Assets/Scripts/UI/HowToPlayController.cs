using UnityEngine;
using UnityEngine.UI;

public class HowToPlayController : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button _backBtn;

    private void Start()
    {
        _backBtn?.onClick.AddListener(() => NavigationManager.Instance?.ShowPreLoginScreen("MainMenu"));
    }
}
