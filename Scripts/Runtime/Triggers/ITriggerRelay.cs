namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    public interface ITriggerRelay
    {
        void RequestStart(object source);
        void RequestStop(object source);
    }
}
