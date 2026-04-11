using System;

namespace BrunoMikoski.Core
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ExposeOnDeveloperSettingsAttribute : Attribute
    {
        public string DisplayName { get; }

        public ExposeOnDeveloperSettingsAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}