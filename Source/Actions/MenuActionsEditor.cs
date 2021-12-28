#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;

namespace VRCAvatarActions
{
    [CustomEditor(typeof(MenuActions))]
    public class MenuActionsEditor : BaseActionsEditor
    {
        MenuActions menuScript;
        private Editor subMenuEditor = null;

        public override void Inspector_Header()
        {
            EditorGUILayout.HelpBox("Menu Actions - Actions that are displayed in the avatar's action menu.", MessageType.Info);
        }

        public override void Inspector_Body()
        {
            menuScript = target as MenuActions;

            int actionCount = 0;
            foreach (var action in menuScript.actions)
            {
                if (action.ShouldBuild())
                    actionCount += 1;
            }

            if (actionCount > ExpressionsMenu.MAX_CONTROLS)
            {
                EditorGUILayout.HelpBox($"Too many actions are defined, disable or delete until there are only {ExpressionsMenu.MAX_CONTROLS}", MessageType.Error);
            }

            base.Inspector_Body();
        }

        public override void Inspector_Action_Header(BaseActions.Action action)
        {
            //Base
            base.Inspector_Action_Header(action);

            //Type
            var menuAction = (MenuActions.MenuAction)action;
            menuAction.menuType = (MenuActions.MenuAction.MenuType)EditorGUILayout.EnumPopup("Type", menuAction.menuType);

            //Icon
            if (menuAction.menuType != MenuActions.MenuAction.MenuType.PreExisting)
                menuAction.icon = (Texture2D)EditorGUILayout.ObjectField("Icon", menuAction.icon, typeof(Texture2D), false);
        }

        public override void Inspector_Action_Body(BaseActions.Action action, bool showParam = true)
        {
            //Details
            var menuAction = (MenuActions.MenuAction)action;
            switch (menuAction.menuType)
            {
                case MenuActions.MenuAction.MenuType.Button:
                case MenuActions.MenuAction.MenuType.Toggle:
                    Inspector_Control(menuAction);
                    break;
                case MenuActions.MenuAction.MenuType.Slider:
                    DrawInspector_Slider(menuAction);
                    break;
                case MenuActions.MenuAction.MenuType.SubMenu:
                    DrawInspector_SubMenu(menuAction);
                    break;
                case MenuActions.MenuAction.MenuType.PreExisting:
                    EditorGUILayout.HelpBox("Pre-Existing will preserve custom expression controls with the same name.", MessageType.Info);
                    break;
            }
        }

        public void Inspector_Control(MenuActions.MenuAction action)
        {
            //Parameter
            action.parameter = DrawParameterDropDown(action.parameter, "Parameter");

            if (action.menuType == MenuActions.MenuAction.MenuType.Toggle)
            {
                string tooltip = "This action will be used when no other toggle with the same parameter is turned on.\n\nOnly one action can be marked as the off state for a parameter name.";
                action.isOffState = EditorGUILayout.Toggle(new GUIContent("Is Off State", tooltip), action.isOffState);
            }

            //Default
            base.Inspector_Action_Body(action);

            //Sub Actions
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            action.foldoutSubActions = EditorGUILayout.Foldout(action.foldoutSubActions, Title("Sub Actions", action.subActions.Count > 0));
            if (action.foldoutSubActions)
            {
                //Add
                if (GUILayout.Button("Add"))
                {
                    action.subActions.Add(null);
                }

                //Sub-Actions
                for (int i = 0; i < action.subActions.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    action.subActions[i] = (NonMenuActions)EditorGUILayout.ObjectField(action.subActions[i], typeof(NonMenuActions), false);
                    if (GUILayout.Button("X", GUILayout.Width(32)))
                    {
                        action.subActions.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }

        public void DrawInspector_SubMenu(MenuActions.MenuAction action)
        {
            EditorGUILayout.BeginHorizontal();
            action.subMenu = (MenuActions)EditorGUILayout.ObjectField("Sub Menu", action.subMenu, typeof(MenuActions), false);
            EditorGUI.BeginDisabledGroup(action.subMenu != null);
            if (GUILayout.Button("New", GUILayout.Width(64f)))
            {
                //Create
                var subMenu = CreateInstance<MenuActions>();
                subMenu.name = $"Menu {action.name}";
                ActionsBuilder.SaveAsset(subMenu, script, null, true);

                //Set
                action.subMenu = subMenu;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (action.subMenu != null)
            {
                DrawFoldoutInspector(action.subMenu, ref subMenuEditor);

                if (subMenuEditor is MenuActionsEditor menuActionsEditor)
                {
                    menuActionsEditor.isSubInspector = true;
                    menuActionsEditor.avatarDescriptor = avatarDescriptor;
                }
            }
        }

        public void DrawInspector_Slider(MenuActions.MenuAction action)
        {
            //Parameter
            action.parameter = DrawParameterDropDown(action.parameter, "Parameter");

            //Animations
            EditorGUI.BeginDisabledGroup(true); //Disable for now
            action.actionLayerAnimations.enter = DrawAnimationReference("Action Layer", action.actionLayerAnimations.enter, $"{action.name}_Action_Slider");
            EditorGUI.EndDisabledGroup();
            action.fxLayerAnimations.enter = DrawAnimationReference("FX Layer", action.fxLayerAnimations.enter, $"{action.name}_FX_Slider");
        }
    }
}
#endif