using UnityEngine;

public class PlayerData : SingleInstance<PlayerData>
{
    [SerializeField] public string Team;
    [SerializeField] public StaticPlayerData Data;

#nullable enable

    public static PlayerData? GetCurrentPlayerData()
    {
        PlayerData[] playerDatas = GameObject.FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        for (int i = 0; i < playerDatas.Length; i++)
        { if (playerDatas[i].Team == Instance.Team) return playerDatas[i]; }
        return null;
    }

    public static PlayerData? GetPlayerData(string team)
    {
        PlayerData[] playerDatas = GameObject.FindObjectsByType<PlayerData>(FindObjectsSortMode.None);
        for (int i = 0; i < playerDatas.Length; i++)
        { if (playerDatas[i].Team == team) return playerDatas[i]; }
        return null;
    }

    public static bool TryGetThumbnail(string id, out Texture2D thumbnail)
    {
        string path = $"Thumbnails/{id}";
        thumbnail = Resources.Load<Texture2D>(path);
        return thumbnail != null;
    }
}
