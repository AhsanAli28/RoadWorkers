using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.Extensions
{

    [DefaultExecutionOrder(-10000)]
    public class PoolManager : SingletonMonoBehaviour<PoolManager>
    {
        new static public PoolManager Instance
        {
            get
            {
                PoolManager pm = PoolManager.InstanceIfExist;
                if (pm == null)
                {
                    GameObject g = new GameObject("PoolManager");
                    pm = g.AddComponent<PoolManager>();
                }
                return pm;
            }
        }

        public List<ObjectPool> pools = new List<ObjectPool>();
        public Dictionary<int, ObjectPool> poolMap = new Dictionary<int, ObjectPool>();

        Transform poolObjectsRoot;

        public Transform PoolObjectsRoot
        {
            get
            {
                if (poolObjectsRoot == null)
                {
                    GameObject root = new GameObject("PoolObjectsRoot");
                    poolObjectsRoot = root.transform;
                }

                return poolObjectsRoot;
            }
        }


        protected override void Awake()
        {
            base.Awake();
            gameObject.GetComponentsInChildren<ObjectPool>(pools);

#if UNITY_EDITOR
            pools.Sort
            (
                (a, b) => string.CompareOrdinal(a.name, b.name)
            );
#endif

            foreach (ObjectPool pool in pools)
            {
                if (pool.prefab == null)
                {
                    CustomDebug.LogError("Missing prefab in pool : " + pool.name, pool);
                }
                else
                {
                    if (!poolMap.ContainsKey(pool.prefab.GetInstanceID()))
                    {
                        poolMap.Add(pool.prefab.GetInstanceID(), pool);
                    }
                    else
                    {
                        CustomDebug.LogError("Duplicate : " + pool.prefab.name, pool);
                    }
                }
            }
        }


        public ObjectPool FindPool(Object prefab)
        {
            ObjectPool pool = null;
            if (!poolMap.TryGetValue(prefab.GetInstanceID(), out pool))
            {
                //			CustomDebug.LogError("Cant find pool for prefab : " + prefab.name);
            }

            return pool;
        }


        public ObjectPool PoolForObject(GameObject prefab)
        {
            var pool = FindPool(prefab);
            if (pool == null)
            {
                var poolObject = new GameObject();
                poolObject.name = prefab.name + "Pool";
                poolObject.transform.position = Vector3.zero;
                poolObject.transform.parent = transform;
                pool = poolObject.AddComponent<ObjectPool>();
                pool.prefab = prefab;
                pool.autoExtend = true;

                pools.Add(pool);
                poolMap.Add(pool.prefab.GetInstanceID(), pool);
            }
            return pool;
        }


        public void RemoveObjectPool(ObjectPool pool)
        {
            if (pool != null)
            {
                pools.Remove(pool);
                poolMap.Remove(pool.prefab.GetInstanceID());
            }
        }
    }
}