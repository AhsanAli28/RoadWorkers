using Spirit604.Attributes;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Weapons;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Session
{
    public class PlayerSessionListener : MonoBehaviour
    {
        private PlayerSession playerSession;

        [InjectWrapper]
        public void Construct(PlayerSession playerSession)
        {
            this.playerSession = playerSession;

            playerSession.OnNpcLinked += PlayerSessionController_OnNpcLinked;
            playerSession.OnNpcUnlinked += PlayerSessionController_OnNpcUnlinked;
        }

        private void PlayerSessionController_OnNpcLinked(NpcBehaviourBase npc)
        {
            npc.WeaponHolder.OnSwitchHideState += WeaponHolder_OnSwitchHideState;
            npc.WeaponHolder.OnSelectWeapon += WeaponHolder_OnSelectWeapon;
        }

        private void PlayerSessionController_OnNpcUnlinked(NpcBehaviourBase npc)
        {
            npc.WeaponHolder.OnSwitchHideState -= WeaponHolder_OnSwitchHideState;
            npc.WeaponHolder.OnSelectWeapon -= WeaponHolder_OnSelectWeapon;
        }

        private void WeaponHolder_OnSwitchHideState(NpcWeaponHolder npcWeaponHolder, bool isHided)
        {
            if (npcWeaponHolder.NpcBase.CanControl)
            {
                playerSession.CurrentSessionData.WeaponIsHided = isHided;
            }
        }

        private void WeaponHolder_OnSelectWeapon(NpcWeaponHolder arg1, WeaponType arg2)
        {
            playerSession.SaveNpcData(arg1.NpcBase);
        }
    }
}