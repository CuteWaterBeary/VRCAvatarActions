#if UNITY_EDITOR
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

        public override void AddKeyframes(AnimationClip animation)
        {
            bool defaultstate = ObjRef.activeSelf;

            //Create curve
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, defaultstate ? 0f : 1f));
            animation.SetCurve(Path, typeof(GameObject), "m_IsActive", curve);

            //Disable the object
            // obj.SetActive(defaultstate);
        }
    }
}
#endif