using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [RequireComponent(typeof(HybridEntityRuntimeAuthoring))]
    public class CarCapacityRuntimeAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        private readonly Vector3 EntryPointCubeSize = Vector3.one;

        [SerializeField][Range(0, 40)] private int maxCapacity = 1;
        [SerializeField] private List<VehicleEntryRuntimeAuthoring> entryPoints = new List<VehicleEntryRuntimeAuthoring>();
        [SerializeField] private bool showEntryPoint;

        public bool ShowEntryPoint { get => showEntryPoint; set => showEntryPoint = value; }

        ComponentType[] IRuntimeEntityComponentSetProvider.GetComponentSet()
        {
            return new ComponentType[] {
                ComponentType.ReadOnly<VehicleEntryElement>(),
                ComponentType.ReadOnly<CarCapacityComponent>(),
            };
        }

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            if (entryPoints.Count == 0)
            {
                Debug.LogError($"{name} Entry points not assigned!");
            }

            var buffer = entityManager.AddBuffer<VehicleEntryElement>(entity);

            for (int i = 0; i < entryPoints.Count; i++)
            {
                var entityRef = entryPoints[i].GetComponent<IHybridEntityRef>();

                buffer.Add(new VehicleEntryElement()
                {
                    EntryPointEntity = entityRef.RelatedEntity,
                    RightSide = entryPoints[i].transform.localPosition.x > 0
                });
            }

            entityManager.SetComponentData(entity, new CarCapacityComponent()
            {
                MaxCapacity = maxCapacity,
                AvailableCapacity = Random.Range(0, maxCapacity)
            });
        }

        [Button]
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

                var entryAuthoring = entry.gameObject.AddComponent<VehicleEntryRuntimeAuthoring>();
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
                        continue;

                    var pos = entryPoints[i].transform.position + new Vector3(0, EntryPointCubeSize.y / 2);
                    Gizmos.DrawWireCube(pos, EntryPointCubeSize);
                }
            }
        }
    }
}