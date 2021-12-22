#if UNITY_EDITOR
using UnityEngine;
using System.Linq;

namespace VRCAvatarActions
{
    //Object Toggles
    [System.Serializable]
    public partial class ObjectProperty
    {
        public ObjectProperty()
        {
        }
        public ObjectProperty(ObjectProperty source)
        {
            this.path = string.Copy(source.path);
            this.type = source.type;
            this.objects = source.objects?.ToArray();
            this.values = source.values?.ToArray();
            this.stringValues = source.stringValues?.ToArray();
            this.objRef = source.objRef;
        }

        public enum Type
        {
            ObjectToggle = 3450,
            MaterialSwap = 7959,
            BlendShape = 9301,
            PlayAudio = 9908,
        }
        public Type type = Type.ObjectToggle;

        //Data
        public string path;
        public UnityEngine.Object[] objects;
        public float[] values;
        public string[] stringValues;

        //Meta-data
        public GameObject objRef;

        public void Clear()
        {
            objRef = null;
            path = null;
            objects = null;
            values = null;
            stringValues = null;
        }

        public abstract class PropertyWrapper
        {
            public ObjectProperty prop;
            public PropertyWrapper(ObjectProperty property)
            {
                this.prop = property;
            }
            public string path
            {
                get { return prop.path; }
            }
            public GameObject objRef
            {
                get { return prop.objRef; }
            }
            public abstract void AddKeyframes(AnimationClip animation);
        }
    }
}
#endif