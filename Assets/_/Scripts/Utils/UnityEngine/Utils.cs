﻿using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

public struct CoolColors
{
    public static Color White => new(1f, 1f, 1f);
    public static Color Black => new(0f, 0f, 0f);

    public static Color Red => new(0.921568632f, 0.137254909f, 0.08235294f);
    public static Color BrightRed => new(0.8588235f, 0.3607843f, 0.282353f);
    public static Color DarkRed => new(0.4313726f, 0.05490196f, 0.1176471f);

    public static Color Green => new(0.06666667f, 0.7411765f, 0.235294119f);
    public static Color BrightGreen => new(0.4941176f, 0.8392157f, 0.3176471f);
    public static Color DarkGreen => new(0.09411765f, 0.3803922f, 0.2f);

    public static Color Blue => new(0.117647059f, 0.254901975f, 0.921568632f);
    public static Color BrightBlue => new(0.1098039f, 0.4352941f, 1f);
    public static Color DarkBlue => new(0.05490196f, 0.0627451f, 0.549019635f);

    public static Color Yellow => new(0.8862745f, 0.9294118f, 0.09411765f);
    public static Color Magenta => new(0.6980392f, 0.01960784f, 0.7098039f);
    public static Color Cyan => new(0.1647059f, 0.8313726f, 0.9215686f);

    public static Color Orange => new(0.9215686f, 0.5450981f, 0.01960784f);
    public static Color Pink => new(0.9490196f, 0.3647059f, 0.9607843f);
    public static Color Purple => new(0.5568628f, 0.06666667f, 0.9607843f);
}

public struct Triangle
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

        float edge1Ext = Math.Clamp(Vector3.Dot(edge1Norm, ptLineA), 0f, edge1Len);
        float edge2Ext = Math.Clamp(Vector3.Dot(edge2Norm, ptLineA), 0f, edge2Len);
        float edge3Ext = Math.Clamp(Vector3.Dot(edge3Norm, ptLineB), 0f, edge3Len);

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

public readonly struct RectUtils
{
    public static Rect FromCenter(Vector2 center, Vector2 size)
        => new(center - (size * .5f), size);

    public static Rect FromCorners(Vector2 topLeft, Vector2 bottomRight)
        => new(topLeft, bottomRight - topLeft);
}

public static class GLUtils
{
    static Material? _solidMaterial;
    public static Material SolidMaterial
    {
        get
        {
            if (_solidMaterial == null)
            { _solidMaterial = new Material(Shader.Find("Hidden/Internal-Colored")); }
            return _solidMaterial;
        }
    }

    const int CircleSegmentCount = 32;

    public static void DrawLine(Vector2 a, Vector2 b, float thickness, Color color)
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
    public static void DrawLine(Vector2 a, Vector2 b, Color color)
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

    public static void DrawLine(Utilities.Line line, float thickness, Color color)
        => DrawLine(line.PointA, line.PointB, thickness, color);
    public static void DrawLine(Utilities.Line line, Color color)
        => DrawLine(line.PointA, line.PointB, color);

    public static void DrawCircle(Vector2 center, float radius, float thickness, Color color, int segmentCount = CircleSegmentCount)
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
                float rad = 2 * Maths.PI * ((float)i / (float)segmentCount);
                Vector2 direction = new(Maths.Cos(rad), Maths.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * (radius)));
            }

            {
                float rad = 2 * Maths.PI * ((float)(i + 1) / (float)segmentCount);
                Vector2 direction = new(Maths.Cos(rad), Maths.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * (radius)));
            }
        }
        GL.End();
    }
    public static void DrawCircle(Vector2 center, float radius, float thickness, Color color, float fillAmount, int segmentCount = CircleSegmentCount)
    {
        if (fillAmount >= .99f)
        {
            DrawCircle(center, radius, thickness, color, segmentCount);
            return;
        }

        if (thickness <= 1f)
        {
            DrawCircle(center, radius, color, segmentCount);
            return;
        }

        int segments = Maths.FloorToInt(fillAmount * segmentCount);
        float step = 1f / (float)segmentCount;

        GL.Begin(GL.TRIANGLE_STRIP);
        GL.Color(color);

        for (int i = 0; i < segments; i++)
        {
            {
                float rad = 2 * Maths.PI * ((float)i / (float)segmentCount);
                Vector2 direction = new(Maths.Cos(rad), Maths.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * (radius)));
            }

            {
                float next = 1 + Maths.Clamp((fillAmount - ((float)(i + 1) / (float)segmentCount)) / step, 0f, 1f);

                float rad = 2 * Maths.PI * ((float)(i + next) / (float)segmentCount);
                Vector2 direction = new(Maths.Cos(rad), Maths.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * (radius)));
            }
        }
        GL.End();
    }

    public static void DrawCircle(Vector2 center, float radius, Color color, int segmentCount = CircleSegmentCount)
    {
        GL.Begin(GL.LINE_STRIP);
        GL.Color(color);

        for (int i = 0; i < segmentCount; i++)
        {
            float rad = 2 * Maths.PI * ((float)i / (float)segmentCount);
            Vector2 direction = new(Maths.Cos(rad), Maths.Sin(rad));

            GL.Vertex(center + (direction * radius));
        }
        GL.End();
    }
    public static void DrawCircle(Vector2 center, float radius, Color color, float fillAmount, int segmentCount = CircleSegmentCount)
    {
        if (fillAmount >= .99f)
        {
            DrawCircle(center, radius, color, segmentCount);
            return;
        }

        int segments = Maths.FloorToInt(fillAmount * segmentCount);
        float step = 1f / (float)segmentCount;

        GL.Begin(GL.LINE_STRIP);
        GL.Color(color);

        for (int i = 0; i < segments; i++)
        {
            {
                float rad = 2 * Maths.PI * ((float)i / (float)segmentCount);
                Vector2 direction = new(Maths.Cos(rad), Maths.Sin(rad));

                GL.Vertex(center + (direction * radius));
            }

            {
                float next = 1 + Maths.Clamp((fillAmount - ((float)(i + 1) / (float)segmentCount)) / step, 0f, 1f);

                float rad = 2 * Maths.PI * ((float)(i + next) / (float)segmentCount);
                Vector2 direction = new(Maths.Cos(rad), Maths.Sin(rad));

                GL.Vertex(center + (direction * radius));
            }
        }
        GL.End();
    }
}

public readonly struct GUIUtils
{
    public static Vector2 TransformPoint(Vector2 screenPosition)
        => new(screenPosition.x, Screen.height - screenPosition.y);

    public static Texture2D GenerateCircleFilled(Vector2Int size)
    {
        Texture2D result = new(size.x, size.y);
        Vector2 center = Vector2.one * .5f;
        for (int x = 0; x < result.width; x++)
        {
            for (int y = 0; y < result.height; y++)
            {
                Vector2 p = new((float)x / (float)result.width, (float)y / (float)result.height);
                float d = Maths.Distance(p, center);
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

    public static Texture2D GenerateCircle(Vector2Int size, float thickness = .25f)
    {
        thickness = Math.Clamp(thickness, 0f, .5f);
        Texture2D result = new(size.x, size.y);
        Vector2 center = Vector2.one * .5f;
        for (int x = 0; x < result.width; x++)
        {
            for (int y = 0; y < result.height; y++)
            {
                Vector2 p = new((float)x / (float)result.width, (float)y / (float)result.height);
                float d = Maths.Distance(p, center);
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

    public static GuiSkinUsage Skin(GUISkin skin) => new(skin);
    public static GuiEnabled Enabled(bool enabled) => new(enabled);

    public static GuiEnabled Enabled() => new(true);
    public static GuiEnabled Disabled() => new(false);

    public static GuiContentColor ContentColor(Color color) => new(color);

    public static bool IsGUIFocused
    {
        get
        {
            if (GUIUtility.hotControl != 0) return true;

            UnityEngine.UIElements.UIDocument[] uiDocuments = GameObject.FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            for (int i = 0; i < uiDocuments.Length; i++)
            {
                if (uiDocuments[i] == null) continue;
                if (!uiDocuments[i].gameObject.activeSelf) continue;
                if (!uiDocuments[i].isActiveAndEnabled) continue;
                if (uiDocuments[i].rootVisualElement == null) continue;

                if (uiDocuments[i].rootVisualElement.focusController.focusedElement == null) continue;

                return true;
            }

            return false;
        }
    }
}

public readonly struct GuiSkinUsage : IDisposable
{
    readonly GUISkin savedSkin;

    public GuiSkinUsage(GUISkin skin)
    {
        savedSkin = GUI.skin;
        GUI.skin = skin;
    }

    public void Dispose()
    {
        GUI.skin = savedSkin;
    }
}

public readonly struct GuiEnabled : IDisposable
{
    readonly bool savedEnabled;

    public GuiEnabled(bool enabled)
    {
        savedEnabled = GUI.enabled;
        GUI.enabled = enabled;
    }

    public void Dispose()
    {
        GUI.enabled = savedEnabled;
    }
}

public readonly struct GuiContentColor : IDisposable
{
    readonly Color savedContentColor;

    public GuiContentColor(Color contentColor)
    {
        savedContentColor = GUI.contentColor;
        GUI.contentColor = contentColor;
    }

    public void Dispose()
    {
        GUI.contentColor = savedContentColor;
    }
}

namespace Utilities
{
    public static partial class UnityUtils
    {
        public static bool GetScreenCorners(Vector3[] points, out (Vector2 TopLeft, Vector2 BottomRight) corners)
        {
            Vector2 topLeft = points[0];
            Vector2 bottomRight = points[0];

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i];

                if (p.z < 0)
                {
                    corners = default;
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

        public static (Vector3 TopLeft, Vector3 BottomRight) GetCorners(Vector3[] points)
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

        public static (Vector2 TopLeft, Vector2 BottomRight) GetScreenCorners(Vector3[] points)
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

        public static float ModularClamp(float val, float min, float max, float rangeMin = -180f, float rangeMax = 180f)
        {
            float modulus = Maths.Abs(rangeMax - rangeMin);
            if ((val %= modulus) < 0f) val += modulus;
            return System.Math.Clamp(val + Maths.Min(rangeMin, rangeMax), min, max);
        }

        static Texture2D? _whiteTexture;
        /// <summary>
        /// <see href="https://github.com/pickles976/RTS_selection/blob/master/Utils.cs"/>
        /// </summary>
        public static Texture2D WhiteTexture
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
        public static void DrawScreenRect(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = Color.white;
        }

        /// <summary>
        /// <see href="https://github.com/pickles976/RTS_selection/blob/master/Utils.cs"/>
        /// </summary>
        public static void DrawScreenRectBorder(Rect rect, float thickness, Color color)
        {
            // Top
            UnityUtils.DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            // Left
            UnityUtils.DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            // Right
            UnityUtils.DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
            // Bottom
            UnityUtils.DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        }

        /// <summary>
        /// <see href="https://github.com/pickles976/RTS_selection/blob/master/Utils.cs"/>
        /// </summary>
        public static Rect GetScreenRect(Vector3 screenPositionA, Vector3 screenPositionB)
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
        public static bool GetScreenCorners(Camera camera, Bounds bounds, out (Vector2 TopLeft, Vector2 BottomRight) corners)
        {
            ProjectBounds(camera, bounds, BoundsProjectorCorners);

            Vector2 topLeft = BoundsProjectorCorners[0];
            Vector2 bottomRight = BoundsProjectorCorners[0];

            for (int i = 0; i < BoundsProjectorCorners.Length; i++)
            {
                Vector3 p = BoundsProjectorCorners[i];

                if (p.z < 0)
                {
                    corners = default;
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

    public static class Ballistics
    {
        public readonly struct ProfilerMarkers
        {
            public static readonly Unity.Profiling.ProfilerMarker TrajectoryMath = new("Game.Math.Trajectory");
        }

        /// <summary>
        /// This is positive
        /// </summary>
        public static float G => Maths.Abs(Physics.gravity.y);
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
                => new(Velocity * Maths.Cos(Angle * Maths.Deg2Rad), Velocity * Maths.Sin(Angle * Maths.Deg2Rad));

            public Vector3 Velocity3D()
            {
                Vector3 result = default;
                result.x = Maths.Sin(Direction * Maths.Deg2Rad);
                result.y = Maths.Sin(Angle * Maths.Deg2Rad);
                result.z = Maths.Cos(Direction * Maths.Deg2Rad);
                return result;
            }

            public Vector3 Position(float t)
            {
                Vector2 displacement = Utilities.Ballistics.Displacement(Angle * Maths.Deg2Rad, Velocity, t);
                Vector3 displacement3D = default;

                displacement3D.x = displacement.x * Maths.Sin(Direction * Maths.Deg2Rad);
                displacement3D.y = displacement.y;
                displacement3D.z = displacement.x * Maths.Cos(Direction * Maths.Deg2Rad);

                displacement3D += StartPosition;

                return displacement3D;
            }

            public static Vector2 TransformPositionToPlane(Vector3 position, float directionRad) => new()
            {
                y = position.y,
                x = position.x * Maths.Cos(directionRad) + position.y * Maths.Sin(directionRad),
            };
        }

        public static Vector3? PredictImpact(Transform shootPosition, float projectileVelocity, float projectileLifetime, out bool outOfRange)
        {
            outOfRange = false;
            float? _relativeHitDistance;
            float angle = -shootPosition.eulerAngles.x;

            using (Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
            { _relativeHitDistance = Ballistics.CalculateX(angle * Maths.Deg2Rad, projectileVelocity, shootPosition.position.y); }
            if (!_relativeHitDistance.HasValue) return null;
            float relativeHitDistance = _relativeHitDistance.Value;

            Vector3 turretRotation = shootPosition.forward.Flatten();
            Vector3 point = shootPosition.position + (relativeHitDistance * turretRotation);
            point.y = 0f;

            if (projectileLifetime > 0f)
            {
                Vector2 maxHitDistance;
                using (Ballistics.ProfilerMarkers.TrajectoryMath.Auto())
                { maxHitDistance = Ballistics.Displacement(angle * Maths.Deg2Rad, projectileVelocity, projectileLifetime); }

                if (maxHitDistance.x < relativeHitDistance)
                {
                    outOfRange = true;
                    point = shootPosition.position + (maxHitDistance.x * turretRotation);
                    point.y = maxHitDistance.y;
                }
            }

            // point.y = Maths.Max(point.y, TheTerrain.Height(point));

            return point;
        }

        /// <param name="v">
        /// Projectile's initial velocity
        /// </param>
        public static float? CalculateTime(float v, float angle, float heightDisplacement)
        {
            float a = v * Maths.Sin(angle);
            float b = 2 * G * heightDisplacement;

            float discriminant = (a * a) + b;
            if (discriminant < 0)
            {
                return null;
            }

            float sqrt = Maths.Sqrt(discriminant);

            return (a + sqrt) / G;
        }

        /*
        public static float CalculateAngle(float v, float x)
        {
            // v0y = v0 * Maths.Sin(theta)
            // v0x = v0 * Maths.Cos(theta)

            // y = y0 + v0y * t + 0.5 * G * t * t
            // 0 = 0 + v0y * t + 0.5 * G * t * t

            // x = v0x * t



            // 0 = 0 + v0 * Maths.Sin(theta) * t + 0.5 * G * t * t

            // x = v0 * Maths.Cos(theta) * t
            // t = x / ( v0 * Maths.Cos(theta) )

            // 0 = 0 + v0 * Maths.Sin(theta) * (x / ( v0 * Maths.Cos(theta) )) + 0.5 * G * Maths.Pow((x / ( v0 * Maths.Cos(theta) )), 2)

            // 0 = Maths.Sin(theta) * Maths.Cos(theta) - ( (G * x) / (2 * v0 * v0) )

            // 0 = 0.5 * Maths.Sin(2 * theta) - ( ... )

            // Maths.Sin(2 * theta) = ( ... )

            float theta = 0.5f * Maths.Asin((G * x) / (v * v));

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
            Vector3 diff = target - from;
            float y = diff.y;
            float x = Maths.Sqrt((diff.x * diff.x) + (diff.z * diff.z));
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
            Vector3 diff = target - from;
            float y = diff.y;
            float x = Maths.Sqrt((diff.x * diff.x) + (diff.z * diff.z));
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
            Vector3 diff = target - from;
            float y = diff.y;
            float x = Maths.Sqrt((diff.x * diff.x) + (diff.z * diff.z));
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

            float theta = Maths.Atan2(v2 - Maths.Sqrt(v4 - g * (g * x2 + 2 * y * v2)), g * x);

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
            => (v * Maths.Sin(angleRad) * t) - ((G * t * t) / 2f);

        public static float CalculateTimeToMaxHeight(float angleRad, float v)
            => (v * Maths.Sin(angleRad)) / G;

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

            float dSqrt = Maths.Sqrt(discriminant);

            float a = (v2 + dSqrt) / (G * x);
            float b = (v2 - dSqrt) / (G * x);

            float a_ = Maths.Atan(a);
            float b_ = Maths.Atan(b);

            return (a_, b_);
        }

        /// <summary>
        /// To hit a <paramref name="target"/> at range <c>x</c> and altitude <c>y</c> when fired from <c>(0,0)</c> and with initial speed <paramref name="v"/>.
        /// </summary>
        public static float? AngleOfReach1(float v, Vector2 target)
        {
            float v2 = v * v;

            float x = target.x;
            float y = target.y;

            float discriminant = (v2 * v2) - (G * ((G * x * x) + (2 * y * v2)));

            if (discriminant < 0f)
            { return null; }

            float dSqrt = Maths.Sqrt(discriminant);

            float a = (v2 + dSqrt) / (G * x);

            float a_ = Maths.Atan(a);

            return a_;
        }

        /// <summary>
        /// To hit a <paramref name="target"/> at range <c>x</c> and altitude <c>y</c> when fired from <c>(0,0)</c> and with initial speed <paramref name="v"/>.
        /// </summary>
        public static float? AngleOfReach2(float v, Vector2 target)
        {
            float v2 = v * v;

            float x = target.x;
            float y = target.y;

            float discriminant = (v2 * v2) - (G * ((G * x * x) + (2 * y * v2)));

            if (discriminant < 0f)
            { return null; }

            float dSqrt = Maths.Sqrt(discriminant);

            float b = (v2 - dSqrt) / (G * x);

            float b_ = Maths.Atan(b);

            return b_;
        }

        /// <param name="angleRad">Launch angle</param>
        /// <param name="v">Initial velocity</param>
        /// <returns>The greatest height that the object will reach</returns>
        public static float MaxHeight(float angleRad, float v)
            => (v * v * Maths.Pow(Maths.Sin(angleRad), 2f)) / (2f * G);

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

            float shallow = 0.5f * Maths.Asin(a);
            float steep = 0.5f * Maths.Acos(a);

            return (shallow, steep);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Radius(float v, float angleRad)
            => ((v * v) / G) * Maths.Sin(angleRad * 2f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MaxRadius(float v)
            => (v * v) / G;

        /// <param name="angleRad">Launch angle</param>
        /// <param name="v">Initial velocity</param>
        /// <param name="t">Time</param>
        /// <returns>The velocity after time <paramref name="t"/> or <c><see langword="null"/></c> if there is no solution</returns>
        public static float? Velocity(float angleRad, float v, float t)
        {
            float vx = v * Maths.Cos(angleRad);
            float vy = (v * Maths.Sin(angleRad)) - (G * t);
            float a = (vx * vx) + (vy * vy);
            if (a < 0f)
            { return null; }
            return Maths.Sqrt(a);
        }

        /// <param name="angleRad">Launch angle</param>
        /// <param name="v">Initial velocity</param>
        /// <param name="t">Time</param>
        public static Vector2 Displacement(float angleRad, float v, float t)
        {
            float x = v * t * Maths.Cos(angleRad);
            float y = (v * t * Maths.Sin(angleRad)) - (0.5f * G * t * t);
            return new Vector2(x, y);
        }

        /// <param name="angleRad">Launch angle</param>
        /// <param name="v">Initial velocity</param>
        /// <param name="t">Time</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DisplacementX(float angleRad, float v, float t)
            => v * t * Maths.Cos(angleRad);

        /// <param name="angleRad">Launch angle</param>
        /// <param name="v">Initial velocity</param>
        /// <param name="t">Time</param>
        public static float DisplacementY(float angleRad, float v, float t)
            => (v * t * Maths.Sin(angleRad)) - (0.5f * G * t * t);

        /// <param name="angleRad">Launch angle</param>
        /// <param name="displacement">Displacement</param>
        /// <returns>The initial velocity or <c><see langword="null"/></c> if there is no solution</returns>
        public static float? InitialVelocity(float angleRad, Vector2 displacement)
        {
            float x = displacement.x;
            float y = displacement.y;

            float a = x * x * G;
            float b = x * Maths.Sin(angleRad * 2f);
            float c = 2f * y * Maths.Pow(Maths.Cos(angleRad), 2f);
            float d = a / (b - c);
            if (d < 0f)
            { return null; }
            return Maths.Sqrt(d);
        }

        public static float MaxRadius(float v, float heightDisplacement)
        {
            float t = CalculateTime(v, 45f * Maths.Deg2Rad, heightDisplacement) ?? throw new Exception();
            float x = DisplacementX(45f * Maths.Deg2Rad, t, v);
            return x;
        }

        /*
        public static float? find_shooting_angle(Vector2 target_position, float target_velocity, float shootAngle, Vector2 shooter_position, float shooter_velocity)
        {
            float tolerance = 0.01f;  // Tolerance for convergence
            int max_iterations = 100;// Maximum number of iterations
            float lower_angle = 0.0f; // Initial lower angle bound (radians)
            float upper_angle = Maths.PI / 2; // Initial upper angle bound (radians)

            for (int i = 0; i < max_iterations; i++)
            {
                float angle = (lower_angle + upper_angle) / 2f;
                float time_of_flight_shooter = calculate_time_of_flight(angle, shooter_velocity, shooter_position);
                float time_of_flight_target = calculate_time_of_flight(shootAngle, target_velocity, target_position);

                if (Maths.Abs(time_of_flight_shooter - time_of_flight_target) <= tolerance)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TimeOfFlight(float v, float angleRad)
            => (2f * v * Maths.Sin(angleRad)) / G;

        public static float? TimeToReachDistance(float v, float angleRad, float d)
        {
            float a = v * Maths.Cos(angleRad);
            if (a <= 0f)
            { return null; }
            return d / a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MaxHeight2(float d, float angleRad)
            => (d * Maths.Tan(angleRad)) / 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 GetPosition(Vector2 v, float t)
            => (v * t) + ((t * t * GVector) / 2);

        /// <summary>
        /// <see href="https://www.toppr.com/guides/physics/motion-in-a-plane/projectile-motion/"/>
        /// <c>y = (tan θ) * x – g (x ^ 2) / 2 * (v * cos θ) ^ 2</c>
        /// </summary>
        public static float GetHeight(float d, float angleRad, float v)
        {
            float a = Maths.Tan(angleRad) * d;
            float b = G * d * d;
            float c = Maths.Pow(v * Maths.Cos(angleRad), 2) * 2f;
            return a - (b / c);
        }

        public static (Vector3 PredictedPosition, float TimeToReach)? CalculateInterceptCourse(float projectileVelocity, float projectileLifetime, Vector3 shootPosition, Trajectory targetTrajectory)
        {
            float? angle_;
            float? t;
            Vector3 targetPosition;
            int iterations = 3;

            using (ProfilerMarkers.TrajectoryMath.Auto())
            {
                // projectileVelocity *= .95f;

                float lifetime = projectileLifetime + Time.fixedDeltaTime;

                float? projectileTimeOfFlight = Ballistics.CalculateTime(targetTrajectory.Velocity, targetTrajectory.Angle * Maths.Deg2Rad, targetTrajectory.StartPosition.y);

                if (projectileTimeOfFlight.HasValue && (projectileTimeOfFlight - lifetime) < .5f)
                { return null; }

                targetPosition = targetTrajectory.Position(lifetime);

                float distance = Maths.Distance(shootPosition.To2D(), targetPosition.To2D());

                angle_ = Ballistics.AngleOfReach2(projectileVelocity, shootPosition, targetPosition);

                t = angle_.HasValue ? Ballistics.TimeToReachDistance(projectileVelocity, angle_.Value, distance) : null;

                for (int i = 0; i < iterations; i++)
                {
                    if (!angle_.HasValue) break;
                    if (!t.HasValue) break;

                    targetPosition = targetTrajectory.Position(lifetime + t.Value);

                    distance = Maths.Distance(shootPosition.To2D(), targetPosition.To2D());

                    angle_ = Ballistics.AngleOfReach2(projectileVelocity, shootPosition, targetPosition);

                    t = angle_.HasValue ? Ballistics.TimeToReachDistance(projectileVelocity, angle_.Value, distance) : null;
                }
            }

            return (targetPosition, t!.Value);
        }

        public static Vector2? CalculateInterceptCourse(Vector2 projectilePosition, float projectileVelocity, Vector2 targetPosition, Vector2 targetVelocity)
        {
            float time = 0f;
            int iterations = 3;
            Vector2 targetOriginalPosition = targetPosition;

            float height = projectilePosition.y - targetPosition.y;

            using (ProfilerMarkers.TrajectoryMath.Auto())
            {
                // projectileVelocity *= .95f;

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

    public struct Line
    {
        public Vector2 PointA;
        public Vector2 PointB;

        public Line(Vector2 pointA, Vector2 pointB)
        {
            PointA = pointA;
            PointB = pointB;
        }

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
            float crossProduct = (c.y - a.y) * (b.x - a.x) - (c.x - a.x) * (b.y - a.y);

            // compare versus epsilon for floating point values, or != 0 if using integers
            if (Maths.Abs(crossProduct) > 0.0001f)
            { return false; }

            float dotProduct = (c.x - a.x) * (b.x - a.x) + (c.y - a.y) * (b.y - a.y);
            if (dotProduct < 0)
            { return false; }

            float squaredLengthBA = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
            if (dotProduct > squaredLengthBA)
            { return false; }

            return true;

        }
    }

    public static class Acceleration
    {
        public const float LargeNumber = 69420f;

        /// <summary>
        /// If <paramref name="time"/> is 0 it returns <see cref="LargeNumber"/> to avoid division by zero
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateAcceleration(float initialVelocity, float topVelocity, float time)
        {
            if (time == 0f) return LargeNumber;
            return (topVelocity - initialVelocity) / time;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SpeedAfterTime(float velocity, float acceleration, float time)
            => velocity + (acceleration * time);

        public static float SpeedAfterDistance(float velocity, float acceleration, float distance)
        {
            if (acceleration == 0f) return velocity;
            if (distance == 0f) return velocity;

            float valueUnderSqr = (2 * acceleration * distance + (velocity * velocity));
            if (valueUnderSqr <= 0f) return 0f;

            return Maths.Sqrt(valueUnderSqr);
        }

        /// <summary>
        /// <b>v * t + ½ * a * t²</b> <br/><br/>
        /// 
        /// v: <paramref name="velocity"/> <br/>
        /// a: <paramref name="acceleration"/> <br/>
        /// t: <paramref name="time"/> <br/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceAfterTime(float velocity, float acceleration, float time)
            => (velocity * time) + ((acceleration / 2) * (time * time));

        /// <summary>
        /// <b>Δv / a</b> <br/>
        /// or <br/>
        /// <b>(v - vₒ) / a</b> <br/><br/>
        /// 
        /// If <paramref name="targetVelocity"/> can't be reached, it returns <see cref="LargeNumber"/> to avoid division by zero. <br/><br/>
        /// 
        /// v: <paramref name="targetVelocity"/> <br/>
        /// vₒ: <paramref name="initialVelocity"/> <br/>
        /// a: <paramref name="acceleration"/> <br/>
        /// </summary>
        public static float TimeToReachVelocity(float initialVelocity, float targetVelocity, float acceleration)
        {
            if (acceleration == 0f) return LargeNumber;
            if (initialVelocity < targetVelocity && acceleration < 0f) return LargeNumber;
            if (initialVelocity > targetVelocity && acceleration > 0f) return LargeNumber;

            return (targetVelocity - initialVelocity) / acceleration;
        }

        /// <summary>
        /// <b>-vₒ / a</b> <br/><br/>
        /// 
        /// If 0 velocity can't be reached, it returns <see cref="LargeNumber"/> to avoid division by zero. <br/><br/>
        /// 
        /// vₒ: <paramref name="initialVelocity"/> <br/>
        /// a: <paramref name="acceleration"/> <br/>
        /// </summary>
        public static float TimeToStop(float initialVelocity, float acceleration)
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
        public static float DistanceToReachVelocity(float initialVelocity, float targetVelocity, float acceleration)
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
        public static float DistanceToStop(float velocity, float braking)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateDistanceFromSpeed(float initialVelocity, float topVelocity, float time)
            => Maths.Average(initialVelocity, topVelocity) * time;

        public static float CalculateTime(float initialVelocity, float topVelocity, float timeToSpeedUp, float distance, float acceleration)
        {
            float distanceTravelledUntilMaxSpeed = DistanceAfterTime(initialVelocity, acceleration, timeToSpeedUp);
            float timeWithMaxVelocity = Velocity.CalculateTime(distance - distanceTravelledUntilMaxSpeed, topVelocity);
            return timeToSpeedUp + timeWithMaxVelocity;
        }

        /// <returns>Aim offset</returns>
        public static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 projectilePosition, float projectileVelocity, float projectileAcceleration)
        {
            float distance;
            float time = 0f;

            int iterations = 3;
            for (int i = 0; i < iterations; i++)
            {
                distance = Maths.Distance(projectilePosition, targetPosition + (targetVelocity * time));
                float speedAfterThis = SpeedAfterDistance(projectileVelocity, projectileAcceleration, distance);
                time = TimeToReachVelocity(projectileVelocity, speedAfterThis, projectileAcceleration);
            }

            return targetVelocity * time;
        }

        /// <returns>Aim offset</returns>
        public static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 targetAcceleration, Vector2 projectilePosition, float projectileVelocity, float projectileAcceleration)
        {
            Vector2 targetOriginalVelocity = targetVelocity;
            float distance;
            float time = 0f;

            int iterations = 4;
            for (int i = 0; i < iterations; i++)
            {
                distance = Maths.Distance(projectilePosition, targetPosition + (targetVelocity * time));
                float speedAfterThis = SpeedAfterDistance(projectileVelocity, projectileAcceleration, distance);
                time = TimeToReachVelocity(projectileVelocity, speedAfterThis, projectileAcceleration);
                targetVelocity = targetOriginalVelocity.normalized * SpeedAfterTime(targetOriginalVelocity.magnitude, targetAcceleration.magnitude, time);
            }

            return targetVelocity * time;
        }

        /// <returns>Aim offset</returns>
        public static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 targetAcceleration, Vector2 projectilePosition, float projectileVelocity)
        {
            Vector2 targetOriginalVelocity = targetVelocity;
            float distance;
            float time = 0f;

            int iterations = 4;
            for (int i = 0; i < iterations; i++)
            {
                distance = Maths.Distance(projectilePosition, targetPosition + (targetVelocity * time));
                time = Velocity.CalculateTime(distance, projectileVelocity);
                targetVelocity = targetOriginalVelocity.normalized * SpeedAfterTime(targetOriginalVelocity.magnitude, targetAcceleration.magnitude, time);
            }

            return targetVelocity * time;
        }
    }

    public static class Velocity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateTime(Vector2 pointA, Vector2 pointB, float speed)
            => CalculateTime(Maths.Distance(pointA, pointB), speed);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateSpeed(float distance, float time)
            => time == 0f ? 0f : distance / time;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateDistance(float velocity, float time)
            => velocity * time;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateTime(float distance, float velocity)
            => velocity == 0f ? 0f : distance / velocity;

        /// <returns>Aim offset</returns>
        public static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 projectilePosition, float projectileVelocity)
        {
            float distance;
            float time = 0f;

            int iterations = 3;
            for (int i = 0; i < iterations; i++)
            {
                distance = Maths.Distance(projectilePosition, targetPosition + (targetVelocity * time));
                time = CalculateTime(distance, projectileVelocity);
            }

            return targetVelocity * time;
        }
        /// <returns>Aim offset</returns>
        public static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 projectilePosition, float projectileVelocity, Maths.Circle circle)
        {
            float p = 1 / projectileVelocity;

            float distance = Maths.Distance(projectilePosition, targetPosition);
            float time = distance * p;

            distance = Maths.Distance(projectilePosition, circle.GetPointAfterTime(targetVelocity.magnitude, time, circle.GetAngle(targetPosition)));
            time = distance * p;

            distance = Maths.Distance(projectilePosition, circle.GetPointAfterTime(targetVelocity.magnitude, time, circle.GetAngle(targetPosition)));
            time = distance * p;

            Vector2 aim = circle.GetPointAfterTime(targetVelocity.magnitude, time, circle.GetAngle(targetPosition));
            return targetPosition - aim;
        }
    }

    public static class Debug3D
    {
        public static void DrawMesh(Mesh mesh, Color color, float duration)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Label(float x, float y, float z, string content)
            => Label(new Vector3(x, y, z), content);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Label(Vector3 position, string content)
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
            new(-0.5f, 0.5f, 0, 1),
            new(0.5f, 0.5f, 0, 1),
            new(0.5f, -0.5f, 0, 1),
            new(-0.5f, -0.5f, 0, 1),
        };
        /// <summary>
        /// Cube with edge of length 1
        /// </summary>
        static readonly Vector4[] s_UnitCube =
        {
            new(-0.5f,  0.5f, -0.5f, 1),
            new(0.5f,  0.5f, -0.5f, 1),
            new(0.5f, -0.5f, -0.5f, 1),
            new(-0.5f, -0.5f, -0.5f, 1),

            new(-0.5f,  0.5f,  0.5f, 1),
            new(0.5f,  0.5f,  0.5f, 1),
            new(0.5f, -0.5f,  0.5f, 1),
            new(-0.5f, -0.5f,  0.5f, 1)
        };
        static readonly Vector4[] s_UnitSphere = MakeUnitSphere(16);

        static Vector4[] MakeUnitSphere(int len)
        {
            Debug.Assert(len > 2);
            Vector4[] v = new Vector4[len * 3];
            for (int i = 0; i < len; i++)
            {
                float f = i / (float)len;
                float c = Maths.Cos(f * Maths.PI * 2f);
                float s = Maths.Sin(f * Maths.PI * 2f);
                v[0 * len + i] = new Vector4(c, s, 0, 1);
                v[1 * len + i] = new Vector4(0, c, s, 1);
                v[2 * len + i] = new Vector4(s, 0, c, 1);
            }
            return v;
        }

        public static void DrawSphere(Vector4 pos, float radius, Color color)
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

        public static void DrawSphere(Vector4 pos, float radius, Color color, float duration)
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

        public static void DrawBox(Vector4 pos, Vector3 size, Color color)
        {
            Vector4[] v = s_UnitCube;
            Vector4 sz = new(size.x, size.y, size.z, 1);
            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[i], sz);
                Vector4 e = pos + Vector4.Scale(v[(i + 1) % 4], sz);
                Debug.DrawLine(s, e, color);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[4 + i], sz);
                Vector4 e = pos + Vector4.Scale(v[4 + ((i + 1) % 4)], sz);
                Debug.DrawLine(s, e, color);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[i], sz);
                Vector4 e = pos + Vector4.Scale(v[i + 4], sz);
                Debug.DrawLine(s, e, color);
            }
        }

        public static void DrawBox(Vector4 pos, Vector3 size, Color color, float duration)
        {
            Vector4[] v = s_UnitCube;
            Vector4 sz = new(size.x, size.y, size.z, 1);
            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[i], sz);
                Vector4 e = pos + Vector4.Scale(v[(i + 1) % 4], sz);
                Debug.DrawLine(s, e, color, duration);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[4 + i], sz);
                Vector4 e = pos + Vector4.Scale(v[4 + ((i + 1) % 4)], sz);
                Debug.DrawLine(s, e, color, duration);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[i], sz);
                Vector4 e = pos + Vector4.Scale(v[i + 4], sz);
                Debug.DrawLine(s, e, color, duration);
            }
        }

        public static void DrawBox(Bounds bounds, Color color)
            => DrawBox(bounds.center, bounds.size, color);

        public static void DrawBox(Bounds bounds, Color color, float duration)
            => DrawBox(bounds.center, bounds.size, color, duration);

        public static void DrawAxes(Vector4 pos)
            => DrawAxes(pos, 1f);

        public static void DrawAxes(Vector4 pos, float scale)
        {
            Debug.DrawLine(pos, pos + new Vector4(scale, 0, 0), Color.red);
            Debug.DrawLine(pos, pos + new Vector4(0, scale, 0), Color.green);
            Debug.DrawLine(pos, pos + new Vector4(0, 0, scale), Color.blue);
        }

        public static void DrawAxes(Vector4 pos, float scale, float duration)
        {
            Debug.DrawLine(pos, pos + new Vector4(scale, 0, 0), Color.red, duration);
            Debug.DrawLine(pos, pos + new Vector4(0, scale, 0), Color.green, duration);
            Debug.DrawLine(pos, pos + new Vector4(0, 0, scale), Color.blue, duration);
        }

        public static void DrawPoint(Vector3 position, float scale, Color color)
        {
            Vector3 up = Vector3.up * scale;
            Vector3 right = Vector3.right * scale;
            Vector3 forward = Vector3.forward * scale;

            Debug.DrawLine(position - up, position + up, color);
            Debug.DrawLine(position - right, position + right, color);
            Debug.DrawLine(position - forward, position + forward, color);
        }

        public static void DrawPoint(Vector3 position, float scale, Color color, float duration)
        {
            Vector3 up = Vector3.up * scale;
            Vector3 right = Vector3.right * scale;
            Vector3 forward = Vector3.forward * scale;

            Debug.DrawLine(position - up, position + up, color, duration);
            Debug.DrawLine(position - right, position + right, color, duration);
            Debug.DrawLine(position - forward, position + forward, color, duration);
        }
    }

    public static class GizmosPlus
    {
        public static void DrawMesh(Mesh mesh)
        {
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (i + 1 < vertices.Length)
                {
                    UnityEngine.Gizmos.DrawLine(vertices[i], vertices[i + 1]);

                    if (i + 2 < vertices.Length)
                    {
                        UnityEngine.Gizmos.DrawLine(vertices[i + 1], vertices[i + 2]);
                        UnityEngine.Gizmos.DrawLine(vertices[i + 2], vertices[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Square with edge of length 1
        /// </summary>
        static readonly Vector4[] s_UnitSquare =
        {
            new(-0.5f, 0.5f, 0, 1),
            new(0.5f, 0.5f, 0, 1),
            new(0.5f, -0.5f, 0, 1),
            new(-0.5f, -0.5f, 0, 1),
        };
        /// <summary>
        /// Cube with edge of length 1
        /// </summary>
        static readonly Vector4[] s_UnitCube =
        {
            new(-0.5f,  0.5f, -0.5f, 1),
            new(0.5f,  0.5f, -0.5f, 1),
            new(0.5f, -0.5f, -0.5f, 1),
            new(-0.5f, -0.5f, -0.5f, 1),

            new(-0.5f,  0.5f,  0.5f, 1),
            new(0.5f,  0.5f,  0.5f, 1),
            new(0.5f, -0.5f,  0.5f, 1),
            new(-0.5f, -0.5f,  0.5f, 1)
        };
        static readonly Vector4[] s_UnitSphere = MakeUnitSphere(16);

        static Vector4[] MakeUnitSphere(int len)
        {
            Debug.Assert(len > 2);
            Vector4[] v = new Vector4[len * 3];
            for (int i = 0; i < len; i++)
            {
                float f = i / (float)len;
                float c = Maths.Cos(f * Maths.PI * 2f);
                float s = Maths.Sin(f * Maths.PI * 2f);
                v[0 * len + i] = new Vector4(c, s, 0, 1);
                v[1 * len + i] = new Vector4(0, c, s, 1);
                v[2 * len + i] = new Vector4(s, 0, c, 1);
            }
            return v;
        }

        public static void DrawSphere(Vector4 pos, float radius)
        {
            Vector4[] v = s_UnitSphere;
            int len = v.Length / 3;
            for (int i = 0; i < len; i++)
            {
                Vector4 sX = pos + radius * v[0 * len + i];
                Vector4 eX = pos + radius * v[0 * len + (i + 1) % len];
                Gizmos.DrawLine(sX, eX);

                Vector4 sY = pos + radius * v[1 * len + i];
                Vector4 eY = pos + radius * v[1 * len + (i + 1) % len];
                Gizmos.DrawLine(sY, eY);

                Vector4 sZ = pos + radius * v[2 * len + i];
                Vector4 eZ = pos + radius * v[2 * len + (i + 1) % len];
                Gizmos.DrawLine(sZ, eZ);
            }
        }

        public static void DrawBox(Vector4 pos, Vector3 size)
        {
            Vector4[] v = s_UnitCube;
            Vector4 sz = new(size.x, size.y, size.z, 1);
            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[i], sz);
                Vector4 e = pos + Vector4.Scale(v[(i + 1) % 4], sz);
                Gizmos.DrawLine(s, e);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[4 + i], sz);
                Vector4 e = pos + Vector4.Scale(v[4 + ((i + 1) % 4)], sz);
                Gizmos.DrawLine(s, e);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[i], sz);
                Vector4 e = pos + Vector4.Scale(v[i + 4], sz);
                Gizmos.DrawLine(s, e);
            }
        }

        public static void DrawBox(Bounds bounds)
            => DrawBox(bounds.center, bounds.size);

        public static void DrawAxes(Vector4 pos)
            => DrawAxes(pos, 1f);

        public static void DrawAxes(Vector4 pos, float scale)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, pos + new Vector4(scale, 0, 0));
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos, pos + new Vector4(0, scale, 0));
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pos, pos + new Vector4(0, 0, scale));
        }

        public static void DrawPoint(Vector3 position, float scale)
        {
            Vector3 up = Vector3.up * scale;
            Vector3 right = Vector3.right * scale;
            Vector3 forward = Vector3.forward * scale;

            Gizmos.DrawLine(position - up, position + up);
            Gizmos.DrawLine(position - right, position + right);
            Gizmos.DrawLine(position - forward, position + forward);
        }
    }
}

public static class Searcher
{
    public static void Search<T>(MonoBehaviour caller, T[] list, Action<T> callback)
        => new ArraySearcherCoroutine<T>(callback).Search(caller, list);

    public static void Search<T>(MonoBehaviour caller, IEnumerable<T> list, Action<T> callback)
        => new AnySearcherCoroutine<T>(callback).Search(caller, list);
}

public class ArraySearcherCoroutine<T>
{
    int i;
    T[]? gotList = null;
    bool isRunning;

    readonly Action<T> callback;

    public bool IsRunning => isRunning;

    public ArraySearcherCoroutine(Action<T> callback)
    {
        this.i = 0;
        this.gotList = null;
        this.callback = callback;
        this.isRunning = false;
    }

    public void Search(MonoBehaviour caller, T[] list)
    {
        isRunning = true;
        caller.StartCoroutine(Search(list));
    }

    public IEnumerator Search(T[] list)
    {
        i = 0;
        gotList = list;

        if (callback == null ||
            gotList == null)
        {
            isRunning = false;
            yield break;
        }

        while (i < gotList.Length)
        {
            yield return new WaitForFixedUpdate();

            callback?.Invoke(gotList[i]);
            i++;
        }

        isRunning = false;
    }
}

public class AnySearcherCoroutine<T>
{
    IEnumerable<T>? gotList = null;
    bool isRunning;

    readonly Action<T> callback;

    public bool IsRunning => isRunning;

    public AnySearcherCoroutine(Action<T> callback)
    {
        this.gotList = null;
        this.callback = callback;
        this.isRunning = false;
    }

    public void Search(MonoBehaviour caller, IEnumerable<T> list)
    {
        isRunning = true;
        caller.StartCoroutine(Search(list));
    }

    public IEnumerator Search(IEnumerable<T> list)
    {
        gotList = list;

        if (callback == null)
        {
            isRunning = false;
            yield break;
        }

        foreach (T item in gotList)
        {
            yield return new WaitForFixedUpdate();
            callback?.Invoke(item);
        }

        isRunning = false;
    }
}

public readonly struct EditorUtils
{
    public static string ProjectPath => System.IO.Path.GetDirectoryName(Application.dataPath);
    public static string ResourcesPath => System.IO.Path.Combine(Application.dataPath, "Resources");
}

/// <summary>
/// This can be converted into <see cref="Component"/>
/// </summary>
public interface IComponent { }

#pragma warning disable UNT0014 // Invalid type for call to GetComponent
public static class IObjectExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasComponent<T>(this IComponent self)
        => ((Component)self).HasComponent<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Component Object(this IComponent self)
        => (Component)self;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GameObject GetGameObject(this IComponent self)
        => ((Component)self).gameObject;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetComponent<T>(this IComponent self)
        => !((Component)self).TryGetComponent(out T component) ? default : component;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetComponent<T>(this IComponent self, out T component)
        => ((Component)self).TryGetComponent(out component);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetComponentInChildren<T>(this IComponent self)
        => !((Component)self).TryGetComponentInChildren(out T component) ? default : component;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetComponentInChildren<T>(this IComponent self, out T component)
        => ((Component)self).TryGetComponentInChildren(out component);
}
#pragma warning restore UNT0014 // Invalid type for call to GetComponent

public static class Intervals
{
    public delegate bool Condition();

    public static void Timeout(this MonoBehaviour context, Action action, float timeout, Condition? condition = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        context.StartCoroutine(Intervals.TimeoutCoroutine(action, timeout, condition));
    }

    static IEnumerator TimeoutCoroutine(Action action, float timeout, Condition? condition = null)
    {
        yield return new WaitForSeconds(timeout);
        while (!(condition?.Invoke() ?? true))
        { yield return new WaitForSeconds(0.1f); }
        action.Invoke();
    }

    public static void Timeout<T0>(this MonoBehaviour context, Action<T0> action, T0 parameter0, float timeout, Condition? condition = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        context.StartCoroutine(Intervals.TimeoutCoroutine(action, parameter0, timeout, condition));
    }

    static IEnumerator TimeoutCoroutine<T0>(Action<T0> action, T0 parameter0, float timeout, Condition? condition = null)
    {
        yield return new WaitForSeconds(timeout);
        while (!(condition?.Invoke() ?? true))
        { yield return new WaitForSeconds(0.1f); }
        action.Invoke(parameter0);
    }

    public static void Timeout<T0, T1>(this MonoBehaviour context, Action<T0, T1> action, T0 parameter0, T1 parameter1, float timeout, Condition? condition = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        context.StartCoroutine(Intervals.TimeoutCoroutine(action, parameter0, parameter1, timeout, condition));
    }

    static IEnumerator TimeoutCoroutine<T0, T1>(Action<T0, T1> action, T0 parameter0, T1 parameter1, float timeout, Condition? condition = null)
    {
        yield return new WaitForSeconds(timeout);
        while (!(condition?.Invoke() ?? true))
        { yield return new WaitForSeconds(0.1f); }
        action.Invoke(parameter0, parameter1);
    }

    public static void Timeout<T0, T1, T2>(this MonoBehaviour context, Action<T0, T1, T2> action, T0 parameter0, T1 parameter1, T2 parameter2, float timeout, Condition? condition = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        context.StartCoroutine(Intervals.TimeoutCoroutine(action, parameter0, parameter1, parameter2, timeout, condition));
    }

    static IEnumerator TimeoutCoroutine<T0, T1, T2>(Action<T0, T1, T2> action, T0 parameter0, T1 parameter1, T2 parameter2, float timeout, Condition? condition = null)
    {
        yield return new WaitForSeconds(timeout);
        while (!(condition?.Invoke() ?? true))
        { yield return new WaitForSeconds(0.1f); }
        action.Invoke(parameter0, parameter1, parameter2);
    }

    public static UnityIntervalDynamicCondition Interval(this MonoBehaviour context, Action action, float interval, Condition? condition = null)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        UnityIntervalDynamicCondition unityInterval = new(context, action, interval, condition);
        unityInterval.Start();
        return unityInterval;
    }

    public abstract class UnityBaseInterval
    {
        protected Coroutine? Coroutine;

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
        readonly Condition? Condition;

        public UnityIntervalDynamicCondition(MonoBehaviour context, Action action, float interval, Condition? condition)
            : base(context, action, interval)
        {
            Condition = condition;
        }

        protected override IEnumerator IntervalCoroutine()
        {
            yield return new WaitForSeconds(Interval);
            while (true)
            {
                if (Condition?.Invoke() ?? true) Action.Invoke();
                yield return new WaitForSeconds(Interval);
            }
        }
    }
}

public struct MetricUtils
{
    const float Multiplier = 0.001f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetMeters(float value) => value * Multiplier;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 GetMeters(Vector2 value) => value * Multiplier;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetMeters(Vector3 value) => value * Multiplier;
}

public struct Mouse
{
    public const int Left = 0;
    public const int Right = 1;
    public const int Middle = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDown(int button) => Input.GetMouseButtonDown(button);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUp(int button) => Input.GetMouseButtonUp(button);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHold(int button) => Input.GetMouseButton(button);

    public static float DeltaX => Input.GetAxisRaw("Mouse X");
    public static float DeltaY => Input.GetAxisRaw("Mouse Y");
    public static Vector2 Delta => new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

    public static float ScrollDelta => Input.mouseScrollDelta.y;
}

public struct MouseAlt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDown(int button) => button switch
    {
        Mouse.Left => Input.GetKeyDown(KeyCode.Keypad0),
        Mouse.Middle => Input.GetKeyDown(KeyCode.KeypadPeriod),
        Mouse.Right => Input.GetKeyDown(KeyCode.KeypadEnter),
        _ => Input.GetMouseButtonDown(button),
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUp(int button) => button switch
    {
        Mouse.Left => Input.GetKeyUp(KeyCode.Keypad0),
        Mouse.Middle => Input.GetKeyUp(KeyCode.KeypadPeriod),
        Mouse.Right => Input.GetKeyUp(KeyCode.KeypadEnter),
        _ => Input.GetMouseButtonUp(button),
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHold(int button) => button switch
    {
        Mouse.Left => Input.GetKey(KeyCode.Keypad0),
        Mouse.Middle => Input.GetKey(KeyCode.KeypadPeriod),
        Mouse.Right => Input.GetKey(KeyCode.KeypadEnter),
        _ => Input.GetMouseButton(button),
    };

    public static bool HasDelta =>
        Input.GetKey(KeyCode.Keypad4) ||
        Input.GetKey(KeyCode.Keypad6) ||
        Input.GetKey(KeyCode.Keypad8) ||
        Input.GetKey(KeyCode.Keypad5);

    public static float DeltaX
    {
        get
        {
            bool left = Input.GetKey(KeyCode.Keypad4);
            bool right = Input.GetKey(KeyCode.Keypad6);

            if (left && !right)
            { return -1f; }

            if (right && !left)
            { return 1f; }

            return 0f;
        }
    }
    public static float DeltaY
    {
        get
        {
            bool up = Input.GetKey(KeyCode.Keypad8);
            bool down = Input.GetKey(KeyCode.Keypad5);

            if (up && !down)
            { return 1f; }

            if (down && !up)
            { return -1f; }

            return 0f;
        }
    }
    public static Vector2 Delta => new(DeltaX, DeltaY);

    public static bool HasScrollDelta =>
        Input.GetKey(KeyCode.KeypadPlus) ||
        Input.GetKey(KeyCode.KeypadMinus);

    public static float ScrollDelta
    {
        get
        {
            if (Input.GetKey(KeyCode.KeypadPlus))
            { return -1f; }

            if (Input.GetKey(KeyCode.KeypadMinus))
            { return 1f; }

            return 0f;
        }
    }
}
