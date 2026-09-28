using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    [TemporaryBakingType]
    public struct CarWheelTempBakingData : IComponentData
    {
        public float WheelBase;
        public NativeArray<WheelData> AllWheels;
        public NativeArray<Entity> SteeringWheels;
    }

    public struct WheelData
    {
        public Entity Entity;
        public quaternion InitialRotation;
        public sbyte InverseValue;
    }

    public class CarWheelAuthoring : MonoBehaviour, IVehicleAuthoring
    {
        [SerializeField][Range(0.01f, 3f)] private float wheelBase = 0.3f;

        [SerializeField] private List<GameObject> allWheels = new List<GameObject>();

        [SerializeField] private List<GameObject> steeringWheels = new List<GameObject>();

        public float WheelRadius { get => wheelBase; set => wheelBase = value; }

        public void AddWheel(GameObject newWheel)
        {
            allWheels.TryToAdd(newWheel);
        }

        public void InsertWheel(GameObject newWheel, int index)
        {
            AddWheel(newWheel);
        }

        public void AddSteeringWheel(GameObject newWheel)
        {
            var currentWheel = newWheel;

            steeringWheels.TryToAdd(currentWheel);
        }

        public void SetDirty()
        {
            EditorSaver.SetObjectDirty(this);
        }

        class CarWheelAuthoringBaker : Baker<CarWheelAuthoring>
        {
            public override void Bake(CarWheelAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                CarWheelTempBakingData carWheelTempBakingData = new CarWheelTempBakingData()
                {
                    WheelBase = authoring.WheelRadius,
                    AllWheels = new NativeArray<WheelData>(authoring.allWheels.Count, Allocator.Temp),
                    SteeringWheels = new NativeArray<Entity>(authoring.steeringWheels.Count, Allocator.Temp),
                };

                for (int i = 0; i < authoring.allWheels.Count; i++)
                {
                    var wheelEntity = GetEntity(authoring.allWheels[i], TransformUsageFlags.Dynamic);

                    carWheelTempBakingData.AllWheels[i] = new WheelData()
                    {
                        Entity = wheelEntity,
                        InitialRotation = authoring.allWheels[i].transform.localRotation,
                        InverseValue = Mathf.Abs(authoring.allWheels[i].transform.localRotation.eulerAngles.y) < 90 ? (sbyte)1 : (sbyte)-1,
                    };
                }

                for (int i = 0; i < authoring.steeringWheels.Count; i++)
                {
                    var wheelEntity = GetEntity(authoring.steeringWheels[i], TransformUsageFlags.Dynamic);

                    carWheelTempBakingData.SteeringWheels[i] = wheelEntity;
                }

                var wheelBuffer = AddBuffer<VehicleWheel>(entity);
                wheelBuffer.Capacity = authoring.allWheels.Count;

                for (int i = 0; i < authoring.allWheels.Count; i++)
                {
                    var wheelEntity = GetEntity(authoring.allWheels[i], TransformUsageFlags.Dynamic);

                    wheelBuffer.Add(new VehicleWheel()
                    {
                        WheelEntity = wheelEntity
                    });
                }

                AddComponent(entity, carWheelTempBakingData);
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class CarWheelBakingSystem : SystemBase
    {
        private EntityQuery bakingWheelQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingWheelQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CarWheelTempBakingData>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingWheelQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .ForEach((
                Entity entity,
                in CarWheelTempBakingData carWheelTempBakingData) =>
            {
                for (int i = 0; i < carWheelTempBakingData.AllWheels.Length; i++)
                {
                    var wheelData = carWheelTempBakingData.AllWheels[i];
                    var wheelEntity = wheelData.Entity;

                    bool isStering = carWheelTempBakingData.SteeringWheels.Contains(wheelEntity);

                    commandBuffer.AddComponent(wheelEntity, new DefaultWheelData()
                    {
                        VehicleEntity = entity,
                        InitialRotation = wheelData.InitialRotation,
                        WheelBase = carWheelTempBakingData.WheelBase,
                        Steering = isStering,
                        InverseValue = wheelData.InverseValue,
                    });

                    commandBuffer.AddComponent<WheelHandlingTag>(wheelEntity);
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}