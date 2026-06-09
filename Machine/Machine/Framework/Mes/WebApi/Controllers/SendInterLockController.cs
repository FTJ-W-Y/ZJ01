using System;
using System.Diagnostics;
using System.Net.Http;
using System.Web.Http;

namespace Machine
{
    public class SendInterLockController : ApiController
    {
        //检查用户名是否已注册
        private ApiTools tool = new ApiTools();
        [HttpPost]
        [HttpGet]
        public HttpResponseMessage SendInterLock(InterLock interLock)
        {
            return tool.MsgFormat(interLock);
        }


    }

    public class ApiTools
    {
        private string msgModel = "{{\"message\":\"{0}\"}}";
        public event EventHandler<EventArgs> onTriggered;
        public ApiTools()
        {
            onTriggered += MachineCtrl.GetInstance().InterLock;
        }

        public HttpResponseMessage MsgFormat(InterLock interLock)
        {
            string json = "";

            try
            {
                if (null == interLock)
                {
                    json = string.Format(msgModel, "信息为null!");
                }
                else if (string.IsNullOrEmpty(interLock.InterLockCode))
                {
                    json = string.Format(msgModel, "InterLockCode互锁信号代码为空!");
                }
                else if (string.IsNullOrEmpty(interLock.EquipmentCode))
                {
                    json = string.Format(msgModel, "EquipmentCode设备编码为空!");
                }
                else if (!interLock.EquipmentCode.Equals(MesResources.Equipment.EquipmentCode))
                {
                    json = string.Format(msgModel, $"EquipmentCode=设备编码与设备不符合!");
                }

                Trace.WriteLine((null == interLock) ? "信息为null!" : interLock.ToString());

                // 触发响应状态400报警
                if (!string.IsNullOrEmpty(json))
                {
                    var resp = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(json),
                        ReasonPhrase = "Api Execute Exception!"
                    };
                    throw new HttpResponseException(resp);
                }

                // 与本地设置互锁编码对比
                string[] strInterLockCode = MachineCtrl.GetInstance().InterLockCode;
                for (int nIdx = 0; nIdx < strInterLockCode.Length; nIdx++)
                {
                    if (interLock.InterLockCode.Equals(strInterLockCode[nIdx]))
                    {
                        MachineCtrl.GetInstance().bInterLockResult = true;
                        MachineCtrl.GetInstance().nInterLockCodeIndex = (nIdx + 1);
                        if(onTriggered != null)
                        {
                            onTriggered.Invoke(null, null);
                        }
                    }
                }

                json = string.Format(msgModel, "成功!");
                return new HttpResponseMessage { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
            }
            // HttpResponseException异常直接抛
            catch (HttpResponseException ex)
            {
                Trace.WriteLine(json);
                throw ex;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);

                json = string.Format(msgModel, "服务器异常!");
                // 抛204
                var resp = new HttpResponseMessage(System.Net.HttpStatusCode.NoContent)
                {
                    Content = new StringContent(json),
                    ReasonPhrase = "Api Execute Exception!"
                };
                throw new HttpResponseException(resp);
            }
            finally
            {
                string text = string.Format("{0},{1},{2},{3},{4}"
                    , DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), (string.IsNullOrEmpty(interLock.InterLockCode) ? "" : interLock.InterLockCode), (string.IsNullOrEmpty(interLock.InterLockMessage) ? "" : interLock.InterLockMessage), (string.IsNullOrEmpty(interLock.EquipmentCode) ? "" : interLock.EquipmentCode), json);
                MachineCtrl.GetInstance().SaveInterLockLogData(text);
            }
        }
    }
}
