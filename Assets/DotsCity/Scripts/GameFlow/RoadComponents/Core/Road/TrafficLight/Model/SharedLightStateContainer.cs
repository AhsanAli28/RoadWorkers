using Spirit604.CityEditor;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.Gameplay.Config.Road
{
    [CreateAssetMenu(fileName = "Shared Light States", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_ROAD_PATH + "Shared Light States")]
    public class SharedLightStateContainer : ScriptableObject
    {
        [Serializable]
        public class StateContainer
        {
            public List<LightStateInfo> States = new List<LightStateInfo>();

            public float TotalTime => States.Select(light => light.Duration).Sum();
        }

        [SerializeField][Range(1, 5)] private int lightCount = 2;

        [SerializeField]
        private List<StateContainer> stateArray = new List<StateContainer>(5)
        {
            new StateContainer()
            {
                States = new List<LightStateInfo>()
            },
            new StateContainer()
            {
                States = new List<LightStateInfo>()
            }
        };

        public int LightCount { get => lightCount; set => lightCount = value; }

        #region Public methods

        public List<LightStateInfo> GetStates(int index)
        {
            if (lightCount <= index)
            {
                return null;
            }

            EnsureCapacity(index);

            return stateArray[index].States;
        }

        public float GetTotalTime()
        {
            float time = 0;

            for (int i = 0; i < lightCount; i++)
            {
                EnsureCapacity(i);
                time = Mathf.Max(time, stateArray[i].TotalTime);
            }

            return time;
        }

        public void AddState(int handlerIndex, LightState lightState, bool recordUndo = false)
        {
            if (lightCount <= handlerIndex)
            {
                UnityEngine.Debug.Log($"TrafficCrossRoadSettings {name}. Failed to add '{lightState}' state. Handler '{handlerIndex}' not found.");
                return;
            }

#if UNITY_EDITOR
            if (recordUndo)
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Undo New LightState");
            }
#endif

            EnsureCapacity(handlerIndex);

            stateArray[handlerIndex].States.Add(new LightStateInfo() { LightState = lightState });

            EditorSaver.SetObjectDirty(this);
        }

        #endregion

        #region Private methods

        private void EnsureCapacity(int index)
        {
            if (stateArray.Count <= index)
            {
                var count = stateArray.Count - index + 1;

                for (int i = 0; i < count; i++)
                {
                    AddHandler();
                }

                EditorSaver.SetObjectDirty(this);
            }
        }

        private void AddHandler()
        {
            stateArray.Add(new StateContainer()
            {
                States = new List<LightStateInfo>()
            });
        }

        #endregion

        #region Editor events

        public void LightCountChanged()
        {
            if (stateArray.Count < lightCount)
            {
                var addCount = lightCount - stateArray.Count;

                for (int i = 0; i < addCount; i++)
                {
                    AddHandler();
                }

                EditorSaver.SetObjectDirty(this);
            }
        }

        #endregion
    }
}