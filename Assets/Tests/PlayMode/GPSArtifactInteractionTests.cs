// ============================================================
// GPSArtifactInteractionTests.cs
// Location: Assets/Tests/PlayMode/GPSArtifactInteractionTests.cs
// Play Mode tests for GPSArtifactInteraction.
// Run via: Window → General → Test Runner → PlayMode → Run All
// ============================================================

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GPSArtifactInteractionTests
{
    // ── Collider setup ────────────────────────────────────────────

    [UnityTest]
    public IEnumerator EnsureCollider_AddsBoxCollider_WhenNoColliderPresent()
    {
        var go = new GameObject("TestArtifact_NoCollider");
        go.AddComponent<GPSArtifactInteraction>();
        yield return null; // one frame — Start() runs

        Assert.IsNotNull(
            go.GetComponent<BoxCollider>(),
            "GPSArtifactInteraction.Start() must add a BoxCollider when none is present.");

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator EnsureCollider_DoesNotAddDuplicate_WhenColliderAlreadyExists()
    {
        var go = new GameObject("TestArtifact_ExistingCollider");
        go.AddComponent<BoxCollider>();                  // pre-existing collider
        go.AddComponent<GPSArtifactInteraction>();
        yield return null; // one frame — Start() runs

        var colliders = go.GetComponents<Collider>();
        Assert.AreEqual(
            1,
            colliders.Length,
            "GPSArtifactInteraction must not add a second Collider when one already exists.");

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator EnsureCollider_UsesFallbackSize_WhenNoRenderersPresent()
    {
        var go = new GameObject("TestArtifact_NoRenderer"); // no MeshRenderer
        go.AddComponent<GPSArtifactInteraction>();
        yield return null; // one frame — Start() runs

        var col = go.GetComponent<BoxCollider>();
        Assert.IsNotNull(col, "BoxCollider must be added even when no Renderer is present.");
        Assert.AreEqual(
            new Vector3(1.2f, 1.2f, 1.2f),
            col.size,
            "Fallback BoxCollider size must be Vector3.one * 1.2f (matches ArtifactSpawner.targetModelSize).");

        Object.Destroy(go);
    }

    // ── Initial rotation state ────────────────────────────────────

    [UnityTest]
    public IEnumerator Start_PreservesIdentityRotation_WhenSpawnedWithNoRotation()
    {
        var go = new GameObject("TestArtifact_ZeroRotation");
        // Spawn with identity rotation — same as ArtifactSpawner does
        go.transform.localRotation = Quaternion.identity;
        go.AddComponent<GPSArtifactInteraction>();
        yield return null; // one frame — Start() runs

        // Start() reads localEulerAngles but does NOT write localRotation —
        // rotation only changes when the user drags. Identity must be preserved.
        Assert.AreEqual(
            Quaternion.identity,
            go.transform.localRotation,
            "Start() must not modify localRotation — initial rotation must remain Quaternion.identity.");

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator Start_PreservesNonZeroRotation_WhenSpawnedWithExistingRotation()
    {
        var go = new GameObject("TestArtifact_NonZeroRotation");
        var spawnRotation = Quaternion.Euler(0f, 180f, 0f); // ArtifactSpawner sets 180° Y
        go.transform.localRotation = spawnRotation;
        go.AddComponent<GPSArtifactInteraction>();
        yield return null; // one frame — Start() runs

        Assert.AreEqual(
            spawnRotation,
            go.transform.localRotation,
            "Start() must not modify localRotation — spawn rotation must be preserved.");

        Object.Destroy(go);
    }
}
