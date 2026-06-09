using System.Collections.Generic;
using static SystemControlLibrary.DataBaseRecord;

namespace Machine
{
    partial class OvenBntPage
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
            this.components = new System.ComponentModel.Container();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnPressure = new System.Windows.Forms.Button();
            this.btnEnable = new System.Windows.Forms.Button();
            this.labState = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.groupBox1.BackColor = System.Drawing.Color.Orange;
            this.groupBox1.Controls.Add(this.btnPressure);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.btnEnable);
            this.groupBox1.Controls.Add(this.labState);
            this.groupBox1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBox1.Location = new System.Drawing.Point(8, 31);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox1.Size = new System.Drawing.Size(365, 194);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            // 
            // btnPressure
            // 
            this.btnPressure.Location = new System.Drawing.Point(194, 102);
            this.btnPressure.Margin = new System.Windows.Forms.Padding(2);
            this.btnPressure.Name = "btnPressure";
            this.btnPressure.Size = new System.Drawing.Size(112, 40);
            this.btnPressure.TabIndex = 6;
            this.btnPressure.Text = "炉腔保压";
            this.btnPressure.UseVisualStyleBackColor = true;
            this.btnPressure.Click += new System.EventHandler(this.btnPressure_Click);
            // 
            // btnEnable
            // 
            this.btnEnable.Location = new System.Drawing.Point(68, 102);
            this.btnEnable.Margin = new System.Windows.Forms.Padding(2);
            this.btnEnable.Name = "btnEnable";
            this.btnEnable.Size = new System.Drawing.Size(112, 40);
            this.btnEnable.TabIndex = 6;
            this.btnEnable.Text = "炉腔使能";
            this.btnEnable.UseVisualStyleBackColor = true;
            this.btnEnable.Click += new System.EventHandler(this.btnEnable_Click);
            // 
            // labState
            // 
            this.labState.AutoSize = true;
            this.labState.Font = new System.Drawing.Font("宋体", 11F);
            this.labState.Location = new System.Drawing.Point(40, 48);
            this.labState.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labState.Name = "labState";
            this.labState.Size = new System.Drawing.Size(67, 15);
            this.labState.TabIndex = 1;
            this.labState.Text = "干燥炉：";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("宋体", 11F);
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(135, 16);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(121, 15);
            this.label2.TabIndex = 1;
            this.label2.Text = "5 秒后自动关闭!";
            // 
            // OvenBntPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Coral;
            this.ClientSize = new System.Drawing.Size(385, 239);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "OvenBntPage";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Machine";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MaintenanceLockPage_FormClosing);
            this.Load += new System.EventHandler(this.OvenBntPage_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OvenBntPage_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OvenBntPage_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OvenBntPage_MouseUp);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnEnable;
        private System.Windows.Forms.Button btnPressure;
        private System.Windows.Forms.Label labState;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label2;
    }
}