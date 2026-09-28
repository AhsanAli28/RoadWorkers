using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using System.Threading.Tasks;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class LaunchPedestrianConversionCommand : IBootstrapCommand
    {
        private readonly World world;

        public LaunchPedestrianConversionCommand(World world)
        {
            this.world = world;
        }

        public Task Execute()
        {
            world.GetOrCreateSystemManaged<PedestrianEntityRuntimeConversionSystem>().Enabled = true;
            return Task.CompletedTask;
        }
    }
}