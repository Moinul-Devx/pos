using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using IntensePoS.Lib;
using IntensePoS.ApiModels.V3;


namespace IntensePoS.Models
{
    static class Order
    {
        //  Database
        private static string ConnectionString = Properties.Settings.Default.connString;
        private static OleDbConnection Conn;
        private static OleDbCommand cmd = new OleDbCommand();
        private static string Query = "";

        public static DataTable GetOrderedItemsByInvoice (string invoiceNo)
        {
            Conn = new OleDbConnection(ConnectionString);
            Conn.Open();

            cmd.Connection = Conn;
            
            Query = " SELECT p.ID, i.qty AS Qty, p.unit AS Unit, p.product_name AS ItemName, VAL(p.unit_price) AS UnitPrice, (i.item_total) AS ItemTotal, VAL(p.discount) AS Discount, VAL(p.vat) AS VAT, p.unit AS bUnit, s.volume AS Volume, p.image  FROM (ordered_items i INNER JOIN products p ON p.ID = i.product_id) INNER JOIN inventories s ON s.product_id = p.ID WHERE i.order_id IN (SELECT order_id FROM orders WHERE order_invoice_no = '" + invoiceNo + "') ";

            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            Conn.Close();
            return dt;
        }
        
        
        public static DataTable GetOrdereByInvoice(string invoiceNo)
        {
            Conn = new OleDbConnection(ConnectionString);
            Conn.Open();

            cmd.Connection = Conn;

            Query = " SELECT o.*, c.* FROM orders o INNER JOIN customers c ON o.customer_id = c.customer_id WHERE o.order_invoice_no = '" + invoiceNo + "' ";

            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            Conn.Close();
            return dt;           
        }


        public static void SyncOrder(Dictionary<string, string> orderInfo)
        {
            string message;

            try
            {
                if (Conn != null && Conn.State == ConnectionState.Closed) Conn.Open();
                else
                {
                    Conn = new OleDbConnection(ConnectionString);
                    Conn.Open();
                }

                cmd = new OleDbCommand();
                cmd.Connection = Conn;
                cmd.CommandText = "SELECT * FROM customers WHERE customer_id = " + orderInfo["customerId"];

                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                DataTable dtCustomer = new DataTable(); 
                da.Fill(dtCustomer);

                int user_id = Int32.Parse(dtCustomer.Rows[0]["user_id"].ToString());
                
                cmd.CommandText = "SELECT p.sync_id AS specification_id, i.qty AS quantity FROM ordered_items i INNER JOIN products p ON i.product_id = p.ID WHERE i.order_id = " + orderInfo["order_id"];
                da = new OleDbDataAdapter(cmd);
                DataTable dtOrder = new DataTable();
                da.Fill(dtOrder);

                var oitems = dtOrder.Select().Select(x => x.ItemArray.Select((a, i) => new { Name = dtOrder.Columns[i].ColumnName, Value = a })
                                                                                   .ToDictionary(a => a.Name, a => a.Value));
                

                var orderobj = new
                {
                    order = new
                    {
                        terminal_id = Int32.Parse(orderInfo["terminal_id"]),
                        API_key = orderInfo["API_key"],
                        invoice_no = orderInfo["invoice_no"],
                        pos_user_id = Int32.Parse(orderInfo["pos_user_id"]),

                        user = new
                        {
                            id = user_id,
                            username = dtCustomer.Rows[0]["customer_name"],
                            email = dtCustomer.Rows[0]["customer_email"],
                            phone_number = dtCustomer.Rows[0]["customer_mobile"]
                        },

                        items = oitems,

                        sub_total = orderInfo["sub_total"],
                        discount = orderInfo["discount"],
                        vat = orderInfo["vat"],
                        num_items = orderInfo["num_items"],
                        additional_discount = orderInfo["additional_discount"],
                        additional_discount_type = orderInfo["additional_discount_type"],
                        grand_total = orderInfo["grand_total"],
                        payment = orderInfo["payment"],
                        changes = orderInfo["changes"],
                        due = orderInfo["due"]
                    }
                };

                string orderJson = JsonConvert.SerializeObject(orderobj);

                // No need. Content-Type will be application/json
                // var keyVal = SyncLibrary.ToKeyValue(orderobj);

                string url = "http://68.183.231.43/Cart/create_pos_order/";

                string response = SyncOrderPost(orderJson.Replace("\\", ""), url);

                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.MissingMemberHandling = MissingMemberHandling.Error;

                OrderRoot root = JsonConvert.DeserializeObject<OrderRoot>(response, settings);

                bool success = SyncOrderToLocalDatabase(root.order);
            }
            catch (Exception x)
            {
                message = x.Message;
            }            
        }

        private static bool SyncOrderToLocalDatabase(SyncedOrder order)
        {
            Conn = new OleDbConnection(ConnectionString);
            Conn.Open();

            OleDbTransaction Trans = Conn.BeginTransaction();
            cmd = new OleDbCommand();
            cmd.Connection = Conn;
            cmd.Transaction = Trans;

            // Sync User (Customer)
            bool customerSynced = SyncOrderCustomer (order.user, cmd, Trans);

            // Sync Order
            bool orderSynced = SyncOrder (order.order_data, order.invoice, cmd, Trans);

            // Sync Ordered Items
            bool itemsSynced = SyncOrderedItems (order.order_data.items, Trans);

            // Sync Invoice. No need as already done with orderSynced
            // bool invoiceSynced = SyncInvoice(order.invoice, cmd, Trans);

            // Sync Stock
            bool stockSynced = SyncStock (order.stock, cmd, Trans);

            /// NOTE:
            /*
            Order API (POST) response returns stock empty due to all specified products are with 0 quantity at server.
            Recommended: A script run on the ecommrce database to increase all quantities for test purpose.
            */

            Trans.Commit();
            Conn.Close();

            FlushTables();

            return false;
        }


        private static void FlushTables()
        {
            string message = "";
            Conn = new OleDbConnection(ConnectionString);
            
            try
            {
                Conn.Open();
                cmd = new OleDbCommand();
                cmd.Connection = Conn;

                cmd.CommandText = "ALTER TABLE __intense_user_API ALTER COLUMN ID COUNTER";
                cmd.ExecuteNonQuery();
                Conn.Close();
            }
            catch (Exception x)
            {
                message = "Flush Table\nError! " + x.Message;
            }

            try
            {
                Conn.Open();
                cmd = new OleDbCommand();
                cmd.Connection = Conn;

                cmd.CommandText = "ALTER TABLE __intense_invoice_API ALTER COLUMN ID COUNTER";
                cmd.ExecuteNonQuery();
                Conn.Close();
            }
            catch (Exception x)
            {
                message = "Flush Table\nError! " + x.Message;
            }

            try
            {
                Conn.Open();
                cmd = new OleDbCommand();
                cmd.Connection = Conn;

                cmd.CommandText = "ALTER TABLE __intense_order_API ALTER COLUMN ID COUNTER";
                cmd.ExecuteNonQuery();
                Conn.Close();
            }
            catch (Exception x)
            {
                message = "Flush Table\nError! " + x.Message;
            }


            try
            {
                Conn.Open();
                cmd = new OleDbCommand();
                cmd.Connection = Conn;

                cmd.CommandText = "ALTER TABLE __intense_orderdetails_API ALTER COLUMN ID COUNTER";
                cmd.ExecuteNonQuery();
                Conn.Close();
            }
            catch (Exception x)
            {
                message = "Flush Table\nError! " + x.Message;
            }

            try
            {
                Conn.Open();
                cmd = new OleDbCommand();
                cmd.Connection = Conn;

                cmd.CommandText = "ALTER TABLE __intense_warehouse_API ALTER COLUMN ID COUNTER";
                cmd.ExecuteNonQuery();
                Conn.Close();
            }
            catch (Exception x)
            {
                message = "Flush Table\nError! " + x.Message;
            }
        }


        private static bool SyncStock(List<IntensePoS.ApiModels.V3.Warehouse> stock, OleDbCommand cmd, OleDbTransaction Trans)
        {
            foreach (IntensePoS.ApiModels.V3.Warehouse warehouse in stock)
            {
                bool warehouseSynced = SyncWarehouse(warehouse, Trans);
            }

            // Invoice
            cmd.CommandText = "DELETE FROM __intense_warehouse_API"; cmd.Transaction = Trans;
            cmd.ExecuteScalar();

            return false;
        }


        private static bool SyncWarehouse (IntensePoS.ApiModels.V3.Warehouse warehouse, OleDbTransaction Trans)
        {
            string message = "";

            try
            {
                string SQL = "";

                SQL = " INSERT INTO [__intense_warehouse_API] ([server_id], [warehouse_id], [specification_id], [quantity], [place]) VALUES ";
                SQL += " ( " + warehouse.id + ", " + warehouse.warehouse_id + ", " + warehouse.specification_id + ", " + warehouse.quantity + ", '" + warehouse.place + "' ) ";

                try
                {
                    cmd.CommandText = SQL;
                    var user_id = cmd.ExecuteScalar();
                }
                catch (Exception x)
                {
                    message += x.Message;
                }

                SQL = " UPDATE ";
                SQL += " [__intense_warehouse_API] [sync] ";
                SQL += " LEFT JOIN ";
                SQL += " [__intense_warehouse] [warehouse]";
                SQL += " ON [warehouse].[server_id] = [sync].[server_id] ";

                SQL += " SET ";

                SQL += " [warehouse].[server_id] = [sync].[server_id], ";
                SQL += " [warehouse].[warehouse_id] = [sync].[warehouse_id], ";
                SQL += " [warehouse].[specification_id] = [sync].[specification_id], ";
                SQL += " [warehouse].[quantity] = [sync].[quantity], ";
                SQL += " [warehouse].[place] = [sync].[place] ";

                cmd.CommandText = SQL;
                var synced_user_id = cmd.ExecuteScalar();                

                /*
                // TRUNCATE the API sync table in Access way

                cmd.CommandText = "DELETE FROM __intense_warehouse_API"; cmd.Transaction = Trans;
                cmd.ExecuteScalar();
                */
            }
            catch (Exception x)
            {
                Trans.Rollback();
                message = "Error!\nUser Sync Failed.\n" + x.Message;
            }

            return false;
        }

        public static bool SyncOrderCustomer(IntensePoS.ApiModels.V3.User user, OleDbCommand cmd, OleDbTransaction Trans)
        {
            string message = "";

            try
            {
                string SQL = "";

                SQL =  " INSERT INTO [__intense_user_API] ([server_id], [email], [phone_number], [username], [_role]) VALUES ";
                SQL += " ( " + user.id + ", '" + user.email + "', '" + user.phone_number + "', '" + user.username + "', '" + user.role + "' ) ";

                try
                {
                    cmd.CommandText = SQL; cmd.Transaction = Trans;
                    var user_id = cmd.ExecuteScalar();
                }
                catch (Exception x) 
                { 
                    message += x.Message; 
                }

                SQL = " UPDATE ";               
                SQL += " [__intense_user_API] [sync] ";
                SQL += " LEFT JOIN ";
                SQL += " [__intense_user] [user]";
                SQL += " ON [user].[server_id] = [sync].[server_id] ";                

                SQL += " SET ";

                SQL += " [user].[server_id] = [sync].[server_id], ";
                SQL += " [user].[email] = [sync].[email], ";
                SQL += " [user].[phone_number] = [sync].[phone_number], ";
                SQL += " [user].[username] = [sync].[username], ";
                SQL += " [user].[_role] = [sync].[_role] ";

                cmd.CommandText = SQL;
                var synced_user_id = cmd.ExecuteScalar();

                Query = "SELECT(MAX(ID) + 1) AS ID FROM customers";                
                cmd.CommandText = Query;
                
                var result = cmd.ExecuteScalar(); int iresult = 0;
                
                string strCustomerId = "0";
                
                if (Int32.TryParse(result.ToString(), out iresult) && iresult > 0)
                    strCustomerId = iresult.ToString();

                SQL = " UPDATE ";
                SQL += " [__intense_user_API] [sync] ";
                SQL += " LEFT JOIN ";
                SQL += " [customers] ";
                SQL += " ON [customers].[customer_email] = [sync].[email] ";                

                SQL += " SET ";

                SQL += " [customers].[customer_id] = " + strCustomerId + ", ";
                SQL += " [customers].[user_id] = [sync].[server_id], ";
                SQL += " [customers].[customer_email] = [sync].[email], ";
                SQL += " [customers].[customer_mobile] = [sync].[phone_number], ";
                SQL += " [customers].[customer_name] = [sync].[username] ";

                cmd.CommandText = SQL;
                var customer_id = cmd.ExecuteScalar();

                // Save customer as user
                cmd.CommandText = " INSERT INTO users (sync_id, username, email) VALUES ( " + user.id + ", " + user.username + ", " + user.email + " ) ";
                var inserted_id = cmd.ExecuteScalar();

                // TRUNCATE the API sync table in Access way                
                cmd.CommandText = "DELETE FROM __intense_user_API"; cmd.Transaction = Trans;
                cmd.ExecuteScalar();
                                                                
            }
            catch(Exception x)
            {
                Trans.Rollback();
                message = "Error!\nUser Sync Failed.\n" + x.Message;
            }

            return false;
        }

        private static bool SyncOrder (IntensePoS.ApiModels.V3.OrderData order, IntensePoS.ApiModels.V3.Invoice invoice, OleDbCommand cmd, OleDbTransaction Trans)
        {
            string message = "";

            try
            {
                string SQL = "";

                SQL = " INSERT INTO [__intense_order_API] " +

                        "(" +
                        "[server_id], " +
                        "[order_status], " +
                        "[delivery_status], " +
                        "[admin_status], " +
                        "[date_created], " +
                        "[user_id], " +
                        "[ip_address], " +
                        "[checkout_status], " +
                        "[ordered_date], " +
                        "[non_verified_user_id], " +
                        "[coupon], " +
                        "[coupon_code], " +
                        "[is_seller], " +
                        "[is_pos], " +
                        "[admin_id], " +
                        "[pos_additional_discount], " +
                        "[pos_additional_discount_type], " +
                        "[sub_total], " +
                        "[grand_total], " +
                        "[payment], " +
                        "[changes], " +
                        "[due], " +
                        "[vat], " +
                        "[num_items] " +
                        ") " +
                
                        "VALUES " ;

                SQL += " ( "; 
                SQL +=      order.id + ", '" + order.order_status + "', '" + order.delivery_status + "', '" + order.admin_status + "', ";
                SQL +=      "'" + order.date_created + "', " + order.user_id + ", '" + order.ip_address + "', ";
                SQL +=      order.checkout_status + ", '" + order.ordered_date + "', " + order.non_verified_user_id + ", ";
                SQL +=      order.coupon + ", '" + order.coupon_code + "', " ;

                SQL += order.is_seller + ", ";
                SQL += order.is_pos + ", ";
                SQL += order.admin_id + ", ";
                SQL += order.pos_additional_discount + ", ";
                SQL += "'" + order.pos_additional_discount_type + "', ";
                SQL += order.sub_total + ", ";
                SQL += order.grand_total + ", ";
                SQL += order.payment + ", ";
                SQL += order.changes + ", ";
                SQL += order.due + ", ";
                SQL += order.vat + ", ";
                SQL += order.num_items;

                SQL += " ) ";

                try
                {
                    cmd.CommandText = SQL;
                    var order_id = cmd.ExecuteScalar();
                }
                catch (Exception x)
                {
                    message += x.Message;
                }

                SQL = " UPDATE ";
                SQL += " [__intense_order_API] [sync] ";
                SQL += " LEFT JOIN ";
                SQL += " [__intense_order] [order]";
                SQL += " ON [order].[server_id] = [sync].[server_id] ";

                SQL += " SET ";

                SQL += " [order].[server_id]=[sync].[server_id], ";
                SQL += " [order].[order_status]=[sync].[order_status], ";
                SQL += " [order].[delivery_status]=[sync].[delivery_status], ";
                SQL += " [order].[admin_status]=[sync].[admin_status], ";
                SQL += " [order].[date_created]=[sync].[date_created], ";
                SQL += " [order].[user_id]=[sync].[user_id], ";
                SQL += " [order].[ip_address]=[sync].[ip_address], ";
                SQL += " [order].[checkout_status]=[sync].[checkout_status], ";
                SQL += " [order].[ordered_date]=[sync].[ordered_date], ";
                SQL += " [order].[non_verified_user_id]=[sync].[non_verified_user_id], ";
                SQL += " [order].[coupon]=[sync].[coupon], ";
                SQL += " [order].[coupon_code]=[sync].[coupon_code], ";
                SQL += " [order].[is_seller]=[sync].[is_seller], ";
                SQL += " [order].[is_pos]=[sync].[is_pos], ";
                SQL += " [order].[admin_id]=[sync].[admin_id], ";
                SQL += " [order].[pos_additional_discount]=[sync].[pos_additional_discount], ";
                SQL += " [order].[pos_additional_discount_type]=[sync].[pos_additional_discount_type], ";
                SQL += " [order].[sub_total]=[sync].[sub_total], ";
                SQL += " [order].[grand_total]=[sync].[grand_total], ";
                SQL += " [order].[payment]=[sync].[payment], ";
                SQL += " [order].[changes]=[sync].[changes], ";
                SQL += " [order].[due]=[sync].[due], ";
                SQL += " [order].[vat]=[sync].[vat], ";
                SQL += " [order].[num_items]=[sync].[num_items]";


                cmd.CommandText = SQL;
                var synced_order_id = cmd.ExecuteScalar();

                /******************************************************/

                SQL = " INSERT INTO [__intense_invoice_API] " +

                        "(" +
                        "[server_id], " +
                        "[order_id], " +
                        "[_date], " +
                        "[_time], " +
                        "[ref_invoice], " +
                        "[is_active], " +
                        "[admin_id], " +
                        "[invoice_no] " +                        
                        ") " +

                        "VALUES ";

                SQL += " ( ";
                SQL += invoice.id + ", " + invoice.order_id + ", '" + invoice.date + "', '" + invoice.time + "', ";
                SQL += invoice.ref_invoice + ", " + invoice.is_active + ", " + invoice.admin_id + ", ";
                SQL += "'" + invoice.invoice_no + "'";
                SQL += " ) ";

                try
                {
                    cmd.CommandText = SQL;
                    var invoice_id = cmd.ExecuteScalar();
                }
                catch (Exception x)
                {
                    message += x.Message;
                }


                SQL = " UPDATE ";
                SQL += " [__intense_invoice_API] [sync] ";
                SQL += " LEFT JOIN ";
                SQL += " [__intense_invoice] [invoice]";
                SQL += " ON [invoice].[server_id] = [sync].[server_id] ";

                SQL += " SET ";

                SQL += " [invoice].[server_id]=[sync].[server_id], ";
                SQL += " [invoice].[order_id]=[sync].[order_id], ";
                SQL += " [invoice].[_date]=[sync].[_date], ";
                SQL += " [invoice].[_time]=[sync].[_time], ";
                SQL += " [invoice].[ref_invoice]=[sync].[ref_invoice], ";
                SQL += " [invoice].[is_active]=[sync].[is_active], ";
                SQL += " [invoice].[admin_id]=[sync].[admin_id], ";
                SQL += " [invoice].[invoice_no]=[sync].[invoice_no] ";

                cmd.CommandText = SQL;
                var synced_invoice_id = cmd.ExecuteScalar();


                /***************************************************/

                SQL = " UPDATE (((";
                SQL += " [__intense_order_API] [sync] ";
                SQL += " INNER JOIN ";
                SQL += " [__intense_invoice_API] [sync_invoice]";
                SQL += " ON [sync].[server_id] = [sync_invoice].[order_id]";
                SQL += " ) INNER JOIN [__intense_user][user] ON [user].[server_id] = [sync].[user_id]";
                SQL += " ) LEFT JOIN [orders] ON [orders].[order_invoice_no] = [sync_invoice].[invoice_no]";
                SQL += " )";
                SQL += " SET ";

                SQL += " [orders].[sync_order_id] = [sync].[server_id], ";                                
                SQL += " [orders].[server_date_time] = [sync].[date_created] ";

                cmd.CommandText = SQL;
                var final_order_id = cmd.ExecuteScalar();

                // TRUNCATE the API sync table in Access way                

                // Ordered Items
                // NO NEED!

                // Invoice
                cmd.CommandText = "DELETE FROM __intense_invoice_API"; cmd.Transaction = Trans;
                cmd.ExecuteScalar();

                // Order
                cmd.CommandText = "DELETE FROM __intense_order_API"; cmd.Transaction = Trans;
                cmd.ExecuteScalar();

            }
            catch (Exception x)
            {
                Trans.Rollback();
                message = "Error!\nUser Sync Failed.\n" + x.Message;
            }

            return false;
        }

        private static bool SyncOrderedItems (List<IntensePoS.ApiModels.V3.Item> items, OleDbTransaction Trans)
        {
            foreach (IntensePoS.ApiModels.V3.Item item in items)
            {
                bool itemSynced = SyncOrderedItem(item, Trans);
            }

            // Invoice
            cmd.CommandText = "DELETE FROM __intense_orderdetails_API"; cmd.Transaction = Trans;
            cmd.ExecuteScalar();

            return false;
        }

        private static bool SyncOrderedItem (IntensePoS.ApiModels.V3.Item item, OleDbTransaction Trans)
        {
            string message = "";

            string SQL = "";

            SQL = " INSERT INTO [__intense_orderdetails_API] " +

                    "(" +
                    "[server_id], " +
                    "[order_id], " +
                    "[product_id], " +
                    "[specification_id], " +
                    "[quantity], " +
                    "[date_added], " +
                    "[is_removed], " +
                    "[delivery_removed], " +
                    "[total_quantity], " +
                    "[unit_price], " +
                    "[total_price], " +
                    "[unit_point], " +
                    "[total_point], " +
                    "[product_name], " +
                    "[product_color], " +
                    "[product_size], " +
                    "[product_weight], " +
                    "[product_unit], " +
                    "[product_images], " +
                    "[remaining], " +
                    "[admin_status], " +
                    "[product_status], " +
                    "[unit_discount], " +
                    "[total_discount] " +
                    ") " +

                    "VALUES ";

            SQL += " ( ";

            SQL += item.id + ", " + item.order_id + ", " + item.product_id + ", " + item.specification_id + ", ";
            SQL += item.quantity + ", '" + item.date_added + "', " + item.is_removed + ", ";
            SQL += item.delivery_removed + ", " + item.total_quantity + ", " + item.unit_price + ", ";
            SQL += item.total_price + ", " + item.unit_point + ", ";

            SQL += item.total_point + ", ";
            SQL += "'" + item.product_name + "', ";
            SQL += "'" + item.product_color + "', ";
            SQL += "'" + item.product_size + "', ";
            SQL += "'" + item.product_weight + "', ";
            SQL += "'" + item.product_unit + "', ";
            SQL += "'" + string.Join(", ", item.product_images) + "', ";
            SQL += item.remaining + ", ";
            SQL += "'" + item.admin_status + "', ";
            SQL += "'" + item.product_status + "', ";            
            SQL += item.unit_discount + ", ";
            SQL += item.total_discount;

            SQL += " ) ";

            try
            {
                cmd.CommandText = SQL;
                var item_id = cmd.ExecuteScalar();
            }
            catch (Exception x)
            {
                message += x.Message;
            }

            SQL = " UPDATE ";
            SQL += " [__intense_orderdetails_API] [sync] ";
            SQL += " LEFT JOIN ";
            SQL += " [__intense_orderdetails] [order]";
            SQL += " ON [order].[server_id] = [sync].[server_id] ";

            SQL += " SET ";

            SQL += " [order].[server_id]=[sync].[server_id], ";
            SQL += " [order].[order_id]=[sync].[order_id], ";
            SQL += " [order].[product_id]=[sync].[product_id], ";
            SQL += " [order].[specification_id]=[sync].[specification_id], ";
            SQL += " [order].[quantity]=[sync].[quantity], ";
            SQL += " [order].[date_added]=[sync].[date_added], ";
            SQL += " [order].[is_removed]=[sync].[is_removed], ";
            SQL += " [order].[delivery_removed]=[sync].[delivery_removed], ";
            SQL += " [order].[total_quantity]=[sync].[total_quantity], ";
            SQL += " [order].[unit_price]=[sync].[unit_price], ";
            SQL += " [order].[total_price]=[sync].[total_price], ";
            SQL += " [order].[unit_point]=[sync].[unit_point], ";
            SQL += " [order].[total_point]=[sync].[total_point], ";
            SQL += " [order].[product_name]=[sync].[product_name], ";
            SQL += " [order].[product_color]=[sync].[product_color], ";
            SQL += " [order].[product_size]=[sync].[product_size], ";
            SQL += " [order].[product_weight]=[sync].[product_weight], ";
            SQL += " [order].[product_unit]=[sync].[product_unit], ";
            SQL += " [order].[product_images]=[sync].[product_images], ";
            SQL += " [order].[remaining]=[sync].[remaining], ";
            SQL += " [order].[admin_status]=[sync].[admin_status], ";
            SQL += " [order].[product_status]=[sync].[product_status], ";
            SQL += " [order].[unit_discount]=[sync].[unit_discount], ";
            SQL += " [order].[total_discount]=[sync].[total_discount]";


            cmd.CommandText = SQL;
            var synced_item_id = cmd.ExecuteScalar();

            return false;
        }

        private static bool SyncInvoice (IntensePoS.ApiModels.V3.Invoice invoice, OleDbCommand cmd, OleDbTransaction Trans)
        {
            return false;
        }

        private static string SyncOrderPost(string postData, string URL)
        {
            postData = postData.Replace("\\", "");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent =
                              "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)";
            request.Accept = "application/json";
            request.UseDefaultCredentials = true;
            request.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);            
            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            string responseFromServer = "";

            try
            {
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                responseFromServer = reader.ReadToEnd();

                reader.Close();
                dataStream.Close();
                response.Close();
            }
            catch (Exception x)
            {
                responseFromServer = x.Message;
            }

            return responseFromServer;
        }
    }
}
