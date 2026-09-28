using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public abstract class RoadWrapperBase
    {
        public GameObject SceneObject { get; protected set; }

        public string Name => SceneObject.name;
    }
}
