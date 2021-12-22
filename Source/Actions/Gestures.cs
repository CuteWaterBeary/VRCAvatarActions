#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace VRCAvatarActions
{
    [CreateAssetMenu(fileName = "Gestures", menuName = "VRCAvatarActions/Other Actions/Gestures")]
	public class Gestures : NonMenuActions
	{
        public enum GestureSide
        {
            Left,
            Right,
            Both,
        }

        [System.Serializable]
        public class GestureAction : Action
        {
            //Gesture
            [System.Serializable]
            public struct GestureTable
            {
                public bool neutral;
                public bool fist;
                public bool openHand;
                public bool fingerPoint;
                public bool victory;
                public bool rockNRoll;
                public bool handGun;
                public bool thumbsUp;

                public bool GetValue(GestureEnum type)
                {
                    switch (type)
                    {
                        case GestureEnum.Neutral: return neutral;
                        case GestureEnum.Fist: return fist;
                        case GestureEnum.OpenHand: return openHand;
                        case GestureEnum.FingerPoint: return fingerPoint;
                        case GestureEnum.Victory: return victory;
                        case GestureEnum.RockNRoll: return rockNRoll;
                        case GestureEnum.HandGun: return handGun;
                        case GestureEnum.ThumbsUp: return thumbsUp;
                    }
                    return false;
                }

                public void SetValue(GestureEnum type, bool value)
                {
                    switch (type)
                    {
                        case GestureEnum.Neutral: neutral = value; break;
                        case GestureEnum.Fist: fist = value; break;
                        case GestureEnum.OpenHand: openHand = value; break;
                        case GestureEnum.FingerPoint: fingerPoint = value; break;
                        case GestureEnum.Victory: victory = value; break;
                        case GestureEnum.RockNRoll: rockNRoll = value; break;
                        case GestureEnum.HandGun: handGun = value; break;
                        case GestureEnum.ThumbsUp: thumbsUp = value; break;
                    }
                }

                public bool IsModified() => neutral || fist || openHand || fingerPoint || victory || rockNRoll || handGun || thumbsUp;
            }

            public GestureTable gestureTable = new GestureTable();
        }

        public List<GestureAction> actions = new List<GestureAction>();

        public override void GetActions(List<Action> output)
        {
            foreach (var action in actions)
                output.Add(action);
        }

        public override Action AddAction()
        {
            var result = new GestureAction();
            actions.Add(result);
            return result;
        }

        public override void RemoveAction(Action action)
        {
            actions.Remove(action as GestureAction);
        }

        public override void InsertAction(int index, Action action)
        {
            actions.Insert(index, action as GestureAction);
        }

        public override bool CanUseLayer(AnimationLayer layer) => layer == AnimationLayer.FX;

        public override bool ActionsHaveExit() => false;

        public GestureSide side;

        public override void Build(MenuActions.MenuAction parentAction)
        {
            //Layer name
            var layerName = name;
            if (parentAction != null)
                layerName = $"{parentAction.name}_{layerName}_SubActions";

            //Build
            BuildNormal(AnimationLayer.FX, layerName, actions, parentAction);
        }

        void BuildNormal(AnimationLayer layerType, string layerName, List<GestureAction> sourceActions, MenuActions.MenuAction parentAction)
        {
            //Find all that affect this layer
            var layerActions = new List<GestureAction>();
            foreach (var action in sourceActions)
            {
                if (!action.ShouldBuild())
                    continue;
                if (!action.AffectsLayer(layerType))
                    continue;
                layerActions.Add(action);
            }
            if (layerActions.Count == 0)
                return;

            //Build
            BuildLayer(layerType, layerName, layerActions, parentAction);
        }
        void BuildLayer(AnimationLayer layerType, string layerName, List<GestureAction> actions, MenuActions.MenuAction parentAction)
        {
            var controller = GetController(layerType);

            //Add parameter
            if(side == GestureSide.Left || side == GestureSide.Both)
                AddParameter(controller, "GestureLeft", AnimatorControllerParameterType.Int, 0);
            if(side == GestureSide.Right || side == GestureSide.Both)
                AddParameter(controller, "GestureRight", AnimatorControllerParameterType.Int, 0);

            //Prepare layer
            var layer = GetControllerLayer(controller, layerName);
            layer.stateMachine.entryTransitions = null;
            layer.stateMachine.anyStateTransitions = null;
            layer.stateMachine.states = null;
            layer.stateMachine.entryPosition = StatePosition(-1, 0);
            layer.stateMachine.anyStatePosition = StatePosition(-1, 1);
            layer.stateMachine.exitPosition = StatePosition(-1, 2);

            //Default state
            AnimatorState defaultState = null;
            GestureAction defaultAction = null;
            var unusedGestures = new List<GestureEnum>();
            unusedGestures.Add(GestureEnum.Neutral);
            unusedGestures.Add(GestureEnum.Fist);
            unusedGestures.Add(GestureEnum.OpenHand);
            unusedGestures.Add(GestureEnum.FingerPoint);
            unusedGestures.Add(GestureEnum.Victory);
            unusedGestures.Add(GestureEnum.RockNRoll);
            unusedGestures.Add(GestureEnum.HandGun);
            unusedGestures.Add(GestureEnum.ThumbsUp);

            //Build states
            int actionIter = 0;
            foreach (var action in this.actions)
            {
                //Check if valid
                if(!action.gestureTable.IsModified())
                {
                    EditorUtility.DisplayDialog("Build Warning", $"Simple Gesture {action.name} has no selected conditions.", "Okay");
                    continue;
                }

                //Build
                var state = layer.stateMachine.AddState(action.name, StatePosition(0, actionIter + 1));
                state.motion = action.GetAnimation(layerType, true);
                actionIter += 1;

                //Conditions
                AddGestureCondition(GestureEnum.Neutral);
                AddGestureCondition(GestureEnum.Fist);
                AddGestureCondition(GestureEnum.OpenHand);
                AddGestureCondition(GestureEnum.FingerPoint);
                AddGestureCondition(GestureEnum.Victory);
                AddGestureCondition(GestureEnum.RockNRoll);
                AddGestureCondition(GestureEnum.HandGun);
                AddGestureCondition(GestureEnum.ThumbsUp);
                void AddGestureCondition(GestureEnum gesture)
                {
                    if (!action.gestureTable.GetValue(gesture))
                        return;

                    //Transition
                    var transition = layer.stateMachine.AddAnyStateTransition(state);
                    transition.hasExitTime = false;
                    transition.exitTime = 0;
                    transition.duration = action.fadeIn;

                    if (side == GestureSide.Left || side == GestureSide.Both)
                        transition.AddCondition(AnimatorConditionMode.Equals, (int)gesture, "GestureLeft");
                    if (side == GestureSide.Right || side == GestureSide.Both)
                        transition.AddCondition(AnimatorConditionMode.Equals, (int)gesture, "GestureRight");

                    //Parent
                    if (parentAction != null && gesture != GestureEnum.Neutral)
                        parentAction.AddCondition(transition, true);

                    //Cleanup
                    unusedGestures.Remove(gesture);
                }

                //Default
                if(action.gestureTable.neutral)
                {
                    defaultState = state;
                    defaultAction = action;
                }
            }

            //Default state
            if(defaultState == null)
                defaultState = layer.stateMachine.AddState("Neutral", StatePosition(0, 0));
            layer.stateMachine.defaultState = defaultState;

            //Animation Layer Weight
            var layerWeight = defaultState.AddStateMachineBehaviour<VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl>();
            layerWeight.goalWeight = 1;
            layerWeight.layer = GetLayerIndex(controller, layer);
            layerWeight.blendDuration = 0;
            layerWeight.playable = VRC.SDKBase.VRC_AnimatorLayerControl.BlendableLayer.FX;

            //Default transitions
            foreach (var gesture in unusedGestures)
            {
                //Transition
                var transition = layer.stateMachine.AddAnyStateTransition(defaultState);
                transition.hasExitTime = false;
                transition.exitTime = 0;
                transition.duration = defaultAction != null ? defaultAction.fadeIn : 0f;

                if (side == GestureSide.Left || side == GestureSide.Both)
                    transition.AddCondition(AnimatorConditionMode.Equals, (int)gesture, "GestureLeft");
                if (side == GestureSide.Right || side == GestureSide.Both)
                    transition.AddCondition(AnimatorConditionMode.Equals, (int)gesture, "GestureRight");
            }

            //Parent
            if (parentAction != null)
            {
                var transition = layer.stateMachine.AddAnyStateTransition(defaultState);
                transition.hasExitTime = false;
                transition.exitTime = 0;
                transition.duration = defaultAction != null ? defaultAction.fadeIn : 0f;

                parentAction.AddCondition(transition, false);
            }
        }
    }
}
#endif