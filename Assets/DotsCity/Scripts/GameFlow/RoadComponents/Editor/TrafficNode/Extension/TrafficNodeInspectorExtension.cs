using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using UnityEditor;

namespace Spirit604.CityEditor.Road
{
    public static class TrafficNodeInspectorExtension
    {
        public static void DrawInspectorSettings(TrafficNode trafficNode, List<TrafficNode> cloneList = null)
        {
            if (trafficNode == null)
            {
                return;
            }

            var nodeSo = new SerializedObject(trafficNode);
            nodeSo.Update();

            DrawClonedProperty(trafficNode, nodeSo, "laneCount", cloneList);
            DrawClonedProperty(trafficNode, nodeSo, "laneWidth", cloneList);
            DrawClonedProperty(trafficNode, nodeSo, "chanceToSpawn", cloneList);
            DrawClonedProperty(trafficNode, nodeSo, "weight", cloneList);
            DrawClonedProperty(trafficNode, nodeSo, "customAchieveDistance", cloneList);
            DrawClonedProperty(trafficNode, nodeSo, "trafficNodeType", cloneList);
            DrawClonedProperty(trafficNode, nodeSo, "hasCrosswalk", cloneList);
            DrawClonedProperty(trafficNode, nodeSo, "isOneWay", cloneList);
            DrawClonedProperty(trafficNode, nodeSo, "isEndOfOneWay", cloneList);
            DrawClonedProperty(trafficNode, nodeSo, "lockPathAutoCreation", cloneList);
            DrawClonedProperty(trafficNode, nodeSo, "autoPathIsCreated", cloneList);

            nodeSo.ApplyModifiedProperties();
        }

        private static void DrawClonedProperty(TrafficNode srcNode, SerializedObject nodeSo, string propName, List<TrafficNode> cloneList = null)
        {
            var prop = nodeSo.FindProperty(propName);

            if (prop == null)
            {
                UnityEngine.Debug.Log($"TrafficNodeInspectorExtension. Property {propName} not found.");
                return;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(prop);

            if (EditorGUI.EndChangeCheck())
            {
                if (cloneList != null)
                {
                    CloneProperty(srcNode, cloneList, prop, propName);
                }
            }
        }

        private static void CloneProperty(TrafficNode srcNode, List<TrafficNode> cloneList, SerializedProperty srcProp, string propName)
        {
            for (int i = 0; i < cloneList?.Count; i++)
            {
                var cloneNode = cloneList[i];

                if (cloneNode == srcNode)
                {
                    continue;
                }

                var tempSo = new SerializedObject(cloneNode);
                var dstProp = tempSo.FindProperty(propName);

                if (dstProp.boxedValue != srcProp.boxedValue)
                    dstProp.boxedValue = srcProp.boxedValue;

                tempSo.ApplyModifiedProperties();
            }
        }
    }
}
