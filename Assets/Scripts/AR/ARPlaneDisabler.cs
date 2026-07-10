using UnityEngine;
using UnityEngine.XR.ARFoundation;

// The app uses GPS distance-based spawning and image tracking — not plane detection.
// ARPlaneManager is enabled by default in the XR Origin prefab and would otherwise
// render detected floor/wall/table planes as large white mesh overlays on top of the
// camera feed. This script permanently disables it and kills any pre-spawned planes.
[DefaultExecutionOrder(-130)]
public class ARPlaneDisabler : MonoBehaviour
{
    private ARPlaneManager _planeManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (FindAnyObjectByType<ARPlaneDisabler>() != null) return;
        DontDestroyOnLoad(new GameObject("[AUTO] ARPlaneDisabler", typeof(ARPlaneDisabler)));
    }

    private void Start()
    {
        _planeManager = FindAnyObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        if (_planeManager == null) return;

        _planeManager.enabled = false;

        foreach (var plane in _planeManager.trackables)
            Destroy(plane.gameObject);

        // Belt-and-suspenders: hide any that slip through during disable.
        _planeManager.trackablesChanged.AddListener(OnPlanesChanged);

        Debug.Log("[ARPlaneDisabler] ARPlaneManager disabled — plane meshes will not render.");
    }

    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        foreach (var plane in args.added)
            plane.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_planeManager != null)
            _planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }
}
