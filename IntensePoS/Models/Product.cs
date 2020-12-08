using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.Models
{
    class Product
    {
        //  Database
        private static string ConnectionString = Properties.Settings.Default.connString;
        private static OleDbConnection Conn;
        private static OleDbCommand cmd = new OleDbCommand();
        private static string Query = "";        

        public static bool UpdateProduct(Dictionary<string, string> product, int productId)
        {
            bool success = false;

            Query = "";

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                err.Equals(null);
                return false;   // FAILED!
            }            
            
            cmd.Connection = Conn;

            Query += " UPDATE products SET ";
            
            foreach (KeyValuePair<string, string> keyValue in product)
                if(keyValue.Key == product.LastOrDefault().Key)
                    Query += " " + keyValue.Key + " = " + keyValue.Value + " ";
                else
                    Query += " " + keyValue.Key + " = " + keyValue.Value + ", ";

            Query += " WHERE product_id = " + productId + "; ";

            try
            {
                cmd.CommandText = Query;                
                cmd.ExecuteScalar();
                success = true;                
            }
            catch (Exception err)
            {
                err.Equals(null);
                success = false;
            }

            Conn.Close();
            return success;
        }
    }
}
