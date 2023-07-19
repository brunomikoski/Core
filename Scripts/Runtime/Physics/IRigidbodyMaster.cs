namespace BrunoMikoski.Core.PhysX
{
    public interface IRigidbodyMaster
    {
        RigidbodyWrapper RigidbodyWrapper { get; }
        
        void Release(RigidbodyWrapper slave);
    }
}