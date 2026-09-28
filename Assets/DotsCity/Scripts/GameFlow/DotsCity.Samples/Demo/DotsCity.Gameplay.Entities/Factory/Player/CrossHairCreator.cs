using Spirit604.DotsCity.Gameplay.Weapon;
using Spirit604.DotsCity.Hybrid.Core;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory
{
    public class CrossHairCreator : MonoBehaviour
    {
        [SerializeField] private CrossHair crossHairPrefab;

        public CrossHair Create()
        {
            CrossHair crossHair = null;

            if (crossHairPrefab)
            {
                crossHair = Instantiate(crossHairPrefab, transform);

                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                var entity = entityManager.CreateEntity(
                    typeof(LocalToWorld),
                    typeof(LocalTransform),
                    typeof(CrossHairComponent),
                    typeof(CrossHairUpdateScaleTag),
                    typeof(CopyTransformToGameObject));

                var initialPos = new Vector3(0, -100, 0);

                entityManager.SetComponentEnabled<CrossHairUpdateScaleTag>(entity, false);
                entityManager.SetComponentData(entity, new CrossHairComponent() { TargetScale = 1, CurrentScale = 1f });
                entityManager.SetComponentData(entity, LocalTransform.FromPosition(initialPos));

                entityManager.AddComponentObject(entity, crossHair.transform);

                crossHair.transform.position = initialPos;
            }

            return crossHair;
        }
    }
}
