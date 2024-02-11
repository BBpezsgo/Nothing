using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

#nullable enable

public static partial class UnclassifiedExtensions
{
    public static float GetProgress(this Task task) => task?.Status switch
    {
        TaskStatus.Created => 0f,
        TaskStatus.WaitingForActivation => .1f,
        TaskStatus.WaitingToRun => .2f,
        TaskStatus.Running => .5f,
        TaskStatus.WaitingForChildrenToComplete => .75f,
        TaskStatus.Faulted or TaskStatus.Canceled or TaskStatus.RanToCompletion => 1f,
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
            if (pair.Key.Equals(key))
            {
                return pair.Value;
            }
        }
        return default;
    }

    /// <exception cref="InvalidOperationException"/>
    public static T Pop<T>(this IList<T> v)
    {
        if (v.Count > 0)
        {
            T? result = v[^1];
            v.RemoveAt(v.Count - 1);
            return result;
        }
        throw new InvalidOperationException($"Stack is empty");
    }

    public static T? PopOrDefault<T>(this IList<T> v, T? @default = default)
    {
        if (v.Count > 0)
        {
            T? result = v[^1];
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
