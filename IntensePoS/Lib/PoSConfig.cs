using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IntensePoS.Lib;
using System.Data;

namespace IntensePoS.Lib
{
    static class PoSConfig
    {
        private static bool success = false;
        private static string sysMsg = "";
        private static string ConnectionString = Properties.Settings.Default.connString;
        private static OleDbConnection Conn = new OleDbConnection(ConnectionString);
        private static OleDbCommand cmd = new OleDbCommand();
        private static OleDbTransaction trans;


        //private static bool VerifyDatabase()
        public static bool VerifyDatabase()
        {
            /*
            string dbPath = Directory.GetCurrentDirectory() + @"\db";
            if (!Directory.Exists(dbPath))
                System.IO.Directory.CreateDirectory(dbPath);
            */

            string dbPath = Directory.GetCurrentDirectory();

            WebClient wclient = new WebClient();
            string dbImportUrl = "http://localhost/intense-ecommerce-api/DB/IntensePoS.accdb";
            string dbFileName = dbPath + "\\" + "IntensePoS.accdb";

            if (!File.Exists(dbFileName))
            {
                try
                {
                    wclient.DownloadFile(new Uri(dbImportUrl), dbFileName);
                    return true;
                }
                catch (Exception x)
                {
                    sysMsg = x.Message + "\n" + x.InnerException.Message;
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public static string VerifyAuthKey (string authKey)
        {
            /*
            bool dbVerified = VerifyDatabase();
            if (!dbVerified) return "Couldn't create the database!";
            */

            string authUrl = "http://68.183.231.43/productdetails/verify_pos/";
            var result = PostAuthKey ("API_key=" + authKey, authUrl);

            if (result != null)
            {
                var user = new { id = -1, username = "", email = "", password = "", role = "" };
                var userList = (new[] { user }).ToList();
                var definition = new { success = false, message = "", data = new { id = -1, terminal_name = "", warehouse_id = -1, shop_id = -1, site_id = -1, API_key = "", date_creation = "", admin_id = -1, is_active = false, users = userList } };
                var response = JsonConvert.DeserializeAnonymousType(result, definition);

                try
                {
                    Conn.Open();
                    string sQL = "";

                    sQL += "INSERT INTO __intense_terminal (server_id, terminal_name, warehouse_id, shop_id, site_id, API_key, date_creation, admin_id, is_active) VALUES ";
                    sQL += " (";
                    sQL += response.data.id + ",";
                    sQL += "'" + response.data.terminal_name + "'" + ",";
                    sQL += response.data.warehouse_id + ",";
                    sQL += response.data.shop_id + ",";
                    sQL += response.data.site_id + ",";
                    sQL += "'" + response.data.API_key + "'" + ",";
                    sQL += "'" + response.data.date_creation + "'" + ",";
                    sQL += response.data.admin_id + ",";
                    sQL += response.data.is_active;
                    sQL += " )";

                    trans = Conn.BeginTransaction();

                    try
                    {
                        cmd.Connection = Conn;

                        cmd.CommandText = sQL; cmd.Transaction = trans;
                        cmd.ExecuteScalar();

                        sysMsg += "Creating new terminal...\n";

                        sQL = "";

                        foreach (var posUser in response.data.users)
                        {
                            sQL += "DELETE FROM __intense_user WHERE server_id = " + posUser.id + ";";

                            try
                            {
                                cmd.CommandText = sQL; cmd.Transaction = trans;
                                cmd.ExecuteScalar();
                                sQL = "";
                                sysMsg += "Refreshing user list...\n";
                            }
                            catch (Exception x)
                            {
                                trans.Rollback();
                                Conn.Close();
                                sysMsg += "Refreshing user list...\nError!\n" + x.Message + "\n";
                            }

                            sQL += "INSERT INTO __intense_user (server_id, username, _password, email, _role) VALUES";
                            sQL += " (";
                            sQL += posUser.id + ", ";
                            sQL += "'" + posUser.username + "'" + ", ";
                            sQL += "'" + posUser.password + "'" + ", ";
                            sQL += "'" + posUser.email + "'" + ", ";
                            sQL += "'" + posUser.role + "'";
                            sQL += " )";
                            sQL += ";";

                            try
                            {
                                cmd.CommandText = sQL; cmd.Transaction = trans;
                                cmd.ExecuteScalar();
                                sQL = "";
                                sysMsg += "Refreshing user list...\n";
                            }
                            catch (Exception x)
                            {
                                trans.Rollback();
                                Conn.Close();
                                sysMsg += "\nRefreshing user list...\nError!\n" + x.Message;
                            }

                            try
                            {
                                string PIN = Util.GeneratePIN().ToString().Trim();
                                sQL = "";
                                /*
                                sQL = "INSERT INTO [users] ([sync_id], [email], [password], [terminal_server_id], [PIN]) ";
                                //sQL += " SELECT [server_id], [email], [_password], " + response.data.id + " AS [terminal_server_id], Int((499 - 100 + 1) * Rnd + 100) AS [PIN] " + "FROM [__intense_user] ";
                                sQL += " SELECT [server_id], [email], [_password], " + response.data.id + " AS [terminal_server_id], " + PIN + " AS [PIN] " + "FROM [__intense_user] ";
                                */

                                sQL += " UPDATE ";

                                sQL += " ( ";

                                sQL += " [users] [u] RIGHT JOIN ";

                                sQL += " ( ";
                                
                                //      sQL += " SELECT [server_id], [username], [email], [_password], 2 AS [terminal_server_id], " + PIN + " AS [PIN] FROM [__intense_user] ";

                                //Int((10-1+1)*Rnd()+1)
                                sQL += " SELECT [server_id], [username], [email], [_password], 2 AS [terminal_server_id], Int((99-10+1) * Rnd() + 10) AS [PIN] FROM [__intense_user] ";

                                sQL += " ) [i] ";

                                sQL += " ON [i].[server_id] = [u].[sync_id] ";

                                sQL += " ) ";

                                sQL += " SET ";

                                sQL += " [u].[sync_id] = [i].[server_id], ";
                                sQL += " [u].[username] = [i].[username], ";
                                sQL += " [u].[email] = [i].[email], ";
                                sQL += " [u].[password] = [i].[_password], ";
                                sQL += " [u].[terminal_server_id] = [i].[terminal_server_id], ";
                                sQL += " [u].[PIN] = [i].[PIN] & [i].[server_id] ";

                                cmd.CommandText = sQL; cmd.Transaction = trans;
                                cmd.ExecuteScalar();
                                sQL = "";
                                sysMsg += "User refreshed.\n***************\nPIN: " + PIN + "\n***************\n";
                            }
                            catch (Exception x)
                            {
                                trans.Rollback();
                                Conn.Close();
                                sysMsg += "\nRefreshing user list...\nError!\n" + x.Message;
                            }

                        }

                        trans.Commit();
                        Conn.Close();
                        sysMsg += "Terminal created.\n";

                    }
                    catch (Exception x)
                    {
                        trans.Rollback();
                        Conn.Close();
                        sysMsg += "\nError!\n" + x.Message;
                    }

                    Conn.Close();
                }
                catch (Exception err)
                {
                    Conn.Close();
                    sysMsg += "Error!\n";
                    err.Equals(null);
                    return sysMsg;
                }

                if (!response.message.Contains("!"))
                    sysMsg += "\n" + response.message + "!";
                else
                    sysMsg += "\n" + response.message;

            }
            else
                sysMsg += "\nError!\nAuthentication Failed. Please check your internet connection and try again.";

            return sysMsg;
        }

       

        private static string PostAuthKey (string postData, string URL)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent =
                              "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)";
            request.Accept = "application/json";
            request.UseDefaultCredentials = true;
            request.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "application/x-www-form-urlencoded";                  
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            string responseFromServer;

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


        public static bool Login(string accessPin)
        {
            sysMsg = "";

            string email = "", password = "";

            try
            {
                if (Conn != null && Conn.State == ConnectionState.Closed)
                    Conn.Open();
                else
                {
                    Conn = new OleDbConnection(ConnectionString);
                    Conn.Open();
                }

                cmd = new OleDbCommand();
                cmd.Connection = Conn;
                cmd.CommandText = " SELECT * FROM users WHERE PIN = " + accessPin.Trim() ;
                
                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                DataTable dt = new DataTable();
                
                try { da.Fill(dt); email = dt.Rows[0]["email"].ToString(); password = dt.Rows[0]["password"].ToString(); } catch (Exception x) { sysMsg += x.Message + "\n"; }
                
            }
            catch (Exception x)
            {                
                Conn.Close();
                sysMsg += "\nAuthenticating user...\nError!\n" + x.Message;
            }

            string URL = "http://68.183.231.43/user/dummy_login/";                    ///    "https://tes.com.bd/user/dummy_login/";

            try
            {
                var result = SyncLoginPostMethod("email=" + email + "&password=" + password, URL);

                if (result != null)
                {
                    var definition = new { success = false, message = "", user = new { user_email = "", user_id = -1, role = "" } };

                    try
                    {
                        var response = JsonConvert.DeserializeAnonymousType(result, definition);

                        sysMsg += response.message + "\n";
                        success = response.success;
                    }
                    catch (Exception x)
                    {
                        sysMsg += x.Message + "\n";
                    }
                }
            }
            catch(Exception x)
            {
                sysMsg += x.Message + "\n";
            }

            return success;
        }

        private static string SyncLoginPostMethod(string postData, string URL)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;
            ((HttpWebRequest)request).UserAgent =
                              "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)";
            request.Accept = "application/json";
            request.UseDefaultCredentials = true;
            request.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentType = "application/x-www-form-urlencoded";                  //       "application/json";               //"application/x-www-form-urlencoded";
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
