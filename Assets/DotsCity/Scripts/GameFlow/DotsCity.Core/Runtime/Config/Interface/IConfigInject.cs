namespace Spirit604.DotsCity.Core
{
    /// <summary>
    /// Interface for injecting configs from the MainMenu.
    /// </summary>
    public interface IConfigInject
    {
        void InjectConfig(object config);
    }
}
