using HelperLibrary;
using System;
using System.Diagnostics;
using SystemControlLibrary;

namespace Machine
{
    /// <summary>
    /// 下料线体
    /// </summary>
    class RunProcessOffloadLine : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,
            Init_CheckBattery,
            Init_CheckCylinder,
            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            Auto_RecvBattery,
            Auto_SendBatttery,
            Auto_RotateCylSend,  // 对接拉带
            Auto_RotateCylRecv,  // 对接放料
            Auto_WorkEnd,
        }

        private enum ModDef
        {
            PlacePos_0 = 0,
            PlacePos_1,
            PlacePos_2,
            PlacePos_3,
            PlacePos_ALL,
            BufferPos_0 = PlacePos_ALL,
            BufferPos_1,
            BufferPos_2,
            BufferPos_3,
            BufferPos_ALL,
            PlacePos_Buffer_ALL = BufferPos_ALL,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OffloadLineMsgStartID,
            TransferTimeout,
            SendTimeout,
        }
        #endregion

        #region // 字段，属性

        #region // IO
        /// <summary>
        /// 旋转气缸，推出到位
        /// </summary>
        private int IRotateCylPush;      // 旋转气缸，推出到位
        /// <summary>
        /// 旋转气缸，拉回到位
        /// </summary>
        private int IRotateCylPull;      // 旋转气缸，拉回到位
        /// <summary>
        /// 推出气缸，推出到位
        /// </summary>
        private int IPushCylPush;        // 推出气缸，推出到位
        /// <summary>
        /// 推出气缸，拉回到位
        /// </summary>
        private int IPushCylPull;        // 推出气缸，拉回到位
        /// <summary>
        /// 放料位有料感应
        /// </summary>
        private int[] IPlaceHasBat;        // 放料位有料感应
        /// <summary>
        /// 放料位出口有料感应
        /// </summary>
        private int IPlaceOut;           // 放料位出口有料感应

        /// <summary>
        /// 旋转气缸，推出
        /// </summary>
        private int ORotateCylPush;      // 旋转气缸，推出
        /// <summary>
        /// 旋转气缸，拉回
        /// </summary>
        private int ORotateCylPull;      // 旋转气缸，拉回
        /// <summary>
        /// 推出气缸，推出
        /// </summary>
        private int OPushCylPush;        // 推出气缸，推出
        /// <summary>
        /// 推出气缸，拉回
        /// </summary>
        private int OPushCylPull;        // 推出气缸，拉回

        /// <summary>
        /// 电机
        /// </summary>
        public int ORecvPosMotor;        // 电机

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数

        #endregion

        #region // 模组数据

        private bool monitorOut;    // 监测电池出：运行时置true，停止时置false
        #endregion

        #endregion

        public RunProcessOffloadLine(int RunID) : base(RunID)
        {
            InitBatteryPalletSize((int)ModDef.PlacePos_Buffer_ALL, 0);

            PowerUpRestart();

            InitParameter();

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
            if (!IsModuleEnable())
            {
                InitFinished();
                return;
            }
            switch ((InitSteps)this.nextInitStep)
            {
                case InitSteps.Init_DataRecover:
                    {
                        CurMsgStr("数据恢复", "Data recover");
                        if (MachineCtrl.GetInstance().DataRecover)
                        {
                            LoadRunData();
                        }
                        this.nextInitStep = InitSteps.Init_CheckBattery;
                        break;
                    }
                case InitSteps.Init_CheckBattery:
                    {
                        CurMsgStr("检查电池状态", "Check battery state");
                        if (!PlacePosIsEmpty())
                        {
                            if (CheckBatInputState(true))
                            {
                                this.nextInitStep = InitSteps.Init_CheckCylinder;
                            }

                            //this.nextInitStep = InitSteps.Init_CheckCylinder;
                        }
                        else
                        {
                            this.nextInitStep = InitSteps.Init_End;
                        }
                        break;
                    }
                case InitSteps.Init_CheckCylinder:
                    {
                        CurMsgStr("检查气缸状态", "Check cylinder state");
                        if (InputState(IPlaceOut, false))
                        {
                            if (PushCylPush(false) && RotateCylPush(true))
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
            if (!IsModuleEnable())
            {
                CurMsgStr("模组禁用", "Moudle not enable");
                Sleep(100);
                return;
            }

            //if (Def.IsNoHardware())
            //{
            //    Sleep(100);
            //}

            if (!this.monitorOut)
            {
                //this.monitorOut = true;
                //MonitorBufferOut();
            }

            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        #region // 等待开始信号 
                        CurMsgStr("等待开始信号", "Wait work start");

                        // 放料位无，发送放电池信号
                        EventStatus state = GetEvent(this, EventList.OffLoadLinePlaceBattery);
                        if (PlacePosIsEmpty() && PlacePosSenserIsSafe() &&
                            ((EventStatus.Invalid == state) || (EventStatus.Finished == state)))
                        {
                            OutputAction(ORecvPosMotor, false);
                            //按顺序电池放料位为空, X轴气缸退回，再旋转气缸退回
                            if (CheckBatInputState(false) && InputState(IPlaceOut, false) && PushCylPush(false) && RotateCylPush(false))
                            {
                                SetEvent(this, EventList.OffLoadLinePlaceBattery, EventStatus.Require);
                            }
                            break;
                        }
                        // 放料位无，已响应
                        else if (PlacePosIsEmpty() && (EventStatus.Response == state))
                        {
                            OutputAction(ORecvPosMotor, false);
                            //按顺序电池放料位为空，X轴气缸退回，再旋转气缸退回
                            if (CheckBatInputState(false) && InputState(IPlaceOut, false) && PushCylPush(false) && RotateCylPush(false))
                            {
                                SetEvent(this, EventList.OffLoadLinePlaceBattery, EventStatus.Ready);
                            }
                            break;
                        }

                        bool PlacePosFull = PlacePosIsFull();
                        if (!PlacePosFull)
                        {
                            //if (PlacePosBatCount() > 0 && MachineCtrl.GetInstance().OffloadClear)
                            if (PlacePosBatCount() > 0)
                            {
                                PlacePosFull = true;
                            }
                        }

                        // 放料位有：放料旋转平台 -> 缓存线
                        if (((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                            && PlacePosFull)
                        {
                            if (RotateCylPush(true) && PushCylPush(true))
                            {

                            }

                            state = GetEvent(this, EventList.OffloadSendBattery);
                            // 发送请求
                            if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                            {
                                SetEvent(this, EventList.OffloadSendBattery, EventStatus.Require);
                            }
                            // 已响应
                            else if (PlacePosFull && (EventStatus.Response == state))
                            {
                                if (this.DryRun)
                                {
                                    this.nextAutoStep = AutoSteps.Auto_SendBatttery;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                                
                                bool StateOK = true;
                                for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
                                {
                                    if (BatteryStatus.OK == this.Battery[i].Type)
                                    {
                                        if (!InputState(IPlaceHasBat[i], true))
                                        {
                                            StateOK = false;
                                            string msg = string.Format("【{0}{1}】感应器非ON，请检查感应器状态是否正常！！！"
                                                        , Inputs(IPlaceHasBat[i]).Num, Inputs(IPlaceHasBat[i]).Name);
                                            ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);

                                            break;
                                        }
                                    }
                                }
                                if (!StateOK)
                                {
                                    break;
                                }
                                this.nextAutoStep = AutoSteps.Auto_SendBatttery;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                        #endregion // 等待开始信号 
                    }
                case AutoSteps.Auto_SendBatttery:
                    {
                        #region // 发送电池
                        CurMsgStr("发送电池", "Send Battery");

                        EventStatus state = GetEvent(this, EventList.OffloadSendBattery);
                        if (EventStatus.Response == state)
                        {
                            //按顺序电池位有电池，旋转气缸推出，X轴气缸推出
                            if (RotateCylPush(true) && PushCylPush(true))
                            {
                                SetEvent(this, EventList.OffloadSendBattery, EventStatus.Ready);
                            }
                        }
                        else if ((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            //按顺序电池位有电池，旋转气缸推出，X轴气缸推出
                            if (RotateCylPush(true) && PushCylPush(true))
                            {
                                if (CheckBatInputState(true) || (CheckBatInputState(false) && InputState(IPlaceOut, true)))
                                {
                                    OutputAction(ORecvPosMotor, true);
                                    DateTime time = DateTime.Now;
                                    while (true)
                                    {
                                        state = GetEvent(this, EventList.OffloadSendBattery);
                                        if (CheckBatInputState(false) && InputState(IPlaceOut, false)
                                            && (EventStatus.Finished == state))
                                        {
                                            break;
                                        }
                                        if ((DateTime.Now - time).TotalSeconds > 20)
                                        {
                                            OutputAction(ORecvPosMotor, false);
                                            ShowMessageBox((int)MsgID.SendTimeout, "发送电池到下工位超时", "请检查电池是否到位", MessageType.MsgWarning);
                                            break;
                                        }
                                        Sleep(1);
                                    }
                                    OutputAction(ORecvPosMotor, false);
                                }
                            }
                        }
                        if (EventStatus.Finished == state)
                        {
                            this.nextAutoStep = AutoSteps.Auto_RotateCylRecv;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }

                        break;
                        #endregion // 发送电池
                    }
                case AutoSteps.Auto_RotateCylSend:
                    {
                        if (RotateCylPush(true) && PushCylPush(true))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                    }
                    break;
                case AutoSteps.Auto_RotateCylRecv:
                    {
                        if (PushCylPush(false) && RotateCylPush(false))
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                    }
                    break;
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

        bool IsPlaceOut = false;
        /// <summary>
        /// 出料位是否过电池
        /// </summary>
        /// <returns></returns>
        public bool OutPosIsEmpty()
        {
            if (!IsPlaceOut && InputState(IPlaceOut, true))
            {
                //出位置有电池
                IsPlaceOut = true;
                return false;
            }
            if (IsPlaceOut && InputState(IPlaceOut, false))
            {
                IsPlaceOut = false;
                return true;
            }
            return false;
        }

        #endregion

        #region // 模组配置及参数

        public override bool InitializeConfig(string module)
        {
            if (!base.InitializeConfig(module))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化通用模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.monitorOut = false;

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.OnLoad = ReadBoolParameter(this.RunModule, "OnLoad", true);
            this.OffLoad = ReadBoolParameter(this.RunModule, "OffLoad", true);

            return base.ReadParameter();
        }
        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxFinger = (int)ModDef.PlacePos_ALL;
            this.IPlaceHasBat = new int[maxFinger];
            for (int i = 0; i < maxFinger; i++)
            {
                this.IPlaceHasBat[i] = AddInput("IPlaceHasBat" + i);
            }

            this.IRotateCylPush = AddInput("IRotateCylPush");
            this.IRotateCylPull = AddInput("IRotateCylPull");

            this.IPushCylPush = AddInput("IPushCylPush");
            this.IPushCylPull = AddInput("IPushCylPull");

            this.IPlaceOut = AddInput("IPlaceOut");
            //this.IBufferEnter = AddInput("IBufferEnter");

            this.ORecvPosMotor = AddOutput("ORecvPosMotor");

            //this.IBufferOut = AddInput("IBufferOut");
            //this.IRequireOffLoad = AddInput("IRequireOffLoad");

            //this.IBufferHasBat = new int[2];
            //for(int i = 0; i < this.IBufferHasBat.Length; i++)
            //{
            //    this.IBufferHasBat[i]= AddInput($"IBufferHasBat{i}");
            //}

            this.ORotateCylPush = AddOutput("ORotateCylPush");
            this.ORotateCylPull = AddOutput("ORotateCylPull");
            this.OPushCylPush = AddOutput("OPushCylPush");
            this.OPushCylPull = AddOutput("OPushCylPull");
            //this.OFrontMotor = AddOutput("OFrontMotor");
            //this.OAfterMotor = AddOutput("OAfterMotor");
            //this.OPlacingBattery = AddOutput("OPlacingBattery");
        }

        /// <summary>
        /// 旋转气缸动作  为 true 推出  为 false 回退  
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        protected bool RotateCylPush(bool push)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            if (IRotateCylPush < 0 || IRotateCylPull < 0 || ORotateCylPush < 0 || ORotateCylPull < 0)
            {
                return false;
            }
            // 操作 
            OutputAction(ORotateCylPush, push);
            OutputAction(ORotateCylPull, !push);

            if (!(WaitInput(Inputs(IRotateCylPush), push) && WaitInput(Inputs(IRotateCylPull), !push)))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 推出气缸推出  为 true 推出  为 false 回退
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        protected bool PushCylPush(bool push)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            if (IPushCylPush < 0 || IPushCylPull < 0 || OPushCylPush < 0 || OPushCylPull < 0)
            {
                return false;
            }
            // 操作 

            OutputAction(OPushCylPush, push);
            OutputAction(OPushCylPull, !push);

            if (!(WaitInput(Inputs(IPushCylPush), push) && WaitInput(Inputs(IPushCylPull), !push)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 放料位电池为满
        /// </summary>
        /// <returns></returns>
        public bool PlacePosIsFull()
        {
            for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if (BatteryStatus.Invalid == this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 放料位电池位空
        /// </summary>
        /// <returns></returns>
        public bool PlacePosIsEmpty()
        {
            for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if (BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 放料位电池数
        /// </summary>
        /// <returns></returns>
        public int PlacePosBatCount()
        {
            int BatCount = 0;
            for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if (BatteryStatus.OK == this.Battery[i].Type)
                {
                    BatCount++;
                }
            }
            return BatCount;
        }

        /// <summary>
        /// 放电池位感应器检测
        /// </summary>
        /// <returns></returns>
        public bool PlacePosSenserIsSafe()
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            if (CheckBatInputState(false))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 放料时 检查气缸感应器安全
        /// </summary>
        /// <returns></returns>
        public bool CheckRotateCylSafe()
        {
            if (!InputState(IRotateCylPush, true) || !InputState(IPushCylPull, true))
            {
                CheckInputState(IRotateCylPush, true);
                CheckInputState(IPushCylPull, true);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 下料线体有电池
        /// </summary>
        /// <returns></returns>
        //private bool OffLineHasBat()
        //{
        //    if (InputState(IBufferEnter, true))
        //    {
        //        return true;
        //    }
        //    if(InputState(IBufferOut, true))
        //    {
        //        return true;
        //    }
        //    for(int i = 0; i < this.IBufferHasBat.Length; i++)
        //    {
        //        if(!InputState(IBufferHasBat[i], false))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// 电池下料夹具为空
        /// </summary>
        /// <returns></returns>
        private bool OffloadPalletEmpty()
        {
            RunProcessOffloadBattery run = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery;
            if (null != run)
            {
                foreach (var item in run.Pallet)
                {
                    if (item.IsEmpty())
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查放料位电芯状态
        /// </summary>
        /// <param name="isOn"></param>
        /// <returns></returns>
        private bool CheckBatInputState(bool isOn)
        {
            if (isOn)
            {
                for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
                {
                    if (BatteryStatus.OK == this.Battery[i].Type )
                    {
                        //电池顺序和感应器相反
                        //int idx = 0;
                        //if (0 == i)
                        //{
                        //    idx = 3;
                        //}
                        //else if (1 == i)
                        //{
                        //    idx = 2;
                        //}
                        //else if(2 == i)
                        //{
                        //    idx = 1;
                        //}

                        if (!InputState(IPlaceHasBat[i], isOn))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
                {
                    if (BatteryStatus.Invalid == this.Battery[i].Type && !InputState(IPlaceHasBat[i], isOn))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        private bool CheckBatPlacePosState()
        {
            for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if (BatteryStatus.OK == this.Battery[i].Type)
                {
                    //电池顺序和感应器相反
                    int idx = 0;
                    if (0 == i)
                    {
                        idx = 3;
                    }
                    else if (1 == i)
                    {
                        idx = 2;
                    }
                    else if (2 == i)
                    {
                        idx = 1;
                    }

                    if (!InputState(IPlaceHasBat[idx], true))
                    {
                        return false;
                    }
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
            // 推出气缸未回退，禁止操作旋转气缸
            if ((ORotateCylPush > -1 && Outputs(ORotateCylPush) == output) || (ORotateCylPull > -1 && Outputs(ORotateCylPull) == output))
            {
                if (!InputState(IPlaceOut, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作旋转气缸！！！"
                                , Inputs(IPlaceOut).Num, Inputs(IPlaceOut).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
                if (InputState(IPushCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作旋转气缸！！！"
                                , Inputs(IPushCylPull).Num, Inputs(IPushCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
                RunProcessOffloadBattery runOffLoadBat = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery;
                if (null != runOffLoadBat)
                {
                    if (!runOffLoadBat.CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
                    {
                        string msg = string.Format("下料电机Z轴不在安全位，禁止操作旋转气缸！！！");
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                        return false;
                    }
                }
            }

            // A气缸退回到位，X不可推出
            if (InputState(IRotateCylPull, true) && Outputs(OPushCylPush) == output && bOn)
            {
                string msg = string.Format("【{0}{1}】感应器ON，禁止操作X气缸推出！！！"
                                , Inputs(IRotateCylPull).Num, Inputs(IRotateCylPull).Name);
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                return false;
            }

            if ((OPushCylPull > -1 && Outputs(OPushCylPull) == output) || (OPushCylPush > -1 && Outputs(OPushCylPush) == output))
            {
                if (!InputState(IPlaceOut, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作推出气缸！！！"
                                , Inputs(IPlaceOut).Num, Inputs(IPlaceOut).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }

            if ((ORecvPosMotor > -1 && Outputs(ORecvPosMotor) == output))
            {
                // 推出气缸 未到位  禁止操作前段电机输出
                if (InputState(IPushCylPush, false))
                {
                    string msg = string.Format("【{0}{1}】感应器状态非ON，禁止操作前段电机！！！"
                               , Inputs(IPushCylPush).Num, Inputs(IPushCylPush).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
                // 旋转气缸 未到位  禁止操作前段电机输出
                if (InputState(IRotateCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器状态非ON，禁止操作前段电机！！！"
                               , Inputs(IRotateCylPull).Num, Inputs(IRotateCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 检查电机是否可移动
        /// </summary>
        /// <param name="motor"></param>
        /// <param name="nLocation"></param>
        /// <param name="fValue"></param>
        /// <param name="moveType"></param>
        /// <returns></returns>
        public override bool CheckMotorCanMove(Motor motor, int nLocation, float fValue, MotorMoveType moveType)
        {
            return true;
        }

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
        }

        /// <summary>
        /// 设备停止后操作，如果派生类重写了该函数，它必须调用基实现。
        /// </summary>
        public override void AfterStopAction()
        {
            this.monitorOut = false;
        }

        #endregion

        #region // 后段物流线，监测电池出

        /// <summary>
        /// 监测电池出
        /// </summary>
        //private async void MonitorBufferOut()
        //{
        //    await System.Threading.Tasks.Task.Delay(1);

        //    while (this.monitorOut)
        //    {
        //        // 缓存位出料感应ON，等待向后工序放料
        //        if (InputState(IBufferOut, true))
        //        {
        //            // 有请求，则完整执行一次放料
        //            if(InputState(IRequireOffLoad, true))
        //            {
        //                DateTime time = DateTime.Now;
        //                OutputAction(OPlacingBattery, true);
        //                OutputAction(OAfterMotor, true);
        //                while(InputState(IBufferOut, true))
        //                {
        //                    if ((DateTime.Now - time).TotalSeconds > 10)
        //                    {
        //                        break;
        //                    }
        //                    Sleep(1);
        //                }
        //                OutputAction(OPlacingBattery, false);
        //            }
        //        }
        //        // 出料口感应OFF，前段电池流向出料口
        //        else if(OffloadPalletEmpty())
        //        {
        //            if(OffLineHasBat() && OutputState(OAfterMotor, false))
        //            {
        //                OutputAction(OAfterMotor, true);
        //                DateTime time = DateTime.Now;
        //                while(InputState(IBufferOut, false))
        //                {
        //                    if((DateTime.Now - time).TotalSeconds > 10)
        //                    {
        //                        break;
        //                    }
        //                    Sleep(1);
        //                }
        //            }
        //        }
        //        if(OutputState(OFrontMotor, false))
        //        {
        //            OutputAction(OAfterMotor, false);
        //        }
        //        Sleep(1);
        //    }
        //    OutputAction(OPlacingBattery, false);
        //    OutputAction(OAfterMotor, false);

        //    Def.WriteLog("RunProcessOffloadLine", "MonitorBufferOut() end");
        //}

        #endregion

    }
}

