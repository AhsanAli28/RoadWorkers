using Spirit604.DotsCity.Gameplay.Player.Session;
using Spirit604.Gameplay.Player.Session;
using Spirit604.Gameplay.Services;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Level
{
    public class LoadSceneTrigger : IEntityTrigger
    {
        private readonly PlayerSession playerSession;
        private readonly ISceneService sceneService;
        private readonly EntityManager entityManager;

        public LoadSceneTrigger(PlayerSession playerSession, ISceneService sceneService, EntityManager entityManager)
        {
            this.playerSession = playerSession;
            this.sceneService = sceneService;
            this.entityManager = entityManager;
        }

        public void Process(Entity triggerEntity)
        {
            if (entityManager.HasComponent<LoadSceneDataComponent>(triggerEntity))
            {
                playerSession.SaveLastCarData();

                var loadSceneData = entityManager.GetComponentData<LoadSceneDataComponent>(triggerEntity);

                playerSession.CurrentSessionData.CurrentState = SessionState.Free;

                var sceneName = loadSceneData.SceneName.ToString();
                sceneService.LoadScene(sceneName);
            }
        }
    }
}
