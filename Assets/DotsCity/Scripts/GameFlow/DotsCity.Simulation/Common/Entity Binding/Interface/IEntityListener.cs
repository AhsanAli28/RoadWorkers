namespace Spirit604.DotsCity.Simulation.Binding
{
    public interface IEntityListener
    {
        void OnInitialized(EntityWeakRef entityWeakRef);
        void OnUnload(EntityWeakRef entityWeakRef);
    }
}