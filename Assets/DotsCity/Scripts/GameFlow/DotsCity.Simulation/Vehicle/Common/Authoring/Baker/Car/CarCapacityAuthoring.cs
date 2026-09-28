using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public class CarCapacityAuthoring : MonoBehaviour
    {
        private readonly Vector3 EntryPointCubeSize = Vector3.one;

        [SerializeField][Range(0, 40)] private int maxCapacity = 1;
        [SerializeField] private List<VehicleEntryAuthoring> entryPoints = new List<VehicleEntryAuthoring>();
        [SerializeField] private bool showEntryPoint;

        public int MaxCapacity { get => maxCapacity; set => maxCapacity = value; }
        public List<VehicleEntryAuthoring> EntryPoints { get => entryPoints; }
        public bool ShowEntryPoint { get => showEntryPoint; set => showEntryPoint = value; }

        class CarCapacityAuthoringBaker : Baker<CarCapacityAuthoring>
        {
            public override void Bake(CarCapacityAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                var buffer = AddBuffer<VehicleEntryElement>(entity);
                buffer.EnsureCapacity(authoring.entryPoints.Count);

                NativeArray<Entity> linkedEntities = new NativeArray<Entity>(authoring.entryPoints.Count, Allocator.Temp);

                for (int i = 0; i < authoring.entryPoints.Count; i++)
                {
                    if (authoring.entryPoints[i] == null)
                    {
                        UnityEngine.Debug.LogError($"{authoring.name} has null entry!");
                        continue;
                    }

                    var entryEntity = GetEntity(authoring.entryPoints[i].gameObject, TransformUsageFlags.Dynamic);

                    buffer.Add(new VehicleEntryElement()
                    {
                        EntryPointEntity = entryEntity,
                        RightSide = authoring.entryPoints[i].transform.localPosition.x > 0
                    });

                    linkedEntities[i] = entryEntity;
                }

                AddComponent(entity, new VehicleLinkBakingComponent()
                {
                    LinkedEntities = linkedEntities
                });

                AddComponent(entity, new CarCapacityComponent()
                {
                    MaxCapacity = authoring.maxCapacity,
                    AvailableCapacity = authoring.maxCapacity,
                });

                if (authoring.entryPoints.Count == 0)
                {
                    UnityEngine.Debug.LogError($"{authoring.name} Entry points not assigned!");
                }
            }
        }

        public void CreateEntry()
        {
            CreateEntries(1);
        }

        public void CreateEntries(int placeCount)
        {
            CreateEntries(placeCount, PedestrianNodeType.TrafficPublicEntry);
        }

        public void CreateEntries(int placeCount, PedestrianNodeType entryType)
        {
            for (int i = 0; i < placeCount; i++)
            {
                var entry = new GameObject($"Entry{entryPoints.Count + 1}");
                entry.transform.SetParent(transform);

                var entryAuthoring = entry.gameObject.AddComponent<VehicleEntryAuthoring>();
                entryAuthoring.EntryType = entryType;
                entryAuthoring.transform.localPosition = new Vector3(2, 0, 0);

                entryPoints.Add(entryAuthoring);

                EditorSaver.SetObjectDirty(entryAuthoring);
            }

            EditorSaver.SetObjectDirty(this);
        }

        private void OnDrawGizmosSelected()
        {
            if (showEntryPoint)
            {
                for (int i = 0; i < entryPoints.Count; i++)
                {
                    if (entryPoints[i] == null)
                    {
                        continue;
                    }

                    var pos = entryPoints[i].transform.position + new Vector3(0, EntryPointCubeSize.y / 2);
                    Gizmos.DrawWireCube(pos, EntryPointCubeSize);
                }
            }
        }
    }
}