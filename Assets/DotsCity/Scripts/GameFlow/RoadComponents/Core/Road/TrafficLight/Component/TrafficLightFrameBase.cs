using Spirit604.Attributes;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public abstract class TrafficLightFrameBase : MonoBehaviour, ITrafficLightListener
    {
        protected enum LightColor { Red, Yellow, Green }

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficLight.html#light-frame")]
        [SerializeField] private string link;

        [SerializeField] private TrafficLightObject trafficLightObject;
        [SerializeField][Range(0, 9)] private int initialLightIndex;
        [SerializeField] private Vector3 indexDirection;

        public TrafficLightObject TrafficLightObject { get => trafficLightObject; set => trafficLightObject = value; }

        public int InitialLightIndex { get => initialLightIndex; set => initialLightIndex = value; }

        public Vector3 IndexDirection { get => indexDirection; set => indexDirection = value; }

        public LightState CurrentLightState { get; protected set; } = LightState.Uninitialized;

        protected abstract bool HasYellowLight { get; }

        public void UpdateState(LightState state)
        {
            SetLightState(state, CurrentLightState);
            CurrentLightState = state;
        }

        public void AssignCrossRoad(TrafficLightCrossroad trafficLightCrossroad, bool reparent = false)
        {
            trafficLightObject?.AssignCrossRoad(trafficLightCrossroad, reparent);
        }

        public Vector3 GetIndexPosition()
        {
            const float offsetLength = 1f;
            Vector3 yOffset = new Vector3(0, 2f);
            var position = transform.position + transform.rotation * indexDirection.normalized * offsetLength + yOffset;

            return position;
        }

        protected abstract void SwitchLight(LightColor lightColor, bool isOn);

        private void SetLightState(LightState newLightState, LightState previousLightState)
        {
            switch (previousLightState)
            {
                case LightState.Uninitialized:
                    SwitchLight(LightColor.Red, false);
                    SwitchLight(LightColor.Yellow, false);
                    SwitchLight(LightColor.Green, false);
                    break;
                case LightState.RedYellow:
                    {
                        SwitchLight(LightColor.Yellow, false);
                        SwitchLight(LightColor.Red, false);
                        break;
                    }
                case LightState.Green:
                    {
                        SwitchLight(LightColor.Green, false);
                        break;
                    }
                case LightState.Yellow:
                    {
                        SwitchLight(LightColor.Yellow, false);
                        break;
                    }
                case LightState.Red:
                    {
                        SwitchLight(LightColor.Red, false);
                        break;
                    }
            }

            if (HasYellowLight)
            {
                switch (newLightState)
                {
                    case LightState.RedYellow:
                        {
                            SwitchLight(LightColor.Yellow, true);
                            SwitchLight(LightColor.Red, true);
                            break;
                        }
                    case LightState.Green:
                        {
                            SwitchLight(LightColor.Green, true);
                            break;
                        }
                    case LightState.Yellow:
                        {
                            SwitchLight(LightColor.Yellow, true);
                            break;
                        }
                    case LightState.Red:
                        {
                            SwitchLight(LightColor.Red, true);
                            break;
                        }
                }
            }
            else
            {
                if (newLightState == LightState.Green)
                {
                    SwitchLight(LightColor.Green, true);
                }
                else
                {
                    SwitchLight(LightColor.Red, true);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (UnityEditor.Selection.activeGameObject != transform.gameObject)
                return;

            var pos1 = transform.position;
            var pos2 = GetIndexPosition();

            pos1.y = pos2.y;
            Gizmos.DrawLine(pos1, pos2);
        }
#endif
    }
}