using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

#nullable enable

public static partial class UnclassifiedExtensions
{
    public static bool TryGetRendererBounds(this GameObject @object, out Bounds bounds)
    {
        MeshRenderer[] renderers = @object.GetComponentsInChildren<MeshRenderer>(false);

        if (renderers.Length == 0)
        {
            bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f));
            return false;
        }

        Bounds result = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        { result.Encapsulate(renderers[i].bounds); }

        bounds = result;
        return true;
    }

    public static Bounds GetRendererBounds(this GameObject @object)
    {
        TryGetRendererBounds(@object, out Bounds bounds);
        return bounds;
    }
    public static bool TryGetColliderBounds(this GameObject @object, out Bounds bounds)
    {
        Collider[] colliders = @object.GetComponentsInChildren<Collider>(false);

        if (colliders.Length == 0)
        {
            bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f));
            return false;
        }

        Bounds result = colliders[0].bounds;
        for (int i = 1; i < colliders.Length; i++)
        { result.Encapsulate(colliders[i].bounds); }

        bounds = result;
        return true;
    }

    public static Bounds GetColliderBounds(this GameObject @object)
    {
        TryGetColliderBounds(@object, out Bounds bounds);
        return bounds;
    }

    public static Color Opacity(this Color c, float alpha) => new(c.r, c.g, c.b, c.a * alpha);

    public static bool Contains(this UnityEngine.Object[] self, UnityEngine.Object v)
    {
        for (int i = 0; i < self.Length; i++)
        { if (self[i] == v) return true; }
        return false;
    }

    public static RaycastHit[] ExcludeTransforms(this RaycastHit[] hits, params Transform[] exclude)
    {
        List<RaycastHit> result = new();
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform != null && exclude.Contains(hits[i].transform))
            { continue; }

            result.Add(hits[i]);
        }
        return result.ToArray();
    }
    public static RaycastHit[] ExcludeTriggers(this RaycastHit[] hits)
    {
        List<RaycastHit> result = new();
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.isTrigger) continue;

            result.Add(hits[i]);
        }
        return result.ToArray();
    }

    public static (int Index, float DistanceSqr) Closest(this RaycastHit[] hits, Vector3 origin)
    {
        float closest = (origin - hits[0].point).sqrMagnitude;
        int closestI = 0;

        for (int i = 1; i < hits.Length; i++)
        {
            float d = (origin - hits[i].point).sqrMagnitude;
            if (closest > d)
            {
                closest = d;
                closestI = i;
            }
        }

        return new ValueTuple<int, float>(closestI, closest);
    }
    public static (int Index, float DistanceSqr) Closest(this Transform[] v, Vector3 origin)
    {
        if (v.Length == 0) return new ValueTuple<int, float>(-1, 0f);
        float closest = float.MaxValue;
        int closestI = -1;

        for (int i = 1; i < v.Length; i++)
        {
            if (v[i] == null) continue;
            float d = (origin - v[i].position).sqrMagnitude;
            if (closestI == -1 || closest > d)
            {
                closest = d;
                closestI = i;
            }
        }

        return new ValueTuple<int, float>(closestI, closest);
    }
    public static (int Index, float DistanceSqr) Closest(this Component[] v, Vector3 origin)
    {
        if (v.Length == 0) return new ValueTuple<int, float>(-1, 0f);
        float closest = float.MaxValue;
        int closestI = -1;

        for (int i = 0; i < v.Length; i++)
        {
            if (v[i] == null) continue;
            float d = (origin - v[i].transform.position).sqrMagnitude;
            if (closestI == -1 || closest > d)
            {
                closest = d;
                closestI = i;
            }
        }

        return new ValueTuple<int, float>(closestI, closest);
    }
    public static (int Index, float DistanceSqr) Closest(this IComponent[] v, Vector3 origin)
    {
        if (v.Length == 0) return new ValueTuple<int, float>(-1, 0f);
        float closest = float.MaxValue;
        int closestI = -1;

        for (int i = 0; i < v.Length; i++)
        {
            if ((UnityEngine.Object)v[i] == null) continue;
            float d = (origin - ((Component)v[i]).transform.position).sqrMagnitude;
            if (closestI == -1 || closest > d)
            {
                closest = d;
                closestI = i;
            }
        }

        return new ValueTuple<int, float>(closestI, closest);
    }
    public static (int Index, float DistanceSqr) Closest(this GameObject[] v, Vector3 origin)
    {
        if (v.Length == 0) return new ValueTuple<int, float>(-1, 0f);
        float closest = float.MaxValue;
        int closestI = -1;

        for (int i = 1; i < v.Length; i++)
        {
            if (v[i] == null) continue;
            float d = (origin - v[i].transform.position).sqrMagnitude;
            if (closestI == -1 || closest > d)
            {
                closest = d;
                closestI = i;
            }
        }

        return new ValueTuple<int, float>(closestI, closest);
    }

    public delegate void SearchCallback<T1>(T1 p0);
    public delegate void SearchCallback<T1, T2>(T1 p0, T2 p1);

    public static IEnumerator ClosestAsync(this Transform[] v, Vector3 origin, SearchCallback<int, float>? intermediateCallback, SearchCallback<int, float>? doneCallback = null)
    {
        if (v.Length == 0) yield break;

        float closest = float.MaxValue;
        int closestI = -1;

        for (int i = 0; i < v.Length; i++)
        {
            yield return new WaitForFixedUpdate();

            if (v[i] == null) continue;
            float d = (origin - v[i].position).sqrMagnitude;
            if (closestI == -1 || closest > d)
            {
                closest = d;
                closestI = i;

                intermediateCallback?.Invoke(closestI, closest);
            }
        }

        doneCallback?.Invoke(closestI, closest);
    }
    public static IEnumerator ClosestAsync(this Component[] v, Vector3 origin, SearchCallback<int, float>? intermediateCallback, SearchCallback<int, float>? doneCallback = null)
    {
        if (v.Length == 0) yield break;

        float closest = float.MaxValue;
        int closestI = -1;

        for (int i = 0; i < v.Length; i++)
        {
            yield return new WaitForFixedUpdate();

            if (v[i] == null) continue;
            float d = (origin - v[i].transform.position).sqrMagnitude;
            if (closestI == -1 || closest > d)
            {
                closest = d;
                closestI = i;

                intermediateCallback?.Invoke(closestI, closest);
            }
        }

        doneCallback?.Invoke(closestI, closest);
    }
    public static IEnumerator ClosestAsync(this IComponent[] v, Vector3 origin, SearchCallback<int, float>? intermediateCallback, SearchCallback<int, float>? doneCallback = null)
    {
        if (v.Length == 0) yield break;

        float closest = float.MaxValue;
        int closestI = -1;

        for (int i = 0; i < v.Length; i++)
        {
            yield return new WaitForFixedUpdate();

            if ((UnityEngine.Object)v[i] == null) continue;
            float d = (origin - ((Component)v[i]).transform.position).sqrMagnitude;
            if (closestI == -1 || closest > d)
            {
                closest = d;
                closestI = i;

                intermediateCallback?.Invoke(closestI, closest);
            }
        }

        doneCallback?.Invoke(closestI, closest);
    }
    public static IEnumerator ClosestAsync(this GameObject[] v, Vector3 origin, SearchCallback<int, float>? intermediateCallback, SearchCallback<int, float>? doneCallback = null)
    {
        if (v.Length == 0) yield break;

        float closest = float.MaxValue;
        int closestI = -1;

        for (int i = 1; i < v.Length; i++)
        {
            yield return new WaitForFixedUpdate();

            if (v[i] == null) continue;
            float d = (origin - v[i].transform.position).sqrMagnitude;
            if (closestI == -1 || closest > d)
            {
                closest = d;
                closestI = i;

                intermediateCallback?.Invoke(closestI, closest);
            }
        }

        doneCallback?.Invoke(closestI, closest);
    }
    public static IEnumerator ClosestAsync<T>(this T[] v, Func<T, float> distanceCalculator, SearchCallback<int, float>? intermediateCallback, SearchCallback<int, float>? doneCallback = null)
    {
        if (v.Length == 0) yield break;

        float closest = float.MaxValue;
        int closestI = -1;

        for (int i = 1; i < v.Length; i++)
        {
            yield return new WaitForFixedUpdate();

            if (v[i] == null) continue;
            float d = distanceCalculator.Invoke(v[i]);
            if (closestI == -1 || closest > d)
            {
                closest = d;
                closestI = i;

                intermediateCallback?.Invoke(closestI, closest);
            }
        }

        doneCallback?.Invoke(closestI, closest);
    }

    const float ScreenRayMaxDistance = 500f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, out Vector3 worldPosition)
        => ScreenToWorldPosition(camera, screenPosition, ScreenRayMaxDistance, out worldPosition);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ScreenToWorldPosition(this Camera camera, Vector2 screenPosition)
        => ScreenToWorldPosition(camera, screenPosition, ScreenRayMaxDistance);
    public static bool ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, float maxDistance, out Vector3 worldPosition)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            worldPosition = hit.point;
            return true;
        }
        worldPosition = default;
        return false;
    }
    public static Vector3 ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, float maxDistance)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            return hit.point;
        }
        return ray.GetPoint(maxDistance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, LayerMask layerMask, out Vector3 worldPosition)
        => ScreenToWorldPosition(camera, screenPosition, ScreenRayMaxDistance, layerMask, out worldPosition);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, LayerMask layerMask)
        => ScreenToWorldPosition(camera, screenPosition, ScreenRayMaxDistance, layerMask);
    public static bool ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, float maxDistance, LayerMask layerMask, out Vector3 worldPosition)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
        {
            worldPosition = hit.point;
            return true;
        }
        worldPosition = default;
        return false;
    }
    public static Vector3 ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, float maxDistance, LayerMask layerMask)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
        { return hit.point; }
        return ray.GetPoint(maxDistance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, out RaycastHit[] hits)
        => ScreenToWorldPosition(camera, screenPosition, ScreenRayMaxDistance, out hits);
    public static Vector3 ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, float maxDistance, out RaycastHit[] hits)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        hits = Physics.RaycastAll(ray, maxDistance).ExcludeTriggers();
        if (hits.Length > 0)
        { return hits[0].point; }
        return ray.GetPoint(maxDistance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, int layerMask, out RaycastHit[] hits)
        => ScreenToWorldPosition(camera, screenPosition, ScreenRayMaxDistance, layerMask, out hits);
    public static Vector3 ScreenToWorldPosition(this Camera camera, Vector2 screenPosition, float maxDistance, int layerMask, out RaycastHit[] hits)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        hits = Physics.RaycastAll(ray, maxDistance, layerMask).ExcludeTriggers();
        if (hits.Length > 0)
        { return hits[0].point; }
        return ray.GetPoint(maxDistance);
    }

    public static void SetEmissionColor(this Material material, Color color, float emission)
    {
        material.color = color;

        if (material.HasColor("_Emission"))
        { material.SetColor("_Emission", color * emission); }

        if (material.HasColor("_EmissionColor"))
        { material.SetColor("_EmissionColor", color * emission); }
    }
}

public static class MeshEx
{
    /// <summary>
    /// <see href="https://discussions.unity.com/t/closest-point-on-mesh-collider/178"/>
    /// </summary>
    public static Vector3 ClosestPointSimple(this MeshFilter meshFilter, Vector3 point)
    {
        Vector3 localPoint = meshFilter.transform.InverseTransformPoint(point);
        Vector3 nearestPoint = meshFilter.mesh.ClosestPoint(localPoint);
        return meshFilter.transform.TransformPoint(nearestPoint);
    }

    /// <summary>
    /// <see href="https://discussions.unity.com/t/closest-point-on-mesh-collider/178"/>
    /// </summary>
    public static Vector3 ClosestPointSimple(this Mesh mesh, Vector3 point)
    {
        float minDistance = float.PositiveInfinity;
        Vector3 nearestPoint = default;

        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            float distance = (point - mesh.vertices[i]).sqrMagnitude;

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPoint = mesh.vertices[i];
            }
        }

        return nearestPoint;
    }

    /// <summary>
    /// Thank you <see href="https://forum.unity.com/threads/closest-point-on-mesh-collider.34660/"/>
    /// </summary>
    public static Vector3 ClosestPoint(this MeshFilter meshFilter, Vector3 point)
    {
        Vector3 localPoint = meshFilter.transform.InverseTransformPoint(point);
        Vector3 closestPoint = meshFilter.sharedMesh.ClosestPointSimple(localPoint);
        Vector3 closestWorldPoint = meshFilter.transform.TransformPoint(closestPoint);
        return closestWorldPoint;
    }

    /// <summary>
    /// Thank you <see href="https://forum.unity.com/threads/closest-point-on-mesh-collider.34660/"/>
    /// </summary>
    public static Vector3 ClosestPoint(this Mesh mesh, Vector3 point)
    {
        VertTriList vt = new(mesh);
        Vector3[] vertices = mesh.vertices;
        KDTree kd = KDTree.MakeFromPoints(vertices);
        Vector3 closestPoint = NearestPointOnMesh(point, vertices, kd, mesh.triangles, vt);
        return closestPoint;
    }

    static Vector3 NearestPointOnMesh(Vector3 pt, Vector3[] verts, KDTree vertProx, int[] tri, VertTriList vt)
    {
        // First, find the nearest vertex (the nearest point must be on one of the triangles
        // that uses this vertex if the mesh is convex).
        (int nearest, _) = vertProx.FindNearest(pt);

        // Get the list of triangles in which the nearest vert "participates".
        int[] nearTris = vt[nearest];

        Vector3 nearestPt = default;
        float nearestSqDist = float.MaxValue;

        for (int i = 0; i < nearTris.Length; i++)
        {
            int triOff = nearTris[i] * 3;
            Vector3 a = verts[tri[triOff]];
            Vector3 b = verts[tri[triOff + 1]];
            Vector3 c = verts[tri[triOff + 2]];

            Vector3 posNearestPt = Triangle.NearestPoint(pt, a, b, c);
            float posNearestSqDist = (pt - posNearestPt).sqrMagnitude;

            if (posNearestSqDist < nearestSqDist)
            {
                nearestPt = posNearestPt;
                nearestSqDist = posNearestSqDist;
            }
        }

        return nearestPt;
    }

    static Vector3 NearestPointOnMesh(Vector3 pt, Vector3[] verts, int[] tri, VertTriList vt)
    {
        //	First, find the nearest vertex (the nearest point must be on one of the triangles
        //	that uses this vertex if the mesh is convex).
        int nearest = -1;
        float nearestSqDist = 100000000f;

        for (int i = 0; i < verts.Length; i++)
        {
            float sqDist = (verts[i] - pt).sqrMagnitude;

            if (sqDist < nearestSqDist)
            {
                nearest = i;
                nearestSqDist = sqDist;
            }
        }

        //	Get the list of triangles in which the nearest vert "participates".
        int[] nearTris = vt[nearest];

        Vector3 nearestPt = default;
        nearestSqDist = 100000000f;

        for (int i = 0; i < nearTris.Length; i++)
        {
            int triOff = nearTris[i] * 3;
            Vector3 a = verts[tri[triOff]];
            Vector3 b = verts[tri[triOff + 1]];
            Vector3 c = verts[tri[triOff + 2]];

            Vector3 posNearestPt = Triangle.NearestPoint(pt, a, b, c);
            float posNearestSqDist = (pt - posNearestPt).sqrMagnitude;

            if (posNearestSqDist < nearestSqDist)
            {
                nearestPt = posNearestPt;
                nearestSqDist = posNearestSqDist;
            }
        }

        return nearestPt;
    }
}

public static class VectorEx
{
    public static bool IsOk(this Vector3 v) =>
        v.x != float.PositiveInfinity && v.x != float.NegativeInfinity && v.x != float.NaN &&
        v.y != float.PositiveInfinity && v.y != float.NegativeInfinity && v.y != float.NaN;

    public static Vector3 Clamp(this Vector3 v, Vector3 limit) => new(
        Math.Clamp(v.x, -limit.x, limit.x),
        Math.Clamp(v.y, -limit.y, limit.y),
        Math.Clamp(v.z, -limit.z, limit.z));

    public static Vector3 Clamp01(this Vector3 v) => new(
        Math.Clamp(v.x, 0, 1),
        Math.Clamp(v.y, 0, 1),
        Math.Clamp(v.z, 0, 1));

    public static Vector2 Clamp(this Vector2 v, Vector2 limit) => new(
        Math.Clamp(v.x, -limit.x, limit.x),
        Math.Clamp(v.y, -limit.y, limit.y));

    public static Vector2 Clamp01(this Vector2 v) => new(
        Math.Clamp(v.x, 0, 1),
        Math.Clamp(v.y, 0, 1));

    public static Vector2 To2D(this Vector3 v) => new(v.x, v.z);
    public static Vector3 To3D(this Vector2 v) => new(v.x, 0f, v.y);
    public static Vector3 To3D(this Vector2 v, float yRotation) => new(v.x * Maths.Sin(yRotation), v.y, v.x * Maths.Cos(yRotation));

    public static Vector2Int ToInt(this Vector2 vector2) => new(Maths.RoundToInt(vector2.x), Maths.RoundToInt(vector2.y));
    public static Vector2 ToFloat(this Vector2Int vector2) => new(vector2.x, vector2.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Rotate(this Vector2 v, float degrees) => v.RotateRadians(degrees * Maths.Deg2Rad);
    public static Vector2 RotateRadians(this Vector2 v, float radians)
    {
        float ca = Maths.Cos(radians);
        float sa = Maths.Sin(radians);
        return new Vector2(ca * v.x - sa * v.y, sa * v.x + ca * v.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Rotate(this Vector3 v, float degrees) => v.RotateRadians(degrees * Maths.Deg2Rad);
    public static Vector3 RotateRadians(this Vector3 v, float radians)
    {
        float ca = Maths.Cos(radians);
        float sa = Maths.Sin(radians);
        return new Vector3(ca * v.x - sa * v.y, sa * v.x + ca * v.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Flatten(this Vector3 v) => new(v.x, 0f, v.z);

    public static bool AreEquals(this Vector2 vectorA, Vector2 vectorB, double tolerance)
    {
        float absX = Maths.Pow(vectorB.x - vectorA.x, 2);
        float absY = Maths.Pow(vectorB.y - vectorA.y, 2);

        return Maths.Abs(absX + absY) < tolerance;
    }

    public static bool IsUnitVector(this Vector2 vector) =>
        vector.x >= 0 &&
        vector.x <= 1 &&
        vector.y >= 0 &&
        vector.y <= 1;

    public static bool IsUnitVector(this Vector3 vector) =>
        vector.x >= 0 &&
        vector.x <= 1 &&
        vector.y >= 0 &&
        vector.y <= 1 &&
        vector.z >= 0 &&
        vector.z <= 1;
}

public static partial class ListEx
{
    public static T[] PurgeObjects<T>(this T[] v) where T : UnityEngine.Object
    {
        List<T> result = new(v);
        result.PurgeObjects();
        return result.ToArray();
    }
    public static void PurgeObjects<T>(this IList<T> v) where T : UnityEngine.Object
    {
        for (int i = v.Count - 1; i >= 0; i--)
        { if ((UnityEngine.Object)v[i] == null) v.RemoveAt(i); }
    }
    public static void PurgeObjects(this IList<GameObject> v)
    {
        for (int i = v.Count - 1; i >= 0; i--)
        { if (v[i] == null) v.RemoveAt(i); }
    }

    public static void PopAndDestroy(this IList<UnityEngine.Object> v)
    {
        if (v.Count > 0)
        {
            UnityEngine.Object.Destroy(v[^1]);
            v.RemoveAt(v.Count - 1);
        }
    }
    public static void PopAndDestroy(this IList<GameObject> v)
    {
        if (v.Count > 0)
        {
            UnityEngine.Object.Destroy(v[^1]);
            v.RemoveAt(v.Count - 1);
        }
    }
    public static GameObject AddInstance(this IList<GameObject> v, GameObject prefab)
    {
        GameObject result = GameObject.Instantiate(prefab);
        v.Add(result);
        return result;
    }
}

public static class RigidbodyEx
{
    public static void Rotate(this Rigidbody rb, float rotateAngle, Vector3 axis)
    {
        rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(rotateAngle, axis));
    }

    public static void Drag(this Rigidbody rb, float value)
    {
        rb.AddForce(-rb.linearVelocity.normalized.Flatten() * value, ForceMode.VelocityChange);
    }
}

#pragma warning disable UNT0014 // Invalid type for call to GetComponent
public static class GameObjectEx
{
    public static void SetLayerRecursive(this GameObject @object, LayerMask layer)
        => SetLayerRecursive(@object, layer.value);

    public static void SetLayerRecursive(this GameObject @object, int layer)
    {
        @object.layer = layer;
        foreach (Transform child in @object.transform)
        { SetLayerRecursive(child.gameObject, layer); }
    }

    public static bool HasComponent<T>(this GameObject obj) => obj.TryGetComponent<T>(out _);

    public static bool HasComponentInChildren<T>(this GameObject obj) => obj.TryGetComponentInChildren<T>(out _);
    public static bool HasComponentInParent<T>(this GameObject obj) => obj.TryGetComponentInParent<T>(out _);

    public static bool TryGetComponentInChildren<T>(this GameObject obj, out T component)
    {
        if (obj.TryGetComponent<T>(out component))
        {
            return true;
        }

        component = obj.GetComponentInChildren<T>();

        return component != null;
    }

    public static bool TryGetComponentInChildren<T>(this Component obj, out T component)
    {
        if (obj.TryGetComponent<T>(out component))
        {
            return true;
        }

        component = obj.GetComponentInChildren<T>();

        return component != null;
    }

    public static bool TryGetComponentInParent<T>(this GameObject obj, [NotNullWhen(true)] out T? component)
    {
        if (obj.TryGetComponent<T>(out component))
        { return component != null; }

        if (obj.transform.parent == null)
        {
            component = default;
            return false;
        }

        return TryGetComponentInParent(obj.transform.parent.gameObject, out component);
    }

    public static bool TryGetComponentInParent<T>(this Component obj, [NotNullWhen(true)] out T? component)
    {
        if (obj.TryGetComponent<T>(out component))
        { return component != null; }

        if (obj.transform.parent == null)
        {
            component = default;
            return false;
        }

        return TryGetComponentInParent(obj.transform.parent.gameObject, out component);
    }
}

public static class ComponentEx
{
    public static bool HasComponent<T>(this Component obj) => obj.TryGetComponent<T>(out _);
}
#pragma warning restore UNT0014 // Invalid type for call to GetComponent

public static class RectEx
{
    public static Rect Padding(this Rect rect, float padding)
    {
        float halfPadding = padding / 2;
        rect.x -= halfPadding;
        rect.y -= halfPadding;
        rect.width += padding;
        rect.height += padding;
        return rect;
    }

    public static Vector2 TopLeft(this Rect rect) => rect.position;
    public static Vector2 TopRight(this Rect rect) => new(rect.position.x + rect.width, rect.position.y);
    public static Vector2 BottomLeft(this Rect rect) => new(rect.position.x, rect.position.y + rect.height);
    public static Vector2 BottomRight(this Rect rect) => new(rect.position.x + rect.width, rect.position.y + rect.height);

    public static (Vector2 TopLeft, Vector2 TopRight, Vector2 BottomLeft, Vector2 BottomRight) Corners(this Rect rect) =>
        (rect.TopLeft(), rect.TopRight(), rect.BottomLeft(), rect.BottomRight());

    public static Rect CutLeft(this Rect rect, float width, out Rect cut)
    {
        cut = new Rect(rect.xMin, rect.yMin, width, rect.height);
        rect.xMin += width;
        return rect;
    }
}

public static class RectIntEx
{
    public static RectInt Padding(this RectInt rect, int padding)
    {
        int halfPadding = padding / 2;
        rect.x -= halfPadding;
        rect.y -= halfPadding;
        rect.width += padding;
        rect.height += padding;
        return rect;
    }

    public static Rect ToFloat(this RectInt rect) => new(rect.x, rect.y, rect.width, rect.height);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int TopLeft(this RectInt rect) => rect.position;
    public static Vector2Int TopRight(this RectInt rect) => new(rect.position.x + rect.width, rect.position.y);
    public static Vector2Int BottomLeft(this RectInt rect) => new(rect.position.x, rect.position.y + rect.height);
    public static Vector2Int BottomRight(this RectInt rect) => new(rect.position.x + rect.width, rect.position.y + rect.height);
}

public static class TransformEx
{
    public static bool IsChildOfRecursive(this Transform child, Transform parent)
    {
        if (child.parent == null) return false;
        if (child.parent == parent) return true;
        return IsChildOfRecursive(child.parent, parent);
    }
}
