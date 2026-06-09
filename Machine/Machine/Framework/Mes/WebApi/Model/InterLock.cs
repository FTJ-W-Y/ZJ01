using System;

namespace Machine
{
    public class InterLock
    {
        private string _InterLockCode;          // 互锁信号代码
        private string _InterLockMessage;       // 互锁信号描述
        private string _EquipmentCode;          // 设备编码

        



        public string InterLockCode
        {
            get { return _InterLockCode; }
            set { _InterLockCode = value; }
        }

        public string InterLockMessage
        {
            get { return _InterLockMessage; }
            set { _InterLockMessage = value; }
        }

        public string EquipmentCode
        {
            get { return _EquipmentCode; }
            set { _EquipmentCode = value; }
        }

        public override string ToString()
        {
            return "互锁信号代码:" + this.InterLockCode + "," + "互锁信号描述:" + this.InterLockMessage + "," + "设备编码:" + this.EquipmentCode;
        }
    }
}
