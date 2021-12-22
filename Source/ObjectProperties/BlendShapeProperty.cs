#if UNITY_EDITOR
using UnityEngine;
using static VRCAvatarActions.ObjectProperty;

namespace VRCAvatarActions
{
    public class BlendShapeProperty : PropertyWrapper
    {
        public BlendShapeProperty(ObjectProperty property) : base(property) { }

        public void Setup()
        {
            if (prop.values == null || prop.values.Length != 2)
                prop.values = new float[2];
            if (prop.stringValues == null || prop.stringValues.Length != 1)
                prop.stringValues = new string[1];
            prop.objects = null;
        }

        public int Index
        {
            get => (int)prop.values[0];
            set => prop.values[0] = value;
        }

        public string Name { get => prop.stringValues[0]; set => prop.stringValues[0] = value; }

        public float Weight { get => prop.values[1]; set => prop.values[1] = value; }

        public override void AddKeyframes(AnimationClip animation)
        {
            try
            {
                var name = Name;

                if (name == null)
                {
                    var skinned = objRef.GetComponent<SkinnedMeshRenderer>();
                    name = Name = skinned.sharedMesh.GetBlendShapeName(Index);
                }
            } catch (System.Exception e) {
                Debug.LogError("Unable to find blendshape " + Name);
            }

            //Create curve
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, Weight));
            animation.SetCurve(path, typeof(SkinnedMeshRenderer), $"blendShape.{Name}", curve);
        }
    }
}
#endif