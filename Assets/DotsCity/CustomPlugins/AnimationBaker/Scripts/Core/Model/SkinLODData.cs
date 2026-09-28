using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [Serializable]
    public class SkinLODData
    {
        public Mesh Mesh;
        public Material Material;
        public List<AnimationData> Animations = new List<AnimationData>();
    }
}