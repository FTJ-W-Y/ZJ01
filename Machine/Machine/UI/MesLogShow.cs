using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Machine.UI
{
    public partial class MesLogShow : Form
    {
        public MesLogShow()
        {
            InitializeComponent();
            //CreateMesLog();
        }

        //private void CreateMesLog()
        //{
        //    DataGridView dgv = new DataGridView();
        //    for (int i = 0; i < 2; i++)
        //    {
        //        int idx = dgv.Columns.Add("SFC", "SFC条码");
        //        dgv.Columns[idx].FillWeight = 5;     // 宽度占比权重   
        //        idx = dgv.Columns.Add("MadeTime", "生产时间");
        //        dgv.Columns[idx].FillWeight = 5;
        //    }
        //}

    }
}
