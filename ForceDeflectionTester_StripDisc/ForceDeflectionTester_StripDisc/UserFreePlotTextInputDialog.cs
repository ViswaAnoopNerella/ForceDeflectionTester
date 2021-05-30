using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ForceDeflectionTester_StripDisc
{
    public partial class UserFreePlotTextInputDialog : Form
    {
        public UserFreePlotTextInputDialog()
        {
            InitializeComponent();
        }

        private Font userFont;

        private void buttonFont_Click(object sender, EventArgs e)
        {
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                this.userFont = fontDialog.Font;
            }

        }

        public Font UserSelectedFont
        {
            get
            {
                return userFont;
            }

        }

        public String UserEnteredText
        {
            get
            {
                return this.InputTextBox.Text;
            }
            set
            {
                this.InputTextBox.Text = value;
            }

        }

        public Color UserSelectedColor
        {
            get;
            set;
        }

            


        private void UserFreePlotTextInputDialog_Load(object sender, EventArgs e)
        {
            userFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
            this.UserSelectedColor = Color.Black;
            buttonColor.ForeColor = this.UserSelectedColor;
        }

        private void buttonColor_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                this.UserSelectedColor = colorDialog.Color;
                buttonColor.ForeColor = this.UserSelectedColor;
            }
            else
            {
                this.UserSelectedColor = Color.Black;
                buttonColor.ForeColor = this.UserSelectedColor;
            }

        }
    }
}
