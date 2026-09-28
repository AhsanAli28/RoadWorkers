using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public static class PathConnectionExtension
    {
        public static void GetIndexes(ref DynamicBuffer<PathConnectionElement> pathConnections, int globalPathIndex, ref int selectedPathIndex, ref Entity targetNode)
        {
            for (int j = 0; j < pathConnections.Length; j++)
            {
                if (pathConnections[j].GlobalPathIndex == globalPathIndex)
                {
                    selectedPathIndex = j;
                    targetNode = pathConnections[j].ConnectedNodeEntity;
                    break;
                }
            }
        }
    }
}