#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Spirit604.DotsCity.Installer
{
    public static class InspectorExtension
    {
        public static void RebindInspector(Component sourceComponent, GameObject root)
        {
            var fields = sourceComponent.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            bool changed = false;

            foreach (var field in fields)
            {
                var attrs = field.GetCustomAttributes<EditorResolveAttribute>();

                if (attrs != null && attrs.Count() > 0)
                {
                    var value = field.GetValue(sourceComponent);

                    if (value == null)
                    {
                        var attr = attrs.FirstOrDefault();

                        var type = field.FieldType;
                        var comp = root.GetComponentInChildren(type);

                        if (comp != null)
                        {
                            field.SetValue(sourceComponent, comp);
                            changed = true;
                        }
                        else if (!attr.Optional)
                        {
                            UnityEngine.Debug.Log($"InspectorExtension.RebindInspector {sourceComponent.name} component '{type.Name}' not found.");
                        }
                    }
                }
            }

            if (changed)
            {
                UnityEditor.EditorUtility.SetDirty(sourceComponent);
            }
        }
    }
}
#endif
