namespace Spirit604.Extensions
{
    public interface IPoolCallback
    {
        void OnCreateFromPool();
        void OnReturnToPool();

        void OnPop();
        void OnPush();
    }
}