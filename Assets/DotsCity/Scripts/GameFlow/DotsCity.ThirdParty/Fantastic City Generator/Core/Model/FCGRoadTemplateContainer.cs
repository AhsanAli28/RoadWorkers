#if FCG
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.FCG
{
    [CreateAssetMenu(menuName = "Spirit604/FCG/FCG Road Template Container")]
    public class FCGRoadTemplateContainer : ScriptableObject
    {
        public List<GameObject> Prefabs = new List<GameObject>();
    }
}
#endif