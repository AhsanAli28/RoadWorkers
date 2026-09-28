#if UNITY_EDITOR
using UnityEngine;

namespace Spirit604.AnimationBaker.EditorInternal
{
    internal class TempSkinData
    {
        public Animator Animator;
        public GameObject TempParent;
        public Mesh Mesh;
        public SkinnedMeshRenderer Skin;
        public MeshFilter[] Attachments;
        public bool NewMeshFlag;
        public int DataIndex;

        public string Name => TempParent != null ? TempParent.name : HasSkin ? Skin.name : Mesh.name;

        public bool HasSkin => Skin != null;
    }
}
#endif