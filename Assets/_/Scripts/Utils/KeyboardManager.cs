using System.Collections.Generic;

using UnityEngine;

namespace Game.Managers
{
    public class KeyboardManager : MonoBehaviour
    {
        static KeyboardManager Instance;

        [SerializeField, ReadOnly, NonReorderable] List<Utilities.PriorityKey> Keys;
        static readonly Utilities.PriorityKeyComparer Comparer = new();

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning($"[{nameof(KeyboardManager)}]: Instance already registered");
                Object.Destroy(this);
                return;
            }
            Instance = this;
            Keys = new List<Utilities.PriorityKey>();
        }

        void Update()
        {
            for (int i = 0; i < Keys.Count; i++)
            {
                if (Keys[i].Update())
                { break; }
            }
        }

        void RegisterMouse_(Utilities.PriorityKey key)
        {
            Keys.Add(key);
            Keys.Sort();
        }

        void DeregisterMouse_(Utilities.PriorityKey key)
        {
            Keys.Remove(key);
        }

        internal static void Register(Utilities.PriorityKey key)
        {
            if (Instance == null) return;
            Instance.RegisterMouse_(key);
        }
        internal static void Deregister(Utilities.PriorityKey key)
        {
            if (Instance == null) return;
            Instance.DeregisterMouse_(key);
        }
    }
}
