using UnityEngine;

/// <summary>
/// Checks whether the device is within 3000m of Mt. Samat (Dambana ng Kagitingan).
/// Gates the GPS route system — image anchor artifacts are unaffected.
/// When LocationServiceManager.forceStableFix = true (indoor/editor testing),
/// the geofence is bypassed so the GPS route still works without real GPS.
/// </summary>
[DefaultExecutionOrder(-50)]
public class GeofenceGuard : MonoBehaviour
{
    public static GeofenceGuard Instance { get; private set; }

    // Dambana ng Kagitingan / Shrine of Valor, Mt. Samat, Bataan
    private const double CENTER_LAT     = 14.6547;
    private const double CENTER_LON     = 120.5284;
    private const float  RADIUS_METERS  = 3000f;

    // Sacred/quiet exclusion zone — the memorial building itself. GPS artifacts must
    // never spawn or remain visible within this radius, out of respect for the site.
    private const double EXCLUSION_LAT          = 14.605753933531217;
    private const double EXCLUSION_LON          = 120.50973828185982;
    private const float  EXCLUSION_RADIUS_METERS = 50f;

    public static event System.Action<bool> OnGeofenceStatusChanged;
    public static event System.Action<bool> OnExclusionZoneStatusChanged;

    // Default true so GPS route is not blocked before the first real GPS fix arrives.
    // OfflineGPSRouteManager.EnsureOriginCaptured also requires HasStableFix, so the
    // route cannot actually start until real GPS data is available regardless.
    private bool _isInside = true;
    private bool _hasChecked;

    // Default false — only exclude once a real GPS fix confirms the player is actually
    // inside the sacred zone. Blocking by default would need no purpose before GPS is live.
    private bool _isInsideExclusionZone;
    private bool _hasCheckedExclusion;

    public bool IsInsideGeofence => _isInside;
    public bool IsInsideExclusionZone => _isInsideExclusionZone;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("[AUTO] GeofenceGuard");
        go.AddComponent<GeofenceGuard>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (LocationServiceManager.Instance == null) return;

        // forceStableFix = true means indoor/editor testing — treat as inside so
        // the GPS route runs normally without a real GPS lock.
        if (LocationServiceManager.Instance.forceStableFix)
        {
            SetStatus(true);
            return;
        }

        // Wait for a real filtered GPS reading before evaluating.
        if (!LocationServiceManager.Instance.HasFilteredFix) return;

        double lat = LocationServiceManager.Instance.FilteredLatitude;
        double lon = LocationServiceManager.Instance.FilteredLongitude;

        bool inside = GeoUtils.IsInsideGeofence(lat, lon, CENTER_LAT, CENTER_LON, RADIUS_METERS);
        SetStatus(inside);

        bool insideExclusion = GeoUtils.IsInsideGeofence(
            lat, lon, EXCLUSION_LAT, EXCLUSION_LON, EXCLUSION_RADIUS_METERS);
        SetExclusionStatus(insideExclusion);
    }

    private void SetStatus(bool inside)
    {
        if (_hasChecked && inside == _isInside) return;
        _isInside = inside;
        _hasChecked = true;

        OnGeofenceStatusChanged?.Invoke(inside);

        if (!inside)
            Debug.LogWarning($"[GeofenceGuard] Outside Mt. Samat geofence ({RADIUS_METERS}m). GPS route paused.");
        else
            Debug.Log("[GeofenceGuard] Inside Mt. Samat geofence. GPS route active.");
    }

    private void SetExclusionStatus(bool inside)
    {
        if (_hasCheckedExclusion && inside == _isInsideExclusionZone) return;
        _isInsideExclusionZone = inside;
        _hasCheckedExclusion = true;

        OnExclusionZoneStatusChanged?.Invoke(inside);

        if (inside)
            Debug.Log($"[GeofenceGuard] Entered sacred exclusion zone ({EXCLUSION_RADIUS_METERS}m) — GPS artifacts hidden here.");
        else
            Debug.Log("[GeofenceGuard] Left sacred exclusion zone — GPS artifacts resumed.");
    }
}
