using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level
{
    public class MirrorPhysicsObject : MonoBehaviour
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
