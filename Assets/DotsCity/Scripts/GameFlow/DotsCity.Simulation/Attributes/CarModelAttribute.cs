using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Attributes
{
    public class CarModelAttribute : PropertyAttribute
    {
        public string CollectionFieldName { get; set; }

        public CarModelAttribute(string sourceCollectionFieldName)
        {
            CollectionFieldName = sourceCollectionFieldName;
        }
    }
}