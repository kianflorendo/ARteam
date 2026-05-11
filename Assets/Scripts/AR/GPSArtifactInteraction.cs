// ============================================================
// GPSArtifactInteraction.cs
// Location: Assets/Scripts/AR/GPSArtifactInteraction.cs
// Mt. Samat AR Scavenger Hunt — Terra App
//
// Attached by ArtifactSpawner to GPS collectible prefabs.
// Two-finger touch rotates the 3D model on X and Y axes.
//
// Uses UnityEngine.InputSystem.EnhancedTouch directly because
// activeInputHandler = 1 (New Input System only) in ProjectSettings.
// Legacy Input.touchCount and LeanTouch both return 0 under this
// setting unless __INPUTSYSTEM__ define is also added — it is not.
// ============================================================

using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class GPSArtifactInteraction : MonoBehaviour
{
    [Header("Rotation")]
    public float rotationSensitivity = 0.4f;
    public float xClampMin = -60f;
    public float xClampMax = 60f;

    private float _currentX;
    private float _currentY;

    private void Awake()
    {
        // Safe to call multiple times — EnhancedTouch checks internally.
        EnhancedTouchSupport.Enable();
    }

    private void Start()
    {
        var euler = transform.localEulerAngles;
        _currentX = euler.x > 180f ? euler.x - 360f : euler.x;
        _currentY = euler.y;
    }

    private void Update()
    {
        var touches = EnhancedTouch.activeTouches;

        if (touches.Count < 2)
            return;

        var t0 = touches[0];
        var t1 = touches[1];

        // Skip if either finger is ending this frame.
        if (t0.phase == InputTouchPhase.Ended || t0.phase == InputTouchPhase.Canceled ||
            t1.phase == InputTouchPhase.Ended || t1.phase == InputTouchPhase.Canceled)
            return;

        // Average both fingers' deltas for smooth, stable rotation.
        Vector2 avgDelta = (t0.delta + t1.delta) * 0.5f;

        if (avgDelta.sqrMagnitude < 0.001f)
            return;

        _currentY += avgDelta.x * rotationSensitivity;

        _currentX -= avgDelta.y * rotationSensitivity;
        _currentX  = Mathf.Clamp(_currentX, xClampMin, xClampMax);

        transform.localRotation = Quaternion.Euler(_currentX, _currentY, 0f);
    }
}
