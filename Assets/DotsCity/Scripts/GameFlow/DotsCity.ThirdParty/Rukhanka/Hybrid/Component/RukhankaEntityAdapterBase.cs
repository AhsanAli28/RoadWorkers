using Rukhanka;
using Spirit604.DotsCity.Hybrid.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RukhankaEngine
{
    public abstract class RukhankaEntityAdapterBase : MonoBehaviour
    {
        private class AttachableData
        {
            public GameObject Attachable;
            public Entity Entity;
        }

        private bool eventRegistered;
        private bool controllerEventRegistered;
        private Dictionary<int, AttachableData> attachments = new Dictionary<int, AttachableData>();

        public event Action<AnimationEventComponent> OnAnimationEvent = delegate { };
        public event Action<AnimatorControllerEventComponent> OnAnimatorControllerEvent = delegate { };

        public ICollection<int> AttachmentIndexes => attachments.Keys;

        protected abstract Entity Entity { get; }

        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public int GetInteger(string name)
        {
            var parameter = new FastAnimatorParameter(name);
            return GetAspect().GetIntParameter(parameter);
        }

        public int GetInteger(uint hash)
        {
            var parameter = new FastAnimatorParameter(hash);
            return GetAspect().GetIntParameter(parameter);
        }

        public void SetInteger(string name, int value)
        {
            var parameter = new FastAnimatorParameter(name);
            GetAspect().SetIntParameter(parameter, value);
        }

        public void SetInteger(uint hash, int value)
        {
            var parameter = new FastAnimatorParameter(hash);
            GetAspect().SetIntParameter(parameter, value);
        }

        public float GetFloat(string name)
        {
            var parameter = new FastAnimatorParameter(name);
            return GetAspect().GetFloatParameter(parameter);
        }

        public float GetFloat(uint hash)
        {
            var parameter = new FastAnimatorParameter(hash);
            return GetAspect().GetFloatParameter(parameter);
        }

        public void SetFloat(string name, float value)
        {
            var parameter = new FastAnimatorParameter(name);
            GetAspect().SetFloatParameter(parameter, value);
        }

        public void SetFloat(uint hash, float value)
        {
            var parameter = new FastAnimatorParameter(hash);
            GetAspect().SetFloatParameter(parameter, value);
        }

        public bool GetBool(string name)
        {
            var parameter = new FastAnimatorParameter(name);
            return GetAspect().GetBoolParameter(parameter);
        }

        public bool GetBool(uint hash)
        {
            var parameter = new FastAnimatorParameter(hash);
            return GetAspect().GetBoolParameter(parameter);
        }

        public void SetBool(string name, bool value)
        {
            var parameter = new FastAnimatorParameter(name);
            GetAspect().SetBoolParameter(parameter, value);
        }

        public void SetBool(uint hash, bool value)
        {
            var parameter = new FastAnimatorParameter(hash);
            GetAspect().SetBoolParameter(parameter, value);
        }

        public void SetTrigger(string name)
        {
            var parameter = new FastAnimatorParameter(name);
            GetAspect().SetTrigger(parameter);
        }

        public void SetTrigger(uint hash)
        {
            var parameter = new FastAnimatorParameter(hash);
            GetAspect().SetTrigger(parameter);
        }

        public void ResetTrigger(string name)
        {
            var parameter = new FastAnimatorParameter(name);
            GetAspect().ResetTrigger(parameter);
        }

        public void ResetTrigger(uint hash)
        {
            var parameter = new FastAnimatorParameter(hash);
            GetAspect().ResetTrigger(parameter);
        }

        public bool StateIsPlaying(string stateName, int layerIndex = 0)
        {
            var stateHash = GetHash(stateName);
            return StateIsPlaying(stateHash, layerIndex);
        }

        public bool StateIsPlaying(uint stateHash, int layerIndex = 0)
        {
            return GetLayerCurrentStateInfo(layerIndex).hash == stateHash;
        }

        public AnimatorStateQueryAspect.RuntimeStateInfo GetLayerCurrentStateInfo(int layerIndex)
            => GetStateQueryAspect().GetLayerCurrentStateInfo(layerIndex);

        public bool TransitionIsPlaying(string transitionName, int layerIndex = 0)
        {
            var stateHash = GetHash(transitionName);
            return TransitionIsPlaying(stateHash, layerIndex);
        }

        public bool TransitionIsPlaying(uint transitionHash, int layerIndex = 0)
        {
            return GetLayerCurrentTransitionInfo(layerIndex).hash == transitionHash;
        }

        public bool IsInTransition(int layerIndex)
        {
            return GetStateQueryAspect().IsInTransition(layerIndex);
        }

        public Entity AttachToBone(GameObject attachable, int localAnchorIndex = 0)
        {
            ReleaseAttachment(localAnchorIndex);

            if (!EntityManager.HasBuffer<RukhankaHybridBoneAnchorElement>(Entity))
            {
                Debug.LogError($"NPC {name} doesn't have <b>RukhankaHybridBoneAnchorElement</b> buffer. Make sure that you have added the <b>RukhankaHybridBoneAnchorAuthoring</b> component to the parent of the entity prefab.");
                return Entity.Null;
            }

            var boneDataBuffer = EntityManager.GetBuffer<RukhankaHybridBoneAnchorElement>(Entity, true);
            RukhankaHybridBoneAnchorElement boneData = default;

            bool found = false;

            for (int i = 0; i < boneDataBuffer.Length; i++)
            {
                if (boneDataBuffer[i].LocalBoneIndex == localAnchorIndex)
                {
                    boneData = boneDataBuffer[i];
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new ArgumentException($"LocalAnchorIndex {localAnchorIndex} not found");

            Entity attachEntity = CreateAttachableEntity(attachable, boneData.BoneEntity);

            var attachableData = new AttachableData()
            {
                Attachable = attachable,
                Entity = attachEntity,
            };

            attachments.Add(localAnchorIndex, attachableData);
            return attachEntity;
        }

        public bool ReleaseAttachment(int localAnchorIndex = 0)
        {
            if (!attachments.ContainsKey(localAnchorIndex))
                return false;

            var data = attachments[localAnchorIndex];

            EntityManager.DestroyEntity(data.Entity);

            attachments.Remove(localAnchorIndex);
            return true;
        }

        public bool ReleaseAllAttachments()
        {
            if (attachments.Keys.Count == 0)
                return false;

            if (attachments.Keys.Count == 1)
            {
                var key = attachments.Keys.ElementAt(0);
                ReleaseAttachment(key);
                return true;
            }

            var keys = attachments.Keys.ToList();

            for (int i = 0; i < keys.Count; i++)
            {
                ReleaseAttachment(keys[i]);
            }

            return true;
        }

        public GameObject TryToGetAttachment(int localAnchorIndex = 0)
        {
            if (attachments.TryGetValue(localAnchorIndex, out var attachmentData))
            {
                return attachmentData.Attachable;
            }

            return null;
        }

        public AnimatorStateQueryAspect.RuntimeTransitionInfo GetLayerCurrentTransitionInfo(int layerIndex)
            => GetStateQueryAspect().GetLayerCurrentTransitionInfo(layerIndex);

        internal void RaiseEvent(AnimationEventComponent animationEventComponent)
        {
            OnAnimationEvent(animationEventComponent);
        }

        internal void RaiseEvent(AnimatorControllerEventComponent animationEventComponent)
        {
            OnAnimatorControllerEvent(animationEventComponent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetHash(string name) => RukhankaUtils.GetHash(name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected AnimatorParametersAspect GetAspect()
            => EntityManager.GetAspect<AnimatorParametersAspect>(Entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected AnimatorStateQueryAspect GetStateQueryAspect()
            => EntityManager.GetAspect<AnimatorStateQueryAspect>(Entity);

        protected void TryToAddEventListener()
        {
            if (!eventRegistered && EntityManager.HasBuffer<AnimationEventComponent>(Entity))
            {
                eventRegistered = true;
                AddEventListener();
            }
        }

        protected void TryToRemoveEventListener()
        {
            if (eventRegistered)
            {
                eventRegistered = false;
                RemoveEventListener();
            }
        }

        private void AddEventListener()
        {
            RukhankaAnimationEventListenSystem.AddListener(Entity, this);
        }

        private void RemoveEventListener()
        {
            RukhankaAnimationEventListenSystem.RemoveListener(Entity);
        }

        protected void TryToAddControllerEventListener()
        {
            if (!controllerEventRegistered && EntityManager.HasBuffer<AnimatorControllerEventComponent>(Entity))
            {
                controllerEventRegistered = true;
                AddControllerEventListener();
            }
        }

        protected void TryToRemoveControllerEventListener()
        {
            if (controllerEventRegistered)
            {
                controllerEventRegistered = false;
                RemoveControllerEventListener();
            }
        }

        private void AddControllerEventListener()
        {
            RukhankaAnimatorControllerEventListenSystem.AddListener(Entity, this);
        }

        private void RemoveControllerEventListener()
        {
            RukhankaAnimatorControllerEventListenSystem.RemoveListener(Entity);
        }

        private Entity CreateAttachableEntity(GameObject attachable, Entity boneEntity)
        {
            var boneTransform = EntityManager.GetComponentData<LocalToWorld>(boneEntity);

            attachable.transform.SetPositionAndRotation(boneTransform.Position, boneTransform.Rotation);

            var attachEntity = EntityManager.CreateEntity(
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(Parent),
                typeof(CopyTransformToGameObject),
                typeof(Transform));

            EntityManager.SetComponentData(attachEntity, new Parent()
            {
                Value = boneEntity
            });

            EntityManager.SetComponentData(attachEntity, new LocalToWorld()
            {
                Value = Matrix4x4.TRS(boneTransform.Position, boneTransform.Rotation, Vector3.one)
            });

            EntityManager.AddComponentObject(attachEntity, attachable.transform);

            return attachEntity;
        }
    }
}