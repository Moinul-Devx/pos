using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels
{
    class ProductImage
    {
        public int id { get; set; } = 0;
        public int product_id { get; set; } = 0;
        public string product_image { get; set; } = string.Empty;
        public string image_url { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }
}
