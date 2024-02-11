using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Utilities;
using System.Diagnostics.CodeAnalysis;


#nullable enable

#if UNITY_EDITOR
using UnityEditor;
[CustomPropertyDrawer(typeof(Game.Components.TurretInput))]
public class TurretInputDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        float turretAngle = Maths.Round(property.FindPropertyRelative("TurretAngle").floatValue);
        float cannonAngle = Maths.Round(property.FindPropertyRelative("CannonAngle").floatValue);

        EditorGUI.SelectableLabel(position, $"Turret: {turretAngle}° Cannon: {cannonAngle}°");

        EditorGUI.EndProperty();
    }
}
#endif

namespace Game.Components
{
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
    public struct TurretInput : IEquatable<TurretInput>
    {
        [ReadOnly] public float TurretAngle;
        [ReadOnly] public float CannonAngle;

        public TurretInput(float turretAngle, float cannonAngle)
        {
            TurretAngle = turretAngle;
            CannonAngle = cannonAngle;
        }

        public override readonly bool Equals(object obj) => obj is TurretInput input && Equals(input);

        public readonly bool Equals(TurretInput other) =>
            TurretAngle == other.TurretAngle &&
            CannonAngle == other.CannonAngle;

        public override readonly int GetHashCode() => HashCode.Combine(TurretAngle, CannonAngle);

        public static bool operator ==(TurretInput left, TurretInput right) => left.Equals(right);
        public static bool operator !=(TurretInput left, TurretInput right) => !(left == right);

        public static implicit operator ValueTuple<float, float>(TurretInput v) => (v.TurretAngle, v.CannonAngle);
        public static implicit operator Vector2(TurretInput v) => new(v.TurretAngle, v.CannonAngle);
        public static implicit operator TurretInput(ValueTuple<float, float> v) => new(v.Item1, v.Item2);
        public static implicit operator TurretInput(Vector2 v) => new(v.x, v.y);

        public override readonly string ToString() => $"(Turret: {TurretAngle}° Cannon: {CannonAngle}°)";
    }

    public class Turret : NetworkBehaviour
    {
        readonly struct BurstParticles
        {
            readonly ParticleSystem ParticleSystem;
            readonly int BurstCount;
            readonly float Probability;

            public BurstParticles(ParticleSystem particleSystem)
            {
                if (particleSystem == null) throw new ArgumentNullException(nameof(particleSystem));

                ParticleSystem = particleSystem;
                ParticleSystem.Burst burst = particleSystem.emission.GetBurst(0);
                ParticleSystem.MinMaxCurve count = burst.count;
                BurstCount = count.mode switch
                {
                    ParticleSystemCurveMode.Constant => Maths.RoundToInt(count.constant),
                    ParticleSystemCurveMode.TwoConstants => Maths.RoundToInt((count.constantMin + count.constantMax) / 2f),
                    _ => 0,
                };
                Probability = particleSystem.emission.GetBurst(0).probability;
            }

            internal readonly void Emit()
            {
                if (Probability != 1f && UnityEngine.Random.value > Probability) return;
                ParticleSystem.Emit(BurstCount);
            }
        }

        enum CannonKnockbackStates
        {
            Still,
            Knockback,
            Restore,
        }

        [SerializeField, Min(0f)] float Knockback = 1f;
        [SerializeField, Min(0f)] float CannonKnockback = 0f;
        [SerializeField, Min(0f)] float CannonKnockbackRestoreSpeed = 1f;
        [SerializeField, Min(0f)] float CannonKnockbackSpeed = 1f;
        [SerializeField, ReadOnly] (float original, float current, float target) CannonKnockbackPosition;
        [SerializeField] bool UseBarrelInstead;
        [SerializeField, ReadOnly] CannonKnockbackStates CannonKnockbackState;
        [SerializeField, ReadOnly] Vector3 CannonOriginalLocalPosition;

        Transform? KnockbackTransform => UseBarrelInstead ? Barrel : cannon;

        [Header("Sound")]
        [SerializeField] AudioSource? AudioSource;
        [SerializeField] AudioClip? ShootSound;

        [Header("Barrel")]
        [SerializeField] public Transform? Barrel;
        [SerializeField, ReadOnly] bool HasBarrel;
        [SerializeField] public float RequiedBarrelRotationSpeed = 0f;
        [SerializeField] public float BarrelRotationAcceleration = 0f;
        [SerializeField, ReadOnly] float BarrelRotationSpeed = 0f;
        [SerializeField, ReadOnly] public bool PrepareShooting = false;

        [Header("Cannon")]
        [SerializeField] public Transform? cannon;
        [SerializeField] public float cannonRotationSpeed = 1f;
        [SerializeField] public float cannonLowestAngle;
        [SerializeField] public float cannonHighestAngle;

        [Header("Turret")]
        [SerializeField] public float rotationSpeed = 5f;

        [Header("Reload")]
        [SerializeField] public float reloadTime = 1f;
        [SerializeField, ReadOnly] float reload;

        public float CurrentReload => reload;
        public float ReloadPercent => (reloadTime <= 0f) ? 1f : (1f - Maths.Clamp01(reload / reloadTime));

        [Header("Projectile")]
        [SerializeField] public float projectileVelocity = 100f;
        [Tooltip("-1 means infinity")]
        [SerializeField] public float ProjectileLifetime = -1f;
        [Tooltip("Used by timer projectiles that explode in mid-air")]
        [SerializeField, ReadOnly] public float RequiredProjectileLifetime = -1f;
        [SerializeField] public List<Transform> projectileIgnoreCollision = new();
        [SerializeField] public Transform shootPosition = null!;
        [SerializeField] public GameObject projectile = null!;
        PooledObject? PooledProjectile;
        [SerializeField] public GameObject[] Projectiles = null!;
        PooledObject[] PooledProjectiles = null!;
        [SerializeField] float Randomness = 0f;
        [SerializeField] Vector2Int BulletCount = Vector2Int.one;

        [SerializeField] ParticleSystem? Shells;

        public float CurrentProjectileLifetime
        {
            get
            {
                if (RequiredProjectileLifetime == -1f) return ProjectileLifetime;
                if (ProjectileLifetime == -1f) return RequiredProjectileLifetime;
                return Maths.Min(ProjectileLifetime, RequiredProjectileLifetime);
            }
        }

        public PooledObject? CurrentProjectile
        {
            get
            {
                if (PooledProjectile != null) return PooledProjectile;
                if (SelectedProjectile < 0 || SelectedProjectile >= PooledProjectiles.Length) return null;
                return PooledProjectiles[SelectedProjectile];
            }
        }

        [SerializeField, ReadOnly] public int SelectedProjectile = 0;
        [SerializeField] public GameObject[] ShootEffects = null!;
        [SerializeField, ReadOnly] BurstParticles[] ShootEffectInstances = null!;
        [SerializeField, ReadOnly] bool IsBallisticProjectile = true;

        [Header("Scope")]
        [SerializeField] public Transform? Scope;
        [SerializeField] public Transform? ScopeHolder;

        [Header("Other")]
        [SerializeField, ReadOnly] Vector3 target;
        readonly NetworkVariable<Vector3> netTarget = new();
        [SerializeField, ReadOnly] Transform? targetTransform;
        [SerializeField, ReadOnly] TurretInput overriddenInput = (0f, 0f);

        [SerializeField, ReadOnly] public BaseObject @base = null!;

        public Transform? TargetTransform => targetTransform;

        public TurretInput Input
        {
            get => input;
            set => overriddenInput = value;
        }

        [SerializeField, ReadOnly] TurretInput input = (0f, 0f);

        [SerializeField, ReadOnly] public float Range;

        readonly List<(Projectile, Vector3)> shots = new();

        public Vector3 ShootPosition => shootPosition.position;

        [SerializeField] bool ShowRadius;

        public Vector3 TargetPosition
        {
            get
            {
                if (targetTransform == null)
                {
                    if (NetcodeUtils.IsClient)
                    { return netTarget.Value; }
                    else
                    { return target; }
                }
                else
                { return targetTransform.position; }
            }
        }

        public void SetTarget(Vector3 target)
        {
            targetTransform = null;
            this.target = target;

            if (NetcodeUtils.IsServer)
            { netTarget.Value = target; }
        }

        public void SetTarget(Transform target)
        {
            targetTransform = target;
            this.target = target.position;

            if (NetcodeUtils.IsServer)
            { netTarget.Value = target.position; }
        }

        public float TurretLocalRotation
        {
            get => transform.localEulerAngles.y;
            set
            {
                if (cannon == null)
                { transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, value, 0f); }
                else
                { transform.localEulerAngles = new Vector3(0f, value, 0f); }
            }
        }
        public float CannonLocalRotation
        {
            get
            {
                if (cannon == null)
                { return -transform.localEulerAngles.x; }
                else
                { return -cannon.transform.localEulerAngles.x; }
            }
            set
            {
                if (cannon == null)
                { transform.localEulerAngles = new Vector3(-value, transform.localEulerAngles.y, 0f); }
                else
                { cannon.transform.localEulerAngles = new Vector3(-value, 0f, 0f); }
            }
        }

        public float TurretRotation
        {
            get => transform.eulerAngles.y;
            set
            {
                if (cannon == null)
                { transform.eulerAngles = new Vector3(transform.eulerAngles.x, value, 0f); }
                else
                { transform.eulerAngles = new Vector3(0f, value, 0f); }
            }
        }
        public float CannonRotation
        {
            get
            {
                if (cannon == null)
                { return -transform.eulerAngles.x; }
                else
                { return -cannon.transform.eulerAngles.x; }
            }
            set
            {
                if (cannon == null)
                { transform.eulerAngles = new Vector3(-value, transform.eulerAngles.y, 0f); }
                else
                { cannon.transform.eulerAngles = new Vector3(-value, 0f, 0f); }
            }
        }

        public float ShootHeight => shootPosition.position.y;
        public bool IsAccurateShoot => _error <= MaxError;
        public bool OutOfRange => _outOfRange;

        [SerializeField, ReadOnly] float _error = 1f;
        [SerializeField, ReadOnly] bool _outOfRange;

        [SerializeField] float MaxError = 0.001f;

        Vector3 predictedOffset;
        public Vector3 PredictedOffset => predictedOffset;

        float CannonRotationFix => -(90f - Vector3.SignedAngle(transform.forward, Vector3.up, transform.forward));

        [SerializeField] bool ResetAfterTime;
        [SerializeField, Min(0f)] float TimeToReset = 1f;
        [SerializeField, ReadOnly] float IdleTime;

        void Start()
        {
            if (projectile != null)
            {
                PooledProjectile = ObjectPool.Instance.GeneratePool(projectile);
            }

            if (Projectiles.Length > 0)
            {
                PooledProjectiles = new PooledObject[Projectiles.Length];
                for (int i = 0; i < Projectiles.Length; i++)
                {
                    PooledProjectiles[i] = ObjectPool.Instance.GeneratePool(Projectiles[i]);
                }
            }

            targetTransform = null;
            if (ShootEffects != null)
            {
                List<BurstParticles> burstParticles = new(ShootEffects.Length);
                for (int i = 0; i < ShootEffects.Length; i++)
                {
                    GameObject newParticleSystem = GameObject.Instantiate(ShootEffects[i], shootPosition.position, shootPosition.rotation, (cannon == null) ? transform : cannon);
                    // burstParticles.Add(new BurstParticles(newParticleSystem));
                    ParticleSystem[] all = newParticleSystem.GetComponentsInChildren<ParticleSystem>(false);
                    for (int j = 0; j < all.Length; j++)
                    {
                        burstParticles.Add(new BurstParticles(all[j]));
                    }
                }
                ShootEffectInstances = burstParticles.ToArray();
            }
            else
            {
                ShootEffectInstances = new BurstParticles[0];
            }

            IsBallisticProjectile = CurrentProjectile != null && CurrentProjectile.TryGetComponent(out Rigidbody projectileRigidbody) && projectileRigidbody.useGravity;

            Range = GetRange();

            HasBarrel = Barrel != null;

            if (CannonKnockback != 0f)
            {
                if (KnockbackTransform == null)
                {
                    CannonKnockback = 0f;
                    Debug.LogWarning($"[{nameof(Turret)}]: {nameof(CannonKnockback)} is set but {nameof(KnockbackTransform)} is null", this);
                }
                else
                {
                    CannonKnockbackPosition = (0f, 0f, 0f);
                    CannonOriginalLocalPosition = KnockbackTransform.localPosition;
                }
            }
        }

        void Update()
        {
            if (reload > 0f) reload -= Time.deltaTime;

            if (HasBarrel)
            {
                if (BarrelRotationSpeed != 0)
                { Barrel!.localEulerAngles = new Vector3(0f, 0f, Barrel.localEulerAngles.z + (BarrelRotationSpeed * Time.deltaTime)); }

                if (PrepareShooting)
                {
                    if (BarrelRotationSpeed < RequiedBarrelRotationSpeed)
                    {
                        BarrelRotationSpeed = Maths.Min(BarrelRotationSpeed + (BarrelRotationAcceleration * Time.deltaTime), RequiedBarrelRotationSpeed);
                    }
                }
                else
                {
                    if (BarrelRotationSpeed > 0)
                    {
                        BarrelRotationSpeed = Maths.Max(BarrelRotationSpeed - (BarrelRotationAcceleration * Time.deltaTime), 0);
                    }
                }
            }

            if (CannonKnockback != 0f && KnockbackTransform != null)
            {
                switch (CannonKnockbackState)
                {
                    case CannonKnockbackStates.Still:

                        if (CannonKnockbackPosition.current != CannonKnockbackPosition.target)
                        {
                            CannonKnockbackPosition.current = Maths.MoveTowards(CannonKnockbackPosition.current, CannonKnockbackPosition.target, CannonKnockbackRestoreSpeed * Time.deltaTime);
                        }

                        break;
                    case CannonKnockbackStates.Knockback:

                        if (CannonKnockbackPosition.current == CannonKnockbackPosition.target)
                        {
                            CannonKnockbackState = CannonKnockbackStates.Restore;
                            CannonKnockbackPosition.target = CannonKnockbackPosition.original;
                        }
                        else
                        {
                            CannonKnockbackPosition.current = Maths.MoveTowards(CannonKnockbackPosition.current, CannonKnockbackPosition.target, CannonKnockbackSpeed * Time.deltaTime);
                        }

                        break;
                    case CannonKnockbackStates.Restore:

                        if (CannonKnockbackPosition.current == CannonKnockbackPosition.target)
                        {
                            CannonKnockbackState = CannonKnockbackStates.Still;
                            CannonKnockbackPosition.target = CannonKnockbackPosition.original;
                        }
                        else
                        {
                            CannonKnockbackPosition.current = Maths.MoveTowards(CannonKnockbackPosition.current, CannonKnockbackPosition.target, CannonKnockbackRestoreSpeed * Time.deltaTime);
                        }

                        break;
                }

                KnockbackTransform.localPosition = CannonOriginalLocalPosition - Vector3.forward * CannonKnockbackPosition.current;
            }

            Vector3 targetPosition = TargetPosition;

            if (targetPosition == default)
            {
                predictedOffset = default;
                _error = 1f;
                _outOfRange = false;
                if (ResetAfterTime)
                {
                    if (IdleTime > TimeToReset)
                    {
                        RotateTurret();
                        RotateCannon();
                    }
                    else
                    {
                        IdleTime += Time.deltaTime;
                    }
                }
            }
            else
            {
                IdleTime = 0f;

                if (overriddenInput != default)
                {
                    predictedOffset = default;
                    input = overriddenInput;
                }
                else
                {
                    Vector3 targetVelocity = default;
                    Vector3 targetAcceleration = default;
                    if (targetTransform != null && targetTransform.gameObject.TryGetComponent(out Rigidbody targetRigidbody))
                    {
                        targetVelocity = targetRigidbody.velocity;
                        if (targetTransform != null && targetTransform.gameObject.TryGetComponent(out MovementEngine movementEngine))
                        { targetAcceleration = movementEngine.Acceleration; }
                    }
                    input = Aim(targetPosition, targetVelocity, targetAcceleration, out _outOfRange);
                }

                _error = CalculateError(input);

                RotateTurret(input.TurretAngle);
                RotateCannon(input.CannonAngle);
            }

            // Debug.DrawLine(ShootPosition, transform.position + transform.forward * 150f, Color.yellow, Time.deltaTime);
            // Debug.DrawLine(ShootPosition, ShootPosition + cannon.forward * 150f, Color.red, Time.deltaTime);
        }

        TurretInput Aim(Vector3 targetPosition, Vector3 targetVelocity, Vector3 targetAcceleration, out bool outOfRange)
        {
            Vector2 selfGround = transform.position.To2D();
            float cannonLength = Maths.Distance(selfGround, shootPosition.position.To2D());

            targetPosition += Vector2.ClampMagnitude(selfGround - targetPosition.To2D(), cannonLength).To3D();

            if (IsBallisticProjectile)
            {
                if (targetVelocity.To2D().sqrMagnitude > .1f)
                {
                    Vector2 offset;
                    /*
                    Vector2? ballisticOffset = Ballistics.CalculateInterceptCourse(ShootPosition, projectileVelocity, targetPosition, targetVelocity); ;

                    if (ballisticOffset.HasValue)
                    { offset = ballisticOffset.Value; }
                    else*/
                    { offset = Velocity.CalculateInterceptCourse(targetPosition.To2D(), targetVelocity.To2D(), selfGround, projectileVelocity); }

                    Vector3 offset3 = offset.To3D();
                    predictedOffset = offset3;

                    targetPosition += offset3;
                }
                else
                { predictedOffset = default; }

                // Debug.DrawLine(transform.position, targetPosition, Color.green, Time.deltaTime);
            }
            else
            {
                if (targetVelocity.To2D().sqrMagnitude > .1f)
                {
                    Vector2 offset = Acceleration.CalculateInterceptCourse(targetPosition.To2D(), targetVelocity.To2D(), selfGround, projectileVelocity, projectile.GetComponent<Projectile>().Acceleration);
                    predictedOffset = offset.To3D();

                    targetPosition += offset.To3D();
                }
                else
                { predictedOffset = default; }
            }

            return Aim(targetPosition, out outOfRange);
        }

        TurretInput Aim(Vector3 targetPosition, out bool outOfRange)
        {
            float turretRotation;
            float cannonAngle;
            outOfRange = false;

            // turretRotation = Maths.Vector3AngleOnPlane(targetPosition, transform.position, -transform.up, transform.forward);

            // cannonAngle = Vector3.Angle(targetPosition, cannon.up);
            // cannonAngle = -(90f - cannonAngle);

            Vector2 selfGround = transform.position.To2D();
            float cannonLength = Maths.Distance(selfGround, shootPosition.position.To2D());

            targetPosition += Vector2.ClampMagnitude(selfGround - targetPosition.To2D(), cannonLength).To3D();

            if (IsBallisticProjectile)
            {
                float targetAngle;

                float? theta_;

                using (Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
                { theta_ = Ballistics.AngleOfReach2(projectileVelocity, shootPosition.position, targetPosition); }

                if (!theta_.HasValue)
                {
                    targetAngle = 45f;
                    outOfRange = true;
                }
                else
                { targetAngle = theta_.Value * Maths.Rad2Deg; }

                Vector3 directionToTarget = Vector3.Normalize(targetPosition - transform.position);
                if (cannon != null)
                {
                    directionToTarget = Vector3.ProjectOnPlane(directionToTarget, cannon.up);
                    targetAngle += CannonRotationFix;
                }

                turretRotation = Quaternion.LookRotation(directionToTarget).eulerAngles.y;

                cannonAngle = Maths.Clamp(targetAngle, -Maths.Abs(cannonLowestAngle), Maths.Abs(cannonHighestAngle));
            }
            else
            {
                Vector3 rotationToTarget = Quaternion.LookRotation(targetPosition - shootPosition.position).eulerAngles;

                turretRotation = rotationToTarget.y;

                cannonAngle = -rotationToTarget.x;
                cannonAngle += CannonRotationFix;
            }

            // transform.Rotate(new Vector3(0f, turretRotation, 0f), Space.Self);
            // cannon.Rotate(new Vector3(cannonAngle, 0f, 0f), Space.Self);

            return (turretRotation, cannonAngle);
        }

        float CalculateError(Vector2 input)
        {
            float error = 0f;
            error += Maths.Abs(input.x - transform.rotation.eulerAngles.y);
            error = GeneralUtils.NormalizeAngle360(error);
            error += Maths.Abs(input.y - CannonLocalRotation);
            error = GeneralUtils.NormalizeAngle360(error);
            error *= .5f;
            error /= 360f;
            return error;
        }

        public Vector3? PredictImpact()
            => PredictImpact(out _);
        public Vector3? PredictImpact(out bool outOfRange)
        {
            outOfRange = false;
            if (!IsBallisticProjectile)
            {
                return (projectileVelocity * CurrentProjectileLifetime * shootPosition.forward) + shootPosition.position;
            }
            else
            {
                using (Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
                { return Ballistics.PredictImpact(shootPosition, projectileVelocity, CurrentProjectileLifetime, out outOfRange); }
            }
        }

        public float? ImpactTime()
        {
            if (this.CurrentProjectile == null)
            { return null; }
            if (!this.CurrentProjectile.TryGetComponent(out Projectile projectile))
            { return null; }

            float v = this.projectileVelocity * .95f;

            Vector3? _impact = PredictImpact();
            if (!_impact.HasValue)
            { return null; }

            Vector3 impact = _impact.Value;

            float d = Maths.Distance(ShootPosition, impact);

            float? t;

            if (IsBallisticProjectile)
            {
                using (Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
                { t = Ballistics.TimeToReachDistance(v, CannonLocalRotation * Maths.Deg2Rad, d); }
            }
            else
            {
                float a = projectile.Acceleration;

                if (a == 0f)
                {
                    t = Velocity.CalculateTime(d, v);
                }
                else
                {
                    float maxV = Acceleration.SpeedAfterDistance(v, a, d);
                    t = Acceleration.TimeToReachVelocity(v, maxV, a);
                }
            }

            if (!t.HasValue) return null;
            else if (projectile.Lifetime > 0f) return Maths.Min(t.Value, projectile.Lifetime);
            else return t.Value;
        }
        internal bool TryGetTrajectory(out Ballistics.Trajectory trajectory)
        {
            trajectory = default;

            if (!IsBallisticProjectile)
            { return false; }

            trajectory = new Ballistics.Trajectory(CannonLocalRotation, transform.rotation.eulerAngles.y, projectileVelocity, shootPosition.position);
            return true;
        }

        void RotateTurretInstant(float to)
        {
            TurretLocalRotation = to;
        }
        void RotateTurret(float to)
        {
            TurretLocalRotation = Maths.MoveTowardsAngle(TurretLocalRotation, to - transform.parent.localEulerAngles.y, rotationSpeed * Time.deltaTime);
        }
        void RotateTurret()
        {
            TurretLocalRotation = Maths.MoveTowardsAngle(TurretLocalRotation, 0f, rotationSpeed * Time.deltaTime);
        }

        void RotateCannon(float to)
        {
            /*
            float min = Maths.Min(cannonLowestAngle, cannonHighestAngle);

            float max = Maths.Max(cannonLowestAngle, cannonHighestAngle);

            float newNewAngle = Utilities.Utils.ModularClamp(newAngle, cannonLowestAngle, cannonHighestAngle); // Maths.Clamp(Utils.NormalizeAngle(newAngle), Utils.NormalizeAngle(min), Utils.NormalizeAngle(max));
            */

            CannonLocalRotation = Maths.MoveTowardsAngle(CannonLocalRotation, to, cannonRotationSpeed * Time.deltaTime);
        }
        void RotateCannonInstant(float to)
        {
            CannonLocalRotation = to;
        }
        void RotateCannon()
        {
            CannonLocalRotation = Maths.MoveTowardsAngle(CannonLocalRotation, 0f, cannonRotationSpeed * Time.deltaTime);
        }

        public bool Shoot()
        {
            float? t = ImpactTime();
            if (targetTransform != null &&
                targetTransform.TryGetComponent(out RequiredShoots? requiredShoots))
            { return Shoot(requiredShoots, t ?? -1f); }
            else
            { return Shoot(null, t ?? -1f); }
        }

        public bool Shoot(RequiredShoots? requiredShoots)
        {
            float? t = ImpactTime();
            return Shoot(requiredShoots, t ?? -1f);
        }

        public bool Shoot(RequiredShoots? requiredShoots, float impactTime)
            => ShootInternal(requiredShoots, impactTime, UnityEngine.Random.Range(BulletCount.x, BulletCount.y));

        bool ShootInternal(RequiredShoots? requiredShoots, float impactTime, int bulletCount)
        {
            if (reload > 0f)
            { return false; }

            if (CurrentProjectile == null)
            { return false; }

            if (Barrel != null &&
                BarrelRotationSpeed < RequiedBarrelRotationSpeed)
            { return false; }

            bool instantiatedAnyProjectile = false;
            for (int i = 0; i < bulletCount; i++)
            {
                GameObject? newProjectile = CurrentProjectile?.Instantiate(shootPosition.position, shootPosition.rotation, ObjectGroups.Projectiles);

                if (newProjectile == null)
                { continue; }

                instantiatedAnyProjectile = true;

                if (newProjectile.TryGetComponent(out Rigidbody rb))
                {
                    Vector3 velocityResult = shootPosition.forward * projectileVelocity;

                    if (Randomness > 0f)
                    {
                        Vector3 randomRotation = UnityEngine.Random.insideUnitSphere * Randomness;
                        velocityResult += randomRotation;
                    }

                    rb.velocity = velocityResult;
                }

                if (newProjectile.TryGetComponent(out Projectile _projectile))
                {
                    _projectile.TargetPosition = TargetPosition;

                    _projectile.lastPosition = shootPosition.position;

                    _projectile.ignoreCollision = projectileIgnoreCollision.ToArray();
                    _projectile.OwnerTeamHash = @base.TeamHash;
                    _projectile.Owner = @base;

                    _projectile.LifeLeft = CurrentProjectileLifetime;
                    _projectile.InfinityLifetime = ProjectileLifetime <= 0f;

                    _projectile.Shot = new Ballistics.Trajectory(CannonLocalRotation, transform.rotation.eulerAngles.y, projectileVelocity, shootPosition.position);

                    Vector3 predictedImpactPosition = PredictImpact() ?? TargetPosition;

                    shots.Add((_projectile, predictedImpactPosition));

                    if (IsAccurateShoot &&
                        requiredShoots != null)
                    {
                        float predictedDamage = 0f;

                        predictedDamage += _projectile.ImpactDamage;
                        predictedDamage += _projectile.ExploisonDamage * .2f;

                        requiredShoots.Shoot(impactTime, predictedDamage);
                    }
                }
            }

            if (!instantiatedAnyProjectile)
            { return false; }

            if (Shells != null && QualityHandler.EnableParticles)
            { Shells.Emit(1); }

            if (IsServer)
            { OnShoot_ClientRpc(new Vector2(TurretLocalRotation, CannonLocalRotation), bulletCount); }

            if (CannonKnockback != 0f)
            {
                CannonKnockbackPosition.target = CannonKnockbackPosition.original + CannonKnockback;
                CannonKnockbackState = CannonKnockbackStates.Knockback;
            }

            if (@base.TryGetComponent(out Rigidbody baseRigidbody))
            { baseRigidbody.AddForceAtPosition((cannon == null ? transform : cannon).forward * -Knockback, transform.position, ForceMode.Impulse); }

            if (AudioSource != null &&
                ShootSound != null)
            { AudioSource.PlayOneShot(ShootSound); }

            reload = reloadTime;

            if (QualityHandler.EnableParticles)
            {
                for (int i = 0; i < ShootEffectInstances.Length; i++)
                { ShootEffectInstances[i].Emit(); }
            }

            return true;
        }

        public void NotifyImpact(Projectile projectile, Vector3 at)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                if (shots[i].Item1 != projectile) continue;
                // Vector3 error = at - shots[i].Item2;
                shots.RemoveAt(i);
            }
        }

        public void NotifyDamage(Projectile projectile, (Vector3 Position, float Amount, DamageKind Kind)[] damages)
        {
            if (@base is ICanTakeControl canTakeControl)
            { canTakeControl.OnDamagedSomebody?.Invoke(damages); }
        }

        public float GetRange()
        {
            float range;
            using (Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
            {
                range = Ballistics.MaxRadius(projectileVelocity, ShootHeight);
                if (ProjectileLifetime > 0f)
                { range = Maths.Min(range, Ballistics.DisplacementX(45f * Maths.Deg2Rad, projectileVelocity, ProjectileLifetime)); }
            }
            this.Range = range;
            return range;
        }

        void OnDrawGizmos()
        {
            if (ShowRadius)
            {
                float? r2 = Ballistics.CalculateX(CannonLocalRotation * Maths.Deg2Rad, projectileVelocity, ShootHeight);
                if (r2.HasValue)
                {
                    Gizmos.color = CoolColors.White;
                    Gizmos.DrawWireSphere(shootPosition.position, r2.Value);
                }

                float maxR = GetRange();

                Gizmos.color = CoolColors.Red;
                Gizmos.DrawWireSphere(shootPosition.position, maxR);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (TargetPosition != default)
            {
                Gizmos.color = CoolColors.Red;
                GizmosPlus.DrawPoint(TargetPosition, 1f);
                Debug3D.Label(TargetPosition, "Target");
            }
        }

        [ClientRpc]
        void OnShoot_ClientRpc(Vector2 input, int bulletCount)
        {
            this.input = input;
            _error = CalculateError(input);
            _outOfRange = false;

            RotateTurretInstant(input.x);
            RotateCannonInstant(input.y);
            reload = 0f;
            ShootInternal(null, -1f, bulletCount);
        }

        internal void ShootRequest()
            => ShootRequest_ServerRpc(new Vector2(TurretLocalRotation, CannonLocalRotation));

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        void ShootRequest_ServerRpc(Vector2 input)
        {
            this.input = input;
            _error = CalculateError(input);
            _outOfRange = false;

            TurretLocalRotation = input.x;
            CannonLocalRotation = input.y;
            Shoot();
        }

        internal void TargetRequest(Vector3 point)
            => TargetRequest_ServerRpc(point);

        [ServerRpc(Delivery = RpcDelivery.Unreliable, RequireOwnership = false)]
        void TargetRequest_ServerRpc(Vector3 point)
        {
            this.target = point;
            if (NetcodeUtils.IsServer)
            { netTarget.Value = target; }
        }

        internal void LoseTarget()
        {
            targetTransform = null;
        }

        internal bool HasNoTarget
        {
            get
            {
                if (NetcodeUtils.IsClient)
                { return targetTransform == null && netTarget.Value == default; }
                else
                { return targetTransform == null && target == default; }
            }
        }
    }
}
