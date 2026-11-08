using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GroundRumble
{
    /// <summary>
    /// Gestiona la estamina del jugador: se gasta al correr y se regenera al descansar.
    /// La barra de estamina es una Image de Canvas asignable desde el inspector.
    /// </summary>
    public class Stamina : MonoBehaviour
    {
        [Header("Estamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float drainPerSecond = 22f;
        [SerializeField] private float regenPerSecond = 14f;
        [SerializeField] private float regenDelay = 1f;
        [SerializeField] private float minimumToStartSprint = 12f;

        [Header("UI (Canvas)")]
        public Image staminaBackground;
        public Image staminaImage;
        public TMP_Text staminaText;

        public float MaxStamina => maxStamina;
        public float CurrentStamina { get; private set; }
        public float Normalized => maxStamina <= 0f ? 0f : CurrentStamina / maxStamina;
        public bool IsExhausted => CurrentStamina <= 0.01f;
        public bool CanStartSprint => CurrentStamina >= minimumToStartSprint;
        public bool IsSprinting { get; private set; }

        public event Action<float> OnStaminaChanged;

        private float regenTimer;

        private void Awake()
        {
            CurrentStamina = maxStamina;
            PrepareBackground(staminaBackground);
            PrepareImage(staminaImage);
            UpdateUI();
        }

        private void Update()
        {
            if (!IsSprinting)
            {
                if (regenTimer > 0f)
                {
                    regenTimer -= Time.deltaTime;
                }
                else if (CurrentStamina < maxStamina)
                {
                    CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + regenPerSecond * Time.deltaTime);
                    OnStaminaChanged?.Invoke(Normalized);
                }
            }

            UpdateUI();
        }

        public void ConfigureUI(Image background, Image image, TMP_Text text)
        {
            staminaBackground = background;
            staminaImage = image;
            staminaText = text;
            PrepareBackground(staminaBackground);
            PrepareImage(staminaImage);

            if (Application.isPlaying)
            {
                UpdateUI();
            }
        }

        /// <summary>
        /// Consume estamina mientras se corre. Devuelve true si puede seguir corriendo.
        /// </summary>
        public bool ConsumeSprint(float deltaTime)
        {
            if (CurrentStamina <= 0f)
            {
                IsSprinting = false;
                return false;
            }

            IsSprinting = true;
            regenTimer = regenDelay;
            CurrentStamina = Mathf.Max(0f, CurrentStamina - drainPerSecond * deltaTime);
            OnStaminaChanged?.Invoke(Normalized);
            UpdateUI();

            if (CurrentStamina <= 0f)
            {
                IsSprinting = false;
                return false;
            }

            return true;
        }

        public void StopSprint()
        {
            IsSprinting = false;
        }

        public void RestoreFull()
        {
            CurrentStamina = maxStamina;
            OnStaminaChanged?.Invoke(Normalized);
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (staminaImage != null)
            {
                staminaImage.fillAmount = Normalized;
                staminaImage.color = Color.Lerp(
                    new Color(0.95f, 0.75f, 0.15f),
                    new Color(0.2f, 0.75f, 1f),
                    Normalized);
            }

            if (staminaText != null)
            {
                staminaText.text = $"Estamina: {CurrentStamina:0} / {maxStamina:0}";
                staminaText.color = Color.Lerp(
                    new Color(1f, 0.6f, 0.2f),
                    new Color(0.55f, 0.85f, 1f),
                    Normalized);
            }
        }

        private static void PrepareBackground(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.raycastTarget = false;
        }

        private static void PrepareImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 1f;
        }
    }
}
