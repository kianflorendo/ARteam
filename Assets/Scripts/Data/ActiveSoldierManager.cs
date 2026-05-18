// ============================================================
// ActiveSoldierManager.cs
// Location: Assets/Scripts/Data/ActiveSoldierManager.cs
// Mt. Samat AR — stores which soldier is currently selected.
// Persists via PlayerPrefs. Defaults to Filipino (S-001).
// ============================================================

using UnityEngine;

public class ActiveSoldierManager : MonoBehaviour
{
    public static ActiveSoldierManager Instance { get; private set; }

    private const string PREF_KEY        = "active_soldier_id";
    private const string DEFAULT_SOLDIER = "S-001"; // Filipino always first

    public string ActiveSoldierId { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ActiveSoldierId = PlayerPrefs.GetString(PREF_KEY, DEFAULT_SOLDIER);
        Debug.Log($"[ActiveSoldierManager] Active soldier: {ActiveSoldierId}");
    }

    public void SetActiveSoldier(string soldierId)
    {
        if (string.IsNullOrEmpty(soldierId)) return;
        ActiveSoldierId = soldierId;
        PlayerPrefs.SetString(PREF_KEY, soldierId);
        PlayerPrefs.Save();
        Debug.Log($"[ActiveSoldierManager] Switched to: {soldierId}");
    }

    public void ResetToDefault()
    {
        SetActiveSoldier(DEFAULT_SOLDIER);
    }
}
