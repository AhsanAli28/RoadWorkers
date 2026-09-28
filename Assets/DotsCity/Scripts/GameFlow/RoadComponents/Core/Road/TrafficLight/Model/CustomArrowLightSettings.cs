using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    [Serializable]
    public class CustomArrowLightSettings
    {
        [Range(-200, 200)]
        public float startTimeOffset;

        [Range(0, 200)]
        public float enabledDuration = 10f;

        public Path path;
        public TrafficLightHandler currentTrafficLightHandler;
        public TrafficLightHandler relatedTrafficLightHandler;

        public CustomArrowLightSettings(Path path)
        {
            this.path = path;
        }

        public List<LightStateInfo> GetStates()
        {
            List<LightStateInfo> states = new List<LightStateInfo>();

            if (relatedTrafficLightHandler == null)
            {
                return states;
            }

            float redStartTime = 0;
            float greenStartTime = 0;
            float redEndTime = 0;
            float greenEndTime = 0;

#if UNITY_EDITOR
            TrafficLightTimingUtils.GetArrowTimings(this, out redStartTime, out greenStartTime, out redEndTime, out greenEndTime);
#endif

            if (redStartTime > 0)
            {
                states.Add(
                    new LightStateInfo()
                    {
                        Duration = redStartTime,
                        LightState = LightState.Red
                    });
            }
            if (greenStartTime > 0)
            {
                states.Add(
                    new LightStateInfo()
                    {
                        Duration = greenStartTime,
                        LightState = LightState.Green
                    });
            }
            if (redEndTime > 0)
            {
                states.Add(
                    new LightStateInfo()
                    {
                        Duration = redEndTime,
                        LightState = LightState.Red
                    });
            }
            if (greenEndTime > 0)
            {
                states.Add(
                    new LightStateInfo()
                    {
                        Duration = greenEndTime,
                        LightState = LightState.Green
                    });
            }

            return states;
        }
    }
}