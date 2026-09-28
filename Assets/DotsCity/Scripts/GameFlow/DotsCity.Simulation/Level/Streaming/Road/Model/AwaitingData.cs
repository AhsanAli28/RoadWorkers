using Spirit604.Extensions;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public struct AwaitingData
    {
        /// <summary> TrafficNode entity waiting for missing nodes. </summary>
        public Entity SourceEntity;

        public ConnectionSettings ConnectionSettings;

        public bool DefaultNode => DotsEnumExtension.HasFlagUnsafe(ConnectionSettings, ConnectionSettings.Default);
        public bool DefaultConnection => DotsEnumExtension.HasFlagUnsafe(ConnectionSettings, ConnectionSettings.DefaultConnection);
        public bool SubConnection => DotsEnumExtension.HasFlagUnsafe(ConnectionSettings, ConnectionSettings.SubConnection);
    }
}