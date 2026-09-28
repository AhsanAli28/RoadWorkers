using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [DisallowMultipleComponent]
    public class HybridEntityRuntimeAuthoring : RuntimeEntityAuthoringBase, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity, IHybridEntityRef
    {
        [Tooltip("If disabled, user should invoke <b>RegisterEntity</b> method manually via code")]
        [SerializeField] private bool autoRegisterEntity = true;

        [Tooltip("If toggled dispose custom logic should be provided by user. Subscribe to '<b>OnDisposeRequested</b>' event in <b>RuntimeHybridEntityService</b>")]
        [SerializeField] private bool customDispose;

        [SerializeField] private List<HybridComponentBase> hybridComponents = new List<HybridComponentBase>();

        private bool registered;

        public bool CustomDispose => customDispose;
        public Entity RelatedEntity => Entity;
        public bool Destroyed { get; set; }

        public ComponentType[] GetComponentSet() => new ComponentType[] { typeof(WorldEntitySharedType), typeof(PooledEventTag), typeof(PoolableTag) };

        public event Action<Entity> OnEntityInitialized = delegate { };

        private void OnEnable()
        {
            if (autoRegisterEntity)
                RegisterEntity();

            Destroyed = false;
        }

        private void OnDisable()
        {
            var instance = RuntimeHybridEntityService.InstanceIfExist;

            if (instance)
                instance.UnregisterEntity(this);

            registered = false;

            if (!Destroyed)
            {
                try
                {
                    DestroyEntity();
                }
                catch { }
            }
            else
            {
                Destroyed = false;
            }
        }

        public void Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentEnabled<PooledEventTag>(entity, false);
            entityManager.SetSharedComponent(entity, new WorldEntitySharedType()
            {
                EntityWorldType = EntityWorldType.HybridRuntimeEntity
            });
        }

        public bool RegisterEntity()
        {
            if (registered)
            {
                Debug.LogError($"HybridEntityRuntimeAuthoring. Entity '{name}' attempt to register entity twice");
                return false;
            }

            registered = true;
            InitEntity();
            RuntimeHybridEntityService.Instance.RegisterEntity(this);
            return true;
        }

        void IHybridEntityRef.DestroyEntity()
        {
            base.DestroyEntity();
        }

        protected override void PostEntityInit(Entity entity)
        {
            OnEntityInitialized(entity);
        }

        public T AddHybridComponent<T>() where T : HybridComponentBase
        {
            var component = ScriptableObject.CreateInstance<T>();
            hybridComponents.Add(component);
            return component;
        }

        protected override List<ComponentType> GetCustomTypeList()
        {
            List<ComponentType> list = new List<ComponentType>();

            for (int i = 0; i < hybridComponents.Count; i++)
            {
                if (hybridComponents[i] is IRuntimeEntityComponentSetProvider)
                {
                    var provider = hybridComponents[i] as IRuntimeEntityComponentSetProvider;

                    var comps = provider.GetComponentSet();

                    for (int j = 0; j < comps?.Length; j++)
                    {
                        list.TryToAdd(comps[j]);
                    }
                }
            }

            return list;
        }

        protected override IRuntimeInitEntity[] GetInitializers()
        {
            List<IRuntimeInitEntity> inits = new List<IRuntimeInitEntity>();

            var originInits = base.GetInitializers();

            if (originInits?.Length > 0)
            {
                inits.AddRange(originInits);
            }

            for (int i = 0; i < hybridComponents.Count; i++)
            {
                if (hybridComponents[i] is IRuntimeInitEntity)
                {
                    inits.Add(hybridComponents[i] as IRuntimeInitEntity);
                }
            }

            return inits.ToArray();
        }
    }
}
