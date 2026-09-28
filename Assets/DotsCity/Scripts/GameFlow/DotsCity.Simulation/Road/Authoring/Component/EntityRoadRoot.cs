using Spirit604.Gameplay.Road;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    public class EntityRoadRoot : MonoBehaviour
    {
        [SerializeField] private RoadParent roadParent;

        [SerializeField] private Transform roadParentRoot;

        [SerializeField] private Transform pedestrianNodesRoot;

        [SerializeField] private Transform lightsRoot;

        [SerializeField] private Transform propsRoot;

        [SerializeField] private Transform toolsRoot;

        [SerializeField] private Transform surfaceRoot;

        public RoadParent RoadParent { get => roadParent; set => roadParent = value; }
        public Transform RoadParentRoot { get => roadParentRoot; set => roadParentRoot = value; }
        public Transform PedestrianNodesRoot { get => pedestrianNodesRoot; set => pedestrianNodesRoot = value; }
        public Transform LightsRoot { get => lightsRoot; set => lightsRoot = value; }
        public Transform PropsRoot { get => propsRoot; set => propsRoot = value; }
        public Transform ToolsRoot { get => toolsRoot; set => toolsRoot = value; }
        public Transform SurfaceRoot { get => surfaceRoot; set => surfaceRoot = value; }
    }
}
