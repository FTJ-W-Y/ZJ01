using Apriso.MIPlugins.Communication.Clients;
using Apriso.MIPlugins.Communication.Clients.WcfServiceAPI;
using HelperLibrary;
using Machine.Framework;
using Machine.Framework.Mes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public partial class UpDataWaterPage : FormEx
    {
        // 定时器
        System.Timers.Timer timerUpdata;
        List<string> OutBankData;
        string filePath = string.Format(@"{0}\{1}\", MachineCtrl.GetInstance().ProductionFilePath, "MESNGLog");
        string fileBindPath = string.Format(@"{0}\{1}\{2}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, "MESNGLog", "MES托盘电芯绑定离线");
        string fileUnBindPath = string.Format(@"{0}\{1}\{2}.csv"
            , MachineCtrl.GetInstance().ProductionFilePath, "MESNGLog", "MES托盘电芯解绑离线");
        string fileInPath = string.Format(@"{0}\{1}\{2}.csv"
            , MachineCtrl.GetInstance().ProductionFilePath, "MESNGLog", "MES入站离线");
        string fileOutPath = string.Format(@"{0}\{1}\{2}.csv"
            , MachineCtrl.GetInstance().ProductionFilePath, "MESNGLog", "MES出站离线");
        string copy_filePath;
        DataBaseRecord dbRecord;

        private List<UnLineData> unLineDataBindList;
        private List<UnLineData> unLineDataUnBindList;
        private List<UnLineData> unLineDataInList;
        private List<UnLineData> unLineDataOutList;

        

        private DataGridView dgvBuf;
        public UpDataWaterPage()
        {
            InitializeComponent();
            unLineDataBindList = new List<UnLineData>();
            unLineDataUnBindList = new List<UnLineData>();
            unLineDataInList = new List<UnLineData>();
            unLineDataOutList = new List<UnLineData>();
            dgvBuf = new DataGridView();
            //创建干燥炉列表
            CreateDryingOvenList();

            if (1 == MachineCtrl.GetInstance().MachineID || 3 == MachineCtrl.GetInstance().MachineID)
            {
                buttonUpData.Enabled = true;
                //if (Def.IsNoHardware())
                //buttonUpDataAll.Show();
            }
            else
            {
                buttonUpData.Enabled = false;
            }
        }
        private void AddList(string vaule, string text, string pltCode = "")
        {
            try
            {
                //dgvIn.DataSource = null;
                //dgvOut.DataSource = null;
                //dgvBind.DataSource = null;
                //dgvUnBind.DataSource = null;
                ReadCsv(fileBindPath, out unLineDataBindList);
                ReadCsv(fileUnBindPath, out unLineDataUnBindList);
                ReadCsv(fileInPath, out unLineDataInList);
                ReadCsv(fileOutPath, out unLineDataOutList);
                CreateList(dgvBind, unLineDataBindList);
                CreateList(dgvUnBind, unLineDataUnBindList);
                CreateList(dgvIn, unLineDataInList);
                CreateList(dgvOut, unLineDataOutList);
                dgvIn.Refresh();
                dgvOut.Refresh();
                dgvBind.Refresh();
                dgvUnBind.Refresh();
                //this.BeginInvoke(new Action(() =>
                //{
                //    UnLineData unLineData = new UnLineData();
                //    string[] strArray = text.Split(',');
                //    unLineData.Time = strArray[0];
                //    unLineData.PltCode = strArray[1];
                //    unLineData.InterfaceName = strArray[2];
                //    unLineData.ElapsedTime = strArray[3];
                //    unLineData.Result = strArray[4];
                //    unLineData.ResultMsg = strArray[5];
                //    unLineData.UpdataInfo = strArray[9];
                //    switch (vaule)
                //    {
                //        case "Bind":
                //            for (int i = 0; i < unLineDataBindList.Count; i++)
                //            {
                //                if (unLineDataBindList[i].PltCode == pltCode)
                //                {
                //                    unLineDataBindList.RemoveAt(i);
                //                }
                //            }
                //            unLineDataBindList.Add(unLineData);
                //            dgvBind.DataSource = null;
                //            dgvBind.DataSource = unLineDataBindList;
                //            dgvBind.Refresh();
                //            break;
                //        case "UnBind":
                //            for (int i = 0; i < unLineDataUnBindList.Count; i++)
                //            {
                //                if (unLineDataUnBindList[i].PltCode == pltCode)
                //                {
                //                    unLineDataUnBindList.RemoveAt(i);
                //                }
                //            }
                //            unLineDataUnBindList.Add(unLineData);
                //            dgvUnBind.DataSource = null;
                //            dgvUnBind.DataSource = unLineDataUnBindList;
                //            dgvUnBind.Refresh();
                //            break;
                //        case "In":
                //            for (int i = 0; i < unLineDataInList.Count; i++)
                //            {
                //                if (unLineDataInList[i].PltCode == pltCode)
                //                {
                //                    unLineDataInList.RemoveAt(i);
                //                }
                //            }
                //            unLineDataInList.Add(unLineData);
                //            dgvIn.DataSource = null;
                //            dgvIn.DataSource = unLineDataInList;
                //            dgvIn.Refresh();
                //            break;
                //        case "Out":
                //            for (int i = 0; i < unLineDataOutList.Count; i++)
                //            {
                //                if (unLineDataInList[i].PltCode == pltCode)
                //                {
                //                    unLineDataOutList.RemoveAt(i);
                //                }
                //            }
                //            unLineDataOutList.Add(unLineData);
                //            dgvOut.DataSource = null;
                //            dgvOut.DataSource = unLineDataOutList;
                //            dgvOut.Refresh();
                //            break;
                //        default:
                //            break;
                //    }
                //}));
            }
            catch (Exception)
            {

            }

        }
        private void UpDataWaterPage_Load(object sender, EventArgs e)
        {
            MachineCtrl.GetInstance().unLineDataListHander += AddList;
            ToolStripItem item = new ToolStripMenuItem();
            item.Name = "Updata";
            item.Text = "离线上传";
            item.Click += new EventHandler(contextMenuStripOP_Click);
            contextMenuStripOP.Items.Add(item);
            item = new ToolStripMenuItem();
            item.Name = "Delet";
            item.Text = "删除选中行";
            item.Click += new EventHandler(contextMenuStripOP_Click);
            contextMenuStripOP.Items.Add(item);
            item = new ToolStripMenuItem();
            item.Name = "UpdataAll";
            item.Text = "全部上传";
            item.Click += new EventHandler(contextMenuStripOP_Click);
            contextMenuStripOP.Items.Add(item);
            item = new ToolStripMenuItem();
            item.Name = "DeletAll";
            item.Text = "全部删除";
            item.Click += new EventHandler(contextMenuStripOP_Click);
            contextMenuStripOP.Items.Add(item);

            ReadCsv(fileBindPath, out unLineDataBindList);
            ReadCsv(fileUnBindPath, out unLineDataUnBindList);
            ReadCsv(fileInPath, out unLineDataInList);
            ReadCsv(fileOutPath, out unLineDataOutList);
            CreateList(dgvBind, unLineDataBindList);
            CreateList(dgvUnBind, unLineDataUnBindList);
            CreateList(dgvIn, unLineDataInList);
            CreateList(dgvOut, unLineDataOutList);
            dgvIn.Refresh();
            dgvOut.Refresh();
            dgvBind.Refresh();
            dgvUnBind.Refresh();
        }
        private void CreateList(DataGridView dgv, List<UnLineData> uData)
        {
            if (null != uData)
            {
                dgv.Rows.Clear();
                dgv.ClearSelection();
                for (int i = 0; i < uData.Count; i++)
                {
                    int index = dgv.Rows.Add();

                    dgv.Rows[index].Cells[0].Value = uData[i].Time;
                    dgv.Rows[index].Cells[1].Value = uData[i].PltCode;
                    dgv.Rows[index].Cells[2].Value = uData[i].InterfaceName;
                    dgv.Rows[index].Cells[3].Value = uData[i].ElapsedTime;
                    dgv.Rows[index].Cells[4].Value = uData[i].Result;
                    dgv.Rows[index].Cells[5].Value = uData[i].UpdataInfo;
                    dgv.Rows[index].Cells[6].Value = uData[i].ResultMsg;
                }
                dgv.Refresh();
            }
        }
        /// <summary>
        /// 界面隐藏时停止更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpDataWaterPage_VisibleChanged(object sender, EventArgs e)
        {
            if (null != this.timerUpdata)
            {
                if (this.Visible)
                {
                    this.timerUpdata.Start();
                }
                else
                {
                    this.timerUpdata.Stop();
                }
            }
        }

        #region // 界面

        private void CreateDryingOvenList()
        {
            for (RunID id = RunID.DryOven0; id < RunID.DryOvenALL; id++)
            {
                string name = "干燥炉  " + ((int)id - (int)RunID.DryOven0 + 1);
                this.comboBoxDryingID.Items.Add(name);
            }
            if (this.comboBoxDryingID.Items.Count > 0)
            {
                this.comboBoxDryingID.SelectedIndex = 0;
            }

            int tier = (int)OvenRowCol.MaxRow;
            for (int tierID = 0; tierID < tier; tierID++)
            {
                string name = (tierID + 1).ToString() + " 层 ";
                this.comboBoxTierID.Items.Add(name);
            }
            if (this.comboBoxTierID.Items.Count > 0)
            {
                this.comboBoxTierID.SelectedIndex = 0;
            }
            ToolTip tip = new ToolTip();
            tip.SetToolTip(this.textBoxWaterValue1, "只允许数字/小数点");
            tip.SetToolTip(this.textBoxWaterValue2, "只允许数字/小数点");
            tip.SetToolTip(this.textBoxUnbindPlt, "需要解绑的夹具条码\r\n分号;后可以添加解绑类型");
        }

        /// <summary>
        /// UI界面可见性发生改变
        /// </summary>
        /// <param name="show">是否在前台显示</param>
        public override void UIVisibleChanged(bool show)
        {
            if (show)
            {
                this.textBoxOperaterID.Text = MachineCtrl.GetInstance().OperaterID;
                this.textBoxOperaterID.ReadOnly = !string.IsNullOrEmpty(this.textBoxOperaterID.Text);

                UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState(), MachineCtrl.GetInstance().dbRecord.UserLevel());
            }
            else
            {
                UpdataUIEnable(MCState.MCRunning, UserLevelType.USER_LOGOUT);
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
                if ((level > UserLevelType.USER_OPERATOR))
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                else if ((MCState.MCInitializing == mc) || (MCState.MCRunning == mc))
                {
                    SetUIEnable(UIEnable.OperatorEnabled);
                }
                //else if (level <= UserLevelType.USER_MAINTENANCE)
                //{
                //    SetUIEnable(UIEnable.AllEnabled);
                //}
                //else if (level == UserLevelType.USER_LOGOUT)
                //{
                //    SetUIEnable(UIEnable.AllDisabled);
                //}
                else
                {
                    SetUIEnable(UIEnable.AllEnabled);
                }
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("UpDataWaterPage .UpdataUIEnable()", ex.Message, LogType.Error);
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
                this.Invoke(new Action(() =>
                {
                    switch (uiEN)
                    {
                        case UIEnable.AllDisabled:
                            this.groupBox2.Enabled = false;
                            this.groupBox1.Enabled = false;
                            break;
                        case UIEnable.AllEnabled:
                        case UIEnable.AdminEnabled:
                        case UIEnable.MaintenanceEnabled:
                            this.groupBox2.Enabled = true;
                            this.groupBox1.Enabled = true;
                            break;
                        case UIEnable.OperatorEnabled:
                            this.groupBox2.Enabled = false;
                            this.groupBox1.Enabled = true;
                            break;
                        default:
                            break;
                    }
                    buttonUpDataAll.Visible = false;
                    //textBoxWaterValue1.Enabled = true;
                    //textBoxWaterValue2.Enabled = true;
                    //buttonUpData.Enabled = true;
                }));

            }));
        }

        #endregion

        #region // 水含量

        /// <summary>
        /// 假电池搜索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.textBoxBatCode.Text))
            {
                ShowMsgBox.ShowDialog("电池条码不能为空", MessageType.MsgWarning);
                return;
            }
            else
            {
                if (!SearchFakeBatPos())
                {
                    ShowMsgBox.ShowDialog("未搜索到电池条码位置", MessageType.MsgWarning);
                }
            }
        }

        /// <summary>
        /// 水含量上传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonUpData_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.textBoxWaterValue1.Text)
                || string.IsNullOrWhiteSpace(this.textBoxWaterValue2.Text)
                || string.IsNullOrWhiteSpace(this.textBoxWaterValue3.Text)
                //|| string.IsNullOrWhiteSpace(this.textBoxWaterValue4.Text)
                )
            {
                ShowMsgBox.ShowDialog("请输入3组水含量值！！！", MessageType.MsgWarning);
                return;
            }

            TextBox[] txtBox = new TextBox[] { this.textBoxWaterValue1, this.textBoxWaterValue2, this.textBoxWaterValue3/*, this.textBoxWaterValue4*/ };
            double[] waterValue = new double[txtBox.Length];
            for (int i = 0; i < txtBox.Length; i++)
            {
                if (!double.TryParse(txtBox[i].Text, out waterValue[i]))
                {
                    ShowMsgBox.ShowDialog(string.Format("【{0}】不是正确的数字，请重新输入！", txtBox[i].Text), MessageType.MsgWarning);
                    return;
                }
            }

            for (int i = 0; i < waterValue.Length; i++)
            {
                if (waterValue[i] <= 0)
                {
                    ShowMsgBox.ShowDialog("正极/负极/隔膜/混合水含量不能小于或等于0，请重新输入！", MessageType.MsgWarning);
                    //ShowMsgBox.ShowDialog("正极/负极不能小于或等于0，请重新输入！", MessageType.MsgWarning);
                    return;
                }
            }

            int dryingID = this.comboBoxDryingID.SelectedIndex;
            int dryID = this.comboBoxDryingID.SelectedIndex;
            int cavityIdx = this.comboBoxTierID.SelectedIndex;
            if (dryingID > -1 && cavityIdx > -1)
            {
                string msg = "";
                RunID runId = RunID.DryOven0 + dryingID;
                CavityStatus cavityState = MachineCtrl.GetInstance().GetDryingOvenCavityState(runId, cavityIdx);
                if (CavityStatus.WaitResult == cavityState)
                {
                    if (null == MachineCtrl.GetInstance().GetModule(runId) && !MachineCtrl.GetInstance().ClientIsConnect())
                    {
                        msg = $"干燥炉{dryID + 1}】模组不在此设备，且模组未连接不能在此设备上传水含量";
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        return;
                    }
                    if (MachineCtrl.GetInstance().SetCavityWaterContent(runId, cavityIdx, waterValue))
                    {
                        if (!Def.IsNoHardware())
                        {
                            msg = $"干燥炉{dryID + 1} - {cavityIdx + 1}层腔体水含量结果上传成功";
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgMessage);
                        }
                        //foreach(var item in txtBox)
                        for (int idx = 0; idx < txtBox.Length - 1; idx++)
                        {
                            txtBox[0].Text = "1";
                        }

                        if (!Def.IsNoHardware())
                        {
                            txtBox[txtBox.Length - 1].Text = "";
                        }
                    }
                }
                else
                {
                    msg = $"干燥炉{dryID + 1} - {cavityIdx + 1}层腔体非等待水含量结果状态，不能上传";
                    ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                }
            }
        }

        /// <summary>
        /// 搜索假电池位置
        /// </summary>
        /// <returns></returns>
        private bool SearchFakeBatPos()
        {
            for (int modID = 0; modID < (int)OvenInfoCount.OvenCount; modID++)
            {
                Pallet[] ovenPlt = MachineCtrl.GetInstance().GetModulePallet(RunID.DryOven0 + modID);
                if (null != ovenPlt)
                {
                    for (int Pat = 0; Pat < ovenPlt.Length; Pat++)
                    {
                        int row, col;
                        row = col = -1;
                        if (ovenPlt[Pat].GetFakePos(ref row, ref col))
                        {
                            if (this.textBoxBatCode.Text == ovenPlt[Pat].Battery[row, col].Code)
                            {
                                string info = $"干燥炉{modID + 1}-{Pat / 2 + 1}层夹具{Pat % 2 + 1}-{row + 1}行{col + 1}列";
                                this.textBoxBatInfo.Text = info;

                                if (this.comboBoxDryingID.Items.Count >= modID)
                                {
                                    this.comboBoxDryingID.SelectedIndex = modID;
                                }
                                if (this.comboBoxTierID.Items.Count >= Pat / 2)
                                {
                                    this.comboBoxTierID.SelectedIndex = Pat / 2;
                                }
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 字符校验
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaterValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 数字 && 删除键 && 小数点
            if (!(Char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8 && e.KeyChar != (char)46)
            {
                e.Handled = true;
            }
        }

        #endregion

        #region // 操作员工

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOperaterLogin_Click(object sender, EventArgs e)
        {
            if (MachineCtrl.GetInstance().MesUserLogin())
            {
                this.textBoxOperaterID.Text = MachineCtrl.GetInstance().OperaterID;
                this.textBoxOperaterID.ReadOnly = true;
            }
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOperaterLogout_Click(object sender, EventArgs e)
        {
            MachineCtrl.GetInstance().OperaterID = "";
            this.textBoxOperaterID.Clear();
            this.textBoxOperaterID.ReadOnly = false;
        }

        #endregion

        private void buttonUpDataAll_Click(object sender, EventArgs e)
        {
            if (!Def.IsNoHardware())
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(this.textBoxWaterValue1.Text)
                || string.IsNullOrWhiteSpace(this.textBoxWaterValue2.Text)
                //|| string.IsNullOrWhiteSpace(this.textBoxWaterValue3.Text)
                //|| string.IsNullOrWhiteSpace(this.textBoxWaterValue4.Text)
                )
            {
                ShowMsgBox.ShowDialog("请输入4组水含量值！！！", MessageType.MsgWarning);
                return;
            }

            TextBox[] txtBox = new TextBox[] { this.textBoxWaterValue1, this.textBoxWaterValue2, /*this.textBoxWaterValue3, this.textBoxWaterValue4*/ };
            double[] waterValue = new double[txtBox.Length];
            for (int i = 0; i < txtBox.Length; i++)
            {
                if (!double.TryParse(txtBox[i].Text, out waterValue[i]))
                {
                    ShowMsgBox.ShowDialog(string.Format("【{0}】不是正确的数字，请重新输入！", txtBox[i].Text), MessageType.MsgWarning);
                    return;
                }
            }

            for (int i = 0; i < waterValue.Length; i++)
            {
                if (waterValue[i] <= 0)
                {
                    ShowMsgBox.ShowDialog("正极/负极/隔膜/混合水含量不能小于或等于0，请重新输入！", MessageType.MsgWarning);
                    return;
                }
            }

            int dryingID = this.comboBoxDryingID.SelectedIndex;
            int dryID = this.comboBoxDryingID.SelectedIndex;
            int cavityIdx = this.comboBoxTierID.SelectedIndex;
            for (cavityIdx = 0; cavityIdx < 4; cavityIdx++)
            {
                string msg = "";
                RunID runId = RunID.DryOven0 + dryingID;
                CavityStatus cavityState = MachineCtrl.GetInstance().GetDryingOvenCavityState(runId, cavityIdx);
                if (CavityStatus.WaitResult == cavityState)
                {
                    if (null == MachineCtrl.GetInstance().GetModule(runId) && !MachineCtrl.GetInstance().ClientIsConnect())
                    {
                        msg = $"干燥炉{dryID + 1}】模组不在此设备，且模组未连接不能在此设备上传水含量";
                        ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
                        return;
                    }
                    if (MachineCtrl.GetInstance().SetCavityWaterContent(runId, cavityIdx, waterValue))
                    {
                        if (!Def.IsNoHardware())
                        {
                            msg = $"干燥炉{dryID + 1} - {cavityIdx + 1}层腔体水含量结果上传成功";
                            ShowMsgBox.ShowDialog(msg, MessageType.MsgMessage);
                        }

                        for (int idx = 0; idx < txtBox.Length - 1; idx++)
                        {
                            txtBox[0].Text = "1";
                        }

                        if (!Def.IsNoHardware())
                        {
                            txtBox[txtBox.Length - 1].Text = "";
                        }
                    }
                }
            }
        }

        private void btnConnection_Click(object sender, EventArgs e)
        {
            try
            {
                //连接远程服务器
                WcfClient client = new WcfClient(
               "WH02C0122PR-HKX00211",
               (messageRequest) =>
               {
                   MessageBox.Show(Newtonsoft.Json.JsonConvert.SerializeObject(messageRequest));
                   return new MessageResponse()
                   {
                       Success = true,
                       MessageGuid = messageRequest.MessageGuid,
                       CommandResponseJson = "I am response for server request. MessageGuid : " + messageRequest.MessageGuid
                   };
               }
               , "172.20.32.103:8007"
               );
            }
            catch (Exception ex)
            {

            }
        }

        //读取csv
        public static bool ReadCsv(string path, out List<UnLineData> data)
        {
            StreamReader sr;
            data = new List<UnLineData>();
            bool titleRes = false;
            try
            {
                using (sr = new StreamReader(path, Encoding.Default))
                {
                    string str = "";
                    while ((str = sr.ReadLine()) != null)
                    {
                        if (!titleRes)
                        {
                            titleRes = true;
                            continue;
                        }
                        UnLineData unLineData = new UnLineData();
                        str.Substring(0, str.IndexOf(','));

                        string[] strArray = str.Split(',');
                        unLineData.Time = strArray[0];
                        unLineData.PltCode = strArray[1];
                        unLineData.InterfaceName = strArray[2];
                        unLineData.ElapsedTime = strArray[3];
                        unLineData.Result = strArray[4];
                        unLineData.ResultMsg = strArray[5];
                        for (int i = 0; i < 9; i++)
                        {
                            str = str.Substring(str.IndexOf(',') + 1, str.Length - str.IndexOf(',') - 1);
                        }
                        unLineData.UpdataInfo = str;

                        //System.Text.RegularExpressions.Regex.Replace('\"','');
                        data.Add(unLineData);
                    }
                }
            }
            catch (Exception ex)
            {
                //if (ex.ToString().Contains("正由另一进程使用"))
                //    return false;
                //foreach (Process process in Process.GetProcesses())
                //{
                //    if (process.ProcessName.ToUpper().Equals("EXCEL"))
                //        process.Kill();
                //}
                //GC.Collect();
                Console.WriteLine(ex.StackTrace);
                //using (sr = new StreamReader(path, Encoding.GetEncoding("GB2312")))
                //{
                //    string str = "";
                //    while ((str = sr.ReadLine()) != null)
                //    {
                //        data.Add(str);
                //    }
                //}
            }
            return true;
        }

        /// <summary>
        /// 更新表格中模组状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateModuleState1(object sender, System.Timers.ElapsedEventArgs e)
        {
            //try
            //{
            //    if (false == MachineCtrl.GetPullInExCsvFileState(ref filePath) || filePath == "")
            //        return;

            //    // 使用委托更新UI
            //    this.Invoke(new Action(() =>
            //    {
            //        if (ReadCsv(filePath, out OutBankData))
            //        {

            //            //当文件更换时删除所有行
            //            if (filePath != copy_filePath)
            //            {
            //                copy_filePath = filePath;

            //                while (this.dgvIn.Rows.Count > 0)
            //                {
            //                    for (int index = 0; index < this.dgvIn.Rows.Count; index++)
            //                    {
            //                        this.dgvIn.Rows.Remove(dgvIn.Rows[index]);
            //                    }
            //                }
            //            }

            //            ////将读取到的信息顺序反转
            //            //OutBankData.Reverse();

            //            for (int i = this.dgvIn.Rows.Count; i < OutBankData.Count - 1; i++)            // 添加行数据
            //            {
            //                int index = this.dgvIn.Rows.Add();
            //                this.dgvIn.Rows[index].Height = 20;        // 行高度                            
            //                for (int j = 0; j < this.dgvIn.ColumnCount; j++)
            //                {
            //                    if (j >= OutBankData[i + 1].Split(',').Length)
            //                        break;

            //                    this.dgvIn.Rows[index].Cells[j].Value = OutBankData[i + 1].Split(',')[j];

            //                    if (j == 3)
            //                    {
            //                        string codeResult = OutBankData[i + 1].Split(',')[j];
            //                        if (codeResult != "True")
            //                            this.dgvIn.Rows[index].DefaultCellStyle.BackColor = Color.Red;
            //                    }
            //                }
            //            }
            //            //将Datagridview的信息按降序排列，让最新的信息显示在上面
            //            dgvIn.Sort(dgvIn.Columns[0], ListSortDirection.Descending);
            //        }
            //    }))
            //    ;
            //}
            //catch (System.Exception ex)
            //{
            //    System.Diagnostics.Trace.WriteLine("FixtureMaintainPage.UpdateModuleState " + ex.Message);
            //}

        }
        private void UpdateModuleState2(object sender, System.Timers.ElapsedEventArgs e)
        {
            //try
            //{
            //    if (false == MachineCtrl.GetOutExCsvFileState(ref filePath) || filePath == "")
            //        return;

            //    // 使用委托更新UI
            //    this.Invoke(new Action(() =>
            //    {
            //        if (ReadCsv(filePath, out OutBankData))
            //        {

            //            //当文件更换时删除所有行
            //            if (filePath != copy_filePath)
            //            {
            //                copy_filePath = filePath;

            //                while (this.dataGridView2.Rows.Count > 0)
            //                {
            //                    for (int index = 0; index < this.dataGridView2.Rows.Count; index++)
            //                    {
            //                        this.dataGridView2.Rows.Remove(dataGridView2.Rows[index]);
            //                    }
            //                }
            //            }

            //            ////将读取到的信息顺序反转
            //            //OutBankData.Reverse();

            //            for (int i = this.dataGridView2.Rows.Count; i < OutBankData.Count - 1; i++)            // 添加行数据
            //            {
            //                int index = this.dataGridView2.Rows.Add();
            //                this.dataGridView2.Rows[index].Height = 20;        // 行高度                            
            //                for (int j = 0; j < this.dataGridView2.ColumnCount; j++)
            //                {
            //                    if (j >= OutBankData[i + 1].Split(',').Length)
            //                        break;

            //                    this.dataGridView2.Rows[index].Cells[j].Value = OutBankData[i + 1].Split(',')[j];

            //                    if (j == 3)
            //                    {
            //                        string codeResult = OutBankData[i + 1].Split(',')[j];
            //                        if (codeResult != "True")
            //                            this.dataGridView2.Rows[index].DefaultCellStyle.BackColor = Color.Red;
            //                    }
            //                }
            //            }
            //            //将Datagridview的信息按降序排列，让最新的信息显示在上面
            //            dataGridView2.Sort(dataGridView2.Columns[0], ListSortDirection.Descending);
            //        }
            //    }))
            //    ;
            //}
            //catch (System.Exception ex)
            //{
            //    System.Diagnostics.Trace.WriteLine("FixtureMaintainPage.UpdateModuleState " + ex.Message);
            //}

        }

        //人员数据
        private void btn_Click(object sender, EventArgs e)
        {
            string msg = "";
            string usercode = "";
            string workplace = "DAL1HK01";
            UserInfo userInfo = new UserInfo();
            if(Jeve_Mes.Mes_CheckUser(usercode,workplace,ref userInfo, ref msg))
            {
                MessageBox.Show(string.Format(@"工号为：{0}，姓名为：{1}，区域为：{2}，部门为：{3}",userInfo.UserCode,userInfo.UserName,userInfo.UserArea,userInfo.UserDep));
            }
            else
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }
            

        }

        private void button5_Click(object sender, EventArgs e)
        {
            //string msg = "";
            //if (!MachineCtrl.GetInstance().ACEQPTALIV_Main(MesResources.Equipment, ref msg))
            //{
            //    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            //}
            ////rtxtSendInfo.Text = sendValue;
            ////rtxtRecvInfo.Text = recvValue;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string msg = "";
            List<string> list = new List<string>();

            if (MachineCtrl.GetInstance().ACEMPLOYEE_Main(MesResources.Equipment, ref msg, ref list))
            {

                string[] idArray = new string[list.Count];
                idArray = list.ToArray();
                this.dbRecord = MachineCtrl.GetInstance().dbRecord;
                this.dbRecord.DeleteAllUserInfo();
                MachineCtrl.GetInstance().dbRecord.AddUserInfo(new UserFormula("999", "Admin", "123456", 0));
                foreach (var item in idArray)
                {
                    string[] data = item.Split(',');
                    string Id = data[0].ToString();
                    string Name = data[2].ToString();
                    string Pw = data[3].ToString();
                    string UserLevel = data[4].ToString();
                    if (UserLevel.Equals("1") || UserLevel.Equals("2") || UserLevel.Equals("3"))
                    {
                        this.dbRecord.AddUserInfo(new UserFormula(Id, Name, Pw, /*(UserLevelType)UserLevel*/(UserLevelType)(Convert.ToInt32(UserLevel) - 1)));

                    }
                }
            }

            else
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }

        }

        private void button7_Click(object sender, EventArgs e)
        {
            string msg = "";
            string Id = "wyn";
            string Pw = "";
            if (!MachineCtrl.GetInstance().ACUSERINFO_Main(MesResources.Equipment, Id, Pw, 0, ref msg))
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }
        }

        //MES设备状态变更
        private void button8_Click(object sender, EventArgs e)
        {
            string msg = "";
            string workplace = "DAL1HK01";
            MesMCState mesState = new MesMCState();
            mesState = MesMCState.Stop;
            if (!Jeve_Mes.Mes_WorkPlaceStatus(workplace, mesState,ref msg))
            {
                ShowMsgBox.ShowDialog($"{msg}",MessageType.MsgAlarm);
            }
            
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string msg = "";

            if (!MachineCtrl.GetInstance().ACEQPTALRT_Main(MesResources.Equipment, "HK001", "MES报警测试——WJL", MesAlarmStatus.Happen, MesAlarmLevel.One, ref msg))
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            string msg = "";
            string lotno = "";
            string workplace = "DAL1HK01";
            string checkroute = "";
            string unbind = "";
            MYSQL.MySqlBatteryBasket.ListBasketData listBasketData = new MYSQL.MySqlBatteryBasket.ListBasketData();
            if (Jeve_Mes.Mes_GetVehicleInfo(lotno, workplace,checkroute,unbind,/*ref listBasketData,*/ ref msg))
            {

            }
            else
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }

        }

        private void button11_Click(object sender, EventArgs e)
        {
            string msg = "";
            RunProcessRobotTransfer run = MachineCtrl.GetInstance().GetModule(RunID.Transfer) as RunProcessRobotTransfer;
            if (!MachineCtrl.GetInstance().ACINBOUND_Main(MesResources.Equipment, run.Pallet[0], MesResources.Equipment.EquipmentCode, "", true, false, ref msg))
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }

        }

        //参数获取
        private void button4_Click(object sender, EventArgs e)
        {
            string msg = "";
            string lotno = "";
            string vehicleno = "";
            string oporder = MesResources.OrderNo;
            string workplace = "DAL1HK01";
            MesRecipeStruct mesRecipeStruct = new MesRecipeStruct();
            if (!Jeve_Mes.Mes_GetParam(lotno, vehicleno, oporder, workplace,ref mesRecipeStruct,ref msg))
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //string msg = "";
            //RunProcessOffloadBattery run = MachineCtrl.GetInstance().GetModule(RunID.OffloadBattery) as RunProcessOffloadBattery;
            //Param param = new Param();
            //object[] Parameters = new object[10];

            //param.getParam(code, preheatTime, bakingTime.bakingVcuum, bakingTemp, pos.coolingTemp, sfcTemp, hk9, hk10, ref  Parameters);
            //if (!MachineCtrl.GetInstance().ACLOGOFF_Main(MesResources.Equipment, run.Pallet[0], Parameters, ref msg))
            //{
            //    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            //}
        }

        private void button12_Click(object sender, EventArgs e)
        {
            string msg = "";
            if (!MachineCtrl.GetInstance().ACEQPTINIT_Main(MesResources.Equipment, ref msg))
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string msg = "";
            if (!MachineCtrl.GetInstance().ACEQPTCHNG_Main(MesResources.Equipment, ref msg))
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }
        }
        private void contextMenuStripOP_Click(object sender, EventArgs e)
        {
            try
            {
                string msg = "";
                if (!(sender is ToolStripItem))
                {
                    return;
                }
                ToolStripItem item = sender as ToolStripItem;
                DataGridViewRow row = null;
                string pltCode = "";
                string intanceName = "";
                string updataInfo = "";
                switch (item.Name)
                {
                    //单条数据上传
                    case "Updata":
                        row = dgvBuf.SelectedRows[0];
                        pltCode = row.Cells[1].Value.ToString();
                        intanceName = row.Cells[2].Value.ToString();
                        updataInfo = row.Cells[5].Value.ToString();

                        switch (dgvBuf.Name)
                        {
                            case "dgvBind":
                                if (MachineCtrl.GetInstance().UpDataUnLineData(pltCode,"dgvBind", intanceName, updataInfo, ref msg))
                                {
                                    Def.DeleteCsvLine(fileBindPath, row.Cells[1].Value.ToString());
                                    unLineDataBindList.Remove(unLineDataBindList.FirstOrDefault(t => t.PltCode == pltCode));
                                    CreateList(dgvBind, unLineDataBindList);
                                }
                                else
                                {
                                    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                                }
                                break;
                            case "dgvUnBind":
                                if (MachineCtrl.GetInstance().UpDataUnLineData(pltCode, "dgvUnBind", intanceName, updataInfo, ref msg))
                                {
                                    Def.DeleteCsvLine(fileUnBindPath, row.Cells[1].Value.ToString());
                                    unLineDataUnBindList.Remove(unLineDataUnBindList.FirstOrDefault(t => t.PltCode == pltCode));
                                    CreateList(dgvUnBind, unLineDataUnBindList);
                                }
                                else
                                {
                                    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                                }
                                break;
                            case "dgvIn":
                                if (MachineCtrl.GetInstance().UpDataUnLineData(pltCode, "dgvIn", intanceName, updataInfo, ref msg))
                                {
                                    Def.DeleteCsvLine(fileInPath, row.Cells[1].Value.ToString());
                                    unLineDataInList.Remove(unLineDataInList.FirstOrDefault(t => t.PltCode == pltCode));
                                    CreateList(dgvIn, unLineDataInList);
                                }
                                else
                                {
                                    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                                }
                                break;
                            case "dgvOut":
                                if (MachineCtrl.GetInstance().UpDataUnLineData(pltCode, "dgvOut", intanceName, updataInfo, ref msg))
                                {
                                    Def.DeleteCsvLine(fileOutPath, row.Cells[1].Value.ToString());
                                    unLineDataOutList.Remove(unLineDataOutList.FirstOrDefault(t => t.PltCode == pltCode));
                                    CreateList(dgvOut, unLineDataOutList);
                                }
                                else
                                {
                                    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    case "Delet":
                        row = dgvBuf.SelectedRows[0];
                        pltCode = row.Cells[1].Value.ToString();
                        intanceName = row.Cells[2].Value.ToString();
                        updataInfo = row.Cells[5].Value.ToString();
                        switch (dgvBuf.Name)
                        {
                            case "dgvBind":
                                Def.DeleteCsvLine(fileBindPath, row.Cells[1].Value.ToString());
                                unLineDataBindList.Remove(unLineDataBindList.FirstOrDefault(t => t.PltCode == pltCode));
                                CreateList(dgvBind, unLineDataBindList);
                                break;
                            case "dgvUnBind":
                                Def.DeleteCsvLine(fileUnBindPath, row.Cells[1].Value.ToString());
                                unLineDataUnBindList.Remove(unLineDataUnBindList.FirstOrDefault(t => t.PltCode == pltCode));
                                CreateList(dgvUnBind, unLineDataUnBindList);
                                break;
                            case "dgvIn":
                                Def.DeleteCsvLine(fileInPath, row.Cells[1].Value.ToString());
                                unLineDataInList.Remove(unLineDataInList.FirstOrDefault(t => t.PltCode == pltCode));
                                CreateList(dgvIn, unLineDataInList);
                                break;
                            case "dgvOut":
                                Def.DeleteCsvLine(fileOutPath, row.Cells[1].Value.ToString());
                                unLineDataOutList.Remove(unLineDataOutList.FirstOrDefault(t => t.PltCode == pltCode));
                                CreateList(dgvOut, unLineDataOutList);
                                break;
                            default:
                                break;
                        }
                        break;
                    case "UpdataAll":
                        int _count = dgvBuf.Rows.Count;
                        //for (int i = 0; i < _count; i++)
                        //{
                            switch (dgvBuf.Name)
                            {
                                case "dgvBind":
                                    for (int idx = 0; idx < _count; idx++)
                                    {
                                        if (MachineCtrl.GetInstance().UpDataUnLineData(unLineDataBindList[idx].PltCode, "dgvBind", unLineDataBindList[idx].InterfaceName, unLineDataBindList[idx].UpdataInfo, ref msg))
                                        {
                                            Def.DeleteCsvLine(fileBindPath, unLineDataBindList[idx].PltCode);
                                        }
                                        else
                                        {
                                            ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                                        }
                                    }
                                    ReadCsv(fileBindPath, out unLineDataBindList);
                                    CreateList(dgvBind, unLineDataBindList);
                                    break;
                                case "dgvUnBind":
                                    for (int idx = 0; idx < _count; idx++)
                                    {
                                        if (MachineCtrl.GetInstance().UpDataUnLineData(unLineDataUnBindList[idx].PltCode, "dgvUnBind", unLineDataUnBindList[idx].InterfaceName, unLineDataUnBindList[idx].UpdataInfo, ref msg))
                                        {
                                            Def.DeleteCsvLine(fileUnBindPath, unLineDataUnBindList[idx].PltCode);
                                        }
                                        else
                                        {
                                            ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                                        }
                                    }
                                    ReadCsv(fileUnBindPath, out unLineDataUnBindList);
                                    CreateList(dgvUnBind, unLineDataUnBindList);
                                    break;
                                case "dgvIn":
                                    for (int idx = 0; idx < _count; idx++)
                                    {
                                        if (MachineCtrl.GetInstance().UpDataUnLineData(unLineDataInList[idx].PltCode,"dgvIn", unLineDataInList[idx].InterfaceName, unLineDataInList[idx].UpdataInfo, ref msg))
                                        {
                                            Def.DeleteCsvLine(fileInPath, unLineDataInList[idx].PltCode);
                                        }
                                        else
                                        {
                                            ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                                        }
                                    }
                                    ReadCsv(fileInPath, out unLineDataInList);
                                    CreateList(dgvIn, unLineDataInList);
                                    break;
                                case "dgvOut":
                                    for (int idx = 0; idx < _count; idx++)
                                    {
                                        if (MachineCtrl.GetInstance().UpDataUnLineData(unLineDataOutList[idx].PltCode, "dgvOut", unLineDataOutList[idx].InterfaceName, unLineDataOutList[idx].UpdataInfo, ref msg))
                                        {
                                            Def.DeleteCsvLine(fileOutPath, unLineDataOutList[idx].PltCode);
                                        }
                                        else
                                        {
                                            ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                                        }
                                    }
                                    ReadCsv(fileOutPath, out unLineDataOutList);
                                    CreateList(dgvOut, unLineDataOutList);
                                    break;
                                default:
                                    break;
                            //}
                        }
                        break;
                    case "DeletAll":
                        switch (dgvBuf.Name)
                        {
                            case "dgvBind":
                                unLineDataBindList.Clear();
                                File.SetAttributes(fileBindPath, FileAttributes.Normal);
                                File.Delete(fileBindPath);
                                CreateList(dgvBind, unLineDataBindList);
                                break;
                            case "dgvUnBind":
                                unLineDataUnBindList.Clear();
                                File.SetAttributes(fileUnBindPath, FileAttributes.Normal);
                                File.Delete(fileUnBindPath);
                                CreateList(dgvUnBind, unLineDataUnBindList);
                                break;
                            case "dgvIn":
                                unLineDataInList.Clear();
                                File.SetAttributes(fileInPath, FileAttributes.Normal);
                                File.Delete(fileInPath);
                                CreateList(dgvIn, unLineDataInList);

                                break;
                            case "dgvOut":
                                unLineDataOutList.Clear();
                                File.SetAttributes(fileOutPath, FileAttributes.Normal);
                                File.Delete(fileOutPath);
                                CreateList(dgvOut, unLineDataOutList);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {

            }

        }
        private void dgvIn_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DataGridView dgv = sender as DataGridView;
                dgvBuf = dgv;
                if (dgv.SelectedRows.Count == 1)
                {
                    this.contextMenuStripOP.Show(MousePosition.X, MousePosition.Y);
                }
            }
        }

        //获取生产任务
        private void buttonGetRunOpList_Click(object sender, EventArgs e)
        {
            string workplace = "DAL1HK01";
            string oporder = "";
            MesRecipeStruct mesRecipeStruct = new MesRecipeStruct();
            MesDefine.GetRunOpList(workplace,oporder, mesRecipeStruct);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            bool status = false;
            string usercode = MesResources.Equipment.OperatorUserID, oporder = MesResources.OpOrder, workplace = "DAL1HK01";
            string msg = "";
            if (!Jeve_Mes.Mes_LogoutOp(usercode, oporder, workplace, status, ref msg))
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            bool status = true;
            string usercode = MesResources.Equipment.OperatorUserID, oporder = MesResources.OpOrder, workplace = "DAL1HK01";
            string msg = "";
            if (!Jeve_Mes.Mes_LogoutOp(usercode, oporder, workplace, status, ref msg))
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }
        }
    }
    public class UnLineData
    {
        //时间
        public string Time { get; set; }
        //托盘码码
        public string PltCode { get; set; }
        //接口名称
        public string InterfaceName { get; set; }
        //耗时
        public string ElapsedTime { get; set; }
        //结果
        public string Result { get; set; }
        //MES上传内容
        public string UpdataInfo { get; set; }
        //MES返回信息
        public string ResultMsg { get; set; }

    }
}

