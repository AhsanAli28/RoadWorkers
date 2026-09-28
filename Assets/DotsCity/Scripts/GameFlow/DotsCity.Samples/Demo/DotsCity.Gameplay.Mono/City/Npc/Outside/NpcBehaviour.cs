namespace Spirit604.Gameplay.Npc
{
    public class NpcBehaviour : NpcBehaviourBase
    {
        private IShootTargetProvider shootTargetProvider;

        protected override void Awake()
        {
            base.Awake();
            shootTargetProvider = GetComponent<IShootTargetProvider>();
        }

        private void Update()
        {
            if (!IsAlive)
                return;

            shootTargetProvider.GetShootDirection(transform.position, out var shootDirection);
            Shoot(shootDirection);
        }
    }
}
