using System.Threading;

namespace Fleeted.utils
{
    public class TimedAction
    {
        private Timer aTimer;
        AutoResetEvent autoEvent = new AutoResetEvent(false);
        private bool done;
        public bool HasEverStated;
        private int lifetime;

        public TimedAction(float lifetime)
        {
            this.lifetime = (int) (lifetime * 1000);
        }

        public void Start()
        {
            HasEverStated = true;
            done = false;
            aTimer = new Timer(OnTimedEvent, autoEvent, lifetime, 0);
        }

        public void TurnOff()
        {
            done = false;
        }

        private void OnTimedEvent(object state)
        {
            done = true;
            aTimer.Dispose();
        }

        public bool TrueDone()
        {
            return done;
        }
    }
}