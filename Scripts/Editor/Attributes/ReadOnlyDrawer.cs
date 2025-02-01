using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute), true)]
    public class ReadOnlyDrawer : ConditionalDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return CustomPropertyDrawerUtility.GetPropertyHeight(
                fieldInfo,
                property,
                label,
                typeof(ConditionalAttribute),
                typeof(ReadOnlyAttribute)
            );
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginDisabledGroup(CheckCondition(property));

            CustomPropertyDrawerUtility.DrawPropertyField(
                fieldInfo,
                position,
                property,
                label,
                typeof(ConditionalAttribute),
                typeof(ReadOnlyAttribute)
            );

            EditorGUI.EndDisabledGroup();
        }
    }
}