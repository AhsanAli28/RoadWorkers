using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Npc;
using System;
using UnityEngine;

namespace Spirit604.Gameplay.Factory
{
    /// <summary>
    /// An interface that handles how to enter and exit the car, as well as the location of the characters inside.
    /// </summary>
    public interface INpcInteractCarService
    {
        public INpcInCar Enter(CarSlot sourceSlot, string npcId, GameObject sourceNpc = null, bool driver = false, Action<CarSlot> onEnter = null);
   
        public GameObject Exit(CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, bool isDriver, Action<GameObject> onExit = null);
    }
}