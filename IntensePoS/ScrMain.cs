using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace IntensePoS
{
    public partial class ScrMain : Form
    {
        public ScrMain()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ScrMain_Load(object sender, EventArgs e)
        {
            // Close button settings
            btnClose.Top = this.Height - (btnClose.Height + 30);
            btnClose.Left = this.Width - (btnClose.Width + 30);

            // Category panel settings
            catPane.Width = this.Width;
            //catPane.BackColor = Color.BlanchedAlmond;
            catPane.Top = 0;
            catPane.Left = 0;
            
            /*
            int j = 0; 
            int k = 0;
            int hMargin;
            int vMargin = 66;
            for (int i = 0; i < categories.Length; i++)
            {
                if (k >= 7)
                {
                    j++; k = 0;
                    hMargin = 0;
                }
                else
                    hMargin = 204;

                int width = 204;
                int height = 66;
                CreateCategoryButton(categories[i].ToString(), height, width, hMargin * k + 10, vMargin * j + 10);
                k++;
            }
            */

            dataGridView1.ColumnCount = 3;
            dataGridView1.Columns[0].Name = "Product ID";
            dataGridView1.Columns[1].Name = "Product Name";
            dataGridView1.Columns[2].Name = "Product Price";

            string[] row = new string[] { "1", "Product 1", "1000" };
            dataGridView1.Rows.Add(row);
            row = new string[] { "2", "Product 2", "2000" };
            dataGridView1.Rows.Add(row);
            row = new string[] { "3", "Product 3", "3000" };
            dataGridView1.Rows.Add(row);
            row = new string[] { "4", "Product 4", "4000" };
            dataGridView1.Rows.Add(row);

            DataGridViewButtonColumn btn = new DataGridViewButtonColumn();
            dataGridView1.Columns.Add(btn);
            btn.HeaderText = "Click Data";
            btn.Text = "Click Here";
            btn.Name = "btn";
            btn.UseColumnTextForButtonValue = true;

        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            MessageBox.Show("");

            if (e.ColumnIndex == 3)
            {
                MessageBox.Show((e.RowIndex + 1) + "  Row  " + (e.ColumnIndex + 1) + "  Column button clicked ");
            }
        }

        #region categories

        string[] categories = { "Groceries", "Fish", "Meat", "Vegetables", "Shirt", "Pant", "Perfumes", "Leather Products", "Toiletries", "Toys", "Dairy Products", "Cars", "Bikes", "Fruits", "Soft Drinks", "Cat A", "Cat B", "Cat D"};

        private void CreateCategoryButton (string caption, int height, int width, int x, int y, string btnType = null)
        {
            // Create a Button object 
            Button catBtn = new Button();

            // Set Button properties
            catBtn.Location = new Point(x, y);
            catBtn.Height = height;
            catBtn.Width = width;
            catBtn.FlatAppearance.BorderSize = 5;
            catBtn.FlatAppearance.BorderColor = Color.White;
            catBtn.FlatStyle = FlatStyle.Flat;

            // Set background and foreground
            if (btnType == null)
            {
                catBtn.BackColor = Color.Purple;
                catBtn.ForeColor = Color.Red;
            }
            else
            {
                catBtn.BackColor = Color.GhostWhite;
                catBtn.ForeColor = Color.Black;
            }

            catBtn.Text = caption;
            catBtn.Name = "catBtn";
            catBtn.Font = new Font("Arial", 16, FontStyle.Regular);
            catBtn.ForeColor = SystemColors.ButtonFace;

            // Add a Button Click Event handler
            catBtn.Click += new EventHandler(CatBtn_Click);
            this.Controls["catPane"].Controls.Add(catBtn);
        }

        private void CatBtn_Click(object sender, EventArgs e)
        {
            MessageBox.Show(((Button)sender).Text);
        }

        #endregion


        #region Utility Methods
        public static void GetTerminals()
        {
            byte[] data = new byte[1024];
            string input, stringData;
            TcpClient server;

            try
            {
                server = new TcpClient("127.0.0.1", 9050);
            }
            catch (SocketException)
            {
                Console.WriteLine("Unable to connect to server");
                return;
            }
            NetworkStream ns = server.GetStream();

            int recv = ns.Read(data, 0, data.Length);
            stringData = Encoding.ASCII.GetString(data, 0, recv);
            Console.WriteLine(stringData);

            while (true)
            {
                input = Console.ReadLine();
                if (input == "exit")
                    break;
                ns.Write(Encoding.ASCII.GetBytes(input), 0, input.Length);
                ns.Flush();

                data = new byte[1024];
                recv = ns.Read(data, 0, data.Length);
                stringData = Encoding.ASCII.GetString(data, 0, recv);
                Console.WriteLine(stringData);
            }
            Console.WriteLine("Disconnecting from server...");
            ns.Close();
            server.Close();
        }
        #endregion

        private void btnNextCat_Click(object sender, EventArgs e)
        {
            GetTerminals();
        }
    }
}
