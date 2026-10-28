using System;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public interface IMutableTagSource : ITagSource
    {
        event Action<Tag> OnTagAddedEvent;
        event Action<Tag> OnTagRemovedEvent;

        bool AddTag(Tag tag);
        bool RemoveTag(Tag tag);
    }
}
