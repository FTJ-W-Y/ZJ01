using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Machine.MYSQL
{
    public static class MySqlBatteryBasket
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
                Def.WriteLog("Machien", $"电芯流拉筐表打开失败，失败原因{ex}", HelperLibrary.LogType.Error);
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
            string sql = @"CREATE TABLE `bakingoutdb`.`BatteryBasket`  (
                          `Id` bigint NOT NULL AUTO_INCREMENT COMMENT '主键，自增',
                          `LotNo` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '电芯条码',
                          `SlotNo` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '位置号',
                          `Status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL COMMENT '批次状态',
                          `ErrorCode` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '不良代码',
                          `Grade` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '档位',
                          `Exclude` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '排出工序',
                          `Fake` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '假电池',
                          `CreateTime` datetime NOT NULL ON UPDATE CURRENT_TIMESTAMP COMMENT '添加时间',
                          `OpOrder` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '工序任务',
                          `OpName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '工序名',
                          `PreOpOrder` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '前工序任务',
                          `PreOpName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '前工序名',
                          `NGCount` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '当站NG次数',
                          `AllowReInput` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL COMMENT '是否可复投',
                           PRIMARY KEY (`Id`) USING BTREE
                           )ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic";
            MySqlHelper.ExecuteNonQuery(mysql, System.Data.CommandType.Text, sql);
        }

        public static int InsertRecord(BasketData data)
        {
            if (mysql.State != System.Data.ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock (dataLock)
            {
                string sql = @"INSERT INTO BatteryBasket(LotNo, SlotNo, Status, ErrorCode, Grade, Exclude, Fake, CreateTime, OpOrder,
                                                   OpName, PreOpOrder, PreOpName, NGCount, AllowReInput) 
                                        VALUES(@LotNo, @SlotNo, @Status, @ErrorCode, @Grade, @Exclude, @Fake, @CreateTime, @OpOrder, 
                                              @OpName, @PreOpOrder, @PreOpName, @NGCount, @AllowReInput)";
                List<MySqlParameter> sqlPare = new List<MySqlParameter>();
                sqlPare.Add(new MySqlParameter("@LotNo", data.LotNo));
                sqlPare.Add(new MySqlParameter("@SlotNo", data.SlotNo));
                sqlPare.Add(new MySqlParameter("@Status", data.Status));
                sqlPare.Add(new MySqlParameter("@ErrorCode", data.ErrorCode));
                sqlPare.Add(new MySqlParameter("@Grade", data.Grade));
                sqlPare.Add(new MySqlParameter("@Exclude", data.Exclude));
                sqlPare.Add(new MySqlParameter("@Fake", data.Fake));
                sqlPare.Add(new MySqlParameter("@CreateTime",DateTime.Now));
                sqlPare.Add(new MySqlParameter("@OpOrder", data.OpOrder));
                sqlPare.Add(new MySqlParameter("@OpName", data.OpName));
                sqlPare.Add(new MySqlParameter("@PreOpOrder", data.PreOpOrder));
                sqlPare.Add(new MySqlParameter("@PreOpName", data.PreOpName));
                sqlPare.Add(new MySqlParameter("@NGCount", data.NGCount));
                sqlPare.Add(new MySqlParameter("@AllowReInput", data.AllowReInput));


                result = MySqlHelper.ExecuteNonQuery(mysql, System.Data.CommandType.Text, sql, sqlPare.ToArray());
            }
            return result;
        }
        public static bool LnQuire(BasketData data, ref ListBasketData listbasketdata )
        {
            if(mysql.State != System.Data.ConnectionState.Open)
            {
                return false;
            }
            //int result = -1;
            try
            {
                lock (dataLock)
                {
                    string sql = @"Select * From batterybasket Where LotNo =@LotNo";
                    List<MySqlParameter> mysqlPara = new List<MySqlParameter>();
                    mysqlPara.Add(new MySqlParameter("@LotNo", data.LotNo));

                    MySqlDataReader dr;
                    dr = MySqlHelper.ExecuteReader(mysql, System.Data.CommandType.Text, sql, mysqlPara.ToArray());

                    listbasketdata.basketDatas = new List<BasketData>();
                    BasketData baskData = new BasketData();
                    while (dr.Read())
                    {
                        baskData.LotNo = dr["LotNo"].ToString();                  //电芯条码
                        baskData.SlotNo = dr["SlotNo"].ToString();                //位置号
                        baskData.Status = dr["Status"].ToString();                //批次状态
                        baskData.ErrorCode = dr["ErrorCode"].ToString();          //不良代码
                        baskData.Grade = dr["Grade"].ToString();                  //档位
                        baskData.Exclude = dr["Exclude"].ToString();              //排出工序
                        baskData.Fake = dr["Fake"].ToString();                    //假电芯
                        baskData.OpOrder = dr["OpOrder"].ToString();              //工序任务
                        baskData.OpName = dr["OpName"].ToString();                //工序名
                        baskData.PreOpOrder = dr["PreOpOrder"].ToString();        //前工序任务
                        baskData.PreOpName = dr["PreOpName"].ToString();          //前工序名
                        baskData.NGCount = dr["NGCount"].ToString();              //当前NG次数
                        baskData.AllowReInput = dr["AllowReInput"].ToString();    //是否可复投

                        listbasketdata.basketDatas.Add(baskData);
                    }
                    if(baskData.Status != "OK")
                    {
                        dr.Close();
                        return false;
                    }
                    dr.Close();
                }
                return true;
            }
            catch(Exception ex)
            {
                MessageBox.Show($"查询异常{ex}");
            }
            return false;

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

        public struct ListBasketData
        {
            public List<BasketData> basketDatas;
        }

        public struct BasketData
        {
            public string LotNo;               // 电芯条码
            public string SlotNo;            // 位置号
            public string Status;           // 批次状态
            public string ErrorCode;         // 不良代码
            public string Grade;         // 档位
            public string Exclude;            // 排出工序
            public string Fake;           // 假电池
            public string OpOrder;         // 工序任务
            public string OpName;   // 工序名
            public string PreOpOrder;            // 前工序任务
            public string PreOpName;           // 前工序名
            public string NGCount;         // 当站NG次数
            public string AllowReInput;   // 是否可复投
        }
    }
}
