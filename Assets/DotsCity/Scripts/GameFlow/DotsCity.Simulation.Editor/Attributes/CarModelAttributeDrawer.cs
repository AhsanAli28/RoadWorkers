using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Factory.Car;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Attributes
{
    [CustomPropertyDrawer(typeof(CarModelAttribute))]
    public class CarModelAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as CarModelAttribute;

            EditorGUI.BeginProperty(position, label, property);

            var collectionField = property.serializedObject.FindProperty(attr.CollectionFieldName);

            if (collectionField != null && collectionField.objectReferenceValue != null)
            {
                var collection = (VehicleDataCollection)collectionField.objectReferenceValue;
                VehicleCollectionExtension.DrawModelOptions(position, collection, property);
            }
            else
            {
                property.intValue = EditorGUI.IntField(position, label, property.intValue);
            }

            EditorGUI.EndProperty();
        }
    }
}