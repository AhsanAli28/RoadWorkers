using Pathfinding;
using Unity.Entities;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public class CustomEntityNode : PointNode
    {
        public Entity Entity;

        public CustomEntityNode(AstarPath active) : base(active) { }
    }
}
