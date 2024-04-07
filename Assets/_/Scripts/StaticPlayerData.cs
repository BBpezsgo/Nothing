using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Static Player Data", fileName = "StaticPlayerData")]
public class StaticPlayerData : ScriptableObject
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
                if (!Building.TryGetComponent<Game.Components.Building>(out Game.Components.Building building)) return default;
                return building.GroundOrigin;
            }
        }
    }

    [SerializeField] public List<ProducableUnit> ProducableUnits = new();
    [SerializeField] public List<ConstructableBuilding> ConstructableBuildings = new();
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

}
