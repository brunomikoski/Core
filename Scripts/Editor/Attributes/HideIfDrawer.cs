using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    [CustomPropertyDrawer(typeof(HideIfAttribute), true)]
    public class HideIfDrawer : ConditionalDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (CheckCondition(property))
                return -2;

            return CustomPropertyDrawerUtility.GetPropertyHeight(fieldInfo, property, label,
                typeof(ConditionalAttribute));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (CheckCondition(property))
                return;

            CustomPropertyDrawerUtility.DrawPropertyField(fieldInfo, position, property, label,
                typeof(ConditionalAttribute));
        }
    }
}