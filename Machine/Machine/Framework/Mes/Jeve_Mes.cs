using Machine.MYSQL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static Machine.MYSQL.MySqlBatteryBasket;

namespace Machine.Framework.Mes
{
    public static class Jeve_Mes
    {
        private static string fileName = "MESlog";

        private static HttpClient httpClient = new HttpClient();      // 通讯的http对象

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
        /// 上位机人员数据验证  业务逻辑OK
        /// </summary>
        /// <returns></returns>
        public static bool Mes_CheckUser(string userCode,string workPlace,ref UserInfo userInfo, ref string errMsg)
        {
            string mesUri="",Code="",mesRecv="",mesSend="",uCaller="DAL1HK01",msg = "";
            int result = -1;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    return true;
                }
                //if (!MachineCtrl.GetInstance().isMESConnect)
                //{
                //    return false;
                //}
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesCheckUser);
                if (!cfg.enable)
                {
                    return true;
                }
                mesUri = cfg.mesUri;

                var body = new
                {
                    UCaller = uCaller,
                    UserCode = userCode,
                    WorkPlace = workPlace
                };

                mesSend = JsonConvert.SerializeObject(body);
                string recvValue = httpClient.Post(mesUri, mesSend);
                //mesSend = Regex.Replace((mesSend),@"\s", "");
                //mesSend = MesOperate.RevertJsonString(mesSend);
                mesSend = Regex.Replace(mesSend.ToString(), @"\s", "");
                mesRecv = MesOperate.RevertJsonString(recvValue);
                JObject recvObj = JObject.Parse(recvValue);

                if (recvObj != null)
                {
                    result = Convert.ToInt32(recvObj["TransFlag"]);
                    msg = recvObj["ErrorMessage"].ToString();
                    userInfo.UserCode = recvObj["UserCode"].ToString();
                    userInfo.UserName = recvObj["UserName"].ToString();
                    userInfo.UserArea = recvObj["UserArea"].ToString();
                    userInfo.UserDep = recvObj["UserDep"].ToString();
                    if (result!=1)
                    {
                        
                        errMsg = $"人员数据校验失败! \r\nMsg:{msg}";

                        return false;
                    }

                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                mesRecv = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesCheckUser {ex.Message}";
            }
            finally
            {
                string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

                //mesSend = RevertJsonString(mesSend);
                //mesRecv = RevertJsonString(mesRecv);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{mesRecv},{linecode},{second},{second},{mesSend}";

                SaveLogData("人员数据校验", text);
            }
            return false;
        }

        /// <summary>
        /// 流拉筐信息获取  获取电芯信息数组并存储到MySQL数据库中，扫码再查询电芯信息
        /// </summary>
        /// <param name="vehicleNo">流拉筐</param>
        /// <param name="workPlace">工作场地</param>
        /// <param name="checkRoute">是否检查工艺路线  1:检查；0：不检查</param>
        /// <param name="unbind">是否解绑  1:解绑；0：不解绑</param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool Mes_GetVehicleInfo(string lotno, string workPlace,string checkRoute,string unbind,/*ref ListBasketData listBasketData,*/ ref string errMsg)
        {
            string mesUri = "", Code = "", mesRecv = "", mesSend = "", uCaller = "DAL1HK01", msg = "";
            int result = -1;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    return true;
                }
                //if (!MachineCtrl.GetInstance().isMESConnect)
                //{
                //    return false;
                //}
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesGetVehicleInfo);
                if (!cfg.enable)
                {
                    return true;
                }
                mesUri = cfg.mesUri;
                //unbind = "1";
                //vehicleNo = "LLK230824006";
                var body = new
                {
                    UCaller = uCaller,
                    LotNo = lotno,
                    //VehicleNo = vehicleNo,
                    WorkPlace = workPlace,
                    CheckRoute = checkRoute,
                    Unbind = unbind
                };

                mesSend = JsonConvert.SerializeObject(body);
                string recvValue = httpClient.Post(mesUri, mesSend);
                mesSend = Regex.Replace(mesSend.ToString(), @"\s", "");
                mesRecv = MesOperate.RevertJsonString(recvValue);
                JObject recvObj = JObject.Parse(recvValue);

                if (recvObj != null)
                {
                    result = Convert.ToInt32(recvObj["TransFlag"]);
                    msg = recvObj["ErrorMessage"].ToString();
                    if (result!=1)
                    {
                        errMsg = $"流拉筐信息获取失败! \r\nMsg:{msg}";
                        return false;

                    }
                    JArray items1 = (JArray)recvObj["Lot"];
                    MySqlBatteryBasket.BasketData basketData = new MySqlBatteryBasket.BasketData();

                    //循环遍历将参数添加到集合里面
                    for (int i = 0; i < items1.Count; i++)
                    {
                        var str = recvObj["Lot"][i].ToString();
                        //反序列化
                        MachineCtrl.BasketData InfoList = JsonConvert.DeserializeObject<MachineCtrl.BasketData>(str);
                        basketData.LotNo = InfoList.LotNo;
                        basketData.SlotNo = InfoList.SlotNo;
                        basketData.Status = InfoList.Status;
                        basketData.ErrorCode = InfoList.ErrorCode;
                        basketData.Grade = InfoList.Grade;
                        basketData.Exclude = InfoList.Exclude;
                        basketData.Fake = InfoList.Fake;
                        basketData.OpOrder = InfoList.OpOrder;
                        basketData.OpName = InfoList.OpName;
                        basketData.PreOpOrder = InfoList.PreOpOrder;
                        basketData.PreOpName = InfoList.PreOpName;
                        basketData.NGCount = InfoList.NGCount;
                        basketData.AllowReInput = InfoList.AllowReInput;
                        MySqlBatteryBasket.InsertRecord(basketData);
                        //listBasketData.basketDatas.Add(basketData);

                       

                    }
                   
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                mesRecv = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesGetVehicleInfo {ex.Message}";
            }
            finally
            {
                string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

                //mesSend = RevertJsonString(mesSend);
                //mesRecv = RevertJsonString(mesRecv);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{mesRecv},{linecode},{second},{second},{mesSend}";

                SaveLogData("流拉筐信息获取", text);
            }
            return false;
        }

        /// <summary>
        /// 设备状态变更  OK
        /// </summary>
        /// <param name="workPlace">设备编号</param>
        /// <param name="status">设备状态</param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool Mes_WorkPlaceStatus(string workPlace, MesMCState status, ref string errMsg)
        {
            string mesUri = "", Code = "", mesRecv = "", mesSend = "", uCaller = "DAL1HK01", msg = "";
            int result = -1;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    return true;
                }
                //if (!MachineCtrl.GetInstance().isMESConnect)
                //{
                //    return false;
                //}
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesWorkPlaceStatus);
                if (!cfg.enable)
                {
                    return true;
                }
                mesUri = cfg.mesUri;

                var body = new
                {
                    UCaller = uCaller,
                    WorkPlace = workPlace,
                    Status = (int)status
                };

                mesSend = JsonConvert.SerializeObject(body);
                string recvValue = httpClient.Post(mesUri, mesSend);
                mesSend = Regex.Replace(mesSend.ToString(), @"\s", "");
                mesRecv = MesOperate.RevertJsonString(recvValue);
                JObject recvObj = JObject.Parse(recvValue);

                if (recvObj != null)
                {
                    result = Convert.ToInt32(recvObj["TransFlag"]);
                    msg = recvObj["ErrorMessage"].ToString();
                    if (result!=1)
                    {
                        errMsg = $"设备状态变更上传失败! \r\nMsg:{msg}";

                        return false;
                    }
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                mesRecv = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesWorkPlaceStatus {ex.Message}";
            }
            finally
            {
                string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

                //mesSend = RevertJsonString(mesSend);
                //mesRecv = RevertJsonString(mesRecv);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{mesRecv},{linecode},{second},{second},{mesSend}";

                SaveLogData("设备状态变更", text);
            }
            return false;
        }

        /// <summary>
        /// 014 产品产出时报工 产出时上传电芯条码和电芯状态
        /// </summary>
        /// <param name="workPlace">工作场地</param>
        /// <param name="OpOrder">工序任务</param>
        /// <param name="LotNo">产出物编号</param>
        /// <param name="VehicleNo">载具号（蓝胶、流拉筐）</param>
        /// <param name="Status">批次状态(OK,NG)</param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool Mes_ReportSN(string workPlace, string lotNo, BatteryStatus status, ref string errMsg)
        {
            string mesUri = "", Code = "", opOrder="", mesRecv = "", mesSend = "",vehicleNo ="", uCaller = "DAL1HK01", msg = "";
            int result = -1;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    return true;
                }
                //if (!MachineCtrl.GetInstance().isMESConnect)
                //{
                //    return false;
                //}
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesReportSN);
                if (!cfg.enable)
                {
                    return true;
                }
                mesUri = cfg.mesUri;

                var body = new
                {
                    UCaller = uCaller,
                    WorkPlace = workPlace,
                    OpOrder = opOrder,
                    LotNo = lotNo,
                    VehicleNo= vehicleNo,
                    Status = status.ToString()
                };

                mesSend = JsonConvert.SerializeObject(body);
                string recvValue = httpClient.Post(mesUri, mesSend);
                mesSend = Regex.Replace(mesSend.ToString(), @"\s", "");
                mesRecv = MesOperate.RevertJsonString(recvValue);
                JObject recvObj = JObject.Parse(recvValue);

                if (recvObj != null)
                {
                    result = Convert.ToInt32(recvObj["TransFlag"]);
                    msg = recvObj["ErrorMessage"].ToString();
                    if (result != 1)
                    {
                        errMsg = $"产品产出时加工参数上报上传失败! \r\nMsg:{msg}";

                        return false;
                    }
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                mesRecv = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesReportSN {ex.Message}";
            }
            finally
            {
                string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

                //mesSend = RevertJsonString(mesSend);
                //mesRecv = RevertJsonString(mesRecv);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{mesRecv},{linecode},{second},{second},{mesSend}";

                SaveLogData("产品产出时加工参数上报", text);
            }
            return false;
        }

        /// <summary>
        /// 任务中断或关闭
        /// </summary>
        /// <param name="userCode">工号</param>
        /// <param name="opOrder">工序任务</param>
        /// <param name="workPlace">工作场地</param>
        /// <param name="status">状态（U：中断；E：关闭）</param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool Mes_LogoutOp(string userCode, string opOrder, string workPlace, bool status, ref string errMsg)
        {
            string mesUri = "", Code = "", mesRecv = "", mesSend = "", uCaller = "DAL1HK01", msg = "";
            int result = -1;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    return true;
                }
                //if (!MachineCtrl.GetInstance().isMESConnect)
                //{
                //    return false;
                //}
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesLogoutOp);
                if (!cfg.enable)
                {
                    return true;
                }
                mesUri = cfg.mesUri;

                var body = new
                {
                    UCaller = uCaller,
                    UserCode = userCode,
                    OpOrder = opOrder,
                    WorkPlace = workPlace,
                    Status = status?"E":"U"
                };

                mesSend = JsonConvert.SerializeObject(body);
                string recvValue = httpClient.Post(mesUri, mesSend);
                mesSend = Regex.Replace(mesSend.ToString(), @"\s", "");
                mesRecv = MesOperate.RevertJsonString(recvValue);
                JObject recvObj = JObject.Parse(recvValue);

                if (recvObj != null)
                {
                    result = Convert.ToInt32(recvObj["TransFlag"]);
                    msg = recvObj["ErrorMessage"].ToString();
                    if (result != 1)
                    {
                        errMsg = $"任务中断或关闭上传失败! \r\nMsg:{msg}";

                        return false;
                    }
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                mesRecv = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesLogoutOp {ex.Message}";
            }
            finally
            {
                string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

                //mesSend = RevertJsonString(mesSend);
                //mesRecv = RevertJsonString(mesRecv);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{mesRecv},{linecode},{second},{second},{mesSend}";

                SaveLogData("任务中断或关闭", text);
            }
            return false;
        }
        
        /// <summary>
        /// 运行中生产任务获取
        /// </summary>
        /// <param name="workPlace">工作场地</param>
        /// <param name="opOrder">任务号</param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool Mes_GetRunOpList(string workPlace, string opOrder, ref MesRecipeStruct mesRecipeStruct, ref string errMsg)
        {
            string mesUri = "", Code = "", mesRecv = "", mesSend = "", uCaller = "DAL1HK01", msg = "";
            int result = -1;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    return true;
                }
                //if (!MachineCtrl.GetInstance().isMESConnect)
                //{
                //    return false;
                //}
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesGetRunOpList);
                if (!cfg.enable)
                {
                    return true;
                }
                mesUri = cfg.mesUri;

                var body = new
                {
                    UCaller = uCaller,
                    WorkPlace = workPlace,
                    OpOrder = opOrder,
                    
                };

                mesSend = JsonConvert.SerializeObject(body);
                string recvValue = httpClient.Post(mesUri, mesSend);
                mesSend = Regex.Replace(mesSend.ToString(), @"\s", "");
                mesRecv = MesOperate.RevertJsonString(recvValue);
                JObject recvObj = JObject.Parse(recvValue);

                if (recvObj != null)
                {
                    if (!string.IsNullOrEmpty(opOrder))
                    {
                        result = Convert.ToInt32(recvObj["TransFlag"]);
                        msg = recvObj["ErrorMessage"].ToString();
                        if (result != 1)
                        {
                            errMsg = $"运行中生产任务获取失败! \r\nMsg:{msg}";

                            return false;
                        }
                    }
                    else
                    {
                        result = Convert.ToInt32(recvObj["TransFlag"]);
                        msg = recvObj["ErrorMessage"].ToString();
                        if (result != 1)
                        {
                            errMsg = $"运行中生产任务获取失败! \r\nMsg:{msg}";

                            return false;
                        }
                        //获取参数的数量
                        JArray items1 = (JArray)recvObj["Op"];
                        mesRecipeStruct.mesRunOps = new List<MesRunOp>();
                        for (int i = 0; i < items1.Count; i++)
                        {
                            var str = recvObj["Op"][i].ToString();
                            MesRunOp runOp = JsonConvert.DeserializeObject<MesRunOp>(str);
                            MesRunOp mesRunOp = new MesRunOp();
                            mesRunOp.OrderNo = runOp.OrderNo;
                            mesRunOp.OpOrder = runOp.OpOrder;
                            mesRunOp.OpNoDesc = runOp.OpNoDesc;
                            if (i==0)
                            {
                                MesResources.OrderNo = runOp.OrderNo;
                                MesResources.OpOrder = runOp.OpOrder;
                                MesResources.OpNoDesc = runOp.OpNoDesc;
                                MesResources.WriteConfig();
                            }

                            mesRecipeStruct.mesRunOps.Add(mesRunOp);
                        }
                    }
                    return true;
                }
                
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                mesRecv = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesGetRunOpList {ex.Message}";
            }
            finally
            {
                string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

                //mesSend = RevertJsonString(mesSend);
                //mesRecv = RevertJsonString(mesRecv);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{mesRecv},{linecode},{second},{second},{mesSend}";

                SaveLogData("运行中生产任务获取", text);
            }
            return false;
        }

        /// <summary>
        /// 参数获取   参数获取保存本地并显示，待沟通参数是否直接工艺参数调用
        /// </summary>
        /// <param name="lotNo">产出物编号（批次、电芯等）</param>
        /// <param name="vehicleNo">载具号（流拉筐、罐、轴）</param>
        /// <param name="opOrder">工序任务</param>
        /// <param name="workPlace">工作场地</param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool Mes_GetParam( string lotNo, string vehicleNo, string opOrder, string workPlace, ref MesRecipeStruct mesRecipeStruct, ref string errMsg)
        {
            string mesUri = "", Code = "", mesRecv = "", mesSend = "", uCaller = "DAL1HK01", msg = "";
            int result = -1;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    return true;
                }
                //if (!MachineCtrl.GetInstance().isMESConnect)
                //{
                //    return false;
                //}
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesGetParam);
                if (!cfg.enable)
                {
                    return true;
                }
                mesUri = cfg.mesUri;
                
                var body = new
                {
                    UCaller = uCaller,
                    LotNo = lotNo,
                    VehicleNo = vehicleNo,
                    OpOrder = opOrder,
                    WorkPlace = workPlace,

                };

                mesSend = JsonConvert.SerializeObject(body);
                string recvValue = httpClient.Post(mesUri, mesSend);
                mesSend = Regex.Replace(mesSend.ToString(), @"\s", "");
                mesRecv = MesOperate.RevertJsonString(recvValue);
                if (null != recvValue)
                {
                    JObject recvObj = JObject.Parse(recvValue);
                    {
                        result = Convert.ToInt32(recvObj["TransFlag"]);
                        msg = recvObj["ErrorMessage"].ToString();
                        if (result!=1)
                        {
                            errMsg = $"参数获取失败! \r\nMsg:{msg}";

                            return false;
                        }

                        // 更新工艺参数
                        JArray items1 = (JArray)recvObj["Param"];
                        mesRecipeStruct.ParamData = new List<MesParamData>();

                        //循环遍历将参数添加到集合里面
                        for (int i = 0; i < items1.Count; i++)
                        {
                            var str = recvObj["Param"][i].ToString();
                            //反序列化
                            MachineCtrl.ParameterData InfoList = JsonConvert.DeserializeObject<MachineCtrl.ParameterData>(str);
                            MesParamData paramData = new MesParamData();
                            paramData.StepID = InfoList.StepID;
                            paramData.StepName = InfoList.StepName;
                            paramData.ParamID = InfoList.ParamID;
                            paramData.ParamName = InfoList.ParamName;
                            paramData.ParamStand = InfoList.ParamStand;
                            paramData.ParamUpper = InfoList.ParamUpper;
                            paramData.ParamLower = InfoList.ParamLower;

                            mesRecipeStruct.ParamData.Add(paramData);
                            
                        }
                        return true;
                    }
                }
            }

            catch (System.Exception ex)
            {
                Code = "-2";
                mesRecv = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesGetParam {ex.Message}";
            }
            finally
            {
                string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

                //mesSend = RevertJsonString(mesSend);
                //mesRecv = RevertJsonString(mesRecv);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{mesRecv},{linecode},{second},{second},{mesSend}";

                SaveLogData("参数获取", text);
            }
            return false;
        }

        /// <summary>
        /// 007 任务产出时加工参数上报 电芯出站时上传工序任务和实际参数  待加入业务逻辑里面
        /// </summary>
        /// <param name="lotNo">产出物编号（批次、电芯、block、模组、系统等）</param>
        /// <param name="vehicleNo">流拉筐、蓝胶码、卷轴号</param>
        /// <param name="workPlace">工作场地</param>
        /// <param name="opOrder">工序任务</param>
        /// <param name="opName">工序名称</param>
        /// <param name="param">参数</param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool Mes_ReportParam(string lotNo,  string workPlace,  string opName, object[] Parameters, ref string errMsg)
        {
            string mesUri = "", Code = "", mesRecv = "", mesSend = "", uCaller = "DAL1HK01", vehicleNo = "",opOrder="", msg = "";
            int result = -1;
            DateTime startTime = DateTime.Now;
            try
            {
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    return true;
                }
                //if (!MachineCtrl.GetInstance().isMESConnect)
                //{
                //    return false;
                //}
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesReportParam);
                if (!cfg.enable)
                {
                    return true;
                }
                mesUri = cfg.mesUri;

                var body = new
                {
                    UCaller = uCaller,
                    LotNo = lotNo,
                    VehicleNo = vehicleNo,
                    WorkPlace = workPlace,
                    OpOrder = opOrder,
                    OpName = opName,
                    Parm = Parameters 
                };

                mesSend = JsonConvert.SerializeObject(body);
                string recvValue = httpClient.Post(mesUri, mesSend);
                mesSend = Regex.Replace(mesSend.ToString(), @"\s", "");
                mesRecv = MesOperate.RevertJsonString(recvValue);
                JObject recvObj = JObject.Parse(recvValue);

                if (recvObj != null)
                {
                    result = Convert.ToInt32(recvObj["TransFlag"]);
                    msg = recvObj["ErrorMessage"].ToString();
                    if (result != 1)
                    {
                        errMsg = $"任务产出时加工参数上报! \r\nMsg:{msg}";

                        return false;
                    }
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Code = "-2";
                mesRecv = ex.Message;
                errMsg = $"Code:-2, Msg:EquToMesReportParam {ex.Message}";
            }
            finally
            {
                string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

                //mesSend = RevertJsonString(mesSend);
                //mesRecv = RevertJsonString(mesRecv);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{mesRecv},{linecode},{second},{second},{mesSend}";

                SaveLogData("任务产出时加工参数上报", text);
            }
            return false;
        }
        /// <summary>
        /// 开机开工上报
        /// </summary>
        /// <param name="workPlace"></param>
        /// <param name="errMsg"></param>
        /// <returns></returns>
        public static bool Mes_StertCheck(string workPlace, ref string errMsg)
        {
            string mesUri = "", Code = "", mesRecv = "", mesSend = "", uCaller = "DAL1HK01", msg = "";
            int result = -1;
            UserLogin user = new UserLogin();
            DateTime startTime = DateTime.Now;
            try
            {
                if (!MachineCtrl.GetInstance().UpdataMes)
                {
                    return true;
                }
                //if (!MachineCtrl.GetInstance().isMESConnect)
                //{
                //    return false;
                //}
                var cfg = MesDefine.GetMesCfg(MesInterface.EquToMesStartCheck);
                if (!cfg.enable)
                {
                    return true;
                }
                mesUri = cfg.mesUri;

                var body = new
                {
                    UCaller = uCaller,
                    UserCode = MesResources.Equipment.OperatorUserID,
                    WorkPlace = workPlace
                };

                mesSend = JsonConvert.SerializeObject(body);
                string recvValue = httpClient.Post(mesUri, mesSend);
                mesSend = Regex.Replace(mesSend.ToString(), @"\s", "");
                mesSend = MesOperate.RevertJsonString(mesSend);
                mesRecv = MesOperate.RevertJsonString(recvValue);
                JObject recvObj = JObject.Parse(recvValue);

                if(recvObj != null)
                {
                    result = Convert.ToInt32(recvObj["TransFlag"]);
                    msg = recvObj["ErrorMessage"].ToString();
                    if (result != 1)
                    {
                        errMsg = $"开机验证失败，不允许开机! \r\nMsg:{msg}";
                        return false;
                    }
                    return true;
                }

            }
            catch(Exception ex)
            {
                Code = "-2";
                mesRecv = ex.Message;
                errMsg = $"Code:-2, Msg:StartCheck {ex.Message}";
            }
            finally
            {
                string second = $"{(DateTime.Now - startTime).TotalSeconds} S";

                //mesSend = RevertJsonString(mesSend);
                //mesRecv = RevertJsonString(mesRecv);

                string sfcode = " ";
                string linecode = " ";
                string text = $"{sfcode},{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},{mesUri},{second},{Code},{mesRecv},{linecode},{second},{second},{mesSend}";

                SaveLogData("开机验证", text);
            }
            return false;
        }
    }
}
