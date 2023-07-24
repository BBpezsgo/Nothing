using UnityEngine;

namespace Game.Managers
{
    public class WaterManager : MonoBehaviour
    {
        static WaterManager instance;

        [SerializeField] float waterLevel;

        public static float WaterLevel => instance != null ? instance.waterLevel : -50f;

        void Awake()
        {
            if (instance != null)
            {
                Debug.Log($"[{nameof(WaterManager)}]: Instance already registered");
                Destroy(this);
                return;
            }
            instance = this;
        }
    }
}
