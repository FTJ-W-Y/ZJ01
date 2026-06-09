using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework.WCF
{
    public class ACLOGOFF_Request
    {
        public bool AutoFlag { get; set; }

        public string Software { get; set; }
        public string EmployeeNo { get; set; }

        public List<EquipmentInfo> EquipmentInfo { get; set; }
    }
    public class EquipmentInfo
    {
        public string EquipmentCode { get; set; }

        public string NextEquipmentCode { get; set; }

        public string Container { get; set; }

        public string InContainer { get; set; }

        public bool ProcessResult { get; set; }

        public string OpFlag { get; set; }

        public string OperationMark { get; set; }

        public string ProcessMessage { get; set; }

        public bool ForceUnbindContainer { get; set; }

        public List<Outputs> Outputs { get; set; }
    }

    public class Outputs
    {
        public string SerialNo { get; set; }

        public string PreSerialNo { get; set; }

        public string SlotID { get; set; }

        public bool IsRealFlag { get; set; }

        public string ProductType { get; set; }

        public string Station { get; set; }

        public string ProcessFlag { get; set; }

        public bool PassFlag { get; set; }

        public List<MatchingInfo> MatchingInfo { get; set; }

        public List<StationInfo> StationInfo { get; set; }

        public List<ProcessSteps> ProcessSteps { get; set; }

        public List<SpartInfo> SpartInfo { get; set; }
        
        public List<MaterialInfo> MaterialInfo { get; set; }

        public List<Parameters> Parameters { get; set; }
    }
    public class MatchingInfo
    {
        public string Type { get; set; }

        public string SerialNo { get; set; }
    }
    public class StationInfo
    {
        public string StationID { get; set; }

        public string StepID { get; set; }

    }
    public class ProcessSteps
    {
        public string StepID { get; set; }

        public string StepStatus { get; set; }

    }
    public class SpartInfo
    {
        public string SpartID { get; set; }

        public string SpartLocation { get; set; }

        public string SpartLife { get; set; }
    }
    public class MaterialInfo
    {
        public string ProductNo { get; set; }

        public string ProductID { get; set; }

        public string SerialNo { get; set; }

        public string LabelNo { get; set; }

        public string ProductDesc { get; set; }

        public string LotNo { get; set; }

        public string Quantity { get; set; }

        public string UomCode { get; set; }

        public string MaterialPosition { get; set; }
    }
    public class Parameters
    {
        public string ParamterCode { get; set; }

        public string Location { get; set; }

        public string Value { get; set; }

        public string ParameterDescription { get; set; }

        public string UpperLimit { get; set; }

        public string LowerLimit { get; set; }

        public string TargetValue { get; set; }

        public string ParameterResult { get; set; }

        public string DefectCode { get; set; }

        public string ParameterMessage { get; set; }

        public string StepSequenceNo { get; set; }
    }
}
