using System.Collections.Generic;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public interface ITagSource
    {
        IReadOnlyList<Tag> Tags { get; }
    }
}
