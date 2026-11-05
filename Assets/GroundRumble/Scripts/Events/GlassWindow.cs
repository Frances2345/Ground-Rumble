using GroundRumble.Player;
using UnityEngine;

namespace GroundRumble.Events
{
    [DisallowMultipleComponent]
    public class GlassWindow : MonoBehaviour
    {
        private Vector3 inward = Vector3.forward;
        private float breakChance = 0.75f;
        private float breakRadius = 12f;
        private int shardCount = 6;
        private float shardLifetime = 4f;
        private float damage = 12f;
        private float damageRadius = 3f;

        private EarthquakeSystem earthquake;
        private PlayerHealth playerHealth;
        private Renderer glassRenderer;
        private Collider glassCollider;
        private Material glassMaterial;
        private bool broken;

        public void Configure(
            Vector3 inwardDirection,
            float chance,
            float radius,
            int shards,
            float lifetime,
            float damageAmount,
            float damageRange)
        {
            inward = inwardDirection.normalized;
            breakChance = chance;
            breakRadius = radius;
            shardCount = shards;
            shardLifetime = lifetime;
            damage = damageAmount;
            damageRadius = damageRange;
        }

        private void Start()
        {
            glassRenderer = GetComponent<Renderer>();
            if (glassRenderer != null)
            {
                glassMaterial = glassRenderer.sharedMaterial;
            }

            glassCollider = GetComponent<Collider>();
            playerHealth = FindAnyObjectByType<PlayerHealth>();

            earthquake = EarthquakeSystem.Current != null
                ? EarthquakeSystem.Current
                : FindAnyObjectByType<EarthquakeSystem>();

            if (earthquake != null)
            {
                earthquake.ShakeStarted += OnShakeStarted;
            }
        }

        private void OnDestroy()
        {
            if (earthquake != null)
            {
                earthquake.ShakeStarted -= OnShakeStarted;
            }
        }

        private void OnShakeStarted()
        {
            if (broken)
            {
                return;
            }

            if (breakRadius > 0f)
            {
                if (playerHealth == null)
                {
                    playerHealth = FindAnyObjectByType<PlayerHealth>();
                }

                if (playerHealth != null &&
                    Vector3.Distance(transform.position, playerHealth.transform.position) > breakRadius)
                {
                    return;
                }
            }

            if (Random.value <= breakChance)
            {
                Break();
            }
        }

        private void Break()
        {
            broken = true;

            if (glassRenderer != null)
            {
                glassRenderer.enabled = false;
            }

            if (glassCollider != null)
            {
                glassCollider.enabled = false;
            }

            SpawnShards();
            ApplyDamage();
        }

        private void SpawnShards()
        {
            if (shardCount <= 0)
            {
                return;
            }

            for (int i = 0; i < shardCount; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "GlassShard";
                shard.transform.SetParent(transform.parent, true);
                shard.transform.position = transform.position + Random.insideUnitSphere * 0.35f;
                shard.transform.rotation = Random.rotation;
                shard.transform.localScale = new Vector3(
                    Random.Range(0.08f, 0.22f),
                    Random.Range(0.08f, 0.22f),
                    Random.Range(0.02f, 0.05f));

                Renderer shardRenderer = shard.GetComponent<Renderer>();
                if (shardRenderer != null && glassMaterial != null)
                {
                    shardRenderer.sharedMaterial = glassMaterial;
                }

                Rigidbody body = shard.AddComponent<Rigidbody>();
                body.mass = 0.05f;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                Vector3 force = inward * Random.Range(1.5f, 3.5f) + Random.insideUnitSphere * 1.5f;
                body.AddForce(force, ForceMode.Impulse);
                body.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

                if (shardLifetime > 0f)
                {
                    Destroy(shard, shardLifetime);
                }
            }
        }

        private void ApplyDamage()
        {
            if (damage <= 0f)
            {
                return;
            }

            if (playerHealth == null)
            {
                playerHealth = FindAnyObjectByType<PlayerHealth>();
            }

            if (playerHealth == null)
            {
                return;
            }

            if (damageRadius > 0f &&
                Vector3.Distance(transform.position, playerHealth.transform.position) > damageRadius)
            {
                return;
            }

            playerHealth.TakeDamage(damage);
        }
    }
}
