using UnityEditor;

namespace Spirit604.Attributes.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScriptableObjectBase), true)]
    public class ScriptableObjectInspectorBase : CustomInspectorBase
    {
    }
}