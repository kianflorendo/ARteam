using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Starts Unity's offline location service and exposes a filtered GPS fix.
/// GPS is only used to capture the player's route origin for the offline route.
/// The exact 1m / 5m / 10m progression distances are measured in AR world space.
/// </summary>
[DefaultExecutionOrder(-150)]
public class LocationServiceManager : MonoBehaviour
{
    public static LocationServiceManager Instance { get; private set; }

    [Header("Location Service")]
    public float desiredAccuracyMeters = 10f;
    public float updateDistanceMeters = 0.5f;
    public int startupTimeoutSeconds = 20;
    public float pollIntervalSeconds = 0.5f;

    [Header("Sample Validation")]
    public float maxAcceptedAccuracyMeters = 25f;
    public float stableFixAccuracyMeters = 12f;
    public int stableSamplesRequired = 3;
    [Range(0.05f, 1f)] public float smoothingFactor = 0.3f;

    private GPSDistanceFilter _filter;
    private bool _serviceRequested;
    private bool _serviceRunning;
    private bool _locationPermissionLogged;
    private float _pollTimer;
    private double _lastTimestamp = -1d;
    private int _stableSampleCount;

    public bool HasFilteredFix => _filter != null && _filter.HasSample;
    public bool HasStableFix => HasFilteredFix && _stableSampleCount >= stableSamplesRequired;
    public double FilteredLatitude => _filter != null ? _filter.FilteredLatitude : 0d;
    public double FilteredLongitude => _filter != null ? _filter.FilteredLongitude : 0d;
    public float FilteredAccuracy => _filter != null ? _filter.FilteredAccuracy : 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null) return;

        var go = new GameObject("[AUTO] LocationServiceManager");
        go.AddComponent<LocationServiceManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _filter = new GPSDistanceFilter(smoothingFactor);
    }

    private void Update()
    {
        if (!_serviceRequested)
        {
            TryStartService();
        }

        if (!_serviceRunning || Input.location.status != LocationServiceStatus.Running)
            return;

        _pollTimer += Time.unscaledDeltaTime;
        if (_pollTimer < pollIntervalSeconds)
            return;

        _pollTimer = 0f;
        ConsumeLatestSample();
    }

    private void TryStartService()
    {
        if (!HasLocationPermission())
        {
            if (!_locationPermissionLogged)
            {
                Debug.Log("[LocationServiceManager] Waiting for location permission.");
                _locationPermissionLogged = true;
            }
            return;
        }

        _serviceRequested = true;
        StartCoroutine(StartLocationServiceCoroutine());
    }

    private IEnumerator StartLocationServiceCoroutine()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("[LocationServiceManager] Device location is disabled by the user.");
            yield break;
        }

        Input.location.Start(desiredAccuracyMeters, updateDistanceMeters);

        int remaining = startupTimeoutSeconds;
        while (Input.location.status == LocationServiceStatus.Initializing && remaining > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            remaining--;
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            _serviceRunning = true;
            Debug.Log("[LocationServiceManager] Location service running.");
            ConsumeLatestSample();
            yield break;
        }

        Debug.LogWarning($"[LocationServiceManager] Location service failed to start: {Input.location.status}");
    }

    private void ConsumeLatestSample()
    {
        var data = Input.location.lastData;
        if (data.timestamp <= 0d || data.timestamp == _lastTimestamp)
            return;

        _lastTimestamp = data.timestamp;

        if (data.horizontalAccuracy <= 0f || data.horizontalAccuracy > maxAcceptedAccuracyMeters)
        {
            _stableSampleCount = 0;
            return;
        }

        _filter.AddSample(data.latitude, data.longitude, data.horizontalAccuracy);

        if (data.horizontalAccuracy <= stableFixAccuracyMeters)
            _stableSampleCount++;
        else
            _stableSampleCount = 0;
    }

    public bool TryGetFilteredLocation(
        out double latitude,
        out double longitude,
        out float accuracyMeters)
    {
        latitude = FilteredLatitude;
        longitude = FilteredLongitude;
        accuracyMeters = FilteredAccuracy;
        return HasFilteredFix;
    }

    public string GetStatusString()
    {
        if (!HasLocationPermission())
            return "Location permission pending";

        if (!_serviceRequested)
            return "Waiting to start";

        if (!_serviceRunning)
            return $"Location status: {Input.location.status}";

        if (!HasFilteredFix)
            return "Acquiring GPS fix";

        if (!HasStableFix)
            return $"Stabilizing GPS ({_stableSampleCount}/{stableSamplesRequired})";

        return $"GPS ready +/-{FilteredAccuracy:F1}m";
    }

    private bool HasLocationPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(Permission.FineLocation);
#else
        return true;
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this && Input.location.status == LocationServiceStatus.Running)
            Input.location.Stop();
    }
}
