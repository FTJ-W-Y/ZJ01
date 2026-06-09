using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Machine
{
    public partial class OtherDebugPage : FormEx
    {
        public OtherDebugPage()
        {
            InitializeComponent();
            
            CreatePalletModuleList();
        }

        #region // 界面

        private void OtherDebugPage_Load(object sender, EventArgs e)
        {
            cmbPalletId.SelectedIndex = 0;
            cmbRow.SelectedIndex = 0;
            cmbCol.SelectedIndex = 0;
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
                        this.buttonPalletFull.Enabled = true;
                        this.buttonPalletNG.Enabled = true;
                        this.buttonServerRestart.Enabled = false;
                        break;
                    case UIEnable.OperatorEnabled:
                        this.Enabled = true;

                        this.buttonPalletAdd.Enabled = false;
                        this.buttonPalletClear.Enabled = false;
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
            //if(null != run)
            //{
            //    this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //}
            //run = MachineCtrl.GetInstance().GetModule(RunID.PalletBuffer);
            //if(null != run)
            //{
            //    this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //}
            //run = MachineCtrl.GetInstance().GetModule(RunID.ManualOperate);
            //if(null != run)
            //{
            //    this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //}
            //for(RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            //{
            //    run = MachineCtrl.GetInstance().GetModule(id);
            //    if(null != run)
            //    {
            //        this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //    }
            //}
            run = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery);
            if(null != run)
            {
                this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            }

            int PalletMaxRow = MachineCtrl.GetInstance().PalletMaxRow;
            int PalletMaxCol = MachineCtrl.GetInstance().PalletMaxCol;

            cmbCol.Items.Add("0");
            for (int i = 0; i < PalletMaxRow; i++)
            {
                cmbCol.Items.Add($"{i + 1}");
            }
            cmbCol.SelectedIndex = 0;

            cmbRow.Items.Add("0");
            for (int i = 0; i < PalletMaxCol; i++)
            {
                cmbRow.Items.Add($"{i + 1}");
            }
            cmbRow.SelectedIndex = 0;
        }

        private void comboBoxPalletModule_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if(txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
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
            MachineCtrl.GetInstance().WritePlcLog = checkWritePlcLog.Checked;
        }

        private void btnAddPalletBat_Click(object sender, EventArgs e)
        {
            int pltIdx = cmbPalletId.SelectedIndex + 1;
            int curCol = cmbRow.SelectedIndex;
            int curRow = cmbCol.SelectedIndex;

            if (0 == curCol && 0 == curRow)
            {
                ShowMsgBox.ShowDialog($"首行/列不能同时等于0", MessageType.MsgWarning);
                return;
            }

            bool isFake = checkPalletFake.Checked;

            RunProcessOffloadBattery run = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery;
            if (null != run)
            {
                Task.Run(() =>
                {
                    run.OffLoadAddPatBats( pltIdx,  curRow,  curCol, isFake);
                }
                );
            }
        }

        private void btnFingerTemper_Click(object sender, EventArgs e)
        {
            RunProcessCoolingOffload run = MachineCtrl.GetInstance().GetModule(RunID.CoolingOffload) as RunProcessCoolingOffload;
            if (null != run)
            {
                Task.Run(() =>
                {
                    List<double> lstTemper = new List<double>();
                    run.CheckTemper(false, ref lstTemper);
                    {
                        if (4 == lstTemper.Count)
                        {
                            string temper = $"抓1：{lstTemper[0].ToString("0.0")} 抓2：{lstTemper[1].ToString("0.0")} 抓3：{lstTemper[2].ToString("0.0")} 抓4：{lstTemper[3].ToString("0.0")}";

                            ShowMsgBox.ShowDialog(temper, MessageType.MsgMessage);
                        }
                    }
                }
                );
            }
        }

        private void btnOffloadOutFinish_Click(object sender, EventArgs e)
        {
            RunProcessOffloadOut run = MachineCtrl.GetInstance().GetModule(RunID.OffloadOut) as RunProcessOffloadOut;
            if (null != run)
            {
                Task.Run(() =>
                {
                    int SendState = 0;
                    //if (run.SetOffloadOutFinish(ref SendState) )
                    //{
                    //    ShowMsgBox.ShowDialog("设置发送状态完成成功!", MessageType.MsgMessage);
                    //    return;
                    //}
                    
                    //if (1== SendState)
                    //{
                    //    ShowMsgBox.ShowDialog("出料位记忆没有电池需设置发送状态完成", MessageType.MsgMessage);
                    //}
                    //else if (2 == SendState)
                    //{
                    //    ShowMsgBox.ShowDialog("出料位点前不属于发送状态，不可设置发送完成状态", MessageType.MsgMessage);
                    //}
                    //else if (3 == SendState)
                    //{
                    //    ShowMsgBox.ShowDialog("出料拉带有电池，不可设置发送完成状态", MessageType.MsgMessage);
                    //}
                    //else if (4 == SendState)
                    //{
                    //    ShowMsgBox.ShowDialog("出料安全位有电池，不可设置发送完成状态", MessageType.MsgMessage);
                    //}
                }
                );
            }
        }
    }
}
