using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

using System.Runtime.CompilerServices;
using Unity.Netcode;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Netcode
{
    public static class Config
    {
        public const int SerializationRate = 30;
    }

    public struct NetworkString : INetworkSerializable
    {
        Collections.FixedString32Bytes info;
        public const int maxLength = 29;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref info);
        }

        public override string ToString()
        {
            return info.ToString();
        }

        public static implicit operator string(NetworkString s) => s.ToString();
        public static implicit operator NetworkString(string s) => new() { info = new Collections.FixedString32Bytes(s ?? "") };
    }
}

internal struct Triangle
{
    /// <summary>
    /// Thank you <see href="https://forum.unity.com/threads/closest-point-on-mesh-collider.34660/"/>
    /// </summary>
    public static Vector3 NearestPoint(Vector3 pt, (Vector3 a, Vector3 b, Vector3 c) triangle)
        => NearestPoint(pt, triangle.a, triangle.b, triangle.c);
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
        public static int Targeting => LayerMask.GetMask(LayerMaskNames.Default, LayerMaskNames.Ground);
        /// <summary>
        /// <see cref="LayerMaskNames.Default"/> ; <see cref="LayerMaskNames.Projectile"/>
        /// </summary>
        public static int PossiblyDamagables => LayerMask.GetMask(LayerMaskNames.Default, LayerMaskNames.Projectile);
    }

    internal static class Utils
    {
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
    }

    internal struct Line
    {
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
        internal static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 bulletPosition, float projectileVelocity, float projectileAcceleration)
        {
            float distance;
            float time = 0f;

            int iterations = 3;
            for (int i = 0; i < iterations; i++)
            {
                distance = Vector2.Distance(bulletPosition, targetPosition + (targetVelocity * time));
                float speedAfterThis = SpeedAfterDistance(projectileVelocity, projectileAcceleration, distance);
                time = TimeToReachVelocity(projectileVelocity, speedAfterThis, projectileAcceleration);
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
        internal static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 bulletPosition, float projectileVelocity)
        {
            float distance;
            float time = 0f;

            int iterations = 3;
            for (int i = 0; i < iterations; i++)
            {
                distance = Vector2.Distance(bulletPosition, targetPosition + (targetVelocity * time));
                time = CalculateTime(distance, projectileVelocity);
            }

            return targetVelocity * time;
        }
        /// <returns>Aim offset</returns>
        internal static Vector2 CalculateInterceptCourse(Vector2 targetPosition, Vector2 targetVelocity, Vector2 bulletPosition, float projectileVelocity, Math.Circle circle)
        {
            float p = 1 / projectileVelocity;

            float distance = Vector2.Distance(bulletPosition, targetPosition);
            float time = distance * p;

            distance = Vector2.Distance(bulletPosition, circle.GetPointAfterTime(targetVelocity.magnitude, time, circle.GetAngle(targetPosition)));
            time = distance * p;

            distance = Vector2.Distance(bulletPosition, circle.GetPointAfterTime(targetVelocity.magnitude, time, circle.GetAngle(targetPosition)));
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
        static readonly Vector4[] s_UnitSquare =
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
            int len = s_UnitSphere.Length / 3;
            for (int i = 0; i < len; i++)
            {
                var sX = pos + radius * v[0 * len + i];
                var eX = pos + radius * v[0 * len + (i + 1) % len];
                var sY = pos + radius * v[1 * len + i];
                var eY = pos + radius * v[1 * len + (i + 1) % len];
                var sZ = pos + radius * v[2 * len + i];
                var eZ = pos + radius * v[2 * len + (i + 1) % len];
                Debug.DrawLine(sX, eX, color);
                Debug.DrawLine(sY, eY, color);
                Debug.DrawLine(sZ, eZ, color);
            }
        }

        internal static void DrawSphere(Vector4 pos, float radius, Color color, float duration)
        {
            Vector4[] v = s_UnitSphere;
            int len = s_UnitSphere.Length / 3;
            for (int i = 0; i < len; i++)
            {
                var sX = pos + radius * v[0 * len + i];
                var eX = pos + radius * v[0 * len + (i + 1) % len];
                var sY = pos + radius * v[1 * len + i];
                var eY = pos + radius * v[1 * len + (i + 1) % len];
                var sZ = pos + radius * v[2 * len + i];
                var eZ = pos + radius * v[2 * len + (i + 1) % len];
                Debug.DrawLine(sX, eX, color, duration);
                Debug.DrawLine(sY, eY, color, duration);
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

    internal delegate bool InputConditionEnabler();

    internal class AdvancedMouse
    {
        internal delegate void OnDragEvent(Vector2 start, Vector2 current);
        internal delegate void OnDraggedEvent(Vector2 start, Vector2 end);
        internal delegate void OnClickEvent(Vector2 position);
        internal delegate void OnDownEvent(Vector2 position);

        internal readonly int ButtonID;

        internal event OnDragEvent OnDrag;
        internal event OnDraggedEvent OnDragged;
        internal event OnClickEvent OnClick;
        internal event OnDownEvent OnDown;

        internal Vector2 DraggingStartPosition => PositionBeforeDrag;
        internal bool IsDragging => Drag && !ClickedOnUI;

        /// <summary>
        /// This must be squared!
        /// </summary>
        internal const float DRAG_THRESHOLD = 5f;

        readonly InputConditionEnabler conditionEnabler;

        Vector2 PositionBeforeDrag;
        bool Drag;

        bool ClickedOnUI;

        static Vector2 Position => Input.mousePosition;

        public AdvancedMouse(int buttonId)
        {
            this.ButtonID = buttonId;
            this.conditionEnabler = null;
        }

        public AdvancedMouse(int buttonId, InputConditionEnabler conditionEnabler)
        {
            this.ButtonID = buttonId;
            this.conditionEnabler = conditionEnabler;
        }

        internal void Update()
        {
            if (conditionEnabler != null && !conditionEnabler.Invoke())
            {
                Reset();
                return;
            }

            if (Input.GetMouseButtonDown(ButtonID))
            {
                PositionBeforeDrag = Position;
                Drag = false;

                ClickedOnUI = MouseManager.IsPointerOverUI(PositionBeforeDrag);

                if (!ClickedOnUI) OnDown?.Invoke(PositionBeforeDrag);
            }
            else if (Input.GetMouseButtonUp(ButtonID))
            {
                if (Drag)
                {
                    if (!ClickedOnUI) OnDragged?.Invoke(PositionBeforeDrag, Position);
                }
                else
                {
                    if (!ClickedOnUI) OnClick?.Invoke(Position);
                }

                PositionBeforeDrag = Position;
                Drag = false;
            }
            else if (Input.GetMouseButton(ButtonID))
            {
                if (!Drag && (new Vector2(Position.x, Position.y) - PositionBeforeDrag).sqrMagnitude > DRAG_THRESHOLD)
                { Drag = true; }

                if (Drag)
                {
                    if (!ClickedOnUI) OnDrag?.Invoke(PositionBeforeDrag, Position);
                }
            }
        }

        internal void Reset()
        {
            this.PositionBeforeDrag = Vector2.zero;
            this.ClickedOnUI = false;
            this.Drag = false;
        }
    }

    internal class AdvancedPriorityMouse : AdvancedMouse
    {
        internal readonly int Priority;

        public AdvancedPriorityMouse(int buttonId, int priority) : base(buttonId)
        {
            this.Priority = priority;
        }

        public AdvancedPriorityMouse(int buttonId, int priority, InputConditionEnabler conditionEnabler) : base(buttonId, conditionEnabler)
        {
            this.Priority = priority;
        }
    }

    internal class AdvancedPriorityMouseComparer : Comparer<AdvancedPriorityMouse>
    {
        public override int Compare(AdvancedPriorityMouse a, AdvancedPriorityMouse b)
            => Comparer.Default.Compare(b.Priority, a.Priority);
    }

    [Serializable]
    internal class PriorityKey : IComparable<PriorityKey>
    {
        internal delegate void KeyEvent();

        readonly InputConditionEnabler ConditionEnabler;

        [SerializeField, ReadOnly] internal KeyCode Key;
        [SerializeField, ReadOnly] internal int Priority;
        internal event KeyEvent OnDown;
        internal event KeyEvent OnHold;
        internal event KeyEvent OnUp;

        public PriorityKey(KeyCode key, int priority)
        {
            this.Key = key;
            this.Priority = priority;
            this.ConditionEnabler = null;
            KeyboardManager.Register(this);
        }

        public PriorityKey(KeyCode key, int priority, InputConditionEnabler conditionEnabler) : this(key, priority)
        {
            this.ConditionEnabler = conditionEnabler;
        }

        internal bool Update()
        {
            if (ConditionEnabler != null && !ConditionEnabler.Invoke())
            { return false; }

            bool eee = false;

            if (OnDown != null && Input.GetKeyDown(Key))
            {
                OnDown.Invoke();
                eee = true;
            }

            if (OnHold != null && Input.GetKey(Key))
            {
                OnHold.Invoke();
                eee = true;
            }

            if (OnUp != null && Input.GetKeyUp(Key))
            {
                OnUp.Invoke();
                eee = true;
            }

            return eee;
        }

        public int CompareTo(PriorityKey other)
            => Comparer.Default.Compare(other.Priority, this.Priority);
    }

    internal class PriorityKeyComparer : Comparer<PriorityKey>
    {
        public override int Compare(PriorityKey a, PriorityKey b)
            => a.CompareTo(b);
    }

    internal static class MouseButton
    {
        internal const int Left = 0;
        internal const int Right = 1;
        internal const int Middle = 2;
    }

    internal struct AI
    {
        class PriorityComparer<T> : IComparer<(T, float)>
        {
            public int Compare((T, float) a, (T, float) b)
                => Comparer.Default.Compare(a.Item2, b.Item2);
        }

        public delegate float GetPriority<T>(T @object);

        public static T[] SortTargets<T>(T[] targets, GetPriority<T> getPriority) where T : UnityEngine.Object
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

            return priorities.Select(v => v.Item1).ToArray();
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

        public static BaseObject[] SortTargets(BaseObject[] targets, Vector3 origin, string team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.Team, team));

        public static void SortTargets(IList<BaseObject> targets, Vector3 origin, string team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.Team, team));

        public static BaseObject[] SortTargets(BaseObject[] targets, Vector3 origin, int team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.TeamHash, team));

        public static void SortTargets(IList<BaseObject> targets, Vector3 origin, int team)
            => SortTargets(targets, target => (origin - target.transform.position).sqrMagnitude * TeamManager.Instance.GetFuckYou(target.TeamHash, team));
    }
}

[Serializable]
public struct CursorConfig
{
    public Texture2D Texture;
    public Vector2 Hotspot;
    public CursorMode Mode;

    public readonly void SetCursor() => UnityEngine.Cursor.SetCursor(Texture, Hotspot, Mode);
}

internal static class ObjectGroups
{
    static Transform game;
    static Transform effects;
    static Transform projectiles;

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
}

internal static class TheTerrain
{
    static Terrain terrain;

    internal static Terrain Terrain
    {
        get
        {
            if (terrain == null)
            { terrain = GameObject.FindObjectOfType<Terrain>(); }
            return terrain;
        }
    }

    internal static Vector3 Height(Vector3 position)
        => new(position.x, Terrain.SampleHeight(position) + Terrain.transform.position.y, position.z);
}

internal static class NetcodeUtils
{
    public static bool FindNetworkObject(ulong id, out NetworkObject networkObject)
    {
        if (NetworkManager.Singleton == null)
        {
            networkObject = null;
            return false;
        }
        if (!NetworkManager.Singleton.IsListening)
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

        if (typeof(System.Collections.IList).IsAssignableFrom(type))
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
        if (typeof(System.Collections.IDictionary).IsAssignableFrom(type))
            return typeof(System.Collections.DictionaryEntry);

        // if it's a list we look for an Item property with an int index parameter
        // where the property type is anything but object
        if (typeof(System.Collections.IList).IsAssignableFrom(type))
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
        if (typeof(System.Collections.ICollection).IsAssignableFrom(type))
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
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
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