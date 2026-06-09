using HelperLibrary;
using System;
using System.Diagnostics;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 下料缓存：接收电池及发送电池
    /// </summary>
    class RunProcessOffloadBuffer : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_CheckBattery,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            
            // 接收电池：从上工位
            Auto_RecvBatttery,
            // 发送电池：到下工位
            Auto_SendBatttery,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
            RecvPosAll = 4,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OffloadBufferMsgStartID,
            RecvTimeout,
            SendTimeout,
            BatPosSenserErr,
        }

        #endregion

        #region // 字段，属性

        #region // IO

        /// <summary>
        /// 接收位，进入
        /// </summary>
        private int IRecvPosEnter;      // 接收位，进入
        /// <summary>
        /// 接收位，缓存
        /// </summary>
        private int IRecvPosBuffer;     // 接收位，缓存
        /// <summary>
        /// 接收位，到位
        /// </summary>
        private int IRecvPosInpos;      // 接收位，到位
        /// <summary>
        /// 接收位，电机
        /// </summary>
        private int ORecvPosMotor;      // 接收位，电机
        /// <summary>
        /// 拉带编号
        /// </summary>
        private int bufferIdx;     

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置

        /// <summary>
        /// 接收电池模组，即前一模组
        /// </summary>
        RunProcess recvModule;      // 接收电池模组，即前一模组

        #endregion

        #region // 模组参数
        #endregion

        #region // 模组数据
        #endregion

        #endregion

        public RunProcessOffloadBuffer(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.RecvPosAll, 0);

            PowerUpRestart();
            
            InitParameter();
            // 参数

        }

        #region // 模组运行

        protected override void PowerUpRestart()
        {
            base.PowerUpRestart();
            CurMsgStr("准备好", "Ready");

            InitRunData();
        }

        protected override void InitOperation()
        {
            if(!IsModuleEnable())
            {
                InitFinished();
                return;
            }

            switch((InitSteps)this.nextInitStep)
            {
                case InitSteps.Init_DataRecover:
                    {
                        CurMsgStr("数据恢复", "Data recover");
                        if(MachineCtrl.GetInstance().DataRecover)
                        {
                            LoadRunData();
                        }
                        this.nextInitStep = InitSteps.Init_CheckBattery;
                        break;
                    }
                case InitSteps.Init_CheckBattery:
                    {
                        CurMsgStr("检查电池状态", "Check sensor");
                        //
                        if (IRecvPosBuffer != -1)
                        {
                            if (CheckInputState(IRecvPosBuffer, !RecvPosIsEmpty())) //检查第一个感应器
                            {
                                this.nextInitStep = InitSteps.Init_End;
                            }
                        }
                        else
                        {
                            if (CheckInputState(IRecvPosInpos, !RecvPosIsEmpty()))
                            {
                                this.nextInitStep = InitSteps.Init_End;
                            }
                        }
                        
                        break;
                    }

                case InitSteps.Init_End:
                    {
                        CurMsgStr("初始化完成", "Init operation finished");
                        InitFinished();
                        break;
                    }

                default:
                    Trace.Assert(false, "this init step invalid");
                    break;
            }
        }

        protected override void AutoOperation()
        {
            if(!IsModuleEnable())
            {
                CurMsgStr("模组禁用", "Moudle not enable");
                Sleep(100);
                return;
            }

            //if(this.DryRun)
            //{
            //    Sleep(50);
            //}

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        // 有，发送电池请求
                        EventStatus state = GetEvent(this, EventList.OffloadSendBattery);
                        if (RecvPosIsFull() && ((EventStatus.Invalid == state) || (EventStatus.Finished == state)))
                        {
                            SetEvent(this, EventList.OffloadSendBattery, EventStatus.Require);
                            break;
                        }
                        // 有，已响应
                        else if (RecvPosIsFull() && (EventStatus.Response == state))
                        {
                            if (IRecvPosBuffer != -1)
                            {
                                // 拉带3出口双感应器判断
                                if (this.DryRun || (CheckInputState(IRecvPosBuffer, true) || CheckInputState(IRecvPosInpos, true)))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_SendBatttery;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                            else
                            {
                                if (this.DryRun || CheckInputState(IRecvPosInpos, true))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_SendBatttery;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                            break;
                        }
                        // 有空位，请求入料
                        if (IRecvPosBuffer != -1)
                        {
                            if (RecvPosIsEmpty() && CheckInputState(IRecvPosEnter, false) && CheckInputState(IRecvPosBuffer, false) && CheckInputState(IRecvPosInpos, false))
                            {
                                state = GetEvent(this.recvModule, EventList.OffloadSendBattery);
                                if (RecvPosIsEmpty() && (EventStatus.Require == state))
                                {
                                    //Def.WriteLog($"拉带[{bufferIdx}]有空位，请求入料 ", $"电机{ORecvPosMotor}");
                                    
                                    this.nextAutoStep = AutoSteps.Auto_RecvBatttery;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        else
                        {
                            if (RecvPosIsEmpty() && InputState(IRecvPosEnter, false) && InputState(IRecvPosInpos, false))
                            {
                                state = GetEvent(this.recvModule, EventList.OffloadSendBattery);
                                if (RecvPosIsEmpty() && (EventStatus.Require == state))
                                {
                                    //Def.WriteLog($"拉带[{bufferIdx}]有空位，请求入料 ", $"电机{ORecvPosMotor}");

                                    this.nextAutoStep = AutoSteps.Auto_RecvBatttery;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        
                        break;
                    }

                case AutoSteps.Auto_RecvBatttery:
                    {
                        #region 接收电池
                        CurMsgStr("接收电池", "Recv Battery");
                        EventStatus state = GetEvent(this.recvModule, EventList.OffloadSendBattery);
                        if (EventStatus.Require == state)
                        {
                            SetEvent(this.recvModule, EventList.OffloadSendBattery, EventStatus.Response);
                        }
                        else if ((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            Def.WriteLog($"拉带[{bufferIdx}]输出电机接收-启动", $"电机{ORecvPosMotor}");
                            OutputAction(ORecvPosMotor, true);
                            bool recvFin = false;
                            DateTime time = DateTime.Now;
                            bool IsBatHadRecv = false;
                            while (true)
                            {
                                if (IRecvPosBuffer != -1)
                                {
                                    if (InputState(IRecvPosEnter, true) && (InputState(IRecvPosInpos, false) && InputState(IRecvPosBuffer, false))
                                    && !IsBatHadRecv)
                                    {
                                        IsBatHadRecv = true;
                                        SetEvent(this.recvModule, EventList.OffloadSendBattery, EventStatus.Finished);

                                        for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
                                        {
                                            this.Battery[i].Copy(this.recvModule.Battery[i]);
                                            this.recvModule.Battery[i].Release();
                                        }
                                        this.recvModule.SaveRunData(SaveType.Battery);
                                        SaveRunData(SaveType.Battery);
                                    }
                                    // 前一个感应器感应到，前一个感应器感应不到且后一个感应器感应到
                                    if ((InputState(IRecvPosBuffer, true) || InputState(IRecvPosInpos, true)))
                                    {
                                        if (InputState(IRecvPosEnter, false))
                                        {
                                            recvFin = true;
                                            break;
                                        }
                                        else
                                        {
                                            OutputAction(ORecvPosMotor, false);

                                            // 拉带上有两组电池，报警
                                            ShowMessageBox((int)MsgID.SendTimeout, "接收上一个拉带电池异常", "请检查电池组是否到位", MessageType.MsgWarning);
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    if (InputState(IRecvPosEnter, true) && InputState(IRecvPosInpos, false)
                                    && !IsBatHadRecv)
                                    {
                                        IsBatHadRecv = true;
                                        SetEvent(this.recvModule, EventList.OffloadSendBattery, EventStatus.Finished);

                                        for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
                                        {
                                            this.Battery[i].Copy(this.recvModule.Battery[i]);
                                            this.recvModule.Battery[i].Release();
                                        }
                                        this.recvModule.SaveRunData(SaveType.Battery);
                                        SaveRunData(SaveType.Battery);
                                    }

                                    if (InputState(IRecvPosInpos, true))
                                    {
                                        if (InputState(IRecvPosEnter, false))
                                        {
                                            recvFin = true;
                                            break;
                                        }
                                        else
                                        {
                                            OutputAction(ORecvPosMotor, false);

                                            // 拉带上有两组电池，报警
                                            ShowMessageBox((int)MsgID.SendTimeout, "接收上一个拉带电池异常", "请检查电池组是否到位", MessageType.MsgWarning);
                                            break;
                                        }
                                    }
                                }

                                if (this.DryRun)
                                {
                                    Sleep(500);
                                    recvFin = true;
                                    break;
                                }
                                if ((DateTime.Now - time).TotalSeconds > 20)
                                {
                                    recvFin = false;
                                    break;
                                }
                                Sleep(1);
                            }

                            OutputAction(ORecvPosMotor, false);
                            Def.WriteLog($"拉带[{bufferIdx}]输出电机停止接收", $"电机{ORecvPosMotor}");

                            if (recvFin)
                            {
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                        #endregion 接收电池
                    }

                case AutoSteps.Auto_SendBatttery:
                    {
                        #region 发送电池
                        CurMsgStr("发送电池", "Send Battery");
                        EventStatus state = GetEvent(this, EventList.OffloadSendBattery);

                        bool sendFin = false;
                        if (EventStatus.Response == state)
                        {
                            SetEvent(this, EventList.OffloadSendBattery, EventStatus.Ready);
                        }
                        else if ((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            if (IRecvPosBuffer != -1)
                            {
                                // 拉带3双感应器判断，第一个感应器没有感应到电池，报警
                                if (CheckInputState(IRecvPosBuffer, true) || InputState(IRecvPosInpos, true))
                                {
                                    DateTime time = DateTime.Now;
                                    Def.WriteLog($"拉带[{bufferIdx}]输出电机启动发送", $"电机{ORecvPosMotor}");
                                    OutputAction(ORecvPosMotor, true);
                                    while (true)
                                    {
                                        state = GetEvent(this, EventList.OffloadSendBattery);
                                        if (EventStatus.Finished == state)
                                        {
                                            if (InputState(IRecvPosEnter, false) && InputState(IRecvPosInpos, false) && InputState(IRecvPosBuffer, false))
                                            {
                                                sendFin = true;
                                                break;
                                            }
                                            else if (InputState(IRecvPosEnter, true))
                                            {
                                                OutputAction(ORecvPosMotor, false);
                                                ShowMessageBox((int)MsgID.SendTimeout, "发送电池到下工位异常", "请检查电池是否到位", MessageType.MsgWarning);
                                            }
                                        }

                                        if ((DateTime.Now - time).TotalSeconds > 20)
                                        {
                                            OutputAction(ORecvPosMotor, false);
                                            ShowMessageBox((int)MsgID.SendTimeout, "发送电池到下工位超时", "请检查电池是否到位", MessageType.MsgWarning);
                                            break;
                                        }
                                        Sleep(1);
                                    }
                                }
                            }
                            else
                            {
                                // 但感应器判断
                                if (InputState(IRecvPosInpos, true))
                                {
                                    DateTime time = DateTime.Now;
                                    OutputAction(ORecvPosMotor, true);
                                    while (true)
                                    {
                                        state = GetEvent(this, EventList.OffloadSendBattery);
                                        if (EventStatus.Finished == state)
                                        {
                                            if (InputState(IRecvPosEnter, false) && InputState(IRecvPosInpos, false))
                                            {
                                                sendFin = true;
                                                break;
                                            }
                                            else if (InputState(IRecvPosEnter, true))
                                            {
                                                ShowMessageBox((int)MsgID.SendTimeout, "发送电池到下工位异常", "请检查电池是否到位", MessageType.MsgWarning);
                                            }
                                        }

                                        if ((DateTime.Now - time).TotalSeconds > 20)
                                        {
                                            OutputAction(ORecvPosMotor, false);
                                            ShowMessageBox((int)MsgID.SendTimeout, "发送电池到下工位超时", "请检查电池是否到位", MessageType.MsgWarning);
                                            break;
                                        }
                                        Sleep(1);
                                    }
                                }
                            }
                            OutputAction(ORecvPosMotor, false);
                            Def.WriteLog($"拉带[{bufferIdx}]输出电机停止发送", $"电机{ORecvPosMotor}");
                        }
                        else if(EventStatus.Finished == state)
                        {
                            sendFin = true;
                        }
                        if (sendFin)
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            //SaveRunData(SaveType.AutoStep | SaveType.Battery); // wjj 0804下工序接收完成时已经保存过电池，这里不重复写入
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                        #endregion 发送电池
                    }

                case AutoSteps.Auto_WorkEnd:
                    {
                        CurMsgStr("工作完成", "Work end");
                        this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                default:
                    {
                        Trace.Assert(false, "RunEx::AutoOperation/no this run step");
                        break;
                    }
            }

        }
        #endregion

        #region // 模组配置及参数

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            bufferIdx = (int)(this.GetRunID() - RunID.OffloadBuffer) + 1;

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组相关得模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string module = IniFile.ReadString(this.RunModule, "RecvModule", "", Def.GetAbsPathName(Def.ModuleExCfg));
            this.recvModule = MachineCtrl.GetInstance().GetModule(module);
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            this.IRecvPosEnter = AddInput("IRecvPosEnter");
            this.IRecvPosBuffer = AddInput("IRecvPosBuffer");
            this.IRecvPosInpos = AddInput("IRecvPosInpos");
            this.ORecvPosMotor = AddOutput("ORecvPosMotor");
        }

        #endregion

        #region // 电池数据

        /// <summary>
        /// 电池为满
        /// </summary>
        /// <returns></returns>
        public bool RecvPosIsFull()
        {
            if (RecvPosBatCount() > 0)
            {
                return true;
            }
            return false;
            //if (MachineCtrl.GetInstance().OffloadClear && RecvPosBatCount() > 0)
            //{
            //    return true;
            //}
            //else
            //{
            //    for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
            //    {
            //        if (BatteryStatus.Invalid == this.Battery[i].Type)
            //        {
            //            return false;
            //        }
            //    }
            //}
            //return true;
        }

        public int RecvPosBatCount()
        {
            int BatCount = 0;
            for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
            {
                if (BatteryStatus.OK == this.Battery[i].Type)
                {
                    BatCount++;
                }
            }
            return BatCount;
        }

        /// <summary>
        /// 电池为空
        /// </summary>
        /// <returns></returns>
        public bool RecvPosIsEmpty()
        {
            for(int i = 0; i < (int)ModDef.RecvPosAll; i++)
            {
                if(BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region // 防呆检查

        /// <summary>
        /// 检查输出点位是否可操作
        /// </summary>
        /// <param name="output"></param>
        /// <param name="bOn"></param>
        /// <returns></returns>
        public override bool CheckOutputCanActive(Output output, bool bOn)
        {
            return true;
        }
        
        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
            base.MonitorAvoidDie();
        }

        /// <summary>
        /// 设备停止后操作，如果派生类重写了该函数，它必须调用基实现。
        /// </summary>
        public override void AfterStopAction()
        {
            OutputAction(ORecvPosMotor, false);  // 停机，不可接收电池

            base.AfterStopAction();
        }

        #endregion

        #region // 保存数据

        /// <summary>
        /// 保存电池扫码数据
        /// </summary>
        private void SaveScanBatData(int batIdx, string code)
        {
            string file, title, text;
            file = string.Format(@"{0}\电芯扫码\{1}\{1}.csv", MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"));
            title = "日期,时间,电芯索引,电芯条码";
            text = string.Format("{0},{1},{2}\r\n", DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss"), (batIdx + 1), code);
            Def.ExportCsvFile(file, title, text);
        }

        #endregion

        #region // 上传Mes数据

        #endregion

    }
}
