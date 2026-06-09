using HelperLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;
using System.Text;
using Machine.Framework;
using System.Linq;
using Machine.MYSQL;
using static Machine.MYSQL.MySqlProcess;
using Machine.Framework.Mes;

namespace Machine
{
    /// <summary>
    /// 干燥炉
    /// </summary>
    class RunProcessDryingOven : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_ConnectDryOven,
            Init_CloseDryOvenDoor,
            Init_OpenDryOvenDoor,
            Init_CheckDryOvenDoor,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,

            // 开门取放
            Auto_PrecloseOvenDoor,          // 关闭非开门层的炉门
            Auto_PreblowAir,                // 开门前破真空：仅破当前炉腔
            Auto_OpenOvenDoor,              // 打开炉门
            Auto_CheckPalletState,          // 取放前检查夹具状态
            Auto_WaitActionFinish,          // 等待动作完成
            Auto_FinishedCheckPltState,     // 完成后检查夹具状态
            Auto_CloseOvenDoor,             // 关闭炉门
            Auto_UpdateMesBindCavity,       // 上传绑炉腔信息

            // 启动加热
            Auto_SetOvenWorkStop,           // 发送当前层工作停止
            Auto_CheckPressure,             // 启动前检查真空值
            Auto_SendWorkParameter,         // 发送参数
            Auto_SetOvenWorkStart,          // 发送启动命令
            Auto_UpdateMesWorkStart,        // 上传启动信息

            Auto_WorkEnd,
        }

        private enum ModDef
        {

        }

        private enum MsgID
        {
            Start = ModuleMsgID.DryingOvenMsgStartID,
            CheckDoor,
            RobotFingerIn,
            DoorOpenClose,
            VacOpenClose,
            BlowOpenClose,
            PressureOpenClose,
            WorkStartStop,
            SetParameter,
            FaultReset,
            SetMcDoor,
            DoorAlarm,
            VacAlarm = DoorAlarm + OvenRowCol.MaxRow,
            BlowAlarm = VacAlarm + +OvenRowCol.MaxRow,
            VacuometerAlarm = BlowAlarm + OvenRowCol.MaxRow,
            ControlAlarm = VacuometerAlarm + OvenRowCol.MaxRow * OvenRowCol.MaxCol,
            PltCheckAlarm = ControlAlarm + OvenRowCol.MaxRow * OvenRowCol.MaxCol,
            TempAlarm = PltCheckAlarm + OvenRowCol.MaxRow,
            HeatStop = TempAlarm + OvenRowCol.MaxRow,
            HeatTimeout = HeatStop + OvenRowCol.MaxRow,
            HeatVacAlarm = HeatTimeout + OvenRowCol.MaxRow,
            WorkingOpenDoor,
            WorkingVacuum,
            WorkingBlowAir,
            WorkingPressure,
            WorkingSetParameter,
            OpenDoorPressureAlm,
            OpenMultiDoorAlm,
            PltStateErr,
            RemoteErr,
            OvenDataError,
            BindCavityErr,
            BakingStatusErr,
            WaterValueErr,
            GetBillParaErr,
            ProductionRecordErr,
            RejectNGErr,
            FTPUploadErr,
            MesNGErr,
        }

        #endregion

        #region // 取放位置结构体

        private struct PickPlacePos
        {
            #region // 字段
            public int row;
            public int col;
            public EventList operateEvent;
            #endregion

            #region // 方法

            public void SetData(int curRow, int curCol, EventList curEvent)
            {
                this.row = curRow;
                this.col = curCol;
                this.operateEvent = curEvent;
            }

            public void Release()
            {
                this.row = -1;
                this.col = -1;
                this.operateEvent = EventList.Invalid;
            }
            #endregion
        };
        #endregion

        #region // 字段，属性

        #region // IO
        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        public int dryingOvenID;           // 干燥炉ID
        #endregion

        #region // 模组参数
        /// <summary>
        /// 腔体使能：true启用，false禁用
        /// </summary>
        public bool[] CavityEnable { get; private set; }        // 腔体使能：true启用，false禁用
        /// <summary>
        /// 腔体复烘：true启用，false禁用
        /// </summary>
        public bool[] CavityOvenEx { get; private set; }        // 腔体复烘：true启用，false禁用
        /// <summary>
        /// 腔体保压：true启用，false禁用
        /// </summary>
        public bool[] CavityPressure { get; private set; }      // 腔体保压：true启用，false禁用
        /// <summary>
        /// 腔体转移：true启用，false禁用
        /// </summary>
        public bool[] CavityTransfer { get; private set; }      // 腔体转移：true启用，false禁用
        /// <summary>
        /// 腔体转移接收：true启用，false禁用
        /// </summary>
        public bool[] CavityTransferRecv { get; private set; }      // 腔体转移：true启用，false禁用
        /// <summary>
        /// 腔体抽检周期：每N次放一次假电池夹具抽检
        /// </summary>
        public int[] CavitySamplingCycle { get; private set; }  // 腔体抽检周期：每N次放一次假电池夹具抽检
        /// <summary>
        /// 腔体加热次数：当前第N次加热
        /// </summary>
        public int[] CavityHeartCycle { get; private set; }     // 腔体加热次数：当前第N次加热

        public string[] CavityNo { get; private set; }     // 编号

        private int dryingOvenGroup;                    // 干燥炉分组：0左靠近上料，1右靠近上料
        private string localIP;                         // 本机IP
        private string ovenIP;                          // 干燥炉IP
        private int ovenPort;                           // 干燥炉IP的Port
        private CavityParameter cavityParameter;        // 干燥炉工艺参数
        private double positiveWaterStandard;                   // 正极水含量标准值：<则合格，>则超标重新回炉干燥
        private double negativeWaterStandard;                   // 负极水含量标准值：<则合格，>则超标重新回炉干燥
        private double separatorWaterStandard;                  // 隔膜水含量标准值：<则合格，>则超标重新回炉干燥
        private double blendWaterStandard;                      // 混合水含量标准值：<则合格，>则超标重新回炉干燥
        private int openDoorDelay;                      // 开门防呆时间：秒
        private int maxWorkTimeRange;                   // 最大工作时间误差范围：分钟min
        private int workDataTime;                       // 加热时数据保存时间间隔：秒
        private bool waitResultPressure;                // 等待测试结果时自动保压：true启用，false禁用
        private bool[] cavityRlarmStatus;            //干燥炉报警状态

        private bool ovenConnected;

        #endregion

        private string equipmentCode { get; set; }           // 设备编码
        private string resourceCode { get; set; }            // 资源编码

        #region // 模组数据

        public CavityStatus[] CavityState { get; private set; }             // 腔体状态
        public List<List<List<double>>> PltHeatTemp { get; private set; }  // 夹具加热温度：夹具<发热板<温度值<>>>
        public List<List<List<uint>>> PltHeatTime { get; private set; }    // 夹具加热时间：夹具<发热板<时间值<>>>

        private PickPlacePos operatePos;                // 当前操作位置
        public double[,] waterContentValue;            // 水含量值：炉层,2个水含量
        private DryingOvenData readOvenData;            // 读取干燥炉数据
        private DryingOvenData writeOvenData;           // 写入干燥炉数据
        private DateTime[] bakingDataStartTime;         // 腔体开始保存干燥数据时间
        private DryingOvenClient ovenClient;            // 干燥炉连接
        //private int lineID;                             // 线体ID：从MachineCtrl获取
        private Task runWhileTask;                      // 任务运行线程
        private CavityStatus[] CavityOldState;          // 腔体上一次状态，MES用

        #endregion

        #region 相关模组
        RunProcessRobotTransfer transferRobotRun;   // 调度模组：transferRun = 
        #endregion

        #endregion

        #region // 构造析构

        public RunProcessDryingOven(int runId) : base(runId)
        {
            InitBatteryPalletSize(0, (int)ModuleMaxPallet.DryingOven);

            PowerUpRestart();

            this.ovenClient = new DryingOvenClient();

            //this.ovenLogFile = new LogFile();

            InitParameter();
            // 参数

            // 以下较为固定参数放最后
            InsertGeneralParameter("SetTempValue", "1)设定温度", "1)设定温度：摄氏度", cavityParameter.SetTempValue, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertGeneralParameter("TempUpperlimit", "2)温度上限", "2)温度上限：摄氏度", cavityParameter.TempUpperlimit, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertGeneralParameter("TempLowerlimit", "3)温度下限", "3)温度下限：摄氏度", cavityParameter.TempLowerlimit, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertGeneralParameter("OpenDoorVacPressure", "4)开门真空压力", "4)开门真空压力：Pa", cavityParameter.OpenDoorVacPressure, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertGeneralParameter("OpenDoorBlowTime", "5)充氮气报警时间", "5)开门破真空时长：分钟", cavityParameter.OpenDoorBlowTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertGeneralParameter("BStateBlowAirTime", "12)呼吸充干燥气时间", "12)呼吸充干燥气时间：分钟", cavityParameter.BStateBlowAirTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertGeneralParameter("HeatPlate", "6)发热板数", "6)发热板数：块", cavityParameter.HeatPlate, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertGeneralParameter("MaxNGHeatPlate", "7)最大NG发热板", "7)最大NG发热板数：块", cavityParameter.MaxNGHeatPlate, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);

            InsertVentilationPhase("AStateVacTime", "8)A状态抽真空报警时间", "8)A状态抽真空报警时间：分钟", cavityParameter.AStateVacTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVentilationPhase("AStateVacPressure", "9)A状态真空压力", "9)A状态真空压力：Pa", cavityParameter.AStateVacPressure, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);

            InsertPreheatingPhase("PreheatTime", "10)预热时间", "10)预热时间：分钟", cavityParameter.PreheatTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertPreheatingPhase("HeatPreBlow", "11)预热充干燥气压力", "11)预热充干燥气压力：Pa", cavityParameter.HeatPreBlow, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);


            InsertVacuumHeatingPhase("VacHeatTime", "12)真空加热总时间", "12)真空加热总时间：分钟", cavityParameter.VacHeatTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);

            InsertVacuumHeatingPhase("AStateVacMaxValue", "13)A状态真空最大值", "13)干燥炉真空值：Pa", cavityParameter.AStateVacMaxValue, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVacuumHeatingPhase("AStateSavePressureTime", "14)A状态真空保压时间", "14)A状态真空保压时间：分钟", cavityParameter.AStateSavePressureTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);

            InsertVacuumHeatingPhase("BStateVacTime", "15)B状态抽真空报警时间", "15)B状态抽真空报警时间：分钟", cavityParameter.BStateVacTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVacuumHeatingPhase("BStateVacPressure", "16)B状态真空压力", "16)B状态真空压力：Pa", cavityParameter.BStateVacPressure, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVacuumHeatingPhase("BStateVacMaxValue", "17)B状态真空最大值", "17)B状态真空最大值：Pa", cavityParameter.BStateVacMaxValue, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVacuumHeatingPhase("BStateSavePressureTime", "18)B状态真空保压时间", "18)B状态真空保压时间：分钟", cavityParameter.BStateSavePressureTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);

            InsertVacuumHeatingPhase("BStateBlowAirKeepTime", "19)呼吸充干燥气保持时间", "19)呼吸充干燥气保持时间：分钟", cavityParameter.BStateBlowAirKeepTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVacuumHeatingPhase("BStateBlowAirPressure", "20)呼吸充干燥气压力", "20)呼吸充干燥气压力：Pa", cavityParameter.BStateBlowAirPressure, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertOvenParameter("BreathTimeInterval", "15)呼吸时间间隔", "15)呼吸时间间隔：分钟", cavityParameter.BreathTimeInterval, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVacuumHeatingPhase("BreathCycleTimes", "21)呼吸循环次数", "21)呼吸循环次数：次", cavityParameter.BreathCycleTimes, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);

            //InsertOvenParameter("HeatPreVacTime", "19)加热前抽真空时间", "19)加热前抽真空时间：分钟", cavityParameter.HeatPreVacTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);

            //InsertOvenParameter("TempDifferAlarmValue", "23)温差报警值", "23)温差报警值：Pa", cavityParameter.TempDifferAlarmValue, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);

            for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                InsertVoidParameter(("CavityEnable" + i), ((i + 1) + "层腔体使能"), "腔体使能：true启用，false禁用", CavityEnable[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            }
            //for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            //{
            //    InsertVoidParameter(("CavityOvenEx" + i), ((i + 1) + "层腔体复烘"), "腔体复烘：true启用，false禁用", CavityOvenEx[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            //}
            for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                InsertVoidParameter(("CavityPressure" + i), ((i + 1) + "层腔体保压"), "腔体保压：true启用，false禁用", CavityPressure[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            }
            for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                InsertVoidParameter(("CavityTransfer" + i), ((i + 1) + "层腔体转移"), "腔体转移：true启用，false禁用", CavityTransfer[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            }
            for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                InsertVoidParameter(("CavityTransferRecv" + i), ((i + 1) + "层腔体转移接收"), "腔体转移接收：true启用，false禁用", CavityTransferRecv[i], RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            }
            for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                InsertVoidParameter(("CavitySamplingCycle" + i), ((i + 1) + "腔体抽检周期"), "腔体抽检周期：每N次放一次假电池夹具抽检", CavitySamplingCycle[i], RecordType.RECORD_INT);
            }
            InsertVoidParameter("waitResultPressure", "测试后保压", "测试水含量后自动保压：true启用，false禁用", waitResultPressure, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_OPER);
            InsertVoidParameter("positiveWaterStandard", "正极水含量标准", "正极水含量标准值：<则合格，>则超标重新回炉干燥", positiveWaterStandard, RecordType.RECORD_DOUBLE, ParameterLevel.PL_ALL_MAIN);
            InsertVoidParameter("negativeWaterStandard", "负极水含量标准", "负极水含量标准值：<则合格，>则超标重新回炉干燥", negativeWaterStandard, RecordType.RECORD_DOUBLE, ParameterLevel.PL_ALL_MAIN);
            InsertVoidParameter("separatorWaterStandard", "隔膜水含量标准", "隔膜水含量标准值：<则合格，>则超标重新回炉干燥", separatorWaterStandard, RecordType.RECORD_DOUBLE, ParameterLevel.PL_ALL_MAIN);
            InsertVoidParameter("blendWaterStandard", "混合水含量标准", "混合水含量标准值：<则合格，>则超标重新回炉干燥", blendWaterStandard, RecordType.RECORD_DOUBLE, ParameterLevel.PL_ALL_MAIN);
            InsertVoidParameter("workDataTime", "加热数据间隔", "加热时数据保存时间间隔：秒", workDataTime, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("openDoorDelay", "开门防呆时间", "开门防呆时间：秒", openDoorDelay, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("maxWorkTimeRange", "工作时间误差", "最大工作时间误差范围：分钟", maxWorkTimeRange, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("equipmentCode", "设备编码", "设备编码", equipmentCode, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("resourceCode", "资源编码", "资源编码", resourceCode, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
           

            //// 以下较为固定参数放最后-复烘参数
            //InsertCopyOvenParameterEx("SetTempValueEx", "1)设定温度", "1)设定温度：摄氏度", cavityParameter.SetTempValueEx, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("TempUpperlimitEx", "2)温度上限", "2)温度上限：摄氏度", cavityParameter.TempUpperlimitEx, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("TempLowerlimitEx", "3)温度下限", "3)温度下限：摄氏度", cavityParameter.TempLowerlimitEx, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("PreheatTimeEx", "4)预热时间", "4)预热时间：分钟", cavityParameter.PreheatTimeEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("VacHeatTimeEx", "5)加热时间", "5)真空加热时间：分钟", cavityParameter.VacHeatTimeEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("OpenDoorBlowTimeEx", "6)开门破真空时长", "6)开门破真空时长：分钟", cavityParameter.OpenDoorBlowTimeEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("OpenDoorVacPressureEx", "7)开门真空压力", "7)开门真空压力：Pa", cavityParameter.OpenDoorVacPressureEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("AStateVacTimeEx", "8)A状态抽真空时间", "8)A状态抽真空时间：分钟", cavityParameter.AStateVacTimeEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("AStateVacPressureEx", "9)A状态真空压力", "9)A状态真空压力：Pa", cavityParameter.AStateVacPressureEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("BStateVacTimeEx", "10)B状态抽真空时间", "10)B状态抽真空时间：分钟", cavityParameter.BStateVacTimeEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("BStateVacPressureEx", "11)B状态真空压力", "11)B状态真空压力：Pa", cavityParameter.BStateVacPressureEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("BStateBlowAirTimeEx", "12)呼吸充干燥气时间", "12)呼吸充干燥气时间：分钟", cavityParameter.BStateBlowAirTimeEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("BStateBlowAirPressureEx", "13)呼吸充干燥气压力", "13)呼吸充干燥气压力：Pa", cavityParameter.BStateBlowAirPressureEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("BStateBlowAirKeepTimeEx", "14)呼吸充干燥气保持时间", "14)呼吸充干燥气保持时间：分钟", cavityParameter.BStateBlowAirKeepTimeEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("BreathTimeIntervalEx", "15)呼吸时间间隔", "15)呼吸时间间隔：分钟", cavityParameter.BreathTimeIntervalEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("BreathCycleTimesEx", "16)呼吸循环次数", "16)呼吸循环次数：次", cavityParameter.BreathCycleTimesEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("HeatPlateEx", "17)发热板数", "17)发热板数：块", cavityParameter.HeatPlateEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("MaxNGHeatPlateEx", "18)最大NG发热板", "18)最大NG发热板数：块", cavityParameter.MaxNGHeatPlateEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("HeatPreVacTimeEx", "19)加热前抽真空时间", "19)加热前抽真空时间：分钟", cavityParameter.HeatPreVacTimeEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("HeatPreBlowEx", "20)加热前充干燥气压力", "20)加热前充干燥气压力：Pa", cavityParameter.HeatPreBlowEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("VacMinValueEx", "21)真空最小值", "21)干燥炉真空值：Pa", cavityParameter.VacMinValueEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("VacMaxValueEx", "22)真空最大值", "22)干燥炉真空值：Pa", cavityParameter.VacMaxValueEx, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            //InsertCopyOvenParameterEx("TempDifferAlarmValueEx", "23)温差报警值", "23)温差报警值：Pa", cavityParameter.TempDifferAlarmValueEx, RecordType.RECORD_DOUBLE, ParameterLevel.PL_STOP_MAIN);


            InsertOvenParameter("DryingOvenGroup", "干燥炉分组", "干燥炉分组：0左靠近上料，1右靠近上料", dryingOvenGroup, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);
            InsertOvenParameter("ovenIP", "干燥炉IP", "干燥炉IP", ovenIP, RecordType.RECORD_STRING, ParameterLevel.PL_IDLE_ADMIN);
            InsertOvenParameter("ovenPort", "干燥炉端口", "干燥炉IP的Port", ovenPort, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);


        }

        ~RunProcessDryingOven()
        {
            ReleaseThread();
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
                        this.nextInitStep = InitSteps.Init_ConnectDryOven;
                        break;
                    }

                case InitSteps.Init_ConnectDryOven:
                    {
                        CurMsgStr("连接干燥炉", "Connect drying oven");
                        if (DryOvenConnect(true))
                        {
                            ovenConnected = true;

                            IOTData pdata = new IOTData();
                            pdata.line = MachineCtrl.GetInstance().LineID;
                            pdata.equip = this.ovenIP;
                            pdata.floor = "0";
                            pdata.datetime = DateTime.Now;
                            PointData points = new PointData();
                            points.code = "HHKD1001";
                            points.name = "plc与上位机连接状态结果";
                            points.type = "BOOL";
                            points.unit = "";
                            points.value = "true";
                            pdata.points = new List<PointData>();
                            pdata.points.Add(points);
                            IOTTaskList.Add(pdata);

                            Sleep(1000);
                            this.nextInitStep = InitSteps.Init_CloseDryOvenDoor;
                        }
                        break;
                    }
                case InitSteps.Init_CloseDryOvenDoor:
                    {
                        for (int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
                        {
                            this.msgChs = string.Format("关闭{0}层干燥炉炉门", rowIdx + 1);
                            this.msgEng = string.Format("Close drying oven door {0}", rowIdx + 1);
                            CurMsgStr(this.msgChs, this.msgEng);
                            // 关门时不能在当前炉层进
                            if (WCavity(rowIdx).doorState != (short)OvenStatus.DoorOpen)
                            {
                                if (CheckRobotTransferSafe(rowIdx))
                                {
                                    WriteLog("InitOperation()操作：" + this.msgChs);
                                    CavityData cavity = new CavityData();
                                    cavity.doorState = (short)OvenStatus.DoorClose;
                                    if (!Def.IsNoHardware() && !DryOvenOpenDoor(rowIdx, cavity, true))
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    ShowMessageBox((int)MsgID.DoorOpenClose, "调度机器人插料架在此干燥炉中，不能开关炉门", "请先移出调度机器人插料架后再启动", MessageType.MsgWarning);
                                    return;
                                }
                            }
                        }
                        this.nextInitStep = InitSteps.Init_OpenDryOvenDoor;
                        break;
                    }
                case InitSteps.Init_OpenDryOvenDoor:
                    {
                        for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                        {
                            this.msgChs = string.Format("打开{0}层干燥炉炉门", i + 1);
                            this.msgEng = string.Format("Open drying oven door {0}", i + 1);
                            CurMsgStr(this.msgChs, this.msgEng);
                            // 开门时不能在任何一层进
                            if ((WCavity(i).doorState == (short)OvenStatus.DoorOpen) && (RCavity(i).doorState != (short)OvenStatus.DoorOpen))
                            {
                                if (CheckRobotTransferSafe(-1))
                                {
                                    WriteLog("InitOperation()操作：" + this.msgChs);
                                    CavityData cavity = new CavityData();
                                    cavity.doorState = (short)OvenStatus.DoorOpen;
                                    if (!Def.IsNoHardware() && !DryOvenOpenDoor(i, cavity, true))
                                    {
                                        return;
                                    }
                                }
                                else
                                {
                                    ShowMessageBox((int)MsgID.DoorOpenClose, "调度机器人插料架在此干燥炉中，不能开关炉门", "请先移出调度机器人插料架后再启动", MessageType.MsgWarning);
                                    return;
                                }
                            }
                        }
                        this.nextInitStep = InitSteps.Init_CheckDryOvenDoor;
                        break;
                    }
                case InitSteps.Init_CheckDryOvenDoor:
                    {
                        for (int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
                        {
                            this.msgChs = string.Format("检查{0}层干燥炉炉门", rowIdx + 1);
                            this.msgEng = string.Format("Check drying oven door {0}", rowIdx + 1);
                            CurMsgStr(this.msgChs, this.msgEng);
                            if (!Def.IsNoHardware() && ((short)OvenStatus.Unknown != WCavity(rowIdx).doorState) && (WCavity(rowIdx).doorState != RCavity(rowIdx).doorState))
                            {
                                string doorState = ((short)OvenStatus.DoorOpen == WCavity(rowIdx).doorState) ? "打开" : "关闭";
                                string msg = string.Format("{0}层炉门状态不正确，应该是{1}", rowIdx + 1, doorState);
                                string dispose = string.Format("请先将{0}层炉门手动恢复到【{1}】后再继续", rowIdx + 1, doorState);
                                ShowMessageBox((int)MsgID.CheckDoor, msg, dispose, MessageType.MsgWarning);
                                return;
                            }

                            for (int colIdx = 0; colIdx < (int)OvenRowCol.MaxCol; colIdx++)
                            {
                                int Idx = rowIdx * (int)OvenRowCol.MaxCol + colIdx;
                                if ((int)OvenStatus.PalletHave == MachineCtrl.GetInstance().GetPalletPosSenser((RunID)this.GetRunID(), Idx))
                                {
                                    //有夹具
                                    if (PalletStatus.Invalid == this.Pallet[Idx].State)
                                    {
                                        this.Pallet[Idx].State = PalletStatus.OK;
                                        this.Pallet[Idx].Stage = PalletStage.Onload;
                                        this.Pallet[Idx].SetRowCol(MachineCtrl.GetInstance().PalletMaxRow, MachineCtrl.GetInstance().PalletMaxCol);

                                        SaveRunData(SaveType.Pallet, Idx);
                                    }
                                }
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
                Sleep(200);
            }

            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");

                        // 遍历所有炉层
                        for (int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
                        {
                            #region // 检查等待干燥炉腔
                            if (CheckWaitWorkStart(rowIdx))
                            {
                                // 每次加热启动如果有假电池夹具，则清除水含量值
                                for (int colIdx = 0; colIdx < (int)OvenRowCol.MaxCol; colIdx++)
                                {
                                    if (this.Pallet[rowIdx * (int)OvenRowCol.MaxCol + colIdx].HasFake())
                                    {
                                        for (int i = 0; i < this.waterContentValue.GetLength(1); i++)
                                        {
                                            this.waterContentValue[rowIdx, i] = 0.0;
                                        }
                                    }
                                }
                                // 每次启动，清楚上次加热过程数据
                                if (null != this.PltHeatTemp)
                                {
                                    for (int col = 0; col < (int)OvenRowCol.MaxCol; col++)
                                    {
                                        int idx = rowIdx * (int)OvenRowCol.MaxCol + col;
                                        // 控温、巡检
                                        for (int i = 0; i < 2; i++)
                                        {
                                            // 发热板
                                            for (int j = 0; j < (int)OvenInfoCount.HeatPanelCount; j++)
                                            {
                                                int heatIdx = i * (int)OvenInfoCount.HeatPanelCount + j;
                                                this.PltHeatTemp[idx][heatIdx].Clear();
                                                this.PltHeatTime[idx][heatIdx].Clear();
                                            }
                                        }
                                    }
                                }
                                this.operatePos.SetData(rowIdx, -1, EventList.Invalid);
                                this.nextAutoStep = AutoSteps.Auto_SetOvenWorkStop;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                break;
                            }
                            #endregion

                            // 检查水含量
                            CheckWaterContentResult(rowIdx, this.waterContentValue);

                            EventList modEvent;
                            EventStatus state;

                            #region // 发送放夹具信号

                            #region // 空夹具
                            int pltIdx = rowIdx * (int)OvenRowCol.MaxCol;
                            if (CavityEnable[rowIdx] && !CavityPressure[rowIdx] && !CavityTransfer[rowIdx] && !CavityTransferRecv[rowIdx] && (CavityStatus.Normal == CavityState[rowIdx])
                                && ((PalletStatus.Invalid == this.Pallet[pltIdx].State) || (PalletStatus.Invalid == this.Pallet[pltIdx + 1].State)))
                            {
                                // 干燥炉放空夹具
                                modEvent = EventList.DryOvenPlaceEmptyPallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require);
                                }
                                // 干燥炉放NG非空夹具
                                modEvent = EventList.DryOvenPlaceNGPallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require);
                                }
                                // 干燥炉放NG空夹具
                                modEvent = EventList.DryOvenPlaceNGEmptyPallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    SetEvent(this, modEvent, EventStatus.Require);
                                }
                                if (1 == this.transferRobotRun.placeFakePalletCnt)
                                {
                                    // 干燥炉放上料完成OK满夹具
                                    modEvent = EventList.DryOvenPlaceOnlOKFullPallet;
                                    state = GetEvent(this, modEvent);
                                    if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                    {
                                        SetEvent(this, modEvent, EventStatus.Require);
                                    }
                                    if ((0 == (this.CavityHeartCycle[rowIdx] % this.CavitySamplingCycle[rowIdx]))
                                    && (!this.Pallet[pltIdx].HasFake() && !this.Pallet[pltIdx + 1].HasFake()))
                                    {
                                        // 干燥炉放上料完成OK带假电池满夹具
                                        modEvent = EventList.DryOvenPlaceOnlOKFakeFullPallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require);
                                        }
                                    }
                                }
                                else
                                {
                                    if ((0 == (this.CavityHeartCycle[rowIdx] % this.CavitySamplingCycle[rowIdx]))
                                    && (this.Pallet[pltIdx].State == PalletStatus.Invalid || this.Pallet[pltIdx + 1].State == PalletStatus.Invalid))
                                    {
                                        // 干燥炉放上料完成OK带假电池满夹具
                                        modEvent = EventList.DryOvenPlaceOnlOKFakeFullPallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require);
                                        }
                                    }
                                }
                            }
                            #endregion // 空夹具

                            //if (CavityEnable[rowIdx] && !CavityPressure[rowIdx] && !CavityTransfer[rowIdx] && (CavityStatus.Normal == CavityState[rowIdx])
                            //    && ((PalletStatus.Invalid == this.Pallet[pltIdx].State) || (PalletStatus.Invalid == this.Pallet[pltIdx + 1].State)))
                            //{
                            //}

                            #region // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
                            if (CavityEnable[rowIdx] && !CavityPressure[rowIdx] && !CavityTransfer[rowIdx] && !CavityTransferRecv[rowIdx] && (CavityStatus.WaitDetect == CavityState[rowIdx]))
                            {
                                modEvent = EventList.DryOvenPlaceWaitResultPallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        if (PalletStatus.Invalid == this.Pallet[pltIdx + i].State)
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                }
                            }
                            #endregion 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）

                            #region // 干燥炉放回炉假电池夹具（已放回假电池的夹具）
                            if (CavityEnable[rowIdx] && !CavityPressure[rowIdx] && !CavityTransfer[rowIdx] && !CavityTransferRecv[rowIdx] && (CavityStatus.WaitRebaking == CavityState[rowIdx])
                                && ((PalletStatus.Invalid == this.Pallet[pltIdx].State) || (PalletStatus.Invalid == this.Pallet[pltIdx + 1].State)))
                            {
                                modEvent = EventList.DryOvenPlaceRebakeFakePallet;
                                state = GetEvent(this, modEvent);
                                if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                {
                                    for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                    {
                                        if (PalletStatus.Invalid == this.Pallet[pltIdx + i].State)
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                }
                            }
                            #endregion 干燥炉放回炉假电池夹具（已放回假电池的夹具）

                            #region // 干燥炉转移放夹具：放至目的炉腔

                            int rid = this.GetRunID();
                            //if (rid == (int)RunID.DryOven0 && rowIdx == 0)
                            //{
                            //    Trace.WriteLine($"干燥炉:{rid - (int)RunID.DryOven0}");
                            //}
                            if (CavityEnable[rowIdx] && !CavityPressure[rowIdx] && CavityTransferRecv[rowIdx] /*&& (CavityStatus.Normal == CavityState[rowIdx])*/)
                            {
                                //Trace.WriteLine($"放至目的炉腔:{rowIdx}");
                                bool IsTransferFinish = true;
                                for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                {
                                    // 接收腔体夹具为空
                                    if (PalletStatus.Invalid == this.Pallet[pltIdx + i].State)
                                    {
                                        modEvent = EventList.DryOvenPlaceTransferPallet;
                                        EventStatus status = GetEvent(this, modEvent);
                                        if (status == EventStatus.Invalid || status == EventStatus.Finished)
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, pltIdx + i);
                                        }
                                        IsTransferFinish = false;
                                    }
                                }

                                if (IsTransferFinish)
                                {
                                    SetCavityTransferState(rowIdx, CavityStatus.Normal);
                                }
                            }

                            // 干燥炉转移取夹具：取来源炉腔
                            if (CavityEnable[rowIdx] && !CavityPressure[rowIdx] && CavityTransfer[rowIdx]/* && (CavityStatus.Maintenance == CavityState[rowIdx])*/)
                            {
                                if (CavityStatus.Maintenance != CavityState[rowIdx])
                                {
                                    SetCavityState(rowIdx, CavityStatus.Maintenance);
                                }

                                for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                {
                                    // 有夹具
                                    if (PalletStatus.Invalid != this.Pallet[pltIdx + i].State)
                                    {
                                        modEvent = EventList.DryOvenPickTransferPallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 无夹具
                                    else
                                    {
                                        modEvent = EventList.DryOvenPickTransferPallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid != state) && (EventStatus.Finished != state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Finished, (pltIdx + i));
                                        }
                                    }
                                }
                            }

                            #endregion 干燥炉转移放夹具：放至目的炉腔

                            #endregion

                            #region // 发送取夹具信号

                            if (CavityEnable[rowIdx] && !CavityPressure[rowIdx] && !CavityTransfer[rowIdx] && !CavityTransferRecv[rowIdx])
                            {
                                for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                {
                                    // 干燥炉取空夹具
                                    if ((CavityStatus.Normal == CavityState[rowIdx]) && (PalletStatus.OK == this.Pallet[pltIdx + i].State) && this.Pallet[pltIdx + i].IsEmpty())
                                    {
                                        modEvent = EventList.DryOvenPickEmptyPallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 干燥炉取NG非空夹具
                                    if ((CavityStatus.Normal == CavityState[rowIdx]) && (PalletStatus.NG == this.Pallet[pltIdx + i].State) && !this.Pallet[pltIdx + i].IsEmpty())
                                    {
                                        modEvent = EventList.DryOvenPickNGPallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 干燥炉取NG空夹具
                                    if ((CavityStatus.Normal == CavityState[rowIdx]) && (PalletStatus.NG == this.Pallet[pltIdx + i].State) && this.Pallet[pltIdx + i].IsEmpty())
                                    {
                                        modEvent = EventList.DryOvenPickNGEmptyPallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 干燥炉取待检测含假电池夹具（未取走假电池的夹具）
                                    if ((CavityStatus.WaitDetect == CavityState[rowIdx]) && (PalletStatus.Detect == this.Pallet[pltIdx + i].State) && this.Pallet[pltIdx + i].HasFake())
                                    {
                                        modEvent = EventList.DryOvenPickDetectFakePallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 干燥炉取待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
                                    if ((CavityStatus.WaitRebaking == CavityState[rowIdx]) && (PalletStatus.ReputFake == this.Pallet[pltIdx + i].State) && this.Pallet[pltIdx + i].HasFake())
                                    {
                                        modEvent = EventList.DryOvenPickReputFakePallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                    // 干燥炉取干燥完成夹具（等待下料）
                                    if ((CavityStatus.Normal == CavityState[rowIdx]) && (PalletStatus.WaitOffload == this.Pallet[pltIdx + i].State) && !this.Pallet[pltIdx + i].IsEmpty())
                                    {
                                        modEvent = EventList.DryOvenPickDryFinishPallet;
                                        state = GetEvent(this, modEvent);
                                        if ((EventStatus.Invalid == state) || (EventStatus.Finished == state))
                                        {
                                            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                                        }
                                    }
                                }
                            }

                            #endregion
                        }

                        #region // 有请求已响应
                        for (EventList modEvent = EventList.DryOvenPlaceEmptyPallet; modEvent < EventList.DryOvenPickPlaceEnd; modEvent++)
                        {
                            int pltIdx = -1;
                            if ((EventStatus.Response == GetEvent(this, modEvent, ref pltIdx))
                                && (-1 < pltIdx) && (pltIdx < (int)OvenRowCol.MaxRow * (int)OvenRowCol.MaxCol))
                            {
                                this.operatePos.SetData(pltIdx / 2, pltIdx % 2, modEvent);

                                this.nextAutoStep = AutoSteps.Auto_PrecloseOvenDoor;
                                SaveRunData(SaveType.AutoStep | SaveType.Variables);
                                break;
                            }
                        }
                        #endregion

                        break;
                    }

                #region // 开门取放
                case AutoSteps.Auto_PrecloseOvenDoor:
                    {
                        bool result = true;
                        for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                        {
                            if (CavityEnable[i] && !CavityPressure[i] && !CavityTransfer[i])
                            {
                                this.msgChs = string.Format("{0}层炉门预先关闭", i + 1);
                                this.msgEng = string.Format("{0} row oven door is closed in advance", i + 1);
                                CurMsgStr(this.msgChs, this.msgEng);

                                WriteLog("AutoOperation()操作：" + this.msgChs);
                                writeOvenData.CavityDatas[i].doorState = (int)OvenStatus.DoorClose;
                                if (!Def.IsNoHardware() && !DryOvenOpenDoor(i, writeOvenData.CavityDatas[i], true))
                                {
                                    result = false;
                                    break;
                                }
                            }
                        }
                        if (result)
                        {
                            this.nextAutoStep = AutoSteps.Auto_PreblowAir;
                            SaveRunData(SaveType.AutoStep | SaveType.Cavity);
                        }
                        break;
                    }
                case AutoSteps.Auto_PreblowAir:
                    {
                        // wjj add 220615 
                        // 待操作的炉层出异常，使能关闭，停止当前的动作，把控制给其他的炉层
                        if (!CavityEnable[this.operatePos.row])
                        {
                            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
                            break;
                        }
                        // wjj add 220615 

                        this.msgChs = string.Format("{0}层开门前破真空", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row open blow air before open door", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if ((-1 < this.operatePos.row) && (this.operatePos.row <= (int)OvenRowCol.MaxRow))
                        {
                            if (GetDryOvenData(ref readOvenData))
                            {
                                if (Def.IsNoHardware() || (RCavity(this.operatePos.row).vacPressure > this.cavityParameter.OpenDoorVacPressure))
                                {
                                    //this.nextAutoStep = AutoSteps.Auto_OpenOvenDoor;

                                    // wjj add 220612
                                    // 等待破真空结束 破真空阀关闭
                                    if (Def.IsNoHardware() || (int)OvenStatus.BlowClose == RCavity(this.operatePos.row).blowValveState)
                                    {
                                        this.nextAutoStep = AutoSteps.Auto_OpenOvenDoor;
                                    }
                                    // wjj add 220612
                                }
                                else if ((int)OvenStatus.BlowOpen != RCavity(this.operatePos.row).blowValveState)
                                {
                                    WriteLog("AutoOperation()操作：" + this.msgChs + "：关闭真空阀，打开破真空阀");
                                    this.writeOvenData.CavityDatas[this.operatePos.row].vacValveState = (int)OvenStatus.VacClose;
                                    DryOvenVacuum(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);
                                    this.writeOvenData.CavityDatas[this.operatePos.row].blowValveState = (int)OvenStatus.BlowOpen;
                                    DryOvenBlowAir(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_OpenOvenDoor:
                    {
                        this.msgChs = string.Format("{0}层开门", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row open door", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        WriteLog("AutoOperation()操作：" + this.msgChs);
                        this.writeOvenData.CavityDatas[this.operatePos.row].doorState = (int)OvenStatus.DoorOpen;
                        if (Def.IsNoHardware() || DryOvenOpenDoor(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                        {
                            this.writeOvenData.CavityDatas[this.operatePos.row].blowValveState = (int)OvenStatus.BlowClose;
                            if (!this.DryRun)
                                DryOvenBlowAir(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);

                            this.nextAutoStep = AutoSteps.Auto_CheckPalletState;
                            SaveRunData(SaveType.AutoStep | SaveType.Cavity);
                        }
                        break;
                    }
                case AutoSteps.Auto_CheckPalletState:
                    {
                        this.msgChs = string.Format("{0}层开门后检查夹具状态", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row check cavity pallet", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltIdx = -1;
                        EventStatus state = GetEvent(this, this.operatePos.operateEvent, ref pltIdx);
                        if ((EventStatus.Response == state) && (-1 < pltIdx) && (pltIdx <= (int)ModuleMaxPallet.DryingOven))
                        {
                            if (GetDryOvenData(ref readOvenData))
                            {
                                if (!Def.IsNoHardware() && !PalletKeepFlat(pltIdx, (this.Pallet[pltIdx].State > PalletStatus.Invalid), true))
                                {
                                    return;
                                }
                                else if (SetEvent(this, this.operatePos.operateEvent, EventStatus.Ready, pltIdx))
                                {
                                    this.nextAutoStep = AutoSteps.Auto_WaitActionFinish;
                                    SaveRunData(SaveType.AutoStep);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_WaitActionFinish:
                    {
                        this.msgChs = string.Format("{0}层炉门已打开等待动作完成", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row door is opened and wait action finsih", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltIdx = -1;
                        EventStatus state = GetEvent(this, this.operatePos.operateEvent, ref pltIdx);
                        if ((EventStatus.Finished == state) && ((pltIdx / (int)OvenRowCol.MaxCol) == this.operatePos.row))
                        {
                            switch (this.operatePos.operateEvent)
                            {
                                // 干燥炉放上料完成OK满夹具
                                case EventList.DryOvenPlaceOnlOKFullPallet:
                                // 干燥炉放上料完成OK带假电池满夹具
                                case EventList.DryOvenPlaceOnlOKFakeFullPallet:
                                    {
                                        SetCavityState(this.operatePos.row, CavityStatus.Normal);
                                        break;
                                    }
                                // 干燥炉放回炉假电池夹具（已放回假电池的夹具）
                                case EventList.DryOvenPlaceRebakeFakePallet:
                                    {
                                        int idx = 0;
                                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                        {
                                            idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                            if (1 == this.transferRobotRun.placeFakePalletCnt)
                                            {
                                                if ((PalletStatus.ReputFake == this.Pallet[idx].State)
                                                || (PalletStatus.Rebaking == this.Pallet[idx].State))
                                                {
                                                    this.Pallet[idx].State = PalletStatus.OK;
                                                }
                                            }
                                            else
                                            {
                                                if ((PalletStatus.Rebaking == this.Pallet[idx].State))
                                                {
                                                    this.Pallet[idx].State = PalletStatus.OK;
                                                }
                                            }
                                        }
                                        idx = this.operatePos.row * (int)OvenRowCol.MaxCol;
                                        if (PalletStatus.OK == this.Pallet[idx].State && PalletStatus.OK == this.Pallet[idx + 1].State)
                                        {
                                            SetCavityState(this.operatePos.row, CavityStatus.Normal);
                                        }

                                        break;
                                    }
                                // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
                                case EventList.DryOvenPlaceWaitResultPallet:
                                    {
                                        //for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                        //{
                                        //    int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                        //    if (PalletStatus.Detect == this.Pallet[idx].State) 
                                        //    {
                                        //        this.Pallet[idx].State = PalletStatus.WaitResult;
                                        //    }
                                        //}
                                        //SetCavityState(this.operatePos.row, CavityStatus.WaitResult);

                                        //wjj 220830
                                        bool IsDetect = false;
                                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                        {
                                            int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                            if (PalletStatus.Detect == this.Pallet[idx].State
                                                && (BatteryStatus.FakeTag == this.Pallet[idx].Battery[0, 0].Type || BatteryStatus.OK == this.Pallet[idx].Battery[0, 0].Type)) //wjj 220830
                                            {
                                                this.Pallet[idx].State = PalletStatus.WaitResult;
                                            }
                                            //if (this.Pallet[idx].Battery[0, 0].Type == BatteryStatus.Fake || this.Pallet[idx].Battery[0, 0].Type == BatteryStatus.Invalid)
                                            //{
                                            //    IsDetect = true;
                                            //}
                                        }
                                        if (!IsDetect)
                                            SetCavityState(this.operatePos.row, CavityStatus.WaitResult);

                                        break;
                                    }
                                default:
                                    break;
                            }
                            this.nextAutoStep = AutoSteps.Auto_FinishedCheckPltState;
                            SaveRunData(SaveType.AutoStep | SaveType.Pallet | SaveType.Cavity);
                        }
                        break;
                    }
                case AutoSteps.Auto_FinishedCheckPltState:
                    {
                        this.msgChs = string.Format("{0}层取放完成后检查夹具状态", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row check cavity pallet after action", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        int pltIdx = this.operatePos.row * (int)OvenRowCol.MaxCol;
                        if (pltIdx > -1)
                        {
                            if (GetDryOvenData(ref readOvenData))
                            {
                                for (int pltCol = 0; pltCol < (int)OvenRowCol.MaxCol; pltCol++)
                                {
                                    if (!Def.IsNoHardware() && !PalletKeepFlat(pltIdx + pltCol, (this.Pallet[pltIdx + pltCol].State > PalletStatus.Invalid), true))
                                    {
                                        return;
                                    }
                                }
                                this.nextAutoStep = AutoSteps.Auto_CloseOvenDoor;
                                SaveRunData(SaveType.AutoStep);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_CloseOvenDoor:
                    {
                        this.msgChs = string.Format("{0}层关门", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row close door", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        WriteLog("AutoOperation()操作：" + this.msgChs);
                        this.writeOvenData.CavityDatas[this.operatePos.row].doorState = (int)OvenStatus.DoorClose;
                        if (Def.IsNoHardware() || DryOvenOpenDoor(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                        {
                            this.nextAutoStep = AutoSteps.Auto_UpdateMesBindCavity;
                            SaveRunData(SaveType.AutoStep | SaveType.Cavity);
                        }
                        break;
                    }
                case AutoSteps.Auto_UpdateMesBindCavity:
                    {
                        this.msgChs = $"{this.operatePos.row + 1}层夹具{this.operatePos.col + 1}上传绑炉腔信息";
                        this.msgEng = $"{this.operatePos.row + 1}row pallet {this.operatePos.col + 1}updata mes";
                        CurMsgStr(this.msgChs, this.msgEng);

                        int pltIdx = this.operatePos.row * (int)OvenRowCol.MaxCol + this.operatePos.col;
                        switch (this.operatePos.operateEvent)
                        {
                            // 干燥炉放上料完成OK满夹具
                            case EventList.DryOvenPlaceOnlOKFullPallet:
                            // 干燥炉放上料完成OK带假电池满夹具
                            case EventList.DryOvenPlaceOnlOKFakeFullPallet:
                                {
                                    string msg = "";
                                    if (Def.IsNoHardware() /*&& !MesOperate.EquToMesInBaking(MesResources.Equipment, this.Pallet[pltIdx], ref msg)*/)
                                    {
                                        ShowMessageID((int)MsgID.BindCavityErr, msg, $"请检查 {this.Pallet[pltIdx].Code} 的绑炉腔信息", MessageType.MsgAlarm);
                                        return;
                                    }
                                    else
                                    {
                                        int cavityIdx = pltIdx % (int)OvenRowCol.MaxCol + 1;
                                        IOTData pdata = new IOTData();
                                        pdata.line = MachineCtrl.GetInstance().LineID;
                                        pdata.equip = this.ovenIP;
                                        pdata.floor = cavityIdx.ToString();
                                        pdata.datetime = DateTime.Now;
                                        pdata.points = new List<PointData>();

                                        PointData points = new PointData();
                                        points.code = "HHKD0001";
                                        points.name = "设备编号";
                                        points.type = "String";
                                        points.unit = "";
                                        points.value = $"{this.equipmentCode}-{cavityIdx}-{pltIdx % 2 + 1}";
                                        pdata.points.Add(points);

                                        string code = "";
                                        int batCnt = 0;
                                        for (int row = 0; row < this.Pallet[pltIdx].MaxRow; row++)
                                        {
                                            for (int col = 0; col < this.Pallet[pltIdx].MaxCol; col++)
                                            {
                                                if (!string.IsNullOrEmpty(this.Pallet[pltIdx].Battery[row, col].Code) && this.Pallet[pltIdx].Battery[row, col].Type != BatteryStatus.Fake)
                                                {
                                                    if (!string.IsNullOrEmpty(code))
                                                    {
                                                        code += ",";
                                                    }
                                                    code += this.Pallet[pltIdx].Battery[row, col].Code;
                                                    batCnt++;
                                                }
                                            }
                                        }

                                        pdata.points.Add(new PointData() { code = "HHKD0005", name = "腔体编码", type = "String", unit = "", value = this.equipmentCode });
                                        if (pltIdx % 2 == 0)
                                        {
                                            pdata.points.Add(new PointData() { code = "HHKD0002", name = "左托盘编码", type = "String", unit = "", value = this.Pallet[pltIdx].Code });
                                            pdata.points.Add(new PointData() { code = "HHKD0006", name = "左电芯编码", type = "String", unit = "", value = code });
                                            pdata.points.Add(new PointData() { code = "HHKD2001", name = "左电芯数量", type = "Int16", unit = "", value = $"{batCnt}" });
                                        }
                                        else
                                        {
                                            pdata.points.Add(new PointData() { code = "HHKD0003", name = "右托盘编码", type = "String", unit = "", value = this.Pallet[pltIdx].Code });
                                            pdata.points.Add(new PointData() { code = "HHKD0007", name = "右电芯编码", type = "String", unit = "", value = code });
                                            pdata.points.Add(new PointData() { code = "HHKD2002", name = "右电芯数量", type = "Int16", unit = "", value = $"{batCnt}" });
                                        }

                                        IOTTaskList.Add(pdata);
                                    }
                                    break;
                                }
                            default:
                                break;
                        }
                        this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                        SaveRunData(SaveType.AutoStep);
                        break;
                    }
                #endregion

                #region // 启动加热
                case AutoSteps.Auto_SetOvenWorkStop:
                    {
                        this.msgChs = string.Format("{0}层炉腔停止加热", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row set cavity work stop", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);

                        this.writeOvenData.CavityDatas[this.operatePos.row].workState = (int)OvenStatus.WorkStop;
                        if (Def.IsNoHardware() || DryOvenWorkStart(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                        {
                            WriteLog($"AutoOperation()操作：{this.operatePos.row + 1}层炉腔自动加热前预先停止加热");
                            this.nextAutoStep = AutoSteps.Auto_CheckPressure;
                            SaveRunData(SaveType.AutoStep | SaveType.Cavity);
                        }
                        break;
                    }
                case AutoSteps.Auto_CheckPressure:
                    {
                        this.msgChs = string.Format("{0}层启动加热前破真空", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row open blow air before work start", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if ((-1 < this.operatePos.row) && (this.operatePos.row <= (int)OvenRowCol.MaxRow))
                        {
                            if (GetDryOvenData(ref readOvenData))
                            {
                                if (Def.IsNoHardware() || (RCavity(this.operatePos.row).vacPressure > this.cavityParameter.AStateVacPressure))
                                {
                                    this.writeOvenData.CavityDatas[this.operatePos.row].blowValveState = (int)OvenStatus.BlowClose;
                                    if (!Def.IsNoHardware())
                                        DryOvenBlowAir(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);

                                    this.nextAutoStep = AutoSteps.Auto_SendWorkParameter;
                                }
                                else if ((int)OvenStatus.BlowOpen != RCavity(this.operatePos.row).blowValveState)
                                {
                                    this.writeOvenData.CavityDatas[this.operatePos.row].vacValveState = (int)OvenStatus.VacClose;
                                    DryOvenVacuum(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);
                                    this.writeOvenData.CavityDatas[this.operatePos.row].blowValveState = (int)OvenStatus.BlowOpen;
                                    DryOvenBlowAir(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true);
                                }
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_SendWorkParameter:
                    {
                        string msg = "";
                        string lotno = "";
                        string vehicleno = "";
                        string oporder = MesResources.OpOrder;
                        string workplace = "DAL1HK01";
                        MesRecipeStruct mesRecipeStruct = new MesRecipeStruct();
                        //if (!Jeve_Mes.Mes_GetParam(lotno, vehicleno, oporder, workplace, ref mesRecipeStruct, ref msg))
                        //{
                        //    //ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                        //}
                        this.msgChs = string.Format("{0}层设置炉腔参数", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row set cavity parameter", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if ((-1 < this.operatePos.row) && (this.operatePos.row <= (int)OvenRowCol.MaxRow))
                        {
                            //this.writeOvenData.CavityDatas[this.operatePos.row].cavityParameter.Copy(this.cavityParameter);

                            if (Def.IsNoHardware() || DryOvenSetParameter(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                            {
                                this.nextAutoStep = AutoSteps.Auto_SetOvenWorkStart;
                                SaveRunData(SaveType.Cavity);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_SetOvenWorkStart:
                    {
                        this.msgChs = string.Format("{0}层启动加热", this.operatePos.row + 1);
                        this.msgEng = string.Format("{0} row set cavity work start", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if ((-1 < this.operatePos.row) && (this.operatePos.row <= (int)OvenRowCol.MaxRow))
                        {
                            for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                            {
                                int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                this.Pallet[idx].StartDate = DateTime.Now;
                                this.Pallet[idx].EndDate = DateTime.MinValue;
                            }
                            this.writeOvenData.CavityDatas[this.operatePos.row].workState = (int)OvenStatus.WorkStart;
                            if (Def.IsNoHardware() || DryOvenWorkStart(this.operatePos.row, this.writeOvenData.CavityDatas[this.operatePos.row], true))
                            {
                                WriteLog($"AutoOperation()操作：{this.operatePos.row + 1}层炉腔自动加热，发送启动");


                                SetCavityState(this.operatePos.row, CavityStatus.Heating);
                                this.CavityHeartCycle[this.operatePos.row]++;
                                if (this.CavityHeartCycle[this.operatePos.row] >= this.CavitySamplingCycle[this.operatePos.row])
                                {
                                    this.CavityHeartCycle[this.operatePos.row] = 0;
                                }

                                this.nextAutoStep = AutoSteps.Auto_UpdateMesWorkStart;
                                SaveRunData(SaveType.AutoStep | SaveType.Cavity | SaveType.Variables | SaveType.Pallet);
                            }
                        }
                        break;
                    }
                case AutoSteps.Auto_UpdateMesWorkStart:
                    {
                        this.msgChs = string.Format("{0}层上传MES启动加热信息", this.operatePos.row + 1);
                        this.msgEng = string.Format("update MES {0} row work start and", this.operatePos.row + 1);
                        CurMsgStr(this.msgChs, this.msgEng);
                        if ((-1 < this.operatePos.row) && (this.operatePos.row <= (int)OvenRowCol.MaxRow))
                        {
                            // 启动，上传启动加热
                            {
                                Pallet[] plt = new Pallet[(int)OvenRowCol.MaxCol];
                                for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                {
                                    int idx = this.operatePos.row * (int)OvenRowCol.MaxCol + i;
                                    this.Pallet[idx].BakingCount++;
                                    plt[i] = this.Pallet[idx];
                                }

                                this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                                SaveRunData(SaveType.AutoStep | SaveType.Pallet);
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
                        Trace.Assert(false, "RunEx::AutoOperation/no this run step");
                        break;
                    }
            }

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

            this.dryingOvenID = IniFile.ReadInt(module, "DryingOvenID", -1, Def.GetAbsPathName(Def.ModuleExCfg));

            if (!InitThread())
            {
                ShowMsgBox.ShowDialog((module + " 后台线程初始化失败"), MessageType.MsgWarning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            this.CavityEnable = new bool[(int)OvenRowCol.MaxRow];
            this.CavityOvenEx = new bool[(int)OvenRowCol.MaxRow];
            this.CavityPressure = new bool[(int)OvenRowCol.MaxRow];
            this.CavityTransfer = new bool[(int)OvenRowCol.MaxRow];
            this.CavityTransferRecv = new bool[(int)OvenRowCol.MaxRow];
            this.CavitySamplingCycle = new int[(int)OvenRowCol.MaxRow];
            for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                this.CavityEnable[i] = false;
                this.CavityOvenEx[i] = false;
                this.CavityPressure[i] = false;
                this.CavityTransfer[i] = false;
                this.CavityTransferRecv[i] = false;
                this.CavitySamplingCycle[i] = 1;
            }
            this.equipmentCode = "";
            this.resourceCode = "";
            this.localIP = "127.0.0.21";
            this.ovenIP = "";
            this.ovenPort = 9600;
            this.ovenConnected = false;
            this.cavityParameter.Release();
            this.positiveWaterStandard = 500.0;
            this.negativeWaterStandard = 500.0;
            this.separatorWaterStandard = 500.0;
            this.blendWaterStandard = 500.0;
            this.dryingOvenGroup = 0;
            this.openDoorDelay = 30;
            this.maxWorkTimeRange = 2;
            this.waitResultPressure = false;
            this.cavityRlarmStatus = new bool[(int)OvenRowCol.MaxRow];
            for (int i = 0; i < cavityRlarmStatus.Length; i++)
                cavityRlarmStatus[i] = false;
            this.workDataTime = 10;
            this.PltHeatTemp = new List<List<List<double>>>();
            this.PltHeatTime = new List<List<List<uint>>>();
            for (int pltIdx = 0; pltIdx < (int)ModuleMaxPallet.DryingOven; pltIdx++)
            {
                this.PltHeatTemp.Add(new List<List<double>>());
                this.PltHeatTime.Add(new List<List<uint>>());
                for (int i = 0; i < 2 * (int)OvenInfoCount.HeatPanelCount; i++)
                {
                    this.PltHeatTemp[pltIdx].Add(new List<double>());
                    this.PltHeatTemp[pltIdx][i].Capacity = 2000;
                    this.PltHeatTime[pltIdx].Add(new List<uint>());
                    this.PltHeatTime[pltIdx][i].Capacity = 2000;
                }
            }

            base.InitParameter();
        }

        /// <summary>
        /// 添加干燥炉IP配置
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertOvenParameter(string key, string name, string description, object value, RecordType paraType, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.RunModule, name, key, value.ToString(), paraType, paraLevel));
            this.ParameterProperty.Add("干燥炉IP配置", key, name, description, value, (int)paraLevel, readOnly, visible);

        }
        

        /// <summary>
        /// 添加通用参数设定值
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertGeneralParameter(string key, string name, string description, object value, RecordType paraType, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.RunModule, name, key, value.ToString(), paraType, paraLevel));
            this.ParameterProperty.Add("1工艺参数：通用参数设定", key, name, description, value, (int)paraLevel, readOnly, visible);
            
        }

        /// <summary>
        /// 添加换气阶段参数值
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertVentilationPhase(string key, string name, string description, object value, RecordType paraType, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.RunModule, name, key, value.ToString(), paraType, paraLevel));
            this.ParameterProperty.Add("2工艺参数：换气阶段参数", key, name, description, value, (int)paraLevel, readOnly, visible);

        }

        /// <summary>
        /// 添加预热阶段参数值
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertPreheatingPhase(string key, string name, string description, object value, RecordType paraType, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.RunModule, name, key, value.ToString(), paraType, paraLevel));
            this.ParameterProperty.Add("3工艺参数：预热阶段参数", key, name, description, value, (int)paraLevel, readOnly, visible);

        }

        /// <summary>
        /// 添加真空呼吸阶段参数值
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertVacuumHeatingPhase(string key, string name, string description, object value, RecordType paraType, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.RunModule, name, key, value.ToString(), paraType, paraLevel));
            this.ParameterProperty.Add("4工艺参数：真空呼吸阶段参数", key, name, description, value, (int)paraLevel, readOnly, visible);

        }

        /// <summary>
        /// 添加复烘参数参数
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertCopyOvenParameterEx(string key, string name, string description, object value, RecordType paraType, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN, bool readOnly = false, bool visible = true)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.RunModule, name, key, value.ToString(), paraType, paraLevel));
            this.ParameterProperty.Add("复烘参数", key, name, description, value, (int)paraLevel, readOnly, visible);
        }
        public override bool CheckParameter(string name, object value)
        {
            // 转炉换腔由false改为true
            for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                if ((("CavityTransfer" + i) == name) && Convert.ToBoolean(value))
                {
                    if ((CavityStatus.Normal != this.CavityState[i]) && (CavityStatus.Maintenance != this.CavityState[i]))
                    {
                        ShowMsgBox.ShowDialog($"【腔体转移{name}】参数只能在腔体正常状态及维护状态下修改", MessageType.MsgAlarm);
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                this.CavityEnable[i] = ReadBoolParameter(this.RunModule, ("CavityEnable" + i), this.CavityEnable[i]);
                this.CavityPressure[i] = ReadBoolParameter(this.RunModule, ("CavityPressure" + i), this.CavityPressure[i]);
                this.CavityTransfer[i] = ReadBoolParameter(this.RunModule, ("CavityTransfer" + i), this.CavityTransfer[i]);
                this.CavityTransferRecv[i] = ReadBoolParameter(this.RunModule, ("CavityTransferRecv" + i), this.CavityTransferRecv[i]);
                this.CavitySamplingCycle[i] = ReadIntParameter(this.RunModule, ("CavitySamplingCycle" + i), this.CavitySamplingCycle[i]);

                this.CavityOvenEx[i] = ReadBoolParameter(this.RunModule, ("CavityOvenEx" + i), this.CavityOvenEx[i]);
            }

            this.ovenIP = ReadStringParameter(this.RunModule, "ovenIP", this.ovenIP);
            this.ovenPort = ReadIntParameter(this.RunModule, "ovenPort", this.ovenPort);
            this.positiveWaterStandard = ReadDoubleParameter(this.RunModule, "positiveWaterStandard", this.positiveWaterStandard);
            this.negativeWaterStandard = ReadDoubleParameter(this.RunModule, "negativeWaterStandard", this.negativeWaterStandard);
            this.separatorWaterStandard = ReadDoubleParameter(this.RunModule, "separatorWaterStandard", this.separatorWaterStandard);
            blendWaterStandard = ReadDoubleParameter(this.RunModule, "blendWaterStandard", this.blendWaterStandard);
            this.dryingOvenGroup = ReadIntParameter(this.RunModule, "DryingOvenGroup", this.dryingOvenGroup);
            this.openDoorDelay = ReadIntParameter(this.RunModule, "openDoorDelay", this.openDoorDelay);
            this.maxWorkTimeRange = ReadIntParameter(this.RunModule, "maxWorkTimeRange", this.maxWorkTimeRange);
            this.waitResultPressure = ReadBoolParameter(this.RunModule, "waitResultPressure", this.waitResultPressure);
            this.workDataTime = ReadIntParameter(this.RunModule, "workDataTime", this.workDataTime);
            this.equipmentCode = ReadStringParameter(this.RunModule, "equipmentCode", this.equipmentCode);
            this.resourceCode = ReadStringParameter(this.RunModule, "resourceCode", this.resourceCode);

            // 干燥炉设置参数
            this.cavityParameter.SetTempValue = (float)ReadDoubleParameter(this.RunModule, "SetTempValue", (double)this.cavityParameter.SetTempValue);
            this.cavityParameter.TempUpperlimit = (float)ReadDoubleParameter(this.RunModule, "TempUpperlimit", (double)this.cavityParameter.TempUpperlimit);
            this.cavityParameter.TempLowerlimit = (float)ReadDoubleParameter(this.RunModule, "TempLowerlimit", (double)this.cavityParameter.TempLowerlimit);
            this.cavityParameter.PreheatTime = (uint)ReadIntParameter(this.RunModule, "PreheatTime", (int)this.cavityParameter.PreheatTime);
            this.cavityParameter.VacHeatTime = (uint)ReadIntParameter(this.RunModule, "VacHeatTime", (int)this.cavityParameter.VacHeatTime);
            this.cavityParameter.OpenDoorBlowTime = (uint)ReadIntParameter(this.RunModule, "OpenDoorBlowTime", (int)this.cavityParameter.OpenDoorBlowTime);
            this.cavityParameter.OpenDoorVacPressure = (uint)ReadIntParameter(this.RunModule, "OpenDoorVacPressure", (int)this.cavityParameter.OpenDoorVacPressure);
            this.cavityParameter.AStateVacTime = (uint)ReadIntParameter(this.RunModule, "AStateVacTime", (int)this.cavityParameter.AStateVacTime);
            this.cavityParameter.AStateVacPressure = (uint)ReadIntParameter(this.RunModule, "AStateVacPressure", (int)this.cavityParameter.AStateVacPressure);
            this.cavityParameter.BStateVacTime = (uint)ReadIntParameter(this.RunModule, "BStateVacTime", (int)this.cavityParameter.BStateVacTime);
            this.cavityParameter.BStateVacPressure = (uint)ReadIntParameter(this.RunModule, "BStateVacPressure", (int)this.cavityParameter.BStateVacPressure);
            //this.cavityParameter.BStateBlowAirTime = (uint)ReadIntParameter(this.RunModule, "BStateBlowAirTime", (int)this.cavityParameter.BStateBlowAirTime);
            this.cavityParameter.BStateBlowAirPressure = (uint)ReadIntParameter(this.RunModule, "BStateBlowAirPressure", (int)this.cavityParameter.BStateBlowAirPressure);
            this.cavityParameter.BStateBlowAirKeepTime = (uint)ReadIntParameter(this.RunModule, "BStateBlowAirKeepTime", (int)this.cavityParameter.BStateBlowAirKeepTime);
            //this.cavityParameter.BreathTimeInterval = (uint)ReadIntParameter(this.RunModule, "BreathTimeInterval", (int)this.cavityParameter.BreathTimeInterval);
            this.cavityParameter.BreathCycleTimes = (uint)ReadIntParameter(this.RunModule, "BreathCycleTimes", (int)this.cavityParameter.BreathCycleTimes);
            this.cavityParameter.HeatPlate = (uint)ReadIntParameter(this.RunModule, "HeatPlate", (int)this.cavityParameter.HeatPlate);
            this.cavityParameter.MaxNGHeatPlate = (uint)ReadIntParameter(this.RunModule, "MaxNGHeatPlate", (int)this.cavityParameter.MaxNGHeatPlate);
            this.cavityParameter.HeatPreVacTime = (uint)ReadIntParameter(this.RunModule, "HeatPreVacTime", (int)this.cavityParameter.HeatPreVacTime);
            this.cavityParameter.HeatPreBlow = (uint)ReadIntParameter(this.RunModule, "HeatPreBlow", (int)this.cavityParameter.HeatPreBlow);
            this.cavityParameter.AStateVacMaxValue = (uint)ReadIntParameter(this.RunModule, "AStateVacMaxValue", (int)this.cavityParameter.AStateVacMaxValue);
            this.cavityParameter.BStateVacMaxValue = (uint)ReadIntParameter(this.RunModule, "BStateVacMaxValue", (int)this.cavityParameter.BStateVacMaxValue);
            this.cavityParameter.TempDifferAlarmValue = (uint)ReadDoubleParameter(this.RunModule, "TempDifferAlarmValue", (int)this.cavityParameter.TempDifferAlarmValue);
            this.cavityParameter.AStateSavePressureTime = (uint)ReadIntParameter(this.RunModule, "AStateSavePressureTime", (int)this.cavityParameter.AStateSavePressureTime);
            this.cavityParameter.BStateSavePressureTime = (uint)ReadIntParameter(this.RunModule, "BStateSavePressureTime", (int)this.cavityParameter.BStateSavePressureTime);

            // 干燥炉设置复烘参数
            this.cavityParameter.SetTempValueEx = (float)ReadDoubleParameter(this.RunModule, "SetTempValueEx", (double)this.cavityParameter.SetTempValueEx);
            this.cavityParameter.TempUpperlimitEx = (float)ReadDoubleParameter(this.RunModule, "TempUpperlimitEx", (double)this.cavityParameter.TempUpperlimitEx);
            this.cavityParameter.TempLowerlimitEx = (float)ReadDoubleParameter(this.RunModule, "TempLowerlimitEx", (double)this.cavityParameter.TempLowerlimitEx);
            this.cavityParameter.PreheatTimeEx = (uint)ReadIntParameter(this.RunModule, "PreheatTimeEx", (int)this.cavityParameter.PreheatTimeEx);
            this.cavityParameter.VacHeatTimeEx = (uint)ReadIntParameter(this.RunModule, "VacHeatTimeEx", (int)this.cavityParameter.VacHeatTimeEx);
            this.cavityParameter.OpenDoorBlowTimeEx = (uint)ReadIntParameter(this.RunModule, "OpenDoorBlowTimeEx", (int)this.cavityParameter.OpenDoorBlowTimeEx);
            this.cavityParameter.OpenDoorVacPressureEx = (uint)ReadIntParameter(this.RunModule, "OpenDoorVacPressureEx", (int)this.cavityParameter.OpenDoorVacPressureEx);
            this.cavityParameter.AStateVacTimeEx = (uint)ReadIntParameter(this.RunModule, "AStateVacTimeEx", (int)this.cavityParameter.AStateVacTimeEx);
            this.cavityParameter.AStateVacPressureEx = (uint)ReadIntParameter(this.RunModule, "AStateVacPressureEx", (int)this.cavityParameter.AStateVacPressureEx);
            this.cavityParameter.BStateVacTimeEx = (uint)ReadIntParameter(this.RunModule, "BStateVacTimeEx", (int)this.cavityParameter.BStateVacTimeEx);
            this.cavityParameter.BStateVacPressureEx = (uint)ReadIntParameter(this.RunModule, "BStateVacPressureEx", (int)this.cavityParameter.BStateVacPressureEx);
            this.cavityParameter.BStateBlowAirTimeEx = (uint)ReadIntParameter(this.RunModule, "BStateBlowAirTimeEx", (int)this.cavityParameter.BStateBlowAirTimeEx);
            this.cavityParameter.BStateBlowAirPressureEx = (uint)ReadIntParameter(this.RunModule, "BStateBlowAirPressureEx", (int)this.cavityParameter.BStateBlowAirPressureEx);
            this.cavityParameter.BStateBlowAirKeepTimeEx = (uint)ReadIntParameter(this.RunModule, "BStateBlowAirKeepTimeEx", (int)this.cavityParameter.BStateBlowAirKeepTimeEx);
            this.cavityParameter.BreathTimeIntervalEx = (uint)ReadIntParameter(this.RunModule, "BreathTimeIntervalEx", (int)this.cavityParameter.BreathTimeIntervalEx);
            this.cavityParameter.BreathCycleTimesEx = (uint)ReadIntParameter(this.RunModule, "BreathCycleTimesEx", (int)this.cavityParameter.BreathCycleTimesEx);
            this.cavityParameter.HeatPlateEx = (uint)ReadIntParameter(this.RunModule, "HeatPlateEx", (int)this.cavityParameter.HeatPlateEx);
            this.cavityParameter.MaxNGHeatPlateEx = (uint)ReadIntParameter(this.RunModule, "MaxNGHeatPlateEx", (int)this.cavityParameter.MaxNGHeatPlateEx);
            this.cavityParameter.HeatPreVacTimeEx = (uint)ReadIntParameter(this.RunModule, "HeatPreVacTimeEx", (int)this.cavityParameter.HeatPreVacTimeEx);
            this.cavityParameter.HeatPreBlowEx = (uint)ReadIntParameter(this.RunModule, "HeatPreBlowEx", (int)this.cavityParameter.HeatPreBlowEx);
            this.cavityParameter.VacMinValueEx = (uint)ReadIntParameter(this.RunModule, "VacMinValueEx", (int)this.cavityParameter.VacMinValueEx);
            this.cavityParameter.VacMaxValueEx = (uint)ReadIntParameter(this.RunModule, "VacMaxValueEx", (int)this.cavityParameter.VacMaxValueEx);
            this.cavityParameter.TempDifferAlarmValueEx = (uint)ReadDoubleParameter(this.RunModule, "TempDifferAlarmValueEx", (int)this.cavityParameter.TempDifferAlarmValueEx);

            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            // 调度机器人
            this.transferRobotRun = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
        }

        #endregion

        #region // 运行数据读写

        public override void InitRunData()
        {
            if (null == this.CavityState)
            {
                this.CavityState = new CavityStatus[(int)OvenRowCol.MaxRow];
                for (int i = 0; i < this.CavityState.Length; i++)
                {
                    this.CavityState[i] = new CavityStatus();
                }
            }
            if (null == this.CavityOldState)
            {
                this.CavityOldState = new CavityStatus[(int)OvenRowCol.MaxRow];
                for (int i = 0; i < this.CavityOldState.Length; i++)
                {
                    this.CavityOldState[i] = CavityStatus.Unknown;
                }
            }
            if (null == this.CavityHeartCycle)
            {
                this.CavityHeartCycle = new int[(int)OvenRowCol.MaxRow];
            }
            for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                this.CavityState[i] = CavityStatus.Normal;
                this.CavityHeartCycle[i] = 0;
            }
            this.operatePos.Release();
            if (null == this.waterContentValue)
            {
                this.waterContentValue = new double[(int)OvenRowCol.MaxRow, 3];
                //this.waterContentValue = new double[(int)OvenRowCol.MaxRow, 4]; //增加2组含水量
            }
            for (int rowIdx = 0; rowIdx < this.waterContentValue.GetLength(0); rowIdx++)
            {
                for (int i = 0; i < this.waterContentValue.GetLength(1); i++)
                {
                    this.waterContentValue[rowIdx, i] = 0.0;
                }
            }
            if (null == this.readOvenData)
            {
                this.readOvenData = new DryingOvenData();
            }
            this.readOvenData.Release();
            if (null == this.writeOvenData)
            {
                this.writeOvenData = new DryingOvenData();
            }
            this.writeOvenData.Release();
            if (null == this.bakingDataStartTime)
            {
                this.bakingDataStartTime = new DateTime[(int)OvenRowCol.MaxRow];
            }

            base.InitRunData();
        }

        public override void LoadRunData()
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            this.operatePos.operateEvent = (EventList)iniStream.ReadInt(section, "operatePos.operateEvent", (int)operatePos.operateEvent);
            this.operatePos.row = iniStream.ReadInt(section, "operatePos.row", operatePos.row);
            this.operatePos.col = iniStream.ReadInt(section, "operatePos.col", operatePos.col);
            for (int rowIdx = 0; rowIdx < this.waterContentValue.GetLength(0); rowIdx++)
            {
                for (int i = 0; i < this.waterContentValue.GetLength(1); i++)
                {
                    key = string.Format("waterContentValue[{0},{1}]", rowIdx, i);
                    this.waterContentValue[rowIdx, i] = iniStream.ReadDouble(section, key, waterContentValue[rowIdx, i]);
                }
            }
            for (int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
            {
                // 状态
                this.CavityState[rowIdx] = (CavityStatus)iniStream.ReadInt(section, ("CavityState" + rowIdx), (int)CavityState[rowIdx]);
                this.CavityHeartCycle[rowIdx] = iniStream.ReadInt(section, ("CavityHeartCycle" + rowIdx), CavityHeartCycle[rowIdx]);
                key = string.Format("WCavity({0}).doorState", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].doorState = (short)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).doorState);
                key = string.Format("WCavity({0}).workState", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].workState = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).workState);
                // 参数
                key = string.Format("WCavity({0}).cavityParameter.SetTempValue", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.SetTempValue = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.SetTempValue);
                key = string.Format("WCavity({0}).cavityParameter.TempUpperlimit", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.TempUpperlimit = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.TempUpperlimit);
                key = string.Format("WCavity({0}).cavityParameter.TempLowerlimit", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.TempLowerlimit = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.TempLowerlimit);
                key = string.Format("WCavity({0}).cavityParameter.PreheatTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.PreheatTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.PreheatTime);
                key = string.Format("WCavity({0}).cavityParameter.VacHeatTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.VacHeatTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.VacHeatTime);
                key = string.Format("WCavity({0}).cavityParameter.OpenDoorBlowTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.OpenDoorBlowTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorBlowTime);
                key = string.Format("WCavity({0}).cavityParameter.OpenDoorVacPressure", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.OpenDoorVacPressure = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorVacPressure);
                key = string.Format("WCavity({0}).cavityParameter.AStateVacTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.AStateVacTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacTime);
                key = string.Format("WCavity({0}).cavityParameter.AStateVacPressure", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.AStateVacPressure = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacPressure);
                key = string.Format("WCavity({0}).cavityParameter.BStateVacTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateVacTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacTime);
                key = string.Format("WCavity({0}).cavityParameter.BStateVacPressure", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateVacPressure = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacPressure);
                key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateBlowAirTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirTime);
                key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirPressure", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateBlowAirPressure = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirPressure);
                key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirKeepTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateBlowAirKeepTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirKeepTime);
                //key = string.Format("WCavity({0}).cavityParameter.BreathTimeInterval", rowIdx);
                //this.writeOvenData.CavityDatas[rowIdx].parameter.BreathTimeInterval = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BreathTimeInterval);
                key = string.Format("WCavity({0}).cavityParameter.BreathCycleTimes", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BreathCycleTimes = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BreathCycleTimes);
                key = string.Format("WCavity({0}).cavityParameter.HeatPlate", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.HeatPlate = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.HeatPlate);
                key = string.Format("WCavity({0}).cavityParameter.MaxNGHeatPlate", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.MaxNGHeatPlate = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.MaxNGHeatPlate);
                key = string.Format("WCavity({0}).cavityParameter.HeatPreVacTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.HeatPreVacTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreVacTime);
                key = string.Format("WCavity({0}).cavityParameter.HeatPreBlow", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.HeatPreBlow = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreBlow);
                key = string.Format("WCavity({0}).cavityParameter.AStateVacMaxValue", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.AStateVacMaxValue = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacMaxValue);
                key = string.Format("WCavity({0}).cavityParameter.BStateVacMaxValue", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateVacMaxValue = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacMaxValue);
                key = string.Format("WCavity({0}).cavityParameter.TempDifferAlarmValue", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.TempDifferAlarmValue = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.TempDifferAlarmValue);

                key = string.Format("WCavity({0}).cavityParameter.AStateSavePressureTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.AStateSavePressureTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.AStateSavePressureTime);
                key = string.Format("WCavity({0}).cavityParameter.BStateSavePressureTime", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateSavePressureTime = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateSavePressureTime);

                // 复烘参数
                key = string.Format("WCavity({0}).cavityParameter.SetTempValueEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.SetTempValueEx = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.SetTempValueEx);
                key = string.Format("WCavity({0}).cavityParameter.TempUpperlimitEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.TempUpperlimitEx = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.TempUpperlimitEx);
                key = string.Format("WCavity({0}).cavityParameter.TempLowerlimitEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.TempLowerlimitEx = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.TempLowerlimitEx);
                key = string.Format("WCavity({0}).cavityParameter.PreheatTimeEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.PreheatTimeEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.PreheatTimeEx);
                key = string.Format("WCavity({0}).cavityParameter.VacHeatTimeEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.VacHeatTimeEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.VacHeatTimeEx);
                key = string.Format("WCavity({0}).cavityParameter.OpenDoorBlowTimeEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.OpenDoorBlowTimeEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorBlowTimeEx);
                key = string.Format("WCavity({0}).cavityParameter.OpenDoorVacPressureEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.OpenDoorVacPressureEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorVacPressureEx);
                key = string.Format("WCavity({0}).cavityParameter.AStateVacTimeEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.AStateVacTimeEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacTimeEx);
                key = string.Format("WCavity({0}).cavityParameter.AStateVacPressureEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.AStateVacPressureEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacPressureEx);
                key = string.Format("WCavity({0}).cavityParameter.BStateVacTimeEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateVacTimeEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacTimeEx);
                key = string.Format("WCavity({0}).cavityParameter.BStateVacPressureEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateVacPressureEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacPressureEx);
                key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirTimeEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateBlowAirTimeEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirTimeEx);
                key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirPressureEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateBlowAirPressureEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirPressureEx);
                key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirKeepTimeEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BStateBlowAirKeepTimeEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirKeepTimeEx);
                key = string.Format("WCavity({0}).cavityParameter.BreathTimeIntervalEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BreathTimeIntervalEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BreathTimeIntervalEx);
                key = string.Format("WCavity({0}).cavityParameter.BreathCycleTimesEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.BreathCycleTimesEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.BreathCycleTimesEx);
                key = string.Format("WCavity({0}).cavityParameter.HeatPlateEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.HeatPlateEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.HeatPlateEx);
                key = string.Format("WCavity({0}).cavityParameter.MaxNGHeatPlateEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.MaxNGHeatPlateEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.MaxNGHeatPlateEx);
                key = string.Format("WCavity({0}).cavityParameter.HeatPreVacTimeEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.HeatPreVacTimeEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreVacTimeEx);
                key = string.Format("WCavity({0}).cavityParameter.HeatPreBlowEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.HeatPreBlowEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreBlowEx);
                key = string.Format("WCavity({0}).cavityParameter.VacMinValueEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.VacMinValueEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.VacMinValueEx);
                key = string.Format("WCavity({0}).cavityParameter.VacMaxValueEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.VacMaxValueEx = (uint)iniStream.ReadInt(section, key, (int)WCavity(rowIdx).parameter.VacMaxValueEx);
                key = string.Format("WCavity({0}).cavityParameter.TempDifferAlarmValueEx", rowIdx);
                this.writeOvenData.CavityDatas[rowIdx].parameter.TempDifferAlarmValueEx = (float)iniStream.ReadDouble(section, key, WCavity(rowIdx).parameter.TempDifferAlarmValueEx);
            }

            base.LoadRunData();
        }

        public override void SaveRunData(SaveType saveType, int index = -1)
        {
            string section, key, file;
            section = this.RunModule;
            file = string.Format("{0}{1}.cfg", Def.GetAbsPathName(Def.RunDataFolder), section);

            if (SaveType.Variables == (SaveType.Variables & saveType))
            {
                iniStream.WriteInt(section, "operatePos.operateEvent", (int)operatePos.operateEvent);
                iniStream.WriteInt(section, "operatePos.row", operatePos.row);
                iniStream.WriteInt(section, "operatePos.col", operatePos.col);
                for (int rowIdx = 0; rowIdx < this.waterContentValue.GetLength(0); rowIdx++)
                {
                    for (int i = 0; i < this.waterContentValue.GetLength(1); i++)
                    {
                        key = string.Format("waterContentValue[{0},{1}]", rowIdx, i);
                        iniStream.WriteDouble(section, key, waterContentValue[rowIdx, i]);
                    }
                }
            }
            if (SaveType.Cavity == (SaveType.Cavity & saveType))
            {
                // 仅保存有用信息
                for (int rowIdx = 0; rowIdx < (int)OvenRowCol.MaxRow; rowIdx++)
                {
                    // 状态
                    iniStream.WriteInt(section, ("CavityState" + rowIdx), (int)CavityState[rowIdx]);
                    iniStream.WriteInt(section, ("CavityHeartCycle" + rowIdx), CavityHeartCycle[rowIdx]);
                    key = string.Format("WCavity({0}).doorState", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).doorState);
                    key = string.Format("WCavity({0}).workState", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).workState);

                    // 参数
                    key = string.Format("WCavity({0}).cavityParameter.SetTempValue", rowIdx);
                    iniStream.WriteDouble(section, key, WCavity(rowIdx).parameter.SetTempValue);
                    key = string.Format("WCavity({0}).cavityParameter.TempUpperlimit", rowIdx);
                    iniStream.WriteDouble(section, key, WCavity(rowIdx).parameter.TempUpperlimit);
                    key = string.Format("WCavity({0}).cavityParameter.TempLowerlimit", rowIdx);
                    iniStream.WriteDouble(section, key, WCavity(rowIdx).parameter.TempLowerlimit);
                    key = string.Format("WCavity({0}).cavityParameter.PreheatTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.PreheatTime);
                    key = string.Format("WCavity({0}).cavityParameter.VacHeatTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.VacHeatTime);
                    key = string.Format("WCavity({0}).cavityParameter.OpenDoorBlowTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorBlowTime);
                    key = string.Format("WCavity({0}).cavityParameter.OpenDoorVacPressure", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorVacPressure);
                    key = string.Format("WCavity({0}).cavityParameter.AStateVacTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacTime);
                    key = string.Format("WCavity({0}).cavityParameter.AStateVacPressure", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacPressure);
                    key = string.Format("WCavity({0}).cavityParameter.BStateVacTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacTime);
                    key = string.Format("WCavity({0}).cavityParameter.BStateVacPressure", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacPressure);
                    key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirTime);
                    key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirPressure", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirPressure);
                    key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirKeepTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirKeepTime);
                    //key = string.Format("WCavity({0}).cavityParameter.BreathTimeInterval", rowIdx);
                    //iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BreathTimeInterval);
                    key = string.Format("WCavity({0}).cavityParameter.BreathCycleTimes", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BreathCycleTimes);
                    key = string.Format("WCavity({0}).cavityParameter.HeatPlate", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.HeatPlate);
                    key = string.Format("WCavity({0}).cavityParameter.MaxNGHeatPlate", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.MaxNGHeatPlate);
                    key = string.Format("WCavity({0}).cavityParameter.HeatPreVacTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreVacTime);
                    key = string.Format("WCavity({0}).cavityParameter.HeatPreBlow", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreBlow);
                    key = string.Format("WCavity({0}).cavityParameter.AStateVacMaxValue", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacMaxValue);
                    key = string.Format("WCavity({0}).cavityParameter.BStateVacMaxValue", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacMaxValue);
                    key = string.Format("WCavity({0}).cavityParameter.TempDifferAlarmValue", rowIdx);
                    iniStream.WriteDouble(section, key, (int)WCavity(rowIdx).parameter.TempDifferAlarmValue);
                    key = string.Format("WCavity({0}).cavityParameter.AStateSavePressureTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.AStateSavePressureTime);
                    key = string.Format("WCavity({0}).cavityParameter.BStateSavePressureTime", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateSavePressureTime);

                    // 复烘参数
                    key = string.Format("WCavity({0}).cavityParameter.SetTempValueEx", rowIdx);
                    iniStream.WriteDouble(section, key, WCavity(rowIdx).parameter.SetTempValueEx);
                    key = string.Format("WCavity({0}).cavityParameter.TempUpperlimitEx", rowIdx);
                    iniStream.WriteDouble(section, key, WCavity(rowIdx).parameter.TempUpperlimitEx);
                    key = string.Format("WCavity({0}).cavityParameter.TempLowerlimitEx", rowIdx);
                    iniStream.WriteDouble(section, key, WCavity(rowIdx).parameter.TempLowerlimitEx);
                    key = string.Format("WCavity({0}).cavityParameter.PreheatTimeEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.PreheatTimeEx);
                    key = string.Format("WCavity({0}).cavityParameter.VacHeatTimeEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.VacHeatTimeEx);
                    key = string.Format("WCavity({0}).cavityParameter.OpenDoorBlowTimeEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorBlowTimeEx);
                    key = string.Format("WCavity({0}).cavityParameter.OpenDoorVacPressureEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.OpenDoorVacPressureEx);
                    key = string.Format("WCavity({0}).cavityParameter.AStateVacTimeEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacTimeEx);
                    key = string.Format("WCavity({0}).cavityParameter.AStateVacPressureEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.AStateVacPressureEx);
                    key = string.Format("WCavity({0}).cavityParameter.BStateVacTimeEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacTimeEx);
                    key = string.Format("WCavity({0}).cavityParameter.BStateVacPressureEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateVacPressureEx);
                    key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirTimeEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirTimeEx);
                    key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirPressureEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirPressureEx);
                    key = string.Format("WCavity({0}).cavityParameter.BStateBlowAirKeepTimeEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BStateBlowAirKeepTimeEx);
                    key = string.Format("WCavity({0}).cavityParameter.BreathTimeIntervalEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BreathTimeIntervalEx);
                    key = string.Format("WCavity({0}).cavityParameter.BreathCycleTimesEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.BreathCycleTimesEx);
                    key = string.Format("WCavity({0}).cavityParameter.HeatPlateEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.HeatPlateEx);
                    key = string.Format("WCavity({0}).cavityParameter.MaxNGHeatPlateEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.MaxNGHeatPlateEx);
                    key = string.Format("WCavity({0}).cavityParameter.HeatPreVacTimeEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreVacTimeEx);
                    key = string.Format("WCavity({0}).cavityParameter.HeatPreBlowEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.HeatPreBlowEx);
                    key = string.Format("WCavity({0}).cavityParameter.VacMinValueEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.VacMinValueEx);
                    key = string.Format("WCavity({0}).cavityParameter.VacMaxValueEx", rowIdx);
                    iniStream.WriteInt(section, key, (int)WCavity(rowIdx).parameter.VacMaxValueEx);
                    key = string.Format("WCavity({0}).cavityParameter.TempDifferAlarmValueEx", rowIdx);
                    iniStream.WriteDouble(section, key, (int)WCavity(rowIdx).parameter.TempDifferAlarmValueEx);
                }
            }

            base.SaveRunData(saveType, index);
        }
        #endregion

        #region // 干燥炉操作

        /// <summary>
        /// 获取干燥炉连接状态
        /// </summary>
        /// <returns></returns>
        public bool DryOvenIsConnect()
        {
            return this.ovenClient.IsConnect();
        }

        /// <summary>
        /// 干燥炉连接
        /// </summary>
        /// <param name="connect"></param>
        /// <returns></returns>
        public bool DryOvenConnect(bool connect)
        {
            if (Def.IsNoHardware())
                return true;

            if (connect)
            {
                if (!DryOvenIsConnect())
                {
                    this.ovenClient.SetFinsType(FinsType.Udp);
                    byte nodeID = Convert.ToByte(this.localIP.Substring(this.localIP.LastIndexOf('.') + 1));
                    return this.ovenClient.Connect(ovenIP, ovenPort, nodeID);
                }
            }
            else
            {
                this.ovenClient.Disconnect();
                this.readOvenData.Release();
            }
            return DryOvenIsConnect();
        }

        /// <summary>
        /// 获取干燥炉IP信息
        /// </summary>
        /// <returns></returns>
        public string GetDryOvenIPInfo()
        {
            return string.Format("{0}:{1}", this.ovenIP, this.ovenPort);
        }

        /// <summary>
        /// 干燥炉开门/关门
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenOpenDoor(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if ((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                msg = "干燥炉非远程运行状态，无法远程操作开门、关门";
                dispose = "需要远程操作干燥炉，请先切换至远程运行";
                ShowMessageBox((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-001";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment,alarmCode,msg,MesAlarmStatus.Happen,MesAlarmLevel.One,ref errmsg);
                return false;
            }
            if (!CheckRobotTransferSafe(-1))
            {
                msg = string.Format("调度机器人在{0}层取放进，不能操作炉门", (cavityIdx + 1));
                dispose = string.Format("请将调度机器人操作到安全位后再操作炉门");
                ShowMessageBox((int)MsgID.RobotFingerIn, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-002";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                return false;
            }
            if ((short)OvenStatus.DoorOpen == cavityData.doorState)
            {
                if ((uint)OvenStatus.WorkStart == RCavity(cavityIdx).workState)
                {
                    msg = string.Format("{0}层腔体干燥中，不能打开炉门", (cavityIdx + 1));
                    dispose = string.Format("请等待烘烤结束后再打开炉门");
                    ShowMessageBox((int)MsgID.WorkingOpenDoor, msg, dispose, MessageType.MsgAlarm);
                    string alarmCode = MesResources.Equipment.ResourceCode + "-003";
                    string errmsg = string.Empty;
                    //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                    return false;
                }
                if (RCavity(cavityIdx).vacPressure < (this.cavityParameter.OpenDoorVacPressure - 500))
                {
                    msg = string.Format("{0}层腔体当前真空压力为[{1}] < 设置的开门真空压力[{2}]，不能打开炉门\r\n{3}"
                        , (cavityIdx + 1), RCavity(cavityIdx).vacPressure, this.cavityParameter.OpenDoorVacPressure
                        , (RCavity(cavityIdx).parameter.OpenDoorVacPressure < 90000) ? "建议设置开门气压为94000以上" : "");
                    dispose = string.Format("请先破真空后再打开炉门");
                    ShowMessageBox((int)MsgID.OpenDoorPressureAlm, msg, dispose, MessageType.MsgAlarm);
                    string alarmCode = MesResources.Equipment.ResourceCode + "-004";
                    string errmsg = string.Empty;
                    //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                    return false;
                }
                for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                {
                    if ((i != cavityIdx) && ((short)OvenStatus.DoorClose != RCavity(i).doorState))
                    {
                        msg = string.Format("{0}层炉门非关闭，不能同时打开两层炉门", (i + 1));
                        dispose = string.Format("请先关闭{0}层炉门后再打开{1}层炉门", (i + 1), (cavityIdx + 1));
                        ShowMessageBox((int)MsgID.OpenMultiDoorAlm, msg, dispose, MessageType.MsgAlarm);
                        string alarmCode = MesResources.Equipment.ResourceCode + "-005";
                        string errmsg = string.Empty;
                        //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                        //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        return false;
                    }
                }
            }
            WriteLog($"{cavityIdx + 1}层炉门{((uint)OvenStatus.DoorOpen == cavityData.doorState ? "打开" : "关闭")}", mode);
            if (this.ovenClient.SetDryOvenData(DryOvenCmd.DoorOpenClose, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if (GetDryOvenData(ref readOvenData))
                    {
                        if (this.readOvenData.CavityDatas[cavityIdx].doorState == cavityData.doorState)
                        {
                            return true;
                        }
                    }
                    if ((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay)
                    {
                        if (alarm)
                        {
                            msg = string.Format("{0}层{1}炉门超时[{2}秒]", (cavityIdx + 1)
                                , ((uint)OvenStatus.DoorOpen == cavityData.doorState ? "打开" : "关闭"), this.openDoorDelay);
                            dispose = "请检查干燥炉是否远程运行";
                            ShowMessageBox((int)MsgID.DoorOpenClose, msg, dispose, MessageType.MsgAlarm);
                            string alarmCode = MesResources.Equipment.ResourceCode + "-006";
                            string errmsg = string.Empty;
                            //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if (alarm)
            {
                msg = string.Format("发送{0}层{1}炉门指令失败"
                    , (cavityIdx + 1), ((uint)OvenStatus.DoorOpen == cavityData.doorState ? "打开" : "关闭"));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.DoorOpenClose, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-007";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
            }
            return false;
        }

        /// <summary>
        /// 打开/关闭干燥炉真空阀
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenVacuum(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if ((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                if (alarm)
                {
                    msg = "干燥炉非远程运行状态，无法远程操作真空阀";
                    dispose = "需要远程操作干燥炉，请先切换至远程运行";
                    ShowMessageBox((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                    string alarmCode = MesResources.Equipment.ResourceCode + "-008";
                    string errmsg = string.Empty;
                    //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                }
                return false;
            }
            if ((uint)OvenStatus.WorkStart == RCavity(cavityIdx).workState)
            {
                if (alarm)
                {
                    msg = string.Format("{0}层腔体干燥中，不能操作真空阀", (cavityIdx + 1));
                    dispose = string.Format("请等待烘烤结束后再操作真空阀");
                    ShowMessageBox((int)MsgID.WorkingVacuum, msg, dispose, MessageType.MsgAlarm);
                    string alarmCode = MesResources.Equipment.ResourceCode + "-009";
                    string errmsg = string.Empty;
                    //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                }
                return false;
            }
            if (((uint)OvenStatus.VacOpen == cavityData.vacValveState)
                && ((uint)OvenStatus.BlowOpen == RCavity(cavityIdx).blowValveState))
            {
                if (alarm)
                {
                    msg = string.Format("{0}层腔体破真空阀已打开，不能打开真空阀", (cavityIdx + 1));
                    dispose = string.Format("请先关闭破真空阀后再打开真空阀");
                    ShowMessageBox((int)MsgID.VacOpenClose, msg, dispose, MessageType.MsgAlarm);
                    string alarmCode = MesResources.Equipment.ResourceCode + "-010";
                    string errmsg = string.Empty;
                    //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                }
                return false;
            }
            WriteLog($"{cavityIdx + 1}层真空阀{((uint)OvenStatus.VacOpen == cavityData.vacValveState ? "打开" : "关闭")}", mode);
            if (this.ovenClient.SetDryOvenData(DryOvenCmd.VacOpenClose, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if (GetDryOvenData(ref readOvenData))
                    {
                        if (this.readOvenData.CavityDatas[cavityIdx].vacValveState == cavityData.vacValveState)
                        {
                            return true;
                        }
                    }
                    if ((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay / 2)
                    {
                        if (alarm)
                        {
                            msg = string.Format("{0}层{1}真空阀超时[{2}秒]", (cavityIdx + 1)
                                , ((uint)OvenStatus.VacOpen == cavityData.vacValveState ? "打开" : "关闭"), 10);
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageBox((int)MsgID.VacOpenClose, msg, dispose, MessageType.MsgAlarm);
                            string alarmCode = MesResources.Equipment.ResourceCode + "-011";
                            string errmsg = string.Empty;
                            //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if (alarm)
            {
                msg = string.Format("发送{0}层{1}真空阀指令失败", (cavityIdx + 1)
                    , ((uint)OvenStatus.VacOpen == cavityData.vacValveState ? "打开" : "关闭"));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.VacOpenClose, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-012";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
            }
            return false;
        }

        /// <summary>
        /// 打开/关闭干燥炉破真空阀
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenBlowAir(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if ((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                if (alarm)
                {
                    msg = "干燥炉非远程运行状态，无法远程操作破真空阀";
                    dispose = "需要远程操作干燥炉，请先切换至远程运行";
                    ShowMessageBox((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                    string alarmCode = MesResources.Equipment.ResourceCode + "-013";
                    string errmsg = string.Empty;
                    //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                }
                return false;
            }
            if ((uint)OvenStatus.WorkStart == RCavity(cavityIdx).workState)
            {
                if (alarm)
                {
                    msg = string.Format("{0}层腔体干燥中，不能操作破真空阀", (cavityIdx + 1));
                    dispose = string.Format("请等待烘烤结束后再操作破真空阀");
                    ShowMessageBox((int)MsgID.WorkingBlowAir, msg, dispose, MessageType.MsgAlarm);
                    string alarmCode = MesResources.Equipment.ResourceCode + "-014";
                    string errmsg = string.Empty;
                    //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                }
                return false;
            }
            if (((uint)OvenStatus.VacOpen == RCavity(cavityIdx).vacValveState)
                && ((uint)OvenStatus.BlowOpen == cavityData.blowValveState))
            {
                if (alarm)
                {
                    msg = string.Format("{0}层腔体真空阀已打开，不能打开破真空阀", (cavityIdx + 1));
                    dispose = string.Format("请先关闭真空阀后再打开破真空阀");
                    ShowMessageBox((int)MsgID.VacOpenClose, msg, dispose, MessageType.MsgAlarm);
                    string alarmCode = MesResources.Equipment.ResourceCode + "-015";
                    string errmsg = string.Empty;
                    //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                }
                return false;
            }
            WriteLog($"{cavityIdx + 1}层破真空阀{((uint)OvenStatus.BlowOpen == cavityData.blowValveState ? "打开" : "关闭")}", mode);
            if (this.ovenClient.SetDryOvenData(DryOvenCmd.BlowOpenClose, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if (GetDryOvenData(ref readOvenData))
                    {
                        if (this.readOvenData.CavityDatas[cavityIdx].blowValveState == cavityData.blowValveState)
                        {
                            return true;
                        }
                    }
                    if ((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay / 2)
                    {
                        if (alarm)
                        {
                            msg = string.Format("{0}层{1}破真空阀超时[{2}秒]", (cavityIdx + 1)
                                , ((uint)OvenStatus.BlowOpen == cavityData.blowValveState ? "打开" : "关闭"), 10);
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageBox((int)MsgID.BlowOpenClose, msg, dispose, MessageType.MsgAlarm);
                            string alarmCode = MesResources.Equipment.ResourceCode + "-016";
                            string errmsg = string.Empty;
                            //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if (alarm)
            {
                msg = string.Format("发送{0}层{1}破真空阀指令失败", (cavityIdx + 1)
                    , ((uint)OvenStatus.BlowOpen == cavityData.blowValveState ? "打开" : "关闭"));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.BlowOpenClose, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-017";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
            }
            return false;
        }

        /// <summary>
        /// 打开/关闭干燥炉保压
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenPressure(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if ((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                if (alarm)
                {
                    msg = "干燥炉非远程运行状态，无法远程操作设置保压";
                    dispose = "需要远程操作干燥炉，请先切换至远程运行";
                    ShowMessageID((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                    string alarmCode = MesResources.Equipment.ResourceCode + "-018";
                    string errmsg = string.Empty;
                    //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                }
                return false;
            }
            WriteLog($"{cavityIdx + 1}层保压{((uint)OvenStatus.PressureOpen == cavityData.pressureState ? "打开" : "关闭")}", mode);
            if (this.ovenClient.SetDryOvenData(DryOvenCmd.PressureOpenClose, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if (GetDryOvenData(ref readOvenData))
                    {
                        if (this.readOvenData.CavityDatas[cavityIdx].pressureState == cavityData.pressureState)
                        {
                            return true;
                        }
                    }
                    if ((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay / 10)
                    {
                        if (alarm)
                        {
                            msg = string.Format("{0}层{1}保压超时[{2}秒]", (cavityIdx + 1)
                                , ((uint)OvenStatus.PressureOpen == cavityData.pressureState ? "打开" : "关闭"), 10);
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageID((int)MsgID.PressureOpenClose, msg, dispose, MessageType.MsgAlarm);
                            string alarmCode = MesResources.Equipment.ResourceCode + "-019";
                            string errmsg = string.Empty;
                            //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if (alarm)
            {
                msg = string.Format("发送{0}层{1}保压指令失败", (cavityIdx + 1)
                    , ((uint)OvenStatus.PressureOpen == cavityData.pressureState ? "打开" : "关闭"));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageID((int)MsgID.PressureOpenClose, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-020";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
            }
            return false;
        }

        /// <summary>
        /// 启动/停止干燥炉加热
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenWorkStart(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if ((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                msg = "干燥炉非远程运行状态，无法远程操作加热启动/停止";
                dispose = "需要远程操作干燥炉，请先切换至远程运行";
                ShowMessageBox((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-021";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                return false;
            }
            if (((uint)OvenStatus.WorkStart == cavityData.workState)
                && RCavity(cavityIdx).vacPressure < this.cavityParameter.OpenDoorVacPressure)
            {
                msg = $"干燥炉当前真空压力为{RCavity(cavityIdx).vacPressure} < 开门气压{this.cavityParameter.OpenDoorVacPressure}，可能导致无法操作加热启动";
                dispose = "需要操作干燥炉加热启动，请先破真空操作";
                ShowMessageBox((int)MsgID.WorkStartStop, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-022";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);

                return false;
            }
            WriteLog($"{cavityIdx + 1}层加热{((uint)OvenStatus.WorkStart == cavityData.workState ? "启动" : "停止")}", mode);
            if (this.ovenClient.SetDryOvenData(DryOvenCmd.WorkStartStop, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if (GetDryOvenData(ref readOvenData))
                    {
                        if (this.readOvenData.CavityDatas[cavityIdx].workState == cavityData.workState)
                        {
                            return true;
                        }
                    }
                    if ((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay / 10)
                    {
                        if (alarm)
                        {
                            msg = string.Format("{0}层{1}加热超时[{2}秒]", (cavityIdx + 1)
                                , ((uint)OvenStatus.WorkStart == cavityData.workState ? "启动" : "停止"), 10);
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageBox((int)MsgID.WorkStartStop, msg, dispose, MessageType.MsgAlarm);
                            string alarmCode = MesResources.Equipment.ResourceCode + "-023";
                            string errmsg = string.Empty;
                            //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if (alarm)
            {
                msg = string.Format("发送{0}层{1}加热指令失败", (cavityIdx + 1)
                    , ((uint)OvenStatus.WorkStart == cavityData.workState ? "启动" : "停止"));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.WorkStartStop, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-024";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
            }
            return false;
        }

        /// <summary>
        /// 设置干燥炉参数
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenSetParameter(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            string msg, dispose;
            if ((short)OvenStatus.RemoteOpen != this.readOvenData.RemoteState)
            {
                msg = "干燥炉非远程运行状态，无法远程设置参数";
                dispose = "需要远程操作干燥炉，请先切换至远程运行";
                ShowMessageBox((int)MsgID.RemoteErr, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-025";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                return false;
            }
            if ((uint)OvenStatus.WorkStart == RCavity(cavityIdx).workState)
            {
                msg = string.Format("{0}层腔体干燥中，不能设置参数", (cavityIdx + 1));
                dispose = string.Format("请等待烘烤结束后再设置参数");
                ShowMessageBox((int)MsgID.WorkingSetParameter, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-026";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                return false;
            }
            // 获取参数数据
            cavityData.parameter.Copy(this.cavityParameter);
            cavityData.parameter.OvenChk = this.CavityOvenEx[cavityIdx];
            msg = $"{cavityIdx + 1}层炉腔发送参数";
            msg += " 1)SetTempValue:" + cavityData.parameter.SetTempValue;
            msg += " 2)TempUpperlimit:" + cavityData.parameter.TempUpperlimit;
            msg += " 3)TempLowerlimit:" + cavityData.parameter.TempLowerlimit;
            msg += " 4)PreheatTime:" + cavityData.parameter.PreheatTime;
            msg += " 5)VacHeatTime:" + cavityData.parameter.VacHeatTime;
            msg += " 6)OpenDoorBlowTime:" + cavityData.parameter.OpenDoorBlowTime;
            msg += " 7)OpenDoorVacPressure:" + cavityData.parameter.OpenDoorVacPressure;
            msg += " 8)AStateVacTime:" + cavityData.parameter.AStateVacTime;
            msg += " 9)AStateVacPressure:" + cavityData.parameter.AStateVacPressure;
            msg += " 10)BStateVacTime:" + cavityData.parameter.BStateVacTime;
            msg += " 11)BStateVacPressure:" + cavityData.parameter.BStateVacPressure;
            msg += " 12)BStateBlowAirTime:" + cavityData.parameter.BStateBlowAirTime;
            msg += " 13)BStateBlowAirPressure:" + cavityData.parameter.BStateBlowAirPressure;
            msg += " 14)BStateBlowAirKeepTime:" + cavityData.parameter.BStateBlowAirKeepTime;
            //msg += " 15)BreathTimeInterval:" + cavityData.parameter.BreathTimeInterval;
            msg += " 16)BreathCycleTimes:" + cavityData.parameter.BreathCycleTimes;
            msg += " 17)HeatPlate:" + cavityData.parameter.HeatPlate;
            msg += " 18)MaxNGHeatPlate:" + cavityData.parameter.MaxNGHeatPlate;
            msg += " 19)HeatPreVacTime:" + cavityData.parameter.HeatPreVacTime;
            msg += " 20)HeatPreBlow:" + cavityData.parameter.HeatPreBlow;
            msg += " 21)AStateVacMaxValue:" + cavityData.parameter.AStateVacMaxValue;
            msg += " 22)BStateVacMaxValue:" + cavityData.parameter.BStateVacMaxValue;
            msg += " 23)TempDifferAlarmValue:" + cavityData.parameter.TempDifferAlarmValue;
            msg += " 24)AStateSavePressureTime:" + cavityData.parameter.AStateSavePressureTime;
            msg += " 25)BStateSavePressureTime:" + cavityData.parameter.BStateSavePressureTime;
            WriteLog(msg, mode);

            if (this.ovenClient.SetDryOvenData(DryOvenCmd.SetParameter, cavityIdx, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if (GetDryOvenData(ref readOvenData))
                    {
                        CavityParameter rParm = RCavity(cavityIdx).parameter;
                        CavityParameter wParm = cavityData.parameter;

                        if (cavityData.parameter.OvenChk)
                        {
                            if ((rParm.SetTempValue == wParm.SetTempValueEx)
                            && (rParm.TempUpperlimit == wParm.TempUpperlimitEx)
                            && (rParm.TempLowerlimit == wParm.TempLowerlimitEx)
                            && (rParm.PreheatTime == wParm.PreheatTimeEx)
                            && (rParm.VacHeatTime == wParm.VacHeatTimeEx)
                            && (rParm.BStateVacTime == wParm.BStateVacTimeEx)
                            && (rParm.BStateVacPressure == wParm.BStateVacPressureEx)
                            && (rParm.OpenDoorBlowTime == wParm.OpenDoorBlowTimeEx)
                            && (rParm.OpenDoorVacPressure == wParm.OpenDoorVacPressureEx)
                            && (rParm.AStateVacTime == wParm.AStateVacTimeEx)
                            && (rParm.AStateVacPressure == wParm.AStateVacPressureEx)
                            //&& (rParm.BStateBlowAirTime == wParm.BStateBlowAirTimeEx)
                            && (rParm.BStateBlowAirPressure == wParm.BStateBlowAirPressureEx)
                            && (rParm.BStateBlowAirKeepTime == wParm.BStateBlowAirKeepTimeEx)
                            //&& (rParm.BreathTimeInterval == wParm.BreathTimeIntervalEx)
                            && (rParm.BreathCycleTimes == wParm.BreathCycleTimesEx)
                            && (rParm.HeatPlate == wParm.HeatPlateEx)
                            && (rParm.MaxNGHeatPlate == wParm.MaxNGHeatPlateEx)
                            && (rParm.HeatPreVacTime == wParm.HeatPreVacTimeEx)
                            && (rParm.HeatPreBlow == wParm.HeatPreBlowEx)
                            && (rParm.AStateVacMaxValue == wParm.VacMaxValueEx)
                            && (rParm.TempDifferAlarmValue == wParm.TempDifferAlarmValueEx))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if ((rParm.SetTempValue == wParm.SetTempValue)
                            && (rParm.TempUpperlimit == wParm.TempUpperlimit)
                            && (rParm.TempLowerlimit == wParm.TempLowerlimit)
                            && (rParm.PreheatTime == wParm.PreheatTime)
                            && (rParm.VacHeatTime == wParm.VacHeatTime)
                            && (rParm.BStateVacTime == wParm.BStateVacTime)
                            && (rParm.BStateVacPressure == wParm.BStateVacPressure)
                            && (rParm.OpenDoorBlowTime == wParm.OpenDoorBlowTime)
                            && (rParm.OpenDoorVacPressure == wParm.OpenDoorVacPressure)
                            && (rParm.AStateVacTime == wParm.AStateVacTime)
                            && (rParm.AStateVacPressure == wParm.AStateVacPressure)
                            && (rParm.BStateBlowAirTime == wParm.BStateBlowAirTime)
                            && (rParm.BStateBlowAirPressure == wParm.BStateBlowAirPressure)
                            && (rParm.BStateBlowAirKeepTime == wParm.BStateBlowAirKeepTime)
                            //&& (rParm.BreathTimeInterval == wParm.BreathTimeInterval)
                            && (rParm.BreathCycleTimes == wParm.BreathCycleTimes)
                            && (rParm.HeatPlate == wParm.HeatPlate)
                            && (rParm.MaxNGHeatPlate == wParm.MaxNGHeatPlate)
                            && (rParm.HeatPreVacTime == wParm.HeatPreVacTime)
                            && (rParm.HeatPreBlow == wParm.HeatPreBlow)
                            && (rParm.AStateVacMaxValue == wParm.AStateVacMaxValue)
                            && (rParm.BStateVacMaxValue == wParm.BStateVacMaxValue)
                            && (rParm.TempDifferAlarmValue == wParm.TempDifferAlarmValue)
                            && (rParm.AStateSavePressureTime == wParm.AStateSavePressureTime)
                            && (rParm.BStateSavePressureTime == wParm.BStateSavePressureTime))
                            
                            {
                                return true;
                            }
                        }
             
                        
                    }
                    if ((DateTime.Now - startTime).TotalSeconds > this.openDoorDelay / 10)
                    {
                        if (alarm)
                        {
                            msg = string.Format("{0}层发送参数设置超时[{1}秒]", (cavityIdx + 1), 10);
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageBox((int)MsgID.SetParameter, msg, dispose, MessageType.MsgAlarm);
                            string alarmCode = MesResources.Equipment.ResourceCode + "-027";
                            string errmsg = string.Empty;
                            //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if (alarm)
            {
                msg = string.Format("{0}层发送参数设置指令失败", (cavityIdx + 1));
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.SetParameter, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-028";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
            }
            return false;
        }

        /// <summary>
        /// 解除干燥炉维修状态
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenFaultReset(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto, bool UnBindPallet = false)
        {
            string msg = "";
            string dispose = "";
            WriteLog($"{cavityIdx + 1}层解除维修状态", mode);
            if (CavityStatus.Maintenance == this.CavityState[cavityIdx])
            {
                //判断夹具有没有NG，NG夹具解绑
                int ptlId = 0;
                for (int idx = 0; idx < (int)OvenRowCol.MaxCol; idx++)
                {
                    ptlId = cavityIdx * (int)OvenRowCol.MaxCol + idx;
                    if (PalletStatus.NG == this.Pallet[ptlId].State)
                    {
                        // 设置电池状态
                        for (int row = 0; row < this.Pallet[ptlId].MaxRow; row++)
                        {
                            for (int col = 0; col < this.Pallet[ptlId].MaxCol; col++)
                            {
                                if (BatteryStatus.NG == this.Pallet[ptlId].Battery[row, col].Type)
                                {
                                    this.Pallet[ptlId].Battery[row, col].Type = BatteryStatus.OK;
                                }
                            }
                        }

                        if (UnBindPallet)
                        {
                            if (!Def.IsNoHardware() && !MesOperate.EquToMesUnBindContainer(MesResources.Equipment, this.Pallet[ptlId], ref msg))
                            {
                                msg = string.Format("解绑夹具【{0}】电池失败", this.Pallet[idx].Code);
                                dispose = string.Format("请联系MES了解相关操作：{0}", msg);
                                ShowMessageBox((int)MsgID.FaultReset, msg, dispose, MessageType.MsgAlarm);
                                return false;
                            }
                        }
                        else
                        {
                            this.Pallet[ptlId].State = PalletStatus.OK;
                        }
                    }
                }

                SetCavityState(cavityIdx, CavityStatus.Normal);
                return true;
            }
            return false;

            if (this.ovenClient.SetDryOvenData(DryOvenCmd.FaultReset, cavityIdx, cavityData))
            {
                return true;
            }
            else if (alarm)
            {
                msg = string.Format("发送故障复位指令失败");
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.FaultReset, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-029";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, string.Format("发送故障复位指令失败"), MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
            }
            return false;
        }

        /// <summary>
        /// 复位干燥炉报警状态
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        /// <param name="alarm"></param>
        /// <returns></returns>
        public bool DryOvenAlarmReset(int cavityIdx, CavityData cavityData, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            WriteLog($"{cavityIdx + 1}层复位报警状态", mode);
            string msg, dispose;
            if (this.ovenClient.SetDryOvenData(DryOvenCmd.AlarmReset, cavityIdx, cavityData))
            {
                return true;
            }
            else if (alarm)
            {
                msg = string.Format("发送报警复位指令失败");
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageBox((int)MsgID.FaultReset, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-029";
                string errmsg = string.Empty;
                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, string.Format("发送故障复位指令失败"), MesAlarmLevel.L, ref errmsg);
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
            }
            return false;
        }


        /// <summary>
        /// 发送门禁状态至干燥炉
        /// </summary>
        /// <param name="mcDoorOpen"></param>
        /// <param name="alarm"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public bool DryOvenSetMcDoorState(bool mcDoorOpen, bool alarm = true, OptMode mode = OptMode.Auto)
        {
            CavityData cavityData = new CavityData();
            cavityData.mcDoorState = (short)(mcDoorOpen ? OvenStatus.McDoorOpen : OvenStatus.McDoorClose);
            string msg, dispose;
            //WriteLog($"发送门禁状态至干燥炉 {(mcDoorOpen ? "门开" : "门关")}", mode);
            if (this.ovenClient.SetDryOvenData(DryOvenCmd.SetMcDoor, 0, cavityData))
            {
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if (GetDryOvenData(ref readOvenData))
                    {
                        if (cavityData.mcDoorState == this.readOvenData.MCDoorState)
                        {
                            return true;
                        }
                    }
                    if ((DateTime.Now - startTime).TotalSeconds > 3)
                    {
                        if (alarm)
                        {
                            msg = $"发送门禁状态至干燥炉 {(mcDoorOpen ? "门开" : "门关")}超时";
                            dispose = string.Format("请检查干燥炉是否远程运行");
                            ShowMessageID((int)MsgID.PressureOpenClose, msg, dispose, MessageType.MsgAlarm);
                            string alarmCode = MesResources.Equipment.ResourceCode + "-030";
                            string errmsg = string.Empty;
                            //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                        break;
                    }
                    Sleep(1);
                }
            }
            else if (alarm)
            {
                msg = string.Format("发送调度门禁状态指令失败");
                dispose = string.Format("请检查干燥炉是否连接");
                ShowMessageID((int)MsgID.SetMcDoor, msg, dispose, MessageType.MsgAlarm);
                string alarmCode = MesResources.Equipment.ResourceCode + "-031";
                string errmsg = string.Empty;
                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉数据
        /// </summary>
        /// <param name="ovenData"></param>
        /// <returns></returns>
        private bool GetDryOvenData(ref DryingOvenData ovenData)
        {
            return this.ovenClient.GetDryOvenData(ref ovenData);
        }

        #endregion

        #region // 后台线程

        /// <summary>
        /// 初始化线程(开始运行)
        /// </summary>
        private bool InitThread()
        {
            try
            {
                this.runWhileTask = new Task(RunWhileThread, TaskCreationOptions.LongRunning);
                this.runWhileTask.Start();
                Def.WriteLog("RunProcessDryingOven ", $"InitThread():RunWhileThread = {runWhileTask.Id} start", LogType.Success);
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
                this.runWhileTask.Wait();
                Def.WriteLog("RunProcessDryingOven", $"ReleaseThread():RunWhileThread = {runWhileTask.Id} end", LogType.Success);
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
            if (!Def.IsNoHardware() && !DryOvenIsConnect())
            {
                Sleep(200);
                return;
            }

            if (!GetDryOvenData(ref this.readOvenData))
            {
                return;
            }
            if (this.DryRun)
            {
                RandomFaultState(ref this.readOvenData);
            }

            #region // 通讯已连接，但数据交互错误

            if (this.readOvenData.DataError)
            {
                ShowMessageID((int)MsgID.OvenDataError, "通讯已连接，但不能获取干燥炉数据", "请检查干燥炉是否报警或故障，处理完毕后断开重新连接", MessageType.MsgAlarm);
                return;
            }

            #endregion

            #region // 遍历炉腔状态
            for (int cavityIdx = 0; cavityIdx < (int)OvenRowCol.MaxRow; cavityIdx++)
            {
                CavityData cavity = RCavity(cavityIdx);

                #region // 检查异常报警

                // 炉门异常报警
                if (cavity.doorAlarm)
                {
                    string msg = string.Format("{0}层炉门异常报警", cavityIdx + 1);
                    ShowMessageID((int)MsgID.DoorAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgAlarm);
                    //string alarmCode = MesResources.Equipment.ResourceCode + "-032";
                    //string errmsg = string.Empty;
                    ////炉门报警
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                    Sleep(5000);
                }
                // 真空报警
                if (cavity.vacAlarm)
                {
                    string msg = string.Format("{0}层真空异常报警[报警值：{1}]"
                        , cavityIdx + 1, cavity.vacAlarmValue);
                    ShowMessageID((int)MsgID.VacAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgAlarm);
                    //string alarmCode = MesResources.Equipment.ResourceCode + "-033";
                    //string errmsg = string.Empty;
                    ////真空报警
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                    Sleep(5000);
                }
                // 破真空报警
                if (cavity.blowAlarm)
                {
                    string msg = string.Format("{0}层破真空异常报警", cavityIdx + 1);
                    ShowMessageID((int)MsgID.BlowAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgAlarm);
                    //string alarmCode = MesResources.Equipment.ResourceCode + "-034";
                    //string errmsg = string.Empty;
                    ////MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    ////破真空报警
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                    Sleep(5000);
                }
                // 真空计报警
                if (cavity.vacuometerAlarm)
                {
                    string msg = string.Format("{0}层真空计异常报警", cavityIdx + 1);
                    ShowMessageID((int)MsgID.VacuometerAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgAlarm);
                    //string alarmCode = MesResources.Equipment.ResourceCode + "-035";
                    //string errmsg = string.Empty;
                    ////MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                    ////真空计报警
                    //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                    Sleep(5000);
                }
                for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                {
                    int almID = 0;
                    // 机械温控报警
                    if (cavity.controlAlarm[i])
                    {
                        almID = (int)MsgID.ControlAlarm + cavityIdx * (int)OvenRowCol.MaxCol + i;
                        string msg = $"{cavityIdx + 1}层夹具{i + 1}机械温控报警";
                        ShowMessageID(almID, msg, "请检查干燥炉", MessageType.MsgAlarm);
                        //string alarmCode = MesResources.Equipment.ResourceCode + "-036";
                        //string errmsg = string.Empty;
                        ////MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                        ////机械温控报警
                        //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        Sleep(5000);
                    }
                    // 机械温控报警
                    if (cavity.pallletAlarm[i])
                    {
                        almID = (int)MsgID.PltCheckAlarm + cavityIdx * (int)OvenRowCol.MaxCol + i;
                        string msg = $"{cavityIdx + 1}层夹具{i + 1}夹具放平检测报警";
                        ShowMessageID(almID, msg, "请检查干燥炉", MessageType.MsgAlarm);
                        //string alarmCode = MesResources.Equipment.ResourceCode + "-037";
                        //string errmsg = string.Empty;
                        ////MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                        ////夹具放平检测报警
                        //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        Sleep(5000);
                    }
                }
                #endregion

                // 系统标记加热工作中
                if (CavityStatus.Heating == this.CavityState[cavityIdx])
                {
                    #region // 检查温度报警

                    // 温度报警
                    string[,] tempAlarm, tempAlarmValue;
                    if (CheckTempAlarm(cavity, out tempAlarm, out tempAlarmValue))
                    {
                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            if (SetCavityPltBatteryNG(cavityIdx, i, cavity))
                            {
                                foreach (var item in this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + i].Battery)
                                {
                                    if (BatteryStatus.NG == item.Type)
                                    {
                                    }
                                }
                            }
                        }

                        string msg = string.Format("{0}层温度报警：\r\n", cavityIdx + 1);
                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            msg += string.Format("夹具{0}：\r\n", (cavityIdx * (int)OvenRowCol.MaxCol + i + 1));
                            for (int j = 0; j < (int)OvenInfoCount.HeatPanelCount; j++)
                            {
                                if (!string.IsNullOrEmpty(tempAlarm[i, j]))
                                {
                                    msg += (tempAlarm[i, j] + tempAlarmValue[i, j] + "\r\n");
                                }
                            }
                        }
                        ShowMessageID((int)MsgID.TempAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgAlarm);
                        //string alarmCode = MesResources.Equipment.ResourceCode + "-038";
                        //string errmsg = string.Empty;
                        ////MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                        ////温度报警
                        //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);

                    }
                    #endregion

                    #region // 检查加热完成

                    int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol;
                    // 有夹具NG，则加热停止
                    if ((PalletStatus.NG == this.Pallet[pltIdx].State) || (PalletStatus.NG == this.Pallet[pltIdx + 1].State))
                    {
                        WriteLog(string.Format("RunWhile()操作：{0}层炉腔有夹具NG，发送停止", cavityIdx + 1));
                        this.writeOvenData.CavityDatas[cavityIdx].workState = (int)OvenStatus.WorkStop;
                        DryOvenWorkStart(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], false);
                        SetCavityState(cavityIdx, CavityStatus.Maintenance);

                    }
                    // 无夹具NG　&&　炉腔已停止加热
                    else if ((int)OvenStatus.WorkStop == cavity.workState)
                    {
                        uint setTime = (cavity.parameter.PreheatTime + cavity.parameter.VacHeatTime - (uint)this.maxWorkTimeRange);
                        // 加热时间足够
                        if (cavity.workTime >= setTime)
                        {
                            // Baking结束，上报结束状态

                            bool hasFake = false;
                            for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                            {
                                if (this.Pallet[pltIdx + i].HasFake())
                                {
                                    hasFake = true;
                                    break;
                                }
                            }
                            if (hasFake)
                            {
                                // 设置夹具-腔体状态：有假电池夹具，置为待检测
                                for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                {
                                    if (PalletStatus.OK == this.Pallet[pltIdx + i].State)
                                    {
                                        this.Pallet[pltIdx + i].State = PalletStatus.Detect;
                                        this.Pallet[pltIdx + i].EndDate = DateTime.Now;
                                    }
                                }
                                SetCavityState(cavityIdx, CavityStatus.WaitDetect);
                            }
                            else
                            {
                                // 设置夹具-腔体状态：无假电池夹具，置为等待结果
                                for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                                {
                                    if (PalletStatus.OK == this.Pallet[pltIdx + i].State)
                                    {
                                        this.Pallet[pltIdx + i].State = PalletStatus.WaitResult;
                                        this.Pallet[pltIdx + i].EndDate = DateTime.Now;
                                    }
                                }
                                SetCavityState(cavityIdx, CavityStatus.WaitResult);
                            }
                            SaveRunData(SaveType.Pallet);

                            #region // 烘烤结束IOT上报

                            IOTData pdata = new IOTData();
                            pdata.line = MachineCtrl.GetInstance().LineID;
                            pdata.equip = this.ovenIP;
                            pdata.floor = cavityIdx.ToString();
                            pdata.datetime = DateTime.Now;
                            pdata.points = new List<PointData>();

                            PointData points = new PointData();
                            points.code = "HHKD0001";
                            points.name = "设备编号";
                            points.type = "String";
                            points.unit = "";
                            points.value = $"{this.equipmentCode}-{cavityIdx}-{pltIdx % 2 + 1}";
                            pdata.points.Add(points);

                            string code = "";
                            int batCnt = 0;
                            for (int row = 0; row < this.Pallet[pltIdx].MaxRow; row++)
                            {
                                for (int col = 0; col < this.Pallet[pltIdx].MaxCol; col++)
                                {
                                    if (!string.IsNullOrEmpty(this.Pallet[pltIdx].Battery[row, col].Code) && this.Pallet[pltIdx].Battery[row, col].Type != BatteryStatus.Fake)
                                    {
                                        if (!string.IsNullOrEmpty(code))
                                        {
                                            code += ",";
                                        }
                                        code += this.Pallet[pltIdx].Battery[row, col].Code;
                                        batCnt++;
                                    }
                                }
                            }

                            pdata.points.Add(new PointData() { code = "HHKD0005", name = "腔体编码", type = "String", unit = "", value = this.equipmentCode });
                            if (pltIdx % 2 == 0)
                            {
                                pdata.points.Add(new PointData() { code = "HHKD0002", name = "左托盘编码", type = "String", unit = "", value = this.Pallet[pltIdx].Code });
                                pdata.points.Add(new PointData() { code = "HHKD0006", name = "左电芯编码", type = "String", unit = "", value = code });
                                pdata.points.Add(new PointData() { code = "HHKD2001", name = "左电芯数量", type = "Int16", unit = "", value = $"{batCnt}" });
                            }
                            else
                            {
                                pdata.points.Add(new PointData() { code = "HHKD0003", name = "右托盘编码", type = "String", unit = "", value = this.Pallet[pltIdx].Code });
                                pdata.points.Add(new PointData() { code = "HHKD0007", name = "右电芯编码", type = "String", unit = "", value = code });
                                pdata.points.Add(new PointData() { code = "HHKD2002", name = "右电芯数量", type = "Int16", unit = "", value = $"{batCnt}" });
                            }

                            // 工艺参数
                            // HHKD3001 预热段烘箱温度最大值
                            // HHKD3002 预热段烘箱温度最小值
                            // HHKD3003 预热段烘箱温度最均值
                            // HHKD3004 预热时间
                            // HHKD3005 干燥段温度最大值
                            // HHKD3006 干燥段温度最小值
                            // HHKD3007 干燥段温度均值
                            // HHKD3008 干燥段真空度最大值
                            // HHKD3009 干燥段真空度最小值
                            // HHKD3010 干燥段真空度均值
                            // HHKD3011 干燥时间
                            // HHKD3012 冷却时间
                            // HHKD3013 呼吸循环次数
                            // HHKD3015 腔体编号
                            // HHKD3016 开始烘烤时间
                            // HHKD3017 结束烘烤时间
                            // HHKD3018 投入时间
                            // HHKD3019 产出时间
                            // HHKD3021 综合判定结果
                            // HHKD3022 NG详情
                            // HHKD3023 返工次数
                            double tempUpper = 0;
                            double tempLower = 0;
                            if (cavity.tempValue[pltIdx, 0, 0] > cavity.parameter.SetTempValue)
                            {
                                tempUpper = cavity.tempValue[pltIdx, 0, 0];
                                tempLower = cavity.parameter.SetTempValue;
                            }
                            else
                            {
                                tempUpper = cavity.parameter.SetTempValue;
                                tempLower = cavity.tempValue[pltIdx, 0, 0];
                            }

                            pdata.points.Add(new PointData() { code = "HHKD3001", name = "预热段烘箱温度最大值", type = "Int16", unit = "℃", value = tempUpper.ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3002", name = "预热段烘箱温度最小值", type = "Int16", unit = "℃", value = tempLower.ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3003", name = "预热段烘箱温度最均值", type = "Int16", unit = "℃", value = cavityParameter.SetTempValue.ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3004", name = "预热时间", type = "Int16", unit = "℃", value = cavityParameter.PreheatTime.ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3005", name = "干燥段温度最大值", type = "Int16", unit = "℃", value = tempUpper.ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3006", name = "干燥段温度最小值", type = "Int16", unit = "℃", value = tempLower.ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3007", name = "干燥段温度均值", type = "Int16", unit = "℃", value = cavityParameter.SetTempValue.ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3011", name = "干燥时间", type = "Int16", unit = "min", value = (cavityParameter.PreheatTime + cavity.parameter.VacHeatTime).ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3008", name = "干燥段真空度最大值", type = "Int16", unit = "pa", value = (cavityParameter.BStateVacPressure).ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3009", name = "干燥段真空度最小值", type = "Int16", unit = "pa", value = (cavity.vacPressure).ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3010", name = "干燥段真空度均值", type = "Int16", unit = "pa", value = (cavity.vacPressure).ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3013", name = "呼吸循环次数", type = "int16", unit = "次", value = (cavityParameter.BreathCycleTimes).ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3015", name = "腔体编号", type = "int16", unit = "", value = (cavityIdx + 1).ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3016", name = "开始烘烤时间", type = "int16", unit = "", value = Def.GetTimeStemp(this.Pallet[pltIdx].StartDate).ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3017", name = "结束烘烤时间", type = "int16", unit = "", value = Def.GetTimeStemp(this.Pallet[pltIdx].EndDate).ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3018", name = "投入时间", type = "int16", unit = "", value = Def.GetTimeStemp(this.Pallet[pltIdx].StartDate).ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3019", name = "产出时间", type = "int16", unit = "", value = Def.GetTimeStemp(this.Pallet[pltIdx].EndDate).ToString() });
                            pdata.points.Add(new PointData() { code = "HHKD3021", name = "综合判定结果", type = "int16", unit = "", value = "1" });
                            pdata.points.Add(new PointData() { code = "HHKD3022", name = "NG详情", type = "int16", unit = "", value = "0" });
                            pdata.points.Add(new PointData() { code = "HHKD3023", name = "返工次数", type = "int16", unit = "", value = (this.CavityHeartCycle[cavityIdx] - 1).ToString() });

                            // HHKP0001 正极片水分   int16 ppm
                            // HHKP0002 负极片水分   int16 ppm
                            // HHKP0003 隔膜水分    int16 ppm
                            // HHKP0004 混合样水分   int16 ppm

                            IOTTaskList.Add(pdata);

                            #endregion 烘烤结束IOT上报
                        }
                        // 加热时间不足 && 且假电池夹具含有NG电池
                        else if ((cavity.workTime < setTime) && ((this.Pallet[pltIdx].HasFake() && PltHasNGBat(this.Pallet[pltIdx]))
                            || (this.Pallet[pltIdx + 1].HasFake() && PltHasNGBat(this.Pallet[pltIdx + 1]))))
                        {
                            SetCavityState(cavityIdx, CavityStatus.Maintenance);
                        }
                        // 加热时间不足 && 真空报警
                        else if ((cavity.workTime < setTime) && (cavity.vacAlarm))
                        {
                            SetCavityState(cavityIdx, CavityStatus.Maintenance);
                        }
                        // 加热时间不足
                        else if (cavity.workTime < setTime)
                        {
                            SetCavityState(cavityIdx, CavityStatus.Maintenance);

                            string msg = string.Format("{0}层异常停止加热，加热时间【{1}】 < 设定时间【{2} + {3}】，请检查！"
                                , (cavityIdx + 1), cavity.workTime, cavity.parameter.PreheatTime, cavity.parameter.VacHeatTime);
                            ShowMessageID((int)MsgID.HeatStop + cavityIdx, msg, "请检查干燥炉", MessageType.MsgWarning);
                            //string alarmCode = MesResources.Equipment.ResourceCode + "-039";
                            //string errmsg = string.Empty;
                            ////MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                            ////加热异常报警
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                    }
                    #endregion 检查加热完成
                }

                #region // 腔体实际加热中

                if ((uint)OvenStatus.WorkStart == cavity.workState)
                {
                    // 间隔保存一次
                    if ((DateTime.Now - this.bakingDataStartTime[cavityIdx]).TotalSeconds >= (this.workDataTime))
                    {
                        SaveWorkingData(cavityIdx, cavity);
                        this.bakingDataStartTime[cavityIdx] = DateTime.Now;

                        if (null != this.PltHeatTemp)
                        {
                            // 腔体中夹具
                            for (int col = 0; col < (int)OvenRowCol.MaxCol; col++)
                            {
                                int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol + col;
                                // 控温、巡检
                                for (int i = 0; i < 2; i++)
                                {
                                    // 发热板
                                    for (int j = 0; j < (int)OvenInfoCount.HeatPanelCount; j++)
                                    {
                                        int heatIdx = i * (int)OvenInfoCount.HeatPanelCount + j;
                                        this.PltHeatTemp[pltIdx][heatIdx].Add(cavity.tempValue[col, i, j]);
                                        long time = this.workDataTime;
                                        if (this.PltHeatTime[pltIdx][heatIdx].Count > 0)
                                        {
                                            time += this.PltHeatTime[pltIdx][heatIdx][this.PltHeatTime[pltIdx][heatIdx].Count - 1];
                                        }
                                        this.PltHeatTime[pltIdx][heatIdx].Add(Convert.ToUInt32(time));
                                    }
                                }
                            }

                        }
                    }
                    // 加热时间超过设定时间，停止加热
                    if (cavity.workTime > cavity.parameter.PreheatTime + cavity.parameter.VacHeatTime + 30)
                    {
                        //this.writeOvenData.CavityDatas[cavityIdx].workState = (uint)OvenStatus.WorkStop;
                        //DryOvenWorkStart(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], false);
                        {
                            string msg = string.Format("{0}层腔体已加热{1}分钟，超过设定时间{2} + {3} + 30分钟，触发加热时间防呆提示"
                                , cavityIdx + 1, cavity.workTime, cavity.parameter.PreheatTime, cavity.parameter.VacHeatTime);
                            ShowMessageID((int)MsgID.HeatTimeout + cavityIdx, msg, "请检查干燥炉", MessageType.MsgWarning);
                            //string alarmCode = MesResources.Equipment.ResourceCode + "-040";
                            //string errmsg = string.Empty;
                            ////MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);

                            ////加热时间超过设定时间报警
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                    }
                    // 加热过程中，真空阶段未抽真空
                    if ((cavity.workTime > cavity.parameter.PreheatTime + cavity.parameter.AStateVacTime + (cavity.parameter.BStateVacTime * 2))
                        && (cavity.vacPressure > (cavity.parameter.BStateVacPressure+30)) && (cavity.parameter.BreathCycleTimes < 1))
                    {
                        string msg = string.Format("{0}层腔体已加热{1}分钟，超过设定时间{2} + {3} + {4}分钟后，真空压力{5} > 设定压力值{6}，触发真空防呆提示"
                            , cavityIdx + 1, cavity.workTime, cavity.parameter.PreheatTime, cavity.parameter.AStateVacTime, cavity.parameter.BStateVacTime
                            , cavity.vacPressure, cavity.parameter.BStateVacPressure);
                        ShowMessageID((int)MsgID.HeatVacAlarm + cavityIdx, msg, "请检查干燥炉", MessageType.MsgWarning);
                        //string alarmCode = MesResources.Equipment.ResourceCode + "-041";
                        //string errmsg = string.Empty;
                        ////MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                        ////真空阶段未抽真空报警
                        //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                    }
                }
                #endregion

                #region // 腔体停止中，判断保压，提前破真空

                if (!Def.IsNoHardware() && (uint)OvenStatus.WorkStop == cavity.workState)
                {
                    // 等待测试结果时自动保压
                    if (this.waitResultPressure
                        && (CavityStatus.WaitResult == this.CavityState[cavityIdx])
                        && ((short)OvenStatus.DoorClose == cavity.doorState))
                    {
                        if (((uint)OvenStatus.PressureOpen != cavity.pressureState))
                        {
                            WriteLog("RunWhile()操作：等待测试结果时自动保压");
                            this.writeOvenData.CavityDatas[cavityIdx].pressureState = (uint)OvenStatus.PressureOpen;
                            if (!DryOvenPressure(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], true))
                            {
                                string msg = string.Format("{0}层等待测试结果时自动保压设置失败", cavityIdx + 1);
                                ShowMessageID((int)MsgID.PressureOpenClose, msg, "请检查干燥炉是否是远程状态", MessageType.MsgWarning);
                                string alarmCode = MesResources.Equipment.ResourceCode + "-042";
                                string errmsg = string.Empty;
                                //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                                //自动保压设置失败报警
                                //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                            }
                        }
                    }
                    // 设置保压
                    else if (this.CavityEnable[cavityIdx] && !this.CavityTransfer[cavityIdx] && (CavityStatus.Maintenance != this.CavityState[cavityIdx])
                        && ((uint)(this.CavityPressure[cavityIdx] ? OvenStatus.PressureOpen : OvenStatus.PressureClose) != cavity.pressureState))
                    {
                        //WriteLog("RunWhile()操作：设置保压：" + (this.CavityPressure[cavityIdx] ? "打开" : "关闭"));
                        this.writeOvenData.CavityDatas[cavityIdx].pressureState = (uint)(this.CavityPressure[cavityIdx] ? OvenStatus.PressureOpen : OvenStatus.PressureClose);
                        if (!DryOvenPressure(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], true))
                        {
                            string msg = string.Format("{0}层{1}保压失败"
                                , (cavityIdx + 1), (this.CavityPressure[cavityIdx] ? "打开" : "关闭"));
                            ShowMessageID((int)MsgID.PressureOpenClose, msg, "请检查干燥炉是否是远程状态", MessageType.MsgWarning);
                            string alarmCode = MesResources.Equipment.ResourceCode + "-043";
                            string errmsg = string.Empty;
                            //MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                            //保压失败报警
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                    }
                    // 提前破真空
                    if (this.CavityEnable[cavityIdx] && !this.CavityPressure[cavityIdx] && !this.CavityTransfer[cavityIdx]
                        && (CavityStatus.WaitResult != this.CavityState[cavityIdx])
                        && (CavityStatus.Maintenance != this.CavityState[cavityIdx])
                        && (cavity.vacPressure < (this.cavityParameter.OpenDoorVacPressure - 500))
                        && ((short)OvenStatus.BlowOpen != RCavity(cavityIdx).blowValveState))
                    {
                        //WriteLog($"RunWhile()操作：提前破真空[{cavity.vacPressure}<{this.cavityParameter.OpenDoorVacPressure}]：关闭真空阀，打开破真空阀");
                        this.writeOvenData.CavityDatas[cavityIdx].vacValveState = (short)OvenStatus.VacClose;
                        DryOvenVacuum(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], false);
                        this.writeOvenData.CavityDatas[cavityIdx].blowValveState = (short)OvenStatus.BlowOpen;
                        if (!DryOvenBlowAir(cavityIdx, this.writeOvenData.CavityDatas[cavityIdx], false))
                        {
                            string msg = string.Format("{0}层真空压力为{1} < {2}，提前破真空时打开破真空失败，请检查干燥炉"
                                , (cavityIdx + 1), cavity.vacPressure, this.cavityParameter.OpenDoorVacPressure);
                            ShowMessageID((int)MsgID.PressureOpenClose, msg, "请检查干燥炉是否是远程状态", MessageType.MsgWarning);
                            //string alarmCode = MesResources.Equipment.ResourceCode + "-044";
                            //string errmsg = string.Empty;
                            ////MesOperate.EquToMesAlarm(MesResources.Equipment, MesAlarmStatus.Happen, alarmCode, msg, MesAlarmLevel.L, ref errmsg);
                            ////破真空失败报警
                            //MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, alarmCode, msg, MesAlarmStatus.Happen, MesAlarmLevel.One, ref errmsg);
                        }
                    }
                }
                #endregion

                #region // 上报腔体状态给MES

                if (this.CavityState[cavityIdx] != this.CavityOldState[cavityIdx])
                {
                    this.CavityOldState[cavityIdx] = this.CavityState[cavityIdx];

                    MesMCState mesMC = MesMCState.Stop;
                    switch (this.CavityOldState[cavityIdx])
                    {
                        case CavityStatus.Normal:
                        case CavityStatus.WaitRebaking:
                            mesMC = MesMCState.Stop;
                            break;
                        case CavityStatus.Heating:
                        case CavityStatus.WaitDetect:
                        case CavityStatus.WaitResult:
                            mesMC = MesMCState.Running;
                            break;
                        case CavityStatus.Maintenance:
                            mesMC = MesMCState.Fault;
                            break;
                    }
                }
                #endregion


                #region // 上报腔体状态给MES  11-12
                if (!this.DryRun)
                {
                    MesMCState mesMC = MesMCState.Stop;
                    string equipmentStatusID = "";
                    switch (this.CavityState[cavityIdx])
                    {
                        case CavityStatus.Unknown:
                            mesMC = MesMCState.Stop;
                            equipmentStatusID = "200";
                            break;
                        case CavityStatus.Normal:
                        case CavityStatus.WaitRebaking:
                        case CavityStatus.Heating:
                        case CavityStatus.WaitDetect:
                        case CavityStatus.WaitResult:
                            mesMC = MesMCState.Running;
                            equipmentStatusID = "300";
                            break;
                        case CavityStatus.Maintenance:
                            mesMC = MesMCState.Fault;
                            equipmentStatusID = "900";
                            break;
                    }

                    if (this.CavityState[cavityIdx] != this.CavityOldState[cavityIdx])
                    {
                        this.CavityOldState[cavityIdx] = this.CavityState[cavityIdx];
                        string dryingOvenCode = "WH02C0122PR-HKX002" + (11 + Convert.ToInt32(this.RunName.Replace("干燥炉", ""))); //设备编号
                        string msg = "";
                        //if (!MachineCtrl.GetInstance().ACEQPTSTUS_Main(MesResources.Equipment, true/*MachineCtrl.GetInstance().Devicestatus*/, equipmentStatusID, dryingOvenCode, cavityIdx,this.RunName, ref msg))
                        //{

                        //    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);

                        //}
                    }
                }

                #endregion
            }
            #endregion


            #region // 安全门状态写入干燥炉

            bool safeDoor = MachineCtrl.GetInstance().SafeDoorStateOpen;
            if ((short)(safeDoor ? OvenStatus.McDoorOpen : OvenStatus.McDoorClose) != this.readOvenData.MCDoorState)
            {
                if (DryOvenIsConnect())
                {
                    //DryOvenSetMcDoorState(safeDoor, true);
                }
            }
            #endregion

        }
        #endregion

        #region // 腔体工作

        /// <summary>
        /// 检查腔体是否等待工作开始
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool CheckWaitWorkStart(int cavityIdx)
        {
            if (CavityStatus.Normal == this.CavityState[cavityIdx])
            {
                //if (CavityEnable[cavityIdx] && !CavityPressure[cavityIdx] && !CavityPressure[cavityIdx])
                if (CavityEnable[cavityIdx] && !CavityPressure[cavityIdx]) //wjj 220505 注释 上一行
                {
                    int idx = cavityIdx * (int)OvenRowCol.MaxCol;
                    if ((PalletStatus.OK == this.Pallet[idx].State)
                        && (PalletStage.Onload == this.Pallet[idx].Stage)
                        && !this.Pallet[idx].IsEmpty()
                        && (PalletStatus.OK == this.Pallet[idx + 1].State)
                        && (PalletStage.Onload == this.Pallet[idx + 1].Stage)
                        && !this.Pallet[idx + 1].IsEmpty())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 设置腔体状态
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private bool SetCavityState(int cavityIdx, CavityStatus state)
        {
            if ((0 > cavityIdx) || (cavityIdx >= (int)PalletRowCol.MaxRow))
            {
                return false;
            }
            if (CavityStatus.Maintenance == state)
            {
                this.CavityEnable[cavityIdx] = false;
                WriteParameter(this.RunModule, ("CavityEnable" + cavityIdx), bool.FalseString);
            }
            this.CavityState[cavityIdx] = state;
            SaveRunData(SaveType.Cavity);
            return true;
        }

        private bool SetCavityTransferState(int cavityIdx, CavityStatus state)
        {
            if ((0 > cavityIdx) || (cavityIdx >= (int)PalletRowCol.MaxRow))
            {
                return false;
            }
            if (CavityStatus.Normal == state)
            {
                this.CavityTransferRecv[cavityIdx] = false;
                WriteParameter(this.RunModule, ("CavityTransferRecv" + cavityIdx), bool.FalseString);
            }

            this.CavityState[cavityIdx] = state;
            SaveRunData(SaveType.Cavity);
            return true;
        }

        public bool ManualSetCavityState(int cavityIdx, CavityStatus state)
        {
            if ((0 > cavityIdx) || (cavityIdx >= (int)PalletRowCol.MaxRow))
            {
                return false;
            }
            if (CavityStatus.Maintenance == state)
            {
                this.CavityEnable[cavityIdx] = false;
                WriteParameter(this.RunModule, ("CavityEnable" + cavityIdx), bool.FalseString);
            }
            this.CavityState[cavityIdx] = state;

            switch (state)
            {
                case CavityStatus.Unknown:              // 未知状态 = -1
                    {
                        int pltIdx = 0;
                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            pltIdx = cavityIdx * 2 + i;
                            if (this.Pallet[pltIdx].IsEmpty())
                            {
                                this.Pallet[pltIdx].State = PalletStatus.Invalid;
                                this.Pallet[pltIdx].Stage = PalletStage.Invalid;

                                this.Pallet[pltIdx].StartDate = DateTime.MinValue;
                                this.Pallet[pltIdx].EndDate = DateTime.MinValue;
                            }
                        }
                        SaveRunData(SaveType.Cavity | SaveType.Variables | SaveType.Pallet);
                    }
                    break;
                case CavityStatus.Normal:               // 正常状态 = 0
                    {
                        int pltIdx = 0;
                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            pltIdx = cavityIdx * 2 + i;
                            if (!this.Pallet[pltIdx].IsEmpty())
                            {
                                this.Pallet[pltIdx].State = PalletStatus.OK;
                                this.Pallet[pltIdx].Stage = PalletStage.Onload;

                                this.Pallet[pltIdx].StartDate = DateTime.Now;
                                this.Pallet[pltIdx].EndDate = DateTime.MinValue;
                            }
                            else
                            {
                                this.Pallet[pltIdx].State = PalletStatus.Invalid;
                                this.Pallet[pltIdx].Stage = PalletStage.Invalid;

                                this.Pallet[pltIdx].StartDate = DateTime.MinValue;
                                this.Pallet[pltIdx].EndDate = DateTime.MinValue;
                            }
                        }

                        // 干燥炉放上料完成OK带假电池满夹具
                        EventList modEvent = EventList.DryOvenPlaceOnlOKFakeFullPallet;
                        SetEvent(this, modEvent, EventStatus.Require);
                        SaveRunData(SaveType.Cavity | SaveType.Pallet);
                    }
                    break;
                case CavityStatus.Heating:              // 加热状态
                    {
                        int pltIdx = 0;
                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            pltIdx = cavityIdx * 2 + i;
                            this.Pallet[pltIdx].State = PalletStatus.OK;
                            this.Pallet[pltIdx].Stage = PalletStage.Onload;

                            //this.Pallet[pltIdx].StartDate = DateTime.Now;
                            this.Pallet[pltIdx].EndDate = DateTime.MinValue;
                        }
                        SaveRunData(SaveType.Cavity | SaveType.Variables | SaveType.Pallet);
                    }
                    break;
                case CavityStatus.WaitDetect:           // 等待测试
                    {
                        int pltIdx = 0;
                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            pltIdx = cavityIdx * 2 + i;
                            // 设置夹具-腔体状态：有假电池夹具，置为待检测
                            if (this.Pallet[pltIdx].HasFake())
                            {
                                this.Pallet[pltIdx].State = PalletStatus.Detect;
                            }
                            else
                            {
                                this.Pallet[pltIdx].State = PalletStatus.WaitResult;
                            }
                            this.Pallet[pltIdx].Stage = PalletStage.Onload;
                            double setTime = (cavityParameter.PreheatTime + cavityParameter.VacHeatTime);
                            this.Pallet[pltIdx].StartDate = DateTime.Now.AddMinutes(-setTime);
                            this.Pallet[pltIdx].EndDate = DateTime.Now;
                        }
                        SetCavityState(cavityIdx, CavityStatus.WaitDetect);
                        SaveRunData(SaveType.Cavity | SaveType.Variables | SaveType.Pallet);
                    }
                    break;
                case CavityStatus.WaitResult:           // 等待结果
                    {
                        int pltIdx = 0;
                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            pltIdx = cavityIdx * 2 + i;
                            // 设置夹具-腔体状态：有假电池夹具，置为待检测
                            if (this.Pallet[pltIdx].HasFake())
                            {
                                this.Pallet[pltIdx].State = PalletStatus.WaitResult;
                            }
                            else
                            {
                                this.Pallet[pltIdx].State = PalletStatus.WaitResult;
                            }
                            this.Pallet[pltIdx].Stage = PalletStage.Onload;
                            double setTime = (cavityParameter.PreheatTime + cavityParameter.VacHeatTime);
                            this.Pallet[pltIdx].StartDate = DateTime.Now.AddMinutes(-setTime);
                            this.Pallet[pltIdx].EndDate = DateTime.Now;
                        }
                        SetCavityState(cavityIdx, CavityStatus.WaitResult);
                        SaveRunData(SaveType.Cavity | SaveType.Variables | SaveType.Pallet);
                    }
                    break;
                case CavityStatus.WaitRebaking:         // 等待回炉
                    {
                        this.waterContentValue[cavityIdx, 0] = 1000.0;
                        this.waterContentValue[cavityIdx, 1] = 1000.0;
                        this.waterContentValue[cavityIdx, 2] = 1000.0;

                        int pltIdx = cavityIdx * 2;
                        if (this.Pallet[pltIdx].BakingCount >= MachineCtrl.GetInstance().BakingMaxCount)
                        {
                            for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                            {
                                pltIdx = cavityIdx * 2 + i;
                                if (PalletStatus.WaitResult == this.Pallet[pltIdx].State)
                                {
                                    // 假电池已被拿走，清除数据
                                    int fakeRow, fakeCol;
                                    fakeRow = fakeCol = -1;
                                    if (this.Pallet[pltIdx].GetFakePos(ref fakeRow, ref fakeCol))
                                    {
                                        this.Pallet[pltIdx].Battery[fakeRow, fakeCol].Release();
                                    }
                                    this.Pallet[pltIdx].State = PalletStatus.NG;
                                }
                            }
                            SetCavityState(cavityIdx, CavityStatus.Maintenance);
                        }
                        else
                        {
                            for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                            {
                                pltIdx = cavityIdx * 2 + i;
                                this.Pallet[pltIdx].State = PalletStatus.ReputFake;
                                this.Pallet[pltIdx].Stage = PalletStage.Onload;
                                // 假电池已被拿走，清除数据
                                int fakeRow, fakeCol;
                                fakeRow = fakeCol = -1;
                                if (this.Pallet[pltIdx].GetFakePos(ref fakeRow, ref fakeCol))
                                {
                                    this.Pallet[pltIdx].Battery[fakeRow, fakeCol].Release();
                                }

                                {
                                    double setTime = (cavityParameter.PreheatTime + cavityParameter.VacHeatTime);
                                    this.Pallet[pltIdx].StartDate = DateTime.Now.AddMinutes(-setTime);
                                    this.Pallet[pltIdx].EndDate = DateTime.Now;
                                }
                            }
                            SaveWaterContentResult(cavityIdx, this.waterContentValue, false);
                            SetCavityState(cavityIdx, CavityStatus.WaitRebaking);
                        }

                        SaveRunData(SaveType.Cavity | SaveType.Variables | SaveType.Pallet);
                    }
                    break;
                case CavityStatus.Maintenance:          // 维修状态
                    {
                        if (CavityStatus.Maintenance != CavityState[cavityIdx])
                        {
                            SetCavityState(cavityIdx, CavityStatus.Maintenance);
                        }
                        // 这里腔体未启用转移功能，不主动发送转移请求
                        //int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol;
                        //for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        //{
                        //    if (PalletStatus.Invalid != this.Pallet[pltIdx + i].State)
                        //    {
                        //        EventList modEvent = EventList.DryOvenPickTransferPallet;
                        //        EventStatus evtstate = GetEvent(this, modEvent);
                        //        if ((EventStatus.Invalid == evtstate) || (EventStatus.Finished == evtstate))
                        //        {
                        //            SetEvent(this, modEvent, EventStatus.Require, (pltIdx + i));
                        //        }
                        //    }
                        //}
                        SaveRunData(SaveType.Cavity);
                    }
                    break;
                case CavityStatus.BakingFinish:
                    {
                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol + i;
                            if (PalletStatus.WaitResult == this.Pallet[pltIdx].State)
                            {
                                this.Pallet[pltIdx].Stage = PalletStage.Baked;
                                this.Pallet[pltIdx].State = PalletStatus.WaitOffload;

                                this.Pallet[pltIdx].Battery[0, 0].Code = "";
                                this.Pallet[pltIdx].Battery[0, 0].Type = BatteryStatus.FakeTag;

                                // wjj add 220612
                                // 保存夹具烘烤次数
                                //PalletFormula pltf = new PalletFormula();
                                //pltf.PalletID = this.Pallet[i].Code;
                                //pltf.BakeCavityID = cavityNo = $"{this.equipmentCode}-{cavityIdx + 1}-{i % 2 + 1}"; ;
                                //pltf.BakeCnts = this.Pallet[i].BakingCnts + 1;
                                //pltf.BakeTime = this.Pallet[i].EndDate.ToString("yyyy-MM-dd HH:mm:ss");
                                //dbRecord.SetPalletOutBaking(pltf);
                                // wjj add 220612
                            }
                        }
                        SaveWaterContentResult(cavityIdx, this.waterContentValue, true);
                        SetCavityState(cavityIdx, CavityStatus.Normal);
                        SaveRunData(SaveType.Pallet);
                    }
                    break;
                default:
                    break;
            }

            return true;
        }

        /// <summary>
        /// 设置腔体水含量
        /// </summary>
        /// <param name="nTier"></param>
        /// <param name="dWater"></param>
        public void SetWaterContent(int cavityIdx, double[] water)
        {
            Trace.WriteLine(this.waterContentValue.GetLength(1));
            if ((cavityIdx > -1) && (cavityIdx < (int)OvenRowCol.MaxRow) && (water.Length == this.waterContentValue.GetLength(1)))
            {
                for (int i = 0; i < water.Length; i++)
                {
                    this.waterContentValue[cavityIdx, i] = water[i];
                }
                SaveRunData(SaveType.Variables);

                //#region // 烘烤结束IOT上报

                //int pltIdx = 0;
                //for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                //{
                //    pltIdx = cavityIdx * (int)OvenRowCol.MaxCol + i;

                //    IOTData pdata = new IOTData();
                //    pdata.line = MachineCtrl.GetInstance().LineID;
                //    pdata.equip = this.ovenIP;
                //    pdata.floor = cavityIdx.ToString();
                //    pdata.datetime = DateTime.Now;
                //    pdata.points = new List<PointData>();

                //    PointData points = new PointData();
                //    points.code = "HHKD0001";
                //    points.name = "设备编号";
                //    points.type = "String";
                //    points.unit = "";
                //    points.value = $"{this.equipmentCode}-{cavityIdx}-{pltIdx % 2 + 1}";
                //    pdata.points.Add(points);

                //    pdata.points.Add(new PointData() { code = "HHKD0005", name = "腔体编码", type = "String", unit = "", value = this.equipmentCode });
                //    if (pltIdx % 2 == 0)
                //    {
                //        pdata.points.Add(new PointData() { code = "HHKD0002", name = "左托盘编码", type = "String", unit = "", value = this.Pallet[pltIdx].Code });
                //    }
                //    else
                //    {
                //        pdata.points.Add(new PointData() { code = "HHKD0003", name = "右托盘编码", type = "String", unit = "", value = this.Pallet[pltIdx].Code });
                //    }

                //    pdata.points.Add(new PointData() { code = "HHKP0001", name = "正极片水分", type = "int16", unit = "ppm", value = (water[0]).ToString() });
                //    pdata.points.Add(new PointData() { code = "HHKP0002", name = "负极片水分", type = "int16", unit = "ppm", value = (water[1]).ToString() });
                //    pdata.points.Add(new PointData() { code = "HHKP0003", name = "隔膜水分", type = "int16", unit = "ppm", value = (water[2]).ToString() });
                //    pdata.points.Add(new PointData() { code = "HHKP0004", name = "混合样水分", type = "int16", unit = "ppm", value = (water[3]).ToString() });

                //    // HHKP0001 正极片水分   int16 ppm
                //    // HHKP0002 负极片水分   int16 ppm
                //    // HHKP0003 隔膜水分    int16 ppm
                //    // HHKP0004 混合样水分   int16 ppm

                //    IOTTaskList.Add(pdata);
                //}

                ////#endregion 烘烤结束IOT上报
            }
        }

        /// <summary>
        /// 检查水含量结果：true检查完成
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="waterValue"></param>
        /// <returns>true检查完成，false检查条件不满足</returns>
        private bool CheckWaterContentResult(int cavityIdx, double[,] waterValue)
        {
            for (int i = 0; i < waterValue.GetLength(1); i++)
            {
                if (waterValue[cavityIdx, i] <= 0.0)
                {
                    return false;
                }
            }
            if (CavityStatus.WaitResult == this.CavityState[cavityIdx])
            {
                Pallet[] plt = new Pallet[(int)OvenRowCol.MaxCol];
                bool[] OutBaking = new bool[(int)OvenRowCol.MaxCol];
                // 检查夹具状态
                for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                {
                    plt[i] = this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + i];
                    if ((PalletStatus.WaitResult != plt[i].State)
                        || (PalletStage.Onload != plt[i].Stage))
                    {
                        return false;
                    }
                }

                //var path = GetFileInfoPath(cavityIdx);
                //if (string.IsNullOrEmpty(path))
                //{
                //    ShowMessageID((int)MsgID.OvenDataError, string.Format("找不到,{0}-{1}层干燥过程数据", this.RunName, (cavityIdx) + 1), "请检查文件是否存在，或者是否已经打开，打开则要关闭", MessageType.MsgAlarm);
                //    return false;
                //}
                //var lstT = Def.ReadCSV(path);
                //var array = lstT.LastOrDefault();
                //if (array == null)
                //{
                //    ShowMessageID((int)MsgID.OvenDataError, string.Format("找不到,{0}-{1}层干燥过程数据", this.RunName, (cavityIdx) + 1), "请检查文件是否存在，或者是否已经打开，打开则要关闭", MessageType.MsgAlarm);
                //    return false;
                //}
                //var bakingTime = Convert.ToDouble(array[1]); //时间
                //var bakingVcuum = Convert.ToDouble(array[2]);//真空
                //var bakingTemp = Convert.ToDouble(array[3]); //温度

                bool waterOK = true;
                for (int i = 0; i < waterValue.GetLength(1); i++)
                {
                    if (0 == i && waterValue[cavityIdx, 0] > this.positiveWaterStandard)
                    {
                        waterOK = false;
                        break;
                    }
                    else if (1 == i && waterValue[cavityIdx, 1] > this.negativeWaterStandard)
                    {
                        waterOK = false;
                        break;
                    }
                    //else if(2 == i && waterValue[cavityIdx, 2] > this.separatorWaterStandard)
                    //{
                    //    waterOK = false;
                    //    break;
                    //}
                    //else if (3 == i && waterValue[cavityIdx, 3] > this.blendWaterStandard)
                    //{
                    //    waterOK = false;
                    //    break;
                    //}
                }

                try
                {
                    OutBaking[0] = true;
                    OutBaking[1] = true;
                    // 水含量合格
                    if (waterOK)
                    {
                        // 上报水含量成功，再置夹具状态
                        int plts = 0;
                        string cavityNo = "";
                        foreach (var item in plt)
                        {
                            //if (item.OutBak)
                            //{
                            //    continue;
                            //}
                            double[] arrWater = new double[3];

                            for (int i = 0; i < waterValue.GetLength(1); i++)
                            {
                                arrWater[i] = waterValue[cavityIdx, i];
                            }
                            cavityNo = $"{this.equipmentCode}-{cavityIdx + 1}-{plts % 2 + 1}";

                            //电池生产信息添加数据库
                            var paramData = new ProcessData();
                            var bakingOutData = new MySqlBakingOut.EquBakingOut();
                            //var bakingOutData = new MySqlBakingOut.EquBakingOut();
                            //var data = new List<ProcessData>();
                            //paramData.OnloadTime
                            //paramData.OffloadTime
                            paramData.StartTime = item.StartDate;
                            paramData.EndTime = item.EndDate;
                            paramData.PreheatTime = cavityParameter.PreheatTime.ToString();
                            paramData.VacHeatTime = cavityParameter.VacHeatTime.ToString();
                            paramData.CoolingTime = "20";
                            paramData.SetTempValue = cavityParameter.SetTempValue.ToString();
                            paramData.TempUpperlimit = cavityParameter.TempUpperlimit.ToString();
                            paramData.TempLowerlimit = cavityParameter.TempLowerlimit.ToString();
                            paramData.PreVacTime = (cavityParameter.AStateVacTime + cavityParameter.BStateVacTime).ToString();
                            paramData.BlowTime = cavityParameter.OpenDoorBlowTime.ToString();
                            bakingOutData.Status = "OK";
                            bakingOutData.BakingOutTime = DateTime.Now;
                            Param param = new Param();
                            string workPlace = "DAL1HK01";
                            string opName = "电芯烘烤";
                            string msg = "";
                            object[] Parameters = new object[3];
                            param.getMesParam(cavityParameter, arrWater, ref Parameters);
                            for (int col = 0; col < item.MaxCol; col++)
                            {
                                for (int row = 0; row < item.MaxRow; row++)
                                {
                                    if (!string.IsNullOrEmpty(item.Battery[row, col].Code.ToString()))
                                    {
                                        paramData.LotNo = item.Battery[row, col].Code.ToString();
                                        bakingOutData.LotNo = item.Battery[row, col].Code.ToString();
                                        
                                        //水含量合格，电池加热工艺数据保存到数据库
                                        MySqlProcess.InsertRecord(paramData);
                                        //水含量合格，电池合格信息保存到数据库
                                        MySqlBakingOut.InsertRecord(bakingOutData);
                                        if (item.Battery[row,col].Type == BatteryStatus.OK)
                                        {
                                            //MES产出时参数上传
                                            if (!Jeve_Mes.Mes_ReportParam(paramData.LotNo, workPlace, opName, Parameters, ref msg))
                                            {
                                                //ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                                                ShowMessageID((int)MsgID.MesNGErr, msg, "请人工检查", MessageType.MsgAlarm);
                                            }
                                            //MES产出时产品上报
                                            if (!Jeve_Mes.Mes_ReportSN(workPlace, paramData.LotNo, BatteryStatus.OK, ref msg))
                                            {
                                                ShowMessageID((int)MsgID.MesNGErr, msg, "请人工检查", MessageType.MsgAlarm);
                                            }
                                        }
                                       
                                    }
                                    //data.Add(paramData);
                                    
                                }
                            }



                            //var pos = string.Format("{0}-{1}", this.RunName.Replace("干燥炉", ""), (cavityIdx + 1));  //烘烤炉子
                            //string dryingOvenCode = "WH02C0122PR-HKX002" + (11 + Convert.ToInt32(this.RunName.Replace("干燥炉", ""))); //设备编号
                            //string machineHalt = ""; //停机原因
                            //string dryingOvenAlarm = "";  //干燥炉报警
                            //string deviceStatus = "300"; //设备实时状态

                            //Param param = new Param();
                            //object[] Parameters = new object[24];
                            //param.getParam(cavityParameter, item.BakingCount.ToString(), pos, item.StartDate.ToString("yyyy-MM-dd HH:mm:ss"), item.EndDate.ToString("yyyy-MM-dd HH:mm:ss"), arrWater, ref Parameters);
                            //// MES 电芯出站
                            //if (MachineCtrl.GetInstance().ACLOGOFF_Main(MesResources.Equipment, item, Parameters, dryingOvenCode, this.RunName, ref msg))
                            //{

                            //    OutBaking[plts] = true;
                            //}
                            //else
                            //{
                            //    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                            //    OutBaking[plts] = false;
                            //    //return false;

                            //}
                            plts++;
                        }

                        for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                        {
                            int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol + i;
                            if (PalletStatus.WaitResult == plt[i].State && OutBaking[i])
                            {
                                plt[i].Stage = PalletStage.Baked;
                                plt[i].State = PalletStatus.WaitOffload;
                                plt[i].OutBak = true;
                                SetCavityState(cavityIdx, CavityStatus.Normal);
                                // wjj add 220612
                                // 保存夹具烘烤次数
                                PalletFormula pltf = new PalletFormula();
                                pltf.PalletID = plt[i].Code;
                                pltf.BakeCavityID = cavityNo = $"{this.equipmentCode}-{cavityIdx + 1}-{i % 2 + 1}"; ;
                                pltf.BakeCnts = plt[i].BakingCnts + 1;
                                pltf.BakeTime = plt[i].EndDate.ToString("yyyy-MM-dd HH:mm:ss");
                                dbRecord.SetPalletOutBaking(pltf);
                                // wjj add 220612
                            }
                            //出站失败夹具状态修改为等待结果-- wjl add 230228
                            else
                            {
                                plt[i].Stage = PalletStage.Onload;
                                plt[i].State = PalletStatus.WaitResult;
                                plt[i].OutBak = false;
                                SetCavityState(cavityIdx, CavityStatus.WaitResult);
                            }
                        }
                        WriteParameter(this.RunModule,string.Format("CavityOvenEx{0}",cavityIdx),"false");
                        ReadParameter();
                        SaveWaterContentResult(cavityIdx, this.waterContentValue, waterOK);
                        SaveRunData(SaveType.Pallet);
                        return true;
                    }
                    // 水含量超标
                    else
                    {
                        if (plt[0].BakingCount >= MachineCtrl.GetInstance().BakingMaxCount)
                        {
                            for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                            {
                                if (PalletStatus.WaitResult == plt[i].State)
                                {
                                    // 假电池已被拿走，清除数据
                                    int fakeRow, fakeCol;
                                    fakeRow = fakeCol = -1;
                                    if (plt[i].GetFakePos(ref fakeRow, ref fakeCol))
                                    {
                                        plt[i].Battery[fakeRow, fakeCol].Release();
                                    }
                                    plt[i].State = PalletStatus.NG;
                                }
                            }
                            SetCavityState(cavityIdx, CavityStatus.Maintenance);
                        }
                        else
                        {
                            for (int i = 0; i < (int)OvenRowCol.MaxCol; i++)
                            {
                                if (PalletStatus.WaitResult == plt[i].State)
                                {
                                    plt[i].State = PalletStatus.ReputFake;
                                }
                            }
                            WriteParameter(this.RunModule, string.Format("CavityOvenEx{0}", cavityIdx), "true");
                            ReadParameter();
                            SetCavityState(cavityIdx, CavityStatus.WaitRebaking);
                        }
                        SaveWaterContentResult(cavityIdx, this.waterContentValue, waterOK);
                        SaveRunData(SaveType.Pallet);
                        return true;
                    }
                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("RunProcessDryingOven", $"CheckWaterContentResult() error : {ex.Message}\r\n{ex.StackTrace}");
                }
            }
            return false;
        }

        /// <summary>
        /// 返回URL
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private string GetFileInfoPath(int cavityIdx)
        {
            try
            {
                List<FileInfo> fileInfos = new List<FileInfo>();
                var path = string.Format(@"{0}\干燥过程数据\{1}-{2}层"
                         , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1));
                var files = Directory.GetFiles(path, "*.csv");
                foreach (var v in files)
                {
                    if (v.Contains("CN"))
                    {
                        FileInfo fileInfo = new FileInfo(v);
                        fileInfos.Add(fileInfo);
                    }
                }
                List<FileInfo> fileInfos1 = fileInfos.OrderByDescending((o) => o.CreationTime).ToList();
                if (fileInfos1 != null && fileInfos1.Count > 0)
                {
                    return fileInfos1[0].FullName;
                }
            }
            catch { }
            return string.Empty;
        }

        /// <summary>
        /// 检查腔体温度报警：夹具-发热板
        /// </summary>
        /// <param name="cavityData">腔体数据</param>
        /// <param name="alarmMsg">腔体夹具报警信息</param>
        /// <param name="alarmValue">腔体夹具报警温度值</param>
        /// <returns></returns>
        public bool CheckTempAlarm(CavityData cavityData, out string[,] alarmMsg, out string[,] alarmValue)
        {
            bool hasAlm = false;
            alarmMsg = new string[(int)OvenRowCol.MaxCol, (int)OvenInfoCount.HeatPanelCount];
            alarmValue = new string[(int)OvenRowCol.MaxCol, (int)OvenInfoCount.HeatPanelCount];
            for (int col = 0; col < (int)OvenRowCol.MaxCol; col++)
            {
                int pltID = (0 == this.dryingOvenGroup) ? col : ((int)OvenRowCol.MaxCol - 1 - col);
                for (int idx = 0; idx < (int)OvenInfoCount.HeatPanelCount; idx++)
                {
                    // 先赋值为空，防止外部不判断为null直接使用导致异常
                    alarmMsg[col, idx] = "";
                    for (int almIdx = 0; almIdx < (int)OvenTmpAlarm.End; almIdx++)
                    {
                        if (cavityData.tempAlarmValue[pltID, idx] < 1.0)
                        {
                            // 没有温度值查询下一个
                            continue;
                        }
                        if ((cavityData.tempAlarm[pltID, idx] & (0x01 << almIdx)) == (0x01 << almIdx))
                        {
                            switch ((OvenTmpAlarm)almIdx)
                            {
                                case OvenTmpAlarm.LowTmp:
                                    alarmMsg[col, idx] += string.Format("{0}低温", (idx + 1));
                                    break;
                                case OvenTmpAlarm.OverTmp:
                                    alarmMsg[col, idx] += string.Format("{0}超温", (idx + 1));
                                    break;
                                case OvenTmpAlarm.HighTmp:
                                    alarmMsg[col, idx] += string.Format("{0}超高温", (idx + 1));
                                    break;
                                case OvenTmpAlarm.Exceptional:
                                    alarmMsg[col, idx] += string.Format("{0}信号异常", (idx + 1));
                                    break;
                                case OvenTmpAlarm.Difference:
                                    alarmMsg[col, idx] += string.Format("{0}温差异常", (idx + 1));
                                    break;
                            }
                            hasAlm = true;
                        }
                    }
                    alarmValue[col, idx] = cavityData.tempAlarmValue[pltID, idx].ToString("#0.00");
                }
            }
            return hasAlm;
        }

        /// <summary>
        /// 随机生成腔体状态：空运行测试
        /// </summary>
        /// <param name="ovenData"></param>
        private void RandomFaultState(ref DryingOvenData ovenData)
        {
            for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
            {
                double workTime = (DateTime.Now - this.Pallet[i * (int)OvenRowCol.MaxCol].StartDate).TotalMinutes;
                double setTime = (cavityParameter.PreheatTime + cavityParameter.VacHeatTime);
                if ((CavityStatus.Heating == this.CavityState[i]) && (workTime >= setTime))
                {
                    ovenData.CavityDatas[i].workState = (uint)OvenStatus.WorkStop;
                    ovenData.CavityDatas[i].workTime = Convert.ToUInt32(setTime);
                    ovenData.CavityDatas[i].parameter.PreheatTime = cavityParameter.PreheatTime;
                    ovenData.CavityDatas[i].parameter.VacHeatTime = cavityParameter.VacHeatTime;
                }
            }
        }

        #endregion

        #region // 腔体数据

        /// <summary>
        /// 干燥炉依据分组，关于col列的实际索引
        /// </summary>
        /// <param name="col"></param>
        /// <returns>返回干燥炉col列的实际索引</returns>
        public int DryOvenGroupColIdx(int col)
        {
            return (0 == this.dryingOvenGroup) ? (col) : (1 - col);
        }

        /// <summary>
        /// 获取干燥炉的远程运行状态
        /// </summary>
        /// <returns></returns>
        public short DryOvenRemoteState()
        {
            return this.readOvenData.RemoteState;
        }

        /// <summary>
        /// 获取干燥炉的远程写入的设备安全门状态
        /// </summary>
        /// <returns></returns>
        public short DryOvenMcDoorState()
        {
            return this.readOvenData.MCDoorState;
        }

        /// <summary>
        /// 读取的腔体数据
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        public CavityData RCavity(int cavityIdx)
        {
            return readOvenData.CavityDatas[cavityIdx];
        }

        /// <summary>
        /// 写入的腔体数据
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private CavityData WCavity(int cavityIdx)
        {
            return writeOvenData.CavityDatas[cavityIdx];
        }

        #endregion

        #region // IO及电机操作接口

        /// <summary>
        /// 夹具放平检测
        /// </summary>
        /// <param name="pltIdx"></param>
        /// <param name="hasPlt"></param>
        /// <param name="alarm"></param>
        public override bool PalletKeepFlat(int pltIdx, bool hasPlt, bool alarm = true)
        {
            if (!MachineCtrl.GetInstance().IsPalletKeepFlat)
            {
                return true;
            }
            if (pltIdx < 0 || pltIdx >= (int)ModuleMaxPallet.DryingOven)
            {
                return false;
            }
            int cavityIdx = pltIdx / ((int)OvenRowCol.MaxCol);
            int pltCol = (0 == this.dryingOvenGroup) ? (pltIdx % ((int)OvenRowCol.MaxCol)) : (1 - pltIdx % ((int)OvenRowCol.MaxCol));
            if ((RCavity(cavityIdx).pallletAlarm[pltCol])
                || (RCavity(cavityIdx).palletState[pltCol] != (short)(hasPlt ? OvenStatus.PalletHave : OvenStatus.PalletNot)))
            {
                if (alarm)
                {
                    ShowMessageID((int)MsgID.PltStateErr, ("夹具状态错误，应该为" + (hasPlt ? "ON" : "OFF")), "请检查干燥炉中夹具状态是否正确", MessageType.MsgAlarm);
                }
                return false;
            }
            return true;
        }

        #endregion

        #region // 安全检查

        /// <summary>
        /// 检查机器人是否在cavityIdx腔体中
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        public bool CheckRobotTransferSafe(int cavityIdx)
        {
            RobotActionInfo action = new RobotActionInfo();
            RunProcessRobotTransfer run = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            if (null != run)
            {
                action = run.GetRobotActionInfo(false);
                for (int i = 0; i < (int)OvenRowCol.MaxRow; i++)
                {
                    if (action.station == ((int)TransferRobotStation.DryOven_0 + this.dryingOvenID))
                    {
                        if ((i == cavityIdx) || (cavityIdx < 0))
                        {
                            if ((action.row == i) && ((action.order == RobotOrder.PICKIN) || action.order == RobotOrder.PLACEIN))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        #endregion

        #region // 添加删除夹具

        public override void ManualAddPallet(int pltIdx, int maxRow, int maxCol, PalletStatus pltState, BatteryStatus batState)
        {
            this.Pallet[pltIdx].State = pltState;
            this.Pallet[pltIdx].SetRowCol(maxRow, maxCol);
            SetCavityState(pltIdx / (int)OvenRowCol.MaxCol, CavityStatus.Normal);
            if (BatteryStatus.Invalid != batState)
            {
                this.Pallet[pltIdx].Stage = PalletStage.Onload;
            }
            if (!this.Pallet[pltIdx].IsEmpty())
            {
                for (int row = 0; row < this.Pallet[pltIdx].MaxRow; row++)
                {
                    for (int col = 0; col < this.Pallet[pltIdx].MaxCol; col++)
                    {
                        if (BatteryStatus.FakeTag == this.Pallet[pltIdx].Battery[row, col].Type)
                        {
                            this.Pallet[pltIdx].Battery[row, col].Release();
                        }
                        if ((BatteryStatus.Invalid != this.Pallet[pltIdx].Battery[row, col].Type)
                            && (BatteryStatus.Fake != this.Pallet[pltIdx].Battery[row, col].Type))
                        {
                            this.Pallet[pltIdx].Battery[row, col].Type = batState;
                            this.Pallet[pltIdx].Battery[row, col].NGType = BatteryNGStatus.Invalid;
                        }
                    }
                }
            }
            SaveRunData(SaveType.Pallet, pltIdx);
        }

        public override void ManualClearPallet(int pltIdx)
        {
            this.Pallet[pltIdx].Release();
            SetCavityState(pltIdx / (int)OvenRowCol.MaxCol, CavityStatus.Normal);
            SaveRunData(SaveType.Pallet, pltIdx);
        }

        public override void ManualAddPalletBattery(int pltIdx, int maxRow, int maxCol, bool isFake, PalletStatus pltState, BatteryStatus batState)
        {
            if (PalletStatus.OK == pltState)
            {
                this.Pallet[pltIdx].State = PalletStatus.OK;
                this.Pallet[pltIdx].SetRowCol(maxRow, maxCol);

                if (pltIdx > -1 /*&& pltIdx <= (int)ModuleMaxPallet.OnloadRobot*/ && Pallet[pltIdx].IsEmpty())
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

        public bool ManualSetPalletState(int pltIdx, PalletStatus state)
        {
            if (0 > pltIdx || pltIdx > (int)OvenRowCol.MaxRow * (int)OvenRowCol.MaxCol)
                return false;

            this.Pallet[pltIdx].State = state;
            SaveRunData(SaveType.Pallet, pltIdx);

            return true;
        }

        #endregion

        #region // 数据保存

        private void WriteLog(string log, OptMode mode = OptMode.Auto)
        {
            //this.ovenLogFile.WriteLog(DateTime.Now, this.RunName, log, logType);
            DataBaseLog.AddDryingOvenLog(new DataBaseLog.OvenLogFormula(Def.GetProductFormula(), this.dryingOvenID, this.RunName
                , MachineCtrl.GetInstance().OperaterID, DateTime.Now.ToString(Def.DateFormal), mode.ToString(), log));
        }

        /// <summary>
        /// 保存加热过程数据：时序表
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="cavityData"></param>
        private void SaveWorkingData(int cavityIdx, CavityData cavityData)
        {
            StringBuilder title, text;
            title = new StringBuilder();
            text = new StringBuilder();

            for (int col = 0; col < (int)OvenRowCol.MaxCol; col++)
            {
                string pltCode = this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].Code;
                string startTime = this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].StartDate.ToString("yyyy_MM_dd_HHmmss");
                if (string.IsNullOrEmpty(pltCode) && (DateTime.MinValue == this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + col].StartDate))
                {
                    pltCode = $"Plt{col + 1}";
                    startTime = DateTime.Now.AddMinutes(-cavityData.workTime).ToString("yyyy_MM_dd_HH");
                }
                string file = string.Format(@"{0}\干燥过程数据\{1}-{2}层\{3}-{4}.csv"
                        , MachineCtrl.GetInstance().ProductionFilePath, this.RunName, (cavityIdx + 1)
                        , pltCode, startTime);

                title.Clear();
                title.Append("日期时间,加热时间,真空值");
                text.Clear();
                text.Append($"{DateTime.Now.ToString(Def.DateFormal)},{cavityData.workTime},{cavityData.vacPressure}");
                for (int i = 0; i < (int)OvenInfoCount.HeatPanelCount; i++)
                {
                    title.Append(string.Format(",{0}层夹具{1}控温{2},{0}层夹具{1}巡检{2}", (cavityIdx + 1), (col + 1), (i + 1)));
                    text.Append($",{cavityData.tempValue[col, 0, i].ToString("#0.00")},{cavityData.tempValue[col, 1, i].ToString("#0.00")}");
                }
                Def.ExportCsvFile(file, title.ToString(), (text.ToString() + "\r\n"));
            }
        }

        /// <summary>
        /// 保存水含量结果
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="water"></param>
        private void SaveWaterContentResult(int cavityIdx, double[,] water, bool waterOK)
        {
            string file, title, text;
            file = string.Format(@"{0}\水含量结果\{1}\{2}\{3}层{1}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"), this.RunName, (cavityIdx + 1));
            title = "日期,时间,夹具1,夹具2,假电池位置,水含量1,水含量2,合格/超标";
            text = string.Format("{0},{1},{2}", DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss")
                , this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol].Code, this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + 1].Code);
            int fakeRow, fakeCol;
            fakeRow = fakeCol = -1;
            if (this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol].GetFakePos(ref fakeRow, ref fakeCol))
            {
                text += string.Format(",夹具1-{0}行-{1}列", (fakeRow + 1), (fakeCol + 1));
            }
            else if (this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + 1].GetFakePos(ref fakeRow, ref fakeCol))
            {
                text += string.Format(",夹具2-{0}行-{1}列", (fakeRow + 1), (fakeCol + 1));
            }
            else
            {
                text += ",未搜索到假电池";
            }
            text += string.Format(",{0},{1},{2}[{3}]", water[cavityIdx, 0], water[cavityIdx, 1], (waterOK ? "合格" : "超标"), this.positiveWaterStandard);

            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }

        #endregion

        #region // 上传Mes数据

        /// <summary>
        /// 输出电池的四个温度数据：
        ///     发热板1控温/巡检，发热板2控温/巡检(无则空)
        /// </summary>
        /// <param name="cavityIdx"></param>
        /// <param name="pltCol"></param>
        /// <param name="batRow"></param>
        /// <param name="batCol"></param>
        /// <param name="temp"></param>
        private void GetBatTemp(int cavityIdx, int pltCol, int batRow, int batCol, ref double[] temp)
        {
            // 底板
            switch (batCol)
            {
                case 0:
                case 1:
                    for (int j = 0; j < 2; j++)  // 控温/巡检
                    {
                        temp[j] = RCavity(cavityIdx).tempValue[pltCol, j, 0];
                    }
                    break;
                case 2:
                    for (int j = 0; j < 4; j++)  // 控温/巡检
                    {
                        temp[j] = RCavity(cavityIdx).tempValue[pltCol, j % 2, j / 2 + 1];
                    }
                    break;
                case 3:
                case 4:
                    for (int j = 0; j < 2; j++)  // 控温/巡检
                    {
                        temp[j] = RCavity(cavityIdx).tempValue[pltCol, j, 3];
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region // 夹具中电池状态

        /// <summary>
        /// 夹具中含有NG电池
        /// </summary>
        /// <param name="plt"></param>
        /// <returns></returns>
        private bool PltHasNGBat(Pallet plt)
        {
            for (int row = 0; row < plt.MaxRow; row++)
            {
                for (int col = 0; col < plt.MaxCol; col++)
                {
                    if (BatteryStatus.NG == plt.Battery[row, col].Type)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 温度报警时设置腔体中夹具电池NG
        /// </summary>
        /// <param name="cavityData"></param>
        public bool SetCavityPltBatteryNG(int cavityIdx, int pltCol, CavityData cavityData)
        {
            bool hasNG = false;
            Pallet plt = this.Pallet[cavityIdx * (int)OvenRowCol.MaxCol + pltCol];
            for (int idx = 0; idx < (int)OvenInfoCount.HeatPanelCount; idx++)
            {
                for (OvenTmpAlarm almIdx = OvenTmpAlarm.Normal; almIdx < OvenTmpAlarm.End; almIdx++)
                {
                    #region // 没有NG，查询下一个
                    if (cavityData.tempAlarmValue[pltCol, idx] < 1.0)
                    {
                        // 没有温度值
                        continue;
                    }
                    if ((cavityData.tempAlarm[pltCol, idx] & (0x01 << (int)almIdx)) != (0x01 << (int)almIdx))
                    {
                        // 没有当前查询的温度报警状态
                        continue;
                    }
                    #endregion

                    switch (almIdx)
                    {
                        #region // 低温，超温，信号异常：整盘NG
                        case OvenTmpAlarm.LowTmp:
                        case OvenTmpAlarm.OverTmp:
                        case OvenTmpAlarm.Exceptional:
                            {
                                // 置夹具NG
                                int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol + pltCol;
                                if (PalletStatus.Invalid != this.Pallet[pltIdx].State)
                                {
                                    this.Pallet[pltIdx].State = PalletStatus.NG;
                                    SaveRunData(SaveType.Pallet);
                                }
                                break;
                            }
                        #endregion

                        #region // 超高温：电芯NG

                        case OvenTmpAlarm.HighTmp:
                            {
                                int pltIdx = cavityIdx * (int)OvenRowCol.MaxCol + pltCol;
                                switch (idx)
                                {
                                    case 3:
                                        {
                                            for (int row = 0; row < this.Pallet[pltIdx].MaxRow; row++)
                                            {
                                                if (BatteryStatus.OK == this.Pallet[pltIdx].Battery[row, 0].Type)
                                                {
                                                    //this.Pallet[pltIdx].Battery[row, 0].Type = BatteryStatus.NG;
                                                    this.Pallet[pltIdx].Battery[row, 0].NGType = BatteryNGStatus.HighTmp;
                                                }
                                            }
                                            break;
                                        }
                                    case 2:
                                        {
                                            for (int row = 0; row < this.Pallet[pltIdx].MaxRow; row++)
                                            {
                                                if (BatteryStatus.OK == this.Pallet[pltIdx].Battery[row, 1].Type)
                                                {
                                                    //this.Pallet[pltIdx].Battery[row, 1].Type = BatteryStatus.NG;
                                                    this.Pallet[pltIdx].Battery[row, 1].NGType = BatteryNGStatus.HighTmp;
                                                }
                                                if (BatteryStatus.OK == this.Pallet[pltIdx].Battery[row, 2].Type)
                                                {
                                                    //this.Pallet[pltIdx].Battery[row, 2].Type = BatteryStatus.NG;
                                                    this.Pallet[pltIdx].Battery[row, 2].NGType = BatteryNGStatus.HighTmp;
                                                }
                                            }
                                            break;
                                        }
                                    case 1:
                                        {
                                            for (int row = 0; row < this.Pallet[pltIdx].MaxRow; row++)
                                            {
                                                if (BatteryStatus.OK == this.Pallet[pltIdx].Battery[row, 2].Type)
                                                {
                                                    //this.Pallet[pltIdx].Battery[row, 2].Type = BatteryStatus.NG;
                                                    this.Pallet[pltIdx].Battery[row, 2].NGType = BatteryNGStatus.HighTmp;
                                                }
                                                if (BatteryStatus.OK == this.Pallet[pltIdx].Battery[row, 3].Type)
                                                {
                                                    //this.Pallet[pltIdx].Battery[row, 3].Type = BatteryStatus.NG;
                                                    this.Pallet[pltIdx].Battery[row, 3].NGType = BatteryNGStatus.HighTmp;
                                                }
                                            }
                                            break;
                                        }
                                    case 0:
                                        {
                                            for (int row = 0; row < this.Pallet[pltIdx].MaxRow; row++)
                                            {
                                                if (BatteryStatus.OK == this.Pallet[pltIdx].Battery[row, 4].Type)
                                                {
                                                    //this.Pallet[pltIdx].Battery[row, 4].Type = BatteryStatus.NG;
                                                    this.Pallet[pltIdx].Battery[row, 4].NGType = BatteryNGStatus.HighTmp;
                                                }
                                            }
                                            break;
                                        }
                                    default:
                                        break;
                                }
                                // 置夹具NG
                                if (PalletStatus.Invalid != this.Pallet[pltIdx].State)
                                {
                                    this.Pallet[pltIdx].State = PalletStatus.NG;
                                    hasNG = true;
                                }
                                break;
                            }
                        #endregion

                        #region // 温差异常：不NG
                        case OvenTmpAlarm.Difference:
                            {
                                break;
                            }
                            #endregion
                    }
                }
            }
            if (hasNG)
            {
                SaveRunData(SaveType.Pallet);
                return true;
            }
            return false;
        }

        #endregion
        #region // 模组重置

        public override void ManualResetEvent()
        {
            //查找调度机器人当前关联的模组
            LoadRunData();
            for (int i = (int)EventList.DryOvenPlaceEmptyPallet; i < (int)EventList.DryOvenPickPlaceEnd; i++)
            {
                SetEvent(this, (EventList)i, EventStatus.Invalid);
            }
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            SaveRunData(SaveType.AutoStep);
        }
        #endregion
        public override void OutMESEvent(Pallet plt, int cavityIdx)
        {
            //var path = GetFileInfoPath(cavityIdx);
            //if (string.IsNullOrEmpty(path))
            //{
            //    ShowMessageID((int)MsgID.OvenDataError, string.Format("找不到,{0}-{1}层干燥过程数据", this.RunName, (cavityIdx) + 1), "请检查文件是否存在，或者是否已经打开，打开则要关闭", MessageType.MsgAlarm);
            //    return;
            //}
            //var lstT = Def.ReadCSV(path);
            //var array = lstT.LastOrDefault();
            //if (array == null)
            //{
            //    ShowMessageID((int)MsgID.OvenDataError, string.Format("找不到,{0}-{1}层干燥过程数据", this.RunName, (cavityIdx) + 1), "请检查文件是否存在，或者是否已经打开，打开则要关闭", MessageType.MsgAlarm);
            //    return;
            //}
            //var bakingTime = Convert.ToDouble(array[1]); //时间
            //var bakingVcuum = Convert.ToDouble(array[2]);//真空
            //var bakingTemp = Convert.ToDouble(array[3]); //温度
            string msg = "";
            //bool isDebug = Def.IsNoHardware();
            //if (!MesOperate. (MesResources.Equipment, item, mapParam, isDebug, ref msg))
            //{
            //    ShowMessageID((int)MsgID.WaterValueErr, msg, $"请检查上传的 {item.Code} 出站信息", MessageType.MsgAlarm);
            //    return false;
            //}
            //string tempUpperlimit = this.cavityParameter.TempUpperlimit.ToString();//烘烤设定最大温度
            //string tempLowerlimit = this.cavityParameter.TempLowerlimit.ToString();//烘烤设定最小温度
            //string setTempValue = this.cavityParameter.SetTempValue.ToString(); //烘烤设定温度
            //var pos = string.Format("{0}-{1}", this.RunName.Replace("干燥炉", ""), (cavityIdx + 1));  //烘烤炉子
            double[] arrWater = new double[2];
            //arrWater[0] = this.waterContentValue[cavityIdx, 0];
            //arrWater[1] = this.waterContentValue[cavityIdx, 1];
            //string dryingOvenCode = "WH02C0122PR-HKX002" + (11 + Convert.ToInt32(this.RunName.Replace("干燥炉", ""))); //设备编号
            //string machineHalt = ""; //停机原因
            //string dryingOvenAlarm = "";  //干燥炉报警
            //string deviceStatus = "300"; //设备实时状态

            Param param = new Param();
            object[] Parameters = new object[3];
            string workPlace = "DAL1HK01";
            string opName = "电芯烘烤";
            param.getMesParam(cavityParameter, arrWater, ref Parameters);
            for (int row = 0; row < plt.MaxRow; row++)
            {
                for (int col = 0; col < plt.MaxCol; col++)
                {
                    if (plt.Battery[row, col].Type == BatteryStatus.OK)
                    {
                        string sfc = plt.Battery[row, col].Code.Trim();
                        //MES产出时参数上传
                        if (!Jeve_Mes.Mes_ReportParam(sfc, workPlace, opName, Parameters, ref msg))
                        {
                            ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                        }
                        //MES产出时产品上报
                        if (!Jeve_Mes.Mes_ReportSN(workPlace, sfc, BatteryStatus.OK, ref msg))
                        {
                            ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                        }
                    }
                }
            }


            //// MES 电芯出站
            //if (!MachineCtrl.GetInstance().ACLOGOFF_Main(MesResources.Equipment, plt, Parameters, dryingOvenCode, this.RunName, ref msg))
            //{
            //    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            //    //ShowMessageID((int)MsgID.OvenDataError, string.Format("出站失败！"), "请检查MES相关！", MessageType.MsgAlarm);
            //}
            //else
            //{
            //    ShowMsgBox.ShowDialog($"出站成功！", MessageType.MsgAlarm);
            //    //ShowMessageID((int)MsgID.OvenDataError, string.Format("出站成功！"), "！", MessageType.MsgAlarm);
            //}
        }
        public void WriteParameterEX(string section, string key, string value)
        {
            base.WriteParameter(section, key, value);
        }
    }
}
