using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GroundRumble.EditorTools
{
    /// <summary>
    /// Configura la escena con jugador (prefab), managers, HUD en Canvas y edificio generado.
    /// </summary>
    public static class GroundRumbleSetup
    {
        private const string MaterialsFolder = "Assets/GroundRumble/Materials";
        private const string PrefabsFolder = "Assets/GroundRumble/Prefabs";
        private const string PlayerPrefabPath = PrefabsFolder + "/Jugador.prefab";
        private const string ColumnPrefabPath = PrefabsFolder + "/Columna.prefab";
        private const string HudCanvasName = "Canvas_UI";
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

        private struct HudRefs
        {
            public Image HealthBackground;
            public Image HealthFill;
            public TMP_Text HealthText;
            public Image StaminaBackground;
            public Image StaminaFill;
            public TMP_Text StaminaText;
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
            HudRefs hud = SetupHudUI();
            player = EnsurePlayerPrefab(player);
            AssignPlayerUI(player, hud);
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

        private static HudRefs SetupHudUI()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject canvasObject = GameObject.Find(HudCanvasName);

            if (canvasObject == null)
            {
                canvasObject = new GameObject(HudCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }

            Canvas canvas = EnsureComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureComponent<GraphicRaycaster>(canvasObject);

            RemoveLegacyHudObjects(canvasObject);

            EarthquakeHUD hud = EnsureComponent<EarthquakeHUD>(canvasObject);

            TMP_Text countdownText = GetOrCreateCountdownText(canvasObject.transform, LoadTmpFont());

            GameObject panel = GetOrCreatePanel(canvasObject.transform, "Panel_UI", font);

            TMP_FontAsset tmpFont = LoadTmpFont();

            Text statusText = GetOrCreateText(panel.transform, "Texto_Estado", -6f, 40f, 30, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.9f, 0.95f, 1f), font);
            Text detailsText = GetOrCreateText(panel.transform, "Texto_Detalles", -46f, 26f, 19, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.78f, 0.82f, 0.9f), font);
            TMP_Text guideText = GetOrCreateTmpText(panel.transform, "Texto_Controles", -76f, 36f, 16f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.62f, 0.66f, 0.74f), tmpFont);
            TMP_Text healthText = GetOrCreateTmpText(panel.transform, "Texto_Vida", -116f, 24f, 20f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.55f, 1f, 0.6f), tmpFont);
            healthText.text = "Vida: 100 / 100";
            Image healthBackground;
            Image healthFill = GetOrCreateBar(panel.transform, "Barra_Vida_Fondo", "Barra_Vida_Relleno", -142f, 10f, new Color(0.2f, 0.9f, 0.35f), out healthBackground);
            TMP_Text staminaText = GetOrCreateTmpText(panel.transform, "Texto_Stamina", -156f, 24f, 20f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.55f, 0.85f, 1f), tmpFont);
            staminaText.text = "Estamina: 100 / 100";
            Image staminaBackground;
            Image staminaFill = GetOrCreateBar(panel.transform, "Barra_Stamina_Fondo", "Barra_Stamina_Relleno", -182f, 10f, new Color(0.2f, 0.75f, 1f), out staminaBackground);
            Image intensityFill = GetOrCreateBar(panel.transform, "Barra_Terremoto_Fondo", "Barra_Terremoto_Relleno", -198f, 8f, new Color(1f, 0.75f, 0.2f), out _);

            controlGuia guide = EnsureComponent<controlGuia>(panel);
            guide.ConfigureUI(guideText);

            Image stateImage = GetOrCreateStateImage(canvasObject.transform);
            Image deathOverlay = GetOrCreateDeathOverlay(canvasObject.transform, tmpFont, out TMP_Text deathText);
            Estado estado = EnsureComponent<Estado>(canvasObject);
            estado.ConfigureUI(stateImage, deathOverlay, deathText);

            hud.ConfigureUI(statusText, detailsText, intensityFill, countdownText);

            return new HudRefs
            {
                HealthBackground = healthBackground,
                HealthFill = healthFill,
                HealthText = healthText,
                StaminaBackground = staminaBackground,
                StaminaFill = staminaFill,
                StaminaText = staminaText
            };
        }

        private static void RemoveLegacyHudObjects(GameObject keep)
        {
            EarthquakeHUD[] huds = UnityEngine.Object.FindObjectsByType<EarthquakeHUD>(FindObjectsInactive.Include);

            foreach (EarthquakeHUD hud in huds)
            {
                if (hud.gameObject != keep)
                {
                    UnityEngine.Object.DestroyImmediate(hud.gameObject);
                }
            }
        }

        private static TMP_Text GetOrCreateCountdownText(Transform canvas, TMP_FontAsset font)
        {
            const string name = "Texto_Cuenta_Terremoto";
            Transform existing = canvas.Find(name);
            TextMeshProUGUI text;

            if (existing != null)
            {
                text = existing.GetComponent<TextMeshProUGUI>();

                if (text == null)
                {
                    return null;
                }
            }
            else
            {
                if (font == null)
                {
                    return null;
                }

                GameObject textObject = new GameObject(name, typeof(RectTransform));
                textObject.transform.SetParent(canvas, false);
                text = textObject.AddComponent<TextMeshProUGUI>();

                RectTransform rect = text.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 120f);
                rect.sizeDelta = new Vector2(1200f, 260f);
            }

            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = 64f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.35f, 0.25f);
            text.raycastTarget = false;
            text.text = string.Empty;
            return text;
        }

        private static TMP_Text GetOrCreateTmpText(Transform parent, string name, float y, float height, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color, TMP_FontAsset font)
        {
            Transform existing = parent.Find(name);
            TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;

            if (existing != null && text == null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            if (text == null)
            {
                GameObject textObject = new GameObject(name, typeof(RectTransform));
                textObject.transform.SetParent(parent, false);
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-24f, height);

            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Image GetOrCreateStateImage(Transform canvas)
        {
            const string name = "Indicador_Estado";
            Transform existing = canvas.Find(name);
            Image image = existing != null ? existing.GetComponent<Image>() : null;

            if (image == null)
            {
                GameObject indicator = new GameObject(name, typeof(RectTransform), typeof(Image));
                indicator.transform.SetParent(canvas, false);

                RectTransform rect = indicator.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(-70f, -70f);
                rect.sizeDelta = new Vector2(64f, 64f);

                image = indicator.GetComponent<Image>();
                image.sprite = GetStateSprite();
                image.color = new Color(0.2f, 0.9f, 0.35f);
                image.raycastTarget = false;
            }

            if (image.sprite == null)
            {
                image.sprite = GetStateSprite();
            }

            return image;
        }

        private static Image GetOrCreateDeathOverlay(Transform canvas, TMP_FontAsset font, out TMP_Text deathText)
        {
            const string overlayName = "Pantalla_Muerte";
            Transform existing = canvas.Find(overlayName);
            GameObject overlay;

            if (existing != null)
            {
                overlay = existing.gameObject;
            }
            else
            {
                overlay = new GameObject(overlayName, typeof(RectTransform), typeof(Image));
                overlay.transform.SetParent(canvas, false);

                RectTransform rect = overlay.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image image = overlay.GetComponent<Image>();
                image.sprite = GetWhiteSprite();
                image.color = Color.black;
                image.raycastTarget = false;
            }

            Image overlayImage = overlay.GetComponent<Image>();

            if (overlayImage.sprite == null)
            {
                overlayImage.sprite = GetWhiteSprite();
            }

            Transform existingText = overlay.transform.Find("Texto_Muerte");
            TextMeshProUGUI text = existingText != null ? existingText.GetComponent<TextMeshProUGUI>() : null;

            if (existingText != null && text == null)
            {
                UnityEngine.Object.DestroyImmediate(existingText.gameObject);
            }

            if (text == null)
            {
                GameObject textObject = new GameObject("Texto_Muerte", typeof(RectTransform));
                textObject.transform.SetParent(overlay.transform, false);

                RectTransform rect = textObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = 96f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            if (string.IsNullOrEmpty(text.text))
            {
                text.text = "HAZ MUERTO";
            }

            overlay.transform.SetAsLastSibling();
            overlay.SetActive(false);
            deathText = text;
            return overlayImage;
        }

        private static Sprite GetStateSprite()
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            return sprite != null ? sprite : GetWhiteSprite();
        }

        private static GameObject GetOrCreatePanel(Transform parent, string name, Font font)
        {
            Transform existing = parent.Find(name);

            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -16f);
            rect.sizeDelta = new Vector2(-32f, 216f);

            Image image = panel.GetComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.color = new Color(0.03f, 0.035f, 0.05f, 0.78f);
            image.raycastTarget = false;

            return panel;
        }

        private static Text GetOrCreateText(Transform parent, string name, float y, float height, int fontSize, FontStyle style, TextAnchor alignment, Color color, Font font)
        {
            Transform existing = parent.Find(name);

            if (existing != null)
            {
                Text existingText = existing.GetComponent<Text>();

                if (existingText != null)
                {
                    return existingText;
                }
            }

            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-24f, height);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            Shadow shadow = textObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1f, -1f);

            return text;
        }

        private static Image GetOrCreateBar(Transform parent, string backgroundName, string fillName, float y, float height, Color fillColor, out Image backgroundImage)
        {
            Transform existingBackground = parent.Find(backgroundName);
            GameObject background;

            if (existingBackground != null)
            {
                background = existingBackground.gameObject;
            }
            else
            {
                background = new GameObject(backgroundName, typeof(RectTransform), typeof(Image));
                background.transform.SetParent(parent, false);

                RectTransform rect = background.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, y);
                rect.sizeDelta = new Vector2(-24f, height);

                Image bgImage = background.GetComponent<Image>();
                bgImage.sprite = GetWhiteSprite();
                bgImage.color = new Color(1f, 1f, 1f, 0.12f);
                bgImage.raycastTarget = false;
            }

            backgroundImage = background.GetComponent<Image>();

            if (backgroundImage.sprite == null)
            {
                backgroundImage.sprite = GetWhiteSprite();
            }

            Transform existingFill = background.transform.Find(fillName);
            GameObject fill;

            if (existingFill != null)
            {
                fill = existingFill.gameObject;
            }
            else
            {
                fill = new GameObject(fillName, typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(background.transform, false);

                RectTransform rect = fill.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image fillImage = fill.GetComponent<Image>();
                fillImage.sprite = GetWhiteSprite();
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImage.color = fillColor;
                fillImage.raycastTarget = false;
            }

            Image image = fill.GetComponent<Image>();

            if (image.sprite == null)
            {
                image.sprite = GetWhiteSprite();
            }

            return image;
        }

        private static Sprite GetWhiteSprite()
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            if (sprite == null)
            {
                sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            }

            return sprite;
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

        private static void AssignPlayerUI(GameObject player, HudRefs hud)
        {
            Vida vida = player.GetComponent<Vida>();

            if (vida != null)
            {
                vida.ConfigureUI(hud.HealthBackground, hud.HealthFill, hud.HealthText);
            }

            Stamina stamina = player.GetComponent<Stamina>();

            if (stamina != null)
            {
                stamina.ConfigureUI(hud.StaminaBackground, hud.StaminaFill, hud.StaminaText);
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
