using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    [CustomPropertyDrawer(typeof(EnumAsIndexAttribute))]
    public class EnumAsIndexPropertyDrawer : ArrayElementPropertyDrawer
    {

        private int GetEnumLength(Type enumType)
        {
            int[] values = (int[])Enum.GetValues(enumType);
            return values.Max() + 1;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!CheckIfArrayOrDrawDefault(position, property, label)) return;

            EnumAsIndexAttribute enumAsIndexAttribute = attribute as EnumAsIndexAttribute;

            Type enumType = enumAsIndexAttribute.EnumType;

            string[] enumNames = Enum.GetNames(enumType);
            int enumLength = GetEnumLength(enumType);

            string arrayName = GetPropertyArrayPathByItemPath(property.propertyPath);
            int index = GetPropertyIndexByPath(property.propertyPath);

            SerializedProperty arrayProperty = property.serializedObject.FindProperty(arrayName);

            if (arrayProperty.arraySize != enumLength)
            {
                arrayProperty.arraySize = enumLength;
            }

            int enumIndex = Array.IndexOf((int[])Enum.GetValues(enumType), index);
            if (enumIndex < 0 || enumIndex >= enumNames.Length)
                return;

            GUIContent myLabel = new GUIContent(StringUtility.GetHumanReadableText(enumNames[enumIndex]));

            CustomPropertyDrawerUtility.DrawPropertyField(fieldInfo, position, property, myLabel,
                typeof(EnumAsIndexAttribute));
        }
    }
}