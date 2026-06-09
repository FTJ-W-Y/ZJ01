namespace Machine
{
    class OffloadData
    {
        #region // 字段

        // 【上料运行状态参数】
        public int runningState;                                 // 下料运行状态
        public uint heartBeat;                                    // PLC心跳
        //public bool pickSignal;                                    // 允许取夹具信号

        //public bool placeSignal;                                    // 允许放夹具信号

        public bool[,] offloadSignal;                            //下平台信号
        public bool[,] bufSignal;                               //缓存架信号

        //【夹具信息】
        public PalletData[] palletDataArray;                           //夹具信息

        //【扫码】
        public uint uploadBarcodeFlag;
        public string ScanCodeOne;
        public string ScanCodeTwo;
        public string ScanCodeThree;
        public string ScanCodeFour;

        //【报警】
        public bool fingerAlarm;
        public bool fingerCommAlarm;
        public bool[] platAlarm;

        //【下料允许取放盘信号】
        public bool[] pickFlag;                                     //下料允许取盘信号
        public bool[] placeFlag;                                     //下料允许放盘信号
        public ushort transFlag;                                    //调度请求取放盘信号
        public uint transSaftFlag;                                    //调度安全位信号
        public bool avoidMove;                                      //下料三轴安全位标志
        public bool clearFlag;                                      //清尾料标志
        public bool endFlag;
        //夹具信息传输完成标志
        //【向PLC传输夹具信息】
        public PalletData palletData;
        //读取扫码信息
        public bool batReadFlag;
        public string[] batCode;
        //写入扫码信息
        public bool batWriteFlag;
        public uint[] batFlag;
        // 【数据锁】
        public object dataLock;
        //权限ID
        public int roleID;
        public string opName;
        //【采集数据错误】
        public bool dataError;
        #endregion


        #region // 构造函数

        public OffloadData()
        {
            // 创建对象
            offloadSignal = new bool[2, 3];
            bufSignal = new bool[5, 3];
            palletDataArray = new PalletData[2];
            palletDataArray[0] = new PalletData();
            palletDataArray[1] = new PalletData();
            pickFlag = new bool[2];
            placeFlag = new bool[2];
            transFlag = 0;
            fingerAlarm = false;
            fingerCommAlarm = false;
            platAlarm = new bool[2];
            avoidMove = false;
            clearFlag = false;
            endFlag = false;

            palletData = new PalletData();
            roleID = 0;
            opName = "";
            dataLock = new object();
            //Release();
        }

        #endregion


        #region // 方法

        public bool CopyFrom(OffloadData offloadData)
        {
            if (null != offloadData)
            {
                if (this == offloadData)
                {
                    return true;
                }

                lock (this.dataLock)
                {
                    lock (offloadData.dataLock)
                    {
                        runningState = offloadData.runningState;
                        heartBeat = offloadData.heartBeat;

                        for (int i = 0; i < offloadData.offloadSignal.GetLength(0); i++)
                            for (int j = 0; j < offloadData.offloadSignal.GetLength(1); j++)
                                offloadSignal[i, j] = offloadData.offloadSignal[i, j];

                        palletDataArray = offloadData.palletDataArray;
                        for (int i = 0; i < palletDataArray.Length; i++)
                        {
                            palletDataArray[i].state = offloadData.palletDataArray[i].state;
                            palletDataArray[i].code = offloadData.palletDataArray[i].code;
                            //palletDataArray[i].batteryState = offloadData.palletDataArray[i].batteryState;
                            for (int rowsIdx = 0; rowsIdx < palletDataArray[i].battery.GetLength(0); rowsIdx++)
                            {
                                for (int colsIdx = 0; colsIdx < palletDataArray[i].battery.GetLength(1); colsIdx++)
                                {
                                    palletDataArray[i].battery[rowsIdx, colsIdx].Type = offloadData.palletDataArray[i].battery[rowsIdx, colsIdx].Type;
                                    palletDataArray[i].battery[rowsIdx, colsIdx].NGType = offloadData.palletDataArray[i].battery[rowsIdx, colsIdx].NGType;
                                    palletDataArray[i].battery[rowsIdx, colsIdx].Code = offloadData.palletDataArray[i].battery[rowsIdx, colsIdx].Code;
                                }
                            }
                            palletDataArray[i].count = offloadData.palletDataArray[i].count;
                            palletDataArray[i].haveFake = offloadData.palletDataArray[i].haveFake;
                            palletDataArray[i].enable = offloadData.palletDataArray[i].enable;

                        }
                        for (int i = 0; i < pickFlag.Length; i++)
                        {
                            pickFlag[i] = offloadData.pickFlag[i];
                        }
                        for (int i = 0; i < placeFlag.Length; i++)
                        {
                            placeFlag[i] = offloadData.placeFlag[i];
                        }
                        fingerAlarm = offloadData.fingerAlarm;
                        fingerCommAlarm = offloadData.fingerCommAlarm;
                        for (int i = 0; i < platAlarm.Length; i++)
                        {
                            platAlarm[i] = offloadData.platAlarm[i];
                        }
                        avoidMove = offloadData.avoidMove;
                        uploadBarcodeFlag = offloadData.uploadBarcodeFlag;
                        runningState = offloadData.runningState;
                        heartBeat = offloadData.heartBeat;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清除数据
        /// </summary>
        public void Release()
        {
            // 【设置参数】
            runningState = 0;
            heartBeat = 0;
            offloadSignal = new bool[3, 3];
            bufSignal = new bool[5, 3];
            palletDataArray = new PalletData[2];
            pickFlag = new bool[3];
            placeFlag = new bool[3];
            fingerAlarm = false;
            fingerCommAlarm = false;
            platAlarm = new bool[2];
            avoidMove = false;
            clearFlag = false;
            endFlag = false;
        }
    }

    #endregion
}
