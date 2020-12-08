using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IntensePoS.ApiModels;
using System.Data.OleDb;
using System.Data.SqlClient;
using IntensePoS.Lib;
using System.Configuration;
using IntensePoS.ApiModels.V2;

namespace IntensePoS
{
    public partial class InventoryScr : Form
    {

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///        
        string ConnectionString = Properties.Settings.Default.connString;
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        public InventoryScr()
        {
            InitializeComponent();
        }

        private void InventoryScr_Load(object sender, EventArgs e)
        {
            /// PoS Testing Console (Developers & Testers)
            /// ==========================================
            ///             
            
            // this.ShowInTaskbar = false;      // will work later if necessary.
        }

        #region Version1
        private void GetSyncedInventory()
        {
            /// Steps ///
            /// 
            /// 1. Connect API
            /// 2. GET JSON string and deserialize to C# (model) objects
            /// 3. Populate synced data into the local sync table(s)     -- e.g.: now only __intense_product_API
            /// 4. UPSERT from the cloned to the destination table.
            /// 5. TRUNCATE the cloned table.            


            WebClient wclient = new WebClient();
            wclient.Headers.Add("Accept", "application/json");
            
            string json = "";
            
            try
            {
                json = @wclient.DownloadString("http://localhost/intense-ecommerce-api/test-products-get-1.json");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Connection error!", MessageBoxButtons.OK, MessageBoxIcon.Error);                
                return;
            }


            JsonSerializerSettings settings = new JsonSerializerSettings();

            try
            {
                settings.MissingMemberHandling = MissingMemberHandling.Error;
                ApiModels.Inventory inventory = JsonConvert.DeserializeObject<ApiModels.Inventory>(json, settings);                
                
                dgvSyncedProducts.DataSource = inventory.products;

                DataTable dtSchema = inventory.products.ToDataTable<Product>();

                /// Excluded fields where most of them have null reference.
                string[] columnsXclude = {
                                                    "seller",               //?
                                                    "sellerId",             //?
                                                    "product_status",
                                                    "purchase_price",
                                                    "old_price",
                                                    "new_price",
                                                    "discount_type",
                                                    "discount_amount",
                                                    "discount_start_date",
                                                    "discount_end_date",
                                                    "point",
                                                    "point_start_date",
                                                    "point_end_date",
                                                    "productImages",
                                                    "variant",
                                                    "quantity",
                                                    "category",
                                                    "sub_category",
                                                    "sub_sub_category",
                                                    "productSpecifications"
                                              };

                PopulateDatabase(inventory, dtSchema, "__intense_product_API", columnsXclude);

                MessageBox.Show("Products sync successful!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception x)
            {                
                MessageBox.Show(x.Message);              
            }

        }

        private void SyncPostResult ()
        {
            /// Steps, (NOT FOR THE ORIGINAL POST) ///
            /// 
            /// 1. Connect API
            /// 2. GET JSON string and deserialize to C# (model) objects
            /// 3. Populate synced data into the local sync table(s)     -- e.g.: now only __intense_product_API
            /// 4. UPSERT from the cloned to the destination table.
            /// 5. TRUNCATE the cloned table.            


            WebClient wclient = new WebClient();
            wclient.Headers.Add("Accept", "application/json");

            string json = "";

            try
            {
                json = @wclient.DownloadString("http://localhost/intense-ecommerce-api/test.json");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Connection error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            JsonSerializerSettings settings = new JsonSerializerSettings();

            try
            {
                settings.MissingMemberHandling = MissingMemberHandling.Error;
                Inventory inventory = JsonConvert.DeserializeObject<Inventory>(json, settings);                

                // Make a list
                List<Product> products = new List<Product>(); products.Add(inventory.product);

                // Assign the list to the inventory.products list
                inventory.products = products;

                DataTable dtSchema = products.ToDataTable<Product>();
                dgvSyncedProducts.DataSource = dtSchema;
                dgvSyncedProducts.Refresh();

                /// Excluded fields where most of them have null reference.
                string[] columnsXclude = {
                                                    "seller",               //?
                                                    "sellerId",             //?                                                    
                                                    "purchase_price",
                                                    "old_price",
                                                    "new_price",
                                                    "discount_type",
                                                    "discount_amount",
                                                    "discount_start_date",
                                                    "discount_end_date",
                                                    "point",
                                                    "point_start_date",
                                                    "point_end_date",
                                                    "productImages",
                                                    "variant",
                                                    "quantity",
                                                    "category",
                                                    "sub_category",
                                                    "sub_sub_category",
                                                    "productSpecifications"
                                              };


                PopulateDatabase(inventory, dtSchema, "__intense_product_API", columnsXclude);

                MessageBox.Show("Newly created product synced successfuly!", "Success without POST!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }

        }


        private void PopulateDatabase(Inventory inventory, DataTable dtSchema, string tableName, string[] columnsXclude)
        {
            
            using (OleDbConnection con = new OleDbConnection(ConnectionString))
            {
                try
                {                          
                    List<string> col = new List<string>();
                    StringBuilder sb = new StringBuilder();

                    StringBuilder insertQuery = new StringBuilder();
                    string columnNames = "";

                    List<DataColumn> cols = new List<DataColumn>();

                    

                    foreach (DataColumn column in dtSchema.Columns)                    
                        if (!columnsXclude.Contains(column.ColumnName))                        
                            cols.Add(column);                                            

                    foreach (DataColumn column in cols)
                    {
                        if (cols[cols.Count - 1] != column)                            
                            columnNames += column.ColumnName + ", ";
                        else 
                            columnNames += column.ColumnName;
                    }

                    

                    foreach (DataRow row in dtSchema.Rows) 
                    {
                        insertQuery.Append("INSERT INTO " + tableName + " (");

                        insertQuery.Append(columnNames);

                        insertQuery.Append(") VALUES ");

                        insertQuery.Append("(");
                        
                        Product product = inventory.products[dtSchema.Rows.IndexOf(row)];
                        

                        foreach (DataColumn column in cols)
                        {
                            var r = row[column.ColumnName];

                            

                                switch (column.DataType.Name)
                                {
                                    case "String":
                                    case "DateTime":
                                    
                                    if (cols[cols.Count - 1] != column)
                                        insertQuery.Append("'" + r + "'" + ", ");
                                        else
                                            insertQuery.Append("'" + r + "'");
                                        break;

                                    case "String[]":
                                        string[] key_features = product.key_features;
                                        insertQuery.Append("'");

                                    
                                    if (cols[cols.Count - 1] != column)
                                        insertQuery.Append(string.Join(", ", key_features) + "'" + ", ");
                                    else
                                        insertQuery.Append(string.Join(", ", key_features) + "'");

                                    break;

                                    default:
                                    
                                    if (cols[cols.Count - 1] != column)
                                        insertQuery.Append(r.ToString()+ ", ");
                                        else
                                            insertQuery.Append(r.ToString());
                                        break;
                                }
                            
                        }
                        
                            insertQuery.Append(");");
                        
                    }                    

                    txtSqlQuery.Text =  insertQuery.ToString();
                    OleDbCommand cmd;

                    try
                    {

                        
                        string str = insertQuery.ToString();
                        // Now here 4 items in the array after splitting semicolons (total 3) at the end of the each statement string.
                        string[] sqlArr = str.Split(';');       
                        // So remove the last one that is empty.
                        sqlArr = sqlArr.Take(sqlArr.Length - 1).ToArray();
                        
                        // INSERT to sync table
                        for (int i=0; i < sqlArr.Length; i++)
                        {
                            con.Open();
                            cmd = new OleDbCommand();
                            cmd.Connection = con;
                            string sql = sqlArr[i] + ";";
                            cmd.CommandText = sql;                            
                            cmd.ExecuteReader();
                            con.Close();
                        }

                        
                        // UPSERT to original table
                        string uSql = " UPDATE __intense_product_API api LEFT JOIN __intense_product p ON [api].[server_id] = [p].[server_id] SET ";
                        
                        //cols.RemoveAt(0);  // Don't remove the server_id that needs to go to the destination table, p

                        foreach (DataColumn column in cols)
                        {                            

                            if (cols[cols.Count - 1] != column)                                        
                                uSql += " [p].[" + column.ColumnName + "] = [api].[" + column.ColumnName + "], ";
                            else
                                uSql += " [p].[" + column.ColumnName + "] = [api].[" + column.ColumnName + "]";
                        }

                        uSql += "; ";

                        txtSqlQuery.Text += "\r\n--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------\r\n" + uSql;

                        con.Open();
                        cmd = new OleDbCommand();
                        cmd.Connection = con;
                        cmd.CommandText = uSql;
                        cmd.ExecuteReader();
                        con.Close();

                        // TRUNCATE the API sync table in Access way
                        con.Open();
                        cmd = new OleDbCommand();
                        cmd.Connection = con;
                        cmd.CommandText = "DELETE FROM __intense_product_api";
                        cmd.ExecuteScalar();
                        // reset the column, ID to count from 1
                        cmd.CommandText = "ALTER TABLE __intense_product_api ALTER COLUMN ID COUNTER";
                        cmd.ExecuteNonQuery();                        
                        con.Close();

                    }
                    catch (Exception err)
                    {                          
                        MessageBox.Show(err.Message);                        
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btnGetSyncedProducts_Click(object sender, EventArgs e)
        {
            GetSyncedInventory();
        }

        private void btnSyncPostResult_Click(object sender, EventArgs e)
        {
            SyncPostResult();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtSqlQuery.Text = "";
            dgvSyncedProducts.DataSource = null;
            dgvSyncedProducts.Refresh();
        }
        #endregion

        #region Version2

        #endregion
    }
}
