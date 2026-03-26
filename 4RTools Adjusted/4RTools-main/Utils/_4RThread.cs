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


        public _4RThread(Func<int, int> toRun)
        {
            this.thread = new Thread(() =>
            {
                while (true)
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
            if (_4RThread != null && _4RThread.thread.IsAlive)
            {
                try {
#pragma warning disable CS0618 // Thread.Suspend is obsolete but no safe alternative without rearchitecting the threading model
                    _4RThread.thread.Suspend();
#pragma warning restore CS0618
                }
                catch (Exception ex) {
                    Console.WriteLine("[4R Thread Exception] =========== We could not suspend curren thread: " + ex);
                }
            }
        }
    }
}
