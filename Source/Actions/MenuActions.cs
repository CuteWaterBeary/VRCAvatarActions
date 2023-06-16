#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;

namespace VRCAvatarActions
{
    [CreateAssetMenu(fileName = "Menu", menuName = "VRCAvatarActions/Menu Actions/Menu")]
    public class MenuActions : BaseActions
    {
        [System.Serializable]
        public class MenuAction : Action
        {
            public enum MenuType
            {
                //Action
                Toggle = 4453,
                Button = 3754,
                Slider = 8579,
                SubMenu = 3641,
                PreExisting = 5697,
            }

            public MenuType menuType = MenuType.Toggle;

            public Texture2D icon;
            public string parameter;
            public MenuActions subMenu;
            public List<NonMenuActions> subActions = new List<NonMenuActions>();
            public bool isOffState = false;

            //Meta
            public int controlValue = 0;
            public bool foldoutSubActions;

            public bool IsNormalAction() => menuType == MenuType.Button || menuType == MenuType.Toggle;
            public bool NeedsControlLayer() => menuType == MenuType.Button || menuType == MenuType.Toggle || menuType == MenuType.Slider;

            public override bool ShouldBuild()
            {
                switch (menuType)
                {
                    case MenuType.Button:
                    case MenuType.Toggle:
                    case MenuType.Slider:
                        if (string.IsNullOrEmpty(parameter))
                            return false;
                        break;
                    case MenuType.SubMenu:
                        if (subMenu == null)
                            return false;
                        break;
                }
                return base.ShouldBuild();
            }

            public override void CopyTo(Action clone)
            {
                base.CopyTo(clone);

                if (clone is MenuAction menuClone)
                {
                    menuClone.icon = icon;
                    menuClone.parameter = parameter;
                    menuClone.menuType = menuType;
                    menuClone.subMenu = subMenu;
                    menuClone.foldoutSubActions = foldoutSubActions;

                    menuClone.subActions.Clear();
                    foreach (var item in subActions)
                        menuClone.subActions.Add(item);
                }
            }

            //Build
            public override string GetLayerGroup() => parameter;

            public override void AddCondition(ActionsBuilder builder, AnimatorStateTransition transition, bool equals)
            {
                if (string.IsNullOrEmpty(parameter))
                    return;

                //Is parameter bool?
                AnimatorConditionMode mode;
                var param = builder.FindExpressionParameter(parameter);
                if (param.valueType == ExpressionParameters.ValueType.Bool)
                    mode = equals ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot;
                else if (param.valueType == ExpressionParameters.ValueType.Int)
                    mode = equals ? AnimatorConditionMode.Equals : AnimatorConditionMode.NotEqual;
                else
                {
                    builder.BuildFailed = true;
                    EditorUtility.DisplayDialog("Build Error", "Parameter value type is not as expected.", "Okay");
                    return;
                }

                //Set
                transition.AddCondition(mode, controlValue, parameter);
            }
        }

        public List<MenuAction> actions = new List<MenuAction>();

        public MenuAction FindMenuAction(string name)
        {
            foreach (var action in actions)
            {
                if (action.name == name)
                    return action;
            }
            foreach (var action in actions)
            {
                if (action.menuType == MenuAction.MenuType.SubMenu && action.subMenu != null)
                {
                    var result = action.subMenu.FindMenuAction(name);
                    if (result != null)
                        return result;
                }
            }
            return null;
        }

        public override void GetActions(List<Action> output)
        {
            foreach (var action in actions)
                output.Add(action);
        }

        public override Action AddAction()
        {
            var result = new MenuAction();
            actions.Add(result);
            return result;
        }

        public override void InsertAction(int index, Action action) => actions.Insert(index, action as MenuAction);
        public override void RemoveAction(Action action) => actions.Remove(action as MenuAction);

        public virtual void Build(ActionsBuilder builder)
        {
            //Collect all menu actions
            var validActions = new List<MenuAction>();
            CollectValidMenuActions(builder, validActions);

            //Expression Parameters
            builder.BuildExpressionParameters(validActions);
            if (builder.BuildFailed)
                return;

            //Expressions Menu
            builder.BuildActionValues(validActions);
            builder.BuildExpressionsMenu(this);

            //Build normal
            builder.BuildNormalLayers(validActions, AnimationLayer.Action);
            builder.BuildNormalLayers(validActions, AnimationLayer.FX);

            //Build sliders
            builder.BuildSliderLayers(validActions, AnimationLayer.Action);
            builder.BuildSliderLayers(validActions, AnimationLayer.FX);

            //Sub Actions
            builder.BuildSubActionLayers(validActions, AnimationLayer.Action);
            builder.BuildSubActionLayers(validActions, AnimationLayer.FX);

            //States
            builder.SetActionStates(validActions);
        }

        public virtual void SetState(ActionsBuilder builder, string parameter)
        {
            //Collect all menu actions
            var validActions = new List<MenuAction>();
            CollectValidMenuActions(builder, validActions);

            validActions.RemoveAll((action) => { return action.parameter != parameter; });

            foreach (var action in validActions)
            {
                action.SetState(builder);
            }
        }

        void CollectValidMenuActions(ActionsBuilder builder, List<MenuAction> output)
        {
            //Add our actions
            int selfAdded = 0;
            foreach (var action in actions)
            {
                //Enabled
                if (!action.ShouldBuild())
                    continue;

                //Parameter
                bool needsParameter = action.NeedsControlLayer();
                if (needsParameter && string.IsNullOrEmpty(action.parameter))
                {
                    builder.BuildFailed = true;
                    EditorUtility.DisplayDialog("Build Error", $"Action '{action.name}' doesn't specify a parameter.", "Okay");
                    return;
                }

                //Check type
                if (action.menuType == MenuAction.MenuType.SubMenu)
                {
                    //Sub-Menus
                    if (action.subMenu != null)
                        action.subMenu.CollectValidMenuActions(builder, output);
                }
                else if (action.menuType == MenuAction.MenuType.PreExisting)
                {
                    //Do Nothing
                }
                else
                {
                    //Add
                    output.Add(action);
                }

                //Increment
                selfAdded += 1;
            }

            //Validate
            if (selfAdded > ExpressionsMenu.MAX_CONTROLS)
            {
                builder.BuildFailed = true;
                EditorUtility.DisplayDialog("Build Failed", $"{name} has too many actions defined.", "Okay");
            }
        }
    }
}
#endif