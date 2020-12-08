using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Globalization;
using System.CodeDom;
using System.Collections;
using System.Drawing.Printing;
using IntensePoS.Models;
using System.IO;
using IntensePoS.Lib;

namespace IntensePoS
{
    public partial class OrderScr : Form
    {
        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int LPAR);

        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HT_CAPTION = 0x2;
        
        string ConnectionString = Properties.Settings.Default.connString;

        string ErrorMessage = "";
        string Query = "";
        OleDbConnection Conn = null;

        // selected items array
        int[] selectedItemsId = new int[0];

        // Customer
        bool isCustomer = false;
        string customerId = "0";
        string iCustomerId = "0";
        string customerName = "";
        string customerEmail = "";
        string customerMobile = "";

        string[] oUnits = new string[0];

        // calc operators
        string calcOperator = "";

        // operands
        double operand1, operand2, calcResult = 0;

        // ops flag
        Dictionary<string, int> opsFlag = new Dictionary<string, int>();

        /////////
        TextInfo ti = CultureInfo.CurrentCulture.TextInfo;

        int oFlag = 0;

        // Payment Mode
        string payMode = "";


        /// <summary>
        /// Not required to reset everytime for state changes
        /// On time updatable field
        double oldDiscount = 0;
        bool reprint = false;
        /// </summary>

        /// SYSTEM CONFIG DATA
        /// (will not be changed once initialized and till the shut down) 
        string __API_KEY = "";
        string warehouse_name = "";
        string warehouse_location = "";
        bool keyVerified = false;
        bool dbVerified = false;
        Dictionary<string, string> terminal = new Dictionary<string, string>();
        /////////////////////////////////////////////////////////////////

        /// <summary>
        /// SESSION DATA (GETS CHANGED ONLY WHEN USER SIGNS OUT)
        bool accessVerified = false;
        string username = "";
        /// </summary>

        public OrderScr()
        {            
            InitializeComponent();
            
            keyVerified = AuthenticationKeyVerified();
            
            /*
            if (!keyVerified)
            {
                Application.ExitThread();
                foreach (Form form in Application.OpenForms)
                {
                    form.Close();
                }
                Application.Exit();                
                return;
            }
            */
        }


        #region Settings
        /*********************************************************************************************************************************************************************/
        /// <summary>
        /// ///////////////////////////
        /// PoS Global Settings and Configurations
        /// </summary>        
        /*********************************************************************************************************************************************************************/
        
        private bool AuthenticationKeyVerified()
        {

            dbVerified = PoSConfig.VerifyDatabase();
            if (!dbVerified)
            {
                MessageBox.Show("Couldn't create the data storage! \nPlease make sure that your internet connection is active. Or, contact the vendor.", "Fatal Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);                
                return false;
            }

            string sysMsg = "";
            
            if (Conn != null && Conn.State == ConnectionState.Closed)
                Conn.Open();
            else
            {
                Conn = new OleDbConnection(ConnectionString);
                Conn.Open();
            }
            
            OleDbCommand cmd = new OleDbCommand();
            cmd.Connection = Conn;

            try
            {
                //string sql = "SELECT API_key FROM __intense_terminal WHERE is_active = true";
                string sql = "SELECT [__intense_terminal].*, [__intense_warehouse].[warehouse_name], [__intense_warehouse].[warehouse_location] FROM [__intense_terminal] INNER JOIN [__intense_warehouse] ON [__intense_terminal].[warehouse_id] = [__intense_warehouse].[warehouse_id] WHERE [__intense_terminal].[is_active] = true";
                cmd.CommandText = sql;                

                DataTable dt = new DataTable();

                try
                {
                    OleDbDataAdapter da = new OleDbDataAdapter(cmd);                    
                    da.Fill(dt);
                }
                catch(Exception x)
                {
                    MessageBox.Show(x.Message);
                }

                //if (result != null)
                if (dt.Rows.Count > 0)
                {
                    //__API_KEY = result.ToString();
                    __API_KEY = dt.Rows[0]["API_key"].ToString();
                    warehouse_name = dt.Rows[0]["warehouse_name"].ToString();
                    warehouse_location = dt.Rows[0]["warehouse_location"].ToString();
                    keyVerified = true;
                }
                Conn.Close();
            }
            catch (Exception err)
            {                                
                sysMsg += "\nError!\n" + err.Message;
                MessageBox.Show(sysMsg, "Authentication Failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);                
                Conn.Close();
                err.Equals(null);                
            }
           
            return keyVerified;
        }

        
        private void VerifyAuthKey()
        {            

            SettingsPrompt scr = new SettingsPrompt(0, keyVerified);

            /////////////////////////////////////////
            //scr.MdiParent = this;   // Fix it later
            /////////////////////////////////////////
            //scr.Size = new Size(this.Width * 50 / 100, this.Height * 50 / 100);

            scr.StartPosition = FormStartPosition.CenterScreen;
            scr.ShowIcon = false;
            scr.ShowInTaskbar = false;
            scr.ShowDialog();
        }
        #endregion


        private void btnCloseOrderScr_Click(object sender, EventArgs e)
        {            
            this.Close();
        }

        private void OrderScr_Load(object sender, EventArgs e)
        {            
            if(!dbVerified) Application.Exit();            

            // Load UI
            LoadUI();
            
            lPane.Visible = false;
            rPane.Visible = false;

            mPane.Visible = iPane.Visible = false;
            btnCloseOrderScr.Left = 42;
            btnCloseSession.Visible = false;

            //Access PIN
            txtAccessPIN.SelectAll();
            txtAccessPIN.Focus();

            // topmenu is hidden first time and login
            menubarTop.Enabled = false;


            /**********************************************************************************/            
            if (!keyVerified)
            {
                VerifyAuthKey();
                //return;
            }
            /**********************************************************************************/
        }

        private void OrderSummaryHeadsShowUp()
        {
            /// Application loading time only
            dgvOrderSummary.Rows.Add("VAT", "0.00", "Sub Total", "0.00");
            dgvOrderSummary.Rows.Add("", "", "Discount", "0.00");
            dgvOrderSummary.Rows.Add("", "", "Grand Total", "0.00");
            dgvOrderSummary.Rows.Add("", "", "Payment", "0.00");
            dgvOrderSummary.Rows.Add("", "", "Change", "0.00");
            dgvOrderSummary.Rows.Add("", "", "Due", "0.00");
            dgvOrderSummary.Rows[0].Cells[0].Style.ForeColor = Color.RoyalBlue;
        }

        private void LoadUI ()
        {            

            gbAccess.Top = this.Height / 2 - gbAccess.Height / 2;
            gbAccess.Left = this.Width / 2 - gbAccess.Width / 2;

            btnCloseOrderScr.Top = this.Height - (btnCloseOrderScr.Height + 50);
            btnCloseOrderScr.Left = this.Width - (btnCloseOrderScr.Width + 35) - 42;

            rPane.Width = this.Width / 4;
            lPane.Width = this.Width - rPane.Width - mPane.Width - iPane.Width -10;
            lPane.BackColor = Color.AliceBlue; 
            rPane.BackColor = Color.AntiqueWhite;

            tCtrlHPane.Height = tSearchPane.Height = tCtrlFPane.Height = lPane.Height / 15;
            tOrderPane.Height = (lPane.Height - (tCtrlHPane.Height + tSearchPane.Height + tCtrlFPane.Height)) * 4/5 - 20 - 40;     // to add Due in the dgvOrderSummary      

            tOrderFooterPane.Height = lPane.Height - (tOrderPane.Height + tCtrlHPane.Height + tSearchPane.Height + tCtrlFPane.Height) -15;

            //// tCtrlHPane is now invisible
            //tOrderPane.Height = tOrderPane.Height + tCtrlHPane.Height;

            tOrderPane.Height = tOrderPane.Height + tCtrlHPane.Height;

            // Last column takes the rest of width of its container            
            dgvOrderDetails.Columns[dgvOrderDetails.Columns.Count - 7].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvOrderSummary.Columns[2].Width = 1230 - 300;
            dgvOrderSummary.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;            
            OrderSummaryHeadsShowUp();
            dgvOrderSummary.Rows[0].Cells[0].Style.SelectionForeColor = dgvOrderSummary.Rows[0].Cells[0].Style.ForeColor = Color.DarkOrange;

            // tCtrlHPane top control header panel
            tCtrlHPane.BackColor = SystemColors.ControlLightLight;
            btnSwitchDashboard.Top = 10;
            btnSwitchDashboard.Left = 0;//10;
            btnSwitchDashboard.Height = tCtrlHPane.Height - 10 ;
            btnSwitchDashboard.Width = 180;

            // tSearchPane top product selection panel
            tSearchPane.BackColor = SystemColors.ControlLightLight;
            cmbProductDropDown.Top = 10;
            cmbProductDropDown.Left = 0;//10;
            cmbProductDropDown.Width = tSearchPane.Width; // - 20;            

            // tCtrlHPane top control header panel
            tCtrlFPane.BackColor = SystemColors.ControlLightLight;            
            btnReturnGoods.Height = btnCancelOrder.Height = btnQPrev.Height = btnQNext.Height = btnCloseOrderScr.Height = btnCloseSession.Height = btnRemoveItem.Height = btnSwitchDashboard.Height;
            btnReturnGoods.Width = btnCancelOrder.Width = btnQPrev.Width = btnQNext.Width = btnCloseOrderScr.Width = btnCloseSession.Width = btnRemoveItem.Width = btnSwitchDashboard.Width;
            btnReturnGoods.Top = btnCancelOrder.Top = btnQPrev.Top = btnQNext.Top = btnRemoveItem.Top = 10;
            btnReturnGoods.Left = 10;
            btnCancelOrder.Left = btnReturnGoods.Left + btnReturnGoods.Width + 10;            
            btnQNext.Left = tOrderFooterPane.Width - (btnQNext.Width);
            btnQPrev.Left = btnQNext.Left - (20 + btnQPrev.Width) ;
            btnCancelOrder.Left = btnQPrev.Left - (20 + btnCancelOrder.Width);
            btnReturnGoods.Left = btnCancelOrder.Left - (20 + btnReturnGoods.Width);
            btnRemoveItem.Left = btnReturnGoods.Left - btnRemoveItem.Width - 20;

            // rPane
            middleRightCornerPane.Height = rPane.Height / 15;
            topRightCornerPane.Height = rPane.Height - (tOrderFooterPane.Height+tCtrlFPane.Height + middleRightCornerPane.Height * 3) - 20 + 35;    // Due added in the dgvOrderSummary
            bottomRightCornerPane.Height = rPane.Height - (topRightCornerPane.Height + middleRightCornerPane.Height);
            btnPrintOrder.Top = btnPrintOrder.Left = 10;            
            btnPrintOrder.Height = middleRightCornerPane.Height - 15;
            btnPrintOrder.Width = middleRightCornerPane.Width - 40;
            middleRightCornerPane.BackColor = bottomRightCornerPane.BackColor = SystemColors.ControlLightLight;

            // calcPane calculator pane
            calcPane.Width = btnPrintOrder.Width+5;
            calcPane.Height = bottomRightCornerPane.Height - 25;
            calcPane.Left = btnPrintOrder.Left - 5;
            //calcPane.Top = calcPane.Top - 5;
            calcPane.Top = calcPane.Height/25 ;

            // txtCalcInput (calculation screen)
            txtCalcInput.Width = calcPane.Width-40;

            // paymentPane
            paymentPane.Width = calcPane.Width;
            paymentPane.Height = topRightCornerPane.Height - 20;
            paymentPane.Left = calcPane.Left;
            paymentPane.Top = 10;

            /////
            btnPrintOrder.Left = calcPane.Left;
            btnPrintOrder.Width = calcPane.Width;

            // Close button
            btnCloseOrderScr.Top = btnCloseSession.Top = tCtrlFPane.Top + 33;
            btnCloseOrderScr.Left = lPane.Left ;
            btnCloseSession.Left = btnCloseOrderScr.Left + btnCloseOrderScr.Width + 20;

            ////
            tCtrlFPane.Height += 20;

            // Invoice Head
            /// When the application ready
            //dgvInvoiceHead.Rows.Add("Invoice No.", "O251020");
            dgvInvoiceHead.Rows.Add("Invoice No.", "");
            dgvInvoiceHead.Rows.Add("Order Amount", "0.00");
            dgvInvoiceHead.Rows.Add("Discount", "0.00");
            dgvInvoiceHead.Rows.Add("Date", System.DateTime.Now.Date.ToString("dd/MM/yyyy"));
            dgvInvoiceHead.Rows.Add("Time", "");
            //dgvInvoiceHead.Rows.Add("Time", System.DateTime.Now.ToLocalTime().ToString("h:mm tt"));
            dgvInvoiceHead.Rows.Add("Status", "");
            dgvInvoiceHead.Rows.Add("Shop", warehouse_name);                                    //"");
            dgvInvoiceHead.Rows.Add("Branch", warehouse_location);                               //"");
            dgvInvoiceHead.Rows.Add("Customer Mobile No.", "");
            dgvInvoiceHead.Rows.Add("Prepared By", username);

            dgvInvoiceHead.Columns[0].Width = topRightCornerPane.Width / 3 + 60;
            dgvInvoiceHead.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // highlight
            dgvInvoiceHead.Rows[2].Cells[1].Style.ForeColor = Color.Green;
            dgvInvoiceHead.Rows[2].Cells[1].Style.Font = new Font("Arial", 20, FontStyle.Bold);
            dgvInvoiceHead.Rows[5].Cells[1].Style.ForeColor = Color.Red;

            dgvInvoiceHead.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            btnCustomer.Top = btnPreviousOrders.Top = dgvInvoiceHead.Height + 10;
            btnCoupon.Top = btnDiscount.Top = btnPayment.Top = btnCustomer.Top + btnCustomer.Height + 15;

            // Customer
            btnSaveCustomer.Enabled = false;
            btnSaveCustomer.BackColor = Color.AliceBlue;
            btnSaveCustomer.ForeColor = SystemColors.ControlDark;

            
            // At the end            
            cmbProductDropDown.Focus();
        }


        private bool IsRePrint (object sender, EventArgs e)
        {
            bool reprint = false;

            if (dgvInvoiceHead.Rows[5].Cells[1].Value.ToString().ToUpper() == "OPEN")
                return false;
            else
            {
                if (isOrder && orderId > 0)
                {
                    try
                    {
                        Conn = new OleDbConnection(ConnectionString);
                        Conn.Open();
                        OleDbCommand cmd = new OleDbCommand();
                        cmd.Connection = Conn;
                        cmd.CommandText = "SELECT cur_status FROM orders WHERE ID = " + orderId.ToString();
                        string status = cmd.ExecuteScalar().ToString();
                        if(status.ToUpper() == "SAVED")
                        {
                            /// ---- WORKS TO DO OR NEW?
                            /// NOTHING TO DO
                            /// -----

                            this.reprint = true;
                            btnCloseSession_Click(sender, e);
                            return this.reprint;    // or // return true;
                        }
                    }
                    catch(Exception x)
                    {
                        MessageBox.Show(x.Message);
                        return false;
                    }
                }

                return reprint;
            }            
        }

        private string InitializeOrderInvoice()
        {

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

            Query = "Select MAX(ID) AS ID FROM orders WHERE DateValue(order_date_time) = Date()";
            cmd.CommandText = Query;

            string result = "";

            try
            {
                result = cmd.ExecuteScalar().ToString();
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }

            Conn.Close();

            string invoiceNo_str = (result + 1).ToString();
            if (invoiceNo_str.Length >= 3)
                invoiceNo_str = invoiceNo_str.Remove(0, invoiceNo_str.Length - 3);
            else
                for (int i = 0; i < 3; i++)
                {
                    invoiceNo_str = "0" + invoiceNo_str;
                    if (invoiceNo_str.Length == 3) break;
                }

            return invoiceNo_str;
        }


        private void btnEnterPoS_Click(object sender, EventArgs e)
        {
            
            accessVerified = VerifyAccess(txtAccessPIN.Text);
            
            if (!accessVerified)
            {
                MessageBox.Show("Access Failed!\n Please insert correct PIN or contact the ecommerce admin.", "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtAccessPIN.Text = "";
                return;
            }

            if(!terminal.ContainsKey("API_key")) terminal.Add("API_key", __API_KEY);

            try
            {
                if (Conn != null && Conn.State == ConnectionState.Closed)
                    Conn.Open();
                else
                {
                    Conn = new OleDbConnection(ConnectionString);
                    Conn.Open();
                }

                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = Conn;


                cmd.CommandText = "SELECT * FROM users WHERE PIN = " + txtAccessPIN.Text;

                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                username = dt.Rows[0]["username"].ToString();

                if (dt.Rows.Count > 0)
                {
                    if (terminal.ContainsKey("terminal_id")) terminal["terminal_id"] = dt.Rows[0]["terminal_server_id"].ToString(); else terminal.Add("terminal_id", dt.Rows[0]["terminal_server_id"].ToString());
                    if (terminal.ContainsKey("pos_user_id")) terminal["pos_user_id"] = dt.Rows[0]["sync_id"].ToString(); else terminal.Add("pos_user_id", dt.Rows[0]["sync_id"].ToString());
                    if (terminal.ContainsKey("pos_user")) terminal["pos_user"] = username; else terminal.Add("pos_user", username);
                    if (dgvInvoiceHead.Rows.Count > 8)
                        dgvInvoiceHead.Rows[9].Cells[1].Value = username;
                }

            }
            catch (Exception x)
            {
                MessageBox.Show("There is a problem to verify the user. Please contact the vendor.\nDetails\n" + x.Message, "Authentication Error!");
                return;
            }

            mPane.Visible = iPane.Visible = true;
            btnCloseOrderScr.Left -= 21;
            btnCloseSession.Left = btnCloseOrderScr.Left + btnCloseOrderScr.Width + 20;

            menubarTop.Enabled = true;
            txtAccessPIN.Text = "";
            gbAccess.Visible = false;
            btnCloseSession.Visible = true;

            lPane.Visible = true;
            rPane.Visible = true;
            cmbProductDropDown.SelectAll(); cmbProductDropDown.Focus();
        }

        private bool VerifyAccess (string accessPin)
        {                    
            bool success =  PoSConfig.Login(accessPin);            
            accessVerified = success;

            /// WAIT FOR API MODIFICATION dummy_login and verify_pos
            //if (success) GetUserName();
            
            return accessVerified;
        }

        private void btnCloseSession_Click(object sender, EventArgs e)
        {
            mPane.Visible = iPane.Visible = false;
            btnCloseOrderScr.Left += 21;

            menubarTop.Enabled = false;
            gbAccess.Visible = true;
            btnCloseSession.Visible = false;

            lPane.Visible = false;
            rPane.Visible = false;

            txtAccessPIN.Focus();
        }

        private void btnApplyCoupon_Click(object sender, EventArgs e)
        {
            switch (txtCouponDiscount.RightToLeft)
            {
                // You are updating any numeric value
                case RightToLeft.No:
                    if (double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString()) == oldDiscount)
                    {
                        txtCouponDiscount.Text = (double.Parse(txtCouponDiscount.Text) + double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString())).ToString("0.00");
                        string additional_discount = double.Parse(txtCouponDiscount.Text).ToString();
                        if (order.ContainsKey("additional_discount")) order["additional_discount"] = additional_discount; else order.Add("additional_discount", additional_discount);
                        if (rdoAmount.Checked)
                            if (order.ContainsKey("additional_discount_type"))
                                order["additional_discount_type"] = "amount";
                            else order.Add("additional_discount_type", "amount");
                        else
                            if (order.ContainsKey("additional_discount_type"))
                               order["additional_discount_type"] = "percent";
                            else order.Add("additional_discount_type", "percent");
                        oldDiscount = 0;
                    }
                    dgvOrderSummary.Rows[1].Cells[3].Value = txtCalcInput.Text = txtCouponDiscount.Text;
                    dgvInvoiceHead.Rows[2].Cells[1].Value = txtCouponDiscount.Text;                    
                    dgvOrderSummary.Rows[2].Selected = true;                    
                    break;

                // You are updating any text, e.g.: invoice
                case RightToLeft.Yes:                    
                    ///////////////////////////////////////////////
                    txtCouponDiscount.RightToLeft = RightToLeft.No;    // DECIDE/ RESET WITH THIS // NO GLOBAL VARIABLE
                    txtInvoiceNo.Visible = false;
                    rdoAmount.Visible = rdoPercent.Visible = true;                    
                    txtCouponDiscount.Top = gbCouponDiscount.Height / 2 + rdoAmount.Top + rdoAmount.Height + 20;
                    btnApplyCoupon.Top = txtCouponDiscount.Top + txtCouponDiscount.Height + 20;
                    ///////////////////////////////////////////////
                    ///
                    // Get order
                    string invoiceNo = txtInvoiceNo.Text;
                    ShowOrderListPane();
                    rdoQueue.Checked = rdoPaid.Checked = rdoDue.Checked = false;
                    rdoQueue.Enabled = rdoPaid.Enabled = rdoDue.Enabled = false;                    
                    gbOrderList.Text = "ORDERS";
                    OrderListFilterByInvoice(invoiceNo);                    

                    break;

            }

            gbCouponDiscount.Visible = false;
            lPane.Enabled = true;
            rPane.Enabled = true;

            // exit - close session enabled
            btnCloseOrderScr.Enabled = true;
            btnCloseSession.Enabled = true;

            txtCouponDiscount.Text = "";
            cmbProductDropDown.Focus();
        }

        private void btnDiscount_Click(object sender, EventArgs e)
        {
            dgvOrderSummary.Rows[1].Selected = true;
            txtCalcInput.Text = dgvOrderSummary.Rows[1].Cells[3].Value.ToString();

            lPane.Enabled = false;
            rPane.Enabled = false;
            gbCouponDiscount.Visible = true;            

            gbCouponDiscount.Left = this.Width / 2 - gbCouponDiscount.Width / 2;
            gbCouponDiscount.Top = this.Height / 2 - gbCouponDiscount.Height / 2;

            gbCouponDiscount.Text = "ENTER DISCOUNT AMOUNT";

            // exit - close session disabled
            btnCloseOrderScr.Enabled = false;
            btnCloseSession.Enabled = false;

            ///////////////////////////////////////////////
            txtCouponDiscount.RightToLeft = RightToLeft.No;    // DECIDE/ RESET WITH THIS // NO GLOBAL VARIABLE
            txtInvoiceNo.Visible = false;
            btnAddDiscountOnGT.Visible = txtCouponDiscount.Visible = true; txtCouponDiscount.BringToFront();
            rdoAmount.Visible = rdoPercent.Visible = true;
            txtCouponDiscount.Top = rdoAmount.Top + rdoAmount.Height + 20;
            btnApplyCoupon.Top = txtCouponDiscount.Top + txtCouponDiscount.Height + 20;
            ///////////////////////////////////////////////

            txtCouponDiscount.Text = dgvOrderSummary.Rows[1].Cells[3].Value.ToString();
            txtCouponDiscount.SelectAll();
            txtCouponDiscount.Focus();
        }

        private void dgvOrderSummary_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            switch (e.RowIndex)
            {
                case 1:
                case 3:
                    txtCalcInput.Text = dgvOrderSummary.Rows[e.RowIndex].Cells[3].Value.ToString();
                    break;
            }
            cmbProductDropDown.SelectAll();
            cmbProductDropDown.Focus();
        }

        private void txtCalcInput_TextChanged(object sender, EventArgs e)
        {
            //// Think later!!!!
            ///
            if (txtCalcInput.Text.Length > 17) return;
            
            if (calcOperator != "") return;
            ///

            if (!IsFloat(txtCalcInput.Text) || double.Parse(txtCalcInput.Text) < 0)
            {               
                txtCalcInput.Text = "0.00";                
            }

            txtCalcInput.Text = double.Parse(txtCalcInput.Text).ToString("0.00"); 
            
            // check
            txtCalcInput.SelectionStart = txtCalcInput.Text.Length-3;

            int i = dgvOrderSummary.SelectedRows[0].Index;

            switch (i)
            {
                case 1:
                case 3:
                    dgvOrderSummary.Rows[i].Cells[3].Value = txtCalcInput.Text;
                    UpdateOrderSummary(i);
                    break;
            }
        }

        // UTILITY FUNCTION - ANY/ ALL PURPOSE

        private bool IsNumeric(string input)
        {
            int test;
            return int.TryParse(input, out test);
        }

        private bool IsFloat(string input)
        {
            float test;
            return float.TryParse(input, out test);
        }

        private void UpdateOrderSummary(int i)
        {
            switch(i)
            {
                case 0:
                    UpdateTotal();
                    break;

                case 1:
                    if (!((double.Parse(dgvOrderSummary.Rows[0].Cells[3].Value.ToString()) - double.Parse(dgvOrderSummary.Rows[i].Cells[3].Value.ToString())) < 0))
                    {
                        dgvOrderSummary.Rows[2].Cells[3].Value = double.Parse(dgvOrderSummary.Rows[0].Cells[3].Value.ToString()) + double.Parse(dgvOrderSummary.Rows[0].Cells[1].Value.ToString()) - double.Parse(dgvOrderSummary.Rows[i].Cells[3].Value.ToString());                                               
                    }
                    else
                    {
                        //MessageBox.Show("Wrong payment amount!", "Payment");
                        dgvOrderSummary.Rows[2].Cells[3].Value = 0.00; //return;
                    }
                    dgvInvoiceHead.Rows[2].Cells[1].Value = double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString());
                    break;
                case 3:
                    if (!((double.Parse(dgvOrderSummary.Rows[i].Cells[3].Value.ToString()) - double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString())) <= 0))
                    {
                        dgvOrderSummary.Rows[4].Cells[3].Value = double.Parse(dgvOrderSummary.Rows[i].Cells[3].Value.ToString()) - double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString());
                        dgvOrderSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.SelectionForeColor = dgvOrderSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                    }
                    else
                    {                        
                        dgvOrderSummary.Rows[4].Cells[3].Value = 0.00;
                        dgvOrderSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.SelectionForeColor = dgvOrderSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.ForeColor = Color.Red;                        
                    }

                    if ((double.Parse(dgvOrderSummary.Rows[i].Cells[3].Value.ToString()) - double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString())) == 0)
                        dgvOrderSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.SelectionForeColor = dgvOrderSummary.Rows[3].Cells[3].OwningRow.DefaultCellStyle.ForeColor = SystemColors.ControlText;

                    double due = double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString()) - double.Parse(dgvOrderSummary.Rows[i].Cells[3].Value.ToString());
                    // Due
                    //if ((double.Parse(dgvOrderSummary.Rows[i].Cells[3].Value.ToString()) - double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString())) < 0)
                    if (due > 0)
                        dgvOrderSummary.Rows[5].Cells[3].Value = due; //double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString()) - double.Parse(dgvOrderSummary.Rows[i].Cells[3].Value.ToString());
                    else
                        dgvOrderSummary.Rows[5].Cells[3].Value = 0;

                    break;  
            }

            
            if (!((double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()) - double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString())) <= 0))
                dgvOrderSummary.Rows[4].Cells[3].Value = double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()) - double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString());
            else
                dgvOrderSummary.Rows[4].Cells[3].Value = 0.00;

            if (isOrder && order.Count > 0)
            {
                order["discount"] = double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                order["grand_total"] = double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00");
                order["payment"] = double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()).ToString("0.00");
                order["changes"] = double.Parse(dgvOrderSummary.Rows[4].Cells[3].Value.ToString()).ToString("0.00");
                order["due"] = double.Parse(dgvOrderSummary.Rows[5].Cells[3].Value.ToString()).ToString("0.00");
                UpdateOrder(order);
            }

        }

        private void txtCouponDiscount_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.KeyCode)
            {
                case Keys.Escape:
                    CancelApplyingDiscount();
                    break;

                case Keys.A:                   
                    rdoAmount.Checked = true;
                    gbCouponDiscount.Text = "ENTER DISCOUNT AMOUNT";
                    rdoAmount.ForeColor = Color.Black;
                    rdoPercent.ForeColor = SystemColors.HighlightText;
                    break;

                case Keys.D:
                case Keys.P:
                    rdoPercent.Checked = true;
                    gbCouponDiscount.Text = "ENTER DISCOUNT (%)";
                    rdoPercent.ForeColor = Color.Black;
                    rdoAmount.ForeColor = SystemColors.HighlightText;
                    break;
            }            
        }

        private void CheckOrderSummary()
        {
            if (!IsFloat(txtCalcInput.Text))
            {
                txtCalcInput.Text = "";
                return;
            }

            int i = dgvOrderSummary.SelectedRows[0].Index;

            switch (i)
            {
                case 1:
                case 3:
                    dgvOrderSummary.Rows[i].Cells[1].Value = txtCalcInput.Text;
                    UpdateOrderSummary(i);
                    break;
            }
        }

        private void CancelApplyingDiscount()
        {
            gbCouponDiscount.Visible = false;
            lPane.Enabled = true;
            rPane.Enabled = true;

            // exit - close session enabled
            btnCloseOrderScr.Enabled = true;
            btnCloseSession.Enabled = true;            

            cmbProductDropDown.Focus();
        }

        private void CancelSavingCustomer ()
        {
            CustomerPane.Visible = false;
            lPane.Enabled = true;
            rPane.Enabled = true;

            // exit - close session enabled
            btnCloseOrderScr.Enabled = true;
            btnCloseSession.Enabled = true;

            txtCustomerName.Text = "";
            txtCustomerEmail.Text = "";
            txtCustomerMobile.Text = "";
            btnSaveCustomer.Enabled = false;

            // Customer
            btnSaveCustomer.Enabled = false;
            btnSaveCustomer.BackColor = Color.AliceBlue;
            btnSaveCustomer.ForeColor = SystemColors.ControlDark;

            //isCustomer = false;   // Not required because customer can be selected.
                        
            btnSaveCustomer.Text = "Find";

            // Lock input customer. Instead, decide either take order or cancel
            txtCustomerName.ReadOnly = txtCustomerEmail.ReadOnly = txtCustomerMobile.ReadOnly = false;

            cmbProductDropDown.Focus();
        }

        private void CancelSavingOrder()
        {
            gbPrintConfirmation.Visible = false;
            lPane.Enabled = true;
            rPane.Enabled = true;

            // exit - close session enabled
            btnCloseOrderScr.Enabled = true;
            btnCloseSession.Enabled = true;

            oFlag = 0;
            payMode = "";

            txtCalcInput.SelectAll();
            txtCalcInput.Focus();
        }

        private void txtCouponDiscount_TextChanged(object sender, EventArgs e)
        {
            if (!IsFloat(txtCouponDiscount.Text) || double.Parse(txtCouponDiscount.Text) < 0 || double.Parse(dgvOrderSummary.Rows[0].Cells[3].Value.ToString()) < double.Parse(txtCouponDiscount.Text))
            {
                txtCouponDiscount.Text = "0.00";
            }

            txtCouponDiscount.Text = double.Parse(txtCouponDiscount.Text).ToString("0.00");

            // check
            txtCouponDiscount.SelectionStart = txtCouponDiscount.Text.Length - 3;
        }

        private void btnAdjustDecimal_Click(object sender, EventArgs e)
        {
            txtCalcInput.Text = (double.Parse(txtCalcInput.Text) * 100).ToString();
        }

        private void SetDiscountType()
        {
            double grandTotal = double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString());
            double discount = double.Parse(txtCouponDiscount.Text);
            if (rdoPercent.Checked)
                discount = grandTotal * discount / 100 + double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString()) ;
            txtCouponDiscount.Text = discount.ToString("0.00");
        }

        private void txtCouponDiscount_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                e.Handled = true;

                SetDiscountType();

                if (double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString()) == oldDiscount)
                {
                    txtCouponDiscount.Text = (double.Parse(txtCouponDiscount.Text) + double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString())).ToString("0.00");
                    oldDiscount = 0;
                }
                
                dgvOrderSummary.Rows[1].Cells[3].Value = txtCalcInput.Text = txtCouponDiscount.Text;
                gbCouponDiscount.Visible = false;
                lPane.Enabled = true;
                rPane.Enabled = true;

                // exit - close session enabled
                btnCloseOrderScr.Enabled = true;
                btnCloseSession.Enabled = true;

                txtCouponDiscount.Text = "";

                dgvOrderSummary.Rows[2].Selected = true;

                cmbProductDropDown.Focus();
            }
        }

        private void btnCustomer_Click(object sender, EventArgs e)
        {
            lPane.Enabled = false;
            rPane.Enabled = false;
            CustomerPane.Visible = true;

            CustomerPane.Left = this.Width / 2 - CustomerPane.Width / 2;
            CustomerPane.Top = this.Height / 2 - CustomerPane.Height / 2 - 30;

            CustomerPane.Text = "Customer";

            // exit - close session disabled
            btnCloseOrderScr.Enabled = false;
            btnCloseSession.Enabled = false;

            // Customer
            btnSaveCustomer.BackColor = Color.AliceBlue;
            btnSaveCustomer.ForeColor = SystemColors.ControlDark;

            if (isCustomer && dgvInvoiceHead.Rows[8].Cells[1].Value.ToString() != "-" || isCustomer && dgvInvoiceHead.Rows[8].Cells[1].Value.ToString() != "")
            {
                txtCustomerName.Text = customerName;
                txtCustomerEmail.Text = customerEmail;
                txtCustomerMobile.Text = customerMobile;

                btnSaveCustomer.Text = "Change";
                btnSaveCustomer.BackColor = Color.RoyalBlue;
                btnSaveCustomer.ForeColor = SystemColors.HighlightText;
                btnSaveCustomer.Enabled = true;
            }
            else
            {
                txtCustomerName.Text = "";
                txtCustomerEmail.Text = "";
                txtCustomerMobile.Text = "";
            }

            txtCustomerMobile.Focus();
        }

        private void ShowCustomerPane()
        {
            lPane.Enabled = false;
            rPane.Enabled = false;
            CustomerPane.Visible = true;

            CustomerPane.Left = this.Width / 2 - CustomerPane.Width / 2;
            CustomerPane.Top = this.Height / 2 - CustomerPane.Height / 2 - 30;

            CustomerPane.Text = "Customer";

            // exit - close session disabled
            btnCloseOrderScr.Enabled = false;
            btnCloseSession.Enabled = false;

            // Customer
            btnSaveCustomer.BackColor = Color.AliceBlue;
            btnSaveCustomer.ForeColor = SystemColors.ControlDark;

            if (isCustomer && dgvInvoiceHead.Rows[8].Cells[1].Value.ToString() != "-" || isCustomer && dgvInvoiceHead.Rows[8].Cells[1].Value.ToString() != "")
            {
                txtCustomerName.Text = customerName;
                txtCustomerEmail.Text = customerEmail;
                txtCustomerMobile.Text = customerMobile;

                btnSaveCustomer.Text = "Change";
                btnSaveCustomer.BackColor = Color.RoyalBlue;
                btnSaveCustomer.ForeColor = SystemColors.HighlightText;
                btnSaveCustomer.Enabled = true;
            }
            else
            {
                txtCustomerName.Text = "";
                txtCustomerEmail.Text = "";
                txtCustomerMobile.Text = "";
            }

            txtCustomerMobile.Focus();
        }

        private void txtCustomerName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                CancelSavingCustomer();
        }

        private void txtCustomerEmail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                CancelSavingCustomer();
        }

        private void txtCustomerMobile_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                CancelSavingCustomer();                                    
        }

        private void btnCancelCustomerWin_Click(object sender, EventArgs e)
        {
            CancelSavingCustomer();            
        }

        private void cmbProductDropDown_TextChanged(object sender, EventArgs e)
        {
            //25-11-2020 4:45 PM //// CUT SHORT...
            //return; // N.B.: Comment Later!

            if (cmbProductDropDown.Text == "")
               return;

            if(dgvOrderDetails.Rows.Count>0)
            {                
                foreach (DataGridViewRow row in dgvOrderDetails.Rows)
                    if(double.Parse(row.Cells[4].Value.ToString()) == 0)
                    {                        
                        dgvOrderDetails.CurrentCell = row.Cells[4];
                        dgvOrderDetails.BeginEdit(true);
                        return;
                    }    
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

            //Query = "SELECT ID AS ID, 1 AS Qty, unit AS Unit, product_name AS ItemName, unit_price AS UnitPrice, (1 * unit_price - discount + vat) AS ItemTotal, discount AS Discount, vat FROM products WHERE shortcode = '" + cmbProductDropDown.Text + "' OR barcode = '" + cmbProductDropDown.Text + "' OR product_name LIKE '%" + cmbProductDropDown.Text + "%'";
            //Query = "SELECT products.ID, 1 AS Qty, products.unit AS Unit, products.product_name AS ItemName, VAL(products.unit_price) AS UnitPrice, (1 * VAL(products.unit_price) - VAL(products.discount) + VAL(products.vat)) AS ItemTotal, VAL(products.discount) AS Discount, VAL(products.vat) AS VAT, products.unit AS bUnit, inventories.volume AS Volume FROM products INNER JOIN inventories ON products.ID = inventories.product_id  WHERE VAL(products.barcode) = " + cmbProductDropDown.Text + " AND inventories.volume > 0 ";

            /// Commmented at 1/12/2020 4:43 
            //Query = "SELECT products.ID, 1 AS Qty, products.unit AS Unit, products.product_name AS ItemName, VAL(products.unit_price) AS UnitPrice, (1 * VAL(products.unit_price)) AS ItemTotal, VAL(products.discount) AS Discount, VAL(products.vat) AS VAT, products.unit AS bUnit, inventories.volume AS Volume, products.image FROM products INNER JOIN inventories ON products.ID = inventories.product_id  WHERE VAL(products.barcode) = " + cmbProductDropDown.Text + " AND inventories.volume > 0 ";

            //Query = "SELECT products.ID, 1 AS Qty, products.unit AS Unit, products.product_name AS ItemName, VAL(products.unit_price) AS UnitPrice, (1 * VAL(products.unit_price)) AS ItemTotal, VAL(products.discount) AS Discount, VAL(products.vat) AS VAT, products.unit AS bUnit, inventories.volume AS Volume, products.image FROM products INNER JOIN inventories ON products.ID = inventories.product_id  WHERE products.barcode = '" + cmbProductDropDown.Text + "' OR products.sync_id = " + cmbProductDropDown.Text + " AND inventories.volume > 0 ";
            Query = "SELECT products.ID, 1 AS Qty, products.unit AS Unit, products.product_name AS ItemName, VAL(products.unit_price) AS UnitPrice, (1 * VAL(products.unit_price)) AS ItemTotal, VAL(products.discount) AS Discount, VAL(products.vat) AS VAT, products.unit AS bUnit, inventories.volume AS Volume, products.image FROM products INNER JOIN inventories ON products.ID = inventories.product_id  WHERE products.barcode = '" + cmbProductDropDown.Text + "' OR products.sync_id = " + cmbProductDropDown.Text + " AND inventories.volume > 0 ";

            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();
            try { da.Fill(dt); } catch(Exception x) { MessageBox.Show(x.Message); }
            if (dt.Rows.Count > 0)
            {
                if (!selectedItemsId.Contains(int.Parse(dt.Rows[0]["ID"].ToString())))
                {
                    //// NOPE!
                    //dgvOrderDetails.Rows.Add(dt.Rows[0]["ID"], dt.Rows[0]["Qty"], dt.Rows[0]["Unit"], dt.Rows[0]["ItemName"], dt.Rows[0]["UnitPrice"], dt.Rows[0]["ItemTotal"]);

                    /*
                     * 
                     3:51 PM 28/10/2020	- Aziz Sir
                     =============================
                        1. Order details panel should be with the sequence as follows:
	                        Item Name	UnitPrice	Qty	Discount	Item
                     * 
                     */

                    //dgvOrderDetails.Rows.Add((dgvOrderDetails.Rows.Count + 1), dt.Rows[0]["ID"], dt.Rows[0]["ItemName"], dt.Rows[0]["UnitPrice"], dt.Rows[0]["Qty"], "(" + ti.ToTitleCase(dt.Rows[0]["Unit"].ToString()) + ")", double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), dt.Rows[0]["bUnit"], dt.Rows[0]["Volume"], dt.Rows[0]["image"]);
                    dgvOrderDetails.Rows.Add((dgvOrderDetails.Rows.Count + 1), dt.Rows[0]["ID"], dt.Rows[0]["ItemName"], dt.Rows[0]["UnitPrice"], dt.Rows[0]["Qty"], "(" + dt.Rows[0]["Unit"].ToString() + ")", double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), dt.Rows[0]["bUnit"], dt.Rows[0]["Volume"], dt.Rows[0]["image"]);

                    Array.Resize(ref selectedItemsId, selectedItemsId.Length + 1);
                    selectedItemsId[selectedItemsId.Length - 1] = int.Parse(dt.Rows[0]["ID"].ToString());

                    Array.Resize(ref oUnits, selectedItemsId.Length + 1);   // item count wise unit count even duplicate unit
                    oUnits[selectedItemsId.Length - 1] = dt.Rows[0]["Unit"].ToString();

                    UpdateOrderSummary(0);

                    if (dgvInvoiceHead.Rows[0].Cells[1].Value.ToString() == "")
                    {
                        string invoiceNo_str = InitializeOrderInvoice();
                        string order_invoice_no = "S/" + DateTime.Now.ToString("dd/MM/yy") + "/" + invoiceNo_str;

                        dgvInvoiceHead.Rows[0].Cells[1].Value = order_invoice_no; //System.DateTime.Now.Date.ToString("dd-MM-yy-001");
                        dgvInvoiceHead.Rows[1].Cells[1].Value = dgvOrderSummary.Rows[0].Cells[3].Value;
                        dgvInvoiceHead.Rows[4].Cells[1].Value = System.DateTime.Now.ToLocalTime().ToString("h:mm tt");
                        dgvInvoiceHead.Rows[5].Cells[1].Value = "NOT PAID";
                        dgvInvoiceHead.Rows[6].Cells[1].Value = warehouse_name;
                        dgvInvoiceHead.Rows[7].Cells[1].Value = warehouse_location;
                        if (isCustomer && Int32.Parse(customerId) > 0)
                            dgvInvoiceHead.Rows[8].Cells[1].Value = customerMobile;
                        else
                            dgvInvoiceHead.Rows[8].Cells[1].Value = "-";
                        dgvInvoiceHead.Rows[9].Cells[1].Value = username;   //  "Md. Moinul Hossain (Manager)";
                    }

                    ////////////////////////// CREATE ORDER IN LOCAL DB ////////////////////////////////////////
                    ///
                    if (!isOrder)
                        CreateOrder(dgvInvoiceHead.Rows[0].Cells[1].Value.ToString());

                    if (isOrder && orderedItem.Count == 0)
                    {
                        orderedItem.Add("order_id", orderId.ToString());
                        orderedItem.Add("product_id", dt.Rows[0]["ID"].ToString());
                        orderedItem.Add("qty", IsNumeric(dt.Rows[0]["Qty"].ToString()) ? dt.Rows[0]["Qty"].ToString() : double.Parse(dt.Rows[0]["Qty"].ToString()).ToString("0.0"));
                        orderedItem.Add("discount_total", double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"));
                        orderedItem.Add("vat_total", double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"));
                        orderedItem.Add("item_total", double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00"));
                    }
                    else
                    {
                        if (isOrder)
                        {
                            orderedItem["product_id"] = dt.Rows[0]["ID"].ToString();
                            orderedItem["qty"] = IsNumeric(dt.Rows[0]["Qty"].ToString()) ? dt.Rows[0]["Qty"].ToString() : double.Parse(dt.Rows[0]["Qty"].ToString()).ToString("0.0");
                            orderedItem["discount_total"] = double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00");
                            orderedItem["vat_total"] = double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00");
                            orderedItem["item_total"] = double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00");
                        }
                    }

                    AddItem(orderedItem);
                    /////////////////////////////////////////////////////////////////////////////////////////////


                    // instead of clear selection
                    // dgvOrderDetails.ClearSelection();
                    // give the edit option by default and  immediately after adding the item

                    if (dgvOrderDetails.Rows.Count > 0)
                        dgvOrderDetails.Rows[dgvOrderDetails.Rows.Count - 1].Selected = true;

                    // show alert for product wise VAT
                    if (double.Parse(dt.Rows[0]["vat"].ToString()) > 0.00) MessageBox.Show("VAT for " + dt.Rows[0]["ItemName"].ToString() + ": " + double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), "VAT inclusive with the product!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    cmbProductDropDown.SelectAll();

                    DataGridViewCell cell = dgvOrderDetails.SelectedRows[0].Cells[4];
                    dgvOrderDetails.CurrentCell = cell;
                    dgvOrderDetails.BeginEdit(true);
                }
                else
                {
                    for (int i = 0; i < dgvOrderDetails.Rows.Count; i++)
                    {
                        if (int.Parse(dgvOrderDetails.Rows[i].Cells[1].Value.ToString()) == int.Parse(dt.Rows[0]["ID"].ToString()))
                        {
                            dgvOrderDetails.Rows[i].Selected = true;
                            DataGridViewCell cell = dgvOrderDetails.Rows[i].Cells[4];
                            dgvOrderDetails.CurrentCell = cell;
                            dgvOrderDetails.BeginEdit(true);
                        }
                    }

                    cmbProductDropDown.SelectAll();
                }


            }
            
            
            Conn.Close();            
        }


        private void UpdateTotal()
        {
            double subTotal = 0.00, discount = 0.00, vat = 0.00, grandTotal = 0.00;

            for (int i = 0; i < dgvOrderDetails.Rows.Count; i++)
            {
                subTotal += double.Parse(dgvOrderDetails.Rows[i].Cells[7].Value.ToString());
                discount += double.Parse(dgvOrderDetails.Rows[i].Cells[6].Value.ToString());
                vat += double.Parse(dgvOrderDetails.Rows[i].Cells[10].Value.ToString());
            }

            grandTotal = subTotal + vat - discount;

            dgvOrderSummary.Rows[0].Cells[1].Value = vat.ToString("0.00");
            dgvOrderSummary.Rows[0].Cells[3].Value = subTotal;
            dgvOrderSummary.Rows[1].Cells[3].Value = discount; //0.00;
            dgvOrderSummary.Rows[2].Cells[3].Value = grandTotal;
            dgvOrderSummary.Rows[3].Cells[3].Value = 0.00;
            dgvOrderSummary.Rows[4].Cells[3].Value = 0.00;

            //Update invoice pane
            dgvInvoiceHead.Rows[1].Cells[1].Value = subTotal;   //grandTotal;
            dgvInvoiceHead.Rows[2].Cells[1].Value = discount;

            // Need Due calculation!
            //dgvOrderSummary.Rows[5].Cells[3].Value = 0.00;
        }



        private void dgvOrderDetails_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Unit of measurement edited?                   
            //dgvOrderDetails.Rows[e.RowIndex].Cells[5].Value = "(" + ti.ToTitleCase(dgvOrderDetails.Rows[e.RowIndex].Cells[11].Value.ToString()) + ")";
            dgvOrderDetails.Rows[e.RowIndex].Cells[5].Value = "(" + dgvOrderDetails.Rows[e.RowIndex].Cells[11].Value.ToString() + ")";
            dgvOrderDetails.Columns[5].ReadOnly = true;

            // if not sufficient stock
            double stock = double.Parse(dgvOrderDetails.Rows[e.RowIndex].Cells[12].Value.ToString());
            double oQty = double.Parse(dgvOrderDetails.Rows[e.RowIndex].Cells[4].Value.ToString());
            if (oQty <= stock)
            {
                // if not in correct format
                if (!IsFloat(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()))
                {
                    dgvOrderDetails.SelectedRows[0].Cells[4].Value = 1;
                    return;
                }
                else
                    if (IsNumeric(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()))
                    dgvOrderDetails.SelectedRows[0].Cells[4].Value = double.Parse(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()).ToString();
                else
                    dgvOrderDetails.SelectedRows[0].Cells[4].Value = double.Parse(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()).ToString("0.0");
            }
            else
            {
                MessageBox.Show("The quantity exceeded the stock!", "Not Sufficient Stock!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                dgvOrderDetails.SelectedRows[0].Cells[4].Value = 0;
                //return;
            }

            // Keep the quantity fixed number for now
            dgvOrderDetails.SelectedRows[0].Cells[4].Value = Convert.ToInt32 (double.Parse(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()));

            // Discount Total
            dgvOrderDetails.SelectedRows[0].Cells[6].Value = double.Parse(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()) * double.Parse(dgvOrderDetails.SelectedRows[0].Cells[9].Value.ToString());
            // VAT Total
            dgvOrderDetails.SelectedRows[0].Cells[10].Value = double.Parse(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()) * double.Parse(dgvOrderDetails.SelectedRows[0].Cells[8].Value.ToString());
            // Item Total
            dgvOrderDetails.SelectedRows[0].Cells[7].Value = double.Parse(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()) * double.Parse(dgvOrderDetails.SelectedRows[0].Cells[3].Value.ToString()); //- (double.Parse(dgvOrderDetails.SelectedRows[0].Cells[9].Value.ToString()) - double.Parse(dgvOrderDetails.SelectedRows[0].Cells[8].Value.ToString())));
            
            string strUnit = dgvOrderDetails.Rows[e.RowIndex].Cells[11].Value.ToString();

            if (oUnits != null && oUnits.Length > 0 && oUnits[e.RowIndex] != null && strUnit.ToLower() != oUnits[e.RowIndex].ToLower())
            {
                dgvOrderDetails.Rows[e.RowIndex].Cells[7].Value = 0.00;
                txtCalcInput.Text = dgvOrderDetails.Rows[e.RowIndex].Cells[3].Value.ToString();
                if (!opsFlag.ContainsKey("ItemTotal"))
                    opsFlag.Add("ItemTotal", e.RowIndex);
            }
            else if (opsFlag.ContainsKey("ItemTotal"))
            {
                opsFlag.Remove("ItemTotal");
                txtCalcInput.Text = "";
                calcOperator = "";
                calcResult = 0;
                operand1 = 0;
                operand2 = 0;
            }


            // for updating in local db
            if (isOrder && orderedItem.Count > 0)
            {
                orderedItem["product_id"] = dgvOrderDetails.SelectedRows[0].Cells[1].Value.ToString();
                orderedItem["qty"] = IsNumeric(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()) ? dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString() : double.Parse(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()).ToString("0.0");
                orderedItem["discount_total"] = (double.Parse(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()) * double.Parse(dgvOrderDetails.SelectedRows[0].Cells[9].Value.ToString())).ToString("0.00");
                orderedItem["vat_total"] = (double.Parse(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()) * double.Parse(dgvOrderDetails.SelectedRows[0].Cells[8].Value.ToString())).ToString("0.00");
                orderedItem["item_total"] = (double.Parse(dgvOrderDetails.SelectedRows[0].Cells[4].Value.ToString()) * double.Parse(dgvOrderDetails.SelectedRows[0].Cells[3].Value.ToString())).ToString() ; //- double.Parse(dgvOrderDetails.SelectedRows[0].Cells[6].Value.ToString()))).ToString("0.00");
            }

            UpdateOrderSummary(0);
            dgvInvoiceHead.Rows[1].Cells[1].Value = double.Parse(dgvOrderSummary.Rows[0].Cells[3].Value.ToString());

            /// update ordered item in database
            UpdateItem(orderedItem);

            cmbProductDropDown.Focus();
        }

        private void cmbProductDropDown_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.D)
            {
                dgvOrderSummary.Rows[1].Selected = true;
                txtCalcInput.Text = dgvOrderSummary.Rows[1].Cells[3].Value.ToString();

                lPane.Enabled = false;
                rPane.Enabled = false;
                gbCouponDiscount.Visible = true;

                gbCouponDiscount.Left = this.Width / 2 - gbCouponDiscount.Width / 2;
                gbCouponDiscount.Top = this.Height / 2 - gbCouponDiscount.Height / 2;

                gbCouponDiscount.Text = "ENTER DISCOUNT AMOUNT";

                // exit - close session disabled
                btnCloseOrderScr.Enabled = false;
                btnCloseSession.Enabled = false;

                ///////////////////////////////////////////////
                txtCouponDiscount.RightToLeft = RightToLeft.No;    // DECIDE/ RESET WITH THIS // NO GLOBAL VARIABLE
                txtInvoiceNo.Visible = false;
                btnAddDiscountOnGT.Visible = txtCouponDiscount.Visible = true; txtCouponDiscount.BringToFront();
                rdoAmount.Visible = rdoPercent.Visible = true;
                txtCouponDiscount.Top = rdoAmount.Top + rdoAmount.Height + 20;
                btnApplyCoupon.Top = txtCouponDiscount.Top + txtCouponDiscount.Height + 20;
                ///////////////////////////////////////////////

                rdoAmount.Checked = true;
                txtCouponDiscount.Text = dgvOrderSummary.Rows[1].Cells[3].Value.ToString();
                txtCouponDiscount.SelectAll();
                txtCouponDiscount.Focus();
            }

            else if (e.Control && e.KeyCode == Keys.P)
            {
                dgvOrderSummary.Rows[3].Selected = true;
                txtCalcInput.Text = dgvOrderSummary.Rows[3].Cells[3].Value.ToString();
                txtCalcInput.Focus();
            }

            else if (e.KeyCode == Keys.Delete)
            {
                RemoveItem();
            }

            else if (e.Control && e.KeyCode == Keys.C)
            {
                lPane.Enabled = false;
                rPane.Enabled = false;
                CustomerPane.Visible = true;

                CustomerPane.Left = this.Width / 2 - CustomerPane.Width / 2;
                CustomerPane.Top = this.Height / 2 - CustomerPane.Height / 2 - 30;

                CustomerPane.Text = "Customer";

                // exit - close session disabled
                btnCloseOrderScr.Enabled = false;
                btnCloseSession.Enabled = false;

                // Customer
                btnSaveCustomer.BackColor = Color.AliceBlue;
                btnSaveCustomer.ForeColor = SystemColors.ControlDark;
                
                if((isCustomer && dgvInvoiceHead.Rows[8].Cells[1].Value.ToString() != "-") && (isCustomer && dgvInvoiceHead.Rows[8].Cells[1].Value.ToString() != ""))
                {
                    txtCustomerName.Text = customerName;
                    txtCustomerEmail.Text = customerEmail;
                    txtCustomerMobile.Text = customerMobile;

                    btnSaveCustomer.Text = "Change";
                    btnSaveCustomer.BackColor = Color.RoyalBlue;
                    btnSaveCustomer.ForeColor = SystemColors.HighlightText;
                    btnSaveCustomer.Enabled = true;
                }
                else
                {
                    txtCustomerName.Text = "";
                    txtCustomerEmail.Text = "";
                    txtCustomerMobile.Text = "";
                }

                txtCustomerMobile.Focus();
            }

            else if (e.Control && e.KeyCode == Keys.R)
            {
                if (dgvOrderDetails.Rows.Count > 0 && dgvOrderDetails.SelectedRows.Count > 0 && selectedItemsId.Contains(int.Parse(dgvOrderDetails.SelectedRows[0].Cells[1].Value.ToString())))
                {
                    List<int> list = selectedItemsId.ToList();
                    list.Remove(int.Parse(dgvOrderDetails.SelectedRows[0].Cells[1].Value.ToString()));
                    selectedItemsId = list.ToArray();
                    dgvOrderDetails.Rows.Remove(dgvOrderDetails.SelectedRows[0]);
                    UpdateOrderSummary(0);
                }

                for (int i = 0; i < dgvOrderDetails.Rows.Count; i++)
                {
                    dgvOrderDetails.Rows[i].Cells[0].Value = (i + 1);
                }

                cmbProductDropDown.SelectAll(); cmbProductDropDown.Focus();
            }
        }

        private void txtAccessPIN_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                accessVerified = VerifyAccess(txtAccessPIN.Text);
                
                if (!accessVerified)
                {
                    MessageBox.Show("Access Failed!\n Please insert correct PIN or contact the ecommerce admin.", "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtAccessPIN.Text = "";
                    return;
                }

                if (!terminal.ContainsKey("API_key")) terminal.Add("API_key", __API_KEY);

                try
                {
                    if (Conn != null && Conn.State == ConnectionState.Closed)
                        Conn.Open();
                    else
                    {
                        Conn = new OleDbConnection(ConnectionString);
                        Conn.Open();
                    }

                        OleDbCommand cmd = new OleDbCommand();
                        cmd.Connection = Conn;


                        cmd.CommandText = "SELECT * FROM users WHERE PIN = " + txtAccessPIN.Text;

                        OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                    username = dt.Rows[0]["username"].ToString();

                    if (dt.Rows.Count > 0)
                    {
                        if (terminal.ContainsKey("terminal_id")) terminal["terminal_id"] = dt.Rows[0]["terminal_server_id"].ToString(); else terminal.Add("terminal_id", dt.Rows[0]["terminal_server_id"].ToString());
                        if (terminal.ContainsKey("pos_user_id")) terminal["pos_user_id"] = dt.Rows[0]["sync_id"].ToString(); else terminal.Add("pos_user_id", dt.Rows[0]["sync_id"].ToString());
                        if (terminal.ContainsKey("pos_user")) terminal["pos_user"] = username; else terminal.Add("pos_user", username);
                        if (dgvInvoiceHead.Rows.Count > 8)
                            dgvInvoiceHead.Rows[9].Cells[1].Value = username;
                    }
                    
                }
                catch (Exception x)
                {
                    MessageBox.Show("There is a problem to verify the user. Please contact the vendor.\nDetails\n" + x.Message, "Authentication Error!");
                    return;
                }

                mPane.Visible = iPane.Visible = true ;

                btnCloseOrderScr.Left -=  21;
                btnCloseSession.Left = btnCloseOrderScr.Left + btnCloseOrderScr.Width + 20;

                txtAccessPIN.Text = "";
                gbAccess.Visible = false;
                btnCloseSession.Visible = true;

                menubarTop.Enabled = true;

                lPane.Visible = true;
                rPane.Visible = true;
                cmbProductDropDown.SelectAll(); cmbProductDropDown.Focus();
            }
        }

        private void txtCalcInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13 && double.Parse(txtCalcInput.Text) >= 0.00 && dgvInvoiceHead.Rows[0].Cells[1].Value.ToString() != "" && double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()) > 0.00)
            {
                // return if the customer not registered but want to keep due (not allowed)

                string customerMobile = dgvInvoiceHead.Rows[8].Cells[1].Value.ToString();
                double due = double.Parse(dgvOrderSummary.Rows[5].Cells[3].Value.ToString());

                if (!isCustomer && customerMobile == "-" && due > 0.00 || !isCustomer && customerMobile == "" && due > 0)
                {
                    var msg = MessageBox.Show("For saving any order with due amount the customer should be registered.\n\nDo you want to register the customer?", "Customer not registered!", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    if (msg == DialogResult.Yes)
                        ShowCustomerPane();
                    return;
                }
                /// end of return for due of unregistered customer ///

                lPane.Enabled = false;
                rPane.Enabled = false;
                gbPrintConfirmation.Visible = true;

                gbPrintConfirmation.Left = this.Width / 2 - gbPrintConfirmation.Width / 2;
                gbPrintConfirmation.Top = this.Height / 2 - gbPrintConfirmation.Height / 2;
                

                // exit - close session disabled
                btnCloseOrderScr.Enabled = false;
                btnCloseSession.Enabled = false;

                OrderStateChecked();
                 
            }
            
        }

        private bool OrderStateChecked()
        {
            bool state = false;

            switch(oFlag)
            {
                case 0:

                    payModePane.Visible = true;
                    pConfirmPane.Visible = false;

                    rdoCash.Focus();
                    break;

                case 1:
                    payModePane.Visible = false;
                    pConfirmPane.Visible = true;

                    btnSaveOrder.Focus();

                    break;
            }

            state = true;
            return state;
        }

        private void btnSaveOrder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                CancelSavingOrder();
        }

        private void btnSaveOrder_Click(object sender, EventArgs e)
        {            
            SaveOrder();
        }

        private void SaveOrder()
        {
            string msg = "";
            MessageBoxIcon icon = MessageBoxIcon.Information;
            
            try
            {
                List<Dictionary<string, string>> products = new List<Dictionary<string, string>>();
                int[] productIds = new int[dgvOrderDetails.Rows.Count];
                DataGridViewRow row = new DataGridViewRow();


                for (int i = 0; i < productIds.Length; i++)
                {
                    Dictionary<string, string> product = new Dictionary<string, string>();

                    row = dgvOrderDetails.Rows[i];

                    string product_id = row.Cells[1].Value.ToString();
                    product.Add("product_id", product_id);

                    string last_order_id = orderId.ToString();        // Order ID on process
                    product.Add("last_process_id", last_order_id);

                    // to increase volume in the database
                    string volume = row.Cells[4].Value.ToString();
                    product.Add("volume", volume);

                    // to increase volume in the database
                    string total_price = "(" + volume + " * products.unit_purchase_price)";     // products table will be joined in the Stock.UpdateReducedStock()
                    product.Add("total_price", total_price);

                    product.Add("is_purchase", "false");

                    products.Add(product);
                }


                /// UPDATE INVENTORY                                
                msg += Stock.UpdateReducedStock(products, productIds);

                if (msg.Contains("Error"))
                {
                    icon = MessageBoxIcon.Error;
                    MessageBox.Show(msg, "Inventory Status!", MessageBoxButtons.OK, icon);
                }
                else
                {
                    if (order.ContainsKey("terminal_id")) order["terminal_id"] = terminal["terminal_id"]; else order.Add("terminal_id", terminal["terminal_id"]);
                    if (order.ContainsKey("API_key")) order["API_key"] = terminal["API_key"]; else order.Add("API_key", terminal["API_key"]);
                    
                    string invoiceno = dgvInvoiceHead.Rows[0].Cells[1].Value.ToString();
                    if (order.ContainsKey("invoice_no")) order["invoice_no"] = invoiceno; else order.Add("invoice_no", invoiceno);                    
                    
                    if (order.ContainsKey("pos_user_id")) order["pos_user_id"] = terminal["pos_user_id"]; else order.Add("pos_user_id", terminal["pos_user_id"]);

                    Order.SyncOrder(order);
                }

                dgvInvoiceHead.Rows.Clear();
                dgvOrderDetails.Rows.Clear();
                dgvOrderSummary.Rows.Clear();

                LoadUI();

                ErrorMessage = "";
                Query = "";
                Conn = null;

                // selected items array
                selectedItemsId = new int[0];

                // Customer
                isCustomer = false;
                customerId = "0";
                iCustomerId = "0";
                customerName = "";
                customerEmail = "";
                customerMobile = "";

                oUnits = new string[0];

                // calc operators
                calcOperator = "";

                // operands
                operand1=operand2=calcResult = 0;

                // ops flag
                opsFlag = new Dictionary<string, int>();


                oFlag = 0;

                // Payment Mode
                payMode = "";

                isOrder = false;
                order.Clear();
                orderedItem.Clear();
                CancelSavingOrder();
                txtCalcInput.Text = "0.00";
                cmbProductDropDown.Text = "";
                cmbProductDropDown.Focus();
            }
            catch (Exception x)
            {
                icon = MessageBoxIcon.Error;
                msg += "\nError: " + x.Message;
                MessageBox.Show(msg, "Inventory Status!", MessageBoxButtons.OK, icon);
            }
        }

        private void ResetOrderScreen()
        {
            

            dgvInvoiceHead.Rows.Clear();
            dgvOrderDetails.Rows.Clear();
            dgvOrderSummary.Rows.Clear();

            LoadUI();

            ErrorMessage = "";
            Query = "";
            Conn = null;

            // selected items array
            selectedItemsId = new int[0];

            // Customer
            isCustomer = false;
            customerId = "0";
            iCustomerId = "0";
            customerName = "";
            customerEmail = "";
            customerMobile = "";

            oUnits = new string[0];

            // calc operators
            calcOperator = "";

            // operands
            operand1 = operand2 = calcResult = 0;

            // ops flag
            opsFlag = new Dictionary<string, int>();


            oFlag = 0;

            // Payment Mode
            payMode = "";

            isOrder = false;
            order.Clear();
            orderedItem.Clear();
            CancelSavingOrder();
            txtCalcInput.Text = "0.00";
            cmbProductDropDown.Text = "";
            cmbProductDropDown.Focus();
        }

        private void txtCalcInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.D)
            {

                dgvOrderSummary.Rows[1].Selected = true;
                txtCalcInput.Text = dgvOrderSummary.Rows[1].Cells[3].Value.ToString();

                lPane.Enabled = false;
                rPane.Enabled = false;
                gbCouponDiscount.Visible = true;

                gbCouponDiscount.Left = this.Width / 2 - gbCouponDiscount.Width / 2;
                gbCouponDiscount.Top = this.Height / 2 - gbCouponDiscount.Height / 2;

                gbCouponDiscount.Text = "ENTER DISCOUNT AMOUNT";

                // exit - close session disabled
                btnCloseOrderScr.Enabled = false;
                btnCloseSession.Enabled = false;

                ///////////////////////////////////////////////
                txtCouponDiscount.RightToLeft = RightToLeft.No;    // DECIDE/ RESET WITH THIS // NO GLOBAL VARIABLE
                txtInvoiceNo.Visible = false;
                btnAddDiscountOnGT.Visible = txtCouponDiscount.Visible = true; txtCouponDiscount.BringToFront();
                rdoAmount.Visible = rdoPercent.Visible = true;
                txtCouponDiscount.Top = rdoAmount.Top + rdoAmount.Height + 20;
                btnApplyCoupon.Top = txtCouponDiscount.Top + txtCouponDiscount.Height + 20;
                ///////////////////////////////////////////////

                rdoAmount.Checked = true;
                txtCouponDiscount.Text = dgvOrderSummary.Rows[1].Cells[3].Value.ToString();
                txtCouponDiscount.SelectAll();
                txtCouponDiscount.Focus();

                /*
                dgvOrderSummary.Rows[1].Selected = true;
                txtCalcInput.Text = dgvOrderSummary.Rows[1].Cells[3].Value.ToString();

                lPane.Enabled = false;
                rPane.Enabled = false;
                gbCouponDiscount.Visible = true;

                gbCouponDiscount.Left = this.Width / 2 - gbCouponDiscount.Width / 2;
                gbCouponDiscount.Top = this.Height / 2 - gbCouponDiscount.Height / 2;

                gbCouponDiscount.Text = "ENTER DISCOUNT AMOUNT";

                // exit - close session disabled
                btnCloseOrderScr.Enabled = false;
                btnCloseSession.Enabled = false;

                txtCouponDiscount.Text = dgvOrderSummary.Rows[1].Cells[3].Value.ToString();
                txtCouponDiscount.SelectAll();
                txtCouponDiscount.Focus();
                */
            }
            else if (e.Control && e.KeyCode == Keys.O)
            {
                cmbProductDropDown.SelectAll();
                cmbProductDropDown.Focus();
            }
            else if(e.Control && e.KeyCode == Keys.C)
            {
                lPane.Enabled = false;
                rPane.Enabled = false;
                CustomerPane.Visible = true;

                CustomerPane.Left = this.Width / 2 - CustomerPane.Width / 2;
                CustomerPane.Top = this.Height / 2 - CustomerPane.Height / 2 - 30;

                CustomerPane.Text = "Customer";

                // exit - close session disabled
                btnCloseOrderScr.Enabled = false;
                btnCloseSession.Enabled = false;

                // Customer
                btnSaveCustomer.BackColor = Color.AliceBlue;
                btnSaveCustomer.ForeColor = SystemColors.ControlDark;

                if (isCustomer && dgvInvoiceHead.Rows[8].Cells[1].Value.ToString() != "-" || isCustomer && dgvInvoiceHead.Rows[8].Cells[1].Value.ToString() != "")
                {
                    txtCustomerName.Text = customerName;
                    txtCustomerEmail.Text = customerEmail;
                    txtCustomerMobile.Text = customerMobile;

                    btnSaveCustomer.Text = "Change";
                    btnSaveCustomer.BackColor = Color.RoyalBlue;
                    btnSaveCustomer.ForeColor = SystemColors.HighlightText;
                    btnSaveCustomer.Enabled = true;
                }
                else
                {
                    txtCustomerName.Text = "";
                    txtCustomerEmail.Text = "";
                    txtCustomerMobile.Text = "";

                    btnSaveCustomer.Enabled = false;
                    btnSaveCustomer.BackColor = Color.AliceBlue;
                    btnSaveCustomer.ForeColor = SystemColors.ControlDark;
                    btnSaveCustomer.Text = "Find";
                }

                txtCustomerMobile.Focus();
            }
        }

        private void btnSavePrintOrder_Click(object sender, EventArgs e)
        {
            btnPrintOrder_Click(sender, e);

            gbPrintConfirmation.Visible = false;
            lPane.Enabled = true;
            rPane.Enabled = true;

            // exit - close session enabled
            btnCloseOrderScr.Enabled = true;
            btnCloseSession.Enabled = true;

            cmbProductDropDown.SelectAll();
            cmbProductDropDown.Focus();
        }

        private void btnSavePrintOrder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                CancelSavingOrder();
        }

        private void btnRemoveItem_Click(object sender, EventArgs e)
        {
            RemoveItem();                       
        }

        private void cmbProductDropDown_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (cmbProductDropDown.Text == "")
                    return;

                if (dgvOrderDetails.Rows.Count > 0)
                {
                    // check if any item with 0 quantity
                    foreach (DataGridViewRow row in dgvOrderDetails.Rows)
                        if (double.Parse(row.Cells[4].Value.ToString()) == 0)
                        {
                            MessageBox.Show("Please enter the quantity of the item no. " + (row.Index + 1) + "!", "Quantity can't be Zero!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            dgvOrderDetails.CurrentCell = row.Cells[4];
                            dgvOrderDetails.BeginEdit(true);
                            return;
                        }
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

                //Query = "SELECT ID AS ID, 1 AS Qty, unit AS Unit, product_name AS ItemName, unit_price AS UnitPrice, (1 * unit_price - discount + vat) AS ItemTotal, discount AS Discount, vat FROM products WHERE shortcode = '" + cmbProductDropDown.Text + "' OR barcode = '" + cmbProductDropDown.Text + "' OR product_name LIKE '%" + cmbProductDropDown.Text + "%'";
                //Query = "SELECT products.ID, 1 AS Qty, products.unit AS Unit, products.product_name AS ItemName, products.unit_price AS UnitPrice, (1 * VAL(products.unit_price) - VAL(products.discount) + VAL(products.vat)) AS ItemTotal, products.discount AS Discount, products.vat, products.unit AS bUnit, inventories.volume AS Volume FROM products INNER JOIN inventories ON inventories.product_id = products.ID WHERE VAL(barcode) = " + cmbProductDropDown.Text + " AND inventories.volume > 0 ";
                Query = "SELECT products.ID, 1 AS Qty, products.unit AS Unit, products.product_name AS ItemName, VAL(products.unit_price) AS UnitPrice, (1 * VAL(products.unit_price)) AS ItemTotal, VAL(products.discount) AS Discount, VAL(products.vat) AS VAT, products.unit AS bUnit, inventories.volume AS Volume, products.image FROM products INNER JOIN inventories ON products.ID = inventories.product_id  WHERE VAL(products.barcode) = " + cmbProductDropDown.Text + " AND inventories.volume > 0 ";
                cmd.CommandText = Query;

                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                DataTable dt = new DataTable();
                try { da.Fill(dt); }catch(Exception x) { MessageBox.Show(x.Message); }
                if (dt.Rows.Count > 0)
                {
                    //dgvOrderDetails.Focus();
                    
                    if (!selectedItemsId.Contains(int.Parse(dt.Rows[0]["ID"].ToString())))
                    {
                        //dgvOrderDetails.Rows.Add((dgvOrderDetails.Rows.Count + 1), dt.Rows[0]["ID"], dt.Rows[0]["ItemName"], dt.Rows[0]["UnitPrice"], dt.Rows[0]["Qty"], "(" + ti.ToTitleCase(dt.Rows[0]["Unit"].ToString()) + ")", double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), dt.Rows[0]["bUnit"], dt.Rows[0]["Volume"], dt.Rows[0]["image"]);
                        dgvOrderDetails.Rows.Add((dgvOrderDetails.Rows.Count + 1), dt.Rows[0]["ID"], dt.Rows[0]["ItemName"], dt.Rows[0]["UnitPrice"], dt.Rows[0]["Qty"], "(" + dt.Rows[0]["Unit"].ToString() + ")", double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), dt.Rows[0]["bUnit"], dt.Rows[0]["Volume"], dt.Rows[0]["image"]);

                        Array.Resize(ref selectedItemsId, selectedItemsId.Length + 1);
                        selectedItemsId[selectedItemsId.Length - 1] = int.Parse(dt.Rows[0]["ID"].ToString());

                        Array.Resize(ref oUnits, selectedItemsId.Length + 1);   // item count wise unit count even duplicate unit
                        oUnits[selectedItemsId.Length - 1] = dt.Rows[0]["Unit"].ToString();

                        UpdateOrderSummary(0);

                        ////////////////////////////////////////////////////////////////////
                        // prevent Null Reference exception on oUnits[e.RowIndex]
                        dgvOrderDetails.Rows[dgvOrderDetails.Rows.Count - 1].Selected = true;
                        ////////////////////////////////////////////////////////////////////

                        ////////////////////////// CREATE ORDER IN LOCAL DB ////////////////////////////////////////
                        ///
                        if (!isOrder)
                            CreateOrder(dgvInvoiceHead.Rows[0].Cells[1].Value.ToString());

                        if (isOrder && orderedItem.Count == 0)
                        {
                            orderedItem.Add("order_id", orderId.ToString());
                            orderedItem.Add("product_id", dt.Rows[0]["ID"].ToString());
                            orderedItem.Add("qty", IsNumeric(dt.Rows[0]["Qty"].ToString()) ? dt.Rows[0]["Qty"].ToString() : double.Parse(dt.Rows[0]["Qty"].ToString()).ToString("0.0"));
                            orderedItem.Add("discount_total", double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00"));
                            orderedItem.Add("vat_total", double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"));
                            orderedItem.Add("item_total", double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00"));
                        }
                        else
                        {
                            if (isOrder)
                            {
                                orderedItem["product_id"] = dt.Rows[0]["ID"].ToString();
                                orderedItem["qty"] = IsNumeric(dt.Rows[0]["Qty"].ToString()) ? dt.Rows[0]["Qty"].ToString() : double.Parse(dt.Rows[0]["Qty"].ToString()).ToString("0.0");
                                orderedItem["discount_total"] = double.Parse(dt.Rows[0]["Discount"].ToString()).ToString("0.00");
                                orderedItem["vat_total"] = double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00");
                                orderedItem["item_total"] = double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00");
                            }
                        }

                        AddItem(orderedItem);
                        /////////////////////////////////////////////////////////////////////////////////////////////


                        // instead of clear selection
                        // dgvOrderDetails.ClearSelection();
                        // give the edit option by default and  immediately after adding the item

                        if (dgvOrderDetails.Rows.Count > 0)
                            dgvOrderDetails.Rows[dgvOrderDetails.Rows.Count - 1].Selected = true;

                        // show alert for product wise VAT
                        if (double.Parse(dt.Rows[0]["vat"].ToString()) > 0.00) MessageBox.Show("VAT for " + dt.Rows[0]["ItemName"].ToString() + ": " + double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), "VAT inclusive with the product!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        cmbProductDropDown.SelectAll();

                        DataGridViewCell cell = dgvOrderDetails.SelectedRows[0].Cells[4];
                        dgvOrderDetails.CurrentCell = cell;
                        dgvOrderDetails.BeginEdit(true);


                        //empty calculation
                        txtCalcInput.Text = "0.00";

                        // show alert for product wise VAT
                        if (double.Parse(dt.Rows[0]["vat"].ToString()) > 0.00) MessageBox.Show("VAT for " + dt.Rows[0]["ItemName"].ToString() + ": " + double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), "VAT inclusive with the product!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    else
                    {
                        for (int i = 0; i < dgvOrderDetails.Rows.Count; i++)
                        {
                            if (int.Parse(dgvOrderDetails.Rows[i].Cells[1].Value.ToString()) == int.Parse(dt.Rows[0]["ID"].ToString()))
                            {
                                dgvOrderDetails.Rows[i].Selected = true;
                                DataGridViewCell cell = dgvOrderDetails.Rows[i].Cells[4];
                                dgvOrderDetails.CurrentCell = cell;
                                dgvOrderDetails.BeginEdit(true);
                            }
                        }

                        cmbProductDropDown.SelectAll();
                    }


                }
                else
                {
                    MessageBox.Show("Product out of stock!", "Not in Stock!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                Conn.Close();
            }
        }


        #region PREVIOUS COPY cmbProductDropDown_KeyPress()

        /*
         * 
         *
         *
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            
            private void cmbProductDropDown_KeyPress(object sender, KeyPressEventArgs e)
            {
                if (e.KeyChar == 13)
                {
                    if (cmbProductDropDown.Text == "")
                        return;

                    if (dgvOrderDetails.Rows.Count > 0)
                    {
                        // check if any item with 0 quantity
                        foreach (DataGridViewRow row in dgvOrderDetails.Rows)
                            if (double.Parse(row.Cells[4].Value.ToString()) == 0)
                            {
                                MessageBox.Show("Please enter the quantity of the item no. " + (row.Index + 1) + "!", "Quantity can't be Zero!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                dgvOrderDetails.CurrentCell = row.Cells[4];
                                dgvOrderDetails.BeginEdit(true);
                                return;
                            }
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

                    //Query = "SELECT ID AS ID, 1 AS Qty, unit AS Unit, product_name AS ItemName, unit_price AS UnitPrice, (1 * unit_price - discount + vat) AS ItemTotal, discount AS Discount, vat FROM products WHERE shortcode = '" + cmbProductDropDown.Text + "' OR barcode = '" + cmbProductDropDown.Text + "' OR product_name LIKE '%" + cmbProductDropDown.Text + "%'";
                    //Query = "SELECT products.ID, 1 AS Qty, products.unit AS Unit, products.product_name AS ItemName, products.unit_price AS UnitPrice, (1 * VAL(products.unit_price) - VAL(products.discount) + VAL(products.vat)) AS ItemTotal, products.discount AS Discount, products.vat, products.unit AS bUnit, inventories.volume AS Volume FROM products INNER JOIN inventories ON inventories.product_id = products.ID WHERE VAL(barcode) = " + cmbProductDropDown.Text + " AND inventories.volume > 0 ";
                    Query = "SELECT products.ID, 1 AS Qty, products.unit AS Unit, products.product_name AS ItemName, VAL(products.unit_price AS UnitPrice), (1 * VAL(products.unit_price)) AS ItemTotal, VAL(products.discount) AS Discount, VAL(products.vat), products.unit AS bUnit, inventories.volume AS Volume FROM products INNER JOIN inventories ON inventories.product_id = products.ID WHERE VAL(barcode) = " + cmbProductDropDown.Text + " AND inventories.volume > 0 ";
                    cmd.CommandText = Query;

                    OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    try { da.Fill(dt); }catch(Exception x) { MessageBox.Show(x.Message); }
                    if (dt.Rows.Count > 0)
                    {
                        dgvOrderDetails.Focus();
                        if (!selectedItemsId.Contains(int.Parse(dt.Rows[0]["ID"].ToString())))
                        {
                            dgvOrderDetails.Rows.Add((dgvOrderDetails.Rows.Count + 1), dt.Rows[0]["ID"], dt.Rows[0]["ItemName"], dt.Rows[0]["UnitPrice"], dt.Rows[0]["Qty"], "(" + ti.ToTitleCase(dt.Rows[0]["Unit"].ToString()) + ")", double.Parse(dt.Rows[0]["Qty"].ToString()) * double.Parse(dt.Rows[0]["Discount"].ToString()), double.Parse(dt.Rows[0]["ItemTotal"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), double.Parse(dt.Rows[0]["discount"].ToString()), double.Parse(dt.Rows[0]["vat"].ToString()), dt.Rows[0]["bUnit"], dt.Rows[0]["Volume"]);
                            Array.Resize(ref selectedItemsId, selectedItemsId.Length + 1);
                            selectedItemsId[selectedItemsId.Length - 1] = int.Parse(dt.Rows[0]["ID"].ToString());
                            UpdateOrderSummary(0);
                            cmbProductDropDown.SelectAll();
                            dgvOrderDetails.ClearSelection();


                            //empty calculation
                            txtCalcInput.Text = "0.00";

                            // show alert for product wise VAT
                            if (double.Parse(dt.Rows[0]["vat"].ToString()) > 0.00) MessageBox.Show("VAT for " + dt.Rows[0]["ItemName"].ToString() + ": " + double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00"), "VAT inclusive with the product!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            for (int i = 0; i < dgvOrderDetails.Rows.Count; i++)
                            {
                                if (int.Parse(dgvOrderDetails.Rows[i].Cells[1].Value.ToString()) == int.Parse(dt.Rows[0]["ID"].ToString()))
                                {
                                    dgvOrderDetails.Rows[i].Selected = true;
                                    DataGridViewCell cell = dgvOrderDetails.Rows[i].Cells[4];
                                    dgvOrderDetails.CurrentCell = cell;
                                    dgvOrderDetails.BeginEdit(true);
                                    break;
                                }
                            }

                            cmbProductDropDown.SelectAll();
                        }


                    }
                    else
                    {
                        MessageBox.Show("Product out of stock!", "Not in Stock!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    Conn.Close();
                }
            }
            
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

         */

        #endregion

        private void FindCustomer ()
        {
            string input = txtCustomerMobile.Text.Replace("\r\n", "");

            
                txtCustomerMobile.Text = txtCustomerMobile.Text.Replace("\r\n", "");                    // IMPORTANT! JUST THIS LINE PREVENTS TO GO NEXT WITHOUT MOBILE NO.

                if (txtCustomerMobile.Text == "")
                {
                    // Customer
                    btnSaveCustomer.BackColor = Color.AliceBlue;
                    btnSaveCustomer.ForeColor = SystemColors.ControlDark;
                    btnSaveCustomer.Text = "Find";
                    txtCustomerMobile.Text = "";
                    return;
                }

                if (btnSaveCustomer.Text == "Order" && txtCustomerMobile.Text.Length > 0)
                {
                    dgvInvoiceHead.Rows[8].Cells[1].Value = txtCustomerMobile.Text;
                    customerId = iCustomerId;
                    customerName = txtCustomerName.Text;
                    customerEmail = txtCustomerEmail.Text;
                    customerMobile = txtCustomerMobile.Text;
                    isCustomer = true;
                    CancelSavingCustomer();

                    if (!order.ContainsKey("customerId")) order.Add("customerId", customerId.ToString()); 
                    else order["customerId"] = customerId.ToString();

                if (order.ContainsKey("customerName")) order["customerName"] = customerName; else order.Add("customerName", customerName);
                if (order.ContainsKey("customerEmail")) order["customerEmail"] = customerEmail; else order.Add("customerEmail", customerEmail);
                if (order.ContainsKey("customerMobile")) order["customerMobile"] = customerMobile; else order.Add("customerMobile", customerMobile);

                if (isOrder) UpdateOrder(order);
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

                Query = "SELECT ID, customer_id, customer_name, customer_email, customer_mobile FROM customers WHERE customer_mobile = '" + txtCustomerMobile.Text.Trim() + "'";
                cmd.CommandText = Query;

                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                btnSaveCustomer.Enabled = true;

                // Customer
                btnSaveCustomer.BackColor = Color.RoyalBlue;
                btnSaveCustomer.ForeColor = SystemColors.HighlightText;

            if (dt.Rows.Count > 0)
            {
                iCustomerId = dt.Rows[0]["ID"].ToString();
                //////////////// ////////////////
                customerId = iCustomerId;
                customerName = txtCustomerName.Text;
                customerEmail = txtCustomerEmail.Text;
                customerMobile = txtCustomerMobile.Text;
                isCustomer = true;
                //////////////// ////////////////

                txtCustomerName.Text = dt.Rows[0]["customer_name"].ToString().TrimEnd();
                txtCustomerEmail.Text = dt.Rows[0]["customer_email"].ToString();
                btnSaveCustomer.Text = "Order";

                // Lock input customer. Instead, decide either take order or cancel
                txtCustomerName.ReadOnly = txtCustomerEmail.ReadOnly = txtCustomerMobile.ReadOnly = true;
                txtCustomerName.ForeColor = txtCustomerEmail.ForeColor = txtCustomerMobile.ForeColor = SystemColors.WindowFrame;
            }
            else
            {
                //bool customerVerified = VerifyCustomerOnServer();
                bool customerVerified = false;
                ApiModels.V3.CustomerResult result = VerifyCustomerOnServer();
                if (result != null)
                    customerVerified = result.success;
                
                /*
                txtCustomerName.Text = "";
                txtCustomerEmail.Text = "";
                */

                if(customerVerified)
                {
                    try
                    {
                        Conn = new OleDbConnection(ConnectionString);
                        Conn.Open();
                        cmd = new OleDbCommand();
                        cmd.Connection = Conn;
                        cmd.CommandText = "SELECT ID FROM customers WHERE user_id = " + result.data.user_id;
                        var IDvar = cmd.ExecuteScalar(); int ID = 0;
                        if (Int32.TryParse(IDvar.ToString(), out ID) && ID > 0) customerId = iCustomerId = ID.ToString();                        
                    }
                    catch (Exception x) 
                    {
                        MessageBox.Show("\nCustomer Verification Error:\n" + x.Message);
                    }

                    customerName = txtCustomerName.Text = result.data.username;
                    customerEmail = txtCustomerEmail.Text = result.data.email;
                    customerMobile = txtCustomerMobile.Text = result.data.phone_number;                    
                    
                }

                //isCustomer = false; Not required. Because, already there a customer can exist.
                btnSaveCustomer.Text = "Save";
            }

                dt.Clear();
                da.Dispose();
                Conn.Close();

                txtCustomerMobile.Text = input;                
                txtCustomerMobile.SelectAll();
            
        }

        private void txtCustomerMobile_KeyPress(object sender, KeyPressEventArgs e)
        {                        
            if(e.KeyChar == 13)
            {
                string input = txtCustomerMobile.Text.Replace("\r\n", "");

                txtCustomerMobile.Text = txtCustomerMobile.Text.Replace("\r\n", "");                    // IMPORTANT! JUST THIS LINE PREVENTS TO GO NEXT WITHOUT MOBILE NO.

                if (txtCustomerMobile.Text == "")
                {
                    // Customer
                    btnSaveCustomer.BackColor = Color.AliceBlue;
                    btnSaveCustomer.ForeColor = SystemColors.ControlDark;
                    btnSaveCustomer.Text = "Find";
                    txtCustomerMobile.Text = "";
                    return;
                }

                if(btnSaveCustomer.Text == "Change")
                {
                    btnSaveCustomer.Enabled = false;
                    btnSaveCustomer.BackColor = Color.AliceBlue;
                    btnSaveCustomer.ForeColor = SystemColors.ControlDark;
                    txtCustomerMobile.Text = txtCustomerMobile.Text.Replace("\r\n", "").Trim();
                    txtCustomerName.Text = txtCustomerEmail.Text = txtCustomerMobile.Text = "";
                    txtCustomerMobile.Focus();
                    return;
                }

                if (btnSaveCustomer.Text == "Save")
                {
                    SaveCustomer();
                }

                    if (btnSaveCustomer.Text == "Order" && txtCustomerMobile.Text.Length > 0)
                {
                    dgvInvoiceHead.Rows[8].Cells[1].Value = txtCustomerMobile.Text;
                    customerId = iCustomerId;
                    customerName = txtCustomerName.Text;
                    customerEmail = txtCustomerEmail.Text;
                    customerMobile = txtCustomerMobile.Text;
                    isCustomer = true;

                    if (!order.ContainsKey("customerId")) order.Add("customerId", customerId.ToString());
                    else order["customerId"] = customerId.ToString();

                    if (order.ContainsKey("customerName")) order["customerName"] = customerName; else order.Add("customerName", customerName);
                    if (order.ContainsKey("customerEmail")) order["customerEmail"] = customerEmail; else order.Add("customerEmail", customerEmail);
                    if (order.ContainsKey("customerMobile")) order["customerMobile"] = customerMobile; else order.Add("customerMobile", customerMobile);

                    if (isOrder) UpdateOrder(order);

                    CancelSavingCustomer();
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

                Query = "SELECT ID, customer_id, customer_name, customer_email, customer_mobile FROM customers WHERE customer_mobile = '" + txtCustomerMobile.Text.Trim() + "'";
                cmd.CommandText = Query;

                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                btnSaveCustomer.Enabled = true;

                // Customer
                btnSaveCustomer.BackColor = Color.RoyalBlue;
                btnSaveCustomer.ForeColor = SystemColors.HighlightText;

                if (dt.Rows.Count > 0)
                {
                    iCustomerId = dt.Rows[0]["ID"].ToString();
                    //////////////// ////////////////
                    customerId = iCustomerId;
                    customerName = txtCustomerName.Text;
                    customerEmail = txtCustomerEmail.Text;
                    customerMobile = txtCustomerMobile.Text;
                    isCustomer = true;
                    //////////////// ////////////////                    
                    txtCustomerName.Text = dt.Rows[0]["customer_name"].ToString().TrimEnd();
                    txtCustomerEmail.Text = dt.Rows[0]["customer_email"].ToString();
                    btnSaveCustomer.Text = "Order";

                    // Lock input customer. Instead, decide either take order or cancel
                    txtCustomerName.ReadOnly = txtCustomerEmail.ReadOnly = txtCustomerMobile.ReadOnly = true;
                    txtCustomerName.ForeColor = txtCustomerEmail.ForeColor = txtCustomerMobile.ForeColor = SystemColors.WindowFrame;
                }
                else
                {                    
                    bool customerVerified = false;
                    ApiModels.V3.CustomerResult result = VerifyCustomerOnServer();
                    if (result != null)
                        customerVerified = result.success;
                    

                    if (customerVerified)
                    {
                        try
                        {
                            Conn = new OleDbConnection(ConnectionString);
                            Conn.Open();
                            cmd = new OleDbCommand();
                            cmd.Connection = Conn;
                            cmd.CommandText = "SELECT ID FROM customers WHERE user_id = " + result.data.user_id;
                            var IDvar = cmd.ExecuteScalar(); int ID = 0;
                            if (Int32.TryParse(IDvar.ToString(), out ID) && ID > 0) customerId = iCustomerId = ID.ToString();
                        }
                        catch (Exception x)
                        {
                            MessageBox.Show("\nCustomer Verification Error:\n" + x.Message);
                        }

                        customerName = txtCustomerName.Text = result.data.username;
                        customerEmail = txtCustomerEmail.Text = result.data.email;
                        customerMobile = txtCustomerMobile.Text = result.data.phone_number;
                    }
                    
                    btnSaveCustomer.Text = "Save";
                }

                dt.Clear();
                da.Dispose();
                Conn.Close();

                txtCustomerMobile.Text = input;
                //txtCustomerName.Focus();
                e.Handled = true;
                txtCustomerMobile.SelectAll();
            }
            
        }

        private ApiModels.V3.CustomerResult VerifyCustomerOnServer()
        {
            // bool customerVerified = IntensePoS.Models.Customer.GetCustomer(txtCustomerMobile.Text);
            ApiModels.V3.CustomerResult result = IntensePoS.Models.Customer.GetCustomer(txtCustomerMobile.Text);
            return result;
        }

        private void txtCustomerMobile_TextChanged(object sender, EventArgs e)
        {
            txtCustomerMobile.Text = txtCustomerMobile.Text.Replace("\r\n", "");

            // Keep the IF block as it is
            if (isCustomer && txtCustomerMobile.Text == customerMobile) 
            {
                txtCustomerName.Text = customerName;
                txtCustomerEmail.Text = customerEmail;
                txtCustomerMobile.Text = customerMobile;
                btnSaveCustomer.Text = "Change";
                btnSaveCustomer.BackColor = Color.RoyalBlue;
                btnSaveCustomer.ForeColor = SystemColors.HighlightText;
                btnSaveCustomer.Enabled = true;
            }

            else if (txtCustomerMobile.Text == "")
            {
                // Customer
                btnSaveCustomer.BackColor = Color.AliceBlue;
                btnSaveCustomer.ForeColor = SystemColors.ControlDark;
                btnSaveCustomer.Enabled = false;
                btnSaveCustomer.Text = "Find";
                txtCustomerMobile.Text = "";
                return;
            }
            else if(txtCustomerMobile.Text.Replace("\r\n", "").Trim().Length > 0 || txtCustomerMobile.Text == "\r\n" || txtCustomerMobile.Text.Length > 0)
            {
                txtCustomerName.Text = "";
                txtCustomerName.ReadOnly = false;
                txtCustomerEmail.Text = "";
                txtCustomerEmail.ReadOnly = false;
                /// Check it
                btnSaveCustomer.Text = "Find";
                btnSaveCustomer.BackColor = Color.RoyalBlue;
                btnSaveCustomer.ForeColor = SystemColors.HighlightText;
                btnSaveCustomer.Enabled = true;
            }
            else if (txtCustomerMobile.Text.Replace("\r\n", "").Trim() == "" || txtCustomerMobile.Text.Replace("\r\n", "").Trim().Length == 0 || txtCustomerMobile.Text == "")
            {
                txtCustomerName.Text = "";
                txtCustomerName.ReadOnly = false;
                txtCustomerEmail.Text = "";
                txtCustomerEmail.ReadOnly = false;
                /// Check it
                btnSaveCustomer.Text = "Find";
                btnSaveCustomer.BackColor = Color.AliceBlue;
                btnSaveCustomer.ForeColor = SystemColors.ControlDark;
                btnSaveCustomer.Enabled = false;
            }
            else
            {
                txtCustomerName.Text = "";
                txtCustomerName.ReadOnly = false;
                txtCustomerEmail.Text = "";
                txtCustomerEmail.ReadOnly = false;
                /// Check it
                btnSaveCustomer.Text = "Find";
                btnSaveCustomer.BackColor = Color.AliceBlue;
                btnSaveCustomer.ForeColor = SystemColors.ControlDark;
                btnSaveCustomer.Enabled = false;
            }
            
        }

        private void exitMenu_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnCancelOrder_Click(object sender, EventArgs e)
        {
            if (isOrder && orderId > 0)
            {
                DialogResult confirm = MessageBox.Show("Do you want to cancel the order?", "Cancel Order", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

                if (confirm == DialogResult.OK)
                {
                    try
                    {
                        if (Conn!=null && Conn.State != ConnectionState.Open)
                            Conn.Open();
                        else
                        {
                            Conn = new OleDbConnection(ConnectionString);
                            Conn.Open();
                        }
                    }
                    catch (Exception x)
                    {
                        MessageBox.Show(x.Message, "Cancel Order", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    OleDbCommand cmd = new OleDbCommand();
                    cmd.Connection = Conn;

                    try
                    {
                        string status = dgvInvoiceHead.Rows[5].Cells[1].Value.ToString();

                        cmd.CommandText = "UPDATE orders SET prev_status = cur_status, cur_status = 'CANCELLED' WHERE ID = " + orderId;
                        cmd.ExecuteScalar();

                        if (status == "SAVED")
                        {
                            cmd.CommandText = "UPDATE ((inventories INNER JOIN ordered_items ON inventories.product_id = ordered_items.product_id) INNER JOIN products ON ordered_items.product_id =  products.ID)   SET inventories.volume = (inventories.volume + ordered_items.qty), inventories.total_price = products.unit_purchase_price * ordered_items.qty WHERE inventories.product_id IN (SELECT product_id FROM ordered_items WHERE order_id = " + orderId.ToString() + ")";
                            try
                            {
                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception x)
                            {
                                MessageBox.Show(x.Message);
                            }
                        }

                        try
                        {
                            Conn.Close();
                        }
                        catch (Exception x)
                        {
                            MessageBox.Show(x.Message, "Cancel Order", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        ResetOrderScreen();
                        MessageBox.Show("Order cancelled.", "Cancel Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception err)
                    {
                        err.Equals(null);
                        MessageBox.Show(err.Message, "Cancel Order", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                                        
                }
            }

            cmbProductDropDown.Focus();
        }

        private void btnPrintOrder_Click(object sender, EventArgs e)
        {
            if (!this.reprint && dgvInvoiceHead.Rows[5].Cells[1].Value.ToString().ToUpper() == "SAVED")
            {
                this.reprint = IsRePrint(sender, e);
                return;
            }

            Button btn = (Button)sender;

            if (btn.Text == "&Print")
            {
                // GetWebPrint();
                GetPrintA4();
                return;         // Comment later
            }

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

            //empty calculation
            txtCalcInput.Text = "";

            this.reprint = false;

            cmbProductDropDown.SelectAll();
            cmbProductDropDown.Focus();

        }


        private void GetPrintA4()
        {
            ////////////////////// PRINTING RECEIPT ///////////////////////
            ///

            PrintDocument doc = new PrintDocument();
            PrintPreviewDialog preview = new PrintPreviewDialog();  // Enable when preview on
            preview.Document = doc;                                 // Enable when preview on          
            doc.PrintPage += new PrintPageEventHandler(PrintReceiptA4);

            preview.Width = Screen.PrimaryScreen.WorkingArea.Width;
            preview.Height = Screen.PrimaryScreen.WorkingArea.Height;
            preview.PrintPreviewControl.Zoom = 1;

            preview.ShowDialog(); //Application.Exit();

            /// Print command
            //doc.Print();

            ///////////////////////////////////////////////////////////////
            ///
            //empty calculation
            txtCalcInput.Text = "";

            this.reprint = false;

            cmbProductDropDown.SelectAll();
            cmbProductDropDown.Focus();
        }

        /*
        private void GetWebPrint()
        {
            printPaneA4.Visible = true;
            printPaneA4.Height = this.Height * 80/100;
            printPaneA4.Width = this.Width * 80/100;
            printPaneA4.Top = this.Height/2 - printPaneA4.Height / 2;
            printPaneA4.Left = this.Width/2 - printPaneA4.Width / 2;
            ieBrowserA4Print.Url = new Uri(Directory.GetCurrentDirectory() + @"\PrintView\A4Print.html");
            return;
        }
        */

        #region RECEIPT CONFIG

        private void PrintReceipt(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            ////    STATIC RECEIPT TEMPLATE ///
            ////    only values are fetched from DB/ data grid view

            int baseY = 80;
            // Store name
            e.Graphics.DrawString("INTENSE PVT. LTD.", new Font("Arial", 10, FontStyle.Bold), Brushes.Black, (e.PageBounds.Width / 2 - 115), baseY);
            baseY += 20;

            // Address
            e.Graphics.DrawString("Gulfesha Plaza, Moghbazar, Dhaka", new Font("Arial", 8, FontStyle.Regular), Brushes.Black, (e.PageBounds.Width / 2 - 140), baseY);
            baseY += 15;

            // Contact no.
            e.Graphics.DrawString("Call: (+88) 01835 410 998, (+88) 01819 244 297", new Font("Arial", 8, FontStyle.Regular), Brushes.Black, (e.PageBounds.Width / 2 - 170), baseY);
            baseY += 25;

            // Invoice no.
            string invoiceno = dgvInvoiceHead.Rows[0].Cells[1].Value.ToString();
            e.Graphics.DrawString("Invoice No. " + invoiceno, new Font("Arial", 9, FontStyle.Bold), Brushes.Black, 10, baseY);
            baseY += 20;

            // Date and time
            string _sdate = dgvInvoiceHead.Rows[3].Cells[1].Value.ToString();
            string _time = dgvInvoiceHead.Rows[4].Cells[1].Value.ToString();
            e.Graphics.DrawString("Date: " + _sdate + " " + _time, new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 10, baseY);
            baseY += 25;

            // Line
            // Create pen.
            Pen blackPen = new Pen(Color.Black, 1);
            blackPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            // Create points that define line.
            Point point1 = new Point(10, 180);
            Point point2 = new Point(600, 180);
            e.Graphics.DrawLine(blackPen, point1, point2);

            //Order details (Column header)
            e.Graphics.DrawString("Sl.", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 10, baseY);
            e.Graphics.DrawString("Item Name", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 30, baseY);
            e.Graphics.DrawString("Unit Price", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 200, baseY);
            e.Graphics.DrawString("Qty", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 290, baseY);
            e.Graphics.DrawString("Discount", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 370, baseY);
            e.Graphics.DrawString("VAT", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 450, baseY);
            e.Graphics.DrawString("Item Total", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 510, baseY);
            baseY += 15 + 10;

            // order details (items)

            Rectangle rect;

            // Create a StringFormat object with each line of text, and the block
            // of text centered on the page.
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Far;
            stringFormat.LineAlignment = StringAlignment.Center;

            StringFormat sFmt2 = new StringFormat();
            sFmt2.Alignment = StringAlignment.Near;
            sFmt2.LineAlignment = StringAlignment.Near;

            for (int i = 0; i < dgvOrderDetails.RowCount; i++)
            {
                // Sl.
                e.Graphics.DrawString(dgvOrderDetails.Rows[i].Cells[0].Value.ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 10, baseY);
                // Item Name
                rect = new Rectangle(35, baseY - 3, 165, 75);
                //e.Graphics.DrawString(dgvOrderDetails.Rows[i].Cells[2].Value.ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 30, baseY);
                e.Graphics.DrawString(dgvOrderDetails.Rows[i].Cells[2].Value.ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, sFmt2);

                // Unit Price
                // Draw the text and the surrounding rectangle.
                rect = new Rectangle(200, baseY - 3, 70, 20);                
                e.Graphics.DrawString(double.Parse(dgvOrderDetails.Rows[i].Cells[3].Value.ToString()).ToString("N2"), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);

                e.Graphics.DrawString(dgvOrderDetails.Rows[i].Cells[4].Value.ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 290, baseY);

                // UNIT NAME
                string unit = dgvOrderDetails.Rows[i].Cells[5].Value.ToString();
                if (unit.Length > 8)
                    unit = dgvOrderDetails.Rows[i].Cells[5].Value.ToString().Substring(0, 8) + "..";
                e.Graphics.DrawString(unit, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 320, baseY);

                // Discount
                rect = new Rectangle(350, baseY - 3, 70, 20);
                string discount = double.Parse(dgvOrderDetails.Rows[i].Cells[6].Value.ToString()).ToString("0.00");
                //e.Graphics.DrawString(discount, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 370, baseY);
                e.Graphics.DrawString(discount, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);

                // VAT
                rect = new Rectangle(410, baseY - 3, 70, 20);
                string vat = double.Parse(dgvOrderDetails.Rows[i].Cells[10].Value.ToString()).ToString("0.00");
                //e.Graphics.DrawString(vat, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 450, baseY);
                e.Graphics.DrawString(vat, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);

                // Item Total
                rect = new Rectangle(480, baseY - 3, 70, 20);
                string itemtotal = double.Parse(dgvOrderDetails.Rows[i].Cells[7].Value.ToString()).ToString("0.00");
                //e.Graphics.DrawString(itemtotal, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 510, baseY);
                e.Graphics.DrawString(itemtotal, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);


                //////////// INPUT STRING WAS NOT IN CORRECT FORMAT ///////////////
                //rect = new Rectangle(230, baseY - 3, 70, 20);
                //e.Graphics.DrawString(double.Parse(dgvOrderDetails.Rows[i].Cells[5].Value.ToString()).ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);
                ///////////////////////////////////////////////////////////////////

                //e.Graphics.DrawString(dgvOrderDetails.Rows[i].Cells[4].Value.ToString().ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 250, baseY);

                /*
                 * 
                 *
                 *      ///// KEEP IT A SIDE FOR NOW. CONTINUE WITH PIXLE INSTEAD (ON THE ABOVE LINES) /////
                rect = new Rectangle(230, baseY - 3, 70, 20);
                e.Graphics.DrawString(double.Parse(dgvOrderDetails.Rows[i].Cells[4].Value.ToString()).ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);

                stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Far;

                rect = new Rectangle(220, baseY - 3, 100, 20);                
                e.Graphics.DrawString(dgvOrderDetails.Rows[i].Cells[5].Value.ToString().ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);

                stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Far;

                rect = new Rectangle(550, baseY - 3, 100, 20);
                e.Graphics.DrawString(double.Parse(dgvOrderDetails.Rows[i].Cells[6].Value.ToString()).ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);

                stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Far;
                stringFormat.LineAlignment = StringAlignment.Center;

                rect = new Rectangle(310, baseY - 3, 70, 20);
                e.Graphics.DrawString(double.Parse(dgvOrderDetails.Rows[i].Cells[8].Value.ToString()).ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);

                rect = new Rectangle(340, baseY - 3, 70, 20);
                e.Graphics.DrawString(double.Parse(dgvOrderDetails.Rows[i].Cells[7].Value.ToString()).ToString(), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);
                */

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

            for (int i = 0; i < dgvOrderSummary.RowCount; i++)
            {
                if(i==1)
                {
                    e.Graphics.DrawString(dgvOrderSummary.Rows[0].Cells[0].Value.ToString(), new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 410, baseY);

                    rect = new Rectangle(480, baseY - 3, 70, 20);
                    // Draw the text and the surrounding rectangle.
                    e.Graphics.DrawString(double.Parse(dgvOrderSummary.Rows[0].Cells[1].Value.ToString()).ToString("0.00"), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);
                    baseY += 15;
                }


                e.Graphics.DrawString(dgvOrderSummary.Rows[i].Cells[2].Value.ToString(), new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 410, baseY);

                rect = new Rectangle(480, baseY - 3, 70, 20);
                // Draw the text and the surrounding rectangle.
                e.Graphics.DrawString(double.Parse(dgvOrderSummary.Rows[i].Cells[3].Value.ToString()).ToString("0.00"), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, rect, stringFormat);
                //e.Graphics.DrawString(double.Parse(dgvOrderSummary.Rows[i].Cells[4].Value.ToString()).ToString("N2"), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, 250, baseY);
                baseY += 15;
            }

            baseY += 10;

            e.Graphics.DrawString("Thanks for shopping with INTENSE PVT. LTD.", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, 10, baseY);
        }


        /******************************************************************** A4 PRINT ****************************************************************************************/

        private void PrintReceiptA4(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            //int pageWidth = e.PageSettings.PaperSize.Width;
            int topMargin = e.PageSettings.Margins.Top;
            int leftMargin = e.PageSettings.Margins.Left;
            //Rectangle r = e.MarginBounds;
            
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;

            StringFormat sfL = new StringFormat();
            sfL.Alignment = StringAlignment.Near;
            sfL.LineAlignment = StringAlignment.Near;

            // STORE NAME
            Rectangle r2 = new Rectangle(leftMargin, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("INTENSE PVT. LTD.", new Font("Arial", 12, FontStyle.Bold), Brushes.Black, r2, sf);
            topMargin += 20;

            // ADDRESS
            r2 = new Rectangle(leftMargin, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("Gulfesha Plaza, Moghbazar, Dhaka", new Font("Arial", 10, FontStyle.Regular), Brushes.Black, r2, sf);
            topMargin += 20;

            // CONTACT NUMBERS
            r2 = new Rectangle(leftMargin, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("Call: (+88) 01835 410 998, (+88) 01819 244 297", new Font("Arial", 9, FontStyle.Regular), Brushes.Black, r2, sf);
            topMargin += 55;

            // Invoice no.
            r2 = new Rectangle(leftMargin, topMargin, e.MarginBounds.Width, 20);
            string invoiceno = dgvInvoiceHead.Rows[0].Cells[1].Value.ToString();
            e.Graphics.DrawString("Invoice No. " + invoiceno, new Font("Arial", 9, FontStyle.Bold), Brushes.Black, r2, sfL);
            topMargin += 20;

            // Date and time
            r2 = new Rectangle(leftMargin, topMargin, e.MarginBounds.Width, 20);
            string _sdate = dgvInvoiceHead.Rows[3].Cells[1].Value.ToString();
            string _time = dgvInvoiceHead.Rows[4].Cells[1].Value.ToString();
            e.Graphics.DrawString("Date: " + _sdate + " " + _time, new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);
            topMargin += 25;

            r2 = new Rectangle(leftMargin, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("Sl.", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);

            r2 = new Rectangle(leftMargin + 20, topMargin + 20, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);

            r2 = new Rectangle(leftMargin + 20 + 20 + 92 + 23, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("Item Name", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);

            r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("Unit Price", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);

            r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("Qty", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);

            // UNIT NAME (AFTER QUANTITY)
            r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);

            r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("Discount", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);

            r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60 + 80, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("VAT", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);

            r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60 + 80 + 50, topMargin, e.MarginBounds.Width, 20);
            e.Graphics.DrawString("Item Total", new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);

            topMargin += 15 + 10;

            for (int i = 0; i < dgvOrderDetails.RowCount; i++)
            {
                // Sl.
                r2 = new Rectangle(leftMargin, topMargin + ((92 + 23) /2), e.MarginBounds.Width, 20);
                string sl = dgvOrderDetails.Rows[i].Cells[0].Value.ToString();
                e.Graphics.DrawString(sl, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, r2, sfL);                

                // image
                r2 = new Rectangle(leftMargin + 20, topMargin, 92 + 23, 92 + 23);
                string imagePath = Directory.GetCurrentDirectory() + @"\images\" + dgvOrderDetails.Rows[i].Cells[dgvOrderDetails.Rows[i].Cells.Count - 1].Value.ToString();
                System.Drawing.Image img = System.Drawing.Image.FromFile(imagePath);                
                e.Graphics.DrawImage(img, r2);

                

                // Itam Name
                r2 = new Rectangle(leftMargin + 20 + 20 + 92 + 23, (topMargin -20 + ((92 + 23) / 2)), 140, 60);
                string itemName = dgvOrderDetails.Rows[i].Cells[2].Value.ToString();
                e.Graphics.DrawString(itemName, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, r2, sfL);


                // Unit Price
                r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80, topMargin - 20 + ((92 + 23) / 2), 80, 20);
                string unitPrice = double.Parse(dgvOrderDetails.Rows[i].Cells[3].Value.ToString()).ToString("0.00");
                e.Graphics.DrawString(unitPrice, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, r2, sfL);

                // Qty
                r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80, topMargin - 20 + ((92 + 23) / 2), 80, 20);
                string qty = dgvOrderDetails.Rows[i].Cells[4].Value.ToString();
                e.Graphics.DrawString(qty, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, r2, sfL);

                // Unit
                r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20, topMargin - 20 + ((92 + 23) / 2), 80, 20);
                string unit = dgvOrderDetails.Rows[i].Cells[5].Value.ToString();
                e.Graphics.DrawString(unit, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, r2, sfL);

                // Discount
                r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60, topMargin - 20 + ((92 + 23) / 2), 80, 20);
                string discount = double.Parse(dgvOrderDetails.Rows[i].Cells[6].Value.ToString()).ToString("0.00");
                e.Graphics.DrawString(discount, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, r2, sfL);

                // VAT
                r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60 + 80, topMargin - 20 + ((92 + 23) / 2), 80, 20);
                string vat = double.Parse(dgvOrderDetails.Rows[i].Cells[8].Value.ToString()).ToString("0.00");
                e.Graphics.DrawString(vat, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, r2, sfL);

                // Item Total
                r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60 + 80 + 50, topMargin - 20 + ((92 + 23) / 2), 80, 20);
                string itemTotal = double.Parse(dgvOrderDetails.Rows[i].Cells[7].Value.ToString()).ToString("0.00");
                e.Graphics.DrawString(itemTotal, new Font("Arial", 8, FontStyle.Regular), Brushes.Black, r2, sfL);


                topMargin += 15 + 20 + 10 + 92 + 23 - 10;
            }

            topMargin -= 20;

            Pen blackPen = new Pen(Color.Black, 1);
            blackPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
            
            // Create points that define line.
            Point point1 = new Point(leftMargin, topMargin);
            Point point2 = new Point(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60 + 80 + 50 + 80, topMargin);
            e.Graphics.DrawLine(blackPen, point1, point2);
            topMargin += 5;


            for (int i = 0; i < dgvOrderSummary.RowCount; i++)
            {
                
                if (i == 1)
                {
                    r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60 + 80 - 25, topMargin, 80, 20);
                    e.Graphics.DrawString(dgvOrderSummary.Rows[0].Cells[0].Value.ToString(), new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);

                    r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60 + 80 - 20 + 70, topMargin, 80, 20);
                    e.Graphics.DrawString(double.Parse(dgvOrderSummary.Rows[0].Cells[1].Value.ToString()).ToString("0.00"), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, r2, sfL);
                    
                    topMargin += 15;
                }
                
                r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60 + 80 - 25, topMargin, 80, 20);
                e.Graphics.DrawString(dgvOrderSummary.Rows[i].Cells[2].Value.ToString(), new Font("Arial", 8, FontStyle.Bold), Brushes.Black, r2, sfL);
                
                r2 = new Rectangle(leftMargin + 20 + 100 + (92 + 23) + 80 + 80 + 20 + 60 + 80 - 20 + 70, topMargin, 80, 20);
                e.Graphics.DrawString(double.Parse(dgvOrderSummary.Rows[i].Cells[3].Value.ToString()).ToString("0.00"), new Font("Arial", 8, FontStyle.Regular), Brushes.Black, r2, sfL);                
                
                topMargin += 15;

            }

            return;            
        }


        #endregion



        private void txtAccessPIN_TextChanged(object sender, EventArgs e)
        {           
            if (!IsNumeric(txtAccessPIN.Text) || txtAccessPIN.Text.Length > 4) txtAccessPIN.Text = "";
        }

        private void dgvOrderDetails_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.ColumnIndex == 5)
            {
                dgvOrderDetails.Columns[e.ColumnIndex].ReadOnly = false;
                DataGridViewCell cell = dgvOrderDetails.SelectedRows[0].Cells[5];
                dgvOrderDetails.CurrentCell = cell;
                dgvOrderDetails.BeginEdit(true);

                // keeping the orignal unit before manual change of unit
                //oUnit = cell.Value.ToString();    // Not necessary now as flag as oUnits is now array and already taken from database
            }
        }

        private void btnCalcDevide_Click(object sender, EventArgs e)
        {
            calcOperator = "/";            
            operand1 = double.Parse(txtCalcInput.Text);
            txtCalcInput.Text = "";
            btnCalcDivide.Enabled = btnCalcMultiply.Enabled = btnCalcPlus.Enabled = btnCalcMinus.Enabled = false;
        }

        private void btnCalcMultiply_Click(object sender, EventArgs e)
        {
            if (operand1 > 0 && operand2 == 0 && txtCalcInput.Text != "" && calcOperator != "" && calcOperator != ".")
            {
                operand2 = double.Parse(txtCalcInput.Text);
                txtCalcInput.Text = operand2.ToString();
                txtCalcInput_TextChanged(sender, e);
                operand2 = 0;
                btnCalculate_Click(sender, e);
                operand1 = calcResult;
                calcResult = 0;     // just one level more to hold value for further calculation
            }
            else operand1 = double.Parse(txtCalcInput.Text);

            calcOperator = "*";                                    
            txtCalcInput.Text = "";

            btnCalcDivide.Enabled = btnCalcMultiply.Enabled = btnCalcPlus.Enabled = btnCalcMinus.Enabled = false;
        }

        private void btnCalcMinus_Click(object sender, EventArgs e)
        {
            calcOperator = "-";            
            operand1 = double.Parse(txtCalcInput.Text);
            txtCalcInput.Text = "";
            btnCalcDivide.Enabled = btnCalcMultiply.Enabled = btnCalcPlus.Enabled = btnCalcMinus.Enabled = false;
        }

        private void btnCalcPlus_Click(object sender, EventArgs e)
        {
            calcOperator = "+";            
            operand1 = double.Parse(txtCalcInput.Text);
            txtCalcInput.Text = "";
            btnCalcDivide.Enabled = btnCalcMultiply.Enabled = btnCalcPlus.Enabled = btnCalcMinus.Enabled = false;
        }

        private void btnDigit1_Click(object sender, EventArgs e)
        {
            InputDigit(btnDigit1.Text);

            /*
            string oldVal = txtCalcInput.Text.Substring(txtCalcInput.SelectionStart) + btnDigit1.Text;
            string newVal = txtCalcInput.Text.Substring(txtCalcInput.SelectionStart) + btnDigit1.Text;
            txtCalcInput.Text = txtCalcInput.Text.Substring(txtCalcInput.SelectionStart).Replace(oldVal, newVal);
            txtCalcInput_TextChanged(sender, e);
            */
        }


        private void InputDigit(string digit)
        {
            if(calcOperator != "" && calcOperator != ".")
            {
                txtCalcInput.Text += digit; return;
            }

            if (txtCalcInput.Text.Length > 17) return;

            string str1 = txtCalcInput.Text.Split('.')[0];
            string str2 = "";
            if (txtCalcInput.Text.Split('.').Length > 2)
                str2 = txtCalcInput.Text.Split('.')[1];

            int decimalPlaces = 0;

            if(txtCalcInput.Text.Split('.').Length>0)
                decimalPlaces = txtCalcInput.Text.Split('.')[1].Length;

            if (decimalPlaces >= 2 && calcOperator != ".")
            {
                if (str2 != "")
                    txtCalcInput.Text = str1 + digit + "." + str2;
                else
                    txtCalcInput.Text = str1 + digit;
                //if(str2 != "")
                    //txtCalcInput.Text = str1 + digit + "." + str2;
                txtCalcInput.SelectionStart = txtCalcInput.Text.Length - (decimalPlaces + 1) - 1;
            }
            else if (decimalPlaces < 2 && calcOperator == ".")
            {
                

                if (decimalPlaces == 1 && calcOperator == ".")
                {
                    //str2 = str2 + digit; //+ digit;
                    //txtCalcInput.Text = str1 + "." + str2 + digit; //txtCalcInput.Text.Split('.')[1] + digit;
                    txtCalcInput.Text += digit; //str2 + "." + str2;
                    calcOperator = "";
                }

                else
                {
                    txtCalcInput.Text = str1 + "." + str2 + digit;
                    txtCalcInput.SelectionStart = txtCalcInput.Text.Length - 1;
                }
            }
            /*
            if (calcOperator == ".")
            {

                if (txtCalcInput.Text.Split('.')[1].Length < 2)
                {
                    str2 = txtCalcInput.Text.Split('.')[1]; //+ digit;
                    txtCalcInput.Text = str1 + "." + txtCalcInput.Text.Split('.')[1] + digit;                    
                }

                if (txtCalcInput.Text.Split('.')[1].Length == 2)
                { 
                    txtCalcInput.Text = double.Parse(txtCalcInput.Text).ToString("0.00");
                    calcOperator = "";
                }
            }
            */
            //txtCalcInput_TextChanged(sender, e);
        }

        private void btnDigit2_Click(object sender, EventArgs e)
        {
            InputDigit(btnDigit2.Text);
        }

        private void btnDigit3_Click(object sender, EventArgs e)
        {
            InputDigit(btnDigit3.Text);
        }

        private void btnDigit4_Click(object sender, EventArgs e)
        {
            InputDigit(btnDigit4.Text);
        }

        private void btnDigit5_Click(object sender, EventArgs e)
        {
            InputDigit(btnDigit5.Text);
        }

        private void btnDigit6_Click(object sender, EventArgs e)
        {
            InputDigit(btnDigit6.Text);
        }

        private void btnDigit7_Click(object sender, EventArgs e)
        {
            InputDigit(btnDigit7.Text);
        }

        private void btnDigit8_Click(object sender, EventArgs e)
        {
            InputDigit(btnDigit8.Text);
        }

        private void btnDigit9_Click(object sender, EventArgs e)
        {
            InputDigit(btnDigit9.Text);
        }

        private void btnDigitDecPoint_Click(object sender, EventArgs e)
        {
            calcOperator = ".";
            int decimalPlaces = txtCalcInput.Text.Split('.')[1].Length;

            if (decimalPlaces >= 2 && txtCalcInput.Text.Split('.')[1] == "00")
                txtCalcInput.Text = txtCalcInput.Text.Split('.')[0] + ".";
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {            
            operand2 = double.Parse(txtCalcInput.Text.ToString()); 

            switch (calcOperator)
            {
                case "/":
                    calcResult = operand1 / operand2;
                    txtCalcInput.Text = calcResult.ToString();
                    break;

                case "*":
                    calcResult = operand1 * operand2;
                    txtCalcInput.Text = calcResult.ToString();
                    break;

                case "-":
                    calcResult = operand1 - operand2;
                    txtCalcInput.Text = calcResult.ToString();
                    break;

                case "+":
                    calcResult = operand1 + operand2;
                    txtCalcInput.Text = calcResult.ToString();
                    break;
            }

            calcOperator = "";
            operand1 = operand2 = 0;

            btnCalcDivide.Enabled = btnCalcMultiply.Enabled = btnCalcPlus.Enabled = btnCalcMinus.Enabled = true;

            txtCalcInput_TextChanged(sender, e);
        }

        private void btnDigit0_Click(object sender, EventArgs e)
        {
            InputDigit(btnDigit0.Text);
        }

        private void btnSaveCustomer_Click(object sender, EventArgs e)
        {
            switch(btnSaveCustomer.Text)
            {
                case "Order":                    
                        dgvInvoiceHead.Rows[8].Cells[1].Value = txtCustomerMobile.Text;
                        customerName = txtCustomerName.Text;
                        customerEmail = txtCustomerEmail.Text;
                        customerMobile = txtCustomerMobile.Text;
                        isCustomer = true;
                        CancelSavingCustomer();                    
                    break;

                case "Change":                    
                    txtCustomerName.Text = txtCustomerEmail.Text = txtCustomerMobile.Text = "";
                    btnSaveCustomer.Enabled = false;
                    btnSaveCustomer.BackColor = Color.AliceBlue;
                    btnSaveCustomer.ForeColor = SystemColors.ControlDark;
                    txtCustomerMobile.Focus();
                    break;

                case "Find":
                    FindCustomer();
                    break;

                case "Save":
                    SaveCustomer();
                    break;
            }
        }

        private void SaveCustomer()
        {
            if (txtCustomerName.Text == "")
            {
                txtCustomerName.Focus();
                return;
            }
            else if (txtCustomerEmail.Text == "")
            {
                txtCustomerEmail.Focus();
                return;
            }
            else if (txtCustomerMobile.Text == "")  // though not required.
            {
                txtCustomerMobile.Focus();
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

            Query = "SELECT(MAX(ID) + 1) AS ID FROM customers";
            //Query = "SELECT TOP 1 ID + 1 AS cid, user_id FROM customers ORDER BY ID DESC";
            cmd.CommandText = Query;

            OleDbDataAdapter da = new OleDbDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count > 0)
                iCustomerId = dt.Rows[0]["ID"].ToString();
                //iCustomerId = dt.Rows[0]["cid"].ToString();
            

            Query = "INSERT INTO customers (customer_id, user_id, customer_name, customer_email, customer_mobile) VALUES ( " + iCustomerId + ", -1 , '" + txtCustomerName.Text + "', '" + txtCustomerEmail.Text + "', '" + txtCustomerMobile.Text + "')";            

            try
            {
                cmd.CommandText = Query;
                cmd.ExecuteReader();

                if (btnSaveCustomer.Text == "Save")
                {
                    btnSaveCustomer.Text = "Order";
                    txtCustomerMobile.Focus();
                }
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                MessageBox.Show(ErrorMessage);
            }

            Conn.Close();
        }

        private void btnCalcOK_Click(object sender, EventArgs e)
        {            
            if(opsFlag.Count > 0)
            {
                int index = opsFlag["ItemTotal"];
                dgvOrderDetails.Rows[index].Cells[7].Value = txtCalcInput.Text;
                UpdateOrderSummary(0);
                opsFlag.Remove("ItemTotal");

                // for updating in local db
                if (isOrder && orderedItem.Count > 0)
                {
                    orderedItem["product_id"] = dgvOrderDetails.Rows[index].Cells[1].Value.ToString();
                    orderedItem["item_total"] = double.Parse(txtCalcInput.Text).ToString("0.00");
                    UpdateItem(orderedItem, "item_total");
                }
            }

            cmbProductDropDown.SelectAll();
            cmbProductDropDown.Focus();
        }

        private void btnClearCalcInput_Click(object sender, EventArgs e)
        {
            txtCalcInput.Text = "";        
        }


        #region Order

        ///////////////////////// ORDER TAKING PROCESS /////////////////////
        
        bool isOrder = false;
        int orderId, iOrderId;

        Dictionary<string, string> orderedItem = new Dictionary<string, string>();
        Dictionary<string, string> order = new Dictionary<string, string>();

        private void CreateOrder(string invoiceNo)
        {
            if (dgvOrderDetails.Rows.Count == 0) return;

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

            Query = "SELECT(MAX(ID) + 1) AS ID FROM orders";
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
                string sOrderId = dt.Rows[0]["ID"].ToString();
                if (sOrderId == "") iOrderId = 1;
                else iOrderId = int.Parse(sOrderId);
            }

            if (isCustomer)
                if (customerId == "0" && Int32.Parse(iCustomerId) > 0) customerId = iCustomerId;    

            Query = "INSERT INTO orders (order_id, customer_id, order_invoice_no, cur_status) " +
                "VALUES ( " + iOrderId + ", " + customerId + ", '" + invoiceNo + "', 'OPEN')";

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
            orderId = (Int32)cmd.ExecuteScalar();
            isOrder = true;     // i.e.: Now, there is an open order that is on process.

            if (order.ContainsKey("customerId")) order["customerId"] = customerId; else order.Add("customerId", customerId.ToString());
            if (order.ContainsKey("customerName")) order["customerName"] = customerName; else order.Add("customerName", customerName);
            if (order.ContainsKey("customerEmail")) order["customerEmail"] = customerEmail; else order.Add("customerEmail", customerEmail);
            if (order.ContainsKey("customerMobile")) order["customerMobile"] = customerMobile; else order.Add("customerMobile", customerMobile);

            if (!order.ContainsKey("additional_discount")) order.Add("additional_discount", "0.00");
            if (!order.ContainsKey("additional_discount_type")) order.Add("additional_discount_type", "none");

            Conn.Close();
        }

        private void UpdateOrder(Dictionary<string, string> order)
        {
            if (dgvOrderDetails.Rows.Count == 0) return;

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

            Query = "UPDATE orders SET ";
            Query += "sub_total = " + order["sub_total"] + " ";

            ///// JUST ADD THE CUSTOMER ID FOR UPDATE AT A TIME
            if(order.ContainsKey("customerId"))
                Query += ", customer_id = " + order["customerId"] + " ";

            if (order.ContainsKey("customerName")) order["customerName"] = customerName; else order.Add("customerName", customerName);
            if (order.ContainsKey("customerEmail")) order["customerEmail"] = customerEmail; else order.Add("customerEmail", customerEmail);
            if (order.ContainsKey("customerMobile")) order["customerMobile"] = customerMobile; else order.Add("customerMobile", customerMobile);

            Query += ", vat = " + order["vat"] + " ";
            Query += ", discount = " + order["discount"] + " ";
            Query += ", num_items = " + order["num_items"] + " ";
            Query += ", grand_total = " + order["grand_total"] + " ";
            Query += ", payment = " + order["payment"] + " ";
            Query += ", changes = " + order["changes"] + " ";
            Query += ", due = " + order["due"] + " ";
            Query += " WHERE order_id = " + order["order_id"];

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
            
            cmd.CommandText = Query;            
            isOrder = true;

            Conn.Close();            
        }

        private void syncInventorySubMenu_Click(object sender, EventArgs e)
        {
            /// POS TEST CONSOLE
            /// ================
            /// This is the PoS Test Console with incorrect naming and for developers and testers only.
            /// 

            InventoryScr scr = new InventoryScr();
            //scr.MdiParent = this;     /// will work later
            scr.Width = this.Width * 85/100;
            scr.Height = this.Height * 85 / 100;
            scr.StartPosition = FormStartPosition.CenterParent;
            //scr.BringToFront();       /// will work later
            scr.ShowDialog();
            scr.ShowInTaskbar = false;  /// will work later
        }

        private void OrderScr_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(!keyVerified)
            {
                e.Cancel = false;
                return;
            }
            
            var msg = MessageBox.Show("Are you sure to exit?", "Exit PoS?", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (msg == DialogResult.OK)
            {
                e.Cancel = false;
            }
            else e.Cancel = true;
        }

        private void smPurchase_Click(object sender, EventArgs e)
        {
            if (gbAccess.Visible == true) return;

            StockScr scr = new StockScr(1);

            /////////////////////////////////////////
            //scr.MdiParent = this;   // Fix it later
            /////////////////////////////////////////

            scr.Size = new Size(this.Width * 95 / 100, this.Height * 95 / 100);
            scr.StartPosition = FormStartPosition.CenterScreen;
            scr.ShowIcon = false;
            scr.ShowInTaskbar = false;
            scr.ShowDialog();                                   
        }

        private void rdoAmount_CheckedChanged(object sender, EventArgs e)
        {            
            gbCouponDiscount.Text = "ENTER DISCOUNT AMOUNT";
            rdoAmount.ForeColor = Color.Black;
            rdoPercent.ForeColor = SystemColors.HighlightText;
            btnAddDiscountOnGT.Enabled = true;
            txtCouponDiscount.Focus();
        }

        private void rdoPercent_CheckedChanged(object sender, EventArgs e)
        {            
            gbCouponDiscount.Text = "ENTER DISCOUNT (%)";
            rdoPercent.ForeColor = Color.Black;
            rdoAmount.ForeColor = SystemColors.HighlightText;
            btnAddDiscountOnGT.Enabled = false;
            txtCouponDiscount.Focus();
        }

        private void rdoPayMode_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radio = (RadioButton)sender;
            if (radio.Checked)
            {
                payMode = radio.Text;
                radio.ForeColor = SystemColors.ControlText;
                btnSelectPayMode.Focus();
            }
        }

        private void btnSelectPayMode_Click(object sender, EventArgs e)
        {
            if (payMode != "")
            {
                oFlag = 1;                
                payModePane.Visible = false;
                pConfirmPane.Visible = true;
                if (OrderStateChecked())
                    btnSaveOrder.Focus();
            }
        }

        private void btnSelectPayMode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                CancelSavingOrder();
        }

        private void btnPayment_Click(object sender, EventArgs e)
        {
            if (double.Parse(txtCalcInput.Text) >= 0.00 && dgvInvoiceHead.Rows[0].Cells[1].Value.ToString() != "" && double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()) > 0.00)
            {
                // return if the customer not registered but want to keep due (not allowed)

                string customerMobile = dgvInvoiceHead.Rows[8].Cells[1].Value.ToString();
                double due = double.Parse(dgvOrderSummary.Rows[5].Cells[3].Value.ToString());

                if (!isCustomer && customerMobile == "-" && due > 0.00 || !isCustomer && customerMobile == "" && due > 0)
                {
                    var msg = MessageBox.Show("For saving any order with due amount the customer should be registered.\n\nDo you want to register the customer?", "Customer not registered!", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                    if (msg == DialogResult.Yes)
                        ShowCustomerPane();
                    return;
                }
                /// end of return for due of unregistered customer ///

                lPane.Enabled = false;
                rPane.Enabled = false;
                gbPrintConfirmation.Visible = true;

                gbPrintConfirmation.Left = this.Width / 2 - gbPrintConfirmation.Width / 2;
                gbPrintConfirmation.Top = this.Height / 2 - gbPrintConfirmation.Height / 2;


                // exit - close session disabled
                btnCloseOrderScr.Enabled = false;
                btnCloseSession.Enabled = false;

                if (OrderStateChecked())
                    btnSaveOrder.Focus();
            }
            else
                cmbProductDropDown.Focus();
        }

        private void ordersInQueueSubMenu_Click(object sender, EventArgs e)
        {
            customerSpecific = false;            
            ShowOrderListPane();

            // Default filter: List in queue
            rdoQueue.Checked = true;
            rdoQueue.Focus();

            /////////////////////            
            rdoQueue.Parent.BackColor = gbOrderList.Parent.BackColor = lPane.BackColor = SystemColors.ControlLightLight;    /// ?? fLayoutPaneOrderListFilters.BackColor

            if (dgvOrderList.Rows.Count == 0)
                OrderListFilter(sender, e);
        }

        private void ShowDefaultPanes(bool show)
        {
            if (show)
                tSearchPane.Visible = tOrderPane.Visible = tOrderFooterPane.Visible = tCtrlFPane.Visible = true;
            else
                tSearchPane.Visible = tOrderPane.Visible = tOrderFooterPane.Visible = tCtrlFPane.Visible = false;
        }
        
        private void ShowOrderListPane()
        {
            ShowDefaultPanes(false);
            
            orderListPane.Width = tOrderPane.Width;
            orderListPane.Top = paymentPane.Top;
            orderListPane.Height = tSearchPane.Height + tOrderPane.Height + tOrderFooterPane.Height - orderListPane.Top;
            orderListPane.Left = tOrderPane.Left;

            rdoQueue.Enabled = rdoPaid.Enabled = rdoDue.Enabled = true;            

            dgvOrderList.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
            dgvOrderList.Columns[0].Width = dgvOrderList.Columns[1].Width = dgvOrderList.Columns[2].Width = dgvOrderList.Columns[3].Width = dgvOrderList.Columns[4].Width = dgvOrderList.Columns[5].Width = dgvOrderList.Columns[6].Width = dgvOrderList.Columns[7].Width = dgvOrderList.Columns[8].Width = dgvOrderList.Columns[9].Width = dgvOrderList.Width / 8;
            dgvOrderList.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            dgvOrderList.Columns[8].DefaultCellStyle.Alignment = dgvOrderList.Columns[9].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            orderListPane.Visible = true;
            dgvOrderList.Rows.Clear();
        }

        private void AddItem(Dictionary<string, string> orderedItem)
        {
            if (dgvOrderDetails.Rows.Count == 0) return;                       

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

            Query = "INSERT INTO ordered_items (order_id, product_id, qty, discount_total, vat_total, item_total) VALUES ( " + orderedItem ["order_id"] + ", " + orderedItem["product_id"] + ", " + orderedItem["qty"] + ", " + orderedItem["discount_total"] + ", " + orderedItem["vat_total"] + ", " + orderedItem["item_total"] + ")";

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

            if (isOrder)
            {
                if(order.Count == 0)
                {
                    order.Add("order_id", orderId.ToString());
                    order.Add("sub_total", double.Parse(dgvOrderSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00"));
                    string vat = dgvOrderDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    order.Add("vat", vat);
                    order.Add("discount", double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00"));                    
                    order.Add("num_items", dgvOrderDetails.Rows.Count.ToString());
                    order.Add("grand_total", (double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString()) + double.Parse(vat)).ToString("0.00"));
                    order.Add("payment", double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()).ToString("0.00"));
                    order.Add("changes", double.Parse(dgvOrderSummary.Rows[4].Cells[3].Value.ToString()).ToString("0.00"));
                    order.Add("due", double.Parse(dgvOrderSummary.Rows[5].Cells[3].Value.ToString()).ToString("0.00"));
                }
                else
                {
                    if (order.ContainsKey("order_id")) order["order_id"] = orderId.ToString(); else order.Add("order_id", orderId.ToString());
                    order["sub_total"] = double.Parse(dgvOrderSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00");
                    string vat = dgvOrderDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    order["vat"] = vat;
                    order["discount"] = double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    order["num_items"] = dgvOrderDetails.Rows.Count.ToString();
                    order["grand_total"] = (double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString()) + double.Parse(vat)).ToString("0.00");
                    order["payment"] = double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()).ToString("0.00");
                    order["changes"] = double.Parse(dgvOrderSummary.Rows[4].Cells[3].Value.ToString()).ToString("0.00");
                    order["due"] = double.Parse(dgvOrderSummary.Rows[5].Cells[3].Value.ToString()).ToString("0.00");
                }

                UpdateOrder(order);
            }

            /////////////////////////////////////////////////////////////////
        }

        private void newOrderSubMenu_Click(object sender, EventArgs e)
        {
            customerSpecific = false;
            orderListPane.Visible = false;
            ShowDefaultPanes(true);
            ResetOrderScreen();
            cmbProductDropDown.Focus();            
        }

        private void btnPreviousOrders_Click(object sender, EventArgs e)
        {
            if (!isCustomer)
            {
                cmbProductDropDown.Focus();
                return;
            }

            dgvOrderList.Rows.Clear();

            customerSpecific = true;
            ShowOrderListPane();

            // Default filter: List in queue
            rdoQueue.Checked = true;
            rdoQueue.Focus();
        }

        private void UpdateItem (Dictionary<string, string> orderedItem, string field = null)
        {
            if (dgvOrderDetails.Rows.Count == 0) return;

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
                Query = "UPDATE ordered_items SET " + field + " = " + orderedItem[field];
            else
                Query = "UPDATE ordered_items SET qty = " + orderedItem["qty"] + ", discount_total = " + orderedItem["discount_total"] + ", vat_total = " + orderedItem["vat_total"] + ", item_total = " + orderedItem["item_total"];
            
            Query += " WHERE order_id = " + orderedItem["order_id"] +  " AND product_id = " + orderedItem["product_id"];

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

            if (isOrder)
            {
                if (order.Count == 0)
                {
                    order.Add("order_id", orderId.ToString());
                    order.Add("sub_total", double.Parse(dgvOrderSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00"));
                    string vat = dgvOrderDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    order.Add("vat", vat);
                    order.Add("discount", double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00"));
                    order.Add("num_items", dgvOrderDetails.Rows.Count.ToString());
                    order.Add("grand_total", double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00"));
                    order.Add("payment", double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()).ToString("0.00"));
                    order.Add("changes", double.Parse(dgvOrderSummary.Rows[4].Cells[3].Value.ToString()).ToString("0.00"));
                    order.Add("due", double.Parse(dgvOrderSummary.Rows[5].Cells[3].Value.ToString()).ToString("0.00"));
                }
                else
                {
                    order["sub_total"] = double.Parse(dgvOrderSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00");
                    string vat = dgvOrderDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    order["vat"] = vat;
                    order["discount"] = double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    order["num_items"] = dgvOrderDetails.Rows.Count.ToString();
                    order["grand_total"] = double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00");
                    order["payment"] = double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()).ToString("0.00");
                    order["changes"] = double.Parse(dgvOrderSummary.Rows[4].Cells[3].Value.ToString()).ToString("0.00");
                    order["due"] = double.Parse(dgvOrderSummary.Rows[5].Cells[3].Value.ToString()).ToString("0.00");
                }

                UpdateOrder(order);
            }
            ////////////

        }

        private void ordersPaidSubMenu_Click(object sender, EventArgs e)
        {
            customerSpecific = false;
            ShowOrderListPane();

            // Default filter: List in queue
            rdoPaid.Checked = true;
            rdoPaid.Focus();
        }

        private void ordersDueSubMenu_Click(object sender, EventArgs e)
        {
            customerSpecific = false;
            ShowOrderListPane();

            // Default filter: List in queue
            rdoDue.Checked = true;
            rdoDue.Focus();
        }

        private void btnCoupon_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show("Sorry the feature is currently unavailable.\nWe are working on it...", "Coupon", MessageBoxButtons.OK, MessageBoxIcon.Question);
        }

        private void btnReturnGoods_Click(object sender, EventArgs e)
        {            
            ReturnProduct();
        }

        private void ReturnProduct()
        {

            lPane.Enabled = false;
            rPane.Enabled = false;
            gbCouponDiscount.Visible = true;

            gbCouponDiscount.Left = this.Width / 2 - gbCouponDiscount.Width / 2;
            gbCouponDiscount.Top = this.Height / 2 - gbCouponDiscount.Height / 2;            

            // exit - close session disabled
            btnCloseOrderScr.Enabled = false;
            btnCloseSession.Enabled = false;

            ///////////////////////////////////////////////
            txtCouponDiscount.RightToLeft = RightToLeft.Yes;    // DECIDE/ RESET WITH THIS // NO GLOBAL VARIABLE           
            rdoAmount.Visible = rdoPercent.Visible = false;
            gbCouponDiscount.Text = "ENTER INVOICE NO.";
            txtCouponDiscount.Top = gbCouponDiscount.Height/2 - txtCouponDiscount.Height/2 - btnApplyCoupon.Height;
            btnApplyCoupon.Top = txtCouponDiscount.Top + txtCouponDiscount.Height + 20;
            btnAddDiscountOnGT.Visible = txtCouponDiscount.Visible = false;
            txtInvoiceNo.Visible = true; txtInvoiceNo.Left = txtCouponDiscount.Left; txtInvoiceNo.Top = txtCouponDiscount.Top; txtInvoiceNo.BringToFront();
            txtInvoiceNo.Text = "";
            txtInvoiceNo.Focus();
            ///////////////////////////////////////////////

        }

        private void dgvOrderList_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (dgvOrderList.Rows.Count == 0) return;

            if (dgvOrderList.Rows.Count > 0 && dgvOrderList.CurrentCell.ColumnIndex == 1)
            {
                if (dgvOrderList.Rows[0].Cells[1].Value == null) return;
                else if (dgvOrderList.Rows[0].Cells[1].Value.ToString() != "")
                    GetOrder(dgvOrderList.Rows[0].Cells[dgvOrderList.CurrentCell.ColumnIndex].Value.ToString());
            }
            else return;
        }



        private void GetOrder(string invoiceNo)
        {
            // Working...
            // return;
            
            ShowDefaultPanes(true);

            customerSpecific = false;
            dgvOrderDetails.Rows.Clear();

            DataTable dt = new DataTable();

            try
            {
                dt = Order.GetOrdereByInvoice(invoiceNo);
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
                return;
            }

            if (dt != null && dt.Rows.Count > 0)
            {
                // LET'S CONFIGURE ORDER
                // Order
                isOrder = true;
                //orderId = int.Parse(dt.Rows[0]["order_id"].ToString());
                orderId = int.Parse(dt.Rows[0]["o.ID"].ToString());
                iOrderId = 0;

                order.Clear();
                orderedItem.Clear();

                order.Add("order_id", dt.Rows[0]["order_id"].ToString());
                orderedItem.Add("order_id", dt.Rows[0]["order_id"].ToString());

                order.Add("order_invoice_no", dt.Rows[0]["order_invoice_no"].ToString());
                order.Add("customer_id", dt.Rows[0]["o.customer_id"].ToString());

                ////// initial values
                order.Add("sub_total", dt.Rows[0]["sub_total"].ToString());
                order.Add("vat", dt.Rows[0]["vat"].ToString());
                order.Add("discount", dt.Rows[0]["discount"].ToString());
                order.Add("num_items", dt.Rows[0]["num_items"].ToString());
                order.Add("grand_total", dt.Rows[0]["grand_total"].ToString());
                order.Add("payment", dt.Rows[0]["payment"].ToString());
                order.Add("changes", dt.Rows[0]["changes"].ToString());
                order.Add("due", dt.Rows[0]["due"].ToString());

                //Customer
                isCustomer = false;
                customerId = dt.Rows[0]["c.customer_id"].ToString();
                customerName = dt.Rows[0]["customer_name"].ToString();
                customerEmail = dt.Rows[0]["customer_email"].ToString();
                customerMobile = dt.Rows[0]["customer_mobile"].ToString();

                // Feel invoice heads
                dgvInvoiceHead.Rows[0].Cells[1].Value = dt.Rows[0]["order_invoice_no"];
                dgvInvoiceHead.Rows[1].Cells[1].Value = double.Parse(dt.Rows[0]["sub_total"].ToString()).ToString("0.00");
                dgvInvoiceHead.Rows[2].Cells[1].Value = double.Parse(dt.Rows[0]["discount"].ToString()).ToString("0.00");
                dgvInvoiceHead.Rows[3].Cells[1].Value = DateTime.Parse(dt.Rows[0]["order_date_time"].ToString()).ToString("dd/MM/yyyy");
                dgvInvoiceHead.Rows[4].Cells[1].Value = DateTime.Parse(dt.Rows[0]["order_date_time"].ToString()).ToString("h:mm tt");
                dgvInvoiceHead.Rows[5].Cells[1].Value = dt.Rows[0]["cur_status"];
                dgvInvoiceHead.Rows[8].Cells[1].Value = dt.Rows[0]["customer_mobile"];

                // Feel order summary
                dgvOrderSummary.Rows[0].Cells[1].Value = double.Parse(dt.Rows[0]["vat"].ToString()).ToString("0.00");
                dgvOrderSummary.Rows[0].Cells[3].Value = dt.Rows[0]["sub_total"];
                dgvOrderSummary.Rows[1].Cells[3].Value = dt.Rows[0]["discount"];
                dgvOrderSummary.Rows[2].Cells[3].Value = dt.Rows[0]["grand_total"];
                dgvOrderSummary.Rows[3].Cells[3].Value = dt.Rows[0]["payment"];
                dgvOrderSummary.Rows[4].Cells[3].Value = dt.Rows[0]["changes"];
                dgvOrderSummary.Rows[5].Cells[3].Value = dt.Rows[0]["due"];
            }
            else return;

            try
            {
                dt = new DataTable();
                dt = Order.GetOrderedItemsByInvoice(invoiceNo);
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
                return;
            }

            if (dt == null || dt.Rows.Count == 0) return;

            foreach (DataRow row in dt.Rows)
            {
                //dgvOrderDetails.Rows.Add((dgvOrderDetails.Rows.Count + 1), row["ID"], row["ItemName"], row["UnitPrice"], row["Qty"], "(" + ti.ToTitleCase(row["Unit"].ToString()) + ")", double.Parse(row["Discount"].ToString()).ToString("0.00"), double.Parse(row["ItemTotal"].ToString()).ToString("0.00"), double.Parse(row["vat"].ToString()).ToString("0.00"), double.Parse(row["Discount"].ToString()).ToString("0.00"), double.Parse(row["vat"].ToString()).ToString("0.00"), row["bUnit"], row["Volume"]);
                dgvOrderDetails.Rows.Add((dgvOrderDetails.Rows.Count + 1), row["ID"], row["ItemName"], row["UnitPrice"], row["Qty"], "(" + row["Unit"].ToString() + ")", double.Parse(row["Discount"].ToString()).ToString("0.00"), double.Parse(row["ItemTotal"].ToString()).ToString("0.00"), double.Parse(row["vat"].ToString()).ToString("0.00"), double.Parse(row["Discount"].ToString()).ToString("0.00"), double.Parse(row["vat"].ToString()).ToString("0.00"), row["bUnit"], row["Volume"], row["image"]);
                Array.Resize(ref selectedItemsId, selectedItemsId.Length + 1);
                selectedItemsId[selectedItemsId.Length - 1] = int.Parse(row["ID"].ToString());

                Array.Resize(ref oUnits, selectedItemsId.Length + 1);   // item count wise unit count even duplicate unit
                oUnits[selectedItemsId.Length - 1] = row["Unit"].ToString();
            }
        }

        private void dgvOrderList_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgvOrderList.Rows.Count == 0) return;

            if (dgvOrderList.Rows.Count > 0)
            {
                if (dgvOrderList.CurrentRow.Cells[1].Value == null) return;
                else if (dgvOrderList.CurrentRow.Cells[1].Value.ToString() != "")
                {
                    customerSpecific = false;
                    orderListPane.Visible = false;
                    ResetOrderScreen();

                    GetOrder(dgvOrderList.CurrentRow.Cells[2].Value.ToString());
                }
            }
            else return;            
            cmbProductDropDown.Focus();
        }

        private void btnQNext_Click(object sender, EventArgs e)
        {
            int baseOrderId = 0;
            /// Direction: Next
            string oInvoiceNo = "";

            if (isOrder && orderId > 0 && dgvInvoiceHead.Rows[5].Cells[1].Value.ToString().ToUpper() == "OPEN")
            {
                baseOrderId = orderId;
                oInvoiceNo = dgvInvoiceHead.Rows[0].Cells[1].Value.ToString();
            }

            Conn = new OleDbConnection(ConnectionString);
            OleDbCommand cmd = new OleDbCommand(); cmd.Connection = Conn;
            var result = "";

            Conn.Open();

            try
            {
                if (oInvoiceNo != "")
                    cmd.CommandText = "SELECT order_invoice_no FROM orders WHERE cur_status = 'OPEN' AND ID > ANY (SELECT ID FROM orders WHERE order_invoice_no = '" + oInvoiceNo + "')";
                else
                    cmd.CommandText = "SELECT order_invoice_no FROM orders WHERE cur_status = 'OPEN' AND ID > ANY (SELECT ID FROM orders WHERE ID > " + baseOrderId + ")";
                result = (string)cmd.ExecuteScalar();
            }
            catch (Exception x) 
            { 
                MessageBox.Show("Error with next order invoice\n"+x.Message); 
            }

            Conn.Close();
            if (result!= null && result.ToString() != "")
                GetOrder(result.ToString());
            cmbProductDropDown.Focus();
            //MessageBox.Show(baseOrderId.ToString());
        }

        private void btnQPrev_Click(object sender, EventArgs e)
        {
            int baseOrderId = 0;
            /// Direction: Previous
            string oInvoiceNo = "";

            if (isOrder && orderId > 0 && dgvInvoiceHead.Rows[5].Cells[1].Value.ToString().ToUpper() == "OPEN")
            {
                baseOrderId = orderId;
                oInvoiceNo = dgvInvoiceHead.Rows[0].Cells[1].Value.ToString();
            }

            Conn = new OleDbConnection(ConnectionString);
            OleDbCommand cmd = new OleDbCommand(); cmd.Connection = Conn;
            var result = "";

            Conn.Open();

            try
            {
                if (oInvoiceNo != "")
                    cmd.CommandText = "SELECT order_invoice_no FROM orders WHERE cur_status = 'OPEN' AND ID < ANY (SELECT ID FROM orders WHERE order_invoice_no = '" + oInvoiceNo + "') ORDER BY ID DESC";
                else
                    cmd.CommandText = "SELECT order_invoice_no FROM orders WHERE cur_status = 'OPEN' AND ID < ANY (SELECT ID FROM orders WHERE ID < " + baseOrderId + ") ORDER BY ID DESC";
                result = (string)cmd.ExecuteScalar();
            }
            catch (Exception x)
            {
                MessageBox.Show("Error with next order invoice\n" + x.Message);
            }

            Conn.Close();
            if (result != null && result.ToString() != "")
                GetOrder(result.ToString());
            cmbProductDropDown.Focus();
            //MessageBox.Show(baseOrderId.ToString());
        }

        private void txtInvoiceNo_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    CancelApplyingDiscount();
                    break;
            }
        }

        private void RemoveItem()
        {
            ///////////////////////////// UI PART ////////////////////////////////
            ///
            int productId = 0;

            if (dgvOrderDetails.Rows.Count > 0 && selectedItemsId.Contains(int.Parse(dgvOrderDetails.SelectedRows[0].Cells[1].Value.ToString())))
            {
                productId = int.Parse(dgvOrderDetails.SelectedRows[0].Cells[1].Value.ToString());
                
                List<int> list = selectedItemsId.ToList();
                list.Remove(int.Parse(dgvOrderDetails.SelectedRows[0].Cells[1].Value.ToString()));
                selectedItemsId = list.ToArray();
                dgvOrderDetails.Rows.Remove(dgvOrderDetails.SelectedRows[0]);
                UpdateOrderSummary(0);
            }

            for (int i = 0; i < dgvOrderDetails.Rows.Count; i++)
            {
                dgvOrderDetails.Rows[i].Cells[0].Value = (i + 1);
            }

            //empty calculation
            txtCalcInput.Text = "0.00";

            cmbProductDropDown.SelectAll(); cmbProductDropDown.Focus();

            ////////////////////////////////////////////////////////////////
            /// DATABASE PART

            if (!isOrder || productId == 0) return;

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

           
            Query = " DELETE FROM ordered_items ";
            Query += " WHERE order_id = " + orderId.ToString() + " AND product_id = " + productId.ToString();

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

            if (isOrder)
            {
                if (order.Count == 0)
                {
                    order.Add("order_id", orderId.ToString());
                    order.Add("sub_total", double.Parse(dgvOrderSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00"));
                    string vat = dgvOrderDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    order.Add("vat", vat);
                    order.Add("discount", double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00"));
                    order.Add("num_items", dgvOrderDetails.Rows.Count.ToString());
                    order.Add("grand_total", double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00"));
                    order.Add("payment", double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()).ToString("0.00"));
                    order.Add("changes", double.Parse(dgvOrderSummary.Rows[4].Cells[3].Value.ToString()).ToString("0.00"));
                    order.Add("due", double.Parse(dgvOrderSummary.Rows[5].Cells[3].Value.ToString()).ToString("0.00"));
                }
                else
                {
                    order["sub_total"] = double.Parse(dgvOrderSummary.Rows[0].Cells[3].Value.ToString()).ToString("0.00");
                    string vat = dgvOrderDetails.Rows.Cast<DataGridViewRow>()
                                   .AsEnumerable()
                                   .Sum(x => double.Parse(x.Cells[10].Value.ToString()))
                                   .ToString("0.00");

                    order["vat"] = vat;
                    order["discount"] = double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString()).ToString("0.00");
                    order["num_items"] = dgvOrderDetails.Rows.Count.ToString();
                    order["grand_total"] = double.Parse(dgvOrderSummary.Rows[2].Cells[3].Value.ToString()).ToString("0.00");
                    order["payment"] = double.Parse(dgvOrderSummary.Rows[3].Cells[3].Value.ToString()).ToString("0.00");
                    order["changes"] = double.Parse(dgvOrderSummary.Rows[4].Cells[3].Value.ToString()).ToString("0.00");
                    order["due"] = double.Parse(dgvOrderSummary.Rows[5].Cells[3].Value.ToString()).ToString("0.00");
                }

                UpdateOrder(order);
            }
            ////////////

        }

        private void txtInvoiceNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == 13)
            {
                switch (txtCouponDiscount.RightToLeft)
                {
                    // You are updating any numeric value
                    case RightToLeft.No:
                        dgvOrderSummary.Rows[1].Cells[3].Value = txtCalcInput.Text = txtCouponDiscount.Text;
                        dgvInvoiceHead.Rows[2].Cells[1].Value = txtCouponDiscount.Text;
                        dgvOrderSummary.Rows[2].Selected = true;
                        break;

                    // You are updating any text, e.g.: invoice
                    case RightToLeft.Yes:
                        ///////////////////////////////////////////////
                        txtCouponDiscount.RightToLeft = RightToLeft.No;    // DECIDE/ RESET WITH THIS // NO GLOBAL VARIABLE
                        txtInvoiceNo.Visible = false;
                        rdoAmount.Visible = rdoPercent.Visible = true;
                        txtCouponDiscount.Top = gbCouponDiscount.Height / 2 + rdoAmount.Top + rdoAmount.Height + 20;
                        btnApplyCoupon.Top = txtCouponDiscount.Top + txtCouponDiscount.Height + 20;
                        ///////////////////////////////////////////////
                        ///
                        // Get order
                        string invoiceNo = txtInvoiceNo.Text;
                        ShowOrderListPane();
                        rdoQueue.Checked = rdoPaid.Checked = rdoDue.Checked = false;
                        rdoQueue.Enabled = rdoPaid.Enabled = rdoDue.Enabled = false;                        
                        gbOrderList.Text = "ORDERS";
                        OrderListFilterByInvoice(invoiceNo);

                        break;

                }

                gbCouponDiscount.Visible = false;
                lPane.Enabled = true;
                rPane.Enabled = true;

                // exit - close session enabled
                btnCloseOrderScr.Enabled = true;
                btnCloseSession.Enabled = true;

                txtCouponDiscount.Text = "";
                cmbProductDropDown.Focus();
            }
        }

        private void btnAddDiscountOnGT_Click(object sender, EventArgs e)
        {
            oldDiscount = double.Parse(dgvOrderSummary.Rows[1].Cells[3].Value.ToString());
            txtCouponDiscount.Text = "";
            txtCouponDiscount.Focus();
        }

        bool customerSpecific = false;
        private void OrderListFilter(object sender, EventArgs e)
        {
            RadioButton radio = new RadioButton();
            if (sender.GetType().Name == "RadioButton")
                radio = (RadioButton)sender;
            
            if (radio.Checked)
            {
                radio.ForeColor = SystemColors.ControlText;
                gbOrderList.Text = radio.Text.ToUpper();
            }
            else
            {
                radio.ForeColor = SystemColors.ControlLightLight;
            }

            dgvOrderList.Rows.Clear(); dgvOrderList.Refresh();

            string sql = " SELECT orders.ID AS ID, orders.order_invoice_no AS oInvoiceNo, orders.order_date_time AS oDateTime, customers.customer_name AS Customer, orders.sub_total AS Amount, orders.num_items AS Items, orders.discount, orders.vat, orders.cur_status AS Status FROM orders INNER JOIN customers ON customers.customer_id = orders.customer_id ";
            string clause = "";

            switch(radio.Text)
            {
                case "Orders (in Queue)":
                default:
                    clause = " WHERE orders.cur_status = 'OPEN'";                    
                    break;

                case "Orders (Paid)":
                    clause = " WHERE orders.cur_status = 'SAVED' AND orders.payment > 0 AND orders.payment > orders.due";
                    break;

                case "Orders (Due)":
                    clause = " WHERE orders.cur_status = 'SAVED' AND orders.payment > 0 AND orders.payment < orders.due";
                    break;
            }

            if (isCustomer && customerSpecific)
                clause += " AND orders.customer_id = " + customerId;

            try
            {
                if (Conn != null && Conn.State == ConnectionState.Closed)
                    Conn.Open();
                else
                {
                    if (Conn == null)
                    {
                        Conn = new OleDbConnection(ConnectionString);
                        Conn.Open();
                    }
                }

                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = Conn;

                cmd.CommandText = sql + clause + " ORDER BY orders.order_date_time DESC ";

                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                DataTable dt = new DataTable();

                try { da.Fill(dt); } catch (Exception x) { MessageBox.Show(x.Message); }

                if (isCustomer && dt.Rows.Count > 0) btnPreviousOrders.Text = "Customer Orders (" + (dt.Rows.Count > 10 ? "10+" : dt.Rows.Count.ToString()) + ")" ; 

                foreach (DataRow row in dt.Rows)
                {
                    int rowNumber = dgvOrderList.Rows.Count + 1;
                    dgvOrderList.Rows.Add(rowNumber, row["ID"], row["oInvoiceNo"], DateTime.Parse(row["oDateTime"].ToString()).ToString("dd/MM/yyyy h:mm tt"), row["Customer"], row["Amount"], row["Items"], row["discount"], row["vat"], row["Status"]);
                }

                Conn.Close();
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }

        }


        private void OrderListFilterByInvoice(string invoiceNo)
        {
            dgvOrderList.Rows.Clear(); dgvOrderList.Refresh();

            string sql = " SELECT orders.ID AS ID, orders.order_invoice_no AS oInvoiceNo, orders.order_date_time AS oDateTime, customers.customer_name AS Customer, orders.sub_total AS Amount, orders.num_items AS Items, orders.discount, orders.vat, orders.cur_status AS Status FROM orders INNER JOIN customers ON customers.customer_id = orders.customer_id ";
            string clause = " WHERE orders.cur_status = 'SAVED' AND orders.order_invoice_no LIKE '%" + invoiceNo + "%'";            

            // NO NEED
            //if (isCustomer && customerSpecific)
                //clause += " AND orders.customer_id = " + customerId;

            try
            {
                if (Conn != null && Conn.State == ConnectionState.Closed)
                    Conn.Open();
                else
                {
                    if (Conn == null)
                        Conn = new OleDbConnection(ConnectionString);
                    Conn.Open();
                }

                OleDbCommand cmd = new OleDbCommand();
                cmd.Connection = Conn;

                cmd.CommandText = sql + clause + " ORDER BY orders.order_date_time DESC ";

                OleDbDataAdapter da = new OleDbDataAdapter(cmd);
                DataTable dt = new DataTable();

                try { da.Fill(dt); } catch (Exception x) { MessageBox.Show(x.Message); }

                // NO NEED
                //if (isCustomer && dt.Rows.Count > 0) btnPreviousOrders.Text = "Customer Orders (" + (dt.Rows.Count > 10 ? "10+" : dt.Rows.Count.ToString()) + ")";

                foreach (DataRow row in dt.Rows)
                {
                    int rowNumber = dgvOrderList.Rows.Count + 1;
                    dgvOrderList.Rows.Add(rowNumber, row["ID"], row["oInvoiceNo"], DateTime.Parse(row["oDateTime"].ToString()).ToString("dd/MM/yyyy h:mm tt"), row["Customer"], row["Amount"], row["Items"], row["discount"], row["vat"], row["Status"]);
                }

                Conn.Close();
            }
            catch (Exception x)
            {
                MessageBox.Show(x.Message);
            }
        }

        ////////////////////////////////////////////////////////////////////
        #endregion

        ///////////////////////// END OF CLASS ///////////////////////// 
    }

    //////////////////////////// END OF NAMESPACE /////////////////////////////
}