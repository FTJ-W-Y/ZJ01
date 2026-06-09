using Machine.UI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Machine
{
    public partial class DebugToolsPage : FormEx
    {
        public DebugToolsPage()
        {
            InitializeComponent();

            CreateTabPage();
        }

        private void DebugToolsPage_Load(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// 销毁自定义非托管资源
        /// </summary>
        public override void DisposeForm()
        {
            foreach(Control tab in this.tabControl.Controls)
            {
                foreach(Control form in tab.Controls)
                {
                    if (form is FormEx)
                    {
                        ((FormEx)form).DisposeForm();
                    }
                }
            }
        }

        /// <summary>
        /// 当设备状态或用户权限改变时，更新UI界面的使能
        /// </summary>
        /// <param name="enable"></param>
        public override void UpdataUIEnable(SystemControlLibrary.MCState mc, SystemControlLibrary.UserLevelType level)
        {
            TabPage tp = this.tabControl.SelectedTab;
            foreach(Control form in tp.Controls)
            {
                if(form is FormEx)
                {
                    ((FormEx)form).UpdataUIEnable(mc, level);
                }
            }
        }

        private void CreateTabPage()
        {
            //Form form = new RobotPage();
            //form.TopLevel = false;
            //form.Dock = DockStyle.Fill;
            //form.Show();
            //this.tabPageRobot.Controls.Add(form);
            
            if (0 == MachineCtrl.GetInstance().MachineID)
            {
                AddTabPage("机器人调试", new RobotPage());
                AddTabPage("上料调试", new OnlineDebugPage());
                AddTabPage("生产信息", new ProductionDays());
            }
            else if (1 == MachineCtrl.GetInstance().MachineID)
            {
                AddTabPage("机器人调试", new RobotPage());
                AddTabPage("干燥炉调试", new DryingOvenPage());
                AddTabPage("其他调试", new TransferDebugPage());
                AddTabPage("帮助说明", new HelperDebugPage());
            }
            else if (2 == MachineCtrl.GetInstance().MachineID)
            {
                AddTabPage("下料调试", new OtherDebugPage());
                AddTabPage("生产信息", new ProductionDays());
            }
            else //if (3 == MachineCtrl.GetInstance().MachineID)
            {
                AddTabPage("机器人调试", new RobotPage());
                AddTabPage("干燥炉调试", new DryingOvenPage());
                AddTabPage("上料调试", new OnlineDebugPage());
                AddTabPage("调度调试", new TransferDebugPage());
                AddTabPage("下料调试", new OtherDebugPage());
                AddTabPage("生产信息", new ProductionDays());
                AddTabPage("帮助说明", new HelperDebugPage());
            }

            foreach (Control item in this.tabControl.Controls)
            {
                item.BackColor = Color.Transparent;
            }
        }

        private void AddTabPage(string tabPageName, Form frm)
        {
            if (null == frm) return;

            frm.TopLevel = false;
            frm.Dock = DockStyle.Fill;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Show();

            TabPage tp = new TabPage();
            tp.Text = tabPageName;
            tp.Controls.Add(frm);
            this.tabControl.Controls.Add(tp);
        }

        private void DebugToolsPage_VisibleChanged(object sender, EventArgs e)
        {
            tabControl_SelectedIndexChanged(sender, e);
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabPage tp = this.tabControl.SelectedTab;
            for(int i = 0; i < this.tabControl.Controls.Count; i++)
            {
                foreach(Control form in this.tabControl.Controls[i].Controls)
                {
                    ((FormEx)form).UIVisibleChanged(tp.Controls.Contains(form));
                }
            }
        }
    }
}
