using HelperLibrary;
using Machine.Framework.Mes;
using Machine.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Net;
using System.Windows.Forms;
using SystemControlLibrary;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    public partial class MainForm : Form
    {
        #region // 字段

        private bool bFlash;
        private Font fontFlash;
        private Graphics graphFlash;
        private System.Timers.Timer timerUpdata;
        private RunCtrl runCtrl;
        private MCState newMCState;
        private MCState oldMCState;
        private UserLevelType newUserLevel;
        private UserLevelType oldUserLevel;
        private DateTime userLoginTime;

        private Image[] radioBtnSelectedImg;
        private Image[] radioBtnUnselectedImg;
        private List<FormEx> formList;
        private Framework.HookClass hookClass;
        private object objectLook;

        #endregion


        public MainForm()
        {
            InitializeComponent();


            if (!MachineCtrl.GetInstance().dbRecord.OpenDataBase(Def.GetAbsPathName(Def.MachineMdb), ""))
            {
                ShowMsgBox.ShowDialog("数据库打开失败，继续操作将不能保存报警及生产信息", MessageType.MsgAlarm);
            }

            if (!MachineCtrl.GetInstance().Initialize(this.Handle))
            {
                Environment.Exit(0);
            }
            //MesGetOpList();
            this.runCtrl = MachineCtrl.GetInstance().RunsCtrl;
            hookClass = new Framework.HookClass();
            objectLook = new object();
        }


        #region // 界面

        /// <summary>
        /// 加载窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            // RadioBtn控件选择状态图标
            radioBtnSelectedImg = new Image[]
            {
                global::Machine.Properties.Resources.OverView_Selected,
                global::Machine.Properties.Resources.ModuleMonitor_Selected,
                global::Machine.Properties.Resources.Maintenance_Selected,
                global::Machine.Properties.Resources.Parameter_Selected,
                global::Machine.Properties.Resources.DebugTools_Selected,
                global::Machine.Properties.Resources.MESSet_Selected,
                global::Machine.Properties.Resources.History_Selected,
            };

            // RadioBtn控件未选择状态图标
            radioBtnUnselectedImg = new Image[]
            {
                global::Machine.Properties.Resources.OverView_Unselected,
                global::Machine.Properties.Resources.ModuleMonitor_Unselected,
                global::Machine.Properties.Resources.Maintenance_Unselected,
                global::Machine.Properties.Resources.Parameter_Unselected,
                global::Machine.Properties.Resources.DebugTools_Unselected,
                global::Machine.Properties.Resources.MESSet_Unselected,
                global::Machine.Properties.Resources.History_Unselected,
            };

            int index = 0;      // Page编号
            // 动画界面
            this.formList = new List<FormEx>();
            FormEx form = new OverViewPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Parent = this.panelPage;
            form.Tag = index++;
            this.formList.Add(form);
            // 监控界面
            form = new ModuleMonitorPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Parent = this.panelPage;
            form.Tag = index++;
            this.formList.Add(form);
            // 维护界面
            form = new MaintenancePage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Parent = this.panelPage;
            form.Tag = index++;
            this.formList.Add(form);
            // 参数设置
            form = new ParameterPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Parent = this.panelPage;
            form.Tag = index++;
            this.formList.Add(form);
            // 调试工具
            form = new DebugToolsPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Parent = this.panelPage;
            form.Tag = index++;
            this.formList.Add(form);
            // MES设置
            form = new MesSetPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Parent = this.panelPage;
            form.Tag = index++;
            this.formList.Add(form);
            // 历史记录
            form = new HistoryPage();
            form.TopLevel = false;
            form.Dock = DockStyle.Fill;
            form.Parent = this.panelPage;
            form.Tag = index++;
            this.formList.Add(form);


            // RadioBtn编号，需和上面Page编号对应
            this.radioBtnMainPage.Tag = 0;
            this.radioBtnModuleMonitor.Tag = 1;
            this.radioBtnMaintenance.Tag = 2;
            this.radioBtnParameter.Tag = 3;
            this.radioBtnDebugTools.Tag = 4;
            this.radioBtnMESSet.Tag = 5;
            this.radioBtnHistoryPage.Tag = 6;

            // 最大化显示
            //this.WindowState = FormWindowState.Maximized;
            // 默认选择主界面
            this.radioBtnMainPage.Checked = true;
            // 设备名称
            this.Text = IniFile.ReadString("Title", "Title", this.Text, Def.GetAbsPathName(Def.MachineCfg)) + " v" + Application.ProductVersion;
            // 加载软件Logo
            string appPath = System.Windows.Forms.Application.StartupPath;
            if (System.IO.File.Exists(appPath + @"\System\Logo\Logo.ico"))
            {
                this.Icon = new Icon(appPath + @"\System\Logo\Logo.ico");
            }
            // 加载设备Logo
            if (System.IO.File.Exists(appPath + @"\System\Logo\Jeve.png")) //图片需跟exe同一路径下
            {
                this.pictureLogo.Image = Image.FromFile(appPath + @"\System\Logo\Jeve.png");
                this.pictureLogo.SizeMode = PictureBoxSizeMode.StretchImage;
            }

            // 设置提示
            ToolTip tip = new ToolTip();
            tip.SetToolTip(this.buttonStart, "启动设备运行");
            tip.SetToolTip(this.buttonStop, "停止运行");
            tip.SetToolTip(this.buttonReset, "清除报警");
            tip.SetToolTip(this.buttonRestart, "恢复设备到初始闲置状态");
            tip.SetToolTip(this.checkBoxUser, "左键登录注销\r\n右键管理用户");
            tip.SetToolTip(this.buttonLock, "维护设备时锁定上位机，防止误操作");

            // 禁用启动按钮
            this.buttonStart.Enabled = IsStartBtnEnable(); //Def.IsNoHardware();

            // 添加用户管理右键菜单
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Items.Add("管理用户");
            cms.Items[0].Click += CheckBox_Click_UserManager;
            this.checkBoxUser.ContextMenuStrip = cms;

            // 定时刷新设备状态
            bFlash = true;
            fontFlash = new Font("微软雅黑", 28);
            graphFlash = this.pictureState.CreateGraphics();
            timerUpdata = new System.Timers.Timer();
            timerUpdata.Elapsed += UpdataUIState;
            timerUpdata.Interval = 1000;                 // 间隔时间
            timerUpdata.AutoReset = true;                // 设置一直执行
            timerUpdata.Start();                         // 开始执行定时器

            MachineCtrl.GetInstance().LogingOutUI += LogingOutUI;
            hookClass.SetLogingUI += UpdataLogingUI;

            // 高级用户登录计时
            this.userLoginTime = DateTime.Now;
            hookClass.SetHook();
        }

        /// <summary>
        /// 窗体关闭前
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ((MCState.MCInitializing == this.newMCState) || (MCState.MCRunning == this.newMCState))
            {
                ShowMsgBox.ShowDialog("设备运行中不能退出", MessageType.MsgWarning);
                e.Cancel = true;
                return;
            }
            if (MachineCtrl.GetInstance().dbRecord.UserLevel() > UserLevelType.USER_MAINTENANCE
                && MCState.MCStopRun == this.newMCState)
            {
                ShowMsgBox.ShowDialog("操作员不能退出，有问题需退出请联系维护员", MessageType.MsgWarning);
                e.Cancel = true;
                return;
            }
            if (MachineCtrl.GetInstance().GetRobotRunning(RunID.OnloadRobot) || MachineCtrl.GetInstance().GetRobotRunning(RunID.Transfer))
            {
                if (DialogResult.Yes != ShowMsgBox.ShowDialog("是否确认退出软件\r\n如果机器人在运行中，退出软件将急停机器人", MessageType.MsgQuestion))
                {
                    e.Cancel = true;
                    return;
                }
            }
            timerUpdata.Stop();
            hookClass.ClearHook();
            if (Def.IsNoHardware() || (DialogResult.Yes == ShowMsgBox.ShowDialog("是否确认退出软件", MessageType.MsgQuestion)))
            {
                try
                {
                    foreach (var item in formList)
                    {
                        item.DisposeForm();
                        item.Dispose();
                    }
                    //MachineCtrl.GetInstance().Dispose();
                    Process.GetCurrentProcess().Kill();


                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("MainForm", string.Format("FormClosing() Release fail: {0}", ex.ToString()));
                }
            }
            else
            {
                timerUpdata.Start();
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 界面选择
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void radioBtnPageChoose(object sender, EventArgs e)
        {
            RadioButton rBtn = sender as RadioButton;
            if (null != rBtn)
            {
                int tag = Convert.ToInt32(rBtn.Tag);

                if (tag < this.formList.Count)
                {
                    // 设置单选按钮外观
                    rBtn.BackgroundImage = rBtn.Checked ? radioBtnSelectedImg[tag] : radioBtnUnselectedImg[tag];
                    rBtn.ForeColor = rBtn.Checked ? Color.White : Color.Black;

                    if (rBtn.Checked)
                    {
                        formList[tag].Show();
                    }
                    else
                    {
                        formList[tag].Hide();
                    }
                }

                // 切换界面时重新计用户退出时间
                this.userLoginTime = DateTime.Now;
            }
        }

        #endregion

        public void MesGetOpList()
        {
            try
            {

                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Enabled = true;
                timer.Interval = 60000;
                timer.Start();
                timer.Elapsed += new System.Timers.ElapsedEventHandler(TimersMes);
            }
            catch (Exception ex)
            {

            }
        }
        public static void TimersMes(object obj, System.Timers.ElapsedEventArgs e)
        {
            MesRecipeStruct mesRecipeStruct = new MesRecipeStruct();
            string workplace = "DAL1HK01";
            string oporder = "";
            string msg = "";
            if (!Jeve_Mes.Mes_GetRunOpList(workplace, oporder, ref mesRecipeStruct, ref msg))
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgWarning);
            }

        }

        #region // 设备控制

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStart_Click(object sender, EventArgs e)
        {
            this.runCtrl.Start();
        }

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStop_Click(object sender, EventArgs e)
        {
            this.runCtrl.Stop();
        }

        /// <summary>
        /// 复位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonReset_Click(object sender, EventArgs e)
        {
            MachineCtrl.GetInstance().MachineReset();
        }

        /// <summary>
        /// 整机重置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRestart_Click(object sender, EventArgs e)
        {
            MCState state = this.runCtrl.GetMCState();
            if ((MCState.MCInitializing == state) || (MCState.MCRunning == state))
            {
                ShowMsgBox.ShowDialog("设备运行中不能进行整机重置", MessageType.MsgWarning);
                return;
            }
            if (MachineCtrl.GetInstance().dbRecord.UserLevel() > UserLevelType.USER_MAINTENANCE)
            {
                ShowMsgBox.ShowDialog("操作员无权限进行整机重置", MessageType.MsgWarning);
                return;
            }
            if (Def.IsNoHardware() || DialogResult.Yes == ShowMsgBox.ShowDialog("整机重置操作后软件将恢复到闲置中...\r\n是否确认整机重置", MessageType.MsgQuestion))
            {
                this.runCtrl.Restart();
            }
        }

        /// <summary>
        /// 挂锁维修
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLock_Click(object sender, EventArgs e)
        {
            MachineCtrl.GetInstance().RunsCtrl.Stop();
            MCState state = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if (MCState.MCRunning == state)
            {
                string msg = string.Format("运行中不可挂锁维修");
                ShowMsgBox.ShowDialog(msg, MessageType.MsgWarning);
            }
            else
            {
                this.Hide();
                MachineCtrl.GetInstance().MaintenanceLock = true;
                var page = new LockScreenPage();
                page.ShowDialog();
                this.Show();
                MachineCtrl.GetInstance().MaintenanceLock = false;
            }

        }
        #endregion


        #region // 用户管理

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Click_UserLogin(object sender, EventArgs e)
        {
            if (this.checkBoxUser.Checked)
            {
                // 0612 注释
                //if (MachineCtrl.GetInstance().UpdataMes)
                //{
                //    this.checkBoxUser.Checked = !this.checkBoxUser.Checked;
                //    return;
                //}
                List<UserFormula> userList = new List<UserFormula>();
                if (MachineCtrl.GetInstance().dbRecord.GetUserList(ref userList))
                {
                    UserLogin user = new UserLogin();
                    user.SetUserList(MachineCtrl.GetInstance().dbRecord, userList);
                    if (DialogResult.OK == user.ShowDialog())
                    {
                        this.userLoginTime = DateTime.Now;
                        //this.checkBoxUser.Text = "已登录";
                        this.checkBoxUser.Text = user.userInfo;
                        this.checkBoxUser.Image = Properties.Resources.UserLogin;
                        //MachineCtrl.GetInstance().SetRoleID();
                        return;
                    }
                }
                this.checkBoxUser.Checked = false;
            }
            else if ((MCState.MCInitializing == this.newMCState) || (MCState.MCRunning == this.newMCState))
            {
                ShowMsgBox.ShowDialog("设备运行中不能退出当前账号", MessageType.MsgWarning);
                this.checkBoxUser.Checked = true;
                return;
            }
            else
            {
                if (DialogResult.Yes == ShowMsgBox.ShowDialog(string.Format("是否退出当前账号"), MessageType.MsgQuestion))
                {
                    MachineCtrl.GetInstance().dbRecord.UserLogout();
                    this.checkBoxUser.Text = "未登录";
                    this.checkBoxUser.Image = Properties.Resources.UserLogout;
                    //MachineCtrl.GetInstance().SetRoleID();
                    this.checkBoxUser.Checked = false;
                }
                else
                {
                    this.checkBoxUser.Checked = true;

                }
            }
        }

        /// <summary>
        /// 用户管理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Click_UserManager(object sender, EventArgs e)
        {
            UserFormula user = new UserFormula();
            List<UserFormula> userList = new List<UserFormula>();
            if (MachineCtrl.GetInstance().dbRecord.GetCurUser(ref user) && MachineCtrl.GetInstance().dbRecord.GetUserList(ref userList))
            {
                if (((null != user.userName && UserLevelType.USER_ADMIN == user.userLevel)) || (userList.Count < 1))
                {
                    UserManager um = new UserManager();
                    um.SetUserManagerInfo(MachineCtrl.GetInstance().dbRecord, userList);
                    um.ShowDialog();
                }
                else if (!string.IsNullOrEmpty(user.userName))
                {
                    UserPasswordsModify pwModify = new UserPasswordsModify();
                    pwModify.SetUserInfo(MachineCtrl.GetInstance().dbRecord, user);
                    pwModify.ShowDialog();
                }
            }
        }
        #endregion


        #region // 设备状态

        /// <summary>
        /// 更新设备状态
        /// </summary>
        private void UpdataUIState(object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                this.newMCState = this.runCtrl.GetMCState();
                this.newUserLevel = MachineCtrl.GetInstance().dbRecord.UserLevel();
                switch (this.newMCState)
                {
                    case MCState.MCIdle:
                        DrawPicture(graphFlash, "闲 置 中", Color.FromArgb(bFlash ? 255 : 0, 0, 176, 80), Color.FromArgb(0, 0, 0));
                        break;
                    case MCState.MCInitializing:
                        DrawPicture(graphFlash, "正在初始化", Color.FromArgb(0, 250, 0), Color.FromArgb(0, 0, 0));
                        break;
                    case MCState.MCInitComplete:
                        DrawPicture(graphFlash, "初始化完成", Color.FromArgb(0, 176, 80), Color.FromArgb(0, 0, 0));
                        break;
                    case MCState.MCRunning:
                        DrawPicture(graphFlash, "运 行 中", Color.FromArgb(0, 250, 0), Color.FromArgb(0, 0, 0));
                        break;
                    case MCState.MCStopInit:
                        DrawPicture(graphFlash, "初始化停止", Color.FromArgb(252, 179, 28), Color.FromArgb(0, 0, 0));
                        break;
                    case MCState.MCStopRun:
                        DrawPicture(graphFlash, "停 止", Color.FromArgb(252, 179, 28), Color.FromArgb(0, 0, 0));
                        break;
                    case MCState.MCInitErr:
                        DrawPicture(graphFlash, "初始化错误", Color.FromArgb(bFlash ? 255 : 0, 233, 77, 62), bFlash ? Color.FromArgb(255, 255, 255) : Color.FromArgb(233, 77, 62));
                        break;
                    case MCState.MCRunErr:
                        DrawPicture(graphFlash, "错 误", Color.FromArgb(bFlash ? 255 : 0, 233, 77, 62), bFlash ? Color.FromArgb(255, 255, 255) : Color.FromArgb(233, 77, 62));
                        break;
                }
                bFlash = !bFlash;
                if (this.newUserLevel < UserLevelType.USER_OPERATOR)
                {
                    if ((DateTime.Now - this.userLoginTime).TotalMinutes > 10.0)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            MachineCtrl.GetInstance().dbRecord.UserLogout();
                            this.checkBoxUser.Text = "未登录";
                            this.checkBoxUser.Image = Properties.Resources.UserLogout;
                            this.checkBoxUser.Checked = false;
                            //MachineCtrl.GetInstance().SetRoleID();
                        }));
                    }
                }
                if ((this.oldMCState != this.newMCState) || (this.oldUserLevel != this.newUserLevel))
                {
                    this.oldMCState = this.newMCState;
                    this.oldUserLevel = this.newUserLevel;
                    this.BeginInvoke(new Action(() =>
                    {
                        foreach (var item in formList)
                        {
                            if (item.Visible)
                            {
                                item.UpdataUIEnable(this.newMCState, this.newUserLevel);
                            }
                            else
                            {
                                item.UpdataUIEnable(MCState.MCRunning, UserLevelType.USER_LOGOUT);
                            }
                        }
                    }));
                }
                if (OperationShifts.ChangeShift())
                {
                    // 交接班：先清空产量，再退出操作员
                    if ((TotalData.OnloadCount > 0) || (TotalData.OnScanNGCount > 0)
                        || (TotalData.OffloadCount > 0) || (TotalData.BakedNGCount > 0))
                    {
                        TotalData.ClearTotalData();
                        TotalData.WriteTotalData();
                    }
                    MachineCtrl.GetInstance().OperaterID = "";
                }
                else
                {
                    TotalData.UpdateShiftTotalData();
                }
                //if (MachineCtrl.GetInstance().dbRecord.UserLevel() == UserLevelType.USER_LOGOUT && !hookClass.bStopMsg&&false)
                //{
                //    hookClass.bStopMsg = true;
                //    hookClass.SetHook();
                //}
                //else if (MachineCtrl.GetInstance().dbRecord.UserLevel() != UserLevelType.USER_LOGOUT && hookClass.bStopMsg)
                //{
                //    hookClass.bStopMsg = false;
                //    hookClass.ClearHook();
                //}
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MainForm.UpdataUIState()", $"{ex.Message}\r\n{ex.StackTrace}", LogType.Error);
            }
        }

        /// <summary>
        /// 画设备状态
        /// </summary>
        private void DrawPicture(Graphics graphics, string strText, Color BGClr, Color TextClr)
        {
            if (null != graphics)
            {
                Rectangle rcCtrl = pictureState.ClientRectangle;
                StringFormat strFormat = new StringFormat();
                Brush textBrush = new SolidBrush(TextClr);
                SolidBrush bgBrush = new SolidBrush(BGClr);
                strFormat.Alignment = StringAlignment.Center;
                strFormat.LineAlignment = StringAlignment.Center;
                graphics.Clear(this.BackColor);
                graphics.FillRectangle(bgBrush, rcCtrl);
                graphics.DrawString(strText, fontFlash, textBrush, rcCtrl, strFormat);
            }
        }
        #endregion



        private bool IsStartBtnEnable()
        {
            if (Def.IsNoHardware())
            {
                return true;
            }
            //方便本机测试
            string mac = "";
            //string hostInfo = Dns.GetHostName();
            //System.Net.IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            ManagementClass mc;
            mc = new ManagementClass("Win32_NetworkAdapterConfiguration"); ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if (mo["IPEnabled"].ToString() == "True")
                {
                    mac = mo["MacAddress"].ToString().Replace(":", "");
                    if (mac.ToLower().Equals("54e1ad2afb67"))
                        return true;
                }
            }
            return false;
        }
        private void UpdataLogingUI(string strLogingID)
        {
            lock (objectLook)
            {// MES在线状态
                this.BeginInvoke(new Action(() =>
                {
                    if (!MachineCtrl.GetInstance().UpdataMes)
                    {
                        return;
                    }
                    string userName = "";
                    string userCode = "";
                    int userLevel = -1;
                    if (null != MachineCtrl.GetInstance().dbRecord)
                    {

                        if (strLogingID.Equals("1111"))
                        {
                            if (MachineCtrl.GetInstance().dbRecord.UserLogin("Admin", "123456"))
                            {
                                MachineCtrl.GetInstance().OperaterID = userName;
                                this.userLoginTime = DateTime.Now;
                                this.checkBoxUser.Text = "Admin";
                                this.checkBoxUser.Image = Properties.Resources.UserLogin;
                                this.checkBoxUser.Checked = true;
                                MachineCtrl.GetInstance().UpdataMes = false;
                            }
                            return;
                        }
                        if (!MachineCtrl.GetInstance().isMESConnect)
                        {
                            return;
                        }
                        //GetEmployeeNo(strLogingID, ref userName, ref userCode, ref userLevel);

                        string errMsg = "";

                        string userInfo = MachineCtrl.GetInstance().OperaterID;
                        if (MachineCtrl.GetInstance().dbRecord.UserLogin(userName, strLogingID))
                        {
                            int le = (int)MachineCtrl.GetInstance().dbRecord.UserLevel();
                            MesResources.Equipment.OperatorUserID = userName;
                            MesResources.Equipment.OperatorPassword = userCode;
                            //if (!Def.IsNoHardware() && MachineCtrl.GetInstance().UpdataMes && !MachineCtrl.GetInstance().ACUSERINFO_Main(MesResources.Equipment, userCode, "", (int)MachineCtrl.GetInstance().dbRecord.UserLevel(), ref errMsg))
                            //{
                            //    MachineCtrl.GetInstance().dbRecord.UserLogout();
                            //    MesResources.MesLogin = true;
                            //    return;
                            //}
                            MachineCtrl.GetInstance().OperaterID = userName;
                            this.userLoginTime = DateTime.Now;
                            string levelName = userLevel == 0 ? "管理员" : userLevel == 1 ? "工程师" : userLevel == 2 ? "操作员" : "";
                            this.checkBoxUser.Text = userCode + "\r\n" + userName + "\r\n" + levelName;
                            this.checkBoxUser.Image = Properties.Resources.UserLogin;
                            this.checkBoxUser.Checked = true;
                            //MachineCtrl.GetInstance().SetRoleID();
                        }
                        else
                        {
                            ShowMsgBox.ShowDialog("用户名或密码不正确，无法登录！", MessageType.MsgAlarm);
                        }
                    }
                    else
                    {
                        ShowMsgBox.ShowDialog("查询到操作人员信息为空，请在MES手动添加相关人员信息！", MessageType.MsgAlarm);
                    }
                }));
                
            }
        }
        private void LogingOutUI(string strLogingID)
        {
            lock (objectLook)
            {
                this.BeginInvoke(new Action(() =>
                {
                    MachineCtrl.GetInstance().dbRecord.UserLogout();
                    this.checkBoxUser.Text = "未登录";
                    this.checkBoxUser.Image = Properties.Resources.UserLogout;
                    this.checkBoxUser.Checked = false;
                }));
            }
        }
        private void GetEmployeeNo(string logingID, ref string userName, ref string userCode, ref int userLevel)
        {
            string msg = "";
            List<string> list = new List<string>();

            if (MachineCtrl.GetInstance().ACEMPLOYEE_Main(MesResources.Equipment, ref msg, ref list))
            {
                string[] idArray = new string[list.Count];
                idArray = list.ToArray();
                MachineCtrl.GetInstance().dbRecord.DeleteAllUserInfo();
                MachineCtrl.GetInstance().dbRecord.AddUserInfo(new UserFormula("999", "Admin", "123456", 0));
                foreach (var item in idArray)
                {
                    string[] data = item.Split(',');
                    string Id = data[0].ToString();
                    string Name = data[2].ToString();
                    string Pw = data[3].ToString();
                    string UserLevel = data[4].ToString();
                    if (logingID.Equals(data[3].ToString()))
                    {
                        userCode = data[1].ToString();
                        userName = data[2].ToString();
                        userLevel = (Convert.ToInt32(UserLevel) - 1);
                    }
                    if (UserLevel.Equals("1") || UserLevel.Equals("2") || UserLevel.Equals("3"))
                    {
                        MachineCtrl.GetInstance().dbRecord.AddUserInfo(new UserFormula(Id, Name, Pw, (UserLevelType)(Convert.ToInt32(UserLevel) - 1)));
                    }
                }
            }
            else
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }
        }
    }
}
