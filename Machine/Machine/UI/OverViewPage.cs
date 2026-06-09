using HelperLibrary;
using Machine.Framework.Mes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SystemControlLibrary;

namespace Machine
{
    public partial class OverViewPage : FormEx
    {
        OvenBntPage ovenBntPage = new OvenBntPage();

        public OverViewPage()
        {
            InitializeComponent();

            CreateTotalDataView();
        }

        #region // 字段

        /// <summary>
        /// 界面更新定时器
        /// </summary>
        private System.Timers.Timer timerUpdata;

        TipDlg tip;
        bool tipShow;
        Point lastMovePos;
        DateTime lastMoveTime;
        bool updating;

        // MES信息
        Rectangle rectMesInfo;
        //MES入站校验
        Rectangle rectMesCheck;
        // MES心跳在线
        Rectangle rectConveyerLineEN;
        // 清尾料
        Rectangle rectDevicestatusEN;
        // 上料
        Rectangle rectOnloadLine;
        Rectangle rectOnloadRecv;
        Rectangle rectOnloadScan;
        Rectangle[] rectOnloadRbtPlt;
        Rectangle rectOnloadRbtFinger;
        Rectangle rectOnloadRbtBuffer;
        Rectangle rectOnloadNG;
        Rectangle rectOnloadFake;
        Rectangle rectOnloadDetect;
        Rectangle rectManualOperate;
        Rectangle[] rectPltBufferPlt;

        // 调度
        Rectangle rectTransfer;

        // 下料
        Rectangle[] rectOffloadBatPlt;
        /// <summary>
        /// 下料夹具区域
        /// </summary>
        Rectangle rectOffloadBatFinger;
        /// <summary>
        /// 下料暂存区域
        /// </summary>
        Rectangle rectOffloadBatBuffer;
        /// <summary>
        /// 下料NG区域
        /// </summary>
        Rectangle rectOffloadNG;
        /// <summary>
        /// 下料待测假电池区域
        /// </summary>
        Rectangle rectOffloadDetect;
        /// <summary>
        /// 下料线区域
        /// </summary>
        Rectangle rectOffloadLine;
        /// <summary>
        /// 冷却系统区域
        /// </summary>
        Rectangle rectCoolingSystem;
        /// <summary>
        /// 冷却下料夹爪区域
        /// </summary>
        Rectangle rectCoolingFinger;
        /// <summary>
        /// 冷却下料暂存区域
        /// </summary>
        Rectangle rectCoolingBuffer;
        /// <summary>
        /// 下料线缓存区域
        /// </summary>
        Rectangle[] rectOfflineBuffer;
        /// <summary>
        /// 下料线出料区域
        /// </summary>
        Rectangle rectOffloadOut;

        // 干燥炉：炉子,腔体/夹具
        Rectangle[,] rectDryOvenCavity;
        Rectangle[,] rectDryOvenPlt;

        #endregion

        #region // 加载及销毁窗体

        /// <summary>
        /// 加载窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OverViewPage_Load(object sender, EventArgs e)
        {
            this.lastMovePos = new Point(0, 0);
            this.lastMoveTime = DateTime.Now;
            this.updating = false;

            InitModuleRectangle();

            this.labelView.Text = "";

            // 开启定时器
            this.timerUpdata = new System.Timers.Timer();
            this.timerUpdata.Elapsed += UpdataOverViewPage;
            this.timerUpdata.Interval = 500;         // 间隔时间
            this.timerUpdata.AutoReset = true;       // 设置是执行一次（false）还是一直执行(true)；
            this.timerUpdata.Start();                // 开始执行定时器
        }

        /// <summary>
        /// 销毁自定义非托管资源
        /// </summary>
        public override void DisposeForm()
        {
            // 关闭定时器
            if (null != this.timerUpdata)
            {
                this.timerUpdata.Stop();
                while (this.updating)
                {
                    System.Threading.Thread.Sleep(1);
                }
            }
        }

        /// <summary>
        /// 解决窗体绘图时闪烁
        /// </summary>
        /// <param name="e">System.Windows.Forms.CreateParams，包含创建控件的句柄时所需的创建参数。</param>
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
            /// <param name="e"></param>
        }

        /// <summary>
        /// 界面隐藏时停止绘图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OverViewPage_VisibleChanged(object sender, EventArgs e)
        {
        }

        #endregion

        #region // 重绘

        /// <summary>
        /// 触发重绘，使其更新界面动画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdataOverViewPage(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.labelView.Invalidate();
            if ((DateTime.Now - this.lastMoveTime).TotalSeconds >= 10)
            {
                try
                {
                    if (this.tipShow)
                    {
                        this.Invoke(new Action(() => { this.tip.Close(); }));
                        this.tipShow = false;
                    }
                }
                catch (System.Exception ex)
                {
                    Def.WriteLog("OverViewPage", "TipDlg is invalid: " + ex.Message);
                }
            }
            UpdataTotalData();
        }

        /// <summary>
        /// 重绘事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelView_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // 画刷，绘笔
            SolidBrush sBrush = new SolidBrush(Color.Transparent);
            Pen pen = new Pen(Color.Black, 1);

            // 分割标准
            Rectangle rcFrame = e.ClipRectangle;
            rcFrame.Inflate(-10, -10);
            //g.DrawRectangle(pen, rcFrame);
            double frameWidth = rcFrame.Width / 100.0;
            double frameHight = rcFrame.Height / 100.0;

            Rectangle rcArea;

            // MES信息
            rcArea = new Rectangle((int)(rcFrame.X), (int)(rcFrame.Y), (int)(frameWidth * 37.5), (int)(frameHight * 5.0));
            DrawMesInfo(g, pen, rcArea);

            //MES入站校验
            //rcArea = new Rectangle((int)(rcFrame.X + (int)(frameWidth * 10)), (int)(rcFrame.Y), (int)(frameWidth * 99.0), (int)(frameHight * 5.0));
            //DrawMesCheck(g, pen, rcArea);

            //联机上料
            //rcArea = new Rectangle((int)(rcFrame.X + (int)(frameWidth * 22)), (int)(rcFrame.Y), (int)(frameWidth * 99.0), (int)(frameHight * 5.0));
            //DrawConveyerLineEN(g, pen, rcArea);

            // MES校验
            rcArea = new Rectangle((int)(rcFrame.X + (int)(frameWidth * 34)), (int)(rcFrame.Y), (int)(frameWidth * 99.0), (int)(frameHight * 5.0));
            DrawDevicestatusEN(g, pen, rcArea);

            // 人工操作台
            rcArea = new Rectangle((int)(rcFrame.X + frameWidth * 27.5), (int)(rcFrame.Y + frameHight * 90.0), (int)(frameWidth * 10.0), (int)(frameHight * 10.0));
            DrawManualOperate(g, pen, rcArea);

            // 夹具缓存架
            rcArea = new Rectangle((int)(rcFrame.X + frameWidth * 27.5), (int)(rcFrame.Y + frameHight * 60.0), (int)(frameWidth * 10.0), (int)(frameHight * 30.0));
            DrawPalletBuffer(g, pen, rcArea);

            // 干燥炉组1
            rcArea = new Rectangle((int)(rcFrame.X), (int)(rcFrame.Y + frameHight * 5.0), (int)(frameWidth * 87.0), (int)(frameHight * 40.0));
            g.DrawRectangle(pen, rcArea);
            DrawOvenGroup0(g, pen, rcArea);

            // 调度
            rcArea = new Rectangle((int)(rcFrame.X + frameWidth), (int)(rcFrame.Y + frameHight * 46.0), (int)(frameWidth * 86), (int)(frameHight * 10));
            DrawTransfer(g, pen, rcArea);

            // 上料区
            rcArea = new Rectangle((rcFrame.X), (int)(rcFrame.Y + (frameHight * 60.0)), (int)(frameWidth * 28.0), (int)(frameHight * 40.0));
            g.DrawRectangle(pen, rcArea);
            DrawOnload(g, pen, rcArea);

            // 干燥炉组0
            rcArea = new Rectangle((int)(rcFrame.X + frameWidth * 37.0), (int)(rcFrame.Y + frameHight * 60.0), (int)(frameWidth * 50.0), (int)(frameHight * 40.0));
            g.DrawRectangle(pen, rcArea);
            DrawOvenGroup1(g, pen, rcArea);

            // 下料区
            rcArea = new Rectangle((int)(rcFrame.X + (frameWidth * 88.0)), (int)(rcFrame.Y + frameHight * 60.0), (int)(frameWidth * 12), (int)(frameHight * 40.0));
            g.DrawRectangle(pen, rcArea);
            DrawOffLoad(g, pen, rcArea);

            // 冷却系统
            rcArea = new Rectangle((int)(rcFrame.X + (frameWidth * 88.0)), (int)(rcFrame.Y + frameHight * 20.0), (int)(frameWidth * 12), (int)(frameHight * 36.0));
            //g.DrawRectangle(pen, rcArea);
            DrawCoolingSystem(g, pen, rcArea, false, false, false);

            // 冷却下料
            rcArea = new Rectangle((int)(rcFrame.X + (frameWidth * 88.0)), (int)(rcFrame.Y + frameHight * 5.0), (int)(frameWidth * 12), (int)(frameHight * 12.0));
            g.DrawRectangle(pen, rcArea);
            DrawCoolingOffload(g, pen, rcArea);

        }

        #endregion

        #region // 鼠标事件

        /// <summary>
        /// 鼠标提示事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelView_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.tipShow)
            {
                this.Invoke(new Action(() => { this.tip.Close(); }));
                this.tipShow = false;
            }
            else
            {
                Point pt = this.labelView.PointToClient(MousePosition);
                if (ShowMesInfo(pt))
                {
                    return;
                }
                if (ShowMesCheck(pt))
                {
                    return;
                }
                //if (ShowConveyerLineEN(pt))
                //{
                //    return;
                //}
                else if (ShowOnload(pt))
                {
                    return;
                }
                else if (ShowManualOperate(pt))
                {
                    return;
                }
                else if (ShowOven(pt))
                {
                    return;
                }
                else if (ShowPalletBuffer(pt))
                {
                    return;
                }
                else if (ShowTransfer(pt))
                {
                    return;
                }
                else if (ShowOffLoad(pt))
                {
                    return;
                }
                else if (ShowDevicestatusEN(pt))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 鼠标移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Location != this.lastMovePos)
            {
                this.lastMovePos = e.Location;
                this.lastMoveTime = DateTime.Now;

                //labelView_MouseHover(sender, e);
            }
        }

        private void labelView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            MCState state = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            if (MCState.MCInitComplete == state
                || MCState.MCRunning == state
                || MCState.MCStopRun == state)
            {
                Point pt = this.labelView.PointToClient(MousePosition);
                RunID runId = RunID.Invalid;
                int cavityId = 0;

                if ((MachineCtrl.GetInstance().MachineID == 1 || MachineCtrl.GetInstance().MachineID == 3) && GetOvenId(pt, ref runId, ref cavityId))
                {
                    ovenBntPage.TopMost = true;
                    ovenBntPage.ReflashPage(runId, cavityId);
                    ovenBntPage.Show();
                }

                if (MachineCtrl.GetInstance().MachineID > 1)
                {

                }
            }
        }

        #endregion

        #region // 绘制模组区域

        /// <summary>
        /// MES信息
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawMesInfo(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);

            // MES在线状态
            bool updata = MachineCtrl.GetInstance().UpdataMes;
            Rectangle rc = new Rectangle((int)(rect.X + (hig * 5)), (int)(rect.Y + (hig * 1)), (int)(hig * 80), (int)(hig * 80));
            DrawRect(g, (new Pen((updata ? Brushes.DarkGreen : Brushes.Red))), rc, (updata ? Brushes.DarkGreen : Brushes.Red));
            g.DrawString((updata ? "在线生产" : "离线生产"), font, (updata ? Brushes.DarkGreen : Brushes.Red), (int)(rc.Right + (wid)), (int)(rect.Y + hig * 18.0));

            //颜色示例区_电池
            g.DrawString("电池：", font, Brushes.Black, (int)(rc.Right + (wid * 121)), (int)(rect.Y + hig * -30));
            Rectangle rc0 = new Rectangle((int)(rc.Right + (wid * 130)), (int)(rect.Y + hig * -30), 20, 10);
            DrawRect(g, pen, rc0, Brushes.Transparent);
            g.DrawString(":无电池", font, Brushes.Black, (int)(rc.Right + (wid * 135)), (int)(rect.Y + hig * -30));
            Rectangle rc1 = new Rectangle((int)(rc.Right + (wid * 147)), (int)(rect.Y + hig * -30), 20, 10);
            DrawRect(g, pen, rc1, Brushes.DarkGreen);
            g.DrawString(":正常电池", font, Brushes.Black, (int)(rc.Right + (wid * 152)), (int)(rect.Y + hig * -30));
            Rectangle rc2 = new Rectangle((int)(rc.Right + (wid * 167)), (int)(rect.Y + hig * -30), 20, 10);
            DrawRect(g, pen, rc2, Brushes.Red);
            g.DrawString(":NG电池", font, Brushes.Black, (int)(rc.Right + (wid * 172)), (int)(rect.Y + hig * -30));
            Rectangle rc3 = new Rectangle((int)(rc.Right + (wid * 184)), (int)(rect.Y + hig * -30), 20, 10);
            DrawRect(g, pen, rc3, Brushes.Blue);
            g.DrawString(":假电池", font, Brushes.Black, (int)(rc.Right + (wid * 189)), (int)(rect.Y + hig * -30));

            //颜色示例区_夹具
            g.DrawString("夹具：", font, Brushes.Black, (int)(rc.Right + (wid * 121)), (int)(rect.Y + hig * 15));
            Rectangle rc4 = new Rectangle((int)(rc.Right + (wid * 130)), (int)(rect.Y + hig * 15), 20, 10);
            DrawRect(g, pen, rc4, Brushes.Transparent);
            g.DrawString(":正常", font, Brushes.Black, (int)(rc.Right + (wid * 135)), (int)(rect.Y + hig * 15));
            Rectangle rc5 = new Rectangle((int)(rc.Right + (wid * 147)), (int)(rect.Y + hig * 15), 20, 10);
            DrawRect(g, pen, rc5, Brushes.DarkGreen);
            g.DrawString(":上料完成", font, Brushes.Black, (int)(rc.Right + (wid * 152)), (int)(rect.Y + hig * 15));
            Rectangle rc6 = new Rectangle((int)(rc.Right + (wid * 167)), (int)(rect.Y + hig * 15), 20, 10);
            DrawRect(g, pen, rc6, Brushes.DarkRed);
            g.DrawString(":NG夹具", font, Brushes.Black, (int)(rc.Right + (wid * 172)), (int)(rect.Y + hig * 15));
            Rectangle rc7 = new Rectangle((int)(rc.Right + (wid * 184)), (int)(rect.Y + hig * 15), 20, 10);
            DrawRect(g, pen, rc7, Brushes.DarkGoldenrod);
            g.DrawString(":待下料", font, Brushes.Black, (int)(rc.Right + (wid * 189)), (int)(rect.Y + hig * 15));
            Rectangle rc8 = new Rectangle((int)(rc.Right + (wid * 203)), (int)(rect.Y + hig * 15), 20, 10);
            DrawRect(g, pen, rc8, Brushes.Magenta);
            g.DrawString(":假电池回炉", font, Brushes.Black, (int)(rc.Right + (wid * 208)), (int)(rect.Y + hig * 15));
            //Rectangle rc9 = new Rectangle((int)(rc.Right + (wid * 68)), (int)(rect.Y + hig * 15), 20, 10);
            //DrawRect(g, pen, rc9, Brushes.Yellow);
            //g.DrawString(":待检测或等待结果", font, Brushes.Black, (int)(rc.Right + (wid * 70)), (int)(rect.Y + hig * 15));

            //颜色示例区_腔体
            g.DrawString("腔体：", font, Brushes.Black, (int)(rc.Right + (wid * 121)), (int)(rect.Y + hig * 60));
            Rectangle rc10 = new Rectangle((int)(rc.Right + (wid * 130)), (int)(rect.Y + hig * 60), 20, 10);
            DrawRect(g, pen, rc10, Brushes.Transparent);
            g.DrawString(":正常", font, Brushes.Black, (int)(rc.Right + (wid * 135)), (int)(rect.Y + hig * 60));
            Rectangle rc11 = new Rectangle((int)(rc.Right + (wid * 147)), (int)(rect.Y + hig * 60), 20, 10);
            DrawRect(g, pen, rc11, Brushes.Yellow);
            g.DrawString(":加热状态", font, Brushes.Black, (int)(rc.Right + (wid * 152)), (int)(rect.Y + hig * 60));
            Rectangle rc12 = new Rectangle((int)(rc.Right + (wid * 167)), (int)(rect.Y + hig * 60), 20, 10);
            DrawRect(g, pen, rc12, Brushes.Cyan);
            g.DrawString(":等待测试", font, Brushes.Black, (int)(rc.Right + (wid * 172)), (int)(rect.Y + hig * 60));
            Rectangle rc13 = new Rectangle((int)(rc.Right + (wid * 187)), (int)(rect.Y + hig * 60), 20, 10);
            DrawRect(g, pen, rc13, new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.DarkCyan));
            g.DrawString(":等待结果", font, Brushes.Black, (int)(rc.Right + (wid * 192)), (int)(rect.Y + hig * 60));
            Rectangle rc14 = new Rectangle((int)(rc.Right + (wid * 207)), (int)(rect.Y + hig * 60), 20, 10);
            DrawRect(g, pen, rc14, new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.Magenta));
            g.DrawString(":等待回炉", font, Brushes.Black, (int)(rc.Right + (wid * 212)), (int)(rect.Y + hig * 60));
            Rectangle rc15 = new Rectangle((int)(rc.Right + (wid * 227)), (int)(rect.Y + hig * 60), 20, 10);
            DrawRect(g, pen, rc15, new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.Black));
            g.DrawString(":维修状态", font, Brushes.Black, (int)(rc.Right + (wid * 232)), (int)(rect.Y + hig * 60));

            g.DrawString(($"登录的员工：{MesResources.Equipment.OperatorUserID}"), font, Brushes.Black, (int)(rc.Right + (wid * 20)), (int)(rect.Y + hig * 18.0));



            rc.Width = (int)(rc.Right + wid * 10 - rc.Left);
            this.rectMesInfo = rc;
        }
        /// <summary>
        /// MES入站校验
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawMesCheck(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);

            // MES在线状态
            bool mescheck = MachineCtrl.GetInstance().MesCheck;
            Rectangle rc = new Rectangle((int)(rect.X + (hig * 5)), (int)(rect.Y + (hig * 1)), (int)(hig * 80), (int)(hig * 80));
            DrawRect(g, (new Pen((mescheck ? Brushes.DarkGreen : Brushes.Red))), rc, (mescheck ? Brushes.DarkGreen : Brushes.Red));
            g.DrawString((mescheck ? "MES校验" : "MES不校验"), font, (mescheck ? Brushes.DarkGreen : Brushes.Red), (int)(rc.Right + (wid)), (int)(rect.Y + hig * 18.0));


            rc.Width = (int)(rc.Right + wid * 10 - rc.Left);
            this.rectMesCheck = rc;

        }

        /// <summary>
        /// MES心跳在线
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawConveyerLineEN(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);

            // MES在线状态
            bool mesConnect = MachineCtrl.GetInstance().IsMESConnect;
            Rectangle rc = new Rectangle((int)(rect.X + (hig * 5)), (int)(rect.Y + (hig * 1)), (int)(hig * 80), (int)(hig * 80));
            DrawRect(g, (new Pen((mesConnect ? Brushes.DarkGreen : Brushes.Red))), rc, (mesConnect ? Brushes.DarkGreen : Brushes.Red));
            g.DrawString((mesConnect ? "MES有心跳" : "MES无心跳"), font, (mesConnect ? Brushes.DarkGreen : Brushes.Red), (int)(rc.Right + (wid)), (int)(rect.Y + hig * 18.0));

            rc.Width = (int)(rc.Right + wid * 10 - rc.Left);
            this.rectConveyerLineEN = rc;

        }

        /// <summary>
        /// 设备状态变更
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawDevicestatusEN(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);

            // MES在线状态
            bool devicestatus = MachineCtrl.GetInstance().Devicestatus;
            Rectangle rc = new Rectangle((int)(rect.X + (hig * 5)), (int)(rect.Y + (hig * 1)), (int)(hig * 80), (int)(hig * 80));
            DrawRect(g, (new Pen((devicestatus ? Brushes.DarkGreen : Brushes.Red))), rc, (devicestatus ? Brushes.DarkGreen : Brushes.DarkBlue));
            g.DrawString((devicestatus ? @"MES任务状态" + "\r\n" + "(E：关闭)" : @"MES任务状态" + "\r\n" + "(U：中断)"), font, (devicestatus ? Brushes.DarkGreen : Brushes.Red), (int)(rc.Right + (wid)), (int)(rect.Y + hig * 18.0));
            //g.DrawString((devicestatus ? @"MES校验" : @"MES不校验"), font, (devicestatus ? Brushes.DarkGreen : Brushes.Red), (int)(rc.Right + (wid)), (int)(rect.Y + hig * 18.0));
            rc.Width = (int)(rc.Right + wid * 10 - rc.Left);
            this.rectDevicestatusEN = rc;
        }

        /// <summary>
        /// 上料区
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rect"></param>
        private void DrawOnload(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            Battery[] arrBattery = null;
            RunID runId = RunID.Invalid;

            // 上料夹具
            runId = RunID.OnloadRobot;
            arrPallet = ModulePallet(runId);
            if (null != arrPallet)
            {
                for (int i = 0; i < arrPallet.Length; i++)
                {
                    int div = (int)(wid * 2 * i + wid * 31 * i);
                    g.DrawString(("夹具" + (i + 1)), font, Brushes.Black, (rect.X+100 + (int)(wid * 8 + div)), (rect.Y + hig * 56));
                    Rectangle rc = new Rectangle((rect.X + (int)(wid + div)+100), (int)(rect.Y + (hig * 3)), (int)(wid * 31), (int)(hig * 52));
                    //DrawPallet(g, pen, rc, arrPallet[i], false, true, true);
                    DrawPallet(g, pen, rc, arrPallet[i], false, false, false);
                    this.rectOnloadRbtPlt[i] = rc;

                    // 夹具位置使能
                    if (!GetPalletPosEnable(runId, i))
                    {
                        Point[] point = new Point[4];
                        point[0] = new Point(rc.X, rc.Y);
                        point[1] = new Point(rc.X + rc.Width, rc.Y + rc.Height);
                        point[2] = new Point(rc.X, rc.Y + rc.Height);
                        point[3] = new Point(rc.X + rc.Width, rc.Y);
                        g.DrawLines(pen, point);
                    }
                }
                RunProcessOnloadRobot run = MachineCtrl.GetInstance().GetModule(runId) as RunProcessOnloadRobot;
                if ((null != run) && (run.OnloadClear))
                {
                    g.DrawString("上料清尾料中...", font, Brushes.Red, (rect.X + 10), (rect.Y + rect.Height / 3));
                }
            }
            // 机器人抓手及暂存
            runId = RunID.OnloadRobot;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                // 抓手 - 暂存
                string[] info = new string[] { "抓手", "暂存" };
                for (int i = 0; i < 2; i++)
                {
                    int div = (int)(wid * 5 * i + wid * 7 * i);
                    g.DrawString(info[i], font, Brushes.Black, (rect.X + wid * 25 + div), (rect.Y + hig * 93));
                    Rectangle rc = new Rectangle((int)(rect.X + (wid * 25 + div)), (int)(rect.Y + (hig * 63)), (int)(wid * 8), (int)(hig * 30));
                    Battery[] arrBat = new Battery[arrBattery.Length / 2];
                    for (int idx = 0; idx < arrBattery.Length / 2; idx++)
                    {
                        arrBat[idx] = arrBattery[(i * arrBattery.Length / 2) + idx];
                    }
                    DrawBattery(g, pen, rc, arrBat, true, true, false);
                    if (0 == i)
                    {
                        this.rectOnloadRbtFinger = rc;
                    }
                    else
                    {
                        this.rectOnloadRbtBuffer = rc;
                    }
                }
            }
            //RobotActionInfo rbtAction = GetRobotActionInfo(runId, true);
            //if(null != rbtAction)
            //{
            //    Rectangle rc = new Rectangle((rect.X ), (int)(rect.Y - 20), (int)(wid * 6), (int)(wid * 6));
            //    bool con = GetDeviceIsConnect(runId);
            //    DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rc, (con ? Brushes.DarkGreen : Brushes.Red));
            //    string rbtInfo = string.Format("{0}:{1}-{2}行-{3}列-{4}", ModuleName(runId)
            //        , rbtAction.stationName, (rbtAction.row + 1), (rbtAction.col + 1), RobotDef.RobotOrderName[(int)rbtAction.order]);
            //    g.DrawString(rbtInfo, (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (rect.X + wid * 7), (rect.Y - 20));
            //}

            // 来料物流框
            runId = RunID.OnloadScan;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                //for (int i = 0; i < arrPallet.Length; i++)
                //{
                //    int div = (int)(wid * 2 * i + wid * 31 * i);
                //    g.DrawString(("夹具" + (i + 1)), font, Brushes.Black, (rect.X + 100 + (int)(wid * 8 + div)), (rect.Y + hig * 56));
                //    Rectangle rc = new Rectangle((rect.X + (int)(wid + div) + 100), (int)(rect.Y + (hig * 3)), (int)(wid * 31), (int)(hig * 52));
                //    //DrawPallet(g, pen, rc, arrPallet[i], false, true, true);
                //    DrawPallet(g, pen, rc, arrPallet[i], false, false, false);
                //    this.rectOnloadRbtPlt[i] = rc;
                //}
                
                string[] info = new string[] { "1行", "2行" };
                for (int i = 0; i < 2; i++)
                {
                    if (!Def.IsNoHardware())
                    {
                        int div = (int)(wid * 5 * i + wid * 7 * i);
                        g.DrawString(info[i], font, Brushes.Black, (rect.X + wid * 25 + div - 90), (rect.Y + hig * 93 - 135));
                        Rectangle rc = new Rectangle((int)(rect.X + (wid * 25 + div) - 100), (int)(rect.Y + (hig * 63) - 217), (int)(wid * 12), (int)(hig * 30 + 82));
                        Battery[] arrBat = new Battery[arrBattery.Length / 2];
                        for (int idx = 0; idx < arrBattery.Length / 2; idx++)
                        {
                            arrBat[idx] = arrBattery[(i * arrBattery.Length / 2) + idx];
                        }
                        DrawBattery(g, pen, rc, arrBat, true, true, false);
                        //if (0 == i)
                        //{
                        //    this.rectOnloadRbtFinger = rc;
                        //}
                        //else
                        //{
                        //    this.rectOnloadRbtBuffer = rc;
                        //}
                    }
                    else
                    {
                        int div = (int)(wid * 5 * i + wid * 7 * i);
                        g.DrawString(info[i], font, Brushes.Black, (rect.X + wid * 25 + div - 70), (rect.Y + hig * 93 - 100));
                        Rectangle rc = new Rectangle((int)(rect.X + (wid * 25 + div) - 70), (int)(rect.Y + (hig * 63) - 165), (int)(wid * 12), (int)(hig * 30 + 70));
                        Battery[] arrBat = new Battery[arrBattery.Length / 2];
                        for (int idx = 0; idx < arrBattery.Length / 2; idx++)
                        {
                            arrBat[idx] = arrBattery[(i * arrBattery.Length / 2) + idx];
                        }
                        DrawBattery(g, pen, rc, arrBat, true, true, false);
                        //if (0 == i)
                        //{
                        //    this.rectOnloadRbtFinger = rc;
                        //}
                        //else
                        //{
                        //    this.rectOnloadRbtBuffer = rc;
                        //}
                    }
                }
            }

            //// 来料收电池
            //runId = RunID.OnloadRecv;
            //arrBattery = ModuleBattery(runId);
            //if (null != arrBattery)
            //{
            //    g.DrawString("收", font, Brushes.Black, (rect.X + wid), (rect.Y + hig * 93));
            //    // 接收位
            //    Rectangle rc = new Rectangle((int)(rect.X + (wid)), (int)(rect.Y + (hig * 63)), (int)(wid * 8), (int)(hig * 30));
            //    DrawBattery(g, pen, rc, arrBattery, true, true, false);
            //    this.rectOnloadRecv = rc;
            //}
            //// 扫码位
            //runId = RunID.OnloadScan;
            //arrBattery = ModuleBattery(runId);
            //if (null != arrBattery)
            //{
            //    g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid), (rect.Y + hig * 93));
            //    // 接收位
            //    Rectangle rc = new Rectangle((int)(rect.X + (wid)), (int)(rect.Y + (hig * 63)), (int)(wid * 8), (int)(hig * 30));
            //    DrawBattery(g, pen, rc, arrBattery, true, true, false);
            //    this.rectOnloadScan = rc;
            //}
            //// 取料线
            //runId = RunID.OnloadLine;
            //arrBattery = ModuleBattery(runId);
            //if (null != arrBattery)
            //{
            //    g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 13), (rect.Y + hig * 93));
            //    // 取料位
            //    Rectangle rc = new Rectangle((int)(rect.X + (wid * 13)), (int)(rect.Y + (hig * 63)), (int)(wid * 8), (int)(hig * 30));
            //    DrawBattery(g, pen, rc, arrBattery, true, true, false);
            //    this.rectOnloadLine = rc;
            //}
            // NG输出
            runId = RunID.OnloadNG;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                //g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 48), (rect.Y + hig * 93));
                //Rectangle rc = new Rectangle((rect.X + (int)(wid * 48)), (rect.Y + (int)(hig * 63)), (int)(wid * 15), (int)(hig * 30));
                //DrawBattery(g, pen, rc, arrBattery, false, true, false);
                //this.rectOnloadNG = rc;
                int arrLen = arrBattery.Length;
                Battery[] arrBattery2 = new Battery[arrLen];
                for (int i = 0; i < arrLen; i++)
                {
                    arrBattery2[i] = new Battery();
                    arrBattery2[i].Copy(arrBattery[arrLen - 1 - i]);
                }
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 48), (rect.Y + hig * 93));
                Rectangle rc = new Rectangle((rect.X + (int)(wid * 48)), (rect.Y + (int)(hig * 63)), (int)(wid * 15), (int)(hig * 30));
                //DrawBattery(g, pen, rc, arrBattery2, false, true, false);
                DrawBattery2(g, pen, rc, arrBattery2, false, true, false);
                this.rectOnloadNG = rc;
            }
            // 假电池输入
            runId = RunID.OnloadFake;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 66), (rect.Y + hig * 93));
                int rowNum = 4;
                for (int i = 0; i < arrBattery.Length / rowNum; i++)
                {
                    Rectangle rc = new Rectangle((rect.X + (int)(wid * 66)), (int)(rect.Y + (hig * 63 + hig * 7.5 * i)), (int)(wid * 23), (int)(hig * 7.5));
                    Battery[] arrBat = new Battery[rowNum];
                    for (int j = 0; j < rowNum; j++)
                    {
                        arrBat[j] = arrBattery[i * rowNum + j];
                    }
                    DrawBattery(g, pen, rc, arrBat, false, true, false);
                }
                this.rectOnloadFake = new Rectangle((rect.X + (int)(wid * 66)), (int)(rect.Y + (hig * 63)), (int)(wid * 25), (int)(hig * 7.5 * arrBattery.Length / rowNum));
            }
            //// 待测假电池
            //runId = RunID.OnloadDetect;
            //arrBattery = ModuleBattery(runId);
            //if(null != arrBattery)
            //{
            //    g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 90), (rect.Y + hig * 93));
            //    // 取料位
            //    Rectangle rc = new Rectangle((int)(rect.X + (wid * 90)), (int)(rect.Y + (hig * 63)), (int)(wid * 8), (int)(hig * 30));
            //    DrawBattery(g, pen, rc, arrBattery, true, true, false);
            //    this.rectOnloadDetect = rc;
            //}
        }

        /// <summary>
        /// 人工操作台
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawManualOperate(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            RunID runId = RunID.Invalid;

            // 人工操作台
            runId = RunID.ManualOperate;
            arrPallet = ModulePallet(runId);
            if (null != arrPallet)
            {
                if (arrPallet.Length > 0)
                {
                    g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + (int)(wid * 25)), (rect.Y + hig * 62));
                    Rectangle rc = new Rectangle((int)(rect.X + wid * 11), (rect.Y + (int)(hig * 8)), (int)(wid * 80), (int)(hig * 52));
                    this.rectManualOperate = rc;
                    DrawPalletRect(g, rc, arrPallet[0]);
                }
            }
        }

        /// <summary>
        /// 夹具缓存架
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawPalletBuffer(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            RunID runId = RunID.Invalid;

            // 夹具缓存架
            runId = RunID.PalletBuffer;
            arrPallet = ModulePallet(runId);
            if (null != arrPallet)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (float)(rect.X + (int)(wid * 18)), (rect.Y + hig * 92));
                Rectangle rcPltBuf = new Rectangle((int)(rect.X + (wid * 10.0)), (int)(rect.Y + (hig * 10.0)), (int)(wid * 80), (int)(hig * 80));

                int bufCol = 1;
                int bufRow = (int)ModuleMaxPallet.PalletBuffer / bufCol;
                float rowHig = rcPltBuf.Height / (float)bufRow;
                // 绘制缓存架：从下往上
                for (int rowIdx = 0; rowIdx < bufRow; rowIdx++)
                {
                    Rectangle rcRow = new Rectangle((int)(rcPltBuf.X), (int)(rcPltBuf.Y + rowHig * (bufRow - 1 - rowIdx)), (int)(rcPltBuf.Width), (int)(rowHig));
                    DrawRect(g, pen, rcRow, Brushes.Transparent);

                    // 夹具
                    float pltWid = rcRow.Width / (float)bufCol;
                    for (int pltIdx = 0; pltIdx < bufCol; pltIdx++)
                    {
                        Rectangle rcPlt = new Rectangle((int)(rcRow.X + pltWid / 10.0 * (pltIdx + 1) + pltWid * pltIdx), (int)(rcRow.Y + rcRow.Height / 20.0 * 3)
                            , (int)(pltWid / 10.0 * 7.5), (int)(rcRow.Height / 20.0 * 14.0));
                        DrawPalletRect(g, rcPlt, arrPallet[rowIdx * bufCol + pltIdx], (rowIdx + 1).ToString());
                        this.rectPltBufferPlt[rowIdx * bufCol + pltIdx] = rcPlt;
                    }

                    // 腔体使能
                    if (!GetPalletBufferRowEnable(runId, rowIdx))
                    {
                        Point[] point = new Point[4];
                        point[0] = new Point(rcRow.X, rcRow.Y);
                        point[1] = new Point(rcRow.X + rcRow.Width, rcRow.Y + rcRow.Height);
                        point[2] = new Point(rcRow.X, rcRow.Y + rcRow.Height);
                        point[3] = new Point(rcRow.X + rcRow.Width, rcRow.Y);
                        g.DrawLines(pen, point);
                    }
                }
            }
        }

        /// <summary>
        /// 干燥炉组1：上料区侧的MachineCtrl.HalfDryingOvens数量的干燥炉
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawOvenGroup1(Graphics g, Pen pen, Rectangle rect)
        {
            bool IsChange = false;
            if (IsChange)
            {
                // 模组名字体
                Font font = new Font(this.Font.FontFamily, (float)10.0);

                int halfOven = MachineCtrl.GetInstance().HalfDryingOvens;
                int ovenCount = halfOven;
                float ovenWid = (float)(rect.Width / (ovenCount + 0.6));
                float hig = (float)(rect.Height / 100.0);
                Pallet[] arrPallet = null;
                RunID runId = RunID.Invalid;
                RunID runTmpId = RunID.Invalid;
                int ovenRow = (int)OvenRowCol.MaxRow;
                int ovenCol = (int)OvenRowCol.MaxCol;
                for (int ovenIdx = 0; ovenIdx < ovenCount; ovenIdx++)
                {
                    //runId = RunID.DryOven0 + ovenIdx;
                    runId = RunID.DryOven0 + ((int)OvenInfoCount.OvenCount - halfOven) + ovenIdx;

                    runTmpId = runId - 8; //减8工位

                    arrPallet = ModulePallet(runTmpId);

                    // 干燥炉
                    g.DrawString(ModuleName(runId), font, Brushes.Black, (float)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 5.0)), (rect.Y + hig * 2));
                    Rectangle rcOven = new Rectangle((int)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 3.5)), (int)(rect.Y + hig * 2), (int)(6 * hig), (int)(6 * hig));
                    bool con = GetDeviceIsConnect(runTmpId);
                    DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rcOven, (con ? Brushes.DarkGreen : Brushes.Red));
                    rcOven = new Rectangle((int)(rect.X + (ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 1))), (int)(rect.Y + (hig * 10.0)), (int)(ovenWid), (int)(hig * 80));
                    // 绘制腔体：从下往上
                    float rowHig = rcOven.Height / (float)ovenRow;
                    for (int rowIdx = 0; rowIdx < ovenRow; rowIdx++)
                    {
                        Rectangle rcCavity = new Rectangle((rcOven.X), (int)(rcOven.Y + rowHig * (ovenRow - 1 - rowIdx)), (int)(rcOven.Width), (int)(rowHig));
                        DrawCavity(g, rcCavity, GetOvenCavityTransfer(runTmpId, rowIdx) ? (CavityStatus.Maintenance + 1) : GetCavityState(runTmpId, rowIdx), (rowIdx + 1).ToString());
                        this.rectDryOvenCavity[ovenIdx, rowIdx] = rcCavity;

                        // 腔体中夹具
                        if (null != arrPallet)
                        {
                            float pltWid = rcCavity.Width / (float)ovenCol;
                            for (int pltIdx = 0; pltIdx < ovenCol; pltIdx++)
                            {
                                Rectangle rcPlt = new Rectangle((int)(rcCavity.X + pltWid / 10.0 * (pltIdx + 1) + pltWid * pltIdx), (int)(rcCavity.Y + rcCavity.Height / 20.0 * 3)
                                    , (int)(pltWid / 10.0 * 7.5), (int)(rcCavity.Height / 20.0 * 14.0));
                                DrawPalletRect(g, rcPlt, arrPallet[rowIdx * ovenCol + pltIdx], $"{pltIdx + 1}-{(pltIdx == 0 ? "左" : "右")}");
                                this.rectDryOvenPlt[ovenIdx, rowIdx * ovenCol + pltIdx] = rcPlt;
                            }
                        }

                        // 腔体使能
                        if (!GetOvenCavityEnable(runTmpId, rowIdx))
                        {
                            Point[] point = new Point[4];
                            point[0] = new Point(rcCavity.X, rcCavity.Y);
                            point[1] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y + rcCavity.Height);
                            point[2] = new Point(rcCavity.X, rcCavity.Y + rcCavity.Height);
                            point[3] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y);
                            g.DrawLines(pen, point);
                        }
                        // 腔体保压
                        if (GetOvenCavityPressure(runTmpId, rowIdx))
                        {
                            Point[] point = new Point[4];
                            for (int i = 0; i < 2; i++)
                            {
                                point[i * 2] = new Point(rcCavity.X, (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                                point[i * 2 + 1] = new Point((rcCavity.X + rcCavity.Width), (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                                g.DrawLine(pen, point[i * 2], point[i * 2 + 1]);
                            }
                        }
                    }
                }
            }
            else
            {
                // 模组名字体
                Font font = new Font(this.Font.FontFamily, (float)10.0);

                int halfOven = MachineCtrl.GetInstance().HalfDryingOvens;
                int ovenCount = halfOven;
                float ovenWid = (float)(rect.Width / (ovenCount + 0.6));
                float hig = (float)(rect.Height / 100.0);
                Pallet[] arrPallet = null;
                RunID runId = RunID.Invalid;
                int ovenRow = (int)OvenRowCol.MaxRow;
                int ovenCol = (int)OvenRowCol.MaxCol;
                for (int ovenIdx = 0; ovenIdx < ovenCount; ovenIdx++)
                {
                    runId = RunID.DryOven0 + ovenIdx;

                    arrPallet = ModulePallet(runId);

                    // 干燥炉
                    g.DrawString(ModuleName(runId), font, Brushes.Black, (float)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 5.0)), (rect.Y + hig * 2));
                    Rectangle rcOven = new Rectangle((int)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 3.5)), (int)(rect.Y + hig * 2), (int)(6 * hig), (int)(6 * hig));
                    bool con = GetDeviceIsConnect(runId);
                    DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rcOven, (con ? Brushes.DarkGreen : Brushes.Red));
                    rcOven = new Rectangle((int)(rect.X + (ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 1))), (int)(rect.Y + (hig * 10.0)), (int)(ovenWid), (int)(hig * 80));
                    // 绘制腔体：从下往上
                    float rowHig = rcOven.Height / (float)ovenRow;
                    for (int rowIdx = 0; rowIdx < ovenRow; rowIdx++)
                    {
                        Rectangle rcCavity = new Rectangle((rcOven.X), (int)(rcOven.Y + rowHig * (ovenRow - 1 - rowIdx)), (int)(rcOven.Width), (int)(rowHig));
                        DrawCavity(g, rcCavity, GetOvenCavityTransfer(runId, rowIdx) ? (CavityStatus.Maintenance + 1) : GetCavityState(runId, rowIdx), (rowIdx + 1).ToString());
                        this.rectDryOvenCavity[ovenIdx, rowIdx] = rcCavity;

                        // 腔体中夹具
                        if (null != arrPallet)
                        {
                            float pltWid = rcCavity.Width / (float)ovenCol;
                            for (int pltIdx = 0; pltIdx < ovenCol; pltIdx++)
                            {
                                Rectangle rcPlt = new Rectangle((int)(rcCavity.X + pltWid / 10.0 * (pltIdx + 1) + pltWid * pltIdx), (int)(rcCavity.Y + rcCavity.Height / 20.0 * 3)
                                    , (int)(pltWid / 10.0 * 7.5), (int)(rcCavity.Height / 20.0 * 14.0));
                                //DrawPalletRect(g, rcPlt, arrPallet[rowIdx * ovenCol + pltIdx], (pltIdx + 1).ToString());
                                DrawPalletRect(g, rcPlt, arrPallet[rowIdx * ovenCol + pltIdx], $"{pltIdx + 1}-{(pltIdx == 0 ? "右" : "左")}");
                                this.rectDryOvenPlt[ovenIdx, rowIdx * ovenCol + pltIdx] = rcPlt;
                            }
                        }

                        // 腔体使能
                        if (!GetOvenCavityEnable(runId, rowIdx))
                        {
                            Point[] point = new Point[4];
                            point[0] = new Point(rcCavity.X, rcCavity.Y);
                            point[1] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y + rcCavity.Height);
                            point[2] = new Point(rcCavity.X, rcCavity.Y + rcCavity.Height);
                            point[3] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y);
                            g.DrawLines(pen, point);
                        }
                        // 腔体保压
                        if (GetOvenCavityPressure(runId, rowIdx))
                        {
                            Point[] point = new Point[4];
                            for (int i = 0; i < 2; i++)
                            {
                                point[i * 2] = new Point(rcCavity.X, (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                                point[i * 2 + 1] = new Point((rcCavity.X + rcCavity.Width), (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                                g.DrawLine(pen, point[i * 2], point[i * 2 + 1]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 干燥炉组0：剩余的总数 = 总数 - MachineCtrl.HalfDryingOvens数量的干燥炉
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawOvenGroup0(Graphics g, Pen pen, Rectangle rect)
        {
            bool IsChage = false;
            if (IsChage)
            {
                // 模组名字体
                Font font = new Font(this.Font.FontFamily, (float)10.0);

                int halfOven = MachineCtrl.GetInstance().HalfDryingOvens;
                int ovenCount = (int)OvenInfoCount.OvenCount - halfOven;
                float ovenWid = (float)(rect.Width / (ovenCount + 0.88));
                float hig = (float)(rect.Height / 100.0);
                Pallet[] arrPallet = null;
                RunID runId = RunID.Invalid;
                RunID runTmpId = RunID.Invalid;
                int ovenRow = (int)OvenRowCol.MaxRow;
                int ovenCol = (int)OvenRowCol.MaxCol;
                for (int ovenIdx = 0; ovenIdx < ovenCount; ovenIdx++)
                {
                    //runId = RunID.DryOven0 + halfOven + ovenIdx;
                    runId = RunID.DryOven0 + ovenIdx; //wjj 220418

                    runTmpId = runId + 5; //加5工位
                    arrPallet = ModulePallet(runTmpId);

                    string modName = ModuleName(runId);

                    // 干燥炉
                    g.DrawString(modName, font, Brushes.Black, (float)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 5.0)), (rect.Y + hig * 92));
                    Rectangle rcOven = new Rectangle((int)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 3.5)), (int)(rect.Y + hig * 91.5), (int)(6 * hig), (int)(6 * hig));
                    bool con = GetDeviceIsConnect(runTmpId);
                    DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rcOven, (con ? Brushes.DarkGreen : Brushes.Red));
                    rcOven = new Rectangle((int)(rect.X + (ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 1))), (int)(rect.Y + (hig * 10.0)), (int)(ovenWid), (int)(hig * 80));
                    // 绘制腔体：从下往上
                    float rowHig = rcOven.Height / (float)ovenRow;
                    for (int rowIdx = 0; rowIdx < ovenRow; rowIdx++)
                    {
                        Rectangle rcCavity = new Rectangle((rcOven.X), (int)(rcOven.Y + rowHig * (ovenRow - 1 - rowIdx)), (int)(rcOven.Width), (int)(rowHig));
                        DrawCavity(g, rcCavity, GetOvenCavityTransfer(runTmpId, rowIdx) ? (CavityStatus.Maintenance + 1) : GetCavityState(runTmpId, rowIdx), (rowIdx + 1).ToString());
                        this.rectDryOvenCavity[halfOven + ovenIdx, rowIdx] = rcCavity;

                        // 腔体中夹具
                        if (null != arrPallet)
                        {
                            float pltWid = rcCavity.Width / (float)ovenCol;
                            for (int pltIdx = 0; pltIdx < ovenCol; pltIdx++)
                            {
                                Rectangle rcPlt = new Rectangle((int)(rcCavity.X + pltWid / 10.0 * (pltIdx + 1) + pltWid * pltIdx), (int)(rcCavity.Y + rcCavity.Height / 20.0 * 3)
                                    , (int)(pltWid / 10.0 * 7.5), (int)(rcCavity.Height / 20.0 * 14.0));
                                DrawPalletRect(g, rcPlt, arrPallet[rowIdx * ovenCol + pltIdx], (pltIdx + 1).ToString());
                                this.rectDryOvenPlt[halfOven + ovenIdx, rowIdx * ovenCol + pltIdx] = rcPlt;
                            }
                        }

                        // 腔体使能
                        if (!GetOvenCavityEnable(runTmpId, rowIdx))
                        {
                            Point[] point = new Point[4];
                            point[0] = new Point(rcCavity.X, rcCavity.Y);
                            point[1] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y + rcCavity.Height);
                            point[2] = new Point(rcCavity.X, rcCavity.Y + rcCavity.Height);
                            point[3] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y);
                            g.DrawLines(pen, point);
                        }
                        // 腔体保压
                        if (GetOvenCavityPressure(runTmpId, rowIdx))
                        {
                            Point[] point = new Point[4];
                            for (int i = 0; i < 2; i++)
                            {
                                point[i * 2] = new Point(rcCavity.X, (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                                point[i * 2 + 1] = new Point((rcCavity.X + rcCavity.Width), (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                                g.DrawLine(pen, point[i * 2], point[i * 2 + 1]);
                            }
                        }
                    }
                }
            }
            else
            {
                // 模组名字体
                Font font = new Font(this.Font.FontFamily, (float)10.0);

                int halfOven = MachineCtrl.GetInstance().HalfDryingOvens;
                int ovenCount = (int)OvenInfoCount.OvenCount - halfOven;
                float ovenWid = (float)(rect.Width / (ovenCount + 0.88));
                float hig = (float)(rect.Height / 100.0);
                Pallet[] arrPallet = null;
                RunID runId = RunID.Invalid;
                int ovenRow = (int)OvenRowCol.MaxRow;
                int ovenCol = (int)OvenRowCol.MaxCol;
                for (int ovenIdx = 0; ovenIdx < ovenCount; ovenIdx++)
                {
                    runId = RunID.DryOven0 + halfOven + ovenIdx;

                    arrPallet = ModulePallet(runId);

                    string modName = ModuleName(runId);

                    // 干燥炉
                    g.DrawString(modName, font, Brushes.Black, (float)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 5.0)), (rect.Y + hig * 92));
                    Rectangle rcOven = new Rectangle((int)(rect.X + ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 3.5)), (int)(rect.Y + hig * 91.5), (int)(6 * hig), (int)(6 * hig));
                    bool con = GetDeviceIsConnect(runId);
                    DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rcOven, (con ? Brushes.DarkGreen : Brushes.Red));
                    rcOven = new Rectangle((int)(rect.X + (ovenWid * ovenIdx + ovenWid / 10.0 * (ovenIdx + 1))), (int)(rect.Y + (hig * 10.0)), (int)(ovenWid), (int)(hig * 80));
                    // 绘制腔体：从下往上
                    float rowHig = rcOven.Height / (float)ovenRow;
                    for (int rowIdx = 0; rowIdx < ovenRow; rowIdx++)
                    {
                        Rectangle rcCavity = new Rectangle((rcOven.X), (int)(rcOven.Y + rowHig * (ovenRow - 1 - rowIdx)), (int)(rcOven.Width), (int)(rowHig));
                        DrawCavity(g, rcCavity, GetOvenCavityTransfer(runId, rowIdx) ? (CavityStatus.Maintenance + 1) : GetCavityState(runId, rowIdx), (rowIdx + 1).ToString());
                        this.rectDryOvenCavity[halfOven + ovenIdx, rowIdx] = rcCavity;

                        // 腔体中夹具
                        if (null != arrPallet)
                        {
                            float pltWid = rcCavity.Width / (float)ovenCol;
                            for (int pltIdx = 0; pltIdx < ovenCol; pltIdx++)
                            {
                                Rectangle rcPlt = new Rectangle((int)(rcCavity.X + pltWid / 10.0 * (pltIdx + 1) + pltWid * pltIdx), (int)(rcCavity.Y + rcCavity.Height / 20.0 * 3)
                                    , (int)(pltWid / 10.0 * 7.5), (int)(rcCavity.Height / 20.0 * 14.0));
                                //DrawPalletRect(g, rcPlt, arrPallet[rowIdx * ovenCol + pltIdx], (pltIdx + 1).ToString());
                                DrawPalletRect(g, rcPlt, arrPallet[rowIdx * ovenCol + pltIdx], $"{pltIdx + 1}-{(pltIdx == 0 ? "左" : "右")}");
                                this.rectDryOvenPlt[halfOven + ovenIdx, rowIdx * ovenCol + pltIdx] = rcPlt;
                            }
                        }

                        // 腔体使能
                        if (!GetOvenCavityEnable(runId, rowIdx))
                        {
                            Point[] point = new Point[4];
                            point[0] = new Point(rcCavity.X, rcCavity.Y);
                            point[1] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y + rcCavity.Height);
                            point[2] = new Point(rcCavity.X, rcCavity.Y + rcCavity.Height);
                            point[3] = new Point(rcCavity.X + rcCavity.Width, rcCavity.Y);
                            g.DrawLines(pen, point);
                        }
                        // 腔体保压
                        if (GetOvenCavityPressure(runId, rowIdx))
                        {
                            Point[] point = new Point[4];
                            for (int i = 0; i < 2; i++)
                            {
                                point[i * 2] = new Point(rcCavity.X, (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                                point[i * 2 + 1] = new Point((rcCavity.X + rcCavity.Width), (int)(rcCavity.Y + rcCavity.Height / 3.0 * (i + 1)));
                                g.DrawLine(pen, point[i * 2], point[i * 2 + 1]);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 调度
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawTransfer(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            RunID runId = RunID.Invalid;

            // 导轨
            g.DrawRectangle(pen, rect);

            // 插料架
            runId = RunID.Transfer;
            arrPallet = ModulePallet(runId);
            if (null != arrPallet)
            {
                Rectangle rcPlt = new Rectangle((int)(rect.X + wid), (int)(rect.Y + hig * 7), (int)(wid * 10.0), (int)(hig * 88));
                //g.DrawRectangle(pen, rcPlt);
                RobotActionInfo actionInfo = GetRobotActionInfo(runId, true);
                if (null == actionInfo)
                {
                    return;
                }
                int halfOven = MachineCtrl.GetInstance().HalfDryingOvens;
                #region // 调整机器人位置
                if (null != actionInfo)
                {
                    switch ((TransferRobotStation)actionInfo.station)
                    {
                        case TransferRobotStation.OnloadStation:
                            rcPlt.Offset((int)(rcPlt.Width / 2 * actionInfo.col), 0);
                            break;
                        case TransferRobotStation.PalletBuffer:
                            rcPlt.Offset((int)(rcPlt.Width / 2 * (4.5 + actionInfo.col)), 0);
                            break;
                        case TransferRobotStation.ManualOperate:
                            rcPlt.Offset((int)(rcPlt.Width / 2 * (4.5 + actionInfo.col)), 0);
                            break;
                        case TransferRobotStation.OffloadStation:
                            rcPlt.Offset((int)(rcPlt.Width / 2 * (16 + actionInfo.col)), 0);
                            break;
                        case TransferRobotStation.StationEnd:
                            break;
                        default:
                            {
                                if ((actionInfo.station >= (int)TransferRobotStation.DryOven_0)
                                    && (actionInfo.station < ((int)TransferRobotStation.DryOven_0 + halfOven)))
                                {
                                    int ovenOffset = actionInfo.station - (int)TransferRobotStation.DryOven_0;
                                    rcPlt.Offset((int)(rcPlt.Width / 2 * (ovenOffset * 2.7 + 7.1 + actionInfo.col)), 0);
                                    break;
                                }
                                else if ((actionInfo.station >= (int)TransferRobotStation.DryOven_0 + halfOven)
                                    && (actionInfo.station <= ((int)TransferRobotStation.DryOven_All)))
                                {
                                    int ovenOffset = actionInfo.station - (int)TransferRobotStation.DryOven_0 - halfOven;
                                    rcPlt.Offset((int)(rcPlt.Width / 2 * (ovenOffset * 2.5 + actionInfo.col)), 0);
                                    break;
                                }
                                break;
                            }
                    }
                }
                #endregion
                DrawPalletRect(g, rcPlt, arrPallet[0], string.Format("{0}-{1}-{2}", actionInfo.station, (actionInfo.row + 1), (actionInfo.col + 1)));

                Rectangle rc = new Rectangle((rect.X + rect.Width / 3), (int)(rect.Bottom + 8), (int)(wid * 1.3), (int)(wid * 1.3));
                bool con = GetDeviceIsConnect(runId);
                DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rc, (con ? Brushes.DarkGreen : Brushes.Red));
                string rbtInfo = string.Format("{0}:{1}-{2}行-{3}列-{4}", RobotDef.RobotIDName[(int)RobotIndexID.Transfer], actionInfo.stationName
                    , (actionInfo.row + 1), (actionInfo.col + 1), RobotDef.RobotOrderName[(int)actionInfo.order]);
                g.DrawString(rbtInfo, (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (int)(rect.X + rect.Width / 3 + wid * 1.5), (rect.Bottom + 8));

                this.rectTransfer = rcPlt;
                DrawSafeDoorState(g, pen, rect);
                DrawOnloadConnectState(g, pen, rect);
                DrawOffloadConnectState(g, pen, rect);

                // 工单及数量
                //g.DrawString(($"登录的员工：{MesResources.Equipment.OperatorUserID}"), (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (int)(rect.X + rect.Width / 3 + wid * 1.5 + 270), (rect.Bottom + 8));
                g.DrawString(($"订单号：{MesResources.OrderNo}"), (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (int)(rect.X + rect.Width / 3 + wid * 1.5 + 350), (rect.Bottom + 8));
                g.DrawString(($"工序任务：{MesResources.OpOrder}"), (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (int)(rect.X + rect.Width / 3 + wid * 1.5 + 550), (rect.Bottom + 8));
            }

            //// 上料机器人移动状态
            //runId = RunID.OnloadRobot;
            //RobotActionInfo rbtAction = GetRobotActionInfo(runId, true);
            //if (null != rbtAction)
            //{
            //    Rectangle rc = new Rectangle((rect.X), (int)(rect.Bottom + 8), (int)(wid * 1.3), (int)(wid * 1.3));
            //    bool con = GetDeviceIsConnect(runId);
            //    DrawRect(g, (new Pen((con ? Brushes.DarkGreen : Brushes.Red))), rc, (con ? Brushes.DarkGreen : Brushes.Red));
            //    string rbtInfo = string.Format("{0}:{1}-{2}行-{3}列-{4}", ModuleName(runId)
            //        , rbtAction.stationName, (rbtAction.row + 1), (rbtAction.col + 1), RobotDef.RobotOrderName[(int)rbtAction.order]);
            //    g.DrawString(rbtInfo, (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (int)(rect.X + wid * 1.5), (rect.Bottom + 8));
            //}

            //// 下料机器人移动状态
            //runId = RunID.OffloadBattery;
            //RobotActionInfo rbtOffloadAction = GetRobotActionInfo(runId, true);
            //if (null != rbtOffloadAction)
            //{
            //    string rbtInfo = string.Format("{0}:{1}-{2}行-{3}列-{4}", ModuleName(runId)
            //        , rbtOffloadAction.stationName, (rbtOffloadAction.row + 1), (rbtOffloadAction.col + 1), RobotDef.RobotOrderName[(int)rbtOffloadAction.order]);
            //    g.DrawString(rbtInfo, (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (int)(rect.X + rect.Width * 4 / 5 + wid * 1.5), (rect.Bottom + 8));
            //}
        }
        /// <summary>
        /// 画安全门状态
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawSafeDoorState(Graphics g, Pen pen, Rectangle rect)
        {
            bool doorState = false;
            int inputIO = 0;
            MCState mc = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            for (int i = 0; i < (int)SystemIO.SafeDoorIO; i++)
            {
                doorState = MachineCtrl.GetInstance().SafeDoorOpenState(i, ref inputIO);

                if (doorState)
                {
                    break;
                }
            }

            float fAvgWid = (float)(rect.Width / 100.0);
            float fAvgHig = (float)(rect.Height / 100.0);
            Font font = new Font(this.Font.FontFamily, (float)10.0);
            Rectangle rcArea;
            int nXPos = (int)(rect.X + fAvgWid * 2);
            int nYPos = (int)(rect.Y + rect.Height + fAvgHig * 5);
            g.DrawString("安全门", font, Brushes.Red, nXPos, nYPos + fAvgHig * 2);
            rcArea = new Rectangle((int)(nXPos - fAvgWid * 2), (nYPos), (int)(fAvgWid * 2), (int)(fAvgHig * 20));

            if (doorState)
            {
                g.FillRectangle(new SolidBrush(Color.Red), rcArea);
            }
            else
            {
                g.FillRectangle(new SolidBrush(Color.Green), rcArea);
            }

            g.DrawRectangle(pen, rcArea);
        }
        private void DrawOnloadConnectState(Graphics g, Pen pen, Rectangle rect)
        {
            int onloadState = 0;
            int inputIO = 0;
            MCState mc = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            onloadState = MachineCtrl.GetInstance().onloadRunState;

            float fAvgWid = (float)(rect.Width / 100.0);
            float fAvgHig = (float)(rect.Height / 100.0);
            Font font = new Font(this.Font.FontFamily, (float)10.0);
            Rectangle rcArea;
            int nXPos = (int)(rect.X + fAvgWid * 10);
            int nYPos = (int)(rect.Y + rect.Height + fAvgHig * 5);
            g.DrawString("上料状态", font, Brushes.Red, nXPos, nYPos + fAvgHig * 2);
            rcArea = new Rectangle((int)(nXPos - fAvgWid * 2), (nYPos), (int)(fAvgWid * 2), (int)(fAvgHig * 20));

            if (onloadState == 2)
            {
                g.FillRectangle(new SolidBrush(Color.Green), rcArea);
            }
            else if (onloadState == 3)
            {
                g.FillRectangle(new SolidBrush(Color.Red), rcArea);
            }
            else
            {
                g.FillRectangle(new SolidBrush(Color.Yellow), rcArea);
            }

            g.DrawRectangle(pen, rcArea);
        }
        private void DrawOffloadConnectState(Graphics g, Pen pen, Rectangle rect)
        {
            int offloadState = 0;
            int inputIO = 0;
            MCState mc = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
            offloadState = MachineCtrl.GetInstance().offloadRunState;


            float fAvgWid = (float)(rect.Width / 100.0);
            float fAvgHig = (float)(rect.Height / 100.0);
            Font font = new Font(this.Font.FontFamily, (float)10.0);
            Rectangle rcArea;
            int nXPos = (int)(rect.X + fAvgWid * 20);
            int nYPos = (int)(rect.Y + rect.Height + fAvgHig * 5);
            g.DrawString("下料状态", font, Brushes.Red, nXPos, nYPos + fAvgHig * 2);
            rcArea = new Rectangle((int)(nXPos - fAvgWid * 2), (nYPos), (int)(fAvgWid * 2), (int)(fAvgHig * 20));

            if (offloadState == 2)
            {
                g.FillRectangle(new SolidBrush(Color.Green), rcArea);
            }
            else if (offloadState == 3)
            {
                g.FillRectangle(new SolidBrush(Color.Red), rcArea);
            }
            else
            {
                g.FillRectangle(new SolidBrush(Color.Yellow), rcArea);
            }
            g.DrawRectangle(pen, rcArea);
        }
        /// <summary>
        /// 下料区
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawOffLoad(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);
            Pallet[] arrPallet = null;
            Battery[] arrBattery = null;
            RunID runId = RunID.Invalid;

            // 下料夹具
            runId = RunID.OffloadBattery;
            arrPallet = ModulePallet(runId);
            if (null != arrPallet)
            {
                for (int i = 0; i < arrPallet.Length; i++)
                {
                    int div = (int)(wid * 2 * i + wid * 48 * i);
                    g.DrawString(("夹具" + (i + 1)), font, Brushes.Black, (rect.X + (int)(wid * 5 + div)), (rect.Y + hig * 56));
                    Rectangle rc = new Rectangle((int)(rect.X + (wid + div)/* + wid*/ - 2), (int)(rect.Y + hig * 3), (int)(wid * 53), (int)(hig * 52));
                    //DrawPallet(g, pen, rc, arrPallet[i], false, true, true);
                    DrawPallet(g, pen, rc, arrPallet[i], false, false, true);
                    this.rectOffloadBatPlt[i] = rc;

                    // 夹具位置使能
                    if (!GetPalletPosOffloadEnable(runId, i))
                    {
                        Point[] point = new Point[4];
                        point[0] = new Point(rc.X, rc.Y);
                        point[1] = new Point(rc.X + rc.Width, rc.Y + rc.Height);
                        point[2] = new Point(rc.X, rc.Y + rc.Height);
                        point[3] = new Point(rc.X + rc.Width, rc.Y);
                        g.DrawLines(pen, point);
                    }
                }
                //if (MachineCtrl.GetInstance().OffloadClear)
                //{
                //    g.DrawString("下料清尾料中...", font, Brushes.Red, (rect.X + 10), (rect.Y + rect.Height / 2));
                //}
            }
            //RobotActionInfo rbtAction = GetRobotActionInfo(runId, true);
            //if(null != rbtAction)
            //{
            //    string rbtInfo = string.Format("{0}:{1}-{2}行-{3}列-{4}", ModuleName(runId)
            //        , rbtAction.stationName, (rbtAction.row + 1), (rbtAction.col + 1), RobotDef.RobotOrderName[(int)rbtAction.order]);
            //    g.DrawString(rbtInfo, (new Font(font.FontFamily, 11, FontStyle.Bold)), Brushes.Black, (rect.X), (rect.Y - 20));
            //}

            // 下料 抓手  及 暂存
            runId = RunID.OffloadBattery;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                // 抓手 - 暂存
                string[] info = new string[] { "抓手", "暂存" };
                for (int i = 0; i < 2; i++)
                {
                    int div = (int)(wid * 10 * i + wid * 25 * i);
                    g.DrawString(info[i], font, Brushes.Black, (rect.X + wid + div), (rect.Y + hig * 93));
                    Rectangle rc = new Rectangle((int)(rect.X + (wid + div) + wid), (rect.Y + (int)(hig * 63)), (int)(wid * 25), (int)(hig * 30));
                    Battery[] arrBat = new Battery[arrBattery.Length / 2];
                    for (int idx = 0; idx < arrBattery.Length / 2; idx++)
                    {
                        arrBat[idx] = arrBattery[(i * arrBattery.Length / 2) + idx];
                    }
                    DrawBattery(g, pen, rc, arrBat, true, true, false);
                    if (0 == i)
                    {
                        this.rectOffloadBatFinger = rc;
                    }
                    else
                    {
                        this.rectOffloadBatBuffer = rc;
                    }
                }
            }
            // 待检测输出
            //runId = RunID.OffloadDetect;
            //arrBattery = ModuleBattery(runId);
            //if (null != arrBattery)
            //{
            //    g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 32), (rect.Y + hig * 93));
            //    Rectangle rc = new Rectangle((rect.X + (int)(wid * 32)), (rect.Y + (int)(hig * 63)), (int)(wid * 10), (int)(hig * 30));
            //    DrawBattery(g, pen, rc, arrBattery, false, true, false);
            //    this.rectOffloadDetect = rc;
            //}
            // 下料NG电池输出
            //runId = RunID.OffloadNG;
            //arrBattery = ModuleBattery(runId);
            //if (null != arrBattery)
            //{
            //    g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 56), (rect.Y + hig * 93));
            //    Rectangle rc = new Rectangle((rect.X + (int)(wid * 56)), (rect.Y + (int)(hig * 63)), (int)(wid * 15), (int)(hig * 30));
            //    DrawBattery(g, pen, rc, arrBattery, false, true, false);
            //    this.rectOffloadNG = rc;
            //}
        }

        /// <summary>
        /// 冷却系统
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawCoolingSystem(Graphics g, Pen pen, Rectangle rect)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width);
            float hig = (float)(rect.Height);
            RunID runId = RunID.Invalid;

            // 冷却系统
            runId = RunID.CoolingSystem;
            BatteryLine batLine = ModuleBatteryLine(runId);
            if (null != batLine)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 53), (rect.Y + hig * 92));
                int maxRow = batLine.MaxRow;
                int maxCol = batLine.MaxCol;
                for (int col = 0; col < maxCol; col++)
                {
                    for (int row = 0; row < maxRow; row++)
                    {
                        Rectangle rc = new Rectangle((int)(rect.X + wid / maxRow * row), (int)(rect.Y + hig / maxCol * (maxCol - 1 - col)), (int)(wid / maxRow), (int)(hig / maxCol));
                        DrawBattery(g, pen, rc, (new Battery[] { batLine.Battery[row, col] }), true, false, false);
                    }
                }
                this.rectCoolingSystem = rect;
            }
        }

        private void DrawCoolingSystem(Graphics g, Pen pen, Rectangle rect, bool level, bool topToDown, bool leftToRight)
        {
            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width);
            float hig = (float)(rect.Height);
            RunID runId = RunID.Invalid;

            // 冷却系统
            runId = RunID.CoolingSystem;
            BatteryLine batLine = ModuleBatteryLine(runId);
            if (null != batLine)
            {
                g.DrawString(ModuleName(runId), font, Brushes.Black, (rect.X + wid * 53), (rect.Y + hig * 92));

                int maxRow = batLine.MaxRow;
                int maxCol = batLine.MaxCol;
                //for (int col = 0; col < maxCol; col++)
                //{
                //    for (int row = 0; row < maxRow; row++)
                //    {
                //        Rectangle rc = new Rectangle((int)(rect.X + wid / maxRow * row), (int)(rect.Y + hig / maxCol * (maxCol - 1 - col)), (int)(wid / maxRow), (int)(hig / maxCol));
                //        DrawBattery(g, pen, rc, (new Battery[] { batLine.Battery[row, col] }), true, false, false);
                //    }
                //}
                //this.rectCoolingSystem = rect;

                // 绘制电池
                if (level)
                {
                    if (topToDown)
                    {
                        for (int row = 0; row < maxRow; row++)
                        {
                            for (int col = 0; col < maxCol; col++)
                            {
                                Rectangle rc = new Rectangle((rect.X + rect.Width / maxCol * col), (rect.Y + rect.Height / maxRow * row), (rect.Width / maxCol), (rect.Height / maxRow));
                                DrawBattery(g, pen, rc, (new Battery[] { batLine.Battery[row, col] }), true, false, false);
                            }
                        }
                    }
                    else
                    {
                        for (int row = 0; row < maxRow; row++)
                        {
                            for (int col = 0; col < maxCol; col++)
                            {
                                Rectangle rc = new Rectangle((rect.X + rect.Width / maxCol * col), (rect.Y + rect.Height / maxRow * (maxRow - row)), (rect.Width / maxCol), (rect.Height / maxRow));
                                DrawBattery(g, pen, rc, (new Battery[] { batLine.Battery[row, col] }), true, false, false);
                            }
                        }
                    }
                }
                else
                {
                    if (topToDown)
                    {
                        for (int row = 0; row < maxRow; row++)
                        {
                            for (int col = 0; col < maxCol; col++)
                            {
                                Rectangle rc = new Rectangle((rect.X + rect.Width / maxRow * row), (rect.Y + rect.Height / maxCol * col), (rect.Width / maxRow), (rect.Height / maxCol));
                                DrawBattery(g, pen, rc, (new Battery[] { batLine.Battery[row, col] }), true, false, false);
                            }
                        }
                    }
                    else
                    {
                        if (leftToRight)
                        {
                            for (int row = 0; row < maxRow; row++)
                            {
                                for (int col = 0; col < maxCol; col++)
                                {
                                    Rectangle rc = new Rectangle((rect.X + rect.Width / maxRow * row), (rect.Y + rect.Height / maxCol * (maxCol - 1 - col)), (rect.Width / maxRow), (rect.Height / maxCol));
                                    DrawBattery(g, pen, rc, (new Battery[] { batLine.Battery[row, col] }), true, false, false);
                                }
                            }
                        }
                        else
                        {
                            for (int row = 0; row < maxRow; row++)
                            {
                                for (int col = 0; col < maxCol; col++)
                                {
                                    Rectangle rc = new Rectangle((rect.Right - rect.Width / maxRow * (row + 2)), (rect.Y + rect.Height / maxCol * (maxCol - 1 - col)), (rect.Width / maxRow), (rect.Height / maxCol));
                                    DrawBattery(g, pen, rc, (new Battery[] { batLine.Battery[row, col] }), true, false, false);
                                }
                            }
                        }
                    }
                }
            }

            // 复原原有画笔颜色
            //pen.Color = oldColor;
        }

        /// <summary>
        /// 冷却下料
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect"></param>
        private void DrawCoolingOffload(Graphics g, Pen pen, Rectangle rect)
        {
            int bufferCnt = MachineCtrl.GetInstance().OffloadBuffers;

            // 模组名字体
            Font font = new Font(this.Font.FontFamily, (float)10.0);

            float wid = (float)(rect.Width / 100.0);
            float hig = (float)(rect.Height / 100.0);
            Battery[] arrBattery = null;
            RunID runId = RunID.Invalid;

            // 冷却下料
            runId = RunID.CoolingOffload;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                // 抓手 - 暂存
                string[] info = new string[] { "抓", "暂" };
                //for (int i = 0; i < 2; i++)
                //{
                //    Battery[] arrBat = new Battery[arrBattery.Length / 2];
                //    for (int idx = 0; idx < arrBattery.Length / 2; idx++)
                //    {
                //        arrBat[idx] = arrBattery[(i * arrBattery.Length / 2) + idx];
                //    }
                //    int div = (int)(wid * 14 * i);
                //    g.DrawString(info[i], font, Brushes.Black, (rect.X + wid + div), (rect.Y + hig * 3));

                //    Rectangle rc = new Rectangle((int)(rect.X + (wid + div)), (rect.Y + (int)(hig * 25)), (int)(wid * 12), (int)(hig * 75));
                //    DrawBattery(g, pen, rc, arrBat, true, true, false);
                //    if (0 == i)
                //    {
                //        this.rectCoolingFinger = rc;
                //    }
                //    else
                //    {
                //        this.rectCoolingBuffer = rc;
                //    }
                //}
                int div = 0;
                for (int i = 0; i < 1; i++)
                {
                    Battery[] arrBat = new Battery[arrBattery.Length / 2];
                    for (int idx = 0; idx < arrBattery.Length / 2; idx++)
                    {
                        arrBat[idx] = arrBattery[(i * arrBattery.Length / 2) + idx];
                    }
                    div = (int)(wid * 14 * i);
                    g.DrawString(info[i], font, Brushes.Black, (rect.X + wid + div), (rect.Y + hig * 3));

                    Rectangle rc = new Rectangle((int)(rect.X + (wid + div)), (rect.Y + (int)(hig * 25)), (int)(wid * 12), (int)(hig * 75));
                    //DrawBattery(g, pen, rc, arrBat, true, true, false);
                    DrawOffloadBattery(g, pen, rc, arrBat, true, true, true);
                    if (0 == i)
                    {
                        this.rectCoolingFinger = rc;
                    }
                    else
                    {
                        this.rectCoolingBuffer = rc;
                    }
                }
                div = (int)(wid * 14 * 1);

            }
            // 下料物流线
            runId = RunID.OffloadLine;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                g.DrawString(/*ModuleName(runId)*/"下", font, Brushes.Black, (rect.X + wid * 28), (rect.Y + hig * 3));
                //for (int i = 0; i < 2; i++)
                //{
                //    Battery[] arrBat = new Battery[arrBattery.Length / 2];
                //    for (int idx = 0; idx < arrBattery.Length / 2; idx++)
                //    {
                //        arrBat[idx] = arrBattery[(/*i **/ arrBattery.Length / 2) + idx];
                //    }

                //    Rectangle rc = new Rectangle((int)(rect.X + (wid * 14 * 2)), (rect.Y + (int)(hig * 25)), (int)(wid * 12), (int)(hig * 75));
                //    DrawBattery(g, pen, rc, arrBat, true, true, false);
                //}

                Battery[] arrBat = new Battery[arrBattery.Length / 2];
                for (int idx = 0; idx < arrBattery.Length / 2; idx++)
                {
                    arrBat[idx] = arrBattery[idx];
                }

                Rectangle rc = new Rectangle((int)(rect.X + (wid * 14 * 2)), (rect.Y + (int)(hig * 25)), (int)(wid * 12), (int)(hig * 75));
                DrawBattery(g, pen, rc, arrBat, true, true, false);

                this.rectOffloadLine = rc;// new Rectangle((rect.X + (int)(wid * 62)), (rect.Y + (int)(hig)), (int)(wid * 6), (int)(hig * 30 * 2));
            }

            for (int idx = 0; idx < this.rectOfflineBuffer.Length; idx++)
            //for (runId = RunID.OffloadBuffer; runId < (RunID.OffloadBufferEnd + 1); runId++)
            {
                g.DrawString(/*ModuleName(runId)*/"缓存", font, Brushes.Black, (rect.X + wid * 42), (rect.Y + hig * 3));

                runId = RunID.OffloadBuffer + idx;
                arrBattery = ModuleBattery(runId);
                if (null != arrBattery)
                {
                    Rectangle rc = new Rectangle((int)(rect.X + (wid * 14 * 3) + wid * 14 * ((int)runId - (int)RunID.OffloadBuffer)), (rect.Y + (int)(hig * 25)), (int)(wid * 12), (int)(hig * 75));
                    DrawBattery(g, pen, rc, arrBattery, true, true, false);

                    this.rectOfflineBuffer[idx] = rc;// new Rectangle((rect.X + (int)(wid * 62)), (rect.Y + (int)(hig)), (int)(wid * 6), (int)(hig * 30 * 2));
                }
            }

            runId = RunID.OffloadOut;
            arrBattery = ModuleBattery(runId);
            if (null != arrBattery)
            {
                g.DrawString(/*ModuleName(runId)*/"出", font, Brushes.Black, (rect.X + wid * 84), (rect.Y + hig * 3));

                Rectangle rc = new Rectangle((int)(rect.X + (wid * 14 * 6)), (rect.Y + (int)(hig * 25)), (int)(wid * 12), (int)(hig * 75));
                DrawBattery(g, pen, rc, arrBattery, true, true, false);

                this.rectOffloadOut = rc;// new Rectangle((rect.X + (int)(wid * 62)), (rect.Y + (int)(hig)), (int)(wid * 6), (int)(hig * 30 * 2));
            }
        }

        #endregion

        #region // 绘制工具

        /// <summary>
        /// 绘制单个电池
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rect">区域</param>
        /// <param name="batState">电池状态</param>
        /// <param name="withTxet">附带文本</param>
        private void DrawBattery(Graphics g, Pen pen, Rectangle rect, BatteryStatus batState, string withTxet, BatteryNGStatus batNGState)
        {
            Font font = new Font(this.Font.FontFamily, (float)10.0);
            StringFormat strFormat = new StringFormat();//文本格式
            strFormat.LineAlignment = StringAlignment.Center;//垂直居中
            strFormat.Alignment = StringAlignment.Center;//水平居中
            SolidBrush brush = null;
            switch (batState)
            {
                case BatteryStatus.Invalid:
                    brush = new SolidBrush(Color.Transparent);
                    break;
                case BatteryStatus.OK:
                    brush = new SolidBrush(Color.Green);
                    break;
                case BatteryStatus.NG:
                    if (BatteryNGStatus.MesNG == batNGState)
                        brush = new SolidBrush(Color.Black);
                    else
                        brush = new SolidBrush(Color.Red);
                    break;
                case BatteryStatus.Fake:
                    brush = new SolidBrush(Color.Blue);
                    break;
                case BatteryStatus.ReFake:
                    brush = new SolidBrush(Color.BlueViolet);
                    break;
                case BatteryStatus.Detect:
                    brush = new SolidBrush(Color.SteelBlue);
                    break;
                default:
                    brush = new SolidBrush(Color.Black);
                    break;
            }
            // 先填充，后绘制，否则会出现白边
            g.FillRectangle(brush, rect);
            g.DrawRectangle(pen, rect);
            g.DrawString(withTxet, font, Brushes.Black, rect, strFormat);
        }

        /// <summary>
        /// 绘制一组电池
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect">区域</param>
        /// <param name="arrBat">电池组</param>
        /// <param name="level">true水平放置，false垂直放置</param>
        /// <param name="withID">带有电池ID</param>
        /// <param name="withCode">带有电池条码</param>
        private void DrawBattery(Graphics g, Pen pen, Rectangle rect, Battery[] arrBat, bool level, bool withID, bool withCode)
        {
            if (null == arrBat)
            {
                return;
            }
            string info = "";
            int length = arrBat.Length;
            if (0 == length)
            {
                return;
            }
            int wid = level ? rect.Width : (rect.Width / length);
            int hig = level ? (rect.Height / length) : rect.Height;
            for (int i = 0; i < length; i++)
            {
                int nleft = level ? 0 : (rect.Width / length * i);
                int ntop = level ? (rect.Height / length * i) : 0;
                Rectangle rcBat = new Rectangle((rect.Left + nleft), (rect.Top + ntop), wid, hig);
                info = withID ? (i + 1).ToString() : "";
                info = withCode ? arrBat[i].Code : info;
                DrawBattery(g, pen, rcBat, arrBat[i].Type, info, arrBat[i].NGType);
            }
        }

        private void DrawBattery2(Graphics g, Pen pen, Rectangle rect, Battery[] arrBat, bool level, bool withID, bool withCode)
        {
            if (null == arrBat)
            {
                return;
            }
            string info = "";
            int length = arrBat.Length;
            if (0 == length)
            {
                return;
            }
            int wid = level ? rect.Width : (rect.Width / length);
            int hig = level ? (rect.Height / length) : rect.Height;
            for (int i = 0; i < length; i++)
            {
                int nleft = level ? 0 : (rect.Width / length * i);
                int ntop = level ? (rect.Height / length * i) : 0;
                Rectangle rcBat = new Rectangle((rect.Left + nleft), (rect.Top + ntop), wid, hig);
                info = withID ? (length - i).ToString() : "";
                info = withCode ? arrBat[i].Code : info;
                DrawBattery(g, pen, rcBat, arrBat[i].Type, info, arrBat[i].NGType);
            }
        }

        private void DrawOffloadBattery(Graphics g, Pen pen, Rectangle rect, Battery[] arrBat, bool level, bool withID, bool withCode)
        {
            if (null == arrBat)
            {
                return;
            }
            string info = "";
            int length = arrBat.Length;
            if (0 == length)
            {
                return;
            }
            int wid = level ? rect.Width : (rect.Width / length);
            int hig = level ? (rect.Height / length) : rect.Height;
            for (int i = 0; i < length; i++)
            {
                int nleft = level ? 0 : (rect.Width / length * i);
                int ntop = level ? (rect.Height / length * i) : 0;
                Rectangle rcBat = new Rectangle((rect.Left + nleft), (rect.Top + ntop), wid * 2, hig);
                info = withID ? (i + 1).ToString() : "";
                //info = withCode ? $"{info}:{arrBat[i].TemperValue.ToString("0.0")}" : info;
                info = withCode ? $"{arrBat[i].TemperValue.ToString("0.")}" : info;
                DrawBattery(g, pen, rcBat, arrBat[i].Type, info, arrBat[i].NGType);
            }
        }

        /// <summary>
        /// 绘制夹具，带电池
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="rect">区域</param>
        /// <param name="pallet">夹具数据</param>
        /// <param name="level">水平绘制电池</param>
        /// <param name="topToDown">从顶到底开始绘制</param>
        /// <param name="leftToRight">从左到右开始绘制</param>
        private void DrawPallet(Graphics g, Pen pen, Rectangle rect, Pallet pallet, bool level, bool topToDown, bool leftToRight)
        {
            int maxRow = (int)pallet.MaxRow;
            int maxCol = (int)pallet.MaxCol;

            Color oldColor = pen.Color;
            // 设置NG夹具画笔颜色
            if (PalletStatus.NG == pallet.State)
            {
                pen.Color = Color.Red;
            }
            if (PalletStatus.Invalid == pallet.State)
            {
                DrawPalletRect(g, rect, pallet);
                return;
            }
            //if (PalletStatus.TestBat == pallet.State)
            //{
            //    DrawPalletRect(g, rect, pallet);
            //    //return;
            //}
            // 绘制电池
            if (level)
            {
                if (topToDown)
                {
                    for (int row = 0; row < maxRow; row++)
                    {
                        for (int col = 0; col < maxCol; col++)
                        {
                            Rectangle rc = new Rectangle((rect.X + rect.Width / maxCol * col), (rect.Y + rect.Height / maxRow * row), (rect.Width / maxCol), (rect.Height / maxRow));
                            DrawBattery(g, pen, rc, (new Battery[] { pallet.Battery[row, col] }), true, false, false);
                        }
                    }
                }
                else
                {
                    for (int row = 0; row < maxRow; row++)
                    {
                        for (int col = 0; col < maxCol; col++)
                        {
                            Rectangle rc = new Rectangle((rect.X + rect.Width / maxCol * col), (rect.Y + rect.Height / maxRow * (maxRow - row)), (rect.Width / maxCol), (rect.Height / maxRow));
                            DrawBattery(g, pen, rc, (new Battery[] { pallet.Battery[row, col] }), true, false, false);
                        }
                    }
                }
            }
            else
            {
                if (topToDown)
                {
                    for (int row = 0; row < maxRow; row++)
                    {
                        for (int col = 0; col < maxCol; col++)
                        {
                            Rectangle rc = new Rectangle((rect.X + rect.Width / maxRow * row), (rect.Y + rect.Height / maxCol * col), (rect.Width / maxRow), (rect.Height / maxCol));
                            DrawBattery(g, pen, rc, (new Battery[] { pallet.Battery[row, col] }), true, false, false);
                        }
                    }
                }
                else
                {
                    if (leftToRight)
                    {
                        for (int row = 0; row < maxRow; row++)
                        {
                            for (int col = 0; col < maxCol; col++)
                            {
                                Rectangle rc = new Rectangle((rect.X + rect.Width / maxRow * row), (rect.Y + rect.Height / maxCol * (maxCol - 1 - col)), (rect.Width / maxRow), (rect.Height / maxCol));
                                DrawBattery(g, pen, rc, (new Battery[] { pallet.Battery[row, col] }), true, false, false);
                            }
                        }
                    }
                    else
                    {
                        for (int row = 0; row < maxRow; row++)
                        {
                            for (int col = 0; col < maxCol; col++)
                            {
                                Rectangle rc = new Rectangle((rect.Right - rect.Width / maxRow * (row + 2)), (rect.Y + rect.Height / maxCol * (maxCol - 1 - col)), (rect.Width / maxRow), (rect.Height / maxCol));
                                DrawBattery(g, pen, rc, (new Battery[] { pallet.Battery[row, col] }), true, false, false);
                            }
                        }
                    }
                }
            }

            // 复原原有画笔颜色
            pen.Color = oldColor;
        }

        /// <summary>
        /// 绘制夹具矩形框，无电池
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rect"></param>
        /// <param name="cavityState"></param>
        /// <param name="withTxet"></param>
        private void DrawPalletRect(Graphics g, Rectangle rect, Pallet pallet, string withTxet = null)
        {
            Pen pen = null;
            Brush brush = null;
            switch (pallet.State)
            {
                case PalletStatus.Invalid:
                    pen = new Pen(Color.Black);
                    brush = Brushes.Transparent;
                    break;
                case PalletStatus.OK:
                    if (!string.IsNullOrEmpty(pallet.Battery[0, 0].Code) && pallet.Battery[0, 0].Code.StartsWith("TEST"))
                    {
                        pen = pallet.HasFake() ? (new Pen(Color.Blue, 2)) : (new Pen(Color.Black));
                        brush = pallet.IsEmpty() ? Brushes.DarkGray : Brushes.GreenYellow;
                    }
                    else
                    {
                        pen = pallet.HasFake() ? (new Pen(Color.Blue, 2)) : (new Pen(Color.Black));
                        brush = pallet.IsEmpty() ? Brushes.DarkGray : Brushes.Green;
                    }
                    break;
                //case PalletStatus.TestBat:
                //    pen = pallet.HasFake() ? (new Pen(Color.Blue, 2)) : (new Pen(Color.Black));
                //    brush = pallet.IsEmpty() ? Brushes.DarkGray : Brushes.GreenYellow;
                //    break;
                case PalletStatus.NG:
                    pen = new Pen(Color.Red, 2);
                    brush = pallet.IsEmpty() ? Brushes.Red : Brushes.DarkRed;
                    break;
                case PalletStatus.Detect:
                case PalletStatus.WaitResult:
                    pen = pallet.HasFake() ? (new Pen(Color.Blue, 2)) : (new Pen(Color.Black));
                    brush = Brushes.Yellow;
                    break;
                case PalletStatus.WaitOffload:
                    pen = pallet.HasFake() ? (new Pen(Color.Blue, 2)) : (new Pen(Color.Black));
                    brush = Brushes.DarkGoldenrod;
                    break;
                case PalletStatus.ReputFake:
                case PalletStatus.Rebaking:
                    pen = pallet.HasFake() ? (new Pen(Color.Blue, 2)) : (new Pen(Color.Black));
                    brush = Brushes.Magenta;
                    break;
                default:
                    break;
            }
            if (null != brush)
            {
                DrawRect(g, pen, rect, brush, Color.Black, withTxet);
            }
        }

        /// <summary>
        /// 绘制腔体状态
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rect"></param>
        /// <param name="cavityState"></param>
        /// <param name="withTxet"></param>
        private void DrawCavity(Graphics g, Rectangle rect, CavityStatus cavityState, string withTxet = null)
        {
            Brush brush = null;
            switch (cavityState)
            {
                case CavityStatus.Normal:
                    brush = Brushes.Transparent;
                    break;
                case CavityStatus.Heating:
                    brush = Brushes.Yellow;
                    break;
                case CavityStatus.WaitDetect:
                    brush = Brushes.Cyan;
                    break;
                case CavityStatus.WaitResult:
                    brush = new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.DarkCyan);
                    break;
                case CavityStatus.WaitRebaking:
                    brush = new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.Magenta);
                    break;
                case CavityStatus.Maintenance:
                    brush = new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.Black);
                    break;
                case CavityStatus.Maintenance + 1:
                    brush = new HatchBrush(HatchStyle.Cross, Color.Transparent, Color.IndianRed);
                    break;
                default:
                    brush = new HatchBrush(HatchStyle.Trellis, Color.Transparent, Color.Black);
                    break;
            }
            if (null != brush)
            {
                DrawRect(g, (new Pen(Color.Black)), rect, brush, Color.Black, withTxet);
            }
        }

        /// <summary>
        /// 绘制一个带颜色的矩形
        /// </summary>
        /// <param name="g"></param>
        /// <param name="rect"></param>
        /// <param name="lineColor">线条颜色</param>
        /// <param name="fillBrush">填充颜色</param>
        /// <param name="textColor">文本颜色</param>
        /// <param name="withTxet">附带文本</param>
        /// <param name="fontSize">文本字体大小</param>
        private void DrawRect(Graphics g, Pen pen, Rectangle rect, Brush fillBrush, Color textColor = new Color(), string withTxet = null, float fontSize = (float)10.0)
        {
            Font font = new Font(this.Font.FontFamily, fontSize);
            StringFormat strFormat = new StringFormat();//文本格式
            strFormat.LineAlignment = StringAlignment.Center;//垂直居中
            strFormat.Alignment = StringAlignment.Center;//水平居中
            Brush txtBrush = new SolidBrush(textColor);
            g.FillRectangle(fillBrush, rect);
            g.DrawRectangle(pen, rect);
            if (null != withTxet)
            {
                g.DrawString(withTxet, font, txtBrush, rect, strFormat);
            }
        }
        #endregion

        #region // 模组数据

        /// <summary>
        /// 获取模组名
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private string ModuleName(RunID id)
        {
            string name = "";
            switch (id)
            {
                case RunID.OnloadRecv:
                    name = "来料收电池";
                    break;
                case RunID.OnloadLine:
                    name = "取料";
                    break;
                case RunID.OnloadScan:
                    name = "来料";
                    break;
                case RunID.OnloadRobot:
                    name = "上料机器人";
                    break;
                case RunID.OnloadNG:
                    name = "NG输出";
                    break;
                case RunID.OnloadFake:
                    name = "上假电池";
                    break;
                case RunID.OnloadDetect:
                    name = "测试";
                    break;
                case RunID.Transfer:
                    name = "调度模组";
                    break;
                case RunID.ManualOperate:
                    name = "人工台";
                    break;
                case RunID.PalletBuffer:
                    name = "夹具缓存架";
                    break;
                case RunID.OffloadBattery:
                    name = "电池下料";
                    break;
                case RunID.OffloadNG:
                    name = "NG输出";
                    break;
                case RunID.OffloadDetect:
                    name = "下待测";
                    break;
                case RunID.OffloadLine:
                    name = "下料线";
                    break;
                case RunID.CoolingSystem:
                    name = "冷却系统";
                    break;
                case RunID.CoolingOffload:
                    name = "冷却下料";
                    break;
                case RunID.OffloadOut:
                    name = "出料线";
                    break;
                default:
                    if (RunID.DryOven0 <= id && id < RunID.DryOvenALL)
                    {
                        name = "干燥炉" + ((int)id - (int)RunID.DryOven0 + 1);
                    }
                    if (RunID.OffloadBuffer <= id && id <= RunID.OffloadBufferEnd)
                    {
                        name = "缓存线" + ((int)id - (int)RunID.OffloadBuffer + 1);
                    }
                    break;
            }
            return name;
        }

        /// <summary>
        /// 获取模组夹具
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private Pallet[] ModulePallet(RunID id)
        {
            return MachineCtrl.GetInstance().GetModulePallet(id);
        }

        /// <summary>
        /// 获取模组电池
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private Battery[] ModuleBattery(RunID id)
        {
            RunProcess run = MachineCtrl.GetInstance().GetModule(id);
            // 模组存在，使用本地数据
            if (null != run)
            {
                return run.Battery;
            }
            // 模组不存在，使用网络数据
            else
            {
                ModuleSocketData socketData = MachineCtrl.GetInstance().GetModuleSocketData(id);
                if (null != socketData)
                {
                    return socketData.battery[id]; // wjj 220416
                }
            }
            return null;
        }

        /// <summary>
        /// 获取模组电池线（冷却系统电池）
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private BatteryLine ModuleBatteryLine(RunID id)
        {
            return MachineCtrl.GetInstance().GetModuleBatteryLine(id);
        }

        /// <summary>
        /// 获取干燥炉/机器人连接状态：true连接，false断开
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool GetDeviceIsConnect(RunID id)
        {
            return MachineCtrl.GetInstance().GetDeviceIsConnect(id);
        }

        /// <summary>
        /// 获取干燥炉腔体状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private CavityStatus GetCavityState(RunID id, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityState(id, cavityIdx);
        }

        /// <summary>
        /// 获取干燥炉腔体使能状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool GetOvenCavityEnable(RunID id, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityEnable(id, cavityIdx);
        }

        /// <summary>
        /// 获取干燥炉腔体保压状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool GetOvenCavityPressure(RunID id, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityPressure(id, cavityIdx);
        }

        /// <summary>
        /// 获取干燥炉腔体转移状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cavityIdx"></param>
        /// <returns></returns>
        private bool GetOvenCavityTransfer(RunID id, int cavityIdx)
        {
            return MachineCtrl.GetInstance().GetDryingOvenCavityTransfer(id, cavityIdx);
        }

        /// <summary>
        /// 获取上下料夹具位使能状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pltIdx"></param>
        /// <returns></returns>
        private bool GetPalletPosEnable(RunID id, int pltIdx)
        {
            return MachineCtrl.GetInstance().GetPalletPosEnable(id, pltIdx);
        }
        /// <summary>
        /// 获取上下料夹具位使能状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pltIdx"></param>
        /// <returns></returns>
        private bool GetPalletPosOffloadEnable(RunID id, int pltIdx)
        {
            return MachineCtrl.GetInstance().GetPalletPosOffloadEnable(id, pltIdx);
        }
        /// <summary>
        /// 获取缓存架层使能状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rowIdx"></param>
        /// <returns></returns>
        private bool GetPalletBufferRowEnable(RunID id, int rowIdx)
        {
            return MachineCtrl.GetInstance().GetPalletBufferRowEnable(id, rowIdx);
        }

        /// <summary>
        /// 获取机器人动作信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="autoAction"></param>
        /// <returns></returns>
        private RobotActionInfo GetRobotActionInfo(RunID id, bool autoAction)
        {
            return MachineCtrl.GetInstance().GetRobotActionInfo(id, autoAction);
        }

        #endregion

        #region // 鼠标点击提示信息

        void InitModuleRectangle()
        {
            // MES信息
            this.rectMesInfo = new Rectangle();
            // MES入站校验
            this.rectMesCheck = new Rectangle();
            // 上料联机
            this.rectConveyerLineEN = new Rectangle();
            // 清尾料
            this.rectDevicestatusEN = new Rectangle();
            // 上料
            this.rectOnloadLine = new Rectangle();
            this.rectOnloadScan = new Rectangle();
            this.rectOnloadRbtPlt = new Rectangle[(int)ModuleMaxPallet.OnloadRobot];
            for (int i = 0; i < this.rectOnloadRbtPlt.Length; i++)
            {
                this.rectOnloadRbtPlt[i] = new Rectangle();
            }
            this.rectOfflineBuffer = new Rectangle[MachineCtrl.GetInstance().OffloadBuffers];
            for (int i = 0; i < this.rectOfflineBuffer.Length; i++)
            {
                this.rectOfflineBuffer[i] = new Rectangle();
            }
            this.rectOnloadRbtFinger = new Rectangle();
            this.rectOnloadRbtBuffer = new Rectangle();
            this.rectOnloadNG = new Rectangle();
            this.rectOnloadFake = new Rectangle();
            this.rectOnloadDetect = new Rectangle();

            // 调度
            this.rectTransfer = new Rectangle();
            this.rectManualOperate = new Rectangle();
            this.rectPltBufferPlt = new Rectangle[(int)ModuleMaxPallet.PalletBuffer];
            for (int i = 0; i < this.rectPltBufferPlt.Length; i++)
            {
                this.rectPltBufferPlt[i] = new Rectangle();
            }

            // 下料
            this.rectOffloadBatPlt = new Rectangle[(int)ModuleMaxPallet.OffloadBattery];
            for (int i = 0; i < this.rectOffloadBatPlt.Length; i++)
            {
                this.rectOffloadBatPlt[i] = new Rectangle();
            }
            this.rectOffloadBatFinger = new Rectangle();
            this.rectOffloadBatBuffer = new Rectangle();
            this.rectOffloadNG = new Rectangle();
            this.rectOffloadDetect = new Rectangle();
            this.rectOffloadLine = new Rectangle();
            this.rectCoolingSystem = new Rectangle();
            this.rectCoolingFinger = new Rectangle();
            this.rectCoolingBuffer = new Rectangle();

            // 干燥炉：炉子,腔体/夹具
            this.rectDryOvenCavity = new Rectangle[(int)OvenInfoCount.OvenCount, (int)OvenRowCol.MaxRow];
            for (int i = 0; i < this.rectDryOvenCavity.GetLength(0); i++)
            {
                for (int j = 0; j < this.rectDryOvenCavity.GetLength(1); j++)
                {
                    this.rectDryOvenCavity[i, j] = new Rectangle();
                }
            }
            this.rectDryOvenPlt = new Rectangle[(int)OvenInfoCount.OvenCount, (int)ModuleMaxPallet.DryingOven];
            for (int i = 0; i < this.rectDryOvenPlt.GetLength(0); i++)
            {
                for (int j = 0; j < this.rectDryOvenPlt.GetLength(1); j++)
                {
                    this.rectDryOvenPlt[i, j] = new Rectangle();
                }
            }
        }

        /// <summary>
        /// 调整提示窗口位置
        /// </summary>
        void AdjustTipPos(ref Rectangle rcDest)
        {
            // 1.假设窗口显示
            Rectangle rcCurTip = new Rectangle();
            rcCurTip.Width = this.tip.GetContentWidth();
            rcCurTip.Height = this.tip.GetContentHeight();
            //rcCurTip.X = Cursor.Position.X - rcCurTip.Width / 2;
            //rcCurTip.Y = Cursor.Position.Y - rcCurTip.Height;
            Rectangle ScreenArea = System.Windows.Forms.Screen.GetBounds(this);
            int width1 = ScreenArea.Width; //屏幕宽度 
            int height1 = ScreenArea.Height; //屏幕高度
            rcCurTip.X = (width1 - rcCurTip.Width) / 2;
            rcCurTip.Y = (height1 - rcCurTip.Height) / 2;

            // 2.计算窗口到屏幕上下左右的距离
            Rectangle rcScreen = new Rectangle();
            rcScreen = Screen.GetWorkingArea(this);
            int leftDis = rcCurTip.Left - rcScreen.Left;
            int rightDis = rcScreen.Right - rcCurTip.Right;
            int topDis = rcCurTip.Top - rcScreen.Top;
            int bottomDis = rcScreen.Bottom - rcCurTip.Bottom;

            //// 3.计算显示位置
            //// 在上方显示
            //if(topDis >= 0 && leftDis >= 0 && rightDis >= 0)
            //{
            //    rcCurTip.Offset(0, 0);
            //}
            //// 在下方显示
            //else if((bottomDis >= rcCurTip.Height / 2) && leftDis >= 0 && rightDis >= 0)
            //{
            //    rcCurTip.Offset(0, rcCurTip.Height);
            //}
            //// 在左边显示
            //else if(leftDis >= rcCurTip.Width / 2 && topDis >= 0)
            //{
            //    rcCurTip.Offset(-rcCurTip.Width / 2, 0);
            //}
            //// 在右边显示
            //else if(rightDis >= rcCurTip.Width / 2 && topDis > 0)
            //{
            //    rcCurTip.Offset(rcCurTip.Width / 2, 0);
            //}
            //// 在右上方显示
            //else if((topDis >= 0 && leftDis < 0 && rightDis >= rcCurTip.Width / 2) || (leftDis < 0))
            //{
            //    rcCurTip.Offset(rcCurTip.Width / 2, 0);
            //}
            //// 在左上方显示
            //else if((topDis >= 0 && leftDis >= rcCurTip.Width / 2 && rightDis < 0) || (rightDis < 0))
            //{
            //    rcCurTip.Offset(-rcCurTip.Width / 2, 0);
            //}
            //// 在右下方显示
            //else if(bottomDis >= rcCurTip.Height && leftDis < 0 && rightDis >= rcCurTip.Width / 2)
            //{
            //    rcCurTip.Offset(rcCurTip.Width / 2, rcCurTip.Height);
            //}
            //// 在左下方显示
            //else if((bottomDis >= rcCurTip.Height / 2) && leftDis >= rcCurTip.Width / 2 && rightDis < 0)
            //{
            //    rcCurTip.Offset(-rcCurTip.Width / 2, rcCurTip.Height);
            //}
            //// 默认在上方显示
            //else
            //{
            //    rcCurTip.Offset(0, 0);
            //}
            if (rcCurTip.Size.Width < 300)
            {
                rcCurTip.Size = new Size(300, rcCurTip.Height);
            }

            rcDest.Size = rcCurTip.Size;
            rcDest.Location = rcCurTip.Location;
        }

        bool ShowToolTip(string html)
        {
            try
            {
                this.Invoke(new Action(() =>
                {
                    if (!this.tipShow)
                    {
                        this.tipShow = true;
                        this.tip = new TipDlg();
                        this.tip.SetHtml(html);
                        Rectangle rcTip = new Rectangle();
                        AdjustTipPos(ref rcTip);
                        this.tip.Visible = true;
                        this.tip.Location = rcTip.Location;
                        this.tip.Size = rcTip.Size;
                        this.tip.Show();
                    }
                }));
                return true;
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("OverViewPage", "ShowToolTip()  error: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// 创建电池Html表
        /// </summary>
        /// <param name="bat">电池数据</param>
        /// <param name="maxRow">最大行</param>
        /// <param name="maxCol">最大列</param>
        /// <param name="rowDiv">行间距数</param>
        /// <returns></returns>
        string CreateBatRowCol(Battery[,] bat, int maxRow, int maxCol, int rowDiv)
        {
            string html = "";
            if (bat.Length > 0)
            {
                html = ("<table border=1 cellspacing=0 width = 50 border-collapse=collapse>");
                for (int col = 0; col < maxCol; col++)
                {
                    html += ("<tr>");
                    for (int row = 0; row < maxRow; row++)
                    {
                        if ((0 != row) && (0 == (row % rowDiv)))
                        {
                            html += ("<tr>");
                        }
                        string info = "";
                        if (0 == col)
                        {
                            info = (row + 1).ToString();
                        }
                        else if (0 == row)
                        {
                            info = (col + 1).ToString();
                        }
                        string code = string.IsNullOrEmpty(bat[row, col].Code) ? info : bat[row, col].Code;
                        switch (bat[row, col].Type)
                        {
                            case BatteryStatus.NG:                // NG
                                info = string.Format("<td style=\"color:red;\">{0}</td>", code);
                                break;
                            case BatteryStatus.Fake:              // 假电池
                            case BatteryStatus.Detect:            // 待检测假电池
                                info = string.Format("<td style=\"color:blue;\">{0}</td>", code);
                                break;
                            case BatteryStatus.ReFake:            // 回炉假电池
                                info = string.Format("<td style=\"color:darkcyan;\">{0}</td>", code);
                                break;
                            default:
                                info = string.Format("<td>{0}</td>", code);
                                break;
                        }
                        html += info;
                        if (0 == ((row + 1) % rowDiv))
                        {
                            html += ("</tr>");
                        }
                    }
                    html += ("</tr>");
                }
                html += ("</table>");
            }
            return html;
        }

        bool ShowPallet(string title, string subTitle, Pallet plt)
        {
            string html = string.Format("<table border=1 cellspacing=0><tr><th><b>【{0}】</b></th></tr>", title);
            html += string.Format("<tr><td align=center>{0}</td></tr>", subTitle);
            if (plt.State > PalletStatus.Invalid)
            {
                html += "<tr><td><ul>";
                string state, stage;
                state = stage = "";
                switch (plt.State)
                {
                    case PalletStatus.Invalid:
                        state = "无夹具";
                        break;
                    case PalletStatus.OK:
                        state = "有效OK";
                        break;
                    case PalletStatus.NG:
                        state = "有效NG";
                        break;
                    case PalletStatus.Detect:
                        state = "等待检测";
                        break;
                    case PalletStatus.WaitResult:
                        state = "等待结果";
                        break;
                    case PalletStatus.WaitOffload:
                        state = "等待下料";
                        break;
                    case PalletStatus.ReputFake:
                        state = "水含量超标，等待放回假电池";
                        break;
                    case PalletStatus.Rebaking:
                        state = "水含量超标，已放回假电池";
                        break;
                }
                switch (plt.Stage)
                {
                    case PalletStage.Invalid:
                        stage = "无效";
                        break;
                    case PalletStage.Onload:
                        stage = "上料完成";
                        break;
                    case PalletStage.Baked:
                        stage = "干燥完成";
                        break;
                    case PalletStage.Offload:
                        stage = "下料完成";
                        break;
                }
                stage += $" 有效电池:{plt.PalletBatCnt()}";

                html += $"<li>{state}[{(int)plt.State}]：{plt.Code}</li>";
                html += $"<li>{(plt.NeedFake ? "假电池夹具" : "正常夹具")}：{stage}[{(int)plt.Stage}]</li>";
                if (plt.SrcStation > -1)
                {
                    html += $"<li>来源：{plt.SrcStation} - {(plt.SrcRow + 1)} - {(plt.SrcCol + 1)}</li>";
                }
                html += $"<li>夹具加热次数：{plt.BakingCount.ToString()}</li>";
                if (plt.LogonCheckDate > DateTime.MinValue)
                {
                    html += $"<li>夹具入站时间：{plt.LogonCheckDate.ToString(Def.DateFormal)}</li>";
                }
                if (plt.StartDate > DateTime.MinValue)
                {
                    html += $"<li>加热开始时间：{plt.StartDate.ToString(Def.DateFormal)}</li>";
                }
                if (plt.EndDate > DateTime.MinValue)
                {
                    html += $"<li>加热结束时间：{plt.EndDate.ToString(Def.DateFormal)}</li>";
                }
                html += $"</ul></td></tr>";
                html += $"<tr><td>{CreateBatRowCol(plt.Battery, plt.MaxRow, plt.MaxCol, 5)}</td></tr>";
            }
            else
            {
                html += string.Format("<tr><td align=center>无夹具</td></tr>");
            }
            html += "</table>";
            return ShowToolTip(html);
        }

        bool ShowBattery(string title, Battery[] bat, int row, int col)
        {
            string html = string.Format("<table border=1 cellspacing=0><tr><th><b>【{0}】</b></th></tr>", title);
            if ((null != bat) && (bat.Length > 0))
            {
                Battery[,] batArray = new Battery[row, col];
                for (int j = 0; j < col; j++)
                {
                    for (int i = 0; i < row; i++)
                    {
                        batArray[i, j] = bat[j * row + i];
                    }
                }
                html += string.Format("<tr><td>{0}</td></tr>", CreateBatRowCol(batArray, row, col, row + 1));
            }
            else
            {
                html += string.Format("<tr><td align=center>无电池</td></tr>");
            }
            html += "</table>";
            return ShowToolTip(html);
        }

        bool ShowBatteryLine(string title, string subTitle, BatteryLine batLine)
        {
            string html = string.Format("<table border=1 cellspacing=0><tr><th><b>【{0}】</b></th></tr>", title);
            html += string.Format("<tr><td align=center>{0}</td></tr>", subTitle);

            html += "<tr><td><ul>";
            html += string.Format("<li>冷却系统：{0}行 - {1}列</li>", batLine.MaxRow, batLine.MaxCol);
            html += string.Format("</ul></td></tr>");

            html += string.Format("<tr><td>{0}</td></tr>", CreateBatRowCol(batLine.Battery, batLine.MaxRow, batLine.MaxCol, 5));
            html += "</table>";
            return ShowToolTip(html);
        }

        bool ShowCavity(string title, string subTitle, RunID id, int cavityIdx)
        {
            string html = $"<table border=1 cellspacing=0><tr><th><b>【{title}】</b></th></tr>";
            html += string.Format("<tr><td align=center>{0}</td></tr>", subTitle);
            html += "<tr><td><ul>";
            string state = "";
            switch (GetCavityState(id, cavityIdx))
            {
                case CavityStatus.Unknown:
                    state = "未知";
                    break;
                case CavityStatus.Normal:
                    state = "正常";
                    break;
                case CavityStatus.Heating:
                    state = "加热中";
                    break;
                case CavityStatus.WaitDetect:
                    state = "等待检测";
                    break;
                case CavityStatus.WaitResult:
                    state = "待上传水含量";
                    break;
                case CavityStatus.WaitRebaking:
                    state = "等待回炉";
                    break;
                case CavityStatus.Maintenance:
                    state = "维修状态";
                    break;
                default:
                    break;
            }
            if (GetOvenCavityTransfer(id, cavityIdx))
            {
                state = "转移状态";
            }
            uint[,] workTime = MachineCtrl.GetInstance().GetDryingOvenWorkTime();
            html += $"<li>腔体状态：{state}</li>";
            html += $"<li>工作时间：{workTime[(int)id - (int)RunID.DryOven0, cavityIdx]}分钟</li>";
            html += $"<li>抽检周期：{MachineCtrl.GetInstance().GetDryingOvenCavitySamplingCycle(id, cavityIdx)}</li>";
            html += $"<li>加热次数：{MachineCtrl.GetInstance().GetDryingOvenCavityHeartCycle(id, cavityIdx)}</li>";
            html += $"</ul></td></tr>";

            html += "</table>";
            return ShowToolTip(html);
        }

        bool ShowMesInfo(Point pt)
        {
            if (!MachineCtrl.GetInstance().McStopState(MachineCtrl.GetInstance().RunsCtrl.GetMCState()))
            {
                return false;
            }
            if (this.rectMesInfo.Contains(pt))
            {
                if (MachineCtrl.GetInstance().UpdataMes)
                {
                    if (MachineCtrl.GetInstance().dbRecord.UserLevel() > UserLevelType.USER_MAINTENANCE)
                    {
                        HelperLibrary.ShowMsgBox.Show("当前用户无权限修改MES状态", HelperLibrary.MessageType.MsgWarning, 5, DialogResult.OK);
                        return false;
                    }
                    else
                    {
                        if (DialogResult.Yes != HelperLibrary.ShowMsgBox.ShowDialog("是否确定修改MES状态为：离线生产？", HelperLibrary.MessageType.MsgQuestion, 5, DialogResult.No))
                        {
                            return false;
                        }
                    }
                }
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    MachineCtrl.GetInstance().LogingUi();
                }
                bool online = MachineCtrl.GetInstance().UpdataMes;
                MachineCtrl.GetInstance().UpdataMes = !online;
                for (MesInterface i = 0; i < MesInterface.End; i++)
                {
                    var cfg = MesDefine.GetMesCfg(i);
                    cfg.enable = !online;
                }
                return true;
            }
            return false;
        }

        bool ShowMesCheck(Point pt)
        {
            if (!MachineCtrl.GetInstance().McStopState(MachineCtrl.GetInstance().RunsCtrl.GetMCState()))
            {
                return false;
            }
            if (this.rectMesCheck.Contains(pt))
            {
                if (MachineCtrl.GetInstance().MesCheck)
                {
                    if (MachineCtrl.GetInstance().dbRecord.UserLevel() > UserLevelType.USER_MAINTENANCE)
                    {
                        HelperLibrary.ShowMsgBox.Show("当前用户无权限修改MES校验状态", HelperLibrary.MessageType.MsgWarning, 5, DialogResult.OK);
                        return false;
                    }
                    else
                    {
                        if (DialogResult.Yes != HelperLibrary.ShowMsgBox.ShowDialog("是否确定修改MES校验状态为：MES不校验？", HelperLibrary.MessageType.MsgQuestion, 5, DialogResult.No))
                        {
                            return false;
                        }
                    }
                }
                bool check = MachineCtrl.GetInstance().MesCheck;
                MachineCtrl.GetInstance().MesCheck = !check;
                return true;
            }
            return false;
        }

        bool ShowConveyerLineEN(Point pt)
        {
            if (0 != MachineCtrl.GetInstance().MachineID && 3 != MachineCtrl.GetInstance().MachineID)
                return false;

            if (this.rectConveyerLineEN.Contains(pt))
            {
                RunProcessOnloadScan run = MachineCtrl.GetInstance().GetModule(RunID.OnloadScan) as RunProcessOnloadScan;
                if ((null != run))
                {
                    if (run.conveyerLineEN)
                    {
                        if (DialogResult.Yes != HelperLibrary.ShowMsgBox.ShowDialog("是否确定修改联机上料为：手动上料？", HelperLibrary.MessageType.MsgQuestion, 5, DialogResult.No))
                        {
                            return false;
                        }
                        run.conveyerLineEN = false;
                    }
                    else
                    {
                        run.conveyerLineEN = true;
                    }
                    return true;
                }
            }
            return false;
        }

        bool ShowDevicestatusEN(Point pt)
        {
            if (!MachineCtrl.GetInstance().McStopState(MachineCtrl.GetInstance().RunsCtrl.GetMCState()))
            {
                return false;
            }
            if (this.rectDevicestatusEN.Contains(pt))
            {
                if (MachineCtrl.GetInstance().Devicestatus)
                {
                    if (MachineCtrl.GetInstance().dbRecord.UserLevel() > UserLevelType.USER_MAINTENANCE)
                    {
                        HelperLibrary.ShowMsgBox.Show("当前用户无权限修改MES任务状态，请点击左下角登录权限账户再操作", HelperLibrary.MessageType.MsgWarning, 5, DialogResult.OK);
                        return false;
                    }
                    if (string.IsNullOrEmpty(MesResources.Equipment.OperatorUserID) )
                    {
                        HelperLibrary.ShowMsgBox.Show("请先登录操作人员账号再修改MES任务状态", HelperLibrary.MessageType.MsgWarning, 5, DialogResult.OK);
                        return false;
                    }
                    else
                    {
                        if (DialogResult.Yes != HelperLibrary.ShowMsgBox.ShowDialog("是否确定切换MES任务状态<E:关闭>为：MES任务状态<U：中断>？", HelperLibrary.MessageType.MsgQuestion, 5, DialogResult.No))
                        {
                            return false;
                        }
                    }
                }

                bool check = MachineCtrl.GetInstance().Devicestatus;
                MachineCtrl.GetInstance().Devicestatus = !check;
                string usercode = MesResources.Equipment.OperatorUserID,oporder = MesResources.OpOrder,workplace = "DAL1HK01";
                string msg = "";
                if (!Jeve_Mes.Mes_LogoutOp(usercode, oporder, workplace, MachineCtrl.GetInstance().Devicestatus,ref msg))
                {
                    MachineCtrl.GetInstance().Devicestatus = check;
                    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                }
                //if (!MachineCtrl.GetInstance().ACEQPTSTUS_Main(MesResources.Equipment, MachineCtrl.GetInstance().Devicestatus, , ref msg))
                //{
                //    ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                //}

                return true;
            }
            return false;
        }

        bool ShowOnload(Point pt)
        {
            RunID id = RunID.OnloadRobot;
            // 上料夹具
            for (int i = 0; i < this.rectOnloadRbtPlt.Length; i++)
            {
                if (this.rectOnloadRbtPlt[i].Contains(pt))
                {
                    return ShowPallet(ModuleName(id), "夹具" + (i + 1), ModulePallet(id)[i]);
                }
            }
            Battery[] arrBat = ModuleBattery(id);
            // 暂存
            if (this.rectOnloadRbtBuffer.Contains(pt))
            {
                Battery[] aBat = new Battery[arrBat.Length / 2];
                for (int idx = 0; idx < arrBat.Length / 2; idx++)
                {
                    aBat[idx] = arrBat[(arrBat.Length / 2) + idx];
                }
                return ShowBattery(ModuleName(id) + " - 暂存", aBat, 1, aBat.Length);
            }
            // 抓手
            if (this.rectOnloadRbtFinger.Contains(pt))
            {
                Battery[] aBat = new Battery[arrBat.Length / 2];
                for (int idx = 0; idx < arrBat.Length / 2; idx++)
                {
                    aBat[idx] = arrBat[idx];
                }
                return ShowBattery(ModuleName(id) + " - 抓手", aBat, 1, aBat.Length);
            }

            Battery[] modBat = null;
            // 来料线
            id = RunID.OnloadLine;
            if (this.rectOnloadLine.Contains(pt))
            {
                modBat = ModuleBattery(id);
                return ShowBattery(ModuleName(id), modBat, 1, modBat.Length);
            }
            // 来料接收电池
            id = RunID.OnloadRecv;
            if (this.rectOnloadRecv.Contains(pt))
            {
                modBat = ModuleBattery(id);
                return ShowBattery(ModuleName(id), modBat, 1, modBat.Length);
            }
            // 来料扫码
            id = RunID.OnloadScan;
            if (this.rectOnloadScan.Contains(pt))
            {
                modBat = ModuleBattery(id);
                return ShowBattery("来料扫码", modBat, 1, modBat.Length);
            }
            // NG输出
            id = RunID.OnloadNG;
            if (this.rectOnloadNG.Contains(pt))
            {
                modBat = ModuleBattery(id);
                return ShowBattery(ModuleName(id), modBat, modBat.Length, 1);
            }
            // 上假电池
            id = RunID.OnloadFake;
            if (this.rectOnloadFake.Contains(pt))
            {
                return ShowBattery(ModuleName(id), ModuleBattery(id), 4, 4);
            }
            // 测试假电池
            id = RunID.OnloadDetect;
            if (this.rectOnloadDetect.Contains(pt))
            {
                return ShowBattery("待测试假电池输出", ModuleBattery(id), 1, 1);
            }

            return false;
        }

        bool ShowManualOperate(Point pt)
        {
            RunID id = RunID.ManualOperate;
            // 人工操作台夹具
            if (this.rectManualOperate.Contains(pt))
            {
                return ShowPallet("人工操作台", "夹具状态", ModulePallet(id)[0]);
            }
            return false;
        }

        bool ShowOven(Point pt)
        {
            int ovenCount = (int)OvenInfoCount.OvenCount;
            for (int ovenIdx = 0; ovenIdx < ovenCount; ovenIdx++)
            {
                RunID id = RunID.DryOven0 + ovenIdx;
                // 干燥炉夹具
                for (int i = 0; i < this.rectDryOvenPlt.GetLength(1); i++)
                {
                    if (this.rectDryOvenPlt[ovenIdx, i].Contains(pt))
                    {
                        return ShowPallet(ModuleName(id), string.Format("{0}层夹具{1}", (i / 2 + 1), (i % 2 + 1)), ModulePallet(id)[i]);
                    }
                }
                // 干燥炉腔体
                for (int i = 0; i < this.rectDryOvenCavity.GetLength(1); i++)
                {
                    if (this.rectDryOvenCavity[ovenIdx, i].Contains(pt))
                    {
                        return ShowCavity(ModuleName(id), string.Format("{0}层腔体", (i + 1)), id, i);
                    }
                }
            }

            return false;
        }

        bool GetOvenId(Point pt, ref RunID runId, ref int cavityId)
        {
            int ovenCount = (int)OvenInfoCount.OvenCount;
            for (int ovenIdx = 0; ovenIdx < ovenCount; ovenIdx++)
            {
                RunID id = RunID.DryOven0 + ovenIdx;
                // 干燥炉腔体
                for (int i = 0; i < this.rectDryOvenCavity.GetLength(1); i++)
                {
                    if (this.rectDryOvenCavity[ovenIdx, i].Contains(pt))
                    {
                        cavityId = i;
                        runId = id;
                        return true;
                    }
                }
            }

            return false;
        }

        bool ShowPalletBuffer(Point pt)
        {
            RunID id = RunID.PalletBuffer;
            // 夹具缓存架夹具
            for (int i = 0; i < this.rectPltBufferPlt.Length; i++)
            {
                if (this.rectPltBufferPlt[i].Contains(pt))
                {
                    return ShowPallet(ModuleName(id), string.Format("{0}层夹具", (i + 1)), ModulePallet(id)[i]);
                }
            }
            return false;
        }

        bool ShowTransfer(Point pt)
        {
            RunID id = RunID.Transfer;
            // 调度机器人夹具
            if (this.rectTransfer.Contains(pt))
            {
                return ShowPallet(ModuleName(id), "插料架夹具", ModulePallet(id)[0]);
            }
            return false;
        }

        bool ShowOffLoad(Point pt)
        {
            RunID id = RunID.OffloadBattery;
            // 下料夹具
            for (int i = 0; i < this.rectOffloadBatPlt.Length; i++)
            {
                if (this.rectOffloadBatPlt[i].Contains(pt))
                {
                    return ShowPallet(ModuleName(id), "夹具" + (i + 1), ModulePallet(id)[i]);
                }
            }
            Battery[] arrBat = ModuleBattery(id);
            // 暂存
            if (this.rectOffloadBatBuffer.Contains(pt))
            {
                return ShowBattery(ModuleName(id) + " - 暂存", (new Battery[] { arrBat[4], arrBat[5], arrBat[6], arrBat[7] }), 2, 2);
            }
            // 抓手
            if (this.rectOffloadBatFinger.Contains(pt))
            {
                return ShowBattery(ModuleName(id) + " - 抓手", (new Battery[] { arrBat[0], arrBat[1], arrBat[2], arrBat[3] }), 2, 2);
            }

            // 下料线
            if (this.rectOffloadLine.Contains(pt))
            {
                id = RunID.OffloadLine;
                return ShowBattery(ModuleName(id), ModuleBattery(id), 2, 2);
            }

            // 下料缓存线
            int offbufferCnt = MachineCtrl.GetInstance().OffloadBuffers;
            for (int idx = 0; idx < offbufferCnt; idx++)
            {
                if (this.rectOfflineBuffer[idx].Contains(pt))
                {
                    id = RunID.OffloadBuffer + idx;
                    return ShowBattery(ModuleName(id), ModuleBattery(id), 2, 2);
                }
            }

            // 下料出料线
            if (this.rectOffloadOut.Contains(pt))
            {
                id = RunID.OffloadOut;
                return ShowBattery(ModuleName(id), ModuleBattery(id), 2, 2);
            }

            //Battery[] modBat = null;
            // 下待测假电池线
            //id = RunID.OffloadDetect;
            //if(this.rectOffloadDetect.Contains(pt))
            //{
            //    modBat = ModuleBattery(id);
            //    return ShowBattery(ModuleName(id), modBat, modBat.Length, 1);
            //}
            //// 下料NG输出
            //id = RunID.OffloadNG;
            //if(this.rectOffloadNG.Contains(pt))
            //{
            //    modBat = ModuleBattery(id);
            //    return ShowBattery(ModuleName(id), modBat, modBat.Length, 1);
            //}
            // 冷却下料
            id = RunID.CoolingOffload;
            arrBat = ModuleBattery(id);
            // 暂存
            if (this.rectCoolingBuffer.Contains(pt))
            {
                return ShowBattery(ModuleName(id) + " - 暂存", (new Battery[] { arrBat[4], arrBat[5], arrBat[6], arrBat[7] }), 2, 1);
            }
            // 抓手
            if (this.rectCoolingFinger.Contains(pt))
            {
                return ShowBattery(ModuleName(id) + " - 抓手", (new Battery[] { arrBat[0], arrBat[1], arrBat[2], arrBat[3] }), 2, 2);
            }
            // 冷却系统
            id = RunID.CoolingSystem;
            if (this.rectCoolingSystem.Contains(pt))
            {
                BatteryLine batLine = ModuleBatteryLine(id);
                if (null != batLine)
                {
                    return ShowBatteryLine(ModuleName(id), "电池", batLine);
                }
            }
            return false;
        }

        #endregion

        #region // 计数表

        /// <summary>
        /// 创建计数表
        /// </summary>
        private void CreateTotalDataView()
        {
            // 设置表格
            DataGridViewNF dgv = this.dataGridViewTotalData;
            dgv.SetViewStatus();
            dgv.RowHeadersVisible = false;          // 行表头不可见
            dgv.ColumnHeadersVisible = false;       // 列表头不可见
                                                    // 项
            int idx = dgv.Columns.Add("key", "项");
            dgv.Columns[idx].FillWeight = 65;     // 宽度占比权重
            idx = dgv.Columns.Add("value", "数");
            dgv.Columns[idx].FillWeight = 35;
            foreach (DataGridViewColumn item in dgv.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
            }
            // 添加行数据
            dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "计数：";
            //dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "上料计数";
            //dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "上料扫码NG";
            //dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "扫码枪1-NG";
            //dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "扫码枪2-NG";
            //dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "扫码枪3-NG";
            //dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "扫码枪4-NG";
            //dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "下料计数";
            dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "烘烤NG";

            idx = dgv.Rows.Add();
            dgv.Rows[idx].Cells[0].Value = "干燥出炉时间：";
            dgv.Rows[idx].Cells[1].Value = "分钟min";
            for (int id = 0; id < (int)OvenInfoCount.OvenCount; id++)
            {
                for (int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                {
                    dgv.Rows[dgv.Rows.Add()].Cells[0].Value = "";
                }
            }

            for (int i = 0; i < dgv.RowCount; i++)
            {
                dgv.Rows[i].Height = 30;
            }

            // 添加用户管理右键菜单
            ContextMenuStrip cms = new ContextMenuStrip();
            cms.Items.Add("清除全部计数");
            cms.Items[0].Click += OverViewPage_Click_ClearTotalData;
            this.dataGridViewTotalData.ContextMenuStrip = cms;
        }

        private void UpdataTotalData()
        {
            try
            {
                // 使用委托更新UI
                this.Invoke(new Action(() =>
                {
                    if (!this.updating)
                    {
                        this.updating = true;
                        int idx = 1;
                        DataGridViewNF dgv = this.dataGridViewTotalData;
                        //dgv.Rows[idx++].Cells[1].Value = TotalData.OnloadCount;
                        //dgv.Rows[idx++].Cells[1].Value = TotalData.OnScanNGCount;
                        //dgv.Rows[idx++].Cells[1].Value = TotalData.OnScan1NGCount;
                        //dgv.Rows[idx++].Cells[1].Value = TotalData.OnScan2NGCount;
                        //dgv.Rows[idx++].Cells[1].Value = TotalData.OnScan3NGCount;
                        //dgv.Rows[idx++].Cells[1].Value = TotalData.OnScan4NGCount;
                        //dgv.Rows[idx++].Cells[1].Value = TotalData.OffloadCount;
                        dgv.Rows[idx++].Cells[1].Value = TotalData.BakedNGCount;
                        idx++;
                        uint[,] workTime = MachineCtrl.GetInstance().GetDryingOvenWorkTime();
                        Dictionary<string, uint> workInfo = new Dictionary<string, uint>();
                        for (int id = 0; id < (int)OvenInfoCount.OvenCount; id++)
                        {
                            for (int row = 0; row < (int)OvenRowCol.MaxRow; row++)
                            {
                                workInfo.Add(string.Format("{0}炉 {1}层", id + 1, row + 1), workTime[id, row]);
                                //dgv.Rows[idx].Cells[0].Value = string.Format("{0}炉 {1}层", id + 1, row + 1);
                                //dgv.Rows[idx++].Cells[1].Value = workTime[id, row];
                            }
                        }
                        var result = workInfo.OrderByDescending(p => p.Value).ToDictionary(p => p.Key, o => o.Value);
                        foreach (var item in result)
                        {
                            dgv.Rows[idx].Cells[0].Value = item.Key;
                            dgv.Rows[idx++].Cells[1].Value = item.Value;
                        }
                        this.updating = false;
                    }
                }));
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("OverViewPage.UpdataTotalData() ", $"{ex.Message}\r\n{ex.StackTrace}", HelperLibrary.LogType.Error);
            }
        }

        private void dataGridViewTotalData_MouseDown(object sender, MouseEventArgs e)
        {
            if (MouseButtons.Right == e.Button)
            {
                MCState mcState = MachineCtrl.GetInstance().RunsCtrl.GetMCState();
                bool enable = ((MCState.MCInitializing != mcState) && (MCState.MCRunning != mcState));
                // 添加判断启用哪些右键菜单
                DataGridViewRow dgvRow = this.dataGridViewTotalData.CurrentRow;
                this.dataGridViewTotalData.ContextMenuStrip.Items[0].Enabled = (enable && (null != dgvRow)); // 清除计数
            }
        }

        private void OverViewPage_Click_ClearTotalData(object sender, EventArgs e)
        {
            TotalData.ClearTotalData();
            TotalData.WriteTotalData();
        }

        #endregion

    }
}
