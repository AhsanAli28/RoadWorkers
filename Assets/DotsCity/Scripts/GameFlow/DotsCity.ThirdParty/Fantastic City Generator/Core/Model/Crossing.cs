#if FCG
using FCG;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spirit604.DotsCity.ThirdParty.FCG
{
    public class Crossing : IEquatable<Crossing>
    {
        private List<ConnectionData> connections = new List<ConnectionData>();

        public IList<ConnectionData> Connections => connections.AsReadOnly();

        public void Add(FCGWaypointsContainer road, bool byEnd)
        {
            connections.Add(new ConnectionData()
            {
                Road = road,
                IsEnd = byEnd,
            });

            connections = connections.OrderBy(a => a.Road.GetInstanceID()).ToList();
        }

        public bool Equals(Crossing other)
        {
            if (connections.Count != other.connections.Count) return false;

            for (int i = 0; i < connections.Count; i++)
            {
                ConnectionData connection = connections[i];
                ConnectionData otherConnection = other.connections[i];

                if (connection.Road != otherConnection.Road) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;

            foreach (var connection in connections)
            {
                hash += connection.Road.GetInstanceID();
            }

            return hash;
        }
    }
}
#endif