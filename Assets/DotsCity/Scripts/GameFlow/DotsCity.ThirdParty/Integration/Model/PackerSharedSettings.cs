using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.Integration
{
    //[CreateAssetMenu(menuName = "Spirit604/City/Level/PackerSharedSettings")]
    public class PackerSharedSettings : ScriptableObject
    {
        public PedestrianNode PedestrianNodePrefab;

        public List<ScriptableObject> DefaultConfigs = new List<ScriptableObject>();
    }
}