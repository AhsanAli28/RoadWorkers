using Spirit604.DotsCity.Core;
using Unity.Entities;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial class HybridEntityAdapterSyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithChangeFilter<CullStateComponent>()
            .ForEach((
                HybridEntityAdapter adapter,
                in CullStateComponent cullStateComponent) =>
            {
                adapter.CheckCullState(cullStateComponent.State);
            }).Run();
        }
    }
}