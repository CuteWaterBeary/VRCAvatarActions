#if UNITY_EDITOR
using System.Linq;
using UnityEngine;

namespace VRCAvatarActions
{
    //Object Toggles
    [System.Serializable]
    public class ObjectProperty
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

        public ObjectProperty() { }

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

        public PropertyWrapper ToWrapper()
        {
            switch (type)
            {
                case Type.ObjectToggle: return new ObjectToggleProperty(this);
                case Type.MaterialSwap: return new MaterialSwapProperty(this);
                case Type.BlendShape: return new BlendShapeProperty(this);
                case Type.PlayAudio: return new PlayAudioProperty(this);
                default: return null;
            }
        }

        public abstract class PropertyWrapper
        {
            public ObjectProperty prop;

            public string Path => prop.path;
            public GameObject ObjRef => prop.objRef;

            public PropertyWrapper(ObjectProperty property)
            {
                prop = property;
            }

            public abstract void AddKeyframes(AnimationClip animation);

            public abstract void OnGUI(BaseActions context);
        }
    }
}
#endif