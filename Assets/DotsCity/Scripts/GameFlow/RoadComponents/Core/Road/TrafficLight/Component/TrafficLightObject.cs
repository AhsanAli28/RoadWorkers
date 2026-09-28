using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class TrafficLightObject : MonoBehaviour
    {
        #region Helper types

        [Serializable]
        public class TrafficLightFrameData
        {
            public List<TrafficLightFrameBase> TrafficLightFrames = new List<TrafficLightFrameBase>();
        }

        [Serializable]
        public class TrafficLightFrameDataDictionary : AbstractSerializableDictionary<int, TrafficLightFrameData> { }

        #endregion

        #region Serialized Variables

        [SerializeField] private TrafficLightCrossroad trafficLightCrossroad;
        [SerializeField] private int connectedId;
        [SerializeField] private TrafficLightFrameDataDictionary trafficLightFrameData;

        #endregion

        #region Properties     

        public TrafficLightCrossroad TrafficLightCrossroad => trafficLightCrossroad;

        public int ConnectedId { get => connectedId; set => connectedId = value; }

        public TrafficLightFrameDataDictionary TrafficLightFrames { get => trafficLightFrameData; }

        #endregion

        #region Methods     

        public void SetIndexOffset(int indexOffset)
        {
            int maxIndex = 0;

            if (trafficLightFrameData.Keys?.Count > 0)
            {
                maxIndex = trafficLightFrameData.Keys.ToList().Max() + 1;
            }

            maxIndex = Mathf.Max(2, maxIndex);

            var frames = GetComponentsInChildren<TrafficLightFrameBase>().ToList();

            var tempTrafficLightFrames = new TrafficLightFrameDataDictionary();

            foreach (var frame in frames)
            {
                var currentIndex = GetCurrentIndexFrame(frame);
                var newIndex = (currentIndex + indexOffset) % maxIndex;

                if (!tempTrafficLightFrames.ContainsKey(newIndex))
                {
                    tempTrafficLightFrames.Add(newIndex, new TrafficLightFrameData() { TrafficLightFrames = new List<TrafficLightFrameBase>() });
                }

                tempTrafficLightFrames[newIndex].TrafficLightFrames.TryToAdd(frame);
            }

            trafficLightFrameData = tempTrafficLightFrames;

            EditorSaver.SetObjectDirty(this);
        }

        public bool SetFrameIndex(int newIndex)
        {
            if (trafficLightFrameData.Keys.Count > 1)
                throw new ArgumentException("This method can work if the frame has only 1 frame index");

            if (trafficLightFrameData.Keys.Count == 0)
                throw new ArgumentException("No frame exists");

            var key = trafficLightFrameData.Keys.ElementAt(0);

            return ChangeFrameIndex(key, newIndex);
        }

        public bool ChangeFrameIndex(int oldIndex, int newIndex)
        {
            if (!trafficLightFrameData.ContainsKey(oldIndex))
                return false;

            if (oldIndex == newIndex)
                return false;

            var frames = trafficLightFrameData[oldIndex].TrafficLightFrames.ToList();

            foreach (var item in frames)
            {
                ChangeFrameIndex(item, oldIndex, newIndex);
            }

            return true;
        }

        public bool ChangeFrameIndex(TrafficLightFrameBase sourceFrame, int oldIndex, int newIndex)
        {
            if (!trafficLightFrameData.ContainsKey(oldIndex) || !trafficLightFrameData[oldIndex].TrafficLightFrames.Contains(sourceFrame))
                return false;

            if (oldIndex == newIndex)
                return false;

            trafficLightFrameData[oldIndex].TrafficLightFrames.TryToRemove(sourceFrame);

            if (trafficLightFrameData[oldIndex].TrafficLightFrames.Count == 0)
            {
                trafficLightFrameData.Remove(oldIndex);
            }

            if (!trafficLightFrameData.ContainsKey(newIndex))
            {
                trafficLightFrameData.Add(newIndex, new TrafficLightFrameData() { TrafficLightFrames = new List<TrafficLightFrameBase>() });
            }

            trafficLightFrameData[newIndex].TrafficLightFrames.TryToAdd(sourceFrame);

            EditorSaver.SetObjectDirty(this);

            return true;
        }

        public void AddFrame(int index, TrafficLightFrameBase frame)
        {
            if (trafficLightFrameData == null)
            {
                trafficLightFrameData = new TrafficLightFrameDataDictionary();
            }

            if (!trafficLightFrameData.ContainsKey(index))
            {
                trafficLightFrameData.Add(index, new TrafficLightFrameData()
                {
                    TrafficLightFrames = new List<TrafficLightFrameBase>()
                    {
                        frame
                    }
                });
            }
            else
            {
                trafficLightFrameData[index].TrafficLightFrames.TryToAdd(frame);
            }
        }

        public bool TryToRemoveFrame(int index, TrafficLightFrameBase frame)
        {
            if (trafficLightFrameData != null && trafficLightFrameData.ContainsKey(index))
            {
                return trafficLightFrameData[index].TrafficLightFrames.TryToRemove(frame);
            }

            return false;
        }

        public int GetCurrentIndexFrame(TrafficLightFrameBase trafficLightFrame)
        {
            foreach (var frameData in trafficLightFrameData)
            {
                if (frameData.Value.TrafficLightFrames.Contains(trafficLightFrame))
                {
                    return frameData.Key;
                }
            }

            return -1;
        }

        public bool HasLightIndex(int index)
        {
            return trafficLightFrameData.ContainsKey(index);
        }

        public bool FrameHasIndex(TrafficLightFrameBase trafficLightFrame, int index)
        {
            var currentIndex = GetCurrentIndexFrame(trafficLightFrame);
            return currentIndex == index;
        }

        public ICollection<int> GetLightIndexes()
        {
            return trafficLightFrameData.Keys;
        }

        public List<TrafficLightFrameBase> GetLightFrames(int index)
        {
            if (trafficLightFrameData.ContainsKey(index))
            {
                return trafficLightFrameData[index].TrafficLightFrames;
            }

            return null;
        }

        public void SetupInitialIndexes()
        {
            foreach (var frameData in TrafficLightFrames)
            {
                var frames = frameData.Value.TrafficLightFrames;

                foreach (var frame in frames)
                {
                    frame.InitialLightIndex = frameData.Key;
                    EditorSaver.SetObjectDirty(frame);
                }
            }
        }

        public void AssignCrossRoad(TrafficLightCrossroad trafficLightCrossroad, bool reparent = false)
        {
            bool changed = false;

            if (this.trafficLightCrossroad != trafficLightCrossroad)
            {
                this.trafficLightCrossroad = trafficLightCrossroad;
                changed = true;
            }

            if (trafficLightCrossroad)
            {
                RebindCrossroad();

                if (reparent && trafficLightCrossroad.TrafficLightParent)
                {
                    transform.SetParent(trafficLightCrossroad.TrafficLightParent);
                }
            }
            else
            {
                if (connectedId != 0)
                {
                    connectedId = 0;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        /// <summary>
        /// Links current traffic light object & related object to make it traffic light sync between them.
        /// </summary>
        public void AssignRoadObject(TrafficNode trafficNode, bool reparent = false)
        {
            if (trafficNode == null)
                throw new ArgumentException("TrafficNode is null");

            if (trafficNode.TrafficLightCrossroad == null)
                throw new ArgumentException("TrafficLightCrossroad is null");

            var crossroad = trafficNode.TrafficLightCrossroad;

            AssignCrossRoad(crossroad, reparent);

            var handler = trafficNode.TrafficLightHandler;
            SetFrameIndex(handler.RelatedLightIndex);
        }

        /// <summary>
        /// Links current traffic light object & related object to make it traffic light sync between them.
        /// </summary>
        public void AssignRoadObject(PedestrianNode pedestrianNode, bool reparent = false)
        {
            if (pedestrianNode == null)
                throw new ArgumentException("PedestrianNode is null");

            if (pedestrianNode.RelatedTrafficLightHandler == null)
                throw new ArgumentException("RelatedTrafficLightHandler is null");

            var crossroad = pedestrianNode.RelatedTrafficLightHandler.TrafficLightCrossroad;

            if (crossroad == null)
                throw new ArgumentException("RelatedTrafficLightHandler contains TrafficLightCrossroad null");

            AssignCrossRoad(crossroad, reparent);

            var handler = pedestrianNode.RelatedTrafficLightHandler;
            SetFrameIndex(handler.RelatedLightIndex);
        }

        public bool RebindCrossroad()
        {
            if (!trafficLightCrossroad)
                return false;

            var newConnectedId = trafficLightCrossroad.UniqueId;

            if (connectedId != newConnectedId)
            {
                connectedId = newConnectedId;
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            return false;
        }

        public void AssignInitialChilds()
        {
            SetIndexOffset(0);
        }
    }

    #endregion
}