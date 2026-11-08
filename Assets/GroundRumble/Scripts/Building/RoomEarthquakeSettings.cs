using System.Collections;
using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Configuracion de eventos por cuarto/piso. Controla cuantos escombros caen
    /// y con que facilidad se rompen las ventanas de este cuarto.
    /// </summary>
    public class RoomEarthquakeSettings : MonoBehaviour
    {
        [Header("Escombros")]
        [SerializeField] private bool enableDebris = true;
        [SerializeField] private int debrisCount = 10;
        [SerializeField] private float spawnRadius = 4f;
        [SerializeField] private float spawnHeight = 2.9f;
        [SerializeField] private float minSize = 0.2f;
        [SerializeField] private float maxSize = 0.55f;
        [SerializeField] private float debrisMinDamage = 5f;
        [SerializeField] private float debrisMaxDamage = 20f;
        [SerializeField] private float debrisLifetime = 10f;
        [SerializeField] private GameObject debrisPrefab;

        [Header("Activacion")]
        [SerializeField] private bool requirePlayerNear = true;
        [SerializeField] private float activationRadius = 12f;

        [Header("Ventanas")]
        [SerializeField] private float windowBreakMultiplier = 1f;

        [Header("Materiales")]
        [SerializeField] private Material debrisMaterial;

        public float WindowBreakMultiplier => Mathf.Max(0f, windowBreakMultiplier);

        private static Material fallbackDebrisMaterial;

        private EarthquakeManager manager;
        private Transform player;
        private Coroutine debrisRoutine;
        private bool subscribed;

        public void Configure(int count, bool playerNearRequired, Material material)
        {
            debrisCount = Mathf.Max(0, count);
            requirePlayerNear = playerNearRequired;
            debrisMaterial = material;
        }

        public void ConfigureActivationRadius(float radius)
        {
            activationRadius = Mathf.Max(0f, radius);
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (!subscribed)
            {
                TrySubscribe();
            }
        }

        private void TrySubscribe()
        {
            if (subscribed)
            {
                return;
            }

            manager = EarthquakeManager.Instance;

            if (manager == null)
            {
                return;
            }

            manager.OnEarthquakeStarted += HandleEarthquakeStarted;
            manager.OnEarthquakeEnded += HandleEarthquakeEnded;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || manager == null)
            {
                return;
            }

            manager.OnEarthquakeStarted -= HandleEarthquakeStarted;
            manager.OnEarthquakeEnded -= HandleEarthquakeEnded;
            subscribed = false;
        }

        private void HandleEarthquakeStarted(float duration, float intensity)
        {
            if (!enableDebris)
            {
                return;
            }

            if (requirePlayerNear && !IsPlayerNear())
            {
                return;
            }

            if (debrisRoutine != null)
            {
                StopCoroutine(debrisRoutine);
            }

            debrisRoutine = StartCoroutine(SpawnDebrisRoutine(duration, intensity));
        }

        private void HandleEarthquakeEnded()
        {
            if (debrisRoutine != null)
            {
                StopCoroutine(debrisRoutine);
                debrisRoutine = null;
            }
        }

        private bool IsPlayerNear()
        {
            if (player == null)
            {
                PlayerController controller = Object.FindAnyObjectByType<PlayerController>();

                if (controller != null)
                {
                    player = controller.transform;
                }
            }

            if (player == null)
            {
                return false;
            }

            return Vector3.Distance(player.position, transform.position) <= activationRadius;
        }

        private IEnumerator SpawnDebrisRoutine(float duration, float intensity)
        {
            int count = Mathf.Max(0, debrisCount);

            if (count == 0)
            {
                yield break;
            }

            float interval = Mathf.Max(0.05f, duration / count);

            for (int i = 0; i < count; i++)
            {
                float wait = Random.Range(interval * 0.35f, interval * 1.35f);
                yield return new WaitForSeconds(wait);

                if (requirePlayerNear && !IsPlayerNear())
                {
                    break;
                }

                SpawnDebris(intensity);
            }

            debrisRoutine = null;
        }

        private void SpawnDebris(float intensity)
        {
            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(circle.x, spawnHeight, circle.y);

            if (debrisPrefab != null)
            {
                GameObject instance = Instantiate(debrisPrefab, spawnPosition, Random.rotation, transform);
                EnsureDebrisComponents(instance, intensity);
                Destroy(instance, debrisLifetime);
                return;
            }

            float size = Random.Range(minSize, maxSize);
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.name = "Escombro";
            debris.transform.SetParent(transform, true);
            debris.transform.position = spawnPosition;
            debris.transform.rotation = Random.rotation;
            debris.transform.localScale = new Vector3(
                size * Random.Range(0.7f, 1.3f),
                size * Random.Range(0.5f, 1f),
                size * Random.Range(0.7f, 1.3f));

            Renderer debrisRenderer = debris.GetComponent<Renderer>();
            Material material = debrisMaterial != null ? debrisMaterial : GetFallbackDebrisMaterial();

            if (debrisRenderer != null && material != null)
            {
                debrisRenderer.sharedMaterial = material;
            }

            EnsureDebrisComponents(debris, intensity);
            Destroy(debris, debrisLifetime);
        }

        private void EnsureDebrisComponents(GameObject debris, float intensity)
        {
            Rigidbody body = debris.GetComponent<Rigidbody>();

            if (body == null)
            {
                body = debris.AddComponent<Rigidbody>();
            }

            body.mass = Mathf.Clamp(body.mass <= 0f ? 3f : body.mass, 1f, 40f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.AddTorque(Random.insideUnitSphere * (2f + intensity * 4f), ForceMode.Impulse);

            DebrisDamage damage = debris.GetComponent<DebrisDamage>();

            if (damage == null)
            {
                damage = debris.AddComponent<DebrisDamage>();
            }

            damage.Configure(debrisMinDamage, debrisMaxDamage);
        }

        private static Material GetFallbackDebrisMaterial()
        {
            if (fallbackDebrisMaterial == null)
            {
                fallbackDebrisMaterial = MaterialFactory.CreateLitMaterial(new Color(0.42f, 0.4f, 0.37f, 1f), 0.05f);
                fallbackDebrisMaterial.name = "Escombro";
            }

            return fallbackDebrisMaterial;
        }
    }
}
