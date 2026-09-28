using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.Extensions;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianNodeDebugger : MonoBehaviourBase
    {
        public enum PedestrianNodeDebuggerType { Empty, Default, LightInfo, TargetDeviation, OtherSettings, CrosswalkSettings, ConnectedEntities }

        [SerializeField] private bool enableDebug;
        [SerializeField] private Color fontColor = Color.white;
        [SerializeField] private PedestrianNodeDebuggerType debuggerType;
        [SerializeField] private bool drawConnectionLine = true;
        [SerializeField] private Color32 connectionColor = Color.blue;
        [SerializeField][Range(0.1f, 5f)] private float lineWidth = 1f;
        [SerializeField] private bool showSpawnData;
        [SerializeField] private int spawnIndex;
        [SerializeField][Range(1, 100)] private int spawnCount = 1;

        private PedestrianEntitySpawnerSystem pedestrianSpawnerSystem;
        private EntityManager entityManager;
        private EntityQuery entityQuery;
        private bool playMode;

        private Dictionary<PedestrianNodeDebuggerType, IEntityDebugger> debuggers = new Dictionary<PedestrianNodeDebuggerType, IEntityDebugger>();

        public EntityDebuggerBase SelectedDebugger => debuggers.ContainsKey(debuggerType) ? (EntityDebuggerBase)debuggers[debuggerType] : null;

        public bool EnableDebug { get => enableDebug; set => enableDebug = value; }
        public PedestrianNodeDebuggerType DebuggerType { get => debuggerType; set => debuggerType = value; }
        public bool DrawConnectionLine { get => drawConnectionLine; set => drawConnectionLine = value; }
        public Color32 ConnectionColor { get => connectionColor; set => connectionColor = value; }

        private void Start()
        {
            pedestrianSpawnerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>();
            Init();
        }

        [Button]
        public void Spawn()
        {
            if (!Application.isPlaying)
            {
                UnityEngine.Debug.Log("Enter playmode!");
                return;
            }

            pedestrianSpawnerSystem.Spawn(true, Entity.Null, spawnCount);
        }

        private bool Init()
        {
            if (debuggers.Count != 0 && playMode == Application.isPlaying)
                return true;

            if (World.DefaultGameObjectInjectionWorld == null)
                return false;

            playMode = Application.isPlaying;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeCapacityComponent>(), ComponentType.ReadOnly<LocalToWorld>());

            debuggers.Clear();
            debuggers.Add(PedestrianNodeDebuggerType.Default, new DefaultPedestrianNodeDebugger(entityManager));
            debuggers.Add(PedestrianNodeDebuggerType.LightInfo, new PedestrianNodeLightDebugger(entityManager));
            debuggers.Add(PedestrianNodeDebuggerType.TargetDeviation, new PedestrianNodeTargetDeviationDebugger(entityManager));
            debuggers.Add(PedestrianNodeDebuggerType.OtherSettings, new PedestrianNodeOtherSettingsDebugger(entityManager));
            debuggers.Add(PedestrianNodeDebuggerType.CrosswalkSettings, new PedestrianNodeCrossSettingsDebugger(entityManager));
            debuggers.Add(PedestrianNodeDebuggerType.ConnectedEntities, new PedestrianNodeConnectionDebugger(entityManager));
            return true;
        }

        private void OnDrawGizmos()
        {
            if (!enableDebug)
                return;

            if (!Init())
                return;

            var entities = entityQuery.ToEntityArray(Unity.Collections.Allocator.TempJob);

            for (int i = 0; i < entities.Length; i++)
            {
                if (!entityManager.HasComponent<LocalToWorld>(entities[i]))
                {
                    UnityEngine.Debug.Log($"PedestrianNodeDebugger Entity {entities[i].Index} Doesn't have LocalToWorld component");
                    continue;
                }

                if (debuggerType != PedestrianNodeDebuggerType.Empty)
                {
                    debuggers[debuggerType].Tick(entities[i], fontColor);
                }

                var position = entityManager.GetComponentData<LocalToWorld>(entities[i]).Position;

                if (EntityDebuggerBase.OutOfCamera(position))
                {
                    continue;
                }

                if (drawConnectionLine)
                {
                    var nodeSettingsComponent = entityManager.GetComponentData<NodeSettingsComponent>(entities[i]);

                    var connections = entityManager.GetBuffer<NodeConnectionDataElement>(entities[i]);

                    for (int j = 0; j < connections.Length; j++)
                    {
                        var connectedEntity = connections[j].ConnectedEntity;

                        if (entityManager.HasComponent<LocalToWorld>(connectedEntity))
                        {
                            var targetPosition = entityManager.GetComponentData<LocalToWorld>(connectedEntity).Position;

                            DebugLine.DrawThickLine(position, targetPosition, lineWidth, connectionColor);
                        }
                    }
                }
            }

            entities.Dispose();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PedestrianNodeDebugger))]
    public class PedestrianNodeDebuggerEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/pedestrianNodeDebug.html";

        private PedestrianNodeDebugger pedestrianNodeDebugger;

        private void OnEnable()
        {
            pedestrianNodeDebugger = target as PedestrianNodeDebugger;
            pedestrianNodeDebugger.SelectedDebugger?.OnSelect();
        }

        private void OnDisable()
        {
            pedestrianNodeDebugger.SelectedDebugger?.OnDeselect();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DocumentationLinkerUtils.ShowButtonAndHeader(target, DocLink);
            var debugField = serializedObject.FindProperty("enableDebug");
            EditorGUILayout.PropertyField(debugField);

            if (debugField.boolValue)
            {
                InspectorExtension.DrawDefaultInspectorGroupBlock("Debugger Data", () =>
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("debuggerType"));

                    if (EditorGUI.EndChangeCheck())
                    {
                        var previousDebugger = pedestrianNodeDebugger.SelectedDebugger;
                        serializedObject.ApplyModifiedProperties();
                        var currentDebugger = pedestrianNodeDebugger.SelectedDebugger;

                        previousDebugger?.OnDeselect();
                        currentDebugger?.OnSelect();
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fontColor"));

                    var connectionLineField = serializedObject.FindProperty("drawConnectionLine");

                    EditorGUILayout.PropertyField(connectionLineField);

                    if (connectionLineField.boolValue)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("lineWidth"));
                    }
                });

                var spawnDataField = serializedObject.FindProperty("showSpawnData");

                InspectorExtension.DrawDefaultInspectorGroupBlock("Spawn Data", () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnIndex"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnCount"));
                }, spawnDataField);

                if (pedestrianNodeDebugger.SelectedDebugger != null && pedestrianNodeDebugger.SelectedDebugger.HasCustomInspectorData)
                {
                    InspectorExtension.DrawGroupBox("Debugger Custom Data", () =>
                    {
                        pedestrianNodeDebugger.SelectedDebugger.DrawCustomInspectorData();
                    });
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
