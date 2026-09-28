#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.AnimationBaker.EditorInternal
{
    internal class TempTextureData
    {
        public List<AnimationTextureData> Data = new List<AnimationTextureData>();
        public List<int> SkinIndices = new List<int>();
        public Texture2D Texture;
        public Texture2D NormalTexture;
        public int DataWidth;
        public int DataHeight;
        public int AtlasWidth;
        public int AtlasHeight;
        public int VertexCount;
        public int AnimationDataIndex;
        public Vector2Int BakingFrameOffset;

        public Texture2D GetTexture(int index)
        {
            switch (index)
            {
                case 0:
                    {
                        return Texture;
                    }
                case 1:
                    {
                        return NormalTexture;
                    }
            }

            return null;
        }

        public void SetTexture(Texture2D newTexture, int index)
        {
            switch (index)
            {
                case 0:
                    {
                        Texture = newTexture;
                        break;
                    }
                case 1:
                    {
                        NormalTexture = newTexture;
                        break;
                    }
            }
        }

        public AnimationTextureData GetTextureData(int skinIndex, int clipCount, int clipIndex)
        {
            var localSkinIndex = this.SkinIndices.IndexOf(skinIndex);
            return Data[clipCount * localSkinIndex + clipIndex];
        }

        public void Merge(TempTextureData tempData)
        {
            this.VertexCount += tempData.VertexCount;
            this.Data.AddRange(tempData.Data);
            this.SkinIndices.AddRange(tempData.SkinIndices);
        }
    }
}
#endif