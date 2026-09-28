using System.Collections.Generic;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class DummyRagdollPrefabProvider : IPedestrianRagdollPrefabProvider
    {
        public List<PedestrianRagdoll> GetPrefabs() => null;
    }
}
