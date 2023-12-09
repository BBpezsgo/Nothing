using Unity.Netcode;
using UnityEngine;

namespace Game.Components
{
    public class DroneEngine : MovementEngine
    {
        [SerializeField] float HoveringHeight = 5f;
        [SerializeField] float SpringStrength = 10f;
        [SerializeField] float SpringDamper = 1f;

        [Header("Engine")]
        public float moveAccelerationFactor = 30.0f;
        /// <summary>
        /// <code>-1f (Reverse) ... 0f (Stop) ... 1f (Accelerate)</code>
        /// </summary>
        [SerializeField, ReadOnly] float TorqueInput = 0f;
        [SerializeField] internal float moveSpeedMax = 50f;
        [SerializeField] internal float engineBrake = 2.0f;
        [SerializeField, ReadOnly] internal float SidewaysInput;

        [Header("Braking")]
        [SerializeField] internal float brake = 3.0f;
        [SerializeField] internal float handbrake = 2.5f;
        [SerializeField, ReadOnly] bool isHandbraking = false;

        [Header("Steer")]
        [SerializeField] internal float turnFactor = 3.5f;
        [SerializeField, Min(0f)] internal float steeringSpeed = 1f;
        /// <summary>
        /// <code>-1f (Left) ... 0f (None) ... 1f (Right)</code>
        /// </summary>
        [SerializeField, ReadOnly] float SteeringInput = 0f;
        [SerializeField, ReadOnly] internal float SmoothSteeringInput = 0f;
        [SerializeField, ReadOnly] internal float Steering;

        Collider Collider;

        /// <summary>
        /// <b>Steering (X):</b> <br/>
        /// <code>-1f (Left) ... 0f (None) ... 1f (Right)</code> <br/>
        /// <b>Acceleration (Y):</b> <br/>
        /// <code>-1f (Reverse) ... 0f (Stop) ... 1f (Accelerate)</code> <br/>
        /// </summary>
        public override Vector2 InputVector
        {
            set
            {
                if (value.x == float.NegativeInfinity || value.x == float.PositiveInfinity || value.x == float.NaN) return;
                if (value.y == float.NegativeInfinity || value.y == float.PositiveInfinity || value.y == float.NaN) return;
                SteeringInput = Maths.Clamp(value.x, -1f, 1f);
                TorqueInput = Maths.Clamp(value.y, -1f, 1f);
            }
            get => new(SteeringInput, TorqueInput);
        }

        public Vector3 DroneInputVector
        {
            set
            {
                if (value.x == float.NegativeInfinity || value.x == float.PositiveInfinity || value.x == float.NaN) return;
                if (value.y == float.NegativeInfinity || value.y == float.PositiveInfinity || value.y == float.NaN) return;
                if (value.z == float.NegativeInfinity || value.z == float.PositiveInfinity || value.z == float.NaN) return;
                SteeringInput = Maths.Clamp(value.x, -1f, 1f);
                TorqueInput = Maths.Clamp(value.y, -1f, 1f);
                SidewaysInput = Maths.Clamp(value.z, -1f, 1f);
            }
            get => new(SteeringInput, TorqueInput, SidewaysInput);
        }

        /// <summary>
        /// Absolute value of the speed.
        /// </summary>
        public float Speed => rb.velocity.sqrMagnitude < .5f ? 0f : (transform.forward * Vector3.Dot(transform.forward, rb.velocity)).magnitude;

        /// <summary>
        /// Absolute value of the speed in sideways.
        /// </summary>
        public float SidewaysSpeed => rb.velocity.sqrMagnitude < .5f ? 0f : (transform.right * Vector3.Dot(transform.right, rb.velocity)).magnitude;

        /// <summary>
        /// Value of the speed. Can be negative or positive.
        /// </summary>
        public float SpeedSigned
        {
            get
            {
                if (rb.velocity.sqrMagnitude < .5f) return 0f;
                float dot = Vector3.Dot(transform.forward, rb.velocity);
                return (transform.forward * dot).magnitude * ((dot < 0f) ? -1f : 1f);
            }
        }

        /// <summary>
        /// Value of the speed in sideways. Can be negative or positive.
        /// </summary>
        public float SidewaysSpeedSigned
        {
            get
            {
                if (rb.velocity.sqrMagnitude < .5f) return 0f;
                float dot = Vector3.Dot(transform.right, rb.velocity);
                return (transform.right * dot).magnitude * ((dot < 0f) ? -1f : 1f);
            }
        }

        public float LaterialVelocity => Vector3.Dot(transform.right, rb.velocity);

        public bool IsBraking => Maths.Abs(TorqueInput) <= .0069f && Maths.Abs(SidewaysInput) <= .0069f;

        public float Braking
        {
            get
            {
                if (IsBraking)
                { return BrakeValue; }

                if (isHandbraking)
                { return HandbrakeValue; }

                if (TorqueInput == 0f)
                { return engineBrake; }

                return 0f;
            }
        }

        public override bool IsHandbraking
        {
            get => isHandbraking;
            set => isHandbraking = value;
        }

        public bool IsAutoHandbraking => TorqueInput == 0f;

        /// <summary>
        /// <b>Check if it's really brakeing!!!</b> <see cref="IsBraking"/>
        /// </summary>
        public float BrakeValue => brake;

        /// <summary>
        /// <b>Check if it's really handbrakeing!!!</b> <see cref="IsHandbraking"/>
        /// </summary>
        public float HandbrakeValue => handbrake * (1 - Maths.Abs(Maths.Min(1, LaterialVelocity)));

        #region Mono Callbacks

        protected override void Awake()
        {
            base.Awake();
            if (!TryGetComponent(out Collider))
            { Debug.LogError($"[{nameof(DroneEngine)}]: {nameof(Collider)} is null", this); }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            DoBasicPhysics();

            SmoothSteeringInput = Maths.MoveTowards(SmoothSteeringInput, SteeringInput, Time.fixedDeltaTime * steeringSpeed);

            ApplySteering();
        }

        void DoBasicPhysics()
        {
            if (!isHandbraking)
            { ApplyEngineForce(); }
            ApplyBraking();

            float terrainHeight = TheTerrain.Height(transform.position);

            float offset = terrainHeight - transform.position.y + HoveringHeight;

            Vector3 springDirection = transform.up;
            float springVelocity = Vector3.Dot(springDirection, rb.velocity);
            float springForce = (offset * SpringStrength) - (springVelocity * SpringDamper);

            Vector3 force = default;

            force += springDirection * springForce;

            force += (-rb.velocity.Flatten() * .1f);

            rb.AddForce(force);

            if (InputVector != default)
            {
                rb.drag = 0f;
            }
            else
            {
                rb.drag = 2f;
            }
        }

        #endregion

        #region Apply Things

        void ApplyEngineForce()
        {
            if (TorqueInput != 0f)
            {
                float engineForce = TorqueInput * moveAccelerationFactor * Maths.Max(0f, 1 - (Speed / moveSpeedMax));
                Vector3 engineForceVector = transform.forward * engineForce;
                rb.AddForce(engineForceVector, ForceMode.Force);
            }

            if (SidewaysInput != 0f)
            {
                float engineForce = SidewaysInput * moveAccelerationFactor * Maths.Max(0f, 1 - (SidewaysSpeed / moveSpeedMax));
                Vector3 engineForceVector = transform.right * engineForce;
                rb.AddForce(engineForceVector, ForceMode.Force);
            }
        }

        void ApplyBraking()
        {
            if (IsBraking)
            {
                rb.Drag(BrakeValue / rb.mass);
                return;
            }

            if (isHandbraking)
            {
                rb.Drag(HandbrakeValue / rb.mass);
                return;
            }
        }

        void ApplySteering()
        {
            Steering = SmoothSteeringInput * turnFactor;

            float rotateAngle = Steering;

            rb.Rotate(rotateAngle, transform.up);
        }

        #endregion

        [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
        void InputRequest_ServerRpc(Vector3 input)
        {
            DroneInputVector = input;
            IsHandbraking = false;
        }

        internal override void DoUserInput()
        {
            Vector3 input = default;

            input.z = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");

            var ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);
            float a = Vector2.SignedAngle(transform.forward.To2D(), ray.GetPoint(150f).To2D() - transform.position.To2D());
            input.x = -(a / 90f);

            if (NetcodeUtils.IsOfflineOrServer)
            {
                DroneInputVector = input;
                IsHandbraking = false;
                return;
            }

            if (NetcodeUtils.IsClient && DroneInputVector != input)
            {
                InputRequest_ServerRpc(input);
                return;
            }
        }
    }
}
