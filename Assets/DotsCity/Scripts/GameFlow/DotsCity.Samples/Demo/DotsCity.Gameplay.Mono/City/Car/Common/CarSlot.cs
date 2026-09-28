using Spirit604.Gameplay.Npc;
using System;
using UnityEngine;

namespace Spirit604.Gameplay.Car
{
    public class CarSlot : MonoBehaviour
    {
        private const float XSpawnOffset = 1.2f;

        [SerializeField] private Transform leftFootIK, rightFootIK;
        [SerializeField] private float minRestrictedAngle, maxRestrictedAngle;
        [SerializeField] private int side = 1;
        [field: SerializeField] public bool ShootingSlot { get; set; } = true;

        private INpcInCar npcInCar;
        private GameObject enteredSourceNpc;

        public float MinRestrictedAngle => minRestrictedAngle;

        public float MaxRestrictedAngle => maxRestrictedAngle;

        public int Side => side;

        public Transform LeftFootIK => leftFootIK;

        public Transform RightFootIK => rightFootIK;

        public Vector3 GetSpawnPosition() => transform.position + transform.right * XSpawnOffset * side;

        public Quaternion GetSpawnRotation() => Quaternion.LookRotation(transform.right * Side);

        public Action<CarSlot> OnEnterStateChanged = delegate { };

        public INpcInCar NpcInCar
        {
            get => npcInCar;

            set
            {
                npcInCar = value;

                if (value != null)
                {
                    NpcInCarActor = value.Actor;
                }
                else
                {
                    NpcInCarActor = null;
                }
            }
        }

        public GameObject NpcInCarActor { get; private set; }

        public GameObject EnteredSourceNpc
        {
            get => enteredSourceNpc;
            set
            {
                if (value != null)
                {
                    EnteredSourceNpcParent = value.transform.parent;
                }
                else
                {
                    EnteredSourceNpcParent = null;
                }

                enteredSourceNpc = value;
            }
        }

        public Transform EnteredSourceNpcParent { get; set; }
        public int Index { get; set; }
        public Transform CarParent { get; set; }
        public bool Driver { get; internal set; }
    }
}