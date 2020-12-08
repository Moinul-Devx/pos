using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntensePoS.Lib;

namespace IntensePoS.Models
{
    class Stock
    {
        //  Database
        private static string ConnectionString = Properties.Settings.Default.connString;
        private static OleDbConnection Conn;
        private static OleDbCommand cmd = new OleDbCommand();
        private static string Query = "";

        public static string UpdateStock (List<Dictionary<string, string>> products, int[] productIds)
        {
            string msg =  "";
            bool insert = false;

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                err.Equals(null);
                msg += err.Message;
                return msg;   // FAILED!
            }

            cmd.Connection = Conn;

            foreach (Dictionary<string, string> product in products)
            {                
                    Query = "";

                    cmd.CommandText = "SELECT product_id FROM inventories WHERE product_id = " + product["product_id"];
                    var result = cmd.ExecuteScalar();

                    if (result == null) insert = true;

                    switch (insert)
                    {
                        case false:

                        Query += " UPDATE inventories SET ";

                        foreach (KeyValuePair<string, string> keyValue in product)
                            if (keyValue.Key == product.LastOrDefault().Key)
                                if (keyValue.Key == "volume" || keyValue.Key == "total_price")
                                    Query += " " + keyValue.Key + " = " + keyValue.Key + " + " + keyValue.Value + " ";
                                else
                                    Query += " " + keyValue.Key + " = " + keyValue.Value + " ";
                            else
                                if (keyValue.Key == "volume" || keyValue.Key == "total_price")
                                Query += " " + keyValue.Key + " = " + keyValue.Key + " + " + keyValue.Value + ", ";
                            else
                                Query += " " + keyValue.Key + " = " + keyValue.Value + ", ";

                        Query += " WHERE product_id = " + product["product_id"] + "; ";

                        try
                        {
                            cmd.CommandText = Query;
                            cmd.ExecuteScalar();
                            msg += "\nStock updated for item " ;
                        }
                        catch (Exception err)
                        {
                            err.Equals(null);
                            msg += "\nError: " + err.Message;
                        }

                        break;

                    case true:

                        string iSQL = " INSERT INTO inventories (product_id, last_process_id, volume, total_price) VALUES (";
                        iSQL += " " + product["product_id"] + ", ";
                        iSQL += " " + product["last_process_id"] + ", ";
                        iSQL += " " + product["volume"] + ", ";
                        iSQL += " " + product["total_price"];
                        iSQL += "); ";

                        cmd.CommandText = iSQL;

                        try
                        {
                            cmd.CommandText = iSQL;
                            cmd.ExecuteScalar();
                            msg += "\nStock updated for item ";
                        }
                        catch (Exception err)
                        {
                            err.Equals(null);
                            msg += "\nError: " + err.Message;
                        }

                        break;
                    }                
            }                        

            try
            {
                cmd.CommandText = "UPDATE purchases SET prev_status = cur_status, cur_status = 'SAVED' WHERE ID = " + products[0]["last_process_id"];
                cmd.ExecuteScalar();
                msg += "\nPurchase order saved.";
            }
            catch (Exception err)
            {
                err.Equals(null);
                msg += "\n" + err.Message;
            }

            Conn.Close();
            return msg;
        }


        public static string UpdateReducedStock(List<Dictionary<string, string>> products, int[] productIds)
        {
            string msg = "";
            bool insert = false;

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                err.Equals(null);
                msg += err.Message;
                return msg;   // FAILED!
            }

            cmd.Connection = Conn;

            foreach (Dictionary<string, string> product in products)
            {
                Query = "";

                cmd.CommandText = "SELECT product_id FROM inventories WHERE product_id = " + product["product_id"];
                var result = cmd.ExecuteScalar();

                if (result == null) insert = true;

                switch (insert)
                {
                    case false:

                        Query += " UPDATE inventories INNER JOIN products ON inventories.product_id = products.ID SET ";

                        foreach (KeyValuePair<string, string> keyValue in product)
                            if (keyValue.Key == product.LastOrDefault().Key)
                                if (keyValue.Key == "volume" || keyValue.Key == "total_price")
                                    Query += " inventories." + keyValue.Key + " = " + keyValue.Key + " - " + keyValue.Value + " ";
                                else
                                    Query += " inventories." + keyValue.Key + " = " + keyValue.Value + " ";
                            else
                                if (keyValue.Key == "volume" || keyValue.Key == "total_price")
                                Query += " inventories." + keyValue.Key + " = " + keyValue.Key + " - " + keyValue.Value + ", ";
                            else
                                Query += " inventories." + keyValue.Key + " = " + keyValue.Value + ", ";

                        Query += " WHERE inventories.product_id = " + product["product_id"] + "; ";

                        try
                        {
                            cmd.CommandText = Query;
                            cmd.ExecuteScalar();
                            msg += "\nStock updated for item ";
                        }
                        catch (Exception err)
                        {
                            err.Equals(null);
                            msg += "\nError: " + err.Message;
                        }

                        break;

                    case true:
                        
                        string iSQL = " INSERT INTO inventories (product_id, last_process_id, volume, total_price) VALUES (";
                        iSQL += " " + product["product_id"] + ", ";
                        iSQL += " " + product["last_process_id"] + ", ";
                        iSQL += " " + product["volume"] + ", ";
                        iSQL += " " + product["total_price"];
                        iSQL += "); ";

                        cmd.CommandText = iSQL;

                        try
                        {
                            cmd.CommandText = iSQL;
                            cmd.ExecuteScalar();
                            msg += "\nStock updated for item ";
                        }
                        catch (Exception err)
                        {
                            err.Equals(null);
                            msg += "\nError: " + err.Message;
                        }

                        break;
                }
            }

            try
            {
                cmd.CommandText = "UPDATE orders SET prev_status = cur_status, cur_status = 'SAVED' WHERE ID = " + products[0]["last_process_id"];
                cmd.ExecuteScalar();
                msg += "\nOrder saved.";
            }
            catch (Exception err)
            {
                err.Equals(null);
                msg += "\n" + err.Message;
            }

            Conn.Close();
            return msg;
        }

        public static DataTable GetStocks()
        {
            Conn = new OleDbConnection(ConnectionString);
            Conn.Open();

            cmd.Connection = Conn;

            Query = "SELECT s.product_id, p.product_name, s.volume, p.unit, s.total_price FROM inventories s INNER JOIN products p ON s.product_id = p.id ORDER BY p.product_name";

            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            Conn.Close();
            return dt;
        }

        /***************************************************************************************************************************************************************************************/
    
    }
}
