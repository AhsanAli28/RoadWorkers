using UnityEngine;

namespace Spirit604.Gameplay.Npc
{
    public class NpcInCarAnimator : MonoBehaviour
    {
        private Transform leftFoot, rightFoot;
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!leftFoot) return;
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFoot.transform.position);
            animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFoot.transform.position);
        }

        public void Initialize(Transform leftFoot, Transform rightFoot)
        {
            this.leftFoot = leftFoot;
            this.rightFoot = rightFoot;
        }
    }
}