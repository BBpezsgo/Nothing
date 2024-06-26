using System;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace Game.Components
{
    public class UnitBehaviour_AvoidObstacles : UnitBehaviour_Base
    {
        const float MaxDistanceToThink = 200f;

        [SerializeField, ReadOnly] public Transform IgnoreCollision;
        [SerializeField, ReadOnly, NonReorderable] List<Collider> Obstacles = new(10);
        readonly Collider[] DetectedThings = new Collider[10];
        [SerializeField, ReadOnly] float TimeToCheck = .1f;

        const float DetectionRange = 10f;
        const float MaxDetectionCooldown = 5f;

        protected virtual void Start()
        {
            TimeToCheck = UnityEngine.Random.value + 1f;
        }

        void Update()
        {
            TimeToCheck -= Time.deltaTime;
        }

        public override Vector2? GetOutput()
        {
            if (TimeToCheck <= 0f)
            {
                DetectObstacles();
                if (MovementEngine.Velocity == default)
                {
                    TimeToCheck = MaxDetectionCooldown;
                }
                else
                {
                    TimeToCheck = Math.Clamp((DetectionRange * DetectionRange) / MovementEngine.Velocity.sqrMagnitude, .1f, MaxDetectionCooldown);
                }
            }

            float closest = float.MaxValue;

            Vector2? result = null;

            for (int i = 0; i < Obstacles.Count; i++)
            {
                Collider obstacle = Obstacles[i];

                if (obstacle == null ||
                    IgnoreCollision != null &&
                    obstacle.transform.IsChildOf(IgnoreCollision))
                { continue; }

                switch (Think(obstacle, ref closest, out Vector2 _result))
                {
                    case ThinkResult.Primary:
                        return _result;
                    case ThinkResult.Secondary:
                        result = _result;
                        break;
                    case ThinkResult.None:
                    default:
                        break;
                }
            }

            return result;
        }

        enum ThinkResult
        {
            None,
            Primary,
            Secondary,
        }

        ThinkResult Think(Collider obstacle, ref float closest, out Vector2 result)
        {
            result = default;

            Vector2 offset = (obstacle.transform.position - transform.position).To2D();

            if (offset.sqrMagnitude > MaxDistanceToThink * MaxDistanceToThink) return ThinkResult.None;

            Vector3 closestPoint = obstacle.ClosestPoint(transform.position);

            offset = (closestPoint - transform.position).To2D();

            float dot = Vector2.Dot(transform.forward.To2D(), offset.normalized);
            if (dot <= 0f) return ThinkResult.None;

            float distance = (closestPoint.To2D() - transform.position.To2D()).magnitude;

            // Start reverse
            if (dot > .6f && distance < 10f)
            {
                Debug.DrawLine(transform.position, closestPoint, Color.red, Time.deltaTime);

                result = new Vector2(-1f, -0.5f);
                return ThinkResult.Primary;
            }

            // Continue reverse
            if (dot > .45f && distance < 50f && MovementEngine.IsReverse)
            {
                Debug.DrawLine(transform.position, closestPoint, Color.red, Time.deltaTime);

                result = new Vector2(-1f, -0.35f);
                return ThinkResult.Primary;
            }

            if (closest < distance) return ThinkResult.None;
            closest = distance;

            float lr = Vector2.SignedAngle(transform.forward, offset) / 180;

            float steering = 1f - Math.Abs(lr);
            if (lr < 0) steering = -steering;

            steering *= 1f - dot;

            Debug.DrawLine(transform.position, closestPoint, Color.yellow, Time.deltaTime);

            result = new Vector2(steering, .5f);

            return ThinkResult.Secondary;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Obstacles.Contains(collision.collider))
            { return; }
            if (collision.gameObject.layer != LayerMask.NameToLayer(LayerMaskNames.Default))
            { return; }
            Obstacles.Add(collision.collider);
        }

        void DetectObstacles()
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, DetectionRange, DetectedThings);

            Debug3D.DrawSphere(transform.position, DetectionRange, Color.white, Time.deltaTime);

            Obstacles.Clear();

            for (int i = 0; i < hits; i++)
            {
                Collider hit = DetectedThings[i];
                if (hit.gameObject.layer != LayerMask.NameToLayer(LayerMaskNames.Default)) continue;
                if (hit.gameObject == gameObject) continue;

                // Debug.DrawLine(transform.position, DetectedThings[i].transform.position, Color.red, Time.deltaTime);

                Obstacles.Add(hit);
            }
        }

        public override void DrawGizmos()
        {
            base.DrawGizmos();

            for (int i = 0; i < Obstacles.Count; i++)
            {
                Collider obstacle = Obstacles[i];

                if (obstacle == null)
                { continue; }

                if (IgnoreCollision != null &&
                    obstacle.transform.IsChildOf(IgnoreCollision))
                {
                    Gizmos.color = Maths.CoolColors.Orange;
                    Gizmos.DrawWireCube(obstacle.bounds.center, obstacle.bounds.size);
                    continue;
                }

                Gizmos.color = Maths.CoolColors.Red;
                Gizmos.DrawWireCube(obstacle.bounds.center, obstacle.bounds.size);
            }
        }
    }
}
