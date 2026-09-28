using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.VFX
{
    public class VFXFactory : SingletonMonoBehaviour<VFXFactory>
    {
        [SerializeField][Range(0, 100)] private int poolSize = 15;
        [SerializeField] private VfxPrefabDataDictionary vfxPrefabData;

        private Dictionary<VFXType, ObjectPool> vfxPools = new Dictionary<VFXType, ObjectPool>();

        protected override void Awake()
        {
            base.Awake();

            FillPool();
        }

        public GameObject GetVFX(VFXType vfxType)
        {
            GameObject vfx = vfxPools[vfxType].Pop();

            return vfx;
        }

        private void FillPool()
        {
            foreach (KeyValuePair<VFXType, ParticleSystem> pair in vfxPrefabData)
            {
                ObjectPool pool = PoolManager.Instance.PoolForObject(pair.Value.gameObject);
                pool.preInstantiateCount = poolSize;
                vfxPools.Add(pair.Key, pool);
            }
        }
    }
}