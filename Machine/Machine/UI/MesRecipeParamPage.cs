using System;
using System.Windows.Forms;
using HelperLibrary;
using SystemControlLibrary;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;

namespace Machine
{
    public partial class MesRecipeParamPage : FormEx
    {
        #region // 字段

        MesInterface mesInterface;
        System.Timers.Timer timerUpdata;        // 界面更新定时器

        string RecipeCode = "";

        #endregion

        public MesRecipeParamPage()
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

                //this.checkBoxEnableRecipeList.Enabled = en;
                //this.checkBoxEnableRecipeGet.Enabled = en;
                //this.checkBoxEnableVExamine.Enabled = en;
                //this.checkBoxEnableRecipe.Enabled = en;

                //this.textBoxUriRecipeList.Enabled = en;
                //this.textBoxUriRecipeGet.Enabled = en;
                //this.textBoxUriVExamine.Enabled = en;
                //this.textBoxUriRecipe.Enabled = en;

                this.dataGridViewRecipe.Enabled = en;
                this.dataGridViewParameter.Enabled = en;

                this.buttonSave.Enabled = en;
                this.buttonRecipeList.Enabled = en;
                this.buttonRecipeGet.Enabled = en;
                this.buttonVExamine.Enabled = en;
            }));
        }

        #endregion

        #region // 界面数据

        private void CreateParameterView()
        {
            // 设置配方表格
            DataGridViewNF dgv = this.dataGridViewRecipe ;
            dgv.SetViewStatus();
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = true;         // 可以添加行
            dgv.AllowUserToDeleteRows = true;      // 可以删除行
            //dgv.EditMode = DataGridViewEditMode.EditOnEnter;
            // 项
            DataGridViewTextBoxColumn txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "配方编码";
            int idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;     // 宽度占比权重
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "工艺路线代码";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "版本信息";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "工序编号";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "最后更新时间";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;
            foreach(DataGridViewColumn item in dgv.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
            }
            // 设置配方中参数表格
            dgv = this.dataGridViewParameter;
            dgv.SetViewStatus();
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = true;         // 可以添加行
            dgv.AllowUserToDeleteRows = true;      // 可以删除行
            //dgv.EditMode = DataGridViewEditMode.EditOnEnter;
            // 项
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "参数编码";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;     // 宽度占比权重
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "参数类型";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "推荐值";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "单位";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "参数上限";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "参数下限";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;
            txtBoxCol = new DataGridViewTextBoxColumn();
            txtBoxCol.HeaderText = "参数描述";
            idx = dgv.Columns.Add(txtBoxCol);
            //dgv[i].Columns[idx].FillWeight = 50;
            foreach(DataGridViewColumn item in dgv.Columns)
            {
                item.SortMode = DataGridViewColumnSortMode.NotSortable;     // 禁止列排序
            }

        }

        private void UpdataMesConfig(MesInterface mes)
        {
            MesConfig cfg = MesDefine.GetMesCfg(mes);
            if (null != cfg)
            {
                this.BeginInvoke(new Action(() =>
                {
                    //txtRecipeCode.Text = MesResources.Equipment.MesRecipeCode;

                    //cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeGet);
                    //this.checkBoxEnableRecipeGet.Checked = cfg.enable;
                    //this.textBoxUriRecipeGet.Text = cfg.mesUri;
                    //cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeVExamine);
                    //this.checkBoxEnableVExamine.Checked = cfg.enable;
                    //this.textBoxUriVExamine.Text = cfg.mesUri;
                    //cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipe);
                    //this.checkBoxEnableRecipe.Checked = cfg.enable;
                    //this.textBoxUriRecipe.Text = cfg.mesUri;
                    // 最后获取，里面包含参数信息
                    cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeListGet);
                    //this.checkBoxEnableRecipeList.Checked = cfg.enable;
                    //this.textBoxUriRecipeList.Text = cfg.mesUri;
                    if(cfg.recipe.Count > 0)
                    {
                        bool fillParam = true;
                        DataGridViewNF dgv = this.dataGridViewRecipe;
                        dgv.Rows.Clear();
                        foreach(var item in cfg.recipe.Values)
                        {
                            int cellIdx = 0;
                            int index = dgv.Rows.Add();
                            dgv.Rows[index].Height = 25;        // 行高度
                            dgv.Rows[index].Cells[cellIdx++].Value = item.RecipeCode;
                            dgv.Rows[index].Cells[cellIdx++].Value = item.ProductCode;
                            dgv.Rows[index].Cells[cellIdx++].Value = item.Version;
                            dgv.Rows[index].Cells[cellIdx++].Value = item.OprSequenceNo;
                            dgv.Rows[index].Cells[cellIdx++].Value = item.LastUpdateOnTime;
                            if (fillParam && (null != item.ParamData))
                            {
                                fillParam = false;
                                DataGridViewNF dgvParam = this.dataGridViewParameter;
                                dgvParam.Rows.Clear();
                                foreach(var param in item.ParamData)
                                {
                                    cellIdx = 0;
                                    index = dgvParam.Rows.Add();
                                    dgvParam.Rows[index].Height = 25;        // 行高度
                                    dgvParam.Rows[index].Cells[cellIdx++].Value = param.StepID;
                                    dgvParam.Rows[index].Cells[cellIdx++].Value = param.StepName;
                                    dgvParam.Rows[index].Cells[cellIdx++].Value = param.ParamID;
                                    dgvParam.Rows[index].Cells[cellIdx++].Value = param.ParamName;
                                    dgvParam.Rows[index].Cells[cellIdx++].Value = param.ParamStand;
                                    dgvParam.Rows[index].Cells[cellIdx++].Value = param.ParamUpper;
                                    dgvParam.Rows[index].Cells[cellIdx++].Value = param.ParamLower;
                                }
                            }
                        }
                    }
                    this.dataGridViewParameter.Sort(dataGridViewParameter.Columns[0], ListSortDirection.Ascending);
                }));
            }
        }

        private void TimerUpdata_MesInfo(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (this.Visible)
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
            CheckBox cbox = sender as CheckBox;
            if (null != cbox)
            {
                string title = cbox.Text.Substring(0, 2);
                cbox.Text = title + (cbox.Checked ? "启用" : "停用");
                MesConfig cfg = MesDefine.GetMesCfg(this.mesInterface);
                if(null != cfg)
                {
                    cfg.enable = cbox.Checked;
                }
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtRecipeCode.Text))
            {
                MessageBox.Show("工艺路线代码不能为空");
                return;
            }
            if (string.IsNullOrEmpty(this.txtVersion.Text))
            {
                MessageBox.Show("版本信息不能为空");
                return;
            }
            if (string.IsNullOrEmpty(this.txtOprSequenceNo.Text))
            {
                MessageBox.Show("工序编号不能为空");
                return;
            }

            MesRecipeStruct mesRecipeStruct = new MesRecipeStruct();
            string productCode = this.txtRecipeCode.Text;
            string version = this.txtVersion.Text;
            string oprSequenceNo = this.txtOprSequenceNo.Text;
            // 获取主动获取参数接口的MES参数
            MesConfig cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeGet);
            cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeListGet);
            //MesDefine.WriteConfig(MesInterface.EquToMesRecipeListGet, productCode, version, oprSequenceNo, ref mesRecipeStruct);
            //MesDefine.WriteConfig(MesInterface.EquToMesRecipeListGet,ref mesRecipeStruct);
            MesResources.Equipment.MesRecipeCode = RecipeCode;
            MesResources.WriteConfig();

            //DataGridViewNF dgv = this.dataGridViewRecipe;
            //dgv.Rows.Clear();
            //int cellidx = 0;
            //dgv.Rows[0].Height = 25;        // 行高度
            //dgv.Rows[0].Cells[cellidx++].Value = "ParameterInfoOld";
            //dgv.Rows[0].Cells[cellidx++].Value = this.txtRecipeCode.Text;
            //dgv.Rows[0].Cells[cellidx++].Value = this.txtVersion.Text;
            //dgv.Rows[0].Cells[cellidx++].Value = this.txtOprSequenceNo.Text;
            //dgv.Rows[0].Cells[cellidx++].Value = DateTime.Now;


            DataGridViewNF dgvParam = this.dataGridViewParameter;
            dgvParam.Rows.Clear();

            foreach (var param in mesRecipeStruct.Param)
            {
                int cellIdx = 0;
                int index = dgvParam.Rows.Add();
                dgvParam.Rows[index].Height = 25;        // 行高度
                dgvParam.Rows[index].Cells[cellIdx++].Value = param.ParameterCode;
                dgvParam.Rows[index].Cells[cellIdx++].Value = param.ParameterType;
                dgvParam.Rows[index].Cells[cellIdx++].Value = param.TargetValue;
                dgvParam.Rows[index].Cells[cellIdx++].Value = param.UomCode;
                dgvParam.Rows[index].Cells[cellIdx++].Value = param.UpperControlLimit;
                dgvParam.Rows[index].Cells[cellIdx++].Value = param.LowerControlLimit;
                dgvParam.Rows[index].Cells[cellIdx++].Value = param.Description;
            }
            this.dataGridViewParameter.Sort(dataGridViewParameter.Columns[0], ListSortDirection.Ascending);

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
                    if(cfg.updataRS)
                    {
                        //this.textBoxSend.Text = $"{DateTime.Now.ToString(Def.DateFormal)}上传：\r\n\r\n{cfg.send}";
                        //this.textBoxRecv.Text = $"{DateTime.Now.ToString(Def.DateFormal)}接收：\r\n\r\n{cfg.recv}";
                        cfg.updataRS = false;
                    }
                }));
            }
        }

        #endregion

        #region // 获取参数操作

        /// <summary>
        /// EquToMesRecipeListGet,          // 获取开机参数列表
        /// </summary>
        /// <returns></returns>
        private void buttonRecipeList_Click(object sender, EventArgs e)
        {
            if (MachineCtrl.GetInstance().MesRecipeListGet())
            {
                UpdataMesConfig(this.mesInterface);
            }
        }

        /// <summary>
        /// EquToMesRecipeGet,              // 获取开机参数明细
        /// </summary>
        /// <returns></returns>
        private void buttonRecipeGet_Click(object sender, EventArgs e)
        {
            //if (string.IsNullOrEmpty(RecipeCode))
            //{
            //    MessageBox.Show("请选择配方参数!");
            //    return;
            //}
            if(MachineCtrl.GetInstance().MesRecipeGet(RecipeCode))
            {
                UpdataMesConfig(this.mesInterface);
            }
        }

        /// <summary>
        /// EquToMesRecipeVExamine,         // 开机参数版本校验
        /// </summary>
        /// <returns></returns>
        private void buttonVExamine_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(RecipeCode))
            {
                MessageBox.Show("请选择检验配方参数！");
                return;
            }
            if (MachineCtrl.GetInstance().MesRecipeVExamine(RecipeCode))
            {
                txtRecipeCode.Text = RecipeCode;
                ShowMsgBox.ShowDialog("开机参数版本校验 及 开机参数采集 操作成功", MessageType.MsgMessage);
            }
        }

        #endregion

        private void dataGridViewRecipe_Click(object sender, EventArgs e)
        {
            try
            {
                RecipeCode = "";
                int index = dataGridViewParameter.CurrentRow.Index;
                if (index != -1)
                {
                    DataGridViewNF dgvParam = this.dataGridViewRecipe;
                    RecipeCode = dgvParam.Rows[index].Cells[0].Value.ToString();
                    Trace.WriteLine(RecipeCode);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void dataGridViewRecipe_CellClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        
        /// <summary>
        /// 设备主动获取参数按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGetACEQPTPARM_Click(object sender, EventArgs e)
        {
            //    MesRecipeStruct mesRecipeStruct = new MesRecipeStruct();
            //    mesRecipeStruct.RecipeCode = this.txtRecipeCode.Text.ToString();
            //    mesRecipeStruct.Version = this.txtVersion.Text.ToString();
            //;   mesRecipeStruct.OprSequenceNo = this.txtOprSequenceNo.Text.ToString();

            //    string msg ="";
            //    if (!MachineCtrl.GetInstance().ACEQPTPARM_Main(MesResources.Equipment, ref mesRecipeStruct, ref msg))
            //    {
            //        ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            //    }
            //    DataGridViewNF dgv = this.dataGridViewRecipe;
            //    dgv.Rows.Clear();
            //    int cellidx = 0;
            //    dgv.Rows[0].Height = 25;        // 行高度
            //    dgv.Rows[0].Cells[cellidx++].Value = "ParameterInfoOld";
            //    dgv.Rows[0].Cells[cellidx++].Value = this.txtRecipeCode.Text;
            //    dgv.Rows[0].Cells[cellidx++].Value = this.txtVersion.Text;
            //    dgv.Rows[0].Cells[cellidx++].Value = this.txtOprSequenceNo.Text;
            //    dgv.Rows[0].Cells[cellidx++].Value = DateTime.Now;


            //    DataGridViewNF dgvParam = this.dataGridViewParameter;
            //    dgvParam.Rows.Clear();

            //    foreach (var param in mesRecipeStruct.Param)
            //    {
            //        int cellIdx = 0;
            //        int index = dgvParam.Rows.Add();
            //        dgvParam.Rows[index].Height = 25;        // 行高度
            //        dgvParam.Rows[index].Cells[cellIdx++].Value = param.ParameterCode;
            //        dgvParam.Rows[index].Cells[cellIdx++].Value = param.ParameterType;
            //        dgvParam.Rows[index].Cells[cellIdx++].Value = param.TargetValue;
            //        dgvParam.Rows[index].Cells[cellIdx++].Value = param.UomCode;
            //        dgvParam.Rows[index].Cells[cellIdx++].Value = param.UpperControlLimit;
            //        dgvParam.Rows[index].Cells[cellIdx++].Value = param.LowerControlLimit;
            //        dgvParam.Rows[index].Cells[cellIdx++].Value = param.Description;
            //    }
            //    this.dataGridViewParameter.Sort(dataGridViewParameter.Columns[0], ListSortDirection.Ascending);
            //    //UpdataMesConfig(this.mesInterface); //刷新获取的数据

        }
    }
}
