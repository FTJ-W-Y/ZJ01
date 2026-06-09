namespace Machine
{
   class PalletData
    {
        //电池条码
        public Battery[,] battery;
        //电池状态
        //public BatteryState batteryState;
        //夹具条码
        public string code;
        //夹具状态
        public PalletStatus state;
        //电池数量
        public uint count;
        //是否需要假电池
        public bool haveFake;

        //是否夹具使能
        public bool enable;

        //清尾料
        public bool clearFlag;
        
        ////假电池位置
        //public uint fakeBatteryPosition;
        ////假电池操作
        //public uint fakeBatteryOperation;

        public PalletData()
        {
            battery = new Battery[19, 4];
            for (int rows = 0; rows < battery.GetLength(0); rows++)
            {
                for (int cols = 0; cols < battery.GetLength(1); cols++)
                {
                    battery[rows, cols] = new Battery();
                }
            }
            code = "";
            state = PalletStatus.Invalid;
            //batteryState=
            count = 0;
            haveFake = false;
            enable = false;
            clearFlag = false;
        }
    }
}
