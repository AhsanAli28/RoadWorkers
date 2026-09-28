using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Train
{
    public struct TrainTag : IComponentData { }
    public struct TrainParentTag : IComponentData { }

    public struct TrainDataComponent : IComponentData
    {
        public float WagonOffset;
    }

    public struct TrainComponent : IComponentData
    {
        public bool IsParent;
        public Entity NextEntity;
    }

    public struct CustomTrainTag : IComponentData { }

    public struct TrainWagonInitTag : IComponentData, IEnableableComponent { }
    public struct TrainWagonMonoInitTag : IComponentData, IEnableableComponent { }
}
