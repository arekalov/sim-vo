using System.IO;
using UnityEditor;
using UnityEngine;

namespace SimVO.EditorTools
{
    /// <summary>Builds URP Lit materials from CC0 texture sets (Poly Haven, 3dmodelscc0) and remaps FBX materials.</summary>
    public static class SimVOPbr
    {
        public const string Generated = SimVOPaths.Root + "/Textures/Generated";

        public static Material FromMaps(string materialPath, string baseColor, string normal, string metallic, string roughness, string ao = null, string emissive = null, Vector2? tiling = null, float fallbackMetallic = 0f, float fallbackSmoothness = 0.5f, string arm = null)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(materialPath));
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var key = Path.GetFileNameWithoutExtension(materialPath);
            material.SetColor("_BaseColor", Color.white);
            if (baseColor != null) material.SetTexture("_BaseMap", LoadTexture(baseColor, false));
            if (normal != null)
            {
                var normalPath = normal;
                if (normal.Contains("DirectX")) normalPath = FlipGreen(normal, $"{Generated}/{key}_normal_gl.png");
                material.SetTexture("_BumpMap", LoadTexture(normalPath, true));
                material.EnableKeyword("_NORMALMAP");
            }
            string packed = null;
            if (arm != null) packed = PackArm(arm, $"{Generated}/{key}_ms.png");
            else if (metallic != null || roughness != null) packed = PackMetallicRoughness(metallic, roughness, $"{Generated}/{key}_ms.png");
            if (packed != null)
            {
                material.SetTexture("_MetallicGlossMap", LoadTexture(packed, false, linear: true));
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_Smoothness", 1f);
            }
            else
            {
                material.SetFloat("_Metallic", fallbackMetallic);
                material.SetFloat("_Smoothness", fallbackSmoothness);
            }
            if (ao != null || arm != null)
            {
                var aoPath = arm != null ? ExtractChannel(arm, 0, $"{Generated}/{key}_ao.png") : ao;
                material.SetTexture("_OcclusionMap", LoadTexture(aoPath, false, linear: true));
                material.EnableKeyword("_OCCLUSIONMAP");
            }
            if (emissive != null)
            {
                material.SetTexture("_EmissionMap", LoadTexture(emissive, false));
                material.SetColor("_EmissionColor", Color.white * 2.5f);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (tiling.HasValue) material.SetTextureScale("_BaseMap", tiling.Value);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Points every material slot of an FBX at one generated material.</summary>
        public static void RemapAll(string fbxPath, Material material)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            if (importer == null) return;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                foreach (var m in renderer.sharedMaterials)
                    if (m != null) names.Add(m.name);
            foreach (var name in names)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
            importer.SaveAndReimport();
        }

        public static Texture2D LoadTexture(string path, bool normalMap, bool linear = false)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                var changed = false;
                var wantType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (importer.textureType != wantType) { importer.textureType = wantType; changed = true; }
                if (!normalMap && importer.sRGBTexture == linear) { importer.sRGBTexture = !linear; changed = true; }
                if (importer.maxTextureSize > 2048) { importer.maxTextureSize = 2048; changed = true; }
                if (changed) importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Texture2D Readable(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && (!importer.isReadable || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.textureType == TextureImporterType.NormalMap))
            {
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static string WritePng(Texture2D texture, string outPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllBytes(outPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(outPath);
            return outPath;
        }

        static string FlipGreen(string normalPath, string outPath)
        {
            if (File.Exists(outPath)) return outPath;
            var src = Readable(normalPath);
            var pixels = src.GetPixels32();
            for (var i = 0; i < pixels.Length; i++) pixels[i].g = (byte)(255 - pixels[i].g);
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            return WritePng(tex, outPath);
        }

        static string PackMetallicRoughness(string metallicPath, string roughnessPath, string outPath)
        {
            if (File.Exists(outPath)) return outPath;
            var metallic = metallicPath != null ? Readable(metallicPath) : null;
            var roughness = roughnessPath != null ? Readable(roughnessPath) : null;
            var reference = metallic != null ? metallic : roughness;
            var w = reference.width; var h = reference.height;
            var m = metallic != null ? Resize(metallic, w, h) : null;
            var r = roughness != null ? Resize(roughness, w, h) : null;
            var pixels = new Color32[w * h];
            for (var i = 0; i < pixels.Length; i++)
            {
                var met = m != null ? m[i].r : (byte)0;
                var smooth = r != null ? (byte)(255 - r[i].r) : (byte)128;
                pixels[i] = new Color32(met, met, met, smooth);
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            return WritePng(tex, outPath);
        }

        static string PackArm(string armPath, string outPath)
        {
            if (File.Exists(outPath)) return outPath;
            var arm = Readable(armPath);
            var src = arm.GetPixels32();
            var pixels = new Color32[src.Length];
            for (var i = 0; i < src.Length; i++) pixels[i] = new Color32(src[i].b, src[i].b, src[i].b, (byte)(255 - src[i].g));
            var tex = new Texture2D(arm.width, arm.height, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            return WritePng(tex, outPath);
        }

        static string ExtractChannel(string path, int channel, string outPath)
        {
            if (File.Exists(outPath)) return outPath;
            var src = Readable(path);
            var s = src.GetPixels32();
            var pixels = new Color32[s.Length];
            for (var i = 0; i < s.Length; i++)
            {
                var v = channel == 0 ? s[i].r : channel == 1 ? s[i].g : s[i].b;
                pixels[i] = new Color32(v, v, v, 255);
            }
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            return WritePng(tex, outPath);
        }

        static Color32[] Resize(Texture2D source, int w, int h)
        {
            var src = source.GetPixels32();
            if (source.width == w && source.height == h) return src;
            var dst = new Color32[w * h];
            for (var y = 0; y < h; y++)
            {
                var sy = y * source.height / h;
                for (var x = 0; x < w; x++) dst[y * w + x] = src[sy * source.width + x * source.width / w];
            }
            return dst;
        }
    }
}
