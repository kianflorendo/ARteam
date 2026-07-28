using UnityEditor;
using UnityEngine;

// Wraps PreLoginGroup/MainAppGroup/AR panels in a centered 390-wide DesignRoot —
// directly inside the UI_Canvas PREFAB ASSET, not a scene instance.
//
// Why this has to be a separate Editor tool: Unity refuses to reparent any
// GameObject that originates from a prefab while you're editing an *instance*
// of that prefab in a scene ("Setting the parent of a transform which resides
// in a Prefab instance is not possible"). UIHierarchySetup's checkbox-driven
// fixAspectRatioCentering ran against the scene instance and silently failed
// this way every time, while still logging a false "success". Editing the
// prefab asset's own contents via PrefabUtility.LoadPrefabContents sidesteps
// the restriction entirely, since there's no "instance" involved.
public static class FixAspectRatioCentering
{
    private const string PrefabPath = "Assets/Prefabs/UI/UI_Canvas.prefab";
    private const float DESIGN_WIDTH = 390f;

    [MenuItem("Tools/Mt. Samat/Fix Aspect Ratio Centering (Prefab)")]
    public static void Fix()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"[FixAspectRatioCentering] Could not load prefab at {PrefabPath}.");
            return;
        }

        try
        {
            var canvas = root.transform; // root of this prefab IS UI_Canvas

            var designRoot = canvas.Find("DesignRoot");
            if (designRoot == null)
            {
                var go = new GameObject("DesignRoot", typeof(RectTransform));
                go.transform.SetParent(canvas, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(DESIGN_WIDTH, 0f);
                go.transform.SetAsFirstSibling();
                designRoot = go.transform;
            }

            string[] namesToWrap = { "PreLoginGroup", "MainAppGroup", "AR_DebugPanel", "AR_ActionPanel", "AR_CollectBanner" };
            int moved = 0;
            foreach (var n in namesToWrap)
            {
                var child = canvas.Find(n);
                if (child == null) continue;
                child.SetParent(designRoot, worldPositionStays: false);
                moved++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

            Debug.Log($"[FixAspectRatioCentering] ✅ {moved} group(s) moved into DesignRoot directly inside the prefab asset. " +
                      "All existing component references (NavigationManager, controllers) remain valid — reparenting doesn't " +
                      "change GameObject identity. Now use File → Revert on the open scene (to discard the broken leftover " +
                      "empty DesignRoot instance) and reopen it fresh.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
