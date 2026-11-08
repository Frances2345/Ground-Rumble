using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Vidrio rompible. Si el jugador esta cerca durante un terremoto, puede romperse
    /// y generar añicos con fisica que hacen dano.
    /// </summary>
    public class BreakableWindow : MonoBehaviour
    {
        [Header("Rotura")]
        [SerializeField] private bool breakOnlyDuringEarthquake = true;
        [SerializeField] private float playerBreakRadius = 5f;
        [SerializeField] private float baseBreakChancePerSecond = 0.35f;
        [SerializeField] private float impactBreakSpeed = 6f;

        [Header("Anicos")]
        [SerializeField] private bool spawnShards = true;
        [SerializeField] private int shardCount = 10;
        [SerializeField] private float shardSize = 0.22f;
        [SerializeField] private float shardForce = 2.5f;
        [SerializeField] private float shardLifetime = 6f;
        [SerializeField] private float shardDamageMin = 3f;
        [SerializeField] private float shardDamageMax = 12f;
        [SerializeField] private Material shardMaterial;

        private static Material fallbackShardMaterial;

        private Renderer glassRenderer;
        private Collider glassCollider;
        private RoomEarthquakeSettings room;
        private EarthquakeManager manager;
        private Transform player;
        private bool broken;

        private void Awake()
        {
            glassRenderer = GetComponent<Renderer>();
            glassCollider = GetComponent<Collider>();
            room = GetComponentInParent<RoomEarthquakeSettings>();
        }

        private void Start()
        {
            FindPlayer();
        }

        public void Configure(Material shardsMaterial, float breakRadius, float chancePerSecond, int shards)
        {
            shardMaterial = shardsMaterial;
            playerBreakRadius = breakRadius;
            baseBreakChancePerSecond = chancePerSecond;
            shardCount = Mathf.Max(0, shards);
        }

        private void Update()
        {
            if (broken)
            {
                return;
            }

            if (manager == null)
            {
                manager = EarthquakeManager.Instance;
            }

            if (breakOnlyDuringEarthquake && (manager == null || !manager.IsShaking))
            {
                return;
            }

            if (player == null)
            {
                FindPlayer();
            }

            if (player == null)
            {
                return;
            }

            if (Vector3.Distance(player.position, transform.position) > playerBreakRadius)
            {
                return;
            }

            float intensity = manager != null ? manager.Intensity : 1f;
            float roomMultiplier = room != null ? room.WindowBreakMultiplier : 1f;
            float chance = baseBreakChancePerSecond * Mathf.Lerp(0.35f, 1.6f, intensity) * roomMultiplier;

            if (Random.value < chance * Time.deltaTime)
            {
                Break();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (broken)
            {
                return;
            }

            DebrisDamage debris = collision.collider.GetComponentInParent<DebrisDamage>();

            if (debris != null)
            {
                Break();
                return;
            }

            Rigidbody body = collision.rigidbody;

            if (body != null && body.linearVelocity.magnitude >= impactBreakSpeed)
            {
                Break();
            }
        }

        private void FindPlayer()
        {
            PlayerController controller = Object.FindAnyObjectByType<PlayerController>();

            if (controller != null)
            {
                player = controller.transform;
            }
        }

        public void Break()
        {
            if (broken)
            {
                return;
            }

            broken = true;

            if (glassRenderer != null)
            {
                glassRenderer.enabled = false;
            }

            if (glassCollider != null)
            {
                glassCollider.enabled = false;
            }

            if (spawnShards)
            {
                SpawnShards();
            }

            Destroy(gameObject, shardLifetime + 1f);
        }

        private void SpawnShards()
        {
            Vector3 scale = transform.lossyScale;
            Vector3 halfSize = scale * 0.5f;
            bool thinOnX = scale.x <= scale.z;
            Material material = shardMaterial != null ? shardMaterial : GetFallbackShardMaterial();

            for (int i = 0; i < shardCount; i++)
            {
                Vector3 localOffset = new Vector3(
                    Random.Range(-halfSize.x, halfSize.x),
                    Random.Range(-halfSize.y, halfSize.y),
                    Random.Range(-halfSize.z, halfSize.z));

                Vector3 worldPosition = transform.TransformPoint(localOffset);

                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Anico_Vidrio";
                shard.transform.SetParent(transform.parent, true);
                shard.transform.position = worldPosition;
                shard.transform.rotation = transform.rotation;

                float sizeX = Random.Range(shardSize * 0.45f, shardSize);
                float sizeY = Random.Range(shardSize * 0.45f, shardSize);
                float sizeZ = Random.Range(shardSize * 0.45f, shardSize);

                shard.transform.localScale = thinOnX
                    ? new Vector3(0.02f, sizeY, sizeZ)
                    : new Vector3(sizeX, sizeY, 0.02f);

                Renderer shardRenderer = shard.GetComponent<Renderer>();

                if (shardRenderer != null && material != null)
                {
                    shardRenderer.sharedMaterial = material;
                }

                Rigidbody body = shard.AddComponent<Rigidbody>();
                body.mass = 0.15f;
                body.interpolation = RigidbodyInterpolation.Interpolate;

                Vector3 direction = worldPosition - transform.position;

                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = Random.insideUnitSphere;
                }

                body.AddForce((direction.normalized * shardForce) + Vector3.down * shardForce, ForceMode.Impulse);
                body.AddTorque(Random.insideUnitSphere * shardForce, ForceMode.Impulse);

                DebrisDamage damage = shard.AddComponent<DebrisDamage>();
                damage.Configure(shardDamageMin, shardDamageMax);

                Destroy(shard, shardLifetime + Random.Range(0f, 1.5f));
            }
        }

        private static Material GetFallbackShardMaterial()
        {
            if (fallbackShardMaterial == null)
            {
                fallbackShardMaterial = MaterialFactory.CreateGlassShardMaterial();
            }

            return fallbackShardMaterial;
        }
    }
}
