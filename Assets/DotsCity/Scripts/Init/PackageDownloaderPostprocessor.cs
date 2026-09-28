#if UNITY_EDITOR
using UnityEditor;

namespace Spirit604.PackageManagerExtension
{
    class PackageDownloaderPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload)
            {
                PackageDownloader.OnLoadMethod();

#if DOTS_CITY
                UpdateManager.CheckForUpdate();
#endif
            }
        }
    }
}
#endif