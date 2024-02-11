using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Internal;

#nullable enable

public struct Maths
{
    /// <summary>
    /// The well-known 3.14159265358979... value (Read Only).
    /// </summary>
    public const float PI = MathF.PI;

    /// <summary>
    /// A representation of positive infinity (Read Only).
    /// </summary>
    public const float Infinity = float.PositiveInfinity;

    /// <summary>
    /// A representation of negative infinity (Read Only).
    /// </summary>
    public const float NegativeInfinity = float.NegativeInfinity;

    /// <summary>
    /// Degrees-to-radians conversion constant (Read Only).
    /// </summary>
    public const float Deg2Rad = MathF.PI / 180f;

    /// <summary>
    /// Radians-to-degrees conversion constant (Read Only).
    /// </summary>
    public const float Rad2Deg = 57.29578f;

    /// <summary>
    /// A tiny floating point value (Read Only).
    /// </summary>
    public static readonly float Epsilon = (UnityEngineInternal.MathfInternal.IsFlushToZeroEnabled ? UnityEngineInternal.MathfInternal.FloatMinNormal : UnityEngineInternal.MathfInternal.FloatMinDenormal);

    /// <summary>
    /// Returns the closest power of two value.
    /// </summary>
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern int ClosestPowerOfTwo(int value);

    /// <summary>
    /// Returns true if the value is power of two.
    /// </summary>
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern bool IsPowerOfTwo(int value);

    /// <summary>
    /// Returns the next power of two that is equal to, or greater than, the argument.
    /// </summary>
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern int NextPowerOfTwo(int value);

    /// <summary>
    /// Converts the given value from gamma (sRGB) to linear color space.
    /// </summary>
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern float GammaToLinearSpace(float value);

    /// <summary>
    /// Converts the given value from linear to gamma (sRGB) color space.
    /// </summary>
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern float LinearToGammaSpace(float value);

    /// <summary>
    /// Convert a color temperature in Kelvin to RGB color.
    /// </summary>
    /// <param name="kelvin">
    /// Temperature in Kelvin. Range 1000 to 40000 Kelvin.
    /// </param>
    /// <returns>
    /// Correlated Color Temperature as floating point RGB color.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color CorrelatedColorTemperatureToRGB(float kelvin) => Mathf.CorrelatedColorTemperatureToRGB(kelvin);

    /// <summary>
    /// Encode a floating point value into a 16-bit representation.
    /// </summary>
    /// <param name="val">
    /// The floating point value to convert.
    /// </param>
    /// <returns>
    /// The converted half-precision float, stored in a 16-bit unsigned integer.
    /// </returns>
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern ushort FloatToHalf(float val);

    /// <summary>
    /// Convert a half precision float to a 32-bit floating point value.
    /// </summary>
    /// <param name="val">
    /// The half precision value to convert.
    /// </param>
    /// <returns>
    /// The decoded 32-bit float.
    /// </returns>
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern float HalfToFloat(ushort val);

    /// <summary>
    /// Generate 2D Perlin noise.
    /// </summary>
    /// <returns>
    /// Value between 0.0 and 1.0. (Return value might be slightly below 0.0 or beyond
    /// 1.0.)
    /// </returns>
    [MethodImpl(MethodImplOptions.InternalCall)]
    public static extern float PerlinNoise(float x, float y);

    /// <summary>
    /// Returns the sine of angle f.
    /// </summary>
    /// <param name="f">
    /// The input angle, in radians.
    /// </param>
    /// <returns>
    /// The return value between -1 and +1.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sin(float f) => MathF.Sin(f);

    /// <summary>
    /// Returns the cosine of angle f.
    /// </summary>
    /// <param name="f">
    /// The input angle, in radians.
    /// </param>
    /// <returns>
    /// The return value between -1 and 1.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cos(float f) => MathF.Cos(f);

    /// <summary>
    /// Returns the tangent of angle f in radians.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Tan(float f) => MathF.Tan(f);

    //
    /// <summary>
    /// Returns the arc-sine of f - the angle in radians whose sine is f.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Asin(float f) => MathF.Asin(f);

    /// <summary>
    /// Returns the arc-cosine of f - the angle in radians whose cosine is f.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Acos(float f) => MathF.Acos(f);

    /// <summary>
    /// Returns the arc-tangent of f - the angle in radians whose tangent is f.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Atan(float f) => MathF.Atan(f);

    /// <summary>
    /// Returns the angle in radians whose Tan is y/x.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Atan2(float y, float x) => MathF.Atan2(y, x);

    /// <summary>
    /// Returns square root of f.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sqrt(float f) => MathF.Sqrt(f);

    /// <summary>
    /// Returns the absolute value of f.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Abs(float f) => MathF.Abs(f);

    /// <summary>
    /// Returns the absolute value of value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Abs(int value) => Math.Abs(value);

    /// <summary>
    /// Returns the smallest of two or more values.
    /// </summary>
    public static float Min(float a, float b) => (a < b) ? a : b;

    /// <summary>
    /// Returns the smallest of two or more values.
    /// </summary>
    public static float Min(params float[] values)
    {
        int n = values.Length;
        if (n == 0) return 0f;

        float result = values[0];
        for (int i = 1; i < n; i++)
        {
            if (values[i] < result)
            {
                result = values[i];
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the smallest of two or more values.
    /// </summary>
    public static int Min(int a, int b) => (a < b) ? a : b;

    /// <summary>
    /// Returns the smallest of two or more values.
    /// </summary>
    public static int Min(params int[] values)
    {
        int n = values.Length;
        if (n == 0) return 0;

        int result = values[0];
        for (int i = 1; i < n; i++)
        {
            if (values[i] < result)
            {
                result = values[i];
            }
        }

        return result;
    }

    /// <summary>
    /// Returns largest of two or more values.
    /// </summary>
    public static float Max(float a, float b) => (a > b) ? a : b;

    /// <summary>
    /// Returns largest of two or more values.
    /// </summary>
    public static float Max(params float[] values)
    {
        int n = values.Length;
        if (n == 0) return 0f;

        float result = values[0];
        for (int i = 1; i < n; i++)
        {
            if (values[i] > result)
            {
                result = values[i];
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the largest of two or more values.
    /// </summary>
    public static int Max(int a, int b) => (a > b) ? a : b;

    /// <summary>
    /// Returns the largest of two or more values.
    /// </summary>
    public static int Max(params int[] values)
    {
        int n = values.Length;
        if (n == 0) return 0;

        int result = values[0];
        for (int i = 1; i < n; i++)
        {
            if (values[i] > result)
            {
                result = values[i];
            }
        }

        return result;
    }

    /// <summary>
    /// Returns f raised to power p.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Pow(float f, float p) => MathF.Pow(f, p);

    /// <summary>
    /// Returns e raised to the specified power.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Exp(float power) => MathF.Exp(power);

    /// <summary>
    /// Returns the logarithm of a specified number in a specified base.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log(float f, float p) => MathF.Log(f, p);

    /// <summary>
    /// Returns the natural (base e) logarithm of a specified number.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log(float f) => MathF.Log(f);

    /// <summary>
    /// Returns the base 10 logarithm of a specified number.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Log10(float f) => MathF.Log10(f);

    /// <summary>
    /// Returns the smallest integer greater to or equal to f.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Ceil(float f) => MathF.Ceiling(f);

    /// <summary>
    /// Returns the largest integer smaller than or equal to f.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Floor(float f) => MathF.Floor(f);

    /// <summary>
    /// Returns f rounded to the nearest integer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Round(float f) => MathF.Round(f);

    /// <summary>
    /// Returns the smallest integer greater to or equal to f.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CeilToInt(float f) => (int)MathF.Ceiling(f);

    /// <summary>
    /// Returns the largest integer smaller to or equal to f.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FloorToInt(float f) => (int)MathF.Floor(f);

    /// <summary>
    /// Returns f rounded to the nearest integer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RoundToInt(float f) => (int)MathF.Round(f);

    /// <summary>
    /// Returns the sign of f.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sign(float f) => (f >= 0f) ? 1f : (-1f);

    /// <summary>
    /// Clamps the given value between the given minimum float and maximum float values.
    /// Returns the given value if it is within the minimum and maximum range.
    /// </summary>
    /// <param name="value">
    /// The floating point value to restrict inside the range defined by the minimum
    /// and maximum values.
    /// </param>
    /// <param name="min">
    /// The minimum floating point value to compare against.
    /// </param>
    /// <param name="max">
    /// The maximum floating point value to compare against.
    /// </param>
    /// <returns>
    /// The float result between the minimum and maximum values.
    /// </returns>
    public static float Clamp(float value, float min, float max)
    {
        if (value < min)
        { value = min; }
        else if (value > max)
        { value = max; }
        return value;
    }

    /// <summary>
    /// Clamps the given value between a range defined by the given minimum integer and
    /// maximum integer values. Returns the given value if it is within min and max.
    /// </summary>
    /// <param name="value">
    /// The integer point value to restrict inside the min-to-max range.
    /// </param>
    /// <param name="min">
    /// The minimum integer point value to compare against.
    /// </param>
    /// <param name="max">
    /// The maximum integer point value to compare against.
    /// </param>
    /// <returns>
    /// The int result between min and max values.
    /// </returns>
    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
        { value = min; }
        else if (value > max)
        { value = max; }
        return value;
    }

    /// <summary>
    /// Clamps value between 0 and 1 and returns value.
    public static float Clamp01(float value)
    {
        if (value < 0f)
        { return 0f; }

        if (value > 1f)
        { return 1f; }

        return value;
    }

    /// <summary>
    /// Linearly interpolates between a and b by t.
    /// </summary>
    public static float Lerp(float a, float b, float t) => a + ((b - a) * Math.Clamp(t, 0f, 1f));

    /// <summary>
    /// Linearly interpolates between a and b by t with no limit to t.
    /// </summary>
    public static float LerpUnclamped(float a, float b, float t) => a + ((b - a) * t);

    /// <summary>
    /// Same as Lerp but makes sure the values interpolate correctly when they wrap around
    /// 360 degrees.
    /// </summary>
    public static float LerpAngle(float a, float b, float t)
    {
        float num = Repeat(b - a, 360f);
        if (num > 180f)
        {
            num -= 360f;
        }

        return a + num * Math.Clamp(t, 0f, 1f);
    }

    /// <summary>
    /// Moves a value current towards target.
    /// </summary>
    public static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
        {
            return target;
        }

        return current + MathF.Sign(target - current) * maxDelta;
    }

    /// <summary>
    /// Same as MoveTowards but makes sure the values interpolate correctly when they
    /// wrap around 360 degrees.
    /// </summary>
    public static float MoveTowardsAngle(float current, float target, float maxDelta)
    {
        float num = DeltaAngle(current, target);
        if (0f - maxDelta < num && num < maxDelta)
        {
            return target;
        }

        target = current + num;
        return MoveTowards(current, target, maxDelta);
    }

    /// <summary>
    /// Interpolates between min and max with smoothing at the limits.
    /// </summary>
    public static float SmoothStep(float from, float to, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        t = -2f * t * t * t + 3f * t * t;
        return to * t + from * (1f - t);
    }

    public static float Gamma(float value, float absMax, float gamma)
    {
        bool flag = value < 0f;
        float num = MathF.Abs(value);
        if (num > absMax)
        {
            return flag ? (0f - num) : num;
        }

        float num2 = MathF.Pow(num / absMax, gamma) * absMax;
        return flag ? (0f - num2) : num2;
    }

    /// <summary>
    /// Compares two floating point values and returns true if they are similar.
    /// </summary>
    public static bool Approximately(float a, float b)
    {
        return MathF.Abs(b - a) < MathF.Max(1E-06f * MathF.Max(MathF.Abs(a), MathF.Abs(b)), Epsilon * 8f);
    }

    public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed)
    {
        float deltaTime = Time.deltaTime;
        return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
    }

    public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime)
    {
        float deltaTime = Time.deltaTime;
        float maxSpeed = float.PositiveInfinity;
        return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
    }

    public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, [DefaultValue("Mathf.Infinity")] float maxSpeed, [DefaultValue("Time.deltaTime")] float deltaTime)
    {
        smoothTime = MathF.Max(0.0001f, smoothTime);
        float num = 2f / smoothTime;
        float num2 = num * deltaTime;
        float num3 = 1f / (1f + num2 + 0.48f * num2 * num2 + 0.235f * num2 * num2 * num2);
        float value = current - target;
        float num4 = target;
        float num5 = maxSpeed * smoothTime;
        value = Clamp(value, 0f - num5, num5);
        target = current - value;
        float num6 = (currentVelocity + num * value) * deltaTime;
        currentVelocity = (currentVelocity - num * num6) * num3;
        float num7 = target + (value + num6) * num3;
        if (num4 - current > 0f == num7 > num4)
        {
            num7 = num4;
            currentVelocity = (num7 - num4) / deltaTime;
        }

        return num7;
    }

    public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed)
    {
        float deltaTime = Time.deltaTime;
        return SmoothDampAngle(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
    }

    public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime)
    {
        float deltaTime = Time.deltaTime;
        float maxSpeed = float.PositiveInfinity;
        return SmoothDampAngle(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
    }

    public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime, [DefaultValue("Mathf.Infinity")] float maxSpeed, [DefaultValue("Time.deltaTime")] float deltaTime)
    {
        target = current + DeltaAngle(current, target);
        return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
    }

    /// <summary>
    /// Loops the value t, so that it is never larger than length and never smaller than 0.
    /// </summary>
    public static float Repeat(float t, float length) => Math.Clamp(t - (MathF.Floor(t / length) * length), 0f, length);

    /// <summary>
    /// PingPong returns a value that will increment and decrement between the value
    /// 0 and length.
    /// </summary>
    public static float PingPong(float t, float length)
    {
        t = Repeat(t, length * 2f);
        return length - MathF.Abs(t - length);
    }

    /// <summary>
    /// Determines where a value lies between two points.
    /// </summary>
    //
    /// Parameters:
    ///   a:
    /// The start of the range.
    //
    ///   b:
    /// The end of the range.
    //
    ///   value:
    /// The point within the range you want to calculate.
    //
    /// Returns:
    /// A value between zero and one, representing where the "value" parameter falls
    /// within the range defined by a and b.
    public static float InverseLerp(float a, float b, float value)
    {
        if (a != b)
        {
            return Math.Clamp((value - a) / (b - a), 0f, 1f);
        }

        return 0f;
    }

    /// <summary>
    /// Calculates the shortest difference between two given angles given in degrees.
    /// </summary>
    public static float DeltaAngle(float current, float target)
    {
        float num = Repeat(target - current, 360f);
        if (num > 180f)
        { num -= 360f; }
        return num;
    }

    internal static bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
    {
        float num = p2.x - p1.x;
        float num2 = p2.y - p1.y;
        float num3 = p4.x - p3.x;
        float num4 = p4.y - p3.y;
        float num5 = num * num4 - num2 * num3;
        if (num5 == 0f)
        {
            return false;
        }

        float num6 = p3.x - p1.x;
        float num7 = p3.y - p1.y;
        float num8 = (num6 * num4 - num7 * num3) / num5;
        result.x = p1.x + num8 * num;
        result.y = p1.y + num8 * num2;
        return true;
    }

    internal static bool LineSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
    {
        float num = p2.x - p1.x;
        float num2 = p2.y - p1.y;
        float num3 = p4.x - p3.x;
        float num4 = p4.y - p3.y;
        float num5 = num * num4 - num2 * num3;
        if (num5 == 0f)
        {
            return false;
        }

        float num6 = p3.x - p1.x;
        float num7 = p3.y - p1.y;
        float num8 = (num6 * num4 - num7 * num3) / num5;
        if (num8 < 0f || num8 > 1f)
        {
            return false;
        }

        float num9 = (num6 * num2 - num7 * num) / num5;
        if (num9 < 0f || num9 > 1f)
        {
            return false;
        }

        result.x = p1.x + num8 * num;
        result.y = p1.y + num8 * num2;
        return true;
    }

    internal static long RandomToLong(System.Random r)
    {
        byte[] array = new byte[8];
        r.NextBytes(array);
        return (long)(BitConverter.ToUInt64(array, 0) & 0x7FFFFFFFFFFFFFFFL);
    }

    #region Custom

    public static float QuadraticEquation(float a, float b, float c, float sign)
    {
        float discriminant = (b * b) - (4 * a * c);
        return (-b + sign * Maths.Sqrt(discriminant)) / (2 * a);
    }
    public static (float, float) QuadraticEquation(float a, float b, float c)
    {
        float discriminant = (b * b) - (4 * a * c);
        float dSrt = Maths.Sqrt(discriminant);
        float x1 = (-b + dSrt) / (2 * a);
        float x2 = (-b - dSrt) / (2 * a);

        return (x1, x2);
    }
    public static float Sum(params float[] values)
    {
        float sum = 0f;
        for (int i = 0; i < values.Length; i++)
        { sum += values[i]; }
        return sum;
    }

    public static float Average(params float[] values) => Sum(values) / values.Length;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Average(float a, float b) => (a + b) / 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Difference(float a, float b) => Maths.Abs(a - b);
    public static Vector2 Difference(Vector2 a, Vector2 b) => new(
        Difference(a.x, b.x),
        Difference(a.y, b.y)
    );
    public static Vector3 Difference(Vector3 a, Vector3 b) => new(
        Difference(a.x, b.x),
        Difference(a.y, b.y),
        Difference(a.z, b.z)
    );

    public static Vector3 Mult(Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);

    public struct Circle
    {
        public Vector2 center;
        public float radius;

        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public override readonly string ToString()
            => $"Circle{{ Center: ({center.x}, {center.y}) radius: {radius} }}";

        /// <param name="angle">Angle in radians</param>
        public readonly Vector2 GetPoint(float angle)
        {
            float x = this.radius * Maths.Cos(angle) + this.center.x;
            float y = this.radius * Maths.Sin(angle) + this.center.y;
            return new Vector2(x, y);
        }

        /// <param name="angleOffset">Angle in radians</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 GetPointAfterTime(float speed, float time, float angleOffset)
            => GetPoint(GetAngle(speed, time) + (angleOffset));

        public readonly float GetAngle(Vector2 pointOnCircle)
            => Maths.Atan2(pointOnCircle.y - this.center.y, pointOnCircle.x - this.center.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetAngle(float speed, float time)
            => GetAngle(speed * time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetAngle(float distance)
            => distance / this.radius;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Circumference(float radius)
            => Maths.PI * 2 * radius;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Circumference()
            => Maths.PI * 2 * radius;

        public static Vector2[] GenerateEquadistancePoints(int n, float radius)
        {
            List<Vector2> points = new();

            for (int i = 0; i < n; i++)
            {
                var k = i + .5f;
                var r = Maths.Sqrt((k) / n);
                var theta = Maths.PI * (1 + Maths.Sqrt(5)) * k;
                var x = r * Maths.Cos(theta) * radius;
                var y = r * Maths.Sin(theta) * radius;
                points.Add(new Vector2(x, y));
            }

            return points.ToArray();
        }
    }

    public static float IsStraightLine(Vector2 positionA, Vector2 positionB, Vector2 positionC)
        => (positionA.x * (positionB.y - positionC.y) + positionB.x * (positionC.y - positionA.y) + positionC.x * (positionA.y - positionB.y)) / 2;

    public static Circle FindCircle(Vector2 positionA, Vector2 positionB, Vector2 positionC)
        => FindCircle(positionA.x, positionA.y, positionB.x, positionB.y, positionC.x, positionC.y);
    public static Circle FindCircle(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        float x12 = x1 - x2;
        float x13 = x1 - x3;

        float y12 = y1 - y2;
        float y13 = y1 - y3;

        float y31 = y3 - y1;
        float y21 = y2 - y1;

        float x31 = x3 - x1;
        float x21 = x2 - x1;

        float sx13 = Maths.Pow(x1, 2) - Maths.Pow(x3, 2);
        float sy13 = Maths.Pow(y1, 2) - Maths.Pow(y3, 2);
        float sx21 = Maths.Pow(x2, 2) - Maths.Pow(x1, 2);
        float sy21 = Maths.Pow(y2, 2) - Maths.Pow(y1, 2);

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

        float c = -Maths.Pow(x1, 2) - Maths.Pow(y1, 2) - 2 * g * x1 - 2 * f * y1;
        float h = g * -1;
        float k = f * -1;
        float sqr_of_r = h * h + k * k - c;

        float r = (sqr_of_r < 0) ? 0f : Maths.Sqrt(sqr_of_r);

        return new Circle(new Vector2(h, k), r);
    }

    /// <returns>In degrees</returns>
    public static float GetAngleFromVectorFloat(Vector3 dir)
    {
        dir = dir.normalized;
        float n = Maths.Atan2(dir.y, dir.x) * Maths.Rad2Deg;
        if (n < 0) n += 360;

        return n;
    }

    /// <returns>In degrees</returns>
    public static float GetAngleFromVectorFloat(Vector2 dir)
    {
        dir = dir.normalized;
        float n = Maths.Atan2(dir.y, dir.x) * Maths.Rad2Deg;
        if (n < 0) n += 360;

        return n;
    }

    public static Vector2 RadianToVector2(float radian) => new(Maths.Cos(radian), Maths.Sin(radian));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 DegreeToVector2(float degree) => RadianToVector2(degree * Maths.Deg2Rad);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float NormalizeDegree(float degree) => (degree + 360) % 360;

    public static Vector3 LengthDir(Vector3 center, float angle, float distance)
    {
        float x = distance * Maths.Cos((90 + angle) * Maths.Deg2Rad);
        float y = distance * Maths.Sin((90 + angle) * Maths.Deg2Rad);
        Vector3 newPosition = center;
        newPosition.x += x;
        newPosition.y += y;
        return newPosition;
    }

    struct PointGroup
    {
        public int GroupID;
        public Vector2 Point1;
        public bool IsGrouped;
    }

    static PointGroup[] GeneratePointGroups(Vector2[] points)
    {
        PointGroup[] groups = new PointGroup[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            groups[i] = new PointGroup() { GroupID = i, IsGrouped = false, Point1 = points[i] };
        }
        return groups;
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
        foreach (List<Vector2> item in vector2s)
        {
            vector2s1.Add(item.ToArray());
        }
        return vector2s1.ToArray();
    }

    public static Vector2[][] GroupPoints(Vector2[] points, float tolerance)
    {
        PointGroup[] colls = GeneratePointGroups(points);
        for (int i = 0; i < colls.Length; i++)
        {
            ref PointGroup pg1 = ref colls[i];
            if (!pg1.IsGrouped)
            {
                for (int j = 0; j < colls.Length; j++)
                {
                    ref PointGroup pg2 = ref colls[j];
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

    public static (Vector2 BottomLeft, Vector2 TopRight) GetRect(Vector2 a, Vector2 b)
    {
        Vector2 lowerLeft = new(Maths.Min(a.x, b.x), Maths.Min(a.y, b.y));
        Vector2 upperRight = new(Maths.Max(a.x, b.x), Maths.Max(a.y, b.y));
        return (lowerLeft, upperRight);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Vector2 BottomLeft, Vector2 TopRight) GetRect(Transform a, Transform b)
        => GetRect(a.position, b.position);

    /// <param name="p1">Angle peak</param>
    public static float CalculateAngle(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float numerator = p2.y * (p1.x - p3.x) + p1.y * (p3.x - p2.x) + p3.y * (p2.x - p1.x);
        float denominator = (p2.x - p1.x) * (p1.x - p3.x) + (p2.y - p1.y) * (p1.y - p3.y);
        float ratio = numerator / denominator;

        float angleRad = Maths.Atan(ratio);
        float angleDeg = (angleRad * 180) / Maths.PI;

        if (angleDeg < 0)
        {
            angleDeg = 180 + angleDeg;
        }

        return angleDeg;
    }

    public static float MapToRange(float outputStart, float outputEnd, float percent)
    {
        /* Note, "slope" below is a constant for given numbers, so if you are calculating
           a lot of output values, it makes sense to calculate it once.  It also makes
           understanding the Code easier */
        var slope = outputEnd - outputStart;
        var output = outputStart + slope * percent;
        return output;
    }

    public static float MapToRange(float outputStart, float outputEnd, float inputStart, float inputEnd, float input)
    {
        /* Note, "slope" below is a constant for given numbers, so if you are calculating
           a lot of output values, it makes sense to calculate it once.  It also makes
           understanding the Code easier */
        var slope = (outputEnd - outputStart) / (inputEnd - inputStart);
        var output = outputStart + slope * (input - inputStart);
        return output;
    }

    public static (Vector3 Turret, Vector3 Barrel) TurretAim(Vector3 targetPosition, Transform turret, Transform barrel)
    {
        float targetPlaneAngle = Vector3AngleOnPlane(targetPosition - turret.position, -turret.up, turret.forward);
        Vector3 turretRotation = new(0f, targetPlaneAngle, 0f);

        float barrelAngle = Vector3.Angle(targetPosition, barrel.up);
        Vector3 barrelRotation = new(-barrelAngle + 90f, 0f, 0f);

        return (turretRotation, barrelRotation);
    }

    /// <summary>
    /// Source: <see href="https://www.youtube.com/watch?v=bCz7awDbl58"/>
    /// </summary>
    /// <param name="from">
    /// Target position
    /// </param>
    /// <param name="to">
    /// Us
    /// </param>
    /// <param name="toZeroAngle">
    /// Orientation
    /// </param>
    public static float Vector3AngleOnPlane(Vector3 from, Vector3 to, Vector3 planeNormal, Vector3 toZeroAngle)
        => Maths.Vector3AngleOnPlane(from - to, planeNormal, toZeroAngle);

    /// <summary>
    /// Source: <see href="https://www.youtube.com/watch?v=bCz7awDbl58"/>
    /// </summary>
    /// <param name="toZeroAngle">
    /// Orientation
    /// </param>
    public static float Vector3AngleOnPlane(Vector3 relativePosition, Vector3 planeNormal, Vector3 toZeroAngle)
    {
        Vector3 projectedVector = Vector3.ProjectOnPlane(relativePosition, planeNormal);
        float projectedVectorAngle = Vector3.SignedAngle(projectedVector, toZeroAngle, planeNormal);
        return projectedVectorAngle;
    }

    public static Triangle[]? GetTriangles(Mesh mesh)
    {
        if (!mesh.isReadable) return null;

        Vector3[] meshVertices = mesh.vertices;
        int[] meshTriangles = mesh.triangles;

        int triCount = meshTriangles.Length / 3;
        Triangle[] triangles = new Triangle[triCount];

        for (int i = 0; i < triCount; i++)
        {
            triangles[i] = new Triangle(
                meshVertices[meshTriangles[(i * 3) + 0]],
                meshVertices[meshTriangles[(i * 3) + 1]],
                meshVertices[meshTriangles[(i * 3) + 2]]);
        }

        return triangles;
    }

    public static float Volume(Bounds bounds) => bounds.size.sqrMagnitude;

    public static float TotalMeshVolume(GameObject @object, bool fallbackToBounds = true)
    {
        float volume = 0f;

        MeshFilter[] meshFilters = @object.GetComponentsInChildren<MeshFilter>(false);

        for (int i = 0; i < meshFilters.Length; i++)
        {
            float meshVolume = Maths.MeshVolume(meshFilters[i].mesh, fallbackToBounds);
            volume += meshVolume;
        }

        return volume;
    }

    public static float Volume(MeshFilter? meshFilter, Collider? collider)
    {
        if (meshFilter != null)
        {
            Triangle[]? triangles = Maths.GetTriangles(meshFilter.mesh);
            if (triangles != null)
            { return Maths.Volume(triangles); }
        }

        if (collider != null)
        { return Maths.Volume(collider.bounds); }

        return default;
    }

    public static float MeshVolume(MeshFilter mesh, bool fallbackToBounds = true)
        => Maths.MeshVolume(mesh.mesh, fallbackToBounds);

    public static float MeshVolume(Mesh mesh, bool fallbackToBounds = true)
    {
        if (!mesh.isReadable)
        {
            if (!fallbackToBounds)
            { return default; }

            return Maths.Volume(mesh.bounds) * .75f;
        }
        Triangle[]? triangles = Maths.GetTriangles(mesh);
        if (triangles == null) return default;
        return Maths.Volume(triangles);
    }

    public static float Volume(Triangle[] triangles)
    {
        float volumeSum = default;

        for (int i = 0; i < triangles.Length; i++)
        { volumeSum += Maths.SignedVolumeOfTriangle(triangles[i]); }

        return Math.Abs(volumeSum);
    }

    static float SignedVolumeOfTriangle(Triangle triangle)
    {
        float v321 = triangle.C.x * triangle.B.y * triangle.A.z;
        float v231 = triangle.B.x * triangle.C.y * triangle.A.z;
        float v312 = triangle.C.x * triangle.A.y * triangle.B.z;
        float v132 = triangle.A.x * triangle.C.y * triangle.B.z;
        float v213 = triangle.B.x * triangle.A.y * triangle.C.z;
        float v123 = triangle.A.x * triangle.B.y * triangle.C.z;
        return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
    }

    public static float Distance(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x;
        float y = a.y - b.y;
        float z = a.z - b.z;
        return MathF.Sqrt((x * x) + (y * y) + (z * z));
    }

    public static float Distance(Vector2 a, Vector2 b)
    {
        float x = a.x - b.x;
        float y = a.y - b.y;
        return MathF.Sqrt((x * x) + (y * y));
    }

    #endregion
}
