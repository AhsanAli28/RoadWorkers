using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Factory.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficSpawnButtonHelper : MonoBehaviour
    {
        [SerializeField] private VehicleDataCollection vehicleDataCollection;
        [SerializeField] private int traffinNodeIndex1;
        [SerializeField] private int traffinNodeIndex2;
        [SerializeField] private int carModel;

        private TrafficSpawnerSystem trafficSpawnerSystem;

        private void Awake()
        {
            trafficSpawnerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficSpawnerSystem>();
        }

        public void Spawn1()
        {
            SpawnByIndexTest(traffinNodeIndex1, (int)carModel);
        }

        public void Spawn2()
        {
            SpawnByIndexTest(traffinNodeIndex2, (int)carModel);
        }

        public void SpawnBoth()
        {
            SpawnByIndexTest(traffinNodeIndex1, (int)carModel);
            SpawnByIndexTest(traffinNodeIndex2, (int)carModel);
        }

        public void SpawnByIndexTest(int spawnEntityIndex, int carModelIndex = -1, int globalPathIndex = -1)
        {
            if (spawnEntityIndex <= 0)
            {
                return;
            }

            Entity spawnNodeEntity = Entity.Null;

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entities = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TrafficNodeComponent>()).ToEntityArray(Allocator.TempJob);

            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i].Index == spawnEntityIndex)
                {
                    spawnNodeEntity = entities[i];
                    break;
                }
            }

            entities.Dispose();

            if (!entityManager.Exists(spawnNodeEntity))
            {
                return;
            }

            var localTransform = entityManager.GetComponentData<LocalTransform>(spawnNodeEntity);

            Vector3 spawnPosition = localTransform.Position;
            Quaternion spawnRotation = localTransform.Rotation;

            TrafficDestinationComponent destinationComponent = new TrafficDestinationComponent
            {
                Destination = spawnPosition,
                DestinationNode = spawnNodeEntity,
                PreviousNode = spawnNodeEntity,
                CurrentNode = spawnNodeEntity,
                NextDestinationNode = Entity.Null,
                NextGlobalPathIndex = -1,
            };

            var trafficSpawnParams = new TrafficSpawnParams(spawnPosition, spawnRotation, destinationComponent)
            {
                globalPathIndex = globalPathIndex,
                targetNodeEntity = spawnNodeEntity,
                spawnNodeEntity = spawnNodeEntity,
                hasDriver = true,
                carModelIndex = carModelIndex,
                customSpawnData = true
            };

            trafficSpawnerSystem.Spawn(trafficSpawnParams, true);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TrafficSpawnButtonHelper), true)]
    public class TrafficSpawnButtonHelperEditor : Editor
    {
        private TrafficSpawnButtonHelper trafficSpawnButtonHelper;

        private void OnEnable()
        {
            trafficSpawnButtonHelper = target as TrafficSpawnButtonHelper;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var vehicleDataCollectionProp = serializedObject.FindProperty("vehicleDataCollection");
            EditorGUILayout.PropertyField(vehicleDataCollectionProp);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("traffinNodeIndex1"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("traffinNodeIndex2"));

            var carModelProp = serializedObject.FindProperty("carModel");
            VehicleCollectionExtension.DrawModelOptions(vehicleDataCollectionProp.objectReferenceValue as VehicleDataCollection, carModelProp);

            if (GUILayout.Button("Spawn 1"))
            {
                trafficSpawnButtonHelper.Spawn1();
            }

            if (GUILayout.Button("Spawn 2"))
            {
                trafficSpawnButtonHelper.Spawn2();
            }

            if (GUILayout.Button("Spawn Both"))
            {
                trafficSpawnButtonHelper.SpawnBoth();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}