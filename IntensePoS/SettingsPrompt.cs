using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IntensePoS.Lib;

namespace IntensePoS
{
    public partial class SettingsPrompt : Form
    {

        private int prompt = 0;
        private bool keyVerified = false;

        public SettingsPrompt()
        {
            InitializeComponent();
        }

        public SettingsPrompt(int prompt, bool keyVerified)
        {
            this.prompt = prompt;
            this.keyVerified = keyVerified;
            InitializeComponent();
        }

        private void SettingsPrompt_Load(object sender, EventArgs e)
        {
            LoadPrompt();
        }

        private void LoadPrompt()
        {
            switch(this.prompt)
            {
                case 0:
                    LoadPromptAuthKey();
                    break;
            }
        }

        private void LoadPromptAuthKey()
        {            
            authKeyPane.Visible = true;
            txtAuthKey.TabIndex = 0;
        }

        private void SettingsPrompt_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = false;
            Application.Exit();
        }

        private void btnSyncInventoryGET_Click(object sender, EventArgs e)
        {
            btnVerifyAuthKey.Text = "Verifying...";
            if (!keyVerified)
                VerifyAuthKey();
        }

        private void VerifyAuthKey()
        {
            string authKey = txtAuthKey.Text.Trim();
            string msg = PoSConfig.VerifyAuthKey(authKey);
            keyVerified = true;

            MessageBox.Show(msg + "\n\nNote: You may need to restart the PoS terminal.", this.Text);

            /*
            OrderScr oScr = new OrderScr();
            oScr.Show();

            Form oForm = Application.OpenForms["OrderScr"];
            if(oForm != null) oForm.Close();
            */

            this.SendToBack();
            /*
            foreach (Form form in Application.OpenForms)
            {
                form.Close();
            }
            */
            Application.Exit();
        }
    }
}
