using AssetManager;

using System;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

using Utilities;

namespace Game.Components
{
    public class Turret : NetworkBehaviour, IHaveAssetFields
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
                    ParticleSystemCurveMode.Constant => Mathf.RoundToInt(count.constant),
                    ParticleSystemCurveMode.TwoConstants => Mathf.RoundToInt((count.constantMin + count.constantMax) / 2f),
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

        Transform KnockbackTransform => UseBarrelInstead ? Barrel : cannon;

        [Header("Sound")]
        [SerializeField] AudioSource AudioSource;
        [SerializeField] AudioClip ShootSound;

        [Header("Barrel")]
        [SerializeField, AssetField] internal Transform Barrel;
        [SerializeField, ReadOnly] bool HasBarrel;
        [SerializeField, AssetField] internal float RequiedBarrelRotationSpeed = 0f;
        [SerializeField, AssetField] internal float BarrelRotationAcceleration = 0f;
        [SerializeField, ReadOnly] float BarrelRotationSpeed = 0f;
        [SerializeField, ReadOnly] internal bool PrepareShooting = false;

        [Header("Cannon")]
        [SerializeField, AssetField] internal Transform cannon;
        [SerializeField, AssetField] internal float cannonRotationSpeed = 1f;
        [SerializeField, AssetField] internal float cannonLowestAngle;
        [SerializeField, AssetField] internal float cannonHighestAngle;

        [Header("Turret")]
        [SerializeField, AssetField] internal float rotationSpeed = 5f;

        [Header("Reload")]
        [SerializeField, AssetField] internal float reloadTime = 1f;
        [SerializeField, ReadOnly] float reload;

        internal float CurrentReload => reload;
        internal float ReloadPercent => (reloadTime <= 0f) ? 1f : (1f - Mathf.Clamp01(reload / reloadTime));

        [Header("Projectile")]
        [SerializeField, AssetField] internal float projectileVelocity = 100f;
        [Tooltip("-1 means infinity")]
        [SerializeField, AssetField] internal float ProjectileLifetime = -1f;
        [Tooltip("Used by timer projectiles that explode in mid-air")]
        [SerializeField, ReadOnly] internal float RequiedProjectileLifetime = -1f;
        [SerializeField, AssetField] internal List<Transform> projectileIgnoreCollision = new();
        [SerializeField, AssetField] internal Transform shootPosition;
        [SerializeField, AssetField] internal GameObject projectile;
        PooledObject PooledProjectile;
        [SerializeField, AssetField] internal GameObject[] Projectiles;
        PooledObject[] PooledProjectiles;

        internal float CurrentProjectileLifetime
        {
            get
            {
                if (RequiedProjectileLifetime == -1f) return ProjectileLifetime;
                if (ProjectileLifetime == -1f) return RequiedProjectileLifetime;
                return Mathf.Min(ProjectileLifetime, RequiedProjectileLifetime);
            }
        }

        internal PooledObject CurrentProjectile
        {
            get
            {
                if (PooledProjectile != null) return PooledProjectile;
                if (SelectedProjectile < 0 || SelectedProjectile >= PooledProjectiles.Length) return null;
                return PooledProjectiles[SelectedProjectile];
            }
        }

        [SerializeField, ReadOnly] internal int SelectedProjectile = 0;
        [SerializeField, AssetField] internal GameObject[] ShootEffects;
        [SerializeField, ReadOnly] BurstParticles[] ShootEffectInstances;
        [SerializeField, ReadOnly] bool IsBallisticProjectile = true;

        [Header("Scope")]
        [SerializeField] internal Transform Scope;
        [SerializeField] internal Transform ScopeHolder;

        [Header("Other")]
        [SerializeField, ReadOnly] Vector3 target;
        readonly NetworkVariable<Vector3> netTarget = new();
        [SerializeField, ReadOnly] Transform targetTransform;
        [SerializeField, ReadOnly] Vector2 overridedInput = Vector2.zero;

        [SerializeField, ReadOnly] internal BaseObject @base;

        internal Transform TargetTransform => targetTransform;

        internal Vector2 Input
        {
            get => input;
            set => overridedInput = value;
        }

        [SerializeField, ReadOnly] Vector2 input = Vector2.zero;

        [SerializeField, ReadOnly] internal float Range;

        readonly List<(Projectile, Vector3)> shots = new();

        internal Vector3 ShootPosition => shootPosition.position;

        [SerializeField] bool ShowRadius;

        internal Vector3 TargetPosition
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

        internal void SetTarget(Vector3 target)
        {
            targetTransform = null;
            this.target = target;

            if (NetcodeUtils.IsServer)
            { netTarget.Value = target; }
        }

        internal void SetTarget(Transform target)
        {
            targetTransform = target;
            this.target = target.position;

            if (NetcodeUtils.IsServer)
            { netTarget.Value = target.position; }
        }

        internal float TurretLocalRotation
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
        internal float CannonLocalRotation
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

        internal float ShootHeight => shootPosition.position.y;
        public bool IsAccurateShoot => currentError <= 0.001f;
        [SerializeField, ReadOnly] float currentError = 1f;

        Vector3 predictedOffset;
        internal Vector3 PredictedOffset => predictedOffset;

        float CannonRotationFix => -(90f - Vector3.SignedAngle(transform.forward, Vector3.up, transform.forward));

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
                ShootEffectInstances = new BurstParticles[ShootEffects.Length];
                for (int i = 0; i < ShootEffects.Length; i++)
                {
                    ParticleSystem newParticleSystem = GameObject.Instantiate(ShootEffects[i], shootPosition.position, shootPosition.rotation, (cannon == null) ? transform : cannon).GetComponent<ParticleSystem>();
                    ShootEffectInstances[i] = new BurstParticles(newParticleSystem);
                }
            }
            else
            {
                ShootEffectInstances = new BurstParticles[0];
            }

            if (CurrentProjectile.TryGetComponent(out Rigidbody projectileRigidbody))
            { IsBallisticProjectile = projectileRigidbody.useGravity; }

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

        void FixedUpdate()
        {
            if (reload > 0f) reload -= Time.fixedDeltaTime;

            if (HasBarrel)
            {
                if (BarrelRotationSpeed != 0)
                { Barrel.localEulerAngles = new Vector3(0f, 0f, Barrel.localEulerAngles.z + (BarrelRotationSpeed * Time.fixedDeltaTime)); }

                if (PrepareShooting)
                {
                    if (BarrelRotationSpeed < RequiedBarrelRotationSpeed)
                    {
                        BarrelRotationSpeed = Mathf.Min(BarrelRotationSpeed + (BarrelRotationAcceleration * Time.fixedDeltaTime), RequiedBarrelRotationSpeed);
                    }
                }
                else
                {
                    if (BarrelRotationSpeed > 0)
                    {
                        BarrelRotationSpeed = Mathf.Max(BarrelRotationSpeed - (BarrelRotationAcceleration * Time.fixedDeltaTime), 0);
                    }
                }
            }

            if (CannonKnockback != 0f)
            {
                switch (CannonKnockbackState)
                {
                    case CannonKnockbackStates.Still:

                        if (CannonKnockbackPosition.current != CannonKnockbackPosition.target)
                        {
                            CannonKnockbackPosition.current = Mathf.MoveTowards(CannonKnockbackPosition.current, CannonKnockbackPosition.target, CannonKnockbackRestoreSpeed * Time.fixedDeltaTime);
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
                            CannonKnockbackPosition.current = Mathf.MoveTowards(CannonKnockbackPosition.current, CannonKnockbackPosition.target, CannonKnockbackSpeed * Time.fixedDeltaTime);
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
                            CannonKnockbackPosition.current = Mathf.MoveTowards(CannonKnockbackPosition.current, CannonKnockbackPosition.target, CannonKnockbackRestoreSpeed * Time.fixedDeltaTime);
                        }

                        break;
                }

                KnockbackTransform.localPosition = CannonOriginalLocalPosition - Vector3.forward * CannonKnockbackPosition.current;
            }

            Vector3 targetPosition = TargetPosition;

            if (targetPosition == Vector3.zero)
            {
                predictedOffset = Vector3.zero;
                // RotateTurret();
                // RotateCannon();
                currentError = 1f;
            }
            else
            {
                Vector2 input;
                if (overridedInput != Vector2.zero)
                {
                    predictedOffset = Vector3.zero;
                    input = overridedInput;
                    this.input = input;
                }
                else
                {
                    Vector3 targetVelocity = Vector3.zero;
                    if (targetTransform != null && targetTransform.gameObject.TryGetComponent(out Rigidbody targetRigidbody))
                    { targetVelocity = targetRigidbody.velocity; }
                    input = CalculateInputVector(targetPosition, targetVelocity);
                    this.input = input;
                }

                currentError = CalculateError(input);

                RotateTurret(input.x);
                RotateCannon(input.y);
            }

            // Debug.DrawLine(ShootPosition, transform.position + transform.forward * 150f, Color.yellow, Time.fixedDeltaTime);
            // Debug.DrawLine(ShootPosition, ShootPosition + cannon.forward * 150f, Color.red, Time.fixedDeltaTime);
        }

        Vector2 CalculateInputVector(Vector3 targetPosition, Vector3 targetVelocity)
        {
            float turretRotation;
            float cannonAngle;

            Vector2 selfGround = transform.position.To2D();
            float cannonLength = Vector2.Distance(selfGround, shootPosition.position.To2D());

            targetPosition += Vector2.ClampMagnitude(selfGround - targetPosition.To2D(), cannonLength).To3D();

            if (IsBallisticProjectile)
            {
                if (targetVelocity.To2D().sqrMagnitude > .1f)
                {
                    Vector2 offset = Velocity.CalculateInterceptCourse(targetPosition.To2D(), targetVelocity.To2D(), selfGround, projectileVelocity);
                    Vector3 offset3 = offset.To3D() * 1.01f;
                    predictedOffset = offset3;

                    targetPosition += offset3;
                }
                else
                { predictedOffset = Vector3.zero; }

                // Debug.DrawLine(transform.position, targetPosition, Color.green, Time.fixedDeltaTime);

                float targetAngle;

                float? theta_;

                using (ProfilerMarkers.TrajectoryMath.Auto())
                { theta_ = Ballistics.AngleOfReach2(projectileVelocity, shootPosition.position, targetPosition); }

                if (!theta_.HasValue)
                { targetAngle = 45f; }
                else
                { targetAngle = theta_.Value * Mathf.Rad2Deg; }

                Vector3 directionToTarget = targetPosition - transform.position;
                if (cannon != null)
                {
                    directionToTarget = Vector3.ProjectOnPlane(directionToTarget, cannon.up);
                    targetAngle += CannonRotationFix;
                }

                turretRotation = Quaternion.LookRotation(directionToTarget).eulerAngles.y;

                cannonAngle = Mathf.Clamp(targetAngle, -Mathf.Abs(cannonLowestAngle), Mathf.Abs(cannonHighestAngle));
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
                { predictedOffset = Vector3.zero; }

                Vector3 rotationToTarget = Quaternion.LookRotation(targetPosition - shootPosition.position).eulerAngles;

                turretRotation = rotationToTarget.y;

                cannonAngle = -rotationToTarget.x;
                cannonAngle += CannonRotationFix;

            }

            return new Vector2(turretRotation, cannonAngle);
        }

        float CalculateError(Vector2 input)
        {
            float error = 0f;
            error += Mathf.Abs(input.x - transform.rotation.eulerAngles.y);
            error = Utilities.Utils.NormalizeAngle360(error);
            error += Mathf.Abs(input.y - CannonLocalRotation);
            error = Utilities.Utils.NormalizeAngle360(error);
            error *= .5f;
            error /= 360f;
            return error;
        }

        internal Vector3? PredictImpact()
            => PredictImpact(out _);
        internal Vector3? PredictImpact(out bool outOfRange)
        {
            outOfRange = false;
            if (!IsBallisticProjectile)
            {
                return (projectileVelocity * CurrentProjectileLifetime * shootPosition.forward) + shootPosition.position;
            }
            else
            {
                float? x_;

                using (ProfilerMarkers.TrajectoryMath.Auto())
                { x_ = Ballistics.CalculateX(CannonLocalRotation * Mathf.Deg2Rad, projectileVelocity, ShootHeight); }
                if (!x_.HasValue) return null;
                float x = x_.Value;

                Vector3 turretRotation = transform.rotation * Vector3.back;
                Vector3 point = (shootPosition.position - x * turretRotation);
                point.y = 0f;

                if (CurrentProjectileLifetime > 0f)
                {
                    Vector2 displacement;
                    using (ProfilerMarkers.TrajectoryMath.Auto())
                    { displacement = Ballistics.Displacement(CannonLocalRotation * Mathf.Deg2Rad, projectileVelocity, CurrentProjectileLifetime); }

                    float x2 = displacement.x;
                    if (x2 < x)
                    {
                        outOfRange = true;
                        point = (shootPosition.position - x2 * turretRotation);
                        point.y = displacement.y;
                    }
                }

                // point.y = Mathf.Max(point.y, TheTerrain.Height(point));

                return point;
            }
        }
        internal float? ImpactTime()
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

            float d = Vector3.Distance(ShootPosition, impact);

            if (IsBallisticProjectile)
            {
                float? t;
                using (ProfilerMarkers.TrajectoryMath.Auto())
                {
                    float? _d = Ballistics.CalculateX(CannonLocalRotation * Mathf.Deg2Rad, projectileVelocity, ShootHeight);
                    if (!_d.HasValue) return null;
                    d = _d.Value;

                    t = Ballistics.TimeToReachDistance(v, CannonLocalRotation * Mathf.Deg2Rad, d);
                }

                if (t.HasValue)
                { t = Mathf.Min(t.Value, projectile.Lifetime); }

                return t;
            }
            else
            {
                float a = projectile.Acceleration;
                float t;

                if (a == 0f)
                {
                    t = Velocity.CalculateTime(d, v);
                }
                else
                {
                    float maxV = Acceleration.SpeedAfterDistance(v, a, d);
                    t = Acceleration.TimeToReachVelocity(v, maxV, a);
                }

                t = Mathf.Min(t, projectile.Lifetime);
                return t;
            }
        }

        void RotateTurretInstant(float to)
        {
            TurretLocalRotation = to;
        }
        void RotateTurret(float to)
        {
            TurretLocalRotation = Mathf.MoveTowardsAngle(TurretLocalRotation, to - transform.parent.localEulerAngles.y, rotationSpeed * Time.fixedDeltaTime);
        }
        void RotateTurret()
        {
            TurretLocalRotation = Mathf.MoveTowardsAngle(TurretLocalRotation, 0f, rotationSpeed * Time.fixedDeltaTime);
        }

        void RotateCannon(float to)
        {
            /*
            float min = Mathf.Min(cannonLowestAngle, cannonHighestAngle);

            float max = Mathf.Max(cannonLowestAngle, cannonHighestAngle);

            float newNewAngle = Utilities.Utils.ModularClamp(newAngle, cannonLowestAngle, cannonHighestAngle); // Mathf.Clamp(Utils.NormalizeAngle(newAngle), Utils.NormalizeAngle(min), Utils.NormalizeAngle(max));
            */

            CannonLocalRotation = Mathf.MoveTowardsAngle(CannonLocalRotation, to, cannonRotationSpeed * Time.fixedDeltaTime);
        }
        void RotateCannonInstant(float to)
        {
            CannonLocalRotation = to;
        }
        void RotateCannon()
        {
            CannonLocalRotation = Mathf.MoveTowardsAngle(CannonLocalRotation, 0f, cannonRotationSpeed * Time.fixedDeltaTime);
        }

        internal bool Shoot()
            => Shoot(null, 0f);
        internal bool Shoot(RequiredShoots requiedShoots)
        {
            float? t = ImpactTime();
            if (!t.HasValue)
            { return Shoot(); }
            return Shoot(requiedShoots, t.Value);
        }
        internal bool Shoot(RequiredShoots requiedShoots, float impactTime)
        {
            if (reload > 0f)
            { return false; }

            if (CurrentProjectile == null)
            { return false; }

            if (Barrel != null &&
                BarrelRotationSpeed < RequiedBarrelRotationSpeed)
            { return false; }

            GameObject newProjectile = CurrentProjectile.Instantiate(shootPosition.position, shootPosition.rotation, ObjectGroups.Projectiles);

            if (newProjectile == null)
            { return false; }

            if (newProjectile.TryGetComponent(out Rigidbody rb))
            {
                rb.velocity = shootPosition.forward * projectileVelocity;
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

                _projectile.Shot = new Projectile.Trajectory(CannonLocalRotation, transform.rotation.eulerAngles.y, projectileVelocity, shootPosition.position);

                Vector3 predictedImpactPosition = PredictImpact() ?? TargetPosition;

                shots.Add((_projectile, predictedImpactPosition));

                if (IsAccurateShoot)
                {
                    if (requiedShoots != null)
                    {
                        float predictedDamage = 0f;
                        if (requiedShoots.HasComponent<Projectile>())
                        {
                            predictedDamage = 1f;
                        }
                        else
                        {
                            predictedDamage += _projectile.ImpactDamage;
                            predictedDamage += _projectile.ExploisonDamage * .2f;
                        }
                        requiedShoots.Shoot(impactTime, predictedDamage);
                    }
                    else if (targetTransform != null &&
                        targetTransform.gameObject.TryGetComponent(out requiedShoots))
                    {
                        if ((predictedImpactPosition - TargetPosition).sqrMagnitude < 1f)
                        {
                            float? predictedImpactTime_ = Utilities.Ballistics.CalculateTime(projectileVelocity, CannonLocalRotation * Mathf.Deg2Rad, ShootHeight);
                            if (predictedImpactTime_.HasValue)
                            {
                                float predictedImpactTime = predictedImpactTime_.Value;
                                float predictedDamage = _projectile.ImpactDamage;
                                predictedDamage += _projectile.ExploisonDamage * .2f;
                                requiedShoots.Shoot(predictedImpactTime, predictedDamage);
                            }
                        }
                    }
                }
            }

            if (IsServer)
            { OnShoot_ClientRpc(new Vector2(TurretLocalRotation, CannonLocalRotation)); }

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

            for (int i = 0; i < ShootEffectInstances.Length; i++)
            { ShootEffectInstances[i].Emit(); }

            return true;
        }

        internal void NotifyImpact(Projectile projectile, Vector3 at)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                if (shots[i].Item1 != projectile) continue;
                // Vector3 error = at - shots[i].Item2;
                shots.RemoveAt(i);
            }
        }

        internal float GetRange()
        {
            float range;
            using (ProfilerMarkers.TrajectoryMath.Auto())
            {
                range = Ballistics.MaxRadius(projectileVelocity, ShootHeight);
                if (ProjectileLifetime > 0f)
                { range = Mathf.Min(range, Ballistics.DisplacementX(45f * Mathf.Deg2Rad, projectileVelocity, ProjectileLifetime)); }
            }
            this.Range = range;
            return range;
        }

        void OnDrawGizmos()
        {
            if (ShowRadius)
            {
                float? r2 = Ballistics.CalculateX(CannonLocalRotation * Mathf.Deg2Rad, projectileVelocity, ShootHeight);
                if (r2.HasValue)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(shootPosition.position, r2.Value);
                }

                float maxR = GetRange();

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(shootPosition.position, maxR);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (TargetPosition != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(TargetPosition, .5f);
            }
        }

        [ClientRpc]
        void OnShoot_ClientRpc(Vector2 input)
        {
            this.input = input;
            currentError = CalculateError(input);

            RotateTurretInstant(input.x);
            RotateCannonInstant(input.y);
            reload = 0f;
            Shoot();
        }

        internal void ShootRequest()
            => ShootRequest_ServerRpc(new Vector2(TurretLocalRotation, CannonLocalRotation));

        [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
        void ShootRequest_ServerRpc(Vector2 input)
        {
            this.input = input;
            currentError = CalculateError(input);

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
                { return targetTransform == null && netTarget.Value == Vector3.zero; }
                else
                { return targetTransform == null && target == Vector3.zero; }
            }
        }
    }
}
