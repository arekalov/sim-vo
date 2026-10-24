using System.Collections;
using SimVO.Audio;
using SimVO.Core;
using UnityEngine;

namespace SimVO.Targets
{
    /// <summary>Flying clay pigeon: any hit is 10 points and shatters it; untouched it lands and is gone.</summary>
    public class ClayTarget : Target
    {
        public float lifetime = 4f;
        public Renderer disc;
        public GameObject fragmentPrefab;
        public int fragments = 7;

        public event System.Action<ClayTarget, bool> Finished;

        Vector3 velocity;
        bool done;

        public override int MaxPoints => 10;

        public override int Evaluate(Vector3 worldPoint, out Vector2 offset)
        {
            offset = Vector2.zero;
            return 10;
        }

        public void Launch(Vector3 initialVelocity)
        {
            velocity = initialVelocity;
            StartCoroutine(Fly());
        }

        IEnumerator Fly()
        {
            var t = 0f;
            while (t < lifetime && !done)
            {
                if (GameSession.Instance == null || !GameSession.Instance.Paused)
                {
                    velocity += Physics.gravity * 0.55f * Time.deltaTime;
                    transform.position += velocity * Time.deltaTime;
                    transform.Rotate(0f, 540f * Time.deltaTime, 0f, Space.Self);
                    t += Time.deltaTime;
                }
                yield return null;
                if (transform.position.y < 0.05f) break;
            }
            if (!done) Miss();
        }

        void Miss()
        {
            done = true;
            Finished?.Invoke(this, false);
            Destroy(gameObject);
        }

        protected override void OnHitVisual(Vector3 point, Vector3 normal, Vector3 direction, int points)
        {
            if (done) return;
            done = true;
            if (fragmentPrefab != null)
            {
                for (var i = 0; i < fragments; i++)
                {
                    var fragment = Instantiate(fragmentPrefab, transform.position, Random.rotation);
                    var body = fragment.GetComponent<Rigidbody>();
                    if (body != null) body.linearVelocity = velocity * 0.4f + Random.insideUnitSphere * 2.5f + direction * 1.5f;
                    Destroy(fragment, 2f);
                }
            }
            Finished?.Invoke(this, true);
            if (disc != null) disc.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            Destroy(gameObject, 0.6f);
        }
    }
}
