using Spirit604.AnimationBaker;
using System.Collections.Generic;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianCrowdSkinFactory : CrowdSkinFactory, IPedestrianRagdollPrefabProvider, IPedestrianSkinInfoProvider
    {
        public List<PedestrianRagdoll> GetPrefabs()
        {
            return CharacterAnimationContainer.GetRagdollPrefabs<PedestrianRagdoll>();
        }
    }
}
