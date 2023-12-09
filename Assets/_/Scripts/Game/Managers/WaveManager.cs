using Game.Components;
using UnityEngine;

namespace Game.Managers
{
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] GameObject[] Prefabs = new GameObject[0];
        [SerializeField, ReadOnly] ThingSpawner Spawner;

        [SerializeField] float Cooldown;

        void Awake()
        {
            if (!TryGetComponent(out Spawner))
            { Debug.LogError($"[{nameof(WaveManager)}]: {nameof(Spawner)} is null", this); }
        }

        void FixedUpdate()
        {
            if (!NetcodeUtils.IsOfflineOrServer) return;

            if (Spawner.Things.Count > 0)
            { return; }

            if (Cooldown > 0f)
            {
                Cooldown -= Time.fixedDeltaTime;
                return;
            }

            Cooldown = 30f;
            for (int i = 0; i < Prefabs.Length; i++)
            {
                int n = 1;
                for (int j = 0; j < n; j++)
                {
                    Spawner.Spawn(Prefabs[i]);
                }
            }
        }
    }
}
