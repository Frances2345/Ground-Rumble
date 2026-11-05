using System.Collections;
using GroundRumble.Level;
using UnityEngine;

namespace GroundRumble.Events
{
    [DisallowMultipleComponent]
    public class DebrisSpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject[] debrisPrefabs;

        [Header("Ceiling Area")]
        [SerializeField] private Transform ceilingArea;
        [SerializeField] private float ceilingMargin = 1f;
        [SerializeField] private float spawnHeightOffset = 0.5f;

        [Header("Spawn")]
        [SerializeField] private int piecesPerShake = 10;
        [SerializeField] private float spawnStagger = 0.12f;
        [SerializeField] private int maxSpawnAttempts = 40;
        [SerializeField] private float columnClearance = 1.5f;
        [SerializeField] private float spawnRadius = 6f;

        [Header("Physics")]
        [SerializeField] private float pieceMass = 0.5f;
        [SerializeField] private float randomTorque = 3f;
        [SerializeField] private float pieceLifetime = 10f;

        private EarthquakeSystem earthquake;
        private RoomGenerator building;
        private Transform player;
        private Collider ceilingCollider;
        private Material debrisMaterial;
        private bool wasShaking;
        private bool burstRunning;
        private int nextPrefabIndex;

        private void Start()
        {
            earthquake = GetComponent<EarthquakeSystem>();
            if (earthquake == null)
            {
                earthquake = FindAnyObjectByType<EarthquakeSystem>();
            }

            building = FindAnyObjectByType<RoomGenerator>();
            debrisMaterial = CreateDebrisMaterial();

            PlayerController playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.transform;
            }

            if (building == null)
            {
                if (ceilingArea == null)
                {
                    Debug.LogWarning("DebrisSpawner: asigna un techo en 'Ceiling Area' o un RoomGenerator.", this);
                    enabled = false;
                    return;
                }

                ceilingCollider = ceilingArea.GetComponent<Collider>();
                if (ceilingCollider == null)
                {
                    ceilingCollider = ceilingArea.GetComponentInChildren<Collider>();
                }
            }

            if (debrisPrefabs == null || debrisPrefabs.Length == 0)
            {
                Debug.LogWarning("DebrisSpawner: asigna al menos un prefab de escombro.", this);
                enabled = false;
                return;
            }

            int assignedPrefabs = 0;
            for (int i = 0; i < debrisPrefabs.Length; i++)
            {
                if (debrisPrefabs[i] != null)
                {
                    assignedPrefabs++;
                }
            }

            if (assignedPrefabs == 0)
            {
                Debug.LogWarning("DebrisSpawner: todos los prefabs del array están vacíos.", this);
                enabled = false;
            }
            else if (assignedPrefabs < debrisPrefabs.Length)
            {
                Debug.LogWarning($"DebrisSpawner: {debrisPrefabs.Length - assignedPrefabs} prefab(s) sin asignar. Revisa el array en el inspector.", this);
            }
        }

        private void Update()
        {
            if (earthquake == null)
            {
                return;
            }

            if (earthquake.IsShaking && !wasShaking && !burstRunning)
            {
                StartCoroutine(SpawnBurst());
            }

            wasShaking = earthquake.IsShaking;
        }

        private IEnumerator SpawnBurst()
        {
            burstRunning = true;
            SafeZone[] safeZones = FindObjectsByType<SafeZone>();

            for (int i = 0; i < piecesPerShake; i++)
            {
                SpawnPiece(safeZones);

                if (spawnStagger > 0f)
                {
                    yield return new WaitForSeconds(spawnStagger);
                }
            }

            burstRunning = false;
        }

        private void SpawnPiece(SafeZone[] safeZones)
        {
            if (!TryGetSpawnPoint(safeZones, out Vector3 spawnPoint))
            {
                return;
            }

            GameObject prefab = GetNextPrefab();
            if (prefab == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.Euler(
                Random.Range(0f, 360f),
                Random.Range(0f, 360f),
                Random.Range(0f, 360f));

            GameObject piece = Instantiate(prefab, spawnPoint, rotation, transform);
            ApplyMaterial(piece, debrisMaterial);

            Rigidbody body = piece.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = piece.AddComponent<Rigidbody>();
            }

            body.useGravity = true;
            body.isKinematic = false;
            body.mass = pieceMass;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);

            if (pieceLifetime > 0f)
            {
                Destroy(piece, pieceLifetime);
            }
        }

        private GameObject GetNextPrefab()
        {
            int count = debrisPrefabs.Length;
            for (int i = 0; i < count; i++)
            {
                int index = (nextPrefabIndex + i) % count;
                GameObject prefab = debrisPrefabs[index];
                if (prefab != null)
                {
                    nextPrefabIndex = (index + 1) % count;
                    return prefab;
                }
            }

            return null;
        }

        private bool TryGetSpawnPoint(SafeZone[] safeZones, out Vector3 point)
        {
            Vector3 center;
            Vector3 size;
            Vector3 anchor;

            if (building != null)
            {
                anchor = player != null ? player.position : building.BuildingCenter;
                building.GetCeilingFor(anchor, out center, out size);
            }
            else
            {
                Bounds bounds = GetCeilingBounds();
                center = new Vector3(bounds.center.x, bounds.min.y - spawnHeightOffset, bounds.center.z);
                size = new Vector3(bounds.size.x, 1f, bounds.size.z);
                anchor = center;
            }

            float marginX = Mathf.Min(ceilingMargin, size.x * 0.45f);
            float marginZ = Mathf.Min(ceilingMargin, size.z * 0.45f);

            float minX = center.x - size.x * 0.5f + marginX;
            float maxX = center.x + size.x * 0.5f - marginX;
            float minZ = center.z - size.z * 0.5f + marginZ;
            float maxZ = center.z + size.z * 0.5f - marginZ;

            if (player != null && spawnRadius > 0f)
            {
                minX = Mathf.Max(minX, player.position.x - spawnRadius);
                maxX = Mathf.Min(maxX, player.position.x + spawnRadius);
                minZ = Mathf.Max(minZ, player.position.z - spawnRadius);
                maxZ = Mathf.Min(maxZ, player.position.z + spawnRadius);
            }

            if (minX > maxX)
            {
                float midX = (minX + maxX) * 0.5f;
                minX = midX;
                maxX = midX;
            }

            if (minZ > maxZ)
            {
                float midZ = (minZ + maxZ) * 0.5f;
                minZ = midZ;
                maxZ = midZ;
            }

            float y = center.y - spawnHeightOffset;

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                float x = Random.Range(minX, maxX);
                float z = Random.Range(minZ, maxZ);
                point = new Vector3(x, y, z);

                if (!IsNearColumn(point, safeZones))
                {
                    return true;
                }
            }

            point = default;
            return false;
        }

        private Bounds GetCeilingBounds()
        {
            if (ceilingCollider != null)
            {
                return ceilingCollider.bounds;
            }

            Renderer ceilingRenderer = ceilingArea.GetComponentInChildren<Renderer>();
            if (ceilingRenderer != null)
            {
                return ceilingRenderer.bounds;
            }

            return new Bounds(ceilingArea.position, ceilingArea.lossyScale);
        }

        private bool IsNearColumn(Vector3 point, SafeZone[] safeZones)
        {
            for (int i = 0; i < safeZones.Length; i++)
            {
                SafeZone safeZone = safeZones[i];
                if (safeZone == null)
                {
                    continue;
                }

                Collider columnCollider = safeZone.GetComponent<Collider>();
                if (columnCollider != null)
                {
                    Bounds bounds = columnCollider.bounds;
                    if (point.x > bounds.min.x - columnClearance &&
                        point.x < bounds.max.x + columnClearance &&
                        point.z > bounds.min.z - columnClearance &&
                        point.z < bounds.max.z + columnClearance)
                    {
                        return true;
                    }
                }
                else
                {
                    Vector3 columnPosition = safeZone.transform.position;
                    float dx = point.x - columnPosition.x;
                    float dz = point.z - columnPosition.z;
                    if (dx * dx + dz * dz < columnClearance * columnClearance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Material CreateDebrisMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.name = "Debris (Runtime)";
            material.color = new Color(0.45f, 0.42f, 0.38f);
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.05f);
            }
            else if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.05f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            return material;
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            if (target == null || material == null)
            {
                return;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (ceilingArea == null)
            {
                return;
            }

            Bounds bounds = GetCeilingBounds();
            Vector3 center = new Vector3(bounds.center.x, bounds.min.y - spawnHeightOffset, bounds.center.z);
            Vector3 size = new Vector3(bounds.size.x, 0.02f, bounds.size.z);
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.35f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
