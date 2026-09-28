using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Sound;
using Spirit604.Gameplay.Factory;
using System;
using UnityEngine;

namespace Spirit604.Gameplay.Weapons
{
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private WeaponType weaponType;
        [SerializeField] private BulletType bulletType;
        [SerializeField][Range(0, 50)] private int maxAmmo;
        [SerializeField][Range(0f, 10f)] private float realodTime;
        [SerializeField][Range(0f, 4f)] private float fireRate;
        [SerializeField][Range(0f, 1f)] private float speedPenaltyMultiplier = 1f;
        [SerializeField] private ParticleSystem shellsVFX;
        [SerializeField] private SoundData shotSound;
        [SerializeField] private SoundData realodSound;

        private float nextShootTime;
        private bool realodRequested;
        private float endRealodTime;
        protected IBulletFactory bulletFactory;
        protected ISoundPlayer soundPlayer;
        protected FactionType factionType;
        protected GameObject owner;

        public bool CanShoot => !IsRealoding && IsShooting && CurrentAmmo > 0 && Time.time > nextShootTime;

        public bool CanPlayAnimation => !IsRealoding && IsShooting && CurrentAmmo > 0;

        public bool IsRealoding { get; private set; }

        public WeaponType WeaponType => weaponType;

        public float SpeedPenaltyMultiplier => speedPenaltyMultiplier;

        public int MaxAmmo => maxAmmo;

        public int CurrentAmmo { get; private set; }

        public bool IsShooting { get; set; } = true;

        public float DamageMultiplier { get; set; } = 1f;

        private bool IsNeededToRealod => CurrentAmmo == 0;

        public FactionType FactionType { get => factionType; set => factionType = value; }

        public event Action OnRealoadRequested = delegate { };

        private void Awake()
        {
            Realod();
        }

        protected void OnDisable()
        {
            DamageMultiplier = 1f;
        }

        protected virtual void Update()
        {
            if (IsRealoding && Time.time >= endRealodTime)
            {
                IsRealoding = false;
                Realod();
            }

            if (!IsRealoding && IsNeededToRealod)
            {
                if (!realodRequested)
                {
                    realodRequested = true;
                    OnRealoadRequested();
                }
            }
        }

        public virtual void Initialize(GameObject owner, IBulletFactory bulletFactory, ISoundPlayer soundPlayer, FactionType factionType)
        {
            this.owner = owner;
            this.bulletFactory = bulletFactory;
            this.soundPlayer = soundPlayer;
            this.factionType = factionType;
        }

        public void Initialize(int currentAmmo)
        {
            CurrentAmmo = currentAmmo;
        }

        public virtual bool TryToShoot(Vector3 direction)
        {
            if (CanShoot)
            {
                CurrentAmmo--;

                nextShootTime = Time.time + fireRate;
                ShotInternal(direction);
                ShotSound();
            }

            return CanShoot;
        }

        public void HandleVFX(bool isShooting)
        {
            if (!shellsVFX)
            {
                return;
            }

            if (isShooting)
            {
                if (shellsVFX.isStopped)
                    shellsVFX.Play();
            }
            else
            {
                if (shellsVFX.isPlaying)
                    shellsVFX.Stop();
            }
        }

        public void Realod()
        {
            realodRequested = false;
            IsRealoding = false;
            CurrentAmmo = maxAmmo;
        }

        public void StartRealod()
        {
            if (realodSound != null)
            {
                soundPlayer.PlayOneShot(realodSound, firePoint.position);
            }

            endRealodTime = Time.time + realodTime;
            IsRealoding = true;
        }

        protected virtual void ShotInternal(Vector3 direction)
        {
            bulletFactory.SpawnBullet(owner, direction, firePoint.position, (int)bulletType, DamageMultiplier);
        }

        protected void ShotSound()
        {
            if (shotSound != null)
            {
                if (firePoint)
                    soundPlayer.PlayOneShot(shotSound, firePoint.position);
                else
                    soundPlayer.PlayOneShot(shotSound, transform.position);
            }
        }
    }
}