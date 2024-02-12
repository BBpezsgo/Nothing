using Unity.Netcode;
using UnityEngine;

namespace Game.Components
{
    public class SimpleDamagable : NetworkBehaviour, IDamageable
    {
        [SerializeField] float HP;
        [SerializeField] GameObject DestroyEffect;
        [SerializeField, Min(0f)] float CollisionDamageMultiplier = 1f;

        [SerializeField, Button(nameof(Explode), false, true, "Destroy")] string ButtonExplode;

        void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.TryGetComponent(out Rigidbody rigidbody))
            { return; }
            if (collision.gameObject.HasComponent<Projectile>())
            { return; }

            float amount = rigidbody.velocity.sqrMagnitude * CollisionDamageMultiplier;

            if (amount <= .1f)
            { return; }

            Damage(amount);
        }

        public void Damage(float amount)
        {
            HP -= amount;
            if (HP <= 0)
            { Explode(); }
        }

        void Explode()
        {
            if (!NetcodeUtils.IsOfflineOrServer) return;

            if (DestroyEffect != null && QualityHandler.EnableParticles)
            { GameObject.Instantiate(DestroyEffect, transform.position, Quaternion.identity, ObjectGroups.Effects); }

            if (TryGetComponent(out Explode explode))
            { explode.Do(); }

            GameObject.Destroy(gameObject);
        }
    }
}
