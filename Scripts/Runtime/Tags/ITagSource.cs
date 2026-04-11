using System.Collections.Generic;

namespace BrunoMikoski.Core
{
    public interface ITagSource
    {
        IReadOnlyList<Tag> Tags { get; }
    }
}
