using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.MYSQL
{
    static class MySqlProcess
    {
        static MySqlConnection mysql;
        static object dataLock;
        /// <summary>
        /// 打开数据库
        /// </summary>
        /// <param name="db"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool Open(string db, string ip, int port, string user, string password)
        {
            string conInfo = $"database={db}; server={ip}; port={port}; user={user}; password={password};Allow User Variables=True";
            mysql = new MySqlConnection(conInfo);
            try
            {
                mysql.Open();
                dataLock = new object();
            }
            catch (Exception ex)
            {
                Def.WriteLog("Machien", $"电芯生产工艺表打开失败，失败原因{ex}", HelperLibrary.LogType.Error);
            }
            return (mysql.State == System.Data.ConnectionState.Open);
        }
        /// <summary>
        /// 关闭数据库
        /// </summary>
        public static void Close()
        {
            if (mysql.State == System.Data.ConnectionState.Open)
            {
                mysql.Close();
            }
        }
        public static bool IsOpen()
        {
            return (mysql.State == System.Data.ConnectionState.Open);
        }
        public static bool Reconnect()
        {
            if (mysql.State != System.Data.ConnectionState.Open)
            {
                try
                {
                    mysql.Open();
                }
                catch (Exception ex)
                {
                    Def.WriteLog("Machine", $"打开MySQL数据库异常，异常原因{ex}", HelperLibrary.LogType.Error);
                }
                return (mysql.State == System.Data.ConnectionState.Open);
            }
            return true;
        }
        public static void CreateTable()
        {
            if (mysql.State != System.Data.ConnectionState.Open)
            {
                return;
            }
            string sql = @"CREATE TABLE `bakingoutdb`.`Process`  (
                         `Id` int NOT NULL AUTO_INCREMENT COMMENT '主键自增',
                         `LotNo` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '电芯条码',
                         `OnloadTime` datetime NULL DEFAULT NULL COMMENT '上料时间',
                         `OffloadTime` datetime NULL DEFAULT NULL COMMENT '下料时间',
                         `StartTime` datetime NULL DEFAULT NULL COMMENT '开始烘烤时间',
                         `EndTime` datetime NULL DEFAULT NULL COMMENT '结束烘烤时间',
                         `PreheatTime` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '预热时长',
                         `VacHeatTime` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '真空烘烤时长',
                         `CoolingTime` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT '30' COMMENT '冷却时长',
                         `SetTempValue` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '温度设定值',
                         `TempUpperlimit` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '温度设定上限',
                         `TempLowerlimit` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '温度设定下限',
                         `PreVacTime` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '抽真空时间',
                         `BlowTime` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '破真空时间',
                         `CoolBatteryTemp` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '冷却后电芯温度',
                         `CoolWindTemp` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '冷却风温度',
                         `CoolWindSpeed` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '冷却风速',
                         `CoolLineSpeed` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '冷却滚筒线速度',
                         `ReBatteryData` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '复投电池信息',
                         `TworeworkData` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '加烘返工信息',
                          PRIMARY KEY (`Id`) USING BTREE
                          ) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic";

            MySqlHelper.ExecuteNonQuery(mysql, System.Data.CommandType.Text, sql);
        }

        public static int InsertRecord(ProcessData data)
        {
            if (mysql.State != System.Data.ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock (dataLock)
            {
                string sql = @"INSERT INTO Process(LotNo, OnloadTime, OffloadTime, StartTime, EndTime, PreheatTime, VacHeatTime, CoolingTime,
                                                   SetTempValue, TempUpperlimit, TempLowerlimit, PreVacTime, BlowTime) 
                                        VALUES(@LotNo, @OnloadTime, @OffloadTime, @StartTime, @EndTime, @PreheatTime, @VacHeatTime, @CoolingTime, 
                                              @SetTempValue, @TempUpperlimit, @TempLowerlimit, @PreVacTime, @BlowTime)";
                List<MySqlParameter> sqlPare = new List<MySqlParameter>();
                sqlPare.Add(new MySqlParameter("@LotNo", data.LotNo));
                sqlPare.Add(new MySqlParameter("@OnloadTime", data.OnloadTime));
                sqlPare.Add(new MySqlParameter("@OffloadTime", data.OffloadTime));
                sqlPare.Add(new MySqlParameter("@StartTime", data.StartTime));
                sqlPare.Add(new MySqlParameter("@EndTime", data.EndTime));
                sqlPare.Add(new MySqlParameter("@PreheatTime", data.PreheatTime));
                sqlPare.Add(new MySqlParameter("@VacHeatTime", data.VacHeatTime));
                sqlPare.Add(new MySqlParameter("@CoolingTime", data.CoolingTime));
                sqlPare.Add(new MySqlParameter("@SetTempValue", data.SetTempValue));
                sqlPare.Add(new MySqlParameter("@TempUpperlimit", data.TempUpperlimit));
                sqlPare.Add(new MySqlParameter("@TempLowerlimit", data.TempLowerlimit));
                sqlPare.Add(new MySqlParameter("@PreVacTime", data.PreVacTime));
                sqlPare.Add(new MySqlParameter("@BlowTime", data.BlowTime));
                //sqlPare.Add(new MySqlParameter("@CoolBatteryTemp", data.CoolBatteryTemp));
                //sqlPare.Add(new MySqlParameter("@CoolWindTemp", data.CoolWindTemp));
                //sqlPare.Add(new MySqlParameter("@CoolWindSpeed", data.CoolWindSpeed));
                //sqlPare.Add(new MySqlParameter("@CoolLineSpeed", data.CoolLineSpeed));
                //sqlPare.Add(new MySqlParameter("@ReBatteryData", data.ReBatteryData));
                //sqlPare.Add(new MySqlParameter("@TworeworkData", data.TworeworkData));

                result = MySqlHelper.ExecuteNonQuery(mysql, System.Data.CommandType.Text, sql, sqlPare.ToArray());
            }
            return result;
        }
        //public static int UpdataEndDate(DateTime endDate)
        //{
        //    if (mysql.State != ConnectionState.Open)
        //    {
        //        return -1;
        //    }
        //    int result = -1;
        //    lock (dataLock)
        //    {
        //        string sql = @"UPDATE equipment_alarm_record SET end_date = @end_date WHERE id > 0 AND end_date is null";

        //        List<MySqlParameter> sqlPara = new List<MySqlParameter>();
        //        sqlPara.Add(new MySqlParameter("@end_date", endDate));

        //        result = MySqlHelper.ExecuteNonQuery(mysql, CommandType.Text, sql, sqlPara.ToArray());
        //    }

        //    return result;
        //}


        //public static int DeleteRecord(DateTime startTime, DateTime endTime)
        //{
        //    if (mysql.State != ConnectionState.Open)
        //    {
        //        return -1;
        //    }
        //    int result = -1;
        //    lock (dataLock)
        //    {
        //        string sql = @"DELETE FROM BakingOut WHERE read_flag<>0 AND end_date BETWEEN @startTime AND @endTime";

        //        List<MySqlParameter> sqlPara = new List<MySqlParameter>();
        //        sqlPara.Add(new MySqlParameter("@startTime", startTime));
        //        sqlPara.Add(new MySqlParameter("@endTime", endTime));

        //        result = MySqlHelper.ExecuteNonQuery(mysql, CommandType.Text, sql, sqlPara.ToArray());
        //    }
        //    return result;
        //}


        public struct ProcessData
        {
            public string LotNo;               // 电芯条码
            public DateTime OnloadTime;        // 上料时间
            public DateTime OffloadTime;        // 下料时间
            public DateTime StartTime;        // 开始烘烤时间
            public DateTime EndTime;        // 结束烘烤时间
            public string PreheatTime;            // 预热时间
            public string VacHeatTime;           // 真空烘烤时间
            public string CoolingTime;         // 冷却时间
            public string SetTempValue;         // 温度设定值
            public string TempUpperlimit;            // 温度设定上限
            public string TempLowerlimit;           // 温度设定下限
            public string PreVacTime;         // 抽真空时间
            public string BlowTime;   // 破真空时间
            public string CoolBatteryTemp;            // 冷却后电芯温度
            public string CoolWindTemp;           // 冷却风温度
            public string CoolWindSpeed;         // 冷却风速度
            public string CoolLineSpeed;   // 冷却滚筒线速度
            public string ReBatteryData;           // 复投电池信息
            public string TworeworkData;         // 加烘返工信息
        }
    }
}
