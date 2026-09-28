using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Chaser
{
    public interface IAIShotTargetProvider : IShootTargetProvider
    {
        public Vector3 TargetForward { get; }
        public Vector3 TargetVelocity { get; }
    }
}