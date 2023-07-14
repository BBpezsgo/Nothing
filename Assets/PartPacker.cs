using Blueprints;

using DataUtilities.ReadableFileFormat;

using System.IO;

using UnityEngine;

public class PartPacker : MonoBehaviour
{
    public PartType Type;
    [SerializeField, Button(nameof(Pack), true, true, "Pack")] string btnPack;
    const string DESKTOP = @"C:\Users\bazsi\Desktop";
    const string ASSETS = DESKTOP + @"\Nothing Assets 3D";

    void Pack()
    {
#if UNITY_EDITOR
        Value data = BlueprintManager.PackPart(this);
        string text = data.ToSDF(false);
        string filename = Utils.FixFilename(Utils.NormalizeID(gameObject.name));

        File.WriteAllText(Path.Combine(ASSETS, "Packed Parts", filename + "-data.sdf"), text);

        if (!File.Exists(Path.Combine(ASSETS, "Parts", filename + ".sdf")))
        {
            Value initialData = Value.Object();
            initialData["Base"] = filename + "-data";
            File.WriteAllText(Path.Combine(ASSETS, "Parts", filename + ".sdf"), initialData.ToSDF());
        }
#endif
    }
}
