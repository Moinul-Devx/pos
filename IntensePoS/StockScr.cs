using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using IntensePoS.Lib;
using IntensePoS.Models;

namespace IntensePoS
{
    public partial class StockScr : Form
    {
        //  Database
        private string ConnectionString = Properties.Settings.Default.connString;
        private OleDbConnection Conn = null;        
        private string Query = "";
        private string ErrorMessage = "";
        
        // Purchase
        private bool isPurchase = false;
        private int purchaseId = 0;
        private int iPurchaseId = 0;
        private Dictionary<string, string> purchase = new Dictionary<string, string>();
        Dictionary<string, string> purchasedItem = new Dictionary<string, string>();

        // Supplier
        private bool isSupplier = false;
        string supplierId = "0";
        string iSupplierId = "0";
        private string supplierName = "";
        private string supplierEmail = "";
        private string supplierMobile = "";

        // selected items array
        int[] selectedItemsId = new int[0];

        // purchased units
        string[] pUnits = new string[0];

        // old row on current datagridview row
        DataGridViewRow oRow = new DataGridViewRow();

        // old cell on current datagridview cell
        string oValue;

        // Stock
        bool stockUpdated = true;   // For initial inventory status update on dgvProducts

        public StockScr()
        {
            InitializeComponent();
        }
        
        public StockScr (int i)
        {
            this.defaultTab = i;
            InitializeComponent();
        }


        private void btnCLose_Click(object sender, EventArgs e)
        {
           this.Close();
        }

        private bool StateChecked()
        {
            bool state = false;

            if (isPurchase && iPurchaseId > 0 || isPurchase && purchaseId > 0)
            {
                var msg = MessageBox.Show("You have an unsaved purchase order. Are you sure to cancel? \n\nN.B.: This PO will be saved with 'OPEN' status. You can process it later.", "Unsaved Purchase Order!", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                if (msg == DialogResult.Yes)
                    state = true;
            }

            return state;
        }

        private void StockScr_Load(object sender, EventArgs e)
        {
            LoadUISettings();            
            iTabControl.SelectedIndex = defaultTab;                                   
        }

        private void ReturnState()
        {
            /// Why??
            /// There is a problem working with TabStrip control.
            /// Always focus captured by the TabStrip control in any state.

            // Creating PO State
            if (isPurchase && iPurchaseId > 0)
            {
                if (keepSupplierHandle && !purchase.ContainsKey("supplier"))
                {                    
                    txtSupplierMobile.Focus();
                }
            }
            else
                cmbProductScan.Focus();
        }

        private void InitializePO()
        {
            
            /// Block this portion as focus always goes to iTabControl. Also ignore the ReturnState() on loading the main inventory screen
            if (isPurchase && iPurchaseId > 0)
                if (keepSupplierHandle && !purchase.ContainsKey("supplier")) 
                    ShowSupplierPane();


            if (purchase.Count > 0)
            {
                cmbProductScan.Focus();
                return;
            }

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            OleDbCommand cmd = new OleDbCommand();
            cmd.Connection = Conn;

            Query = "Select MAX(ID) AS ID FROM purchases WHERE DateValue(po_date_time) = Date()";
            cmd.CommandText = Query;

            string result = "";

            try
            {
                result = cmd.ExecuteScalar().ToString();
            }
            catch(Exception x)
            {
                MessageBox.Show(x.Message);
            }

            if (Util.IsNumeric(result))
                iPurchaseId = int.Parse(result) + 1;
            else
                iPurchaseId++;
            
            isPurchase = true;     // i.e.: Now, there is an active purchase order that is on process.            
            Conn.Close();

            string po_str = iPurchaseId.ToString();
            if (po_str.Length >= 3)
                po_str = po_str.Remove(0, po_str.Length - 3);
            else
                for(int i=0; i<3; i++)
                {
                    po_str = "0" + po_str;
                    if (po_str.Length == 3) break;
                }

            string po_no = "P/" + DateTime.Now.ToString("dd/MM/yy") + "/" + po_str ;

            purchase.Add("iPurchaseId", iPurchaseId.ToString());
            purchase.Add("po_no", po_no);

            ///
            purchase.Add("supplier_id", supplierId);
            purchase.Add("po_date_time", System.DateTime.Now.ToString("dd/MM/yyyy"));
            purchase.Add("due_date_time", System.DateTime.Now.ToString("dd/MM/yyyy"));

            ////// initial values
            purchase.Add("sub_total", "0.00");
            purchase.Add("num_items", "0");
            purchase.Add("debit", "0.00");
            purchase.Add("credit", "0");

            if (dgvPurchaseHeader.Rows.Count == 0)
                dgvPurchaseHeader.Rows.Add("PO No.", po_no, "Purchase Date", System.DateTime.Now.ToString("dd/MM/yyyy"), "Due Date", System.DateTime.Now.ToString("dd/MM/yyyy"), "Supplier", "Not Selected!");
            else
            {
                dgvPurchaseHeader.Rows[0].Cells[1].Value = po_no;
                dgvPurchaseHeader.Rows[0].Cells[3].Value = System.DateTime.Now.ToString("dd/MM/yyyy");
                dgvPurchaseHeader.Rows[0].Cells[5].Value = System.DateTime.Now.ToString("dd/MM/yyyy");
                dgvPurchaseHeader.Rows[0].Cells[7].Value = "Not Selected!";
            }

            /// Keep the line. Work later. Don't delete.
            //dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = dgvPurchaseHeader.Rows[0].Cells[7].Style.SelectionForeColor = Color.Salmon;

            if (!keepSupplierHandle) // DON'T CALL AGAIN IF ALREADY CALLED!
                ShowSupplierPane();
            else
                cmbProductScan.Focus();
        }


        int defaultTab = 0;

        private void LoadUISettings()
        {
            // PURCHASE HEADER
            dgvPurchaseHeader.Columns[0].Width = dgvPurchaseHeader.Columns[1].Width = dgvPurchaseHeader.Columns[2].Width = dgvPurchaseHeader.Columns[3].Width = dgvPurchaseHeader.Columns[4].Width = dgvPurchaseHeader.Columns[5].Width = dgvPurchaseHeader.Columns[6].Width = dgvPurchaseHeader.Width / 8;
            dgvPurchaseHeader.Columns[7].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            
            // PURCHASED ITEMS
            dgvPurchaseDetails.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
            dgvPurchaseDetails.Columns[3].Width = dgvPurchaseDetails.Columns[4].Width = dgvPurchaseDetails.Columns[5].Width = dgvPurchaseDetails.Columns[7].Width = dgvPurchaseDetails.Columns[8].Width = dgvPurchaseDetails.Columns[3].Width + 80;
            dgvPurchaseDetails.Columns[6].Width = dgvPurchaseDetails.Columns[6].Width - dgvPurchaseDetails.Columns[3].Width/2/3;
            dgvPurchaseDetails.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // PURCHASE SUMMARY
            tOrderPane.Height = purchasePane.Height - (tLayoutPurchaseTop.Height + tOrderFooterPane.Height);
            dgvPurchaseSummary.Columns[2].Width = dgvPurchaseSummary.Width / 3;            
            PurchaseSummaryHeadsShowUp();
            dgvPurchaseSummary.Columns[0].Width = dgvPurchaseDetails.Columns[0].Width;            
            dgvPurchaseSummary.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvPurchaseSummary.Columns[2].Width = dgvPurchaseDetails.Columns[0].Width + dgvPurchaseDetails.Columns[2].Width + dgvPurchaseDetails.Columns[3].Width + dgvPurchaseDetails.Columns[4].Width + dgvPurchaseDetails.Columns[6].Width + dgvPurchaseDetails.Columns[7].Width - dgvPurchaseSummary.Columns[0].Width - dgvPurchaseSummary.Columns[1].Width + dgvPurchaseDetails.Columns[8].Width * 50/100;
            dgvPurchaseSummary.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvPurchaseSummary.Rows[0].Cells[0].Style.SelectionForeColor = dgvPurchaseSummary.Rows[0].Cells[0].Style.ForeColor = Color.DarkOrange;


            // INVENTORY STATUS
            dgvProducts.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
            dgvProducts.Columns[3].Width = dgvPurchaseDetails.Columns[3].Width - dgvPurchaseDetails.Columns[0].Width / 2 / 3;
            dgvProducts.Columns[4].Width = dgvPurchaseDetails.Width / 5;
            dgvProducts.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        }

        private void PurchaseSummaryHeadsShowUp()
        {                        
            dgvPurchaseSummary.Rows.Add("", "", "Total", "0.00");            
            dgvPurchaseSummary.Rows.Add("", "", "Debit", "0.00");
            dgvPurchaseSummary.Rows.Add("", "", "Credit", "0.00");            
        }

                
        private void btnPurchase_Click(object sender, EventArgs e)
        {
            iTabControl.SelectedTab = tabPurchase;
        }        

        private void dgvPurchaseHeader_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            DataGridViewRow row = dgvPurchaseHeader.Rows[0];
            DataGridViewCell cell = dgvPurchaseHeader.SelectedCells[0];

            if (cell == row.Cells[3])
            {
                mCalDate.Visible = true;
                mCalDate.Top = tOrderPane.Parent.Parent.Parent.Parent.Top + tOrderPane.Parent.Parent.Parent.Top + tOrderPane.Parent.Parent.Top + tOrderPane.Parent.Top + tOrderPane.Top + dgvPurchaseHeader.Top + dgvPurchaseHeader.Height;
                mCalDate.Left = dgvPurchaseHeader.Left + dgvPurchaseHeader.Columns[0].Width + dgvPurchaseHeader.Columns[1].Width + dgvPurchaseHeader.Columns[2].Width;                
                SetCalDate(cell);
            }
            else if (cell == row.Cells[5])
            {
                mCalDate.Visible = true;
                mCalDate.Top = tOrderPane.Parent.Parent.Parent.Parent.Top + tOrderPane.Parent.Parent.Parent.Top + tOrderPane.Parent.Parent.Top + tOrderPane.Parent.Top + tOrderPane.Top + dgvPurchaseHeader.Top + dgvPurchaseHeader.Height;
                mCalDate.Left = dgvPurchaseHeader.Left + dgvPurchaseHeader.Columns[0].Width + dgvPurchaseHeader.Columns[1].Width + dgvPurchaseHeader.Columns[2].Width + dgvPurchaseHeader.Columns[3].Width + dgvPurchaseHeader.Columns[4].Width;                
                SetCalDate(cell);
            }
            else if (cell == row.Cells[dgvPurchaseHeader.Columns.Count - 1] && cell.Value == null ) //|| cell == row.Cells[dgvPurchaseHeader.Columns.Count - 1] && cell.Value.ToString() == "")
            {
                ShowSupplierPane(sender);
            }
        }

        private void ShowSupplierPane(object sender = null)
        {
            // Keep the line. Don't delete. Work later.
            //if (!isSupplier) dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Red; else dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Black;

            // precaution!
            if (!isSupplier) txtSupplierName.Text = txtSupplierEmail.Text = txtSupplierMobile.Text = "";

            dgvPurchaseHeader.EndEdit();
            SupplierPane.Visible = true;
            SupplierPane.Top = tOrderPane.Parent.Parent.Parent.Parent.Top + tOrderPane.Parent.Parent.Parent.Top + tOrderPane.Parent.Parent.Top + tOrderPane.Parent.Top + tOrderPane.Top + dgvPurchaseHeader.Top + dgvPurchaseHeader.Height;
            SupplierPane.Left = purchasePane.Width - SupplierPane.Width - 0;
            keepSupplierHandle = true;

            if (isSupplier && purchase.ContainsKey("supplier"))
            {
                txtSupplierName.Text = supplierName;
                txtSupplierEmail.Text = supplierEmail;
                txtSupplierMobile.Text = supplierMobile;
            }
           
            txtSupplierMobile.Focus();  // Will not work as the edit mode on dgvPurchaseHeader
        }

        private void SetCalDate(DataGridViewCell cell)
        {
            // Prevent at first! System.NullReferenceException: 'Object reference not set to an instance of an object.'
            if (cell.Value == null) return;

            DateTime tDate = DateTime.ParseExact(cell.Value.ToString(), "dd/MM/yyyy", CultureInfo.InvariantCulture);            
            mCalDate.TodayDate = tDate;
            mCalDate.SelectionStart = tDate;
            mCalDate.SelectionEnd = tDate;            
            return;            
        }

        private void mCalDate_DateSelected(object sender, DateRangeEventArgs e)
        {
            mCalDate.Select();
            dgvPurchaseHeader.SelectedCells[0].Value = mCalDate.SelectionStart.ToString("dd/MM/yyyy");
            purchase["po_date_time"] = dgvPurchaseHeader.Rows[0].Cells[3].Value.ToString();
            purchase["due_date_time"] = dgvPurchaseHeader.Rows[0].Cells[5].Value.ToString();
            mCalDate.Visible = false;
        }

        private void iTabControl_KeyPress(object sender, KeyPressEventArgs e)        
        {
            if (e.KeyChar == 13)
            {
                /********************** STATE MANAGEMENT CRITICAL ********************/
                /*
                if(!isSupplier)
                    if(gbxSupplier.Visible) dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Red; else dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Salmon;
                else
                    dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Black;
                */
                /*********************************************************************/
                
                switch (iTabControl.SelectedIndex)
                {
                    case 0:
                        cmbProductSearch.Focus();
                        break;

                    case 1:

                        if (keepSupplierHandle) txtSupplierMobile.Focus();
                        else
                        {
                            HidePickers();
                            cmbProductScan.Focus();
                        }

                        break;
                }
                
            }
        }

        private void iTabControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
                ReturnState();
        }        

        private void lblPurchaseCaption_Click(object sender, EventArgs e)
        {
            ReturnState();
        }

        private void dgvPurchaseHeader_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            HidePickers();           
        }

        private void mCalDate_Leave(object sender, EventArgs e)
        {
            mCalDate.Visible = false;
        }

        private void txtSupplierName_Leave(object sender, EventArgs e)
        {
            HideSupplierPane(sender);
        }

        private void HideSupplierPane(object sender = null)
        {
            //  if (keepSupplierHandle) return;     // No, let the user do whatever the choice either adding items or else.

            if (dgvPurchaseHeader.CurrentCell != dgvPurchaseHeader.Rows[0].Cells[dgvPurchaseHeader.ColumnCount - 1] || dgvPurchaseHeader.CurrentCell == dgvPurchaseHeader.Rows[0].Cells[dgvPurchaseHeader.ColumnCount - 1] && !gbxSupplier.Controls.Contains(this.ActiveControl))
            {
                /// Keep the line. Work later. Don't delete.
                //if(!isSupplier) dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Salmon; else dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Black;

                keepSupplierHandle = false;
                SupplierPane.Visible = false;                
            }
        }

        bool keepSupplierHandle = false;
        private void HidePickers()
        {
            // Prevent runtime error for Index out of range.
            if (dgvPurchaseHeader.Rows.Count == 0) return;

            if (dgvPurchaseHeader.SelectedCells[0] == dgvPurchaseHeader.Rows[0].Cells[3] || dgvPurchaseHeader.SelectedCells[0] == dgvPurchaseHeader.Rows[0].Cells[5])
            {
                if (this.ActiveControl.Name == "mCalDate")
                    mCalDate.Visible = true;
                else
                    mCalDate.Visible = false;

                keepSupplierHandle = false;
                SupplierPane.Visible = false;
            }
            else if (dgvPurchaseHeader.SelectedCells[0] == dgvPurchaseHeader.Rows[0].Cells[7])
            {
                if (!gbxSupplier.Controls.Contains(this.ActiveControl))
                    SupplierPane.Visible = false;

                mCalDate.Visible = false;
            }
            else
            {
                /// HIDE ALL!
                /*
                keepSupplierHandle = false;
                SupplierPane.Visible = false;
                */
                HideSupplierPane();
                mCalDate.Visible = false;
            }

            if(gbPaidAmount.Visible==true)
            {
                txtPaymentAmount.Text = string.Empty;
                gbPaidAmount.Visible = false;
                cmbProductScan.Focus();
            }
        }

        private void txtSupplierEmail_Leave(object sender, EventArgs e)
        {
            HideSupplierPane(sender);
        }

        private void btnCancelSupplierWin_Leave(object sender, EventArgs e)
        {
            HideSupplierPane(sender);
        }

        private void btnSaveSupplier_Click(object sender, EventArgs e)
        {
            switch (btnSaveSupplier.Text)
            {
                case "Select":                    
                    supplierName = txtSupplierName.Text;
                    supplierEmail = txtSupplierEmail.Text;
                    supplierMobile = txtSupplierMobile.Text;
                    isSupplier = true;
                    dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Black;

                    supplierId = iSupplierId;
                    if (purchase.ContainsKey("supplier_id")) purchase["supplier_id"] = supplierId; else purchase.Add("supplier_id", supplierId);

                    CancelSavingSupplier();
                    break;

                case "Change":
                    txtSupplierName.Text = txtSupplierEmail.Text = txtSupplierMobile.Text = "";
                    btnSaveSupplier.Enabled = false;
                    btnSaveSupplier.BackColor = Color.AliceBlue;
                    btnSaveSupplier.ForeColor = SystemColors.ControlDark;
                    txtSupplierMobile.Focus();
                    break;

                case "Find":
                    FindSupplier();
                    break;

                case "Save":
                    SaveSupplier();
                    break;
            }            
        }


        private void FindSupplier()
        {
            string input = txtSupplierMobile.Text.Replace("\r\n", "");


            txtSupplierMobile.Text = txtSupplierMobile.Text.Replace("\r\n", "");                    // IMPORTANT! JUST THIS LINE PREVENTS TO GO NEXT WITHOUT MOBILE NO.

            if (txtSupplierMobile.Text == "")
            {
                // Supplier
                btnSaveSupplier.BackColor = Color.AliceBlue;
                btnSaveSupplier.ForeColor = SystemColors.ControlDark;
                btnSaveSupplier.Text = "Find";
                txtSupplierMobile.Text = "";
                return;
            }

            if (btnSaveSupplier.Text == "Select" && txtSupplierMobile.Text.Length > 0)
            {
                dgvPurchaseHeader.Rows[8].Cells[1].Value = txtSupplierMobile.Text;
                supplierId = iSupplierId;
                supplierName = txtSupplierName.Text;
                supplierEmail = txtSupplierEmail.Text;
                supplierMobile = txtSupplierMobile.Text;
                isSupplier = true;
                CancelSavingSupplier();
            }

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            OleDbCommand cmd = new OleDbCommand();
            cmd.Connection = Conn;

            Query = "SELECT ID, supplier_id, supplier_name, supplier_email, supplier_mobile FROM suppliers WHERE supplier_mobile = '" + txtSupplierMobile.Text.Trim() + "'";
            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            btnSaveSupplier.Enabled = true;

            // Supplier
            btnSaveSupplier.BackColor = Color.RoyalBlue;
            btnSaveSupplier.ForeColor = SystemColors.HighlightText;

            if (dt.Rows.Count > 0)
            {
                iSupplierId = dt.Rows[0]["supplier_id"].ToString();     // ?? Make it clear ASAP!!                
                txtSupplierName.Text = dt.Rows[0]["supplier_name"].ToString().TrimEnd();
                txtSupplierEmail.Text = dt.Rows[0]["supplier_email"].ToString();
                btnSaveSupplier.Text = "Select";

                // Lock input supplier. Instead, decide either take order or cancel
                txtSupplierName.ReadOnly = txtSupplierEmail.ReadOnly = txtSupplierMobile.ReadOnly = true;
                txtSupplierName.ForeColor = txtSupplierEmail.ForeColor = txtSupplierMobile.ForeColor = SystemColors.WindowFrame;
            }
            else
            {
                txtSupplierName.Text = "";
                txtSupplierEmail.Text = "";
                //isSupplier = false; Not required. Because, already there a supplier can exist.
                btnSaveSupplier.Text = "Save";
            }

            dt.Clear();
            da.Dispose();
            Conn.Close();

            txtSupplierMobile.Text = input;
            txtSupplierMobile.SelectAll();

        }

        private void txtSupplierMobile_Leave(object sender, EventArgs e)
        {
            HideSupplierPane(sender);
        }

        private void iTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(iTabControl.SelectedIndex)
            {
                case 1:
                    InitializePO();
                    break;

                case 0:
                    GetInventoryStatus();
                    break;
            }
                
            ClearScr();            
        }

        private void GetInventoryStatus()
        {
            if (!stockUpdated) return;

            if (dgvProducts.Rows.Count > 0) dgvProducts.Rows.Clear();

            // Fill stock
            DataTable dt = new DataTable();
            
            try
            {
                dt = new DataTable();
                dt = Stock.GetStocks();
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
                return;
            }

            if (dt == null || dt.Rows.Count == 0) return;

            double subTotalPurchase = 0;

            foreach (DataRow row in dt.Rows)
            {
                dgvProducts.Rows.Add((dt.Rows.IndexOf(row) + 1), row["product_name"], Util.IsNumeric(row["volume"].ToString()) ? row["volume"].ToString() : double.Parse(row["volume"].ToString()).ToString("0.0"), row["unit"], double.Parse(row["total_price"].ToString()).ToString("0.00"));
                subTotalPurchase += double.Parse(row["total_price"].ToString());
            }

            dgvProducts.Rows.Add("", "", "", "Total", subTotalPurchase.ToString("0.00"));           

            stockUpdated = false;
        }

        private void ClearScr()
        {
            if (iTabControl.SelectedIndex != 1) HidePickers();
        }

        private void dgvPurchaseHeader_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            dgvPurchaseHeader.Select();

            DataGridViewCell cell = dgvPurchaseHeader.SelectedCells[0];

            if (cell == dgvPurchaseHeader.Rows[0].Cells[dgvPurchaseHeader.ColumnCount - 1])
                if (keepSupplierHandle)
                    txtSupplierMobile.Focus();
                else
                    ShowSupplierPane(sender);
        }

        private void txtSupplierMobile_TextChanged(object sender, EventArgs e)
        {
            txtSupplierMobile.Text = txtSupplierMobile.Text.Replace("\r\n", "");

            // Keep the IF block as it is
            if (isSupplier && txtSupplierMobile.Text == supplierMobile)
            {
                txtSupplierName.Text = supplierName;
                txtSupplierEmail.Text = supplierEmail;
                txtSupplierMobile.Text = supplierMobile;
                btnSaveSupplier.Text = "Change";
                btnSaveSupplier.BackColor = Color.RoyalBlue;
                btnSaveSupplier.ForeColor = SystemColors.HighlightText;
                btnSaveSupplier.Enabled = true;
            }

            else if (txtSupplierMobile.Text == "")
            {
                // Supplier
                btnSaveSupplier.BackColor = Color.AliceBlue;
                btnSaveSupplier.ForeColor = SystemColors.ControlDark;
                btnSaveSupplier.Enabled = false;
                btnSaveSupplier.Text = "Find";
                txtSupplierMobile.Text = "";
                return;
            }
            else if (txtSupplierMobile.Text.Replace("\r\n", "").Trim().Length > 0 || txtSupplierMobile.Text == "\r\n" || txtSupplierMobile.Text.Length > 0)
            {
                txtSupplierName.Text = "";
                txtSupplierName.ReadOnly = false;
                txtSupplierEmail.Text = "";
                txtSupplierEmail.ReadOnly = false;
                /// Check it
                btnSaveSupplier.Text = "Find";
                btnSaveSupplier.BackColor = Color.RoyalBlue;
                btnSaveSupplier.ForeColor = SystemColors.HighlightText;
                btnSaveSupplier.Enabled = true;
            }
            else if (txtSupplierMobile.Text.Replace("\r\n", "").Trim() == "" || txtSupplierMobile.Text.Replace("\r\n", "").Trim().Length == 0 || txtSupplierMobile.Text == "")
            {
                txtSupplierName.Text = "";
                txtSupplierName.ReadOnly = false;
                txtSupplierEmail.Text = "";
                txtSupplierEmail.ReadOnly = false;
                /// Check it
                btnSaveSupplier.Text = "Find";
                btnSaveSupplier.BackColor = Color.AliceBlue;
                btnSaveSupplier.ForeColor = SystemColors.ControlDark;
                btnSaveSupplier.Enabled = false;
            }
            else
            {
                txtSupplierName.Text = "";
                txtSupplierName.ReadOnly = false;
                txtSupplierEmail.Text = "";
                txtSupplierEmail.ReadOnly = false;
                /// Check it
                btnSaveSupplier.Text = "Find";
                btnSaveSupplier.BackColor = Color.AliceBlue;
                btnSaveSupplier.ForeColor = SystemColors.ControlDark;
                btnSaveSupplier.Enabled = false;
            }
        }

        private void txtSupplierMobile_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                CancelSavingSupplier ();
        }

        private void CancelSavingSupplier()
        {            
            txtSupplierName.Text = "";
            txtSupplierEmail.Text = "";
            txtSupplierMobile.Text = "";
            btnSaveSupplier.Enabled = false;

            // Supplier
            btnSaveSupplier.Enabled = false;
            btnSaveSupplier.BackColor = Color.AliceBlue;
            btnSaveSupplier.ForeColor = SystemColors.ControlDark;

            //isSupplier = false;   // Not required because Supplier can be selected.

            btnSaveSupplier.Text = "Find";

            // Lock input Supplier. Instead, decide either take order or cancel
            txtSupplierName.ReadOnly = txtSupplierEmail.ReadOnly = txtSupplierMobile.ReadOnly = false;

            keepSupplierHandle = false;
            HideSupplierPane();

            cmbProductScan.Focus();
        }

        int keyPressCounter = 0;

        private void txtSupplierMobile_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                string input = txtSupplierMobile.Text.Replace("\r\n", "");

                txtSupplierMobile.Text = txtSupplierMobile.Text.Replace("\r\n", "");                    // IMPORTANT! JUST THIS LINE PREVENTS TO GO NEXT WITHOUT MOBILE NO.

                if (txtSupplierMobile.Text == "")
                {                    
                    // Supplier
                    btnSaveSupplier.BackColor = Color.AliceBlue;
                    btnSaveSupplier.ForeColor = SystemColors.ControlDark;
                    btnSaveSupplier.Text = "Find";
                    txtSupplierMobile.Text = "";
                    
                    keyPressCounter++;
                    if (keyPressCounter > 0)
                    {
                        HideSupplierPane();                        
                        cmbProductScan.Focus();
                    }
                    
                    return;
                }

                if (btnSaveSupplier.Text == "Change")
                {
                    btnSaveSupplier.Enabled = false;
                    btnSaveSupplier.BackColor = Color.AliceBlue;
                    btnSaveSupplier.ForeColor = SystemColors.ControlDark;
                    txtSupplierMobile.Text = txtSupplierMobile.Text.Replace("\r\n", "").Trim();
                    txtSupplierName.Text = txtSupplierEmail.Text = txtSupplierMobile.Text = "";
                    txtSupplierMobile.Focus();
                    return;
                }

                if (btnSaveSupplier.Text == "Save")
                {
                    SaveSupplier();
                }

                if (btnSaveSupplier.Text == "Select" && txtSupplierMobile.Text.Length > 0)
                {
                    dgvPurchaseHeader.Rows[0].Cells[7].Value = txtSupplierName.Text;
                    supplierId = iSupplierId;
                    if (purchase.ContainsKey("supplier_id")) purchase["supplier_id"] = supplierId; else purchase.Add("supplier_id", supplierId);
                    supplierName = txtSupplierName.Text;
                    supplierEmail = txtSupplierEmail.Text;
                    supplierMobile = txtSupplierMobile.Text;

                    if (purchase.ContainsKey("supplier")) purchase["supplier"] = supplierName; else purchase.Add("supplier", supplierName);

                    isSupplier = true;
                    dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Black;
                    CancelSavingSupplier();
                }

                try
                {
                    Conn = new OleDbConnection(ConnectionString);
                    Conn.Open();
                }
                catch (Exception err)
                {
                    ErrorMessage = err.Message;
                    MessageBox.Show(ErrorMessage);
                }

                if (btnSaveSupplier.Enabled = false && txtSupplierMobile.Text == "") return;

                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = Conn;

                Query = "SELECT ID, supplier_id, supplier_name, supplier_email, supplier_mobile FROM suppliers WHERE supplier_mobile = '" + txtSupplierMobile.Text.Trim() + "'";
                cmd.CommandText = Query;

                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                btnSaveSupplier.Enabled = true;

                // Supplier
                btnSaveSupplier.BackColor = Color.RoyalBlue;
                btnSaveSupplier.ForeColor = SystemColors.HighlightText;

                if (dt.Rows.Count > 0)
                {
                    iSupplierId = dt.Rows[0]["supplier_id"].ToString();
                    txtSupplierName.Text = dt.Rows[0]["supplier_name"].ToString().TrimEnd();
                    txtSupplierEmail.Text = dt.Rows[0]["supplier_email"].ToString();
                    btnSaveSupplier.Text = "Select";

                    // Lock input Supplier. Instead, decide either take order or cancel
                    txtSupplierName.ReadOnly = txtSupplierEmail.ReadOnly = txtSupplierMobile.ReadOnly = true;
                    txtSupplierName.ForeColor = txtSupplierEmail.ForeColor = txtSupplierMobile.ForeColor = SystemColors.WindowFrame;
                }
                else
                {
                    txtSupplierName.Text = "";
                    txtSupplierEmail.Text = "";
                    //isSupplier = false; Not required. Because, already there a Supplier can exist.
                    btnSaveSupplier.Text = "Save";
                }

                dt.Clear();
                da.Dispose();
                Conn.Close();

                txtSupplierMobile.Text = input;
                
                e.Handled = true;
                txtSupplierMobile.SelectAll();
            }
        }

        private void SaveSupplier()
        {
            if (txtSupplierName.Text == "")
            {
                txtSupplierName.Focus();
                return;
            }
            else if (txtSupplierEmail.Text == "")
            {
                txtSupplierEmail.Focus();
                return;
            }
            else if (txtSupplierMobile.Text == "")  // though not required.
            {
                txtSupplierMobile.Focus();
                return;
            }

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = Conn;

            Query = "SELECT(MAX(ID) + 1) AS ID FROM suppliers";
            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count > 0)
                iSupplierId = dt.Rows[0]["ID"].ToString();

            Query = "INSERT INTO suppliers (supplier_id, supplier_name, supplier_email, supplier_mobile) VALUES ( " + iSupplierId + ", '" + txtSupplierName.Text + "', '" + txtSupplierEmail.Text + "', '" + txtSupplierMobile.Text + "')";

            try
            {
                cmd.CommandText = Query;
                cmd.ExecuteReader();

                if (btnSaveSupplier.Text == "Save")
                {
                    btnSaveSupplier.Text = "Select";
                    txtSupplierMobile.Focus();
                }
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            Conn.Close();
        }

        private void StockScr_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool state = StateChecked();

            if (state)
                e.Cancel = false;
            else
            {
                e.Cancel = true;                
            }
        }

        private void btnCancelSupplierWin_Click(object sender, EventArgs e)
        {
            CancelSavingSupplier();
        }

        private void cmbProductScan_TextChanged(object sender, EventArgs e)
        {
            if (cmbProductScan.Text == "")
                return;

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            OleDbCommand cmd = new OleDbCommand();
            cmd.Connection = Conn;

            //Query = "SELECT ID AS ID, 1 AS Qty, unit AS Unit, product_name AS ItemName, unit_purchase_price AS UnitPurchasePrice, unit_price AS UnitSalesPrice, (1 * unit_purchase_price) AS ItemTotal, discount AS Discount, vat FROM products WHERE shortcode = '" + cmbProductScan.Text + "' OR barcode = '" + cmbProductScan.Text + "' OR product_name LIKE '%" + cmbProductScan.Text + "%'";
            Query = "SELECT ID AS ID, 1 AS Qty, unit AS Unit, product_name AS ItemName, unit_purchase_price AS UnitPurchasePrice, unit_price AS UnitSalesPrice, (1 * unit_purchase_price) AS ItemTotal, discount AS Discount, vat FROM products WHERE VAL(barcode) = " + cmbProductScan.Text;

            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();

            try { da.Fill(dt); }catch(Exception x) { MessageBox.Show(x.Message); }
            
            if (dt.Rows.Count > 0)
            {
                if (!selectedItemsId.Contains(int.Parse(dt.Rows[0]["ID"].ToString())))
                {                    

                    dgvPurchaseDetails.Rows.Add((dgvPurchaseDetails.Rows.Count + 1), dt.Rows[0]["ID"], dt.Rows[0]["ItemName"], double.Parse(dt.Rows[0]["UnitPurchasePrice"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["UnitSalesPrice"].ToString()).ToString("0.00"), dt.Rows[0]["Qty"], dt.Rows[0]["Unit"], double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"));

                    Array.Resize(ref selectedItemsId, selectedItemsId.Length + 1);
                    selectedItemsId[selectedItemsId.Length - 1] = int.Parse(dt.Rows[0]["ID"].ToString());

                    Array.Resize(ref pUnits, selectedItemsId.Length + 1);               // item count wise unit count even duplicate unit
                    pUnits[selectedItemsId.Length - 1] = dt.Rows[0]["Unit"].ToString();

                    UpdatePurchaseSummary(0);

                    ////////////////////////// CREATE PURCHASE IN LOCAL DB ////////////////////////////////////////
                    ///
                    
                    // Enable the Save button that can be used for updating stock
                    btnSave.Enabled = true;
                    ///////////////////////
                    

                    if (!isPurchase)
                        CreatePurchase(dgvPurchaseHeader.Rows[0].Cells[1].Value.ToString());

                    if (isPurchase && purchasedItem.Count == 0)
                    {
                        purchasedItem.Add("purchase_id", purchaseId.ToString());
                        purchasedItem.Add("product_id", dt.Rows[0]["ID"].ToString());
                        purchasedItem.Add("qty",Util.IsNumeric(dt.Rows[0]["Qty"].ToString()) ? dt.Rows[0]["Qty"].ToString() : double.Parse(dt.Rows[0]["Qty"].ToString()).ToString("0.0"));
                        purchasedItem.Add("discount_total", double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"));
                        purchasedItem.Add("vat_total", double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"));
                        purchasedItem.Add("item_total", double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00"));
                    }
                    else
                    {
                        if (isPurchase)
                        {
                            purchasedItem["product_id"] = dt.Rows[0]["ID"].ToString();
                            purchasedItem["qty"] =Util.IsNumeric(dt.Rows[0]["Qty"].ToString()) ? dt.Rows[0]["Qty"].ToString() : double.Parse(dt.Rows[0]["Qty"].ToString()).ToString("0.0");
                            purchasedItem["discount_total"] = double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00");
                            purchasedItem["vat_total"] = double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00");
                            purchasedItem["item_total"] = double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00");
                        }
                    }

                    AddItem(purchasedItem);
                    /////////////////////////////////////////////////////////////////////////////////////////////


                    // instead of clear selection
                    // dgvPurchaseDetails.ClearSelection();
                    // give the edit option by default and  immediately after adding the item

                    if (dgvPurchaseDetails.Rows.Count > 0)
                        dgvPurchaseDetails.Rows[dgvPurchaseDetails.Rows.Count - 1].Selected = true;

                    // show alert for product wise VAT
                    if (double.Parse(dt.Rows[0]["vat"].ToString()) > 0.00) MessageBox.Show("VAT for " + dt.Rows[0]["ItemName"].ToString() + ": " + double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), "VAT inclusive with the product!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    cmbProductScan.SelectAll();

                    DataGridViewCell cell = dgvPurchaseDetails.SelectedRows[0].Cells[5];
                    dgvPurchaseDetails.CurrentCell = cell;
                    dgvPurchaseDetails.BeginEdit(true);
                }
                else
                {
                    for (int i = 0; i < dgvPurchaseDetails.Rows.Count; i++)
                    {
                        if (int.Parse(dgvPurchaseDetails.Rows[i].Cells[1].Value.ToString()) == int.Parse(dt.Rows[0]["ID"].ToString()))
                        {
                            dgvPurchaseDetails.Rows[i].Selected = true;
                            DataGridViewCell cell = dgvPurchaseDetails.Rows[i].Cells[5];
                            dgvPurchaseDetails.CurrentCell = cell;
                            dgvPurchaseDetails.BeginEdit(true);
                        }
                    }

                    cmbProductScan.SelectAll();
                }


            }

            Conn.Close();
        }


        private void AddItem(Dictionary<string, string> purchasedItem)
        {
            if (dgvPurchaseDetails.Rows.Count == 0) return;

            /////////////////////////////////////////////////////////////////

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = Conn;

            Query = "INSERT INTO purchased_items (purchase_id, product_id, qty, discount_total, vat_total, item_total) VALUES ( " + purchasedItem["purchase_id"] + ", " + purchasedItem["product_id"] + ", " + purchasedItem["qty"] + ", " + purchasedItem["discount_total"] + ", " + purchasedItem["vat_total"] + ", " + purchasedItem["item_total"] + ")";

            try
            {
                cmd.CommandText = Query;
                cmd.ExecuteReader();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            Conn.Close();

            ////////////
            ///

            if (isPurchase)
            {
                if (purchase.Count == 0)
                {
                    if (!purchase.ContainsKey("purchase_id"))
                        purchase.Add("purchase_id", purchaseId.ToString());
                    purchase.Add("sub_total", double.Parse(dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00"));
                    string vat = dgvPurchaseDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    purchase.Add("vat", vat);
                    purchase.Add("discount", double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("num_items", dgvPurchaseDetails.Rows.Count.ToString());
                    purchase.Add("grand_total", (double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()) + double.Parse(vat)).ToString("0.00"));
                    purchase.Add("payment", double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("debit", double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("credit", double.Parse(dgvPurchaseSummary.Rows[5].Cells[3].Value.ToString()).ToString("0.00"));
                }
                else
                {
                    purchase["sub_total"] = double.Parse(dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00");
                    string vat = dgvPurchaseDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    purchase["vat"] = vat;
                    purchase["discount"] = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    purchase["num_items"] = dgvPurchaseDetails.Rows.Count.ToString();
                    purchase["grand_total"] = (double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()) + double.Parse(vat)).ToString("0.00");
                    purchase["payment"] = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    purchase["debit"] = double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00");
                    purchase["credit"] = double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00");
                }

                UpdatePurchase(purchase);
            }
            ////////////
        }

        

        private void UpdateItem(Dictionary<string, string> purchasedItem, string field = null)
        {
            if (dgvPurchaseDetails.Rows.Count == 0) return;

            /////////////////////////////////////////////////////////////////

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = Conn;

            if (field != null)
                Query = " UPDATE purchased_items SET " + field + " = " + purchasedItem[field] + " ";
            else
                Query = " UPDATE purchased_items SET qty = " + purchasedItem["qty"] + ", discount_total = " + purchasedItem["discount_total"] + ", vat_total = " + purchasedItem["vat_total"] + ", item_total = " + purchasedItem["item_total"];

            Query += " WHERE purchase_id = " + purchasedItem["purchase_id"] + " AND product_id = " + purchasedItem["product_id"] + " ";

            try
            {
                cmd.CommandText = Query;
                //cmd.ExecuteReader();
                cmd.ExecuteScalar();
            }
            catch (Exception err)
            {
                MessageBox.Show(Query);
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            Conn.Close();

            ////////////
            ///

            if (isPurchase)
            {
                if (purchase.Count == 0)
                {
                    if (!purchase.ContainsKey("purchase_id"))
                        purchase.Add("purchase_id", purchaseId.ToString());
                    purchase.Add("sub_total", double.Parse(dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00"));
                    string vat = dgvPurchaseDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    purchase.Add("vat", vat);
                    purchase.Add("discount", double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("num_items", dgvPurchaseDetails.Rows.Count.ToString());
                    purchase.Add("grand_total", double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("payment", double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("debit", double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("credit", double.Parse(dgvPurchaseSummary.Rows[5].Cells[3].Value.ToString()).ToString("0.00"));
                }
                else
                {
                    purchase["sub_total"] = double.Parse(dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00");
                    string vat = dgvPurchaseDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    purchase["vat"] = vat;
                    //purchase["discount"] = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    purchase["num_items"] = dgvPurchaseDetails.Rows.Count.ToString();
                    //purchase["grand_total"] = double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00");
                    //purchase["payment"] = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    purchase["debit"] = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    purchase["credit"] = double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00");
                }

                UpdatePurchase(purchase);
            }
            ////////////

        }

        private void RemoveItem()
        {
            ///////////////////////////// UI PART ////////////////////////////////
            ///
            int productId = 0;

            if (dgvPurchaseDetails.Rows.Count > 0 && selectedItemsId.Contains(int.Parse(dgvPurchaseDetails.SelectedRows[0].Cells[1].Value.ToString())))
            {
                productId = int.Parse(dgvPurchaseDetails.SelectedRows[0].Cells[1].Value.ToString());

                List<int> list = selectedItemsId.ToList();
                list.Remove(int.Parse(dgvPurchaseDetails.SelectedRows[0].Cells[1].Value.ToString()));
                selectedItemsId = list.ToArray();
                dgvPurchaseDetails.Rows.Remove(dgvPurchaseDetails.SelectedRows[0]);
                UpdatePurchaseSummary(0);
            }

            for (int i = 0; i < dgvPurchaseDetails.Rows.Count; i++)
            {
                dgvPurchaseDetails.Rows[i].Cells[0].Value = (i + 1);
            }

            //empty calculation
            //txtCalcInput.Text = "0.00";

            cmbProductScan.SelectAll(); cmbProductScan.Focus();

            ////////////////////////////////////////////////////////////////
            /// DATABASE PART

            if (!isPurchase || productId == 0) return;

            /////////////////////////////////////////////////////////////////

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = Conn;


            Query = " DELETE FROM purchased_items ";
            Query += " WHERE purchase_id = " + purchaseId.ToString() + " AND product_id = " + productId.ToString();

            try
            {
                cmd.CommandText = Query;
                cmd.ExecuteReader();
            }
            catch (Exception err)
            {
                MessageBox.Show(Query);
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            Conn.Close();

            ////////////
            ///

            if (isPurchase)
            {
                if (purchase.Count == 0)
                {
                    purchase.Add("purchase_id", purchaseId.ToString());
                    purchase.Add("sub_total", double.Parse(dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00"));
                    string vat = dgvPurchaseDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    purchase.Add("vat", vat);
                    purchase.Add("discount", double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("num_items", dgvPurchaseDetails.Rows.Count.ToString());
                    purchase.Add("grand_total", double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("payment", double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("debit", double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00"));
                    purchase.Add("credit", double.Parse(dgvPurchaseSummary.Rows[5].Cells[3].Value.ToString()).ToString("0.00"));
                }
                else
                {
                    purchase["sub_total"] = double.Parse(dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00");
                    string vat = dgvPurchaseDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    purchase["vat"] = vat;
                    //purchase["discount"] = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    purchase["num_items"] = dgvPurchaseDetails.Rows.Count.ToString();
                    //purchase["grand_total"] = double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00");
                    //purchase["payment"] = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    purchase["debit"] = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    purchase["credit"] = double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00");
                }

                UpdatePurchase(purchase);
            }
            ////////////

        }

        private void CreatePurchase(string poNo)
        {
            if(dgvPurchaseHeader.Rows[0].Cells[1].Value.ToString() == "") return;

            if (dgvPurchaseDetails.Rows.Count == 0) return;

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = Conn;

            Query = "SELECT(MAX(ID) + 1) AS ID FROM purchases";
            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();

            try
            {
                da.Fill(dt);
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            if (dt.Rows.Count > 0)
            {
                string spurchaseId = dt.Rows[0]["ID"].ToString();
                if (spurchaseId == "") iPurchaseId = 1;
                else iPurchaseId = int.Parse(spurchaseId);
            }
            Query = " INSERT INTO purchases (purchase_id, supplier_id, po_no, cur_status) "       //, po_date_time, due_date_time) " +
            + " VALUES ( " + iPurchaseId + ", " + supplierId + ", '" + poNo + "', 'OPEN'); ";      //, #" + purchase["po_date_time"] + "#, #" + purchase["due_date_time"] + "#)";

            try
            {
                cmd.CommandText = Query;
                cmd.ExecuteReader();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            cmd = new OleDbCommand(); cmd.Connection = Conn;

            Query = "Select @@IDENTITY";
            cmd.CommandText = Query;
            purchaseId = (Int32)cmd.ExecuteScalar();            
            isPurchase = true;     // i.e.: Now, there is an open order that is on process.            
            iPurchaseId = 0;
            if (purchase.ContainsKey("iPurchaseId")) purchase.Remove("iPurchaseId");
            if (!purchase.ContainsKey("purchase_id"))
                purchase.Add("purchase_id", purchaseId.ToString());
            else
                purchase["purchase_id"] = purchaseId.ToString();
            Conn.Close();
        }

        private void UpdatePurchaseSummary(int i)
        {
            switch (i)
            {
                case 0:
                    UpdateTotal();
                    break;

                case 1:
                    if (!((double.Parse(dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString()) - double.Parse(dgvPurchaseSummary.Rows[i].Cells[3].Value.ToString())) < 0))
                    {
                        dgvPurchaseSummary.Rows[2].Cells[3].Value = double.Parse(dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString()) + double.Parse(dgvPurchaseSummary.Rows[0].Cells[1].Value.ToString()) - double.Parse(dgvPurchaseSummary.Rows[i].Cells[3].Value.ToString());
                    }
                    else
                    {                        
                        dgvPurchaseSummary.Rows[2].Cells[3].Value = 0.00; 
                    }
                    dgvPurchaseHeader.Rows[2].Cells[1].Value = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString());
                    break;

                case 3:
                    if (!((double.Parse(dgvPurchaseSummary.Rows[i].Cells[3].Value.ToString()) - double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString())) <= 0))
                    {
                        dgvPurchaseSummary.Rows[2].Cells[3].Value = double.Parse(dgvPurchaseSummary.Rows[i].Cells[3].Value.ToString()) - double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString());
                        dgvPurchaseSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.SelectionForeColor = dgvPurchaseSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                    }
                    else
                    {
                        dgvPurchaseSummary.Rows[2].Cells[3].Value = 0.00;
                        dgvPurchaseSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.SelectionForeColor = dgvPurchaseSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.ForeColor = Color.Red;
                    }

                    if ((double.Parse(dgvPurchaseSummary.Rows[i].Cells[3].Value.ToString()) - double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString())) == 0)
                        dgvPurchaseSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.SelectionForeColor = dgvPurchaseSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.ForeColor = SystemColors.ControlText;

                    double due = double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()) - double.Parse(dgvPurchaseSummary.Rows[i].Cells[3].Value.ToString());
                    
                    if (due > 0)
                        dgvPurchaseSummary.Rows[5].Cells[3].Value = due; 
                    else
                        dgvPurchaseSummary.Rows[5].Cells[3].Value = 0;

                    break;
            }


            if (!((double.Parse(dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString()) - double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString())) <= 0))
                dgvPurchaseSummary.Rows[2].Cells[3].Value = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()) - double.Parse(dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString());
            else
                dgvPurchaseSummary.Rows[2].Cells[3].Value = 0.00;

            if (isPurchase && purchase.Count > 0)
            {                
                purchase["debit"] = double.Parse(dgvPurchaseSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                purchase["credit"] = double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00");
                UpdatePurchase(purchase);
            }

        }

        private void UpdatePurchase(Dictionary<string, string> purchase)
        {
            /// Verify either to create new or continue ///
            int cuOption = 0;
            if (!isPurchase)  cuOption = 1;
            if (purchase.ContainsKey("iPurchaseId")) cuOption = 1;
            if (!purchase.ContainsKey("purchase_id")) cuOption = 1;
            ////////////// End verification ///////////////
            /// is cuOption == 1?
            ///
            if(cuOption==1)
            {
                CreatePurchase(dgvPurchaseHeader.Rows[0].Cells[1].Value.ToString());
                return;
            }

            if (dgvPurchaseDetails.Rows.Count == 0) return;

            try
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            OleDbCommand cmd = new OleDbCommand();

            cmd.Connection = Conn;

            Query = "UPDATE purchases SET ";
            Query += "sub_total = " + purchase["sub_total"] + " ";            
            Query += ", num_items = " + purchase["num_items"] + " ";            
            Query += ", debit = " + purchase["debit"] + " ";
            Query += ", credit = " + purchase["credit"] + " ";
            
            Query += ", po_no = '" + purchase["po_no"] + "' ";
            /*
            Query += ", po_date_time = #" + purchase["po_date_time"] + "# ";
            Query += ", due_date_time = #" + purchase["due_date_time"] + "# ";
            */
            Query += ", supplier_id = " + purchase["supplier_id"] + " ";

            Query += " WHERE purchase_id = " + purchase["purchase_id"];

            try
            {
                cmd.CommandText = Query;
                cmd.ExecuteScalar();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            //cmd = new OleDbCommand(); cmd.Connection = Conn;
            //cmd.CommandText = Query;

            isPurchase = true;

            Conn.Close();
        }



        private void UpdateTotal()
        {
            double subTotal = 0.00, discount = 0.00, vat = 0.00, grandTotal = 0.00;

            for (int i = 0; i < dgvPurchaseDetails.Rows.Count; i++)
            {
                subTotal += double.Parse(dgvPurchaseDetails.Rows[i].Cells[8].Value.ToString());                
            }

            grandTotal = subTotal + vat - discount;
            
            dgvPurchaseSummary.Rows[0].Cells[3].Value = subTotal;            
            dgvPurchaseSummary.Rows[1].Cells[3].Value = 0.00;
            dgvPurchaseSummary.Rows[2].Cells[3].Value = 0.00;
        }

        

        private void dgvPurchaseDetails_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewRow row = dgvPurchaseDetails.CurrentRow;
            DataGridViewCell cell = dgvPurchaseDetails.CurrentCell;
            string uValue = "";

            purchasedItem["product_id"] = row.Cells[1].Value.ToString();            

            switch (cell.ColumnIndex)
            {
                // Unit Purchase Price
                case 3:
                    uValue = double.Parse(oValue).ToString("0.00");
                    if (!Util.IsFloat(cell.Value.ToString()) || double.Parse(cell.Value.ToString()) <= 0) { dgvPurchaseDetails.CurrentCell.Value = uValue; return; }
                    cell.Value = double.Parse(cell.Value.ToString()).ToString("0.00");
                    row.Cells[(cell.ColumnIndex + 5)].Value = double.Parse((double.Parse(cell.Value.ToString()) * double.Parse(row.Cells[(cell.ColumnIndex + 2)].Value.ToString())).ToString()).ToString("0.00"); 
                    if(double.Parse(cell.Value.ToString()) > double.Parse(row.Cells[cell.ColumnIndex + 1].Value.ToString()))
                    {
                        MessageBox.Show("Product: " + row.Cells[cell.ColumnIndex - 1].Value.ToString() + "\nPurchase price is higher than sales price.", "Change Required (Selling Price)", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        dgvPurchaseDetails.CurrentCell = row.Cells[cell.ColumnIndex + 1];
                        dgvPurchaseDetails.BeginEdit(true);
                    }
                    UpdatePurchaseSummary(0);
                    purchasedItem["item_total"] = row.Cells[(cell.ColumnIndex + 5)].Value.ToString();
                    /// update ordered item in the database
                    UpdateItem(purchasedItem);                    

                    break;

                // Unit Sales Price
                case 4:
                    uValue = double.Parse(oValue).ToString("0.00");
                    if (!Util.IsFloat(cell.Value.ToString()) || double.Parse(cell.Value.ToString()) <= 0 || double.Parse(cell.Value.ToString()) < double.Parse(row.Cells[cell.ColumnIndex - 1].Value.ToString())) { dgvPurchaseDetails.CurrentCell.Value = uValue; return; }
                    cell.Value = double.Parse(cell.Value.ToString()).ToString("0.00");
          
                    break;

                //Qty
                case 5:
                    if (!Util.IsFloat(cell.Value.ToString()) && double.Parse(cell.Value.ToString()) <= 0) {dgvPurchaseDetails.CurrentCell.Value = oValue; return; }
                    row.Cells[(cell.ColumnIndex + 3)].Value = double.Parse((double.Parse(cell.Value.ToString()) * double.Parse(row.Cells[(cell.ColumnIndex - 2)].Value.ToString())).ToString()).ToString("0.00");
                    cell.Value = Util.IsNumeric(cell.Value.ToString()) ? cell.Value : double.Parse(cell.Value.ToString()).ToString("0.0");
                    purchasedItem["qty"] = cell.Value.ToString();
                    purchasedItem["item_total"] = row.Cells[(cell.ColumnIndex + 3)].Value.ToString();
                    UpdatePurchaseSummary(0);
                    /// update ordered item in the database
                    UpdateItem(purchasedItem);
                    break;                
            }            
        }

        private void dgvPurchaseDetails_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {            
            oRow = dgvPurchaseDetails.CurrentRow;
            oValue = dgvPurchaseDetails.CurrentCell.Value.ToString();
        }

        private void ChangeRowUIState()
        {
            DataGridViewRow row = dgvPurchaseDetails.CurrentRow;
            DataGridViewCell cell = dgvPurchaseDetails.CurrentCell;
            dgvPurchaseDetails.BeginEdit(true);            
        }

        private void cmbProductScan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                ShowSupplierPane();
                txtSupplierMobile.Focus();
            }
            else if (e.Control && e.KeyCode == Keys.P)      // Doesn't work, doesn't get key accept Control
                PaidAmountEditMode();
            else if (e.KeyCode == Keys.Delete)
                RemoveItem();
            else if (e.Control && e.KeyCode == Keys.S)
                SavePurchase();
        }

        private void PaidAmountEditMode()
        {
            if (dgvPurchaseDetails.Rows.Count == 0 || dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString() == "0.00" || dgvPurchaseSummary.Rows[0].Cells[3].Value.ToString() == "") return;
            gbPaidAmount.Top    = tabPurchase.Height / 2 - gbPaidAmount.Height / 2;
            gbPaidAmount.Left   = tabPurchase.Width / 2 - gbPaidAmount.Width / 2;
            gbPaidAmount.Visible = true;
            txtPaymentAmount.Text = "0.00";
            txtPaymentAmount.SelectionStart = 1;
            txtPaymentAmount.Focus();
            return;            
        }        

        private void dgvPurchaseDetails_RowLeave(object sender, DataGridViewCellEventArgs e)
        {
            cmbProductScan.Text = string.Empty;
        }

        private void dgvPurchaseDetails_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            ChangeRowUIState();
        }

        private void dgvPurchaseHeader_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            /// Keep the line. Work later. Don't delete.
            ///if (dgvPurchaseHeader.CurrentCell.ColumnIndex == 7) dgvPurchaseHeader.CurrentCell.Style.ForeColor = Color.Salmon;
            ///

            dgvPurchaseHeader.BeginEdit(true);
        }        

        private void txtPaymentAmount_TextChanged(object sender, EventArgs e)
        {
            if (Util.IsFloat(txtPaymentAmount.Text))
                txtPaymentAmount.Text = double.Parse(txtPaymentAmount.Text).ToString("0.00");
            else
                txtPaymentAmount.Text = "0.00";
            txtPaymentAmount.SelectionStart = txtPaymentAmount.Text.Length - 3;     // cursor before the decimal point
        }

        private void txtPaymentAmount_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar==13) SetPayment();
        }

        private void btnApplyCoupon_Click(object sender, EventArgs e)
        {
            SetPayment();
        }

        private void SetPayment()
        {
            DataGridViewCell totalAmount = dgvPurchaseSummary.Rows[0].Cells[3];            

            DataGridViewCell paidAmount = dgvPurchaseSummary.Rows[1].Cells[3];
            paidAmount.Value = txtPaymentAmount.Text;

            DataGridViewCell dueAmount = dgvPurchaseSummary.Rows[2].Cells[3];
            dueAmount.Value = (double.Parse(paidAmount.Value.ToString()) - double.Parse(totalAmount.Value.ToString())).ToString("0.00");            

            purchase["debit"] = paidAmount.Value.ToString();
            purchase["credit"] = dueAmount.Value.ToString();

            UpdatePurchase(purchase);

            txtPaymentAmount.Text = string.Empty;
            gbPaidAmount.Visible = false;

            cmbProductScan.Focus();
        }

        private void txtPaymentAmount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) HidePickers();
        }

        private void cmbProductScan_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13) SetPurchaseHeader();
        }

        private void SetPurchaseHeader()
        {
            // Prevent System.NullReferenceException: 'Object reference not set to an instance of an object.'
            if (dgvPurchaseHeader.Rows.Count == 0) return;

            DataGridViewRow row = dgvPurchaseHeader.Rows[0];
            DataGridViewCell cell = dgvPurchaseHeader.CurrentCell;

            if (cell.ColumnIndex >= 7)
            {
                dgvPurchaseHeader.Rows[0].Cells[0].Selected = true;
                cmbProductScan.Focus(); return;
            }

            DataGridViewCell cellEdit = row.Cells[cell.ColumnIndex + 1];
            if (cell.ColumnIndex == 0 || cell.ColumnIndex == 2 || cell.ColumnIndex == 4 || cell.ColumnIndex == 6)
            {
                cellEdit.Selected = true;
                dgvPurchaseSummary.BeginEdit(true);

                if (cell.ColumnIndex == 6)
                {   // keep the line. work later. don't delete.
                    //if (!isSupplier) dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Red; else dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Black;
                    ShowSupplierPane();
                }
            }
        }

        private void dgvPurchaseHeader_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {                
                DataGridViewRow row = dgvPurchaseHeader.Rows[0];
                DataGridViewCell currCell = dgvPurchaseHeader.CurrentCell;

                if (!isPurchase && currCell.ColumnIndex == 1 && currCell.Value == null || !isPurchase && currCell.ColumnIndex == 1 && currCell.Value.ToString() == "") iTabControl.SelectedIndex = 1;
                else if (currCell.ColumnIndex >= 7)
                {
                    cmbProductScan.Focus(); return;
                }
               
                DataGridViewCell nextCell = row.Cells[currCell.ColumnIndex + 1];
                nextCell.Selected = true;

                // Keep the line. Work later. Don't delete.
                //if (nextCell.ColumnIndex >= 5) if (!isSupplier) dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Red; else dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Black;

                    SetPurchaseHeader();                
            }
        }

        private void dgvPurchaseHeader_CellLeave(object sender, DataGridViewCellEventArgs e)
        {
            /**
             /// Keep the line. Work later. Don't delete. 
            if (!isSupplier)
                if(dgvPurchaseHeader.CurrentCell.ColumnIndex == 6)
                    dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Red;
                else if (dgvPurchaseHeader.CurrentCell.ColumnIndex > 6)
                    dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Salmon;
            else if (isSupplier) dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Black;
            **/
        }

        private void txtSupplierMobile_Enter(object sender, EventArgs e)
        {
            // Keep the line. Work later. Don't delete.
            //if (!isSupplier) dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Red; else dgvPurchaseHeader.Rows[0].Cells[7].Style.ForeColor = Color.Black;
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            RemoveItem();
        }

        private void dgvPurchaseDetails_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!isPurchase || dgvPurchaseDetails.Rows.Count == 0) return;
                 
            DataGridViewRow row = dgvPurchaseDetails.CurrentRow;
            DataGridViewCell cell = dgvPurchaseDetails.CurrentCell;

            purchasedItem["product_id"] = row.Cells[1].Value.ToString();

            Dictionary<string, string> product = new Dictionary<string, string>();
            bool success;

            switch (cell.ColumnIndex)
            {
                case 3:
                    
                    /// UPDATE PRODUCT                    
                    product.Add("unit_purchase_price", cell.Value.ToString());
                    success = Product.UpdateProduct(product, int.Parse(purchasedItem["product_id"]));
                    if (!success) MessageBox.Show("Can't update Unit Purchase Price of " + dgvPurchaseDetails.CurrentRow.Cells[2].Value.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    product.Clear();
                    
                    break;

                case 4:
                    
                    /// UPDATE PRODUCT                    
                    product.Add("unit_price", cell.Value.ToString());
                    success = Product.UpdateProduct(product, int.Parse(purchasedItem["product_id"]));
                    if (!success) MessageBox.Show("Can't update Unit Purchase Price of " + dgvPurchaseDetails.CurrentRow.Cells[2].Value.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    product.Clear();

                    break;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SavePurchase();            
        }

        private void SavePurchase()
        {
            string msg = "";
            MessageBoxIcon icon = MessageBoxIcon.Information;

            try
            {
                List<Dictionary<string, string>> products = new List<Dictionary<string, string>>();                
                int[] productIds = new int[dgvPurchaseDetails.Rows.Count];
                DataGridViewRow row = new DataGridViewRow();


                for (int i = 0; i < productIds.Length; i++)
                {
                    Dictionary<string, string> product = new Dictionary<string, string>();

                    row = dgvPurchaseDetails.Rows[i];

                    string product_id = row.Cells[1].Value.ToString();
                    product.Add("product_id", product_id);

                    string last_process_id = purchaseId.ToString();        // Purchase ID on process
                    product.Add("last_process_id", last_process_id);

                    // to increase volume in the database
                    string volume = row.Cells[5].Value.ToString();
                    product.Add("volume", volume);

                    // to increase volume in the database
                    string total_price = row.Cells[8].Value.ToString();
                    product.Add("total_price", total_price);

                    // Make it last_process_type (SALES/ PURCHASE/ SYNC) and add previous_process_type and create sync history table
                    product.Add("is_purchase", "true");

                    products.Add(product);
                }


                /// UPDATE INVENTORY                                
                msg += Stock.UpdateStock(products, productIds);
                if (msg.Contains("Error")) icon = MessageBoxIcon.Error;
                MessageBox.Show(msg, "Inventory Status!", MessageBoxButtons.OK, icon);

                ////////////////////// PRINTING RECEIPT ///////////////////////
                ///

                PrintDocument doc = new PrintDocument();
                PrintPreviewDialog preview = new PrintPreviewDialog();  // Enable when preview on
                preview.Document = doc;                                 // Enable when preview on          
                doc.PrintPage += new PrintPageEventHandler(PrintReceipt);

                preview.Width = Screen.PrimaryScreen.WorkingArea.Width;
                preview.Height = Screen.PrimaryScreen.WorkingArea.Height;
                preview.PrintPreviewControl.Zoom = 1;

                preview.ShowDialog(); //Application.Exit();

                /// Print command
                //doc.Print();

                ///////////////////////////////////////////////////////////////
                ///

                dgvPurchaseHeader.Rows[0].Cells[1].Value = dgvPurchaseHeader.Rows[0].Cells[3].Value = dgvPurchaseHeader.Rows[0].Cells[5].Value = dgvPurchaseHeader.Rows[0].Cells[7].Value = null;
                dgvPurchaseDetails.Rows.Clear();
                dgvPurchaseSummary.Rows.Clear();
                PurchaseSummaryHeadsShowUp();

                // Purchase
                isPurchase = false;
                purchaseId = 0;
                iPurchaseId = 0;
                purchase.Clear();
                purchasedItem.Clear();

                // Supplier
                isSupplier = false;
                supplierId = "0";
                iSupplierId = "0";
                supplierName = "";
                supplierEmail = "";
                supplierMobile = "";

                // selected items array
                selectedItemsId = new int[0];

                // purchased units
                pUnits = new string[0];

                // old row on current datagridview row
                oRow = new DataGridViewRow();

                // old cell on current datagridview cell
                oValue = "";

                btnSave.Enabled = false;
                stockUpdated = true;
                //  iTabControl.SelectedIndex = 0;  // Not helpful
                //  cmbProductName.Focus(); it will not work 
                //  So, finally and for the time being
                InitializePO();                
            }
            catch (Exception x)
            {
                icon = MessageBoxIcon.Error;
                msg += "\nError: " + x.Message;
                MessageBox.Show(msg, "Inventory Status!", MessageBoxButtons.OK, icon);
            }
        }

        private void dgvPurchaseHeader_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvPurchaseHeader.Rows.Count == 0) return;

            if (dgvPurchaseHeader.Rows.Count > 0 && e.ColumnIndex == 1)
            {
                if (dgvPurchaseHeader.Rows[0].Cells[1].Value == null) return;
                else if(dgvPurchaseHeader.Rows[0].Cells[1].Value.ToString() != "")
                    GetPurchase(dgvPurchaseHeader.Rows[0].Cells[e.ColumnIndex].Value.ToString());
            }
            else return;
        }

        private void GetPurchase(string poNo)
        {
            dgvPurchaseDetails.Rows.Clear();
            
            DataTable dt = new DataTable();

            try
            {
                dt = Purchase.GetPurchaseByPo(poNo);
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
                return;
            }

            if (dt != null && dt.Rows.Count > 0)
            {
                // LET'S CONFIGURE PURCHASE
                // Purchase
                isPurchase = true;
                purchaseId = int.Parse(dt.Rows[0]["purchase_id"].ToString());
                iPurchaseId = 0;

                purchase.Clear();
                purchasedItem.Clear();

                purchase.Add("purchase_id", dt.Rows[0]["purchase_id"].ToString());
                purchasedItem.Add("purchase_id", dt.Rows[0]["purchase_id"].ToString());

                purchase.Add("po_no", dt.Rows[0]["po_no"].ToString());
                purchase.Add("supplier_id", dt.Rows[0]["u.supplier_id"].ToString());
                
                ////// initial values
                purchase.Add("sub_total", dt.Rows[0]["sub_total"].ToString());
                purchase.Add("num_items", dt.Rows[0]["num_items"].ToString());
                purchase.Add("debit", dt.Rows[0]["debit"].ToString());
                purchase.Add("credit", dt.Rows[0]["credit"].ToString());

                //Supplier
                isSupplier = false;
                supplierId = dt.Rows[0]["s.supplier_id"].ToString();                
                supplierName = dt.Rows[0]["supplier_name"].ToString();
                supplierEmail = dt.Rows[0]["supplier_email"].ToString();
                supplierMobile = dt.Rows[0]["supplier_mobile"].ToString();

                // Feel purchase header
                dgvPurchaseHeader.Rows[0].Cells[1].Value = dt.Rows[0]["po_no"];
                dgvPurchaseHeader.Rows[0].Cells[3].Value = DateTime.Parse(dt.Rows[0]["po_date_time"].ToString()).ToString("dd/MM/yyyy");
                dgvPurchaseHeader.Rows[0].Cells[5].Value = DateTime.Parse(dt.Rows[0]["due_date_time"].ToString()).ToString("dd/MM/yyyy");
                dgvPurchaseHeader.Rows[0].Cells[7].Value = dt.Rows[0]["supplier_name"];

                // Feel purchase footer
                dgvPurchaseSummary.Rows[0].Cells[3].Value = dt.Rows[0]["sub_total"];
                dgvPurchaseSummary.Rows[1].Cells[3].Value = dt.Rows[0]["debit"];
                dgvPurchaseSummary.Rows[2].Cells[3].Value = dt.Rows[0]["credit"];
            }
            else return;

            try
            {
                dt = new DataTable();
                dt = Purchase.GetPurchasedItemsByPo(poNo);
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message); 
                return;
            }

            if (dt==null || dt.Rows.Count == 0) return;

            foreach (DataRow row in dt.Rows)
                dgvPurchaseDetails.Rows.Add((dgvPurchaseDetails.Rows.Count + 1), row["ID"], row["ItemName"], double.Parse(row["UnitPurchasePrice"].ToString()).ToString("0.00"), double.Parse(row["UnitSalesPrice"].ToString()).ToString("0.00"), row["Qty"], row["Unit"], double.Parse(row["Discount"].ToString()).ToString("0.00"), double.Parse(row["ItemTotal"].ToString()).ToString("0.00"), double.Parse(row["vat"].ToString()).ToString("0.00"), double.Parse(row["Discount"].ToString()).ToString("0.00"), double.Parse(row["vat"].ToString()).ToString("0.00"));
        }

        private void dgvPurchaseDetails_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode== Keys.Delete)
                RemoveItem();
        }


        #region RECEIPT CONFIG
        void PrintReceipt(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            ////    STATIC RECEIPT TEMPLATE ///
            ////    only values are fetched from DB/ data grid view

            int baseY = 80;
            // Store name
            e.Graphics.DrawString(dgvPurchaseHeader.Rows[0].Cells[7].Value.ToString(), new Font("Arial", 10, FontStyle.Bold), Brushes.Black, (e.PageBounds.Width / 2 - 115), baseY);
            baseY += 20;

            // Address
            e.Graphics.DrawString("Gulfesha Plaza, Moghbazar, Dhaka", new Font("Arial", 8, FontStyle.Regular), Brushes.Black, (e.PageBounds.Width / 2 - 140), baseY);
            baseY += 15;

            // Contact no.
            e.Graphics.DrawString("Call: (+88) 01835 410 998, (+88) 01819 244 297", new Font("Arial", 8, FontStyle.Regular), Brushes.Black, (e.PageBounds.Width / 2 - 170), baseY);
            baseY += 25;

            // Invoice no.
            string invoiceno = dgvPurchaseHeader.Rows[0].Cells[1].Value.ToString();
            e.Graphics.DrawString("Invoice No. " + invoiceno, new Font("Arial", 9, FontStyle.Bold), Brushes.Black, 10, baseY);
            baseY += 20;

            ////
            /// DATE AND TIME MISSING! CHECK LATER
            ////

            // Date and time
            // string _sdate = dgvPurchaseHeader.Rows[3].Cells[1].Value.ToString();
            //  string _time = dgvPurchaseHeader.Rows[4].Cells[1].Value.ToString();
            //  e.Graphics.DrawString("Date: " + _sdate + " " + _time, new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 10, baseY);
            //  baseY += 25;

            // Line
            // Create pen.

            Pen blackPen = new Pen(Color.Black, 1);
            blackPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            // Create points that define line.
            Point point1 = new Point(10, 180);
            Point point2 = new Point(600, 180);
            e.Graphics.DrawLine(blackPen, point1, point2);

            //Purchase details (Column header)
            e.Graphics.DrawString("Sl.", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 10, baseY);
            e.Graphics.DrawString("Item Name", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 35, baseY);
            e.Graphics.DrawString("Unit Price", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 200, baseY);
            e.Graphics.DrawString("Qty", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 290, baseY);            
            e.Graphics.DrawString("Sales Price", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 370, baseY);
            //e.Graphics.DrawString("VAT", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 450, baseY);
            e.Graphics.DrawString("Item Total", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 450, baseY);
            baseY += 15 + 10;

            // Purchase details (items)

            Rectangle rect;

            // Create a StringFormat object with each line of text, and the block
            // of text centered on the page.
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Far;
            stringFormat.LineAlignment = StringAlignment.Center;

            StringFormat sFmt2 = new StringFormat();
            sFmt2.Alignment = StringAlignment.Near;
            sFmt2.LineAlignment = StringAlignment.Near;

            for (int i = 0; i < dgvPurchaseDetails.RowCount; i++)
            {
                // Sl.
                e.Graphics.DrawString(dgvPurchaseDetails.Rows[i].Cells[0].Value.ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 10, baseY);
                // Item Name
                rect = new Rectangle(35, baseY - 3, 165, 75);
                //e.Graphics.DrawString(dgvPurchaseDetails.Rows[i].Cells[2].Value.ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 30, baseY);
                e.Graphics.DrawString(dgvPurchaseDetails.Rows[i].Cells[2].Value.ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, sFmt2);

                // Unit Purchase Price
                // Draw the text and the surrounding rectangle.
                rect = new Rectangle(200, baseY - 3, 70, 20);
                e.Graphics.DrawString(double.Parse(dgvPurchaseDetails.Rows[i].Cells[3].Value.ToString()).ToString("N2"), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);

                //Qty
                e.Graphics.DrawString(dgvPurchaseDetails.Rows[i].Cells[5].Value.ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 290, baseY);

                // Unit
                string unit = dgvPurchaseDetails.Rows[i].Cells[6].Value.ToString();
                if (unit.Length > 8)
                    unit = dgvPurchaseDetails.Rows[i].Cells[6].Value.ToString().Substring(0, 8) + "..";
                e.Graphics.DrawString("("+unit+")", new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 320, baseY);

                // Unit Sales Price
                rect = new Rectangle(350, baseY - 3, 70, 20);
                string uSalesPrice = double.Parse(dgvPurchaseDetails.Rows[i].Cells[4].Value.ToString()).ToString("0.00");
                //e.Graphics.DrawString(discount, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 370, baseY);
                e.Graphics.DrawString(uSalesPrice, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);

                /*
                // VAT
                rect = new Rectangle(410, baseY - 3, 70, 20);
                string vat = double.Parse(dgvPurchaseDetails.Rows[i].Cells[10].Value.ToString()).ToString("0.00");
                //e.Graphics.DrawString(vat, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 450, baseY);
                e.Graphics.DrawString(vat, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);
                */

                // Item Total
                rect = new Rectangle(450, baseY - 3, 70, 20);
                string itemtotal = double.Parse(dgvPurchaseDetails.Rows[i].Cells[8].Value.ToString()).ToString("0.00");
                //e.Graphics.DrawString(itemtotal, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 510, baseY);
                e.Graphics.DrawString(itemtotal, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);

                baseY += 15 + 20;
            }

            // Line
            // Create pen.
            baseY += 5;
            blackPen = new Pen(Color.Black, 1);
            blackPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            // Create points that define line.
            point1 = new Point(10, baseY);
            point2 = new Point(600, baseY);
            e.Graphics.DrawLine(blackPen, point1, point2);
            baseY += 5;

            for (int i = 0; i < dgvPurchaseSummary.RowCount; i++)
            {
                /*
                if (i == 1)
                {
                    e.Graphics.DrawString(dgvPurchaseSummary.Rows[0].Cells[0].Value.ToString(), new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 410, baseY);

                    rect = new Rectangle(480, baseY - 3, 70, 20);
                    // Draw the text and the surrounding rectangle.
                    e.Graphics.DrawString(double.Parse(dgvPurchaseSummary.Rows[0].Cells[1].Value.ToString()).ToString("0.00"), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);
                    baseY += 15;
                }
                */

                e.Graphics.DrawString(dgvPurchaseSummary.Rows[i].Cells[2].Value.ToString(), new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 410, baseY);

                rect = new Rectangle(450, baseY - 3, 70, 20);
                // Draw the text and the surrounding rectangle.
                e.Graphics.DrawString(double.Parse(dgvPurchaseSummary.Rows[i].Cells[3].Value.ToString()).ToString("0.00"), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);
                //e.Graphics.DrawString(double.Parse(dgvPurchaseSummary.Rows[i].Cells[4].Value.ToString()).ToString("N2"), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 250, baseY);
                baseY += 15;
            }

            baseY += 10;
            if(double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()) > 0)
                e.Graphics.DrawString("CREDIT AMOUNT: " + double.Parse(dgvPurchaseSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00"), new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 10, baseY);
        }
        #endregion

        /********************************************************************************************************************************************************************************************/

    }
}
