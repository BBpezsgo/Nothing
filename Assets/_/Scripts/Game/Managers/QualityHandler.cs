using UnityEngine;

namespace Game
{
    public class QualityHandler : PrivateSingleInstance<QualityHandler>
    {
        [SerializeField] bool _enableParticles;
        [SerializeField] bool _enableModelFragmentation;
        [SerializeField] bool _enableProjectileTrails;
        [SerializeField, EditorOnly] int _targetFps = 30;

        void Reset()
        {
            _enableParticles = true;
            _enableModelFragmentation = true;
            _enableProjectileTrails = true;
            Application.targetFrameRate = _targetFps;
        }

        public static bool EnableParticles => instance._enableParticles;
        public static bool EnableModelFragmentation => instance._enableModelFragmentation;
        public static bool EnableProjectileTrails => instance._enableProjectileTrails;
    }
}
