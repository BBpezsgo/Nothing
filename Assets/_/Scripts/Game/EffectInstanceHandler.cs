using UnityEngine;

namespace Game.Components
{
    public class EffectInstanceHandler : MonoBehaviour
    {
        [SerializeField, EditorOnly] bool WaitForParticles;
        [SerializeField, EditorOnly] bool WaitForSound;
        [SerializeField, Min(0f), EditorOnly] float WaitForSeconds;

        [SerializeField, ReadOnly, NonReorderable] ParticleSystem[] ParticleSystems;
        [SerializeField, ReadOnly, NonReorderable] AudioSource[] AudioSources;

        void Reset()
        {
            WaitForParticles = true;
            WaitForSound = true;
            WaitForSeconds = 0f;
        }

        void Awake()
        {
            ParticleSystems = WaitForParticles ? GetComponentsInChildren<ParticleSystem>(false) : System.Array.Empty<ParticleSystem>();
            AudioSources = WaitForSound ? GetComponentsInChildren<AudioSource>(false) : System.Array.Empty<AudioSource>();
        }

        void Update()
        {
            for (int i = 0; i < ParticleSystems.Length; i++)
            {
                if (ParticleSystems[i] == null) continue;
                if (ParticleSystems[i].particleCount > 0) return;
            }

            for (int i = 0; i < AudioSources.Length; i++)
            {
                if (AudioSources[i] == null) continue;
                if (AudioSources[i].isPlaying) return;
            }

            if ((WaitForSeconds -= Time.deltaTime) >= 0f) return;

            GameObject.Destroy(gameObject);
        }
    }
}
