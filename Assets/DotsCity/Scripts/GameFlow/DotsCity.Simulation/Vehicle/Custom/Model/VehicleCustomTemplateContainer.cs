using Spirit604.CityEditor;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Custom.Authoring
{
    [CreateAssetMenu(fileName = VehicleCustomTemplateContainer.DefaultName, menuName = CityEditorBookmarks.CITY_EDITOR_TRAFFIC_EDITOR_CONFIGS_PATH + VehicleCustomTemplateContainer.DefaultName)]
    public class VehicleCustomTemplateContainer : ScriptableObject
    {
        public const string LoadPath = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Traffic/" + VehicleCustomTemplateContainer.DefaultName;
        public const string DefaultName = "VehicleCustomTemplateContainer";

        public List<VehicleCustomTemplate> Templates = new List<VehicleCustomTemplate>();

#if UNITY_EDITOR
        private static VehicleCustomTemplateContainer container;

        public static VehicleCustomTemplateContainer GetContainer()
        {
            if (!container)
            {
                var path = $"{CityEditorBookmarks.VEHICLE_TEMPLATE_PATH}{DefaultName}.asset";
                container = AssetDatabase.LoadAssetAtPath<VehicleCustomTemplateContainer>(path);
            }

            return container;
        }

        public static void LoadPresets(out VehicleCustomTemplate[] templates, out string[] templateHeaders)
        {
            templates = null;
            templateHeaders = null;

            var container = GetContainer();

            if (container && container.Templates.Count > 0)
            {
                templates = container.Templates.ToArray();
                templateHeaders = container.Templates.Select(a => a.TemplateName).ToArray();
            }
        }
#endif

        public void AddTemplate(VehicleCustomTemplate template)
        {
            if (Templates.TryToAdd(template))
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void RemoveTemplate(VehicleCustomTemplate template)
        {
            if (Templates.TryToRemove(template))
            {
                EditorSaver.SetObjectDirty(this);
            }
        }
    }
}