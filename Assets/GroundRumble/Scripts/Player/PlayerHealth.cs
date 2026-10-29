using UnityEngine;

namespace GroundRumble.Player
{
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead => CurrentHealth <= 0f;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }
    }
}
