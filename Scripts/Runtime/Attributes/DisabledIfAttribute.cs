namespace BrunoMikoski.Core.Attributes
{
    public class DisabledIfAttribute : ReadOnlyAttribute
    {
        public DisabledIfAttribute()
        {
        }

        public DisabledIfAttribute(string dependingOnField, params object[] whenValues) : base(
            dependingOnField, whenValues)
        {
        }
    }
}