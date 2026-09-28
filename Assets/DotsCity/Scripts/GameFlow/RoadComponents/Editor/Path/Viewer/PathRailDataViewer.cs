#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class PathRailDataViewer : PathDataSimpleParamViewerBase<bool>
    {
        protected override string PathTypeSaveKey { get => $"PathRail_ColorDictionary"; set => base.PathTypeSaveKey = value; }

        protected override bool Editable => false;

        public override bool ShouldShowPathButton(Path path)
        {
            bool show = true;

            var data = GetData(GetValue(path), out var found, true);

            if (found)
            {
                show = data.Enabled;
            }

            return show;
        }

        protected override bool GetValue(Path path)
        {
            return path.Rail;
        }

        protected override Color GetDefaultParamColor(bool value)
        {
            if (value)
            {
                return Color.cyan;
            }

            return Color.white;
        }

        protected override string GetLabelRowText(bool keyValue)
        {
            return $"Rail: {keyValue}";
        }
    }
}
#endif