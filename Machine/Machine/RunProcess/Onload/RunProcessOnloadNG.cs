using HelperLibrary;
using System;
using System.Diagnostics;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 上料NG输出线体
    /// </summary>
    class RunProcessOnloadNG : RunProcess
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

            // 传递到缓存位
            Auto_SendToBufferPos,
            // 传递到人工取料位
            Auto_SendToManualPos,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
            PlacePos_0 = 0,
            PlacePos_1,
            PlacePos_2,
            PlacePos_3,
            PlacePos_ALL,

        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadNGMsgStartID,
            BufferFull,
            SendTimeout,
        }

        #endregion

        #region // 字段，属性

        #region // IO

        private int[] IPlacePosInpos;       // 放料位到位感应器
        /// <summary>
        /// 放料位安全
        /// </summary>
        private int IPlacePosSafe;          // 放料位安全
        /// <summary>
        /// 缓存位到位
        /// </summary>
        private int IBufferPosInpos;        // 缓存位到位
        /// <summary>
        /// 放料位出
        /// </summary>
        private int IBufferPosOut;           // 缓存取料位
        /// <summary>
        /// 人工确认
        /// </summary>
        private int IManualButton;          // 人工确认

        private int OManualButtonLED;       // 人工确认LED指示灯
        private int ONGLineAlarm;           // 假电池线报警
        private int ONGLineMotor;           // 假电池线输送电机
        private int ONGBufferMotor;         // 假电池线缓存线输送电机

        private int BatterySendSeconds;
        private int BatteryBufferCnt;

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数
        #endregion

        #region // 模组数据
        private RunProcessOnloadRobot onloadRobot;
        #endregion

        #endregion

        public RunProcessOnloadNG(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.PlacePos_ALL, 0);

            PowerUpRestart();

            // 参数
            InsertVoidParameter("batterySendSeconds", "电机每次行走时间秒", "每次触发电机每次行走时间秒", BatterySendSeconds, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("batteryBufferCnt", "缓存电池个数", "触发扫码的指令", BatteryBufferCnt, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
                       
        }

        #region // 模组配置及参数

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.BatterySendSeconds = 5;
            this.BatteryBufferCnt = 3;

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.BatterySendSeconds = ReadIntParameter(this.RunModule, "batterySendSeconds", this.BatterySendSeconds);
            this.BatteryBufferCnt = ReadIntParameter(this.RunModule, "batteryBufferCnt", this.BatteryBufferCnt);
            
            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            onloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
        }

        #endregion

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
                        //if (!PlaceSenserIsSafe())
                        //{
                        //    for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
                        //    {
                        //        if(!InputState(this.IPlacePosInpos[i], false))
                        //        {
                        //            this.Battery[i].Type = BatteryStatus.NG;
                        //        }
                        //    }
                        //}
                        this.nextInitStep = InitSteps.Init_End;
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

            if(Def.IsNoHardware())
            {
                Sleep(1000);
            }

            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        this.nextAutoStep = AutoSteps.Auto_SendToBufferPos;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_SendToBufferPos:
                    {
                        CurMsgStr("接收电池信息", "Read Battery Info");
                        for (int i = 0; i < onloadRobot.onloadData.batNGSignal.Length; i++)
                        {
                            this.Battery[i].Type = onloadRobot.onloadData.batNGSignal[i].Type;
                            this.Battery[i].Code = onloadRobot.onloadData.batNGSignal[i].Code;
                        }
                        break;
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

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxPos = (int)ModDef.PlacePos_ALL;
            this.IPlacePosInpos = new int[maxPos];
            for(int i = 0; i < maxPos; i++)
            {
                this.IPlacePosInpos[i] = AddInput("IPlacePosInpos" + i);
            }
            this.IPlacePosSafe = AddInput("IPlacePosSafe");
            this.IBufferPosInpos = AddInput("IBufferPosInpos");
            this.IBufferPosOut = AddInput("IBufferPosOut ");

            this.IManualButton = AddInput("IManualButton");

            this.OManualButtonLED = AddOutput("OManualButtonLED");
            this.ONGLineAlarm = AddOutput("ONGLineAlarm");
            this.ONGLineMotor = AddOutput("ONGLineMotor");
            this.ONGBufferMotor = AddOutput("ONGBufferMotor");
        }

        /// <summary>
        /// 缓存位感应器状态
        /// </summary>
        /// <returns></returns>
        public bool BufferPosSensorState(bool isOn)
        {
            if (!InputState(IBufferPosInpos, isOn))
            {
                return false;
            }
            return true;
        }

        public bool BufferPosState()
        {
            return InputState(IBufferPosInpos, true);
        }

        /// <summary>
        /// 人工确认按钮操作状态
        /// </summary>
        /// <param name="isOn"></param>
        /// <returns></returns>
        private bool ManualButton(bool isOn)
        {
            if(Def.IsNoHardware())
            {
                return false;
            }
            if(InputState(IManualButton, isOn))
            {
                for(int i = 0; i < 5; i++)
                {
                    if(!InputState(IManualButton, isOn))
                    {
                        return false;
                    }
                    Sleep(200);
                }
                OutputAction(OManualButtonLED, isOn);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 放料位传感器状态安全
        /// </summary>
        /// <returns></returns>
        public bool PlaceSenserIsSafe()
        {
            //if(/*!InputState(IPlacePosSafe, false) ||*/ !InputState(IBufferPosOut, false))
            //{
            //    if(alm)
            //    {
            //        //CheckInputState(IPlacePosSafe, false);
            //        CheckInputState(IBufferPosOut, false);
            //    }
            //    return false;
            //}
            //return true;

            for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if (!InputState(this.IPlacePosInpos[i], false))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 放料位到位感应器安全状态
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool PlacePosInposIsSafe(int row, bool alm = true)
        {
            for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                //if (i == row)
                {
                    if (!InputState(this.IPlacePosInpos[i], false))
                    {
                        return CheckInputState(this.IPlacePosInpos[i], false);
                    }
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region // 电池数据

        /// <summary>
        /// 放料位电池为满
        /// </summary>
        /// <returns></returns>
        public bool PlacePosIsFull()
        {
            //for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            //{
            //    if (BatteryStatus.Invalid == this.Battery[i].Type)
            //    {
            //        return false;
            //    }
            //}
            //return true;

            for (int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if (BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 放料位电池为空
        /// </summary>
        /// <returns></returns>
        public bool PlacePosIsEmpty()
        {
            for(int i = 0; i < (int)ModDef.PlacePos_ALL; i++)
            {
                if(BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

    }
}
