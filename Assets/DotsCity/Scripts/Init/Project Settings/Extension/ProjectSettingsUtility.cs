using System;
using UnityEditor;

namespace Spirit604.PackageManagerExtension
{
    public static class ProjectSettingsUtility
    {
        public static string ReplaceConstantRow(string sourceString, int newVal)
        {
            var firstIndex = sourceString.LastIndexOf("= ");
            var lastIndex = sourceString.LastIndexOf(";");

            var sourceReplace = sourceString.Substring(firstIndex, lastIndex - firstIndex + 1);

            var newReplace = $"= {newVal};";

            return sourceString.Replace(sourceReplace, newReplace);
        }

        public static SerializedProperty FindProperty(SerializedObject obj, string name)
        {
            SerializedProperty iterator = obj.GetIterator();

            while (iterator.NextVisible(true))
            {
                if (iterator.name == name)
                    return iterator;
            }

            return null;
        }

        public static string[] GetStrings(MonoScript monoScript)
        {
            string[] lines = monoScript.text.Split(
              new string[] { "\r\n", "\r", "\n" },
              StringSplitOptions.None);

            return lines;
        }
    }
}
