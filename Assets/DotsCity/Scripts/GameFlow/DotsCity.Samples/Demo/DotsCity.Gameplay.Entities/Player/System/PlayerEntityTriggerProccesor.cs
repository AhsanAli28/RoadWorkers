using Spirit604.DotsCity.Gameplay.Level;
using Spirit604.DotsCity.Gameplay.Player.Session;
using Spirit604.Gameplay.Services;
using Spirit604.Gameplay.UI;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerEntityTriggerProccesor : IPlayerEntityTriggerProccesor
    {
        private const string ENTER_TEXT = "Enter";
        private const string TAKE_TEXT = "Take";

        private Dictionary<TriggerType, IEntityTrigger> triggers = new Dictionary<TriggerType, IEntityTrigger>();
        private Entity lastTriggerEntity;
        private EntityManager entityManager;

        private PlayerInteractTriggerPresenter playerInteractTriggerPresenter;
        private PlayerSession playerSession;
        private ISceneService sceneService;

        public PlayerEntityTriggerProccesor(PlayerInteractTriggerPresenter playerInteractTriggerPresenter, PlayerSession playerSession, ISceneService sceneService)
        {
            this.playerInteractTriggerPresenter = playerInteractTriggerPresenter;
            this.playerSession = playerSession;
            this.sceneService = sceneService;

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            triggers.Add(TriggerType.LoadScene, new LoadSceneTrigger(playerSession, sceneService, entityManager));
            triggers.Add(TriggerType.Item, new ItemTrigger(entityManager));
        }

        public bool TriggerIsBlocked { get; set; }

        public void ProcessTrigger(Entity triggerEntity)
        {
            if (lastTriggerEntity == triggerEntity)
            {
                return;
            }

            lastTriggerEntity = triggerEntity;

            if (!entityManager.HasComponent<TriggerComponent>(triggerEntity))
            {
                return;
            }

            TriggerComponent triggerComponent = entityManager.GetComponentData<TriggerComponent>(triggerEntity);
            var triggerPosition = entityManager.GetComponentData<LocalToWorld>(triggerEntity).Position;

            playerSession.CurrentSessionData.SpawnPosition = (UnityEngine.Vector3)triggerPosition;

            bool isAvailable = triggerComponent.AvailableByDefault || !triggerComponent.IsClosed;

            if (isAvailable)
            {
                if (triggerComponent.InteractType == TriggerInteractType.Auto)
                {
                    ProcessTrigger(triggerEntity, triggerComponent.TriggerType);
                }
                else
                {
                    System.Action processAction = () => ProcessTrigger(triggerEntity, triggerComponent.TriggerType);

                    string text = triggerComponent.TriggerType != TriggerType.Item ? ENTER_TEXT : TAKE_TEXT;

                    playerInteractTriggerPresenter.SetWorldButton(triggerPosition, processAction, text);
                }
            }
        }

        public void ProcessExitTrigger()
        {
            lastTriggerEntity = Entity.Null;
            playerInteractTriggerPresenter.SwitchWorldButtonState(false);
        }

        private void ProcessTrigger(Entity triggerEntity, TriggerType triggerType)
        {
            if (TriggerIsBlocked)
            {
                TriggerIsBlocked = false;
                return;
            }

            if (triggers.TryGetValue(triggerType, out IEntityTrigger trigger))
            {
                trigger.Process(triggerEntity);
            }
        }
    }
}
