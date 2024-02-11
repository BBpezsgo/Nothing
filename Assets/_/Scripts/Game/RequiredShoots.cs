using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Game.Components
{
    public class RequiredShoots : MonoBehaviour
    {
        [System.Serializable]
        class IncomingBullet
        {
            [SerializeField, ReadOnly] internal float timeToImpact;
            [SerializeField, ReadOnly] internal readonly float damage;

            public IncomingBullet(float timeToImpact, float damage)
            {
                this.timeToImpact = timeToImpact;
                this.damage = damage;
            }
        }

        [SerializeField, ReadOnly, NonReorderable] List<IncomingBullet> incomingBullets = new List<IncomingBullet>();
        [SuppressMessage("", "IDE0052")]
        [SerializeField, ReadOnly] float estimatedHP;

        float HP
        {
            get
            {
                if (TryGetComponent(out Unit ingameObject))
                { return ingameObject.HP; }

                if (TryGetComponent(out Building building))
                { return building.HP; }

                return 0f;
            }
        }

        float EstimatedDamage
        {
            get
            {
                float dmg = 0f;
                for (int i = 0; i < incomingBullets.Count; i++)
                {
                    if (incomingBullets[i].timeToImpact <= 0f) continue;
                    dmg += incomingBullets[i].damage;
                }
                return dmg;
            }
        }

        public float EstimatedHP
        {
            get
            {
                float v = HP - EstimatedDamage;
                estimatedHP = v;
                return v;
            }
        }

        public void Shoot(float timeToImpact, float damage)
        {
            if (timeToImpact < 0f) return;
            if (damage <= 0.01f) return;
            incomingBullets.Add(new IncomingBullet(timeToImpact, damage));
        }

        void Start()
        {
            estimatedHP = HP;
        }

        void Update()
        {
            for (int i = incomingBullets.Count - 1; i >= 0; i--)
            {
                incomingBullets[i].timeToImpact -= Time.deltaTime;
                if (incomingBullets[i].timeToImpact <= 0f)
                { incomingBullets.RemoveAt(i); }
            }
        }
    }
}
