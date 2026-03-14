namespace BrunoMikoski.Core.Routines
{
    public class WaitForRealSeconds : IRoutineYield
    {
        private float startTime;
        private float seconds;

        public WaitForRealSeconds(float seconds)
        {
            startTime = UnityEngine.Time.realtimeSinceStartup;
            this.seconds = seconds;
        }

        public bool Running =>
            !(startTime + seconds
              < UnityEngine.Time.realtimeSinceStartup);

        private bool stopped;
        public bool Stopped => stopped;

        public void Stop()
        {
            stopped = true;
        }
        
        
    }
}
