using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Factory.Traffic;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class TrafficInitializeHybridHullSystem : BeginSimulationSystemBase
    {
        private TrafficCarPoolGlobal trafficPoolGlobal;

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithNone<TrafficWagonComponent>()
            .ForEach((
                Entity trafficEntity,
                in LocalTransform localTransform,
                in CarLoadHullTag carLoadHullTag,
                in CarModelComponent carModelComponent) =>
            {
                commandBuffer.SetComponentEnabled<CarLoadHullTag>(trafficEntity, false);

                var car = trafficPoolGlobal.GetCarGo(carLoadHullTag.TrafficEntityType, carModelComponent.Value);

                if (!car)
                {
                    UnityEngine.Debug.Log($"TrafficInitializeHybridHullSystem. Hybrid hull type '{carLoadHullTag.TrafficEntityType}' Model '{carModelComponent.Value}' not found.");
                    return;
                }

                EntityManager.AddComponentObject(trafficEntity, car.transform);

                var vehicleRef = car.GetComponent<IVehicleEntityRef>();

                if (vehicleRef != null)
                    vehicleRef.Initialize(trafficEntity);

                if (carLoadHullTag.TrafficEntityType != EntityType.HybridEntityMonoPhysics)
                {
                    commandBuffer.SetComponentEnabled<CopyTransformToGameObject>(trafficEntity, true);
                }
                else
                {
                    if (vehicleRef != null && vehicleRef is CarEntityAdapter)
                    {
                        var entityAdapter = vehicleRef as CarEntityAdapter;
                        EntityManager.AddComponentObject(trafficEntity, entityAdapter);
                    }
                    else if (vehicleRef != null && vehicleRef is PhysicsHybridEntityAdapter)
                    {
                        var entityAdapter = vehicleRef as PhysicsHybridEntityAdapter;
                        EntityManager.AddComponentObject(trafficEntity, entityAdapter);
                        EntityManager.AddComponentObject(trafficEntity, entityAdapter.GetComponent<UnityEngine.Rigidbody>());

                        if (!entityAdapter.CullPhysics)
                        {
                            entityAdapter.SetPhysicsState(Core.CullState.InViewOfCamera);
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"TrafficInitializeHybridHullSystem. HybridEntityMonoPhysics Model '{carModelComponent.Value}' 'CarEntityAdapter' component not found.");
                    }
                }

            }).Run();

            AddCommandBufferForProducer();
        }

        public void Initialize(TrafficCarPoolGlobal trafficPoolGlobal)
        {
            this.trafficPoolGlobal = trafficPoolGlobal;
            Enabled = true;
        }
    }
}