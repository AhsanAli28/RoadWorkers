using UnityEngine;

namespace Spirit604.Gameplay.Road.Debug
{
    public static class TrafficLightSceneColor
    {
        public static Color StateToColor(LightState state)
        {
            switch (state)
            {
                case LightState.Red:
                    {
                        return Color.red;
                    }
                case LightState.RedYellow:
                    {
                        return new Color32(255, 140, 0, 255); // orange
                    }
                case LightState.Yellow:
                    {
                        return Color.yellow;
                    }
                case LightState.Green:
                    {
                        return Color.green;
                    }
            }

            return default;
        }
    }
}