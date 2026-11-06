using System;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public interface IMutableTagSource : ITagSource
    {
        event Action<Tag> OnTagAddedEvent;
        event Action<Tag> OnTagRemovedEvent;
        event Action OnReadyEvent;

        bool IsReady { get; }

        bool AddTag(Tag tag);
        bool AddTag(Tag tag, float duration, Tag expireTag = null, TagRefreshMode refreshMode = TagRefreshMode.Restart);
        bool RemoveTag(Tag tag);
        bool TryGetRemainingTime(Tag tag, out float remainingSeconds);
        void SetRelay(ITagRelay relay);
    }
}
