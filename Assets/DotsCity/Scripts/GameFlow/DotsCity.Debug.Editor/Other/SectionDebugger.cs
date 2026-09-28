using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.Gameplay.Road.Debug;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spirit604.DotsCity.Debug
{
    public class SectionDebugger : MonoBehaviour
    {
        [SerializeField] private PathDebugger pathDebugger;
        [SerializeField] private PedestrianNodeDebugger pedestrianNodeDebugger;
        [SerializeField] private bool enableDebug;
        [SerializeField] private Color loadedSectionColor = Color.green;
        [SerializeField] private Color unloadedSectionColor = Color.red;
        [SerializeField] private Color loadCircleColor = Color.green;
        [SerializeField] private Color unloadCircleColor = Color.red;

#if UNITY_EDITOR

        private EntityManager entityManager;
        private EntityQuery sectionConfigQuery;
        private EntityQuery sectionQuery;
        private EntityQuery cullPointGroup;

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            sectionConfigQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RoadStreamingConfigReference>());
            sectionQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RoadSectionData>());
            cullPointGroup = entityManager.CreateEntityQuery(ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<CullPointTag>());
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enableDebug || sectionConfigQuery.CalculateEntityCount() == 0)
                return;

            var sectionConfig = sectionConfigQuery.GetSingleton<RoadStreamingConfigReference>().Config.Value;

            var sections = sectionQuery.ToComponentDataArray<RoadSectionData>(Allocator.TempJob);
            var sectionEntities = sectionQuery.ToEntityArray(Allocator.TempJob);

            for (int i = 0; i < sections.Length; i++)
            {
                var pos = sections[i].Position;

                var size = Vector3.one * sectionConfig.SectionCellSize;
                size.y = 1;

                var loaded = entityManager.HasComponent<RequestSceneLoaded>(sectionEntities[i]);
                Gizmos.color = loaded ? loadedSectionColor : unloadedSectionColor;
                Gizmos.DrawWireCube(pos, size);
            }

            sections.Dispose();
            sectionEntities.Dispose();

            var entity = cullPointGroup.GetSingletonEntity();
            var position = entityManager.GetComponentData<LocalTransform>(entity).Position;

            Gizmos.color = loadCircleColor;

            Gizmos.DrawWireSphere(position, Mathf.Sqrt(sectionConfig.DistanceForStreamingInSQ));

            Gizmos.color = unloadCircleColor;

            Gizmos.DrawWireSphere(position, Mathf.Sqrt(sectionConfig.DistanceForStreamingOutSQ));
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SectionDebugger))]
    public class SectionDebuggerEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/commonDebug.html#section-debugger";

        private SectionDebugger sectionDebugger;
        private SerializedProperty pathDebuggerProp;
        private SerializedProperty pedestrianNodeDebuggerProp;
        private PedestrianNodeDebugger pedestrianNodeDebugger;
        private PathDebugger pathDebugger;

        private void OnEnable()
        {
            sectionDebugger = target as SectionDebugger;

            pathDebuggerProp = serializedObject.FindProperty("pathDebugger");
            pedestrianNodeDebuggerProp = serializedObject.FindProperty("pedestrianNodeDebugger");

            if (pathDebuggerProp.objectReferenceValue)
            {
                pathDebugger = (PathDebugger)pathDebuggerProp.objectReferenceValue;
            }

            if (pedestrianNodeDebuggerProp.objectReferenceValue)
            {
                pedestrianNodeDebugger = (PedestrianNodeDebugger)pedestrianNodeDebuggerProp.objectReferenceValue;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DocumentationLinkerUtils.ShowButtonAndHeader(target, DocLink);
            var enableDebugProp = serializedObject.FindProperty("enableDebug");
            EditorGUILayout.PropertyField(enableDebugProp);

            if (pathDebuggerProp.objectReferenceValue == null)
            {
                EditorGUILayout.PropertyField(pathDebuggerProp);
            }
            if (pedestrianNodeDebuggerProp.objectReferenceValue == null)
            {
                EditorGUILayout.PropertyField(pedestrianNodeDebuggerProp);
            }

            if (enableDebugProp.boolValue)
            {
                if (pathDebugger)
                {
                    pathDebugger.DrawEntityTrafficPath = EditorGUILayout.Toggle("Show Traffic Path", pathDebugger.DrawEntityTrafficPath);
                }

                if (pedestrianNodeDebugger)
                {
                    pedestrianNodeDebugger.EnableDebug = EditorGUILayout.Toggle("Show Pedestrian Path", pedestrianNodeDebugger.EnableDebug);
                    pedestrianNodeDebugger.DrawConnectionLine = pedestrianNodeDebugger.EnableDebug;

                    if (pedestrianNodeDebugger.EnableDebug)
                    {
                        pedestrianNodeDebugger.DebuggerType = PedestrianNodeDebugger.PedestrianNodeDebuggerType.Empty;
                    }
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("loadedSectionColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unloadedSectionColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loadCircleColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unloadCircleColor"));

                if (pedestrianNodeDebugger && pedestrianNodeDebugger.DrawConnectionLine)
                {
                    pedestrianNodeDebugger.ConnectionColor = EditorGUILayout.ColorField("Pedestrian Path Color", pedestrianNodeDebugger.ConnectionColor);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}