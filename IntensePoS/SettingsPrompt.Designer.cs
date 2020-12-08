namespace IntensePoS
{
    partial class SettingsPrompt
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsPrompt));
            this.authKeyPane = new System.Windows.Forms.Panel();
            this.tLayoutPaneAuthKey = new System.Windows.Forms.TableLayoutPanel();
            this.lblStatusSyncLoginPOST = new System.Windows.Forms.Label();
            this.btnVerifyAuthKey = new System.Windows.Forms.Button();
            this.txtAuthKey = new System.Windows.Forms.TextBox();
            this.authKeyPane.SuspendLayout();
            this.tLayoutPaneAuthKey.SuspendLayout();
            this.SuspendLayout();
            // 
            // authKeyPane
            // 
            this.authKeyPane.Controls.Add(this.tLayoutPaneAuthKey);
            this.authKeyPane.Dock = System.Windows.Forms.DockStyle.Fill;
            this.authKeyPane.Location = new System.Drawing.Point(0, 0);
            this.authKeyPane.Name = "authKeyPane";
            this.authKeyPane.Size = new System.Drawing.Size(800, 91);
            this.authKeyPane.TabIndex = 1;
            this.authKeyPane.Visible = false;
            // 
            // tLayoutPaneAuthKey
            // 
            this.tLayoutPaneAuthKey.ColumnCount = 2;
            this.tLayoutPaneAuthKey.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 68.5F));
            this.tLayoutPaneAuthKey.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 31.5F));
            this.tLayoutPaneAuthKey.Controls.Add(this.txtAuthKey, 0, 1);
            this.tLayoutPaneAuthKey.Controls.Add(this.btnVerifyAuthKey, 0, 1);
            this.tLayoutPaneAuthKey.Controls.Add(this.lblStatusSyncLoginPOST, 0, 0);
            this.tLayoutPaneAuthKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tLayoutPaneAuthKey.Location = new System.Drawing.Point(0, 0);
            this.tLayoutPaneAuthKey.Name = "tLayoutPaneAuthKey";
            this.tLayoutPaneAuthKey.RowCount = 2;
            this.tLayoutPaneAuthKey.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tLayoutPaneAuthKey.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tLayoutPaneAuthKey.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tLayoutPaneAuthKey.Size = new System.Drawing.Size(800, 91);
            this.tLayoutPaneAuthKey.TabIndex = 0;
            // 
            // lblStatusSyncLoginPOST
            // 
            this.lblStatusSyncLoginPOST.AutoSize = true;
            this.tLayoutPaneAuthKey.SetColumnSpan(this.lblStatusSyncLoginPOST, 2);
            this.lblStatusSyncLoginPOST.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatusSyncLoginPOST.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatusSyncLoginPOST.Location = new System.Drawing.Point(3, 0);
            this.lblStatusSyncLoginPOST.Name = "lblStatusSyncLoginPOST";
            this.lblStatusSyncLoginPOST.Size = new System.Drawing.Size(794, 27);
            this.lblStatusSyncLoginPOST.TabIndex = 40;
            this.lblStatusSyncLoginPOST.Text = "Please enter Verification Key";
            this.lblStatusSyncLoginPOST.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnVerifyAuthKey
            // 
            this.btnVerifyAuthKey.BackColor = System.Drawing.Color.Red;
            this.btnVerifyAuthKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnVerifyAuthKey.FlatAppearance.BorderSize = 0;
            this.btnVerifyAuthKey.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnVerifyAuthKey.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnVerifyAuthKey.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnVerifyAuthKey.Location = new System.Drawing.Point(551, 30);
            this.btnVerifyAuthKey.Name = "btnVerifyAuthKey";
            this.btnVerifyAuthKey.Size = new System.Drawing.Size(246, 58);
            this.btnVerifyAuthKey.TabIndex = 42;
            this.btnVerifyAuthKey.Text = "Verify Auth Key";
            this.btnVerifyAuthKey.UseVisualStyleBackColor = false;
            this.btnVerifyAuthKey.Click += new System.EventHandler(this.btnSyncInventoryGET_Click);
            // 
            // txtAuthKey
            // 
            this.txtAuthKey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtAuthKey.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAuthKey.Location = new System.Drawing.Point(3, 30);
            this.txtAuthKey.Multiline = true;
            this.txtAuthKey.Name = "txtAuthKey";
            this.txtAuthKey.Size = new System.Drawing.Size(542, 58);
            this.txtAuthKey.TabIndex = 43;
            this.txtAuthKey.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // SettingsPrompt
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 91);
            this.Controls.Add(this.authKeyPane);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SettingsPrompt";
            this.Text = "Intense PoS 1.0 (Configurations)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingsPrompt_FormClosing);
            this.Load += new System.EventHandler(this.SettingsPrompt_Load);
            this.authKeyPane.ResumeLayout(false);
            this.tLayoutPaneAuthKey.ResumeLayout(false);
            this.tLayoutPaneAuthKey.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel authKeyPane;
        private System.Windows.Forms.TableLayoutPanel tLayoutPaneAuthKey;
        private System.Windows.Forms.TextBox txtAuthKey;
        private System.Windows.Forms.Button btnVerifyAuthKey;
        private System.Windows.Forms.Label lblStatusSyncLoginPOST;
    }
}