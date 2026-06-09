using HelperLibrary;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;
using Machine.Framework.WCF;
using NetMQ.Sockets;
using NetMQ;
using Machine.Framework;
using System.Xml.Serialization;
using BakingDataLib;
using System.Windows.Documents;
using Machine.SQLServer;
using Machine.Framework.Mes;

namespace Machine
{
    public class MachineCtrl : ControlInterface
    {
        #region // 枚举：模组数据，报警列表

        private enum MsgID
        {
            Start = ModuleMsgID.SystemStartID,
            DoorAlarm_0,
            DoorAlarm_End = DoorAlarm_0 + 9,
            OnloadSysRunning,
            TransferSysRunning,
            OffloadSysRunning,
            SysRunningEnd = OnloadSysRunning + 9,
            ModuleDisconnect,
            ModuleDisconnectEnd = ModuleDisconnect + 9,
            RobotRun,
            RobotRunningEnd = RobotRun + 9,
            AirPressureAlm,
            HeartbeatErr,
            MesStateErr,
            MesRecipeListGetErr,
            MesRecipeGetErr,
            MesRecipeErr,
            MesRecipeVExamineErr,
            UnbindPltErr,
        }
        #endregion

        #region // 字段，属性

        #region // IO

        // 设备按钮
        private int[] IStartButton;             // 输入：启动按钮
        private int[] IStopButton;              // 输入：停止按钮
        //private int[] IEStopButton;             // 输入：急停按钮,工控机急停硬件断电控制，安全门急停软件控制
        private int[] IResetButton;             // 输入：复位按钮

        private int[] IEStopTranButton;                // 输入：操作台急停
        private int[] IEStopOnloadButton;                // 输入：上料急停
        private int[] IEStopOffloadButton;                // 输入：下料急停
        private int[] IEStopDoor1Button;                // 输入：正门急停
        private int[] IEStopdoor2Button;                // 输入：侧门急停


        private int[] OStartLed;                // 输出：启动按钮LED
        private int[] OStopLed;                 // 输出：停止按钮LED
        private int[] OResetLed;                // 输出：复位按钮LED




        // 灯塔
        private int[] OLightTowerRed;           // 输出：灯塔：红
        private int[] OLightTowerYellow;        // 输出：灯塔：黄
        private int[] OLightTowerGreen;         // 输出：灯塔：绿
        private int[] OLightTowerBuzzer;        // 输出：灯塔：蜂鸣器

        // 安全门
        private int[] SafeDoorState;            // 安全门状态
        //private int[] ISafeDoorOpenBtn;         // 安全门开门按钮
        //private int[] ISafeDoorCloseBtn;        // 安全门关门按钮
        //private int[] OSafeDoorOpenLed;         // 安全门开门按钮LED
        //private int[] OSafeDoorCloseLed;        // 安全门关门按钮LED
        //private int[] OSafeDoorUnlock;          // 安全门解锁
        //private string[] SafeDoorStopModule;    // 安全门打开时停止运行模组
        //private int[] SafeDoorDelay;            // 安全门打开延时：毫秒ms
        private bool safeDoorEnabled;             // 启用安全门

        // 气压报警
        private int IAirPressureAlarm;          // 气压报警

        #endregion

        #region // ModuleEx.cfg配置

        public string MachineName { get; private set; }        // 系统类名
        public short MachineID { get; private set; }           // 设备ID
        public short LineID { get; private set; }
        public short HalfDryingOvens { get; private set; }     // 一半的干燥炉数量：用以绘图
        /// <summary>
        /// 下料缓存线数量：用以绘图
        /// </summary>
        public int OffloadBuffers { get; private set; }        // 
        public bool AlarmStopMC { get; private set; }          // 报警是否整线停机

        public string machineServerIP;                        // 服务端IP
        private int machineServerPort;                         // 服务端Port
        private List<string> machineClientIP;                  // 客户端IP
        private List<int> machineClientPort;                   // 客户端Port

        private List<string> idList;

        public string[] idArray;


        #endregion

        #region // 参数

        public int PalletMaxRow { get; private set; }          // 夹具最大行，只能为奇数：（0<X<PalletRowCol.MaxRow）
        public int PalletMaxCol { get; private set; }          // 夹具最大列：（0<X<PalletRowCol.MaxCol）
        public int BakingMaxCount { get; private set; }        // 烘烤最大次数：超过则置NG
        public bool DataRecover { get; set; }                  // 数据恢复
        public bool UpdataMes { get; set; }                    // 上传MES
        public bool MesCheck { get; set; }                     // 入站校验 ，MES前期设备调试用
        public bool UpdataMesTest { get; set; }                // 上传MES测试

        public bool Devicestatus { get; set; }                 //设备状态变更



        private WCF_Client wcf_Client;                         //MES客户端
        /// <summary>
        /// 上料方式（true：自动上料，false：手动上料）
        /// </summary>
        public bool autoOnLoadBattery { get; set; }            //  2022.07.12 8:47
        /// <summary>
        /// 下料清尾料-冷却上料和冷却下料
        /// </summary>
        public bool OffloadClear { get; set; }
        public string ProductionFilePath { get; private set; } // 生产信息文件路径
        public string MesFilePath { get; private set; }        // MES信息文件路径

        private int productionFileStorageLife;                 // 生产信息文件存储时间：天
        private string mcLogFilePath;                          // Log文件相对路径文件夹
        private int mcLogFileSize;                             // Log文件大小：兆M
        private int mcLogFileStorageLife;                      // Log文件存储时间：天

        public bool isMESConnect;                  // MES连接状态
        private Object mesFileLock;                 // Mes通讯文件锁
        public bool bUploadingOfflineDataIn { get; set; } = false;//是否正在上传进站离线数据
        public bool bUploadingOfflineDataOut { get; set; } = false;//是否正在上传出站站离线数据
        public bool bUploadingOfflineDataBind { get; set; } = false;//是否正在上传出站站离线数据

        private static bool PullInLogHasChanged;
        private static string PullInExCsvFilePath;

        private static bool OutLogHasChanged;
        private static string OutExCsvFilePath;

        /// <summary>
        /// MES连接状态
        /// </summary>
        public bool IsMESConnect
        {
            get
            { return isMESConnect; }

            set
            { this.isMESConnect = value; }
        }

        public string[] InterLockCode;              // 互锁信号代码（MES配置更改界面修改）
        public bool bInterLockResult;               // 互锁信号结果
        public int nInterLockCodeIndex;             // 互锁信号代码索引
        /// <summary>
        /// 互锁日志文件名
        /// </summary>
        private string InterLockLogFileName;

        /// <summary>
        /// 读写PLC日志输出
        /// </summary>
        public bool WritePlcLog;

        //夹爪防呆
        public bool FingerCheckCanActive;

        /// <summary>
        /// true = 检测放平信号 false=不检测放平信号
        /// </summary>
        public bool IsPalletKeepFlat;                       // 检测炉子夹具放平

        #endregion

        #region // 模组数据

        public bool SafeDoorStateOpen { get; private set; }               // 安全门状态：true：已打开；false：关闭
        public List<RunProcess> ListRuns { get; private set; }        // 运行模组
        public DataBaseRecord dbRecord;                               // 数据库记录
        public string OperaterID;                           // 操作者ID
        public bool MaintenanceLock;                        // 维护锁屏

        private static MachineCtrl machineCtrl;             // 设备
        private List<string> listInput;                     // 输入点
        private List<string> listOutput;                    // 输出点
        private List<string> listMotor;                     // 电机

        private Dictionary<string, ParameterFormula> insertParameterList;      // 模组中插入的参数集：<参数关键字key, 参数样式>
        private Dictionary<string, ParameterFormula> dataBaseParameterList;    // 数据库中已保存的参数
        private PropertyManage parameterProperty;           // 参数管理类

        private bool autoConnectCSState;                    // 客户端自动重连服务端状态
        private DateTime autoConnectCSTime;                 // 客户端自动重连服务端计时
        private ModuleServer machineServer;                 // 服务端
        private List<ModuleClient> machineClient;           // 客户端
        private Dictionary<int, ModuleSocketData> moduleSocketData;   // 网络模组数据

        private List<Task> taskList;                        // MachineCtrl中创建的所有Task
        private List<int> MsgList;                          // 非阻塞报警对话框弹出列表
        private bool hasMsgBox;                             // 模组是否有enum MESSAGE_TYPE任意一项信息弹窗：当自动运行有弹窗时，请置为TRUE

        private bool monitorRunning;                        // 监视线程运行中
        private bool resetButtonOff;                        // 复位按钮OFF状态
        private bool startButtonOff;                        // 复位按钮OFF状态
        private DateTime setTowerStart;                     // 设置灯塔计时

        // MES项
        private DateTime heartbeatTime;                     // Mes心跳计时
        private int heartbeatCount;                         // Mes无心跳计次
        private HttpClient httpClient;                      // MES交互
        private MesMCState mesOldState;                     // MES定义设备原状态
        private bool mcStopTimeReset;                       // 设备停机时间复位
        public DateTime McStopTime { get; private set; }    // 设备停机时间

        public int onloadRunState;                     // 上料运行状态
        public int offloadRunState;                     // 下料运行状态

        OnloadClient onloadClient;                   //上料客户端
        OffloadClient offloadClient;                 //下料客户端
        OnloadData onloadData;                      //上料端数据
        OffloadData OffloadData;                     //下料端数据

        public List<MesParameterData> Param;

        public delegate void ElapsedEventHandler(string strLogingID);
        public event ElapsedEventHandler LogingOutUI;

        public delegate void UnLineDataListHander(string value, string unLineData, string pltCode);
        public event UnLineDataListHander unLineDataListHander;
        //public delegate void UnLineDataUnBindListHander();
        //public event UnLineDataUnBindListHander unLineDataUnBindListHander;
        //public delegate void UnLineDataInListHander();
        //public event UnLineDataInListHander unLineDataInListHander;
        //public delegate void UnLineDataOutListHander();
        //public event UnLineDataOutListHander unLineDataOutListHander;

        public class ParameterData
        {
            public string StepID { get; set; }       //步骤代码
            public string StepName { get; set; }     //步骤名
            public string ParamID { get; set; }      //参数代码
            public string ParamName { get; set; }    //参数名
            public string ParamStand { get; set; }   //参数标准值
            public string ParamUpper { get; set; }   //参数上限
            public string ParamLower { get; set; }   //参数下限
        }


        public class BasketData
        {
            public string LotNo { get; set; }                // 电芯条码
            public string SlotNo { get; set; }             // 位置号
            public string Status { get; set; }            // 批次状态
            public string ErrorCode { get; set; }          // 不良代码
            public string Grade { get; set; }          // 档位
            public string Exclude { get; set; }             // 排出工序
            public string Fake { get; set; }            // 假电池
            public string OpOrder { get; set; }          // 工序任务
            public string OpName { get; set; }    // 工序名
            public string PreOpOrder { get; set; }             // 前工序任务
            public string PreOpName { get; set; }            // 前工序名
            public string NGCount { get; set; }          // 当站NG次数
            public string AllowReInput { get; set; }    // 是否可复投
        }

        #endregion

        #region //OPC UA
        private PublisherSocket m_publisherSocket;
        private BakingData m_bakingData;
        #endregion

        #endregion

        #region // 设备初始化

        public MachineCtrl()
        {
            this.UpdataMesTest = true;
            this.ListRuns = new List<RunProcess>();
            this.listInput = new List<string>();
            this.listOutput = new List<string>();
            this.listMotor = new List<string>();

            this.safeDoorEnabled = false;
            this.OffloadClear = false;
            this.DataRecover = true;
            this.UpdataMes = Def.IsNoHardware(); //MES在线状态
            this.MesCheck = true;    //MES入站校验
            this.Devicestatus = true;    //设备状态变更
            this.dbRecord = new DataBaseRecord();
            this.moduleSocketData = new Dictionary<int, ModuleSocketData>();
            this.insertParameterList = new Dictionary<string, ParameterFormula>();
            this.dataBaseParameterList = new Dictionary<string, ParameterFormula>();
            this.parameterProperty = new PropertyManage();
            this.monitorRunning = false;
            this.setTowerStart = DateTime.Now;
            this.OperaterID = "";
            this.MaintenanceLock = false;
            this.httpClient = new HttpClient();
            this.hasMsgBox = false;
            this.autoConnectCSState = false;
            this.autoConnectCSTime = DateTime.Now;
            this.mcStopTimeReset = false;
            this.McStopTime = DateTime.Now;
            this.WritePlcLog = false;
            this.IsPalletKeepFlat = true;
            this.isMESConnect = false;
            this.FingerCheckCanActive = true;
            this.mesOldState = MesMCState.Other;
            this.MsgList = new List<int>();
            this.InterLockLogFileName = "MES下发互锁日志";
            this.bInterLockResult = false;
            this.nInterLockCodeIndex = 0;
            autoOnLoadBattery = true;
            this.mesFileLock = new object();

            wcf_Client = new WCF_Client();      //MES客户端

            //onloadClient = new OnloadClient();
            //offloadClient = new OffloadClient();
            //onloadData = new OnloadData();
            //OffloadData = new OffloadData();

            this.InterLockCode = new string[3];
            for (int nIdx = 0; nIdx < InterLockCode.Length; nIdx++)
            {
                InterLockCode[nIdx] = string.Empty;
            }

            InitParameter();
            // 添加参数
            string description;
            description = $"夹具最大行，只能为偶数：（0＜X≤{(int)PalletRowCol.MaxRow}）";
            InsertVoidParameter("PalletMaxRow", "夹具最大行", description, PalletMaxRow, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);
            description = $"夹具最大列：（0＜X≤{(int)PalletRowCol.MaxCol}）";
            InsertVoidParameter("PalletMaxCol", "夹具最大列", description, PalletMaxCol, RecordType.RECORD_INT, ParameterLevel.PL_IDLE_ADMIN);
            InsertVoidParameter("BakingMaxCount", "烘烤最大次数", "烘烤最大次数：未超过时则重新烘烤，超过则置NG不再继续烘烤", BakingMaxCount, RecordType.RECORD_INT);
            //InsertVoidParameter("mcLogFilePath", "Log存储路径", "Log存储文件的相对路径", mcLogFilePath, RecordType.RECORD_STRING);
            InsertVoidParameter("mcLogFileSize", "Log文件大小", "Log文件的大小：兆(M)", mcLogFileSize, RecordType.RECORD_INT);
            InsertVoidParameter("mcLogFileStorageLife", "Log存储时间", "Log存储时间：天；超时后删除", mcLogFileStorageLife, RecordType.RECORD_INT);
            InsertVoidParameter("ProductionFilePath", "生产文件路径", "生产信息文件的存储完整路径", ProductionFilePath, RecordType.RECORD_STRING);
            InsertVoidParameter("productionFileStorageLife", "生产文件存储", "生产信息文件存储时间：天", productionFileStorageLife, RecordType.RECORD_INT);
            InsertVoidParameter("safeDoorEnabled", "安全门监控", "安全门监控：true=是 false=否", safeDoorEnabled, RecordType.RECORD_BOOL);

            #region //OPCUA
            m_publisherSocket = new PublisherSocket();
            m_publisherSocket.Options.SendHighWatermark = 1000;
            m_publisherSocket.Bind("tcp://*:12345");
            m_bakingData = new BakingData();
            #endregion
        }

        public static MachineCtrl GetInstance()
        {
            if (null == machineCtrl)
            {
                machineCtrl = new MachineCtrl();
            }
            return machineCtrl;
        }

        /// <summary>
        /// 执行与释放或重置非托管资源相关的应用程序定义的任务。
        /// </summary>
        public void Dispose()
        {
            TotalData.WriteTotalData();
            ReleaseThread();
            this.dbRecord.CloseDataBase();
            DataBaseLog.CloseDataBase();
            SetTowerButton(MCState.NumMCState);
        }

        public bool Initialize(IntPtr hMsgWnd)
        {
            string section, name;

            #region // input
            for (int index = 0; index < int.MaxValue; index++)
            {
                section = "INPUT" + index;
                name = IniFile.ReadString(section, "Num", "", Def.GetAbsPathName(Def.InputCfg));
                if ("" == name)
                {
                    break;
                }
                this.listInput.Add(name);
            }
            #endregion

            #region // output
            for (int index = 0; index < int.MaxValue; index++)
            {
                section = "OUTPUT" + index;
                name = IniFile.ReadString(section, "Num", "", Def.GetAbsPathName(Def.OutputCfg));
                if ("" == name)
                {
                    break;
                }
                this.listOutput.Add(name);
            }
            #endregion

            #region // motor
            for (int index = 0; index < int.MaxValue; index++)
            {
                section = string.Format("{0}Motor{1}.cfg", Def.MotorCfgFolder, index);
                name = "Motor" + index;
                if (!File.Exists(section))
                {
                    break;
                }
                this.listMotor.Add(name);
            }
            #endregion

            // 删除已有模组信息，重新创建
            if (File.Exists(Def.ModuleCfg))
            {
                File.Delete(Def.ModuleCfg);
            }

            if (!base.Initialize(hMsgWnd, listMotor.Count, listInput.Count, listOutput.Count))
            {
                return false;
            }

            #region // 电机点位初始化

            int num = DeviceManager.GetMotorManager().MotorsTotal;
            for (int index = 0; index < num; index++)
            {
                if (!LoadMotorLocation(index))
                {
                    return false;
                }
            }
            #endregion

            return true;
        }

        protected override bool InitializeRunThreads(IntPtr hMsgWnd)
        {
            Trace.Assert(null == this.RunsCtrl, "ControlInterface.RunsCtrl is null.");

            #region // 系统配置

            this.AlarmStopMC = IniFile.ReadBool("Run", "AlarmStopMC", false, Def.GetAbsPathName(Def.MachineCfg));
            this.MachineName = IniFile.ReadString("Modules", "MachineName", "System", Def.GetAbsPathName(Def.ModuleExCfg));
            this.MachineID = (short)IniFile.ReadInt("Modules", "MachineID", -1, Def.GetAbsPathName(Def.ModuleExCfg));
            this.LineID = (short)IniFile.ReadInt("Modules", "LineID", 0, Def.GetAbsPathName(Def.ModuleExCfg));
            if (this.MachineID < 0)
            {
                ShowMsgBox.ShowDialog("设备编号MachineID未配置，请在ModuleEx.cfg中配置", MessageType.MsgAlarm);
            }

            this.HalfDryingOvens = (short)IniFile.ReadInt("Modules", "HalfDryingOvens", 0, Def.GetAbsPathName(Def.ModuleExCfg));
            this.OffloadBuffers = IniFile.ReadInt("Modules", "OffloadBuffers", 0, Def.GetAbsPathName(Def.ModuleExCfg));

            #endregion

            #region // 系统参数

            ReadParameter();

            #endregion

            #region // 检查文件路径

            // 运行数据路径
            if (!Def.CreateFilePath(Def.GetAbsPathName(Def.RunDataFolder))
                || !Def.CreateFilePath(Def.GetAbsPathName(Def.RunDataBakFolder)))
            {
                Trace.Assert(false, "CreateFilePath( " + Def.GetAbsPathName(Def.RunDataFolder) + " ) fail.");
                return false;
            }
            // Log文件路径
            if (!Def.CreateFilePath(Def.GetAbsPathName(Def.MachineLogFolder))
                || !Def.CreateFilePath(Def.GetAbsPathName(Def.SystemLogFolder)))
            {
                Trace.Assert(false, "CreateFilePath( " + Def.GetAbsPathName(Def.MachineLogFolder) + " ) fail.");
                return false;
            }

            Def.SetFileInfo(mcLogFilePath, mcLogFileSize, mcLogFileStorageLife);

            #endregion

            #region // 检查数据库表

            for (TableType tab = TableType.TABLE_USER; tab < TableType.TABLE_END; tab++)
            {
                if (!this.dbRecord.CheckTable(tab) && !this.dbRecord.CreateTable(tab))
                {
                    Trace.Assert(false, "DataBaseRecord." + tab + "表不存在，请检查");
                    return false;
                }
            }
            #endregion

            #region // 打开Log数据库

            if (!DataBaseLog.OpenDataBase(Def.GetAbsPathName(Def.MachineMdb), ""))
            {
                ShowMsgBox.ShowDialog("Log数据库打开失败，继续操作将不能保存Log信息", MessageType.MsgAlarm);
            }
            for (DataBaseLog.LogTableType tab = 0; tab < DataBaseLog.LogTableType.End; tab++)
            {
                if (!DataBaseLog.CheckTable(tab) && !DataBaseLog.CreateTable(tab))
                {
                    Trace.Assert(false, "DataBaseLog." + tab + "表不存在，请检查");
                    return false;
                }
            }
            #endregion

            #region // MES配置参数，资源班次信息

            for (MesInterface mes = 0; mes < MesInterface.End; mes++)
            {
                MesDefine.ReadConfig(mes);
            }
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "MesResources";
            key = $"Equipment.OperatorUserID";
            IniFile.WriteString(section, key, "", file);
            key = $"Equipment.OperatorPassword";
            IniFile.WriteString(section, key, "", file);
            MesResources.ReadConfig();
            OperationShifts.ReadConfig();
            FTPDefine.ReadConfig();

            #endregion

            #region // 打开MES的MySql服务

            if (!MesOperateMySql.OpenMesMySql())
            {
                return false;
            }

            if (!SQLServerBakingIn.Open())
            {
                return false;
            }
            #endregion

            #region // 创建模组

            IniFile.WriteString("Module0", "Name", this.MachineName, Def.GetAbsPathName(Def.ModuleCfg));

            string strSection, strKey, strClass;
            strSection = strKey = strClass = "";
            RunProcess runModule = null;
            Dictionary<int, string> checkRunID = new Dictionary<int, string>();
            for (int index = 0; index < int.MaxValue; index++)
            {
                strKey = "Module" + index;
                strSection = IniFile.ReadString("Modules", strKey, "", Def.GetAbsPathName(Def.ModuleExCfg));
                if (string.IsNullOrEmpty(strSection))
                {
                    break;
                }
                strClass = IniFile.ReadString(strSection, "Class", "", Def.GetAbsPathName(Def.ModuleExCfg));
                int runID = index;

                #region // 上料
                if ("OnloadRobot" == strClass)
                {
                    runID = (int)RunID.OnloadRobot;
                    runModule = new RunProcessOnloadRobot(runID);
                }
                else if ("OnloadLine" == strClass)
                {
                    runID = (int)RunID.OnloadLine;
                    runModule = new RunProcessOnloadLine(runID);
                }
                else if ("OnloadScan" == strClass)
                {
                    runID = (int)RunID.OnloadScan;
                    runModule = new RunProcessOnloadScan(runID);
                }
                else if ("OnloadNG" == strClass)
                {
                    runID = (int)RunID.OnloadNG;
                    runModule = new RunProcessOnloadNG(runID);
                }
                else if ("OnloadFake" == strClass)
                {
                    runID = (int)RunID.OnloadFake;
                    runModule = new RunProcessOnloadFake(runID);
                }
                else if ("OnloadDetect" == strClass)
                {
                    runID = (int)RunID.OnloadDetect;
                    runModule = new RunProcessDetectFake(runID);
                }
                #endregion

                #region // 人工台、暂存架
                else if ("ManualOperate" == strClass)
                {
                    runID = (int)RunID.ManualOperate;
                    runModule = new RunProcessManualOperate(runID);
                }
                else if ("PalletBuffer" == strClass)
                {
                    runID = (int)RunID.PalletBuffer;
                    runModule = new RunProcessPalletBuffer(runID);
                }
                #endregion

                #region // 调度
                else if ("RobotTransfer" == strClass)
                {
                    runID = (int)RunID.Transfer;
                    runModule = new RunProcessRobotTransfer(runID);
                }
                #endregion

                #region // 下料
                else if ("OffloadBattery" == strClass)
                {
                    runID = (int)RunID.OffloadBattery;
                    runModule = new RunProcessOffloadBattery(runID);
                }
                //else if ("OffloadNG" == strClass)
                //{
                //    runID = (int)RunID.OffloadNG;
                //    runModule = new RunProcessOffloadNG(runID);
                //}
                else if ("OffloadDetect" == strClass)
                {
                    runID = (int)RunID.OffloadDetect;
                    runModule = new RunProcessDetectFake(runID);
                }
                else if ("OffloadLine" == strClass)
                {
                    runID = (int)RunID.OffloadLine;
                    runModule = new RunProcessOffloadLine(runID);
                }
                else if ("CoolingSystem" == strClass)
                {
                    runID = (int)RunID.CoolingSystem;
                    runModule = new RunProcessCoolingSystem(runID);
                }
                else if ("CoolingOffload" == strClass)
                {
                    runID = (int)RunID.CoolingOffload;
                    runModule = new RunProcessCoolingOffload(runID);
                }
                else if ("OffloadBuffer" == strClass)
                {
                    int id = Int32.Parse(strSection.Remove(0, strClass.Length)) - 1;
                    runID = (int)RunID.OffloadBuffer + id;
                    runModule = new RunProcessOffloadBuffer(runID);
                }
                else if ("OffloadOut" == strClass)
                {
                    runID = (int)RunID.OffloadOut;
                    runModule = new RunProcessOffloadOut(runID);
                }
                #endregion

                #region // 干燥炉
                else if ("DryingOven" == strClass)
                {
                    int id = IniFile.ReadInt(strSection, "DryingOvenID", 0, Def.GetAbsPathName(Def.ModuleExCfg));
                    runID = (int)RunID.DryOven0 + id;

                    Trace.WriteLine($"strClass={strClass} -> DryingOvenID={id} -> runID={runID}");

                    runModule = new RunProcessDryingOven(runID);
                }
                #endregion

                #region // 安全门
                else if ("SafeDoor" == strClass)
                {
                    runID = (int)RunID.SafeDoor;
                    runModule = new RunProcessSafeDoor(runID);
                }

                #endregion // 安全门

                else
                {
                    runModule = new RunProcess(runID);
                }
                if (!checkRunID.ContainsKey(runID))
                {
                    checkRunID.Add(runID, strSection);
                }
                else
                {
                    ShowMsgBox.ShowDialog((strSection + "模组RunID = " + runID + "已存在，请检查！"), MessageType.MsgAlarm);
                    return false;
                }

                ListRuns.Add(runModule);
                List<int> inputs, outputs, motors;
                runModule.AlarmStopMC(this.AlarmStopMC);
                runModule.InitializeConfig(strSection);
                runModule.GetHardwareConfig(out inputs, out outputs, out motors);
                WriteModuleCfg(index + 1, strSection, inputs, outputs, motors);
            }
            IniFile.WriteInt("Modules", "CountModules", ListRuns.Count + 1, Def.GetAbsPathName(Def.ModuleCfg));
            #endregion

            #region // 创建模组完成后，读取该模组的关联模组
            foreach (RunProcess run in this.ListRuns)
            {
                // 有硬件运行时不能空运行
                if (!Def.IsNoHardware())
                {
                    run.DryRun = false;
                }
                run.ReadRelatedModule();
            }
            #endregion

            #region // 创建RunCtrl

            this.RunsCtrl = new RunCtrl();
            if (null == this.RunsCtrl)
            {
                ShowMsgBox.ShowDialog("创建RunCtrl线程失败", MessageType.MsgAlarm);
                return false;
            }

            if (!this.RunsCtrl.Initialize(ListRuns.Count, (this.ListRuns.ConvertAll<RunEx>(tmp => tmp as RunEx)), (new ManualDebugCheck(this.ListRuns.Count)), hMsgWnd))
            {
                ShowMsgBox.ShowDialog("RunCtrl线程初始化失败", MessageType.MsgAlarm);
                return false;
            }
            // 设置启动前检查，及停止后操作委托
            this.RunsCtrl.beforeStart = BeforeStart;
            this.RunsCtrl.afterStop = AfterStop;
            #endregion

            #region// MES连接
            wcf_Client.Connect(MesResources.Equipment.EquipmentCode, MesResources.Equipment.MesURL);
            string msg = "";

            //for (int i = 0; i < 3; i++)
            //{
            //    //设备联机请求
            //    if (!ACEQPTCONN_Main(MesResources.Equipment, ref msg))
            //    {
            //        ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            //        //return false;
            //    }
            //    else
            //    {
            //        break;
            //    }
            //}


            #endregion
            #region // 读取系统IO，统计数据

            ReadSystemIO();

            // 设备IO读取完成后不再使用，删除输入输出及电机列表
            listInput.Clear();
            listInput = null;
            listOutput.Clear();
            listOutput = null;
            listMotor.Clear();
            listMotor = null;

            // 统计数据
            TotalData.ReadTotalData();

            #endregion

            #region // 模组服务器及客户端

            strKey = IniFile.ReadString(this.MachineName, "machineServerIP", "", Def.GetAbsPathName(Def.ModuleExCfg));
            if (!string.IsNullOrEmpty(strKey))
            {
                this.machineServerIP = strKey;
                this.machineServerPort = IniFile.ReadInt(this.MachineName, "machineServerPort", 5001, Def.GetAbsPathName(Def.ModuleExCfg));
                this.machineServer = new ModuleServer();

                foreach (RunProcess run in this.ListRuns)
                {
                    switch ((RunID)run.GetRunID())
                    {
                        case RunID.OnloadRobot:
                        case RunID.Transfer:
                        case RunID.ManualOperate:
                        case RunID.PalletBuffer:
                        case RunID.OffloadBattery:
                        case RunID.CoolingSystem:
                            {
                                this.machineServer.AddServerData((RunID)run.GetRunID());
                                break;
                            }
                        default:
                            {
                                if ((RunID.DryOven0 <= (RunID)run.GetRunID()) && ((RunID)run.GetRunID() < RunID.DryOvenALL))
                                {
                                    this.machineServer.AddServerData((RunID)run.GetRunID(), true);
                                }
                                break;
                            }
                    }
                }
                CreateServer();
            }
            this.machineClientIP = new List<string>();
            this.machineClientPort = new List<int>();
            this.machineClient = new List<ModuleClient>();
            int idx = 0;
            do
            {
                strKey = IniFile.ReadString(this.MachineName, $"machineClientIP{idx}", "", Def.GetAbsPathName(Def.ModuleExCfg));
                if (!string.IsNullOrEmpty(strKey))
                {
                    this.machineClientIP.Add(strKey);
                    this.machineClientPort.Add(IniFile.ReadInt(this.MachineName, $"machineClientPort{idx}", 5001 + idx, Def.GetAbsPathName(Def.ModuleExCfg)));
                    this.machineClient.Add(new ModuleClient());
                }
                idx++;
            } while (!string.IsNullOrEmpty(strKey));

            #endregion

            #region // 监视线程

            if (!InitThread())
            {
                return false;
            }
            #endregion

            return true;
        }

       

        #endregion

        #region // 报警弹窗

        /// <summary>
        /// 非模态弹窗：非阻塞弹出线程会继续向下执行，一般弹出在Messsage.cfg中配置的报警
        /// </summary>
        /// <param name="msgID">报警ID</param>
        /// <param name="addMsg">附加报警内容</param>
        /// <param name="countdownTime">倒计时时间</param>
        /// <param name="countdownDlgBtn">倒计时完默认按钮</param>
        private async void ShowMessageID(int msgID, string[] addMsg = null, int countdownTime = 0, DialogResult countdownDlgBtn = DialogResult.None)
        {
            try
            {
                #region // 查找是否已有当前报警ID

                if (null != this.MsgList)
                {
                    if (this.MsgList.Contains(msgID))
                    {
                        return; // 已弹窗则跳过
                    }
                }
                // 保存报警ID
                this.MsgList.Add(msgID);

                #endregion

                int msgType;
                bool showDlg;
                string section, msg, msgDisp;

                #region // 读报警ID的内容
                section = string.Format("M{0:D4}", msgID);
                msg = IniFile.ReadString(section, "Name", (section + "未配置报警"), Def.GetAbsPathName(SysDef.MessageCfg));
                msgDisp = IniFile.ReadString(section, "Dispose", "", Def.GetAbsPathName(SysDef.MessageCfg));
                msgType = IniFile.ReadInt(section, "Type", 0, Def.GetAbsPathName(SysDef.MessageCfg));
                showDlg = IniFile.ReadBool(section, "ShowDialog", true, Def.GetAbsPathName(SysDef.MessageCfg));
                #endregion

                #region // 判断报警是否整机停机
                if (MessageType.MsgAlarm == (MessageType)msgType)
                {
                    if (this.AlarmStopMC)
                    {
                        RunsCtrl.Stop();
                    }
                }
                #endregion

                // 后续开始异步执行
                await System.Threading.Tasks.Task.Delay(1);

                #region // 替换内容
                int index = 0;
                string key, value;
                while (true)
                {
                    key = value = "#" + index + "#";
                    if (!msg.Contains(key))
                    {
                        break;
                    }
                    else
                    {
                        if ((null != addMsg) && (addMsg.Length > index))
                        {
                            value = addMsg[index];
                            addMsg[index] = "";
                        }
                    }
                    msg = msg.Replace(key, value);
                    index++;
                }

                if (null != addMsg)
                {
                    for (int i = 0; i < addMsg.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(addMsg[i]))
                        {
                            msg += addMsg[i];
                        }
                    }
                }
                #endregion

                #region // 构造报警内容
                string msgData = $"{MachineName}\r\n报警[{msgID:D4}]：{msg}\r\n处理方法：{msgDisp}";
                this.hasMsgBox = true; // 保存报警信息
                this.dbRecord.AddAlarmInfo(new AlarmFormula(Def.GetProductFormula(), msgID, msgData, msgType, (int)RunID.Invalid, MachineName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                #endregion

                #region // 弹窗报警
                if (showDlg)
                {
                    if (countdownTime > 0)
                    {
                        ShowMsgBox.ShowDialog(msgData, (MessageType)msgType, countdownTime, countdownDlgBtn);
                    }
                    else
                    {
                        ShowMsgBox.ShowDialog(msgData, (MessageType)msgType);
                    }
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MachineCtrl", $"ShowMessageID(string[]) {ex.Message}\r\n{ex.StackTrace}", LogType.Error);
            }
        }

        /// <summary>
        /// 非模态弹窗：非阻塞弹出线程会继续向下执行，弹出自定义报警内容与报警处理方法
        /// </summary>
        /// <param name="msgID">报警ID</param>
        /// <param name="msg">报警内容</param>
        /// <param name="msgDispose">报警处理方法</param>
        /// <param name="msgType">报警类型</param>
        /// <param name="countdownTime">倒计时时间</param>
        /// <param name="countdownDlgBtn">倒计时完默认按钮：OK或YES/NO</param>
        /// <returns>用户响应的弹窗按钮：OK或YES/NO</returns>
        private async void ShowMessageID(int msgID, string msg, string msgDispose, MessageType msgType, int countdownTime = 0, DialogResult countdownDlgBtn = DialogResult.None)
        {
            try
            {
                #region // 判断报警是否整机停机
                if (MessageType.MsgAlarm == msgType)
                {
                    if (this.AlarmStopMC)
                    {
                        RunsCtrl.Stop();
                    }
                }
                #endregion

                // 后续开始异步执行
                await System.Threading.Tasks.Task.Delay(1);

                #region // 查找是否已有当前报警ID

                if (null != this.MsgList)
                {
                    if (this.MsgList.Contains(msgID))
                    {
                        return; // 已弹窗则跳过
                    }
                }
                // 保存报警ID
                this.MsgList.Add(msgID);

                #endregion

                #region // 构造报警内容
                string msgData = $"{MachineName}\r\n报警[{msgID:D4}]：{msg}\r\n处理方法：{msgDispose}";
                this.hasMsgBox = true; // 保存报警信息
                this.dbRecord.AddAlarmInfo(new AlarmFormula(Def.GetProductFormula(), msgID, msgData, (int)msgType, (int)RunID.Invalid, MachineName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                #endregion

                #region // 弹窗报警
                if (countdownTime > 0)
                {
                    ShowMsgBox.ShowDialog(msgData, msgType, countdownTime, countdownDlgBtn);
                }
                else
                {
                    ShowMsgBox.ShowDialog(msgData, msgType);
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MachineCtrl", $"ShowMessageID(string) {ex.Message}\r\n{ex.StackTrace}", LogType.Error);
            }
        }

        #endregion

        #region // 系统的配置及参数

        /// <summary>
        /// 保存模组配置
        /// </summary>
        /// <param name="index"></param>
        /// <param name="moduleName"></param>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        /// <param name="motors"></param>
        private void WriteModuleCfg(int index, string moduleName, List<int> inputs, List<int> outputs, List<int> motors)
        {
            string section = "Module" + index;
            string path = Def.GetAbsPathName(Def.ModuleCfg);

            // 模组名
            IniFile.WriteString(section, "Name", moduleName, path);

            // 输入
            int count = inputs.Count;
            IniFile.WriteInt(section, "InputCount", count, path);
            for (int i = 0; i < count; i++)
            {
                IniFile.WriteInt(section, ("Input" + i), inputs[i], path);
            }
            // 输出
            count = outputs.Count;
            IniFile.WriteInt(section, "OutputCount", count, path);
            for (int i = 0; i < count; i++)
            {
                IniFile.WriteInt(section, ("Output" + i), outputs[i], path);
            }
            // 电机
            count = motors.Count;
            IniFile.WriteInt(section, "MotorCount", count, path);
            for (int i = 0; i < count; i++)
            {
                IniFile.WriteInt(section, ("Motor" + i), motors[i], path);
            }
        }

        /// <summary>
        /// 添加模组参数
        /// </summary>
        /// <param name="key">属性关键字</param>
        /// <param name="name">显示名称</param>
        /// <param name="description">描述</param>
        /// <param name="value">属性值</param>
        /// <param name="paraLevel">属性权限</param>
        /// <param name="readOnly">属性仅可读</param>
        /// <param name="visible">属性可见性</param>
        protected void InsertVoidParameter(string key, string name, string description, object value, RecordType paraType, ParameterLevel paraLevel = ParameterLevel.PL_STOP_MAIN)
        {
            this.insertParameterList.Add(key, new ParameterFormula(Def.GetProductFormula(), this.MachineName, name, key, value.ToString(), paraType, paraLevel));
            this.parameterProperty.Add("系统参数", key, name, description, value, (int)paraLevel, false, true);
        }

        /// <summary>
        /// 获取参数列表
        /// </summary>
        /// <returns></returns>
        public PropertyManage GetParameterList()
        {
            ReadParameter();
            PropertyManage pm = this.parameterProperty;
            foreach (Property item in this.parameterProperty)
            {
                if (null != pm[item.Name])
                {
                    if (item.Value is int)
                    {
                        pm[item.Name].Value = Convert.ToInt32(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if (item.Value is uint)
                    {
                        pm[item.Name].Value = Convert.ToUInt32(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if (item.Value is short)
                    {
                        pm[item.Name].Value = Convert.ToInt16(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if (item.Value is bool)
                    {
                        pm[item.Name].Value = Convert.ToBoolean(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if (item.Value is float)
                    {
                        pm[item.Name].Value = Convert.ToSingle(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if (item.Value is double)
                    {
                        pm[item.Name].Value = Convert.ToDouble(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else if (item.Value is string)
                    {
                        pm[item.Name].Value = Convert.ToString(GetParameterValue(item.Name, pm[item.Name].Value));
                    }
                    else
                    {
                        string msg = string.Format("{0}】为{1}类型，未找到相匹配类型，无法获取参数值", item.DisplayName, item.Value.GetType().ToString());
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                    }
                }
            }
            return pm;
        }

        /// <summary>
        /// 修改参数时检查是否可修改
        /// </summary>
        public virtual bool CheckParameter(string name, object value)
        {
            return true;
        }

        /// <summary>
        /// 读系统设置参数
        /// </summary>
        public void ReadParameter()
        {
            #region // 从数据库读取参数

            List<ParameterFormula> listPara = new List<ParameterFormula>();
            this.dbRecord.GetParameterList(Def.GetProductFormula(), this.MachineName, ref listPara);
            foreach (var item in listPara)
            {
                if (this.dataBaseParameterList.ContainsKey(item.key))
                {
                    this.dataBaseParameterList[item.key] = item;
                }
                else
                {
                    this.dataBaseParameterList.Add(item.key, item);
                }
            }
            #endregion

            int maxRow = Convert.ToInt32(GetParameterValue("PalletMaxRow", (int)PalletRowCol.MaxRow));
            int maxCol = Convert.ToInt32(GetParameterValue("PalletMaxCol", (int)PalletRowCol.MaxCol));
            if ((maxRow != this.PalletMaxRow) || (maxCol != this.PalletMaxCol))
            {
                for (int i = 0; i < this.ListRuns.Count; i++)
                {
                    for (int plt = 0; plt < this.ListRuns[i].Pallet.Length; plt++)
                    {
                        this.ListRuns[i].Pallet[plt].SetRowCol(maxRow, maxCol);
                    }
                }
                this.PalletMaxRow = maxRow;
                this.PalletMaxCol = maxCol;
            }
            this.BakingMaxCount = Convert.ToInt32(GetParameterValue("BakingMaxCount", this.BakingMaxCount));
            //this.mcLogFilePath = Def.GetAbsPathName(Def.MachineLogFolder);
            this.mcLogFileSize = Convert.ToInt32(GetParameterValue("mcLogFileSize", 2));
            this.mcLogFileStorageLife = Convert.ToInt32(GetParameterValue("mcLogFileStorageLife", 7));
            this.ProductionFilePath = Convert.ToString(GetParameterValue("ProductionFilePath", @"D:\生产信息"));
            this.productionFileStorageLife = Convert.ToInt32(GetParameterValue("productionFileStorageLife", 30));
            this.safeDoorEnabled = Convert.ToBoolean(GetParameterValue("safeDoorEnabled", false));
            this.autoOnLoadBattery = Convert.ToBoolean(GetParameterValue("OnLoadBatteryType", autoOnLoadBattery));
        }

        /// <summary>
        /// 保存设置参数
        /// </summary>
        public void WriteParameter(string key, string value)
        {
            if (this.insertParameterList.ContainsKey(key))
            {
                ParameterFormula insertPara, dbPara;
                insertPara = this.insertParameterList[key];
                insertPara.module = this.MachineName;
                insertPara.value = value;
                if (this.dataBaseParameterList.ContainsKey(key))
                {
                    dbPara = this.dataBaseParameterList[key];
                    dbPara.value = insertPara.value;
                    dbPara.level = insertPara.level;
                    this.dbRecord.ModifyParameter(dbPara);
                }
                else
                {
                    this.dbRecord.AddParameter(insertPara);
                }
            }
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        private object GetParameterValue(string key, object defaultValue)
        {
            if (this.dataBaseParameterList.ContainsKey(key))
            {
                return this.dataBaseParameterList[key].value;
            }
            return defaultValue;
        }

        /// <summary>
        /// 初始化参数
        /// </summary>
        private void InitParameter()
        {
            this.PalletMaxRow = (int)PalletRowCol.MaxRow;
            this.PalletMaxCol = (int)PalletRowCol.MaxCol;
            this.mcLogFilePath = Def.GetAbsPathName(Def.MachineLogFolder);
            this.mcLogFileSize = 2;
            this.mcLogFileStorageLife = 7;
            this.ProductionFilePath = @"D:\生产信息";
            this.MesFilePath = @"D:\生产信息";
            this.productionFileStorageLife = 30;
            this.BakingMaxCount = 3;
            this.autoOnLoadBattery = true;
        }

        /// <summary>
        /// 读系统IO
        /// </summary>
        private void ReadSystemIO()
        {
            string module, key, path;
            module = this.MachineName;
            path = Def.GetAbsPathName(Def.ModuleExCfg);
            List<int> inputs, outputs, motors;
            inputs = new List<int>();
            outputs = new List<int>();
            motors = new List<int>();

            int maxCount = (int)SystemIO.ButtonIO;
            #region // 输入：按钮

            this.IStartButton = new int[maxCount];
            this.IStopButton = new int[maxCount];
            //this.IEStopButton = new int[maxCount];
            this.IResetButton = new int[maxCount];
            this.IEStopTranButton = new int[maxCount];
            this.IEStopOnloadButton = new int[maxCount];
            this.IEStopOffloadButton = new int[maxCount];
            this.IEStopDoor1Button = new int[maxCount];
            this.IEStopdoor2Button = new int[maxCount];

            for (int idx = 0; idx < maxCount; idx++)
            {
                key = ("IStartButton" + idx);
                this.IStartButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IStartButton[idx] > -1)
                {
                    inputs.Add(this.IStartButton[idx]);
                }
                key = ("IStopButton" + idx);
                this.IStopButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IStopButton[idx] > -1)
                {
                    inputs.Add(this.IStopButton[idx]);
                }
                key = ("IResetButton" + idx);
                this.IResetButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IResetButton[idx] > -1)
                {
                    inputs.Add(this.IResetButton[idx]);
                }
                key = ("IEStopTranButton" + idx);
                this.IEStopTranButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IEStopTranButton[idx] > -1)
                {
                    inputs.Add(this.IEStopTranButton[idx]);
                }
                key = ("IEStopOnloadButton" + idx);
                this.IEStopOnloadButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IEStopOnloadButton[idx] > -1)
                {
                    inputs.Add(this.IEStopOnloadButton[idx]);
                }
                key = ("IEStopOffloadButton" + idx);
                this.IEStopOffloadButton[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IEStopOffloadButton[idx] > -1)
                {
                    inputs.Add(this.IEStopOffloadButton[idx]);
                }
                key = ("IEStopDoor1Button" + idx);
                this.IEStopDoor1Button[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IEStopDoor1Button[idx] > -1)
                {
                    inputs.Add(this.IEStopDoor1Button[idx]);
                }
                key = ("IEStopdoor2Button" + idx);
                this.IEStopdoor2Button[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.IEStopdoor2Button[idx] > -1)
                {
                    inputs.Add(this.IEStopdoor2Button[idx]);
                }

            }
            #endregion

            #region // 输出：按钮LED

            this.OStartLed = new int[maxCount];
            this.OStopLed = new int[maxCount];
            this.OResetLed = new int[maxCount];
            for (int idx = 0; idx < maxCount; idx++)
            {
                key = ("OStartLed" + idx);
                this.OStartLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OStartLed[idx] > -1)
                {
                    outputs.Add(this.OStartLed[idx]);
                }
                key = ("OStopLed" + idx);
                this.OStopLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OStopLed[idx] > -1)
                {
                    outputs.Add(this.OStopLed[idx]);
                }
                key = ("OResetLed" + idx);
                this.OResetLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OResetLed[idx] > -1)
                {
                    outputs.Add(this.OResetLed[idx]);
                }
            }
            #endregion

            maxCount = (int)SystemIO.SafeDoorIO;
            #region // 输入：安全门

            this.SafeDoorState = new int[maxCount];
            //this.ISafeDoorOpenBtn = new int[maxCount];
            //this.ISafeDoorCloseBtn = new int[maxCount];
            //this.SafeDoorStopModule = new string[maxCount];
            //this.SafeDoorDelay = new int[maxCount];
            for (int idx = 0; idx < maxCount; idx++)
            {
                this.SafeDoorState[idx] = -1;
                key = ("ISafeDoorState" + (idx + 1));
                this.SafeDoorState[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                if (this.SafeDoorState[idx] > -1)
                {
                    inputs.Add(this.SafeDoorState[idx]);

                    //key = ("SafeDoorStopModule" + idx);
                    //this.SafeDoorStopModule[idx] = IniFile.ReadString(module, key, "", path);
                    //key = ("SafeDoorDelay" + idx);
                    //this.SafeDoorDelay[idx] = IniFile.ReadInt(module, key, 500, path);
                    //}
                    //    key = ("ISafeDoorOpenBtn" + idx);
                    //    this.ISafeDoorOpenBtn[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                    //    if(this.ISafeDoorOpenBtn[idx] > -1)
                    //    {
                    //        inputs.Add(this.ISafeDoorOpenBtn[idx]);
                    //    }
                    //    key = ("ISafeDoorCloseBtn" + idx);
                    //    this.ISafeDoorCloseBtn[idx] = DecodeInputID(IniFile.ReadString(module, key, "", path));
                    //    if(this.ISafeDoorCloseBtn[idx] > -1)
                    //    {
                    //        inputs.Add(this.ISafeDoorCloseBtn[idx]);
                    //    }
                }
            }
            #endregion

            #region // 输出：安全门

            //this.OSafeDoorOpenLed = new int[maxCount];
            //this.OSafeDoorCloseLed = new int[maxCount];
            //this.OSafeDoorUnlock = new int[maxCount];
            //for(int idx = 0; idx < maxCount; idx++)
            //{
            //    key = ("OSafeDoorOpenLed" + idx);
            //    this.OSafeDoorOpenLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
            //    if(this.OSafeDoorOpenLed[idx] > -1)
            //    {
            //        outputs.Add(this.OSafeDoorOpenLed[idx]);
            //    }
            //    key = ("OSafeDoorCloseLed" + idx);
            //    this.OSafeDoorCloseLed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
            //    if(this.OSafeDoorCloseLed[idx] > -1)
            //    {
            //        outputs.Add(this.OSafeDoorCloseLed[idx]);
            //    }
            //    key = ("OSafeDoorUnlock" + idx);
            //    this.OSafeDoorUnlock[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
            //    if(this.OSafeDoorUnlock[idx] > -1)
            //    {
            //        outputs.Add(this.OSafeDoorUnlock[idx]);
            //    }
            //}
            #endregion

            maxCount = (int)SystemIO.TowerIO;
            #region // 输出：灯塔

            this.OLightTowerRed = new int[maxCount];
            this.OLightTowerYellow = new int[maxCount];
            this.OLightTowerGreen = new int[maxCount];
            this.OLightTowerBuzzer = new int[maxCount];
            for (int idx = 0; idx < maxCount; idx++)
            {
                key = ("OLightTowerRed" + idx);
                this.OLightTowerRed[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OLightTowerRed[idx] > -1)
                {
                    outputs.Add(this.OLightTowerRed[idx]);
                }
                key = ("OLightTowerYellow" + idx);
                this.OLightTowerYellow[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OLightTowerYellow[idx] > -1)
                {
                    outputs.Add(this.OLightTowerYellow[idx]);
                }
                key = ("OLightTowerGreen" + idx);
                this.OLightTowerGreen[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OLightTowerGreen[idx] > -1)
                {
                    outputs.Add(this.OLightTowerGreen[idx]);
                }
                key = ("OLightTowerBuzzer" + idx);
                this.OLightTowerBuzzer[idx] = DecodeOutputID(IniFile.ReadString(module, key, "", path));
                if (this.OLightTowerBuzzer[idx] > -1)
                {
                    outputs.Add(this.OLightTowerBuzzer[idx]);
                }
            }
            #endregion

            #region // 气压报警

            this.IAirPressureAlarm = DecodeInputID(IniFile.ReadString(module, "IAirPressureAlarm", "", path));
            if (this.IAirPressureAlarm > -1)
            {
                inputs.Add(this.IAirPressureAlarm);
            }
            #endregion

            WriteModuleCfg(0, module, inputs, outputs, motors);
        }

        /// <summary>
        /// 加载指定电机的点位
        /// </summary>
        /// <param name="motorID"></param>
        /// <returns></returns>
        internal bool LoadMotorLocation(int motorID)
        {
            List<MotorFormula> motorlist = new List<MotorFormula>();
            if (this.dbRecord.GetMotorPosList(Def.GetProductFormula(), motorID, ref motorlist))
            {
                DeviceManager.GetMotorManager().LstMotors[motorID].DeleteAllLoc();
                motorlist.Sort(delegate (MotorFormula left, MotorFormula right)
                { return left.posID - right.posID; });
                foreach (var item in motorlist)
                {
                    if ((int)MotorCode.MotorOK != DeviceManager.GetMotorManager().LstMotors[motorID].AddLocation(item.posID, item.posName, item.posValue))
                    {
                        Def.WriteLog("MachineCtrl", $"M{motorID}电机{item.posID}】{item.posName}点位添加失败", LogType.Error);
                        return false;
                    }
                }
                return true;
            }
            else
            {
                string msg = $"M{motorID}电机点位获取错误，请检查！\r\n确认后软件将会退出";
                //ShowMsgBox.ShowDialog(msg, MessageType.MsgAlarm);
                Def.WriteLog("MachineCtrl", msg, LogType.Error);
            }
            return false;
        }

        #endregion

        #region // 系统的运行数据

        #endregion

        #region // 设备运行检查

        /// <summary>
        /// 设备复位，清除报警等信息
        /// </summary>
        public void MachineReset()
        {
            this.MsgList.Clear();

            this.hasMsgBox = false;
            this.RunsCtrl.Reset();
        }

        /// <summary>
        /// 设备启动前检查是否能启动
        /// </summary>
        /// <returns></returns>
        protected bool BeforeStart()
        {
            if (MCState.MCRunning == RunsCtrl.GetMCState())
            {
                return false;
            }
            if (this.UpdataMes)
            {
                if (MachineCtrl.GetInstance().dbRecord.UserLevel() > UserLevelType.USER_OPERATOR)
                {
                    ShowMsgBox.ShowDialog("权限不足，不能启动软件！", MessageType.MsgWarning);
                    return false;
                }
                ////2 - 17 注释
                //if (/*!MesResources.MesLogin && */!string.IsNullOrEmpty(MesResources.Equipment.OperatorUserID))
                //{
                //    if (MesUserLogin())
                //    {
                //        MesRecipeListGet(); //这里不获取
                //        MesRecipeGet(MesResources.Equipment.MesRecipeCode);
                //        MesRecipeVExamine(MesResources.Equipment.MesRecipeCode);
                //    }
                //}
            }
            //if (!ClientIsConnect()/* && !ConnectClient()*/)
            //{
            //    ShowMsgBox.ShowDialog("请等候模组服务连接后再启动软件...", MessageType.MsgWarning);
            //    return false;
            //}
            if (CheckSafeDoorState())
            {
                ShowMsgBox.ShowDialog("安全门已经打开，不能启动软件！", MessageType.MsgWarning);
                return false;
            }
            if (EStopButtonOn())
            {
                ShowMsgBox.ShowDialog("急停或停止按钮被按下，不能启动软件！", MessageType.MsgWarning);
                return false;
            }
            if (!this.UpdataMes && !Def.IsNoHardware())
            {
                string msg = string.Format("【离线生产】将不能上传MES，是否继续！");
                if (DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                {
                    return false;
                }
            }
            //if (this.UpdataMes && (DateTime.Now - this.McStopTime).TotalSeconds > MesResources.StopUpdataInterval)
            //{
            //    StopReasonPage page = new StopReasonPage();
            //    if (DialogResult.OK != page.ShowDialog())
            //    {
            //        return false;
            //    }
            //    if (!GetInstance().MesRecipeVExamine(MesResources.Equipment.MesRecipeCode))
            //    {
            //        return false;
            //    }
            //}
            if (this.UpdataMes)
             {
                //if (Framework.Mes.Jeve_Mes.Mes_GetRunOpList())
                //{

                //}
                if (!MesResources.MesLogin || string.IsNullOrEmpty(this.OperaterID))
                {
                    MesUserLogin();
                    return false;
                }

                //if (!StartUpdataDevice())
                //{
                //    return false;
                //}
            }
            this.mcStopTimeReset = true;
            return true;
        }

        /// <summary>
        /// 设备停止后进行的操作
        /// </summary>
        protected void AfterStop()
        {
            foreach (var item in this.ListRuns)
            {
                item.AfterStopAction();
            }
            if (this.mcStopTimeReset)
            {
                this.mcStopTimeReset = false;
                this.McStopTime = DateTime.Now;
            }
        }

        #endregion

        #region // 模组数据

        /// <summary>
        /// 获取指定RunModule模组名的模组
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public RunProcess GetModule(string runModule)
        {
            foreach (RunProcess run in this.ListRuns)
            {
                if ((null != run) && (runModule == run.RunModule))
                {
                    return run;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取指定RunModule模组名的模组
        /// </summary>
        /// <param name="moduleName"></param>
        /// <returns></returns>
        public RunProcess GetModule(RunID runID)
        {
            foreach (RunProcess run in this.ListRuns)
            {
                if ((null != run) && ((int)runID == run.GetRunID()))
                {
                    return run;
                }
            }
            return null;
        }

        /// <summary>
        /// 保存模组通讯数据
        /// </summary>
        /// <param name="socketData"></param>
        public void SetModuleSocketData(ModuleSocketData socketData)
        {
            if (this.moduleSocketData.ContainsKey(socketData.machineID))
            {
                this.moduleSocketData[socketData.machineID] = socketData;
            }
            else
            {
                this.moduleSocketData.Add(socketData.machineID, socketData);
            }
            switch (socketData.machineID)
            {
                case 0:
                    TotalData.OnloadCount = socketData.onloadCount;
                    TotalData.OnScanNGCount = socketData.onScanNGCount;
                    TotalData.OnScan1NGCount = socketData.onScan1NGCount;
                    TotalData.OnScan2NGCount = socketData.onScan2NGCount;
                    TotalData.OnScan3NGCount = socketData.onScan3NGCount;
                    TotalData.OnScan4NGCount = socketData.onScan4NGCount;
                    break;
                case 2:
                    TotalData.OffloadCount = socketData.offloadCount;
                    TotalData.BakedNGCount = socketData.bakedNGCount;
                    break;
            }
        }

        /// <summary>
        /// 获取模组通讯数据
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public ModuleSocketData GetModuleSocketData(RunID runId)
        {
            ModuleSocketData[] socketData = this.moduleSocketData.Values.ToArray();
            foreach (var item in socketData)
            {
                if (item.moduleEnable.ContainsKey(runId))
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取指定模组所在设备的设备状态
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public MCState GetModuleMCState(RunID runId)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if (null != run)
            {
                return RunsCtrl.GetMCState();
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return (MCState)socketData.machineState;
                }
            }
            return MCState.MCInvalidState;
        }

        /// <summary>
        /// 获取模组使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public bool GetModuleEnable(RunID runId)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if (null != run)
            {
                return run.IsModuleEnable();
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.moduleEnable[runId];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取模组运行状态
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public bool GetModuleRunning(RunID runId)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if (null != run)
            {
                return run.IsRunning();
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.moduleRunning[runId];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉/机器人连接状态：true连接，false断开
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public bool GetDeviceIsConnect(RunID runId)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if (null != run)
            {
                if (run is RunProcessDryingOven)
                {
                    return ((RunProcessDryingOven)run).DryOvenIsConnect();
                }
                else if (run is RunProcessOnloadRobot)
                {
                    return ((RunProcessOnloadRobot)run).RobotIsConnect();
                }
                else if (run is RunProcessRobotTransfer)
                {
                    return ((RunProcessRobotTransfer)run).RobotIsConnect();
                }
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.deviceIsConnect[runId];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取所有干燥炉的运行时间：[干燥炉，炉腔]
        /// </summary>
        /// <returns></returns>
        public uint[,] GetDryingOvenWorkTime()
        {
            bool result = false;
            uint[,] workTime = new uint[(int)OvenInfoCount.OvenCount, (int)OvenRowCol.MaxRow];

            bool IsChange = false;
            if (IsChange)
            {
                #region //匹配炉层编号 wjj 220507
                int idx = 0;
                for (int id = 0; id < (int)OvenInfoCount.OvenCount; id++)
                {
                    idx = id;
                    if (id < 5)
                    {
                        idx += 8;
                    }
                    else
                    {
                        idx -= 5;
                    }
                    RunProcessDryingOven run = GetModule(idx + RunID.DryOven0) as RunProcessDryingOven;
                    if (null != run)
                    {
                        for (int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                        {
                            if ((CavityStatus.Heating == run.CavityState[row])
                                || (CavityStatus.WaitDetect == run.CavityState[row]))
                            {
                                workTime[id, row] = run.RCavity(row).workTime;
                            }
                            else
                            {
                                workTime[id, row] = 0;
                            }
                        }
                        result = true;
                    }
                }
                #endregion //匹配炉层编号
                if (!result)
                {
                    ModuleSocketData socketData = GetModuleSocketData(RunID.DryOven0);
                    if (null != socketData)
                    {
                        RunID[] runId = socketData.cavityTime.Keys.ToArray();
                        for (int i = 0; i < runId.Length; i++)
                        {
                            uint[] cavityTime = socketData.cavityTime[runId[i]];
                            if (null != cavityTime)
                            {
                                for (int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                                {
                                    if (((int)CavityStatus.Heating == socketData.cavityState[runId[i]][row])
                                        || ((int)CavityStatus.WaitDetect == socketData.cavityState[runId[i]][row]))
                                    {
                                        workTime[(runId[i] - RunID.DryOven0), row] = cavityTime[row];
                                    }
                                    else
                                    {
                                        workTime[(runId[i] - RunID.DryOven0), row] = 0;
                                    }
                                }
                                result = true;
                            }
                        }
                    }
                }
            }
            else
            {
                for (int id = 0; id < (int)OvenInfoCount.OvenCount; id++)
                {
                    RunProcessDryingOven run = GetModule(id + RunID.DryOven0) as RunProcessDryingOven;
                    if (null != run)
                    {
                        for (int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                        {
                            if ((CavityStatus.Heating == run.CavityState[row])
                                || (CavityStatus.WaitDetect == run.CavityState[row]))
                            {
                                workTime[id, row] = run.RCavity(row).workTime;
                            }
                            else
                            {
                                workTime[id, row] = 0;
                            }
                        }
                        result = true;
                    }
                }
                if (!result)
                {
                    ModuleSocketData socketData = GetModuleSocketData(RunID.DryOven0);
                    if (null != socketData)
                    {
                        RunID[] runId = socketData.cavityTime.Keys.ToArray();
                        for (int i = 0; i < runId.Length; i++)
                        {
                            uint[] cavityTime = socketData.cavityTime[runId[i]];
                            if (null != cavityTime)
                            {
                                for (int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                                {
                                    if (((int)CavityStatus.Heating == socketData.cavityState[runId[i]][row])
                                        || ((int)CavityStatus.WaitDetect == socketData.cavityState[runId[i]][row]))
                                    {
                                        workTime[(runId[i] - RunID.DryOven0), row] = cavityTime[row];
                                    }
                                    else
                                    {
                                        workTime[(runId[i] - RunID.DryOven0), row] = 0;
                                    }
                                }
                                result = true;
                            }
                        }
                    }
                }
            }
            return workTime;
        }

        /// <summary>
        /// 获取干燥炉腔体干燥状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        public CavityStatus GetDryingOvenCavityState(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if (null != run)
            {
                return run.CavityState[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return (CavityStatus)socketData.cavityState[runId][cavityIdx];
                }
            }
            return CavityStatus.Unknown;
        }

        /// <summary>
        /// 获取干燥炉腔体使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public bool GetDryingOvenCavityEnable(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if (null != run)
            {
                return run.CavityEnable[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.cavityEnable[runId][cavityIdx];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉腔体保压状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public bool GetDryingOvenCavityPressure(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if (null != run)
            {
                return run.CavityPressure[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.cavityPressure[runId][cavityIdx];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉腔体转移状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public bool GetDryingOvenCavityTransfer(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if (null != run)
            {
                return run.CavityTransfer[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.cavityTransfer[runId][cavityIdx];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉腔体转移接收状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public bool GetDryingOvenCavityTransferRecv(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if (null != run)
            {
                return run.CavityTransferRecv[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.cavityTransferRecv[runId][cavityIdx];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取干燥炉腔体抽检周期
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public int GetDryingOvenCavitySamplingCycle(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if (null != run)
            {
                return run.CavitySamplingCycle[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.cavitySamplingCycle[runId][cavityIdx];
                }
            }
            return 1;
        }

        /// <summary>
        /// 获取干燥炉腔体加热次数
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx">腔体索引</param>
        /// <returns></returns>
        public int GetDryingOvenCavityHeartCycle(RunID runId, int cavityIdx)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if (null != run)
            {
                return run.CavityHeartCycle[cavityIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.cavityHeartCycle[runId][cavityIdx];
                }
            }
            return 1;
        }

        /// <summary>
        /// 获取缓存架层使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx">夹具位置索引</param>
        /// <returns></returns>
        public bool GetPalletBufferRowEnable(RunID runId, int rowIdx)
        {
            // 本地
            RunProcessPalletBuffer run = GetInstance().GetModule(runId) as RunProcessPalletBuffer;
            if (null != run)
            {
                return run.BufferEnable[rowIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    if (null != socketData.pltPosEnable)
                    {
                        return socketData.pltPosEnable[runId][rowIdx];
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取上下料夹具位使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx">夹具位置索引</param>
        /// <returns></returns>
        public bool GetPalletPosEnable(RunID runId, int pltIdx)
        {
            // 本地
            RunProcessOnloadRobot run = GetInstance().GetModule(runId) as RunProcessOnloadRobot;
            if (null != run)
            {
                return run.PalletPosEnable[pltIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.pltPosEnable[runId][pltIdx];
                }
            }
            return false;
        }
        /// <summary>
        /// 获取上下料夹具位使能状态
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx">夹具位置索引</param>
        /// <returns></returns>
        public bool GetPalletPosOffloadEnable(RunID runId, int pltIdx)
        {
            // 本地
            RunProcessOffloadBattery run = GetInstance().GetModule(runId) as RunProcessOffloadBattery;
            if (null != run)
            {
                return run.PalletPosEnable[pltIdx];
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.pltPosEnable[runId][pltIdx];
                }
            }
            return false;
        }

        /// <summary>
        /// 获取模组夹具位感应器状态：0未知，1为OFF，2为ON，3为错误（和enum OvenStatus枚举中夹具状态对应）
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx">夹具位置索引</param>
        /// <returns>夹具位感应器状态：0未知，1为OFF，2为ON，3为错误（和enum OvenStatus枚举中夹具状态对应）</returns>
        public int GetPalletPosSenser(RunID runId, int pltIdx)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if (null != run)
            {
                bool hasPlt = run.PalletKeepFlat(pltIdx, true, false);
                bool noPlt = run.PalletKeepFlat(pltIdx, false, false);
                // 无夹具
                if (!hasPlt && noPlt)
                {
                    return (int)OvenStatus.PalletNot;
                }
                // 有夹具
                else if (hasPlt && !noPlt)
                {
                    return (int)OvenStatus.PalletHave;
                }
                // 错误
                else
                {
                    return (int)OvenStatus.PalletErrror;
                }
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    if (socketData.pltPosSenser[runId].Length > pltIdx)
                    {
                        return socketData.pltPosSenser[runId][pltIdx];
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// 获取机器人动作信息
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="autoAction"></param>
        /// <returns></returns>
        public RobotActionInfo GetRobotActionInfo(RunID runId, bool autoAction)
        {
            RunProcess run = GetInstance().GetModule(runId);
            // 模组存在，使用本地数据
            if (null != run)
            {
                if (run is RunProcessOnloadRobot)
                {
                    return ((RunProcessOnloadRobot)run).GetRobotActionInfo(autoAction);
                }
                else if (run is RunProcessRobotTransfer)
                {
                    return ((RunProcessRobotTransfer)run).GetRobotActionInfo(autoAction);
                }
                else if (run is RunProcessOffloadBattery)
                {
                    return ((RunProcessOffloadBattery)run).GetRobotActionInfo(autoAction);
                }
            }
            // 模组不存在，使用网络数据
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.robotAction[runId][autoAction ? 0 : 1];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取机器人移动状态：true移动中，false非移动中
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public bool GetRobotRunning(RunID runId)
        {
            RunProcess run = GetInstance().GetModule(runId);
            // 模组存在，使用本地数据
            if (null != run)
            {
                if (run is RunProcessOnloadRobot)
                {
                    return ((RunProcessOnloadRobot)run).RobotRunning;
                }
                else if (run is RunProcessRobotTransfer)
                {
                    return ((RunProcessRobotTransfer)run).RobotRunning;
                }
            }
            // 模组不存在，使用网络数据
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.robotRunning[runId];
                }
            }
            return false;
        }

        /// <summary>
        /// 设置模组的信号状态
        /// </summary>
        /// <param name="modEvent"></param>
        /// <param name="eventState"></param>
        /// <returns></returns>
        public bool SetModuleEvent(RunID runId, EventList modEvent, EventStatus eventState, int eventPos)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if (null != run)
            {
                return run.SetEvent(run, modEvent, eventState, eventPos);
            }
            // 网络
            else
            {
                ModuleSocketData socketData = new ModuleSocketData();
                ModuleEvent[] evt = new ModuleEvent[1];
                evt[0] = new ModuleEvent(modEvent, eventState, eventPos);
                socketData.moduleEvent = new Dictionary<RunID, ModuleEvent[]>();
                socketData.moduleEvent.Add(runId, evt);
                for (int i = 0; i < this.machineClient.Count; i++)
                {
                    if ((null != this.machineClient[i]) && this.machineClient[i].CheckRunID(-1, (int)runId))
                    {
                        return this.machineClient[i].SendAndWait((uint)PacketType.SetEvent, socketData);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取模组的信号状态
        /// </summary>
        /// <param name="modEvent"></param>
        /// <returns></returns>
        public EventStatus GetModuleEvent(RunID runId, EventList modEvent, ref int eventPos)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if (null != run)
            {
                return run.GetEvent(run, modEvent, ref eventPos);
            }
            // 网络
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    if ((null != socketData.moduleEvent) && (null != socketData.moduleEvent[runId]))
                    {
                        for (int i = 0; i < socketData.moduleEvent[runId].Length; i++)
                        {
                            if (modEvent == socketData.moduleEvent[runId][i].Event)
                            {
                                eventPos = socketData.moduleEvent[runId][i].Pos;
                                return socketData.moduleEvent[runId][i].State;
                            }
                        }
                    }
                }
            }
            return EventStatus.Invalid;
        }

        /// <summary>
        /// 获取模组夹具数据
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public Pallet[] GetModulePallet(RunID runId)
        {
            RunProcess run = GetInstance().GetModule(runId);
            // 模组存在，使用本地数据
            if (null != run)
            {
                return run.Pallet;
            }
            // 模组不存在，使用网络数据
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.pallet[runId];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取模组电池线数据（冷却系统电池）
        /// </summary>
        /// <param name="runId"></param>
        /// <returns></returns>
        public BatteryLine GetModuleBatteryLine(RunID runId)
        {
            RunProcess run = GetInstance().GetModule(runId);
            // 模组存在，使用本地数据
            if (null != run)
            {
                return run.BatteryLine;
            }
            // 模组不存在，使用网络数据
            else
            {
                ModuleSocketData socketData = GetInstance().GetModuleSocketData(runId);
                if (null != socketData)
                {
                    return socketData.batteryLine[runId];
                }
            }
            return null;
        }

        /// <summary>
        /// 设置模组夹具数据
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx"></param>
        /// <param name="pallet"></param>
        /// <returns></returns>
        public bool SetModulePallet(RunID runId, int pltIdx, Pallet pallet, bool place = false)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if (null != run)
            {
                run.SetPallet(pltIdx, pallet, place);
                run.Pallet[pltIdx].Copy(pallet);
                run.SaveRunData(SaveType.Pallet);
                return true;
            }
            // 网络
            else
            {
                ModuleSocketData socketData = new ModuleSocketData();
                Pallet[] plt = new Pallet[pltIdx + 1];
                plt[pltIdx] = pallet;
                socketData.pallet = new Dictionary<RunID, Pallet[]>();
                socketData.pallet.Add(runId, plt);
                if (null != socketData.pallet)
                {
                    for (int i = 0; i < this.machineClient.Count; i++)
                    {
                        if ((null != this.machineClient[i]) && this.machineClient[i].CheckRunID(-1, (int)runId))
                        {
                            return this.machineClient[i].SendAndWait((uint)PacketType.SetPallet, socketData);
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 发送夹具信息到上下料平台
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="pltIdx"></param>
        /// <param name="pallet"></param>
        /// <returns></returns>
        public bool SetPltInfo(RunID runId, int pltIdx, Pallet pallet)
        {
            // 本地
            RunProcess run = GetInstance().GetModule(runId);
            if (null != run)
            {
                run.Pallet[pltIdx].Copy(pallet);
                run.SaveRunData(SaveType.Pallet);
                return true;
            }
            // 网络
            else
            {
                ModuleSocketData socketData = new ModuleSocketData();
                Pallet[] plt = new Pallet[pltIdx + 1];
                plt[pltIdx] = pallet;
                socketData.pallet = new Dictionary<RunID, Pallet[]>();
                socketData.pallet.Add(runId, plt);
                if (null != socketData.pallet)
                {
                    for (int i = 0; i < this.machineClient.Count; i++)
                    {
                        if ((null != this.machineClient[i]) && this.machineClient[i].CheckRunID(-1, (int)runId))
                        {
                            return this.machineClient[i].SendAndWait((uint)PacketType.SetPallet, socketData);
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 设置腔体水含量数据
        /// </summary>
        /// <param name="runId"></param>
        /// <param name="cavityIdx"></param>
        /// <param name="water"></param>
        /// <returns></returns>
        public bool SetCavityWaterContent(RunID runId, int cavityIdx, double[] water)
        {
            // 本地
            RunProcessDryingOven run = GetInstance().GetModule(runId) as RunProcessDryingOven;
            if (null != run)
            {
                run.SetWaterContent(cavityIdx, water);
                return true;
            }
            // 网络
            else
            {
                ModuleSocketData socketData = new ModuleSocketData();
                double[,] waterContent = new double[(int)OvenRowCol.MaxRow, 2];
                for (int i = 0; i < 2; i++)
                {
                    waterContent[cavityIdx, i] = water[i];
                }
                socketData.waterContentValue = new Dictionary<RunID, double[,]>();
                socketData.waterContentValue.Add(runId, waterContent);
                if (null != socketData.waterContentValue)
                {
                    for (int i = 0; i < this.machineClient.Count; i++)
                    {
                        if ((null != this.machineClient[i]) && this.machineClient[i].CheckRunID(-1, (int)runId))
                        {
                            return this.machineClient[i].SendAndWait((uint)PacketType.SetWaterContent, socketData);
                        }
                    }
                }
            }
            return false;
        }

        #endregion

        #region // 解析IO及电机配置

        public int DecodeInputID(string strID)
        {
            if (!string.IsNullOrEmpty(strID) && (strID.IndexOf("-1") < 0))
            {
                int id = this.listInput.IndexOf(strID);
                if (id > -1)
                {
                    return id;
                }
                else if (!Def.IsNoHardware())
                {
                    ShowMsgBox.ShowDialog(string.Format("未找到输入配置[{0}]", strID), MessageType.MsgAlarm);
                }
            }
            return -1;
        }

        public int DecodeOutputID(string strID)
        {
            if (!string.IsNullOrEmpty(strID) && (strID.IndexOf("-1") < 0))
            {
                int id = this.listOutput.IndexOf(strID);
                if (id > -1)
                {
                    return id;
                }
                else if (!Def.IsNoHardware())
                {
                    ShowMsgBox.ShowDialog(string.Format("未找到输出配置[{0}]", strID), MessageType.MsgAlarm);
                }
            }
            return -1;
        }

        public int DecodeMotorID(string strID)
        {
            if (!string.IsNullOrEmpty(strID) && (strID.IndexOf("-1") < 0))
            {
                strID = "Motor" + strID.Trim("M".ToCharArray());
                int id = this.listMotor.IndexOf(strID);
                if (id > -1)
                {
                    return id;
                }
                else if (!Def.IsNoHardware())
                {
                    ShowMsgBox.ShowDialog(string.Format("未找到电机文件[{0}]", strID), MessageType.MsgAlarm);
                }
            }
            return -1;
        }

        #endregion

        #region // 线程初始化及释放

        /// <summary>
        /// 初始化线程(开始运行)
        /// </summary>
        private bool InitThread()
        {
            this.taskList = new List<Task>();
            try
            {
                this.monitorRunning = true;

                Task task = new Task(MonitorThread, TaskCreationOptions.LongRunning);
                task.Start();
                this.taskList.Add(task);
                Def.WriteLog("MachineCtrl", $"InitThread():MonitorThread = {task.Id} start", LogType.Success);

                task = new Task(HeartbeatThread, TaskCreationOptions.LongRunning);
                task.Start();
                this.taskList.Add(task);
                this.heartbeatTime = DateTime.Now;
                this.heartbeatCount = 0;
                Def.WriteLog("MachineCtrl", $"InitThread():HeartbeatThread = {task.Id} start", LogType.Success);

                task = new Task(TelemetryThread, TaskCreationOptions.LongRunning);
                task.Start();
                this.taskList.Add(task);
                Def.WriteLog("MachineCtrl", $"InitThread():TelemetryThread = {task.Id} start", LogType.Success);

                task = (new Task(ExpirationCheck, TaskCreationOptions.None));
                task.Start();
                this.taskList.Add(task);
                Def.WriteLog("MachineCtrl", $"InitThread():ExpirationCheck = {task.Id} start", LogType.Success);

                task = new Task(BakingDataPublishThread, TaskCreationOptions.LongRunning);
                task.Start();
                this.taskList.Add(task);
                Def.WriteLog("MachineCtrl", $"InitThread():BakingDataPublishThread = {task.Id} start", LogType.Success);

                ThreadManager.Init(IOTThread);
                ThreadManager.Start();

                return true;
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MachineCtrl", $"InitThread() error : {ex.Message}", LogType.Error);
            }
            return false;
        }

        /// <summary>
        /// 释放线程(终止运行)
        /// </summary>
        private bool ReleaseThread()
        {
            foreach (var item in this.ListRuns)
            {
                item.MotorMoveToSafePos();
            }

            try
            {
                this.monitorRunning = false;

                Task.WaitAll(this.taskList.ToArray(), 10000);
                Def.WriteLog("MachineCtrl", $"ReleaseThread() All Task end", LogType.Success);

                ThreadManager.Terminal();

                return true;
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MachineCtrl", $"ReleaseThread() error: {ex.Message}", LogType.Error);
            }
            return false;
        }

        #endregion

        #region // 监视线程

        /// <summary>
        /// 模组监视线程
        /// </summary>
        private void MonitorThread()
        {
            while (this.monitorRunning)
            {
                try
                {
                    MCState mcState = this.RunsCtrl.GetMCState();

                    if (McStopState(mcState))
                    {
                        if (!autoConnectCSState && (DateTime.Now - autoConnectCSTime).TotalSeconds > 3)
                        {
                            if (!ClientIsConnect(false))
                            {
                                autoConnectCSState = true;
                                AutoConnectClient();
                            }
                            autoConnectCSTime = DateTime.Now;
                        }
                    }
                    else
                    {
                        if (ClientIsConnect(true))
                        {

                        }
                    }
                    MachineMonitor(mcState);
                    SafeDoorMonitor(mcState);
                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("MachineCtrl", "MonitorThread()" + ex.Message, LogType.Error);
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 设备监视
        /// </summary>
        private void MachineMonitor(MCState mcState)
        {
            //if (!ClientIsConnect()/* && !ConnectClient()*/)
            //{
            //    ConnectClient();
            //    //ShowMsgBox.ShowDialog("请等候模组服务连接后再启动软件...", MessageType.MsgWarning);
            //}

            // 操作灯塔、按钮LED
            if ((DateTime.Now - setTowerStart).TotalMilliseconds >= 200.0)
            {
                SetTowerButton(mcState);
                this.setTowerStart = DateTime.Now;
            }
            // 判断系统按钮
            // 急停-停止
            if (StopButtonOn())
            {
                this.RunsCtrl.Stop();
            }
            if (EStopButtonOn())
            {
                this.RunsCtrl.Stop();
                this.RunsCtrl.EmStop();
            }
            // 复位
            if (ResetButtonOn())
            {
                if (!this.resetButtonOff)
                {
                    this.resetButtonOff = true;
                    MachineReset();
                }
            }
            else if (this.resetButtonOff && !ResetButtonOn())
            {
                this.resetButtonOff = false;
            }
            // 启动 && 非维护锁屏状态
            if (StartButtonOn() && !this.MaintenanceLock)
            {
                if (!this.startButtonOff)
                {
                    //MES启动前检查

                    if (true)
                    {
                        if(RunsCtrl.GetMCState() != MCState.MCIdle)
                        {
                            this.startButtonOff = true;
                            string msg = "";
                            string workplace = "DAL1HK01";
                            if (Jeve_Mes.Mes_StertCheck(workplace, ref msg))
                            {
                                this.RunsCtrl.Start();
                                
                            }
                            else
                            {
                                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                            }
                        }
                        else
                        {
                            this.RunsCtrl.Start();
                        }
                        
                       
                        
                    }
                }
            }
            else if (this.startButtonOff && !StartButtonOn())
            {
                this.startButtonOff = false;
            }
            if (this.IAirPressureAlarm > -1 && DeviceManager.Inputs(IAirPressureAlarm).IsOn())
            {
                ShowMessageID((int)MsgID.AirPressureAlm, "气压过低报警", "请检查气压", MessageType.MsgAlarm);
                if (!McStopState(mcState))
                {
                    this.RunsCtrl.Stop();
                }
            }

            foreach (var item in this.ListRuns)
            {
                item.MonitorAvoidDie();
            }

            // 开启WebApi
            //if (MesResources.MesLogin && !WebServer.Start())
            if (!WebServer.Start())
            {
                ShowMsgBox.ShowDialog("WebApi接口服务启动失败, 请检查MES网口IP地址设置是否正确!!!", MessageType.MsgAlarm);
            }
        }

        #endregion
        #region // 删除过期文件

        /// <summary>
        /// 过期检查线程，只执行一次
        /// </summary>
        private void ExpirationCheck()
        {
            try
            {
                DeleteDirectoryFile(new DirectoryInfo(Def.GetAbsPathName("Log")));
                DeleteDirectoryFile(new DirectoryInfo(this.ProductionFilePath));
                DeleteDataBaseRecord();
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MachineCtrl", "ExpirationCheck()" + ex.Message, LogType.Error);
            }
        }

        /// <summary>
        /// 删除目录中超期的文件
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private void DeleteDirectoryFile(DirectoryInfo dir)
        {
            FileInfo[] fileInfo = dir.GetFiles();
            // 遍历文件
            foreach (FileInfo item in fileInfo)
            {
                if ((DateTime.Now - item.CreationTime).TotalDays > this.productionFileStorageLife)
                {
                    File.Delete(item.FullName);
                    Def.WriteLog("DeleteDirectoryFile()", $"{item.FullName} 超过{productionFileStorageLife}天，已被删除", LogType.Success);
                }
            }
            DirectoryInfo[] dirInfo = dir.GetDirectories();
            // 遍历文件夹
            foreach (DirectoryInfo item in dirInfo)
            {
                DeleteDirectoryFile(item);
            }
            // 删除空文件夹
            if ((fileInfo.Length < 1) && (dirInfo.Length < 1))
            {
                Directory.Delete(dir.FullName);
            }
        }

        /// <summary>
        /// 删除数据库中超期的记录
        /// </summary>
        private void DeleteDataBaseRecord()
        {
            int formulaId = Def.GetProductFormula();
            DateTime startDT = new DateTime();
            DateTime endDT = DateTime.Now.AddDays(-this.productionFileStorageLife);
            string startTime = startDT.ToString(Def.DateFormal);
            string endTime = endDT.ToString(Def.DateFormal);
            string logEndTime = DateTime.Now.AddDays(-this.mcLogFileStorageLife).ToString(Def.DateFormal);

            if (DataBaseLog.DeleteDryingOvenLog(formulaId, -1, startTime, logEndTime))
            {
                Def.WriteLog("DeleteDataBaseRecord()", $"{DataBaseLog.LogTableType.DryingOvenLog} 表中{logEndTime}之前的记录已被删除", LogType.Success);
            }
            if (DataBaseLog.DeleteParameterLog(formulaId, -1, startTime, endTime))
            {
                Def.WriteLog("DeleteDataBaseRecord()", $"{DataBaseLog.LogTableType.ParameterLog} 表中{endTime}之前的记录已被删除", LogType.Success);
            }
            if (DataBaseLog.DeleteRobotLog(formulaId, -1, startTime, logEndTime))
            {
                Def.WriteLog("DeleteDataBaseRecord()", $"{DataBaseLog.LogTableType.RobotLog} 表中{logEndTime}之前的记录已被删除", LogType.Success);
            }
            if (DataBaseLog.DeleteMotorLog(formulaId, -1, startTime, logEndTime))
            {
                Def.WriteLog("DeleteDataBaseRecord()", $"{DataBaseLog.LogTableType.MotorLog} 表中{logEndTime}之前的记录已被删除", LogType.Success);
            }
            if (this.dbRecord.DeleteAlarmInfo(formulaId, -1, -1, startTime, endTime))
            {
                Def.WriteLog("DeleteDataBaseRecord()", $"{TableName[(int)TableType.TABLE_ALARM]} 表中{endTime}之前的记录已被删除", LogType.Success);
            }
        }

        #endregion

        #region // 设备按钮及灯塔

        bool StartButtonOn()
        {
            foreach (var item in this.IStartButton)
            {
                if (item > -1 && DeviceManager.Inputs(item).IsOn())
                {
                    return true;
                }
            }
            return false;
        }

        bool StopButtonOn()
        {
            for (int i = 0; i < this.IStopButton.Length; i++)
            {
                if (/*((this.IEStopButton[i] > -1) && DeviceManager.Inputs(IEStopButton[i]).IsOn())
                    || */(this.IStopButton[i] > -1) && DeviceManager.Inputs(IStopButton[i]).IsOn())
                {
                    return true;
                }
            }
            return false;
        }
        bool EStopButtonOn()
        {
            for (int i = 0; i < this.IEStopTranButton.Length; i++)
            {
                if ((this.IEStopTranButton[i] > -1) && DeviceManager.Inputs(IEStopTranButton[i]).IsOn())
                {
                    string msg = string.Format("{0}触发", DeviceManager.Inputs(IEStopTranButton[i]).Name);
                    ShowMessageID((int)MsgID.TransferSysRunning, msg, "请检查当前急停按钮状态", MessageType.MsgAlarm);
                    return true;
                }
            }
            for (int i = 0; i < this.IEStopOnloadButton.Length; i++)
            {
                if ((this.IEStopOnloadButton[i] > -1) && DeviceManager.Inputs(IEStopOnloadButton[i]).IsOff())
                {
                    string msg = string.Format("{0}触发", DeviceManager.Inputs(IEStopOnloadButton[i]).Name);
                    ShowMessageID((int)MsgID.TransferSysRunning, msg, "请检查当前急停按钮状态", MessageType.MsgAlarm);
                    return true;
                }
            }
            for (int i = 0; i < this.IEStopOffloadButton.Length; i++)
            {
                if ((this.IEStopOffloadButton[i] > -1) && DeviceManager.Inputs(IEStopOffloadButton[i]).IsOff())
                {
                    string msg = string.Format("{0}触发", DeviceManager.Inputs(IEStopOffloadButton[i]).Name);
                    ShowMessageID((int)MsgID.TransferSysRunning, msg, "请检查当前急停按钮状态", MessageType.MsgAlarm);
                    return true;
                }
            }
            for (int i = 0; i < this.IEStopDoor1Button.Length; i++)
            {
                if ((this.IEStopDoor1Button[i] > -1) && DeviceManager.Inputs(IEStopDoor1Button[i]).IsOn())
                {
                    string msg = string.Format("{0}触发", DeviceManager.Inputs(IEStopDoor1Button[i]).Name);
                    ShowMessageID((int)MsgID.TransferSysRunning, msg, "请检查当前急停按钮状态", MessageType.MsgAlarm);
                    return true;
                }
            }
            for (int i = 0; i < this.IEStopdoor2Button.Length; i++)
            {
                if ((this.IEStopdoor2Button[i] > -1) && DeviceManager.Inputs(IEStopdoor2Button[i]).IsOn())
                {
                    string msg = string.Format("{0}触发", DeviceManager.Inputs(IEStopdoor2Button[i]).Name);
                    ShowMessageID((int)MsgID.TransferSysRunning, msg, "请检查当前急停按钮状态", MessageType.MsgAlarm);
                    return true;
                }
            }
            return false;
        }
        bool ResetButtonOn()
        {
            foreach (var item in this.IResetButton)
            {
                if (item > -1 && DeviceManager.Inputs(item).IsOn())
                {
                    return true;
                }
            }
            return false;
        }

        void SetTowerButton(MCState mcState)
        {
            if (Def.IsNoHardware())
            {
                return;
            }
            switch (mcState)
            {
                case MCState.MCIdle:
                case MCState.MCInitComplete:
                case MCState.MCStopInit:
                case MCState.MCStopRun:
                    {
                        // 按钮
                        for (int i = 0; i < this.OStartLed.Length; i++)
                        {
                            if (OStartLed[i] > -1)
                                DeviceManager.Outputs(OStartLed[i]).Off();
                            if (OStopLed[i] > -1)
                                DeviceManager.Outputs(OStopLed[i]).On();
                            if (OResetLed[i] > -1)
                                DeviceManager.Outputs(OResetLed[i]).Off();
                        }
                        // 灯塔
                        for (int i = 0; i < this.OLightTowerRed.Length; i++)
                        {
                            if (OLightTowerRed[i] > -1)
                                DeviceManager.Outputs(OLightTowerRed[i]).Off();
                            if (OLightTowerYellow[i] > -1)
                                DeviceManager.Outputs(OLightTowerYellow[i]).On();
                            if (OLightTowerGreen[i] > -1)
                                DeviceManager.Outputs(OLightTowerGreen[i]).Off();
                            if (OLightTowerBuzzer[i] > -1)
                                DeviceManager.Outputs(OLightTowerBuzzer[i]).Off();
                        }
                        break;
                    }
                case MCState.MCInitializing:
                case MCState.MCRunning:
                    {
                        // 有模组报警 || 有弹窗
                        bool hasMsg = (this.RunsCtrl.HasModuleAlarm() || this.RunsCtrl.HasModuleMessage() || this.hasMsgBox);

                        // 按钮
                        for (int i = 0; i < this.OStartLed.Length; i++)
                        {
                            if (OStartLed[i] > -1)
                                DeviceManager.Outputs(OStartLed[i]).On();
                            if (OStopLed[i] > -1)
                                DeviceManager.Outputs(OStopLed[i]).Off();
                            if (OResetLed[i] > -1)
                                DeviceManager.Outputs(OResetLed[i]).Off();
                        }
                        // 灯塔
                        for (int i = 0; i < this.OLightTowerRed.Length; i++)
                        {
                            if (OLightTowerRed[i] > -1)
                            {
                                if (hasMsg)
                                    DeviceManager.Outputs(OLightTowerRed[i]).On();
                                else
                                    DeviceManager.Outputs(OLightTowerRed[i]).Off();
                            }
                            if (OLightTowerYellow[i] > -1)
                                DeviceManager.Outputs(OLightTowerYellow[i]).Off();
                            if (OLightTowerGreen[i] > -1)
                                DeviceManager.Outputs(OLightTowerGreen[i]).On();
                            if (OLightTowerBuzzer[i] > -1)
                            {
                                if (hasMsg && DeviceManager.Outputs(OLightTowerBuzzer[i]).IsOff())
                                    DeviceManager.Outputs(OLightTowerBuzzer[i]).On();
                                else
                                    DeviceManager.Outputs(OLightTowerBuzzer[i]).Off();
                            }
                        }
                        break;
                    }
                case MCState.MCInitErr:
                case MCState.MCRunErr:
                    {
                        // 按钮
                        for (int i = 0; i < this.OStartLed.Length; i++)
                        {
                            if (OStartLed[i] > -1)
                                DeviceManager.Outputs(OStartLed[i]).Off();
                            if (OStopLed[i] > -1)
                                DeviceManager.Outputs(OStopLed[i]).Off();
                            if (OResetLed[i] > -1)
                                DeviceManager.Outputs(OResetLed[i]).On();
                        }
                        // 灯塔
                        for (int i = 0; i < this.OLightTowerRed.Length; i++)
                        {
                            if (OLightTowerRed[i] > -1)
                                DeviceManager.Outputs(OLightTowerRed[i]).On();
                            if (OLightTowerYellow[i] > -1)
                                DeviceManager.Outputs(OLightTowerYellow[i]).Off();
                            if (OLightTowerGreen[i] > -1)
                                DeviceManager.Outputs(OLightTowerGreen[i]).Off();
                            if (OLightTowerBuzzer[i] > -1)
                                DeviceManager.Outputs(OLightTowerBuzzer[i]).On();
                        }
                        break;
                    }
                // 退出设备时关闭所有系统输出
                case MCState.NumMCState:
                    {
                        // 按钮
                        for (int i = 0; i < this.OStartLed.Length; i++)
                        {
                            if (OStartLed[i] > -1)
                                DeviceManager.Outputs(OStartLed[i]).Off();
                            if (OStopLed[i] > -1)
                                DeviceManager.Outputs(OStopLed[i]).Off();
                            if (OResetLed[i] > -1)
                                DeviceManager.Outputs(OResetLed[i]).Off();
                        }
                        // 灯塔
                        for (int i = 0; i < this.OLightTowerRed.Length; i++)
                        {
                            if (OLightTowerRed[i] > -1)
                                DeviceManager.Outputs(OLightTowerRed[i]).Off();
                            if (OLightTowerYellow[i] > -1)
                                DeviceManager.Outputs(OLightTowerYellow[i]).Off();
                            if (OLightTowerGreen[i] > -1)
                                DeviceManager.Outputs(OLightTowerGreen[i]).Off();
                            if (OLightTowerBuzzer[i] > -1)
                                DeviceManager.Outputs(OLightTowerBuzzer[i]).Off();
                        }
                        break;
                    }
            }
        }

        #endregion

        #region // 安全门

        /// <summary>
        /// 安全门监视
        /// </summary>
        private void SafeDoorMonitor(MCState mcState)
        {
            if (!safeDoorEnabled)
            {
                this.SafeDoorStateOpen = false;
                return;
            }
            // 检查安全门
            if (mcState > MCState.MCIdle)
            {
                this.SafeDoorStateOpen = CheckSafeDoorState();
            }
            //SafeDoorCanOpen();
        }

        /// <summary>
        /// 检查安全门状态：true：已打开；false：关闭
        /// </summary>
        /// <returns>true：已打开；false：关闭</returns>
        private bool CheckSafeDoorState()
        {
            // 服务端的连接状态
            //if (!ClientIsConnect())
            //{
            //    return true;
            //}

            if (!safeDoorEnabled)
            {
                return false;
            }

            // 安全门操作：
            //1、开门检查，上下料，调度程序是否停止运行，没有停止，先停止。
            //2、程序启动时检查安全门是否关闭，门未关闭。
            //3、程序启动时检查急停是否按下。
            //return false;

            bool isOpen = false;
            bool mcStop = McStopState(RunsCtrl.GetMCState());
            int ioInput = -1;
            // 模组所在设备配置的安全门
            for (int door = 0; door < (int)SystemIO.SafeDoorIO; door++)
            {
                if (SafeDoorOpenState(door, ref ioInput))
                {
                    if (!mcStop) // 非（闲置/初始化停止/初始化完成/运行停止)
                    {
                        //RunsCtrl.Stop(); // 
                        string[] arrErr = new string[1];
                        arrErr[0] = string.Format("X{0:d4}", ioInput);
                        string msg = string.Format("{0}触发", DeviceManager.Inputs(SafeDoorState[door]).Name);
                        ShowMessageID((int)MsgID.TransferSysRunning, msg, "请检查当前安全门状态", MessageType.MsgAlarm);
                        //ShowMessageID((int)MsgID.DoorAlarm_0 + door, arrErr);
                        return true;
                    }
                }
            }

            //if (GetInstance().MachineID == 0)
            //{
            //    // 下料安全门
            //    ModuleSocketData socketData = GetModuleSocketData(RunID.OffloadBattery);
            //    if (null != socketData)
            //    {
            //        for (int door = 0; door < (int)SystemIO.SafeDoorIO; door++)
            //        {
            //            if (socketData.safeDoor[door])
            //            {
            //                if (!mcStop)
            //                {
            //                    //RunsCtrl.Stop();
            //                    string[] arrErr = new string[1];
            //                    arrErr[0] = string.Format("下料安全门{0}", door + 1);
            //                    ShowMessageID((int)MsgID.DoorAlarm_0 + door, arrErr);
            //                    return true;
            //                }
            //            }
            //            if (socketData.safeDoorStop[door])
            //            {
            //                //急停机器人
            //                RunProcessOnloadRobot run = GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            //                if (null != run)
            //                {
            //                    run.SetORobotEStop();
            //                }

            //                RunProcessRobotTransfer run2 = GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            //                if (null != run2)
            //                {
            //                    run2.SetORobotEStop();
            //                }

            //                string msg = "下料安全门急停打开！";
            //                ShowMessageID((int)(MsgID.DoorAlarm_0 + door), msg, "请先检查排除设备异常情况后再操作运行", MessageType.MsgWarning);
            //                return true;
            //            }
            //        }
            //    }
            //}
            //else if (GetInstance().MachineID == 1)
            //{
            //    // 上料安全门
            //    ModuleSocketData socketData = GetModuleSocketData(RunID.OnloadRobot);
            //    if (null != socketData)
            //    {
            //        for (int door = 0; door < (int)SystemIO.SafeDoorIO; door++)
            //        {
            //            if (socketData.safeDoor[door])
            //            {
            //                if (!mcStop)
            //                {
            //                    string[] arrErr = new string[1];
            //                    arrErr[0] = string.Format("上料安全门{0}", door + 1);
            //                    ShowMessageID((int)MsgID.DoorAlarm_0 + door, arrErr);
            //                    //RunsCtrl.Stop();
            //                    return true;
            //                }
            //            }
            //            if (socketData.safeDoorStop[door])
            //            {
            //                //急停机器人
            //                RunProcessOnloadRobot run = GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            //                if (null != run)
            //                {
            //                    run.SetORobotEStop();
            //                }

            //                RunProcessRobotTransfer run2 = GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            //                if (null != run2)
            //                {
            //                    run2.SetORobotEStop();
            //                }
            //                string msg = "上料安全门急停打开！";
            //                ShowMessageID((int)(MsgID.DoorAlarm_0 + door), msg, "请先检查排除设备异常情况后再操作运行", MessageType.MsgWarning);
            //                return true;
            //            }
            //        }
            //    }
            //    // 下料安全门
            //    socketData = GetModuleSocketData(RunID.OffloadBattery);
            //    if (null != socketData)
            //    {
            //        for (int door = 0; door < (int)SystemIO.SafeDoorIO; door++)
            //        {
            //            if (socketData.safeDoor[door])
            //            {
            //                if (!mcStop)
            //                {
            //                    //RunsCtrl.Stop();
            //                    string[] arrErr = new string[1];
            //                    arrErr[0] = string.Format("下料安全门{0}", door + 1);
            //                    ShowMessageID((int)MsgID.DoorAlarm_0 + door, arrErr);
            //                    return true;
            //                }
            //            }
            //            if (socketData.safeDoorStop[door])
            //            {
            //                //急停机器人
            //                RunProcessOnloadRobot run = GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            //                if (null != run)
            //                {
            //                    run.SetORobotEStop();
            //                }

            //                RunProcessRobotTransfer run2 = GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            //                if (null != run2)
            //                {
            //                    run2.SetORobotEStop();
            //                }

            //                string msg = "下料安全门急停打开！";
            //                ShowMessageID((int)(MsgID.DoorAlarm_0 + door), msg, "请先检查排除设备异常情况后再操作运行", MessageType.MsgWarning);
            //                return true;
            //            }
            //        }
            //    }
            //}
            //else if (GetInstance().MachineID == 2)
            //{
            //    // 上料安全门
            //    ModuleSocketData socketData = GetModuleSocketData(RunID.OnloadRobot);
            //    if (null != socketData)
            //    {
            //        for (int door = 0; door < (int)SystemIO.SafeDoorIO; door++)
            //        {
            //            if (socketData.safeDoor[door])
            //            {
            //                if (!mcStop)
            //                {
            //                    string[] arrErr = new string[1];
            //                    arrErr[0] = string.Format("上料安全门{0}", door + 1);
            //                    ShowMessageID((int)MsgID.DoorAlarm_0 + door, arrErr);
            //                    //RunsCtrl.Stop();
            //                    return true;
            //                }
            //            }
            //            if (socketData.safeDoorStop[door])
            //            {
            //                // 急停机器人
            //                RunProcessOffloadBattery run = GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery;
            //                if (null != run)
            //                {
            //                    run.SetORobotEStop();
            //                }

            //                RunProcessCoolingSystem run2 = GetModule(RunID.CoolingSystem) as RunProcessCoolingSystem;
            //                if (null != run2)
            //                {
            //                    run2.SetORobotEStop();
            //                }

            //                RunProcessCoolingOffload run3 = GetModule(RunID.CoolingOffload) as RunProcessCoolingOffload;
            //                if (null != run3)
            //                {
            //                    run3.SetORobotEStop();
            //                }

            //                string msg = "上料安全门急停打开！";

            //                ShowMessageID((int)(MsgID.DoorAlarm_0 + door), msg, "请先检查并排除设备异常情况后再操作运行", MessageType.MsgWarning);

            //                return true;
            //            }
            //        }
            //    }
            //}
            return isOpen;
        }

        /// <summary>
        /// 判断安全门是否已打开：true已打开，false已关闭
        /// </summary>
        /// <param name="doorIdx"></param>
        /// <returns></returns>
        public bool SafeDoorOpenState(int doorIdx, ref int ioHx)
        {
            if (Def.IsNoHardware())
            {
                return false;
            }
            //if (doorIdx > -1 && doorIdx < (int)SystemIO.SafeDoorIO)
            //{
            //    RunProcessSafeDoor run = GetInstance().GetModule(RunID.SafeDoor) as RunProcessSafeDoor;
            //    if (run != null)
            //    {
            //        return run.GetSafeDoorOpenState(doorIdx, ref ioHx);
            //    }
            //}
            //return false;

            if (Def.IsNoHardware())
            {
                return false;
            }
            if (doorIdx > -1 && doorIdx < (int)SystemIO.SafeDoorIO)
            {
                if (SafeDoorState[doorIdx] > -1)
                {
                    return DeviceManager.Inputs(SafeDoorState[doorIdx]).IsOn();
                }
            }
            return false;
        }

        /// <summary>
        /// 判断安全门是否急停：true，false
        /// </summary>
        /// <param name="doorIdx"></param>
        /// <returns></returns>
        public bool SafeDoorIsStop(int doorIdx)
        {
            if (Def.IsNoHardware())
            {
                return false;
            }
            if (doorIdx > -1 && doorIdx < (int)SystemIO.SafeDoorIO)
            {
                RunProcessSafeDoor run = GetInstance().GetModule(RunID.SafeDoor) as RunProcessSafeDoor;
                if (run != null)
                {
                    return run.SafeDoorIsStop(doorIdx);
                }
            }
            return false;
        }

        /// <summary>
        /// 安全门能否打开
        /// </summary>
        /// <param name="mcState"></param>
        private void SafeDoorCanOpen()
        {
            RunProcessSafeDoor run = MachineCtrl.GetInstance().GetModule(RunID.SafeDoor) as RunProcessSafeDoor;
            if (run == null)
            {
                return;
            }
            for (int idx = 0; idx < (int)SystemIO.SafeDoorIO; idx++)
            {
                // 无开门请求，上锁
                //run.SetSafeDoorUnlock(idx, false);

                // 当前设备有安全门控制
                if (run.HadSafeDoor(idx))
                {
                    bool canOpen = false;
                    // 开按钮按下
                    if (run.GetSafeDoorOpenDown(idx))
                    {
                        if (run.SafeDoorDelay[idx] > 0)
                            Thread.Sleep(run.SafeDoorDelay[idx]);
                        canOpen = true;
                    }
                    // 长按
                    if (canOpen && run.GetSafeDoorOpenDown(idx))
                    {
                        canOpen = true;
                    }
                    else
                    {
                        canOpen = false;
                    }

                    if (canOpen)
                    {
                        if (!ClientIsConnect())
                        {
                            return;
                        }
                        // 有设备在运行中
                        if (!McStopState(GetModuleMCState(RunID.OnloadRobot)))
                        {
                            string msg = "上料机器人设备非停止状态不能打开安全门！";
                            ShowMessageID((int)MsgID.OnloadSysRunning, msg, "请先停止设备运行后再操作", MessageType.MsgWarning);
                            return;
                        }
                        if (!McStopState(GetModuleMCState(RunID.Transfer)))
                        {
                            string msg = "调度机器人设备非停止状态不能打开安全门！";
                            ShowMessageID((int)MsgID.TransferSysRunning, msg, "请先停止设备运行后再操作", MessageType.MsgWarning);
                            return;
                        }
                        if (!McStopState(GetModuleMCState(RunID.OffloadBattery)))
                        {
                            string msg = "下料设备非停止状态不能打开安全门！";
                            ShowMessageID((int)MsgID.OffloadSysRunning, msg, "请先停止设备运行后再操作", MessageType.MsgWarning);
                            return;
                        }
                        if (GetRobotRunning(RunID.OnloadRobot))
                        {
                            string msg = "上料机器人运动中不能打开安全门！";
                            ShowMessageID((int)MsgID.RobotRun, msg, "请先等待机器人运动完成后再操作", MessageType.MsgWarning);
                            return;
                        }
                        if (GetRobotRunning(RunID.Transfer))
                        {
                            string msg = "调度机器人运动中不能打开安全门！";
                            ShowMessageID((int)MsgID.RobotRun + 1, msg, "请先等待机器人运动完成后再操作", MessageType.MsgWarning);
                            return;
                        }

                        run.SetSafeDoorUnlock(idx, true);
                    }
                    else
                    {
                        // 无开门请求，上锁
                        run.SetSafeDoorUnlock(idx, false);
                    }
                }
            }
        }

        /// <summary>
        /// 设备停止状态
        /// </summary>
        /// <param name="mcState"></param>
        /// <returns></returns>
        public bool McStopState(MCState mcState)
        {
            if ((MCState.MCIdle == mcState) || (MCState.MCStopInit == mcState)
                || (MCState.MCInitComplete == mcState) || (MCState.MCStopRun == mcState))
            {
                return true;
            }
            return false;
        }

        #endregion

        #region // 模组服务端，客户端

        public bool CreateServer()
        {
            if (false && Def.IsNoHardware())
            {
                return true;
            }
            bool result = true;
            this.machineServer.CloseServer();
            if (!this.machineServer.CreateServer(machineServerIP, machineServerPort))
            {
                ShowMsgBox.ShowDialog($"{machineServerIP}:{machineServerPort}】服务端创建失败！", MessageType.MsgWarning);
                result = false;
            }
            return result;
        }

        public bool ConnectClient()
        {
            if (!autoConnectCSState)
            {
                autoConnectCSState = true;
                if (false && Def.IsNoHardware())
                {
                    //autoConnectCSState = false;
                    return true;
                }
                for (int i = 0; i < this.machineClient.Count; i++)
                {
                    if (!this.machineClient[i].Connect(machineClientIP[i], machineClientPort[i]))
                    {
                        ShowMsgBox.ShowDialog($"{machineClientIP[i]}:{machineClientPort[i]}】服务器连接失败！", MessageType.MsgWarning);
                        autoConnectCSState = false;
                        return false;
                    }
                }
            }
            autoConnectCSState = false;
            return true;
        }

        public bool ClientIsConnect(bool almStop = false)
        {
            return true;
            if (Def.IsNoHardware())
            {
                return true;
            }
            for (int i = 0; i < this.machineClient.Count; i++)
            {
                if (!this.machineClient[i].IsConnect())
                {
                    if (almStop)
                    {
                        RunsCtrl.Stop();
                        string msg = $"服务端{machineClientIP[i]}:{machineClientPort[i]}】连接断开！";
                        ShowMessageID((int)MsgID.ModuleDisconnect + i, msg, "请先在【调试工具-其它调试】重连模组服务端", MessageType.MsgAlarm);
                    }
                    return false;
                }
            }
            return true;
        }

        private async void AutoConnectClient()
        {
            await Task.Delay(1);

            for (int i = 0; i < this.machineClient.Count; i++)
            {
                Def.WriteLog("MachineCtrl", $"从新连接{machineClientIP[i]}:{machineClientPort[i]}");
                if (!this.machineClient[i].Connect(machineClientIP[i], machineClientPort[i]))
                {
                    break;
                }
            }
            autoConnectCSState = false;
        }

        #endregion
        #region // Mes交互
        #region 设备联机请求 通讯和业务OK
        /// <summary>
        /// MES-->设备联机请求 
        /// </summary>
        /// <returns></returns>
        //public bool ACEQPTCONN_Main(ResourcesStruct rs, ref string errMsg)
        //{
        //    string RecvValue = "";
        //    string SendValue = "";
        //    string jsonResponse = "";
        //    string errorMessage = "";
        //    string msg = "";
        //    var result = false;
        //    MesDefine mesDef = new MesDefine();
        //    MesConfig mes = null;
        //    DateTime startTime = DateTime.Now;
        //    try
        //    {
        //        mes = MesDefine.GetMesCfg(MesInterface.EquToMesEquipmentOnLine);
        //        if (!GetInstance().UpdataMes)
        //        {
        //            return true;
        //        }
        //        //ACEQPTCONN_Request ACEQPTCONN = new ACEQPTCONN_Request();

        //        //ACEQPTCONN.AutoFlag = GetInstance().UpdataMes;
        //        //ACEQPTCONN.Software = "烘烤调度线体";
        //        ////实例化JSON数组
        //        //ACEQPTCONN.EquipmentInfo = new List<EquipmentInfo1>();
        //        //EquipmentInfo1 equi = new EquipmentInfo1();  //实例化JSON数组对象，用于添加JSON数组集合
        //        //equi.EquipmentCode = rs.EquipmentCode;
        //        //ACEQPTCONN.EquipmentInfo.Add(equi);  //添加JSON数组集合

        //        var ACEQPTCONN = new
        //        {
        //            AutoFlag = GetInstance().UpdataMes,
        //            Software = "烘烤调度线体",

        //            EquipmentInfo = new object[1]
        //                {
        //                    new
        //                    {
        //                    EquipmentCode=rs.EquipmentCode
        //                    }
        //                }
        //        };


        //        //序列化
        //        string jsonRequest = JsonConvert.SerializeObject(ACEQPTCONN);
        //        SendValue = MesOperate.RevertJsonString(jsonRequest);     //发送数据

        //        wcf_Client.SendMsg("ACEQPTCONN", jsonRequest, ref jsonResponse, ref errorMessage);
        //        RecvValue = MesOperate.RevertJsonString(jsonResponse);    //接收数据
        //        mes.SetMesInfo(SendValue, RecvValue);
        //        mes.updataRS = true;
        //        JObject revObj = JObject.Parse(jsonResponse);

        //        if (revObj != null)
        //        {
        //            //if (revObj.ContainsKey())
        //            {
        //                result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["Result"]);
        //            }
        //            //if (revObj.ContainsKey("Message"))
        //            {
        //                msg = revObj["EquipmentInfo"][0]["Message"].ToString();
        //            }
        //            if (!result)
        //            {
        //                errMsg = $"设备联机请求失败! \r\nMsg:{msg + errorMessage}";
        //                return false;

        //            }
        //            return true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        RecvValue = ex.Message;
        //        errMsg = $"设备联机请求异常！ \r\nMsg:请检查网线和MES网络通讯问题";
        //    }
        //    finally
        //    {
        //        int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
        //        string sfcode = " ";
        //        string linecode = " ";
        //        string mesUri = "ACEQPTCONN";
        //        string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

        //        MesOperate.SaveLogData("MES设备联机请求", text);
        //    }

        //    return false;
        //}
        #endregion

        #region 设备心跳 通讯和业务OK
        /// <summary>
        /// MES-->设备心跳
        /// </summary>
        /// <returns></returns>
        //public bool ACEQPTALIV_Main(ResourcesStruct rs, ref string errMsg)
        //{
        //    string RecvValue = "";
        //    string SendValue = "";
        //    string jsonResponse = "";
        //    string errorMessage = "";
        //    string msg = "";
        //    var result = false;
        //    MesConfig mes = null;
        //    DateTime startTime = DateTime.Now;
        //    try
        //    {
        //        mes = MesDefine.GetMesCfg(MesInterface.EquToMesHeartbeat);
        //        if (!GetInstance().UpdataMes)
        //        {
        //            return true;
        //        }
        //        var ACEQPTALIV = new
        //        {
        //            AutoFlag = GetInstance().UpdataMes,
        //            Software = "烘烤调度线体",
        //            EmployeeNo = rs.OperatorUserID,             //员工工号
        //            EquipmentInfo = new object[1]
        //                {
        //                    new
        //                    {
        //                    EquipmentCode=rs.EquipmentCode
        //                    }
        //                }
        //        };

        //        string jsonRequest = JsonConvert.SerializeObject(ACEQPTALIV);

        //        SendValue = MesOperate.RevertJsonString(jsonRequest);   //发送数据

        //        wcf_Client.SendMsg("ACEQPTALIV", jsonRequest, ref jsonResponse, ref errorMessage);
        //        RecvValue = MesOperate.RevertJsonString(jsonResponse);  //接收数据
        //        mes.SetMesInfo(SendValue, RecvValue);
        //        mes.updataRS = true;
        //        if (!string.IsNullOrEmpty(jsonResponse))
        //        {
        //            JObject revObj = JObject.Parse(jsonResponse);
        //            jsonResponse = JsonConvert.SerializeObject(revObj);

        //            if (revObj != null)
        //            {
        //                //if (revObj.ContainsKey())
        //                {
        //                    result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["ResultFlag"]);
        //                }
        //                //if (revObj.ContainsKey("Message"))
        //                {
        //                    msg = revObj["EquipmentInfo"][0]["Message"].ToString();
        //                }
        //                if (!result)
        //                {
        //                    errMsg = $"设备心跳请求失败! \r\nMsg:{msg}，\r\nerrorMessage:{errorMessage}";
        //                    return false;
        //                }
        //                return true;
        //            }
        //        }
        //        else
        //        {
        //            errMsg = $"设备心跳请求失败! \r\nMsg:{msg}，\r\nerrorMessage:{errorMessage}";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        RecvValue = ex.Message;
        //        errMsg = $"设备心跳异常！ \r\nMsg:{msg}，\r\nerrorMessage:{errorMessage}，\r\nex:{ex.ToString()}";
        //    }
        //    finally
        //    {
        //        int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
        //        string sfcode = " ";
        //        string linecode = " ";
        //        string mesUri = "ACEQPTALIV";
        //        string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

        //        MesOperate.SaveLogData("MES设备心跳", text);
        //    }

        //    return false;
        //}
        #endregion

        #region 上位机获取人员信息 先用数组存储返回的信息
        public bool ACEMPLOYEE_Main(ResourcesStruct rs, ref string errMsg, ref List<string> idList)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!GetInstance().UpdataMes)
                {
                    return true;
                }
                if (!GetInstance().isMESConnect)
                {
                    return false;
                }
                var ACEMPLOYEE = new
                {
                    Equipment = rs.EquipmentCode,
                    OprSequenceNo = "ZHK211"
                };

                string jsonRequest = JsonConvert.SerializeObject(ACEMPLOYEE);
                SendValue = MesOperate.RevertJsonString(jsonRequest);

                wcf_Client.SendMsg("ACEMPLOYEE", jsonRequest, ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);

                JObject revObj = JObject.Parse(jsonResponse);
                JArray items = (JArray)revObj["UserInfos"];
                //int length = items.Count;
                if (revObj != null)
                {
                    ACEMPLOYEE_Response Loyee = new ACEMPLOYEE_Response();
                    //this.idList = new List<string>();
                    this.idArray = new string[items.Count];
                    for (int i = 0; i < items.Count; i++)
                    {
                        Loyee.id = revObj["UserInfos"][i]["id"].ToString();
                        Loyee.usercode = revObj["UserInfos"][i]["usercode"].ToString();
                        Loyee.name = revObj["UserInfos"][i]["name"].ToString();
                        Loyee.IcCardNo = revObj["UserInfos"][i]["IcCardNo"].ToString();
                        Loyee.RoleID = revObj["UserInfos"][i]["RoleID"].ToString();

                        string str = string.Format("{0},{1},{2},{3},{4}", Loyee.id, Loyee.usercode, Loyee.name, Loyee.IcCardNo, Loyee.RoleID);
                        idList.Add(str);
                    }
                    idArray = idList.ToArray();
                }
                return true;

            }
            catch (Exception ex)
            {
                RecvValue = ex.Message;
                errMsg = $"上位机获取人员信息异常 \r\nMsg:{ex.Message}";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACEMPLOYEE";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

                MesOperate.SaveLogData("MES上位机获取人员信息", text);
            }
            return false;
        }

        #endregion

        #region 用户信息验证 通讯OK
        public bool ACUSERINFO_Main(ResourcesStruct rs, string Id, string Pw, int roleID, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            ACUSERINFO_Request AcuserRest = new ACUSERINFO_Request();
            try
            {
                if (!GetInstance().UpdataMes)
                {
                    return true;
                }
                if (!GetInstance().isMESConnect)
                {
                    return false;
                }
                var ACUSERINFO = new
                {
                    Software = "烘烤调度线体",
                    AutoFlag = GetInstance().UpdataMes,
                    EmployeeNo = rs.OperatorUserID,

                    EquipmentInfo = new object[1]

                        {
                            new
                            {
                            EquipmentCode=rs.EquipmentCode,
                            EmployeeNo = Id,
                            Password= Pw,
                            RoleID = roleID==0?"ADMIN":roleID==1?"TECHNICIAN":roleID==2?"OPERATOR":"",
                            }
                        }
                };

                string jsonRequest = JsonConvert.SerializeObject(ACUSERINFO);
                SendValue = MesOperate.RevertJsonString(jsonRequest);

                wcf_Client.SendMsg("ACUSERINFO", jsonRequest, ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    //if (revObj.ContainsKey())
                    {
                        result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["ResultFlag"]);
                    }
                    //if (revObj.ContainsKey("Message"))
                    {
                        msg = revObj["EquipmentInfo"][0]["Message"].ToString();
                    }
                    if (!result)
                    {
                        errMsg = $"用户信息验证失败! \r\nMsg:{msg}，\r\nerrorMessage:{errorMessage}";

                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                RecvValue = ex.Message;
                errMsg = $"用户信息验证异常！ \r\nMsg:请检查网线和MES网络通讯问题";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACUSERINFO";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

                MesOperate.SaveLogData("MES用户信息验证", text);
            }
            return false;
        }
        #endregion

        #region 单电芯校验接口
        public bool ACPROCESSCHECK_Main(ResourcesStruct rs, string code, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            return true;
            try
            {
                //if (!GetInstance().UpdataMes)
                //{
                //    return true;
                //}
                //if (!GetInstance().MesCheck)
                //{
                //    return true;
                //}
                var ACLOGONCHECK = new
                {
                    AutoFlag = GetInstance().UpdataMes,
                    Software = "烘烤调度线体",
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                    {
                                new
                                {
                                EquipmentCode=rs.EquipmentCode,
                                OprSequenceNo=rs.ProcessCode,
                                SerialNos= new object[1]
                                {
                                new
                                {
                                    SerialNo = code,
                                }
                                },
                                PastSerialNos = new object[1]
                                {
                                    new
                                    {
                                        SerialNo =""
                                    }
                                }
                                }
                    }
                };
                string jsonRequest = JsonConvert.SerializeObject(ACLOGONCHECK);
                SendValue = MesOperate.RevertJsonString(jsonRequest);

                if (GetInstance().UpdataMes)
                {
                    wcf_Client.SendMsg("ACPROCESSCHECK", jsonRequest, ref jsonResponse, ref errorMessage);
                }
                RecvValue = MesOperate.RevertJsonString(jsonResponse);

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    if (revObj.ContainsKey("EquipmentInfo"))
                    {
                        revObj = JObject.Parse(revObj["EquipmentInfo"][0].ToString());
                        if (revObj.ContainsKey("ResultFlag"))
                            result = Convert.ToBoolean(revObj["ResultFlag"]);
                        if (revObj.ContainsKey("SerialNos"))
                        {
                            revObj = JObject.Parse(revObj["SerialNos"][0].ToString());
                            if (revObj.ContainsKey("Message"))
                                msg = revObj["Message"].ToString();
                        }
                    }
                    if (!result)
                    {
                        errMsg = $"电芯入站校验失败! \r\nMsg:{msg},\r\nerrorMessage:{errorMessage}";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMsg = $"电芯入站校验失败! \r\nMsg:{msg},\r\nerrorMessage:{errorMessage},\r\nex:{ex.ToString()}";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = code;
                string linecode = rs.ProcessCode;
                string mesUri = "ACPROCESSCHECK";
                string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{sfcode},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";
                SaveLogPullInData("MES入站校验", text);
            }
            if (!GetInstance().MesCheck || !GetInstance().UpdataMes)
            {
                return true;
            }
            return false;

        }
        #endregion 
        #region 单电芯入站接口
        public bool ACLOGONCHECK_Main(ResourcesStruct rs, string batCode, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!GetInstance().UpdataMes)
                {
                    return true;
                }
                var ACLOGONCHECK = new
                {
                    AutoFlag = GetInstance().UpdataMes,
                    Software = "烘烤调度线体",
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                    {
                                new
                                {
                                EquipmentCode=rs.EquipmentCode,
                                RequestType="1",
                                Container="",
                                InContainer="",
                                OpFlag="0",
                                OperationMark=rs.ProcessCode,
                                SerialNos= new object[1]
                                {
                                    new
                                    {
                                     SerialNo = batCode,
                                     GetProductTypeFlag = false,
                                     SlotID = 0,
                                     IsRealFlag = true,
                                    }

                                },
                                PastSerialNos = new object[1]
                                {
                                    new
                                    {
                                        SerialNo =""
                                    }
                                }
                                }
                    }
                };
                string jsonRequest = JsonConvert.SerializeObject(ACLOGONCHECK);
                SendValue = MesOperate.RevertJsonString(jsonRequest);

                //// 离线上传
                //if (!isMESConnect)
                //{
                //    SaveMesData(MesInterface.EquToMesInBaking, jsonRequest);
                //    errMsg = $"电芯入站校验失败! \r\nMsg:MES心跳掉线，请检查原因";
                //    return false;
                //}
                wcf_Client.SendMsg("ACLOGONCHECK", jsonRequest, ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    if (revObj.ContainsKey("EquipmentInfo"))
                    {
                        revObj = JObject.Parse(revObj["EquipmentInfo"][0].ToString());
                        if (revObj.ContainsKey("Result"))
                            result = Convert.ToBoolean(revObj["Result"]);
                        if (revObj.ContainsKey("SerialNos"))
                        {
                            revObj = JObject.Parse(revObj["SerialNos"][0].ToString());
                            if (revObj.ContainsKey("Message"))
                                msg = revObj["Message"].ToString();
                        }
                    }
                    //if (!GetInstance().MesCheck)
                    //{
                    //    return true;
                    //}
                    if (!result)
                    {
                        errMsg = $"电芯入站失败! \r\nMsg:{msg},\r\nerrorMessage:{errorMessage}";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMsg = $"电芯入站失败! \r\nMsg:{msg},\r\nerrorMessage:{errorMessage},\r\nex:{ex.ToString()}";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = "";
                string linecode = rs.ProcessCode;
                string mesUri = "ACLOGONCHECK";
                string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{sfcode},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";
                SaveLogPullInData("MES入站校验", text);
            }
            if (!GetInstance().MesCheck)
            {
                return true;
            }
            return false;
        }
        #endregion
        #region 单电芯出站接口
        public bool ACLOGOFF_Main(ResourcesStruct rs, string pltCode, string batCode, object[] Parameters, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;

            try
            {
                if (!GetInstance().UpdataMes)
                {
                    return true;
                }
                object[] Outputs = new object[1];
                for (int i = 0; i < Outputs.Length; i++)
                {
                    Outputs[i] = new
                    {
                        SerialNo = batCode,           //电芯条码
                        PreSerialNo = "",
                        SlotID = i + 1,
                        IsRealFlag = true,      //是否为真电池
                        ProductType = "",
                        Station = "A1",
                        PassFlag = "true",
                        ProcessFlag = "1",
                        MatchingInfo = new object[0]   //配对电芯信息  烘烤勿传
                            {
                            },
                        StationInfo = new object[0]     //工位清单  接口清单显示可为空
                            {
                            },
                        ProcessSteps = new object[1]    //加工工步信息  
                            {
                                new
                                {
                                    StepID = "1",
                                    StepStatus = "OK",
                                }
                            },
                        SpartInfo = new object[0]  //零部件信息  烘烤勿传
                            {
                            },
                        MaterialInfo = new object[0]  //物料信息  烘烤勿传
                            {
                            },
                        Parameters = Parameters,
                    };
                }
                var ACLOGOFF = new
                {
                    Software = "烘烤调度线体",
                    AutoFlag = GetInstance().UpdataMes,
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                    {
                        new
                        {
                        EquipmentCode=rs.EquipmentCode,
                        Container = pltCode.Trim(),  //托盘号
                        NextEquipmentCode="",
                        ProcessResult=false,
                        OperationMark = "YZ211",
                        ProcessMessage = "",
                        Outputs = Outputs
                        }
                        }
                };
                string jsonRequest = JsonConvert.SerializeObject(ACLOGOFF);
                SendValue = MesOperate.RevertJsonString(jsonRequest);     //发送信息
                wcf_Client.SendMsg("ACLOGOFF", jsonRequest, ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);    //接收信息

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    if (revObj.ContainsKey("EquipmentInfo"))
                    {
                        revObj = JObject.Parse(revObj["EquipmentInfo"][0].ToString());
                        if (revObj.ContainsKey("ResultFlag"))
                            result = Convert.ToBoolean(revObj["ResultFlag"]);
                        if (revObj.ContainsKey("Message"))
                            msg = revObj["Message"].ToString();
                    }
                    if (!result)
                    {
                        errMsg = $"夹具电芯出站失败! \r\nMsg:{msg},errorMessage:{errorMessage}";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMsg = $"夹具电芯出站异常！ \r\nMsg:{msg},errorMessage:{errorMessage},ex:{ex.ToString()}";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACLOGOFF";
                string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{sfcode},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";
                SaveLogData("MES出站", text);
            }

            return false;
        }
        #endregion
        #region 单电芯托盘绑定/解绑接口
        /// <summary>
        /// MES--> 托盘电芯绑定与解绑
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="plt"></param>
        /// <param name="bindingFlag">绑定标识 Ture为绑定，Flase为只返回校验结果，不绑定</param>
        /// <param name="opFlag">Ture为绑定托盘，Flase为解绑托盘</param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public bool ACINBOUNDByOne_Main(ResourcesStruct rs, string pltCode, string batCode, int batCodeIdx, bool bindingFlag, bool opFlag, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            object[] sfcs = new object[1];
            sfcs[0] = batCode;
            try
            {

                // 逻辑OK注释
                object[] ProductInfo = new object[1];
                ProductInfo[0] = new
                {
                    SerialNo = batCode,        //电芯条码
                    SlotID = batCodeIdx,            //电芯在托盘的位置
                    IsRealFlag = true      //是否为真电池
                };
                var ACINBOUND = new
                {
                    Software = "烘烤调度线体",
                    AutoFlag = GetInstance().UpdataMes,
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                        {
                        new
                        {
                        EquipmentCode=rs.EquipmentCode,
                        Container=pltCode.Trim(),                           //托盘号
                        InContainer="",                                   //父容器编号
                        BindingFlag= bindingFlag,                         //绑定标识 Ture为绑定，Flase为只返回校验结果，不绑定
                        OpFlag=opFlag,                                    //Ture为绑定托盘，Flase为解绑托盘
                        ProductInfo = ProductInfo
                        }
                        }
                };
                //序列化
                string jsonRequest = JsonConvert.SerializeObject(ACINBOUND);
                SendValue = MesOperate.RevertJsonString(jsonRequest);     //发送信息
                if (GetInstance().UpdataMes && MachineCtrl.GetInstance().isMESConnect)
                {
                    wcf_Client.SendMsg("ACINBOUND", jsonRequest, ref jsonResponse, ref errorMessage);
                }
                RecvValue = MesOperate.RevertJsonString(jsonResponse);    //接收信息

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    if (revObj.ContainsKey("EquipmentInfo"))
                    {
                        revObj = JObject.Parse(revObj["EquipmentInfo"][0].ToString());
                        if (revObj.ContainsKey("ResultFlag"))
                            result = Convert.ToBoolean(revObj["ResultFlag"]);
                        if (revObj.ContainsKey("Message"))
                            msg = revObj["Message"].ToString();
                    }
                    if (!result)
                    {
                        errMsg = $"托盘电芯绑定与解绑失败! \r\nMsg:{msg},errorMessage:{errorMessage}";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMsg = $"托盘电芯绑定与解绑异常！ \r\nMsg:{msg},errorMessage:{errorMessage},ex:{ex.ToString()}";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACINBOUND";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

                MesOperate.SaveLogData("MES托盘电芯绑定与解绑", text);
            }

            return false;
        }
        #endregion
        #region 满盘入站接口
        public bool ACLOGONCHECK_Main(ResourcesStruct rs, Pallet plt, string EquipmentCode, string softName, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;

            //if (!MachineCtrl.GetInstance().isMESConnect)
            //{
            //    return false;
            //}
            try
            {
                int batRow = plt.MaxRow;
                int batCol = plt.MaxCol;
                var bindCon = new List<string>();
                for (int row = 0; row < batRow; row++)
                {
                    for (int col = 0; col < batCol; col++)
                    {
                        if (plt.Battery[row, col].Type == BatteryStatus.OK)
                        {
                            string sfc = plt.Battery[row, col].Code.Trim();
                            bindCon.Add(sfc);
                        }
                    }
                }
                object[] sfcs = new object[batRow * batCol];
                sfcs = bindCon.ToArray();

                object[] SerialNos = new object[sfcs.Length];
                for (int i = 0; i < SerialNos.Length; i++)
                {
                    SerialNos[i] = new
                    {
                        SerialNo = sfcs[i],
                        GetProductTypeFlag = false,
                        SlotID = i + 1,
                        IsRealFlag = true,
                    };
                }

                var ACLOGONCHECK = new
                {
                    AutoFlag = GetInstance().UpdataMes,
                    Software = "烘烤调度线体",
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                    {
                                new
                                {
                                EquipmentCode=EquipmentCode,
                                RequestType="1",
                                Container="",
                                InContainer="",
                                OpFlag="0",
                                OperationMark=rs.ProcessCode,
                                SerialNos= SerialNos,
                                PastSerialNos = new object[1]
                                {
                                    new
                                    {
                                        SerialNo =""
                                    }
                                }
                                }
                    }
                };
                string jsonRequest = JsonConvert.SerializeObject(ACLOGONCHECK);
                SendValue = MesOperate.RevertJsonString(jsonRequest);
                if (GetInstance().UpdataMes)
                {
                    wcf_Client.SendMsg("ACLOGONCHECK", jsonRequest, ref jsonResponse, ref errorMessage);
                }
                //if (string.IsNullOrEmpty(jsonResponse))
                //{
                //    if (GetInstance().UpdataMes)
                //    {
                //        wcf_Client.SendMsg("ACLOGONCHECK", jsonRequest, ref jsonResponse, ref errorMessage);
                //    }
                //}
                msg += string.Format("errorMessage:{0}", errorMessage);
                //RecvValue = MesOperate.RevertJsonString(jsonResponse);

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    if (revObj.ContainsKey("EquipmentInfo"))
                    {
                        revObj = JObject.Parse(revObj["EquipmentInfo"][0].ToString());
                        if (revObj.ContainsKey("Result"))
                            result = Convert.ToBoolean(revObj["Result"]);
                        if (revObj.ContainsKey("SerialNos"))
                        {
                            revObj = JObject.Parse(revObj["SerialNos"][0].ToString());
                            if (revObj.ContainsKey("Message"))
                                msg += string.Format("Message:{0}", revObj["Message"].ToString());
                        }
                    }
                    if (!result)
                    {
                        errMsg = $"电芯入站校验失败! \r\nMsg:{msg},\r\nerrorMessage:{errorMessage}";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMsg = $"电芯入站校验失败! \r\nMsg:{msg},\r\nerrorMessage:{errorMessage},\r\nex:{ex.ToString()}";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string tryCode = plt.Code;
                string linecode = rs.ProcessCode;
                string mesUri = "ACLOGONCHECK";
                msg = MesOperate.RevertJsonString(msg);
                string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{tryCode},{mesUri},{second},{result},{msg},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";
                if (result)
                {
                    SaveLogData("MES入站", text);

                    int batRow = plt.MaxRow;
                    int batCol = plt.MaxCol;
                    var bindCon = new List<string>();
                    for (int row = 0; row < batRow; row++)
                    {
                        for (int col = 0; col < batCol; col++)
                        {
                            if (plt.Battery[row, col].Type == BatteryStatus.OK)
                            {
                                string sfc = plt.Battery[row, col].Code.Trim();
                                string textRe = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{EquipmentCode},{tryCode},{sfc}";
                                SaveLogReData("入站电芯记录", textRe);
                            }
                        }
                    }
                }
                else
                {
                    SaveLogNGData("MES入站离线", text, plt.Code);
                    unLineDataListHander("In", text, plt.Code);
                    int batRow = plt.MaxRow;
                    int batCol = plt.MaxCol;
                    var bindCon = new List<string>();
                    for (int row = 0; row < batRow; row++)
                    {
                        for (int col = 0; col < batCol; col++)
                        {
                            if (plt.Battery[row, col].Type == BatteryStatus.OK)
                            {
                                string sfc = plt.Battery[row, col].Code.Trim();
                                string textRe = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{EquipmentCode},{tryCode},{sfc}";
                                SaveLogReData("入站失败电芯记录", textRe);
                            }
                        }
                    }
                }
            }

            if (!GetInstance().MesCheck || !GetInstance().UpdataMes)
            {
                return true;
            }

            return false;
        }
        #endregion 
        #region 满盘入站取消接口
        public bool CANCELINBOUND_Main(ResourcesStruct rs, Pallet plt, string EquipmentCode, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;

            //if (!MachineCtrl.GetInstance().isMESConnect)
            //{
            //    return false;
            //}
            try
            {
                int batRow = plt.MaxRow;
                int batCol = plt.MaxCol;
                var bindCon = new List<string>();
                for (int row = 0; row < batRow; row++)
                {
                    for (int col = 0; col < batCol; col++)
                    {
                        if (plt.Battery[row, col].Type == BatteryStatus.OK)
                        {
                            string sfc = plt.Battery[row, col].Code.Trim();
                            bindCon.Add(sfc);
                        }
                    }
                }
                object[] sfcs = new object[batRow * batCol];
                sfcs = bindCon.ToArray();

                object[] SerialNos = new object[sfcs.Length];
                for (int i = 0; i < SerialNos.Length; i++)
                {
                    SerialNos[i] = new
                    {
                        SerialNo = sfcs[i],
                        GetProductTypeFlag = false,
                        SlotID = i + 1,
                        IsRealFlag = true,
                    };
                }

                var ACLOGONCHECK = new
                {
                    AutoFlag = GetInstance().UpdataMes,
                    Software = "烘烤调度线体",
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                    {
                                new
                                {
                                EquipmentCode=EquipmentCode,
                                RequestType="0",
                                Container="",
                                InContainer="",
                                OpFlag="0",
                                OperationMark=rs.ProcessCode,
                                SerialNos= SerialNos,
                                PastSerialNos = new object[1]
                                {
                                    new
                                    {
                                        SerialNo =""
                                    }
                                }
                                }
                    }
                };
                string jsonRequest = JsonConvert.SerializeObject(ACLOGONCHECK);
                SendValue = MesOperate.RevertJsonString(jsonRequest);
                if (GetInstance().UpdataMes)
                {
                    wcf_Client.SendMsg("CANCELINBOUND", jsonRequest, ref jsonResponse, ref errorMessage);
                }
                RecvValue = MesOperate.RevertJsonString(jsonResponse);

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    if (revObj.ContainsKey("EquipmentInfo"))
                    {
                        revObj = JObject.Parse(revObj["EquipmentInfo"][0].ToString());
                        if (revObj.ContainsKey("Result"))
                            result = Convert.ToBoolean(revObj["Result"]);
                        if (revObj.ContainsKey("SerialNos"))
                        {
                            revObj = JObject.Parse(revObj["SerialNos"][0].ToString());
                            if (revObj.ContainsKey("Message"))
                                msg = revObj["Message"].ToString();
                        }
                    }
                    if (!result)
                    {
                        errMsg = $"电芯取消入站失败! \r\nMsg:{msg},\r\nerrorMessage:{errorMessage}";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMsg = $"电芯取消入站失败! \r\nMsg:{msg},\r\nerrorMessage:{errorMessage},\r\nex:{ex.ToString()}";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string tryCode = plt.Code;
                string linecode = rs.ProcessCode;
                string mesUri = "CANCELINBOUND";
                string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{tryCode},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";
                SaveLogData("MES取消入站", text);
            }
            if (!GetInstance().MesCheck)
            {
                return true;
            }
            return false;
        }
        #endregion 
        #region 满盘出站接口
        public bool ACLOGOFF_Main(ResourcesStruct rs, Pallet plt, object[] Parameters, string EquipmentCode, string softName, ref string errMsg)
        {
            string RecvValue = "";
            string RecvValueByBuf = "";
            string jsonRequestByBuf = "";
            string SendValue = "";
            string SendValueByBuf = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            string equipmentCode = "";
            string container = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            int batRow = plt.MaxRow;
            int batCol = plt.MaxCol;
            var bindCon = new List<string>();
            for (int row = 0; row < batRow; row++)
            {
                for (int col = 0; col < batCol; col++)
                {
                    if (plt.Battery[row, col].Type == BatteryStatus.OK)
                    {
                        string sfc = plt.Battery[row, col].Code.Trim();
                        bindCon.Add(sfc);
                    }
                }
            }
            object[] sfcs = new object[batRow * batCol];
            sfcs = bindCon.ToArray();
            try
            {
                object[] Outputs = new object[sfcs.Length];
                for (int i = 0; i < Outputs.Length; i++)
                {
                    Outputs[i] = new
                    {
                        SerialNo = sfcs[i],           //电芯条码
                        PreSerialNo = "",
                        SlotID = i + 1,
                        IsRealFlag = true,      //是否为真电池
                        ProductType = "",
                        Station = "A1",
                        PassFlag = "true",
                        ProcessFlag = "1",
                        MatchingInfo = new object[0]   //配对电芯信息  烘烤勿传
                            {
                            },
                        StationInfo = new object[0]     //工位清单  接口清单显示可为空
                            {
                            },
                        ProcessSteps = new object[1]    //加工工步信息  
                            {
                                new
                                {
                                    StepID = "1",
                                    StepStatus = "OK",
                                }
                            },
                        SpartInfo = new object[0]  //零部件信息  烘烤勿传
                            {
                            },
                        MaterialInfo = new object[0]  //物料信息  烘烤勿传
                            {
                            },
                        Parameters = Parameters,
                    };
                }
                object[] OutputsByBuf = new object[sfcs.Length];
                for (int i = 0; i < OutputsByBuf.Length; i++)
                {
                    OutputsByBuf[i] = new
                    {
                        SerialNo = sfcs[i],           //电芯条码
                        PreSerialNo = "",
                        SlotID = i + 1,
                        IsRealFlag = true,      //是否为真电池
                        ProductType = "",
                        Station = "A1",
                        PassFlag = "true",
                        ProcessFlag = "1",
                        MatchingInfo = new object[0]   //配对电芯信息  烘烤勿传
                            {
                            },
                        StationInfo = new object[0]     //工位清单  接口清单显示可为空
                            {
                            },
                        ProcessSteps = new object[1]    //加工工步信息  
                            {
                                new
                                {
                                    StepID = "1",
                                    StepStatus = "OK",
                                }
                            },
                        SpartInfo = new object[0]  //零部件信息  烘烤勿传
                            {
                            },
                        MaterialInfo = new object[0]  //物料信息  烘烤勿传
                            {
                            },
                        //Parameters = Parameters,
                    };
                }

                var ACLOGOFF = new
                {
                    Software = softName,
                    AutoFlag = GetInstance().UpdataMes,
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                    {
                        new
                        {
                        EquipmentCode=EquipmentCode,
                        Container = plt.Code.Trim(),  //托盘号
                        NextEquipmentCode="",
                        ProcessResult=false,
                        OperationMark = "YZ211",
                        ProcessMessage = "",
                        Outputs = Outputs
                        }
                        }
                };
                var ACLOGOFFByBuf = new
                {
                    Software = softName,
                    AutoFlag = GetInstance().UpdataMes,
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                    {
                        new
                        {
                        EquipmentCode=EquipmentCode,
                        Container = plt.Code.Trim(),  //托盘号
                        NextEquipmentCode="",
                        ProcessResult=false,
                        OperationMark = "YZ211",
                        ProcessMessage = "",
                        Outputs = OutputsByBuf,
                          Parameters = Parameters
                        }
                        }
                };
                //序列化
                string jsonRequest = JsonConvert.SerializeObject(ACLOGOFF);
                //SendValue = MesOperate.RevertJsonString(jsonRequest);     //发送信息
                jsonRequestByBuf = JsonConvert.SerializeObject(ACLOGOFFByBuf).ToString();
                SendValueByBuf = MesOperate.RevertJsonString(jsonRequestByBuf);     //发送信息
                if (GetInstance().UpdataMes)
                {
                    wcf_Client.SendMsg("ACLOGOFF", jsonRequest, ref jsonResponse, ref errorMessage);
                }
                //if (string.IsNullOrEmpty(jsonResponse))
                //{
                //    if (GetInstance().UpdataMes)
                //    {
                //        wcf_Client.SendMsg("ACLOGOFF", jsonRequest, ref jsonResponse, ref errorMessage);
                //    }
                //}
                msg += string.Format("errorMessage:{0}", errorMessage);
                //RecvValue = MesOperate.RevertJsonString(jsonResponse);    //接收信息

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    if (revObj.ContainsKey("EquipmentInfo"))
                    {
                        revObj = JObject.Parse(revObj["EquipmentInfo"][0].ToString());
                        if (revObj.ContainsKey("ResultFlag"))
                            result = Convert.ToBoolean(revObj["ResultFlag"]);
                        if (revObj.ContainsKey("Message"))
                            msg += string.Format("Message:{0}", revObj["Message"].ToString());
                    }
                    if (!result)
                    {
                        errMsg = $"夹具电芯出站失败! \r\nMsg:{msg},errorMessage:{errorMessage}";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMsg = $"夹具电芯出站异常！ \r\nMsg:{msg},errorMessage:{errorMessage},ex:{ex.ToString()}";
            }
            finally
            {
                //上传内容和返回信息划分10等份
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string tryCode = plt.Code;
                string linecode = rs.ProcessCode;
                string mesUri = "ACLOGOFF";
                msg = MesOperate.RevertJsonString(msg);
                string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{tryCode},{mesUri},{second},{result},{msg},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValueByBuf}";
                if (result)
                {
                    SaveLogData("MES出站", text);

                    batRow = plt.MaxRow;
                    batCol = plt.MaxCol;
                    bindCon = new List<string>();
                    for (int row = 0; row < batRow; row++)
                    {
                        for (int col = 0; col < batCol; col++)
                        {
                            if (plt.Battery[row, col].Type == BatteryStatus.OK)
                            {
                                string sfc = plt.Battery[row, col].Code.Trim();
                                string textRe = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{EquipmentCode},{tryCode},{sfc}";
                                SaveLogReData("出站电芯记录", textRe);
                            }
                        }
                    }

                }
                else
                {

                    SaveLogNGData("MES出站离线", text, plt.Code);
                    unLineDataListHander("Out", text, plt.Code);

                    batRow = plt.MaxRow;
                    batCol = plt.MaxCol;
                    bindCon = new List<string>();
                    for (int row = 0; row < batRow; row++)
                    {
                        for (int col = 0; col < batCol; col++)
                        {
                            if (plt.Battery[row, col].Type == BatteryStatus.OK)
                            {
                                string sfc = plt.Battery[row, col].Code.Trim();
                                string textRe = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{EquipmentCode},{tryCode},{sfc}";
                                SaveLogReData("出站失败电芯记录", textRe);
                            }
                        }
                    }
                }
            }
            if (!GetInstance().MesCheck)
            {
                return true;
            }
            return false;
        }
        #endregion
        #region 满盘托盘绑定/解绑接口
        /// <summary>
        /// MES--> 托盘电芯绑定与解绑
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="plt"></param>
        /// <param name="bindingFlag">绑定标识 Ture为绑定，Flase为只返回校验结果，不绑定</param>
        /// <param name="opFlag">Ture为绑定托盘，Flase为解绑托盘</param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public bool ACINBOUND_Main(ResourcesStruct rs, Pallet plt, string EquipmentCode, string softName, bool bindingFlag, bool opFlag, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            int batRow = plt.MaxRow;
            int batCol = plt.MaxCol;
            var bindCon = new List<string>();
            for (int row = 0; row < batRow; row++)
            {
                for (int col = 0; col < batCol; col++)
                {
                    if (plt.Battery[row, col].Type == BatteryStatus.OK)
                    {
                        string sfc = plt.Battery[row, col].Code.Trim();
                        bindCon.Add(sfc);
                    }
                }
            }
            object[] sfcs = new object[batRow * batCol];
            sfcs = bindCon.ToArray();
            try
            {
                // 逻辑OK注释
                object[] ProductInfo = new object[sfcs.Length];
                for (int i = 0; i < ProductInfo.Length; i++)
                {
                    ProductInfo[i] = new
                    {
                        SerialNo = sfcs[i],           //电芯条码
                        SlotID = i + 1,            //电芯在托盘的位置
                        IsRealFlag = true      //是否为真电池
                    };
                }
                var ACINBOUND = new
                {
                    Software = softName,
                    AutoFlag = GetInstance().UpdataMes,
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                        {
                        new
                        {
                        EquipmentCode=EquipmentCode,                     //设备编码
                        Container=plt.Code,                           //托盘号
                        InContainer="",                                   //父容器编号
                        BindingFlag= bindingFlag,                         //绑定标识 Ture为绑定，Flase为只返回校验结果，不绑定
                        OpFlag=opFlag,                                    //Ture为绑定托盘，Flase为解绑托盘
                        ProductInfo = ProductInfo
                        }
                        }
                };
                //序列化
                string jsonRequest = JsonConvert.SerializeObject(ACINBOUND);
                SendValue = MesOperate.RevertJsonString(jsonRequest);     //发送信息
                if (GetInstance().UpdataMes)
                {
                    wcf_Client.SendMsg("ACINBOUND", jsonRequest, ref jsonResponse, ref errorMessage);
                }
                //if (string.IsNullOrEmpty(jsonResponse))
                //{
                //    if (GetInstance().UpdataMes)
                //    {
                //        wcf_Client.SendMsg("ACINBOUND", jsonRequest, ref jsonResponse, ref errorMessage);
                //    }
                //}
                msg += string.Format("errorMessage:{0}", errorMessage);
                //RecvValue = MesOperate.RevertJsonString(jsonResponse);    //接收信息

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    if (revObj.ContainsKey("EquipmentInfo"))
                    {
                        revObj = JObject.Parse(revObj["EquipmentInfo"][0].ToString());
                        if (revObj.ContainsKey("ResultFlag"))
                            result = Convert.ToBoolean(revObj["ResultFlag"]);
                        if (revObj.ContainsKey("Message"))
                            msg += string.Format("Message:{0}", revObj["Message"].ToString());
                    }
                    if (!result)
                    {
                        errMsg = $"托盘电芯绑定与解绑失败! \r\nMsg:{msg},errorMessage:{errorMessage}";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errMsg = $"托盘电芯绑定与解绑异常！ \r\nMsg:{msg},errorMessage:{errorMessage},ex:{ex.ToString()}";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string tryCode = plt.Code;
                string linecode = rs.ProcessCode;
                string mesUri = "ACINBOUND";
                msg = MesOperate.RevertJsonString(msg);
                string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{tryCode},{mesUri},{second},{result},{msg},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";
                if (result)
                {
                    if (opFlag)
                        SaveLogData("MES托盘电芯绑定", text);
                    else
                        SaveLogData("MES托盘电芯解绑", text);
                }
                else
                {
                    if (opFlag)
                    {
                        SaveLogNGData("MES托盘电芯绑定离线", text, plt.Code);
                        unLineDataListHander("Bind", text, plt.Code);
                    }
                    else
                    {

                        SaveLogNGData("MES托盘电芯解绑离线", text, plt.Code);
                        unLineDataListHander("UnBind", text, plt.Code);
                    }
                }
            }
            if (!GetInstance().MesCheck || !GetInstance().UpdataMes)
            {
                return true;
            }
            return false;

        }
        #endregion

        #region 设备状态变更 通讯和业务OK
        public bool ACEQPTSTUS_Main(ResourcesStruct rs, bool status, string equipmentStatusID, string EquipmentCode, int cavityIdx, string softName, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!GetInstance().UpdataMes || !GetInstance().MesCheck)
                {
                    return true;
                }
                var ACEQPTSTUS = new
                {
                    Software = softName,
                    AutoFlag = GetInstance().UpdataMes,
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                        {
                        new
                        {
                        EquipmentCode=EquipmentCode,
                        OpFlag="2",
                        EquipmentModel= status ?"CONT":"MONT",
                        EquipmentStatusID = equipmentStatusID,
                        ReasonCode="900-001",
                        Description="AutoAIarmLeveI1",
                        Location=cavityIdx.ToString()
                        }
                        }
                };
                string jsonRequest = JsonConvert.SerializeObject(ACEQPTSTUS);
                SendValue = MesOperate.RevertJsonString(jsonRequest);
                wcf_Client.SendMsg("ACEQPTSTUS", jsonRequest, ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    //if (revObj.ContainsKey())
                    {
                        result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["Result"]);
                    }
                    //if (revObj.ContainsKey("Message"))
                    {
                        msg = revObj["EquipmentInfo"][0]["Message"].ToString();
                    }
                    if (!result)
                    {
                        errMsg = $"设备状态变更失败! \r\nMsg:{msg}";
                        return false;

                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                RecvValue = ex.Message;
                errMsg = $"设备状态变更异常！ \r\nMsg:请检查网线和MES网络通讯问题";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACEQPTSTUS";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

                MesOperate.SaveLogData("MES设备状态变更", text);
            }
            return false;
        }
        #endregion

        #region 设备主动获取参数 手动输入工艺路线代码和版本号获取运行参数并保存在本地
        public bool ACEQPTPARM_Main(ResourcesStruct rs, ref MesRecipeStruct mesRecipeStruct, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            int num = 0;
            var result = false;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!GetInstance().UpdataMes)
                {
                    return true;
                }
                var ACEQPTPARM = new
                {
                    EmployeeNo = rs.OperatorUserID,
                    AutoFlag = GetInstance().UpdataMes,
                    Software = "烘烤调度线体",
                    EquipmentInfo = new object[1]
                        {
                        new
                        {
                        EquipmentCode="WH02C0122PR-HKX00212",
                        ProcessCode=mesRecipeStruct.RecipeCode,
                        Version=mesRecipeStruct.Version,
                        OprSequenceNo =mesRecipeStruct.OprSequenceNo ,
                        }
                        }
                };
                //序列化
                string jsonRequest = JsonConvert.SerializeObject(ACEQPTPARM);
                SendValue = MesOperate.RevertJsonString(jsonRequest);     //发送信息

                wcf_Client.SendMsg("ACEQPTPARM", jsonRequest, ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);    //接收信息

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    //if (revObj.ContainsKey())
                    {
                        //result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["Result"]);
                        result = true;
                        //获取参数的数量
                        num = revObj["EquipmentInfo"][0]["StepInfo"][0]["ParameterInfo"].Count();
                        mesRecipeStruct.Param = new List<MesParameterData>();

                        //循环遍历将参数添加到集合里面
                        //for (int i = 0; i < num; i++)
                        //{
                        //    var str = revObj["EquipmentInfo"][0]["StepInfo"][0]["ParameterInfo"][i].ToString();
                        //    //反序列化
                        //    ParameterData InfoList = JsonConvert.DeserializeObject<ParameterData>(str);
                        //    MesParameterData paramData = new MesParameterData();
                        //    paramData.ParameterCode = InfoList.ParameterCode;
                        //    paramData.ParameterType = InfoList.ParameterType;
                        //    paramData.TargetValue = InfoList.TargetValue;
                        //    paramData.UomCode = InfoList.UomCode;
                        //    paramData.UpperControlLimit = InfoList.UpperControlLimit;
                        //    paramData.LowerControlLimit = InfoList.LowerControlLimit;
                        //    paramData.Description = InfoList.Description;

                        //    mesRecipeStruct.Param.Add(paramData);


                        //}



                    }
                    //if (revObj.ContainsKey("Message"))
                    {
                        //msg = revObj["EquipmentInfo"][0]["Message"].ToString();
                    }

                    if (!result)
                    {
                        errMsg = $"设备主动获取参数失败! \r\nMsg:{msg}";
                        return false;

                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                RecvValue = ex.Message;
                errMsg = $"设备主动获取参数异常！ \r\nMsg:请检查网线和MES网络通讯问题";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACEQPTPARM";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

                MesOperate.SaveLogData("MES设备主动获取参数", text);
            }

            return false;
        }
        #endregion

        #region 设备报警 通讯OK
        public bool ACEQPTALRT_Main(ResourcesStruct rs, string almCode, string almMsg, MesAlarmStatus state, MesAlarmLevel almLevel, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!GetInstance().UpdataMes)
                {
                    return true;
                }
                var ACEQPTALRT = new
                {
                    Software = "烘烤调度线体",
                    AutoFlag = GetInstance().UpdataMes,
                    EmployeeNo = rs.OperatorUserID,
                    EquipmentInfo = new object[1]
                        {
                        new
                        {
                        EquipmentCode=rs.EquipmentCode,
                        AlertInfo=new object[1]
                        {
                        new
                        {
                        AlertCode = almCode,            //报警编码  自己编辑
                        AlertReset = $"{(int)state}",
                        AlertDescription = almMsg,
                        AlertLevel = $"{(int)almLevel}",
                        AlertID=/*"CNZJTB0120220804",*/ rs.EquipmentCode + DateTime.Now.ToString("yyyyMMddHHmmss"),
                        AlertLocation="",  //报警位置,就是设备库位，目前为空
                        SubEquipmentCode=""
                        }
                        }
                        }
                        }
                };
                string jsonRequest = JsonConvert.SerializeObject(ACEQPTALRT);
                SendValue = MesOperate.RevertJsonString(jsonRequest);

                wcf_Client.SendMsg("ACEQPTALRT", jsonRequest, ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    //if (revObj.ContainsKey())
                    {
                        result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["Result"]);
                    }
                    //if (revObj.ContainsKey("Message"))
                    {
                        msg = revObj["EquipmentInfo"][0]["Message"].ToString();
                    }
                    if (!result)
                    {
                        errMsg = $"设备报警上传失败! \r\nMsg:{msg}";
                        return false;

                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                RecvValue = ex.Message;
                errMsg = $"设备报警上传异常！ \r\nMsg:请检查网线和MES网络通讯问题";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACEQPTALRT";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

                MesOperate.SaveLogData("MES设备报警", text);
            }

            return false;
        }
        #endregion

        #region 设备初始化(参数下发) 工单换型时MES下发参数，上位机只接收
        /// <summary>
        /// MES--> 设备初始化(参数下发)
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public bool ACEQPTINIT_Main(ResourcesStruct rs, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";

            var result = false;

            DateTime startTime = DateTime.Now;
            try
            {
                if (!GetInstance().UpdataMes)
                {
                    return true;
                }
                var ACEQPTINIT = new
                {
                    AutoFlag = GetInstance().UpdataMes,
                    Software = "烘烤调度线体",
                    EmployeeNo = "wyn",             //员工工号
                    EquipmentInfo = new object[1]
                        {
                            new
                            {
                            EquipmentCode=rs.EquipmentCode,
                            WipOrder ="",
                            WipOrderType = "",
                            Customer = "",
                            ProductNo = "",
                            ProductDesc = "",
                            ProcessID = "",
                            Version = "",
                            OprSequenceNo = "",
                            OperationCode = "",
                            OprSequenceDesc = "",
                            FirstArticleNum ="",
                            DebugNum ="",
                            RecipeID ="",
                            ModuleType ="",
                            EquipmentStatusList = new object[1]
                            {
                                new
                                {
                                    EquipmentStatusID ="",
                                    ReasonCode ="",
                                    Description = "",
                                    StepInfo = new object[1]
                                    {
                                        new
                                        {
                                            StepID ="",
                                            StepType ="",
                                            ParameterInfo =new object[1]
                                            {
                                                new
                                                {
                                                    ParameterCode = "",
                                                    ParameterType ="",
                                                    TargetValue ="",
                                                    UOMCode="",
                                                    UpperControlLimit ="",
                                                    LowerControlLimit ="",
                                                    Description ="",
                                                    UploadFlag ="",
                                                    Active =""
                                                }

                                            }
                                        }
                                    }
                                }

                            }
                            }
                        }
                };

                string jsonRequest = JsonConvert.SerializeObject(ACEQPTINIT);
                SendValue = MesOperate.RevertJsonString(jsonRequest);   //发送数据

                wcf_Client.SendMsg("ACEQPTINIT", jsonRequest, ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);  //接收数据
                JObject revObj = JObject.Parse(jsonResponse);
                //jsonResponse = JsonConvert.SerializeObject(revObj);

                if (revObj != null)
                {
                    //if (revObj.ContainsKey())
                    {
                        result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["ResultFlag"]);
                    }
                    //if (revObj.ContainsKey("Message"))
                    {
                        msg = revObj["EquipmentInfo"][0]["Message"].ToString();
                    }
                    if (!result)
                    {
                        errMsg = $"MES接口：设备初始化(参数下发)失败! \r\nMsg:{msg}";
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                RecvValue = ex.Message;
                errMsg = $"设备参数下发！ \r\nMsg:请检查网线和MES网络通讯问题";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACEQPTINIT";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

                MesOperate.SaveLogData("MES设备参数下发", text);
            }

            return false;
        }
        #endregion
        /// <summary>
        /// 上传电能表数据
        /// </summary>
        /// <returns></returns>
        public bool UpLoadTelemetry()
        {
            try
            {
                {
                    //                    
                    //y":0.5,"status":1}
                    string sendPost = "";
                    string recvJson = "";
                    string result = "";
                    string errorCode = "";
                    string message = "";
                    bool status = false;

                    //if (null == dic)
                    //{
                    //    return false;
                    //}


                    //// MES 使能关闭
                    //if (!isMESEnable)
                    //{
                    //    return true;
                    //}

                    // MES 未登录
                    //if (!this.loginState)
                    //{
                    //    ShowMsgBox.ShowDialog("MES 未登录，请重新登录MES账号", MessageType.MsgAlarm);
                    //    return false;
                    //}

                    //string strSiteNo = MesDefine.mesParameter[(int)MesInterface.MesLogin].siteNo;
                    //"voltage":{ "ua":238.7,"ub":238.2,"uc":236.6,"uab":386.2,"ubc":385.5,"uca":385.8},"inten
                    //sity":{"ia":58.88,"ib":41.18,"ic":49.38},"energy":{"epi":675.8,"eqi":102.7,"epo":645.3,"eqo"

                    //                    :1052.3},"power":{
                    //                    "pa":-0.015,"pb":-0.035,"pc":0.083,"ps":0.033,"qa":-0.012,"qb":-
                    //0.038,"qc":0.075,"qs":0.025},"factor":{ "pfa":0.75,"pfb":0.55,"pfc":0.6,"pfs":1.92},"frequenc
                    JObject jsonRequest = JObject.Parse(JsonConvert.SerializeObject(new
                    {
                        voltage = new
                        {
                            ua = 0.00,
                            ub = 0.00,
                            uc = 0.00,
                            uab = 0.00,
                            ubc = 0.00,
                            uca = 0.00,
                        },
                        intensity = new
                        {
                            ia = 0.00,
                            ib = 0.00,
                            ic = 0.00,
                        },
                        energy = new
                        {
                            epi = 0.00,
                            eqi = 0.00,
                            epo = 0.00,
                            eqo = 0.00,
                        },
                        power = new
                        {
                            pa = 0.00,
                            pb = 0.00,
                            ps = 0.00,
                            qa = 0.00,
                            qb = 0.00,
                            qc = 0.00,
                            qs = 0.00,
                        },
                        factor = new
                        {
                            pfa = 0.00,
                            pfb = 0.00,
                            pfc = 0.00,
                            pfs = 0.00,
                        },
                        frequenc = 0.00,
                        status = 0
                    }));

                    sendPost = jsonRequest.ToString();
                    //MesDefine.mesParameter[(int)MesInterface.MesGetTrayInfo].sendData = sendPost;

                    recvJson = httpClient.Post("https://ems.t.cn-np.com/api/v1/$", sendPost);
                    //MesDefine.mesParameter[(int)MesInterface.MesGetTrayInfo].recvData = recvJson;
                    //GetBack descJson = JsonConvert.DeserializeObject<GetBack>(returnJson);
                    //if (null != recvJson)
                    //{
                    //    JObject jsonReturn = JObject.Parse(recvJson);
                    //    if (jsonReturn.ContainsKey("status"))
                    //    {
                    //        status = Convert.ToBoolean(jsonReturn["status"]);
                    //    }

                    //    if (jsonReturn.ContainsKey("result"))
                    //    {
                    //        result = Convert.ToString(jsonReturn["result"]);
                    //    }

                    //    JArray batArry = (JArray)JsonConvert.DeserializeObject(result);

                    //    foreach (var item in batArry)
                    //    {
                    //        JObject j = JObject.Parse(item.ToString());
                    //        dic.Add(int.Parse(j["TARY_NUMBER"].ToString()), j["SFC_NO"].ToString());
                    //        if (!string.IsNullOrEmpty(j["ITEM_NO"].ToString()))
                    //        {
                    //            //if (j["SFC_NO"].ToString().Substring(6, 1).Contains("B"))
                    //            //{
                    //            //    itemType = "AJXA0BZ-00001";
                    //            //}
                    //            //if (j["SFC_NO"].ToString().Substring(6, 1).Equals("C"))
                    //            //{
                    //            //    itemType = "AJXA0CZ-00001";
                    //            //}
                    //            itemType = j["ITEM_NO"].ToString();
                    //        }
                    //    }

                    //    if (jsonReturn.ContainsKey("errorCode"))
                    //    {
                    //        errorCode = Convert.ToString(jsonReturn["errorCode"]);
                    //    }

                    //    if (jsonReturn.ContainsKey("message"))
                    //    {
                    //        message = Convert.ToString(jsonReturn["message"]);
                    //    }

                    //    if (!status)
                    //    {
                    //        string alarmInfo = string.Format("MES 获取料框信息失败！MES返回错误码：{0} 错误消息：{1}", errorCode, message);
                    //        ShowMsgBox.ShowDialog(alarmInfo, MessageType.MsgAlarm);
                    //    }

                    //string file, title, text;
                    //file = string.Format(@"{0}\MesDtata\获取料框信息\{1}.csv", MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"));
                    //title = "日期,时间,料框码,返回状态,返回错误码,返回消息,";
                    //text = string.Format("{0},{1},", DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss"), trayCode);
                    //text += string.Format(",{0},{1},{2}", status.ToString(), errorCode, message);

                    //if (status)
                    //{
                    //    for (int nRow = 1; nRow < trayMaxRow + 1; nRow++)
                    //    {
                    //        title += string.Format("电芯{0}条码" + ",", nRow.ToString());
                    //        if (dic.ContainsKey(nRow))
                    //        {
                    //            text += dic[nRow] + ",";
                    //        }
                    //    }
                    //    title += string.Format("电芯型号" + ",");
                    //    text += itemType + ",";
                    //}


                    //Def.ExportCsvFile(file, title, (text + "\r\n"));

                    return status;
                }
                return false;
            }

            catch (System.Exception ex)
            {
                string alarmInfo = string.Format("Mes获取料框信息异常：{0}", ex.Message);
                ShowMsgBox.ShowDialog(alarmInfo, MessageType.MsgAlarm);
            }
            return false;
        }
        #region 设备运行参数变更   保留接口
        public bool ACEQPTCHNG_Main(ResourcesStruct rs, ref string errMsg)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            var result = false;

            DateTime startTime = DateTime.Now;
            try
            {
                //if (GetInstance().UpdataMes)
                {
                    var ACEQPTCHNG = new
                    {
                        AutoFlag = GetInstance().UpdataMes,
                        Software = "烘烤调度线体",
                        EmployeeNo = "wyn",             //员工工号
                        EquipmentInfo = new object[1]
                        {
                            new
                            {
                            EquipmentCode=rs.EquipmentCode,
                            ProductNo ="",
                            OprSequenceNo ="",
                            ProcessID = "",
                            Version = "",
                            RecipeID = "",
                            ParameterInfo = new object[1]
                            {
                                new
                                {
                                    ParameterCode = "",
                                    Location = "",
                                    OldValue = "",
                                    NewValue = "",
                                    Description = "",
                                    UpperControlLimit = "",
                                    LowerControlLimit = "",
                                    OldUpperControlLimit = "",
                                    OldLowerControlLimit = "",
                                    Active = false
                                }

                            }
                            }
                        }
                    };

                    string jsonRequest = JsonConvert.SerializeObject(ACEQPTCHNG);
                    SendValue = MesOperate.RevertJsonString(jsonRequest);   //发送数据

                    wcf_Client.SendMsg("ACEQPTCHNG", jsonRequest, ref jsonResponse, ref errorMessage);
                    RecvValue = MesOperate.RevertJsonString(jsonResponse);  //接收数据
                    JObject revObj = JObject.Parse(jsonResponse);
                    //jsonResponse = JsonConvert.SerializeObject(revObj);

                    if (revObj != null)
                    {
                        //if (revObj.ContainsKey())
                        {
                            result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["ResultFlag"]);
                        }
                        //if (revObj.ContainsKey("Message"))
                        {
                            msg = revObj["EquipmentInfo"][0]["Message"].ToString();
                        }
                        if (!result)
                        {
                            errMsg = $"设备运行参数变更上传失败! \r\nMsg:{msg}";
                            return false;
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                RecvValue = ex.Message;
                errMsg = $"设备运行参数变更！ \r\nMsg:请检查网线和MES网络通讯问题";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACEQPTCHNG";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

                MesOperate.SaveLogData("MES设备运行参数变更", text);
            }

            return false;
        }
        #endregion

        /// <summary>
        /// MES心跳线程
        /// </summary>
        private void HeartbeatThread()
        {
            while (this.monitorRunning)
            {
                try
                {
                    HeartbeatRunWhile();
                    DeviceStatusUpLoad();
                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("MachineCtrl", "HeartbeatThread()" + ex.Message, LogType.Error);
                }
                Thread.Sleep(1);
            }
        }
        /// <summary>
        /// MES心跳线程
        /// </summary>
        private void TelemetryThread()
        {
            while (this.monitorRunning)
            {
                try
                {
                    TelemetryRunWhile();
                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("MachineCtrl", "TelemetryThread()" + ex.Message, LogType.Error);
                }
                Thread.Sleep(1);
            }
        }
        private MCState oldMCState = MCState.MCInvalidState;
        MesMCState mesMC = MesMCState.Stop;
        private void DeviceStatusUpLoad()
        {
            if (oldMCState != RunsCtrl.GetMCState())
            {
                oldMCState = RunsCtrl.GetMCState();
                switch (RunsCtrl.GetMCState())
                {
                    case MCState.MCIdle:
                    case MCState.MCInitializing:
                    case MCState.MCStopInit:
                    case MCState.MCInitComplete:
                        mesMC = MesMCState.Stop;
                        string msg = "";
                        string workplace = "DAL1HK01";
                        if (!Jeve_Mes.Mes_WorkPlaceStatus(workplace, mesMC, ref msg))
                        {
                            ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                        }
                        break;
                    case MCState.MCRunning:
                        mesMC = MesMCState.Running;
                        msg = "";
                        workplace = "DAL1HK01";
                        if (!Jeve_Mes.Mes_WorkPlaceStatus(workplace, mesMC, ref msg))
                        {
                            ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                        }
                        break;
                    
                    case MCState.MCStopRun:
                        mesMC = MesMCState.Manual;
                        msg = "";
                        workplace = "DAL1HK01";
                        if (!Jeve_Mes.Mes_WorkPlaceStatus(workplace, mesMC, ref msg))
                        {
                            ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                        }
                        break;
                    case MCState.MCInitErr:
                    case MCState.MCRunErr:
                        mesMC = MesMCState.Alram;
                        msg = "";
                        workplace = "DAL1HK01";
                        if (!Jeve_Mes.Mes_WorkPlaceStatus(workplace, mesMC, ref msg))
                        {
                            ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                        }
                        break;
                    default:
                        break;
                }
               
                    
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// MES心跳操作
        /// </summary>
        private void HeartbeatRunWhile()
        {
            if (GetInstance().UpdataMes)
            {
                // MES心跳
                if ((DateTime.Now - heartbeatTime).TotalSeconds >= MesResources.HeartbeatInterval)
                {
                    if (!MesHeartbeat(heartbeatCount))
                    {
                        heartbeatCount++;
                    }
                    else
                    {
                        heartbeatCount = 0;
                    }
                    heartbeatTime = DateTime.Now;
                }
            }
            Thread.Sleep(500);
            if (!MesResources.MesLogin)
            {
                return;
            }

        }

        /// <summary>
        /// MES心跳
        /// </summary>
        /// <param name="count">心跳次数</param>
        /// <returns></returns>
        private bool MesHeartbeat(int count)
        {
            string msg = "";
            ////未启用MES
            //if (!this.UpdataMes)
            //{
            //    isMESConnect = false;
            //    return false;
            //}
            //if (!ACEQPTALIV_Main(MesResources.Equipment, ref msg))
            //{
            //    //ShowMessageID((int)MsgID.HeartbeatErr, msg, "请人工检查", MessageType.MsgWarning);
            //    isMESConnect = false;
            //    //this.UpdataMes = false;
            //    return false;
            //}
            //else
            //{
            //    isMESConnect = true;
            //}
            return true;
        }
        /// <summary>
        /// MES电能表数据上传
        /// </summary>
        private void TelemetryRunWhile()
        {
            if (GetInstance().UpdataMes)
            {
                //UpLoadTelemetry();
                Thread.Sleep(60000);
            }

            //if (!MesResources.MesLogin)
            //{
            //    return;
            //}

        }

        /// <summary>
        /// MES用户登录
        /// </summary>
        /// <returns></returns>
        public bool MesUserLogin()
        {
            UserLoginMES user = new UserLoginMES();
            if (DialogResult.OK == user.ShowDialog())
            {
                GetInstance().OperaterID = MesResources.Equipment.OperatorUserID;
                MesResources.MesLogin = true;
                return true;
            }
            if (string.IsNullOrEmpty(GetInstance().OperaterID))
            {
                ShowMsgBox.ShowDialog("未登录操作人员工号，不能启动软件！", MessageType.MsgWarning);
            }
            return false;
        }

        #region//离线上传
        public bool UpDataUnLineData(string pltCode, string bind, string intanceName, string updataInfoEx, ref string msg)
        {
            string jsonResponse = "";
            string errorMessage = "";
            bool result = false;
            string para = "";
            int count = 1;
            DateTime startTime = DateTime.Now;
            //string pltCode = "";
            string lineCode = "";
            string updataInfo = "";
            if (intanceName.Equals("ACLOGOFF"))
            {
                updataInfo = MesOperate.RevertStringJson(updataInfoEx);
                //string jsonstr = JsonConvert.DeserializeObject(updataInfo).ToString();
                JObject revObjRe = null;
                JObject revObjReParam = null;
                JObject revObjReRoot = JObject.Parse(updataInfo);
                IEnumerable<JProperty> pr;
                if (revObjReRoot.ContainsKey("EquipmentInfo"))
                {
                    revObjRe = JObject.Parse(revObjReRoot["EquipmentInfo"][0].ToString());
                    //if (revObjRe.ContainsKey("Outputs"))
                    //    revObjRe = JObject.Parse(revObjRe["Outputs"][0].ToString());
                    if (revObjRe.ContainsKey("Parameters"))
                    {
                        revObjReParam = JObject.Parse(revObjRe["Parameters"][0].ToString());
                        //para = para.Replace('\"', '"');
                        //para = MesOperate.RevertJsonString(para);
                        //para = MesOperate.RevertStringJson(para);
                    }
                }
                pr = revObjReParam.Properties();
                object[] parameters = new object[24];
                for (int i = 0; i < 24; i++)
                {
                    parameters[i] =
                new
                {
                    ParamterCode = revObjRe["Parameters"][i]["ParamterCode"].ToString(),
                    Location = revObjRe["Parameters"][i]["Location"].ToString(),
                    Value = revObjRe["Parameters"][i]["Value"].ToString(),
                    ParameterDescription = revObjRe["Parameters"][i]["ParameterDescription"].ToString(),
                    UpperLimit = revObjRe["Parameters"][i]["UpperLimit"].ToString(),
                    LowerLimit = revObjRe["Parameters"][i]["LowerLimit"].ToString(),
                    TargetValue = revObjRe["Parameters"][i]["TargetValue"].ToString(),
                    ParameterResult = revObjRe["Parameters"][i]["ParameterResult"].ToString(),
                    DefectCode = revObjRe["Parameters"][i]["DefectCode"].ToString(),
                    ParameterMessage = revObjRe["Parameters"][i]["ParameterMessage"].ToString(),
                    StepSequenceNo = revObjRe["Parameters"][i]["StepSequenceNo"].ToString(),
                };
                }
                int revCountOut = revObjRe["Outputs"].Count();

                object[] OutputsByBuf = new object[revCountOut];

                for (int i = 0; i < revCountOut; i++)
                {
                    OutputsByBuf[i] = new
                    {
                        SerialNo = revObjRe["Outputs"][i]["SerialNo"].ToString(),
                        PreSerialNo = revObjRe["Outputs"][i]["PreSerialNo"].ToString(),
                        SlotID = revObjRe["Outputs"][i]["SlotID"].ToString(),
                        IsRealFlag = revObjRe["Outputs"][i]["IsRealFlag"].ToString(),
                        ProductType = revObjRe["Outputs"][i]["ProductType"].ToString(),
                        Station = revObjRe["Outputs"][i]["Station"].ToString(),
                        PassFlag = revObjRe["Outputs"][i]["PassFlag"].ToString(),
                        ProcessFlag = revObjRe["Outputs"][i]["ProcessFlag"].ToString(),
                        MatchingInfo = new object[0]   //配对电芯信息  烘烤勿传
                            {
                            },
                        StationInfo = new object[0]     //工位清单  接口清单显示可为空
                            {
                            },
                        ProcessSteps = new object[1]    //加工工步信息  
                            {
                                new
                                {
                                    StepID = "1",
                                    StepStatus = "OK",
                                }
                            },
                        SpartInfo = new object[0]  //零部件信息  烘烤勿传
                            {
                            },
                        MaterialInfo = new object[0]  //物料信息  烘烤勿传
                            {
                            },
                        Parameters = parameters,
                    };
                }
                //pltCode = revObjRe["Container"].ToString();
                //lineCode = revObjRe["OperationMark"].ToString();
                var ACLOGOFFByBuf = new
                {
                    Software = revObjReRoot["Software"].ToString(),
                    AutoFlag = revObjReRoot["AutoFlag"].ToString(),
                    EmployeeNo = revObjReRoot["EmployeeNo"].ToString(),
                    EquipmentInfo = new object[1]
                    {
                        new
                        {
                        EquipmentCode= revObjRe["EquipmentCode"].ToString(),
                        Container =  revObjRe["Container"].ToString(),
                        NextEquipmentCode= revObjRe["NextEquipmentCode"].ToString(),
                        ProcessResult= revObjRe["ProcessResult"].ToString(),
                        OperationMark = revObjRe["OperationMark"].ToString(),
                        ProcessMessage =  revObjRe["ProcessMessage"].ToString(),
                        Outputs = OutputsByBuf,
                        }
                        }
                };
                try
                {
                    string rep = JsonConvert.SerializeObject(ACLOGOFFByBuf);
                    //rep=rep.Replace('\'','"');
                    wcf_Client.SendMsg(intanceName, rep, ref jsonResponse, ref errorMessage);
                    msg += string.Format("errorMessage:{0}", errorMessage);
                    JObject revObj = JObject.Parse(jsonResponse);

                    if (revObj != null)
                    {
                        //if (revObj.ContainsKey())
                        {
                            result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["Result"]);
                        }
                        //if (revObj.ContainsKey("Message"))
                        {
                            msg += revObj["EquipmentInfo"][0]["Message"].ToString();
                        }
                        if (!result)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    msg += string.Format("ex:{0}", ex.ToString());
                }
                finally
                {
                    //上传内容和返回信息划分10等份
                    int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                    string tryCode = pltCode;
                    string linecode = MesResources.Equipment.ProcessCode;
                    string mesUri = intanceName;
                    msg = MesOperate.RevertJsonString(msg);
                    string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{tryCode},{mesUri},{second},{result},{msg},{linecode},{MesResources.Equipment.EquipmentCode},{MesResources.Equipment.OperatorUserID},{updataInfoEx}";
                    if (result)
                    {
                        SaveLogData("MES出站", text);
                    }
                }
                return false;
            }
            try
            {
                updataInfo = MesOperate.RevertStringJson(updataInfoEx);
                wcf_Client.SendMsg(intanceName, updataInfo, ref jsonResponse, ref errorMessage);
                msg += string.Format("errorMessage:{0}", errorMessage);
                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    if (revObj.ContainsKey("EquipmentInfo"))
                    {
                        revObj = JObject.Parse(revObj["EquipmentInfo"][0].ToString());
                        if (revObj.ContainsKey("Result"))
                            result = Convert.ToBoolean(revObj["Result"]);
                        if (revObj.ContainsKey("ResultFlag"))
                            result = Convert.ToBoolean(revObj["ResultFlag"]);
                        if (revObj.ContainsKey("Message"))
                            msg = revObj["Message"].ToString();
                    }

                    if (!result)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                msg += string.Format("ex:{0}", ex.ToString());
            }
            finally
            {

                //上传内容和返回信息划分10等份
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string tryCode = pltCode;
                string linecode = MesResources.Equipment.ProcessCode;
                string mesUri = "ACLOGOFF";
                msg = MesOperate.RevertJsonString(msg);
                string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{tryCode},{mesUri},{second},{result},{msg},{linecode},{MesResources.Equipment.EquipmentCode},{MesResources.Equipment.OperatorUserID},{updataInfoEx}";
                switch (bind)
                {
                    case "dgvIn":
                        if (result)
                        {
                            SaveLogData("MES入站", text);
                        }
                        break;
                    case "dgvBind":
                        if (result)
                        {
                            SaveLogData("MES托盘电芯绑定", text);
                        }
                        break;
                    case "dgvUnBind":
                        if (result)
                        {
                            SaveLogData("MES托盘电芯解绑", text);
                        }
                        break;
                    default:
                        break;
                }

                //else
                //{
                //    SaveLogNGData("MES出站离线", text, pltCode);
                //    unLineDataListHander("Out", text, pltCode);
                //}
            }
            return false;
        }
        #endregion

        //判断入站信息日志是否更新
        public static bool GetPullInExCsvFileState(ref string filePath)
        {
            if (false == PullInLogHasChanged)
            {
                filePath = "";
                return false;
            }
            //PullInLogHasChanged = false;
            filePath = PullInExCsvFilePath;
            return true;
        }
        //判断出站信息日志是否更新
        public static bool GetOutExCsvFileState(ref string filePath)
        {
            if (false == OutLogHasChanged)
            {
                filePath = "";
                return false;
            }
            OutLogHasChanged = false;
            filePath = OutExCsvFilePath;
            return true;
        }

        //保存入站MES的展示信息
        //private void SaveOutBoundLogData(string text)
        //{
        //    string file, title;
        //    string fileName = "信息展示";
        //    file = string.Format(@"{0}\{1}\{2}\{3}\{2}{4}.csv"
        //            , MachineCtrl.GetInstance().ProductionFilePath, fileName, "入站", DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("yyyy-MM-dd HH-00"));

        //    PullInExCsvFilePath = file;
        //    PullInLogHasChanged = true;

        //    title = "时间,电芯条码,接口名称,结果,MES返回信息";

        //    Def.ExportCsvFile(file, title, (text + "\r\n"));
        //}


        //MES接口日志保存
        public static void SaveLogPullInData(string strName, string text)
        {
            string file, title;
            file = string.Format(@"{0}\{1}\{2}\{3}\{2}{4}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, "MESlog", strName, DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("yyyy-MM-dd HH-00"));
            title = "开始调用时间,条码(SFC),请求接口,耗时(ms),返回代码,返回信息,工序,设备,调用账号,上传内容";
            PullInExCsvFilePath = file;
            PullInLogHasChanged = true;
            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }
        //保存出站成功数据
        public static void SaveLogData(string strName, string text)
        {
            string file, title;
            file = string.Format(@"{0}\{1}\{2}\{3}\{2}{4}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, "MESlog", strName, DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("yyyy-MM-dd HH-00"));
            title = "开始调用时间,托盘码,请求接口,耗时(ms),返回结果,返回信息,工序,设备,调用账号,上传内容";
            OutExCsvFilePath = file;
            OutLogHasChanged = true;
            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }
        //保存出站成功数据
        public static void SaveLogReData(string strName, string text)
        {
            string file, title;
            file = string.Format(@"{0}\{1}\{2}\{3}\{2}{3}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, "ProRecordlog", strName, DateTime.Now.ToString("yyyy-MM-dd"));
            title = "开始调用时间,设备编码,托盘码,电芯码";
            OutExCsvFilePath = file;
            OutLogHasChanged = true;
            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }
        //保存出站离线数据
        public static void SaveLogNGData(string strName, string text, string pltCode = "")
        {
            string file, title;
            file = string.Format(@"{0}\{1}\{2}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, "MESNGLog", strName);
            title = "开始调用时间,托盘码,请求接口,耗时(ms),返回结果,返回信息,工序,设备,调用账号,上传内容";
            OutExCsvFilePath = file;
            OutLogHasChanged = true;
            Def.DeleteCsvLine(file, pltCode);
            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }
        /// <summary>
        /// 保存MES数据，离线上传
        /// </summary>
        /// <param name="mes"></param>
        /// <param name="mesData"></param>
        /// <returns></returns>
        public bool SaveMesData(MesInterface mes, string mesData)
        {
            if (null == this.mesFileLock)
            {
                return false;
            }
            bool result = false;
            lock (this.mesFileLock)
            {
                string mesProcessName = "";
                GetMesProcessName(mes, ref mesProcessName);
                string fileName = string.Format("{0}\\MES离线上传\\{1}\\{2}.mes", GetInstance().ProductionFilePath
                    , mesProcessName, DateTime.Now.ToString("yyyy-MM-dd"));
                string offlinefilename = string.Format("{0}\\MES离线上传\\{1}\\{2}.mes", GetInstance().ProductionFilePath
                    , mesProcessName, "offlinedata");
                Def.WriteText(fileName, mesData);
                Def.WriteText(offlinefilename, mesData);
                result = true;
            }
            return result;
        }

        /// <summary>
        /// 获取第一个文件中的Mes数据
        /// </summary>
        /// <returns></returns>
        public bool GetFirstFileMesData(MesInterface mes, ref List<string> mesData)
        {

            string mesProcessName = "";
            GetMesProcessName(mes, ref mesProcessName);
            string filePath = string.Format("{0}\\MES离线上传\\{1}", GetInstance().ProductionFilePath, mesProcessName);
            if (!Directory.Exists(filePath))
            {
                return false;
            }
            //D:\生产信息\MES上传\进站
            string fullpath = Path.Combine(filePath, "offlinedata.mes");
            if (!File.Exists(fullpath))
            {
                return false;
            }
            if (null == this.mesFileLock)
            {
                return false;
            }
            lock (this.mesFileLock)
            {
                using (StreamReader sr = new StreamReader(fullpath, System.Text.Encoding.Default))
                {
                    mesData.Clear();

                    string txt = "";
                    do
                    {
                        txt = sr.ReadLine();
                        if (!string.IsNullOrEmpty(txt))
                        {
                            try
                            {
                                mesData.Add(txt);
                            }
                            catch (System.Exception ex)
                            {
                                Trace.WriteLine(string.Format("GetFirstFileMesData():{0}", ex.Message));
                            }
                        }
                    } while (!string.IsNullOrEmpty(txt));
                }
            }

            return true;
        }

        public bool DeleteOfflineDataFile(MesInterface mes)
        {
            string mesProcessName = "";
            GetMesProcessName(mes, ref mesProcessName);
            string filePath = string.Format("{0}\\MES离线上传\\{1}", GetInstance().ProductionFilePath, mesProcessName);
            if (!Directory.Exists(filePath))
            {
                return false;
            }
            //D:\生产信息\MES上传\进站
            string fullpath = Path.Combine(filePath, "offlinedata.mes");
            if (!File.Exists(fullpath))
            {
                return false;
            }
            if (null == this.mesFileLock)
            {
                return false;
            }
            lock (this.mesFileLock)
            {
                File.Delete(fullpath);
            }
            return true;
        }

        public bool SaveMesLeftData(MesInterface mes, string mesData)
        {
            if (null == this.mesFileLock)
            {
                return false;
            }
            bool result = false;
            lock (this.mesFileLock)
            {
                string mesProcessName = "";
                GetMesProcessName(mes, ref mesProcessName);


                string offlinefilename = string.Format("{0}\\MES离线上传\\{1}\\{2}.mes", GetInstance().ProductionFilePath
                    , mesProcessName, "offlinedata");

                Def.WriteText(offlinefilename, mesData);
                result = true;
            }
            return result;
        }
        public bool GetMesProcessName(MesInterface mes, ref string strMesName)
        {
            switch (mes)
            {
                //case MesInterface.EquToMesInBaking:
                //    {
                //        strMesName = "入站校验";
                //        break;
                //    }
                //case MesInterface.EquToMesOutBaking:
                //    {
                //        strMesName = "出站上传";
                //        break;
                //    }
                case MesInterface.EquToMesBindingOrUnBind:
                    {
                        strMesName = "托盘绑定与解绑";
                        break;
                    }
            }
            return true;
        }

        //入站校验离线上传接口
        public bool UpdataMesData1(List<string> mesData)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            string errMsg = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            try
            {
                SendValue = MesOperate.RevertJsonString(mesData[0]);

                wcf_Client.SendMsg("ACLOGONCHECK", mesData[0], ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    //if (revObj.ContainsKey())
                    {
                        result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["Result"]);
                    }
                    //if (revObj.ContainsKey("Message"))
                    {
                        msg = revObj["EquipmentInfo"][0]["SerialNos"][0]["Message"].ToString();
                    }
                    if (!result)
                    {
                        errMsg = $"电芯入站校验失败! \r\nMsg:{msg}";
                        return false;
                    }
                    return true;
                }

            }
            catch (Exception ex)
            {
                RecvValue = ex.Message;
                errMsg = $"入站校验异常！ \r\nMsg:请检查网线和MES网络通讯问题";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACLOGONCHECK";
                string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{sfcode},{mesUri},{second},{result},{msg},{linecode},{"离线数据"},{"离线数据"},{SendValue}";
                SaveLogPullInData("MES入站校验", text);


            }
            return false;
        }
        //出站上传离线上传接口
        public bool UpdataMesData2(List<string> mesData)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            string errMsg = "";
            string equipmentCode = "";
            string container = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            try
            {

                //序列化
                SendValue = MesOperate.RevertJsonString(mesData[0]);     //发送信息

                wcf_Client.SendMsg("ACLOGOFF", mesData[0], ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);    //接收信息

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    //if (revObj.ContainsKey())
                    {
                        equipmentCode = revObj["EquipmentInfo"][0]["EquipmentCode"].ToString();
                    }
                    //if (revObj.ContainsKey("Message"))
                    {
                        container = revObj["EquipmentInfo"][0]["Container"].ToString();
                    }
                    //if (revObj.ContainsKey("Message"))
                    {
                        result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["Message"]);
                    }
                    //if (revObj.ContainsKey("Message"))
                    {
                        msg = revObj["EquipmentInfo"][0]["Message"].ToString();
                    }
                    if (!result)
                    {
                        errMsg = $"夹具电芯出站失败! \r\nMsg:{msg}";
                        return false;

                    }
                    return true;
                }

            }
            catch (Exception ex)
            {
                RecvValue = errorMessage;
                errMsg = $"夹具电芯出站异常！ \r\nMsg:" + errorMessage;
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACLOGOFF";
                string text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{sfcode},{mesUri},{second},{result},{errorMessage},{linecode},{"离线数据"},{"离线数据"},{SendValue}";

                SaveLogData("MES出站", text);
            }

            return false;

        }
        //托盘绑定与解绑离线上传接口
        public bool UpdataMesData3(List<string> mesData)
        {
            string RecvValue = "";
            string SendValue = "";
            string jsonResponse = "";
            string errorMessage = "";
            string msg = "";
            string errMsg = "";
            var result = false;
            DateTime startTime = DateTime.Now;
            try
            {

                //序列化
                SendValue = MesOperate.RevertJsonString(mesData[0]);     //发送信息

                wcf_Client.SendMsg("ACINBOUND", mesData[0], ref jsonResponse, ref errorMessage);
                RecvValue = MesOperate.RevertJsonString(jsonResponse);    //接收信息

                JObject revObj = JObject.Parse(jsonResponse);

                if (revObj != null)
                {
                    //if (revObj.ContainsKey())
                    {
                        result = Convert.ToBoolean(revObj["EquipmentInfo"][0]["ResultFlag"]);
                    }
                    //if (revObj.ContainsKey("Message"))
                    {
                        msg = revObj["EquipmentInfo"][0]["Message"].ToString();
                    }
                    if (!result)
                    {
                        errMsg = $"托盘电芯绑定与解绑失败! \r\nMsg:{msg}";
                        return false;

                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                RecvValue = ex.Message;
                errMsg = $"托盘电芯绑定与解绑异常！ \r\nMsg:请检查网线和MES网络通讯问题";
            }
            finally
            {
                int second = (int)(DateTime.Now - startTime).TotalMilliseconds;
                string sfcode = " ";
                string linecode = " ";
                string mesUri = "ACINBOUND";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{result},{RecvValue},{linecode},{"离线数据"},{"离线数据"},{SendValue}";

                MesOperate.SaveLogData("MES托盘电芯绑定与解绑", text);
            }

            return false;
        }

        /// <summary>
        /// 获取开机参数列表
        /// </summary>
        /// <returns></returns>
        public bool MesRecipeListGet()
        {
            try
            {
                string msg = "";
                MesRecipeStruct[] recipe = null;
                MesConfig cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeListGet);
                if (MesOperate.EquToMesRecipeListGet(MesResources.Equipment, ref recipe, ref msg))
                {
                    cfg.recipe.Clear();
                    foreach (var item in recipe)
                    {
                        if (cfg.recipe.ContainsKey(item.RecipeCode))
                        {
                            cfg.recipe[item.RecipeCode] = item;
                        }
                        else
                        {
                            cfg.recipe.Add(item.RecipeCode, item);
                        }
                    }
                    return true;
                }
                else
                {
                    ShowMessageID((int)MsgID.MesRecipeListGetErr, $"获取开机参数列表 失败\r\n{msg}", "请人工检查", MessageType.MsgAlarm);
                }
            }
            catch (System.Exception ex)
            {
                ShowMessageID((int)MsgID.MesRecipeListGetErr, $"获取开机参数列表 失败\r\n{ex.Message}", "请人工检查", MessageType.MsgAlarm);
            }
            return false;
        }

        /// <summary>
        /// 获取开机参数明细
        /// </summary>
        /// <returns></returns>
        public bool MesRecipeGet(string recipeCode)
        {
            try
            {
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeListGet);
                var recipe = new Dictionary<string, MesRecipeStruct>();
                foreach (var item in cfg.recipe)
                {
                    recipe.Add(item.Key, item.Value);
                }
                foreach (var item in recipe)
                {
                    if (!recipeCode.Equals(item.Key))
                    {
                        continue;
                    }
                    string msg = "";
                    MesParameterData[] param = null;
                    if (MesOperate.EquToMesRecipeGet(MesResources.Equipment, item.Key, ref param, ref msg))
                    {
                        MesRecipeStruct mesRecipe = new MesRecipeStruct();
                        mesRecipe.Param = new List<MesParameterData>(param);
                        mesRecipe.RecipeCode = item.Value.RecipeCode;
                        mesRecipe.Version = item.Value.Version;
                        mesRecipe.ProductCode = item.Value.ProductCode;
                        mesRecipe.LastUpdateOnTime = item.Value.LastUpdateOnTime;

                        cfg.recipe[item.Key] = mesRecipe;
                    }
                    else
                    {
                        ShowMessageID((int)MsgID.MesRecipeGetErr, $"获取开机参数明细 失败\r\n{msg}", "请人工检查", MessageType.MsgAlarm);
                        return false;
                    }
                    return true;
                }
                return false;
            }
            catch (System.Exception ex)
            {
                ShowMessageID((int)MsgID.MesRecipeGetErr, $"获取开机参数明细 失败\r\n{ex.Message}", "请人工检查", MessageType.MsgAlarm);
            }
            return false;
        }

        /// <summary>
        /// 参数校验及采集上报
        /// </summary>
        /// <returns></returns>
        public bool MesRecipeVExamine(string recipeCode)
        {
            var recipe = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeListGet).recipe;
            foreach (var item in recipe)
            {
                if (recipeCode.Equals(item.Value.RecipeCode))
                {
                    string msg = "";
                    if (MesOperate.EquToMesRecipeVExamine(MesResources.Equipment, item.Value.RecipeCode, item.Value.Version, ref msg))
                    {
                        if (MesOperate.EquToMesRecipe(MesResources.Equipment, item.Value, ref msg))
                        {
                            //ShowMessageID((int)MsgID.MesRecipeErr, $"开机参数采集 成功\r\n{msg}", "", MessageType.MsgMessage);
                            return true;
                        }
                        else
                        {
                            ShowMessageID((int)MsgID.MesRecipeErr, $"开机参数采集 失败\r\n{msg}", "请人工检查", MessageType.MsgAlarm);
                            return false;
                        }
                    }
                    else
                    {
                        ShowMessageID((int)MsgID.MesRecipeVExamineErr, $"开机参数版本校验 失败\r\n{msg}", "请人工检查", MessageType.MsgAlarm);
                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 保存MES下发互锁信息日志
        /// </summary>
        /// <param name="text"></param>
        public void SaveInterLockLogData(string text)
        {
            string file, title;
            file = string.Format(@"{0}\{1}\{2}\{3}\{2}{4}.csv",
                    MachineCtrl.GetInstance().ProductionFilePath,
                    InterLockLogFileName,
                    "互锁日志",
                    DateTime.Now.ToString("yyyy-MM-dd"),
                    DateTime.Now.ToString("yyyy-MM-dd HH"));

            title = "时间,互锁信号代码,互锁信号描述,设备编码,执行结果";

            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }

        /// <summary>
        /// WebApi释放
        /// </summary>
        /// <returns></returns>
        public bool ReleaseWebApi()
        {
            return WebServer.Dispose();
        }

        public async void InterLock(object sender, EventArgs e)
        {
            await Task.Run(() =>
                {
                    if (bInterLockResult)
                    {
                        // 向炉子发送MES互锁信号
                        //if (!result.IsSuccess)
                        //{
                        //    ShowMsgBox.ShowDialog("上位机MES互锁发送PLC数据失败,请检查PLC是否连接!", MessageType.MsgAlarm);
                        //}
                        //else
                        {
                            bInterLockResult = false;
                            nInterLockCodeIndex = 0;
                        }
                    }
                }
                );
        }
        #endregion




        #region // 上下料统计
        public void SaveBakingBatCnt()
        {
            try
            {
                BakingBatFormula bak = new BakingBatFormula();
                bak.BakingDate = DateTime.Now.ToString("yyyy-MM-dd HH:00:00");
                bak.BakingHour = TotalData.CurCountHour;
                bak.OnloadCnt = TotalData.OnloadHourCnt;
                bak.OnloadNgCnt = TotalData.OnloadHourNgCnt;
                bak.OffloadCnt = TotalData.OffloadHourCnt;

                if (dbRecord.BakingBatExists(bak))
                {
                    dbRecord.UpdateBakingBatInfo(bak);
                }
                else
                {
                    dbRecord.AddBakingBatInfo(bak);
                }
            }
            catch (Exception ex)
            {
                Def.WriteLog("RunProcessOnloadRobot", "保存上下料每小时统计记录异常" + ex.Message, LogType.Error);
            }
        }
        /// <summary>
        /// 获取上下料统计
        /// </summary>
        public DataTable GetBakingBatCnt(string dateTime, string endTime)
        {
            try
            {
                DataTable dt = new DataTable();
                if (dbRecord.GetBakingBatInfo(dateTime, endTime, ref dt))
                {
                    return dt;
                }
            }
            catch (Exception ex)
            {
                Def.WriteLog("GetBakingBatCnt", "获取上下料每小时统计记录异常" + ex.Message, LogType.Error);
            }
            return null;
        }

        /// <summary>
        /// 删除统计记录
        /// </summary>
        public void BakingBatCntDel()
        {
            try
            {
                //dbRecord.BakingBatInfoDel(dateTime, endTime);
            }
            catch (Exception ex)
            {
                Def.WriteLog("BakingBatCntDel", "获取上下料每小时统计记录异常" + ex.Message, LogType.Error);
            }
        }

        #endregion

        //
        /// <summary>
        /// 进站条码处理线程
        /// </summary>
        private void IOTThread()
        {
            while (this.monitorRunning)
            {
                if (IOTTaskList.IsReady())
                {
                    IOTData model = IOTTaskList.GetFirstModel();
                    if (model != null)
                    {
                        ThreadManager.AddWork(model, IOTUploadTask);
                    }
                }
            }
        }

        private void IOTUploadTask(object obj)
        {
            IOTData iot = (IOTData)obj;
            if (null == iot)
            {
                return;
            }

            try
            {
                string errMsg = "";
                if (!MesOperate.EquToIOTServer(MesResources.Equipment, iot, ref errMsg))
                {
                    //提交成功
                }
            }
            catch (Exception ex)
            {
                Def.WriteLog("MachineCtrl", ex.Message, LogType.Error);
            }
        }
        public void LogingUi()
        {
            LogingOutUI("");
        }
        //#region
        ///// <summary>
        ///// 下发权限
        ///// </summary>
        ///// <returns></returns>
        //public void SetRoleID()
        //{
        //    if (!(MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot).SetRoleToOnload((int)this.dbRecord.UserLevel()))
        //        ShowMsgBox.ShowDialog("上料PLC连接异常，权限下发失败！", MessageType.MsgWarning);
        //    if (!(MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery).SetRoleToOffload((int)this.dbRecord.UserLevel()))
        //        ShowMsgBox.ShowDialog("下料PLC连接异常，权限下发失败！", MessageType.MsgWarning);
        //}
        //#endregion
        private bool StartUpdataDevice()
        {
            string msg = "";
            try
            {
                if (!GetInstance().UpdataMes)
                {
                    return true;
                }
                string file, section, key;
                file = Def.GetAbsPathName(Def.MesParameterCfg);
                MesRecipeStruct mesRecipeStruct = new MesRecipeStruct();
                mesRecipeStruct.RecipeCode = IniFile.ReadString("ParameterInfoOld", "ProductCode", "", file);
                mesRecipeStruct.Version = IniFile.ReadString("ParameterInfoOld", "Version", "", file);
                mesRecipeStruct.OprSequenceNo = IniFile.ReadString("ParameterInfoOld", "OprSequenceNo", "", file);
                if (MachineCtrl.GetInstance().ACEQPTPARM_Main(MesResources.Equipment, ref mesRecipeStruct, ref msg))
                {
                    for (int i = (int)RunID.DryOven0; i < (int)RunID.DryOvenALL; i++)
                    {
                        RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule((RunID)i) as RunProcessDryingOven;
                        if (null == run)
                        {
                            return false;
                        }
                        foreach (var item in mesRecipeStruct.Param)
                        {
                            if (item.ParameterType.Equals("1"))
                            {
                                continue;
                            }
                            switch (item.Description)
                            {
                                //case "烘烤设定温度":
                                //    run.WriteParameterEX(run.RunModule, "SetTempValue", item.TargetValue);
                                //    break;
                                //case "烘烤设定最大温度":
                                //    run.WriteParameterEX(run.RunModule, "TempUpperlimit", item.TargetValue);
                                //    break;
                                //case "烘烤设定最小温度":
                                //    run.WriteParameterEX(run.RunModule, "TempLowerlimit", item.TargetValue);
                                //    break;
                                //case "预热时间":
                                //    run.WriteParameterEX(run.RunModule, "PreheatTime", item.TargetValue);
                                //    break;
                                //case "加热时间":
                                //    run.WriteParameterEX(run.RunModule, "VacHeatTime", item.TargetValue);
                                //    break;
                                //case "开门破真空时长":
                                //    run.WriteParameterEX(run.RunModule, "OpenDoorBlowTime", item.TargetValue);
                                //    break;
                                //case "开门真空压力":
                                //    run.WriteParameterEX(run.RunModule, "OpenDoorVacPressure", item.TargetValue);
                                //    break;
                                //case "A状态抽真空时间":
                                //    run.WriteParameterEX(run.RunModule, "AStateVacTime", item.TargetValue);
                                //    break;
                                //case "A状态真空压力":
                                //    run.WriteParameterEX(run.RunModule, "AStateVacPressure", item.TargetValue);
                                //    break;
                                //case "B状态抽真空时间":
                                //    run.WriteParameterEX(run.RunModule, "BStateVacTime", item.TargetValue);
                                //    break;
                                //case "B状态真空压力":
                                //    run.WriteParameterEX(run.RunModule, "BStateVacPressure", item.TargetValue);
                                //    break;
                                //case "呼吸充干燥气时间":
                                //    run.WriteParameterEX(run.RunModule, "BStateBlowAirTime", item.TargetValue);
                                //    break;
                                //case "呼吸充干燥气压力":
                                //    run.WriteParameterEX(run.RunModule, "BStateBlowAirPressure", item.TargetValue);
                                //    break;
                                //case "呼吸充干燥气保持时间":
                                //    run.WriteParameterEX(run.RunModule, "BStateBlowAirKeepTime", item.TargetValue);
                                //    break;
                                //case "呼吸时间间隔":
                                //    run.WriteParameterEX(run.RunModule, "BreathTimeInterval", item.TargetValue);
                                //    break;
                                //case "呼吸循环次数":
                                //    run.WriteParameterEX(run.RunModule, "BreathCycleTimes", item.TargetValue);
                                //    break;
                                //case "发热板数":
                                //    run.WriteParameterEX(run.RunModule, "HeatPlate", item.TargetValue);
                                //    break;
                                //case "最大NG发热板数":
                                //    run.WriteParameterEX(run.RunModule, "MaxNGHeatPlate", item.TargetValue);
                                //    break;
                                //case "加热前抽真空时间":
                                //    run.WriteParameterEX(run.RunModule, "HeatPreVacTime", item.TargetValue);
                                //    break;
                                //case "加热前充干燥气压力":
                                //    run.WriteParameterEX(run.RunModule, "HeatPreBlow", item.TargetValue);
                                //    break;
                                //case "真空最小值":
                                //    run.WriteParameterEX(run.RunModule, "VacMinValue", item.TargetValue);
                                //    break;
                                //case "真空最大值":
                                //    run.WriteParameterEX(run.RunModule, "VacMaxValue", item.TargetValue);
                                //    break;
                                //case "温差报警值":
                                //    run.WriteParameterEX(run.RunModule, "TempDifferAlarmValue", item.TargetValue);
                                //    break;
                                case "正极片水含量":
                                    run.WriteParameterEX(run.RunModule, "positiveWaterStandard", item.UpperControlLimit);
                                    break;
                                case "负极片水含量":
                                    run.WriteParameterEX(run.RunModule, "negativeWaterStandard", item.UpperControlLimit);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    return true;
                }
                else
                {
                    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                    return false;
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                return false;
            }
            return true; 
        }

        private void UpdateCavityData(Machine.CavityData srcCavityData, BakingDataLib.CavityData tagCavityData)
        {
            Random random = new Random();
            //腔体数据
            tagCavityData.vacuum = srcCavityData.vacPressure;
            tagCavityData.runningTime = srcCavityData.workTime;
            tagCavityData.vacuum = (ushort)random.Next(1, 10);
            //更新夹具温度
            tagCavityData.palletData.Clear();
            for (int i = 0; i < 2; i++)
            {
                BakingDataLib.PalletData palletData = new BakingDataLib.PalletData()
                {
                    patrolTemp = srcCavityData.tempValue[i, 0, 0],
                    currentTemp = srcCavityData.tempValue[i, 0, 0]
                };
                tagCavityData.palletData.Add(palletData);
            }
            //腔体工艺参数
            tagCavityData.cavityParam.cycleBlowPressure = srcCavityData.parameter.BStateBlowAirPressure;
            tagCavityData.cavityParam.cycleBlowTime = srcCavityData.parameter.BStateBlowAirKeepTime; //呼吸充干燥气时间
            tagCavityData.cavityParam.cycleHoldTime = srcCavityData.parameter.BStateBlowAirKeepTime;
            tagCavityData.cavityParam.cycleInterval = srcCavityData.parameter.BreathCycleTimes;  //呼吸间隔注释
            tagCavityData.cavityParam.cycleTimes = srcCavityData.parameter.BreathCycleTimes;
            tagCavityData.cavityParam.maxTemp = srcCavityData.parameter.TempUpperlimit;
            tagCavityData.cavityParam.minTemp = srcCavityData.parameter.TempLowerlimit;
            tagCavityData.cavityParam.preheatBlowPressure = srcCavityData.parameter.HeatPreBlow;
            tagCavityData.cavityParam.preheatPumpTime = srcCavityData.parameter.HeatPreVacTime;
            tagCavityData.cavityParam.preheatTime = srcCavityData.parameter.PreheatTime;
            tagCavityData.cavityParam.setTemp = srcCavityData.parameter.SetTempValue;
            tagCavityData.cavityParam.vacuumA = srcCavityData.parameter.AStateVacPressure;
            tagCavityData.cavityParam.vacuumB = srcCavityData.parameter.BStateVacPressure;
            tagCavityData.cavityParam.vacuumBreakerPressure = srcCavityData.parameter.OpenDoorVacPressure;
            tagCavityData.cavityParam.vacuumBreakerTime = srcCavityData.parameter.OpenDoorBlowTime;
            tagCavityData.cavityParam.vacuumHoldTime = srcCavityData.parameter.VacHeatTime;
            tagCavityData.cavityParam.vacuumPumpTimeA = srcCavityData.parameter.AStateVacTime;
            tagCavityData.cavityParam.vacuumPumpTimeB = srcCavityData.parameter.BStateVacTime;
            //炉腔状态
            tagCavityData.doorState = srcCavityData.doorState;                  //炉门状态
            tagCavityData.dwellState = srcCavityData.pressureState;             //保压状态
            tagCavityData.plateHeaterState = srcCavityData.heatingState[0];     //夹具加热状态
            tagCavityData.state = srcCavityData.workState;                      //工作状态
            tagCavityData.vacuumBreakerState = srcCavityData.blowValveState;    //破真空阀状态
            //报警状态
            tagCavityData.doorAlarm = srcCavityData.doorAlarm;                  //炉门报警
            tagCavityData.leftMechTempAlarm = srcCavityData.controlAlarm[0];    //左机械温控报警
            tagCavityData.leftPlateDetectAlarm = srcCavityData.pallletAlarm[0]; //左夹具放平检测报警：夹具
            tagCavityData.rightMechTempAlarm = srcCavityData.controlAlarm[1];   //右机械温控报警
            tagCavityData.rightPlateDetectAlarm = srcCavityData.pallletAlarm[1];//右夹具放平检测报警：夹具
            tagCavityData.vacuumAlarm = srcCavityData.vacAlarm;                 //真空报警
            tagCavityData.vacuumBreakerAlarm = srcCavityData.blowAlarm;         //破真空报警
            tagCavityData.vacuumGauge1Alarm = srcCavityData.vacuometerAlarm;    //真空计报警
            //报警值
            tagCavityData.leftTempAlarm = srcCavityData.tempAlarmValue[0,0];    //左温度报警温度值：夹具-发热板   
            tagCavityData.rightTempAlarm = srcCavityData.tempAlarmValue[1, 0];  //右温度报警温度值：夹具-发热板  
            tagCavityData.vacuumAlarmValue = srcCavityData.vacAlarmValue;       //真空报警值
        }

        //更新上料的读取数据
        private void UpdateOnloadData(OnloadData onload,BakingDataLib.OnloadData onloadData)
        {
            onloadData.state =(ushort)onload.runningState;
            for (int i = 0; i < 3; i++)
            {
                onloadData.platPlateState[i] = (ushort)(onload.onloadSignal[i, 0] ? (onload.onloadSignal[i, 1]? (onload.onloadSignal[i, 2]?1 : 0) : 0):0);
                onloadData.platPlateBatNum[i] = (ushort)onload.palletDataArray[i].count;
            }
            onloadData.robotSafeState = onload.avoidMove;
            for (int i = 0; i < 5; i++)
            {
                if (i==0)
                {
                    onloadData.bufPlatState[i] = onload.operateSignal[i, 0] && onload.operateSignal[i, 1] && onload.operateSignal[i, 2] ? true : false;
                }
                else
                {
                    onloadData.bufPlatState[i] = onload.bufSignal[i-1, 0]&& onload.bufSignal[i-1, 1] && onload.bufSignal[i-1, 2]?true:false;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                onloadData.scannerAlarm[i] = onload.scanAlram[i];
            }
            onloadData.plateScannerAlarm = onload.scanAlram[4];
            onloadData.robotAlarm = onload.onloadRobotAlram;
        }

        //更新下料的读取数据
        private void UpdateOffloadData(OffloadData offload, BakingDataLib.OffloadData offloadData)
        {
            offloadData.state = (ushort)offload.runningState;
            for (int i = 0; i < 2; i++)
            {
                offloadData.platPlateState[i] = (ushort)(offload.offloadSignal[i, 0] ? (offload.offloadSignal[i, 1] ? (offload.offloadSignal[i, 2] ? 1 : 0) : 0) : 0);
                offloadData.platPlateBatNum[i] = (ushort)offload.palletDataArray[i].count;
            }
            offloadData.robotSafeState = offload.avoidMove;

            offloadData.fingerAlarm = offload.fingerAlarm;
            offloadData.fingerCommAlarm = offload.fingerCommAlarm;
            for (int i = 0; i < 2; i++)
            {
                offloadData.platAlarm[i] = offload.platAlarm[i];
            }
            
        }
        //更新调度机器人的读取数据
        private void UpdateTransferData( BakingDataLib.TransferData transferData)
        {
            RobotPage robotPage = new RobotPage();
            RunProcessRobotTransfer runTransfer = GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            transferData.state = (ushort)(robotPage.robotRunning ? 1 : 0);
            transferData.robotState = (robotPage.robotRunning ? false : true);
            transferData.robotAlarm = runTransfer.robotAlarm;
            transferData.robotCommAlarm = runTransfer.robotAlarm;
        }

        private void BakingDataPublish()
        {
            byte[] buff;
            //更新炉子数据
            m_bakingData.OvenDataList.Clear();
            for(int i = 0; i < ((int)OvenInfoCount.OvenCount); i++)
            {
                RunProcessDryingOven run = GetInstance().GetModule(RunID.DryOven0 + i) as RunProcessDryingOven;
                OvenData ovenData = new OvenData();
                for (int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                {
                    BakingDataLib.CavityData cavity = new BakingDataLib.CavityData();
                    UpdateCavityData(run.RCavity(row), cavity);
                    ovenData.cavityData.Add(cavity);
                }
                m_bakingData.OvenDataList.Add(ovenData);
            }
            //更新上料数据
            BakingDataLib.OnloadData onloadData = new BakingDataLib.OnloadData();
            RunProcessOnloadRobot runOnload = GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            UpdateOnloadData(runOnload.onloadData, onloadData);
            m_bakingData.OnloadData = onloadData;
            //更新下料数据
            BakingDataLib.OffloadData offloadData = new BakingDataLib.OffloadData();
            RunProcessOffloadBattery runOffload = GetInstance().GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery;
            UpdateOffloadData(runOffload.offloadData, offloadData);
            m_bakingData.OffloadData = offloadData;
            //更新调度数据
            BakingDataLib.TransferData transData = new BakingDataLib.TransferData();
            UpdateTransferData(transData);
            m_bakingData.TransferData = transData;

            //发布数据
            buff = m_bakingData.Serialize();
            if(!m_publisherSocket.TrySendFrame(buff, buff.Length))
            {
                Def.WriteLog("MachineCtrl", "BakingDataPublish() - sendFrame fail");
            }
        }

        private void BakingDataPublishThread()
        {
            while (this.monitorRunning)
            {
                try
                {
                    BakingDataPublish();
                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("MachineCtrl", "BakingDataPublishThread()" + ex.Message, LogType.Error);
                }
                Thread.Sleep(200);
            }
        }

    }
}
