using System.IO;

#if UNITY_EDITOR
namespace AssetManager
{
    public static class AssetBundleManager
    {
        [UnityEditor.MenuItem("Assets/Build AssetBundles")]
        public static void BuildAllAssetBundles()
        {
            string assetBundleDirectory = "Assets/StreamingAssets";
            if (!Directory.Exists(assetBundleDirectory))
            { Directory.CreateDirectory(assetBundleDirectory); }
            UnityEditor.BuildPipeline.BuildAssetBundles(assetBundleDirectory, UnityEditor.BuildAssetBundleOptions.None, UnityEditor.BuildTarget.StandaloneWindows);
        }
    }
}
#endif
