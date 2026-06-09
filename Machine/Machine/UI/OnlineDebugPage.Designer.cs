namespace Machine
{
    partial class OnlineDebugPage
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
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.btnEventSet = new System.Windows.Forms.Button();
            this.cmbEventPalletModule = new System.Windows.Forms.ComboBox();
            this.label18 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.cmbEventPalletID = new System.Windows.Forms.ComboBox();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.cmbNewEvent = new System.Windows.Forms.ComboBox();
            this.cmbCurEvent = new System.Windows.Forms.ComboBox();
            this.cmbEventID = new System.Windows.Forms.ComboBox();
            this.btnOnloadInit = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.btnBatShow = new System.Windows.Forms.Button();
            this.comboBoxBatModule = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBoxRowId = new System.Windows.Forms.ComboBox();
            this.checkBoxPallet = new System.Windows.Forms.CheckBox();
            this.comboBoxPalletID2 = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.btnBatDel = new System.Windows.Forms.Button();
            this.btnBatAdd = new System.Windows.Forms.Button();
            this.txtBatteryCodeOld = new System.Windows.Forms.TextBox();
            this.txtBatteryCode = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.comboBoxColId = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonPalletTestBat = new System.Windows.Forms.Button();
            this.buttonPalletNG = new System.Windows.Forms.Button();
            this.buttonPalletFull = new System.Windows.Forms.Button();
            this.buttonPalletClear = new System.Windows.Forms.Button();
            this.buttonPalletAdd = new System.Windows.Forms.Button();
            this.label11 = new System.Windows.Forms.Label();
            this.comboBoxPalletModule = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.comboBoxPalletID = new System.Windows.Forms.ComboBox();
            this.checkBoxFake = new System.Windows.Forms.CheckBox();
            this.btnScandCode = new System.Windows.Forms.Button();
            this.checkBoxFinger = new System.Windows.Forms.CheckBox();
            this.groupBoxServer = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonClientReconnect = new System.Windows.Forms.Button();
            this.buttonServerRestart = new System.Windows.Forms.Button();
            this.groupBoxScanner = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBoxScanChose = new System.Windows.Forms.ComboBox();
            this.buttonScanCode = new System.Windows.Forms.Button();
            this.textBoxCodeData = new System.Windows.Forms.TextBox();
            this.buttonScanDisconnect = new System.Windows.Forms.Button();
            this.labelScanConState = new System.Windows.Forms.Label();
            this.labelScanAdder = new System.Windows.Forms.Label();
            this.buttonScanConnect = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.groupBoxServer.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.groupBoxScanner.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox6);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.groupBoxServer);
            this.panel1.Controls.Add(this.groupBoxScanner);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(919, 443);
            this.panel1.TabIndex = 0;
            this.panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.tableLayoutPanel7);
            this.groupBox6.Location = new System.Drawing.Point(11, 227);
            this.groupBox6.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox6.Size = new System.Drawing.Size(226, 216);
            this.groupBox6.TabIndex = 15;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "夹具事件";
            // 
            // tableLayoutPanel7
            // 
            this.tableLayoutPanel7.ColumnCount = 3;
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel7.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel7.Controls.Add(this.btnEventSet, 3, 4);
            this.tableLayoutPanel7.Controls.Add(this.cmbEventPalletModule, 1, 0);
            this.tableLayoutPanel7.Controls.Add(this.label18, 0, 0);
            this.tableLayoutPanel7.Controls.Add(this.label17, 0, 1);
            this.tableLayoutPanel7.Controls.Add(this.cmbEventPalletID, 1, 1);
            this.tableLayoutPanel7.Controls.Add(this.label19, 0, 2);
            this.tableLayoutPanel7.Controls.Add(this.label20, 0, 3);
            this.tableLayoutPanel7.Controls.Add(this.cmbNewEvent, 2, 3);
            this.tableLayoutPanel7.Controls.Add(this.cmbCurEvent, 1, 3);
            this.tableLayoutPanel7.Controls.Add(this.cmbEventID, 1, 2);
            this.tableLayoutPanel7.Controls.Add(this.btnOnloadInit, 0, 4);
            this.tableLayoutPanel7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel7.Location = new System.Drawing.Point(2, 16);
            this.tableLayoutPanel7.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            this.tableLayoutPanel7.RowCount = 5;
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel7.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.tableLayoutPanel7.Size = new System.Drawing.Size(222, 198);
            this.tableLayoutPanel7.TabIndex = 8;
            // 
            // btnEventSet
            // 
            this.btnEventSet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnEventSet.Location = new System.Drawing.Point(150, 158);
            this.btnEventSet.Margin = new System.Windows.Forms.Padding(2);
            this.btnEventSet.Name = "btnEventSet";
            this.btnEventSet.Size = new System.Drawing.Size(70, 38);
            this.btnEventSet.TabIndex = 13;
            this.btnEventSet.Text = "设置";
            this.btnEventSet.UseVisualStyleBackColor = true;
            this.btnEventSet.Click += new System.EventHandler(this.btnEventSet_Click);
            // 
            // cmbEventPalletModule
            // 
            this.cmbEventPalletModule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel7.SetColumnSpan(this.cmbEventPalletModule, 2);
            this.cmbEventPalletModule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEventPalletModule.Font = new System.Drawing.Font("宋体", 11F);
            this.cmbEventPalletModule.FormattingEnabled = true;
            this.cmbEventPalletModule.Location = new System.Drawing.Point(76, 8);
            this.cmbEventPalletModule.Margin = new System.Windows.Forms.Padding(2);
            this.cmbEventPalletModule.Name = "cmbEventPalletModule";
            this.cmbEventPalletModule.Size = new System.Drawing.Size(144, 23);
            this.cmbEventPalletModule.TabIndex = 0;
            this.cmbEventPalletModule.SelectedIndexChanged += new System.EventHandler(this.cmbEventPalletModule_SelectedIndexChanged);
            // 
            // label18
            // 
            this.label18.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("宋体", 11F);
            this.label18.Location = new System.Drawing.Point(2, 12);
            this.label18.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(70, 15);
            this.label18.TabIndex = 7;
            this.label18.Text = "模组：";
            // 
            // label17
            // 
            this.label17.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label17.AutoSize = true;
            this.label17.Font = new System.Drawing.Font("宋体", 11F);
            this.label17.Location = new System.Drawing.Point(2, 51);
            this.label17.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(70, 15);
            this.label17.TabIndex = 8;
            this.label17.Text = "夹具：";
            // 
            // cmbEventPalletID
            // 
            this.cmbEventPalletID.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel7.SetColumnSpan(this.cmbEventPalletID, 2);
            this.cmbEventPalletID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEventPalletID.Font = new System.Drawing.Font("宋体", 11F);
            this.cmbEventPalletID.FormattingEnabled = true;
            this.cmbEventPalletID.Location = new System.Drawing.Point(76, 47);
            this.cmbEventPalletID.Margin = new System.Windows.Forms.Padding(2);
            this.cmbEventPalletID.Name = "cmbEventPalletID";
            this.cmbEventPalletID.Size = new System.Drawing.Size(144, 23);
            this.cmbEventPalletID.TabIndex = 10;
            this.cmbEventPalletID.SelectedIndexChanged += new System.EventHandler(this.cmbEventPalletID_SelectedIndexChanged);
            // 
            // label19
            // 
            this.label19.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label19.AutoSize = true;
            this.label19.Font = new System.Drawing.Font("宋体", 11F);
            this.label19.Location = new System.Drawing.Point(2, 90);
            this.label19.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(70, 15);
            this.label19.TabIndex = 8;
            this.label19.Text = "事件：";
            // 
            // label20
            // 
            this.label20.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label20.AutoSize = true;
            this.label20.Font = new System.Drawing.Font("宋体", 11F);
            this.label20.Location = new System.Drawing.Point(2, 121);
            this.label20.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(70, 30);
            this.label20.TabIndex = 8;
            this.label20.Text = "设置状态：";
            // 
            // cmbNewEvent
            // 
            this.cmbNewEvent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbNewEvent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbNewEvent.Font = new System.Drawing.Font("宋体", 11F);
            this.cmbNewEvent.FormattingEnabled = true;
            this.cmbNewEvent.Location = new System.Drawing.Point(150, 125);
            this.cmbNewEvent.Margin = new System.Windows.Forms.Padding(2);
            this.cmbNewEvent.Name = "cmbNewEvent";
            this.cmbNewEvent.Size = new System.Drawing.Size(70, 23);
            this.cmbNewEvent.TabIndex = 10;
            // 
            // cmbCurEvent
            // 
            this.cmbCurEvent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbCurEvent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbCurEvent.Font = new System.Drawing.Font("宋体", 11F);
            this.cmbCurEvent.FormattingEnabled = true;
            this.cmbCurEvent.Location = new System.Drawing.Point(76, 125);
            this.cmbCurEvent.Margin = new System.Windows.Forms.Padding(2);
            this.cmbCurEvent.Name = "cmbCurEvent";
            this.cmbCurEvent.Size = new System.Drawing.Size(70, 23);
            this.cmbCurEvent.TabIndex = 10;
            // 
            // cmbEventID
            // 
            this.cmbEventID.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel7.SetColumnSpan(this.cmbEventID, 2);
            this.cmbEventID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEventID.Font = new System.Drawing.Font("宋体", 11F);
            this.cmbEventID.FormattingEnabled = true;
            this.cmbEventID.Location = new System.Drawing.Point(76, 86);
            this.cmbEventID.Margin = new System.Windows.Forms.Padding(2);
            this.cmbEventID.Name = "cmbEventID";
            this.cmbEventID.Size = new System.Drawing.Size(144, 23);
            this.cmbEventID.TabIndex = 10;
            this.cmbEventID.SelectedIndexChanged += new System.EventHandler(this.cmbEventID_SelectedIndexChanged);
            // 
            // btnOnloadInit
            // 
            this.btnOnloadInit.Location = new System.Drawing.Point(3, 159);
            this.btnOnloadInit.Name = "btnOnloadInit";
            this.btnOnloadInit.Size = new System.Drawing.Size(68, 35);
            this.btnOnloadInit.TabIndex = 15;
            this.btnOnloadInit.Text = "来料初始化";
            this.btnOnloadInit.UseVisualStyleBackColor = true;
            this.btnOnloadInit.Click += new System.EventHandler(this.btnOnloadInit_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tableLayoutPanel3);
            this.groupBox2.Location = new System.Drawing.Point(501, 10);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.groupBox2.Size = new System.Drawing.Size(228, 197);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "模组记忆清除-夹爪顺序编：4-3-2-1";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.Controls.Add(this.btnBatShow, 0, 6);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxBatModule, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label6, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.label1, 0, 3);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxRowId, 1, 3);
            this.tableLayoutPanel3.Controls.Add(this.checkBoxPallet, 2, 1);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxPalletID2, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.label7, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.label8, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.btnBatDel, 1, 6);
            this.tableLayoutPanel3.Controls.Add(this.btnBatAdd, 2, 6);
            this.tableLayoutPanel3.Controls.Add(this.txtBatteryCodeOld, 1, 4);
            this.tableLayoutPanel3.Controls.Add(this.txtBatteryCode, 1, 5);
            this.tableLayoutPanel3.Controls.Add(this.label9, 0, 4);
            this.tableLayoutPanel3.Controls.Add(this.label10, 0, 5);
            this.tableLayoutPanel3.Controls.Add(this.comboBoxColId, 1, 2);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(2, 22);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 7;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 14.28571F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(224, 173);
            this.tableLayoutPanel3.TabIndex = 7;
            // 
            // btnBatShow
            // 
            this.btnBatShow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBatShow.Location = new System.Drawing.Point(3, 147);
            this.btnBatShow.Name = "btnBatShow";
            this.btnBatShow.Size = new System.Drawing.Size(68, 23);
            this.btnBatShow.TabIndex = 18;
            this.btnBatShow.Text = "查询电池";
            this.btnBatShow.UseVisualStyleBackColor = true;
            this.btnBatShow.Click += new System.EventHandler(this.btnBatShow_Click);
            // 
            // comboBoxBatModule
            // 
            this.comboBoxBatModule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.SetColumnSpan(this.comboBoxBatModule, 2);
            this.comboBoxBatModule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBatModule.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxBatModule.FormattingEnabled = true;
            this.comboBoxBatModule.Location = new System.Drawing.Point(76, 2);
            this.comboBoxBatModule.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxBatModule.Name = "comboBoxBatModule";
            this.comboBoxBatModule.Size = new System.Drawing.Size(146, 23);
            this.comboBoxBatModule.TabIndex = 0;
            this.comboBoxBatModule.SelectedIndexChanged += new System.EventHandler(this.comboBoxBatModule_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("宋体", 11F);
            this.label6.Location = new System.Drawing.Point(2, 4);
            this.label6.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(70, 15);
            this.label6.TabIndex = 7;
            this.label6.Text = "模组：";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 11F);
            this.label1.Location = new System.Drawing.Point(2, 72);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 24);
            this.label1.TabIndex = 8;
            this.label1.Text = "第N个电池：";
            // 
            // comboBoxRowId
            // 
            this.comboBoxRowId.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.SetColumnSpan(this.comboBoxRowId, 2);
            this.comboBoxRowId.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxRowId.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxRowId.FormattingEnabled = true;
            this.comboBoxRowId.Location = new System.Drawing.Point(76, 74);
            this.comboBoxRowId.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxRowId.Name = "comboBoxRowId";
            this.comboBoxRowId.Size = new System.Drawing.Size(146, 23);
            this.comboBoxRowId.TabIndex = 10;
            // 
            // checkBoxPallet
            // 
            this.checkBoxPallet.AutoSize = true;
            this.checkBoxPallet.Location = new System.Drawing.Point(151, 27);
            this.checkBoxPallet.Name = "checkBoxPallet";
            this.checkBoxPallet.Size = new System.Drawing.Size(48, 16);
            this.checkBoxPallet.TabIndex = 17;
            this.checkBoxPallet.Text = "夹具";
            this.checkBoxPallet.UseVisualStyleBackColor = true;
            this.checkBoxPallet.CheckedChanged += new System.EventHandler(this.checkBoxPallet_CheckedChanged);
            // 
            // comboBoxPalletID2
            // 
            this.comboBoxPalletID2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxPalletID2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPalletID2.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxPalletID2.FormattingEnabled = true;
            this.comboBoxPalletID2.Location = new System.Drawing.Point(76, 26);
            this.comboBoxPalletID2.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxPalletID2.Name = "comboBoxPalletID2";
            this.comboBoxPalletID2.Size = new System.Drawing.Size(70, 23);
            this.comboBoxPalletID2.TabIndex = 10;
            this.comboBoxPalletID2.SelectedIndexChanged += new System.EventHandler(this.comboBoxPalletID2_SelectedIndexChanged);
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("宋体", 11F);
            this.label7.Location = new System.Drawing.Point(2, 48);
            this.label7.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(70, 24);
            this.label7.TabIndex = 8;
            this.label7.Text = "第N排电池：";
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("宋体", 11F);
            this.label8.Location = new System.Drawing.Point(2, 28);
            this.label8.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(70, 15);
            this.label8.TabIndex = 8;
            this.label8.Text = "夹具位：";
            // 
            // btnBatDel
            // 
            this.btnBatDel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBatDel.Location = new System.Drawing.Point(76, 146);
            this.btnBatDel.Margin = new System.Windows.Forms.Padding(2);
            this.btnBatDel.Name = "btnBatDel";
            this.btnBatDel.Size = new System.Drawing.Size(70, 25);
            this.btnBatDel.TabIndex = 13;
            this.btnBatDel.Text = "删除电池";
            this.btnBatDel.UseVisualStyleBackColor = true;
            this.btnBatDel.Visible = false;
            this.btnBatDel.Click += new System.EventHandler(this.btnBatDel_Click);
            // 
            // btnBatAdd
            // 
            this.btnBatAdd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnBatAdd.Location = new System.Drawing.Point(150, 146);
            this.btnBatAdd.Margin = new System.Windows.Forms.Padding(2);
            this.btnBatAdd.Name = "btnBatAdd";
            this.btnBatAdd.Size = new System.Drawing.Size(72, 25);
            this.btnBatAdd.TabIndex = 13;
            this.btnBatAdd.Text = "修改电池";
            this.btnBatAdd.UseVisualStyleBackColor = true;
            this.btnBatAdd.Click += new System.EventHandler(this.btnBatAdd_Click);
            // 
            // txtBatteryCodeOld
            // 
            this.txtBatteryCodeOld.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.SetColumnSpan(this.txtBatteryCodeOld, 2);
            this.txtBatteryCodeOld.Font = new System.Drawing.Font("宋体", 11F);
            this.txtBatteryCodeOld.Location = new System.Drawing.Point(76, 98);
            this.txtBatteryCodeOld.Margin = new System.Windows.Forms.Padding(2);
            this.txtBatteryCodeOld.Name = "txtBatteryCodeOld";
            this.txtBatteryCodeOld.ReadOnly = true;
            this.txtBatteryCodeOld.Size = new System.Drawing.Size(146, 24);
            this.txtBatteryCodeOld.TabIndex = 14;
            // 
            // txtBatteryCode
            // 
            this.txtBatteryCode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.SetColumnSpan(this.txtBatteryCode, 2);
            this.txtBatteryCode.Font = new System.Drawing.Font("宋体", 11F);
            this.txtBatteryCode.Location = new System.Drawing.Point(76, 122);
            this.txtBatteryCode.Margin = new System.Windows.Forms.Padding(2);
            this.txtBatteryCode.Name = "txtBatteryCode";
            this.txtBatteryCode.Size = new System.Drawing.Size(146, 24);
            this.txtBatteryCode.TabIndex = 14;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("宋体", 11F);
            this.label9.Location = new System.Drawing.Point(2, 96);
            this.label9.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(70, 24);
            this.label9.TabIndex = 8;
            this.label9.Text = "工位电池：";
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("宋体", 11F);
            this.label10.Location = new System.Drawing.Point(2, 120);
            this.label10.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(70, 24);
            this.label10.TabIndex = 8;
            this.label10.Text = "新增电池：";
            // 
            // comboBoxColId
            // 
            this.comboBoxColId.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.SetColumnSpan(this.comboBoxColId, 2);
            this.comboBoxColId.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxColId.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxColId.FormattingEnabled = true;
            this.comboBoxColId.Location = new System.Drawing.Point(76, 50);
            this.comboBoxColId.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxColId.Name = "comboBoxColId";
            this.comboBoxColId.Size = new System.Drawing.Size(146, 23);
            this.comboBoxColId.TabIndex = 10;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tableLayoutPanel4);
            this.groupBox1.Location = new System.Drawing.Point(255, 10);
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
            this.tableLayoutPanel4.Controls.Add(this.buttonPalletTestBat, 1, 4);
            this.tableLayoutPanel4.Controls.Add(this.buttonPalletNG, 2, 3);
            this.tableLayoutPanel4.Controls.Add(this.buttonPalletFull, 2, 4);
            this.tableLayoutPanel4.Controls.Add(this.buttonPalletClear, 0, 3);
            this.tableLayoutPanel4.Controls.Add(this.buttonPalletAdd, 0, 4);
            this.tableLayoutPanel4.Controls.Add(this.label11, 0, 2);
            this.tableLayoutPanel4.Controls.Add(this.comboBoxPalletModule, 1, 0);
            this.tableLayoutPanel4.Controls.Add(this.label15, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.comboBoxPalletID, 1, 2);
            this.tableLayoutPanel4.Controls.Add(this.checkBoxFake, 2, 2);
            this.tableLayoutPanel4.Controls.Add(this.btnScandCode, 1, 3);
            this.tableLayoutPanel4.Controls.Add(this.checkBoxFinger, 1, 1);
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
            // buttonPalletTestBat
            // 
            this.buttonPalletTestBat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPalletTestBat.Location = new System.Drawing.Point(76, 138);
            this.buttonPalletTestBat.Margin = new System.Windows.Forms.Padding(2);
            this.buttonPalletTestBat.Name = "buttonPalletTestBat";
            this.buttonPalletTestBat.Size = new System.Drawing.Size(70, 33);
            this.buttonPalletTestBat.TabIndex = 16;
            this.buttonPalletTestBat.Text = "模拟上料";
            this.buttonPalletTestBat.UseVisualStyleBackColor = true;
            this.buttonPalletTestBat.Click += new System.EventHandler(this.buttonPalletTestBat_Click);
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
            this.comboBoxPalletID.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPalletID.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxPalletID.FormattingEnabled = true;
            this.comboBoxPalletID.Location = new System.Drawing.Point(76, 73);
            this.comboBoxPalletID.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxPalletID.Name = "comboBoxPalletID";
            this.comboBoxPalletID.Size = new System.Drawing.Size(70, 23);
            this.comboBoxPalletID.TabIndex = 10;
            // 
            // checkBoxFake
            // 
            this.checkBoxFake.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxFake.AutoSize = true;
            this.checkBoxFake.Location = new System.Drawing.Point(151, 77);
            this.checkBoxFake.Name = "checkBoxFake";
            this.checkBoxFake.Size = new System.Drawing.Size(70, 16);
            this.checkBoxFake.TabIndex = 17;
            this.checkBoxFake.Text = "假电芯";
            this.checkBoxFake.UseVisualStyleBackColor = true;
            // 
            // btnScandCode
            // 
            this.btnScandCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnScandCode.Enabled = false;
            this.btnScandCode.Location = new System.Drawing.Point(76, 104);
            this.btnScandCode.Margin = new System.Windows.Forms.Padding(2);
            this.btnScandCode.Name = "btnScandCode";
            this.btnScandCode.Size = new System.Drawing.Size(70, 30);
            this.btnScandCode.TabIndex = 18;
            this.btnScandCode.Text = "手动扫码";
            this.btnScandCode.UseVisualStyleBackColor = true;
            this.btnScandCode.Click += new System.EventHandler(this.btnScandCode_Click);
            // 
            // checkBoxFinger
            // 
            this.checkBoxFinger.AutoSize = true;
            this.tableLayoutPanel4.SetColumnSpan(this.checkBoxFinger, 2);
            this.checkBoxFinger.Location = new System.Drawing.Point(77, 37);
            this.checkBoxFinger.Name = "checkBoxFinger";
            this.checkBoxFinger.Size = new System.Drawing.Size(96, 16);
            this.checkBoxFinger.TabIndex = 17;
            this.checkBoxFinger.Text = "夹爪高位防呆";
            this.checkBoxFinger.UseVisualStyleBackColor = true;
            this.checkBoxFinger.CheckedChanged += new System.EventHandler(this.checkBoxFinger_CheckedChanged);
            // 
            // groupBoxServer
            // 
            this.groupBoxServer.Controls.Add(this.tableLayoutPanel2);
            this.groupBoxServer.Location = new System.Drawing.Point(558, 225);
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
            // groupBoxScanner
            // 
            this.groupBoxScanner.Controls.Add(this.tableLayoutPanel1);
            this.groupBoxScanner.Location = new System.Drawing.Point(9, 10);
            this.groupBoxScanner.Margin = new System.Windows.Forms.Padding(2);
            this.groupBoxScanner.Name = "groupBoxScanner";
            this.groupBoxScanner.Padding = new System.Windows.Forms.Padding(2, 8, 2, 2);
            this.groupBoxScanner.Size = new System.Drawing.Size(228, 197);
            this.groupBoxScanner.TabIndex = 0;
            this.groupBoxScanner.TabStop = false;
            this.groupBoxScanner.Text = "扫码器调试";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxScanChose, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonScanCode, 2, 4);
            this.tableLayoutPanel1.Controls.Add(this.textBoxCodeData, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.buttonScanDisconnect, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.labelScanConState, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelScanAdder, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonScanConnect, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(2, 22);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(2);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(224, 173);
            this.tableLayoutPanel1.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("宋体", 11F);
            this.label5.Location = new System.Drawing.Point(2, 111);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(70, 15);
            this.label5.TabIndex = 10;
            this.label5.Text = "条码：";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("宋体", 11F);
            this.label4.Location = new System.Drawing.Point(2, 77);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(70, 15);
            this.label4.TabIndex = 9;
            this.label4.Text = "状态：";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 11F);
            this.label3.Location = new System.Drawing.Point(2, 43);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 15);
            this.label3.TabIndex = 8;
            this.label3.Text = "地址：";
            // 
            // comboBoxScanChose
            // 
            this.comboBoxScanChose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxScanChose.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxScanChose.Font = new System.Drawing.Font("宋体", 11F);
            this.comboBoxScanChose.FormattingEnabled = true;
            this.comboBoxScanChose.Location = new System.Drawing.Point(76, 5);
            this.comboBoxScanChose.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxScanChose.Name = "comboBoxScanChose";
            this.comboBoxScanChose.Size = new System.Drawing.Size(70, 23);
            this.comboBoxScanChose.TabIndex = 0;
            this.comboBoxScanChose.SelectedIndexChanged += new System.EventHandler(this.comboBoxScanChose_SelectedIndexChanged);
            // 
            // buttonScanCode
            // 
            this.buttonScanCode.AutoSize = true;
            this.buttonScanCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonScanCode.Location = new System.Drawing.Point(150, 138);
            this.buttonScanCode.Margin = new System.Windows.Forms.Padding(2);
            this.buttonScanCode.Name = "buttonScanCode";
            this.buttonScanCode.Size = new System.Drawing.Size(72, 33);
            this.buttonScanCode.TabIndex = 3;
            this.buttonScanCode.Text = "扫码";
            this.buttonScanCode.UseVisualStyleBackColor = true;
            this.buttonScanCode.Click += new System.EventHandler(this.buttonScanCode_Click);
            // 
            // textBoxCodeData
            // 
            this.textBoxCodeData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.textBoxCodeData, 2);
            this.textBoxCodeData.Font = new System.Drawing.Font("宋体", 11F);
            this.textBoxCodeData.Location = new System.Drawing.Point(76, 107);
            this.textBoxCodeData.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxCodeData.Name = "textBoxCodeData";
            this.textBoxCodeData.ReadOnly = true;
            this.textBoxCodeData.Size = new System.Drawing.Size(146, 24);
            this.textBoxCodeData.TabIndex = 4;
            // 
            // buttonScanDisconnect
            // 
            this.buttonScanDisconnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonScanDisconnect.Location = new System.Drawing.Point(76, 138);
            this.buttonScanDisconnect.Margin = new System.Windows.Forms.Padding(2);
            this.buttonScanDisconnect.Name = "buttonScanDisconnect";
            this.buttonScanDisconnect.Size = new System.Drawing.Size(70, 33);
            this.buttonScanDisconnect.TabIndex = 2;
            this.buttonScanDisconnect.Text = "断开";
            this.buttonScanDisconnect.UseVisualStyleBackColor = true;
            this.buttonScanDisconnect.Click += new System.EventHandler(this.buttonScanDisconnect_Click);
            // 
            // labelScanConState
            // 
            this.labelScanConState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.labelScanConState, 2);
            this.labelScanConState.Location = new System.Drawing.Point(76, 74);
            this.labelScanConState.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelScanConState.Name = "labelScanConState";
            this.labelScanConState.Size = new System.Drawing.Size(146, 22);
            this.labelScanConState.TabIndex = 5;
            // 
            // labelScanAdder
            // 
            this.labelScanAdder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.labelScanAdder, 2);
            this.labelScanAdder.Location = new System.Drawing.Point(76, 40);
            this.labelScanAdder.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelScanAdder.Name = "labelScanAdder";
            this.labelScanAdder.Size = new System.Drawing.Size(146, 22);
            this.labelScanAdder.TabIndex = 6;
            // 
            // buttonScanConnect
            // 
            this.buttonScanConnect.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonScanConnect.Location = new System.Drawing.Point(2, 138);
            this.buttonScanConnect.Margin = new System.Windows.Forms.Padding(2);
            this.buttonScanConnect.Name = "buttonScanConnect";
            this.buttonScanConnect.Size = new System.Drawing.Size(70, 33);
            this.buttonScanConnect.TabIndex = 1;
            this.buttonScanConnect.Text = "连接";
            this.buttonScanConnect.UseVisualStyleBackColor = true;
            this.buttonScanConnect.Click += new System.EventHandler(this.buttonScanConnect_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 11F);
            this.label2.Location = new System.Drawing.Point(2, 9);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 15);
            this.label2.TabIndex = 7;
            this.label2.Text = "扫码器：";
            // 
            // OnlineDebugPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(919, 443);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "OnlineDebugPage";
            this.Text = "OtherDebugPage";
            this.Load += new System.EventHandler(this.OnlineDebugPage_Load);
            this.panel1.ResumeLayout(false);
            this.groupBox6.ResumeLayout(false);
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.groupBoxServer.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.groupBoxScanner.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox groupBoxScanner;
        private System.Windows.Forms.TextBox textBoxCodeData;
        private System.Windows.Forms.Button buttonScanCode;
        private System.Windows.Forms.Button buttonScanDisconnect;
        private System.Windows.Forms.Button buttonScanConnect;
        private System.Windows.Forms.ComboBox comboBoxScanChose;
        private System.Windows.Forms.Label labelScanConState;
        private System.Windows.Forms.Label labelScanAdder;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
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
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBoxBatModule;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnBatDel;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox comboBoxRowId;
        private System.Windows.Forms.ComboBox comboBoxColId;
        private System.Windows.Forms.TextBox txtBatteryCode;
        private System.Windows.Forms.Button btnBatAdd;
        private System.Windows.Forms.Button buttonPalletTestBat;
        private System.Windows.Forms.CheckBox checkBoxFake;
        private System.Windows.Forms.CheckBox checkBoxFinger;
        private System.Windows.Forms.TextBox txtBatteryCodeOld;
        private System.Windows.Forms.ComboBox comboBoxPalletID2;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btnBatShow;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckBox checkBoxPallet;
        private System.Windows.Forms.Button btnScandCode;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.Button btnEventSet;
        private System.Windows.Forms.ComboBox cmbEventPalletModule;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.ComboBox cmbEventPalletID;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.ComboBox cmbNewEvent;
        private System.Windows.Forms.ComboBox cmbCurEvent;
        private System.Windows.Forms.ComboBox cmbEventID;
        private System.Windows.Forms.Button btnOnloadInit;
    }
}