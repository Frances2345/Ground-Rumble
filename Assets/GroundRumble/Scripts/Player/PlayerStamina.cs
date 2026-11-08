using System;
using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Gestiona la estamina del jugador: se gasta al correr y se regenera al descansar.
    /// </summary>
    public class PlayerStamina : MonoBehaviour
    {
        [Header("Estamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float drainPerSecond = 22f;
        [SerializeField] private float regenPerSecond = 14f;
        [SerializeField] private float regenDelay = 1f;
        [SerializeField] private float minimumToStartSprint = 12f;

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
        }

        private void Update()
        {
            if (IsSprinting)
            {
                return;
            }

            if (regenTimer > 0f)
            {
                regenTimer -= Time.deltaTime;
                return;
            }

            if (CurrentStamina < maxStamina)
            {
                CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + regenPerSecond * Time.deltaTime);
                OnStaminaChanged?.Invoke(Normalized);
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
        }
    }
}
