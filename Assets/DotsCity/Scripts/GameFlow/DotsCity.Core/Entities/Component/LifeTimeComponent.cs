using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    public struct LifeTimeComponent : IComponentData
    {
        /// <summary>
        /// Elapsed system timestamp.
        /// </summary>
        public float DestroyTimeStamp;
    }
}