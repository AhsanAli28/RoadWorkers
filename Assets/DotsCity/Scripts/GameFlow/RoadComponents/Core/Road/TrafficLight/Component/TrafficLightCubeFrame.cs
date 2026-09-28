using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class TrafficLightCubeFrame : TrafficLightFrameBase
    {
        [SerializeField] private MeshRenderer lightMesh;
        [SerializeField] private string materialColorName = "_BaseColor";
        protected override bool HasYellowLight => true;

        private MaterialPropertyBlock block;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        protected override void SwitchLight(LightColor lightColor, bool isOn)
        {
            if (isOn)
            {
                var color = GetColor(lightColor);

                block.SetColor(materialColorName, color);

                lightMesh.SetPropertyBlock(block);
            }
        }

        private Color GetColor(LightColor lightColor)
        {
            switch (lightColor)
            {
                case LightColor.Red:
                    return Color.red;
                case LightColor.Yellow:
                    return Color.yellow;
                case LightColor.Green:
                    return Color.green;
            }

            return default;
        }
    }
}