namespace Machine
{
    partial class OtherDebugPage
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.labelFinger1 = new System.Windows.Forms.Label();
            this.labelFinger3 = new System.Windows.Forms.Label();
            this.labelFinger4 = new System.Windows.Forms.Label();
            this.labelFinger2 = new System.Windows.Forms.Label();
            this.btnOffloadOutFinish = new System.Windows.Forms.Button();
            this.btnFingerTemper = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbPalletId = new System.Windows.Forms.ComboBox();
            this.checkPalletFake = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbRow = new System.Windows.Forms.ComboBox();
            this.cmbCol = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.btnAddPalletBat = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonRBackoff = new System.Windows.Forms.Button();
            this.buttonRForward = new System.Windows.Forms.Button();
            this.btnAXsend = new System.Windows.Forms.Button();
            this.btnAXRecv = new System.Windows.Forms.Button();
            this.btnYAXsend = new System.Windows.Forms.Button();
            this.btnYAXrecv = new System.Windows.Forms.Button();
            this.buttonRHome = new System.Windows.Forms.Button();
            this.StopFord = new System.Windows.Forms.Button();
            this.StopBack = new System.Windows.Forms.Button();
            this.checkWritePlcLog = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonPalletNG = new System.Windows.Forms.Button();
            this.buttonPalletFull = new System.Windows.Forms.Button();
            this.buttonPalletClear = new System.Windows.Forms.Button();
            this.buttonPalletAdd = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.comboBoxPalletModule = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.comboBoxPalletID = new System.Windows.Forms.ComboBox();
            this.groupBoxServer = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonClientReconnect = new System.Windows.Forms.Button();
            this.buttonServerRestart = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.groupBoxServer.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox4);
            this.panel1.Controls.Add(this.groupBox3);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.groupBoxServer);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(750, 443);
            this.panel1.TabIndex = 0;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.tableLayoutPanel6);
            this.groupBox4.Location = new System.Drawing.Point(9, 227);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.groupBox4.Size = new System.Drawing.Size(228, 197);
            this.groupBox4.TabIndex = 9;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "冷却下料";
            // 
            // tableLayoutPanel6
            // 
            this.tableLayoutPanel6.ColumnCount = 3;
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel6.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel6.Controls.Add(this.labelFinger1, 0, 1);
            this.tableLayoutPanel6.Controls.Add(this.labelFinger3, 0, 2);
            this.tableLayoutPanel6.Controls.Add(this.labelFinger4, 2, 2);
            this.tableLayoutPanel6.Controls.Add(this.labelFinger2, 2, 1);
            this.tableLayoutPanel6.Controls.Add(this.btnOffloadOutFinish, 0, 0);
            this.tableLayoutPanel6.Controls.Add(this.btnFingerTemper, 0, 3);
            this.tableLayoutPanel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel6.Location = new System.Drawing.Point(2, 22);
            this.tableLayoutPanel6.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            this.tableLayoutPanel6.RowCount = 5;
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel6.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel6.Size = new System.Drawing.Size(224, 173);
            this.tableLayoutPanel6.TabIndex = 7;
            // 
            // labelFinger1
            // 
            this.labelFinger1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFinger1.AutoSize = true;
            this.labelFinger1.Font = new System.Drawing.Font("宋体", 11F);
            this.labelFinger1.Location = new System.Drawing.Point(2, 43);
            this.labelFinger1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelFinger1.Name = "labelFinger1";
            this.labelFinger1.Size = new System.Drawing.Size(70, 15);
            this.labelFinger1.TabIndex = 7;
            this.labelFinger1.Text = "抓1";
            this.labelFinger1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelFinger3
            // 
            this.labelFinger3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFinger3.AutoSize = true;
            this.labelFinger3.Font = new System.Drawing.Font("宋体", 11F);
            this.labelFinger3.Location = new System.Drawing.Point(2, 77);
            this.labelFinger3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelFinger3.Name = "labelFinger3";
            this.labelFinger3.Size = new System.Drawing.Size(70, 15);
            this.labelFinger3.TabIndex = 7;
            this.labelFinger3.Text = "抓3";
            this.labelFinger3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelFinger4
            // 
            this.labelFinger4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFinger4.AutoSize = true;
            this.labelFinger4.Font = new System.Drawing.Font("宋体", 11F);
            this.labelFinger4.Location = new System.Drawing.Point(150, 77);
            this.labelFinger4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelFinger4.Name = "labelFinger4";
            this.labelFinger4.Size = new System.Drawing.Size(72, 15);
            this.labelFinger4.TabIndex = 7;
            this.labelFinger4.Text = "抓4";
            this.labelFinger4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelFinger2
            // 
            this.labelFinger2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFinger2.AutoSize = true;
            this.labelFinger2.Font = new System.Drawing.Font("宋体", 11F);
            this.labelFinger2.Location = new System.Drawing.Point(150, 43);
            this.labelFinger2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelFinger2.Name = "labelFinger2";
            this.labelFinger2.Size = new System.Drawing.Size(72, 15);
            this.labelFinger2.TabIndex = 7;
            this.labelFinger2.Text = "抓2";
            this.labelFinger2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnOffloadOutFinish
            // 
            this.tableLayoutPanel6.SetColumnSpan(this.btnOffloadOutFinish, 2);
            this.btnOffloadOutFinish.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnOffloadOutFinish.Location = new System.Drawing.Point(3, 3);
            this.btnOffloadOutFinish.Name = "btnOffloadOutFinish";
            this.btnOffloadOutFinish.Size = new System.Drawing.Size(142, 28);
            this.btnOffloadOutFinish.TabIndex = 9;
            this.btnOffloadOutFinish.Text = "置出料电池完成";
            this.btnOffloadOutFinish.UseVisualStyleBackColor = true;
            this.btnOffloadOutFinish.Visible = false;
            this.btnOffloadOutFinish.Click += new System.EventHandler(this.btnOffloadOutFinish_Click);
            // 
            // btnFingerTemper
            // 
            this.tableLayoutPanel6.SetColumnSpan(this.btnFingerTemper, 2);
            this.btnFingerTemper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnFingerTemper.Location = new System.Drawing.Point(3, 105);
            this.btnFingerTemper.Name = "btnFingerTemper";
            this.btnFingerTemper.Size = new System.Drawing.Size(142, 28);
            this.btnFingerTemper.TabIndex = 8;
            this.btnFingerTemper.Text = "夹爪温度";
            this.btnFingerTemper.UseVisualStyleBackColor = true;
            this.btnFingerTemper.Click += new System.EventHandler(this.btnFingerTemper_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.tableLayoutPanel5);
            this.groupBox3.Location = new System.Drawing.Point(499, 11);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.groupBox3.Size = new System.Drawing.Size(228, 197);
            this.groupBox3.TabIndex = 9;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "托盘添加电池";
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 3;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel5.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.cmbPalletId, 1, 0);
            this.tableLayoutPanel5.Controls.Add(this.checkPalletFake, 2, 0);
            this.tableLayoutPanel5.Controls.Add(this.label6, 0, 1);
            this.tableLayoutPanel5.Controls.Add(this.cmbRow, 1, 1);
            this.tableLayoutPanel5.Controls.Add(this.cmbCol, 1, 2);
            this.tableLayoutPanel5.Controls.Add(this.label7, 0, 2);
            this.tableLayoutPanel5.Controls.Add(this.label8, 2, 2);
            this.tableLayoutPanel5.Controls.Add(this.label9, 2, 1);
            this.tableLayoutPanel5.Controls.Add(this.btnAddPalletBat, 1, 3);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(2, 22);
            this.tableLayoutPanel5.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 5;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(224, 173);
            this.tableLayoutPanel5.TabIndex = 7;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 11F);
            this.label1.Location = new System.Drawing.Point(2, 9);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 15);
            this.label1.TabIndex = 7;
            this.label1.Text = "托盘编号";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbPalletId
            // 
            this.cmbPalletId.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPalletId.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPalletId.Font = new System.Drawing.Font("宋体", 11F);
            this.cmbPalletId.FormattingEnabled = true;
            this.cmbPalletId.Items.AddRange(new object[] {
            "1",
            "2"});
            this.cmbPalletId.Location = new System.Drawing.Point(76, 5);
            this.cmbPalletId.Margin = new System.Windows.Forms.Padding(2);
            this.cmbPalletId.Name = "cmbPalletId";
            this.cmbPalletId.Size = new System.Drawing.Size(70, 23);
            this.cmbPalletId.TabIndex = 0;
            // 
            // checkPalletFake
            // 
            this.checkPalletFake.AutoSize = true;
            this.checkPalletFake.Location = new System.Drawing.Point(151, 3);
            this.checkPalletFake.Name = "checkPalletFake";
            this.checkPalletFake.Size = new System.Drawing.Size(60, 16);
            this.checkPalletFake.TabIndex = 9;
            this.checkPalletFake.Text = "假电芯";
            this.checkPalletFake.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("宋体", 11F);
            this.label6.Location = new System.Drawing.Point(2, 43);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(70, 15);
            this.label6.TabIndex = 7;
            this.label6.Text = "电池满";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // cmbRow
            // 
            this.cmbRow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbRow.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRow.Font = new System.Drawing.Font("宋体", 11F);
            this.cmbRow.FormattingEnabled = true;
            this.cmbRow.Location = new System.Drawing.Point(76, 39);
            this.cmbRow.Margin = new System.Windows.Forms.Padding(2);
            this.cmbRow.Name = "cmbRow";
            this.cmbRow.Size = new System.Drawing.Size(70, 23);
            this.cmbRow.TabIndex = 0;
            // 
            // cmbCol
            // 
            this.cmbCol.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbCol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCol.Font = new System.Drawing.Font("宋体", 11F);
            this.cmbCol.FormattingEnabled = true;
            this.cmbCol.Location = new System.Drawing.Point(76, 73);
            this.cmbCol.Margin = new System.Windows.Forms.Padding(2);
            this.cmbCol.Name = "cmbCol";
            this.cmbCol.Size = new System.Drawing.Size(70, 23);
            this.cmbCol.TabIndex = 0;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("宋体", 11F);
            this.label7.Location = new System.Drawing.Point(2, 77);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(70, 15);
            this.label7.TabIndex = 7;
            this.label7.Text = "不满行";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("宋体", 11F);
            this.label8.Location = new System.Drawing.Point(150, 77);
            this.label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(72, 15);
            this.label8.TabIndex = 7;
            this.label8.Text = "个电池";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("宋体", 11F);
            this.label9.Location = new System.Drawing.Point(150, 43);
            this.label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(72, 15);
            this.label9.TabIndex = 7;
            this.label9.Text = "行";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnAddPalletBat
            // 
            this.btnAddPalletBat.Location = new System.Drawing.Point(77, 105);
            this.btnAddPalletBat.Name = "btnAddPalletBat";
            this.btnAddPalletBat.Size = new System.Drawing.Size(68, 28);
            this.btnAddPalletBat.TabIndex = 4;
            this.btnAddPalletBat.Text = "添加";
            this.btnAddPalletBat.UseVisualStyleBackColor = true;
            this.btnAddPalletBat.Click += new System.EventHandler(this.btnAddPalletBat_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tableLayoutPanel3);
            this.groupBox2.Location = new System.Drawing.Point(256, 10);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.groupBox2.Size = new System.Drawing.Size(228, 197);
            this.groupBox2.TabIndex = 9;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "冷却系统R轴调试";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel3.Controls.Add(this.buttonRBackoff, 0, 4);
            this.tableLayoutPanel3.Controls.Add(this.buttonRForward, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.btnAXsend, 1, 3);
            this.tableLayoutPanel3.Controls.Add(this.btnAXRecv, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.btnYAXsend, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.btnYAXrecv, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.buttonRHome, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.StopFord, 2, 2);
            this.tableLayoutPanel3.Controls.Add(this.StopBack, 2, 4);
            this.tableLayoutPanel3.Controls.Add(this.checkWritePlcLog, 2, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(2, 22);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 5;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(224, 173);
            this.tableLayoutPanel3.TabIndex = 7;
            // 
            // buttonRBackoff
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonRBackoff, 2);
            this.buttonRBackoff.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonRBackoff.Location = new System.Drawing.Point(2, 138);
            this.buttonRBackoff.Margin = new System.Windows.Forms.Padding(2);
            this.buttonRBackoff.Name = "buttonRBackoff";
            this.buttonRBackoff.Size = new System.Drawing.Size(144, 33);
            this.buttonRBackoff.TabIndex = 3;
            this.buttonRBackoff.Text = "R轴后退一行";
            this.buttonRBackoff.UseVisualStyleBackColor = true;
            this.buttonRBackoff.Click += new System.EventHandler(this.buttonRBackoff_Click);
            // 
            // buttonRForward
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonRForward, 2);
            this.buttonRForward.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonRForward.Location = new System.Drawing.Point(2, 70);
            this.buttonRForward.Margin = new System.Windows.Forms.Padding(2);
            this.buttonRForward.Name = "buttonRForward";
            this.buttonRForward.Size = new System.Drawing.Size(144, 30);
            this.buttonRForward.TabIndex = 2;
            this.buttonRForward.Text = "R轴前进一行";
            this.buttonRForward.UseVisualStyleBackColor = true;
            this.buttonRForward.Click += new System.EventHandler(this.buttonRForward_Click);
            // 
            // btnAXsend
            // 
            this.btnAXsend.Location = new System.Drawing.Point(77, 105);
            this.btnAXsend.Name = "btnAXsend";
            this.btnAXsend.Size = new System.Drawing.Size(68, 23);
            this.btnAXsend.TabIndex = 4;
            this.btnAXsend.Text = "AX旋转发送";
            this.btnAXsend.UseVisualStyleBackColor = true;
            this.btnAXsend.Click += new System.EventHandler(this.btnAXsend_Click);
            // 
            // btnAXRecv
            // 
            this.btnAXRecv.Location = new System.Drawing.Point(3, 105);
            this.btnAXRecv.Name = "btnAXRecv";
            this.btnAXRecv.Size = new System.Drawing.Size(68, 23);
            this.btnAXRecv.TabIndex = 3;
            this.btnAXRecv.Text = "AX旋转接收";
            this.btnAXRecv.UseVisualStyleBackColor = true;
            this.btnAXRecv.Click += new System.EventHandler(this.btnAXRecv_Click);
            // 
            // btnYAXsend
            // 
            this.btnYAXsend.Location = new System.Drawing.Point(77, 37);
            this.btnYAXsend.Name = "btnYAXsend";
            this.btnYAXsend.Size = new System.Drawing.Size(68, 23);
            this.btnYAXsend.TabIndex = 4;
            this.btnYAXsend.Text = "YAX旋转发送";
            this.btnYAXsend.UseVisualStyleBackColor = true;
            this.btnYAXsend.Click += new System.EventHandler(this.btnYAXsend_Click);
            // 
            // btnYAXrecv
            // 
            this.btnYAXrecv.Location = new System.Drawing.Point(3, 37);
            this.btnYAXrecv.Name = "btnYAXrecv";
            this.btnYAXrecv.Size = new System.Drawing.Size(68, 23);
            this.btnYAXrecv.TabIndex = 3;
            this.btnYAXrecv.Text = "YAX旋转接收";
            this.btnYAXrecv.UseVisualStyleBackColor = true;
            this.btnYAXrecv.Click += new System.EventHandler(this.btnYAXrecv_Click);
            // 
            // buttonRHome
            // 
            this.tableLayoutPanel3.SetColumnSpan(this.buttonRHome, 2);
            this.buttonRHome.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonRHome.Location = new System.Drawing.Point(2, 2);
            this.buttonRHome.Margin = new System.Windows.Forms.Padding(2);
            this.buttonRHome.Name = "buttonRHome";
            this.buttonRHome.Size = new System.Drawing.Size(144, 30);
            this.buttonRHome.TabIndex = 1;
            this.buttonRHome.Text = "R轴回零";
            this.buttonRHome.UseVisualStyleBackColor = true;
            this.buttonRHome.Click += new System.EventHandler(this.buttonRHome_Click);
            // 
            // StopFord
            // 
            this.StopFord.Location = new System.Drawing.Point(151, 71);
            this.StopFord.Name = "StopFord";
            this.StopFord.Size = new System.Drawing.Size(70, 28);
            this.StopFord.TabIndex = 4;
            this.StopFord.Text = "停止前进";
            this.StopFord.UseVisualStyleBackColor = true;
            this.StopFord.Click += new System.EventHandler(this.StopFord_Click);
            // 
            // StopBack
            // 
            this.StopBack.Location = new System.Drawing.Point(151, 139);
            this.StopBack.Name = "StopBack";
            this.StopBack.Size = new System.Drawing.Size(70, 28);
            this.StopBack.TabIndex = 4;
            this.StopBack.Text = "停止后退";
            this.StopBack.UseVisualStyleBackColor = true;
            this.StopBack.Click += new System.EventHandler(this.StopBack_Click);
            // 
            // checkWritePlcLog
            // 
            this.checkWritePlcLog.AutoSize = true;
            this.checkWritePlcLog.Location = new System.Drawing.Point(151, 3);
            this.checkWritePlcLog.Name = "checkWritePlcLog";
            this.checkWritePlcLog.Size = new System.Drawing.Size(70, 16);
            this.checkWritePlcLog.TabIndex = 5;
            this.checkWritePlcLog.Text = "日志PLC读写";
            this.checkWritePlcLog.UseVisualStyleBackColor = true;
            this.checkWritePlcLog.CheckedChanged += new System.EventHandler(this.checkWritePlcLog_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel4);
            this.groupBox1.Location = new System.Drawing.Point(9, 10);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.groupBox1.Size = new System.Drawing.Size(228, 197);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "添加删除夹具";
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.ColumnCount = 3;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel4.Controls.Add(this.buttonPalletNG, 2, 3);
            this.tableLayoutPanel4.Controls.Add(this.buttonPalletFull, 2, 4);
            this.tableLayoutPanel4.Controls.Add(this.buttonPalletClear, 0, 3);
            this.tableLayoutPanel4.Controls.Add(this.buttonPalletAdd, 0, 4);
            this.tableLayoutPanel4.Controls.Add(this.label11, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.comboBoxPalletModule, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.label15, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.comboBoxPalletID, 1, 2);
            this.tableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel4.Location = new System.Drawing.Point(2, 22);
            this.tableLayoutPanel4.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 5;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(224, 173);
            this.tableLayoutPanel4.TabIndex = 7;
            // 
            // buttonPalletNG
            // 
            this.buttonPalletNG.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPalletNG.Location = new System.Drawing.Point(150, 104);
            this.buttonPalletNG.Margin = new System.Windows.Forms.Padding(2);
            this.buttonPalletNG.Name = "buttonPalletNG";
            this.buttonPalletNG.Size = new System.Drawing.Size(72, 30);
            this.buttonPalletNG.TabIndex = 15;
            this.buttonPalletNG.Text = "置NG转盘";
            this.buttonPalletNG.UseVisualStyleBackColor = true;
            this.buttonPalletNG.Click += new System.EventHandler(this.buttonPalletNG_Click);
            // 
            // buttonPalletFull
            // 
            this.buttonPalletFull.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPalletFull.Location = new System.Drawing.Point(150, 138);
            this.buttonPalletFull.Margin = new System.Windows.Forms.Padding(2);
            this.buttonPalletFull.Name = "buttonPalletFull";
            this.buttonPalletFull.Size = new System.Drawing.Size(72, 33);
            this.buttonPalletFull.TabIndex = 14;
            this.buttonPalletFull.Text = "置OK电池";
            this.buttonPalletFull.UseVisualStyleBackColor = true;
            this.buttonPalletFull.Click += new System.EventHandler(this.buttonPalletFull_Click);
            // 
            // buttonPalletClear
            // 
            this.buttonPalletClear.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPalletClear.Location = new System.Drawing.Point(2, 104);
            this.buttonPalletClear.Margin = new System.Windows.Forms.Padding(2);
            this.buttonPalletClear.Name = "buttonPalletClear";
            this.buttonPalletClear.Size = new System.Drawing.Size(70, 30);
            this.buttonPalletClear.TabIndex = 13;
            this.buttonPalletClear.Text = "删除";
            this.buttonPalletClear.UseVisualStyleBackColor = true;
            this.buttonPalletClear.Click += new System.EventHandler(this.buttonPalletClear_Click);
            // 
            // buttonPalletAdd
            // 
            this.buttonPalletAdd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPalletAdd.Location = new System.Drawing.Point(2, 138);
            this.buttonPalletAdd.Margin = new System.Windows.Forms.Padding(2);
            this.buttonPalletAdd.Name = "buttonPalletAdd";
            this.buttonPalletAdd.Size = new System.Drawing.Size(70, 33);
            this.buttonPalletAdd.TabIndex = 12;
            this.buttonPalletAdd.Text = "添加空夹具";
            this.buttonPalletAdd.UseVisualStyleBackColor = true;
            this.buttonPalletAdd.Click += new System.EventHandler(this.buttonPalletAdd_Click);
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("宋体", 11F);
            this.label11.Location = new System.Drawing.Point(2, 77);
            this.label11.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(70, 15);
            this.label11.TabIndex = 8;
            this.label11.Text = "夹具：";
            // 
            // comboBoxPalletModule
            // 
            this.comboBoxPalletModule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel4.SetColumnSpan(this.comboBoxPalletModule, 2);
            this.comboBoxPalletModule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPalletModule.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxPalletModule.FormattingEnabled = true;
            this.comboBoxPalletModule.Location = new System.Drawing.Point(76, 5);
            this.comboBoxPalletModule.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxPalletModule.Name = "comboBoxPalletModule";
            this.comboBoxPalletModule.Size = new System.Drawing.Size(146, 23);
            this.comboBoxPalletModule.TabIndex = 0;
            this.comboBoxPalletModule.SelectedIndexChanged += new System.EventHandler(this.comboBoxPalletModule_SelectedIndexChanged);
            // 
            // label15
            // 
            this.label15.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("宋体", 11F);
            this.label15.Location = new System.Drawing.Point(2, 9);
            this.label15.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(70, 15);
            this.label15.TabIndex = 7;
            this.label15.Text = "模组：";
            // 
            // comboBoxPalletID
            // 
            this.comboBoxPalletID.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel4.SetColumnSpan(this.comboBoxPalletID, 2);
            this.comboBoxPalletID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPalletID.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxPalletID.FormattingEnabled = true;
            this.comboBoxPalletID.Location = new System.Drawing.Point(76, 73);
            this.comboBoxPalletID.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxPalletID.Name = "comboBoxPalletID";
            this.comboBoxPalletID.Size = new System.Drawing.Size(146, 23);
            this.comboBoxPalletID.TabIndex = 10;
            // 
            // groupBoxServer
            // 
            this.groupBoxServer.Controls.Add(this.tableLayoutPanel2);
            this.groupBoxServer.Location = new System.Drawing.Point(256, 229);
            this.groupBoxServer.Margin = new System.Windows.Forms.Padding(2);
            this.groupBoxServer.Name = "groupBoxServer";
            this.groupBoxServer.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.groupBoxServer.Size = new System.Drawing.Size(228, 197);
            this.groupBoxServer.TabIndex = 8;
            this.groupBoxServer.TabStop = false;
            this.groupBoxServer.Text = "模组通讯服务";
            this.groupBoxServer.Visible = false;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 15F));
            this.tableLayoutPanel2.Controls.Add(this.buttonClientReconnect, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.buttonServerRestart, 0, 1);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(2, 22);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 5;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(224, 173);
            this.tableLayoutPanel2.TabIndex = 7;
            // 
            // buttonClientReconnect
            // 
            this.tableLayoutPanel2.SetColumnSpan(this.buttonClientReconnect, 2);
            this.buttonClientReconnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonClientReconnect.Location = new System.Drawing.Point(2, 70);
            this.buttonClientReconnect.Margin = new System.Windows.Forms.Padding(2);
            this.buttonClientReconnect.Name = "buttonClientReconnect";
            this.buttonClientReconnect.Size = new System.Drawing.Size(144, 30);
            this.buttonClientReconnect.TabIndex = 2;
            this.buttonClientReconnect.Text = "重连模组服务";
            this.buttonClientReconnect.UseVisualStyleBackColor = true;
            this.buttonClientReconnect.Click += new System.EventHandler(this.buttonClientReconnect_Click);
            // 
            // buttonServerRestart
            // 
            this.tableLayoutPanel2.SetColumnSpan(this.buttonServerRestart, 2);
            this.buttonServerRestart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonServerRestart.Location = new System.Drawing.Point(2, 36);
            this.buttonServerRestart.Margin = new System.Windows.Forms.Padding(2);
            this.buttonServerRestart.Name = "buttonServerRestart";
            this.buttonServerRestart.Size = new System.Drawing.Size(144, 30);
            this.buttonServerRestart.TabIndex = 1;
            this.buttonServerRestart.UseVisualStyleBackColor = true;
            this.buttonServerRestart.Visible = false;
            this.buttonServerRestart.Click += new System.EventHandler(this.buttonServerRestart_Click);
            // 
            // OtherDebugPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(750, 443);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "OtherDebugPage";
            this.Text = "OtherDebugPage";
            this.Load += new System.EventHandler(this.OtherDebugPage_Load);
            this.panel1.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel6.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.groupBoxServer.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox groupBoxServer;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button buttonServerRestart;
        private System.Windows.Forms.Button buttonClientReconnect;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox comboBoxPalletModule;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ComboBox comboBoxPalletID;
        private System.Windows.Forms.Button buttonPalletClear;
        private System.Windows.Forms.Button buttonPalletAdd;
        private System.Windows.Forms.Button buttonPalletFull;
        private System.Windows.Forms.Button buttonPalletNG;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Button buttonRForward;
        private System.Windows.Forms.Button buttonRHome;
        private System.Windows.Forms.Button buttonRBackoff;
        private System.Windows.Forms.Button StopFord;
        private System.Windows.Forms.Button StopBack;
        private System.Windows.Forms.Button btnYAXrecv;
        private System.Windows.Forms.Button btnYAXsend;
        private System.Windows.Forms.Button btnAXRecv;
        private System.Windows.Forms.Button btnAXsend;
        private System.Windows.Forms.CheckBox checkWritePlcLog;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Button btnAddPalletBat;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbPalletId;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox checkPalletFake;
        private System.Windows.Forms.ComboBox cmbRow;
        private System.Windows.Forms.ComboBox cmbCol;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.Label labelFinger1;
        private System.Windows.Forms.Label labelFinger3;
        private System.Windows.Forms.Label labelFinger4;
        private System.Windows.Forms.Label labelFinger2;
        private System.Windows.Forms.Button btnFingerTemper;
        private System.Windows.Forms.Button btnOffloadOutFinish;
    }
}