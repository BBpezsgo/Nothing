using InputUtils;

using System.Collections.Generic;

using UnityEngine;

namespace Game.Managers
{
    public class KeyboardManager : MonoBehaviour
    {
        static KeyboardManager Instance;

        [SerializeField, ReadOnly, NonReorderable] List<PriorityKey> Keys;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning($"[{nameof(KeyboardManager)}]: Instance already registered, destroying self");
                Object.Destroy(this);
                return;
            }
            Instance = this;
            Keys = new List<PriorityKey>();
        }

        void Update()
        {
            for (int i = 0; i < Keys.Count; i++)
            {
                if (Keys[i].Update())
                { break; }
            }
        }

        void RegisterMouse_(PriorityKey key)
        {
            Keys.Add(key);
            Keys.Sort();
        }

        void DeregisterMouse_(PriorityKey key)
        {
            Keys.Remove(key);
        }

        internal static void Register(PriorityKey key)
        {
            if (Instance == null) return;
            Instance.RegisterMouse_(key);
        }
        internal static void Deregister(PriorityKey key)
        {
            if (Instance == null) return;
            Instance.DeregisterMouse_(key);
        }
    }
}
