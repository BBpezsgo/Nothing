using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Utilities;
using UnityEngine.Serialization;
using Maths;

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

        float turretAngle = MathF.Round(property.FindPropertyRelative("TurretAngle").floatValue);
        float cannonAngle = MathF.Round(property.FindPropertyRelative("CannonAngle").floatValue);

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

    [Serializable]
    public struct TurretBarrel
    {
        [SerializeField] public Transform? Barrel;
        [SerializeField] public Transform ShootPosition;

        [Header("Debug")]
        [SerializeField, ReadOnly, NonReorderable] public TurretBurstParticles[] BurstParticles;
        [SerializeField, ReadOnly] public Vector3 OriginalLocalPosition;
        [SerializeField, ReadOnly] public TurretCannonKnockbackState KnockbackState;
        [SerializeField, ReadOnly] public (float Original, float Current, float Target) KnockbackPosition;
    }

    [Serializable]
    public readonly struct TurretBurstParticles
    {
        readonly ParticleSystem ParticleSystem;
        readonly int BurstCount;
        readonly float Probability;

        public TurretBurstParticles(ParticleSystem particleSystem)
        {
            if (particleSystem == null) throw new ArgumentNullException(nameof(particleSystem));

            ParticleSystem = particleSystem;
            ParticleSystem.Burst burst = particleSystem.emission.GetBurst(0);
            ParticleSystem.MinMaxCurve count = burst.count;
            BurstCount = count.mode switch
            {
                ParticleSystemCurveMode.Constant => (int)MathF.Round(count.constant),
                ParticleSystemCurveMode.TwoConstants => (int)MathF.Round((count.constantMin + count.constantMax) / 2f),
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

    public enum TurretCannonKnockbackState
    {
        Still,
        Knockback,
        Restore,
    }

    public class Turret : NetworkBehaviour
    {
        [SerializeField, Min(0f)] float Knockback = 1f;
        [SerializeField, Min(0f)] float CannonKnockback = 0f;
        [SerializeField, Min(0f)] float CannonKnockbackRestoreSpeed = 1f;
        [SerializeField, Min(0f)] float CannonKnockbackSpeed = 1f;
        [SerializeField] bool UseBarrelInstead;

        [Header("Sound")]
        [SerializeField] AudioSource? AudioSource;
        [SerializeField] AudioClip? ShootSound;

        [Header("Barrel")]
        [SerializeField] Transform? Barrel;
        [SerializeField] public TurretBarrel[] Barrels = null!;

        public ref TurretBarrel NextBarrel => ref Barrels[NextShoot];

        [SerializeField, ReadOnly] bool HasBarrel;
        [FormerlySerializedAs("RequiedBarrelRotationSpeed")]
        [SerializeField] public float RequiredBarrelRotationSpeed = 0f;
        [SerializeField] public float BarrelRotationAcceleration = 0f;
        [SerializeField, ReadOnly] float BarrelRotationSpeed = 0f;
        [SerializeField, ReadOnly] public bool PrepareShooting = false;

        [Header("Cannon")]
        [SerializeField] public Transform? cannon;
        [SerializeField] public float cannonRotationSpeed = 1f;
        [MinMax(-90f, 90f)]
        [SerializeField] public Vector2 CannonAngleRange;
        [SerializeField] bool HighAngleFallback;

        [Header("Turret")]
        [SerializeField] public float rotationSpeed = 5f;

        [Header("Reload")]
        [SerializeField] public float reloadTime = 1f;
        [SerializeField, ReadOnly] float reload;

        public float CurrentReload => reload;
        public float ReloadPercent => (reloadTime <= 0f) ? 1f : (1f - Math.Clamp(reload / reloadTime, 0, 1));

        [Header("Projectile")]
        [SerializeField] public float projectileVelocity = 100f;
        [Tooltip("-1 means infinity")]
        [SerializeField] public float ProjectileLifetime = -1f;
        [Tooltip("Used by timer projectiles that explode in mid-air")]
        [SerializeField, ReadOnly] public float RequiredProjectileLifetime = -1f;
        [SerializeField] public List<Transform> projectileIgnoreCollision = new();
        [SerializeField] Transform shootPosition = null!;

        [SerializeField] public GameObject projectile = null!;
        PooledPrefab PooledProjectile;
        [SerializeField] public GameObject[] Projectiles = null!;
        PooledPrefab[] PooledProjectiles = null!;
        [SerializeField] float Randomness = 0f;
        [SerializeField] UnityEngine.Vector2Int BulletCount = UnityEngine.Vector2Int.one;

        [SerializeField] ParticleSystem? Shells;

        public float CurrentProjectileLifetime
        {
            get
            {
                if (RequiredProjectileLifetime == -1f) return ProjectileLifetime;
                if (ProjectileLifetime == -1f) return RequiredProjectileLifetime;
                return Math.Min(ProjectileLifetime, RequiredProjectileLifetime);
            }
        }

        public PooledPrefab CurrentProjectile
        {
            get
            {
                if (PooledProjectile.IsNotNull) return PooledProjectile;
                if (SelectedProjectile < 0 || SelectedProjectile >= PooledProjectiles.Length) return default;
                return PooledProjectiles[SelectedProjectile];
            }
        }

        [SerializeField, ReadOnly] public int SelectedProjectile = 0;
        [SerializeField] public GameObject[] ShootEffects = null!;
        [SerializeField, ReadOnly] bool IsBallisticProjectile = true;

        [Header("Scope")]
        [SerializeField] public Transform? Scope;
        [SerializeField] public Transform? ScopeHolder;

        [Header("Other")]
        [SerializeField, ReadOnly] Vector3 target;
        readonly NetworkVariable<Vector3> netTarget = new();
        [SerializeField, ReadOnly] Transform? targetTransform;
        [SerializeField, ReadOnly] TurretInput overriddenInput = (0f, 0f);
        [SerializeField, ReadOnly] int NextShoot;

        [SerializeField, ReadOnly] public BaseObject @base = null!;

        public Transform? TargetTransform => targetTransform;

        public TurretInput Input
        {
            get => input;
            set => overriddenInput = value;
        }

        [SerializeField, ReadOnly] TurretInput input = (0f, 0f);

        [SerializeField, ReadOnly] public float Range;

        readonly List<(Projectile Projectile, Vector3 PredictedImpact)> shots = new();

        public Vector3 ShootPosition => NextBarrel.ShootPosition.position;

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
        Vector3 _calculatedTargetPosition;

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

        void RotateTurret(float to, bool instant)
        {
            TurretLocalRotation = instant ? to : Mathf.MoveTowardsAngle(TurretLocalRotation, to, rotationSpeed * Time.deltaTime);
        }

        void RotateCannon(float to, bool instant)
        {
            to = Math.Clamp(Rotation.NormalizeAngle(to), CannonAngleRange.x, CannonAngleRange.y);

            CannonLocalRotation = instant ? to : Mathf.MoveTowardsAngle(CannonLocalRotation, to, cannonRotationSpeed * Time.deltaTime);
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
                { return transform.localEulerAngles.x; }
                else
                { return cannon.transform.localEulerAngles.x; }
            }
            set
            {
                if (cannon == null)
                { transform.localEulerAngles = new Vector3(value, transform.localEulerAngles.y, 0f); }
                else
                { cannon.transform.localEulerAngles = new Vector3(value, 0f, 0f); }
            }
        }

        public float ShootHeight => NextBarrel.ShootPosition.position.y;
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

        void Awake()
        {
            if (Barrels.Length == 0)
            {
                Debug.Log($"Generating barrel info for {this}", this);

                TurretBarrel barrel = new()
                {
                    Barrel = Barrel,
                    ShootPosition = shootPosition,
                };
                Barrels = new TurretBarrel[] { barrel };
            }
        }

        void Start()
        {
            if (projectile != null)
            {
                PooledProjectile = ObjectPool.Instance.GeneratePool(projectile);
            }

            if (Projectiles.Length > 0)
            {
                PooledProjectiles = new PooledPrefab[Projectiles.Length];
                for (int i = 0; i < Projectiles.Length; i++)
                {
                    PooledProjectiles[i] = ObjectPool.Instance.GeneratePool(Projectiles[i]);
                }
            }

            targetTransform = null;

            IsBallisticProjectile = CurrentProjectile.IsNotNull && CurrentProjectile.TryGetComponent(out Rigidbody projectileRigidbody) && projectileRigidbody.useGravity;

            Range = GetRange();

            for (int i = 0; i < Barrels.Length; i++)
            {
                ref TurretBarrel barrel = ref Barrels[i];
                barrel.BurstParticles = GenerateShootParticles(new List<TurretBurstParticles>(), ShootEffects, barrel.ShootPosition).ToArray();
                barrel.KnockbackPosition = (0f, 0f, 0f);
                barrel.KnockbackState = TurretCannonKnockbackState.Still;
                barrel.OriginalLocalPosition = (UseBarrelInstead && Barrel != null) ? Barrel.localPosition : ((cannon != null) ? cannon.localPosition : default);
            }

            HasBarrel = Barrel != null || Barrels.Length > 0;

            if (CannonKnockback != 0f)
            {
                for (int i = 0; i < Barrels.Length; i++)
                {
                    Transform? knockbackTransform = UseBarrelInstead ? Barrels[i].Barrel : cannon;

                    Barrels[i].KnockbackPosition = (0f, 0f, 0f);
                    Barrels[i].OriginalLocalPosition = knockbackTransform != null ? knockbackTransform.localPosition : default;
                }
            }
        }

        void Update()
        {
            if (reload > 0f) reload -= Time.deltaTime;

            if (HasBarrel)
            {
                if (BarrelRotationSpeed != 0)
                { NextBarrel.Barrel!.localEulerAngles = new Vector3(0f, 0f, NextBarrel.Barrel.localEulerAngles.z + (BarrelRotationSpeed * Time.deltaTime)); }

                if (PrepareShooting)
                {
                    if (BarrelRotationSpeed < RequiredBarrelRotationSpeed)
                    {
                        BarrelRotationSpeed = Math.Min(BarrelRotationSpeed + (BarrelRotationAcceleration * Time.deltaTime), RequiredBarrelRotationSpeed);
                    }
                }
                else
                {
                    if (BarrelRotationSpeed > 0)
                    {
                        BarrelRotationSpeed = Math.Max(BarrelRotationSpeed - (BarrelRotationAcceleration * Time.deltaTime), 0);
                    }
                }
            }

            if (CannonKnockback != 0f)
            {
                for (int i = 0; i < Barrels.Length; i++)
                {
                    ref TurretBarrel barrel = ref Barrels[i];
                    Transform knockbackTransform = (UseBarrelInstead ? barrel.Barrel : cannon)!;

                    switch (barrel.KnockbackState)
                    {
                        case TurretCannonKnockbackState.Still:

                            if (barrel.KnockbackPosition.Current != barrel.KnockbackPosition.Target)
                            {
                                barrel.KnockbackPosition.Current = Mathf.MoveTowards(barrel.KnockbackPosition.Current, barrel.KnockbackPosition.Target, CannonKnockbackRestoreSpeed * Time.deltaTime);
                            }

                            break;
                        case TurretCannonKnockbackState.Knockback:

                            if (barrel.KnockbackPosition.Current == barrel.KnockbackPosition.Target)
                            {
                                barrel.KnockbackState = TurretCannonKnockbackState.Restore;
                                barrel.KnockbackPosition.Target = barrel.KnockbackPosition.Original;
                            }
                            else
                            {
                                barrel.KnockbackPosition.Current = Mathf.MoveTowards(barrel.KnockbackPosition.Current, barrel.KnockbackPosition.Target, CannonKnockbackSpeed * Time.deltaTime);
                            }

                            break;
                        case TurretCannonKnockbackState.Restore:

                            if (barrel.KnockbackPosition.Current == barrel.KnockbackPosition.Target)
                            {
                                barrel.KnockbackState = TurretCannonKnockbackState.Still;
                                barrel.KnockbackPosition.Target = barrel.KnockbackPosition.Original;
                            }
                            else
                            {
                                barrel.KnockbackPosition.Current = Mathf.MoveTowards(barrel.KnockbackPosition.Current, barrel.KnockbackPosition.Target, CannonKnockbackRestoreSpeed * Time.deltaTime);
                            }

                            break;
                    }

                    knockbackTransform.localPosition = barrel.OriginalLocalPosition - (Vector3.forward * barrel.KnockbackPosition.Current);
                }
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
                        RotateTurret(0f, false);
                        RotateCannon(0f, false);
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
                        targetVelocity = targetRigidbody.linearVelocity;
                        if (targetTransform != null && targetTransform.gameObject.TryGetComponent(out MovementEngine movementEngine))
                        { targetAcceleration = movementEngine.Acceleration; }
                    }
                    input = Aim(targetPosition, targetVelocity, targetAcceleration, out _outOfRange);
                }

                _error = _outOfRange ? 1f : CalculateError(input);

                RotateTurret(input.TurretAngle, false);
                RotateCannon(input.CannonAngle, false);
            }

            // Debug.DrawLine(ShootPosition, transform.position + transform.forward * 150f, Color.yellow, Time.deltaTime);
            // Debug.DrawLine(ShootPosition, ShootPosition + cannon.forward * 150f, Color.red, Time.deltaTime);
        }

        List<TurretBurstParticles> GenerateShootParticles(List<TurretBurstParticles> burstParticles, GameObject[] prefabs, Transform parent)
        {
            foreach (GameObject prefab in prefabs)
            { GenerateShootParticles(burstParticles, prefab, parent); }
            return burstParticles;
        }
        List<TurretBurstParticles> GenerateShootParticles(List<TurretBurstParticles> burstParticles, GameObject prefab, Transform parent)
        {
            GameObject newParticleSystem = GameObject.Instantiate(prefab, parent.position, parent.rotation, parent);
            ParticleSystem[] particleSystems = newParticleSystem.GetComponentsInChildren<ParticleSystem>(false);
            foreach (ParticleSystem particleSystem in particleSystems)
            { burstParticles.Add(new TurretBurstParticles(particleSystem)); }
            return burstParticles;
        }

        TurretInput Aim(Vector3 targetPosition, Vector3 targetVelocity, Vector3 targetAcceleration, out bool outOfRange)
        {
            Vector2 selfGround = transform.position.To2D();
            // float cannonLength = Vector2.Distance(selfGround, NextBarrel.ShootPosition.position.To2D());

            // targetPosition += Vector2.ClampMagnitude(selfGround - targetPosition.To2D(), cannonLength).To3D();

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

            _calculatedTargetPosition = targetPosition;

            return Aim(targetPosition, out outOfRange);
        }

        TurretInput Aim(Vector3 targetPosition, out bool outOfRange)
        {
            outOfRange = false;

            if (IsBallisticProjectile)
            {
                float targetAngle;

                using (Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
                {
                    float? theta = Ballistics.AngleOfReach2(projectileVelocity, transform.InverseTransformPoint(ShootPosition).To(), transform.InverseTransformPoint(targetPosition).To());

                    outOfRange = !theta.HasValue;
                    targetAngle = theta.HasValue ? -theta.Value * Rotation.Rad2Deg : 45f;

                    if (HighAngleFallback && (targetAngle < CannonAngleRange.x || targetAngle > CannonAngleRange.y))
                    {
                        theta = Ballistics.AngleOfReach1(projectileVelocity, transform.InverseTransformPoint(ShootPosition).To(), transform.InverseTransformPoint(targetPosition).To());

                        outOfRange = !theta.HasValue;
                        targetAngle = theta.HasValue ? -theta.Value * Rotation.Rad2Deg : 45f;
                    }
                }

                Vector3 directionToTarget = Vector3.Normalize(targetPosition - ShootPosition);
                float turretRotation2 = Vector3AngleOnPlane(directionToTarget, -transform.up, transform.parent.forward);

                return (turretRotation2, targetAngle);
            }
            else
            {
                Vector3 directionToTarget = Vector3.Normalize(targetPosition - transform.position);
                float turretRotation2 = Vector3AngleOnPlane(directionToTarget, -transform.up, transform.parent.forward);
                float upAngle = Vector3.Angle(transform.up, directionToTarget);

                return (turretRotation2, upAngle - 90);
            }
        }

        float Vector3AngleOnPlane(Vector3 direction, Vector3 planeNormal, Vector3 toZeroAngle)
        {
            Vector3 projectedVector = Vector3.ProjectOnPlane(direction, planeNormal);
            return Vector3.SignedAngle(projectedVector, toZeroAngle, planeNormal);
        }

        float CalculateError(Vector2 input)
        {
            float error = 0f;
            error += Rotation.NormalizeAngle360(Math.Abs(input.x - TurretLocalRotation));
            error += Rotation.NormalizeAngle360(Math.Abs(input.y - CannonLocalRotation));
            error /= 360f * 2f;
            return error;
        }

        public Vector3? PredictImpact()
            => PredictImpact(out _);
        public Vector3? PredictImpact(out bool outOfRange)
        {
            outOfRange = false;
            if (!IsBallisticProjectile)
            {
                return (projectileVelocity * CurrentProjectileLifetime * NextBarrel.ShootPosition.forward) + NextBarrel.ShootPosition.position;
            }
            else
            {
                using (Maths.Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
                { return Maths.Ballistics.PredictImpact(NextBarrel.ShootPosition, projectileVelocity, CurrentProjectileLifetime, out outOfRange)?.To(); }
            }
        }

        public float? ImpactTime()
        {
            if (this.CurrentProjectile.IsNull)
            { return null; }
            if (!this.CurrentProjectile.TryGetComponent(out Projectile projectile))
            { return null; }

            float v = this.projectileVelocity * .95f;

            Vector3? _impact = PredictImpact();
            if (!_impact.HasValue)
            { return null; }

            Vector3 impact = _impact.Value;

            float d = Vector3.Distance(ShootPosition, impact);

            float? t;

            if (IsBallisticProjectile)
            {
                using (Maths.Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
                { t = Maths.Ballistics.TimeToReachDistance(v, CannonLocalRotation * Rotation.Deg2Rad, d); }
            }
            else
            {
                float a = projectile.Acceleration;

                if (a == 0f)
                {
                    t = Maths.Velocity.CalculateTime(d, v);
                }
                else
                {
                    float maxV = Maths.Acceleration.SpeedAfterDistance(v, a, d);
                    t = Maths.Acceleration.TimeToReachVelocity(v, maxV, a);
                }
            }

            if (!t.HasValue) return null;
            else if (projectile.Lifetime > 0f) return Math.Min(t.Value, projectile.Lifetime);
            else return t.Value;
        }
        internal bool TryGetTrajectory(out Ballistics.Trajectory trajectory)
        {
            trajectory = default;

            if (!IsBallisticProjectile)
            { return false; }

            trajectory = new Maths.Ballistics.Trajectory(CannonLocalRotation, transform.rotation.eulerAngles.y, projectileVelocity, NextBarrel.ShootPosition.position.To());
            return true;
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

            if (CurrentProjectile.IsNull)
            { return false; }

            if (NextBarrel.Barrel != null &&
                BarrelRotationSpeed < RequiredBarrelRotationSpeed)
            { return false; }

            bool instantiatedAnyProjectile = false;
            for (int i = 0; i < bulletCount; i++)
            {
                GameObject? newProjectile = CurrentProjectile.Instantiate(NextBarrel.ShootPosition.position, NextBarrel.ShootPosition.rotation, ObjectGroups.Projectiles);

                if (newProjectile == null)
                {
                    Debug.LogWarning("Failed to instantiate projectile", this);
                    continue;
                }

                instantiatedAnyProjectile = true;

                if (newProjectile.TryGetComponent(out Rigidbody rb))
                {
                    Vector3 velocityResult = NextBarrel.ShootPosition.forward * projectileVelocity;

                    if (Randomness > 0f)
                    {
                        Vector3 randomRotation = UnityEngine.Random.insideUnitSphere * Randomness;
                        velocityResult += randomRotation;
                    }

                    rb.linearVelocity = velocityResult;
                }

                if (newProjectile.TryGetComponent(out Projectile _projectile))
                {
                    _projectile.TargetPosition = TargetPosition;

                    _projectile.lastPosition = NextBarrel.ShootPosition.position;

                    _projectile.ignoreCollision = projectileIgnoreCollision.ToArray();
                    _projectile.OwnerTeamHash = @base.TeamHash;
                    _projectile.Owner = @base;

                    _projectile.LifeLeft = CurrentProjectileLifetime;
                    _projectile.InfinityLifetime = ProjectileLifetime <= 0f;

                    _projectile.Shot = new Maths.Ballistics.Trajectory(CannonLocalRotation, transform.rotation.eulerAngles.y, projectileVelocity, NextBarrel.ShootPosition.position.To());

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
                NextBarrel.KnockbackPosition.Target = NextBarrel.KnockbackPosition.Original + CannonKnockback;
                NextBarrel.KnockbackState = TurretCannonKnockbackState.Knockback;
            }

            if (@base.TryGetComponent(out Rigidbody baseRigidbody))
            { baseRigidbody.AddForceAtPosition((cannon == null ? transform : cannon).forward * -Knockback, transform.position, ForceMode.Impulse); }

            if (AudioSource != null &&
                ShootSound != null)
            { AudioSource.PlayOneShot(ShootSound); }

            reload = reloadTime;

            if (QualityHandler.EnableParticles)
            {
                foreach (TurretBurstParticles burst in Barrels[NextShoot].BurstParticles)
                { burst.Emit(); }
            }

            if (Barrels.Length > 0)
            {
                NextShoot++;
                NextShoot %= Barrels.Length;
            }

            return true;
        }

        public void NotifyImpact(Projectile projectile, Vector3 at)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                if (shots[i].Projectile != projectile) continue;
                // Vector3 error = at - shots[i].PredictedImpact;
                shots.RemoveAt(i);
            }
        }

        public void NotifyDamage((Vector3 Position, float Amount, DamageKind Kind)[] damages)
        {
            if (@base is ICanTakeControl canTakeControl)
            { canTakeControl.OnDamagedSomebody?.Invoke(damages); }
        }

        public float GetRange()
        {
            float range;
            using (Maths.Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
            {
                range = Maths.Ballistics.MaxRadius(projectileVelocity, ShootHeight);
                if (ProjectileLifetime > 0f)
                { range = Math.Min(range, Maths.Ballistics.DisplacementX(45f * Rotation.Deg2Rad, projectileVelocity, ProjectileLifetime)); }
            }
            this.Range = range;
            return range;
        }

        void OnDrawGizmos()
        {
            if (ShowRadius)
            {
                float? r2 = Maths.Ballistics.CalculateX(CannonLocalRotation * Rotation.Deg2Rad, projectileVelocity, ShootHeight);
                if (r2.HasValue)
                {
                    Gizmos.color = Maths.CoolColors.White;
                    Gizmos.DrawWireSphere(NextBarrel.ShootPosition.position, r2.Value);
                }

                float maxR = GetRange();

                Gizmos.color = Maths.CoolColors.Red;
                Gizmos.DrawWireSphere(NextBarrel.ShootPosition.position, maxR);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (CannonAngleRange != default)
            {
                Gizmos.color = Color.white;

                Span<Vector3> points = stackalloc Vector3[10];

                for (int i = 0; i < points.Length; i++)
                {
                    float v = (float)i / (float)points.Length;
                    v = General.MapToRange(v, 0f, (float)(points.Length - 1) / (float)points.Length, CannonAngleRange.x, CannonAngleRange.y);
                    Vector3 point = transform.forward;
                    point = Quaternion.AngleAxis(v, transform.right) * point;
                    points[i] = transform.position + ((transform.position - ShootPosition).magnitude * 1.5f * point);
                }

                Gizmos.DrawLineStrip(points, false);
            }

            if (TargetPosition != default)
            {
                Gizmos.color = CoolColors.Red;
                GizmosPlus.DrawPoint(TargetPosition, 1f);
                Debug3D.Label(TargetPosition, "Target");

                Gizmos.color = CoolColors.Orange;
                GizmosPlus.DrawPoint(_calculatedTargetPosition, 1f);

                // Debug3D.Label(transform.position, $"Error: {_error}");
            }
        }

        [ClientRpc]
        void OnShoot_ClientRpc(Vector2 input, int bulletCount)
        {
            this.input = input;
            _error = CalculateError(input);
            _outOfRange = false;

            RotateTurret(input.x, true);
            RotateCannon(input.y, true);
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
