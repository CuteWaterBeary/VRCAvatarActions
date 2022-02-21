#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using static VRCAvatarActions.BaseActions;
using static VRCAvatarActions.MenuActions;
using Action = VRCAvatarActions.BaseActions.Action;
using AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using TrackingType = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType;

namespace VRCAvatarActions
{
    public class ActionsBuilder
    {
        public AvatarDescriptor AvatarDescriptor = null;
        public AvatarActions ActionsDescriptor = null;
        public List<ExpressionParameters.Parameter> AllParameters = new List<ExpressionParameters.Parameter>();
        public List<ExpressionParameters.Parameter> BuildParameters = new List<ExpressionParameters.Parameter>();
        public AnimatorController ActionController;
        public AnimatorController FxController;
        public AnimatorController GetController(AnimationLayer layer)
        {
            switch (layer)
            {
                case AnimationLayer.Action:
                    return ActionController;
                case AnimationLayer.FX:
                    return FxController;
            }
            return null;
        }

        public bool BuildFailed = false;
        public Dictionary<string, AnimationClip> GeneratedClips = new Dictionary<string, AnimationClip>();
        public Dictionary<string, List<MenuAction>> ParameterToMenuActions = new Dictionary<string, List<MenuAction>>();

        public void BuildAvatarData(AvatarDescriptor desc, AvatarActions actionsDesc)
        {
            //Store
            AvatarDescriptor = desc;
            ActionsDescriptor = actionsDesc;
            BuildFailed = false;

            //Build
            BuildSetup();
            BuildMain(this);
            BuildCleanup();

            //Error
            if (BuildFailed)
            {
                EditorUtility.DisplayDialog("Build Failed", "Build has failed.", "Okay");
            }
        }

        public void UpdateParameter(AvatarDescriptor desc, AvatarActions actionsDesc, string parameterName)
        {
            //Store
            AvatarDescriptor = desc;
            ActionsDescriptor = actionsDesc;
            BuildFailed = false;

            AvatarActions.ParameterDefault settingParameter = ActionsDescriptor.parameterDefaults.Find(p => p.name == parameterName);
            ExpressionParameters.Parameter parameter = FindExpressionParameter(parameterName);
            if (settingParameter != null && parameter != null)
            {
                parameter.defaultValue = settingParameter.defaultValue;
                parameter.saved = settingParameter.saved;
            }

            if (ActionsDescriptor.menuActions != null)
            {
                ActionsDescriptor.menuActions.SetState(this, parameterName);
            }
        }

        public void BuildSetup()
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
                foreach (var layer in AvatarDescriptor.baseAnimationLayers)
                {
                    if (layer.type == animLayerType)
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
                    for (int i = 0; i < controller.layers.Length; i++)
                    {
                        if (controller.layers[i].name == "Base Layer")
                            continue;
                        if (ActionsDescriptor.ignoreLayers.Contains(controller.layers[i].name))
                            continue;

                        //Remove
                        controller.RemoveLayer(i);
                        i--;
                    }

                    //Clean parameters
                    for (int i = 0; i < controller.parameters.Length; i++)
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

        public void BuildMain(ActionsBuilder builder)
        {
            //Build menu
            if (ActionsDescriptor.menuActions != null)
            {
                ActionsDescriptor.menuActions.Build(builder);
                if (BuildFailed)
                    return;
            }

            //Build others
            foreach (var actionSet in ActionsDescriptor.otherActions)
            {
                if (actionSet != null)
                {
                    actionSet.Build(builder, null);
                    if (BuildFailed)
                        return;
                }
            }
        }

        public void BuildCleanup()
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
                    AvatarActions.ParameterDefault parameterDefault = ActionsDescriptor.parameterDefaults.Find(p => p.name == parameter.name);
                    if (parameterDefault != null)
                    {
                        parameter.defaultValue = parameterDefault.defaultValue;
                        parameter.saved = parameterDefault.saved;
                    }
                    else
                    {
                        ActionsDescriptor.parameterDefaults.Add(new AvatarActions.ParameterDefault(parameter));
                    }
                }

                //Parameter defaults
                foreach (var paramDefault in ActionsDescriptor.parameterDefaults)
                {
                    var param = AvatarDescriptor.expressionParameters.FindParameter(paramDefault.name);
                    if (param != null)
                        param.defaultValue = paramDefault.defaultValue;
                }

                //Check parameter count
                var parametersObject = AvatarDescriptor.expressionParameters;
                if (parametersObject.CalcTotalCost() > ExpressionParameters.MAX_PARAMETER_COST)
                {
                    BuildFailed = true;
                    EditorUtility.DisplayDialog("Build Error", $"Unable to build VRCExpressionParameters. Too many parameters defined.", "Okay");
                    return;
                }

                EditorUtility.SetDirty(ActionsDescriptor);
                EditorUtility.SetDirty(AvatarDescriptor.expressionParameters);
            }

            //Save prefab
            AssetDatabase.SaveAssets();
        }

        //Parameters
        void InitExpressionParameters()
        {
            //Check if parameter object exists
            var parametersObject = AvatarDescriptor.expressionParameters;
            if (AvatarDescriptor.expressionParameters == null || !AvatarDescriptor.customExpressions)
            {
                parametersObject = ScriptableObject.CreateInstance<ExpressionParameters>();
                parametersObject.name = "ExpressionParameters";
                BuildFailed = !SaveAsset(parametersObject, ActionsDescriptor.ReturnAnyScriptableObject(), "Generated");

                AvatarDescriptor.customExpressions = true;
                AvatarDescriptor.expressionParameters = parametersObject;
            }

            //Clear parameters
            BuildParameters.Clear();
            if (parametersObject.parameters != null)
            {
                foreach (var param in parametersObject.parameters)
                {
                    if (param != null && ActionsDescriptor.ignoreParameters.Contains(param.name))
                        BuildParameters.Add(param);
                }
            }
        }

        public void DefineExpressionParameter(ExpressionParameters.Parameter parameter)
        {
            //Check if already exists
            if (FindExpressionParameter(parameter.name) != null)
                return;

            //Add
            BuildParameters.Add(parameter);
        }

        public ExpressionParameters.Parameter FindExpressionParameter(string name)
        {
            foreach (var param in BuildParameters)
            {
                if (param.name == name)
                    return param;
            }
            return null;
        }

        public float GetExpressionParameterDefaultState(Action action)
        {
            float value = 0f;
            if (action is MenuAction menuAction)
            {
                AvatarActions.ParameterDefault param = ActionsDescriptor.parameterDefaults.Find(p => p.name == menuAction.parameter);
                if (param != null)
                    value = param.defaultValue;
            }
            return value;
        }

        //Normal
        public void BuildActionLayer(AnimatorController controller, IEnumerable<Action> actions, string layerName, MenuAction parentAction, bool turnOffState = true)
        {
            AnimatorControllerLayer layer = PrepareLayer(controller, layerName);

            //Animation Layer Weight
            int layerIndex = GetLayerIndex(controller, layer);

            AnimatorState waitingState = BuildWaitingState(turnOffState, layer, layerIndex, VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.Action);

            //Actions
            int actionIter = 0;
            foreach (var action in actions)
            {
                AnimatorState lastState;

                //Enter state
                {
                    var state = layer.stateMachine.AddState(action.name + "_Setup", StatePosition(1, actionIter));
                    state.motion = action.actionLayerAnimations.enter;

                    //Transition
                    action.AddTransitions(this, controller, waitingState, state, 0, Action.Trigger.Type.Enter, parentAction);

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
                    state.motion = action.GetAnimation(this, AnimationLayer.Action, true);

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
                    var holdMotion = action.GetAnimation(this, AnimationLayer.Action, true);
                    BuildHoldState(action, layer, StatePosition(3, actionIter), holdMotion, ref lastState);
                }

                //Disable state
                {
                    var state = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(4, actionIter));
                    state.motion = action.GetAnimation(this, AnimationLayer.Action, false);

                    //Transition
                    action.AddTransitions(this, controller, lastState, state, 0, Action.Trigger.Type.Exit, parentAction);

                    //Playable Layer
                    var playable = state.AddStateMachineBehaviour<VRCPlayableLayerControl>();
                    playable.goalWeight = 0.0f;
                    playable.blendDuration = action.fadeOut;

                    //Store
                    lastState = state;
                }

                var motion = action.GetAnimation(this, AnimationLayer.Action, false);
                BuildFadeoutState(action, layer, StatePosition(5, actionIter), motion, ref lastState);

                BuildCleanupState(action, layer, StatePosition(6, actionIter), ref lastState);

                BuildExitTransition(lastState);

                //Iterate
                actionIter += 1;
            }
        }

        public void BuildNormalLayer(AnimatorController controller, IEnumerable<Action> actions, string layerName, AnimationLayer layerType, MenuAction parentAction, bool turnOffState = true)
        {
            AnimatorControllerLayer layer = PrepareLayer(controller, layerName);

            //Animation Layer Weight
            int layerIndex = GetLayerIndex(controller, layer);

            AnimatorState waitingState = BuildWaitingState(turnOffState, layer, layerIndex, VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX);

            //Each action
            int actionIter = 0;
            foreach (var action in actions)
            {
                AnimatorState lastState = waitingState;
                bool enabledByDefault = GetExpressionParameterDefaultState(action) == 1f;

                //Enable
                var enableState = layer.stateMachine.AddState(action.name + "_Enable", StatePosition(1, actionIter));
                enableState.motion = action.GetAnimation(this, layerType, !enabledByDefault);

                // //Transition
                action.AddTransitions(this, controller, lastState, enableState, action.fadeIn, enabledByDefault ? Action.Trigger.Type.Exit : Action.Trigger.Type.Enter, parentAction);

                //Animation Layer Weight
                var layerWeight = enableState.AddStateMachineBehaviour<VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 1;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;

                //Tracking
                SetupTracking(action, enableState, TrackingType.Animation);

                //Parameter Drivers
                BuildParameterDrivers(action, enableState);

                //Store
                lastState = enableState;

                //Hold
                if (action.hold > 0)
                {
                    var holdMotion = action.GetAnimation(this, layerType, true);
                    BuildHoldState(action, layer, StatePosition(2, actionIter), holdMotion, ref lastState);
                }

                //Exit
                if (action.HasExit() || parentAction != null)
                {
                    //Disable
                    var disableState = layer.stateMachine.AddState(action.name + "_Disable", StatePosition(3, actionIter));
                    disableState.motion = action.GetAnimation(this, layerType, enabledByDefault);

                    //Transition
                    action.AddTransitions(this, controller, lastState, disableState, 0, enabledByDefault ? Action.Trigger.Type.Enter : Action.Trigger.Type.Exit, parentAction);

                    //Store
                    lastState = disableState;

                    // Euan: I don't think this is needed?
                    // BuildFadeoutState(action, layer, StatePosition(4, actionIter), null, ref lastState);

                    BuildCleanupState(action, layer, StatePosition(5, actionIter), ref lastState);

                    BuildExitTransition(lastState);
                }

                //Iterate
                actionIter += 1;
            }
        }

        private AnimatorControllerLayer PrepareLayer(AnimatorController controller, string layerName)
        {
            var layer = GetControllerLayer(controller, layerName);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(7, 0);
            return layer;
        }

        private AnimatorState BuildWaitingState(bool turnOffState, AnimatorControllerLayer layer, int layerIndex, VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer blendableLayer)
        {
            AnimatorState waitingState = layer.stateMachine.AddState("Waiting", Vector3.zero);
            if (turnOffState)
            {
                //Animation Layer Weight
                var layerWeight = waitingState.AddStateMachineBehaviour<VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 0;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = blendableLayer;
            }
            else
                waitingState.writeDefaultValues = false;
            return waitingState;
        }

        private void BuildHoldState(Action action, AnimatorControllerLayer layer, Vector3 statePos, Motion motion, ref AnimatorState lastState)
        {
            var state = layer.stateMachine.AddState(action.name + "_Hold", statePos);
            state.motion = motion;

            //Transition
            var transition = lastState.AddTransition(state);
            transition.hasExitTime = true;
            transition.exitTime = action.hold;
            transition.duration = 0;

            //Store
            lastState = state;
        }

        private void BuildFadeoutState(Action action, AnimatorControllerLayer layer, Vector3 statePos, Motion motion, ref AnimatorState lastState)
        {
            var state = layer.stateMachine.AddState(action.name + "_Fadeout", statePos);
            
            if (motion != null)
                state.motion = motion;

            //Transition
            var transition = lastState.AddTransition(state);
            transition.hasExitTime = false;
            transition.exitTime = 0;
            transition.duration = action.fadeOut;
            transition.AddCondition(AnimatorConditionMode.If, 1, "True");

            //Store
            lastState = state;
        }

        private void BuildCleanupState(Action action, AnimatorControllerLayer layer, Vector3 statePos, ref AnimatorState lastState)
        {
            var state = layer.stateMachine.AddState(action.name + "_Cleanup", statePos);

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

        private void BuildExitTransition(AnimatorState lastState)
        {
            var transition = lastState.AddExitTransition();
            transition.hasExitTime = false;
            transition.exitTime = 0f;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.If, 1, "True");
        }

        //Generated
        public void BuildGroupedLayers(IEnumerable<Action> sourceActions, AnimationLayer layerType, MenuAction parentAction, System.Func<Action, bool> onCheck, System.Action<AnimatorController, string, List<Action>> onBuild)
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
        public void AddTriggerConditions(AnimatorController controller, AnimatorStateTransition transition, IEnumerable<Action.Condition> conditions)
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
                                if (param != null)
                                {
                                    switch (param.valueType)
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
                            if (!found)
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

                            if (!found)
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

        protected void BuildParameterDrivers(Action action, AnimatorState state)
        {
            if (action.parameterDrivers.Count == 0)
                return;

            var driverBehaviour = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driverBehaviour.localOnly = true;
            foreach (var driver in action.parameterDrivers)
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
                else if (driver.type == Action.ParameterDriver.Type.MenuToggle)
                {
                    //Search for menu action
                    var drivenAction = ActionsDescriptor.menuActions.FindMenuAction(driver.name);
                    if (drivenAction == null || drivenAction.menuType != MenuAction.MenuType.Toggle)
                    {
                        BuildFailed = true;
                        EditorUtility.DisplayDialog("Build Error", $"Action '{action.name}' unable to find menu toggle named '{driver.name}' for a parameter driver.  Build Failed.", "Okay");
                        return;
                    }
                    param.name = drivenAction.parameter;
                    param.value = driver.value == 0 ? 0 : drivenAction.controlValue;
                }
                else if (driver.type == Action.ParameterDriver.Type.MenuRandom)
                {
                    //Find max values    
                    if (ParameterToMenuActions.TryGetValue(driver.name, out List<MenuAction> list))
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

        //Menu
        public void BuildExpressionParameters(List<MenuAction> sourceActions)
        {
            //Find all unique menu parameters
            AllParameters.Clear();
            foreach (var action in sourceActions)
            {
                var param = GenerateParameter(action);
                if (param != null && IsNewParameter(param))
                    AllParameters.Add(param);

                param = GenerateParameterIsOpen(action);
                if (param != null && IsNewParameter(param))
                    AllParameters.Add(param);
            }

            bool IsNewParameter(ExpressionParameters.Parameter param)
            {
                foreach (var item in AllParameters)
                {
                    if (string.IsNullOrEmpty(item.name))
                        continue;
                    if (item.name == param.name)
                    {
                        if (item.valueType == param.valueType)
                            return false;
                        else
                        {
                            BuildFailed = true;
                            EditorUtility.DisplayDialog("Build Error", $"Unable to build VRCExpressionParameters. Parameter named '{item.name}' is used twice but with different types.", "Okay");
                            return false;
                        }
                    }
                }
                return true;
            }

            //Add
            foreach (var param in AllParameters)
                DefineExpressionParameter(param);
        }

        public void BuildActionValues(List<MenuAction> sourceActions)
        {
            var parametersObject = AvatarDescriptor.expressionParameters;
            ParameterToMenuActions.Clear();
            foreach (var parameter in BuildParameters)
            {
                if (parameter == null || string.IsNullOrEmpty(parameter.name))
                    continue;

                //Find all actions
                List<MenuAction> actions = new List<MenuAction>();
                int actionCount = 1;
                bool defaultUsed = false;
                foreach (var action in sourceActions)
                {
                    if (action.parameter == parameter.name)
                    {
                        actions.Add(action);
                        action.controlValue = actionCount;
                        actionCount += 1;

                        //Default value
                        if (action.isOffState)
                        {
                            if (!defaultUsed)
                            {
                                defaultUsed = true;
                                action.controlValue = 0;
                            }
                            else
                            {
                                BuildFailed = true;
                                EditorUtility.DisplayDialog("Build Failed", $"Two menu actions are marked as 'Is Default State' for parameter '{parameter.name}'.  Only one can be marked as default at a time.", "Okay");
                                return;
                            }
                        }
                    }
                }
                ParameterToMenuActions.Add(parameter.name, actions);

                //Modify to bool
                if (actions.Count == 1 && (actions[0].menuType == MenuAction.MenuType.Toggle || actions[0].menuType == MenuAction.MenuType.Button))
                {
                    parameter.valueType = ExpressionParameters.ValueType.Bool;

                    //Save
                    EditorUtility.SetDirty(parametersObject);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        public void BuildExpressionsMenu(MenuActions rootMenu)
        {
            List<MenuActions> menuList = new List<MenuActions>();

            //Create root menu if needed
            if (AvatarDescriptor.expressionsMenu == null)
            {
                AvatarDescriptor.expressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                AvatarDescriptor.expressionsMenu.name = "ExpressionsMenu_Root";
                BuildFailed = !SaveAsset(AvatarDescriptor.expressionsMenu, rootMenu, "Generated");
            }

            //Expressions
            CreateMenu(rootMenu, AvatarDescriptor.expressionsMenu);

            void CreateMenu(MenuActions ourMenu, VRCExpressionsMenu expressionsMenu)
            {
                //Clear old controls
                List<VRCExpressionsMenu.Control> oldControls = new List<VRCExpressionsMenu.Control>();
                oldControls.AddRange(expressionsMenu.controls);
                expressionsMenu.controls.Clear();

                //Create controls from actions
                foreach (var action in ourMenu.actions)
                {
                    if (!action.ShouldBuild())
                        continue;

                    if (action.menuType == MenuAction.MenuType.Button || action.menuType == MenuAction.MenuType.Toggle || action.menuType == MenuAction.MenuType.Slider)
                    {
                        //Create control
                        var control = new VRCExpressionsMenu.Control();
                        control.name = action.name;
                        control.icon = action.icon;
                        control.value = action.controlValue;
                        expressionsMenu.controls.Add(control);

                        if (action.menuType == MenuAction.MenuType.Button)
                        {
                            control.type = VRCExpressionsMenu.Control.ControlType.Button;
                            control.parameter = new VRCExpressionsMenu.Control.Parameter();
                            control.parameter.name = action.parameter;
                        }
                        else if (action.menuType == MenuAction.MenuType.Toggle)
                        {
                            control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                            control.parameter = new VRCExpressionsMenu.Control.Parameter();
                            control.parameter.name = action.parameter;
                        }
                        else if (action.menuType == MenuAction.MenuType.Slider)
                        {
                            control.type = VRCExpressionsMenu.Control.ControlType.RadialPuppet;
                            control.subParameters = new VRCExpressionsMenu.Control.Parameter[1];
                            control.subParameters[0] = new VRCExpressionsMenu.Control.Parameter();
                            control.subParameters[0].name = action.parameter;
                        }
                    }
                    else if (action.menuType == MenuAction.MenuType.SubMenu)
                    {
                        //Recover old sub-menu
                        VRCExpressionsMenu expressionsSubMenu = null;
                        foreach (var controlIter in oldControls)
                        {
                            if (controlIter.name == action.name)
                            {
                                expressionsSubMenu = controlIter.subMenu;
                                break;
                            }
                        }

                        //Create if needed
                        if (expressionsSubMenu == null)
                        {
                            expressionsSubMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                            expressionsSubMenu.name = "ExpressionsMenu_" + action.name;
                            BuildFailed = !SaveAsset(expressionsSubMenu, rootMenu, "Generated");
                        }

                        //Create control
                        var control = new VRCExpressionsMenu.Control();
                        control.name = action.name;
                        control.icon = action.icon;
                        control.type = VRCExpressionsMenu.Control.ControlType.SubMenu;
                        control.subMenu = expressionsSubMenu;
                        expressionsMenu.controls.Add(control);

                        //Populate sub-menu
                        CreateMenu(action.subMenu, expressionsSubMenu);
                    }
                    else if (action.menuType == MenuAction.MenuType.PreExisting)
                    {
                        //Recover old control
                        foreach (var controlIter in oldControls)
                        {
                            if (controlIter.name == action.name)
                            {
                                oldControls.Remove(controlIter);
                                expressionsMenu.controls.Add(controlIter);
                                break;
                            }
                        }
                    }
                }

                //Save prefab
                EditorUtility.SetDirty(expressionsMenu);
            }

            //Save all assets
            AssetDatabase.SaveAssets();
        }

        //Normal
        public void BuildNormalLayers(List<MenuAction> sourceActions, AnimationLayer layerType)
        {
            var controller = GetController(layerType);

            //Find matching actions
            var layerActions = new List<MenuAction>();
            foreach (var parameter in AllParameters)
            {
                layerActions.Clear();
                foreach (var action in sourceActions)
                {
                    if (action.parameter != parameter.name)
                        continue;
                    if (!action.NeedsControlLayer())
                        continue;
                    if (action.menuType == MenuAction.MenuType.Slider)
                        continue;
                    if (!action.GetAnimation(this, layerType, true))
                        continue;
                    layerActions.Add(action);
                }
                if (layerActions.Count == 0)
                    continue;

                //Check of off state
                MenuAction offAction = null;
                foreach (var action in layerActions)
                {
                    if (action.controlValue == 0)
                    {
                        offAction = action;
                        break;
                    }
                }

                //Parameter
                AddParameter(controller, parameter.name, parameter.valueType == ExpressionParameters.ValueType.Bool ? AnimatorControllerParameterType.Bool : AnimatorControllerParameterType.Int, 0);

                //Build
                bool turnOffState = offAction == null;
                if (layerType == AnimationLayer.Action)
                    BuildActionLayer(controller, layerActions, parameter.name, null, turnOffState);
                else
                    BuildNormalLayer(controller, layerActions, parameter.name, layerType, null, turnOffState);
            }
        }

        public void BuildSliderLayers(List<MenuAction> sourceActions, AnimationLayer layerType)
        {
            //For each parameter create a new layer
            foreach (var parameter in AllParameters)
            {
                BuildSliderLayer(sourceActions, layerType, parameter.name);
            }
        }

        public void BuildSliderLayer(List<MenuAction> sourceActions, AnimationLayer layerType, string parameter)
        {
            var controller = GetController(layerType);

            //Find all slider actions
            var layerActions = new List<MenuAction>();
            foreach (var actionIter in sourceActions)
            {
                if (actionIter.menuType == MenuAction.MenuType.Slider && actionIter.parameter == parameter && actionIter.GetAnimation(this, layerType) != null)
                    layerActions.Add(actionIter);
            }
            if (layerActions.Count == 0)
                return;
            var action = layerActions[0];

            //Add parameter
            AddParameter(controller, parameter, AnimatorControllerParameterType.Float, 0);

            //Prepare layer
            var layer = GetControllerLayer(controller, parameter);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(-1, 2);

            int layerIndex = GetLayerIndex(controller, layer);

            //Blend state
            {
                var state = layer.stateMachine.AddState(action.name + "_Blend", StatePosition(0, 0));
                state.motion = action.GetAnimation(this, layerType);
                state.timeParameter = action.parameter;
                state.timeParameterActive = true;

                //Animation Layer Weight
                var layerWeight = state.AddStateMachineBehaviour<VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 1;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;
            }
        }

        public void BuildSubActionLayers(List<MenuAction> sourceActions, AnimationLayer layerType)
        {
            var controller = GetController(layerType);

            //Find matching actions
            var layerActions = new List<MenuAction>();
            foreach (var parameter in AllParameters)
            {
                layerActions.Clear();
                foreach (var action in sourceActions)
                {
                    if (action.parameter != parameter.name)
                        continue;
                    if (action.subActions.Count == 0)
                        continue;
                    layerActions.Add(action);
                }
                if (layerActions.Count == 0)
                    continue;

                //Parameter
                AddParameter(controller, parameter.name, parameter.valueType == ExpressionParameters.ValueType.Bool ? AnimatorControllerParameterType.Bool : AnimatorControllerParameterType.Int, 0);

                //Sub-Actions
                foreach (var parentAction in layerActions)
                {
                    foreach (var subActions in parentAction.subActions)
                    {
                        subActions.Build(this, parentAction);
                    }
                }
            }
        }

        public void SetActionStates(List<MenuAction> sourceActions)
        {
            foreach (var action in sourceActions)
            {
                action.SetState(this);
            }
        }

        //Other
        ExpressionParameters.Parameter GenerateParameter(MenuAction action)
        {
            if (string.IsNullOrEmpty(action.parameter))
                return null;
            var parameter = new ExpressionParameters.Parameter();
            parameter.name = action.parameter;
            switch (action.menuType)
            {
                case MenuAction.MenuType.Button:
                case MenuAction.MenuType.Toggle:
                    parameter.valueType = ExpressionParameters.ValueType.Int;
                    break;
                case MenuAction.MenuType.Slider:
                    parameter.valueType = ExpressionParameters.ValueType.Float;
                    break;
            }
            return parameter;
        }

        ExpressionParameters.Parameter GenerateParameterIsOpen(MenuAction action)
        {
            return null;
            /*if (string.IsNullOrEmpty(action.parameterIsOpen))
                return null;
            if(!(action.menuType == MenuAction.MenuType.Slider || action.menuType == MenuAction.MenuType.SubMenu))
                return null;

            var parameter = new ExpressionParameters.Parameter();
            parameter.name = action.parameterIsOpen;
            parameter.valueType = ExpressionParameters.ValueType.Bool;
            return parameter;*/
        }

        //Helpers
        protected void SetupTracking(Action action, AnimatorState state, TrackingType trackingType)
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

        public Vector3 StatePosition(int x, int y) => new Vector3(x * 300, y * 100, 0);

        public int GetLayerIndex(AnimatorController controller, AnimatorControllerLayer layer)
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

        public AnimatorControllerLayer GetControllerLayer(AnimatorController controller, string name)
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

        public AnimatorControllerParameter AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type, float value)
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
            if (string.IsNullOrEmpty(dirPath))
            {
                EditorUtility.DisplayDialog("Build Error", "Unable to save asset, unknown asset path.", "Okay");
                return false;
            }
            dirPath = dirPath.Replace(Path.GetFileName(dirPath), "");
            if (!string.IsNullOrEmpty(subDir))
                dirPath += $"{subDir}/";
            Directory.CreateDirectory(dirPath);

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