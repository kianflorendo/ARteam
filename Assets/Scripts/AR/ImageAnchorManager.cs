// ============================================================
// ImageAnchorManager.cs - IMPROVED VERSION WITH DEBUG LOGGING
// Location: Assets/Scripts/AR/ImageAnchorManager.cs
// Mt. Samat AR Scavenger Hunt -- Terra App
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageAnchorManager : MonoBehaviour
{
    // -- References --
    [Header("AR References")]
    [Tooltip("Assign the ARTrackedImageManager from XR Origin here")]
    public ARTrackedImageManager imageManager;

    // -- Spawned tracking --
    private Dictionary<string, TrackableId> _spawnedMap
        = new Dictionary<string, TrackableId>();

    private Dictionary<TrackableId, string> _trackableToArtifact
        = new Dictionary<TrackableId, string>();

    // ============================================================
    //  Unity lifecycle
    // ============================================================

    private void Awake()
    {
        if (imageManager == null)
            imageManager = FindAnyObjectByType<ARTrackedImageManager>();

        if (imageManager == null)
            Debug.LogError("[ImageAnchorManager] ARTrackedImageManager not found!");
        else
            Debug.Log("[ImageAnchorManager] ✅ ARTrackedImageManager found: "
                      + imageManager.name);
    }

    private void Start()
    {
        // Log all reference images in the library for debugging
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
            Debug.LogError("[ImageAnchorManager] ❌ Reference library is NULL! " +
                           "Assign MtSamatImageLibrary to ARTrackedImageManager!");
        }

        // Test lookup for each image in library against manifest
        if (ManifestLoader.Instance != null)
        {
            Debug.Log("[ImageAnchorManager] ManifestLoader is ready");
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
            Debug.LogError("[ImageAnchorManager] ManifestLoader.Instance is NULL!");
        }
    }

    private void OnEnable()
    {
        if (imageManager != null)
        {
            imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
            Debug.Log("[ImageAnchorManager] ✅ Subscribed to trackablesChanged");
        }
        ManifestLoader.OnManifestLoaded += OnManifestLoaded;
    }

    private void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        ManifestLoader.OnManifestLoaded -= OnManifestLoaded;
    }

    // If the manifest finishes loading AFTER an image was already detected,
    // HandleImageAdded returned null at that point and the spawn was skipped.
    // This re-processes any currently-tracked images that were missed.
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
            if (trackedImage.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.None)
                continue;
            // Skip if already registered (spawn already happened or in progress)
            if (_trackableToArtifact.ContainsKey(trackedImage.trackableId))
                continue;
            Debug.Log($"[ImageAnchorManager] Retrying missed image: " +
                      $"'{trackedImage.referenceImage.name}'");
            HandleImageAdded(trackedImage);
        }
    }

    // ============================================================
    //  Main event handler
    // ============================================================

    private void OnTrackablesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        // Images added
        foreach (var trackedImage in args.added)
        {
            Debug.Log($"[ImageAnchorManager] 📸 IMAGE DETECTED: " +
                      $"'{trackedImage.referenceImage.name}' " +
                      $"state={trackedImage.trackingState}");
            HandleImageAdded(trackedImage);
        }

        // Images updated
        foreach (var trackedImage in args.updated)
        {
            HandleImageUpdated(trackedImage);
        }

        // Images removed
        foreach (var kvp in args.removed)
        {
            HandleImageRemoved(kvp.Key);
        }
    }

    // ============================================================
    //  Added
    // ============================================================

    private void HandleImageAdded(ARTrackedImage trackedImage)
    {
        string markerName = trackedImage.referenceImage.name;

        Debug.Log($"[ImageAnchorManager] Looking up marker: '{markerName}'");

        if (ManifestLoader.Instance == null)
        {
            Debug.LogError("[ImageAnchorManager] ❌ ManifestLoader.Instance is NULL!");
            return;
        }

        var artifact = ManifestLoader.Instance.GetArtifactByMarker(markerName);

        if (artifact == null)
        {
            Debug.LogWarning($"[ImageAnchorManager] ⚠️ Marker '{markerName}' " +
                             $"NOT found in manifest! Check marker name matches exactly.");
            return;
        }

        Debug.Log($"[ImageAnchorManager] ✅ Found artifact: " +
                  $"'{artifact.name}' (type={artifact.type})");

        if (IsAlreadySpawned(artifact.id))
        {
            Debug.Log($"[ImageAnchorManager] '{artifact.id}' already spawned. Skipping.");
            return;
        }

        RegisterSpawned(artifact.id, trackedImage.trackableId);

        if (ArtifactSpawner.Instance == null)
        {
            Debug.LogError("[ImageAnchorManager] ❌ ArtifactSpawner.Instance is NULL!");
            return;
        }

        ArtifactSpawner.Instance.Spawn(artifact, trackedImage.transform);
        Debug.Log($"[ImageAnchorManager] ✅ Spawning: {artifact.name}");
    }

    // ============================================================
    //  Updated
    // ============================================================

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

    // ============================================================
    //  Removed
    // ============================================================

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

    // ============================================================
    //  Helpers
    // ============================================================

    private void RegisterSpawned(string artifactId, TrackableId trackableId)
    {
        _spawnedMap[artifactId] = trackableId;
        _trackableToArtifact[trackableId] = artifactId;
    }

    private bool IsAlreadySpawned(string artifactId)
        => _spawnedMap.ContainsKey(artifactId);

    public int GetSpawnedCount() => _spawnedMap.Count;
}