using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Persists offline route state across app sessions.
/// The route uses GPS only to establish the starting origin.
/// After that, segment progression is measured in AR world meters.
/// </summary>
[DefaultExecutionOrder(-140)]
public class GPSRouteStateStore : MonoBehaviour
{
    public static GPSRouteStateStore Instance { get; private set; }

    private const string FileName = "gps_route_state.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public GPSRouteStateData State { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null) return;

        var go = new GameObject("[AUTO] GPSRouteStateStore");
        go.AddComponent<GPSRouteStateStore>();
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
        Load();
    }

    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(State, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GPSRouteStateStore] Failed to save route state: {e.Message}");
        }
    }

    public void ResetState()
    {
        State = CreateDefaultState();
        Save();
    }

    private void Load()
    {
        if (!File.Exists(SavePath))
        {
            State = CreateDefaultState();
            Save();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            State = JsonUtility.FromJson<GPSRouteStateData>(json);
            if (State == null)
                State = CreateDefaultState();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GPSRouteStateStore] Failed to load route state: {e.Message}");
            State = CreateDefaultState();
            Save();
        }
    }

    private static GPSRouteStateData CreateDefaultState()
    {
        return new GPSRouteStateData
        {
            origin_lat = 0d,
            origin_lng = 0d,
            origin_accuracy_m = 0f,
            initialized_at = "",
            has_origin = false,
            next_sequence_index = 1,
            active_artifact_id = ""
        };
    }
}
