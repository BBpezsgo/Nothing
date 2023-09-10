using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Unity.Netcode;

using Game.Components;
using Game.Managers;

using Networking;
using UI;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Netcode.Transports.WebSocket;
using Netcode.Transports.Offline;
using System.Net;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
internal struct Pair<TKey, TValue>
{
    [SerializeField] internal TKey Key;
    [SerializeField] internal TValue Value;

    public Pair(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
}

internal struct Triangle
{
    public Vector3 A;
    public Vector3 B;
    public Vector3 C;

    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        A = a;
        B = b;
        C = c;
    }

    public static implicit operator (Vector3 a, Vector3 b, Vector3 c)(Triangle triangle)
        => (triangle.A, triangle.B, triangle.C);

    /// <summary>
    /// Thank you <see href="https://forum.unity.com/threads/closest-point-on-mesh-collider.34660/"/>
    /// </summary>
    public static Vector3 NearestPoint(Vector3 pt, Triangle triangle)
        => NearestPoint(pt, triangle.A, triangle.B, triangle.C);

    /// <summary>
    /// Thank you <see href="https://forum.unity.com/threads/closest-point-on-mesh-collider.34660/"/>
    /// </summary>
    public static Vector3 NearestPoint(Vector3 pt, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 edge1 = b - a;
        Vector3 edge2 = c - a;
        Vector3 edge3 = c - b;
        float edge1Len = edge1.magnitude;
        float edge2Len = edge2.magnitude;
        float edge3Len = edge3.magnitude;

        Vector3 ptLineA = pt - a;
        Vector3 ptLineB = pt - b;
        Vector3 ptLineC = pt - c;
        Vector3 xAxis = edge1 / edge1Len;
        Vector3 zAxis = Vector3.Cross(edge1, edge2).normalized;
        Vector3 yAxis = Vector3.Cross(zAxis, xAxis);

        Vector3 edge1Cross = Vector3.Cross(edge1, ptLineA);
        Vector3 edge2Cross = Vector3.Cross(edge2, -ptLineC);
        Vector3 edge3Cross = Vector3.Cross(edge3, ptLineB);
        bool edge1On = Vector3.Dot(edge1Cross, zAxis) > 0f;
        bool edge2On = Vector3.Dot(edge2Cross, zAxis) > 0f;
        bool edge3On = Vector3.Dot(edge3Cross, zAxis) > 0f;

        //	If the point is inside the triangle then return its coordinate.
        if (edge1On && edge2On && edge3On)
        {
            float xExtent = Vector3.Dot(ptLineA, xAxis);
            float yExtent = Vector3.Dot(ptLineA, yAxis);
            return a + xAxis * xExtent + yAxis * yExtent;
        }

        //	Otherwise, the nearest point is somewhere along one of the edges.
        Vector3 edge1Norm = xAxis;
        Vector3 edge2Norm = edge2.normalized;
        Vector3 edge3Norm = edge3.normalized;

        float edge1Ext = Mathf.Clamp(Vector3.Dot(edge1Norm, ptLineA), 0f, edge1Len);
        float edge2Ext = Mathf.Clamp(Vector3.Dot(edge2Norm, ptLineA), 0f, edge2Len);
        float edge3Ext = Mathf.Clamp(Vector3.Dot(edge3Norm, ptLineB), 0f, edge3Len);

        Vector3 edge1Pt = a + edge1Ext * edge1Norm;
        Vector3 edge2Pt = a + edge2Ext * edge2Norm;
        Vector3 edge3Pt = b + edge3Ext * edge3Norm;

        float sqDist1 = (pt - edge1Pt).sqrMagnitude;
        float sqDist2 = (pt - edge2Pt).sqrMagnitude;
        float sqDist3 = (pt - edge3Pt).sqrMagnitude;

        if (sqDist1 < sqDist2)
        {
            if (sqDist1 < sqDist3)
            {
                return edge1Pt;
            }
            else
            {
                return edge3Pt;
            }
        }
        else if (sqDist2 < sqDist3)
        {
            return edge2Pt;
        }
        else
        {
            return edge3Pt;
        }
    }
}

internal readonly struct RectUtils
{
    internal static Rect Center(Vector2 center, Vector2 size)
        => new(center - (size * .5f), size);

    internal static Rect FromCorners(Vector2 topLeft, Vector2 bottomRight)
        => new(topLeft, bottomRight - topLeft);
}

internal static class GLUtils
{
    static Material _solidMaterial;
    internal static Material SolidMaterial
    {
        get
        {
            if (_solidMaterial == null)
            { _solidMaterial = new Material(Shader.Find("Hidden/Internal-Colored")); }
            return _solidMaterial;
        }
    }

    const int CircleSegmentCount = 32;

    internal static void DrawLine(Vector2 a, Vector2 b, float thickness, Color color)
    {
        if (thickness <= 0f)
        { return; }

        if (thickness <= 1f)
        {
            DrawLine(a, b, color);
            return;
        }

        Vector2 diff = b - a;
        if (diff.x < 1 &&
            diff.x > -1 &&
            diff.y < 1 &&
            diff.y > -1)
        { return; }

        Vector2 direction = diff.normalized;

        Vector2 left = new(-direction.y, direction.x);
        Vector2 right = new(direction.y, -direction.x);

        Vector2 pointA = a + (left * thickness);
        Vector2 pointB = a + (right * thickness);

        Vector2 pointC = b + (right * thickness);
        Vector2 pointD = b + (left * thickness);

        GL.Begin(GL.TRIANGLES);

        GL.Color(color);

        GL.Vertex(pointA);
        GL.Vertex(pointB);
        GL.Vertex(pointC);

        GL.Vertex(pointA);
        GL.Vertex(pointC);
        GL.Vertex(pointD);

        GL.End();
    }
    internal static void DrawLine(Vector2 a, Vector2 b, Color color)
    {
        Vector2 diff = b - a;
        if (diff.x < 1 &&
            diff.x > -1 &&
            diff.y < 1 &&
            diff.y > -1)
        { return; }

        GL.Begin(GL.LINES);

        GL.Color(color);

        GL.Vertex(a);
        GL.Vertex(b);

        GL.End();
    }

    internal static void DrawLine(Utilities.Line line, float thickness, Color color)
        => DrawLine(line.PointA, line.PointB, thickness, color);
    internal static void DrawLine(Utilities.Line line, Color color)
        => DrawLine(line.PointA, line.PointB, color);

    internal static void DrawCircle(Vector2 center, float radius, float thickness, Color color, int segmentCount = CircleSegmentCount)
    {
        if (thickness <= 1f)
        {
            DrawCircle(center, radius, color, segmentCount);
            return;
        }

        GL.Begin(GL.TRIANGLE_STRIP);
        GL.Color(color);

        for (int i = 0; i < segmentCount; i++)
        {
            {
                float rad = 2 * Mathf.PI * ((float)i / (float)segmentCount);
                Vector2 direction = new(Mathf.Cos(rad), Mathf.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * (radius)));
            }

            {
                float rad = 2 * Mathf.PI * ((float)(i + 1) / (float)segmentCount);
                Vector2 direction = new(Mathf.Cos(rad), Mathf.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * (radius)));
            }
        }
        GL.End();
    }
    internal static void DrawCircle(Vector2 center, float radius, float thickness, Color color, float fillAmmount, int segmentCount = CircleSegmentCount)
    {
        if (fillAmmount >= .99f)
        {
            DrawCircle(center, radius, thickness, color, segmentCount);
            return;
        }

        if (thickness <= 1f)
        {
            DrawCircle(center, radius, color, segmentCount);
            return;
        }

        int segments = Mathf.FloorToInt(fillAmmount * segmentCount);
        float step = 1f / (float)segmentCount;

        GL.Begin(GL.TRIANGLE_STRIP);
        GL.Color(color);

        for (int i = 0; i < segments; i++)
        {
            {
                float rad = 2 * Mathf.PI * ((float)i / (float)segmentCount);
                Vector2 direction = new(Mathf.Cos(rad), Mathf.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * (radius)));
            }

            {
                float next = 1 + Mathf.Clamp01((fillAmmount - ((float)(i + 1) / (float)segmentCount)) / step);

                float rad = 2 * Mathf.PI * ((float)(i + next) / (float)segmentCount);
                Vector2 direction = new(Mathf.Cos(rad), Mathf.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * (radius)));
            }
        }
        GL.End();
    }

    internal static void DrawCircle(Vector2 center, float radius, Color color, int segmentCount = CircleSegmentCount)
    {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(color);

        for (int i = 0; i < segmentCount; i++)
        {
            float rad = 2 * Mathf.PI * ((float)i / (float)segmentCount);
            Vector2 direction = new(Mathf.Cos(rad), Mathf.Sin(rad));

            GL.Vertex(center + (direction * radius));
        }
        GL.End();
    }
    internal static void DrawCircle(Vector2 center, float radius, Color color, float fillAmmount, int segmentCount = CircleSegmentCount)
    {
        if (fillAmmount >= .99f)
        {
            DrawCircle(center, radius, color, segmentCount);
            return;
        }

        int segments = Mathf.FloorToInt(fillAmmount * segmentCount);
        float step = 1f / (float)segmentCount;

        GL.Begin(GL.LINE_STRIP);
        GL.Color(color);

        for (int i = 0; i < segments; i++)
        {
            {
                float rad = 2 * Mathf.PI * ((float)i / (float)segmentCount);
                Vector2 direction = new(Mathf.Cos(rad), Mathf.Sin(rad));

                GL.Vertex(center + (direction * radius));
            }

            {
                float next = 1 + Mathf.Clamp01((fillAmmount - ((float)(i + 1) / (float)segmentCount)) / step);

                float rad = 2 * Mathf.PI * ((float)(i + next) / (float)segmentCount);
                Vector2 direction = new(Mathf.Cos(rad), Mathf.Sin(rad));

                GL.Vertex(center + (direction * radius));
            }
        }
        GL.End();
    }

}

internal readonly struct GUIUtils
{
    internal static Vector2 TransformPoint(Vector2 screenPosition)
        => new(screenPosition.x, Screen.height - screenPosition.y);

    internal static Texture2D GenerateCircleFilled(Vector2Int size)
    {
        var result = new Texture2D(size.x, size.y);
        Vector2 center = Vector2.one * .5f;
        for (int x = 0; x < result.width; x++)
        {
            for (int y = 0; y < result.height; y++)
            {
                Vector2 p = new((float)x / (float)result.width, (float)y / (float)result.height);
                float d = Vector2.Distance(p, center);
                if (d < .5f)
                {
                    result.SetPixel(x, y, Color.white);
                }
                else
                {
                    result.SetPixel(x, y, new Color(1, 1, 1, 0));
                }
            }
        }
        result.Apply();
        return result;
    }

    internal static Texture2D GenerateCircle(Vector2Int size, float thickness = .25f)
    {
        thickness = Mathf.Clamp(thickness, 0f, .5f);
        var result = new Texture2D(size.x, size.y);
        Vector2 center = Vector2.one * .5f;
        for (int x = 0; x < result.width; x++)
        {
            for (int y = 0; y < result.height; y++)
            {
                Vector2 p = new((float)x / (float)result.width, (float)y / (float)result.height);
                float d = Vector2.Distance(p, center);
                if (d < .5f && d >= (.5f - thickness))
                {
                    result.SetPixel(x, y, Color.white);
                }
                else
                {
                    result.SetPixel(x, y, new Color(1, 1, 1, 0));
                }
            }
        }
        result.Apply();
        return result;
    }

}

namespace Utilities
{
    internal readonly struct LayerMaskNames
    {
        public const string Default = "Default";
        public const string Ground = "Ground";
        public const string Projectile = "Projectile";
        public const string PhotographyStudio = "PhotographyStudio";
        public const string Water = "Water";
    }

    internal readonly struct DefaultLayerMasks
    {
        public static int JustDefault => LayerMask.GetMask(LayerMaskNames.Default);
        public static int JustGround => LayerMask.GetMask(LayerMaskNames.Ground);
        public static int PhotographyStudio => LayerMask.GetMask(LayerMaskNames.PhotographyStudio);

        /// <summary>
        /// <see cref="LayerMaskNames.Default"/> ; <see cref="LayerMaskNames.Ground"/>
        /// </summary>
        public static int Solids => LayerMask.GetMask(LayerMaskNames.Default, LayerMaskNames.Ground);
        /// <summary>
        /// <see cref="LayerMaskNames.Default"/> ; <see cref="LayerMaskNames.Projectile"/>
        /// </summary>
        public static int PossiblyDamagables => LayerMask.GetMask(LayerMaskNames.Default, LayerMaskNames.Projectile);
    }

    internal static class Utils
    {
        internal static (Vector3 TopLeft, Vector3 BottomRight) GetCorners(Vector3[] points)
        {
            Vector3 topLeft = points[0];
            Vector3 bottomRight = points[0];

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i];

                if (p.x < topLeft.x)
                { topLeft.x = p.x; }

                if (p.y < topLeft.y)
                { topLeft.y = p.y; }

                if (p.z < topLeft.z)
                { topLeft.z = p.z; }

                if (p.x > bottomRight.x)
                { bottomRight.x = p.x; }

                if (p.y > bottomRight.y)
                { bottomRight.y = p.y; }

                if (p.z > bottomRight.z)
                { bottomRight.z = p.z; }
            }

            return (topLeft, bottomRight);
        }

        internal static (Vector2 TopLeft, Vector2 BottomRight) GetScreenCorners(Vector3[] points)
        {
            Vector2 topLeft = points[0];
            Vector2 bottomRight = points[0];

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i];

                if (p.x < topLeft.x)
                { topLeft.x = p.x; }

                if (p.y < topLeft.y)
                { topLeft.y = p.y; }

                if (p.x > bottomRight.x)
                { bottomRight.x = p.x; }

                if (p.y > bottomRight.y)
                { bottomRight.y = p.y; }
            }

            return (topLeft, bottomRight);
        }

        internal static bool GetScreenCorners(Vector3[] points, out (Vector2 TopLeft, Vector2 BottomRight) corners)
        {
            Vector2 topLeft = points[0];
            Vector2 bottomRight = points[0];

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i];

                if (p.z < 0)
                {
                    corners = (Vector2.zero, Vector2.zero);
                    return false;
                }

                if (p.x < topLeft.x)
                { topLeft.x = p.x; }

                if (p.y < topLeft.y)
                { topLeft.y = p.y; }

                if (p.x > bottomRight.x)
                { bottomRight.x = p.x; }

                if (p.y > bottomRight.y)
                { bottomRight.y = p.y; }
            }

            corners = (topLeft, bottomRight);
            return true;
        }

        /// <summary>
        /// Normalizes the angle <paramref name="a"/> between -180 .. 180
        /// </summary>
        internal static float NormalizeAngle(float a)
        {
            float angle = a % 360;
            angle = angle > 180 ? angle - 360 : angle;
            return angle;
            //return a - 180f * Mathf.Floor((a + 180f) / 180f);
        }

        /// <summary>
        /// Normalizes the angle <paramref name="a"/> between 0 .. 360
        /// </summary>
        internal static float NormalizeAngle360(float a)
        {
            if (a < 0f)
            { a += 360f; }
            if (a >= 360f)
            { a -= 360f; }
            return a;
        }

        internal static float ModularClamp(float val, float min, float max, float rangemin = -180f, float rangemax = 180f)
        {
            var modulus = Mathf.Abs(rangemax - rangemin);
            if ((val %= modulus) < 0f) val += modulus;
            return Mathf.Clamp(val + Mathf.Min(rangemin, rangemax), min, max);
        }

        static Texture2D _whiteTexture;
        /// <summary>
        /// <see href="https://github.com/pickles976/RTS_selection/blob/master/Utils.cs"/>
        /// </summary>
        internal static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }

                return _whiteTexture;
            }
        }

        /// <summary>
        /// <see href="https://github.com/pickles976/RTS_selection/blob/master/Utils.cs"/>
        /// </summary>
        internal static void DrawScreenRect(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = Color.white;
        }

        /// <summary>
        /// <see href="https://github.com/pickles976/RTS_selection/blob/master/Utils.cs"/>
        /// </summary>
        internal static void DrawScreenRectBorder(Rect rect, float thickness, Color color)
        {
            // Top
            Utils.DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            // Left
            Utils.DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            // Right
            Utils.DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
            // Bottom
            Utils.DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        }

        /// <summary>
        /// <see href="https://github.com/pickles976/RTS_selection/blob/master/Utils.cs"/>
        /// </summary>
        internal static Rect GetScreenRect(Vector3 screenPositionA, Vector3 screenPositionB)
        {
            screenPositionA.y = Screen.height - screenPositionA.y;
            screenPositionB.y = Screen.height - screenPositionB.y;
            // Calculate corners
            Vector3 topLeft = Vector3.Min(screenPositionA, screenPositionB);
            Vector3 bottomRight = Vector3.Max(screenPositionA, screenPositionB);
            // Create Rect
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }


        static readonly Vector3[] BoundsProjectorCorners = new Vector3[8];
        static void ProjectBounds(Camera camera, Bounds bounds, Vector3[] result)
        {
            result[0] = camera.WorldToScreenPoint(bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, bounds.extents.z));
            result[1] = camera.WorldToScreenPoint(bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z));
            result[2] = camera.WorldToScreenPoint(bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z));
            result[3] = camera.WorldToScreenPoint(bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z));
            result[4] = camera.WorldToScreenPoint(bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z));
            result[5] = camera.WorldToScreenPoint(bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z));
            result[6] = camera.WorldToScreenPoint(bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z));
            result[7] = camera.WorldToScreenPoint(bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z));
        }
        internal static bool GetScreenCorners(Bounds bounds, out (Vector2 TopLeft, Vector2 BottomRight) corners)
            => Utils.GetScreenCorners(Game.MainCamera.Camera, bounds, out corners);
        internal static bool GetScreenCorners(Camera camera, Bounds bounds, out (Vector2 TopLeft, Vector2 BottomRight) corners)
        {
            ProjectBounds(camera, bounds, BoundsProjectorCorners);

            Vector2 topLeft = BoundsProjectorCorners[0];
            Vector2 bottomRight = BoundsProjectorCorners[0];

            for (int i = 0; i < BoundsProjectorCorners.Length; i++)
            {
                Vector3 p = BoundsProjectorCorners[i];

                if (p.z < 0)
                {
                    corners = (Vector2.zero, Vector2.zero);
                    return false;
                }

                if (p.x < topLeft.x)
                { topLeft.x = p.x; }

                if (p.y < topLeft.y)
                { topLeft.y = p.y; }

                if (p.x > bottomRight.x)
                { bottomRight.x = p.x; }

                if (p.y > bottomRight.y)
                { bottomRight.y = p.y; }
            }

            corners = (topLeft, bottomRight);
            return true;
        }
    }

    internal static class Ballistics
    {
        /// <summary>
        /// This is positive
        /// </summary>
        public static float G => Mathf.Abs(Physics.gravity.y);
        /// <summary>
        /// This points downwards
        /// </summary>
        public static Vector2 GVector => new(0f, -G);

        public readonly struct Trajectory
        {
            /// <summary>
            /// In degrees
            /// </summary>
            public readonly float Angle;
            /// <summary>
            /// In degrees
            /// </summary>
            public readonly float Direction;
            public readonly float Velocity;
            public readonly Vector3 StartPosition;

            public Trajectory(float angle, float direction, float velocity, Vector3 startPosition)
            {
                Angle = angle;
                Direction = direction;
                Velocity = velocity;
                StartPosition = startPosition;
            }

            public Vector2 Velocity2D()
                => new(Velocity * Mathf.Cos(Angle * Mathf.Deg2Rad), Velocity * Mathf.Sin(Angle * Mathf.Deg2Rad));

            public Vector3 Velocity3D()
            {
                Vector3 result = Vector3.zero;
                result.x = Mathf.Sin(Direction * Mathf.Deg2Rad);
                result.y = Mathf.Sin(Angle * Mathf.Deg2Rad);
                result.z = Mathf.Cos(Direction * Mathf.Deg2Rad);
                return result;
            }

            public Vector3 Position(float t)
            {
                Vector2 displacement = Utilities.Ballistics.Displacement(Angle * Mathf.Deg2Rad, Velocity, t);
                Vector3 displacement3D = Vector3.zero;

                displacement3D.x = displacement.x * Mathf.Sin(Direction * Mathf.Deg2Rad);
                displacement3D.y = displacement.y;
                displacement3D.z = displacement.x * Mathf.Cos(Direction * Mathf.Deg2Rad);

                displacement3D += StartPosition;

                return displacement3D;
            }

            public static Vector2 TransformPositionToPlane(Vector3 position, float directionRad) => new()
            {
                y = position.y,
                x = position.x * Mathf.Cos(directionRad) + position.y * Mathf.Sin(directionRad),
            };
        }

        /// <param name="v">
        /// Projectile's initial velocity
        /// </param>
        public static float? CalculateTime(float v, float angle, float heightDisplacement)
        {
            float a = v * Mathf.Sin(angle);
            float b = 2 * G * heightDisplacement;

            float discriminant = (a * a) + b;
            if (discriminant < 0)
            {
                return null;
            }

            float sqrt = Mathf.Sqrt(discriminant);

            return (a + sqrt) / G;
        }

        /*
        internal static float CalculateAngle(float v, float x)
        {
            // v0y = v0 * Mathf.Sin(theta)
            // v0x = v0 * Mathf.Cos(theta)

            // y = y0 + v0y * t + 0.5 * G * t * t
            // 0 = 0 + v0y * t + 0.5 * G * t * t

            // x = v0x * t



            // 0 = 0 + v0 * Mathf.Sin(theta) * t + 0.5 * G * t * t

            // x = v0 * Mathf.Cos(theta) * t
            // t = x / ( v0 * Mathf.Cos(theta) )

            // 0 = 0 + v0 * Mathf.Sin(theta) * (x / ( v0 * Mathf.Cos(theta) )) + 0.5 * G * Mathf.Pow((x / ( v0 * Mathf.Cos(theta) )), 2)

            // 0 = Mathf.Sin(theta) * Mathf.Cos(theta) - ( (G * x) / (2 * v0 * v0) )

            // 0 = 0.5 * Mathf.Sin(2 * theta) - ( ... )

            // Mathf.Sin(2 * theta) = ( ... )

            float theta = 0.5f * Mathf.Asin((G * x) / (v * v));

            return theta;
        }
        */

        /// <param name="v">Initial velocity</param>
        /// <param name="target">Position of the target</param>
        /// <returns>
        /// The required <b>angle in radians</b> to hit a <paramref name="target"/>
        /// fired <paramref name="from"/> initial projectile speed <paramref name="v"/>
        /// </returns>
        public static (float, float)? AngleOfReach(float v, Vector3 from, Vector3 target)
        {
            var diff = target - from;
            float y = diff.y;
            float x = Mathf.Sqrt((diff.x * diff.x) + (diff.z * diff.z));
            return Ballistics.AngleOfReach(v, new Vector2(x, y));
        }

        /// <param name="v">
        /// Projectile's initial velocity
        /// </param>
        /// <param name="target">
        /// Position of the target in <b>world space</b>
        /// </param>
        /// <returns>
        /// The required <b>angle in radians</b> to hit a <paramref name="target"/>
        /// fired <paramref name="from"/> initial projectile speed <paramref name="v"/>
        /// </returns>
        public static float? AngleOfReach1(float v, Vector3 from, Vector3 target)
        {
            var diff = target - from;
            float y = diff.y;
            float x = Mathf.Sqrt((diff.x * diff.x) + (diff.z * diff.z));
            return Ballistics.AngleOfReach1(v, new Vector2(x, y));
        }

        /// <param name="v">
        /// Projectile's initial velocity
        /// </param>
        /// <param name="target">
        /// Position of the target in <b>world space</b>
        /// </param>
        /// <returns>
        /// The required <b>angle in radians</b> to hit a <paramref name="target"/>
        /// fired <paramref name="from"/> initial projectile speed <paramref name="v"/>
        /// </returns>
        public static float? AngleOfReach2(float v, Vector3 from, Vector3 target)
        {
            var diff = target - from;
            float y = diff.y;
            float x = Mathf.Sqrt((diff.x * diff.x) + (diff.z * diff.z));
            return Ballistics.AngleOfReach2(v, new Vector2(x, y));
        }

        /*
        /// <param name="v">
        /// Projectile's initial velocity
        /// </param>
        /// <param name="x">
        /// Range / Distance to the target
        /// </param>
        /// <param name="y">
        /// Altitude of the target
        /// </param>
        /// <summary>
        /// <seealso href="https://en.wikipedia.org/wiki/Projectile_motion#Angle_%CE%B8_required_to_hit_coordinate_(x,_y)"/>
        /// </summary>
        /// <returns>
        /// The required <b>angle in radians</b> to hit a target at range <paramref name="x"/> and altitude <paramref name="y"/> when
        /// fired from (0,0) and with initial projectile speed <paramref name="v"/>
        /// </returns>
        public static float CalculateAngle(float v, float x, float y)
        {
            float g = G;

            float v2 = v * v;
            float v4 = v2 * v2;
            float x2 = x * x;

            float theta = Mathf.Atan2(v2 - Mathf.Sqrt(v4 - g * (g * x2 + 2 * y * v2)), g * x);

            return theta;
        }
        */

        public static float? CalculateX(float angle, float v, float heightDisplacement)
        {
            float? t = CalculateTime(v, angle, heightDisplacement);

            if (t.HasValue)
            { return DisplacementX(angle, t.Value, v); }

            return null;
        }

        public static float CalculateY(float angleRad, float t, float v)
            => (v * Mathf.Sin(angleRad) * t) - ((G * t * t) / 2f);

        public static float CalculateTimeToMaxHeight(float angleRad, float v)
            => (v * Mathf.Sin(angleRad)) / G;

        /// <summary>
        /// To hit a target at range x and altitude y when fired from (0,0) and with initial speed v.
        /// </summary>
        public static (float, float)? AngleOfReach(float v, Vector2 target)
        {
            float v2 = v * v;

            float x = target.x;
            float y = target.y;

            float discriminant = (v2 * v2) - (G * ((G * x * x) + (2 * y * v2)));

            if (discriminant < 0f)
            { return null; }

            float dSqrt = Mathf.Sqrt(discriminant);

            float a = (v2 + dSqrt) / (G * x);
            float b = (v2 - dSqrt) / (G * x);

            float a_ = Mathf.Atan(a);
            float b_ = Mathf.Atan(b);

            return (a_, b_);
        }

        /// <summary>
        /// To hit a target at range x and altitude y when fired from (0,0) and with initial speed v.
        /// </summary>
        public static float? AngleOfReach1(float v, Vector2 target)
        {
            float v2 = v * v;

            float x = target.x;
            float y = target.y;

            float discriminant = (v2 * v2) - (G * ((G * x * x) + (2 * y * v2)));

            if (discriminant < 0f)
            { return null; }

            float dSqrt = Mathf.Sqrt(discriminant);

            float a = (v2 + dSqrt) / (G * x);

            float a_ = Mathf.Atan(a);

            return a_;
        }

        /// <summary>
        /// To hit a target at range x and altitude y when fired from (0,0) and with initial speed v.
        /// </summary>
        public static float? AngleOfReach2(float v, Vector2 target)
        {
            float v2 = v * v;

            float x = target.x;
            float y = target.y;

            float discriminant = (v2 * v2) - (G * ((G * x * x) + (2 * y * v2)));

            if (discriminant < 0f)
            { return null; }

            float dSqrt = Mathf.Sqrt(discriminant);

            float b = (v2 - dSqrt) / (G * x);

            float b_ = Mathf.Atan(b);

            return b_;
        }

        /// <param name="angleRad">Launch angle</param>
        /// <param name="v">Initial velocity</param>
        /// <returns>The greatest height that the object will reach</returns>
        public static float MaxHeight(float angleRad, float v)
            => (v * v * Mathf.Pow(Mathf.Sin(angleRad), 2f)) / (2f * G);

        /// <summary>
        /// The "angle of reach" is the angle at which a projectile must be launched in order to go a distance <paramref name="d"/>, given the initial velocity <paramref name="v"/>.
        /// <seealso href="https://en.wikipedia.org/wiki/Projectile_motion#Angle_of_reach"/>
        /// </summary>
        /// <param name="v">Initial velocity</param>
        /// <param name="d">Target distance</param>
        /// <returns><c>(shallow, steep)</c> in radians or <c><see langword="null"/></c> if there is no solution</returns>
        public static (float, float)? AngleOfReach(float v, float d)
        {
            float a = (G * d) / v * v;

            if (a < -1f || a > 1f) return null;

            float shallow = 0.5f * Mathf.Asin(a);
            float steep = 0.5f * Mathf.Acos(a);

            return (shallow, steep);
        }

        public static float Radius(float v, float angleRad)
            => ((v * v) / G) * Mathf.Sin(angleRad * 2f);

        public static float MaxRadius(float v)
            => (v * v) / G;

        /// <param name="angleRad">Launch angle</param>
        /// <param name="v">Initial velocity</param>
        /// <param name="t">Time</param>
        /// <returns>The velocity after time <paramref name="t"/> or <c><see langword="null"/></c> if there is no solution</returns>
        public static float? Velocity(float angleRad, float v, float t)
        {
            float vx = v * Mathf.Cos(angleRad);
            float vy = (v * Mathf.Sin(angleRad)) - (G * t);
            float a = (vx * vx) + (vy * vy);
            if (a < 0f)
            { return null; }
            return Mathf.Sqrt(a);
        }

        /// <param name="angleRad">Launch angle</param>
        /// <param name="v">Initial velocity</param>
        /// <param name="t">Time</param>
        public static Vector2 Displacement(float angleRad, float v, float t)
        {
            float x = v * t * Mathf.Cos(angleRad);
            float y = (v * t * Mathf.Sin(angleRad)) - (0.5f * G * t * t);
            return new Vector2(x, y);
        }

        /// <param name="angleRad">Launch angle</param>
        /// <param name="v">Initial velocity</param>
        /// <param name="t">Time</param>
        public static float DisplacementX(float angleRad, float v, float t)
            => v * t * Mathf.Cos(angleRad);

        /// <param name="angleRad">Launch angle</param>
        /// <param name="v">Initial velocity</param>
        /// <param name="t">Time</param>
        public static float DisplacementY(float angleRad, float v, float t)
            => (v * t * Mathf.Sin(angleRad)) - (0.5f * G * t * t);

        /// <param name="angleRad">Launch angle</param>
        /// <param name="displacement">Displacement</param>
        /// <returns>The initial velocity or <c><see langword="null"/></c> if there is no solution</returns>
        public static float? InitialVelocity(float angleRad, Vector2 displacement)
        {
            float x = displacement.x;
            float y = displacement.y;

            float a = x * x * G;
            float b = x * Mathf.Sin(angleRad * 2f);
            float c = 2f * y * Mathf.Pow(Mathf.Cos(angleRad), 2f);
            float d = a / (b - c);
            if (d < 0f)
            { return null; }
            return Mathf.Sqrt(d);
        }

        public static float MaxRadius(float v, float heightDisplacement)
        {
            float t = CalculateTime(v, 45f * Mathf.Deg2Rad, heightDisplacement) ?? throw new Exception();
            float x = DisplacementX(45f * Mathf.Deg2Rad, t, v);
            return x;
        }

        /*
        public static float? find_shooting_angle(Vector2 target_position, float target_velocity, float shootAngle, Vector2 shooter_position, float shooter_velocity)
        {
            float tolerance = 0.01f;  // Tolerance for convergence
            int max_iterations = 100;// Maximum number of iterations
            float lower_angle = 0.0f; // Initial lower angle bound (radians)
            float upper_angle = Mathf.PI / 2; // Initial upper angle bound (radians)

            for (int i = 0; i < max_iterations; i++)
            {
                float angle = (lower_angle + upper_angle) / 2f;
                float time_of_flight_shooter = calculate_time_of_flight(angle, shooter_velocity, shooter_position);
                float time_of_flight_target = calculate_time_of_flight(shootAngle, target_velocity, target_position);

                if (Mathf.Abs(time_of_flight_shooter - time_of_flight_target) <= tolerance)
                { return angle; }
                else if (time_of_flight_shooter > time_of_flight_target)
                {
                    upper_angle = angle;
                }
                else
                {
                    lower_angle = angle;
                }
            }

            // Return None if convergence is not achieved within the maximum iterations
            return null;
        }
        */

        /// <summary>
        /// The total time for which the projectile remains in the air.
        /// <seealso href="https://en.wikipedia.org/wiki/Projectile_motion#Time_of_flight_or_total_time_of_the_whole_journey"/>
        /// </summary>
        /// <param name="v">Initial velocity</param>
        /// <param name="angleRad">Launch angle</param>
        public static float TimeOfFlight(float v, float angleRad)
            => (2f * v * Mathf.Sin(angleRad)) / G;

        public static float? TimeToReachDistance(float v, float angleRad, float d)
        {
            float a = v * Mathf.Cos(angleRad);
            if (a <= 0f)
            { return null; }
            return d / a;
        }

        public static float MaxHeight2(float d, float angleRad)
            => (d * Mathf.Tan(angleRad)) / 4;

        public static Vector2 GetPosition(Vector2 v, float t)
            => (v * t) + ((t * t * GVector) / 2);

        /// <summary>
        /// <see href="https://www.toppr.com/guides/physics/motion-in-a-plane/projectile-motion/"/>
        /// <c>y = (tan θ) * x – g (x ^ 2) / 2 * (v * cos θ) ^ 2</c>
        /// </summary>
        public static float GetHeight(float d, float angleRad, float v)
        {
            float a = Mathf.Tan(angleRad) * d;
            float b = G * d * d;
            float c = Mathf.Pow(v * Mathf.Cos(angleRad), 2) * 2f;
            return a - (b / c);
        }

        /*
        public static float calculate_time_of_flight(float angle, float velocity, Vector2 position)
        {
            // Assuming no air resistance, calculate time of flight for a projectile
            float x = position.x;  // Target position (x, y)
            float y = position.y;
            float v0 = velocity;  // Initial velocity

            float time_of_flight = (x / (v0 * Mathf.Cos(angle))) * (Mathf.Sin(angle) + Mathf.Sqrt((Mathf.Pow(Mathf.Sin(angle), 2f)) + (2f * G * y) / (v0 * v0 * Mathf.Pow(Mathf.Cos(angle), 2f))));
            return time_of_flight;
        }
        */

        public static (Vector3 PredictedPosition, float TimeToReach)? CalculateInterceptCourse(float projectileVelocity, float projectileLifetime, Vector3 shootPosition, Trajectory targetTrajectory)
        {
            float? angle_;
            float? t;
            Vector3 targetPosition;
            int iterations = 3;

            using (ProfilerMarkers.TrajectoryMath.Auto())
            {
                projectileVelocity *= .95f;

                float lifetime = projectileLifetime + Time.fixedDeltaTime;

                float? projectileTimeOfFlight = Ballistics.CalculateTime(targetTrajectory.Velocity, targetTrajectory.Angle * Mathf.Deg2Rad, targetTrajectory.StartPosition.y);

                if (projectileTimeOfFlight.HasValue && (projectileTimeOfFlight - lifetime) < .5f)
                { return null; }

                targetPosition = targetTrajectory.Position(lifetime);

                float distance = Vector2.Distance(shootPosition.To2D(), targetPosition.To2D());

                angle_ = Ballistics.AngleOfReach2(projectileVelocity, shootPosition, targetPosition);

                t = angle_.HasValue ? Ballistics.TimeToReachDistance(projectileVelocity, angle_.Value, distance) : null;

                for (int i = 0; i < iterations; i++)
                {
                    if (!angle_.HasValue) break;
                    if (!t.HasValue) break;

                    targetPosition = targetTrajectory.Position(lifetime + t.Value);

                    distance = Vector2.Distance(shootPosition.To2D(), targetPosition.To2D());

                    angle_ = Ballistics.AngleOfReach2(projectileVelocity, shootPosition, targetPosition);

                    t = angle_.HasValue ? Ballistics.TimeToReachDistance(projectileVelocity, angle_.Value, distance) : null;
                }
            }

            return (targetPosition, t.Value);
        }

        public static Vector2? CalculateInterceptCourse(Vector2 projectilePosition, float projectileVelocity, Vector2 targetPosition, Vector2 targetVelocity)
        {
            float time = 0f;
            int iterations = 3;
            Vector2 targetOriginalPosition = targetPosition;

            float height = projectilePosition.y - targetPosition.y;

            using (ProfilerMarkers.TrajectoryMath.Auto())
            {
                projectileVelocity *= .95f;

                for (int i = 0; i < iterations; i++)
                {
                    float? _angle = Ballistics.AngleOfReach2(projectileVelocity, projectilePosition, targetPosition);
                    if (!_angle.HasValue)
                    { return null; }
                    float angle = _angle.Value;

                    float? _time = Ballistics.CalculateTime(projectileVelocity, angle, height);
                    if (!_time.HasValue)
                    { return null; }
                    time = _time.Value;

                    targetPosition = targetOriginalPosition + (targetVelocity * time);
                }
            }

            return targetVelocity * time;
        }
    }

    internal struct Line
    {
        public Vector2 PointA;
        public Vector2 PointB;

        public Line(Vector2 pointA, Vector2 pointB)
        {
            PointA = pointA;
            PointB = pointB;
        }

        public static Line Zero => new(Vector2.zero, Vector2.zero);

        public static implicit operator (Vector2, Vector2)(Line v)
            => (v.PointA, v.PointB);
        public static implicit operator Line((Vector2, Vector2) v)
            => (v.Item1, v.Item2);

        public static Line operator +(Line a, Vector2 b)
            => new(a.PointA + b, a.PointB + b);
        public static Line operator -(Line a, Vector2 b)
            => new(a.PointA - b, a.PointB - b);
        public static Line operator *(Line a, Vector2 b)
            => new(a.PointA * b, a.PointB * b);
        public static Line operator *(Line a, float b)
            => new(a.PointA * b, a.PointB * b);

        public static Vector2? Line2LineIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            // Line AB represented as a1x + b1y = c1
            float a1 = b.y - a.y;
            float b1 = a.x - b.x;
            float c1 = a1 * (a.x) + b1 * (a.y);

            // Line CD represented as a2x + b2y = c2
            float a2 = d.y - c.y;
            float b2 = c.x - d.x;
            float c2 = a2 * (c.x) + b2 * (c.y);

            float determinant = a1 * b2 - a2 * b1;

            if (determinant == 0)
            {
                // The lines are parallel. This is simplified
                // by returning a pair of FLT_MAX
                return null;
            }
            else
            {
                float x = (b2 * c1 - b1 * c2) / determinant;
                float y = (a1 * c2 - a2 * c1) / determinant;
                return new Vector2(x, y);
            }
        }

        public static bool IsBetween(Vector2 a, Vector2 b, Vector2 c)
        {
            var crossproduct = (c.y - a.y) * (b.x - a.x) - (c.x - a.x) * (b.y - a.y);

            //  compare versus epsilon for floating point values, or != 0 if using integers
            if (Mathf.Abs(crossproduct) > 0.0001f)
            { return false; }

            var dotproduct = (c.x - a.x) * (b.x - a.x) + (c.y - a.y) * (b.y - a.y);
            if (dotproduct < 0)
            { return false; }

            var squaredlengthba = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
            if (dotproduct > squaredlengthba)
            { return false; }

            return true;

        }
    }

    /// <summary>
    /// A collection of generic math functions.
    /// </summary>
    internal struct Math
    {
        internal static float QuadraticEquation(float a, float b, float c, float sign)
        {
            float discriminant = (b * b) - (4 * a * c);
            return (-b + sign * Mathf.Sqrt(discriminant)) / (2 * a);
        }
        internal static (float, float) QuadraticEquation(float a, float b, float c)
        {
            float discriminant = (b * b) - (4 * a * c);
            float dqrt = Mathf.Sqrt(discriminant);
            float x1 = (-b + dqrt) / (2 * a);
            float x2 = (-b - dqrt) / (2 * a);

            return (x1, x2);
        }
        internal static float Sum(params float[] values)
        {
            var sum = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
            }
            return sum;
        }
        internal static float Sum(float a)
        {
            return a;
        }
        internal static float Sum(float a, float b)
        {
            return a + b;
        }

        internal static float Average(params float[] values)
        {
            return Sum(values) / values.Length;
        }
        internal static float Average(float a, float b)
        {
            return (a + b) / 2;
        }
        internal static float Average(float a)
        {
            return a;
        }

        internal static float Difference(float a, float b)
        {
            return Mathf.Abs(a - b);
        }
        internal static Vector2 Difference(Vector2 a, Vector2 b)
        {
            return new Vector2(
                    Difference(a.x, b.x),
                    Difference(a.y, b.y)
                );
        }
        internal static Vector3 Difference(Vector3 a, Vector3 b)
        {
            return new Vector3(
                    Difference(a.x, b.x),
                    Difference(a.y, b.y),
                    Difference(a.z, b.z)
                );
        }

        internal static Vector3 Mult(Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);

        internal struct Circle
        {
            internal Vector2 center;
            internal float radius;

            internal Circle(Vector2 center, float radius)
            {
                this.center = center;
                this.radius = radius;
            }

            internal readonly void DebugPrint()
            {
                Debug.Log("Circle { center: { x: " + center.x.ToString() + ", y: " + center.y.ToString() + "}, radius: " + radius.ToString() + "}");
            }

            /// <param name="angle">Angle in radians</param>
            internal readonly Vector2 GetPoint(float angle)
            {
                float x = this.radius * Mathf.Cos(angle) + this.center.x;
                float y = this.radius * Mathf.Sin(angle) + this.center.y;
                return new Vector2(x, y);
            }

            /// <param name="angleOffset">Angle in radians</param>
            internal readonly Vector2 GetPointAfterTime(float speed, float time, float angleOffset)
                => GetPoint(GetAngle(speed, time) + (angleOffset));

            internal readonly float GetAngle(Vector2 pointOnCircle)
                => Mathf.Atan2(pointOnCircle.y - this.center.y, pointOnCircle.x - this.center.y);

            internal readonly float GetAngle(float speed, float time)
                => GetAngle(speed * time);

            internal readonly float GetAngle(float distance)
                => distance / this.radius;

            internal static float Circumference(float radius)
                => Mathf.PI * 2 * radius;

            internal readonly float Circumference()
                => Mathf.PI * 2 * radius;

            internal static Vector2[] GenerateEquadistancePoints(int n, float radius)
            {
                List<Vector2> points = new();

                for (int i = 0; i < n; i++)
                {
                    var k = i + .5f;
                    var r = Mathf.Sqrt((k) / n);
                    var theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * k;
                    var x = r * Mathf.Cos(theta) * radius;
                    var y = r * Mathf.Sin(theta) * radius;
                    points.Add(new Vector2(x, y));
                }

                return points.ToArray();
            }
        }

        internal static float IsStraightLine(Vector2 positionA, Vector2 positionB, Vector2 positionC)
            => (positionA.x * (positionB.y - positionC.y) + positionB.x * (positionC.y - positionA.y) + positionC.x * (positionA.y - positionB.y)) / 2;

        internal static Circle FincCircle(Vector2 positionA, Vector2 positionB, Vector2 positionC)
            => FindCircle(positionA.x, positionA.y, positionB.x, positionB.y, positionC.x, positionC.y);
        internal static Circle FindCircle(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            float x12 = x1 - x2;
            float x13 = x1 - x3;

            float y12 = y1 - y2;
            float y13 = y1 - y3;

            float y31 = y3 - y1;
            float y21 = y2 - y1;

            float x31 = x3 - x1;
            float x21 = x2 - x1;

            float sx13 = Mathf.Pow(x1, 2) - Mathf.Pow(x3, 2);
            float sy13 = Mathf.Pow(y1, 2) - Mathf.Pow(y3, 2);
            float sx21 = Mathf.Pow(x2, 2) - Mathf.Pow(x1, 2);
            float sy21 = Mathf.Pow(y2, 2) - Mathf.Pow(y1, 2);

            float f = ((sx13) * (x12)
                    + (sy13) * (x12)
                    + (sx21) * (x13)
                    + (sy21) * (x13))
                    / (2 * ((y31) * (x12) - (y21) * (x13)));
            float g = ((sx13) * (y12)
                    + (sy13) * (y12)
                    + (sx21) * (y13)
                    + (sy21) * (y13))
                    / (2 * ((x31) * (y12) - (x21) * (y13)));

            float c = -Mathf.Pow(x1, 2) - Mathf.Pow(y1, 2) - 2 * g * x1 - 2 * f * y1;
            float h = g * -1;
            float k = f * -1;
            float sqr_of_r = h * h + k * k - c;

            float r = (sqr_of_r < 0) ? 0f : Mathf.Sqrt(sqr_of_r);

            return new Circle(new Vector2(h, k), r);
        }

        /// <returns>In degrees</returns>
        internal static float GetAngleFromVectorFloat(Vector3 dir)
        {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (n < 0) n += 360;

            return n;
        }

        /// <returns>In degrees</returns>
        internal static float GetAngleFromVectorFloat(Vector2 dir)
        {
            dir = dir.normalized;
            float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (n < 0) n += 360;

            return n;
        }

        internal static Vector2 RadianToVector2(float radian)
        { return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian)); }
        internal static Vector2 DegreeToVector2(float degree)
        { return RadianToVector2(degree * Mathf.Deg2Rad); }

        internal static float NormalizeDegree(float degree)
        { return (degree + 360) % 360; }

        internal static Vector3 LengthDir(Vector3 center, float angle, float distance)
        {
            float x = distance * Mathf.Cos((90 + angle) * Mathf.Deg2Rad);
            float y = distance * Mathf.Sin((90 + angle) * Mathf.Deg2Rad);
            Vector3 newPosition = center;
            newPosition.x += x;
            newPosition.y += y;
            return newPosition;
        }

        internal static int BoolToInt(bool val)
        { return val ? 1 : 0; }
        internal static bool IntToBool(int val)
        {
            if (val == 0)
            {
                return false;
            }
            if (val == 1)
            {
                return true;
            }

            throw new System.Exception("Unable to convert int " + val.ToString() + " to bool");
        }

        class PointGroup
        {
            internal int GroupID { get; set; }
            internal Vector2 Point1 { get; set; }
            internal bool IsGrouped { get; set; }
        }

        static PointGroup[] GeneratePointGroups(Vector2[] points)
        {
            List<PointGroup> groups = new();
            for (int i = 0; i < points.Length; i++)
            {
                groups.Add(new PointGroup() { GroupID = i, IsGrouped = false, Point1 = points[i] });
            }
            return groups.ToArray();
        }

        static Vector2[][] GetGroupsFromGroups(PointGroup[] pointGroups)
        {
            List<List<Vector2>> vector2s = new();
            Dictionary<int, int> groupIdToIndex = new();
            for (int i = 0; i < pointGroups.Length; i++)
            {
                if (groupIdToIndex.TryGetValue(pointGroups[i].GroupID, out int groupIndex))
                {
                    vector2s[groupIndex].Add(pointGroups[i].Point1);
                }
                else
                {
                    vector2s.Add(new List<Vector2>());
                    groupIdToIndex.Add(pointGroups[i].GroupID, vector2s.Count - 1);
                }
            }
            List<Vector2[]> vector2s1 = new();
            foreach (var item in vector2s)
            {
                vector2s1.Add(item.ToArray());
            }
            return vector2s1.ToArray();
        }

        internal static Vector2[][] GroupPoints(Vector2[] points, float tolerance)
        {
            PointGroup[] colls = GeneratePointGroups(points);
            for (int i = 0; i < colls.Length; i++)
            {
                PointGroup pg1 = colls[i];
                if (!pg1.IsGrouped)
                {
                    for (int j = 0; j < colls.Length; j++)
                    {
                        PointGroup pg2 = colls[j];
                        if (pg1.Point1.AreEquals(pg2.Point1, tolerance) && pg2.IsGrouped == false)
                        {
                            if (pg2.GroupID == j)
                            {
                                pg2.GroupID = pg1.GroupID;
                                pg2.IsGrouped = true;
                            }
                        }
                    }

                    pg1.IsGrouped = true;
                }
            }
            return GetGroupsFromGroups(colls);
        }

        /// <returns>(lowerLeft, upperRight)</returns>
        internal static (Vector2, Vector2) GetRect(Vector2 a, Vector2 b)
        {
            Vector2 lowerLeft = new(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y));
            Vector2 upperRight = new(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            return (lowerLeft, upperRight);
        }

        /// <returns>(lowerLeft, upperRight)</returns>
        internal static (Vector2, Vector2) GetRect(Transform a, Transform b)
        {
            return GetRect(a.position, b.position);
        }

        /// <param name="p1">Angle peak</param>
        internal static float CalculateAngle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float numerator = p2.y * (p1.x - p3.x) + p1.y * (p3.x - p2.x) + p3.y * (p2.x - p1.x);
            float denominator = (p2.x - p1.x) * (p1.x - p3.x) + (p2.y - p1.y) * (p1.y - p3.y);
            float ratio = numerator / denominator;

            float angleRad = Mathf.Atan(ratio);
            float angleDeg = (angleRad * 180) / Mathf.PI;

            if (angleDeg < 0)
            {
                angleDeg = 180 + angleDeg;
            }

            return angleDeg;
        }

        internal static float MapToRange(float outputStart, float outputEnd, float percent)
        {
            /* Note, "slope" below is a constant for given numbers, so if you are calculating
               a lot of output values, it makes sense to calculate it once.  It also makes
               understanding the Code easier */
            var slope = outputEnd - outputStart;
            var output = outputStart + slope * percent;
            return output;
        }

        internal static float MapToRange(float outputStart, float outputEnd, float inputStart, float inputEnd, float input)
        {
            /* Note, "slope" below is a constant for given numbers, so if you are calculating
               a lot of output values, it makes sense to calculate it once.  It also makes
               understanding the Code easier */
            var slope = (outputEnd - outputStart) / (inputEnd - inputStart);
            var output = outputStart + slope * (input - inputStart);
            return output;
        }
    }

    internal static class Acceleration
    {
        internal const float LargeNumber = 69420f;

        /// <summary>
        /// If <paramref name="time"/> is 0 it returns <see cref="LargeNumber"/> to avoid divison by zero
        /// </summary>
        internal static float CalculateAcceleration(float initialVelocity, float topVelocity, float time)
        {
            if (time == 0f) return LargeNumber;
            return (topVelocity - initialVelocity) / time;
        }

        internal static float SpeedAfterTime(float velocity, float acceleration, float time)
        {
            return velocity + (acceleration * time);
        }

        internal static float SpeedAfterDistance(float velocity, float acceleration, float distance)
        {
            if (acceleration == 0f) return velocity;
            if (distance == 0f) return velocity;

            float valueUnderSqr = (2 * acceleration * distance + (velocity * velocity));
            if (valueUnderSqr <= 0f) return 0f;

            return Mathf.Sqrt(valueUnderSqr);
        }

        /// <summary>
        /// <b>v * t + ½ * a * t²</b> <br/><br/>
        /// 
        /// v: <paramref name="velocity"/> <br/>
        /// a: <paramref name="acceleration"/> <br/>
        /// t: <paramref name="time"/> <br/>
        /// </summary>
        internal static float DistanceAfterTime(float velocity, float acceleration, float time)
        {
            return (velocity * time) + ((acceleration / 2) * (time * time));
        }

        /// <summary>
        /// <b>Δv / a</b> <br/>
        /// or <br/>
        /// <b>(v - vₒ) / a</b> <br/><br/>
        /// 
        /// If <paramref name="targetVelocity"/> can't be reached, it returns <see cref="LargeNumber"/> to avoid divison by zero. <br/><br/>
        /// 
        /// v: <paramref name="targetVelocity"/> <br/>
        /// vₒ: <paramref name="initialVelocity"/> <br/>
        /// a: <paramref name="acceleration"/> <br/>
        /// </summary>
        internal static float TimeToReachVelocity(float initialVelocity, float targetVelocity, float acceleration)
        {
            if (acceleration == 0f) return LargeNumber;
            if (initialVelocity < targetVelocity && acceleration < 0f) return LargeNumber;
            if (initialVelocity > targetVelocity && acceleration > 0f) return LargeNumber;

            return (targetVelocity - initialVelocity) / acceleration;
        }

        /// <summary>
        /// <b>-vₒ / a</b> <br/><br/>
        /// 
        /// If 0 velocity can't be reached, it returns <see cref="LargeNumber"/> to avoid divison by zero. <br/><br/>
        /// 
        /// vₒ: <paramref name="initialVelocity"/> <br/>
        /// a: <paramref name="acceleration"/> <br/>
        /// </summary>
        internal static float TimeToStop(float initialVelocity, float acceleration)
        {
            if (acceleration == 0f) return LargeNumber;
            if (initialVelocity < 0f && acceleration < 0f) return LargeNumber;
            if (initialVelocity > 0f && acceleration > 0f) return LargeNumber;

            return (-initialVelocity) / acceleration;
        }

        /// <summary>
        /// <b>vₒ * t + ½ * a * t²</b> <br/><br/>
        /// 
        /// v: <paramref name="targetVelocity"/> <br/>
        /// vₒ: <paramref name="initialVelocity"/> <br/>
        /// a: <paramref name="acceleration"/> <br/>
        /// t: <see cref="TimeToReachVelocity(float, float, float)"/> <br/>
        /// </summary>
        internal static float DistanceToReachVelocity(float initialVelocity, float targetVelocity, float acceleration)
        {
            float time = TimeToReachVelocity(initialVelocity, targetVelocity, acceleration);

            if (time == 0f) return 0f;

            return DistanceAfterTime(initialVelocity, acceleration, time);
        }

        /// <summary>
        /// <b>vₒ * t + ½ * -a * t²</b> <br/><br/>
        /// 
        /// vₒ: <paramref name="velocity"/> <br/>
        /// a: -<paramref name="braking"/> <br/>
        /// t: <see cref="TimeToStop(float, float)"/> <br/>
        /// </summary>
        internal static float DistanceToStop(float velocity, float braking)
        {
            float time = TimeToStop(velocity, -braking);

            if (time == 0f) return 0f;

            return DistanceAfterTime(velocity, -braking, time);
        }

        /// <summary>
        /// <b>(vₒ + v)/2 * t</b> <br/><br/>
        /// 
        /// vₒ: <paramref name="initialVelocity"/> <br/>
        /// vₒ: <paramref name="topVelocity"/> <br/>
        /// t: <paramref name="time"/> <br/>
        /// </summary>
        internal static float CalculateDistanceFromSpeed(float initialVelocity, float topVelocity, float time)
        {
            return Math.Average(initialVelocity, topVelocity) * time;
        }

        internal static float CalculateTime(float initialVelocity, float topVelocity, float timeToSpeedUp, float distance, float acceleration)
        {
            float distanceTravelledUntilMaxSpeed = DistanceAfterTime(initialVelocity, acceleration, timeToSpeedUp);
            float timeWithMaxVelocity = Velocity.CalculateTime(distance - distanceTravelledUntilMaxSpeed, topVelocity);
            return timeToSpeedUp + timeWithMaxVelocity;
        }

        /// <returns>Aim offset</returns>
        internal static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 projectilePosition, float projectileVelocity, float projectileAcceleration)
        {
            float distance;
            float time = 0f;

            int iterations = 3;
            for (int i = 0; i < iterations; i++)
            {
                distance = Vector2.Distance(projectilePosition, targetPosition + (targetVelocity * time));
                float speedAfterThis = SpeedAfterDistance(projectileVelocity, projectileAcceleration, distance);
                time = TimeToReachVelocity(projectileVelocity, speedAfterThis, projectileAcceleration);
            }

            return targetVelocity * time;
        }

        /// <returns>Aim offset</returns>
        internal static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 targetAcceleration, Vector2 projectilePosition, float projectileVelocity, float projectileAcceleration)
        {
            Vector2 targetOriginalVelocity = targetVelocity;
            float distance;
            float time = 0f;

            int iterations = 4;
            for (int i = 0; i < iterations; i++)
            {
                distance = Vector2.Distance(projectilePosition, targetPosition + (targetVelocity * time));
                float speedAfterThis = SpeedAfterDistance(projectileVelocity, projectileAcceleration, distance);
                time = TimeToReachVelocity(projectileVelocity, speedAfterThis, projectileAcceleration);
                targetVelocity = targetOriginalVelocity.normalized * SpeedAfterTime(targetOriginalVelocity.magnitude, targetAcceleration.magnitude, time);
            }

            return targetVelocity * time;
        }

        /// <returns>Aim offset</returns>
        internal static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 targetAcceleration, Vector2 projectilePosition, float projectileVelocity)
        {
            Vector2 targetOriginalVelocity = targetVelocity;
            float distance;
            float time = 0f;

            int iterations = 4;
            for (int i = 0; i < iterations; i++)
            {
                distance = Vector2.Distance(projectilePosition, targetPosition + (targetVelocity * time));
                time = Velocity.CalculateTime(distance, projectileVelocity);
                targetVelocity = targetOriginalVelocity.normalized * SpeedAfterTime(targetOriginalVelocity.magnitude, targetAcceleration.magnitude, time);
            }

            return targetVelocity * time;
        }
    }

    internal static class Velocity
    {
        internal static float CalculateTime(Vector2 pointA, Vector2 pointB, float speed)
        {
            return CalculateTime(Vector2.Distance(pointA, pointB), speed);
        }

        internal static float CalculateSpeed(float distance, float time)
        {
            if (time == 0f) return 0f;
            return (distance / time);
        }

        internal static float CalculateDistance(float velocity, float time)
        {
            return velocity * time;
        }

        internal static float CalculateTime(float distance, float velocity)
        {
            if (velocity == 0f) return 0f;
            return distance / velocity;
        }

        /// <returns>Aim offset</returns>
        internal static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 projectilePosition, float projectileVelocity)
        {
            float distance;
            float time = 0f;

            int iterations = 3;
            for (int i = 0; i < iterations; i++)
            {
                distance = Vector2.Distance(projectilePosition, targetPosition + (targetVelocity * time));
                time = CalculateTime(distance, projectileVelocity);
            }

            return targetVelocity * time;
        }
        /// <returns>Aim offset</returns>
        internal static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 projectilePosition, float projectileVelocity, Math.Circle circle)
        {
            float p = 1 / projectileVelocity;

            float distance = Vector2.Distance(projectilePosition, targetPosition);
            float time = distance * p;

            distance = Vector2.Distance(projectilePosition, circle.GetPointAfterTime(targetVelocity.magnitude, time, circle.GetAngle(targetPosition)));
            time = distance * p;

            distance = Vector2.Distance(projectilePosition, circle.GetPointAfterTime(targetVelocity.magnitude, time, circle.GetAngle(targetPosition)));
            time = distance * p;

            Vector2 aim = circle.GetPointAfterTime(targetVelocity.magnitude, time, circle.GetAngle(targetPosition));
            return targetPosition - aim;
        }
    }

    internal static class Debug3D
    {
        internal static void DrawMesh(Mesh mesh, Color color, float duration)
        {
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (i + 1 < vertices.Length)
                {
                    UnityEngine.Debug.DrawLine(vertices[i], vertices[i + 1], color, duration);

                    if (i + 2 < vertices.Length)
                    {
                        UnityEngine.Debug.DrawLine(vertices[i + 1], vertices[i + 2], color, duration);
                        UnityEngine.Debug.DrawLine(vertices[i + 2], vertices[i], color, duration);
                    }
                }
            }
        }

        internal static void Label(float x, float y, float z, string content)
        {
#if UNITY_EDITOR
            Label(new Vector3(x, y, z), content);
#endif
        }

        internal static void Label(Vector3 position, string content)
        {
#if UNITY_EDITOR
            Handles.Label(position, content);
#endif
        }


        /// <summary>
        /// Square with edge of length 1
        /// </summary>
#pragma warning disable IDE0052 // Remove unread private members
        static readonly Vector4[] s_UnitSquare =
#pragma warning restore IDE0052 // Remove unread private members
        {
            new Vector4(-0.5f, 0.5f, 0, 1),
            new Vector4(0.5f, 0.5f, 0, 1),
            new Vector4(0.5f, -0.5f, 0, 1),
            new Vector4(-0.5f, -0.5f, 0, 1),
        };
        /// <summary>
        /// Cube with edge of length 1
        /// </summary>
        static readonly Vector4[] s_UnitCube =
        {
            new Vector4(-0.5f,  0.5f, -0.5f, 1),
            new Vector4(0.5f,  0.5f, -0.5f, 1),
            new Vector4(0.5f, -0.5f, -0.5f, 1),
            new Vector4(-0.5f, -0.5f, -0.5f, 1),

            new Vector4(-0.5f,  0.5f,  0.5f, 1),
            new Vector4(0.5f,  0.5f,  0.5f, 1),
            new Vector4(0.5f, -0.5f,  0.5f, 1),
            new Vector4(-0.5f, -0.5f,  0.5f, 1)
        };
        static readonly Vector4[] s_UnitSphere = MakeUnitSphere(16);

        static Vector4[] MakeUnitSphere(int len)
        {
            Debug.Assert(len > 2);
            var v = new Vector4[len * 3];
            for (int i = 0; i < len; i++)
            {
                var f = i / (float)len;
                float c = Mathf.Cos(f * Mathf.PI * 2f);
                float s = Mathf.Sin(f * Mathf.PI * 2f);
                v[0 * len + i] = new Vector4(c, s, 0, 1);
                v[1 * len + i] = new Vector4(0, c, s, 1);
                v[2 * len + i] = new Vector4(s, 0, c, 1);
            }
            return v;
        }

        internal static void DrawSphere(Vector4 pos, float radius, Color color)
        {
            Vector4[] v = s_UnitSphere;
            int len = v.Length / 3;
            for (int i = 0; i < len; i++)
            {
                Vector4 sX = pos + radius * v[0 * len + i];
                Vector4 eX = pos + radius * v[0 * len + (i + 1) % len];
                Debug.DrawLine(sX, eX, color);

                Vector4 sY = pos + radius * v[1 * len + i];
                Vector4 eY = pos + radius * v[1 * len + (i + 1) % len];
                Debug.DrawLine(sY, eY, color);

                Vector4 sZ = pos + radius * v[2 * len + i];
                Vector4 eZ = pos + radius * v[2 * len + (i + 1) % len];
                Debug.DrawLine(sZ, eZ, color);
            }
        }

        internal static void DrawSphere(Vector4 pos, float radius, Color color, float duration)
        {
            Vector4[] v = s_UnitSphere;
            int len = v.Length / 3;
            for (int i = 0; i < len; i++)
            {
                Vector4 sX = pos + radius * v[0 * len + i];
                Vector4 eX = pos + radius * v[0 * len + (i + 1) % len];
                Debug.DrawLine(sX, eX, color, duration);

                Vector4 sY = pos + radius * v[1 * len + i];
                Vector4 eY = pos + radius * v[1 * len + (i + 1) % len];
                Debug.DrawLine(sY, eY, color, duration);

                Vector4 sZ = pos + radius * v[2 * len + i];
                Vector4 eZ = pos + radius * v[2 * len + (i + 1) % len];
                Debug.DrawLine(sZ, eZ, color, duration);
            }
        }

        internal static void DrawBox(Vector4 pos, Vector3 size, Color color)
        {
            Vector4[] v = s_UnitCube;
            Vector4 sz = new(size.x, size.y, size.z, 1);
            for (int i = 0; i < 4; i++)
            {
                var s = pos + Vector4.Scale(v[i], sz);
                var e = pos + Vector4.Scale(v[(i + 1) % 4], sz);
                Debug.DrawLine(s, e, color);
            }
            for (int i = 0; i < 4; i++)
            {
                var s = pos + Vector4.Scale(v[4 + i], sz);
                var e = pos + Vector4.Scale(v[4 + ((i + 1) % 4)], sz);
                Debug.DrawLine(s, e, color);
            }
            for (int i = 0; i < 4; i++)
            {
                var s = pos + Vector4.Scale(v[i], sz);
                var e = pos + Vector4.Scale(v[i + 4], sz);
                Debug.DrawLine(s, e, color);
            }
        }

        internal static void DrawBox(Vector4 pos, Vector3 size, Color color, float duration)
        {
            Vector4[] v = s_UnitCube;
            Vector4 sz = new(size.x, size.y, size.z, 1);
            for (int i = 0; i < 4; i++)
            {
                var s = pos + Vector4.Scale(v[i], sz);
                var e = pos + Vector4.Scale(v[(i + 1) % 4], sz);
                Debug.DrawLine(s, e, color, duration);
            }
            for (int i = 0; i < 4; i++)
            {
                var s = pos + Vector4.Scale(v[4 + i], sz);
                var e = pos + Vector4.Scale(v[4 + ((i + 1) % 4)], sz);
                Debug.DrawLine(s, e, color, duration);
            }
            for (int i = 0; i < 4; i++)
            {
                var s = pos + Vector4.Scale(v[i], sz);
                var e = pos + Vector4.Scale(v[i + 4], sz);
                Debug.DrawLine(s, e, color, duration);
            }
        }

        internal static void DrawBox(Bounds bounds, Color color)
            => DrawBox(bounds.center, bounds.size, color);

        internal static void DrawBox(Bounds bounds, Color color, float duration)
            => DrawBox(bounds.center, bounds.size, color, duration);

        internal static void DrawAxes(Vector4 pos)
            => DrawAxes(pos, 1f);

        internal static void DrawAxes(Vector4 pos, float scale)
        {
            Debug.DrawLine(pos, pos + new Vector4(scale, 0, 0), Color.red);
            Debug.DrawLine(pos, pos + new Vector4(0, scale, 0), Color.green);
            Debug.DrawLine(pos, pos + new Vector4(0, 0, scale), Color.blue);
        }

        internal static void DrawAxes(Vector4 pos, float scale, float duration)
        {
            Debug.DrawLine(pos, pos + new Vector4(scale, 0, 0), Color.red, duration);
            Debug.DrawLine(pos, pos + new Vector4(0, scale, 0), Color.green, duration);
            Debug.DrawLine(pos, pos + new Vector4(0, 0, scale), Color.blue, duration);
        }

        internal static void DrawPoint(Vector3 position, float scale, Color color)
        {
            Vector3 up = Vector3.up * scale;
            Vector3 right = Vector3.right * scale;
            Vector3 forward = Vector3.forward * scale;

            Debug.DrawLine(position - up, position + up, color);
            Debug.DrawLine(position - right, position + right, color);
            Debug.DrawLine(position - forward, position + forward, color);
        }

        internal static void DrawPoint(Vector3 position, float scale, Color color, float duration)
        {
            Vector3 up = Vector3.up * scale;
            Vector3 right = Vector3.right * scale;
            Vector3 forward = Vector3.forward * scale;

            Debug.DrawLine(position - up, position + up, color, duration);
            Debug.DrawLine(position - right, position + right, color, duration);
            Debug.DrawLine(position - forward, position + forward, color, duration);
        }
    }

    internal struct AI
    {
        class PriorityComparer<T> : IComparer<(T, float)>
        {
            public int Compare((T, float) a, (T, float) b)
                => Comparer.Default.Compare(a.Item2, b.Item2);
        }

        public delegate float GetPriority<T>(T @object);

        public static void SortTargets<T>(T[] targets, GetPriority<T> getPriority) where T : UnityEngine.Object
        {
            (T, float)[] priorities = new (T, float)[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                float priority = 0f;

                if ((targets[i] as UnityEngine.Object) != null)
                { priority = getPriority?.Invoke(targets[i]) ?? 0f; }

                priorities[i] = (targets[i], priority);
            }

            Array.Sort(priorities, new PriorityComparer<T>());

            for (int i = 0; i < targets.Length; i++)
            { targets[i] = priorities[i].Item1; }
        }

        public static void SortTargets<T>(IList<T> targets, GetPriority<T> getPriority) where T : UnityEngine.Object
        {
            (T, float)[] priorities = new (T, float)[targets.Count];

            for (int i = 0; i < targets.Count; i++)
            {
                float priority = 0f;

                if ((targets[i] as UnityEngine.Object) != null)
                { priority = getPriority?.Invoke(targets[i]) ?? 0f; }

                priorities[i] = (targets[i], priority);
            }

            Array.Sort(priorities, new PriorityComparer<T>());

            for (int i = 0; i < priorities.Length; i++)
            { targets[i] = priorities[i].Item1; }
        }

        public static void SortTargets(BaseObject[] targets, Vector3 origin, string team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.Team, team));

        public static void SortTargets(IList<BaseObject> targets, Vector3 origin, string team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.Team, team));

        public static void SortTargets(BaseObject[] targets, Vector3 origin, int team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.TeamHash, team));

        public static void SortTargets(IList<BaseObject> targets, Vector3 origin, int team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.TeamHash, team));
    }
}

namespace Game
{
    [Serializable]
    public struct CursorConfig
    {
        public Texture2D Texture;
        public Vector2 Hotspot;

        public readonly void Set() => CursorManager.SetCursor(Texture, Hotspot);
    }

    internal static class ObjectGroups
    {
        static Transform game;
        static Transform effects;
        static Transform projectiles;
        static Transform items;

        public static Transform Game
        {
            get
            {
                if (game == null)
                {
                    GameObject obj = GameObject.Find("Game");
                    if (obj == null)
                    { obj = new GameObject("Game"); }
                    game = obj.transform;
                }
                return game;
            }
        }

        public static Transform Effects
        {
            get
            {
                GameObject obj = GameObject.Find("Effects");
                if (obj == null)
                { obj = new GameObject("Effects"); }
                effects = obj.transform;
                return effects;
            }
        }

        public static Transform Projectiles
        {
            get
            {
                GameObject obj = GameObject.Find("Projectiles");
                if (obj == null)
                { obj = new GameObject("Projectiles"); }
                projectiles = obj.transform;
                return projectiles;
            }
        }

        public static Transform Items
        {
            get
            {
                GameObject obj = GameObject.Find("Items");
                if (obj == null)
                { obj = new GameObject("Items"); }
                items = obj.transform;
                return items;
            }
        }
    }

    internal static class TheTerrain
    {
        static Terrain terrain;
        static float terrainBaseHeight = 0f;
        static bool hasTerrain = false;

        static void FindTerrainIfNone()
        {
            if (!hasTerrain)
            {
                terrain = GameObject.FindObjectOfType<Terrain>();
                hasTerrain = terrain != null;

                if (hasTerrain)
                { terrainBaseHeight = terrain.transform.position.y; }
            }
            else
            {
                hasTerrain = terrain != null;
            }
        }

        internal static Terrain Terrain
        {
            get
            {
                FindTerrainIfNone();

                return terrain;
            }
        }

        internal static float Height(Vector3 position)
        {
            FindTerrainIfNone();

            if (hasTerrain)
            { return terrain.SampleHeight(position) + terrainBaseHeight; }
            else
            { return 0f; }
        }
    }
}

internal readonly struct ProfilerMarkers
{
    internal static readonly Unity.Profiling.ProfilerMarker Animations = new("Utilities.Animations");
    internal static readonly Unity.Profiling.ProfilerMarker UnitsBehaviour = new("Game.Units.Behaviour");
    internal static readonly Unity.Profiling.ProfilerMarker VehicleEngine_Wheels = new("Game.VehicleEngine.Wheels");
    internal static readonly Unity.Profiling.ProfilerMarker VehicleEngine_Basic = new("Game.VehicleEngine.Basic");
    internal static readonly Unity.Profiling.ProfilerMarker TrajectoryMath = new("Game.Math.Trajectory");
}

internal static class NetcodeUtils
{
    public static bool IsServer => !IsOffline && NetworkManager.Singleton.IsServer;

    public static bool IsOfflineOrServer => IsOffline || NetworkManager.Singleton.IsServer;

    public static bool IsActiveOfflineOrServer => IsActiveOffline || NetworkManager.Singleton.IsServer;

    /// <summary>
    /// Returns <see langword="true"/> if there is <b>no <see cref="NetworkManager.Singleton"/></b>, if it is running in <b>offline mode</b>, or if the <see cref="NetworkManager"/> is <b>not listening</b>.
    /// </summary>
    public static bool IsOffline => IsActiveOffline || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

    public static bool IsActiveOffline => OfflineManager.IsActiveOffline;

    internal static bool IsClient => !IsOffline && NetworkManager.Singleton.IsClient;

    public static bool FindNetworkObject(ulong id, out NetworkObject networkObject)
    {
        if (NetcodeUtils.IsOffline)
        {
            networkObject = null;
            return false;
        }
        return NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out networkObject);
    }

    public static bool FindGameObject(ulong id, out GameObject @object)
    {
        if (!NetcodeUtils.FindNetworkObject(id, out NetworkObject networkObject))
        {
            @object = null;
            return false;
        }
        @object = networkObject.gameObject;
        return true;
    }

    /// <returns>
    ///   Current NetworkConfig as a string depending on the current transport as follows:
    ///   <list type="table">
    ///     <item>
    ///       <term> <see cref="UnityTransport"/> </term>
    ///       <description> Socket </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="WebSocketTransport"/> </term>
    ///       <description> URL </description>
    ///     </item>
    ///     <item>
    ///       <term> <see cref="OfflineTransport"/> </term>
    ///       <description> "offline" </description>
    ///     </item>
    ///   </list>
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public static string NetworkConfig
    {
        get
        {
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is UnityTransport unityTransport)
            { return $"{unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}"; }

            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is WebSocketTransport webSocketTransport)
            { return $"{(webSocketTransport.SecureConnection ? "wss" : "ws")}://{webSocketTransport.ConnectAddress}:{webSocketTransport.Port}{webSocketTransport.Path}"; }

            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is OfflineTransport)
            { return "offline"; }

            throw new NotImplementedException($"Unknown netcode transport {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType()}");
        }
    }

    static IEnumerator SetConnectionData(string input, Action<string> callback, UnityEngine.Object context = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            callback?.Invoke("Input is empty");
            yield break;
        }

        input = input.Trim();

        if (!input.Contains("://"))
        {
            input = "udp://" + input;
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out Uri uri))
        {
            callback?.Invoke("Invalid URI");
            yield break;
        }

        switch (uri.Scheme)
        {
            case "udp":
                yield return SetUDPConnectionData(uri, callback, context);
                yield break;

            case "ws":
            case "wss":
                yield return SetWebSocketConnectionData(uri, callback, context);
                yield break;

            default:
                callback?.Invoke($"Unknown scheme \"{uri.Scheme}\"");
                yield break;
        }
    }

    static IEnumerator SetUDPConnectionData(Uri uri, Action<string> callback, UnityEngine.Object context = null)
    {
        if (!NetworkManager.Singleton.gameObject.TryGetComponent(out UnityTransport unityTransport))
        {
            callback?.Invoke($"UDP not supported :(");
            yield break;
        }

        string socketAddress = null;

        if (uri.IsDefaultPort)
        {
            callback?.Invoke($"No port specified");
            yield break;
        }

        if (uri.Port < 1 || uri.Port > ushort.MaxValue)
        {
            callback?.Invoke($"Invalid port {uri.Port}");
            yield break;
        }

        if (!IPAddress.TryParse(uri.Host ?? "", out IPAddress address))
        {
            Debug.Log($"Resolving hostname \"{uri.Host}\" ...", context);
            Task<IPHostEntry> dnsTask = Dns.GetHostEntryAsync(uri.Host);

            yield return new WaitUntil(() => dnsTask.IsCompleted);

            if (!dnsTask.IsCompletedSuccessfully || dnsTask.Result == null)
            {
                Debug.Log($"[{nameof(NetcodeUtils)}]: Failed to resolve \"{uri.Host}\"", context);

                callback?.Invoke($"Failed to resolve \"{uri.Host}\"");
                yield break;
            }

            IPHostEntry dnsResult = dnsTask.Result;

            if (dnsResult.AddressList.Length == 0)
            {
                Debug.Log($"[{nameof(NetcodeUtils)}]: DNS entry \"{uri.Host}\" does not have any address", context);

                callback?.Invoke($"DNS entry \"{uri.Host}\" does not have any address");
                yield break;
            }

            Debug.Log($"Hostname (\"{uri.Host}\") result: {dnsResult.AddressList.ToReadableString()}", context);

            socketAddress = dnsResult.AddressList[0].ToString();
        }
        else
        {
            socketAddress = address.ToString();

            if (socketAddress != uri.Host)
            {
                callback?.Invoke($"Invalid IP Address \"{uri.Host}\"");
                yield break;
            }
        }

        NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
        Debug.Log($"[{nameof(NetcodeUtils)}]: {nameof(NetworkManager.Singleton.NetworkConfig.NetworkTransport)} set to {nameof(UnityTransport)}", context);

        unityTransport.SetConnectionData(socketAddress, (ushort)uri.Port, socketAddress);
        Debug.Log($"[{nameof(NetcodeUtils)}]: Connection data set to {unityTransport.ConnectionData.Address}:{unityTransport.ConnectionData.Port}", context);
        callback?.Invoke(null);
        yield break;
    }

    static IEnumerator SetWebSocketConnectionData(Uri uri, Action<string> callback, UnityEngine.Object context = null)
    {
        if (!NetworkManager.Singleton.gameObject.TryGetComponent(out WebSocketTransport webSocketTransport))
        {
            callback?.Invoke($"WebSocket not supported :(");
            yield break;
        }

        if (uri.IsDefaultPort)
        {
            callback?.Invoke($"No port specified");
            yield break;
        }

        if (uri.Port < 1 || uri.Port > ushort.MaxValue)
        {
            callback?.Invoke($"Invalid port {uri.Port}");
            yield break;
        }

        NetworkManager.Singleton.NetworkConfig.NetworkTransport = webSocketTransport;
        Debug.Log($"[{nameof(NetcodeUtils)}]: {nameof(NetworkManager.Singleton.NetworkConfig.NetworkTransport)} set to {nameof(WebSocketTransport)}", context);

        webSocketTransport.AllowForwardedRequest = false;
        webSocketTransport.CertificateBase64String = "";
        webSocketTransport.ConnectAddress = uri.Host;
        webSocketTransport.Port = (ushort)uri.Port;
        webSocketTransport.SecureConnection = (uri.Scheme == "wss");
        webSocketTransport.Path = uri.AbsolutePath;

        Debug.Log($"[{nameof(NetcodeUtils)}]: Connection data set to {(webSocketTransport.SecureConnection ? "wss" : "ws")}://{webSocketTransport.ConnectAddress}:{webSocketTransport.Port}{webSocketTransport.Path}", context);
        callback?.Invoke(null);
        yield break;
    }

    public static IEnumerator HostAsync(string input, Action<string> callback, UnityEngine.Object context = null)
    {
        string socketComputeError = null;
        yield return SetConnectionData(input, result => { socketComputeError = result; });

        if (socketComputeError != null)
        {
            callback?.Invoke(socketComputeError);
            yield break;
        }

        Debug.Log($"[{nameof(NetcodeUtils)}]: Start server on {NetworkConfig} ...", context);

        bool success = NetworkManager.Singleton.StartServer();

        if (success)
        {
            Debug.Log($"[{nameof(NetcodeUtils)}]: Server started on {NetworkConfig}", context);
        }
        else
        {
            callback?.Invoke($"Failed to start server on {NetworkConfig}");
            Debug.LogError($"[{nameof(NetcodeUtils)}]: Failed to start server on {NetworkConfig}", context);
        }
    }

    public static IEnumerator ConnectAsync(string input, Action<string> callback, UnityEngine.Object context = null)
    {
        string socketComputeError = null;
        yield return SetConnectionData(input, result => { socketComputeError = result; });

        if (socketComputeError != null)
        {
            callback?.Invoke(socketComputeError);
            yield break;
        }

        Debug.Log($"[{nameof(NetcodeUtils)}]: Start client on {NetworkConfig} ...", context);

        bool success = NetworkManager.Singleton.StartClient();

        if (success)
        {
            Debug.Log($"[{nameof(NetcodeUtils)}]: Client started on {NetworkConfig}", context);
        }
        else
        {
            callback?.Invoke($"Failed to start client on {NetworkConfig}");
            Debug.LogError($"[{nameof(NetcodeUtils)}]: Failed to start client on {NetworkConfig}", context);
        }
    }

}

/// <summary>
/// <see href="https://www.codeproject.com/Tips/5267157/How-to-Get-a-Collection-Element-Type-Using-Reflect"/>
/// </summary>
static partial class ReflectionUtility
{
    /// <summary>
    /// Indicates whether or not the specified type is a list.
    /// </summary>
    /// <param name="type">The type to query</param>
    /// <returns>True if the type is a list, otherwise false</returns>
    public static bool IsList(Type type)
    {
        if (null == type)
            throw new ArgumentNullException("type");

        if (typeof(IList).IsAssignableFrom(type))
            return true;
        foreach (var it in type.GetInterfaces())
            if (it.IsGenericType && typeof(IList<>) == it.GetGenericTypeDefinition())
                return true;
        return false;
    }
    /// <summary>
    /// Retrieves the collection element type from this type
    /// </summary>
    /// <param name="type">The type to query</param>
    /// <returns>The element type of the collection or null if the type was not a collection
    /// </returns>
    public static Type GetCollectionElementType(Type type)
    {
        if (null == type)
            throw new ArgumentNullException("type");

        // first try the generic way
        // this is easy, just query the IEnumerable<T> interface for its generic parameter
        var etype = typeof(IEnumerable<>);
        foreach (var bt in type.GetInterfaces())
            if (bt.IsGenericType && bt.GetGenericTypeDefinition() == etype)
                return bt.GetGenericArguments()[0];

        // now try the non-generic way

        // if it's a dictionary we always return DictionaryEntry
        if (typeof(IDictionary).IsAssignableFrom(type))
            return typeof(DictionaryEntry);

        // if it's a list we look for an Item property with an int index parameter
        // where the property type is anything but object
        if (typeof(IList).IsAssignableFrom(type))
        {
            foreach (var prop in type.GetProperties())
            {
                if ("Item" == prop.Name && typeof(object) != prop.PropertyType)
                {
                    var ipa = prop.GetIndexParameters();
                    if (1 == ipa.Length && typeof(int) == ipa[0].ParameterType)
                    {
                        return prop.PropertyType;
                    }
                }
            }
        }

        // if it's a collection, we look for an Add() method whose parameter is 
        // anything but object
        if (typeof(ICollection).IsAssignableFrom(type))
        {
            foreach (var meth in type.GetMethods())
            {
                if ("Add" == meth.Name)
                {
                    var pa = meth.GetParameters();
                    if (1 == pa.Length && typeof(object) != pa[0].ParameterType)
                        return pa[0].ParameterType;
                }
            }
        }
        if (typeof(IEnumerable).IsAssignableFrom(type))
            return typeof(object);
        return null;
    }

    public readonly struct Flags
    {
        public static readonly System.Reflection.BindingFlags AllInstance = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    }

    public static T[] GetMembers<T>(Type type, Type stopAt, Func<Type, IEnumerable<T>> memberSearcher)
        => GetMembers<T>(type, stopAt, memberSearcher, 0);
    static T[] GetMembers<T>(Type type, Type stopAt, Func<Type, IEnumerable<T>> memberSearcher, int depth)
    {
        if (depth > 5)
        { throw new Exception($"Inherit depth exceed"); }

        List<T> result = new();

        if (type == stopAt)
        {
            return result.ToArray();
        }

        result.AddRange(memberSearcher.Invoke(type));

        result.AddRange(GetMembers<T>(type.BaseType, stopAt, memberSearcher, depth + 1));

        return result.ToArray();
    }
}

public class SearcherCoroutine<T>
{
    int i;
    T[] gotList = null;
    bool isRunning;

    readonly Func<T[]> list;
    readonly Action<T> callback;

    public bool IsRunning => isRunning;

    public SearcherCoroutine(Func<T[]> list, Action<T> callback)
    {
        i = 0;
        this.list = list;
        this.callback = callback;
        isRunning = false;
    }

    public void Search(MonoBehaviour caller)
    {
        isRunning = true;
        caller.StartCoroutine(Search());
    }

    public IEnumerator Search()
    {
        i = 0;

        if (list == null)
        {
            isRunning = false;
            yield break;
        }

        if (callback == null)
        {
            isRunning = false;
            yield break;
        }

        gotList = list?.Invoke();

        if (gotList == null)
        {
            isRunning = false;
            yield break;
        }

        while (i < gotList.Length)
        {
            yield return new WaitForFixedUpdate();

            this.callback?.Invoke(gotList[i]);
            i++;
        }

        isRunning = false;
    }
}

namespace InputUtils
{
    internal delegate bool InputConditionEnabler();

    internal delegate void SimpleInputEvent<T>(T sender);

    internal class AdvancedInput : IComparable<AdvancedInput>
    {
        internal static float ScreenSize => Mathf.Sqrt(Mathf.Pow(Screen.width, 2) + Mathf.Pow(Screen.height, 2));

        protected readonly InputConditionEnabler ConditionEnabler;
        internal readonly int Priority;
        protected readonly Type OwnedBy;

        internal virtual bool Enabled => ConditionEnabler?.Invoke() ?? true;

        public AdvancedInput(int priority)
            : this(priority, null) { }

        public AdvancedInput(int priority, InputConditionEnabler conditionEnabler)
        {
            this.Priority = priority;
            this.ConditionEnabler = conditionEnabler;

            System.Diagnostics.StackTrace stack = new(false);
            for (int i = 0; i < stack.FrameCount; i++)
            {
                System.Diagnostics.StackFrame frame = stack.GetFrame(i);
                System.Reflection.MethodBase method = frame.GetMethod();
                if (method.IsConstructor)
                { continue; }
                Type declaringType = method.DeclaringType;
                OwnedBy = declaringType;
                break;
            }
        }

        public int CompareTo(AdvancedInput other)
            => Comparer.DefaultInvariant.Compare(other.Priority, this.Priority);
    }

    [Serializable]
    internal class AdvancedMouse : AdvancedInput
    {
        internal delegate void DragEvent(Vector2 start, Vector2 current);
        internal delegate void DraggedEvent(Vector2 start, Vector2 end);

        internal event DragEvent OnDrag;
        internal event DraggedEvent OnDragged;
        internal event SimpleInputEvent<AdvancedMouse> OnClick;
        internal event SimpleInputEvent<AdvancedMouse> OnDown;

        internal readonly int ButtonID;

        internal static Vector2 Position => Input.mousePosition;

        [SerializeField, ReadOnly] bool ClickedOnUI;

        internal Vector2 DragStart { get; private set; }
        internal bool IsActive { get; private set; }
        internal bool IsDragging => Drag && !ClickedOnUI;
        [SerializeField, ReadOnly] bool Drag;
        internal const float DragThreshold = 25f;
        internal static readonly float DragThresholdSqr = Mathf.Sqrt(DragThreshold);

        float PressedAt;
        readonly float UpTimeout;

        [SerializeField, ReadOnly] bool DownInvoked;
        [SerializeField, ReadOnly] bool UpInvoked;

        internal float HoldTime => Time.unscaledTime - PressedAt;

        internal AdvancedMouse(int buttonId, int priority)
            : this(buttonId, priority, null, 0f) { }

        internal AdvancedMouse(int buttonId, int priority, InputConditionEnabler conditionEnabler)
            : this(buttonId, priority, conditionEnabler, 0f) { }

        internal AdvancedMouse(int buttonId, int priority, float upTimeout)
            : this(buttonId, priority, null, upTimeout) { }

        internal AdvancedMouse(int buttonId, int priority, InputConditionEnabler conditionEnabler, float upTimeout)
            : base(priority, conditionEnabler)
        {
            this.ButtonID = buttonId;
            this.UpTimeout = upTimeout;
            MouseManager.RegisterInput(this);
        }

        internal void Update()
        {
            if (!Enabled)
            {
                Reset();
                return;
            }

            this.IsActive = true;

            if (Input.GetMouseButtonDown(ButtonID))
            { Down(); }
            else if (Input.GetMouseButtonUp(ButtonID))
            { Up(); }
            else if (Input.GetMouseButton(ButtonID))
            { Hold(); }
        }

        internal void Reset()
        {
            this.DragStart = Vector2.zero;
            this.ClickedOnUI = false;
            this.Drag = false;
            this.UpInvoked = true;
            this.DownInvoked = false;
            this.PressedAt = 0f;
            this.IsActive = false;
        }

        void Down()
        {
            DownInvoked = true;
            DragStart = Position;
            Drag = false;
            PressedAt = Time.unscaledTime;
            UpInvoked = false;
            ClickedOnUI = MouseManager.IsOverUI(Position);

            if (!ClickedOnUI)
            {
                try
                { OnDown?.Invoke(this); }
                catch (Exception exception)
                { Debug.LogException(exception); }
            }
        }

        void Hold()
        {
            if (!DownInvoked)
            { return; }

            if (!Drag && (Position - DragStart).sqrMagnitude > DragThresholdSqr)
            { Drag = true; }

            if (Drag)
            {
                if (!ClickedOnUI)
                {
                    try
                    { OnDrag?.Invoke(DragStart, Position); }
                    catch (Exception exception)
                    { Debug.LogException(exception); }
                }
            }

            if (UpTimeout != 0f && UpTimeout < HoldTime)
            { Up(); }
        }

        void Up()
        {
            if (!DownInvoked)
            { return; }

            if (!UpInvoked)
            {
                if (Drag)
                {
                    if (!ClickedOnUI)
                    {
                        try
                        { OnDragged?.Invoke(DragStart, Position); }
                        catch (Exception exception)
                        { Debug.LogException(exception); }
                    }
                }
                else
                {
                    if (!ClickedOnUI)
                    {
                        try
                        { OnClick?.Invoke(this); }
                        catch (Exception exception)
                        { Debug.LogException(exception); }
                    }
                }
            }

            DragStart = Vector2.zero;
            Drag = false;
            UpInvoked = true;
            PressedAt = 0f;
            IsActive = false;
        }

        internal void DebugDraw()
        {
            Vector2 outerPointV = Vector2.up * 10;
            Vector2 outerPointH = Vector2.left * 10;

            Vector2 position = AdvancedMouse.Position;
            position = GUIUtils.TransformPoint(position);

            Color color = Color.white;

            if (ClickedOnUI)
            { color = Color.red; }

            position += new Vector2(1, -1);

            GLUtils.DrawLine(position - outerPointH, position + outerPointH, 1.5f, color);
            GLUtils.DrawLine(position - outerPointV, position + outerPointV, 1.5f, color);

            GUI.Label(new Rect(position - new Vector2(0, 20), new Vector2(200, 20)), $"{this.HoldTime:####.00} ms");
            GUI.Label(new Rect(position - new Vector2(0, 40), new Vector2(200, 20)), this.OwnedBy.Name);
        }
    }

    [Serializable]
    internal class AdvancedTouch : AdvancedInput
    {
        internal event SimpleInputEvent<AdvancedTouch> OnClick;
        internal event SimpleInputEvent<AdvancedTouch> OnDown;
        internal event SimpleInputEvent<AdvancedTouch> OnMove;
        internal event SimpleInputEvent<AdvancedTouch> OnUp;
        internal event SimpleInputEvent<AdvancedTouch> OnCancelled;

        [SerializeField, ReadOnly] Touch Touch;

        internal TouchPhase Phase => Touch.phase;

        internal Vector2 Position => Touch.position;

        internal Vector2 PositionDelta => Touch.deltaPosition;

        [SerializeField, ReadOnly] internal int FingerID;
        internal bool IsActive => FingerID != -1;
        internal bool IsActiveAndCaptured => IsCaptured && IsActive;

        [SerializeField, ReadOnly] bool ClickedOnUI;

        float PressedAt;
        readonly float UpTimeout;
        readonly RectInt ValidScreenRect;
        bool DownInvoked;
        bool UpInvoked;

        internal bool IsCaptured;

        internal bool IsHolding { get; private set; }

        internal AdvancedTouch(int priority) : base(priority)
        {
            MouseManager.RegisterInput(this);
            ValidScreenRect = new RectInt(0, 0, 0, 0);
        }

        internal AdvancedTouch(int priority, InputConditionEnabler conditionEnabler) : base(priority, conditionEnabler)
        {
            MouseManager.RegisterInput(this);
            ValidScreenRect = new RectInt(0, 0, 0, 0);
        }

        internal AdvancedTouch(int priority, RectInt validScreenRect) : base(priority)
        {
            MouseManager.RegisterInput(this);
            ValidScreenRect = validScreenRect;
        }

        internal AdvancedTouch(int priority, InputConditionEnabler conditionEnabler, RectInt validScreenRect) : base(priority, conditionEnabler)
        {
            MouseManager.RegisterInput(this);
            ValidScreenRect = validScreenRect;
        }

        internal float HoldTime => Time.unscaledTime - PressedAt;

        internal void Update()
        {
            // if (!Input.touchSupported) return;
            if (!Enabled)
            {
                FingerID = -1;
                Touch = default;
                IsCaptured = false;
                IsHolding = false;
                return;
            }

            Touch[] touches = Input.touches;

            if (FingerID != -1)
            {
                if (!MouseManager.IsTouchCaptured(this.FingerID, this))
                {
                    for (int i = 0; i < touches.Length; i++)
                    {
                        if (touches[i].fingerId == FingerID)
                        {
                            Touch = touches[i];
                            UpdateInternal();
                            return;
                        }
                    }
                }

                FingerID = -1;
                Touch = default;
                IsCaptured = false;
                IsHolding = false;
            }

            for (int i = 0; i < touches.Length; i++)
            {
                if (MouseManager.IsTouchCaptured(touches[i].fingerId, this))
                { continue; }

                Touch = touches[i];

                if (ValidScreenRect.size == Vector2Int.zero || ValidScreenRect.Contains(new Vector2Int(Mathf.RoundToInt(Position.x), Mathf.RoundToInt(Position.y))))
                {
                    FingerID = Touch.fingerId;
                    UpdateInternal();
                    return;
                }
            }

            FingerID = -1;
            Touch = default;
            IsCaptured = false;
            IsHolding = false;
        }

        void UpdateInternal()
        {
            switch (Touch.phase)
            {
                case TouchPhase.Began:
                    OnBegan();
                    break;
                case TouchPhase.Moved:
                    OnMoved();
                    break;
                case TouchPhase.Stationary:
                    OnStationary();
                    break;
                case TouchPhase.Ended:
                    OnEnded();
                    break;
                case TouchPhase.Canceled:
                    OnCanceled();
                    break;
                default:
                    break;
            }
        }

        void OnBegan()
        {
            DownInvoked = true;
            PressedAt = Time.unscaledTime;
            UpInvoked = false;
            ClickedOnUI = MouseManager.IsOverUI(Position);
            IsHolding = true;

            if (ClickedOnUI) return;

            OnDown?.Invoke(this);
        }

        void OnMoved()
        {
            IsHolding = false;

            if (ClickedOnUI) return;

            OnMove?.Invoke(this);
        }

        void OnStationary()
        {
            if (ClickedOnUI) return;

            if (UpTimeout != 0f && UpTimeout < HoldTime)
            {
                OnEnded();
                return;
            }
        }

        void OnEnded()
        {
            if (!DownInvoked) return;

            if (!UpInvoked)
            {
                if (!ClickedOnUI)
                {
                    OnClick?.Invoke(this);
                    OnUp?.Invoke(this);
                }
            }

            UpInvoked = true;
            PressedAt = 0f;
            FingerID = -1;
            IsCaptured = false;
            IsHolding = false;
        }

        void OnCanceled()
        {
            if (!ClickedOnUI)
            { OnCancelled?.Invoke(this); }

            UpInvoked = true;
            PressedAt = 0f;
            FingerID = -1;
            IsCaptured = false;
            IsHolding = false;
        }

        internal void Reset()
        {
            ClickedOnUI = false;
            UpInvoked = true;
            DownInvoked = false;
            PressedAt = 0f;
            FingerID = -1;
            IsCaptured = false;
            IsHolding = false;
        }

        internal void DebugDraw()
        {
            if (!IsActive) return;

            Vector2 position = this.Position;

            Color color;

            if (ClickedOnUI)
            {
                color = Color.red;
            }
            else
            {
                color = Phase switch
                {
                    TouchPhase.Began => Color.cyan,
                    TouchPhase.Moved => Color.blue,
                    TouchPhase.Stationary => Color.white,
                    TouchPhase.Ended => Color.yellow,
                    TouchPhase.Canceled => Color.magenta,
                    _ => Color.white,
                };
            }

            Vector2 guiPosition = GUIUtils.TransformPoint(position);

            float radius = 20;

            GLUtils.DrawCircle(guiPosition / 4.3f, radius, 2, color, 16);

            GUIStyle style = new(IMGUIManager.Instance.Skin.label)
            {
                fontSize = 48,
                fontStyle = FontStyle.Bold,
            };

            Vector2 textOffset = new(radius, -radius);
            textOffset *= 4.3f / 1.4f;

            int line = 1;

            void Label(string text)
            {
                GUI.enabled = false;
                GUI.Label(new Rect(guiPosition + textOffset + new Vector2(0, style.fontSize * -line), new Vector2(Screen.width, style.fontSize)), text, style);
                GUI.enabled = true;
                line++;
            }

            Label($"{this.HoldTime:####.00} ms");
            Label(this.OwnedBy.Name);

            if (IsCaptured)
            { Label("Captured"); }
            else
            { Label("Not Captured"); }

            if (IsHolding)
            { Label("Holding"); }
            else
            { Label("Moving"); }
        }
    }

    internal class TouchZoom : AdvancedInput
    {
        internal delegate void ZoomEvent(TouchZoom sender, float delta);

        internal event ZoomEvent OnZoom;
        internal event SimpleInputEvent<AdvancedTouch> OnMove;

        readonly AdvancedTouch Touch1;
        readonly AdvancedTouch Touch2;

        (Vector2 Start, Vector2 Current) Touch1Position;
        (Vector2 Start, Vector2 Current) Touch2Position;

        float StartDistance;
        float LastDistanceDiff;
        float Distance => Vector2.Distance(Touch1Position.Current, Touch2Position.Current);

        internal bool BothTouchActive => Touch1.IsActive && Touch2.IsActive;
        internal bool BothTouchActiveAndCaptured => Touch1.IsActiveAndCaptured && Touch2.IsActiveAndCaptured;

        internal Vector2 PositionDelta
        {
            get
            {
                if (BothTouchActive) return Vector2.zero;
                if (Touch1.IsActive) return Touch1.PositionDelta;
                if (Touch2.IsActive) return Touch2.PositionDelta;
                return Vector2.zero;
            }
        }

        internal TouchZoom(int priority, InputConditionEnabler condition) : base(priority, condition)
        {
            Touch1 = new AdvancedTouch(priority, condition);
            Touch2 = new AdvancedTouch(priority, condition);

            Touch1.OnDown += OnDown1;
            Touch2.OnDown += OnDown2;

            Touch1.OnMove += OnMove1;
            Touch2.OnMove += OnMove2;
        }

        internal TouchZoom(int priority) : this(priority, null)
        { }

        void OnDown1(AdvancedTouch sender)
        {
            Touch1Position = (sender.Position, sender.Position);
            StartZooming();
        }

        void OnDown2(AdvancedTouch sender)
        {
            Touch2Position = (sender.Position, sender.Position);
            StartZooming();
        }

        void StartZooming()
        {
            StartDistance = Vector2.Distance(Touch1Position.Start, Touch2Position.Start);
            LastDistanceDiff = Distance - StartDistance;
        }

        void OnMove1(AdvancedTouch sender)
        {
            sender.IsCaptured = true;
            if (!Touch2.IsActive)
            {
                OnMove?.Invoke(sender);

                Touch1Position = (sender.Position, sender.Position);
                StartZooming();
                return;
            }

            Touch1Position.Current = sender.Position;
            UpdateInternal();
        }

        void OnMove2(AdvancedTouch sender)
        {
            sender.IsCaptured = true;
            if (!Touch1.IsActive)
            {
                OnMove?.Invoke(sender);

                Touch2Position = (sender.Position, sender.Position);
                StartZooming();
                return;
            }

            Touch2Position.Current = sender.Position;
            UpdateInternal();
        }

        void UpdateInternal()
        {
            if (!BothTouchActiveAndCaptured) return;

            if (StartDistance == 0)
            {
                StartDistance = Distance;
                return;
            }

            float distanceDiff = Distance - StartDistance;
            float distanceDelta = LastDistanceDiff - distanceDiff;

            OnZoom?.Invoke(this, distanceDelta / ScreenSize);

            LastDistanceDiff = distanceDiff;
        }
    }

    internal class PriorityKey : AdvancedInput
    {
        internal delegate void KeyEvent();

        internal event KeyEvent OnDown;
        internal event KeyEvent OnHold;
        internal event KeyEvent OnUp;

        internal readonly KeyCode Key;

        internal PriorityKey(KeyCode key, int priority)
            : this(key, priority, null) { }

        internal PriorityKey(KeyCode key, int priority, InputConditionEnabler conditionEnabler)
            : base(priority, conditionEnabler)
        {
            this.Key = key;
            KeyboardManager.Register(this);
        }

        internal bool Update()
        {
            if (!Enabled)
            { return false; }

            bool consumed = false;

            if (OnDown != null && Input.GetKeyDown(Key))
            {
                OnDown.Invoke();
                consumed = true;
            }

            if (OnHold != null && Input.GetKey(Key))
            {
                OnHold.Invoke();
                consumed = true;
            }

            if (OnUp != null && Input.GetKeyUp(Key))
            {
                OnUp.Invoke();
                consumed = true;
            }

            return consumed;
        }
    }
}

internal readonly struct MouseButton
{
    internal const int Left = 0;
    internal const int Right = 1;
    internal const int Middle = 2;
}

internal readonly struct EditorUtils
{
    internal static string ProjectPath => System.IO.Path.GetDirectoryName(Application.dataPath);
    internal static string ResourcesPath => System.IO.Path.Combine(Application.dataPath, "Resources");
}

internal interface ICopiable<T> : ICopiable
{
    public void CopyTo(T destination);
}

internal interface ICopiable
{
    public void CopyTo(object destination);
}

internal static class UnityCopiables
{
    internal static void CopyTo(Rigidbody source, Rigidbody destination)
    {
        destination.angularDrag = source.angularDrag;
        destination.angularVelocity = source.angularVelocity;
        destination.centerOfMass = source.centerOfMass;
        destination.collisionDetectionMode = source.collisionDetectionMode;
        destination.constraints = source.constraints;
        destination.drag = source.drag;
        destination.freezeRotation = source.freezeRotation;
        destination.inertiaTensor = source.inertiaTensor;
        destination.inertiaTensorRotation = source.inertiaTensorRotation;
        destination.interpolation = source.interpolation;
        destination.isKinematic = source.isKinematic;
        destination.mass = source.mass;
        destination.maxAngularVelocity = source.maxAngularVelocity;
        destination.maxDepenetrationVelocity = source.maxDepenetrationVelocity;
        destination.sleepThreshold = source.sleepThreshold;
        destination.useGravity = source.useGravity;
    }
}

internal static class CopiableExtensions
{
    internal static bool CopyTo<T>(this ICopiable<T> source, object destination)
    {
        if (destination is not T _destination)
        {
            Debug.LogError($"[{nameof(CopiableExtensions)}]: Invalid destination type");
            return false;
        }
        source.CopyTo(_destination);
        return true;
    }
}

/// <summary>
/// This can be converted into <see cref="Component"/>
/// </summary>
public interface IComponent
{ }

public static class IObjectExtensions
{
    public static bool HasComponent<T>(this IComponent self)
        => (self as Component).HasComponent<T>();

    public static Component Object(this IComponent self)
        => self as Component;

    public static GameObject GetGameObject(this IComponent self)
        => (self as Component).gameObject;

    public static ulong? GetNetworkID(this IComponent self)
    {
        if (!self.TryGetComponentInChildren(out NetworkObject networkObject))
        {
            Debug.LogError($"Object {self.GetGameObject()} does not have a {nameof(NetworkObject)} component", self.GetGameObject());
            return null;
        }
        return networkObject.NetworkObjectId;
    }

    public static ulong GetNetworkIDForce(this IComponent self)
    {
        if (!self.TryGetComponentInChildren(out NetworkObject networkObject))
        { throw new MissingComponentException($"Object {self.GetGameObject()} does not have a {nameof(NetworkObject)} component"); }

        return networkObject.NetworkObjectId;
    }

    public static bool TryGetNetworkID(this IComponent self, out ulong networkID)
    {
        if (!self.TryGetComponentInChildren(out NetworkObject networkObject))
        {
            networkID = default;
            return false;
        }
        networkID = networkObject.NetworkObjectId;
        return true;
    }

    public static T GetComponent<T>(this IComponent self)
    {
        if (!(self as Component).TryGetComponent(out T component))
        { return default; }
        return component;
    }

    public static bool TryGetComponent<T>(this IComponent self, out T component)
        => (self as Component).TryGetComponent(out component);

    public static T GetComponentInChildren<T>(this IComponent self)
    {
        if (!(self as Component).TryGetComponentInChildren(out T component))
        { return default; }
        return component;
    }

    public static bool TryGetComponentInChildren<T>(this IComponent self, out T component)
        => (self as Component).TryGetComponentInChildren(out component);
}

internal static class ListUtils
{
    internal static string ToReadableString<T>(this NetworkList<T> self) where T : unmanaged, IEquatable<T>
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        for (int i = 0; i < self.Count; i++)
        {
            if (i > 0)
            { builder.Append(", "); }
            T element = self[i];
            builder.Append(element.ToString());
        }

        builder.Append(" }");

        return builder.ToString();
    }
    internal static string ToReadableString<T>(this T[] self)
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        for (int i = 0; i < self.Length; i++)
        {
            if (i > 0)
            { builder.Append(", "); }
            T element = self[i];
            builder.Append(element.ToString());
        }

        builder.Append(" }");

        return builder.ToString();
    }
    internal static string ToReadableString<T>(this IReadOnlyList<T> self)
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        for (int i = 0; i < self.Count; i++)
        {
            if (i > 0)
            { builder.Append(", "); }
            T element = self[i];
            builder.Append(element.ToString());
        }

        builder.Append(" }");

        return builder.ToString();
    }
    internal static string ToReadableString<T>(this IEnumerable<T> self)
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        bool notFirst = false;
        foreach (T element in self)
        {
            if (notFirst)
            { builder.Append(", "); }

            builder.Append(element.ToString());

            notFirst = true;
        }

        builder.Append(" }");

        return builder.ToString();
    }
}

internal static class Intervals
{
    public delegate bool Condition();

    static bool AlwaysTrue() => true;

    public static void Timeout(this MonoBehaviour context, Action action, float timeout, Condition condition = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        context.StartCoroutine(Intervals.TimeoutCoroutine(action, timeout, condition ?? AlwaysTrue));
    }

    static IEnumerator TimeoutCoroutine(Action action, float timeout, Condition condition)
    {
        yield return new WaitForSeconds(timeout);
        while (!condition.Invoke())
        { yield return new WaitForSeconds(0.1f); }
        action.Invoke();
    }

    public static void Timeout<T0>(this MonoBehaviour context, Action<T0> action, T0 parameter0, float timeout, Condition condition = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        context.StartCoroutine(Intervals.TimeoutCoroutine(action, parameter0, timeout, condition ?? AlwaysTrue));
    }

    static IEnumerator TimeoutCoroutine<T0>(Action<T0> action, T0 parameter0, float timeout, Condition condition)
    {
        yield return new WaitForSeconds(timeout);
        while (!condition.Invoke())
        { yield return new WaitForSeconds(0.1f); }
        action.Invoke(parameter0);
    }

    public static void Timeout<T0, T1>(this MonoBehaviour context, Action<T0, T1> action, T0 parameter0, T1 parameter1, float timeout, Condition condition = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        context.StartCoroutine(Intervals.TimeoutCoroutine(action, parameter0, parameter1, timeout, condition ?? AlwaysTrue));
    }

    static IEnumerator TimeoutCoroutine<T0, T1>(Action<T0, T1> action, T0 parameter0, T1 parameter1, float timeout, Condition condition)
    {
        yield return new WaitForSeconds(timeout);
        while (!condition.Invoke())
        { yield return new WaitForSeconds(0.1f); }
        action.Invoke(parameter0, parameter1);
    }

    public static void Timeout<T0, T1, T2>(this MonoBehaviour context, Action<T0, T1, T2> action, T0 parameter0, T1 parameter1, T2 parameter2, float timeout, Condition condition = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        context.StartCoroutine(Intervals.TimeoutCoroutine(action, parameter0, parameter1, parameter2, timeout, condition ?? AlwaysTrue));
    }

    static IEnumerator TimeoutCoroutine<T0, T1, T2>(Action<T0, T1, T2> action, T0 parameter0, T1 parameter1, T2 parameter2, float timeout, Condition condition)
    {
        yield return new WaitForSeconds(timeout);
        while (!condition.Invoke())
        { yield return new WaitForSeconds(0.1f); }
        action.Invoke(parameter0, parameter1, parameter2);
    }

    public static UnityIntervalDynamicCondition Interval(this MonoBehaviour context, Action action, float interval, Condition condition = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        UnityIntervalDynamicCondition unityInterval = new(context, action, interval, condition ?? AlwaysTrue);
        unityInterval.Start();
        return unityInterval;
    }

    public static UnityIntervalStaticCondition Interval(this MonoBehaviour context, Action action, float interval, bool enabled = true)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        UnityIntervalStaticCondition unityInterval = new(context, action, interval, enabled);
        unityInterval.Start();
        return unityInterval;
    }

    public abstract class UnityBaseInterval
    {
        protected Coroutine Coroutine;

        protected readonly MonoBehaviour Context;
        protected readonly Action Action;

        public float Interval;

        public UnityBaseInterval(MonoBehaviour context, Action action, float interval)
        {
            Context = context;
            Action = action;
            Interval = interval;
        }

        public void Stop()
        {
            if (Coroutine == null) return;
            Context.StopCoroutine(Coroutine);
            Coroutine = null;
        }

        public void Start()
        {
            if (Coroutine != null) return;
            Coroutine = Context.StartCoroutine(IntervalCoroutine());
        }

        protected abstract IEnumerator IntervalCoroutine();
    }

    public class UnityIntervalDynamicCondition : UnityBaseInterval
    {
        readonly Condition Condition;

        public UnityIntervalDynamicCondition(MonoBehaviour context, Action action, float interval, Condition condition)
            : base(context, action, interval)
        {
            Condition = condition;
        }

        protected override IEnumerator IntervalCoroutine()
        {
            yield return new WaitForSeconds(Interval);
            while (true)
            {
                if (Condition.Invoke()) Action.Invoke();
                yield return new WaitForSeconds(Interval);
            }
        }
    }

    public class UnityIntervalStaticCondition : UnityBaseInterval
    {
        public bool Enabled;

        public UnityIntervalStaticCondition(MonoBehaviour context, Action action, float interval, bool enabled)
            : base(context, action, interval)
        {
            Enabled = enabled;
        }

        protected override IEnumerator IntervalCoroutine()
        {
            yield return new WaitForSeconds(Interval);
            while (true)
            {
                if (Enabled) Action.Invoke();
                yield return new WaitForSeconds(Interval);
            }
        }
    }
}

internal struct MetricUtils
{
    const float Multiplier = 0.001f;

    public static float GetMeters(float value) => value * Multiplier;
    public static Vector2 GetMeters(Vector2 value) => value * Multiplier;
    public static Vector3 GetMeters(Vector3 value) => value * Multiplier;
}

internal class WindowsAPI
{
#if PLATFORM_STANDALONE_WIN || UNITY_EDITOR_WIN
    public static readonly bool IsSupported = true;
#else
    public static readonly bool IsSupported = false;
#endif

    public enum Cursor
    {
        StandardArrowAndSmallHourglass = 32650,
        StandardArrow = 32512,
        Crosshair = 32515,
        Hand = 32649,
        ArrowAndQuestionMark = 32651,
        IBeam = 32513,
        [Obsolete("Obsolete for applications marked version 4.0 or later.")]
        Icon = 32641,
        SlashedCircle = 32648,
        [Obsolete(" Obsolete for applications marked version 4.0 or later. Use FourPointedArrowPointingNorthSouthEastAndWest")]
        Size = 32640,
        FourPointedArrowPointingNorthSouthEastAndWest = 32646,
        DoublePointedArrowPointingNortheastAndSouthwest = 32643,
        DoublePointedArrowPointingNorthAndSouth = 32645,
        DoublePointedArrowPointingNorthwestAndSoutheast = 32642,
        DoublePointedArrowPointingWestAndEast = 32644,
        VerticalArrow = 32516,
        Hourglass = 32514
    }

    public static void SetCursor(Cursor cursor)
    {
#if PLATFORM_STANDALONE_WIN || UNITY_EDITOR_WIN
        SetCursor(LoadCursor(IntPtr.Zero, (int)cursor));
#else
        throw new NotSupportedException();
#endif
    }

#if PLATFORM_STANDALONE_WIN || UNITY_EDITOR_WIN
    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetCursor")]
    public static extern IntPtr SetCursor(IntPtr hCursor);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "LoadCursor")]
    public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);
#endif
}
