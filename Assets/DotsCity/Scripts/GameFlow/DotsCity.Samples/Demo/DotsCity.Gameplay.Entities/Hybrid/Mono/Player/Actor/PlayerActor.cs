using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Gameplay;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Npc;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerActor : MonoBehaviour
    {
        [SerializeField] private PlayerActorType actorType;

        private EntityManager entityManager;
        private IHybridEntityRef hybridEntityRef;

        public CarSlots CarSlots { get; private set; }
        public PlayerNpcCarBehaviour PlayerNpcCarBehaviour { get; private set; }
        public NpcWeaponHolder WeaponHolder { get; private set; }

        public IHealth Health { get; private set; }

        public float MaxSpeed { get; set; }

        public float Speed => Velocity.magnitude;

        public Vector3 Velocity
        {
            get
            {
                if (!hybridEntityRef.HasEntity)
                {
                    return default;
                }

                return entityManager.GetComponentData<VelocityComponent>(hybridEntityRef.RelatedEntity).Value;
            }
        }

        public Entity RelatedEntity => hybridEntityRef.RelatedEntity;

        public PlayerActorType CurrentActorType { get => actorType; set => actorType = value; }

        public bool IsCamera => actorType == PlayerActorType.FreeFly || actorType == PlayerActorType.TrackingCamera;

        private void Awake()
        {
            hybridEntityRef = GetComponent<IHybridEntityRef>();
            CarSlots = GetComponent<CarSlots>();
            PlayerNpcCarBehaviour = GetComponent<PlayerNpcCarBehaviour>();
            Health = GetComponent<IHealth>();
            WeaponHolder = GetComponent<NpcWeaponHolder>();
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }
    }
}
