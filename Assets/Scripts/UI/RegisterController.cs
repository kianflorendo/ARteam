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

    [Header("Avatar Sprites (match button order)")]
    [SerializeField] private Sprite[] _avatarSprites = new Sprite[8];

    private int _selectedAvatar = 0;

    private static readonly Color RING_SELECTED = new Color(0.102f, 0.102f, 0.102f);
    private static readonly Color RING_NORMAL   = new Color(0.910f, 0.910f, 0.910f);

    private void Start()
    {
        _saveBtn?.onClick.AddListener(OnSave);
        _backBtn?.onClick.AddListener(() => NavigationManager.Instance?.ShowPreLoginScreen("MainMenu"));

        for (int i = 0; i < _avatarButtons.Length; i++)
        {
            int idx = i;
            if (_avatarButtons[i] == null) continue;
            _avatarButtons[i].onClick.AddListener(() => SelectAvatar(idx));
            SetupAvatarButton(_avatarButtons[i], i);
        }

        SelectAvatar(0);
    }

    private void SetupAvatarButton(Button btn, int index)
    {
        // Button's own Image = the selection ring (border visible around the avatar)
        var ring = btn.GetComponent<Image>();
        if (ring != null) ring.color = RING_NORMAL;

        // Child Image shows the actual avatar sprite, inset 3px so the ring is visible
        var go = new GameObject("AvatarImg", typeof(RectTransform));
        go.transform.SetParent(btn.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(3f, 3f);
        rt.offsetMax = new Vector2(-3f, -3f);

        var img = go.AddComponent<Image>();
        img.raycastTarget = false;

        if (_avatarSprites != null && index < _avatarSprites.Length)
            img.sprite = _avatarSprites[index];
    }

    private void SelectAvatar(int index)
    {
        _selectedAvatar = index;
        for (int i = 0; i < _avatarButtons.Length; i++)
        {
            if (_avatarButtons[i] == null) continue;
            var ring = _avatarButtons[i].GetComponent<Image>();
            if (ring != null) ring.color = (i == index) ? RING_SELECTED : RING_NORMAL;
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
