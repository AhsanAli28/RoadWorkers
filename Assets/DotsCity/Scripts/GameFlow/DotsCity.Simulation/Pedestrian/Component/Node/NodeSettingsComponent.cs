using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct NodeSettingsComponent : IComponentData
    {
        public PedestrianNodeType NodeType;
        public NodeShapeType NodeShapeType;
        public float Weight;
        public float SumWeight;
        public float CustomAchieveDistance;
        public int CanSpawnInVision;
        public float ChanceToSpawn;
        public float MaxPathWidth;
        public float Height;
        public int HasMovementRandomOffset;

        public bool IsAvailableForSpawnInVision() { return CanSpawnInVision == 1; }

        public bool CanSpawn()
        {
            return RandomHelper.ChanceDropped(ChanceToSpawn);
        }

        public bool CanSpawn(Unity.Mathematics.Random randomGen)
        {
            return UnityMathematicsExtension.ChanceDropped(ChanceToSpawn, randomGen);
        }
    }
}