using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class RuntimeConfigAwaiter : RuntimeEntityConfigBase
    {
        protected bool RecreateOnStart { get; private set; }

        public virtual void Initialize(bool recreateOnStart)
        {
            RecreateOnStart = recreateOnStart;
        }
    }

    public abstract class RuntimeConfigAwaiter<T> : RuntimeConfigAwaiter where T : unmanaged, IComponentData
    {
        private const float MaxAwaitTime = 4f;

        private WaitForEndOfFrame awaiter = new WaitForEndOfFrame();
        private float finishSearchTime;
        private EntityQuery query;
        private bool hasQuery;

        private bool CanRecreate
        {
            get
            {
                bool canRecreate = false;

                try
                {
                    // Internal _QueryData can be null
                    if (query != null && !query.IsEmpty)
                    {
                        canRecreate = true;
                    }
                }
                catch { }

                return canRecreate;
            }
        }

        public override void Initialize(bool recreateOnStart)
        {
            base.Initialize(recreateOnStart);

            if (!hasQuery)
            {
                hasQuery = true;
                query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
            }
            else
            {
                UnityEngine.Debug.Log("halt!!!");
            }

            if (CanRecreate)
            {
                TryToRecreateConfig();
            }
            else
            {
                StartCoroutine(WaitForEntityLoad());
            }
        }

        private IEnumerator WaitForEntityLoad()
        {
            finishSearchTime = Time.time + MaxAwaitTime;

            while (true)
            {
                if (CanRecreate)
                {
                    TryToRecreateConfig();
                    break;
                }
                else
                {
                    yield return awaiter;
                }

                if (Time.time >= finishSearchTime)
                {
                    if (!IgnoreExist)
                    {
                        Debug.Log($"Config {name}, {typeof(T).Name} not found");
                    }

                    break;
                }
            }
        }

        private void TryToRecreateConfig()
        {
            var entity = query.GetSingletonEntity();
            SetEntity(entity);
            TryToCreateNewConfig();
        }

        private void TryToCreateNewConfig()
        {
            if (RecreateOnStart)
            {
                UpdateConfig();
            }
        }
    }
}