using UnityEngine;
using UnityEngine.Rendering;

namespace GroundRumble
{
    /// <summary>
    /// Genera un edificio editable de 30 pisos. Cada piso es un cuarto con paredes,
    /// losa, techo y ventanas de vidrio rompibles.
    /// </summary>
    public class BuildingGenerator : MonoBehaviour
    {
        private const int CurrentLayoutVersion = 4;

        private enum WallSide
        {
            North,
            South,
            East,
            West
        }

        public enum HoleCorner
        {
            TopRight,
            TopLeft,
            BottomRight,
            BottomLeft
        }

        [Header("Rampas entre pisos")]
        [SerializeField] private bool generateRamps = true;
        [Range(15f, 42f)]
        [SerializeField] private float rampAngle = 42f;
        [SerializeField] private float rampWidth = 3.4f;
        [SerializeField] private float rampThickness = 0.28f;
        [SerializeField] private float rampRailHeight = 0.3f;
        [Tooltip("Solo como reserva si falta el prefab; nunca se crea por codigo.")]
        [SerializeField] private Material rampMaterial;
        [SerializeField] private GameObject rampPrefab;

        [Header("Dimensiones")]
        [SerializeField] private int floorCount = 30;
        [SerializeField] private float floorHeight = GameSettings.FloorHeight;
        [SerializeField] private float floorWidth = 42f;
        [SerializeField] private float floorDepth = 36f;
        [SerializeField] private float slabThickness = 0.25f;
        [SerializeField] private float wallThickness = 0.2f;

        [Header("Hueco para bajar")]
        [SerializeField] private bool generateFloorHoles = true;
        [SerializeField] private float holeSize = 4f;
        [SerializeField] private float holeMarginFromWall = 1f;

        [Header("Columnas / zonas seguras")]
        [SerializeField] private bool generateSafeColumns = true;
        [SerializeField] private float columnOffsetX = 19.8f;
        [SerializeField] private float columnOffsetZ = 16.2f;
        [SerializeField] private float columnRadius = 0.4f;
        [SerializeField] private float safeZoneRadius = 1.8f;
        [SerializeField] private Material columnMaterial;
        [SerializeField] private GameObject columnPrefab;

        [Header("Salida del nivel")]
        [SerializeField] private bool generateExit = true;
        [SerializeField] private Material exitMaterial;
        [SerializeField] private string finalSceneName = "Final";

        [Header("Ventanas")]
        [SerializeField] private bool generateWindows = true;
        [SerializeField] private int windowsPerWall = 4;
        [Range(0f, 1f)]
        [SerializeField] private float fullGlassWallChance = 0.3f;
        [SerializeField] private float windowWidth = 2.4f;
        [SerializeField] private float windowHeight = 1.7f;
        [SerializeField] private float windowSillHeight = 0.9f;
        [SerializeField] private float windowBreakRadius = 5f;
        [SerializeField] private float windowBreakChance = 0.35f;
        [SerializeField] private int windowShardCount = 10;

        [Header("Oficinas interiores")]
        [SerializeField] private bool generateOfficeRooms = true;
        [SerializeField] private float officeWallThickness = 0.16f;
        [SerializeField] private float officeDoorWidth = 2.8f;
        [SerializeField] private float officeDoorHeight = 2.2f;

        [Header("Eventos por cuarto")]
        [SerializeField] private int debrisCountPerRoom = 10;
        [SerializeField] private bool debrisRequirePlayerNear = true;
        [SerializeField] private float roomActivationRadius = 12f;

        [Header("Pisos rompibles")]
        [SerializeField] private bool breakableFloors = true;

        [Header("Materiales")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material ceilingMaterial;
        [SerializeField] private Material glassMaterial;
        [SerializeField] private Material shardMaterial;
        [SerializeField] private Material debrisMaterial;

        [Header("Otros")]
        [SerializeField] private bool generateRoof = true;
        [SerializeField] private string generatedRootName = "Edificio_Generado";
        [SerializeField, HideInInspector] private int layoutVersion = CurrentLayoutVersion;

        public int FloorCount => Mathf.Max(1, floorCount);
        public float FloorHeight => floorHeight;
        public float SlabThickness => slabThickness;

        public Vector3 GetTopFloorSpawnPosition()
        {
            float topFloorY = (FloorCount - 1) * floorHeight;
            return transform.TransformPoint(new Vector3(0f, topFloorY + slabThickness + 0.05f, -2f));
        }

        public void SetMaterials(Material wall, Material floor, Material ceiling, Material glass, Material shards, Material debris, Material exit = null, Material ramp = null)
        {
            wallMaterial = wall;
            floorMaterial = floor;
            ceilingMaterial = ceiling;
            glassMaterial = glass;
            shardMaterial = shards;
            debrisMaterial = debris;
            rampMaterial = ramp;

            if (exit != null)
            {
                exitMaterial = exit;
            }
        }

        public void SetColumnMaterial(Material material)
        {
            if (material != null)
            {
                columnMaterial = material;
            }
        }

        public void SetColumnPrefab(GameObject prefab)
        {
            columnPrefab = prefab;
        }

        public void SetRampPrefab(GameObject prefab)
        {
            rampPrefab = prefab;
        }

        /// <summary>
        /// Aplica el tamano del edificio y recalcula las columnas para el nuevo perimetro.
        /// No regenera: llamar a <see cref="Generate"/> despues.
        /// </summary>
        public void ConfigureLayout(int floors, float height, float width, float depth, int windows, float glassChance)
        {
            floorCount = Mathf.Max(1, floors);
            floorHeight = Mathf.Max(1f, height);
            floorWidth = Mathf.Max(6f, width);
            floorDepth = Mathf.Max(6f, depth);
            windowsPerWall = Mathf.Clamp(windows, 0, 12);
            fullGlassWallChance = Mathf.Clamp01(glassChance);
            columnOffsetX = floorWidth * 0.5f - 1.2f;
            columnOffsetZ = floorDepth * 0.5f - 1.8f;
            safeZoneRadius = Mathf.Max(1.2f, columnRadius * 4.5f);
        }

        public void SetRoomDefaults(int debrisCount, bool requirePlayerNear, float activationRadius)
        {
            debrisCountPerRoom = Mathf.Max(0, debrisCount);
            debrisRequirePlayerNear = requirePlayerNear;
            roomActivationRadius = Mathf.Max(0f, activationRadius);
        }

        private void Start()
        {
            if (!Application.isPlaying || layoutVersion >= CurrentLayoutVersion)
            {
                return;
            }

            floorHeight = GameSettings.FloorHeight;
            ConfigureLayout(floorCount, floorHeight, floorWidth, floorDepth, windowsPerWall, fullGlassWallChance);
            Generate();
            RepositionPlayer();
            Debug.Log("Ground Rumble: edificio regenerado con rampas y altura de piso actualizada.");
        }

        private void RepositionPlayer()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();

            if (player == null)
            {
                return;
            }

            CharacterController controller = player.GetComponent<CharacterController>();

            if (controller != null)
            {
                controller.enabled = false;
            }

            player.ConfigureFloorHeight(floorHeight);

            Vector3 spawn = GetTopFloorSpawnPosition();
            Quaternion spawnRotation = transform.rotation;
            player.transform.SetPositionAndRotation(spawn, spawnRotation);

            if (controller != null)
            {
                controller.enabled = true;
            }

            Vida health = player.GetComponent<Vida>();

            if (health != null)
            {
                health.SetSpawnPoint(spawn, spawnRotation);
            }
        }

        [ContextMenu("Generar Edificio (30 pisos)")]
        public void Generate()
        {
            ClearGenerated();
            EnsureMaterials();

            GameObject root = new GameObject(generatedRootName);
            root.transform.SetParent(transform, false);

            for (int i = 0; i < Mathf.Max(1, floorCount); i++)
            {
                BuildFloor(root.transform, i);
            }

            layoutVersion = CurrentLayoutVersion;
            MarkSceneDirty();
        }

        [ContextMenu("Limpiar Edificio Generado")]
        public void ClearGenerated()
        {
            Transform existing = transform.Find(generatedRootName);

            if (existing == null)
            {
                return;
            }

            existing.gameObject.SetActive(false);

            if (Application.isPlaying)
            {
                existing.name = $"{generatedRootName}_Obsoleto";
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }

            MarkSceneDirty();
        }

        private void EnsureMaterials()
        {
            if (wallMaterial == null)
            {
                wallMaterial = MaterialFactory.CreateLitMaterial(new Color(0.72f, 0.72f, 0.75f, 1f), 0.15f);
                wallMaterial.name = "Muro";
            }

            if (floorMaterial == null)
            {
                floorMaterial = MaterialFactory.CreateLitMaterial(new Color(0.55f, 0.55f, 0.58f, 1f), 0.2f);
                floorMaterial.name = "Piso";
            }

            if (ceilingMaterial == null)
            {
                ceilingMaterial = floorMaterial;
            }

            if (glassMaterial == null)
            {
                glassMaterial = MaterialFactory.CreateGlassMaterial();
            }

            if (shardMaterial == null)
            {
                shardMaterial = MaterialFactory.CreateGlassShardMaterial();
            }

            if (debrisMaterial == null)
            {
                debrisMaterial = MaterialFactory.CreateLitMaterial(new Color(0.42f, 0.4f, 0.37f, 1f), 0.05f);
                debrisMaterial.name = "Escombro";
            }

            if (columnMaterial == null)
            {
                columnMaterial = MaterialFactory.CreateLitMaterial(new Color(0.3f, 0.62f, 0.45f, 1f), 0.35f);
                columnMaterial.name = "Columna";
            }

            // El material de la rampa nunca se genera en codigo: la rampa es el
            // prefab Rampa.prefab y todos sus hijos usan Rampa.mat (el asset).
        }

        private void BuildFloor(Transform buildingRoot, int index)
        {
            GameObject floor = new GameObject($"Piso_{index + 1:00}");
            floor.transform.SetParent(buildingRoot, false);
            floor.transform.localPosition = new Vector3(0f, index * floorHeight, 0f);

            RoomEarthquakeSettings room = floor.AddComponent<RoomEarthquakeSettings>();
            room.Configure(debrisCountPerRoom, debrisRequirePlayerNear, debrisMaterial);
            room.ConfigureActivationRadius(roomActivationRadius);
            room.ConfigureBreakableFloor(breakableFloors);

            // La losa del piso 1 es solida; del piso 2 en adelante tiene el hueco
            // por el que se baja desde el piso de arriba.
            if (index == 0 || !generateFloorHoles)
            {
                CreateBox(
                    "Losa_Piso",
                    floor.transform,
                    new Vector3(0f, slabThickness * 0.5f, 0f),
                    new Vector3(floorWidth, slabThickness, floorDepth),
                    floorMaterial);
            }
            else
            {
                CreateSlabWithHole(
                    "Losa_Piso",
                    floor.transform,
                    slabThickness * 0.5f,
                    slabThickness,
                    floorMaterial,
                    GetHoleCorner(index - 1));
            }

            if (index < FloorCount - 1)
            {
                // El techo de este piso es la losa del piso de arriba: mismo hueco.
                CreateSlabWithHole(
                    "Losa_Techo",
                    floor.transform,
                    floorHeight - slabThickness * 0.5f,
                    slabThickness,
                    ceilingMaterial,
                    GetHoleCorner(index));
            }
            else if (generateRoof)
            {
                CreateBox(
                    "Losa_Techo",
                    floor.transform,
                    new Vector3(0f, floorHeight - slabThickness * 0.5f, 0f),
                    new Vector3(floorWidth, slabThickness, floorDepth),
                    ceilingMaterial);
            }

            if (index > 0 && generateRamps)
            {
                CreateRampBetweenFloors(floor.transform, GetHoleCorner(index - 1));
            }

            BuildWall(floor.transform, WallSide.North);
            BuildWall(floor.transform, WallSide.South);
            BuildWall(floor.transform, WallSide.East);
            BuildWall(floor.transform, WallSide.West);

            if (generateSafeColumns)
            {
                CreateSafeColumns(floor.transform);
            }

            if (generateOfficeRooms)
            {
                CreateOfficeRooms(floor.transform, index);
            }

            if (index == 0 && generateExit)
            {
                CreateLevelExit(floor.transform);
            }
        }

        private HoleCorner GetHoleCorner(int boundaryIndex)
        {
            int value = ((boundaryIndex % 4) + 4) % 4;
            return (HoleCorner)value;
        }

        private void GetHoleLayout(HoleCorner corner, out float centerX, out float centerZ, out float size)
        {
            float halfWidth = floorWidth * 0.5f;
            float halfDepth = floorDepth * 0.5f;
            float maxHole = Mathf.Min(floorWidth, floorDepth) - 1.2f;
            size = Mathf.Clamp(holeSize, 1.2f, Mathf.Max(1.2f, maxHole));

            float maxMarginX = Mathf.Max(0.1f, halfWidth - size * 0.5f - 0.1f);
            float maxMarginZ = Mathf.Max(0.1f, halfDepth - size * 0.5f - 0.1f);
            float marginX = Mathf.Clamp(holeMarginFromWall, 0.1f, maxMarginX);
            float marginZ = Mathf.Clamp(holeMarginFromWall, 0.1f, maxMarginZ);

            bool right = corner == HoleCorner.TopRight || corner == HoleCorner.BottomRight;
            bool top = corner == HoleCorner.TopRight || corner == HoleCorner.TopLeft;
            float signX = right ? 1f : -1f;
            float signZ = top ? 1f : -1f;

            centerX = signX * (halfWidth - marginX - size * 0.5f);
            centerZ = signZ * (halfDepth - marginZ - size * 0.5f);
        }

        private void CreateSlabWithHole(string name, Transform parent, float centerY, float thickness, Material material, HoleCorner corner)
        {
            GetHoleLayout(corner, out float holeCenterX, out float holeCenterZ, out float size);
            float halfWidth = floorWidth * 0.5f;
            float halfDepth = floorDepth * 0.5f;
            float holeMinX = holeCenterX - size * 0.5f;
            float holeMaxX = holeCenterX + size * 0.5f;
            float holeMinZ = holeCenterZ - size * 0.5f;
            float holeMaxZ = holeCenterZ + size * 0.5f;

            GameObject slab = new GameObject(name);
            slab.transform.SetParent(parent, false);
            slab.transform.localPosition = new Vector3(0f, centerY, 0f);

            float lowDepth = holeMinZ - (-halfDepth);

            if (lowDepth > 0.01f)
            {
                CreateBox(
                    $"{name}_Bajo",
                    slab.transform,
                    new Vector3(0f, 0f, (-halfDepth + holeMinZ) * 0.5f),
                    new Vector3(floorWidth, thickness, lowDepth),
                    material);
            }

            float highDepth = halfDepth - holeMaxZ;

            if (highDepth > 0.01f)
            {
                CreateBox(
                    $"{name}_Alto",
                    slab.transform,
                    new Vector3(0f, 0f, (holeMaxZ + halfDepth) * 0.5f),
                    new Vector3(floorWidth, thickness, highDepth),
                    material);
            }

            float bandDepth = holeMaxZ - holeMinZ;
            float westWidth = holeMinX - (-halfWidth);

            if (westWidth > 0.01f)
            {
                CreateBox(
                    $"{name}_Oeste",
                    slab.transform,
                    new Vector3((-halfWidth + holeMinX) * 0.5f, 0f, holeCenterZ),
                    new Vector3(westWidth, thickness, bandDepth),
                    material);
            }

            float eastWidth = halfWidth - holeMaxX;

            if (eastWidth > 0.01f)
            {
                CreateBox(
                    $"{name}_Este",
                    slab.transform,
                    new Vector3((holeMaxX + halfWidth) * 0.5f, 0f, holeCenterZ),
                    new Vector3(eastWidth, thickness, bandDepth),
                    material);
            }
        }

        private void CreateRampBetweenFloors(Transform floor, HoleCorner corner)
        {
            GetHoleLayout(corner, out float holeCenterX, out float holeCenterZ, out float holeSizeValue);

            bool right = corner == HoleCorner.TopRight || corner == HoleCorner.BottomRight;
            float signX = right ? 1f : -1f;
            float angleDegrees = Mathf.Clamp(rampAngle, 15f, 42f);
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            float horizontalRun = floorHeight / Mathf.Tan(angleRadians);
            float slopeLength = floorHeight / Mathf.Sin(angleRadians);

            Vector3 direction = new Vector3(-signX, 0f, 0f);

            // El borde superior va en el borde EXTERIOR del hueco (junto a la pared)
            // para que la rampa pase por debajo del hueco: al caer por el hueco caes
            // sobre la rampa y te deslizas al piso de abajo; subiendo sales por el
            // lado de la pared al nivel de la losa.
            Vector3 topEdge = new Vector3(
                holeCenterX + signX * holeSizeValue * 0.5f,
                slabThickness,
                holeCenterZ);
            Vector3 bottomEdge = topEdge + direction * horizontalRun + Vector3.down * floorHeight;

            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up)
                * Quaternion.Euler(angleDegrees, 0f, 0f);
            Vector3 normal = rotation * Vector3.up;
            Vector3 surfaceMiddle = (topEdge + bottomEdge) * 0.5f;
            Vector3 center = surfaceMiddle - normal * (rampThickness * 0.5f);

            GameObject ramp = CreateRampPlank(
                floor,
                center,
                rotation,
                new Vector3(rampWidth, rampThickness, slopeLength));

            // Bordes y soportes usan el material del propio prefab (Rampa.mat);
            // aqui no se crea ningun material.
            Renderer rampRenderer = ramp.GetComponent<Renderer>();
            Material plankMaterial = rampRenderer != null ? rampRenderer.sharedMaterial : rampMaterial;

            Vector3 railOffset = rotation * Vector3.right;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 railCenter = surfaceMiddle
                    + railOffset * (side * (rampWidth * 0.5f - 0.06f))
                    + normal * (rampRailHeight * 0.5f);

                GameObject rail = CreateBox(
                    $"Rampa_Borde_{(side < 0 ? "Izq" : "Der")}",
                    floor,
                    railCenter,
                    new Vector3(0.12f, rampRailHeight, slopeLength),
                    plankMaterial);
                rail.transform.localRotation = rotation;
            }

            float lowerFloorY = slabThickness - floorHeight;

            // Con el hueco casi tan largo como el recorrido horizontal, la rampa
            // apoya en ambos bordes del hueco; los postes bajan por la boca hasta
            // la losa del piso de abajo, que en esta esquina siempre es solida
            // (el hueco rota de esquina en cada piso).
            float verticalThickness = rampThickness * Mathf.Cos(angleRadians);
            float[] supportTs = { 0.45f, 0.8f };

            for (int i = 0; i < supportTs.Length; i++)
            {
                float t = supportTs[i];
                Vector3 point = topEdge + direction * (horizontalRun * t) + Vector3.down * (floorHeight * t);
                float supportHeight = point.y - verticalThickness - lowerFloorY;

                if (supportHeight <= 0.05f)
                {
                    continue;
                }

                GameObject support = CreateBox(
                    "Rampa_Soporte",
                    floor,
                    new Vector3(point.x, lowerFloorY + supportHeight * 0.5f, point.z),
                    new Vector3(0.24f, supportHeight, 0.24f),
                    plankMaterial);

                Collider collider = support.GetComponent<Collider>();

                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }

        private GameObject CreateRampPlank(Transform floor, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            GameObject ramp;

            if (rampPrefab == null)
            {
                ramp = CreateBox("Rampa_Piso", floor, localPosition, localScale, rampMaterial);
                ramp.transform.localRotation = localRotation;
                return ramp;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ramp = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(rampPrefab, floor);
            }
            else
            {
                ramp = Instantiate(rampPrefab, floor);
            }
#else
            ramp = Instantiate(rampPrefab, floor);
#endif

            ramp.name = "Rampa_Piso";
            ramp.transform.localPosition = localPosition;
            ramp.transform.localRotation = localRotation;
            ramp.transform.localScale = localScale;
            return ramp;
        }

        private void CreateSafeColumns(Transform floor)
        {
            float height = floorHeight - 2f * slabThickness;
            float centerY = floorHeight * 0.5f;
            float diameter = columnRadius * 2f;

            Vector3[] positions =
            {
                new Vector3(0f, centerY, columnOffsetZ),
                new Vector3(0f, centerY, -columnOffsetZ),
                new Vector3(columnOffsetX, centerY, 0f),
                new Vector3(-columnOffsetX, centerY, 0f)
            };

            string[] names = { "Norte", "Sur", "Este", "Oeste" };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject column = InstantiateColumn(floor, names[i]);

                if (column == null)
                {
                    continue;
                }

                column.transform.localPosition = positions[i];
                // El cilindro por defecto mide 2 unidades de alto.
                column.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);

                Renderer renderer = column.GetComponent<Renderer>();

                if (renderer != null && columnMaterial != null)
                {
                    renderer.sharedMaterial = columnMaterial;
                }

                SafeZone zone = column.GetComponent<SafeZone>();

                if (zone == null)
                {
                    zone = column.AddComponent<SafeZone>();
                }

                zone.Configure(safeZoneRadius, floorHeight * 0.5f + 0.5f);
            }
        }

        private GameObject InstantiateColumn(Transform parent, string suffix)
        {
            if (columnPrefab != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    GameObject instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(columnPrefab, parent);
                    instance.name = $"Columna_{suffix}";
                    return instance;
                }
#endif
                GameObject runtimeInstance = Instantiate(columnPrefab, parent);
                runtimeInstance.name = $"Columna_{suffix}";
                return runtimeInstance;
            }

            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            primitive.name = $"Columna_{suffix}";
            primitive.transform.SetParent(parent, false);
            return primitive;
        }

        private void CreateOfficeRooms(Transform floor, int floorIndex)
        {
            float wallHeight = floorHeight - 2f * slabThickness;
            float centerY = floorHeight * 0.5f;
            float halfWidth = floorWidth * 0.5f;
            float halfDepth = floorDepth * 0.5f;
            float margin = 2.2f;
            float door = Mathf.Clamp(officeDoorWidth + 0.8f, 2.8f, 3.6f);

            bool hasHole = floorIndex > 0 && generateFloorHoles;
            HoleCorner holeCorner = hasHole ? GetHoleCorner(floorIndex - 1) : HoleCorner.TopRight;
            float holeX = 0f, holeZ = 0f, holeS = 0f;
            if (hasHole)
            {
                GetHoleLayout(holeCorner, out holeX, out holeZ, out holeS);
            }

            bool HoleIntersects(Vector3 center, Vector3 size)
            {
                if (!hasHole) return false;
                float hx = holeS * 0.5f + 0.6f;
                float hz = holeS * 0.5f + 0.6f;
                return Mathf.Abs(center.x - holeX) < (size.x * 0.5f + hx) &&
                       Mathf.Abs(center.z - holeZ) < (size.z * 0.5f + hz);
            }

            // División central con pasillo amplio: crea 2 zonas grandes tipo oficina.
            Vector3 divCenter = new Vector3(0f, centerY, 0f);
            Vector3 divSize = new Vector3(officeWallThickness, wallHeight, floorDepth - margin * 2f);
            if (!HoleIntersects(divCenter, divSize))
            {
                BuildInteriorWallWithDoor(floor, "Oficina_Division_Central", divCenter, divSize, door, officeDoorHeight, false, 0f);
            }

            // Cuartos amplios a cada lado, dejando pasillo central libre.
            float sideRoomWidth = halfWidth - margin - 1.2f;
            float sideRoomDepth = halfDepth * 0.58f;
            float zOffset = halfDepth - margin - sideRoomDepth * 0.5f;

            TryCreateOfficeRoom(floor, "Oficina_NE", new Vector3(halfWidth * 0.5f, centerY, zOffset),
                new Vector3(sideRoomWidth, wallHeight, officeWallThickness), door, true, 0.8f, HoleIntersects);
            TryCreateOfficeRoom(floor, "Oficina_SE", new Vector3(halfWidth * 0.5f, centerY, -zOffset),
                new Vector3(sideRoomWidth, wallHeight, officeWallThickness), door, true, 0.8f, HoleIntersects);
            TryCreateOfficeRoom(floor, "Oficina_NO", new Vector3(-halfWidth * 0.5f, centerY, zOffset),
                new Vector3(sideRoomWidth, wallHeight, officeWallThickness), door, true, -0.8f, HoleIntersects);
            TryCreateOfficeRoom(floor, "Oficina_SO", new Vector3(-halfWidth * 0.5f, centerY, -zOffset),
                new Vector3(sideRoomWidth, wallHeight, officeWallThickness), door, true, -0.8f, HoleIntersects);
        }

        private void TryCreateOfficeRoom(Transform floor, string name, Vector3 center, Vector3 size, float door, bool doorAlongX, float doorOffset, System.Func<Vector3, Vector3, bool> intersectsHole)
        {
            if (intersectsHole(center, size))
            {
                return;
            }

            // Evita solapar columnas seguras cerca del centro de los bordes.
            if (Mathf.Abs(center.x) < 1.5f || Mathf.Abs(center.z) < 1.5f)
            {
                return;
            }

            BuildInteriorWallWithDoor(floor, name, center, size, door, officeDoorHeight, doorAlongX, doorOffset);
        }

        private void BuildInteriorWallWithDoor(Transform parent, string name, Vector3 center, Vector3 size, float doorWidth, float doorHeight, bool doorAlongX, float doorOffset)
        {
            float wallHeight = size.y;
            float wallBottom = center.y - wallHeight * 0.5f;
            float clampedDoorHeight = Mathf.Clamp(doorHeight, 1.8f, wallHeight - 0.2f);
            float topHeight = wallHeight - clampedDoorHeight;

            float length = doorAlongX ? size.x : size.z;
            float clampedDoorWidth = Mathf.Clamp(doorWidth, 1.6f, length - 1.2f);
            float doorCenter = Mathf.Clamp(doorOffset, -length * 0.5f + clampedDoorWidth * 0.5f + 0.4f, length * 0.5f - clampedDoorWidth * 0.5f - 0.4f);
            float doorMin = doorCenter - clampedDoorWidth * 0.5f;
            float doorMax = doorCenter + clampedDoorWidth * 0.5f;

            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);

            if (topHeight > 0.05f)
            {
                Vector3 topCenter = center + Vector3.up * (clampedDoorHeight * 0.5f + topHeight * 0.5f - wallHeight * 0.5f);
                CreateBox($"{name}_Alto", wall.transform, topCenter, size - new Vector3(0f, wallHeight - topHeight, 0f), wallMaterial);
            }

            float start = -length * 0.5f;
            float end = length * 0.5f;

            CreateInteriorSegment(wall.transform, $"{name}_A", center, size, doorAlongX, start, doorMin, wallBottom, clampedDoorHeight);
            CreateInteriorSegment(wall.transform, $"{name}_B", center, size, doorAlongX, doorMax, end, wallBottom, clampedDoorHeight);
        }

        private void CreateInteriorSegment(Transform parent, string name, Vector3 wallCenter, Vector3 wallSize, bool alongX, float segMin, float segMax, float wallBottom, float segHeight)
        {
            float length = segMax - segMin;
            if (length <= 0.05f)
            {
                return;
            }

            float mid = (segMin + segMax) * 0.5f;
            Vector3 pos = wallCenter;
            Vector3 scale = wallSize;

            if (alongX)
            {
                pos.x = wallCenter.x - wallSize.x * 0.5f + (wallSize.x * 0.5f) + mid;
                scale.x = length;
            }
            else
            {
                pos.z = wallCenter.z - wallSize.z * 0.5f + (wallSize.z * 0.5f) + mid;
                scale.z = length;
            }

            pos.y = wallBottom + segHeight * 0.5f;
            scale.y = segHeight;
            CreateBox(name, parent, pos, scale, wallMaterial);
        }

        private void CreateLevelExit(Transform floor)
        {
            GameObject exit = new GameObject("Salida_Nivel");
            exit.transform.SetParent(floor, false);
            exit.transform.localPosition = new Vector3(0f, 0.5f, -floorDepth * 0.5f + 2f);

            BoxCollider trigger = exit.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.5f, 2f, 2f);

            LevelExit levelExit = exit.AddComponent<LevelExit>();
            levelExit.Configure(finalSceneName);

            GameObject marker = CreateBox(
                "Marcador_Salida",
                exit.transform,
                new Vector3(0f, -0.23f, 0f),
                new Vector3(3.5f, 0.05f, 2f),
                exitMaterial != null ? exitMaterial : floorMaterial);

            Collider markerCollider = marker.GetComponent<Collider>();

            if (markerCollider != null)
            {
                markerCollider.enabled = false;
            }
        }

        private void BuildWall(Transform floor, WallSide side)
        {
            GameObject wall = new GameObject($"Pared_{side}");
            wall.transform.SetParent(floor, false);

            bool alongX = side == WallSide.North || side == WallSide.South;
            float length = alongX ? floorWidth : floorDepth;
            float wallHeight = floorHeight - 2f * slabThickness;
            float centerY = floorHeight * 0.5f;

            if (generateWindows && windowsPerWall > 0 && Random.value < fullGlassWallChance)
            {
                BuildFullGlassWall(wall.transform, side, length, wallHeight);
                return;
            }

            if (!generateWindows || windowsPerWall <= 0)
            {
                CreateBox(
                    "Muro_Completo",
                    wall.transform,
                    GetWallPosition(side, 0f, centerY),
                    GetWallScale(side, length, wallHeight),
                    wallMaterial);

                return;
            }

            int count = Mathf.Clamp(windowsPerWall, 1, 12);
            float maxWindowHeight = wallHeight - windowSillHeight - 0.3f;
            float usableWindowHeight = Mathf.Min(windowHeight, maxWindowHeight);

            if (usableWindowHeight < 0.5f)
            {
                CreateBox(
                    "Muro_Completo",
                    wall.transform,
                    GetWallPosition(side, 0f, centerY),
                    GetWallScale(side, length, wallHeight),
                    wallMaterial);

                return;
            }

            float gap = (length - count * windowWidth) / (count + 1);
            float usableWindowWidth = windowWidth;
            float minimumGap = 0.25f;

            if (gap < minimumGap)
            {
                usableWindowWidth = Mathf.Max(0.4f, (length - minimumGap * (count + 1)) / count);
                gap = (length - count * usableWindowWidth) / (count + 1);
            }

            if (usableWindowWidth < 0.4f)
            {
                CreateBox(
                    "Muro_Completo",
                    wall.transform,
                    GetWallPosition(side, 0f, centerY),
                    GetWallScale(side, length, wallHeight),
                    wallMaterial);

                return;
            }

            float bottomHeight = windowSillHeight;
            float topHeight = wallHeight - windowSillHeight - usableWindowHeight;
            float windowCenterY = slabThickness + windowSillHeight + usableWindowHeight * 0.5f;

            CreateBox(
                "Muro_Bajo",
                wall.transform,
                GetWallPosition(side, 0f, slabThickness + bottomHeight * 0.5f),
                GetWallScale(side, length, bottomHeight),
                wallMaterial);

            if (topHeight > 0.01f)
            {
                CreateBox(
                    "Muro_Alto",
                    wall.transform,
                    GetWallPosition(side, 0f, slabThickness + windowSillHeight + usableWindowHeight + topHeight * 0.5f),
                    GetWallScale(side, length, topHeight),
                    wallMaterial);
            }

            float start = -length * 0.5f;
            float firstCenter = start + gap + usableWindowWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float windowCenter = firstCenter + i * (usableWindowWidth + gap);
                float windowLeft = windowCenter - usableWindowWidth * 0.5f;
                float windowRight = windowCenter + usableWindowWidth * 0.5f;

                if (windowLeft - start > 0.01f)
                {
                    float pillarWidth = windowLeft - start;
                    CreateBox(
                        $"Pilar_{side}_{i:00}",
                        wall.transform,
                        GetWallPosition(side, (start + windowLeft) * 0.5f, windowCenterY),
                        GetWallScale(side, pillarWidth, usableWindowHeight),
                        wallMaterial);
                }

                CreateWindow(wall.transform, side, windowCenter, windowCenterY, usableWindowWidth, usableWindowHeight, i);

                start = windowRight;
            }

            if (length * 0.5f - start > 0.01f)
            {
                float pillarWidth = length * 0.5f - start;
                CreateBox(
                    $"Pilar_{side}_Fin",
                    wall.transform,
                    GetWallPosition(side, (start + length * 0.5f) * 0.5f, windowCenterY),
                    GetWallScale(side, pillarWidth, usableWindowHeight),
                    wallMaterial);
            }
        }

        private void BuildFullGlassWall(Transform wall, WallSide side, float length, float wallHeight)
        {
            float sideMargin = 0.35f;
            float usableWidth = Mathf.Max(1f, length - sideMargin * 2f);
            int panes = Mathf.Clamp(windowsPerWall, 1, 8);
            float mullion = 0.08f;
            float paneWidth = (usableWidth - mullion * (panes - 1)) / panes;

            if (paneWidth < 0.3f)
            {
                panes = 1;
                paneWidth = usableWidth;
            }

            float glassBottom = slabThickness + 0.12f;
            float glassTop = wallHeight - 0.12f;
            float glassHeight = Mathf.Max(0.5f, glassTop - glassBottom);

            CreateBox(
                "Ventanal_Marco_Bajo",
                wall,
                GetWallPosition(side, 0f, slabThickness + 0.06f),
                GetWallScale(side, length, 0.12f),
                wallMaterial);

            CreateBox(
                "Ventanal_Marco_Alto",
                wall,
                GetWallPosition(side, 0f, wallHeight - 0.06f),
                GetWallScale(side, length, 0.12f),
                wallMaterial);

            float start = -usableWidth * 0.5f;

            for (int i = 0; i < panes; i++)
            {
                float paneCenter = start + paneWidth * 0.5f;
                CreateWindow(wall, side, paneCenter, glassBottom + glassHeight * 0.5f, paneWidth, glassHeight, i);
                start += paneWidth;

                if (i < panes - 1)
                {
                    CreateBox(
                        $"Ventanal_Montante_{i:00}",
                        wall,
                        GetWallPosition(side, start + mullion * 0.5f, glassBottom + glassHeight * 0.5f),
                        GetWallScale(side, mullion, glassHeight),
                        wallMaterial);

                    start += mullion;
                }
            }
        }

        private void CreateWindow(Transform wall, WallSide side, float along, float centerY, float width, float height, int index)
        {
            bool alongX = side == WallSide.North || side == WallSide.South;
            Vector3 scale = alongX
                ? new Vector3(width, height, 0.04f)
                : new Vector3(0.04f, height, width);

            GameObject window = CreateBox(
                $"Ventana_{side}_{index + 1:00}",
                wall,
                GetWallPosition(side, along, centerY),
                scale,
                glassMaterial);

            Renderer renderer = window.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            BreakableWindow breakable = window.AddComponent<BreakableWindow>();
            breakable.Configure(shardMaterial, windowBreakRadius, windowBreakChance, windowShardCount);
        }

        private Vector3 GetWallPosition(WallSide side, float along, float y)
        {
            switch (side)
            {
                case WallSide.North:
                    return new Vector3(along, y, floorDepth * 0.5f - wallThickness * 0.5f);
                case WallSide.South:
                    return new Vector3(along, y, -floorDepth * 0.5f + wallThickness * 0.5f);
                case WallSide.East:
                    return new Vector3(floorWidth * 0.5f - wallThickness * 0.5f, y, along);
                default:
                    return new Vector3(-floorWidth * 0.5f + wallThickness * 0.5f, y, along);
            }
        }

        private Vector3 GetWallScale(WallSide side, float length, float height)
        {
            bool alongX = side == WallSide.North || side == WallSide.South;
            return alongX
                ? new Vector3(length, height, wallThickness)
                : new Vector3(wallThickness, height, length);
        }

        private GameObject CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = Quaternion.identity;
            box.transform.localScale = localScale;

            Renderer renderer = box.GetComponent<Renderer>();

            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return box;
        }

        private void MarkSceneDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }
    }
}
