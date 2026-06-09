using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework.WCF
{
    /// <summary>
    /// 入站校验请求
    /// </summary>
    class ACLOGONCHECK_Request
    {
        private bool AutoFlag { get; set; }

        private string EmployeeNo { get; set; }

        private string Software { get; set; }

        private EquipmentInfo1 equipmentInfo;
    }
}
