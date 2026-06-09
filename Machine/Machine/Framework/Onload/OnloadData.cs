namespace Machine
{
    class OnloadData
    {
        #region // 字段

        // 【上料运行状态参数】
        public int runningState;                                 // 上料运行状态
        public uint heartBeat;                                          // PLC心跳
        public bool[,] operateSignal;                               //人工操作台信号
        public bool[,] onloadSignal;                            //上料平台信号
        public bool[,] bufSignal;                               //缓存架信号

        public bool bufSignalBtnStat;                               //人工操作台启动按钮信号
        public bool bufSignalBtnStop;                               //人工操作太停止按钮信号
        public bool bufSignalBtnAlarmStop;                               //人工操作台急停按钮信号

        public bool bufOnloadBtnAlarmStop;                               //上料急停按钮
        public bool bufManualOperateBtnAlarmStop;                               //人工干预急停按钮

        public Battery[] lineSignal;                               //来料工位信号
        public Battery[] pickSignal;                               //取料工位信号
        public Battery[] fingerSignal;                               //上料夹爪信号
        public Battery[] bufOnloadSignal;                               //上料缓存工位信号
        public Battery[] batNGSignal;                               //NG电池
        public Battery[] fakeSignal;                               //假电池工位信号
        public Battery[] reFakeSignal;                               //回炉电池工位信号
        public Battery[] reBattery;                                 //复投电池工位信号

        public Battery[] batteryScan;                               //来料物流框工位信号


        //【夹具信息】
        public PalletData[] palletDataArray;                           //夹具信息

        //【报警】
        public bool[] scanAlram;                                       //扫码报警
        public bool onloadRobotAlram;                                  //上料机器人防撞报警

        //public uint uploadBarcodeFlag;
        //public string ScanCodeOne;
        //public string ScanCodeTwo;
        //public string ScanCodeThree;
        //public string ScanCodeFour;

        //【上料允许取放盘信号】
        public bool[] pickFlag;                                     //上料允许取盘信号
        public bool[] placeFlag;                                     //上料允许放盘信号
        public ushort transFlag;                                    //调度请求取放盘信号
        public ushort transAvoidFlag;                                    //调度避让信号
        public bool avoidMove;                                      //上料机器人安全位标志
        public bool clearFlag;                                      //清尾料标志
        public bool endFlag;
        //夹具信息传输完成标志
        //【向PLC传输夹具信息】
        public PalletData palletData;


        public string PickPalletCode;       

        //读取扫码信息
        public bool batReadFlag;     //条码读取上传MES触发标志
        public bool UpPalletFlag;    //料框读取上传MES标志
        public string PalletPickCode;    //来料料框条码
        public string[] batCode;
        //写入扫码信息
        public bool batReadFlagReset;     
        public bool batWriteFlag;
        public uint[] batFlag;
        public bool palletWriteFlag;      //写料框条码标志复位
        // 【腔体数据锁】
        public object dataLock;
        //调度安全位信号
        public int tranSaft;
        //权限ID
        public int roleID;
        public string opName;
        //复投电池状态
        public uint[] reBatteryState;
        //【采集数据错误】
        public bool dataError;
        #endregion


        #region // 构造函数

        public OnloadData()
        {
            // 创建对象
            onloadSignal = new bool[3, 3];
            operateSignal = new bool[1, 3];
            bufSignal = new bool[4, 3];
            scanAlram = new bool[5];
            onloadRobotAlram = false;
            palletDataArray = new PalletData[3];
            palletDataArray[0] = new PalletData();
            palletDataArray[1] = new PalletData();
            palletDataArray[2] = new PalletData();
            palletData = new PalletData();
            pickFlag = new bool[3];
            placeFlag = new bool[3];
            batFlag = new uint[2];
            transFlag = 0;
            avoidMove = false;
            clearFlag = false;
            endFlag = false;
            batCode = new string[2];
            PalletPickCode = "";
            //public bool[] lineSignal;                               //来料工位信号
            //public bool[] pickSignal;                               //取料工位信号
            //public bool[] fingerSignal;                               //上料夹爪信号
            //public bool[] bufOnloadSignal;                               //上料缓存工位信号
            //public bool[] fakeSignal;                               //假电池工位信号
            //public bool[] reFakeSignal;                               //回炉电池工位信号
            bufSignalBtnStat = false;
            bufSignalBtnStop = false;
            bufSignalBtnAlarmStop = false;
            bufOnloadBtnAlarmStop = false;
            bufManualOperateBtnAlarmStop = false;
            roleID = 0;
            opName = "";
            lineSignal = new Battery[4];
            for (int i = 0; i < lineSignal.Length; i++)
            {
                lineSignal[i] = new Battery();
            }
            pickSignal = new Battery[4];
            for (int i = 0; i < pickSignal.Length; i++)
            {
                pickSignal[i] = new Battery();
            }
            fingerSignal = new Battery[4];
            for (int i = 0; i < fingerSignal.Length; i++)
            {
                fingerSignal[i] = new Battery();
            }
            bufOnloadSignal = new Battery[4];
            for (int i = 0; i < bufOnloadSignal.Length; i++)
            {
                bufOnloadSignal[i] = new Battery();
            }
            batNGSignal = new Battery[4];
            for (int i = 0; i < batNGSignal.Length; i++)
            {
                batNGSignal[i] = new Battery();
            }
            fakeSignal = new Battery[4];
            for (int i = 0; i < fakeSignal.Length; i++)
            {
                fakeSignal[i] = new Battery();
            }
            reBattery = new Battery[1];
            for (int i = 0; i < reBattery.Length; i++)
            {
                reBattery[i] = new Battery();
            }

            batteryScan = new Battery[24];
            for (int i = 0; i < batteryScan.Length; i++)
            {
                batteryScan[i] = new Battery();
            }
            reBatteryState = new uint[1];
           
            tranSaft = 1;
            dataLock = new object();
            //Release();
        }

        #endregion


        #region // 方法

        public bool CopyFrom(OnloadData loadingData)
        {
            if (null != loadingData)
            {
                if (this == loadingData)
                {
                    return true;
                }

                lock (this.dataLock)
                {
                    lock (loadingData.dataLock)
                    {
                        bufSignalBtnStat = loadingData.bufSignalBtnStat;
                        bufSignalBtnStop = loadingData.bufSignalBtnStop;
                        runningState = loadingData.runningState;
                        heartBeat = loadingData.heartBeat;
                        for (int i = 0; i < loadingData.onloadSignal.GetLength(0); i++)
                            for (int j = 0; j < loadingData.onloadSignal.GetLength(1); j++)
                                onloadSignal[i, j] = loadingData.onloadSignal[i, j];

                        for (int i = 0; i < loadingData.operateSignal.GetLength(0); i++)
                            for (int j = 0; j < loadingData.operateSignal.GetLength(1); j++)
                                operateSignal[i, j] = loadingData.operateSignal[i, j];

                        for (int i = 0; i < loadingData.bufSignal.GetLength(0); i++)
                            for (int j = 0; j < loadingData.bufSignal.GetLength(1); j++)
                                bufSignal[i, j] = loadingData.bufSignal[i, j];

                        for (int i = 0; i < scanAlram.Length; i++)
                        {
                            scanAlram[i] = loadingData.scanAlram[i];
                        }

                        for (int i = 0; i < palletDataArray.Length; i++)
                        {
                            palletDataArray[i].state = loadingData.palletDataArray[i].state;
                            palletDataArray[i].code = loadingData.palletDataArray[i].code;
                            for (int rowsIdx = 0; rowsIdx < palletDataArray[i].battery.GetLength(0); rowsIdx++)
                            {
                                for (int colsIdx = 0; colsIdx < palletDataArray[i].battery.GetLength(1); colsIdx++)
                                {
                                    if (loadingData.palletDataArray[i].battery[rowsIdx, colsIdx] != null)
                                    {
                                        palletDataArray[i].battery[rowsIdx, colsIdx].Type = loadingData.palletDataArray[i].battery[rowsIdx, colsIdx].Type;
                                        palletDataArray[i].battery[rowsIdx, colsIdx].NGType = loadingData.palletDataArray[i].battery[rowsIdx, colsIdx].NGType;
                                        palletDataArray[i].battery[rowsIdx, colsIdx].Code = loadingData.palletDataArray[i].battery[rowsIdx, colsIdx].Code;
                                    }
                                }
                            }
                            palletDataArray[i].count = loadingData.palletDataArray[i].count;
                            palletDataArray[i].haveFake = loadingData.palletDataArray[i].haveFake;
                            palletDataArray[i].enable = loadingData.palletDataArray[i].enable;
                            palletDataArray[i].clearFlag = loadingData.palletDataArray[i].clearFlag;
                        }
                        batReadFlag = loadingData.batReadFlag;
                        UpPalletFlag = loadingData.UpPalletFlag;
                        
                        for (int i = 0; i < batCode.Length; i++)
                        {
                            batCode[i] = loadingData.batCode[i];
                        }
                        PalletPickCode = loadingData.PalletPickCode;
                        for (int i = 0; i < pickFlag.Length; i++)
                        {
                            pickFlag[i] = loadingData.pickFlag[i];
                        }
                        for (int i = 0; i < placeFlag.Length; i++)
                        {
                            placeFlag[i] = loadingData.placeFlag[i];
                        }
                        onloadRobotAlram = loadingData.onloadRobotAlram;
                        avoidMove = loadingData.avoidMove;
                        clearFlag = loadingData.clearFlag;                                      //清尾料标志
                        endFlag = loadingData.endFlag;
                        //uploadBarcodeFlag = loadingData.uploadBarcodeFlag;
                        //                ScanCodeOne = loadingData.ScanCodeOne;
                        //                ScanCodeTwo = loadingData.ScanCodeTwo;
                        //                ScanCodeThree = loadingData.ScanCodeThree;
                        //                ScanCodeFour = loadingData.ScanCodeFour;
                        //onloadToTransBaty = loadingData.onloadToTransBaty;
                        ////来料线工位
                        for (int idx = 0; idx < loadingData.lineSignal.Length; idx++)
                            {
                                lineSignal[idx].Code = loadingData.lineSignal[idx].Code;
                                lineSignal[idx].Type = loadingData.lineSignal[idx].Type;
                            }
                        ////取料工位
                        for (int idx = 0; idx < loadingData.pickSignal.Length; idx++)
                            {
                                pickSignal[idx].Code = loadingData.pickSignal[idx].Code;
                                pickSignal[idx].Type = loadingData.pickSignal[idx].Type;
                            }
                        ////夹爪
                        for (int idx = 0; idx < loadingData.fingerSignal.Length; idx++)
                            {
                                fingerSignal[idx].Code = loadingData.fingerSignal[idx].Code;
                                fingerSignal[idx].Type = loadingData.fingerSignal[idx].Type;
                            }
                        ////缓存
                        for (int idx = 0; idx < loadingData.bufOnloadSignal.Length; idx++)
                            {
                                bufOnloadSignal[idx].Code = loadingData.bufOnloadSignal[idx].Code;
                                bufOnloadSignal[idx].Type = loadingData.bufOnloadSignal[idx].Type;
                            }
                        ////来料物流框
                        for (int idx = 0; idx < loadingData.batteryScan.Length; idx++)
                        {
                            batteryScan[idx].Code = loadingData.batteryScan[idx].Code;
                            batteryScan[idx].Type = loadingData.batteryScan[idx].Type;
                        }
                        ////NG
                        for (int idx = 0; idx < loadingData.batNGSignal.Length; idx++)
                        {
                            batNGSignal[idx].Code = loadingData.batNGSignal[idx].Code;
                            batNGSignal[idx].Type = loadingData.batNGSignal[idx].Type;
                        }
                        ////假电池
                        for (int idx = 0; idx < loadingData.fakeSignal.Length; idx++)
                            {
                                fakeSignal[idx].Code = loadingData.fakeSignal[idx].Code;
                                fakeSignal[idx].Type = loadingData.fakeSignal[idx].Type;
                            }

                        runningState = loadingData.runningState;
                        heartBeat = loadingData.heartBeat;
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
            operateSignal = new bool[1, 3];
            onloadSignal = new bool[3, 3];
            bufSignal = new bool[4, 3];
            scanAlram = new bool[5];
            palletDataArray = new PalletData[3];
            palletDataArray[0] = new PalletData();
            palletDataArray[1] = new PalletData();
            palletDataArray[2] = new PalletData();
            pickFlag = new bool[3];
            placeFlag = new bool[3];
            onloadRobotAlram = false;
            avoidMove = false;
            clearFlag = false;
            endFlag = false;
        }
    }

    #endregion
}
