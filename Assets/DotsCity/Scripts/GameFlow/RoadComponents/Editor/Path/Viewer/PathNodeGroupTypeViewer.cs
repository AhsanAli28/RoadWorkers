#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class PathNodeGroupTypeViewer : PathWaypointParamViewerBase<TrafficGroupType>
    {
        private const int IntBitCount = 32;
        private const int MaxFlagCount = 8;

        protected override string PathSaveDictionaryKey => "PathNodeGroupTypeViewer_Dictionary";

        private StringBuilder sb = new StringBuilder();
        private Dictionary<TrafficGroupType, string> cache = new Dictionary<TrafficGroupType, string>();

        #region Overriden methods

        public override void UpdateData(Path[] paths)
        {
            base.UpdateData(paths);
            cache.Clear();
        }

        protected override string GetLabelText(TrafficGroupType mask)
        {
            string text = string.Empty;

            if (!cache.TryGetValue(mask, out text))
            {
                if (mask == TrafficGroupType.Default)
                {
                    text = "Default";
                }
                else
                {
                    sb.Clear();

                    var flags = mask.GetUniqueFlags();

                    foreach (var flag in flags)
                    {
                        sb.Append(flag.ToString());
                        sb.Append(" ");
                    }

                    text = sb.ToString();
                }

                cache.Add(mask, text);
            }

            return text;
        }

        protected override TrafficGroupType GetParamValue(PathNode pathNode)
        {
            return pathNode.CustomGroup ? pathNode.TrafficGroupMask.GetValue() : TrafficGroupType.Default;
        }

        protected override Color GetDefaultParamColor(TrafficGroupType mask)
        {
            var color = Color.white;

            if (mask != TrafficGroupType.Default)
            {
                color = UnityEngine.Random.ColorHSV();
            }

            return color;
        }

        #endregion
    }
}
#endif