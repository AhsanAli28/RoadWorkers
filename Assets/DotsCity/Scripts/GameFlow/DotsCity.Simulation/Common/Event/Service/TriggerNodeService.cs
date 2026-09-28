using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Events.Authoring;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Events
{
    [DefaultExecutionOrder(-10000)]
    public class TriggerNodeService : SingletonMonoBehaviour<TriggerNodeService>
    {
        [SerializeField] private TriggerNodeConfigAuthoring triggerNodeConfig;

        private Dictionary<Entity, List<ITriggerNodeListener>> listeners = new Dictionary<Entity, List<ITriggerNodeListener>>();

        private bool isActivated;

        public bool IsActivated
        {
            get
            {
                if (triggerNodeConfig)
                    return triggerNodeConfig.IsActivated;

#if UNITY_EDITOR
                triggerNodeConfig = ObjectUtils.FindObjectOfType<TriggerNodeConfigAuthoring>();

                if (triggerNodeConfig && triggerNodeConfig.IsActivated)
                {
                    Debug.Log("TriggerNodeService. TriggerNodeConfigAuthoring is activated but not assigned in <b>TriggerNodeService</b>");
                    return true;
                }
#endif

                return false;
            }
        }

        /// <summary>
        /// Traffic or Npc Entity, Node Entity, TriggerNodeType
        /// </summary>
        public event Action<Entity, Entity, TriggerNodeType> OnTriggerEnter = delegate { };

        private void Start()
        {
            if (IsActivated)
                Initialize();
        }

        public void AddListener(Entity nodeEntity, ITriggerNodeListener listener)
        {
            if (!isActivated)
                Debug.Log("TriggerNodeService. Trying to add listener but <b>TriggerNodeConfig</b> is not activated");

            if (!listeners.ContainsKey(nodeEntity))
            {
                listeners.Add(nodeEntity, new List<ITriggerNodeListener>());
            }

            listeners[nodeEntity].Add(listener);
        }

        public void RemoveListener(Entity nodeEntity, ITriggerNodeListener listener)
        {
            if (listeners.ContainsKey(nodeEntity))
            {
                var list = listeners[nodeEntity];
                list.Remove(listener);

                if (list.Count == 0)
                {
                    listeners.Remove(nodeEntity);
                }
            }
        }

        internal void RaiseEvent(Entity entity, Entity nodeEntity, TriggerNodeType triggerNodeType)
        {
            OnTriggerEnter(entity, nodeEntity, triggerNodeType);

            if (listeners.TryGetValue(nodeEntity, out var list))
            {
                for (var i = 0; i < list.Count; i++)
                {
                    list[i].RaiseEvent(entity, nodeEntity, triggerNodeType);
                }
            }
        }

        private void Initialize()
        {
            isActivated = true;
            DefaultWorldUtils.CreateAndAddSystemManaged<TriggerNodeSystem, MainThreadEventPlaybackGroup>().Initialize(this);
        }
    }
}
