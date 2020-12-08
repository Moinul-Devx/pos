using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntensePoS.Models
{
    class Purchase
    {
        //  Database
        private static string ConnectionString = Properties.Settings.Default.connString;
        private static OleDbConnection Conn;
        private static OleDbCommand cmd = new OleDbCommand();
        private static string Query = "";

        public static DataTable GetPurchasedItemsByPo(string poNo)
        {         
            Conn = new OleDbConnection(ConnectionString);
            Conn.Open();
            
            cmd.Connection = Conn;

            Query = " SELECT p.ID, i.qty AS Qty, p.unit AS Unit, p.product_name AS ItemName, p.unit_purchase_price AS UnitPurchasePrice, p.unit_price AS UnitSalesPrice, (i.qty * p.unit_purchase_price) AS ItemTotal, p.discount AS Discount, p.vat FROM purchased_items i INNER JOIN products p ON p.ID = i.product_id WHERE i.purchase_id IN (SELECT purchase_id FROM purchases WHERE po_no = '" + poNo + "') ";

            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);            
                
            Conn.Close();
            return dt;
        }

        public static DataTable GetPurchaseByPo(string poNo)
        {
            Conn = new OleDbConnection(ConnectionString);
            Conn.Open();

            cmd.Connection = Conn;

            Query = " SELECT u.*, s.* FROM purchases u INNER JOIN suppliers s ON u.supplier_id = s.supplier_id WHERE po_no = '" + poNo + "' ";

            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            Conn.Close();
            return dt;
        }        

    }
}
