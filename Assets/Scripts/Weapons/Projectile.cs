using UnityEngine;

namespace Popayork.Weapons
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class Projectile : MonoBehaviour
    {
        private Rigidbody body;
        private SphereCollider trigger;
        private ProjectilePool homePool;
        private float damage;
        private float lifeLeft;
        private bool live;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            trigger = GetComponent<SphereCollider>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            trigger.isTrigger = true;
        }

        public void Configure(ProjectilePool pool)
        {
            homePool = pool;
        }

        private void EnsureParts()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }
            if (trigger == null)
            {
                trigger = GetComponent<SphereCollider>();
            }
            if (body != null)
            {
                body.useGravity = false;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }

        // Sin asignaciones: solo activa y fija velocidad/daño.
        public void Launch(Vector3 origin, Vector3 direction, float speed, float hitDamage, float lifeSeconds)
        {
            EnsureParts();
            damage = hitDamage;
            lifeLeft = lifeSeconds;
            live = true;
            transform.position = origin;
            transform.rotation = Quaternion.LookRotation(direction);
            gameObject.SetActive(true);
            body.linearVelocity = direction * speed;
        }

        private void Update()
        {
            if (!live)
            {
                return;
            }
            lifeLeft -= Time.deltaTime;
            if (lifeLeft <= 0.0f)
            {
                Sleep();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!live)
            {
                return;
            }
            var dummy = other.GetComponentInParent<Popayork.World.TargetDummy>();
            Vector3 point = other.ClosestPoint(transform.position);
            if (dummy != null)
            {
                dummy.TakeHit(damage, body.linearVelocity.normalized);
            }
            if (ImpactPool.Instance != null)
            {
                ImpactPool.Instance.SpawnImpact(point, Vector3.up);
            }
            Sleep();
        }

        private void Sleep()
        {
            live = false;
            body.linearVelocity = Vector3.zero;
            gameObject.SetActive(false);
            if (homePool != null)
            {
                homePool.Return(this);
            }
        }
    }
}
