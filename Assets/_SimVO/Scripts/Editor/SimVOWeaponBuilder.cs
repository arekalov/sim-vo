using SimVO.Core;
using SimVO.Environment;
using SimVO.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using static SimVO.EditorTools.SimVOAssetBuilder;

namespace SimVO.EditorTools
{
    /// <summary>Builds weapon prefabs around the CC0 gun models: anchors are derived from the models' named parts.</summary>
    public static class SimVOWeaponBuilder
    {
        static readonly Vector3 ModelEuler = new Vector3(0f, 180f, 0f); // models point down -Z; prefabs point +Z

        static Bounds NodeBounds(GameObject model, string node)
        {
            var t = model.transform.Find(node);
            if (t == null) return WholeBounds(model);
            var renderer = t.GetComponent<Renderer>();
            return renderer != null ? renderer.bounds : WholeBounds(model);
        }

        static Bounds WholeBounds(GameObject model)
        {
            var bounds = new Bounds();
            var first = true;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        static void AssignMaterial(GameObject model, Material material)
        {
            if (material == null) return;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
        }

        static GameObject GunRoot(string name, string folder, string file, float mass, Material material, out GameObject model)
        {
            var root = new GameObject(name);
            model = InstantiateModel(GunModel(folder, file), root.transform, Vector3.zero, ModelEuler);
            model.name = "Model";
            AssignMaterial(model, material);
            var body = root.AddComponent<Rigidbody>();
            body.mass = mass;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            return root;
        }

        static BoxCollider Box(GameObject root, string name, Bounds bounds, Vector3 padding)
        {
            var go = Empty(name, root.transform, bounds.center);
            var box = go.AddComponent<BoxCollider>();
            box.size = bounds.size + padding;
            return box;
        }

        static Light MuzzleLight(Transform muzzle)
        {
            var light = Empty("MuzzleFlash", muzzle, Vector3.zero).AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 1.5f;
            light.intensity = 4f;
            light.color = new Color(1f, 0.8f, 0.5f);
            light.enabled = false;
            return light;
        }

        static XRGrabInteractable Grab(GameObject root, Transform attach, Transform secondary, params Collider[] colliders)
        {
            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.attachTransform = attach;
            grab.useDynamicAttach = false;
            grab.throwOnDetach = false;
            grab.colliders.Clear();
            foreach (var c in colliders) grab.colliders.Add(c);
            if (secondary != null)
            {
                grab.selectMode = InteractableSelectMode.Multiple;
                grab.secondaryAttachTransform = secondary;
                var transformer = root.AddComponent<XRGeneralGrabTransformer>();
                transformer.allowTwoHandedRotation = XRGeneralGrabTransformer.TwoHandedRotationMode.FirstHandDirectedTowardsSecondHand;
                transformer.allowTwoHandedScaling = false;
            }
            return grab;
        }

        static MagazineSocket Socket(GameObject root, Vector3 position, float radius)
        {
            var socketGo = Empty("MagazineSocket", root.transform, position);
            var trigger = socketGo.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            var socket = socketGo.AddComponent<MagazineSocket>();
            socket.interactionLayers = MagazineSocket.DefaultLayer | MagazineSocket.InsertedLayer;
            socket.showInteractableHoverMeshes = false;
            socket.recycleDelayTime = 0.25f;
            return socket;
        }

        static void Finish(GameObject root)
        {
            root.AddComponent<XRWeaponGrab>();
            root.AddComponent<ReturnToSpawnOnDrop>();
            SetLayerRecursive(root, SimVOLayers.Weapon);
        }

        public static GameObject BuildMagazine(string name, string folder, string file, int capacity, Material material)
        {
            var temp = InstantiateModel(GunModel(folder, file), null, Vector3.zero, ModelEuler);
            AssignMaterial(temp, material);
            var node = temp.transform.Find("Magazine");
            var renderer = node.GetComponent<Renderer>();
            var bounds = renderer.bounds;
            var top = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            var root = new GameObject(name);
            root.transform.position = top;
            var visual = new GameObject("Model");
            visual.transform.SetPositionAndRotation(node.position, node.rotation);
            visual.transform.localScale = node.lossyScale;
            visual.transform.SetParent(root.transform, true);
            visual.AddComponent<MeshFilter>().sharedMesh = node.GetComponent<MeshFilter>().sharedMesh;
            visual.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
            Object.DestroyImmediate(temp);

            var collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center - top;
            collider.size = bounds.size + new Vector3(0.012f, 0f, 0.012f);
            var body = root.AddComponent<Rigidbody>();
            body.mass = 0.15f;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.throwOnDetach = true;
            grab.useDynamicAttach = false;
            root.AddComponent<Magazine>().capacity = capacity;
            root.AddComponent<DestroyWhenDropped>();
            root.transform.position = Vector3.zero;
            return root;
        }

        public static GameObject BuildPistol(GameObject magazinePrefab, Material material)
        {
            var root = GunRoot("Pistol", "Makarov", "Makarov.fbx", 0.73f, material, out var model);
            var barrel = NodeBounds(model, "Barrel");
            var slide = NodeBounds(model, "Slide");
            var body = NodeBounds(model, "Body");
            var mag = NodeBounds(model, "Magazine");
            model.transform.Find("Magazine").gameObject.SetActive(false);

            // The model keeps its magazine as a detached display piece, so the grip is derived from the body: its lower rear part.
            var gripTop = new Vector3(body.center.x, body.min.y + body.size.y * 0.62f, body.min.z + 0.025f);
            var grip = new Bounds(new Vector3(gripTop.x, body.min.y + 0.035f, gripTop.z), new Vector3(0.03f, 0.07f, 0.036f));
            var muzzle = Empty("Muzzle", root.transform, new Vector3(barrel.center.x, barrel.center.y, barrel.max.z));
            var attach = Empty("Attach", root.transform, grip.center + new Vector3(0f, 0.01f, 0f));
            var upper = slide; upper.Encapsulate(body);
            var upperCollider = Box(root, "UpperCollider", upper, new Vector3(0.01f, 0f, 0f));
            var gripCollider = Box(root, "GripCollider", grip, Vector3.zero);
            var socket = Socket(root, gripTop, 0.06f);
            var light = MuzzleLight(muzzle.transform);
            Grab(root, attach.transform, null, upperCollider, gripCollider);

            var audio = SimVOAssetBuilder.Audio(root);
            var pistol = root.AddComponent<Pistol>();
            pistol.displayName = "Пистолет";
            pistol.muzzle = muzzle.transform;
            pistol.audioSource = audio;
            pistol.socket = socket;
            pistol.magazinePrefab = magazinePrefab.GetComponent<Magazine>();
            pistol.muzzleFlashLight = light;
            pistol.muzzleVelocity = 320f;
            pistol.minShotInterval = 0.15f;
            socket.audioSource = audio;
            Finish(root);
            return root;
        }

        public static GameObject BuildCarbine(GameObject magazinePrefab, Material material)
        {
            var root = GunRoot("Carbine", "Sniper", "Sniper.fbx", 3.2f, material, out var model);
            var barrel = NodeBounds(model, "Barrel");
            var trigger = NodeBounds(model, "Trigger");
            var mag = NodeBounds(model, "Magazine");
            var whole = WholeBounds(model);
            model.transform.Find("Magazine").gameObject.SetActive(false);

            var muzzle = Empty("Muzzle", root.transform, new Vector3(barrel.center.x, barrel.center.y + 0.01f, barrel.max.z));
            var attach = Empty("Attach", root.transform, trigger.center + new Vector3(0f, -0.03f, -0.06f));
            var secondary = Empty("SecondaryAttach", root.transform, new Vector3(0f, barrel.min.y - 0.02f, 0.16f));
            var bodyCollider = Box(root, "BodyCollider", new Bounds(new Vector3(whole.center.x, whole.center.y - 0.02f, whole.center.z), new Vector3(0.06f, 0.16f, whole.size.z)), Vector3.zero);
            var socket = Socket(root, new Vector3(mag.center.x, mag.max.y, mag.center.z), 0.07f);
            var light = MuzzleLight(muzzle.transform);
            Grab(root, attach.transform, secondary.transform, bodyCollider);

            var audio = SimVOAssetBuilder.Audio(root);
            var carbine = root.AddComponent<Carbine>();
            carbine.displayName = "Карабин";
            carbine.muzzle = muzzle.transform;
            carbine.audioSource = audio;
            carbine.socket = socket;
            carbine.magazinePrefab = magazinePrefab.GetComponent<Magazine>();
            carbine.muzzleFlashLight = light;
            carbine.muzzleVelocity = 800f;
            carbine.minShotInterval = 0.5f;
            carbine.recoilHaptic = 1f;
            carbine.swayStanding = 0.35f;
            carbine.swayKneeling = 0.18f;
            carbine.swayProne = 0.06f;
            carbine.twoHanded = true;
            socket.audioSource = audio;
            Finish(root);
            return root;
        }

        public static GameObject BuildShotgun(Material material)
        {
            var root = GunRoot("Shotgun", "Shotgun", "Shotgun.fbx", 3.4f, material, out var model);
            var whole = WholeBounds(model);
            var trigger = NodeBounds(model, "Trigger");
            var fore = NodeBounds(model, "Fore_Stock");

            var muzzle = Empty("Muzzle", root.transform, new Vector3(0f, whole.max.y - 0.03f, whole.max.z));
            var attach = Empty("Attach", root.transform, trigger.center + new Vector3(0f, -0.03f, -0.06f));
            var secondary = Empty("SecondaryAttach", root.transform, new Vector3(0f, fore.center.y - 0.01f, fore.center.z));
            var bodyCollider = Box(root, "BodyCollider", new Bounds(new Vector3(whole.center.x, whole.center.y, whole.center.z), new Vector3(0.06f, 0.18f, whole.size.z)), Vector3.zero);
            var portGo = Empty("LoadingPort", root.transform, trigger.center + new Vector3(0f, -0.035f, 0.12f));
            var portTrigger = portGo.AddComponent<SphereCollider>();
            portTrigger.isTrigger = true;
            portTrigger.radius = 0.05f;
            var port = portGo.AddComponent<ShellSocket>();
            port.showInteractableHoverMeshes = false;
            var light = MuzzleLight(muzzle.transform);
            Grab(root, attach.transform, secondary.transform, bodyCollider);

            var audio = SimVOAssetBuilder.Audio(root);
            var shotgun = root.AddComponent<Shotgun>();
            shotgun.displayName = "Дробовик";
            shotgun.muzzle = muzzle.transform;
            shotgun.audioSource = audio;
            shotgun.loadingPort = port;
            shotgun.muzzleFlashLight = light;
            shotgun.muzzleVelocity = 400f;
            shotgun.minShotInterval = 0.7f;
            shotgun.recoilHaptic = 1f;
            shotgun.recoilHapticDuration = 0.15f;
            shotgun.projectilesPerShot = 8;
            shotgun.pelletSpread = 2.5f;
            shotgun.twoHanded = true;
            port.shotgun = shotgun;
            Finish(root);
            return root;
        }

        public static GameObject BuildWeaponWall(GameObject pistol, GameObject shotgun, GameObject carbine)
        {
            var root = new GameObject("WeaponWall");
            Primitive(PrimitiveType.Cube, "Panel", root.transform, new Vector3(0f, 1.3f, 0f), new Vector3(2.7f, 2.2f, 0.06f), Plywood);
            Primitive(PrimitiveType.Cube, "FrameTop", root.transform, new Vector3(0f, 2.42f, 0f), new Vector3(2.76f, 0.06f, 0.09f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "FrameBottom", root.transform, new Vector3(0f, 0.18f, 0f), new Vector3(2.76f, 0.06f, 0.09f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "FrameLeft", root.transform, new Vector3(-1.38f, 1.3f, 0f), new Vector3(0.06f, 2.3f, 0.09f), Rack, default, false);
            Primitive(PrimitiveType.Cube, "FrameRight", root.transform, new Vector3(1.38f, 1.3f, 0f), new Vector3(0.06f, 2.3f, 0.09f), Rack, default, false);
            Text("Header", root.transform, new Vector3(0f, 2.28f, 0.035f), "ОРУЖИЕ", 0.5f, new Vector2(1.5f, 0.15f), new Vector3(0f, 180f, 0f), new Color(0.95f, 0.9f, 0.8f));

            var stand = root.AddComponent<WeaponStand>();
            stand.weapons.Add(Hang(root, pistol, "Slot0", new Vector3(-0.72f, 1.0f, 0.09f), 0.12f).GetComponent<Weapon>());
            stand.weapons.Add(Hang(root, carbine, "Slot1", new Vector3(0f, 1.3f, 0.09f), 0.32f).GetComponent<Weapon>());
            stand.weapons.Add(Hang(root, shotgun, "Slot2", new Vector3(0f, 1.6f, 0.09f), 0.32f).GetComponent<Weapon>());

            Decor(root, "M4A1", "M4A1.fbx", MatM4A1, new Vector3(-0.6f, 1.92f, 0.09f), new Vector3(0f, -90f, 0f), 0.26f);
            Decor(root, "AK47", "AK47.fbx", MatAK47, new Vector3(0.62f, 1.92f, 0.09f), new Vector3(0f, -90f, 0f), 0.26f);
            Decor(root, "Luger", "Luger.fbx", MatLuger, new Vector3(-0.1f, 1.0f, 0.09f), new Vector3(0f, 90f, 0f), 0.1f);
            Decor(root, "Grease_Gun", "Grease_Gun.fbx", MatGrease, new Vector3(0.62f, 1.0f, 0.09f), new Vector3(0f, -90f, 0f), 0.2f);

            Label(root, "ПИСТОЛЕТ", new Vector3(-0.72f, 0.82f, 0.035f));
            Label(root, "КАРАБИН", new Vector3(0.98f, 1.3f, 0.035f));
            Label(root, "ДРОБОВИК", new Vector3(0.98f, 1.6f, 0.035f));
            Label(root, "Люгер", new Vector3(-0.1f, 0.82f, 0.035f));
            Label(root, "M3", new Vector3(0.62f, 0.82f, 0.035f));
            return root;
        }

        static GameObject Hang(GameObject wall, GameObject prefab, string slotName, Vector3 position, float hookSpread)
        {
            var slot = Empty(slotName, wall.transform, position, new Vector3(0f, 90f, 0f));
            Hooks(wall, position, hookSpread);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, slot.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            return instance;
        }

        static void Decor(GameObject wall, string folder, string file, Material material, Vector3 position, Vector3 euler, float hookSpread)
        {
            Hooks(wall, position, hookSpread);
            var model = InstantiateModel(GunModel(folder, file), wall.transform, position, euler);
            model.name = "Decor_" + folder;
            AssignMaterial(model, material);
            foreach (var collider in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
        }

        static void Hooks(GameObject wall, Vector3 position, float spread)
        {
            foreach (var sign in new[] { -1f, 1f })
            {
                Primitive(PrimitiveType.Cylinder, "Hook", wall.transform, new Vector3(position.x + sign * spread, position.y - 0.02f, 0.06f), new Vector3(0.012f, 0.04f, 0.012f), Rack, new Vector3(90f, 0f, 0f), false);
            }
        }

        static void Label(GameObject wall, string text, Vector3 position)
        {
            Text("Label_" + text, wall.transform, position, text, 0.22f, new Vector2(0.6f, 0.08f), new Vector3(0f, 180f, 0f), new Color(0.95f, 0.9f, 0.8f));
        }
    }
}
