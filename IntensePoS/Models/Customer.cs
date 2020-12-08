using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IntensePoS.ApiModels.V3;
using System.Data.OleDb;

namespace IntensePoS.Models
{
    static class Customer
    {
        //  Database
        private static string ConnectionString = Properties.Settings.Default.connString;
        private static OleDbConnection Conn;
        private static OleDbCommand cmd = new OleDbCommand();
        private static OleDbTransaction trans;        

        public static CustomerResult GetCustomer(string phone_number)
        {
            CustomerResult result = new CustomerResult();
            bool success = false;
            string sysMsg = "";

            WebClient wclient = new WebClient();
            wclient.Headers.Add("Accept", "application/json");

            string json = "";

            JsonSerializerSettings settings = new JsonSerializerSettings();

            try
            {
                string URI = "http://68.183.231.43/Cart/check_user/";
                // string myParameters = "username=-&email=-&phone_number=" + phone_number;
                wclient.Headers[HttpRequestHeader.ContentType] = "application/json";                                           // "application/x-www-form-urlencoded";

                string data = "{ \"user\" : { \"username\" : \"-\", \"email\" : \"-\", \"phone_number\" : \"" + phone_number + "\" } }";

                json = @wclient.UploadString(URI, data);

                try
                {
                    settings.MissingMemberHandling = MissingMemberHandling.Error;                    
                    CustomerResult root = JsonConvert.DeserializeObject<CustomerResult>(json, settings);
                    success = root.success;

                    if (success)
                    {
                        IntensePoS.ApiModels.V3.Customer customer = root.data;
                        IntensePoS.ApiModels.V3.User user = new IntensePoS.ApiModels.V3.User();
                        
                        user.id = customer.user_id;
                        user.username = customer.username;
                        user.email = customer.email;
                        user.phone_number = customer.phone_number;
                        user.role = customer.role;

                        Conn = new OleDbConnection(ConnectionString);
                        Conn.Open();
                        trans = Conn.BeginTransaction();
                        cmd = new OleDbCommand();
                        cmd.Connection = Conn;

                        if (customer != null)
                        {
                            success = Order.SyncOrderCustomer(user, cmd, trans);                           //  SaveCustomer(customer);   // NO NEED!
                            trans.Commit();
                            Conn.Close();
                            result = root;
                        }
                    }                    
                }
                catch (Exception x) 
                { 
                    sysMsg += "Find Customer\nError!\n" + x.Message;
                    success = false;
                }

            }
            catch (Exception e)
            {
                sysMsg += "\n[Connect Server]\n" + e.Message;
                success = false;
            }

            //return success;
            return result;
        }


        public static bool SaveCustomer(IntensePoS.ApiModels.V3.Customer customer)
        {
            bool success = false;
            string sysMsg = "";

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
                cmd = new OleDbCommand();
                cmd.Connection = Conn;

                cmd.CommandText = " INSERT INTO customers (user_id, customer_name, customer_email, customer_mobile) VALUES (" + customer.user_id + "'" + customer.username + "', '" + customer.email + "', '" + customer.phone_number + "')";
                var result = cmd.ExecuteScalar();
                int iresult = 0;

                if (Int32.TryParse(result.ToString(), out iresult) && iresult > 0)
                    success = true;
            }
            catch (Exception x)
            {
                sysMsg = x.Message;
                success = false;
            }

            return success;
        }


    }
}
