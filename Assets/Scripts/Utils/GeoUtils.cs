// ============================================================
// GeoUtils.cs
// Location: Assets/Scripts/Utils/GeoUtils.cs
// Mt. Samat AR Scavenger Hunt -- Terra App
//
// GPS utility functions for coordinate math and debug calculations.
// ============================================================

using UnityEngine;

public static class GeoUtils
{
    // Earth radius in meters
    private const double EARTH_RADIUS_METERS = 6371000.0;

    // ============================================================
    //  Haversine Distance
    //  Returns the distance in meters between two GPS coordinates.
    //  Used to check if user is within geofence_radius of artifact.
    // ============================================================

    public static float HaversineDistance(
        double lat1, double lng1,
        double lat2, double lng2)
    {
        double dLat = ToRadians(lat2 - lat1);
        double dLng = ToRadians(lng2 - lng1);

        double a = System.Math.Sin(dLat / 2) * System.Math.Sin(dLat / 2)
                 + System.Math.Cos(ToRadians(lat1))
                 * System.Math.Cos(ToRadians(lat2))
                 * System.Math.Sin(dLng / 2) * System.Math.Sin(dLng / 2);

        double c = 2 * System.Math.Atan2(
                           System.Math.Sqrt(a),
                           System.Math.Sqrt(1 - a));

        return (float)(EARTH_RADIUS_METERS * c);
    }

    // ============================================================
    //  IsInsideGeofence
    //  Returns true if the user's GPS position is within
    //  the given radius (meters) of the target coordinates.
    // ============================================================

    public static bool IsInsideGeofence(
        double userLat, double userLng,
        double targetLat, double targetLng,
        float radiusMeters)
    {
        float distance = HaversineDistance(userLat, userLng, targetLat, targetLng);
        return distance <= radiusMeters;
    }

    // ============================================================
    //  Helpers
    // ============================================================

    private static double ToRadians(double degrees)
        => degrees * System.Math.PI / 180.0;

    // ============================================================
    //  Debug helper -- returns formatted distance string
    // ============================================================

    public static string FormatDistance(float meters)
    {
        if (meters < 1000f)
            return $"{meters:F0}m";
        return $"{meters / 1000f:F1}km";
    }
}
