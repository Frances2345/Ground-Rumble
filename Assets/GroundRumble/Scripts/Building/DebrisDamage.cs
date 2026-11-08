using UnityEngine;

namespace GroundRumble
{
    /// <summary>
    /// Hace dano al jugador cuando un escombro o añico lo golpea.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DebrisDamage : MonoBehaviour
    {
        [Header("Dano")]
        [SerializeField] private float minDamage = 5f;
        [SerializeField] private float maxDamage = 25f;
        [SerializeField] private float speedForMaxDamage = 12f;
        [SerializeField] private float damageCooldown = 0.6f;

        private Rigidbody body;
        private float lastHitTime = -999f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void Configure(float minimum, float maximum)
        {
            minDamage = Mathf.Max(0f, minimum);
            maxDamage = Mathf.Max(minDamage, maximum);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Vida health = collision.collider.GetComponentInParent<Vida>();

            if (health != null)
            {
                TryHitPlayer(health);
            }
        }

        public void TryHitPlayer(Vida health)
        {
            if (health == null || Time.time - lastHitTime < damageCooldown)
            {
                return;
            }

            lastHitTime = Time.time;

            float speed = body != null ? body.linearVelocity.magnitude : 0f;
            float t = Mathf.InverseLerp(1f, Mathf.Max(1.01f, speedForMaxDamage), speed);
            float damage = Mathf.Lerp(minDamage, maxDamage, t);

            health.TakeDamage(damage, transform.position);
        }
    }
}
