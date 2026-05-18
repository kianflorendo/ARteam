// ============================================================
// HowToPlayController.cs
// Location: Assets/Scripts/UI/HowToPlayController.cs
// Mt. Samat AR — How To Play screen navigation.
// All references are [SerializeField] — wire in Prefab Editor.
// ============================================================

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
