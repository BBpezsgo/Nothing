using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

#nullable enable

public static class RectUtils
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
                float rad = 2 * MathF.PI * ((float)i / (float)segmentCount);
                Vector2 direction = new(MathF.Cos(rad), MathF.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * (radius)));
            }

            {
                float rad = 2 * MathF.PI * ((float)(i + 1) / (float)segmentCount);
                Vector2 direction = new(MathF.Cos(rad), MathF.Sin(rad));

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

        int segments = (int)(fillAmount * segmentCount);
        float step = 1f / (float)segmentCount;

        GL.Begin(GL.TRIANGLE_STRIP);
        GL.Color(color);

        for (int i = 0; i < segments; i++)
        {
            {
                float rad = 2 * MathF.PI * ((float)i / (float)segmentCount);
                Vector2 direction = new(MathF.Cos(rad), MathF.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * radius));
            }

            {
                float next = 1 + Math.Clamp((fillAmount - ((float)(i + 1) / (float)segmentCount)) / step, 0f, 1f);

                float rad = 2 * MathF.PI * ((float)(i + next) / (float)segmentCount);
                Vector2 direction = new(MathF.Cos(rad), MathF.Sin(rad));

                GL.Vertex(center + (direction * (radius + thickness)));
                GL.Vertex(center + (direction * radius));
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
            float rad = 2 * MathF.PI * ((float)i / (float)segmentCount);
            Vector2 direction = new(MathF.Cos(rad), MathF.Sin(rad));

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

        int segments = (int)(fillAmount * segmentCount);
        float step = 1f / (float)segmentCount;

        GL.Begin(GL.LINE_STRIP);
        GL.Color(color);

        for (int i = 0; i < segments; i++)
        {
            {
                float rad = 2 * MathF.PI * ((float)i / (float)segmentCount);
                Vector2 direction = new(MathF.Cos(rad), MathF.Sin(rad));

                GL.Vertex(center + (direction * radius));
            }

            {
                float next = 1 + Math.Clamp((fillAmount - ((float)(i + 1) / (float)segmentCount)) / step, 0f, 1f);

                float rad = 2 * MathF.PI * ((float)(i + next) / (float)segmentCount);
                Vector2 direction = new(MathF.Cos(rad), MathF.Sin(rad));

                GL.Vertex(center + (direction * radius));
            }
        }
        GL.End();
    }
}

public static class GUIUtils
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
    public static class UnityUtils
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
            float modulus = Math.Abs(rangeMax - rangeMin);
            if ((val %= modulus) < 0f) val += modulus;
            return System.Math.Clamp(val + Math.Min(rangeMin, rangeMax), min, max);
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
                    Debug.DrawLine(vertices[i], vertices[i + 1], color, duration);

                    if (i + 2 < vertices.Length)
                    {
                        Debug.DrawLine(vertices[i + 1], vertices[i + 2], color, duration);
                        Debug.DrawLine(vertices[i + 2], vertices[i], color, duration);
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
                float c = MathF.Cos(f * MathF.PI * 2f);
                float s = MathF.Sin(f * MathF.PI * 2f);
                v[(0 * len) + i] = new Vector4(c, s, 0, 1);
                v[(1 * len) + i] = new Vector4(0, c, s, 1);
                v[(2 * len) + i] = new Vector4(s, 0, c, 1);
            }
            return v;
        }

        public static void DrawSphere(Vector4 pos, float radius, Color color)
        {
            Vector4[] v = s_UnitSphere;
            int len = v.Length / 3;
            for (int i = 0; i < len; i++)
            {
                Vector4 sX = pos + (radius * v[(0 * len) + i]);
                Vector4 eX = pos + (radius * v[(0 * len) + ((i + 1) % len)]);
                Debug.DrawLine(sX, eX, color);

                Vector4 sY = pos + (radius * v[(1 * len) + i]);
                Vector4 eY = pos + (radius * v[(1 * len) + ((i + 1) % len)]);
                Debug.DrawLine(sY, eY, color);

                Vector4 sZ = pos + (radius * v[(2 * len) + i]);
                Vector4 eZ = pos + (radius * v[(2 * len) + ((i + 1) % len)]);
                Debug.DrawLine(sZ, eZ, color);
            }
        }

        public static void DrawSphere(Vector4 pos, float radius, Color color, float duration)
        {
            Vector4[] v = s_UnitSphere;
            int len = v.Length / 3;
            for (int i = 0; i < len; i++)
            {
                Vector4 sX = pos + (radius * v[(0 * len) + i]);
                Vector4 eX = pos + (radius * v[(0 * len) + ((i + 1) % len)]);
                Debug.DrawLine(sX, eX, color, duration);

                Vector4 sY = pos + (radius * v[(1 * len) + i]);
                Vector4 eY = pos + (radius * v[(1 * len) + ((i + 1) % len)]);
                Debug.DrawLine(sY, eY, color, duration);

                Vector4 sZ = pos + (radius * v[(2 * len) + i]);
                Vector4 eZ = pos + (radius * v[(2 * len) + ((i + 1) % len)]);
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
                float c = MathF.Cos(f * MathF.PI * 2f);
                float s = MathF.Sin(f * MathF.PI * 2f);
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

public static class EditorUtils
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

        protected UnityBaseInterval(MonoBehaviour context, Action action, float interval)
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

public static class MetricUtils
{
    const float Multiplier = 0.001f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetMeters(float value) => value * Multiplier;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 GetMeters(Vector2 value) => value * Multiplier;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 GetMeters(Vector3 value) => value * Multiplier;
}

public static class Mouse
{
    public static Vector2 Position => Input.mousePosition;
    public static Vector2 LockedPosition => Cursor.lockState switch
    {
        CursorLockMode.None => Input.mousePosition,
        CursorLockMode.Locked => Game.MainCamera.Camera.ViewportToScreenPoint(new Vector2(0.5f, 0.5f)),
        CursorLockMode.Confined => Input.mousePosition,
        _ => Input.mousePosition,
    };
    public static float DeltaX => Input.GetAxisRaw("Mouse X");
    public static float DeltaY => Input.GetAxisRaw("Mouse Y");
    public static Vector2 Delta => new(DeltaX, DeltaY);
    public static float ScrollDelta => Input.mouseScrollDelta.y;
    public static bool IsPresent => Input.mousePresent;

    public const int Left = 0;
    public const int Right = 1;
    public const int Middle = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDown(int button) => Input.GetMouseButtonDown(button);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUp(int button) => Input.GetMouseButtonUp(button);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHold(int button) => Input.GetMouseButton(button);
}

public static class MouseAlt
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
        Input.GetKey(KeyCode.KeypadMinus) ||
        Input.GetKey(KeyCode.Period) ||
        Input.GetKey(KeyCode.Minus);

    public static float ScrollDelta
    {
        get
        {
            float result = 0f;

            if (Input.GetKey(KeyCode.KeypadPlus))
            { result += -1f; }

            if (Input.GetKey(KeyCode.KeypadMinus))
            { result += 1f; }

            if (Input.GetKey(KeyCode.Period))
            { result += -1f; }

            if (Input.GetKey(KeyCode.Minus))
            { result += 1f; }

            return Math.Clamp(result, -1f, 1f);
        }
    }
}
