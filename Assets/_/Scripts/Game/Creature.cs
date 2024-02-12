using System.Collections;
using System.Collections.Generic;
using Game.Managers;
using Unity.Netcode;
using UnityEngine;
using Utilities;

namespace Game.Components
{
    public class Creature : NetworkBehaviour, IDamageable
    {
        [SerializeField] internal string CreatureName;

        [SerializeField, ReadOnly] Collider Collider;
        [SerializeField, ReadOnly] protected Rigidbody rb;

        [SerializeField, ReadOnly] protected Vector3 Destination;

        [SerializeField] internal GameObject ExplodeEffect;
        [SerializeField] internal float HP;
        float _maxHp;

        [SerializeField, ReadOnly] protected bool IsSearchingForFood = false;

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
        [SerializeField] Vector2 IdleSoundInterval = new(3f, 9f);
        [SerializeField, ReadOnly] float IdleSoundTimer = 6f;
        [SerializeField] AudioClip[] IdleSounds = new AudioClip[0];
        [SerializeField] AudioClip[] DamageSounds = new AudioClip[0];
        [SerializeField, ReadOnly] protected AudioSource AudioSource;

        bool Grounded()
        {
            Vector3 rayOrigin = Collider.bounds.center - (transform.up * .2f);
            Vector3 raySize = Collider.bounds.size;

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
            Creature[] creatures = FindObjectsByType<Creature>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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

            (int, float) closest = filteredCreatures.Closest(transform.position);
            return filteredCreatures[closest.Item1];
        }

        protected void FindFoodAsync(System.Action<Creature> callback)
        {
            if (FoodCreatures.Length == 0) return;
            Creature[] creatures = FindObjectsByType<Creature>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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
            if (filteredCreatures.Length == 0) return;

            IsSearchingForFood = true;
            StartCoroutine(filteredCreatures.ClosestAsync(transform.position, (i, d) => callback.Invoke(filteredCreatures[i]), (i, d) => IsSearchingForFood = false));
        }

        protected virtual void Awake()
        {
            Collider = GetComponent<Collider>();
            rb = GetComponent<Rigidbody>();
            AudioSource = GetComponent<AudioSource>();
        }

        protected virtual void Start()
        {
            _maxHp = HP == 0f ? 1f : HP;
            IdleSoundTimer = Random.Range(IdleSoundInterval.x, IdleSoundInterval.y);
        }

        public override void OnDestroy()
        {
            if (gameObject.scene.isLoaded && ExplodeEffect != null && QualityHandler.EnableParticles)
            { GameObject.Instantiate(ExplodeEffect, transform.position, Quaternion.identity, ObjectGroups.Effects); }

            base.OnDestroy();
        }

        void Update()
        {
            if (NextGroundCheck > 0f)
            { NextGroundCheck -= Time.deltaTime; }
            else
            {
                NextGroundCheck = 2f;
                IsGrounded = Grounded();
            }

            if (IdleSoundTimer > 0f)
            {
                IdleSoundTimer -= Time.deltaTime;
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

            Thinking();
        }

        void FixedUpdate()
        {
            if (Collider.bounds.center.y <= WaterManager.WaterLevel)
            { rb.AddForce(Vector3.up * 3f, ForceMode.Force); }

            if (Collider.bounds.max.y <= WaterManager.WaterLevel)
            { rb.AddForce(Vector3.up * 8f, ForceMode.Force); }
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
            if (collision.gameObject.TryGetComponent(out VehicleEngine vehicle))
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

            if (!Flee())
            {
                if (Destination == default)
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

        protected bool Flee()
        {
            if (FleeCooldown <= 0f) return false;

            FleeCooldown -= Time.deltaTime;

            if (NextFleePosition > 0f)
            {
                NextFleePosition -= Time.deltaTime;
            }
            else
            {
                NextFleePosition = 1f;
                Destination = FindFleeDestination();
            }
            return true;
        }

        void Movement(Vector3 destination)
        {
            float distanceToDestination = Maths.Distance(transform.position.To2D(), destination.To2D());

            if (distanceToDestination < 1f)
            {
                Bored += Time.deltaTime;
                return;
            }

            Vector3 localTarget = transform.InverseTransformPoint(destination);

            float deltaAngle = Maths.Atan2(localTarget.x, localTarget.z) * Maths.Rad2Deg;

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

        public virtual void Damage(float amount)
        {
            HP -= amount;
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
            if (!NetcodeUtils.IsOfflineOrServer) return;


            GameObject.Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, .5f);
            Gizmos.DrawLine(transform.position, Destination);
            Gizmos.color = CoolColors.White;
            GizmosPlus.DrawPoint(Destination, 1f);
            Debug3D.Label(Destination, "Target");
        }
    }
}
