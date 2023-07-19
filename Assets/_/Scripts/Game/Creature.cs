using Game.Managers;

using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

using Utilities;

namespace Game.Components
{
    public class Creature : NetworkBehaviour, IDamagable
    {
        [SerializeField] internal string CreatureName;

        [SerializeField, ReadOnly] Collider Collider;
        [SerializeField, ReadOnly] protected Rigidbody rb;

        [SerializeField, ReadOnly] protected Vector3 Destination;

        [SerializeField] internal GameObject ExplodeEffect;
        [SerializeField] internal float HP;
        float _maxHp;

        internal float NormalizedHP => HP / _maxHp;

        [SerializeField, ReadOnly] protected bool IsGrounded;
        [SerializeField, ReadOnly] float FlippedOverValue;
        [SerializeField, ReadOnly] protected bool InWater;
        [SerializeField, ReadOnly] float NextGroundCheck = .5f;
        [SerializeField, ReadOnly] protected float Bored = 4f;

        [SerializeField, Min(0)] protected float Speed;
        [SerializeField, Min(0)] protected float FleeSpeed;

        [SerializeField] string[] FoodCreatures = new string[0];

        protected bool IsFlippedOver => FlippedOverValue < .5f;

        readonly Collider[] GroundHits = new Collider[1];

        [SerializeField, ReadOnly] protected float FleeCooldown = 0f;
        [SerializeField, ReadOnly] protected float NextFleePosition = 0f;

        [Header("Sounds")]
        [SerializeField] Vector2 IdleSoundInterval = new Vector2(3f, 9f);
        [SerializeField, ReadOnly] float IdleSoundTimer = 6f;
        [SerializeField] AudioClip[] IdleSounds = new AudioClip[0];
        [SerializeField] AudioClip[] DamageSounds = new AudioClip[0];
        [SerializeField, ReadOnly] protected AudioSource AudioSource;

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

        protected Creature FindFood()
        {
            if (FoodCreatures.Length == 0) return null;
            Creature[] creatures = FindObjectsOfType<Creature>(false);
            List<Creature> filteredCreatures_ = new();
            foreach (Creature creature in creatures)
            {
                foreach (string food in FoodCreatures)
                {
                    if (creature.CreatureName == food)
                    {
                        filteredCreatures_.Add(creature);
                        break;
                    }
                }
            }
            Creature[] filteredCreatures = filteredCreatures_.ToArray();
            if (filteredCreatures.Length == 0) return null;

            (int, float) closest = filteredCreatures.ClosestI(transform.position);
            return filteredCreatures[closest.Item1];
        }

        protected virtual void Start()
        {
            Collider = GetComponent<Collider>();
            rb = GetComponent<Rigidbody>();
            _maxHp = HP == 0f ? 1f : HP;

            IdleSoundTimer = Random.Range(IdleSoundInterval.x, IdleSoundInterval.y);

            AudioSource = GetComponent<AudioSource>();
        }

        public override void OnDestroy()
        {
            if (gameObject.scene.isLoaded && ExplodeEffect != null)
            { GameObject.Instantiate(ExplodeEffect, transform.position, Quaternion.identity, ObjectGroups.Effects); }

            base.OnDestroy();
        }

        void FixedUpdate()
        {
            if (NextGroundCheck > 0f)
            { NextGroundCheck -= Time.fixedDeltaTime; }
            else
            {
                NextGroundCheck = 2f;
                IsGrounded = Grounded();
            }

            if (IdleSoundTimer > 0f)
            {
                IdleSoundTimer -= Time.fixedDeltaTime;
            }
            else
            {
                IdleSoundTimer = Random.Range(IdleSoundInterval.x, IdleSoundInterval.y);
                if (IdleSounds.Length > 0)
                {
                    int i = Random.Range(0, IdleSounds.Length - 1);
                    AudioSource.PlayOneShot(IdleSounds[i]);
                }
            }

            InWater = Collider.bounds.min.y <= WaterManager.WaterLevel;

            FlippedOverValue = Vector3.Dot(Vector3.up, transform.up);

            if (Collider.bounds.center.y <= WaterManager.WaterLevel)
            {
                rb.AddForce(Vector3.up * 3f, ForceMode.Force);
            }

            if (Collider.bounds.max.y <= WaterManager.WaterLevel)
            {
                rb.AddForce(Vector3.up * 8f, ForceMode.Force);
            }

            Thinking();
        }

        protected virtual Vector3 FindNewDestination()
        {
            Vector3 result = transform.position;
            result.x += Random.Range(-20, 20);
            result.z += Random.Range(-20, 20);

            result.z = TheTerrain.Height(result);

            return result;
        }

        protected virtual Vector3 FindFleeDestination()
        {
            Vector3 result = transform.position;
            result.x += Random.Range(-40, 40);
            result.z += Random.Range(-40, 40);

            result.z = TheTerrain.Height(result);

            return result;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent<VehicleEngine>(out var vehicle))
            {
                if (vehicle.rb.velocity.sqrMagnitude > .1f)
                {
                    float speedDiffSqr = (vehicle.rb.velocity - rb.velocity).sqrMagnitude;
                    if (speedDiffSqr > .5f)
                    {
                        Damage(speedDiffSqr);
                    }
                }
            }
        }

        protected virtual void Thinking()
        {
            if (IsFlippedOver) return;

            if (FleeCooldown > 0f)
            {
                FleeCooldown -= Time.fixedDeltaTime;

                if (NextFleePosition > 0f)
                {
                    NextFleePosition -= Time.fixedDeltaTime;
                }
                else
                {
                    NextFleePosition = 1f;
                    Destination = FindFleeDestination();
                }
            }
            else
            {
                if (Destination == Vector3.zero)
                {
                    Destination = FindNewDestination();
                    return;
                }

                if (Bored > 2f)
                {
                    Destination = FindNewDestination();
                    Bored = 0f;
                }
            }

            if (!IsGrounded && !InWater) return;

            Movement(Destination);
        }

        void Movement(Vector3 destination)
        {
            float distanceToDestination = Vector2.Distance(transform.position.To2D(), destination.To2D());

            if (distanceToDestination < 1f)
            {
                Bored += Time.fixedDeltaTime;
                return;
            }

            Vector3 localTarget = transform.InverseTransformPoint(destination);

            float deltaAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

            Vector3 deltaPosition = Speed * Time.deltaTime * transform.forward;

            if (FleeCooldown > 0)
            {
                deltaPosition = FleeSpeed * Time.deltaTime * transform.forward;
            }

            if (InWater)
            {
                deltaPosition *= .5f;
                deltaAngle *= .3f;
            }

            rb.MoveRotation(rb.rotation * Quaternion.Euler(70f * Time.deltaTime * new Vector3(0, deltaAngle, 0)));
            rb.MovePosition(rb.position + deltaPosition);
        }

        public virtual void Damage(float ammount)
        {
            HP -= ammount;
            if (HP <= 0f)
            {
                Destroy();
            }
            else
            {
                if (DamageSounds.Length > 0)
                {
                    int i = Random.Range(0, DamageSounds.Length - 1);
                    AudioSource.PlayOneShot(DamageSounds[i]);
                }

                FleeCooldown = 6f;
                NextFleePosition = 0f;
            }
        }

        void Destroy()
        {
            if (NetcodeUtils.IsOfflineOrServer)
            {
                GameObject.Destroy(gameObject);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, Destination);
            Gizmos.DrawWireSphere(Destination, .5f);
        }
    }
}
