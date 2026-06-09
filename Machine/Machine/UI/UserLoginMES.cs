using HelperLibrary;
using Machine.Framework.Mes;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Machine
{
    public partial class UserLoginMES : Form
    {
        public UserLoginMES()
        {
            InitializeComponent();

            // 创建用户管理视图
            CreateUserManagerView();
        }

        #region // 字段

        TextBox txtUserID;
        TextBox txtUserPW;

        #endregion

        /// <summary>
        /// 创建用户登录视图
        /// </summary>
        private void CreateUserManagerView()
        {
            this.TopLevel = true;

            int row, col, index;
            float fHig = (float)0.0;
            row = col = index = 0;

            // 设置表
            row = 5;
            col = 4;
            fHig = (float)(100.0 / row);
            this.tablePanelUserLogin.RowCount = row;
            this.tablePanelUserLogin.ColumnCount = col;
            this.tablePanelUserLogin.Padding = new Padding(0, 20, 0, 0);
            // 设置行列风格
            for(int i = 0; i < this.tablePanelUserLogin.RowCount; i++)
            {
                if(i < this.tablePanelUserLogin.RowStyles.Count)
                {
                    this.tablePanelUserLogin.RowStyles[i] = new RowStyle(SizeType.Percent, fHig);
                }
                else
                {
                    this.tablePanelUserLogin.RowStyles.Add(new RowStyle(SizeType.Percent, fHig));
                }
            }
            for(int i = this.tablePanelUserLogin.ColumnStyles.Count; i < col; i++)
            {
                this.tablePanelUserLogin.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, fHig));
            }
            this.tablePanelUserLogin.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 1));
            this.tablePanelUserLogin.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 3));
            this.tablePanelUserLogin.ColumnStyles[2] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 5));
            this.tablePanelUserLogin.ColumnStyles[3] = new ColumnStyle(SizeType.Percent, (float)(100.0 / 10 * 1));
            // 添加控件
            index = 0;
            Label lbl = new Label();
            lbl.Text = "用户名：";
            this.tablePanelUserLogin.Controls.Add(lbl, 1, index);
            this.txtUserID = new TextBox();
            this.txtUserID.Text = string.IsNullOrEmpty(MesResources.Equipment.OperatorUserID) ? string.Empty : MesResources.Equipment.OperatorUserID; ;
            this.tablePanelUserLogin.Controls.Add(this.txtUserID, 2, index++);
            lbl = new Label();
            lbl.Text = "密码：";
            this.tablePanelUserLogin.Controls.Add(lbl, 1, index);
            this.txtUserPW = new TextBox();
            this.txtUserPW.Text = string.IsNullOrEmpty(MesResources.Equipment.OperatorPassword) ? string.Empty : MesResources.Equipment.OperatorPassword;
            //this.txtUserPW.UseSystemPasswordChar = true;
            this.txtUserPW.PasswordChar = '*';
            this.tablePanelUserLogin.Controls.Add(this.txtUserPW, 2, index++);
            index++;    // 间隔一行
            Button btn = new Button();
            btn.Text = "登  录";
            btn.Click += Btn_Click_Login;
            this.AcceptButton = btn;    // 接收Enter按键
            this.tablePanelUserLogin.Controls.Add(btn, 1, index++);

            foreach(Control item in this.tablePanelUserLogin.Controls)
            {
                if((item is Label) || (item is TextBox) || (item is ComboBox))
                {
                    item.Font = new Font(item.Font.FontFamily, 12);
                    item.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                }
                else if(item is Button)
                {
                    this.tablePanelUserLogin.SetColumnSpan(item, 2);
                    item.Dock = DockStyle.Fill;
                }
            }

        }

        /// <summary>
        /// 登录用户
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_Click_Login(object sender, EventArgs e)
        {
            string msg = "";

            string workPlace = "DAL1HK01";
            UserInfo userInfo = new UserInfo();

            if (Jeve_Mes.Mes_CheckUser(this.txtUserID.Text, workPlace,ref userInfo,ref msg))
            {
                MesResources.MesLogin = true;
                DialogResult = DialogResult.OK;
                ShowMsgBox.ShowDialog($"MES登录成功！ \r\n{msg}", MessageType.MsgMessage);
                MesResources.Equipment.OperatorUserID = this.txtUserID.Text;
                MesResources.Equipment.OperatorPassword = this.txtUserPW.Text;
                MesResources.WriteConfig();
            }
            else
            {
                ShowMsgBox.ShowDialog($"登录验证失败！\r\n{msg}", MessageType.MsgAlarm);
            }

        }
    }
}
