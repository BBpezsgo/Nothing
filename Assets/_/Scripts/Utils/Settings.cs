#if false || !UNITY_EDITOR && PLATFORM_WEBGL
#define DOWNLOAD_ASSETS
#endif

using DataUtilities.ReadableFileFormat;
using DataUtilities.Serializer;

using System;
using System.IO;
using System.Runtime.Serialization;

using UnityEngine;

public static class Settings
{
    public static Data Current { get; set; } = new Data();

    public static string Path => Application.persistentDataPath + "/settings.bin";

    public class Data : ISerializable<Data>, IFullySerializableText
    {
        public class DataVolume : ISerializable<DataVolume>, IFullySerializableText
        {
            public float Master = 0f;
            public float VFX = 0f;
            public float UI = 0f;

            public void Deserialize(Deserializer deserializer)
            {
                Master = deserializer.DeserializeFloat();
                VFX = deserializer.DeserializeFloat();
                UI = deserializer.DeserializeFloat();
            }

            public void DeserializeText(Value data)
            {
                Master = data["Master"].Float ?? 0f;
                VFX = data["VFX"].Float ?? 0f;
                UI = data["UI"].Float ?? 0f;
            }

            public void Serialize(Serializer serializer)
            {
                serializer.Serialize(Master);
                serializer.Serialize(VFX);
                serializer.Serialize(UI);
            }

            public Value SerializeText()
            {
                Value result = Value.Object();
                result["Master"] = Master;
                result["VFX"] = VFX;
                result["UI"] = UI;
                return result;
            }
        }

        internal DataVolume Volume = new();

        public void Save() => Settings.Save(this);
        public static void Load() => Current = Settings.Load();

        internal void SerializeGet(SerializationInfo info)
        {
            info.AddValue("volume.master", this.Volume.Master);
            info.AddValue("volume.vfx", this.Volume.VFX);
            info.AddValue("volume.ui", this.Volume.UI);
        }

        internal void SerializeSet(SerializationInfo info)
        {
            this.Volume = new()
            {
                Master = (float)info.GetValue("volume.master", typeof(float)),
                VFX = (float)info.GetValue("volume.vfx", typeof(float)),
                UI = (float)info.GetValue("volume.ui", typeof(float)),
            };
        }

        public void Serialize(Serializer serializer)
        {
            serializer.Serialize(Volume);
        }

        public void Deserialize(Deserializer deserializer)
        {
            Volume = deserializer.DeserializeObject<DataVolume>();
        }

        public Value SerializeText()
        {
            Value result = Value.Object();
            result["Volume"] = Volume.SerializeText();
            return result;
        }

        public void DeserializeText(Value data)
        {
            Volume = new();
            Volume.DeserializeText(data["Volume"]);
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

        byte[] serialized = SerializerStatic.Serialize(data);
        File.WriteAllBytes(Path, serialized);

        if (Logs) Debug.Log("[Settings]: Saved to file");
    }
    static Data Load()
    {
        if (Logs) Debug.Log("[Settings]: Loading from file");

        if (!File.Exists(Path)) return Data.Default;

        Deserializer deserializer = new(File.ReadAllBytes(Path));
        return deserializer.DeserializeObject<Data>();
    }
}

public static class GameConfigManager
{
    const string CONFIG_FILE = "config.json";
    const string BASE_URI = "http://192.168.1.100:7777";

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

        Debug.Log($"[{nameof(GameConfigManager)}]: Config downloaded:" +
            $"{{\n" +
            $"  assets_path: '{result.assets_path}'\n" +
            $"  netcode_tcpServer_port: '{result.netcode_tcpServer_port}'\n" +
            $"  test_object: '{result.test_object}'\n" +
            $"  test_scene: '{result.test_scene}'\n" +
            $"}}"
        );
        Debug.Log($"[{nameof(GameConfigManager)}]: Modify 'assets_path' to '{BASE_URI + "/assets.bin"}'");
        result.assets_path = BASE_URI + "/assets.bin";
        callback?.Invoke(result);
        yield break;
#else
        // Debug.Log($"[{nameof(GameConfigManager)}]: Loading config file from {Path} ...");

        if (!File.Exists(Path))
        {
            // Debug.Log($"[{nameof(GameConfigManager)}]: Config file does not exists. Creating new one ...");

            SetDefaults();
            callback?.Invoke(GameConfig.Default);
            yield break;
        }
        string Json = File.ReadAllText(Path);
        if (string.IsNullOrEmpty(Json) || string.IsNullOrWhiteSpace(Json))
        {
            // Debug.Log($"[{nameof(GameConfigManager)}]: Config file is empty. Creating new one ...");

            SetDefaults();
            callback?.Invoke(GameConfig.Default);
            yield break;
        }
        GameConfig result = JsonUtility.FromJson<GameConfig>(Json);

        /*
        Debug.Log($"[{nameof(GameConfigManager)}]: Config loaded:\n" +
            $"{{\n" +
            $"  assets_path: '{result.assets_path}'\n" +
            $"  netcode_tcpServer_port: '{result.netcode_tcpServer_port}'\n" +
            $"  test_object: '{result.test_object}'\n" +
            $"  test_scene: '{result.test_scene}'\n" +
            $"}}"
        );
        */

        callback?.Invoke(result);
        yield break;
#endif
    }

    /*
    public static Data Get()
    {
#if PLATFORM_WEBGL && DOWNLOAD_ASSETS
        return Data.Default;
#else
        if (!File.Exists(Path))
        {
            SetDefaults();
            return Data.Default;
        }
        var Json = File.ReadAllText(Path);
        if (string.IsNullOrEmpty(Json) || string.IsNullOrWhiteSpace(Json))
        {
            SetDefaults();
            return Data.Default;
        }
        return JsonUtility.FromJson<Data>(Json);
#endif
    }
    */

    public static void SetDefaults()
    {
#if !PLATFORM_WEBGL
        if (!Directory.Exists(Application.streamingAssetsPath))
        { Directory.CreateDirectory(Application.streamingAssetsPath); }
        var DefaultData = GameConfig.Default;
        var Json = JsonUtility.ToJson(DefaultData);
        File.WriteAllText(Path, Json);
#endif
    }

    [Serializable]
    public struct GameConfig
    {
        public ushort netcode_tcpServer_port;
        public string assets_path;
        public string test_object;
        public string test_scene;

        public static GameConfig Default => new()
        {
            netcode_tcpServer_port = 7779,
#if PLATFORM_WEBGL && DOWNLOAD_ASSETS
            assets_path = BASE_URI + "/assets.bin",
#else
            assets_path = "C:\\Users\\bazsi\\Desktop\\Nothing Assets 3D\\",
#endif
            test_object = "player_apc",
            test_scene = "test",
        };
    }
}