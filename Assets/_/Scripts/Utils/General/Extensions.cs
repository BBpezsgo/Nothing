using System;
using System.Collections;
using System.Collections.Generic;

public static partial class UnclassifiedExtensions
{
    public static float GetProgress(this System.Threading.Tasks.Task task) => task?.Status switch
    {
        System.Threading.Tasks.TaskStatus.Created => 0f,
        System.Threading.Tasks.TaskStatus.WaitingForActivation => .1f,
        System.Threading.Tasks.TaskStatus.WaitingToRun => .2f,
        System.Threading.Tasks.TaskStatus.Running => .5f,
        System.Threading.Tasks.TaskStatus.WaitingForChildrenToComplete => .75f,
        System.Threading.Tasks.TaskStatus.Faulted or System.Threading.Tasks.TaskStatus.Canceled or System.Threading.Tasks.TaskStatus.RanToCompletion => 1f,
        _ => 0f,
    };

    public static T? PeekOrNull<T>(this Stack<T> stack) where T : struct
        => stack.TryPeek(out T result) ? result : null;

    public static T PeekOrDefault<T>(this Stack<T> stack)
        => stack.TryPeek(out T result) ? result : default;

    public static T Get<T>(this IReadOnlyList<T> v, int index, T @default)
    {
        if (index < 0 || index >= v.Count)
        { return @default; }
        return v[index];
    }

    public static bool Contains(this string[] self, string v, StringComparison comparison = StringComparison.InvariantCulture)
    {
        for (int i = 0; i < self.Length; i++)
        {
            if (string.Equals(self[i], v, comparison)) return true;
        }
        return false;
    }

    public static bool Contains<T>(this T[] self, T v) where T : IEquatable<T>
    {
        for (int i = 0; i < self.Length; i++)
        {
            if ((IEquatable<T>)self[i] == (IEquatable<T>)v) return true;
        }
        return false;
    }

    public delegate void SearchCallback<T1>(T1 p0);
    public delegate void SearchCallback<T1, T2>(T1 p0, T2 p1);
}

public static partial class ListEx
{
    public static void Enqueue<T>(this ICollection<T> v, T element)
    {
        v.Add(element);
    }

    public static T Dequeue<T>(this IList<T> v)
    {
        T element = v[0];
        v.RemoveAt(0);
        return element;
    }

    public static bool TryGetValue<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> v, TKey key, out TValue value)
        where TKey : IEquatable<TKey>
    {
        foreach (var pair in v)
        {
            if ((IEquatable<TKey>)pair.Key == (IEquatable<TKey>)key)
            {
                value = pair.Value;
                return true;
            }
        }
        value = default(TValue);
        return false;
    }

    public static TValue Get<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> v, TKey key)
        where TKey : IEquatable<TKey>
    {
        foreach (var pair in v)
        {
            if ((IEquatable<TKey>)pair.Key == (IEquatable<TKey>)key)
            {
                return pair.Value;
            }
        }
        return default(TValue);
    }

    public static bool TryGetValue<TKey, TValue>(this IEnumerable<Pair<TKey, TValue>> v, TKey key, out TValue value)
        where TKey : IEquatable<TKey>
    {
        foreach (var pair in v)
        {
            if (pair.Key.Equals(key))
            {
                value = pair.Value;
                return true;
            }
        }
        value = default(TValue);
        return false;
    }

    public static bool ContainsValue<TKey, TValue>(this IEnumerable<Pair<TKey, TValue>> v, TValue value)
        where TValue : IEquatable<TValue>
    {
        foreach (var pair in v)
        {
            if (pair.Value.Equals(value))
            {
                return true;
            }
        }
        return false;
    }

    public static TValue Get<TKey, TValue>(this IEnumerable<Pair<TKey, TValue>> v, TKey key)
        where TKey : IEquatable<TKey>
    {
        foreach (var pair in v)
        {
            if ((IEquatable<TKey>)pair.Key == (IEquatable<TKey>)key)
            {
                return pair.Value;
            }
        }
        return default(TValue);
    }

    public static void AddOrModify<TKey, TValue>(this Dictionary<TKey, TValue> v, TKey key, TValue value)
    {
        if (v.ContainsKey(key))
        {
            v[key] = value;
        }
        else
        {
            v.Add(key, value);
        }
    }

    public static void Set<TKey, TValue>(this List<KeyValuePair<TKey, TValue>> v, TKey key, TValue value)
            where TKey : IEquatable<TKey>
    {
        for (int i = 0; i < v.Count; i++)
        {
            KeyValuePair<TKey, TValue> pair = v[i];
            if ((IEquatable<TKey>)pair.Key == (IEquatable<TKey>)key)
            {
                v[i] = new KeyValuePair<TKey, TValue>(key, value);
            }
        }
        v.Add(new KeyValuePair<TKey, TValue>(key, value));
    }

    public static T[] Purge<T>(this T[] v) where T : class
    {
        List<T> result = new(v);
        result.Purge();
        return result.ToArray();
    }
    public static void Purge<T>(this IList<T> v) where T : class
    {
        for (int i = v.Count - 1; i >= 0; i--)
        { if (v[i] == null) v.RemoveAt(i); }
    }

    public static void RemoveLast(this IList v)
    { if (v.Count > 0) v.RemoveAt(v.Count - 1); }
    public static T Pop<T>(this IList<T> v) where T : class
    {
        if (v.Count > 0)
        {
            var result = v[^1];
            v.RemoveAt(v.Count - 1);
            return result;
        }
        return null;
    }
    public static bool Pop<T>(this IList<T> v, out T popped)
    {
        if (v.Count > 0)
        {
            popped = v[^1];
            v.RemoveAt(v.Count - 1);
            return true;
        }
        popped = default(T);
        return false;
    }
}

public static class DataChunk
{
    public static T[][] Chunks<T>(this T[] v, int chunkSize)
    {
        int chunkCount = (int)MathF.Ceiling(((float)v.Length) / ((float)chunkSize));

        T[][] result = new T[chunkCount][];

        for (int i = 0; i < v.Length; i += chunkSize)
        {
            int currentChunkSize = Math.Min(chunkSize, v.Length - i);
            result[i / chunkSize] = new T[currentChunkSize];
            Array.Copy(v, i, result[i / chunkSize], 0, currentChunkSize);
        }

        return result;
    }
}
