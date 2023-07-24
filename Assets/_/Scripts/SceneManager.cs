using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager : SingleInstance<SceneManager>
{
    [SerializeField] List<string> scenes = new();

    public static IReadOnlyList<string> Scenes => instance.scenes;
    public static string LoadedScene
    {
        get
        {
            for (int i = 0; i < instance.scenes.Count; i++)
            {
                if (UnityEngine.SceneManagement.SceneManager.GetSceneByName(instance.scenes[i]).isLoaded)
                { return instance.scenes[i]; }
            }
            return null;
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
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene, LoadSceneMode.Additive);
        }
        else if (Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[{nameof(SceneManager)}]: Loading scene \"{scene}\" (netcode)");
            Unity.Netcode.SceneEventProgressStatus status = Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(scene, LoadSceneMode.Additive);
            Debug.Log($"[{nameof(SceneManager)}]: Scene loading status: {status}");
        }
    }
}
