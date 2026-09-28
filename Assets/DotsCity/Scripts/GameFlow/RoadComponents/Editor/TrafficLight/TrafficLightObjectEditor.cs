#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Gameplay.Road;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TrafficLightObject))]
    public class TrafficLightObjectEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/trafficLight.html#traffic-light-object";

        private TrafficLightObject trafficLightObject;
        private GUIStyle guiStyle = new GUIStyle();

        private void OnEnable()
        {
            trafficLightObject = target as TrafficLightObject;
            guiStyle.normal.textColor = Color.white;
            guiStyle.fontSize = 24;
        }

        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            DocumentationLinkerUtils.ShowButtonAndHeader(target, DocLink);

            EditorGUI.BeginChangeCheck();

            var trafficLightCrossroadProp = so.FindProperty("trafficLightCrossroad");

            if (trafficLightObject.ConnectedId == 0 || !Application.isPlaying || trafficLightObject.gameObject.scene.isSubScene)
            {
                if (trafficLightCrossroadProp.objectReferenceValue)
                {
                    var crossRoad = trafficLightCrossroadProp.objectReferenceValue as TrafficLightCrossroad;

                    if (crossRoad.gameObject.scene == trafficLightObject.gameObject.scene)
                    {
                        EditorGUILayout.PropertyField(trafficLightCrossroadProp);
                    }
                    else
                    {
                        var newRef = EditorGUILayout.ObjectField("Traffic Light Crossroad", trafficLightCrossroadProp.objectReferenceValue, typeof(TrafficLightCrossroad), true);

                        if (trafficLightCrossroadProp.objectReferenceValue != newRef)
                        {
                            trafficLightCrossroadProp.objectReferenceValue = newRef;
                        }
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(trafficLightCrossroadProp);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                trafficLightObject.AssignCrossRoad(trafficLightCrossroadProp.objectReferenceValue as TrafficLightCrossroad);
            }

            GUI.enabled = false;
            var connectedIdProp = so.FindProperty("connectedId");
            EditorGUILayout.IntField("Connected Id", connectedIdProp.intValue);
            GUI.enabled = true;

            if (Selection.objects.Length > 1)
            {
                so.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(so.FindProperty("trafficLightFrameData"));

            so.ApplyModifiedProperties();

            if (GUILayout.Button("Setup Initial Indexes"))
            {
                trafficLightObject.SetupInitialIndexes();
            }

            if (GUILayout.Button("Assign Initial Childs"))
            {
                trafficLightObject.AssignInitialChilds();
            }
        }

        private void OnSceneGUI()
        {
            var trafficLightObject = target as TrafficLightObject;

            DrawIndexes(trafficLightObject, guiStyle);
        }

        public static void DrawIndexes(TrafficLightObject trafficLightObject, GUIStyle guiStyle)
        {
            if (trafficLightObject == null || trafficLightObject.TrafficLightFrames == null)
                return;

            foreach (var framesData in trafficLightObject.TrafficLightFrames)
            {
                var frames = framesData.Value;
                var relatedLightIndex = framesData.Key;

                for (int j = 0; j < frames.TrafficLightFrames?.Count; j++)
                {
                    if (frames.TrafficLightFrames[j] == null)
                        continue;

                    var position = frames.TrafficLightFrames[j].GetIndexPosition();
                    Handles.Label(position, relatedLightIndex.ToString(), guiStyle);
                }
            }
        }
    }
}
#endif
