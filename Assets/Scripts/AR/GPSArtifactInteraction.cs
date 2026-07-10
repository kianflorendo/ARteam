using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Uses UnityEngine.InputSystem.EnhancedTouch directly because activeInputHandler = 1
// (New Input System only) in ProjectSettings. Legacy Input.touchCount and LeanTouch
// both return 0 under this setting unless __INPUTSYSTEM__ define is also added — it is not.
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

        if (t0.phase == InputTouchPhase.Ended || t0.phase == InputTouchPhase.Canceled ||
            t1.phase == InputTouchPhase.Ended || t1.phase == InputTouchPhase.Canceled)
            return;

        Vector2 avgDelta = (t0.delta + t1.delta) * 0.5f;

        if (avgDelta.sqrMagnitude < 0.001f)
            return;

        _currentY += avgDelta.x * rotationSensitivity;
        _currentX -= avgDelta.y * rotationSensitivity;
        _currentX  = Mathf.Clamp(_currentX, xClampMin, xClampMax);

        transform.localRotation = Quaternion.Euler(_currentX, _currentY, 0f);
    }
}
