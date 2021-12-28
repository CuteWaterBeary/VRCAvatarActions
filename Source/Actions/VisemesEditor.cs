#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarActions
{
    [CustomEditor(typeof(Visemes))]
    public class VisemesEditor : BaseActionsEditor
    {
        Visemes visemesScript;
        IEnumerable<VisemeEnum> VisemeValues;

        public void OnEnable()
        {
            VisemeValues = System.Enum.GetValues(typeof(VisemeEnum)).Cast<VisemeEnum>();
            visemesScript = target as Visemes;
        }

        public override void Inspector_Header()
        {
            EditorGUILayout.HelpBox("Visimes - Simplified actions triggered by visimes.", MessageType.Info);
        }

        public override void Inspector_Action_Header(BaseActions.Action action)
        {
            var VisemeAction = (Visemes.VisemeAction)action;

            //Name
            action.name = EditorGUILayout.TextField("Name", action.name);

            //Viseme
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                EditorGUILayout.LabelField("Viseme");
                foreach (var value in VisemeValues)
                    DrawVisemeToggle(value.ToString(), value);

                void DrawVisemeToggle(string name, VisemeEnum type)
                {
                    var value = VisemeAction.visimeTable.GetValue(type);
                    EditorGUI.BeginDisabledGroup(!value && !CheckVisemeTypeUsed(type));
                    VisemeAction.visimeTable.SetValue(type, EditorGUILayout.Toggle(name, value));
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Warning
            if (!VisemeAction.visimeTable.IsModified())
            {
                EditorGUILayout.HelpBox("No conditions currently selected.", MessageType.Warning);
            }
        }

        bool CheckVisemeTypeUsed(VisemeEnum type)
        {
            foreach (var action in visemesScript.actions)
            {
                if (action.visimeTable.GetValue(type))
                    return false;
            }
            return true;
        }
    }
}
#endif