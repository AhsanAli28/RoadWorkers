using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Root.Authoring
{
    public class ConfigRoot : MonoBehaviour, IVersionProvider
    {
        [HideInInspector]
        [SerializeField] private string version;

        public string Version => version;

        [Button]
        public void SyncConfigs()
        {
            var configs = GetComponentsInChildren<ISyncableConfig>();

            for (int i = 0; i < configs?.Length; i++)
            {
                configs[i].SyncConfig();
            }
        }
    }
}
