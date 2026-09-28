using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    [CreateAssetMenu(menuName = HybridComponentBase.BasePath + "TrafficCustomRaycastTargetTagHybrid")]
    public class CustomRaycastTargetHybridComponent : HybridComponentBase, IRuntimeEntityComponentSetProvider
    {
        public ComponentType[] GetComponentSet() => new ComponentType[] { typeof(TrafficCustomRaycastTargetTag) };
    }
}
