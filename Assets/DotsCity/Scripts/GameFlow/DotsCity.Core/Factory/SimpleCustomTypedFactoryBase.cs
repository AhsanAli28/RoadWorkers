using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class SimpleCustomTypedFactoryBase : MonoBehaviour
    {
        [Serializable]
        protected class FactoryDataDictionary : AbstractSerializableDictionary<string, GameObject> { }

        [SerializeField] private Transform poolParent;
        [SerializeField][Range(1, 1000)] private int poolSize;
        [SerializeField] private bool autoGeneratePool = true;
        [SerializeField] private FactoryDataDictionary prefabData;

        private Dictionary<string, ObjectPool> pool = new Dictionary<string, ObjectPool>();
        private List<string> poolNames = new List<string>();

        public ICollection<string> Options => prefabData.Keys;
        public string FactoryName => name;

        protected FactoryDataDictionary Prefabs { get => prefabData; }

        protected virtual void Awake()
        {
            if (autoGeneratePool)
            {
                GeneratePool();
            }
        }

        public virtual void GeneratePool()
        {
            foreach (var item in prefabData)
            {
                if (item.Value == null)
                {
                    UnityEngine.Debug.LogError($"{name} Entry is null {item.Key}");
                    continue;
                }

                var objectPool = PoolManager.Instance.PoolForObject(item.Value.gameObject);
                objectPool.preInstantiateCount = poolSize;
                objectPool.Parent = poolParent != null ? poolParent : transform;

                pool.Add(item.Key, objectPool);
                poolNames.Add(item.Key);
            }
        }

        public virtual GameObject Get(string key)
        {
            if (!string.IsNullOrEmpty(key) && pool.TryGetValue(key, out var value))
            {
                return value.Pop();
            }

            return null;
        }

        public virtual GameObject Get(int index)
        {
            if (index >= 0 && poolNames.Count > index)
            {
                return Get(poolNames[index]);
            }

            return null;
        }

        public string GetName(int index)
        {
            if (index >= 0 && Options.Count > index)
            {
                return Options.ToArray()[index];
            }

            return "NaN";
        }
    }
}