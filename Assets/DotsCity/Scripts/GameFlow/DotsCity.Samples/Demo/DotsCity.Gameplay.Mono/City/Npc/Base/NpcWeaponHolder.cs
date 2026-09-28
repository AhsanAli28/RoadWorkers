using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Sound;
using Spirit604.Extensions;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Weapons;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.Gameplay.Npc
{
    public class NpcWeaponHolder : MonoBehaviour, IFactionProvider
    {
        private const string HANDS_IN_POSE_KEY = "HandsInPosition";

        private readonly int HANDS_IN_POSE_KEY_HASH = Animator.StringToHash(HANDS_IN_POSE_KEY);

        [SerializeField] private Transform weaponAnchor;
        [SerializeField] private FactionType factionType;

        [SerializeField] private List<WeaponType> defaultStartWeapons = new List<WeaponType>();

        private bool isHided;

        private WeaponFactory weaponFactory;
        private IBulletFactory bulletFactory;
        private ISoundPlayer soundPlayer;

        private List<WeaponType> availableWeaponTypes = new List<WeaponType>(); // Should replaced by inventory

        public Weapon CurrentWeapon { get; private set; }
        public float CurrentAnimatorWeaponID { get; private set; }

        public bool IsHided
        {
            get
            {
                return isHided;
            }

            set
            {
                if (isHided != value)
                {
                    isHided = value;

                    SwitchVisibleState(!isHided);
                    OnSwitchHideState(this, isHided);
                }
            }
        }

        public FactionType FactionType
        {
            get => factionType;
            set
            {
                factionType = value;

                if (CurrentWeapon)
                {
                    CurrentWeapon.FactionType = factionType;
                }
            }
        }

        public NpcBehaviourBase NpcBase { get; private set; }
        public List<WeaponType> AvailableWeaponTypes { get => availableWeaponTypes; }
        public bool ReleaseWeaponOnDisable { get; set; } = true;

        public event Action<NpcWeaponHolder, bool> OnSwitchHideState = delegate { };
        public event Action<NpcWeaponHolder, WeaponType> OnSelectWeapon = delegate { };

        private void Awake()
        {
            NpcBase = GetComponent<NpcBehaviourBase>();

            if (weaponAnchor == null)
            {
                UnityEngine.Debug.LogError($"NpcWeaponHolder {name}. Anchor is null");
            }
        }

        private void OnEnable()
        {
            if (weaponFactory && defaultStartWeapons.Count > 0)
            {
                InitializeWeapon(defaultStartWeapons[0], defaultStartWeapons);
            }
        }

        private void OnDisable()
        {
            if (ReleaseWeaponOnDisable)
            {
                ReturnWeapons();
            }
        }

        public Weapon SelectWeapon(WeaponType weaponType)
        {
            ReturnCurrentWeapon();

            CurrentWeapon = weaponFactory.Get(weaponType);

            if (CurrentWeapon != null)
            {
                CurrentWeapon.OnRealoadRequested += Weapon_OnRealoadRequested;
                CurrentWeapon.transform.SetParent(weaponAnchor);
                CurrentWeapon.transform.localPosition = Vector3.zero;
                CurrentWeapon.transform.localRotation = Quaternion.identity;
                CurrentWeapon.Initialize(gameObject, bulletFactory, soundPlayer, factionType);

                SwitchVisibleState(!IsHided);

                if (!IsHided)
                {
                    AnimatorSetHandsIsHoldingWeapon(true);
                }
            }
            else
            {
                IsHided = true;
                AnimatorSetHandsIsHoldingWeapon(false);

                if (weaponType != WeaponType.Default)
                {
                    UnityEngine.Debug.Log($"NpcWeaponHolder. {name} Weapon not found {weaponType}");
                }
            }

            OnSelectWeapon(this, weaponType);

            return CurrentWeapon;
        }

        public void SwitchVisibleState(bool isActive)
        {
            if (NpcBase.LocationType == NpcLocationType.InCar)
            {
                isActive = true;
            }

            CurrentAnimatorWeaponID = -1;

            if (CurrentWeapon != null)
            {
                CurrentWeapon.gameObject.SetActive(isActive);

                if (isActive)
                {
                    CurrentAnimatorWeaponID = WeaponAnimatorID.GetID(CurrentWeapon.WeaponType);
                }
            }

            AnimatorSetHandsIsHoldingWeapon(isActive);
            NpcBase.SetAnimatorWeaponIndex();
        }

        public void AnimatorSetHandsIsHoldingWeapon(bool isActive)
        {
            NpcBase.Animator?.SetBool(HANDS_IN_POSE_KEY_HASH, isActive);
        }

        public void Initialize(WeaponFactory weaponFactory, IBulletFactory bulletFactory, ISoundPlayer soundPlayer)
        {
            this.weaponFactory = weaponFactory;
            this.bulletFactory = bulletFactory;
            this.soundPlayer = soundPlayer;

            if (defaultStartWeapons.Count > 0 && gameObject.activeSelf)
            {
                InitializeWeapon(defaultStartWeapons[0], defaultStartWeapons);
            }
        }

        public Weapon InitializeWeapon(WeaponType selectedWeaponType, List<WeaponType> weaponTypes)
        {
            for (int i = 0; i < weaponTypes.Count; i++)
            {
                var currentType = weaponTypes[i];

                if (availableWeaponTypes.TryToAdd(currentType))
                {

                }
            }

            return SelectWeapon(selectedWeaponType);
        }

        public Weapon InitializeWeapon(WeaponType selectedWeaponType)
        {
            availableWeaponTypes.TryToAdd(selectedWeaponType);
            return SelectWeapon(selectedWeaponType);
        }

        public void ReturnWeapons()
        {
            ReturnCurrentWeapon();

            availableWeaponTypes.Clear();
        }

        public void CompleteRealod()
        {
            CurrentWeapon?.Realod();
        }

        private void Weapon_OnRealoadRequested()
        {
            CurrentWeapon.StartRealod();
            NpcBase.StartRealod();
        }

        private void ReturnCurrentWeapon()
        {
            if (CurrentWeapon != null)
            {
                CurrentWeapon.OnRealoadRequested -= Weapon_OnRealoadRequested;
                CurrentWeapon.gameObject.ReturnToPool();
                CurrentWeapon = null;
            }
        }
    }
}
