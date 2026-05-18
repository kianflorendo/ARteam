// ============================================================
// PlayerProfileManager.cs
// Location: Assets/Scripts/Data/PlayerProfileManager.cs
// Mt. Samat AR — stores player registration data in PlayerPrefs.
// ============================================================

using UnityEngine;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance { get; private set; }

    private const string KEY_NAME       = "player_name";
    private const string KEY_USERNAME   = "player_username";
    private const string KEY_AVATAR     = "player_avatar_index";
    private const string KEY_REGISTERED = "player_is_registered";

    public bool   IsRegistered => PlayerPrefs.GetInt(KEY_REGISTERED, 0) == 1;
    public string PlayerName   => PlayerPrefs.GetString(KEY_NAME, "");
    public string Username     => PlayerPrefs.GetString(KEY_USERNAME, "");
    public int    AvatarIndex  => PlayerPrefs.GetInt(KEY_AVATAR, 0);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveProfile(string playerName, string username, int avatarIndex)
    {
        PlayerPrefs.SetString(KEY_NAME, playerName);
        PlayerPrefs.SetString(KEY_USERNAME, username);
        PlayerPrefs.SetInt(KEY_AVATAR, avatarIndex);
        PlayerPrefs.SetInt(KEY_REGISTERED, 1);
        PlayerPrefs.Save();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.SetPlayerName(playerName);

        Debug.Log($"[PlayerProfileManager] Saved: {playerName} / @{username} / avatar {avatarIndex}");
    }

    public void ClearProfile()
    {
        PlayerPrefs.DeleteKey(KEY_NAME);
        PlayerPrefs.DeleteKey(KEY_USERNAME);
        PlayerPrefs.DeleteKey(KEY_AVATAR);
        PlayerPrefs.DeleteKey(KEY_REGISTERED);
        PlayerPrefs.Save();
        Debug.Log("[PlayerProfileManager] Profile cleared.");
    }
}
