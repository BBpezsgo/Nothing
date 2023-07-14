using AssetManager;

using System;
using System.Collections.Generic;

using UnityEngine;

using Utilities;

public class UnitBehaviour_AvoidObstacles : UnitBehaviour_Base, IHaveAssetFields
{
    [SerializeField, ReadOnly] RaycastHitCache raycastHitCache;
    [SerializeField, ReadOnly, NonReorderable] Collider[] DetectedThings = new Collider[10];
    [SerializeField, ReadOnly] float TimeToCheck = .1f;

    [Serializable]
    struct RaycastHitCache
    {
        [SerializeField, ReadOnly, NonReorderable] internal List<Collider> Obstacles;
    }

    protected override void Start()
    {
        base.Start();
        raycastHitCache = new RaycastHitCache()
        {
            Obstacles = new List<Collider>(),
        };
    }

    internal override Vector2? GetOutput()
    {
        if (TimeToCheck <= 0f)
        {
            DetectObstacles();
            TimeToCheck = 5f;
        }

        float closest = float.MaxValue;

        Vector2? result = null;

        for (int i = 0; i < raycastHitCache.Obstacles.Count; i++)
        {
            var obstacle = raycastHitCache.Obstacles[i];
            if (obstacle == null) continue;
            Vector2 offset = (obstacle.transform.position - transform.position).To2D();

            if (offset.sqrMagnitude > 200f) continue;

            float dot = Vector2.Dot(transform.forward.To2D(), offset.normalized);
            if (dot < 0f) continue;

            float distSqr = (obstacle.ClosestPoint(transform.position).To2D() - transform.position.To2D()).sqrMagnitude;

            // Reverse
            if (dot > .8f && (
                    distSqr < 10f ||
                    (VehicleEngine.IsReverse && distSqr < 50f)
                ))
            {
                return new Vector2(-1f, -1f);
            }

            if (closest < distSqr) continue;

            float lr = Vector2.SignedAngle(transform.forward, offset) / 180;

            float steering = 1f - Mathf.Abs(lr);
            if (lr < 0) steering = -steering;

            steering *= 1f - dot;

            Debug.DrawLine(transform.position, obstacle.transform.position, Color.red, Time.fixedDeltaTime);

            result = new Vector2(steering, .5f);
        }

        return result;
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
        int hits = Physics.OverlapSphereNonAlloc(transform.position, 10f, DetectedThings);

        Debug3D.DrawSphere(transform.position, 10f, Color.white, Time.fixedDeltaTime);

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
}
