using System;
using System.Collections;
using Game.Managers;
using Maths;
using UnityEngine;
using Utilities;

namespace Game.Components
{
    public class Projectile : MonoBehaviour
    {
        struct TrailData
        {
            internal Transform Parent;
            internal Vector3 LocalPosition;
        }

        [Serializable]
        struct ImpactEffect
        {
            [SerializeField] internal string MaterialID;

            [SerializeField] internal AudioClip[] Sounds;

            internal readonly AudioClip GetRandomSound()
            {
                if (Sounds.Length == 0) return null;
                if (Sounds.Length == 1) return Sounds[0];
                return Sounds[UnityEngine.Random.Range(0, Sounds.Length - 1)];
            }

            [Header("Particles")]
            [SerializeField] internal GameObject Particles;
            [SerializeField] internal bool UseNormal;

            [Header("Hoe")]
            [SerializeField] internal GameObject Hoe;
        }

        [SerializeField] bool Register;
        [SerializeField, ReadOnly] AudioSource AudioSource;
        [SerializeField] bool Instant;

        [Header("Movement")]
        [SerializeField] internal float Acceleration = 0f;

        [Header("Impact")]
        [SerializeField] float ImpactForce = 1f;
        [SerializeField] internal float ImpactDamage = 1f;
        [SerializeField] bool CanBounce = true;
        [SerializeField, Range(0f, 90f)] float BounceAngle = 50f;

        [Header("Explosion")]
        [SerializeField] float ExploisonForce = 0f;
        [SerializeField] float ExploisonRadius = 0f;
        [SerializeField] internal float ExplosionDamage = 0f;
        [SerializeField] bool ExplodeOnImpact = false;
        [SerializeField] bool ExplodeWhenExpires = false;

        [Header("Effects")]
        [SerializeField] GameObject ExploisonEffect;
        [SerializeField] AudioClip ImpactSound;
        [SerializeField] GameObject impactEffect;
        [SerializeField] ImpactEffect[] ImpactEffects = new ImpactEffect[0];
        [SerializeField] GameObject trail;
        TrailData trailData;

        [SerializeField] GameObject RicochetEffect;

        [SerializeField] float rotationSpeedX;
        [SerializeField] float rotationSpeedY;
        [SerializeField] float rotationSpeedZ;

        [SerializeField] bool RandomRotation = false;

        [SerializeField] Transform RotateThis;

        [Header("Stuff")]
        [SerializeField] LayerMask HitLayerMask;
        [SerializeField] bool DieInWater = true;
        [SerializeField] bool DestroyInExploisons = false;
        [SerializeField, ReadOnly, NonReorderable] internal Transform[] ignoreCollision;
        [SerializeField, ReadOnly] internal float LifeLeft;
        [SerializeField, ReadOnly] internal float Lifetime;
        [SerializeField, ReadOnly] internal bool InfinityLifetime = true;

        [SerializeField, ReadOnly] Rigidbody rb;

        [SerializeField, ReadOnly] internal int OwnerTeamHash;
        [SerializeField, ReadOnly] internal BaseObject Owner;

        [SerializeField, Range(0f, 1f)] internal float PropabilityOfProjectileIntersection = .5f;

        [SerializeField] bool DamageAllies = false;

        internal Rigidbody Rigidbody => rb;

        internal Vector3 Position => rb == null ? transform.position : rb.position + (rb.linearVelocity * Time.fixedDeltaTime);

        internal Maths.Ballistics.Trajectory Shot;

        [SerializeField, ReadOnly] bool destroyed = false;

        readonly RaycastHit[] hits = new RaycastHit[5];
        [SerializeField, ReadOnly] internal Vector3 lastPosition;
        [SerializeField, ReadOnly] internal Vector3 positionDelta;
        [SerializeField, ReadOnly] internal Vector3 TargetPosition;

        int ticksUntilTrailClear = 0;

        void OnEnable()
        {
            if (Register) RegisteredObjects.Projectiles.Add(this);

            destroyed = false;
            AttachTrail();

            if (RandomRotation)
            {
                Quaternion randomRotation = Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(0f, 360f)));
                if (RotateThis != null)
                { RotateThis.rotation = randomRotation; }
                else
                { transform.rotation = randomRotation; }
            }

            if (Instant)
            { StartCoroutine(InstantHit()); }
        }
        void OnDisable()
        {
            if (Register) RegisteredObjects.Projectiles.Remove(this);
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            AudioSource = GetComponentInChildren<AudioSource>();
        }

        void Start()
        {
            if (trail != null)
            {
                trailData = new TrailData()
                {
                    Parent = trail.transform.parent,
                    LocalPosition = trail.transform.localPosition,
                };
            }
        }

        IEnumerator InstantHit()
        {
            yield return new WaitForFixedUpdate();

            Vector3 startPosition = transform.position;
            int hitCount = Physics.RaycastNonAlloc(startPosition, transform.forward, hits, 500f, HitLayerMask);

            int closestI = -1;
            float closestD = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                if (!hits[i].collider.gameObject.HasComponent<Projectile>())
                {
                    if (hits[i].collider.isTrigger &&
                        (hits[i].collider.gameObject.name != "Water" || !DieInWater))
                    { continue; }
                }
                else if (PropabilityOfProjectileIntersection <= 0f)
                { continue; }

                if (hits[i].transform == transform)
                { continue; }

                if (ignoreCollision.Contains(hits[i].transform))
                { continue; }

                if (closestI == -1)
                {
                    closestI = i;
                }
                else
                {
                    float d = (lastPosition - hits[i].point).sqrMagnitude;
                    if (d < closestD)
                    {
                        closestD = d;
                        closestI = i;
                    }
                }
            }

            Vector3 point;
            if (closestI != -1)
            {
                point = hits[closestI].point;
            }
            else
            {
                point = transform.position + transform.forward * 500f;
            }
            transform.position = point;

            if (trail != null)
            {
                if (trail.TryGetComponent(out TrailRenderer trailRenderer) &&
                    QualityHandler.EnableProjectileTrails)
                {
                    trailRenderer.SetPositions(new Vector3[] { startPosition, point });
                }

                if (trail.TryGetComponent(out LineRenderer lineRenderer) &&
                    QualityHandler.EnableProjectileTrails)
                {
                    lineRenderer.SetPositions(new Vector3[] { startPosition, point });
                }
            }

            if (closestI != -1)
            {
                if (Impact(hits[closestI].point, hits[closestI].normal, hits[closestI].collider))
                {

                }
            }
        }

        void FixedUpdate()
        {
            if (destroyed) return;

            if (ticksUntilTrailClear > 0 && ticksUntilTrailClear != int.MaxValue)
            {
                ticksUntilTrailClear--;

                if (ticksUntilTrailClear <= 0)
                {
                    ticksUntilTrailClear = int.MaxValue;

                    if (trail != null &&
                        trail.TryGetComponent(out TrailRenderer trailRenderer))
                    {
                        trailRenderer.Clear();
                        trailRenderer.emitting = QualityHandler.EnableProjectileTrails;
                    }
                }
            }

            Lifetime += Time.fixedDeltaTime;

            if (Acceleration != 0f && rb != null)
            {
                rb.linearVelocity += Acceleration * Time.fixedDeltaTime * rb.linearVelocity.normalized;
            }

            CheckImpact(lastPosition);

            positionDelta = transform.position - lastPosition;
            lastPosition = transform.position;

            if (rotationSpeedX != 0f ||
                rotationSpeedY != 0f ||
                rotationSpeedZ != 0f)
            {
                if (RotateThis != null)
                { RotateThis.Rotate(rotationSpeedX * Time.fixedDeltaTime, rotationSpeedY * Time.fixedDeltaTime, rotationSpeedZ * Time.fixedDeltaTime, Space.Self); }
                else
                { transform.Rotate(rotationSpeedX * Time.fixedDeltaTime, rotationSpeedY * Time.fixedDeltaTime, rotationSpeedZ * Time.fixedDeltaTime, Space.Self); }
            }
            else
            {
                Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, positionDelta.normalized);
                if (positionDelta != default) transform.rotation = rotation;
            }

            if (!InfinityLifetime)
            {
                LifeLeft -= Time.fixedDeltaTime;
                if (LifeLeft <= 0f)
                {
                    NotifyImpact(Position);

                    if (ExplodeWhenExpires)
                    {
                        Vector3 position = transform.position;

                        if (TargetPosition != default && (TargetPosition - position).sqrMagnitude < 2f)
                        { position = TargetPosition; }

                        if (ExploisonEffect != null && QualityHandler.EnableParticles) GameObject.Instantiate(ExploisonEffect, position, Quaternion.identity, ObjectGroups.Effects);
                        Explode(position, 0f);
                    }

                    gameObject.SetActive(false);
                    OwnerTeamHash = -1;
                    Owner = null;
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (destroyed) return;
            if (Lifetime <= 0f) return;

            if (!other.gameObject.HasComponent<Projectile>())
            {
                if (other.gameObject.name != "Water" || !DieInWater)
                {
                    return;
                }
            }
            else if (PropabilityOfProjectileIntersection <= 0f)
            { return; }

            if (ignoreCollision.Contains(other.transform)) return;
            Vector3 point = other.ClosestPointOnBounds(Position);
            Vector3 normal = Vector3.up;
            Impact(point, normal, other);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (destroyed) return;
            if (collision.collider.isTrigger)
            {
                if (!collision.gameObject.HasComponent<Projectile>())
                {
                    if (collision.gameObject.name != "Water" || !DieInWater)
                    { return; }
                }
                else if (PropabilityOfProjectileIntersection <= 0f)
                { return; }
            }
            if (ignoreCollision.Contains(collision.transform)) return;
            Vector3 point;
            Vector3 normal;
            if (collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);
                point = contact.point;
                normal = contact.normal;
            }
            else
            {
                point = collision.collider.ClosestPointOnBounds(Position); ;
                normal = Vector3.up;
            }
            Impact(point, normal, collision.collider);
        }

        bool CheckImpact(Vector3 lastPosition)
        {
            Debug3D.DrawPoint(lastPosition, .5f, Color.gray);
            Debug3D.DrawPoint(transform.position, .5f, Color.white);

            Debug.DrawRay(lastPosition, positionDelta, Color.white, Time.fixedDeltaTime);

            int hitCount = Physics.RaycastNonAlloc(lastPosition, positionDelta, hits, positionDelta.magnitude, HitLayerMask);

            if (hitCount <= 0)
            { return false; }

            int closestI = -1;
            float closestD = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                ref RaycastHit hit = ref hits[i];

                if (hit.collider.gameObject.HasComponent<Projectile>())
                {
                    if (PropabilityOfProjectileIntersection <= 0f)
                    { continue; }
                }
                else
                {
                    if (hit.collider.isTrigger &&
                        (hit.collider.gameObject.name != "Water" || !DieInWater))
                    { continue; }
                }

                if (hit.transform == transform)
                { continue; }

                if (ignoreCollision.Contains(hit.transform))
                { continue; }

                if (closestI == -1)
                {
                    closestI = i;
                }
                else
                {
                    float d = (lastPosition - hit.point).sqrMagnitude;
                    if (d < closestD)
                    {
                        closestD = d;
                        closestI = i;
                    }
                }
            }

            if (closestI == -1)
            { return false; }

            if (!Impact(hits[closestI].point, hits[closestI].normal, hits[closestI].collider))
            { return false; }

            return true;
        }

        bool Impact(Vector3 at, Vector3 normal, Collider obj, bool force = false)
        {
            if (destroyed) return true;
            Debug.DrawLine(at, at + normal, Color.red, 5f);
            Debug3D.DrawPoint(at, .2f, Color.red, 5f);
            if (ExplosionDamage > 0f && ExploisonRadius > 0f)
            { Debug3D.DrawSphere(at, ExploisonRadius, Color.red, 5f); }

            bool hasMaterial = obj.gameObject.TryGetComponent(out IObjectMaterial material);

            float fuckYouValue = obj.TryGetComponent(out BaseObject _baseObj) ? TeamManager.Instance.GetFuckYou(_baseObj.TeamHash, this.OwnerTeamHash) : 0f;

            if (obj.TryGetComponent(out Projectile otherProjectile))
            {
                if (!force)
                {
                    if (PropabilityOfProjectileIntersection == 0f)
                    { return false; }

                    if (PropabilityOfProjectileIntersection != 1f && UnityEngine.Random.value > PropabilityOfProjectileIntersection)
                    { return false; }
                }

                destroyed = true;
                otherProjectile.Impact(at, -normal, GetComponent<Collider>(), true);
            }
            else if (CanBounce && (!hasMaterial || material.Hardness > float.Epsilon))
            {
                float normalAngle = Vector3.Angle(rb.linearVelocity, -normal);
                float impactAngle = 90f - Math.Clamp(normalAngle, 0f, 90f);

                float modifiedImpactAngle = impactAngle;
                if (hasMaterial)
                { modifiedImpactAngle /= material.Hardness; }

                if (modifiedImpactAngle < BounceAngle && rb != null)
                {
                    float impactEnergy = MathF.Sin(impactAngle * Rotation.Deg2Rad);
                    float remainingEnergy = 1f - impactEnergy;

                    if ((DamageAllies || fuckYouValue >= 0f) && ImpactForce != 0f && obj.attachedRigidbody != null)
                    { obj.attachedRigidbody.AddForceAtPosition(ImpactForce * impactEnergy * transform.forward, at, ForceMode.Impulse); }

                    Vector3 newVelocity = Vector3.Reflect(rb.linearVelocity * remainingEnergy, normal);
                    rb.linearVelocity = newVelocity;

                    if (RicochetEffect != null &&
                        QualityHandler.EnableParticles)
                    { GameObject.Instantiate(RicochetEffect, at, Quaternion.LookRotation(newVelocity, Vector3.up), ObjectGroups.Effects); }

                    return false;
                }
            }

            DetachTrail();

            TakeDamage(obj, at, fuckYouValue);

            bool impactEffectCreated = false;

            if (ExplodeOnImpact)
            { impactEffectCreated = impactEffectCreated || Explode(at, hasMaterial ? material.BlastAbsorptionCapacity : 0f); }

            destroyed = true;

            if (!impactEffectCreated)
            { impactEffectCreated = CreateImpactEffect(obj.gameObject, at, normal, material); }

            NotifyImpact(at);

            if (!impactEffectCreated && impactEffect != null && QualityHandler.EnableParticles) GameObject.Instantiate(impactEffect, at, Quaternion.FromToRotation(Vector3.up, normal), ObjectGroups.Effects);

            gameObject.SetActive(false);
            OwnerTeamHash = -1;
            Owner = null;

            return true;
        }

        void DetachTrail()
        {
            if (trail == null) return;
            trail.transform.SetParent(ObjectGroups.Effects);

            if (trail.TryGetComponent(out TrailRenderer trailRenderer))
            { trailRenderer.emitting = false; }

            if (trail.TryGetComponent(out ParticleSystem particleSystem))
            {
                ParticleSystem.EmissionModule emission = particleSystem.emission;
                emission.enabled = false;
            }
        }

        void AttachTrail()
        {
            if (trail == null) return;
            ticksUntilTrailClear = 1;

            trail.SetActive(true);

            if (trail.TryGetComponent(out TrailRenderer trailRenderer))
            {
                trailRenderer.Clear();
                trailRenderer.emitting = false;
            }

            if (trail.TryGetComponent(out LineRenderer lineRenderer))
            {
                lineRenderer.SetPositions(new Vector3[0]);
            }

            if (trailData.Parent == null ||
                trail.transform.parent.gameObject != trailData.Parent.gameObject)
            { trail.transform.SetParent(transform); }

            trail.transform.localPosition = trailData.LocalPosition;

            if (trail.TryGetComponent(out ParticleSystem particleSystem))
            {
                ParticleSystem.EmissionModule emission = particleSystem.emission;
                emission.enabled = QualityHandler.EnableParticles;
            }
        }

        void TakeDamage(Collider other, Vector3 at, float fuckYouValue)
        {
            if (fuckYouValue < 0f && !DamageAllies)
            { return; }

            if (ImpactForce != 0f && other.attachedRigidbody != null)
            { other.attachedRigidbody.AddForceAtPosition(transform.forward * ImpactForce, at, ForceMode.Impulse); }

            if (ImpactDamage <= 0f)
            { return; }

            if (other.gameObject.TryGetComponentInParent(out IDetailedDamageable detailedDamageable))
            {
                detailedDamageable.Damage(ImpactDamage, this);
                NotifyDamage((at, ImpactDamage, DamageKind.Physical));
                return;
            }

            if (other.gameObject.TryGetComponentInParent(out IDamageable damageable))
            {
                damageable.Damage(ImpactDamage);
                NotifyDamage((at, ImpactDamage, DamageKind.Physical));
                return;
            }
        }

        bool CreateImpactEffect(GameObject other, Vector3 at, Vector3 normal, IObjectMaterial material)
        {
            if (ImpactEffects == null ||
                ImpactEffects.Length == 0 ||
                material == null)
            { return false; }

            bool impactEffectCreated = false;

            string m = material.Material;
            for (int i = 0; i < ImpactEffects.Length; i++)
            {
                if (ImpactEffects[i].MaterialID == m)
                {
                    if (ImpactEffects[i].Particles != null)
                    {
                        if (QualityHandler.EnableParticles)
                        {
                            Vector3 direction = ImpactEffects[i].UseNormal ? normal : Vector3.up;

                            if (other.HasComponent<TerrainCollider>())
                            { direction = Vector3.up; }

                            GameObject.Instantiate(ImpactEffects[i].Particles, at, Quaternion.LookRotation(Vector3.up, direction), ObjectGroups.Effects);
                        }

                        impactEffectCreated = true;
                    }

                    if (ImpactEffects[i].Hoe != null &&
                        QualityHandler.EnableParticles)
                    { GameObject.Instantiate(ImpactEffects[i].Hoe, at, Quaternion.LookRotation(normal, Vector3.up), other.transform); }

                    AudioClip sound = ImpactEffects[i].GetRandomSound();
                    if (sound != null)
                    {
                        AudioSource.PlayClipAtPoint(sound, transform.position);

                        /*
                        if (AudioSource == null)
                        {
                            Debug.LogWarning($"[{nameof(Projectile)}]: {nameof(AudioSource)} is null", this);
                        }
                        else
                        {
                            AudioSource.PlayClipAtPoint(sound, transform.position);
                        }
                        */
                    }
                }
            }

            return impactEffectCreated;
        }

        bool Explode(Vector3 origin, float absorbed)
        {
            if (ExploisonRadius <= 0f || destroyed)
            { return false; }

            destroyed = true;

            if (absorbed < 1f)
            {
                Collider[] objectsInRange = Physics.OverlapSphere(origin, ExploisonRadius);

                for (int i = 0; i < objectsInRange.Length; i++)
                {
                    Collider objectCollider = objectsInRange[i];

                    if (objectCollider.gameObject == this.gameObject) continue;

                    if (objectCollider.isTrigger)
                    {
                        if (objectCollider.gameObject.TryGetComponent(out Projectile otherProjectile))
                        { otherProjectile.OnOtherExplosion(); }
                        else
                        { continue; }
                    }

                    float fuckYouValue = objectCollider.gameObject.TryGetComponent(out BaseObject _baseObj) ? TeamManager.Instance.GetFuckYou(_baseObj.TeamHash, this.OwnerTeamHash) : 0f;

                    if (fuckYouValue < 0f && !DamageAllies)
                    { continue; }

                    if (objectCollider.attachedRigidbody != null) objectCollider.attachedRigidbody.AddExplosionForce(ExploisonForce * (1f - absorbed), origin, ExploisonRadius, 1f, ForceMode.Impulse);

                    GameObject @object = objectCollider.gameObject;

                    float distance = Math.Max(1f, Vector3.Distance(@object.transform.position, origin));
                    float amount = (ExplosionDamage * (1f - absorbed)) / distance;

                    if (@object.TryGetComponent(out IDetailedDamageable detailedDamageable))
                    {
                        detailedDamageable.Damage(amount, this);
                        NotifyDamage((@object.transform.position, amount, DamageKind.Explosive));
                    }
                    else if (@object.TryGetComponent(out IDamageable damageable))
                    {
                        damageable.Damage(amount);
                        NotifyDamage((@object.transform.position, amount, DamageKind.Explosive));
                    }
                }
            }

            if (ExploisonEffect != null)
            {
                if (QualityHandler.EnableParticles) GameObject.Instantiate(ExploisonEffect, origin, Quaternion.identity, ObjectGroups.Effects);
                return true;
            }

            return false;
        }

        private void OnOtherExplosion()
        {
            if (!DestroyInExploisons) return;
            if (destroyed) return;

            destroyed = true;

            if (trail != null)
            {
                trail.transform.SetParent(ObjectGroups.Effects);
                if (trail.TryGetComponent(out TrailRenderer trailRenderer))
                { trailRenderer.emitting = false; }
            }

            if (ExploisonRadius > 0f)
            { Explode(Position, 0f); }

            NotifyImpact(Position);

            if (impactEffect != null && QualityHandler.EnableParticles)
            { GameObject.Instantiate(impactEffect, Position, Quaternion.FromToRotation(transform.forward, Vector3.up), ObjectGroups.Effects); }

            gameObject.SetActive(false);
            OwnerTeamHash = -1;
            Owner = null;
        }

        void NotifyImpact(Vector3 at)
        {
            if (Owner == null) return;
            if (Owner is not UnitAttacker owner) return;

            owner.Turret.NotifyImpact(this, at);
        }

        void NotifyDamage(params (Vector3 Position, float Amount, DamageKind Kind)[] damages)
        {
            if (Owner == null) return;
            if (Owner is not UnitAttacker owner) return;

            owner.Turret.NotifyDamage(damages);
        }
    }
}
