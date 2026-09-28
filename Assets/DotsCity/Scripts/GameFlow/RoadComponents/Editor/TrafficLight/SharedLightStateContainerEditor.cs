using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.Gameplay.Config.Road
{
    [CustomEditor(typeof(SharedLightStateContainer))]
    public class SharedLightStateContainerEditor : Editor
    {
        private List<ReorderableList> reordableLists = new List<ReorderableList>();
        private GUIStyle timeLineStyle;
        private const float prefixLabelOffset = 100f;
        private List<List<LightStateInfo>> list = new List<List<LightStateInfo>>(2);

        private void OnEnable()
        {
            SharedLightStateContainer settings = (SharedLightStateContainer)target;

            InitTimelineStyle();
            InitLists(settings);
        }

        public override void OnInspectorGUI()
        {
            SharedLightStateContainer settings = (SharedLightStateContainer)target;

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("lightCount"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                settings.LightCountChanged();
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                InitLists(settings);
            }

            InitTimelineStyle();

            DrawSettings(settings, list);

            for (int i = 0; i < reordableLists.Count; i++)
            {
                reordableLists[i].DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static void DrawSettings(SharedLightStateContainer settings, List<List<LightStateInfo>> list, GUIStyle timeLineStyle = null)
        {
            if (timeLineStyle == null)
            {
                timeLineStyle = TrafficLightTimingDrawerUtils.GetDefaultTimelineStyle();
            }

            var inspectorWidth = EditorGUIUtility.currentViewWidth - 70f;

            GUILayout.BeginVertical("GroupBox");

            float totalTime = settings.GetTotalTime();

            list.Clear();

            for (int i = 0; i < settings.LightCount; i++)
            {
                list.Add(settings.GetStates(i));
            }

            TrafficLightTimingDrawerUtils.DrawSignalTimings(timeLineStyle, inspectorWidth, prefixLabelOffset, totalTime, handlerStateList: list);

            GUILayout.EndVertical();

            EditorGUILayout.Separator();
        }

        private void InitLists(SharedLightStateContainer settings)
        {
            reordableLists.Clear();

            var statesArray = serializedObject.FindProperty("stateArray");

            for (int i = 0; i < settings.LightCount; i++)
            {
                var prop = statesArray.GetArrayElementAtIndex(i).FindPropertyRelative("States");
                var reordableList = LightStateDrawer.DrawList(prop, serializedObject, settings.GetStates(i), AddItem, $"TrafficLightStates [{i}]", removeCallback: RemoveState);
                reordableLists.Add(reordableList);
            }
        }

        public void AddItem(object obj)
        {
            SharedLightStateContainer settings = (SharedLightStateContainer)target;

            var lightStateAddData = (LightStateAddData)obj;

            LightState lightState = lightStateAddData.LightState;

            var handlerIndex = reordableLists.IndexOf(lightStateAddData.ReorderableList);

            settings.AddState(handlerIndex, lightState, true);
        }

        private void RemoveState(ReorderableList list)
        {
            SharedLightStateContainer settings = (SharedLightStateContainer)target;

            if (list.serializedProperty.arraySize > list.index)
            {
                list.serializedProperty.DeleteArrayElementAtIndex(list.index);
            }

            EditorSaver.SetObjectDirty(settings);
        }

        private void InitTimelineStyle()
        {
            if (timeLineStyle != null)
            {
                return;
            }

            timeLineStyle = TrafficLightTimingDrawerUtils.GetDefaultTimelineStyle();
        }
    }
}