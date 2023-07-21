namespace BrunoMikoski.Core.Attributes
{
    public class ShowIfAttribute : ConditionalAttribute
    {
        public ShowIfAttribute()
        {
        }

        public ShowIfAttribute(string dependingOnField, params object[] whenValues)
            : base(dependingOnField, whenValues)
        {
        }

        public ShowIfAttribute(ConditionOperator conditionOperator, string dependingOnField,
            params object[] whenValues) : base(conditionOperator, dependingOnField, whenValues)
        {
        }
    }
}
