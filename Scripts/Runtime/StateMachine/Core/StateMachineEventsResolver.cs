namespace BrunoMikoski.Core.StateMachines
{
    public static class StateMachineResolver
    {
        public static void RegisterStateMachine<T>(StateMachine<T> stateMachine) where T : StateID
        {
            
        }

        public static void UnregisterStateMachine<T>(StateMachine<T> stateMachine) where T : StateID
        {
            
        }
    }
}