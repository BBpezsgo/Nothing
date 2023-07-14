using UnityEngine;

public class ParticleSystemDestroy : MonoBehaviour
{
    [SerializeField, ReadOnly] ParticleSystem _particleSystem;

    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    void FixedUpdate()
    {
        if (_particleSystem.particleCount == 0)
        {
            GameObject.Destroy(gameObject);
        }
    }
}
