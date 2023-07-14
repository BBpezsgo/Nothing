using DataUtilities.ReadableFileFormat;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace AssetManager
{
    internal static class UnityThingLoaders
    {
        static readonly List<UnityThingLoader> unityThingLoaders = new()
        {
            new Vector2Loader(),
            new Vector3Loader(),
            new BoundsLoader(),
            new BoxColliderLoader(),
            new RigidbodyLoader(),
            new NetworkObjectLoader(),
            new NetworkTransformLoader(),
        };

        public static Type[] GetLoadableComponents()
        {
            List<Type> result = new();
            foreach (var loader in unityThingLoaders)
            {
                if (loader is UnityComponentLoader componentLoader)
                {
                    result.Add(componentLoader.LoaderType);
                }
            }
            return result.ToArray();
        }
        public static Type[] GetLoadableObjects()
        {
            List<Type> result = new();
            foreach (var loader in unityThingLoaders)
            {
                if (loader is UnityObjectLoader objectLoader)
                {
                    result.Add(objectLoader.LoaderType);
                }
            }
            return result.ToArray();
        }

        public static Value SaveObject<T>(this T value)
        {
            UnityObjectLoader<T> loader = GetObjectLoader<T>();
            return loader.Save(value);
        }

        public static T LoadObject<T>(this Value data)
            => LoadObject<T>(data, default);

        public static T LoadObject<T>(this Value data, T @default)
        {
            UnityObjectLoader<T> loader = GetObjectLoader<T>();
            return loader.Load(data, @default);
        }

        public static UnityObjectLoader<T> GetObjectLoader<T>()
        {
            foreach (var loader in unityThingLoaders)
            {
                if (loader is UnityObjectLoader<T> loader2)
                {
                    return loader2;
                }
            }
            Debug.LogWarning($"Loader {typeof(UnityObjectLoader<T>).Name} not found");
            return null;
        }

        public static UnityComponentLoader<T> GetComponentLoader<T>() where T : Component
        {
            foreach (var loader in unityThingLoaders)
            {
                if (loader is UnityComponentLoader<T> loader2)
                {
                    return loader2;
                }
            }
            Debug.LogWarning($"Loader {typeof(UnityComponentLoader<T>).Name} not found");
            return null;
        }

        public static object LoadObject(Type type, Value value)
        {
            UnityObjectLoader loader = GetObjectLoader(type);
            return loader.Load(value);
        }

        public static UnityObjectLoader GetObjectLoader(Type type)
        {
            foreach (var loader in unityThingLoaders)
            {
                if (loader.LoaderType == type && loader is UnityObjectLoader loader2)
                {
                    return loader2;
                }
            }
            Debug.LogWarning($"Loader {typeof(UnityThingLoader).Name} ({type}) not found");
            return null;
        }

        public static void AddComponent<T>(GameObject @object, Value data) where T : Component
        {
            UnityComponentLoader<T> loader = GetComponentLoader<T>();
            T newComp = @object.AddComponent<T>();
            loader.Load(@object, newComp, data);
        }

        public static void AddOrModifyComponent<T>(GameObject @object, Value data) where T : Component
        {
            UnityComponentLoader<T> loader = GetComponentLoader<T>();
            T newComp = @object.AddOrModifyComponent<T>();
            loader.Load(@object, newComp, data);
        }

        public static Value SaveComponent(Type type, Component component)
        {
            UnityComponentLoader loader = GetComponentLoader(type);
            return loader.Save(component);
        }

        public static Value SaveComponent<T>(this T value) where T : Component
        {
            UnityComponentLoader<T> loader = GetComponentLoader<T>();
            return loader.Save(value);
        }

        public static UnityComponentLoader GetComponentLoader(Type type)
        {
            foreach (var loader in unityThingLoaders)
            {
                if (loader.LoaderType == type && loader is UnityComponentLoader loader2)
                {
                    return loader2;
                }
            }
            Debug.LogWarning($"Loader {typeof(UnityComponentLoader).Name} ({type}) not found");
            return null;
        }
    }
}
