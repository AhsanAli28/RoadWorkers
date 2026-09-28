#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class PathDataSpeedLimitViewer : PathWaypointParamViewerBase<float>
    {
        protected override string PathSaveDictionaryKey => "PathSpeedLimitViewer_SpeedLimitColorDictionary";

        #region Overriden methods

        protected override string GetLabelText(float value)
        {
            string speedLimitText = $"SpeedLimit: {value} ";

            if (value == 0)
            {
                speedLimitText += "(Default Lane Speed)";
            }

            return speedLimitText;
        }

        protected override float GetParamValue(PathNode pathNode)
        {
            return pathNode.SpeedLimit;
        }

        protected override Color GetDefaultParamColor(float speedLimit)
        {
            var color = Color.white;

            if (speedLimit > 0)
            {
                float t = (float)(speedLimit) / ProjectConstants.DefaultLaneSpeed;
                color = Color.Lerp(Color.red, Color.green, t);
            }

            return color;
        }

        #endregion
    }
}
#endif