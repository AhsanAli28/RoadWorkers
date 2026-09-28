using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Npc;
using Spirit604.Extensions;
using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.Npc;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [UpdateInGroup(typeof(InitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PlayerInputSystem : SystemBase
    {
        private IMotionInput input;
        private IShootTargetProvider shootTargetProvider;
        private EntityQuery playerQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            playerQuery = GetEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<InputComponent>());

            RequireForUpdate(playerQuery);
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithAll<PlayerTag>()
            .ForEach((
                ref InputComponent inputComponent,
                ref LocalTransform transform) =>
             {
                 Vector3 shootDirection = Vector3.zero;

                 shootTargetProvider.GetShootDirection(transform.Position, out shootDirection);

                 inputComponent.ShootDirection = shootDirection;
                 inputComponent.ShootInput = input.FireInput.Flat();
                 inputComponent.MovingInput = input.MovementInput.ToVector2_2DSpace();

             }).Run();
        }

        public void Initialize(IMotionInput input, IShootTargetProvider shootTargetProvider)
        {
            this.input = input;
            this.shootTargetProvider = shootTargetProvider;
            Enabled = true;
        }
    }
}