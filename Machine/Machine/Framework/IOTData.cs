using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    public class IOTData
    {
        public int line { get; set; }
        /// <summary>
        /// 设备地址
        /// </summary>
        public string equip { get; set; }
        /// <summary>
        /// 炉层
        /// </summary>
        public string floor { get; set; }
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime datetime { get; set; }
        /// <summary>
        /// 点位数据
        /// </summary>
        public List<PointData> points { get; set; }
    }

    public class PointData
    {
        public string code { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string unit { get; set; }
        public string value { get; set; }
    }
}
