using System.IO;

using UnityEngine;

namespace AssetManager.Components
{
    public class PrefabPacker : MonoBehaviour
    {
        [Button(nameof(Pack), true, true, "Pack"), SerializeField] string btn_pack;

        void Pack()
        {
#if UNITY_EDITOR
            var data = AssetManager.PackPrefab(gameObject);
            var text = data.ToSDF(false);
            File.WriteAllText(Path.Combine(@"C:\Users\bazsi\Desktop\Nothing Assets 3D", "Packed Objects", gameObject.name + ".obj.sdf"), text);
#endif
        }
    }
}
