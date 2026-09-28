using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Factory.Car
{
    public static class VehicleCollectionExtension
    {
        public static string GetVehicleID(UnityEngine.Object sourceVehicle)
        {
            return GetVehicleID(sourceVehicle as GameObject);
        }

        public static string GetVehicleID(GameObject sourceVehicle)
        {
            if (!sourceVehicle)
            {
                return string.Empty;
            }

            var idProvider = sourceVehicle.GetComponent<ICarIDProvider>();

            if (idProvider != null)
            {
                return idProvider.ID;
            }

            return string.Empty;
        }

        public static int GetVehicleModelIndex(this VehicleDataCollection vehicleDataCollection, GameObject sourceVehicle)
        {
            if (!vehicleDataCollection)
            {
                return -1;
            }

            var id = GetVehicleID(sourceVehicle);
            var index = vehicleDataCollection.GetCarModelIndexByID(id);

            return index;
        }

        public static int[] GetVehicleIds(this VehicleDataCollection vehicleDataCollection, List<CarPrefabPair> prefabs)
        {
            List<int> tempIndexes = new List<int>();

            foreach (var prefab in prefabs)
            {
                var modelIndex = vehicleDataCollection.GetVehicleModelIndex(prefab.EntityPrefab);

                if (modelIndex != -1)
                {
                    tempIndexes.Add(modelIndex);
                }
            }

            return tempIndexes.ToArray();
        }

        public static string[] GetVehicleNames(this VehicleDataCollection vehicleDataCollection, List<CarPrefabPair> prefabs)
        {
            List<string> tempOptions = new List<string>();

            foreach (var prefab in prefabs)
            {
                var modelIndex = vehicleDataCollection.GetVehicleModelIndex(prefab.EntityPrefab);

                if (modelIndex != -1)
                {
                    var vehicleName = vehicleDataCollection.GetName(modelIndex);

                    tempOptions.Add(vehicleName);
                }
            }

            return tempOptions.ToArray();
        }

#if UNITY_EDITOR
        public static void DrawModelOptions(VehicleDataCollection vehicleDataCollection, SerializedProperty indexProperty)
        {
            if (vehicleDataCollection != null)
            {
                if (vehicleDataCollection.Options?.Length > 0)
                {
                    var newValue = EditorGUILayout.Popup("Car Model", indexProperty.intValue, vehicleDataCollection.Options);

                    if (indexProperty.intValue != newValue)
                    {
                        indexProperty.intValue = newValue;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Vehicle collection is empty", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(indexProperty);
            }
        }

        public static void DrawModelOptions(Rect rect, VehicleDataCollection vehicleDataCollection, SerializedProperty indexProperty)
        {
            if (vehicleDataCollection != null)
            {
                if (vehicleDataCollection.Options?.Length > 0)
                {
                    var newValue = EditorGUI.Popup(rect, "Car Model", indexProperty.intValue, vehicleDataCollection.Options);

                    if (indexProperty.intValue != newValue)
                    {
                        indexProperty.intValue = newValue;
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Vehicle collection is empty", MessageType.Info);
                }
            }
            else
            {
                EditorGUI.PropertyField(rect, indexProperty);
            }
        }
#endif
    }
}