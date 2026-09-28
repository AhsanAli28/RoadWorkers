using System;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        [Serializable]
        public class LightObjectBindingData
        {
            public int Index = -1;
            public int Side;
            public LightType LightType;
        }
    }
}