using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels
{
    class ProductPoint
    {
        public int id { get; set; } = 0;
        public double point { get; set; } = 0.00;
        public int product_id { get; set; } = 0;
        public int specification_id { get; set; } = 0;
        public DateTime start_date { get; set; } = DateTime.Now;
        public bool is_active { get; set; } = false;
        public DateTime end_date { get; set; } = DateTime.Now;
    }
}
