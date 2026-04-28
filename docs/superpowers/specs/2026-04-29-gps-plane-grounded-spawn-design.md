# GPS Plane-Grounded Spawn — Design Spec
**Date:** 2026-04-29  
**Status:** Approved by user — awaiting implementation  
**Feature:** Plane-anchored GPS artifact spawning with shadow disc and long-press Y-rotation  

---

## Summary

Replace the current `camera_forward` GPS spawn (object floats 1m in front of camera at eye level) with a `detected_plane` mode where:

- Artifact spawns on a detected AR ground plane, hovering 0.3m above it
- A shadow disc sits on the ground beneath the artifact to visually anchor it
- Object is world-locked — stays in place as user walks around it
- User can long-press + drag to rotate the artifact on its Y-axis
- 2-second plane scan with graceful fallback to estimated ground if no plane found

---

## Architecture

### New Files

| File | Purpose |
|------|---------|
| `Assets/Scripts/AR/ARPlaneGrounder.cs` | Singleton plane detection service |
| `Assets/Scripts/AR/ArtifactRotationInteraction.cs` | Long-press Y-rotation component |

### Modified Files

| File | Change |
|------|--------|
| `Assets/Scripts/AR/OfflineGPSRouteManager.cs` | `PresentArtifact()` — new `detected_plane` branch |
| `Assets/Scripts/AR/ArtifactSpawner.cs` | Add `ArtifactRotationInteraction` after GPS collectible spawn |
| `Assets/StreamingAssets/manifest.json` | GPS-TEST-001/002/003 `spawn_presentation` → `"detected_plane"` |

### Scene Changes (manual, one-time in Unity Editor)

- XR Origin → add **ARRaycastManager** component
- XR Origin → add **ARPlaneManager** component (enables horizontal plane detection)
- Both are AR Foundation components already in the project package

---

## Component: ARPlaneGrounder

**Location:** `Assets/Scripts/AR/ARPlaneGrounder.cs`  
**Pattern:** Singleton MonoBehaviour, DontDestroyOnLoad  
**Auto-discovers** `ARRaycastManager` via `FindFirstObjectByType` in `Start()`

### Public API

```
StartGroundScan(Vector3 fromPosition, float timeout, Action<Vector3> onResult)
```

### Internal flow

```
GroundScanCoroutine:
  every 0.1s:
    cast Ray(fromPosition + Vector3.up*2, Vector3.down)
    against TrackableType.PlaneWithinPolygon | PlaneWithinBounds
    if hit → onResult(hitPoint), stop coroutine
  after timeout (2s):
    onResult(EstimatedGround(fromPosition)), stop coroutine

EstimatedGround(Vector3 pos):
  → new Vector3(pos.x, Camera.main.transform.position.y − 1.5f, pos.z)
```

### Error handling

- `ARRaycastManager` null → log warning, call `onResult(EstimatedGround)` immediately
- Camera null at fallback time → use `Vector3.zero` Y as last resort

---

## Component: OfflineGPSRouteManager — detected_plane path

`PresentArtifact()` gains a new branch. **The `camera_forward` path is not modified.**

### New flow when `spawn_presentation == "detected_plane"`

```
1. Compute candidatePos:
     XZ: camera.position + flatForward * spawnDistance
     Y:  camera.position.y  (ARPlaneGrounder raycasts downward anyway)

2. ARPlaneGrounder.Instance.StartGroundScan(candidatePos, 2f, groundPoint =>
   {
     if (anchorObject already destroyed / artifact already collected) → return

     anchorObject.transform.position = groundPoint + Vector3.up * 0.3f
     anchorObject.transform.rotation = Quaternion.Euler(0, camera.eulerAngles.y + 180f, 0)

     CreateShadowDisc(artifact.id, anchorObject.transform)
     ArtifactSpawner.Instance.Spawn(artifact, anchorObject.transform)
   })
```

The anchor and shadow disc are created **inside the callback** — they only exist once a ground point is confirmed.

---

## Component: Shadow Disc

Child `GameObject` of the anchor, placed at `localPosition = (0, -0.3f, 0)` so it sits exactly on the ground plane surface.

| Property | Value |
|----------|-------|
| Name | `ShadowDisc_{artifactId}` |
| Mesh | Cylinder primitive, scaled `(0.5f, 0.001f, 0.5f)` — extremely flat |
| Material | URP/Unlit, `Color = new Color(0.05f, 0.05f, 0.05f, 1f)` — opaque dark |
| Parent | `anchorObject.transform` |
| Local Position | `(0, -0.3f, 0)` |

Opaque (no transparency) to avoid URP shader property fragility. Destroyed automatically with the anchor on artifact collect.

---

## Component: ArtifactRotationInteraction

**Location:** `Assets/Scripts/AR/ArtifactRotationInteraction.cs`  
Added by `ArtifactSpawner` to every GPS collectible after spawn. Uses Lean Touch static events — no extra Lean Touch components needed on prefabs.

### State

```
bool       _rotating      = false
LeanFinger _activeFinger  = null
```

### Behaviour

```
OnEnable  → subscribe LeanTouch.OnFingerUpdate, OnFingerUp
OnDisable → unsubscribe, clear state

OnFingerUpdate(LeanFinger finger):
  if finger.IsOverGui              → return
  if LeanTouch.Fingers.Count > 1  → return   (two-finger gesture: ignore)
  if !_rotating && finger.Old     → _rotating=true, _activeFinger=finger
  if _rotating && finger==_activeFinger:
    transform.Rotate(0f, -finger.ScreenDelta.x * 0.3f, 0f, Space.World)

OnFingerUp(LeanFinger finger):
  if finger == _activeFinger → _rotating=false, _activeFinger=null
```

`finger.Old` is Lean Touch's built-in long-press flag — true once finger held > `LeanTouch.TapThreshold` (default 0.2s).  
Rotation sensitivity: `0.3 degrees per pixel` of horizontal drag.

---

## ArtifactSpawner change

In `SpawnCoroutine()`, after `instance.Initialise()`:

```csharp
if (artifact.anchor_mode == AnchorMode.GPS
    && artifact.type == ArtifactType.Collectible)
{
    if (spawnedObject.GetComponent<ArtifactRotationInteraction>() == null)
        spawnedObject.AddComponent<ArtifactRotationInteraction>();
}
```

---

## Manifest Change

GPS-TEST-001, GPS-TEST-002, GPS-TEST-003:
```json
"spawn_presentation": "detected_plane"
```
(changed from `"camera_forward"`)

---

## Error Handling Matrix

| Scenario | Handling |
|----------|----------|
| `ARRaycastManager` not in scene | Log warning, fall back to estimated ground immediately |
| Plane never detected in 2s | Estimated ground used — artifact always spawns, never blocked |
| Camera null in callback | Skip spawn, log error |
| `LeanTouch` not in scene | Static events never fire — rotation silently inactive, no crash |
| Duplicate scan for same artifact | `_presentationAnchors` guard in `OfflineGPSRouteManager` prevents re-entry |
| Artifact collected during scan | Null-check anchor in callback before placing — silently exits |
| Multi-finger touch during rotation | `LeanTouch.Fingers.Count > 1` check — rotation disengages |

---

## What Is NOT Changed

- Image-tracking artifacts (`anchor_mode == "image"`) — completely unaffected
- `camera_forward` GPS artifacts — existing path unchanged
- `ArtifactSpawner.Spawn()` signature — unchanged
- `GPSRouteStateStore`, `InventoryManager`, `CollectionController` — unchanged
- Despawn / Hide / Show logic — unchanged

---

## Implementation Order (for writing-plans)

1. `ARPlaneGrounder.cs` — standalone, no dependencies on other new code
2. `ArtifactRotationInteraction.cs` — standalone
3. `OfflineGPSRouteManager.cs` — depends on `ARPlaneGrounder`
4. `ArtifactSpawner.cs` — depends on `ArtifactRotationInteraction`
5. `manifest.json` — update test artifact `spawn_presentation` values
6. Scene setup instructions (documented, manual step)
