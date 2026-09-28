using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Gameplay;
using Spirit604.Gameplay.Config.Player;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Inventory;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Player;
using Spirit604.Gameplay.Player.Session;
using Spirit604.Gameplay.Weapons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Session
{
    public partial class PlayerSession : MonoBehaviour
    {
        public const int START_RANDOM_MATE_COUNT = 3;

        private const int InitialPlayerHealth = 9999;
        private const int InitialBandHealth = 4;
        private readonly WeaponType[] InitialPlayerWeapons = new WeaponType[] { WeaponType.Revolver, WeaponType.TommyGun };

        [SerializeField] private GlobalSessionPlayerData lastSavedSessionData;
        [SerializeField] private GlobalSessionPlayerData currentSessionData;

        private bool isInitialized;

        public GlobalSessionPlayerData CurrentSessionData => currentSessionData;

        public Entity LastCar { get; set; }

        public event Action<NpcBehaviourBase> OnNpcUnlinked = delegate { };
        public event Action<NpcBehaviourBase> OnNpcLinked = delegate { };

        public void SetInitialData(GlobalSessionPlayerData playerData)
        {
            currentSessionData = playerData;
            lastSavedSessionData = DeepCopy(currentSessionData);
            ResetNpcBinding();
        }

        public void InitSpawnConfig(PlayerSpawnDataConfig playerSpawnDataConfig, WeaponFactory weaponFactory, bool force = false)
        {
            if (isInitialized && !force)
            {
                UnityEngine.Debug.Log("PlayerSession. Attempt to initialize twice.");
                return;
            }

            isInitialized = true;
            var data = new GlobalSessionPlayerData();
            data.WeaponIsHided = true;
            data.TotalCharaterData = new List<CharacterSessionData>();

            CharacterSessionData playerData = new CharacterSessionData();

            var playerCharacterData = playerSpawnDataConfig.PlayerCharacterData;
            List<ItemBase> playerWeaponData = new List<ItemBase>();

            if (playerCharacterData != null)
            {
                playerData.CurrentHealth = playerCharacterData.CurrentHealth;
                playerData.CurrentSelectedWeapon = playerCharacterData.CurrentSelectedWeapon;
                playerData.NpcId = playerCharacterData.NpcIdValue;
                playerWeaponData = GetWeaponData(playerCharacterData);
            }
            else
            {
                playerData.CurrentHealth = InitialPlayerHealth;
                playerData.CurrentSelectedWeapon = WeaponType.Revolver;

                var selectedNpcID = playerSpawnDataConfig.SelectedNpcID;
                playerData.NpcId = selectedNpcID;

                foreach (var weapon in InitialPlayerWeapons)
                {
                    int ammo = weaponFactory.GetMaxAmmoWeapon(weapon);

                    var weaponData = new WeaponData(ammo);

                    playerWeaponData.Add(new WeaponItem(weapon, weaponData)
                    {
                        ItemType = ItemType.Weapon,
                        ItemId = weapon.ToString(),
                    });
                }
            }

            playerData.WeaponIsHided = true;
            TryToAddItems(playerData, playerWeaponData);

            data.TotalCharaterData.Add(playerData);
            data.CurrentSelectedPlayer = playerData;

            if (playerSpawnDataConfig.HasCharacterData)
            {
                var characterDatas = playerSpawnDataConfig.CharacterDatas;

                for (int i = 0; i < characterDatas.Count; i++)
                {
                    var characterData = characterDatas[i];

                    if (characterData.IsPlayer)
                    {
                        continue;
                    }

                    var aiCharacterData = new CharacterSessionData()
                    {
                        CurrentHealth = characterData.CurrentHealth,
                        CurrentSelectedWeapon = characterData.CurrentSelectedWeapon,
                        NpcId = characterData.NpcIdValue,
                        WeaponIsHided = true
                    };

                    var characterWeaponData = GetWeaponData(characterData);

                    TryToAddItems(aiCharacterData, characterWeaponData);
                    data.TotalCharaterData.Add(aiCharacterData);
                }
            }
            else
            {
                for (int i = 0; i < playerSpawnDataConfig.BandSize - 1; i++)
                {
                    CharacterSessionData aiMateData = new CharacterSessionData();

                    aiMateData.CurrentHealth = InitialBandHealth;
                    aiMateData.WeaponIsHided = true;

                    WeaponType randomWeaponType = WeaponFactory.GetRandomWeaponType();

                    aiMateData.CurrentSelectedWeapon = randomWeaponType;

                    int maxAmmo = weaponFactory.GetMaxAmmoWeapon(randomWeaponType);

                    WeaponData weaponData = new WeaponData(maxAmmo);

                    List<ItemBase> aiMateWeaponData = new List<ItemBase>();

                    aiMateWeaponData.Add(new WeaponItem(randomWeaponType, weaponData)
                    {
                        ItemType = ItemType.Weapon,
                        ItemId = randomWeaponType.ToString(),
                    });

                    TryToAddItems(aiMateData, aiMateWeaponData);
                    data.TotalCharaterData.Add(aiMateData);
                }
            }

            if (playerSpawnDataConfig.PlayerCarSpawnData != null && playerSpawnDataConfig.PlayerCarSpawnData.CurrentHealth > 0)
            {
                data.CarData = new CarData()
                {
                    HasData = true,
                    CarModel = playerSpawnDataConfig.SelectedCarModel,
                    CurrentHealth = playerSpawnDataConfig.PlayerCarSpawnData.CurrentHealth,
                };
            }
            else if (playerSpawnDataConfig.CurrentSpawnPlayerType == PlayerSpawnDataConfig.SpawnPlayerType.Car)
            {
                data.CarData = new CarData()
                {
                    HasData = true,
                    CarModel = playerSpawnDataConfig.SelectedCarModel,
                    CurrentHealth = InitialPlayerHealth,
                };
            }

            SetInitialData(data);
        }

        public bool SaveNpcData(NpcBehaviourBase targetNpc)
        {
            var characterSessionData = GetSessionByNpc(targetNpc);

            if (characterSessionData != null)
            {
                if (targetNpc.CurrentHealth > 0)
                    characterSessionData.CurrentHealth = targetNpc.CurrentHealth;

                characterSessionData.WeaponIsHided = targetNpc.WeaponHolder.IsHided;

                var weapon = targetNpc.WeaponHolder.CurrentWeapon;

                if (weapon != null)
                {
                    var weaponItem = TryToGetItem(characterSessionData, weapon.WeaponType) as WeaponItem;

                    if (weaponItem != null)
                    {
                        characterSessionData.CurrentSelectedWeapon = weapon.WeaponType;
                        weaponItem.WeaponData.Ammo = weapon.CurrentAmmo;
                    }
                }
                else
                {
                    characterSessionData.CurrentSelectedWeapon = WeaponType.Default;
                }

                return true;
            }

            return false;
        }

        public bool TryToAddItem(NpcBehaviourBase targetNpc, ItemBase item)
        {
            var characterSessionData = GetSessionByNpc(targetNpc);

            if (characterSessionData != null)
            {
                return TryToAddItem(characterSessionData, item);
            }

            return false;
        }

        public bool TryToAddItems(CharacterSessionData characterSessionData, List<ItemBase> items)
        {
            if (characterSessionData != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    TryToAddItem(characterSessionData, items[i]);
                }

                return true;
            }

            return false;
        }

        public bool TryToAddItem(CharacterSessionData characterSessionData, ItemBase item)
        {
            if (characterSessionData != null)
            {
                bool canAdd = true;

                if (!item.ItemConsumeType.HasFlag(ItemSettingFlags.AllowDuplicate))
                {
                    var oldItem = TryToGetItem(characterSessionData, item.ItemId);

                    if (oldItem != null)
                    {
                        canAdd = false;
                    }
                }

                if (canAdd)
                {
                    characterSessionData.Items.Add(item);
                    return true;
                }
            }

            return false;
        }

        public ItemBase TryToGetItem(NpcBehaviourBase targetNpc, object itemId)
        {
            var characterSessionData = GetSessionByNpc(targetNpc);

            if (characterSessionData != null)
            {
                TryToGetItem(characterSessionData, itemId);
            }

            return null;
        }

        public ItemBase TryToGetItem(CharacterSessionData characterSessionData, object itemId)
        {
            if (characterSessionData != null)
            {
                var itemIdStr = itemId.ToString();
                var item = characterSessionData.Items.Where(a => a.ItemId == itemIdStr).FirstOrDefault();
                return item;
            }

            return null;
        }

        public T TryToGetItem<T>(CharacterSessionData characterSessionData, object itemId) where T : ItemBase
        {
            if (characterSessionData != null)
            {
                var item = TryToGetItem(characterSessionData, itemId);

                if (item != null)
                {
                    return item as T;
                }
            }

            return null;
        }

        public IEnumerable<T> TryToGetItems<T>(CharacterSessionData characterSessionData, ItemType itemType) where T : ItemBase
        {
            if (characterSessionData != null)
            {
                return characterSessionData.Items.Where(a => a.ItemType == itemType).Cast<T>();
            }

            return null;
        }

        public T TryToGetItem<T>(CharacterSessionData characterSessionData, ItemType itemType, int index) where T : ItemBase
        {
            var items = TryToGetItems<T>(characterSessionData, itemType);

            if (items != null)
            {
                int counter = 0;

                foreach (var item in items)
                {
                    if (counter == index)
                    {
                        return item;
                    }

                    counter++;
                }
            }

            return null;
        }

        public bool TryToSelectItem<T>(CharacterSessionData characterSessionData, object itemId) where T : ItemBase
        {
            if (characterSessionData != null)
            {
                var item = TryToGetItem(characterSessionData, itemId);

                if (item != null)
                {
                    var itemObj = item as T;

                    switch (itemObj.ItemType)
                    {
                        case ItemType.Default:
                            break;
                        case ItemType.Weapon:
                            {
                                var weaponType = (itemObj as WeaponItem).WeaponType;
                                TryToSelectWeapon(characterSessionData, weaponType);
                                return true;
                            }
                    }
                }
            }

            return false;
        }

        public bool TryToSelectWeapon(CharacterSessionData characterSessionData, WeaponType weaponType)
        {
            var npc = characterSessionData.WorldNpcRef;

            npc?.WeaponHolder?.SelectWeapon(weaponType);

            return true;
        }

        public static List<ItemBase> GetWeaponData(BandCharacterSpawnData characterSpawnData)
        {
            List<ItemBase> characterWeaponData = new List<ItemBase>();

            foreach (var item in characterSpawnData.WeaponData)
            {
                var weaponData = new WeaponData(item.Value.Ammo);

                characterWeaponData.Add(new WeaponItem(item.Key, weaponData)
                {
                    ItemType = ItemType.Weapon,
                    ItemId = item.Key.ToString(),
                });
            }

            return characterWeaponData;
        }

        public void UpdateLinkNpc(NpcBehaviourBase sourceNpc, NpcBehaviourBase newNpc)
        {
            SaveNpcData(sourceNpc);

            var session = GetSessionByNpc(sourceNpc);

            if (session != null)
            {
                OnNpcUnlinked(sourceNpc);
                LinkNpc(newNpc, session);
            }
        }

        public void LinkNpc(NpcBehaviourBase npc)
        {
            var playerData = GetUnlinkedPlayerData();

            if (playerData != null)
            {
                LinkNpc(npc, playerData);
            }
        }

        public void LinkNpc(NpcBehaviourBase npc, CharacterSessionData characterSessionData)
        {
            OnNpcLinked(npc);

            if (npc.TryGetComponent<IHealth>(out var health))
            {
                health.Initialize(characterSessionData.CurrentHealth);
            }

            var npcWeaponHolder = npc.WeaponHolder;

            npcWeaponHolder.IsHided = characterSessionData.WeaponIsHided;

            var items = TryToGetItems<WeaponItem>(characterSessionData, ItemType.Weapon);
            var currentSelectedWeapon = characterSessionData.CurrentSelectedWeapon;

            if (items?.Count() > 0)
            {
                var availableWeaponTypes = items.Select(a => a.WeaponType).ToList();

                var currentWeapon = npc.WeaponHolder.InitializeWeapon(currentSelectedWeapon, availableWeaponTypes);

                if (currentWeapon != null)
                {
                    WeaponItem weaponItem = null;

                    foreach (var item in items)
                    {
                        if (item.WeaponType == currentSelectedWeapon)
                        {
                            weaponItem = item;
                            break;
                        }
                    }

                    currentWeapon.Initialize(weaponItem.WeaponData.Ammo);
                }
            }

            characterSessionData.WorldNpcRef = npc;
        }

        public CharacterSessionData GetUnlinkedPlayerData()
        {
            CharacterSessionData playerData = null;

            for (int i = 1; i < currentSessionData.TotalCharaterData.Count; i++)
            {
                if (CurrentSessionData.TotalCharaterData[i].WorldNpcRef == null)
                {
                    playerData = CurrentSessionData.TotalCharaterData[i];
                    break;
                }
            }

            return playerData;
        }

        public CharacterSessionData GetSessionByNpc(NpcBehaviourBase targetNpc)
        {
            return currentSessionData.TotalCharaterData.Where(a => a.WorldNpcRef == targetNpc).FirstOrDefault();
        }

        public void SwitchWeaponHidedState()
        {
            var state = !currentSessionData.WeaponIsHided;
            SwitchWeaponHidedState(state);
        }

        public void SwitchWeaponHidedState(bool isHided)
        {
            currentSessionData.WeaponIsHided = isHided;

            for (int i = 0; i < currentSessionData.TotalCharaterData?.Count; i++)
            {
                var sessionData = currentSessionData.TotalCharaterData[i];
                var worldNpcRef = sessionData.WorldNpcRef;

                if (worldNpcRef != null)
                {
                    worldNpcRef.WeaponHolder.IsHided = currentSessionData.WeaponIsHided;
                }
            }
        }

        public void SaveSessionState()
        {
            lastSavedSessionData = DeepCopy(currentSessionData);
        }

        public void ResetSessionState()
        {
            currentSessionData = DeepCopy(lastSavedSessionData);

            ResetNpcBinding();
        }

        public void SaveLastCarData()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            if (LastCar != Entity.Null && entityManager.Exists(LastCar))
            {
                int health = entityManager.GetComponentData<HealthComponent>(LastCar).Value;
                var carModel = entityManager.GetComponentData<CarModelComponent>(LastCar).Value;
                var transform = entityManager.GetComponentData<LocalToWorld>(LastCar);

                if (health > 0)
                {
                    currentSessionData.CarData = new CarData()
                    {
                        HasData = true,
                        CarModel = carModel,
                        CurrentHealth = health,
                        Position = (Vector3)transform.Position,
                        Rotation = (Quaternion)transform.Rotation,
                    };
                }
                else
                {
                    currentSessionData.CarData = null;
                }
            }
            else
            {
                currentSessionData.CarData = null;
            }
        }

        private void ResetNpcBinding()
        {
            var characterData = currentSessionData.TotalCharaterData;

            foreach (var charaterSessionData in characterData)
            {
                charaterSessionData.WorldNpcRef = null;
            }
        }

        private T DeepCopy<T>(T other)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, other);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }
    }
}
