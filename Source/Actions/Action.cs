#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
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
            [System.Serializable]
            public class Animations
            {
                //Source
                public AnimationClip enter;
                public AnimationClip exit;
            }

            [System.Serializable]
            public class ParameterDriver
            {
                public enum Type
                {
                    RawValue = 4461,
                    MenuToggle = 3632,
                    MenuRandom = 9065,
                }

                public Type type = Type.RawValue;
                public string name;
                public float value = 1f;

                public VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType changeType = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
                public float valueMin = 0;
                public float valueMax = 1;
                public float chance = 0.5f;
                public bool isZeroValid = true;

                public ParameterDriver() {}
                public ParameterDriver(ParameterDriver source)
                {
                    type = source.type;
                    name = source.name;
                    value = source.value;
                }
            }

            [System.Serializable]
            public class Trigger
            {
                public enum Type
                {
                    Enter = 0,
                    Exit = 1,
                }

                public Type type;
                public List<Condition> conditions = new List<Condition>();
                public bool foldout;

                public Trigger() {}
                public Trigger(Trigger source)
                {
                    type = source.type;
                    foldout = source.foldout;
                    foreach (var item in source.conditions)
                        conditions.Add(new Condition(item));
                }
            }

            [System.Serializable]
            public class Condition
            {
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

                public ParameterEnum type;
                public string parameter;
                public Logic logic = Logic.Equals;
                public float value = 1;
                public bool shared = false;

                public string GetParameter() => type == ParameterEnum.Custom ? parameter : type.ToString();

                public Condition() {}
                public Condition(Condition source)
                {
                    type = source.type;
                    parameter = string.Copy(source.parameter);
                    logic = source.logic;
                    value = source.value;
                    shared = source.shared;
                }
            }

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

                public bool GetAll() => head && leftHand && rightHand && hip && leftFoot && rightFoot && leftFingers && rightFingers && eyes && mouth;
                public bool HasAny() => head || leftHand || rightHand || hip || leftFoot || rightFoot || leftFingers || rightFingers || eyes || mouth;
            }

            // END OF CLASSES AND STRUCTS

            //Simple Data
            public bool enabled = true;
            public string name;

            //Animations
            public Animations actionLayerAnimations = new Animations();
            public Animations fxLayerAnimations = new Animations();
            public float fadeIn = 0;
            public float hold = 0;
            public float fadeOut = 0;

            public List<ObjectProperty> objectProperties = new List<ObjectProperty>();
            public List<ParameterDriver> parameterDrivers = new List<ParameterDriver>();
            public List<Trigger> triggers = new List<Trigger>();

            public BodyOverride bodyOverride = new BodyOverride();


            public bool HasAnimations() => actionLayerAnimations.enter != null || actionLayerAnimations.exit || fxLayerAnimations.enter != null || fxLayerAnimations.exit;

            public bool AffectsAnyLayer()
            {
                bool result = false;
                result |= AffectsLayer(AnimationLayer.Action);
                result |= AffectsLayer(AnimationLayer.FX);
                return result;
            }

            public bool AffectsLayer(AnimationLayer layerType) => GetAnimationRaw(layerType, true) != null || GeneratesLayer(layerType);
            public bool GeneratesLayer(AnimationLayer layerType, bool enter = true)
            {
                return layerType == AnimationLayer.FX && (objectProperties.Any(p => p.ToWrapper().ShouldGenerate(enter)) || parameterDrivers.Count > 0);
            }

            public virtual bool HasExit() => true;
            public virtual bool ShouldBuild() => enabled;

            //Build
            public virtual string GetLayerGroup() => null;

            public void AddTransitions(ActionsBuilder builder, AnimatorController controller, AnimatorState lastState, AnimatorState state, float transitionTime, Trigger.Type triggerType, MenuActions.MenuAction parentAction)
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
                        AddCondition(builder, transition, controlEquals);

                        //Conditions
                        builder.AddTriggerConditions(controller, transition, trigger.conditions);

                        //Parent Conditions - Enter
                        if (triggerType == Trigger.Type.Enter && parentAction != null)
                            parentAction.AddCondition(builder, transition, controlEquals);

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
                        AddCondition(builder, transition, controlEquals);

                        //Parent Conditions
                        if (parentAction != null)
                            parentAction.AddCondition(builder, transition, controlEquals);

                        //Finalize
                        Finalize(transition);
                    }
                    else if (triggerType == Trigger.Type.Exit && HasExit())
                    {
                        //Add single transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = transitionTime;
                        AddCondition(builder, transition, controlEquals);

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
                    parentAction.AddCondition(builder, transition, controlEquals);
                }

                void Finalize(AnimatorStateTransition transition)
                {
                    if (transition.conditions.Length == 0)
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");
                }
            }

            public virtual void AddCondition(ActionsBuilder builder, AnimatorStateTransition transition, bool equals)
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
                Animations group;
                if (layer == AnimationLayer.Action)
                    group = actionLayerAnimations;
                else if (layer == AnimationLayer.FX)
                    group = fxLayerAnimations;
                else
                    return null;

                if (enter)
                    return group.enter;
                else
                    return group.exit != null ? group.exit : group.enter;
            }

            public AnimationClip GetAnimation(ActionsBuilder builder, AnimationLayer layer, bool enter = true)
            {
                //Find layer group
                Animations group;
                if (layer == AnimationLayer.Action)
                    group = actionLayerAnimations;
                else if (layer == AnimationLayer.FX)
                    group = fxLayerAnimations;
                else
                    return null;

                AnimationClip clip = GetClip(enter);

                if (enter == false && clip == null)
                {
                    clip = GetClip(true);
                }

                AnimationClip GetClip(bool useEnter)
                {
                    AnimationClip animationClip = enter ? group.enter : group.exit;

                    //Find/Generate
                    return GeneratesLayer(layer, useEnter) ? FindOrGenerate(builder, $"{name}_Generated{(useEnter ? "" : "_Exit")}", animationClip, useEnter) : animationClip;
                }

                return clip;
            }

            protected AnimationClip FindOrGenerate(ActionsBuilder builder, string clipName, AnimationClip parentClip, bool enter)
            {
                //Find/Generate
                if (builder.GeneratedClips.TryGetValue(clipName, out AnimationClip generated))
                {
                    return generated;
                }
                else
                {
                    //Generate
                    generated = BuildGeneratedAnimation(builder, clipName, parentClip, enter);
                    if (generated != null)
                    {
                        builder.GeneratedClips.Add(clipName, generated);
                        return generated;
                    }
                    else
                        return parentClip;
                }
            }

            protected AnimationClip BuildGeneratedAnimation(ActionsBuilder builder, string clipName, AnimationClip source, bool enter)
            {
                try
                {
                    //Create new animation
                    AnimationClip animation = new AnimationClip();
                    if (source != null)
                    {
                        EditorUtility.CopySerialized(source, animation);
                    }

                    //Properties
                    foreach (var item in objectProperties)
                    {
                        //Is anything defined?
                        if (string.IsNullOrEmpty(item.path))
                            continue;

                        //Find object
                        item.objRef = BaseActionsEditor.FindPropertyObject(builder.AvatarDescriptor.gameObject, item.path);
                        if (item.objRef == null)
                            continue;

                        item.ToWrapper().AddKeyframes(builder, this, animation, enter);
                    }

                    //Save
                    animation.name = clipName;
                    builder.BuildFailed = !ActionsBuilder.SaveAsset(animation, builder.ActionsDescriptor.ReturnAnyScriptableObject(), "Generated");

                    //Return
                    return animation;
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError($"Error while trying to generate animation '{clipName}'");
                    return null;
                }
            }

            public void SetState(ActionsBuilder builder)
            {
                foreach (var item in objectProperties)
                {
                    //Is anything defined?
                    if (string.IsNullOrEmpty(item.path))
                        continue;

                    //Find object
                    item.objRef = BaseActionsEditor.FindPropertyObject(builder.AvatarDescriptor.gameObject, item.path);
                    if (item.objRef == null)
                        continue;

                    item.ToWrapper().SetState(builder, this);
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