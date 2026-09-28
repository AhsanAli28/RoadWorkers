using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Spirit604.Extensions;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Spirit604.DotsCity.Simulation.Mono
{
    public class ArcadeVehicleManager : SingletonMonoBehaviour<ArcadeVehicleManager>
    {
        private const int Capacity = 1000;
        private const int WheelCount = 4;

        [SerializeField] private TrafficSettings trafficSettings;
        [SerializeField] private int raycastsPerJob = 32;
        [SerializeField] private bool doProfile;

        private List<ArcadeVehicleController> vehicles = new List<ArcadeVehicleController>();

        public bool JobSystem => trafficSettings?.TrafficSettingsConfig?.JobSystem ?? false;

        private NativeArray<RaycastHit> results;
        private NativeArray<RaycastCommand> commands;

        protected override void Awake()
        {
            base.Awake();

            results = new NativeArray<RaycastHit>(Capacity * WheelCount, Allocator.Persistent);
            commands = new NativeArray<RaycastCommand>(Capacity * WheelCount, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            results.Dispose();
            commands.Dispose();
        }

        private void FixedUpdate()
        {
            if (!JobSystem)
            {
                if (doProfile)
                    Profiler.BeginSample("ArcadeVehicleManager. TickFixed");

                TickFixed();

                if (doProfile)
                    Profiler.EndSample();
            }
            else
            {
                PopulateCommands();

                JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, raycastsPerJob, 1, default(JobHandle));

                handle.Complete();

                if (doProfile)
                    Profiler.BeginSample("ArcadeVehicleManager. TickFixed Jobs");

                int wheelIndex = 0;

                for (int vehicleIndex = 0; vehicleIndex < vehicles.Count; vehicleIndex++)
                {
                    var wheels = vehicles[vehicleIndex].AllWheels;

                    for (var j = 0; j < wheels.Count; j++)
                    {
                        wheels[j].Hit = results[wheelIndex];
                        wheelIndex++;
                    }
                }

                TickFixed();

                if (doProfile)
                    Profiler.EndSample();
            }
        }

        private void Update()
        {
            Tick();
        }

        public void Add(ArcadeVehicleController arcadeVehicle)
        {
            vehicles.Add(arcadeVehicle);
            arcadeVehicle.JobManager = JobSystem;
        }

        public void Remove(ArcadeVehicleController arcadeVehicle)
        {
            vehicles.Remove(arcadeVehicle);
        }

        private void PopulateCommands()
        {
            if (doProfile)
                Profiler.BeginSample("ArcadeVehicleManager. PopulateCommands");

            int wheelIndex = 0;

            for (int vehicleIndex = 0; vehicleIndex < vehicles.Count; vehicleIndex++)
            {
                var vehicle = vehicles[vehicleIndex];
                var vehicleTransform = vehicle.transform;
                var up = vehicleTransform.up;
                var vehicleRigidTransform = new RigidTransform(vehicleTransform.rotation, vehicleTransform.position);

                var wheels = vehicles[vehicleIndex].AllWheels;

                for (var j = 0; j < wheels.Count; j++)
                {
                    var wheel = wheels[j];

                    commands[wheelIndex] = new RaycastCommand()
                    {
                        direction = -up,
                        from = math.transform(vehicleRigidTransform, wheel.SpringOriginLocalPos),
                        distance = vehicle.SpringRestLength + vehicle.WheelRadius,
                        queryParameters = new QueryParameters()
                        {
                            layerMask = vehicle.RaycastLayer.value
                        }
                    };

                    wheelIndex++;
                }
            }

            if (doProfile)
                Profiler.EndSample();
        }

        private void TickFixed()
        {
            for (int i = 0; i < vehicles.Count; i++)
            {
                vehicles[i].TickFixed();
            }
        }

        private void Tick()
        {
            for (int i = 0; i < vehicles.Count; i++)
            {
                vehicles[i].Tick();
            }
        }
    }
}