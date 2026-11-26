using System.Collections;
using UnityEngine;
using TMPro;

public class TextoGUIA : MonoBehaviour
{
    public TextMeshProUGUI texto;
    public float intervalo = 1f;
    public float durationTotal = 10f;

    void Start()
    {
        if (texto != null)
        {
            StartCoroutine(Parpadear());
        }
    }

    private IEnumerator Parpadear()
    {
        float tiempoInicio = Time.time;
        float tiempoFase = 0f;
        bool desapareciendo = true;

        SetAlpha(1f);

        while (Time.time - tiempoInicio < durationTotal)
        {
            tiempoFase += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoFase / Mathf.Max(0.01f, intervalo));

            if (desapareciendo)
            {
                SetAlpha(Mathf.Lerp(1f, 0f, t));
            }
            else
            {
                SetAlpha(Mathf.Lerp(0f, 1f, t));
            }

            if (tiempoFase >= intervalo)
            {
                tiempoFase = 0f;
                desapareciendo = !desapareciendo;
            }

            yield return null;
        }

        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        Color c = texto.color;
        c.a = Mathf.Clamp01(alpha);
        texto.color = c;
    }
}