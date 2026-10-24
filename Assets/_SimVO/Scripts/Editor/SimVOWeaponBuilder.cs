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

        static GameObject GunRoot(string name, string folder, string file, float mass, Material material, out GameObject model, Vector3? modelEuler = null)
        {
            var root = new GameObject(name);
            model = InstantiateModel(GunModel(folder, file), root.transform, Vector3.zero, modelEuler ?? ModelEuler);
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

        /// <summary>Grabbable slide/bolt/pump on a named model part.</summary>
        static void AddHandle(GameObject root, GameObject model, string nodeName, Weapon weapon, float travel, float radius = 0.045f)
        {
            var node = model.transform.Find(nodeName);
            if (node == null) return;
            var bounds = NodeBounds(model, nodeName);
            var handleGo = Empty("CockingHandle_" + nodeName, root.transform, bounds.center);
            var collider = handleGo.AddComponent<SphereCollider>();
            collider.radius = radius;
            handleGo.AddComponent<XRSimpleInteractable>();
            var handle = handleGo.AddComponent<CockingHandle>();
            handle.weapon = weapon;
            handle.moving = node;
            handle.travel = travel;
            handle.audioSource = weapon.audioSource;
            handleGo.layer = SimVOLayers.Weapon;
        }

        static void Finish(GameObject root)
        {
            var weapon = root.GetComponent<Weapon>();
            var laserGo = Empty("LaserSight", root.transform, Vector3.zero);
            var line = laserGo.AddComponent<LineRenderer>();
            line.startWidth = 0.0025f;
            line.endWidth = 0.0025f;
            line.sharedMaterial = LaserMat;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.positionCount = 2;
            var dot = Primitive(PrimitiveType.Quad, "Dot", laserGo.transform, Vector3.zero, new Vector3(0.02f, 0.02f, 1f), LaserMat, default, false);
            dot.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var laser = laserGo.AddComponent<LaserSight>();
            laser.weapon = weapon;
            laser.dot = dot.transform;
            root.AddComponent<XRWeaponGrab>();
            root.AddComponent<ReturnToSpawnOnDrop>();
            SetLayerRecursive(root, SimVOLayers.Weapon);
        }

        public static GameObject BuildMagazine(string name, string folder, string file, int capacity, Material material, string nodeName = "Magazine", Vector3? modelEuler = null)
        {
            var temp = InstantiateModel(GunModel(folder, file), null, Vector3.zero, modelEuler ?? ModelEuler);
            AssignMaterial(temp, material);
            var node = temp.transform.Find(nodeName);
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
            grab.throwOnDetach = false;
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
            pistol.recoilPivot = model.transform;
            pistol.recoilKickDistance = 0.025f;
            pistol.recoilKickAngle = 8f;
            pistol.recoilHaptic = 1f;
            pistol.recoilHapticDuration = 0.1f;
            socket.audioSource = audio;
            AddHandle(root, model, "Slide", pistol, 0.035f);
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
            carbine.recoilPivot = model.transform;
            carbine.recoilKickDistance = 0.05f;
            carbine.recoilKickAngle = 4f;
            carbine.recoilRecoverSeconds = 0.2f;
            carbine.recoilHapticDuration = 0.18f;
            carbine.recoilHaptic = 1f;
            AddScope(root, model, carbine);
            carbine.requiresCockingAfterShot = true;
            AddHandle(root, model, "Bolt", carbine, 0.06f);
            carbine.swayStanding = 0.35f;
            carbine.swayKneeling = 0.18f;
            carbine.swayProne = 0.06f;
            carbine.twoHanded = true;
            socket.audioSource = audio;
            Finish(root);
            return root;
        }

        /// <summary>Camera at the objective renders into a lens quad at the eyepiece; a reticle quad sits in front of it.</summary>
        static void AddScope(GameObject root, GameObject model, Weapon weapon)
        {
            var scope = NodeBounds(model, "Scope");
            var radius = Mathf.Max(scope.size.y, scope.size.x) * 0.5f;
            if (radius < 0.012f) radius = 0.02f;
            var ocular = new Vector3(scope.center.x, scope.center.y, scope.min.z - 0.004f);
            var objective = new Vector3(scope.center.x, scope.center.y, scope.max.z - 0.01f);
            var scopeRoot = Empty("Scope", root.transform, ocular);
            var lens = Primitive(PrimitiveType.Quad, "Lens", scopeRoot.transform, Vector3.zero, new Vector3(radius * 1.8f, radius * 1.8f, 1f), ScopeLens, default, false);
            var reticle = Primitive(PrimitiveType.Quad, "Reticle", scopeRoot.transform, new Vector3(0f, 0f, -0.001f), new Vector3(radius * 1.8f, radius * 1.8f, 1f), Reticle, default, false);
            var camGo = Empty("ScopeCamera", root.transform, objective);
            var cam = camGo.AddComponent<Camera>();
            cam.targetTexture = ScopeRenderTexture();
            cam.fieldOfView = 8f;
            cam.nearClipPlane = 0.4f;
            cam.farClipPlane = 80f;
            cam.cullingMask = ~(1 << SimVOLayers.Weapon);
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            cam.enabled = false;
            var data = UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(cam);
            data.renderPostProcessing = false;
            data.renderShadows = false;
            var view = scopeRoot.AddComponent<ScopeView>();
            view.weapon = weapon;
            view.scopeCamera = cam;
            view.lens = lens.GetComponent<Renderer>();
            view.reticle = reticle.GetComponent<Renderer>();
            lens.GetComponent<Renderer>().enabled = false;
            reticle.GetComponent<Renderer>().enabled = false;
        }

        public struct GunSpec
        {
            public string name, displayName, folder, file, barrelNode, triggerNode, magazineNode, bodyNode;
            public Material material;
            public GameObject magazinePrefab;
            public Vector3? modelEuler;
            public bool pistolGrip, twoHanded, automatic;
            public float mass, velocity, interval, swayStanding, swayKneeling, swayProne, kickDistance, kickAngle, hapticDuration, muzzleYOffset;
            public Vector3 secondaryLocal;
            public string[] clips;
            public string handleNode;
            public float handleTravel;
        }

        public static GunSpec M4Spec(GameObject magazine, Material material) => new GunSpec
        {
            name = "M4A1", displayName = "M4A1", folder = "M4A1", file = "M4A1.fbx", barrelNode = "Barrel", triggerNode = "Trigger", magazineNode = "Magazine",
            material = material, magazinePrefab = magazine, twoHanded = true, mass = 3f, velocity = 900f, interval = 0.12f,
            swayStanding = 0.3f, swayKneeling = 0.16f, swayProne = 0.06f, kickDistance = 0.035f, kickAngle = 3f, hapticDuration = 0.12f,
            secondaryLocal = new Vector3(0f, -0.03f, 0.22f), clips = new[] { "carbine_556_near" }, handleNode = "Charging_Handle", handleTravel = 0.05f,
        };

        public static GunSpec AkSpec(GameObject magazine, Material material) => new GunSpec
        {
            name = "AK47", displayName = "АК-47", folder = "AK47", file = "AK47.fbx", barrelNode = null, triggerNode = "Trigger", magazineNode = "Magazine", bodyNode = "AK47",
            material = material, magazinePrefab = magazine, twoHanded = true, mass = 3.5f, velocity = 715f, interval = 0.13f, muzzleYOffset = 0.06f,
            swayStanding = 0.35f, swayKneeling = 0.18f, swayProne = 0.07f, kickDistance = 0.045f, kickAngle = 4f, hapticDuration = 0.14f,
            secondaryLocal = new Vector3(0f, -0.02f, 0.2f), clips = new[] { "ak47_762_near", "ak47_762_mid" }, handleNode = "Ejection_Port", handleTravel = 0.05f,
        };

        public static GunSpec LugerSpec(GameObject magazine, Material material) => new GunSpec
        {
            name = "Luger", displayName = "Люгер", folder = "Luger", file = "Luger.fbx", barrelNode = "Barrel", triggerNode = "Trigger", bodyNode = "Body",
            material = material, magazinePrefab = magazine, pistolGrip = true, modelEuler = Vector3.zero, mass = 0.9f, velocity = 350f, interval = 0.15f,
            swayStanding = 0.45f, swayKneeling = 0.25f, swayProne = 0.12f, kickDistance = 0.025f, kickAngle = 8f, hapticDuration = 0.1f,
            clips = new[] { "pistol_9mm_near", "pistol_9mm_mid" }, handleNode = "Slide", handleTravel = 0.03f,
        };

        public static GunSpec SmgSpec(GameObject magazine, Material material) => new GunSpec
        {
            name = "M3", displayName = "M3 «Grease Gun»", folder = "Grease_Gun", file = "Grease_Gun.fbx", barrelNode = null, triggerNode = "Trigger", magazineNode = "Magazine", bodyNode = "Grease_Gun",
            material = material, magazinePrefab = magazine, twoHanded = true, automatic = true, mass = 3.7f, velocity = 280f, interval = 0.13f, muzzleYOffset = 0.07f,
            swayStanding = 0.7f, swayKneeling = 0.4f, swayProne = 0.2f, kickDistance = 0.02f, kickAngle = 2.5f, hapticDuration = 0.08f,
            secondaryLocal = new Vector3(0f, -0.09f, -0.03f), clips = new[] { "smg_9mm_near", "smg_9mm_mid" }, handleNode = "Bolt", handleTravel = 0.06f,
        };

        public static GameObject BuildGun(GunSpec spec)
        {
            var root = GunRoot(spec.name, spec.folder, spec.file, spec.mass, spec.material, out var model, spec.modelEuler);
            var whole = WholeBounds(model);
            var trigger = NodeBounds(model, spec.triggerNode);
            var body = spec.bodyNode != null ? NodeBounds(model, spec.bodyNode) : whole;
            Vector3 muzzlePos;
            if (spec.barrelNode != null && model.transform.Find(spec.barrelNode) != null)
            {
                var barrel = NodeBounds(model, spec.barrelNode);
                muzzlePos = new Vector3(barrel.center.x, barrel.center.y, barrel.max.z);
            }
            else muzzlePos = new Vector3(whole.center.x, trigger.center.y + spec.muzzleYOffset, whole.max.z);
            var muzzle = Empty("Muzzle", root.transform, muzzlePos);

            Vector3 socketPos;
            Vector3 attachPos;
            Collider[] colliders;
            if (spec.pistolGrip)
            {
                var gripTop = new Vector3(body.center.x, body.min.y + body.size.y * 0.62f, body.min.z + 0.025f);
                var grip = new Bounds(new Vector3(gripTop.x, body.min.y + 0.035f, gripTop.z), new Vector3(0.03f, 0.07f, 0.036f));
                attachPos = grip.center + new Vector3(0f, 0.01f, 0f);
                socketPos = gripTop;
                colliders = new Collider[] { Box(root, "UpperCollider", new Bounds(new Vector3(body.center.x, body.max.y - 0.02f, body.center.z), new Vector3(0.035f, 0.045f, body.size.z)), Vector3.zero), Box(root, "GripCollider", grip, Vector3.zero) };
                var magNode = model.transform.Find("Clip") ?? model.transform.Find("Magazine");
                if (magNode != null) magNode.gameObject.SetActive(false);
            }
            else
            {
                var mag = NodeBounds(model, spec.magazineNode);
                model.transform.Find(spec.magazineNode).gameObject.SetActive(false);
                socketPos = new Vector3(mag.center.x, mag.max.y, mag.center.z);
                attachPos = trigger.center + new Vector3(0f, -0.03f, -0.06f);
                colliders = new Collider[] { Box(root, "BodyCollider", new Bounds(new Vector3(whole.center.x, whole.center.y, whole.center.z), new Vector3(0.07f, Mathf.Min(whole.size.y, 0.2f), whole.size.z)), Vector3.zero) };
            }
            var attach = Empty("Attach", root.transform, attachPos);
            var secondary = spec.twoHanded ? Empty("SecondaryAttach", root.transform, spec.secondaryLocal) : null;
            var socket = Socket(root, socketPos, spec.pistolGrip ? 0.06f : 0.07f);
            var light = MuzzleLight(muzzle.transform);
            Grab(root, attach.transform, secondary != null ? secondary.transform : null, colliders);

            var audio = SimVOAssetBuilder.Audio(root);
            var gun = spec.twoHanded ? root.AddComponent<Carbine>() : root.AddComponent<Pistol>();
            gun.displayName = spec.displayName;
            gun.muzzle = muzzle.transform;
            gun.audioSource = audio;
            gun.socket = socket;
            gun.magazinePrefab = spec.magazinePrefab.GetComponent<Magazine>();
            gun.muzzleFlashLight = light;
            gun.muzzleVelocity = spec.velocity;
            gun.minShotInterval = spec.interval;
            gun.swayStanding = spec.swayStanding;
            gun.swayKneeling = spec.swayKneeling;
            gun.swayProne = spec.swayProne;
            gun.twoHanded = spec.twoHanded;
            gun.automatic = spec.automatic;
            gun.recoilPivot = model.transform;
            gun.recoilKickDistance = spec.kickDistance;
            gun.recoilKickAngle = spec.kickAngle;
            gun.recoilHaptic = 1f;
            gun.recoilHapticDuration = spec.hapticDuration;
            gun.shotClips = Clips(spec.clips);
            socket.audioSource = audio;
            if (spec.handleNode != null) AddHandle(root, model, spec.handleNode, gun, spec.handleTravel);
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
            shotgun.recoilPivot = model.transform;
            shotgun.recoilKickDistance = 0.06f;
            shotgun.recoilKickAngle = 6f;
            shotgun.recoilRecoverSeconds = 0.22f;
            shotgun.recoilHaptic = 1f;
            shotgun.recoilHapticDuration = 0.25f;
            shotgun.projectilesPerShot = 8;
            shotgun.pelletSpread = 2.5f;
            shotgun.twoHanded = true;
            shotgun.requiresCockingAfterShot = true;
            port.shotgun = shotgun;
            AddHandle(root, model, "Fore_Stock", shotgun, 0.07f, 0.06f);
            Finish(root);
            return root;
        }

        public static GameObject BuildWeaponWall(GameObject pistol, GameObject shotgun, GameObject carbine, GameObject m4, GameObject ak, GameObject luger, GameObject smg)
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
            stand.weapons.Add(Hang(root, m4, "Slot3", new Vector3(-0.62f, 1.92f, 0.09f), 0.26f).GetComponent<Weapon>());
            stand.weapons.Add(Hang(root, ak, "Slot4", new Vector3(0.64f, 1.92f, 0.09f), 0.26f).GetComponent<Weapon>());
            stand.weapons.Add(Hang(root, luger, "Slot5", new Vector3(-0.1f, 1.0f, 0.09f), 0.1f).GetComponent<Weapon>());
            stand.weapons.Add(Hang(root, smg, "Slot6", new Vector3(0.66f, 1.0f, 0.09f), 0.2f).GetComponent<Weapon>());

            Label(root, "ПИСТОЛЕТ ПМ", new Vector3(-0.72f, 0.82f, 0.035f));
            Label(root, "КАРАБИН", new Vector3(0.98f, 1.3f, 0.035f));
            Label(root, "ДРОБОВИК", new Vector3(0.98f, 1.6f, 0.035f));
            Label(root, "ЛЮГЕР", new Vector3(-0.1f, 0.82f, 0.035f));
            Label(root, "M3", new Vector3(0.66f, 0.82f, 0.035f));
            Label(root, "M4A1", new Vector3(-0.62f, 2.1f, 0.035f));
            Label(root, "АК-47", new Vector3(0.64f, 2.1f, 0.035f));
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
