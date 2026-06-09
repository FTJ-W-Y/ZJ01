using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Machine.UI
{
    public partial class frmUploadOfflineData : Form
    {
        public MesInterface mes;
        public bool buploaddata;
        public frmUploadOfflineData()
        {
            InitializeComponent();
            CenterToParent();
            CheckForIllegalCrossThreadCalls = false;
        }
        private void frmUploadOfflineData_Load(object sender, EventArgs e)
        {
            //   this.checkthress
            string mesProcessName = "";
            MachineCtrl.GetInstance().GetMesProcessName(mes, ref mesProcessName);
            string filePath = string.Format("{0}\\MES离线上传\\{1}", MachineCtrl.GetInstance().ProductionFilePath, mesProcessName);
            txtFilePath.Text = filePath;

            //D:\生产信息\MES上传\进站
            string fullpath = Path.Combine(filePath, "offlinedata.mes");
            txtFilePath.Text = fullpath;
        }

        private async void btnUpload_Click(object sender, EventArgs e)
        {
            MachineCtrl.GetInstance().IsMESConnect = true;
            if (MachineCtrl.GetInstance().IsMESConnect)
            {
                List<string> mesofflinedata = new List<string>();
                //if (MachineCtrl.GetInstance().bAutoIsOfflineUpload)
                //{
                bool bret = MachineCtrl.GetInstance().GetFirstFileMesData(mes, ref mesofflinedata);
                //}
                if (bret)
                {
                    await Task.Run(() =>
                    {
                        if (buploaddata == false && mesofflinedata.Count > 0)
                        {
                            btnUpload.Enabled = false;
                            btnUpload.Text = "上传中....";
                            buploaddata = true;
                            //if (mes == MesInterface.EquToMesInBaking)
                            //{
                            //    bret = MachineCtrl.GetInstance().UpdataMesData1(mesofflinedata);
                            //}
                            //if (mes == MesInterface.EquToMesOutBaking)
                            //{
                            //    bret = MachineCtrl.GetInstance().UpdataMesData2(mesofflinedata);
                            //}
                            if (mes == MesInterface.EquToMesBindingOrUnBind)
                            {
                                bret = MachineCtrl.GetInstance().UpdataMesData3(mesofflinedata);
                            }

                            if (bret)
                            {
                                MachineCtrl.GetInstance().DeleteOfflineDataFile(mes);
                                buploaddata = false;
                                btnUpload.Enabled = true;
                                btnUpload.Text = "上传完成";
                            }
                            else
                            {
                                buploaddata = false;

                                btnUpload.Text = "中途中断,上传失败!";
                                MachineCtrl.GetInstance().DeleteOfflineDataFile(mes);
                                for (int j = 0; j < mesofflinedata.Count; j++)
                                {
                                    MachineCtrl.GetInstance().SaveMesLeftData(mes, mesofflinedata[j]);
                                }
                                btnUpload.Enabled = true;
                            }

                        }
                        else
                        {
                            if (buploaddata == true)
                            {
                                MessageBox.Show("正在上传离线数据!");
                            }
                            if (mesofflinedata.Count == 0)
                            {
                                MessageBox.Show("没有可上传的离线数据!");
                            }
                        }

                    }
                        );

                }
                else
                {
                    MessageBox.Show("没有可上传的离线数据!");

                }
            }
            else
            {
                MessageBox.Show("MES心跳断开");
            }
        }


    }
}

