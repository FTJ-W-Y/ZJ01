using HelperLibrary;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Machine
{
    public partial class OnlineDebugPage : FormEx
    {
        private int ShowPltId = -1;
        public OnlineDebugPage()
        {
            InitializeComponent();

            CreateScanList();
            CreatePalletModuleList();
            CreateBatModuleList();
            CreatePalletEventList();
        }

        #region // 界面

        private void OnlineDebugPage_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// 关闭窗口前销毁自定义非托管资源
        /// </summary>
        /// <returns></returns>
        public override void DisposeForm()
        {
        }

        /// <summary>
        /// UI界面可见性发生改变
        /// </summary>
        /// <param name="show">是否在前台显示</param>
        public override void UIVisibleChanged(bool show)
        {
            if (show)
            {
                UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState(), MachineCtrl.GetInstance().dbRecord.UserLevel());
            }
            else
            {
                UpdataUIEnable(SystemControlLibrary.MCState.MCRunning, SystemControlLibrary.UserLevelType.USER_LOGOUT);
            }
            base.UIVisibleChanged(show);
        }

        /// <summary>
        /// 当设备状态或用户权限改变时，更新UI界面的使能
        /// </summary>
        /// <param name="mc">j设备运行状态</param>
        /// <param name="level">用户等级</param>
        public override void UpdataUIEnable(SystemControlLibrary.MCState mc, SystemControlLibrary.UserLevelType level)
        {
            try
            {
                if((SystemControlLibrary.MCState.MCInitializing == mc) || (SystemControlLibrary.MCState.MCRunning == mc))
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                else if(level <= SystemControlLibrary.UserLevelType.USER_MAINTENANCE)
                {
                    SetUIEnable(UIEnable.MaintenanceEnabled);
                }
                else
                {
                    SetUIEnable(UIEnable.OperatorEnabled);
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog("DryingOvenPage.UpdataUIEnable()", ex.Message, LogType.Error);
            }
            base.UpdataUIEnable(mc, level);
        }

        /// <summary>
        /// 设置界面控件使能
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="level"></param>
        private void SetUIEnable(UIEnable uiEN)
        {
            this.Invoke(new Action(()=>
            {
                switch(uiEN)
                {
                    case UIEnable.AllDisabled:
                        this.Enabled = false;
                        break;
                    case UIEnable.AllEnabled:
                    case UIEnable.AdminEnabled:
                    case UIEnable.MaintenanceEnabled:
                        this.Enabled = true;

                        this.buttonPalletAdd.Enabled = true;
                        this.buttonPalletClear.Enabled = true;
                        this.btnScandCode.Enabled = true;
                        this.buttonPalletTestBat.Enabled = true;
                        this.buttonPalletFull.Enabled = true;
                        this.buttonPalletNG.Enabled = true;
                        this.buttonServerRestart.Enabled = false;
                        break;
                    case UIEnable.OperatorEnabled:
                        this.Enabled = true;

                        this.buttonPalletAdd.Enabled = false;
                        this.buttonPalletClear.Enabled = false;
                        this.btnScandCode.Enabled = false;
                        this.buttonPalletTestBat.Enabled = false;
                        this.buttonPalletFull.Enabled = false;
                        this.buttonPalletNG.Enabled = false;
                        this.buttonServerRestart.Enabled = false;
                        break;
                    default:
                        break;
                }
            }));
        }

        #endregion

        #region // 扫码器

        private void CreateScanList()
        {
            RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.OnloadScan);
            if(null != run)
            {
                this.comboBoxScanChose.Items.Add(run.RunName + " - 扫码器1");
                this.comboBoxScanChose.Items.Add(run.RunName + " - 扫码器2");
                this.comboBoxScanChose.Items.Add(run.RunName + " - 扫码器3");
                this.comboBoxScanChose.Items.Add(run.RunName + " - 扫码器4");
            }
            run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot);
            if(null != run)
            {
                this.comboBoxScanChose.Items.Add(run.RunName + " - 扫码器");
            }
            if(this.comboBoxScanChose.Items.Count > 0)
            {
                this.comboBoxScanChose.SelectedIndex = 0;
            }
        }
        

        private void comboBoxScanChose_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            int idx = this.comboBoxScanChose.SelectedIndex;
            if (idx < 2)
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv) as RunProcessOnloadScan;
                if (null != run)
                {
                    this.labelScanAdder.Text = run.ScanAdderInfo(idx);
                    this.labelScanConState.Text = run.ScanIsConnect(idx) ? "已连接" : "断开";
                }
            }
            else
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                if(null != run)
                {
                    this.labelScanAdder.Text = run.ScanAdderInfo();
                    this.labelScanConState.Text = run.ScanIsConnect() ? "已连接" : "断开";
                }
            }
        }

        private void buttonScanConnect_Click(object sender, System.EventArgs e)
        {
            int idx = this.comboBoxScanChose.SelectedIndex;
            if(idx < 2)
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadScan) as RunProcessOnloadScan;
                if(null != run)
                {
                    if (!run.ScanConnect(idx, true))
                    {
                        ShowMsgBox.Show("连接失败", MessageType.MsgMessage);
                    }
                    this.labelScanAdder.Text = run.ScanAdderInfo(idx);
                    this.labelScanConState.Text = run.ScanIsConnect(idx) ? "已连接" : "断开";
                }
            }
            else
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                if(null != run)
                {
                    if (!run.ScanConnect(true))
                    {
                        ShowMsgBox.Show("连接失败", MessageType.MsgMessage);
                    }
                    this.labelScanAdder.Text = run.ScanAdderInfo();
                    this.labelScanConState.Text = run.ScanIsConnect() ? "已连接" : "断开";
                }
            }
        }

        private void buttonScanDisconnect_Click(object sender, System.EventArgs e)
        {
            int idx = this.comboBoxScanChose.SelectedIndex;
            if(idx < 2)
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv) as RunProcessOnloadScan;
                if(null != run)
                {
                    if (!run.ScanConnect(idx, false))
                    {
                        ShowMsgBox.Show("断开失败", MessageType.MsgMessage);
                    }
                    //this.labelScanAdder.Text = run.ScanAdderInfo(idx);
                    this.labelScanConState.Text = run.ScanIsConnect(idx) ? "已连接" : "断开";
                }
            }
            else
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                if(null != run)
                {
                    if (!run.ScanConnect(false))
                    {
                        ShowMsgBox.Show("断开失败", MessageType.MsgMessage);
                    }
                    //this.labelScanAdder.Text = run.ScanAdderInfo();
                    this.labelScanConState.Text = run.ScanIsConnect() ? "已连接" : "断开";
                }
            }
        }

        private void buttonScanCode_Click(object sender, System.EventArgs e)
        {
            int idx = this.comboBoxScanChose.SelectedIndex;
            if(idx < 2)
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv) as RunProcessOnloadScan;
                if(null != run)
                {
                    if (run.ScanCode(idx))
                    {
                        string code = "";
                        run.GetScanResult(idx, ref code);
                        this.textBoxCodeData.Text = code;
                    }
                }
            }
            else
            {
                var run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                if(null != run)
                {
                    if(run.ScanCode())
                    {
                        string code = "";
                        run.GetScanResult(ref code);
                        this.textBoxCodeData.Text = code;
                    }
                }
            }
        }

        #endregion

        #region // 模组通讯服务

        private void buttonServerRestart_Click(object sender, System.EventArgs e)
        {
            MachineCtrl.GetInstance().CreateServer();
        }

        private void buttonClientReconnect_Click(object sender, System.EventArgs e)
        {
            if(MachineCtrl.GetInstance().ConnectClient())
            {
                ShowMsgBox.ShowDialog("模组通讯客户端重连服务器成功", MessageType.MsgMessage);
            }
        }

        #endregion

        #region // 添加删除夹具

        private void CreatePalletModuleList()
        {
            RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot);
            if(null != run)
            {
                this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            }
            run = MachineCtrl.GetInstance().GetModule(RunID.PalletBuffer);
            if(null != run)
            {
                this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            }
            run = MachineCtrl.GetInstance().GetModule(RunID.ManualOperate);
            if(null != run)
            {
                this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            }
            //for(RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            //{
            //    run = MachineCtrl.GetInstance().GetModule(id);
            //    if(null != run)
            //    {
            //        this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //    }
            //}
            //run = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery);
            //if(null != run)
            //{
            //    this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //}
        }

        private void CreateBatModuleList()
        {
            RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot);
            if (null != run)
            {
                this.comboBoxBatModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            }

            //run = MachineCtrl.GetInstance().GetModule(RunID.OnloadLine);
            //if (null != run)
            //{
            //    this.comboBoxBatModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //}

            //run = MachineCtrl.GetInstance().GetModule(RunID.OnloadScan);
            //if (null != run)
            //{
            //    this.comboBoxBatModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //}

            //run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRecv);
            //if (null != run)
            //{
            //    this.comboBoxBatModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //}
            checkBoxFinger.Checked = MachineCtrl.GetInstance().FingerCheckCanActive;
        }

        private void CreatePalletEventList()
        {
            RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot);
            if (null != run)
            {
                this.cmbEventPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            }
            run = MachineCtrl.GetInstance().GetModule(RunID.PalletBuffer);
            if (null != run)
            {
                this.cmbEventPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            }
            run = MachineCtrl.GetInstance().GetModule(RunID.ManualOperate);
            if (null != run)
            {
                this.cmbEventPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            }
            //for(RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            //{
            //    run = MachineCtrl.GetInstance().GetModule(id);
            //    if(null != run)
            //    {
            //        this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //    }
            //}
            //run = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery);
            //if(null != run)
            //{
            //    this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //}

            //Invalid = 0,                        // 无效状态
            //Require,                            // 请求状态
            //Response,                           // 响应状态
            //Ready,                              // 准备状态
            //Start,                              // 开始状态
            //Finished,                           // 完成状态
            //Cancel,                             // 取消状态

            cmbCurEvent.Items.Add("无效状态");
            cmbCurEvent.Items.Add("请求状态");
            cmbCurEvent.Items.Add("响应状态");
            cmbCurEvent.Items.Add("准备状态");
            cmbCurEvent.Items.Add("开始状态");
            cmbCurEvent.Items.Add("完成状态");
            cmbCurEvent.Items.Add("取消状态");

            cmbNewEvent.Items.Add("无效状态");
            cmbNewEvent.Items.Add("请求状态");
            cmbNewEvent.Items.Add("响应状态");
            cmbNewEvent.Items.Add("准备状态");
            cmbNewEvent.Items.Add("开始状态");
            cmbNewEvent.Items.Add("完成状态");
            cmbNewEvent.Items.Add("取消状态");
        }
        
        private void comboBoxPalletModule_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    RunID runID = (RunID)Convert.ToInt32(txt[0]);
                    RunProcess run = MachineCtrl.GetInstance().GetModule(runID);
                    if (null != run)
                    {
                        if (RunID.OnloadRobot == runID)
                        {
                            btnScandCode.Enabled = true;
                        }
                        else
                        {
                            btnScandCode.Enabled = false;
                        }

                        this.comboBoxPalletID.Items.Clear();
                        int pltLen = run.Pallet.Length;
                        for(int i = 0; i < pltLen; i++)
                        {
                            this.comboBoxPalletID.Items.Add((i + 1).ToString());
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("comboBoxPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void buttonPalletAdd_Click(object sender, System.EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if(null != run)
                    {
                        int pltIdx = this.comboBoxPalletID.SelectedIndex;
                        if (pltIdx > -1)
                        {
                            run.ManualAddPallet(pltIdx, MachineCtrl.GetInstance().PalletMaxRow, MachineCtrl.GetInstance().PalletMaxCol, PalletStatus.OK, BatteryStatus.Invalid);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：添加空夹具{1}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("夹具索引无效，添加失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        private void buttonPalletFull_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if(null != run)
                    {
                        int pltIdx = this.comboBoxPalletID.SelectedIndex;
                        if(pltIdx > -1)
                        {
                            run.ManualAddPallet(pltIdx, MachineCtrl.GetInstance().PalletMaxRow, MachineCtrl.GetInstance().PalletMaxCol, PalletStatus.OK, BatteryStatus.OK);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：置OK电池{1}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("夹具索引无效，添加失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        private void buttonPalletClear_Click(object sender, System.EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    string msg = "删除夹具后必须手动将夹具从系统中插出来并拿走夹具中的电池！且无法添加电池数据！\r\n请确认是否删除夹具！";
                    if (System.Windows.Forms.DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                    {
                        return;
                    }
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if(null != run)
                    {
                        int pltIdx = this.comboBoxPalletID.SelectedIndex;
                        if(pltIdx > -1)
                        {
                            run.ManualClearPallet(pltIdx);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：删除夹具{1}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("夹具索引无效，删除失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletClear_Click() error : " + ex.Message);
            }
        }

        private void buttonPalletNG_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if(null != run)
                    {
                        int pltIdx = this.comboBoxPalletID.SelectedIndex;
                        if(pltIdx > -1)
                        {
                            run.ManualAddPallet(pltIdx, MachineCtrl.GetInstance().PalletMaxRow, MachineCtrl.GetInstance().PalletMaxCol, PalletStatus.NG, BatteryStatus.OK);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：置NG转盘夹具{1}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("夹具索引无效，添加失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch(System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        #endregion

        #region // 冷却系统R轴调试

        private void buttonRHome_Click(object sender, EventArgs e)
        {
            RunProcessCoolingSystem run = MachineCtrl.GetInstance().GetModule(RunID.CoolingSystem) as RunProcessCoolingSystem;
            if (null != run)
            {
                Task.Run(() => { run.MotorRHome(); });
               
            }
        }

        private void buttonRForward_Click(object sender, EventArgs e)
        {
            RunProcessCoolingSystem run = MachineCtrl.GetInstance().GetModule(RunID.CoolingSystem) as RunProcessCoolingSystem;
            if (null != run)
            {
                Task.Run(() =>
                {
                    run.MotorRMove(false);
                }
                );
            }
        }

        private void buttonRBackoff_Click(object sender, EventArgs e)
        {
            RunProcessCoolingSystem run = MachineCtrl.GetInstance().GetModule(RunID.CoolingSystem) as RunProcessCoolingSystem;
            if (null != run)
            {
                Task.Run(() =>
                {
                    run.MotorRMove(true);
                });
            }
        }

        #endregion

        private void StopFord_Click(object sender, EventArgs e)
        {
            RunProcessCoolingSystem run = MachineCtrl.GetInstance().GetModule(RunID.CoolingSystem) as RunProcessCoolingSystem;
            if (null != run)
            {
                run.MotorStop();
            }
          }

        private void StopBack_Click(object sender, EventArgs e)
        {
            RunProcessCoolingSystem run = MachineCtrl.GetInstance().GetModule(RunID.CoolingSystem) as RunProcessCoolingSystem;
            if (null != run)
            {
                run.MotorStop();
            }
        }

        private void btnYAXrecv_Click(object sender, EventArgs e)
        {
            RunProcessOffloadOut run = MachineCtrl.GetInstance().GetModule(RunID.OffloadOut) as RunProcessOffloadOut;
            if (null != run)
            {
                Task.Run(() =>
                {
                    run.YAXMotorCanRecvBat();
                }
                );
            }
        }

        private void btnYAXsend_Click(object sender, EventArgs e)
        {
            RunProcessOffloadOut run = MachineCtrl.GetInstance().GetModule(RunID.OffloadOut) as RunProcessOffloadOut;
            if (null != run)
            {
                Task.Run(() =>
                {
                    run.YAXMotorCanSendBat();
                }
                );
            }
        }

        private void btnAXRecv_Click(object sender, EventArgs e)
        {
            RunProcessOffloadOut run = MachineCtrl.GetInstance().GetModule(RunID.OffloadOut) as RunProcessOffloadOut;
            if (null != run)
            {
                Task.Run(() =>
                {
                    run.AXMotorCanRecvBat();
                }
                );
            }
        }

        private void btnAXsend_Click(object sender, EventArgs e)
        {
            RunProcessOffloadOut run = MachineCtrl.GetInstance().GetModule(RunID.OffloadOut) as RunProcessOffloadOut;
            if (null != run)
            {
                Task.Run(() =>
                {
                    run.AXMotorCanSendBat();
                }
                );
            }
        }

        private void checkWritePlcLog_CheckedChanged(object sender, EventArgs e)
        {
        }
        
        private void panel1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {

        }

        private void comboBoxBatModule_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxBatModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        this.comboBoxRowId.Items.Clear();
                        this.comboBoxColId.Items.Clear();

                        int pltLen = run.Pallet.Length;
                        int batLen = run.Battery.Length;

                        this.comboBoxColId.Items.Add("1");

                        if (pltLen > 0)
                        {
                            ShowPltId = 0;
                            checkBoxPallet.Enabled = true;
                            checkBoxPallet.Checked = false;
                            comboBoxPalletID2.Enabled = true;
                            comboBoxPalletID2.Items.Clear();
                            for (int i = 0; i < pltLen; i++)
                            {
                                comboBoxPalletID2.Items.Add($"夹具{i + 1}");
                            }
                            comboBoxPalletID2.SelectedIndex = 0;
                            for (int i = 0; i < batLen; i++)
                            {
                                if (i < 4)
                                {
                                    this.comboBoxRowId.Items.Add($"夹爪{i + 1}");
                                }
                                else
                                {
                                    this.comboBoxRowId.Items.Add($"暂存{i%4 + 1}");
                                }
                            }
                            this.comboBoxRowId.SelectedIndex = 0;
                            this.comboBoxColId.SelectedIndex = 0;
                        }
                        else
                        {
                            ShowPltId = -1;
                            checkBoxPallet.Enabled = false;
                            checkBoxPallet.Checked = false;
                            comboBoxPalletID2.Enabled = false;
                            if (batLen > 0)
                            {
                                for (int i = 0; i < batLen; i++)
                                {
                                    this.comboBoxRowId.Items.Add((i + 1).ToString());
                                }
                                this.comboBoxRowId.SelectedIndex = 0;
                                this.comboBoxColId.SelectedIndex = 0;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("comboBoxPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void btnBatDel_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxBatModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    string msg = "删除电池后必须手动将工位上的电池拿走！且无法添加电池数据！\r\n请确认是否删除电池！";
                    if (System.Windows.Forms.DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                    {
                        return;
                    }
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        int pltIdx = checkBoxPallet.Checked ? comboBoxPalletID2.SelectedIndex : -1;
                        int rowIdx = this.comboBoxRowId.SelectedIndex;
                        int colIdx = this.comboBoxColId.SelectedIndex;
                        if (rowIdx > -1 && colIdx > -1)
                        {
                            run.ManualDelBattery(pltIdx, rowIdx, colIdx);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：删除电池工位{1}-{2}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("电池工位索引无效，删除失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletClear_Click() error : " + ex.Message);
            }
        }

        private void btnBatAdd_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxBatModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    string msg = "添加电池后必须手动将电池放到工位上！且无法添加电池数据！\r\n请确认是否添加电池！";
                    if (System.Windows.Forms.DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                    {
                        return;
                    }
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        int pltIdx = checkBoxPallet.Checked ? comboBoxPalletID2.SelectedIndex : -1;
                        int rowIdx = this.comboBoxRowId.SelectedIndex;
                        int colIdx = this.comboBoxColId.SelectedIndex;
                        string code = this.txtBatteryCode.Text.Trim();
                        if (string.IsNullOrEmpty(code))
                        {
                            msg = "请确认先扫码电池！";
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion);
                            return;
                        }
                        if (rowIdx > -1 && colIdx > -1)
                        {
                            msg = "";
                            if (!Def.IsNoHardware() && MachineCtrl.GetInstance().UpdataMes && !MesOperate.EquToMesCheckSfc(MesResources.Equipment, code, ref msg))
                            {
                                ShowMsgBox.ShowDialog("电芯校验失败", MessageType.MsgWarning);
                                return;
                            }
                            run.ManualAddBattery(pltIdx, rowIdx, colIdx, code);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：添加电池工位{1}-{2}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("电池工位索引无效，添加失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletClear_Click() error : " + ex.Message);
            }
        }
        
        private void buttonPalletTestBat_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        int pltIdx = this.comboBoxPalletID.SelectedIndex;
                        if (pltIdx > -1)
                        {
                            run.ManualAddPalletBattery(pltIdx, MachineCtrl.GetInstance().PalletMaxRow, MachineCtrl.GetInstance().PalletMaxCol, checkBoxFake.Checked, PalletStatus.OK, BatteryStatus.OK);
                            Def.WriteLog("OtherDebugPage", string.Format("{0}：置OK电池{1}", run.RunName, (pltIdx + 1)));
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("夹具索引无效，添加失败", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        private void checkBoxFinger_CheckedChanged(object sender, EventArgs e)
        {
             MachineCtrl.GetInstance().FingerCheckCanActive = checkBoxFinger.Checked;
        }

        private void comboBoxPalletID2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowPltId = comboBoxPalletID2.SelectedIndex;
        }

        private void btnBatShow_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxBatModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        int pltIdx = checkBoxPallet.Checked ? comboBoxPalletID2.SelectedIndex : -1;
                        int rowIdx = this.comboBoxRowId.SelectedIndex;
                        int colIdx = this.comboBoxColId.SelectedIndex;
                        
                        if (rowIdx > -1 && colIdx > -1)
                        {
                            string code = "";
                            run.ManualGetBattery(pltIdx, rowIdx, colIdx, ref code);
                            txtBatteryCodeOld.Text = code;
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("电池工位索引无效", MessageType.MsgWarning);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletClear_Click() error : " + ex.Message);
            }
        }

        private void checkBoxPallet_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxPallet.Checked)
            {
                try
                {
                    string[] txt = this.comboBoxBatModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (txt.Length > 0)
                    {
                        RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                        if (null != run)
                        {
                            ShowPltId = 0;
                            this.comboBoxRowId.Items.Clear();
                            this.comboBoxColId.Items.Clear();

                            int pltLen = run.Pallet.Length;

                            comboBoxPalletID2.Enabled = true;
                            comboBoxPalletID2.Items.Clear();
                            for (int i = 0; i < pltLen; i++)
                            {
                                comboBoxPalletID2.Items.Add($"夹具{i + 1}");
                            }
                            comboBoxPalletID2.SelectedIndex = 0;

                            int PalletMaxRow = MachineCtrl.GetInstance().PalletMaxRow;
                            int PalletMaxCol = MachineCtrl.GetInstance().PalletMaxCol;
                            for (int i = 0; i < PalletMaxRow; i++)
                            {
                                this.comboBoxRowId.Items.Add($"{i + 1}");
                            }
                            for (int i = 0; i < PalletMaxCol; i++)
                            {
                                this.comboBoxColId.Items.Add($"{i + 1}");
                            }
                            this.comboBoxRowId.SelectedIndex = 0;
                            this.comboBoxColId.SelectedIndex = 0;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("comboBoxPalletModule_SelectedIndexChanged() error : " + ex.Message);
                }
            }
            else
            {
                try
                {
                    string[] txt = this.comboBoxBatModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (txt.Length > 0)
                    {
                        RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                        if (null != run)
                        {
                            ShowPltId = 0;
                            this.comboBoxRowId.Items.Clear();
                            this.comboBoxColId.Items.Clear();

                            int pltLen = run.Pallet.Length;
                            int batLen = run.Battery.Length;

                            comboBoxPalletID2.Enabled = true;
                            comboBoxPalletID2.Items.Clear();
                            for (int i = 0; i < pltLen; i++)
                            {
                                comboBoxPalletID2.Items.Add($"夹具{i + 1}");
                            }
                            comboBoxPalletID2.SelectedIndex = 0;
                            for (int i = 0; i < batLen; i++)
                            {
                                if (i < 4)
                                {
                                    this.comboBoxRowId.Items.Add($"夹爪{i + 1}");
                                }
                                else
                                {
                                    this.comboBoxRowId.Items.Add($"暂存{i % 4 + 1}");
                                }
                            }
                            comboBoxColId.Items.Add("1");
                            this.comboBoxRowId.SelectedIndex = 0;
                            this.comboBoxColId.SelectedIndex = 0;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("comboBoxPalletModule_SelectedIndexChanged() error : " + ex.Message);
                }
            }
        }

        private void btnScandCode_Click(object sender, EventArgs e)
        {
            int pltId = comboBoxPalletID.SelectedIndex;
            if (pltId == -1)
            {
                MessageBox.Show("请选择待操作托盘夹具");
                return;
            }

            ManualScanCode dlg = new ManualScanCode();
            dlg.IsHadFake = checkBoxFake.Checked;
            dlg.pltIdx = pltId;
            dlg.runID = RunID.OnloadRobot;
            dlg.ShowDialog();
        }

        private void btnEventSet_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.cmbEventPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunID runID = (RunID)Convert.ToInt32(txt[0]);
                    switch (runID)
                    {
                        case RunID.OnloadRobot:
                            {
                                RunProcessOnloadRobot run = MachineCtrl.GetInstance().GetModule(runID) as RunProcessOnloadRobot;
                                if (null != run)
                                {
                                    EventList modEvent = (EventList)(cmbEventID.SelectedIndex + (int)EventList.OnloadPlaceEmptyPallet);
                                    EventStatus state = (EventStatus)cmbNewEvent.SelectedIndex;
                                    int rowId = cmbEventPalletID.SelectedIndex;
                                    if ((rowId != -1) && run.SetEvent(run, modEvent, state, rowId))
                                    {
                                        MessageBox.Show("设置事件成功!");
                                    }
                                    else
                                    {
                                        MessageBox.Show("设置事件失败!");
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("cmbEventPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void cmbEventPalletID_SelectedIndexChanged(object sender, EventArgs e)
        {
            //获取模块事件
            try
            {
                string[] txt = this.cmbEventPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunID runID = (RunID)Convert.ToInt32(txt[0]);
                    switch (runID)
                    {
                        case RunID.OnloadRobot:
                            {
                                RunProcessOnloadRobot run = MachineCtrl.GetInstance().GetModule(runID) as RunProcessOnloadRobot;
                                if (null != run)
                                {
                                    cmbEventID.Items.Clear();
                                    //OnloadPlaceEmptyPallet,           // 
                                    //OnloadPlaceNGPallet,              // 
                                    //OnLoadPlaceDetectFakePallet,      // 
                                    //OnloadPlaceReputFakePallet,       // 
                                    //OnloadPickNGEmptyPallet,          // 
                                    //OnloadPickOKFullPallet,           // 
                                    //OnloadPickOKFakeFullPallet,       // 
                                    //OnLoadPickWaitResultPallet,       // 
                                    //OnloadPickRebakeFakePallet,       // 
                                    //OnloadPickPlaceEnd,               // 

                                    cmbEventID.Items.Add("上料区放NG非空夹具");//，转盘
                                    cmbEventID.Items.Add("上料区放待检测含假电池夹具");//（未取走假电池的夹具）
                                    cmbEventID.Items.Add("上料区放待回炉含假电池夹具");//（已取走假电池，待重新放回假电池的夹具）
                                    cmbEventID.Items.Add("上料区取NG空夹具");
                                    cmbEventID.Items.Add("上料区取OK无假电池满夹具");
                                    cmbEventID.Items.Add("上料区取OK带假电池满夹具");
                                    cmbEventID.Items.Add("上料区取等待水含量结果夹具");//（已取待测假电池的夹具）
                                    cmbEventID.Items.Add("上料区取回炉假电池夹具");//（已放回假电池的夹具）
                                    cmbEventID.Items.Add("上料区信号结束");
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("cmbEventPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void cmbEventID_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.cmbEventPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunID runID = (RunID)Convert.ToInt32(txt[0]);
                    switch (runID)
                    {
                        case RunID.OnloadRobot:
                            {
                                RunProcessOnloadRobot run = MachineCtrl.GetInstance().GetModule(runID) as RunProcessOnloadRobot;
                                if (null != run)
                                {
                                    EventList modEvent = (EventList)(cmbEventID.SelectedIndex + (int)EventList.OnloadPlaceEmptyPallet);
                                    EventStatus state = EventStatus.Invalid;

                                    state = run.GetEvent(run, modEvent);

                                    cmbCurEvent.SelectedIndex = (int)state;
                                    cmbNewEvent.SelectedIndex = (int)state;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("cmbEventPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void cmbEventPalletModule_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.cmbEventPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        this.cmbEventPalletID.Items.Clear();
                        int pltLen = run.Pallet.Length;
                        for (int i = 0; i < pltLen; i++)
                        {
                            this.cmbEventPalletID.Items.Add((i + 1).ToString());
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("cmbEventPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void btnOnloadInit_Click(object sender, EventArgs e)
        {
            //清理来料线扫码电池，取料电池，夹爪电池和缓存电池，小机器人动作
            string msg = "初始化上料线电池！将删除扫码位、取料位、夹爪和暂存位电池记忆且无法恢复！\r\n请确认是否删除并取走以上位置的电池！";
            if (System.Windows.Forms.DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
            {
                return;
            }
            // 扫码位
            RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.OnloadScan);
            if (null != run)
            {
                // 清理电池记忆
                for (int i = 0; i < run.Battery.Length; i++)
                {
                    run.Battery[i].Release();
                }
                run.SaveRunData(SaveType.Battery);
                // 取消扫码请求信号
                run.SetEvent(run, EventList.OnloadScanSendBattery, EventStatus.Invalid);
                run.ManulSetAutoStep(0);
            }

            // 取料位
            run = MachineCtrl.GetInstance().GetModule(RunID.OnloadLine);
            if (null != run)
            {
                // 清理电池记忆
                for (int i = 0; i < run.Battery.Length; i++)
                {
                    run.Battery[i].Release();
                }
                run.SaveRunData(SaveType.Battery);
                // 取消取请求信号
                run.SetEvent(run, EventList.OnloadLinePickBattery, EventStatus.Invalid);
                run.ManulSetAutoStep(0);
            }

            // 夹爪和缓存
            run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot);
            if (null != run)
            {
                // 清理电池记忆
                for (int i = 0; i < run.Battery.Length; i++)
                {
                    run.Battery[i].Release();
                }
                run.SaveRunData(SaveType.Battery);
                run.ManulSetAutoStep(0);
            }

            // NG拉带
            run = MachineCtrl.GetInstance().GetModule(RunID.OnloadNG);
            if (null != run)
            {
                // 清理电池记忆
                for (int i = 0; i < run.Battery.Length; i++)
                {
                    run.Battery[i].Release();
                }
                run.SaveRunData(SaveType.Battery);
                run.ManulSetAutoStep(0);
            }

            // 上假电池
            run = MachineCtrl.GetInstance().GetModule(RunID.OnloadFake);
            if (null != run)
            {
                // 上假电池不清理电池记忆
                run.SetEvent(run, EventList.OnloadFakePickBattery, EventStatus.Invalid); 
                run.ManulSetAutoStep(0);
            }

            // 下假电池
            run = MachineCtrl.GetInstance().GetModule(RunID.OnloadDetect);
            if (null != run)
            {
                // 清理电池记忆
                for (int i = 0; i < run.Battery.Length; i++)
                {
                    run.Battery[i].Release();
                }
                run.SaveRunData(SaveType.Battery);
                run.ManulSetAutoStep(0);
            }
        }
    }
}
