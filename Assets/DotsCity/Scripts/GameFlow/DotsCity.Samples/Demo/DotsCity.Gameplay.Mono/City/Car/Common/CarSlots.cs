using Spirit604.Gameplay.Factory;
using System;
using UnityEngine;

namespace Spirit604.Gameplay.Car
{
    public class CarSlots : MonoBehaviour, ICarSlots
    {
        [SerializeField] private CarSlot driverSlot;
        [SerializeField] private CarSlot[] carSlots;

        private INpcInteractCarService npcInteractCarService;

        public bool HasDriver => driverSlot.NpcInCar != null;

        public int SlotCount => carSlots.Length;

        public CarSlot DriverSlot => driverSlot;

        public bool HasSlots => TakenSeatsCount < carSlots.Length;

        public int TakenSeatsCount { get; private set; }

        private void Awake()
        {
            InitSlotsInternal();
        }

        private void OnDisable()
        {
            for (int i = 0; i < carSlots?.Length; i++)
            {
                if (carSlots[i].NpcInCar != null)
                {
                    carSlots[i].NpcInCar.Dispose();
                    carSlots[i].NpcInCar = null;
                }
            }

            TakenSeatsCount = 0;
        }

        public void Initialize(INpcInteractCarService npcIteractCarService)
        {
            this.npcInteractCarService = npcIteractCarService;
        }

        public CarSlot EnterCar(string npcId, GameObject enteredNpc = null, bool driver = false, Action<CarSlot> onNpcEntered = null)
        {
            CarSlot emptyCarSlot = null;

            if (!driver)
            {
                if (HasSlots)
                {
                    emptyCarSlot = GetEmptySlot();
                }
            }
            else
            {
                if (!HasDriver)
                {
                    emptyCarSlot = driverSlot;
                }
            }

            if (emptyCarSlot != null)
            {
                if (npcInteractCarService.Enter(emptyCarSlot, npcId, enteredNpc, driver, onNpcEntered) != null)
                {
                    TakenSeatsCount++;

                    if (emptyCarSlot.NpcInCar == null)
                    {
                        onNpcEntered?.Invoke(emptyCarSlot);
                    }

                    return emptyCarSlot;
                }
            }

            return null;
        }

        public void ExitCarAll(bool includeDrive = false, Action<GameObject, bool> onExit = null)
        {
            if (npcInteractCarService == null)
            {
                Debug.Log($"CarSlots {name} NpcInteractCarService not found");
            }

            for (int i = 0; i < carSlots.Length; i++)
            {
                if (carSlots[i].NpcInCar == null)
                    continue;

                bool isDriver = carSlots[i] == driverSlot;

                if (!includeDrive && isDriver)
                    continue;

                ExitSlot(carSlots[i], onExit);
            }
        }

        public void ExitSlot(CarSlot carSlot, Action<GameObject, bool> onExit = null)
        {
            if (npcInteractCarService == null)
            {
                Debug.LogError($"CarSlots {name} NpcInteractCarService not found");
                return;
            }

            var sourceNpc = carSlot.NpcInCar;

            Vector3 spawnPosition = carSlot.GetSpawnPosition();
            Quaternion spawnRotation = carSlot.GetSpawnRotation();

            if (onExit != null)
            {
                npcInteractCarService.Exit(carSlot, sourceNpc.ID, spawnPosition, spawnRotation, carSlot.Driver, (npc) =>
                {
                    onExit?.Invoke(npc, carSlot.Driver);
                });
            }
            else
            {
                npcInteractCarService.Exit(carSlot, sourceNpc.ID, spawnPosition, spawnRotation, carSlot.Driver);
            }

            carSlot.NpcInCar.Dispose();
            carSlot.NpcInCar = null;
            carSlot.EnteredSourceNpc = null;
            TakenSeatsCount--;
        }

        public bool GetSlotSpawnTransform(int slotIndex, out Vector3 spawnPosition, out Quaternion spawnRotation)
        {
            spawnPosition = default;
            spawnRotation = default;

            var slot = GetSlot(slotIndex);

            if (slot != null)
            {
                spawnPosition = slot.GetSpawnPosition();
                spawnRotation = slot.GetSpawnRotation();
                return true;
            }

            return false;
        }

        public GameObject ExitDriver(Action<GameObject, bool> onExit = null)
        {
            if (driverSlot.NpcInCar != null)
            {
                ExitSlot(driverSlot, onExit);
                return driverSlot.EnteredSourceNpc;
            }

            return null;
        }

        public CarSlot GetSlot(int index)
        {
            if (index >= 0 && index < carSlots.Length)
            {
                return carSlots[index];
            }

            return null;
        }

        public void InitSlots()
        {
            carSlots = GetComponentsInChildren<CarSlot>();
            driverSlot = carSlots[0];
            InitSlotsInternal();
        }

        public void GenerateSlot(bool driver)
        {
#if UNITY_EDITOR
            if (driver)
            {
                if (driverSlot == null)
                {
                    driverSlot = CreateSlot();
                    Extensions.EditorSaver.SetObjectDirty(this);
                }
            }
            else
            {

            }
#endif
        }

        private void InitSlotsInternal()
        {
            if (carSlots == null) return;

            for (int i = 0; i < SlotCount; i++)
            {
                carSlots[i].CarParent = transform;
                carSlots[i].Index = i;
                carSlots[i].Driver = i == 0;
            }
        }

#if UNITY_EDITOR

        private CarSlot CreateSlot()
        {
            var slot = new GameObject("CarSlot").AddComponent<CarSlot>();

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(slot.gameObject, transform.gameObject.scene);
            slot.transform.SetParent(transform.parent);
            slot.transform.localPosition = default;
            slot.transform.localRotation = Quaternion.identity;

            return slot;
        }

#endif

        private CarSlot GetEmptySlot()
        {
            CarSlot carSlot = null;

            for (int i = 0; i < carSlots.Length; i++)
            {
                if (carSlots[i].NpcInCar == null && carSlots[i] != driverSlot)
                {
                    carSlot = carSlots[i];
                    break;
                }
            }

            return carSlot;
        }
    }
}