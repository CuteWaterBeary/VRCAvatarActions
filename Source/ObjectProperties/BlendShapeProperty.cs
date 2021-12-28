#if UNITY_EDITOR
using UnityEditor;
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

        public int Index { get => (int)prop.values[0]; set => prop.values[0] = value; }

        public string Name { get => prop.stringValues[0]; set => prop.stringValues[0] = value; }

        public float Weight { get => prop.values[1]; set => prop.values[1] = value; }

        public override void AddKeyframes(AnimationClip animation)
        {
            try
            {
                var name = Name;

                if (name == null)
                {
                    var skinned = ObjRef.GetComponent<SkinnedMeshRenderer>();
                    name = Name = skinned.sharedMesh.GetBlendShapeName(Index);
                }
            }
            catch (System.Exception)
            {
                Debug.LogError("Unable to find blendshape " + Name);
            }

            //Create curve
            var curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, Weight));
            animation.SetCurve(Path, typeof(SkinnedMeshRenderer), $"blendShape.{Name}", curve);
        }

        public override void OnGUI(BaseActions context)
        {
            var skinnedRenderer = ObjRef.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer == null)
            {
                EditorGUILayout.HelpBox("GameObject doesn't have a MeshFilter or SkinnedMeshRenderer component.", MessageType.Error);
                return;
            }

            if (skinnedRenderer.name == "Body" || prop.path == "Body")
            {
                prop.objRef = skinnedRenderer.transform.parent.Find("Face").gameObject;
                prop.path = "Face";
                skinnedRenderer = ObjRef.GetComponent<SkinnedMeshRenderer>();
                EditorUtility.SetDirty(context);
            }

            //Get mesh
            Mesh mesh = skinnedRenderer.sharedMesh;

            //Setup data
            Setup();

            var popup = new string[mesh.blendShapeCount];
            for (int i = 0; i < mesh.blendShapeCount; i++)
                popup[i] = mesh.GetBlendShapeName(i);

            //Editor
            EditorGUILayout.BeginHorizontal();
            {
                int index = Name != null ? mesh.GetBlendShapeIndex(Name) : Index;
                //Property
                Index = EditorGUILayout.Popup(index, popup);
                Name = popup[Index];

                //Value
                EditorGUI.BeginChangeCheck();
                Weight = EditorGUILayout.Slider(Weight, 0f, 100f);
                if (EditorGUI.EndChangeCheck())
                {
                    //I'd like to preview the change, but preserving the value
                    //TODO
                    //skinnedRenderer.SetBlendShapeWeight((int)values[0], values[1]);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif