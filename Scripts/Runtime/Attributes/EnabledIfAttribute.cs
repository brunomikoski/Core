namespace BrunoMikoski.Core.Attributes
{
    public class EnabledIfAttribute : ReadOnlyAttribute
    {
        public EnabledIfAttribute()
        {
        }

        public EnabledIfAttribute(string dependingOnField, params object[] whenValues) : base(
            dependingOnField, whenValues)
        {
        }
    }
}