using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GroundRumble.EditorTools
{
    /// <summary>
    /// Configura la escena con jugador, managers, HUD y edificio generado.
    /// </summary>
    public static class GroundRumbleSetup
    {
        private const string MaterialsFolder = "Assets/GroundRumble/Materials";

        private struct MaterialSet
        {
            public Material Wall;
            public Material Floor;
            public Material Ceiling;
            public Material Glass;
            public Material Shards;
            public Material Debris;
        }

        [MenuItem("Ground Rumble/Configurar escena completa", false, 1)]
        public static void SetupSceneMenu()
        {
            SetupScene();
        }

        [MenuItem("Ground Rumble/Generar edificio de 30 pisos", false, 2)]
        public static void GenerateBuildingMenu()
        {
            MaterialSet materials = EnsureMaterials();
            BuildingGenerator generator = GetOrCreateGenerator();
            ApplyMaterials(generator, materials);
            generator.Generate();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Ground Rumble/Limpiar edificio generado", false, 3)]
        public static void ClearBuildingMenu()
        {
            BuildingGenerator generator = UnityEngine.Object.FindAnyObjectByType<BuildingGenerator>();

            if (generator != null)
            {
                generator.ClearGenerated();
            }
        }

        [MenuItem("Ground Rumble/Crear o actualizar materiales", false, 4)]
        public static void CreateMaterialsMenu()
        {
            EnsureMaterials();
            AssetDatabase.SaveAssets();
        }

        public static void SetupScene()
        {
            MaterialSet materials = EnsureMaterials();
            MoveSampleGroundDown();
            SetupPlayer();
            SetupManagers(materials);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        public static void SetupSceneBatch()
        {
            SetupScene();

            Scene scene = SceneManager.GetActiveScene();

            if (!string.IsNullOrEmpty(scene.path))
            {
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
        }

        private static MaterialSet EnsureMaterials()
        {
            EnsureMaterialsFolder();

            return new MaterialSet
            {
                Wall = GetOrCreateMaterial("Muro", () => MaterialFactory.CreateLitMaterial(new Color(0.72f, 0.72f, 0.75f, 1f), 0.15f)),
                Floor = GetOrCreateMaterial("Piso", () => MaterialFactory.CreateLitMaterial(new Color(0.55f, 0.55f, 0.58f, 1f), 0.2f)),
                Ceiling = GetOrCreateMaterial("Techo", () => MaterialFactory.CreateLitMaterial(new Color(0.63f, 0.63f, 0.66f, 1f), 0.1f)),
                Glass = GetOrCreateMaterial("Vidrio", MaterialFactory.CreateGlassMaterial),
                Shards = GetOrCreateMaterial("Anico_Vidrio", MaterialFactory.CreateGlassShardMaterial),
                Debris = GetOrCreateMaterial("Escombro", () => MaterialFactory.CreateLitMaterial(new Color(0.42f, 0.4f, 0.37f, 1f), 0.05f))
            };
        }

        private static void EnsureMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/GroundRumble"))
            {
                AssetDatabase.CreateFolder("Assets", "GroundRumble");
            }

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/GroundRumble", "Materials");
            }
        }

        private static Material GetOrCreateMaterial(string fileName, Func<Material> factory)
        {
            string path = $"{MaterialsFolder}/{fileName}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing != null)
            {
                return existing;
            }

            Material material = factory();
            material.name = fileName;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void MoveSampleGroundDown()
        {
            GameObject ground = GameObject.Find("Cube");

            if (ground == null || ground.GetComponent<MeshRenderer>() == null)
            {
                return;
            }

            float halfHeight = Mathf.Abs(ground.transform.lossyScale.y) * 0.5f;
            Vector3 position = ground.transform.position;
            ground.transform.position = new Vector3(position.x, -halfHeight, position.z);
            ground.name = "Suelo";
        }

        private static void SetupPlayer()
        {
            PlayerController controller = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            GameObject player;

            if (controller == null)
            {
                player = new GameObject("Jugador");
                player.transform.position = new Vector3(0f, 0.3f, 0f);
                player.tag = "Player";

                CharacterController characterController = player.AddComponent<CharacterController>();
                characterController.height = 2f;
                characterController.radius = 0.35f;
                characterController.center = new Vector3(0f, 1f, 0f);
                characterController.slopeLimit = 45f;
                characterController.stepOffset = 0.3f;
                characterController.skinWidth = 0.02f;
                characterController.minMoveDistance = 0f;

                player.AddComponent<PlayerStamina>();
                player.AddComponent<PlayerHealth>();
                controller = player.AddComponent<PlayerController>();
                player.AddComponent<MouseLook>();
            }
            else
            {
                player = controller.gameObject;
            }

            EnsureComponent<PlayerStamina>(player);
            EnsureComponent<PlayerHealth>(player);
            EnsureComponent<CharacterController>(player);

            Transform pivot = player.transform.Find("PivoteCamara");

            if (pivot == null)
            {
                GameObject pivotObject = new GameObject("PivoteCamara");
                pivot = pivotObject.transform;
                pivot.SetParent(player.transform, false);
                pivot.localPosition = new Vector3(0f, 1.65f, 0f);
                pivot.localRotation = Quaternion.identity;
            }

            Camera camera = player.GetComponentInChildren<Camera>(true);

            if (camera == null)
            {
                camera = Camera.main;
            }

            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            Transform cameraTransform = camera.transform;
            cameraTransform.SetParent(pivot, false);
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;

            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 300f;
            camera.fieldOfView = 70f;

            EnsureComponent<AudioListener>(camera.gameObject);
            EnsureComponent<CameraShake>(camera.gameObject);

            MouseLook look = EnsureComponent<MouseLook>(player);
            look.Configure(pivot);

            controller.Configure(cameraTransform, pivot);

            DisableExtraAudioListeners(camera.gameObject);
        }

        private static void DisableExtraAudioListeners(GameObject cameraObject)
        {
            AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);

            foreach (AudioListener listener in listeners)
            {
                if (listener.gameObject != cameraObject)
                {
                    listener.enabled = false;
                }
            }
        }

        private static void SetupManagers(MaterialSet materials)
        {
            if (UnityEngine.Object.FindAnyObjectByType<EarthquakeManager>() == null)
            {
                GameObject managerObject = new GameObject("EarthquakeManager");
                managerObject.AddComponent<EarthquakeManager>();
            }

            if (UnityEngine.Object.FindAnyObjectByType<EarthquakeHUD>() == null)
            {
                GameObject hudObject = new GameObject("EarthquakeHUD");
                hudObject.AddComponent<EarthquakeHUD>();
            }

            BuildingGenerator generator = GetOrCreateGenerator();
            ApplyMaterials(generator, materials);
            generator.SetRoomDefaults(10, true, 12f);

            if (generator.transform.Find("Edificio_Generado") == null)
            {
                generator.Generate();
            }
        }

        private static BuildingGenerator GetOrCreateGenerator()
        {
            BuildingGenerator generator = UnityEngine.Object.FindAnyObjectByType<BuildingGenerator>();

            if (generator != null)
            {
                return generator;
            }

            GameObject generatorObject = new GameObject("Edificio");
            generatorObject.transform.position = Vector3.zero;
            generatorObject.transform.rotation = Quaternion.identity;
            return generatorObject.AddComponent<BuildingGenerator>();
        }

        private static void ApplyMaterials(BuildingGenerator generator, MaterialSet materials)
        {
            generator.SetMaterials(
                materials.Wall,
                materials.Floor,
                materials.Ceiling,
                materials.Glass,
                materials.Shards,
                materials.Debris);
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();

            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}
