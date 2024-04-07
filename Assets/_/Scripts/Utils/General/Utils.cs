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

    public static implicit operator KeyValuePair<TKey, TValue>(Pair<TKey, TValue> pair) => new(pair.Key, pair.Value);
    public static implicit operator Pair<TKey, TValue>(KeyValuePair<TKey, TValue> pair) => new(pair.Key, pair.Value);
}

public static class ListExtensions
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

    public static string ToReadableString<T>(this IEnumerable<T> self)
    {
        StringBuilder builder = new();
        self.ToReadableString(builder);
        return builder.ToString();
    }
    public static StringBuilder ToReadableString<T>(this IEnumerable<T> self, StringBuilder builder)
    {
        if (self == null)
        {
            builder.Append("null");
            return builder;
        }

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

        return builder;
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

public static class WindowsAPI
{
    public enum Cursor : int
    {
        StandardArrow = 32512,
        IBeam = 32513,
        Hourglass = 32514,
        Crosshair = 32515,
        VerticalArrow = 32516,
        [Obsolete(" Obsolete for applications marked version 4.0 or later. Use FourPointedArrowPointingNorthSouthEastAndWest")]
        Size = 32640,
        [Obsolete("Obsolete for applications marked version 4.0 or later.")]
        Icon = 32641,
        DoublePointedArrowPointingNorthwestAndSoutheast = 32642,
        DoublePointedArrowPointingNortheastAndSouthwest = 32643,
        DoublePointedArrowPointingWestAndEast = 32644,
        DoublePointedArrowPointingNorthAndSouth = 32645,
        FourPointedArrowPointingNorthSouthEastAndWest = 32646,
        SlashedCircle = 32648,
        Hand = 32649,
        StandardArrowAndSmallHourglass = 32650,
        ArrowAndQuestionMark = 32651
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
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void SetCursor(Cursor cursor) => throw new PlatformNotSupportedException("Win32 APIs not supported on the current platform");

#endif
}
