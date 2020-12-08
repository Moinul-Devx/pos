namespace IntensePoS
{
    partial class InventoryScr
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InventoryScr));
            this.dgvSyncedProducts = new System.Windows.Forms.DataGridView();
            this.cPane = new System.Windows.Forms.Panel();
            this.txtSqlQuery = new System.Windows.Forms.TextBox();
            this.flpCpControls = new System.Windows.Forms.FlowLayoutPanel();
            this.btnGetSyncedProducts = new System.Windows.Forms.Button();
            this.btnSyncPostResult = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSyncedProducts)).BeginInit();
            this.cPane.SuspendLayout();
            this.flpCpControls.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvSyncedProducts
            // 
            this.dgvSyncedProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSyncedProducts.Dock = System.Windows.Forms.DockStyle.Top;
            this.dgvSyncedProducts.Location = new System.Drawing.Point(0, 0);
            this.dgvSyncedProducts.Name = "dgvSyncedProducts";
            this.dgvSyncedProducts.Size = new System.Drawing.Size(952, 309);
            this.dgvSyncedProducts.TabIndex = 1;
            // 
            // cPane
            // 
            this.cPane.Controls.Add(this.flpCpControls);
            this.cPane.Controls.Add(this.txtSqlQuery);
            this.cPane.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cPane.Location = new System.Drawing.Point(0, 309);
            this.cPane.Name = "cPane";
            this.cPane.Size = new System.Drawing.Size(952, 373);
            this.cPane.TabIndex = 1;
            // 
            // txtSqlQuery
            // 
            this.txtSqlQuery.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtSqlQuery.Font = new System.Drawing.Font("Arial", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSqlQuery.Location = new System.Drawing.Point(0, 0);
            this.txtSqlQuery.Multiline = true;
            this.txtSqlQuery.Name = "txtSqlQuery";
            this.txtSqlQuery.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtSqlQuery.Size = new System.Drawing.Size(952, 301);
            this.txtSqlQuery.TabIndex = 2;
            // 
            // flpCpControls
            // 
            this.flpCpControls.Controls.Add(this.btnClear);
            this.flpCpControls.Controls.Add(this.btnGetSyncedProducts);
            this.flpCpControls.Controls.Add(this.btnSyncPostResult);
            this.flpCpControls.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpCpControls.Location = new System.Drawing.Point(0, 301);
            this.flpCpControls.Name = "flpCpControls";
            this.flpCpControls.Size = new System.Drawing.Size(952, 72);
            this.flpCpControls.TabIndex = 3;
            // 
            // btnGetSyncedProducts
            // 
            this.btnGetSyncedProducts.BackColor = System.Drawing.SystemColors.HotTrack;
            this.btnGetSyncedProducts.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGetSyncedProducts.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnGetSyncedProducts.Location = new System.Drawing.Point(116, 3);
            this.btnGetSyncedProducts.Name = "btnGetSyncedProducts";
            this.btnGetSyncedProducts.Size = new System.Drawing.Size(199, 61);
            this.btnGetSyncedProducts.TabIndex = 0;
            this.btnGetSyncedProducts.Text = "Sync &Products (GET)";
            this.btnGetSyncedProducts.UseVisualStyleBackColor = false;
            this.btnGetSyncedProducts.Click += new System.EventHandler(this.btnGetSyncedProducts_Click);
            // 
            // btnSyncPostResult
            // 
            this.btnSyncPostResult.BackColor = System.Drawing.SystemColors.HotTrack;
            this.btnSyncPostResult.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSyncPostResult.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnSyncPostResult.Location = new System.Drawing.Point(321, 3);
            this.btnSyncPostResult.Name = "btnSyncPostResult";
            this.btnSyncPostResult.Size = new System.Drawing.Size(236, 61);
            this.btnSyncPostResult.TabIndex = 1;
            this.btnSyncPostResult.Text = "Sync Post &Result (Product)";
            this.btnSyncPostResult.UseVisualStyleBackColor = false;
            this.btnSyncPostResult.Click += new System.EventHandler(this.btnSyncPostResult_Click);
            // 
            // btnClear
            // 
            this.btnClear.BackColor = System.Drawing.Color.Red;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClear.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnClear.Location = new System.Drawing.Point(3, 3);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(107, 61);
            this.btnClear.TabIndex = 2;
            this.btnClear.Text = "C&lear";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // InventoryScr
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(952, 682);
            this.Controls.Add(this.cPane);
            this.Controls.Add(this.dgvSyncedProducts);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "InventoryScr";
            this.Text = "PoS Testing Console (Developers & Testers)";
            this.Load += new System.EventHandler(this.InventoryScr_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvSyncedProducts)).EndInit();
            this.cPane.ResumeLayout(false);
            this.cPane.PerformLayout();
            this.flpCpControls.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvSyncedProducts;
        private System.Windows.Forms.Panel cPane;
        private System.Windows.Forms.FlowLayoutPanel flpCpControls;
        private System.Windows.Forms.Button btnGetSyncedProducts;
        private System.Windows.Forms.Button btnSyncPostResult;
        private System.Windows.Forms.TextBox txtSqlQuery;
        private System.Windows.Forms.Button btnClear;
    }
}