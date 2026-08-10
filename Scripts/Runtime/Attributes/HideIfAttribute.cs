namespace BrunoMikoski.Core.Attributes
{
    public class HideIfAttribute : ConditionalAttribute
    {
        public HideIfAttribute()
        {
        }

        public HideIfAttribute(string dependingOnField, params object[] whenValues)
            : base(dependingOnField, whenValues)
        {
        }

        public HideIfAttribute(ConditionOperator conditionOperator, string dependingOnField,
            params object[] whenValues) : base(conditionOperator, dependingOnField, whenValues)
        {
        }
    }
}