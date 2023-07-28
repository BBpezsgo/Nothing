using UnityEngine;

public class Hitbox : MonoBehaviour
{
    internal Bounds RendererBounds
    {
        get
        {
            float t = Time.time - lastRendererRefresh;
            if (t > 1f)
            {
                Bounds bounds = gameObject.GetRendererBounds();
                lastRendererRefresh = Time.time;
                localRendererBounds = new Bounds(transform.InverseTransformPoint(bounds.center), bounds.size);
                return bounds;
            }
            else
            {
                Bounds bounds = localRendererBounds;
                bounds.center = transform.TransformPoint(bounds.center);
                return bounds;
            }
        }
    }

    internal Bounds ColliderBounds
    {
        get
        {
            float t = Time.time - lastColliderRefresh;
            if (t > 1f)
            {
                Bounds bounds = gameObject.GetColliderBounds();
                lastColliderRefresh = Time.time;
                localColliderBounds = new Bounds(transform.InverseTransformPoint(bounds.center), bounds.size);
                return bounds;
            }
            else
            {
                Bounds bounds = localColliderBounds;
                bounds.center = transform.TransformPoint(bounds.center);
                return bounds;
            }
        }
    }

    void Start()
    {
        lastRendererRefresh = 0f;
        _ = RendererBounds;
        lastColliderRefresh = 0f;
        _ = ColliderBounds;
    }

    float lastRendererRefresh;
    Bounds localRendererBounds;

    float lastColliderRefresh;
    Bounds localColliderBounds;
}