using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    [CustomPropertyDrawer(typeof(ShowIfMethodAttribute), true)]
    public class ShowIfMethodDrawer : PropertyDrawer
    {
        protected ShowIfMethodAttribute ConditionalAttribute => (ShowIfMethodAttribute)attribute;

        private Dictionary<string, MethodInfo> validationMethods = new Dictionary<string, MethodInfo>();

        private ShowIfMethodAttribute[] showIfMethodAttributes;
        protected ShowIfMethodAttribute[] ShowIfMethodAttributes
        {
            get
            {
                if (showIfMethodAttributes == null)
                {
                    showIfMethodAttributes = fieldInfo
                        .GetCustomAttributes(typeof(ShowIfMethodAttribute), true)
                        .Cast<ShowIfMethodAttribute>().ToArray();
                }
                return showIfMethodAttributes;
            }
        }
        
        public bool CheckCondition(SerializedProperty property)
        {
            for (int i = 0; i < ShowIfMethodAttributes.Length; i++)
            {
                ShowIfMethodAttribute showIfMethodAttribute = ShowIfMethodAttributes[i];

                if (!validationMethods.TryGetValue(property.propertyPath, out MethodInfo validationMethod))
                {
                    validationMethod = property.serializedObject.targetObject.GetType()
                        .GetMethod(showIfMethodAttribute.MethodName, BindingFlags.Public |
                                                                     BindingFlags.NonPublic |
                                                                     BindingFlags.Instance |
                                                                     BindingFlags.Static);
                    
                    validationMethods.Add(property.propertyPath, validationMethod);
                }

                if (validationMethod == null)
                {
                    Debug.LogWarning($"Failed to find method {showIfMethodAttribute.MethodName} in {property.serializedObject.targetObject.GetType().Name}");
                    continue;
                }
                
                bool result = (bool) validationMethod.Invoke(property.serializedObject.targetObject, null);
                if (result != showIfMethodAttribute.ExpectedValue)
                    return false;
            }
            return true;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!CheckCondition(property))
                return -2;

            return base.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!CheckCondition(property))
                return;
            
            EditorGUI.PropertyField(position, property, label, true);
        }
        
    }
}