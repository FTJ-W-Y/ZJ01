using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework.WCF
{
    public class ACLOGOFF_Response
    {
        public List<EquipmentInfo1> EquipmentInfo { get; set; }
    }

    public class EquipmentInfo1
    {
        public bool EquipmentCode { get; set; }

        public string Container { get; set; }

        public string Result { get; set; }

        public string Message { get; set; }

        public List<Products> Products { get; set; }
    }
    public class Products
    {
        public bool SerialNo { get; set; }

        public string SlotID { get; set; }

        public string CounterfeitFlag { get; set; }

        public string ProductType { get; set; }
        public string OutputFlag { get; set; }

        public string OutputMessage { get; set; }

        public List<ParameterInfo> ParameterInfo { get; set; }
    }
    public class ParameterInfo
    {
        public bool ParamterCode { get; set; }

        public string Value { get; set; }

        public string UpperLimit { get; set; }

        public string LowerLomit { get; set; }

        public string TargetValue { get; set; }

        public string DefectCode { get; set; }

        public string KValue { get; set; }

        public string ParameterResult { get; set; }

        public string ParameterMessage { get; set; }
    }
}

