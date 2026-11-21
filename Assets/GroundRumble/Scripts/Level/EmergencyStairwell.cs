using System.Collections.Generic;
using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Ruta de escape alternativa: un hueco de escaleras de emergencia fijo,
    /// con rellanos, senalizacion y atajos de salto. Se construye en tiempo de
    /// ejecucion sobre el edificio ya generado, asi que funciona en cualquier
    /// escena sin tener que regenerarla desde el editor.
    /// </summary>
    public class EmergencyStairwell : MonoBehaviour
    {
        [Header("Ruta de emergencia")]
        [SerializeField] private bool enabled = true;
        [SerializeField] private float teleportCooldown = 0.9f;
        [SerializeField] private float landingYOffset = 0.55f;

        [Header("Atajos de salto")]
        [SerializeField] private bool enableJumpShortcuts = true;
        [SerializeField] private int shortcutEveryFloors = 5;
        [SerializeField] private int shortcutFloors = 2;

        [Header("Colores de senalizacion")]
        [SerializeField] private Color normalRouteColor = new Color(0.15f, 1f, 0.35f);
        [SerializeField] private Color shortcutRouteColor = new Color(1f, 0.62f, 0.1f);

        [Header("Camino seguro central")]
        [SerializeField] private bool rebuildSafePath = true;
        [SerializeField] private float safePathRadius = 4.2f;

        private BuildingGenerator generator;
        private GameObject routeRoot;
        private Material stepMaterial;
        private Material railMaterial;
        private Material normalSignMaterial;
        private Material shortcutSignMaterial;
        private float nextAllowedTime;

        public bool CanUseRoute => Time.time >= nextAllowedTime;

        public void NotifyRouteUsed()
        {
            nextAllowedTime = Time.time + Mathf.Max(0f, teleportCooldown);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRouteExists()
        {
            if (FindAnyObjectByType<EmergencyStairwell>() != null)
            {
                return;
            }

            BuildingGenerator generator = FindAnyObjectByType<BuildingGenerator>();

            if (generator == null)
            {
                return;
            }

            generator.gameObject.AddComponent<EmergencyStairwell>();
        }

        private void Awake()
        {
            if (!enabled)
            {
                return;
            }

            generator = GetComponent<BuildingGenerator>();

            if (generator == null)
            {
                return;
            }

            CreateMaterials();
            Build();
            RebuildSafePath();
        }

        /// <summary>
        /// Las columnas del edificio horneado en la escena estan pegadas a las
        /// paredes. Las reparte por todo el piso (bordes, esquinas e interior)
        /// con una distribucion aleatoria estable por piso.
        /// </summary>
        private void RebuildSafePath()
        {
            if (!rebuildSafePath)
            {
                return;
            }

            SafeZone[] zones = generator.GetComponentsInChildren<SafeZone>(true);
            Dictionary<int, List<SafeZone>> byFloor = new Dictionary<int, List<SafeZone>>();

            foreach (SafeZone zone in zones)
            {
                Transform parent = zone.transform.parent;

                if (parent == null || !parent.name.StartsWith("Piso_"))
                {
                    continue;
                }

                if (!int.TryParse(parent.name.Substring("Piso_".Length), out int floor))
                {
                    continue;
                }

                if (!byFloor.TryGetValue(floor, out List<SafeZone> list))
                {
                    list = new List<SafeZone>();
                    byFloor.Add(floor, list);
                }

                list.Add(zone);
            }

            float centerY = generator.FloorHeight * 0.5f;

            foreach (KeyValuePair<int, List<SafeZone>> pair in byFloor)
            {
                Vector2[] positions = SafePathLayout.GetPositions(
                    pair.Key, generator.FloorWidth, generator.FloorDepth);
                List<SafeZone> list = pair.Value;

                while (list.Count < positions.Length)
                {
                    SafeZone clone = Instantiate(list[0], list[0].transform.parent);
                    clone.name = "Columna_Ruta";
                    list.Add(clone);
                }

                for (int i = 0; i < list.Count; i++)
                {
                    if (i >= positions.Length)
                    {
                        list[i].gameObject.SetActive(false);
                        continue;
                    }

                    list[i].transform.localPosition = new Vector3(
                        positions[i].x,
                        centerY,
                        positions[i].y);

                    list[i].Configure(safePathRadius, generator.FloorHeight * 0.5f + 0.5f);
                }
            }
        }

        private void CreateMaterials()
        {
            stepMaterial = MaterialFactory.CreateLitMaterial(new Color(0.28f, 0.34f, 0.4f, 1f), 0.35f, 0.5f);
            stepMaterial.name = "Escalera_Emergency_Steps";

            railMaterial = MaterialFactory.CreateLitMaterial(new Color(0.75f, 0.78f, 0.82f, 1f), 0.5f, 0.7f);
            railMaterial.name = "Escalera_Emergency_Rails";

            normalSignMaterial = CreateSignMaterial(normalRouteColor);
            shortcutSignMaterial = CreateSignMaterial(shortcutRouteColor);
        }

        private static Material CreateSignMaterial(Color color)
        {
            Material material = MaterialFactory.CreateLitMaterial(color, 0.6f);
            material.name = "Senal_Ruta_Emergencia";

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.6f);
            }

            return material;
        }

        /// <summary>
        /// El rellano de cada piso esta en una esquina distinta (este/sur,
        /// oeste/norte) para obligar al jugador a cruzar la planta.
        /// </summary>
        private Vector3 GetLandingPosition(int floor)
        {
            bool east = floor % 2 == 0;
            float x = east
                ? generator.FloorWidth * 0.5f - 3.2f
                : -generator.FloorWidth * 0.5f + 3.2f;
            float z = east
                ? -generator.FloorDepth * 0.5f + 3.4f
                : generator.FloorDepth * 0.5f - 3.4f;
            float y = floor * generator.FloorHeight + generator.SlabThickness + 0.05f;

            return new Vector3(x, y, z);
        }

        private void Build()
        {
            routeRoot = new GameObject("Ruta_Emergencia");
            routeRoot.transform.SetParent(transform, false);

            int floors = generator.FloorCount;
            int jumpEvery = Mathf.Max(2, shortcutEveryFloors);
            int jumpFloors = Mathf.Clamp(shortcutFloors, 1, 8);

            for (int i = 0; i < floors; i++)
            {
                Vector3 landing = GetLandingPosition(i);
                bool east = i % 2 == 0;
                float stepDirection = east ? 1f : -1f;

                bool isShortcut = enableJumpShortcuts && i >= jumpEvery && i % jumpEvery == 0;
                Material sign = isShortcut ? shortcutSignMaterial : normalSignMaterial;

                // Relleno: plataforma con collider sobre la que se entra en el Trigger.
                CreateBox(
                    $"Relleno_{i + 1:00}",
                    routeRoot.transform,
                    landing + new Vector3(0f, 0.1f, 0f),
                    new Vector3(2.8f, 0.2f, 2.8f),
                    stepMaterial,
                    true);

                // Trigger de bajada al rellano del piso destino (lado opuesto).
                GameObject triggerObject = CreateBox(
                    $"Bajada_{i + 1:00}",
                    routeRoot.transform,
                    landing + new Vector3(0f, 1.1f, 0f),
                    new Vector3(2.2f, 2f, 2.2f),
                    null,
                    false);

                EmergencyLanding trigger = triggerObject.AddComponent<EmergencyLanding>();
                int target = isShortcut ? i - jumpFloors : i - 1;
                Vector3 targetLanding = GetLandingPosition(Mathf.Max(0, target));

                // Llegas al piso inferior en el MISMO lado donde pisaste, no en el
                // rellano de enfrente. Asi hay que cruzar la planta hasta la
                // siguiente senal. Se desplaza un poco hacia el centro para no
                // caer en un hueco del losa.
                Vector3 arrival = new Vector3(
                    landing.x - Mathf.Sign(landing.x) * 1.5f,
                    targetLanding.y,
                    landing.z - Mathf.Sign(landing.z) * 1.5f);

                // El edificio esta desplazado en la escena: local -> mundo.
                Vector3 destination = transform.TransformPoint(arrival + Vector3.up * landingYOffset);
                trigger.Configure(
                    this,
                    destination,
                    isShortcut ? "Salto de emergencia" : "Escalera de emergencia");

                // Escalones decorativos hacia el centro del piso (sin collider).
                for (int step = 0; step < 4; step++)
                {
                    CreateBox(
                        $"Escalon_{i + 1:00}_{step + 1}",
                        routeRoot.transform,
                        landing + new Vector3(0f, -step * 0.08f, stepDirection * (1.1f + step * 0.55f)),
                        new Vector3(2.4f, 0.14f, 0.5f),
                        stepMaterial,
                        false);
                }

                // Barandales.
                CreateBox(
                    $"Barandal_Izq_{i + 1:00}",
                    routeRoot.transform,
                    landing + new Vector3(-1.3f, 0.6f, stepDirection * 1.3f),
                    new Vector3(0.08f, 1f, 3.4f),
                    railMaterial,
                    false);

                CreateBox(
                    $"Barandal_Der_{i + 1:00}",
                    routeRoot.transform,
                    landing + new Vector3(1.3f, 0.6f, stepDirection * 1.3f),
                    new Vector3(0.08f, 1f, 3.4f),
                    railMaterial,
                    false);

                // Senal luminosa: verde = bajada normal, naranja = atajo.
                CreateBox(
                    $"Senal_{i + 1:00}",
                    routeRoot.transform,
                    landing + new Vector3(0f, 2.35f, stepDirection * 0.2f),
                    new Vector3(1.8f, 0.4f, 0.14f),
                    sign,
                    false);
            }
        }

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool keepCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;

            Renderer renderer = box.GetComponent<Renderer>();

            if (renderer != null)
            {
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }
                else
                {
                    // Es el volumen del Trigger: no debe dibujarse.
                    renderer.enabled = false;
                }
            }

            Collider collider = box.GetComponent<Collider>();

            if (collider != null && !keepCollider)
            {
                if (material == null)
                {
                    collider.isTrigger = true;
                }
                else
                {
                    collider.enabled = false;
                }
            }

            return box;
        }
    }
}
