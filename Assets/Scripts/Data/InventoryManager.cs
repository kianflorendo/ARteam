// ============================================================
// InventoryManager.cs
// Location: Assets/Scripts/Data/InventoryManager.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// Manages all player progress: collected artifacts,
// soldier progress, division progress, AFP token badges.
// Auto-loads on Awake, auto-saves after every mutation.
// Persists across scenes via DontDestroyOnLoad.
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────
    public static InventoryManager Instance { get; private set; }

    // ── Events ───────────────────────────────────────────────
    public static event Action<string> OnArtifactCollected;         // artifactId
    public static event Action<string> OnSoldierProgressUpdated;    // soldierId
    public static event Action<string> OnDivisionProgressUpdated;   // divisionId
    public static event Action<AFPTokenBadge> OnBadgeAdded;

    // ── Private state ────────────────────────────────────────
    private InventoryData _inventory;

    // ── File paths ───────────────────────────────────────────
    private const string INVENTORY_FILENAME = "inventory.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, INVENTORY_FILENAME);

    // ─────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    // ─────────────────────────────────────────────────────────
    //  Load & Save
    // ─────────────────────────────────────────────────────────

    private void Load()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                _inventory = JsonUtility.FromJson<InventoryData>(json);
                Debug.Log($"[InventoryManager] Loaded inventory for player {_inventory.player_id}. " +
                          $"Collected: {_inventory.collected_artifact_ids?.Count ?? 0} artifacts, " +
                          $"Badges: {_inventory.earned_badges?.Count ?? 0}/19");
            }
            catch (Exception e)
            {
                Debug.LogError($"[InventoryManager] Failed to load inventory: {e.Message}. Creating default.");
                CreateDefault();
            }
        }
        else
        {
            Debug.Log("[InventoryManager] No inventory found. Creating default for new player.");
            CreateDefault();
        }
    }

    private void CreateDefault()
    {
        _inventory = new InventoryData
        {
            player_id = Guid.NewGuid().ToString(),
            player_name = "",
            level = 1,
            tokens_earned = 0,
            collected_artifact_ids = new List<string>(),
            soldier_progress = new List<SoldierProgressEntry>(),
            division_progress = new List<DivisionProgressEntry>(),
            earned_badges = new List<AFPTokenBadge>(),
            afp_tokens = new List<AFPToken>()
        };
        Save();
        Debug.Log($"[InventoryManager] New player created: {_inventory.player_id}");
    }

    /// Saves inventory to persistentDataPath/inventory.json
    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(_inventory, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventoryManager] Failed to save inventory: {e.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Artifact collection
    // ─────────────────────────────────────────────────────────

    /// Returns true if the artifact has already been collected
    public bool IsCollected(string artifactId)
    {
        return _inventory.collected_artifact_ids?.Contains(artifactId) ?? false;
    }

    /// Records artifact as collected and auto-saves.
    /// Does nothing if already collected (prevents duplicates).
    public void CollectArtifact(string artifactId)
    {
        if (IsCollected(artifactId))
        {
            Debug.LogWarning($"[InventoryManager] Artifact {artifactId} already collected. Skipping.");
            return;
        }

        _inventory.collected_artifact_ids.Add(artifactId);
        Save();
        Debug.Log($"[InventoryManager] Collected: {artifactId}");
        OnArtifactCollected?.Invoke(artifactId);
    }

    // ─────────────────────────────────────────────────────────
    //  Soldier progress
    // ─────────────────────────────────────────────────────────

    public SoldierProgress GetSoldierProgress(string soldierId)
    {
        var entry = _inventory.soldier_progress?.Find(e => e.soldier_id == soldierId);
        if (entry == null)
        {
            // Create entry if it doesn't exist yet
            entry = new SoldierProgressEntry
            {
                soldier_id = soldierId,
                progress = new SoldierProgress
                {
                    collected = new List<string>(),
                    completed = false,
                    completion_date = ""
                }
            };
            _inventory.soldier_progress.Add(entry);
        }
        return entry.progress;
    }

    public void AddToSoldierProgress(string soldierId, string artifactId)
    {
        var progress = GetSoldierProgress(soldierId);
        if (!progress.collected.Contains(artifactId))
        {
            progress.collected.Add(artifactId);
            Save();
            Debug.Log($"[InventoryManager] Soldier {soldierId} progress: {progress.collected.Count} artifacts");
            OnSoldierProgressUpdated?.Invoke(soldierId);
        }
    }

    public void MarkSoldierComplete(string soldierId)
    {
        var progress = GetSoldierProgress(soldierId);
        if (!progress.completed)
        {
            progress.completed = true;
            progress.completion_date = DateTime.UtcNow.ToString("o");
            Save();
            Debug.Log($"[InventoryManager] Soldier {soldierId} COMPLETED!");
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Division progress
    // ─────────────────────────────────────────────────────────

    public DivisionProgress GetDivisionProgress(string divisionId)
    {
        var entry = _inventory.division_progress?.Find(e => e.division_id == divisionId);
        if (entry == null)
        {
            entry = new DivisionProgressEntry
            {
                division_id = divisionId,
                progress = new DivisionProgress
                {
                    collected = new List<string>(),
                    completed = false,
                    completion_date = ""
                }
            };
            _inventory.division_progress.Add(entry);
        }
        return entry.progress;
    }

    public void AddToDivisionProgress(string divisionId, string artifactId)
    {
        var progress = GetDivisionProgress(divisionId);
        if (!progress.collected.Contains(artifactId))
        {
            progress.collected.Add(artifactId);
            Save();
            Debug.Log($"[InventoryManager] Division {divisionId} progress: {progress.collected.Count} artifacts");
            OnDivisionProgressUpdated?.Invoke(divisionId);
        }
    }

    public void MarkDivisionComplete(string divisionId)
    {
        var progress = GetDivisionProgress(divisionId);
        if (!progress.completed)
        {
            progress.completed = true;
            progress.completion_date = DateTime.UtcNow.ToString("o");
            Save();
            Debug.Log($"[InventoryManager] Division {divisionId} COMPLETED!");
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Token Badges
    // ─────────────────────────────────────────────────────────

    /// Adds a newly generated AFP token badge and auto-saves
    public void AddBadge(AFPTokenBadge badge)
    {
        if (_inventory.earned_badges == null)
            _inventory.earned_badges = new List<AFPTokenBadge>();

        // Prevent duplicate badge
        if (_inventory.earned_badges.Exists(b => b.badge_id == badge.badge_id))
        {
            Debug.LogWarning($"[InventoryManager] Badge {badge.badge_id} already exists. Skipping.");
            return;
        }

        _inventory.earned_badges.Add(badge);
        _inventory.tokens_earned = _inventory.earned_badges.Count;
        Save();
        Debug.Log($"[InventoryManager] Badge earned: {badge.badge_name} " +
                  $"({_inventory.tokens_earned}/19)");
        OnBadgeAdded?.Invoke(badge);
    }

    /// Returns all earned badges
    public List<AFPTokenBadge> GetAllBadges()
    {
        return _inventory.earned_badges ?? new List<AFPTokenBadge>();
    }

    /// Returns all badges with status "pending" (not yet synced to AFP)
    public List<AFPTokenBadge> GetPendingBadges()
    {
        return _inventory.earned_badges?.FindAll(b => b.status == BadgeStatus.Pending)
               ?? new List<AFPTokenBadge>();
    }

    /// Updates the status of a badge (e.g. pending → synced → approved)
    public void UpdateBadgeStatus(string badgeId, string newStatus)
    {
        var badge = _inventory.earned_badges?.Find(b => b.badge_id == badgeId);
        if (badge != null)
        {
            badge.status = newStatus;
            if (newStatus == BadgeStatus.Synced)
                badge.synced_at = DateTime.UtcNow.ToString("o");
            else if (newStatus == BadgeStatus.Approved)
                badge.approved_at = DateTime.UtcNow.ToString("o");
            Save();
        }
    }

    public int GetTotalBadgesEarned() => _inventory.earned_badges?.Count ?? 0;

    // ─────────────────────────────────────────────────────────
    //  AFP Tokens (legacy — kept for backend compatibility)
    // ─────────────────────────────────────────────────────────

    public void AddToken(AFPToken token)
    {
        if (_inventory.afp_tokens == null)
            _inventory.afp_tokens = new List<AFPToken>();
        _inventory.afp_tokens.Add(token);
        Save();
    }

    public List<AFPToken> GetPendingTokens()
    {
        return _inventory.afp_tokens?.FindAll(t => t.status == BadgeStatus.Pending)
               ?? new List<AFPToken>();
    }

    public void UpdateTokenStatus(string tokenId, string newStatus)
    {
        var token = _inventory.afp_tokens?.Find(t => t.token_id == tokenId);
        if (token != null)
        {
            token.status = newStatus;
            if (newStatus == BadgeStatus.Synced)
                token.synced_at = DateTime.UtcNow.ToString("o");
            Save();
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Player info
    // ─────────────────────────────────────────────────────────

    public string GetPlayerId() => _inventory.player_id;
    public string GetPlayerName() => _inventory.player_name;
    public int GetLevel() => _inventory.level;
    public int GetTokenCount() => _inventory.tokens_earned;

    public void SetPlayerName(string playerName)
    {
        _inventory.player_name = playerName;
        Save();
    }

    /// Increments player level and saves
    public void IncrementLevel()
    {
        _inventory.level++;
        Save();
    }

    // ─────────────────────────────────────────────────────────
    //  Debug helper — call from TestData.cs to verify
    // ─────────────────────────────────────────────────────────

    public void DebugPrintInventory()
    {
        Debug.Log("=== INVENTORY DEBUG ===");
        Debug.Log($"Player: {_inventory.player_id}");
        Debug.Log($"Level:  {_inventory.level}");
        Debug.Log($"Collected artifacts: {_inventory.collected_artifact_ids?.Count ?? 0}");
        Debug.Log($"Badges earned: {_inventory.earned_badges?.Count ?? 0}/19");
        Debug.Log($"Tokens pending sync: {GetPendingBadges()?.Count ?? 0}");
        Debug.Log("=======================");
    }
}