using HelperLibrary;
using Machine.Framework;
using Machine.Framework.Mes;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Machine
{
    #region // MES定义设备状态

    /// <summary>
    /// MES定义设备状态
    /// </summary>
    public enum MesMCState
    {
        Invalid = 0,    //无效的
        Running = 1,    // 自动运行
        Manual,         // 手动运行
        Alram,        // 离线
        Fault,          // 故障
        Stop,           // 停机

        Other,
    }
    #endregion

    #region // MES定义报警状态

    /// <summary>
    /// MES定义报警状态
    /// </summary>
    public enum MesAlarmStatus
    {
        Restore = 0,    // 恢复
        Happen = 1,     // 发生
    }
    #endregion

    #region // MES定义报警状态

    /// <summary>
    /// MES定义报警等级
    /// </summary>
    public enum MesAlarmLevel
    {
        Zero,    // 零级报警
        One,     // 一级报警
        Two      //二级报警
    }
    #endregion

    #region // MES定义停机原因

    /// <summary>
    /// MES定义停机原因
    /// </summary>
    public enum EquDownReason
    {
        Waiting = 1,        // 1：待料;
        Eat,                // 2：吃饭;
        Remodel,            // 3：换型;
        Fault,              // 4：设备故障;
        MaterialAdverse,    // 5：来料不良;
        EquipmentCheck,     // 6：设备校验;
        Tally,              // 7：首件/点检;
        QualityAbnormal,    // 8：品质异常;
        Stacking,           // 9：堆料;
        EnviromentAbnormal, // 10：环境异常;
        DeviceInfoLack,     // 11：设备信息不完善;

        End,
    }
    #endregion

    #region // MES接口

    /// <summary>
    /// MES接口
    /// </summary>
    public enum MesInterface
    {
        //EquToMesEquipmentOnLine,        // 设备联机请求
        //EquToMesHeartbeat,              // 设备在线检测
        //EquToMesOperator,               // 获取人员信息
        EquToMesStartCheck,             //效验开机请求  --捷威MES
        EquToMesCheckUser,              // 校验用户数据 ——捷威MES
        EquToMesGetVehicleInfo,         // 流拉筐信息获取 ——捷威MES
        EquToMesReportParam,            // 任务产出时加工参数上报 ——捷威MES
        EquToMesWorkPlaceStatus,        // 设备状态变更 ——捷威MES
        EquToMesReportSN,               // 产品产出时加工参数上报 ——捷威MES
        EquToMesLogoutOp,               // 任务中断或关闭 ——捷威MES
        EquToMesGetRunOpList,           // 运行中生产任务获取 ——捷威MES
        EquToMesGetParam,               // 参数获取 ——捷威MES
        //EquToMesLogin,                  // 用户登录
        //EquToMesInBaking,               // 入站校验
        //EquToMesOutBaking,              // 出站校验
        //EquToMesState,                  // 设备状态变更
        EquToMesGetRecipe,              // 设备主动获取参数
        //EquToMesAlarm,                  // 设备报警采集
        EquToMesBindingOrUnBind,        // 托盘绑定与解绑
        EquToMesParameterInitialize,    // 设备初始化(参数下发)
        //EquToMesDownReason,           // 设备停机采集
        EquToMesRecipeListGet,          // 获取开机参数列表：此接口保存所有配方参数
        EquToMesRecipeGet,              // 获取开机参数明细：此接口无配方参数
        EquToMesRecipeVExamine,         // 开机参数版本校验：此接口无配方参数
        EquToMesRecipe,                 // 开机参数采集
        //EquToMesCheckSfc,               // 电芯校验并绑托盘：电芯校验
        //EquToMesBindContainer,          // 电芯校验并绑托盘：托盘绑定
        //EquToMesUnBindContainer,        // 电芯校验并绑托盘：托盘解绑
        //EquToMesInBaking,               // 电芯校验并绑托盘：满托盘进站
        //EquToMesOutBaking,              // 水含量测试上传并出站，解绑：出站解绑
        //EquToMesGetContainer,           // 电芯烘烤-获取托盘明细接口
        //EquToIOTServer,                 // IOT数据采集
        End,
    }
    #endregion

    #region // MES下发或上载参数

    /// <summary>
    /// MES下发或上载配方结构
    /// </summary>
    public struct MesRecipeStruct
    {
        /// <summary>
        /// 配方编码
        /// </summary>
        public string RecipeCode;               // 配方编码
        public string OprSequenceNo;            // 工序编号
        public string Version;                  // 版本
        /// <summary>
        /// 产品编码
        /// </summary>
        public string ProductCode;              // 产品编码
        public string LastUpdateOnTime;         // 最后更新时间

        public List<MesParameterData> Param;    // 参数

        public List<MesParamData> ParamData;    // Jeve 参数

        public List<MesRunOp> mesRunOps;         // Jeve 生产任务
    }



    /// <summary>
    /// 参数信息数据
    /// </summary>
    public struct MesParameterData
    {
        public string ParameterCode;          //参数编码
        public string Description;        //参数描述
        public string TargetValue;         //推荐值    
        public string UpperControlLimit;       //参数上限
        public string LowerControlLimit;      //参数下限
        public string ParameterType;      //参数类型
        public string UomCode;      //单位
    }

    /// <summary>
    /// 捷威 参数信息数据
    /// </summary>
    public struct MesParamData
    {
        public string StepID;          //步骤代码
        public string StepName;        //步骤名
        public string ParamID;         //参数代码    
        public string ParamName;       //参数名
        public string ParamStand;      //参数标准值
        public string ParamUpper;      //参数上限
        public string ParamLower;      //参数下限
    }

    /// <summary>
    /// 捷威 生产任务信息
    /// </summary>
    public struct MesRunOp
    {
        public string OrderNo;          //订单号
        public string OpOrder;          //工序任务
        public string OpNoDesc;         //工序描述    
    }

    #endregion

    #region // Mes配置

    /// <summary>
    /// Mes配置
    /// </summary>
    public class MesConfig
    {
        public bool enable;                                           // 接口启用状态
        public string mesUri;                                         // 地址
        public Dictionary<string, MesRecipeStruct> recipe;            // 参数配方集：<string配方代码, 配方>
        public string send;                                           // 发送数据
        public string recv;                                           // 接收数据
        public bool updataRS;                                         // 收发数据已更新

        public MesConfig()
        {
            this.recipe = new Dictionary<string, MesRecipeStruct>();
            Clear();
        }

        public void Clear()
        {
            enable = true;
            mesUri = string.Empty;
            recipe.Clear();
            send = "";
            recv = "";
            updataRS = false;
        }

        public void Copy(MesConfig mesCfg)
        {
            this.enable = mesCfg.enable;
            this.mesUri = mesCfg.mesUri;
            this.recipe.Clear();
            foreach(var item in mesCfg.recipe)
            {
                this.recipe.Add(item.Key, item.Value);
            }
        }

        public void SetMesInfo(string mesSendData, string mesRecvData)
        {
            if (this.send != mesSendData)
            {
                this.send = mesSendData;
            }

            this.recv = mesRecvData;
        }
    }
    #endregion

    #region // Mes配置参数定义

    /// <summary>
    /// Mes配置参数定义
    /// </summary>
    public class MesDefine
    {
        private static string[] MesTitle;
        private static MesConfig[] MesCfg;

        public static MesConfig GetMesCfg(MesInterface mes)
        {
            if (null == MesCfg)
            {
                MesCfg = new MesConfig[(int)MesInterface.End];
                for(int i = 0; i < MesCfg.Length; i++)
                {
                    MesCfg[i] = new MesConfig();
                }
            }
            return MesCfg[(int)mes];
        }

        public static void ReadConfig(MesInterface mes)
        {
            MesConfig mesCfg = GetMesCfg(mes);
            if (null == mesCfg)
            {
                return;
            }
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = mes.ToString();

            List<string> paramList = new List<string>();
            paramList.Add(nameof(mesCfg.enable));
            /*mesCfg.enable = true;*/
            IniFile.ReadBool(section, nameof(mesCfg.enable), mesCfg.enable, file);
            paramList.Add(nameof(mesCfg.mesUri));
            mesCfg.mesUri = IniFile.ReadString(section, nameof(mesCfg.mesUri), "", file);

            // 搜索包含的子节点
            string[] sectionKV = IniFile.ReadAllItems(section, file);
            if (sectionKV.Length > 0)
            {
                foreach(var item in sectionKV)
                {
                    string[] kv = item.Split((new char[] { '=' }), StringSplitOptions.RemoveEmptyEntries);
                    if(kv.Length < 1)
                        continue;
                    kv = kv[0].Split('.');
                    if(kv.Length < 1)
                        continue;
                    if (paramList.Contains(kv[0]))
                        continue;
                    paramList.Add(kv[0]);
                    section = kv[0];
                    string[] subSectionKV = IniFile.ReadAllItems(section, file);
                    if (subSectionKV.Length > 0)
                    {
                        MesRecipeStruct mesRecipe = new MesRecipeStruct();
                        mesRecipe.RecipeCode = kv[0];
                        key = "ProductCode";
                        paramList.Add(key);
                        mesRecipe.ProductCode = IniFile.ReadString(section, key, "", file);
                        key = "Version";
                        paramList.Add(key);
                        mesRecipe.Version = IniFile.ReadString(section, key, "", file);
                        key = "OprSequenceNo";
                        paramList.Add(key);
                        mesRecipe.OprSequenceNo = IniFile.ReadString(section, key, "", file);
                        key = "LastUpdateOnTime";
                        paramList.Add(key);
                        mesRecipe.LastUpdateOnTime = IniFile.ReadString(section, "LastUpdateOnTime", "", file);

                        // 参数项解析
                        mesRecipe.ParamData = new List<MesParamData>();
                        foreach(var subItem in subSectionKV)
                        {
                            kv = subItem.Split((new char[] { '=' }), StringSplitOptions.RemoveEmptyEntries);
                            if(kv.Length < 1)
                                continue;
                            kv = kv[0].Split('.');
                            if(kv.Length < 1)
                                continue;
                            if(paramList.Contains(kv[0]))
                                continue;
                            paramList.Add(kv[0]);
                            MesParamData param = new MesParamData();
                            param.StepID = kv[0];
                            key = $"{param.StepID}.StepName";
                            param.StepName = IniFile.ReadString(section, key, "", file);
                            key = $"{param.StepID}.ParamID";
                            param.ParamID = IniFile.ReadString(section, key, "", file);
                            key = $"{param.StepID}.ParamName";
                            param.ParamName = IniFile.ReadString(section, key, "", file);
                            key = $"{param.StepID}.ParamStand";
                            param.ParamStand = IniFile.ReadString(section, key, "", file);
                            key = $"{param.StepID}.ParamUpper";
                            param.ParamUpper = IniFile.ReadString(section, key, "", file);
                            key = $"{param.StepID}.ParamLower";
                            param.ParamLower = IniFile.ReadString(section, key, "", file);
                            mesRecipe.ParamData.Add(param);
                        }
                        mesCfg.recipe.Add(mesRecipe.RecipeCode, mesRecipe);
                    }
                }
            }
        }

        public static void WriteConfig(MesInterface mes)
        {
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = mes.ToString();

            MesConfig mesCfg = GetMesCfg(mes);

            IniFile.EmptySection(section, file);

            IniFile.WriteBool(section, nameof(mesCfg.enable), mesCfg.enable, file);
            IniFile.WriteString(section, nameof(mesCfg.mesUri), mesCfg.mesUri, file);
        }

        public static void WriteConfig(MesInterface mes, ref MesRecipeStruct mesRecipeStruct)
        {
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = mes.ToString();

            MesConfig mesCfg = GetMesCfg(mes);

            //IniFile.EmptySection(section, file);
            //IniFile.EmptySection("ParameterInfoOld", file);

            //IniFile.WriteBool(section, nameof(mesCfg.enable), mesCfg.enable, file);
            //IniFile.WriteString(section, nameof(mesCfg.mesUri), mesCfg.mesUri, file);

            //IniFile.WriteString("ParameterInfoOld", "ProductCode", productCode, file);
            //IniFile.WriteString("ParameterInfoOld", "Version", version, file);
            //IniFile.WriteString("ParameterInfoOld", "OprSequenceNo", oprSequenceNo, file);
            //IniFile.WriteString("ParameterInfoOld", "LastUpdateOnTime", DateTime.Now.ToString(), file);

            foreach (var item in mesCfg.recipe)
            {
                // 每种配方一个节
                IniFile.WriteString(section, item.Key, item.Key, file);

                if (null != item.Value.Param)
                {

                    mesRecipeStruct = new MesRecipeStruct();
                    //mesRecipeStruct.RecipeCode = productCode;
                    //mesRecipeStruct.Version = version;
                    //mesRecipeStruct.OprSequenceNo = oprSequenceNo;
                    string msg = "";
                    if (!MachineCtrl.GetInstance().ACEQPTPARM_Main(MesResources.Equipment, ref mesRecipeStruct, ref msg))
                    {
                        ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
                    }
                    //List<MesParameterData> list = mesRecipeStruct.Param.Sort(mesRecipeStruct.Param);
                    foreach (var param in mesRecipeStruct.ParamData)
                    {
                        key = $"{param.StepID}.StepName";
                        IniFile.WriteString(item.Key, key, param.StepName, file);
                        key = $"{param.StepID}.ParamID";
                        IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.ParamID) ? "" : param.ParamID, file);
                        key = $"{param.StepID}.ParamName";
                        IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.ParamName) ? "" : param.ParamName, file);
                        key = $"{param.StepID}.ParamStand";
                        IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.ParamStand) ? "" : param.ParamStand, file);
                        key = $"{param.StepID}.ParamUpper";
                        IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.ParamUpper) ? "" : param.ParamUpper, file);
                        key = $"{param.StepID}.ParamLower";
                        IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.ParamLower) ? "" : param.ParamLower, file);

                    }
                }
            }
        }

        //public static void WriteConfig(MesInterface mes,string productCode,string version,string oprSequenceNo,ref MesRecipeStruct mesRecipeStruct)
        //{
        //    string file, section, key;
        //    file = Def.GetAbsPathName(Def.MesParameterCfg);
        //    section = mes.ToString();

        //    MesConfig mesCfg = GetMesCfg(mes);

        //    //IniFile.EmptySection(section, file);
        //    //IniFile.EmptySection("ParameterInfoOld", file);

        //    //IniFile.WriteBool(section, nameof(mesCfg.enable), mesCfg.enable, file);
        //    //IniFile.WriteString(section, nameof(mesCfg.mesUri), mesCfg.mesUri, file);

        //    IniFile.WriteString("ParameterInfoOld", "ProductCode", productCode, file);
        //    IniFile.WriteString("ParameterInfoOld", "Version", version, file);
        //    IniFile.WriteString("ParameterInfoOld", "OprSequenceNo", oprSequenceNo, file);
        //    IniFile.WriteString("ParameterInfoOld", "LastUpdateOnTime", DateTime.Now.ToString(), file);

        //    foreach (var item in mesCfg.recipe)
        //    {
        //        // 每种配方一个节
        //        IniFile.WriteString(section, item.Key, item.Key, file);

        //        if (null != item.Value.Param)
        //        {

        //            mesRecipeStruct = new MesRecipeStruct();
        //            mesRecipeStruct.RecipeCode = productCode;
        //            mesRecipeStruct.Version = version;
        //            mesRecipeStruct.OprSequenceNo = oprSequenceNo;
        //            string msg = "";
        //            if (!MachineCtrl.GetInstance().ACEQPTPARM_Main(MesResources.Equipment, ref mesRecipeStruct, ref msg))
        //            {
        //                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
        //            }
        //            //List<MesParameterData> list = mesRecipeStruct.Param.Sort(mesRecipeStruct.Param);
        //            foreach (var param in mesRecipeStruct.Param)
        //            {
        //                key = $"{param.ParameterCode}.ParameterType";
        //                IniFile.WriteString(item.Key, key, param.ParameterType, file);
        //                key = $"{param.ParameterCode}.TargetValue";
        //                IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.TargetValue) ? "" : param.TargetValue, file);
        //                key = $"{param.ParameterCode}.UomCode";
        //                IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.UomCode) ? "" : param.UomCode, file);
        //                key = $"{param.ParameterCode}.UpperControlLimit";
        //                IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.UpperControlLimit) ? "" : param.UpperControlLimit, file);
        //                key = $"{param.ParameterCode}.LowerControlLimit";
        //                IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.LowerControlLimit) ? "" : param.LowerControlLimit, file);
        //                key = $"{param.ParameterCode}.Description";
        //                IniFile.WriteString(item.Key, key, string.IsNullOrEmpty(param.Description) ? "" : param.Description, file);

        //            }
        //        }
        //    }
        //}

        public static string GetMesTitle(MesInterface mes)
        {
            if (null == MesTitle)
            {
                MesTitle = new string[(int)MesInterface.End]
                {
                    //"设备联机请求",
                    //"设备在线检测",
                    //"上位机获取人员信息",
                    "开机请求",
                    "校验用户数据",
                    "流拉筐信息获取",
                    "任务产出时加工参数上报",
                    "设备状态变更",
                    "产品产出时加工参数上报",
                    "任务中断或关闭",
                    "运行中生产任务获取",
                    "参数获取",
                    //"用户登录",
                    //"入站校验",
                    //"出站校验",
                    //"设备状态变更",
                    "设备主动获取参数",
                    //"设备报警采集",
                    //"设备停机采集",
                    "托盘绑定与解绑",
                    "设备初始化(参数下发)",
                    "获取开机参数列表",
                    "获取开机参数明细",
                    "开机参数版本校验",
                    "开机参数采集",
                    //"入站校验",
                    //"托盘绑定与解绑",
                    //"托盘解绑",
                    //"满托盘进站",
                    //"出站解绑",
                    //"获取托盘明细",
                    //"IOT数据采集",
                };
            }
            return MesTitle[(int)mes];
        }

        //保存生产任务
        public static void GetRunOpList(string workplace,string oporder, MesRecipeStruct mesRecipeStruct)
        {
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "MesResources";
            //IniFile.EmptySection(section, file);

            string runoplist = "";
            int num = 1;
            string msg = "";
            if (Jeve_Mes.Mes_GetRunOpList(workplace, oporder, ref mesRecipeStruct, ref msg))
            {
                foreach (var item in mesRecipeStruct.mesRunOps)
                {
                    runoplist += string.Format(@"{0}>订单号：{1},工序任务：{2},工序描述：{3}\r\n", num, item.OrderNo, item.OpOrder, item.OpNoDesc);
                    num++;
                }
                MessageBox.Show(runoplist);
            }
            else
            {
                ShowMsgBox.ShowDialog($"{msg}", MessageType.MsgAlarm);
            }
        }

        //public static void WriteRunOpList()
        //{
        //    string file, section, key;
        //    file = Def.GetAbsPathName(Def.MesParameterCfg);
        //    section = "RunOpList";

        //    IniFile.EmptySection(section, file);
        //    key = $"BillNo";
        //    IniFile.WriteString(section, key, BillNo, file);
        //    key = $"BillNum";
        //    IniFile.WriteString(section, key, BillNum, file);
        //}



    }
    #endregion

    

    #region // Mes接口Log

    /// <summary>
    /// Mes接口Log
    /// </summary>
    public static class MesLog
    {
        private static LogFile[] mesLog;

        private static LogFile GetLogFile(MesInterface mes)
        {
            if(null == mesLog)
            {
                mesLog = new LogFile[(int)MesInterface.End];
                for(int i = 0; i < mesLog.Length; i++)
                {
                    mesLog[i] = new LogFile();
                    mesLog[i].SetFileInfo(Def.GetAbsPathName($"Log\\MES\\{(MesInterface)i}\\"), 2, 30);
                }
            }
            return mesLog[(int)mes];
        }

        public static void SetFileInfo(MesInterface mes, string filePath, long size, int storageLife)
        {
            GetLogFile(mes).SetFileInfo(filePath, size, storageLife);
        }

        public static void WriteLog(MesInterface mes, string msgText, LogType msgType = LogType.Error)
        {
            GetLogFile(mes).WriteLog(DateTime.Now, mes.ToString(), msgText, msgType);
        }
    }

    #endregion

    #region // 作业班次

    /// <summary>
    /// 班次包含信息
    /// </summary>
    public struct ShiftStruct
    {
        public string Code;
        public string Name;
        public DateTime Start;
        public DateTime End;
    }

    /// <summary>
    /// 作业班次
    /// </summary>
    public static class OperationShifts
    {
        public static List<ShiftStruct> Shifts;
        private static List<string> ShiftsTime;

        /// <summary>
        /// 获取班次信息
        /// </summary>
        /// <returns></returns>
        public static ShiftStruct Shift()
        {
            ShiftStruct sfift = new ShiftStruct();
            DateTime dt = DateTime.Now;
            foreach(var item in Shifts)
            {
                if(item.Start.Hour <= item.End.Hour)
                {
                    if((item.Start.TimeOfDay <= dt.TimeOfDay) && (dt.TimeOfDay <= (item.End.AddSeconds(1).TimeOfDay)))
                    {
                        sfift = item;
                        break;
                    }
                }
                else
                {
                    if((item.Start.TimeOfDay <= dt.TimeOfDay) || (dt.TimeOfDay <= item.End.AddSeconds(1).TimeOfDay))
                    {
                        sfift = item;
                        break;
                    }
                }
            }
            return sfift;
        }

        /// <summary>
        /// 是否换班
        /// </summary>
        /// <returns></returns>
        public static bool ChangeShift()
        {
            if (ShiftsTime.Contains(DateTime.Now.ToString("HH:mm:ss")))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 读取班次配置
        /// </summary>
        public static void ReadConfig()
        {
            if(null == Shifts)
            {
                Shifts = new List<ShiftStruct>();
                ShiftsTime = new List<string>();
            }
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "Shifts";

            int idx = 0;
            ShiftStruct shift = new ShiftStruct();
            while(true)
            {
                shift.Code = IniFile.ReadString(section, "Code" + idx, "", file);
                if("" == shift.Code)
                {
                    break;
                }
                shift.Name = IniFile.ReadString(section, "Name" + idx, "", file);
                key = IniFile.ReadString(section, "Start" + idx, "", file);
                DateTime.TryParse(key, out shift.Start);
                ShiftsTime.Add(shift.Start.ToString("HH:mm:ss"));
                key = IniFile.ReadString(section, "End" + idx, "", file);
                DateTime.TryParse(key, out shift.End);
                ShiftsTime.Add(shift.End.ToString("HH:mm:ss"));

                Shifts.Add(shift);
                idx++;
            }
        }

        /// <summary>
        /// 保存班次配置
        /// </summary>
        public static void WriteConfig()
        {
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "Shifts";

            IniFile.EmptySection(section, file);
            ShiftsTime.Clear();

            for(int i = 0; i < Shifts.Count; i++)
            {
                IniFile.WriteString(section, "Code" + i, Shifts[i].Code, file);
                IniFile.WriteString(section, "Name" + i, Shifts[i].Name, file);
                key = Shifts[i].Start.ToString("HH:mm:ss");
                IniFile.WriteString(section, "Start" + i, key, file);
                ShiftsTime.Add(key);
                key = Shifts[i].End.ToString("HH:mm:ss");
                IniFile.WriteString(section, "End" + i, key, file);
                ShiftsTime.Add(key);
            }
        }
    }

    #endregion

    #region // Mes资源参数

    /// <summary>
    /// Mes资源参数包含信息
    /// </summary>
    public struct ResourcesStruct
    {
        public string MesRecipeCode { get; set; }           // MES配方
        public string EquipmentCode { get; set; }           // 设备编码
        public string ResourceCode { get; set; }            // 资源编码
        public string ProcessCode { get; set; }             // 工序编码
        public string MesURL { get; set; }                  // MESURL
        public string OperatorUserID { get; set; }          // 操作员账号
        public string OperatorPassword { get; set; }        // 操作员密码
    }

    public struct UserInfo
    {
        public string UserCode { get; set; }           // 工号
        public string UserName { get; set; }            // 姓名
        public string UserArea { get; set; }            // 区域
        public string UserDep { get; set; }            // 部门
    }

    /// <summary>
    /// Mes资源参数
    /// </summary>
    public static class MesResources
    {
        public static ResourcesStruct Equipment;            // 组资源信息

        //public static OvenResourcesStruct OvenEquipment;    // 炉子资源信息

        public static int HeartbeatInterval;                // 心跳时间间隔：秒s
        public static int StopUpdataInterval;               // 停机上报时间间隔：秒s

        public static bool MesLogin;                        // MES已登录

        public static string OrderNo;                            // 工单号
        public static string OpOrder;                           // 工单号包含的工单数量
        public static string OpNoDesc;                           // 工单号包含的工单数量

        /// <summary>
        /// 读取Mes资源参数配置
        /// </summary>
        public static void ReadConfig()
        {
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "MesResources";

            key = $"Equipment.EquipmentCode";
            Equipment.EquipmentCode = IniFile.ReadString(section, key, "", file);
            key = $"Equipment.ResourceCode";
            Equipment.ResourceCode = IniFile.ReadString(section, key, "", file);
            key = $"Equipment.ProcessCode";
            Equipment.ProcessCode = IniFile.ReadString(section, key, "", file);
            key = $"Equipment.MesURL";
            Equipment.MesURL = IniFile.ReadString(section, key, "", file);
            key = $"Equipment.OperatorUserID";
            Equipment.OperatorUserID = IniFile.ReadString(section, key, "", file);
            key = $"Equipment.OperatorPassword";
            Equipment.OperatorPassword = IniFile.ReadString(section, key, "", file);

            key = $"HeartbeatInterval";
            HeartbeatInterval = IniFile.ReadInt(section, key, 10, file);
            key = $"StopUpdataInterval";
            StopUpdataInterval = IniFile.ReadInt(section, key, 180, file);
            key = $"Equipment.MesRecipeCode";
            Equipment.MesRecipeCode = IniFile.ReadString(section, key, "", file);

            key = $"OrderNo";
            OrderNo = IniFile.ReadString(section, key, "", file);
            key = $"OpOrder";
            OpOrder = IniFile.ReadString(section, key, "", file);
            key = $"OpNoDesc";
            OpNoDesc = IniFile.ReadString(section, key, "", file);

            MesLogin = false;
        }

        /// <summary>
        /// 保存Mes资源参数配置
        /// </summary>
        public static void WriteConfig()
        {
            string file, section, key;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "MesResources";

            IniFile.EmptySection(section, file);

            string[] resStr = new string[] { "Equipment" };
            ResourcesStruct[] resStruct = new ResourcesStruct[] { Equipment };
            for(int i = 0; i < resStruct.Length; i++)
            {
                key = $"{resStr[i]}.EquipmentCode";
                IniFile.WriteString(section, key, resStruct[i].EquipmentCode, file);
                key = $"{resStr[i]}.ResourceCode";
                IniFile.WriteString(section, key, resStruct[i].ResourceCode, file);
                key = $"{resStr[i]}.ProcessCode";
                IniFile.WriteString(section, key, resStruct[i].ProcessCode, file);
                key = $"{resStr[i]}.MesURL";
                IniFile.WriteString(section, key, resStruct[i].MesURL, file);
                key = $"{resStr[i]}.OperatorUserID";
                IniFile.WriteString(section, key, resStruct[i].OperatorUserID, file);
                key = $"{resStr[i]}.OperatorPassword";
                IniFile.WriteString(section, key, resStruct[i].OperatorPassword, file);
                key = $"{resStr[i]}.MesRecipeCode";
                IniFile.WriteString(section, key, resStruct[i].MesRecipeCode, file);
            }
            key = $"HeartbeatInterval";
            IniFile.WriteInt(section, key, HeartbeatInterval, file);

            key = $"OrderNo";
            IniFile.WriteString(section, key, OrderNo, file);
            key = $"OpOrder";
            IniFile.WriteString(section, key, OpOrder, file);
            key = $"OpNoDesc";
            IniFile.WriteString(section, key, OpNoDesc, file);
        }


    }
    #endregion

    #region // FTP配置参数定义

    /// <summary>
    /// FTP配置参数定义
    /// </summary>
    public static class FTPDefine
    {
        public static string FilePath;        // FTP文件路径
        public static string User;            // FTP用户名
        public static string Password;        // FTP密码

        public static void ReadConfig()
        {
            string file, section;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "FTPClient";

            FilePath = IniFile.ReadString(section, nameof(FilePath), "", file);
            User = IniFile.ReadString(section, nameof(User), "", file);
            Password = IniFile.ReadString(section, nameof(Password), "", file);
        }

        public static void WriteConfig()
        {
            string file, section;
            file = Def.GetAbsPathName(Def.MesParameterCfg);
            section = "FTPClient";

            IniFile.EmptySection(section, file);

            IniFile.WriteString(section, nameof(FilePath), FilePath, file);
            IniFile.WriteString(section, nameof(User), User, file);
            IniFile.WriteString(section, nameof(Password), Password, file);
        }

    }
    #endregion

}
