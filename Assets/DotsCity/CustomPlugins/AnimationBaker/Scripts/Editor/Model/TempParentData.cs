#if UNITY_EDITOR
using UnityEngine;

namespace Spirit604.AnimationBaker.EditorInternal
{
    internal class TempParentData
    {
        public GameObject Parent;
        public bool PreviousActiveState;

        public void SwitchActiveState(bool isActive)
        {
            if (Parent)
            {
                Parent.SetActive(isActive);
            }
        }

        public void RevertState()
        {
            SwitchActiveState(PreviousActiveState);
        }
    }
}
#endif