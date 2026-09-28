#if FCG
using Spirit604.CityEditor.Road;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.FCG
{
    public class FCGPrefab : MonoBehaviour
    {
        [field: SerializeField] public string BindingName { get; set; }
        [field: SerializeField] public List<RoadSegmentCreator> InnerSegments { get; set; }
    }
}
#endif