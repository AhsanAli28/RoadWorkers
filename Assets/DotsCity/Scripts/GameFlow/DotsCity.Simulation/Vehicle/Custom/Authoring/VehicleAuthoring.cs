using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Custom.Authoring
{
    public class VehicleAuthoring : MonoBehaviour, IVehicleAuthoring
    {
        #region Constans

        public const float DefaultSuspensionLength = 0.6f;

        #endregion

        #region Helper types

        public enum OriginMoveType { Disabled, Wheel, SuspensionOrigin, Suspension }

        #endregion

        #region Inspector variables

        #region Wheel

        [Tooltip("Wheel mass")]
        public float WheelMass = 20;

        [Tooltip("Max steering angle of the steering wheel in degrees")]
        public float MaxSteeringAngle = 35f;

        [Tooltip("Rate of steering improvement")]
        public float PowerSteering = 1f;

        [Tooltip("Limiting steering angle based on vehicle speed")]
        public bool CustomSteeringLimit;

        [Tooltip("Y-axis rate value (1 - max steering angle), X-axis speed in metres per second")]
        public AnimationCurve SteeringLimitCurve = AnimationCurve.Constant(0, 40, 1);

        [Tooltip("Wheel radius")]
        public float Radius = 0.4f;

        [Tooltip("Wheel width")]
        public float Width = 0.2f;

        [Tooltip("Applying force offset relative to lower point of wheel (without offsetting the force applied to the lower point of the wheel)")]
        public float ApplyImpulseOffset = -1;

        [Tooltip("" +
            "<b>Ray</b> : raycast by ray\r\n\r\n" +
            "<b>Collider</b> : raycast by capsule collider (collider size based on wheel radius and width)")]
        public CastType CastType;

        [Tooltip("Physical layer that collides with the wheel")]
        public PhysicsCategoryTags CastLayer = new PhysicsCategoryTags()
        {
            Category00 = true
        };

        #endregion

        #region Suspension

        [Tooltip("Length of suspension")]
        public float SuspensionLength = DefaultSuspensionLength;

        [Tooltip("Spring stiffness of suspension")]
        public float Stiffness = 40f;

        [Tooltip("Force to return spring to its original length")]
        public float Damping = 3000f;

        #endregion

        #region Friction

        [Tooltip("Forward friction curve (Y-axis - forward slip value, X-axis forward speed in metres per second)")]
        public AnimationCurve Longitudinal;

        [Tooltip("Lateral friction curve (Y-axis - lateral slip value, X-axis lateral speed in metres per second)")]
        public AnimationCurve Lateral;

        [Tooltip("Forward friction value")]
        public float ForwardFriction = 2000f;

        [Tooltip("Lateral friction value")]
        public float LateralFriction = 2000f;

        [Tooltip("Brake friction value")]
        public float BrakeFriction = 5000f;

        [Tooltip("Drag value of the vehicle")]
        public float Drag = 1f;

        #endregion

        #region Transient force

        [Tooltip("Transient force required to hold the car on an inclined ramp during manual braking in the local Z-axis of the car")]
        public bool UseForwardTransientForce = true;

        [Tooltip("Min forward speed when transient force is applied")]
        public float MinTransientForwardSpeed = 1f;

        [Tooltip("Max friction for transient force calculated by multiplying the entered rate by the forward friction")]
        public float MaxForwardFrictionRate = 50f;

        [Tooltip("Step of forward force increase per frame")]
        public float ForwardRelaxMultiplier = 0.1f;

        [Tooltip("Transient force required to hold the car on an inclined ramp during manual braking in the local X-axis of the car")]
        public bool UseLateralTransientForce = true;

        [Tooltip("Min lateral speed when transient force is applied")]
        public float MinTransientLateralSpeed = 0.4f;

        [Tooltip("Max friction for transient force calculated by multiplying the entered rate by the lateral friction")]
        public float MaxLateralFrictionRate = 10;

        [Tooltip("Step of lateral force increase per frame")]
        public float LateralRelaxMultiplier = 0.1f;

        #endregion

        #region Brakes

        [Tooltip("Torque of brake")]
        public float BrakeTorque = 100;

        [Tooltip("Torque of handbrake")]
        public float HandbrakeTorque = 200f;

        #endregion

        #region Engine

        [Tooltip("Engine torque")]
        public AnimationCurve Torque;

        [Tooltip("Engine torque to wheel speed ratio")]
        public float TransmissionRate = 0.2f;

        #endregion

        #region Debug

        [Tooltip("On/off debugging for wheel and suspension at runtime (VehicleCustomDebugger required on the scene)")]
        public bool ShowDebug;

        [Tooltip("On/off display of suspension origin")]
        public bool ShowSuspensionOrigin;

        [Tooltip("On/off display of suspension")]
        public bool ShowSuspension = true;

        [Tooltip("" +
            "<b>Disabled</b> : disabled handle\r\n\r\n" +
            "<b>Wheel</b> : on/off handle for wheel origin\r\n\r\n" +
            "<b>Suspension origin</b> : on/off handle for suspesnion origin\r\n\r\n" +
            "<b>Suspension</b> : on/off handle for suspension and wheel origin")]
        public OriginMoveType OriginMove = OriginMoveType.Wheel;

        #endregion

        #region Wheel links

        public List<GameObject> SteeringWheels = new List<GameObject>();
        public List<WheelData> AllWheels = new List<WheelData>();

        #endregion

        #endregion

        #region Properties

        public float WheelRadius { get => Radius; set => Radius = value; }

        public Vector3 GetWorldSuspensionOrigin(Transform sourceObject) => sourceObject.position + GetSuspensionOffset();

        public Vector3 GetSuspensionOffset() => new Vector3(0, SuspensionLength, 0);

        public Vector3 GetWorldSuspensionEnd(Transform sourceObject) => GetWorldSuspensionOrigin(sourceObject) - new Vector3(0, SuspensionLength);

        #endregion

        #region Public methods

        public void AddWheel(GameObject sourceObject)
        {
            if (!sourceObject)
            {
                return;
            }

            if (HasWheel(sourceObject))
            {
                return;
            }

            AllWheels.Add(new WheelData(sourceObject));

            EditorSaver.SetObjectDirty(this);
        }

        public void InsertWheel(GameObject sourceObject, int index)
        {
            if (!sourceObject)
            {
                return;
            }

            if (AllWheels.Count > index)
            {
                AllWheels[index].Wheel = sourceObject;
                EditorSaver.SetObjectDirty(this);
                return;
            }

            var wheelData = new WheelData(sourceObject);

            if (AllWheels.Count > 0)
            {
                wheelData.Brake = AllWheels[0].Brake;
                wheelData.Driving = AllWheels[0].Driving;
                wheelData.HandBrakeRate = AllWheels[0].HandBrakeRate;
            }

            AllWheels.Add(wheelData);

            EditorSaver.SetObjectDirty(this);
        }

        public void AddSteeringWheel(GameObject sourceObject)
        {
            if (!sourceObject)
            {
                return;
            }

            if (HasSteeringWheel(sourceObject))
            {
                return;
            }

            SteeringWheels.Add(sourceObject);

            SetDirty();
        }

        public bool HasWheel(GameObject sourceObject)
        {
            return AllWheels.Where(a => a.Wheel.Equals(sourceObject)).Any();
        }

        public bool HasSteeringWheel(GameObject sourceObject)
        {
            return SteeringWheels.Contains(sourceObject);
        }

        public void MoveWheels(float offset, bool recordUndo = true)
        {
            MoveWheels(new Vector3(0, offset), recordUndo);
        }

        public void MoveWheels(Vector3 offset, bool recordUndo = true)
        {
            if (offset == Vector3.zero)
            {
                return;
            }

#if UNITY_EDITOR

            for (int i = 0; i < AllWheels.Count; i++)
            {
                if (!AllWheels[i].Wheel)
                {
                    continue;
                }

                if (recordUndo)
                {
                    Undo.RegisterCompleteObjectUndo(AllWheels[i].Wheel.transform, "Undo offset");
                }

                AllWheels[i].Wheel.transform.position += offset;
            }

            if (recordUndo && AllWheels.Count > 1)
            {
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
#endif
        }

        public void ChangeSuspension(float offset, bool recordUndo = true)
        {
            if (offset == 0)
            {
                return;
            }

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Undo suspension");
#endif
            }

            SuspensionLength += offset;
            EditorSaver.SetObjectDirty(this);
        }

        public void SetDirty()
        {
            EditorSaver.SetObjectDirty(this);
        }

        #endregion
    }
}