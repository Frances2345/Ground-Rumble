using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GroundRumble.EditorTools
{
    /// <summary>
    /// Configura la escena con jugador (prefab), managers y edificio generado.
    /// </summary>
    public static class GroundRumbleSetup
    {
        private const string MaterialsFolder = "Assets/GroundRumble/Materials";
        private const string PrefabsFolder = "Assets/GroundRumble/Prefabs";
        private const string PlayerPrefabPath = PrefabsFolder + "/Jugador.prefab";
        private const string ColumnPrefabPath = PrefabsFolder + "/Columna.prefab";
        private const string TmpFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private const int BuildingFloors = 30;
        private const float BuildingFloorHeight = 3.6f;
        private const float BuildingWidth = 28f;
        private const float BuildingDepth = 24f;
        private const int BuildingWindows = 4;
        private const float BuildingGlassChance = 0.3f;
        private const float SprintSpeed = 8.6f;
        private const float QuakeDamagePerSecond = 4f;

        private struct MaterialSet
        {
            public Material Wall;
            public Material Floor;
            public Material Ceiling;
            public Material Glass;
            public Material Shards;
            public Material Debris;
            public Material Column;
            public Material Exit;
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
            ConfigureBuilding(generator, materials);
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

        public static bool SetupScene()
        {
            if (!EnsureTmpEssentials())
            {
                Debug.LogWarning("Ground Rumble: importando TMP Essentials. El setup se completara en cuanto terminen de importarse.");
                return false;
            }

            MaterialSet materials = EnsureMaterials();
            BuildingGenerator generator = GetOrCreateGenerator();
            ConfigureBuilding(generator, materials);
            MoveSampleGroundDown();
            GameObject player = SetupPlayer();
            SetupManagers(materials);
            player = EnsurePlayerPrefab(player);
            ApplyPlayerTuning(player);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            return true;
        }

        public static bool SetupSceneBatch()
        {
            Scene scene = SceneManager.GetActiveScene();
            bool wasDirty = scene.isDirty;
            bool ready = SetupScene();

            if (!ready)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(scene.path))
            {
                if (wasDirty)
                {
                    Debug.LogWarning("Ground Rumble: la escena tenia cambios sin guardar. Se aplico la configuracion pero no se guardo automaticamente; guarda la escena manualmente.");
                }
                else
                {
                    EditorSceneManager.SaveScene(scene);
                }
            }

            AssetDatabase.SaveAssets();
            return true;
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
                Debris = GetOrCreateMaterial("Escombro", () => MaterialFactory.CreateLitMaterial(new Color(0.42f, 0.4f, 0.37f, 1f), 0.05f)),
                Column = GetOrCreateMaterial("Columna", () => MaterialFactory.CreateLitMaterial(new Color(0.3f, 0.62f, 0.45f, 1f), 0.35f)),
                Exit = GetOrCreateMaterial("Salida", () => MaterialFactory.CreateLitMaterial(new Color(0.1f, 0.9f, 0.35f, 1f), 0.3f))
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

        private static GameObject SetupPlayer()
        {
            PlayerController controller = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            BuildingGenerator generator = GetOrCreateGenerator();
            Vector3 spawnPosition = generator.GetTopFloorSpawnPosition();
            GameObject player;

            if (controller == null)
            {
                player = new GameObject("Jugador");
                player.tag = "Player";

                CharacterController characterController = player.AddComponent<CharacterController>();
                characterController.height = 2f;
                characterController.radius = 0.35f;
                characterController.center = new Vector3(0f, 1f, 0f);
                characterController.slopeLimit = 45f;
                characterController.stepOffset = 0.3f;
                characterController.skinWidth = 0.02f;
                characterController.minMoveDistance = 0f;

                player.AddComponent<Stamina>();
                player.AddComponent<Vida>();
                controller = player.AddComponent<PlayerController>();
                player.AddComponent<MouseLook>();
            }
            else
            {
                player = controller.gameObject;
            }

            player.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

            EnsureComponent<Stamina>(player);
            EnsureComponent<Vida>(player);
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
            return player;
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

            BuildingGenerator generator = GetOrCreateGenerator();
            ApplyMaterials(generator, materials);
            generator.SetRoomDefaults(10, true, 12f);
            generator.Generate();
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

        private static void ConfigureBuilding(BuildingGenerator generator, MaterialSet materials)
        {
            ApplyMaterials(generator, materials);
            generator.SetColumnPrefab(EnsureColumnPrefab(materials.Column));
            generator.ConfigureLayout(
                BuildingFloors,
                BuildingFloorHeight,
                BuildingWidth,
                BuildingDepth,
                BuildingWindows,
                BuildingGlassChance);
        }

        private static void ApplyMaterials(BuildingGenerator generator, MaterialSet materials)
        {
            generator.SetMaterials(
                materials.Wall,
                materials.Floor,
                materials.Ceiling,
                materials.Glass,
                materials.Shards,
                materials.Debris,
                materials.Exit);

            generator.SetColumnMaterial(materials.Column);
        }

        private static GameObject EnsureColumnPrefab(Material material)
        {
            EnsurePrefabsFolder();

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ColumnPrefabPath);

            if (existing != null)
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(ColumnPrefabPath);
                Renderer renderer = contents.GetComponent<Renderer>();

                if (renderer != null && material != null)
                {
                    renderer.sharedMaterial = material;
                }

                if (contents.GetComponent<SafeZone>() == null)
                {
                    SafeZone contentsZone = contents.AddComponent<SafeZone>();
                    contentsZone.Configure(1.8f, 2.3f);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, ColumnPrefabPath);
                PrefabUtility.UnloadPrefabContents(contents);
                return AssetDatabase.LoadAssetAtPath<GameObject>(ColumnPrefabPath);
            }

            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            temp.name = "Columna";
            Renderer tempRenderer = temp.GetComponent<Renderer>();

            if (tempRenderer != null && material != null)
            {
                tempRenderer.sharedMaterial = material;
            }

            SafeZone zone = EnsureComponent<SafeZone>(temp);
            zone.Configure(1.8f, 2.3f);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, ColumnPrefabPath);
            UnityEngine.Object.DestroyImmediate(temp);

            return prefab != null ? prefab : AssetDatabase.LoadAssetAtPath<GameObject>(ColumnPrefabPath);
        }

        private static bool EnsureTmpEssentials()
        {
            if (LoadTmpFont() != null)
            {
                return true;
            }

            string packagePath = FindTmpEssentialsPackage();

            if (string.IsNullOrEmpty(packagePath))
            {
                return false;
            }

            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return LoadTmpFont() != null;
        }

        private static string FindTmpEssentialsPackage()
        {
            string[] packageRoots = Directory.GetDirectories("Library/PackageCache", "com.unity.ugui@*");
            string[] oldRoots = Directory.GetDirectories("Library/PackageCache", "com.unity.textmeshpro@*");
            string[] roots = new string[packageRoots.Length + oldRoots.Length];
            packageRoots.CopyTo(roots, 0);
            oldRoots.CopyTo(roots, packageRoots.Length);

            foreach (string root in roots)
            {
                string path = Path.Combine(root, "Package Resources", "TMP Essential Resources.unitypackage");

                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static TMP_FontAsset LoadTmpFont()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
        }

        private static GameObject EnsurePlayerPrefab(GameObject player)
        {
            EnsurePrefabsFolder();

            if (PrefabUtility.IsPartOfPrefabInstance(player))
            {
                return player;
            }

            Vector3 currentPosition = player.transform.position;
            Quaternion currentRotation = player.transform.rotation;

            player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            player.transform.SetPositionAndRotation(currentPosition, currentRotation);

            if (prefabAsset == null)
            {
                return player;
            }

            Transform parent = player.transform.parent;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
            instance.transform.SetPositionAndRotation(currentPosition, currentRotation);

            UnityEngine.Object.DestroyImmediate(player);
            return instance;
        }

        private static void EnsurePrefabsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/GroundRumble"))
            {
                AssetDatabase.CreateFolder("Assets", "GroundRumble");
            }

            if (!AssetDatabase.IsValidFolder(PrefabsFolder))
            {
                AssetDatabase.CreateFolder("Assets/GroundRumble", "Prefabs");
            }
        }

        private static void ApplyPlayerTuning(GameObject player)
        {
            TunePlayer(player);

            if (!File.Exists(PlayerPrefabPath))
            {
                return;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            TunePlayer(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            TunePlayer(player);
        }

        private static void TunePlayer(GameObject root)
        {
            PlayerController controller = root.GetComponent<PlayerController>();

            if (controller != null)
            {
                controller.ConfigureSprint(SprintSpeed, BuildingFloorHeight);
            }

            Vida vida = root.GetComponent<Vida>();

            if (vida != null)
            {
                vida.ConfigureQuakeDamage(QuakeDamagePerSecond);
                vida.RespawnOnDeath = false;
            }
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
