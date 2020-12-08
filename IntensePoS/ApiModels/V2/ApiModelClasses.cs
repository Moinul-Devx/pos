using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.ApiModels.V2
{
    public class Image
    {
        public int id { get; set; }
        public int product_id { get; set; }
        public string product_image { get; set; }
        public string image_url { get; set; }
        public string content { get; set; }
    }

    public class CategoryObject
    {
        public int id { get; set; }
        public string title { get; set; }
        public int category_id { get; set; }
        public bool active { get; set; }
        public string level { get; set; }
        public DateTime timestamp { get; set; }
        public bool is_active { get; set; }
    }

    public class SubCategoryObject
    {
        public int id { get; set; }
        public int category_id { get; set; }
        public int sub_category_id { get; set; }
        public string title { get; set; }
        public bool active { get; set; }
        public string level { get; set; }
        public bool is_active { get; set; }
        public DateTime timestamp { get; set; }
        public List<object> children { get; set; }
    }

    public class SubSubCategoryObject
    {
        public int id { get; set; }
        public int sub_category_id { get; set; }
        public string title { get; set; }
        public bool active { get; set; }
        public string level { get; set; }
        public bool is_active { get; set; }
        public DateTime timestamp { get; set; }
        public int sub_sub_category_id { get; set; }
    }

    public class Price
    {
        public int id { get; set; }
        public int product_id { get; set; }
        public int specification_id { get; set; }
        public double price { get; set; }
        public double purchase_price { get; set; }
        public DateTime date_added { get; set; }
        public int currency_id { get; set; }
    }

    public class Discount
    {
        public int id { get; set; }
        public string discount_type { get; set; }
        public double amount { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public double max_amount { get; set; }
        public int group_product_id { get; set; }
        public int product_id { get; set; }
        public int specification_id { get; set; }
        public bool is_active { get; set; }
    }

    public class Point
    {
        public int id { get; set; }
        public double point { get; set; }
        public int product_id { get; set; }
        public int specification_id { get; set; }
        public string start_date { get; set; }
        public bool is_active { get; set; }
        public string end_date { get; set; }
    }

    public class DeliveryInfo
    {
        public int id { get; set; }
        public object location_id { get; set; }
        public object height { get; set; }
        public object width { get; set; }
        public object length { get; set; }
        public object weight { get; set; }
        public string measument_unit { get; set; }
        public int unit_price { get; set; }
        public object delivery_day { get; set; }
        public double minimum_amount { get; set; }
        public int specification_id { get; set; }
    }

    public class Warehouse
    {
        public int id { get; set; }
        public int warehouse_id { get; set; }
        public string warehouse_name { get; set; }
        public string warehouse_location { get; set; }
        public int specification_id { get; set; }
        public int quantity { get; set; }
    }

    public class Shop
    {
        public int id { get; set; }
        public int shop_id { get; set; }
        public object shop_name { get; set; }
        public object shop_location { get; set; }
        public int specification_id { get; set; }
        public int quantity { get; set; }
    }

    public class Specification
    {
        public int id { get; set; }
        public int product_id { get; set; }
        public string color { get; set; }
        public string size { get; set; }
        public string weight { get; set; }
        public string unit { get; set; }
        public string weight_unit { get; set; }
        public string warranty { get; set; }
        public string warranty_unit { get; set; }
        public double vat { get; set; }
        public int quantity { get; set; }
        public int seller_quantity { get; set; }
        public int remaining { get; set; }
        public string SKU { get; set; }
        public string barcode { get; set; }
        public string new_price { get; set; }
        public string old_price { get; set; }
        public string purchase_price { get; set; }
        public Price price { get; set; }
        public Discount discount { get; set; }
        public Point point { get; set; }
        public DeliveryInfo delivery_info { get; set; }
        public string manufacture_date { get; set; }
        public string expire { get; set; }
        public List<Warehouse> warehouse { get; set; }
        public List<Shop> shop { get; set; }
    }

    public class sProduct
    {
        public int id { get; set; }
        public int seller { get; set; }
        public object seller_name { get; set; }
        public string seller_email { get; set; }
        public string product_admin_status { get; set; }
        public string product_status { get; set; }
        public string title { get; set; }
        public string brand { get; set; }
        public DateTime date { get; set; }
        public string description { get; set; }
        public List<string> key_features { get; set; }
        public bool properties { get; set; }
        public bool is_deleted { get; set; }
        public bool is_group { get; set; }
        public string origin { get; set; }
        public string shipping_country { get; set; }
        public List<Image> images { get; set; }
        public int category_id { get; set; }
        public int sub_category_id { get; set; }
        public int sub_sub_category_id { get; set; }
        public string category { get; set; }
        public string sub_category { get; set; }
        public string sub_sub_category { get; set; }
        public CategoryObject category_object { get; set; }
        public SubCategoryObject sub_category_object { get; set; }
        public SubSubCategoryObject sub_sub_category_object { get; set; }
        public List<Specification> specifications { get; set; }
    }

    public class Root
    {
        public List<sProduct> products { get; set; }
    }


}
