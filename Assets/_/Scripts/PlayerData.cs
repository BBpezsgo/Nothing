using System;
using System.Collections.Generic;

using UnityEngine;

public class PlayerData : MonoBehaviour
{
    [Serializable]
    internal class ProducableUnit
    {
        [SerializeField, ReadOnly] internal uint Hash;
        [SerializeField] internal GameObject Unit;
        [SerializeField] internal float ProgressRequied;
        [SerializeField] internal string ThumbnailID;
    }

    [Serializable]
    internal class ConstructableBuilding
    {
        [SerializeField, ReadOnly] internal uint Hash;
        [SerializeField] internal Vector3 SpaceNeed = Vector3.one;
        [SerializeField] internal GameObject Building;
        [SerializeField] internal float ProgressRequied;
        [SerializeField] internal string ThumbnailID;
        internal Vector3 GroundOrigin
        {
            get
            {
                if (Building == null) return Vector3.zero;
                if (!Building.TryGetComponent<Game.Components.Building>(out var building)) return Vector3.zero;
                return building.GroundOrigin;
            }
        }
    }

    [SerializeField] internal string Team;

    [SerializeField] internal List<ProducableUnit> ProducableUnits = new();
    [SerializeField] internal List<ConstructableBuilding> ConstructableBuildings = new();
    [SerializeField, Button(nameof(GenerateHashes), true, false, "Generate Hashes")] string btn_GenHash;

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

    internal static PlayerData GetPlayerData(string team)
    {
        PlayerData[] playerDatas = GameObject.FindObjectsOfType<PlayerData>();
        for (int i = 0; i < playerDatas.Length; i++)
        { if (playerDatas[i].Team == team) return playerDatas[i]; }
        return null;
    }

    internal static bool TryGetThumbnail(string id, out Texture2D thumbnail)
    {
        string path = $"Thumbnails/{id}";
        thumbnail = Resources.Load<Texture2D>(path);
        return thumbnail != null;
    }
}
