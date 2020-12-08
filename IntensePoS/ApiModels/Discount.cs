using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels
{
    class Discount
    {
        public int id { get; set; } = 0;
        public string discount_type { get; set; } = string.Empty;
        public double amount { get; set; } = 0.00;
        public DateTime start_date { get; set; } = DateTime.Now;
        public DateTime end_date { get; set; } = DateTime.Now;
        public double max_amount { get; set; } = 0.00;
        public int group_product_id { get; set; } = 0;
        public int product_id { get; set; } = 0;
        public int specification_id { get; set; } = 0;
        public bool is_active { get; set; } = false;
    }
}
