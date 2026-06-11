using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BakingDataLib
{
    [Serializable]
    public struct PalletData
    {
        public double currentTemp;              //实际温度
        public double patrolTemp;               //巡检温度
    }

    [Serializable]
    public struct CavityParam
    {
        public uint cycleBlowPressure;          //呼吸充干燥气压力：Pa
        public uint cycleBlowTime;              //呼吸充干燥气时间：分钟 
        public uint cycleHoldTime;              //呼吸充干燥气保持时间：分钟
        public uint cycleInterval;              //呼吸时间间隔：分钟
        public uint cycleTimes;                 //呼吸循环次数：次
        public float maxTemp;                   //温度上限：摄氏度
        public float minTemp;                   //温度下限：摄氏度  
        public uint preheatBlowPressure;        //加热前充干燥气压力：Pa
        public uint preheatPumpTime;            //加热前抽真空时间：分钟
        public uint preheatTime;                //预热时间：分钟     
        public float setTemp;                   //设定温度：摄氏度
        public uint vacuumA;                    //A状态真空压力：Pa
        public uint vacuumB;                    //B状态真空压力：Pa      
        public uint vacuumBreakerPressure;      //开门真空压力：Pa
        public uint vacuumBreakerTime;          //开门破真空时长：分钟
        public uint vacuumHoldTime;             //真空加热时间：分钟
        public uint vacuumPumpTimeA;            //A状态抽真空时间：分钟
        public uint vacuumPumpTimeB;            //B状态抽真空时间：分钟
    }

    [Serializable]
    public class CavityData
    {
        #region CavityData
        public uint vacuum;             //真空压力
        public uint runningTime;        //工作时间
        public List<PalletData> palletData = new List<PalletData>();    //夹具温度
        #endregion

        #region CavityParam
        public CavityParam cavityParam = new CavityParam();     //腔体工艺参数
        #endregion

        #region CavityState
        public short doorState;                     //炉门状态
        public uint dwellState;                     //保压状态
        public short plateHeaterState;              //夹具加热状态
        public uint state;                          //工作状态
        public short vacuumBreakerState;            //破真空阀状态
        #endregion

        #region CavityAlarm
        public bool doorAlarm;                      //炉门报警
        public bool leftMechTempAlarm;              //左机械温控报警
        public bool leftPlateDetectAlarm;           //左夹具放平检测报警：夹具
        public bool rightMechTempAlarm;             //右机械温控报警
        public bool rightPlateDetectAlarm;          //右夹具放平检测报警：夹具
        public bool vacuumAlarm;                    //真空报警
        public bool vacuumBreakerAlarm;             //破真空报警
        public bool vacuumGauge1Alarm;              //真空计报警
        #endregion

        #region CavityAlarmData
        public double leftTempAlarm;                //左温度报警温度值：夹具-发热板   
        public double rightTempAlarm;               //右温度报警温度值：夹具-发热板  
        public double vacuumAlarmValue;             //真空报警值
        #endregion
    }

    [Serializable]
    public class OvenData
    {
        public List<CavityData> cavityData = new List<CavityData>();    //炉腔数据
        public ushort state;                        //干燥炉远程状态
    }

    [Serializable]
    public class OnloadData
    {
        #region State
        public ushort state;                                //上料运行状态
        public ushort[] platPlateState = new ushort[3];     //上料平台1~3夹具状态
        public ushort[] platPlateBatNum = new ushort[3];    //上料平台1~3夹具电芯数量
        public bool robotSafeState;                         //上料机器人安全状态
        public bool[] bufPlatState = new bool[5];           //缓存架1~5夹具有无检测信号
        #endregion

        #region Alarm
        public bool[] scannerAlarm = new bool[4];           //来料线扫码枪1~4异常报警
        public bool plateScannerAlarm;                      //夹具扫码枪异常报警
        public bool robotAlarm;                             //机器人防撞感应报警
        #endregion
    }

    [Serializable]
    public class OffloadData
    {
        #region State
        public ushort state;                                //下料运行状态
        public ushort[] platPlateState = new ushort[2];     //下料平台1~2夹具状态
        public ushort[] platPlateBatNum = new ushort[2];    //下料平台1~2夹具电芯数量
        public bool robotSafeState;                         //下料机器人安全状态
        #endregion

        #region Alarm
        public bool fingerAlarm;                            //下料机械手故障报警
        public bool fingerCommAlarm;                        //下料机械手通讯故障报警
        public bool[] platAlarm = new bool[2];              //下料平台1~2夹具感应异常报警
        #endregion
    }

    [Serializable]
    public class TransferData
    {
        #region State
        public ushort state;                //调度运行状态
        public bool robotState;             //调度机器人安全状态
        #endregion

        #region Alarm
        public bool robotAlarm;             //调度机器人异常报警
        public bool robotCommAlarm;         //调度机器人通讯异常报警
        #endregion
    }

    [Serializable]
    public class BakingData
    {
        public List<OvenData> OvenDataList = new List<OvenData>();
        public OnloadData OnloadData = new OnloadData();
        public OffloadData OffloadData = new OffloadData();
        public TransferData TransferData = new TransferData();

        public object dataLock { get; private set; }             // 数据互斥锁

        /// <summary>
        /// 初始化空实例
        /// </summary>
        public BakingData()
        {
            this.dataLock = new object();
        }

        /// <summary>
        /// 序列化本类数据至buffer
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            byte[] data;
            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    lock (this.dataLock)
                    {
#pragma warning disable SYSLIB0011
                        bf.Serialize(memory, this);
#pragma warning restore SYSLIB0011
                    }
                    memory.Seek(0, SeekOrigin.Begin);
                    memory.Flush();
                    data = new byte[memory.Length];
                    memory.Read(data, 0, Convert.ToInt32(memory.Length));
                    return data;
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("BadkingData.Serialize error : " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// 反序列化buffer数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static object Deserialize(byte[] buffer, int size)
        {
            object obj;
            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    memory.Seek(0, SeekOrigin.Begin);
                    memory.Write(buffer, 0, size);
                    memory.Flush();
                    BinaryFormatter bf = new BinaryFormatter();
                    if (memory.Capacity > 0)
                    {
                        memory.Position = 0;
#pragma warning disable SYSLIB0011
                        obj = bf.Deserialize(memory);
#pragma warning restore SYSLIB0011
                        return obj;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("BakingData.Deserialize error : " + ex.Message);
            }
            return null;
        }
    }
}
