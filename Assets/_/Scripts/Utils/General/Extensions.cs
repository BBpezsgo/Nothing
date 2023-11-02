using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

#nullable enable

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

    public static T? PeekOrDefault<T>(this Stack<T> stack)
        => stack.TryPeek(out T result) ? result : default;

    public static T? GetOrDefault<T>(this IReadOnlyList<T> v, int index, T? @default = default)
    {
        if (index < 0 || index >= v.Count)
        { return @default; }
        return v[index];
    }
}

public static partial class ListEx
{
    public static bool TryGetValue<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> v, TKey key, out TValue? value)
        where TKey : IEquatable<TKey>
    {
        foreach (KeyValuePair<TKey, TValue> pair in v)
        {
            if ((IEquatable<TKey>)pair.Key == (IEquatable<TKey>)key)
            {
                value = pair.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    public static TValue? Get<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> v, TKey key)
        where TKey : IEquatable<TKey>
    {
        foreach (KeyValuePair<TKey, TValue> pair in v)
        {
            if ((IEquatable<TKey>)pair.Key == (IEquatable<TKey>)key)
            {
                return pair.Value;
            }
        }
        return default;
    }

    public static bool TryGetValue<TKey, TValue>(this IEnumerable<Pair<TKey, TValue>> v, TKey key, out TValue? value)
        where TKey : IEquatable<TKey>
    {
        foreach (Pair<TKey, TValue> pair in v)
        {
            if (pair.Key.Equals(key))
            {
                value = pair.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    public static bool ContainsValue<TKey, TValue>(this IEnumerable<Pair<TKey, TValue>> v, TValue value)
        where TValue : IEquatable<TValue>
    {
        foreach (Pair<TKey, TValue> pair in v)
        {
            if (pair.Value.Equals(value))
            {
                return true;
            }
        }
        return false;
    }

    public static TValue? Get<TKey, TValue>(this IEnumerable<Pair<TKey, TValue>> v, TKey key)
        where TKey : IEquatable<TKey>
    {
        foreach (Pair<TKey, TValue> pair in v)
        {
            if ((IEquatable<TKey>)pair.Key == (IEquatable<TKey>)key)
            {
                return pair.Value;
            }
        }
        return default;
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

    public static void Push<T>(this IList<T> v, T value) => v.Add(value);

    /// <exception cref="InvalidOperationException"/>
    public static T Pop<T>(this IList<T> v)
    {
        if (v.Count > 0)
        {
            var result = v[^1];
            v.RemoveAt(v.Count - 1);
            return result;
        }
        throw new InvalidOperationException($"Stack is empty");
    }
    public static T? PopOrDefault<T>(this IList<T> v, T? @default = default)
    {
        if (v.Count > 0)
        {
            var result = v[^1];
            v.RemoveAt(v.Count - 1);
            return result;
        }
        return @default;
    }
    public static bool TryPop<T>(this IList<T> v, out T? popped)
    {
        if (v.Count > 0)
        {
            popped = v[^1];
            v.RemoveAt(v.Count - 1);
            return true;
        }
        popped = default;
        return false;
    }
}

public static class DataChunk
{
    public static T[][] Chunks<T>(this T[] v, int chunkSize)
    {
        int chunkCount = (int)MathF.Ceiling((float)v.Length / (float)chunkSize);

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
