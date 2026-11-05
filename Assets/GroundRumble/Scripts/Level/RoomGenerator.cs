using GroundRumble.Events;
using UnityEngine;

namespace GroundRumble.Level
{
    public class RoomGenerator : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject ceilingPrefab;
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject columnPrefab;

        [Header("Building")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private int floors = 30;

        [Header("Floor size (in units)")]
        [SerializeField] private float width = 20f;
        [SerializeField] private float depth = 20f;
        [SerializeField] private float height = 6f;
        [SerializeField] private float slabThickness = 0.5f;
        [SerializeField] private float wallThickness = 0.3f;

        [Header("Stairs (ramps)")]
        [SerializeField] private bool generateStairs = true;
        [SerializeField] private float stairHoleLength = 5f;
        [SerializeField] private float stairHoleWidth = 3.5f;
        [SerializeField] private float stairHoleInset = 4f;
        [SerializeField] private float rampRun = 10f;
        [SerializeField] private float rampWidth = 3f;

        [Header("Rooms (partitions)")]
        [SerializeField] private bool generateRooms = true;
        [SerializeField] private float partitionInset = 6.5f;
        [SerializeField] private float partitionLength = 3f;

        [Header("Windows")]
        [SerializeField] private int windowsPerWall = 2;
        [SerializeField] private float windowWidth = 3f;
        [SerializeField] private float windowHeight = 2.5f;
        [SerializeField] private float windowSillHeight = 1.5f;
        [SerializeField] private Color glassColor = new Color(0.55f, 0.75f, 0.9f, 0.18f);
        [SerializeField, Range(0f, 1f)] private float windowBreakChance = 0.75f;
        [SerializeField] private float windowBreakRadius = 16f;
        [SerializeField] private int windowShardCount = 6;
        [SerializeField] private float windowShardLifetime = 4f;
        [SerializeField] private float windowDamage = 12f;
        [SerializeField] private float windowDamageRadius = 3f;

        [Header("Interior Light")]
        [SerializeField] private bool generateLights = true;
        [SerializeField] private float floorLightIntensity = 1.1f;
        [SerializeField] private float floorLightRange = 17f;
        [SerializeField] private Color floorLightColor = new Color(1f, 0.95f, 0.85f);

        [Header("Player")]
        [SerializeField] private bool movePlayerToTop = true;

        private Material glassMaterial;

        public int FloorCount => floors;
        public float FloorHeight => height;
        public float BuildingWidth => width;
        public float BuildingDepth => depth;
        public Vector3 BuildingCenter => transform.position;

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateBuilding();
            }
        }

        [ContextMenu("Generate Building")]
        public void GenerateBuilding()
        {
            ClearRoom();
            glassMaterial = CreateGlassMaterial();

            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;

            for (int floor = 0; floor < floors; floor++)
            {
                float baseY = floor * height;

                CreateFloorSlabs(floor, baseY, halfW, halfD);
                CreateWalls(baseY, halfW, halfD);
                CreateColumns(baseY, halfW, halfD);

                if (generateRooms)
                {
                    CreatePartitions(baseY, halfW, halfD);
                }

                if (generateStairs && floor < floors - 1)
                {
                    CreateRamp(floor, baseY);
                }

                if (generateLights)
                {
                    CreateFloorLight(baseY);
                }
            }

            CreateRoof();

            if (movePlayerToTop)
            {
                MovePlayerToTop();
            }
        }

        [ContextMenu("Clear Room")]
        public void ClearRoom()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        public void GetCeilingFor(Vector3 worldPosition, out Vector3 center, out Vector3 size)
        {
            float localY = worldPosition.y - transform.position.y;
            int floor = Mathf.Clamp(Mathf.FloorToInt(localY / Mathf.Max(0.01f, height)), 0, floors - 1);
            float ceilingY = transform.position.y + (floor + 1) * height - slabThickness * 0.5f;
            center = new Vector3(transform.position.x, ceilingY, transform.position.z);
            size = new Vector3(width, height, depth);
        }

        private Vector2 GetHoleCenter(int floor)
        {
            int index = ((floor % 4) + 4) % 4;
            switch (index)
            {
                case 0:
                    return new Vector2(stairHoleInset, stairHoleInset);
                case 1:
                    return new Vector2(-stairHoleInset, stairHoleInset);
                case 2:
                    return new Vector2(-stairHoleInset, -stairHoleInset);
                default:
                    return new Vector2(stairHoleInset, -stairHoleInset);
            }
        }

        private void CreateFloorSlabs(int floor, float baseY, float halfW, float halfD)
        {
            if (floorPrefab == null)
            {
                return;
            }

            if (!generateStairs || floor == 0)
            {
                CreateSlab(0f, 0f, width, depth, baseY);
                return;
            }

            Vector2 hole = GetHoleCenter(floor);
            float hx0 = hole.x - stairHoleLength * 0.5f;
            float hx1 = hole.x + stairHoleLength * 0.5f;
            float hz0 = hole.y - stairHoleWidth * 0.5f;
            float hz1 = hole.y + stairHoleWidth * 0.5f;

            if (hx0 > -halfW)
            {
                CreateSlab((-halfW + hx0) * 0.5f, 0f, hx0 + halfW, depth, baseY);
            }

            if (hx1 < halfW)
            {
                CreateSlab((hx1 + halfW) * 0.5f, 0f, halfW - hx1, depth, baseY);
            }

            if (hz0 > -halfD)
            {
                CreateSlab(hole.x, (-halfD + hz0) * 0.5f, stairHoleLength, hz0 + halfD, baseY);
            }

            if (hz1 < halfD)
            {
                CreateSlab(hole.x, (hz1 + halfD) * 0.5f, stairHoleLength, halfD - hz1, baseY);
            }
        }

        private void CreateSlab(float centerX, float centerZ, float sizeX, float sizeZ, float baseY)
        {
            GameObject slab = Instantiate(floorPrefab, transform);
            slab.name = "Losa";
            slab.transform.localRotation = Quaternion.identity;
            slab.transform.localPosition = new Vector3(centerX, baseY - slabThickness * 0.5f, centerZ);
            slab.transform.localScale = new Vector3(sizeX, slabThickness, sizeZ);
        }

        private void CreateWalls(float baseY, float halfW, float halfD)
        {
            if (wallPrefab == null)
            {
                return;
            }

            CreateWallAlongX(1f, baseY, halfW, halfD);
            CreateWallAlongX(-1f, baseY, halfW, halfD);
            CreateWallAlongZ(1f, baseY, halfW, halfD);
            CreateWallAlongZ(-1f, baseY, halfW, halfD);
        }

        private void CreateWallAlongX(float sign, float baseY, float halfW, float halfD)
        {
            float planeZ = sign * halfD;
            Vector3 inward = Vector3.forward * -sign;
            float start = -halfW;
            float end = halfW;
            float centerY = baseY + height * 0.5f;
            float sill = windowSillHeight;
            float topHeight = height - (sill + windowHeight);

            float[] centers = GetWindowCenters(halfW - 2f);
            float cursor = start;

            for (int i = 0; i < centers.Length; i++)
            {
                float windowStart = centers[i] - windowWidth * 0.5f;
                float windowEnd = centers[i] + windowWidth * 0.5f;
                windowStart = Mathf.Max(windowStart, start);
                windowEnd = Mathf.Min(windowEnd, end);

                if (windowStart > cursor)
                {
                    CreateWallPiece(
                        new Vector3((cursor + windowStart) * 0.5f, centerY, planeZ),
                        new Vector3(windowStart - cursor, height, wallThickness));
                }

                CreateWallPiece(
                    new Vector3(centers[i], baseY + sill * 0.5f, planeZ),
                    new Vector3(windowWidth, sill, wallThickness));

                if (topHeight > 0f)
                {
                    CreateWallPiece(
                        new Vector3(centers[i], baseY + sill + windowHeight + topHeight * 0.5f, planeZ),
                        new Vector3(windowWidth, topHeight, wallThickness));
                }

                CreateWindow(
                    new Vector3(centers[i], baseY + sill + windowHeight * 0.5f, planeZ),
                    new Vector3(windowWidth, windowHeight, 0.04f),
                    inward);

                cursor = Mathf.Max(cursor, windowEnd);
            }

            if (cursor < end)
            {
                CreateWallPiece(
                    new Vector3((cursor + end) * 0.5f, centerY, planeZ),
                    new Vector3(end - cursor, height, wallThickness));
            }
        }

        private void CreateWallAlongZ(float sign, float baseY, float halfW, float halfD)
        {
            float planeX = sign * halfW;
            Vector3 inward = Vector3.right * -sign;
            float start = -halfD;
            float end = halfD;
            float centerY = baseY + height * 0.5f;
            float sill = windowSillHeight;
            float topHeight = height - (sill + windowHeight);

            float[] centers = GetWindowCenters(halfD - 2f);
            float cursor = start;

            for (int i = 0; i < centers.Length; i++)
            {
                float windowStart = centers[i] - windowWidth * 0.5f;
                float windowEnd = centers[i] + windowWidth * 0.5f;
                windowStart = Mathf.Max(windowStart, start);
                windowEnd = Mathf.Min(windowEnd, end);

                if (windowStart > cursor)
                {
                    CreateWallPiece(
                        new Vector3(planeX, centerY, (cursor + windowStart) * 0.5f),
                        new Vector3(wallThickness, height, windowStart - cursor));
                }

                CreateWallPiece(
                    new Vector3(planeX, baseY + sill * 0.5f, centers[i]),
                    new Vector3(wallThickness, sill, windowWidth));

                if (topHeight > 0f)
                {
                    CreateWallPiece(
                        new Vector3(planeX, baseY + sill + windowHeight + topHeight * 0.5f, centers[i]),
                        new Vector3(wallThickness, topHeight, windowWidth));
                }

                CreateWindow(
                    new Vector3(planeX, baseY + sill + windowHeight * 0.5f, centers[i]),
                    new Vector3(0.04f, windowHeight, windowWidth),
                    inward);

                cursor = Mathf.Max(cursor, windowEnd);
            }

            if (cursor < end)
            {
                CreateWallPiece(
                    new Vector3(planeX, centerY, (cursor + end) * 0.5f),
                    new Vector3(wallThickness, height, end - cursor));
            }
        }

        private float[] GetWindowCenters(float usableHalf)
        {
            if (windowsPerWall <= 0)
            {
                return new float[0];
            }

            float[] centers = new float[windowsPerWall];
            if (windowsPerWall == 1)
            {
                centers[0] = 0f;
                return centers;
            }

            for (int i = 0; i < windowsPerWall; i++)
            {
                float t = (i + 0.5f) / windowsPerWall;
                centers[i] = Mathf.Lerp(-usableHalf, usableHalf, t);
            }

            return centers;
        }

        private void CreateWallPiece(Vector3 localPosition, Vector3 localScale)
        {
            GameObject piece = Instantiate(wallPrefab, transform);
            piece.name = "Pared";
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
        }

        private void CreateWindow(Vector3 localPosition, Vector3 localScale, Vector3 inward)
        {
            GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
            window.name = "Ventana";
            window.transform.SetParent(transform, false);
            window.transform.localPosition = localPosition;
            window.transform.localRotation = Quaternion.identity;
            window.transform.localScale = localScale;

            Renderer renderer = window.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = glassMaterial;
            }

            GlassWindow glass = window.AddComponent<GlassWindow>();
            glass.Configure(
                inward,
                windowBreakChance,
                windowBreakRadius,
                windowShardCount,
                windowShardLifetime,
                windowDamage,
                windowDamageRadius);
        }

        private void CreateColumns(float baseY, float halfW, float halfD)
        {
            if (columnPrefab == null)
            {
                return;
            }

            const float columnSize = 0.8f;
            Vector3[] corners =
            {
                new Vector3(-halfW, 0f, -halfD),
                new Vector3(halfW, 0f, -halfD),
                new Vector3(-halfW, 0f, halfD),
                new Vector3(halfW, 0f, halfD)
            };

            foreach (Vector3 corner in corners)
            {
                GameObject column = Instantiate(columnPrefab, transform);
                column.name = "Columna (Zona Segura)";
                column.transform.localPosition = corner + Vector3.up * (baseY + height * 0.5f);
                column.transform.localRotation = Quaternion.identity;
                column.transform.localScale = new Vector3(columnSize, height, columnSize);
            }
        }

        private void CreatePartitions(float baseY, float halfW, float halfD)
        {
            float centerY = baseY + height * 0.5f;

            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    float zCenter = sz * (halfD - partitionLength * 0.5f);
                    CreateWallPiece(
                        new Vector3(sx * partitionInset, centerY, zCenter),
                        new Vector3(wallThickness, height, partitionLength));

                    float xCenter = sx * (halfW - partitionLength * 0.5f);
                    CreateWallPiece(
                        new Vector3(xCenter, centerY, sz * partitionInset),
                        new Vector3(partitionLength, height, wallThickness));
                }
            }
        }

        private void CreateRamp(int lowerFloor, float baseY)
        {
            if (floorPrefab == null)
            {
                return;
            }

            Vector2 hole = GetHoleCenter(lowerFloor + 1);
            float sign = hole.x >= 0f ? 1f : -1f;
            float topY = baseY + height;

            Vector3 upper = new Vector3(hole.x + sign * stairHoleLength * 0.5f, topY, hole.y);
            Vector3 lower = new Vector3(upper.x - sign * rampRun, baseY, hole.y);
            Vector3 direction = lower - upper;
            float length = direction.magnitude;

            GameObject ramp = Instantiate(floorPrefab, transform);
            ramp.name = "Rampa";
            ramp.transform.localPosition = (upper + lower) * 0.5f + Vector3.up * 0.1f;
            ramp.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            ramp.transform.localScale = new Vector3(rampWidth, slabThickness, length);
        }

        private void CreateFloorLight(float baseY)
        {
            GameObject lightObject = new GameObject("LuzPiso");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, baseY + height * 0.8f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = floorLightRange;
            light.intensity = floorLightIntensity;
            light.color = floorLightColor;
            light.shadows = LightShadows.None;
        }

        private void CreateRoof()
        {
            GameObject prefab = ceilingPrefab != null ? ceilingPrefab : floorPrefab;
            if (prefab == null)
            {
                return;
            }

            GameObject roof = Instantiate(prefab, transform);
            roof.name = "Techo";
            roof.transform.localRotation = Quaternion.identity;
            roof.transform.localPosition = new Vector3(0f, floors * height + slabThickness * 0.5f, 0f);
            roof.transform.localScale = new Vector3(width, slabThickness, depth);
        }

        private void MovePlayerToTop()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                return;
            }

            Vector3 local = new Vector3(0f, (floors - 1) * height + 1.2f, 0f);
            Vector3 world = transform.TransformPoint(local);

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = world;
            }

            player.transform.position = world;
        }

        private Material CreateGlassMaterial()
        {
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader);
            material.name = "Glass (Runtime)";
            material.color = glassColor;
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.12f, 0.18f, 0.24f));
            material.renderQueue = 3000;
            return material;
        }
    }
}
