using UnityEngine;

namespace Game
{
    public class QualityHandler : PrivateSingleInstance<QualityHandler>
    {
        [SerializeField] bool _enableParticles;
        [SerializeField] bool _enableModelFragmentation;

        void Reset()
        {
            _enableParticles = true;
            _enableModelFragmentation = true;
        }

        public static bool EnableParticles => instance._enableParticles;
        public static bool EnableModelFragmentation => instance._enableModelFragmentation;
    }
}
