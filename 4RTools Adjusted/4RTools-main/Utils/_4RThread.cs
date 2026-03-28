using System;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4RTools.Utils
{
    public class _4RThread
    {
        private Thread thread;
        private volatile bool _running;

        public bool IsRunning => _running;

        public _4RThread(Func<int, int> toRun)
        {
            _running = true;
            this.thread = new Thread(() =>
            {
                while (_running)
                {
                    try
                    {
                        toRun(0);
                    }catch(Exception ex) {
                        Console.WriteLine("[4RThread Exception] Error while Executing Thread Method ==== "+ex.Message);
                    }
                    finally
                    {
                        Thread.Sleep(5);
                    }
                }
            });
            this.thread.SetApartmentState(ApartmentState.STA);
        }

        public static void Start(_4RThread _4RThread)
        {
            _4RThread.thread.Start();
        }

        public static void Stop(_4RThread _4RThread)
        {
            if (_4RThread != null)
            {
                _4RThread._running = false;
                try
                {
                    if (_4RThread.thread.IsAlive)
                    {
                        _4RThread.thread.Join(2000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[4R Thread Exception] =========== We could not stop current thread: " + ex);
                }
            }
        }
    }
}
