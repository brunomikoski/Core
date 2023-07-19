namespace BrunoMikoski.Core
{
    public interface IOnUpdate
    {
        void OnUpdate();
    }
    
    public interface ILateUpdate
    {
        void OnLateUpdate();
    }
    
    public interface IFixedUpdate
    {
        void OnFixedUpdate();
    }
}