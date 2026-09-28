using Spirit604.DotsCity.Simulation.Traffic;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public class CarLightEntityAdapter : CarLightBase
    {
        [SerializeField][Range(0.01f, 2f)] private float blinkerFrequency = 0.4f;

        [SerializeField] private List<GameObject> leftBlinkers = new List<GameObject>();
        [SerializeField] private List<GameObject> rightBlinkers = new List<GameObject>();
        [SerializeField] private List<GameObject> brakingLights = new List<GameObject>();
        [SerializeField] private List<GameObject> reverseLights = new List<GameObject>();

        private float switchBlinkerTime;
        private bool blinkerEnabled;
        private BlinkerState blinkerState;
        private bool engine = true;
        private bool braking;
        private bool reverse;

        private void Awake()
        {
            SwitchBraking(false);
            SwitchReverse(false);
            UpdateBlinkerState(BlinkerState.Left, false);
            UpdateBlinkerState(BlinkerState.Right, false);
            enabled = false;
        }

        private void Update()
        {
            Tick();
        }

        public override void UpdateState(in VehicleLightComponent vehicleLightComponent)
        {
            if (!vehicleLightComponent.EngineOn)
            {
                if (engine != vehicleLightComponent.EngineOn)
                {
                    engine = vehicleLightComponent.EngineOn;
                    SwitchBraking(false);
                    SwitchReverse(false);
                    SetBlinkerState(BlinkerState.None);
                    reverse = false;
                    braking = false;
                }

                return;
            }
            else
            {
                engine = true;
            }

            SetBlinkerState(vehicleLightComponent.BlinkerState);

            if (reverse != vehicleLightComponent.Reverse)
            {
                reverse = vehicleLightComponent.Reverse;
                SwitchReverse(reverse);
            }

            if (braking != vehicleLightComponent.Braking)
            {
                braking = vehicleLightComponent.Braking;
                SwitchBraking(braking);
            }
        }

        private void Tick()
        {
            TickBlinker();
        }

        private void TickBlinker()
        {
            if (blinkerState == BlinkerState.None) return;

            if (Time.time >= switchBlinkerTime)
            {
                blinkerEnabled = !blinkerEnabled;
                UpdateBlinkerState(blinkerState, blinkerEnabled);
                UpdateBlinkerTime();
            }
        }

        private void SetBlinkerState(BlinkerState newBlinkerState)
        {
            if (blinkerState == newBlinkerState) return;
            UpdateBlinkerState(blinkerState, false);
            blinkerState = newBlinkerState;
            blinkerEnabled = false;
            enabled = blinkerState != BlinkerState.None;
            UpdateBlinkerTime();
        }

        private void UpdateBlinkerTime()
        {
            switchBlinkerTime = Time.time + blinkerFrequency;
        }

        private void UpdateBlinkerState(BlinkerState blinkerState, bool isActive)
        {
            switch (blinkerState)
            {
                case BlinkerState.None:
                    break;
                case BlinkerState.Left:
                    SwitchLeftBlinker(isActive);
                    break;
                case BlinkerState.Right:
                    SwitchRightBlinker(isActive);
                    break;
            }
        }

        private void SwitchLeftBlinker(bool isActive)
        {
            for (int i = 0; i < leftBlinkers.Count; i++)
            {
                leftBlinkers[i].SetActive(isActive);
            }
        }

        private void SwitchRightBlinker(bool isActive)
        {
            for (int i = 0; i < rightBlinkers.Count; i++)
            {
                rightBlinkers[i].SetActive(isActive);
            }
        }

        private void SwitchReverse(bool isActive)
        {
            for (int i = 0; i < reverseLights.Count; i++)
            {
                reverseLights[i].SetActive(isActive);
            }
        }

        private void SwitchBraking(bool isActive)
        {
            for (int i = 0; i < brakingLights.Count; i++)
            {
                brakingLights[i].SetActive(isActive);
            }
        }
    }
}
