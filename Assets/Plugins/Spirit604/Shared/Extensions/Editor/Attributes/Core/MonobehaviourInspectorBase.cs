using UnityEditor;

namespace Spirit604.Attributes.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviourBase), true)]
    public class MonobehaviourInspectorBase : CustomInspectorBase
    {
    }
}