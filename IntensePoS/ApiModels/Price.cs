using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels
{
    class Price
    {
        public int id { get; set; } = 0;
        public int product_id { get; set; } = 0;
        public int specification_id { get; set; } = 0;
        public double price { get; set; } = 0.00;
        public double purchase_price { get; set; } = 0.00;
        public DateTime date_added { get; set; } = DateTime.Now;
        public int currency_id { get; set; } = 0;
    }
}
