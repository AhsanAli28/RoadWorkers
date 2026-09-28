using Spirit604.Gameplay.Inventory;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Level.Authoring
{
    public class ItemAuthoring : MonoBehaviour
    {
        [SerializeField] private ItemType itemType;
        [SerializeField] private TriggerType triggerType = TriggerType.Item;
        [SerializeField] private TriggerInteractType interactType = TriggerInteractType.Manual;

        class ItemAuthoringBaker : Baker<ItemAuthoring>
        {
            public override void Bake(ItemAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new ItemComponent() { ItemType = authoring.itemType });
                AddComponent<ItemTakenTag>(entity);

                this.SetComponentEnabled<ItemTakenTag>(entity, false);

                AddComponent(entity, new TriggerComponent()
                {
                    InteractType = authoring.interactType,
                    TriggerType = authoring.triggerType,
                    AvailableByDefault = true
                });
            }
        }
    }
}