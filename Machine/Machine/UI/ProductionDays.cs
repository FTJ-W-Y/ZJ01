using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Machine.UI
{
    public partial class ProductionDays : FormEx
    {
        public ProductionDays()
        {
            InitializeComponent();
        }
        public string Hr = "0";
        public int Sum = 0;

        List<TextBox> lstHour = new List<TextBox>();
        List<TextBox> lstHourNg = new List<TextBox>();

        private void ProductionDays_Load(object sender, EventArgs e)
        {
            if (MachineCtrl.GetInstance().MachineID == 0)
            {
                labOnOff1.Text = labOnOff2.Text = "上料数";
            }
            else if (MachineCtrl.GetInstance().MachineID == 2)
            {
                labOnOff1.Text = labOnOff2.Text = "下料数";
            }
            
            lstHour.Add(txtBox1);
            lstHour.Add(txtBox2);
            lstHour.Add(txtBox3);
            lstHour.Add(txtBox4);
            lstHour.Add(txtBox5);
            lstHour.Add(txtBox6);
            lstHour.Add(txtBox7);
            lstHour.Add(txtBox8);
            lstHour.Add(txtBox9);
            lstHour.Add(txtBox10);
            lstHour.Add(txtBox11);
            lstHour.Add(txtBox12);
            lstHour.Add(txtBox13);
            lstHour.Add(txtBox14);
            lstHour.Add(txtBox15);
            lstHour.Add(txtBox16);
            lstHour.Add(txtBox17);
            lstHour.Add(txtBox18);
            lstHour.Add(txtBox19);
            lstHour.Add(txtBox20);
            lstHour.Add(txtBox21);
            lstHour.Add(txtBox22);
            lstHour.Add(txtBox23);
            lstHour.Add(txtBox24);
            for (int id = 0; id <lstHour.Count; id++)
            {
                lstHour[id].Text = "0";
            }

            lstHourNg.Add(txtBoxNG1);
            lstHourNg.Add(txtBoxNG2);
            lstHourNg.Add(txtBoxNG3);
            lstHourNg.Add(txtBoxNG4);
            lstHourNg.Add(txtBoxNG5);
            lstHourNg.Add(txtBoxNG6);
            lstHourNg.Add(txtBoxNG7);
            lstHourNg.Add(txtBoxNG8);
            lstHourNg.Add(txtBoxNG9);
            lstHourNg.Add(txtBoxNG10);
            lstHourNg.Add(txtBoxNG11);
            lstHourNg.Add(txtBoxNG12);
            lstHourNg.Add(txtBoxNG13);
            lstHourNg.Add(txtBoxNG14);
            lstHourNg.Add(txtBoxNG15);
            lstHourNg.Add(txtBoxNG16);
            lstHourNg.Add(txtBoxNG17);
            lstHourNg.Add(txtBoxNG18);
            lstHourNg.Add(txtBoxNG19);
            lstHourNg.Add(txtBoxNG20);
            lstHourNg.Add(txtBoxNG21);
            lstHourNg.Add(txtBoxNG22);
            lstHourNg.Add(txtBoxNG23);
            lstHourNg.Add(txtBoxNG24);

            for (int id = 0; id < lstHourNg.Count; id++)
            {
                lstHourNg[id].Text = "0";
            }
        }

        void OpenProductionDaysScv(string filePath)
        {
            //实例化一个datatable用来存储数据
            DataTable dt = new DataTable();

            //文件流读取
            System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.OpenOrCreate);
            System.IO.StreamReader sr = new System.IO.StreamReader(fs, Encoding.GetEncoding(0));

            string tempText = "";
            bool isFirst = true;
            while ((tempText = sr.ReadLine()) != null)
            {
                //string[] arr = tempText.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                
                tempText = tempText.Trim();
                string[] arr = tempText.Split(",".ToCharArray());
                //一般第一行为标题，所以取出来作为标头
                if (isFirst)
                {
                    foreach (string str in arr)
                    {
                        dt.Columns.Add(str);
                    }
                    isFirst = false;
                }
                else
                {
                    //从第二行开始添加到datatable数据行
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (i == 0)
                        {
                            dr[i] = DateTime.Parse(arr[i]);
                        }
                        else
                        {
                            dr[i] = i < arr.Length ? arr[i] : "";
                        }

                    }
                    dt.Rows.Add(dr);
                }
            }
            //关闭流
            sr.Close();
            fs.Close();
            //csv 时间,条码
            var result = from r in dt.AsEnumerable()
                     group r by new { time = DateTime.Parse(r["时间"].ToString()).ToString("HH") } into g
                     select new
                     {
                         hr = g.Key.time,
                         sum = g.Count()
                     };

            for (int id =0; id < lstHour.Count; id++)
            {
                lstHour[id].Text = "0";
            }
            int idx = 0;
            foreach (var itm in result)
            {
                idx = -1;
                if (!string.IsNullOrEmpty(itm.hr))
                {
                    if (itm.hr.StartsWith("0"))
                    {
                        idx = Convert.ToInt32(itm.hr.Substring(1));
                    }
                    else
                    {
                        idx = Convert.ToInt32(itm.hr);
                    }
                }
                
                if (idx > -1)
                {
                    lstHour[idx].Text = itm.sum.ToString();
                }         
            }
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            DateTime curDay = DateTime.Now;
            //string filePath = $"{@"D:\生产信息\进出条码"}\\{curDay.ToString("yyyy-MM-dd")}\\{curDay.ToString("yyyy-MM-dd")}.csv";
            //if (!File.Exists(filePath))
            //{
            //    MessageBox.Show($"{filePath}文件不存在");
            //    return;
            //}
            //OpenProductionDaysScv(filePath);
            string beginTime = "";
            string endTime = "";
            DateTime dtTime = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd 08:00:00"));
            if (DateTime.Now.Hour >= 8)
            {
                beginTime = dtTime.ToString("yyyy-MM-dd HH:mm:ss");
                endTime = dtTime.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                beginTime = dtTime.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss");
                endTime = dtTime.AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss");
            }
            GetReport(beginTime, endTime);
        }

        private void btnQuery2_Click(object sender, EventArgs e)
        {
            string beginTime = "";
            string endTime = "";
            DateTime dtTime = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd 08:00:00"));
            if (DateTime.Now.Hour >= 8)
            {
                beginTime = dtTime.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss");
                endTime = dtTime.AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                beginTime = dtTime.AddDays(-2).ToString("yyyy-MM-dd HH:mm:ss");
                endTime = dtTime.AddDays(-1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss");
            }
            GetReport(beginTime, endTime);
        }

        private void GetReport(string dateTime, string endTime)
        {
            try
            {
                for (int id = 0; id < lstHour.Count; id++)
                {
                    lstHour[id].Text = "0";
                    lstHourNg[id].Text = "0";
                }
                txtBoxTotal1.Text = "0";
                txtBoxTotal2.Text = "0";
                txtTotalNG1.Text = "0";
                txtTotalNG2.Text = "0";

                DataTable dt = MachineCtrl.GetInstance().GetBakingBatCnt(dateTime, endTime);
                if (dt != null)
                {
                    int hours = 0;
                    int OnloadCnt = 0;
                    int OffloadCnt = 0;
                    int OnloadNgCnt = 0;
                    int dayCnt = 0;
                    int nightCnt = 0;
                    int dayNgCnt = 0;
                    int nightNgCnt = 0;
                    foreach (DataRow row in dt.Rows)
                    {
                        hours = Convert.ToInt16(row["BakingHour"].ToString());
                        OnloadCnt = Convert.ToInt16(row["OnloadCnt"].ToString());
                        OnloadNgCnt = Convert.ToInt16(row["OnloadNgCnt"].ToString());
                        OffloadCnt = Convert.ToInt16(row["OffloadCnt"].ToString());

                        hours = hours == 0 ? 24 : hours;

                        if (MachineCtrl.GetInstance().MachineID == 0)
                        {
                            lstHour[hours - 1].Text = OnloadCnt.ToString();
                            lstHourNg[hours - 1].Text = OnloadNgCnt.ToString();

                            if (hours >= 8 && hours < 20)
                            {
                                dayCnt += OnloadCnt;
                                dayNgCnt += OnloadNgCnt;
                            }
                            else
                            {
                                nightCnt += OnloadCnt;
                                nightNgCnt += OnloadNgCnt;
                            }
                        }
                        else
                        {
                            lstHour[hours - 1].Text = OffloadCnt.ToString();

                            if (hours >= 8 && hours < 20)
                            {
                                dayCnt += OffloadCnt;
                            }
                            else
                            {
                                nightCnt += OffloadCnt;
                            }
                        }
                    }

                    txtBoxTotal1.Text = dayCnt.ToString();
                    txtBoxTotal2.Text = nightCnt.ToString();
                    txtTotalNG1.Text = dayNgCnt.ToString();
                    txtTotalNG2.Text = nightNgCnt.ToString();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
    }
}
