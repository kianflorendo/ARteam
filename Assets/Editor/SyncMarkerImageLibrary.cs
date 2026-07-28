using System.IO;
using UnityEditor;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

// Rebuilds MtSamatImageLibrary.asset FROM SCRATCH, sourcing its entries
// directly from manifest.json so the library can never drift out of sync with
// the game data again.
//
// History: the asset was accidentally deleted while syncing the 2026-07-25
// marker replacement and had to be recreated. Unity assigned it a new guid
// (9e60743abe089184a8d48883be365501) rather than adopting the original one, so
// SampleScene.unity's ARTrackedImageManager.m_SerializedLibrary was repointed
// to match. ExpectedGuid below is just a safety check for future reruns --
// if this asset is ever deleted and recreated again, whatever guid it gets
// will need the same scene-reference fixup repeated.
public static class SyncMarkerImageLibrary
{
    private const string LibraryPath = "Assets/MtSamatImageLibrary.asset";
    private const string ExpectedGuid = "9e60743abe089184a8d48883be365501";
    private const string ReferenceImagesFolder = "Assets/ReferenceImages";
    private const string ManifestPath = "Assets/StreamingAssets/manifest.json";

    [MenuItem("Tools/Mt. Samat/Rebuild Marker Image Library From Manifest")]
    public static void Rebuild()
    {
        if (!File.Exists(ManifestPath))
        {
            Debug.LogError($"[SyncMarkerImageLibrary] Manifest not found at {ManifestPath}.");
            return;
        }

        var manifest = JsonUtility.FromJson<ManifestData>(File.ReadAllText(ManifestPath));
        if (manifest?.artifacts == null)
        {
            Debug.LogError("[SyncMarkerImageLibrary] Failed to parse manifest.json.");
            return;
        }

        XRReferenceImageLibrary library;
        bool alreadyExisted = File.Exists(LibraryPath);
        if (alreadyExisted)
        {
            library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
            for (int i = library.count - 1; i >= 0; i--)
                library.RemoveAt(i);
        }
        else
        {
            library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        int added = 0;
        var missing = new System.Collections.Generic.List<string>();
        var seen = new System.Collections.Generic.HashSet<string>();

        foreach (var artifact in manifest.artifacts)
        {
            if (artifact.anchor_mode != "image" || string.IsNullOrEmpty(artifact.marker_name))
                continue;
            if (!seen.Add(artifact.marker_name))
                continue; // some markers are shared by multiple artifacts

            var texturePath = $"{ReferenceImagesFolder}/{artifact.marker_name}.jpg";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                missing.Add(artifact.marker_name);
                continue;
            }

            library.Add();
            int index = library.count - 1;
            library.SetName(index, artifact.marker_name);
            library.SetTexture(index, texture, keepTexture: false);
            library.SetSpecifySize(index, true);
            library.SetSize(index, new Vector2(0.5f, 0.8888889f));
            added++;
        }

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string actualGuid = AssetDatabase.AssetPathToGUID(LibraryPath);
        string guidStatus = actualGuid == ExpectedGuid
            ? "guid matches scene reference ✓"
            : $"WARNING: guid is {actualGuid}, expected {ExpectedGuid} -- scene reference will be broken!";

        Debug.Log($"[SyncMarkerImageLibrary] Rebuilt library ({(alreadyExisted ? "updated existing" : "created new")} asset). "
            + $"Added {added} entries. {guidStatus}");
        if (missing.Count > 0)
        {
            Debug.LogWarning("[SyncMarkerImageLibrary] Skipped markers with no matching image file in "
                + $"{ReferenceImagesFolder}: {string.Join(", ", missing)}");
        }
    }
}
