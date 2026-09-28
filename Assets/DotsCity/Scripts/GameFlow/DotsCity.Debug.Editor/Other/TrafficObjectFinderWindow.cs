#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficObjectFinderWindow : EditorWindowBase
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/trafficObjectFinder.html";

        private enum TrafficObjectType { TrafficNode, Path, PedestrianNode, TrafficLightObjectAuthoring, TrafficLightHandler, TrafficLightCrossroad }

        [SerializeField] private TrafficObjectType trafficObjectType;
        [SerializeField] private int instanceId = 0;

        private bool found = true;
        private bool showMessage;
        private Vector3 lastObjectPosition;

        #region Constructor

        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "Traffic Object Finder")]
        public static TrafficObjectFinderWindow ShowWindow()
        {
            TrafficObjectFinderWindow trafficObjectFinderWindow = (TrafficObjectFinderWindow)GetWindow(typeof(TrafficObjectFinderWindow));
            trafficObjectFinderWindow.titleContent = new GUIContent("Traffic Object Finder");

            return trafficObjectFinderWindow;
        }

        #endregion

        private void OnGUI()
        {
            var so = new SerializedObject(this);
            so.Update();

            DocumentationLinkerUtils.ShowButtonFirst(DocLink, -3, -14);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(trafficObjectType)));

            EditorGUILayout.PropertyField(so.FindProperty(nameof(instanceId)));

            if (EditorGUI.EndChangeCheck())
            {
                showMessage = false;
                lastObjectPosition = default;
            }

            GUI.enabled = lastObjectPosition != Vector3.zero;

            if (GUILayout.Button("Focus"))
            {
                EditorExtension.SceneFocus(lastObjectPosition);
            }

            GUI.enabled = true;

            if (GUILayout.Button("Find"))
            {
                Find();
            }

            so.ApplyModifiedProperties();

            if (!found && showMessage)
            {
                EditorGUILayout.HelpBox($"Object {trafficObjectType} InstanceId '{instanceId}' not found", MessageType.Info);
            }
        }

        private void Find()
        {
            found = false;

            switch (trafficObjectType)
            {
                case TrafficObjectType.TrafficNode:
                    {
                        found = FindObject<TrafficNode>();
                        break;
                    }
                case TrafficObjectType.Path:
                    {
                        found = FindObject<Path>();
                        break;
                    }
                case TrafficObjectType.PedestrianNode:
                    {
                        found = FindObject<PedestrianNode>();
                        break;
                    }
                case TrafficObjectType.TrafficLightObjectAuthoring:
                    {
                        found = FindObject<TrafficLightObjectAuthoring>();
                        break;
                    }
                case TrafficObjectType.TrafficLightHandler:
                    {
                        found = FindObject<TrafficLightHandler>();
                        break;
                    }
                case TrafficObjectType.TrafficLightCrossroad:
                    {
                        found = FindObject<TrafficLightCrossroad>();
                        break;
                    }
            }

            showMessage = true;
        }

        private bool FindObject<T>() where T : MonoBehaviour
        {
            var root = EditorExtension.GetPrefabStageRoot();

            if (root == null)
            {
                var obj = ObjectUtils.FindObjectsOfType<T>().Where(a => a.GetInstanceID() == instanceId).FirstOrDefault();

                if (obj)
                {
                    SelectObject(obj.gameObject);
                    lastObjectPosition = obj.transform.position;
                    return true;
                }

                lastObjectPosition = default;
            }
            else
            {
                var obj = root.GetComponentsInChildren<T>().Where(a => a.GetInstanceID() == instanceId).FirstOrDefault();

                if (obj)
                {
                    SelectObject(obj.gameObject);
                    lastObjectPosition = obj.transform.position;
                    return true;
                }

                lastObjectPosition = default;
            }

            return false;
        }

        private static void SelectObject(GameObject obj)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
    }
}
#endif