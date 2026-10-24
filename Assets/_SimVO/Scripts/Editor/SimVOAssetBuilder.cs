using System.IO;
using SimVO.Core;
using SimVO.Environment;
using SimVO.Platform;
using SimVO.Targets;
using SimVO.UI;
using SimVO.Weapons;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SimVO.EditorTools
{
    /// <summary>Generates materials (flat and PBR from CC0 sets), target textures, the Cyrillic TMP font and gameplay prefabs.</summary>
    public static class SimVOAssetBuilder
    {
        public const string ThirdParty = SimVOPaths.Root + "/ThirdParty";
        const string PbrDir = SimVOPaths.Materials + "/PBR";

        public static Material Floor, Wall, Ceiling, Table, Rack, GunDark, TargetPaper, SilhouettePaper, TargetFrame, BulletHole, Panel, Button, Marker, Lane, Tracer, Dispenser, ShellRed, Brass, Sign, Clay, ClayDark, Hologram, ConsoleFace, DisplayGlass, StartRed, LampOff, LampGreen, LampRed, Mat, Sandbag, LaserMat, HologramScan, Duck, DuckBeak, ScopeLens, Reticle;
        public static Material ConcreteFloor, ConcreteWallLong, ConcreteWallShort, Plywood, WoodTable, MetalPlate;
        public static Material MatMakarov, MatShotgun, MatSniper, MatM4A1, MatLuger, MatGrease, MatAK47, MatAmmoBox, MatCagedLight, MatWallLamp, MatExtinguisher, MatToolbox;
        public static TMP_FontAsset Font;
        public static GameObject PistolPrefab, CarbinePrefab, ShotgunPrefab, MagazinePrefab, RifleMagazinePrefab, ShellPrefab, RingTargetPrefab, SilhouetteTargetPrefab,
            BulletHolePrefab, HitPopupPrefab, ProjectilePrefab, PanelButtonPrefab, ControlPanelPrefab, ScoreboardPrefab, WeaponWallPrefab, DesktopRigPrefab, TargetCarrierPrefab, CagedLightPrefab, DispenserPrefab, ShellBoxPrefab,
            GongTargetPrefab, ClayTargetPrefab, ClayFragmentPrefab, ClayThrowerPrefab, HologramTargetPrefab, BarricadePrefab, MenuButtonPrefab, MenuSmallButtonPrefab, MenuPanelPrefab,
            DuckPrefab, DuckGalleryPrefab, M4Prefab, AkPrefab, LugerPrefab, SmgPrefab, M4MagazinePrefab, AkMagazinePrefab, LugerMagazinePrefab, SmgMagazinePrefab;

        public static void BuildAll()
        {
            EnsureFolders();
            BuildMaterials();
            BuildPbrMaterials();
            ConfigureAudioImports();
            Font = BuildFont();
            BuildPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SimVO: assets built");
        }

        public static void LoadExisting()
        {
            BuildMaterials();
            BuildPbrMaterials();
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            PistolPrefab = Load("Pistol"); CarbinePrefab = Load("Carbine"); ShotgunPrefab = Load("Shotgun");
            MagazinePrefab = Load("Magazine"); RifleMagazinePrefab = Load("RifleMagazine"); ShellPrefab = Load("Shell");
            RingTargetPrefab = Load("RingTarget"); SilhouetteTargetPrefab = Load("SilhouetteTarget");
            BulletHolePrefab = Load("BulletHole"); HitPopupPrefab = Load("HitPopup"); ProjectilePrefab = Load("Projectile");
            PanelButtonPrefab = Load("PanelButton"); ControlPanelPrefab = Load("ControlPanel"); ScoreboardPrefab = Load("Scoreboard");
            WeaponWallPrefab = Load("WeaponWall"); DesktopRigPrefab = Load("DesktopRig"); TargetCarrierPrefab = Load("TargetCarrier");
            CagedLightPrefab = Load("CagedLight"); DispenserPrefab = Load("MagazineDispenser"); ShellBoxPrefab = Load("ShellBox");
            GongTargetPrefab = Load("GongTarget"); ClayTargetPrefab = Load("ClayTarget"); ClayFragmentPrefab = Load("ClayFragment"); ClayThrowerPrefab = Load("ClayThrower");
            HologramTargetPrefab = Load("HologramTarget"); BarricadePrefab = Load("CoverBarricade"); MenuButtonPrefab = Load("MenuButton"); MenuSmallButtonPrefab = Load("MenuSmallButton"); MenuPanelPrefab = Load("MenuPanel");
            DuckPrefab = Load("DuckTarget"); DuckGalleryPrefab = Load("DuckGallery");
            M4Prefab = Load("M4A1"); AkPrefab = Load("AK47"); LugerPrefab = Load("Luger"); SmgPrefab = Load("M3");
        }

        static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{SimVOPaths.Prefabs}/{name}.prefab");

        static void EnsureFolders()
        {
            foreach (var folder in new[] { SimVOPaths.Root, SimVOPaths.Scenes, SimVOPaths.Prefabs, SimVOPaths.Materials, PbrDir, SimVOPaths.Textures, SimVOPbr.Generated, SimVOPaths.Fonts })
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            }
            AssetDatabase.Refresh();
        }

        // ---------- flat materials ----------

        static void BuildMaterials()
        {
            Floor = Lit("M_Floor", new Color(0.30f, 0.30f, 0.32f), 0f, 0.25f);
            Wall = Lit("M_Wall", new Color(0.62f, 0.62f, 0.60f), 0f, 0.15f);
            Ceiling = Lit("M_Ceiling", new Color(0.16f, 0.17f, 0.19f), 0f, 0.1f);
            Table = Lit("M_Table", new Color(0.42f, 0.30f, 0.20f), 0f, 0.35f);
            Rack = Lit("M_Rack", new Color(0.14f, 0.15f, 0.17f), 0.7f, 0.45f);
            GunDark = Lit("M_GunDark", new Color(0.05f, 0.05f, 0.05f), 0.5f, 0.35f);
            TargetPaper = Lit("M_TargetPaper", Color.white, 0f, 0.05f, RingTexture());
            SilhouettePaper = Lit("M_SilhouettePaper", Color.white, 0f, 0.05f, SilhouetteTexture());
            TargetFrame = Lit("M_TargetFrame", new Color(0.55f, 0.42f, 0.28f), 0f, 0.3f);
            BulletHole = Lit("M_BulletHole", new Color(0.03f, 0.03f, 0.03f), 0f, 0f);
            Panel = Lit("M_Panel", new Color(0.10f, 0.13f, 0.20f), 0.2f, 0.6f);
            Button = Lit("M_Button", new Color(0.90f, 0.70f, 0.15f), 0f, 0.5f);
            Marker = Lit("M_Marker", new Color(0.2f, 0.9f, 0.3f), 0f, 0.4f, null, new Color(0.2f, 0.9f, 0.3f) * 1.5f);
            Lane = Lit("M_Lane", new Color(0.85f, 0.75f, 0.15f), 0f, 0.3f);
            Dispenser = Lit("M_Dispenser", new Color(0.25f, 0.28f, 0.35f), 0.4f, 0.5f);
            ShellRed = Lit("M_ShellRed", new Color(0.75f, 0.12f, 0.1f), 0f, 0.5f);
            Brass = Lit("M_Brass", new Color(0.85f, 0.65f, 0.25f), 0.9f, 0.75f);
            Sign = Lit("M_Sign", new Color(0.92f, 0.92f, 0.9f), 0f, 0.3f);
            Clay = Lit("M_Clay", new Color(0.95f, 0.45f, 0.1f), 0f, 0.35f);
            ClayDark = Lit("M_ClayDark", new Color(0.2f, 0.12f, 0.08f), 0f, 0.3f);
            ConsoleFace = Lit("M_ConsoleFace", new Color(0.09f, 0.1f, 0.11f), 0.3f, 0.45f);
            DisplayGlass = Lit("M_DisplayGlass", new Color(0.02f, 0.05f, 0.03f), 0.1f, 0.95f);
            StartRed = Lit("M_StartRed", new Color(0.85f, 0.08f, 0.06f), 0f, 0.6f);
            LampOff = Lit("M_LampOff", new Color(0.18f, 0.18f, 0.18f), 0f, 0.7f);
            LampGreen = Lit("M_LampGreen", new Color(0.3f, 1f, 0.35f), 0f, 0.7f, null, new Color(0.3f, 1f, 0.35f) * 2.5f);
            LampRed = Lit("M_LampRed", new Color(1f, 0.25f, 0.2f), 0f, 0.7f, null, new Color(1f, 0.25f, 0.2f) * 2.5f);
            Mat = Lit("M_Mat", new Color(0.12f, 0.12f, 0.13f), 0f, 0.2f);
            Duck = Lit("M_Duck", new Color(1f, 0.8f, 0.15f), 0.3f, 0.6f);
            DuckBeak = Lit("M_DuckBeak", new Color(0.95f, 0.45f, 0.1f), 0.3f, 0.5f);
            ScopeLens = Unlit("M_ScopeLens", Color.white);
            ScopeLens.SetTexture("_BaseMap", ScopeRenderTexture());
            Reticle = AlphaUnlit("M_Reticle", ReticleTexture(), new Color(0.05f, 0.05f, 0.05f, 1f));
            Sandbag = Lit("M_Sandbag", new Color(0.6f, 0.52f, 0.36f), 0f, 0.15f);
            Hologram = HologramMaterial();
            Tracer = Unlit("M_Tracer", new Color(1f, 0.85f, 0.4f, 1f));
            LaserMat = Unlit("M_Laser", new Color(1f, 0.1f, 0.05f, 1f));
        }

        static Material Lit(string name, Color color, float metallic, float smoothness, Texture2D baseMap = null, Color? emission = null)
        {
            var path = $"{SimVOPaths.Materials}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (baseMap != null) material.SetTexture("_BaseMap", baseMap);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material AlphaUnlit(string name, Texture2D map, Color color)
        {
            var material = Additive(name, map, color);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 10;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Additive(string name, Texture2D map, Color color)
        {
            var path = $"{SimVOPaths.Materials}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", map);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material HologramMaterial()
        {
            HologramScan = Additive("M_HologramScan", ScanlineTexture(), new Color(0.35f, 0.9f, 1f, 0.35f));
            return Additive("M_Hologram", HologramRingTexture(), new Color(0.35f, 0.9f, 1f, 1f));
        }

        public static RenderTexture ScopeRenderTexture()
        {
            var path = $"{SimVOPaths.Textures}/RT_Scope.renderTexture";
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (existing != null) return existing;
            var rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "RT_Scope", antiAliasing = 1 };
            AssetDatabase.CreateAsset(rt, path);
            return rt;
        }

        static Texture2D ReticleTexture()
        {
            var path = $"{SimVOPaths.Textures}/T_Reticle.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int size = 512;
            var pixels = new Color32[size * size];
            var c = size * 0.5f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x + 0.5f - c; var dy = y + 0.5f - c;
                var d = Mathf.Sqrt(dx * dx + dy * dy);
                var a = 0f;
                if (d < c - 2f)
                {
                    var thick = d < 60f ? 1.2f : 4f;
                    if (Mathf.Abs(dx) < thick || Mathf.Abs(dy) < thick) a = 1f;
                    if (d < 3f) a = 1f;
                    if (Mathf.Abs(d - 160f) < 1.2f) a = 0.8f;
                }
                if (d >= c - 10f && d < c) a = 1f;
                pixels[y * size + x] = new Color32(0, 0, 0, (byte)(a * 255f));
            }
            return SavePng(pixels, size, size, path);
        }

        static Texture2D HologramRingTexture()
        {
            var path = $"{SimVOPaths.Textures}/T_HologramRings.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int size = 1024;
            const int rings = 10;
            var radius = size * 0.5f * (0.25f / 0.3f);
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x + 0.5f - size * 0.5f;
                var dy = y + 0.5f - size * 0.5f;
                var d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                var a = 0f;
                if (d <= 1.02f)
                {
                    var ringPos = d * rings;
                    var frac = ringPos - Mathf.Floor(ringPos);
                    var line = Mathf.Min(frac, 1f - frac) / (0.5f / rings * 0.6f);
                    a = Mathf.Clamp01(1f - line) * 0.9f;
                    if (d < 0.06f) a = 1f;
                    a += 0.08f * (1f - d);
                    var cross = Mathf.Min(Mathf.Abs(dx), Mathf.Abs(dy));
                    if (cross < 1.5f && d < 1f) a = Mathf.Max(a, 0.5f);
                }
                var v = (byte)Mathf.Clamp(a * 255f, 0f, 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, v);
            }
            return SavePng(pixels, size, size, path);
        }

        static Texture2D ScanlineTexture()
        {
            var path = $"{SimVOPaths.Textures}/T_Scanlines.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int size = 256;
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var band = (y % 8) < 3 ? 1f : 0.15f;
                var sweep = Mathf.Clamp01(1f - Mathf.Abs(y - size * 0.5f) / (size * 0.12f));
                var a = Mathf.Clamp01(band * 0.5f + sweep * 0.6f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            var tex = SavePng(pixels, size, size, path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Unlit(string name, Color color)
        {
            var path = $"{SimVOPaths.Materials}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---------- PBR materials from CC0 sets ----------

        static string PH(string set, string map) => $"{ThirdParty}/PolyHaven/Textures/{set}/{set}_{map}_1k.jpg";
        static string Gun(string folder, string file) => $"{ThirdParty}/CC0Guns/{folder}/{file}";
        static string Model(string name) => $"{ThirdParty}/PolyHaven/Models/{name}/{name}_1k.fbx";
        static string ModelTex(string name, string suffix) => $"{ThirdParty}/PolyHaven/Models/{name}/textures/{name}_{suffix}";

        static void BuildPbrMaterials()
        {
            ConcreteFloor = SimVOPbr.FromMaps($"{PbrDir}/M_ConcreteFloor.mat", PH("concrete_floor_worn_001", "diff"), PH("concrete_floor_worn_001", "nor_gl"), null, null, arm: PH("concrete_floor_worn_001", "arm"), tiling: new Vector2(4.6f, 11.5f));
            ConcreteWallLong = SimVOPbr.FromMaps($"{PbrDir}/M_ConcreteWallLong.mat", PH("concrete_wall_003", "diff"), PH("concrete_wall_003", "nor_gl"), null, null, arm: PH("concrete_wall_003", "arm"), tiling: new Vector2(11.5f, 1.7f));
            ConcreteWallShort = SimVOPbr.FromMaps($"{PbrDir}/M_ConcreteWallShort.mat", PH("concrete_wall_003", "diff"), PH("concrete_wall_003", "nor_gl"), null, null, arm: PH("concrete_wall_003", "arm"), tiling: new Vector2(4.6f, 1.7f));
            Plywood = SimVOPbr.FromMaps($"{PbrDir}/M_Plywood.mat", PH("plywood", "diff"), PH("plywood", "nor_gl"), null, null, arm: PH("plywood", "arm"), tiling: new Vector2(1f, 1f));
            WoodTable = SimVOPbr.FromMaps($"{PbrDir}/M_WoodTable.mat", PH("wood_table_001", "diff"), PH("wood_table_001", "nor_gl"), null, null, arm: PH("wood_table_001", "arm"), tiling: new Vector2(1.5f, 0.6f));
            MetalPlate = SimVOPbr.FromMaps($"{PbrDir}/M_MetalPlate.mat", PH("metal_plate", "diff"), PH("metal_plate", "nor_gl"), null, null, arm: PH("metal_plate", "arm"), tiling: new Vector2(3f, 1f));

            MatMakarov = GunMaterial("Makarov", "Makarov", "Makarov_Normal.png");
            MatShotgun = GunMaterial("Shotgun", "Shotgun", "Shotgun_Normal_DirectX.png");
            MatSniper = GunMaterial("Sniper", "Sniper", "Sniper_Normal.png");
            MatM4A1 = GunMaterial("M4A1", "M4A1", "M4A1_Normal.png");
            MatLuger = GunMaterial("Luger", "Luger", "Luger_Normal_DirectX.png");
            MatGrease = GunMaterial("Grease_Gun", "Grease_Gun", "Grease_Gun_Normal_DirectX.png");
            MatAK47 = GunMaterial("AK47", "AK47", "AK47_Normal_DirectX.png");

            MatAmmoBox = PropMaterial("ammo_box", "jpg", false);
            MatCagedLight = PropMaterial("caged_hanging_light", "jpg", true);
            MatWallLamp = PropMaterial("industrial_wall_lamp", "jpg", true);
            MatToolbox = PropMaterial("metal_toolbox", "jpg", false);
            MatExtinguisher = SimVOPbr.FromMaps($"{PbrDir}/M_Extinguisher.mat",
                ModelTex("korean_fire_extinguisher_01", "body_diff_1k.png"), ModelTex("korean_fire_extinguisher_01", "body_nor_gl_1k.png"),
                ModelTex("korean_fire_extinguisher_01", "body_metal_1k.png"), ModelTex("korean_fire_extinguisher_01", "body_rough_1k.png"));
            SimVOPbr.RemapAll(Model("korean_fire_extinguisher_01"), MatExtinguisher);
        }

        static Material GunMaterial(string folder, string prefix, string normalFile)
        {
            var material = SimVOPbr.FromMaps($"{PbrDir}/M_{prefix}.mat", Gun(folder, $"{prefix}_Base_Color.png"), Gun(folder, normalFile), Gun(folder, $"{prefix}_Metallic.png"), Gun(folder, $"{prefix}_Roughness.png"));
            SimVOPbr.RemapAll(Gun(folder, $"{prefix}.fbx"), material);
            return material;
        }

        static Material PropMaterial(string name, string diffExt, bool emissive)
        {
            var metal = ModelTex(name, "metal_1k.exr");
            var rough = ModelTex(name, "rough_1k.exr");
            var material = SimVOPbr.FromMaps($"{PbrDir}/M_{name}.mat", ModelTex(name, $"diff_1k.{diffExt}"), ModelTex(name, "nor_gl_1k.exr"),
                File.Exists(metal) ? metal : null, File.Exists(rough) ? rough : null, null, emissive ? ModelTex(name, "emissive_1k.png") : null);
            SimVOPbr.RemapAll(Model(name), material);
            return material;
        }

        static void ConfigureAudioImports()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { ThirdParty + "/Audio" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.6f;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
        }

        public static AudioClip[] Clips(params string[] names)
        {
            var list = new System.Collections.Generic.List<AudioClip>();
            foreach (var name in names)
            {
                foreach (var guid in AssetDatabase.FindAssets(name + " t:AudioClip"))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                    if (clip != null && clip.name == name) list.Add(clip);
                }
            }
            return list.ToArray();
        }

        // ---------- textures ----------

        static Texture2D RingTexture()
        {
            var path = $"{SimVOPaths.Textures}/T_RingTarget.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int size = 1024;
            const int rings = 10;
            var radius = size * 0.5f * (0.25f / 0.3f);
            var pixels = new Color32[size * size];
            var paper = new Color32(240, 236, 222, 255);
            var ink = new Color32(25, 25, 25, 255);
            var black = new Color32(30, 30, 32, 255);
            var white = new Color32(235, 235, 235, 255);
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x + 0.5f - size * 0.5f;
                var dy = y + 0.5f - size * 0.5f;
                var d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                Color32 color = paper;
                if (d <= 1f)
                {
                    var ringPos = d * rings;
                    var ring = Mathf.FloorToInt(ringPos);
                    var inBlack = ring <= 3;
                    color = inBlack ? black : paper;
                    var frac = ringPos - ring;
                    if (frac < 0.07f || frac > 0.93f) color = inBlack ? white : ink;
                    if (d < 0.05f) color = white;
                }
                else if (d < 1.015f) color = ink;
                pixels[y * size + x] = color;
            }
            return SavePng(pixels, size, size, path);
        }

        static Texture2D SilhouetteTexture()
        {
            var path = $"{SimVOPaths.Textures}/T_SilhouetteTarget.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int w = 640, h = 1024;
            var pixels = new Color32[w * h];
            var paper = new Color32(236, 228, 205, 255);
            var zone5 = new Color32(40, 40, 40, 255);
            var zone3 = new Color32(80, 80, 80, 255);
            var zone1 = new Color32(120, 120, 120, 255);
            var line = new Color32(230, 230, 230, 255);
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var p = new Vector2((x + 0.5f) / w - 0.5f, (y + 0.5f) / h - 0.5f);
                p = new Vector2(p.x * 0.5f, p.y * 0.8f);
                var score = SilhouetteTarget.Score(p);
                Color32 color = score == 5 ? zone5 : score == 3 ? zone3 : score == 1 ? zone1 : paper;
                if (score > 0)
                {
                    var edge = false;
                    foreach (var o in new[] { new Vector2(0.003f, 0f), new Vector2(-0.003f, 0f), new Vector2(0f, 0.003f), new Vector2(0f, -0.003f) })
                    {
                        var s = SilhouetteTarget.Score(p + o);
                        if (s != score && s > 0) edge = true;
                    }
                    if (edge) color = line;
                }
                pixels[y * w + x] = color;
            }
            return SavePng(pixels, w, h, path);
        }

        static Texture2D SavePng(Color32[] pixels, int w, int h, string path)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.maxTextureSize = 1024;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------- font ----------

        const string FontPath = SimVOPaths.Fonts + "/Inter Cyrillic SDF.asset";

        static TMP_FontAsset BuildFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>(SimVOPaths.InterTtf);
            var font = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            font.name = "Inter Cyrillic SDF";
            AssetDatabase.CreateAsset(font, FontPath);
            font.material.name = font.name + " Material";
            font.atlasTexture.name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(font.material, font);
            AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
            font.TryAddCharacters("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
                                  "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?%+-–—·/()«»");
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        }

        // ---------- prefab helpers ----------

        public static GameObject Save(GameObject go, string name)
        {
            var path = $"{SimVOPaths.Prefabs}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler = default, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Empty(string name, Transform parent, Vector3 position, Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localEulerAngles = euler;
            return go;
        }

        public static TextMeshPro Text(string name, Transform parent, Vector3 position, string text, float size, Vector2 rect, Vector3 euler = default, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = Empty(name, parent, position, euler);
            var tmp = go.AddComponent<TextMeshPro>();
            SetFontAndText(tmp, text);
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            tmp.rectTransform.sizeDelta = rect;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        /// <summary>TMP raises a global font-changed event when a dynamic atlas grows; stale listeners from destroyed temp objects throw inside it.</summary>
        public static void SetFontAndText(TMP_Text tmp, string text)
        {
            try { tmp.font = Font; } catch (MissingReferenceException) { }
            try { tmp.text = text; } catch (MissingReferenceException) { }
        }

        public static AudioSource Audio(GameObject go, float spatial = 1f)
        {
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatial;
            source.minDistance = 0.5f;
            source.maxDistance = 30f;
            return source;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        public static GameObject InstantiateModel(string fbxPath, Transform parent, Vector3 position, Vector3 euler, float scale = 1f)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localEulerAngles = euler;
            instance.transform.localScale = Vector3.one * scale;
            return instance;
        }

        public static string PolyHavenModel(string name) => Model(name);
        public static string GunModel(string folder, string file) => Gun(folder, file);

        // ---------- prefabs ----------

        static void BuildPrefabs()
        {
            ProjectilePrefab = Save(BuildProjectile(), "Projectile");
            BulletHolePrefab = Save(BuildBulletHole(), "BulletHole");
            HitPopupPrefab = Save(BuildHitPopup(), "HitPopup");
            ShellPrefab = Save(BuildShell(), "Shell");
            MagazinePrefab = Save(SimVOWeaponBuilder.BuildMagazine("Magazine", "Makarov", "Makarov.fbx", 8, MatMakarov), "Magazine");
            RifleMagazinePrefab = Save(SimVOWeaponBuilder.BuildMagazine("RifleMagazine", "Sniper", "Sniper.fbx", 5, MatSniper), "RifleMagazine");
            PistolPrefab = Save(SimVOWeaponBuilder.BuildPistol(MagazinePrefab, MatMakarov), "Pistol");
            CarbinePrefab = Save(SimVOWeaponBuilder.BuildCarbine(RifleMagazinePrefab, MatSniper), "Carbine");
            ShotgunPrefab = Save(SimVOWeaponBuilder.BuildShotgun(MatShotgun), "Shotgun");
            M4MagazinePrefab = Save(SimVOWeaponBuilder.BuildMagazine("M4Magazine", "M4A1", "M4A1.fbx", 30, MatM4A1), "M4Magazine");
            AkMagazinePrefab = Save(SimVOWeaponBuilder.BuildMagazine("AkMagazine", "AK47", "AK47.fbx", 30, MatAK47), "AkMagazine");
            LugerMagazinePrefab = Save(SimVOWeaponBuilder.BuildMagazine("LugerMagazine", "Luger", "Luger.fbx", 8, MatLuger, "Clip", Vector3.zero), "LugerMagazine");
            SmgMagazinePrefab = Save(SimVOWeaponBuilder.BuildMagazine("SmgMagazine", "Grease_Gun", "Grease_Gun.fbx", 30, MatGrease), "SmgMagazine");
            M4Prefab = Save(SimVOWeaponBuilder.BuildGun(SimVOWeaponBuilder.M4Spec(M4MagazinePrefab, MatM4A1)), "M4A1");
            AkPrefab = Save(SimVOWeaponBuilder.BuildGun(SimVOWeaponBuilder.AkSpec(AkMagazinePrefab, MatAK47)), "AK47");
            LugerPrefab = Save(SimVOWeaponBuilder.BuildGun(SimVOWeaponBuilder.LugerSpec(LugerMagazinePrefab, MatLuger)), "Luger");
            SmgPrefab = Save(SimVOWeaponBuilder.BuildGun(SimVOWeaponBuilder.SmgSpec(SmgMagazinePrefab, MatGrease)), "M3");
            RingTargetPrefab = Save(BuildRingTarget(), "RingTarget");
            SilhouetteTargetPrefab = Save(BuildSilhouetteTarget(), "SilhouetteTarget");
            GongTargetPrefab = Save(SimVOTargetBuilder.BuildGong(), "GongTarget");
            ClayFragmentPrefab = Save(SimVOTargetBuilder.BuildClayFragment(), "ClayFragment");
            ClayTargetPrefab = Save(SimVOTargetBuilder.BuildClay(ClayFragmentPrefab), "ClayTarget");
            ClayThrowerPrefab = Save(SimVOTargetBuilder.BuildThrower(ClayTargetPrefab), "ClayThrower");
            HologramTargetPrefab = Save(SimVOTargetBuilder.BuildHologram(), "HologramTarget");
            DuckPrefab = Save(SimVOTargetBuilder.BuildDuck(), "DuckTarget");
            DuckGalleryPrefab = Save(SimVOTargetBuilder.BuildDuckGallery(DuckPrefab), "DuckGallery");
            BarricadePrefab = Save(SimVOTargetBuilder.BuildBarricade(), "CoverBarricade");
            TargetCarrierPrefab = Save(BuildTargetCarrier(), "TargetCarrier");
            PanelButtonPrefab = Save(BuildPanelButton(), "PanelButton");
            ControlPanelPrefab = Save(SimVOUiBuilder.BuildControlPanel(), "ControlPanel");
            MenuButtonPrefab = Save(SimVOUiBuilder.BuildMenuButton("MenuButton", 0.5f), "MenuButton");
            MenuSmallButtonPrefab = Save(SimVOUiBuilder.BuildMenuButton("MenuSmallButton", 0.09f), "MenuSmallButton");
            MenuPanelPrefab = Save(SimVOUiBuilder.BuildMenuPanel(MenuButtonPrefab, MenuSmallButtonPrefab), "MenuPanel");
            ScoreboardPrefab = Save(BuildScoreboard(), "Scoreboard");
            DispenserPrefab = Save(BuildDispenser("MagazineDispenser", MagazinePrefab, "МАГАЗИНЫ"), "MagazineDispenser");
            ShellBoxPrefab = Save(BuildDispenser("ShellBox", ShellPrefab, "ПАТРОНЫ 12к"), "ShellBox");
            WeaponWallPrefab = Save(SimVOWeaponBuilder.BuildWeaponWall(PistolPrefab, ShotgunPrefab, CarbinePrefab, M4Prefab, AkPrefab, LugerPrefab, SmgPrefab), "WeaponWall");
            CagedLightPrefab = Save(BuildCagedLight(), "CagedLight");
            DesktopRigPrefab = Save(BuildDesktopRig(), "DesktopRig");
        }

        static GameObject BuildProjectile()
        {
            var go = new GameObject("Projectile");
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.08f;
            trail.startWidth = 0.006f;
            trail.endWidth = 0.001f;
            trail.minVertexDistance = 0.05f;
            trail.sharedMaterial = Tracer;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<Projectile>().trail = trail;
            return go;
        }

        static GameObject BuildBulletHole()
        {
            var go = Primitive(PrimitiveType.Quad, "BulletHole", null, Vector3.zero, new Vector3(0.014f, 0.014f, 1f), BulletHole, default, false);
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        static GameObject BuildHitPopup()
        {
            var go = new GameObject("HitPopup");
            var tmp = go.AddComponent<TextMeshPro>();
            SetFontAndText(tmp, "+10");
            tmp.fontSize = 1.4f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(1f, 0.3f);
            go.AddComponent<HitPopup>();
            return go;
        }

        static GameObject BuildShell()
        {
            var root = new GameObject("Shell");
            var body = root.AddComponent<Rigidbody>();
            body.mass = 0.04f;
            body.interpolation = RigidbodyInterpolation.None;
            Primitive(PrimitiveType.Cylinder, "Hull", root.transform, new Vector3(0f, -0.03f, 0f), new Vector3(0.02f, 0.028f, 0.02f), ShellRed, default, false);
            Primitive(PrimitiveType.Cylinder, "Base", root.transform, new Vector3(0f, -0.058f, 0f), new Vector3(0.022f, 0.006f, 0.022f), Brass, default, false);
            var collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, -0.032f, 0f);
            collider.radius = 0.011f;
            collider.height = 0.066f;
            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = false;
            root.AddComponent<Shell>();
            root.AddComponent<DestroyWhenDropped>();
            return root;
        }

        static GameObject HangingTarget(string name, Material paper, Vector2 size)
        {
            var root = new GameObject(name);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(size.x, size.y, 0.02f);
            collider.center = new Vector3(0f, 0f, 0.005f);
            Primitive(PrimitiveType.Quad, "Face", root.transform, Vector3.zero, new Vector3(size.x, size.y, 1f), paper, default, false);
            Primitive(PrimitiveType.Cube, "Backing", root.transform, new Vector3(0f, 0f, 0.012f), new Vector3(size.x + 0.04f, size.y + 0.04f, 0.012f), TargetFrame, default, false);
            Primitive(PrimitiveType.Cube, "ClipLeft", root.transform, new Vector3(-size.x * 0.4f, size.y * 0.5f + 0.01f, 0.006f), new Vector3(0.03f, 0.04f, 0.025f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "ClipRight", root.transform, new Vector3(size.x * 0.4f, size.y * 0.5f + 0.01f, 0.006f), new Vector3(0.03f, 0.04f, 0.025f), Rack, default, false);
            SetLayerRecursive(root, SimVOLayers.Target);
            return root;
        }

        static GameObject BuildRingTarget()
        {
            var root = HangingTarget("RingTarget", TargetPaper, new Vector2(0.6f, 0.6f));
            var target = root.AddComponent<RingTarget>();
            target.radius = 0.25f;
            target.audioSource = Audio(root);
            return root;
        }

        static GameObject BuildSilhouetteTarget()
        {
            var root = HangingTarget("SilhouetteTarget", SilhouettePaper, new Vector2(0.5f, 0.8f));
            var target = root.AddComponent<SilhouetteTarget>();
            target.audioSource = Audio(root);
            return root;
        }

        static GameObject BuildTargetCarrier()
        {
            var root = new GameObject("TargetCarrier");
            Primitive(PrimitiveType.Cube, "Rail", root.transform, new Vector3(0f, 0.02f, 8.6f), new Vector3(0.1f, 0.05f, 16.4f), MetalPlate);
            var trolley = Empty("Trolley", root.transform, new Vector3(0f, 0f, 1.2f));
            Primitive(PrimitiveType.Cube, "Body", trolley.transform, new Vector3(0f, -0.07f, 0f), new Vector3(0.24f, 0.1f, 0.32f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "Motor", trolley.transform, new Vector3(0.1f, -0.09f, -0.08f), new Vector3(0.08f, 0.07f, 0.1f), Dispenser, default, false);
            Primitive(PrimitiveType.Cylinder, "WheelL", trolley.transform, new Vector3(-0.09f, 0.02f, 0.1f), new Vector3(0.05f, 0.012f, 0.05f), GunDark, new Vector3(0f, 0f, 90f), false);
            Primitive(PrimitiveType.Cylinder, "WheelR", trolley.transform, new Vector3(0.09f, 0.02f, 0.1f), new Vector3(0.05f, 0.012f, 0.05f), GunDark, new Vector3(0f, 0f, 90f), false);
            var frame = Empty("Frame", trolley.transform, new Vector3(0f, -0.12f, 0f));
            Primitive(PrimitiveType.Cylinder, "RodL", frame.transform, new Vector3(-0.2f, -0.65f, 0f), new Vector3(0.012f, 0.65f, 0.012f), Rack, default, false);
            Primitive(PrimitiveType.Cylinder, "RodR", frame.transform, new Vector3(0.2f, -0.65f, 0f), new Vector3(0.012f, 0.65f, 0.012f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "Crossbar", frame.transform, new Vector3(0f, -1.3f, 0f), new Vector3(0.5f, 0.025f, 0.025f), Rack, default, false);
            var mount = Empty("TargetMount", frame.transform, new Vector3(0f, -1.64f, 0f));
            mount.AddComponent<TargetMotion>();
            var carrier = root.AddComponent<TargetCarrier>();
            carrier.trolley = trolley.transform;
            carrier.frame = frame.transform;
            carrier.targetMount = mount.transform;
            carrier.audioSource = Audio(trolley, 1f);
            carrier.audioSource.volume = 0.35f;
            return root;
        }

        static GameObject BuildPanelButton()
        {
            var root = new GameObject("PanelButton");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.13f, 0.03f, 0.09f);
            collider.center = new Vector3(0f, 0.015f, 0f);
            var cap = Primitive(PrimitiveType.Cube, "Cap", root.transform, new Vector3(0f, 0.0125f, 0f), new Vector3(0.125f, 0.025f, 0.085f), Button, default, false);
            var label = Text("Label", root.transform, new Vector3(0f, 0.027f, 0f), "Кнопка", 0.2f, new Vector2(0.12f, 0.08f), new Vector3(90f, 0f, 0f), new Color(0.1f, 0.1f, 0.1f));
            root.AddComponent<XRSimpleInteractable>();
            root.AddComponent<UnityEngine.XR.Interaction.Toolkit.Filtering.XRPokeFilter>();
            var button = root.AddComponent<PanelButton>();
            button.label = label;
            button.cap = cap.transform;
            button.audioSource = Audio(root);
            return root;
        }

        static GameObject BuildScoreboard()
        {
            var root = new GameObject("Scoreboard");
            Primitive(PrimitiveType.Cube, "Board", root.transform, new Vector3(0f, 0f, 0.03f), new Vector3(1.8f, 1.0f, 0.04f), Panel);
            Primitive(PrimitiveType.Cube, "Bezel", root.transform, new Vector3(0f, 0f, 0.045f), new Vector3(1.86f, 1.06f, 0.03f), Rack, default, false);
            var board = root.AddComponent<Scoreboard>();
            board.title = Text("Title", root.transform, new Vector3(0f, 0.4f, 0f), "SimVO", 0.7f, new Vector2(1.7f, 0.15f));
            board.scoreText = Text("Score", root.transform, new Vector3(0f, 0.17f, 0f), "0", 2.2f, new Vector2(1.7f, 0.3f), default, new Color(1f, 0.85f, 0.2f));
            board.scoreText.fontStyle = FontStyles.Bold;
            board.statsText = Text("Stats", root.transform, new Vector3(0f, -0.22f, 0f), "", 0.62f, new Vector2(1.7f, 0.45f));
            board.audioSource = Audio(root, 0.5f);
            return root;
        }

        static GameObject BuildDispenser(string name, GameObject itemPrefab, string label)
        {
            var root = new GameObject(name);
            var box = Primitive(PrimitiveType.Cube, "Box", root.transform, new Vector3(0f, 0.045f, 0f), new Vector3(0.14f, 0.09f, 0.1f), Dispenser);
            Primitive(PrimitiveType.Cube, "Lip", root.transform, new Vector3(0f, 0.095f, -0.045f), new Vector3(0.14f, 0.01f, 0.01f), Rack, default, false);
            var socketGo = Empty("Socket", root.transform, new Vector3(0f, 0.15f, 0f));
            var trigger = socketGo.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.06f;
            var socket = socketGo.AddComponent<MagazineSocket>();
            socket.lockWhenInserted = false;
            socket.showInteractableHoverMeshes = false;
            if (itemPrefab.GetComponent<Shell>() != null)
            {
                Object.DestroyImmediate(socket);
                var shellSocket = socketGo.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
                shellSocket.showInteractableHoverMeshes = false;
                var d = box.AddComponent<MagazineDispenser>();
                d.socket = shellSocket;
                d.itemPrefab = itemPrefab;
            }
            else
            {
                var d = box.AddComponent<MagazineDispenser>();
                d.socket = socket;
                d.itemPrefab = itemPrefab;
            }
            Text("Label", root.transform, new Vector3(0f, 0.05f, -0.051f), label, 0.1f, new Vector2(0.13f, 0.05f), default, Color.white);
            return root;
        }

        static GameObject BuildCagedLight()
        {
            var root = new GameObject("CagedLight");
            InstantiateModel(Model("caged_hanging_light"), root.transform, Vector3.zero, Vector3.zero, 0.6f);
            var light = Empty("Light", root.transform, new Vector3(0f, -0.35f, 0f)).AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 3.4f;
            light.color = new Color(1f, 0.93f, 0.8f);
            light.shadows = LightShadows.None;
            return root;
        }

        static GameObject BuildDesktopRig()
        {
            var root = new GameObject("DesktopRig");
            var controller = root.AddComponent<CharacterController>();
            controller.radius = 0.3f;
            controller.height = 1.75f;
            controller.center = new Vector3(0f, 0.875f, 0f);
            var camGo = Empty("Camera", root.transform, new Vector3(0f, 1.65f, 0f));
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
            var hand = Empty("HandAnchor", camGo.transform, new Vector3(0.14f, -0.16f, 0.42f));
            var crosshair = Text("Crosshair", camGo.transform, new Vector3(0f, 0f, 0.5f), "+", 0.3f, new Vector2(0.2f, 0.1f), default, new Color(1f, 1f, 1f, 0.8f));
            crosshair.gameObject.layer = SimVOLayers.IgnoreRaycast;
            var player = root.AddComponent<DesktopPlayer>();
            player.cam = cam;
            player.handAnchor = hand.transform;
            SetLayerRecursive(root, SimVOLayers.IgnoreRaycast);
            return root;
        }
    }
}
