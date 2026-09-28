using Spirit604.CityEditor;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road.Debug;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public partial class TrafficNode : MonoBehaviour
    {
#if UNITY_EDITOR
        private static CityEditorSettings CityEditorSettings => CityEditorSettings.GetOrCreateSettings();

        private void OnDrawGizmos()
        {
            if (!PathDebugger.ShouldDrawEditorPath)
                return;

            DrawArrows();
        }

        private void DrawArrows()
        {
            if (!isOneWay)
            {
                var color = CityEditorSettings.ArrowColor;

                if (MergeNodeMode)
                {
                    color = NodeIsMerged ? CityEditorSettings.MergeArrowColor : Color.red;
                }

                for (int laneIndex = 0; laneIndex < laneCount; laneIndex++)
                {
                    var startPosition = TrafficNodeExtension.GetLanePosition(this, laneIndex);
                    DrawArrow(startPosition, this.GetNodeForward(true), color);
                }
            }

            if (!MergeNodeMode || IsOneWay && HasRightLanes)
            {
                for (int laneIndex = 0; laneIndex < GetLaneCount(!IsOneWay || IsEndOfOneWay); laneIndex++)
                {
                    var startPosition = TrafficNodeExtension.GetLanePosition(this, laneIndex, true);

                    Color color = default;

                    if (!MergeNodeMode)
                    {
                        color = IsOneWay && HasRightLanes ? CityEditorSettings.ArrowColor : CityEditorSettings.ExternalArrowColor;
                    }
                    else
                    {
                        color = CityEditorSettings.MergeArrowColor;
                    }

                    DrawArrow(startPosition, this.GetNodeForward(false), color);
                }
            }
        }

        public static void DrawArrow(Vector3 pos, Vector3 dir, Color color) => GizmosDrawer.DrawArrow(pos, dir, color, CityEditorSettings.ArrowThickness, CityEditorSettings.ArrowLength);
#endif
    }
}