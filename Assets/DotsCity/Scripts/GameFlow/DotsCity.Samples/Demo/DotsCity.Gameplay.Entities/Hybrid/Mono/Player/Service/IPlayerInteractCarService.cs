using System;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public interface IPlayerInteractCarService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="car"></param>
        /// <param name="sourceNpc"></param>
        /// <param name="onEnter">Returns gameobject npc</param>
        bool EnterCar(GameObject car, GameObject sourceNpc, Action<GameObject> onEnter = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="car"></param>
        /// <param name="onExit">Returns gameobject npc</param>
        void ExitCar(GameObject car, Action<GameObject> onExit = null);
    }
}