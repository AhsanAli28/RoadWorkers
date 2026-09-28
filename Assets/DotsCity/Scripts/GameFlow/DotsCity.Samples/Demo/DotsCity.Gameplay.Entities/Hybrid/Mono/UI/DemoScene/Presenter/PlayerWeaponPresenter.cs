using Spirit604.Attributes;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Gameplay.Player.Session;
using Spirit604.Gameplay.Inventory;
using Spirit604.Gameplay.Player.Session;
using Spirit604.Gameplay.Weapons;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.UI
{
    public class PlayerWeaponPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerSelectWeaponView playerSelectWeaponView;
        [SerializeField] private bool hideVehicleUI;

        private PlayerActorTracker playerActorTracker;
        private PlayerSession playerSession;

        [InjectWrapper]
        public void Construct(PlayerActorTracker playerActorTracker, PlayerSession playerSession)
        {
            this.playerActorTracker = playerActorTracker;
            this.playerSession = playerSession;
        }

        private void Start()
        {
            playerSelectWeaponView.OnHideWeapon += PlayerSelectWeaponView_OnHideWeapon;
            playerSelectWeaponView.OnSelectWeapon += PlayerSelectWeaponView_OnSelectWeapon;
        }

        private void OnEnable()
        {
            playerActorTracker.OnSwitchActor += PlayerActorTracker_OnSwitchActor;
        }

        private void OnDisable()
        {
            playerActorTracker.OnSwitchActor -= PlayerActorTracker_OnSwitchActor;
        }

        public void SwitchActivePanelState(bool isActive)
        {
            playerSelectWeaponView.SwitchActivePanelState(isActive);
        }

        public void TryToSelectWeapon(int index)
        {
            var currentPlayerData = playerSession.CurrentSessionData.CurrentSelectedPlayer;

            if (currentPlayerData != null)
            {
                if (index == 0)
                {
                    playerSession.SwitchWeaponHidedState(true);
                    SelectWeapon(WeaponType.Default);
                }
                else
                {
                    var item = playerSession.TryToGetItem<WeaponItem>(currentPlayerData, ItemType.Weapon, index - 1);

                    if (item != null)
                    {
                        SelectWeapon(item.WeaponType);
                    }
                }
            }
        }

        private void SelectWeapon(WeaponType selectedWeaponType)
        {
            var currentPlayerData = playerSession.CurrentSessionData.CurrentSelectedPlayer;

            if (currentPlayerData != null)
            {
                SelectWeapon(currentPlayerData, selectedWeaponType);
                playerSelectWeaponView.TryToSelectButton(selectedWeaponType, true);
            }
            else
            {
                UnityEngine.Debug.LogError($"OnSelectWeapon {selectedWeaponType} CurrentSelectedPlayer is null");
            }
        }

        private void SelectWeapon(CharacterSessionData currentPlayerData, WeaponType selectedWeaponType)
        {
            playerSelectWeaponView.TryToSelectButton(selectedWeaponType, true);

            if (selectedWeaponType != WeaponType.Default)
            {
                playerSession.TryToSelectItem<WeaponItem>(currentPlayerData, selectedWeaponType);
            }
            else
            {
                playerSession.TryToSelectWeapon(currentPlayerData, WeaponType.Default);
            }
        }

        private void PlayerActorTracker_OnSwitchActor(GameObject newTarget)
        {
            var newActor = newTarget.GetComponent<PlayerActor>();

            if (!newActor)
            {
                SwitchActivePanelState(false);
                return;
            }

            var isCar = newActor.CarSlots != null;
            var IsHided = newActor.IsCamera;

            if (isCar && hideVehicleUI)
            {
                IsHided = true;
            }

            if (!IsHided)
            {
                var currentPlayerData = playerSession.CurrentSessionData.CurrentSelectedPlayer;

                if (currentPlayerData != null)
                {
                    var items = playerSession.TryToGetItems<WeaponItem>(currentPlayerData, ItemType.Weapon);

                    playerSelectWeaponView.InitPanel(items);
                    SelectWeapon(currentPlayerData.CurrentSelectedWeapon);
                }
                else
                {
                    UnityEngine.Debug.LogError($"OnSwitchTarget {newTarget} CurrentSelectedPlayer is null");
                }
            }

            SwitchActivePanelState(!IsHided);
        }

        private void PlayerSelectWeaponView_OnSelectWeapon(WeaponType selectedWeaponType)
        {
            SelectWeapon(selectedWeaponType);
        }

        private void PlayerSelectWeaponView_OnHideWeapon()
        {
            playerSession.SwitchWeaponHidedState(true);
        }
    }
}
