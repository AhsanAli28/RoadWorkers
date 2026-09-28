#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions.Common
{
    public class PlayerBuildExtension : MonoBehaviour
    {
        public static bool TryToAddDefineToCurrentBuild(string define, bool showWarnings = true)
        {
            return TryToAddDefineToBuild(EditorUserBuildSettings.selectedBuildTargetGroup, define, showWarnings);
        }

        public static bool TryToAddDefineToCurrentBuilds(string define, bool showWarnings = true)
        {
            var opt1 = TryToAddDefineToBuild(BuildTargetGroup.Android, define, showWarnings);
            var opt2 = TryToAddDefineToBuild(BuildTargetGroup.iOS, define, showWarnings);
            var opt3 = TryToAddDefineToBuild(BuildTargetGroup.Standalone, define, showWarnings);

            return opt1 || opt2 || opt3;
        }

        public static bool TryToAddDefineToBuild(BuildTargetGroup buildTargetGroup, string define, bool showWarnings = true)
        {
            string defines = string.Empty;

#if UNITY_2022_3_OR_NEWER
            defines = PlayerSettings.GetScriptingDefineSymbols(GetNamedBuildTarget(buildTargetGroup));
#else
            defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
#endif

            // Append only if not defined already.
            if (defines.Contains(define))
            {
                if (showWarnings)
                {
                    Debug.LogWarning("Selected build target (" + buildTargetGroup.ToString() + ") already contains <b>" + define + "</b> <i>Scripting Define Symbol</i>.");
                }

                return false;
            }

            var newDefines = (defines + ";" + define);

            // Append.
#if UNITY_2022_3_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(GetNamedBuildTarget(buildTargetGroup), newDefines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newDefines);
#endif

            if (showWarnings)
            {
                Debug.LogWarning("<b>" + define + "</b> added to <i>Scripting Define Symbols</i> for selected build target (" + buildTargetGroup.ToString() + ").");
            }

            return true;
        }

#if UNITY_2022_3_OR_NEWER
        private static UnityEditor.Build.NamedBuildTarget GetNamedBuildTarget(BuildTargetGroup group)
        {
            switch (group)
            {
                case BuildTargetGroup.Unknown:
                    return UnityEditor.Build.NamedBuildTarget.Unknown;
                case BuildTargetGroup.Standalone:
                    return UnityEditor.Build.NamedBuildTarget.Standalone;
                case BuildTargetGroup.iOS:
                    return UnityEditor.Build.NamedBuildTarget.iOS;
                case BuildTargetGroup.Android:
                    return UnityEditor.Build.NamedBuildTarget.Android;
                case BuildTargetGroup.WebGL:
                    return UnityEditor.Build.NamedBuildTarget.WebGL;
                case BuildTargetGroup.PS4:
                    return UnityEditor.Build.NamedBuildTarget.PS4;
#if UNITY_2023_1_OR_NEWER
                case BuildTargetGroup.PS5:
                    return UnityEditor.Build.NamedBuildTarget.PS5;
#endif
            }

            return default;
        }
#endif
    }
}
#endif