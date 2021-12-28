#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace VRCAvatarActions
{
    public abstract partial class BaseActions : ScriptableObject
    {
        public enum AnimationLayer
        {
            Action,
            FX,
        }

        public enum OnOffEnum
        {
            On = 1,
            Off = 0
        }

        public abstract void GetActions(List<Action> output);
        public abstract Action AddAction();
        public abstract void RemoveAction(Action action);
        public abstract void InsertAction(int index, Action action);

        public virtual bool CanUseLayer(AnimationLayer layer) => true;

        public virtual bool ActionsHaveExit() => true;
    }
}
#endif