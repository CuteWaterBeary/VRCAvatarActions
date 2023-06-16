#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static VRCAvatarActions.ObjectProperty;

namespace VRCAvatarActions
{
    public class ObjectToggleProperty : PropertyWrapper
    {
        public ObjectToggleProperty(ObjectProperty property) : base(property) { }

        public void Setup()
        {
            if (prop.values == null || prop.values.Length != 1)
                prop.values = new float[1];
            prop.objects = null;
        }

        public bool DesiredState { get => prop.values[0] != 0f; set => prop.values[0] = value ? 1f : 0f; }

        public override bool ShouldGenerate(bool enter) => true;

        public override void AddKeyframes(ActionsBuilder builder, BaseActions.Action action, AnimationClip animation, bool enter)
        {
            if (DesiredState == false)
            {
                enter = !enter;
            }

            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, enter ? 1f : 0f));
            animation.SetCurve(Path, typeof(GameObject), "m_IsActive", curve);
        }

        public override void SetState(ActionsBuilder builder, BaseActions.Action action)
        {
            bool defaultstate = builder.GetExpressionParameterDefaultState(action) == 1f;

            if (DesiredState == false)
            {
                defaultstate = !defaultstate;
            }

            ObjRef.SetActive(defaultstate);
        }

        public override void OnGUI(BaseActions context)
        {
            Setup();

            DesiredState = EditorGUILayout.Toggle("Enable", DesiredState);
        }
    }
}
#endif