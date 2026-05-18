// ============================================================
// RegisterController.cs
// Location: Assets/Scripts/UI/RegisterController.cs
// Mt. Samat AR — handles player registration form.
// All references are [SerializeField] — wire in Prefab Editor.
// ============================================================

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegisterController : MonoBehaviour
{
    [Header("Form Fields")]
    [SerializeField] private TMP_InputField _nameInput;
    [SerializeField] private TMP_InputField _usernameInput;

    [Header("Buttons")]
    [SerializeField] private Button _saveBtn;
    [SerializeField] private Button _backBtn;

    [Header("Avatar Grid (8 buttons in order)")]
    [SerializeField] private Button[] _avatarButtons = new Button[8];

    private int _selectedAvatar = 0;

    private static readonly Color SELECTED = new Color(0.102f, 0.102f, 0.102f);
    private static readonly Color NORMAL   = new Color(0.910f, 0.910f, 0.910f);

    private void Start()
    {
        _saveBtn?.onClick.AddListener(OnSave);
        _backBtn?.onClick.AddListener(() => NavigationManager.Instance?.ShowPreLoginScreen("MainMenu"));

        for (int i = 0; i < _avatarButtons.Length; i++)
        {
            int idx = i;
            _avatarButtons[i]?.onClick.AddListener(() => SelectAvatar(idx));
        }

        SelectAvatar(0);
    }

    private void SelectAvatar(int index)
    {
        _selectedAvatar = index;
        for (int i = 0; i < _avatarButtons.Length; i++)
        {
            if (_avatarButtons[i] == null) continue;
            var img = _avatarButtons[i].GetComponent<Image>();
            if (img != null) img.color = (i == index) ? SELECTED : NORMAL;
        }
    }

    private void OnSave()
    {
        string name = _nameInput != null ? _nameInput.text.Trim() : "";
        string user = _usernameInput != null ? _usernameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(user))
        {
            Debug.LogWarning("[RegisterController] Name and username are required.");
            return;
        }

        PlayerProfileManager.Instance?.SaveProfile(name, user, _selectedAvatar);
        NavigationManager.Instance?.SwitchToMainApp();
    }
}
