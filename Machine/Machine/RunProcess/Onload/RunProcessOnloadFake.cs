using HelperLibrary;
using System;
using System.Diagnostics;

namespace Machine
{
    /// <summary>
    /// 上料上假电池线体
    /// </summary>
    class RunProcessOnloadFake : RunProcess
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

            // 接收电池
            Auto_RecvBatttery,
            // 传递到缓存位
            Auto_SendToBufferPos,
            // 传递到取料位
            Auto_SendToPickPos,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
            PickPos_0 = 0,
            PickPos_ALL = 4,
            // 缓存行：从取料位开始0
            Buffer_Col_0 = PickPos_ALL,
            Buffer_Col_1 = Buffer_Col_0 + PickPos_ALL,
            Buffer_Col_2 = Buffer_Col_1 + PickPos_ALL,
            PickPos_Buffer_ALL = Buffer_Col_2 + PickPos_ALL,
            
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadFakeMsgStartID,
            SendBufPosTimeout,
            SendPickPosTimeout,
        }

        #endregion

        #region // 字段，属性

        #region // IO

        private int[] IPlacePosInpos;       // 放料位到位
        private int[] IPickPosInpos;        // 取料位到位
        private int IPickPosEnter;          // 取料位进入
        private int IBufferPosSafe;         // 放料位安全-假电池已分离
        private int IBufferPosLeave;        // 缓存位离开-进入取料位
        private int IManualButton;          // 人工确认

        private int OManualButtonLED;       // 人工确认LED指示灯
        private int OFakeLineAlarm;         // 假电池线报警
        private int OFakePickMotor;         // 假电池取料线输送电机
        private int OFakeBufferMotor;       // 假电池缓存线输送电机

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

        public RunProcessOnloadFake(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.PickPos_Buffer_ALL, 0);

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
                        //if(!CheckInputState(IPickPosEnter, false))
                        //{
                        //    return;
                        //}
                        //for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
                        //{
                        //    if(InputState(IPickPosInpos[i], true))
                        //    {
                        //        this.Battery[i].Type = BatteryStatus.Fake;
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
                Sleep(10);
            }
            
            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        this.nextAutoStep = AutoSteps.Auto_RecvBatttery;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_RecvBatttery:
                    {
                        CurMsgStr("接收电池信息", "Read Battery Info");
                        for (int i = 0; i < onloadRobot.onloadData.fakeSignal.Length; i++)
                        {
                            this.Battery[i].Type = onloadRobot.onloadData.fakeSignal[i].Type;
                            this.Battery[i].Code = onloadRobot.onloadData.fakeSignal[i].Code;
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

        public override void ManulSetAutoStep(int step)
        {
            this.nextAutoStep = (AutoSteps)step;
            SaveRunData(SaveType.AutoStep);
        }

        #endregion

        #region // 模组配置及参数

        protected override void InitParameter()
        {

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
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            onloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxPos = (int)ModDef.PickPos_ALL;
            this.IPickPosInpos = new int[maxPos];
            for (int i = 0; i < maxPos; i++)
			{
                this.IPickPosInpos[i] = AddInput("IPickPosInpos" + i);
            }
            this.IPickPosEnter = AddInput("IPickPosEnter");
            this.IPlacePosInpos = new int[maxPos];
            for(int i = 0; i < maxPos; i++)
            {
                this.IPlacePosInpos[i] = AddInput("IPlacePosInpos" + i);
            }
            this.IBufferPosSafe = AddInput("IBufferPosSafe");
            this.IBufferPosLeave = AddInput("IBufferPosLeave");
            this.IManualButton = AddInput("IManualButton");

            this.OManualButtonLED = AddOutput("OManualButtonLED");
            this.OFakeLineAlarm = AddOutput("OFakeLineAlarm");
            this.OFakePickMotor = AddOutput("OFakePickMotor");
            this.OFakeBufferMotor = AddOutput("OFakeBufferMotor");
        }

        /// <summary>
        /// 电池感应器到位
        /// </summary>
        /// <returns></returns>
        public bool PickPosSenserInpos(bool alm = false)
        {
            if(InputState(IPickPosEnter, false))
            {
                for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
                {
                    bool hasBat = this.Battery[i].Type > BatteryStatus.Invalid;
                    if(!InputState(IPickPosInpos[i], hasBat))
                    {
                        if(alm) CheckInputState(IPickPosInpos[i], hasBat);
                        return false;
                    }
                }
                return true;
            }
            if(alm) CheckInputState(IPickPosEnter, false);
            return false;
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
                return true;
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
                OutputAction(this.OManualButtonLED, isOn);
                return true;
            }
            return false;
        }

        #endregion

        #region // 电池数据

        /// <summary>
        /// 取料位电池为满
        /// </summary>
        /// <returns></returns>
        private bool PickPosIsFull()
        {
            for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
            {
                if(BatteryStatus.Invalid == this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 取料位电池为空
        /// </summary>
        /// <returns></returns>
        private bool PickPosIsEmpty()
        {
            for(int i = 0; i < (int)ModDef.PickPos_ALL; i++)
            {
                if(BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 缓存位电池为满
        /// </summary>
        /// <returns></returns>
        private bool BufferPosIsFull(ModDef col = ModDef.Buffer_Col_0)
        {
            int start = (int)col;
            int end = (int)col + (int)ModDef.PickPos_ALL;
            for(int i = start; i < end; i++)
            {
                if(BatteryStatus.Invalid == this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 缓存位电池为空
        /// </summary>
        /// <returns></returns>
        private bool BufferPosIsEmpty(ModDef col = ModDef.Buffer_Col_0)
        {
            int start = (int)col;
            int end = (int)col + (int)ModDef.PickPos_ALL;
            for(int i = start; i < end; i++)
            {
                if(BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 计算取假电池列：固定首行取
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool GetPickFakeRow(ref int row)
        {
            for(ModDef i = ModDef.PickPos_0; i < ModDef.PickPos_ALL; i++)
            {
                if(BatteryStatus.Fake == this.Battery[(int)i].Type)
                {
                    row = (int)i;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region // 防呆检查

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
            if (InputState(IManualButton, true))
            {
                OutputAction(OFakeLineAlarm, false);
            }
        }
        
        #endregion

    }
}
