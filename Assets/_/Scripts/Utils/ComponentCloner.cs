using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

public static class ComponentCloner
{
    static T[] GetMembers<T>(Type type, Type stopAt, Func<Type, IEnumerable<T>> memberSearcher, int depth = 0)
    {
        if (depth > 5)
        { throw new Exception($"Inherit depth exceed"); }

        List<T> result = new();

        if (type == stopAt)
        {
            return result.ToArray();
        }

        result.AddRange(memberSearcher.Invoke(type));

        result.AddRange(GetMembers(type.BaseType, stopAt, memberSearcher, depth + 1));

        return result.ToArray();
    }

    public static T GetCopyOf<T>(this Component self, T other) where T : Component
    {
        Type type = self.GetType();
        if (type != other.GetType())
        { throw new Exception("Type mis-match"); }

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        
        PropertyInfo[] properties = GetMembers(type, typeof(Component), t => t.GetProperties(flags));

        foreach (var property in properties)
        {
            if (property.CanWrite)
            {
                try
                { property.SetValue(self, property.GetValue(other)); }
                catch { }
            }
        }

        FieldInfo[] fields = GetMembers(type, typeof(Component), t => t.GetFields(flags));

        foreach (var field in fields)
        {
            field.SetValue(self, field.GetValue(other));
        }

        return self as T;
    }

    public static T AddComponent<T>(this GameObject self, T toAdd)
        where T : Component
        => self.AddComponent<T>().GetCopyOf(toAdd);

    public static T AddOrModifyComponent<T>(this GameObject self, T toAdd)
        where T : Component
        => self.TryGetComponent(out T c) ? c.GetCopyOf(toAdd) : self.AddComponent<T>().GetCopyOf(toAdd);
}
