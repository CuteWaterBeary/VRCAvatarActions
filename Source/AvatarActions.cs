using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;

#if UNITY_EDITOR
namespace VRCAvatarActions
{
    [ExecuteAlways]
    public class AvatarActions : MonoBehaviour
    {
        //Descriptor Data
        public VRCAvatarDescriptor avatarDescriptor;
        public MenuActions menuActions;
        public ExpressionParameters expressionParametersStorage;
        public List<NonMenuActions> otherActions = new List<NonMenuActions>();

        //Build Options
        public List<string> ignoreLayers = new List<string>();
        public List<string> ignoreParameters = new List<string>();
        public List<ParamDefault> parameterDefaults = new List<ParamDefault>();

        [System.Serializable]
        public struct ParamDefault
        {
            public string name;
            public float value;
            public bool saved;
        }

        //Meta
        public bool foldoutParameterSettings = false;
        public bool foldoutBuildData = false;
        public bool foldoutBuildOptions = false;
        public bool foldoutIgnoreLayers = false;
        public bool foldoutIgnoreParameters = false;
        public bool foldoutParameterDefaults = false;

        //Helper
        public UnityEngine.Object ReturnAnyScriptableObject()
        {
            if (menuActions != null)
                return menuActions;
            foreach(var action in otherActions)
            {
                if (action != null)
                    return action;
            }
            return null;
        }

        public void Awake()
        {
            if(GetComponent<VRCAvatarDescriptor>() != null)
            {
                EditorUtility.DisplayDialog("Error", "You are unable to add this script directly to an avatar. Please place this on a blank game object in the scene.", "Okay");
                GameObject.DestroyImmediate(this);
            }
        }
    }
}
#endif