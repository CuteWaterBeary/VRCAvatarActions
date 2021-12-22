#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using TrackingType = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType;

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

        public virtual bool CanUseLayer(AnimationLayer layer)
        {
            return true;
        }

        public virtual bool ActionsHaveExit()
        {
            return true;
        }

        protected static AvatarDescriptor AvatarDescriptor = null;
        protected static AvatarActions ActionsDescriptor = null;
        protected static List<ExpressionParameters.Parameter> AllParameters = new List<ExpressionParameters.Parameter>();
        protected static AnimatorController ActionController;
        protected static AnimatorController FxController;
        protected static AnimatorController GetController(AnimationLayer layer)
        {
            switch(layer)
            {
                case AnimationLayer.Action:
                    return ActionController;
                case AnimationLayer.FX:
                    return FxController;
            }
            return null;
        }

        protected static bool BuildFailed = false;
        protected static Dictionary<string, AnimationClip> GeneratedClips = new Dictionary<string, AnimationClip>();
        public static Dictionary<string, List<MenuActions.MenuAction>> ParameterToMenuActions = new Dictionary<string, List<MenuActions.MenuAction>>();

        public static void BuildAvatarData(AvatarDescriptor desc, AvatarActions actionsDesc)
        {
            //Store
            AvatarDescriptor = desc;
            ActionsDescriptor = actionsDesc;
            BuildFailed = false;

            //Build
            BuildSetup();
            BuildMain();
            BuildCleanup();

            //Error
            if (BuildFailed)
            {
                EditorUtility.DisplayDialog("Build Failed", "Build has failed.", "Okay");
            }
        }

        public static void BuildSetup()
        {
            //Action Controller
            AvatarDescriptor.customizeAnimationLayers = true;
            ActionController = GetController(AvatarDescriptor.AnimLayerType.Action, "AnimationController_Action");
            FxController = GetController(AvatarDescriptor.AnimLayerType.FX, "AnimationController_FX");

            AnimatorController GetController(AvatarDescriptor.AnimLayerType animLayerType, string name)
            {
                //Find desc layer
                AvatarDescriptor.CustomAnimLayer descLayer = new AvatarDescriptor.CustomAnimLayer();
                int descLayerIndex = 0;
                foreach(var layer in AvatarDescriptor.baseAnimationLayers)
                {
                    if(layer.type == animLayerType)
                    {
                        descLayer = layer;
                        break;
                    }
                    descLayerIndex++;
                }

                //Find/Create Layer
                var controller = descLayer.animatorController as AnimatorController;
                if (controller == null || descLayer.isDefault)
                {
                    //Dir Path
                    var dirPath = AssetDatabase.GetAssetPath(ActionsDescriptor.ReturnAnyScriptableObject());
                    dirPath = dirPath.Replace(Path.GetFileName(dirPath), $"Generated/");
                    System.IO.Directory.CreateDirectory(dirPath);

                    //Create
                    var path = $"{dirPath}{name}.controller";
                    controller = AnimatorController.CreateAnimatorControllerAtPath(path);

                    //Add base layer
                    controller.AddLayer("Base Layer");

                    //Save
                    descLayer.animatorController = controller;
                    descLayer.isDefault = false;
                    AvatarDescriptor.baseAnimationLayers[descLayerIndex] = descLayer;
                    EditorUtility.SetDirty(AvatarDescriptor);
                }

                //Cleanup Layers
                {
                    //Clean layers
                    for(int i=0; i< controller.layers.Length; i++)
                    {
                        if(controller.layers[i].name == "Base Layer")
                            continue;
                        if (ActionsDescriptor.ignoreLayers.Contains(controller.layers[i].name))
                            continue;

                        //Remove
                        controller.RemoveLayer(i);
                        i--;
                    }

                    //Clean parameters
                    for(int i=0; i<controller.parameters.Length; i++)
                    {
                        if (ActionsDescriptor.ignoreParameters.Contains(controller.parameters[i].name))
                            continue;

                        //Remove
                        controller.RemoveParameter(i);
                        i--;
                    }
                }

                //Add defaults
                AddParameter(controller, "True", AnimatorControllerParameterType.Bool, 1);

                //Return
                return controller;
            }

            //Delete all generated animations
            GeneratedClips.Clear();
            /*{
                var dirPath = AssetDatabase.GetAssetPath(ActionsDescriptor.ReturnAnyScriptableObject());
                dirPath = dirPath.Replace(Path.GetFileName(dirPath), $"Generated/");
                var files = System.IO.Directory.GetFiles(dirPath);
                foreach (var file in files)
                {
                    if (file.Contains("_Generated"))
                        System.IO.File.Delete(file);
                }
            }*/

            //Parameters
            InitExpressionParameters();
        }
        public static void BuildMain()
        {
            //Build menu
            if (ActionsDescriptor.menuActions != null)
            {
                ActionsDescriptor.menuActions.Build();
                if (BuildFailed)
                    return;
            }

            //Build others
            foreach (var actionSet in ActionsDescriptor.otherActions)
            {
                if(actionSet != null)
                {
                    actionSet.Build(null);
                    if (BuildFailed)
                        return;
                }
            }
        }

        public static void BuildCleanup()
        {
            var components = AvatarDescriptor.gameObject.GetComponentsInChildren<ITemporaryComponent>();
            foreach (var comp in components)
                GameObject.DestroyImmediate(comp as MonoBehaviour);

            //Save
            EditorUtility.SetDirty(AvatarDescriptor);
            EditorUtility.SetDirty(AvatarDescriptor.expressionsMenu);

            //Save Parameters
            {
                AvatarDescriptor.expressionParameters.parameters = BuildParameters.ToArray();

                foreach (var parameter in AvatarDescriptor.expressionParameters.parameters)
                {
                    var parameterSetting = ActionsDescriptor.expressionParametersStorage.FindParameter(parameter.name);
                    if (parameterSetting != null)
                    {
                        parameter.defaultValue = parameterSetting.defaultValue;
                        parameter.saved = parameterSetting.saved;
                    }
                    else
                    {
                        ActionsDescriptor.expressionParametersStorage.parameters = ActionsDescriptor.expressionParametersStorage.parameters.Append(parameter).ToArray();
                    }
                }

                //Parameter defaults
                foreach (var paramDefault in ActionsDescriptor.parameterDefaults)
                {
                    var param = AvatarDescriptor.expressionParameters.FindParameter(paramDefault.name);
                    if (param != null)
                        param.defaultValue = paramDefault.value;
                }

                //Check parameter count
                var parametersObject = AvatarDescriptor.expressionParameters;
                if (parametersObject.CalcTotalCost() > ExpressionParameters.MAX_PARAMETER_COST)
                {
                    BuildFailed = true;
                    EditorUtility.DisplayDialog("Build Error", $"Unable to build VRCExpressionParameters. Too many parameters defined.", "Okay");
                    return;
                }

                EditorUtility.SetDirty(AvatarDescriptor.expressionParameters);
            }

            //Save prefab
            AssetDatabase.SaveAssets();
        }

        //Parameters
        static protected List<ExpressionParameters.Parameter> BuildParameters = new List<ExpressionParameters.Parameter>();

        static void InitExpressionParameters()
        {
            //Check if parameter object exists
            var parametersObject = AvatarDescriptor.expressionParameters;
            if (AvatarDescriptor.expressionParameters == null || !AvatarDescriptor.customExpressions)
            {
                parametersObject = ScriptableObject.CreateInstance<ExpressionParameters>();
                parametersObject.name = "ExpressionParameters";
                SaveAsset(parametersObject, ActionsDescriptor.ReturnAnyScriptableObject(), "Generated");

                AvatarDescriptor.customExpressions = true;
                AvatarDescriptor.expressionParameters = parametersObject;
            }

            //Clear parameters
            BuildParameters.Clear();
            if(parametersObject.parameters != null)
            {
                foreach (var param in parametersObject.parameters)
                {
                    if (param != null && ActionsDescriptor.ignoreParameters.Contains(param.name))
                        BuildParameters.Add(param);
                }
            }
        }

        protected static void DefineExpressionParameter(ExpressionParameters.Parameter parameter)
        {
            //Check if already exists
            foreach(var param in BuildParameters)
            {
                if (param.name == parameter.name)
                    return;
            }

            //Add
            BuildParameters.Add(parameter);
        }

        protected static ExpressionParameters.Parameter FindExpressionParameter(string name)
        {
            foreach (var param in BuildParameters)
            {
                if (param.name == name)
                    return param;
            }
            return null;
        }

        //Normal
        protected static void BuildActionLayer(AnimatorController controller, IEnumerable<Action> actions, string layerName, MenuActions.MenuAction parentAction, bool turnOffState = true)
        {
            //Prepare layer
            var layer = GetControllerLayer(controller, layerName);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(7, 0);

            //Animation Layer Weight
            int layerIndex = 0;
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == layer.name)
                {
                    layerIndex = i;
                    break;
                }
            }

            //Waiting state
            var waitingState = layer.stateMachine.AddState("Waiting", new Vector3(0, 0, 0));
            if (turnOffState)
            {
                //Animation Layer Weight
                var layerWeight = waitingState.AddStateMachineBehaviour<VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 0;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.Action;
            }
            else
                waitingState.writeDefaultValues = false;

            //Actions
            int actionIter = 0;
            foreach(var action in actions)
            {
                AnimatorState lastState;

                //Enter state
                {
                    var state = layer.stateMachine.AddState(action.name + "_Setup", StatePosition(1, actionIter));
                    state.motion = action.actionLayerAnimations.enter;

                    //Transition
                    action.AddTransitions(controller, waitingState, state, 0, Action.Trigger.Type.Enter, parentAction);

                    //Animation Layer Weight
                    var layerWeight = state.AddStateMachineBehaviour<VRCAnimatorLayerControl>();
                    layerWeight.goalWeight = 1;
                    layerWeight.layer = layerIndex;
                    layerWeight.blendDuration = 0;
                    layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.Action;

                    //Playable Layer
                    var playable = state.AddStateMachineBehaviour<VRCPlayableLayerControl>();
                    playable.layer = VRC.SDKBase.VRC_PlayableLayerControl.BlendableLayer.Action;
                    playable.goalWeight = 1.0f;
                    playable.blendDuration = action.fadeIn;

                    //Tracking
                    SetupTracking(action, state, TrackingType.Animation);

                    //Store
                    lastState = state;
                }

                //Enable state
                {
                    var state = layer.stateMachine.AddState(action.name + "_Enable", StatePosition(2, actionIter));
                    state.motion = action.GetAnimation(AnimationLayer.Action, true);

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0f;
                    transition.duration = action.fadeIn;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                    //Store
                    lastState = state;
                }

                //Hold
                if (action.hold > 0)
                {
                    //Hold
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Hold", StatePosition(3, actionIter));
                        state.motion = action.GetAnimation(AnimationLayer.Action, true);

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = true;
                        transition.exitTime = action.hold;
                        transition.duration = 0;

                        //Store
                        lastState = state;
                    }
                }

                //Disable state
                {
                    var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(4, actionIter));
                    state.motion = action.GetAnimation(AnimationLayer.Action, false);

                    //Transition
                    action.AddTransitions(controller, lastState, state, 0, Action.Trigger.Type.Exit, parentAction);

                    //Playable Layer
                    var playable = state.AddStateMachineBehaviour<VRCPlayableLayerControl>();
                    playable.goalWeight = 0.0f;
                    playable.blendDuration = action.fadeOut;

                    //Store
                    lastState = state;
                }

                //Fadeout state
                if(action.fadeOut > 0)
                {
                    var state = layer.stateMachine.AddState(action.name + "_Fadeout", StatePosition(5, actionIter));
                    state.motion = action.GetAnimation(AnimationLayer.Action, false);

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0;
                    transition.duration = action.fadeOut;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                    //Store
                    lastState = state;
                }

                //Cleanup state
                if(action.bodyOverride.HasAny())
                {
                    var state = layer.stateMachine.AddState(action.name + "_Cleanup", StatePosition(6, actionIter));

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0f;
                    transition.duration = 0f;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                    //Tracking
                    SetupTracking(action, state, TrackingType.Tracking);

                    //Store
                    lastState = state;
                }

                //Exit transition
                {
                    var transition = lastState.AddExitTransition();
                    transition.hasExitTime = false;
                    transition.exitTime = 0f;
                    transition.duration = 0f;
                    transition.AddCondition(AnimatorConditionMode.If, 1, "True");
                }

                //Iterate
                actionIter += 1;
            }
        }
        protected static void BuildNormalLayer(AnimatorController controller, IEnumerable<Action> actions, string layerName, AnimationLayer layerType, MenuActions.MenuAction parentAction, bool turnOffState = true)
        {
            //Prepare layer
            var layer = GetControllerLayer(controller, layerName);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(6, 0);

            //Animation Layer Weight
            var layerIndex = GetLayerIndex(controller, layer);

            //Waiting
            AnimatorState waitingState = layer.stateMachine.AddState("Waiting", new Vector3(0, 0, 0));
            if (turnOffState)
            {
                //Animation Layer Weight
                var layerWeight = waitingState.AddStateMachineBehaviour<VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 0;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;
            }
            else
                waitingState.writeDefaultValues = false;

            //Each action
            int actionIter = 0;
            foreach(var action in actions)
            {
                AnimatorState lastState = waitingState;

                //Enable
                {
                    var state = layer.stateMachine.AddState(action.name + "_Enable", StatePosition(1, actionIter));
                    state.motion = action.GetAnimation(layerType, true);

                    //Transition
                    action.AddTransitions(controller, lastState, state, action.fadeIn, Action.Trigger.Type.Enter, parentAction);

                    //Animation Layer Weight
                    var layerWeight = state.AddStateMachineBehaviour<VRCAnimatorLayerControl>();
                    layerWeight.goalWeight = 1;
                    layerWeight.layer = layerIndex;
                    layerWeight.blendDuration = 0;
                    layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;

                    //Tracking
                    SetupTracking(action, state, TrackingType.Animation);

                    //Parameter Drivers
                    BuildParameterDrivers(action, state);

                    //Store
                    lastState = state;
                }

                //Hold
                if (action.hold > 0)
                {
                    var state = layer.stateMachine.AddState(action.name + "_Hold", StatePosition(2, actionIter));
                    state.motion = action.GetAnimation(layerType, true);

                    //Transition
                    var transition = lastState.AddTransition(state);
                    transition.hasExitTime = true;
                    transition.exitTime = action.hold;
                    transition.duration = 0;

                    //Store
                    lastState = state;
                }

                //Exit
                if (action.HasExit() || parentAction != null)
                {
                    //Disable
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(3, actionIter));
                        state.motion = action.GetAnimation(layerType, false);

                        //Transition
                        action.AddTransitions(controller, lastState, state, 0, Action.Trigger.Type.Exit, parentAction);

                        //Store
                        lastState = state;
                    }

                    //Fadeout
                    if(action.fadeOut > 0)
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Fadeout", StatePosition(4, actionIter));

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = action.fadeOut;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                        //Store
                        lastState = state;
                    }

                    //Cleanup
                    if (action.bodyOverride.HasAny())
                    {
                        var state = layer.stateMachine.AddState(action.name + "_Cleanup", StatePosition(5, actionIter));

                        //Transition
                        var transition = lastState.AddTransition(state);
                        transition.hasExitTime = false;
                        transition.duration = 0;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");

                        //Tracking
                        SetupTracking(action, state, TrackingType.Tracking);

                        //Store
                        lastState = state;
                    }

                    //Transition Exit
                    {
                        var transition = lastState.AddExitTransition();
                        transition.hasExitTime = false;
                        transition.exitTime = 0f;
                        transition.duration = 0f;
                        transition.AddCondition(AnimatorConditionMode.If, 1, "True");
                    }
                }

                //Iterate
                actionIter += 1;
            }
        }

        //Generated
        protected static void BuildGroupedLayers(IEnumerable<Action> sourceActions, AnimationLayer layerType, MenuActions.MenuAction parentAction, System.Func<Action, bool> onCheck, System.Action<AnimatorController, string, List<Action>> onBuild)
        {
            var controller = GetController(layerType);

            //Build layer groups
            List<string> layerGroups = new List<string>();
            foreach (var action in sourceActions)
            {
                var group = action.GetLayerGroup();
                if (!string.IsNullOrEmpty(group) && !layerGroups.Contains(group))
                    layerGroups.Add(group);
            }

            //Build grouped layers
            var layerActions = new List<Action>();
            foreach (var group in layerGroups)
            {
                //Check if valid
                layerActions.Clear();
                foreach (var action in sourceActions)
                {
                    if (action.GetLayerGroup() != group)
                        continue;
                    if (!onCheck(action))
                        continue;
                    layerActions.Add(action);
                }
                if (layerActions.Count == 0)
                    continue;

                //Build
                onBuild(controller, group, layerActions);
            }

            //Build unsorted layers
            foreach (var action in sourceActions)
            {
                if (!string.IsNullOrEmpty(action.GetLayerGroup()))
                    continue;
                if (!onCheck(action))
                    continue;

                layerActions.Clear();
                layerActions.Add(action);
                onBuild(controller, action.name, layerActions);
            }
        }

        //Conditions
        protected static void AddTriggerConditions(AnimatorController controller, AnimatorStateTransition transition, IEnumerable<Action.Condition> conditions)
        {
            foreach (var condition in conditions)
            {
                //Find parameter data
                string paramName = condition.GetParameter();
                AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Int;
                switch (condition.type)
                {
                    //Bool
                    case ParameterEnum.AFK:
                    case ParameterEnum.Seated:
                    case ParameterEnum.Grounded:
                    case ParameterEnum.MuteSelf:
                    case ParameterEnum.InStation:
                    case ParameterEnum.IsLocal:
                        paramType = AnimatorControllerParameterType.Bool;
                        break;
                    //Int
                    case ParameterEnum.Viseme:
                    case ParameterEnum.GestureLeft:
                    case ParameterEnum.GestureRight:
                    case ParameterEnum.VRMode:
                    case ParameterEnum.TrackingType:
                        paramType = AnimatorControllerParameterType.Int;
                        break;
                    //Float
                    case ParameterEnum.GestureLeftWeight:
                    case ParameterEnum.GestureRightWeight:
                    case ParameterEnum.AngularY:
                    case ParameterEnum.VelocityX:
                    case ParameterEnum.VelocityY:
                    case ParameterEnum.VelocityZ:
                        paramType = AnimatorControllerParameterType.Float;
                        break;
                    //Custom
                    case ParameterEnum.Custom:
                    {
                        bool found = false;

                        //Find
                        {
                            var param = FindExpressionParameter(condition.parameter);
                            if(param != null)
                            {
                                switch(param.valueType)
                                {
                                    default:
                                    case ExpressionParameters.ValueType.Int: paramType = AnimatorControllerParameterType.Int; break;
                                    case ExpressionParameters.ValueType.Float: paramType = AnimatorControllerParameterType.Float; break;
                                    case ExpressionParameters.ValueType.Bool: paramType = AnimatorControllerParameterType.Bool; break;
                                }
                                found = true;
                            } 
                        }

                        //Find
                        if(!found)
                        {
                            foreach (var param in controller.parameters)
                            {
                                if (param.name == condition.parameter)
                                {
                                    paramType = param.type;
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if(!found)
                        {
                            Debug.LogError($"AddTriggerConditions, unable to find parameter named:{condition.parameter}");
                            BuildFailed = true;
                            return;
                        }
                        break;
                    }
                    default:
                    {
                        Debug.LogError("AddTriggerConditions, unknown parameter type for trigger condition.");
                        BuildFailed = true;
                        return;
                    }
                }

                //Add parameter
                AddParameter(controller, paramName, paramType, 0);

                //Add condition
                switch (paramType)
                {
                    case AnimatorControllerParameterType.Bool:
                        transition.AddCondition(condition.logic == Action.Condition.Logic.NotEquals ? AnimatorConditionMode.IfNot : AnimatorConditionMode.If, 1f, paramName);
                        break;
                    case AnimatorControllerParameterType.Int:
                        transition.AddCondition(condition.logic == Action.Condition.Logic.NotEquals ? AnimatorConditionMode.NotEqual : AnimatorConditionMode.Equals, condition.value, paramName);
                        break;
                    case AnimatorControllerParameterType.Float:
                        transition.AddCondition(condition.logic == Action.Condition.Logic.LessThen ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater, condition.value, paramName);
                        break;
                }
            }

            //Default true
            if (transition.conditions.Length == 0)
                transition.AddCondition(AnimatorConditionMode.If, 1f, "True");
        }

        protected static void BuildParameterDrivers(Action action, AnimatorState state)
        {
            if (action.parameterDrivers.Count == 0)
                return;

            var driverBehaviour = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driverBehaviour.localOnly = true;
            foreach(var driver in action.parameterDrivers)
            {
                if (string.IsNullOrEmpty(driver.name))
                    continue;

                //Build param
                var param = new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter();
                if (driver.type == Action.ParameterDriver.Type.RawValue)
                {
                    param.name = driver.name;
                    param.value = driver.value;
                    param.type = driver.changeType;
                    param.valueMin = driver.valueMin;
                    param.valueMax = driver.valueMax;
                    param.chance = driver.chance;
                }
                else if(driver.type == Action.ParameterDriver.Type.MenuToggle)
                {
                    //Search for menu action
                    var drivenAction = ActionsDescriptor.menuActions.FindMenuAction(driver.name);
                    if(drivenAction == null || drivenAction.menuType != MenuActions.MenuAction.MenuType.Toggle)
                    {
                        BuildFailed = true;
                        EditorUtility.DisplayDialog("Build Error", $"Action '{action.name}' unable to find menu toggle named '{driver.name}' for a parameter driver.  Build Failed.", "Okay");
                        return;
                    }
                    param.name = drivenAction.parameter;
                    param.value = driver.value == 0 ? 0 : drivenAction.controlValue;
                }
                else if(driver.type == Action.ParameterDriver.Type.MenuRandom)
                {
                    //Find max values    
                    List<MenuActions.MenuAction> list;
                    if(ParameterToMenuActions.TryGetValue(driver.name, out list))
                    {
                        param.name = driver.name;
                        param.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Random;
                        param.value = 0;
                        param.valueMin = driver.isZeroValid ? 1 : 0;
                        param.valueMax = list.Count;
                        param.chance = 0.5f;
                    }
                    else
                    {
                        BuildFailed = true;
                        EditorUtility.DisplayDialog("Build Error", $"Action '{action.name}' unable to find any menu actions driven by parameter '{driver.name} for a parameter driver'.  Build Failed.", "Okay");
                        return;
                    }
                }
                driverBehaviour.parameters.Add(param);
            }
        }

        //Helpers
        protected static void SetupTracking(Action action, AnimatorState state, TrackingType trackingType)
        {
            if (!action.bodyOverride.HasAny())
                return;

            //Add tracking behaviour
            var tracking = state.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
            tracking.trackingHead = action.bodyOverride.head ? trackingType : TrackingType.NoChange;
            tracking.trackingLeftHand = action.bodyOverride.leftHand ? trackingType : TrackingType.NoChange;
            tracking.trackingRightHand = action.bodyOverride.rightHand ? trackingType : TrackingType.NoChange;
            tracking.trackingHip = action.bodyOverride.hip ? trackingType : TrackingType.NoChange;
            tracking.trackingLeftFoot = action.bodyOverride.leftFoot ? trackingType : TrackingType.NoChange;
            tracking.trackingRightFoot = action.bodyOverride.rightFoot ? trackingType : TrackingType.NoChange;
            tracking.trackingLeftFingers = action.bodyOverride.leftFingers ? trackingType : TrackingType.NoChange;
            tracking.trackingRightFingers = action.bodyOverride.rightFingers ? trackingType : TrackingType.NoChange;
            tracking.trackingEyes = action.bodyOverride.eyes ? trackingType : TrackingType.NoChange;
            tracking.trackingMouth = action.bodyOverride.mouth ? trackingType : TrackingType.NoChange;
        }

        protected static Vector3 StatePosition(int x, int y) => new Vector3(x * 300, y * 100, 0);

        protected static int GetLayerIndex(AnimatorController controller, AnimatorControllerLayer layer)
        {
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == layer.name)
                {
                    return i;
                }
            }
            return -1;
        }

        protected static AnimatorControllerLayer GetControllerLayer(AnimatorController controller, string name)
        {
            //Check if exists
            foreach (var layer in controller.layers)
            {
                if (layer.name == name)
                    return layer;
            }

            //Create
            controller.AddLayer(name);
            return controller.layers[controller.layers.Length - 1];
        }

        protected static AnimatorControllerParameter AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type, float value)
        {
            //Clear
            for (int i = 0; i < controller.parameters.Length; i++)
            {
                if (controller.parameters[i].name == name)
                {
                    controller.RemoveParameter(i);
                    break;
                }
            }

            //Create
            var param = new AnimatorControllerParameter();
            param.name = name;
            param.type = type;
            param.defaultBool = value >= 1f;
            param.defaultInt = (int)value;
            param.defaultFloat = value;
            controller.AddParameter(param);

            return param;
        }


        public static bool SaveAsset(UnityEngine.Object asset, UnityEngine.Object rootAsset, string subDir = null, bool checkIfExists = false)
        {
            //Dir Path
            var dirPath = AssetDatabase.GetAssetPath(rootAsset);
            if(string.IsNullOrEmpty(dirPath))
            {
                BuildFailed = true;
                EditorUtility.DisplayDialog("Build Error", "Unable to save asset, unknown asset path.", "Okay");
                return false;
            }
            dirPath = dirPath.Replace(Path.GetFileName(dirPath), "");
            if (!string.IsNullOrEmpty(subDir))
                dirPath += $"{subDir}/";
            System.IO.Directory.CreateDirectory(dirPath);

            //Path
            var path = $"{dirPath}{asset.name}.asset";

            //Check if existing
            var existing = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            if (checkIfExists && existing != null && existing != asset)
            {
                if (!EditorUtility.DisplayDialog("Replace Asset?", $"Another asset already exists at '{path}'.\nAre you sure you want to replace it?", "Replace", "Cancel"))
                    return false;
            }

            AssetDatabase.CreateAsset(asset, path);
            return true;
        }
    }
}
#endif