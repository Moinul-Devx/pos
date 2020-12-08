using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels.V3
{
    public class Customer
    {
        public int user_id { get; set; }
        public string email { get; set; }
        public string username { get; set; }
        public string phone_number { get; set; }
        public string role { get; set; }
    }

    public class CustomerResult
    {       
        public bool success { get; set; }
        public string message { get; set; }
        public Customer data { get; set; }
    }

}
