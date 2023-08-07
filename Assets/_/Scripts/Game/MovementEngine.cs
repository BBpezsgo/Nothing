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
        public bool IsReverse => rb.velocity.sqrMagnitude > .001f && Vector3.Dot(transform.forward, rb.velocity) < 0f;

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

        Vector3 lastSpeed = Vector3.zero;
        [SerializeField, ReadOnly] Vector3 acceleration = Vector3.zero;
        public Vector3 Acceleration => acceleration;

        protected virtual void Start()
        {
            if (!TryGetComponent(out rb))
            { Debug.LogError($"[{nameof(MovementEngine)}]: {nameof(rb)} is null", this); }
        }

        protected virtual void FixedUpdate()
        {
            Vector3 speed = Velocity;
            Vector3 speedDifference = lastSpeed - speed;
            acceleration = speedDifference / Time.fixedDeltaTime;
            lastSpeed = speed;
        }

        internal virtual float CalculateBrakingDistance()
            => Utilities.Acceleration.DistanceToStop(Velocity.magnitude, Drag);

        internal abstract void DoUserInput();
    }
}
