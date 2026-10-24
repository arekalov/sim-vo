using System.Collections.Generic;
using SimVO.Core;
using SimVO.Environment;
using SimVO.Platform;
using SimVO.Targets;
using SimVO.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;

namespace SimVO.EditorTools
{
    /// <summary>Builds the Main and Sandbox scenes from prefabs: room, lanes, lights, rigs, table, panel, scoreboard.</summary>
    public static class SimVOSceneBuilder
    {
        static readonly float[] LaneDistances = { 5f, 10f, 15f };
        const float TargetHeight = 1.3f;

        public static void BuildAll()
        {
            SimVOAssetBuilder.LoadExisting();
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
            RenderSettings.ambientLight = new Color(0.40f, 0.40f, 0.43f);
            RenderSettings.fog = false;

            var managers = new GameObject("Managers");
            var session = managers.AddComponent<GameSession>();
            var pool = managers.AddComponent<ProjectilePool>();
            pool.prefab = SimVOAssetBuilder.ProjectilePrefab.GetComponent<Projectile>();
            var fx = managers.AddComponent<FxLibrary>();
            fx.bulletHolePrefab = SimVOAssetBuilder.BulletHolePrefab;
            fx.popupPrefab = SimVOAssetBuilder.HitPopupPrefab.GetComponent<HitPopup>();

            var environment = new GameObject("Environment");
            var floorMarker = BuildRoom(environment.transform);
            BuildLights(environment.transform);

            var lanesRoot = new GameObject("Lanes");
            var spawner = lanesRoot.AddComponent<TargetSpawner>();
            spawner.ringTargetPrefab = SimVOAssetBuilder.RingTargetPrefab.GetComponent<RingTarget>();
            foreach (var distance in LaneDistances)
            {
                var lane = new GameObject($"Lane{distance:0}m");
                lane.transform.SetParent(lanesRoot.transform, false);
                lane.transform.position = new Vector3(0f, TargetHeight, distance);
                spawner.lanes.Add(lane.transform);
            }

            var table = BuildTable(environment.transform);
            var stand = Instance(SimVOAssetBuilder.WeaponStandPrefab, "WeaponStand", new Vector3(-0.4f, 1.005f, 0.66f));
            stand.transform.SetParent(table.transform, true);
            var slot = stand.transform.Find("Slot0");
            var pistol = Instance(SimVOAssetBuilder.PistolPrefab, "Pistol", slot.position, slot.rotation);
            pistol.transform.SetParent(slot, true);
            stand.GetComponent<WeaponStand>().weapons.Add(pistol.GetComponent<Weapon>());

            var panel = Instance(SimVOAssetBuilder.ControlPanelPrefab, "ControlPanel", new Vector3(0.38f, 1.02f, 0.66f), Quaternion.Euler(-20f, 0f, 0f));
            panel.transform.SetParent(table.transform, true);

            Instance(SimVOAssetBuilder.ScoreboardPrefab, "Scoreboard", new Vector3(0f, 2.55f, 4.2f));

            var xrRig = BuildXRRig(floorMarker);
            var desktopRig = Instance(SimVOAssetBuilder.DesktopRigPrefab, "DesktopRig", new Vector3(0f, 0f, -0.2f));
            desktopRig.GetComponent<DesktopPlayer>().stand = stand.GetComponent<WeaponStand>();
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
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            go.isStatic = true;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Renderer BuildRoom(Transform parent)
        {
            const float width = 8f, length = 22f, height = 3.6f, zCentre = 8f;
            var m = SimVOAssetBuilder.Floor;
            Box("Floor", parent, new Vector3(0f, -0.05f, zCentre), new Vector3(width, 0.1f, length), SimVOAssetBuilder.Floor);
            Box("Ceiling", parent, new Vector3(0f, height + 0.05f, zCentre), new Vector3(width, 0.1f, length), SimVOAssetBuilder.Ceiling);
            Box("WallBack", parent, new Vector3(0f, height / 2f, zCentre - length / 2f - 0.05f), new Vector3(width, height, 0.1f), SimVOAssetBuilder.Wall);
            Box("WallFar", parent, new Vector3(0f, height / 2f, zCentre + length / 2f + 0.05f), new Vector3(width, height, 0.1f), SimVOAssetBuilder.Wall);
            Box("WallLeft", parent, new Vector3(-width / 2f - 0.05f, height / 2f, zCentre), new Vector3(0.1f, height, length), SimVOAssetBuilder.Wall);
            Box("WallRight", parent, new Vector3(width / 2f + 0.05f, height / 2f, zCentre), new Vector3(0.1f, height, length), SimVOAssetBuilder.Wall);
            Box("Backstop", parent, new Vector3(0f, 1.4f, 17.5f), new Vector3(6f, 2.8f, 0.3f), SimVOAssetBuilder.Rack, new Vector3(-12f, 0f, 0f));

            Box("FiringLine", parent, new Vector3(0f, 0.006f, 1.0f), new Vector3(3.2f, 0.01f, 0.08f), SimVOAssetBuilder.Lane, default, false);
            Box("LaneEdgeLeft", parent, new Vector3(-1.5f, 0.006f, 9f), new Vector3(0.05f, 0.01f, 16f), SimVOAssetBuilder.Lane, default, false);
            Box("LaneEdgeRight", parent, new Vector3(1.5f, 0.006f, 9f), new Vector3(0.05f, 0.01f, 16f), SimVOAssetBuilder.Lane, default, false);
            foreach (var distance in LaneDistances)
            {
                Box($"Mark{distance:0}m", parent, new Vector3(0f, 0.006f, distance), new Vector3(3f, 0.01f, 0.06f), SimVOAssetBuilder.Lane, default, false);
                SimVOAssetBuilder.Text($"Label{distance:0}m", parent, new Vector3(-1.9f, 0.01f, distance), $"{distance:0} м", 1.2f, new Vector2(0.8f, 0.3f), new Vector3(90f, 0f, 0f), new Color(0.9f, 0.9f, 0.4f));
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "FloorMarker";
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            marker.transform.localScale = new Vector3(0.9f, 0.004f, 0.9f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            var renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = SimVOAssetBuilder.Marker;
            return renderer;
        }

        static GameObject BuildTable(Transform parent)
        {
            var table = new GameObject("ShootingTable");
            table.transform.SetParent(parent, false);
            Box("Top", table.transform, new Vector3(0f, 0.975f, 0.66f), new Vector3(1.5f, 0.05f, 0.5f), SimVOAssetBuilder.Table);
            Box("LegLeft", table.transform, new Vector3(-0.7f, 0.475f, 0.66f), new Vector3(0.06f, 0.95f, 0.45f), SimVOAssetBuilder.Rack);
            Box("LegRight", table.transform, new Vector3(0.7f, 0.475f, 0.66f), new Vector3(0.06f, 0.95f, 0.45f), SimVOAssetBuilder.Rack);
            return table;
        }

        static void BuildLights(Transform parent)
        {
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.transform.SetParent(parent, false);
            sun.type = LightType.Directional;
            sun.intensity = 0.7f;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
            sun.shadows = LightShadows.Soft;

            Spot(parent, "Light Shooter", new Vector3(0f, 3.4f, 0.5f), 3f);
            foreach (var distance in LaneDistances) Spot(parent, $"Light {distance:0}m", new Vector3(0f, 3.4f, distance - 0.6f), 5f);
        }

        static void Spot(Transform parent, string name, Vector3 position, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.position = position;
            light.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            light.type = LightType.Spot;
            light.spotAngle = 80f;
            light.range = 9f;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        static GameObject BuildXRRig(Renderer floorMarker)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimVOPaths.XRRigPrefab);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.name = "XR Origin (XR Rig)";
            rig.transform.position = Vector3.zero;
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
