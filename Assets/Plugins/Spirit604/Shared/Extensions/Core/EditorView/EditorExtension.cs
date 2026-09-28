#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class EditorExtension
    {
        public static void DrawWorldString(string text, Vector3 worldPos, Color? colour = null)
        {
            var currentSceneView = SceneView.currentDrawingSceneView;

            if (currentSceneView != null)
            {
                Handles.BeginGUI();

                var restoreColor = GUI.color;

                if (colour.HasValue) GUI.color = colour.Value;
                Vector3 screenPos = currentSceneView.camera.WorldToScreenPoint(worldPos);

                if (screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0)
                {
                    GUI.color = restoreColor;
                    Handles.EndGUI();
                    return;
                }

                Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
                GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + currentSceneView.position.height + 4, size.x, size.y), text);
                GUI.color = restoreColor;
                Handles.EndGUI();
            }
        }

        private static GUIStyle cachedButtonGuiStyle;

        public static void DrawButton(string text, Vector3 worldPosition, float width, System.Action callback, GUIStyle guiStyle = null, int fontSize = 24, bool centralizeGuiAlign = false, bool drawOnlyInView = true)
        {
            if (guiStyle == null)
            {
                if (cachedButtonGuiStyle == null)
                {
                    cachedButtonGuiStyle = new GUIStyle("button");
                    cachedButtonGuiStyle.fontSize = fontSize;
                    cachedButtonGuiStyle.normal.textColor = Color.black;
                }

                guiStyle = cachedButtonGuiStyle;
            }

            if (drawOnlyInView)
            {
                var lastActiveSceneView = SceneView.lastActiveSceneView;

                if (lastActiveSceneView != null)
                {
                    var inView = lastActiveSceneView.camera.InViewOfCamera(worldPosition);

                    if (!inView)
                    {
                        return;
                    }
                }
            }

            var guiPosition = HandleUtility.WorldToGUIPoint(worldPosition);

            if (centralizeGuiAlign)
            {
                guiPosition -= new Vector2(width, width) / 2;
            }

            Rect rect = new Rect(guiPosition, new Vector2(width, width));

            Handles.BeginGUI();

            try
            {
                GUILayout.BeginArea(rect);
            }
            catch
            {
                Handles.EndGUI();
                return;
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(text, guiStyle, GUILayout.Width(width)))
            {
                callback();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        public static void DrawButtons(List<string> texts, Vector3 worldPosition, float width, List<System.Action> callbacks, GUIStyle guiStyle = null, int fontSize = 24, bool centralizeGuiAlign = false)
        {
            if (texts == null || callbacks == null || texts.Count != callbacks.Count)
            {
                return;
            }

            if (guiStyle == null)
            {
                guiStyle = new GUIStyle("button");
                guiStyle.fontSize = fontSize;
                guiStyle.normal.textColor = Color.black;
            }

            var guiPosition = HandleUtility.WorldToGUIPoint(worldPosition);
            int buttonCount = texts.Count;

            if (centralizeGuiAlign)
            {
                guiPosition -= new Vector2(width * buttonCount, width) / 2;
            }

            float padding = 10f * (buttonCount - 1);

            Rect rect = new Rect(guiPosition, new Vector2(width * buttonCount + padding, width));

            Handles.BeginGUI();
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();

            for (int i = 0; i < texts.Count; i++)
            {
                string text = texts[i];
                var callback = callbacks[i];

                if (GUILayout.Button(text, guiStyle, GUILayout.Width(width)))
                {
                    callback();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        public static void DrawButton(Texture texture, Vector3 worldPosition, float width, System.Action callback, GUIStyle guiStyle = null)
        {
            if (guiStyle == null)
            {
                guiStyle = new GUIStyle();
            }

            var guiPosition = HandleUtility.WorldToGUIPoint(worldPosition);
            Rect rect = new Rect(guiPosition, new Vector2(width, width));

            Handles.BeginGUI();
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(texture, guiStyle, GUILayout.Width(width), GUILayout.Height(width)))
            {
                callback();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        public static void DrawGizmosSimpleCube(Vector3 position, Vector3 size, Color color)
        {
            var savedColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireCube(position, size);
            Gizmos.color = savedColor;
        }

        public static void DrawSimpleHandlesCube(Vector3 position, Vector3 size, Color color)
        {
            var savedColor = Handles.color;
            Handles.color = color;
            Handles.DrawWireCube(position, size);
            Handles.color = savedColor;
        }

        public static void DrawArrow(Vector3 sourcePosition, Vector3 direction, float arrowLength, float arrowAngle, float arrowSideLength)
        {
            DrawArrow(sourcePosition, direction, arrowLength, arrowAngle, arrowSideLength, Color.white);
        }

        public static void DrawArrow(Vector3 sourcePosition, Vector3 direction, float arrowLength, float arrowAngle, float arrowSideLength, Color color)
        {
            var point1 = sourcePosition;
            var point2 = point1 + direction * arrowLength;

            var point3 = point2 - Quaternion.Euler(0, arrowAngle, 0) * direction * arrowSideLength;
            var point4 = point2 - Quaternion.Euler(0, -arrowAngle, 0) * direction * arrowSideLength;

            var oldColor = Handles.color;
            Handles.color = color;
            Handles.DrawLine(point1, point2);
            Handles.DrawLine(point2, point3);
            Handles.DrawLine(point2, point4);
            Handles.color = oldColor;
        }

        public static void DrawArrow(Vector3 sourcePosition, Vector3 direction, float arrowAngle, float arrowSideLength)
        {
            DrawArrow(sourcePosition, direction, arrowAngle, arrowSideLength, Color.white);
        }

        public static void DrawArrow(Vector3 sourcePosition, Vector3 direction, float arrowAngle, float arrowSideLength, Color color)
        {
            var point1 = sourcePosition;

            var point3 = point1 - Quaternion.Euler(0, arrowAngle, 0) * direction * arrowSideLength;
            var point4 = point1 - Quaternion.Euler(0, -arrowAngle, 0) * direction * arrowSideLength;

            var oldColor = Handles.color;
            Handles.color = color;
            Handles.DrawLine(point1, point3);
            Handles.DrawLine(point1, point4);
            Handles.color = oldColor;
        }

        static MethodInfo _clearConsoleMethod;
        static MethodInfo clearConsoleMethod
        {
            get
            {
                if (_clearConsoleMethod == null)
                {
                    Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
                    Type logEntries = assembly.GetType("UnityEditor.LogEntries");
                    _clearConsoleMethod = logEntries.GetMethod("Clear");
                }
                return _clearConsoleMethod;
            }
        }

        public static void ClearLogConsole()
        {
            clearConsoleMethod.Invoke(new object(), null);
        }

        public static string GetPath(Transform current)
        {
            if (current.parent == null)
                return "/" + current.name;
            return GetPath(current.parent) + "/" + current.name;
        }

        public static bool IsInLayer(int layer, LayerMask layermask)
        {
            return layermask == (layermask | (1 << layer));
        }

        public static int LayerMaskToLayer(LayerMask layerMask)
        {
            int layerNumber = 0;
            int layer = layerMask.value;
            while (layer > 0)
            {
                layer = layer >> 1;
                layerNumber++;
            }
            return layerNumber - 1;
        }

        public static GameObject GetClosestPrefabInstanceParent(GameObject gameObject)
        {
            var obj = PrefabUtility.GetPrefabInstanceHandle(gameObject);

            if (obj != null)
            {
                return obj as GameObject;
            }

            return null;
        }

        public static T GetOrCreateComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();

            return component == null ? gameObject.AddComponent<T>() : component;
        }

        public static Component GetOrCreateComponent(this GameObject gameObject, Type type)
        {
            var component = gameObject.GetComponent(type);

            return component == null ? gameObject.AddComponent(type) : component;
        }

        public static void DeleteChildComponents<T>(GameObject createdTile, bool recordUndo = false) where T : Component
        {
            var comps = createdTile.GetComponentsInChildren<T>().ToList();

            if (comps != null)
            {
                while (comps.Count > 0)
                {
                    if (!recordUndo)
                    {
                        GameObject.DestroyImmediate(comps[0]);
                    }
                    else
                    {
                        Undo.DestroyObjectImmediate(comps[0]);
                    }

                    comps.RemoveAt(0);
                }
            }
        }

        public static void CollapseUndoCurrentOperations()
        {
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        public static
#if UNITY_2021_2_OR_NEWER
             UnityEditor.SceneManagement.PrefabStage
#else
                 UnityEditor.Experimental.SceneManagement.PrefabStage
#endif
            GetCurrentPrefabStage()
        {
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#else
            UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
#endif

            return prefabStage;
        }

        public static GameObject GetPrefabStageRoot()
        {
            var prefabStage = GetCurrentPrefabStage();

            return GetPrefabStageRoot(prefabStage);
        }

        public static GameObject GetPrefabStageRoot(
#if UNITY_2021_2_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage prefabStage
#else
            UnityEditor.Experimental.SceneManagement.PrefabStage prefabStage
#endif
            )
        {
            if (prefabStage != null)
                return prefabStage.scene.GetRootGameObjects()[0];

            return null;
        }

        public static string ClearPathSlashes(this string sourceCamelString, bool autoTrim = true)
        {
            if (autoTrim)
            {
                sourceCamelString = sourceCamelString.Trim();
            }

            if (sourceCamelString[0] == '/')
            {
                sourceCamelString = sourceCamelString.Substring(1, sourceCamelString.Length - 1);
            }

            if (sourceCamelString[sourceCamelString.Length - 1] == '/')
            {
                sourceCamelString = sourceCamelString.Substring(0, sourceCamelString.Length - 1);
            }

            return sourceCamelString;
        }

        public static string GetPrefsKeyWithProjectPrefix(string sourceKey)
        {
            return $"{PlayerSettings.companyName}.{PlayerSettings.productName}.{sourceKey}";
        }

        public static string GetUniquePrefsKey(string sourceKey)
        {
            int projectPathHash = Application.dataPath.GetHashCode();

            return $"{sourceKey}_{projectPathHash}";
        }

        public static AnimationCurve GetCopyCurve(AnimationCurve source)
        {
            AnimationCurve copyCurve = new AnimationCurve(source.keys);

            copyCurve.preWrapMode = source.preWrapMode;
            copyCurve.postWrapMode = source.postWrapMode;

            return copyCurve;
        }

        public static void SceneFocus(Vector3 position, float size = 5f, bool instant = false)
        {
            SceneView.lastActiveSceneView?.Frame(new Bounds(position, Vector3.one * size), instant);
        }

        public static void FocusSceneViewTab()
        {
            SceneView.lastActiveSceneView?.Focus();

            try
            {
                SceneView.lastActiveSceneView?.SendEvent(EditorGUIUtility.CommandEvent("SetSceneViewMotionHotControlEventCommand"));
            }
            catch { }
        }
    }
}
#endif