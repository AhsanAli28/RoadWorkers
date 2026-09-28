using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct SpawnParams
    {
        public DestinationComponent DestinationComponent;
        public RigidTransform RigidTransform;
        public PedestrianEntitySpawnerSystem.CaptureNodeInfo CapturedNodeInfo;
        public uint Seed;
        public int SkinIndex;

        public SpawnParams(
            uint seed,
            DestinationComponent destinationComponent,
            RigidTransform rigidTransform,
            PedestrianEntitySpawnerSystem.CaptureNodeInfo capturedNodeInfo,
            int skinIndex = -1)
        {
            DestinationComponent = destinationComponent;
            RigidTransform = rigidTransform;
            CapturedNodeInfo = capturedNodeInfo;
            Seed = seed;
            SkinIndex = skinIndex;
        }
    }
}
