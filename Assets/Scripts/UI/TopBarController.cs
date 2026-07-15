using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBarController : MonoBehaviour
{
    [Header("Avatar Sprites (match RegisterController order)")]
    [SerializeField] private Sprite[] _avatarSprites = new Sprite[8];

    private Image           _avatarCircle;
    private TextMeshProUGUI _usernameLabel;

    private void Awake()
    {
        _avatarCircle  = transform.Find("AvatarCircle")?.GetComponent<Image>();
        _usernameLabel = transform.Find("UsernameLabel")?.GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        if (PlayerProfileManager.Instance == null) return;

        int    idx  = PlayerProfileManager.Instance.AvatarIndex;
        string user = PlayerProfileManager.Instance.Username;

        if (_avatarCircle != null && _avatarSprites != null && idx < _avatarSprites.Length)
            _avatarCircle.sprite = _avatarSprites[idx];

        if (_usernameLabel != null)
            _usernameLabel.text = string.IsNullOrEmpty(user) ? "PLAYER" : user.ToUpper();
    }
}
