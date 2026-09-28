using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Level.Authoring
{
    [RequireComponent(typeof(RuntimeEntityAuthoring))]
    public class TriggerRuntimeAuthoring : MonoBehaviour, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        [SerializeField] private TriggerType triggerType;
        [SerializeField] private TriggerInteractType triggerInteract = TriggerInteractType.Manual;
        [SerializeField] private bool availablebyDefault;
        [SerializeField] private bool hasScene;

        [ShowIf(nameof(hasScene))]
        [SerializeField] private string sceneName;

        ComponentType[] IRuntimeEntityComponentSetProvider.GetComponentSet()
        {
            return new ComponentType[] { ComponentType.ReadOnly<TriggerComponent>(), ComponentType.ReadOnly<LoadSceneDataComponent>() };
        }

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentData(entity,
                new TriggerComponent()
                {
                    InteractType = triggerInteract,
                    AvailableByDefault = availablebyDefault,
                    TriggerType = triggerType,
                    IsClosed = !availablebyDefault
                });

            entityManager.SetComponentData(entity,
                new LoadSceneDataComponent()
                {
                    SceneName = sceneName
                });
        }
    }
}