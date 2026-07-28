using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// One-click generator for the Main Menu title's TMP Font Asset (Arial Black SDF).
// Run once via Tools → Mt. Samat → Generate Title Font Asset, then drag the
// resulting asset into UIHierarchySetup's Title Font field in the Inspector.
//
// Uses a STATIC atlas with all needed characters pre-baked at generation time —
// Dynamic atlas population was tried first but proved unreliable: it produced
// garbled/mismatched glyphs in the Editor and a blank title on-device, because
// glyph rendering happens lazily at runtime and can race/fail. Static bakes
// everything deterministically right now, so there's nothing left to fail later.
public static class GenerateTitleFontAsset
{
    private const string SourceFontPath = "Assets/Fonts/ArialBlack.ttf";
    private const string OutputAssetPath = "Assets/Fonts/ArialBlack SDF.asset";

    // Every character actually used by MainMenuScreen's TMP text, plus the common
    // ASCII printable range so this font asset is safe to reuse elsewhere later.
    private const string RequiredCharacters =
        " !\"#$%&'()*+,-./0123456789:;<=>?@" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
        "abcdefghijklmnopqrstuvwxyz{|}~" +
        "·"; // middle dot, used in "v1.2.0 · CLASSIFIED"

    [MenuItem("Tools/Mt. Samat/Generate Title Font Asset")]
    public static void Generate()
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"[GenerateTitleFontAsset] Source font not found at {SourceFontPath}.");
            return;
        }

        // Delete any previous (possibly broken) asset at this path first — leftover
        // sub-assets from a failed prior generation can otherwise linger and conflict.
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputAssetPath) != null)
            AssetDatabase.DeleteAsset(OutputAssetPath);

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            samplingPointSize: 90,
            atlasPadding: 9,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 1024,
            atlasHeight: 1024,
            atlasPopulationMode: AtlasPopulationMode.Dynamic, // populate now, freeze to Static below
            enableMultiAtlasSupport: true);

        if (fontAsset == null)
        {
            Debug.LogError("[GenerateTitleFontAsset] TMP_FontAsset.CreateFontAsset returned null.");
            return;
        }

        AssetDatabase.CreateAsset(fontAsset, OutputAssetPath);

        // CRITICAL: persist the generated atlas texture(s) and material as sub-assets.
        // Without this they only exist in memory — the font asset serializes fine but
        // its glyph data points at objects that vanish on the next domain reload,
        // producing garbled glyphs in-editor and a blank/broken font in builds.
        if (fontAsset.atlasTextures != null)
        {
            foreach (var tex in fontAsset.atlasTextures)
                if (tex != null)
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
        }
        if (fontAsset.material != null)
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        // Bake every character we actually use into the atlas right now, deterministically.
        bool allAdded = fontAsset.TryAddCharacters(RequiredCharacters, out string missingCharacters);
        if (!allAdded)
            Debug.LogWarning($"[GenerateTitleFontAsset] Some characters failed to bake: {missingCharacters}");

        // Freeze — no further runtime/edit-time glyph generation, so nothing can
        // corrupt or fail to render later.
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GenerateTitleFontAsset] ✅ Created {OutputAssetPath} with a static, pre-baked atlas. " +
                  "Drag it onto UIHierarchySetup → Title Font, then tick rebuildMainMenuScreen.");

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(OutputAssetPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
