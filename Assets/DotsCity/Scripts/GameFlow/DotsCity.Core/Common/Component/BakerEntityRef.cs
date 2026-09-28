using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    /// <summary>
    /// Component is created to clean up the entire unused object hierarchy & and leave only the created entity.
    /// </summary>
    [BakingType]
    public struct BakerEntityRef : IComponentData
    {
        public Entity LinkedEntity;
    }
}