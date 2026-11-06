namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    public interface ITriggerRelay
    {
        void RequestStart(TriggerContext context);
        void RequestStop(TriggerContext context);
    }
}
