#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GroundRumble.EditorTools
{
    /// <summary>
    /// Ejecuta la configuracion de la escena una sola vez tras recompilar,
    /// para que el prefab del jugador y el Canvas queden generados.
    /// </summary>
    [InitializeOnLoad]
    public static class GroundRumbleBootstrap
    {
        private const string MarkerPath = "Temp/GroundRumbleSetup.v3.done";
        private const string SetupScenePath = "Assets/Scenes/SampleScene.unity";
        private const int MaxAttempts = 600;

        private static int attempts;

        static GroundRumbleBootstrap()
        {
            EditorApplication.delayCall += TryRun;
        }

        private static void TryRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || EditorApplication.isCompiling
                || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryRun;
                return;
            }

            if (File.Exists(MarkerPath))
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();

            if (scene.path != SetupScenePath)
            {
                return;
            }

            attempts++;

            if (!GroundRumbleSetup.SetupSceneBatch())
            {
                if (attempts < MaxAttempts)
                {
                    EditorApplication.delayCall += TryRun;
                }
                else
                {
                    Debug.LogError("Ground Rumble: no se pudieron importar TMP Essentials; ejecuta Ground Rumble/Configurar escena completa manualmente.");
                    File.WriteAllText(MarkerPath, System.DateTime.Now.ToString("o"));
                }

                return;
            }

            File.WriteAllText(MarkerPath, System.DateTime.Now.ToString("o"));
            AssetDatabase.Refresh();
        }
    }
}
#endif
