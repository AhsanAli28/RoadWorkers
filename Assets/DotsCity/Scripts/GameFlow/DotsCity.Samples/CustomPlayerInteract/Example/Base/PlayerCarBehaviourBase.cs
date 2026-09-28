using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    public abstract class PlayerCarBehaviourBase : MonoBehaviour
    {
        public abstract bool EnterCar(GameObject playerNpc);
        public abstract GameObject ExitCar();
        public abstract void Init();
    }
}