using AssetManager;

using System;

using Unity.Netcode;

using UnityEngine;

namespace Game.Components
{
    [RequireComponent(typeof(UnitBehaviour))]
    public class UnitBehaviour_Base : NetworkBehaviour, IComparable<UnitBehaviour_Base>, ICopyable<UnitBehaviour_Base>
    {
        [SerializeField] internal float Priority;
        [SerializeField, ReadOnly] protected MovementEngine MovementEngine;

        protected virtual void Start()
        {
            if (!TryGetComponent(out MovementEngine))
            { Debug.LogWarning($"[{nameof(UnitBehaviour_Base)}]: {nameof(MovementEngine)} is null", this); }
        }

        public int CompareTo(UnitBehaviour_Base other) => (-Priority).CompareTo(-other.Priority);

        /// <summary>
        /// <b>Steering (X):</b> <br/>
        /// <code>-1f (Left) ... 0f (None) ... 1f (Right)</code> <br/>
        /// <b>Acceleration (Y):</b> <br/>
        /// <code>-1f (Reverse) ... 0f (Stop) ... 1f (Accelerate)</code> <br/>
        /// </summary>
        internal virtual Vector2? GetOutput()
            => null;

        public virtual void CopyTo(object destination) => this.CopyTo<UnitBehaviour_Base>(destination);
        public void CopyTo(UnitBehaviour_Base destination)
        {
            destination.Priority = Priority;
        }
    }
}
