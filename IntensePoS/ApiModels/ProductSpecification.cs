using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels
{
    class ProductSpecification
    {
        public int id { get; set; } = 0; 
        public int product_id { get; set; } = 0; 
        public string color { get; set; } = string.Empty; 
        public string size { get; set; } = string.Empty; 
        public string weight { get; set; } = string.Empty; 
        public string unit { get; set; } = string.Empty; 
        public string weight_unit { get; set; } = string.Empty; 
        public string warranty { get; set; } = string.Empty; 
        public string warranty_unit { get; set; } = string.Empty; 
        public double vat { get; set; } = 0.00; 
        public float quantity { get; set; } = 0; 
        public string new_price { get; set; } = string.Empty; 
        public string old_price { get; set; } = string.Empty; 
        public string purchase_price { get; set; } = string.Empty;
        public Price price { get; set; } = new Price();
        public Discount discount { get; set; } = new Discount();
        public ProductPoint point { get; set; } = new ProductPoint();
        [JsonProperty("delivery_info")]
        public DeliveryInfo deliveryInfo { get; set; } = new DeliveryInfo();
    }
}
