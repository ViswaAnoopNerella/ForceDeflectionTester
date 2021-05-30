namespace ForceDeflectionTester_StripDisc
{
    partial class UserControlRecordTimer
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.UserControlTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();

            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.UserControlTimer.Enabled = false;
            this.UserControlTimer.Tick += new System.EventHandler(this.timer1_Tick);
            
            this.ResumeLayout();
        }

        #endregion
        private System.Windows.Forms.Timer UserControlTimer;
    }
}
