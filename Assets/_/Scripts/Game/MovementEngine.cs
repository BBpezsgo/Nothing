using Unity.Netcode;

using UnityEngine;

namespace Game.Components
{
    public abstract class MovementEngine : NetworkBehaviour
    {
        [SerializeField, ReadOnly] internal Rigidbody rb;

        public abstract Vector2 InputVector { get; set; }
        public abstract bool IsHandbraking { get; set; }

        /// <summary>
        /// Returns <see langword="true"/> if the dot product of <see cref="Transform.forward"/> and <see cref="Rigidbody2D.velocity"/> is less than 0
        /// </summary>
        public bool IsReverse => Vector3.Dot(transform.forward, rb.velocity) < 0f;

        /// <summary>
        /// Coefficient of drag
        /// </summary>
        public float Drag => rb.drag;

        /// <summary>
        /// Linear velocity in units per second
        /// </summary>
        public Vector3 Velocity => rb.velocity;

        /// <summary>
        /// Angular velocity in degrees per second
        /// </summary>
        public float AngularVelocity => rb.angularVelocity.y;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        internal virtual float CalculateBrakingDistance()
        {
            return Utilities.Acceleration.DistanceToStop(Velocity.magnitude, Drag);
        }

        internal abstract void DoUserInput();
    }
}
