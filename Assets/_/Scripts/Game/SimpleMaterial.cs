using System;
using UnityEngine;

namespace Game.Components
{
    public class SimpleMaterial : MonoBehaviour, IObjectMaterial
    {
        [field: SerializeField]
        public string Material { get; set; }

        [field: SerializeField, Range(0f, 1f)]
        public float Hardness { get; set; }

        [field: SerializeField, Range(0f, 1f)]
        public float BlastAbsorptionCapacity { get; set; }

        public void CopyFrom(IObjectMaterial other)
        {
            Material = other.Material;
            Hardness = other.Hardness;
            BlastAbsorptionCapacity = other.BlastAbsorptionCapacity;
        }
    }
}
