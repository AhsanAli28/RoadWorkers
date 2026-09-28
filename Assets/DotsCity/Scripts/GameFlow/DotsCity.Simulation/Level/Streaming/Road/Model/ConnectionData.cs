using Spirit604.Extensions;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public struct ConnectionData
    {
        /// <summary> Connected TrafficNode entity. </summary>
        public Entity ConnectedEntity;

        /// <summary> Hash representation of the connected TrafficNode entity in case the connected entity doesn't currently exist. </summary>
        public int Hash;

        public ConnectionSettings ConnectionSettings;

        public bool DefaultNode => DotsEnumExtension.HasFlagUnsafe(ConnectionSettings, ConnectionSettings.Default);
        public bool DefaultConnection => DotsEnumExtension.HasFlagUnsafe(ConnectionSettings, ConnectionSettings.DefaultConnection);
        public bool SubConnection => DotsEnumExtension.HasFlagUnsafe(ConnectionSettings, ConnectionSettings.SubConnection);
    }
}