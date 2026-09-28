using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class DestinationNodeUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetDestination(Random rndGen, in NodeSettingsComponent nodeSettings, in LocalToWorld nodeTransform)
        {
            bool hasMovementRandomOffset = nodeSettings.HasMovementRandomOffset == 1;
            float3 destination = nodeTransform.Position;

            if (hasMovementRandomOffset)
            {
                switch (nodeSettings.NodeShapeType)
                {
                    case NodeShapeType.Circle:
                        {
                            float maxWidth = nodeSettings.MaxPathWidth;

                            var randomOffset = rndGen.NextFloat3(new float3(-maxWidth, 0, -maxWidth), new float3(maxWidth, 0, maxWidth));

                            destination = nodeTransform.Position + randomOffset;

                            break;
                        }
                    case NodeShapeType.Square:
                        {
                            destination = UnityMathematicsExtension.GetRandomPointInRectangle(rndGen, nodeTransform.Position, nodeTransform.Rotation, nodeSettings.MaxPathWidth, nodeSettings.Height);
                            break;
                        }
                    case NodeShapeType.Rectangle:
                        {
                            destination = UnityMathematicsExtension.GetRandomPointInRectangle(rndGen, nodeTransform.Position, nodeTransform.Rotation, nodeSettings.MaxPathWidth, nodeSettings.Height);
                            break;
                        }
                }
            }

            return destination;
        }
    }
}
