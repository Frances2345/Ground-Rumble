using UnityEngine;
using UnityEngine.SceneManagement;

namespace GroundRumble
{
    /// <summary>
    /// Carga la escena final cuando el jugador llega a la salida del nivel.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LevelExit : MonoBehaviour
    {
        [SerializeField] private string sceneName = "Final";
        [SerializeField] private bool requirePlayer = true;

        private bool loading;

        public void Configure(string targetScene)
        {
            sceneName = string.IsNullOrEmpty(targetScene) ? "Final" : targetScene;
        }

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();

            if (trigger != null)
            {
                trigger.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (loading)
            {
                return;
            }

            if (requirePlayer && other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            loading = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
