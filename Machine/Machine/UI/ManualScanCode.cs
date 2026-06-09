using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Machine
{
    public partial class ManualScanCode : Form
    {
        public string codeInfo { get; private set; }
        public bool manualEndFlag;

        public bool IsHadFake = false;
        public RunID runID = RunID.Invalid;
        public int pltIdx = 0;
        private int faceCodeCnt = 0;
        private int codeCnt = 0;
        private bool palletCodeEmpty = false;
        
        public ManualScanCode()
        {
            InitializeComponent();
        }

        private void ManualScanCode_Load(object sender, EventArgs e)
        {
            labFace.Text = IsHadFake ? "假电池托盘" : "非假电池托盘";
            try
            {
                RunProcess run = MachineCtrl.GetInstance().GetModule(runID);
                if (null != run)
                {
                    btnFaceDown.Enabled = false;

                    int palletCodeCnt = run.Pallet[pltIdx].MaxRow * run.Pallet[pltIdx].MaxCol;
                    // 没有夹具
                    if (PalletStatus.Invalid == run.Pallet[pltIdx].State)
                    {
                        txtCode.Enabled = false;
                        btnCode.Enabled = false;

                        txtResult.Text = $"请先添加空夹具";
                        return;
                    }
                    else if (PalletStatus.ReputFake == run.Pallet[pltIdx].State)
                    {
                        labFace.Text = "待回炉假电池托盘";
                    }
                    else if (PalletStatus.WaitResult == run.Pallet[pltIdx].State)
                    {
                        txtCode.Enabled = false;
                        btnCode.Enabled = false;

                        txtResult.Text = $"夹具[{run.Pallet[pltIdx].Code}]已经下假电池完成";
                        return;
                    }
                    // 待测假电池
                    else if (PalletStatus.Detect == run.Pallet[pltIdx].State)
                    {
                        txtCode.Enabled = false;
                        btnCode.Enabled = false;
                        labFace.Text = "待下假电池托盘";
                        btnFaceDown.Enabled = true;
                    }
                    // 空夹具，未扫码夹具
                    else if (string.IsNullOrEmpty(run.Pallet[pltIdx].Code))
                    {
                        palletCodeEmpty = true;
                        btnCode.Text = "托盘扫码";
                        labCode.Text = "托盘条码：";
                    }

                    for (int row = 0; row < run.Pallet[pltIdx].MaxRow; row++)
                    {
                        for (int col = 0; col < run.Pallet[pltIdx].MaxCol; col++)
                        {
                            if (run.Pallet[pltIdx].Battery[row, col].Type == BatteryStatus.Fake)
                            {
                                faceCodeCnt++;
                                if (PalletStatus.Detect != run.Pallet[pltIdx].State)
                                    labFace.Text = "假电池托盘";
                            }
                            else if (run.Pallet[pltIdx].Battery[row, col].Type == BatteryStatus.OK)
                            {
                                codeCnt++;
                            }
                        }
                    }
                    txtFaceCodeCnt.Text = faceCodeCnt.ToString();
                    txtCodeCnt.Text = (codeCnt + faceCodeCnt).ToString();
                    labPalletCodeCnt.Text = $" <= {palletCodeCnt}";
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("ManualScanCode_Load() error : " + ex.Message);
            }
        }

        bool CodeExists(string code)
        {
            RunProcess run = MachineCtrl.GetInstance().GetModule(runID);
            if (null != run)
            {
                for (int row = 0; row < run.Pallet[pltIdx].MaxRow; row++)
                {
                    for (int col = 0; col < run.Pallet[pltIdx].MaxCol; col++)
                    {
                        if (run.Pallet[pltIdx].Battery[row, col].Code.Equals(code))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void btnCode_Click(object sender, EventArgs e)
        {
            string code = this.txtCode.Text.Trim();
            if (palletCodeEmpty)
            {
                PalletAdd(code);
            }
            else
            {
                AddCode(code);
            }
        }
        
        private void txtCode_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.KeyChar = char.ToUpper(e.KeyChar);

            if (Convert.ToInt32(e.KeyChar) >= 48 && Convert.ToInt32(e.KeyChar) < 58           //包括数字
                || (Convert.ToInt32(e.KeyChar) >= 65 && Convert.ToInt32(e.KeyChar) < 91)      //包括大写字母
                //|| (Convert.ToInt32(e.KeyChar) >= 97 && Convert.ToInt32(e.KeyChar) < 123)        //包括小写字母
                //|| (Convert.ToInt32(e.KeyChar) == 46)                                            //包括.
                //|| (Convert.ToInt32(e.KeyChar) == 32)                                            //包括空格
                //|| (Convert.ToInt32(e.KeyChar) == 64)                                            //包括@
                //|| (Convert.ToInt32(e.KeyChar) > 127)                                            //包括中文
                || Convert.ToInt32(e.KeyChar) == 8 )                                                //包括退格
            {

            }
            else
            {
                e.Handled = true;
            }
        }

        private void txtCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string code = txtCode.Text.Trim();
                if (palletCodeEmpty)
                {
                    PalletAdd(code);
                }
                else
                {
                    AddCode(code);
                }
            }
        }

        private void AddCode(string code)
        {
            Trace.WriteLine("提交MES校验电芯");
            // 提交MES校验电芯
            // 校验成功加入托盘
            // 校验失败报警

            if (string.IsNullOrEmpty(code) && code.Length != 24)
            {
                txtResult.Text = $"{code} 电芯条码不能为空";
                return;
            }
            if (code.Length != 24)
            {
                txtResult.Text = $"{code} 电芯条码长度不正确，请确认系统输入法是否为英文输入！";
                return;
            }

            string errMsg = "";
            if (!checkInPallet.Checked)
            {
                if (!Def.IsNoHardware() && !MesOperate.EquToMesCheckSfc(MesResources.Equipment, code, ref errMsg))
                {
                    txtResult.Text = $"{code}电芯校验异常:{errMsg}";
                }
                else
                {
                    txtResult.Text = $"{code}电芯校验，正常条码";
                }
            }
            else
            {
                if (!CodeExists(code))
                {
                    // 插入托盘
                    try
                    {
                        RunProcess run = MachineCtrl.GetInstance().GetModule(runID);
                        if (null != run)
                        {
                            if (!IsHadFake || !string.IsNullOrEmpty(run.Pallet[pltIdx].Battery[0, 0].Code))
                            {
                                if (!Def.IsNoHardware() && !MesOperate.EquToMesCheckSfc(MesResources.Equipment, code, ref errMsg))
                                {
                                    txtResult.Text = $"{code}电芯校验异常:{errMsg}";
                                    return;
                                }
                            }

                            bool exists = false;
                            for (int col = 0; col < run.Pallet[pltIdx].MaxCol; col++)
                            {
                                if (exists)
                                    break;

                                for (int row = 0; row < run.Pallet[pltIdx].MaxRow; row++)
                                {
                                    if (col == 0 && row == 0)
                                    {
                                        if (BatteryStatus.Invalid == run.Pallet[pltIdx].Battery[row, col].Type)
                                        {
                                            run.Pallet[pltIdx].Battery[row, col].Code = code;
                                            run.Pallet[pltIdx].Battery[row, col].Type = IsHadFake ? BatteryStatus.Fake : BatteryStatus.OK;
                                            exists = true;
                                            run.SaveRunData(SaveType.Pallet);
                                            break;
                                        }
                                        else if (BatteryStatus.FakeTag == run.Pallet[pltIdx].Battery[row, col].Type)
                                        {
                                            if (PalletStatus.ReputFake == run.Pallet[pltIdx].State)
                                            {
                                                run.Pallet[pltIdx].State = PalletStatus.Rebaking;
                                                run.Pallet[pltIdx].Battery[row, col].Code = code;
                                                run.Pallet[pltIdx].Battery[row, col].Type = BatteryStatus.Fake;
                                                exists = true;
                                                run.SaveRunData(SaveType.Pallet);
                                                break;
                                            }
                                        }
                                    }
                                    else if (BatteryStatus.Invalid == run.Pallet[pltIdx].Battery[row, col].Type)
                                    {
                                        run.Pallet[pltIdx].Battery[row, col].Code = code;
                                        run.Pallet[pltIdx].Battery[row, col].Type = BatteryStatus.OK;
                                        run.SaveRunData(SaveType.Pallet);
                                        exists = true;
                                        codeCnt++;
                                        break;
                                    }
                                }
                            }
                            if (exists)
                            {
                                txtCodeCnt.Text = (codeCnt + faceCodeCnt).ToString();
                                txtResult.Text = $"{code} 电芯入盘成功";
                            }
                            else
                            {
                                txtResult.Text = $"{code} 电芯入盘失败，托盘已经满";
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Trace.WriteLine("ManualScanCode_Load() error : " + ex.Message);
                    }
                }
                else
                {
                    txtResult.Text = $"{code}电芯已经存在";
                }
            }

            txtCode.Text = "";
        }

        private void PalletAdd(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return;
            }
            // 插入托盘
            try
            {
                RunProcess run = MachineCtrl.GetInstance().GetModule(runID);
                if (null != run)
                {
                    run.Pallet[pltIdx].Code = code;
                    run.Pallet[pltIdx].State = PalletStatus.OK;
                    run.SaveRunData(SaveType.Pallet);

                    palletCodeEmpty = false;

                    txtCode.Text = "";
                    btnCode.Text = "电芯扫码";
                    labCode.Text = "电芯条码：";
                    txtResult.Text = $"托盘扫码成功";
                }
            }
            catch { }
        }

        private void btnFaceDown_Click(object sender, EventArgs e)
        {
            // 插入托盘
            try
            {
                RunProcess run = MachineCtrl.GetInstance().GetModule(runID);
                if (null != run)
                {
                    if (BatteryStatus.Fake == run.Pallet[pltIdx].Battery[0, 0].Type)
                    {
                        run.Pallet[pltIdx].Battery[0, 0].Release();
                        run.Pallet[pltIdx].State = PalletStatus.WaitResult;
                        run.Pallet[pltIdx].Battery[0, 0].Type = BatteryStatus.FakeTag;
                        run.SaveRunData(SaveType.Pallet);
                        txtResult.Text = $"下假电池成功";
                    }
                    else if (BatteryStatus.Detect == run.Pallet[pltIdx].Battery[0, 0].Type)
                    {
                        run.Pallet[pltIdx].Battery[0, 0].Release();
                        run.Pallet[pltIdx].State = PalletStatus.WaitResult;
                        run.Pallet[pltIdx].Battery[0, 0].Type = BatteryStatus.FakeTag;
                        run.SaveRunData(SaveType.Pallet);
                        txtResult.Text = $"下假电池成功";
                    }
                    else if (run.Pallet[pltIdx].State == PalletStatus.WaitResult)
                    {
                        txtResult.Text = $"夹具[{run.Pallet[pltIdx].Code}]已经下假电池完成";
                    }
                    else
                    {
                        txtResult.Text = $"不存在待下假电池";
                    }
                }
            }
            catch { }
        }
    }
}
