#if UNITY_EDITOR
using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor
{
    public static class SceneDataGuiViewPopup
    {
        private static readonly Color32 mainRectColor = new Color32(0, 0, 0, 80);

        private const float selectSquareSize = 25f;

        private const float minWidth = 60f;
        private static float height = 250f;
        private static float width;

        private const float contentPadding = 3f;

        private const float shortInfoRectWidth = 70f;
        private const float longInfoRectWidth = 110f;

        private const float selectButtonHeight = 15;

        private const float sliderHeight = 8f;
        private const float paramSpacing = 6f;
        private const float additiveHeight = 10f;

        private const int MaxCharLabelSize = 6;
        private const int MinFontLabelSize = 7;
        private const int DefaultFontLabelSize = 10;
        private const float DecreasePerCharRate = 3;

        private const float LabelRelativeLongSize = 0.55f;
        private const float LabelRelativeShortSize = 0.4f;

        private const float SliderRelativeOffset = 0.5f;
        private const float SliderRelativeSize = 0.4f;

        private const float SelectButtonRelativeSize = 0.85f;

        private static GUIStyle paramValueGuiStyle;
        private static GUIStyle barTextGuiStyle;
        private static Dictionary<int, GUIStyle> styles;
        private static GameObject selectedObject;

        public static void DrawInfo(GameObject sourceObject, List<FilteredVariableData> customParams, bool fullLabels = false, bool showOnlyClose = false, System.Action<GameObject> selectCallback = null)
        {
            if (sourceObject == null || customParams == null || customParams.Count == 0)
            {
                return;
            }

            if (!CameraExtension.InViewOfSceneView(sourceObject.transform.position))
            {
                return;
            }

            bool closeEnough = true;

            if (showOnlyClose)
            {
                if (selectedObject != sourceObject)
                {
                    Rect clickRect = GetClickRekt(sourceObject);
                    closeEnough = clickRect.Contains(Event.current.mousePosition);

                    if (closeEnough)
                    {
                        selectedObject = sourceObject;
                    }
                }
            }

            var paramCount = customParams.Count;

            if (closeEnough)
            {
                float currentButtonHeight = 0;

                if (selectCallback != null)
                {
                    currentButtonHeight = selectButtonHeight;
                }

                height = paramCount * (sliderHeight + paramSpacing) + contentPadding * 2 + additiveHeight + currentButtonHeight;

                var rectWidth = fullLabels ? longInfoRectWidth : shortInfoRectWidth;

                width = 0;

                width += rectWidth;

                width = Mathf.Clamp(width, minWidth, int.MaxValue);

                var guiPosition = HandleUtility.WorldToGUIPoint(sourceObject.transform.position) - new Vector2(width / 2, 0);
                Rect mainRect = new Rect(guiPosition, new Vector2(width, height));

                Handles.BeginGUI();
                GUILayout.BeginArea(mainRect);

                EditorGUI.DrawRect(new Rect(0, 0, width, height), mainRectColor);

                Rect contentRect = new Rect(0, 0, width - contentPadding * 2, height - contentPadding * 2);

                GUILayout.BeginArea(contentRect);

                DrawParams(customParams, fullLabels);

                if (selectCallback != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Select", GUILayout.Width(width * SelectButtonRelativeSize)))
                    {
                        selectCallback.Invoke(sourceObject);
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndArea();

                GUILayout.EndArea();
                Handles.EndGUI();
            }
            else
            {
                Rect clickRect = GetClickRekt(sourceObject);
                Handles.BeginGUI();
                GUILayout.BeginArea(clickRect);

                EditorGUI.DrawRect(new Rect(0, 0, width, height), Color.yellow);

                GUILayout.EndArea();
                Handles.EndGUI();
            }
        }

        private static Rect GetClickRekt(GameObject sourceObject)
        {
            var clickRectGuiPosition = HandleUtility.WorldToGUIPoint(sourceObject.transform.position) - new Vector2(selectSquareSize / 2, selectSquareSize / 2);
            Rect clickRect = new Rect(clickRectGuiPosition, new Vector2(selectSquareSize, selectSquareSize));
            return clickRect;
        }

        private static void Init()
        {
            if (styles == null)
            {
                styles = new Dictionary<int, GUIStyle>();

                for (int i = 6; i <= 10; i++)
                {
                    var guiStyle = new GUIStyle("label")
                    {
                        fontSize = i,
                    };

                    guiStyle.normal.textColor = Color.white;

                    styles.Add(i, guiStyle);
                }
            }
        }

        private static void DrawParams(List<FilteredVariableData> customParams, bool fullLabels)
        {
            Init();

            if (paramValueGuiStyle == null)
            {
                paramValueGuiStyle = new GUIStyle("label")
                {
                    fontSize = 8,
                    alignment = TextAnchor.MiddleLeft,
                    stretchWidth = true
                };

                paramValueGuiStyle.normal.textColor = Color.white;
            }

            if (barTextGuiStyle == null)
            {
                barTextGuiStyle = new GUIStyle("label")
                {
                    fontSize = 8,
                    alignment = TextAnchor.MiddleLeft,
                    stretchWidth = true
                };

                barTextGuiStyle.normal.textColor = Color.black;
            }

            foreach (var paramData in customParams)
            {
                string label = fullLabels ? paramData.SceneViewName : paramData.SceneViewShortName;

                if (paramData.NumberValue)
                {
                    var floatValue = paramData.FloatValue;
                    var hasRange = paramData.HasRange;

                    DrawDefaultBarParam(label, floatValue, barTextGuiStyle, hasRange, paramData.LerpedValue);
                }
                else
                {
                    var value = paramData.Value;

                    DrawTextParam(label, value.ToString(), paramValueGuiStyle, fullLabels);
                }
            }
        }

        private static void DrawDefaultBarParam(string label, float value, GUIStyle barGuiStyle, bool hasLerpedValue = false, float lerpedValue = 0)
        {
            GUILayout.BeginHorizontal();

            DrawLabel(label, true);
            Color color = Color.green;

            if (hasLerpedValue)
            {
                color = GetColor(lerpedValue);
            }

            var sliderRect = GUILayoutUtility.GetLastRect();

            sliderRect.x += width * SliderRelativeOffset;
            sliderRect.y += 2;

            sliderRect.width = width * SliderRelativeSize - 2 * contentPadding;
            sliderRect.height = sliderHeight;

            EditorGUI.DrawRect(sliderRect, color);

            Rect r = GUILayoutUtility.GetLastRect();
            r.x = sliderRect.x + sliderRect.width * 0.2f;

            EditorGUI.LabelField(r, MathUtilMethods.Round(value, 2).ToString(), barGuiStyle);

            GUILayout.EndHorizontal();
        }

        private static void DrawTextParam(string label, string value, GUIStyle labelGuiStyle, bool fullLabels)
        {
            GUILayout.BeginHorizontal();

            DrawLabel(label, fullLabels);

            GUILayout.FlexibleSpace();
            GUILayout.Label(value, labelGuiStyle);
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        private static void DrawLabel(string label, bool fullLabels)
        {
            var font = GetFontSize(label, MaxCharLabelSize, DefaultFontLabelSize, MinFontLabelSize);

            var style = styles[font];

            float currentWidth = fullLabels ? width * LabelRelativeLongSize : width * LabelRelativeShortSize;

            GUILayout.Label(label, style, GUILayout.Width(currentWidth));
        }

        private static int GetFontSize(string text, int maxCharLabelSize, int defaultLabelSize, int minLabelSize)
        {
            var font = defaultLabelSize;

            int overlayCharCount = text.Length - maxCharLabelSize;

            if (overlayCharCount > 0)
            {
                var decFontSize = Mathf.RoundToInt((float)overlayCharCount / DecreasePerCharRate);
                font = Mathf.Clamp(font - decFontSize, minLabelSize, defaultLabelSize);
            }

            return font;
        }

        private static Color GetColor(float tValue)
        {
            var color = Color.Lerp(Color.red, Color.green, tValue);

            return color;
        }
    }
}
#endif