using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 下料出：接收电池、对接下工序（发送电池至下工序）
    /// </summary>
    class RunProcessOffloadOut : RunProcess
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
            Start = ModuleMsgID.OffloadOutMsgStartID,
            RecvTimeout,
            SendTimeout,
            BatPosSenserErr,
        }

        #endregion

        #region // 字段，属性

        #region // IO

        /// <summary>
        /// 放料位有电池
        /// </summary>
        private int[] IPlacePosInpos;    // 放料位有电池
        /// <summary>
        ///  出料位有电池
        /// </summary>
        private int IPlaceOut;            // 出料位有电池

        /// <summary>
        /// 旋转气缸，推出到位
        /// </summary>
        private int IRotateCylPush;      // 旋转气缸，推出到位
        /// <summary>
        /// 旋转气缸，拉回到位
        /// </summary>
        private int IRotateCylPull;      // 旋转气缸，拉回到位
        /// <summary>
        /// X推出气缸，推出到位
        /// </summary>
        private int IPushCylPush;        // 推出气缸，推出到位
        /// <summary>
        /// X推出气缸，拉回到位
        /// </summary>
        private int IPushCylPull;        // 推出气缸，拉回到位

        /// <summary>
        /// Y推出气缸，推出到位
        /// </summary>
        private int IYPushCylPush;        // 推出气缸，推出到位
        /// <summary>
        /// Y推出气缸，拉回到位
        /// </summary>
        private int IYPushCylPull;        // 推出气缸，拉回到位

        /// <summary>
        /// 旋转气缸，推出
        /// </summary>
        private int ORotateCylPush;      // 旋转气缸，推出
        /// <summary>
        /// 旋转气缸，拉回
        /// </summary>
        private int ORotateCylPull;      // 旋转气缸，拉回
        /// <summary>
        /// X推出气缸，推出
        /// </summary>
        private int OPushCylPush;        // 推出气缸，推出
        /// <summary>
        /// X推出气缸，拉回
        /// </summary>
        private int OPushCylPull;        // 推出气缸，拉回

        /// <summary>
        /// Y推出气缸，推出
        /// </summary>
        private int OYPushCylPush;        // 推出气缸，推出
        /// <summary>
        /// Y推出气缸，拉回
        /// </summary>
        private int OYPushCylPull;        // 推出气缸，拉回

        /// <summary>
        /// 接收电机
        /// </summary>
        private int ORecvPosMotor;      // 接收位，电机

        /// <summary>
        /// 发送电机
        /// </summary>
        private int OSendPosMotor;      // 接收位，电机

        /// <summary>
        /// 请求放料
        /// </summary>
        private int OSendRequire;       // ①发送电池请求：本工序 → 下工序

        private int OSendOutSave;

        /// <summary>
        /// ②接收电池响应：下工序 → 本工序（取料请求）
        /// </summary>
        private int IRecvResponse;      // ②接收电池响应：下工序 → 本工序

        /// <summary>
        /// ②接收电池完成：下工序 → 本工序（取料完成）
        /// </summary>
        private int IRecvFinish;      // ②发送电池完成：下工序 → 本工序

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置

        RunProcess recvModule;      // 接收电池模组，即前一模组

        #endregion

        #region // 模组参数
        private int sendBatTimeout;   // 发送电池超时 秒
        private int recvBatDelay;     // 接收电池延迟 毫秒
        #endregion

        #region // 模组数据
        #endregion

        #endregion

        public RunProcessOffloadOut(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.RecvPosAll, 0);

            PowerUpRestart();
            
            InitParameter();

            // 参数
            InsertVoidParameter("sendBatTimeout", "发送电池超时", "电池离开出料口后等待超时(秒)：5-15", sendBatTimeout, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("recvBatDelay", "接收电池延迟", "接收电池延迟毫秒：ms", recvBatDelay, RecordType.RECORD_INT);
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
                        //bool recvPosIsEmpty = RecvPosIsEmpty();
                        if (CheckInputState(IPlaceOut, false)) // 出入料位置感应到有电池，则报警
                        {
                            this.nextInitStep = InitSteps.Init_End;
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

            //if (this.DryRun)
            //{
            //    Sleep(50);
            //}

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        bool RecvPosFull = RecvPosIsFull();
                        if (!RecvPosFull)
                        {
                            if (RecvPosBatCount() > 0 && MachineCtrl.GetInstance().OffloadClear)
                            {
                                RecvPosFull = true;
                            }
                        }

                        // 给下一工位发送拉带出口安全信号
                        OutputAction(OSendOutSave, InputState(IPlaceOut, true));

                        // 有，已响应
                        if (RecvPosFull) // 不等待下工序请求，先做好下工序请求前的动作准备
                        {
                            if (this.DryRun || CheckBatInputState(true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_SendBatttery;
                                SaveRunData(SaveType.AutoStep);
                                break;
                            }
                            else if (CheckBatInputState(false))
                            {
                                ShowMessageBox((int)MsgID.SendTimeout, "接收上一个工位电池异常", "请检查电池组是否到位", MessageType.MsgWarning);
                                break;
                            }
                            else if (CheckInputState(IPlaceOut, false)) // 拉带上有电池，且处于非接收和发送状态下，感应到电池，则报警
                            {
                                ShowMessageBox((int)MsgID.SendTimeout, "接收上一个工位电池异常", "请检查电池组是否到位", MessageType.MsgWarning);
                                break;
                            }
                        }
                        // 有空位，请求入料
                        if (RecvPosIsEmpty() && CheckBatInputState(false) && CheckInputState(IPlaceOut, false))
                        {
                            if (PushToRecv()) //对接上一工位
                            {
                                EventStatus state = GetEvent(this.recvModule, EventList.OffloadSendBattery);
                                if (EventStatus.Require == state)
                                {
                                    this.nextAutoStep = AutoSteps.Auto_RecvBatttery;
                                    SaveRunData(SaveType.AutoStep);
                                    break;
                                }
                            }
                        }
                        break;
                    }

                case AutoSteps.Auto_RecvBatttery:
                    {
                        #region //接收电池
                        CurMsgStr("接收电池", "Recv Battery");
                        //出料线没有电池
                        if (CheckBatInputState(false) && CheckInputState(IPlaceOut, false))
                        {
                            // 对接接收,信号准备好之后再检测一遍感应器
                            if (!PushToRecv())
                            {
                                break;
                            }

                            EventStatus state = GetEvent(this.recvModule, EventList.OffloadSendBattery);
                            if (EventStatus.Require == state)
                            {
                                SetEvent(this.recvModule, EventList.OffloadSendBattery, EventStatus.Response);
                            }
                            else if (EventStatus.Ready == state)
                            {
                                Def.WriteLog("出料旋转拉带接收电池电机启动", $"电机{ORecvPosMotor}");
                                OutputAction(ORecvPosMotor, true);

                                bool recvFin = false;
                                DateTime time = DateTime.Now;
                                while (true)
                                {
                                    if (InputState(IPlaceOut, false) && CheckBatInputState(true))
                                    {
                                        Sleep(recvBatDelay);

                                        recvFin = true;
                                        OutputAction(ORecvPosMotor, false);
                                        Def.WriteLog("出料旋转拉带接收电池电机停止", $"电机{ORecvPosMotor}");
                                        break;
                                    }
                                    if (this.DryRun)
                                    {
                                        Sleep(500);
                                        OutputAction(ORecvPosMotor, false);
                                        recvFin = true;
                                        break;
                                    }
                                    if ((DateTime.Now - time).TotalSeconds > 10)
                                    {
                                        recvFin = false;
                                        OutputAction(ORecvPosMotor, false);
                                        Def.WriteLog("出料旋转拉带接收电池超时电机停止", $"电机{ORecvPosMotor}");
                                        ShowMessageBox((int)MsgID.SendTimeout, "接收电池上一工序超时", "请检查电池是否到位", MessageType.MsgWarning);
                                        break;
                                    }
                                    Sleep(1);
                                }
                                
                                if (recvFin)
                                {
                                    for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
                                    {
                                        this.Battery[i].Copy(this.recvModule.Battery[i]);
                                        this.recvModule.Battery[i].Release();
                                    }
                                    this.recvModule.SaveRunData(SaveType.Battery);

                                    SetEvent(this.recvModule, EventList.OffloadSendBattery, EventStatus.Finished);

                                    this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                    SaveRunData(SaveType.AutoStep | SaveType.Battery);
                                }
                            }
                        }
                        
                        break;
                        #endregion //接收电池
                    }

                case AutoSteps.Auto_SendBatttery:
                    {
                        #region //发送电池

                        if (this.DryRun)
                        {
                            Sleep(500);
                            for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
                            {
                                this.Battery[i].Release();
                            }
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                            break;
                        }
                        // X轴推出，对接下料拉带，回退回安全位
                        // A轴回退，旋转口对接下料拉带，回退接口对接来料拉带
                        // Y轴推出对接来料拉带，回退回安全位

                        // 调整XYA气缸，准备发送电池(Y退，A退，X出)
                        // Y退回到位
                        if (!PushToSend())
                        {
                            break;
                        }
                        //出料安全感应器输出给下一工序
                        OutputAction(OSendOutSave, InputState(IPlaceOut, true));
                        OutputAction(OSendRequire, true); //向下一工序放料请求

                        bool recvFin = false;
                        bool sendFin = false;
                        //取料请求
                        if (InputState(IRecvResponse, true))
                        {
                            CurMsgStr("发送电池", "Send Battery");

                            // 拉带上无电池
                            if (!CheckBatInputState(true))
                            {
                                OutputAction(OSendRequire, false);
                                Def.WriteLog("出料旋转拉带发送电池异常-感应位无电池", $"电机{ORecvPosMotor}");
                                ShowMessageBox((int)MsgID.BatPosSenserErr, "发送电池到下工序超时", "请检查电池是否到位", MessageType.MsgWarning);
                                break;
                            }

                            Def.WriteLog("出料旋转拉带发送电池开始-电机启动", $"电机{ORecvPosMotor}");
                            OutputAction(OSendPosMotor, true);

                            DateTime time = DateTime.Now;
                            DateTime timeFinish = DateTime.Now;
                            while (true)
                            {
                                // sendFin = false -> true -> false -> true
                                // 电池过出口感应器
                                if (InputState(IPlaceOut, true))
                                {
                                    sendFin = false;
                                }

                                // 拉带上没有电池
                                if (!sendFin && CheckBatInputState(false) && InputState(IPlaceOut, false))
                                {
                                    sendFin = true; // 发送完成
                                    timeFinish = DateTime.Now;
                                }

                                //发送完成
                                if (sendFin && 
                                    (InputState(IRecvFinish, true) || ((DateTime.Now - timeFinish).TotalSeconds > this.sendBatTimeout)))
                                {
                                    recvFin = true;
                                    OutputAction(OSendPosMotor, false);
                                    Def.WriteLog("出料旋转拉带发送电池完成-电机停止", $"电机{ORecvPosMotor}");
                                    break;
                                }
                                if ((DateTime.Now - time).TotalSeconds > 15)
                                {
                                    OutputAction(OSendPosMotor, false);
                                    OutputAction(OSendRequire, false);
                                    Def.WriteLog("出料旋转拉带发送电池超时-电机停止", $"电机{ORecvPosMotor}");
                                    ShowMessageBox((int)MsgID.SendTimeout, "发送电池到下工序超时", "请检查电池是否到位", MessageType.MsgWarning);
                                    break;
                                }
                                Sleep(1);
                            }
                        }
                        else
                        {
                            CurMsgStr("发送电池，等待发送电池请求", "Send Battery");
                            // 电池被拿走或感应器异常 报警
                            if (CheckBatInputState(false) && InputState(IPlaceOut, false))
                            {
                                Def.WriteLog("出料旋转拉带发送电池-未检测到电池", $"电机{ORecvPosMotor}");
                                ShowMessageBox((int)MsgID.BatPosSenserErr, "出料位发送未检测到电池", "请检查电池是否到位或感应器是否正常", MessageType.MsgWarning);
                                break;
                            }
                        }
                        if (recvFin)
                        {
                            OutputAction(OSendRequire, false);

                            for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
                            {
                                this.Battery[i].Release();
                            }

                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                        #endregion //发送电池
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
            this.recvBatDelay = 1000;
            this.sendBatTimeout = 5;
            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.sendBatTimeout = ReadIntParameter(this.RunModule, "sendBatTimeout", this.sendBatTimeout);
            this.recvBatDelay = ReadIntParameter(this.RunModule, "recvBatDelay", this.recvBatDelay);

            if (sendBatTimeout < 5)
            {
                this.sendBatTimeout = 5;
            }
            else if (sendBatTimeout > 15)
            {
                this.sendBatTimeout = 15;
            }

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
            this.IPlacePosInpos = new int[(int)ModDef.RecvPosAll];// AddInput("IRecvPosEnter");

            for (int i=0; i < (int)ModDef.RecvPosAll; i++)
            {
                this.IPlacePosInpos[i] = AddInput("IPlacePosInpos" + i);
            }

            this.IPlaceOut = AddInput("IPlaceOut");

            this.IRotateCylPush = AddInput("IRotateCylPush");
            this.IRotateCylPull = AddInput("IRotateCylPull");

            this.IPushCylPush = AddInput("IPushCylPush");
            this.IPushCylPull = AddInput("IPushCylPull");

            this.IYPushCylPush = AddInput("IYPushCylPush");
            this.IYPushCylPull = AddInput("IYPushCylPull");

            this.ORotateCylPush = AddOutput("ORotateCylPush");
            this.ORotateCylPull = AddOutput("ORotateCylPull");

            this.OPushCylPush = AddOutput("OPushCylPush");
            this.OPushCylPull = AddOutput("OPushCylPull");

            this.OYPushCylPush = AddOutput("OYPushCylPush");
            this.OYPushCylPull = AddOutput("OYPushCylPull");

            this.ORecvPosMotor = AddOutput("ORecvPosMotor");
            this.OSendPosMotor = AddOutput("OSendPosMotor");

            this.OSendRequire = AddOutput("OSendRequire");
            this.OSendOutSave = AddOutput("OSendOutsave"); 

            this.IRecvResponse = AddInput("IRecvResponse");
            this.IRecvFinish = AddInput("IRecvFinish");
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
        /// X推出气缸推出  为 true 推出  为 false 回退
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        protected bool XPushCylPush(bool push)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            if (IPushCylPush < 0 || IPushCylPull < 0 
                || OPushCylPush < 0 || OPushCylPull < 0)
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
        /// Y推出气缸推出  为 true 推出  为 false 回退
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        protected bool YPushCylPush(bool push)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            if (IYPushCylPush < 0 || IYPushCylPull < 0 
                || OYPushCylPush < 0 || OYPushCylPull < 0)
            {
                return false;
            }
            // 操作 

            OutputAction(OYPushCylPush, push);
            OutputAction(OYPushCylPull, !push);

            if (!(WaitInput(Inputs(IYPushCylPush), push) && WaitInput(Inputs(IYPushCylPull), !push)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 对接接收电芯
        /// </summary>
        /// <returns></returns>
        public bool PushToRecv()
        {
            // X退（ X气缸没有退回到位，先退回X气缸 ）
            if (!InputState(IPushCylPull, true))
            {
                XPushCylPush(false);
                Trace.WriteLine("X气缸没有退回到位，先退回X气缸");
                return false;
            }

            // A出 (Y气缸没有退回到位且旋转气缸没有推出到位，先Y退回)
            // A旋转气缸操作，先退回Y气缸
            if (!InputState(IYPushCylPull, true) && !InputState(IRotateCylPush, true))
            {
                // Y 气缸退回操作
                YPushCylPush(false);
                return false;
            }

            // 旋转气缸没有推出到位
            if (!InputState(IRotateCylPush, true))
            {
                // 旋转气缸推出
                RotateCylPush(true);
                return false;
            }

            // Y 气缸没有推出到位，Y气缸推出
            if (!InputState(IYPushCylPush, true))
            {
                YPushCylPush(true);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 对接发送电芯
        /// </summary>
        /// <returns></returns>
        public bool PushToSend()
        {
            if (!InputState(IYPushCylPull, true) && !YPushCylPush(false))
            {
                return false;
            }
            // X气缸没有退回到位且旋转气缸没有退回到位,X气缸退回
            if (InputState(IPushCylPull, false) && InputState(IRotateCylPull, false))
            {
                XPushCylPush(false);
                return false;
            }
            else if (InputState(IPushCylPull, true) && !RotateCylPush(false))
            {
                return false;
            }
            // X 气缸推出
            if (!InputState(IPushCylPush, true) && !XPushCylPush(true))
            {
                return false;
            }
            return true;
        }


        public bool YAXMotorCanRecvBat()
        {
            DateTime dateTime = DateTime.Now;
            bool canRecv = false;
            while (true)
            {
                // X退（ X气缸没有退回到位，先退回X气缸 ）
                if (!InputState(IPushCylPull, true))
                {
                    XPushCylPush(false);
                    Trace.WriteLine("X气缸没有退回到位，先退回X气缸");
                }
                else
                {
                    // A出 (Y气缸没有退回到位且旋转气缸没有推出到位，先Y退回)
                    // A旋转气缸操作，先退回Y气缸
                    if (!InputState(IYPushCylPull, true) && !InputState(IRotateCylPush, true))
                    {
                        // Y 气缸退回操作
                        YPushCylPush(false);
                    }
                    else
                    {
                        // 旋转气缸没有推出到位
                        if (!InputState(IRotateCylPush, true))
                        {
                            // 旋转气缸推出
                            RotateCylPush(true);
                        }
                        else
                        {
                            // Y 气缸没有推出到位，Y气缸推出
                            if (!InputState(IYPushCylPush, true))
                            {
                                YPushCylPush(true);
                            }
                            if (InputState(IYPushCylPush, true))
                            {
                                canRecv = true;
                                break;
                            }
                        }
                    }
                }

                if ( (DateTime.Now - dateTime).TotalSeconds > 60 )
                {
                    ShowMessageBox((int)MsgID.SendTimeout, "接收操作旋转超时", "请检旋转模块连接和感应器状态是否正常", MessageType.MsgWarning);
                    break;
                }

                Sleep(1);
            }

            return canRecv;
        }

        public bool YAXMotorCanSendBat()
        {
            DateTime dateTime = DateTime.Now;
            bool canSend = false;
            bool IsOutTime = false;
            while (true)
            {
                //调整XYA气缸，准备发送电池(Y退，A退，X出)
                // Y退回到位
                if (!InputState(IYPushCylPull, true) )
                {
                    YPushCylPush(false);
                }
                else
                {
                    // X气缸没有退回到位且旋转气缸没有退回到位,X气缸退回
                    if (!InputState(IPushCylPull, true) && !InputState(IRotateCylPull, true))
                    {
                        XPushCylPush(false);
                    }
                    else
                    {
                        if (InputState(IPushCylPull, true) && !InputState(IRotateCylPull, true))
                        {
                            RotateCylPush(false);
                        }
                        else
                        {
                            if (!InputState(IPushCylPush, true))
                            {
                                XPushCylPush(true);
                            }
                            if (InputState(IPushCylPush, true))
                            {
                                canSend = true;
                                break;
                            }
                        }
                    }
                }
                if ((DateTime.Now - dateTime).TotalSeconds > 60)
                {
                    IsOutTime = true;
                    break;
                }

                Sleep(1);
            }

            if (IsOutTime)
            {
                ShowMessageBox((int)MsgID.SendTimeout, "接收操作旋转超时", "请检旋转模块连接和感应器状态是否正常", MessageType.MsgWarning);
            }

            return canSend;
        }

        public bool AXMotorCanRecvBat()
        {
            return true;
        }

        public bool AXMotorCanSendBat()
        {
            return true;
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
                return true;
            for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
            {
                if (BatteryStatus.Invalid == this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
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

        /// <summary>
        /// 接收位电池数
        /// </summary>
        /// <returns></returns>
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
        /// 检查接收位电芯状态
        /// </summary>
        /// <param name="isOn"></param>
        /// <returns></returns>
        private bool CheckBatInputState(bool isOn)
        {
            if (isOn)
            {
                for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
                {
                    //判断有电池-这里不判断
                    if (InputState(IPlacePosInpos[i], isOn))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
                {
                    if (!InputState(IPlacePosInpos[i], isOn))
                    {
                        return false;
                    }
                }
                return true;
            }
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
            // 操作旋转气缸检测
            if ( (ORotateCylPush > -1 && Outputs(ORotateCylPush) == output) || (ORotateCylPull > -1 && Outputs(ORotateCylPull) == output))
            {
                // X 气缸退回到位检测
                if (IPushCylPull > -1 && InputState(IPushCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作旋转气缸！！！"
                                , Inputs(IPushCylPull).Num, Inputs(IPushCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }

                // Y 气缸退回到位检测
                if (IYPushCylPull > -1 && InputState(IYPushCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作旋转气缸！！！"
                                , Inputs(IYPushCylPull).Num, Inputs(IYPushCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }

                if (!InputState(IPlaceOut, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作旋转气缸！！！"
                                , Inputs(IPlaceOut).Num, Inputs(IPlaceOut).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }

            // 操作X气缸检测
            if ((OPushCylPull > -1 && Outputs(OPushCylPull) == output) || (OPushCylPush > -1 && Outputs(OPushCylPush) == output))
            {
                // 旋转气缸退回到位检测
                if (IRotateCylPull > -1 && InputState(IRotateCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作X推出气缸！！！"
                                , Inputs(IRotateCylPull).Num, Inputs(IRotateCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }

                // Y 气缸退回到位检测
                if (IYPushCylPull > -1 && InputState(IYPushCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作X推出气缸！！！"
                                , Inputs(IYPushCylPull).Num, Inputs(IYPushCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }

            // 操作Y气缸检测
            if ((OYPushCylPull > -1 && Outputs(OYPushCylPull) == output) || (OYPushCylPush > -1 && Outputs(OYPushCylPush) == output))
            {
                // 旋转气缸推出到位检测
                if (IRotateCylPush > -1 && InputState(IRotateCylPush, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作X推出气缸！！！"
                               , Inputs(IRotateCylPush).Num, Inputs(IRotateCylPush).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }

                // X 推出气缸检测
                if (IPushCylPull > -1 && InputState(IPushCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器非ON，禁止操作X推出气缸！！！"
                              , Inputs(IPushCylPull).Num, Inputs(IPushCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }

            // 电机接收电池
            if (ORecvPosMotor > -1 && Outputs(ORecvPosMotor) == output)
            {
                // X气缸 退回未到位 禁止操作接收电机输出
                if (InputState(IPushCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器状态非ON，禁止操作前段电机！！！"
                               , Inputs(IPushCylPull).Num, Inputs(IPushCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }

                // 旋转气缸 推出未到位  禁止操作接收电机输出
                if (InputState(IRotateCylPush, false))
                {
                    string msg = string.Format("【{0}{1}】感应器状态非ON，禁止操作前段电机！！！"
                               , Inputs(IRotateCylPush).Num, Inputs(IRotateCylPush).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
                // Y推出气缸 推出未到位  禁止操作接收电机输出
                if (InputState(IYPushCylPush, false))
                {
                    string msg = string.Format("【{0}{1}】感应器状态非ON，禁止操作前段电机！！！"
                               , Inputs(IYPushCylPush).Num, Inputs(IYPushCylPush).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }

            // 电机发送电池
            if (OSendPosMotor > -1 && Outputs(OSendPosMotor) == output)
            {
                // Y气缸 推出未到位  禁止操作发送电机输出
                if (InputState(IYPushCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器状态非ON，禁止操作前段电机！！！"
                               , Inputs(IYPushCylPull).Num, Inputs(IYPushCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }

                // 旋转气缸 未到位  禁止操作发送电机输出
                if (InputState(IRotateCylPull, false))
                {
                    string msg = string.Format("【{0}{1}】感应器状态非ON，禁止操作前段电机！！！"
                               , Inputs(IRotateCylPull).Num, Inputs(IRotateCylPull).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }

                if (InputState(IPushCylPush, false))
                {
                    string msg = string.Format("【{0}{1}】感应器状态非ON，禁止操作前段电机！！！"
                               , Inputs(IPushCylPush).Num, Inputs(IPushCylPush).Name);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }

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
            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_RecvBatttery:
                    if (ORecvPosMotor > -1)
                        OutputAction(ORecvPosMotor, false);  // 停机，不可接收电池
                    break;
                case AutoSteps.Auto_SendBatttery:
                    if (OSendRequire > -1)
                    {
                        Def.WriteLog("RunProcessOffloadOut", "设备停止,取消电池发送请求信号");
                        OutputAction(OSendRequire, false);    // 取消发送电池信号
                    }
                    if (OSendPosMotor > -1)
                        OutputAction(OSendPosMotor, false);   // 停机，不可发送电池
                    break;
                default:
                    break;
            }
            
            base.AfterStopAction();
        }

        /// <summary>
        /// 手动设置出料位发送电池完成
        /// </summary>
        /// <param name="sendState"></param>
        /// <returns></returns>
        public bool SetOffloadOutFinish(ref int sendState)
        {
            bool IsHadBat = false;
            for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
            {
                if (this.Battery[i].Type == BatteryStatus.OK)
                {
                    IsHadBat = true;
                    break;
                }
            }
            // 没有电池，无需设置
            if (!IsHadBat)
            {
                sendState = 1;
                return false;
            }
            // 电池是否属于发送状态
            if (AutoSteps.Auto_SendBatttery != (AutoSteps)this.nextAutoStep)
            {
                sendState = 2;
                return false;
            }

            for (int i = 0; i < (int)ModDef.RecvPosAll; i++)
            {
                // 有电池不可设置发送完成
                if (this.Battery[i].Type == BatteryStatus.OK && InputState(IPlacePosInpos[i], true))
                {
                    sendState = 3;
                    return false;
                }
            }

            // 出料位置有电池不可设置发送完成
            if (InputState(IPlaceOut, true))
            {
                sendState = 4;
                return false;
            }

            return false;
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
