#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using System;

namespace Spirit604.CityEditor.Road
{
    public static class RoadEditorEvents
    {
        public static event Action<TrafficNode> OnTrafficNodeAdd = delegate { };
        public static event Action<TrafficNode> OnTrafficNodeRemove = delegate { };

        public static void AddNode(TrafficNode node) => OnTrafficNodeAdd.Invoke(node);

        public static void RemoveNode(TrafficNode node) => OnTrafficNodeRemove.Invoke(node);
    }
}
#endif
