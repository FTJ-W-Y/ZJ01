
namespace Machine
{
    partial class ManualScanCode
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
            if (disposing && (components != null))
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
            this.labCode = new System.Windows.Forms.Label();
            this.txtCode = new System.Windows.Forms.TextBox();
            this.btnCode = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.txtFaceCodeCnt = new System.Windows.Forms.TextBox();
            this.txtCodeCnt = new System.Windows.Forms.TextBox();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.labPalletCodeCnt = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.labFace = new System.Windows.Forms.Label();
            this.btnFaceDown = new System.Windows.Forms.Button();
            this.checkInPallet = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // labCode
            // 
            this.labCode.AutoSize = true;
            this.labCode.Font = new System.Drawing.Font("宋体", 12F);
            this.labCode.Location = new System.Drawing.Point(29, 33);
            this.labCode.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labCode.Name = "labCode";
            this.labCode.Size = new System.Drawing.Size(88, 16);
            this.labCode.TabIndex = 0;
            this.labCode.Text = "电芯条码：";
            // 
            // txtCode
            // 
            this.txtCode.Font = new System.Drawing.Font("宋体", 12F);
            this.txtCode.Location = new System.Drawing.Point(127, 28);
            this.txtCode.Margin = new System.Windows.Forms.Padding(2);
            this.txtCode.Name = "txtCode";
            this.txtCode.Size = new System.Drawing.Size(247, 26);
            this.txtCode.TabIndex = 0;
            this.txtCode.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtCode_KeyDown);
            this.txtCode.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCode_KeyPress);
            // 
            // btnCode
            // 
            this.btnCode.Location = new System.Drawing.Point(389, 25);
            this.btnCode.Margin = new System.Windows.Forms.Padding(2);
            this.btnCode.Name = "btnCode";
            this.btnCode.Size = new System.Drawing.Size(132, 33);
            this.btnCode.TabIndex = 1;
            this.btnCode.Text = "电芯扫码";
            this.btnCode.UseVisualStyleBackColor = true;
            this.btnCode.Click += new System.EventHandler(this.btnCode_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 12F);
            this.label1.Location = new System.Drawing.Point(29, 75);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "假电池数：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 12F);
            this.label2.Location = new System.Drawing.Point(13, 117);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 16);
            this.label2.TabIndex = 0;
            this.label2.Text = "托盘电池数：";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("宋体", 12F);
            this.label5.Location = new System.Drawing.Point(29, 159);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(88, 16);
            this.label5.TabIndex = 0;
            this.label5.Text = "电芯校验：";
            // 
            // txtFaceCodeCnt
            // 
            this.txtFaceCodeCnt.Font = new System.Drawing.Font("宋体", 12F);
            this.txtFaceCodeCnt.Location = new System.Drawing.Point(127, 70);
            this.txtFaceCodeCnt.Margin = new System.Windows.Forms.Padding(2);
            this.txtFaceCodeCnt.Name = "txtFaceCodeCnt";
            this.txtFaceCodeCnt.ReadOnly = true;
            this.txtFaceCodeCnt.Size = new System.Drawing.Size(92, 26);
            this.txtFaceCodeCnt.TabIndex = 1;
            this.txtFaceCodeCnt.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtCode_KeyDown);
            this.txtFaceCodeCnt.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCode_KeyPress);
            // 
            // txtCodeCnt
            // 
            this.txtCodeCnt.Font = new System.Drawing.Font("宋体", 12F);
            this.txtCodeCnt.Location = new System.Drawing.Point(127, 112);
            this.txtCodeCnt.Margin = new System.Windows.Forms.Padding(2);
            this.txtCodeCnt.Name = "txtCodeCnt";
            this.txtCodeCnt.ReadOnly = true;
            this.txtCodeCnt.Size = new System.Drawing.Size(92, 26);
            this.txtCodeCnt.TabIndex = 1;
            this.txtCodeCnt.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtCode_KeyDown);
            this.txtCodeCnt.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCode_KeyPress);
            // 
            // txtResult
            // 
            this.txtResult.Font = new System.Drawing.Font("宋体", 12F);
            this.txtResult.Location = new System.Drawing.Point(127, 154);
            this.txtResult.Margin = new System.Windows.Forms.Padding(2);
            this.txtResult.Multiline = true;
            this.txtResult.Name = "txtResult";
            this.txtResult.ReadOnly = true;
            this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtResult.Size = new System.Drawing.Size(383, 113);
            this.txtResult.TabIndex = 4;
            this.txtResult.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtCode_KeyDown);
            this.txtResult.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCode_KeyPress);
            // 
            // labPalletCodeCnt
            // 
            this.labPalletCodeCnt.AutoSize = true;
            this.labPalletCodeCnt.Font = new System.Drawing.Font("宋体", 12F);
            this.labPalletCodeCnt.Location = new System.Drawing.Point(233, 115);
            this.labPalletCodeCnt.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labPalletCodeCnt.Name = "labPalletCodeCnt";
            this.labPalletCodeCnt.Size = new System.Drawing.Size(24, 16);
            this.labPalletCodeCnt.TabIndex = 0;
            this.labPalletCodeCnt.Text = "<=";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("宋体", 12F);
            this.label3.ForeColor = System.Drawing.Color.Red;
            this.label3.Location = new System.Drawing.Point(30, 276);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(496, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "注：使用此功能时候，请将扫码枪扫码输出设置成（码+回车）模式。";
            // 
            // labFace
            // 
            this.labFace.AutoSize = true;
            this.labFace.Font = new System.Drawing.Font("宋体", 12F);
            this.labFace.ForeColor = System.Drawing.Color.Red;
            this.labFace.Location = new System.Drawing.Point(233, 77);
            this.labFace.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labFace.Name = "labFace";
            this.labFace.Size = new System.Drawing.Size(24, 16);
            this.labFace.TabIndex = 0;
            this.labFace.Text = "<=";
            // 
            // btnFaceDown
            // 
            this.btnFaceDown.Location = new System.Drawing.Point(389, 67);
            this.btnFaceDown.Margin = new System.Windows.Forms.Padding(2);
            this.btnFaceDown.Name = "btnFaceDown";
            this.btnFaceDown.Size = new System.Drawing.Size(132, 33);
            this.btnFaceDown.TabIndex = 1;
            this.btnFaceDown.Text = "下假电芯";
            this.btnFaceDown.UseVisualStyleBackColor = true;
            this.btnFaceDown.Click += new System.EventHandler(this.btnFaceDown_Click);
            // 
            // checkInPallet
            // 
            this.checkInPallet.AutoSize = true;
            this.checkInPallet.Location = new System.Drawing.Point(131, 7);
            this.checkInPallet.Name = "checkInPallet";
            this.checkInPallet.Size = new System.Drawing.Size(72, 16);
            this.checkInPallet.TabIndex = 5;
            this.checkInPallet.Text = "扫码入盘";
            this.checkInPallet.UseVisualStyleBackColor = true;
            // 
            // ManualScanCode
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(533, 323);
            this.Controls.Add(this.checkInPallet);
            this.Controls.Add(this.btnFaceDown);
            this.Controls.Add(this.btnCode);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.txtCodeCnt);
            this.Controls.Add(this.txtFaceCodeCnt);
            this.Controls.Add(this.txtCode);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.labFace);
            this.Controls.Add(this.labPalletCodeCnt);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labCode);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManualScanCode";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "人工扫码窗口";
            this.Load += new System.EventHandler(this.ManualScanCode_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Label labCode;
        public System.Windows.Forms.TextBox txtCode;
        public System.Windows.Forms.Button btnCode;
        public System.Windows.Forms.Label label1;
        public System.Windows.Forms.Label label2;
        public System.Windows.Forms.Label label5;
        public System.Windows.Forms.TextBox txtFaceCodeCnt;
        public System.Windows.Forms.TextBox txtCodeCnt;
        public System.Windows.Forms.TextBox txtResult;
        public System.Windows.Forms.Label labPalletCodeCnt;
        public System.Windows.Forms.Label label3;
        public System.Windows.Forms.Label labFace;
        public System.Windows.Forms.Button btnFaceDown;
        private System.Windows.Forms.CheckBox checkInPallet;
    }
}