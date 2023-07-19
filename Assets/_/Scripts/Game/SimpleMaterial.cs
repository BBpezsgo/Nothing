using AssetManager;

using UnityEngine;

namespace Game.Components
{
    public class SimpleMaterial : MonoBehaviour, IObjectMaterial, IHaveAssetFields
    {
        [field: SerializeField]
        [AssetProperty]
        public string Material { get; set; }

        [field: SerializeField, Range(0f, 1f)]
        [AssetProperty]
        public float Hardness { get; set; }

        [field: SerializeField, Range(0f, 1f)]
        [AssetProperty]
        public float BlastAbsorptionCapacity { get; set; }
    }
}
