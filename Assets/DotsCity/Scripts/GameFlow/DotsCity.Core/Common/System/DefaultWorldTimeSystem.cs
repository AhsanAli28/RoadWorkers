#if UNITY_EDITOR
using Spirit604.DotsCity.Core;
using Unity.Entities;

namespace Spirit604.DotsCity
{
    [UpdateInGroup(typeof(InitGroup), OrderFirst = true)]
    public partial class DefaultWorldTimeSystem : SystemBase
    {
        public float CurrentTime => (float)SystemAPI.Time.ElapsedTime;

        protected override void OnUpdate() { }
    }
}
#endif
