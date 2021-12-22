#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VRCAvatarActions
{
    [CustomEditor(typeof(Gestures))]
    public class GesturesEditor : BaseActionsEditor
    {
        Gestures gestureScript;

        public override void Inspector_Header()
        {
            gestureScript = target as Gestures;
            EditorGUILayout.HelpBox("Gestures - Simplified actions controlled by gestures.", MessageType.Info);

            //Default Action
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                //Side
                gestureScript.side = (Gestures.GestureSide)EditorGUILayout.EnumPopup("Side", gestureScript.side);
            }
            EditorGUILayout.EndVertical();
        }
        public override void Inspector_Action_Header(BaseActions.Action action)
        {
            var gestureAction = (Gestures.GestureAction)action;

            //Name
            action.name = EditorGUILayout.TextField("Name", action.name);

            //Gesture
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                EditorGUILayout.LabelField("Gesture Type");
                DrawGestureToggle("Neutral", GestureEnum.Neutral);
                DrawGestureToggle("Fist", GestureEnum.Fist);
                DrawGestureToggle("Open Hand", GestureEnum.OpenHand);
                DrawGestureToggle("Finger Point", GestureEnum.FingerPoint);
                DrawGestureToggle("Victory", GestureEnum.Victory);
                DrawGestureToggle("Rock N Roll", GestureEnum.RockNRoll);
                DrawGestureToggle("Hand Gun", GestureEnum.HandGun);
                DrawGestureToggle("Thumbs Up", GestureEnum.ThumbsUp);

                void DrawGestureToggle(string name, GestureEnum type)
                {
                    var value = gestureAction.gestureTable.GetValue(type);
                    EditorGUI.BeginDisabledGroup(!value && !CheckGestureTypeUsed(type));
                    gestureAction.gestureTable.SetValue(type, EditorGUILayout.Toggle(name, value));
                    EditorGUI.EndDisabledGroup();
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Warning
            if(!gestureAction.gestureTable.IsModified())
            {
                EditorGUILayout.HelpBox("No conditions currently selected.", MessageType.Warning);
            }
        }
        bool CheckGestureTypeUsed(GestureEnum type)
        {
            foreach (var action in gestureScript.actions)
            {
                if (action.gestureTable.GetValue(type))
                    return false;
            }
            return true;
        }
    }
}
#endif