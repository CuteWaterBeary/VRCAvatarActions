#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static VRCAvatarActions.ObjectProperty;

namespace VRCAvatarActions
{
    public class MaterialSwapProperty : PropertyWrapper
    {
        public MaterialSwapProperty(ObjectProperty property) : base(property) { }

        public void Setup()
        {
            prop.objects = null;
        }

        public override bool ShouldGenerate(bool enter) => enter;

        public override void AddKeyframes(ActionsBuilder builder, BaseActions.Action action, AnimationClip animation, bool enter)
        {
            //For each material
            for (int i = 0; i < prop.objects.Length; i++)
            {
                var material = prop.objects[i];
                if (material == null)
                    continue;

                //Create curve
                var keyframes = new ObjectReferenceKeyframe[1];
                var keyframe = new ObjectReferenceKeyframe();
                keyframe.time = 0;
                keyframe.value = material;
                keyframes[0] = keyframe;
                EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(Path, typeof(Renderer), $"m_Materials.Array.data[{i}]");
                AnimationUtility.SetObjectReferenceCurve(animation, binding, keyframes);
            }
        }

        public override void SetState(ActionsBuilder builder, BaseActions.Action action)
        {
            // Euan: To make this work we need to modify this so we swap what the saved materials here are otherwise we can't switch back
        }

        public override void OnGUI(BaseActions context)
        {
            //Get object materials
            var mesh = ObjRef.GetComponent<Renderer>();
            if (mesh == null)
            {
                EditorGUILayout.HelpBox("GameObject doesn't have a Renderer component.", MessageType.Error);
                return;
            }

            //Materials
            var materials = mesh.sharedMaterials;
            if (materials != null)
            {
                //Create/Resize
                if (prop.objects == null || prop.objects.Length != materials.Length)
                    prop.objects = new Object[materials.Length];

                //Materials
                for (int materialIter = 0; materialIter < materials.Length; materialIter++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Material", GUILayout.MaxWidth(100));
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(materials[materialIter], typeof(Material), false);
                        EditorGUI.EndDisabledGroup();
                        prop.objects[materialIter] = EditorGUILayout.ObjectField(prop.objects[materialIter], typeof(Material), false) as Material;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}
#endif