using UnityEngine;

/// <summary>
/// Lightweight low-pass filter for GPS coordinates.
/// This is used to stabilize the initial route origin capture.
/// Short-range progression distances are measured in AR world space,
/// not by raw GPS displacement.
/// </summary>
public class GPSDistanceFilter
{
    private readonly float _smoothingFactor;

    public bool HasSample { get; private set; }
    public double FilteredLatitude { get; private set; }
    public double FilteredLongitude { get; private set; }
    public float FilteredAccuracy { get; private set; }

    public GPSDistanceFilter(float smoothingFactor)
    {
        _smoothingFactor = Mathf.Clamp01(smoothingFactor);
    }

    public void Reset()
    {
        HasSample = false;
        FilteredLatitude = 0d;
        FilteredLongitude = 0d;
        FilteredAccuracy = 0f;
    }

    public void AddSample(double latitude, double longitude, float accuracy)
    {
        if (!HasSample)
        {
            FilteredLatitude = latitude;
            FilteredLongitude = longitude;
            FilteredAccuracy = accuracy;
            HasSample = true;
            return;
        }

        FilteredLatitude = LerpDouble(FilteredLatitude, latitude, _smoothingFactor);
        FilteredLongitude = LerpDouble(FilteredLongitude, longitude, _smoothingFactor);

        FilteredAccuracy = Mathf.Lerp(
            FilteredAccuracy,
            accuracy,
            _smoothingFactor);
    }

    private static double LerpDouble(double a, double b, float t)
    {
        return a + (b - a) * t;
    }
}
