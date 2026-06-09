using Microsoft.Owin.Hosting;
using System;
using System.Diagnostics;

namespace Machine
{
    class WebServer
    {
        static IDisposable server = null;
        public static bool Start()
        {
            if (null != server)
                return true;
            try
            {
                StartOptions opt = new StartOptions();
                string strDomain = System.Configuration.ConfigurationManager.AppSettings.Get("Domain");
                int nPort = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("Port"));
                if (string.IsNullOrEmpty(strDomain))
                {
                    return false;
                }
                opt.Port = nPort;
                string baseAddress = string.Format("http://{0}:{1}/", strDomain, nPort);
                baseAddress = $"http://127.0.0.1:{nPort}/";
                StartOptions options  = new StartOptions();
                options.Urls.Add(baseAddress);

                server = WebApp.Start<Startup>(options);
                Def.WriteLog("WebServer", string.Format("WebApi接口服务启动成功, 设置网站为: {0}", baseAddress), HelperLibrary.LogType.Information);
                return true;
            }
            catch (Exception ex)
            {
                server = null;
                string msg = ex.Message;
                Trace.WriteLine("WebApi接口服务启动失败, 请检查MES网口IP地址设置是否正确!");

                Trace.WriteLine(" 异常 "+ex.ToString());

                return false;
            }
        }

        public static bool Dispose()
        {
            try
            {
                if (null != server)
                {
                    server.Dispose();
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
