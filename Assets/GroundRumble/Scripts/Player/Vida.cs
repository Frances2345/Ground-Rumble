using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GroundRumble
{
    /// <summary>
    /// Vida del jugador. Los añicos y escombros le hacen daño al impactar.
    /// La barra de vida es una Image de Canvas asignable desde el inspector.
    /// </summary>
    public class Vida : MonoBehaviour
    {
        [Header("Vida")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invulnerabilityTime = 0.25f;

        [Header("Muerte")]
        [SerializeField] private bool respawnOnDeath = true;
        [SerializeField] private float respawnDelay = 3f;

        [Header("Terremoto")]
        [SerializeField] private float quakeDamagePerSecond = 4f;

        [Header("UI (Canvas)")]
        public Image healthBackground;
        public Image healthImage;
        public TMP_Text healthText;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float Normalized => maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;
        public bool IsDead { get; private set; }

        public bool RespawnOnDeath
        {
            get => respawnOnDeath;
            set => respawnOnDeath = value;
        }

        public event Action<float, float> OnHealthChanged;
        public event Action OnDied;
        public event Action OnRespawned;

        private PlayerController playerController;
        private CharacterController characterController;
        private float invulnerabilityTimer;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        // --- Nuevas variables para la curación por inactividad ---
        private float immobileTimer;
        private Vector3 lastPosition;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            playerController = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            lastPosition = transform.position; // Inicializamos posición

            PrepareBackground(healthBackground);
            PrepareImage(healthImage);
            UpdateUI();
        }

        private void Update()
        {
            if (invulnerabilityTimer > 0f)
            {
                invulnerabilityTimer -= Time.deltaTime;
            }

            ApplyQuakeDamage(Time.deltaTime);
            HandleInactivityHealing(Time.deltaTime); // Llamada a la curación
            UpdateUI();
        }

        // --- Nuevo método respetando tu estilo ---
        private void HandleInactivityHealing(float deltaTime)
        {
            if (IsDead || CurrentHealth >= maxHealth)
            {
                immobileTimer = 0f;
                lastPosition = transform.position;
                return;
            }

            // Comprueba si se ha movido
            if (Vector3.Distance(transform.position, lastPosition) > 0.01f)
            {
                immobileTimer = 0f;
                lastPosition = transform.position;
            }
            else
            {
                immobileTimer += deltaTime;
                if (immobileTimer >= 5f)
                {
                    Heal(2f * deltaTime); // Cura 2 por segundo
                }
            }
        }

        private void ApplyQuakeDamage(float deltaTime)
        {
            EarthquakeManager manager = EarthquakeManager.Instance;

            if (manager == null || !manager.IsShaking || IsDead)
            {
                return;
            }

            if (SafeZone.IsPositionSafe(transform.position))
            {
                return;
            }

            float amount = quakeDamagePerSecond * Mathf.Clamp01(manager.Intensity) * deltaTime;

            if (amount > 0f)
            {
                ApplyDamage(amount, true);
            }
        }

        public void ConfigureUI(Image background, Image image, TMP_Text text)
        {
            healthBackground = background;
            healthImage = image;
            healthText = text;
            PrepareBackground(healthBackground);
            PrepareImage(healthImage);

            if (Application.isPlaying)
            {
                UpdateUI();
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            ApplyDamage(amount, false);
        }

        /// <summary>
        /// Aplica dano respetando las columnas seguras. Si ignoreInvulnerability es
        /// true se usa para el desgaste continuo del terremoto o los resbalones.
        /// </summary>
        public void ApplyDamage(float amount, bool ignoreInvulnerability)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            if (!ignoreInvulnerability && invulnerabilityTimer > 0f)
            {
                return;
            }

            if (SafeZone.IsPositionSafe(transform.position))
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            immobileTimer = 0f; // Reiniciamos el temporizador al recibir daño

            if (!ignoreInvulnerability)
            {
                invulnerabilityTimer = invulnerabilityTime;
            }

            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            UpdateUI();

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        public void ConfigureQuakeDamage(float damagePerSecond)
        {
            quakeDamagePerSecond = Mathf.Max(0f, damagePerSecond);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            UpdateUI();
        }

        public void RestoreFull()
        {
            IsDead = false;
            CurrentHealth = maxHealth;
            invulnerabilityTimer = 0f;
            immobileTimer = 0f; // Reiniciamos aquí también
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            UpdateUI();
        }

        public void Kill()
        {
            if (IsDead)
            {
                return;
            }

            CurrentHealth = 0f;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            UpdateUI();
            Die();
        }

        private void Die()
        {
            IsDead = true;
            OnDied?.Invoke();

            if (playerController != null)
            {
                playerController.SetControlEnabled(false);
            }

            if (respawnOnDeath)
            {
                StartCoroutine(RespawnRoutine());
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            RestoreFull();

            if (playerController != null)
            {
                playerController.SetControlEnabled(true);
            }

            OnRespawned?.Invoke();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            DebrisDamage debris = hit.collider.GetComponentInParent<DebrisDamage>();

            if (debris != null)
            {
                debris.TryHitPlayer(this);
            }
        }

        private void UpdateUI()
        {
            if (healthImage != null)
            {
                healthImage.fillAmount = Normalized;
                healthImage.color = Color.Lerp(
                    new Color(0.9f, 0.15f, 0.1f),
                    new Color(0.2f, 0.9f, 0.35f),
                    Normalized);
            }

            if (healthText != null)
            {
                healthText.text = $"Vida: {CurrentHealth:0} / {maxHealth:0}";
                healthText.color = Color.Lerp(
                    new Color(1f, 0.3f, 0.25f),
                    new Color(0.55f, 1f, 0.6f),
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