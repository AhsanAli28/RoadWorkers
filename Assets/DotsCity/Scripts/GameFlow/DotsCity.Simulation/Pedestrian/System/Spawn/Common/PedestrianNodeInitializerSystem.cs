using Spirit604.DotsCity.Core;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class PedestrianNodeInitializerSystem : SystemBase
    {
        public const int MIN_INIT_COUNT = 2;

        private EntityQuery permittedSpawnPointGroup;

        public bool IsInitialized { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            permittedSpawnPointGroup = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                .WithNone<CustomSpawnerTag, CulledEventTag>()
                .WithAny<InViewOfCameraTag, InPermittedRangeTag>()
                .WithAll<NodeSettingsComponent>()
                .Build(this);

            Enabled = false;
        }

        protected override void OnUpdate()
        {
            if (permittedSpawnPointGroup.CalculateEntityCount() >= MIN_INIT_COUNT)
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
