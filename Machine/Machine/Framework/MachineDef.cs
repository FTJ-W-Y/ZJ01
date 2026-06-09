using HelperLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using SystemControlLibrary;
using Excel = Microsoft.Office.Interop.Excel;

namespace Machine
{
    #region // 设备中所有模组的ID

    /// <summary>
    /// 设备中同类型模组总数，即用同一个类生成的模组
    /// </summary>
    public enum ModuleCount
    {
        DryingOven = 14,
        OffloadBuffer = 3,
    }

    /// <summary>
    /// 设备中所有模组的ID
    /// </summary>
    public enum RunID
    {
        Invalid = -1,      // 无效

        // 上料
        OnloadRobot = 0,
        OnloadLine,
        OnloadRecv,
        OnloadScan,
        OnloadNG,
        OnloadFake,
        OnloadDetect,

        // 人工台、暂存架
        ManualOperate,
        PalletBuffer,

        // 调度
        Transfer,

        // 下料
        OffloadBattery,
        OffloadNG,
        OffloadDetect,
        OffloadLine,
        CoolingSystem,
        CoolingOffload,
        OffloadBuffer,
        OffloadBufferEnd = OffloadBuffer + ModuleCount.OffloadBuffer - 1,
        OffloadOut,
        // 干燥炉
        DryOven0,
        DryOvenALL = DryOven0 + OvenInfoCount.OvenCount,

        // 安全门
        SafeDoor,

        RunIDEnd,

    }
    #endregion


    #region // 运行数据保存类型

    /// <summary>
    /// 运行数据保存类型
    /// </summary>
    public enum SaveType
    {
        AutoStep = 0x01 << 0,      // 步骤（自动流程步骤）
        Variables = 0x01 << 1,     // 变量（成员变量）
        SignalEvent = 0x01 << 2,   // 信号
        Battery = 0x01 << 3,       // 电池（抓手||缓存||假电池||NG||暂存）
        Pallet = 0x01 << 4,        // 治具（夹具||料框）
        Cylinder = 0x01 << 5,      // 气缸状态
        Motor = 0x01 << 6,         // 电机位置
        Robot = 0x01 << 7,         // 机器人位置
        Cavity = 0x01 << 8,        // 干燥炉腔体数据
        Avoid = 0x01 << 9,        // 干燥炉腔体数据
    };
    #endregion


    #region // 模组事件状态

    /// <summary>
    /// 事件状态
    /// </summary>
    public enum EventStatus
    {
        Invalid = 0,      // 无效状态
        Require,          // 请求状态
        Response,         // 响应状态
        Ready,            // 准备状态
        Start,            // 开始状态
        Finished,         // 完成状态
        Cancel,           // 取消状态
    };
    #endregion


    #region // 模组事件枚举（禁止改变顺序！！！）

    /// <summary>
    /// 模组事件（禁止改变顺序！！！）
    /// </summary>
    public enum EventList
    {
        Invalid = -1,                          // 信号无效
        
        OnloadRecvSendBattery = 0,
        OnloadScanSendBattery,
        OnloadLinePickBattery,
        OnloadFakePickBattery,
        OnloadNGPlaceBattery,

        OnloadPlaceEmptyPallet,           // 上料区放空夹具
        OnloadPlaceNGPallet,              // 上料区放NG非空夹具，转盘
        OnLoadPlaceDetectFakePallet,      // 上料区放待检测含假电池夹具（未取走假电池的夹具）
        OnloadPlaceReputFakePallet,       // 上料区放待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
        OnloadPickNGEmptyPallet,          // 上料区取NG空夹具
        OnloadPickOKFullPallet,           // 上料区取OK无假电池满夹具
        OnloadPickOKFakeFullPallet,       // 上料区取OK带假电池满夹具
        OnLoadPickWaitResultPallet,       // 上料区取等待水含量结果夹具（已取待测假电池的夹具）
        OnloadPickRebakeFakePallet,       // 上料区取回炉假电池夹具（已放回假电池的夹具）
        OnloadPickPlaceEnd,               // 上料区信号结束

        // RunProcessManualOperate
        ManualPlaceNGEmptyPallet,         // 人工操作台放NG空夹具
        ManualPickEmptyPallet,            // 人工操作台取OK空夹具
        ManualPickPlaceEnd,               // 人工操作台信号结束

        // RunProcessPalletBuffer
        PalletBufferPlaceEmptyPallet,           // 缓存架放空夹具
        PalletBufferPlaceNGEmptyPallet,         // 缓存架放NG空夹具
        PalletBufferPlaceOKFullPallet,          // 缓存架放上料完成OK满夹具
        PalletBufferPlaceOKFakeFullPallet,      // 缓存架放上料完成OK带假电池满夹具
        PalletBufferPickEmptyPallet,            // 缓存架取空夹具
        PalletBufferPickNGEmptyPallet,          // 缓存架取NG空夹具
        PalletBufferPickOKFullPallet,           // 缓存架取上料完成OK满夹具
        PalletBufferPickOKFakeFullPallet,       // 缓存架取上料完成OK带假电池满夹具
        PalletBufferPickPlaceEnd,               // 缓存架信号结束

        // RunProcessDryingOven
        DryOvenPlaceEmptyPallet,                // 干燥炉放空夹具
        DryOvenPlaceNGPallet,                   // 干燥炉放NG非空夹具
        DryOvenPlaceNGEmptyPallet,              // 干燥炉放NG空夹具
        DryOvenPlaceOnlOKFullPallet,            // 干燥炉放上料完成OK满夹具
        DryOvenPlaceOnlOKFakeFullPallet,        // 干燥炉放上料完成OK带假电池满夹具
        DryOvenPlaceRebakeFakePallet,           // 干燥炉放回炉假电池夹具（已放回假电池的夹具）
        DryOvenPlaceWaitResultPallet,           // 干燥炉放等待水含量结果夹具（已取待测假电池的夹具）
        DryOvenPickEmptyPallet,                 // 干燥炉取空夹具
        DryOvenPickNGPallet,                    // 干燥炉取NG非空夹具
        DryOvenPickNGEmptyPallet,               // 干燥炉取NG空夹具
        DryOvenPickDetectFakePallet,            // 干燥炉取待检测含假电池夹具（未取走假电池的夹具）
        DryOvenPickReputFakePallet,             // 干燥炉取待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
        DryOvenPickDryFinishPallet,             // 干燥炉取干燥完成夹具（等待下料）
        DryOvenPickTransferPallet,              // 干燥炉转移取夹具：取来源炉腔
        DryOvenPlaceTransferPallet,             // 干燥炉转移放夹具：放至目的炉腔
        DryOvenPickPlaceEnd,					// 干燥炉信号结束

        // RunProcessOffloadBattery
        OffLoadPlaceDryFinishPallet,           // 下料区放干燥完成夹具
        OffLoadPlaceDetectFakePallet,          // 下料区放待检测含假电池夹具（未取走假电池的夹具）
        OffLoadPlaceNGPallet,                  // 下料区放NG夹具（非空）
        OffLoadPickEmptyPallet,                // 下料区取空夹具
        OffLoadPickWaitResultPallet,           // 下料区取等待水含量结果夹具（已取待测假电池的夹具）
        OffLoadPickRebakeFakePallet,           // 干燥炉取待回炉含假电池夹具（已取走假电池，待重新放回假电池的夹具）
        OffLoadPickNGPallet,                   // 下料区取NG夹具（非空）
        OffLoadPickNGEmptyPallet,              // 下料区取NG空夹具
        OffLoadPickPlaceEnd,                   // 下料区信号结束

        // RunProcessOffloadDetectFake
        /// <summary>
        /// 下料放待检测电池
        /// </summary>
        PlaceDetectBattery,             // 下料放待检测电池    
        // RunProcessOffloadNG
        /// <summary>
        /// 下料放烘烤NG电池
        /// </summary>
        OffLoadPlaceNGBattery,                 // 下料放烘烤NG电池
        // RunProcessOffloadLine
        /// <summary>
        /// 下料放下料线电池
        /// </summary>
        OffLoadLinePlaceBattery,               // 下料放下料线电池
        // RunProcessCoolingSystem
        /// <summary>
        /// 冷却系统放电池
        /// </summary>
        CoolingSystemPlaceBattery,             // 冷却系统放电池
        /// <summary>
        /// 冷却系统取电池
        /// </summary>
        CoolingSystemPickBattery,              // 冷却系统取电池
        CoolingSystemPickPlaceEnd,

        OffloadSendBattery,                    //下料区的线体发送电池

        EventEnd,
    };
    #endregion


    #region // 模组事件结构

    /// <summary>
    /// 模组事件
    /// </summary>
    [System.Serializable]
    public struct ModuleEvent
    {
        public EventList Event;
        public EventStatus State;
        public int Pos;

        public ModuleEvent(EventList modEvent, EventStatus eventState, int eventPos)
        {
            this.Event = modEvent;
            this.State = eventState;
            this.Pos = eventPos;
        }
    };
    #endregion


    #region // 模组中电机点位

    /// <summary>
    /// 模组中电机点位
    /// </summary>
    public enum MotorPosition
    {
        Invalid = -1,

        // RunProcessOnloadRobot
        Onload_LinePickPos = 0,       // 来料取料位间距
        Onload_ScanPalletPos,         // 夹具扫码位间距
        Onload_PalletPos,             // 夹具放料位间距
        Onload_BufferPos,             // 暂存位间距
        Onload_ScanFakePos,           // 假电池扫码位间距
        Onload_FakePos,               // 假电池取料位间距
        Onload_NGPos,                 // NG输出放料位间距
        Onload_DetectPos,             // 测试假电池放料位间距
        Onload_Pos_End,               // 结束

        // RunProcessOffloadBattery
        OffLoad_SafetyPos = 0,        // 下料区安全位
        OffLoad_PickPltPos1,          // 下料区夹具1取料位
        OffLoad_PickPltPos2,          // 下料区夹具2取料位
        OffLoad_PlacePos,             // 下料区放料位
        OffLoad_BufferPos,            // 下料区暂存位：4夹爪正好对4暂存

        // RunProcessCoolingOffload
        CoolingOffload_SafetyPos = 0, // 冷却下料安全位
        CoolingOffload_PickPos,       // 冷却下料取料位
        CoolingOffload_BufferPos,     // 冷却下料暂存位：4夹爪正好对4暂存
        CoolingOffload_PlacePos,      // 冷却下料放料位

    }
    #endregion


    #region // 模组中的最大夹具数

    /// <summary>
    /// 模组中的最大夹具数
    /// </summary>
    enum ModuleMaxPallet
    {
        // 上料区夹具：1层，3列
        OnloadRobot = 2,

        // 调度机器人抓手夹具：1层，1列
        TransferRobot = 1,

        // 人工操作台夹具：1层，1列
        ManualOperate = 1,

        // 夹具缓存架夹具：2层，1列
        PalletBuffer = 4,

        // 干燥炉夹具：4层，2列
        DryingOven = OvenRowCol.MaxRow*OvenRowCol.MaxCol,

        //RunProcessBatteryOffload：1层，2列    
        OffloadBattery = 2,

    };
    #endregion


    #region // 模组报警ID范围

    /// <summary>
    /// 模组报警ID范围
    /// </summary>
    enum ModuleMsgID
    {
        // 模组其实ID在库ID后开始
        SystemStartID = LibMsgID.MsgLibIDEnd,
        SystemEndID = SystemStartID + 99,

        // RunProcessOnloadLine
        OnloadRecvMsgStartID,
        OnloadRecvMsgEndID = OnloadRecvMsgStartID + 99,

        // RunProcessOnloadLine
        OnloadLineMsgStartID,
        OnloadLineMsgEndID = OnloadLineMsgStartID + 99,

        // RunProcessOnloadScan
        OnloadScanMsgStartID,
        OnloadScanMsgEndID = OnloadScanMsgStartID + 99,

        // RunProcessOnloadFake
        OnloadFakeMsgStartID,
        OnloadFakeMsgEndID = OnloadFakeMsgStartID + 99,

        // RunProcessOnloadNG
        OnloadNGMsgStartID,
        OnloadNGMsgEndID = OnloadNGMsgStartID + 99,

        // RunProcessOnloadRobot
        OnloadRobotMsgStartID,
        OnloadRobotMsgEndID = OnloadRobotMsgStartID + 99,

        // RunProcessRobotTransfer
        RobotTransferMsgStartID,
        RobotTransferMsgEndID = RobotTransferMsgStartID + 99,

        // RunProcessManualOperate
        ManualOperateMsgStartID,
        ManualOperateMsgEndID = ManualOperateMsgStartID + 99,

        // RunProcessPalletBuffer
        PalletBufferMsgStartID,
        PalletBufferMsgEndID = PalletBufferMsgStartID + 99,

        // RunProcessDryingOven
        DryingOvenMsgStartID,
        DryingOvenMsgEndID = DryingOvenMsgStartID + 99,

        // RunProcessOffloadBattery
        OffloadBatteryMsgStartID,
        OffloadBatteryMsgEndID = OffloadBatteryMsgStartID + 99,

        // RunProcessOffloadDetectFake
        OffloadDetectFakeMsgStartID,
        OffloadDetectFakeMsgEndID = OffloadDetectFakeMsgStartID + 99,

        // RunProcessOffloadNG
        OffloadNGMsgStartID,
        OffloadNGMsgEndID = OffloadNGMsgStartID + 99,

        // RunProcessOffloadLine
        OffloadLineMsgStartID,
        OffloadLineMsgEndID = OffloadLineMsgStartID + 99,

        // RunProcessCoolingSystem
        CoolingSystemMsgStartID,
        CoolingSystemMsgEndID = CoolingSystemMsgStartID + 99,

        // RunProcessOffloadBufferLine
        OffloadBufferMsgStartID,
        OffloadBufferMsgEndID = OffloadBufferMsgStartID + 99,

        // RunProcessOffloadOut
        OffloadOutMsgStartID,
        OffloadOutMsgEndID = OffloadOutMsgStartID + 99,

    }
    #endregion


    #region // 设备系统IO：按钮-灯塔-安全门

    /// <summary>
    /// 设备系统IO：按钮-灯塔-安全门
    /// </summary>
    enum SystemIO
    {
        ButtonIO = 2,       // 按钮IO数量
        TowerIO = 2,        // 灯塔IO数量
        SafeDoorIO = 2,     // 安全门数量
    }
    #endregion


    #region // 操作模式：手动/自动

    /// <summary>
    /// 操作模式：手动/自动
    /// </summary>
    public enum OptMode
    {
        Auto,
        Manual,
    }

    #endregion

    #region //编解码模式
    /// <summary>
    /// 编解码模式
    /// </summary>
    public enum CodecMode
    {
        // 16位字节顺序
        bit16_12 = 0,
        bit16_21,

        // 32位字节顺序
        bit32_1234,
        bit32_2143,
        bit32_3412,
        bit32_4321,
    };

    #endregion 编解码模式


    #region // 系统宏定义类

    /// <summary>
    /// 系统宏定义类
    /// </summary>
    public static class Def
    {
        #region // 系统字段

        /// <summary>
        /// Dump文件夹
        /// </summary>
        public const string DumpFolder = SysDef.DumpFolder;
        /// <summary>
        /// 系统Log文件夹
        /// </summary>
        public const string SystemLogFolder = SysDef.SystemLogFolder;
        /// <summary>
        /// 设备Log文件夹
        /// </summary>
        public const string MachineLogFolder = SysDef.MachineLogFolder;
        /// <summary>
        /// 电机配置文件夹
        /// </summary>
        public const string MotorCfgFolder = SysDef.MotorCfgFolder;
        /// <summary>
        /// 硬件配置文件
        /// </summary>
        public const string HardwareCfg = SysDef.HardwareCfg;
        /// <summary>
        /// 输入配置文件
        /// </summary>
        public const string InputCfg = SysDef.InputCfg;
        /// <summary>
        /// 输出配置文件
        /// </summary>
        public const string OutputCfg = SysDef.OutputCfg;
        /// <summary>
        /// 模组文件
        /// </summary>
        public const string ModuleCfg = SysDef.ModuleCfg;
        /// <summary>
        /// 模组配置文件
        /// </summary>
        public const string ModuleExCfg = SysDef.ModuleExCfg;
        /// <summary>
        /// 以ID报警的配置文件
        /// </summary>
        public const string MessageCfg = SysDef.MessageCfg;
        /// <summary>
        /// 设备参数文件
        /// </summary>
        public const string MachineCfg = SysDef.MachineCfg;
        /// <summary>
        /// 设备本地数据库文件
        /// </summary>
        public const string MachineMdb = SysDef.MachineMdb;
        /// <summary>
        /// 运行数据文件夹
        /// </summary>
        public const string RunDataFolder = "Data\\RunData\\";
        /// <summary>
        /// 运行数据备份文件夹
        /// </summary>
        public const string RunDataBakFolder = "Data\\RunDataBak\\";
        /// <summary>
        /// MES参数文件
        /// </summary>
        public const string MesParameterCfg = "System\\MesParameter.cfg";

        /// <summary>
        /// 系统时间样式
        /// </summary>
        public const string DateFormal = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// 设备Log文件
        /// </summary>
        private static LogFile mcLogFile;

        /// <summary>
        /// 随机数
        /// </summary>
        private static Random random;

        #endregion

        #region // 系统方法

        /// <summary>
        /// 获取设备显示语言：CHS中文，ENG英文
        /// </summary>
        public static string GetLanguage()
        {
            return HelperDef.GetLanguage();
        }

        /// <summary>
        /// 获取设备当前运行方式：TRUE无硬件设备模拟运行，FALSE有硬件运行
        /// </summary>
        public static bool IsNoHardware()
        {
            return HelperDef.IsNoHardware();
        }

        /// <summary>
        /// 当前设备产品配方
        /// </summary>
        public static int GetProductFormula()
        {
            return HelperDef.GetProductFormula();
        }

        /// <summary>
        /// 获取当前相对路径的绝对路径
        /// </summary>
        /// <param name="relPath">相对路径</param>
        /// <returns></returns>
        public static string GetAbsPathName(string relPath)
        {
            return HelperDef.GetAbsPathName(relPath);
        }

        /// <summary>
        /// 创建当前绝对路径
        /// </summary>
        /// <param name="absPath">绝对路径</param>
        /// <returns></returns>
        public static bool CreateFilePath(string absPath)
        {
            // 剔除掉文件名
            return HelperDef.CreateFilePath(absPath.Remove(absPath.LastIndexOf('\\')));
        }

        /// <summary>
        /// 删除文件夹strDir中nDays天以前的文件
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="days"></param>
        public static void DeleteOldFiles(string dir, int days)
        {
            HelperDef.DeleteOldFiles(dir, days);
        }

        /// <summary>
        /// 获取随机数
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns></returns>
        public static int GetRandom(int min, int max)
        {
            if (null == random)
            {
                random = new Random();
            }
            return random.Next(min, max);
        }

        /// <summary>
        /// 生成全局不重复GUID
        /// </summary>
        /// <returns></returns>
        public static string GetGUID()
        {
            return SysDef.GetGUID();
        }

        /// <summary>
        /// CRC校验
        /// </summary>
        /// <param name="data">校验数据</param>
        /// <returns>高低8位</returns>
        public static int CRCCalc(byte[] data, int len)
        {
            //计算并填写CRC校验码
            int crc = 0xffff;
            for(int n = 0; n < len; n++)
            {
                byte i;
                crc = crc ^ data[n];
                for(i = 0; i < 8; i++)
                {
                    int TT;
                    TT = crc & 1;
                    crc = crc >> 1;
                    crc = crc & 0x7fff;
                    if(TT == 1)
                    {
                        crc = crc ^ 0xa001;
                    }
                    crc = crc & 0xffff;
                }

            }
            return crc;
        }

        /// <summary>
        /// CRC16Calc 返回int
        /// </summary>
        /// <param name="data">校验数据</param>
        /// <returns>高低位</returns>
        public static short CRC16Calc(byte[] data, int len)
        {
            int check;
            int crc_reg = 0xFFFF;
            for (int i = 0; i < len; i++)
            {
                crc_reg = crc_reg ^ data[i];
                for (int j = 0; j < 8; j++)
                {
                    check = crc_reg & 0x0001;
                    crc_reg >>= 1;
                    if (check == 0x0001)
                    {
                        crc_reg ^= 0xA001;
                    }
                }
            }
            return (short)crc_reg;
        }

        /// <summary>
        /// CRC16Calc 返回byte[]
        /// </summary>
        /// <param name="data">要进行计算的数组</param>
        /// <returns>计算后的数组</returns>
        public static byte[] CRC16Calc2(byte[] data, int len)
        {
            int check;
            int crc_reg = 0xFFFF;
            for (int i = 0; i < len; i++)
            {
                crc_reg = crc_reg ^ data[i];
                for (int j = 0; j < 8; j++)
                {
                    check = crc_reg & 0x0001;
                    crc_reg >>= 1;
                    if (check == 0x0001)
                    {
                        crc_reg ^= 0xA001;
                    }
                }
            }
            return BitConverter.GetBytes(crc_reg);
        }

        /// <summary>
        /// 导出Excel文件
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool ExportExcel(DataTable dt, string fileName)
        {
            try
            {
                if(dt == null)
                {
                    Trace.WriteLine("Machine.Def.ExportExcel() 数据库为空");
                    return false;
                }

                bool fileSaved = false;
                Excel.Application xlApp = new Excel.Application();
                if(xlApp == null)
                {
                    Trace.WriteLine("Machine.Def.ExportExcel() 无法创建Excel对象，可能您的设备未安装Excel.");
                    return false;
                }
                Excel.Workbooks workbooks = xlApp.Workbooks;
                Excel.Workbook workbook = workbooks.Add(Excel.XlWBATemplate.xlWBATWorksheet);
                Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Worksheets[1];//取得sheet1
                //写入字段
                for(int i = 0; i < dt.Columns.Count; i++)
                {
                    worksheet.Cells[1, i + 1] = dt.Columns[i].ColumnName;
                }
                //写入数值
                for(int r = 0; r < dt.Rows.Count; r++)
                {
                    for(int i = 0; i < dt.Columns.Count; i++)
                    {
                        worksheet.Cells[r + 2, i + 1] = dt.Rows[r][i];
                    }
                    System.Windows.Forms.Application.DoEvents();
                }
                string msg = string.Empty;
                worksheet.Columns.EntireColumn.AutoFit();//列宽自适应。
                if(!string.IsNullOrEmpty(fileName))
                {
                    workbook.Saved = true;
                    workbook.SaveCopyAs(fileName);
                    fileSaved = true;
                }
                xlApp.Quit();
                GC.Collect();//强行销毁
                if(fileSaved && File.Exists(fileName))
                {
                    return true;
                }
            }
            catch(System.Exception ex)
            {
                WriteLog(string.Format("Machine.Def.ExportExcel() 导出文件{0}时出错！", fileName), ex.Message, LogType.Error);
            }
            return false;
        }

        /// <summary>
        /// 导出CSV文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="title"></param>
        /// <param name="fileText"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static bool ExportCsvFile(string fileName, string title, string fileText, Encoding encode = null)
        {
            try
            {
                if(!CreateFilePath(fileName))
                    return false;

                bool writeTitle = false;
                if(!File.Exists(fileName))
                {
                    writeTitle = true;
                }
                else
                {
                    File.SetAttributes(fileName, FileAttributes.Normal);
                }
                using (StreamWriter sw = new StreamWriter(fileName, true, (null == encode ? Encoding.Default : encode)))
                {
                    if (writeTitle)
                    {
                        sw.WriteLine(title);
                    }
                    sw.Write(fileText);

                    sw.Flush();
                    File.SetAttributes(fileName, FileAttributes.ReadOnly);
                }
                return true;
            }
            catch (System.Exception ex)
            {
                WriteLog(string.Format("Machine.Def.ExportCsvFile() 导出文件{0}时出错！", fileName), ex.Message, LogType.Error);
            }
            return false;
        }

        /// <summary>
        /// 导出CSV文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="mapValue"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static bool ExportCsvFile(string fileName, Dictionary<string, string> mapValue,  Encoding encode = null)
        {
            if (null == mapValue)
            {
                return false;
            }

            try
            {
                string title="",  fileText = "";
                foreach (var item in mapValue)
                {
                    if (!string.IsNullOrEmpty(title))
                        title += ",";

                    if (!string.IsNullOrEmpty(fileText))
                        fileText += ",";

                    title += item.Key;
                    fileText += item.Value;
                }

                fileText += "\r\n";

                if (!CreateFilePath(fileName))
                    return false;

                bool writeTitle = false;
                if (!File.Exists(fileName))
                {
                    writeTitle = true;
                }

                using (StreamWriter sw = new StreamWriter(fileName, true, (null == encode ? Encoding.Default : encode)))
                {
                    if (writeTitle)
                    {
                        sw.WriteLine(title);
                    }
                    sw.Write(fileText);

                    sw.Flush();
                }
                return true;
            }
            catch (System.Exception ex)
            {
                WriteLog(string.Format("Machine.Def.ExportCsvFile() 导出文件{0}时出错！", fileName), ex.Message, LogType.Error);
            }
            return false;
        }

        /// <summary>
        /// 读取CSV文件
        /// </summary>
        /// <param name="filePathName"></param>
        /// <returns></returns>
        public static List<string[]> ReadCSV(string filePathName)
        {
            List<string[]> list = new List<string[]>();
            using (StreamReader streamReader = new StreamReader(filePathName, Encoding.Default))
            {
                string str = "";
                while (str != null)
                {
                    str = streamReader.ReadLine();
                    if (str != null && str.Length > 0)
                        list.Add(str.Split(','));
                }
                streamReader.Close();
                return list;
            }
        }
        //删除指定行CSV文件
        public static void DeleteCsvLine(string path, string temp, Encoding encode = null)
        {
            try
            {
                if (!CreateFilePath(path))
                    return;

                bool writeTitle = false;
                if (!File.Exists(path))
                {
                    return;
                }
                else
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }
                StreamReader reader = new StreamReader(path, (null == encode ? Encoding.Default : encode));
                List<String[]> ls = new List<String[]>();
                string strLine = "";
                int headIdx = 0;
                bool res = false;
                while (strLine != null)
                {
                    headIdx++;
                       strLine = reader.ReadLine();
                    if (headIdx==1)
                    {
                        ls.Add(strLine.Split(','));
                        continue;
                    }
                    if (strLine != null && strLine.Length > 0)
                    {
                        if (strLine.Split(',')[1] == temp)
                            res = true;
                        if (strLine.Split(',')[1] != temp)//temp 要删除行里面的内容
                            ls.Add(strLine.Split(','));
                    }
                }
                reader.Close();
                if (!res)
                {
                    return;
                }
                StreamWriter fileWriter = new StreamWriter(path, false, (null == encode ? Encoding.Default : encode));
                foreach (String[] strArr in ls)
                {
                    fileWriter.WriteLine(String.Join(",", strArr));
                }

                File.SetAttributes(path, FileAttributes.ReadOnly);
                fileWriter.Flush();
                fileWriter.Close();
            }
            catch (System.Exception ex)
            {
                WriteLog(string.Format("Machine.Def.ExportCsvFile() 删除文件行{0}时出错！", path), ex.Message, LogType.Error);
            }
            return;
            

        }

        //删除指定行CSV文件
        public static void DeleteCsvAllLine(string path, string temp, Encoding encode = null)
        {
            try
            {
                //if (!CreateFilePath(path))
                //    return;

                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                }
            }
            catch (System.Exception ex)
            {
                WriteLog(string.Format("Machine.Def.DeleteCsvAllLine() 删除文件时出错！"), ex.Message, LogType.Error);
            }
            return;


        }

        public static void GetCSV()
        {

            //实例化一个datatable用来存储数据
            DataTable dt = new DataTable();

            //文件流读取
            System.IO.FileStream fs = new System.IO.FileStream(@"{0}\干燥过程数据\{1}-{2}层\{3}-{4}.csv", System.IO.FileMode.Open);
            System.IO.StreamReader sr = new System.IO.StreamReader(fs, Encoding.Default);

            string tempText = "";
            bool isFirst = true;
            while ((tempText = sr.ReadLine()) != null)
            {
                string[] arr = tempText.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

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
                        dr[i] = i < arr.Length ? arr[i] : "";
                    }
                    dt.Rows.Add(dr);
                }
            }
            //展示到页面
            //  dataGridView1.DataSource = dt;
            //关闭流
            sr.Close(); fs.Close();

        }

        /// <summary>
        /// 写文本文件：适用于MES文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileText"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static bool WriteText(string fileName, string fileText, Encoding encode = null)
        {
            try
            {
                if(!CreateFilePath(fileName))
                    return false;

                using(StreamWriter sw = new StreamWriter(fileName, true, (null == encode ? Encoding.Default : encode)))
                {
                    sw.WriteLine(fileText);

                    sw.Flush();
                }
                return true;
            }
            catch(System.Exception ex)
            {
                WriteLog(string.Format("Machine.Def.WriteText({0})时出错！", fileName), ex.Message, LogType.Error);
            }
            return false;
        }

        #endregion
        
        #region // Log文件

        /// <summary>
        /// 设置Log信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="size">文件大小</param>
        /// <param name="storageLife">文件存储周期</param>
        public static void SetFileInfo(string filePath, long size, int storageLife)
        {
            if (null == mcLogFile)
            {
                mcLogFile = new LogFile();
            }
            mcLogFile.SetFileInfo(filePath, size, storageLife);
        }

        /// <summary>
        /// 输出Log
        /// </summary>
        /// <param name="msglocation">Log的定位信息：一般为，类.方法</param>
        /// <param name="log">Log</param>
        /// <param name="type">Log类型</param>
        public static void WriteLog(string msglocation, string log, LogType type = LogType.Information)
        {
            try
            {
                if(null == mcLogFile)
                {
                    mcLogFile = new LogFile();
                }
                Trace.WriteLine(string.Format("{0}:{1}", msglocation, log));
                mcLogFile.WriteLog(DateTime.Now, msglocation, log, type);
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("Def.WriteLog() error" + ex.Message);
            }
        }

        #endregion

        #region // 编码解码
        
        /// <summary>
        /// 字节序调整
        /// </summary>
        public static void ByteCodec(byte[] data, int startIdx, int count, CodecMode codec)
        {
            if (null == data || count <= 0)
            {
                return;
            }
            switch (codec)
            {
                case CodecMode.bit16_12:
                    break;
                case CodecMode.bit16_21:
                    {
                        byte buf;
                        for (int idx = 0; idx < count; idx += 2)
                        {
                            buf = data[startIdx + idx];
                            data[startIdx + idx] = data[startIdx + idx + 1];
                            data[startIdx + idx + 1] = buf;
                        }
                        break;
                    }
                case CodecMode.bit32_1234:
                    break;
                case CodecMode.bit32_2143:
                    {
                        byte[] buf = new byte[4];
                        for (int idx = 0; idx < count; idx += 4)
                        {
                            Array.Copy(data, (startIdx + idx), buf, 0, 4);
                            data[startIdx + idx] = buf[1];
                            data[startIdx + idx + 1] = buf[0];
                            data[startIdx + idx + 2] = buf[3];
                            data[startIdx + idx + 3] = buf[2];
                        }
                        break;
                    }
                case CodecMode.bit32_3412:
                    {
                        byte[] buf = new byte[4];
                        for (int idx = 0; idx < count; idx += 4)
                        {
                            Array.Copy(data, (startIdx + idx), buf, 0, 4);
                            data[startIdx + idx] = buf[2];
                            data[startIdx + idx + 1] = buf[3];
                            data[startIdx + idx + 2] = buf[0];
                            data[startIdx + idx + 3] = buf[1];
                        }
                        break;
                    }
                case CodecMode.bit32_4321:
                    {
                        byte[] buf = new byte[4];
                        for (int idx = 0; idx < count; idx++)
                        {
                            Array.Copy(data, (startIdx + idx), buf, 0, 4);
                            data[startIdx + idx] = buf[3];
                            data[startIdx + idx + 1] = buf[2];
                            data[startIdx + idx + 2] = buf[1];
                            data[startIdx + idx + 3] = buf[0];
                        }
                        break;
                    }
            }
        }

        #endregion

        // 获取时间戳
        public static int GetTimeStemp(DateTime dt)
        {
            DateTime dtSart = new DateTime(1970,1,1,8,0,0);
            int timeStamp = Convert.ToInt32((dt - dtSart).TotalSeconds);
            return timeStamp;
        }
    }
    #endregion


    #region // 生产统计数据

    /// <summary>
    /// 生产统计数据
    /// </summary>
    public static class TotalData
    {
        public static short OnloadCount;                // 上料总数
        public static short OffloadCount;               // 下料总数
        
        public static short OnScanNGCount;              // 上料扫码NG总数
        public static short OnScan1NGCount;             // 上料扫码枪1-NG总数
        public static short OnScan2NGCount;             // 上料扫码枪2-NG总数
        public static short OnScan3NGCount;             // 上料扫码枪3-NG总数
        public static short OnScan4NGCount;             // 上料扫码枪4-NG总数
        
        public static short BakedNGCount;               // BakedNG总数
        public static DateTime countTime;

        public static short CurCountHour;               // 当前统计小时
        public static short OnloadHourCnt;              // 小时上料统计
        public static short OnloadHourNgCnt;            // 小时NG上料统计
        public static short OffloadHourCnt;             // 小时下料统计
        
        /// <summary>
        /// 读统计数据
        /// </summary>
        public static void ReadTotalData()
        {
            string file, section;
            file = Def.GetAbsPathName(Def.RunDataFolder + "TotalData.cfg");
            section = "TotalData";

            OnloadCount = (short)IniFile.ReadInt(section, nameof(OnloadCount), 0, file);
            OffloadCount = (short)IniFile.ReadInt(section, nameof(OffloadCount), 0, file);
            
            OnScanNGCount = (short)IniFile.ReadInt(section, nameof(OnScanNGCount), 0, file);
            OnScan1NGCount = (short)IniFile.ReadInt(section, nameof(OnScan1NGCount), 0, file);
            OnScan2NGCount = (short)IniFile.ReadInt(section, nameof(OnScan2NGCount), 0, file);
            OnScan3NGCount = (short)IniFile.ReadInt(section, nameof(OnScan3NGCount), 0, file);
            OnScan4NGCount = (short)IniFile.ReadInt(section, nameof(OnScan4NGCount), 0, file);
            BakedNGCount = (short)IniFile.ReadInt(section, nameof(BakedNGCount), 0, file);
            
            CurCountHour = (short)IniFile.ReadInt(section, nameof(CurCountHour), 0, file);
            OnloadHourCnt = (short)IniFile.ReadInt(section, nameof(OnloadHourCnt), 0, file);
            OnloadHourNgCnt = (short)IniFile.ReadInt(section, nameof(OnloadHourNgCnt), 0, file);
            OffloadHourCnt = (short)IniFile.ReadInt(section, nameof(OffloadHourCnt), 0, file);
            
        }

        /// <summary>
        /// 保存统计数据
        /// </summary>
        public static void WriteTotalData()
        {
            string file, section;
            file = Def.GetAbsPathName(Def.RunDataFolder + "TotalData.cfg");
            section = "TotalData";

            IniFile.WriteInt(section, nameof(OnloadCount), OnloadCount, file);
            IniFile.WriteInt(section, nameof(OnScanNGCount), OnScanNGCount, file);
            IniFile.WriteInt(section, nameof(OnScan1NGCount), OnScan1NGCount, file);
            IniFile.WriteInt(section, nameof(OnScan2NGCount), OnScan2NGCount, file);
            IniFile.WriteInt(section, nameof(OnScan3NGCount), OnScan3NGCount, file);
            IniFile.WriteInt(section, nameof(OnScan4NGCount), OnScan4NGCount, file);
            IniFile.WriteInt(section, nameof(OffloadCount), OffloadCount, file);
            IniFile.WriteInt(section, nameof(BakedNGCount), BakedNGCount, file);

            IniFile.WriteInt(section, nameof(CurCountHour), CurCountHour, file);
            IniFile.WriteInt(section, nameof(OnloadHourCnt), OnloadHourCnt, file);
            IniFile.WriteInt(section, nameof(OnloadHourNgCnt), OnloadHourNgCnt, file);
            IniFile.WriteInt(section, nameof(OffloadHourCnt), OffloadHourCnt, file);
        }

        /// <summary>
        /// 清空统计数据
        /// </summary>
        public static void ClearTotalData()
        {
            ShiftStruct shift = OperationShifts.Shift();
            string file, title, text;
            int hour = DateTime.Now.Hour;
            file = string.Format(@"{0}\生产计数\{1}\{1}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"));
            title = $"日期,时间,操作员,班次,上料计数,上料扫码NG,下料计数,烘烤NG,24小时,分时上料数,分时下料数";
            text = string.Format("{0},{1},{2}[{3}],{4},{5},{6},{7},{8}\r\n"
                , DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss")
                , MachineCtrl.GetInstance().OperaterID, shift.Name, shift.Code
                , OnloadCount, OnScanNGCount, OffloadCount, BakedNGCount, hour);
            Def.ExportCsvFile(file, title, text);

            OnloadCount = 0;
            OnScanNGCount = 0;
            OffloadCount = 0;
            BakedNGCount = 0;
            OnScan1NGCount = 0;
            OnScan2NGCount = 0;
            OnScan3NGCount = 0;
            OnScan4NGCount = 0;
        }

        public static void UpdateShiftTotalData()
        {
            if (countTime.Hour == DateTime.Now.Hour)
                return;

            countTime = DateTime.Now;
            ShiftStruct shift = OperationShifts.Shift();
            string file, title, text;
            int hour = DateTime.Now.Hour;
            file = string.Format(@"{0}\生产计数\{1}\{1}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, DateTime.Now.ToString("yyyy-MM-dd"));
            title = $"日期,时间,操作员,班次,上料计数,上料扫码NG,下料计数,烘烤NG,24小时,分时上料数,分时下料数";
            text = string.Format("{0},{1},{2}[{3}],{4},{5},{6},{7},{8}\r\n"
                , DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss")
                , MachineCtrl.GetInstance().OperaterID, shift.Name, shift.Code
                , OnloadCount, OnScanNGCount, OffloadCount, BakedNGCount, hour);
            Def.ExportCsvFile(file, title, text);
        }
    }
    #endregion

    
}


