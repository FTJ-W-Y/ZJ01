using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Machine
{
    class IOTTaskList
    {
        private static List<IOTData> _inList = new List<IOTData>();
        private static ManualResetEvent _event = new ManualResetEvent(false);

        public IOTTaskList()
        {
            //_event.Set();
        }

        public static void Add(IOTData model)
        {
            lock (_inList)
            {
                _inList.Add(model);
                _event.Set();
            }
        }

        public static void Clean()
        {
            lock (_inList)
            {
                _inList.Clear();
                _event.Reset();
            }
        }
        public static IOTData GetFirstModel()
        {
            IOTData model = null;
            lock (_inList)
            {
                if (_inList.Count > 0)
                {
                    model = _inList[0];
                    _inList.RemoveAt(0);
                }

                if (_inList.Count <= 0)
                {
                    _event.Reset();
                }
            }
            return model;
        }

        public static bool IsReady()
        {
            return _event.WaitOne();
        }
    }
}
