#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using UnityEditor;

namespace Spirit604.CityEditor.Road
{
    public class TrafficGroupMaskSettingsEditorInit : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (!TrafficGroupMaskSettings.Loaded || !TrafficGroupMaskSettings.HasSettings)
            {
                var settings = TrafficGroupMaskSettings.Init();

                if (settings != null) TrafficGroupMaskSettings.Loaded = true;
            }
        }
    }
}
#endif