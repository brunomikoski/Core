using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrunoMikoski.Core.Utils
{
    [Serializable]
    public class MinMaxFloat
    {
        [SerializeField] float _min;
        public float Min => _min;

        [SerializeField] float _max;
        public float Max => _max;
        
        public float Range => _max - _min;
        public float RandomInclusive => UnityEngine.Random.Range(_min, _max);
        public MinMaxFloat()
        {
        }

        public MinMaxFloat(float min, float max)
        {
            this._min = min;
            this._max = max;
        }

        public float Lerp(float t)
        {
            return Mathf.Lerp(_min, _max, t);
        }
        
        public float LerpUnclamped(float t)
        {
            return Mathf.LerpUnclamped(_min, _max, t);
        }

        public float InverseLerp(float value)
        {
            return Mathf.InverseLerp(_min, _max, value);
        }

        public bool Contains(float value) {
            return value >= _min && value <= _max;
        }
    }
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MinMaxFloat))]
    public class MinMaxFloatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            const float labelWidth = 45f;
            float fieldWidth = (position.width - labelWidth * 2 - 10f) / 2;
            SerializedProperty minProp = property.FindPropertyRelative("_min");
            SerializedProperty maxProp = property.FindPropertyRelative("_max");
            
            Rect minLabelRect = new Rect(position.x, position.y, labelWidth, position.height);
            Rect minFieldRect = new Rect(minLabelRect.xMax, position.y, fieldWidth, position.height);
            Rect maxLabelRect = new Rect(minFieldRect.xMax + 5, position.y, labelWidth, position.height);
            Rect maxFieldRect = new Rect(maxLabelRect.xMax, position.y, fieldWidth, position.height);

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
