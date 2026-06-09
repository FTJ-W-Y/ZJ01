using System;
using System.IO;
using System.Windows.Forms;
using HelperLibrary;
using Machine.UI;
using SystemControlLibrary;

namespace Machine
{
    public partial class MesInterfacePage : FormEx
    {
        #region // 字段

        MesInterface mesInterface;
        System.Timers.Timer timerUpdata;        // 界面更新定时器

        #endregion

        public MesInterfacePage()
        {
            InitializeComponent();

            CreateParameterView();
        }

        #region // 加载及销毁窗体

        private void MesParameterPage_Load(object sender, EventArgs e)
        {
            // 开启定时器
            this.timerUpdata = new System.Timers.Timer();
            this.timerUpdata.Elapsed += TimerUpdata_MesInfo;
            this.timerUpdata.Interval = 500;         // 间隔时间
            this.timerUpdata.AutoReset = true;       // 设置是执行一次（false）还是一直执行(true)；
            this.timerUpdata.Start();                // 开始执行定时器
        }

        /// <summary>
        /// 释放线程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void DisposeForm()
        {
            try
            {
                // 关闭定时器
                if(null != this.timerUpdata)
                {
                    this.timerUpdata.Stop();
                }
            }
            catch(System.Exception ex)
            {
            }
        }

        /// <summary>
        /// UI界面可见性发生改变
        /// </summary>
        /// <param name="show">是否在前台显示</param>
        public override void UIVisibleChanged(bool show)
        {
            if (null != this.timerUpdata)
            {
                if (show)
                {
                    UpdataMesConfig(this.mesInterface);      // 加载参数
                    this.timerUpdata.Start();                // 开始执行定时器
                    UpdataUIEnable(MachineCtrl.GetInstance().RunsCtrl.GetMCState(), MachineCtrl.GetInstance().dbRecord.UserLevel());
                }
                else
                {
                    this.timerUpdata.Stop();
                    SetUIEnable(UIEnable.AllDisabled);
                }
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
                if((MCState.MCInitializing == mc) || (MCState.MCRunning == mc) || (level > UserLevelType.USER_MAINTENANCE))
                {
                    SetUIEnable(UIEnable.AllDisabled);
                }
                else if(level <= UserLevelType.USER_MAINTENANCE)
                {
                    SetUIEnable(UIEnable.AllEnabled);
                }
            }
            catch(System.Exception ex)
            {
                Def.WriteLog($"{this.mesInterface}.MesParameterPage.UpdataUIEnable()", ex.Message, LogType.Error);
            }
            base.UpdataUIEnable(mc, level);
        }

        /// <summary>
        /// 设置当前界面接口
        /// </summary>
        /// <param name="mes"></param>
        public void SetInterface(MesInterface mes)
        {
            this.mesInterface = mes;
        }

        #endregion

        #region // 界面使能

        /// <summary>
        /// 设置界面控件使能
        /// </summary>
        /// <param name="uiEN"></param>
        private void SetUIEnable(UIEnable uiEN)
        {
            this.Invoke(new Action(() =>
            {
                bool en = (uiEN == UIEnable.AllEnabled);

                this.checkBoxEnable.Enabled = en;
                this.textBoxUri.Enabled = en;
                this.dataGridViewParameter.Enabled = en;
                this.buttonSave.Enabled = en;
            }));
        }

        #endregion

        #region // 界面数据

        private void CreateParameterView()
        {
            // 设置表格
            DataGridViewNF[] dgv = new DataGridViewNF[] { this.dataGridViewParameter };
            for(int i = 0; i < dgv.Length; i++)
            {
                dgv[i].SetViewStatus();
                dgv[i].ReadOnly = false;
                dgv[i].AllowUserToAddRows = true;         // 可以添加行
                dgv[i].AllowUserToDeleteRows = true;      // 可以删除行
                dgv[i].EditMode = DataGridViewEditMode.EditOnEnter;
                // 项
                DataGridViewTextBoxColumn txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数代码";
                int idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;     // 宽度占比权重
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数名称";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数单位";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数设定值上限";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数设定值";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "参数设定值下限";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                txtBoxCol = new DataGridViewTextBoxColumn();
                txtBoxCol.HeaderText = "映射的程序参数名";
                idx = dgv[i].Columns.Add(txtBoxCol);
                //dgv[i].Columns[idx].FillWeight = 50;
                foreach(DataGridViewColumn item in dgv[i].Columns)
                {
                    item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
                }
            }
        }

        private void UpdataMesConfig(MesInterface mes)
        {
            MesConfig cfg = MesDefine.GetMesCfg(mes);
            if (null != cfg)
            {
                this.BeginInvoke(new Action(() =>
                {
                    this.checkBoxEnable.Checked = cfg.enable;
                    this.textBoxUri.Text = cfg.mesUri;
                    if (cfg.recipe.Count > 0)
                    {
                        this.dataGridViewParameter.Rows.Clear();
                        foreach(var item in cfg.recipe.Values)
                        {
                            int index = this.dataGridViewParameter.Rows.Add();
                            this.dataGridViewParameter.Rows[index].Height = 25;        // 行高度
                            int cellIdx = 0;
                            this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.RecipeCode;
                            this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.Version;
                            this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.ProductCode;
                            this.dataGridViewParameter.Rows[index].Cells[cellIdx++].Value = item.LastUpdateOnTime;
                        }
                    }
                }));
            }
        }

        private void TimerUpdata_MesInfo(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                //this.Visible = true;
                //if (this.Visible)
                {
                    UpdataMesData();
                }
            }
            catch (System.Exception ex)
            {
                Def.WriteLog("MesParameterPage.TimerUpdata_MesInfo()", ex.ToString());
            }
        }

        private void checkBoxMesEnable_CheckedChanged(object sender, EventArgs e)
        {
            this.checkBoxEnable.Text = this.checkBoxEnable.Checked ? "接口启用" : "接口停用";
            MesConfig cfg = MesDefine.GetMesCfg(this.mesInterface);
            if(null != cfg)
            {
                cfg.enable = this.checkBoxEnable.Checked;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            MesConfig cfg = new MesConfig();
            cfg.enable = this.checkBoxEnable.Checked;
            cfg.mesUri = this.textBoxUri.Text;

            MesRecipeStruct mesRecipeStruct = new MesRecipeStruct();

            DataGridViewNF dgv = this.dataGridViewParameter;
            MesDefine.GetMesCfg(this.mesInterface).Copy(cfg);
            MesDefine.WriteConfig(this.mesInterface);
        }

        /// <summary>
        /// 更新MES数据
        /// </summary>
        private void UpdataMesData()
        {
            MesConfig cfg = MesDefine.GetMesCfg(this.mesInterface);
            if(null != cfg)
            {
                this.BeginInvoke(new Action(() =>
                {
                    //cfg.updataRS = true;
                    if (cfg.updataRS)
                    {
                        this.textBoxSend.Text = $"{DateTime.Now.ToString(Def.DateFormal)}上传：\r\n\r\n{cfg.send}";
                        this.textBoxRecv.Text = $"{DateTime.Now.ToString(Def.DateFormal)}接收：\r\n\r\n{cfg.recv}";
                        cfg.updataRS = false;
                    }
                }));
            }
        }

        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            string mesProcessName = "";
            MachineCtrl.GetInstance().GetMesProcessName(mesInterface, ref mesProcessName);
            string filePath = string.Format("{0}\\MES离线上传\\{1}", MachineCtrl.GetInstance().ProductionFilePath, mesProcessName);
            if (!Directory.Exists(filePath))
            {
                MessageBox.Show("没有离线数据!");
                return;
            }
            string fullpath = Path.Combine(filePath, "offlinedata.mes");
            if (!File.Exists(fullpath))
            {
                MessageBox.Show("没有离线数据!");
                return;
            }
            //if (mesInterface == MesInterface.EquToMesInBaking)
            //{
            //    frmUploadOfflineData pullinData = new frmUploadOfflineData();
            //    pullinData.mes = mesInterface;
            //    pullinData.buploaddata = MachineCtrl.GetInstance().bUploadingOfflineDataIn;
            //    pullinData.Text = "入站校验离线数据";
            //    pullinData.ShowDialog();
            //    MachineCtrl.GetInstance().bUploadingOfflineDataIn = false;
            //}
            //else if(mesInterface == MesInterface.EquToMesOutBaking)
            //{
            //    frmUploadOfflineData pulloutData = new frmUploadOfflineData();
            //    pulloutData.mes = MesInterface.EquToMesOutBaking;
            //    pulloutData.buploaddata = MachineCtrl.GetInstance().bUploadingOfflineDataOut;
            //    pulloutData.Text = "出站上传线数据";
            //    pulloutData.ShowDialog();
            //    MachineCtrl.GetInstance().bUploadingOfflineDataOut = false;
            //}
            //else if (mesInterface == MesInterface.EquToMesBindingOrUnBind)
            //{
            //    frmUploadOfflineData BindData = new frmUploadOfflineData();
            //    BindData.mes = MesInterface.EquToMesOutBaking;
            //    BindData.buploaddata = MachineCtrl.GetInstance().bUploadingOfflineDataBind;
            //    BindData.Text = "托盘绑定与解绑离线数据";
            //    BindData.ShowDialog();
            //    MachineCtrl.GetInstance().bUploadingOfflineDataBind = false;
            //}
            //else
            //{
            //    MessageBox.Show("没有离线数据!");
            //    return;
            //}

        }
    }
}
