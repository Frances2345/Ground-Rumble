using System.Collections;
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

        [Header("Physics")]
        [SerializeField] private float pieceMass = 0.5f;
        [SerializeField] private float randomTorque = 3f;
        [SerializeField] private float pieceLifetime = 10f;

        private EarthquakeSystem earthquake;
        private Collider ceilingCollider;
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

            if (ceilingArea == null)
            {
                Debug.LogWarning("DebrisSpawner: asigna un techo en 'Ceiling Area'.", this);
                enabled = false;
                return;
            }

            ceilingCollider = ceilingArea.GetComponent<Collider>();
            if (ceilingCollider == null)
            {
                ceilingCollider = ceilingArea.GetComponentInChildren<Collider>();
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
            Bounds bounds = GetCeilingBounds();
            float marginX = Mathf.Min(ceilingMargin, bounds.extents.x * 0.9f);
            float marginZ = Mathf.Min(ceilingMargin, bounds.extents.z * 0.9f);
            float y = bounds.min.y - spawnHeightOffset;

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                float x = Random.Range(bounds.min.x + marginX, bounds.max.x - marginX);
                float z = Random.Range(bounds.min.z + marginZ, bounds.max.z - marginZ);
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
