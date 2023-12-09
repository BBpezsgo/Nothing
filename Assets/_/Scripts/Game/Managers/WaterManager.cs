using UnityEngine;

namespace Game.Managers
{
    public class WaterManager : PrivateSingleInstance<WaterManager>
    {
        [SerializeField] float waterLevel;

        public static float WaterLevel => instance != null ? instance.waterLevel : -50f;
    }
}
