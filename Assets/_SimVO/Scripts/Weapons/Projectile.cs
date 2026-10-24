using SimVO.Core;
using SimVO.Targets;
using UnityEngine;

namespace SimVO.Weapons
{
    /// <summary>
    /// Pooled bullet moved by hand with a swept raycast each physics step: no tunnelling at 350 m/s,
    /// real travel time for leading moving targets, slight gravity drop.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        public float maxLifetime = 2f;
        public TrailRenderer trail;

        Vector3 velocity;
        float life;
        int hitMask;
        Weapon source;
        ProjectilePool pool;

        public void Launch(ProjectilePool owner, Vector3 position, Vector3 direction, float speed, Weapon weapon, int mask)
        {
            pool = owner;
            source = weapon;
            hitMask = mask;
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(direction);
            velocity = direction.normalized * speed;
            life = 0f;
            gameObject.SetActive(true);
            if (trail != null) trail.Clear();
        }

        void FixedUpdate()
        {
            var dt = Time.fixedDeltaTime;
            velocity += Physics.gravity * dt;
            var step = velocity * dt;
            var distance = step.magnitude;
            if (distance > 0f && Physics.Raycast(transform.position, step / distance, out var hit, distance, hitMask, QueryTriggerInteraction.Ignore))
            {
                OnHit(hit);
                return;
            }
            transform.position += step;
            life += dt;
            if (life > maxLifetime)
            {
                GameSession.Instance?.RegisterMiss();
                Despawn();
            }
        }

        void OnHit(RaycastHit hit)
        {
            transform.position = hit.point;
            var target = hit.collider.GetComponentInParent<Target>();
            if (target != null)
            {
                target.RegisterHit(hit.point, hit.normal, velocity.normalized);
            }
            else
            {
                GameSession.Instance?.RegisterMiss();
                FxLibrary.Instance?.SpawnHole(hit.point, hit.normal, hit.collider.transform);
            }
            Despawn();
        }

        void Despawn()
        {
            gameObject.SetActive(false);
            pool?.Return(this);
        }
    }
}
