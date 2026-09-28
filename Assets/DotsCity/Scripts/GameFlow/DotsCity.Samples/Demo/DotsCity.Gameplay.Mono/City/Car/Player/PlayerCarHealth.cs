using Spirit604.Extensions;

namespace Spirit604.Gameplay.Player
{
    public class PlayerCarHealth : HealthBaseWithDelay
    {
        protected override void Death()
        {
            gameObject.ReturnToPool();
        }
    }
}
