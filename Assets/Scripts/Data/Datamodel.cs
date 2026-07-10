using System;
using System.Collections.Generic;
using UnityEngine;

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
    public string type;
    public string anchor_mode;
    public string gps_progression_mode;
    public int sequence_index;
    public float distance_from_previous_meters;
    public float spawn_distance_from_player_meters;
    public float spawn_height_offset_meters;
    public string spawn_presentation;
    public string marker_name;
    public double gps_lat;
    public double gps_lng;
    public double gps_altitude;
    public float gps_geofence_radius_meters;
    public string soldier_id;
    public List<string> shared_soldier_ids;
    public string division_id;
    public string bundle_key;
    public string tracking_lost_behavior;
    public ScrollData scroll;
}

[Serializable]
public class ScrollData
{
    public string title;
    public string category;
    public string description;
    public string location;
    public List<SpecItem> specs;
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
    public string nationality;
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

[Serializable]
public class AFPToken
{
    public string token_id;
    public string type;
    public string reference_id;
    public string status;
    public string generated_at;
    public string synced_at;
    public string approved_at;
    public string player_id;
}

[Serializable]
public class AFPTokenBadge
{
    public string badge_id;
    public string badge_name;
    public string badge_description;
    public string badge_bundle_key;
    public string type;
    public string reference_id;
    public string status;
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
