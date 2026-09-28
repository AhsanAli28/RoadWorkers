using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using UnityEditor;

namespace Spirit604.DotsCity.Core
{
    class ProjectFirstBootstrap : AssetPostprocessor
    {
        private const string ProjectFirstBoostrapKey = "ProjectFirstBoostrap";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            var key = EditorExtension.GetUniquePrefsKey(ProjectFirstBoostrapKey);

            if (!EditorPrefs.HasKey(key))
            {
                EditorPrefs.SetBool(key, true);
                TrafficGroupMaskSettings.Init();
            }
        }
    }
}
