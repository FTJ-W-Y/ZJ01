using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    public class ACEQPTPARM_Response
    {
        private EquipmentInfo1 equipmentInfo;
    }
    public class EquipmentInfo
    {
        public string EquipmentCode { get; set; }
        public bool Result { get; set; }
        public string Message { get; set; }
        public string WipOrder { get; set; }
        public string WipOrderType { get; set; }
        public string Customer { get; set; }
        public string ProductNo { get; set; }
        public string ProductDesc { get; set; }
        public string ProcessID { get; set; }
        public string Version { get; set; }
        public string OprSequenceNo { get; set; }
        public string OperationCode { get; set; }
        public string OprSequenceDesc { get; set; }
        public string FirstArticleNum { get; set; }
        public string DebugNum { get; set; }
        public string RecipeID { get; set; }
        public string ModuleType { get; set; }
        public List<EquipmentStatusList> listEquipmentStatusList { get; set; }
    }
    public class EquipmentStatusList
    {
        private string EquipmentStatusID { get; set; }
        private string ReasonCode { get; set; }
        private string Description { get; set; }
        private List<StepInfo> listStepInfo { get; set; }
    }
    class StepInfo
    {
        private string StepID { get; set; }
        private string StepType { get; set; }
        private List<ParamenterInfo> listParamenterInfo { get; set; }
    }
    class ParamenterInfo
    {
        private string ParameterCode { get; set; }
        private string ParameterType { get; set; }
        private string TargetValue { get; set; }
        private string UOMCode { get; set; }
        private string UperContorlLimit { get; set; }
        private string LowerControlLimit { get; set; }
        private string Description { get; set; }
        private bool UploadFlag { get; set; }
        private bool Active { get; set; }
    }
}
