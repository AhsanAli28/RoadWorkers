using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class RuntimeRoadTileView : RuntimeRoadTileViewBase
    {
        private const string ColorName = "_BaseColor";
        private readonly int ColorID = Shader.PropertyToID(ColorName);

        private const byte previewAlpha = 200;

        private MaterialPropertyBlock materialPropertyBlock;
        private bool currentAvailable = true;
        private bool currentIsVisible = true;

        protected override void Awake()
        {
            base.Awake();
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        public override void SwitchAvailableState(bool available)
        {
            if (currentAvailable == available) return;

            currentAvailable = available;

            Color32 color = Color.white;

            if (Preview)
            {
                color.a = previewAlpha;
            }

            if (!available)
            {
                color = Color.red;
                color.a = previewAlpha;
            }

            materialPropertyBlock.SetColor(ColorID, color);

            for (int i = 0; i < renders.Count; i++)
            {
                renders[i].SetPropertyBlock(materialPropertyBlock);
            }
        }

        public override void SwitchVisibleState(bool isVisible)
        {
            if (currentIsVisible == isVisible) return;

            currentIsVisible = isVisible;

            for (int i = 0; i < renders.Count; i++)
            {
                renders[i].enabled = isVisible;
            }
        }
    }
}
