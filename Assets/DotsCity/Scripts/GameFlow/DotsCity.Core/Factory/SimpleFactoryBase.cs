using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class SimpleFactoryBase<T> : MonoBehaviour where T : Behaviour
    {
        [SerializeField] private Transform parent;
        [SerializeField] private T prefab;
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
            objectPool = PoolManager.Instance.PoolForObject(prefab.gameObject);
            objectPool.preInstantiateCount = poolSize;
            objectPool.Parent = parent != null ? parent : transform;
        }

        public T Get() => objectPool.Pop().GetComponent<T>();
    }
}