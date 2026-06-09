using HelperLibrary;
using Machine.Framework.Mes;
using Machine.MYSQL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SystemControlLibrary;
using static Machine.MYSQL.MySqlBatteryBasket;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    /// <summary>
    /// 来料扫码线体
    /// </summary>
    class RunProcessOnloadScan : RunProcess
    {
        #region // 枚举：步骤，模组数据，报警列表

        protected new enum InitSteps
        {
            Init_DataRecover = 0,

            Init_CheckBattery,
            Init_ConnectScanner,

            Init_End,
        }

        protected new enum AutoSteps
        {
            Auto_WaitWorkStart = 0,
            
            // 接收电池
            Auto_RecvBatttery,
            // 电池扫码
            Auto_ScanBatteryCode,
            Auto_MesCheckCode,
            // 发送电池
            Auto_SendBatttery,

            Auto_WorkEnd,
        }

        public enum ModDef
        {
            RecvPos_0 = 0,
            RecvPos_1,
            RecvPos_2,
            RecvPos_3,
            RecvPos_ALL = 24,
        }

        private enum MsgID
        {
            Start = ModuleMsgID.OnloadScanMsgStartID,
            RecvTimeout,
            SendTimeout,
            ScanCodeFail,
            ScanCodeTimeout,
            CodeLenError,
            CodeTypeError,
            CheckBattery
        }

        #endregion

        #region // 字段，属性

        #region // IO
        /// <summary>
        /// 接收位，进入
        /// </summary>
        private int IRecvPosEnter;      // 接收位，进入
        /// <summary>
        /// 接收位，到位
        /// </summary>
        private int IRecvPosInpos;      // 接收位，到位
        /// <summary>
        /// 定位气缸，推出到位
        /// </summary>
        private int IPositionPush;      // 定位气缸，推出到位
        /// <summary>
        /// 定位气缸，拉回到位
        /// </summary>
        private int IPositionPull;      // 定位气缸，拉回到位
        /// <summary>
        /// 对接信号（输入请求入料），②响应，必接
        /// </summary>
        private int IResponseSend;      // 对接信号，②响应，必接

        /// <summary>
        /// 定位气缸，推出
        /// </summary>
        private int OPositionPush;      // 定位气缸，推出
        //private int OPositionPull;      // 定位气缸，拉回
        /// <summary>
        /// 取料位电机
        /// </summary>
        private int ORecvPosMotor;      // 取料位电机
        /// <summary>
        /// 对接信号，①请求，可以不接
        /// </summary>
        //private int ORequireMaterial;   // 对接信号，①请求，可以不接
        /// <summary>
        /// 对接信号（输出请求入料），③线体可接收，请求入料，必接
        /// </summary>
        private int ORequireSend;       // 对接信号，③线体可接收，请求入料，必接
        /// <summary>
        /// 对接信号，③线体接收完成，必接
        /// </summary>
        private int ORequireSendEnd;
        

        #endregion

        #region // 电机
        #endregion

        #region // ModuleEx.cfg配置
        #endregion

        #region // 模组参数
        /// <summary>
        /// 上工序联机使能：TRUE启用，FALSE禁用
        /// </summary>
        public bool conveyerLineEN;         // 上工序联机使能：TRUE启用，FALSE禁用
        public bool recvBatteryEnable;      // 接收来料电池
        private bool[] scanEnable;          // 扫码使能：TRUE启用，FALSE禁用
        private string scanCmd;             // 扫码器的扫码指令
        private bool scanLinefeed;          // 扫码器的扫码结束符
        private string[] barcodeScanIP;     // 扫码器的IP：进行网口通讯则填，否则为空
        private int[] barcodeScanCom;       // 扫码器的COM口：进行串口通讯则填，否则为-1
        private int[] barcodeScanPort;      // 扫码器的Port
        public int codeLength;             // 条码长度：-1则不检查
        private string codeType;            // 条码类别：空则不检查，多种类别以英文逗号(,)分隔
        private string scanNGType;          // 扫码NG字符：空则不检查
        private int scanMaxCount;           // 最大扫码次数：（X≥1）
        private int recvDelay;              // 接收电池延时：毫秒ms
        private int recvTimeOut;            // 接收电池超时：秒s
        private bool randNGBat;             // 生成随机NG电池
        //private int scanNum;                // 跳过扫码，1为正常，2

        #endregion

        #region // 模组数据

        private BarcodeScan[] barcodeScan;  // 扫码器
        private string[] codeTypeArray;     // 条码类型列表
        private RunProcessOnloadRobot onloadRobot;

        private Task bgThread;                          // 后台线程
        private bool isRunThread;                       // 指示线程运行
        private bool connectState;                      // 当前连接状态（提示用）

        #endregion

        #endregion

        public RunProcessOnloadScan(int runId) : base(runId)
        {
            InitBatteryPalletSize((int)ModDef.RecvPos_ALL, 0);

            PowerUpRestart();

            InitParameter();
            // 参数
            InsertVoidParameter("conveyerLineEN", "上工序联机使能", "上工序联机使能：TRUE启用，FALSE禁用", conveyerLineEN, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("RecvBatteryEnable", "接收来料电池", "上料接收来料电池：TRUE=接收电池，FALSE=停止接收电池", recvBatteryEnable, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            //for (int i = 0; i < this.scanEnable.Length; i++)
            //{
            //    InsertVoidParameter(("scanEnable" + i), ("扫码器" + (i + 1) + "使能"), "扫码使能：TRUE启用，FALSE禁用", scanEnable[i], RecordType.RECORD_BOOL);
            //    InsertVoidParameter(("barcodeScanIP" + i), ("扫码器" + (i + 1) + "的IP"), "扫码器的IP：进行网口通讯则填，否则为空", barcodeScanIP[i], RecordType.RECORD_STRING);
            //    //InsertVoidParameter(("barcodeScanCom" + i), ("扫码器" + (i + 1) + "的COM口"), "扫码器的COM口：进行串口通讯则填，否则为-1", barcodeScanCom[i], RecordType.RECORD_INT);
            //    InsertVoidParameter(("barcodeScanPort" + i), ("扫码器" + (i + 1) + "的端口/波特率"), "扫码器的Port", barcodeScanPort[i], RecordType.RECORD_INT);
            //}
            InsertVoidParameter("scanCmd", "扫码指令", "触发扫码的指令", scanCmd, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanLinefeed", "扫码结束符", "扫码器的扫码结束符：true有回车换行结束符，false无结束符", scanLinefeed, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("codeLength", "条码长度", "条码长度：-1则不检查", codeLength, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("codeType", "条码类别", "条码类别：空则不检查，多种类别以英文逗号(,)分隔", codeType, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanNGType", "扫码NG字符", "扫码NG时扫码器反馈字符：空则不检查", scanNGType, RecordType.RECORD_STRING, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("scanMaxCount", "最大扫码次数", "最大扫码次数：（X≥1）", scanMaxCount, RecordType.RECORD_INT, ParameterLevel.PL_STOP_MAIN);
            InsertVoidParameter("recvDelay", "接收电池延时", "接收电池时感应到位后延时：毫秒ms", recvDelay, RecordType.RECORD_INT, ParameterLevel.PL_STOP_OPER);
            InsertVoidParameter("recvTimeOut", "接收电池超时时间", "接收电池超时时间：秒s", recvTimeOut, RecordType.RECORD_INT, ParameterLevel.PL_STOP_OPER);

            //InsertVoidParameter("recvTimeOut", "跳过扫码", "1为正常，2为跳过扫码", scanNum, RecordType.RECORD_INT, ParameterLevel.PL_STOP_OPER);

            this.bgThread = new Task(RunWhileThread, TaskCreationOptions.LongRunning);
            this.bgThread.Start();

            if (Def.IsNoHardware())
                InsertVoidParameter("randNGBat", "生成NG电池", "生成随机NG电池", randNGBat, RecordType.RECORD_BOOL, ParameterLevel.PL_STOP_MAIN);
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
                    RunWhileOnloadScan();
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
        protected void RunWhileOnloadScan()
        {
            if (Def.IsNoHardware())
                return;
            if (!IsModuleEnable())
                return;
            if (!this.onloadRobot.OnloadIsConnect())
                return;
            Sleep(2000);
            if (onloadRobot.onloadData.UpPalletFlag)
            {
                //string msg = "";
                //string vehicleno = "";
                //string workplace = "DAL1HK01";
                //string checkroute = "1";
                //string unbind = "1";
                //vehicleno = onloadRobot.onloadData.PalletPickCode;
                //MYSQL.MySqlBatteryBasket.ListBasketData listBasketData = new MYSQL.MySqlBatteryBasket.ListBasketData();
                //int count = 0;
                //do
                //{
                //    if (!Def.IsNoHardware() && Jeve_Mes.Mes_GetVehicleInfo(vehicleno, workplace, checkroute, unbind, ref listBasketData, ref msg) && !vehicleno.Contains("ERROR"))
                //    {
                //        break;
                //    }
                //    else 
                //    {
                //        if (count >= 3 && MachineCtrl.GetInstance().UpdataMes)
                //        {
                //            ShowMsgBox.ShowDialog($"{msg}解绑调用接口调用3次失败！", MessageType.MsgAlarm);
                //            break;
                //        }
                //        else if(count >= 3 && !MachineCtrl.GetInstance().UpdataMes)
                //        {
                //            break;
                //        }
                //        else
                //        {
                //            count++;
                //        }
                //    }
                   
                //}
                //while (count != 0);
                //if (!Jeve_Mes.Mes_GetVehicleInfo(vehicleno, workplace, checkroute, unbind, ref listBasketData, ref msg) && !string.IsNullOrWhiteSpace(vehicleno) && !vehicleno.Contains("ERROR"))
                //{
                //        ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                //        count++;
                //        return;
                //}
                onloadRobot.onloadData.palletWriteFlag = false;
                onloadRobot.onloadClient.SetLoadingData(LoadingCmd.WritePalletFlag, onloadRobot.onloadData);

            }
            if (onloadRobot.onloadData.batReadFlag)
            {
                for (int batIdx = 0; batIdx < 2; batIdx++)
                {
                    this.Battery[batIdx].Code = onloadRobot.onloadData.batCode[batIdx];
                if (this.Battery[batIdx].Code.Contains("ERROR"))
                    {
                        onloadRobot.onloadData.batFlag[batIdx] = 2;
                    }
                    if (string.IsNullOrEmpty(this.Battery[batIdx].Code))
                    {
                        onloadRobot.onloadData.batFlag[batIdx] = 2;
                    }
                    if(!string.IsNullOrEmpty(this.Battery[batIdx].Code) && !this.Battery[batIdx].Code.Contains("ERROR"))
                    {
                        onloadRobot.onloadData.batFlag[batIdx] = 1;
                        string msg = "";
                        ListBasketData listbaskdata = new ListBasketData();
                        string lotno = "";
                        string workplace = "DAL1HK01";
                        string checkroute = "1";
                        string unbind = "1";
                        lotno = this.onloadRobot.onloadData.batCode[batIdx];
                        //lotno = this.Battery[batIdx].Code;
                        //MySqlBatteryBasket.BasketData basketData = new MySqlBatteryBasket.BasketData();
                        //basketData.LotNo = this.Battery[batIdx].Code;
                        MesRecipeStruct mesRecipeStruct = new MesRecipeStruct();
                        string oporder = "";
                        string vehicleno = "";

                        if (!Def.IsNoHardware() && !Jeve_Mes.Mes_GetVehicleInfo(lotno, workplace, checkroute, unbind, /*ref listBasketData,*/ ref msg))
                            {
                            onloadRobot.onloadData.batFlag[batIdx] = 3;
                            string file = string.Format(@"{0}\{1}\{2}.csv", MachineCtrl.GetInstance().ProductionFilePath, "电池校验NG", DateTime.Now.ToString("yyyy-MM-dd"));
                            string msgData = $"扫码校验MES-NG：{this.Battery[batIdx].Code}";
                            this.dbRecord.AddAlarmInfo(new AlarmFormula(Def.GetProductFormula(), (int)RunID.OnloadScan, msgData, (int)MessageType.MsgMessage, (int)RunID.OnloadScan, MachineCtrl.GetInstance().MachineName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                            this.Battery[batIdx].Type = BatteryStatus.NG;
                            this.Battery[batIdx].NGType = BatteryNGStatus.MesNG;
                        }
                        else
                        {
                            if (!Jeve_Mes.Mes_GetRunOpList(workplace, oporder, ref mesRecipeStruct, ref msg))
                            {
                                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgWarning);
                            }
                            if (!Jeve_Mes.Mes_GetParam(lotno, vehicleno, MesResources.OpOrder, workplace, ref mesRecipeStruct, ref msg))
                            {
                                //ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                            }
                        }
                    }
                }
                //for (int i = 0; i < 24; i++)
                //{
                //    if (!string.IsNullOrEmpty(this.Battery[i].Code) && !this.Battery[i].Code.Contains("ERROR"))
                //    {
                //    onloadRobot.onloadData.batFlag[i] = 1;
                //    string msg = "";
                //        ListBasketData listbaskdata = new ListBasketData();
                //        MySqlBatteryBasket.BasketData basketData = new MySqlBatteryBasket.BasketData();
                //        basketData.LotNo = this.Battery[i].Code;
                //        if (!Def.IsNoHardware() /*&& MachineCtrl.GetInstance().UpdataMes && MachineCtrl.GetInstance().isMESConnect*/ &&  !MySqlBatteryBasket.LnQuire(basketData, ref listbaskdata))
                //        {
                //            onloadRobot.onloadData.batFlag[i] = 3;
                //            string file = string.Format(@"{0}\{1}\{2}.csv", MachineCtrl.GetInstance().ProductionFilePath, "电池校验NG", DateTime.Now.ToString("yyyy-MM-dd"));
                //            string msgData = $"扫码校验MES-NG：{this.Battery[i].Code}";
                //            this.dbRecord.AddAlarmInfo(new AlarmFormula(Def.GetProductFormula(), (int)RunID.OnloadScan, msgData, (int)MessageType.MsgMessage, (int)RunID.OnloadScan, MachineCtrl.GetInstance().MachineName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                //            this.Battery[i].Type = BatteryStatus.NG;
                //            this.Battery[i].NGType = BatteryNGStatus.MesNG;
                //        }

                //        //else if (Def.IsNoHardware() && this.randNGBat)
                //        //{
                //        //    this.Battery[i].Type = (this.scanEnable[i] ? BatteryStatus.OK : (BatteryStatus)rd.Next(1, rd.Next(1, 4)));
                //        //    this.Battery[i].NGType = this.Battery[i].Type == BatteryStatus.NG ? BatteryNGStatus.MesNG : BatteryNGStatus.Invalid;
                //        //}
                //    }
                //}
                onloadRobot.onloadData.batWriteFlag = true;
                onloadRobot.onloadClient.SetLoadingData(LoadingCmd.WriteBarcode, onloadRobot.onloadData);
                onloadRobot.onloadData.batReadFlagReset = false;
                onloadRobot.onloadClient.SetLoadingData(LoadingCmd.WriteBarcodeFlag, onloadRobot.onloadData);
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
                        this.nextInitStep = InitSteps.Init_CheckBattery;
                        break;
                    }
                case InitSteps.Init_CheckBattery:
                    {
                        CurMsgStr("检查电池状态", "Check sensor");
                        if (CheckInputState(IRecvPosInpos, !RecvPosIsEmpty()))
                        {
                            this.nextInitStep = InitSteps.Init_End;
                        }
                        break;
                    }

                case InitSteps.Init_ConnectScanner:
                    {
                        CurMsgStr("连接扫码枪", "Connect scanner");
                        for(int i = 0; i < barcodeScan.Length; i++)
                        {
                            if (!ScanConnect(i, true))
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
                Sleep(50);
            }

            switch ((AutoSteps)this.nextAutoStep)
            {
                case AutoSteps.Auto_WaitWorkStart:
                    {
                        CurMsgStr("等待开始信号", "Wait work start");
                        string msg = "";
                        //this.Battery = onloadRobot.onloadData.lineSignal;
                        //Sleep(5000);
                        //Console.WriteLine(onloadRobot.onloadData.batReadFlag.ToString());
                        //if (onloadRobot.onloadData.batReadFlag)
                        //{
                        //    for (int batIdx = 0; batIdx < this.Battery.Length; batIdx++)
                        //    { 
                        //        this.Battery[batIdx].Code = onloadRobot.onloadData.batCode[batIdx];
                        //        if (this.Battery[batIdx].Code.Contains("ERROR"))
                        //        {
                        //            onloadRobot.onloadData.batFlag[batIdx] = 2;
                        //            //this.Battery[batIdx].Release();
                        //        }
                        //        if (string.IsNullOrEmpty(this.Battery[batIdx].Code))
                        //        {
                        //            onloadRobot.onloadData.batFlag[batIdx] = 2;
                        //            //this.Battery[batIdx].Release();
                        //        }
                        //    }
                         
                        //    this.nextAutoStep = AutoSteps.Auto_MesCheckCode;
                        //    SaveRunData(SaveType.AutoStep);
                        //}
                        break;
                    }
                case AutoSteps.Auto_MesCheckCode:
                    {
                        CurMsgStr("MES校验电芯条码", "MES check battery code");
                        Console.WriteLine(DateTime.Now+"MES校验电芯条码");
                        bool result = true;
                        Random rd = new Random();
                        for (int i = 0; i < 2; i++)
                        {
                            if (!string.IsNullOrEmpty(this.Battery[i].Code)&&!this.Battery[i].Code.Contains("ERROR"))
                            {
                                onloadRobot.onloadData.batFlag[i] = 1;
                                string msg = "";
                                if (!Def.IsNoHardware() && MachineCtrl.GetInstance().UpdataMes && MachineCtrl.GetInstance().isMESConnect && !MachineCtrl.GetInstance().ACPROCESSCHECK_Main(MesResources.Equipment, this.Battery[i].Code, ref msg))
                                {
                                    onloadRobot.onloadData.batFlag[i] = 3;
                                    string file = string.Format(@"{0}\{1}\{2}.csv", MachineCtrl.GetInstance().ProductionFilePath, "电池校验NG", DateTime.Now.ToString("yyyy-MM-dd"));
                                    string msgData = $"扫码校验MES-NG：{this.Battery[i].Code}";
                                    this.dbRecord.AddAlarmInfo(new AlarmFormula(Def.GetProductFormula(), (int)RunID.OnloadScan, msgData, (int)MessageType.MsgMessage, (int)RunID.OnloadScan, MachineCtrl.GetInstance().MachineName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                                    this.Battery[i].Type = BatteryStatus.NG;
                                    this.Battery[i].NGType = BatteryNGStatus.MesNG;
                                }

                                else if (Def.IsNoHardware() && this.randNGBat)
                                {
                                    this.Battery[i].Type = (this.scanEnable[i] ? BatteryStatus.OK : (BatteryStatus)rd.Next(1, rd.Next(1, 4)));
                                    this.Battery[i].NGType = this.Battery[i].Type == BatteryStatus.NG ? BatteryNGStatus.MesNG : BatteryNGStatus.Invalid;
                                }
                            }
                        }
                        onloadRobot.onloadData.batWriteFlag = true;
                        onloadRobot.onloadClient.SetLoadingData(LoadingCmd.WriteBarcode, onloadRobot.onloadData);
                        onloadRobot.onloadData.batReadFlagReset = false;
                        onloadRobot.onloadClient.SetLoadingData(LoadingCmd.WriteBarcodeFlag, onloadRobot.onloadData);
                        if (result)
                        {
                            this.nextAutoStep = AutoSteps.Auto_WorkEnd;
                            SaveRunData(SaveType.AutoStep);
                        }
                        break;
                    }
                case AutoSteps.Auto_WorkEnd:
                    {
                        CurMsgStr("工作完成", "Work end");
                        if (!onloadRobot.onloadData.batReadFlag || !onloadRobot.onloadData.UpPalletFlag)
                        {
                            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
                            SaveRunData(SaveType.AutoStep);
                        }
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

        /// <summary>
        /// 初始化通用组参数 + 模组参数
        /// </summary>
        protected override void InitParameter()
        {
            int scanNum = (int)ModDef.RecvPos_ALL;
            this.conveyerLineEN = true;
            this.recvBatteryEnable = true;
            this.scanEnable = new bool[scanNum];
            this.barcodeScanIP = new string[scanNum];
            this.barcodeScanCom = new int[scanNum];
            this.barcodeScanPort = new int[scanNum];
            for(int i = 0; i < scanNum; i++)
            {
                this.scanEnable[i] = true;
                this.barcodeScanIP[i] = string.Empty;
                this.barcodeScanCom[i] = -1;
                this.barcodeScanPort[i] = 0;
            }
            this.scanCmd = "Scan";
            this.scanLinefeed = true;
            this.codeLength = -1;
            this.codeType = string.Empty;
            this.scanNGType = "ERROR";
            this.scanMaxCount = 1;
            this.recvDelay = 100;

            this.codeTypeArray = new string[scanNum];
            this.barcodeScan = new BarcodeScan[scanNum];
            for(int i = 0; i < this.barcodeScan.Length; i++)
            {
                this.barcodeScan[i] = new BarcodeScan();
            }
            this.randNGBat = false;
            this.recvTimeOut = 20;
            //this.scanNum = 1;

            base.InitParameter();
        }

        /// <summary>
        /// 读取通用组参数 + 模组参数
        /// </summary>
        /// <returns></returns>
        public override bool ReadParameter()
        {
            for(int i = 0; i < this.scanEnable.Length; i++)
            {
                this.scanEnable[i] = ReadBoolParameter(this.RunModule, ("scanEnable" + i), this.scanEnable[i]);
                this.barcodeScanIP[i] = ReadStringParameter(this.RunModule, ("barcodeScanIP" + i), this.barcodeScanIP[i]);
                this.barcodeScanCom[i] = ReadIntParameter(this.RunModule, ("barcodeScanCom" + i), this.barcodeScanCom[i]);
                this.barcodeScanPort[i] = ReadIntParameter(this.RunModule, ("barcodeScanPort" + i), this.barcodeScanPort[i]);
            }
            this.scanCmd = ReadStringParameter(this.RunModule, "scanCmd", this.scanCmd);
            this.scanLinefeed = ReadBoolParameter(this.RunModule, "scanLinefeed", this.scanLinefeed);
            this.codeLength = ReadIntParameter(this.RunModule, "codeLength", this.codeLength);
            this.codeType = ReadStringParameter(this.RunModule, "codeType", this.codeType);
            this.scanNGType = ReadStringParameter(this.RunModule, "scanNGType", this.scanNGType);
            this.codeTypeArray = this.codeType.Split((new char[] { ',' }), StringSplitOptions.RemoveEmptyEntries);
            this.conveyerLineEN = ReadBoolParameter(this.RunModule, "conveyerLineEN", this.conveyerLineEN);
            this.recvBatteryEnable =  ReadBoolParameter(this.RunModule, "RecvBatteryEnable", this.recvBatteryEnable);
            this.randNGBat = ReadBoolParameter(this.RunModule, "randNGBat", this.randNGBat);
            this.scanMaxCount = ReadIntParameter(this.RunModule, "scanMaxCount", this.scanMaxCount);
            this.recvDelay = ReadIntParameter(this.RunModule, "recvDelay", this.recvDelay);
            this.recvTimeOut = ReadIntParameter(this.RunModule, "recvTimeOut", this.recvTimeOut);
            //this.scanNum = ReadIntParameter(this.RunModule, "scanNum", this.scanNum);
            

            if (!Def.IsNoHardware())
                this.randNGBat = false;

            return base.ReadParameter();
        }

        /// <summary>
        /// 读取本模组的相关模组
        /// </summary>
        public override void ReadRelatedModule()
        {
            string strValue = "";
            string strModule = RunModule;

            // 取电池模组
            onloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
            //// 上料模组
            //strValue = iniStream.ReadString(strModule, "OnloadRobot", "");
            //onloadRobot = MachineCtrl.GetInstance().GetModule(strValue) as RunProcessOnloadRobot;
        }

        #endregion

        #region // IO及电机

        /// <summary>
        /// 初始化模组IO及电机
        /// </summary>
        protected override void InitModuleIOMotor()
        {
            this.IRecvPosEnter = AddInput("IRecvPosEnter");
            this.IRecvPosInpos = AddInput("IRecvPosInpos");
            this.IPositionPush = AddInput("IPositionPush");
            this.IPositionPull = AddInput("IPositionPull");
            this.IResponseSend = AddInput("IResponseSend");

            this.OPositionPush = AddOutput("OPositionPush");
            //this.OPositionPull = AddOutput("OPositionPull");
            this.ORecvPosMotor = AddOutput("ORecvPosMotor");
            //this.ORequireMaterial = AddOutput("ORequireMaterial");
            this.ORequireSend = AddOutput("ORequireSend");
            this.ORequireSendEnd = AddOutput("ORequireSendEnd");
        }

        /// <summary>
        /// 定位气缸推出
        /// </summary>
        /// <param name="push">true推出，false回退</param>
        /// <returns></returns>
        protected bool PositionPush(bool push)
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            // 检查IO配置
            //if((IPositionPush < 0) || (IPositionPull < 0)
            //    || (OPositionPush < 0 && OPositionPull < 0))
            if ((IPositionPush < 0) || (IPositionPull < 0) || (OPositionPush < 0))
            {
                return false;
            }
            // 操作
            OutputAction(OPositionPush, push);
            //OutputAction(OPositionPull, !push);
            // 检查到位
            // 仅有其一ON时才认为状态正确
            //if(!WaitInputState(IPositionPush, push) || !WaitInputState(IPositionPull, !push))
            // 这里没有回退，只是判断一个信号
            if (!WaitInputState(IPositionPush, push) || !WaitInputState(IPositionPull, !push))
            {
                return false;
            }
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
            for(int i = 0; i < 2; i++)
            {
                if(BatteryStatus.Invalid == this.Battery[i].Type)
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
            for(int i = 0; i < 2; i++)
            {
                if(BatteryStatus.Invalid != this.Battery[i].Type)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 电池已扫码
        /// </summary>
        /// <returns></returns>
        public bool BatScanCodeFinish()
        {
            for(int i = 0; i < 2; i++)
            {
                if ((BatteryStatus.OK == this.Battery[i].Type) && string.IsNullOrEmpty(this.Battery[i].Code))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region // 扫码器

        /// <summary>
        /// 扫码器的连接地址信息
        /// </summary>
        /// <returns></returns>
        public string ScanAdderInfo(int index)
        {
            return this.barcodeScan[index].AdderInfo();
        }

        /// <summary>
        /// 扫码器连接状态
        /// </summary>
        /// <param name="index">扫码器索引</param>
        /// <returns></returns>
        public bool ScanIsConnect(int index)
        {
            if(!this.scanEnable[index])
            {
                return true;
            }
            return this.barcodeScan[index].IsConnect();
        }

        /// <summary>
        /// 扫码器连接
        /// </summary>
        /// <param name="index">扫码器索引</param>
        /// <param name="connect">true连接，false断开</param>
        /// <returns></returns>
        public bool ScanConnect(int index, bool connect = true)
        {
            if(!this.scanEnable[index] || Def.IsNoHardware())
            {
                return true;
            }

            if (connect)
            {
                if(string.IsNullOrEmpty(this.barcodeScanIP[index]) && (this.barcodeScanCom[index] > -1))
                {
                    return this.barcodeScan[index].ConnectCom(this.barcodeScanCom[index], this.barcodeScanPort[index], (this.scanLinefeed ? "\r\n" : "\n"));
                }
                else if(!string.IsNullOrEmpty(this.barcodeScanIP[index]) && (this.barcodeScanCom[index] < 0))
                {
                    return this.barcodeScan[index].ConnectSocket(this.barcodeScanIP[index], this.barcodeScanPort[index]);
                }
            }
            else
            {
                return this.barcodeScan[index].Disconnect();
            }
            return false;
        }

        /// <summary>
        /// 扫码器触发扫码
        /// </summary>
        /// <returns></returns>
        public bool ScanCode(int index)
        {
            if(!this.scanEnable[index] || Def.IsNoHardware())
            {
                return true;
            }
            string errMsg = "";
            if (this.barcodeScan[index].Send(scanCmd + (scanLinefeed ? "\r\n" : ""), ref errMsg))
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
        public bool GetScanResult(int index, ref string code, int timeout = 5 * 1000)
        {
            if(!this.scanEnable[index] || Def.IsNoHardware())
            {
                return true;
            }
            if(this.barcodeScan[index].Recv(ref code, timeout))
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
        public bool CheckScanCode(int index, string code, bool alm)
        {
            if(!this.scanEnable[index] || Def.IsNoHardware())
            {
                return true;
            }
            string msg, disp;
            if(!string.IsNullOrEmpty(this.scanNGType) && (code.IndexOf(this.scanNGType) > -1))
            {
                if (alm)
                {
                    msg = string.Format("扫码器扫码失败，扫码器反馈：{0}", code);
                    disp = "请检查扫码器";
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
            if(this.codeTypeArray.Length > 0)
            {
                bool result = false;
                foreach(var item in this.codeTypeArray)
                {
                    if(code.EndsWith(item))
                    {
                        result = true;
                        break;
                    }
                }
                if(!result)
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
            base.AfterStopAction();
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
        #region // 模组重置

        public override void ManualResetEvent()
        {
            LoadRunData();
            this.nextAutoStep = AutoSteps.Auto_WaitWorkStart;
            SaveRunData(SaveType.AutoStep);
        }
        #endregion
    }
}
