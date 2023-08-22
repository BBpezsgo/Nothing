using System;
using System.Collections.Generic;
using AssetManager;
using Game.Managers;
using Unity.Netcode;
using UnityEngine;
using Utilities;

namespace Game.Components
{
    public class VehicleEngine : MovementEngine, IHaveAssetFields, ICopiable<VehicleEngine>
    {
        [SerializeField, ReadOnly] Unit unit;

#pragma warning disable IDE0052 // Remove unread private members
        /// <summary><b>Only for debugging!</b></summary>
        [SerializeField, ReadOnly] Vector2 input = Vector2.zero;
#pragma warning restore IDE0052 // Remove unread private members

        [Serializable]
        public class Wheel
        {
            [SerializeField] internal Transform Transform;
            [SerializeField, Range(0f, 90f)] internal float MaxSteerAngle;
            [SerializeField, Min(0f)] internal float Radius;

            [SerializeField, ReadOnly] Transform _subtransform;
            [SerializeField, ReadOnly] bool _hasSubtransform;

            internal Vector3 Position => Transform.position;
            internal Vector3 Forward => Transform.forward;
            internal Vector3 Up => Transform.up;
            internal Vector3 Right => Transform.right;

            internal float Rotation
            {
                get => Transform.localEulerAngles.y;
                set => Transform.localEulerAngles = new Vector3(0f, value, 0f);
            }

            internal float DistanceFromGround
            {
                get
                {
                    Vector3 position = Transform.position;
                    return position.y - TheTerrain.Height(position);
                }
            }

            internal Transform Subtransform => _subtransform;
            internal bool HasSubtransform => _hasSubtransform;

            internal void OnDrawGizmos()
            {
                if (Transform == null) return;
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(Position, Position - new Vector3(0f, Radius, 0f));
            }

            internal void Init()
            {
                _subtransform = (Transform.childCount == 1) ? Transform.GetChild(0) : null;
                _hasSubtransform = _subtransform != null;
            }
        }

        [SerializeField] float CenterOfMass;

        [Header("Wheels")]
        [SerializeField] Wheel[] Wheels = new Wheel[0];
        [SerializeField] float SpringDamper = 1f;
        [SerializeField] float SpringStrength = 10f;
        [SerializeField, Range(0f, 1f)] float TireGripFactor = 1f;
        [SerializeField] float TireMass = 1f;
        [SerializeField, Min(0)] float EngineForce = 1f;
        [SerializeField, Range(0f, 1f)] float Handbrake = 1f;

        [Header("Engine")]
        [AssetField] public float moveAccelerationFactor = 30.0f;
        /// <summary>
        /// <code>-1f (Reverse) ... 0f (Stop) ... 1f (Accelerate)</code>
        /// </summary>
        [SerializeField, ReadOnly] float TorqueInput = 0f;
        [SerializeField, AssetField] internal float moveSpeedMax = 50f;
        [SerializeField, AssetField] internal float engineBrake = 2.0f;
        [SerializeField, ReadOnly] internal float Torque;

        [Header("Braking")]
        [SerializeField, AssetField] internal float brake = 3.0f;
        [SerializeField, AssetField] internal float handbrake = 2.5f;
        [SerializeField, ReadOnly] bool isHandbraking = false;
#pragma warning disable IDE0052 // Remove unread private members
        /// <summary><b>Only for debugging!</b></summary>
        [SerializeField, ReadOnly] bool isBraking = false;
#pragma warning restore IDE0052 // Remove unread private members

        [Header("Steer")]
        [SerializeField, AssetField, Range(0f, 1f)] internal float driftFactor = 0.95f;
        [SerializeField, AssetField, Range(0f, 1f)] internal float driftFactorWithHandbrake = 0.98f;
        [SerializeField, AssetField] internal bool isHaveTracks = false;
        [SerializeField, AssetField] internal float speedAndSteer = 40f;
        [SerializeField, AssetField] internal float turnFactor = 3.5f;
        [SerializeField, Min(0f), AssetField] internal float steeringSpeed = 1f;
        /// <summary>
        /// <code>-1f (Left) ... 0f (None) ... 1f (Right)</code>
        /// </summary>
        [SerializeField, ReadOnly] float SteeringInput = 0f;
        [SerializeField, ReadOnly] internal float SmoothSteeringInput = 0f;
        [SerializeField, ReadOnly] internal float Steering;

        [Header("Effects")]
        [SerializeField, ReadOnly] bool InWater;

        [SerializeField] internal ParticleSystem waterParticles;
        ParticleSystem.EmissionModule waterParticlesEmission;
        bool HasWaterParticles;
        [SerializeField, ReadOnly] float WaterEmissionRate = 0f;

        [SerializeField] internal ParticleSystem dustParticles;
        ParticleSystem.EmissionModule dustParticlesEmission;
        bool HasDustParticles;
        [SerializeField, ReadOnly] float DustEmissionRate = 0f;

        const float DUST_HANDBRAKING = 30f;
        const float DUST_MIN_LATERIAL_VELOCITY = DUST_HANDBRAKING * 0.5f;
        const float DUST_LATERIAL_VELOCITY_MULTIPLIER = 2f;

        readonly List<TrailRenderer> trailRenderers = new();
        bool HasTrailRenderers = true;
        [SerializeField, ReadOnly] bool TrailEmission = false;
        [SerializeField, ReadOnly] bool IsGrounded;
        [SerializeField, ReadOnly] float FlippedOverValue;

        Collider Collider;

        [SerializeField, ReadOnly] float NextGroundCheck = .5f;

        #region Properties

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
                SteeringInput = Mathf.Clamp(value.x, -1f, 1f);
                TorqueInput = Mathf.Clamp(value.y, -1f, 1f);
                input = new Vector2(SteeringInput, TorqueInput);
            }
            get => new(SteeringInput, TorqueInput);
        }

        /// <summary>
        /// Absolute value of the speed.
        /// </summary>
        public float Speed => rb.velocity.sqrMagnitude < .5f ? 0f : (transform.forward * Vector3.Dot(transform.forward, rb.velocity)).magnitude;

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

        public float LaterialVelocity => Vector3.Dot(transform.right, rb.velocity);

        public bool IsBraking
        {
            get
            {
                if (TorqueInput > 0f && IsReverse)
                {
                    return true;
                }
                else if (TorqueInput < 0 && !IsReverse)
                {
                    return true;
                }
                return false;
            }
        }

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
        public float HandbrakeValue => handbrake * (1 - Mathf.Abs(Mathf.Min(1, LaterialVelocity)));

        bool IsFlippedOver => FlippedOverValue < .5f;

        #endregion

        #region Mono Callbacks

        protected virtual void Awake()
        {
            if (trailRenderers.Count == 0) HasTrailRenderers = false;
            for (int i = 0; i < trailRenderers.Count; i++)
            {
                trailRenderers[i].emitting = false;
            }

            if (dustParticles)
            {
                dustParticlesEmission = dustParticles.emission;
                dustParticlesEmission.rateOverTime = 0f;
                HasDustParticles = true;
            }
            else
            {
                HasDustParticles = false;
            }


            if (waterParticles)
            {
                waterParticlesEmission = waterParticles.emission;
                waterParticlesEmission.rateOverTime = 0f;
                HasWaterParticles = true;
            }
            else
            {
                HasWaterParticles = false;
            }
        }

        protected override void Start()
        {
            base.Start();

            if (!this.TryGetComponentInChildren(out Collider))
            { Debug.LogError($"[{nameof(VehicleEngine)}]: {nameof(Collider)} is null", this); }

            if (!TryGetComponent(out unit))
            { Debug.LogError($"[{nameof(VehicleEngine)}]: {nameof(unit)} is null", this); }

            rb.centerOfMass = new Vector3(0f, rb.centerOfMass.x + CenterOfMass, rb.centerOfMass.z);

            for (int i = 0; i < Wheels.Length; i++)
            { Wheels[i].Init(); }
        }

        void Update()
        {
            if (HasTrailRenderers)
            {
                for (int i = 0; i < trailRenderers.Count; i++)
                { trailRenderers[i].emitting = TrailEmission; }
            }

            if (HasDustParticles)
            {
                DustEmissionRate = Mathf.Lerp(DustEmissionRate, 0, Time.deltaTime * 5);
                dustParticlesEmission.rateOverTime = InWater ? 0f : DustEmissionRate;
            }

            if (HasWaterParticles)
            {
                WaterEmissionRate = Mathf.Lerp(WaterEmissionRate, 0, Time.deltaTime * 5);
                waterParticlesEmission.rateOverTime = InWater ? WaterEmissionRate : 0f;
            }

            if (IsFlippedOver && unit != null && unit.IAmControllingThis() && Input.GetKeyDown(KeyCode.R) && !MenuManager.AnyMenuVisible)
            { FlipBack(); }
        }

        void FlipBack()
        {
            rb.MovePosition(transform.position + new Vector3(0f, 2f, 0f));
            rb.MoveRotation(Quaternion.Euler(0f, rb.rotation.eulerAngles.y, 0f));
            rb.angularVelocity = Vector3.zero;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            InWater = Collider.bounds.min.y <= WaterManager.WaterLevel;

            if (InWater)
            { rb.AddForce(rb.velocity * -0.2f); }

            FlippedOverValue = Vector3.Dot(Vector3.up, transform.up);

            if (FlippedOverValue < .5f) return;

            DoWheelPhysics();

            if (NextGroundCheck > 0f)
            {
                NextGroundCheck -= Time.fixedDeltaTime;
            }
            else
            {
                NextGroundCheck = 2f;
                IsGrounded = CheckGround();
            }

            if (!IsGrounded) return;

            DoEffects();

            if (Wheels.Length > 0)
            { return; }

            DoBasicPhysics();

            SmoothSteeringInput = Mathf.MoveTowards(SmoothSteeringInput, SteeringInput, Time.fixedDeltaTime * steeringSpeed);

            ApplySteering();
        }

        void DoWheelPhysics()
        {
            using (ProfilerMarkers.Wheels.Auto())
            {
                if (Wheels.Length == 0) return;

                Vector3? up = null;
                Vector3? straightForward = null;
                Vector3? straightRight = null;
                Vector3? forward = null;
                Vector3? right = null;

                for (int i = 0; i < Wheels.Length; i++)
                {
                    Wheel wheel = Wheels[i];

                    if (wheel.MaxSteerAngle >= 1f)
                    { wheel.Rotation = SteeringInput * wheel.MaxSteerAngle; }

                    float distanceFromGround = wheel.DistanceFromGround;

                    float springOffset = wheel.Radius - distanceFromGround;

                    if (wheel.HasSubtransform)
                    { wheel.Subtransform.localPosition = new Vector3(0f, Mathf.Clamp(springOffset, 0f, wheel.Radius), 0f); }

                    if (distanceFromGround > wheel.Radius) continue;

                    Vector3 wheelPosition = wheel.Position;

                    Vector3 tireWorldVelocity = rb.GetPointVelocity(wheelPosition);

                    Vector3 force = Vector3.zero;

                    // Spring
                    {
                        up ??= transform.up;

                        Vector3 springDirection = up.Value;
                        float springVelocity = Vector3.Dot(springDirection, tireWorldVelocity);
                        float springForce = (springOffset * SpringStrength) - (springVelocity * SpringDamper);

                        force += springDirection * springForce;
                    }

                    // Steering
                    {
                        Vector3 steeringDirection;
                        if (wheel.MaxSteerAngle == 0f)
                        {
                            straightRight ??= transform.right;
                            steeringDirection = straightRight.Value;
                        }
                        else
                        {
                            right ??= wheel.Right;
                            steeringDirection = right.Value;
                        }

                        float steeringVelocity = Vector3.Dot(steeringDirection, tireWorldVelocity);
                        float desiredVelocityChange = -steeringVelocity * TireGripFactor;
                        float desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;

                        force += desiredAcceleration * TireMass * steeringDirection;
                    }

                    if (isHandbraking || IsAutoHandbraking)
                    {
                        Vector3 steeringDirection;
                        if (wheel.MaxSteerAngle == 0f)
                        {
                            straightForward ??= transform.forward;
                            steeringDirection = straightForward.Value;
                        }
                        else
                        {
                            forward ??= wheel.Forward;
                            steeringDirection = forward.Value;
                        }

                        float steeringVelocity = Vector3.Dot(steeringDirection, tireWorldVelocity);
                        float desiredVelocityChange = -steeringVelocity * Handbrake;
                        float desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;

                        force += desiredAcceleration * TireMass * steeringDirection;
                    }
                    else
                    {
                        Vector3 _forward;
                        if (wheel.MaxSteerAngle == 0f)
                        {
                            straightForward ??= transform.forward;
                            _forward = straightForward.Value;
                        }
                        else
                        {
                            forward ??= wheel.Forward;
                            _forward = forward.Value;
                        }

                        float normalizedSpeed = Mathf.Clamp(Speed / moveSpeedMax, -1, 1);
                        float avaliableTorque = (1f - normalizedSpeed) * EngineForce;

                        force += avaliableTorque * TorqueInput * _forward;
                    }

                    rb.AddForceAtPosition(force, wheelPosition);
                }
            }
        }

        void DoBasicPhysics()
        {
            isBraking = IsBraking;

            if (!isHandbraking)
            { ApplyEngineForce(); }
            ApplyBraking();
            KillOrthogonalVelocity();
        }

        void DoEffects()
        {
            bool isTireScreeching = (HasDustParticles || HasTrailRenderers) && IsTireScreeching;

            if (HasDustParticles && isTireScreeching)
            {
                if (isHandbraking)
                { DustEmissionRate = DUST_HANDBRAKING; }
                else if (LaterialVelocity > DUST_MIN_LATERIAL_VELOCITY)
                { DustEmissionRate = Mathf.Abs(LaterialVelocity) * DUST_LATERIAL_VELOCITY_MULTIPLIER; }
            }

            if (isHaveTracks && HasDustParticles)
            {
                DustEmissionRate = Mathf.Max(DustEmissionRate, Speed * 10f);
            }

            if (HasWaterParticles)
            {
                WaterEmissionRate = Speed * 10f;
            }

            if (HasTrailRenderers)
            {
                TrailEmission = isTireScreeching;
            }
        }

        #endregion

        void OnDrawGizmosSelected()
        {
            for (int i = 0; i < Wheels.Length; i++)
            { Wheels[i].OnDrawGizmos(); }

            if (Collider == null) return;

            Gizmos.color = IsGrounded ? Color.green : Color.red;

            Vector3 rayOrigin = Collider.bounds.center;
            Vector3 raySize = Collider.bounds.size;
            var rayLength = 0.1f;

            Gizmos.DrawWireCube(rayOrigin, raySize);
            Gizmos.DrawWireCube(rayOrigin - (transform.up * rayLength), raySize);

            Gizmos.DrawRay(rayOrigin, -transform.up);
        }

        #region Apply Things

        void ApplyEngineForce()
        {
            // Not pressing the gas pedal
            if (TorqueInput == 0f)
            {
                Torque = 0f;
            }
            // Pressing the gas pedal
            else
            {
                float engineForce = Mathf.Clamp01(FlippedOverValue) * TorqueInput * moveAccelerationFactor * Mathf.Max(0f, 1 - (Speed / moveSpeedMax));
                Vector3 engineForceVector = transform.forward * engineForce;
                Vector3 engineForcePosition = transform.position - (.2f * Collider.bounds.extents.y * transform.up);
                rb.AddForceAtPosition(engineForceVector, engineForcePosition, ForceMode.Force);
                Torque = engineForce;
            }
        }

        void ApplyBraking()
        {
            if (IsBraking)
            {
                rb.AddForce(rb.velocity.normalized * -BrakeValue);
                return;
            }

            if (isHandbraking)
            {
                rb.AddForce(rb.velocity.normalized * -HandbrakeValue);
                return;
            }
        }

        void ApplySteering()
        {
            Steering = Mathf.Clamp01(FlippedOverValue) * SmoothSteeringInput * turnFactor;

            float rotateAngle = 0f;
            if (isHaveTracks)
            {
                rotateAngle += Steering;
            }
            else
            {
                float minSpeedBeforeAllowTurningFactor = Mathf.Clamp01(Velocity.magnitude / speedAndSteer);
                if (IsReverse)
                {
                    rotateAngle -= Steering * minSpeedBeforeAllowTurningFactor;
                }
                else
                {
                    rotateAngle += Steering * minSpeedBeforeAllowTurningFactor;
                }
            }

            rb.Rotate(rotateAngle, transform.up);
        }

        #endregion

        #region Drifting

        /// <summary>
        /// Remove side forces
        /// </summary>
        void KillOrthogonalVelocity()
        {
            /*
            // Get the local velocity of the wheel - where transform is the wheel's transform
            Vector3 localVelocity = transform.TransformDirection(rb.velocity);

            //Remove the velocity from the local axis - x in this case
            localVelocity.x *= (isHandbraking ? driftFactorWithHandbrake : driftFactor);

            //Apply it back to the rigidbody
            rb.velocity = transform.InverseTransformDirection(localVelocity);
            */

            Vector3 velocity = rb.velocity;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            Vector3 forwardVelocity = Vector3.Dot(velocity, forward) * forward;
            Vector3 rightVelocity = (isHandbraking ? driftFactorWithHandbrake : driftFactor) * Vector3.Dot(velocity, right) * right;
            Vector3 finalVelocity = forwardVelocity + rightVelocity;

            rb.velocity = new Vector3(finalVelocity.x, velocity.y, finalVelocity.z);
        }

        bool IsTireScreeching => isHandbraking || Mathf.Abs(LaterialVelocity) > 15f;

        #endregion

        readonly Collider[] GroundHits = new Collider[1];
        bool CheckGround()
        {
            Vector3 rayOrigin = Collider.bounds.center - (transform.up * .2f);
            Vector3 raySize = Collider.bounds.size;

            int n = Physics.OverlapBoxNonAlloc(
                rayOrigin,
                raySize,
                GroundHits,
                transform.rotation,
                DefaultLayerMasks.Solids);

            return n > 0;
        }

        internal override float CalculateBrakingDistance()
        {
            float speed = this.Speed;

            if (Mathf.Abs(speed) <= 1f)
            { return 0f; }

            float brake = this.Braking;
            if (brake == 0f) brake = this.HandbrakeValue;

            brake /= rb.mass;

            if (brake == 0f) brake = this.engineBrake;

            float brakingDistance = Mathf.Abs(Utilities.Acceleration.DistanceToReachVelocity(speed, 0f, brake * -20f));
            if (this.IsReverse) brakingDistance = -brakingDistance;
            return brakingDistance;
        }

        [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
        void InputRequest_ServerRpc(Vector2 input, bool handbrake)
        {
            InputVector = input;
            isHandbraking = handbrake;
        }

        internal override void DoUserInput()
        {
            Vector2 input = Vector2.zero;

            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");
            bool handbrake = Input.GetKey(KeyCode.Space);

            if (NetcodeUtils.IsOfflineOrServer)
            {
                InputVector = input;
                isHandbraking = handbrake;
                return;
            }

            if (NetcodeUtils.IsClient && InputVector != input)
            {
                InputRequest_ServerRpc(input, handbrake);
                return;
            }
        }

        public void CopyTo(object destination) => this.CopyTo<VehicleEngine>(destination);
        public void CopyTo(VehicleEngine destination)
        {
            destination.brake = brake;
            destination.CenterOfMass = CenterOfMass;
            destination.Collider = Collider;
            destination.driftFactor = driftFactor;
            destination.driftFactorWithHandbrake = driftFactorWithHandbrake;
            destination.DustEmissionRate = DustEmissionRate;
            destination.dustParticles = dustParticles;
            destination.dustParticlesEmission = dustParticlesEmission;
            destination.engineBrake = engineBrake;
            destination.EngineForce = EngineForce;
            destination.FlippedOverValue = FlippedOverValue;
            destination.Handbrake = Handbrake;
            destination.handbrake = handbrake;
            destination.HasDustParticles = HasDustParticles;
            destination.HasTrailRenderers = HasTrailRenderers;
            destination.HasWaterParticles = HasWaterParticles;
            destination.input = input;
            destination.InWater = InWater;
            destination.isBraking = isBraking;
            destination.IsGrounded = IsGrounded;
            destination.isHandbraking = isHandbraking;
            destination.isHaveTracks = isHaveTracks;
            destination.moveAccelerationFactor = moveAccelerationFactor;
            destination.moveSpeedMax = moveSpeedMax;
            destination.NextGroundCheck = NextGroundCheck;
            destination.rb = rb;
            destination.SmoothSteeringInput = SmoothSteeringInput;
            destination.speedAndSteer = speedAndSteer;
            destination.SpringDamper = SpringDamper;
            destination.SpringStrength = SpringStrength;
            destination.Steering = Steering;
            destination.SteeringInput = SteeringInput;
            destination.steeringSpeed = steeringSpeed;
            destination.TireGripFactor = TireGripFactor;
            destination.TireMass = TireMass;
            destination.Torque = Torque;
            destination.TorqueInput = TorqueInput;
            destination.TrailEmission = TrailEmission;
            destination.turnFactor = turnFactor;
            destination.unit = unit;
            destination.WaterEmissionRate = WaterEmissionRate;
            destination.waterParticles = waterParticles;
            destination.waterParticlesEmission = waterParticlesEmission;
            destination.Wheels = Wheels;
        }
    }
}
