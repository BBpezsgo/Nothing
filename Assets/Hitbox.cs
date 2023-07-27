using Game.Managers;

using UnityEngine;

public class Hitbox : MonoBehaviour
{
    internal Bounds Bounds
    {
        get
        {
            float t = Time.time - lastRefresh;
            if (t > 1f)
            {
                Bounds bounds = PhotographyStudio.GetSummedBounds(gameObject);
                lastRefresh = Time.time;
                localBounds = new Bounds(transform.InverseTransformPoint(bounds.center), bounds.size);
                return bounds;
            }
            else
            {
                Bounds bounds = localBounds;
                bounds.center = transform.TransformPoint(bounds.center);
                return bounds;
            }
        }
    }

    void Start()
    {
        lastRefresh = 0f;
        _ = Bounds;
    }

    float lastRefresh;
    Bounds localBounds;
}