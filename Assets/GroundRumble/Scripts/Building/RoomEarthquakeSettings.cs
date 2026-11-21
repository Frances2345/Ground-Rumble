using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private float spawnHeight = GameSettings.FloorHeight - 0.8f;
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

        [Header("Piso rompible")]
        [SerializeField] private bool breakableFloor = false;
        [SerializeField] private bool breakWithoutPlayer = true;
        [Range(0f, 1f)]
        [SerializeField] private float autoCollapseChance = 0.3f;
        [SerializeField] private float breakableCellSize = 5.5f;
        [Range(0f, 1f)]
        [SerializeField] private float minimumIntensityToBreak = 0.18f;
        [SerializeField] private float floorWarningDuration = 3f;
        [SerializeField] private float columnSafeMargin = 1.5f;
        [SerializeField] private float breakCooldown = 1.5f;

        [Header("Materiales")]
        [SerializeField] private Material debrisMaterial;

        public float WindowBreakMultiplier => Mathf.Max(0f, windowBreakMultiplier);

        private static Material fallbackDebrisMaterial;

        private readonly List<BreakableFloorSection> sections = new List<BreakableFloorSection>();

        private EarthquakeManager manager;
        private Transform player;
        private Coroutine debrisRoutine;
        private Coroutine autoCollapseRoutine;
        private bool subscribed;
        private bool sectionsBuilt;
        private BreakableFloorSection warningSection;
        private float warningTimer;
        private float nextBreakTime;

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

        public void ConfigureBreakableFloor(bool enabled)
        {
            if (TryGetFloorNumber(out int floorNumber))
            {
                breakableFloor = enabled && IsSelectedBreakableFloor(floorNumber);
            }
            else
            {
                breakableFloor = enabled;
            }

            EnableAutoCollapseIfNeeded();
        }

        private void Awake()
        {
            SanitizeBreakableSettings();

            if (TryGetFloorNumber(out int floorNumber))
            {
                breakableFloor = IsSelectedBreakableFloor(floorNumber);
            }

            EnableAutoCollapseIfNeeded();
        }

        private void EnableAutoCollapseIfNeeded()
        {
            if (breakableFloor)
            {
                breakWithoutPlayer = true;

                if (autoCollapseChance <= 0f)
                {
                    autoCollapseChance = 0.5f;
                }
            }
        }

        private void SanitizeBreakableSettings()
        {
            if (breakableCellSize < 1f)
            {
                breakableCellSize = 5.5f;
            }

            if (minimumIntensityToBreak <= 0f)
            {
                minimumIntensityToBreak = 0.18f;
            }

            if (floorWarningDuration <= 0f)
            {
                floorWarningDuration = 3f;
            }

            if (columnSafeMargin < 0f)
            {
                columnSafeMargin = 1.5f;
            }

            if (breakCooldown < 0f)
            {
                breakCooldown = 1.5f;
            }

            if (autoCollapseChance <= 0f)
            {
                autoCollapseChance = 0.5f;
            }
        }

        private bool TryGetFloorNumber(out int floorNumber)
        {
            floorNumber = 0;
            string objectName = gameObject.name;
            string prefix = "Piso_";

            if (!objectName.StartsWith(prefix))
            {
                return false;
            }

            return int.TryParse(objectName.Substring(prefix.Length), out floorNumber);
        }

        // Se eligen algunos pisos repartidos por el edificio (cada tercer piso
        // desde el 4). Los pisos 1-3 y 29-30 quedan solidos.
        private static bool IsSelectedBreakableFloor(int floorNumber)
        {
            if (floorNumber < 4 || floorNumber > 28)
            {
                return false;
            }

            return (floorNumber - 4) % 3 == 0;
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

            UpdateBreakableFloor();
        }

        private void UpdateBreakableFloor()
        {
            if (!breakableFloor)
            {
                return;
            }

            if (warningSection != null)
            {
                warningTimer -= Time.deltaTime;

                if (warningTimer <= 0f)
                {
                    if (player != null && warningSection.ContainsPlayer(player.position))
                    {
                        PlayerController controller = player.GetComponent<PlayerController>();

                        if (controller != null)
                        {
                            controller.MarkDangerousFall();
                        }
                    }

                    warningSection.BreakOpen();
                    warningSection = null;
                    nextBreakTime = Time.time + Mathf.Max(0f, breakCooldown);
                }

                return;
            }

            if (Time.time < nextBreakTime)
            {
                return;
            }

            if (manager == null || !manager.IsShaking)
            {
                return;
            }

            if (manager.Intensity < minimumIntensityToBreak)
            {
                return;
            }

            if (!IsPlayerNear())
            {
                return;
            }

            if (!sectionsBuilt)
            {
                BuildBreakableSections();
            }

            for (int i = 0; i < sections.Count; i++)
            {
                BreakableFloorSection section = sections[i];

                if (section == null || !section.CanBreak || !section.ContainsPlayer(player.position))
                {
                    continue;
                }

                warningSection = section;
                warningTimer = Mathf.Max(0.5f, floorWarningDuration);
                CameraShake.Instance?.SetFloorWarning(warningTimer);
                return;
            }
        }

        private void BuildBreakableSections()
        {
            sectionsBuilt = true;

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            List<BoxCollider> floorSlabs = new List<BoxCollider>();
            Material floorSurface = null;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];

                if (candidate == null || !candidate.name.Contains("Losa_Piso"))
                {
                    continue;
                }

                BoxCollider box = candidate as BoxCollider;

                if (box == null)
                {
                    continue;
                }

                floorSlabs.Add(box);

                if (floorSurface == null)
                {
                    Renderer renderer = box.GetComponent<Renderer>();

                    if (renderer != null)
                    {
                        floorSurface = renderer.sharedMaterial;
                    }
                }
            }

            if (floorSlabs.Count == 0)
            {
                return;
            }

            List<BoxCollider> ceilingSlabs = new List<BoxCollider>();
            Material ceilingSurface = null;
            float ceilingCenterY = 0f;
            float ceilingThickness = 0.25f;
            bool hasCeiling = TryGetCeilingBelow(ceilingSlabs, out ceilingSurface, out ceilingCenterY, out ceilingThickness);

            GameObject grid = new GameObject("Secciones_Piso_Romplible");
            grid.transform.SetParent(transform, false);

            for (int i = 0; i < floorSlabs.Count; i++)
            {
                BoxCollider slab = floorSlabs[i];
                Bounds bounds = slab.bounds;

                int cellsX = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / Mathf.Max(0.5f, breakableCellSize)));
                int cellsZ = Mathf.Max(1, Mathf.CeilToInt(bounds.size.z / Mathf.Max(0.5f, breakableCellSize)));
                float cellWidth = bounds.size.x / cellsX;
                float cellDepth = bounds.size.z / cellsZ;

                for (int x = 0; x < cellsX; x++)
                {
                    for (int z = 0; z < cellsZ; z++)
                    {
                        Vector3 center = new Vector3(
                            bounds.min.x + (x + 0.5f) * cellWidth,
                            bounds.center.y,
                            bounds.min.z + (z + 0.5f) * cellDepth);

                        bool nearColumn = IsNearColumn(center, cellWidth, cellDepth, bounds.max.y);

                        BreakableFloorSection floorSection = CreateSection(
                            grid.transform,
                            center,
                            cellWidth,
                            bounds.size.y,
                            cellDepth,
                            floorSurface,
                            !nearColumn);

                        if (!hasCeiling || !IsCeilingCovered(ceilingSlabs, center))
                        {
                            continue;
                        }

                        Vector3 ceilingCenter = new Vector3(center.x, ceilingCenterY, center.z);
                        BreakableFloorSection ceilingSection = CreateSection(
                            grid.transform,
                            ceilingCenter,
                            cellWidth,
                            ceilingThickness,
                            cellDepth,
                            ceilingSurface,
                            true,
                            false);

                        floorSection.SetLinkedSection(ceilingSection);
                    }
                }
            }

            for (int i = 0; i < floorSlabs.Count; i++)
            {
                DisableSlab(floorSlabs[i]);
            }

            for (int i = 0; i < ceilingSlabs.Count; i++)
            {
                DisableSlab(ceilingSlabs[i]);
            }
        }

        private bool TryGetCeilingBelow(List<BoxCollider> ceilingSlabs, out Material surface, out float centerY, out float thickness)
        {
            surface = null;
            centerY = 0f;
            thickness = 0.25f;

            Transform parent = transform.parent;

            if (parent == null)
            {
                return false;
            }

            Transform below = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform sibling = parent.GetChild(i);

                if (sibling == transform)
                {
                    continue;
                }

                float distance = transform.position.y - sibling.position.y;

                if (distance > 0.1f && distance < closestDistance)
                {
                    closestDistance = distance;
                    below = sibling;
                }
            }

            if (below == null)
            {
                return false;
            }

            Collider[] colliders = below.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];

                if (candidate == null || !candidate.name.Contains("Losa_Techo"))
                {
                    continue;
                }

                BoxCollider box = candidate as BoxCollider;

                if (box == null)
                {
                    continue;
                }

                ceilingSlabs.Add(box);

                if (surface == null)
                {
                    Renderer renderer = box.GetComponent<Renderer>();

                    if (renderer != null)
                    {
                        surface = renderer.sharedMaterial;
                    }

                    centerY = box.bounds.center.y;
                    thickness = box.bounds.size.y;
                }
            }

            return ceilingSlabs.Count > 0;
        }

        private static bool IsCeilingCovered(List<BoxCollider> ceilingSlabs, Vector3 cellCenter)
        {
            Vector3 point = new Vector3(cellCenter.x, 0f, cellCenter.z);

            for (int i = 0; i < ceilingSlabs.Count; i++)
            {
                Bounds bounds = ceilingSlabs[i].bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;

                if (point.x >= min.x && point.x <= max.x && point.z >= min.z && point.z <= max.z)
                {
                    return true;
                }
            }

            return false;
        }

        private static void DisableSlab(BoxCollider slab)
        {
            if (slab == null)
            {
                return;
            }

            Renderer renderer = slab.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.enabled = false;
            }

            slab.enabled = false;
        }

        private bool IsNearColumn(Vector3 center, float width, float depth, float topY)
        {
            float halfWidth = width * 0.5f;
            float halfDepth = depth * 0.5f;
            Vector3 point = new Vector3(center.x, topY, center.z);

            if (SafeZone.IsPositionSafe(point, columnSafeMargin))
            {
                return true;
            }

            if (SafeZone.IsPositionSafe(point + new Vector3(-halfWidth, 0f, -halfDepth), columnSafeMargin))
            {
                return true;
            }

            if (SafeZone.IsPositionSafe(point + new Vector3(halfWidth, 0f, -halfDepth), columnSafeMargin))
            {
                return true;
            }

            if (SafeZone.IsPositionSafe(point + new Vector3(-halfWidth, 0f, halfDepth), columnSafeMargin))
            {
                return true;
            }

            return SafeZone.IsPositionSafe(point + new Vector3(halfWidth, 0f, halfDepth), columnSafeMargin);
        }

        private BreakableFloorSection CreateSection(Transform parent, Vector3 center, float width, float height, float depth, Material material, bool breakable, bool register = true)
        {
            GameObject sectionObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sectionObject.name = "Seccion_Piso";
            sectionObject.transform.SetParent(parent, true);
            sectionObject.transform.position = center;
            sectionObject.transform.rotation = Quaternion.identity;

            Vector3 parentScale = parent.lossyScale;
            sectionObject.transform.localScale = new Vector3(
                width / Mathf.Max(0.0001f, parentScale.x),
                height / Mathf.Max(0.0001f, parentScale.y),
                depth / Mathf.Max(0.0001f, parentScale.z));

            Renderer renderer = sectionObject.GetComponent<Renderer>();

            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            Bounds sectionBounds = new Bounds(center, new Vector3(width, height, depth));
            BreakableFloorSection section = sectionObject.AddComponent<BreakableFloorSection>();
            section.Configure(sectionBounds, breakable);

            if (breakable && register)
            {
                sections.Add(section);
            }

            return section;
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
            TryScheduleAutoCollapse(duration, intensity);

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

            if (autoCollapseRoutine != null)
            {
                StopCoroutine(autoCollapseRoutine);
                autoCollapseRoutine = null;
            }
        }

        private void TryScheduleAutoCollapse(float duration, float intensity)
        {
            if (!breakableFloor || !breakWithoutPlayer)
            {
                return;
            }

            if (intensity < minimumIntensityToBreak)
            {
                return;
            }

            if (IsPlayerNear())
            {
                // El piso donde esta el jugador se rompe con aviso previo desde Update().
                return;
            }

            float chance = Mathf.Clamp01(autoCollapseChance + 0.25f * Mathf.Clamp01(intensity));

            if (Random.value > chance)
            {
                return;
            }

            if (autoCollapseRoutine != null)
            {
                StopCoroutine(autoCollapseRoutine);
            }

            autoCollapseRoutine = StartCoroutine(AutoCollapseRoutine(duration, intensity));
        }

        private IEnumerator AutoCollapseRoutine(float duration, float intensity)
        {
            float endTime = Time.time + Mathf.Max(1f, duration);
            int maxBreaks = Mathf.Clamp(1 + Mathf.RoundToInt(Mathf.Clamp01(intensity) * 2f), 1, 3);
            int breaks = 0;

            while (breaks < maxBreaks && Time.time < endTime)
            {
                float wait = Mathf.Min(Random.Range(0.8f, 2.2f), Mathf.Max(0.1f, endTime - Time.time));
                yield return new WaitForSeconds(wait);

                if (manager == null || !manager.IsShaking || !breakableFloor)
                {
                    autoCollapseRoutine = null;
                    yield break;
                }

                if (!sectionsBuilt)
                {
                    BuildBreakableSections();
                }

                BreakableFloorSection target = PickBreakableSection();

                if (target == null)
                {
                    break;
                }

                if (IsPlayerNear() && player != null && target.ContainsPlayer(player.position))
                {
                    warningSection = target;
                    warningTimer = Mathf.Max(0.5f, floorWarningDuration);
                    CameraShake.Instance?.SetFloorWarning(warningTimer);
                    break;
                }

                target.BreakOpen();
                breaks++;
                nextBreakTime = Time.time + Mathf.Max(0f, breakCooldown);
            }

            autoCollapseRoutine = null;
        }

        private BreakableFloorSection PickBreakableSection()
        {
            if (sections.Count == 0)
            {
                return null;
            }

            int startIndex = Random.Range(0, sections.Count);

            for (int i = 0; i < sections.Count; i++)
            {
                BreakableFloorSection candidate = sections[(startIndex + i) % sections.Count];

                if (candidate != null && candidate.CanBreak)
                {
                    return candidate;
                }
            }

            return null;
        }

        private bool IsPlayerNear()
        {
            if (!TryResolvePlayer())
            {
                return false;
            }

            // Cada cuarto cubre un piso completo, asi que solo importa que el
            // jugador este en esa altura (no la distancia horizontal al centro).
            float tolerance = Mathf.Max(3f, spawnHeight + 0.5f);
            return Mathf.Abs(player.position.y - transform.position.y) <= tolerance;
        }

        private bool TryResolvePlayer()
        {
            if (player != null)
            {
                return true;
            }

            PlayerController controller = Object.FindAnyObjectByType<PlayerController>();

            if (controller != null)
            {
                player = controller.transform;
            }

            return player != null;
        }

        private IEnumerator SpawnDebrisRoutine(float duration, float intensity)
        {
            int count = GameSettings.GetDebrisCount(debrisCount, intensity);

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
            Vector3 center = transform.position;

            if (TryResolvePlayer())
            {
                center = new Vector3(player.position.x, transform.position.y, player.position.z);
            }

            Vector2 circle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = center + new Vector3(circle.x, spawnHeight, circle.y);

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
