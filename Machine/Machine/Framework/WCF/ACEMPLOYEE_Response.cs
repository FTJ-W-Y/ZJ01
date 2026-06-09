using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machine
{
    public class ACEMPLOYEE_Response
    {
        public string id { get; set; }
        public string usercode { get; set; }

        public string name { get; set; }

        public string IcCardNo { get; set; }

        public string RoleID { get; set; }

        public List<string> list { get; set; }
    }

}
