using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Range(0f, 1f)]
    public float timeOfDay = 0f;
    public float dayDurationInSeconds = 60f;

    [Header("Referencia del Sol")]
    public Light directionalLight;

    void Update()
    {
        timeOfDay += Time.deltaTime / dayDurationInSeconds;
        if (timeOfDay >= 1f)
        {
            timeOfDay = 0f;
        }

        if (directionalLight != null)
        {
            float sunAngle = (timeOfDay * 360f) - 90f;
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

            if (IsDay())
            {
                directionalLight.intensity = 1.0f;
            }
            else
            {
                directionalLight.intensity = 0.15f;
            }
        }

        bool isCurrentDay = IsDay();
        Debug.Log("Estado actual del ciclo - ¿Es de día?: " + isCurrentDay + " | Tiempo: " + timeOfDay);
    }

    public bool IsDay()
    {
        return (timeOfDay >= 0.25f && timeOfDay <= 0.75f);
    }
}