#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Pedestrian;
using System.Text;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class PedestrianTalkDebugger : EntityDebuggerBase
    {
        private StringBuilder sb = new StringBuilder();
        private DefaultWorldTimeSystem pedestrianTalkSystem;

        public PedestrianTalkDebugger(EntityManager entityManager) : base(entityManager)
        {
            pedestrianTalkSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<DefaultWorldTimeSystem>();
        }

        public override bool ShouldDraw(Entity entity)
        {
            return false;
        }

        protected override bool ShouldTick(Entity entity)
        {
            return EntityManager.HasComponent(entity, typeof(TalkComponent)) && base.ShouldTick(entity);
        }

        public override StringBuilder GetDescriptionText(Entity entity)
        {
            var talkComponent = EntityManager.GetComponentData<TalkComponent>(entity);
            var position = EntityManager.GetComponentData<LocalToWorld>(entity).Position;

            Vector3 pos = position + new Unity.Mathematics.float3(0, 1f, 0f);

            var currentTime = pedestrianTalkSystem.CurrentTime;
            var remain = talkComponent.StopTalkingTime - currentTime;

            remain = (float)System.Math.Round(remain, 1);

            sb.Clear();
            sb.Append("Remain time: ");

            if (remain < 99999)
            {
                sb.Append((remain)).Append("\n");
            }
            else
            {
                sb.Append("unlimited \n");
            }

            return sb;
        }
    }
}
#endif