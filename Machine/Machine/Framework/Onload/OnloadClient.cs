using System;

namespace Machine
{
    class OnloadClient : BaseThread
    {
        #region // 字段

        private FinsTCP finsTcp;            // FinsTCP客户端
        private FinsUDP finsUdp;            // FinsUDP客户端
        private byte[] sendBuffer;          // 发送缓存
        private byte[] recvBuffer;          // 接收缓存
        private DryingOvenData ovenData;    // 接收数据解析得到的干燥炉数据
        private int errCount;               // 接收数据错误次数计数
        private FinsType finsType;          // Fins通讯类型
        private static DryOvenFinsCmd[] finsCmdAddr;    // 命令地址表
        private OnloadData onloadData;     // 上料数据
        private OnloadData loadingBuf;       // 上料数据缓存
        private int batIdx;                //电池条码索引
        private int rowsIdx;                //电池条码行
        private int colsIdx;                //电池条码列
        private int pltCodeLen;             //夹具条码长度
        private int batCodeLen;             //电池条码长度
        private object updateLock;          // 数据更新锁
        private object writeLock;          // 数据写入锁
        #endregion

        #region // 命令地址表

        public static OnloadCmdAddr[] FinsCmdAddr = new OnloadCmdAddr[(int)LoadingCmd.End]
        {
            new OnloadCmdAddr(ZoneCode.DMWord, 1000,10,0,0, 10),              // 运行状态与IO信号（读）
                          
            new OnloadCmdAddr(ZoneCode.DMWord, 1010,1000,0,0,  1000),          // 夹具1电池条码前50个（读）    
            new OnloadCmdAddr(ZoneCode.DMWord, 2010,1000, 0,0, 1000),         // 夹具1电池条码中50个（读）       
            new OnloadCmdAddr(ZoneCode.DMWord, 3010,1000,0,0,  1000),         // 夹具1电池条码后50个（读）
            new OnloadCmdAddr(ZoneCode.DMWord, 4010,175, 0,0, 175),            // 夹具1电池信息（读） 


            new OnloadCmdAddr(ZoneCode.DMWord, 4233,1000, 0,0, 1000),          // 夹具2电池条码前50个（读）    
            new OnloadCmdAddr(ZoneCode.DMWord, 5233,1000,0,0,  1000),         // 夹具2电池条码中50个（读）       
            new OnloadCmdAddr(ZoneCode.DMWord, 6233,1000, 0,0, 1000),         // 夹具2电池条码后50个（读）
            new OnloadCmdAddr(ZoneCode.DMWord, 7233,175, 0,0, 175),            // 夹具2电池信息（读） 


            new OnloadCmdAddr(ZoneCode.DMWord, 7456,1000, 0,0, 1000),          // 夹具3电池条码前50个（读）    
            new OnloadCmdAddr(ZoneCode.DMWord, 8456,1000,0,0,  1000),         // 夹具3电池条码中50个（读）       
            new OnloadCmdAddr(ZoneCode.DMWord, 9456,1000, 0,0, 1000),         // 夹具3电池条码后50个（读）
            new OnloadCmdAddr(ZoneCode.DMWord, 10456,175, 0,0, 175),            // 夹具3电池信息（读） 


            new OnloadCmdAddr(ZoneCode.DMWord, 10679,81,0,0,  81),         // 电池扫码（读）

            new OnloadCmdAddr(ZoneCode.DMWord, 10786,1,0,0,  1),         // 上料允许取放夹具（读）

            new OnloadCmdAddr(ZoneCode.DMWord, 25000,21,0,0,  21),         // 来料料框电芯条码读取（读）

            new OnloadCmdAddr(ZoneCode.DMWord, 13993,1000,0,0,  1000),         // 上料区小模组信息（读）

            new OnloadCmdAddr(ZoneCode.DMWord, 20000,1,0,0,1),         // 心跳（读/写）
            new OnloadCmdAddr(ZoneCode.DMWord, 20001,1,0,0,1),         // 机台状态（读）

            new OnloadCmdAddr(ZoneCode.DMWord, 26128,1,0,0,1),         // 扫码枪报警（读）
            new OnloadCmdAddr(ZoneCode.DMWord, 26134,1,0,0,1),         // 上料机器人防撞报警（读）

            new OnloadCmdAddr(ZoneCode.DMWord, 20002,1,0,0,1),         // 权限下发（写）

            new OnloadCmdAddr(ZoneCode.DMWord, 20003,10,0,0,10),         // 操作员名字下发（写）

            new OnloadCmdAddr(ZoneCode.DMWord, 10788,1,0,0,  1),         // 调度请求取放夹具（写）

            new OnloadCmdAddr(ZoneCode.DMWord, 10788,1,0,0,  1),         // 调度避让（写）

            new OnloadCmdAddr(ZoneCode.DMWord, 10679,1,0,0,  1),         // 电池扫码结束复位（写）

            new OnloadCmdAddr(ZoneCode.DMWord, 25000,1,0,0,  1),         // 来料料框标志复位（写）

            new OnloadCmdAddr(ZoneCode.DMWord, 10780,5,0,0,  5),         // 电池扫码（写）
      
            new OnloadCmdAddr(ZoneCode.DMWord, 10792,1000,0,0,  1000),         // 夹具信息写入前50个(写)
            new OnloadCmdAddr(ZoneCode.DMWord, 11792,1000,0,0,  1000),         // 夹具信息写入前50个(写)
            new OnloadCmdAddr(ZoneCode.DMWord, 12792,1000,0,0,  1000),         // 夹具信息写入前50个(写)
            new OnloadCmdAddr(ZoneCode.DMWord, 13792,173,0,0,  173),         // 夹具信息写入(写)

            new OnloadCmdAddr(ZoneCode.DMWord, 13965,21,0,0,  21),         // 复投电池信息(写)


            new OnloadCmdAddr(ZoneCode.DMWord, 10789,1,0,0,1),         // 调度机器人安全位信号（写）

            new OnloadCmdAddr(ZoneCode.DMWord, 10790,1,0,0,1),                 // 信息传输完成信号（写）

        };

        #endregion

        #region // 构造函数

        public OnloadClient()
        {
            this.finsTcp = new FinsTCP();
            this.finsUdp = new FinsUDP();
            this.onloadData = new OnloadData();
            this.loadingBuf = new OnloadData();
            this.ovenData = new DryingOvenData();
            this.sendBuffer = new byte[2000];
            this.recvBuffer = new byte[2000];
            this.errCount = 0;
            this.finsType = FinsType.Unknown;
            this.batIdx = 0;
            this.rowsIdx = 0;
            this.colsIdx = 0;
            this.batCodeLen = 20;
            this.pltCodeLen = 20;
            updateLock = new object();
            writeLock = new object();
        }

        #endregion

        #region // 编码解码

        /// <summary>
        /// 编解码模式
        /// </summary>
        enum CodecMode
        {
            // 16位字节顺序
            bit16_12 = 0,
            bit16_21,

            // 32位字节顺序
            bit32_1234,
            bit32_2143,
            bit32_3412,
            bit32_4321,
        };
        /// <summary>
        /// 字节序调整
        /// </summary>
        private void ByteCodec(byte[] data, int startIdx, int count, CodecMode codec)
        {
            if (null == data || count <= 0)
            {
                return;
            }
            switch (codec)
            {
                case CodecMode.bit16_12:
                    break;
                case CodecMode.bit16_21:
                    {
                        byte buf;
                        for (int idx = 0; idx < count; idx += 2)
                        {
                            buf = data[startIdx + idx];
                            data[startIdx + idx] = data[startIdx + idx + 1];
                            data[startIdx + idx + 1] = buf;
                        }
                        break;
                    }
                case CodecMode.bit32_1234:
                    break;
                case CodecMode.bit32_2143:
                    {
                        byte[] buf = new byte[4];
                        for (int idx = 0; idx < count; idx += 4)
                        {
                            Array.Copy(data, (startIdx + idx), buf, 0, 4);
                            data[startIdx + idx] = buf[1];
                            data[startIdx + idx + 1] = buf[0];
                            data[startIdx + idx + 2] = buf[3];
                            data[startIdx + idx + 3] = buf[2];
                        }
                        break;
                    }
                case CodecMode.bit32_3412:
                    {
                        byte[] buf = new byte[4];
                        for (int idx = 0; idx < count; idx += 4)
                        {
                            Array.Copy(data, (startIdx + idx), buf, 0, 4);
                            data[startIdx + idx] = buf[2];
                            data[startIdx + idx + 1] = buf[3];
                            data[startIdx + idx + 2] = buf[0];
                            data[startIdx + idx + 3] = buf[1];
                        }
                        break;
                    }
                case CodecMode.bit32_4321:
                    {
                        byte[] buf = new byte[4];
                        for (int idx = 0; idx < count; idx++)
                        {
                            Array.Copy(data, (startIdx + idx), buf, 0, 4);
                            data[startIdx + idx] = buf[3];
                            data[startIdx + idx + 1] = buf[2];
                            data[startIdx + idx + 2] = buf[1];
                            data[startIdx + idx + 3] = buf[0];
                        }
                        break;
                    }
            }
        }

        #endregion

        #region // 内部接口

        protected override void RunWhile()
        {
            if (IsConnect())
            {
                try
                {
                    //DryingOvenData data = new DryingOvenData();
                    // 所有读取数据
                    for (int i = (int)LoadingCmd.RunState; i < (int)LoadingCmd.WriteTrans; i++)
                    {
                        recvBuffer.Initialize();
                        switch (this.finsType)
                        {
                            case FinsType.Tcp:
                                if (this.finsTcp.ReadWords(FinsCmdAddr[i].zone, FinsCmdAddr[i].wordAddr, FinsCmdAddr[i].bitAddr, FinsCmdAddr[i].count, ref recvBuffer))
                                {
                                    BufToData((LoadingCmd)i, FinsCmdAddr[i].count, FinsCmdAddr[i].wordInterval, loadingBuf, recvBuffer);
                                }
                                else
                                {
                                    loadingBuf.dataError = true;
                                    if (this.errCount < (2 * (int)DryOvenCmd.SetParameter))
                                    {
                                        loadingBuf.Release();
                                        this.errCount++;
                                        WriteLog($"{this.finsTcp.GetIPInfo()}.DryingOvenClient.RunWhile() read {((DryOvenCmd)i).ToString()} fail.", HelperLibrary.LogType.Error);
                                    }
                                }
                                break;
                            case FinsType.Udp:

                                Array.Clear(recvBuffer, 0, recvBuffer.Length);
                                if (this.finsUdp.ReadWords(FinsCmdAddr[i].zone, FinsCmdAddr[i].wordAddr, FinsCmdAddr[i].bitAddr, FinsCmdAddr[i].count, ref recvBuffer))
                                {
                                    BufToData((LoadingCmd)i, FinsCmdAddr[i].count, FinsCmdAddr[i].wordInterval, loadingBuf, recvBuffer);
                                    this.errCount = 0; //wjj 220503
                                }
                                else
                                {
                                    loadingBuf.dataError = true;
                                    if (this.errCount < (2 * (int)DryOvenCmd.SetParameter))
                                    {
                                        loadingBuf.Release();
                                        this.errCount++;
                                        WriteLog($"{this.finsUdp.GetIPInfo()}.OnloadClient.RunWhile() read {((DryOvenCmd)i).ToString()} fail.", HelperLibrary.LogType.Error);
                                    }
                                }
                                break;
                        }
                        lock (updateLock)
                        {
                            onloadData.CopyFrom(loadingBuf);
                            //loadingBuf.Release();
                        }
                    }
                    //this.ovenData.Copy(data);
                }
                catch (System.Exception ex)
                {
                    WriteLog("RunWhile() Exception: " + ex.Message, HelperLibrary.LogType.Error);
                }
            }
            Sleep(50);
        }

        private void BufToData(LoadingCmd cmdID, int count, int dataInterval, OnloadData onloadData, byte[] buf)
        {
            try
            {
                ByteCodec(buf, 0, (count * 2), CodecMode.bit16_21);
                switch (cmdID)
                {
                    // 上料运行状态
                    case LoadingCmd.RunState:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            UInt16 unValue = 0;
                            ////上料运行状态
                            //data.runningState = BitConverter.ToUInt16(buf, nByteIdx += 0);
                            ////PLC心跳
                            //data.heartBeat = BitConverter.ToUInt16(buf, nByteIdx += 2);
                            unValue = BitConverter.ToUInt16(buf, nByteIdx += 0);
                            data.bufOnloadBtnAlarmStop = (unValue & 0x01) == 0x01;
                            data.bufManualOperateBtnAlarmStop = (unValue & 0x02) == 0x02;

                            unValue = BitConverter.ToUInt16(buf, nByteIdx += 2);
                            //f/*o*/r (int j = 0; j < data.onloadSignal.GetLength(1); j++)
                            //{
                            data.bufSignalBtnStat = (unValue & 0x01) == 0x01;
                            data.bufSignalBtnStop = (unValue & 0x02) == 0x02;
                            data.bufSignalBtnAlarmStop = (unValue & 0x04) == 0x04;
                            //}
                            //上料平台信号

                            for (int i = 0; i < data.onloadSignal.GetLength(0); i++)
                            {
                                unValue = BitConverter.ToUInt16(buf, nByteIdx += 2);
                                //f/*o*/r (int j = 0; j < data.onloadSignal.GetLength(1); j++)
                                //{
                                data.onloadSignal[i, 0] = (unValue & 0x01) == 0x01;
                                data.onloadSignal[i, 1] = (unValue & 0x02) == 0x02;
                                data.onloadSignal[i, 2] = (unValue & 0x04) == 0x04;
                                //}
                            }
                            //人工操作台
                            for (int i = 0; i < data.operateSignal.GetLength(0); i++)
                            {
                                unValue = BitConverter.ToUInt16(buf, nByteIdx += 2);
                                //for (int j = 0; j < data.bufSignal.GetLength(1); j++)
                                //{
                                data.operateSignal[i, 0] = (unValue & 0x01) == 0x01;
                                data.operateSignal[i, 1] = (unValue & 0x02) == 0x02;
                                data.operateSignal[i, 2] = (unValue & 0x04) == 0x04;
                                //}
                            }
                            //缓存架信号
                            for (int i = 0; i < data.bufSignal.GetLength(0); i++)
                            {
                                unValue = BitConverter.ToUInt16(buf, nByteIdx += 2);
                                //for (int j = 0; j < data.bufSignal.GetLength(1); j++)
                                //{
                                data.bufSignal[i, 0] = (unValue & 0x01) == 0x01;
                                data.bufSignal[i, 1] = (unValue & 0x02) == 0x02;
                                data.bufSignal[i, 2] = (unValue & 0x04) == 0x04;
                                //}
                            }
                            break;
                        }
                    case LoadingCmd.PalletUpCodeOne:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            int batIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[0].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[0].battery.GetLength(0); rowsIdx++)
                                {
                                    batIdx++;
                                    if (batIdx > 50)
                                        continue;
                                    string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                    nByteIdx += (batCodeLen * 2);

                                    data.palletDataArray[0].battery[rowsIdx, colsIdx].Code = strValue;
                                }
                            break;
                        }
                    case LoadingCmd.PalletMidCodeOne:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            int batIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[0].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[0].battery.GetLength(0); rowsIdx++)
                                {
                                    batIdx++;
                                    if (batIdx > 100 || batIdx <= 50)
                                        continue;
                                    string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                    nByteIdx += (batCodeLen * 2);

                                    data.palletDataArray[0].battery[rowsIdx, colsIdx].Code = strValue;
                                }
                            break;
                        }
                    case LoadingCmd.PalletDownCodeOne:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            int batIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[0].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[0].battery.GetLength(0); rowsIdx++)
                                {
                                    batIdx++;
                                    if (batIdx <= 100)
                                        continue;
                                    string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                    nByteIdx += (batCodeLen * 2);

                                    data.palletDataArray[0].battery[rowsIdx, colsIdx].Code = strValue;
                                }
                            break;
                        }
                    case LoadingCmd.PalletOne:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[0].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[0].battery.GetLength(0); rowsIdx++)
                                {
                                    if (BitConverter.ToUInt16(buf, nByteIdx) == 2 || BitConverter.ToUInt16(buf, nByteIdx) == 3)
                                    {
                                        data.palletDataArray[0].battery[rowsIdx, colsIdx].Type = BatteryStatus.NG;
                                        data.palletDataArray[0].battery[rowsIdx, colsIdx].NGType = (BatteryNGStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                    }
                                    else if (BitConverter.ToUInt16(buf, nByteIdx) > 3)
                                    {
                                        data.palletDataArray[0].battery[rowsIdx, colsIdx].Type = (BatteryStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                    }
                                    else
                                    {
                                        data.palletDataArray[0].battery[rowsIdx, colsIdx].Type = (BatteryStatus)BitConverter.ToUInt16(buf, nByteIdx);
                                    }
                                    nByteIdx += 2;
                                }
                            nByteIdx = 300;
                            data.palletDataArray[0].code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, pltCodeLen * 2).TrimEnd('\0').Trim();
                            nByteIdx += (pltCodeLen * 2);
                            data.palletDataArray[0].state = (PalletStatus)(/*BitConverter.ToUInt16(buf, nByteIdx) == 8 ? 1 :*/ BitConverter.ToUInt16(buf, nByteIdx));
                            Console.WriteLine(string.Format("夹具1当前状态：{0}", data.palletDataArray[0].state.ToString()));
                            nByteIdx += 2;
                            data.palletDataArray[0].count = BitConverter.ToUInt16(buf, nByteIdx);
                            nByteIdx += 2;
                            data.palletDataArray[0].haveFake = BitConverter.ToBoolean(buf, nByteIdx);
                            nByteIdx += 2;
                            data.palletDataArray[0].enable = BitConverter.ToUInt16(buf, nByteIdx) == 1 ? true : false;
                            nByteIdx += 2;
                            data.palletDataArray[0].clearFlag = BitConverter.ToUInt16(buf, nByteIdx) == 1 ? true : false;
                            nByteIdx += 2;
                            break;
                        }
                    case LoadingCmd.PalletUpCodeTwo:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            int batIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[1].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[1].battery.GetLength(0); rowsIdx++)
                                {
                                    batIdx++;
                                    if (batIdx > 50)
                                        continue;
                                    string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                    nByteIdx += (batCodeLen * 2);

                                    data.palletDataArray[1].battery[rowsIdx, colsIdx].Code = strValue;
                                }
                            break;
                        }
                    case LoadingCmd.PalletMidCodeTwo:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            int batIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[1].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[1].battery.GetLength(0); rowsIdx++)
                                {
                                    batIdx++;
                                    if (batIdx > 100 || batIdx <= 50)
                                        continue;
                                    string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                    nByteIdx += (batCodeLen * 2);

                                    data.palletDataArray[1].battery[rowsIdx, colsIdx].Code = strValue;
                                }
                            break;
                        }
                    case LoadingCmd.PalletDownCodeTwo:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            int batIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[1].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[1].battery.GetLength(0); rowsIdx++)
                                {
                                    batIdx++;
                                    if (batIdx <= 100)
                                        continue;
                                    string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                    nByteIdx += (batCodeLen * 2);

                                    data.palletDataArray[1].battery[rowsIdx, colsIdx].Code = strValue;
                                }
                            break;
                        }
                    case LoadingCmd.PalletTwo:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[1].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[1].battery.GetLength(0); rowsIdx++)
                                {
                                    if (BitConverter.ToUInt16(buf, nByteIdx) == 2 || BitConverter.ToUInt16(buf, nByteIdx) == 3)
                                    {
                                        data.palletDataArray[1].battery[rowsIdx, colsIdx].Type = BatteryStatus.NG;
                                        data.palletDataArray[1].battery[rowsIdx, colsIdx].NGType = (BatteryNGStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                    }
                                    else if (BitConverter.ToUInt16(buf, nByteIdx) > 3)
                                    {
                                        data.palletDataArray[1].battery[rowsIdx, colsIdx].Type = (BatteryStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                    }
                                    else
                                    {
                                        data.palletDataArray[1].battery[rowsIdx, colsIdx].Type = (BatteryStatus)BitConverter.ToUInt16(buf, nByteIdx);
                                    }
                                    nByteIdx += 2;
                                }
                            nByteIdx = 300;
                            data.palletDataArray[1].code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, pltCodeLen).TrimEnd('\0').Trim();
                            nByteIdx += (pltCodeLen * 2);
                            data.palletDataArray[1].state = (PalletStatus)(/*BitConverter.ToUInt16(buf, nByteIdx) == 8 ? 1 : */BitConverter.ToUInt16(buf, nByteIdx));
                            Console.WriteLine(string.Format("夹具2当前状态：{0}", data.palletDataArray[1].state.ToString()));
                            nByteIdx += 2;
                            data.palletDataArray[1].count = BitConverter.ToUInt16(buf, nByteIdx);
                            nByteIdx += 2;
                            data.palletDataArray[1].haveFake = BitConverter.ToBoolean(buf, nByteIdx);
                            nByteIdx += 2;
                            data.palletDataArray[1].enable = BitConverter.ToUInt16(buf, nByteIdx) == 1 ? true : false;
                            nByteIdx += 2;
                            data.palletDataArray[1].clearFlag = BitConverter.ToUInt16(buf, nByteIdx) == 1 ? true : false;
                            nByteIdx += 2;
                            break;
                        }
                    case LoadingCmd.PalletUpCodeThree:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            int batIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[2].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[2].battery.GetLength(0); rowsIdx++)
                                {
                                    batIdx++;
                                    if (batIdx > 50)
                                        continue;
                                    string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                    nByteIdx += (batCodeLen * 2);

                                    data.palletDataArray[2].battery[rowsIdx, colsIdx].Code = strValue;
                                }
                            break;
                        }
                    case LoadingCmd.PalletMidCodeThree:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            int batIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[2].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[2].battery.GetLength(0); rowsIdx++)
                                {
                                    batIdx++;
                                    if (batIdx > 100 || batIdx <= 50)
                                        continue;
                                    string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                    nByteIdx += (batCodeLen * 2);

                                    data.palletDataArray[2].battery[rowsIdx, colsIdx].Code = strValue;
                                }
                            break;
                        }
                    case LoadingCmd.PalletDownCodeThree:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            int batIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[2].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[2].battery.GetLength(0); rowsIdx++)
                                {
                                    batIdx++;
                                    if (batIdx <= 100)
                                        continue;
                                    string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                    nByteIdx += (batCodeLen * 2);

                                    data.palletDataArray[2].battery[rowsIdx, colsIdx].Code = strValue;
                                }
                            break;
                        }
                    case LoadingCmd.PalletThree:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            for (int colsIdx = 0; colsIdx < data.palletDataArray[2].battery.GetLength(1); colsIdx++)
                                for (int rowsIdx = 0; rowsIdx < data.palletDataArray[2].battery.GetLength(0); rowsIdx++)
                                {
                                    if (BitConverter.ToUInt16(buf, nByteIdx) == 2 || BitConverter.ToUInt16(buf, nByteIdx) == 3)
                                    {
                                        data.palletDataArray[2].battery[rowsIdx, colsIdx].Type = BatteryStatus.NG;
                                        data.palletDataArray[2].battery[rowsIdx, colsIdx].NGType = (BatteryNGStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                    }
                                    else if (BitConverter.ToUInt16(buf, nByteIdx) > 3)
                                    {
                                        data.palletDataArray[2].battery[rowsIdx, colsIdx].Type = (BatteryStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                    }
                                    else
                                    {
                                        data.palletDataArray[2].battery[rowsIdx, colsIdx].Type = (BatteryStatus)BitConverter.ToUInt16(buf, nByteIdx);
                                    }
                                    nByteIdx += 2;
                                }
                            nByteIdx = 300;
                            data.palletDataArray[2].code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, pltCodeLen).TrimEnd('\0').Trim();
                            nByteIdx += (pltCodeLen * 2);
                            data.palletDataArray[2].state = (PalletStatus)(/*BitConverter.ToUInt16(buf, nByteIdx) == 8 ? 1 : */BitConverter.ToUInt16(buf, nByteIdx));
                            Console.WriteLine(string.Format("夹具3当前状态：{0}", data.palletDataArray[2].state.ToString()));
                            nByteIdx += 2;
                            data.palletDataArray[2].count = BitConverter.ToUInt16(buf, nByteIdx);
                            nByteIdx += 2;
                            data.palletDataArray[2].haveFake = BitConverter.ToBoolean(buf, nByteIdx);
                            nByteIdx += 2;
                            data.palletDataArray[2].enable = BitConverter.ToUInt16(buf, nByteIdx) == 1 ? true : false;
                            nByteIdx += 2;
                            data.palletDataArray[2].clearFlag = BitConverter.ToUInt16(buf, nByteIdx) == 1 ? true : false;
                            nByteIdx += 2;
                            break;
                        }
                    // 上料允许取放
                    case LoadingCmd.ReadOnload:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            //上料运行状态
                            UInt16 unValue = BitConverter.ToUInt16(buf, nByteIdx += 0);
                            int bitMoveIdx = 0;
                            for (int i = 0; i < data.pickFlag.Length; i++)
                                data.placeFlag[i] = ((unValue >> bitMoveIdx++) & 0x01) > 0 ? true : false;
                            for (int i = 0; i < data.placeFlag.Length; i++)
                                data.pickFlag[i] = ((unValue >> bitMoveIdx++) & 0x01) > 0 ? true : false;

                            data.avoidMove = ((unValue >> bitMoveIdx++) & 0x01) > 0 ? true : false;
                            data.clearFlag = ((unValue >> bitMoveIdx++) & 0x01) > 0 ? true : false;
                            data.endFlag = ((unValue >> bitMoveIdx++) & 0x01) > 0 ? true : false;
                            break;
                        }
                    // 电池信息读
                    case LoadingCmd.ReadBarcode:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            data.batReadFlag = BitConverter.ToUInt16(buf, nByteIdx += 0) == 1 ? true : false;
                            nByteIdx += 2;
                            for (int batIdx = 0; batIdx < data.batCode.Length; batIdx++)
                            {
                                data.batCode[batIdx] = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                string codee = data.batCode[batIdx].Trim();
                                //if ("PF100A091501A013".Equals(codee))
                                //{

                                //}
                                nByteIdx += (batCodeLen * 2);
                            }
                            //data.UpPalletFlag = BitConverter.ToUInt16(buf, nByteIdx += 0) == 1 ? true : false;
                            //nByteIdx += 2;
                            //pltCodeLen
                            //data.PalletPickCode = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, pltCodeLen * 2).TrimEnd('\0').Trim();

                            break;
                        }
                        //来料料框读取
                    case LoadingCmd.ReadPickPalletCode:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            data.UpPalletFlag = BitConverter.ToUInt16(buf, nByteIdx += 0) == 1 ? true : false;
                            nByteIdx += 2;
                            data.PalletPickCode = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, pltCodeLen * 2).TrimEnd('\0').Trim();
                            data.PickPalletCode = data.PalletPickCode.Trim();
                            //for (int batIdx = 0; batIdx < data.batCode.Length; batIdx++)
                            //{
                            //    data.batCode[batIdx] = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                            //    string codee = data.batCode[batIdx].Trim();
                            //    nByteIdx += (batCodeLen * 2);
                            //}
                            break;
                        }
                    // 电池信息读
                    case LoadingCmd.ReadMoudle:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            //来料线
                            for (int idx = 0; idx < data.lineSignal.Length; idx++)
                            {
                                data.lineSignal[idx].Code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);
                            }
                            //取料
                            for (int idx = 0; idx < data.pickSignal.Length; idx++)
                            {
                                data.pickSignal[idx].Code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);
                            }
                            //夹爪
                            for (int idx = 0; idx < data.fingerSignal.Length; idx++)
                            {
                                data.fingerSignal[idx].Code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);
                            }
                            //配对位
                            for (int idx = 0; idx < data.bufOnloadSignal.Length; idx++)
                            {
                                data.bufOnloadSignal[idx].Code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);
                            }
                            //NG电池
                            for (int idx = 0; idx < data.batNGSignal.Length; idx++)
                            {
                                data.batNGSignal[idx].Code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);
                            }
                            //假电池
                            for (int idx = 0; idx < data.fakeSignal.Length; idx++)
                            {
                                data.fakeSignal[idx].Code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);
                            }
                            //来料物流框
                            for (int idx = 0; idx < data.batteryScan.Length; idx++)
                            {
                                data.batteryScan[idx].Code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, batCodeLen * 2).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);
                            }
                            nByteIdx = (800 * 2);
                            //扫码位
                            for (int idx = 0; idx < data.lineSignal.Length; idx++)
                            {
                                if (BitConverter.ToUInt16(buf, nByteIdx) == 2 || BitConverter.ToUInt16(buf, nByteIdx) == 3)
                                {
                                    data.lineSignal[idx].Type = BatteryStatus.NG;
                                    data.lineSignal[idx].NGType = (BatteryNGStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else if (BitConverter.ToUInt16(buf, nByteIdx) > 3)
                                {
                                    data.lineSignal[idx].Type = (BatteryStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else
                                {
                                    data.lineSignal[idx].Type = (BatteryStatus)BitConverter.ToUInt16(buf, nByteIdx);
                                }
                                nByteIdx += 2;
                            }
                            //来料线电池状态
                            for (int idx = 0; idx < data.pickSignal.Length; idx++)
                            {
                                if (BitConverter.ToUInt16(buf, nByteIdx) == 2 || BitConverter.ToUInt16(buf, nByteIdx) == 3)
                                {
                                    data.pickSignal[idx].Type = BatteryStatus.NG;
                                    data.pickSignal[idx].NGType = (BatteryNGStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else if (BitConverter.ToUInt16(buf, nByteIdx) > 3)
                                {
                                    data.pickSignal[idx].Type = (BatteryStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else
                                {
                                    data.pickSignal[idx].Type = (BatteryStatus)BitConverter.ToUInt16(buf, nByteIdx);
                                }
                                nByteIdx += 2;
                            }
                            //夹爪电池状态
                            for (int idx = 0; idx < data.fingerSignal.Length; idx++)
                            {
                                if (BitConverter.ToUInt16(buf, nByteIdx) == 2 || BitConverter.ToUInt16(buf, nByteIdx) == 3)
                                {
                                    data.fingerSignal[idx].Type = BatteryStatus.NG;
                                    data.fingerSignal[idx].NGType = (BatteryNGStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else if (BitConverter.ToUInt16(buf, nByteIdx) > 3)
                                {
                                    data.fingerSignal[idx].Type = (BatteryStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else
                                {
                                    data.fingerSignal[idx].Type = (BatteryStatus)BitConverter.ToUInt16(buf, nByteIdx);
                                }
                                nByteIdx += 2;
                            }
                            //配对位电池状态
                            for (int idx = 0; idx < data.bufOnloadSignal.Length; idx++)
                            {
                                if (BitConverter.ToUInt16(buf, nByteIdx) == 2 || BitConverter.ToUInt16(buf, nByteIdx) == 3)
                                {
                                    data.bufOnloadSignal[idx].Type = BatteryStatus.NG;
                                    data.bufOnloadSignal[idx].NGType = (BatteryNGStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else if (BitConverter.ToUInt16(buf, nByteIdx) > 3)
                                {
                                    data.bufOnloadSignal[idx].Type = (BatteryStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else
                                {
                                    data.bufOnloadSignal[idx].Type = (BatteryStatus)BitConverter.ToUInt16(buf, nByteIdx);
                                }
                                nByteIdx += 2;
                            }
                            //NG电池
                            for (int idx = 0; idx < data.batNGSignal.Length; idx++)
                            {
                                if (BitConverter.ToUInt16(buf, nByteIdx) == 2 || BitConverter.ToUInt16(buf, nByteIdx) == 3)
                                {
                                    data.batNGSignal[idx].Type = BatteryStatus.NG;
                                    data.batNGSignal[idx].NGType = (BatteryNGStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else if (BitConverter.ToUInt16(buf, nByteIdx) > 3)
                                {
                                    data.batNGSignal[idx].Type = (BatteryStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else
                                {
                                    data.batNGSignal[idx].Type = (BatteryStatus)BitConverter.ToUInt16(buf, nByteIdx);
                                }
                                nByteIdx += 2;
                            }
                            //假电池
                            for (int idx = 0; idx < data.fakeSignal.Length; idx++)
                            {
                                if (BitConverter.ToUInt16(buf, nByteIdx) == 2 || BitConverter.ToUInt16(buf, nByteIdx) == 3)
                                {
                                    data.fakeSignal[idx].Type = BatteryStatus.NG;
                                    data.fakeSignal[idx].NGType = (BatteryNGStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else if (BitConverter.ToUInt16(buf, nByteIdx) > 3)
                                {
                                    data.fakeSignal[idx].Type = (BatteryStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else
                                {
                                    data.fakeSignal[idx].Type = (BatteryStatus)BitConverter.ToUInt16(buf, nByteIdx);
                                }
                                nByteIdx += 2;
                            }

                            //来料物流框电池状态   从地址 字为824开始
                            for (int idx = 0; idx < data.batteryScan.Length; idx++)
                            {
                                if (BitConverter.ToUInt16(buf, nByteIdx) == 2 || BitConverter.ToUInt16(buf, nByteIdx) == 3)
                                {
                                    data.batteryScan[idx].Type = BatteryStatus.NG;
                                    data.batteryScan[idx].NGType = (BatteryNGStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else if (BitConverter.ToUInt16(buf, nByteIdx) > 3)
                                {
                                    data.batteryScan[idx].Type = (BatteryStatus)(BitConverter.ToUInt16(buf, nByteIdx) - 1);
                                }
                                else
                                {
                                    data.batteryScan[idx].Type = (BatteryStatus)BitConverter.ToUInt16(buf, nByteIdx);
                                }
                                nByteIdx += 2;
                            }
                            break;
                        }
                    case LoadingCmd.ReadState:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            UInt16 unValue = 0;
                            //上料运行状态
                            data.runningState = BitConverter.ToUInt16(buf, nByteIdx += 0);
                            break;
                        }
                    case LoadingCmd.ReadOrWriteHeartBeat:
                        {
                            OnloadData data = onloadData;
                            int nByteIdx = 0;
                            UInt16 unValue = 0;
                            //PLC心跳
                            data.heartBeat = BitConverter.ToUInt16(buf, nByteIdx += 0);
                            break;
                        }
                    case LoadingCmd.ReadScanAlram:
                        {
                            OnloadData data = onloadData;
                            UInt16 unValue = 0;
                            int nByteIdx = 0;
                            unValue = BitConverter.ToUInt16(buf, nByteIdx += 0);
                            data.scanAlram[0] = (unValue & 0x01) == 0x01;
                            data.scanAlram[1] = (unValue & 0x02) == 0x02;
                            data.scanAlram[2] = (unValue & 0x04) == 0x04;  //上料扫码枪为4个，OPCUA读取的5只，默认上传为0
                            data.scanAlram[3] = (unValue & 0x08) == 0x08;
                            data.scanAlram[4] = (unValue & 0x10) == 0x10;
                            break;
                        }
                    case LoadingCmd.ReadRobotStopAlram:
                        {
                            OnloadData data = onloadData;
                            UInt16 unValue = 0;
                            int nByteIdx = 0;
                            unValue = BitConverter.ToUInt16(buf, nByteIdx += 0);
                            data.onloadRobotAlram = (unValue & 0x080) == 0x80;
                            break;
                        }
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {

            }

        }

        private bool DataToBuf(LoadingCmd cmdID, int count, OnloadData onloadData, ref byte[] sendData)
        {
            bool result = false;
            switch (cmdID)
            {
                case LoadingCmd.WriteTrans:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(onloadData.transFlag);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case LoadingCmd.WriteTransAvoid:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(0);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);

                        break;
                    }
                case LoadingCmd.WriteBarcodeFlag:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf;
                        buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.batReadFlagReset));
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        break;
                    }
                case LoadingCmd.WritePalletFlag:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf;
                        buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.palletWriteFlag));
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        break;
                    }
                case LoadingCmd.WriteBarcode:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf;
                        buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.batWriteFlag));
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        for (int batIdx = 0; batIdx < onloadData.batFlag.Length; batIdx++)
                        {
                            buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.batFlag[batIdx]));
                            Array.Copy(buf, 0, sendData, idx, buf.Length);
                            idx += buf.Length;
                        }
                        break;
                    }
                case LoadingCmd.WritePalletCodeUp:
                    {
                        result = true;
                        int idx = 0;
                        int batIdx = 0;
                        byte[] buf;
                        for (int colsIdx = 0; colsIdx < onloadData.palletData.battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < onloadData.palletData.battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx > 50)
                                    continue;
                                buf = System.Text.Encoding.ASCII.GetBytes(onloadData.palletData.battery[rowsIdx, colsIdx].Code);

                                Array.Copy(buf, 0, sendData, idx, buf.Length);
                                idx += (batCodeLen * 2);
                            }
                        break;
                    }
                case LoadingCmd.WritePalletCodeMid:
                    {
                        result = true;
                        int idx = 0;
                        int batIdx = 0;
                        byte[] buf;
                        for (int colsIdx = 0; colsIdx < onloadData.palletData.battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < onloadData.palletData.battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx > 100 || batIdx <= 50)
                                    continue;
                                buf = System.Text.Encoding.ASCII.GetBytes(onloadData.palletData.battery[rowsIdx, colsIdx].Code);

                                Array.Copy(buf, 0, sendData, idx, buf.Length);
                                idx += (batCodeLen * 2);

                            }
                        break;
                    }
                case LoadingCmd.WritePalletCodeDown:
                    {
                        result = true;
                        int idx = 0;
                        int batIdx = 0;
                        byte[] buf;
                        for (int colsIdx = 0; colsIdx < onloadData.palletData.battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < onloadData.palletData.battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx <= 100)
                                    continue;
                                buf = System.Text.Encoding.ASCII.GetBytes(onloadData.palletData.battery[rowsIdx, colsIdx].Code);

                                Array.Copy(buf, 0, sendData, idx, buf.Length);
                                idx += (batCodeLen*2);

                            }
                        break;
                    }
                case LoadingCmd.WritePallet:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf;
                        for (int colsIdx = 0; colsIdx < onloadData.palletData.battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < onloadData.palletData.battery.GetLength(0); rowsIdx++)
                            {
                                if (Convert.ToInt16(onloadData.palletData.battery[rowsIdx, colsIdx].Type) == 2)
                                {
                                    buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.palletData.battery[rowsIdx, colsIdx].NGType + 1));
                                }
                                else if (Convert.ToInt16(onloadData.palletData.battery[rowsIdx, colsIdx].Type) == 6)
                                {
                                    buf = BitConverter.GetBytes(Convert.ToInt16(BatteryStatus.Invalid));
                                }
                                else if (Convert.ToInt16(onloadData.palletData.battery[rowsIdx, colsIdx].Type) == 3)
                                {
                                    buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.palletData.battery[rowsIdx, colsIdx].Type + 3));
                                }
                                else if (Convert.ToInt16(onloadData.palletData.battery[rowsIdx, colsIdx].Type) >= 3)
                                {
                                    buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.palletData.battery[rowsIdx, colsIdx].Type + 1));
                                }
                                else
                                {
                                    buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.palletData.battery[rowsIdx, colsIdx].Type));
                                }

                                Array.Copy(buf, 0, sendData, idx, buf.Length);
                                idx += buf.Length;
                            }
                        idx = 300;
                        buf = System.Text.Encoding.ASCII.GetBytes(onloadData.palletData.code);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += (pltCodeLen * 2);
                        buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.palletData.state));
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.palletData.count));
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(onloadData.palletData.haveFake);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        break;
                    }
                case LoadingCmd.WriteTranSaft:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(onloadData.tranSaft);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case LoadingCmd.ReadOrWriteHeartBeat:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(1);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case LoadingCmd.WriteInfoEnd:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(1);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case LoadingCmd.WriteRoleID:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(onloadData.roleID);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case LoadingCmd.WriteOPName:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = System.Text.Encoding.Unicode.GetBytes(onloadData.opName);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case LoadingCmd.WriteReBattery:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf;
                        for (int batIdx = 0; batIdx < onloadData.reBatteryState.Length; batIdx++)
                        {
                            buf = BitConverter.GetBytes(Convert.ToInt16(onloadData.reBatteryState[batIdx]));
                            Array.Copy(buf, 0, sendData, idx, buf.Length);
                            idx += buf.Length;
                        }
                        for (int batIdx = 0; batIdx < onloadData.reBattery.Length; batIdx++)
                        {
                            buf = System.Text.Encoding.ASCII.GetBytes(onloadData.reBattery[batIdx].Code);
                            Array.Copy(buf, 0, sendData, idx, buf.Length);
                            idx += (batCodeLen * 2);
                        }

                        break;
                    }
                default:
                    break;
            }
            ByteCodec(sendData, 0, count * 2, CodecMode.bit16_21);
            return result;
        }

        #endregion

        #region // 对外接口

        /// <summary>
        /// 设置Fins通讯类型
        /// </summary>
        /// <param name="type"></param>
        public void SetFinsType(FinsType type)
        {
            this.finsType = type;
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        /// <returns></returns>
        public bool IsConnect()
        {
            switch (this.finsType)
            {
                case FinsType.Tcp:
                    return this.finsTcp.IsConnect();
                case FinsType.Udp:
                    return this.finsUdp.IsConnect();
            }
            return false;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="ip">PLC的IP地址</param>
        /// <param name="port">PLC的端口号，默认9600</param>
        /// <param name="pcNodeID">PC节点号</param>
        /// <returns></returns>
        public bool Connect(string ip, int port, byte pcNodeID)
        {
            switch (this.finsType)
            {
                case FinsType.Tcp:
                    if (this.finsTcp.Connect(ip, port, pcNodeID))
                    {
                        InitThread(string.Format("DryingOvenClient {0}: {1} read Task", ip, port));
                    }
                    break;
                case FinsType.Udp:
                    if (this.finsUdp.Connect(ip, port, pcNodeID))
                    {
                        InitThread(string.Format("DryingOvenClient {0}: {1} read Task", ip, port));
                    }
                    break;
            }
            return IsConnect();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            bool result = false;
            switch (this.finsType)
            {
                case FinsType.Tcp:
                    result = this.finsTcp.Disconnect();
                    break;
                case FinsType.Udp:
                    result = this.finsUdp.Disconnect();
                    break;
            }
            ReleaseThread();
            this.ovenData.Release();
            this.errCount = 0;
            return result;
        }
        public ushort ConvertFlag()
        {
            return 0;
        }
        /// <summary>
        /// 写上料数据
        /// </summary>
        /// <param name="cmdID"></param>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <returns></returns>
        public bool SetLoadingData(LoadingCmd cmdID, OnloadData onloadData)
        {
            lock (writeLock)
            {
                bool sendResult = false;
                OnloadCmdAddr cmd = FinsCmdAddr[(int)cmdID];
                if (DataToBuf(cmdID, cmd.count, onloadData, ref sendBuffer))
                {
                    switch (this.finsType)
                    {
                        case FinsType.Tcp:
                            {
                                sendResult = this.finsTcp.WriteWords(cmd.zone, cmd.wordAddr, cmd.bitAddr, cmd.count, sendBuffer);
                                break;
                            }

                        case FinsType.Udp:
                            {
                                sendResult = this.finsUdp.WriteWords(cmd.zone, cmd.wordAddr, cmd.bitAddr, cmd.count, sendBuffer);
                                break;
                            }
                    }
                    Array.Clear(sendBuffer, 0, sendBuffer.Length);
                    return sendResult;
                }
                return false;
            }
        }

        /// <summary>
        /// 获取干燥炉数据
        /// </summary>
        /// <param name="dryOvenData"></param>
        /// <returns></returns>
        public bool GetLoadingData(ref OnloadData onloadData)
        {
            onloadData.CopyFrom(this.onloadData);
            return true;
        }

        #endregion

    }
}
