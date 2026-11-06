using System;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public interface ITagRelay
    {
        bool IsReady { get; }
        event Action OnReadyEvent;

        bool CanRelay(Tag tag);
        bool RequestAddTag(Tag tag, float duration, Tag expireTag, TagRefreshMode refreshMode);
        bool TryGetRemainingTime(Tag tag, out float remainingSeconds);
    }
}
