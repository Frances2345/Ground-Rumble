
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FondoMenu : MonoBehaviour
{
    public Text texto;
    public float velocidadParpadeo = 1.5f;
    public string nombreEscena;

    void Start()
    {
        StartCoroutine(Parpadear());
    }

    IEnumerator Parpadear()
    {
        while (true)
        {
            texto.CrossFadeAlpha(0f, velocidadParpadeo, false);
            yield return new WaitForSeconds(velocidadParpadeo);

            texto.CrossFadeAlpha(1f, velocidadParpadeo, false);
            yield return new WaitForSeconds(velocidadParpadeo);
        }
    }

    void Update()
    {
        if (Input.anyKeyDown)
        {
            SceneManager.LoadScene();
        }
    }
}
