using Spirit604.CityEditor.Pedestrian;
using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Spirit604.CityEditor.CityEditorBookmarks;

namespace Spirit604.CityEditor
{
    public class CityEditorToolbar : ScriptableObject
    {
#if UNITY_EDITOR

        [MenuItem(CITY_CREATE_PATH + "Create RoadSegment", priority = 1)]
        public static GameObject CreateRoadSegmentCreator()
        {
            var roadSegment = CreatePrefab(ROAD_SEGMENT_PREFAB_PATH, VectorExtensions.GetCenterOfSceneView(), true, allowPrefabStage: true,
                onCreate: (obj) =>
                {
                    var selectedObject = Selection.activeGameObject;

                    if (selectedObject != null && selectedObject.TryGetComponent<RoadSegmentCreator>(out var oldCreator))
                    {
                        oldCreator.OnInspectorDisabled();
                    }

                    var creator = obj.GetComponent<RoadSegmentCreator>();
                    creator.OnCreateSegment();
                    creator.Create();
                });

            if (roadSegment)
            {
                var count = ObjectUtils.FindObjectsOfType<RoadSegment>().Length;
                var indexText = count.ToString();
                roadSegment.name = $"{roadSegment.name}{indexText}";

                bool added = false;

#if RUNTIME_ROAD
                added = AddToPrefabStage(roadSegment);

                if (added)
                {
                    roadSegment.transform.localPosition = Vector3.zero;
                }
#endif

                if (!added)
                {
                    var roadParent = ObjectUtils.FindObjectOfType<RoadParent>();

                    if (roadParent)
                    {
                        roadSegment.transform.SetParent(roadParent.transform, true);
                        EditorGUIUtility.PingObject(roadSegment);
                    }

                    var creator = roadSegment.GetComponent<RoadSegmentCreator>();

                    if (creator)
                    {
                        if (creator.roadSegmentCreatorConfig && creator.roadSegmentCreatorConfig.SnapOnCreate)
                        {
                            creator.transform.position = VectorExtensions.GetCenterOfSceneView(true);
                        }
                    }
                }
            }

            return roadSegment;
        }

        [MenuItem(CITY_CREATE_PATH + "Create TrafficPublicRoute", priority = 2)]
        public static void CreateTrafficPublicRoute()
        {
            CreatePrefab(ROUTE_PREFAB_PATH, Vector3.zero);
        }

        [MenuItem(CITY_CREATE_PATH + "Create PedestrianNode", priority = 4)]
        public static void CreatePedestrianNode()
        {
            CreatePrefab(PEDESTRIAN_NODE_PREFAB_PATH, VectorExtensions.GetCenterOfSceneView(), allowPrefabStage: true);
        }

        [MenuItem(CITY_CREATE_PATH + "City Base", priority = 5)]
        public static void CreateCityBase()
        {
            CreateCityBaseInternal();
        }

        [MenuItem(CITY_CREATE_PATH + "Create PedestrianNodeCreator", priority = 6)]
        public static void CreatePedestrianNodeCreator()
        {
            if (!TryToSelect<PedestrianNodeCreator>())
            {
                CreatePrefab(PEDESTRIAN_NODE_CREATOR_PREFAB_PATH, Vector3.zero);
            }
        }

        [MenuItem(CITY_CREATE_PATH + "Create RoadSegmentPlacer", priority = 7)]
        public static void CreateRoadSegmentPlacer()
        {
            if (!TryToSelect<RoadSegmentPlacer>())
            {
                CreatePrefab(ROADSEGMENTPLACER_PREFAB_PATH, Vector3.zero);
            }
        }

        private static GameObject CreatePrefab(string prefabPath, Vector3 position, bool unpackPrefab = false, bool allowSearchRoot = true, bool allowPrefabStage = false, Action<GameObject> onCreate = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));

            if (prefab == null && allowSearchRoot)
            {
                CityEditorStartup.ForceUpdateRoot();
                return CreatePrefab(prefabPath, position, unpackPrefab, false);
            }

            if (prefab == null)
            {
                Debug.LogError("Prefab not found!");
                return null;
            }

            GameObject createdObject = PrefabExtension.InstantiatePrefab(prefab, unpackPrefab);

            createdObject.transform.position = position;
            createdObject.transform.SetAsLastSibling();

            if (allowPrefabStage)
            {
                AddToPrefabStage(createdObject);
            }

            onCreate?.Invoke(createdObject.gameObject);
            Selection.activeObject = createdObject.gameObject;

            return createdObject;
        }

        private static bool AddToPrefabStage(GameObject createdObject)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (prefabStage != null)
            {
                createdObject.transform.parent = null;
                EditorSceneManager.MoveGameObjectToScene(createdObject, prefabStage.scene);

                createdObject.transform.SetParent(EditorExtension.GetPrefabStageRoot(prefabStage).transform);
                return true;
            }

            return false;
        }

        private static void CreateCityBaseInternal()
        {
            CityEditorHubWindow.ShowWindow();
        }

        private static bool TryToSelect<T>() where T : MonoBehaviour
        {
            var obj = ObjectUtils.FindObjectOfType<T>();

            if (obj)
            {
                Selection.activeObject = obj;
                return true;
            }

            return false;
        }
#endif
    }
}
