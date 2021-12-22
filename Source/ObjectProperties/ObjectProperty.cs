#if UNITY_EDITOR
using UnityEngine;
using System.Linq;

namespace VRCAvatarActions
{
    //Object Toggles
    [System.Serializable]
    public partial class ObjectProperty
    {
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
        public Object[] objects;
        public float[] values;
        public string[] stringValues;

        //Meta-data
        public GameObject objRef;

        public ObjectProperty()
        {
        }

        public ObjectProperty(ObjectProperty source)
        {
            path = string.Copy(source.path);
            type = source.type;
            objects = source.objects?.ToArray();
            values = source.values?.ToArray();
            stringValues = source.stringValues?.ToArray();
            objRef = source.objRef;
        }

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
                prop = property;
            }

            public string Path => prop.path;
            public GameObject ObjRef => prop.objRef;

            public abstract void AddKeyframes(AnimationClip animation);
        }
    }
}
#endif