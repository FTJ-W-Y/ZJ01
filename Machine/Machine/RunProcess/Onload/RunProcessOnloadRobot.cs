using HelperLibrary;
using Machine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;
using System.Linq;

namespace Machine
{
    /// <summary>
    /// 上料机器人
    /// </summary>
    class RunProcessOnloadRobot : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_CheckFinger,
            Init_CheckPallet,
            Init_DeviceConnect,
            Init_RobotHome,
            Init_MotorHome,
            Init_ScannerConnect,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 避让大机器人
            Auto_RobotMoveAvoidPos,
            Auto_WaitActionFinish,

            // 取：NG夹具转盘，取待测假电池
            Auto_CalcPalletPickPos,
            Auto_PalletPickPosSetEvent,
            Auto_PalletPickPosPickMove,
            Auto_PalletPickPosPickDown,
            Auto_PalletPickPosFingerAction,
            Auto_PalletPickPosPickUp,
            Auto_PalletPickPosCheckFinger,

            // 取：来料线
            Auto_CalcOnlinePickPos,
            Auto_OnlinePosSetEvent,
            Auto_OnlinePosPickMove,
            Auto_OnlinePosPickDown,
            Auto_OnlinePosFingerAction,
            Auto_OnlinePosPickUp,
            Auto_OnlinePosCheckFinger,

            // 取：假电池线
            Auto_CalcFakePickPos,
            Auto_FakePosSetEvent,
            Auto_FakeScanPosMove,
            Auto_FakeScanPosDown,
            Auto_FakeScanPosScanCode,
            Auto_FakeScanPosUp,
            Auto_FakePosPickMove,
            Auto_FakePosPickDown,
            Auto_FakePosFingerAction,
            Auto_FakePosPickUp,
            Auto_FakePosCheckFinger,

            // 夹具扫码
            Auto_PalletScanCodeMove,
            Auto_PalletScanCodeDown,
            Auto_PalletScanCodeAction,
            Auto_PalletScanCodeUp,

            // 暂存：可取可防，主要看抓手操作
            Auto_CalcBufferPos,
            Auto_BufferPosSetEvent,
            Auto_BufferPosMove,
            Auto_BufferPosDown,
            Auto_BufferPosFingerAction,
            Auto_BufferPosUp,
            Auto_BufferPosCheckFinger,

            // 计算放位置
            Auto_CalcPlacePos,

            // 放：夹具
            Auto_CalcPalletPlacePos,
            Auto_PalletPlacePosSetEvent,
            Auto_PalletPlacePosPlaceMove,
            Auto_PalletPlacePosPlaceDown,
            Auto_PalletPlacePosFingerAction,
            Auto_PalletPlacePosPlaceUp,
            Auto_PalletPlacePosCheckFinger,
            Auto_MesUpdataCount,

            // 放：NG线
            Auto_CalcNGLinePlacePos,
            Auto_NGLinePosSetEvent,
            Auto_NGLinePosPlaceMove,
            Auto_NGLinePosPlaceDown,
            Auto_NGLinePosPlaceAction,
            Auto_NGLinePosPlaceUp,
            Auto_NGLinePosCheckFinger,

            // 放：待测试线
            Auto_CalcDetectPlacePos,
            Auto_DetectPosSetEvent,
            Auto_DetectPosPlaceMove,
            Auto_DetectPosPlaceDown,
            Auto_DetectPosPlaceAction,
            Auto_DetectPosPlaceUp,
            Auto_DetectPosCheckFinger,

            //人工
            //Manual_PalletScanCode,
            //Manual_PickDetect,
            //Manual_WaitFakeScanCode,
            //Manual_PlaceFake,
            //Manual_WaitBatteryScanCode,
            //Manual_PlaceBattery,

            Auto_WorkEnd,
        }

        private enum ModDef
        {
            // 夹爪必须从0起
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
            Start = ModuleMsgID.OnloadRobotMsgStartID,
            RbtDelayEStop,
            SafeDoorOpenRbtEStop,
            RbtActionChange,
            SendRbtMoveCmd,
            RbtMoveCmdError,
            RbtMoveTimeout,
            BufStationDownAlm,
            ScanCodeFail,
            ScanCodeTimeout,
            CodeLenError,
            CodeTypeError,
            CheckPallet,
            BindPallet,
            UnbindPallet,
            WaitFakeBatTimeout,
            BattryPickPosSave,
            ManualAction,
        }

        private enum AvoidStatus
        {
            Safe,
            UnSafe,
        }

        #endregion
        #region
        public enum PSignal
        {
            OnePick,
            TwoPick,
            ThreePick,
            OnePlace,
            TwoPlace,
            ThreePlace,
        }
        #endregion

        #region // 取放位置结构体

        private struct PickPlacePos
        {
            #region // 字段
            public OnloadRobotStation station;
            public int row;
            public int col;
            public int finger;
            public bool fingerClose;
            public MotorPosition motorPos;
            #endregion

            #region // 方法

            public void SetData(OnloadRobotStation curStation, int curRow, int curCol, int curFinger, bool figClose, MotorPosition curMotorPos)
            {
                this.station = curStation;
                this.row = curRow;
                this.col = curCol;
                this.finger = curFinger;
                this.fingerClose = figClose;
                this.motorPos = curMotorPos;
            }

            public void Release()
            {
                this.station = OnloadRobotStation.InvalidStatioin;
                this.row = -1;
                this.col = -1;
                this.finger = 0;
                this.fingerClose = false;
                this.motorPos = MotorPosition.Invalid;
            }
            #endregion
        };
        #endregion

        #region // 字段，属性

        #region // IO

        private int[] IPalletKeepFlatLeft;      // 夹具放平检测左：配置取反+逻辑取反
        private int[] IPalletKeepFlatRight;     // 夹具放平检测右：配置取反+逻辑取反
        private int[] IPalletHasCheck;          // 夹具位有夹具检测
        private int[] IPalletInposCheck;        // 夹具到位检测
        private int[] IFingerOpen;              // 抓手打开到位
        private int[] IFingerClose;             // 抓手关闭到位
        private int[] IFingerCheck;             // 抓手有料检查
        private int[] IBufferCheck;             // 暂存有料检查
        private int IFingerDelay;               // 抓手碰撞防呆检测
        private int IRobotRunning;              // 机器人运行中输入
        /// <summary>
        /// 间距气缸推出到位
        /// </summary>
        private int IIntervalCylPush;           // 间距气缸推出
        /// <summary>
        /// 间距气缸回退到位
        /// </summary>
        private int IIntervalCylPull;           // 间距气缸回退

        /// <summary>
        /// 夹具位报警
        /// </summary>
        private int[] OPalletAlarm;             // 夹具位报警
        /// <summary>
        /// 抓手打开
        /// </summary>
        private int[] OFingerOpen;              // 抓手打开
        /// <summary>
        /// 抓手关闭
        /// </summary>
        private int[] OFingerClose;             // 抓手关闭
        /// <summary>
        /// 机器人急停输出
        /// </summary>
        private int ORobotEStop;                // 机器人急停输出
        /// <summary>
        /// 间距气缸推出
        /// </summary>
        private int OIntervalCylPush;           // 间距气缸推出
        /// <summary>
        /// 间距气缸回退
        /// </summary>
        private int OIntervalCylPull;           // 间距气缸回退

        #endregion

        #region // 电机

        private int MotorU;         // 调宽电机
        #endregion

        #region // ModuleEx.cfg配置

        public RobotIndexID RobotID { get; private set; }   // 机器人ID

        #endregion

        #region // 模组参数
        public string localIP = "127.0.0.1";//*/
        public string onloadIP = "";//*/
        public int onloadPort = 9600;//*/
        public bool onloadConnected = false;//*/

        public bool OnloadClear { get; set; }                   // 上料清尾料
        public bool autoOnLoadBattery;                          // 自动上料/手动上料
        public int RobotLowSpeed { get; private set; }          // 机器人低速速度：1-80，用以手动调试
        public bool[] PalletPosEnable { get; private set; }     // 夹具位使能：TRUE启用，FALSE禁用

        /// <summary>
        /// 夹具放假电池行：机构干涉，仅支持0号抓手取放，且固定在第一次放
        /// </summary>
        private int placeFakeRow;           // 夹具放假电池行：机构干涉，仅支持0号抓手取放，且固定在第一次放
        /// <summary>
        /// 夹具放假电池列
        /// </summary>
        private int placeFakeCol;		    // 夹具放假电池列
        /// <summary>
        /// 全检模式下上假电池夹具：0=优先假电池夹具，1=夹具不上假电池，2=夹具全上假电池
        /// </summary>
        private int placeFakePlt;
        /// <summary>
        /// 上料测试待测假电池：TRUE启用，FALSE禁用
        /// </summary>
        private bool detectFakeBat;         // 上料测试待测假电池：TRUE启用，FALSE禁用
        /// <summary>
        /// 上料放NG夹具转盘NG电池：TRUE启用，FALSE禁用
        /// </summary>
        private bool placeNGPallet;         // 上料放NG夹具转盘NG电池：TRUE启用，FALSE禁用
        /// <summary>
        /// 上料NG夹具转盘限制：TRUE启用，FALSE禁用
        /// </summary>
        //private bool tranNGPalletLimit;     // 上料NG夹具转盘限制：TRUE启用，FALSE禁用
        /// <summary>
        /// 最后位置放置待测假电池夹具
        /// </summary>
        private bool lastPosPlaceDetectFake;

        private bool robotEnable;           // 机器人使能：TRUE启用，FALSE禁用
        private int robotSpeed;             // 机器人速度：1-100
        private int robotSpeedUp;           // 机器人速度上升速度：1-100
        private int robotSpeedDown;         // 机器人速度下降速度：1-100
        private int robotDelay;             // 机器人防呆时间(s)
        private int fingerPullDelay;        // 夹爪间距气缸回退延迟
        private string robotIP;             // 机器人IP
        private int robotPort;              // 机器人IP的Port
        private bool scanEnable;            // 扫码使能：TRUE启用，FALSE禁用
        private string scanCmd;             // 扫码器的扫码指令
        private bool scanLinefeed;          // 扫码器的扫码结束符
        private string barcodeScanIP;       // 扫码器的IP：进行网口通讯则填，否则为空
        private int barcodeScanCom;         // 扫码器的COM口：进行串口通讯则填，否则为-1
        private int barcodeScanPort;        // 扫码器的Port
        private int codeLength;             // 条码长度：-1则不检查
        private string codeType;            // 条码类别：空则不检查，多种类别以英文逗号(,)分隔
        private string scanNGType;          // 扫码NG字符：空则不检查
        private int scanMaxCount;           // 最大扫码次数：（X≥1）
        private bool scanPalletEnable;      // 扫夹具条码使能：TRUE启用，FALSE禁用
        private bool scanFakeBatEnable;     // 扫假电池条码使能：TRUE启用，FALSE禁用
        private int waitFakeDelay;          // 等待假电池防呆时间(s)
        public bool TranSaftResult { get; set; }                   // 调度安全位
        #endregion

        #region // 模组数据
        public OnloadData onloadData;                //上料数据
        public OnloadClient onloadClient;              // 干燥炉客户端
        private int heartBeatNum;                       // 心跳数 
        private bool heartBeatState;                    // 心跳状态
        private DateTime heartBeatTime;                 // 心跳时间

        private Task bgThread;                          // 后台线程
        private bool isRunThread;                       // 指示线程运行
        private bool connectState;                      // 当前连接状态（提示用）
        // 配置关联模组
        RunProcessOnloadLine pickBatRun;            // 取电池模组：PickBatRun = 
        RunProcessOnloadFake pickFakeRun;           // 取假电池模组：PickFakeRun = 
        RunProcessOnloadNG placeNGRun;              // 放NG电池模组：PlaceNGRun = 
        RunProcessDetectFake placeFakeRun;          // 放假电池模组：PlaceFakeRun = 

        RunProcessRobotTransfer transferRoot;            // 调度模组：PickBatRun = 
        private RobotActionInfo rotoActionInfo;             // 调度机器人手动信息
        //手动扫码
        RunProcessOnloadScan OnloadScanRun;

        public Battery manualBattery;
        public bool manualEndFlag;


        public bool RobotRunning { get; private set; }      // 机器人运行中

        private int placePallet;                      // 放夹具索引
        public int transAvoid;                      // 避让位
        private PickPlacePos pickPos;                 // 取位置
        private PickPlacePos placePos;                // 放位置
        private int[] robotCmd;                       // 机器人指令
        private EventList avoidEvent;                 // 避让大机器人事件
        private RobotActionInfo robotAutoAction;      // 机器人自动动作信息
        private RobotActionInfo robotDebugAction;     // 机器人手动调试动作信息
        private RobotClient robotClient;              // 机器人通讯
        private BarcodeScan barcodeScan;              // 扫码器
        private string[] codeTypeArray;               // 条码类型列表
        private bool robotNeedEStop;                  // 机器人可以触发急停ON
        private Dictionary<OnloadRobotStation, RobotFormula> robotStationInfo;  // 机器人工位信息
        private DateTime stepDelayTime;               // 步骤防呆计时
        private int pltIntervalPos;                   // 夹具中取放电池时的间隔位置
        private bool pltPlaceNgBat;
        private List<string>[] arrayListBatCode;      //绑盘电池信息

        public Battery[] reBattery;                   //复投电池信息

        #endregion

        #endregion

        public RunProcessOnloadRobot(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.Finger_Buffer_ALL, (int)ModuleMaxPallet.OnloadRobot);

            PowerUpRestart();

            InitParameter();

            // 参数
            InsertVoidParameter("OnloadIP", "上料IP", "上料IP", onloadIP, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_ADMIN);
            InsertVoidParameter("OnloadPort", "上料端口", "上料端口", onloadPort, RecordType.RECORD_INT, ParameterLevel.PL_STOP_ADMIN);


            InsertVoidParameter("OnloadClear", "上料清尾料", "上料清尾料，不想上料时开启，想上料时关闭：true开启上料清尾料，false正常上料", OnloadClear, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            InsertVoidParameter("OnLoadBatteryType", "夹具电池上料方式", "true：机器人自动上料 false：人工扫码上料 ", autoOnLoadBattery, RecordType.RECORD_BOOL);
            InsertVoidParameter("detectFakeBat", "测试假电池", "上料测试待测水含量电池：TRUE启用，FALSE禁用", detectFakeBat, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("placeNGPallet", "NG夹具转盘", "上料从NG夹具中转移OK电池至OK夹具：TRUE启用，FALSE禁用", placeNGPallet, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("placeFakePlt", "上假电池夹具方式", "全检模式下上假电池夹具：0=优先假电池夹具，1=夹具不上假电池，2=夹具全上假电池", placeFakePlt, RecordType.RECORD_INT);
            InsertVoidParameter("placeFakeCol", "夹具放假电池列", $"夹具放假电池列≤{(int)PalletRowCol.MaxCol}", placeFakeCol, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("lastPosPlaceDetectFake", "末位放置待测假电池夹具", "末位放置待测假电池夹具：TRUE=夹具顺序最后位置，FALSE=不指定位置", lastPosPlaceDetectFake, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotEnable", "机器人使能", "机器人使能：TRUE启用，FALSE禁用", robotEnable, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotSpeed", "机器人速度", "机器人速度：1-100", robotSpeed, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotSpeedUp", "机器人上升速度", "机器人上升速度：1-100", robotSpeedUp, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotSpeedDown", "机器人下降速度", "机器人下降速度：1-100", robotSpeedDown, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("RobotLowSpeed", "机器人调试速度", "机器人手动调试速度：1-100", RobotLowSpeed, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotDelay", "机器人防呆", "机器人防呆时间(s)", robotDelay, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("robotIP", "机器人IP", "机器人IP", robotIP, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_ADMIN);
            InsertVoidParameter("robotPort", "机器人端口", "机器人IP的Port", robotPort, RecordType.RECORD_INT, ParameterLevel.PL_STOP_ADMIN);
            InsertVoidParameter("fingerPullDelay", "夹爪间距气缸回退延迟", "夹爪间距气缸回退延迟时间（毫秒）", fingerPullDelay, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            for (int i = 0; i < (int)ModuleMaxPallet.OnloadRobot; i++)
            {
                InsertVoidParameter(("PalletPosEnable" + i), ("夹具位" + (i + 1) + "使能"), "夹具位使能：TRUE启用，FALSE禁用", PalletPosEnable[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            }
            InsertVoidParameter("scanEnable", "扫码器使能", "扫码器使能：TRUE启用，FALSE禁用", scanEnable, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanPalletEnable", "扫夹具条码", "扫夹具条码使能：TRUE启用，FALSE禁用", scanPalletEnable, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanFakeBatEnable", "扫假电池条码", "扫假电池条码使能：TRUE启用，FALSE禁用", scanFakeBatEnable, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            InsertVoidParameter("scanMaxCount", "最大扫码次数", "最大扫码次数：（X≥1）", scanMaxCount, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanCmd", "扫码指令", "触发扫码的指令", scanCmd, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanLinefeed", "扫码结束符", "扫码器的扫码结束符：true有回车换行结束符，false无结束符", scanLinefeed, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("barcodeScanIP", "扫码器的IP", "扫码器的IP：进行网口通讯则填，否则为空", barcodeScanIP, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            //InsertVoidParameter("barcodeScanCom", "扫码器的COM口", "扫码器的COM口：进行串口通讯则填，否则为-1", barcodeScanCom, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("barcodeScanPort", "扫码器的Port", "扫码器的端口号/波特率", barcodeScanPort, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("codeLength", "条码长度", "条码长度：-1则不检查", codeLength, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("codeType", "条码类别", "条码类别：空则不检查，多种类别以英文逗号(,)分隔", codeType, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanNGType", "扫码NG字符", "扫码NG时扫码器反馈字符：空则不检查", scanNGType, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("waitFakeDelay", "假电池防呆", "等待假电池防呆时间(s)，超时则报警提示", waitFakeDelay, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
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
                        this.nextInitStep = InitSteps.Init_DeviceConnect;
                        break;
                    }
                case InitSteps.Init_DeviceConnect:
                    {
                        CurMsgStr("连接上料", "Connect robot");

                        if (this.DryRun || OnloadConnect(true))
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
        private bool BindPalletByBattery(Pallet plt, List<string> arrayList)
        {
            try
            {
                if (plt.State == PalletStatus.Invalid)
                {
                    arrayList.Clear();
                }
                if (plt.State == PalletStatus.OK)
                {
                    string msg = "";
                    for (int rowsIdx = 0; rowsIdx < plt.Battery.GetLength(0); rowsIdx++)
                    {
                        for (int colsIdx = 0; colsIdx < plt.Battery.GetLength(1); colsIdx++)
                        {
                            if (plt.Battery[rowsIdx, colsIdx].Type == BatteryStatus.OK
                                && !string.IsNullOrEmpty(plt.Battery[rowsIdx, colsIdx].Code)
                                && !arrayList.Contains(plt.Battery[rowsIdx, colsIdx].Code))
                            {
                                //调用绑盘接口
                                if (!MachineCtrl.GetInstance().ACINBOUNDByOne_Main(MesResources.Equipment, plt.Code, plt.Battery[rowsIdx, colsIdx].Code, (rowsIdx * plt.Battery.GetLength(1) + colsIdx + 1), true, true, ref msg))
                                {
                                    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                                }
                                arrayList.Add(plt.Battery[rowsIdx, colsIdx].Code);
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {

            }
            return false;
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
                Sleep(50);
            }
            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        bool onloadClear = this.OnloadClear;


                        #region // 设置检查取放请求
                        for (int idx = 0; idx < this.Pallet.Length; idx++)
                        {
                            #region //绑盘
                            //BindPalletByBattery(this.Pallet[idx], arrayListBatCode[idx]);
                            #endregion
                            #region // 夹具上料完成->请求取
                            if ((PalletStatus.OK == this.Pallet[idx].State || PalletStatus.Turn == this.Pallet[idx].State)
                                && (PalletStage.Onload != this.Pallet[idx].Stage)
                                && (onloadData.palletDataArray[idx].clearFlag || this.Pallet[idx].IsFull())
                                && this.PalletPosEnable[idx])
                            {
                                string msg = "";
                                this.placePallet = -1;
                                // 一次上传绑盘，需去掉放料时绑盘
                                //if (Def.IsNoHardware() || MesOperate.EquToMesBindContainer(MesResources.Equipment, this.Pallet[idx], ref msg))
                                //{
                                //    this.Pallet[idx].Stage = PalletStage.Onload;
                                //    SaveRunData(SaveType.Variables | SaveType.Pallet, idx);
                                //}
                                if (!Def.IsNoHardware()/*|| MachineCtrl.GetInstance().ACINBOUND_Main(MesResources.Equipment,this.Pallet[idx],false,true,ref msg)*/)
                                {
                                    this.Pallet[idx].Stage = PalletStage.Onload;
                                    SaveRunData(SaveType.Variables | SaveType.Pallet, idx);
                                }
                                else
                                {
                                    ShowMessageID((int)MsgID.BindPallet, msg, $"请检查 {this.Pallet[idx].Code} 的绑盘信息", MessageType.MsgAlarm);
                                }
                            }
                            #endregion 夹具上料完成

                            EventList modEvent = EventList.Invalid;
                            EventStatus state = EventStatus.Invalid;

                            #region // 有空位 -> 请求放
                            if ((PalletStatus.Invalid == this.Pallet[idx].State)
                                && this.PalletPosEnable[idx])
                            {
                                // 上料区放待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
                                modEvent = EventList.OnloadPlaceReputFakePallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }

                                // 上料区放空夹具
                                modEvent = EventList.OnloadPlaceEmptyPallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }

                                // 上料区放待检测含假电池夹具（未取走假电池的夹具）
                                //if (this.detectFakeBat && (!this.lastPosPlaceDetectFake || (this.lastPosPlaceDetectFake && ((int)ModuleMaxPallet.OnloadRobot - 1) == idx)))
                                {
                                    modEvent = EventList.OnLoadPlaceDetectFakePallet;
                                    state = GetEvent(this, modEvent);
                                    if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                    {
                                        SetEvent(this, modEvent, EventStatus.Require, idx);
                                    }
                                }
                                // 仅上料区最后一个位置放转NG
                                if (((int)ModuleMaxPallet.OnloadRobot - 1) == idx)
                                {
                                    // 上料区放NG非空夹具，转盘
                                    if (this.placeNGPallet)
                                    {
                                        modEvent = EventList.OnloadPlaceNGPallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, idx);
                                        }
                                    }
                                }
                            }
                            #endregion 有空位 -> 请求放

                            #region // 上料区有NG空夹具 -> 请求取
                            if ((PalletStatus.NG == this.Pallet[idx].State) && this.Pallet[idx].IsEmpty() && this.PalletPosEnable[idx])
                            {
                                modEvent = EventList.OnloadPickNGEmptyPallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            #endregion 上料区有NG空夹具 -> 请求取

                            #region // 上料区取OK满夹具 -> 请求取
                            if ((PalletStatus.OK == this.Pallet[idx].State || PalletStatus.Turn == this.Pallet[idx].State)
                                && (PalletStage.Onload == this.Pallet[idx].Stage)
                                && (onloadData.palletDataArray[idx].clearFlag || this.Pallet[idx].IsFull())
                                && !this.Pallet[idx].IsEmpty()
                                && !this.Pallet[idx].HasFake()
                                && this.PalletPosEnable[idx])
                            {
                                modEvent = EventList.OnloadPickOKFullPallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            #endregion 上料区取OK满夹具 -> 请求取

                            #region // 上料区OK带假电池满夹具 -> 请求取
                            if ((PalletStatus.OK == this.Pallet[idx].State || PalletStatus.Turn == this.Pallet[idx].State)
                                && (PalletStage.Onload == this.Pallet[idx].Stage)
                                && (onloadData.palletDataArray[idx].clearFlag || this.Pallet[idx].IsFull())
                                && this.Pallet[idx].HasFake()
                                && this.PalletPosEnable[idx])
                            {
                                modEvent = EventList.OnloadPickOKFakeFullPallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            #endregion 上料区OK带假电池满夹具 -> 请求取

                            #region // 上料区待回炉假电池夹具（已放回假电池的夹具） -> 请求取
                            if ((PalletStatus.Rebaking == this.Pallet[idx].State)
                                //&& (onloadData.palletDataArray[idx].clearFlag || this.Pallet[idx].IsFull())
                                && this.Pallet[idx].HasFake()
                                && this.PalletPosEnable[idx])
                            {
                                modEvent = EventList.OnloadPickRebakeFakePallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            #endregion 上料区待回炉假电池夹具（已放回假电池的夹具） -> 请求取


                            #region // 上料区取等待水含量结果夹具（已取待测假电池的夹具）
                            if ((PalletStatus.WaitResult == this.Pallet[idx].State)
                                //&& this.Pallet[idx].HasFake()
                             && onloadData.palletDataArray[idx].enable)
                            {
                                modEvent = EventList.OnLoadPickWaitResultPallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require, idx);
                                }
                            }
                            #endregion 上料区取等待水含量结果夹具（已取待测假电池的夹具）
                        }
                        #endregion

                        #region // 有取放已响应
                        for (EventList i = EventList.OnloadPlaceEmptyPallet; i < EventList.OnloadPickPlaceEnd; i++)
                        {
                            if (EventStatus.Response == GetEvent(this, i))
                            {
                                this.avoidEvent = i;
                                this.nextAutoStep = AutoSteps.Auto_RobotMoveAvoidPos;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                return;
                            }
                        }
                        #endregion
                        //#region // 无任务时回来料位等待
                        //if (!this.AutoStepSafe && autoOnLoadBattery)
                        //{
                        //    if (this.DryRun || (GetRobotCmd(OnloadRobotStation.OnloadLine, 0, 0, robotSpeed, RobotOrder.MOVE, ref this.robotCmd)
                        //        && RobotMotorMove(this.robotCmd, MotorPosition.Onload_LinePickPos)))
                        //    {
                        //        this.AutoStepSafe = true;
                        //    }
                        //}
                        //#endregion
                        break;
                    }

                #region // 避让大机器人
                case AutoSteps.Auto_RobotMoveAvoidPos:
                    {
                        CurMsgStr("机器人移动到避让位", "Robot move to avoid pos");
                        if (!Def.IsNoHardware() /*|| RobotHome()*/)
                        {
                            int pltIdx = -1;
                            if (EventStatus.Response == GetEvent(this, this.avoidEvent, ref pltIdx))
                            {
                                DateTime dt = DateTime.Now;
                                string msg = "";
                                bool res = false;

                                while ((DateTime.Now - dt).TotalSeconds < 60)
                                {
                                    if (RobotHome(pltIdx, true))
                                    {
                                        msg = string.Format("上料机器人未在避让位！！！");
                                        if (onloadData.avoidMove)
                                        {
                                            msg = string.Format("上料机工位未检测到允许取放信号！！！");
                                            if (CheckPickOrPlaceStatus_(this.avoidEvent, pltIdx))
                                            {
                                                this.transAvoid = 0;
                                                if (SetEvent(this, this.avoidEvent, EventStatus.Ready, pltIdx))
                                                {
                                                    res = true;
                                                    this.nextAutoStep = AutoSteps.Auto_WaitActionFinish;
                                                    
                                                    SaveRunData(SaveType.AutoStep | SaveType.Avoid);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (!res)
                                {
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                                    MachineCtrl.GetInstance().RunsCtrl.Stop();
                                }
                                //ManualResetEvent();
                                //ClearEvent();
                                //RunProcessRobotTransfer robotTran = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
                                //robotTran.ManualResetEvent();
                             
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_WaitActionFinish:
                    {
                        CurMsgStr("等待取放动作完成", "Robot wait action finish");
                        int pltIdx = -1;
                        if ((EventStatus.Finished == GetEvent(this, this.avoidEvent, ref pltIdx))
                            && ((pltIdx > -1) && (pltIdx < (int)ModuleMaxPallet.OnloadRobot)))
                        {
                            if (RobotHome(pltIdx, false))
                            {
                                Sleep(100);
                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                this.transAvoid = 1;
                                SaveRunData(SaveType.AutoStep | SaveType.Pallet | SaveType.Avoid, pltIdx);
                            }
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
                        Trace.Assert(false, "RunProcessOnloadRobot.AutoOperation/no this run step");
                        break;
                    }
            }
        }

        public override void ManulSetAutoStep(int step)
        {
            this.nextAutoStep = (AutoSteps)step;
            SaveRunData(SaveType.AutoStep);
        }

        /// <summary>
        /// 人工上料计算夹具中放电池位置
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcManulaOnloadPlacePalletPos(int placePlt, ref PickPlacePos curPos)
        {
            if (placePlt < 0 || placePlt >= (int)ModuleMaxPallet.OnloadRobot)
            {
                return false;
            }
            OnloadRobotStation station = (OnloadRobotStation)((int)OnloadRobotStation.PalletStation_0 + placePlt);
            // 放OK电池
            int pltRow, pltCol;
            pltRow = pltCol = -1;
            if (GetPalletCurPlaceRowCol(placePlt, ref pltRow, ref pltCol))
            {
                curPos.SetData(station, pltRow, pltCol, 0, false, MotorPosition.Onload_PalletPos);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 人工上料获取夹具当前需要电池的行列
        /// </summary>
        /// <param name="pltRow"></param>
        /// <param name="pltCol"></param>
        /// <returns></returns>
        private bool GetManualOnloadPalletCurPlaceRowCol(int pltIndex, ref int pltRow, ref int pltCol)
        {
            for (int col = 0; col < this.Pallet[pltIndex].MaxCol; col++)
            {
                for (int row = 0; row < this.Pallet[pltIndex].MaxRow; row++)
                {
                    if ((BatteryStatus.Invalid == this.Pallet[pltIndex].Battery[row, col].Type))
                    {
                        pltRow = row;
                        pltCol = col;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算重新烘烤夹具中放电池位置
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool GetReFakePltIdxAndBatteryPosFunc(int placePlt, ref PickPlacePos curPos)
        {
            int i = 0;

            for (; i < this.Pallet.Length; i++)
            {
                if (this.PalletPosEnable[i] && (PalletStatus.ReputFake == this.Pallet[i].State) && this.Pallet[i].HasFake())
                {
                    placePlt = i;
                }
            }
            if (i == this.Pallet.Length) return false;

            for (int col = 0; col < this.Pallet[placePlt].MaxCol; col++)
            {
                for (int row = 0; row < this.Pallet[placePlt].MaxRow; row++)
                {
                    if ((BatteryStatus.FakeTag == this.Pallet[placePlt].Battery[row, col].Type))
                    {
                        curPos.SetData((OnloadRobotStation)((int)OnloadRobotStation.PalletStation_0 + placePlt), row, col, 0, false, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 人工上料夹具需要取待测假电池
        /// </summary>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool ManualOnloadPltNeedPickDetectFake(ref PickPlacePos curPickPos)
        {
            int fakeRow, fakeCol;
            fakeRow = fakeCol = -1;
            for (int i = 0; i < (int)ModuleMaxPallet.OnloadRobot; i++)
            {
                if ((PalletStatus.Detect == this.Pallet[i].State) && this.Pallet[i].GetFakePos(ref fakeRow, ref fakeCol))
                {
                    curPickPos.SetData((OnloadRobotStation.PalletStation_0 + i), fakeRow, fakeCol, 0, true, MotorPosition.Onload_PalletPos);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 夹具需要人工扫码
        /// </summary>
        /// <param name="pickPos"></param>
        /// <returns></returns>
        private bool PltNeedManualScanCode(ref PickPlacePos pPick)
        {
            if (scanPalletEnable)
            {
                for (int i = 0; i < this.Pallet.Length; i++)
                {
                    if (this.PalletPosEnable[i] && (PalletStatus.OK == this.Pallet[i].State)
                        && ("" == this.Pallet[i].Code) && this.Pallet[i].IsEmpty())
                    {
                        pPick.SetData((OnloadRobotStation)((int)OnloadRobotStation.ScanPalletCode_0 + i), 0, 0, 0, false, MotorPosition.Onload_ScanPalletPos);
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region // 运行数据读写

        public override void InitRunData()
        {
            this.placePallet = -1;
            this.pickPos.Release();
            this.placePos.Release();
            if (null == this.robotCmd)
            {
                this.robotCmd = new int[(int)RobotCmdFormat.End];
            }
            this.robotCmd.Initialize();
            if (null == this.robotAutoAction)
            {
                this.robotAutoAction = new RobotActionInfo();
            }
            this.robotAutoAction.Release();
            if (null == this.robotDebugAction)
            {
                this.robotDebugAction = new RobotActionInfo();
            }
            this.robotDebugAction.Release();
            if (null == this.robotClient)
            {
                this.robotClient = new RobotClient();
            }
            if (null == this.barcodeScan)
            {
                this.barcodeScan = new BarcodeScan();
            }
            this.avoidEvent = EventList.Invalid;
            this.robotNeedEStop = true;

            base.InitRunData();
        }

        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);
            this.placePallet = iniStream.ReadInt(section, "placePallet", this.placePallet);
            this.transAvoid = iniStream.ReadInt(section, "transAvoid", this.transAvoid);

            key = string.Format("pickPos.station");
            this.pickPos.station = (OnloadRobotStation)iniStream.ReadInt(section, key, (int)this.pickPos.station);
            key = string.Format("pickPos.row");
            this.pickPos.row = iniStream.ReadInt(section, key, this.pickPos.row);
            key = string.Format("pickPos.col");
            this.pickPos.col = iniStream.ReadInt(section, key, this.pickPos.col);
            key = string.Format("pickPos.finger");
            this.pickPos.finger = iniStream.ReadInt(section, key, (int)this.pickPos.finger);
            key = string.Format("pickPos.fingerClose");
            this.pickPos.fingerClose = iniStream.ReadBool(section, key, this.pickPos.fingerClose);
            key = string.Format("pickPos.motorPos");
            this.pickPos.motorPos = (MotorPosition)iniStream.ReadInt(section, key, (int)this.pickPos.motorPos);

            key = string.Format("placePos.station");
            this.placePos.station = (OnloadRobotStation)iniStream.ReadInt(section, key, (int)this.placePos.station);
            key = string.Format("placePos.row");
            this.placePos.row = iniStream.ReadInt(section, key, this.placePos.row);
            key = string.Format("placePos.col");
            this.placePos.col = iniStream.ReadInt(section, key, this.placePos.col);
            key = string.Format("placePos.finger");
            this.placePos.finger = iniStream.ReadInt(section, key, (int)this.placePos.finger);
            key = string.Format("placePos.fingerClose");
            this.placePos.fingerClose = iniStream.ReadBool(section, key, this.placePos.fingerClose);
            key = string.Format("placePos.motorPos");
            this.placePos.motorPos = (MotorPosition)iniStream.ReadInt(section, key, (int)this.placePos.motorPos);

            for (int i = 0; i < this.robotCmd.Length; i++)
            {
                key = string.Format("robotCmd[{0}]", i);
                this.robotCmd[i] = iniStream.ReadInt(section, key, this.robotCmd[i]);
            }
            this.avoidEvent = (EventList)iniStream.ReadInt(section, "avoidEvent", (int)this.avoidEvent);

            base.LoadRunData();
        }

        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);
            if (SaveType.Avoid == (SaveType.Avoid & saveType))
            {
                iniStream.WriteInt(section, "transAvoid", this.transAvoid);
            }
            if (SaveType.Variables == (SaveType.Variables & saveType))
            {
                iniStream.WriteInt(section, "placePallet", this.placePallet);


                string[] posName = new string[] { "pickPos", "placePos" };
                PickPlacePos[] pos = new PickPlacePos[] { pickPos, placePos };
                for (int i = 0; i < pos.Length; i++)
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
                    key = string.Format("{0}.motorPos", posName[i]);
                    iniStream.WriteInt(section, key, (int)pos[i].motorPos);
                }
                iniStream.WriteInt(section, "avoidEvent", (int)this.avoidEvent);
            }
            if (SaveType.Robot == (SaveType.Robot & saveType))
            {
                for (int i = 0; i < this.robotCmd.Length; i++)
                {
                    key = string.Format("robotCmd[{0}]", i);
                    iniStream.WriteInt(section, key, this.robotCmd[i]);
                }
                string[] rbtActionName = new string[] { "robotAutoAction", "robotDebugAction" };
                RobotActionInfo[] rbtAction = new RobotActionInfo[] { robotAutoAction, robotDebugAction };
                for (int i = 0; i < rbtAction.Length; i++)
                {
                    key = string.Format("{0}.station", rbtActionName[i]);
                    iniStream.WriteInt(section, key, rbtAction[i].station);
                    key = string.Format("{0}.row", rbtActionName[i]);
                    iniStream.WriteInt(section, key, rbtAction[i].row);
                    key = string.Format("{0}.col", rbtActionName[i]);
                    iniStream.WriteInt(section, key, rbtAction[i].col);
                    key = string.Format("{0}.order", rbtActionName[i]);
                    iniStream.WriteInt(section, key, (int)rbtAction[i].order);
                    key = string.Format("{0}.stationName", rbtActionName[i]);
                    iniStream.WriteString(section, key, rbtAction[i].stationName);
                }
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
            if (!InitThread())
            {
                ShowMsgBox.ShowDialog((module + " 后台线程初始化失败"), MessageType.MsgWarning);
                return false;
            }


            string type = IniFile.ReadString(this.RunModule, "RobotType", "", Def.GetAbsPathName(Def.ModuleExCfg));
            if (string.IsNullOrEmpty(type) || !this.robotClient.SetRobotType(type))
            {
                string msg = string.Format("RobotType = {0} 配置错误！", type);
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            this.RobotID = (RobotIndexID)IniFile.ReadInt(this.RunModule, "RobotID", (int)RobotIndexID.Invalid, Def.GetAbsPathName(Def.ModuleExCfg));
            if (this.RobotID <= RobotIndexID.Invalid || this.RobotID >= RobotIndexID.End)
            {
                string msg = string.Format("RobotID = {0} 配置错误，应该为{1} < RobotID < {2}", (int)this.RobotID, (int)RobotIndexID.Invalid, (int)RobotIndexID.End);
                ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
            }
            this.robotClient.SetRobotInfo((int)this.RobotID, this.RunName);
            InitRobotStation();

            return true;
        }

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            onloadData = new OnloadData();
            //onloadData.palletData = new PalletData[3];
            this.heartBeatTime = DateTime.Now;
            onloadClient = new OnloadClient();
            this.localIP = "127.0.0.1";
            this.onloadIP = "";
            this.onloadPort = 9600;
            this.onloadConnected = false;
            this.transAvoid = 1;

            this.placeFakeRow = 1;
            this.placeFakeCol = 1;
            this.placeFakePlt = 0;
            this.placeNGPallet = false;
            this.detectFakeBat = false;
            this.robotEnable = false;
            this.robotSpeed = 10;
            this.RobotLowSpeed = 10;
            this.robotSpeedUp = 10;
            this.robotSpeedDown = 10;
            this.robotDelay = 60;
            this.fingerPullDelay = 500;
            this.robotIP = "";
            this.robotPort = 0;
            this.scanPalletEnable = true;
            this.autoOnLoadBattery = true;
            if (null == this.PalletPosEnable)
            {
                this.PalletPosEnable = new bool[(int)ModuleMaxPallet.OnloadRobot];
            }
            //this.PalletPosEnable.Initialize();
            this.lastPosPlaceDetectFake = false;
            this.scanEnable = true;
            this.scanCmd = "Start";
            this.scanLinefeed = true;
            this.barcodeScanIP = string.Empty;
            this.barcodeScanCom = -1;
            this.barcodeScanPort = 0;
            this.codeLength = -1;
            this.codeType = string.Empty;
            this.scanNGType = "ERROR";
            this.scanMaxCount = 1;
            this.RobotRunning = false;
            this.scanFakeBatEnable = true;
            this.waitFakeDelay = 120;
            this.stepDelayTime = DateTime.Now;
            this.OnloadClear = false;
            this.pltIntervalPos = 0;

            this.manualBattery = new Battery();
            this.manualEndFlag = false;
            //删除上料扫码NG夹具
            //this.blDelReqEventPlt = false;
            // 正在倒盘
            this.pltPlaceNgBat = false;
            arrayListBatCode = new List<string>[(int)ModuleMaxPallet.OnloadRobot];
            for (int i = 0; i < arrayListBatCode.Length; i++)
            {
                arrayListBatCode[i] = new List<string>();
            }

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            this.onloadIP = ReadStringParameter(this.RunModule, "OnloadIP", this.onloadIP);
            this.onloadPort = ReadIntParameter(this.RunModule, "OnloadPort", this.onloadPort);


            this.placeFakeRow = 1;
            this.placeFakeCol = ReadIntParameter(this.RunModule, "placeFakeCol", this.placeFakeCol);
            this.placeFakePlt = ReadIntParameter(this.RunModule, "placeFakePlt", this.placeFakePlt);
            this.placeNGPallet = ReadBoolParameter(this.RunModule, "placeNGPallet", this.placeNGPallet);
            this.detectFakeBat = ReadBoolParameter(this.RunModule, "detectFakeBat", this.detectFakeBat);
            this.robotEnable = ReadBoolParameter(this.RunModule, "robotEnable", this.robotEnable);
            this.robotSpeed = ReadIntParameter(this.RunModule, "robotSpeed", this.robotSpeed);
            this.robotSpeedUp = ReadIntParameter(this.RunModule, "robotSpeedUp", this.robotSpeedUp);
            this.robotSpeedDown = ReadIntParameter(this.RunModule, "robotSpeedDown", this.robotSpeedDown);
            this.RobotLowSpeed = ReadIntParameter(this.RunModule, "RobotLowSpeed", this.RobotLowSpeed);
            this.robotDelay = ReadIntParameter(this.RunModule, "robotDelay", this.robotDelay);
            this.fingerPullDelay = ReadIntParameter(this.RunModule, "fingerCloseDealy", this.fingerPullDelay);
            this.robotIP = ReadStringParameter(this.RunModule, "robotIP", this.robotIP);
            this.robotPort = ReadIntParameter(this.RunModule, "robotPort", this.robotPort);
            this.scanPalletEnable = ReadBoolParameter(this.RunModule, "scanPalletEnable", this.scanPalletEnable);
            this.scanFakeBatEnable = ReadBoolParameter(this.RunModule, "scanFakeBatEnable", this.scanPalletEnable);
            this.OnloadClear = ReadBoolParameter(this.RunModule, "OnloadClear", this.OnloadClear);
            this.autoOnLoadBattery = ReadBoolParameter(this.RunModule, "OnLoadBatteryType", this.autoOnLoadBattery);
            for (int i = 0; i < (int)ModuleMaxPallet.OnloadRobot; i++)
            {
                this.PalletPosEnable[i] = ReadBoolParameter(this.RunModule, ("PalletPosEnable" + i), this.PalletPosEnable[i]);
            }
            this.lastPosPlaceDetectFake = ReadBoolParameter(this.RunModule, "lastPosPlaceDetectFake", this.lastPosPlaceDetectFake);
            this.scanEnable = ReadBoolParameter(this.RunModule, "scanEnable", this.scanEnable);
            this.scanCmd = ReadStringParameter(this.RunModule, "scanCmd", this.scanCmd);
            this.scanLinefeed = ReadBoolParameter(this.RunModule, "scanLinefeed", this.scanLinefeed);
            this.barcodeScanIP = ReadStringParameter(this.RunModule, "barcodeScanIP", this.barcodeScanIP);
            this.barcodeScanCom = ReadIntParameter(this.RunModule, "barcodeScanCom", this.barcodeScanCom);
            this.barcodeScanPort = ReadIntParameter(this.RunModule, "barcodeScanPort", this.barcodeScanPort);
            this.codeLength = ReadIntParameter(this.RunModule, "codeLength", this.codeLength);
            this.codeType = ReadStringParameter(this.RunModule, "codeType", this.codeType);
            this.codeTypeArray = this.codeType.Split((new char[] { ',' }), StringSplitOptions.RemoveEmptyEntries);
            this.scanNGType = ReadStringParameter(this.RunModule, "scanNGType", this.scanNGType);
            this.scanMaxCount = ReadIntParameter(this.RunModule, "scanMaxCount", this.scanMaxCount);

            pltPlaceNgBat = placeNGPallet;

            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            // 取电池模组
            this.pickBatRun = MachineCtrl.GetInstance().GetModule(RunID.OnloadLine) as RunProcessOnloadLine;
            // 假电池线
            this.pickFakeRun = MachineCtrl.GetInstance().GetModule(RunID.OnloadFake) as RunProcessOnloadFake;
            // NG输出线
            this.placeNGRun = MachineCtrl.GetInstance().GetModule(RunID.OnloadNG) as RunProcessOnloadNG;
            // 待测电池线
            this.placeFakeRun = MachineCtrl.GetInstance().GetModule(RunID.OnloadDetect) as RunProcessDetectFake;
            //手动扫码
            this.OnloadScanRun = MachineCtrl.GetInstance().GetModule(RunID.OnloadScan) as RunProcessOnloadScan;

            this.transferRoot = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            int maxPlt = (int)ModuleMaxPallet.OnloadRobot;
            this.IPalletKeepFlatLeft = new int[maxPlt];
            this.IPalletKeepFlatRight = new int[maxPlt];
            this.IPalletInposCheck = new int[maxPlt];
            this.IPalletHasCheck = new int[maxPlt];
            for (int i = 0; i < maxPlt; i++)
            {
                this.IPalletKeepFlatLeft[i] = AddInput("IPalletKeepFlatLeft" + i);
                this.IPalletKeepFlatRight[i] = AddInput("IPalletKeepFlatRight" + i);
                this.IPalletInposCheck[i] = AddInput("IPalletInposCheck" + i);
                this.IPalletHasCheck[i] = AddInput("IPalletHasCheck" + i);
            }
            int maxFinger = (int)ModDef.Finger_ALL;
            this.IFingerOpen = new int[maxFinger];
            this.IFingerClose = new int[maxFinger];
            this.IFingerCheck = new int[maxFinger];
            this.IBufferCheck = new int[maxFinger];
            for (int i = 0; i < maxFinger; i++)
            {
                this.IFingerOpen[i] = AddInput("IFingerOpen" + i);
                this.IFingerClose[i] = AddInput("IFingerClose" + i);
                this.IFingerCheck[i] = AddInput("IFingerCheck" + i);
            }
            this.IFingerDelay = AddInput("IFingerDelay");
            this.IIntervalCylPush = AddInput("IIntervalCylPush");
            this.IIntervalCylPull = AddInput("IIntervalCylPull");
            for (int i = 0; i < maxFinger; i++)
            {
                this.IBufferCheck[i] = AddInput("IBufferCheck" + i);
            }
            this.IRobotRunning = AddInput("IRobotRunning");

            this.OPalletAlarm = new int[maxPlt];
            for (int i = 0; i < maxPlt; i++)
            {
                this.OPalletAlarm[i] = AddOutput("OPalletAlarm" + i);
            }
            this.OFingerOpen = new int[maxFinger];
            this.OFingerClose = new int[maxFinger];
            for (int i = 0; i < maxFinger; i++)
            {
                this.OFingerOpen[i] = AddOutput("OFingerOpen" + i);
                this.OFingerClose[i] = AddOutput("OFingerClose" + i);
            }
            this.ORobotEStop = AddOutput("ORobotEStop");
            this.OIntervalCylPush = AddOutput("OIntervalCylPush");
            this.OIntervalCylPull = AddOutput("OIntervalCylPull");

            this.MotorU = AddMotor("MotorU");
        }

        /// <summary>
        /// 夹具放平检测
        /// </summary>
        /// <param name="pltIdx"></param>
        /// <param name="hasPlt"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public override bool PalletKeepFlat(int pltIdx, bool hasPlt, bool alarm = true)
        {
            if (pltIdx < 0 || pltIdx > (int)ModuleMaxPallet.OnloadRobot)
            {
                return false;
            }
            if (hasPlt)
            {
                if ((!onloadData.onloadSignal[pltIdx, 0]
               || !onloadData.onloadSignal[pltIdx, 1]
               || !onloadData.onloadSignal[pltIdx, 2]))
                {
                    if (alarm)
                    {

                    }
                    return false;
                }
            }
            else
            {
                if ((onloadData.onloadSignal[pltIdx, 0]
               || onloadData.onloadSignal[pltIdx, 1]
               || onloadData.onloadSignal[pltIdx, 2]))
                {
                    if (alarm)
                    {

                    }
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 夹爪间距气缸动作
        /// </summary>
        /// <param name="motorLoc"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private bool FingerIntervalPush(MotorPosition motorLoc, float offset = 0)
        {
            switch (motorLoc)
            {
                case MotorPosition.Onload_LinePickPos:
                case MotorPosition.Onload_ScanPalletPos:
                case MotorPosition.Onload_BufferPos:
                case MotorPosition.Onload_ScanFakePos:
                case MotorPosition.Onload_FakePos:
                case MotorPosition.Onload_NGPos:
                case MotorPosition.Onload_DetectPos:
                    {
                        //return FingerIntervalIncr(false, true); //wjj 220402
                        return FingerIntervalIncr(true, true);
                    }
                    // 此动作在移动中执行，这里针对放夹具单独处理 wjj 220891
                    //case MotorPosition.Onload_PalletPos:
                    //    {
                    //        //return FingerIntervalIncr(true, true); //wjj 220402
                    //        return FingerIntervalIncr(false, true);
                    //    }
            }
            return false;

            //if (this.MotorU < 0)
            //{
            //    return true;
            //}
            //return MotorMove(this.MotorU, (int)motorLoc, offset);
        }

        #endregion

        #region // 取放料计算

        /// <summary>
        /// 夹具需要扫码
        /// </summary>
        /// <param name="pPick"></param>
        /// <returns></returns>
        private bool PltNeedScanCode(ref PickPlacePos pPick)
        {
            if (scanPalletEnable)
            {
                // 有电池不能扫码
                for (ModDef i = ModDef.Finger_0; i < ModDef.Finger_ALL; i++)
                {
                    if (FingerBat(i).Type > BatteryStatus.Invalid)
                    {
                        return false;
                    }
                }
                for (int i = 0; i < this.Pallet.Length; i++)
                {
                    if (this.PalletPosEnable[i] && (PalletStatus.OK == this.Pallet[i].State)
                        && ("" == this.Pallet[i].Code) && this.Pallet[i].IsEmpty())
                    {
                        pPick.SetData((OnloadRobotStation)((int)OnloadRobotStation.ScanPalletCode_0 + i), 0, 0, 0, false, MotorPosition.Onload_ScanPalletPos);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 夹具需要回炉假电池
        /// </summary>
        /// <returns></returns>
        private bool PltNeedReFake()
        {
            for (int i = 0; i < this.Pallet.Length; i++)
            {
                if (this.PalletPosEnable[i] && (PalletStatus.ReputFake == this.Pallet[i].State) && this.Pallet[i].HasFake())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 夹具需要上电池
        /// </summary>
        /// <param name="pltPos"></param>
        /// <returns></returns>
        private bool PltNeedBat(ref int pltPos)
        {
            if ((pltPos < 0) || (PalletStatus.Invalid == this.Pallet[pltPos].State) || this.Pallet[pltPos].IsFull())
            {
                for (int i = 0; i < this.Pallet.Length; i++)
                {
                    if (this.PalletPosEnable[i] && (PalletStatus.OK == this.Pallet[i].State)
                        && (PalletStage.Invalid == this.Pallet[i].Stage) && !this.Pallet[i].IsFull())
                    {
                        if (this.placeFakePlt == 0)
                        {
                            this.Pallet[i].NeedFake = CalcNeedFakeBatteryPlt();
                        }
                        else if (this.placeFakePlt == 2)
                        {
                            this.Pallet[i].NeedFake = true;
                        }
                        else //if (this.placeFakePlt == 1) //启用后需及时恢复参数
                        {
                            this.Pallet[i].NeedFake = false;
                        }

                        pltPos = i;
                        SaveRunData(SaveType.Pallet, i);
                        return true;
                    }
                }
            }
            else if ((pltPos > -1) && this.PalletPosEnable[pltPos]
                && (PalletStatus.OK == this.Pallet[pltPos].State)
                && (PalletStage.Invalid == this.Pallet[pltPos].Stage)
                && !this.Pallet[pltPos].IsFull())
            {
                return true;
            }
            pltPos = -1;
            return false;
        }

        /// <summary>
        /// 夹具需要取待测假电池
        /// </summary>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool PltNeedPickDetectFake(ref PickPlacePos curPickPos)
        {
            int fakeRow, fakeCol;
            fakeRow = fakeCol = -1;
            for (int i = 0; i < (int)ModuleMaxPallet.OnloadRobot; i++)
            {
                if ((PalletStatus.Detect == this.Pallet[i].State) && this.Pallet[i].GetFakePos(ref fakeRow, ref fakeCol))
                {
                    // 只处理 ModDef.Finger_0 取假电池
                    if (fakeRow <= (this.Pallet[i].MaxRow - ((int)ModDef.Finger_ALL * (this.pltIntervalPos + 1))))
                    {
                        curPickPos.SetData((OnloadRobotStation.PalletStation_0 + i), fakeRow, fakeCol, (0x01 << (int)ModDef.Finger_0), true, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算是否需要上假电池夹具
        /// </summary>
        /// <returns></returns>
        //private bool CalcNeedFakeBatteryPlt(int curPltPos)
        //{
        //    int fakePlt, nomalPlt, placePltIdx;
        //    fakePlt = nomalPlt = placePltIdx = 0;
        //    RunID id = RunID.Invalid;
        //    Pallet[] plt = null;
        //    bool checkAll = true;
        //    MachineCtrl mc = MachineCtrl.GetInstance();

        //    // 判断是否有抽检炉腔
        //    for (id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
        //    {
        //        // 模组非使能 || 模组非运行中
        //        if (!mc.GetModuleEnable(id) || !mc.GetModuleRunning(id))
        //        {
        //            continue;
        //        }
        //        plt = mc.GetModulePallet(id);
        //        for (int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
        //        {
        //            if (mc.GetDryingOvenCavityEnable(id, rowIdx)
        //                && !mc.GetDryingOvenCavityPressure(id, rowIdx)
        //                && !mc.GetDryingOvenCavityTransfer(id, rowIdx)
        //                && (CavityStatus.Normal == mc.GetDryingOvenCavityState(id, rowIdx)))
        //            {
        //                if (0 != (mc.GetDryingOvenCavityHeartCycle(id, rowIdx) % mc.GetDryingOvenCavitySamplingCycle(id, rowIdx)))
        //                {
        //                    checkAll = false;
        //                    break;
        //                }
        //            }
        //        }
        //        if (!checkAll)
        //        {
        //            break;
        //        }
        //    }
        //    // 全检
        //    if (checkAll)
        //    {
        //        // 获取干燥炉中需要假电池夹具数量
        //        for (id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
        //        {
        //            // 模组非使能 || 模组非运行中
        //            if (!mc.GetModuleEnable(id) || !mc.GetModuleRunning(id))
        //            {
        //                continue;
        //            }
        //            plt = mc.GetModulePallet(id);
        //            for (int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.DryingOven; pltIdx++)
        //            {
        //                if (mc.GetDryingOvenCavityEnable(id, (pltIdx / 2))
        //                    && !mc.GetDryingOvenCavityPressure(id, (pltIdx / 2))
        //                    && !mc.GetDryingOvenCavityTransfer(id, (pltIdx / 2))
        //                    && (CavityStatus.Normal == mc.GetDryingOvenCavityState(id, (pltIdx / 2))))
        //                {
        //                    if ((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].HasFake() && plt[pltIdx].IsFull())
        //                    {
        //                        nomalPlt++;
        //                    }
        //                    else if ((PalletStatus.OK == plt[pltIdx].State) && plt[pltIdx].HasFake() && plt[pltIdx].IsFull())
        //                    {
        //                        fakePlt++;
        //                    }
        //                }
        //            }
        //        }
        //        // 计算当前在上料已有的假电池夹具
        //        id = RunID.OnloadRobot;
        //        plt = mc.GetModulePallet(id);
        //        for (int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.OnloadRobot; pltIdx++)
        //        {
        //            if ((pltIdx != curPltPos) && this.PalletPosEnable[pltIdx])
        //            {
        //                if ((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && !plt[pltIdx].NeedFake)
        //                {
        //                    nomalPlt++;
        //                }
        //                else if ((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && plt[pltIdx].NeedFake)
        //                {
        //                    fakePlt++;
        //                }
        //            }
        //        }
        //        return (nomalPlt >= fakePlt);
        //    }
        //    // 抽检
        //    else
        //    {
        //        // 获取干燥炉中需要假电池夹具数量
        //        for (id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
        //        {
        //            // 模组非使能 || 模组非运行中
        //            if (!mc.GetModuleEnable(id) || !mc.GetModuleRunning(id))
        //            {
        //                continue;
        //            }
        //            plt = mc.GetModulePallet(id);
        //            for (int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
        //            {
        //                if (mc.GetDryingOvenCavityEnable(id, rowIdx)
        //                    && !mc.GetDryingOvenCavityPressure(id, rowIdx)
        //                    && !mc.GetDryingOvenCavityTransfer(id, rowIdx)
        //                    && (CavityStatus.Normal == mc.GetDryingOvenCavityState(id, rowIdx))
        //                    && (0 == (mc.GetDryingOvenCavityHeartCycle(id, rowIdx) % mc.GetDryingOvenCavitySamplingCycle(id, rowIdx))))
        //                {
        //                    int pltIdx = rowIdx * (int)OvenRowCol.MaxCol;
        //                    // 统计需要假电池夹具
        //                    if (EventStatus.Require == mc.GetModuleEvent(id, EventList.DryOvenPlaceOnlOKFakeFullPallet, ref placePltIdx))
        //                    {
        //                        if (((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && !plt[pltIdx].HasFake() && !plt[pltIdx + 1].HasFake())
        //                            || ((PalletStatus.OK == plt[pltIdx + 1].State) && !plt[pltIdx + 1].IsEmpty() && !plt[pltIdx + 1].HasFake() && !plt[pltIdx].HasFake()))
        //                        {
        //                            fakePlt++;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        for (int colIdx = 0; colIdx < (int)OvenRowCol.MaxCol; colIdx++)
        //                        {
        //                            // 统计已有的正常夹具
        //                            if ((PalletStatus.OK == plt[pltIdx + colIdx].State) && !plt[pltIdx + colIdx].IsEmpty() && !plt[pltIdx + colIdx].HasFake())
        //                            {
        //                                nomalPlt++;
        //                            }
        //                            // 统计已有的假电池夹具
        //                            // 统计已有的假电池夹具需要的正常夹具
        //                            else if ((PalletStatus.OK == plt[pltIdx + colIdx].State) && !plt[pltIdx + colIdx].IsEmpty() && plt[pltIdx + colIdx].HasFake())
        //                            {
        //                                fakePlt++;
        //                                nomalPlt++;
        //                            }
        //                        }
        //                        // 统计需要的正常夹具：不在抽检次数
        //                        if (((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && (PalletStatus.Invalid == plt[pltIdx + 1].State))
        //                            || ((PalletStatus.OK == plt[pltIdx + 1].State) && !plt[pltIdx + 1].IsEmpty() && (PalletStatus.Invalid == plt[pltIdx].State)))
        //                        {
        //                            nomalPlt++;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        // 计算当前在上料已有的假电池夹具
        //        id = RunID.OnloadRobot;
        //        plt = mc.GetModulePallet(id);
        //        for (int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.OnloadRobot; pltIdx++)
        //        {
        //            if ((pltIdx != curPltPos) && this.PalletPosEnable[pltIdx])
        //            {
        //                if ((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && plt[pltIdx].NeedFake)
        //                {
        //                    fakePlt--;
        //                }
        //                else if ((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && !plt[pltIdx].NeedFake)
        //                {
        //                    nomalPlt--;
        //                }
        //            }
        //        }

        //        return (fakePlt > 0) && (fakePlt > nomalPlt);
        //    }
        //    //return this.firstFakePlt ? (fakePlt >= nomalPlt) : (nomalPlt >= fakePlt);
        //}
        private bool CalcNeedFakeBatteryPlt()
        {
            int fakePlt, nomalPlt, placePltIdx;
            fakePlt = nomalPlt = placePltIdx = 0;
            RunID id = RunID.Invalid;
            Pallet[] plt = null;
            bool checkAll = true;
            MachineCtrl mc = MachineCtrl.GetInstance();

            if (transferRoot.placeFakePalletCnt == 2)
            {
                return true;
            }
            //// 判断是否有抽检炉腔
            //for (id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            //{
            //    // 模组非使能 || 模组非运行中
            //    if (!mc.GetModuleEnable(id) || !mc.GetModuleRunning(id))
            //    {
            //        continue;
            //    }
            //    plt = mc.GetModulePallet(id);
            //    for (int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
            //    {
            //        if (mc.GetDryingOvenCavityEnable(id, rowIdx)
            //            && !mc.GetDryingOvenCavityPressure(id, rowIdx)
            //            && !mc.GetDryingOvenCavityTransfer(id, rowIdx)
            //            && (CavityStatus.Normal == mc.GetDryingOvenCavityState(id, rowIdx)))
            //        {
            //            if (0 != (mc.GetDryingOvenCavityHeartCycle(id, rowIdx) % mc.GetDryingOvenCavitySamplingCycle(id, rowIdx)))
            //            {
            //                checkAll = false;
            //                break;
            //            }
            //        }
            //    }
            //    if (!checkAll)
            //    {
            //        break;
            //    }
            //}
            // 全检
            if (checkAll)
            {
                // 获取干燥炉中需要假电池夹具数量
                for (id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
                {
                    // 模组非使能 || 模组非运行中
                    if (!mc.GetModuleEnable(id) || !mc.GetModuleRunning(id))
                    {
                        continue;
                    }
                    plt = mc.GetModulePallet(id);
                    for (int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.DryingOven; pltIdx++)
                    {
                        if (mc.GetDryingOvenCavityEnable(id, (pltIdx / 2))
                            && !mc.GetDryingOvenCavityPressure(id, (pltIdx / 2))
                            && !mc.GetDryingOvenCavityTransfer(id, (pltIdx / 2))
                            && (CavityStatus.Normal == mc.GetDryingOvenCavityState(id, (pltIdx / 2))))
                        {
                            if ((PalletStatus.OK == plt[pltIdx].State) && plt[pltIdx].Stage == PalletStage.Onload && !plt[pltIdx].HasFake() /*&& plt[pltIdx].IsFull()*/)
                            {
                                nomalPlt++;
                            }
                            else if ((PalletStatus.OK == plt[pltIdx].State) && plt[pltIdx].Stage == PalletStage.Onload && plt[pltIdx].HasFake()/* && plt[pltIdx].IsFull()*/)
                            {
                                fakePlt++;
                            }
                        }
                    }
                }
                // 计算当前在上料已有的假电池夹具
                //id = RunID.OnloadRobot;
                //plt = mc.GetModulePallet(id);
                //for (int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.OnloadRobot; pltIdx++)
                //{
                //    if ((pltIdx != curPltPos) && this.PalletPosEnable[pltIdx])
                //    {
                //        if ((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && !plt[pltIdx].NeedFake)
                //        {
                //            nomalPlt++;
                //        }
                //        else if ((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && plt[pltIdx].NeedFake)
                //        {
                //            fakePlt++;
                //        }
                //    }
                //}
                return (nomalPlt >= fakePlt);
            }
            // 抽检
            else
            {
                // 获取干燥炉中需要假电池夹具数量
                for (id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
                {
                    // 模组非使能 || 模组非运行中
                    if (!mc.GetModuleEnable(id) || !mc.GetModuleRunning(id))
                    {
                        continue;
                    }
                    plt = mc.GetModulePallet(id);
                    for (int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
                    {
                        if (mc.GetDryingOvenCavityEnable(id, rowIdx)
                            && !mc.GetDryingOvenCavityPressure(id, rowIdx)
                            && !mc.GetDryingOvenCavityTransfer(id, rowIdx)
                            && (CavityStatus.Normal == mc.GetDryingOvenCavityState(id, rowIdx))
                            && (0 == (mc.GetDryingOvenCavityHeartCycle(id, rowIdx) % mc.GetDryingOvenCavitySamplingCycle(id, rowIdx))))
                        {
                            int pltIdx = rowIdx * (int)OvenRowCol.MaxCol;
                            // 统计需要假电池夹具
                            if (EventStatus.Require == mc.GetModuleEvent(id, EventList.DryOvenPlaceOnlOKFakeFullPallet, ref placePltIdx))
                            {
                                if (((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && !plt[pltIdx].HasFake() && !plt[pltIdx + 1].HasFake())
                                    || ((PalletStatus.OK == plt[pltIdx + 1].State) && !plt[pltIdx + 1].IsEmpty() && !plt[pltIdx + 1].HasFake() && !plt[pltIdx].HasFake()))
                                {
                                    fakePlt++;
                                }
                            }
                            else
                            {
                                for (int colIdx = 0; colIdx < (int)OvenRowCol.MaxCol; colIdx++)
                                {
                                    // 统计已有的正常夹具
                                    if ((PalletStatus.OK == plt[pltIdx + colIdx].State) && !plt[pltIdx + colIdx].IsEmpty() && !plt[pltIdx + colIdx].HasFake())
                                    {
                                        nomalPlt++;
                                    }
                                    // 统计已有的假电池夹具
                                    // 统计已有的假电池夹具需要的正常夹具
                                    else if ((PalletStatus.OK == plt[pltIdx + colIdx].State) && !plt[pltIdx + colIdx].IsEmpty() && plt[pltIdx + colIdx].HasFake())
                                    {
                                        fakePlt++;
                                        nomalPlt++;
                                    }
                                }
                                // 统计需要的正常夹具：不在抽检次数
                                if (((PalletStatus.OK == plt[pltIdx].State) && !plt[pltIdx].IsEmpty() && (PalletStatus.Invalid == plt[pltIdx + 1].State))
                                    || ((PalletStatus.OK == plt[pltIdx + 1].State) && !plt[pltIdx + 1].IsEmpty() && (PalletStatus.Invalid == plt[pltIdx].State)))
                                {
                                    nomalPlt++;
                                }
                            }
                        }
                    }
                }
                return (fakePlt > 0) && (fakePlt > nomalPlt);
            }
            //return this.firstFakePlt ? (fakePlt >= nomalPlt) : (nomalPlt >= fakePlt);
        }

        /// <summary>
        /// 检查是否是上假电池行列
        /// </summary>
        /// <param name="placePlt"></param>
        /// <returns></returns>
        private bool IsOnloadFakeRowCol(int placePlt)
        {
            if (placePlt > -1 && this.Pallet[placePlt].NeedFake)
            {
                for (int row = 0; row < this.Pallet[placePlt].MaxRow; row++)
                {
                    for (int col = 0; col < this.Pallet[placePlt].MaxCol; col++)
                    {
                        if ((row == (placeFakeRow - 1)) && (col == (placeFakeCol - 1))
                            && (BatteryStatus.Invalid == this.Pallet[placePlt].Battery[row, col].Type))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算取假电池位置
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool CalcPickFakePos(int placePlt, ref PickPlacePos curPickPos)
        {
            RunProcessOnloadFake run = this.pickFakeRun;
            if (null != run)
            {
                if (EventStatus.Require == GetEvent(run, EventList.OnloadFakePickBattery))
                {
                    int pickRow = -1;
                    if (run.GetPickFakeRow(ref pickRow))
                    {
                        // 固定使用0号抓手抓取假电池
                        curPickPos.SetData(OnloadRobotStation.OnloadFake, pickRow, 0, (0x01 << (int)ModDef.Finger_0), true, MotorPosition.Onload_FakePos);
                        return true;
                    }
                }
                else if ((DateTime.Now - this.stepDelayTime).TotalMinutes > 5)
                {
                    this.stepDelayTime = DateTime.Now;
                }
                else if ((DateTime.Now - this.stepDelayTime).TotalSeconds > this.waitFakeDelay)
                {
                    ShowMessageBox((int)MsgID.WaitFakeBatTimeout, "假电池线体无假电池", "请上假电池", MessageType.MsgWarning);
                    this.stepDelayTime = DateTime.Now;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取夹具当前需要电池的行列
        /// </summary>
        /// <param name="pltRow"></param>
        /// <param name="pltCol"></param>
        /// <returns></returns>
        private bool GetPalletCurPlaceRowCol(int pltIndex, ref int pltRow, ref int pltCol)
        {
            for (int col = 0; col < this.Pallet[pltIndex].MaxCol; col++)
            {
                for (int row = 0; row < this.Pallet[pltIndex].MaxRow; row++)
                {
                    if ((BatteryStatus.Invalid == this.Pallet[pltIndex].Battery[row, col].Type))
                    {
                        bool canPlace = true;
                        for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
                        {
                            int batRow = row + i * (pltIntervalPos + 1);
                            if (batRow >= this.Pallet[pltIndex].MaxRow)
                            {
                                return false;
                            }
                            if (BatteryStatus.Invalid != this.Pallet[pltIndex].Battery[batRow, col].Type)
                            {
                                canPlace = false;
                                break;
                            }
                        }
                        if (canPlace)
                        {
                            pltRow = row;
                            pltCol = col;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算取料位置
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool CalcPickPos(int placePlt, ref PickPlacePos curPickPos)
        {
            if (placePlt < 0 || placePlt >= this.Pallet.Length)
            {
                return false;
            }
            int pltRow, pltCol;
            pltRow = pltCol = -1;
            if (GetPalletCurPlaceRowCol(placePlt, ref pltRow, ref pltCol))
            {
                // 来料取
                if ((null != this.pickBatRun))
                {
                    EventStatus status = GetEvent(this.pickBatRun, EventList.OnloadLinePickBattery);
                    if (EventStatus.Require == status || EventStatus.Ready == status)
                    {
                        if (!this.pickBatRun.RecvPosIsEmpty())
                        {
                            int nendNum = (int)ModDef.Finger_ALL;
                            int count = 0;
                            int finger = 0;

                            for (int i = 0; i < this.pickBatRun.Battery.Length; i++)
                            {
                                if (BatteryStatus.NG == this.pickBatRun.Battery[i].Type)
                                {
                                    nendNum = 2;
                                }
                            }

                            if (nendNum != 2)
                            {
                                if (IsOnloadFakeRowCol(placePlt))
                                {
                                    // 暂存空则全部抓手取，暂存非空则少取1
                                    nendNum = (BufferCount() < 1) ? (int)ModDef.Finger_ALL : ((int)ModDef.Finger_ALL - BufferCount() - 1);
                                }
                                else
                                {
                                    for (int i = 0; i < this.pickBatRun.Battery.Length; i++)
                                    {
                                        if (this.pickBatRun.Battery[i].Type == BatteryStatus.Invalid)
                                        {
                                            // 缓存位空，全取
                                            if (BufferCount() > 0)
                                            {
                                                nendNum = 2;
                                            }
                                            break;
                                        }
                                    }

                                    // 暂存 > 2 且来料有NG或没有电池则取2，否则全部抓手取
                                    //if (BufferCount() > 2)
                                    //{
                                    //    for (int i = 0; i < this.pickBatRun.Battery.Length; i++)
                                    //    {
                                    //        if (BatteryStatus.NG == this.pickBatRun.Battery[i].Type
                                    //            || BatteryStatus.Invalid == this.pickBatRun.Battery[i].Type)
                                    //        {
                                    //            nendNum = 2;
                                    //            break;
                                    //        }
                                    //    }
                                    //}

                                    //else if ((BufferCount() == 1) && (pltRow == 0))
                                    //{
                                    //    nendNum = 2;
                                    //}
                                }
                            }

                            if (nendNum == 2)
                            {
                                // 如果1-2位置有电池，只是取1-2位置
                                if (this.pickBatRun.Battery[0].Type != BatteryStatus.Invalid || this.pickBatRun.Battery[1].Type != BatteryStatus.Invalid)
                                {
                                    for (int i = 0; i < this.pickBatRun.Battery.Length; i++)
                                    {
                                        if (i == 2)
                                        {
                                            break;
                                        }
                                        if (this.pickBatRun.Battery[i].Type > BatteryStatus.Invalid)
                                        {
                                            if (count >= nendNum)
                                            {
                                                break;
                                            }
                                            count++;
                                            finger |= (0x01 << i);
                                        }
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < this.pickBatRun.Battery.Length; i++)
                                    {
                                        if (this.pickBatRun.Battery[i].Type > BatteryStatus.Invalid)
                                        {
                                            if (count >= nendNum)
                                            {
                                                break;
                                            }
                                            count++;
                                            finger |= (0x01 << i);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < this.pickBatRun.Battery.Length; i++)
                                {
                                    if (this.pickBatRun.Battery[i].Type > BatteryStatus.Invalid)
                                    {
                                        if (count >= nendNum)
                                        {
                                            break;
                                        }
                                        count++;
                                        finger |= (0x01 << i);
                                    }
                                }
                            }

                            if (finger > 0)
                            {
                                curPickPos.SetData(OnloadRobotStation.OnloadLine, 0, 0, finger, true, MotorPosition.Onload_LinePickPos);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算NG夹具转盘
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPickPos"></param>
        /// <returns></returns>
        private bool CalcPickNGPalletPos(int placePlt, ref PickPlacePos curPickPos)
        {
            if (placePlt < 0 || placePlt >= this.Pallet.Length)
            {
                return false;
            }
            // NG转盘：假电池状态相同
            int ngPlt = (int)ModuleMaxPallet.OnloadRobot - 1;
            if ((PalletStatus.NG == this.Pallet[ngPlt].State)
                && (this.Pallet[placePlt].NeedFake == this.Pallet[ngPlt].NeedFake)
                && !this.Pallet[ngPlt].IsEmpty())
            {
                int pltRow, pltCol, pltMaxPos;
                pltRow = pltCol = -1;
                pltMaxPos = this.Pallet[ngPlt].MaxRow - ((int)ModDef.Finger_ALL * (this.pltIntervalPos + 1));
                if (GetPalletCurPlaceRowCol(placePlt, ref pltRow, ref pltCol))
                {
                    for (int col = 0; col < this.Pallet[ngPlt].MaxCol; col++)
                    {
                        // 每次抓取ModDef.Finger_ALL（4）个电池
                        for (int row = 0; row <= pltMaxPos; row++)
                        {
                            // 首行首列时，则放夹具必须为空
                            //if (((0 == row) || (1 == row)) && (0 == col) && !this.Pallet[placePlt].IsEmpty()) // wjj 220507
                            if (((0 == pltRow) || (1 == pltRow)) && (0 == pltCol) && !this.Pallet[placePlt].IsEmpty())
                            {
                                return false;
                            }
                            // 每次抓取ModDef.Finger_ALL（4）个电池
                            else
                            {
                                int finger = 0;
                                // 首行抓取两个电池
                                if (0 == pltRow)
                                {
                                    for (int i = 0; i < (int)ModDef.Finger_ALL / 2; i++)
                                    {
                                        if (BatteryStatus.Invalid != this.Pallet[ngPlt].Battery[pltRow + (i * (this.pltIntervalPos + 1)), pltCol].Type)
                                        {
                                            finger |= (0x01 << i);
                                        }
                                    }
                                    if (finger > 0)
                                    {
                                        curPickPos.SetData(OnloadRobotStation.PalletStation_0 + ngPlt, pltRow, pltCol, finger, true, MotorPosition.Onload_PalletPos);
                                        return true;
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
                                    {
                                        if (BatteryStatus.Invalid != this.Pallet[ngPlt].Battery[pltRow + (i * (this.pltIntervalPos + 1)), pltCol].Type)
                                        {
                                            finger |= (0x01 << i);
                                        }
                                    }
                                    if (finger > 0)
                                    {
                                        curPickPos.SetData(OnloadRobotStation.PalletStation_0 + ngPlt, pltRow, pltCol, finger, true, MotorPosition.Onload_PalletPos);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算放NG电池位置
        /// </summary>
        /// <param name="placeplt"></param>
        /// <param name="curPlacePos"></param>
        /// <returns></returns>
        private bool CalcPlaceNGPos(ref PickPlacePos curPlacePos)
        {
            RunProcessOnloadNG run = placeNGRun;
            EventStatus state = GetEvent(run, EventList.OnloadNGPlaceBattery);
            if ((null != run) && (EventStatus.Require == state))
            {
                if (run.PlacePosIsEmpty())
                {
                    int fingerScan = 0;
                    int fingerMes = 0;

                    for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
                    {
                        if (FingerBat((ModDef)i).Type == BatteryStatus.NG)
                        {
                            if (FingerBat((ModDef)i).NGType == BatteryNGStatus.Scan)
                            {
                                fingerScan |= (0x01 << i);
                            }
                            else if (FingerBat((ModDef)i).NGType == BatteryNGStatus.MesNG)
                            {
                                fingerMes |= (0x01 << i);
                            }
                        }
                    }
                    //0011
                    //1100
                    // 1-2号位置放扫码NG，3-4放校验NG
                    if (fingerScan > 0)
                    {
                        Trace.WriteLine(string.Format("{0}", Convert.ToString(fingerScan, 2).PadLeft(4, '0')));

                        // 判断是哪个夹爪
                        if (fingerScan <= 3) // 1-2爪
                        {
                            curPlacePos.SetData(OnloadRobotStation.NGOutput, 1, 0, fingerScan, false, MotorPosition.Onload_NGPos);
                        }
                        else // 3-4爪
                        {
                            curPlacePos.SetData(OnloadRobotStation.NGOutput, 0, 0, fingerScan, false, MotorPosition.Onload_NGPos);
                        }
                        return true;
                    }
                    else if (fingerMes > 0)
                    {
                        Trace.WriteLine(string.Format("{0}", Convert.ToString(fingerMes, 2).PadLeft(4, '0')));

                        // 判断是哪个夹爪
                        if (fingerMes <= 3) // 1-2爪
                        {
                            curPlacePos.SetData(OnloadRobotStation.NGOutput, 2, 0, fingerMes, false, MotorPosition.Onload_NGPos);
                        }
                        else // 3-4爪
                        {
                            curPlacePos.SetData(OnloadRobotStation.NGOutput, 1, 0, fingerMes, false, MotorPosition.Onload_NGPos);
                        }
                        return true;
                    }

                    return false;
                }
                else
                {
                    Trace.WriteLine("放料位电池不为空");
                }
                // 无法放，置取消
                SetEvent(run, EventList.OnloadNGPlaceBattery, EventStatus.Cancel);
            }
            return false;
        }

        /// <summary>
        /// 计算放回炉假电池位置
        /// </summary>
        /// <param name="curPlacePos"></param>
        /// <returns></returns>
        private bool CalcPlaceReFakePos(ref PickPlacePos curPlacePos)
        {
            for (int idx = 0; idx < this.Pallet.Length; idx++)
            {
                int fakeRow, fakeCol;
                fakeRow = fakeCol = -1;
                if (this.PalletPosEnable[idx] && (PalletStatus.ReputFake == this.Pallet[idx].State) && this.Pallet[idx].GetFakePos(ref fakeRow, ref fakeCol))
                {
                    if ((BatteryStatus.Fake == FingerBat(ModDef.Finger_0).Type))
                    {
                        int finger = 0x01 << (int)ModDef.Finger_0;
                        //#region // wjj 220508 注销
                        //for (int i = 0; i < (int)ModDef.Finger_ALL; i++) 
                        //{
                        //    //if (FingerBat((ModDef)i).Type != BatteryStatus.Invalid)
                        //    if (FingerBat((ModDef)i).Type != BatteryStatus.Fake)
                        //    {
                        //        return false;
                        //    }
                        //}
                        //#endregion

                        curPlacePos.SetData((OnloadRobotStation.PalletStation_0 + idx), fakeRow, fakeCol, finger, false, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算放待测假电池位置
        /// </summary>
        /// <param name="curPlacePos"></param>
        /// <returns></returns>
        private bool CalcPlaceDetectFakePos(ref PickPlacePos curPlacePos)
        {
            RunProcessDetectFake run = this.placeFakeRun;
            if ((null != run) && (EventStatus.Require == GetEvent(run, EventList.PlaceDetectBattery)))
            {
                // 0号抓手的待测假电池，抓手1空
                if ((BatteryStatus.Detect == this.FingerBat(ModDef.Finger_0).Type))
                {
                    int finger = 0x01 << (int)ModDef.Finger_0;
                    for (int i = 1; i < (int)ModDef.Finger_ALL; i++)
                    {
                        if (this.FingerBat((ModDef)i).Type != BatteryStatus.Invalid)
                        {
                            return false;
                        }
                    }
                    if (BatteryStatus.Invalid == run.Battery[(int)ModDef.Finger_0].Type)
                    {
                        curPlacePos.SetData(OnloadRobotStation.DetectFake, 0, 0, finger, false, MotorPosition.Onload_DetectPos);
                        return true;
                    }
                }
                // 无法放，置取消
                SetEvent(run, EventList.PlaceDetectBattery, EventStatus.Cancel);
            }
            return false;
        }

        /// <summary>
        /// 计算抓手及暂存配对位置
        /// </summary>
        /// <param name="placeplt"></param>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcFingerBufferMatchesPos(int placeplt, ref PickPlacePos curPos)
        {
            // 电池倒盘，不进行配对
            if (pltPlaceNgBat)
            {
                return false;
            }

            int pltRow, pltCol;
            pltRow = pltCol = -1;
            if ((placeplt > -1) && GetPalletCurPlaceRowCol(placeplt, ref pltRow, ref pltCol))
            {
                int bufRow, calcFinger;
                int fingBit, bufBit;
                int fingBatNum, bufBatNum;

                #region // 计算抓手及暂存的电芯

                fingBit = fingBatNum = 0;
                bufBit = bufBatNum = 0;
                for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
                {
                    // 有NG电池，优先排NG
                    if (FingerBat((ModDef)i).Type == BatteryStatus.NG)
                    {
                        return false;
                    }
                    else if (FingerBat((ModDef)i).Type != BatteryStatus.Invalid)
                    {
                        fingBit |= (0x01 << i);
                        fingBatNum++;
                    }

                    if (BufferBat(ModDef.Buffer_0 + i).Type == BatteryStatus.OK)
                    {
                        bufBit |= (0x01 << i);
                        bufBatNum++;
                    }
                    // 暂存有非OK的电芯，则退出
                    else if (BufferBat(ModDef.Buffer_0 + i).Type != BatteryStatus.Invalid)
                    {
                        return false;
                    }
                }
                #endregion 计算抓手及暂存的电芯

                // 假电池行列，抓手无假电池，暂存为空，放暂存
                if (IsOnloadFakeRowCol(placeplt))
                {
                    #region // 假电池行列
                    if (fingBatNum < 1)
                    {
                        return false;
                    }
                    // 首行只放2个
                    if (0 == pltRow)
                    {
                        if ((BatteryStatus.Fake == FingerBat(ModDef.Finger_0).Type) && (fingBit >= (int)BatState.BS_0011))
                        {
                            return false;
                        }
                    }
                    // 4夹爪，固定 Finger_0 取放假电池，只处理 Finger_0 的假电池情况
                    // Finger_0 非假电池  &&  抓手 > 0，放暂存去取假电池
                    if ((BatteryStatus.Fake != FingerBat(ModDef.Finger_0).Type) && (fingBatNum + bufBatNum <= (int)ModDef.Finger_ALL))
                    {
                        bufRow = calcFinger = 0;
                        if (MatchesPos.CalcPos(false, fingBit, bufBit, ref bufRow, ref calcFinger))
                        {
                            Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPos", $"假电池行列(false, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                            curPos.SetData(OnloadRobotStation.BufferStation, bufRow, 0, calcFinger, false, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    // 配对取OK电池
                    else if ((BatteryStatus.Fake == FingerBat(ModDef.Finger_0).Type) && (fingBatNum < (int)ModDef.Finger_ALL)
                        && (fingBatNum + bufBatNum >= (int)ModDef.Finger_ALL))
                    {
                        bufRow = calcFinger = 0;
                        if (MatchesPos.CalcPos(true, fingBit, bufBit, ref bufRow, ref calcFinger))
                        {
                            // 首行只放2个
                            if (0 == pltRow)
                            {
                                //calcFinger &= (int)BatState.BS_0011;
                                calcFinger &= (int)BatState.BS_1111;
                            }
                            Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPos", $"假电池行列(true, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                            curPos.SetData(OnloadRobotStation.BufferStation, bufRow, 0, calcFinger, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    #endregion 假电池行列
                }
                // 上料清尾料：将所有电池都放入夹具
                else if (this.OnloadClear)
                {
                    #region // 上料清尾料
                    // 暂存有，取走放至夹具
                    if (bufBatNum > 0)
                    {
                        bufRow = calcFinger = 0;
                        if (MatchesPos.CalcPos(true, fingBit, bufBit, ref bufRow, ref calcFinger))
                        {
                            Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPos", $"上料清尾料(true, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                            curPos.SetData(OnloadRobotStation.BufferStation, bufRow, 0, calcFinger, true, MotorPosition.Onload_BufferPos);
                            return true;
                        }
                    }
                    return false;
                    #endregion
                }
                else
                {
                    #region // 非清尾料，放 ModDef.Finger_ALL 个
                    // 首行只放2个
                    if (0 == pltRow)
                    {
                        if (fingBit == (int)BatState.BS_1111 || (fingBit == (int)BatState.BS_0000 && bufBit != (int)BatState.BS_1111))
                        {
                            return false;
                        }

                        if (fingBit >= (int)BatState.BS_0011)
                        {
                            if ((BatteryStatus.Fake != FingerBat(ModDef.Finger_0).Type))
                            {
                                bufRow = calcFinger = 0;
                                // 保留前2个电池
                                int calcFingBit = 0;
                                if (fingBit == (int)BatState.BS_0011)
                                {
                                    calcFingBit = fingBit;
                                }
                                else
                                {
                                    calcFingBit = fingBit & (int)BatState.BS_1100;
                                }
                                if (MatchesPos.CalcPos(false, calcFingBit, bufBit, ref bufRow, ref calcFinger))
                                {
                                    Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPos", $"不能取则先放(false, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                                    curPos.SetData(OnloadRobotStation.BufferStation, bufRow, 0, calcFinger, false, MotorPosition.Onload_BufferPos);
                                    return true;
                                }
                            }
                            //return false;
                        }

                        if (bufBatNum >= (int)ModDef.Finger_ALL / 2)
                        {
                            bufRow = calcFinger = 0;
                            if (MatchesPos.CalcPos(true, fingBit, bufBit, ref bufRow, ref calcFinger))
                            {
                                // 首行只放2个
                                //if (0 == pltRow && (pltCol % 2 == 1))
                                //{
                                //    calcFinger &= (int)BatState.BS_0011;
                                //}
                                Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPos", $"首行只放2个(true, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                                curPos.SetData(OnloadRobotStation.BufferStation, bufRow, 0, calcFinger, true, MotorPosition.Onload_BufferPos);
                                return true;
                            }
                        }
                    }

                    if (fingBatNum < (int)ModDef.Finger_ALL)
                    {
                        // 双列 - 夹爪3-4有电池
                        if (pltCol % 2 == 0 && fingBit == (int)BatState.BS_1100)
                        {
                            // 当前双列可放 下一单列尾部可放
                            if (pltCol + 1 < this.Pallet[placeplt].MaxCol)
                            {
                                if (this.Pallet[placeplt].Battery[this.Pallet[placeplt].MaxRow - 1, pltCol + 1].Type == BatteryStatus.Invalid
                                    && this.Pallet[placeplt].Battery[this.Pallet[placeplt].MaxRow - 2, pltCol + 1].Type == BatteryStatus.Invalid)
                                {
                                    //calcFinger = fingBit;
                                    //bufRow = this.Pallet[placeplt].MaxRow - 4;
                                    //Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPos", $"抓手 + 暂存 >= ModDef.Finger_ALL  取(true, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                                    //curPos.SetData((OnloadRobotStation)((int)OnloadRobotStation.PalletStation_0 + placeplt), bufRow, pltCol + 1, calcFinger, false, MotorPosition.Onload_PalletPos);
                                    return false;
                                }
                            }
                        }

                        // 抓手 + 暂存 >= ModDef.Finger_ALL  取
                        if (fingBatNum + bufBatNum >= (int)ModDef.Finger_ALL)
                        {
                            bufRow = calcFinger = 0;
                            if (MatchesPos.CalcPos(true, fingBit, bufBit, ref bufRow, ref calcFinger))
                            {
                                // 首行只放2个
                                if (0 == pltRow)
                                {
                                    calcFinger &= (int)BatState.BS_0011;
                                }
                                Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPos", $"抓手 + 暂存 >= ModDef.Finger_ALL  取(true, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                                curPos.SetData(OnloadRobotStation.BufferStation, bufRow, 0, calcFinger, true, MotorPosition.Onload_BufferPos);
                                return true;
                            }
                            // 不能取则先放
                            else if (MatchesPos.CalcPos(false, fingBit, bufBit, ref bufRow, ref calcFinger))
                            {
                                Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPos", $"不能取则先放(false, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                                curPos.SetData(OnloadRobotStation.BufferStation, bufRow, 0, calcFinger, false, MotorPosition.Onload_BufferPos);
                                return true;
                            }
                        }
                        // 抓手 + 暂存 < ModDef.Finger_ALL && 抓手 > 0  放
                        else if (fingBatNum > 0)
                        {
                            bufRow = calcFinger = 0;
                            if (MatchesPos.CalcPos(false, fingBit, bufBit, ref bufRow, ref calcFinger))
                            {
                                // 首行只放2个
                                if (0 == pltRow)
                                {
                                    calcFinger &= (int)BatState.BS_0011;
                                }
                                Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPos", $"抓手 + 暂存 < ModDef.Finger_ALL && 抓手 > 0  放(false, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                                curPos.SetData(OnloadRobotStation.BufferStation, bufRow, 0, calcFinger, false, MotorPosition.Onload_BufferPos);
                                return true;
                            }
                        }
                    }
                    #endregion
                }
            }
            return false;
        }

        /// <summary>
        /// 暂存配对位电池整理
        /// </summary>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcFingerBufferMatchesPosMove(ref PickPlacePos curPos)
        {
            int bufRow, calcFinger;
            int fingBit, bufBit;
            int fingBatNum, bufBatNum;

            fingBit = fingBatNum = 0;
            bufBit = bufBatNum = 0;
            for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
            {
                if (FingerBat((ModDef)i).Type != BatteryStatus.Invalid)
                {
                    fingBit |= (0x01 << i);
                    fingBatNum++;
                }

                //if (BufferBat(ModDef.Buffer_0 + i).Type == BatteryStatus.OK)
                //{
                //    bufBit |= (0x01 << i);
                //    bufBatNum++;
                //}
            }
            // 夹爪有电池
            if (fingBatNum > 0)
            {
                return false;
            }
            for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
            {
                if (BufferBat(ModDef.Buffer_0 + i).Type == BatteryStatus.OK)
                {
                    bufBit |= (0x01 << i);
                    bufBatNum++;
                }
            }
            // 暂存配对位没有电池
            if (1 != bufBatNum && (bufBatNum == 3 && bufBit != (int)BatState.BS_1101))
            {
                return false;
            }
            if (bufBatNum == 3)
            {
                Trace.WriteLine("ddd");
            }
            // 只是处理暂存配对2号位有电池的情况，
            // 夹爪 BatState.BS_0000 - 暂存 BatState.BS_0001 -> BatState.BS_1000
            // 夹爪 BatState.BS_0000 - 暂存 BatState.BS_0010 -> BatState.BS_1000
            // 夹爪 BatState.BS_0000 - 暂存 BatState.BS_0100 -> BatState.BS_1000
            if (bufBit != (int)BatState.BS_0001 && bufBit != (int)BatState.BS_0010 && bufBit != (int)BatState.BS_0100
                && bufBit != (int)BatState.BS_1101)
            {
                return false;
            }

            bufRow = calcFinger = 0;
            if (MatchesPos.CalcPos(true, fingBit, bufBit, ref bufRow, ref calcFinger))
            {
                Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPosMove", $"暂存配对电池行列(false, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                curPos.SetData(OnloadRobotStation.BufferStation, bufRow, 0, calcFinger, true, MotorPosition.Onload_BufferPos);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 计算夹具中放电池位置
        /// </summary>
        /// <param name="placePlt"></param>
        /// <param name="curPos"></param>
        /// <returns></returns>
        private bool CalcPlacePalletPos(int placePlt, ref PickPlacePos curPos)
        {
            if (placePlt < 0 || placePlt >= (int)ModuleMaxPallet.OnloadRobot)
            {
                return false;
            }
            OnloadRobotStation station = (OnloadRobotStation)((int)OnloadRobotStation.PalletStation_0 + placePlt);
            // 放OK电池
            int pltRow, pltCol;
            pltRow = pltCol = -1;
            if (GetPalletCurPlaceRowCol(placePlt, ref pltRow, ref pltCol))
            {
                // 最后一行放一次电池
                if (pltRow > this.Pallet[placePlt].MaxRow - (int)ModDef.Finger_ALL)
                {
                    return false;
                }
                // 抓手有电芯，放入夹具
                int finger = 0;
                for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
                {
                    //if (FingerBat((ModDef)i).Type != BatteryStatus.Invalid) // wjj 220509
                    if (FingerBat((ModDef)i).Type != BatteryStatus.Invalid && FingerBat((ModDef)i).Type != BatteryStatus.NG)
                    {
                        finger |= (0x01 << i);
                    }
                }
                if (0 == pltRow)
                {
                    if (pltPlaceNgBat)
                    {
                        //首行放两个电池、或清尾料
                        if (finger == (int)BatState.BS_0011
                            || (this.OnloadClear && (finger == (int)BatState.BS_0001 || finger == (int)BatState.BS_0111)))
                        {
                            curPos.SetData(station, pltRow, pltCol, (int)finger, false, MotorPosition.Onload_PalletPos);
                            return true;
                        }
                    }
                    else
                    {
                        if (finger == (int)BatState.BS_1111
                        || (this.OnloadClear && (finger == (int)BatState.BS_0001 || finger == (int)BatState.BS_0111)))
                        {
                            // 双行
                            if (pltCol % 2 == 0)
                            {
                                curPos.SetData(station, pltRow, pltCol, (int)BatState.BS_0011, false, MotorPosition.Onload_PalletPos);
                            }
                            // 单行
                            else
                            {
                                curPos.SetData(station, pltRow, pltCol, finger, false, MotorPosition.Onload_PalletPos);
                            }

                            return true;
                        }
                    }

                    return false;
                }
                // 放假电池
                if (IsOnloadFakeRowCol(placePlt))
                {
                    if ((BatteryStatus.Fake == FingerBat(ModDef.Finger_0).Type))
                    {
                        finger = 0;
                        for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
                        {
                            if ((FingerBat((ModDef)i).Type == BatteryStatus.Invalid)
                                || ((BatteryStatus.Invalid != this.Pallet[placePlt].Battery[pltRow, pltCol].Type)
                                    && (BatteryStatus.Invalid != FingerBat((ModDef)i).Type)))
                            {
                                return false;
                            }
                            finger |= 0x01 << i;
                        }
                        if (finger > 0)
                        {
                            curPos.SetData(station, pltRow, pltCol, finger, false, MotorPosition.Onload_PalletPos);
                            return true;
                        }
                    }
                    return false;
                }
                // 上料清尾料
                if (this.OnloadClear)
                {
                    if (finger > 0)
                    {
                        curPos.SetData(station, pltRow, pltCol, finger, false, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                }
                // 非清尾料时必须放
                else
                {
                    // 抓手有电芯，放入夹具ModDef.Finger_ALL个电芯
                    if ((int)ModDef.Finger_Full == finger)
                    {
                        curPos.SetData(station, pltRow, pltCol, finger, false, MotorPosition.Onload_PalletPos);
                        return true;
                    }
                    else if (finger == (int)BatState.BS_1100)
                    {
                        if (pltCol % 2 == 0)
                        {
                            // 当前双列可放 下一单列尾部可放
                            if (pltCol + 1 < this.Pallet[placePlt].MaxCol)
                            {
                                if (this.Pallet[placePlt].Battery[this.Pallet[placePlt].MaxRow - 1, pltCol + 1].Type == BatteryStatus.Invalid
                                    && this.Pallet[placePlt].Battery[this.Pallet[placePlt].MaxRow - 2, pltCol + 1].Type == BatteryStatus.Invalid)
                                {
                                    pltRow = this.Pallet[placePlt].MaxRow - 4;
                                    pltCol = pltCol + 1;
                                    //Def.WriteLog("RunProcessOnloadRobot.CalcFingerBufferMatchesPos", $"抓手 + 暂存 >= ModDef.Finger_ALL  取(true, {Convert.ToString(fingBit, 2).PadLeft(4, '0')}, {Convert.ToString(bufBit, 2).PadLeft(4, '0')}, ref {bufRow}, ref {Convert.ToString(calcFinger, 2).PadLeft(4, '0')})");
                                    curPos.SetData(station, pltRow, pltCol, finger, false, MotorPosition.Onload_PalletPos);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 计算电池数量
        /// </summary>
        /// <param name="batData"></param>
        /// <returns></returns>
        private int CalcBatCount(int batData)
        {
            int nCount = 0;
            while (0 != batData)
            {
                if (0x01 == (batData & 0x01))
                {
                    nCount++;
                }
                batData >>= 1;
            }
            return nCount;
        }

        #endregion

        #region // 抓手及暂存

        private bool FingerClose(int finger, bool close)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            for (int i = 0; i < IFingerOpen.Length; i++)
            {
                if ((finger & (0x01 << i)) == (0x01 << i))
                {
                    if (IFingerOpen[i] < 0 || IFingerClose[i] < 0 || OFingerOpen[i] < 0 || OFingerClose[i] < 0)
                    {
                        return false;
                    }
                }
            }
            // 操作
            for (int i = 0; i < IFingerOpen.Length; i++)
            {
                if ((finger & (0x01 << i)) == (0x01 << i))
                {
                    OutputAction(OFingerClose[i], close);
                    OutputAction(OFingerOpen[i], !close);
                }
            }
            // 检查到位
            for (int i = 0; i < IFingerOpen.Length; i++)
            {
                if ((finger & (0x01 << i)) == (0x01 << i))
                {
                    if (!(WaitInputState(IFingerClose[i], close) && WaitInputState(IFingerOpen[i], !close)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private Battery FingerBat(ModDef finger)
        {
            if (finger < ModDef.Finger_0 || finger >= ModDef.Finger_ALL)
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
            for (ModDef i = ModDef.Finger_0; i < ModDef.Finger_ALL; i++)
            {
                if (FingerBat(i).Type > BatteryStatus.Invalid)
                {
                    count++;
                }
            }
            return count;
        }

        private Battery BufferBat(ModDef buffer)
        {
            if (buffer < ModDef.Buffer_0 || buffer >= ModDef.Buffer_ALL)
            {
                return null;
            }
            return this.Battery[(int)buffer];
        }

        private bool BufferCheck(int buffer, bool hasBat, bool alarm = true)
        {
            if (Def.IsNoHardware() || this.DryRun)
            {
                return true;
            }

            for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
            {
                if ((buffer & (0x01 << i)) == (0x01 << i))
                {
                    if (!InputState(IBufferCheck[i], hasBat))
                    {
                        if (alarm)
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
            for (ModDef i = ModDef.Buffer_0; i < ModDef.Buffer_ALL; i++)
            {
                if (BufferBat(i).Type > BatteryStatus.Invalid)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 夹爪间距气缸推出  increase:true=推出 false=退回
        /// </summary>
        /// <param name="increase"></param>
        /// <returns></returns>
        private bool FingerIntervalIncr(bool increase, bool wait)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }

            // 检查IO配置
            if (IIntervalCylPush < 0 || IIntervalCylPull < 0 ||
                OIntervalCylPush < 0 || OIntervalCylPull < 0)
            {
                return false;
            }

            // 操作
            OutputAction(OIntervalCylPush, increase);
            OutputAction(OIntervalCylPull, !increase);

            // 检查到位
            if (wait)
            {
                if (WaitInputState(IIntervalCylPush, increase) && WaitInputState(IIntervalCylPull, !increase))
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        #endregion

        #region // 机器人操作

        /// <summary>
        /// 获取机器人IP信息
        /// </summary>
        /// <returns></returns>
        public string RobotIPInfo()
        {
            return string.Format("{0}:{1}", this.robotIP, this.robotPort);
        }

        /// <summary>
        /// 获取机器人连接状态：true链接，false断开
        /// </summary>
        /// <returns>true链接，false断开</returns>
        public bool RobotIsConnect()
        {
            if (!robotEnable || Def.IsNoHardware())
            {
                return true;
            }
            return this.robotClient.IsConnect();
        }

        /// <summary>
        /// 连接机器人
        /// </summary>
        /// <param name="connect">true链接，false断开</param>
        /// <returns></returns>
        public bool RobotConnect(bool connect = true)
        {
            if (!robotEnable || Def.IsNoHardware())
            {
                return true;
            }

            if (connect)
            {
                if (!RobotIsConnect())
                {
                    return this.robotClient.Connect(robotIP, robotPort);
                }
            }
            else
            {
                return this.robotClient.Disconnect();
            }
            return RobotIsConnect();
        }

        /// <summary>
        /// 获取机器人命令
        /// </summary>
        /// <param name="station">工位</param>
        /// <param name="row">工位行</param>
        /// <param name="col">工位列</param>
        /// <param name="speed">速度</param>
        /// <param name="order">动作指令</param>
        /// <param name="rbtCmd">命令缓存</param>
        /// <returns></returns>
        public bool GetRobotCmd(OnloadRobotStation station, int row, int col, int speed, RobotOrder order, ref int[] rbtCmd)
        {
            rbtCmd[(int)RobotCmdFormat.Station] = (int)station;
            rbtCmd[(int)RobotCmdFormat.StationRow] = row + 1;
            rbtCmd[(int)RobotCmdFormat.StationCol] = col + 1;
            rbtCmd[(int)RobotCmdFormat.Speed] = speed;
            rbtCmd[(int)RobotCmdFormat.Order] = (int)order;
            rbtCmd[(int)RobotCmdFormat.Result] = (int)RobotOrder.END;

            // 工位行列非法
            if ((rbtCmd[(int)RobotCmdFormat.Station] < (int)OnloadRobotStation.InvalidStatioin)
                || (rbtCmd[(int)RobotCmdFormat.Station] >= (int)OnloadRobotStation.StationEnd)
                || (rbtCmd[(int)RobotCmdFormat.StationRow] < 0)
                || (rbtCmd[(int)RobotCmdFormat.StationRow] >= MachineCtrl.GetInstance().PalletMaxRow)
                || (rbtCmd[(int)RobotCmdFormat.StationCol] < 0)
                || (rbtCmd[(int)RobotCmdFormat.StationCol] > MachineCtrl.GetInstance().PalletMaxCol))
            {
                ShowMsgBox.ShowDialog(string.Format("{0},{1},{2},{3},{4},END\r\n工位行列非法，不能构造机器人指令", station, row, col, speed, order.ToString()), MessageType.MsgAlarm);
                return false;
            }

            MCState state = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if ((MCState.MCInitializing == state) || (MCState.MCRunning == state))
            {
                this.robotAutoAction.SetData((int)station, row, col, order, GetRobotStationName(station));
                this.robotDebugAction.SetData((int)station, row, col, order, GetRobotStationName(station));
            }
            else
            {
                this.robotDebugAction.SetData((int)station, row, col, order, GetRobotStationName(station));
            }
            //SaveRunData(SaveType.Robot);
            return true;
        }
        /// <summary>
        /// 调度取放信号
        /// </summary>
        /// <param name="pltIdx"></param>
        /// <param name="pickOrPlace 放：true 取：false"></param>
        /// <param name="actionState"></param>
        /// <returns></returns>
        public ushort ConvertTransFlag(int pltIdx, bool pickOrPlace, bool actionState)
        {
            if (actionState)
            {
                switch (pltIdx)
                {
                    case 0:
                        return pickOrPlace ? (ushort)(onloadData.transFlag ^ 0x01) : (ushort)(onloadData.transFlag ^ 0x40);
                    case 1:
                        return pickOrPlace ? (ushort)(onloadData.transFlag ^ 0x02) : (ushort)(onloadData.transFlag ^ 0x80);
                    case 2:
                        return pickOrPlace ? (ushort)(onloadData.transFlag ^ 0x04) : (ushort)(onloadData.transFlag ^ 0x100);
                    default:
                        break;
                }
            }
            else
            {
                switch (pltIdx)
                {
                    case 0:
                        return pickOrPlace ? (ushort)(onloadData.transFlag ^ 0x8) : (ushort)(onloadData.transFlag ^ 0x200);
                    case 1:
                        return pickOrPlace ? (ushort)(onloadData.transFlag ^ 0x10) : (ushort)(onloadData.transFlag ^ 0x400);
                    case 2:
                        return pickOrPlace ? (ushort)(onloadData.transFlag ^ 0x20) : (ushort)(onloadData.transFlag ^ 0x800);
                    default:
                        break;
                }
            }
            return 0;
        }

        private bool CheckPickOrPlaceStatus_(EventList modEvent, int pltIdx)
        {
            switch (modEvent)
            {
                case EventList.OnloadPlaceEmptyPallet:
                case EventList.OnloadPlaceNGPallet:
                case EventList.OnloadPlaceReputFakePallet:
                case EventList.OnLoadPlaceDetectFakePallet:
                    {
                        return onloadData.placeFlag[pltIdx];
                    }
                case EventList.OnloadPickNGEmptyPallet:
                case EventList.OnloadPickOKFullPallet:
                case EventList.OnloadPickOKFakeFullPallet:
                case EventList.OnloadPickRebakeFakePallet:
                case EventList.OnLoadPickWaitResultPallet:
                    {
                        return onloadData.pickFlag[pltIdx];
                    }
            }
            return false;
        }
        /// <summary>
        /// 机器人回零
        /// </summary>
        /// <returns></returns>
        public bool RobotHome(int pltIdx, bool actionState)
        {
            // 小机器人出问题，手动上料屏蔽回零动作
            //if (Def.IsNoHardware()/* || !autoOnLoadBattery*/)
            //    return true;


            switch (this.avoidEvent)
            {
                case EventList.OnloadPlaceEmptyPallet:
                case EventList.OnloadPlaceNGPallet:
                case EventList.OnloadPlaceReputFakePallet:
                case EventList.OnLoadPlaceDetectFakePallet:
                    {
                        //if (!PalletKeepFlat(pltIdx, false, true))
                        //{
                        //    return false;
                        //}
                        onloadData.transFlag = 0;
                        onloadData.transFlag = ConvertTransFlag(pltIdx, true, actionState);
                        break;
                    }
                case EventList.OnloadPickNGEmptyPallet:
                case EventList.OnloadPickOKFullPallet:
                case EventList.OnloadPickOKFakeFullPallet:
                case EventList.OnloadPickRebakeFakePallet:
                case EventList.OnLoadPickWaitResultPallet:
                    {
                        //if (!PalletKeepFlat(pltIdx, true, true))
                        //{
                        //    return false;
                        //}
                        onloadData.transFlag = 0;
                        onloadData.transFlag = ConvertTransFlag(pltIdx, false, actionState);
                        break;
                    }
            }
            //this.transAvoid = onloadData.transFlag;
            //发送夹具取放信号，调度机器人干预信号
            return onloadClient.SetLoadingData(LoadingCmd.WriteTrans, onloadData);
        }

        /// <summary>
        /// 机器人移动
        /// </summary>
        /// <param name="rbtCmd"></param>
        /// <param name="wait"></param>
        /// <returns></returns>
        public bool RobotMove(int[] rbtCmd, bool wait, OptMode mode = OptMode.Auto)
        {
            if (!robotEnable || Def.IsNoHardware())
            {
                return true;
            }

            string errMsg = "";
            if (!this.robotClient.Send(rbtCmd, mode, ref errMsg))
            {
                Def.WriteLog("RunProcessOnloadRobot", errMsg, LogType.Error);

                int[] recvCmd = new int[(int)RobotCmdFormat.End];
                string msg = string.Format("机器人反馈移动超时[10秒]");
                string dispose = "";
                if (this.robotClient.GetReceiveResult(ref recvCmd))
                {
                    msg += string.Format("，机器人反馈：{0},{1},{2},{3},{4},{5}"
                    , recvCmd[(int)RobotCmdFormat.Station], recvCmd[(int)RobotCmdFormat.StationRow]
                    , recvCmd[(int)RobotCmdFormat.StationCol], recvCmd[(int)RobotCmdFormat.Speed]
                    , (RobotOrder)recvCmd[(int)RobotCmdFormat.Order], (RobotOrder)recvCmd[(int)RobotCmdFormat.Result]);
                    dispose = "请检查机器人指令是否错误，或检查示教器是否有异常提示或报警";
                }
                else
                {
                    msg += "，机器人无响应反馈";
                    dispose = "请检查机器人示教器，是否在自动运行状态";
                }
                ShowMessageBox((int)MsgID.SendRbtMoveCmd, msg, dispose, MessageType.MsgAlarm);
                return false;
            }
            if (wait)
            {
                return RobotMoveFinish(rbtCmd, DateTime.Now);
            }
            return true;
        }

        /// <summary>
        /// 等待机器人运动完成
        /// </summary>
        /// <param name="rbtCmd">运动命令</param>
        /// <param name="startTime">开始时间</param>
        /// <returns></returns>
        public bool RobotMoveFinish(int[] rbtCmd, DateTime startTime)
        {
            if (!robotEnable || Def.IsNoHardware())
            {
                return true;
            }

            string msg, dispose;
            int[] recvCmd = new int[(int)RobotCmdFormat.End];
            this.RobotRunning = true;
            while (true)
            {
                if (this.robotClient.GetReceiveResult(ref recvCmd))
                {
                    if ((rbtCmd[(int)RobotCmdFormat.Station] == recvCmd[(int)RobotCmdFormat.Station])
                        && (rbtCmd[(int)RobotCmdFormat.StationRow] == recvCmd[(int)RobotCmdFormat.StationRow])
                        && (rbtCmd[(int)RobotCmdFormat.StationCol] == recvCmd[(int)RobotCmdFormat.StationCol])
                        && (rbtCmd[(int)RobotCmdFormat.Order] == recvCmd[(int)RobotCmdFormat.Order])
                        && ((int)RobotOrder.FINISH == recvCmd[(int)RobotCmdFormat.Result]))
                    {
                        this.RobotRunning = false;
                        return true;
                    }
                    if (((int)RobotOrder.INVALID == recvCmd[(int)RobotCmdFormat.Result])
                        || ((int)RobotOrder.ERR == recvCmd[(int)RobotCmdFormat.Result]))
                    {
                        this.RobotRunning = false;
                        msg = string.Format("机器人指令错误[{0},{1},{2},{3},{4},{5}]"
                            , rbtCmd[(int)RobotCmdFormat.Station], rbtCmd[(int)RobotCmdFormat.StationRow]
                            , rbtCmd[(int)RobotCmdFormat.StationCol], rbtCmd[(int)RobotCmdFormat.Speed]
                            , (RobotOrder)rbtCmd[(int)RobotCmdFormat.Order], (RobotOrder)rbtCmd[(int)RobotCmdFormat.Result]);
                        dispose = string.Format("请检查机器人指令");
                        ShowMessageBox((int)MsgID.RbtMoveCmdError, msg, dispose, MessageType.MsgAlarm);
                        break;
                    }
                }
                if ((DateTime.Now - startTime).TotalSeconds > this.robotDelay)
                {
                    this.RobotRunning = false;
                    msg = string.Format("机器人运动超时[{0}秒]", this.robotDelay);
                    dispose = string.Format("请检查机器人是否运行");
                    ShowMessageBox((int)MsgID.RbtMoveTimeout, msg, dispose, MessageType.MsgAlarm);
                    break;
                }
                if (!OutputState(this.ORobotEStop, false))
                {
                    this.RobotRunning = false;
                    Def.WriteLog(this.RunName, "机器人急停，退出等待机器人动作完成");
                    break;
                }
                Sleep(1);
            }
            return false;
        }

        /// <summary>
        /// 机器人电机同时移动且等待完成，仅对GO指令起作用：motorLoc为-1时电机不移动
        /// </summary>
        /// <param name="rbtCmd"></param>
        /// <param name="motorLoc"></param>
        /// <returns></returns>
	    public bool RobotMotorMove(int[] rbtCmd, MotorPosition motorLoc = MotorPosition.Invalid, OptMode mode = OptMode.Auto)
        {
            if (!robotEnable || Def.IsNoHardware())
            {
                return true;
            }
            if (!RobotMove(rbtCmd, false, mode))
            {
                return false;
            }
            DateTime startTime = DateTime.Now;

            if (((int)RobotOrder.MOVE == rbtCmd[(int)RobotCmdFormat.Order])
                && (motorLoc > MotorPosition.Invalid))
            {
                FingerIntervalPush(motorLoc);
            }

            return RobotMoveFinish(rbtCmd, startTime);
        }

        /// <summary>
        /// 初始化机器人工位
        /// </summary>
        public void InitRobotStation()
        {
            if (null == this.robotStationInfo)
            {
                this.robotStationInfo = new Dictionary<OnloadRobotStation, RobotFormula>();
                int rbtID = (int)RobotID;
                if (this.RobotID <= RobotIndexID.Invalid || this.RobotID >= RobotIndexID.End)
                {
                    return;
                }
                int formulaID = Def.GetProductFormula();
                string rbtName = RobotDef.RobotIDName[rbtID];
                List<RobotFormula> listStation = new List<RobotFormula>();
                this.dbRecord.GetRobotStationList(Def.GetProductFormula(), (int)RobotID, ref listStation);
                foreach (var item in listStation)
                {
                    this.robotStationInfo.Add((OnloadRobotStation)item.stationID, item);
                }
                for (OnloadRobotStation station = OnloadRobotStation.InvalidStatioin; station < OnloadRobotStation.StationEnd; station++)
                {
                    bool add = false;
                    RobotFormula rbtFormula = new RobotFormula();
                    string stationName = "";
                    int stationID = (int)station;
                    #region // 查找工位是否存在
                    switch (station)
                    {
                        case OnloadRobotStation.InvalidStatioin:
                            {
                                if (!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】闲置", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.HomeStatioin:
                            {
                                if (!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】回零位", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.OnloadLine:
                            {
                                if (!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】来料取料位", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.ScanPalletCode_0:
                        case OnloadRobotStation.ScanPalletCode_1:
                        case OnloadRobotStation.ScanPalletCode_2:
                            {
                                if (!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】上料夹具{1}扫码", stationID, (stationID - (int)OnloadRobotStation.ScanPalletCode_0 + 1));
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.PalletStation_0:
                        case OnloadRobotStation.PalletStation_1:
                        case OnloadRobotStation.PalletStation_2:
                            {
                                stationName = string.Format("{0}】上料夹具{1}", stationID, (stationID - (int)OnloadRobotStation.PalletStation_0 + 1));
                                int maxRow = MachineCtrl.GetInstance().PalletMaxRow - (int)ModDef.Finger_ALL + 1;
                                int maxCol = MachineCtrl.GetInstance().PalletMaxCol;
                                rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, maxRow, maxCol);
                                if (!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                }
                                else
                                {
                                    break;
                                    this.dbRecord.ModifyRobotStation(rbtFormula);
                                }
                                break;
                            }
                        case OnloadRobotStation.BufferStation:
                            {
                                if (!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】暂存工位", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, (2 * (int)ModDef.Finger_ALL - 1), 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.NGOutput:
                            {
                                if (!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】NG电池输出", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.OnloadFakeScan:
                            {
                                if (!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】假电池扫码", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 5, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.OnloadFake:
                            {
                                if (!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】假电池输入", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 5, 1);
                                }
                                break;
                            }
                        case OnloadRobotStation.DetectFake:
                            {
                                if (!this.robotStationInfo.ContainsKey(station))
                                {
                                    add = true;
                                    stationName = string.Format("{0}】待测假电池输出", stationID);
                                    rbtFormula = new RobotFormula(formulaID, rbtID, rbtName, stationID, stationName, 1, 1);
                                }
                                break;
                            }
                        default:
                            break;
                    }
                    #endregion
                    if (add)
                    {
                        this.robotStationInfo.Add(station, rbtFormula);
                        this.dbRecord.AddRobotStation(rbtFormula);
                    }
                }
            }
        }

        /// <summary>
        /// 获取工位名称
        /// </summary>
        /// <param name="station"></param>
        /// <returns></returns>
        public string GetRobotStationName(OnloadRobotStation station)
        {
            string info = "";
            if (this.robotStationInfo.ContainsKey(station))
            {
                info = this.robotStationInfo[station].stationName;
            }
            return info;
        }

        /// <summary>
        /// 获取动作信息
        /// </summary>
        /// <param name="autoAction"></param>
        /// <returns></returns>
        public RobotActionInfo GetRobotActionInfo(bool autoAction)
        {
            return autoAction ? this.robotAutoAction : this.robotDebugAction;
        }

        /// <summary>
        /// 获取机器人是否在安全位
        /// </summary>
        /// <returns></returns>
        public bool RobotInSafePos()
        {
            if (((int)OnloadRobotStation.HomeStatioin == this.robotAutoAction.station)
                && (RobotOrder.HOME == this.robotAutoAction.order))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 自动运行开始时机器人位置防呆检查
        /// </summary>
        /// <returns></returns>
        private bool CheckRobotPos(RobotActionInfo autoCmd, RobotActionInfo debugCmd)
        {
            // 自动在回零位置，手动在回零位置，则不判断
            if ((RobotOrder.HOME == autoCmd.order) && (RobotOrder.HOME == debugCmd.order))
            {
                return true;
            }
            else if ((autoCmd.station == debugCmd.station)
                && (autoCmd.row == debugCmd.row)
                && (autoCmd.col == debugCmd.col))
            {
                if ((autoCmd.order == debugCmd.order)
                    || ((RobotOrder.MOVE == autoCmd.order) && (RobotOrder.UP == debugCmd.order))
                    || ((RobotOrder.UP == autoCmd.order) && (RobotOrder.MOVE == debugCmd.order)))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 手动防呆
        /// </summary>
        /// <param name="nStation"></param>
        /// <param name="nRow"></param>
        /// <param name="nCol"></param>
        /// <param name="nOrder"></param>
        /// <returns></returns>
        public bool RobotManulAvoid(OnloadRobotStation station, int row, int col, RobotOrder order)
        {
            string msg = "";

            #region // 已有Ready || Start信号 || 调度执行取进或放进

            if (RobotOrder.HOME != order)
            {
                for (EventList i = EventList.OnloadPlaceEmptyPallet; i < EventList.OnloadPickPlaceEnd; i++)
                {
                    EventStatus state = GetEvent(this, i);
                    if ((EventStatus.Ready == state) || (EventStatus.Start == state))
                    {
                        msg = string.Format("调度机器人已开始执行取放上料夹具事件，仅能操作上料机器人【{0}】", RobotDef.RobotOrderName[(int)RobotOrder.HOME]);
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        return false;
                    }
                }
            }
            RobotActionInfo rbtAction = MachineCtrl.GetInstance().GetRobotActionInfo(RunID.Transfer, false);
            if (null == rbtAction)
            {
                msg = string.Format("无法获取调度机器人当前动作，不能操作上料机器人\r\n在【其它调试】界面重连模组客户端后再操作");
                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                return false;
            }
            if ((int)TransferRobotStation.OnloadStation == rbtAction.station)
            {
                if (RobotOrder.MOVE != rbtAction.order)
                {
                    msg = string.Format("调度机器人已在上料位取放夹具，不能操作上料机器人\r\n在【机器人调试】界面将大机器人移动到当前工位移动位置后再操作");
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                    return false;
                }
            }
            MCState mcState = MachineCtrl.GetInstance().GetModuleMCState(RunID.Transfer);
            if ((MCState.MCInitComplete != mcState) && (MCState.MCStopRun != mcState))
            {
                msg = string.Format("调度设备非【初始化完成】或【运行停止】状态，不能操作{0}", RobotDef.RobotIDName[(int)this.RobotID]);
                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                return false;
            }

            #endregion

            #region // 判断动作指令
            switch (order)
            {
                case RobotOrder.HOME:
                    break;
                case RobotOrder.MOVE:
                    {
                        if (RobotOrder.DOWN == this.robotDebugAction.order)
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                case RobotOrder.DOWN:
                    {
                        if (((int)station != this.robotDebugAction.station) || (row != this.robotDebugAction.row)
                             || (col != this.robotDebugAction.col) || (RobotOrder.DOWN == this.robotDebugAction.order))
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                case RobotOrder.UP:
                    {
                        if (((int)station != this.robotDebugAction.station) || (row != this.robotDebugAction.row)
                             || (col != this.robotDebugAction.col))
                        {
                            msg = string.Format("{0}\r\n当前在<{1}-{2}行-{3}列-{4}>位置\r\n不能执行<{5}-{6}行-{7}列-{8}>操作", this.RunName, this.robotDebugAction.stationName
                                , this.robotDebugAction.row + 1, this.robotDebugAction.col + 1, RobotDef.RobotOrderName[(int)this.robotDebugAction.order]
                                , GetRobotStationName(station), row + 1, col + 1, RobotDef.RobotOrderName[(int)order]);
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                default:
                    ShowMsgBox.ShowDialog(order.ToString() + "非上料机器人动作，不能操作上料机器人", MessageType.MsgWarning);
                    return false;
                    break;
            }
            #endregion

            #region // 判断工位行列
            switch (station)
            {

                case OnloadRobotStation.HomeStatioin:
                    {
                        if (RobotOrder.HOME != order)
                        {
                            msg = string.Format("回零工位仅能执行回零指令！");
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            return false;
                        }
                        break;
                    }
                case OnloadRobotStation.OnloadLine: //来料取料位置，气缸推出
                    {
                        if (RobotOrder.MOVE == order)
                        {
                            if (!FingerIntervalPush(MotorPosition.Onload_LinePickPos))
                            {
                                return false;
                            }
                        }
                        else if (RobotOrder.DOWN == order)
                        {
                            //if (!CheckMotorPos(this.MotorU, MotorPosition.Onload_LinePickPos))
                            //{
                            //    return false;
                            //}
                            //if (!FingerIntervalIncr(false, true)) //wjj 220402
                            if (!FingerIntervalIncr(true, true))
                            {
                                return false;
                            }
                            if (null != this.pickBatRun)
                            {
                                if (!FingerCheck((int)ModDef.Finger_Full, false, false))
                                {
                                    msg = string.Format("{0}抓手有电池，机器人不能在来料取料位下降！", this.RunName);
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                    return false;
                                }
                                if (!FingerClose((int)ModDef.Finger_Full, false))
                                {
                                    msg = string.Format("{0}抓手非打开状态，机器人不能在夹具扫码位下降！", this.RunName);
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                    return false;
                                }
                                //if (!this.pickBatRun.RecvPosSenserInpos(false))
                                if (!this.pickBatRun.RecvPosSenserInpos(true))
                                {
                                    msg = string.Format("来料线取料位进入感应器检测非【ON】，机器人不能下降！");
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                                    return false;
                                }
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.ScanPalletCode_0:
                case OnloadRobotStation.ScanPalletCode_1:
                case OnloadRobotStation.ScanPalletCode_2:
                    {
                        if (RobotOrder.MOVE == order)
                        {
                            if (!FingerIntervalPush(MotorPosition.Onload_ScanPalletPos))
                            {
                                return false;
                            }
                        }
                        else if (RobotOrder.DOWN == order)
                        {

                            //if (!CheckMotorPos(this.MotorU, MotorPosition.Onload_ScanPalletPos))
                            //{
                            //    return false;
                            //}
                            if (!PalletKeepFlat(((int)station - (int)OnloadRobotStation.ScanPalletCode_0), true, true))
                            {
                                return false;
                            }
                            if (!FingerCheck((int)ModDef.Finger_Full, false, false))
                            {
                                msg = string.Format("{0}抓手有电池，机器人不能在夹具扫码位下降！", this.RunName);
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                            if (!FingerClose((int)ModDef.Finger_Full, false))
                            {
                                msg = string.Format("{0}抓手非打开状态，机器人不能在夹具扫码位下降！", this.RunName);
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                            for (int i = 0; i < this.IFingerOpen.Length; i++)
                            {
                                if (!InputState(IFingerOpen[i], true))
                                {
                                    msg = string.Format("机器人抓手打开感应器非【ON】，机器人不能下降！\r\n保证抓手无料且打开后再下降");
                                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                    return false;
                                }
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.PalletStation_0:
                case OnloadRobotStation.PalletStation_1:
                case OnloadRobotStation.PalletStation_2:
                    {
                        if (RobotOrder.MOVE == order)
                        {
                            //if (!FingerIntervalPush(MotorPosition.Onload_PalletPos))FingerIntervalIncr(false, true)
                            if (!FingerIntervalIncr(false, true))
                            {
                                return false;
                            }
                        }
                        else if (RobotOrder.DOWN == order)
                        {
                            //if (!CheckMotorPos(this.MotorU, MotorPosition.Onload_PalletPos))
                            //{
                            //    return false;
                            //}
                            if (!PalletKeepFlat(((int)station - (int)OnloadRobotStation.PalletStation_0), true, true))
                            {
                                return false;
                            }
                            //if (!FingerIntervalIncr(true, true))//wjj 220517
                            if (!FingerIntervalIncr(false, true))
                            {
                                return false;
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.BufferStation:
                    {
                        if (RobotOrder.MOVE == order)
                        {
                            if (!FingerIntervalPush(MotorPosition.Onload_BufferPos))
                            {
                                return false;
                            }
                        }
                        else if (RobotOrder.DOWN == order)
                        {
                            //if (!CheckMotorPos(this.MotorU, MotorPosition.Onload_BufferPos))
                            //{
                            //    return false;
                            //}
                            //气缸推出
                            if (!FingerIntervalIncr(true, true))
                            {
                                return false;
                            }
                            //if (0 == col)
                            //{
                            //    if (FingerCheck(ModDef.Finger_1, true, false))
                            //    {
                            //        if (!BufferCheck(ModDef.Buffer_0, false, false))
                            //        {
                            //            msg = string.Format("{0}抓手2有电池，缓存1位置有电池，机器人不能下降！", this.RunName);
                            //            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            //            return false;
                            //        }
                            //    }
                            //}
                            //else if (1 == col)
                            //{
                            //    for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                            //    {
                            //        if(FingerCheck((ModDef.Finger_0 + i), true, false))
                            //        {
                            //            if(!BufferCheck((ModDef.Buffer_0 + i), false, false))
                            //            {
                            //                msg = string.Format("{0}抓手{1}有电池，缓存{1}位置有电池，机器人不能下降！", this.RunName, (i + 1));
                            //                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            //                return false;
                            //            }
                            //        }
                            //    }
                            //}
                            //else if (2 == col)
                            //{
                            //    if(FingerCheck(ModDef.Finger_0, true, false))
                            //    {
                            //        if(!BufferCheck(ModDef.Buffer_1, false, false))
                            //        {
                            //            msg = string.Format("{0}抓手1有电池，缓存2位置有电池，机器人不能下降！", this.RunName);
                            //            ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                            //            return false;
                            //        }
                            //    }
                            //}
                        }
                        break;
                    }
                case OnloadRobotStation.NGOutput:
                    {
                        if (null != this.placeNGRun)
                        {
                            if (!this.placeNGRun.PlaceSenserIsSafe())
                            {
                                msg = string.Format("NG输出工位放到位或放料位流出感应器检测非【OFF】，机器人不能下降！");
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                        }
                        if (RobotOrder.MOVE == order)
                        {
                            if (!FingerIntervalPush(MotorPosition.Onload_NGPos))
                            {
                                return false;
                            }
                        }
                        else if (RobotOrder.DOWN == order)
                        {
                            //if (!CheckMotorPos(this.MotorU, MotorPosition.Onload_NGPos))
                            //{
                            //    return false;
                            //}
                            if (!FingerIntervalIncr(true, true))
                            {
                                return false;
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.OnloadFakeScan:
                case OnloadRobotStation.OnloadFake:
                    {
                        MotorPosition mtrPos = (OnloadRobotStation.OnloadFakeScan == station) ? MotorPosition.Onload_ScanFakePos : MotorPosition.Onload_FakePos;
                        if (RobotOrder.MOVE == order)
                        {
                            if (!FingerIntervalPush(mtrPos))
                            {
                                return false;
                            }
                        }
                        else if (RobotOrder.DOWN == order)
                        {
                            //if (!CheckMotorPos(this.MotorU, mtrPos))
                            //{
                            //    return false;
                            //}
                            if (!FingerCheck((int)ModDef.Finger_Full, false, false))
                            {
                                msg = string.Format("{0}抓手有电池，机器人不能在假电池位下降！", this.RunName);
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                            if (!FingerClose((int)ModDef.Finger_Full, false))
                            {
                                msg = string.Format("{0}抓手非打开状态，机器人不能在夹具扫码位下降！", this.RunName);
                                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                                return false;
                            }
                            if (null != this.pickFakeRun)
                            {
                                if (!this.pickFakeRun.PickPosSenserInpos(true))
                                {
                                    msg = string.Format("假电池工位到位感应器检测非【ON】，机器人不能下降！\r\n是】强制下降，否】取消下降");
                                    if (DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                                    {
                                        return false;
                                    }
                                }
                            }
                            if (!FingerIntervalIncr(true, true))
                            {
                                return false;
                            }
                        }
                        break;
                    }
                case OnloadRobotStation.DetectFake:
                    {
                        if (!FingerIntervalIncr(true, true))
                        {
                            return false;
                        }
                        break;
                    }
                default:
                    return false;
                    break;
            }
            #endregion

            return true;
        }

        /// <summary>
        /// 自动运行时检查目标工位安全状态
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        private bool CheckStationSafe(PickPlacePos pos, RobotOrder order)
        {
            switch (pos.station)
            {
                case OnloadRobotStation.HomeStatioin:
                    {
                        return true;
                    }
                case OnloadRobotStation.OnloadLine:
                    {
                        if (null != this.pickBatRun)
                        {
                            return this.pickBatRun.RecvPosSenserInpos(true);
                        }
                        break;
                    }
                case OnloadRobotStation.ScanPalletCode_0:
                case OnloadRobotStation.ScanPalletCode_1:
                case OnloadRobotStation.ScanPalletCode_2:
                    {
                        return PalletKeepFlat((int)(pos.station - OnloadRobotStation.ScanPalletCode_0), true, true);
                    }
                case OnloadRobotStation.PalletStation_0:
                case OnloadRobotStation.PalletStation_1:
                case OnloadRobotStation.PalletStation_2:
                    {
                        return PalletKeepFlat((int)(pos.station - OnloadRobotStation.PalletStation_0), true, true);
                    }
                case OnloadRobotStation.BufferStation:
                    {
                        if (RobotOrder.MOVE == order)
                        {
                            return true;
                        }
                        string msg, dispose;
                        msg = dispose = "";
                        return true;
                        //if(0 == pos.row)
                        //{
                        //    if(FingerCheck(ModDef.Finger_1, !pos.fingerClose, false))
                        //    {
                        //        if(!BufferCheck(ModDef.Buffer_0, pos.fingerClose, false))
                        //        {
                        //            msg = string.Format("{0}抓手2有电池，缓存1位置有电池，机器人不能下降！", this.RunName);
                        //            ShowMessageBox((int)MsgID.BufStationDownAlm, msg, dispose, MessageType.MsgAlarm);
                        //            return false;
                        //        }
                        //        return true;
                        //    }
                        //}
                        //else if(1 == pos.row)
                        //{
                        //    for(int i = 0; i < (int)ModDef.Finger_ALL; i++)
                        //    {
                        //        // 抓手 && 暂存同时有
                        //        if(!FingerCheck((ModDef.Finger_0 + i), false, false) && !BufferCheck((ModDef.Buffer_0 + i), false, false))
                        //        {
                        //            msg = string.Format("{0}抓手{1}有电池，缓存{1}位置有电池，机器人不能下降！", this.RunName, (i + 1));
                        //            ShowMessageBox((int)MsgID.BufStationDownAlm, msg, dispose, MessageType.MsgAlarm);
                        //            return false;
                        //        }
                        //    }
                        //    return true;
                        //}
                        //else if(2 == pos.row)
                        //{
                        //    if(FingerCheck(ModDef.Finger_0, !pos.fingerClose, false))
                        //    {
                        //        if(!BufferCheck(ModDef.Buffer_1, pos.fingerClose, false))
                        //        {
                        //            msg = string.Format("{0}抓手1有电池，缓存2位置有电池，机器人不能下降！", this.RunName);
                        //            ShowMessageBox((int)MsgID.BufStationDownAlm, msg, dispose, MessageType.MsgAlarm);
                        //            return false;
                        //        }
                        //        return true;
                        //    }
                        //}
                        break;
                    }
                case OnloadRobotStation.NGOutput:
                    {
                        if (null != this.placeNGRun)
                        {
                            for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
                            {
                                if (FingerBat((ModDef)i).Type > BatteryStatus.Invalid)
                                {
                                    if (!this.placeNGRun.PlacePosInposIsSafe(pos.row + i))
                                    {
                                        return false;
                                    }
                                }
                            }
                            return this.placeNGRun.PlaceSenserIsSafe();
                        }
                        break;
                    }
                case OnloadRobotStation.OnloadFakeScan:
                case OnloadRobotStation.OnloadFake:
                    {
                        if (null != this.pickFakeRun)
                        {
                            return this.pickFakeRun.PickPosSenserInpos(true);
                        }
                        break;
                    }
                case OnloadRobotStation.DetectFake:
                    {
                        return true;
                        break;
                    }
                default:
                    break;
            }
            return false;
        }

        #endregion

        #region // 扫码器

        /// <summary>
        /// 扫码器的连接地址信息
        /// </summary>
        /// <returns></returns>
        public string ScanAdderInfo()
        {
            return this.barcodeScan.AdderInfo();
        }

        /// <summary>
        /// 扫码器连接状态
        /// </summary>
        /// <returns></returns>
        public bool ScanIsConnect()
        {
            if (!this.scanEnable)
            {
                return true;
            }
            return this.barcodeScan.IsConnect();
        }

        /// <summary>
        /// 扫码器连接
        /// </summary>
        /// <param name="connect">true连接，false断开</param>
        /// <returns></returns>
        public bool ScanConnect(bool connect = true)
        {
            if (!this.scanEnable || Def.IsNoHardware())
            {
                return true;
            }

            if (connect)
            {
                if (string.IsNullOrEmpty(this.barcodeScanIP) && (this.barcodeScanCom > -1))
                {
                    return this.barcodeScan.ConnectCom(this.barcodeScanCom, this.barcodeScanPort, (this.scanLinefeed ? "\r\n" : "\n"));
                }
                else if (!string.IsNullOrEmpty(this.barcodeScanIP) && (this.barcodeScanCom < 0))
                {
                    return this.barcodeScan.ConnectSocket(this.barcodeScanIP, this.barcodeScanPort);
                }
            }
            else
            {
                return this.barcodeScan.Disconnect();
            }
            return false;
        }

        /// <summary>
        /// 扫码器触发扫码
        /// </summary>
        /// <returns></returns>
        public bool ScanCode()
        {
            if (!this.scanEnable || Def.IsNoHardware())
            {
                return true;
            }
            if (!ScanIsConnect())
            {
                if (!ScanConnect(true))
                {
                    return false;
                }
            }
            string errMsg = "";
            if (this.barcodeScan.Send(scanCmd + (scanLinefeed ? "\r\n" : ""), ref errMsg))
            {
                return true;
            }
            ShowMessageBox((int)MsgID.ScanCodeFail, "触发扫码失败", "请检查扫码器连接", MessageType.MsgAlarm);
            return false;
        }

        /// <summary>
        /// 获取扫码器扫码结果
        /// </summary>
        /// <param name="code">获取到的条码</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public bool GetScanResult(ref string code, int timeout = 5 * 1000)
        {
            if (Def.IsNoHardware())
            {
                code = $"CV0000000{new Random().Next(10, 99)}";
                return true;
            }

            if (!this.scanEnable)
            {
                code = $"CV0000000{new Random().Next(10, 99)}";
                return true;
            }
            if (this.barcodeScan.Recv(ref code, timeout))
            {
                return true;
            }
            ShowMessageBox((int)MsgID.ScanCodeTimeout, "获取条码超时", "请检查扫码器", MessageType.MsgAlarm);
            return false;
        }

        /// <summary>
        /// 检查条码内容
        /// </summary>
        /// <param name="code">需要检查的条码</param>
        /// <param name="alm">是否报警</param>
        /// <returns></returns>
        public bool CheckScanCode(string code, bool alm)
        {
            if (!this.scanPalletEnable || Def.IsNoHardware())
            {
                return true;
            }
            string msg, disp;
            if (!string.IsNullOrEmpty(this.scanNGType) && (code.IndexOf(this.scanNGType) > -1))
            {
                if (alm)
                {
                    msg = string.Format("扫码器扫码失败，扫码器反馈：{0}", code);
                    disp = "请检查当前条码";
                    ShowMessageBox((int)MsgID.CodeTypeError, msg, disp, MessageType.MsgWarning);
                }
                return false;
            }
            if ((this.codeLength > -1) && (code.Length != this.codeLength))
            {
                if (alm)
                {
                    msg = string.Format("【{0}】条码长度和【条码长度：{1}】参数不匹配", code, this.codeLength);
                    disp = "请检查扫码器";
                    ShowMessageBox((int)MsgID.CodeLenError, msg, disp, MessageType.MsgAlarm);
                }
                return false;
            }
            if (this.codeTypeArray.Length > 0)
            {
                bool result = false;
                foreach (var item in this.codeTypeArray)
                {
                    if (code.EndsWith(item))
                    {
                        result = true;
                        break;
                    }
                }
                if (!result)
                {
                    if (alm)
                    {
                        msg = string.Format("【{0}】条码未在【条码类型：{1}】参数中找到匹配项", code, this.codeType);
                        disp = "请检查扫码器";
                        ShowMessageBox((int)MsgID.CodeTypeError, msg, disp, MessageType.MsgAlarm);
                    }
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 检查人工扫电池条码内容
        /// </summary>
        /// <param name="code">需要检查的条码</param>
        /// <param name="alm">是否报警</param>
        /// <returns></returns>
        public bool CheckBatteryManualScanCode(string code, bool alm)
        {
            string msg, disp;
            if (!string.IsNullOrEmpty(this.scanNGType) && (code.IndexOf(this.scanNGType) > -1))
            {
                if (alm)
                {
                    msg = string.Format("扫码器扫码失败，扫码器反馈：{0}", code);
                    disp = "请检查扫码器";
                    ShowMessageBox((int)MsgID.CodeTypeError, msg, disp, MessageType.MsgWarning);
                }
                return false;
            }
            if ((OnloadScanRun.codeLength > -1) && (code.Length != OnloadScanRun.codeLength))
            {
                if (alm)
                {
                    msg = string.Format("【{0}】条码长度和【条码长度：{1}】参数不匹配", code, OnloadScanRun.codeLength);
                    disp = "请检查扫码器";
                    ShowMessageBox((int)MsgID.CodeLenError, msg, disp, MessageType.MsgAlarm);
                }
                return false;
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
            if (!MachineCtrl.GetInstance().FingerCheckCanActive)
            {
                return true;
            }
            // 高位防呆
            if (RobotOrder.DOWN != this.robotDebugAction.order)
            {
                bool findOutput = false;
                for (int i = 0; i < (int)ModDef.Finger_ALL; i++)
                {
                    if (!InputState(IFingerCheck[i], false))
                    {
                        if (bOn && (Outputs(OFingerOpen[i]) == output))
                        {
                            findOutput = true;
                            break;
                        }
                        else if (!bOn && (Outputs(OFingerClose[i]) == output))
                        {
                            findOutput = true;
                            break;
                        }
                    }
                }
                if (findOutput)
                {
                    ShowMsgBox.ShowDialog("抓手有料，只能在机器人【下降】时打开抓手", MessageType.MsgWarning);
                    return false;
                }
            }
            else
            {
                // 变距气缸低位禁止操作
                if (((this.OIntervalCylPull > -1) && (Outputs(this.OIntervalCylPull) == output))
                    || ((this.OIntervalCylPush > -1) && (Outputs(this.OIntervalCylPush) == output)))
                {
                    ShowMsgBox.ShowDialog("机器人【下降】时不能操作变距气缸！", MessageType.MsgWarning);
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
            if ((MotorU > -1) && (Motors(MotorU) == motor))
            {
                if (this.RobotRunning)
                {
                    ShowMsgBox.ShowDialog("机器人运行中，不能移动调宽电机", MessageType.MsgWarning);
                    return false;
                }
                if ((RobotOrder.HOME != this.robotDebugAction.order)
                    && (RobotOrder.MOVE != this.robotDebugAction.order)
                    && (RobotOrder.UP != this.robotDebugAction.order))
                {
                    string msg = string.Format("机器人不在【{0}/{1}/{2}】，机器人不能操作调宽电机"
                        , RobotDef.RobotOrderName[(int)RobotOrder.HOME], RobotDef.RobotOrderName[(int)RobotOrder.MOVE], RobotDef.RobotOrderName[(int)RobotOrder.UP]);
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
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
            if (this.robotNeedEStop && !InputState(this.IFingerDelay, false))
            {
                OutputAction(this.ORobotEStop, true);
                this.robotNeedEStop = false;
                string msg = string.Format("{0} {1} 感应器ON，急停被触发", Inputs(IFingerDelay).Num, Inputs(IFingerDelay).Name);
                ShowMessageID((int)MsgID.RbtDelayEStop, msg, "请人工处理撞机问题", MessageType.MsgAlarm);
            }
            else if (!this.robotNeedEStop && !InputState(IFingerDelay, true))
            {
                this.robotNeedEStop = true;
            }
            if (this.RobotRunning || !InputState(IRobotRunning, false))
            {
                if (MachineCtrl.GetInstance().SafeDoorStateOpen && MachineCtrl.GetInstance().ClientIsConnect())
                {
                    // 只急停移动动作
                    if (RobotOrder.MOVE == robotDebugAction.order)
                    {
                        OutputAction(ORobotEStop, true);
                        ShowMessageID((int)MsgID.SafeDoorOpenRbtEStop, "安全门打开，机器人急停！", "请关闭安全门后再操作机器人", MessageType.MsgAlarm);
                    }
                }
            }
        }

        /// <summary>
        /// 设备停止后操作，如果派生类重写了该函数，它必须调用基实现。
        /// </summary>
        public override void AfterStopAction()
        {
            for (int i = 0; i < OPalletAlarm.Length; i++)
            {
                OutputAction(OPalletAlarm[i], false);
            }
            base.AfterStopAction();
        }

        /// <summary>
        /// 外部触发急停
        /// </summary>
        public void SetORobotEStop()
        {
            OutputAction(ORobotEStop, true);
            ShowMessageID((int)MsgID.SafeDoorOpenRbtEStop, "安全门打开，机器人急停！", "请关闭安全门后再操作机器人", MessageType.MsgAlarm);
        }

        #endregion

        #region // 添加删除夹具/电池

        public override void ManualAddPallet(int pltIdx, int maxRow, int maxCol, PalletStatus pltState, BatteryStatus batState)
        {
            // 仅空夹具
            if (PalletStatus.OK == pltState)
            {
                this.Pallet[pltIdx].State = PalletStatus.OK;
                this.Pallet[pltIdx].SetRowCol(maxRow, maxCol);
                SaveRunData(SaveType.Pallet, pltIdx);
            }
            //if (PalletStatus.NG == pltState)
            //{
            //    this.Pallet[pltIdx].State = PalletStatus.NG;
            //    this.Pallet[pltIdx].SetRowCol(maxRow, maxCol);
            //    SaveRunData(SaveType.Pallet, pltIdx);
            //}
        }

        public override void ManualAddPalletBattery(int pltIdx, int maxRow, int maxCol, bool isFake, PalletStatus pltState, BatteryStatus batState)
        {
            if (PalletStatus.OK == pltState)
            {
                this.Pallet[pltIdx].State = PalletStatus.OK;
                this.Pallet[pltIdx].SetRowCol(maxRow, maxCol);

                if (pltIdx > -1 && pltIdx <= (int)ModuleMaxPallet.OnloadRobot && Pallet[pltIdx].IsEmpty())
                {
                    System.Random rnd = new System.Random();
                    this.Pallet[pltIdx].Release();
                    this.Pallet[pltIdx].State = PalletStatus.OK;
                    this.Pallet[pltIdx].Stage = PalletStage.Onload;

                    for (int row = 0; row < (int)this.Pallet[pltIdx].MaxRow; row++)
                    {
                        for (int col = 0; col < (int)this.Pallet[pltIdx].MaxCol; col++)
                        {
                            if ((0 == row) && (0 == col) && isFake)
                            {
                                Pallet[pltIdx].Battery[row, col].Type = isFake ? BatteryStatus.Fake : BatteryStatus.OK;
                                Pallet[pltIdx].Battery[row, col].Code = $"TEST{rnd.Next(100000000, 900000000)}T{rnd.Next(100000000, 900000000)}";
                                continue;
                            }
                            Pallet[pltIdx].Battery[row, col].Type = BatteryStatus.OK;
                            Pallet[pltIdx].Battery[row, col].Code = $"TEST{rnd.Next(100000000, 900000000)}T{rnd.Next(100000000, 900000000)}";
                        }
                    }
                }

                if ((PalletStatus.OK == this.Pallet[pltIdx].State)
                                && (PalletStage.Onload == this.Pallet[pltIdx].Stage)
                                && !this.Pallet[pltIdx].IsEmpty() && !this.Pallet[pltIdx].HasFake())
                {
                    EventList modEvent = EventList.OnloadPickOKFullPallet;
                    EventStatus state = GetEvent(this, modEvent);
                    if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                    {
                        SetEvent(this, modEvent, EventStatus.Require, pltIdx);
                    }
                }
                // 上料区取OK带假电池满夹具
                if ((PalletStatus.OK == this.Pallet[pltIdx].State)
                    && (PalletStage.Onload == this.Pallet[pltIdx].Stage)
                    && this.Pallet[pltIdx].HasFake())
                {
                    EventList modEvent = EventList.OnloadPickOKFakeFullPallet;
                    EventStatus state = GetEvent(this, modEvent);
                    if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                    {
                        SetEvent(this, modEvent, EventStatus.Require, pltIdx);
                    }
                }

                SaveRunData(SaveType.Pallet, pltIdx);
            }
        }

        public override void ManualClearPallet(int pltIdx)
        {
            if (AutoSteps.Auto_WaitWorkStart == (AutoSteps)this.nextAutoStep)
            {
                // 清除夹具 已发送请求 → 禁止删除夹具
                for (EventList i = EventList.OnloadPlaceEmptyPallet; i < EventList.OnloadPickPlaceEnd; i++)
                {
                    int evtPltIdx = -1;
                    EventStatus state = GetEvent(this, i, ref evtPltIdx);
                    if ((evtPltIdx == pltIdx) && (EventStatus.Invalid != state) && (EventStatus.Finished != state))
                    {
                        ShowMsgBox.ShowDialog("当前夹具已发送取夹具事件，不能清除夹具", MessageType.MsgWarning);
                        return;
                    }
                }
                this.Pallet[pltIdx].Release();
                SaveRunData(SaveType.Pallet, pltIdx);
            }
            else
            {
                ShowMsgBox.ShowDialog("仅在等待开始信号步骤才能清除夹具", MessageType.MsgWarning);
            }
        }

        public override void ManualDelBattery(int pltIdx, int rowIdx, int colIdx)
        {
            //if (AutoSteps.Auto_WaitWorkStart == (AutoSteps)this.nextAutoStep)
            {
                // 清除电池 已发送请求 → 禁止删除电池
                //for (EventList i = EventList.OnloadPlaceEmptyPallet; i < EventList.OnloadPickPlaceEnd; i++)
                //{
                //    int evtPltIdx = -1;
                //    EventStatus state = GetEvent(this, i, ref evtPltIdx);
                //    if ((evtPltIdx == pltIdx) && (EventStatus.Invalid != state) && (EventStatus.Finished != state))
                //    {
                //        ShowMsgBox.ShowDialog("当前夹具已发送取电池事件，不能清除电池", MessageType.MsgWarning);
                //        return;
                //    }
                //}
                if (pltIdx == -1)
                {
                    this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
                    this.Battery[rowIdx].Release();
                    SaveRunData(SaveType.Battery, rowIdx);
                }
                else
                {
                    if (BatteryStatus.Fake != this.Pallet[pltIdx].Battery[rowIdx, colIdx].Type)
                        this.Pallet[pltIdx].Battery[rowIdx, colIdx].Release();
                    SaveRunData(SaveType.Pallet, pltIdx);
                }
            }
            //else
            //{
            //    ShowMsgBox.ShowDialog("仅在等待开始信号步骤才能清除电池", MessageType.MsgWarning);
            //}
        }

        public override void ManualAddBattery(int pltIdx, int rowIdx, int colIdx, string code)
        {
            //if (AutoSteps.Auto_WaitWorkStart == (AutoSteps)this.nextAutoStep)
            {
                // 清除电池 已发送请求 → 禁止删除电池
                //for (EventList i = EventList.OnloadPlaceEmptyPallet; i < EventList.OnloadPickPlaceEnd; i++)
                //{
                //    int evtPltIdx = -1;
                //    EventStatus state = GetEvent(this, i, ref evtPltIdx);
                //    if ((evtPltIdx == pltIdx) && (EventStatus.Invalid != state) && (EventStatus.Finished != state))
                //    {
                //        ShowMsgBox.ShowDialog("当前夹具已发送取电池事件，不能清除电池", MessageType.MsgWarning);
                //        return;
                //    }
                //}
                if (pltIdx == -1)
                {
                    this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                    this.Battery[rowIdx].Code = code;
                    this.Battery[rowIdx].Type = BatteryStatus.OK;
                    SaveRunData(SaveType.Battery, rowIdx);
                }
                else
                {
                    //this.nextAutoStep = AutoSteps.Auto_CalcPlacePos;
                    this.Pallet[pltIdx].Battery[rowIdx, colIdx].Code = code;
                    if (BatteryStatus.Fake != this.Pallet[pltIdx].Battery[rowIdx, colIdx].Type)
                        this.Pallet[pltIdx].Battery[rowIdx, colIdx].Type = BatteryStatus.OK;
                    SaveRunData(SaveType.Pallet, pltIdx);
                }
            }
            //else
            //{
            //    ShowMsgBox.ShowDialog("仅在等待开始信号步骤才能清除电池", MessageType.MsgWarning);
            //}
        }

        /// <summary>
        /// 查询工位电池
        /// </summary>
        /// <param name="pltIdx"></param>
        /// <param name="rowIdx"></param>
        /// <param name="colIdx"></param>
        public override void ManualGetBattery(int pltIdx, int rowIdx, int colIdx, ref string code)
        {
            if (pltIdx == -1)
            {
                code = this.Battery[rowIdx].Code;
            }
            else
            {
                code = this.Pallet[pltIdx].Battery[rowIdx, colIdx].Code;
            }
        }

        #endregion

        #region // 保存数据

        /// <summary>
        /// 保存电池绑定夹具数据
        /// </summary>
        private void SaveBatBindPltData(int batRow, int batCol, Battery bat, int pltIdx, string pltCode)
        {
            string file, title, text;
            file = string.Format(@"{0}\电芯绑定夹具\{1}\夹具位{2}-{3}-{1}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"), (pltIdx + 1), pltCode);
            title = "日期,时间,夹具条码,电芯行,电芯列,电芯条码,电芯状态";
            text = string.Format("{0},{1},{2},{3},{4},{5}\r\n", DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss"), pltCode, (batRow + 1), (batCol + 1), bat.Code, bat.Type);
            Def.ExportCsvFile(file, title, text);
        }

        #endregion
        /// <summary>
        /// 获取上料连接状态
        /// </summary>
        /// <returns></returns>
        public bool OnloadIsConnect()
        {
            return this.onloadClient.IsConnect();
        }

        /// <summary>
        /// 上料连接
        /// </summary>
        /// <param name="connect"></param>
        /// <returns></returns>
        public bool OnloadConnect(bool connect)
        {
            if (Def.IsNoHardware())
                return true;

            if (connect)
            {
                if (!OnloadIsConnect())
                {
                    this.onloadClient.SetFinsType(FinsType.Udp);
                    byte nodeID = Convert.ToByte(this.localIP.Substring(this.localIP.LastIndexOf('.') + 1));
                    return this.onloadClient.Connect(onloadIP, onloadPort, nodeID);
                }
            }
            else
            {
                this.onloadClient.Disconnect();
                this.onloadData.Release();
            }
            return OnloadIsConnect();
        }

        /// <summary>
        /// 获取上料IP信息
        /// </summary>
        /// <returns></returns>
        public string GetLoadingIPInfo()
        {
            return string.Format("{0}:{1}", this.onloadIP, this.onloadPort);
        }
        /// <summary>
        /// 获取上料数据
        /// </summary>
        /// <param name="ovenData"></param>
        /// <returns></returns>
        private bool GetLoadingData(ref OnloadData onloadData)
        {
            return this.onloadClient.GetLoadingData(ref onloadData);
        }
        #region // 后台线程

        /// <summary>
        /// 初始化线程(开始运行)
        /// </summary>
        private bool InitThread()
        {
            try
            {
                this.bgThread = new Task(RunWhileThread, TaskCreationOptions.LongRunning);
                this.bgThread.Start();
                Def.WriteLog("RunProcessDryingOven ", $"InitThread():RunWhileThread = {bgThread.Id} start", LogType.Success);
                return true;
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("RunProcessDryingOven", $"InitThread: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 释放线程(终止运行)
        /// </summary>
        private bool ReleaseThread()
        {
            try
            {
                this.bgThread.Wait();
                Def.WriteLog("RunProcessDryingOven", $"ReleaseThread():RunWhileThread = {bgThread.Id} end", LogType.Success);
                return true;
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("RunProcessDryingOven", $"ReleaseThread: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 后台线程
        /// </summary>
        private void RunWhileThread()
        {
            // 和主线程同生命周期
            while (!IsTerminate())
            {
                try
                {
                    RunWhile();
                    RunWhileTranSaft();
                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("RunProcessDryingOven", $"RunWhileThread: {ex.Message}\r\n{ex.StackTrace}");
                }
                Sleep(1);
            }
        }

        /// <summary>
        /// 循环函数
        /// </summary>
        protected void RunWhile()
        {
            if (!Def.IsNoHardware() && !OnloadIsConnect())
            {
                Sleep(200);
                OnloadConnect(true);
                return;
            }
            #region//心跳数据
            // 间隔5s 检查一次心跳
            if ((DateTime.Now - this.heartBeatTime).TotalSeconds >= 5.0)
            {
                if (onloadData.heartBeat == 0)
                {
                    onloadClient.SetLoadingData(LoadingCmd.ReadOrWriteHeartBeat, onloadData);
                }
                else
                {
                    ShowMessageID((int)MsgID.RbtMoveCmdError, "上料工位心跳异常", "请检查上料工位是否报警或故障，处理完毕后断开重新连接", MessageType.MsgAlarm);
                }
                this.heartBeatTime = DateTime.Now;
            }
            #endregion
            #region//权限数据

            #endregion
            if (!GetLoadingData(ref this.onloadData))
            {
                return;
            }
            #region // 通讯已连接，但数据交互错误

            if (this.onloadData.dataError)
            {
                ShowMessageID((int)MsgID.RbtMoveCmdError, "通讯已连接，但不能获取上料工位数据", "请检查上料工位是否报警或故障，处理完毕后断开重新连接", MessageType.MsgAlarm);
                return;
            }
            #endregion


            #region // 上料电池数据刷新
            for (int plt = 0; plt < Pallet.Length; plt++)
            {
                Pallet[plt].State = (PalletStatus)onloadData.palletDataArray[plt].state;
                //Console.WriteLine(string.Format("当前夹具{0}，读取状态：{1}", plt, Pallet[plt].State));
                Pallet[plt].Code = onloadData.palletDataArray[plt].code;
                Pallet[plt].NeedFake = onloadData.palletDataArray[plt].haveFake;
                for (int rowsIdx = 0; rowsIdx < Pallet[plt].Battery.GetLength(0); rowsIdx++)
                {
                    for (int colsIdx = 0; colsIdx < Pallet[plt].Battery.GetLength(1); colsIdx++)
                    {
                        Pallet[plt].Battery[rowsIdx, colsIdx].Type = onloadData.palletDataArray[plt].battery[rowsIdx, colsIdx].Type;
                        Pallet[plt].Battery[rowsIdx, colsIdx].Code = onloadData.palletDataArray[plt].battery[rowsIdx, colsIdx].Code;
                    }
                }
                this.PalletPosEnable[plt] = onloadData.palletDataArray[plt].enable;
            }
            #endregion
            #region//夹爪暂存数据更新
            for (int i = 0; i < onloadData.fingerSignal.Length; i++)
            {
                this.Battery[i].Type = onloadData.fingerSignal[i].Type;
                this.Battery[i].Code = onloadData.fingerSignal[i].Code;
            }
            for (int i = 0; i < onloadData.bufOnloadSignal.Length; i++)
            {
                this.Battery[(int)(ModDef.Buffer_0) + i].Type = onloadData.bufOnloadSignal[i].Type;
                this.Battery[(int)(ModDef.Buffer_0) + i].Code = onloadData.bufOnloadSignal[i].Code;
            }
            for (int i = 0; i < onloadData.batteryScan.Length; i++)
            {
                OnloadScanRun.Battery[i].Type = onloadData.batteryScan[i].Type;
                OnloadScanRun.Battery[i].Code = onloadData.batteryScan[i].Code;
            }
            #endregion
            MachineCtrl.GetInstance().onloadRunState = onloadData.runningState;
            //SaveRunData(SaveType.Pallet);
            Sleep(200);
        }

        #endregion
        /// <summary>
        /// 循环函数
        /// </summary>
        protected void RunWhileTranSaft()
        {
            if (Def.IsNoHardware())
                return;
            if (!IsModuleEnable())
                return;
            if (!this.OnloadIsConnect())
                return;
            Sleep(1000);
            onloadData.tranSaft = this.transAvoid;
            onloadClient.SetLoadingData(LoadingCmd.WriteTranSaft, onloadData);

            int aa = (int)this.dbRecord.UserLevel();
            onloadData.roleID = ((int)this.dbRecord.UserLevel() + 1) == 4 ? 0 : ((int)this.dbRecord.UserLevel() + 1);
            onloadClient.SetLoadingData(LoadingCmd.WriteRoleID, onloadData);

            onloadData.opName = MachineCtrl.GetInstance().OperaterID;
            onloadClient.SetLoadingData(LoadingCmd.WriteOPName, onloadData);
        }
        #region//发送托盘信息
        public override void SetPallet(int pltIdx, Pallet pallet, bool place)
        {
            uint count = 0;
            for (int rowsIdx = 0; rowsIdx < onloadData.palletData.battery.GetLength(0); rowsIdx++)
                for (int colsIdx = 0; colsIdx < onloadData.palletData.battery.GetLength(1); colsIdx++)
                {
                    if (pallet.Battery[rowsIdx, colsIdx] != null)
                    {
                        onloadData.palletData.battery[rowsIdx, colsIdx].Type = pallet.Battery[rowsIdx, colsIdx].Type;
                        onloadData.palletData.battery[rowsIdx, colsIdx].Code = pallet.Battery[rowsIdx, colsIdx].Code;
                        if (!string.IsNullOrEmpty(onloadData.palletData.battery[rowsIdx, colsIdx].Code))
                        {
                            count++;
                        }
                    }
                }

            onloadData.palletData.count = count;
            onloadData.palletData.code = pallet.Code;
            onloadData.palletData.state = pallet.State;

            //判断是否是上料放夹具，如果是则判断炉子正常夹具和假电池夹具数量多少，正常>=假电池则放置为假电池夹具
            if (place && !(pallet.State == PalletStatus.ReputFake || pallet.State == PalletStatus.NG))
            {
                pallet.NeedFake = CalcNeedFakeBatteryPlt();
            }
            onloadData.palletData.haveFake = pallet.NeedFake;

            onloadClient.SetLoadingData(LoadingCmd.WritePalletCodeUp, onloadData);
            onloadClient.SetLoadingData(LoadingCmd.WritePalletCodeMid, onloadData);
            onloadClient.SetLoadingData(LoadingCmd.WritePalletCodeDown, onloadData);
            onloadClient.SetLoadingData(LoadingCmd.WritePallet, onloadData);

            onloadClient.SetLoadingData(LoadingCmd.WriteInfoEnd, onloadData);
            //Console.WriteLine(string.Format("当前夹具{0}，设置状态：{1}", pltIdx,pallet.State));
        }
        private bool AvoidTranRobot()
        {
            //上料与调度防呆
            rotoActionInfo = transferRoot.GetRobotActionInfo(false);

            if (rotoActionInfo.station == (int)TransferRobotStation.OnloadStation)
            {
                if (rotoActionInfo.order == RobotOrder.PICKIN
                || rotoActionInfo.order == RobotOrder.PICKOUT
                || rotoActionInfo.order == RobotOrder.PLACEIN
                || rotoActionInfo.order == RobotOrder.PLACEOUT)
                {
                    return false;
                }
            }
            return true;
        }
        //public bool SetRoleToOnload(int roleID) 
        //{
        //    if (Def.IsNoHardware())
        //        return true;
        //    if (!IsModuleEnable())
        //        return true;
        //    if (!this.OnloadIsConnect())
        //        return false;
        //    onloadData.roleID = roleID+1;
        //    return onloadClient.SetLoadingData(LoadingCmd.WriteRoleID, onloadData);
        //}
        #endregion

        #region // 上传Mes数据

        #endregion
        #region // 模组重置

        public override void ManualResetEvent()
        {
            //查找调度机器人当前关联的模组
            LoadRunData();
            for (int i = (int)EventList.OnloadPlaceEmptyPallet; i < (int)EventList.OnloadPickPlaceEnd; i++)
            {
                SetEvent(this, (EventList)i, EventStatus.Invalid);
            }
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            SaveRunData(SaveType.AutoStep);
        }
        #endregion

        #region // 上料信号复位

        public void ClearEvent()
        {
            this.transAvoid = onloadData.tranSaft = 1;
            onloadClient.SetLoadingData(LoadingCmd.WriteTranSaft, onloadData);

            onloadData.transFlag = 0;
            onloadClient.SetLoadingData(LoadingCmd.WriteTrans, onloadData);

            SaveRunData(SaveType.AutoStep | SaveType.Avoid);
        }
        #endregion


        #region // 复投电池信息
        public void ReBattery(string batCode)
        {
            for (int i = 0; i < onloadData.reBatteryState.Length; i++)
            {
                onloadData.reBatteryState[i] = 1;
            }

            for (int i = 0; i < onloadData.reBattery.Length; i++)
            {
                onloadData.reBattery[i].Code = batCode;
            }
            onloadClient.SetLoadingData(LoadingCmd.WriteReBattery, onloadData);
        }
        #endregion
    }
}