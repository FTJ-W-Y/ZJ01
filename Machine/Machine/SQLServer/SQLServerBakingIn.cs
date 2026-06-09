using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace Machine.SQLServer
{

    /// <summary>
    /// 烘烤电池离线产入表
    /// </summary>
    static class SQLServerBakingIn
    {
        static SqlConnection SqlServer;
        static object dataLock;

        public struct BakingInData
        {
            public List<EquBakingIn> bakingIns;    // 入站表参数
        }
        

        public static bool Open()
        {
            string connStr = "Data Source=127.0.0.1;Initial Catalog=Baking;Persist Security Info=True;User ID=sa;PassWord = 123456";
            //string connStr = "Data Source=192.168.1.210,1433;Initial Catalog=Baking;Persist Security Info=True;User ID=sa;PassWord = 123456";
            SqlServer = new SqlConnection(connStr);
            try
            {
                SqlServer.Open();
                dataLock = new object();
            }            
            catch (Exception ex)
            {
                Def.WriteLog("Machine", $"烘烤电池离线产入表打开失败，失败原因{ex}", HelperLibrary.LogType.Error);
            }

            return (SqlServer.State == System.Data.ConnectionState.Open);

        }

        /// <summary>
        /// 关闭数据库
        /// </summary>
        public static void Close()
        {
            if (SqlServer.State == System.Data.ConnectionState.Open)
            {
                SqlServer.Close();
            }
        }
        public static bool IsOpen()
        {
            return (SqlServer.State == System.Data.ConnectionState.Open);
        }
        public static bool Reconnect()
        {
            if (SqlServer.State != System.Data.ConnectionState.Open)
            {
                try
                {
                    SqlServer.Open();
                }
                catch (Exception ex)
                {
                    Def.WriteLog("Machine", $"打开SQLServer数据库异常，异常原因{ex}", HelperLibrary.LogType.Error);
                }
                return (SqlServer.State == System.Data.ConnectionState.Open);
            }
            return true;
        }

        
        /// <summary>
        /// 数据库查询
        /// </summary>
        /// <param name="data"></param>
        /// <param name="bakingIn"></param>
        /// <returns></returns>
        public static bool SelectRecord(EquBakingIn data,ref BakingInData bakingInData)
        {
            if (SqlServer.State != System.Data.ConnectionState.Open)
            {
                return false;
            }
            lock (dataLock)
            {
                string sql = @"Select * From BakingIn Where VehicleNo =@VehicleNo";

                List<SqlParameter> sqlPare = new List<SqlParameter>();
                sqlPare.Add(new SqlParameter("@VehicleNo", data.VehicleNo));

                SqlDataReader dr;
                dr =  SqlServerHelper.ExecuteReader(SqlServer, System.Data.CommandType.Text, sql, sqlPare.ToArray());

                bakingInData.bakingIns = new List<EquBakingIn>();
                EquBakingIn equBakingIn = new EquBakingIn();
                while (dr.Read())
                {
                    equBakingIn.LotNo = dr["LotNo"].ToString();
                    equBakingIn.Status = dr["Status"].ToString();
                    equBakingIn.OpOrder = dr["OpOrder"].ToString();
                    equBakingIn.BakingInTime = Convert.ToDateTime( dr["BakingInTime"]);

                    bakingInData.bakingIns.Add(equBakingIn);
                }
                dr.Close();
                return true;
            }
            
        }

        public static int SelectBakingInDB(EquBakingIn data)
        {
            if (SqlServer.State != System.Data.ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock (dataLock)
            {
                string sql = @"Select * From gd Where VehicleNo =@VehicleNo And LotNo =@LotNo";

                List<SqlParameter> sqlPare = new List<SqlParameter>();
                sqlPare.Add(new SqlParameter("@LotNo", data.LotNo));
                sqlPare.Add(new SqlParameter("@VehicleNo", data.VehicleNo));

                result = SqlServerHelper.ExecuteNonQuery(SqlServer, System.Data.CommandType.Text, sql, sqlPare.ToArray());
            }
            return result;
        }

        /// <summary>
        /// 数据库插入
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int InsertRecord(EquBakingIn data)
        {
            //string connStr = "DataSource =.;Initial Catalog = biaoming;User ID = sa;PassWord = 123456";
            //SqlConnection conn = new SqlConnection(connStr);
            if (SqlServer.State != System.Data.ConnectionState.Open)
            {
                return -1;
            }
            int result = -1;
            lock (dataLock)
            {
                string sql = @"INSERT INTO BakingIn(LotNo, Status, OpOrder, VehicleNo, BakingInTime) 
                                        VALUES(@LotNo, @Status, @OpOrder, @VehicleNo, @BakingInTime)";

                List<SqlParameter> sqlPare = new List<SqlParameter>();
                sqlPare.Add(new SqlParameter("@LotNo", data.LotNo));
                sqlPare.Add(new SqlParameter("@Status", data.Status));
                sqlPare.Add(new SqlParameter("@OpOrder", data.OpOrder));
                sqlPare.Add(new SqlParameter("@VehicleNo", data.VehicleNo));
                sqlPare.Add(new SqlParameter("@BakingInTime", data.BakingInTime));

                result = SqlServerHelper.ExecuteNonQuery(SqlServer, System.Data.CommandType.Text, sql, sqlPare.ToArray());
            }
            return result;
        }
        
        public struct EquBakingIn
        {
            public string LotNo;             // 电芯条码
            public string Status;            // 电芯状态
            public string OpOrder;           // 工序任务号
            public string VehicleNo;         // 托盘码
            public DateTime BakingInTime;    // 进站时间
        }

    }
}
