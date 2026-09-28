using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class EditorEnumExtension
    {
        private const string fileExtension = ".cs";

        public static void AddToEnum<T>(string path, string fileName, string newTypeName) where T : Enum
        {
            Dictionary<string, int> enumDict = GetEnumDictionary<T>(newTypeName);
            WriteToFile(path, fileName, enumDict);
        }

        public static void SaveEnums<T>(IEnumerable<string> names, IEnumerable<int> flagArray, bool flags = false) where T : Enum
        {
            var fileName = typeof(T).Name;
            var fileNamespace = typeof(T).Namespace;

            string[] res = Directory.GetFiles(Application.dataPath, $"{fileName}.cs", SearchOption.AllDirectories);

            if (res.Length == 0)
            {
                Debug.LogError($"Enum {fileName}.cs not found");
                return;
            }

            string path = res[0].Replace($"{fileName}.cs", "").Replace("\\", "/");

            SaveEnums<T>(path, fileNamespace, fileName, names, flagArray, flags);
        }

        public static void SaveEnums<T>(string path, string fileNamespace, string fileName, IEnumerable<string> names, IEnumerable<int> flagArray, bool flags = false) where T : Enum
        {
            var arr1 = names.ToArray();
            int[] arr2 = null;

            if (!flags)
            {
                arr2 = flagArray.ToArray();
            }
            else
            {
                arr2 = flagArray.Select(a => FlagToLocal(a)).ToArray();
            }

            if (arr1.Length != arr2.Length)
            {
                throw new ArgumentException("Names and values not matched");
            }

            var enumDict = new Dictionary<string, int>(arr1.Length);

            for (int i = 0; i < arr1.Length; i++)
            {
                enumDict.Add(arr1[i], arr2[i]);
            }

            WriteToFile(path, fileName, enumDict, fileNamespace, flags);
        }

        public static void AddToEnum<T>(string path, string fileName, List<string> newTypeNames) where T : Enum
        {
            Dictionary<string, int> enumDict = GetEnumDictionary<T>(newTypeNames);
            WriteToFile(path, fileName, enumDict);
        }

        public static string ReplaceWhiteSpaces(string str)
        {
            str = str.Trim();
            str = str.Replace(" ", "_");

            return str;
        }

        public static void DeleteEnumType<T>(string path, string fileName, string typeName) where T : Enum
        {
            Dictionary<string, int> enumDict;
            int newValue;
            FillEnumDictionary<T>(out enumDict, out newValue);

            if (enumDict.ContainsKey(typeName))
            {
                enumDict.Remove(typeName);
            }

            WriteToFile(path, fileName, enumDict);
        }

        public static int GetClosestEmptyFlag(IEnumerable<int> flags) => GetClosestEmptyFlag(flags, out var index);

        public static int GetClosestEmptyFlag(IEnumerable<int> flags, out int localIndex)
        {
            localIndex = -1;

            for (int i = 0; i < sizeof(int) * 8; i++)
            {
                var flag = 1 << i;

                if (!flags.Contains(flag))
                {
                    localIndex = i;
                    return flag;
                }
            }

            return -1;
        }

        private static void WriteToFile(string path, string name, Dictionary<string, int> enumDict, string fileNamespace = "", bool flags = false)
        {
            bool hasNamespace = !string.IsNullOrEmpty(fileNamespace);

            string fullPath = path + name + fileExtension;

            int tabs = hasNamespace ? 1 : 0;

            using (StreamWriter file = new StreamWriter(fullPath, false))
            {
                if (hasNamespace)
                {
                    file.WriteLine($"namespace {fileNamespace}");
                    file.WriteLine($"{{");
                }

                if (flags)
                {
                    WriteLine(file, "[System.Flags]", tabs);
                }

                WriteLine(file, "public enum " + name, tabs);
                WriteLine(file, "{", tabs);

                foreach (var line in enumDict)
                {
                    string lineRep = line.ToString().Replace(" ", string.Empty);

                    if (!string.IsNullOrEmpty(lineRep))
                    {
                        string str = string.Empty;

                        if (!flags)
                        {
                            str = string.Format("{0} = {1},", line.Key, line.Value);
                        }
                        else
                        {
                            str = string.Format("{0} = 1 << {1},", line.Key, line.Value);
                        }

                        WriteLine(file, str, tabs + 1);
                    }
                }

                if (hasNamespace)
                {
                    WriteLine(file, "}", tabs);
                    file.WriteLine($"}}");
                }
                else
                {
                    WriteLine(file, $"}}", tabs);
                }

                WriteLine(file, $"{Environment.NewLine}");
            }

            var localPath = AssetDatabaseExtension.FullPathToLocal(path);
            AssetDatabase.ImportAsset(localPath + name + fileExtension, ImportAssetOptions.ForceUpdate);
        }

        private static void WriteLine(StreamWriter file, string text, int tabCount = 0)
        {
            string tabs = string.Empty;

            for (int i = 0; i < tabCount; i++)
            {
                tabs += "\t";
            }

            file.WriteLine($"{tabs}{text}");
        }

        private static Dictionary<string, int> GetEnumDictionary<T>(List<string> newTypeNames) where T : Enum
        {
            Dictionary<string, int> enumDict;
            int newValue;
            FillEnumDictionary<T>(out enumDict, out newValue);

            for (int i = 0; i < newTypeNames.Count; i++)
            {
                newTypeNames[i] = ReplaceWhiteSpaces(newTypeNames[i]);

                if (!enumDict.ContainsKey(newTypeNames[i]))
                {
                    enumDict.Add(newTypeNames[i], newValue++);
                }
            }

            return enumDict;
        }

        private static void FillEnumDictionary<T>(out Dictionary<string, int> enumDict, out int newValue) where T : Enum
        {
            var names = Enum.GetNames(typeof(T));

            enumDict = new Dictionary<string, int>();
            newValue = 0;

            string str = string.Empty;

            if (names?.Count() > 0)
            {
                var values = Enum.GetValues(typeof(T)).Cast<int>().ToList();

                for (int i = 0; i < names.Length; i++)
                {
                    enumDict.Add(names[i], values[i]);
                    str += names[i] + " ";
                }

                enumDict = enumDict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

                newValue = values.Max();
                newValue = ++newValue;
            }
        }

        private static int FlagToLocal(int flag)
        {
            for (int i = 0; i < sizeof(int) * 8; i++)
            {
                var number = 1 << i;

                if (number == flag)
                {
                    return i;
                }
            }

            return -1;
        }

        private static Dictionary<string, int> GetEnumDictionary<T>(string newTypeName) where T : Enum
        {
            Dictionary<string, int> enumDict;
            int newValue;
            FillEnumDictionary<T>(out enumDict, out newValue);

            newTypeName = ReplaceWhiteSpaces(newTypeName);

            if (!enumDict.ContainsKey(newTypeName))
            {
                enumDict.Add(newTypeName, newValue);
            }

            return enumDict;
        }
    }
}
#endif