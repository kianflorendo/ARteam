// Assets/Editor/GlbWebPConverter.cs
// Converts embedded WebP textures inside GLB files to PNG so that
// GLTFast can import them correctly in Unity 6 on Windows.
// Run: Tools → Fix WebP Textures in GLBs
// After running, Unity will auto-reimport all modified GLBs.
// Then rebuild Addressables and rebuild the APK.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class GlbWebPConverter
{
    const uint MAGIC_GLTF = 0x46546C67; // "glTF"
    const uint TYPE_JSON  = 0x4E4F534A; // "JSON"
    const uint TYPE_BIN   = 0x004E4942; // "BIN\0"

    [MenuItem("Tools/Fix WebP Textures in GLBs")]
    public static void ConvertAll()
    {
        string dir = Path.Combine(Application.dataPath, "Models", "Artifacts");
        if (!Directory.Exists(dir))
        {
            EditorUtility.DisplayDialog("Error", $"Folder not found:\n{dir}", "OK");
            return;
        }

        string[] files = Directory.GetFiles(dir, "*.glb", SearchOption.TopDirectoryOnly);
        int converted = 0, skipped = 0, failed = 0;

        try
        {
            for (int fi = 0; fi < files.Length; fi++)
            {
                string f = files[fi];
                bool cancel = EditorUtility.DisplayCancelableProgressBar(
                    "Converting WebP → PNG in GLBs",
                    Path.GetFileName(f), (float)fi / files.Length);
                if (cancel) break;

                try
                {
                    string r = ProcessGlb(f);
                    if (r == "converted") converted++;
                    else skipped++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GlbWebPConverter] {Path.GetFileName(f)}: {e.Message}");
                    failed++;
                }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.Refresh();
        Debug.Log($"[GlbWebPConverter] Done — Converted:{converted} Skipped:{skipped} Failed:{failed}");
        EditorUtility.DisplayDialog("GLB WebP Fix",
            $"Converted: {converted} file(s)\nSkipped (no WebP): {skipped}\nErrors: {failed}\n\nNext steps:\n1. Wait for Unity to reimport the GLBs\n2. Rebuild Addressables\n3. Rebuild APK", "OK");
    }

    // Returns "converted" or "skipped". Throws on hard error.
    static string ProcessGlb(string path)
    {
        byte[] raw = File.ReadAllBytes(path);
        if (raw.Length < 12 || R32(raw, 0) != MAGIC_GLTF)
            throw new Exception("Not a valid GLB");

        // JSON chunk
        int p = 12;
        int jsonLen = (int)R32(raw, p); p += 4;
        if (R32(raw, p) != TYPE_JSON) throw new Exception("Expected JSON chunk first");
        p += 4;
        string json = Encoding.UTF8.GetString(raw, p, jsonLen);
        p += jsonLen;

        // BIN chunk (may be absent for URI-based assets)
        if (p + 8 > raw.Length) return "skipped";
        int binLen = (int)R32(raw, p); p += 4;
        if (R32(raw, p) != TYPE_BIN) return "skipped";
        p += 4;
        byte[] bin = new byte[binLen];
        Buffer.BlockCopy(raw, p, bin, 0, binLen);

        // Parse the minimal glTF arrays we need
        var imgObjs = GetObjects(json, "images");
        var bvObjs  = GetObjects(json, "bufferViews");
        if (imgObjs == null || bvObjs == null || imgObjs.Count == 0) return "skipped";

        // Find all WebP images and convert them
        var pngMap = new Dictionary<int, byte[]>(); // bufferView index → PNG bytes
        foreach (string img in imgObjs)
        {
            if (GetStr(img, "mimeType") != "image/webp") continue;
            int bvi = GetInt(img, "bufferView", -1);
            if (bvi < 0 || bvi >= bvObjs.Count) continue;

            int off = GetInt(bvObjs[bvi], "byteOffset", 0);
            int len = GetInt(bvObjs[bvi], "byteLength", 0);
            if (off + len > bin.Length) continue;

            byte[] webpBytes = new byte[len];
            Buffer.BlockCopy(bin, off, webpBytes, 0, len);

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool ok = tex.LoadImage(webpBytes); // Unity 6 supports WebP here
            if (ok)
            {
                pngMap[bvi] = tex.EncodeToPNG();
                Debug.Log($"[GlbWebPConverter] {Path.GetFileName(path)} bv[{bvi}]: " +
                          $"WebP {len / 1024}KB → PNG {pngMap[bvi].Length / 1024}KB");
            }
            else
            {
                Debug.LogWarning($"[GlbWebPConverter] {Path.GetFileName(path)} bv[{bvi}]: " +
                                 $"LoadImage failed for WebP (check Unity 6 WebP support)");
            }
            UnityEngine.Object.DestroyImmediate(tex);
        }

        if (pngMap.Count == 0) return "skipped";

        // Rebuild BIN: copy all bufferView data, swap WebP → PNG where converted
        var bvData = new byte[bvObjs.Count][];
        for (int i = 0; i < bvObjs.Count; i++)
        {
            if (pngMap.ContainsKey(i))
            {
                bvData[i] = pngMap[i];
            }
            else
            {
                int off = GetInt(bvObjs[i], "byteOffset", 0);
                int len = GetInt(bvObjs[i], "byteLength", 0);
                bvData[i] = Slice(bin, off, len);
            }
        }

        // Layout new BIN sequentially with 4-byte alignment
        var newBinMs = new MemoryStream();
        var newOff = new int[bvObjs.Count];
        var newLen = new int[bvObjs.Count];
        for (int i = 0; i < bvObjs.Count; i++)
        {
            AlignTo4(newBinMs, 0x00);
            newOff[i] = (int)newBinMs.Length;
            newLen[i] = bvData[i].Length;
            newBinMs.Write(bvData[i], 0, bvData[i].Length);
        }
        AlignTo4(newBinMs, 0x00);
        byte[] newBin = newBinMs.ToArray();

        // Patch JSON
        string newJson = json;
        newJson = PatchImages(newJson, imgObjs, pngMap.Keys);
        newJson = PatchBufferViews(newJson, bvObjs, newOff, newLen);
        newJson = PatchBuffers(newJson, newBin.Length);

        // Pad JSON to 4 bytes with spaces (GLB spec)
        byte[] jBytes = Encoding.UTF8.GetBytes(newJson);
        int jPad = (4 - jBytes.Length % 4) % 4;
        byte[] jPadded = new byte[jBytes.Length + jPad];
        Array.Copy(jBytes, jPadded, jBytes.Length);
        for (int i = jBytes.Length; i < jPadded.Length; i++) jPadded[i] = 0x20;

        // Write new GLB
        int total = 12 + 8 + jPadded.Length + 8 + newBin.Length;
        using var ms = new MemoryStream(total);
        W32(ms, MAGIC_GLTF); W32(ms, 2); W32(ms, (uint)total);
        W32(ms, (uint)jPadded.Length); W32(ms, TYPE_JSON);
        ms.Write(jPadded, 0, jPadded.Length);
        W32(ms, (uint)newBin.Length); W32(ms, TYPE_BIN);
        ms.Write(newBin, 0, newBin.Length);

        File.WriteAllBytes(path, ms.ToArray());
        return "converted";
    }

    // ── GLB binary I/O ───────────────────────────────────────────────

    static uint R32(byte[] d, int i) =>
        (uint)(d[i] | d[i+1]<<8 | d[i+2]<<16 | d[i+3]<<24);

    static void W32(Stream s, uint v)
    {
        s.WriteByte((byte)v);
        s.WriteByte((byte)(v >> 8));
        s.WriteByte((byte)(v >> 16));
        s.WriteByte((byte)(v >> 24));
    }

    static void AlignTo4(MemoryStream ms, byte fill)
    {
        int n = (4 - (int)(ms.Length % 4)) % 4;
        for (int i = 0; i < n; i++) ms.WriteByte(fill);
    }

    static byte[] Slice(byte[] src, int off, int len)
    {
        if (off < 0 || len <= 0 || off + len > src.Length) return Array.Empty<byte>();
        var b = new byte[len];
        Buffer.BlockCopy(src, off, b, 0, len);
        return b;
    }

    // ── Minimal JSON helpers ─────────────────────────────────────────

    // Extracts top-level JSON object strings from a named array
    static List<string> GetObjects(string json, string key)
    {
        int kp = json.IndexOf($"\"{key}\"", StringComparison.Ordinal);
        if (kp < 0) return null;
        int ap = json.IndexOf('[', kp + key.Length + 2);
        if (ap < 0) return null;

        int depth = 0, ep = ap;
        for (int i = ap; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '[' || c == '{') depth++;
            else if (c == ']' || c == '}') { depth--; if (depth == 0) { ep = i; break; } }
        }

        var result = new List<string>();
        int d2 = 0, sp = -1;
        for (int i = ap + 1; i < ep; i++)
        {
            char c = json[i];
            if (c == '{') { if (d2++ == 0) sp = i; }
            else if (c == '}') { if (--d2 == 0 && sp >= 0) { result.Add(json.Substring(sp, i - sp + 1)); sp = -1; } }
        }
        return result;
    }

    static string GetStr(string obj, string key)
    {
        var m = Regex.Match(obj, $"\"{Regex.Escape(key)}\"\\s*:\\s*\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : null;
    }

    static int GetInt(string obj, string key, int def = 0)
    {
        var m = Regex.Match(obj, $"\"{Regex.Escape(key)}\"\\s*:\\s*(-?\\d+)");
        return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : def;
    }

    // Rebuild images array, changing mimeType for converted bufferViews
    static string PatchImages(string json, List<string> imgs, IEnumerable<int> convertedBvis)
    {
        var converted = new HashSet<int>(convertedBvis);
        var sb = new StringBuilder("[");
        for (int i = 0; i < imgs.Count; i++)
        {
            if (i > 0) sb.Append(',');
            string img = imgs[i];
            int bvi = GetInt(img, "bufferView", -1);
            if (converted.Contains(bvi))
                img = Regex.Replace(img,
                    "\"mimeType\"\\s*:\\s*\"image/webp\"",
                    "\"mimeType\":\"image/png\"");
            sb.Append(img);
        }
        sb.Append(']');
        return SpliceArray(json, "images", sb.ToString());
    }

    // Rebuild bufferViews array with updated byteOffset + byteLength
    static string PatchBufferViews(string json, List<string> bvs, int[] offsets, int[] lengths)
    {
        var sb = new StringBuilder("[");
        for (int i = 0; i < bvs.Count; i++)
        {
            if (i > 0) sb.Append(',');
            string bv = bvs[i];
            // Update or insert byteOffset
            if (Regex.IsMatch(bv, "\"byteOffset\""))
                bv = Regex.Replace(bv, "\"byteOffset\"\\s*:\\s*-?\\d+", $"\"byteOffset\":{offsets[i]}");
            else
                bv = bv.Insert(1, $"\"byteOffset\":{offsets[i]},");
            // Update byteLength
            bv = Regex.Replace(bv, "\"byteLength\"\\s*:\\s*\\d+", $"\"byteLength\":{lengths[i]}");
            sb.Append(bv);
        }
        sb.Append(']');
        return SpliceArray(json, "bufferViews", sb.ToString());
    }

    // Update buffer[0].byteLength to the new BIN size
    static string PatchBuffers(string json, int newByteLength)
    {
        int bp = json.IndexOf("\"buffers\"", StringComparison.Ordinal);
        if (bp < 0) return json;

        // Find the first { in the buffers array
        int ap = json.IndexOf('[', bp + 9);
        if (ap < 0) return json;
        int op = json.IndexOf('{', ap);
        if (op < 0) return json;

        // Find the closing } of this object
        int d = 0, ep = op;
        for (int i = op; i < json.Length; i++)
        {
            if (json[i] == '{') d++;
            else if (json[i] == '}') { d--; if (d == 0) { ep = i; break; } }
        }

        string bufObj = json.Substring(op, ep - op + 1);
        var m = Regex.Match(bufObj, "\"byteLength\"\\s*:\\s*(\\d+)");
        if (!m.Success) return json;

        int rel = m.Index + m.Value.IndexOf(m.Groups[1].Value, StringComparison.Ordinal);
        int abs = op + rel;
        return json.Substring(0, abs) + newByteLength + json.Substring(abs + m.Groups[1].Length);
    }

    // Replace the content of a named JSON array in-place
    static string SpliceArray(string json, string key, string newArray)
    {
        int kp = json.IndexOf($"\"{key}\"", StringComparison.Ordinal);
        if (kp < 0) return json;
        int ap = json.IndexOf('[', kp + key.Length + 2);
        if (ap < 0) return json;

        int depth = 0, ep = ap;
        for (int i = ap; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '[' || c == '{') depth++;
            else if (c == ']' || c == '}') { depth--; if (depth == 0) { ep = i; break; } }
        }
        return json.Substring(0, ap) + newArray + json.Substring(ep + 1);
    }
}
