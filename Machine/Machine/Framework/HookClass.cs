using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Machine.Framework
{
    class HookClass
    {
        [DllImport("user32.dll")]
        public static extern int SetWindowsHookEx(int idHook, HookProc hProc, IntPtr hMod, int dwThreadId);
        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(int hHook, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(int hHook);
        [DllImport("kernel32.dll")]//获取模块句柄  
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        public struct KeyInfoStruct
        {
            public int vkCode;        //按键键码
            public int scanCode;
            public int flags;       //键盘是否按下的标志
            public int time;
            public int dwExtraInfo;
        }

        private const int WH_KEYBOARD_LL = 13;      //钩子类型 全局钩子
        private const int WM_KEYUP = 0x101;     //按键抬起
        private const int WM_KEYDOWN = 0x100;       //按键按下

        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        public bool bStopMsg = false;
        int hHook = 0;
        GCHandle gc;

        private long lastdt = 0;
        StringBuilder strUerPwd = new StringBuilder();
        StringBuilder strAdmin = new StringBuilder();
        private System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        public delegate void ElapsedEventHandler(string strLogingID);
        public event ElapsedEventHandler SetLogingUI;

        public int MethodHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0)
                {
                    KeyInfoStruct inputInfo = (KeyInfoStruct)Marshal.PtrToStructure(lParam, typeof(KeyInfoStruct));

                    if (wParam == (IntPtr)WM_KEYUP)
                    {//如果按键按下

                        string charCode = ((char)inputInfo.vkCode).ToString();
                        string keysCode = ((Keys)inputInfo.vkCode).ToString();

                        strAdmin.Append(charCode);
                        if (keysCode.ToString().Equals("LControlKey"))
                        {
                            strAdmin.Clear();
                        }
                        if (strAdmin.ToString().Equals("1111"))
                        {
                            SetLogingUI(strAdmin.ToString().Trim());
                            strAdmin.Clear();
                        }
                        if (strAdmin.Length>1000)
                        {
                            strAdmin.Clear();
                        }
                        if (lastdt != 0)
                        {
                           
                            if ((sw.ElapsedMilliseconds - lastdt) > 300)
                            {
                                strUerPwd.Clear();
                                sw.Stop();
                                lastdt = 0;
                                strUerPwd.Append(charCode);
                                
                            }
                            else
                            {
                                if (keysCode.ToString().Equals("Return") && !string.IsNullOrEmpty(strUerPwd.ToString().Trim()))
                                {
                                    SetLogingUI(strUerPwd.ToString().Trim());
                                    strUerPwd.Clear();
                                    sw.Stop();
                                    lastdt = 0;
                                }
                                else
                                {
                                    lastdt = sw.ElapsedMilliseconds;
                                    strUerPwd.Append(charCode);
                                }
                            }
                            //strUerPwd.Append(charCode);
                        }
                        else
                        {
                            sw.Start();
                            lastdt = sw.ElapsedMilliseconds;
                            strUerPwd.Append(charCode);
                        }
                    }
                    if (bStopMsg)
                        return 1;
                }
            }
            catch (Exception)
            {

            }
            return CallNextHookEx(hHook, nCode, wParam, lParam);
        }
        public void SetHook()
        {
            if (0 == hHook)
            {
                HookProc KeyCallBack = new HookProc(MethodHookProc);
                string str = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
                hHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyCallBack,
                    GetModuleHandle(System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName), 0);
                if (hHook == 0)
                {
                    HelperLibrary.ShowMsgBox.ShowDialog("设置Hook失败", HelperLibrary.MessageType.MsgWarning);
                }
                else
                {
                    gc = GCHandle.Alloc(KeyCallBack);
                }
            }
        }
        public void ClearHook()
        {
            if (0 != hHook)
            {
                if (UnhookWindowsHookEx(hHook))
                {
                    hHook = 0;
                    gc.Free();
                }
                else
                {
                    HelperLibrary.ShowMsgBox.ShowDialog("Hook卸载失败", HelperLibrary.MessageType.MsgWarning);
                }
            }
        }
    }
}
