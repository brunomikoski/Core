using System.Linq;
using UnityEditor;

namespace BrunoMikoski.Core.Attributes
{
    public class ConditionalDrawer : PropertyDrawer
    {
        protected ConditionalAttribute ConditionalAttribute => (ConditionalAttribute)attribute;

        private ConditionalAttribute[] conditionalAttributes;
        protected ConditionalAttribute[] ConditionalAttributes
        {
            get
            {
                if (conditionalAttributes == null)
                {
                    conditionalAttributes = fieldInfo
                        .GetCustomAttributes(typeof(ConditionalAttribute), true)
                        .Cast<ConditionalAttribute>().ToArray();
                }
                return conditionalAttributes;
            }
        }

        public bool CheckCondition(SerializedProperty property)
        {
            bool result = true;
            for (int i = 0; i < ConditionalAttributes.Length; i++)
            {
                ConditionalAttribute conditionalAttribute = ConditionalAttributes[i];
                if (i == 0)
                {
                    result = conditionalAttribute.CheckCondition(property);
                }
                else
                {
                    if (conditionalAttribute.ConditionOperator == ConditionOperator.Or)
                    {
                        result = result || conditionalAttribute.CheckCondition(property);
                    }
                    else
                    {
                        result = result && conditionalAttribute.CheckCondition(property);
                    }
                }
            }
            return result;
        }
    }
}