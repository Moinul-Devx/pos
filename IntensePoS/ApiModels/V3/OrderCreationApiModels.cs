using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels.V3
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class User
    {
        public int id { get; set; }
        public string email { get; set; }
        public string phone_number { get; set; }
        public string username { get; set; }
        public string role { get; set; }
    }

    public class Item
    {
        public int id { get; set; }
        public int order_id { get; set; }
        public int product_id { get; set; }
        public int specification_id { get; set; }
        public int quantity { get; set; }
        public DateTime date_added { get; set; }
        public bool is_removed { get; set; }
        public bool delivery_removed { get; set; }
        public int total_quantity { get; set; }
        public double unit_price { get; set; }
        public double total_price { get; set; }
        public double unit_point { get; set; }
        public double total_point { get; set; }
        public string product_name { get; set; }
        public string product_color { get; set; }
        public string product_size { get; set; }
        public double product_weight { get; set; }
        public string product_unit { get; set; }
        public List<string> product_images { get; set; }
        public int remaining { get; set; }
        public string admin_status { get; set; }
        public string product_status { get; set; }
        public int unit_discount { get; set; }
        public int total_discount { get; set; }
    }

    public class Warehouse
    {
        public int id { get; set; }
        public int warehouse_id { get; set; }
        public int specification_id { get; set; }
        public double quantity { get; set; }
        public string place { get; set; }
    }

    public class OrderData
    {
        public int id { get; set; }
        public DateTime date_created { get; set; }
        public string order_status { get; set; }
        public string delivery_status { get; set; }
        public string admin_status { get; set; }
        public int user_id { get; set; }
        public int non_verified_user_id { get; set; }
        public string ip_address { get; set; }
        public bool checkout_status { get; set; }
        public string coupon_code { get; set; }
        public bool coupon { get; set; }
        public string ordered_date { get; set; }
        public bool is_seller { get; set; }
        public bool is_pos { get; set; }
        public int admin_id { get; set; }
        public double pos_additional_discount { get; set; }
        public string pos_additional_discount_type { get; set; }
        public double sub_total { get; set; }
        public double grand_total { get; set; }
        public double payment { get; set; }
        public double changes { get; set; }
        public double due { get; set; }
        public double vat { get; set; }
        public int num_items { get; set; }
        public string point_total { get; set; }
        public List<Item> items { get; set; }
    }

    public class Invoice
    {
        public int id { get; set; }
        public int order_id { get; set; }
        public DateTime date { get; set; }
        public DateTime time { get; set; }
        public int ref_invoice { get; set; }
        public bool is_active { get; set; }
        public int admin_id { get; set; }
        public string invoice_no { get; set; }
    }

    public class SyncedOrder
    {
        public User user { get; set; }
        public OrderData order_data { get; set; }
        public Invoice invoice { get; set; }
        public List<Warehouse> stock { get; set; }
    }

    public class OrderRoot
    {
        public bool success { get; set; }
        public string message { get; set; }
        public SyncedOrder order { get; set; }
    }

}
