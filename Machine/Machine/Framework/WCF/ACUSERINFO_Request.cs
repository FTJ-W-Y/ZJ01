using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework.WCF
{
    class ACUSERINFO_Request
    {
        private string Software { get; set; }

        private bool AutoFlag { get; set; }

        private string EmpployeeNo { get; set; }

        private EquipmentInfo1 equipmentInfo;
    }
}
