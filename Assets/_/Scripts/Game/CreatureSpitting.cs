using System.Collections;

using Unity.Netcode;

using UnityEngine;

using Utilities;

namespace Game.Components
{
    public class CreatureSpitting : Creature
    {
        [SerializeField] AudioClip SpitSound;

        [SerializeField] Transform SpitPosition;
        [SerializeField] GameObject Spit;
        [SerializeField, Min(0)] float SpitCooldown;
        [SerializeField, Min(0)] float SpitVelocity = 15f;
        [SerializeField, ReadOnly] float currentSpitCooldown;
        [SerializeField, ReadOnly] Creature Food;

        [SerializeField, Min(0)] float SpitDelay = 0f;

        protected override void Thinking()
        {
            if (currentSpitCooldown > 0f)
            {
                currentSpitCooldown -= Time.fixedDeltaTime;
            }

            if (!Flee())
            {
                if (Food == null)
                {
                    if (!IsSearchingForFood)
                    { FindFoodAsync(v => Food = v); }
                    base.Thinking();
                    return;
                }
                else
                {
                    Destination = Food.transform.position;
                }
            }

            if (!IsGrounded && !InWater) return;
            if (IsFlippedOver) return;

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

            Movement(Destination);
        }

        IEnumerator SpitToDelayed(GameObject target, float delaySec)
        {
            if (currentSpitCooldown > 0f) yield break;

            currentSpitCooldown = SpitCooldown;

            yield return new WaitForSeconds(delaySec);

            if (target == null) yield break;

            SpitTo(target, true);
        }
        void SpitTo(GameObject target, bool force = false)
        {
            if (currentSpitCooldown > 0f && !force) return;
            currentSpitCooldown = SpitCooldown;

            Vector2 selfGround = transform.position.To2D();
            float cannonLength = Vector2.Distance(selfGround, SpitPosition.position.To2D());

            Vector3 targetPosition = target.transform.position;

            Vector3 targetVelocity = Vector3.zero;
            if (target.TryGetComponent(out Rigidbody targetRigidbody))
            { targetVelocity = targetRigidbody.velocity; }
            if (targetVelocity.To2D().sqrMagnitude > .1f)
            {
                Vector2 offset = Velocity.CalculateInterceptCourse(targetPosition.To2D(), targetVelocity.To2D(), selfGround, SpitVelocity);
                targetPosition += offset.To3D() * 1.01f;
            }

            targetPosition += Vector2.ClampMagnitude(selfGround - targetPosition.To2D(), cannonLength).To3D();

            targetPosition += new Vector3(0f, .5f, 0f);

            if (NetcodeUtils.IsOfflineOrServer)
            {
                SpitTo(targetPosition);

                if (NetcodeUtils.IsServer)
                { SpitTo_ClientRpc(targetPosition); }
            }
        }
        void SpitTo(Vector3 targetPosition)
        {
            float? theta_ = Ballistics.AngleOfReach2(SpitVelocity, SpitPosition.position, targetPosition);

            float angle;
            if (!theta_.HasValue)
            { angle = 45f; }
            else
            { angle = theta_.Value * Mathf.Rad2Deg; }

            float rotation = Quaternion.LookRotation(targetPosition - transform.position).eulerAngles.y;

            SpitPosition.rotation = Quaternion.Euler(-angle, rotation, 0f);

            GameObject spit = GameObject.Instantiate(Spit, SpitPosition.position, Quaternion.identity, ObjectGroups.Projectiles);
            if (spit.TryGetComponent(out Rigidbody spitRigidbody))
            {
                spitRigidbody.AddForce(SpitVelocity * SpitPosition.forward, ForceMode.Impulse);
            }
            if (spit.TryGetComponent(out Projectile projectile))
            {
                projectile.ignoreCollision = new Transform[]
                {
                transform,
                };
            }
        }

        [ClientRpc]
        void SpitTo_ClientRpc(Vector3 target)
        {
            SpitTo(target);
        }

        [ClientRpc]
        void SpitSound_ClientRpc()
        {
            AudioSource.PlayOneShot(SpitSound);
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

            if (InWater)
            {
                deltaAngle *= .3f;
            }

            rb.MoveRotation(rb.rotation * Quaternion.Euler(70f * Time.deltaTime * new Vector3(0, deltaAngle, 0)));

            if (Food != null)
            {
                if (distanceToDestination < 11f)
                {
                    if (currentSpitCooldown <= 0f)
                    {
                        if (AudioSource != null && SpitSound != null)
                        {
                            if (NetcodeUtils.IsOfflineOrServer)
                            {
                                AudioSource.PlayOneShot(SpitSound);
                                if (!NetcodeUtils.IsServer)
                                { SpitSound_ClientRpc(); }
                            }
                        }
                    }

                    if (SpitDelay <= 0f)
                    {
                        SpitTo(Food.gameObject);
                    }
                    else
                    {
                        StartCoroutine(SpitToDelayed(Food.gameObject, SpitDelay));
                    }
                }

                if (distanceToDestination < 9f)
                {
                    return;
                }
            }

            Vector3 deltaPosition = Speed * Time.deltaTime * transform.forward;

            if (FleeCooldown > 0)
            {
                deltaPosition = FleeSpeed * Time.deltaTime * transform.forward;
            }

            if (InWater)
            {
                deltaPosition *= .5f;
            }

            rb.MovePosition(rb.position + deltaPosition);
        }
    }
}
