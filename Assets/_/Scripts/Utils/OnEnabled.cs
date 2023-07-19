
using UnityEngine;

namespace Game.Components
{
    internal delegate void OnEnableEvent();

    public class OnEnabled : MonoBehaviour
    {
        internal event OnEnableEvent onEnable;
        void OnEnable() => onEnable?.Invoke();
    }

    public static class OnEnableExtension
    {
        public static OnEnabled OnEnabled(this GameObject self) => self == null ? null : self.GetComponent<OnEnabled>();
        public static OnEnabled OnEnabled(this Component self) => self == null ? null : self.GetComponent<OnEnabled>();
    }
}
