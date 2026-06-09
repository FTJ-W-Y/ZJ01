using HelperLibrary;
using Machine.Framework;
using Machine.Framework.Mes;
using Machine.MYSQL;
using Machine.SQLServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SystemControlLibrary;
using static Machine.MYSQL.MySqlBatteryBasket;
using static Machine.MYSQL.MySqlProcess;
using static Machine.SQLServer.SQLServerBakingIn;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public partial class TransferDebugPage : FormEx
    {
        string msg = "";
        public static readonly string[] CavityStatusName = new string[]
        {
            //"未知状态",         // 未知状态
            "正常状态",           // 正常状态
            "加热状态",           // 加热状态
            "等待测试-下假电池",  // 等待测试
            "等待结果",           // 等待结果
            "等待回炉",           // 等待回炉
            "维修状态",           // 维修状态
            "烘烤结束-电池下料",  // 烘烤结束
        };

        public static readonly string[] PalletStatusName = new string[]
        {
            "无效状态",
            "有效OK状态",
            "有效NG状态",
            "待检水含量",
            "等待结果-水含量",
            "等待下料",
            "假电池回炉",
            "等待二次干燥",
        };

        public static readonly string[] PalletStageName = new string[]
        {
            "无效阶段",
            "上料阶段完成",
            "烘烤阶段完成",
            "下料阶段完成",
        };

        public TransferDebugPage()
        {
            InitializeComponent();

            CreatePalletModuleList();

            CreatePalletModuleList2();

            CreatePalletEventList();

            checkPalletKeepFlat.Checked = MachineCtrl.GetInstance().IsPalletKeepFlat;
        }

        #region // 界面

        private void OtherDebugPage_Load(object sender, EventArgs e)
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
                if ((SystemControlLibrary.MCState.MCInitializing == mc) || (SystemControlLibrary.MCState.MCRunning == mc))
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                else if (level <= SystemControlLibrary.UserLevelType.USER_MAINTENANCE)
                {
                    SetUIEnable(UIEnable.MaintenanceEnabled);
                }
                else if (level == SystemControlLibrary.UserLevelType.USER_LOGOUT)
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                else
                {
                    SetUIEnable(UIEnable.OperatorEnabled);
                }
            }
            catch (System.Exception ex)
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
            this.Invoke(new Action(() =>
            {
                switch (uiEN)
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
                        this.btnOnloadCl.Enabled = true;
                        this.buttonReset.Enabled = true;
                        break;
                    case UIEnable.OperatorEnabled:
                        this.Enabled = true;

                        this.buttonPalletAdd.Enabled = false;
                        this.buttonPalletClear.Enabled = false;
                        this.buttonPalletFull.Enabled = false;
                        this.buttonPalletNG.Enabled = false;
                        this.buttonServerRestart.Enabled = false;
                        this.btnOnloadCl.Enabled = false;
                        this.buttonReset.Enabled = false;
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
            if (MachineCtrl.GetInstance().ConnectClient())
            {
                ShowMsgBox.ShowDialog("模组通讯客户端重连服务器成功", MessageType.MsgMessage);
            }
        }

        #endregion

        #region // 添加删除夹具

        private void CreatePalletModuleList()
        {
            //上料机器人
            RunProcess onloadRobot = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot);
            if (null != onloadRobot)
            {
                this.comboBoxPalletModule.Items.Add(string.Format("{0}:{1}", onloadRobot.GetRunID(), onloadRobot.RunName));
            }
            //缓存架
            RunProcess pltBuf = MachineCtrl.GetInstance().GetModule(RunID.PalletBuffer);
            if (null != pltBuf)
            {
                this.comboBoxPalletModule.Items.Add(string.Format("{0}:{1}", pltBuf.GetRunID(), pltBuf.RunName));
            }
            //人工干预台
            RunProcess manualPlatform = MachineCtrl.GetInstance().GetModule(RunID.ManualOperate);
            if (null != manualPlatform)
            {
                this.comboBoxPalletModule.Items.Add(string.Format("{0}:{1}", manualPlatform.GetRunID(), manualPlatform.RunName));
            }
            //托盘下料
            RunProcess offLoadRun = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery);
            if (null != offLoadRun)
            {
                this.comboBoxPalletModule.Items.Add(string.Format("{0}:{1}", offLoadRun.GetRunID(), offLoadRun.RunName));
            }
            //调度机器人
            RunProcess Trans = MachineCtrl.GetInstance().GetModule(RunID.Transfer);
            if (null != Trans)
            {
                this.comboBoxPalletModule.Items.Add(string.Format("{0}:{1}", Trans.GetRunID(), Trans.RunName));
                this.comboBoxPalletModuleByMes.Items.Add(string.Format("{0}:{1}", Trans.GetRunID(), Trans.RunName));
            }
            RunProcess run = null;
            for (RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            {
                run = MachineCtrl.GetInstance().GetModule(id);
                if (null != run)
                {
                    this.comboBoxPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
                    //this.comboBoxPalletModule.Items.Add(string.Format("{0}:{1}", run.GetRunID(), run.RunName));
                    this.cmbSourceModule.Items.Add(string.Format("{0}:{1}", run.GetRunID(), run.RunName));
                    this.cmbDestModule.Items.Add(string.Format("{0}:{1}", run.GetRunID(), run.RunName));
                    this.comboBoxPalletModuleByMes.Items.Add(string.Format("{0}:{1}", run.GetRunID(), run.RunName));
                }
            }
            for (int i = 0; i < 10; i++)
            {
                this.cmbSourcePlt.Items.Add((i + 1).ToString());
                this.cmbDestPlt.Items.Add((i + 1).ToString());
            }
        }

        private void CreatePalletModuleList2()
        {
            RunProcess run = null;
            for (RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            {
                run = MachineCtrl.GetInstance().GetModule(id);
                if (null != run)
                {
                    this.comboBoxPalletModule2.Items.Add($"{run.GetRunID()}:{run.RunName}");
                }
            }

            for (int idx = 0; idx < (int)CavityStatus.BakingFinish + 1; idx++)
            {
                comboBoxCavityStatus.Items.Add(CavityStatusName[idx]);
            }
            comboBoxCavityStatus.SelectedIndex = 0;

            for (int idx = 0; idx < (int)PalletStatus.Rebaking; idx++)
            {
                comboBoxPalletStatus.Items.Add(PalletStatusName[idx]);
            }
            comboBoxPalletStatus.SelectedIndex = 0;

            comboBoxPalletStage.Items.Clear();
            for (int idx = 0; idx < 4; idx++)
            {
                comboBoxPalletStage.Items.Add(PalletStageName[idx]);
            }
            comboBoxPalletStage.SelectedIndex = 0;
            comboBoxPallet2.SelectedIndex = 0;
        }

        private void CreatePalletEventList()
        {
            RunProcess run = MachineCtrl.GetInstance().GetModule(RunID.Transfer);
            if (null != run)
            {
                this.cmbEventPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            }
            for (RunID runID = RunID.DryOven0; runID < RunID.DryOvenALL; runID++)
            {
                run = MachineCtrl.GetInstance().GetModule(runID);
                if (null != run)
                {
                    this.cmbEventPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
                }
            }

            //run = MachineCtrl.GetInstance().GetModule(RunID.ManualOperate);
            //if (null != run)
            //{
            //    this.cmbEventPalletModule.Items.Add($"{run.GetRunID()}:{run.RunName}");
            //}
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
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        this.comboBoxPalletID.Items.Clear();
                        int pltLen = run.Pallet.Length;
                        for (int i = 0; i < pltLen; i++)
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
        private void comboBoxPalletModuleByMes_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModuleByMes.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        this.comboBoxPalletIDByMes.Items.Clear();
                        int pltLen = run.Pallet.Length;
                        for (int i = 0; i < pltLen; i++)
                        {
                            this.comboBoxPalletIDByMes.Items.Add((i + 1).ToString());
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
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
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
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        private void buttonPalletAdd2_Click(object sender, System.EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        for (int pltIdx = 0; pltIdx < this.comboBoxPalletID.Items.Count; pltIdx++)
                        {
                            if (pltIdx > -1)
                            {
                                if (run.Pallet[pltIdx].IsEmpty() && run.Pallet[pltIdx].State == PalletStatus.Invalid)
                                {
                                    run.ManualAddPallet(pltIdx, MachineCtrl.GetInstance().PalletMaxRow, MachineCtrl.GetInstance().PalletMaxCol, PalletStatus.OK, BatteryStatus.Invalid);
                                    Def.WriteLog("OtherDebugPage", string.Format("{0}：添加空夹具{1}", run.RunName, (pltIdx + 1)));
                                }
                            }
                            else
                            {
                                ShowMsgBox.ShowDialog("夹具索引无效，添加失败", MessageType.MsgWarning);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        private void buttonPalletFull_Click(object sender, EventArgs e)
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
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        private void buttonPalletClear_Click(object sender, System.EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    string msg = "删除夹具后必须手动将夹具从系统中插出来并拿走夹具中的电池！且无法添加电池数据！\r\n请确认是否删除夹具！";
                    if (System.Windows.Forms.DialogResult.Yes != ShowMsgBox.ShowDialog(msg, MessageType.MsgQuestion))
                    {
                        return;
                    }
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        int pltIdx = this.comboBoxPalletID.SelectedIndex;
                        if (pltIdx > -1)
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
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletClear_Click() error : " + ex.Message);
            }
        }

        private void buttonPalletNG_Click(object sender, EventArgs e)
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
            catch (System.Exception ex)
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

        //炉子夹具数据转移
        private void btnPltTransfer_Click(object sender, EventArgs e)
        {
            MCState mc = MachineCtrl.GetInstance().RunsCtrl.GetMCState();

            if (MCState.MCInvalidState == mc || MCState.MCIdle == mc || MCState.MCInitErr == mc)
            {
                ShowMsgBox.ShowDialog("请初始化完成后再操作！！！", MessageType.MsgWarning);
                return;
            }
            if (MachineCtrl.GetInstance().dbRecord.UserLevel() > UserLevelType.USER_OPERATOR)
            {
                ShowMsgBox.ShowDialog("权限不足，禁止操作！", MessageType.MsgWarning);
                return;
            }
            try
            {
                string[] txtSource = this.cmbSourceModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                string[] txtDest = this.cmbDestModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                RunProcess runSource = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txtSource[0]));
                RunProcess runDest = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txtDest[0]));
                if ((null != runSource) && (null != runDest))
                {
                    int SourcePltIdx = this.cmbSourcePlt.SelectedIndex;
                    int DestPltIdx = this.cmbDestPlt.SelectedIndex;
                    if ((SourcePltIdx < 0) && (DestPltIdx < 0))
                    {
                        ShowMsgBox.ShowDialog("夹具索引无效，转移失败", MessageType.MsgWarning);
                        return;
                    }
                    if ((runSource.Pallet[SourcePltIdx].State > PalletStatus.Invalid)
                        && (runDest.Pallet[DestPltIdx].State == PalletStatus.Invalid))
                    {
                        runDest.Pallet[DestPltIdx].Copy(runSource.Pallet[SourcePltIdx]);
                        runDest.Pallet[DestPltIdx].Stage = PalletStage.Onload;
                        runDest.Pallet[DestPltIdx].State = PalletStatus.OK;
                        runSource.Pallet[SourcePltIdx].Release();
                        runDest.SaveRunData(SaveType.Pallet);
                        runSource.SaveRunData(SaveType.Pallet);
                        ShowMsgBox.ShowDialog("转移成功！", MessageType.MsgMessage);
                    }
                    else
                    {
                        ShowMsgBox.ShowDialog("源位置没有夹具或目标位置有夹具！，不能转移", MessageType.MsgWarning);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("comboBoxPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void btnPltTransferToOven_Click(object sender, EventArgs e)
        {
            MCState mc = MachineCtrl.GetInstance().RunsCtrl.GetMCState();

            if (MCState.MCInvalidState == mc || MCState.MCIdle == mc || MCState.MCInitErr == mc)
            {
                ShowMsgBox.ShowDialog("请初始化完成后再操作！！！", MessageType.MsgWarning);
                return;
            }
            if (MachineCtrl.GetInstance().dbRecord.UserLevel() > UserLevelType.USER_OPERATOR)
            {
                ShowMsgBox.ShowDialog("权限不足，禁止操作！", MessageType.MsgWarning);
                return;
            }
            try
            {
                string[] txtDest = this.cmbDestModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                RunProcess runDest = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txtDest[0]));
                RunProcess runRobot = MachineCtrl.GetInstance().GetModule((RunID)9);
                if (null != runDest)
                {
                    int DestPltIdx = this.cmbDestPlt.SelectedIndex;
                    if (DestPltIdx < 0)
                    {
                        ShowMsgBox.ShowDialog("夹具索引无效，转移失败", MessageType.MsgWarning);
                        return;
                    }

                    if (runDest.Pallet[DestPltIdx].State == PalletStatus.Invalid && runRobot.Pallet[0].State != PalletStatus.Invalid)
                    {
                        runDest.Pallet[DestPltIdx].Copy(runRobot.Pallet[0]);
                        runRobot.Pallet[0].Release();
                        runDest.SaveRunData(SaveType.Pallet);
                        runRobot.SaveRunData(SaveType.Pallet);
                        ShowMsgBox.ShowDialog("转移成功！", MessageType.MsgMessage);
                    }
                    else
                    {
                        ShowMsgBox.ShowDialog("源位置没有夹具或目标位置有夹具，不能转移", MessageType.MsgWarning);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("comboBoxPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }



        private void comboBoxPalletModule2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule2.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0])) as RunProcessDryingOven;
                    if (null != run)
                    {
                        this.comboBoxCavity.Items.Clear();
                        for (int i = 0; i < run.CavityState.Length; i++)
                        {
                            this.comboBoxCavity.Items.Add($"{i + 1}层");
                        }
                        this.comboBoxCavity.SelectedIndex = 0;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("comboBoxPalletModule2_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void comboBoxCavity_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetCavityState();
        }

        private void GetCavityState()
        {
            try
            {
                string[] txt = this.comboBoxPalletModule2.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0])) as RunProcessDryingOven;
                    if (null != run)
                    {
                        comboBoxCavityStatus.SelectedIndex = (int)run.CavityState[comboBoxCavity.SelectedIndex];

                        int palletId = 0;
                        for (int idx = 0; idx < 2; idx++)
                        {
                            palletId = comboBoxCavity.SelectedIndex * 2 + idx;
                            comboBoxPalletStatus.SelectedIndex = (int)run.Pallet[palletId].State;
                            if ((int)run.Pallet[palletId].Stage == 4)
                            {
                                comboBoxPalletStage.SelectedIndex = (int)run.Pallet[palletId].Stage - 1;
                            }
                            else
                            {
                                comboBoxPalletStage.SelectedIndex = (int)run.Pallet[palletId].Stage;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("GetCavityState() error : " + ex.Message);
            }
        }

        private void btnCavitySet_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule2.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0])) as RunProcessDryingOven;
                    if (null != run)
                    {
                        int CavityId = comboBoxCavity.SelectedIndex;
                        //if (4 == CavityId) //回炉状态=设置等待结果+提交水含量
                        //{
                        //    return;
                        //}
                        //设置腔体状态
                        CavityStatus cavityStatus = (CavityStatus)comboBoxCavityStatus.SelectedIndex;
                        if (run.ManualSetCavityState(CavityId, cavityStatus))
                        {
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("comboBoxPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void buttonPalletBat_Click(object sender, EventArgs e)
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

        private void btnPalletSet_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule2.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0])) as RunProcessDryingOven;
                    if (null != run)
                    {
                        int CavityId = comboBoxCavity.SelectedIndex;
                        int pltCol = CavityId * (int)OvenRowCol.MaxCol + comboBoxPallet2.SelectedIndex;
                        PalletStatus state = new PalletStatus();
                        if (run.ManualSetPalletState(pltCol, PalletStatus.NG))
                        {
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("comboBoxPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }


        private void btnInBaking_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModule2.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0])) as RunProcessDryingOven;
                    if (null != run)
                    {
                        int CavityId = comboBoxCavity.SelectedIndex;
                        int PalletId = comboBoxPallet2.SelectedIndex;

                        int plt = CavityId * 2 + PalletId;

                        string msg = "";
                        Pallet pallet = run.Pallet[plt];
                        if (pallet.IsEmpty())
                        {
                            return;
                        }
                        if (MesOperate.EquToMesBindContainer(MesResources.Equipment, pallet, ref msg))
                        {
                            if (!MesOperate.EquToMesInBaking(MesResources.Equipment, pallet, ref msg))
                            {
                                MessageBox.Show($"托盘{pallet.Code}电池进站失败:{msg}");
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show($"绑定托盘{pallet.Code}失败：{msg}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("comboBoxPalletModule_SelectedIndexChanged() error : " + ex.Message);
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

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
                        case RunID.DryOven0:
                        case RunID.DryOven0 + 1:
                        case RunID.DryOven0 + 2:
                        case RunID.DryOven0 + 3:
                        case RunID.DryOven0 + 4:
                        case RunID.DryOven0 + 5:
                        case RunID.DryOven0 + 6:
                        case RunID.DryOven0 + 7:
                        case RunID.DryOven0 + 8:
                        case RunID.DryOven0 + 9:
                        case RunID.DryOven0 + 10:
                        case RunID.DryOven0 + 11:
                            {
                                RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule(runID) as RunProcessDryingOven;
                                if (null != run)
                                {
                                    cmbEventID.Items.Clear();

                                    //DryOvenPlaceEmptyPallet,                // 干燥炉放空夹具
                                    //DryOvenPlaceNGPallet,                   // 干燥炉放NG非空夹具
                                    //DryOvenPlaceNGEmptyPallet,              // 干燥炉放NG空夹具
                                    //DryOvenPlaceOnlOKFullPallet,            // 干燥炉放上料完成OK满夹具
                                    //DryOvenPlaceOnlOKFakeFullPallet,        // 干燥炉放上料完成OK带假电池满夹具
                                    //DryOvenPlaceRebakeFakePallet,           // 干燥炉放回炉假电池夹具（已放回假电池的夹具）
                                    //DryOvenPlaceWaitResultPallet,           // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
                                    //DryOvenPickEmptyPallet,                 // 干燥炉取空夹具
                                    //DryOvenPickNGPallet,                    // 干燥炉取NG非空夹具
                                    //DryOvenPickNGEmptyPallet,               // 干燥炉取NG空夹具
                                    //DryOvenPickDetectFakePallet,            // 干燥炉取待检测含假电池夹具（未取走假电池的夹具）
                                    //DryOvenPickReputFakePallet,             // 干燥炉取待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
                                    //DryOvenPickDryFinishPallet,             // 干燥炉取干燥完成夹具（等待下料）
                                    //DryOvenPickTransferPallet,              // 干燥炉转移取夹具：取来源炉腔
                                    //DryOvenPlaceTransferPallet,             // 干燥炉转移放夹具：放至目的炉腔
                                    //DryOvenPickPlaceEnd,					  // 干燥炉信号结束

                                    cmbEventID.Items.Add("干燥炉放空夹具");
                                    cmbEventID.Items.Add("干燥炉放NG非空夹具");
                                    cmbEventID.Items.Add("干燥炉放NG空夹具");
                                    cmbEventID.Items.Add("干燥炉放上料完成OK满夹具");
                                    cmbEventID.Items.Add("干燥炉放上料完成OK带假电池满夹具");
                                    cmbEventID.Items.Add("干燥炉放回炉假电池夹具");//（已放回假电池的夹具）
                                    cmbEventID.Items.Add("干燥炉放等待水含量结果夹具");//（已取待测假电池的夹具）
                                    cmbEventID.Items.Add("干燥炉取空夹具");
                                    cmbEventID.Items.Add("干燥炉取NG非空夹具");
                                    cmbEventID.Items.Add("干燥炉取NG空夹具");
                                    cmbEventID.Items.Add("干燥炉取待检测含假电池夹具");//（未取走假电池的夹具）
                                    cmbEventID.Items.Add("干燥炉取待回炉含假电池夹具");//（已取走假电池，待重新放回假电池的夹具）
                                    cmbEventID.Items.Add("干燥炉取干燥完成夹具");//（等待下料）
                                    cmbEventID.Items.Add("干燥炉转移取夹具");//：取来源炉腔
                                    cmbEventID.Items.Add("干燥炉转移放夹具");//：放至目的炉腔
                                    cmbEventID.Items.Add("干燥炉信号结束");
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
                        case RunID.DryOven0:
                        case RunID.DryOven0 + 1:
                        case RunID.DryOven0 + 2:
                        case RunID.DryOven0 + 3:
                        case RunID.DryOven0 + 4:
                        case RunID.DryOven0 + 5:
                        case RunID.DryOven0 + 6:
                        case RunID.DryOven0 + 7:
                        case RunID.DryOven0 + 8:
                        case RunID.DryOven0 + 9:
                        case RunID.DryOven0 + 10:
                        case RunID.DryOven0 + 11:
                            {
                                RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule(runID) as RunProcessDryingOven;
                                if (null != run)
                                {
                                    EventList modEvent = (EventList)(cmbEventID.SelectedIndex + (int)EventList.DryOvenPlaceEmptyPallet);
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
                        case RunID.DryOven0:
                        case RunID.DryOven0 + 1:
                        case RunID.DryOven0 + 2:
                        case RunID.DryOven0 + 3:
                        case RunID.DryOven0 + 4:
                        case RunID.DryOven0 + 5:
                        case RunID.DryOven0 + 6:
                        case RunID.DryOven0 + 7:
                        case RunID.DryOven0 + 8:
                        case RunID.DryOven0 + 9:
                        case RunID.DryOven0 + 10:
                        case RunID.DryOven0 + 11:
                            {
                                RunProcessDryingOven run = MachineCtrl.GetInstance().GetModule(runID) as RunProcessDryingOven;
                                if (null != run)
                                {
                                    EventList modEvent = (EventList)(cmbEventID.SelectedIndex + (int)EventList.DryOvenPlaceEmptyPallet);
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

        private void checkPalletKeepFlat_CheckedChanged(object sender, EventArgs e)
        {
            MachineCtrl.GetInstance().IsPalletKeepFlat = checkPalletKeepFlat.Checked;
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.comboBoxPalletModule.SelectedItem == null)
                {
                    return;
                }
                string[] txt = this.comboBoxPalletModule.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                if (txt.Length > 0)
                {
                    if (DialogResult.Yes == ShowMsgBox.ShowDialog(string.Format("是否重置{0}模组信号", txt[1]), MessageType.MsgQuestion))
                    {
                        RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                        if (null != run)
                        {
                            run.ManualResetEvent();
                            if ((RunID)Convert.ToInt32(txt[0]) == RunID.Transfer)
                            {
                                ShowMsgBox.ShowDialog("调度机器人信号重置完成，请清理调度机器人上的托盘，确保调度机器人上无托盘。", MessageType.MsgMessage);
                            }
                            if ((RunID)Convert.ToInt32(txt[0]) == RunID.OffloadBattery)
                            {
                                ShowMsgBox.ShowDialog("托盘下料信号重置完成，请清理托盘下料夹爪中的电池，确保托盘下料夹爪无电池。", MessageType.MsgMessage);
                            }
                            if ((RunID)Convert.ToInt32(txt[0]) == RunID.OnloadRobot)
                            {
                                ShowMsgBox.ShowDialog("上料机器人信号重置完成，请清理上料机器人夹爪中的电池，确保上料机器人夹爪无电池。", MessageType.MsgMessage);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonReset_Click() error : " + ex.Message);
            }
        }
        //满盘进站
        private void btnIn_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModuleByMes.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        int pltIdx = this.comboBoxPalletIDByMes.SelectedIndex;
                        if (pltIdx > -1)
                        {
                            string dryingOvenCode = "WH02C0122PR-HKX002" + (11 + Convert.ToInt32(run.RunName.Replace("干燥炉", ""))); //设备编号
                            if (!MachineCtrl.GetInstance().ACLOGONCHECK_Main(MesResources.Equipment, run.Pallet[pltIdx], dryingOvenCode, run.RunName, ref msg))
                            {
                                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                            }
                            else
                            {
                                ShowMsgBox.ShowDialog($"入站成功！", MessageType.MsgAlarm);
                            }
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
        //托盘出站
        private void btnOut_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModuleByMes.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        int pltIdx = this.comboBoxPalletIDByMes.SelectedIndex;
                        if (pltIdx > -1)
                        {
                            run.OutMESEvent(run.Pallet[pltIdx], (pltIdx / 2));
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("buttonPalletAdd_Click() error : " + ex.Message);
            }
        }

        //托盘绑定
        private void btnBind_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModuleByMes.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        int pltIdx = this.comboBoxPalletIDByMes.SelectedIndex;
                        if (pltIdx > -1)
                        {
                            string dryingOvenCode = "WH02C0122PR-HKX002" + (11 + Convert.ToInt32(run.RunName.Replace("干燥炉", ""))); //设备编号
                            //MES 托盘电池绑定
                            if (!MachineCtrl.GetInstance().ACINBOUND_Main(MesResources.Equipment, run.Pallet[pltIdx], dryingOvenCode, run.RunName, true, true, ref msg))
                            {
                                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                            }
                            else
                            {
                                ShowMsgBox.ShowDialog($"绑盘成功！", MessageType.MsgAlarm);
                            }
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
        //托盘解绑
        private void btnUnBind_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModuleByMes.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        int pltIdx = this.comboBoxPalletIDByMes.SelectedIndex;
                        if (pltIdx > -1)
                        {
                            string dryingOvenCode = "WH02C0122PR-HKX002" + (11 + Convert.ToInt32(run.RunName.Replace("干燥炉", ""))); //设备编号
                            //MES 托盘电池绑定
                            if (!MachineCtrl.GetInstance().ACINBOUND_Main(MesResources.Equipment, run.Pallet[pltIdx], dryingOvenCode, run.RunName, true, false, ref msg))
                            {
                                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                            }
                            else
                            {
                                ShowMsgBox.ShowDialog($"解盘成功！", MessageType.MsgAlarm);
                            }
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

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string[] txt = this.comboBoxPalletModuleByMes.SelectedItem.ToString().Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (txt.Length > 0)
                {
                    RunProcess run = MachineCtrl.GetInstance().GetModule((RunID)Convert.ToInt32(txt[0]));
                    if (null != run)
                    {
                        int pltIdx = this.comboBoxPalletIDByMes.SelectedIndex;
                        if (pltIdx > -1)
                        {
                            string dryingOvenCode = "WH02C0122PR-HKX002" + (11 + Convert.ToInt32(run.RunName.Replace("干燥炉", ""))); //设备编号
                            if (!MachineCtrl.GetInstance().CANCELINBOUND_Main(MesResources.Equipment, run.Pallet[pltIdx], dryingOvenCode, ref msg))
                            {
                                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                            }
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

        private void btnOnloadCl_Click(object sender, EventArgs e)
        {
            try
            {
                if (DialogResult.Yes == ShowMsgBox.ShowDialog(string.Format("是否将上料请求信号，上料完成信号清零，调度置不在干涉区域信号！"), MessageType.MsgQuestion))
                {
                    RunProcessOnloadRobot run = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                    if (null != run)
                    {
                        run.ClearEvent();
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("btnOnloadCl_Click() error : " + ex.Message);
            }
        }

        //复投电池
        private void btnReBattery_Click(object sender, EventArgs e)
        {
            try
            {
                
                if (!string.IsNullOrEmpty(this.textBoxReBattery.Text))
                {
                    if (DialogResult.Yes == ShowMsgBox.ShowDialog(string.Format("请确认是否将电池：{0}\r\n投入到复投位！！！", this.textBoxReBattery.Text), MessageType.MsgQuestion))
                    {
                        if (!Def.IsNoHardware() && !Jeve_Mes.Mes_GetVehicleInfo(this.textBoxReBattery.Text, "DAL1HK01", "1", "1", ref msg))
                        {
                            
                            string file = string.Format(@"{0}\{1}\{2}.csv", MachineCtrl.GetInstance().ProductionFilePath, "电池校验NG", DateTime.Now.ToString("yyyy-MM-dd"));
                            string msgData = $"人工复投位扫码校验MES-NG：{this.textBoxReBattery.Text}";
                            MachineCtrl.GetInstance().dbRecord.AddAlarmInfo(new AlarmFormula(Def.GetProductFormula(), (int)RunID.OnloadScan, msgData, (int)MessageType.MsgMessage, (int)RunID.OnloadScan, MachineCtrl.GetInstance().MachineName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                            ShowMsgBox.ShowDialog(string.Format("{0},{1},该电芯不可复投！！！", msgData, msg), MessageType.MsgAlarm);
                            this.textBoxReBattery.Text = null;
                            return;
                        }
                        RunProcessOnloadRobot runOnload = MachineCtrl.GetInstance().GetModule(RunID.OnloadRobot) as RunProcessOnloadRobot;
                        if (null != runOnload)
                        {
                            runOnload.ReBattery(this.textBoxReBattery.Text);
                            this.textBoxReBattery.Text = null;
                        }
                    }
                }
                else
                {
                    ShowMsgBox.ShowDialog(string.Format("复投电池条码不能为空！！！"), MessageType.MsgMessage);
                }

            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("btnOnloadCl_Click() error : " + ex.Message);
            }
        }

        //电芯数据上传数据库
        private void btnUploadSQL_Click(object sender, EventArgs e)
        {
            EquBakingIn equBakingIn = new EquBakingIn();
            equBakingIn.LotNo = this.textBoxLotNo.Text;
            equBakingIn.Status = this.textBoxStatus.Text;
            equBakingIn.OpOrder = this.textBoxOpOrder.Text;
            equBakingIn.VehicleNo = this.textBoxVehicleNo.Text;
            equBakingIn.BakingInTime = DateTime.Now;

            try
            {
                if (!string.IsNullOrEmpty(this.textBoxLotNo.Text))
                {
                    if (SQLServerBakingIn.InsertRecord(equBakingIn) > -1)
                    {
                        MessageBox.Show("添加成功");
                        this.textBoxLotNo.Text = null;
                    }
                    else
                    {
                        MessageBox.Show("添加失败");
                    }
                }
                else
                {
                    MessageBox.Show("添加失败,电池条码不能为空");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("添加失败,{0}", ex.ToString()));
            }

        }

        private void btnSelectSQL_Click(object sender, EventArgs e)
        {
            ListBasketData listbaskdata = new ListBasketData();
            MySqlBatteryBasket.BasketData basketData = new MySqlBatteryBasket.BasketData();
            basketData.LotNo = this.textBoxLotNo.Text;
            try
            {
                if (!string.IsNullOrEmpty(this.textBoxLotNo.Text))
                {
                    if (MySqlBatteryBasket.LnQuire(basketData, ref listbaskdata))
                    {
                        MessageBox.Show("查询成功");
                        this.textBoxLotNo.Text = null;
                    }
                    else
                    {
                        MessageBox.Show("查询失败");
                    }
                }
                else
                {
                    MessageBox.Show("查询失败,物料框条码不能为空");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询失败{ex}");
            }
            BakingInData bakingInData = new BakingInData();
            SQLServerBakingIn.EquBakingIn equBakingIn = new SQLServerBakingIn.EquBakingIn();
            equBakingIn.VehicleNo = this.textBoxVehicleNo.Text;

            //try
            //{
            //    if (!string.IsNullOrEmpty(this.textBoxVehicleNo.Text))
            //    {
            //        if (SQLServerBakingIn.SelectRecord(equBakingIn, ref bakingInData))
            //        {
            //            MessageBox.Show("查询成功");
            //            //this.textBoxLotNo.Text = null;
            //        }
            //        else
            //        {
            //            MessageBox.Show("查询失败");
            //        }
            //    }
            //    else
            //    {
            //        MessageBox.Show("查询失败,物料框条码不能为空");
            //    }

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"查询失败{ex}");
            //}

            //电池生产信息添加数据库
            //MySqlProcess
            var paramData = new ProcessData();
            var data = new List<ProcessData>();
            //paramData.OnloadTime
            //paramData.OffloadTime
            //paramData.StartTime = item.StartDate;
            //paramData.EndTime = item.EndDate;
            paramData.PreheatTime = "10";
            paramData.VacHeatTime = "10";
            paramData.CoolingTime = "20";
            paramData.SetTempValue = "10";
            paramData.TempUpperlimit = "10";
            paramData.TempLowerlimit = "10";
            paramData.PreVacTime = "10";
            paramData.BlowTime = "10";
            for (int col = 0; col < 10; col++)
            {
                for (int row = 0; row < 10; row++)
                {
                    paramData.LotNo = "10";
                    //data.Add(paramData);

                    MySqlProcess.InsertRecord(paramData);
                }
            }
        }

        //单电芯报工
        private void buttonUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(this.textBoxUploadBattery.Text))
                {
                    string errMsg = "";
                    string lotNo = this.textBoxUploadBattery.Text;
                    if (!Jeve_Mes.Mes_ReportSN("DAL1HK01",lotNo,BatteryStatus.OK,ref errMsg))
                    {
                        ShowMsgBox.ShowDialog($"单电芯报工失败：{msg}", MessageType.MsgAlarm);
                    }
                    else
                    {
                        MessageBox.Show("单电芯报工成功");
                        this.textBoxLotNo.Text = null;
                    }
                }
                else
                {
                    ShowMsgBox.ShowDialog(string.Format("MES上报电池条码不能为空！！！"), MessageType.MsgMessage);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }


    }
}
