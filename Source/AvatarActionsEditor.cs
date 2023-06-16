using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;

#if UNITY_EDITOR
namespace VRCAvatarActions
{
    [CustomEditor(typeof(AvatarActions))]
    public class AvatarActionsEditor : Editor
    {
        AvatarActions script;
        private Editor menuEditor = null;

        public override void OnInspectorGUI()
        {
            script = target as AvatarActions;
            EditorGUI.BeginChangeCheck();
            {
                Inspector_Body();
            }
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }
        }

        public void Inspector_Body()
        {
            //Avatar
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                script.avatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", script.avatarDescriptor, typeof(VRCAvatarDescriptor), true);
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Menu Actions
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                EditorGUILayout.BeginHorizontal();
                script.menuActions = (MenuActions)EditorGUILayout.ObjectField("Menu Actions", script.menuActions, typeof(MenuActions), false);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel -= 1;

            EditorBase.Divider();

            if (script.menuActions != null)
            {
                // DrawFoldoutInspector(script.menuActions, ref menuEditor);
                if (!menuEditor)
                    CreateCachedEditor(script.menuActions, null, ref menuEditor);

                if (menuEditor is MenuActionsEditor menuActionsEditor)
                {
                    menuActionsEditor.isSubInspector = true;
                    menuActionsEditor.avatarDescriptor = script.avatarDescriptor;
                    menuActionsEditor.OnInspectorGUI();
                }
            }
            EditorGUILayout.EndVertical();

            //Non-Menu Actions
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            EditorGUILayout.LabelField("Other Actions");
            {
                if (GUILayout.Button("Add"))
                    script.otherActions.Add(null);
                for (int i = 0; i < script.otherActions.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        //Reference
                        script.otherActions[i] = (NonMenuActions)EditorGUILayout.ObjectField("Actions", script.otherActions[i], typeof(NonMenuActions), false);

                        //Delete
                        if (GUILayout.Button("X", GUILayout.Width(32)))
                        {
                            script.otherActions.RemoveAt(i);
                            i -= 1;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Expression Parameter Settings
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            script.foldoutParameterSettings = EditorGUILayout.Foldout(script.foldoutParameterSettings, "Parameter Settings");
            EditorGUI.indentLevel -= 1;

            if (script.foldoutParameterSettings)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField("Name", GUILayout.Width(120));
                EditorGUILayout.LabelField("Type", GUILayout.Width(80));
                EditorGUILayout.LabelField("Default", GUILayout.Width(64));
                EditorGUILayout.LabelField("Saved", GUILayout.Width(40));

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < script.parameterDefaults.Count; i++)
                {
                    AvatarActions.ParameterDefault parameter = script.parameterDefaults[i];
                    parameter.enabled = script.avatarDescriptor.expressionParameters.FindParameter(parameter.name) != null;
                }
                script.parameterDefaults.Sort((a, b) => b.enabled == a.enabled ? a.name.CompareTo(b.name) : b.enabled.CompareTo(a.enabled));

                foreach (var parameter in script.parameterDefaults.ToArray())
                {
                    DrawExpressionParameter(parameter);
                }
            }
            EditorGUILayout.EndVertical();

            EditorBase.Divider();

            //Build
            EditorGUI.BeginDisabledGroup(script.ReturnAnyScriptableObject() == null || script.avatarDescriptor == null);
            if (GUILayout.Button("Build Avatar Data", GUILayout.Height(32)))
            {
                ActionsBuilder builder = new ActionsBuilder();
                builder.BuildAvatarData(script.avatarDescriptor, script);
            }
            EditorGUI.EndDisabledGroup();

            //Build Options
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                script.foldoutBuildOptions = EditorGUILayout.Foldout(script.foldoutBuildOptions, "Built Options");
                if (script.foldoutBuildOptions)
                {
                    //Ignore Lists
                    DrawStringList(ref script.foldoutIgnoreLayers, "Ignore Layers", script.ignoreLayers);
                    DrawStringList(ref script.foldoutIgnoreParameters, "Ignore Parameters", script.ignoreParameters);

                    void DrawStringList(ref bool foldout, string title, List<string> list)
                    {
                        EditorGUI.indentLevel += 1;
                        foldout = EditorGUILayout.Foldout(foldout, BaseActionsEditor.Title(title, list.Count > 0));
                        if (foldout)
                        {
                            //Add
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(EditorGUI.indentLevel * 10);
                            if (GUILayout.Button("Add"))
                            {
                                list.Add(null);
                            }
                            GUILayout.EndHorizontal();

                            //Layers
                            for (int i = 0; i < list.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                list[i] = EditorGUILayout.TextField(list[i]);
                                if (GUILayout.Button("X", GUILayout.Width(32)))
                                {
                                    list.RemoveAt(i);
                                    i--;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUI.indentLevel -= 1;
                    }
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Build Data
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            script.foldoutBuildData = EditorGUILayout.Foldout(script.foldoutBuildData, "Built Data");
            if (script.foldoutBuildData && script.avatarDescriptor != null)
            {
                void AnimationController(VRCAvatarDescriptor.AnimLayerType animLayerType, string name)
                {
                    VRCAvatarDescriptor.CustomAnimLayer descLayer = new VRCAvatarDescriptor.CustomAnimLayer();
                    foreach (var layer in script.avatarDescriptor.baseAnimationLayers)
                    {
                        if (layer.type == animLayerType)
                        {
                            descLayer = layer;
                            break;
                        }
                    }

                    var controller = descLayer.animatorController as UnityEditor.Animations.AnimatorController;

                    EditorGUI.BeginChangeCheck();
                    controller = (UnityEditor.Animations.AnimatorController)EditorGUILayout.ObjectField(name, controller, typeof(UnityEditor.Animations.AnimatorController), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        descLayer.animatorController = controller;
                        descLayer.isDefault = false;
                    }
                }

                EditorGUILayout.HelpBox("Objects built and linked on the avatar descriptor. Anything referenced here will be modified and possibly destroyed by the compiling process.", MessageType.Info);

                AnimationController(VRCAvatarDescriptor.AnimLayerType.Action, "Action Controller");
                AnimationController(VRCAvatarDescriptor.AnimLayerType.FX, "FX Controller");
                script.avatarDescriptor.expressionsMenu = (ExpressionsMenu)EditorGUILayout.ObjectField("Expressions Menu", script.avatarDescriptor.expressionsMenu, typeof(ExpressionsMenu), false);
                script.avatarDescriptor.expressionParameters = (ExpressionParameters)EditorGUILayout.ObjectField("Expression Parameters", script.avatarDescriptor.expressionParameters, typeof(ExpressionParameters), false);
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }

        void DrawExpressionParameter(AvatarActions.ParameterDefault parameter)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(!parameter.enabled);

            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(parameter.name), GUILayout.Width(120));
            EditorGUILayout.LabelField(parameter.valueType.ToString(), GUILayout.Width(80));

            EditorGUI.BeginChangeCheck();

            switch (parameter.valueType)
            {
                case ExpressionParameters.ValueType.Int:
                    parameter.defaultValue = Mathf.Clamp(EditorGUILayout.IntField((int)parameter.defaultValue, GUILayout.Width(64)), 0, 255);
                    break;
                case ExpressionParameters.ValueType.Float:
                    parameter.defaultValue = Mathf.Clamp(EditorGUILayout.FloatField(parameter.defaultValue, GUILayout.Width(64)), -1f, 1f);
                    break;
                case ExpressionParameters.ValueType.Bool:
                    parameter.defaultValue = EditorGUILayout.Toggle(parameter.defaultValue != 0, GUILayout.Width(64)) ? 1f : 0f;
                    break;
            }
            parameter.saved = EditorGUILayout.Toggle(parameter.saved, GUILayout.Width(40));

            if (EditorGUI.EndChangeCheck())
            {
                ActionsBuilder builder = new ActionsBuilder();
                builder.UpdateParameter(script.avatarDescriptor, script, parameter.name);
            }

            GUILayout.FlexibleSpace();
            EditorGUI.EndDisabledGroup();

            if (parameter.enabled == false && GUILayout.Button("X", GUILayout.MaxWidth(20)))
            {
                script.parameterDefaults.Remove(parameter);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif