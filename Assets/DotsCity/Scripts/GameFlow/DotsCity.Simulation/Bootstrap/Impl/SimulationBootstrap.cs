using Spirit604.DotsCity.Core.Bootstrap;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Bootstrap
{
    public class SimulationBootstrap : CityBootstrapBase
    {
        protected override void InitCommands()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var worldUnmanaged = World.DefaultGameObjectInjectionWorld.Unmanaged;
            var entityManager = world.EntityManager;

            commands.Clear();
            commands.Add(new LaunchPedestrianConversionCommand(world));
            commands.Add(new LaunchTrafficConversionCommand(world));

            RegisterPlayerSpawn();

            commands.Add(new SceneSectionLoadedCommand(entityManager, this));
            commands.Add(new InitialNodeInitCommand(world, entityManager, this));
            commands.Add(new EnableSceneSystemsCommand(world, entityManager));
            commands.Add(new InitialGraphResolveCommand(entityManager, this));
            commands.Add(new PedestrianSpawnCommand(world, entityManager));
            commands.Add(new TrafficSpawnCommand(world, entityManager));
            commands.Add(new WaitForTrafficCommand(world, entityManager, this));
            commands.Add(new WaitForPedestrianCommand(world, entityManager, this));
            commands.Add(new PostSpawnPedestrianCommand(worldUnmanaged, entityManager));
            Log("InitCommands");
        }

        protected virtual void RegisterPlayerSpawn() { }
    }
}