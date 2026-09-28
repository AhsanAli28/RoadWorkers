using Spirit604.CityEditor;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Custom.Authoring
{
    [CreateAssetMenu(fileName = "VehicleCustomTemplate", menuName = CityEditorBookmarks.CITY_EDITOR_TRAFFIC_EDITOR_CONFIGS_PATH + "VehicleCustomTemplate")]
    [ExecuteInEditMode]
    public class VehicleCustomTemplate : ScriptableObject
    {
        #region Helper types

        [Flags]
        public enum CopySettingsType
        {
            PhysicsSettings = 1 << 0,
            CenterOfMass = 1 << 1,
            Offsets = 1 << 2,
            Settings = 1 << 3,
        }

        public enum TemplateOperationType
        {
            CreateNew, CopyFromTemplate, SaveToTemplate
        }

        public enum TemplateNameSource
        {
            VehicleName, Custom
        }

        public enum TemplatePathSource
        {
            DefaultPath, Custom
        }

        [Serializable]
        public class CachedWheelData
        {
            public bool Driving;
            public bool Brake;
            public float BrakeRate;
            public float HandBrakeRate;
            public float LocalOffset;
        }

        [Serializable]
        public struct MassDistributionWrapper
        {
            public RigidTransform Transform;
            public float3 InertiaTensor;

            public static implicit operator MassDistributionWrapper(MassDistribution massDistribution)
            {
                return new MassDistributionWrapper()
                {
                    Transform = massDistribution.Transform,
                    InertiaTensor = massDistribution.InertiaTensor
                };
            }

            public static implicit operator MassDistribution(MassDistributionWrapper massDistribution)
            {
                return new MassDistribution()
                {
                    Transform = massDistribution.Transform,
                    InertiaTensor = massDistribution.InertiaTensor
                };
            }
        }

        #endregion

        #region Template settings

        [HideInInspector]
        public string GUID;

        public string TemplateName;

        #endregion

        #region PhysicsBody settings

        public float VehicleMass;
        public float LinearDamping;
        public float AngularDamping;
        public float GravityFactor;

        public bool OverrideDefaultMassDistribution;
        public MassDistributionWrapper CustomMassDistribution;

        #endregion

        #region Offset settings

        public float VehicleMeshOffset;

        #endregion

        #region VehicleAuthoring settings

        public float WheelMass;
        public float MaxSteeringAngle;
        public float PowerSteering;
        public float Radius;
        public float Width;
        public float ApplyImpulseOffset;
        public CastType CastType;
        public PhysicsCategoryTags CastLayer;

        public float SuspensionLength;
        public float Stiffness;
        public float Damping;

        public AnimationCurve Longitudinal;
        public AnimationCurve Lateral;
        public float ForwardFriction;
        public float LateralFriction;
        public float BrakeFriction;

        public bool UseForwardTransientForce;
        public float MinTransientForwardSpeed;
        public float MaxForwardFrictionRate;
        public float ForwardRelaxMultiplier;

        public bool UseLateralTransientForce;
        public float MinTransientLateralSpeed;
        public float MaxLateralFrictionRate;
        public float LateralRelaxMultiplier;

        public float BrakeTorque;
        public float HandbrakeTorque;

        public AnimationCurve Torque;
        public float TransmissionRate;
        public float Drag;

        public List<CachedWheelData> AllWheels = new List<CachedWheelData>();

        #endregion

        #region Properties

        public Vector3 CenterOfMass => CustomMassDistribution.Transform.pos;

        #endregion

        #region Unity lifecycle

#if UNITY_EDITOR
        private void OnEnable()
        {
            var container = VehicleCustomTemplateContainer.GetContainer();

            if (container)
            {
                container.AddTemplate(this);
            }
        }

        private void OnDestroy()
        {
            var container = VehicleCustomTemplateContainer.GetContainer();

            if (container)
            {
                container.RemoveTemplate(this);
            }
        }
#endif

        #endregion

        #region Public static methods

        public static VehicleCustomTemplate CreateTemplate(string path, string name)
        {
            var currentPath = path;

            if (string.IsNullOrEmpty(currentPath))
            {
                currentPath = CityEditorBookmarks.VEHICLE_TEMPLATE_PATH;
            }

#if UNITY_EDITOR

            var vehicleCustomTemplate = AssetDatabaseExtension.CreatePersistScriptableObject<VehicleCustomTemplate>(currentPath, name);
            vehicleCustomTemplate.GUID = Guid.NewGuid().ToString();
            vehicleCustomTemplate.TemplateName = name;

            var container = VehicleCustomTemplateContainer.GetContainer();

            if (container)
            {
                container.AddTemplate(vehicleCustomTemplate);
            }

            EditorSaver.SetObjectDirty(vehicleCustomTemplate);

            return vehicleCustomTemplate;

#else
            return null;
#endif
        }

        public static VehicleCustomTemplate CreateTemplate(string path, string name, VehicleAuthoring vehicleAuthoring, PhysicsBodyAuthoring physicsBodyAuthoring, CopySettingsType copyType)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var template = CreateTemplate(path, name);
            template.SaveToTemplate(vehicleAuthoring, physicsBodyAuthoring, copyType);

            return template;
        }

        #endregion

        #region Public methods

        public void SaveToTemplate(VehicleAuthoring vehicleAuthoring, PhysicsBodyAuthoring physicsBody, CopySettingsType copyType, bool recordUndo = false)
        {
            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Undo template");
#endif
            }

            CopyPhysics(physicsBody, copyType);
            CopySettings(vehicleAuthoring, copyType);

            EditorSaver.SetObjectDirty(this);
        }

        public void CopyPhysics(PhysicsBodyAuthoring physicsBody, CopySettingsType copyType)
        {
            if (!physicsBody)
            {
                return;
            }

            if (copyType.HasFlag(CopySettingsType.PhysicsSettings))
            {
                VehicleMass = physicsBody.Mass;
                AngularDamping = physicsBody.AngularDamping;
                LinearDamping = physicsBody.LinearDamping;
                GravityFactor = physicsBody.GravityFactor;
            }

            if (copyType.HasFlag(CopySettingsType.CenterOfMass))
            {
                OverrideDefaultMassDistribution = physicsBody.OverrideDefaultMassDistribution;
                CustomMassDistribution = physicsBody.CustomMassDistribution;
            }
        }

        public void CopySettings(VehicleAuthoring vehicleAuthoring, CopySettingsType copyType)
        {
            if (!copyType.HasFlag(CopySettingsType.Settings))
            {
                return;
            }

            if (!vehicleAuthoring)
            {
                return;
            }

            if (copyType.HasFlag(CopySettingsType.Offsets))
            {
                VehicleMeshOffset = vehicleAuthoring.GetComponent<CarEntityAuthoringBase>()?.HullMeshRenderer?.transform.localPosition.y ?? 0;
            }

            WheelMass = vehicleAuthoring.WheelMass;
            MaxSteeringAngle = vehicleAuthoring.MaxSteeringAngle;
            PowerSteering = vehicleAuthoring.PowerSteering;
            Radius = vehicleAuthoring.Radius;
            Width = vehicleAuthoring.Width;
            ApplyImpulseOffset = vehicleAuthoring.ApplyImpulseOffset;
            CastType = vehicleAuthoring.CastType;
            CastLayer = vehicleAuthoring.CastLayer;

            SuspensionLength = vehicleAuthoring.SuspensionLength;
            Stiffness = vehicleAuthoring.Stiffness;
            Damping = vehicleAuthoring.Damping;

#if UNITY_EDITOR
            Longitudinal = EditorExtension.GetCopyCurve(vehicleAuthoring.Longitudinal);
            Lateral = EditorExtension.GetCopyCurve(vehicleAuthoring.Lateral);
#endif

            ForwardFriction = vehicleAuthoring.ForwardFriction;
            LateralFriction = vehicleAuthoring.LateralFriction;
            BrakeFriction = vehicleAuthoring.BrakeFriction;

            UseForwardTransientForce = vehicleAuthoring.UseForwardTransientForce;
            MinTransientForwardSpeed = vehicleAuthoring.MinTransientForwardSpeed;
            MaxForwardFrictionRate = vehicleAuthoring.MaxForwardFrictionRate;
            ForwardRelaxMultiplier = vehicleAuthoring.ForwardRelaxMultiplier;

            UseLateralTransientForce = vehicleAuthoring.UseLateralTransientForce;
            MinTransientLateralSpeed = vehicleAuthoring.MinTransientLateralSpeed;
            MaxLateralFrictionRate = vehicleAuthoring.MaxLateralFrictionRate;
            LateralRelaxMultiplier = vehicleAuthoring.LateralRelaxMultiplier;

            BrakeTorque = vehicleAuthoring.BrakeTorque;
            HandbrakeTorque = vehicleAuthoring.HandbrakeTorque;

#if UNITY_EDITOR
            Torque = EditorExtension.GetCopyCurve(vehicleAuthoring.Torque);
#endif

            TransmissionRate = vehicleAuthoring.TransmissionRate;
            Drag = vehicleAuthoring.Drag;

            AllWheels.Clear();

            for (int i = 0; i < vehicleAuthoring.AllWheels.Count; i++)
            {
                var wheelData = vehicleAuthoring.AllWheels[i];

                float wheelOffset = 0;

                if (copyType.HasFlag(CopySettingsType.Offsets))
                {
                    wheelOffset = wheelData.Wheel.transform.localPosition.y;
                }

                AllWheels.Add(new CachedWheelData()
                {
                    Brake = wheelData.Brake,
                    Driving = wheelData.Driving,
                    BrakeRate = wheelData.BrakeRate,
                    HandBrakeRate = wheelData.HandBrakeRate,
                    LocalOffset = wheelOffset
                });
            }
        }

        public void CopyFromTemplate(VehicleAuthoring vehicleAuthoring, PhysicsBodyAuthoring physicsBody, CopySettingsType copyType, bool recordUndo = false)
        {
            Transform meshTransform = vehicleAuthoring.GetComponent<CarEntityAuthoringBase>()?.HullMeshRenderer?.transform ?? null;

            CopyFromTemplate(vehicleAuthoring, physicsBody, meshTransform, copyType, recordUndo);
        }

        public void CopyFromTemplate(VehicleAuthoring vehicleAuthoring, PhysicsBodyAuthoring physicsBody, Transform meshTransform, CopySettingsType copyType, bool recordUndo = false)
        {
            SetPhysics(physicsBody, copyType, recordUndo);
            SetSettings(vehicleAuthoring, meshTransform, copyType, recordUndo);

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
            }
        }

        public void SetPhysics(PhysicsBodyAuthoring physicsBody, CopySettingsType copyType, bool recordUndo = false)
        {
            if (!physicsBody)
            {
                return;
            }

            if (copyType.HasFlag(CopySettingsType.PhysicsSettings) || copyType.HasFlag(CopySettingsType.CenterOfMass))
            {
                if (recordUndo)
                {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(physicsBody, "Undo physics body");
#endif
                }
            }

            if (copyType.HasFlag(CopySettingsType.PhysicsSettings))
            {
                physicsBody.Mass = VehicleMass;
                physicsBody.LinearDamping = LinearDamping;
                physicsBody.AngularDamping = AngularDamping;
                physicsBody.GravityFactor = GravityFactor;
                EditorSaver.SetObjectDirty(physicsBody);
            }

            if (copyType.HasFlag(CopySettingsType.CenterOfMass))
            {
                physicsBody.OverrideDefaultMassDistribution = OverrideDefaultMassDistribution;
                physicsBody.CustomMassDistribution = CustomMassDistribution;
                EditorSaver.SetObjectDirty(physicsBody);
            }
        }

        public void SetSettings(VehicleAuthoring vehicleAuthoring, Transform meshTransform, CopySettingsType copyType, bool recordUndo = false)
        {
            if (!copyType.HasFlag(CopySettingsType.Settings))
            {
                return;
            }

            if (!vehicleAuthoring)
            {
                return;
            }

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(vehicleAuthoring, "Undo vehicleAuthoring");
#endif
            }

            if (copyType.HasFlag(CopySettingsType.Offsets))
            {

                if (meshTransform)
                {
                    var pos = meshTransform.transform.localPosition;
                    pos.y = VehicleMeshOffset;
                    meshTransform.transform.localPosition = pos;
                }
            }

            vehicleAuthoring.WheelMass = WheelMass;
            vehicleAuthoring.MaxSteeringAngle = MaxSteeringAngle;
            vehicleAuthoring.PowerSteering = PowerSteering;
            vehicleAuthoring.Radius = Radius;
            vehicleAuthoring.Width = Width;
            vehicleAuthoring.ApplyImpulseOffset = ApplyImpulseOffset;
            vehicleAuthoring.CastType = CastType;
            vehicleAuthoring.CastLayer = CastLayer;

            vehicleAuthoring.SuspensionLength = SuspensionLength;
            vehicleAuthoring.Stiffness = Stiffness;
            vehicleAuthoring.Damping = Damping;

#if UNITY_EDITOR
            vehicleAuthoring.Longitudinal = EditorExtension.GetCopyCurve(Longitudinal);
            vehicleAuthoring.Lateral = EditorExtension.GetCopyCurve(Lateral);
#endif

            vehicleAuthoring.ForwardFriction = ForwardFriction;
            vehicleAuthoring.LateralFriction = LateralFriction;
            vehicleAuthoring.BrakeFriction = BrakeFriction;

            vehicleAuthoring.UseForwardTransientForce = UseForwardTransientForce;
            vehicleAuthoring.MinTransientForwardSpeed = MinTransientForwardSpeed;
            vehicleAuthoring.MaxForwardFrictionRate = MaxForwardFrictionRate;
            vehicleAuthoring.ForwardRelaxMultiplier = ForwardRelaxMultiplier;

            vehicleAuthoring.UseLateralTransientForce = UseLateralTransientForce;
            vehicleAuthoring.MinTransientLateralSpeed = MinTransientLateralSpeed;
            vehicleAuthoring.MaxLateralFrictionRate = MaxLateralFrictionRate;
            vehicleAuthoring.LateralRelaxMultiplier = LateralRelaxMultiplier;

            vehicleAuthoring.BrakeTorque = BrakeTorque;
            vehicleAuthoring.HandbrakeTorque = HandbrakeTorque;

#if UNITY_EDITOR
            vehicleAuthoring.Torque = EditorExtension.GetCopyCurve(Torque);
#endif

            vehicleAuthoring.TransmissionRate = TransmissionRate;
            vehicleAuthoring.Drag = Drag;

            for (int i = 0; i < AllWheels.Count; i++)
            {
                if (vehicleAuthoring.AllWheels.Count > i)
                {
                    var wheelData = AllWheels[i];

                    vehicleAuthoring.AllWheels[i].Brake = wheelData.Brake;
                    vehicleAuthoring.AllWheels[i].Driving = wheelData.Driving;
                    vehicleAuthoring.AllWheels[i].BrakeRate = wheelData.BrakeRate;
                    vehicleAuthoring.AllWheels[i].HandBrakeRate = wheelData.HandBrakeRate;

                    if (copyType.HasFlag(CopySettingsType.Offsets))
                    {
                        if (vehicleAuthoring.AllWheels[i].Wheel)
                        {
                            var pos = vehicleAuthoring.AllWheels[i].Wheel.transform.localPosition;
                            pos.y = wheelData.LocalOffset;
                            vehicleAuthoring.AllWheels[i].Wheel.transform.localPosition = pos;
                        }
                    }
                }
            }

            EditorSaver.SetObjectDirty(vehicleAuthoring);
        }

        #endregion
    }
}