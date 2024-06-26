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
        public bool IsReverse => rb.linearVelocity.sqrMagnitude > .001f && Vector3.Dot(transform.forward, rb.linearVelocity) < 0f;

        /// <summary>
        /// Coefficient of drag
        /// </summary>
        public float Drag => rb.linearDamping;

        /// <summary>
        /// Linear velocity in units per second
        /// </summary>
        public Vector3 Velocity => rb.linearVelocity;

        /// <summary>
        /// Angular velocity in degrees per second
        /// </summary>
        public float AngularVelocity => rb.angularVelocity.y;

        Vector3 lastSpeed = default;
        [SerializeField, ReadOnly] Vector3 acceleration = default;
        public Vector3 Acceleration => acceleration;

        protected virtual void Awake()
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
            => Maths.Acceleration.DistanceToStop(Velocity.magnitude, Drag);

        internal abstract void DoUserInput();
    }
}
