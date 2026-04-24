// ============================================================
// ScrollFollowCamera.cs
// Location: Assets/Scripts/UI/ScrollFollowCamera.cs
// Mt. Samat AR Scavenger Hunt - Terra App
//
// Keeps the scroll billboard-facing the camera (Y-axis only)
// and smoothly positioned so it is always readable.
//
// Two position modes:
//   1. Close anchors (for image tracking and locally presented GPS artifacts):
//      float 1.5 m in front of the camera so the scroll stays readable.
//   2. Mid-range anchors: position beside the artifact anchor.
// ============================================================

using UnityEngine;

public class ScrollFollowCamera : MonoBehaviour
{
    private Transform _anchor;
    private float _offsetRight = 0.5f;
    private float _offsetUp = 0f;

    private const float IMAGE_TRACKING_RANGE   =  2f;

    public void SetAnchor(Transform anchor, float offsetRight, float offsetUp)
    {
        _anchor = anchor;
        _offsetRight = offsetRight;
        _offsetUp = offsetUp;
    }

    void LateUpdate()
    {
        if (_anchor == null || Camera.main == null) return;

        float distToAnchor = Vector3.Distance(Camera.main.transform.position, _anchor.position);

        Vector3 target;
        if (distToAnchor < IMAGE_TRACKING_RANGE)
        {
            // Close anchors are easier to read with a camera-relative panel.
            target = Camera.main.transform.position
                + Camera.main.transform.forward * 1.5f
                + Vector3.up * 0.1f;
        }
        else
        {
            // Mid-range anchors keep the scroll near the object.
            target = _anchor.position
                + _anchor.right * _offsetRight
                + Vector3.up * _offsetUp;
        }

        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 8f);

        // Y-axis billboard - face camera, never tilt
        Vector3 lookDir = Camera.main.transform.position - transform.position;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(-lookDir);
    }
}
