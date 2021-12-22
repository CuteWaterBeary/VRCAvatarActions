#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

namespace VRCAvatarActions
{
    public abstract partial class BaseActions
    {
        [System.Serializable]
        public class Action
        {
            //Simple Data
            public bool enabled = true;
            public string name;

            //Animations
            [System.Serializable]
            public class Animations
            {
                //Source
                public AnimationClip enter;
                public AnimationClip exit;
            }
            public Animations actionLayerAnimations = new Animations();
            public Animations fxLayerAnimations = new Animations();
            public float fadeIn = 0;
            public float hold = 0;
            public float fadeOut = 0;

            public bool HasAnimations()
            {
                return actionLayerAnimations.enter != null || actionLayerAnimations.exit || fxLayerAnimations.enter != null || fxLayerAnimations.exit;
            }
            public bool AffectsAnyLayer()
            {
                bool result = false;
                result |= AffectsLayer(AnimationLayer.Action);
                result |= AffectsLayer(AnimationLayer.FX);
                return result;
            }
            public bool AffectsLayer(AnimationLayer layerType)
            {
                if (GetAnimationRaw(layerType, true) != null)
                    return true;
                if (GeneratesLayer(layerType))
                    return true;
                return false;
            }
            public bool GeneratesLayer(AnimationLayer layerType)
            {
                if (layerType == AnimationLayer.FX)
                {
                    if (objectProperties.Count > 0)
                        return true;
                    if (parameterDrivers.Count > 0)
                        return true;
                }
                return false;
            }
            public virtual bool HasExit()
            {
                return true;
            }
            public virtual bool ShouldBuild()
            {
                if (!enabled)
                    return false;
                return true;
            }

            //Object Properties
            public List<ObjectProperty> objectProperties = new List<ObjectProperty>();

            //Drive Parameters
            [System.Serializable]
            public class ParameterDriver
            {
                public enum Type
                {
                    RawValue = 4461,
                    MenuToggle = 3632,
                    MenuRandom = 9065,
                }

                public ParameterDriver() { }
                public ParameterDriver(ParameterDriver source)
                {
                    type = source.type;
                    name = source.name;
                    value = source.value;
                }

                public Type type = Type.RawValue;
                public string name;
                public float value = 1f;

                public VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType changeType = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
                public float valueMin = 0;
                public float valueMax = 1;
                public float chance = 0.5f;
                public bool isZeroValid = true;
            }
            public List<ParameterDriver> parameterDrivers = new List<ParameterDriver>();

            //Triggers
            [System.Serializable]
            public class Trigger
            {
                public Trigger()
                {
                }
                public Trigger(Trigger source)
                {
                    type = source.type;
                    foldout = source.foldout;
                    foreach (var item in source.conditions)
                        conditions.Add(new Condition(item));
                }
                public enum Type
                {
                    Enter = 0,
                    Exit = 1,
                }
                public Type type;
                public List<Condition> conditions = new List<Condition>();
                public bool foldout;
            }

            [System.Serializable]
            public class Condition
            {
                public Condition()
                {
                }
                public Condition(Condition source)
                {
                    type = source.type;
                    parameter = string.Copy(source.parameter);
                    logic = source.logic;
                    value = source.value;
                    shared = source.shared;
                }
                public enum Logic
                {
                    Equals = 0,
                    NotEquals = 1,
                    GreaterThen = 2,
                    LessThen = 3,
                }
                public enum LogicEquals
                {
                    Equals = 0,
                    NotEquals = 1,
                }
                public enum LogicCompare
                {
                    GreaterThen = 2,
                    LessThen = 3,
                }

                public string GetParameter()
                {
                    if (type == ParameterEnum.Custom)
                        return parameter;
                    else
                        return type.ToString();
                }

                public ParameterEnum type;
                public string parameter;
                public Logic logic = Logic.Equals;
                public float value = 1;
                public bool shared = false;
            }
            public List<Trigger> triggers = new List<Trigger>();

            [System.Serializable]
            public struct BodyOverride
            {
                public bool head;
                public bool leftHand;
                public bool rightHand;
                public bool hip;
                public bool leftFoot;
                public bool rightFoot;
                public bool leftFingers;
                public bool rightFingers;
                public bool eyes;
                public bool mouth;

                public BodyOverride(BodyOverride source)
                {
                    head = source.head;
                    leftHand = source.leftHand;
                    rightHand = source.rightHand;
                    hip = source.hip;
                    leftFoot = source.leftFoot;
                    rightFoot = source.rightFoot;
                    leftFingers = source.leftFingers;
                    rightFingers = source.rightFingers;
                    eyes = source.eyes;
                    mouth = source.mouth;
                }
                public void SetAll(bool value)
                {
                    head = value;
                    leftHand = value;
                    rightHand = value;
                    hip = value;
                    leftFoot = value;
                    rightFoot = value;
                    leftFingers = value;
                    rightFingers = value;
                    eyes = value;
                    mouth = value;
                }
                public bool GetAll()
                {
                    return
                        head &&
                        leftHand &&
                        rightHand &&
                        hip &&
                        leftFoot &&
                        rightFoot &&
                        leftFingers &&
                        rightFingers &&
                        eyes &&
                        mouth;
                }
                public bool HasAny()
                {
                    return head || leftHand || rightHand || hip || leftFoot || rightFoot || leftFingers || rightFingers || eyes || mouth;
                }
            }
            public BodyOverride bodyOverride = new BodyOverride();

            //Build
            public virtual string GetLayerGroup()
            {
                return null;
            }
            public void AddTransitions(AnimatorController controller, AnimatorState lastState, AnimatorState state, float transitionTime, Trigger.Type triggerType, MenuActions.MenuAction parentAction)
            {
                //Find valid triggers
                List<Trigger> triggers = new List<Trigger>();
                foreach (var trigger in this.triggers)
                {
                    if (trigger.type == triggerType)
                        triggers.Add(trigger);
                }

                bool controlEquals = triggerType != Trigger.Type.Exit;

                //Add triggers
                if (triggers.Count > 0)
                {
                    //Add each transition
                    foreach (var trigger in triggers)
                    {
                        //Check type
                        if (trigger.type != triggerType)
                            continue;

                        //Add
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = transitionTime;
                        AddCondition(transition, controlEquals);

                        //Conditions
                        AddTriggerConditions(controller, transition, trigger.conditions);

                        //Parent Conditions - Enter
                        if (triggerType == Trigger.Type.Enter && parentAction != null)
                            parentAction.AddCondition(transition, controlEquals);

                        //Finalize
                        Finalize(transition);
                    }
                }
                else
                {
                    if (triggerType == Trigger.Type.Enter)
                    {
                        //Add single transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = transitionTime;
                        AddCondition(transition, controlEquals);

                        //Parent Conditions
                        if (parentAction != null)
                            parentAction.AddCondition(transition, controlEquals);

                        //Finalize
                        Finalize(transition);
                    }
                    else if (triggerType == Trigger.Type.Exit && HasExit())
                    {
                        //Add single transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = transitionTime;
                        AddCondition(transition, controlEquals);

                        //Finalize
                        Finalize(transition);
                    }
                }

                //Parent Conditions - Exit
                if (triggerType == Trigger.Type.Exit && parentAction != null)
                {
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = transitionTime;
                    parentAction.AddCondition(transition, controlEquals);
                }

                void Finalize(AnimatorStateTransition transition)
                {
                    if (transition.conditions.Length == 0)
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");
                }
            }
            public virtual void AddCondition(AnimatorStateTransition transition, bool equals)
            {
                //Nothing
            }

            //Metadata
            public bool foldoutMain = false;
            public bool foldoutTriggers = false;
            public bool foldoutIkOverrides = false;
            public bool foldoutToggleObjects = false;
            public bool foldoutMaterialSwaps = false;
            public bool foldoutAnimations = false;
            public bool foldoutParameterDrivers = false;

            AnimationClip GetAnimationRaw(AnimationLayer layer, bool enter = true)
            {
                //Find layer group
                Action.Animations group;
                if (layer == AnimationLayer.Action)
                    group = actionLayerAnimations;
                else if (layer == AnimationLayer.FX)
                    group = fxLayerAnimations;
                else
                    return null;

                if (enter)
                    return group.enter;
                else
                {
                    return group.exit != null ? group.exit : group.enter;
                }
            }
            public AnimationClip GetAnimation(AnimationLayer layer, bool enter = true)
            {
                //Find layer group
                Animations group;
                if (layer == AnimationLayer.Action)
                    group = actionLayerAnimations;
                else if (layer == AnimationLayer.FX)
                    group = fxLayerAnimations;
                else
                    return null;

                //Return
                return enter ? GetEnter() : GetExit();

                AnimationClip GetEnter()
                {
                    //Find/Generate
                    if (GeneratesLayer(layer))
                        return FindOrGenerate(name + "_Generated", group.enter);
                    else
                        return group.enter;
                }
                AnimationClip GetExit()
                {
                    //Fallback to enter
                    if (group.exit == null)
                        return GetEnter();

                    //Find/Generate
                    if (GeneratesLayer(layer))
                        return FindOrGenerate(name + "_Generated_Exit", group.exit);
                    else
                        return group.exit;
                }
            }
            protected AnimationClip FindOrGenerate(string clipName, AnimationClip parentClip)
            {
                //Find/Generate
                if (GeneratedClips.TryGetValue(clipName, out AnimationClip generated))
                    return generated;
                else
                {
                    //Generate
                    generated = BuildGeneratedAnimation(clipName, parentClip);
                    if (generated != null)
                    {
                        GeneratedClips.Add(clipName, generated);
                        return generated;
                    }
                    else
                        return parentClip;
                }
            }
            protected AnimationClip BuildGeneratedAnimation(string clipName, AnimationClip source)
            {
                try
                {
                    //Create new animation
                    AnimationClip animation = null;
                    if (source != null)
                    {
                        animation = new AnimationClip();
                        EditorUtility.CopySerialized(source, animation);
                    }
                    else
                        animation = new AnimationClip();

                    //Properties
                    foreach (var item in objectProperties)
                    {
                        //Is anything defined?
                        if (string.IsNullOrEmpty(item.path))
                            continue;

                        //Find object
                        item.objRef = BaseActionsEditor.FindPropertyObject(AvatarDescriptor.gameObject, item.path);
                        if (item.objRef == null)
                            continue;

                        switch (item.type)
                        {
                            case ObjectProperty.Type.ObjectToggle: new ObjectToggleProperty(item).AddKeyframes(animation); break;
                            case ObjectProperty.Type.MaterialSwap: new MaterialSwapProperty(item).AddKeyframes(animation); break;
                            case ObjectProperty.Type.BlendShape: new BlendShapeProperty(item).AddKeyframes(animation); break;
                            case ObjectProperty.Type.PlayAudio: new PlayAudioProperty(item).AddKeyframes(animation); break;
                        }
                    }

                    //Save
                    animation.name = clipName;
                    SaveAsset(animation, ActionsDescriptor.ReturnAnyScriptableObject(), "Generated");

                    //Return
                    return animation;
                }
                catch(System.Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError($"Error while trying to generate animation '{clipName}'");
                    return null;
                }
            }

            public virtual void CopyTo(Action clone)
            {
                //Generic
                clone.name = string.Copy(name);
                clone.enabled = enabled;

                //Animation
                clone.actionLayerAnimations.enter = actionLayerAnimations.enter;
                clone.actionLayerAnimations.exit = actionLayerAnimations.exit;
                clone.fxLayerAnimations.enter = fxLayerAnimations.enter;
                clone.fxLayerAnimations.exit = fxLayerAnimations.exit;
                clone.fadeIn = fadeIn;
                clone.hold = hold;
                clone.fadeOut = fadeOut;

                //Object Properties
                clone.objectProperties.Clear();
                foreach (var prop in objectProperties)
                    clone.objectProperties.Add(new ObjectProperty(prop));

                //Parameter drivers
                clone.parameterDrivers.Clear();
                foreach (var driver in parameterDrivers)
                    clone.parameterDrivers.Add(new ParameterDriver(driver));

                //Triggers
                clone.triggers.Clear();
                foreach (var trigger in triggers)
                    clone.triggers.Add(new Action.Trigger(trigger));

                //Body overrides
                clone.bodyOverride = new BodyOverride(bodyOverride);

                //Meta
                clone.foldoutMain = foldoutMain;
                clone.foldoutTriggers = foldoutTriggers;
                clone.foldoutIkOverrides = foldoutIkOverrides;
                clone.foldoutToggleObjects = foldoutToggleObjects;
                clone.foldoutMaterialSwaps = foldoutMaterialSwaps;
                clone.foldoutAnimations = foldoutAnimations;
            }
        }
    }
}
#endif