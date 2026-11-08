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
        private enum WallSide
        {
            North,
            South,
            East,
            West
        }

        [Header("Dimensiones")]
        [SerializeField] private int floorCount = 30;
        [SerializeField] private float floorHeight = 3.2f;
        [SerializeField] private float floorWidth = 14f;
        [SerializeField] private float floorDepth = 12f;
        [SerializeField] private float slabThickness = 0.25f;
        [SerializeField] private float wallThickness = 0.2f;

        [Header("Ventanas")]
        [SerializeField] private bool generateWindows = true;
        [SerializeField] private int windowsPerWall = 2;
        [SerializeField] private float windowWidth = 2.4f;
        [SerializeField] private float windowHeight = 1.7f;
        [SerializeField] private float windowSillHeight = 0.9f;
        [SerializeField] private float windowBreakRadius = 5f;
        [SerializeField] private float windowBreakChance = 0.35f;
        [SerializeField] private int windowShardCount = 10;

        [Header("Eventos por cuarto")]
        [SerializeField] private int debrisCountPerRoom = 10;
        [SerializeField] private bool debrisRequirePlayerNear = true;
        [SerializeField] private float roomActivationRadius = 12f;

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

        public void SetMaterials(Material wall, Material floor, Material ceiling, Material glass, Material shards, Material debris)
        {
            wallMaterial = wall;
            floorMaterial = floor;
            ceilingMaterial = ceiling;
            glassMaterial = glass;
            shardMaterial = shards;
            debrisMaterial = debris;
        }

        public void SetRoomDefaults(int debrisCount, bool requirePlayerNear, float activationRadius)
        {
            debrisCountPerRoom = Mathf.Max(0, debrisCount);
            debrisRequirePlayerNear = requirePlayerNear;
            roomActivationRadius = Mathf.Max(0f, activationRadius);
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

            if (Application.isPlaying)
            {
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
        }

        private void BuildFloor(Transform buildingRoot, int index)
        {
            GameObject floor = new GameObject($"Piso_{index + 1:00}");
            floor.transform.SetParent(buildingRoot, false);
            floor.transform.localPosition = new Vector3(0f, index * floorHeight, 0f);

            RoomEarthquakeSettings room = floor.AddComponent<RoomEarthquakeSettings>();
            room.Configure(debrisCountPerRoom, debrisRequirePlayerNear, debrisMaterial);
            room.ConfigureActivationRadius(roomActivationRadius);

            CreateBox(
                "Losa_Piso",
                floor.transform,
                new Vector3(0f, slabThickness * 0.5f, 0f),
                new Vector3(floorWidth, slabThickness, floorDepth),
                floorMaterial);

            if (index < floorCount - 1 || generateRoof)
            {
                CreateBox(
                    "Losa_Techo",
                    floor.transform,
                    new Vector3(0f, floorHeight - slabThickness * 0.5f, 0f),
                    new Vector3(floorWidth, slabThickness, floorDepth),
                    ceilingMaterial);
            }

            BuildWall(floor.transform, WallSide.North);
            BuildWall(floor.transform, WallSide.South);
            BuildWall(floor.transform, WallSide.East);
            BuildWall(floor.transform, WallSide.West);
        }

        private void BuildWall(Transform floor, WallSide side)
        {
            GameObject wall = new GameObject($"Pared_{side}");
            wall.transform.SetParent(floor, false);

            bool alongX = side == WallSide.North || side == WallSide.South;
            float length = alongX ? floorWidth : floorDepth;
            float wallHeight = floorHeight - 2f * slabThickness;
            float centerY = floorHeight * 0.5f;

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
