using Unity.Entities;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct GlobalTimeUpdateSystem : ISystem
    {
        private int globalTimeId;

        void ISystem.OnCreate(ref SystemState state)
        {
            globalTimeId = Shader.PropertyToID(Constans.GlobalTime);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            Shader.SetGlobalFloat(globalTimeId, currentTime);
        }
    }
}
