using System.Collections;
using TMPro;
using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Alerta temporal: hace parpadear un TMP_Text (aparece y desaparece) durante
    /// unos segundos y luego lo oculta. Se vuelve a activar con Activar() o al
    /// habilitar el objeto.
    /// </summary>
    public class Indicador : MonoBehaviour
    {
        [Header("Texto (asignar en el inspector)")]
        public TMP_Text texto;

        [Header("Parpadeo")]
        [SerializeField] private float duracion = 10f;
        [SerializeField] private float intervaloParpadeo = 0.4f;

        private Coroutine rutina;
        private float alphaOriginal = 1f;
        private bool alphaCapturado;

        private void Awake()
        {
            CapturarAlpha();
        }

        private void OnEnable()
        {
            Activar();
        }

        private void OnDisable()
        {
            Detener();
        }

        public void Activar()
        {
            if (texto == null)
            {
                return;
            }

            CapturarAlpha();
            Detener();
            rutina = StartCoroutine(Parpadear());
        }

        public void Detener()
        {
            if (rutina != null)
            {
                StopCoroutine(rutina);
                rutina = null;
            }
        }

        private void CapturarAlpha()
        {
            if (!alphaCapturado && texto != null && texto.alpha > 0f)
            {
                alphaOriginal = texto.alpha;
                alphaCapturado = true;
            }
        }

        private IEnumerator Parpadear()
        {
            float intervalo = Mathf.Max(0.05f, intervaloParpadeo);
            float final = Time.time + Mathf.Max(0f, duracion);
            bool visible = true;
            texto.alpha = alphaOriginal;

            while (Time.time < final)
            {
                yield return new WaitForSeconds(intervalo);
                visible = !visible;
                texto.alpha = visible ? alphaOriginal : 0f;
            }

            texto.alpha = 0f;
            rutina = null;
        }
    }
}
