using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable

public class SceneManager : SingleInstance<SceneManager>
{
    [SerializeField] List<string> scenes = new();
    readonly List<AsyncOperation> sceneLoadings = new();
    readonly List<AsyncOperation> sceneUnloadings = new();

    const int BaseScene = 0;

    public static IReadOnlyList<string> Scenes => Instance.scenes;
    public static string? LoadedScene
    {
        get
        {
            foreach (Scene scene in AllScenes)
            {
                if (scene.isLoaded && Instance.scenes.Contains(scene.name))
                { return scene.name; }
            }
            return null;
        }
    }

    public static IReadOnlyList<AsyncOperation> SceneLoadings
    {
        get
        {
            if (instance == null) return new List<AsyncOperation>();
            return instance.sceneLoadings;
        }
    }
    public static IReadOnlyList<AsyncOperation> SceneUnloadings
    {
        get
        {
            if (instance == null) return new List<AsyncOperation>();
            return instance.sceneUnloadings;
        }
    }
    public static IEnumerable<Scene> AllScenes
    {
        get
        {

            int n = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < n; i++)
            { yield return UnityEngine.SceneManagement.SceneManager.GetSceneAt(i); }
        }
    }

    protected override void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning($"[{nameof(SceneManager)}]: Instance already registered, destroying self");
            Destroy(this);
            return;
        }
        instance = this;

        transform.SetParent(null);
        DontDestroyOnLoad(this);

        /*
        {
            string v = "Some Scene Info:\n" +
                $"Active Scene: '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'\n" +
                $"Loaded Scene Count: {UnityEngine.SceneManagement.SceneManager.sceneCount}\n" +
                $"Scene Count: {UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings}\n" +
                $"Scenes: [\n";
            int n = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < n; i++)
            {
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                v += $"  {i}: '{s.name}' (loaded: {s.isLoaded}),\n";
            }
            v += $"]";

            Debug.Log(v);
        }
        */
    }

    public static void LoadScene(string scene)
    {
        if (scene == null) return;

        if (LoadedScene != null &&
            LoadedScene.Equals(scene, System.StringComparison.InvariantCultureIgnoreCase))
        {
            Debug.Log($"[{nameof(SceneManager)}]: Scene \"{scene}\" already loaded (i think ...)");
            return;
        }

        Scene scene1 = UnityEngine.SceneManagement.SceneManager.GetSceneByName(scene);

        if (scene1.isLoaded)
        {
            Debug.Log($"[{nameof(SceneManager)}]: Scene \"{scene}\" already loaded");
            return;
        }

        if (Unity.Netcode.NetworkManager.Singleton == null ||
            !Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            Debug.Log($"[{nameof(SceneManager)}]: Loading scene \"{scene}\" (unity)");
            AsyncOperation task = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            if (instance != null) instance.sceneLoadings.Add(task);
        }
        else if (Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[{nameof(SceneManager)}]: Loading scene \"{scene}\" (netcode)");
            Unity.Netcode.SceneEventProgressStatus status = Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(scene, LoadSceneMode.Additive);
            Debug.Log($"[{nameof(SceneManager)}]: Scene loading status: {status}");
        }
    }

    public static void UnloadAllScenes()
    {
        int n = UnityEngine.SceneManagement.SceneManager.sceneCount;
        for (int i = 0; i < n; i++)
        {
            Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            if (scene.buildIndex == BaseScene)
            {
                Debug.Log($"[{nameof(SceneManager)}]: Skip unloading scene \"{scene.name}\"");
                continue;
            }

            if (Unity.Netcode.NetworkManager.Singleton == null ||
                !Unity.Netcode.NetworkManager.Singleton.IsListening)
            {
                Debug.Log($"[{nameof(SceneManager)}]: Unloading scene \"{scene.name}\" (unity)");
                AsyncOperation task = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
                if (instance != null) instance.sceneUnloadings.Add(task);
            }
            else if (Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                Debug.Log($"[{nameof(SceneManager)}]: Unloading scene \"{scene.name}\" (netcode)");
                Unity.Netcode.SceneEventProgressStatus status = Unity.Netcode.NetworkManager.Singleton.SceneManager.UnloadScene(scene);
                Debug.Log($"[{nameof(SceneManager)}]: Scene unloading status: {status}");
            }
        }
    }

    public static bool IsLoaded(string scene)
    {
        foreach (Scene _scene in AllScenes)
        {
            if (_scene.isLoaded && Instance.scenes.Contains(_scene.name))
            { return true; }
        }
        return false;
    }
}
