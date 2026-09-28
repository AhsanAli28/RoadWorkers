using Spirit604.DotsCity.Gameplay.Chaser;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Spirit604.Gameplay;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace Spirit604.StateMachine
{
    [RequireComponent(typeof(ChasingCarHealth), typeof(AiNpcCarBehaviour), typeof(NavMeshAgent))]
    public class ChasingCarBehaviour : StateMachine
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private float maxSteeringAngle = 40f;
        [SerializeField] private float minimumTargetVelocity = 1f;
        [SerializeField] private float maxDistanceToTarget = 35F;
        [SerializeField] private float maxDistanceForSlowing = 12f;
        [SerializeField] private float slowTargetMaxSpeed = 5f;
        [SerializeField] private float slowingMultiplier = 0.4F;
        [SerializeField] private float sideChaseDistance = 4F;
        [SerializeField] private float accelerationSpeedMultiplier = 1.5f;
        [SerializeField] private float minDistanceToChase = 6f;
        [SerializeField] private float maxDistanceToFollow = 10f;
        [SerializeField] private float targetOffset = 4f;
        [SerializeField] private float calculateFollowAttemptTime = 3f;

        private IAIShotTargetProvider shootTargetProvider;
        private IVehicleInput input;
        private AiNpcCarBehaviour aiNpcCarBehaviour;

        private float speedLimitMultiplier = 1f;
        private int gasInput;
        private float desiredSteeringAngle;
        private float spawnTime;
        private HealthBehaviourBase healthBehaviour;

        public float MinimumTargetVelocity => minimumTargetVelocity;
        public float AccelerationSpeedMultiplier => accelerationSpeedMultiplier;
        public float SlowingMultiplier => slowingMultiplier;
        public float MaxDistanceForSlowing => maxDistanceForSlowing;
        public float SlowTargetMaxSpeed => slowTargetMaxSpeed;
        public float MinDistanceToChase => minDistanceToChase;
        public float MaxDistanceToFollow => maxDistanceToFollow;
        public float CalculateFollowAttemptTime => calculateFollowAttemptTime;
        public float SideChaseDistance => sideChaseDistance;

        public float DistanceToTarget { get; private set; }
        public int ChaseIndex { get; set; }
        public float TargetOffset { get => targetOffset; }
        public NavMeshAgent NavMeshAgent { get; private set; }
        public Camera MainCamera { get; private set; }
        public Vector3 TargetWayPoint { get; set; }
        public Vector3 TargetPosition => shootTargetProvider.GetTarget();
        public Vector3 TargetForward => shootTargetProvider.TargetForward;
        public float TargetVelocityValue => shootTargetProvider.TargetVelocity.magnitude;

        public bool CanSideChasing { get; set; }
        public bool OutOfTargetRangeNotified { get; set; }

        public int GasInput
        {
            get => gasInput;
            set
            {
                if (gasInput != value)
                {
                    gasInput = value;
                    UpdateThrottle();
                }
            }
        }

        public float DesiredSteeringAngle
        {
            get => desiredSteeringAngle;
            set
            {
                value = Mathf.Clamp(value, -maxSteeringAngle, maxSteeringAngle);
                desiredSteeringAngle = value;
                input.Steering = value / maxSteeringAngle;
            }
        }

        public Vector3 ForceDirection { get; internal set; }

        private bool TargetTooFar => DistanceToTarget > maxDistanceToTarget;

        public event Action<ChasingCarBehaviour> OutOfTargetRange = delegate { };

        protected override void Awake()
        {
            NavMeshAgent = GetComponent<NavMeshAgent>();
            input = GetComponent<IVehicleInput>();
            aiNpcCarBehaviour = GetComponent<AiNpcCarBehaviour>();
            aiNpcCarBehaviour.IsCombatState = true;
            healthBehaviour = GetComponent<HealthBehaviourBase>();
            healthBehaviour.OnDeath += HealthBehaviour_OnDeath;

            NavMeshAgent.updatePosition = false;
            NavMeshAgent.updateRotation = false;
            NavMeshAgent.enabled = false;
            DisableByDefault = false;
            base.Awake();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            input.SwitchEnabledState(true);
            spawnTime = Time.time;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            NavMeshAgent.enabled = false;
            OutOfTargetRangeNotified = false;
            DistanceToTarget = 0;
        }

        protected override void Update()
        {
            if (shootTargetProvider.HasTarget)
            {
                DistanceToTarget = Vector3.Distance(transform.position, shootTargetProvider.GetTarget());

                if (!NavMeshAgent.isOnNavMesh)
                {
                    NavMeshAgent.enabled = false;
                    NavMeshAgent.enabled = true;
                }

                if (!NavMeshAgent.isOnNavMesh)
                    return;

                if (TargetTooFar && !OutOfTargetRangeNotified & Time.time - spawnTime > 3f)
                {
                    OutOfTargetRangeNotified = true;
                    OutOfTargetRange(this);
                }
                //TargetVelocity = Target.Velocity.Flat().magnitude;

                base.Update();
            }

            NavMeshAgent.nextPosition = transform.position;
        }

        public void Initialize(IAIShotTargetProvider shootTargetProvider, Camera camera, int index)
        {
            this.shootTargetProvider = shootTargetProvider;
            this.MainCamera = camera;
            aiNpcCarBehaviour.Initialize(shootTargetProvider, camera);
            ChaseIndex = index;
        }

        public void SetSpeedLimit(float speedLimit)
        {
            this.speedLimitMultiplier = speedLimit;
            UpdateThrottle();
        }

        public void SetPositionAndRotation(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            rb.position = spawnPosition;
            rb.rotation = spawnRotation;
            NavMeshAgent.nextPosition = transform.position;
            NavMeshAgent.enabled = true;
        }

        private void UpdateThrottle()
        {
            input.Throttle = gasInput * speedLimitMultiplier;
        }

        private void Reset()
        {
            rb = GetComponentInChildren<Rigidbody>();
            EditorSaver.SetObjectDirty(this);
        }

        private void HealthBehaviour_OnDeath(HealthBehaviourBase @base)
        {
            enabled = false;
            NavMeshAgent.enabled = false;
            input.SwitchEnabledState(false);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (CurrentState != null)
            {
                var pos = transform.position + new Vector3(0, 2f);
                EditorExtension.DrawWorldString(CurrentState.name, pos);
            }
        }
#endif
    }
}