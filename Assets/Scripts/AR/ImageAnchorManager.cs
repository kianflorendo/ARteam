using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageAnchorManager : MonoBehaviour
{
    [Header("AR References")]
    [Tooltip("Assign the ARTrackedImageManager from XR Origin here")]
    public ARTrackedImageManager imageManager;

    private Dictionary<string, TrackableId> _spawnedMap
        = new Dictionary<string, TrackableId>();

    private Dictionary<TrackableId, string> _trackableToArtifact
        = new Dictionary<TrackableId, string>();

    private void Awake()
    {
        if (imageManager == null)
            imageManager = FindAnyObjectByType<ARTrackedImageManager>();

        if (imageManager == null)
            Debug.LogError("[ImageAnchorManager] ARTrackedImageManager not found!");
        else
            Debug.Log("[ImageAnchorManager] ARTrackedImageManager found: " + imageManager.name);
    }

    private void Start()
    {
        if (imageManager != null && imageManager.referenceLibrary != null)
        {
            Debug.Log($"[ImageAnchorManager] Reference library has " +
                      $"{imageManager.referenceLibrary.count} images:");

            for (int i = 0; i < imageManager.referenceLibrary.count; i++)
            {
                Debug.Log($"[ImageAnchorManager]   Image[{i}]: " +
                          $"'{imageManager.referenceLibrary[i].name}'");
            }
        }
        else
        {
            Debug.LogError("[ImageAnchorManager] Reference library is NULL. " +
                           "Assign MtSamatImageLibrary to ARTrackedImageManager.");
        }

        if (ManifestLoader.Instance != null)
        {
            if (imageManager != null && imageManager.referenceLibrary != null)
            {
                for (int i = 0; i < imageManager.referenceLibrary.count; i++)
                {
                    string imgName = imageManager.referenceLibrary[i].name;
                    var artifact = ManifestLoader.Instance.GetArtifactByMarker(imgName);
                    if (artifact != null)
                        Debug.Log("[ImageAnchorManager] MATCH: " + imgName +
                                  " -> " + artifact.name + " type=" + artifact.type);
                    else
                        Debug.LogWarning("[ImageAnchorManager] NO MATCH: " + imgName +
                                         " not found in manifest!");
                }
            }
        }
        else
        {
            Debug.LogError("[ImageAnchorManager] ManifestLoader.Instance is NULL.");
        }
    }

    private void OnEnable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
        ManifestLoader.OnManifestLoaded += OnManifestLoaded;
    }

    private void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        ManifestLoader.OnManifestLoaded -= OnManifestLoaded;
    }

    // Re-process any currently-tracked images that were missed when the manifest
    // hadn't loaded yet at the time of their first detection.
    private void OnManifestLoaded()
    {
        Debug.Log("[ImageAnchorManager] Manifest loaded — retrying any already-tracked images.");
        RetryTrackedImages();
    }

    private void RetryTrackedImages()
    {
        if (imageManager == null) return;
        foreach (var trackedImage in imageManager.trackables)
        {
            if (trackedImage.trackingState == TrackingState.None)
                continue;
            if (_trackableToArtifact.ContainsKey(trackedImage.trackableId))
                continue;
            Debug.Log($"[ImageAnchorManager] Retrying missed image: " +
                      $"'{trackedImage.referenceImage.name}'");
            HandleImageAdded(trackedImage);
        }
    }

    private void OnTrackablesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var trackedImage in args.added)
        {
            Debug.Log($"[ImageAnchorManager] IMAGE DETECTED: " +
                      $"'{trackedImage.referenceImage.name}' " +
                      $"state={trackedImage.trackingState}");
            HandleImageAdded(trackedImage);
        }

        foreach (var trackedImage in args.updated)
            HandleImageUpdated(trackedImage);

        foreach (var kvp in args.removed)
            HandleImageRemoved(kvp.Key);
    }

    private void HandleImageAdded(ARTrackedImage trackedImage)
    {
        string markerName = trackedImage.referenceImage.name;

        Debug.Log($"[ImageAnchorManager] Looking up marker: '{markerName}'");

        if (ManifestLoader.Instance == null)
        {
            Debug.LogError("[ImageAnchorManager] ManifestLoader.Instance is NULL.");
            return;
        }

        var artifact = ManifestLoader.Instance.GetArtifactByMarker(markerName);

        if (artifact == null)
        {
            Debug.LogWarning($"[ImageAnchorManager] Marker '{markerName}' " +
                             $"NOT found in manifest. Check marker name matches exactly.");
            return;
        }

        Debug.Log($"[ImageAnchorManager] Found artifact: " +
                  $"'{artifact.name}' (type={artifact.type})");

        if (IsAlreadySpawned(artifact.id))
        {
            // Image was fully removed by ARCore and re-detected as a new trackable.
            // Don't re-spawn — just re-show the existing anchor and re-register
            // the new trackableId so updated/removed events map correctly.
            _trackableToArtifact[trackedImage.trackableId] = artifact.id;
            _spawnedMap[artifact.id] = trackedImage.trackableId;
            ArtifactSpawner.Instance?.Show(artifact.id);
            Debug.Log($"[ImageAnchorManager] '{artifact.id}' re-detected after removal — re-showing.");
            return;
        }

        RegisterSpawned(artifact.id, trackedImage.trackableId);

        if (ArtifactSpawner.Instance == null)
        {
            Debug.LogError("[ImageAnchorManager] ArtifactSpawner.Instance is NULL.");
            return;
        }

        ArtifactSpawner.Instance.Spawn(artifact, trackedImage.transform);
        Debug.Log($"[ImageAnchorManager] Spawning: {artifact.name}");
    }

    private void HandleImageUpdated(ARTrackedImage trackedImage)
    {
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            if (_trackableToArtifact.TryGetValue(
                trackedImage.trackableId, out var artifactId))
            {
                ArtifactSpawner.Instance?.Show(artifactId);
            }
        }
        else if (trackedImage.trackingState == TrackingState.Limited)
        {
            if (_trackableToArtifact.TryGetValue(
                trackedImage.trackableId, out var artifactId))
            {
                var artifact = ManifestLoader.Instance?.GetArtifact(artifactId);
                if (artifact?.tracking_lost_behavior == TrackingLostBehavior.Hide)
                    ArtifactSpawner.Instance?.Hide(artifactId);
            }
        }
    }

    private void HandleImageRemoved(TrackableId trackableId)
    {
        if (!_trackableToArtifact.TryGetValue(trackableId, out var artifactId))
            return;

        var artifact = ManifestLoader.Instance?.GetArtifact(artifactId);
        if (artifact == null) return;

        if (artifact.tracking_lost_behavior == TrackingLostBehavior.Hide)
        {
            ArtifactSpawner.Instance?.Hide(artifactId);
            Debug.Log($"[ImageAnchorManager] Hidden: {artifactId}");
        }
    }

    private void RegisterSpawned(string artifactId, TrackableId trackableId)
    {
        _spawnedMap[artifactId] = trackableId;
        _trackableToArtifact[trackableId] = artifactId;
    }

    private bool IsAlreadySpawned(string artifactId)
        => _spawnedMap.ContainsKey(artifactId);

    public int GetSpawnedCount() => _spawnedMap.Count;
}
