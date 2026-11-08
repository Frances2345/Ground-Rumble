using System;
using System.Collections;
using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Vida del jugador. Los añicos y escombros le hacen daño al impactar.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Vida")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invulnerabilityTime = 0.25f;

        [Header("Muerte")]
        [SerializeField] private bool respawnOnDeath = true;
        [SerializeField] private float respawnDelay = 3f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public float Normalized => maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;
        public bool IsDead { get; private set; }

        public event Action<float, float> OnHealthChanged;
        public event Action OnDied;
        public event Action OnRespawned;

        private PlayerController playerController;
        private CharacterController characterController;
        private float invulnerabilityTimer;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            playerController = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }

        private void Update()
        {
            if (invulnerabilityTimer > 0f)
            {
                invulnerabilityTimer -= Time.deltaTime;
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            if (IsDead || amount <= 0f || invulnerabilityTimer > 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            invulnerabilityTimer = invulnerabilityTime;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void RestoreFull()
        {
            IsDead = false;
            CurrentHealth = maxHealth;
            invulnerabilityTimer = 0f;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
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
    }
}
