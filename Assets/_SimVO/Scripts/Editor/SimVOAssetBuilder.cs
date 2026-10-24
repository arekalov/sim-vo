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
    /// <summary>Generates placeholder materials, the ring target texture, a Cyrillic TMP font and all gameplay prefabs.</summary>
    public static class SimVOAssetBuilder
    {
        public static Material Floor, Wall, Ceiling, Table, Rack, Gun, GunDark, MagazineMat, TargetPaper, TargetFrame, BulletHole, Panel, Button, Marker, Lane, Tracer, Dispenser;
        public static TMP_FontAsset Font;
        public static GameObject PistolPrefab, MagazinePrefab, RingTargetPrefab, BulletHolePrefab, HitPopupPrefab, ProjectilePrefab, PanelButtonPrefab, ControlPanelPrefab, ScoreboardPrefab, WeaponStandPrefab, DesktopRigPrefab;

        public static void BuildAll()
        {
            EnsureFolders();
            BuildMaterials();
            Font = BuildFont();
            BuildPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("SimVO: assets built");
        }

        public static void LoadExisting()
        {
            BuildMaterials();
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            PistolPrefab = Load("Pistol");
            MagazinePrefab = Load("Magazine");
            RingTargetPrefab = Load("RingTarget");
            BulletHolePrefab = Load("BulletHole");
            HitPopupPrefab = Load("HitPopup");
            ProjectilePrefab = Load("Projectile");
            PanelButtonPrefab = Load("PanelButton");
            ControlPanelPrefab = Load("ControlPanel");
            ScoreboardPrefab = Load("Scoreboard");
            WeaponStandPrefab = Load("WeaponStand");
            DesktopRigPrefab = Load("DesktopRig");
        }

        static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{SimVOPaths.Prefabs}/{name}.prefab");

        static void EnsureFolders()
        {
            foreach (var folder in new[] { SimVOPaths.Root, SimVOPaths.Scenes, SimVOPaths.Prefabs, SimVOPaths.Materials, SimVOPaths.Textures, SimVOPaths.Fonts })
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
                }
            }
        }

        // ---------- materials ----------

        static void BuildMaterials()
        {
            Floor = Lit("M_Floor", new Color(0.30f, 0.30f, 0.32f), 0f, 0.25f);
            Wall = Lit("M_Wall", new Color(0.62f, 0.62f, 0.60f), 0f, 0.15f);
            Ceiling = Lit("M_Ceiling", new Color(0.45f, 0.45f, 0.47f), 0f, 0.1f);
            Table = Lit("M_Table", new Color(0.42f, 0.30f, 0.20f), 0f, 0.35f);
            Rack = Lit("M_Rack", new Color(0.18f, 0.19f, 0.21f), 0.7f, 0.5f);
            Gun = Lit("M_Gun", new Color(0.10f, 0.10f, 0.11f), 0.75f, 0.55f);
            GunDark = Lit("M_GunDark", new Color(0.05f, 0.05f, 0.05f), 0.5f, 0.35f);
            MagazineMat = Lit("M_Magazine", new Color(0.22f, 0.22f, 0.24f), 0.8f, 0.5f);
            TargetPaper = Lit("M_TargetPaper", Color.white, 0f, 0.05f, RingTexture());
            TargetFrame = Lit("M_TargetFrame", new Color(0.55f, 0.42f, 0.28f), 0f, 0.3f);
            BulletHole = Lit("M_BulletHole", new Color(0.03f, 0.03f, 0.03f), 0f, 0f);
            Panel = Lit("M_Panel", new Color(0.12f, 0.16f, 0.24f), 0.2f, 0.6f);
            Button = Lit("M_Button", new Color(0.90f, 0.70f, 0.15f), 0f, 0.5f);
            Marker = Lit("M_Marker", new Color(0.2f, 0.9f, 0.3f), 0f, 0.4f, null, new Color(0.2f, 0.9f, 0.3f) * 1.5f);
            Lane = Lit("M_Lane", new Color(0.85f, 0.85f, 0.2f), 0f, 0.3f);
            Dispenser = Lit("M_Dispenser", new Color(0.25f, 0.28f, 0.35f), 0.4f, 0.5f);
            Tracer = Unlit("M_Tracer", new Color(1f, 0.85f, 0.4f, 1f));
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

        static Texture2D RingTexture()
        {
            var path = $"{SimVOPaths.Textures}/T_RingTarget.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int size = 1024;
            const int rings = 10;
            // Quad is 0.6 m wide, scoring radius 0.25 m.
            var radius = size * 0.5f * (0.25f / 0.3f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var paper = new Color32(245, 242, 232, 255);
            var ink = new Color32(20, 20, 20, 255);
            var centre = new Color32(210, 40, 35, 255);
            var nine = new Color32(30, 30, 30, 255);
            for (var y = 0; y < size; y++)
            {
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
                        if (ring == 0) color = centre;
                        else if (ring == 1) color = nine;
                        var frac = ringPos - ring;
                        if (frac < 0.035f || frac > 0.965f) color = ring <= 1 ? paper : ink;
                    }
                    pixels[y * size + x] = color;
                }
            }
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
            if (source == null)
            {
                Debug.LogError("SimVO: Inter-Regular.ttf not found");
                return null;
            }
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

        // ---------- prefabs ----------

        static void BuildPrefabs()
        {
            ProjectilePrefab = Save(BuildProjectile(), "Projectile");
            BulletHolePrefab = Save(BuildBulletHole(), "BulletHole");
            HitPopupPrefab = Save(BuildHitPopup(), "HitPopup");
            MagazinePrefab = Save(BuildMagazine(), "Magazine");
            PistolPrefab = Save(BuildPistol(), "Pistol");
            RingTargetPrefab = Save(BuildRingTarget(), "RingTarget");
            PanelButtonPrefab = Save(BuildPanelButton(), "PanelButton");
            ControlPanelPrefab = Save(BuildControlPanel(), "ControlPanel");
            ScoreboardPrefab = Save(BuildScoreboard(), "Scoreboard");
            WeaponStandPrefab = Save(BuildWeaponStand(), "WeaponStand");
            DesktopRigPrefab = Save(BuildDesktopRig(), "DesktopRig");
        }

        static GameObject Save(GameObject go, string name)
        {
            var path = $"{SimVOPaths.Prefabs}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler = default, bool keepCollider = true)
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

        static GameObject Empty(string name, Transform parent, Vector3 position, Vector3 euler = default)
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
            tmp.font = Font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color ?? Color.white;
            tmp.rectTransform.sizeDelta = rect;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        static AudioSource Audio(GameObject go, float spatial = 1f)
        {
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatial;
            source.minDistance = 0.5f;
            source.maxDistance = 30f;
            return source;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
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
            var projectile = go.AddComponent<Projectile>();
            projectile.trail = trail;
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
            tmp.font = Font;
            tmp.text = "+10";
            tmp.fontSize = 1.4f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(1f, 0.3f);
            go.AddComponent<HitPopup>();
            return go;
        }

        static GameObject BuildMagazine()
        {
            var root = new GameObject("Magazine");
            var body = root.AddComponent<Rigidbody>();
            body.mass = 0.15f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Primitive(PrimitiveType.Cube, "Body", root.transform, new Vector3(0f, -0.0475f, 0f), new Vector3(0.024f, 0.095f, 0.036f), MagazineMat);
            Primitive(PrimitiveType.Cube, "Base", root.transform, new Vector3(0f, -0.098f, 0f), new Vector3(0.03f, 0.008f, 0.044f), GunDark, default, false);
            var grab = root.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = true;
            grab.useDynamicAttach = true;
            root.AddComponent<Magazine>();
            root.AddComponent<DestroyWhenDropped>();
            return root;
        }

        static GameObject BuildPistol()
        {
            var root = new GameObject("Pistol");
            var body = root.AddComponent<Rigidbody>();
            body.mass = 0.9f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var slide = Primitive(PrimitiveType.Cube, "Slide", root.transform, new Vector3(0f, 0.04f, 0.05f), new Vector3(0.032f, 0.035f, 0.19f), Gun);
            Primitive(PrimitiveType.Cube, "Frame", root.transform, new Vector3(0f, 0.012f, 0.045f), new Vector3(0.03f, 0.022f, 0.17f), GunDark, default, false);
            Primitive(PrimitiveType.Cylinder, "Barrel", root.transform, new Vector3(0f, 0.04f, 0.15f), new Vector3(0.012f, 0.008f, 0.012f), GunDark, new Vector3(90f, 0f, 0f), false);
            var grip = Primitive(PrimitiveType.Cube, "Grip", root.transform, new Vector3(0f, -0.04f, -0.015f), new Vector3(0.03f, 0.11f, 0.045f), GunDark, new Vector3(-12f, 0f, 0f));
            Primitive(PrimitiveType.Cube, "TriggerGuard", root.transform, new Vector3(0f, -0.015f, 0.035f), new Vector3(0.008f, 0.03f, 0.04f), GunDark, default, false);
            Primitive(PrimitiveType.Cube, "Sight", root.transform, new Vector3(0f, 0.062f, 0.135f), new Vector3(0.006f, 0.01f, 0.006f), GunDark, default, false);
            Primitive(PrimitiveType.Cube, "RearSight", root.transform, new Vector3(0f, 0.062f, -0.03f), new Vector3(0.02f, 0.01f, 0.006f), GunDark, default, false);

            var muzzle = Empty("Muzzle", root.transform, new Vector3(0f, 0.04f, 0.158f));
            var attach = Empty("Attach", root.transform, new Vector3(0f, -0.03f, -0.015f));
            var light = Empty("MuzzleFlash", muzzle.transform, Vector3.zero).AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 1.5f;
            light.intensity = 4f;
            light.color = new Color(1f, 0.8f, 0.5f);
            light.enabled = false;

            var socketGo = Empty("MagazineSocket", root.transform, new Vector3(0f, -0.035f, -0.02f), new Vector3(-12f, 0f, 0f));
            var trigger = socketGo.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.07f;
            var socket = socketGo.AddComponent<MagazineSocket>();
            socket.interactionLayers = MagazineSocket.DefaultLayer | MagazineSocket.InsertedLayer;
            socket.showInteractableHoverMeshes = false;
            socket.recycleDelayTime = 0.25f;

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.attachTransform = attach.transform;
            grab.useDynamicAttach = false;
            grab.throwOnDetach = false;
            grab.colliders.Clear();
            grab.colliders.Add(slide.GetComponent<Collider>());
            grab.colliders.Add(grip.GetComponent<Collider>());

            var audio = Audio(root);
            var pistol = root.AddComponent<Pistol>();
            pistol.displayName = "Пистолет";
            pistol.muzzle = muzzle.transform;
            pistol.audioSource = audio;
            pistol.socket = socket;
            pistol.magazinePrefab = MagazinePrefab.GetComponent<Magazine>();
            pistol.muzzleFlashLight = light;
            socket.audioSource = audio;

            root.AddComponent<XRWeaponGrab>();
            root.AddComponent<ReturnToSpawnOnDrop>();
            SetLayerRecursive(root, SimVOLayers.Weapon);
            return root;
        }

        static GameObject BuildRingTarget()
        {
            var root = new GameObject("RingTarget");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.6f, 0.6f, 0.02f);
            collider.center = new Vector3(0f, 0f, 0.005f);
            Primitive(PrimitiveType.Quad, "Face", root.transform, Vector3.zero, new Vector3(0.6f, 0.6f, 1f), TargetPaper, default, false);
            Primitive(PrimitiveType.Cube, "Frame", root.transform, new Vector3(0f, 0f, 0.016f), new Vector3(0.64f, 0.64f, 0.02f), TargetFrame, default, false);
            Primitive(PrimitiveType.Cylinder, "Post", root.transform, new Vector3(0f, -0.9f, 0.02f), new Vector3(0.03f, 0.6f, 0.03f), Rack, default, false);
            var target = root.AddComponent<RingTarget>();
            target.radius = 0.25f;
            target.audioSource = Audio(root);
            SetLayerRecursive(root, SimVOLayers.Target);
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
            var button = root.AddComponent<PanelButton>();
            button.label = label;
            button.cap = cap.transform;
            button.audioSource = Audio(root);
            return root;
        }

        static GameObject BuildControlPanel()
        {
            var root = new GameObject("ControlPanel");
            Primitive(PrimitiveType.Cube, "Board", root.transform, Vector3.zero, new Vector3(0.64f, 0.025f, 0.34f), Panel);
            Text("Title", root.transform, new Vector3(0f, 0.014f, 0.125f), "ПУЛЬТ ТИРА", 0.3f, new Vector2(0.6f, 0.06f), new Vector3(90f, 0f, 0f));
            var panel = root.AddComponent<ControlPanel>();
            panel.modeButton = PlaceButton(root.transform, "ModeButton", -0.225f);
            panel.gameButton = PlaceButton(root.transform, "GameButton", -0.075f);
            panel.targetButton = PlaceButton(root.transform, "TargetButton", 0.075f);
            panel.startButton = PlaceButton(root.transform, "StartButton", 0.225f);
            return root;
        }

        static PanelButton PlaceButton(Transform parent, string name, float x)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(PanelButtonPrefab, parent);
            instance.name = name;
            instance.transform.localPosition = new Vector3(x, 0.0125f, -0.03f);
            return instance.GetComponent<PanelButton>();
        }

        static GameObject BuildScoreboard()
        {
            var root = new GameObject("Scoreboard");
            Primitive(PrimitiveType.Cube, "Board", root.transform, new Vector3(0f, 0f, 0.03f), new Vector3(1.8f, 1.0f, 0.04f), Panel);
            var board = root.AddComponent<Scoreboard>();
            board.title = Text("Title", root.transform, new Vector3(0f, 0.4f, 0f), "SimVO", 0.7f, new Vector2(1.7f, 0.15f));
            board.scoreText = Text("Score", root.transform, new Vector3(0f, 0.17f, 0f), "0", 2.2f, new Vector2(1.7f, 0.3f), default, new Color(1f, 0.85f, 0.2f));
            board.scoreText.fontStyle = FontStyles.Bold;
            board.statsText = Text("Stats", root.transform, new Vector3(0f, -0.22f, 0f), "", 0.62f, new Vector2(1.7f, 0.45f));
            board.audioSource = Audio(root, 0.5f);
            return root;
        }

        static GameObject BuildWeaponStand()
        {
            var root = new GameObject("WeaponStand");
            Primitive(PrimitiveType.Cube, "Base", root.transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.55f, 0.02f, 0.32f), Rack);
            Primitive(PrimitiveType.Cube, "Lip", root.transform, new Vector3(0f, 0.03f, -0.15f), new Vector3(0.55f, 0.04f, 0.02f), Rack);
            Text("Label", root.transform, new Vector3(0f, 0.052f, -0.15f), "ОРУЖИЕ", 0.18f, new Vector2(0.5f, 0.05f), new Vector3(90f, 0f, 0f));
            Empty("Slot0", root.transform, new Vector3(-0.15f, 0.06f, 0.02f), new Vector3(0f, 0f, 90f));
            Empty("Slot1", root.transform, new Vector3(0f, 0.06f, 0.02f), new Vector3(0f, 0f, 90f));
            Empty("Slot2", root.transform, new Vector3(0.15f, 0.06f, 0.02f), new Vector3(0f, 0f, 90f));

            var dispenserBox = Primitive(PrimitiveType.Cube, "Dispenser", root.transform, new Vector3(0.22f, 0.05f, 0.08f), new Vector3(0.09f, 0.06f, 0.07f), Dispenser);
            var socketGo = Empty("DispenserSocket", root.transform, new Vector3(0.22f, 0.12f, 0.08f));
            var trigger = socketGo.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.06f;
            var socket = socketGo.AddComponent<MagazineSocket>();
            socket.lockWhenInserted = false;
            socket.showInteractableHoverMeshes = false;
            var dispenser = dispenserBox.AddComponent<MagazineDispenser>();
            dispenser.socket = socket;
            dispenser.magazinePrefab = MagazinePrefab.GetComponent<Magazine>();
            Text("DispenserLabel", root.transform, new Vector3(0.22f, 0.052f, -0.0f), "МАГАЗИНЫ", 0.12f, new Vector2(0.12f, 0.04f), new Vector3(90f, 0f, 0f));

            root.AddComponent<WeaponStand>();
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
            var hand = Empty("HandAnchor", camGo.transform, new Vector3(0.2f, -0.17f, 0.42f));
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
