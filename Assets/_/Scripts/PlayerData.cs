using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : SingleInstance<PlayerData>
{
    [Serializable]
    public class ProducableUnit
    {
        [SerializeField, ReadOnly] public uint Hash;
        [SerializeField] public GameObject Unit;
        [SerializeField] public float ProgressRequied;
        [SerializeField] public string ThumbnailID;
    }

    [Serializable]
    public class ConstructableBuilding
    {
        [SerializeField, ReadOnly] public uint Hash;
        [SerializeField] public Vector3 SpaceNeed = Vector3.one;
        [SerializeField] public GameObject Building;
        [SerializeField] public float ProgressRequied;
        [SerializeField] public string ThumbnailID;
        public Vector3 GroundOrigin
        {
            get
            {
                if (Building == null) return default;
                if (!Building.TryGetComponent<Game.Components.Building>(out var building)) return default;
                return building.GroundOrigin;
            }
        }
    }

    [SerializeField] public string Team;

    [SerializeField] public List<ProducableUnit> ProducableUnits = new();
    [SerializeField] public List<ConstructableBuilding> ConstructableBuildings = new();
    [SerializeField, Button(nameof(GenerateHashes), true, false, "Generate Hashes")] string btn_GenHash;

#nullable enable

    bool IsHashUnique(uint hash)
    {
        if (hash == 0) return false;
        for (int i = 0; i < ProducableUnits.Count; i++)
        { if (ProducableUnits[i].Hash == hash) return false; }
        for (int i = 0; i < ConstructableBuildings.Count; i++)
        { if (ConstructableBuildings[i].Hash == hash) return false; }
        return true;
    }

    void GenerateHashes()
    {
        for (int i = 0; i < ProducableUnits.Count; i++)
        {
            if (!IsHashUnique(ProducableUnits[i].Hash))
            {
                ProducableUnits[i].Hash = unchecked((uint)ProducableUnits[i].Unit.name.GetHashCode(StringComparison.InvariantCulture));
            }
        }
        for (int i = 0; i < ConstructableBuildings.Count; i++)
        {
            if (!IsHashUnique(ConstructableBuildings[i].Hash))
            {
                ConstructableBuildings[i].Hash = unchecked((uint)ConstructableBuildings[i].Building.name.GetHashCode(StringComparison.InvariantCulture));
            }
        }
    }

    public static PlayerData? GetCurrentPlayerData()
    {
        PlayerData[] playerDatas = GameObject.FindObjectsOfType<PlayerData>();
        for (int i = 0; i < playerDatas.Length; i++)
        { if (playerDatas[i].Team == instance.Team) return playerDatas[i]; }
        return null;
    }

    public static PlayerData? GetPlayerData(string team)
    {
        PlayerData[] playerDatas = GameObject.FindObjectsOfType<PlayerData>();
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
