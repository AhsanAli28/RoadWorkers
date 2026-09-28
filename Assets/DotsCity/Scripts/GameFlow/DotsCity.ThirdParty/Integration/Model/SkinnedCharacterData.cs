using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.Integration
{
    [Serializable]
    public class SkinnedCharacterData
    {
        public GameObject Prefab;
        public string MainAssetName;
        public string AvatarMainAsset;
        public string AvatarExtension;
        public string AvatarName;
        public MeshList MeshList;
    }

    [Serializable]
    public class MeshList
    {
        public List<SkinMeshData> Meshes;
    }
}

