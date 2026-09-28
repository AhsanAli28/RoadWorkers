#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using Spirit604.Gameplay.Road.Debug;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public static class TrafficLightTimingDrawerUtils
    {
        private const float IntervalTimeLength = 10f;
        private const int SliderHeight = 15;

        public static void DrawCrossroadSignalTimings(TrafficLightCrossroad trafficLightCrossroad, GUIStyle timeLineStyle, float inspectorWidth, float prefixLabelOffset, int maxIntervalCount = 10)
        {
            float totalTime = trafficLightCrossroad.GetMaxTotalCycleTime();

            DrawSignalTimings(timeLineStyle, inspectorWidth, prefixLabelOffset, totalTime, maxIntervalCount, trafficLightCrossroad);
        }

        public static void DrawSignalTimings(GUIStyle timeLineStyle, float inspectorWidth, float prefixLabelOffset, float totalTime, int maxIntervalCount = 10, TrafficLightCrossroad trafficLightCrossroad = null, List<List<LightStateInfo>> handlerStateList = null)
        {
            int intervalCount = Mathf.CeilToInt(totalTime / IntervalTimeLength);

            float localTimeLength = IntervalTimeLength;

            float divider = 1;

            if (intervalCount > maxIntervalCount)
            {
                divider = Mathf.CeilToInt((float)intervalCount / (float)maxIntervalCount);
                localTimeLength *= divider;
                intervalCount = Mathf.CeilToInt(totalTime / localTimeLength);
            }

            intervalCount = Mathf.Clamp(intervalCount, 4, int.MaxValue) + 1;
            float intervalScreenWidth = (inspectorWidth - prefixLabelOffset) / intervalCount;

            float intervalScreenFieldWidth = intervalScreenWidth;
            float multiplier = 10;

            multiplier *= divider;

            Vector2 startScreenPoint = default;
            Vector2 endScreenPoint = default;
            DrawTimeLineHeaderInternal(timeLineStyle, prefixLabelOffset, intervalScreenFieldWidth, intervalCount, multiplier, ref startScreenPoint, ref endScreenPoint);

            intervalScreenWidth = (endScreenPoint.x - startScreenPoint.x) / intervalCount;

            if (trafficLightCrossroad != null)
            {
                DrawCrossroadTimingsInternal(trafficLightCrossroad, prefixLabelOffset, localTimeLength, intervalScreenWidth);
            }
            else
            {
                for (int i = 0; i < handlerStateList.Count; i++)
                {
                    var listLocal = handlerStateList[i];
                    DrawHandlerTimingsInternal(listLocal, i, prefixLabelOffset, localTimeLength, intervalScreenWidth);
                }
            }
        }

        public static string GetTimeText(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60);
            seconds = seconds - minutes * 60;
            var secondsText = seconds == 0 ? "00" : seconds.ToString();
            var text = $"{minutes}:{secondsText}";
            return text;
        }

        public static string GetLightName(TrafficLightHandler trafficLightHandler)
        {
            if (trafficLightHandler == null)
            {
                return string.Empty;
            }

            CustomArrowLightSettings arrowLightSettings = null;

            if (trafficLightHandler.TrafficLightCrossroad != null)
            {
                arrowLightSettings = trafficLightHandler.TrafficLightCrossroad.GetCustomLightSettings(trafficLightHandler);
            }

            if (arrowLightSettings == null)
            {
                return GetLightName(trafficLightHandler.RelatedLightIndex);
            }
            else
            {
                return GetArrowName(arrowLightSettings);
            }
        }

        public static string GetLightName(int relatedLightIndex)
        {
            return $"TrafficLight [{relatedLightIndex}]";
        }

        public static string GetArrowName(CustomArrowLightSettings customArrowLightSettings)
        {
            string indexStr1 = "x";
            string indexStr2 = "x";

            if (customArrowLightSettings.currentTrafficLightHandler != null)
            {
                indexStr1 = customArrowLightSettings.currentTrafficLightHandler.RelatedLightIndex.ToString();
            }

            if (customArrowLightSettings.relatedTrafficLightHandler != null)
            {
                indexStr2 = customArrowLightSettings.relatedTrafficLightHandler.RelatedLightIndex.ToString();
            }

            return $"Arrow [{indexStr1}] [L{indexStr2}]";
        }

        public static GUIStyle GetDefaultTimelineStyle()
        {
            var timeLineStyle = new GUIStyle(EditorStyles.textArea);
            timeLineStyle.alignment = TextAnchor.MiddleLeft;
            timeLineStyle.padding = new RectOffset(2, 0, 0, 0);
            timeLineStyle.margin = new RectOffset(0, 0, 0, 0);

            return timeLineStyle;
        }

        private static void DrawCrossroadTimingsInternal(TrafficLightCrossroad trafficLightCrossroad, float prefixLabelOffset, float localTimeLength, float intervalScreenWidth)
        {
            foreach (var item in trafficLightCrossroad.TrafficLightHandlers)
            {
                if (trafficLightCrossroad.IsArrowLight(item.Value))
                {
                    continue;
                }

                if (item.Value == null)
                {
                    EditorGUILayout.LabelField($"TrafficLight [{item.Key}] is NULL ||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||", EditorStyles.boldLabel);
                    continue;
                }

                DrawHandlerTimingsInternal(item.Value.LightStates, item.Value.RelatedLightIndex, prefixLabelOffset, localTimeLength, intervalScreenWidth);

                var arrows = trafficLightCrossroad.GetRelatedLightArrows(item.Value);

                if (arrows != null)
                {
                    for (int i = 0; i < arrows.Length; i++)
                    {
                        DrawArrowTimingsInternal(arrows[i], prefixLabelOffset + 3, localTimeLength, intervalScreenWidth);
                    }
                }
            }
        }

        private static void DrawArrowTimingsInternal(CustomArrowLightSettings settings, float prefixLabelOffset, float intervalTimeLength, float intervalScreenWidth)
        {
            var arrowName = GetArrowName(settings);
            EditorGUILayout.LabelField(arrowName, GUILayout.MaxWidth(prefixLabelOffset));

            var lastRect = GUILayoutUtility.GetLastRect();

            float startOffset = prefixLabelOffset + 2;

            float redStartTime, greenEndTime, greenStartTime, redEndTime;
            TrafficLightTimingUtils.GetArrowTimings(settings, out redStartTime, out greenStartTime, out redEndTime, out greenEndTime);

            var colorRed = Color.red;
            var colorGreen = Color.green;

            float currentRectWidth1 = redStartTime * (intervalScreenWidth) / intervalTimeLength;
            float currentRectWidth2 = greenStartTime * (intervalScreenWidth) / intervalTimeLength;
            float currentRectWidth3 = redEndTime * (intervalScreenWidth) / intervalTimeLength;

            DrawSliderInternal(startOffset, currentRectWidth1, lastRect, colorRed);
            startOffset += currentRectWidth1;

            DrawSliderInternal(startOffset, currentRectWidth2, lastRect, colorGreen);
            startOffset += currentRectWidth2;

            DrawSliderInternal(startOffset, currentRectWidth3, lastRect, colorRed);
            startOffset += currentRectWidth3;

            if (greenEndTime > 0)
            {
                float currentRectWidth4 = greenEndTime * (intervalScreenWidth) / intervalTimeLength;
                DrawSliderInternal(startOffset, currentRectWidth4, lastRect, colorGreen);
                startOffset += currentRectWidth4;
            }
        }

        private static void DrawHandlerTimingsInternal(List<LightStateInfo> lightStates, int index, float prefixLabelOffset, float intervalTimeLength, float intervalScreenWidth)
        {
            var lightName = GetLightName(index);
            EditorGUILayout.LabelField(lightName, GUILayout.MaxWidth(prefixLabelOffset));

            var lastRect = GUILayoutUtility.GetLastRect();

            float startOffset = prefixLabelOffset + 5;
            float currentOffset = startOffset;
            float totalDuration = 0;

            for (int i = 0; i < lightStates?.Count; i++)
            {
                var lightState = lightStates[i];

                var state = lightState.LightState;
                var color = TrafficLightSceneColor.StateToColor(state);
                float duration = lightState.Duration;
                float currentRectWidth = duration * (intervalScreenWidth) / intervalTimeLength;
                totalDuration += duration;

                DrawSliderInternal(currentOffset, currentRectWidth, lastRect, color);

                currentOffset += currentRectWidth;
            }

            GUIStyle gUIStyle = new GUIStyle();
            gUIStyle.normal.textColor = Color.black;

            if (currentOffset == startOffset)
            {
                currentOffset = startOffset + intervalScreenWidth;
            }

            float timeRectXOffset = (startOffset + currentOffset) / 2;
            var timeRect = new Rect(timeRectXOffset, lastRect.y, 50f, SliderHeight);
            var timeText = GetTimeText(totalDuration);

            EditorGUI.LabelField(timeRect, timeText, gUIStyle);
        }

        private static void DrawTimeLineHeaderInternal(GUIStyle timeLineStyle, float offset, float intervalWidth, int count, float multiplier, ref Vector2 startPoint, ref Vector2 endPoint)
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Timeline:", GUILayout.Width(offset));

            for (int i = 0; i < count; i++)
            {
                float seconds = multiplier * i;
                string text = GetTimeText(seconds);
                EditorGUILayout.LabelField(text, timeLineStyle, GUILayout.MaxHeight(15), GUILayout.Width(intervalWidth));

                var lastRect = GUILayoutUtility.GetLastRect();

                if (i == 0)
                {
                    startPoint = lastRect.position;
                }
                if (i == count - 1)
                {
                    endPoint = lastRect.position + new Vector2(lastRect.width, 0);
                }
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawSliderInternal(float offset, float length, Rect rect, Color color)
        {
            var sliderRect = new Rect(rect.x + offset, rect.y, length, SliderHeight);

            EditorGUI.DrawRect(sliderRect, color);
        }
    }
}
#endif