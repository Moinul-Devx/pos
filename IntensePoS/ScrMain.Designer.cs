namespace IntensePoS
{
    partial class ScrMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnClose = new System.Windows.Forms.Button();
            this.catPane = new System.Windows.Forms.Panel();
            this.btnNextCat = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.catPane.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Calibri", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClose.Location = new System.Drawing.Point(1237, 769);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(212, 56);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "E&xit";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // catPane
            // 
            this.catPane.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.catPane.Controls.Add(this.btnNextCat);
            this.catPane.Dock = System.Windows.Forms.DockStyle.Top;
            this.catPane.Location = new System.Drawing.Point(0, 0);
            this.catPane.Name = "catPane";
            this.catPane.Size = new System.Drawing.Size(1461, 252);
            this.catPane.TabIndex = 1;
            // 
            // btnNextCat
            // 
            this.btnNextCat.BackColor = System.Drawing.Color.GhostWhite;
            this.btnNextCat.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnNextCat.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNextCat.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNextCat.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.btnNextCat.Location = new System.Drawing.Point(542, 144);
            this.btnNextCat.Name = "btnNextCat";
            this.btnNextCat.Size = new System.Drawing.Size(300, 70);
            this.btnNextCat.TabIndex = 0;
            this.btnNextCat.Text = "Next";
            this.btnNextCat.UseVisualStyleBackColor = false;
            this.btnNextCat.Click += new System.EventHandler(this.btnNextCat_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Left;
            this.dataGridView1.Location = new System.Drawing.Point(0, 252);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(1394, 585);
            this.dataGridView1.TabIndex = 2;
            // 
            // ScrMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(1461, 837);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.catPane);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MinimizeBox = false;
            this.Name = "ScrMain";
            this.Text = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.ScrMain_Load);
            this.catPane.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Panel catPane;
        private System.Windows.Forms.Button btnNextCat;
        private System.Windows.Forms.DataGridView dataGridView1;
    }
}

