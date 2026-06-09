using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Machine
{
    /// <summary>
    /// 烘烤电池离线产出表
    /// </summary>
    static class MySqlBakingOut
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
        public static bool Open(string db,string ip,int port,string user,string password)
           {
            string conInfo = $"database={db}; server={ip}; port={port}; user={user}; password={password};Allow User Variables=True";
            mysql = new MySqlConnection(conInfo);
            try
            {
                mysql.Open();
                dataLock = new object();
            }
            catch(Exception ex)
            {
                Def.WriteLog("Machien", $"烘烤电池离线产出表打开失败，失败原因{ex}", HelperLibrary.LogType.Error);
            }
            return (mysql.State == System.Data.ConnectionState.Open);
        }
        /// <summary>
        /// 关闭数据库
        /// </summary>
        public static void Close()
        {
                if(mysql.State == System.Data.ConnectionState.Open)
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
            if(mysql.State != System.Data.ConnectionState.Open)
            {
                try
                {
                    mysql.Open();
                }
                catch(Exception ex)
                {
                    Def.WriteLog("Machine", $"打开MySQL数据库异常，异常原因{ex}", HelperLibrary.LogType.Error);
                }
                return (mysql.State == System.Data.ConnectionState.Open);
            }
            return true;
        }
        public static void CreateTable()
        {
            if(mysql.State != System.Data.ConnectionState.Open)
            {
                return;
            }
            string sql = @"CREATE TABLE `bakingoutdb`.`bakingout`  (
                          `id` bigint(20) NOT NULL AUTO_INCREMENT,
                          `LotNo` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
                          `Status` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
                          `OpOrder` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
                          `VehicleNo` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
                          `BakingOutTime` datetime NOT NULL,
                          PRIMARY KEY (`id`) USING BTREE
                        ) ENGINE = InnoDB AUTO_INCREMENT = 3 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic";

            MySqlHelper.ExecuteNonQuery(mysql, System.Data.CommandType.Text, sql);
        }

        public static int InsertRecord(EquBakingOut data)
        {
            if(mysql.State != System.Data.ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock (dataLock)
            {
                string sql = @"INSERT INTO BakingOut(LotNo, Status, OpOrder, VehicleNo, BakingOutTime) 
                                        VALUES(@LotNo, @Status, @OpOrder, @VehicleNo, @BakingOutTime)";
                List<MySqlParameter> sqlPare = new List<MySqlParameter>();
                sqlPare.Add(new MySqlParameter("@LotNo", data.LotNo));
                sqlPare.Add(new MySqlParameter("@Status", data.Status));
                sqlPare.Add(new MySqlParameter("@OpOrder", data.OpOrder));
                sqlPare.Add(new MySqlParameter("@VehicleNo", data.VehicleNo));
                sqlPare.Add(new MySqlParameter("@BakingOutTime", data.BakingOutTime));

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

        public static int DeleteRecord(DateTime startTime, DateTime endTime)
        {
            if (mysql.State != ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock (dataLock)
            {
                string sql = @"DELETE FROM BakingOut WHERE read_flag<>0 AND end_date BETWEEN @startTime AND @endTime";

                List<MySqlParameter> sqlPara = new List<MySqlParameter>();
                sqlPara.Add(new MySqlParameter("@startTime", startTime));
                sqlPara.Add(new MySqlParameter("@endTime", endTime));

                result = MySqlHelper.ExecuteNonQuery(mysql, CommandType.Text, sql, sqlPara.ToArray());
            }
            return result;
        }


        public struct EquBakingOut
        {
            public string LotNo;             // 电芯条码
            public string Status;            // 电芯状态
            public string OpOrder;           // 工序任务号
            public string VehicleNo;         // 托盘码
            public DateTime BakingOutTime;   // 出站时间
        }
    }
}
