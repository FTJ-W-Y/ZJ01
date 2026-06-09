using HelperLibrary;
using Machine.Properties;
using System;
using System.Threading.Tasks;

namespace Machine
{
    public partial class HelperDebugPage : FormEx
    {
        public HelperDebugPage()
        {
            InitializeComponent();
        }

        #region // 界面

        private void OnlineDebugPage_Load(object sender, EventArgs e)
        {
            pictureBox1.Image = Resources.oven_state1;
            pictureBox2.Image = Resources.oven_state2;
            pictureBox3.Image = Resources.oven_state3;
            pictureBox4.Image = Resources.oven_state4;
            pictureBox5.Image = Resources.oven_state5;
            pictureBox6.Image = Resources.oven_state6;
            pictureBox7.Image = Resources.oven_state7;
            pictureBox8.Image = Resources.oven_state7_2;
            pictureBox9.Image = Resources.oven_state8;
            pictureBox10.Image = Resources.oven_state9;
            pictureBox11.Image = Resources.oven_state10;
            pictureBox12.Image = Resources.buffer_state;
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
                        
                        break;
                    case UIEnable.OperatorEnabled:
                        this.Enabled = true;
                        
                        break;
                    default:
                        break;
                }
            }));
        }

        #endregion
    }
}
