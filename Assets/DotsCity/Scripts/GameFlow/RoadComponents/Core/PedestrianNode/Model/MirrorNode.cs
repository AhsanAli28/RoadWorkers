using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class MirrorNode : MonoBehaviour
    {
        [SerializeField] private int sourceNodeHash;

        public int SourceNodeHash
        {
            get => sourceNodeHash;
            set
            {
                sourceNodeHash = value;
                EditorSaver.SetObjectDirty(this);
            }
        }
    }
}
