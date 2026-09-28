namespace Spirit604.DotsCity.Core.Bootstrap
{
    /// <summary> Initialization after all IBootstrapCommand have been executed. </summary>
    public interface ILateInitializer
    {
        void LateInitialize();
    }
}
