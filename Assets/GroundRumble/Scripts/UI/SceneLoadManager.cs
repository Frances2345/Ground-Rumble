using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    public void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    public void LoadMainGame()
    {
        SceneManager.LoadScene("MainGame");
    }

    public void LoadOpciones()
    {
        SceneManager.LoadScene("Opciones");
    }

    public void LoadGameOver()
    {
        SceneManager.LoadScene("GameOver");
    }

    public void LoadFinal()
    {
        SceneManager.LoadScene("Final");
    }

    public void LoadCreditos()
    {
        SceneManager.LoadScene("Creditos");
    }

    // Método general para cargar cualquier escena escribiendo su nombre
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Cerrando el juego...");
        Application.Quit();
    }
}