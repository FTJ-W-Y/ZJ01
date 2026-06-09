using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Machine
{
    class ThreadManager
    {
        public static Thread manageThread;

        static List<Thread> threadPool = new List<Thread>();

        public static void Init(ThreadStart thThread, int nMaxThreadNum = 10, int nMinThreadNum = 1, int nPortThreadNum = 1)
        {
            ThreadPool.GetMaxThreads(out nMaxThreadNum, out nPortThreadNum);
            ThreadPool.GetMinThreads(out nMinThreadNum, out nPortThreadNum);
            manageThread = new Thread(thThread);

            threadPool.Add(manageThread);
        }

        public static void Start()
        {
            manageThread.Start();
        }

        public static void Terminal()
        {
            try
            {
                foreach (var thread in threadPool)
                    thread.Abort();
            }
            catch
            {
            }
        }

        public static void AddWork(object obj, WaitCallback callBack)
        {
            ThreadPool.QueueUserWorkItem(callBack, obj);
        }
    }
}
