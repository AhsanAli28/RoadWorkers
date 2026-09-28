using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [Serializable]
    public class SkinData
    {
        public GameObject Ragdoll;
        public SkinLODData[] LODs;

        public Texture2D TempMainTexture { get; set; }

        public SkinData()
        {
            LODs = new SkinLODData[Constans.LODLevelCount];

            for (int i = 0; i < LODs.Length; i++)
            {
                LODs[i] = new SkinLODData();
            }
        }

        public SkinData(Mesh mesh, int initialAnimationCount = 0, int lodLevel = 0) : this()
        {
            SetMesh(mesh, lodLevel);

            for (int i = 0; i < initialAnimationCount; i++)
            {
                AddAnimationData(new AnimationData());
            }
        }

        public Mesh GetMesh(int lodLevel = 0)
        {
            return LODs[lodLevel].Mesh;
        }

        public Material GetMaterial(int lodLevel = 0)
        {
            return LODs[lodLevel].Material;
        }

        public AnimationData GetAnimationData(string guid, int lodLevel = 0)
        {
            return LODs[lodLevel].Animations.Where(a => a.Guid == guid).FirstOrDefault();
        }

        public AnimationData GetAnimationData(int animationIndex, int lodLevel = 0)
        {
            return LODs[lodLevel].Animations[animationIndex];
        }

        public void SetMesh(Mesh mesh, int lodLevel = 0)
        {
            LODs[lodLevel].Mesh = mesh;
        }

        public bool SetMaterial(Material material, int lodLevel = 0)
        {
            if (LODs[lodLevel].Material != material)
            {
                LODs[lodLevel].Material = material;
                return true;
            }

            return false;
        }

        public void AddAnimationData(AnimationData animationData, int lodLevel = 0)
        {
            LODs[lodLevel].Animations.Add(animationData);
        }

        public bool RemoveAnimationData(string guid, int lodLevel = 0)
        {
            List<AnimationData> animations = GetAnimations(lodLevel);

            for (int i = 0; i < animations.Count; i++)
            {
                if (animations[i].Guid == guid)
                {
                    animations.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public bool RemoveAnimationData(int index, int lodLevel = 0)
        {
            List<AnimationData> animations = GetAnimations(lodLevel);

            animations.RemoveAt(index);

            return true;
        }

        public bool ContainsMesh(Mesh mesh, int lodLevel = -1)
        {
            for (int j = 0; j < LODs.Length; j++)
            {
                if (lodLevel == -1 || lodLevel == j)
                {
                    if (LODs[j].Mesh == mesh)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<AnimationData> GetAnimations(int lodLevel)
        {
            return LODs[lodLevel].Animations;
        }

        public int GetAnimationCount(int lodLevel)
        {
            return LODs[lodLevel].Animations.Count;
        }

        public void SetAnimationData(AnimationData animationData, int animationIndex, int lodLevel = 0)
        {
            LODs[lodLevel].Animations[animationIndex] = animationData;
        }

        public void ClearAnimations(int lodLevel = 0)
        {
            LODs[lodLevel].Animations.Clear();
        }
    }
}