using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    /// <summary>
    /// 设备联机请求
    /// </summary>
    public class ACEQPTCONN_Request
    {
        public bool AutoFlag { get; set; }

        public string Software { get; set; }

        //public EquipmentInfo equipmentInfo;
        public List<EquipmentInfo1> EquipmentInfo { get; set; }

    }
    public class EquipmentInfo1
    {
        public string EquipmentCode { get; set; }
        
    }
}
