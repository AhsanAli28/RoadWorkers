using Spirit604.DotsCity.Gameplay.Player.Authoring;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [DisallowMultipleComponent]
    public class PlayerCarHybridMonoAuthoring : PlayerCarEntityAuthoring
    {
        public override bool CustomID { get => true; set => base.CustomID = value; }

        protected class PlayerCarHybridMonoAuthoringBaker : Baker<PlayerCarHybridMonoAuthoring>
        {
            public override void Bake(PlayerCarHybridMonoAuthoring sourceAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                CarEntityAuthoringBase.CarEntityAuthoringBaseBaker.Bake(this, entity, sourceAuthoring);
                PlayerCarEntityAuthoring.PlayerCarEntityAuthoringBaker.Bake(this, entity);
            }
        }
    }
}