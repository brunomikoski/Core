using System;
using System.Collections.Generic;
using System.Reflection;
using BrunoMikoski.Core.System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    public static class CustomPropertyDrawerUtility
    {
        private struct PropertyDrawerData
        {
            public Type DrawerType;
            public Type TargetType;
            public int DistanceToTarget;
        }

        private static Dictionary<Type, PropertyDrawerData> TYPES_TO_PROPERTY_DRAWERS;
        private static Dictionary<Type, Dictionary<string, PropertyDrawer>> TYPES_TO_PROPERTY_DRAWER_INSTANCE;

        private static FieldInfo FIELD_INFO_FIELD_INFO;
        private static FieldInfo ATTRIBUTE_FIELD_INFO;

        /// <summary>
        /// You can use this method to draw a property field with its custom property drawer except
        /// for attribute-based property drawers
        /// </summary>
        public static void DrawPropertyField(FieldInfo fieldInfo, Rect position,
            SerializedProperty property, GUIContent label, params Type[] ignoreAttributeTypes)
        {
            DrawPropertyField(fieldInfo, null, position, property, label,
                ignoreAttributeTypes);
        }

        /// <summary>
        /// You can use this method to draw a property field with its custom property drawer except
        /// for attribute-based property drawers
        /// </summary>
        public static void DrawPropertyField(FieldInfo fieldInfo, Type forceFieldType, Rect position,
            SerializedProperty property, GUIContent label, params Type[] ignoreAttributeTypes)
        {
            if (EditorApplication.isCompiling)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            PropertyDrawer propertyDrawer = CreatePropertyDrawer(fieldInfo, forceFieldType, property);

            if (propertyDrawer == null)
            {
                PropertyAttribute targetAttribute
                    = FindPropertyAttribute(fieldInfo, ignoreAttributeTypes);

                if (targetAttribute != null)
                {
                    propertyDrawer = CreatePropertyDrawer(targetAttribute, fieldInfo, property);
                }
            }


            if (propertyDrawer != null)
            {
                propertyDrawer.OnGUI(position, property, label);
            }
            else
            {
                if (forceFieldType != null)
                {
                    EditorGUI.BeginProperty(position, label, property);

                    property.objectReferenceValue
                        = EditorGUI.ObjectField(position, label, property.objectReferenceValue,
                            forceFieldType, true);

                    EditorGUI.EndProperty();
                }
                else
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }
        }

        private static PropertyDrawer CreatePropertyDrawer(PropertyAttribute propertyAttribute,
            FieldInfo fieldInfo, SerializedProperty property)
        {
            PropertyDrawer result = CreatePropertyDrawer(propertyAttribute.GetType(), fieldInfo, property);

            if (result == null)
                return null;

            if (ATTRIBUTE_FIELD_INFO == null)
            {
                ATTRIBUTE_FIELD_INFO = typeof(PropertyDrawer).GetField("m_Attribute",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (ATTRIBUTE_FIELD_INFO != null) 
                ATTRIBUTE_FIELD_INFO.SetValue(result, propertyAttribute);

            return result;
        }

        private static PropertyAttribute FindPropertyAttribute(FieldInfo fieldInfo,
            IList<Type> ignoreAttributeTypes)
        {
            if (fieldInfo == null)
                return null;

            object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(PropertyAttribute), true);

            PropertyAttribute targetAttribute = null;
            for (int i = 0; i < customAttributes.Length; i++)
            {
                object customAttribute = customAttributes[i];
                Type customAttributeType = customAttribute.GetType();
                bool ignore = false;
                for (int j = 0; j < ignoreAttributeTypes.Count; j++)
                {
                    Type ignoreAttributeType = ignoreAttributeTypes[j];
                    if (ignoreAttributeType.IsAssignableFrom(customAttributeType))
                    {
                        ignore = true;
                        break;
                    }
                }
                if (!ignore)
                {
                    targetAttribute = (PropertyAttribute)customAttribute;
                    break;
                }
            }
            return targetAttribute;
        }

        private static PropertyDrawer CreatePropertyDrawer(FieldInfo fieldInfo, Type forceFieldType,
            SerializedProperty property)
        {
            if (forceFieldType == null)
                forceFieldType = fieldInfo.FieldType;

            Type elementType = forceFieldType.GetArrayOrListType();

            if (elementType != null)
                forceFieldType = elementType;

            return CreatePropertyDrawer(forceFieldType, fieldInfo, property);
        }

        private static PropertyDrawer CreatePropertyDrawer(Type propertyDrawerSelectionType,
            FieldInfo fieldInfo, SerializedProperty property)
        {
            Type type = propertyDrawerSelectionType;

            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition();
            }

            if (!TYPES_TO_PROPERTY_DRAWER_INSTANCE.ContainsKey(type))
            {
                TYPES_TO_PROPERTY_DRAWER_INSTANCE[type] = new Dictionary<string, PropertyDrawer>();
            }

            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            ulong id = targetObject != null ? EntityId.ToULong(targetObject.GetEntityId()) : 0;
            string key = $"{id}{property.propertyPath}";

            if (!TYPES_TO_PROPERTY_DRAWER_INSTANCE[type].ContainsKey(key))
            {
                if (!TYPES_TO_PROPERTY_DRAWERS.ContainsKey(type))
                {
                    return null;
                }
                TYPES_TO_PROPERTY_DRAWER_INSTANCE[type][key] =
                    Activator.CreateInstance(TYPES_TO_PROPERTY_DRAWERS[type].DrawerType) as PropertyDrawer;
            }

            FIELD_INFO_FIELD_INFO.SetValue(TYPES_TO_PROPERTY_DRAWER_INSTANCE[type][key], fieldInfo);
            return TYPES_TO_PROPERTY_DRAWER_INSTANCE[type][key];
        }

        public static float GetPropertyHeight(FieldInfo fieldInfo, SerializedProperty property,
            GUIContent label, params Type[] ignoreAttributeTypes)
        {
            return GetPropertyHeight(fieldInfo, null, property, label,
                ignoreAttributeTypes);
        }

        public static float GetPropertyHeight(FieldInfo fieldInfo, Type forceFieldType,
            SerializedProperty property, GUIContent label, params Type[] ignoreAttributeTypes)
        {
            if (EditorApplication.isCompiling)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            PropertyDrawer propertyDrawer = CreatePropertyDrawer(fieldInfo, forceFieldType,
                property);

            if (propertyDrawer == null)
            {
                PropertyAttribute targetAttribute =
                    FindPropertyAttribute(fieldInfo, ignoreAttributeTypes);

                if (targetAttribute != null)
                {
                    propertyDrawer = CreatePropertyDrawer(targetAttribute, fieldInfo, property);
                }
            }

            if (propertyDrawer == null)
                return EditorGUI.GetPropertyHeight(property, label, true);
            else
                return propertyDrawer.GetPropertyHeight(property, label);
        }


        private static void OnSelectionChanged()
        {
            TYPES_TO_PROPERTY_DRAWER_INSTANCE.Clear();
        }
    }
}
