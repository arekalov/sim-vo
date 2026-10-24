using System.Collections;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Ring target projected as a hologram: glowing rings, drifting scanlines, flicker and a glitch on hit.</summary>
    public class HologramTarget : RingTarget
    {
        public Renderer ringsRenderer;
        public Renderer scanlines;
        public Light glow;
        public float scrollSpeed = 0.35f;
        public Color tint = new Color(0.35f, 0.9f, 1f);

        MaterialPropertyBlock block;
        Vector2 scroll;
        float flash;
        float noise;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            noise = Random.value * 10f;
        }

        void Update()
        {
            noise += Time.deltaTime;
            var flicker = 0.82f + 0.12f * Mathf.PerlinNoise(noise * 6f, 0.3f) + 0.06f * Mathf.Sin(noise * 37f);
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 3f);
            var intensity = flicker + flash * 2f;
            if (ringsRenderer != null)
            {
                ringsRenderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", tint * intensity);
                ringsRenderer.SetPropertyBlock(block);
            }
            if (scanlines != null)
            {
                scroll.y -= Time.deltaTime * scrollSpeed;
                scanlines.GetPropertyBlock(block);
                block.SetColor("_BaseColor", tint * (0.35f * intensity));
                block.SetVector("_BaseMap_ST", new Vector4(1f, 1f, 0f, scroll.y));
                scanlines.SetPropertyBlock(block);
            }
            if (glow != null) glow.intensity = 1.2f * intensity;
        }

        protected override void OnHitVisual(Vector3 point, Vector3 normal, Vector3 direction, int points)
        {
            flash = 1f;
            StartCoroutine(Glitch());
        }

        IEnumerator Glitch()
        {
            var t = 0f;
            var rest = transform.localScale;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                var jitter = 1f + 0.08f * Mathf.Sin(t * 90f) * (1f - t / 0.25f);
                transform.localScale = new Vector3(rest.x * jitter, rest.y, rest.z);
                yield return null;
            }
            transform.localScale = rest;
        }
    }
}
