using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#nullable enable

namespace Game.Components
{
    public delegate void OnEnableEvent();

    public class OnEnabled : MonoBehaviour
    {
        public event OnEnableEvent? onEnable;
        void OnEnable() => onEnable?.Invoke();
    }

    public static class OnEnableExtension
    {
        [return: NotNullIfNotNull("self")]
        public static OnEnabled? OnEnabled(this GameObject? self) => self == null ? null : self.GetComponent<OnEnabled>();
        [return: NotNullIfNotNull("self")]
        public static OnEnabled? OnEnabled(this Component? self) => self == null ? null : self.GetComponent<OnEnabled>();
    }
}
