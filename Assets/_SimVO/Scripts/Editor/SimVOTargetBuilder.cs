using SimVO.Core;
using SimVO.Targets;
using UnityEngine;
using static SimVO.EditorTools.SimVOAssetBuilder;

namespace SimVO.EditorTools
{
    /// <summary>Gong, clay pigeon, trap, hologram target and the cover barricade.</summary>
    public static class SimVOTargetBuilder
    {
        public static GameObject BuildGong()
        {
            var root = new GameObject("GongTarget");
            var pivot = Empty("SwingPivot", root.transform, new Vector3(0f, 0.34f, 0f));
            var plate = Primitive(PrimitiveType.Cylinder, "Plate", pivot.transform, new Vector3(0f, -0.34f, 0f), new Vector3(0.4f, 0.012f, 0.4f), MetalPlate, new Vector3(90f, 0f, 0f), false);
            var collider = plate.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 1f, 1f);
            Primitive(PrimitiveType.Cylinder, "Centre", pivot.transform, new Vector3(0f, -0.34f, -0.008f), new Vector3(0.14f, 0.004f, 0.14f), GunDark, new Vector3(90f, 0f, 0f), false);
            Primitive(PrimitiveType.Cylinder, "ChainL", pivot.transform, new Vector3(-0.14f, -0.17f, 0f), new Vector3(0.012f, 0.17f, 0.012f), Rack, default, false);
            Primitive(PrimitiveType.Cylinder, "ChainR", pivot.transform, new Vector3(0.14f, -0.17f, 0f), new Vector3(0.012f, 0.17f, 0.012f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "Bar", root.transform, new Vector3(0f, 0.34f, 0f), new Vector3(0.36f, 0.02f, 0.02f), Rack, default, false);
            var gong = root.AddComponent<GongTarget>();
            gong.radius = 0.2f;
            gong.swingPivot = pivot.transform;
            gong.surface = TargetSurface.Metal;
            gong.showHoles = false;
            gong.audioSource = SimVOAssetBuilder.Audio(root);
            SetLayerRecursive(root, SimVOLayers.Target);
            return root;
        }

        public static GameObject BuildClayFragment()
        {
            var fragment = Primitive(PrimitiveType.Cube, "ClayFragment", null, Vector3.zero, new Vector3(0.035f, 0.008f, 0.03f), Clay);
            var body = fragment.AddComponent<Rigidbody>();
            body.mass = 0.02f;
            fragment.layer = SimVOLayers.IgnoreRaycast;
            return fragment;
        }

        public static GameObject BuildClay(GameObject fragmentPrefab)
        {
            var root = new GameObject("ClayTarget");
            var disc = Primitive(PrimitiveType.Cylinder, "Disc", root.transform, Vector3.zero, new Vector3(0.11f, 0.012f, 0.11f), Clay, default, false);
            Primitive(PrimitiveType.Cylinder, "Dome", root.transform, new Vector3(0f, 0.012f, 0f), new Vector3(0.07f, 0.008f, 0.07f), ClayDark, default, false);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.26f, 0.14f, 0.26f);
            var clay = root.AddComponent<ClayTarget>();
            clay.disc = disc.GetComponent<Renderer>();
            clay.fragmentPrefab = fragmentPrefab;
            clay.showHoles = false;
            clay.surface = TargetSurface.Metal;
            clay.audioSource = SimVOAssetBuilder.Audio(root);
            SetLayerRecursive(root, SimVOLayers.Target);
            return root;
        }

        public static GameObject BuildThrower(GameObject clayPrefab)
        {
            var root = new GameObject("ClayThrower");
            Primitive(PrimitiveType.Cube, "Base", root.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.55f, 0.4f, 0.55f), Rack);
            Primitive(PrimitiveType.Cube, "Arm", root.transform, new Vector3(0.1f, 0.5f, 0f), new Vector3(0.45f, 0.03f, 0.08f), Dispenser, new Vector3(0f, 0f, -20f), false);
            Primitive(PrimitiveType.Cylinder, "Axle", root.transform, new Vector3(0f, 0.46f, 0f), new Vector3(0.06f, 0.05f, 0.06f), GunDark, default, false);
            Text("Label", root.transform, new Vector3(0f, 0.25f, -0.28f), "МЕТАТЕЛЬ", 0.22f, new Vector2(0.5f, 0.08f), default, new Color(0.95f, 0.9f, 0.8f));
            var muzzle = Empty("Muzzle", root.transform, new Vector3(0.3f, 0.62f, 0f));
            var thrower = root.AddComponent<ClayThrower>();
            thrower.clayPrefab = clayPrefab.GetComponent<ClayTarget>();
            thrower.muzzle = muzzle.transform;
            thrower.audioSource = SimVOAssetBuilder.Audio(root);
            return root;
        }

        public static GameObject BuildHologram()
        {
            var root = new GameObject("HologramTarget");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.6f, 0.6f, 0.02f);
            var rings = Primitive(PrimitiveType.Quad, "Rings", root.transform, Vector3.zero, new Vector3(0.6f, 0.6f, 1f), Hologram, default, false);
            var scan = Primitive(PrimitiveType.Quad, "Scanlines", root.transform, new Vector3(0f, 0f, -0.004f), new Vector3(0.6f, 0.6f, 1f), HologramScan, default, false);
            Primitive(PrimitiveType.Cube, "Emitter", root.transform, new Vector3(0f, 0.36f, 0.02f), new Vector3(0.34f, 0.04f, 0.06f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "EmitterLens", root.transform, new Vector3(0f, 0.335f, 0.02f), new Vector3(0.3f, 0.01f, 0.03f), LampGreen, default, false);
            var glow = Empty("Glow", root.transform, new Vector3(0f, 0f, -0.15f)).AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.35f, 0.9f, 1f);
            glow.range = 1.6f;
            glow.intensity = 1.2f;
            glow.shadows = LightShadows.None;
            var target = root.AddComponent<HologramTarget>();
            target.radius = 0.25f;
            target.surface = TargetSurface.Hologram;
            target.showHoles = false;
            target.ringsRenderer = rings.GetComponent<Renderer>();
            target.scanlines = scan.GetComponent<Renderer>();
            target.glow = glow;
            target.audioSource = SimVOAssetBuilder.Audio(root);
            SetLayerRecursive(root, SimVOLayers.Target);
            return root;
        }

        public static GameObject BuildDuck()
        {
            var root = new GameObject("DuckTarget");
            var pivot = Empty("FlipPivot", root.transform, Vector3.zero);
            var body = Primitive(PrimitiveType.Sphere, "Body", pivot.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.2f, 0.13f, 0.08f), Duck, default, false);
            Primitive(PrimitiveType.Sphere, "Head", pivot.transform, new Vector3(0.07f, 0.22f, 0f), new Vector3(0.09f, 0.09f, 0.07f), Duck, default, false);
            Primitive(PrimitiveType.Cube, "Beak", pivot.transform, new Vector3(0.125f, 0.21f, 0f), new Vector3(0.05f, 0.025f, 0.03f), DuckBeak, default, false);
            Primitive(PrimitiveType.Sphere, "Eye", pivot.transform, new Vector3(0.095f, 0.235f, -0.03f), new Vector3(0.015f, 0.015f, 0.015f), GunDark, default, false);
            Primitive(PrimitiveType.Cube, "Post", pivot.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.02f, 0.07f, 0.02f), Rack, default, false);
            var collider = pivot.AddComponent<BoxCollider>();
            collider.center = new Vector3(0.03f, 0.15f, 0f);
            collider.size = new Vector3(0.26f, 0.22f, 0.06f);
            var duck = root.AddComponent<DuckTarget>();
            duck.flipPivot = pivot.transform;
            duck.surface = TargetSurface.Metal;
            duck.showHoles = false;
            duck.audioSource = SimVOAssetBuilder.Audio(root);
            SetLayerRecursive(root, SimVOLayers.Target);
            return root;
        }

        public static GameObject BuildDuckGallery(GameObject duckPrefab)
        {
            var root = new GameObject("DuckGallery");
            Primitive(PrimitiveType.Cube, "Belt", root.transform, new Vector3(0f, -0.03f, 0f), new Vector3(1.8f, 0.05f, 0.14f), Rack);
            Primitive(PrimitiveType.Cube, "BeltBack", root.transform, new Vector3(0f, 0.1f, 0.09f), new Vector3(1.8f, 0.3f, 0.02f), Dispenser, default, false);
            Primitive(PrimitiveType.Cube, "PostL", root.transform, new Vector3(-0.85f, -0.5f, 0f), new Vector3(0.06f, 1.0f, 0.06f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "PostR", root.transform, new Vector3(0.85f, -0.5f, 0f), new Vector3(0.06f, 1.0f, 0.06f), Rack, default, false);
            Text("Label", root.transform, new Vector3(0f, 0.32f, 0.09f), "ТИР · УТКИ", 0.3f, new Vector2(1f, 0.1f), default, new Color(1f, 0.85f, 0.3f));
            var gallery = root.AddComponent<DuckGallery>();
            gallery.beltLength = 1.8f;
            for (var i = 0; i < 5; i++)
            {
                var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(duckPrefab, root.transform);
                instance.name = $"Duck{i + 1}";
                instance.transform.localPosition = new Vector3(-0.72f + i * 0.36f, 0f, 0f);
                gallery.ducks.Add(instance.GetComponent<DuckTarget>());
            }
            return root;
        }

        public static GameObject BuildBarricade()
        {
            var root = new GameObject("CoverBarricade");
            var rising = Empty("Rising", root.transform, Vector3.zero);
            var left = Primitive(PrimitiveType.Cube, "Left", rising.transform, new Vector3(-0.225f, 0.975f, 0f), new Vector3(0.85f, 1.95f, 0.12f), MetalPlate);
            var rightBottom = Primitive(PrimitiveType.Cube, "RightBottom", rising.transform, new Vector3(0.425f, 0.65f, 0f), new Vector3(0.45f, 1.3f, 0.12f), MetalPlate);
            var rightTop = Primitive(PrimitiveType.Cube, "RightTop", rising.transform, new Vector3(0.425f, 1.8f, 0f), new Vector3(0.45f, 0.3f, 0.12f), MetalPlate);
            Primitive(PrimitiveType.Cube, "FrameTop", rising.transform, new Vector3(0f, 1.97f, 0f), new Vector3(1.34f, 0.05f, 0.16f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "FrameLeft", rising.transform, new Vector3(-0.67f, 0.975f, 0f), new Vector3(0.05f, 1.95f, 0.16f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "FrameRight", rising.transform, new Vector3(0.67f, 0.975f, 0f), new Vector3(0.05f, 1.95f, 0.16f), Rack, default, false);
            var sill = Primitive(PrimitiveType.Cube, "WindowSill", rising.transform, new Vector3(0.425f, 1.3f, 0f), new Vector3(0.45f, 0.03f, 0.18f), Rack, default, false);
            Text("Label", rising.transform, new Vector3(-0.225f, 1.5f, -0.065f), "УКРЫТИЕ", 0.5f, new Vector2(0.8f, 0.15f), default, new Color(0.95f, 0.85f, 0.3f));
            var barricade = root.AddComponent<CoverBarricade>();
            barricade.rising = rising.transform;
            barricade.blockers = new Collider[] { left.GetComponent<Collider>(), rightBottom.GetComponent<Collider>(), rightTop.GetComponent<Collider>() };
            barricade.rightBottom = rightBottom.transform;
            barricade.rightTop = rightTop.transform;
            barricade.sill = sill.transform;
            barricade.audioSource = SimVOAssetBuilder.Audio(root);
            return root;
        }
    }
}
