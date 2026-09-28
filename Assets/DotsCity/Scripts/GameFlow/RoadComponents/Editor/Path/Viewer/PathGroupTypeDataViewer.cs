#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class PathGroupTypeDataViewer : PathDataSimpleParamViewerBase<TrafficGroupType>
    {
        private const int ShortStringLength = 6;
        private const int LongLineWordCount = 2;
        private const int ShortLineWordCount = 3;

        private class CachedData
        {
            public string Text;
            public int Lines;
        }

        private StringBuilder sb = new StringBuilder();
        private Dictionary<TrafficGroupType, CachedData> cache = new Dictionary<TrafficGroupType, CachedData>();

        protected override string PathTypeSaveKey { get => $"TrafficType_ColorDictionary"; set => base.PathTypeSaveKey = value; }

        public override void UpdateData(Path[] paths)
        {
            base.UpdateData(paths);
            cache.Clear();
        }

        public override bool ShouldShowPathButton(Path path)
        {
            bool show = true;

            var data = GetData(path.TrafficGroup, out var found);

            if (found)
            {
                show = data.Enabled;
            }

            return show;
        }

        protected override void DrawLabel(KeyValuePair<TrafficGroupType, ColorPathData> nodeData)
        {
            if (TrafficGroupMaskSettings.GetDefault() != nodeData.Key)
            {
                EditorGUILayout.LabelField("TrafficGroupType: ", GUILayout.MaxWidth(110f));
            }
            else
            {
                EditorGUILayout.LabelField($"TrafficGroupType {Environment.NewLine}(Default): ", GUILayout.MaxWidth(110f), GUILayout.Height(30));
            }

            var cachedData = GetLabel(nodeData);

            EditorGUILayout.TextArea(cachedData.Text, GUILayout.Height((21 - (cachedData.Lines - 1) * 2) * cachedData.Lines), GUILayout.Width(150));
        }

        private CachedData GetLabel(KeyValuePair<TrafficGroupType, ColorPathData> nodeData)
        {
            CachedData cachedData = null;
            var key = nodeData.Key;

            if (!cache.TryGetValue(key, out cachedData))
            {
                var flags = key.GetUniqueFlags();

                int counter = 0;
                int lines = 1;

                sb.Clear();

                var count = flags.Count();

                if (count == 0)
                {
                    sb.Append("No groups");
                }

                int localCounter = 0;

                foreach (var flag in flags)
                {
                    counter++;
                    localCounter++;

                    if (count != counter)
                    {
                        var flagText = flag.ToString();
                        sb.Append($"{flagText}, ");

                        bool shortString = flagText.Length <= ShortStringLength;

                        if (!shortString && localCounter % LongLineWordCount == 0 || shortString && localCounter % ShortLineWordCount == 0)
                        {
                            sb.Append(Environment.NewLine);
                            lines++;
                            localCounter = 0;
                        }
                    }
                    else
                    {
                        sb.Append($"{flag}");
                    }
                }

                cachedData = new CachedData()
                {
                    Text = sb.ToString(),
                    Lines = lines
                };

                cache.Add(key, cachedData);
            }

            return cachedData;
        }

        protected override TrafficGroupType GetValue(Path path)
        {
            return path.TrafficGroup;
        }
    }
}
#endif