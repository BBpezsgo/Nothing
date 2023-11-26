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

        public static Value SaveObject<T>(this T value)
        {
            UnityObjectLoader<T> loader = GetObjectLoader<T>();
            return loader.Save(value);
        }

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
    }
}
