using AssetManager;

using System.Collections.Generic;

using UnityEngine;

using Utilities;

namespace Game.Components
{
    public class UnitBehaviour_AvoidObstacles : UnitBehaviour_Base, IHaveAssetFields, ICopiable<UnitBehaviour_AvoidObstacles>
    {
        [SerializeField, ReadOnly] internal Transform IgnoreCollision;
        [SerializeField, ReadOnly] RaycastHitCache raycastHitCache;
        [SerializeField, ReadOnly, NonReorderable] Collider[] DetectedThings = new Collider[10];
        [SerializeField, ReadOnly] float TimeToCheck = .1f;

        const float DetectionRange = 10f;
        const float MaxDetectionCooldown = 5f;

        [System.Serializable]
        struct RaycastHitCache
        {
            [SerializeField, ReadOnly, NonReorderable] internal List<Collider> Obstacles;
        }

        protected override void Start()
        {
            base.Start();
            TimeToCheck = Random.value + 1f;
            raycastHitCache = new RaycastHitCache()
            {
                Obstacles = new List<Collider>(),
            };
        }

        static bool IsChildOf(Transform child, Transform parent)
        {
            if (child == parent) return true;
            if (child.parent == null) return false;
            if (child.parent == parent) return true;
            return IsChildOf(child.parent, parent);
        }

        internal override Vector2? GetOutput()
        {
            if (TimeToCheck <= 0f)
            {
                DetectObstacles();
                if (MovementEngine.Velocity == Vector3.zero)
                {
                    TimeToCheck = MaxDetectionCooldown;
                }
                else
                {
                    TimeToCheck = Mathf.Clamp((DetectionRange * DetectionRange) / MovementEngine.Velocity.sqrMagnitude, .1f, MaxDetectionCooldown);
                }
            }

            float closest = float.MaxValue;

            Vector2? result = null;

            for (int i = 0; i < raycastHitCache.Obstacles.Count; i++)
            {
                Collider obstacle = raycastHitCache.Obstacles[i];
                if (obstacle == null) continue;
                if (IgnoreCollision != null && IsChildOf(obstacle.transform, IgnoreCollision))
                { continue; }

                var thinkResult = Think(obstacle, ref closest, out Vector2 _result);
                switch (thinkResult)
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

        const float MaxDistanceToThink = 200f;

        ThinkResult Think(Collider obstacle, ref float closest, out Vector2 result)
        {
            result = Vector2.zero;

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
                Debug.DrawLine(transform.position, closestPoint, Color.red, Time.fixedDeltaTime);

                result = new Vector2(-1f, -0.5f);
                return ThinkResult.Primary;
            }

            // Continue reverse
            if (dot > .45f && distance < 50f && MovementEngine.IsReverse)
            {
                Debug.DrawLine(transform.position, closestPoint, Color.red, Time.fixedDeltaTime);

                result = new Vector2(-1f, -0.35f);
                return ThinkResult.Primary;
            }

            if (closest < distance) return ThinkResult.None;
            closest = distance;

            float lr = Vector2.SignedAngle(transform.forward, offset) / 180;

            float steering = 1f - Mathf.Abs(lr);
            if (lr < 0) steering = -steering;

            steering *= 1f - dot;

            Debug.DrawLine(transform.position, closestPoint, Color.yellow, Time.fixedDeltaTime);

            result = new Vector2(steering, .5f);

            return ThinkResult.Secondary;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (raycastHitCache.Obstacles.Contains(collision.collider))
            { return; }
            if (collision.gameObject.layer != LayerMask.NameToLayer(LayerMaskNames.Default))
            { return; }
            raycastHitCache.Obstacles.Add(collision.collider);
        }

        void DetectObstacles()
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, DetectionRange, DetectedThings);

            Debug3D.DrawSphere(transform.position, DetectionRange, Color.white, Time.fixedDeltaTime);

            raycastHitCache.Obstacles.Clear();

            for (int i = 0; i < hits; i++)
            {
                var hit = DetectedThings[i];
                if (hit.gameObject.layer != LayerMask.NameToLayer(LayerMaskNames.Default)) continue;
                if (hit.gameObject == gameObject) continue;

                // Debug.DrawLine(transform.position, DetectedThings[i].transform.position, Color.red, Time.fixedDeltaTime);

                raycastHitCache.Obstacles.Add(hit);
            }
        }

        void FixedUpdate()
        {
            TimeToCheck -= Time.fixedDeltaTime;
        }

        public override void CopyTo(object destination) => this.CopyTo<UnitBehaviour_AvoidObstacles>(destination);
        public void CopyTo(UnitBehaviour_AvoidObstacles destination)
        {
            base.CopyTo(destination);
        }
    }
}
