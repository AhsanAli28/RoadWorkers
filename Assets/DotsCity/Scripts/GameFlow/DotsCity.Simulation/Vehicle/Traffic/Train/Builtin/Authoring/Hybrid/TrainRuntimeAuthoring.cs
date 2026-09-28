using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Traffic;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Train
{
    public class TrainRuntimeAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity, IHybridLinkEntity
    {
        [SerializeField] private float wagonOffset = 0.5f;
        [SerializeField] private List<TrainWagonRuntimeAuthoring> wagons = new List<TrainWagonRuntimeAuthoring>();

        public List<TrainWagonRuntimeAuthoring> Wagons => wagons;

        public float WagonOffset { get => wagonOffset; set => wagonOffset = value; }

        ComponentType[] IRuntimeEntityComponentSetProvider.GetComponentSet()
        {
            return new ComponentType[] {
                ComponentType.ReadOnly<TrainTag>(),
                ComponentType.ReadOnly<TrainDataComponent>(),
                ComponentType.ReadOnly<TrainComponent>(),
                ComponentType.ReadOnly<TrafficWagonElement>(),
            };
        }

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentData(entity, new TrainDataComponent()
            {
                WagonOffset = wagonOffset
            });

            entityManager.SetComponentData(entity, new TrainComponent()
            {
                IsParent = true
            });

            var buffer = entityManager.GetBuffer<TrafficWagonElement>(entity);

            Entity nextEntity = entity;

            for (int i = 0; i < wagons.Count; i++)
            {
                var wagonEntity = wagons[i].GetComponent<IHybridEntityRef>().RelatedEntity;

                buffer.Add(new TrafficWagonElement()
                {
                    Entity = wagonEntity
                });

                entityManager.SetComponentData(wagonEntity, new TrainComponent()
                {
                    IsParent = false,
                    NextEntity = nextEntity
                });

                entityManager.SetComponentData(wagonEntity, new TrafficWagonComponent()
                {
                    OwnerEntity = entity
                });

                nextEntity = wagonEntity;
            }
        }

        void IHybridLinkEntity.Destroy()
        {
            for (int i = 0; i < wagons.Count; i++)
            {
                wagons[i].gameObject.transform.SetParent(transform);
            }
        }
    }
}