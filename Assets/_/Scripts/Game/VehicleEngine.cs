using AssetManager;

using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

using Utilities;

public class VehicleEngine : NetworkBehaviour, IHaveAssetFields
{
    [SerializeField, ReadOnly] internal Rigidbody rb;
    [SerializeField, ReadOnly] Unit unit;

#pragma warning disable IDE0052 // Remove unread private members
    /// <summary><b>Only for debugging!</b></summary>
    [SerializeField, ReadOnly] Vector2 input = Vector2.zero;
#pragma warning restore IDE0052 // Remove unread private members

    [Header("Engine")]
    [AssetField] public float moveAccelerationFactor = 30.0f;
    /// <summary>
    /// <code>-1f (Reverse) ... 0f (Stop) ... 1f (Accelerate)</code>
    /// </summary>
    [SerializeField, ReadOnly] float moveAccelerationInput = 0f;
    [SerializeField, AssetField] internal float moveSpeedMax = 50f;
    [SerializeField, AssetField] internal float engineBrake = 2.0f;
    [SerializeField, ReadOnly] internal float savedAccelerationValue;

    [Header("Braking")]
    [SerializeField, AssetField] internal float brake = 3.0f;
    [SerializeField, AssetField] internal float handbrake = 2.5f;
    [SerializeField, ReadOnly] bool isHandbraking = false;
#pragma warning disable IDE0052 // Remove unread private members
    /// <summary><b>Only for debugging!</b></summary>
    [SerializeField, ReadOnly] bool isBraking = false;
#pragma warning restore IDE0052 // Remove unread private members

    [Header("Steer")]
    [SerializeField, AssetField] internal float driftFactor = 0.95f;
    [SerializeField, AssetField] internal float driftFactorWithHandbrake = 0.98f;
    [SerializeField, AssetField] internal bool isHaveTracks = false;
    [SerializeField, AssetField] internal float speedAndSteer = 40f;
    [SerializeField, AssetField] internal float turnFactor = 3.5f;
    [SerializeField, Min(0f), AssetField] internal float steeringSpeed = 1f;
    /// <summary>
    /// <code>-1f (Left) ... 0f (None) ... 1f (Right)</code>
    /// </summary>
    [SerializeField, ReadOnly] float steeringInput = 0f;
    [SerializeField, ReadOnly] internal float steering = 0f;
    [SerializeField, ReadOnly] internal float savedSteeringValue;

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
    [SerializeField, ReadOnly] bool grounded;
    [SerializeField, ReadOnly] float FlippedOverValue;

    #region Properties

    /// <summary>
    /// <b>Steering (X):</b> <br/>
    /// <code>-1f (Left) ... 0f (None) ... 1f (Right)</code> <br/>
    /// <b>Acceleration (Y):</b> <br/>
    /// <code>-1f (Reverse) ... 0f (Stop) ... 1f (Accelerate)</code> <br/>
    /// </summary>
    public Vector2 InputVector
    {
        set
        {
            if (value.x == float.NegativeInfinity || value.x == float.PositiveInfinity || value.x == float.NaN) return;
            if (value.y == float.NegativeInfinity || value.y == float.PositiveInfinity || value.y == float.NaN) return;
            steeringInput = Mathf.Clamp(value.x, -1f, 1f);
            moveAccelerationInput = Mathf.Clamp(value.y, -1f, 1f);
            input = new Vector2(steeringInput, moveAccelerationInput);
        }
        get => new(steeringInput, moveAccelerationInput);
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

    public float LaterialVelocity => Vector3.Dot(transform.right, rb.velocity);
    /// <summary>
    /// Angular velocity in degrees per second
    /// </summary>
    public float AngularVelocity => rb.angularVelocity.y;

    public bool IsBraking
    {
        get
        {
            if (moveAccelerationInput > 0f && IsReverse)
            {
                return true;
            }
            else if (moveAccelerationInput < 0 && !IsReverse)
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

            if (moveAccelerationInput == 0f)
            { return engineBrake; }

            return 0f;
        }
    }

    public bool IsHandbraking
    {
        get => isHandbraking;
        set => isHandbraking = value;
    }

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

    Collider Collider;

    Vector3 engineForceVector;
    Vector3 engineForcePosition;

    [SerializeField, ReadOnly] float NextGroundCheck = .5f;

    #region Mono Callbacks

    void Awake()
    {
        Collider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        unit = GetComponent<Unit>();
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

    void FixedUpdate()
    {
        isBraking = IsBraking;

        if (NextGroundCheck > 0f)
        {
            NextGroundCheck -= Time.fixedDeltaTime;
        }
        else
        {
            NextGroundCheck = 2f;
            grounded = Grounded();
        }

        InWater = Collider.bounds.min.y <= WaterManager.WaterLevel;

        FlippedOverValue = Vector3.Dot(Vector3.up, transform.up);

        engineForceVector = Vector3.zero;
        engineForcePosition = Vector3.zero;

        if (!grounded) return;
        if (FlippedOverValue < .5f) return;

        if (!isHandbraking)
        { ApplyEngineForce(); }
        ApplyBraking();
        KillOrthogonalVelocity();

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

        steering = Mathf.MoveTowards(steering, steeringInput, Time.fixedDeltaTime * steeringSpeed);

        if (isHandbraking)
        {
            float rotateAngle = 0f;
            if (isHaveTracks)
            {
                rotateAngle -= savedSteeringValue;
            }
            else
            {
                float minSpeedBeforeAllowTurningFactor = Mathf.Clamp01(Velocity.magnitude / speedAndSteer);
                rotateAngle -= savedSteeringValue * (minSpeedBeforeAllowTurningFactor);
            }
            rb.Rotate(rotateAngle, transform.up);
        }
        else
        { ApplySteering(); }
    }

    #endregion

    void OnDrawGizmosSelected()
    {
        if (engineForceVector != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(engineForcePosition, engineForcePosition + engineForceVector);
        }

        if (Collider == null) return;

        Gizmos.color = grounded ? Color.green : Color.red;

        var rayOrigin = Collider.bounds.center;
        var raySize = Collider.bounds.size;
        var rayLength = 0.1f;

        Gizmos.DrawWireCube(rayOrigin, raySize);
        Gizmos.DrawWireCube(rayOrigin - (transform.up * rayLength), raySize);

        Gizmos.DrawRay(rayOrigin, -transform.up);
    }

    #region Apply Things

    void ApplyEngineForce()
    {
        // Not pressing the gas pedal
        if (moveAccelerationInput == 0f)
        {
            savedAccelerationValue = 0f;
        }
        // Pressing the gas pedal
        else
        {
            float engineForce = Mathf.Clamp01(FlippedOverValue) * moveAccelerationInput * moveAccelerationFactor * Mathf.Max(0f, 1 - (Speed / moveSpeedMax));
            engineForceVector = transform.forward * engineForce;
            engineForcePosition = transform.position - (.2f * Collider.bounds.extents.y * transform.up);
            rb.AddForceAtPosition(engineForceVector, engineForcePosition, ForceMode.Force);
            savedAccelerationValue = engineForce;
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
        savedSteeringValue = Mathf.Clamp01(FlippedOverValue) * steering * turnFactor;

        float rotateAngle = 0f;
        if (isHaveTracks)
        {
            rotateAngle += savedSteeringValue;
        }
        else
        {
            float minSpeedBeforeAllowTurningFactor = Mathf.Clamp01(Velocity.magnitude / speedAndSteer);
            if (IsReverse)
            {
                rotateAngle -= savedSteeringValue * minSpeedBeforeAllowTurningFactor;
            }
            else
            {
                rotateAngle += savedSteeringValue * minSpeedBeforeAllowTurningFactor;
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
        Vector3 forwardVelocity = transform.forward * Vector3.Dot(rb.velocity, transform.forward);
        Vector3 rightVelocity = transform.right * Vector3.Dot(rb.velocity, transform.right);
        Vector3 finalVelocity = forwardVelocity + rightVelocity * (isHandbraking ? driftFactorWithHandbrake : driftFactor);

        rb.velocity = new Vector3(finalVelocity.x, rb.velocity.y, finalVelocity.z);
    }

    bool IsTireScreeching => isHandbraking || Mathf.Abs(LaterialVelocity) > 15f;

    #endregion

    readonly Collider[] GroundHits = new Collider[1];
    bool Grounded()
    {
        var rayOrigin = Collider.bounds.center - (transform.up * .2f);
        var raySize = Collider.bounds.size;

        int n = Physics.OverlapBoxNonAlloc(
            rayOrigin,
            raySize,
            GroundHits,
            transform.rotation,
            DefaultLayerMasks.JustGround);

        return n > 0;
    }

    internal float CalculateBrakingDistance()
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

    internal void InputRequest(Vector2 input)
        => InputRequest_ServerRpc(input);
    [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
    void InputRequest_ServerRpc(Vector2 input)
    {
        InputVector = input;
    }
}
