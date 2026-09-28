#if UNITY_EDITOR
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

#if !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Spirit604.DotsCity.Gameplay.Player
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitGroup))]
    public partial class PlayerVehicleDummyInputSystem : SystemBase
    {
#if !ENABLE_LEGACY_INPUT_MANAGER

        private DefaultInputActions inputActions;

        protected override void OnCreate()
        {
            inputActions = new DefaultInputActions();
            inputActions.Enable();
            base.OnCreate();
        }
#endif

        protected override void OnUpdate()
        {
            Entities
            .WithName("PlayerVehicleDummyInputJob")
            .WithoutBurst()
            .WithAll<PlayerTag, CarEngineStartedTag>()
            .ForEach((ref VehicleInputReader playerVehicleInput, in LocalTransform transform) =>
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                playerVehicleInput.Throttle = Input.GetAxis("Vertical");
                playerVehicleInput.SteeringInput = Input.GetAxis("Horizontal");
#else
                var move = inputActions.Player.Move.ReadValue<Vector2>();
                playerVehicleInput.Throttle = move.y;
                playerVehicleInput.SteeringInput = move.x;
#endif
                playerVehicleInput.HandbrakeInput = InputExtension.GetKey(KeyCode.Space) ? 1 : 0;
            }).Run();

            Entities
            .WithBurst()
            .WithNone<CarEngineStartedTag>()
            .WithAll<PlayerTag>()
            .ForEach((ref VehicleInputReader playerVehicleInput) =>
            {
                playerVehicleInput = VehicleInputReader.GetBrake();
            }).Run();
        }
    }
}
#endif