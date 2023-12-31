using Unity.Netcode;
using UnityEngine;

namespace Game.Components
{
    public class SimpleDamagable : NetworkBehaviour, IDamagable
    {
        [SerializeField] float HP;
        [SerializeField] GameObject DestroyEffect;

        public void Damage(float ammount)
        {
            HP -= ammount;
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
