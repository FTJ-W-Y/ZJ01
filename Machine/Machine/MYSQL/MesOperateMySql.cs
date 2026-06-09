using HelperLibrary;
using Machine.MYSQL;
using System;
using System.Collections.Generic;

namespace Machine
{
    /// <summary>
    /// MES操作数据库增删改查
    /// </summary>
    static class MesOperateMySql
    {
        /// <summary>
        /// 打开MySql
        /// </summary>
        /// <returns></returns>
        public static bool OpenMesMySql()
        {
            string section = MachineCtrl.GetInstance().MachineName;

            string MySqlDB = IniFile.ReadString(section, "MySqlDB", "BakingOutDB", Def.GetAbsPathName(Def.ModuleExCfg));
            string MySqlIP = IniFile.ReadString(section, "MySqlIP", "localhost", Def.GetAbsPathName(Def.ModuleExCfg));
            int MySqlPort = IniFile.ReadInt(section, "MySqlPort", 3306, Def.GetAbsPathName(Def.ModuleExCfg));
            string MySqlUser = IniFile.ReadString(section, "MySqlUser", "root", Def.GetAbsPathName(Def.ModuleExCfg));
            string MySqlPassword = IniFile.ReadString(section, "MySqlPassword", "123456", Def.GetAbsPathName(Def.ModuleExCfg));
            if (!MySqlBakingOut.Open(MySqlDB, MySqlIP, MySqlPort, MySqlUser, MySqlPassword))
            {
                ShowMsgBox.ShowDialog("MySqlBakingOut数据库连接失败", MessageType.MsgAlarm);
                return false;
            }
            MySqlBakingOut.CreateTable();
            if (!MySqlProcess.Open(MySqlDB, MySqlIP, MySqlPort, MySqlUser, MySqlPassword))
            {
                ShowMsgBox.ShowDialog("MySqlProcess数据库连接失败", MessageType.MsgAlarm);
                return false;
            }
            MySqlProcess.CreateTable();
            if (!MySqlBatteryBasket.Open(MySqlDB, MySqlIP, MySqlPort, MySqlUser, MySqlPassword))
            {
                ShowMsgBox.ShowDialog("MySqlBatteryBasket数据库连接失败", MessageType.MsgAlarm);
                return false;
            }
            MySqlBatteryBasket.CreateTable();
            return true;
        }

        /// <summary>
        /// 关闭MySql连接
        /// </summary>
        public static void CloseMesMySql()
        {
            MySqlBakingOut.Close();
            MySqlProcess.Close();
            MySqlBatteryBasket.Close();
        }

        /// <summary>
        /// 检查MySql的连接状态
        /// </summary>
        /// <returns></returns>
        public static bool MySqlIsOpen()
        {

            if(!MySqlBakingOut.IsOpen())
            {
                return false;
            }
            if (!MySqlProcess.IsOpen())
            {
                return false;
            }
            if (!MySqlBatteryBasket.IsOpen())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// MySql服务重连
        /// </summary>
        /// <returns></returns>
        public static bool MySqlReconnect()
        {
           
            if(!MySqlBakingOut.Reconnect())
            {
                return false;
            }
            if (!MySqlProcess.Reconnect())
            {
                return false;
            }
            if (!MySqlBatteryBasket.Reconnect())
            {
                return false;
            }


            return true;
        }


        /// <summary>
        /// 删除数据库中超期的记录
        /// </summary>
        public static void DeleteRecord(DateTime startDT, DateTime endDT)
        {
            //if (MySqlEquipmentAlarm.DeleteRecord(startDT, endDT) > 0)
            //{
            //    Def.WriteLog("DeleteRecord()", $"MySqlEquipmentAlarm 表中{endDT.ToString(Def.DateFormal)}之前的记录已被删除", LogType.Success);
            //}
            //if (MySqlEquipmentOperation.DeleteRecord(startDT, endDT) > 0)
            //{
            //    Def.WriteLog("DeleteRecord()", $"MySqlEquipmentOperation 表中{endDT.ToString(Def.DateFormal)}之前的记录已被删除", LogType.Success);
            //}
            //if (MySqlProductionRecord.DeleteRecord(startDT, endDT) > 0)
            //{
            //    Def.WriteLog("DeleteRecord()", $"MySqlProductionRecord 表中{endDT.ToString(Def.DateFormal)}之前的记录已被删除", LogType.Success);
            //}
            //if (MySqlRealData.DeleteRecord(startDT, endDT) > 0)
            //{
            //    Def.WriteLog("DeleteRecord()", $"MySqlRealData 表中{endDT.ToString(Def.DateFormal)}之前的记录已被删除", LogType.Success);
            //}
        }

        ///// <summary>
        ///// 设备状态实时表
        ///// </summary>
        ///// <param name="mc"></param>
        //public static bool EquipmentReal(MesMCState mc, ResourcesStruct rs)
        //{
        //    RunDBData data = new RunDBData();
        //    //data.equipment_id = rs.EquipmentID;
        //    //data.process_code = rs.ProcessID;
        //    //data.state_code = $"{((int)mc):00}";
        //    //switch(mc)
        //    //{
        //    //    case MesMCState.Running:
        //    //        data.state_name = "自动运行";
        //    //        break;
        //    //    case MesMCState.Waiting:
        //    //        data.state_name = "待机";
        //    //        break;
        //    //    case MesMCState.Stop:
        //    //        data.state_name = "停机";
        //    //        break;
        //    //    case MesMCState.Alarm:
        //    //        data.state_name = "报警";
        //    //        break;
        //    //    case MesMCState.Other:
        //    //        data.state_name = "其它";
        //    //        break;
        //    //    default:
        //    //        break;
        //    //}
        //    //data.update_time = DateTime.Now;

        //    return MySqlRunData.UpdateRecord(data) > -1;
        //}
        
        ///// <summary>
        ///// 设备报警记录表结束时间更新
        ///// </summary>
        ///// <param name="endDate"></param>
        //public static void EquipmentAlarmEndTime(DateTime endDate)
        //{
        //    MySqlEquipmentAlarm.UpdataEndDate(endDate);
        //}


    }
}
