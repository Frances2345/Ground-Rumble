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

        [Header("Room size (in units)")]
        [SerializeField] private float width = 10f;
        [SerializeField] private float depth = 10f;
        [SerializeField] private float height = 3f;

        [ContextMenu("Generate Room")]
        public void GenerateRoom()
        {
            ClearRoom();

            // Floor
            if (floorPrefab != null)
            {
                GameObject floor = Instantiate(floorPrefab, transform);
                floor.transform.localPosition = Vector3.zero;
                floor.transform.localScale = new Vector3(width, 1f, depth);
            }

            // Ceiling
            if (ceilingPrefab != null)
            {
                GameObject ceiling = Instantiate(ceilingPrefab, transform);
                ceiling.transform.localPosition = new Vector3(0f, height, 0f);
                ceiling.transform.localScale = new Vector3(width, 1f, depth);
            }

            // Walls (4 sides)
            if (wallPrefab != null)
            {
                // Front / Back
                for (int i = 0; i < 2; i++)
                {
                    float z = (i == 0) ? depth * 0.5f : -depth * 0.5f;
                    float rotY = (i == 0) ? 0f : 180f;
                    GameObject wall = Instantiate(wallPrefab, transform);
                    wall.transform.localPosition = new Vector3(0f, height * 0.5f, z);
                    wall.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
                    wall.transform.localScale = new Vector3(width, height, 1f);
                }

                // Left / Right
                for (int i = 0; i < 2; i++)
                {
                    float x = (i == 0) ? width * 0.5f : -width * 0.5f;
                    float rotY = (i == 0) ? 90f : -90f;
                    GameObject wall = Instantiate(wallPrefab, transform);
                    wall.transform.localPosition = new Vector3(x, height * 0.5f, 0f);
                    wall.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
                    wall.transform.localScale = new Vector3(depth, height, 1f);
                }
            }

            // Columns at corners and along edges
            if (columnPrefab != null)
            {
                Vector3[] corners =
                {
                    new Vector3(-width * 0.5f, 0f, -depth * 0.5f),
                    new Vector3(width * 0.5f, 0f, -depth * 0.5f),
                    new Vector3(-width * 0.5f, 0f, depth * 0.5f),
                    new Vector3(width * 0.5f, 0f, depth * 0.5f)
                };

                foreach (Vector3 corner in corners)
                {
                    GameObject col = Instantiate(columnPrefab, transform);
                    col.transform.localPosition = corner + Vector3.up * (height * 0.5f);
                    col.transform.localScale = new Vector3(1f, height, 1f);
                }
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
    }
}
