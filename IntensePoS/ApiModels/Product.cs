using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels
{
    class Product
    {
        [JsonProperty("id")]
        public int server_id { get; set; } = 0;

        ////////////////////////////////////////////////////
        /// TO BE REORGANIZED LATER AFTER DISCUSSION!!
        /// 
        [JsonProperty("seller_name")]
        public string seller { get; set; } = string.Empty;

        [JsonProperty("seller")]
        public int sellerId { get; set; } = 0;
        
        ////////////////////////////////////////////////////
        
        public string product_admin_status { get; set; } = string.Empty;
        public string product_status { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string brand { get; set; } = string.Empty;
        [JsonProperty("date")]
        public DateTime _date { get; set; } = DateTime.Now;
        public string description { get; set; } = string.Empty;
        public string[] key_features { get; set;} = null;
        public bool properties { get; set; } = false;
        public string origin { get; set; } = string.Empty;
        public string shipping_country { get; set; } = string.Empty;
        public string purchase_price { get; set; } = string.Empty;
        public string old_price { get; set; } = string.Empty;
        public string new_price { get; set; } = string.Empty;
        public string discount_type { get; set; } = string.Empty;
        public double discount_amount { get; set; } = 0.00;
        public DateTime discount_start_date { get; set; } = DateTime.Now;
        public DateTime discount_end_date { get; set; } = DateTime.Now;
        public double point { get; set; } = 0.00;
        public DateTime point_start_date { get; set; } = DateTime.Now;
        public DateTime point_end_date { get; set; } = DateTime.Now;
        [JsonProperty("images")]
        public List<ProductImage> productImages { get; set; } = new List<ProductImage>();
        [JsonProperty("specification")]
        public ProductVariant variant { get; set; } = new ProductVariant();
        public float quantity { get; set; } = 0;

        //////////////////////////////////////////////////////////////
        /// TO BE REORGANIZED LATER AFTER DISCUSSION!!
        ///         
        public int category_id { get; set; } = 0;
        public string category { get; set; } = string.Empty;
        public int sub_category_id { get; set; } = 0;
        public string sub_category { get; set; } = string.Empty;
        public int sub_sub_category_id { get; set; } = 0;
        public string sub_sub_category { get; set; } = string.Empty;
        //////////////////////////////////////////////////////////////
        
        [JsonProperty("specifications")]
        public List<ProductSpecification> productSpecifications { get; set; } = new List<ProductSpecification>();

        public bool is_deleted { get; set; } = false;
        public bool is_group { get; set; } = false;

    }
}
