using Apriso.MIPlugins.Communication.Clients;
using Apriso.MIPlugins.Communication.Clients.WcfServiceAPI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    class WCF_Client
    {
        private static WcfClient client;
        public WCF_Client()
        {
            
        }
        public bool Connect(string deviceNo, string mesUrl) 
        {
            client = new WcfClient(
                   deviceNo,

                   (messageRequest) =>
                   {
                       return new MessageResponse()
                       {
                           Success = true,
                           MessageGuid = messageRequest.MessageGuid,
                           CommandResponseJson = "I am response for server request. MessageGuid : " + messageRequest.MessageGuid
                       };
                   }

             , mesUrl /*"59.172.37.74:8007"*/);
            return true;
        }
        public bool SendMsg(string CommandId, string json, ref string revJson,ref string errorMessage)
        {
            try
            {
                MessageResponse rs1 = client.SendMessage(new MessageRequest()
                {
                    CommandId = CommandId,
                    MessageGuid = System.Guid.NewGuid(),
                    RequestDate = DateTime.Now,
                    CommandRequestJson = json
                });
                revJson = rs1.CommandResponseJson;
                errorMessage = rs1.ErrorMessage;
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        internal void SendMsg(string v, JObject jsonRequest, ref string jsonResponse)
        {
            throw new NotImplementedException();
        }
    }
}
