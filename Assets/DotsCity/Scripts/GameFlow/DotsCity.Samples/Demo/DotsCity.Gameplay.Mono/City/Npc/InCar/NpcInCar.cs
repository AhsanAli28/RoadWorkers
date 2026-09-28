using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Spirit604.Gameplay.Car;
using UnityEngine;

namespace Spirit604.Gameplay.Npc
{
    public class NpcInCar : NpcBehaviourBase, INpcInCar
    {
        private const float UP_SPEED = 3f;
        private const string SHOW_KEY = "IsShow";
        private readonly Vector3 targetPosition = new Vector3(0.25f, 0.35f, 0);
        private readonly Vector3 hidePosition = new Vector3(-0.5f, 0, 0);

        private readonly int showKeyId = Animator.StringToHash(SHOW_KEY);

        private bool shouldUp;
        private bool isUpping;
        private bool isUpped;
        private bool inTransitionOfAppearing;
        private bool isShowing;

        private CarSlot mateInCarSpawnPoint;
        private NpcInCarAnimator mateInCarAnimator;
        private int inCarLayer;

        public GameObject Actor => gameObject;

        public override bool CanShoot => base.CanShoot && !isUpping;

        public virtual bool AutoDisposeWeapon { get; set; } = true;

        protected override void Awake()
        {
            base.Awake();

            WeaponHolder.IsHided = false;
            WeaponHolder.ReleaseWeaponOnDisable = false;
            mateInCarAnimator = GetComponent<NpcInCarAnimator>();
            inCarLayer = animator.GetLayerIndex("InCar");
        }

        private void OnEnable()
        {
            SetAnimatorWeaponIndex();
            WeaponHolder.AnimatorSetHandsIsHoldingWeapon(true);
        }

        private void OnDisable()
        {
            Reset();
        }

        protected override void FixedUpdate()
        {
            if (!mateInCarSpawnPoint) return;

            if (!WeaponHolder.IsHided && animator.GetFloat(weaponIdKeyId) != WeaponHolder.CurrentAnimatorWeaponID)
            {
                SetAnimatorWeaponIndex();
            }

            HandleLocalCarPosition();
        }

        public void Initialize(CarSlot _mateInCarSpawnPoint)
        {
            mateInCarSpawnPoint = _mateInCarSpawnPoint;

            if (mateInCarAnimator == null)
            {
                Awake();
            }

            mateInCarAnimator.Initialize(_mateInCarSpawnPoint.LeftFootIK, _mateInCarSpawnPoint.RightFootIK);
            animator.SetLayerWeight(inCarLayer, 1f);
        }

        public void Show(bool _isShowing)
        {
            if (_isShowing)
            {
                gameObject.SetActive(true);
                animator.SetBool(showKeyId, true);
            }

            inTransitionOfAppearing = true;
            isShowing = _isShowing;
        }

        public void SnapHide()
        {
            transform.localPosition = hidePosition;
            gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if (!AutoDisposeWeapon) return;
            WeaponHolder.ReturnWeapons();
            gameObject.ReturnToPool();
        }

        private void HandleLocalCarPosition()
        {
            if (inTransitionOfAppearing)
            {
                if (isShowing)
                {
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, Vector3.zero, UP_SPEED * Time.fixedDeltaTime);
                }
                else
                {
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, hidePosition * mateInCarSpawnPoint.Side, UP_SPEED * Time.fixedDeltaTime);
                }

                if (isShowing)
                {
                    if (transform.localPosition.IsEqual(Vector3.zero))
                    {
                        inTransitionOfAppearing = false;
                    }
                }
                else
                {
                    if (transform.localPosition.IsEqual(hidePosition * mateInCarSpawnPoint.Side))
                    {
                        inTransitionOfAppearing = false;
                        gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (mateInCarSpawnPoint.MinRestrictedAngle != 0)
                {
                    shouldUp = (transform.localRotation.eulerAngles.y > mateInCarSpawnPoint.MinRestrictedAngle && transform.localRotation.eulerAngles.y < mateInCarSpawnPoint.MaxRestrictedAngle);
                }

                if (shouldUp != isUpped && !isUpping)
                {
                    isUpping = true;
                }

                if (isUpping)
                {
                    if (!isUpped)
                    {
                        Vector3 localTargetPosition = new Vector3(targetPosition.x * mateInCarSpawnPoint.Side, targetPosition.y, targetPosition.z);
                        transform.localPosition = Vector3.MoveTowards(transform.localPosition, localTargetPosition, UP_SPEED * Time.fixedDeltaTime);
                    }
                    else
                    {
                        transform.localPosition = Vector3.MoveTowards(transform.localPosition, Vector3.zero, UP_SPEED * Time.fixedDeltaTime);
                    }

                    if (!isUpped)
                    {
                        Vector3 localTargetPosition = new Vector3(targetPosition.x * mateInCarSpawnPoint.Side, targetPosition.y, targetPosition.z);
                        if (transform.localPosition.IsEqual(localTargetPosition))
                        {
                            isUpping = false;
                            isUpped = true;
                        }
                    }
                    else
                    {
                        if (transform.localPosition.IsEqual(Vector3.zero))
                        {
                            isUpping = false;
                            isUpped = false;
                        }
                    }
                }

                TryToShoot();
            }
        }

        protected override void Reset()
        {
            base.Reset();

            shouldUp = false;
            isUpping = false;
            isUpped = false;
            inTransitionOfAppearing = false;
            isShowing = false;
            SnapHide();
        }
    }
}
