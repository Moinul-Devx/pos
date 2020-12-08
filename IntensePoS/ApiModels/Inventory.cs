using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels
{
    class Inventory
    {
        public bool success { get; set; } = false;
        public string message { get; set; } = string.Empty;
        [JsonProperty("data")]
        public List<Product> products { get; set; } = new List<Product>();
        [JsonProperty("product_data")]
        public Product product { get; set; } = new Product();
    }
}
