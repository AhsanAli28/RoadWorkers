using Spirit604.Attributes;
using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Level.Authoring
{
    public class TriggerAuthoring : TriggerAuthoringBase
    {
        private const float DefaultTriggerSize = 2.5f;

        class TriggerAuthoringBaker : Baker<TriggerAuthoring>
        {
            public override void Bake(TriggerAuthoring authoring)
            {
                authoring.Bake(this, authoring);
            }
        }

        [Button]
        private void AddPhysicsShape()
        {
            var physicsShape = GetComponent<PhysicsShapeAuthoring>();

            if (!physicsShape)
            {
                physicsShape = gameObject.AddComponent<PhysicsShapeAuthoring>();

                physicsShape.SetBox(new Unity.Physics.BoxGeometry()
                {
                    Size = Vector3.one * DefaultTriggerSize
                });
            }

            physicsShape.CollisionResponse = Unity.Physics.CollisionResponsePolicy.RaiseTriggerEvents;

            physicsShape.BelongsTo = new PhysicsCategoryTags()
            {
                Value = ProjectConstants.TRIGGER_LAYER_VALUE
            };

            physicsShape.CollidesWith = new PhysicsCategoryTags()
            {
                Value = ProjectConstants.PLAYER_LAYER_VALUE
            };
        }
    }
}