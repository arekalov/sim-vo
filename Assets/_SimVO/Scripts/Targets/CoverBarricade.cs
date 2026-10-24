using System.Collections;
using SimVO.Core;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>
    /// Barricade that rises in front of the booth in cover mode and measures how long the shooter's head is
    /// visible to the target while it is hidden (needless exposure).
    /// </summary>
    public class CoverBarricade : MonoBehaviour
    {
        public Transform rising;
        public Collider[] blockers;
        public TargetSpawner spawner;
        public float riseHeight = 2.2f;
        public float riseSeconds = 0.9f;
        public AudioSource audioSource;
        [Header("Window that follows the shooter's position")]
        public Transform rightBottom;
        public Transform rightTop;
        public Transform sill;
        public float wallHeight = 1.95f;
        public float windowHeight = 0.35f;
        public float standingWindowBottom = 1.3f;
        public float kneelingWindowBottom = 0.78f;
        public float proneWindowBottom = 0.18f;
        public float windowSpeed = 1.5f;

        float windowBottom = -1f;

        GameSession session;
        bool shown;
        Coroutine slide;

        void Start()
        {
            session = GameSession.Instance;
            if (session != null) session.SettingsChanged += Apply;
            shown = session != null && session.settings.mode == ShootingMode.Cover;
            if (rising != null) rising.localPosition = new Vector3(0f, shown ? 0f : -riseHeight, 0f);
        }

        void OnDestroy()
        {
            if (session != null) session.SettingsChanged -= Apply;
        }

        void Apply()
        {
            var wanted = session.settings.mode == ShootingMode.Cover;
            if (wanted == shown) return;
            shown = wanted;
            if (slide != null) StopCoroutine(slide);
            slide = StartCoroutine(Slide(wanted));
        }

        IEnumerator Slide(bool up)
        {
            if (rising == null) yield break;
            if (audioSource != null) Audio.Sfx.Play(audioSource, Audio.ProceduralSfx.MotorHum, 0.8f);
            var from = rising.localPosition.y;
            var to = up ? 0f : -riseHeight;
            var t = 0f;
            while (t < riseSeconds)
            {
                t += Time.deltaTime;
                rising.localPosition = new Vector3(0f, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / riseSeconds)), 0f);
                yield return null;
            }
            rising.localPosition = new Vector3(0f, to, 0f);
            slide = null;
        }

        void UpdateWindow()
        {
            if (rightBottom == null || rightTop == null) return;
            var position = session != null ? session.CurrentPosition : BodyPosition.Standing;
            var wanted = position switch
            {
                BodyPosition.Kneeling => kneelingWindowBottom,
                BodyPosition.Prone => proneWindowBottom,
                _ => standingWindowBottom,
            };
            windowBottom = windowBottom < 0f ? wanted : Mathf.MoveTowards(windowBottom, wanted, windowSpeed * Time.deltaTime);
            var top = windowBottom + windowHeight;
            rightBottom.localPosition = new Vector3(rightBottom.localPosition.x, windowBottom * 0.5f, rightBottom.localPosition.z);
            rightBottom.localScale = new Vector3(rightBottom.localScale.x, Mathf.Max(0.01f, windowBottom), rightBottom.localScale.z);
            rightTop.localPosition = new Vector3(rightTop.localPosition.x, (top + wallHeight) * 0.5f, rightTop.localPosition.z);
            rightTop.localScale = new Vector3(rightTop.localScale.x, Mathf.Max(0.01f, wallHeight - top), rightTop.localScale.z);
            if (sill != null) sill.localPosition = new Vector3(sill.localPosition.x, windowBottom, sill.localPosition.z);
        }

        void Update()
        {
            UpdateWindow();
            if (!shown || session == null || session.State != SessionState.Running || session.settings.mode != ShootingMode.Cover) return;
            var head = Camera.main;
            var target = spawner != null ? spawner.ActiveTarget : null;
            if (head == null || target == null) return;
            var from = head.transform.position;
            var delta = target.transform.position - from;
            var ray = new Ray(from, delta.normalized);
            var blocked = false;
            foreach (var blocker in blockers)
            {
                if (blocker != null && blocker.Raycast(ray, out _, delta.magnitude))
                {
                    blocked = true;
                    break;
                }
            }
            session.RegisterExposure(Time.deltaTime, !blocked, spawner.TargetExposed);
        }
    }
}
