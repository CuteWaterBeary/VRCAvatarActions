#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static VRCAvatarActions.ObjectProperty;

namespace VRCAvatarActions
{
    public class PlayAudioProperty : PropertyWrapper
    {
        public PlayAudioProperty(ObjectProperty property) : base(property) { }

        public void Setup()
        {
            if (prop.values == null || prop.values.Length != 4)
            {
                prop.values = new float[4];
                Spatial = true;
                Volume = 1;
                Near = 6;
                Far = 20;
            }
            if (prop.objects == null || prop.objects.Length != 1)
                prop.objects = new Object[1];
        }

        public AudioClip AudioClip { get => prop.objects[0] as AudioClip; set => prop.objects[0] = value; }

        public float Volume { get => prop.values[1]; set => prop.values[1] = value; }

        public bool Spatial { get => prop.values[0] != 0; set => prop.values[0] = value ? 1f : 0f; }

        public float Near { get => prop.values[2]; set => prop.values[2] = value; }

        public float Far { get => prop.values[3]; set => prop.values[3] = value; }

        public override void AddKeyframes(AnimationClip animation)
        {
            if (AudioClip == null)
                return;

            //Find/Create child object
            var name = $"Audio_{AudioClip.name}";
            var child = ObjRef.transform.Find(name)?.gameObject;
            if (child == null)
            {
                child = new GameObject(name);
                child.transform.SetParent(ObjRef.transform, false);
            }
            child.SetActive(false); //Disable

            //Find/Create component
            var audioSource = child.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = child.AddComponent<AudioSource>();
            audioSource.clip = AudioClip;
            audioSource.volume = 0f; //Audio 0 by default

            //Spatial
            var spatialComp = child.GetComponent<VRCSpatialAudioSource>();
            if (spatialComp == null)
                spatialComp = child.AddComponent<VRCSpatialAudioSource>();
            spatialComp.EnableSpatialization = Spatial;
            spatialComp.Near = Near;
            spatialComp.Far = Far;

            //Create curve
            var subPath = $"{Path}/{name}";
            {
                var curve = new AnimationCurve();
                curve.AddKey(new Keyframe(0f, Volume));
                animation.SetCurve(subPath, typeof(AudioSource), $"m_Volume", curve);
            }
            {
                var curve = new AnimationCurve();
                curve.AddKey(new Keyframe(0f, 1f));
                animation.SetCurve(subPath, typeof(GameObject), $"m_IsActive", curve);
            }
        }

        public void OnGUI(BaseActions context)
        {
            //Setup
            Setup();

            //Editor
            //EditorGUILayout.BeginHorizontal();
            {
                //Property
                EditorGUILayout.BeginHorizontal();
                {
                    AudioClip = (AudioClip)EditorGUILayout.ObjectField("Clip", AudioClip, typeof(AudioClip), false);
                    EditorGUILayout.TextField(AudioClip != null ? $"{AudioClip.length:N2}" : "", GUILayout.Width(64));
                }
                EditorGUILayout.EndHorizontal();
                Volume = EditorGUILayout.Slider("Volume", Volume, 0f, 1f);
                Spatial = EditorGUILayout.Toggle("Spatial", Spatial);
                EditorGUI.BeginDisabledGroup(!Spatial);
                {
                    Near = EditorGUILayout.FloatField("Near", Near);
                    Far = EditorGUILayout.FloatField("Far", Far);
                }
                EditorGUI.EndDisabledGroup();
            }
            //EditorGUILayout.EndHorizontal();
        }

    }
}
#endif