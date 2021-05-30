namespace ForceDeflectionTester_StripDisc
{
    partial class ForceDeflectionPlot
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
            this.ShowLiveDataTimer = new System.Windows.Forms.Timer(this.components);
            this.RecordPlotTimer = new System.Windows.Forms.Timer(this.components);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ShowLiveDataTimer.Tick += new System.EventHandler(this.ShowLiveDataTimer_Tick);            
            this.RecordPlotTimer.Tick += new System.EventHandler(this.RecordPlotTimer_Tick);
        }

        #endregion

        private System.Windows.Forms.Timer ShowLiveDataTimer;
        private System.Windows.Forms.Timer RecordPlotTimer;
    }
}
