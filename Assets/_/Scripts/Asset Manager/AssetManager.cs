using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataUtilities.ReadableFileFormat;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace AssetManager
{
    using Components;

    using Game.Managers;

    using LoadedAssets;

    using Networking;
    using Networking.Components;

    using UI;

    using Utilities;

    public class AssetManager : MonoBehaviour
    {
        public static AssetManager Instance;

        internal static readonly bool EnableDebugLogging = true;

        [Header("Inheritance")]
        [SerializeField, Min(0)] int MaxInheritanceDepth = 4;

        [Header("Base Assets")]
        [SerializeField, ReadOnly] internal FolderLoader Assets;

        [Header("Scenes")]
        [SerializeField] string EmptyScene;
        static Scene? CurrentScene
        {
            get
            {
                if (string.IsNullOrEmpty(SceneManager.LoadedScene))
                { return null; }

                Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneManager.LoadedScene);

                if (!scene.isLoaded)
                { return null; }

                return scene;
            }
        }

        [Header("Models")]
        [SerializeField] Shader defaultShader;

        [Header("Textures")]
        [SerializeField, ReadOnly, NonReorderable] List<CachedAsset<Texture2D>> textures = new();

        [Header("Prefabs")]
        [SerializeField, ReadOnly] Transform Prefabs;
        [SerializeField] internal List<Pair<string, GameObject>> BuiltinPrefabs = new();

        [Header("Audio")]
        [SerializeField] AudioMixerGroup AudioMixerVFX;

        [Header("Debug")]
        [SerializeField] ImguiWindow window;

        [SerializeField] string testObject;
        [SerializeField] Transform SpawnAt;
        [Button(nameof(SpawnTestObject), false, true, "Spawn Test Object")]
        [SerializeField] string btnXd;
        [Button(nameof(LoadTestObject), false, true, "Load Test Object")]
        [SerializeField] string btnXd2;
        /*
        [SerializeField] string testScene;
        [Button(nameof(LoadTestScene), false, true, "Load Test Scene")]
        [SerializeField] string btnXd3;
        [SerializeField] bool AutoLoadTestScene;
        */
        [SerializeField] DownloadProgress AssetsDownloadProgress = new();
        [SerializeField, ReadOnly, NonReorderable] List<string> LoadedFromResources = new();

        [SerializeField, ReadOnly, NonReorderable] List<LoadedAssetObjectEnabler> loadedAssetObjectEnablers = new();

        [Header("Debug - Cache")]
        [SerializeField, ReadOnly] int cacheTextures;
        [Button(nameof(ClearCache), false, true, "Clear Cache")]
        [SerializeField] string btnXd4;

        [Header("Debug - Settings")]
        [SerializeField, ReadOnly] internal GameConfigManager.GameConfig settings;
        [Button(nameof(LoadConfigByUser), false, true, "Reload Config")]
        [SerializeField] string btnXd5;

        [Header("Editor - Pack Assets")]
        [SerializeField] string packAssetsPath = "C:\\Users\\bazsi\\Desktop\\Nothing Assets";
        [Button(nameof(PackAssets), true, false, "Pack Assets")]
        [SerializeField] string btnXd6;
        [SerializeField] bool TestPackedAssets;

        Transform PrefabsGroup
        {
            get
            {
                if (Prefabs != null) return Prefabs;
                Prefabs = new GameObject("Prefabs").transform;
                Prefabs.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                return Prefabs;
            }
        }

        void ClearCache()
        {
            textures.Clear();
        }

        void PackAssets()
        {
            DataUtilities.FilePacker.Packer packer = new(new DataUtilities.FilePacker.PackHeader()
            {
                SaveMetadata = false,
            });
            packer.Pack(packAssetsPath, packAssetsPath + ".bin");
        }

        void SpawnTestObject() => InstantiatePrefab(testObject, true, (SpawnAt == null) ? Vector3.up : SpawnAt.position, (SpawnAt == null) ? Quaternion.identity : SpawnAt.rotation);
        void LoadTestObject() => LoadPrefab(testObject);
        // void LoadTestScene() => LoadScene(testScene, InstantiatePrefab);


        #region Base Loader

        public void LoadIfNot()
        {
            if (Assets.IsLoaded) return;
            LoadAssets();
        }

        public Value LoadFile(string path)
            => Files.LoadFile(Assets.GetAbsoluteFile(path));
        public Value[] LoadFiles(string folder)
            => Files.LoadFiles(Assets.GetAbsoluteFolder(folder));
        public Value[] LoadFilesWithInheritance(string folder)
            => Files.LoadFilesWithInheritacne(Assets.GetAbsoluteFolder(folder), Assets.GetFile, MaxInheritanceDepth);
        public Value LoadFileWithInheritance(string path)
            => Files.LoadFileWithInheritacne(Assets.GetAbsoluteFile(path), Assets.GetFile, MaxInheritanceDepth);

        #endregion

        #region Scene Loader

        public delegate GameObject ScenePrefabSpawner(string prefabID, bool spawnOverNetwork, Vector3 position, Quaternion rotation);

        /// <exception cref="SingletonNotExistException{AssetManager}"></exception>
        public static void LoadScene(string name, ScenePrefabSpawner spawner)
        {
            if (Instance == null) throw new SingletonNotExistException<AssetManager>();

            var files = Instance.Assets.GetAllFilesEnumerable("*.scene.sdf");
            foreach (var file in files)
            {
                if (EnableDebugLogging) Debug.Log($"Load file {file.FullName} ...");
                TimeSpan loadStarted = DateTime.UtcNow.TimeOfDay;
                Value content = Parser.Parse(file.Text);
                if (EnableDebugLogging) Debug.Log($"File loaded in {(DateTime.UtcNow.TimeOfDay - loadStarted).TotalMilliseconds} ms : {file.FullName}");
                string id = content["ID"].String ?? file.Name[..^10];
                if (id != name) continue;
                content = Files.ProcessInheritance(content, file, Instance.Assets.GetFile, Instance.MaxInheritanceDepth);
                Instance.StartCoroutine(Instance.LoadScene(content, file, spawner));
                return;
            }
            AssetLogger.LogError($"Scene \"{name}\" does not exists");
        }

        System.Collections.IEnumerator LoadScene(Value content, DataUtilities.FilePacker.IFile file, ScenePrefabSpawner spawner)
        {
            string id = content["ID"].String ?? file.Name[..^10];
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == EmptyScene)
            {
                LoadedAssetObject[] spawnedObjects = GameObject.FindObjectsOfType<LoadedAssetObject>(false);
                for (int i = 0; i < spawnedObjects.Length; i++)
                {
                    spawnedObjects[i].gameObject.Destroy();
                }
            }
            else
            {
                if (EnableDebugLogging) Debug.Log($"Loading scene \"{id}\" ...");
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(EmptyScene, UnityEngine.SceneManagement.LoadSceneMode.Single);

                if (EnableDebugLogging) Debug.Log($"Scene \"{id}\" loaded");
                yield return new WaitForSecondsRealtime(1f);
            }

            if (EnableDebugLogging) Debug.Log($"Spawning objects ...");

            if (content.TryGetNode("Relationships", out Value _relationshipsArray))
            {
                var _relationships = _relationshipsArray.Array;
                if (_relationships != null)
                {
                    for (int i = 0; i < _relationships.Length; i++)
                    {
                        string a = _relationships[i]["a"].String ?? null;
                        string b = _relationships[i]["b"].String ?? null;
                        float value = _relationships[i]["value"].Float ?? 0f;
                        if (a != null && b != null && value >= uint.MinValue)
                        {
                            TeamManager.Team teamA = TeamManager.Instance.GetOrCreateTeam(a);
                            TeamManager.Team teamB = TeamManager.Instance.GetOrCreateTeam(b);
                            TeamManager.Instance.SetFuckYou(teamA.ID, teamB.ID, value);
                        }
                    }
                }
            }

            if (content.TryGetNode("Objects", out Value _objects))
            {
                var objects = _objects.Array;
                if (objects != null)
                {
                    for (int i = 0; i < objects.Length; i++)
                    {
                        Value obj = objects[i];
                        if (string.IsNullOrWhiteSpace(obj["ID"].String))
                        {
                            AssetLogger.LogWarning($"Unknown object at index {i} in scene \"{id}\" ({file.FullName}:{obj["id"].Location})");
                            continue;
                        }
                        spawner?.Invoke(obj["ID"].String, true, obj["At", "Pos", "Position"].Vector2(), Quaternion.identity);
                    }
                }
            }
        }

        #endregion

        #region Prefab Loader

        /// <summary>
        /// It checks that a prefab has been generated with name <paramref name="prefabName"/> and exists as a child of <see cref="Prefabs"/>.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        bool HasGeneratedPrefab(string prefabName, out GameObject prefab)
        {
            if (string.IsNullOrWhiteSpace(prefabName)) throw new ArgumentException($"'{nameof(prefabName)}' cannot be null or whitespace.", nameof(prefabName));

            if (LoadedFromResources.Contains(prefabName))
            {
                prefab = Resources.Load<GameObject>(prefabName);
                return true;
            }

            Transform generatedPrefab = PrefabsGroup.Find(prefabName);
            if (generatedPrefab != null)
            {
                prefab = generatedPrefab.gameObject;
                return true;
            }

            prefab = null;
            return false;
        }

        /// <summary>
        /// Returns the generated prefab named <paramref name="prefabName"/> from the children of <see cref="Prefabs"/>.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        GameObject GetGeneratedPrefab(string prefabName)
        {
            if (string.IsNullOrWhiteSpace(prefabName)) throw new ArgumentException($"'{nameof(prefabName)}' cannot be null or whitespace.", nameof(prefabName));
            return PrefabsGroup.Find(prefabName).gameObject;
        }

        /// <summary>
        /// Instantiates the prefab <paramref name="prefab"/>.
        /// </summary>
        public static GameObject InstantiatePrefab(GameObject prefab, bool spawnOverNetwork, bool isBuiltin, Vector3 position, Quaternion rotation)
        {
            bool networked = spawnOverNetwork && prefab.HasComponent<Unity.Netcode.NetworkObject>();

            GameObject newObject = GameObject.Instantiate(prefab, position, rotation);
            if (CurrentScene.HasValue)
            { UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(newObject, CurrentScene.Value); }

            newObject.transform.SetPositionAndRotation(position, rotation);
            newObject.name = prefab.name;

            if (networked)
            {
                if (NetcodeSynchronizer.Instance != null && newObject.HasComponent<NetcodeView>())
                { NetcodeSynchronizer.Instance.RegisterObjectInstance(newObject, prefab.name, position, true); }

                if (Unity.Netcode.NetworkManager.Singleton != null &&
                    Unity.Netcode.NetworkManager.Singleton.IsListening)
                {
                    if (newObject.TryGetComponent(out Unity.Netcode.NetworkObject networkObject))
                    { networkObject.Spawn(true); }
                    else
                    { Debug.LogWarning($"[{nameof(AssetManager)}]: Can not spawn netcode object ({newObject}): Object does not have NetworkObject", newObject); }
                }
            }

            if (!isBuiltin) Instance.loadedAssetObjectEnablers.Add(newObject.GetComponent<LoadedAssetObjectEnabler>());

            return newObject;
        }

        /// <summary>
        /// Creates the prefab named <paramref name="prefabName"/> as a child of <see cref="Prefabs"/>.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="SingletonNotExistException{AssetManager}"></exception>
        public static GameObject LoadPrefab(string prefabName)
        {
            if (string.IsNullOrWhiteSpace(prefabName)) throw new ArgumentException($"'{nameof(prefabName)}' cannot be null or whitespace.", nameof(prefabName));
            if (Instance == null) throw new SingletonNotExistException<AssetManager>();

            if (Instance.HasGeneratedPrefab(prefabName, out GameObject existingPrefab))
            {
                if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Prefab \"{prefabName}\" already loaded");
                return existingPrefab;
            }

            if (Resources.Load<GameObject>(prefabName) != null)
            {
                var prefab = Resources.Load<GameObject>(prefabName);
                Instance.LoadedFromResources.Add(prefabName);
                if (prefab.HasComponent<Unity.Netcode.NetworkObject>())
                {
                    if (Unity.Netcode.NetworkManager.Singleton != null)
                    {
                        Unity.Netcode.NetworkManager.Singleton.AddNetworkPrefab(prefab);
                        if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Prefab \"{prefabName}\" registered for netcode");
                    }
                }
                Debug.Log($"[{nameof(AssetManager)}]: Prefab \"{prefabName}\" loaded (from Resources)");
                return prefab;
            }

            DataUtilities.FilePacker.IFile[] files = Instance.Assets.GetAllFiles("*.obj.sdf");
            foreach (var file in files)
            {
                if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Load file \"{file.FullName}\" ...");
                TimeSpan loadStarted = DateTime.UtcNow.TimeOfDay;
                Value content = Parser.Parse(file.Text);
                if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: File loaded in {(DateTime.UtcNow.TimeOfDay - loadStarted).TotalMilliseconds} ms : {file.FullName}");
                string id = content["ID"].String ?? file.Name[..^8];
                if (id != prefabName) continue;
                content = Files.ProcessInheritance(content, file, Instance.Assets.GetFile, Instance.MaxInheritanceDepth);
                return Instance.LoadPrefab(content, file);
            }

            AssetLogger.LogError($"Prefab \"{prefabName}\" does not exists");
            return null;
        }
        /// <summary>
        /// Creates the prefab named <paramref name="name"/> as a child of <see cref="Prefabs"/>.
        /// </summary>
        GameObject LoadPrefab(Value content, DataUtilities.FilePacker.IFile file)
        {
            string prefabName = content["ID"].String ?? file.Name[..^8];

            if (HasGeneratedPrefab(prefabName, out GameObject existingPrefab))
            {
                if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Prefab \"{prefabName}\" already loaded");
                return existingPrefab;
            }
            else
            {
                GameObject prefab = GeneratePrefab(content, file);

                if (prefab.HasComponent<Unity.Netcode.NetworkObject>())
                {
                    if (Unity.Netcode.NetworkManager.Singleton != null)
                    {
                        Unity.Netcode.NetworkManager.Singleton.AddNetworkPrefab(prefab);
                        if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Prefab \"{prefabName}\" registered for netcode");
                    }
                }
                Debug.Log($"[{nameof(AssetManager)}]: Prefab \"{prefabName}\" loaded");

                return prefab;
            }
        }

        public static void InstantiatePrefab(string prefabName, bool spawnOverNetwork, Vector3 position, Quaternion rotation, System.Action<GameObject> onInstantiated)
        {
            GameObject eh = InstantiatePrefab(prefabName, spawnOverNetwork, position, rotation);
            if (eh != null)
            {
                onInstantiated?.Invoke(eh);
                return;
            }

            Game.Blueprints.NetworkBlueprintManager.GetBlueprint(prefabName, blueprint =>
            {
                GameObject instance = Game.Blueprints.BlueprintManager.InstantiateBlueprint(blueprint);
                if (CurrentScene.HasValue)
                { UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, CurrentScene.Value); }

                instance.transform.SetPositionAndRotation(position, rotation);

                if (NetcodeSynchronizer.Instance != null && instance.HasComponent<NetcodeView>())
                { NetcodeSynchronizer.Instance.RegisterObjectInstance(instance, instance.name, position, true); }

                onInstantiated?.Invoke(instance);
            });
        }

        /// <summary>
        /// Generates and instantiates a prefab named <paramref name="prefabName"/>.
        /// This also creates the prefab as a child of <see cref="Prefabs"/>.<br/>
        /// If the prefab already exists as a child of <see cref="Prefabs"/> <see cref="InstantiatePrefab(GameObject, bool, bool)"/> is called.<br/>
        /// If the prefab already loaded <see cref="InstantiatePrefab(Value, DataUtilities.FilePacker.IFile, bool)"/> is called.<br/>
        /// If none of these, this will load the corresponding file, parses it and calls the <see cref="InstantiatePrefab(Value, FileInfo, bool)"/> method.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="SingletonNotExistException{AssetManager}"></exception>
        public static GameObject InstantiatePrefab(string prefabName, bool spawnOverNetwork, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(prefabName)) throw new ArgumentException($"'{nameof(prefabName)}' cannot be null or whitespace.", nameof(prefabName));
            if (Instance == null) throw new SingletonNotExistException<AssetManager>();

            if (Instance.BuiltinPrefabs.TryGetValue(prefabName, out GameObject builtinObject))
            {
                return InstantiatePrefab(builtinObject, spawnOverNetwork, true, position, rotation);
            }

            {
                if (Game.Blueprints.BlueprintManager.TryGetBlueprint(prefabName, out Game.Blueprints.Blueprint blueprint))
                {
                    GameObject instance = Game.Blueprints.BlueprintManager.InstantiateBlueprint(blueprint);
                    if (CurrentScene.HasValue)
                    { UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, CurrentScene.Value); }

                    instance.transform.SetPositionAndRotation(position, rotation);

                    if (NetcodeSynchronizer.Instance != null && instance.HasComponent<NetcodeView>())
                    { NetcodeSynchronizer.Instance.RegisterObjectInstance(instance, instance.name, position, true); }

                    return instance;
                }
            }

            {
                GameObject resourceAsset = Resources.Load<GameObject>(prefabName);
                if (resourceAsset != null)
                {
                    return InstantiatePrefab(resourceAsset, spawnOverNetwork, true, position, rotation);
                }
            }

            Debug.LogError($"[{nameof(AssetManager)}]: Bruh :(");
            return null;

            /*
            if (Instance.HasGeneratedPrefab(prefabName, out GameObject existingPrefab))
            {
                if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Prefab \"{prefabName}\" already generated, cloning ...");
                return InstantiatePrefab(existingPrefab, spawnOverNetwork, false, position, rotation);
            }

            var filesSDF = Instance.Assets.GetAllFilesEnumerable("*.obj.sdf");
            foreach (var file in filesSDF)
            {
                if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Load file \"{file.FullName}\" ...");
                TimeSpan loadStarted = DateTime.UtcNow.TimeOfDay;
                Value content = Parser.Parse(file.Text);
                if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: File loaded in {(DateTime.UtcNow.TimeOfDay - loadStarted).TotalMilliseconds} ms : {file.FullName}");
                string id = content["ID"].String ?? file.Name[..^8];
                if (id != prefabName) continue;
                content = Files.ProcessInheritance(content, file, Instance.Assets.GetFile, Instance.MaxInheritanceDepth);
                return Instance.InstantiatePrefab(content, file, spawnOverNetwork, position, rotation);
            }

            var filesJSON = Instance.Assets.GetAllFilesEnumerable("*.obj.json");
            foreach (var file in filesJSON)
            {
                if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Load file \"{file.FullName}\" ...");
                TimeSpan loadStarted = DateTime.UtcNow.TimeOfDay;
                Value content;
                try
                {
                    content = DataUtilities.Json.Parser.Parse(file.Text);
                }
                catch (Exception ex)
                {
                    AssetLogger.LogError($"Failed to parse file \"{file.FullName}\"\r\n{ex}");
                    continue;
                }
                if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: File loaded in {(DateTime.UtcNow.TimeOfDay - loadStarted).TotalMilliseconds} ms : {file.FullName}");
                string id = content["ID"].String ?? file.Name[..^8];
                if (id != prefabName) continue;
                content = Files.ProcessInheritance(content, file, Instance.Assets.GetFile, Instance.MaxInheritanceDepth);
                return Instance.InstantiatePrefab(content, file, spawnOverNetwork, position, rotation);
            }

            AssetLogger.LogError($"Prefab \"{prefabName}\" does not exists!");
            return null;
            */
        }

        /// <summary>
        /// Generates and instantiates a prefab named <paramref name="name"/>.<br/>
        /// This also creates the prefab as a child of <see cref="Prefabs"/>.<br/>
        /// If the prefab already exists as a child of <see cref="Prefabs"/> <see cref="InstantiatePrefab(GameObject, bool)"/> is called.<br/>
        /// If not, <see cref="GeneratePrefab(Value, DataUtilities.FilePacker.IFile, out bool)"/> is called before.
        /// </summary>
        GameObject InstantiatePrefab(Value content, DataUtilities.FilePacker.IFile file, bool spawnOverNetwork, Vector3 position, Quaternion rotation)
        {
            string prefabName = content["ID"].String ?? file.Name[..^8];

            if (HasGeneratedPrefab(prefabName, out GameObject existingPrefab))
            {
                if (EnableDebugLogging) Debug.Log($"Instantiating generated prefab {prefabName} ...");
                return InstantiatePrefab(existingPrefab, spawnOverNetwork, false, position, rotation);
            }
            else
            {
                GameObject prefab = GeneratePrefab(content, file);

                if (prefab.HasComponent<Unity.Netcode.NetworkObject>())
                {
                    if (Unity.Netcode.NetworkManager.Singleton != null)
                    {
                        Unity.Netcode.NetworkManager.Singleton.AddNetworkPrefab(prefab);
                        if (EnableDebugLogging) Debug.Log($"Prefab {prefabName} registered for netcode");
                    }
                }

                if (!HasGeneratedPrefab(prefabName, out GameObject existingPrefab2))
                {
                    AssetLogger.LogError($"Failed to generate prefab '{prefabName}'");
                    return null;
                }

                Debug.Log($"[{nameof(AssetManager)}]: Prefab \"{prefabName}\" loaded");
                return InstantiatePrefab(existingPrefab2, spawnOverNetwork, false, position, rotation);
            }
        }

        /// <summary>
        /// Generates and instantiates the prefab as a child of <see cref="Prefabs"/>.
        /// </summary>
        GameObject GeneratePrefab(Value content, DataUtilities.FilePacker.IFile file)
        {
            TimeSpan started = DateTime.Now.TimeOfDay;
            string prefabName = content["ID"].String ?? file.Name[..^8];

            if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Generating prefab \"{prefabName}\" ...");

            Type type = typeof(IHaveAssetFields);
            Type[] types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p))
                .ToArray();

            GameObject prefab;
            prefab = new(prefabName);
            prefab.transform.SetParent(PrefabsGroup);
            if (content.Has("Tag")) prefab.tag = content["Tag"].String;
            prefab.SetActive(false);

            Type[] loadableComponents = UnityThingLoaders.GetLoadableComponents();

            if (content.TryGetNode("Childs", out Value ChildsNode))
            {
                var ChildNames = ChildsNode.ChildNames;
                foreach (var i in ChildNames)
                {
                    var ChildNode = ChildsNode[i];
                    if (ChildNode.Type == DataUtilities.ReadableFileFormat.ValueType.LITERAL)
                    {
                        if (!BuiltinPrefabs.TryGetValue(ChildNode.String, out GameObject builtinPrefab))
                        {
                            Debug.LogError($"[{nameof(AssetManager)}]: Builtin prefab \"{ChildNode.String}\" not found");
                        }
                        else
                        {
                            GameObject instance = GameObject.Instantiate(builtinPrefab, prefab.transform);
                            instance.name = ChildNode.String;
                            if (CurrentScene.HasValue)
                            { UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, CurrentScene.Value); }
                        }
                    }
                    else
                    {
                        var child = GeneratePrefab(ChildNode, file);
                        child.transform.SetParent(prefab.transform);
                        child.SetActive(true);
                    }
                }
            }

            if (content.TryGetNode("Components", out Value components))
            {
                Value component;

                prefab.AddOrModifyComponent<LoadedAssetObjectEnabler>();
                prefab.AddOrModifyComponent<LoadedAssetObject>();

                IReadOnlyCollection<string> componentNames = components.ChildNames;

                foreach (string componentName in componentNames)
                {
                    component = components[componentName];

                    foreach (Type type_ in types)
                    {
                        if (type_.Name != componentName) continue;

                        Component newComp = ComponentUtils.AddComponent(prefab, type_);

                        break;
                    }

                    foreach (Type type_ in loadableComponents)
                    {
                        if (type_.Name != componentName) continue;

                        Component newComp = ComponentUtils.AddComponent(prefab, type_);

                        break;
                    }
                }

                foreach (string componentName in componentNames)
                {
                    component = components[componentName];

                    foreach (Type type_ in types)
                    {
                        if (type_.Name != componentName) continue;

                        if (!prefab.TryGetComponent(type_, out Component addedComponent))
                        { Debug.LogWarning($"[{nameof(AssetManager)}]: Component \"{type}\" not found in {prefab}", prefab); }
                        else
                        { ComponentUtils.LoadComponent(prefab, addedComponent, component); }

                        break;
                    }

                    foreach (Type type_ in loadableComponents)
                    {
                        if (type_.Name != componentName) continue;

                        if (!prefab.TryGetComponent(type_, out Component addedComponent))
                        { Debug.LogWarning($"[{nameof(AssetManager)}]: Component \"{type}\" not found in {prefab}", prefab); }
                        else
                        {
                            UnityComponentLoader loader = UnityThingLoaders.GetComponentLoader(type_);
                            loader.Load(prefab, addedComponent, component);
                        }

                        break;
                    }
                }

                if (components.TryGetNode("MeshFilter", out component))
                {
                    string modelFile = component["Model"].String;
                    if (modelFile != null)
                    {
                        LoadModel(modelFile, prefab);
                    }
                }

                if (components.TryGetNode("Transform", out component))
                {
                    prefab.transform.SetLocalPositionAndRotation(UnityThingLoaders.LoadObject<Vector3>(component["LocalPosition"], Vector3.zero), Quaternion.Euler(UnityThingLoaders.LoadObject<Vector3>(component["LocalRotation"], Vector3.zero)));
                    prefab.transform.localScale = UnityThingLoaders.LoadObject<Vector3>(component["LocalScale"], Vector3.one);
                }

                if (prefab.HasComponent<MeshFilter>())
                {
                    prefab.transform.localScale = new Vector3(-prefab.transform.localScale.x, prefab.transform.localScale.y, prefab.transform.localScale.z);
                }
            }

            if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Prefab \"{prefabName}\" generated in {(DateTime.Now.TimeOfDay - started).TotalMilliseconds} ms", prefab);

            return prefab;
        }

#if UNITY_EDITOR
        public static Value PackPrefab(GameObject @object)
        {
            if (Instance == null)
            { Instance = FindObjectOfType<AssetManager>(); }

            Value result = Value.Object();

            result["ID"] = Value.Literal(@object.name);
            if (!string.IsNullOrWhiteSpace(@object.tag) && !@object.CompareTag("Untagged")) result["Tag"] = Value.Literal(@object.tag);
            Value components = Value.Object();

            Type type = typeof(IHaveAssetFields);
            Type[] types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p))
                .ToArray();

            foreach (var type_ in types)
            {
                if (!@object.TryGetComponent(type_, out Component component))
                { continue; }

                Value componentData = Value.Object();

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

                foreach (var field in fields)
                {
                    string fieldName = field.Name;

                    Value? value = DataValueUtils.SaveValue(@object, field.GetValue(component));

                    if (value.HasValue)
                    { componentData[fieldName] = value.Value; }
                }

                foreach (var property in properties)
                {
                    string propertyName = property.Name;

                    Value? value = DataValueUtils.SaveValue(@object, property.GetValue(component));

                    if (value.HasValue)
                    { componentData[propertyName] = value.Value; }
                }

                components[component.GetType().Name] = componentData;
            }

            var unityComponents = UnityThingLoaders.GetLoadableComponents();

            foreach (var type_ in unityComponents)
            {
                if (!@object.TryGetComponent(type_, out Component component))
                { continue; }

                Value componentData = UnityThingLoaders.SaveComponent(type_, component);

                components[component.GetType().Name] = componentData;
            }

            if (@object.transform.localPosition != Vector3.zero ||
                @object.transform.localRotation != Quaternion.identity ||
                @object.transform.localScale != Vector3.one)
            {
                Value componentData = Value.Object();

                if (@object.transform.localPosition != Vector3.zero)
                {
                    componentData["LocalPosition"] = UnityThingLoaders.SaveObject(@object.transform.localPosition);
                }
                if (@object.transform.localRotation != Quaternion.identity)
                {
                    componentData["LocalRotation"] = UnityThingLoaders.SaveObject(@object.transform.localRotation.eulerAngles);
                }
                if (@object.transform.localScale != Vector3.one)
                {
                    componentData["LocalScale"] = UnityThingLoaders.SaveObject(@object.transform.localScale);
                }

                components["Transform"] = componentData;
            }

            if (@object.HasComponent<MeshRenderer>())
            {
                var component = @object.GetComponent<MeshRenderer>();
                components["MeshRenderer"] = Value.Object();
            }

            if (@object.HasComponent<MeshFilter>())
            {
                var component = @object.GetComponent<MeshFilter>();
                Value componentData = Value.Object();

                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(component.sharedMesh);
                if (!string.IsNullOrWhiteSpace(assetPath))
                {
                    string modelName = Path.GetFileName(assetPath);
                    componentData["Model"] = Value.Literal(modelName);
                }
                else
                {
                    Debug.LogWarning($"Model asset not found", component);
                    componentData["Model"] = Value.Literal("");
                }

                components["MeshFilter"] = componentData;
            }

            result["Components"] = components;

            Value ChildObjects = Value.Object();

            for (int i = 0; i < @object.transform.childCount; i++)
            {
                var child = @object.transform.GetChild(i).gameObject;

                if (child.name.StartsWith("__"))
                {
                    var builtinPrefabName = child.name[2..];
                    if (!Instance.BuiltinPrefabs.TryGetValue(builtinPrefabName, out GameObject builtinPrefab))
                    { Debug.LogError($"Builtin prefab \"{builtinPrefabName}\" not found", child); }
                    ChildObjects[i] = Value.Literal(builtinPrefabName);
                }
                else
                {
                    Value childData = PackPrefab(child);
                    ChildObjects[i] = childData;
                }
            }

            result["Childs"] = ChildObjects;

            return result;
        }
#endif

        #endregion

        #region Audio Loader

        public static async System.Threading.Tasks.Task<AudioClip> LoadClipAsync(string path, AudioType audioType)
        {
            if (EnableDebugLogging) Debug.Log($"Loading audio clip {path} ...");
            AudioClip clip = null;

            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType))
            {
                uwr.SendWebRequest();

                try
                {
                    // TODO: Support on WebGL
                    while (!uwr.isDone) await System.Threading.Tasks.Task.Delay(5);

                    switch (uwr.result)
                    {
                        case UnityWebRequest.Result.Success:
                            clip = DownloadHandlerAudioClip.GetContent(uwr);
                            if (clip == null)
                            {
                                AssetLogger.LogError($"Audio clip {path} failed to load");
                            }
                            else
                            {
                                Debug.Log($"Audio clip {path} loaded");
                            }
                            break;
                        case UnityWebRequest.Result.ConnectionError:
                        case UnityWebRequest.Result.ProtocolError:
                        case UnityWebRequest.Result.DataProcessingError:
                            AssetLogger.LogError($"{uwr.result}");
                            break;
                        case UnityWebRequest.Result.InProgress:
                        default:
                            AssetLogger.LogError($"Unknown error");
                            break;
                    }
                }
                catch (System.Exception err)
                {
                    AssetLogger.LogError(err);
                }
            }

            return clip;
        }

        public static AudioClip LoadClip(string path, AudioType audioType)
        {
            /*
            var task = LoadClipAsync(path, audioType);
            task.Wait();
            if (task.IsCanceled)
            {
                Debug.LogError($"{nameof(LoadClipAsync)} is cancelled");
                return null;
            }
            if (task.IsFaulted)
            {
                Debug.LogError($"{nameof(LoadClipAsync)} is faulted");
                return null;
            }
            if (!task.IsCompleted)
            {
                Debug.LogError($"{nameof(LoadClipAsync)} is not completed");
                return null;
            }
            if (!task.IsCompletedSuccessfully)
            {
                Debug.LogError($"{nameof(LoadClipAsync)} is not completed successfully");
                return null;
            }
            return task.Result;
            */

            if (EnableDebugLogging) Debug.Log($"Loading audio clip {path} ...");
            TimeSpan started = DateTime.Now.TimeOfDay;
            AudioClip clip = null;

            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType))
            {
                uwr.SendWebRequest();

                try
                {
                    // TODO: Support on WebGL
                    while (!uwr.isDone)
                    {
                        System.Threading.Tasks.Task.Delay(5);
                        if ((DateTime.Now.TimeOfDay - started).TotalSeconds > 5f)
                        {
                            AssetLogger.LogError($"Audio clip {path} loading is exceeded 5 sec ({(DateTime.Now.TimeOfDay - started).TotalSeconds} sec)");
                            return null;
                        }
                    }

                    switch (uwr.result)
                    {
                        case UnityWebRequest.Result.Success:
                            clip = DownloadHandlerAudioClip.GetContent(uwr);
                            if (clip == null)
                            {
                                AssetLogger.LogError($"Audio clip {path} failed to load");
                            }
                            else
                            {
                                if (EnableDebugLogging) Debug.Log($"Audio clip {path} loaded in {(DateTime.Now.TimeOfDay - started).TotalMilliseconds} ms");
                            }
                            break;
                        case UnityWebRequest.Result.ConnectionError:
                        case UnityWebRequest.Result.ProtocolError:
                        case UnityWebRequest.Result.DataProcessingError:
                            AssetLogger.LogError($"{uwr.result}");
                            break;
                        case UnityWebRequest.Result.InProgress:
                        default:
                            AssetLogger.LogError($"Unknown error");
                            break;
                    }
                }
                catch (Exception err)
                {
                    AssetLogger.LogError(err);
                }
            }

            return clip;
        }

        #endregion

        #region Model Loader

        public static void LoadModel(string name, GameObject @object)
        {
            var files = Instance.Assets.GetAllFiles("*.obj");
            foreach (var file in files)
            {
                if (file.Name != name) continue;

                Dummiesman.OBJLoader loader = new()
                { Shader = Instance.defaultShader };

                DataUtilities.FilePacker.IFile mtlFile = Instance.Assets.GetAbsoluteFile(file.FullName[..^3] + "mtl");

                GameObject loaded;
                if (mtlFile != null)
                {
                    loaded = loader.Load(file.FullName, mtlFile.FullName);
                }
                else
                {
                    Debug.LogWarning($"Material file \"{file.FullName[..^3] + "mtl"}\" not found");
                    loaded = loader.Load(file.FullName);
                }

                var meshRenderer = loaded.GetComponentInChildren<MeshRenderer>(false);
                var meshFilter = loaded.GetComponentInChildren<MeshFilter>(false);

                meshFilter.mesh.RecalculateBounds();
                meshFilter.mesh.RecalculateNormals();
                meshFilter.mesh.RecalculateTangents();
                meshFilter.sharedMesh.RecalculateBounds();
                meshFilter.sharedMesh.RecalculateNormals();
                meshFilter.sharedMesh.RecalculateTangents();

                @object.AddComponent(meshFilter);
                @object.AddComponent(meshRenderer);

                @object.GetComponent<MeshFilter>().mesh.RecalculateBounds();
                @object.GetComponent<MeshFilter>().mesh.RecalculateNormals();
                @object.GetComponent<MeshFilter>().mesh.RecalculateTangents();
                @object.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
                @object.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
                @object.GetComponent<MeshFilter>().sharedMesh.RecalculateTangents();

                loaded.Destroy();
                return;
            }

            AssetLogger.LogError($"Model (.obj) \"{name}\" not found");
        }

        #endregion

        /// <summary>
        /// Loads the image from the <paramref name="path"/>.
        /// </summary>
        /// <exception cref="SingletonNotExistException{AssetManager}"></exception>
        public static Texture2D LoadTexture(DataUtilities.FilePacker.IFile file)
        {
            if (Instance == null) throw new SingletonNotExistException<AssetManager>();
            if (Instance.textures.TryGetAsset(file.FullName, out Texture2D saved)) return saved;

            Texture2D texture = new(2, 2);
            if (EnableDebugLogging) Debug.Log($"Load image file {file.FullName}");
            texture.LoadImage(file.Bytes);
            Instance.textures.Add(new CachedAsset<Texture2D>(file.FullName, texture));
            return texture;
        }

        /// <summary>
        /// Loads the image from the <paramref name="path"/>.
        /// </summary>
        /// <exception cref="SingletonNotExistException{AssetManager}"></exception>
        public static Texture2D LoadTexture(string ID)
        {
            if (Instance == null) throw new SingletonNotExistException<AssetManager>();
            if (Instance.textures.TryGetAsset(ID, out Texture2D saved)) return saved;
            return null;
        }

        void Awake()
        {
            if (Instance != null)
            {
                if (EnableDebugLogging) Debug.Log($"[{nameof(AssetManager)}]: Instance already registered, destroying self gameObject");
                gameObject.Destroy();
                return;
            }
            Instance = this;
            // Assets = new FileLoader("C:\\Users\\bazsi\\Desktop\\Nothing Assets\\");
            // AssetLogger.Path = Path.Combine(Assets.BasePath, "errors.log");
            // Photon.Pun.PhotonNetwork.PrefabPool = new PhotonPrefabTool();
            transform.SetParent(null);
            DontDestroyOnLoad(this);
        }

        void Start()
        {
            /*
            Debug.Log(
                "Some Application Info:\n" +
                $"Application.absoluteURL: '{Application.absoluteURL}'\n" +
                $"Application.consoleLogPath: '{Application.consoleLogPath}'\n" +
                $"Application.dataPath: '{Application.dataPath}'\n" +
                $"Application.GetBuildTags(): '{string.Join(", ", Application.GetBuildTags() ?? new string[0])}'\n" +
                $"Application.isBatchMode: '{Application.isBatchMode}'\n" +
                $"Application.persistentDataPath: '{Application.persistentDataPath}'\n" +
                $"Application.streamingAssetsPath: '{Application.streamingAssetsPath}'\n" +
                $"Application.temporaryCachePath: '{Application.temporaryCachePath}'"
            );
            */

            LoadConfig();
            window = IMGUIManager.Instance.CreateWindow(new Rect(Screen.width - 20 - 140, 20, 140, 110));
            window.DrawContent = OnWindow;
            window.Title = "Asset Manager";
        }

        void OnConfigLoaded(GameConfigManager.GameConfig config)
        {
            settings = config;
            Assets = new FolderLoader();
            testObject = settings.test_object;
            /*
            testScene = settings.test_scene;

            if (AutoLoadTestScene)
            { LoadAssets(); }
            */
        }

        void LoadAssets()
        {
            string path = settings.assets_path;
            if (Unity.Netcode.NetworkManager.Singleton != null &&
                Unity.Netcode.NetworkManager.Singleton.IsConnectedClient)
            { path = "netcode"; }
            StartCoroutine(Assets.LoadAsync(path, OnAssetsLoaded, OnAssetDownloadProgress));
        }

        void OnAssetsLoaded()
        {
            AssetLogger.Path = Path.Combine(Assets.Root.FullName, "errors.log");

            // if (AutoLoadTestScene) LoadTestScene();

            DataUtilities.FilePacker.IFile prefabsFile = Assets.GetAbsoluteFile("prefabs.txt");
            if (prefabsFile != null)
            {
                string content = prefabsFile.Text;
                if (content != null)
                {
                    string[] lines = content.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (string.IsNullOrEmpty(line)) continue;
                        LoadPrefab(line);
                    }
                }
                else
                { Debug.LogWarning($"[{nameof(AssetManager)}]: Failed to load file {prefabsFile.FullName}"); }
            }
            else
            { Debug.LogWarning($"[{nameof(AssetManager)}]: File \"prefabs.txt\" not found in root"); }
        }

        void LoadConfig() => StartCoroutine(GameConfigManager.GetAsync(OnConfigLoaded));

        void LoadConfigByUser() => StartCoroutine(GameConfigManager.GetAsync(config =>
        {
            settings = config;
            Assets = new FolderLoader();
            testObject = settings.test_object;
            // testScene = settings.test_scene;
            StartCoroutine(Assets.LoadAsync(Unity.Netcode.NetworkManager.Singleton.IsConnectedClient ? "netcode" : settings.assets_path, () => { }, OnAssetDownloadProgress));
        }));

        void OnAssetDownloadProgress(Networking.Network.ChunkCollector chunkCollector)
        {
            AssetsDownloadProgress.OnProgress(chunkCollector);
        }

        void FixedUpdate()
        {
            for (int i = loadedAssetObjectEnablers.Count - 1; i >= 0; i--)
            {
                if (loadedAssetObjectEnablers[i] == null) continue;
                if (Prefabs == null) continue;

                if (loadedAssetObjectEnablers[i].transform.parent == Prefabs)
                { continue; }

                if (loadedAssetObjectEnablers[i].gameObject.activeSelf)
                {
                    Destroy(loadedAssetObjectEnablers[i]);
                    continue;
                }

                if (EnableDebugLogging) Debug.Log($"Enable spawned object {loadedAssetObjectEnablers[i].gameObject}", loadedAssetObjectEnablers[i]);
                loadedAssetObjectEnablers[i].gameObject.SetActive(true);

                Destroy(loadedAssetObjectEnablers[i]);
            }
            loadedAssetObjectEnablers.Clear();

            cacheTextures = textures.Count;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4)) window.Visible = !window.Visible;
        }

        void OnWindow()
        {
            if (GUILayout.Button("Load Assets"))
            { LoadAssets(); }

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label($"Download Progress: {Maths.Round(AssetsDownloadProgress.Progress * 1000f) * 0.1f}%");
            GUILayout.Label($"Download Speed: {Utils.ReadableSize(AssetsDownloadProgress.Speed)} b/s");
            if (AssetsDownloadProgress.RemaingTime == TimeSpan.MaxValue)
            { GUILayout.Label($"Download Remaining Time: --:--:--"); }
            else
            { GUILayout.Label($"Download Remaining Time: {AssetsDownloadProgress.RemaingTime:hh\\:mm\\:ss}"); }

            GUILayout.EndVertical();

            if (Assets == null)
            {
                GUILayout.Label($"Assets is null");
            }
            else if (Assets.IsLoaded)
            {
                if (GUILayout.Button("Spawn Test Object"))
                { SpawnTestObject(); }
                // if (GUILayout.Button("Load Test Scene"))
                // { LoadTestScene(); }
            }
            else
            {
                GUILayout.Label($"Assets are not loaded");
            }

            if (GUILayout.Button("Clear Cache"))
            { ClearCache(); }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}
