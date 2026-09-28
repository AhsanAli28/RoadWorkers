using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class SimpleGameObjectFactoryBase : MonoBehaviour
    {
        [SerializeField] private Transform parent;
        [SerializeField] private GameObject prefab;
        [SerializeField][Range(0, 1000)] private int poolSize;
        [SerializeField] private bool autoPopulate = true;

        private ObjectPool objectPool;

        protected virtual void Awake()
        {
            if (autoPopulate)
                Populate();
        }

        public void Populate()
        {
            if (objectPool) return;

            objectPool = PoolManager.Instance.PoolForObject(prefab.gameObject);
            objectPool.preInstantiateCount = poolSize;
            objectPool.Parent = parent != null ? parent : transform;
        }

        public GameObject Get() => objectPool.Pop();
    }
}