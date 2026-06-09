using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Apriso.MIPlugins.Communication.Clients;
using Apriso.MIPlugins.Communication.Clients.WcfServiceAPI;

namespace Machine
{
    /// <summary>
    /// MES操作接口类
    /// </summary>
    public static class MesOperate
    {
        private static string fileName = "MESlog";

        private static HttpClient httpClient = new HttpClient();      // IOT通讯的http对象
        /// <summary>
        /// EquToMesOperatorLogin,          // 操作员登录
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool EquToMesOperatorLogin(ResourcesStruct rs, ref string errMsg)
        {
            DateTime startTime = DateTime.Now;

            string mesUri = "";
            try
            {
                //var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesOperatorLogin);
                //if (!cfg.enable)
                //{
                //    return true;
                //}
                var header = new EquToMesOperatorLoginAPI.LoginSoapHeader();
                //header.ProcessCode = rs.ProcessCode;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesOperatorLoginAPI.OperatorLogin();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.OperatorUserID = rs.OperatorUserID;
                body.OperatorPassword = rs.OperatorPassword;
                body.LocalTime = DateTime.Now;

                var equToMes = new EquToMesOperatorLoginAPI.EquToMesOperatorLogin();
                equToMes.LoginSoapHeaderValue = header;
                //if (!string.IsNullOrEmpty(cfg.mesUri))
                //{
                //    equToMes.Url = cfg.mesUri;
                //}
                startTime = DateTime.Now;
                var result = equToMes.OperatorLogin(body);

                // 调用接口用时
                TimeSpan ts = DateTime.Now - startTime;
                string second = $"{ts.TotalSeconds}S";

                //调用接口返回结果
                string retsValue = "";
                if (null != result)
                {
                    var outparm = new
                    {
                        Code = result.Code.ToString(),
                        Msg = result.Msg
                    };

                    retsValue = JsonConvert.SerializeObject(outparm);
                    retsValue = RevertJsonString(retsValue);
                }

                // 调用接口信息
                var meslogin = new
                {
                    //ProcessCode = rs.ProcessCode,
                    //EquipmentPassWord = rs.EquPassword,
                    EquipmentCode = rs.EquipmentCode,
                    ResourceCode = rs.ResourceCode,
                    OperatorUserID = rs.OperatorUserID,
                    OperatorPassword = rs.OperatorPassword,
                };
                string loginret = JsonConvert.SerializeObject(meslogin);
                loginret = RevertJsonString(loginret);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{ result.Code.ToString()},{retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{loginret}";

                SaveLogData("MES操作员登录", text);

                if (null != result)
                {
                    if (1 != result.Code)
                    {
                        errMsg = $"操作员登录 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesOperatorLogin 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                errMsg = $"Code:-2, Msg:EquToMesOperatorLogin {ex.Message}";
            }
            return false;
        }

        /// <summary>
        /// 保存通用日志
        /// </summary>
        /// <param name="text"></param>
        public static void SaveLogData(string strName, string text)
        {
            string file, title;
            file = string.Format(@"{0}\{1}\{2}\{3}\{2}{4}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, fileName, strName, DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("yyyy-MM-dd HH-00"));
            title = "条码(SFC),开始调用时间,请求接口,耗时(ms),返回代码,返回信息,工序,设备,调用账号,上传内容";

            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }

        /// <summary>
        /// 电芯进站
        /// </summary>
        /// <param name="strName"></param>
        /// <param name="text"></param>
        public static void SaveOvenData(string strName, string text)
        {
            string file, title;
            file = string.Format(@"{0}\{1}\{2}\{3}\{2}{4}.csv"
                    , MachineCtrl.GetInstance().ProductionFilePath, fileName, strName, DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("yyyy-MM-dd HH-00"));
            title = "进站时间,炉子名称,工位行,工位列,夹具编号,电芯编码,真假电池";

            Def.ExportCsvFile(file, title, (text + "\r\n"));
        }

        /// <summary>
        /// 转换字符串   added by nico 2021.12.21
        /// 
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string RevertJsonString(string json)
        {
            string retValue = "";
            string[] Aarray = json.Split('\"');
            foreach (var item in Aarray)
            {
                retValue = retValue + "\"" + item + "\"";
            }
            return retValue;
        }
        /// <summary>
        /// 转换字符串   added by nico 2021.12.21
        /// 
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string RevertStringJson(string str)
        {
            int res = 0;
            string retValue = "";
            string[] Aarray = str.Split('\"');
            foreach (var item in Aarray)
            {
                if (!string.IsNullOrEmpty(item)|| res==2)
                {
                    res = 0;
                    retValue = retValue + item + "\"";
                }
                res++;
            }
            retValue=retValue.Remove(retValue.Length-1,1);
            return retValue;
        }
        /// <summary>
        /// EquToMesHeartbeat,              // 设备在线检测
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        //public static bool EquToMesHeartbeat(ResourcesStruct rs, ref string errMsg)
        //{
        //    string Code = "";
        //    string mesUri = "";
        //    string SendValue = "";
        //    string RecvValue = "";
        //    DateTime dateTime = DateTime.Now;
        //    try
        //    {
        //        DateTime startTime = DateTime.Now;
        //        DateTime endTime = DateTime.Now;
        //        var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesHeartbeat);
        //        if (!cfg.enable)
        //        {
        //            return true;
        //        }
        //        var header = new EquToMesHeartbeatAPI.LoginSoapHeader();
        //        //header.ProcessCode = rs.ProcessCode;
        //        //header.EquPassword = rs.EquPassword;
        //        //header.OperatorUserID = rs.OperatorUserID;
        //        //header.OperatorPassword = rs.OperatorPassword;

        //        var body = new EquToMesHeartbeatAPI.Heartbeat();
        //        body.EquipmentCode = rs.EquipmentCode;
        //        body.ResourceCode = rs.ResourceCode;
        //        body.LocalTime = DateTime.Now;
        //        body.IsOnline = true;

        //        var equToMes = new EquToMesHeartbeatAPI.EquToMesHeartbeat();
        //        equToMes.LoginSoapHeaderValue = header;
        //        if (!string.IsNullOrEmpty(cfg.mesUri))
        //        {
        //            equToMes.Url = cfg.mesUri;
        //        }
        //        mesUri = cfg.mesUri;
        //        var result = equToMes.Heartbeat(body);

        //        // 调用接口返回结果
        //        string retsValue = "";
        //        if (null != result)
        //        {
        //            Code = result.Code.ToString();
        //            RecvValue = result.Msg;
        //            var outparm = new
        //            {
        //                Code = result.Code.ToString(),
        //                Msg = result.Msg
        //            };
        //            retsValue = JsonConvert.SerializeObject(outparm);
        //            RecvValue = RevertJsonString(retsValue);

        //            if (1 != result.Code)
        //            {
        //                errMsg = $"设备在线检测 Code:{result.Code}, Msg:{result.Msg}";
        //                return false;
        //            }
        //            return true;
        //        }
        //        errMsg = $"Code:-1, Msg:EquToMesHeartbeat 调用失败，无法访问";
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Code = "-2";
        //        RecvValue = ex.Message;
        //        errMsg = $"Code:-2, Msg:EquToMesHeartbeat {ex.Message}";
        //    }
        //    finally
        //    {
        //        int second = (int)(DateTime.Now - dateTime).TotalSeconds;
        //        var mesHeartbeat = new
        //        {
        //            //ProcessCode = rs.ProcessCode,
        //            //EquipmentPassWord = rs.EquPassword,
        //            EquipmentCode = rs.EquipmentCode,
        //            ResourceCode = rs.ResourceCode,
        //            OperatorUserID = rs.OperatorUserID,
        //            OperatorPassword = rs.OperatorPassword,
        //        };
        //        SendValue = RevertJsonString(JsonConvert.SerializeObject(mesHeartbeat));

        //        string sfcode = " ";
        //        string linecode = " ";
        //        string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{RecvValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{SendValue}";

        //        SaveLogData("MES设备在线检测", text);
        //    }
        //    return false;
        //}

        /// <summary>
        /// EquToMesState,                  // 设备实时状态
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="state"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool EquToMesState(ResourcesStruct rs, MesMCState state, ref string errMsg)
        {
            string mesUri = "";
            string Code = "";
            string Msg = "";
            DateTime startTime = DateTime.Now;
            try
            {
                //var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesState);
                //if (!cfg.enable)
                //{
                //    return true;
                //}
                var header = new EquToMesStateAPI.LoginSoapHeader();
                //header.ProcessCode = rs.ProcessCode;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesStateAPI.State();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;
                body.StateCode = $"{(int)state}";

                var equToMes = new EquToMesStateAPI.EquToMesState();
                equToMes.LoginSoapHeaderValue = header;
                //if (!string.IsNullOrEmpty(cfg.mesUri))
                //{
                //    equToMes.Url = cfg.mesUri;
                //}
                //mesUri = cfg.mesUri;
                var result = equToMes.State(body);
                if (null != result)
                {
                    Code = result.Code.ToString();
                    Msg = result.Msg;
                    if (1 != result.Code)
                    {
                        errMsg = $"设备实时状态 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesState 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                Msg = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesState {ex.Message}";
            }
            finally
            {
                var outparm = new
                {
                    Code = Code,
                    Msg = Msg
                };

                string retsValue = JsonConvert.SerializeObject(outparm);
                retsValue = RevertJsonString(retsValue);

                var mesState = new
                {
                    //ProcessCode = rs.ProcessCode,
                    //EquipmentPassWord = rs.EquPassword,

                    EquipmentCode = rs.EquipmentCode,
                    ResourceCode = rs.ResourceCode,
                    OperatorUserID = rs.OperatorUserID,
                    OperatorPassword = rs.OperatorPassword,
                    StateCode = $"{(int)state}"
                };

                string loginret = JsonConvert.SerializeObject(mesState);
                loginret = RevertJsonString(loginret);

                int second = (int)(DateTime.Now - startTime).TotalSeconds;

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{loginret}";

                SaveLogData("MES设备实时状态", text);
            }
            return false;
        }

        /// <summary>
        /// EquToMesAlarm,                  // 设备报警采集
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="state"></param>
        /// <param name="almCode"></param>
        /// <param name="almMsg"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        //public static bool EquToMesAlarm(ResourcesStruct rs, MesAlarmStatus state, string almCode, string almMsg, string almLevel, ref string errMsg)
        //{
        //    try
        //    {
        //        DateTime startTime = DateTime.Now;
        //        DateTime endTime = DateTime.Now;
        //        var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesAlarm);
        //        if (!cfg.enable)
        //        {
        //            return true;
        //        }
        //        var header = new EquToMesAlarmAPI.LoginSoapHeader();
        //        //header.ProcessCode = rs.ProcessCode;
        //        //header.EquPassword = rs.EquPassword;
        //        header.OperatorUserID = rs.OperatorUserID;
        //        header.OperatorPassword = rs.OperatorPassword;

        //        var body = new EquToMesAlarmAPI.Alarm();
        //        body.EquipmentCode = rs.EquipmentCode;
        //        body.ResourceCode = rs.ResourceCode;
        //        body.LocalTime = DateTime.Now;
        //        body.Status = (int)state;
        //        body.AlarmCode = $"{almCode}";
        //        body.AlarmMsg = almMsg;

        //        var equToMes = new EquToMesAlarmAPI.EquToMesAlarm();

        //        endTime = DateTime.Now;

        //        equToMes.LoginSoapHeaderValue = header;
        //        if (!string.IsNullOrEmpty(cfg.mesUri))
        //        {
        //            equToMes.Url = cfg.mesUri;
        //        }
        //        var result = equToMes.Alarm(new EquToMesAlarmAPI.Alarm[] { body });

        //        TimeSpan ts = endTime - startTime;
        //        string second = (ts.TotalMilliseconds / 1000).ToString() + "S";

        //        var outparm = new
        //        {
        //            Code = result.Code.ToString(),
        //            Msg = result.Msg
        //        };


        //        string retsValue = JsonConvert.SerializeObject(outparm);
        //        retsValue = RevertJsonString(retsValue);
        //        var mesState = new
        //        {
        //            //ProcessCode = rs.ProcessCode,
        //            //EquipmentPassWord = rs.EquPassword,

        //            EquipmentCode = rs.EquipmentCode,
        //            ResourceCode = rs.ResourceCode,
        //            OperatorUserID = rs.OperatorUserID,
        //            OperatorPassword = rs.OperatorPassword,
        //            Status = (int)state,
        //            AlarmCode = $"{almCode}",
        //            AlarmMsg = almMsg,
        //        };

        //        string loginret = JsonConvert.SerializeObject(mesState);
        //        loginret = RevertJsonString(loginret);

        //        string sfcode = " ";
        //        string linecode = " ";
        //        string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{cfg.mesUri},{second},{ result.Code.ToString()},{retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{loginret}";



        //        SaveLogData("MES设备报警采集", text);

        //        if (null != result)
        //        {
        //            if (1 != result.Code)
        //            {
        //                errMsg = $"设备报警采集 Code:{result.Code}, Msg:{result.Msg}";
        //                return false;
        //            }
        //            return true;
        //        }
        //        errMsg = $"Code:-1, Msg:EquToMesAlarm 调用失败，无法访问";
        //    }
        //    catch (System.Exception ex)
        //    {
        //        errMsg = $"Code:-2, Msg:EquToMesAlarm {ex.Message}";
        //    }
        //    return false;
        //}

        /// <summary>
        /// EquToMesDownReason,             // 设备停机采集
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="equDown"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool EquToMesDownReason(ResourcesStruct rs, EquDownReason equDown, ref string errMsg)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                DateTime endTime = DateTime.Now;
                //var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesDownReason);
                //if (!cfg.enable)
                //{
                //    return true;
                //}
                var header = new EquToMesDownReasonAPI.LoginSoapHeader();
                //header.ProcessCode = rs.ProcessCode;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesDownReasonAPI.DownReason();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;
                body.DownReasonCode = $"{(int)equDown}";
                body.BeginTime = DateTime.Now.AddMinutes(-5);
                body.EndTime = DateTime.Now;

                var equToMes = new EquToMesDownReasonAPI.EquToMesDownReason();

                equToMes.LoginSoapHeaderValue = header;
                //if (!string.IsNullOrEmpty(cfg.mesUri))
                //{
                //    equToMes.Url = cfg.mesUri;
                //}

                List<EquToMesDownReasonAPI.DownReason> lstRs = new List<EquToMesDownReasonAPI.DownReason>();
                lstRs.Add(body);
                EquToMesDownReasonAPI.DownReason[] arrDownReason = lstRs.ToArray();

                endTime = DateTime.Now;
                //Def.WriteLog("MesOperate", "上报停机原因：" + JsonConvert.SerializeObject(body));
                var result = equToMes.DownReason(arrDownReason);

                //Def.WriteLog("MesOperate", "上报停机原因返回：" + JsonConvert.SerializeObject(result));

                TimeSpan ts = endTime - startTime;
                string second = (ts.TotalMilliseconds / 1000).ToString() + "S";

                var outparm = new
                {
                    Code = result.Code.ToString(),
                    Msg = result.Msg
                };


                string retsValue = JsonConvert.SerializeObject(outparm);
                retsValue = RevertJsonString(retsValue);
                var mesState = new
                {
                    //ProcessCode = rs.ProcessCode,
                    //EquipmentPassWord = rs.EquPassword,

                    EquipmentCode = rs.EquipmentCode,
                    ResourceCode = rs.ResourceCode,
                    OperatorUserID = rs.OperatorUserID,
                    OperatorPassword = rs.OperatorPassword,
                    DownReasonCode = $"{(int)equDown}",
                    BeginTime = DateTime.Now.AddMinutes(-5),
                    EndTime = DateTime.Now
                };

                string loginret = JsonConvert.SerializeObject(mesState);
                loginret = RevertJsonString(loginret);

                string sfcode = " ";
                string linecode = " ";
                //string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{cfg.mesUri},{second},{ result.Code.ToString()},{retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{loginret}";

                if (null != result)
                {
                    if (1 != result.Code)
                    {
                        errMsg = $"设备停机采集 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesDownReason 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                errMsg = $"Code:-2, Msg:EquToMesDownReason {ex.Message}";
            }
            return false;
        }

        /// <summary>
        /// EquToMesRecipeListGet,          // 获取开机参数列表
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="recipe"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool EquToMesRecipeListGet(ResourcesStruct rs, ref MesRecipeStruct[] recipe, ref string errMsg)
        {
            DateTime dateTime = DateTime.Now;
            string Code = "";
            string retsValue = "";
            string mesUri = "";
            try
            {
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeListGet);
                if (!cfg.enable)
                {
                    return true;
                }
                var header = new EquToMesRecipeListGetAPI.LoginSoapHeader();
                //header.ProcessCode = rs.ProcessCode;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesRecipeListGetAPI.RecipeListGet();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;
                body.ProductCode = "";

                var equToMes = new EquToMesRecipeListGetAPI.EquToMesRecipeListGet();
                equToMes.LoginSoapHeaderValue = header;
                if (!string.IsNullOrEmpty(cfg.mesUri))
                {
                    equToMes.Url = cfg.mesUri;
                }
                mesUri = cfg.mesUri;
                var result = equToMes.GetRecipeList(body);
                if (null != result)
                {
                    Code = $"{result.Code}";
                    retsValue = result.Msg;
                    if (1 != result.Code)
                    {
                        errMsg = $"获取开机参数列表 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    recipe = new MesRecipeStruct[result.Data.Length];
                    for (int i = 0; i < result.Data.Length; i++)
                    {
                        recipe[i].RecipeCode = result.Data[i].RecipeCode;
                        recipe[i].Version = result.Data[i].Version;
                        recipe[i].ProductCode = result.Data[i].ProductCode;
                        recipe[i].LastUpdateOnTime = result.Data[i].LastUpdateOnTime.ToString(Def.DateFormal);
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesRecipeListGet 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                retsValue = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesRecipeListGet {ex.Message}";
            }
            finally
            {
                int second = (int)(DateTime.Now - dateTime).TotalSeconds;
                string sfcode = " ";
                string linecode = " ";

                var meslogin = new
                {
                    //ProcessCode = rs.ProcessCode,
                    //EquipmentPassWord = rs.EquPassword,

                    EquipmentCode = rs.EquipmentCode,
                    ResourceCode = rs.ResourceCode,
                    OperatorUserID = rs.OperatorUserID,
                    OperatorPassword = rs.OperatorPassword
                };

                string loginret = JsonConvert.SerializeObject(meslogin);
                loginret = RevertJsonString(loginret);

                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code}, {retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{loginret}";

                SaveLogData("MES获取开机参数列表", text);
            }
            return false;
        }

        /// <summary>
        /// EquToMesRecipeGet,              // 获取开机参数明细
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="data"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool EquToMesRecipeGet(ResourcesStruct rs, string recipeCode, ref MesParameterData[] param, ref string errMsg)
        {
            DateTime dateTime = DateTime.Now;
            string mesUri = "";
            string Code = "";
            string retsValue = "";
            try
            {
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeGet);
                if (!cfg.enable)
                {
                    return true;
                }
                var header = new EquToMesRecipeGetAPI.LoginSoapHeader();
                //header.ProcessCode = rs.ProcessCode;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesRecipeGetAPI.RecipeGet();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;
                body.RecipeCode = recipeCode;

                var equToMes = new EquToMesRecipeGetAPI.EquToMesRecipeGet();
                equToMes.LoginSoapHeaderValue = header;
                if (!string.IsNullOrEmpty(cfg.mesUri))
                {
                    equToMes.Url = cfg.mesUri;
                }
                mesUri = cfg.mesUri;
                var result = equToMes.GetRecipe(body);
                if (null != result)
                {
                    Code = $"{result.Code}";
                    retsValue = result.Msg;
                    if (1 != result.Code)
                    {
                        errMsg = $"获取开机参数明细 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    int len = result.Data.ParamList.Length;
                    param = new MesParameterData[len];
                    for (int i = 0; i < len; i++)
                    {
                        //param[i].ParamCode = result.Data.ParamList[i].ParamCode;
                        //param[i].Version = string.IsNullOrEmpty(result.Data.Version) ? "" : result.Data.Version;
                        //param[i].ParamValue = string.IsNullOrEmpty(result.Data.ParamList[i].ParamValue) ? "" : result.Data.ParamList[i].ParamValue;
                        //param[i].ParamUpper = string.IsNullOrEmpty(result.Data.ParamList[i].ParamUpper) ? "" : result.Data.ParamList[i].ParamUpper;
                        //param[i].ParamLower = string.IsNullOrEmpty(result.Data.ParamList[i].ParamLower) ? "" : result.Data.ParamList[i].ParamLower;
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesRecipeGet 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                Code = $"-2";
                retsValue = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesRecipeGet {ex.Message}";
            }
            finally
            {
                int second = (int)(DateTime.Now - dateTime).TotalSeconds;
                string sfcode = " ";
                string linecode = " ";

                var meslogin = new
                {
                    //EquUserID = rs.EquUserID,
                    //EquipmentPassWord = rs.EquPassword,

                    EquipmentCode = rs.EquipmentCode,
                    ResourceCode = rs.ResourceCode,
                    OperatorUserID = rs.OperatorUserID,
                    OperatorPassword = rs.OperatorPassword,
                    RecipeCode = recipeCode
                };

                string loginret = JsonConvert.SerializeObject(meslogin);
                loginret = RevertJsonString(loginret);

                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code}, {retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{loginret}";

                SaveLogData("MES获取开机参数明细", text);
            }
            return false;
        }

        /// <summary>
        /// EquToMesRecipeVExamine,         // 开机参数版本校验
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="recipeCode"></param>
        /// <param name="version"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool EquToMesRecipeVExamine(ResourcesStruct rs, string recipeCode, string version, ref string errMsg)
        {
            DateTime dateTime = DateTime.Now;
            string mesUri = "";
            string Code = "";
            string retsValue = "";
            DateTime startTime = DateTime.Now;
            try
            {
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeVExamine);
                if (!cfg.enable)
                {
                    return true;
                }
                var header = new EquToMesRecipeVEAPI.LoginSoapHeader();
                //header.EquUserID = rs.EquUserID;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesRecipeVEAPI.RecipeVersionExamine();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;
                body.RecipeCode = recipeCode;
                body.Version = version;

                var equToMes = new EquToMesRecipeVEAPI.EquToMesRecipeVersionExamine();
                equToMes.LoginSoapHeaderValue = header;
                if (!string.IsNullOrEmpty(cfg.mesUri))
                {
                    equToMes.Url = cfg.mesUri;
                }
                mesUri = cfg.mesUri;
                var result = equToMes.RecipeVersionExamine(body);
                if (null != result)
                {
                    Code = result.Code.ToString();
                    retsValue = result.Msg;
                    if (1 != result.Code)
                    {
                        errMsg = $"开机参数版本校验 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesRecipeVersionExamine 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                retsValue = ex.Message;

                errMsg = $"Code:-2, Msg:EquToMesRecipeVersionExamine {ex.Message}";
            }
            finally
            {
                int second = (int)(DateTime.Now - dateTime).TotalSeconds;
                string sfcode = " ";
                string linecode = " ";

                var meslogin = new
                {
                    //EquUserID = rs.EquUserID,
                    //EquipmentPassWord = rs.EquPassword,

                    EquipmentCode = rs.EquipmentCode,
                    ResourceCode = rs.ResourceCode,
                    OperatorUserID = rs.OperatorUserID,
                    OperatorPassword = rs.OperatorPassword,
                    RecipeCode = recipeCode,
                    Version = version
                };

                string loginret = JsonConvert.SerializeObject(meslogin);
                loginret = RevertJsonString(loginret);

                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{ Code}, {retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{loginret}";

                SaveLogData("MES开机参数版本校验", text);
            }
            return false;
        }

        /// <summary>
        /// EquToMesRecipe,                 // 开机参数采集
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool EquToMesRecipe(ResourcesStruct rs, MesRecipeStruct recipe, ref string errMsg)
        {
            DateTime dateTime = DateTime.Now;
            string mesUri = "";
            string Code = "";
            string retsValue = "";
            string sendValue = "";
            try
            {
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipe);
                if (!cfg.enable)
                {
                    return true;
                }
                var header = new EquToMesRecipeAPI.LoginSoapHeader();
                //header.EquUserID = rs.EquUserID;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesRecipeAPI.Recipe();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;
                body.RecipeCode = recipe.RecipeCode;
                body.Version = recipe.Version;
                body.ProductCode = recipe.ProductCode;
                body.ParamList = new EquToMesRecipeAPI.RecipeParam[recipe.Param.Count];
                for (int i = 0; i < recipe.Param.Count; i++)
                {
                    //body.ParamList[i] = new EquToMesRecipeAPI.RecipeParam();
                    //body.ParamList[i].ParamCode = recipe.Param[i].ParamCode;
                    //body.ParamList[i].ParamValue = recipe.Param[i].ParamValue;
                    //body.ParamList[i].ParamUpper = recipe.Param[i].ParamUpper;
                    //body.ParamList[i].ParamLower = recipe.Param[i].ParamLower;
                    //body.ParamList[i].Timestamp = DateTime.Now;
                }

                var equToMes = new EquToMesRecipeAPI.EquToMesRecipe();
                equToMes.LoginSoapHeaderValue = header;
                if (!string.IsNullOrEmpty(cfg.mesUri))
                {
                    equToMes.Url = cfg.mesUri;
                }
                mesUri = cfg.mesUri;
                sendValue = RevertJsonString(JsonConvert.SerializeObject(body));
                var result = equToMes.Recipe(body);
                if (null != result)
                {
                    Code = $"{result.Code}";
                    retsValue = result.Msg;
                    if (1 != result.Code)
                    {
                        errMsg = $"开机参数采集 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesRecipe 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                Code = $"-2";
                retsValue = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesRecipe {ex.Message}";
            }
            finally
            {
                int second = (int)(DateTime.Now - dateTime).TotalSeconds;
                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code}, {retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{sendValue}";

                SaveLogData("MES开机参数采集", text);
            }
            return false;
        }

        /// <summary>
        /// EquToMesCheckSfc,		     // 电芯校验并绑托盘：电芯校验
        /// </summary>
        /// <returns></returns>
        public static bool EquToMesCheckSfc(ResourcesStruct rs, string sfcCode, ref string errMsg)
        {
            string mesUri = "";
            string Code = "";
            string retsValue = "";
            string sendValue = "";
            DateTime dateTime = DateTime.Now;

            try
            {
                //var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesCheckSfc);
                //if (!cfg.enable)
                //{
                //    return true;
                //}
                var header = new EquToMesBakingAPI.LoginSoapHeader();
                //header.EquUserID = rs.EquUserID;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesBakingAPI.EquipmentBakingCheckRequest();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;
                body.SFC = sfcCode;

                var equToMes = new EquToMesBakingAPI.EquToMesBaking();

                equToMes.LoginSoapHeaderValue = header;
                //if (!string.IsNullOrEmpty(cfg.mesUri))
                //{
                //    equToMes.Url = cfg.mesUri;
                //}
                mesUri = equToMes.Url;
                sendValue = RevertJsonString(JsonConvert.SerializeObject(body));

                var result = equToMes.CheckSFC(body);

                if (null != result)
                {
                    Code = $"{result.Code}";
                    retsValue = result.Msg;
                    if (1 != result.Code)
                    {
                        errMsg = $"电芯校验 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesCheckSFC 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                retsValue = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesCheckSFC {ex.Message}";
            }
            finally
            {
                int second = (int)(DateTime.Now - dateTime).TotalSeconds;
                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code}, {retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{sendValue}";

                SaveLogData("MES电芯校验", text);
            }
            return false;
        }

        /// <summary>
        /// EquToMesBindContainer,          // 电芯校验并绑托盘：托盘绑定
        /// </summary>
        /// <param name="rs"></param>
        /// <returns></returns>
        public static bool EquToMesBindContainer(ResourcesStruct rs, Pallet plt, ref string errMsg)
        {
            DateTime dateTime = DateTime.Now;
            string mesUri = "";
            string Code = "";
            string retsValue = "";
            string sendValue = "";
            int second = 0;

            if (!string.IsNullOrEmpty(plt.Battery[0, 0].Code) && plt.Battery[0, 0].Code.StartsWith("TEST"))
            {
                return true;
            }

            try
            {

                //var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesBindContainer);
                //if (!cfg.enable)
                //{
                //    return true;
                //}
                var header = new EquToMesBakingAPI.LoginSoapHeader();
                //header.EquUserID = rs.EquUserID;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesBakingAPI.BindContainer();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;
                body.ContainerCode = plt.Code;

                int batRow = plt.MaxRow;
                int batCol = plt.MaxCol;
                var bindCon = new List<EquToMesBakingAPI.BindContainerSFC>();

                for (int row = 0; row < batRow; row++)
                {
                    for (int col = 0; col < batCol; col++)
                    {
                        if (plt.Battery[row, col].Type == BatteryStatus.OK)
                        {
                            if (plt.Battery[row, col].Code.Length > 2)
                            {
                                var sfc = new EquToMesBakingAPI.BindContainerSFC();
                                sfc.Location = (row * batCol + col + 1).ToString();
                                sfc.SFC = plt.Battery[row, col].Code;
                                bindCon.Add(sfc);
                            }
                        }
                    }
                }
                body.ContainerSFCs = bindCon.ToArray();

                var equToMes = new EquToMesBakingAPI.EquToMesBaking();

                equToMes.LoginSoapHeaderValue = header;
                //if (!string.IsNullOrEmpty(cfg.mesUri))
                //{
                //    equToMes.Url = cfg.mesUri;
                //}
                sendValue = RevertJsonString(JsonConvert.SerializeObject(body));
                var result = equToMes.BakingBindContainer(body);

                if (null != result)
                {
                    Code = result.Code.ToString();
                    retsValue = result.Msg;
                    if (1 != result.Code && 11121 != result.Code) // 11121 重复绑盘
                    {
                        errMsg = $"托盘绑定 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesBindContainer 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                retsValue = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesBindContainer {ex.Message}";
            }
            finally
            {
                second = (int)(DateTime.Now - dateTime).TotalSeconds;
                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code}, {retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{sendValue}";

                SaveLogData("MES托盘绑定", text);
            }
            return false;
        }

        /// <summary>
        /// EquToMesUnBindContainer,          // 电芯校验并绑托盘：托盘解绑
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="plt"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool EquToMesUnBindContainer(ResourcesStruct rs, Pallet plt, ref string errMsg)
        {
            DateTime dateTime = DateTime.Now;
            string mesUri = "";
            string Code = "";
            string retsValue = "";
            string sendValue = "";
            int second = 0;

            if (!string.IsNullOrEmpty(plt.Battery[0, 0].Code) && plt.Battery[0, 0].Code.StartsWith("TEST"))
            {
                return true;
            }

            try
            {
                //var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesUnBindContainer);
                //if (!cfg.enable)
                //{
                //    return true;
                //}
                var header = new EquToMesUnBindContainerAPI.LoginSoapHeader();
                //header.EquUserID = rs.EquUserID;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesUnBindContainerAPI.UnBindContainerInCP();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;
                body.ContainerCode = plt.Code;

                int batRow = plt.MaxRow;
                int batCol = plt.MaxCol;
                var bindCon = new List<string>();

                string sfc = "";
                for (int row = 0; row < batRow; row++)
                {
                    for (int col = 0; col < batCol; col++)
                    {
                        if (plt.Battery[row, col].Code.Length > 2 && plt.Battery[row, col].Type != BatteryStatus.Fake)
                        {
                            sfc = plt.Battery[row, col].Code;
                            bindCon.Add(sfc);
                        }
                    }
                }
                body.SFCs = bindCon.ToArray();

                var equToMes = new EquToMesUnBindContainerAPI.EquToMesUnBindContainerInCP();

                equToMes.LoginSoapHeaderValue = header;
                //if (!string.IsNullOrEmpty(cfg.mesUri))
                //{
                //    equToMes.Url = cfg.mesUri;
                //}
                sendValue = RevertJsonString(JsonConvert.SerializeObject(body));
                var result = equToMes.UnBindContainerInCP(body);

                if (null != result)
                {
                    Code = result.Code.ToString();
                    retsValue = result.Msg;
                    if (1 != result.Code)
                    {
                        errMsg = $"托盘解绑 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesUnBindContainer 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                retsValue = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesUnBindContainer {ex.Message}";
            }
            finally
            {
                second = (int)(DateTime.Now - dateTime).TotalSeconds;
                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code}, {retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{sendValue}";

                SaveLogData("MES托盘解绑", text);
            }
            return false;
        }

        /// <summary>
        /// EquToMesInBaking,               // 电芯校验并绑托盘：满托盘进站
        /// </summary>
        /// <returns></returns>
        public static bool EquToMesInBaking(ResourcesStruct rs, Pallet plt, ref string errMsg)
        {
            string retsValue = "";
            string mesUri = "";
            string Code = "";
            string sendValue = "";
            DateTime startTime = DateTime.Now;

            if (!string.IsNullOrEmpty(plt.Battery[0, 0].Code) && plt.Battery[0, 0].Code.StartsWith("TEST"))
            {
                return true;
            }

            try
            {
                //var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesInBaking);
                //if (!cfg.enable)
                //{
                //    return true;
                //}
                var header = new EquToMesBakingAPI.LoginSoapHeader();
                //header.EquUserID = rs.EquUserID;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesBakingAPI.EquipmentBoundInBakingRequest();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;
                body.ContainerCode = plt.Code;

                int batRow = plt.MaxRow;
                int batCol = plt.MaxCol;
                var bindCon = new List<EquToMesBakingAPI.ContainerInfo>();
                for (int row = 0; row < batRow; row++)
                {
                    for (int col = 0; col < batCol; col++)
                    {
                        if (plt.Battery[row, col].Type == BatteryStatus.OK && (plt.Battery[row, col].Code.Length > 2))
                        {
                            var sfc = new EquToMesBakingAPI.ContainerInfo();
                            sfc.Location = (row * batCol + col + 1).ToString();
                            sfc.SFC = plt.Battery[row, col].Code;
                            bindCon.Add(sfc);
                        }
                    }
                }
                body.ContainerInfo = bindCon.ToArray();

                var equToMes = new EquToMesBakingAPI.EquToMesBaking();

                equToMes.LoginSoapHeaderValue = header;
                //if (!string.IsNullOrEmpty(cfg.mesUri))
                //{
                //    equToMes.Url = cfg.mesUri;
                //}
                mesUri = equToMes.Url;

                sendValue = RevertJsonString(JsonConvert.SerializeObject(body));

                var result = equToMes.BoundInBaking(body);
                if (null != result)
                {
                    var outparm = new
                    {
                        Code = result.Code.ToString(),
                        Msg = result.Msg
                    };
                    retsValue = RevertJsonString(JsonConvert.SerializeObject(outparm));

                    if (1 != result.Code)
                    {
                        errMsg = $"满托盘进站 Code:{result.Code}, Msg:{result.Msg}";
                        return false;
                    }
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesInBaking 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                retsValue = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesInBaking {ex.Message}";
            }
            finally
            {
                string second = $"{(int)(DateTime.Now - startTime).TotalSeconds} S";

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{sendValue}";

                SaveLogData("MES满托盘进站", text);
            }
            return false;
        }

        /// <summary>
        /// EquToMesOutBaking,              // 水含量测试上传并出站，解绑：出站解绑
        /// </summary>
        /// <returns></returns>
        public static bool EquToMesOutBaking(ResourcesStruct rs, Pallet plt, Dictionary<string, object> param, bool isDebug, ref string errMsg)
        {
            Def.WriteLog("EquToMesOutBaking", $"EquToMesOutBaking:[{plt.Code}]调用出站接口");

            string mesUri = "";
            int resCode = 0;
            string Msg = "";
            string retsValue = "";
            string loginret = "";
            int second = 0;
            bool Uploaded = false;
            DateTime startTime = DateTime.Now;

            try
            {
                //var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesOutBaking);
                //if (!cfg.enable)
                //{
                //    Def.WriteLog("EquToMesOutBaking", $"EquToMesOutBaking:[{plt.Code}]出站功能未启用");
                //    return true;
                //}
                var header = new EquToMesBakingAPI.LoginSoapHeader();
                //header.EquUserID = rs.EquUserID;
                //header.EquPassword = rs.EquPassword;
                header.OperatorUserID = rs.OperatorUserID;
                header.OperatorPassword = rs.OperatorPassword;

                var body = new EquToMesBakingAPI.EquipmentOutBoundInBakingRequest();
                body.EquipmentCode = rs.EquipmentCode;
                body.ResourceCode = rs.ResourceCode;
                body.LocalTime = DateTime.Now;

                int batRow = plt.MaxRow;
                int batCol = plt.MaxCol;
                DateTime dt = DateTime.Now;
                var bindCon = new List<EquToMesBakingAPI.ContainerResultInfo>();
                for (int row = 0; row < batRow; row++)
                {
                    for (int col = 0; col < batCol; col++)
                    {
                        if (plt.Battery[row, col].Type == BatteryStatus.OK && (plt.Battery[row, col].Code.Length > 2))
                        {
                            var sfc = new EquToMesBakingAPI.ContainerResultInfo();
                            sfc.SFCLocation = (row * batCol + col + 1);
                            sfc.SFC = plt.Battery[row, col].Code;
                            sfc.Passed = (BatteryStatus.NG == plt.Battery[row, col].Type) ? 0 : 1;

                            var paramList = new List<EquToMesBakingAPI.ParamDTO>();

                            foreach (var itm in param)
                            {
                                paramList.Add(new EquToMesBakingAPI.ParamDTO { ParamCode = itm.Key, ParamValue = itm.Value.ToString(), Timestamp = dt });
                            }

                            sfc.ParamList = paramList.ToArray();
                            if (plt.Battery[row, col].Type == BatteryStatus.NG)
                            {
                                sfc.NG = new EquToMesBakingAPI.ProductNG[1];
                                sfc.NG[0] = new EquToMesBakingAPI.ProductNG();
                                sfc.NG[0].NGCode = plt.Battery[row, col].NGType.ToString();
                            }
                            bindCon.Add(sfc);
                        }
                    }
                }
                //body.ContainerInfo = bindCon.ToArray();

                List<EquToMesBakingAPI.OutBoundContainerInfo> lstOutBoundContainerInfo = new List<EquToMesBakingAPI.OutBoundContainerInfo>();

                EquToMesBakingAPI.OutBoundContainerInfo outBoundContainerInfo = new EquToMesBakingAPI.OutBoundContainerInfo();
                outBoundContainerInfo.ContainerCode = plt.Code;
                outBoundContainerInfo.ContainerInfo = bindCon.ToArray();
                lstOutBoundContainerInfo.Add(outBoundContainerInfo);

                body.OutBoundContainerInfo = lstOutBoundContainerInfo.ToArray();

                var equToMes = new EquToMesBakingAPI.EquToMesBaking();

                equToMes.LoginSoapHeaderValue = header;
                //if (!string.IsNullOrEmpty(cfg.mesUri))
                //{
                //    equToMes.Url = cfg.mesUri;
                //}
                //mesUri = cfg.mesUri;

                loginret = JsonConvert.SerializeObject(body);
                loginret = RevertJsonString(loginret);

                EquToMesBakingAPI.ApiResponseForScada result = new EquToMesBakingAPI.ApiResponseForScada();
                if (!isDebug)
                {
                    Def.WriteLog("EquToMesOutBaking", $"EquToMesOutBaking:[{plt.Code}]提交出站");
                    result = equToMes.OutBoundInBaking(body);
                }
                else
                {
                    Def.WriteLog("EquToMesOutBaking", $"EquToMesOutBaking:[{plt.Code}]手动出站成功");
                    result.Code = 1;
                    result.Msg = "手动出站成功";
                }
                if (null != result)
                {
                    retsValue = JsonConvert.SerializeObject(result);

                    resCode = result.Code;
                    Msg = result.Msg;
                    if (1 == result.Code)
                    {
                        Uploaded = true;
                    }
                    else if (11119 == result.Code)
                    {
                        Uploaded = true;

                        errMsg = $"出站解绑 Code:{result.Code}, Msg:{result.Msg} 重复出站";
                        Def.WriteLog("EquToMesOutBaking", $"EquToMesOutBaking:[{plt.Code}]{errMsg}");
                    }
                    else
                    {
                        errMsg = $"出站解绑 Code:{result.Code}, Msg:{result.Msg}";
                    }
                }
                else
                {
                    retsValue = errMsg = $"Code:-1, Msg:EquToMesOutBaking 调用失败，无法访问";
                }
            }
            catch (System.Exception ex)
            {
                resCode = -2;
                Msg = ex.Message;
                retsValue = errMsg = $"Code:-2, Msg:EquToMesOutBaking {ex.Message}";
            }
            finally
            {
                second = (int)(DateTime.Now - startTime).TotalSeconds;

                retsValue = RevertJsonString(retsValue);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{resCode},{retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{loginret}";

                SaveLogData("MES出站解绑", text);
            }
            return Uploaded;
        }

        static bool GetwaterValue(string paramCode, double[] waterValue, ref double paramValue)
        {
            //DF2280024 正极片水分
            //DF2280025 负极片水分
            //DF2280026 隔膜水分
            //DF2280027 混合样水分
            switch (paramCode)
            {
                case "DF2280024":
                    paramValue = waterValue[0];
                    break;
                case "DF2280025":
                    paramValue = waterValue[1];
                    break;
                case "DF2280026":
                    paramValue = waterValue[2];
                    break;
                case "DF2280027":
                    paramValue = waterValue[3];
                    break;
                default:
                    return false;
            }
            return true;
        }
        //public static bool EquToMesOutBaking(ResourcesStruct rs, Pallet plt, double waterValue, ref string errMsg)
        //{
        //    try
        //    {
        //        DateTime startTime = DateTime.Now;
        //        DateTime endTime = DateTime.Now;
        //        var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesOutBaking);
        //        if (!cfg.enable)
        //        {
        //            return true;
        //        }
        //        var header = new EquToMesBakingAPI.LoginSoapHeader();
        //        header.EquUserID = rs.EquUserID;
        //        header.EquPassword = rs.EquPassword;
        //        header.OperatorUserID = rs.OperatorUserID;
        //        header.OperatorPassword = rs.OperatorPassword;

        //        var body = new EquToMesBakingAPI.EquipmentOutBoundInBakingRequest();
        //        body.EquipmentCode = rs.EquipmentCode;
        //        body.ResourceCode = rs.ResourceCode;
        //        body.LocalTime = DateTime.Now;
        //        //body.ContainerCode = plt.Code;

        //        int batRow = plt.MaxRow;
        //        int batCol = plt.MaxCol;
        //        DateTime dt = DateTime.Now;
        //        var bindCon = new List<EquToMesBakingAPI.ContainerResultInfo>();
        //        for (int row = 0; row < batRow; row++)
        //        {
        //            for (int col = 0; col < batCol; col++)
        //            {
        //                if (plt.Battery[row, col].Type == BatteryStatus.OK)
        //                {
        //                    var sfc = new EquToMesBakingAPI.ContainerResultInfo();
        //                    sfc.SFCLocation = (row * batCol + col + 1);
        //                    sfc.SFC = plt.Battery[row, col].Code;
        //                    sfc.Passed = (BatteryStatus.NG == plt.Battery[row, col].Type) ? 0 : 1;
        //                    //sfc.Moisture = Convert.ToDecimal(waterValue);
        //                    var paramList = new List<EquToMesBakingAPI.ParamDTO>();
        //                    foreach (var itemRecipe in cfg.recipe)
        //                    {
        //                        if (null != itemRecipe.Value.Param)
        //                        {
        //                            foreach (var item in itemRecipe.Value.Param)
        //                            {
        //                                var param = new EquToMesBakingAPI.ParamDTO();
        //                                param.ParamCode = item.ParamCode;
        //                                param.ParamValue = item.ParamValue;
        //                                param.Timestamp = dt;
        //                                paramList.Add(param);
        //                            }
        //                        }
        //                    }
        //                    sfc.ParamList = paramList.ToArray();
        //                    if (plt.Battery[row, col].Type == BatteryStatus.NG)
        //                    {
        //                        sfc.NG = new EquToMesBakingAPI.ProductNG[1];
        //                        sfc.NG[0] = new EquToMesBakingAPI.ProductNG();
        //                        sfc.NG[0].NGCode = plt.Battery[row, col].NGType.ToString();
        //                    }
        //                    bindCon.Add(sfc);
        //                }
        //            }
        //        }
        //        //body.ContainerInfo = bindCon.ToArray();

        //        List<EquToMesBakingAPI.OutBoundContainerInfo> lstOutBoundContainerInfo = new List<EquToMesBakingAPI.OutBoundContainerInfo>();

        //        EquToMesBakingAPI.OutBoundContainerInfo outBoundContainerInfo = new EquToMesBakingAPI.OutBoundContainerInfo();
        //        outBoundContainerInfo.ContainerCode = plt.Code;
        //        outBoundContainerInfo.ContainerInfo = bindCon.ToArray();
        //        lstOutBoundContainerInfo.Add(outBoundContainerInfo);

        //        body.OutBoundContainerInfo = lstOutBoundContainerInfo.ToArray();

        //        var equToMes = new EquToMesBakingAPI.EquToMesBaking();

        //        equToMes.LoginSoapHeaderValue = header;
        //        if (!string.IsNullOrEmpty(cfg.mesUri))
        //        {
        //            equToMes.Url = cfg.mesUri;
        //        }
        //        endTime = DateTime.Now;
        //        var result = equToMes.OutBoundInBaking(body);
        //        TimeSpan ts = endTime - startTime;
        //        string second = (ts.TotalMilliseconds / 1000).ToString() + "S";

        //        var outparm = new
        //        {
        //            Code = result.Code.ToString(),
        //            Msg = result.Msg
        //        };


        //        string retsValue = JsonConvert.SerializeObject(outparm);
        //        retsValue = RevertJsonString(retsValue);

        //        var meslogin = new
        //        {
        //            EquUserID = rs.EquUserID,
        //            EquipmentPassWord = rs.EquPassword,

        //            EquipmentCode = rs.EquipmentCode,
        //            ResourceCode = rs.ResourceCode,
        //            OperatorUserID = rs.OperatorUserID,
        //            OperatorPassword = rs.OperatorPassword,
        //            ContainerCode = plt.Code
        //        };

        //        string loginret = JsonConvert.SerializeObject(meslogin);
        //        loginret = RevertJsonString(loginret);

        //        string sfcode = " ";
        //        string linecode = " ";
        //        string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{cfg.mesUri},{second},{ result.Code.ToString()},{retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{loginret}";

        //        SaveLogData("出站解绑", text);

        //        if (null != result)
        //        {
        //            if (1 != result.Code)
        //            {
        //                errMsg = $"出站解绑 Code:{result.Code}, Msg:{result.Msg}";
        //                return false;
        //            }
        //            return true;
        //        }
        //        errMsg = $"Code:-1, Msg:EquToMesOutBaking 调用失败，无法访问";
        //    }
        //    catch (System.Exception ex)
        //    {
        //        errMsg = $"Code:-2, Msg:EquToMesOutBaking {ex.Message}";
        //    }
        //    return false;
        //}

        /// <summary>
        /// EquToMesGetContainer,           // 电芯烘烤-获取托盘明细接口，用于提交绑盘查询绑盘信息
        /// </summary>
        /// <returns></returns>
        //public static bool EquToMesGetContainer(ResourcesStruct rs, string pltCode, ref string errMsg)
        //{
        //    string mesUri = "";
        //    string Code = "";
        //    string retsValue = "";
        //    string mesSend = "";
        //    DateTime startTime = DateTime.Now;
        //    try
        //    {
        //        var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesGetContainer);
        //        if (!cfg.enable)
        //        {
        //            return true;
        //        }
        //        var header = new EquToMesBakingAPI.LoginSoapHeader();
        //        //header.EquUserID = rs.EquUserID;
        //        //header.EquPassword = rs.EquPassword;
        //        header.OperatorUserID = rs.OperatorUserID;
        //        header.OperatorPassword = rs.OperatorPassword;

        //        var body = new EquToMesBakingAPI.EquipmentBakingGetBindContainerRequest();
        //        body.EquipmentCode = rs.EquipmentCode;
        //        body.ResourceCode = rs.ResourceCode;
        //        body.LocalTime = DateTime.Now;
        //        body.ContainerCode = pltCode;

        //        var equToMes = new EquToMesBakingAPI.EquToMesBaking();
        //        equToMes.LoginSoapHeaderValue = header;
        //        if (!string.IsNullOrEmpty(cfg.mesUri))
        //        {
        //            equToMes.Url = cfg.mesUri;
        //        }
        //        mesUri = equToMes.Url;
        //        mesSend = RevertJsonString(JsonConvert.SerializeObject(body));
        //        var result = equToMes.GetBindContainer(body);

        //        if (null != result)
        //        {
        //            var outparm = new
        //            {
        //                Code = result.Code.ToString(),
        //                Msg = result.Msg
        //            };
        //            retsValue = RevertJsonString(JsonConvert.SerializeObject(outparm));
        //            if (1 != result.Code)
        //            {
        //                errMsg = $"获取托盘明细 Code:{result.Code}, Msg:{result.Msg}";
        //                return false;
        //            }
        //            return true;
        //        }
        //        errMsg = $"Code:-1, Msg:EquToMesGetContainer 调用失败，无法访问";
        //    }
        //    catch (System.Exception ex)
        //    {
        //        Code = "-2";
        //        retsValue = ex.Message;
        //        errMsg = $"Code:-2, Msg:EquToMesGetContainer {ex.Message}";
        //    }
        //    finally
        //    {
        //        string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

        //        string sfcode = " ";
        //        string linecode = " ";
        //        string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{retsValue},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{mesSend}";

        //        SaveLogData("MES获取托盘明细", text);
        //    }
        //    return false;
        //}

        /// <summary>
        /// IOT平台数据采集
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool EquToIOTServer(ResourcesStruct rs, IOTData pData, ref string errMsg)
        {
            string mesUri = "";
            string Code = "";
            string mesRecv = "";
            string mesSend = "";
            DateTime startTime = DateTime.Now;
            try
            {
                //var cfg = MesDefine.GetMesCfg(MesInterface.EquToIOTServer);
                //if (!cfg.enable)
                //{
                //    return true;
                //}
                //mesUri = cfg.mesUri;

                var body = new Dictionary<string, object>();
                body.Add("line", pData.line == 0 ? 1 : pData.line);
                body.Add("equip", pData.equip);
                body.Add("floor", pData.floor);
                body.Add("datetime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                body.Add("points", pData.points);

                mesSend = JsonConvert.SerializeObject(body);
                Trace.WriteLine("IOT采集上报：" + mesSend);
                mesRecv = httpClient.Post(mesUri, mesSend);
                if (null != mesRecv)
                {
                    Trace.WriteLine("IOT采集上报返回：" + mesRecv);
                    //var outparm = new
                    //{
                    //    Code = result.Code.ToString(),
                    //    Msg = result.Msg
                    //};
                    //retsValue = RevertJsonString(JsonConvert.SerializeObject(outparm));
                    //if (1 != result.Code)
                    //{
                    //    errMsg = $"获取托盘明细 Code:{result.Code}, Msg:{result.Msg}";
                    //    return false;
                    //}
                    return true;
                }
                errMsg = $"Code:-1, Msg:EquToMesGetContainer 调用失败，无法访问";
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                mesRecv = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesGetContainer {ex.Message}";
            }
            finally
            {
                string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

                mesSend = RevertJsonString(mesSend);
                mesRecv = RevertJsonString(mesRecv);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{mesRecv},{linecode},{rs.EquipmentCode},{rs.OperatorUserID},{mesSend}";

                SaveLogData("IOT采集数据上报", text);
            }
            return false;
        }
    }
}
