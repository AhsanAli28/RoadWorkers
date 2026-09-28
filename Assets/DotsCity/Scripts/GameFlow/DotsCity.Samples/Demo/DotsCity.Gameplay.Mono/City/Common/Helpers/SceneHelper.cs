using UnityEngine.SceneManagement;

namespace Spirit604.Utils
{
    public static class SceneHelper
    {
        public static string NameFromIndex(int BuildIndex)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(BuildIndex);
            int slash = path.LastIndexOf('/');
            string name = path.Substring(slash + 1);
            int dot = name.LastIndexOf('.');
            return name.Substring(0, dot);
        }

        public static int SceneIndexFromName(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string testedScreen = NameFromIndex(i);

                if (testedScreen == sceneName)
                    return i;
            }
            return -1;
        }
    }
}
