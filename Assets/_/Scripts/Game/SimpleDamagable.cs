using Unity.Netcode;
using UnityEngine;

namespace Game.Components
{
    public class SimpleDamagable : NetworkBehaviour, IDamageable
    {
        [SerializeField] float HP;
        [SerializeField] GameObject DestroyEffect;

        [SerializeField, Button(nameof(Explode), false, true, "Destroy")] string ButtonExplode;

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
