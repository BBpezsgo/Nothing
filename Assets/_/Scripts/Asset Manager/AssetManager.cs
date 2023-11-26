using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetManager
{
    public class AssetManager : MonoBehaviour
    {
        public static AssetManager Instance;

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

        [SerializeField] List<Pair<string, GameObject>> BuiltinPrefabs = new();

        #region Prefab Loader

        static GameObject InstantiatePrefab(GameObject prefab, bool spawnOverNetwork, Vector3 position, Quaternion rotation)
        {
            bool networked = spawnOverNetwork && prefab.HasComponent<Unity.Netcode.NetworkObject>();

            GameObject newObject = GameObject.Instantiate(prefab, position, rotation);
            if (CurrentScene.HasValue)
            { UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(newObject, CurrentScene.Value); }

            newObject.transform.SetPositionAndRotation(position, rotation);
            newObject.name = prefab.name;

            if (networked)
            {
                if (Unity.Netcode.NetworkManager.Singleton != null &&
                    Unity.Netcode.NetworkManager.Singleton.IsListening)
                {
                    if (newObject.TryGetComponent(out Unity.Netcode.NetworkObject networkObject))
                    { networkObject.Spawn(true); }
                    else
                    { Debug.LogWarning($"[{nameof(AssetManager)}]: Can not spawn netcode object ({newObject}): Object does not have NetworkObject", newObject); }
                }
            }

            return newObject;
        }

        public static GameObject InstantiatePrefab(string prefabName, bool spawnOverNetwork, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(prefabName)) throw new ArgumentException($"'{nameof(prefabName)}' cannot be null or whitespace.", nameof(prefabName));
            if (Instance == null) throw new SingletonNotExistException<AssetManager>();

            if (Instance.BuiltinPrefabs.TryGetValue(prefabName, out GameObject builtinObject))
            {
                return InstantiatePrefab(builtinObject, spawnOverNetwork, position, rotation);
            }

            {
                GameObject resourceAsset = Resources.Load<GameObject>(prefabName);
                if (resourceAsset != null)
                {
                    return InstantiatePrefab(resourceAsset, spawnOverNetwork, position, rotation);
                }
            }

            Debug.LogError($"[{nameof(AssetManager)}]: Bruh :(");
            return null;
        }

        #endregion

        void Awake()
        {
            if (Instance != null)
            {
                Debug.Log($"[{nameof(AssetManager)}]: Instance already registered, destroying self gameObject");
                gameObject.Destroy();
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(this);
        }
    }
}
