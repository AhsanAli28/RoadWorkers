#if UNITY_EDITOR
using Spirit604.CityEditor;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Doc
{
    public static class DotsDocumentationReference
    {
        public const string DocUrl = "https://dotstrafficcity.readthedocs.io/en/latest/index.html";
        public const string DocUrlGettingStarted = "https://dotstrafficcity.readthedocs.io/en/latest/packageInstallation.html#package-installation";

        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "Documentation", priority = 101)]
        public static void OpenDocumentation()
        {
            Application.OpenURL(DocUrl);
        }
    }
}
#endif