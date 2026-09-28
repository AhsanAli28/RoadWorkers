using Spirit604.Extensions;
using Spirit604.Gameplay.Weapons;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spirit604.Gameplay.Npc
{
    [RequireComponent(typeof(NpcWeaponHolder), typeof(Animator))]
    public class NpcBehaviourBase : MonoBehaviour, INpcIDProvider
    {
        public const string SHOOT_ANIMATOR_AIMING_KEY = "IsAiming";
        private const string SHOOT_ANIMATOR_SHOOTING_KEY = "IsShooting";
        private const string WEAPON_ID_KEY = "WeaponID";
        private const string WEAPON_REALODING_KEY = "IsRealoding";

        [FormerlySerializedAs("mateType")]
        [SerializeField] private NpcLocationType locationType;

        [FormerlySerializedAs("mateName")]
        [SerializeField] private NpcId id;

        private Vector3 shootDirection;
        private bool shootTriggered;
        protected Animator animator;

        protected int shootAnimatorAimingKeyId = Animator.StringToHash(SHOOT_ANIMATOR_AIMING_KEY);
        protected int shootAnimatorShootingKeyId = Animator.StringToHash(SHOOT_ANIMATOR_SHOOTING_KEY);
        protected int weaponIdKeyId = Animator.StringToHash(WEAPON_ID_KEY);
        protected int weaponRealodingId = Animator.StringToHash(WEAPON_REALODING_KEY);

        public virtual bool CanShoot => WeaponHolder.CurrentWeapon != null && WeaponHolder.CurrentWeapon.CanShoot;

        public NpcLocationType LocationType => locationType;

        public NpcWeaponHolder WeaponHolder { get; private set; }

        public Animator Animator => animator;

        public bool CanControl { get; set; }

        public int CurrentHealth => EntityHealth.CurrentHealth;

        public bool IsAlive => CurrentHealth > 0;

        public float ReducationFactor
        {
            get
            {
                if (CurrentWeapon)
                {
                    return CurrentWeapon.SpeedPenaltyMultiplier;
                }

                return 1;
            }
        }

        public string ID => id.ToString();

        protected bool IsShooting { get; set; }

        protected IHealth EntityHealth { get; set; }

        protected virtual bool ShouldAimingAlways => true;

        private Weapon CurrentWeapon => WeaponHolder.CurrentWeapon;

        protected virtual void Awake()
        {
            WeaponHolder = GetComponent<NpcWeaponHolder>();
            WeaponHolder.OnSelectWeapon += WeaponHolder_OnSelectWeapon;
            animator = GetComponent<Animator>();
            EntityHealth = GetComponent<IHealth>();
        }

        protected virtual void FixedUpdate()
        {
            TryToShoot();
        }

        public void Shoot(Vector3 shootDirection)
        {
            IsShooting = shootDirection != Vector3.zero;

            if (IsShooting)
            {
                this.shootDirection = shootDirection;
                transform.rotation = Quaternion.LookRotation(shootDirection.Flat(), Vector3.up);
            }

            if (CurrentWeapon == null)
            {
                return;
            }

            CurrentWeapon.IsShooting = IsShooting;

            bool isShooting = IsShooting && CanShoot;
            var shootingKey = isShooting && CurrentWeapon.CanPlayAnimation;

            if (isShooting)
            {
                WeaponHolder.IsHided = false;
            }

            bool isAiming = ShouldAimingAlways && WeaponHolder.CurrentAnimatorWeaponID != -1 ? true : IsShooting;
            animator.SetBool(shootAnimatorAimingKeyId, isAiming);
            animator.SetBool(shootAnimatorShootingKeyId, shootingKey);

            CurrentWeapon.HandleVFX(shootingKey);
        }

        public void ShootTrigger() // Animator event
        {
            shootTriggered = true;
        }

        public void SetAnimatorWeaponIndex()
        {
            if (!animator)
                Awake();

            animator.SetFloat(weaponIdKeyId, WeaponHolder.CurrentAnimatorWeaponID);
        }

        public void StartRealod()
        {
            animator.SetBool(weaponRealodingId, true);
        }

        public void StopRealoding()
        {
            WeaponHolder.CompleteRealod();
            animator.SetBool(weaponRealodingId, false);
        }

        public void Clone(NpcBehaviourBase sourceNpc)
        {
            if (sourceNpc == null) return;

            SetHealth(sourceNpc.CurrentHealth);
            WeaponHolder.IsHided = false;

            var sourceNpcWeaponHolder = sourceNpc.WeaponHolder;
            var availableWeaponTypes = new List<WeaponType>(sourceNpcWeaponHolder.AvailableWeaponTypes);

            if (availableWeaponTypes?.Count > 0)
            {
                WeaponHolder.InitializeWeapon(sourceNpcWeaponHolder.CurrentWeapon.WeaponType, availableWeaponTypes);
                Weapon currentWeapon = WeaponHolder.SelectWeapon(sourceNpcWeaponHolder.CurrentWeapon.WeaponType);
                currentWeapon.Initialize(sourceNpcWeaponHolder.CurrentWeapon.CurrentAmmo);
            }
        }

        public void SetHealth(int health)
        {
            EntityHealth.Initialize(health);
        }

        public virtual void Initialize(Vector3 spawnPosition, int localSpawnIndex = -1)
        {
        }

        public virtual void DestroyEntity() { }

        // Animator event
        public virtual void Landed() { }

        protected virtual void TryToShoot()
        {
            if (CurrentWeapon != null)
            {
                if (CanShoot)
                {
                    if (shootTriggered)
                    {
                        CurrentWeapon.TryToShoot(shootDirection);
                        shootTriggered = false;
                    }
                }
            }
            else
            {
                if (shootTriggered)
                {
                    shootTriggered = false;
                }
            }
        }

        protected virtual void Reset()
        {
            IsShooting = false;
            shootTriggered = false;
            animator.SetBool(shootAnimatorAimingKeyId, false);
        }

        private void WeaponHolder_OnSelectWeapon(NpcWeaponHolder npcWeaponHolder, WeaponType weaponType)
        {
            if (weaponType == WeaponType.Default)
            {
                animator.SetBool(shootAnimatorAimingKeyId, false);
                animator.SetBool(shootAnimatorShootingKeyId, false);
            }
        }
    }
}