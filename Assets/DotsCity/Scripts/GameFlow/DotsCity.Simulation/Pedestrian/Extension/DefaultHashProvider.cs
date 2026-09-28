using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class DefaultHashProvider : IAnimationHashProvider
    {
        public int GetHash(string name) => Animator.StringToHash(name);
    }
}
