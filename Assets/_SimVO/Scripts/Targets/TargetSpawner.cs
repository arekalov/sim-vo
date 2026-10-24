using System.Collections;
using System.Collections.Generic;
using SimVO.Core;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>
    /// Runs the player's lane. Normal: a target per distance, hit it to move on. Advanced: each distance demands a body
    /// position. Cover: the target hides edge-on and shows itself for a few seconds at a time. Clays: the trap throws discs.
    /// </summary>
    public class TargetSpawner : MonoBehaviour
    {
        public TargetCarrier carrier;
        public Target ringTargetPrefab;
        public Target silhouetteTargetPrefab;
        public Target gongTargetPrefab;
        public Target hologramTargetPrefab;
        public ClayThrower clayThrower;
        public DuckGallery duckGallery;
        public List<float> distances = new List<float> { 5f, 10f, 15f };
        public float practiceDistance = 5f;
        public float swapDelay = 0.35f;
        public float coverExposureSeconds = 3f;
        public Vector2 coverHideSeconds = new Vector2(1.5f, 3.5f);
        public float clayInterval = 2.6f;

        public static readonly BodyPosition[] AdvancedPositions = { BodyPosition.Standing, BodyPosition.Kneeling, BodyPosition.Prone };

        public Target ActiveTarget => active;
        /// <summary>Cover mode: the target is face-on and armed right now.</summary>
        public bool TargetExposed { get; private set; }
        public string StageHint { get; private set; } = "";
        /// <summary>Which step the lane coroutine is in; for diagnostics.</summary>
        public string Phase { get; private set; } = "idle";
        public event System.Action HintChanged;

        Target active;
        int distanceIndex;
        GameSession session;
        bool hitFlag;
        Coroutine game;
        TargetMotion motion;

        bool Running => session != null && session.State == SessionState.Running;

        void Start()
        {
            session = GameSession.Instance;
            motion = carrier.targetMount != null ? carrier.targetMount.GetComponent<TargetMotion>() : null;
            if (session != null)
            {
                session.StateChanged += OnStateChanged;
                session.SettingsChanged += OnSettingsChanged;
            }
            Hang();
            ApplyMotion();
            carrier.DriveTo(practiceDistance);
        }

        void OnDestroy()
        {
            if (session == null) return;
            session.StateChanged -= OnStateChanged;
            session.SettingsChanged -= OnSettingsChanged;
        }

        Target Prefab
        {
            get
            {
                var kind = session != null ? session.settings.targetKind : TargetKind.Ring;
                return kind switch
                {
                    TargetKind.Silhouette when silhouetteTargetPrefab != null => silhouetteTargetPrefab,
                    TargetKind.Gong when gongTargetPrefab != null => gongTargetPrefab,
                    TargetKind.Hologram when hologramTargetPrefab != null => hologramTargetPrefab,
                    _ => ringTargetPrefab,
                };
            }
        }

        void OnSettingsChanged()
        {
            if (session.State == SessionState.Running) return;
            if (active == null || active.name != Prefab.name + "(Clone)") Hang();
            ApplyMotion();
        }

        void OnStateChanged()
        {
            if (session.State == SessionState.Running)
            {
                if (game != null) StopCoroutine(game);
                game = StartCoroutine(RunGame());
            }
            else
            {
                if (game != null) StopCoroutine(game);
                game = StartCoroutine(ReturnHome());
            }
        }

        void Hang()
        {
            if (active != null) Destroy(active.gameObject);
            var mount = carrier.targetMount != null ? carrier.targetMount : carrier.transform;
            active = Instantiate(Prefab, mount.position, mount.rotation, mount);
            active.HitRegistered += OnHit;
            active.Armed = true;
            TargetExposed = true;
        }

        void ClearHanging()
        {
            if (active != null) Destroy(active.gameObject);
            active = null;
            TargetExposed = false;
        }

        void ApplyMotion()
        {
            if (motion != null) motion.mode = session != null ? session.settings.motion : TargetMotionMode.Static;
            if (duckGallery != null) duckGallery.gameObject.SetActive(session != null && session.settings.targetKind == TargetKind.Ducks);
        }

        void SetHint(string hint)
        {
            if (StageHint == hint) return;
            StageHint = hint;
            HintChanged?.Invoke();
        }

        void OnHit(Target target, int points)
        {
            if (target != active || !target.Armed) return;
            hitFlag = true;
            target.Armed = false;
            TargetExposed = false;
        }

        IEnumerator RunGame()
        {
            distanceIndex = 0;
            if (session.settings.targetKind == TargetKind.Clay)
            {
                yield return RunClays();
                yield break;
            }
            if (session.settings.targetKind == TargetKind.Ducks)
            {
                yield return RunDucks();
                yield break;
            }
            while (Running)
            {
                var distance = distances[distanceIndex];
                session.RequiredPosition = AdvancedPositions[distanceIndex % AdvancedPositions.Length];
                Phase = $"swap->{distance}";
                yield return SwapAndDrive(distance);
                ApplyMotion();
                if (!Running) break;
                Phase = session.settings.mode == ShootingMode.Cover ? $"cover@{distance}" : $"stage@{distance}";
                if (session.settings.mode == ShootingMode.Cover) yield return CoverStage();
                else yield return PlainStage();
                distanceIndex = (distanceIndex + 1) % distances.Count;
            }
            Phase = "done";
            SetHint("");
            game = null;
        }

        IEnumerator PlainStage()
        {
            hitFlag = false;
            while (!hitFlag && Running)
            {
                var ready = session.PositionSatisfied;
                if (active != null) active.Armed = ready;
                TargetExposed = ready;
                SetHint(ready ? "" : $"Примите положение: {Labels.Position(session.RequiredPosition)}");
                yield return null;
            }
            SetHint("");
        }

        IEnumerator CoverStage()
        {
            hitFlag = false;
            if (active != null) active.Armed = false;
            TargetExposed = false;
            SetHint("Укройтесь");
            yield return carrier.TurnTo(90f);
            while (!hitFlag && Running)
            {
                yield return WaitGame(Random.Range(coverHideSeconds.x, coverHideSeconds.y));
                if (!Running) break;
                yield return carrier.TurnTo(0f);
                if (active != null) active.Armed = true;
                TargetExposed = true;
                SetHint("ОГОНЬ!");
                var exposure = session.settings.mode == ShootingMode.Cover && session.settings.gameType == GameType.Timed ? coverExposureSeconds * 0.8f : coverExposureSeconds;
                var t = 0f;
                while (t < exposure && !hitFlag && Running)
                {
                    if (!session.Paused) t += Time.deltaTime;
                    yield return null;
                }
                if (hitFlag) break;
                session.RegisterMissedExposure();
                if (active != null) active.Armed = false;
                TargetExposed = false;
                SetHint("Укройтесь");
                yield return carrier.TurnTo(90f);
            }
            TargetExposed = false;
            SetHint("");
        }

        IEnumerator RunClays()
        {
            SetHint("Тарелочки: стреляйте влёт");
            ClearHanging();
            var arrived = false;
            carrier.DriveTo(carrier.homeDistance, () => arrived = true);
            while (!arrived) yield return null;
            while (Running)
            {
                if (clayThrower == null) break;
                var clay = clayThrower.Throw();
                if (clay != null)
                {
                    clay.Armed = true;
                    var finished = false;
                    clay.Finished += (c, hit) =>
                    {
                        finished = true;
                        if (!hit) session.RegisterMissedExposure();
                    };
                    var t = 0f;
                    while (!finished && Running && t < 6f)
                    {
                        t += Time.deltaTime;
                        yield return null;
                    }
                }
                yield return WaitGame(clayInterval * 0.4f);
            }
            SetHint("");
            game = null;
        }

        IEnumerator RunDucks()
        {
            SetHint("Утки: сбивайте по одной");
            ClearHanging();
            ApplyMotion();
            var arrived = false;
            carrier.DriveTo(carrier.homeDistance, () => arrived = true);
            while (!arrived) yield return null;
            while (Running) yield return null;
            SetHint("");
            game = null;
        }

        IEnumerator WaitGame(float seconds)
        {
            var t = 0f;
            while (t < seconds && Running)
            {
                if (!session.Paused) t += Time.deltaTime;
                yield return null;
            }
        }

        IEnumerator SwapAndDrive(float distance)
        {
            Phase = "swapDelay";
            yield return WaitGame(swapDelay);
            Phase = "flip";
            yield return carrier.Flip(Hang);
            Phase = "drive";
            var arrived = false;
            carrier.DriveTo(distance, () => arrived = true);
            while (!arrived) yield return null;
            Phase = "arrived";
        }

        IEnumerator ReturnHome()
        {
            Phase = "returnHome";
            SetHint("");
            TargetExposed = false;
            var arrived = false;
            carrier.DriveTo(carrier.homeDistance, () => arrived = true);
            while (!arrived) yield return null;
            if (active != null)
            {
                yield return new WaitForSeconds(2.5f);
                yield return carrier.Flip(Hang);
            }
            else
            {
                Hang();
            }
            ApplyMotion();
            carrier.DriveTo(practiceDistance);
            game = null;
        }
    }
}
