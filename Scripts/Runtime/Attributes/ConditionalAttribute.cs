using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrunoMikoski.Core.Attributes
{
    
    public enum ConditionOperator
    {
        And,
        Or
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public abstract class ConditionalAttribute : PropertyAttribute
    {
        private readonly ConditionOperator conditionOperator = ConditionOperator.And;
        private readonly string dependingOnField;
        private readonly object[] whenValues;

        protected ConditionalAttribute()
        {
            dependingOnField = null;
        }

        protected ConditionalAttribute(ConditionOperator conditionOperator, string dependingOnField,
            params object[] whenValues)
        {
            this.dependingOnField = dependingOnField;
            this.whenValues = whenValues;
            this.conditionOperator = conditionOperator;
        }

        protected ConditionalAttribute(string dependingOnField, params object[] whenValues)
        {
            this.dependingOnField = dependingOnField;
            this.whenValues = whenValues;
        }

        public string DependingOnField => dependingOnField;

        public object[] WhenValues => whenValues;

        public ConditionOperator ConditionOperator => conditionOperator;

        public bool Matches<T>(T query)
        {
            if (whenValues == null)
            {
                return Equals(query, null);
            }

            if (whenValues.Length == 0)
                return query.Equals(true);

            for (int i = 0; i < whenValues.Length; i++)
            {
                if (Equals(query, (T) whenValues[i]))
                    return true;
            }

            return false;
        }

#if UNITY_EDITOR
        public bool CheckCondition(SerializedProperty property)
        {
            if (string.IsNullOrEmpty(DependingOnField))
            {
                return true;
            }

            SerializedProperty dependingOn = FindDependingOnProperty(property);

            if (dependingOn == null)
            {
                Debug.LogError("Could not find property: " + DependingOnField);
                return true;
            }

            switch (dependingOn.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                    return Matches(dependingOn.intValue);
                case SerializedPropertyType.Boolean:
                    return Matches(dependingOn.boolValue);
                case SerializedPropertyType.String:
                    return Matches(dependingOn.stringValue);
                case SerializedPropertyType.ObjectReference:
                    return Matches(dependingOn.objectReferenceValue);
                case SerializedPropertyType.Enum:
                    return Matches(dependingOn.intValue);
                case SerializedPropertyType.ArraySize:
                    return Matches(dependingOn.arraySize);
                default:
                    throw new ArgumentOutOfRangeException("dependingOn.propertyType");
            }
        }

        public SerializedProperty FindDependingOnProperty(SerializedProperty property)
        {
            string currentPath = property.propertyPath;
            string siblingPath = currentPath.Substring(0,
                currentPath.Length - property.name.Length) + dependingOnField;

            return property.serializedObject.FindProperty(siblingPath);
        }
#endif
    }
}
