using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine.Framework
{
    public class Param
    {
        public string[,] listArray = new string [24,6];
        public void getParam(CavityParameter cavityParameter,
                            string bakingCount,string pos,string start, string end,
                            double[] water,ref object [] Parameters)
        {
            MesConfig cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeGet);
            cfg = MesDefine.GetMesCfg(MesInterface.EquToMesRecipeListGet);
            foreach (var item in cfg.recipe.Values)
            {
                for (int i = 0; i < item.Param.Count; i++)
                {
                    listArray[i,0] = item.Param[i].ParameterCode.ToString();
                    listArray[i,1] = item.Param[i].Description.ToString();
                    listArray[i,2] = item.Param[i].TargetValue.ToString();
                    listArray[i,3] = item.Param[i].UpperControlLimit.ToString();
                    listArray[i,4] = item.Param[i].LowerControlLimit.ToString();
                    switch (item.Param[i].ParameterCode)
                    {
                        case "ZHK211-01":
                            listArray[i, 5] = cavityParameter.TempUpperlimit.ToString();
                            break;
                        case "ZHK211-02":
                            listArray[i, 5] = cavityParameter.TempLowerlimit.ToString();
                            break;
                        case "ZHK211-03":
                            listArray[i, 5] = cavityParameter.SetTempValue.ToString();
                            break;
                        case "ZHK211-04":
                            listArray[i, 5] = cavityParameter.PreheatTime.ToString();
                            break;
                        case "ZHK211-05":
                            listArray[i, 5] = cavityParameter.VacHeatTime.ToString();
                            break;
                        case "ZHK211-06":
                            listArray[i, 5] = cavityParameter.AStateVacTime.ToString();
                            break;
                        case "ZHK211-07":
                            listArray[i, 5] = cavityParameter.AStateVacPressure.ToString();
                            break;
                        case "ZHK211-08":
                            listArray[i, 5] = cavityParameter.BStateVacTime.ToString();
                            break;
                        case "ZHK211-09":
                            listArray[i, 5] = cavityParameter.BStateVacPressure.ToString();
                            break;
                        //case "ZHK211-10":
                        //    listArray[i, 5] = cavityParameter.BStateBlowAirTime.ToString();
                        //    break;
                        case "ZHK211-11":
                            listArray[i, 5] = cavityParameter.BStateBlowAirPressure.ToString();
                            break;
                        case "ZHK211-12":
                            listArray[i, 5] = cavityParameter.BStateBlowAirKeepTime.ToString();
                            break;
                        //case "ZHK211-13":
                        //    listArray[i, 5] = cavityParameter.BreathTimeInterval.ToString();
                        //    break;
                        case "ZHK211-14":
                            listArray[i, 5] = cavityParameter.BreathCycleTimes.ToString();
                            break;
                        case "ZHK211-15":
                            listArray[i, 5] = cavityParameter.HeatPreVacTime.ToString();
                            break;
                        case "ZHK211-16":
                            listArray[i, 5] = cavityParameter.HeatPreBlow.ToString();
                            break;
                        case "ZHK211-18":
                            listArray[i, 5] = cavityParameter.AStateVacMaxValue.ToString();
                            break;
                        case "ZHK211-20":
                            listArray[i, 5] = water[0].ToString();
                            break;
                        case "ZHK211-21":
                            listArray[i, 5] = water[1].ToString();
                            break;
                        case "ZHK211-22":
                            listArray[i, 5] = "";
                            break;
                        case "ZHK211-23":
                            listArray[i, 5] = bakingCount;
                            break;
                        case "ZHK211-24":
                            listArray[i, 5] = pos;
                            break;
                        case "ZHK211-25":
                            listArray[i, 5] = start;
                            break;
                        case "ZHK211-26":
                            listArray[i, 5] = end;
                            break;
                        default:
                            break;
                    }
                };
            }
                Parameters = new object[24];
                for (int i = 0; i < Parameters.Length; i++)
                {
                    Parameters[i] = new
                    {
                        ParamterCode = listArray[i,0].ToString(),  //参数编码
                        Location = "",
                        Value = listArray[i, 5].ToString(),
                        ParameterDescription = listArray[i, 1].ToString(),
                        UpperLimit = listArray[i, 3].ToString(),
                        LowerLimit = listArray[i, 4].ToString(),
                        TargetValue = listArray[i, 2].ToString(),  //推荐值
                        ParameterResult = "",
                        DefectCode = "",
                        ParameterMessage = "成功",
                        StepSequenceNo = "",
                    };
                }
        }

        private static string[] paramName = new string[3]
                {
                    "水含量",
                    "真空值",
                    "温度设定值",
                };
        public string[,] arrayList = new string[paramName.Count(), 1];


        //获取实际参数并上传
        public void getMesParam(CavityParameter cavityParameter, double[] water, ref object[] Parameters)
        {
            Parameters = new object[3];
            for (int i = 0; i < Parameters.Length; i++)
            {
                switch (paramName[i].ToString())
                {
                    case "水含量":
                        listArray[i, 1] = water[0].ToString();
                        break;
                    case "真空值":
                        listArray[i, 1] = cavityParameter.BStateVacPressure.ToString();
                        break;
                    case "温度设定值":
                        listArray[i, 1] = cavityParameter.SetTempValue.ToString();
                        break;
                }
            }
            for (int i = 0; i < Parameters.Length; i++)
            {
                Parameters[i] = new
                {
                    StepID = "",  //参数编码
                    StepName = "",
                    ParamID = "HK00" + (i + 1),
                    ParamName = paramName[i].ToString(),
                    ParamValue = listArray[i, 1].ToString()
                };
            }
        }
    }
}
