using System.Collections;
using System.IO;

using UnityEngine;

namespace AssetManager
{
    using DataUtilities.ReadableFileFormat;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    [Serializable]
    public class DirectoryNotExistsException : System.Exception
    {
        public DirectoryNotExistsException(DirectoryInfo directory) : base($"Directory \"{directory}\" does not exist!") { }
        public DirectoryNotExistsException(string directory) : base($"Directory \"{directory}\" does not exist!") { }
        public DirectoryNotExistsException(DirectoryInfo directory, System.Exception inner) : base($"Directory \"{directory}\" does not exist!", inner) { }
        public DirectoryNotExistsException(string directory, System.Exception inner) : base($"Directory \"{directory}\" does not exist!", inner) { }
        protected DirectoryNotExistsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class SingletonNotExistException<T> : System.Exception
    {
        public SingletonNotExistException() : base($"Singleton {typeof(T)} does not exist!") { }
        public SingletonNotExistException(string message) : base(message) { }
        public SingletonNotExistException(string message, System.Exception inner) : base(message, inner) { }
        protected SingletonNotExistException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class AssetBundleException : System.Exception
    {
        public AssetBundleException() { }
        public AssetBundleException(string message) : base(message) { }
        public AssetBundleException(string message, System.Exception inner) : base(message, inner) { }
        protected AssetBundleException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    internal readonly struct CachedAsset<TType>
    {
        [SerializeField] internal readonly string Path;
        [SerializeField] internal readonly TType Data;

        public CachedAsset(string path, TType data)
        {
            Path = path;
            Data = data;
        }
    }

    static class Extensions
    {
        internal static bool TryGetAsset<T>(this System.Collections.Generic.IEnumerable<CachedAsset<T>> self, string path, out T result)
        {
            using (System.Collections.Generic.IEnumerator<CachedAsset<T>> eumerator = self.GetEnumerator())
            {
                while (eumerator.MoveNext())
                {
                    CachedAsset<T> asset = eumerator.Current;
                    if (asset.Path == path)
                    {
                        result = asset.Data;
                        return true;
                    }
                }
            }
            result = default;
            return false;
        }

        internal static T AddOrModifyComponent<T>(this GameObject self) where T : Component
        {
            if (self.TryGetComponent<T>(out T result)) return result;
            return self.AddComponent<T>();
        }

        internal static Component AddOrModifyComponent(this GameObject self, System.Type type)
        {
            if (self.TryGetComponent(type, out Component result)) return result;
            return self.AddComponent(type);
        }

        public static Value TryOverrideWithFile(this Value v, System.Func<string, DataUtilities.FilePacker.IFile> finder, System.Func<DataUtilities.FilePacker.IFile, Value> parser)
        {
            switch (v.Type)
            {
                case DataUtilities.ReadableFileFormat.ValueType.LITERAL:
                    {
                        var foundFile = finder?.Invoke(v.String + ".sdf");
                        if (foundFile == null)
                        {
                            AssetLogger.LogError($"File \"{v.String + ".sdf"}\" not found at ?:{v.Location}");
                            return v;
                        }
                        var newV = parser?.Invoke(foundFile);
                        if (newV == null)
                        {
                            AssetLogger.LogError($"Unknown error while loading file {foundFile.FullName}");
                            return v;
                        }
                        return newV.Value;
                    }
                case DataUtilities.ReadableFileFormat.ValueType.OBJECT:
                default:
                    return v;
            }
        }
        public static Value TryOverrideWithFile(this Value v, System.Func<string, DataUtilities.FilePacker.IFile> finder, System.Func<DataUtilities.FilePacker.IFile, Value?> parser) => TryOverrideWithFile(v, finder, (f) => parser.Invoke(f) ?? v);
    }

    internal static class Utils
    {
        public static IEnumerator FixNetcodeObject(GameObject gameObject)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            gameObject.GetComponent<Unity.Netcode.NetworkObject>().Destroy();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            gameObject.AddComponent<Unity.Netcode.NetworkObject>();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            gameObject.GetComponent<Unity.Netcode.NetworkObject>().Spawn(true);
        }

        public static Sprite LoadSprite(DataUtilities.FilePacker.IFile file, int pixelsPerUnit = 1, FilterMode filterMode = FilterMode.Point)
        {
            if (file == null)
            {
                Debug.LogError("File is null");
                return null;
            }

            Texture2D texture = AssetManager.LoadTexture(file);
            texture.filterMode = filterMode;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), pixelsPerUnit);
        }

        public static string ReadableSize(int size)
        {
            string[] sizes = new string[] { "B", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return System.String.Format("{0:0.##} {1}", len, sizes[order]);
        }

        public static string ReadableSize(float size)
        {
            string[] sizes = new string[] { "B", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return System.String.Format("{0:0.##} {1}", len, sizes[order]);
        }

        public static bool IsChildOf(GameObject child, GameObject potentialParent)
        {
            GameObject current = child;

            while (current != potentialParent)
            {
                if (current.transform.parent == null)
                { return false; }
                current = current.transform.parent.gameObject;
            }

            return true;
        }

        public static string GetObjectPath(GameObject parent, GameObject child)
        {
            string result = "";

            if (IsChildOf(child, parent))
            {
                GameObject current = child;
                while (current != parent)
                {
                    string name = current.name;
                    if (name.StartsWith("__")) name = name[2..];
                    result = name + '/' + result;
                    current = current.transform.parent.gameObject;
                }
            }
            else
            {
                GameObject current = parent;
                while (current != child)
                {
                    result = "../";
                    current = current.transform.parent != null ? current.transform.parent.gameObject : null;
                    if (current == null)
                    { return null; }
                }
            }

            if (result.StartsWith('/'))
            { result = result[1..]; }

            if (result.EndsWith('/'))
            { result = result[..^1]; }

            if (string.IsNullOrEmpty(result) && child != parent)
            { Debug.LogWarning($"Child \"{child}\" not found in \"{parent}\"", parent); }

            return result;
        }

        public static GameObject GetObjectByPath(GameObject root, string path)
        {
            GameObject current = root;
            string[] segments = path.Split('/');
            foreach (var segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                { continue; }
                if (segment == "..")
                {
                    current = current.transform.parent.gameObject;
                }
                else
                {
                    bool found = false;
                    for (int i = 0; i < current.transform.childCount; i++)
                    {
                        if (current.transform.GetChild(i).name == segment)
                        {
                            current = current.transform.GetChild(i).gameObject;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        Debug.LogError($"Child object \"{segment}\" not found in object {current}", current);
                        return null;
                    }
                }
            }
            return current;
        }
    }

    internal static class DataValueUtils
    {
        public static bool IsList(object o)
        {
            if (o == null) return false;
            return
                o is IList &&
                o.GetType().IsGenericType &&
                o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        public static bool IsList(Type type)
        {
            if (type == null) return false;
            return
                type.IsGenericType &&
                type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        public static string FindBuiltinPrefab(GameObject @object)
        {
            for (int i = AssetManager.Instance.BuiltinPrefabs.Count - 1; i >= 0; i--)
            {
                if (AssetManager.Instance.BuiltinPrefabs[i].Value == @object)
                {
                    return $"__{AssetManager.Instance.BuiltinPrefabs[i].Key}";
                }
            }
            return null;
        }

        public static GameObject FindBuiltinPrefab(string name)
        {
            if (AssetManager.Instance.BuiltinPrefabs.TryGetValue(name, out GameObject prefab))
            { return prefab; }
            return null;
        }

        public static Value? SaveValue(GameObject gameObject, object v)
        {
            if (v is int) return Value.Literal((int)v);
            if (v is float) return Value.Literal((float)v);
            if (v is bool) return Value.Literal((bool)v);
            if (v is string) return Value.Literal((string)v);

            if (v is LayerMask) return Value.Literal(((LayerMask)v).value);

            if (v is GameObject)
            {
                string builtinPrefabName = FindBuiltinPrefab((GameObject)v);
                if (!string.IsNullOrEmpty(builtinPrefabName))
                { return Value.Literal(builtinPrefabName); }

                return Value.Literal($"{Utils.GetObjectPath(gameObject, (GameObject)v)}");
            }

            if (v is Transform) return Value.Literal($"{Utils.GetObjectPath(gameObject, ((Transform)v).gameObject)}:{((Transform)v).GetType().FullName}");

            if (v is Component) return Value.Literal($"{Utils.GetObjectPath(gameObject, ((Component)v).gameObject)}:{((Component)v).GetType().FullName}");

            if (v.GetType().IsArray)
            {
                Array array = (Array)v;

                Value result = Value.Object();
                int i1 = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    Value? value = SaveValue(gameObject, array.GetValue(i));
                    if (value.HasValue)
                    { result[(i1++).ToString()] = value.Value; }
                }
                return result;
            }

            if (v is IList)
            {
                Value result = Value.Object();
                IList list = (IList)v;
                int i1 = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    var value = SaveValue(gameObject, list[i]);
                    if (value.HasValue)
                    { result[(i1++).ToString()] = value.Value; }
                }
                return result;
            }

            Debug.LogWarning($"Unknown type {v.GetType()}");
            return null;
        }

        public static object LoadValue(GameObject gameObject, Type t, Value v)
        {
            if (t == typeof(int))
            { return v.Int ?? 0; }

            if (t == typeof(float))
            { return v.Float ?? 0f; }

            if (t == typeof(string))
            { return v.String; }

            if (t == typeof(bool))
            { return v.Bool ?? false; }

            if (t == typeof(LayerMask))
            { return (LayerMask)(v.Int ?? 0); }

            if (t == typeof(GameObject))
            {
                if (string.IsNullOrWhiteSpace(v.String))
                {
                    Debug.LogError($"Expected string literal as a GameObject ID ({v.Path})");
                    return null;
                }

                if (v.String.StartsWith("__"))
                {
                    GameObject builtinPrefab = FindBuiltinPrefab(v.String[2..]);
                    if (builtinPrefab != null)
                    { return builtinPrefab; }

                    Debug.LogError($"Builtin prefab \"{v.String[2..]}\" not found ({v.Path})");
                    return null;
                }

                GameObject childObject = Utils.GetObjectByPath(gameObject, v.String);
                if (childObject == null)
                {
                    Debug.LogError($"Child object \"{v.String}\" not found ({v.Path})");
                    return null;
                }

                return childObject;
            }

            if (t == typeof(Transform))
            {
                if (string.IsNullOrWhiteSpace(v.String))
                {
                    Debug.LogError($"Expected string literal as a Transform ID ({v.Path})");
                    return null;
                }
                if (!v.String.Contains(':'))
                {
                    Debug.LogError($"Invalid component reference format \"{v.String}\" ({v.Path})");
                    return null;
                }
                string objectPath = v.String.Split(':')[0] ?? "";
                string componentType = v.String[(objectPath.Length + 1)..];
                if (string.IsNullOrWhiteSpace(componentType))
                {
                    Debug.LogError($"Invalid component reference format (componentType is null) \"{v.String}\" ({v.Path})");
                    return null;
                }
                if (componentType != "UnityEngine.Transform")
                {
                    Debug.LogWarning($"Wat");
                }
                GameObject childObj = Utils.GetObjectByPath(gameObject, objectPath);
                if (childObj == null)
                {
                    Debug.LogError($"Child object \"{objectPath}\" not found");
                    return null;
                }
                if (t != typeof(Transform))
                {
                    Debug.LogWarning($"Field type ({t.Name}) is not {typeof(Transform)}");
                }
                return childObj.transform;
            }

            if (t.IsSubclassOf(typeof(Component)))
            {
                if (string.IsNullOrWhiteSpace(v.String))
                {
                    Debug.LogError($"Expected string literal as a {t.GetType().Name} ID ({v.Path})");
                    return null;
                }
                if (!v.String.Contains(':'))
                {
                    Debug.LogError($"Invalid component reference format \"{v.String}\" ({v.Path})");
                    return null;
                }
                string objectPath = v.String.Split(':')[0] ?? "";
                string componentType = v.String[(objectPath.Length + 1)..];
                if (string.IsNullOrWhiteSpace(componentType))
                {
                    Debug.LogError($"Invalid component reference format (componentType is null) \"{v.String}\" ({v.Path})");
                    return null;
                }

                GameObject childObject = Utils.GetObjectByPath(gameObject, objectPath);
                if (childObject == null)
                {
                    Debug.LogError($"Child object \"{objectPath}\" not found ({v.Path})");
                    return null;
                }
                if (!childObject.TryGetComponent(componentType, out Component component1))
                {
                    Debug.LogError($"Component \"{componentType}\" not found in object \"{childObject.name}\" ({v.Path})", childObject);
                    return null;
                }
                if (t != component1.GetType())
                {
                    // Debug.LogWarning($"Referenced component ({component1.GetType()}) is differ from field type ({t.Name})");
                }
                return component1;
            }

            Type[] unityTypes = UnityThingLoaders.GetLoadableObjects();
            for (int i = 0; i < unityTypes.Length; i++)
            {
                if (unityTypes[i] == t)
                { return UnityThingLoaders.LoadObject(unityTypes[i], v); }
            }

            if (t.IsArray)
            {
                var elementType = t.GetElementType();
                var elementNames = v.ChildNames;
                Array result = Array.CreateInstance(elementType, elementNames.Count);
                int currentIndex = 0;
                foreach (var i in elementNames)
                {
                    if (!int.TryParse(i, out _))
                    {
                        Debug.LogWarning($"Invalid index \"{i}\" ({v.Path})");
                        continue;
                    }
                    var elementValue = LoadValue(gameObject, elementType, v[i]);
                    if (elementValue == null) continue;
                    result.SetValue(elementValue, currentIndex++);
                }
                return result;
            }

            if (ReflectionUtility.IsList(t))
            {
                var elementType = ReflectionUtility.GetCollectionElementType(t);
                var elementNames = v.ChildNames;
                IList result = (IList)Activator.CreateInstance(t);
                foreach (var i in elementNames)
                {
                    if (!int.TryParse(i, out _))
                    {
                        Debug.LogWarning($"Invalid index \"{i}\" ({v.Path})");
                        continue;
                    }
                    var elementValue = LoadValue(gameObject, elementType, v[i]);
                    if (elementValue == null) continue;
                    result.Add(elementValue);
                }
                return result;
            }

            Debug.LogWarning($"Unknown type {t}");
            return null;
        }

        public static T LoadValue<T>(GameObject gameObject, Value v)
            => (T)LoadValue(gameObject, typeof(T), v);
    }

    internal static class ComponentUtils
    {
        public static Component AddComponent(GameObject prefab, Type component)
        {
            Attribute attribute = Attribute.GetCustomAttribute(component, typeof(RequireComponent));
            if (attribute != null && attribute is RequireComponent requireComponent)
            {
                if (requireComponent.m_Type0 != null &&
                    !prefab.HasComponent(requireComponent.m_Type0))
                { AddComponent(prefab, requireComponent.m_Type0); }

                if (requireComponent.m_Type1 != null &&
                    !prefab.HasComponent(requireComponent.m_Type1))
                { AddComponent(prefab, requireComponent.m_Type1); }

                if (requireComponent.m_Type2 != null &&
                    !prefab.HasComponent(requireComponent.m_Type2))
                { AddComponent(prefab, requireComponent.m_Type2); }
            }

            return prefab.AddOrModifyComponent(component);
        }
        public static void LoadComponent(GameObject prefab, Component component, Value data)
        {
            System.Reflection.FieldInfo[] fields = ReflectionUtility.GetMembers(
                component.GetType(),
                typeof(MonoBehaviour),
                t => t.GetFields(ReflectionUtility.Flags.AllInstance)
                    .Where(prop => prop.IsDefined(typeof(AssetFieldAttribute), false)));

            System.Reflection.PropertyInfo[] properties = ReflectionUtility.GetMembers(
                component.GetType(),
                typeof(MonoBehaviour),
                t => t.GetProperties(ReflectionUtility.Flags.AllInstance)
                    .Where(prop => prop.IsDefined(typeof(AssetFieldAttribute), false)));

            foreach (System.Reflection.FieldInfo field in fields)
            {
                string n = field.Name;

                if (data.TryGetNode(n, out Value dataValue))
                { }
                else if (data.TryGetNode(n.ToLower(), out dataValue))
                { }
                else
                { continue; }

                Type t = Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
                field.SetValue(component, DataValueUtils.LoadValue(prefab, t, dataValue));
            }

            foreach (System.Reflection.PropertyInfo property in properties)
            {
                string n = property.Name;

                if (data.TryGetNode(n, out Value dataValue))
                { }
                else if (data.TryGetNode(n.ToLower(), out dataValue))
                { }
                else
                { continue; }

                Type t = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                property.SetValue(component, DataValueUtils.LoadValue(prefab, t, dataValue));
            }

            if (component is ICanLoadAsset canLoadAsset)
            { canLoadAsset.LoadAsset(data); }
        }
    }

    static class AssetBundleLoader
    {
        internal static IEnumerator LoadAsset<T>(this AssetBundle bundle, string path, Action<T> callback) where T : UnityEngine.Object
        {
            if (AssetManager.EnableDebugLogging) Debug.Log($"Load asset '{path}' ...");

            AssetBundleRequest assetRequest = bundle.LoadAssetAsync<T>(path);
            yield return assetRequest;
            T result = assetRequest.asset as T;

            if (result == null)
            {
                Debug.LogError("Failed to load asset!");
                yield break;
            }

            if (AssetManager.EnableDebugLogging) Debug.Log($"Asset loaded");
            callback?.Invoke(result);
        }

        internal static void LoadAsset<T>(this DirectoryInfo folder, string path, Action<T> callback) where T : UnityEngine.Object
        {
            callback?.Invoke(Resources.Load<T>(Path.Join(folder.FullName, path)));
        }

        internal static AssetBundle LoadBundle(string name)
        {
            if (AssetManager.EnableDebugLogging) Debug.Log($"Load bundle '{name}' ...");

            string path = Path.Combine(Application.streamingAssetsPath, name);
            if (AssetManager.EnableDebugLogging) Debug.Log($"Bundle path: {path}");

            var assetBundle = AssetBundle.LoadFromFile(path);

            if (assetBundle == null)
            {
                Debug.LogError("Failed to load AssetBundle!");
                throw new AssetBundleException($"Failed to load asset bundle '{name}'!");
            }
            else
            {
                if (AssetManager.EnableDebugLogging) Debug.Log($"Bundle loaded");
                return assetBundle;
            }
        }
    }

    /*
    public class PhotonPrefabTool : Photon.Pun.IPunPrefabPool
    {
        /// <summary>Returns an inactive instance of a networked GameObject, to be used by PUN.</summary>
        /// <param name="prefabId">String identifier for the networked object.</param>
        /// <param name="position">Location of the new object.</param>
        /// <param name="rotation">Rotation of the new object.</param>
        /// <returns></returns>
        public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
        {
            GameObject instance = null;

            if (true)
            {
                GameObject objectInMemory = Resources.Load<GameObject>(prefabId);
                if (objectInMemory != null)
                {
                    bool wasActive = objectInMemory.activeSelf;
                    if (wasActive) objectInMemory.SetActive(false);
                    instance = GameObject.Instantiate(objectInMemory, position, rotation);
                    if (wasActive) objectInMemory.SetActive(true);
                }
            }

            if (instance == null)
            {
                instance = AssetManager.InstantiatePrefab(prefabId, false);
                if (instance != null) instance.transform.SetPositionAndRotation(position, rotation);
            }

            if (instance == null) throw new System.Exception($"Failed to load prefab {prefabId}");

            return instance;
        }

        /// <summary>Simply destroys a GameObject.</summary>
        /// <param name="gameObject">The GameObject to get rid of.</param>
        public void Destroy(GameObject gameObject)
        {
            GameObject.Destroy(gameObject);
        }
    }
    */

    /*
    public class NetcodePrefabPool2 : Unity.Netcode.INetworkPrefabInstanceHandler
    {
        /// <summary>
        /// Invoked on Client and Server Once an implementation is registered with the NetworkPrefabHandler, this method will be called when a Network Prefab associated NetworkObject is: Server Side: destroyed or despawned with the destroy parameter equal to true If Despawn(Boolean) is invoked with the default destroy parameter (i.e. false) then this method will NOT be invoked! Client Side: destroyed when the client receives a destroy object message from the server or host. Note on Pooling: When this method is invoked, you do not need to destroy the NetworkObject as long as you want your pool to persist. The most common approach is to make the NetworkObject inactive by calling .
        /// </summary>
        /// <param name="networkObject">The NetworkObject being destroyed</param>
        public void Destroy(Unity.Netcode.NetworkObject networkObject)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Client Side Only Once an implementation is registered with the NetworkPrefabHandler, this method will be called every time a Network Prefab associated NetworkObject is spawned on clients Note On Hosts: Use the RegisterHostGlobalObjectIdHashValues(GameObject, List\<GameObject>) method to register all targeted NetworkPrefab overrides manually since the host will be acting as both a server and client.
        ///
        /// Note on Pooling: If you are using a NetworkObject pool, don't forget to make the NetworkObject active via the method.
        /// </summary>
        /// <param name="ownerClientId">the owner for the NetworkObject to be instantiated</param>
        /// <param name="position">the initial/default position for the NetworkObject to be instantiated</param>
        /// <param name="rotation">the initial/default rotation for the NetworkObject to be instantiated</param>
        /// <returns></returns>
        public Unity.Netcode.NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            throw new System.NotImplementedException();
        }
    }
    */

    namespace LoadedAssets
    {
        static class Extensions
        {
            internal static Sprite Sprite(this Value self, FolderLoader loader, int pixelsPerUnit = 1, FilterMode filterMode = FilterMode.Point)
            {
                if (self.String == null)
                {
                    Debug.LogWarning("File null not found");
                    return null;
                }
                var file = loader.GetFile(self.String);
                if (file == null)
                {
                    Debug.LogWarning($"File {self.String} not found");
                    return null;
                }
                return Utils.LoadSprite(file, pixelsPerUnit, filterMode);
            }
            internal static Vector2 Vector2(this Value self) => new(self["x"].Float ?? self["X"].Float ?? 0f, self["y"].Float ?? self["Y"].Float ?? 0f);
            internal static TEnum? Enum<TEnum>(this Value self) where TEnum : struct
            {
                if (self.String == null) return null;
                if (string.IsNullOrWhiteSpace(self.String)) return null;
                if (!System.Enum.TryParse(self.String ?? "", true, out TEnum result)) return null;
                return result;
            }
            internal static TEnum Enum<TEnum>(this Value self, TEnum @default) where TEnum : struct => self.Enum<TEnum>() ?? @default;
        }
    }
}