#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Factory.Car;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    [CustomEditor(typeof(TrafficRoadDebugger))]
    public class TrafficRoadDebuggerEditor : SharedSettingsEditorBase<TrafficRoadDebuggerEditor.EditorSettings>
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/trafficTestScene.html#trafficcar-road-debugger";

        [Serializable]
        public class EditorSettings
        {
            public bool SceneSettingsFoldout = true;
            public bool SpawnInfoFoldout = true;
            public bool OtherSettingsFoldout = true;
        }

        private SerializedProperty trafficDebugModeProp;
        private SerializedProperty trafficSpawnTestInfosProp;
        private List<TrafficNode> trafficNodes;

        private string[] trafficNodesHeaders;
        private int selectedIndex = -1;
        private bool showDescription;
        private Dictionary<Path, bool> routeHighlightInfo = new Dictionary<Path, bool>();
        private Dictionary<TrafficDebugMode, IEntityDebugger> trafficDebuggers = new Dictionary<TrafficDebugMode, IEntityDebugger>();
        private bool init;

        private TrafficRoadDebugger TrafficRoadDebugger => target as TrafficRoadDebugger;

        protected override string SaveKey => "TrafficRoadDebuggerSettings";

        protected override void OnEnable()
        {
            base.OnEnable();

            var trafficRoadDebugger = target as TrafficRoadDebugger;

            trafficDebugModeProp = serializedObject.FindProperty("TrafficDebugMode");
            trafficSpawnTestInfosProp = serializedObject.FindProperty("TrafficSpawnTestInfos");

            trafficNodes = trafficRoadDebugger.GetComponentsInChildren<TrafficNode>().ToList();
            trafficNodesHeaders = new string[trafficNodes.Count + 1];

            trafficNodesHeaders[0] = "All";

            for (int i = 0; i < trafficNodes.Count; i++)
            {
                trafficNodesHeaders[i + 1] = trafficNodes[i].name;
            }

            FillPathHighlightInfo(trafficRoadDebugger);
            HightLightPaths(trafficRoadDebugger);

            trafficRoadDebugger.OnInspectorEnabled();

            InitComponent(trafficRoadDebugger);

            InitDebugger();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DisableHighlight();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var trafficRoadDebugger = target as TrafficRoadDebugger;

            DocumentationLinkerUtils.ShowButtonAndHeader(target, DocLink);

            EditorGUILayout.PropertyField(trafficDebugModeProp);

            InspectorExtension.DrawDefaultInspectorGroupBlock("Scene Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(trafficRoadDebugger.vehicleDataCollection)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(trafficRoadDebugger.enableVisualDebug)));

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(trafficRoadDebugger.showButtons)));

                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(trafficRoadDebugger.highlightPathAfterAdd)));

            }, ref SharedSettings.SceneSettingsFoldout);


            InspectorExtension.DrawDefaultInspectorGroupBlock("Spawn Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(trafficRoadDebugger.spawnOnPlay)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(trafficRoadDebugger.spawnOnView)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(trafficRoadDebugger.autoClearOnSpawn)));

                if (!Application.isPlaying)
                {
                    VehicleCollectionExtension.DrawModelOptions(trafficRoadDebugger.vehicleDataCollection, serializedObject.FindProperty(nameof(trafficRoadDebugger.spawnCarModel)));
                }
                else
                {
                    trafficRoadDebugger.RuntimeCarModel = EditorGUILayout.Popup("Car Model", trafficRoadDebugger.RuntimeCarModel, trafficRoadDebugger.vehicleDataCollection.Options);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(trafficRoadDebugger.disableLaneChanging)));

                var hash = TrafficRoadSpawnDebuggerSystem.GetHash(trafficRoadDebugger.transform.position);
                var hashString = hash.ToString();

                GUI.enabled = false;
                EditorGUILayout.TextField("Hash", hashString);
                GUI.enabled = true;

                if (InspectorExtension.DrawClipboardButton(hashString))
                {
                    UnityEngine.Debug.Log($"Hash {hashString} copied to the clipboard.");
                }

            }, ref SharedSettings.SpawnInfoFoldout);

            InspectorExtension.DrawDefaultInspectorGroupBlock("Other Settings", () =>
            {
                showDescription = EditorGUILayout.Toggle("Show Description", showDescription);

                if (showDescription)
                {
                    if (!trafficRoadDebugger.customDescription)
                    {
                        string description = string.Empty;

                        TrafficDebugDescription.Description.TryGetValue(trafficRoadDebugger.TrafficDebugMode, out description);

                        if (!string.IsNullOrEmpty(description))
                        {
                            EditorGUILayout.HelpBox(description, MessageType.Info);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Description not found", MessageType.Warning);
                        }
                    }
                    else
                    {
                        var description = trafficRoadDebugger.description.Replace("\\n", "\n");
                        EditorGUILayout.HelpBox(description, MessageType.Info);
                    }
                }
            }, ref SharedSettings.OtherSettingsFoldout);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(trafficSpawnTestInfosProp);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                OnRouteListChanged();
            }

            if (trafficRoadDebugger.showButtons && trafficNodes != null)
            {
                EditorGUI.BeginChangeCheck();

                selectedIndex = GUILayout.Toolbar(selectedIndex + 1, trafficNodesHeaders) - 1;

                if (EditorGUI.EndChangeCheck())
                {
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("Spawn"))
            {
                if (Application.isPlaying)
                {
                    trafficRoadDebugger.Spawn();
                }
                else
                {
                    EditorGUILayout.HelpBox("Enter play mode!", MessageType.Info);
                }
            }

            if (GUILayout.Button("Clear"))
            {
                if (Application.isPlaying)
                {
                    trafficRoadDebugger.Clear();
                }
                else
                {
                    EditorGUILayout.HelpBox("Enter play mode!", MessageType.Info);
                }
            }

            if (!Application.isPlaying)
            {
                if (GUILayout.Button("Resolve Indexes"))
                {
                    trafficRoadDebugger.ResolvePathIndexes();
                }
            }
        }

        private void OnSceneGUI()
        {
            var controller = target as TrafficRoadDebugger;

            for (int i = 0; i < controller.TrafficSpawnTestInfos?.Count; i++)
            {
                var route = controller.TrafficSpawnTestInfos[i].Path;

                if (route)
                {
                    var dummyRotation = Quaternion.identity;
                    int index = 0;
                    var position = controller.GetSpawnData(route, controller.TrafficSpawnTestInfos[i].NormalizedPathPosition, ref index, ref dummyRotation);

                    EditorExtension.DrawWorldString((i + 1).ToString(), position);
                    Handles.DrawWireDisc(position, Vector3.up, 1f);
                }
            }

            if (controller.showButtons && trafficNodes != null)
            {
                if (selectedIndex == -1)
                {
                    for (int i = 0; i < trafficNodes.Count; i++)
                    {
                        var trafficNode = trafficNodes[i];
                        DrawAddPathButton(trafficNode);
                    }
                }
                else
                {
                    var trafficNode = trafficNodes[selectedIndex];
                    DrawAddPathButton(trafficNode);
                }
            }

            DrawDebugger();
        }

        private void FillPathHighlightInfo(TrafficRoadDebugger controller)
        {
            routeHighlightInfo.Clear();

            for (int i = 0; i < controller.TrafficSpawnTestInfos?.Count; i++)
            {
                if (controller.TrafficSpawnTestInfos[i].Path)
                {
                    if (!routeHighlightInfo.ContainsKey(controller.TrafficSpawnTestInfos[i].Path))
                    {
                        routeHighlightInfo.Add(controller.TrafficSpawnTestInfos[i].Path, controller.TrafficSpawnTestInfos[i].HighLight);
                    }
                    else
                    {
                        if (controller.TrafficSpawnTestInfos[i].HighLight)
                        {
                            routeHighlightInfo[controller.TrafficSpawnTestInfos[i].Path] = true;
                        }
                    }
                }
            }
        }

        private void HightLightPaths(TrafficRoadDebugger controller)
        {
            foreach (var item in routeHighlightInfo)
            {
                if (item.Key != null)
                {
                    item.Key.Highlighted = item.Value;
                }
            }
        }

        private void DisableHighlight()
        {
            foreach (var item in routeHighlightInfo)
            {
                if (item.Key != null)
                {
                    item.Key.Highlighted = false;
                }
            }

            routeHighlightInfo.Clear();
        }

        private void DrawAddPathButton(TrafficNode trafficNode)
        {
            var controller = target as TrafficRoadDebugger;

            trafficNode.IterateAllPaths(path =>
            {
                var labelPosition = path.GetMiddlePosition();

                Action addCallback = () =>
                {
                    controller.AddPath(path);
                    OnRouteListChanged();
                };

                EditorExtension.DrawButton("+", labelPosition, 35f, addCallback);
            });
        }

        private void InitComponent(TrafficRoadDebugger trafficRoadDebugger)
        {
            if (!trafficRoadDebugger.editorInitialized)
            {
                var components = trafficRoadDebugger.GetComponents<Component>();
                int moveCount = 0;

                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == trafficRoadDebugger)
                    {
                        moveCount = i;
                        break;
                    }
                }

                for (int i = 0; i < moveCount; i++)
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(trafficRoadDebugger);
                }

                trafficRoadDebugger.editorInitialized = true;
                EditorSaver.SetObjectDirty(trafficRoadDebugger);
            }
        }

        protected override EditorSettings GetDefaultSettings() => new EditorSettings();

        private void DrawDebugger()
        {
            var debugger = TrafficRoadDebugger;

            if (!debugger.IsInitialized)
                return;

            if (!Application.isPlaying)
                return;

            if (debugger.TrafficDebugMode == TrafficDebugMode.Disabled) return;

            InitDebugger();

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var currentDebugger = trafficDebuggers[debugger.TrafficDebugMode];
            DynamicBuffer<SpawnedCarDataElement> spawnedCars = debugger.GetSpawnedCars();

            for (int i = 0; i < spawnedCars.Length; i++)
            {
                int index = i % debugger.TrafficSpawnTestInfos.Count;

                if (!debugger.TrafficSpawnTestInfos[index].ShowInfo)
                    continue;

                var carEntity = spawnedCars[i].CarEntity;

                if (entityManager.HasComponent<LocalToWorld>(carEntity))
                {
                    currentDebugger.Tick(carEntity, debugger.fontColor);
                }
            }
        }

        private void InitDebugger()
        {
            if (!Application.isPlaying || init)
                return;

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            init = true;
            trafficDebuggers.Clear();
            trafficDebuggers.Add(TrafficDebugMode.DebugObstacleDistance, new TrafficObstacleDistanceDebug(entityManager));
            trafficDebuggers.Add(TrafficDebugMode.DebugNextPathCalculationDistance, new TrafficNextPathCalculationDistanceDebug(entityManager));
            trafficDebuggers.Add(TrafficDebugMode.DebugIntersectedPath, new TrafficIntersectedPathDebug(entityManager));
            trafficDebuggers.Add(TrafficDebugMode.DebugChangeLane, new TrafficChangeLaneDebug(entityManager));
            trafficDebuggers.Add(TrafficDebugMode.DebugNpc, new TrafficNpcObstacleDebug(entityManager));
        }

        private void OnRouteListChanged()
        {
            var controller = target as TrafficRoadDebugger;

            DisableHighlight();
            FillPathHighlightInfo(controller);
            HightLightPaths(controller);
            controller.OnListChanged();
        }
    }
}
#endif