using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

[Serializable]
public struct Pair<TKey, TValue>
{
    public TKey Key;
    public TValue Value;

    public Pair(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }
}

namespace Utilities
{
    public static partial class GeneralUtils
    {
        /// <summary>
        /// Normalizes the angle <paramref name="a"/> between <c>[-180,180[</c>
        /// </summary>
        public static float NormalizeAngle(float a)
        {
            float angle = a % 360;
            angle = angle > 180 ? angle - 360 : angle;
            return angle;
        }

        /// <summary>
        /// Normalizes the angle <paramref name="a"/> between <c>[0,360[</c>
        /// </summary>
        public static float NormalizeAngle360(float a)
        {
            if (a < 0f)
            { a += 360f; }
            if (a >= 360f)
            { a -= 360f; }
            return a;
        }
    }
}

public static partial class ListUtils
{
    public static string ToReadableString<T>(this T[] self)
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
            builder.Append(element?.ToString() ?? "null");
        }

        builder.Append(" }");

        return builder.ToString();
    }
    public static string ToReadableString<T>(this IReadOnlyList<T> self)
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
            builder.Append(element?.ToString() ?? "null");
        }

        builder.Append(" }");

        return builder.ToString();
    }
    public static string ToReadableString<T>(this IEnumerable<T> self)
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

            builder.Append(element?.ToString() ?? "null");

            notFirst = true;
        }

        builder.Append(" }");

        return builder.ToString();
    }

    public static string ToReadableString<T1, T2>(this T1[] self, Func<T1, T2> converter)
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        for (int i = 0; i < self.Length; i++)
        {
            if (i > 0)
            { builder.Append(", "); }
            T1 element = self[i];
            T2 converted = converter.Invoke(element);
            builder.Append(converted?.ToString() ?? "null");
        }

        builder.Append(" }");

        return builder.ToString();
    }
    public static string ToReadableString<T1, T2>(this IReadOnlyList<T1> self, Func<T1, T2> converter)
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        for (int i = 0; i < self.Count; i++)
        {
            if (i > 0)
            { builder.Append(", "); }
            T1 element = self[i];
            T2 converted = converter.Invoke(element);
            builder.Append(converted?.ToString() ?? "null");
        }

        builder.Append(" }");

        return builder.ToString();
    }
    public static string ToReadableString<T1, T2>(this IEnumerable<T1> self, Func<T1, T2> converter)
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        bool notFirst = false;
        foreach (T1 element in self)
        {
            if (notFirst)
            { builder.Append(", "); }

            T2 converted = converter.Invoke(element);
            builder.Append(converted?.ToString() ?? "null");

            notFirst = true;
        }

        builder.Append(" }");

        return builder.ToString();
    }

    public static string ToReadableString<T1, T2>(this T1[] self, IReadOnlyDictionary<T1, T2> converter)
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        for (int i = 0; i < self.Length; i++)
        {
            if (i > 0)
            { builder.Append(", "); }
            T1 element = self[i];
            if (!converter.TryGetValue(element, out T2? converted))
            { converted = default; }
            builder.Append(converted?.ToString() ?? "null");
        }

        builder.Append(" }");

        return builder.ToString();
    }
    public static string ToReadableString<T1, T2>(this IReadOnlyList<T1> self, IReadOnlyDictionary<T1, T2> converter)
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        for (int i = 0; i < self.Count; i++)
        {
            if (i > 0)
            { builder.Append(", "); }
            T1 element = self[i];
            if (!converter.TryGetValue(element, out T2? converted))
            { converted = default; }
            builder.Append(converted?.ToString() ?? "null");
        }

        builder.Append(" }");

        return builder.ToString();
    }
    public static string ToReadableString<T1, T2>(this IEnumerable<T1> self, IReadOnlyDictionary<T1, T2> converter)
    {
        if (self == null)
        { return "null"; }

        StringBuilder builder = new();

        builder.Append("{ ");

        bool notFirst = false;
        foreach (T1 element in self)
        {
            if (notFirst)
            { builder.Append(", "); }

            if (!converter.TryGetValue(element, out T2? converted))
            { converted = default; }
            builder.Append(converted?.ToString() ?? "null");

            notFirst = true;
        }

        builder.Append(" }");

        return builder.ToString();
    }
}

public class WindowsAPI
{
    public enum Cursor : int
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

#if (PLATFORM_STANDALONE_WIN || UNITY_EDITOR_WIN)

    public static readonly bool IsSupported = true;

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetCursor")]
    static extern IntPtr SetCursor(
        [System.Runtime.InteropServices.In, System.Runtime.InteropServices.Optional] IntPtr cursorHandle);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "LoadCursorW")]
    static extern IntPtr LoadCursor(
        [System.Runtime.InteropServices.In, System.Runtime.InteropServices.Optional] IntPtr instanceHandle,
        [System.Runtime.InteropServices.In] IntPtr cursorName);

    /// <exception cref="PlatformNotSupportedException"/>
    public static void SetCursor(Cursor cursor) => SetCursor(LoadCursor(IntPtr.Zero, (IntPtr)cursor));

#else

    public static readonly bool IsSupported = false;

    /// <exception cref="PlatformNotSupportedException"/>
    public static void SetCursor(Cursor cursor) => throw new PlatformNotSupportedException($"Win32 APIs not supported on the current platform");

#endif
}
