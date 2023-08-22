#if UNITY_EDITOR
using UnityEngine;
using System.IO;
using UnityEditor.AssetImporters;

[ScriptedImporter(1, "css")]
public class SrtImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext context)
    {
        TextAsset subAsset = new(File.ReadAllText(context.assetPath));
        context.AddObjectToAsset("text", subAsset);
        context.SetMainObject(subAsset);
    }
}

#endif