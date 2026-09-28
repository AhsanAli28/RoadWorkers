using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using System;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    [RequireComponent(typeof(PhysicsSwitcher))]
    public class PhysicsHybridEntityAdapter : HybridEntityAdapter, IVehicleEntityRef, IHybridComponent
    {
        public enum PhysicsState { Default, FullEnabled, Disabled }

        [SerializeField] private Rigidbody rb;
        [SerializeField] private Transform _transform;
        [SerializeField] private PhysicsSwitcher physicsSwitcher;
        [SerializeField] private ScriptSwitcher scriptSwitcher;

        private PhysicsState physicsState;
        private bool cullPhysics;

#if UNITY_6000_0_OR_NEWER
        public Vector3 Velocity => rb.linearVelocity;
#else
        public Vector3 Velocity => rb.velocity;
#endif

        public Transform Transform => _transform;

        public bool Enabled { get; set; }
        public bool Destroyed { get; set; }

        public bool CullPhysics => cullPhysics;
        public PhysicsState CurrentPhysicsState => physicsState;

        protected bool SetVelocityOnEnable { get; private set; } = true;

        public event Action<PhysicsHybridEntityAdapter> OnPhysicsStateChanged = delegate { };

        protected virtual void OnEnable()
        {
            Enabled = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (!Destroyed)
            {
                ResetPhysics();
            }
        }

        public override void Initialize(Entity entity)
        {
            cullPhysics = EntityManager.HasComponent<CullPhysicsTag>(entity);

            var localTransform = EntityManager.GetComponentData<LocalTransform>(entity);
            transform.SetPositionAndRotation(localTransform.Position, localTransform.Rotation);
            rb.position = transform.position;
            rb.rotation = transform.rotation;
            base.Initialize(entity);
        }

        public override bool CheckCullState(CullState cullState)
        {
            if (base.CheckCullState(cullState))
            {
                SetPhysicsState(cullState);
                return true;
            }

            return false;
        }

        public void SetPhysicsState(CullState cullState)
        {
            if (!gameObject.activeSelf || !Enabled) return;

            PhysicsState newPhysicsType = GetPhysicsState(cullState);

            if (physicsState != newPhysicsType)
            {
                physicsState = newPhysicsType;

                switch (newPhysicsType)
                {
                    case PhysicsState.FullEnabled:
                        {
                            rb.position = transform.position;
                            rb.rotation = transform.rotation;

                            physicsSwitcher.SwitchPhysics(true);

                            rb.angularVelocity = default;

                            if (SetVelocityOnEnable)
                            {
                                var velocity = EntityManager.GetComponentData<VelocityComponent>(RelatedEntity).Value;

#if UNITY_6000_0_OR_NEWER
                                rb.linearVelocity = velocity;

#else
                                rb.velocity = velocity;
#endif
                            }
                            else
                            {
#if UNITY_6000_0_OR_NEWER
                                rb.linearVelocity = default;

#else
                                rb.velocity = default;
#endif
                            }

                            scriptSwitcher.SwitchScripts(true);
                            EntityManager.SetComponentEnabled<TrafficMonoMovementDisabled>(RelatedEntity, false);
                            break;
                        }
                    case PhysicsState.Disabled:
                        {
                            physicsSwitcher.SwitchPhysics(false);
                            scriptSwitcher.SwitchScripts(false);
                            EntityManager.SetComponentEnabled<TrafficMonoMovementDisabled>(RelatedEntity, true);
                            break;
                        }
                }

                OnPhysicsStateChanged(this);
            }
        }

        private void ResetPhysics()
        {
            physicsState = default;
            physicsSwitcher.SwitchPhysics(false);
            scriptSwitcher.SwitchScripts(false);
        }

        private PhysicsState GetPhysicsState(CullState cullState)
        {
            var newPhysicsType = PhysicsState.Default;

            switch (cullState)
            {
                case CullState.InViewOfCamera:
                    newPhysicsType = PhysicsState.FullEnabled;
                    break;
                case CullState.PreInitInCamera:
                    newPhysicsType = PhysicsState.FullEnabled;
                    break;
                default:
                    newPhysicsType = PhysicsState.Disabled;
                    break;
            }

            if (!cullPhysics && newPhysicsType == PhysicsState.Disabled)
            {
                newPhysicsType = PhysicsState.FullEnabled;
            }

            return newPhysicsType;
        }

        private void Reset()
        {
            rb = GetComponentInChildren<Rigidbody>();
            _transform = this.transform;
            physicsSwitcher = GetComponentInChildren<PhysicsSwitcher>();
            scriptSwitcher = GetComponentInChildren<ScriptSwitcher>();
            EditorSaver.SetObjectDirty(this);
        }
    }
}
