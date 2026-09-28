using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public static class TrafficLightTimingUtils
    {
        public static float GetTotalDuration(List<LightStateInfo> lightStates)
        {
            return lightStates.Select(item => item.Duration).Sum();
        }

        public static List<LightStateInfo> GetLightStatesWithTimeOffset(List<LightStateInfo> lightStates, float timeOffset)
        {
            if (lightStates == null)
            {
                return null;
            }

            List<LightStateInfo> newStates = new List<LightStateInfo>();
            float stateCutTime = 0;
            LightState cutState = default;

            int stateStartIndex = 0;

            float currentDuration = 0;

            for (int i = 0; i < lightStates.Count; i++)
            {
                currentDuration += lightStates[i].Duration;

                if (currentDuration >= timeOffset)
                {
                    stateStartIndex = i;
                    stateCutTime = lightStates[i].Duration - (currentDuration - timeOffset);
                    cutState = lightStates[i].LightState;

                    break;
                }
            }

            for (int i = stateStartIndex; i < lightStates.Count; i++)
            {
                float duration = lightStates[i].Duration;

                if (i == stateStartIndex)
                {
                    duration -= stateCutTime;
                }

                var lightState = lightStates[i].LightState;

                if (duration > 0)
                {
                    newStates.Add(new LightStateInfo()
                    {
                        Duration = duration,
                        LightState = lightState
                    });
                }
            }

            for (int i = 0; i < stateStartIndex; i++)
            {
                var duration = lightStates[i].Duration;
                var lightState = lightStates[i].LightState;

                bool shouldAddNewState = true;

                if (i == 0)
                {
                    var lastState = newStates.Last();

                    if (lastState.LightState == lightState)
                    {
                        shouldAddNewState = false;
                        lastState.Duration += lightStates[i].Duration;
                        newStates[newStates.Count - 1] = lastState;
                    }
                }

                if (shouldAddNewState)
                {
                    newStates.Add(new LightStateInfo()
                    {
                        Duration = duration,
                        LightState = lightState
                    });
                }
            }

            if (stateCutTime > 0)
            {
                newStates.Add(new LightStateInfo()
                {
                    Duration = stateCutTime,
                    LightState = cutState
                });
            }

            return newStates;
        }

        public static List<LightStateInfo> GetArrowLightStates(CustomArrowLightSettings settings)
        {
            List<LightStateInfo> states = new List<LightStateInfo>();

            float redStartTime, greenStartTime, redEndTime, greenEndTime;
            GetArrowTimings(settings, out redStartTime, out greenStartTime, out redEndTime, out greenEndTime);

            if (redStartTime > 0)
            {
                states.Add(new LightStateInfo()
                {
                    LightState = LightState.Red,
                    Duration = redStartTime
                });
            }

            if (greenStartTime > 0)
            {
                states.Add(new LightStateInfo()
                {
                    LightState = LightState.Green,
                    Duration = greenStartTime
                });
            }

            if (redEndTime > 0)
            {
                states.Add(new LightStateInfo()
                {
                    LightState = LightState.Red,
                    Duration = redEndTime
                });
            }

            if (greenEndTime > 0)
            {
                states.Add(new LightStateInfo()
                {
                    LightState = LightState.Green,
                    Duration = greenEndTime
                });
            }

            return states;
        }

        public static void GetArrowTimings(CustomArrowLightSettings settings, out float redStartTime, out float greenStartTime, out float redEndTime, out float greenEndTime)
        {
            redStartTime = 0;
            LightState targetState = LightState.Green;

            for (int i = 0; i < settings.relatedTrafficLightHandler.LightStates?.Count; i++)
            {
                if (settings.relatedTrafficLightHandler.LightStates[i].LightState == targetState)
                {
                    break;
                }

                redStartTime += settings.relatedTrafficLightHandler.LightStates[i].Duration;
            }

            greenEndTime = 0;
            greenStartTime = 0;
            redEndTime = 0;
            float totalTime = settings.relatedTrafficLightHandler.GetTotalLightCycleTime();
            float startTimeOffset = settings.startTimeOffset;

            var startTimeOffsetAbs = Mathf.Abs(startTimeOffset);
            int sign = startTimeOffset < 0 ? -1 : 1;

            if (startTimeOffsetAbs > totalTime)
            {
                int count = Mathf.FloorToInt(startTimeOffsetAbs / totalTime);

                startTimeOffsetAbs -= count * totalTime;
                startTimeOffset = startTimeOffsetAbs * sign;
            }

            if (startTimeOffset < 0 && redStartTime < startTimeOffsetAbs)
            {
                greenEndTime = startTimeOffsetAbs - redStartTime;

                if (greenEndTime > settings.enabledDuration)
                {
                    greenStartTime = settings.enabledDuration;
                    redEndTime = greenEndTime - settings.enabledDuration;
                    redStartTime = totalTime - redEndTime - greenStartTime;
                    greenEndTime = 0;
                }
                else
                {
                    redStartTime = 0;
                    greenStartTime = settings.enabledDuration - greenEndTime;
                    redEndTime = totalTime - redStartTime - greenStartTime - greenEndTime;
                }
            }
            else
            {
                redStartTime += startTimeOffset;
                greenStartTime = settings.enabledDuration;
                redEndTime = totalTime - redStartTime - greenStartTime;

                if (startTimeOffset > 0)
                {
                    if (redStartTime + settings.enabledDuration > totalTime)
                    {
                        greenEndTime = settings.enabledDuration - (redStartTime + settings.enabledDuration - totalTime);

                        if (greenEndTime < 0)
                        {
                            redStartTime = Mathf.Abs(greenEndTime);
                            greenEndTime = 0;
                        }
                        else
                        {
                            redStartTime = 0;
                        }

                        greenStartTime = (settings.enabledDuration - greenEndTime);
                        redEndTime = totalTime - greenEndTime - greenStartTime - redStartTime;
                    }
                }
            }

            if (redStartTime + greenStartTime > totalTime)
            {
                greenStartTime = (redStartTime + greenStartTime) - totalTime;
                redStartTime = 0;
                greenEndTime = settings.enabledDuration - greenStartTime;
                redEndTime = totalTime - greenStartTime - greenEndTime;
            }

            if (settings.enabledDuration > totalTime)
            {
                redStartTime = 0;
                greenStartTime = totalTime;
                greenEndTime = 0;
                redEndTime = 0;
            }
        }
    }
}
