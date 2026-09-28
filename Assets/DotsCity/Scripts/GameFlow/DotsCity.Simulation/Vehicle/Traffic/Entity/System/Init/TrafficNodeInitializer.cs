using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class TrafficNodeInitializer : SystemBase
    {
        public const int MIN_INIT_COUNT = 2;

        private EntityQuery trafficNodeGroup;

        public bool IsInitialized { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            trafficNodeGroup = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                .WithNone<CulledEventTag>()
                .WithAny<InViewOfCameraTag, InPermittedRangeTag>()
                .WithAll<TrafficNodeComponent, TrafficNodeAvailableTag>()
                .Build(this);

            Enabled = false;
        }

        protected override void OnUpdate()
        {
            if (trafficNodeGroup.CalculateEntityCount() >= MIN_INIT_COUNT)
            {
                IsInitialized = true;
                Enabled = false;
            }
        }

        public void Launch()
        {
            Enabled = true;
        }
    }
}
