using HelperLibrary;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Machine
{
    public partial class OvenBntPage : Form
    {
        #region // 字段

        int CloseSeconds;
        
        RunID runId;
        int ovenId;
        int cavityId;
        bool IsEnable;
        bool IsPressure;
        
        #endregion

        public OvenBntPage()
        {
            InitializeComponent();

            CloseSeconds = -1;
            runId = RunID.Invalid;
            ovenId = 0;
            cavityId = 0;
            IsEnable = true;
            IsPressure = false;
        }

        public void ReflashPage(RunID rid, int cid)
        {
            cavityId = cid;
            runId = rid;

            CavityStatus status  = MachineCtrl.GetInstance().GetDryingOvenCavityState(rid, cid);
            ovenId = rid - RunID.DryOven0 + 1;
            IsEnable = MachineCtrl.GetInstance().GetDryingOvenCavityEnable(rid, cid);
            IsPressure = MachineCtrl.GetInstance().GetDryingOvenCavityPressure(rid, cid);
            if (IsEnable)
            {
                btnEnable.Text = $"禁用炉腔";
            }
            else
            {
                btnEnable.Text = $"启用炉腔";
            }
            
            if (IsPressure)
            {
                btnPressure.Text = $"关闭保压";
            }
            else
            {
                btnPressure.Text = $"开启保压";
            }
            labState.Text = $"干燥炉{ovenId}-{cavityId + 1}层使能状态[{(IsEnable ? "开启" : "关闭")}],保压状态[{(IsPressure ? "开启" : "关闭")}]";
            if (CloseSeconds < 0)
            {
                CloseSeconds = 10;
                timer1.Start();
            }
            else
            {
                CloseSeconds = 10;
            }
        }
        
        private void MaintenanceLockPage_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void OvenBntPage_Load(object sender, EventArgs e)
        {
            label2.Text = "";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                CloseSeconds--;
                if (CloseSeconds > -1)
                {
                    label2.Text = $"{CloseSeconds} 秒后自动关闭!";
                }
                else
                {
                    timer1.Stop();
                    this.Hide();
                }
            }
            catch { }
        }

        private void btnEnable_Click(object sender, EventArgs e)
        {
            try
            {
                RunProcess run = MachineCtrl.GetInstance().GetModule(runId);
                if ((null != run) && run.CheckParameter($"CavityEnable{cavityId}", IsEnable))
                {
                    run.WriteParameter(run.RunModule, $"CavityEnable{cavityId}", (!IsEnable).ToString());
                    run.ReadParameter();
                }
            }
            catch (Exception ex)
            {
                if (IsEnable)
                {
                    MessageBox.Show($"关闭干燥炉{ovenId}-{cavityId + 1}层使能失败：" + ex.Message);
                }
                else
                {
                    MessageBox.Show($"开启干燥炉{ovenId}-{cavityId + 1}层使能失败：" + ex.Message);
                }
            }
            this.Hide();
        }

        private void btnPressure_Click(object sender, EventArgs e)
        {
            try
            {
                RunProcess run = MachineCtrl.GetInstance().GetModule(runId);
                if ((null != run) && run.CheckParameter($"CavityPressure{cavityId}", IsPressure))
                {
                    run.WriteParameter(run.RunModule, $"CavityPressure{cavityId}", (!IsPressure).ToString());
                    run.ReadParameter();
                }
            }
            catch (Exception ex)
            {
                if (IsPressure)
                {
                    MessageBox.Show($"关闭干燥炉{ovenId}-{cavityId + 1}层保压失败：" + ex.Message);
                }
                else
                {
                    MessageBox.Show($"开启干燥炉{ovenId}-{cavityId + 1}层保压失败：" + ex.Message);
                }
            }
            this.Hide();
        }

        #region // 窗体移动

        private Point mouseOff;//鼠标移动位置变量
        private bool leftFlag;//标签是否为左键

        private void OvenBntPage_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseOff = new Point(-e.X, -e.Y); //得到变量的值
                leftFlag = true;                  //点击左键按下时标注为true;
            }
        }

        private void OvenBntPage_MouseMove(object sender, MouseEventArgs e)
        {
            if (leftFlag)
            {
                Point mouseSet = Control.MousePosition;
                mouseSet.Offset(mouseOff.X, mouseOff.Y);  //设置移动后的位置
                Location = mouseSet;
            }
        }

        private void OvenBntPage_MouseUp(object sender, MouseEventArgs e)
        {
            if (leftFlag)
            {
                leftFlag = false;//释放鼠标后标注为false;
            }
        }

        #endregion 窗体移动
    }
}
