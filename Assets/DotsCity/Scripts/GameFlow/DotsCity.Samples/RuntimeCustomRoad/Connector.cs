using Spirit604.CityEditor.Road;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.RuntimeCustomRoad
{
    /// <summary>
    /// Example component that contains data about the connection with other roads. Used for example only.
    /// </summary>
    public class Connector : MonoBehaviour
    {
        [SerializeField] private RoadSegmentCreator current;
        [SerializeField] private List<RoadSegmentCreator> connectedRoads = new List<RoadSegmentCreator>();

        public RoadSegmentCreator Current => current;

        public List<RoadSegmentCreator> ConnectedRoads => connectedRoads;

#if UNITY_EDITOR
        private void Reset()
        {
            current = GetComponent<RoadSegmentCreator>();
            Extensions.EditorSaver.SetObjectDirty(this);
        }
#endif
    }
}