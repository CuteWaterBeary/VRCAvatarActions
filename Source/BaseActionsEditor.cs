#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace VRCAvatarActions
{
    [CustomEditor(typeof(BaseActions))]
    public class BaseActionsEditor : EditorBase
    {
        protected BaseActions script;
        protected BaseActions.Action selectedAction;

        public void OnEnable()
        {
            var editor = target as BaseActions;
        }
        public override void Inspector_Body()
        {
            script = target as BaseActions;

            //Controls
            EditorGUILayout.BeginHorizontal();
            {
                //Add
                if (GUILayout.Button("Add"))
                {
                    var action = script.AddAction();
                    action.name = "New Action";
                }

                //Selected
                EditorGUI.BeginDisabledGroup(selectedAction == null);
                {
                    //Move Up
                    if (GUILayout.Button("Move Up"))
                    {
                        var temp = new List<BaseActions.Action>();
                        script.GetActions(temp);

                        var index = temp.IndexOf(selectedAction);
                        script.RemoveAction(selectedAction);
                        script.InsertAction(Mathf.Max(0, index - 1), selectedAction);
                    }

                    //Move Down
                    if (GUILayout.Button("Move Down"))
                    {
                        var temp = new List<BaseActions.Action>();
                        script.GetActions(temp);

                        var index = temp.IndexOf(selectedAction);
                        script.RemoveAction(selectedAction);
                        script.InsertAction(Mathf.Min(temp.Count - 1, index + 1), selectedAction);
                    }

                    //Move Down
                    if (GUILayout.Button("Duplicate"))
                    {
                        var action = script.AddAction();
                        selectedAction.CopyTo(action);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            //Draw actions
            var actions = new List<BaseActions.Action>();
            script.GetActions(actions);
            DrawActions(actions);

            Divider();

            //Action Info
            if (selectedAction != null)
            {
                EditorGUI.BeginDisabledGroup(!selectedAction.enabled);
                Inspector_Action_Header(selectedAction);
                Inspector_Action_Body(selectedAction);
                EditorGUI.EndDisabledGroup();
            }
        }
        public virtual void Inspector_Action_Header(BaseActions.Action action)
        {
            //Name
            action.name = EditorGUILayout.TextField("Name", action.name);
        }
        public virtual void Inspector_Action_Body(BaseActions.Action action, bool showParam = true)
        {
            //Transitions
            action.fadeIn = EditorGUILayout.FloatField("Fade In", action.fadeIn);
            if(script.ActionsHaveExit())
            {
                action.hold = EditorGUILayout.FloatField("Hold", action.hold);
                action.fadeOut = EditorGUILayout.FloatField("Fade Out", action.fadeOut);
            }

            //Properties
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUI.indentLevel += 1;
                {
                    action.foldoutToggleObjects = EditorGUILayout.Foldout(action.foldoutToggleObjects, Title("Object Properties", action.objectProperties.Count > 0));
                    if (action.foldoutToggleObjects)
                    {
                        /*var clip = (AnimationClip)EditorGUILayout.ObjectField("Clip", null, typeof(AnimationClip), false);
                        if(clip != null)
                        {
                            var bindings = AnimationUtility.GetCurveBindings(clip);
                            Debug.Log("Bindings");
                            foreach(var binding in bindings)
                            {
                                Debug.Log("Name:" + binding.propertyName);
                                Debug.Log("Path:" + binding.path);
                            }
                        }*/

                        //Add
                        EditorGUILayout.BeginHorizontal();
                        {
                            //Add
                            if (GUILayout.Button("Add"))
                                action.objectProperties.Add(new ObjectProperty());
                        }
                        EditorGUILayout.EndHorizontal();

                        //Properties
                        for (int i = 0; i < action.objectProperties.Count; i++)
                        {
                            var property = action.objectProperties[i];
                            EditorGUILayout.BeginVertical(GUI.skin.box);
                            {
                                //Header
                                EditorGUILayout.BeginHorizontal();
                                {
                                    //Object Ref
                                    if(ObjectPropertyReferece(property))
                                    {
                                        //Clear prop data
                                        property.objects = null;
                                        property.values = null;
                                        property.stringValues = null;
                                    }

                                    //Type
                                    property.type = (ObjectProperty.Type)EditorGUILayout.EnumPopup(property.type);

                                    //Delete
                                    if (GUILayout.Button("X", GUILayout.Width(32)))
                                    {
                                        action.objectProperties.RemoveAt(i);
                                        i--;
                                    }
                                }
                                EditorGUILayout.EndHorizontal();

                                //Body
                                if (property.objRef != null)
                                {
                                    switch (property.type)
                                    {
                                        case ObjectProperty.Type.MaterialSwap: MaterialSwapProperty(property); break;
                                        case ObjectProperty.Type.BlendShape: BlendShapeProperty(new BlendShapeProperty(property)); break;
                                        case ObjectProperty.Type.PlayAudio: PlayAudioProperty(new PlayAudioProperty(property)); break;
                                    }
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }


                    }
                }
                EditorGUI.indentLevel -= 1;
                EditorGUILayout.EndVertical();
            }

            bool ObjectPropertyReferece(ObjectProperty property)
            {
                //Object Ref
                EditorGUI.BeginChangeCheck();
                if (property.objRef == null)
                    property.objRef = FindPropertyObject(avatarDescriptor.gameObject, property.path);
                property.objRef = (GameObject)EditorGUILayout.ObjectField("", property.objRef, typeof(GameObject), true, null);
                if (EditorGUI.EndChangeCheck())
                {
                    if (property.objRef != null)
                    {
                        //Get path
                        property.path = FindPropertyPath(property.objRef);
                        if (property.path == null)
                        {
                            property.Clear();
                            EditorUtility.DisplayDialog("Error", "Unable to determine the object's path", "Okay");
                        }
                        else
                            return true;
                    }
                    else
                        property.Clear();
                }
                return false;
            }
            void MaterialSwapProperty(ObjectProperty property)
            {
                //Get object materials
                var mesh = property.objRef.GetComponent<Renderer>();
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
                    if (property.objects == null || property.objects.Length != materials.Length)
                        property.objects = new UnityEngine.Object[materials.Length];

                    //Materials
                    for (int materialIter = 0; materialIter < materials.Length; materialIter++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("Material", GUILayout.MaxWidth(100));
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(materials[materialIter], typeof(Material), false);
                            EditorGUI.EndDisabledGroup();
                            property.objects[materialIter] = EditorGUILayout.ObjectField(property.objects[materialIter], typeof(Material), false) as Material;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            void BlendShapeProperty(BlendShapeProperty property)
            {
                var skinnedRenderer = property.objRef.GetComponent<SkinnedMeshRenderer>();
                if (skinnedRenderer == null)
                {
                    EditorGUILayout.HelpBox("GameObject doesn't have a MeshFilter or SkinnedMeshRenderer component.", MessageType.Error);
                    return;
                }

                if (skinnedRenderer.name == "Body" || property.prop.path == "Body")
                {
                    property.prop.objRef = skinnedRenderer.transform.parent.Find("Face").gameObject;
                    property.prop.path = "Face";
                    skinnedRenderer = property.objRef.GetComponent<SkinnedMeshRenderer>();
                    EditorUtility.SetDirty(script);
                }

                //Get mesh
                Mesh mesh = skinnedRenderer.sharedMesh;

                //Setup data
                property.Setup();

                var popup = new string[mesh.blendShapeCount];
                for (int i = 0; i < mesh.blendShapeCount; i++)
                    popup[i] = mesh.GetBlendShapeName(i);

                //Editor
                EditorGUILayout.BeginHorizontal();
                {
                    int index = property.Name != null ? mesh.GetBlendShapeIndex(property.Name) : property.Index;
                    //Property
                    property.Index = EditorGUILayout.Popup(index, popup);
                    property.Name = popup[property.Index];

                    //Value
                    EditorGUI.BeginChangeCheck();
                    property.Weight = EditorGUILayout.Slider(property.Weight, 0f, 100f);
                    if(EditorGUI.EndChangeCheck())
                    {
                        //I'd like to preview the change, but preserving the value
                        //TODO
                        //skinnedRenderer.SetBlendShapeWeight((int)property.values[0], property.values[1]);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            void PlayAudioProperty(PlayAudioProperty property)
            {
                //Setup
                property.Setup();

                //Editor
                //EditorGUILayout.BeginHorizontal();
                {
                    //Property
                    EditorGUILayout.BeginHorizontal();
                    {
                        property.AudioClip = (AudioClip)EditorGUILayout.ObjectField("Clip", property.AudioClip, typeof(AudioClip), false);
                        EditorGUILayout.TextField(property.AudioClip != null ? $"{property.AudioClip.length:N2}" : "", GUILayout.Width(64));
                    }
                    EditorGUILayout.EndHorizontal();
                    property.Volume = EditorGUILayout.Slider("Volume", property.Volume, 0f, 1f);
                    property.Spatial = EditorGUILayout.Toggle("Spatial", property.Spatial);
                    EditorGUI.BeginDisabledGroup(!property.Spatial);
                    {
                        property.Near = EditorGUILayout.FloatField("Near", property.Near);
                        property.Far = EditorGUILayout.FloatField("Far", property.Far);
                    }
                    EditorGUI.EndDisabledGroup();
                }
                //EditorGUILayout.EndHorizontal();
            }

            //Animations
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                action.foldoutAnimations = EditorGUILayout.Foldout(action.foldoutAnimations, Title("Animations", action.HasAnimations()));
                if (action.foldoutAnimations)
                {
                    //Action layer
                    if (script.CanUseLayer(BaseActions.AnimationLayer.Action))
                    {
                        EditorGUILayout.LabelField("Action Layer");
                        EditorGUI.indentLevel += 1;
                        action.actionLayerAnimations.enter = DrawAnimationReference("Enter", action.actionLayerAnimations.enter, $"{action.name}_Action_Enter");
                        if (script.ActionsHaveExit())
                            action.actionLayerAnimations.exit = DrawAnimationReference("Exit", action.actionLayerAnimations.exit, $"{action.name}_Action_Exit");
                        EditorGUILayout.HelpBox("Use for transfoming the humanoid skeleton.  You will need to use IK Overrides to disable IK control of body parts.", MessageType.Info);
                        EditorGUI.indentLevel -= 1;
                    }

                    //FX Layer
                    if(script.CanUseLayer(BaseActions.AnimationLayer.FX))
                    {
                        EditorGUILayout.LabelField("FX Layer");
                        EditorGUI.indentLevel += 1;
                        action.fxLayerAnimations.enter = DrawAnimationReference("Enter", action.fxLayerAnimations.enter, $"{action.name}_FX_Enter");
                        if (script.ActionsHaveExit())
                            action.fxLayerAnimations.exit = DrawAnimationReference("Exit", action.fxLayerAnimations.exit, $"{action.name}_FX_Exit");
                        EditorGUILayout.HelpBox("Use for most everything else, including bones not part of the humanoid skeleton.", MessageType.Info);
                        EditorGUI.indentLevel -= 1;
                    }
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Parameter Drivers
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                action.foldoutParameterDrivers = EditorGUILayout.Foldout(action.foldoutParameterDrivers, Title("Parameter Drivers", action.parameterDrivers.Count > 0));
                if (action.foldoutParameterDrivers)
                {
                    //Add
                    if (GUILayout.Button("Add"))
                    {
                        action.parameterDrivers.Add(new BaseActions.Action.ParameterDriver());
                    }

                    //Drivers
                    for (int i = 0; i < action.parameterDrivers.Count; i++)
                    {
                        var parameter = action.parameterDrivers[i];
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        EditorGUILayout.BeginHorizontal();
                        {
                            parameter.type = (BaseActions.Action.ParameterDriver.Type)EditorGUILayout.EnumPopup("Type", parameter.type);
                            if(GUILayout.Button("X", GUILayout.Width(32)))
                            {
                                action.parameterDrivers.RemoveAt(i);
                                i--;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        if (parameter.type == BaseActions.Action.ParameterDriver.Type.RawValue)
                        {
                            parameter.name = DrawParameterDropDown(parameter.name, "Parameter");
                            EditorGUILayout.BeginHorizontal();
                            {
                                //EditorGUILayout.LabelField("Value");
                                parameter.changeType = (VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType)EditorGUILayout.EnumPopup("Value", parameter.changeType);
                                if (parameter.changeType == VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Random)
                                {
                                    parameter.valueMin = EditorGUILayout.FloatField(parameter.valueMin, GUILayout.MaxWidth(98));
                                    parameter.valueMax = EditorGUILayout.FloatField(parameter.valueMax, GUILayout.MaxWidth(98));
                                }
                                else
                                    parameter.value = EditorGUILayout.FloatField(parameter.value, GUILayout.MaxWidth(200));
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        else if(parameter.type == BaseActions.Action.ParameterDriver.Type.MenuToggle)
                        {
                            parameter.name = EditorGUILayout.TextField("Menu Action", parameter.name);
                            var value = parameter.value == 0 ? BaseActions.OnOffEnum.Off : BaseActions.OnOffEnum.On;
                            parameter.value = System.Convert.ToInt32(EditorGUILayout.EnumPopup("Value", value));
                        }
                        else if(parameter.type == BaseActions.Action.ParameterDriver.Type.MenuRandom)
                        {
                            parameter.name = DrawParameterDropDown(parameter.name, "Parameter");
                            parameter.isZeroValid = EditorGUILayout.Toggle("Is Zero Valid", parameter.isZeroValid);
                        }
                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.HelpBox("Raw Value - Set a parameter to a specific value.", MessageType.Info);
                    EditorGUILayout.HelpBox("Menu Toggle - Toggles a menu action by name.", MessageType.Info);
                    EditorGUILayout.HelpBox("Menu Random - Enables a random menu action driven by this parameter.", MessageType.Info);
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Body Overrides
            EditorGUI.indentLevel += 1;
            EditorGUILayout.BeginVertical(GUI.skin.box);
            action.foldoutIkOverrides = EditorGUILayout.Foldout(action.foldoutIkOverrides, Title("IK Overrides", action.bodyOverride.HasAny()));
            if (action.foldoutIkOverrides)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Toggle On"))
                {
                    action.bodyOverride.SetAll(true);
                }
                if (GUILayout.Button("Toggle Off"))
                {
                    action.bodyOverride.SetAll(false);
                }
                EditorGUILayout.EndHorizontal();

                action.bodyOverride.head = EditorGUILayout.Toggle("Head", action.bodyOverride.head);
                action.bodyOverride.leftHand = EditorGUILayout.Toggle("Left Hand", action.bodyOverride.leftHand);
                action.bodyOverride.rightHand = EditorGUILayout.Toggle("Right Hand", action.bodyOverride.rightHand);
                action.bodyOverride.hip = EditorGUILayout.Toggle("Hips", action.bodyOverride.hip);
                action.bodyOverride.leftFoot = EditorGUILayout.Toggle("Left Foot", action.bodyOverride.leftFoot);
                action.bodyOverride.rightFoot = EditorGUILayout.Toggle("Right Foot", action.bodyOverride.rightFoot);
                action.bodyOverride.leftFingers = EditorGUILayout.Toggle("Left Fingers", action.bodyOverride.leftFingers);
                action.bodyOverride.rightFingers = EditorGUILayout.Toggle("Right Fingers", action.bodyOverride.rightFingers);
                action.bodyOverride.eyes = EditorGUILayout.Toggle("Eyes", action.bodyOverride.eyes);
                action.bodyOverride.mouth = EditorGUILayout.Toggle("Mouth", action.bodyOverride.mouth);
            }
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel -= 1;

            //Triggers
            DrawInspector_Triggers(action);
        }

        public void DrawInspector_Triggers(BaseActions.Action action)
        {
            //Enter Triggers
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            action.foldoutTriggers = EditorGUILayout.Foldout(action.foldoutTriggers, Title("Triggers", action.triggers.Count > 0));
            if (action.foldoutTriggers)
            {
                //Header
                if (GUILayout.Button("Add Trigger"))
                    action.triggers.Add(new BaseActions.Action.Trigger());

                //Triggers
                for (int triggerIter = 0; triggerIter < action.triggers.Count; triggerIter++)
                {
                    //Foldout
                    var trigger = action.triggers[triggerIter];
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    {
                        EditorGUILayout.BeginHorizontal();
                        trigger.foldout = EditorGUILayout.Foldout(trigger.foldout, "Trigger");
                        if(GUILayout.Button("Up", GUILayout.Width(48)))
                        {
                            if(triggerIter > 0)
                            {
                                action.triggers.RemoveAt(triggerIter);
                                action.triggers.Insert(triggerIter - 1, trigger);
                            }
                        }
                        if (GUILayout.Button("Down", GUILayout.Width(48)))
                        {
                            if (triggerIter < action.triggers.Count-1)
                            {
                                action.triggers.RemoveAt(triggerIter);
                                action.triggers.Insert(triggerIter + 1, trigger);
                            }
                        }
                        if (GUILayout.Button("X", GUILayout.Width(32)))
                        {
                            action.triggers.RemoveAt(triggerIter);
                            triggerIter -= 1;
                            continue;
                        }
                        EditorGUILayout.EndHorizontal();
                        if (trigger.foldout)
                        {
                            //Type
                            trigger.type = (BaseActions.Action.Trigger.Type)EditorGUILayout.EnumPopup("Type", trigger.type);

                            //Conditions
                            if (GUILayout.Button("Add Condition"))
                                trigger.conditions.Add(new BaseActions.Action.Condition());

                            //Each Conditions
                            for (int conditionIter = 0; conditionIter < trigger.conditions.Count; conditionIter++)
                            {
                                var condition = trigger.conditions[conditionIter];
                                if (!DrawInspector_Condition(condition))
                                {
                                    trigger.conditions.RemoveAt(conditionIter);
                                    conditionIter -= 1;
                                }
                            }

                            if (trigger.conditions.Count == 0)
                            {
                                EditorGUILayout.HelpBox("Triggers without any conditions default to true.", MessageType.Warning);
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                } //End loop
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }
        public bool DrawInspector_Condition(BaseActions.Action.Condition trigger)
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            {
                //Type
                trigger.type = (BaseActions.ParameterEnum)EditorGUILayout.EnumPopup(trigger.type);

                //Parameter
                if (trigger.type == BaseActions.ParameterEnum.Custom)
                    trigger.parameter = EditorGUILayout.TextField(trigger.parameter);
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(trigger.GetParameter());
                    EditorGUI.EndDisabledGroup();
                }

                //Logic
                switch (trigger.type)
                {
                    case BaseActions.ParameterEnum.Custom:
                        trigger.logic = (BaseActions.Action.Condition.Logic)EditorGUILayout.EnumPopup(trigger.logic);
                        break;
                    case BaseActions.ParameterEnum.Upright:
                    case BaseActions.ParameterEnum.AngularY:
                    case BaseActions.ParameterEnum.VelocityX:
                    case BaseActions.ParameterEnum.VelocityY:
                    case BaseActions.ParameterEnum.VelocityZ:
                    case BaseActions.ParameterEnum.GestureRightWeight:
                    case BaseActions.ParameterEnum.GestureLeftWeight:
                        trigger.logic = (BaseActions.Action.Condition.Logic)EditorGUILayout.EnumPopup((BaseActions.Action.Condition.LogicCompare)trigger.logic);
                        break;
                    default:
                        trigger.logic = (BaseActions.Action.Condition.Logic)EditorGUILayout.EnumPopup((BaseActions.Action.Condition.LogicEquals)trigger.logic);
                        break;
                }

                //Value
                switch (trigger.type)
                {
                    case BaseActions.ParameterEnum.Custom:
                    case BaseActions.ParameterEnum.Upright:
                    case BaseActions.ParameterEnum.AngularY:
                    case BaseActions.ParameterEnum.VelocityX:
                    case BaseActions.ParameterEnum.VelocityY:
                    case BaseActions.ParameterEnum.VelocityZ:
                    case BaseActions.ParameterEnum.GestureRightWeight:
                    case BaseActions.ParameterEnum.GestureLeftWeight:
                        trigger.value = EditorGUILayout.FloatField(trigger.value);
                        break;
                    case BaseActions.ParameterEnum.GestureLeft:
                    case BaseActions.ParameterEnum.GestureRight:
                        trigger.value = System.Convert.ToInt32(EditorGUILayout.EnumPopup((BaseActions.GestureEnum)(int)trigger.value));
                        break;
                    case BaseActions.ParameterEnum.Viseme:
                        trigger.value = System.Convert.ToInt32(EditorGUILayout.EnumPopup((BaseActions.VisemeEnum)(int)trigger.value));
                        break;
                    case BaseActions.ParameterEnum.TrackingType:
                        trigger.value = System.Convert.ToInt32(EditorGUILayout.EnumPopup((BaseActions.TrackingTypeEnum)(int)trigger.value));
                        break;
                    case BaseActions.ParameterEnum.AFK:
                    case BaseActions.ParameterEnum.MuteSelf:
                    case BaseActions.ParameterEnum.InStation:
                    case BaseActions.ParameterEnum.IsLocal:
                    case BaseActions.ParameterEnum.Grounded:
                    case BaseActions.ParameterEnum.Seated:
                        EditorGUI.BeginDisabledGroup(true);
                        trigger.value = 1;
                        EditorGUILayout.TextField("True");
                        EditorGUI.EndDisabledGroup();
                        break;
                    case BaseActions.ParameterEnum.VRMode:
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.IntField(1);
                        EditorGUI.EndDisabledGroup();
                        break;
                }

                if (GUILayout.Button("X", GUILayout.Width(32)))
                {
                    return false;
                }
            }
            EditorGUILayout.EndHorizontal();
            return true;
        }

        void DrawActions(List<BaseActions.Action> actions)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                bool isSelected = selectedAction == action;

                //Draw header
                var headerRect = EditorGUILayout.BeginHorizontal(isSelected ? boxSelected : boxUnselected);
                {
                    EditorGUILayout.LabelField(action.name);
                    GUILayout.FlexibleSpace();
                    action.enabled = EditorGUILayout.Toggle(action.enabled, GUILayout.Width(32));

                    if (GUILayout.Button("X", GUILayout.Width(32)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Action?", $"Delete the action '{action.name}'?", "Delete", "Cancel"))
                        {
                            script.RemoveAction(action);
                            if (isSelected)
                                SelectAction(null);
                            i -= 1;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                //Selection
                if (Event.current.type == EventType.MouseDown)
                {
                    if (headerRect.Contains(Event.current.mousePosition))
                    {
                        SelectAction(action);
                        Event.current.Use();
                    }
                }
            }
        }
        void SelectAction(BaseActions.Action action)
        {
            if (selectedAction != action)
            {
                selectedAction = action;
                Repaint();
                GUI.FocusControl(null);
            }
        }

#region HelperMethods
        public static string Title(string name, bool isModified)
        {
            return name + (isModified ? "*" : "");
        }
        protected UnityEngine.AnimationClip DrawAnimationReference(string name, UnityEngine.AnimationClip clip, string newAssetName)
        {
            EditorGUILayout.BeginHorizontal();
            {
                clip = (UnityEngine.AnimationClip)EditorGUILayout.ObjectField(name, clip, typeof(UnityEngine.AnimationClip), false);
                EditorGUI.BeginDisabledGroup(clip != null);
                {
                    if (GUILayout.Button("New", GUILayout.Width(SmallButtonSize)))
                    {
                        //Create animation    
                        clip = new AnimationClip();
                        clip.name = newAssetName;
                        BaseActions.SaveAsset(clip, this.script as BaseActions, "", true);
                    }
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(clip == null);
                {
                    if (GUILayout.Button("Edit", GUILayout.Width(SmallButtonSize)))
                    {
                        EditAnimation(clip);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            //Return
            return clip;
        }
        void EditAnimation(UnityEngine.AnimationClip clip)
        {
            //Add clip source
            var clipSource = avatarDescriptor.gameObject.GetComponent<ClipSource>();
            if (clipSource == null)
                clipSource = avatarDescriptor.gameObject.AddComponent<ClipSource>();

            clipSource.clips.Clear();
            clipSource.clips.Add(clip);

            //Select the root object
            Selection.activeObject = avatarDescriptor.gameObject;

            //Open the animation window
            EditorApplication.ExecuteMenuItem("Window/Animation/Animation");
        }
        public static GameObject FindPropertyObject(GameObject root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            return root.transform.Find(path)?.gameObject;
        }
        string FindPropertyPath(GameObject obj)
        {
            string path = obj.name;
            while (true)
            {
                obj = obj.transform.parent?.gameObject;
                if (obj == null)
                    return null;
                if (obj.GetComponent<VRCAvatarDescriptor>() != null) //Stop at the avatar descriptor
                    break;
                path = $"{obj.name}/{path}";
            }
            return path;
        }
#endregion
    }
}
#endif