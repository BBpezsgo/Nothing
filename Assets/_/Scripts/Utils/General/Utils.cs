using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// Normalizes the angle <paramref name="a"/> between -180 .. 180
        /// </summary>
        public static float NormalizeAngle(float a)
        {
            float angle = a % 360;
            angle = angle > 180 ? angle - 360 : angle;
            return angle;
        }

        /// <summary>
        /// Normalizes the angle <paramref name="a"/> between 0 .. 360
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

/// <summary>
/// <see href="https://www.codeproject.com/Tips/5267157/How-to-Get-a-Collection-Element-Type-Using-Reflect"/>
/// </summary>
public static partial class ReflectionUtility
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

public interface ICopiable<T> : ICopiable
{
    public void CopyTo(T destination);
}

public interface ICopiable
{
    public void CopyTo(object destination);
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
            builder.Append(element.ToString());
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
            builder.Append(element.ToString());
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

            builder.Append(element.ToString());

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
            builder.Append(converted.ToString());
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
            builder.Append(converted.ToString());
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
            builder.Append(converted.ToString());

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
            if (!converter.TryGetValue(element, out T2 converted))
            { converted = default; }
            builder.Append(converted.ToString());
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
            if (!converter.TryGetValue(element, out T2 converted))
            { converted = default; }
            builder.Append(converted.ToString());
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

            if (!converter.TryGetValue(element, out T2 converted))
            { converted = default; }
            builder.Append(converted.ToString());

            notFirst = true;
        }

        builder.Append(" }");

        return builder.ToString();
    }
}

public class WindowsAPI
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
