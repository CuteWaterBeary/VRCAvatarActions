#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRCAvatarActions;

[CreateAssetMenu(fileName = "VisemeToVolume", menuName = "VRCAvatarActions/Special Actions/VisemeToVolume")]
public class VisemeToVolume : NonMenuActions
{
    public string parameter;
    public AnimationClip animationFx;

    public override void GetActions(List<Action> output) { }
    public override Action AddAction() { return null; }
    public override void RemoveAction(Action action) { }
    public override void InsertAction(int index, Action action) { }

    public override void Build(ActionsBuilder builder, MenuActions.MenuAction parent)
    {
        var controller = builder.GetController(AnimationLayer.FX);

        //Define volume param
        {
            var param = new VRCExpressionParameters.Parameter();
            param.name = parameter;
            param.valueType = VRCExpressionParameters.ValueType.Float;
            param.defaultValue = 0;
            param.saved = false;
            builder.DefineExpressionParameter(param);
        }

        //Define parameters on controller
        builder.AddParameter(controller, "Viseme", AnimatorControllerParameterType.Int, 0);
        builder.AddParameter(controller, parameter, AnimatorControllerParameterType.Float, 0);

        BuildDriverLayer(builder);
        BuildAnimationLayer(builder);
    }

    void BuildDriverLayer(ActionsBuilder builder)
    {
        var controller = builder.GetController(AnimationLayer.FX);

        var layer = builder.GetControllerLayer(controller, "VisimeVolumeDriver");
        layer.stateMachine.entryTransitions = null;
        layer.stateMachine.anyStateTransitions = null;
        layer.stateMachine.states = null;
        layer.stateMachine.entryPosition = builder.StatePosition(-1, 0);
        layer.stateMachine.anyStatePosition = builder.StatePosition(-1, 1);
        layer.stateMachine.exitPosition = builder.StatePosition(7, 0);

        int layerIndex = builder.GetLayerIndex(controller, layer);

        for (int i = 0; i <= 100; i++)
        {
            //State
            var state = layer.stateMachine.AddState($"Volume_{i}", builder.StatePosition(1, i));

            //Layer weight
            if (i == 0)
            {
                //Animation Layer Weight
                var layerWeight = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
                layerWeight.goalWeight = 1;
                layerWeight.layer = layerIndex;
                layerWeight.blendDuration = 0;
                layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;
            }

            //Transition
            var transition = layer.stateMachine.AddAnyStateTransition(state);
            transition.canTransitionToSelf = false;
            transition.hasExitTime = false;
            transition.duration = 0;
            transition.hasFixedDuration = true;
            transition.AddCondition(AnimatorConditionMode.Equals, i, "Viseme");

            //Playable
            var driver = state.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAvatarParameterDriver>();
            var param = new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter();
            param.name = parameter;
            param.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
            param.value = i * 0.01f;
            driver.parameters.Add(param);
        }
    }

    void BuildAnimationLayer(ActionsBuilder builder)
    {
        var action = new MenuActions.MenuAction();
        action.menuType = MenuActions.MenuAction.MenuType.Slider;
        action.parameter = parameter;
        action.name = "VisimeAnimation";
        action.fxLayerAnimations.enter = animationFx;

        List<MenuActions.MenuAction> list = new List<MenuActions.MenuAction>();
        list.Add(action);
        builder.BuildSliderLayer(list, AnimationLayer.FX, action.parameter);
    }
}

#endif