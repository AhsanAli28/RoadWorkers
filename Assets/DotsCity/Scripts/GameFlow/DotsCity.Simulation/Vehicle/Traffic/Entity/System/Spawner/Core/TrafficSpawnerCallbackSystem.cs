using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class TrafficSpawnerCallbackSystem : SystemBase
    {
        public event Action<Entity> OnSpawn = delegate { };

        protected override void OnUpdate()
        {
            foreach (var (trafficStartCallbackTag, entity) in SystemAPI.Query<EnabledRefRW<TrafficSpawnCallbackTag>>()
             .WithEntityAccess())
            {
                trafficStartCallbackTag.ValueRW = false;
                OnSpawn(entity);
            }
        }

        public static TrafficSpawnerCallbackSystem RegisterSystem() => DefaultWorldUtils.CreateAndAddSystemManaged<TrafficSpawnerCallbackSystem, MainThreadEventGroup>(true);
    }
}
