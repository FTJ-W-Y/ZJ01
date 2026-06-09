using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    class RunProcessSafeDoor : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_CheckSafeDoor,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 等待人工操作完成
            Auto_WaitManualOperateFinish,
            // 等待取放完成
            Auto_WaitPickPlaceFinish,

            Auto_WorkEnd,
        }

        private enum ModDef
        {

        }

        private enum MsgID
        {
            Start = ModuleMsgID.ManualOperateMsgStartID,
            HasRequireOnload,
            HasRequireOffload,
        }

        #endregion

        #region // 字段，属性

        #region // IO

        // 安全门
        private int[] ISafeDoorOpenBtn;         // 安全门开门按钮
        private int[] ISafeDoorStopBtn;         // 安全门急停按钮
        private int[] ISafeDoorState;           // 安全门状态
        
        private int[] OSafeDoorOpenLed;         // 安全门开门按钮LED
        private int[] OSafeDoorCloseLed;        // 安全门关门按钮LED
        private int[] OSafeDoorUnlock;          // 安全门解锁
        public int[] SafeDoorDelay;             // 安全门打开延时：毫秒ms

        public bool[] SafeDoorOpenBtnDown;      // 安全门开门动作
        public bool[] SafeDoorStopBtnDown;      // 安全门急停动作
        public bool[] SafeDoorOpenState;        // 安全门开状态
        /// <summary>
        /// 开门状态
        /// </summary>
        private bool openState;

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数

        private bool onloadTest;              // 上夹具模拟测试参数
        private bool offloadTest;             // 下夹具模拟测试参数

        #endregion

        #region // 模组数据

        private EventList operateEvent;       // 当前操作事件

        #endregion

        #endregion

        public RunProcessSafeDoor(int runId) : base(runId)
        {
            InitBatteryPalletSize(0, (int)ModuleMaxPallet.ManualOperate);

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
                        this.nextInitStep = InitSteps.Init_CheckSafeDoor;
                        break;
                    }
                case InitSteps.Init_CheckSafeDoor:
                    {
                        CurMsgStr("检查安全门状态", "Check sensor");
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
            if (!IsModuleEnable())
            {
                CurMsgStr("模组禁用", "Moudle not enable");
                Sleep(100);
                return;
            }

            if (Def.IsNoHardware())
            {
                Sleep(10);
            }

            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        for (int idx = 0; idx < (int)SystemIO.SafeDoorIO; idx++)
                        {
                            openState = false; // 门按钮按下 或 门磁非吸合
                            if (this.ISafeDoorState[idx] > -1)
                            {
                                openState = InputState(this.ISafeDoorState[idx], true);
                                this.SafeDoorOpenState[idx] = openState;
                            }

                            if (this.ISafeDoorOpenBtn[idx] > -1 && !openState)
                            {
                                openState = InputState(this.ISafeDoorOpenBtn[idx], true);
                                this.SafeDoorOpenBtnDown[idx] = openState;
                            }

                            //正常给信号，异常或按下不给信号
                            if (this.ISafeDoorStopBtn[idx] > -1)
                            {
                                this.SafeDoorStopBtnDown[idx] = !InputState(this.ISafeDoorStopBtn[idx], true);
                            }
                            
                            SetSafeDoorOpenLed(idx, openState);
                            SetSafeDoorCloseLed(idx, !openState);
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

        #region // 运行数据读写

        public override void InitRunData()
        {
            this.operateEvent = EventList.Invalid;

            base.InitRunData();
        }

        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            this.operateEvent = (EventList)iniStream.ReadInt(section, "operateEvent", (int)this.operateEvent);

            base.LoadRunData();
        }

        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            if (SaveType.Variables == (SaveType.Variables & saveType))
            {
                iniStream.WriteInt(section, "operateEvent", (int)this.operateEvent);
            }

            base.SaveRunData(saveType, index);
        }
        #endregion

        #region // 模组配置及参数

        /// <summary>
        /// 读取模组配置
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        public override bool InitializeConfig(string module)
        {
            if (!base.InitializeConfig(module))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.onloadTest = false;
            this.offloadTest = false;
            openState = false;

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.onloadTest = ReadBoolParameter(this.RunModule, "onloadTest", this.onloadTest);
            this.offloadTest = ReadBoolParameter(this.RunModule, "offloadTest", this.offloadTest);

            return base.ReadParameter();
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxCount = (int)SystemIO.SafeDoorIO;

            this.ISafeDoorState = new int[maxCount];
            this.ISafeDoorOpenBtn = new int[maxCount];
            this.ISafeDoorStopBtn = new int[maxCount];

            this.OSafeDoorOpenLed = new int[maxCount];
            this.OSafeDoorCloseLed = new int[maxCount];
            this.OSafeDoorUnlock = new int[maxCount];

            //this.SafeDoorStopModule = new string[maxCount];
            this.SafeDoorDelay = new int[maxCount];

            this.SafeDoorOpenBtnDown = new bool[maxCount];
            this.SafeDoorStopBtnDown = new bool[maxCount];
            this.SafeDoorOpenState = new bool[maxCount];

            string key = "";
            for (int i = 0; i < maxCount; i++)
            {
                this.ISafeDoorState[i] = -1;
                this.ISafeDoorOpenBtn[i] = -1;
                this.ISafeDoorStopBtn[i] = -1;

                this.OSafeDoorOpenLed[i] = -1;
                this.OSafeDoorCloseLed[i] = -1;
                this.OSafeDoorUnlock[i] = -1;
                
                this.SafeDoorDelay[i] = 0;

                this.SafeDoorOpenBtnDown[i] = false;
                this.SafeDoorStopBtnDown[i] = false;
                this.SafeDoorOpenState[i] = false;
                
                key = $"ISafeDoorOpenBtn{i + 1}";
                this.ISafeDoorOpenBtn[i] = AddInput(key);
                    
                key = $"ISafeDoorStop{i + 1}";
                this.ISafeDoorStopBtn[i] = AddInput(key);

                key = $"ISafeDoorState{i + 1}";
                this.ISafeDoorState[i] = AddInput(key);

                key = $"OSafeDoorOpenLed{i + 1}";
                this.OSafeDoorOpenLed[i] = AddOutput(key);

                key = $"OSafeDoorCloseLed{i + 1}";
                this.OSafeDoorCloseLed[i] = AddOutput(key);

                key = $"OSafeDoorUnlock{i + 1}";
                this.OSafeDoorUnlock[i] = AddOutput(key);

                key = $"SafeDoorDelay{i + 1}";
                this.SafeDoorDelay[i] = ReadInt(key, 500);
            }
        }
        
        #endregion

        #region // 安全门状态

        /// <summary>
        /// 安全门开状态
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool GetSafeDoorOpenDown(int idx)
        {
            if (ISafeDoorOpenBtn[idx] > -1)
            {
                bool btnDown = InputState(ISafeDoorOpenBtn[idx], true);
                if (btnDown)
                {
                    SetSafeDoorOpenLed(idx, true);
                    SetSafeDoorCloseLed(idx, false);
                }
                else
                {
                    SetSafeDoorOpenLed(idx, false);
                    SetSafeDoorCloseLed(idx, true);
                }
                return btnDown;
            }
            return false;
        }

        /// <summary>
        /// 安全门急停状态
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool SafeDoorIsStop(int idx)
        {
            // on = 正常 0ff=按下或异常
            if (ISafeDoorStopBtn[idx] > -1)
            {
                return !InputState(ISafeDoorStopBtn[idx], true);
            }
            return false;
        }
        /// <summary>
        /// 安全门锁开状态 true=开状态 false=非开状态
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool GetSafeDoorOpenState(int idx, ref int ioInput)
        {
            ioInput = ISafeDoorState[idx];
            if (ISafeDoorState[idx] > -1)
            {
                return InputState(ISafeDoorState[idx], true);
            }
            return false;
        }
        /// <summary>
        /// 是否有此门控制权限
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool HadSafeDoor(int idx)
        {
            return ISafeDoorState[idx] > -1 ? true : false;
        }

        /// <summary>
        /// 安全门解锁 true=解锁 false=上锁
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public bool SetSafeDoorUnlock(int idx, bool isOn)
        {
            if (OSafeDoorUnlock[idx] > -1)
            {
                return OutputAction(OSafeDoorUnlock[idx], isOn);
            }
            return false;
        }
        /// <summary>
        /// 开门按钮LED
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="isOn"></param>
        /// <returns></returns>
        public bool SetSafeDoorOpenLed(int idx, bool isOn)
        {
            if (OSafeDoorOpenLed[idx] > -1)
            {
                return OutputAction(OSafeDoorOpenLed[idx], isOn);
            }
            return false;
        }
        /// <summary>
        /// 关门按钮LED
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="isOn"></param>
        /// <returns></returns>
        public bool SetSafeDoorCloseLed(int idx, bool isOn)
        {
            if (OSafeDoorCloseLed[idx] > -1)
            {
                return OutputAction(OSafeDoorCloseLed[idx], isOn);
            }
            return false;
        }
        #endregion
    }
}
