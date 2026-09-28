using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.PackageManagerExtension
{
    [Serializable]
    public class ChildObjectData
    {
        public GameObject Prefab;
        public List<LocalChildObject> Childs = new List<LocalChildObject>();
    }
}
