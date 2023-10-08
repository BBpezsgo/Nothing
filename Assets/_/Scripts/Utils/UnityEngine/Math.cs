using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Internal;

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
    public static float Cos(float f) => MathF.Cos(f);

    /// <summary>
    /// Returns the tangent of angle f in radians.
    /// </summary>
    public static float Tan(float f) => MathF.Tan(f);

    //
    /// <summary>
    /// Returns the arc-sine of f - the angle in radians whose sine is f.
    /// </summary>
    public static float Asin(float f) => MathF.Asin(f);

    /// <summary>
    /// Returns the arc-cosine of f - the angle in radians whose cosine is f.
    /// </summary>
    public static float Acos(float f) => MathF.Acos(f);

    /// <summary>
    /// Returns the arc-tangent of f - the angle in radians whose tangent is f.
    /// </summary>
    public static float Atan(float f) => MathF.Atan(f);

    /// <summary>
    /// Returns the angle in radians whose Tan is y/x.
    /// </summary>
    public static float Atan2(float y, float x) => MathF.Atan2(y, x);

    /// <summary>
    /// Returns square root of f.
    /// </summary>
    public static float Sqrt(float f) => MathF.Sqrt(f);

    /// <summary>
    /// Returns the absolute value of f.
    /// </summary>
    public static float Abs(float f) => MathF.Abs(f);

    /// <summary>
    /// Returns the absolute value of value.
    /// </summary>
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
    public static float Pow(float f, float p) => MathF.Pow(f, p);

    /// <summary>
    /// Returns e raised to the specified power.
    /// </summary>
    public static float Exp(float power) => MathF.Exp(power);

    /// <summary>
    /// Returns the logarithm of a specified number in a specified base.
    /// </summary>
    public static float Log(float f, float p) => MathF.Log(f, p);

    /// <summary>
    /// Returns the natural (base e) logarithm of a specified number.
    /// </summary>
    public static float Log(float f) => MathF.Log(f);

    /// <summary>
    /// Returns the base 10 logarithm of a specified number.
    /// </summary>
    public static float Log10(float f) => MathF.Log10(f);

    /// <summary>
    /// Returns the smallest integer greater to or equal to f.
    /// </summary>
    public static float Ceil(float f) => MathF.Ceiling(f);

    /// <summary>
    /// Returns the largest integer smaller than or equal to f.
    /// </summary>
    public static float Floor(float f) => MathF.Floor(f);

    /// <summary>
    /// Returns f rounded to the nearest integer.
    /// </summary>
    public static float Round(float f) => MathF.Round(f);

    /// <summary>
    /// Returns the smallest integer greater to or equal to f.
    /// </summary>
    public static int CeilToInt(float f) => (int)MathF.Ceiling(f);

    /// <summary>
    /// Returns the largest integer smaller to or equal to f.
    /// </summary>
    public static int FloorToInt(float f) => (int)MathF.Floor(f);

    /// <summary>
    /// Returns f rounded to the nearest integer.
    /// </summary>
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

    public static float Gamma(float value, float absmax, float gamma)
    {
        bool flag = value < 0f;
        float num = MathF.Abs(value);
        if (num > absmax)
        {
            return flag ? (0f - num) : num;
        }

        float num2 = MathF.Pow(num / absmax, gamma) * absmax;
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
        {
            num -= 360f;
        }

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
}