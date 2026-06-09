using System;

namespace Machine
{
    class OffloadClient : BaseThread
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
        private OffloadData offloadData;     // 上料数据
        private OffloadData offloadBuf;       // 上料数据缓存
        private int batIdx;                //电池条码索引
        private int rowsIdx;                //电池条码行
        private int colsIdx;                //电池条码列
        private int pltCodeLen;             //电池条码长度
        private int batCodeLen;             //电池条码长度
        private object updateLock;          // 数据更新锁
        private object writeLock;          // 数据写入锁
        #endregion

        #region // 命令地址表

        public static OffloadCmdAddr[] FinsCmdAddr = new OffloadCmdAddr[(int)OffloadCmd.End]
        {
            new OffloadCmdAddr(ZoneCode.DMWord, 1000,4,0,0, 4),                 // 下料平台信号（读）

            new OffloadCmdAddr(ZoneCode.DMWord, 1010,1000,0,0,  1000),          // 夹具1电池条码前50个（读）    
            new OffloadCmdAddr(ZoneCode.DMWord, 2010,1000, 0,0, 1000),          // 夹具1电池条码中50个（读）       
            new OffloadCmdAddr(ZoneCode.DMWord, 3010,1000,0,0,  1000),          // 夹具1电池条码后50个（读）
            new OffloadCmdAddr(ZoneCode.DMWord, 4010,174, 0,0, 174),            // 夹具1电池信息（读） 

            new OffloadCmdAddr(ZoneCode.DMWord, 4233,1000, 0,0, 1000),          // 夹具2电池条码前50个（读）    
            new OffloadCmdAddr(ZoneCode.DMWord, 5233,1000,0,0,  1000),          // 夹具2电池条码中50个（读）       
            new OffloadCmdAddr(ZoneCode.DMWord, 6233,1000, 0,0, 1000),          // 夹具2电池条码后50个（读）
            new OffloadCmdAddr(ZoneCode.DMWord, 7233,174, 0,0, 174),            // 夹具2电池信息（读） 

            new OffloadCmdAddr(ZoneCode.DMWord, 10786,1,0,0,  1),               // 下料允许取放夹具（读）

            new OffloadCmdAddr(ZoneCode.DMWord, 20000,1,0,0,1),         // 心跳（读/写）
            new OffloadCmdAddr(ZoneCode.DMWord, 20001,1,0,0,1),         // 机台状态（读）

            new OffloadCmdAddr(ZoneCode.DMWord, 20002,1,0,0,1),         // 权限（读）

            new OffloadCmdAddr(ZoneCode.DMWord, 26102,1,0,0,1),         // 下料机械手报警（读）
            new OffloadCmdAddr(ZoneCode.DMWord, 26103,1,0,0,1),         // 下料夹具报警（读）

            new OffloadCmdAddr(ZoneCode.DMWord, 20003,10,0,0,10),         // 操作员名字下发（写）
            new OffloadCmdAddr(ZoneCode.DMWord, 10788,1,0,0,  1),               // 调度避让（写）

            new OffloadCmdAddr(ZoneCode.DMWord, 10792,1000,0,0,  1000),         // 夹具信息写入前50个(写)
            new OffloadCmdAddr(ZoneCode.DMWord, 11792,1000,0,0,  1000),         // 夹具信息写入前50个(写)
            new OffloadCmdAddr(ZoneCode.DMWord, 12792,1000,0,0,  1000),         // 夹具信息写入前50个(写)
            new OffloadCmdAddr(ZoneCode.DMWord, 13792,173,0,0,  173),           // 夹具信息写入(写)

            new OffloadCmdAddr(ZoneCode.DMWord, 10789,1,0,0,1),                 // 调度机器人安全位信号（写）

            new OffloadCmdAddr(ZoneCode.DMWord, 10790,1,0,0,1),                 // 信息传输完成信号（写）
        };

        #endregion

        #region // 构造函数

        public OffloadClient()
        {
            offloadData = new OffloadData();
            offloadBuf = new OffloadData();

            this.finsTcp = new FinsTCP();
            this.finsUdp = new FinsUDP();
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
                    for (int i = (int)OffloadCmd.RunState; i < (int)OffloadCmd.End; i++)
                    {
                        recvBuffer.Initialize();
                        switch (this.finsType)
                        {
                            case FinsType.Tcp:
                                if (this.finsTcp.ReadWords(FinsCmdAddr[i].zone, FinsCmdAddr[i].wordAddr, FinsCmdAddr[i].bitAddr, FinsCmdAddr[i].count, ref recvBuffer))
                                {
                                    BufToData((OffloadCmd)i, FinsCmdAddr[i].count, FinsCmdAddr[i].wordInterval, offloadBuf, recvBuffer);
                                }
                                else
                                {
                                    offloadBuf.dataError = true;
                                    if (this.errCount < (2 * (int)DryOvenCmd.SetParameter))
                                    {
                                        this.errCount++;
                                        WriteLog($"{this.finsTcp.GetIPInfo()}.DryingOvenClient.RunWhile() read {((DryOvenCmd)i).ToString()} fail.", HelperLibrary.LogType.Error);
                                    }
                                }
                                break;
                            case FinsType.Udp:

                                Array.Clear(recvBuffer, 0, recvBuffer.Length);
                                if (this.finsUdp.ReadWords(FinsCmdAddr[i].zone, FinsCmdAddr[i].wordAddr, FinsCmdAddr[i].bitAddr, FinsCmdAddr[i].count, ref recvBuffer))
                                {
                                    BufToData((OffloadCmd)i, FinsCmdAddr[i].count, FinsCmdAddr[i].wordInterval, offloadBuf, recvBuffer);
                                    this.errCount = 0; //wjj 220503
                                }
                                else
                                {
                                    offloadBuf.dataError = true;
                                    if (this.errCount < (2 * (int)DryOvenCmd.SetParameter))
                                    {
                                        this.errCount++;
                                        WriteLog($"{this.finsUdp.GetIPInfo()}.OnloadClient.RunWhile() read {((DryOvenCmd)i).ToString()} fail.", HelperLibrary.LogType.Error);
                                    }
                                }
                                break;
                        }
                        lock (updateLock)
                        {
                            offloadData.CopyFrom(offloadBuf);
                            //offloadBuf.Release();
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

        private void BufToData(OffloadCmd cmdID, int count, int dataInterval, OffloadData offloadData, byte[] buf)
        {
            ByteCodec(buf, 0, (count * 2), CodecMode.bit16_21);
            switch (cmdID)
            {
                // 下料运行状态
                case OffloadCmd.RunState:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        UInt16 unValue = 0;
                        unValue = BitConverter.ToUInt16(buf, nByteIdx += 0);

                        unValue = BitConverter.ToUInt16(buf, nByteIdx += 2);
                        //下料平台信号
                        for (int i = 0; i < data.offloadSignal.GetLength(0); i++)
                        {
                            unValue = BitConverter.ToUInt16(buf, nByteIdx += 2);
                            //for (int j = 0; j < data.onloadSignal.GetLength(1); j++)
                            //{
                            data.offloadSignal[i, 0] = (unValue & 0x01) == 0x01;
                            data.offloadSignal[i, 1] = (unValue & 0x02) == 0x02;
                            data.offloadSignal[i, 2] = (unValue & 0x04) == 0x04;
                            //nByteIdx += 2;
                            //}
                        }
                        break;
                    }
                case OffloadCmd.PalletUpCodeOne:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        int batIdx = 0;
                        for (int colsIdx = 0; colsIdx < data.palletDataArray[0].battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < data.palletDataArray[0].battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx > 50)
                                    continue;
                                string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, (batCodeLen * 2)).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);

                                data.palletDataArray[0].battery[rowsIdx, colsIdx].Code = strValue;
                            }
                        break;
                    }
                case OffloadCmd.PalletMidCodeOne:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        int batIdx = 0;
                        for (int colsIdx = 0; colsIdx < data.palletDataArray[0].battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < data.palletDataArray[0].battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx > 100 || batIdx <= 50)
                                    continue;
                                string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, (batCodeLen * 2)).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);

                                data.palletDataArray[0].battery[rowsIdx, colsIdx].Code = strValue;
                            }
                        break;
                    }
                case OffloadCmd.PalletDownCodeOne:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        int batIdx = 0;
                        for (int colsIdx = 0; colsIdx < data.palletDataArray[0].battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < data.palletDataArray[0].battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx <= 100)
                                    continue;
                                string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, (batCodeLen * 2)).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);

                                data.palletDataArray[0].battery[rowsIdx, colsIdx].Code = strValue;
                            }
                        break;
                    }

                case OffloadCmd.ReadPalletOne:
                    {
                        OffloadData data = offloadData;
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
                        data.palletDataArray[0].code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, (pltCodeLen * 2)).TrimEnd('\0').Trim();
                        nByteIdx += (pltCodeLen * 2);
                        //夹具状态
                        data.palletDataArray[0].state = (PalletStatus)BitConverter.ToUInt16(buf, nByteIdx += 0);
                        //电池数量
                        data.palletDataArray[0].count = BitConverter.ToUInt16(buf, nByteIdx += 2);
                        //有无假电池
                        data.palletDataArray[0].haveFake = BitConverter.ToBoolean(buf, nByteIdx += 2);
                        data.palletDataArray[0].enable = BitConverter.ToUInt16(buf, nByteIdx += 2) == 1 ? true : false;
                        break;
                    }

                case OffloadCmd.PalletUpCodeTwo:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        int batIdx = 0;
                        for (int colsIdx = 0; colsIdx < data.palletDataArray[1].battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < data.palletDataArray[1].battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx > 50)
                                    continue;
                                string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, (batCodeLen * 2)).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);

                                data.palletDataArray[1].battery[rowsIdx, colsIdx].Code = strValue;
                            }
                        break;
                    }
                case OffloadCmd.PalletMidCodeTwo:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        int batIdx = 0;
                        for (int colsIdx = 0; colsIdx < data.palletDataArray[1].battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < data.palletDataArray[1].battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx > 100 || batIdx <= 50)
                                    continue;
                                string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, (batCodeLen * 2)).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);

                                data.palletDataArray[1].battery[rowsIdx, colsIdx].Code = strValue;
                            }
                        break;
                    }
                case OffloadCmd.PalletDownCodeTwo:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        int batIdx = 0;
                        for (int colsIdx = 0; colsIdx < data.palletDataArray[1].battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < data.palletDataArray[1].battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx <= 100)
                                    continue;
                                string strValue = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, (batCodeLen * 2)).TrimEnd('\0').Trim();
                                nByteIdx += (batCodeLen * 2);

                                data.palletDataArray[1].battery[rowsIdx, colsIdx].Code = strValue;
                            }
                        break;
                    }

                case OffloadCmd.ReadPalletTwo:
                    {
                        OffloadData data = offloadData;
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
                        data.palletDataArray[1].code = System.Text.Encoding.ASCII.GetString(buf, nByteIdx, (pltCodeLen * 2)).TrimEnd('\0').Trim();
                        nByteIdx += (pltCodeLen * 2);
                        //夹具状态
                        data.palletDataArray[1].state = (PalletStatus)((BitConverter.ToUInt16(buf, nByteIdx += 0))==6?5:(BitConverter.ToUInt16(buf, nByteIdx += 0)));
                        //电池数量
                        data.palletDataArray[1].count = BitConverter.ToUInt16(buf, nByteIdx += 2);
                        //有无假电池
                        data.palletDataArray[1].haveFake = BitConverter.ToBoolean(buf, nByteIdx += 2);
                        data.palletDataArray[1].enable = BitConverter.ToUInt16(buf, nByteIdx += 2) == 1 ? true : false;

                        break;
                    }
                case OffloadCmd.ReadOffload:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        UInt16 unValue = BitConverter.ToUInt16(buf, nByteIdx += 0);
                        int bitMoveIdx = 0;
                        for (int i = 0; i < data.placeFlag.Length; i++)
                            data.placeFlag[i] = ((unValue >> bitMoveIdx++) & 0x01) > 0 ? true : false;
                        bitMoveIdx++;
                        for (int i = 0; i < data.pickFlag.Length; i++)
                            data.pickFlag[i] = ((unValue >> bitMoveIdx++) & 0x01) > 0 ? true : false;
                        bitMoveIdx++;

                        data.avoidMove = ((unValue >> bitMoveIdx++) & 0x01) > 0 ? true : false;
                        data.clearFlag = ((unValue >> bitMoveIdx++) & 0x01) > 0 ? true : false;
                        data.endFlag = ((unValue >> bitMoveIdx++) & 0x01) > 0 ? true : false;
                        break;
                    }
                case OffloadCmd.ReadState:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        UInt16 unValue = 0;
                        //上料运行状态
                        data.runningState = BitConverter.ToUInt16(buf, nByteIdx += 0);
                        break;
                    }
                case OffloadCmd.ReadOrWriteHeartBeat:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        UInt16 unValue = 0;
                        //PLC心跳
                        data.heartBeat = BitConverter.ToUInt16(buf, nByteIdx += 0);
                        break;
                    }
                // 下料夹爪报警
                case OffloadCmd.ReadFingerAlram:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        UInt16 unValue = 0;
                        unValue = BitConverter.ToUInt16(buf, nByteIdx += 0);
                        //下料夹爪报警
                        data.fingerAlarm = (unValue & 0x08) == 0x08;
                        data.fingerCommAlarm = (unValue & 0x10) == 0x10;
                        break;
                    }
                // 下料夹具感应报警
                case OffloadCmd.ReadPalletAlram:
                    {
                        OffloadData data = offloadData;
                        int nByteIdx = 0;
                        UInt16 unValue = 0;
                        unValue = BitConverter.ToUInt16(buf, nByteIdx += 0);
                        //下料夹具感应报警
                        if ((unValue & 0x40) == 0x40|| (unValue & 0x80) == 0x80 || (unValue & 0x100) == 0x100)
                        {
                            data.platAlarm[0] = true;
                        }
                        else
                        {
                            data.platAlarm[0] = false;
                        }
                        if ((unValue & 0x40) == 0x200 || (unValue & 0x400) == 0x400 || (unValue & 0x800) == 0x800)
                        {
                            data.platAlarm[1] = true;
                        }
                        else
                        {
                            data.platAlarm[1] = false;
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        private bool DataToBuf(OffloadCmd cmdID, int count, OffloadData offloadData, ref byte[] sendData)
        {
            //sendData = new byte[2000];
            bool result = false;
            switch (cmdID)
            {
                case OffloadCmd.WriteTrans:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(offloadData.transFlag);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case OffloadCmd.WriteTranSaft:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(offloadData.transSaftFlag);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case OffloadCmd.WritePalletCodeUp:
                    {
                        result = true;
                        int idx = 0;
                        int batIdx = 0;
                        byte[] buf;
                        for (int colsIdx = 0; colsIdx < offloadData.palletData.battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < offloadData.palletData.battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx > 50)
                                    continue;
                                buf = System.Text.Encoding.ASCII.GetBytes(offloadData.palletData.battery[rowsIdx, colsIdx].Code);

                                Array.Copy(buf, 0, sendData, idx, buf.Length);
                                idx += (batCodeLen * 2);

                            }
                        break;
                    }
                case OffloadCmd.WritePalletCodeMid:
                    {
                        result = true;
                        int idx = 0;
                        int batIdx = 0;
                        byte[] buf;
                        for (int colsIdx = 0; colsIdx < offloadData.palletData.battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < offloadData.palletData.battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx > 100 || batIdx <= 50)
                                    continue;
                                buf = System.Text.Encoding.ASCII.GetBytes(offloadData.palletData.battery[rowsIdx, colsIdx].Code);

                                Array.Copy(buf, 0, sendData, idx, buf.Length);
                                idx += (batCodeLen * 2);

                            }
                        break;
                    }
                case OffloadCmd.WritePalletCodeDown:
                    {
                        result = true;
                        int idx = 0;
                        int batIdx = 0;
                        byte[] buf;
                        for (int colsIdx = 0; colsIdx < offloadData.palletData.battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < offloadData.palletData.battery.GetLength(0); rowsIdx++)
                            {
                                batIdx++;
                                if (batIdx <= 100)
                                    continue;
                                buf = System.Text.Encoding.ASCII.GetBytes(offloadData.palletData.battery[rowsIdx, colsIdx].Code);

                                Array.Copy(buf, 0, sendData, idx, buf.Length);
                                idx += (batCodeLen * 2);

                            }
                        break;
                    }
                case OffloadCmd.WritePallet:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf;
                        for (int colsIdx = 0; colsIdx < offloadData.palletData.battery.GetLength(1); colsIdx++)
                            for (int rowsIdx = 0; rowsIdx < offloadData.palletData.battery.GetLength(0); rowsIdx++)
                            {
                                if (Convert.ToInt16(offloadData.palletData.battery[rowsIdx, colsIdx].Type) == 2)
                                {
                                    buf = BitConverter.GetBytes(Convert.ToInt16(offloadData.palletData.battery[rowsIdx, colsIdx].NGType + 1));
                                }
                                else if (Convert.ToInt16(offloadData.palletData.battery[rowsIdx, colsIdx].Type) == 6)
                                {
                                    buf = BitConverter.GetBytes(Convert.ToInt16(BatteryStatus.Invalid));
                                }
                                else if (Convert.ToInt16(offloadData.palletData.battery[rowsIdx, colsIdx].Type) >= 3)
                                {
                                    buf = BitConverter.GetBytes(Convert.ToInt16(offloadData.palletData.battery[rowsIdx, colsIdx].Type + 1));
                                }
                                else
                                {
                                    buf = BitConverter.GetBytes(Convert.ToInt16(offloadData.palletData.battery[rowsIdx, colsIdx].Type));
                                }

                                Array.Copy(buf, 0, sendData, idx, buf.Length);
                                idx += buf.Length;
                            }
                        idx = 300;
                        buf = System.Text.Encoding.ASCII.GetBytes(offloadData.palletData.code);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += (pltCodeLen * 2);
                        buf = BitConverter.GetBytes(Convert.ToInt16(offloadData.palletData.state));
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(Convert.ToInt16(offloadData.palletData.count));
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;
                        buf = BitConverter.GetBytes(offloadData.palletData.haveFake);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        idx += buf.Length;

                        break;
                    }
                case OffloadCmd.ReadOrWriteHeartBeat:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(1);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case OffloadCmd.WriteInfoEnd:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(1);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case OffloadCmd.WriteRoleID:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = BitConverter.GetBytes(offloadData.roleID);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
                        break;
                    }
                case OffloadCmd.WriteOPName:
                    {
                        result = true;
                        int idx = 0;
                        byte[] buf = System.Text.Encoding.Unicode.GetBytes(offloadData.opName);
                        Array.Copy(buf, 0, sendData, idx, buf.Length);
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
        /// 写下料数据
        /// </summary>
        /// <param name="cmdID"></param>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <returns></returns>
        public bool SetBlankingData(OffloadCmd cmdID, OffloadData offloadData)
        {
            lock (writeLock)
            {
                bool sendResult = false;
                OffloadCmdAddr cmd = FinsCmdAddr[(int)cmdID];
                if (DataToBuf(cmdID, cmd.count, offloadData, ref sendBuffer))
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
        /// 获取下料数据
        /// </summary>
        /// <param name="dryOvenData"></param>
        /// <returns></returns>
        public bool GetOffloadData(ref OffloadData offloadData)
        {
            offloadData.CopyFrom(this.offloadData);
            return true;
        }

        #endregion

    }
}
