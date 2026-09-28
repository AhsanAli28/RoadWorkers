#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class PathDataNodeDirectionViewer : PathWaypointParamViewerBase<bool>
    {
        #region Overriden methods

        protected override string GetLabelText(bool value)
        {
            string text = $"Backward: {value} ";

            return text;
        }

        protected override bool GetParamValue(PathNode pathNode)
        {
            return pathNode.BackwardDirection;
        }

        protected override Color GetDefaultParamColor(bool value)
        {
            if (value)
            {
                return Color.cyan;
            }

            return Color.white;
        }

        #endregion
    }
}
#endif