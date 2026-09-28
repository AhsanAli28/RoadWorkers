using Spirit604.Attributes;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Level.Authoring
{
    public abstract class TriggerAuthoringBase : MonoBehaviourBase
    {
        [SerializeField] private TriggerType triggerType;
        [SerializeField] private TriggerInteractType triggerInteract = TriggerInteractType.Manual;
        [SerializeField] private bool availablebyDefault;
        [SerializeField] private bool hasScene;
        [ShowIf(nameof(hasScene))]
        [SerializeField] private string sceneName;

        public void Bake(IBaker baker, TriggerAuthoringBase authoring)
        {
            var entity = baker.GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            baker.AddComponent(entity,
                new TriggerComponent()
                {
                    InteractType = authoring.triggerInteract,
                    AvailableByDefault = authoring.availablebyDefault,
                    TriggerType = authoring.triggerType,
                    IsClosed = !authoring.availablebyDefault
                });

            if (authoring.hasScene && !string.IsNullOrEmpty(authoring.sceneName))
            {
                baker.AddComponent(entity,
                    new LoadSceneDataComponent()
                    {
                        SceneName = authoring.sceneName
                    });
            }
        }
    }
}