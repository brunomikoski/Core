namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    public interface ITrigger
    {
        bool IsConnected(Triggerable triggerable);
    }
}