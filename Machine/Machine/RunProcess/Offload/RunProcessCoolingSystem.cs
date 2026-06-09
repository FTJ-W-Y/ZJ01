using HelperLibrary;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 冷却系统
    /// </summary>
    class RunProcessCoolingSystem : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_MotorRHome,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            Auto_SendToNextCol,
            Auto_SendEndCheckSensor,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
        }

        private enum MsgID
        {
            Start = ModuleMsgID.CoolingSystemMsgStartID,
            FirstColUnsafe,
            LastColUnsafe,
            OffloadBatteryMotorZUnsafe,
            CoolingOffloadMotorZUnsafe,
            MotorRMoveFail,
            MotorRDelay,
            MotorRTimeout,
            IMotorRInposErr,
            GetRPosErr,
            RMoveRowMaxDis,
            SafeDoorOpenEStop
        }

        #endregion

        #region // 字段，属性

        #region // IO

        /// <summary>
        /// 首列放位置安全：无电池 
        /// </summary>
        private int IFirstColSafe;          // 首列放位置安全：无电池
        /// <summary>
        /// 首列放位置离开
        /// </summary>
        //private int IFirstColLeave;         // 首列放位置离开
        /// <summary>
        /// 末位取位置到位：有电池 
        /// </summary>
        private int ILastColInpos;          // 末位取位置到位：有电池
        /// <summary>
        /// 末位取位置进入
        /// </summary>
        //private int ILastColEnter;          // 末位取位置进入
        /// <summary>
        /// 电机R取放料位到位：链条大格检测
        /// </summary>
        private int IMotorRInpos;           // 电机R取放料位到位：链条大格检测
        /// <summary>
        /// 电机R防呆到位：链条大格检测后的防撞安全检测
        /// </summary>
        private int IMotorRDelay;           // 电机R防呆到位：链条大格检测后的防撞安全检测

        /// <summary>
        /// 电机R上电
        /// </summary>
        private int OMororRPowerON;         // 电机R上电
        /// <summary>
        /// 电机R正转
        /// </summary>
        private int OMororRForwardMove;     // 电机R正转
        /// <summary>
        /// 电机R反转
        /// </summary>
        private int OMororRBackwardMove;    // 电机R反转
        /// <summary>
        /// 风机
        /// </summary>
        private int[] OAirBlower;           // 风机

        private int OMotorMove;  //电机旋转

        #endregion

        #region // 电机

        /// <summary>
        /// 旋转电机R：扭矩模式移动
        /// </summary>
        private int MotorR;     // 旋转电机R：扭矩模式移动

        DateTime MotorMoveToNextColTime = DateTime.Now;
        #endregion

        #region // ModuleEx.cfg配置

        #endregion

        #region // 模组参数

        /// <summary>
        /// 冷却系统最大行
        /// </summary>
        private int stationRow;         // 冷却系统最大行
        /// <summary>
        /// 冷却系统最大列
        /// </summary>
        private int stationCol;         // 冷却系统最大列
        /// <summary>
        /// 冷却时间
        /// </summary>
        private int coolTime;           // 冷却时间
        /// <summary>
        /// 等待放料的时间：超时后自动移动一列
        /// </summary>
        private int waitPlaceTimeout;   // 等待放料的时间：超时后自动移动一列
        /// <summary>
        /// 冷却系统R轴扭矩
        /// </summary>
        private int motorRTorque;       // 冷却系统R轴扭矩
        /// <summary>
        /// 移动一行时能旋转的最大距离
        /// </summary>
        private double moveRowMaxDis;   // 移动一行时能旋转的最大距离

        private bool IsSendToNextCol;     // 发送下一行

        #endregion

        #region // 模组数据

        DateTime waitPlaceStartTime;

        RunProcessOffloadBattery offloadBattery;
        RunProcessCoolingOffload coolingOffload;

        #endregion

        #endregion

        public RunProcessCoolingSystem(int runId) : base(runId)
        {
            InitParameter();

            // 参数
            string key;
            key = string.Format("冷却系统最大行：（0 < X ≤ {0}）", (int)BatteryLineRowCol.MaxRow);
            InsertVoidParameter("stationRow", "冷却系统行", key, stationRow, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);
            key = string.Format("冷却系统最大列：（0 < X ≤ {0}）", (int)BatteryLineRowCol.MaxCol);
            InsertVoidParameter("stationCol", "冷却系统列", key, stationCol, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);
            key = $"冷却时间(分钟min)：电池过冷却炉总时间";
            InsertVoidParameter("coolTime", "冷却时间", key, coolTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            key = $"冷却系统R轴扭矩(牛)：移动R轴时的扭矩值，不能过大，防止一直移动撞坏硬件；不能太小，否则推不动电池";
            InsertVoidParameter("motorRTorque", "R轴扭矩", key, motorRTorque, RecordType.RECORD_INT, ParameterLevel.PL_STOP_ADMIN);
            key = $"冷却系统行间距防呆(毫米mm)：移动一行能旋转的最大距离，防止一直移动撞坏硬件";
            InsertVoidParameter("moveRowMaxDis", "行间距防呆", key, moveRowMaxDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_ADMIN);

            InitBatteryPalletSize(0, 0, 1);

            PowerUpRestart();
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
                        this.waitPlaceStartTime = DateTime.Now; //这里重置开始时间，防止停机重开机时间超时冷却直接前推
                        this.nextInitStep = InitSteps.Init_MotorRHome;
                        break;
                    }
                case InitSteps.Init_MotorRHome:
                    {
                        CurMsgStr("冷却系统电机回零", "Motor R home");
                        if (MotorRHome())
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
                    Trace.Assert(false, "RunProcess.InitOperation/no this init step");
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

            // 冷风机刀闸开关
            AirBlowerOperator();

            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        #region // 设置检查取放请求
                        
                        // 首行非满请求放
                        EventList placeEvt = EventList.CoolingSystemPlaceBattery;
                        EventStatus placestate = GetEvent(this, placeEvt);
                        if (((EventStatus.Invalid == placestate) || (EventStatus.Finished == placestate) || (EventStatus.Cancel == placestate))
                            && !IsFullCol(0))
                        {
                            SetEvent(this, placeEvt, EventStatus.Require);
                        }
                        // 末行非空请求取
                        EventList pickEvt = EventList.CoolingSystemPickBattery;
                        EventStatus pickState = GetEvent(this, pickEvt);
                        if (((EventStatus.Invalid == pickState) || (EventStatus.Finished == pickState))
                            && !IsEmptyCol(this.BatteryLine.MaxCol - 1)
                            && (this.DryRun || CheckInputState(this.ILastColInpos, true)))
                        {
                            SetEvent(this, pickEvt, EventStatus.Require);
                        }
                        #endregion

                        #region // 有取放进行中
                        bool IsContinue = true;
                        for (EventList idx = EventList.CoolingSystemPlaceBattery; idx < EventList.CoolingSystemPickPlaceEnd; idx++)
                        {
                            pickState = GetEvent(this, idx);
                            if (EventStatus.Response == pickState)
                            {
                                if (EventList.CoolingSystemPlaceBattery == idx)
                                    this.waitPlaceStartTime = DateTime.Now;

                                SetEvent(this, idx, EventStatus.Ready);
                                IsContinue = false;
                            }
                            else if ((EventStatus.Ready == pickState) || (EventStatus.Start == pickState))
                            {
                                if (IsEmptyCol(this.BatteryLine.MaxCol - 1) && EventList.CoolingSystemPickBattery == idx)
                                {
                                    SetEvent(this, EventList.CoolingSystemPickBattery, EventStatus.Finished);
                                }
                                IsContinue = false;
                            }
                        }
                        if (!IsContinue)
                        {
                            return;
                        }
                        #endregion

                        // 首满末空，有电池，移动
                        if (((EventStatus.Invalid == pickState) || (EventStatus.Finished == pickState))
                            && !this.BatteryLine.IsEmpty() && IsEmptyCol(this.BatteryLine.MaxCol - 1))
                        {
                            IsSendToNextCol = false;
                            //满行，夹具电池已经空且超时,如果正在下料，则不需要按往前一行时间实推进
                            if ((EventStatus.Invalid == placestate) || (EventStatus.Finished == placestate) || (EventStatus.Require == placestate))
                            {
                                // 首行满行
                                if (IsFullCol(0))
                                {
                                    IsSendToNextCol = true;
                                }
                                else if (OfflineBatEmpty())
                                {
                                    // 下料夹具空 末2行无料
                                    if ((IsEmptyCol(this.BatteryLine.MaxCol - 2) && (DateTime.Now - this.waitPlaceStartTime).TotalSeconds > this.waitPlaceTimeout))
                                    {
                                        IsSendToNextCol = true;
                                    }
                                    // 下料夹具空 末2行有料
                                    else if (!IsEmptyCol(this.BatteryLine.MaxCol - 2) && (DateTime.Now - this.waitPlaceStartTime).TotalSeconds > 20)
                                    {
                                        IsSendToNextCol = true;
                                    }
                                }
                            }
                            else if (EventStatus.Cancel == placestate)
                            {
                                IsSendToNextCol = true;
                            }

                            if (IsSendToNextCol)
                            {
                                this.waitPlaceStartTime = DateTime.Now;

                                this.nextAutoStep = AutoSteps.Auto_SendToNextCol;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        // 超时太久则重新计时
                        //if ((DateTime.Now - this.waitPlaceStartTime).TotalSeconds > this.waitPlaceTimeout)
                        //{
                        //    this.waitPlaceStartTime = DateTime.Now;
                        //}
                        // 取消清尾
                        if (MachineCtrl.GetInstance().OffloadClear && OfflineBatEmpty() && !FignerHadBattery())
                        {
                            MachineCtrl.GetInstance().OffloadClear = false;
                        }
                        break;
                    }
                case AutoSteps.Auto_SendToNextCol:
                    {
                        CurMsgStr("向后传递电池", "Send battery to next col");
                        MotorMoveToNextColTime = DateTime.Now;

                        if (offloadBattery.CheckMotorZPos(MotorPosition.OffLoad_SafetyPos) 
                            && coolingOffload.CheckMotorZPos(MotorPosition.CoolingOffload_SafetyPos))
                        {
                            if (MotorRMove(false))
                            {
                                for (int col = this.BatteryLine.MaxCol - 1; col > 0; col--)
                                {
                                    for (int row = 0; row < this.BatteryLine.MaxRow; row++)
                                    {
                                        this.BatteryLine.Battery[row, col].Copy(this.BatteryLine.Battery[row, col - 1]);
                                        this.BatteryLine.Battery[row, col - 1].Release();
                                    }
                                }
                                this.nextAutoStep = AutoSteps.Auto_SendEndCheckSensor;
                                SaveRunData(SaveType.AutoStep | SaveType.Battery);
                            }
                        }
                        
                        break;
                    }
                case AutoSteps.Auto_SendEndCheckSensor:
                    {
                        CurMsgStr("传递电池后检查感应器", "Send end and check sensor status");
                        //if (this.DryRun || (CheckInputState(IFirstColSafe, false) && CheckInputState(IFirstColLeave, false)
                        //    && CheckInputState(ILastColEnter, false) && CheckInputState(ILastColInpos, !IsEmptyCol(this.stationCol - 1))))
                        //{
                        if (this.DryRun || (CheckInputState(IFirstColSafe, false) 
                            && CheckInputState(ILastColInpos, !IsEmptyCol(this.stationCol - 1))))
                        {
                            SetEvent(this, EventList.CoolingSystemPlaceBattery, EventStatus.Invalid);
                            SetEvent(this, EventList.CoolingSystemPickBattery, EventStatus.Invalid);

                            this.waitPlaceStartTime = DateTime.Now;
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
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
        
        /// <summary>
        /// 初始化运行数据
        /// </summary>
        public override void InitRunData()
        {
  
            if (null != this.BatteryLine)
            {
                this.BatteryLine.Release();
            }

            base.InitRunData();
        }

        /// <summary>
        /// 读取运行数据
        /// </summary>
        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            key = string.Format("BatteryLine.MaxRow");
            this.BatteryLine.MaxRow = iniStream.ReadInt(section, key, this.BatteryLine.MaxRow);
            key = string.Format("BatteryLine.MaxCol");
            this.BatteryLine.MaxCol = iniStream.ReadInt(section, key, this.BatteryLine.MaxCol);
            for(int row = 0; row < this.BatteryLine.MaxRow; row++)
            {
                for(int col = 0; col < this.BatteryLine.MaxCol; col++)
                {
                    key = string.Format("BatteryLine.Battery[{0}, {1}].Type", row, col);
                    this.BatteryLine.Battery[row, col].Type = (BatteryStatus)iniStream.ReadInt(section, key, (int)this.BatteryLine.Battery[row, col].Type);
                    key = string.Format("BatteryLine.Battery[{0}, {1}].NGType", row, col);
                    this.BatteryLine.Battery[row, col].NGType = (BatteryNGStatus)iniStream.ReadInt(section, key, (int)this.BatteryLine.Battery[row, col].NGType);
                    iniStream.WriteInt(section, key, (int)this.BatteryLine.Battery[row, col].NGType);
                    key = string.Format("BatteryLine.Battery[{0}, {1}].Code", row, col);
                    this.BatteryLine.Battery[row, col].Code = iniStream.ReadString(section, key, this.BatteryLine.Battery[row, col].Code);
                }
            }
            base.LoadRunData();
        }

        /// <summary>
        /// 保存运行数据
        /// </summary>
        /// <param name="saveType"></param>
        /// <param name="index"></param>
        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            if(SaveType.Battery == (SaveType.Battery & saveType))
            {
                key = string.Format("BatteryLine.MaxRow");
                iniStream.WriteInt(section, key, this.BatteryLine.MaxRow);
                key = string.Format("BatteryLine.MaxCol");
                iniStream.WriteInt(section, key, this.BatteryLine.MaxCol);
                for(int row = 0; row < this.BatteryLine.MaxRow; row++)
                {
                    for(int col = 0; col < this.BatteryLine.MaxCol; col++)
                    {
                        key = string.Format("BatteryLine.Battery[{0}, {1}].Type", row, col);
                        iniStream.WriteInt(section, key, (int)this.BatteryLine.Battery[row, col].Type);
                        key = string.Format("BatteryLine.Battery[{0}, {1}].NGType", row, col);
                        iniStream.WriteInt(section, key, (int)this.BatteryLine.Battery[row, col].NGType);
                        key = string.Format("BatteryLine.Battery[{0}, {1}].Code", row, col);
                        iniStream.WriteString(section, key, this.BatteryLine.Battery[row, col].Code);
                    }
                }
            }
            base.SaveRunData(saveType, index);
        }
        
        #endregion

        #region // 模组配置及参数

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.stationRow = (int)BatteryLineRowCol.MaxRow;
            this.stationCol = (int)BatteryLineRowCol.MaxCol;
            this.waitPlaceStartTime = DateTime.Now;
            this.coolTime = 20;
            this.IsSendToNextCol = false;
        }
        
        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.stationRow = ReadIntParameter(this.RunModule, "stationRow", this.stationRow);
            this.stationCol = ReadIntParameter(this.RunModule, "stationCol", this.stationCol);
            if ((this.stationRow > 0) && (this.stationRow <= (int)BatteryLineRowCol.MaxRow)
                && (this.stationCol > 0) && (this.stationCol <= (int)BatteryLineRowCol.MaxCol))
            {
                if((this.stationRow != this.BatteryLine.MaxRow) || (this.stationCol != this.BatteryLine.MaxCol))
                {
                    this.BatteryLine.SetRowCol(stationRow, stationCol);
                }
            }
            this.coolTime = ReadIntParameter(this.RunModule, "coolTime", this.coolTime);
            this.motorRTorque = ReadIntParameter(this.RunModule, "motorRTorque", this.motorRTorque);
            this.moveRowMaxDis = ReadDoubleParameter(this.RunModule, "moveRowMaxDis", this.moveRowMaxDis);
            this.waitPlaceTimeout = coolTime * 60 / 10;
            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组相关得模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            this.offloadBattery = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery;

            this.coolingOffload = MachineCtrl.GetInstance().GetModule(RunID.CoolingOffload) as RunProcessCoolingOffload;
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            this.IFirstColSafe = AddInput("IFirstColSafe");
            //this.IFirstColLeave = AddInput("IFirstColLeave");
            this.ILastColInpos = AddInput("ILastColInpos");
            //this.ILastColEnter = AddInput("ILastColEnter");
            this.IMotorRInpos = AddInput("IMotorRInpos");
            this.IMotorRDelay = AddInput("IMotorRDelay");

            this.OMororRPowerON = AddOutput("OMororRPowerON");
            this.OMororRForwardMove = AddOutput("OMororRForwardMove");
            this.OMororRBackwardMove = AddOutput("OMororRBackwardMove");

            this.OAirBlower = new int[2];
            for(int i = 0; i < 2; i++)
            {
                this.OAirBlower[i] = AddOutput("OAirBlower" + i);
            }
            this.MotorR = AddMotor("MotorR");

            this.OMotorMove = AddOutput("OMotorMove");
        }
        
        #endregion

        #region // 电池状态

        public bool IsEmptyCol(int col)
        {
            for (int row = 0; row < this.BatteryLine.MaxRow; row++)
            {
                if (BatteryStatus.Invalid != this.BatteryLine.Battery[row, col].Type)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsFullCol(int col)
        {
            // 有连续2个空位，即非满
            //for(int row = 0; row < this.BatteryLine.MaxRow - 1; row++)
            //{
            //    if((BatteryStatus.Invalid == this.BatteryLine.Battery[row, col].Type)
            //        && (BatteryStatus.Invalid == this.BatteryLine.Battery[row + 1, col].Type))
            //    {
            //        return false;
            //    }
            //}
            //-> 改为判断末尾一个是否OK
            int row = this.BatteryLine.MaxRow - 1;
            if (BatteryStatus.Invalid == this.BatteryLine.Battery[row, col].Type)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 冷却炉内是否有电池
        /// </summary>
        /// <returns></returns>
        public bool IsEmptyBatteryLine()
        {
            for (int row = 0; row < this.BatteryLine.MaxRow - 1; row++)
            {
                for (int col = 0; col < this.BatteryLine.MaxCol - 1; col++)
                {
                    if ((BatteryStatus.Invalid != this.BatteryLine.Battery[row, col].Type))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 添加夹具电池
        /// </summary>
        /// <param name="curRow"></param>
        /// <param name="curCol"></param>
        /// <param name="isAdd"></param>
        public void OffLoadAddPatBats(int curRow, int curCol, bool isAdd)
        {
            {
                System.Random rnd = new System.Random();
                if (isAdd && string.IsNullOrEmpty(this.BatteryLine.Battery[curRow, curCol].Code))
                {
                    this.BatteryLine.Battery[curRow, curCol].Code = $"CODE{rnd.Next(100000000, 900000000)}T{rnd.Next(100000000, 900000000)}";
                    this.BatteryLine.Battery[curRow, curCol].Type = BatteryStatus.OK;
                    this.BatteryLine.Battery[curRow, curCol].NGType = BatteryNGStatus.Invalid;
                }

                if (!isAdd && !string.IsNullOrEmpty(this.BatteryLine.Battery[curRow, curCol].Code))
                {
                    this.BatteryLine.Battery[curRow, curCol].Code = "";
                    this.BatteryLine.Battery[curRow, curCol].Type = BatteryStatus.Invalid;
                    this.BatteryLine.Battery[curRow, curCol].NGType = BatteryNGStatus.Invalid;
                }
            }
        }
        
        /// <summary>
        /// 电池冷风开关操作
        /// </summary>
        void AirBlowerOperator()
        {
            if (!IsEmptyBatteryLine())
            {
                for (int i = 0; i < OAirBlower.Length; i++)
                {
                    OutputAction(OAirBlower[i], true);
                }
            }
            else
            {
                for (int i = 0; i < OAirBlower.Length; i++)
                {
                    OutputAction(OAirBlower[i], false);
                }
            }
        }

        /// <summary>
        /// 冷却上料夹具电池是否为空
        /// </summary>
        /// <returns></returns>
        bool OfflineBatEmpty()
        {
            foreach (var itm in this.offloadBattery.Pallet)
            {
                for (int i = 0; i < itm.MaxRow; i++)
                {
                    for (int j = 0; j < itm.MaxCol; j++)
                    {
                        if (!string.IsNullOrEmpty(itm.Battery[i,j].Code) && itm.Battery[i, j].Type == BatteryStatus.OK)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 夹爪/缓存是否有电池
        /// </summary>
        /// <returns></returns>
        bool FignerHadBattery()
        {
            for (int i = 0; i < this.Battery.Length; i++)
            {
                if (BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region // 旋转电机操作

        /// <summary>
        /// 旋转电机回零
        /// </summary>
        /// <returns></returns>
        public bool MotorRHome()
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            if (InputState(this.IFirstColSafe, true) || InputState(IMotorRInpos, true))
            {
                return true;
            }

            if (InputState(this.IMotorRInpos, true))
            {
                return true;
            }

            // 链条大格检测到位时定为零点
            return RMoveServo(true);
        }

        public bool MotorStop()
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            int ret = Motors(this.MotorR).SetSvon(false);
            return true;
        }
      
        /// <summary>
        /// 旋转电机旋转移动一行
        /// </summary>
        /// <returns></returns>
        public bool MotorRMove(bool forward)
        {
           // return true;

            return RMoveServo(forward);
        }

        /// <summary>
        /// 旋转伺服电机
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
        private bool RMoveServo(bool forward, bool autoRun=true)
        {
            if (Def.IsNoHardware())
                return true;

            bool canMove = true;
            bool result = false;
            MsgID msgID = MsgID.Start;

            #region // 检查能否旋转移动

            // 链条大格检测感应器必需有
            if (this.IMotorRInpos < 0)
            {
                canMove = false;
                msgID = MsgID.IMotorRInposErr;
            }
            //向后移动-放料位置有电池(向冷却上料方向推)
            if (canMove && forward && InputState(this.IFirstColSafe, true))
            {
                canMove = false;
                msgID = MsgID.FirstColUnsafe;
            }
            //向前移动-尾行有电池(向冷却下料方向推)
            if (canMove && !forward && InputState(this.ILastColInpos, true))
            {
                canMove = false;
                msgID = MsgID.LastColUnsafe;
            }
            if (canMove)
            {
                DateTime dateTime = DateTime.Now;
                while (true)
                {
                    canMove = true;
                    RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery);
                    if (null != run)
                    {
                        if (!((RunProcessOffloadBattery)run).CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
                        {
                            canMove = false;
                            msgID = MsgID.OffloadBatteryMotorZUnsafe;
                        }
                    }
                    run = MachineCtrl.GetInstance().GetModule(RunID.CoolingOffload);
                    if (null != run)
                    {
                        if (!((RunProcessCoolingOffload)run).CheckMotorZPos(MotorPosition.CoolingOffload_SafetyPos))
                        {
                            canMove = false;
                            msgID = MsgID.CoolingOffloadMotorZUnsafe;
                        }
                    }
                    // wjj 220429 增加状态判断,防止单次判断，设备还在运行导致，冷却推动无法向前
                    if (canMove)
                    {
                        msgID = MsgID.Start;
                        break;
                    }
                    if ((int)(DateTime.Now - dateTime).TotalSeconds > 20)
                    {
                        break;
                    }
                    Sleep(1);
                }
            }
            #endregion
            canMove = true;
            
            #region // 旋转移动
            if (canMove)
            {
                bool RInpos = false;
                if (InputState(IMotorRInpos, true))
                {
                    RInpos = true; //从安全大格开始移动
                }

                Motors(this.MotorR).Reset();
                Motors(this.MotorR).SetSvon(true);
                Motors(this.MotorR).SetPos(0);
                Motors(this.MotorR).TorqueMove(forward, motorRTorque);
                float curPos = 0;
                DateTime time = DateTime.Now;
                int ts = 0;
                while (canMove)
                {
                    //安全大格到位信号消失检测
                    ts = (int)(DateTime.Now - time).TotalSeconds;
                    if (RInpos && !InputState(IMotorRInpos, true))
                    {
                        RInpos = false; //离开安全大格
                    }
                    else if (!RInpos && InputState(IMotorRInpos, true))
                    {
                        result = true;
                        break;
                    }
                        
                    if ((int)MotorCode.MotorOK == Motors(this.MotorR).GetCurPos(ref curPos))
                    {
                        if (Math.Abs(curPos) > this.moveRowMaxDis)
                        {
                            canMove = false;
                            msgID = MsgID.RMoveRowMaxDis;
                            break;
                        }
                    }
                    else
                    {
                        canMove = false;
                        msgID = MsgID.GetRPosErr;
                        break;
                    }
                    if ((DateTime.Now - time).TotalSeconds > 60)
                    {
                        canMove = false;
                        msgID = MsgID.MotorRTimeout;
                        break;
                    }
                    Sleep(1);
                }
                Motors(this.MotorR).TorqueMove(forward, 0);
            }
            #endregion
            

            #region // 失败报警
            string msg, dispose;
            switch (msgID)
            {
                case MsgID.FirstColUnsafe:
                    msg = string.Format("冷却系统首行有电池，冷却旋转电机不能开始移动");
                    dispose = string.Format("请在首行无电池后再操作");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.LastColUnsafe:
                    msg = string.Format("冷却系统最后一行有电池，冷却旋转电机不能开始移动");
                    dispose = string.Format("请在最后一行无电池后再操作");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.OffloadBatteryMotorZUnsafe:
                    msg = string.Format("冷却上料Z轴电机不在安全位，冷却旋转电机不能开始移动");
                    dispose = string.Format("请人工确认安全后将电机移动至安全位");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.CoolingOffloadMotorZUnsafe:
                    msg = string.Format("冷却下料Z轴电机不在安全位，冷却旋转电机不能开始移动");
                    dispose = string.Format("请人工确认安全后将电机移动至安全位");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.MotorRMoveFail:
                    msg = string.Format("电机开始移动超时，还未检测到{0} {1}为ON状态", Inputs(IMotorRDelay).Num, Inputs(IMotorRDelay).Name);
                    dispose = string.Format("请人工检查电机，确认后将电机移动至到位感应器位置");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.MotorRDelay:
                    msg = string.Format("电机移动时，已触碰{0} {1}为ON状态，但是还未检测到{2} {3}为ON状态"
                        , Inputs(IMotorRDelay).Num, Inputs(IMotorRDelay).Name, Inputs(IMotorRInpos).Num, Inputs(IMotorRInpos).Name);
                    dispose = string.Format("请人工检查电机，确认后将电机移动至到位感应器位置");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.MotorRTimeout:
                    msg = string.Format("电机移动超时，但是还未检测到{0} {1}为ON状态", Inputs(IMotorRInpos).Num, Inputs(IMotorRInpos).Name);
                    dispose = string.Format("请人工检查电机，确认后将电机移动至到位感应器位置");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.IMotorRInposErr:
                    msg = string.Format("未配置链条大格检测，不能判断旋转电机到位");
                    dispose = string.Format("请人工检查，确认后重新复位启动");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.GetRPosErr:
                    msg = string.Format("获取 {0} 当前位置出错", Motors(this.MotorR).Name);
                    dispose = string.Format("请人工检查，确认后重新复位启动");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                case MsgID.RMoveRowMaxDis:
                    msg = string.Format("{0} 当前实际移动超过设定行间距防呆距离 {1}mm"
                        , Motors(this.MotorR).Name, this.moveRowMaxDis);
                    dispose = string.Format("请人工检查，确认后重新复位启动");
                    ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
                    break;
                default:
                    break;
            }
            #endregion
            return result;
        }

        /// <summary>
        /// 旋转步进电机：IO控制
        /// </summary>
        /// <param name="forward"></param>
        /// <returns></returns>
        //private bool RMoveIO(bool forward)
        //{
        //    bool canMove = true;
        //    bool result = false;
        //    bool checkMororInpos = false;
        //    MsgID msgID = MsgID.Start;

        //    #region // 检查能否旋转移动

        //    if (this.IMotorRInpos < 0)
        //    {
        //        canMove = false;
        //        msgID = MsgID.IMotorRInposErr;
        //    }
        //    if (canMove && !forward && !InputState(this.IFirstColSafe, false))
        //    {
        //        canMove = false;
        //        msgID = MsgID.FirstColUnsafe;
        //    }
        //    if (canMove && forward && !InputState(this.ILastColInpos, false))
        //    {
        //        canMove = false;
        //        msgID = MsgID.LastColUnsafe;
        //    }
        //    if (canMove)
        //    {
        //        RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery);
        //        if (null != run)
        //        {
        //            if (!((RunProcessOffloadBattery)run).CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
        //            {
        //                canMove = false;
        //                msgID = MsgID.OffloadBatteryMotorZUnsafe;
        //            }
        //        }
        //        run = MachineCtrl.GetInstance().GetModule(RunID.CoolingOffload);
        //        if (null != run)
        //        {
        //            if (!((RunProcessCoolingOffload)run).CheckMotorZPos(MotorPosition.CoolingOffload_SafetyPos))
        //            {
        //                canMove = false;
        //                msgID = MsgID.CoolingOffloadMotorZUnsafe;
        //            }
        //        }
        //    }
        //    #endregion

        //    #region // 旋转移动
        //    OutputAction(OMororRPowerON, true);
        //    OutputAction((forward ? OMororRBackwardMove : OMororRForwardMove), false);
        //    DateTime time = DateTime.Now;
        //    if(InputState(this.IMotorRInpos, true))
        //    {
        //        OutputAction((forward ? OMororRForwardMove : OMororRBackwardMove), true);
        //        while(canMove)
        //        {
        //            // 电机移动到防呆位
        //            if(InputState(this.IMotorRDelay, true))
        //            {
        //                break;
        //            }
        //            // 超时
        //            if((DateTime.Now - time).TotalSeconds > 10)
        //            {
        //                msgID = MsgID.MotorRTimeout;
        //                canMove = false;
        //                break;
        //            }
        //            Sleep(1);
        //        }
        //    }
        //    time = DateTime.Now;
        //    while(canMove)
        //    {
        //        OutputAction((forward ? OMororRForwardMove : OMororRBackwardMove), true);
        //        // 开始移动
        //        if(!checkMororInpos)
        //        {
        //            // 开始
        //            if(InputState(this.IMotorRInpos, false) || InputState(this.IMotorRDelay, true))
        //            {
        //                checkMororInpos = true;
        //            }
        //            // 超时
        //            if((DateTime.Now - time).TotalSeconds > 2)
        //            {
        //                msgID = MsgID.MotorRMoveFail;
        //                break;
        //            }
        //            Sleep(1);
        //            continue;
        //        }
        //        // 电机移动到位
        //        if(InputState(this.IMotorRInpos, true))
        //        {
        //            result = true;
        //            break;
        //        }
        //        // 电机防呆感应器
        //        if(InputState(this.IMotorRDelay, true) && ((DateTime.Now - time).TotalSeconds > 5))
        //        {
        //            msgID = MsgID.MotorRDelay;
        //            break;
        //        }
        //        // 超时
        //        if((DateTime.Now - time).TotalSeconds > 30)
        //        {
        //            msgID = MsgID.MotorRTimeout;
        //            break;
        //        }
        //        Sleep(1);
        //    }
        //    OutputAction(OMororRForwardMove, false);
        //    OutputAction(OMororRBackwardMove, false);
        //    #endregion

        //    #region // 失败报警
        //    string msg, dispose;
        //    switch(msgID)
        //    {
        //        case MsgID.FirstColUnsafe:
        //            msg = string.Format("冷却系统首行有电池，冷却旋转电机不能开始移动");
        //            dispose = string.Format("请在首行无电池后再操作");
        //            ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
        //            break;
        //        case MsgID.LastColUnsafe:
        //            msg = string.Format("冷却系统最后一行有电池，冷却旋转电机不能开始移动");
        //            dispose = string.Format("请在最后一行无电池后再操作");
        //            ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
        //            break;
        //        case MsgID.OffloadBatteryMotorZUnsafe:
        //            msg = string.Format("冷却上料Z轴电机不在安全位，冷却旋转电机不能开始移动");
        //            dispose = string.Format("请人工确认安全后将电机移动至安全位");
        //            ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
        //            break;
        //        case MsgID.CoolingOffloadMotorZUnsafe:
        //            msg = string.Format("冷却下料Z轴电机不在安全位，冷却旋转电机不能开始移动");
        //            dispose = string.Format("请人工确认安全后将电机移动至安全位");
        //            ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
        //            break;
        //        case MsgID.MotorRMoveFail:
        //            msg = string.Format("电机开始移动超时，还未检测到{0} {1}为ON状态", Inputs(IMotorRDelay).Num, Inputs(IMotorRDelay).Name);
        //            dispose = string.Format("请人工检查电机，确认后将电机移动至到位感应器位置");
        //            ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
        //            break;
        //        case MsgID.MotorRDelay:
        //            msg = string.Format("电机移动时，已触碰{0} {1}为ON状态，但是还未检测到{2} {3}为ON状态"
        //                , Inputs(IMotorRDelay).Num, Inputs(IMotorRDelay).Name, Inputs(IMotorRInpos).Num, Inputs(IMotorRInpos).Name);
        //            dispose = string.Format("请人工检查电机，确认后将电机移动至到位感应器位置");
        //            ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
        //            break;
        //        case MsgID.MotorRTimeout:
        //            msg = string.Format("电机移动超时，但是还未检测到{0} {1}为ON状态", Inputs(IMotorRInpos).Num, Inputs(IMotorRInpos).Name);
        //            dispose = string.Format("请人工检查电机，确认后将电机移动至到位感应器位置");
        //            ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
        //            break;
        //        case MsgID.IMotorRInposErr:
        //            msg = string.Format("未配置链条大格检测，不能判断旋转电机到位");
        //            dispose = string.Format("请人工检查，确认后重新复位启动");
        //            ShowMessageID((int)msgID, msg, dispose, MessageType.MsgAlarm);
        //            break;
        //        default:
        //            break;
        //    }
        //    #endregion
        //    return result;
        //}

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
            if(((this.OMororRForwardMove > -1) && (Outputs(this.OMororRForwardMove) == output))
                || ((this.OMororRBackwardMove > -1) && (Outputs(this.OMororRBackwardMove) == output)))
            {
                // 禁止手动点动这两个输出，使用移动功能
                ShowMsgBox.ShowDialog("禁止手动点动旋转电机得两个输出，使用调试界面得移动功能", MessageType.MsgWarning);
                return false;
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
            if ((this.MotorR > -1) && (Motors(this.MotorR) == motor))
            {
                ShowMsgBox.ShowDialog("冷却系统旋转R轴仅能在【其他调试-冷却系统调试】里面操作！", MessageType.MsgWarning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 外部触发急停
        /// </summary>
        public void SetORobotEStop()
        {
            Motors(this.MotorR).TorqueMove(false, 0);
            ShowMessageID((int)MsgID.SafeDoorOpenEStop, "安全门急停按下，机器人急停！", "请放开安全门急停按钮后再操作机器人", MessageType.MsgAlarm);
        }

        /// <summary>
        /// 模组防呆监视：请勿加入耗时过长或阻塞操作
        /// </summary>
        public override void MonitorAvoidDie()
        {
        }
        
        #endregion

    }
}
