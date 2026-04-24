// ============================================================
// DataModels.cs
// Location: Assets/Scripts/Data/DataModels.cs
// Mt. Samat AR Scavenger Hunt — Terra App
// All C# data classes matching manifest.json and inventory.json
// No logic — pure data containers only
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────
//  MANIFEST DATA — matches manifest.json schema
// ─────────────────────────────────────────────

[Serializable]
public class ManifestData
{
    public string version;
    public string build_date;
    public List<ArtifactData> artifacts;
    public List<SoldierData> soldiers;
    public List<DivisionData> divisions;
}

[Serializable]
public class ArtifactData
{
    public string id;
    public string name;
    public string type;                        // "collectible" or "info_only"
    public string anchor_mode;                 // "image" or "gps"
    public string gps_progression_mode;        // "distance_chain" for offline route progression
    public int sequence_index;                 // ordered route step for GPS artifacts
    public float distance_from_previous_meters;// how far player must walk after previous GPS artifact
    public float spawn_distance_from_player_meters; // local AR presentation distance from player
    public string spawn_presentation;          // "camera_forward" or future "detected_plane"
    public string marker_name;                 // XRReferenceImageLibrary image name
    public double gps_lat;
    public double gps_lng;
    public double gps_altitude;
    public float gps_geofence_radius_meters;
    public string soldier_id;                  // "" if info_only
    public string division_id;                 // "" if info_only
    public string bundle_key;                  // Addressables key, "" if info_only
    public string tracking_lost_behavior;      // "freeze" or "hide"
    public ScrollData scroll;
}

[Serializable]
public class ScrollData
{
    public string title;
    public string category;
    public string description;
    public string location;
    public SpecsWrapper specs;                 // key-value display in scroll UI
}

// Unity's JsonUtility cannot deserialize Dictionary directly.
// SpecsWrapper holds named fields for the known spec keys.
// For dynamic specs, use a List<SpecItem> pattern below.
[Serializable]
public class SpecsWrapper
{
    // Common spec fields — add more as needed when filling real artifact data
    [SerializeField] private List<SpecItem> _items = new List<SpecItem>();

    public List<SpecItem> Items => _items;
}

[Serializable]
public class SpecItem
{
    public string key;
    public string value;
}

[Serializable]
public class SoldierData
{
    public string id;
    public string name;
    public string nationality;                 // "Filipino", "Japanese", "American"
    public string bundle_key;
    public List<string> required_artifacts;
    public BadgeConfig token_badge;
}

[Serializable]
public class DivisionData
{
    public string id;
    public string name;
    public string motto;
    public string emblem_key;
    public List<string> required_artifacts;
    public BadgeConfig token_badge;
}

[Serializable]
public class BadgeConfig
{
    public string badge_id;
    public string badge_name;
    public string badge_description;
    public string badge_bundle_key;
}

// ─────────────────────────────────────────────
//  INVENTORY DATA — matches inventory.json schema
// ─────────────────────────────────────────────

[Serializable]
public class InventoryData
{
    public string player_id;
    public string player_name;
    public int level;
    public int tokens_earned;
    public List<string> collected_artifact_ids;
    public List<SoldierProgressEntry> soldier_progress;
    public List<DivisionProgressEntry> division_progress;
    public List<AFPTokenBadge> earned_badges;
    public List<AFPToken> afp_tokens;
}

// Unity JsonUtility cannot serialize Dictionary<string, T>.
// We store progress as List<Entry> and look up by id at runtime.
[Serializable]
public class SoldierProgressEntry
{
    public string soldier_id;
    public SoldierProgress progress;
}

[Serializable]
public class DivisionProgressEntry
{
    public string division_id;
    public DivisionProgress progress;
}

[Serializable]
public class SoldierProgress
{
    public List<string> collected;
    public bool completed;
    public string completion_date;
}

[Serializable]
public class DivisionProgress
{
    public List<string> collected;
    public bool completed;
    public string completion_date;
}

// ─────────────────────────────────────────────
//  AFP TOKEN & BADGE DATA
// ─────────────────────────────────────────────

[Serializable]
public class AFPToken
{
    public string token_id;
    public string type;              // "soldier" or "division"
    public string reference_id;      // e.g. "S-001" or "D-21"
    public string status;            // "pending" | "synced" | "approved" | "issued"
    public string generated_at;      // ISO8601
    public string synced_at;
    public string approved_at;
    public string player_id;
}

[Serializable]
public class AFPTokenBadge
{
    public string badge_id;          // e.g. "BADGE-S-001"
    public string badge_name;        // e.g. "Guardian of Bataan"
    public string badge_description;
    public string badge_bundle_key;  // Addressables key for badge image
    public string type;              // "soldier" or "division"
    public string reference_id;      // "S-001" or "D-21"
    public string status;            // "pending" | "synced" | "approved" | "issued"
    public string generated_at;
    public string synced_at;
    public string approved_at;
    public string player_id;
}

[Serializable]
public class GPSRouteStateData
{
    public double origin_lat;
    public double origin_lng;
    public float origin_accuracy_m;
    public string initialized_at;
    public bool has_origin;
    public int next_sequence_index;
    public string active_artifact_id;
}

// ─────────────────────────────────────────────
//  CONSTANTS — artifact type and anchor mode
//  Use these throughout all scripts instead of
//  raw strings to prevent typos
// ─────────────────────────────────────────────

public static class ArtifactType
{
    public const string Collectible = "collectible";
    public const string InfoOnly = "info_only";
}

public static class AnchorMode
{
    public const string Image = "image";
    public const string GPS = "gps";
}

public static class GPSProgressionMode
{
    public const string DistanceChain = "distance_chain";
}

public static class GPSSpawnPresentation
{
    public const string CameraForward = "camera_forward";
    public const string DetectedPlane = "detected_plane";
}

public static class TrackingLostBehavior
{
    public const string Freeze = "freeze";
    public const string Hide = "hide";
}

public static class BadgeStatus
{
    public const string Pending = "pending";
    public const string Synced = "synced";
    public const string Approved = "approved";
    public const string Issued = "issued";
}
