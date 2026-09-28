using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct PedestrianEntityPrefabContainer : IBufferElementData
    {
        public Entity PrefabEntity;
    }

    public static class PedestrianEntityPrefabContainerExtension
    {
        public static bool HasPrefabVariants(this in DynamicBuffer<PedestrianEntityPrefabContainer> buffer) => buffer.Length > 1;

        public static Entity GetFirstPrefab(this in DynamicBuffer<PedestrianEntityPrefabContainer> buffer)
        {
            return buffer[0].PrefabEntity;
        }

        public static Entity GetRandomPrefab(this in DynamicBuffer<PedestrianEntityPrefabContainer> buffer, Unity.Mathematics.Random random)
        {
            return GetRandomPrefab(in buffer, random);
        }

        public static Entity GetRandomPrefab(this in DynamicBuffer<PedestrianEntityPrefabContainer> buffer, Unity.Mathematics.Random random, out int skinIndex)
        {
            skinIndex = -1;

            if (buffer.Length == 1)
            {
                return buffer[0].PrefabEntity;
            }
            else if (buffer.Length > 1)
            {
                skinIndex = random.NextInt(0, buffer.Length);
                return buffer[skinIndex].PrefabEntity;
            }

            return default;
        }
    }
}
