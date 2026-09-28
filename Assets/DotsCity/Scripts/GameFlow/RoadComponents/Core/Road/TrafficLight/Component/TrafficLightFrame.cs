using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class TrafficLightFrame : TrafficLightFrameBase
    {
        [SerializeField] private GameObject redLight;
        [SerializeField] private GameObject yellowLight;
        [SerializeField] private GameObject greenLight;

        public GameObject RedLight { get => redLight; set => redLight = value; }
        public GameObject YellowLight { get => yellowLight; set => yellowLight = value; }
        public GameObject GreenLight { get => greenLight; set => greenLight = value; }

        protected override bool HasYellowLight => yellowLight != null;

        protected override void SwitchLight(LightColor lightColor, bool isOn)
        {
            var lightObject = GetLightObject(lightColor);

            if (lightObject != null && lightObject.activeSelf != isOn)
            {
                lightObject.SetActive(isOn);
            }
        }

        private GameObject GetLightObject(LightColor lightColor)
        {
            switch (lightColor)
            {
                case LightColor.Red:
                    return redLight;
                case LightColor.Yellow:
                    return yellowLight;
                case LightColor.Green:
                    return greenLight;
            }

            return null;
        }
    }
}