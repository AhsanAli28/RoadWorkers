#if UNITY_EDITOR
using Spirit604.CityEditor;
using Spirit604.DotsCity.Installer;
using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEditor;

namespace Spirit604.DotsCity.Initialization.Installer
{
    public class ProjectContextSwitcherPostprocessor : AssetPostprocessor
    {
        private const string ProjectContextSwitcherKey = "ProjectContextSwitcherKey";

        private static readonly List<string> HubObjectNames = new List<string>()
        {
            "HubBase.prefab",
#if !DOTS_SIMULATION
            "Hub.prefab"
#endif
        };

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (!didDomainReload)
                return;

            var key = EditorExtension.GetUniquePrefsKey(ProjectContextSwitcherKey);

            bool initialized = EditorPrefs.HasKey(key);
            var installed = EditorPrefs.GetBool(key, false);

            bool zenjectInstalled = false;

#if ZENJECT
            zenjectInstalled = true;
#endif

            bool switchContext = false;

            if (!initialized)
            {
                if (!zenjectInstalled)
                {
                    switchContext = true;
                }
                else
                {
                    EditorPrefs.SetBool(key, zenjectInstalled);
                }
            }
            else
            {
                switchContext = zenjectInstalled != installed;
            }

            if (switchContext)
            {
                for (int i = 0; i < HubObjectNames.Count; i++)
                {
                    var hubObjectName = HubObjectNames[i];

                    PrefabExtension.EditPrefab(CityEditorBookmarks.CITY_BASE_PATH + hubObjectName, (hub) =>
                    {
                        var projectContextSwitcher = hub.GetComponentInChildren<ProjectContextSwitcher>();

                        if (projectContextSwitcher)
                        {
                            if (projectContextSwitcher.SwitchContext(zenjectInstalled) && i == 0)
                            {
                                EditorPrefs.SetBool(key, zenjectInstalled);
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"ProjectContextSwitcherPostprocessor. Trying to switch context, but ProjectContextSwitcher not found in {hubObjectName}.");
                        }
                    });
                }
            }
        }
    }
}
#endif