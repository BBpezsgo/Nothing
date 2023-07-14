using System;
using System.Collections.Generic;

using UnityEngine;

public class PlayerData : MonoBehaviour
{
    [Serializable]
    internal class ProducableUnit
    {
        [SerializeField] internal GameObject Unit;
        [SerializeField] internal float ProgressRequied;
    }

    [Serializable]
    internal class ConstructableBuilding
    {
        [SerializeField] internal Vector3 SpaceNeed = Vector3.one;
        [SerializeField] internal GameObject Building;
        [SerializeField] internal float ProgressRequied;
        internal Vector3 GroundOrigin
        {
            get
            {
                if (Building == null) return Vector3.zero;
                if (!Building.TryGetComponent<Building>(out var building)) return Vector3.zero;
                return building.GroundOrigin;
            }
        }
    }

    [SerializeField] internal string Team;

    [SerializeField] internal List<ProducableUnit> ProducableUnits = new();
    [SerializeField] internal List<ConstructableBuilding> ConstructableBuildings = new();

    internal static PlayerData GetPlayerData(string team)
    {
        PlayerData[] playerDatas = GameObject.FindObjectsOfType<PlayerData>();
        for (int i = 0; i < playerDatas.Length; i++)
        { if (playerDatas[i].Team == team) return playerDatas[i]; }
        return null;
    }
}
