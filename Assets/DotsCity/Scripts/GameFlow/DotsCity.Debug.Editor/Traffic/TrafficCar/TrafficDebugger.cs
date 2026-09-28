using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR
using static Spirit604.DotsCity.Debug.TrafficDebuggerSystem;
#endif

namespace Spirit604.DotsCity.Debug
{
    public class TrafficDebugger : MonoBehaviour
    {
        public enum TrafficDebuggerType { Default, Target, ApproachSpeed, State, IdleState, TargetIndex, PathIndex, SpeedLimit, ChangeLane, Input, Collision, Obstacle, NoTarget, FollowPath }

#pragma warning disable 0414

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficDebug.html#traffic-debugger")]
        [SerializeField]
        private string link;

        [OnValueChanged(nameof(EnabledStateChanged))]
        [SerializeField] private bool enableDebug;

        [Tooltip("" +
            "<b>Target</b> : shows the target position of the vehicle\r\n\r\n" +
            "<b>Approach speed</b> : shows the vehicle's approach speed\r\n\r\n" +
            "<b>State</b> : shows the state of the vehicle\r\n\r\n" +
            "<b>Idle State</b> : shows the reason for the vehicle's idle state\r\n\r\n" +
            "<b>Target index</b> : shows the target indexes of the vehicle\r\n\r\n" +
            "<b>Path index</b> : shows the path index of the vehicle\r\n\r\n" +
            "<b>Speed limit</b> : shows the current speed and the speed limit of the vehicle\r\n\r\n" +
            "<b>Change lane</b> : shows the change lane point on the lane in the scene\r\n\r\n" +
            "<b>Input</b> : shows the input data of the vehicle\r\n\r\n" +
            "<b>Collision</b> : shows the collision direction of the vehicle\r\n\r\n" +
            "<b>Obstacle</b> : shows the obstacle entity & obstacle reason type of the vehicle\r\n\r\n" +
            "<b>No target</b> : shows the list of vehicles without a destination" +
            "")]
        [SerializeField] private TrafficDebuggerType debuggerType;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private bool showObstacleInfo = true;
        [SerializeField] private bool showCommonInfo = true;
        [SerializeField] private bool showList = true;

#pragma warning restore 0414

#if UNITY_EDITOR

        private TrafficDebuggerSystem trafficDebuggerSystem;
        private Dictionary<TrafficDebuggerType, ITrafficDebugger> debuggers = new Dictionary<TrafficDebuggerType, ITrafficDebugger>();
        private Entity entity;
        private bool registered;

        public bool EnableDebug => enableDebug && registered;
        public TrafficDebuggerType DebuggerType => debuggerType;
        public Color TextColor => textColor;
        public bool ShowObstacleInfo => showObstacleInfo;
        public bool ShowCommonInfo => showCommonInfo;
        public NativeList<TrafficDebugInfo> Traffics => trafficDebuggerSystem.Traffics;
        public ITrafficDebugger Debugger => debuggerType != TrafficDebuggerType.Default && Application.isPlaying ? debuggers[debuggerType] : null;
        public ICustomTrafficDebugger CustomDebugger { get; private set; }

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        private void Awake()
        {
            trafficDebuggerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficDebuggerSystem>();
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            EnabledStateChanged();

            debuggers.Add(TrafficDebuggerType.Target, new TrafficTargetIndexCommonDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.ApproachSpeed, new TrafficApproachSpeedCommonDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.State, new TrafficStateDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.IdleState, new TrafficIdleStateDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.TargetIndex, new TrafficIndexDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.PathIndex, new TrafficPathIndexDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.SpeedLimit, new TrafficSpeedLimitDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.ChangeLane, new TrafficChangeLaneCommonDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.Input, new TrafficInputDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.Collision, new TrafficCollisionDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.Obstacle, new TrafficObstacleDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.NoTarget, new TrafficNoTargetDebugger(entityManager));
            debuggers.Add(TrafficDebuggerType.FollowPath, new TrafficFollowPathDebugger(entityManager));
            DebuggerChanged();
        }

        public void EnabledStateChanged()
        {
            if (!Application.isPlaying) return;

            try
            {
                if (enableDebug)
                {
                    if (entity == Entity.Null)
                    {
                        entity = EntityManager.CreateEntity(typeof(TrafficDebuggerSystem.EnabledTag));

                        if (!registered)
                        {
                            registered = true;
                            trafficDebuggerSystem = DefaultWorldUtils.CreateAndAddSystemManaged<TrafficDebuggerSystem, DebugGroup>();
                        }
                    }
                }
                else
                {
                    if (entity != Entity.Null)
                    {
                        EntityManager.DestroyEntity(entity);
                        entity = Entity.Null;
                    }
                }
            }
            catch { }
        }

        public void OnInspectorEnabled()
        {
            DebuggerChanged();
        }

        public void DebuggerChanged()
        {
            CustomDebugger = null;

            var debugger = Debugger;

            if (debugger != null)
            {
                var custom = debugger is ICustomTrafficDebugger;

                if (custom)
                {
                    CustomDebugger = debugger as ICustomTrafficDebugger;
                }
            }
        }
#endif
    }
}