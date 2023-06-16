#if UNITY_EDITOR
using UnityEditor;

namespace VRCAvatarActions
{
    [CustomEditor(typeof(BasicActions))]
    public class GenericActionsEditor : BaseActionsEditor
    {
        public override void Inspector_Header()
        {
            EditorGUILayout.HelpBox("Basic Actions - Actions with no default triggers.", MessageType.Info);
        }
    }
}
#endif