using System.Collections.Generic;
using SimVO.Core;
using UnityEngine;

namespace SimVO.Weapons
{
    public class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Instance { get; private set; }

        public Projectile prefab;
        public int prewarm = 24;

        readonly Queue<Projectile> free = new Queue<Projectile>();

        void Awake()
        {
            Instance = this;
            for (var i = 0; i < prewarm; i++) free.Enqueue(Create());
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        Projectile Create()
        {
            var projectile = Instantiate(prefab, transform);
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        public void Launch(Vector3 position, Vector3 direction, float speed, Weapon source)
        {
            var projectile = free.Count > 0 ? free.Dequeue() : Create();
            projectile.Launch(this, position, direction, speed, source, SimVOLayers.ProjectileHitMask);
        }

        public void Return(Projectile projectile)
        {
            free.Enqueue(projectile);
        }
    }
}
