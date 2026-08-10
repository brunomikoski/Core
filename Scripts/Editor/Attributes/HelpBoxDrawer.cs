using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    [CustomPropertyDrawer(typeof(HelpBoxAttribute))]
    public class HelpBoxDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            HelpBoxAttribute helpBoxAttribute = attribute as HelpBoxAttribute;
            string message = GetMessage(property);
            bool hasMessage = !string.IsNullOrEmpty(message);
            bool hasProperty = !string.IsNullOrEmpty(helpBoxAttribute.Message);

            float result = 0;
            if (hasProperty)
            {
                result += EditorGUI.GetPropertyHeight(property, label, true);
            }

            if (hasMessage)
            {
                result += GetHelpBoxHeight();
            }

            if (hasProperty && hasMessage)
            {
                result += 2f;
            }

            return result;
        }

        private float GetHelpBoxHeight()
        {
            HelpBoxAttribute helpBoxAttribute = attribute as HelpBoxAttribute;
            return EditorGUIUtility.singleLineHeight * helpBoxAttribute.LineCount;
        }

        /// <summary>
        /// Gets the message for this field. If no message is supplied to the HelpBoxAttribute, the
        /// field is assumed to contain the message. The message is either taken from the field
        /// directly, or from a property with the same name
        /// </summary>
        private string GetMessage(SerializedProperty property)
        {
            HelpBoxAttribute helpBoxAttribute = attribute as HelpBoxAttribute;
            string message = helpBoxAttribute.Message;

            if (helpBoxAttribute.Message == null)
            {
                // If the message in the attribute is null, try to find the value in the property itself.

                // First look up the property belonging to this field
                UnityEngine.Object target = property.serializedObject.targetObject;
                Type @type = fieldInfo.DeclaringType;
                PropertyInfo propertyInfo
                    = @type.GetProperty(fieldInfo.Name,
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.FlattenHierarchy
                        | BindingFlags.Instance | BindingFlags.NonPublic);

                if (propertyInfo != null)
                {
                    message = propertyInfo.GetValue(target, null) as string;
                }
                else
                {
                    message = property.stringValue;
                }
            }

            return message;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HelpBoxAttribute helpBoxAttribute = attribute as HelpBoxAttribute;
            string message = GetMessage(property);
            bool hasMessage = !string.IsNullOrEmpty(message);
            bool hasProperty = !string.IsNullOrEmpty(helpBoxAttribute.Message);
            float helpBoxHeight = GetHelpBoxHeight();

            if (hasProperty && hasMessage)
            {

                float innerX = position.x;
                float innerY = position.y;
                float innerWidth = position.width - 2f;

                float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
                Rect propertyRect = new Rect(innerX, innerY, innerWidth, propertyHeight);
                Rect helpBoxRect = new Rect(innerX, innerY + propertyHeight,
                    innerWidth, helpBoxHeight);

                EditorGUI.PropertyField(propertyRect, property, label, true);
                EditorGUI.HelpBox(helpBoxRect, message, (MessageType) helpBoxAttribute.MessageType);
            }
            else if (hasMessage)
            {
                Rect helpBoxRect = new Rect(position.x, position.y, position.width, helpBoxHeight);
                EditorGUI.HelpBox(helpBoxRect, message, (MessageType) helpBoxAttribute.MessageType);
            }
            else if (hasProperty)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }
}
