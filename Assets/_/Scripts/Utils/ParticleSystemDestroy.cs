using UnityEngine;

namespace Game.Components
{
    public class ParticleSystemDestroy : MonoBehaviour
    {
        [SerializeField, ReadOnly] ParticleSystem[] ParticleSystems;

        void Awake()
        {
            ParticleSystems = GetComponentsInChildren<ParticleSystem>(false);
        }

        void FixedUpdate()
        {
            for (int i = 0; i < ParticleSystems.Length; i++)
            {
                if (ParticleSystems[i] == null)
                { continue; }
                if (ParticleSystems[i].particleCount > 0)
                { return; }
            }
            GameObject.Destroy(gameObject);
        }
    }
}
