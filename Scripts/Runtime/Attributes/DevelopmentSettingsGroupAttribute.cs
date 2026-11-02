using System;

namespace BrunoMikoski.Core.DevelopmentTools
{
    // Marks a static class whose DevelopmentSettingGeneric fields show in the development settings windows.
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DevelopmentSettingsGroupAttribute : Attribute
    {
        public string DisplayName { get; }

        public DevelopmentSettingsGroupAttribute(string displayName = null)
        {
            DisplayName = displayName;
        }
    }
}
