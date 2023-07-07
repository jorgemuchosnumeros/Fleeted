using System.Threading;

namespace Fleeted.utils
{
    public class TimedAction
    {
        private readonly AutoResetEvent _autoEvent = new(false);
        private readonly int _lifetime;
        private Timer _aTimer;
        private bool _done;
        private bool _running;
        public bool HasEverStated;

        public TimedAction(float lifetime)
        {
            _lifetime = (int) (lifetime * 1000);
        }

        public void Start()
        {
            HasEverStated = true;
            _running = true;
            _done = false;
            _aTimer = new Timer(OnTimedEvent, _autoEvent, _lifetime, 0);
        }


        public void TurnOff()
        {
            _done = false;
            _running = false;
        }

        private void OnTimedEvent(object state)
        {
            _done = true;
            _running = false;
            _aTimer.Dispose();
        }

        public bool IsRunning()
        {
            return _running;
        }

        public bool TrueDone()
        {
            return _done;
        }
    }
}