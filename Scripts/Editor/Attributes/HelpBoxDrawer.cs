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
            float result = 0;
            if (!string.IsNullOrEmpty(helpBoxAttribute.Message))
            {
                result += EditorGUI.GetPropertyHeight(property, label, true);
            }

            if (!string.IsNullOrEmpty(message))
            {
                result += GetHelpBoxHeight();
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

            Rect helpBoxPosition = new Rect(position.x, position.y, position.width, GetHelpBoxHeight());
            Rect remainingPosition = new Rect(position.x, position.y + GetHelpBoxHeight(),
                position.width, position.height - GetHelpBoxHeight());

            string message = GetMessage(property);
            if (!string.IsNullOrEmpty(message))
            {
                EditorGUI.HelpBox(helpBoxPosition, message,
                    (MessageType) helpBoxAttribute.MessageType);
            }

            if (!string.IsNullOrEmpty(helpBoxAttribute.Message))
            {
                EditorGUI.PropertyField(remainingPosition, property, label, true);
            }
        }
    }
}
