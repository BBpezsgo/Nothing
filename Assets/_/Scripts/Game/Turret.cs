using AssetManager;

using System;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    [Header("Sound")]
    [SerializeField] AudioSource AudioSource;
    [SerializeField] AudioClip ShootSound;

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

    [Header("Projectile")]
    [SerializeField, AssetField] internal float projectileVelocity = 100f;
    [Tooltip("-1 means infinity")]
    [SerializeField, AssetField] internal float ProjectileLifetime = -1f;
    [Tooltip("Used by timer projectiles that explode in mid-air")]
    [SerializeField, ReadOnly] internal float RequiedProjectileLifetime = -1f;
    [SerializeField, AssetField] internal List<Transform> projectileIgnoreCollision = new();
    [SerializeField, AssetField] internal Transform shootPosition;
    [SerializeField, AssetField] internal GameObject projectile;
    [SerializeField, AssetField] internal GameObject[] Projectiles;

    internal float CurrentProjectileLifetime
    {
        get
        {
            if (RequiedProjectileLifetime == -1f) return ProjectileLifetime;
            if (ProjectileLifetime == -1f) return RequiedProjectileLifetime;
            return Mathf.Min(ProjectileLifetime, RequiedProjectileLifetime);
        }
    }

    internal GameObject CurrentProjectile
    {
        get
        {
            if (projectile != null) return projectile;
            if (SelectedProjectile < 0 || SelectedProjectile >= Projectiles.Length) return null;
            return Projectiles[SelectedProjectile];
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
    [SerializeField, ReadOnly] internal NetworkVariable<Vector3> target;
    [SerializeField, ReadOnly] internal Transform targetTransform;
    [SerializeField, ReadOnly] Vector2 overridedInput = Vector2.zero;

    [SerializeField, ReadOnly] internal BaseObject @base;

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

    internal Vector3 TargetPosition => targetTransform == null ? target.Value : targetTransform.position;

    internal float TurretLocalRotation
    {
        get => transform.localRotation.eulerAngles.y;
        set => transform.localRotation = Quaternion.Euler(0f, value, 0f);
    }
    internal float CannonLocalRotation
    {
        get => -cannon.transform.localRotation.eulerAngles.x;
        set => cannon.transform.localRotation = Quaternion.Euler(-value, 0f, 0f);
    }
    internal float ShootHeight => shootPosition.position.y;
    public bool IsAccurateShoot => currentError <= 0.001f;
    [SerializeField, ReadOnly] float currentError = 1f;

    void Start()
    {
        targetTransform = null;
        if (ShootEffects != null)
        {
            ShootEffectInstances = new BurstParticles[ShootEffects.Length];
            for (int i = 0; i < ShootEffects.Length; i++)
            {
                ParticleSystem newParticleSystem = GameObject.Instantiate(ShootEffects[i], shootPosition.position, shootPosition.rotation, cannon).GetComponent<ParticleSystem>();
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
    }

    void FixedUpdate()
    {
        if (reload > 0f) reload -= Time.fixedDeltaTime;

        if (TargetPosition == Vector3.zero)
        {
            RotateTurret();
            RotateCannon();
            currentError = 1f;
        }
        else
        {
            Vector2 input;
            if (overridedInput != Vector2.zero)
            {
                input = overridedInput;
                this.input = input;
            }
            else
            {
                Vector3 targetVelocity = Vector3.zero;
                if (targetTransform != null && targetTransform.gameObject.TryGetComponent(out Rigidbody targetRigidbody))
                { targetVelocity = targetRigidbody.velocity; }
                input = CalculateInputVector(TargetPosition, targetVelocity);
                this.input = input;
            }

            currentError = CalculateError(input);

            RotateTurret(input.x);
            RotateCannon(input.y);
        }
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
                Vector2 offset = Utilities.Velocity.CalculateInterceptCourse(targetPosition.To2D(), targetVelocity.To2D(), selfGround, projectileVelocity);
                targetPosition += offset.To3D() * 1.01f;
            }

            float targetAngle;

            float? theta_ = Utilities.Ballistics.AngleOfReach2(projectileVelocity, shootPosition.position, targetPosition);

            if (!theta_.HasValue)
            { targetAngle = 45f; }
            else
            { targetAngle = theta_.Value * Mathf.Rad2Deg; }

            turretRotation = Quaternion.LookRotation(targetPosition - transform.position).eulerAngles.y;

            cannonAngle = Mathf.Clamp(targetAngle, -Mathf.Abs(cannonLowestAngle), Mathf.Abs(cannonHighestAngle));
        }
        else
        {
            if (targetVelocity.To2D().sqrMagnitude > .1f)
            {
                Vector2 offset = Utilities.Acceleration.CalculateInterceptCourse(targetPosition.To2D(), targetVelocity.To2D(), selfGround, projectileVelocity, projectile.GetComponent<Projectile>().Acceleration);
                targetPosition += offset.To3D() * 1.01f;
            }

            Vector3 rotationToTarget = Quaternion.LookRotation(targetPosition - transform.position).eulerAngles;
            turretRotation = rotationToTarget.y;
            cannonAngle = -rotationToTarget.x;
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

    internal float? GetCurrentRange()
        => Utilities.Ballistics.CalculateX(CannonLocalRotation * Mathf.Deg2Rad, projectileVelocity, ShootHeight);

    internal Vector3? PredictImpact()
    {
        if (!IsBallisticProjectile)
        {
            return (projectileVelocity * CurrentProjectileLifetime * shootPosition.forward) + shootPosition.position;
        }
        else
        {

            float? x_ = Utilities.Ballistics.CalculateX(CannonLocalRotation * Mathf.Deg2Rad, projectileVelocity, ShootHeight);
            if (!x_.HasValue) return null;
            float x = x_.Value;

            Vector3 turretRotation = transform.rotation * Vector3.back;
            Vector3 point = (shootPosition.position - x * turretRotation);
            point.y = 0f;

            if (CurrentProjectileLifetime > 0f)
            {
                Vector2 displacement = Utilities.Ballistics.Displacement(CannonLocalRotation * Mathf.Deg2Rad, projectileVelocity, CurrentProjectileLifetime);
                float x2 = displacement.x;
                if (x2 < x)
                {
                    point = (shootPosition.position - x2 * turretRotation);
                    point.y = displacement.y;
                }
            }

            point.y = Mathf.Max(point.y, TheTerrain.Height(point).y);

            return point;
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
    internal bool Shoot(RequiredShoots requiedShoots, float impactTime)
    {
        if (reload > 0f) return false;
        if (CurrentProjectile == null) return false;

        if (IsServer)
        { OnShoot_ClientRpc(new Vector2(TurretLocalRotation, CannonLocalRotation)); }

        if (AudioSource != null &&
            ShootSound != null)
        { AudioSource.PlayOneShot(ShootSound); }

        reload = reloadTime;

        for (int i = 0; i < ShootEffectInstances.Length; i++)
        { ShootEffectInstances[i].Emit(); }

        GameObject newProjectile = GameObject.Instantiate(CurrentProjectile, shootPosition.position, shootPosition.rotation, ObjectGroups.Projectiles);
        if (newProjectile.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.velocity = shootPosition.forward * projectileVelocity;
        }

        if (newProjectile.TryGetComponent<Projectile>(out var _projectile))
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
        float range = Utilities.Ballistics.MaxRadius(projectileVelocity, ShootHeight);
        if (CurrentProjectileLifetime > 0f)
        { range = Mathf.Min(range, Utilities.Ballistics.DisplacementX(45f * Mathf.Deg2Rad, projectileVelocity, ProjectileLifetime)); }
        this.Range = range;
        return range;
    }

    void OnDrawGizmos()
    {
        if (ShowRadius)
        {
            float? r2 = Utilities.Ballistics.CalculateX(CannonLocalRotation * Mathf.Deg2Rad, projectileVelocity, ShootHeight);
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
        this.target.Value = point;
    }
}
