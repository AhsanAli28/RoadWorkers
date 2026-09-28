using Spirit604.Attributes;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public class RuntimeConfigManager : InitializerBase, IRuntimeConfigManager
    {
        [SerializeField] private bool forceRuntimeInit;

        [Tooltip("List configs that are only intended to be created at runtime")]
        [SerializeField] private List<RuntimeEntityConfigBase> configs = new List<RuntimeEntityConfigBase>();

        [SerializeField] private List<RuntimeConfigAwaiter> runtimeConfigUpdaters = new List<RuntimeConfigAwaiter>();

        public bool RecreateOnStart { get => forceRuntimeInit; set => forceRuntimeInit = value; }

        public event Action OnInitialized = delegate { };

        public override void Initialize()
        {
            base.Initialize();

            for (int i = 0; i < configs.Count; i++)
            {
                configs[i].Create();
            }

            for (int i = 0; i < runtimeConfigUpdaters.Count; i++)
            {
                runtimeConfigUpdaters[i].Initialize(RecreateOnStart);
            }

            OnInitialized();
        }

        public void AddConfig(RuntimeEntityConfigBase config)
        {
            if (configs.TryToAdd(config))
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

#if UNITY_EDITOR
        [Button]
        public void FindConfigs()
        {
            configs = ObjectUtils.FindObjectsOfType<RuntimeEntityConfigBase>().Where(a => a.gameObject.scene == gameObject.scene && a is not RuntimeConfigAwaiter && a is not RuntimeEntityConfig).ToList();
            runtimeConfigUpdaters = ObjectUtils.FindObjectsOfType<RuntimeConfigAwaiter>().Where(a => a.gameObject.scene == gameObject.scene).ToList();
            EditorSaver.SetObjectDirty(this);
        }
#endif  
    }
}
