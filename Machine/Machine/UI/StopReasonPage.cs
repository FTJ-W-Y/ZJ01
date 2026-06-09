using HelperLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Machine
{
    public partial class StopReasonPage : Form
    {
        public StopReasonPage()
        {
            InitializeComponent();

            CreateStopReasonView();
        }

        #region // 字段

        ComboBox cmbStopReasonList;

        #endregion

        /// <summary>
        /// 创建用户登录视图
        /// </summary>
        private void CreateStopReasonView()
        {
            this.TopLevel = true;

            int row, col, index;
            float fHig = (float)0.0;
            row = col = index = 0;

            // 设置表
            row = 5;
            col = 4;
            fHig = (float)(100.0 / row);
            this.tablePanelStopReason.RowCount = row;
            this.tablePanelStopReason.ColumnCount = col;
            this.tablePanelStopReason.Padding = new Padding(0, 20, 0, 0);
            // 设置行列风格
            for(int i = 0; i < this.tablePanelStopReason.RowCount; i++)
            {
                if(i < this.tablePanelStopReason.RowStyles.Count)
                {
                    this.tablePanelStopReason.RowStyles[i] = new RowStyle(SizeType.Percent, fHig);
                }
                else
                {
                    this.tablePanelStopReason.RowStyles.Add(new RowStyle(SizeType.Percent, fHig));
                }
            }
            for(int i = this.tablePanelStopReason.ColumnStyles.Count; i < col; i++)
            {
                this.tablePanelStopReason.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, fHig));
            }
            this.tablePanelStopReason.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 1));
            this.tablePanelStopReason.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 3));
            this.tablePanelStopReason.ColumnStyles[2] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 5));
            this.tablePanelStopReason.ColumnStyles[3] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 1));
            // 添加控件
            index = 0;
            Label lbl = new Label();
            lbl.Text = "停机原因：";
            this.tablePanelStopReason.Controls.Add(lbl, 1, index);
            this.cmbStopReasonList = new ComboBox();
            this.cmbStopReasonList.Sorted = false;
            this.cmbStopReasonList.DropDownStyle = ComboBoxStyle.DropDownList;
            this.tablePanelStopReason.Controls.Add(this.cmbStopReasonList, 2, index++);
            lbl = new Label();
            lbl.Text = $"开始时间：{MachineCtrl.GetInstance().McStopTime.ToString(Def.DateFormal)}";
            this.tablePanelStopReason.Controls.Add(lbl, 1, index);
            this.tablePanelStopReason.SetColumnSpan(lbl, 2);
            index += 2;    // 间隔一行
            Button btn = new Button();
            btn.Text = "上报MES";
            btn.Click += Btn_Click_UpdataMES;
            this.AcceptButton = btn;    // 接收Enter按键
            this.tablePanelStopReason.Controls.Add(btn, 1, index++);

            foreach(Control item in this.tablePanelStopReason.Controls)
            {
                if((item is Label) || (item is TextBox) || (item is ComboBox))
                {
                    item.Font = new Font(item.Font.FontFamily, 12);
                    item.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                }
                else if(item is Button)
                {
                    this.tablePanelStopReason.SetColumnSpan(item, 2);
                    item.Dock = DockStyle.Fill;
                }
            }

            // 填充停机原因
            this.cmbStopReasonList.Items.Add("1：待料;");
            this.cmbStopReasonList.Items.Add("2：吃饭;");
            this.cmbStopReasonList.Items.Add("3：换型;");
            this.cmbStopReasonList.Items.Add("4：设备故障;");
            this.cmbStopReasonList.Items.Add("5：来料不良;");
            this.cmbStopReasonList.Items.Add("6：设备校验;");
            this.cmbStopReasonList.Items.Add("7：首件/点检;");
            this.cmbStopReasonList.Items.Add("8：品质异常;");
            this.cmbStopReasonList.Items.Add("9：堆料;");
            this.cmbStopReasonList.Items.Add("10：环境异常;");
            this.cmbStopReasonList.Items.Add("11：设备信息不完善;");
        }

        /// <summary>
        /// 上报MES
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Click_UpdataMES(object sender, EventArgs e)
        {
            string msg = "";
            EquDownReason equDown = (EquDownReason)(this.cmbStopReasonList.SelectedIndex + 1);
            if (MesOperate.EquToMesDownReason(MesResources.Equipment, equDown, ref msg))
            {
                DialogResult = DialogResult.OK;
                
                IOTData pdata = new IOTData();
                pdata.line = MachineCtrl.GetInstance().LineID;
                pdata.equip = MachineCtrl.GetInstance().machineServerIP;
                pdata.floor = "0";
                pdata.datetime = DateTime.Now;
                pdata.points = new List<PointData>();

                PointData points = new PointData();
                points.code = "HHKD2017";
                points.name = "停机原因详情";
                points.type = "String";
                points.unit = "";
                points.value = $"{cmbStopReasonList.Text}";
                pdata.points.Add(points);  

                IOTTaskList.Add(pdata);
            }
            else
            {
                ShowMsgBox.ShowDialog($"无法上报MES！\r\n{msg}", MessageType.MsgAlarm);
            }
        }
    }
}
