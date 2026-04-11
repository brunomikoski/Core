using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrunoMikoski.Core.Utils
{
    [System.Serializable]
    public struct MinMaxInt
    {
        public int Min;
        public int Max;

        public MinMaxInt(int min, int max) {
            Min = min;
            Max = max;
        }

        public int GetRandomValue()
        {
            return UnityEngine.Random.Range(Min, Max + 1);
        }
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MinMaxInt))]
    public class MinMaxIntDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Label widths and spacing
            const float labelWidth = 45f;
            float fieldWidth = (position.width - labelWidth * 2 - 10f) / 2;
            SerializedProperty minProp = property.FindPropertyRelative("Min");
            SerializedProperty maxProp = property.FindPropertyRelative("Max");
            
            // Rects
            Rect minLabelRect = new Rect(position.x, position.y, labelWidth, position.height);
            Rect minFieldRect = new Rect(minLabelRect.xMax, position.y, fieldWidth, position.height);
            Rect maxLabelRect = new Rect(minFieldRect.xMax + 5, position.y, labelWidth, position.height);
            Rect maxFieldRect = new Rect(maxLabelRect.xMax, position.y, fieldWidth, position.height);

            // Draw labels and fields
            EditorGUI.LabelField(minLabelRect, "Min:");
            EditorGUI.PropertyField(minFieldRect, minProp, GUIContent.none);

            EditorGUI.LabelField(maxLabelRect, "Max:");
            EditorGUI.PropertyField(maxFieldRect, maxProp, GUIContent.none);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
#endif
}
