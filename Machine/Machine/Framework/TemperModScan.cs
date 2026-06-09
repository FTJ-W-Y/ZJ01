using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework
{
    class TemperModScan : BaseThread
    {
        #region // 字段

        //private bool isSocket;
        //private ComPort comPort;
        private TcpSocket client;
        private bool recvFinish;
        private short recvTemper;  //不取小数点
        private string adderInfo;
        private int temperScanNo;
        private bool ScanEnabled;

        #endregion


        #region // 方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public TemperModScan()
        {
            //this.isSocket = false;
            //this.comPort = new ComPort();
            this.client = new TcpSocket();
            this.recvTemper = 0;
            this.ScanEnabled = false;
            this.adderInfo = string.Empty;
        }

        public void SetTemperScanNo(int id)
        {
            this.temperScanNo = id;
        }

        /// <summary>
        /// 扫码器的连接状态
        /// </summary>
        /// <returns></returns>
        public bool IsConnect()
        {
            //if (isSocket)
            //{
            return this.client.IsConnect();
            //}
            //else
            //{
            //    return this.comPort.IsOpen();
            //}
        }

        /// <summary>
        /// 扫码器以网口通讯连接
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool ConnectSocket(string ip, int port)
        {
            if (IsConnect())
            {
                return true;
            }
            if (this.client.Connect(ip, port))
            {
                //isSocket = true;
                adderInfo = string.Format("{0}:{1}", ip, port);
                return InitThread("BarcodeScan Socket " + adderInfo);
            }
            return false;
        }

        /// <summary>
        /// 扫码器以串口通讯连接
        /// </summary>
        /// <param name="com">串口号</param>
        /// <param name="port">串口波特率</param>
        /// <param name="linefeed">换行符</param>
        /// <returns></returns>
        //public bool ConnectCom(int com, int port, string linefeed = "\r\n")
        //{
        //    if (IsConnect())
        //    {
        //        return true;
        //    }
        //    if (comPort.Open(com, port))
        //    {
        //        isSocket = false;
        //        adderInfo = string.Format("COM{0}:{1}", com, port);
        //        this.comPort.SetLinefeed(linefeed);
        //        return InitThread("BarcodeScan Com " + adderInfo);
        //    }
        //    return false;
        //}

        /// <summary>
        /// 断开通讯
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            //this.adderInfo = string.Empty;
            //if (isSocket)
            //{
            this.client.Disconnect();
            return ReleaseThread();
            //}
            //else
            //{
            //    this.comPort.Close();
            //    return ReleaseThread();
            //}
        }

        /// <summary>
        /// 地址信息
        /// </summary>
        /// <returns></returns>
        public string AdderInfo()
        {
            return this.adderInfo;
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool Send(byte[] data, int len)
        {
            this.recvFinish = false;
            this.recvTemper = 0;
            //if (isSocket)
            //{
            return this.client.Send(data, len);
            //}
            //else
            //{
            //    return this.comPort.Write(sendText);
            //}
        }

        /// <summary>
        /// 接收
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool Recv(ref short recvValue, int timeout)
        {
            DateTime time = DateTime.Now;
            while ((DateTime.Now - time).TotalMilliseconds < timeout)
            {
                if (this.recvFinish)
                {
                    recvValue = this.recvTemper;
                    return true;
                }
                Sleep(1);
            }
            return false;
        }

        protected override void RunWhile()
        {
            if (!IsConnect())
            {
                return;
            }
            
            byte[] buf = new byte[1024];
            int len = client.Recv(ref buf);
            if (len > 0)
            {
                if (9 == len)
                {
                    //编号+命令+长度+数据+CRC16
                    int idx = 0;
                    int cmd = 0;
                    int cmdLen = 0;
                    short temper = 0;
                    int check = 0;
                    short crc16 = Def.CRC16Calc(buf, 7);
                    MemoryStream ms = new MemoryStream(buf);
                    using (BinaryReader reader = new BinaryReader(ms))
                    {
                        idx = reader.ReadByte();
                        cmd = reader.ReadByte();
                        cmdLen = reader.ReadByte();
                        byte[] id4 = reader.ReadBytes(4);
                        Def.ByteCodec(id4, 0, 1, CodecMode.bit32_4321);
                        temper = (short)BitConverter.ToInt32(id4, 0);
                        byte[] id5 = reader.ReadBytes(2);
                        check = BitConverter.ToInt16(id5, 0);
                    }
                    //读取命令
                    if (cmd == 0x03 && crc16 == check)
                    {
                        this.recvTemper = temper;
                        this.recvFinish = true;
                    }
                }
                string tmp = "";
                for (int i = 0; i < len; i++)
                {
                    tmp += string.Format("{0:X2} ", buf[i]);
                }
                WriteLog(string.Format("Client Recv: <- {0}【{1}】", tmp, tmp.Length));
            }
        }

        #endregion
    }
}
