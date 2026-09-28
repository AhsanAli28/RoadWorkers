using Spirit604.Gameplay.Config.Common;
using UnityEngine;

namespace Spirit604.Gameplay.Common
{
    public class PersistDataHolder : MonoBehaviour
    {
        [SerializeField] private PersistData persistData;

        public PersistData PersistData { get => persistData; }
    }
}