using Spirit604.DotsCity.Gameplay.Bootstrap;
using Spirit604.Gameplay.Npc;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public class NpcEntityFactory : MonoBehaviour
    {
        private EntityManager entityManager;
        private Dictionary<NpcEntityShapeType, ICustomEntityNpcInitilization> npcInitilizators = new Dictionary<NpcEntityShapeType, ICustomEntityNpcInitilization>();

        private void Awake()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            SceneBootstrap.OnEntityLoaded += SceneBootstrap_OnEntityLoaded;
        }

        private void OnDisable()
        {
            SceneBootstrap.OnEntityLoaded -= SceneBootstrap_OnEntityLoaded;
        }

        public Entity Spawn(NpcEntityShapeType npcType, Transform npcTransform, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            Entity entity = Entity.Null;

            if (npcInitilizators.TryGetValue(npcType, out var initilizator))
            {
                entity = initilizator.Spawn(npcTransform);

                if (spawnPosition != Vector3.zero)
                {
                    entityManager.SetComponentData(entity, LocalTransform.FromPositionRotation(spawnPosition, spawnRotation));
                }
            }

            return entity;
        }

        private void Initialize()
        {
            npcInitilizators.Add(NpcEntityShapeType.Player, new PlayerNpcInitilization(entityManager));
            npcInitilizators.Add(NpcEntityShapeType.Mob, new PlayerMobNpcInitilization(entityManager));
            npcInitilizators.Add(NpcEntityShapeType.Police, new PoliceEntityNpcInitilization(entityManager));
        }

        private void SceneBootstrap_OnEntityLoaded()
        {
            Initialize();
        }
    }
}
