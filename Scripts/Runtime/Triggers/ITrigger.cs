namespace BrunoMikoski.Core.Triggers
{
    public interface ITrigger
    {
        bool IsConnected(Triggerable triggerable);
    }
}