using Spirit604.AnimationBaker.EditorInternal;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    public class AnimationTextureDataContainer : AnimationTextureDataContainerBase
    {
        [SerializeField] private Texture2D texture;

        [SerializeField] private Texture2D normalTexture;

        [SerializeField] private List<AnimationTextureData> textureDatas = new List<AnimationTextureData>();

        [SerializeField] private int atlasWidth;

        [SerializeField] private int atlasHeight;

        public override ContainerType CurrentContainerType => ContainerType.Single;
        public List<AnimationTextureData> TextureDatas { get => textureDatas; set => textureDatas = value; }
        public int AtlasWidth { get => atlasWidth; set => atlasWidth = value; }
        public int AtlasHeight { get => atlasHeight; set => atlasHeight = value; }

        public bool ContainsMesh(Mesh mesh)
        {
            if (mesh == null)
                return false;

            for (int i = 0; i < textureDatas.Count; i++)
            {
                var textureData = textureDatas[i];

                if (textureData != null && textureData.SourceMesh == mesh)
                    return true;
            }

            return false;
        }

        public Texture2D GetTexture(int index)
        {
            switch (index)
            {
                case 0:
                    return texture;
                case 1:
                    return normalTexture;
            }

            return null;
        }

        public void SetTexture(Texture2D newTexture, int index)
        {
            switch (index)
            {
                case 0:
                    {
                        texture = newTexture;
                        break;
                    }
                case 1:
                    {
                        normalTexture = newTexture;
                        break;
                    }
            }
        }

        public void MakeDirty(bool includeTextures = false)
        {
            EditorSaver.SetObjectDirty(this);

            if (includeTextures)
            {
                var animTexture = GetTexture(0);

                if (animTexture)
                {
                    EditorSaver.SetObjectDirty(animTexture);
                }

                var normalTexture = GetTexture(1);

                if (normalTexture)
                {
                    EditorSaver.SetObjectDirty(normalTexture);
                }
            }
        }
    }
}