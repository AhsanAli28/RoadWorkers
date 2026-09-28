using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Pedestrian.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class PedestrianPlayerTargetBakingSystem : PedestrianEntityCustomBakingSystemBase
    {
        protected override void Bake(ref EntityCommandBuffer commandBuffer, Entity entity)
        {
            //commandBuffer.AddComponent(entity, new PlayerTargetComponent { ScaleRadius = 1 });
        }
    }
}