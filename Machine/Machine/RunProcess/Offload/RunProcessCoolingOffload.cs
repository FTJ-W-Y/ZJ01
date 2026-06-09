using HelperLibrary;
using Machine.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 冷却下料：冷却系统取 → 下料线体放
    /// </summary>
    class RunProcessCoolingOffload : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_MotorZHome,
            Init_MotorZMoveSafe,
            Init_CheckFinger,
            Init_MotorXYUHome,
            Init_MotorXYUMoveSafe,
            Init_ConnectTemper,

            Init_End,
        }

        protected new enum TemperSteps
        {
            Scan_Start = 0,

            Scan_Finished,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 取：冷却系统
            Auto_CalcCoolingSystemPickPos,
            Auto_CoolingSystemPosSetEvent,
            Auto_CoolingSystemPosPickMove,
            Auto_CoolingSystemPosPickDown,
            Auto_CoolingSystemPosFingerAction,
            Auto_CoolingSystemPosPickUp,
            Auto_CoolingSystemPosCheckFinger,

            // 计算放料位
            Auto_CalcPlacePos,

            // 暂存：可取可防，主要看抓手操作
            Auto_CalcBufferPos,
            Auto_BufferPosSetEvent,
            Auto_BufferPosMove,
            Auto_BufferPosDown,
            Auto_BufferPosFingerAction,
            Auto_BufferPosUp,
            Auto_BufferPosCheckFinger,

            // 放：下料线
            Auto_CalcOffloadPlacePos,
            Auto_OffloadPosSetEvent,
            Auto_OffloadPosPlaceMove,
            Auto_OffloadPosPlaceDown,
            Auto_OffloadPosFingerAction,
            Auto_OffloadPosPlaceUp,
            Auto_OffloadPosCheckFinger,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
            Finger_0 = 0,
            Finger_1,
            Finger_2,
            Finger_3,
            Finger_ALL,
            Buffer_0 = Finger_ALL,
            Buffer_1,
            Buffer_2,
            Buffer_3,
            Buffer_ALL,
            Finger_Buffer_ALL = Buffer_ALL,

            // 所有夹爪的位值表示
            Finger_Full = 0x0F,

        }

        private enum MsgID
        {
            Start = ModuleMsgID.CoolingSystemMsgStartID,
            RecvTimeout,
            SendTimeout,
            ScanCodeFail,
            ScanCodeTimeout,
            CodeLenError,
            CodeTypeError,
            CheckBattery,
            SafeDoorOpenEStop
        }

        #endregion

        #region // 取放位置结构体

        private struct PickPlacePos
        {
            #region //字段
            public MotorPosition station;           // 站号
            public int row;                         // 行索引
            public int col;                         // 列索引   
            public int finger;                      // 抓手索引         
            public bool fingerClose;                // 抓手关闭
            #endregion

            #region //方法
            public void SetData(MotorPosition curStation, int curRow, int curCol, int curFinger, bool curFingerClose)
            {
                this.station = curStation;
                this.row = curRow;
                this.col = curCol;
                this.finger = curFinger;
                this.fingerClose = curFingerClose;
            }

            public void Release()
            {
                this.station = MotorPosition.Invalid;
                this.row = -1;
                this.col = -1;
                this.finger = 0;
                this.fingerClose = false;
            }
            #endregion
        };
        #endregion

        #region // 字段，属性

        #region // IO

        private int[] IFingerOpen;              // 抓手打开到位
        private int[] IFingerClose;             // 抓手关闭到位
        private int[] IFingerCheck;             // 抓手有料检查
        private int[] IBufferCheck;             // 暂存有料检查
        private int IFingerDelay;               // 抓手防呆
        private int IRotatePush;               // 下料旋转气缸推出到位
        private int IRotatePull;               // 下料旋转气缸回退到位

        private int[] OFingerOpen;              // 抓手打开
        private int[] OFingerClose;             // 抓手关闭
        private int ORotatePush;               // 下料旋转气缸推出
        private int ORotatePull;               // 下料旋转气缸回退

        #endregion

        #region // 电机

        private int MotorX;         // 电机
        private int MotorY;         // 电机
        private int MotorZ;         // 电机
        private int MotorU;         // 电机

        #endregion

        #region // ModuleEx.cfg配置

        #endregion

        #region // 模组参数

        private float pickPosXDis;      // 取料位X方向间距：mm
        private float pickPosYDis;      // 取料位Y方向间距：mm
        private float pickPosYSafeDis;  // 取料位Y方向安全偏移间距：mm
        private float bufferPosXDis;    // 暂存位X方向间距：mm
        private float bufferPosYDis;    // 暂存位Y方向间距：mm

        private bool[] temperEnable;          // 扫码使能：TRUE启用，FALSE禁用
        private string[] temperScanIP;     // 温控仪的IP：进行网口通讯则填，否则为空
        private int[] temperScanPort;      // 温控仪的Port
        private int[] temperScanNo;        // 温控仪的编号
        private TemperModScan[] temperScan;  // 数字温度器
        private int temperScanUpper;       // 过冷却温度
        private int fingerCloseDelay;      // 夹爪夹紧延迟

        #endregion

        #region // 模组数据

        // 模组指针
        private RunProcessCoolingSystem coolingSystem;      // 冷却系统：CoolingSystem = 
        private RunProcessOffloadLine offloadLine;          // 下料线：OffloadLine = 


        private PickPlacePos pickPos;                       // 取料位置
        private PickPlacePos placePos;                      // 放料位置
        private int csIntervalPos;                          // 冷却系统中取放电池时的间隔位置

        private int TemperStepsEvent;

        

        #endregion

        #endregion


        public RunProcessCoolingOffload(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.Finger_Buffer_ALL, 0);

            PowerUpRestart();

            InitParameter();
            // 参数
            InsertVoidParameter("pickPosXDis", "取料X方向间距", "取料位X方向间距：mm", pickPosXDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("pickPosYDis", "取料Y方向间距", "取料位Y方向间距：mm", pickPosYDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("pickPosYSafeDis", "Y方向安全偏移", "取料位Y方向安全偏移间距：mm", pickPosYSafeDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("bufferPosXDis", "暂存X方向间距", "暂存位X方向间距：mm", bufferPosXDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("bufferPosYDis", "暂存Y方向间距", "暂存位Y方向间距：mm", bufferPosYDis, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter(("fingerCloseDelay"), ("夹爪抓取延迟"), "夹爪抓取延迟：夹爪下降后抓取电池延迟时间上升（毫秒）", fingerCloseDelay, RecordType.RECORD_INT);
            InsertVoidParameter(("temperScanUpper"), ("冷却后温度"), "冷却后温度：默认温度小于50", temperScanUpper, RecordType.RECORD_INT);
            
            for (int i = 0; i < this.temperEnable.Length; i++)
            {
                InsertVoidParameter(("temperEnable" + i), ("夹爪" + (i + 1) + "温控仪使能"), "温控仪使能：TRUE启用，FALSE禁用", temperEnable[i], RecordType.RECORD_BOOL);
                InsertVoidParameter(("temperScanIP" + i), ("夹爪" + (i + 1) + "温控仪IP"), "温控仪的IP：进行网口通讯则填，否则为空", temperScanIP[i], RecordType.RECORD_STRING);
                InsertVoidParameter(("temperScanPort" + i), ("夹爪" + (i + 1) + "温控仪端口"), "温控仪的Port", temperScanPort[i], RecordType.RECORD_INT);
                InsertVoidParameter(("temperScanNo" + i), ("夹爪" + (i + 1) + "温控仪编号"), "温控仪的编号", temperScanNo[i], RecordType.RECORD_INT); 
            }

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
                        this.nextInitStep = InitSteps.Init_MotorZHome;
                        break;
                    }
                case InitSteps.Init_MotorZHome:
                    {
                        CurMsgStr("电机Z回零", "Motor Z home");
                        if (Def.IsNoHardware() || MotorHome(this.MotorZ))
                        {
                            this.nextInitStep = InitSteps.Init_MotorZMoveSafe;
                        }
                        break;
                    }
                case InitSteps.Init_MotorZMoveSafe:
                    {
                        CurMsgStr("电机Z移动到安全位", "Motor Z move safety pos");
                        if(MotorZMove(MotorPosition.CoolingOffload_SafetyPos))
                        {
                            this.nextInitStep = InitSteps.Init_CheckFinger;
                        }
                        break;
                    }
                case InitSteps.Init_CheckFinger:
                    {
                        CurMsgStr("检查抓手及暂存感应器", "Check finger senser");
                        for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                        {
                            if(!FingerCheck((0x01 << i), (FingerBat((ModDef)i).Type > BatteryStatus.Invalid), true))
                            {
                                return;
                            }
                            if(!BufferCheck((0x01 << i), (BufferBat(ModDef.Buffer_0 + i).Type > BatteryStatus.Invalid), true))
                            {
                                return;
                            }
                        }
                        this.nextInitStep = InitSteps.Init_MotorXYUHome;
                        break;
                    }
                case InitSteps.Init_MotorXYUHome:
                    {
                        CurMsgStr("电机XYU回零", "Motor XYU home");
                        int[] motorsID = new int[] { this.MotorX, this.MotorY/*, this.MotorU */};
                        if (Def.IsNoHardware() || MotorsHome(motorsID, motorsID.Length))
                        {
                            this.nextInitStep = InitSteps.Init_MotorXYUMoveSafe;
                        }
                        break;
                    }
                case InitSteps.Init_MotorXYUMoveSafe:
                    {
                        CurMsgStr("电机XYU移动到安全位", "Motor XYU move safety pos");
                        if (MotorXYUMove(MotorPosition.CoolingOffload_SafetyPos, 0, 0))
                        {
                            this.nextInitStep = InitSteps.Init_ConnectTemper;
                        }
                        break;
                    }
                case InitSteps.Init_ConnectTemper:
                    {
                        CurMsgStr("连接串口服务器", "Connect scanner");
                        for (int i = 0; i < temperScan.Length; i++)
                        {
                            if (!TemperConnect(i, true))
                            {
                                return;
                            }
                        }
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
            if (Def.IsNoHardware())
            {
                Sleep(50);
            }

            switch((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        // 取料
                        if (CalcPickPos(ref this.pickPos))
                        {
                            AutoStepSafe = false;
                            this.nextAutoStep = AutoSteps.Auto_CalcCoolingSystemPickPos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        else
                        {
                            #region // 无任务回取料位等待
                            if (!this.AutoStepSafe)
                            {
                                if (Def.IsNoHardware() || MotorXYUMove(MotorPosition.CoolingOffload_PickPos, 0, 0))
                                {
                                    Def.WriteLog("RunProcessCoolingOffload", "无任务回取料位等待");
                                    this.AutoStepSafe = true;
                                }
                            }
                            #endregion
                        }

                        break;
                    }

                #region // 取：冷却系统
                case AutoSteps.Auto_CalcCoolingSystemPickPos:
                    {
                        CurMsgStr("计算冷却系统取料位", "Calc cooling system pick pos");
                        this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosSetEvent;
                        //SaveRunData(SaveType.AutoStep);  //wjj 220726
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosSetEvent:
                    {
                        CurMsgStr("冷却系统取料设置响应信号", "Cooling system set pick event");
                        EventStatus state = GetEvent(this.coolingSystem, EventList.CoolingSystemPickBattery);
                        if (EventStatus.Require == state || EventStatus.Ready == state)
                        {
                            if (SetEvent(this.coolingSystem, EventList.CoolingSystemPickBattery, EventStatus.Response))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosPickMove;
                                //SaveRunData(SaveType.AutoStep); //wjj 220726
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosPickMove:
                    {
                        CurMsgStr("冷却系统取料位移动", "Cooling system pick pos move");
                        if (Def.IsNoHardware() || (MotorXYUMove(pickPos.station, pickPos.row, pickPos.col)))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosPickDown;
                            //SaveRunData(SaveType.AutoStep); //wjj 220726
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosPickDown:
                    {
                        CurMsgStr("冷却系统取料位下降", "Cooling system pick pos down");
                        EventStatus state = GetEvent(this.coolingSystem, EventList.CoolingSystemPickBattery);
                        if ((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            SetEvent(this.coolingSystem, EventList.CoolingSystemPickBattery, EventStatus.Start);

                            //if(RotatePush(true, true) && MotorZMove(pickPos.station)) 
                            // 在下降过程中停机，初始化回原点，先回到停机前的位置 // wjj 220424 add
                            // 下降前务必保证夹爪张开
                            if (Def.IsNoHardware() || (FingerClose(pickPos.finger, false) //wjj 220428
                                && MotorXYUMove(pickPos.station, pickPos.row, pickPos.col) //wjj 220424
                                && MotorZMove(pickPos.station)))
                            {
                                this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosFingerAction;
                                //SaveRunData(SaveType.AutoStep); //wjj 220726
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosFingerAction:
                    {
                        CurMsgStr("冷却系统取料位抓手关闭", "Cooling system pick pos finger close");
                        if (FingerClose(pickPos.finger, pickPos.fingerClose))
                        {
                            Sleep(this.fingerCloseDelay);

                            RunProcessCoolingSystem run = this.coolingSystem;
                            if(null != run)
                            {
                                int col = run.BatteryLine.MaxCol - 1;
                                for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                {
                                    if((pickPos.finger & (0x01 << i)) == (0x01 << i))
                                    {
                                        int batRow = i * (this.csIntervalPos + 1) + pickPos.row;
                                        this.Battery[i].Copy(run.BatteryLine.Battery[batRow, col]);
                                        run.BatteryLine.Battery[batRow, col].Release();
                                    }
                                }
                                run.SaveRunData(SaveType.Battery);
                            }
                            this.TemperStepsEvent = (int)TemperSteps.Scan_Start;
                            this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosPickUp;
                            SaveRunData(SaveType.AutoStep|SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosPickUp:
                    {
                        CurMsgStr("冷却系统取料位上升", "Cooling system pick pos up");
                        if (Def.IsNoHardware() || MotorZMove(MotorPosition.CoolingOffload_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CoolingSystemPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_CoolingSystemPosCheckFinger:
                    {
                        CurMsgStr("冷却系统取料后检查抓手", "Cooling system pick pos check finger");
                        if (FingerCheck(pickPos.finger, pickPos.fingerClose, true))
                        {
                            if (this.TemperStepsEvent != (int)TemperSteps.Scan_Finished)
                            {
                                List<double> lstTemper = new List<double>();
                                if (!CheckTemper(true, ref lstTemper))
                                {
                                    break;
                                }
                                this.TemperStepsEvent = (int)TemperSteps.Scan_Finished;
                            }

                            string locName = "";
                            float curPos, locPos;
                            curPos = locPos = 0.0f;
                            if (Def.IsNoHardware() || ((int)MotorCode.MotorOK == Motors(this.MotorY).GetCurPos(ref curPos))
                                && ((int)MotorCode.MotorOK == Motors(this.MotorY).GetLocation((int)pickPos.station, ref locName, ref locPos)))
                            {
                                if (curPos > locPos + this.pickPosYSafeDis)
                                {
                                    if (Def.IsNoHardware() || !MotorMove(this.MotorY, (int)pickPos.station, this.pickPosYSafeDis))
                                    {
                                        break;
                                    }
                                }
                                SetEvent(this.coolingSystem, EventList.CoolingSystemPickBattery, EventStatus.Finished);

                                this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                #endregion

                #region // 计算放料位
                case AutoSteps.Auto_CalcPlacePos:
                    {
                        CurMsgStr("计算放料位", "Calc place pos");
                        // 这里暂时不放暂存位，电池已经在上料做好配对，这里除非是清尾料，不然暂时不考虑配对问题
                        //if (CalcFingerBufferMatchesPos(ref placePos))
                        //{
                        //    this.nextAutoStep = AutoSteps.Auto_CalcBufferPos;
                        //    SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        //}
                        //else 
                        if (FingerCount() < 1)
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        else if (CalcPlacePos(ref placePos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcOffloadPlacePos;
                            SaveRunData(SaveType.AutoStep | SaveType.Variables);
                        }
                        break;
                    }
                #endregion

                #region // 暂存：可取可防
                case AutoSteps.Auto_CalcBufferPos:
                    {
                        CurMsgStr("计算缓存取放位", "Calc buffer pos");
                        this.nextAutoStep = AutoSteps.Auto_BufferPosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_BufferPosSetEvent:
                    {
                        CurMsgStr("缓存位设置响应信号", "Set buffer pos event");
                        this.nextAutoStep = AutoSteps.Auto_BufferPosMove;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_BufferPosMove:
                    {
                        CurMsgStr("缓存位移动", "Buffer pos move");
                        if (Def.IsNoHardware() || MotorXYUMove(placePos.station, placePos.row, placePos.col))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosDown;
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosDown:
                    {
                        CurMsgStr("缓存位下降", "Buffer pos down");
                        if (Def.IsNoHardware() || MotorZMove(placePos.station))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosFingerAction;
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosFingerAction:
                    {
                        CurMsgStr("缓存位抓手动作", "Buffer pos finger action");
                        if (FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            int bufStart, fingStart;
                            bufStart = fingStart = 0;
                            if(placePos.row <= ((int)ModDef.Finger_ALL - 1))
                            {
                                fingStart = (int)ModDef.Finger_ALL - 1 - placePos.row;
                                bufStart = (int)ModDef.Buffer_0 - fingStart;
                            }
                            else
                            {
                                bufStart = (int)ModDef.Buffer_0 + placePos.row - ((int)ModDef.Finger_ALL - 1);
                                fingStart = 0;
                            }
                            // 取
                            if(placePos.fingerClose)
                            {
                                for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                {
                                    if((placePos.finger & (0x01 << i)) == (0x01 << i))
                                    {
                                        this.Battery[i].Copy(this.Battery[bufStart + i]);
                                        this.Battery[bufStart + i].Release();
                                    }
                                }
                            }
                            // 放
                            else
                            {
                                for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                {
                                    if((placePos.finger & (0x01 << i)) == (0x01 << i))
                                    {
                                        this.Battery[bufStart + i].Copy(this.Battery[i]);
                                        this.Battery[i].Release();
                                    }
                                }
                            }
                            this.nextAutoStep = AutoSteps.Auto_BufferPosUp;
                            SaveRunData(SaveType.AutoStep | SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosUp:
                    {
                        CurMsgStr("缓存位上升", "Buffer pos up");
                        if (Def.IsNoHardware() || MotorZMove(MotorPosition.CoolingOffload_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_BufferPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_BufferPosCheckFinger:
                    {
                        CurMsgStr("缓存位取放料后检查抓手", "Buffer pos Check finger senser");
                        if (FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

                #region // 放：下料线
                case AutoSteps.Auto_CalcOffloadPlacePos:
                    {
                        CurMsgStr("计算放料位", "Calc offload place pos");
                        this.nextAutoStep = AutoSteps.Auto_OffloadPosSetEvent;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                case AutoSteps.Auto_OffloadPosSetEvent:
                    {
                        CurMsgStr("放料位设置响应信号", "Set offload place pos event");
                        // wjj 220726
                        //if (EventStatus.Require == GetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery))
                        //{
                        //    if (SetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery, EventStatus.Response))
                        //    {
                        //        this.nextAutoStep = AutoSteps.Auto_OffloadPosPlaceMove;
                        //        SaveRunData(SaveType.AutoStep);
                        //    }
                        //}
                        this.nextAutoStep = AutoSteps.Auto_OffloadPosPlaceMove;

                        // wjj 220726

                        break;
                    }
                case AutoSteps.Auto_OffloadPosPlaceMove:
                    {
                        CurMsgStr("放料位移动", "Offload place pos move");
                        if (Def.IsNoHardware() || (MotorXYUMove(placePos.station, placePos.row, placePos.col)))
                        {
                            // wjj 220726 //先移动到放料位置上方
                            //this.nextAutoStep = AutoSteps.Auto_OffloadPosPlaceDown;
                            //SaveRunData(SaveType.AutoStep);
                            EventStatus state = GetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery);
                            if (EventStatus.Require == state || EventStatus.Ready == state || EventStatus.Start == state)
                            {
                                if (SetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery, EventStatus.Response))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_OffloadPosPlaceDown;
                                }
                            }
                            // wjj 220726
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPosPlaceDown:
                    {
                        CurMsgStr("放料位下降", "Offload place pos down");
                        EventStatus state = GetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery);
                        if((EventStatus.Ready == state) || (EventStatus.Start == state))
                        {
                            SetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery, EventStatus.Start);

                            if (Def.IsNoHardware() || (MotorXYUMove(placePos.station, placePos.row, placePos.col) // wjj 220424
                                && MotorZMove(placePos.station)))
                            {
                                this.nextAutoStep = AutoSteps.Auto_OffloadPosFingerAction;
                                //SaveRunData(SaveType.AutoStep); // wjj 220726
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPosFingerAction:
                    {
                        CurMsgStr("放料位抓手打开", "Offload place pos finger open");
                        if (FingerClose(placePos.finger, placePos.fingerClose))
                        {
                            RunProcessOffloadLine run = this.offloadLine;
                            if(null != run)
                            {
                                for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                {
                                    if((placePos.finger & (0x01 << i)) == (0x01 << i))
                                    {
                                        run.Battery[placePos.row + i].Copy(this.Battery[i]);
                                        this.Battery[i].Release();
                                    }
                                }
                                run.SaveRunData(SaveType.Battery);
                            }

                            this.nextAutoStep = AutoSteps.Auto_OffloadPosPlaceUp;
                            SaveRunData(SaveType.AutoStep|SaveType.Battery);
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPosPlaceUp:
                    {
                        CurMsgStr("放料位上升", "Offload place pos up");
                        if (Def.IsNoHardware() || MotorZMove(MotorPosition.CoolingOffload_SafetyPos))
                        {
                            this.nextAutoStep = AutoSteps.Auto_OffloadPosCheckFinger;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_OffloadPosCheckFinger:
                    {
                        CurMsgStr("放料后检查抓手", "Offload place pos check finger senser");
                        if(FingerCheck(placePos.finger, placePos.fingerClose))
                        {
                            SetEvent(this.offloadLine, EventList.OffLoadLinePlaceBattery, EventStatus.Finished);

                            this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                #endregion

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
            this.pickPos.Release();
            this.placePos.Release();

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

            key = string.Format("pickPos.station");
            this.pickPos.station = (MotorPosition)iniStream.ReadInt(section, key, (int)this.pickPos.station);
            key = string.Format("pickPos.row");
            this.pickPos.row = iniStream.ReadInt(section, key, this.pickPos.row);
            key = string.Format("pickPos.col");
            this.pickPos.col = iniStream.ReadInt(section, key, this.pickPos.col);
            key = string.Format("pickPos.finger");
            this.pickPos.finger = iniStream.ReadInt(section, key, (int)this.pickPos.finger);
            key = string.Format("pickPos.fingerClose");
            this.pickPos.fingerClose = iniStream.ReadBool(section, key, this.pickPos.fingerClose);

            key = string.Format("placePos.station");
            this.placePos.station = (MotorPosition)iniStream.ReadInt(section, key, (int)this.placePos.station);
            key = string.Format("placePos.row");
            this.placePos.row = iniStream.ReadInt(section, key, this.placePos.row);
            key = string.Format("placePos.col");
            this.placePos.col = iniStream.ReadInt(section, key, this.placePos.col);
            key = string.Format("placePos.finger");
            this.placePos.finger = iniStream.ReadInt(section, key, (int)this.placePos.finger);
            key = string.Format("placePos.fingerClose");
            this.placePos.fingerClose = iniStream.ReadBool(section, key, this.placePos.fingerClose);

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

            if(SaveType.Variables == (SaveType.Variables & saveType))
            {
                string[] posName = new string[] { "pickPos", "placePos" };
                PickPlacePos[] pos = new PickPlacePos[] { pickPos, placePos };
                for(int i = 0; i < pos.Length; i++)
                {
                    key = string.Format("{0}.station", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].station);
                    key = string.Format("{0}.row", posName[i]);
                    iniStream.WriteInt(section, key, pos[i].row);
                    key = string.Format("{0}.col", posName[i]);
                    iniStream.WriteInt(section, key, pos[i].col);
                    key = string.Format("{0}.finger", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].finger);
                    key = string.Format("{0}.fingerClose", posName[i]);
                    iniStream.WriteBool(section, key, pos[i].fingerClose);
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
            this.pickPosXDis = 0.0f;
            this.pickPosYDis = 0.0f;
            this.pickPosYSafeDis = 0.0f;
            this.bufferPosXDis = 0.0f;
            this.bufferPosYDis = 0.0f;
            this.csIntervalPos = 0;
            this.fingerCloseDelay = 0;

            int scanNum = (int)ModDef.Finger_ALL;
            this.temperEnable = new bool[scanNum];
            this.temperScanIP = new string[scanNum];
            this.temperScanPort = new int[scanNum];
            this.temperScanNo = new int[scanNum];
            this.temperScanUpper = 50;
            for (int i = 0; i < scanNum; i++)
            {
                this.temperEnable[i] = false;
                this.temperScanIP[i] = string.Empty;
                this.temperScanPort[i] = 0;
                this.temperScanNo[i] = 1;
            }
            this.temperScan = new TemperModScan[scanNum];
            for (int i = 0; i < this.temperScan.Length; i++)
            {
                this.temperScan[i] = new TemperModScan();
            }
            this.TemperStepsEvent = 0;
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.pickPosXDis = (float)ReadDoubleParameter(this.RunModule, "pickPosXDis", this.pickPosXDis);
            this.pickPosYDis = (float)ReadDoubleParameter(this.RunModule, "pickPosYDis", this.pickPosYDis);
            this.pickPosYSafeDis = (float)ReadDoubleParameter(this.RunModule, "pickPosYSafeDis", this.pickPosYSafeDis);
            this.bufferPosXDis = (float)ReadDoubleParameter(this.RunModule, "bufferPosXDis", this.bufferPosXDis);
            this.bufferPosYDis = (float)ReadDoubleParameter(this.RunModule, "bufferPosYDis", this.bufferPosYDis);
            this.fingerCloseDelay = ReadIntParameter(this.RunModule, "fingerCloseDelay", this.fingerCloseDelay);
            this.temperScanUpper = ReadIntParameter(this.RunModule, ("temperScanUpper"), this.temperScanUpper);
            for (int i = 0; i < this.temperEnable.Length; i++)
            {
                this.temperEnable[i] = ReadBoolParameter(this.RunModule, ("temperEnable" + i), this.temperEnable[i]);
                this.temperScanIP[i] = ReadStringParameter(this.RunModule, ("temperScanIP" + i), this.temperScanIP[i]);
                this.temperScanPort[i] = ReadIntParameter(this.RunModule, ("temperScanPort" + i), this.temperScanPort[i]);
                this.temperScanNo[i] = ReadIntParameter(this.RunModule, ("temperScanNo" + i), this.temperScanNo[i]);
            }

            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            // 取电池模组
            this.coolingSystem = MachineCtrl.GetInstance().GetModule(RunID.CoolingSystem) as RunProcessCoolingSystem;
            // 放电池模组
            this.offloadLine = MachineCtrl.GetInstance().GetModule(RunID.OffloadLine) as RunProcessOffloadLine;

        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxFinger = (int)ModDef.Finger_ALL;
            this.IFingerOpen = new int[maxFinger];
            this.IFingerClose = new int[maxFinger];
            this.IFingerCheck = new int[maxFinger];
            this.IBufferCheck = new int[maxFinger];
            for(int i = 0; i < maxFinger; i++)
            {
                this.IFingerOpen[i] = AddInput("IFingerOpen" + i);
                this.IFingerClose[i] = AddInput("IFingerClose" + i);
                this.IFingerCheck[i] = AddInput("IFingerCheck" + i);
            }
            for(int i = 0; i < maxFinger; i++)
            {
                this.IBufferCheck[i] = AddInput("IBufferCheck" + i);
            }
            this.IFingerDelay = AddInput("IFingerDelay");
            this.IRotatePush = AddInput("IRotatePush");
            this.IRotatePull = AddInput("IRotatePull");

            this.OFingerOpen = new int[maxFinger];
            this.OFingerClose = new int[maxFinger];
            for(int i = 0; i < maxFinger; i++)
            {
                this.OFingerOpen[i] = AddOutput("OFingerOpen" + i);
                this.OFingerClose[i] = AddOutput("OFingerClose" + i);
            }
            this.ORotatePush = AddOutput("ORotatePush");
            this.ORotatePull = AddOutput("ORotatePull");

            this.MotorX = AddMotor("MotorX");
            this.MotorY = AddMotor("MotorY");
            this.MotorZ = AddMotor("MotorZ");
            this.MotorU = AddMotor("MotorU");
        }

        #endregion

        #region // 电机操作

        private bool MotorZMove(MotorPosition station)
        {
            if (this.MotorZ < 0)
            {
                return true;
            }
            if (Def.IsNoHardware())
            {
                return true;
            }
            return MotorMove(this.MotorZ, (int)station);
        }

        private bool MotorXYUMove(MotorPosition station, int row, int col)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            if (this.MotorX < 0 || this.MotorY < 0/* || this.MotorU < 0*/)
            {
                return false;
            }

            float XOffset = 0.0f;
            float YOffset = 0.0f;
            float UOffset = 0.0f;

            switch(station)
            {
                case MotorPosition.CoolingOffload_PickPos:
                    XOffset = (row * this.pickPosXDis);
                    YOffset = (col * this.pickPosYDis);
                    break;
                case MotorPosition.CoolingOffload_BufferPos:
                    XOffset = ((row - 3) * this.bufferPosXDis);
                    YOffset = (col * this.bufferPosYDis);
                    break;
            }

            int[] mtrIdx = { this.MotorX, this.MotorY/*, this.MotorU*/ };
            Motor[] mtrs = { Motors(this.MotorX), Motors(this.MotorY)/*, Motors(this.MotorU)*/ };
            int[] loc = { (int)station, (int)station, (int)station };
            float[] offsetPos = { XOffset, YOffset, UOffset };
            float[] destPos = { 0, 0, 0 };
            //double[] maxPos = { this.motorXMaxPos, this.motorYMaxPos, this.motorUMaxPos };

            //string stationName = "";
            //for(int i = 0; i < mtrIdx.Length; i++)
            //{
            //    string locName = "";
            //    float stationPos = 0;
            //    mtrs[i].GetLocation((int)station, ref locName, ref stationPos);
            //    destPos[i] = stationPos + offsetPos[i];
            //    if(!CheckMotorPosRange(mtrs[i], destPos[i], MotorMoveType.MotorMoveAbsMove, 0, maxPos[i]))
            //    {
            //        return false;
            //    }
            //    if(string.IsNullOrEmpty(stationName))
            //        stationName = locName;
            //}

            //this.XYZAutoAction.SetData((int)station, row, col, RobotOrder.MOVE, stationName);
            //this.XYZDebugAction.SetData((int)station, row, col, RobotOrder.MOVE, stationName);

            //DataBaseLog.AddMotorLog(new DataBaseLog.MotorLogFormula(Def.GetProductFormula(), MachineCtrl.GetInstance().OperaterID
            //    , DateTime.Now.ToString(Def.DateFormal), Motors(MotorX).MotorIdx, Motors(MotorX).Name, OptMode.Auto.ToString(), "XYU轴移动", "", stationName));

            if (Def.IsNoHardware() || MotorsMove(mtrIdx, loc, offsetPos, mtrIdx.Length))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查电机Z轴是否在安全位
        /// </summary>
        /// <returns></returns>
        public bool CheckMotorZPos(MotorPosition posID)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            if (CheckMotorPos(this.MotorZ, posID, false))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 电机回安全位置
        /// </summary>
        /// <returns></returns>
        public override bool MotorMoveToSafePos()
        {
            MCState mcState = MachineCtrl.GetInstance().GetModuleMCState(RunID.CoolingOffload);
            if ((MCState.MCInitComplete == mcState) || (MCState.MCStopRun == mcState))
            {
                if (Def.IsNoHardware() || MotorZMove(MotorPosition.CoolingOffload_SafetyPos))
                {
                    if (Def.IsNoHardware() || MotorXYUMove(MotorPosition.CoolingOffload_SafetyPos, 0, 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region // 抓手及暂存
        
        private bool FingerClose(int finger, bool close)
        {
            return CylinderAction.CylinderPush(this, finger, close, IFingerClose, IFingerOpen, OFingerClose, OFingerOpen);
        }

        private Battery FingerBat(ModDef finger)
        {
            if(finger < ModDef.Finger_0 || finger >= ModDef.Finger_ALL)
            {
                return null;
            }
            return this.Battery[(int)finger];
        }

        private bool FingerCheck(int finger, bool hasBat, bool alarm = true)
        {
            if (Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            for (int i = 0; i < IFingerCheck.Length; i++)
            {
                if ((finger & (0x01 << i)) == (0x01 << i))
                {
                    if (!InputState(IFingerCheck[i], hasBat))
                    {
                        if (alarm)
                        {
                            CheckInputState(IFingerCheck[i], hasBat);
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        private int FingerCount()
        {
            int count = 0;
            for(ModDef i = ModDef.Finger_0; i < ModDef.Finger_ALL; i++)
            {
                if(FingerBat(i).Type > BatteryStatus.Invalid)
                {
                    count++;
                }
            }
            return count;
        }

        private Battery BufferBat(ModDef buffer)
        {
            if(buffer < ModDef.Buffer_0 || buffer >= ModDef.Buffer_ALL)
            {
                return null;
            }
            return this.Battery[(int)buffer];
        }

        private bool BufferCheck(int buffer, bool hasBat, bool alarm = true)
        {
            if(Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            for(int i = 0; i < (int)ModDef.Buffer_ALL; i++)
            {
                if((buffer & (0x01 << i)) == (0x01 << i))
                {
                    if(!InputState(IBufferCheck[i], hasBat))
                    {
                        if(alarm)
                        {
                            CheckInputState(IBufferCheck[i], hasBat);
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        private int BufferCount()
        {
            int count = 0;
            for(ModDef i = ModDef.Buffer_0; i < ModDef.Buffer_ALL; i++)
            {
                if(BufferBat(i).Type > BatteryStatus.Invalid)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region // 取放料计算

        /// <summary>
        /// 计算取位置
        /// </summary>
        /// <param name="pick"></param>
        /// <returns></returns>
        private bool CalcPickPos(ref PickPlacePos pick)
        {
            RunProcessCoolingSystem run = this.coolingSystem;
            if ((null != run) && (EventStatus.Require == GetEvent(run, EventList.CoolingSystemPickBattery)))
            {
                int col = run.BatteryLine.MaxCol - 1;
                int maxPos = run.BatteryLine.MaxRow - (int)ModDef.Finger_ALL * (csIntervalPos + 1);
                for(int row = 0; row <= maxPos; row++)
                {
                    if((BatteryStatus.Invalid != run.BatteryLine.Battery[row, col].Type)
                        || (row == maxPos))
                    {
                        // 暂存 > 2 且来料有NG则取2，否则全部抓手取
                        int nendNum = (BufferCount() > 2) ? (int)ModDef.Finger_ALL / 2 : (int)ModDef.Finger_ALL;
                        int count = 0;
                        int finger = 0;
                        for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                        {
                            int batRow = row + i * (csIntervalPos + 1);
                            if((BatteryStatus.Invalid != run.BatteryLine.Battery[batRow, col].Type))
                            {
                                if(count >= nendNum)
                                {
                                    break;
                                }
                                count++;
                                finger |= (0x01 << i);
                            }
                        }
                        if(finger > 0)
                        {
                            pick.SetData(MotorPosition.CoolingOffload_PickPos, row, 0, finger, true);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算抓手及暂存配对位置
        /// </summary>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcFingerBufferMatchesPos(ref PickPlacePos curPos)
        {
            int bufRow, calcFinger;
            int fingBit, bufBit;
            int fingBatNum, bufBatNum;

            #region // 计算抓手及暂存的电芯

            fingBit = fingBatNum = 0;
            bufBit = bufBatNum = 0;
            for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
            {
                // 有NG电池，优先排NG
                if(FingerBat((ModDef)i).Type == BatteryStatus.NG)
                {
                    return false;
                }
                else if(FingerBat((ModDef)i).Type != BatteryStatus.Invalid)
                {
                    fingBit |= (0x01 << i);
                    fingBatNum++;
                }

                if(BufferBat(ModDef.Buffer_0 + i).Type == BatteryStatus.OK)
                {
                    bufBit |= (0x01 << i);
                    bufBatNum++;
                }
                // 暂存有非OK的电芯，则退出
                else if(BufferBat(ModDef.Buffer_0 + i).Type != BatteryStatus.Invalid)
                {
                    return false;
                }
            }
            #endregion

            if(fingBatNum < (int)ModDef.Finger_ALL)
            {
                // 抓手 + 暂存 >= ModDef.Finger_ALL  取
                if(fingBatNum + bufBatNum >= (int)ModDef.Finger_ALL)
                {
                    bufRow = calcFinger = 0;
                    if(MatchesPos.CalcPos(true, fingBit, bufBit, ref bufRow, ref calcFinger))
                    {
                        Def.WriteLog("RunProcessCoolingOffload.CalcFingerBufferMatchesPos", $"抓手 + 暂存 >= ModDef.Finger_ALL  取(true, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                        curPos.SetData(MotorPosition.CoolingOffload_BufferPos, bufRow, 0, calcFinger, true);
                        return true;
                    }
                    // 不能取则先放
                    else if(MatchesPos.CalcPos(false, fingBit, bufBit, ref bufRow, ref calcFinger))
                    {
                        Def.WriteLog("RunProcessCoolingOffload.CalcFingerBufferMatchesPos", $"不能取则先放(false, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                        curPos.SetData(MotorPosition.CoolingOffload_BufferPos, bufRow, 0, calcFinger, false);
                        return true;
                    }
                }
                // 抓手 + 暂存 < ModDef.Finger_ALL && 抓手 > 0  放
                else if(fingBatNum > 0)
                {
                    bufRow = calcFinger = 0;
                    if(MatchesPos.CalcPos(false, fingBit, bufBit, ref bufRow, ref calcFinger))
                    {
                        Def.WriteLog("RunProcessCoolingOffload.CalcFingerBufferMatchesPos", $"抓手 + 暂存 < ModDef.Finger_ALL && 抓手 > 0  放(false, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                        curPos.SetData(MotorPosition.CoolingOffload_BufferPos, bufRow, 0, calcFinger, false);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 计算放位置
        /// </summary>
        /// <param name="place"></param>
        /// <returns></returns>
        private bool CalcPlacePos(ref PickPlacePos place)
        {
            //if ((int)ModDef.Finger_ALL == FingerCount() || (MachineCtrl.GetInstance().OffloadClear && FingerCount() > 0))
            {
                RunProcessOffloadLine run = this.offloadLine;
                //if ((null != run) && (EventStatus.Require == GetEvent(run, EventList.OffLoadLinePlaceBattery)))
                if (null != run)
                {
                    place.SetData(MotorPosition.CoolingOffload_PlacePos, 0, 0, (int)ModDef.Finger_Full, false);
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region // 防呆检查

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
            // 电机Z轴不在在安全位，禁止操作XYU 轴
            if((MotorX > -1 && Motors(MotorX) == motor) || (MotorY > -1 && Motors(MotorY) == motor) || (MotorU > -1 && Motors(MotorU) == motor))
            {
                if(!CheckMotorZPos(MotorPosition.OffLoad_SafetyPos))
                {
                    string msg = string.Format("Z轴不在安全位，禁止操作XYU电机！！！");
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    return false;
                }
            }
            if((MotorZ > -1) && (Motors(MotorZ) == motor) && (this.MotorY > -1))
            {
                if((MotorMoveType.MotorMoveBackward == moveType)
                    || (MotorMoveType.MotorMoveHome == moveType)
                    || ((MotorMoveType.MotorMoveLocation == moveType) && (nLocation == (int)MotorPosition.CoolingOffload_SafetyPos)))
                {
                    return true;
                }
                else if(MotorMoveType.MotorMoveLocation == moveType)
                {
                    string posName, msg;
                    posName = msg = "";
                    float posValue, curValue;
                    posValue = curValue = 0.0f;
                    int[] mtr = { MotorX, MotorY, MotorU };
                    float[] fOffset = { pickPosXDis, 1.0f, 1.0f };
                    for(int i = 0; i < mtr.Length; i++)
                    {
                        if((mtr[i] > -1) && (int)MotorCode.MotorOK == Motors(mtr[i]).GetCurPos(ref curValue))
                        {
                            Motors(mtr[i]).GetLocation(nLocation, ref posName, ref posValue);
                            if(Math.Abs(Math.Abs((curValue - posValue) % fOffset[i]) - Math.Abs(fOffset[i])) > Motors(mtr[i]).PosErrRange
                                && Math.Abs((curValue - posValue) % fOffset[i]) > Motors(mtr[i]).PosErrRange)
                            {
                                msg = string.Format("{0}】不在[{1} {2}]位置或此位置的偏移位置！\r\n不能操作Z轴下降到[{1} {2}]！"
                                    , Motors(mtr[i]).Name, nLocation, posName);
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                        }
                    }
                    int input = ((int)MotorPosition.CoolingOffload_PlacePos == nLocation) ? IRotatePull : IRotatePush;
                    if(!InputState(input, true))
                    {
                        msg = string.Format("{0} {1}】感应器非ON，不能操作Z轴下降！"
                            , Inputs(input).Num, Inputs(input).Name);
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        return false;
                    }
                }
                else if((MotorMoveType.MotorMoveAbsMove == moveType)
                    || (MotorMoveType.MotorMoveForward == moveType))
                {
                    if(fValue > 10.0)
                    {
                        ShowMsgBox.ShowDialog("非点位移动，Z轴不能一次下降超过[10.0mm]！", MessageType.MsgAlarm);
                        return false;
                    }
                }
                else if(MotorMoveType.MotorMoveLocation == moveType)
                {
                    ShowMsgBox.ShowDialog("XYU轴电机未找到保存的位置，不能操作Z轴下降", MessageType.MsgWarning);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 检查输出点位是否可操作
        /// </summary>
        /// <param name="output"></param>
        /// <param name="bOn"></param>
        /// <returns></returns>
        public override bool CheckOutputCanActive(Output output, bool bOn)
        {
            // 夹爪非安全位禁止旋转
            int outNum = -1;
            if(ORotatePush > -1 && Outputs(ORotatePush) == output)
            {
                outNum = ORotatePush;
            }
            else if(ORotatePull > -1 && Outputs(ORotatePull) == output)
            {
                outNum = ORotatePull;
            }
            if((outNum > -1) && !CheckMotorZPos(MotorPosition.CoolingOffload_SafetyPos))
            {
                string msg = string.Format("Z轴不在安全位，夹爪禁止旋转操作！！！");
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                return false;
            }
            // Y小于安全位，则不能操作旋转气缸
            if((this.MotorY > -1) 
                && ((this.ORotatePush > -1) && (Outputs(ORotatePush) == output) || ((this.ORotatePull > -1) && (Outputs(ORotatePull) == output))))
            {
                string posName, msg;
                posName = msg = "";
                float posValue, curValue;
                posValue = curValue = 0.0f;
                Motors(MotorY).GetLocation((int)MotorPosition.CoolingOffload_PickPos, ref posName, ref posValue);
                if ((int)MotorCode.MotorOK == Motors(MotorY).GetCurPos(ref curValue))
                {
                    if (curValue > posValue)
                    {
                        msg = string.Format("{0}】当前位置[{1}]＞{2}位置[{3}]，不能操作【{4} {5}】"
                            , Motors(MotorY).Name, curValue.ToString("#0.00"), posName, posValue.ToString("#0.00"), output.Num, output.Name);
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 外部触发急停
        /// </summary>
        public void SetORobotEStop()
        {
            Motor[] mtrs = { Motors(this.MotorX), Motors(this.MotorY), Motors(this.MotorZ) };
            MultiAxisStop(mtrs, mtrs.Length);
            ShowMessageID((int)MsgID.SafeDoorOpenEStop, "安全门急停按下，机器人急停！", "请放开安全门急停按钮后再操作机器人", MessageType.MsgAlarm);
        }

        #endregion

        #region //温度采集

        /// <summary>
        /// 温控仪连接
        /// </summary>
        /// <param name="index">温控仪索引</param>
        /// <param name="connect">true连接，false断开</param>
        /// <returns></returns>
        public bool TemperConnect(int index, bool connect = true)
        {
            if (!this.temperEnable[index] || Def.IsNoHardware())
            {
                return true;
            }
            if (connect)
            {
                if (!string.IsNullOrEmpty(this.temperScanIP[index]) && this.temperScanPort[index] > 0)
                {
                    return this.temperScan[index].ConnectSocket(this.temperScanIP[index], this.temperScanPort[index]);
                }
            }
            else
            {
                return this.temperScan[index].Disconnect();
            }
            return false;
        }

        public bool CheckTemper(bool checkCode, ref List<double> lstTemper)
        {
            #region //采集温度
            bool temperOk = true;
            //触发获取温度
            for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
            {
                if ((!string.IsNullOrEmpty(this.Battery[i].Code) || !checkCode) && !TemperScan(i))
                {
                    return false; // 发送读取命令失败则直接报警
                }
            }

            //获取温度
            short temperValue = 0;
            for (int batIdx = 0; batIdx < (int)ModDef.Finger_ALL; batIdx++)
            {
                if (!string.IsNullOrEmpty(this.Battery[batIdx].Code) || !checkCode)
                {
                    temperValue = 0;
                    if (!GetTemperResult(batIdx, ref temperValue))
                    {
                        temperOk = false;
                    }

                    this.Battery[batIdx].TemperValue = temperValue;

                    string code = string.IsNullOrEmpty( this.Battery[batIdx].Code ) ? "空" : this.Battery[batIdx].Code;
                    string text = $"{code},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{$"夹爪{batIdx+1}"},{temperValue.ToString()}";
                    SaveLogData("冷却电芯温度", text);
                    lstTemper.Add(temperValue);
                    //写日志
                    if ((temperValue > this.temperScanUpper))
                    {
                        temperOk = false;
                    }
                }
            }

            #endregion //采集温度
            return temperOk;
        }

        /// <summary>
        /// 温控仪触发采集
        /// </summary>
        /// <returns></returns>
        public bool TemperScan(int index)
        {
            if (!this.temperEnable[index] || Def.IsNoHardware())
            {
                return true;
            }
            int len = 0;
            byte[] data = new byte[10];
            data[len] = (byte)this.temperScanNo[index]; len++;
            data[len] = 0x03; len++; //读取
            data[len] = 0x00; len++; //4字节读取
            data[len] = 0x00; len++;
            data[len] = 0x00; len++;
            data[len] = 0x02; len++;
            byte[] bCrc = Def.CRC16Calc2(data, len);
            data[len] = bCrc[0]; len++;
            data[len] = bCrc[1]; len++;
            if (this.temperScan[index].Send(data, len))
            {
                return true;
            }
            ShowMessageBox((int)MsgID.ScanCodeFail, "触发温控器失败", "请检查串口服务器连接", MessageType.MsgAlarm);
            return false;
        }

        /// <summary>
        /// 获取温控仪采集结果
        /// </summary>
        /// <param name="code">获取到的条码</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public bool GetTemperResult(int index, ref short temperValue, int timeout = 5 * 1000)
        {
            if (!this.temperEnable[index] || Def.IsNoHardware())
            {
                temperValue = 20;
                return true;
            }
            if (this.temperScan[index].Recv(ref temperValue, timeout))
            {
                return true;
            }
            ShowMessageBox((int)MsgID.ScanCodeTimeout, "获取温度超时", "请检查温度采集器", MessageType.MsgAlarm);
            return false;
        }
        
        public static void SaveLogData(string strName, string text)
        {
            string file, title, fileName = "冷却日志";
            file = string.Format(@"{0}\{1}\{2}\{3}\{2}{4}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, fileName, strName, DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("yyyy-MM-dd"));

            title = "条码(SFC),采集时间,夹爪,温度(c)" ;

            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }

        #endregion 温度采集

    }
}

