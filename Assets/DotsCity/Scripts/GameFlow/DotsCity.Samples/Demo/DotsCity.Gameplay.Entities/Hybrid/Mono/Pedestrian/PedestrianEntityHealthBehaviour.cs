using Spirit604.DotsCity.Simulation.Npc;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public class PedestrianEntityHealthBehaviour : NpcEntityHealthBehaviour
    {
        protected override void ActivateDeathVFX()
        {
            base.ActivateDeathVFX();
            entityManager.SetComponentEnabled<RagdollActivateEventTag>(npcEntity.RelatedEntity, true);
        }
    }
}