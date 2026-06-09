using HelperLibrary;
using System;
using SystemControlLibrary;

namespace Machine
{
    /// <summary>
    /// 气缸操作类
    /// </summary>
    public static class CylinderAction
    {
        /// <summary>
        /// 输出点操作
        /// </summary>
        /// <param name="output">输出点</param>
        /// <param name="isOn">true打开，false关闭</param>
        /// <returns></returns>
        public static bool OutputAction(int output, bool isOn)
        {
            if(output < 0 || Def.IsNoHardware())
            {
                return true;
            }
            if(isOn ? DeviceManager.Outputs(output).IsOn() : DeviceManager.Outputs(output).IsOff())
            {
                return true;
            }
            return (isOn ? DeviceManager.Outputs(output).On() : DeviceManager.Outputs(output).Off());
        }

        /// <summary>
        /// 等待输入点状态
        /// </summary>
        /// <param name="run">当前模组，记录报警信息</param>
        /// <param name="input">输入点</param>
        /// <param name="isOn">输入点等待的状态</param>
        /// <returns></returns>
        private static bool WaitInputState(RunProcess run, int input, bool isOn)
        {
            System.Diagnostics.Trace.Assert(input > -1, "CylinderAction::WaitInputState");

            if(Def.IsNoHardware())
            {
                return true;
            }

            DateTime startTime = DateTime.Now;
            Input inputIO = DeviceManager.Inputs(input);
            uint timeOut = inputIO.Timeout;
            bool waitOK = false;

            while(true)
            {
                if(isOn ? inputIO.IsOn() : inputIO.IsOff())
                {
                    waitOK = true;
                    break;
                }

                if((DateTime.Now - startTime).TotalMilliseconds > timeOut)
                {
                    // 报错
                    string[] strMag = new string[] { $"{inputIO.Num} {inputIO.Name}" };
                    run.ModuleShowMessageID(isOn ? (int)LibMsgID.MsgWaitInputOnTimeout : (int)LibMsgID.MsgWaitInputOffTimeout, strMag);
                    break;
                }
                System.Threading.Thread.Sleep(1);
            }
            return waitOK;
        }

        /// <summary>
        /// 气缸推出操作
        /// </summary>
        /// <param name="run">当前模组，记录报警信息</param>
        /// <param name="cylinderIdx">用位状态表示操作的气缸索引</param>
        /// <param name="push">true推出push，false回退pull</param>
        /// <param name="inPush">推出push的输入感应</param>
        /// <param name="inPull">回退pull的输入感应</param>
        /// <param name="outPush">推出push的输出点</param>
        /// <param name="outPull">回退pull的输出点</param>
        /// <returns></returns>
        public static bool CylinderPush(RunProcess run, int cylinderIdx, bool push, int[] inPush, int[] inPull, int[] outPush, int[] outPull)
        {
            if(Def.IsNoHardware())
            {
                return true;
            }
            #region // 检查IO点
            if(inPush.Length != inPull.Length)
            {
                ShowMsgBox.ShowDialog("inPush 和 inPull 长度不一致", MessageType.MsgAlarm);
                return false;
            }
            if (outPush.Length != outPull.Length)
            {
                ShowMsgBox.ShowDialog("outPush 和 outPull 长度不一致", MessageType.MsgAlarm);
                return false;
            }
            if (inPush.Length != outPush.Length)
            {
                ShowMsgBox.ShowDialog("inPush 和 outPush 长度不一致", MessageType.MsgAlarm);
                return false;
            }
            for(int i = 0; i < inPush.Length; i++)
            {
                if((cylinderIdx & (0x01 << i)) == (0x01 << i))
                {
                    if ((inPush[i] < 0) || (inPull[i] < 0) || ((outPush[i] < 0) && (outPull[i] < 0)))
                    {
                        return false;
                    }
                }
            }
            #endregion

            #region // 操作
            for(int i = 0; i < inPush.Length; i++)
            {
                if((cylinderIdx & (0x01 << i)) == (0x01 << i))
                {
                    OutputAction(outPush[i], push);
                    OutputAction(outPull[i], !push);
                }
            }
            #endregion

            #region // 检查到位

            return true;


            for (int i = 0; i < inPush.Length; i++)
            {
                if((cylinderIdx & (0x01 << i)) == (0x01 << i))
                {
                    if (!WaitInputState(run, inPush[i], push) || !WaitInputState(run, inPull[i], !push))
                    {
                        return false;
                    }
                }
            }
            #endregion

            return false;
        }

    }
}
