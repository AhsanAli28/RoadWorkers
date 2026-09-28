using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianRagdollFactory : MonoBehaviour, IPedestrianRagdollFactory
    {
        #region Variables

        private Dictionary<int, ObjectPool> pools = new Dictionary<int, ObjectPool>();

        private int ragdollPoolSize;

        #endregion

        #region Constructor

        [InjectWrapper]
        public void Construct(PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder, IPedestrianRagdollPrefabProvider prefabProvider)
        {
            InitPool(pedestrianSpawnerConfigHolder, prefabProvider);
        }

        #endregion

        #region Public methods      

        public PedestrianRagdoll SpawnRagdoll(int skinIndex)
        {
            if (ragdollPoolSize > 0 && pools.TryGetValue(skinIndex, out var pedestrianPool))
            {
                PedestrianRagdoll ragdoll = pedestrianPool.Pop().GetComponent<PedestrianRagdoll>();

                return ragdoll;
            }
            else
            {
                UnityEngine.Debug.LogError($"Ragdoll index '{skinIndex}' not found");
            }

            return null;
        }

        #endregion

        #region Private methods

        private void InitPool(PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder, IPedestrianRagdollPrefabProvider prefabProvider)
        {
            var pedestrianSettingsConfig = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig;

            ragdollPoolSize = pedestrianSpawnerConfigHolder.PedestrianSpawnerConfig.RagdollPoolSize;

            var prefabs = prefabProvider.GetPrefabs();

            if (pedestrianSettingsConfig.HasRagdoll &&
                pedestrianSettingsConfig.RagdollType == RagdollType.Default &&
                ragdollPoolSize > 0 &&
                prefabs != null)
            {
                int index = 0;

                foreach (var ragdollPrefab in prefabs)
                {
                    if (ragdollPrefab)
                    {
                        ObjectPool pedestrianPool = PoolManager.Instance.PoolForObject(ragdollPrefab.gameObject);
                        pedestrianPool.preInstantiateCount = ragdollPoolSize;
                        pools.Add(index, pedestrianPool);
                    }

                    index++;
                }
            }
        }

        #endregion
    }
}