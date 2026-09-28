using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using System.Threading.Tasks;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class LaunchTrafficConversionCommand : IBootstrapCommand
    {
        private readonly World world;

        public LaunchTrafficConversionCommand(World world)
        {
            this.world = world;
        }

        public Task Execute()
        {
            world.GetOrCreateSystemManaged<TrafficEntityRuntimeConversionSystem>().Enabled = true;
            return Task.CompletedTask;
        }
    }
}