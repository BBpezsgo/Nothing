#if !UNITY_EDITOR && PLATFORM_WEBGL
#define DOWNLOAD_ASSETS
#endif

using System;
using System.IO;
using UnityEngine;
using Utilities;

public static class Settings
{
    public static Data Current { get; set; } = new Data();

    public static string Path => Application.persistentDataPath + "/settings.bin";

    public class Data : ISerializable
    {
        public class DataVolume : ISerializable
        {
            public float Master = 0f;
            public float VFX = 0f;
            public float UI = 0f;

            public void Deserialize(BinaryReader reader)
            {
                Master = reader.ReadSingle();
                VFX = reader.ReadSingle();
                UI = reader.ReadSingle();
            }

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Master);
                writer.Write(VFX);
                writer.Write(UI);
            }
        }

        internal DataVolume Volume = new();

        public void Save() => Settings.Save(this);
        public static void Load() => Current = Settings.Load();

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Volume);
        }

        public void Deserialize(BinaryReader reader)
        {
            Volume = reader.ReadObj<DataVolume>();
        }

        public static Data Default => new()
        {
            Volume = new Data.DataVolume()
            {
                Master = .5f,
                UI = .5f,
                VFX = .5f,
            },
        };
    }

    static readonly bool Logs = false;

    static void Save(Data data)
    {
        if (Logs) Debug.Log("[Settings]: Saving to file ...");

        byte[] serialized = Serializing.Serialize(data);
        File.WriteAllBytes(Path, serialized);

        if (Logs) Debug.Log("[Settings]: Saved to file");
    }
    static Data Load()
    {
        if (Logs) Debug.Log("[Settings]: Loading from file");

        if (!File.Exists(Path)) return Data.Default;

        return Serializing.Deserialize<Data>(File.ReadAllBytes(Path));
    }
}

public static class GameConfigManager
{
    const string CONFIG_FILE = "config.json";
#if PLATFORM_WEBGL && DOWNLOAD_ASSETS
    const string BASE_URI = "http://192.168.1.100:7777";
#endif

#if !PLATFORM_WEBGL || !DOWNLOAD_ASSETS
    static string Path => System.IO.Path.Combine(Application.streamingAssetsPath, CONFIG_FILE);
#endif

    public static System.Collections.IEnumerator GetAsync(Action<GameConfig> callback)
    {
#if PLATFORM_WEBGL && DOWNLOAD_ASSETS
        Uri uri = new(new Uri(BASE_URI), "/StreamingAssets/" + CONFIG_FILE);
        Debug.Log($"[{nameof(GameConfigManager)}]: Downloading config file from {uri}");
        UnityEngine.Networking.UnityWebRequest req = UnityEngine.Networking.UnityWebRequest.Get(uri);
        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[{nameof(GameConfigManager)}]: HTTP Error while downloading {req.uri}. Result: {req.result} Error: {req.error}");
            callback?.Invoke(GameConfig.Default);
            yield break;
        }
        else if (req.responseCode != 200)
        {
            Debug.LogError($"[{nameof(GameConfigManager)}]: HTTP {req.responseCode} while downloading {req.uri}");
            callback?.Invoke(GameConfig.Default);
            yield break;
        }

        if (req.GetResponseHeader("Content-Type") != "application/json")
        {
            Debug.LogWarning($"[{nameof(GameConfigManager)}]: Unknown content type for config file: \'{req.GetResponseHeader("Content-Type")}\'");
        }
        var result = JsonUtility.FromJson<GameConfig>(req.downloadHandler.text);

        // Debug.Log($"[{nameof(GameConfigManager)}]: Modify 'assets_path' to '{BASE_URI + "/assets.bin"}'");
        // result.assets_path = BASE_URI + "/assets.bin";
        callback?.Invoke(result);
        yield break;
#else
        if (!File.Exists(Path))
        {
            SetDefaults();
            callback?.Invoke(GameConfig.Default);
            yield break;
        }
        string Json = File.ReadAllText(Path);
        if (string.IsNullOrEmpty(Json) || string.IsNullOrWhiteSpace(Json))
        {
            SetDefaults();
            callback?.Invoke(GameConfig.Default);
            yield break;
        }
        GameConfig result = JsonUtility.FromJson<GameConfig>(Json);

        callback?.Invoke(result);
        yield break;
#endif
    }

    public static void SetDefaults()
    {
#if !PLATFORM_WEBGL
        try
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            { Directory.CreateDirectory(Application.streamingAssetsPath); }
            GameConfig defaultData = GameConfig.Default;
            string json = JsonUtility.ToJson(defaultData);
            File.WriteAllText(Path, json);
        }
        catch (Exception)
        { }
#endif
    }

    [Serializable]
    public struct GameConfig : ISerializable
    {
        public ushort netcode_tcpServer_port;

        public static GameConfig Default => new()
        {
            netcode_tcpServer_port = 7779,
        };

        public readonly void Serialize(BinaryWriter serializer)
        {
            serializer.Write(netcode_tcpServer_port);
        }

        public void Deserialize(BinaryReader deserializer)
        {
            netcode_tcpServer_port = deserializer.ReadUInt16();
        }
    }
}
