using System.IO;

using UnityEngine;

namespace AssetManager
{
    public class AssetBundleManager : MonoBehaviour
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Build AssetBundles")]
        static void BuildAllAssetBundles()
        {
            string assetBundleDirectory = "Assets/StreamingAssets";
            if (!Directory.Exists(assetBundleDirectory))
            { Directory.CreateDirectory(assetBundleDirectory); }
            UnityEditor.BuildPipeline.BuildAssetBundles(assetBundleDirectory, UnityEditor.BuildAssetBundleOptions.None, UnityEditor.BuildTarget.StandaloneWindows);
        }
#endif
    }
}
