using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class SimpleTypedFactoryBase<T, TMonoObject> : MonoBehaviour where TMonoObject : Component
    {
        [Serializable]
        public class FactoryDataDictionary : AbstractSerializableDictionary<T, TMonoObject> { }

        [SerializeField] private Transform poolParent;
        [SerializeField][Range(0, 1000)] private int poolSize;
        [SerializeField] private bool autoPopulate = true;
        [SerializeField] private FactoryDataDictionary prefabData;

        private Dictionary<T, ObjectPool> pool = new Dictionary<T, ObjectPool>();
        private bool generated;

        public FactoryDataDictionary Prefabs => prefabData;

        protected virtual void Awake()
        {
            if (autoPopulate)
                GeneratePool();
        }

        public virtual void GeneratePool()
        {
            foreach (var item in prefabData)
            {
                if (item.Value == null)
                {
                    Debug.LogError($"{name} Entry is null {item.Key}");
                    continue;
                }

                var objectPool = PoolManager.Instance.PoolForObject(item.Value.gameObject);
                objectPool.preInstantiateCount = poolSize;
                objectPool.Parent = poolParent != null ? poolParent : transform;

                pool.Add(item.Key, objectPool);
            }

            generated = true;
        }

        public virtual TMonoObject Get(T key)
        {
            if (!generated)
            {
                GeneratePool();
            }

            if (pool.TryGetValue(key, out var value))
            {
                return value.Pop().GetComponent<TMonoObject>();
            }

            return null;
        }
    }
}