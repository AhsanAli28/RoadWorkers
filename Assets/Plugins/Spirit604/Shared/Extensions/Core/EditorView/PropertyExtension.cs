#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Spirit604.Extensions
{
    public static class PropertyExtension
    {
        public static T GetPropertyInstance<T>(SerializedProperty property)
        {
            string path = property.propertyPath;

            System.Object obj = property.serializedObject.targetObject;
            var type = obj.GetType();

            var fieldNames = path.Split('.');
            for (int i = 0; i < fieldNames.Length; i++)
            {
                var info = type.GetField(fieldNames[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (info == null)
                    break;

                // Recurse down to the next nested object.
                obj = info.GetValue(obj);

                if (obj is IEnumerable<T>)
                {
                    var arrayDataIndex = fieldNames.Length - 1;
                    int startIndex = fieldNames[arrayDataIndex].IndexOf('[');
                    int endIndex = fieldNames[arrayDataIndex].IndexOf(']');
                    var textIndex = fieldNames[arrayDataIndex].Substring(startIndex + 1, endIndex - startIndex - 1);
                    int arrayIndex = int.Parse(textIndex);

                    return (obj as IEnumerable<T>).ElementAt(arrayIndex);
                }

                type = info.FieldType;
            }

            return (T)obj;
        }
    }
}
#endif
