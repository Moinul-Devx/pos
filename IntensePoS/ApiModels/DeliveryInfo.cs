using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels
{
    class DeliveryInfo
    {
        public int id { get; set; } = 0;
        public int location_id { get; set; } = 0;
        public double height { get; set; } = 0.00;
        public double width { get; set; } = 0.00;
        public double length { get; set; } = 0.00;
        public double weight { get; set; } = 0.00;
        public string measument_unit { get; set; } = string.Empty;
        public double unit_price { get; set; } = 0.00;
        public int delivery_day { get; set; } = 0;
        public double minimum_amount { get; set; } = 0.00;
        public int specification_id { get; set; } = 0;
    }
}
