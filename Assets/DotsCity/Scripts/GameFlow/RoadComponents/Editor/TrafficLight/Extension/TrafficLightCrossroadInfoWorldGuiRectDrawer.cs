#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using Spirit604.Gameplay.Road.Debug;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road.Debug
{
    public static class TrafficLightCrossroadInfoWorldGuiRectDrawer
    {
        private const float sliderHeight = 10f;
        private const float buttonHeight = 15f;
        private const float buttonRectPadding = 10f;
        private const float headerHeight = 16f;
        private const float prefixWidth = 10f;
        private const float sliderPadding = 2f;
        private const float windowWidth = 100f;
        private const byte rectAlpha = 70;
        private static readonly Vector2 padding = new Vector2(5, 5);

        private static Vector2 sliderStartOffset;
        private static float totalDuration;
        private static int handlerCount = 0;
        private static float screenLengthPerDuration;
        private static GUIStyle headerGuiStyle;
        private static GUIStyle prefixGuiStyle;

        public static void DrawInfo(TrafficLightCrossroad trafficLightCrossroad, Vector3 worldPosition, bool allowDisabled = false)
        {
            headerGuiStyle = new GUIStyle();
            headerGuiStyle.fontSize = 12;
            headerGuiStyle.normal.textColor = Color.white;
            headerGuiStyle.alignment = TextAnchor.MiddleCenter;

            prefixGuiStyle = new GUIStyle();
            prefixGuiStyle.fontSize = 8;
            prefixGuiStyle.normal.textColor = Color.white;
            prefixGuiStyle.alignment = TextAnchor.MiddleLeft;

            if (trafficLightCrossroad == null)
            {
                return;
            }

            bool hasLights = trafficLightCrossroad.HasLights;

            if (hasLights)
            {
                DrawInfoEnabled(trafficLightCrossroad, worldPosition);
            }
            else if (allowDisabled)
            {
                DrawInfoDisabled(trafficLightCrossroad, worldPosition);
            }
        }

        private static void DrawInfoEnabled(TrafficLightCrossroad trafficLightCrossroad, Vector3 worldPosition)
        {
            Action<Rect> action = (contentRect) =>
            {
                DrawStateContent(trafficLightCrossroad, contentRect);
            };

            DrawRect(trafficLightCrossroad, worldPosition, action);
        }

        private static void DrawRect(TrafficLightCrossroad trafficLightCrossroad, Vector3 worldPosition, Action<Rect> content)
        {
            var position = worldPosition;

            var slidersCount = trafficLightCrossroad.HasLights ? trafficLightCrossroad.GetHandlerCount() : 1;

            float width = padding.x * 2 + prefixWidth + windowWidth;
            var height = padding.y * 2 + headerHeight + 10f * slidersCount + buttonHeight + buttonRectPadding + (slidersCount - 1) * sliderPadding;

            var guiPosition = HandleUtility.WorldToGUIPoint(position) + new Vector2(-width / 2, 0);
            Rect mainRect = new Rect(guiPosition, new Vector2(width, height));

            Handles.BeginGUI();
            GUILayout.BeginArea(mainRect);

            EditorGUI.DrawRect(new Rect(0, 0, width, height), new Color32(0, 0, 0, rectAlpha));

            Rect contentRect = new Rect(padding, new Vector2(width - padding.x * 2, height - padding.y * 2 - buttonHeight - buttonRectPadding));

            GUILayout.BeginArea(contentRect);

            EditorGUI.LabelField(new Rect(0, 0, contentRect.width, headerHeight), "Light Info", headerGuiStyle);

            content?.Invoke(contentRect);

            GUILayout.EndArea();

            var buttonRect = new Rect(padding.x, padding.y + contentRect.size.y + buttonRectPadding, width, buttonHeight);

            GUILayout.BeginArea(buttonRect);

            if (GUILayout.Button("Select", GUILayout.Height(buttonHeight), GUILayout.Width(contentRect.size.x)))
            {
                Selection.activeObject = trafficLightCrossroad;
            }

            GUILayout.EndArea();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void DrawStateContent(TrafficLightCrossroad trafficLightCrossroad, Rect contentRect)
        {
            var maxDuration = trafficLightCrossroad.GetMaxTotalCycleTime();

            sliderStartOffset = new Vector2(prefixWidth, headerHeight);
            handlerCount = 0;

            screenLengthPerDuration = contentRect.width / maxDuration;

            foreach (var trafficLightHandlerData in trafficLightCrossroad.TrafficLightHandlers)
            {
                var trafficLightHandler = trafficLightHandlerData.Value;

                if (!trafficLightHandler || trafficLightHandler.LightStates == null)
                {
                    continue;
                }

                var states = trafficLightHandler.LightStates;

                string prefixLabelText = trafficLightHandler.RelatedLightIndex.ToString();

                DrawStates(prefixLabelText, states, prefixGuiStyle);

                var arrows = trafficLightCrossroad.GetRelatedLightArrows(trafficLightHandler);

                if (arrows != null)
                {
                    foreach (var arrow in arrows)
                    {
                        var arrowStates = arrow.GetStates();

                        prefixLabelText = "A" + trafficLightHandler.RelatedLightIndex.ToString();
                        DrawStates(prefixLabelText, arrowStates, prefixGuiStyle);
                    }
                }
            }
        }

        private static void DrawInfoDisabled(TrafficLightCrossroad trafficLightCrossroad, Vector3 worldPosition)
        {
            Action<Rect> action = (contentRect) =>
            {
                var rect = new Rect(new Vector2(contentRect.x, contentRect.y), contentRect.size);
                EditorGUI.LabelField(rect, "[DISABLED]", headerGuiStyle);
            };

            DrawRect(trafficLightCrossroad, worldPosition, action);
        }

        private static void DrawStates(string prefixLabelText, System.Collections.Generic.List<LightStateInfo> states, GUIStyle prefixGuiStyle)
        {
            totalDuration = 0;

            float yOffset = sliderStartOffset.y + handlerCount * (sliderHeight + sliderPadding);
            EditorGUI.LabelField(new Rect(0, yOffset, 20f, sliderHeight), prefixLabelText, prefixGuiStyle);

            foreach (var state in states)
            {
                var color = TrafficLightSceneColor.StateToColor(state.LightState);
                var duration = state.Duration;

                var sliderLength = duration * screenLengthPerDuration;

                var offset = new Vector2(totalDuration * screenLengthPerDuration, sliderHeight * handlerCount);

                var sliderRect = new Rect(sliderStartOffset.x + offset.x, yOffset, sliderLength, sliderHeight);

                EditorGUI.DrawRect(sliderRect, color);

                totalDuration += duration;
            }

            GUIStyle gUIStyle = new GUIStyle();
            gUIStyle.normal.textColor = Color.black;
            gUIStyle.fontSize = 10;
            float timeRectXOffset = (sliderStartOffset.x + totalDuration * screenLengthPerDuration) / 2 - 9;
            var timeRect = new Rect(timeRectXOffset, yOffset - 1, 50, sliderHeight);
            var timeText = TrafficLightTimingDrawerUtils.GetTimeText(totalDuration);

            EditorGUI.LabelField(timeRect, timeText, gUIStyle);

            handlerCount++;
        }
    }
}
#endif