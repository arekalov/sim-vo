using SimVO.Audio;
using SimVO.Core;
using SimVO.Environment;
using SimVO.Platform;
using SimVO.Targets;
using SimVO.UI;
using SimVO.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.XR.CoreUtils;
using static SimVO.EditorTools.SimVOAssetBuilder;

namespace SimVO.EditorTools
{
    /// <summary>Builds the gallery: five booths side by side, ceiling target carriers, the player's lane in the middle, a gun wall behind.</summary>
    public static class SimVOSceneBuilder
    {
        const int LaneCount = 5;
        const float LaneWidth = 1.8f;
        const int PlayerLane = 2;
        const float RoomWidth = LaneCount * LaneWidth + 0.4f;
        const float RoomMinZ = -4.6f;
        const float RoomMaxZ = 18.6f;
        const float RoomHeight = 3.4f;
        const float RailHeight = 2.95f;
        static readonly float[] Distances = { 5f, 10f, 15f };
        static readonly float[] DecorDistances = { 7f, 12f, 0f, 4f, 15f };

        static float LaneX(int lane) => (lane - PlayerLane) * LaneWidth;
        static float RoomZCentre => (RoomMinZ + RoomMaxZ) * 0.5f;
        static float RoomLength => RoomMaxZ - RoomMinZ;

        public static void BuildAll()
        {
            LoadExisting();
            Build(SimVOPaths.MainScene, "Main");
            Build(SimVOPaths.SandboxScene, "Sandbox");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SimVOPaths.MainScene, true),
                new EditorBuildSettingsScene(SimVOPaths.SandboxScene, true),
            };
            EditorSceneManager.OpenScene(SimVOPaths.MainScene);
            Debug.Log("SimVO: scenes built");
        }

        static void Build(string path, string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = name;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.37f, 0.41f);
            RenderSettings.fog = false;

            var managers = new GameObject("Managers");
            managers.AddComponent<GameSession>();
            managers.AddComponent<ProjectilePool>().prefab = ProjectilePrefab.GetComponent<Projectile>();
            var fx = managers.AddComponent<FxLibrary>();
            fx.bulletHolePrefab = BulletHolePrefab;
            fx.popupPrefab = HitPopupPrefab.GetComponent<HitPopup>();
            var sfx = managers.AddComponent<SfxLibrary>();
            sfx.pistolShots = Clips("pistol_9mm_near", "pistol_45_near");
            sfx.rifleShots = Clips("rifle_bolt_near", "carbine_556_near");
            sfx.shotgunShots = Clips("shotgun_12ga_near", "shotgun_12ga_mid");
            sfx.paperHits = Clips("impactSoft_heavy_000", "impactSoft_heavy_001", "impactSoft_heavy_002");
            sfx.metalHits = Clips("impactMetal_light_000", "impactMetal_light_001", "impactMetal_light_002");
            sfx.bullseye = Clips("impactBell_heavy_000", "impactBell_heavy_001");
            sfx.magIn = Clips("impactMetal_medium_000");
            sfx.magOut = Clips("impactMetal_light_000");
            sfx.shellIn = Clips("impactPlate_light_000");
            sfx.buttonClick = Clips("Button_22_click");
            sfx.targetFlip = Clips("impactPlank_medium_000");

            var environment = new GameObject("Environment");
            var floorMarker = BuildRoom(environment.transform);
            BuildLights(environment.transform);
            for (var lane = 0; lane < LaneCount; lane++) BuildBooth(environment.transform, lane);
            for (var j = 0; j <= LaneCount; j++) BuildPartition(environment.transform, LaneX(0) - LaneWidth * 0.5f + j * LaneWidth);
            BuildProps(environment.transform);

            var lanes = new GameObject("Lanes");
            TargetSpawner spawner = null;
            for (var lane = 0; lane < LaneCount; lane++)
            {
                var carrier = Instance(TargetCarrierPrefab, $"Carrier{lane + 1}", new Vector3(LaneX(lane), RailHeight, 0f));
                carrier.transform.SetParent(lanes.transform, true);
                var component = carrier.GetComponent<TargetCarrier>();
                if (lane == PlayerLane)
                {
                    spawner = lanes.AddComponent<TargetSpawner>();
                    spawner.carrier = component;
                    spawner.ringTargetPrefab = RingTargetPrefab.GetComponent<RingTarget>();
                    spawner.silhouetteTargetPrefab = SilhouetteTargetPrefab.GetComponent<SilhouetteTarget>();
                    spawner.distances = new System.Collections.Generic.List<float>(Distances);
                    foreach (var d in Distances) DistanceSign(lanes.transform, LaneX(lane), d);
                }
                else
                {
                    component.trolley.localPosition = new Vector3(0f, 0f, DecorDistances[lane]);
                    component.homeDistance = DecorDistances[lane];
                    var decor = (GameObject)PrefabUtility.InstantiatePrefab(lane % 2 == 0 ? RingTargetPrefab : SilhouetteTargetPrefab, component.targetMount);
                    decor.transform.localPosition = Vector3.zero;
                    decor.transform.localRotation = Quaternion.identity;
                    PrefabUtility.UnpackPrefabInstance(decor, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    Object.DestroyImmediate(decor.GetComponent<Target>());
                    Object.DestroyImmediate(decor.GetComponent<AudioSource>());
                }
            }

            var wall = Instance(WeaponWallPrefab, "WeaponWall", new Vector3(0f, 0f, -1.25f));
            var stand = wall.GetComponent<WeaponStand>();

            var benchY = 0.975f;
            var panel = Instance(ControlPanelPrefab, "ControlPanel", new Vector3(0.42f, benchY + 0.03f, 0.62f), Quaternion.Euler(-20f, 0f, 0f));
            Instance(DispenserPrefab, "MagazineDispenser", new Vector3(-0.3f, benchY, 0.6f));
            Instance(ShellBoxPrefab, "ShellBox", new Vector3(-0.52f, benchY, 0.6f));
            InstantiateModel(PolyHavenModel("ammo_box"), null, new Vector3(-0.1f, benchY, 0.72f), new Vector3(0f, 20f, 0f)).name = "AmmoBox";

            var scoreboard = Instance(ScoreboardPrefab, "Scoreboard", new Vector3(-0.98f, 2.42f, 1.0f));
            scoreboard.transform.localScale = Vector3.one * 0.55f;

            var xrRig = BuildXRRig(floorMarker);
            var desktopRig = Instance(DesktopRigPrefab, "DesktopRig", new Vector3(0f, 0f, -0.45f));
            desktopRig.GetComponent<DesktopPlayer>().stand = stand;
            xrRig.SetActive(false);
            desktopRig.SetActive(false);
            var bootstrap = managers.AddComponent<PlatformBootstrap>();
            bootstrap.xrRig = xrRig;
            bootstrap.desktopRig = desktopRig;

            EditorSceneManager.SaveScene(scene, path);
        }

        static GameObject Instance(GameObject prefab, string name, Vector3 position, Quaternion rotation = default)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
            return instance;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler = default, bool collider = true)
        {
            var go = Primitive(PrimitiveType.Cube, name, parent, position, scale, material, euler, collider);
            go.isStatic = true;
            return go;
        }

        static Renderer BuildRoom(Transform parent)
        {
            var room = new GameObject("Room").transform;
            room.SetParent(parent, false);
            Box("Floor", room, new Vector3(0f, -0.05f, RoomZCentre), new Vector3(RoomWidth, 0.1f, RoomLength), ConcreteFloor);
            Box("Ceiling", room, new Vector3(0f, RoomHeight + 0.05f, RoomZCentre), new Vector3(RoomWidth, 0.1f, RoomLength), Ceiling);
            Box("WallBack", room, new Vector3(0f, RoomHeight / 2f, RoomMinZ - 0.05f), new Vector3(RoomWidth, RoomHeight, 0.1f), ConcreteWallShort);
            Box("WallFar", room, new Vector3(0f, RoomHeight / 2f, RoomMaxZ + 0.05f), new Vector3(RoomWidth, RoomHeight, 0.1f), ConcreteWallShort);
            Box("WallLeft", room, new Vector3(-RoomWidth / 2f - 0.05f, RoomHeight / 2f, RoomZCentre), new Vector3(0.1f, RoomHeight, RoomLength), ConcreteWallLong);
            Box("WallRight", room, new Vector3(RoomWidth / 2f + 0.05f, RoomHeight / 2f, RoomZCentre), new Vector3(0.1f, RoomHeight, RoomLength), ConcreteWallLong);
            Box("CeilingBeam", room, new Vector3(0f, RoomHeight - 0.1f, 1.2f), new Vector3(RoomWidth, 0.2f, 0.25f), Rack, default, false);

            Box("Backstop", room, new Vector3(0f, 1.45f, 17.6f), new Vector3(RoomWidth - 0.4f, 2.9f, 0.16f), MetalPlate, new Vector3(-14f, 0f, 0f));
            Box("BulletTrap", room, new Vector3(0f, 0.3f, 17.1f), new Vector3(RoomWidth - 0.4f, 0.6f, 0.8f), Rack);
            Box("BackstopHeader", room, new Vector3(0f, 3.0f, 17.3f), new Vector3(RoomWidth - 0.4f, 0.7f, 0.5f), Rack);

            for (var lane = 0; lane < LaneCount; lane++)
            {
                Box($"FiringLine{lane + 1}", room, new Vector3(LaneX(lane), 0.006f, 0.05f), new Vector3(LaneWidth - 0.2f, 0.012f, 0.08f), Lane, default, false);
            }
            Box("LaneEdgeLeft", room, new Vector3(LaneX(0) - LaneWidth * 0.5f, 0.006f, 9f), new Vector3(0.05f, 0.012f, 16f), Lane, default, false);
            Box("LaneEdgeRight", room, new Vector3(LaneX(LaneCount - 1) + LaneWidth * 0.5f, 0.006f, 9f), new Vector3(0.05f, 0.012f, 16f), Lane, default, false);
            foreach (var d in Distances)
            {
                Box($"Mark{d:0}m", room, new Vector3(0f, 0.006f, d), new Vector3(RoomWidth - 0.6f, 0.012f, 0.05f), Lane, default, false);
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "FloorMarker";
            marker.transform.SetParent(room, false);
            marker.transform.localPosition = new Vector3(0f, 0.004f, -0.45f);
            marker.transform.localScale = new Vector3(0.8f, 0.004f, 0.8f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            var renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = Marker;
            return renderer;
        }

        static void BuildBooth(Transform parent, int lane)
        {
            var x = LaneX(lane);
            var booth = new GameObject($"Booth{lane + 1}").transform;
            booth.SetParent(parent, false);
            Box("BenchTop", booth, new Vector3(x, 0.95f, 0.6f), new Vector3(LaneWidth - 0.3f, 0.05f, 0.6f), WoodTable);
            Box("BenchApron", booth, new Vector3(x, 0.89f, 0.31f), new Vector3(LaneWidth - 0.3f, 0.1f, 0.03f), Rack, default, false);
            Box("LegLeft", booth, new Vector3(x - (LaneWidth - 0.3f) / 2f + 0.04f, 0.46f, 0.6f), new Vector3(0.05f, 0.92f, 0.55f), Rack);
            Box("LegRight", booth, new Vector3(x + (LaneWidth - 0.3f) / 2f - 0.04f, 0.46f, 0.6f), new Vector3(0.05f, 0.92f, 0.55f), Rack);
            Box("Shelf", booth, new Vector3(x, 0.45f, 0.65f), new Vector3(LaneWidth - 0.4f, 0.03f, 0.45f), Plywood, default, false);

            var sign = Box("Sign", booth, new Vector3(x, 2.45f, -0.5f), new Vector3(0.55f, 0.3f, 0.03f), Sign, default, false);
            Text("SignText", booth, new Vector3(x, 2.45f, -0.517f), $"<b>{lane + 1}</b>\n<size=50%>ДОРОЖКА</size>", 0.9f, new Vector2(0.5f, 0.3f), default, new Color(0.1f, 0.1f, 0.12f));
            Text("SignTextBack", booth, new Vector3(x, 2.45f, -0.483f), $"<b>{lane + 1}</b>", 1.2f, new Vector2(0.5f, 0.3f), new Vector3(0f, 180f, 0f), new Color(0.1f, 0.1f, 0.12f));

            var light = Instance(CagedLightPrefab, "BoothLight", new Vector3(x + 0.55f, RoomHeight - 0.05f, 0.3f));
            light.transform.SetParent(booth, true);
        }

        static void BuildPartition(Transform parent, float x)
        {
            var partition = new GameObject("Partition").transform;
            partition.SetParent(parent, false);
            Box("Panel", partition, new Vector3(x, 1.05f, 0.2f), new Vector3(0.08f, 2.1f, 1.5f), Plywood);
            Box("TopRail", partition, new Vector3(x, 2.12f, 0.2f), new Vector3(0.12f, 0.05f, 1.55f), Rack, default, false);
            Box("Post", partition, new Vector3(x, 1.05f, -0.52f), new Vector3(0.1f, 2.1f, 0.1f), Rack, default, false);
        }

        static void BuildProps(Transform parent)
        {
            var props = new GameObject("Props").transform;
            props.SetParent(parent, false);
            InstantiateModel(PolyHavenModel("korean_fire_extinguisher_01"), props, new Vector3(-RoomWidth / 2f + 0.25f, 0f, -2.6f), new Vector3(0f, 90f, 0f)).name = "FireExtinguisher";
            InstantiateModel(PolyHavenModel("metal_toolbox"), props, new Vector3(LaneX(1) + 0.3f, 0.975f, 0.65f), new Vector3(0f, -15f, 0f)).name = "Toolbox";
            InstantiateModel(PolyHavenModel("ammo_box"), props, new Vector3(LaneX(3) - 0.3f, 0.975f, 0.7f), new Vector3(0f, 70f, 0f)).name = "AmmoBoxDecor";
            foreach (var x in new[] { -3.2f, 3.2f })
            {
                var lamp = InstantiateModel(PolyHavenModel("industrial_wall_lamp"), props, new Vector3(x, 2.3f, RoomMinZ + 0.12f), new Vector3(0f, 0f, 0f));
                lamp.name = "WallLamp";
                var light = Empty("Light", lamp.transform, new Vector3(0f, -0.1f, 0.2f)).AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 5f;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.9f, 0.75f);
                light.shadows = LightShadows.None;
            }
        }

        static void DistanceSign(Transform parent, float x, float distance)
        {
            var sign = new GameObject($"DistanceSign{distance:0}").transform;
            sign.SetParent(parent, false);
            Box("Plate", sign, new Vector3(x + 0.6f, 2.6f, distance), new Vector3(0.35f, 0.18f, 0.02f), Sign, default, false);
            Box("Hanger", sign, new Vector3(x + 0.6f, 2.78f, distance), new Vector3(0.02f, 0.18f, 0.02f), Rack, default, false);
            Text("Label", sign, new Vector3(x + 0.6f, 2.6f, distance - 0.012f), $"{distance:0} м", 0.7f, new Vector2(0.34f, 0.18f), default, new Color(0.1f, 0.1f, 0.12f));
        }

        static void BuildLights(Transform parent)
        {
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.transform.SetParent(parent, false);
            sun.type = LightType.Directional;
            sun.intensity = 0.45f;
            sun.color = new Color(0.9f, 0.93f, 1f);
            sun.transform.rotation = Quaternion.Euler(62f, -18f, 0f);
            sun.shadows = LightShadows.Soft;

            var lights = new GameObject("LaneLights").transform;
            lights.SetParent(parent, false);
            for (var lane = 0; lane < LaneCount; lane++)
            {
                var zs = lane == PlayerLane ? new[] { 4.4f, 9.4f, 14.4f } : new[] { 8f };
                foreach (var z in zs)
                {
                    var lamp = Instance(CagedLightPrefab, $"LaneLight{lane + 1}_{z:0}", new Vector3(LaneX(lane) + 0.6f, RoomHeight - 0.05f, z));
                    lamp.transform.SetParent(lights, true);
                    if (lane != PlayerLane) lamp.GetComponentInChildren<Light>().intensity = 2.2f;
                }
            }
        }

        static GameObject BuildXRRig(Renderer floorMarker)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimVOPaths.XRRigPrefab);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.name = "XR Origin (XR Rig)";
            rig.transform.position = new Vector3(0f, 0f, -0.45f);
            rig.layer = SimVOLayers.IgnoreRaycast;
            var locomotion = rig.transform.Find("Locomotion");
            if (locomotion != null) locomotion.gameObject.SetActive(false);
            var origin = rig.GetComponent<XROrigin>();
            var tracker = rig.AddComponent<BodyPositionTracker>();
            tracker.head = origin != null && origin.Camera != null ? origin.Camera.transform : rig.transform;
            tracker.floorMarker = floorMarker;
            return rig;
        }
    }
}
