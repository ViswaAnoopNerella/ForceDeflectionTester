using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ForceDeflectionTester_StripDisc
{
    public partial class MainForm : Form
    {
        private TemperatureBath[] baths = new TemperatureBath[FDTesterPersistentData.NumbBaths];

        public MainForm()
        {
            InitializeComponent();
        }

        private void ForceDeflectionPlotControl_Load(object sender, EventArgs e)
        {
            //ForceDeflectionPlotControl.InitializeChart();
        }

        private void MainFormTimer_Tick(object sender, EventArgs e)
        {
            if (radioSelectVolts.Checked)
            {
                LVDT_Value_Label.Text = FDTesterSingleton.Instance.PlotData.livePlotData.DisplacementData[(int)PlotPersistentData.ReadingType.enRawVolts].ToString();
                LoadCell_Value_Label.Text = FDTesterSingleton.Instance.PlotData.livePlotData.ForceData[(int)PlotPersistentData.ReadingType.enRawVolts].ToString();
                labelLVDTScaleUnits.Visible = false;
                labelLoadCellScaleUnits.Visible = false;
            }
            else 
            {
                LVDT_Value_Label.Text = FDTesterSingleton.Instance.PlotData.livePlotData.DisplacementData[(int)PlotPersistentData.ReadingType.enScaled].ToString();
                LoadCell_Value_Label.Text = FDTesterSingleton.Instance.PlotData.livePlotData.ForceData[(int)PlotPersistentData.ReadingType.enScaled].ToString();
                labelLVDTScaleUnits.Visible = true;
                labelLoadCellScaleUnits.Visible = true;
            
            }
            timerBathTempUpdate.Enabled = true; // update the temperatures every period
            string[] PVArray = { ProcessValueBath1.Text, ProcessValueBath2.Text, ProcessValueBath3.Text, ProcessValueBath4.Text, ProcessValueBath5.Text };
            string[] SetpointArray = { SetpointBath1.Text, SetpointBath2.Text, SetpointBath3.Text, SetpointBath4.Text, SetpointBath5.Text };
            Panel[] PanelArray = { BathPanel1, BathPanel2, BathPanel3, BathPanel4, BathPanel5 };
            for (int i=0; i<FDTesterPersistentData.NumbBaths; i++)
            {
                
                PVArray[i] = FDTesterSingleton.Instance.PlotData.BathsData[i].CurrentReading.ToString("F1") + " C";
                SetpointArray[i] = FDTesterSingleton.Instance.PlotData.BathsData[i].SetpointRead.ToString("F1") + " C";
                if( (FDTesterSingleton.Instance.PlotData.BathsData[i].SetpointRead - FDTesterSingleton.Instance.PlotData.BathsData[i].CurrentReading) > FDTesterSingleton.Instance.PlotData.BathsData[i].SetpointTolerance)
                {
                    PanelArray[i].BackColor = Color.OrangeRed;
                }
                else if ((FDTesterSingleton.Instance.PlotData.BathsData[i].SetpointRead - FDTesterSingleton.Instance.PlotData.BathsData[i].CurrentReading) < -1* FDTesterSingleton.Instance.PlotData.BathsData[i].SetpointTolerance)
                {
                    PanelArray[i].BackColor = Color.Blue;
                }
                else
                {
                    PanelArray[i].BackColor = Color.Green;
                }
                if (FDTesterSingleton.Instance.PlotData.BathsData[i].LastCommStatus == TemperatureBathPersistentData.LastSerialCommStatus.enCommFail)
                {
                    PanelArray[i].BackColor = Color.Red;
                }
                PanelArray[i].Visible = true;
                PVLabel.Visible = true;
                SetpointLabel.Visible = true;

            }
            //Need to set the right controls
            ProcessValueBath1.Text = PVArray[0];
            ProcessValueBath2.Text = PVArray[1];
            ProcessValueBath3.Text = PVArray[2];
            ProcessValueBath4.Text = PVArray[3];
            ProcessValueBath5.Text = PVArray[4];

            SetpointBath1.Text = SetpointArray[0];
            SetpointBath2.Text = SetpointArray[1];
            SetpointBath3.Text = SetpointArray[2];
            SetpointBath4.Text = SetpointArray[3];
            SetpointBath5.Text = SetpointArray[4];

            if (userControlRecordTimer1.StartStopStateValue == false)
            {
                userControlRecordTimer1.Visible = false;
                NewPlotButton.Visible = true;
                ZeroButton.Visible = true;
                InsertXYPointButton.Visible = true;
                FreeTextButton.Visible = true;
                SelectTextObjectButton.Visible = true;
                MoveTextButton.Visible = true;
                TestStampButton.Visible = true; //done automatically when test starts
                SaveTestButton.Visible = true;
                TestPrintButton.Visible = true;
                LiveButton.Visible = true;
                TestEstablishZeroPoint.Visible = false;// true; //now done through menu 
                toolStripButtonDelete.Visible = true;
                     

            }
            else
            {
                
                userControlRecordTimer1.Visible = true;
                NewPlotButton.Visible = false;
                ZeroButton.Visible = false;
                InsertXYPointButton.Visible = false;
                FreeTextButton.Visible = false;
                SelectTextObjectButton.Visible = false;
                MoveTextButton.Visible = false;
                TestStampButton.Visible = false; //done automatically when test starts
                SaveTestButton.Visible = false;
                TestPrintButton.Visible = false;
                LiveButton.Visible = false;
                TestEstablishZeroPoint.Visible = false;
                toolStripButtonDelete.Visible = false;


            }
            
            
        }

        private void userControlRecordTimer1_Load(object sender, EventArgs e)
        {
            userControlRecordTimer1.refPlot = ForceDeflectionPlotControl;
            userControlRecordTimer1.InitializeGraph();
        }

        private void InsertXYPointButton_Click(object sender, EventArgs e)
        {
            ForceDeflectionPlotControl.EnableDataPointDraw();
        }

        private void NewPlotButton_Click(object sender, EventArgs e)
        {
            String UserText = "25.0";
            Font UserFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
            UserFreePlotTextInputDialog textDlg = new UserFreePlotTextInputDialog();
            textDlg.buttonFont.Visible = false;
            textDlg.labelUserInstruct.Text = "Enter Temperature Below";
            textDlg.UserEnteredText = "25.0";
            if (textDlg.ShowDialog() == DialogResult.OK)
            {
                UserText = textDlg.UserEnteredText;
                userControlRecordTimer1.NextPlotcolor = textDlg.UserSelectedColor;
                FDTesterSingleton.Instance.PlotData.TestData.currentPlotNumber++;
                if (FDTesterSingleton.Instance.PlotData.TestData.currentPlotNumber >= TestPersistentData.MaxPlotsPerTest)
                {
                    FDTesterSingleton.Instance.PlotData.TestData.currentPlotNumber = 0;
                }

                FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData[FDTesterSingleton.Instance.PlotData.TestData.currentPlotNumber].Temperature = double.Parse(UserText);
                userControlRecordTimer1.StartStopStateValue = true;
                ForceDeflectionPlotControl.LiveDataMode(true); // show values in live mode
            }
        }

        private void FreeTextButton_Click(object sender, EventArgs e)
        {
            String UserText="";
            Font UserFont = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
            UserFreePlotTextInputDialog textDlg = new UserFreePlotTextInputDialog();
            textDlg.buttonColor.Visible = false;
            if (textDlg.ShowDialog() == DialogResult.OK)
            {
                UserFont = textDlg.UserSelectedFont;
                UserText = textDlg.UserEnteredText;
            }
            ForceDeflectionPlotControl.AddTextToChart(UserText, UserFont, 0);
        }

        private void ZeroButton_Click(object sender, EventArgs e)
        {
            ForceDeflectionPlotControl.EnablePlotForceCoordinateZero();
        }

        private void SaveTestButton_Click(object sender, EventArgs e)
        {
            SaveTestResults();
            ForceDeflectionPlotControl.PrintSaveAsFile();

           

        }

        private static void SaveTestResults()
        {
            // Write sample data to CSV file

            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            //saveFileDialog1.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            saveFileDialog1.Filter = "csv files (*.csv)|*.csv";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            int NumbPlotscomplete = Math.Min(FDTesterSingleton.Instance.PlotData.TestData.numberOfPlotRecordingCompleted, TestPersistentData.MaxPlotsPerTest);
            PlotPersistentData[] plots = FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData;
            TestPersistentData theTest = FDTesterSingleton.Instance.PlotData.TestData;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    // Code to write the stream goes here.
                    using (ReadWriteCsv.CsvFileWriter writer = new ReadWriteCsv.CsvFileWriter(myStream))
                    {
                        ReadWriteCsv.CsvRow rowHeading = new ReadWriteCsv.CsvRow();
                        rowHeading.Add(String.Format("Test Name {0}",theTest.TestName.Value));
                        rowHeading.Add(String.Format("Test Number {0}",theTest.TestNumber.Value));
                        rowHeading.Add(String.Format("Test Data {0}",theTest.TestEndTime.ToShortDateString()));
                        rowHeading.Add(String.Format("Product {0}", theTest.ProductName.Value));
                        writer.WriteRow(rowHeading);
                        for (int i = 1; i <= NumbPlotscomplete; i++)
                        {
                            ReadWriteCsv.CsvRow row = new ReadWriteCsv.CsvRow();
                            row.Add(String.Format("Temperature: {0} C", plots[i].Temperature.ToString("F")));                                                        
                            row.Add(String.Format("Force Offset: {0} g", plots[i].ForceOffset.ToString("F")));
                            writer.WriteRow(row);
                            row = new ReadWriteCsv.CsvRow();
                            row.Add(String.Format("\n"));
                            writer.WriteRow(row);
                            row = new ReadWriteCsv.CsvRow();
                            row.Add(String.Format("Force , Deflection"));
                            writer.WriteRow(row);

                            for (int j = 0; j < plots[i].NumberOfRecordedDataPoints; j++)
                            {
                                ReadWriteCsv.CsvRow rowplot = new ReadWriteCsv.CsvRow();
                                rowplot.Add(String.Format("{0}", plots[i].ForceData[j].ToString("F")));
                                rowplot.Add(String.Format("{0}", plots[i].DisplacementData[j].ToString("F")));
                                writer.WriteRow(rowplot);
                            }
                        }
                    }
                    myStream.Close();
                }
            }

            
        }        

        private void TestPrintButton_Click(object sender, EventArgs e)
        {

            ForceDeflectionPlotControl.PrintPage(sender,e);
		

        }

        private void TestStampButton_Click(object sender, EventArgs e)
        {
            String UserText = "";
            Font UserFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
            double XAxisPos = FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin + (FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax - FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin) * 0.8;
            double YAxisPos = FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax;// +() * 0.2;
            double YAxisIncrementValue = (FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax - FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin) / 35;
            double XScaledUnits = (FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax - FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin) / 100; //100 tick marks 
            double YScaledUnits = (FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax - FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin) / 100; //100 tick marks 

            FDTesterSingleton.Instance.PlotData.TestData.XScale.Value = XScaledUnits.ToString("F") + " mils/div";
            FDTesterSingleton.Instance.PlotData.TestData.YScale.Value = YScaledUnits.ToString("F") + " grams/div";

            foreach (TestStampElement teststamp in FDTesterSingleton.Instance.PlotData.TestData.testStampList)
            {
                YAxisPos -= YAxisIncrementValue;
                UserText = teststamp.Name + ":  "+ teststamp.Value;
                ForceDeflectionPlotControl.AddTextToChart(UserText, UserFont, 0, true,XAxisPos,YAxisPos);
                
            }
                
        }

        private void MoveTextButton_Click(object sender, EventArgs e)
        {
            ForceDeflectionPlotControl.EnableUserMoveText();                
        }

        private void TextDownArrowButton_Click(object sender, EventArgs e)
        {

        }

        private void TextUpArrowButton_Click(object sender, EventArgs e)
        {

        }

        private void SelectTextObjectButton_Click(object sender, EventArgs e)
        {

        }

        private void LiveButton_Click(object sender, EventArgs e)
        {
            ForceDeflectionPlotControl.ToggleLiveMode();

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            radioSelectScaled.Checked = true;
            for (int i = 0; i < FDTesterPersistentData.NumbBaths; i++)
            {
                baths[i] = new TemperatureBath(i);
                baths[i].RunAsynchSerialIOThread();
            }
            MainFormTimer.Enabled = true;
            userControlRecordTimer1.Visible = false;
       //     FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisZeroValueScaled = 0.0;
       //     FDTesterSingleton.Instance.PlotData.TestData.ForceAxisZeroValueScaled = 0.0; // clear out the scaling values first.                
            //ForceDeflectionPlotControl.LiveDataMode(true);
        }

        private void startNewTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
             const string message = "Are you sure that you would start a new test. All plots will be cleared?";
            const string caption = "Starting a new test choice";
                    var result = MessageBox.Show(message, caption,MessageBoxButtons.YesNo,MessageBoxIcon.Question);

                    // If the no button was pressed ...
                    if (result == DialogResult.No)
                    {
                        return;
                    }
                    else
                    {
                        FDTesterSingleton.Instance.PlotData.TestData.InitializeNewTest();
                        TestStartUserInput userTestInputDlg = new TestStartUserInput();
                        userTestInputDlg.textBoxName.Text = FDTesterSingleton.Instance.PlotData.TestData.TestName.Value;
                        userTestInputDlg.textBoxTestNumb.Text = FDTesterSingleton.Instance.PlotData.TestData.TestNumber.Value;
                        userTestInputDlg.textBoxLotNumber.Text = FDTesterSingleton.Instance.PlotData.TestData.LotNumber.Value;
                        userTestInputDlg.textBoxBiMetal.Text = FDTesterSingleton.Instance.PlotData.TestData.BiMetal.Value;
                        userTestInputDlg.textBoxHeatTreat.Text = FDTesterSingleton.Instance.PlotData.TestData.HeatTreat.Value;
                        userTestInputDlg.textBoxProduct.Text = FDTesterSingleton.Instance.PlotData.TestData.ProductName.Value;
                        userTestInputDlg.textBoxSampleNum.Text = FDTesterSingleton.Instance.PlotData.TestData.DiscNumber.Value;
                        userTestInputDlg.textBoxDiscTemps.Text = FDTesterSingleton.Instance.PlotData.TestData.DiscTemps.Value;
                        userTestInputDlg.textBoxProbe.Text = FDTesterSingleton.Instance.PlotData.TestData.ProbeNumber.Value;
                        userTestInputDlg.textBoxNotes.Text = FDTesterSingleton.Instance.PlotData.TestData.Notes.Value;

                        if (userTestInputDlg.ShowDialog() == DialogResult.OK)
                        {
                            FDTesterSingleton.Instance.PlotData.TestData.TestName.Value = userTestInputDlg.textBoxName.Text;
                            FDTesterSingleton.Instance.PlotData.TestData.TestDate.Value = FDTesterSingleton.Instance.PlotData.TestData.TestEndTime.ToShortDateString();
                            FDTesterSingleton.Instance.PlotData.TestData.TestNumber.Value =  userTestInputDlg.textBoxTestNumb.Text;
                            FDTesterSingleton.Instance.PlotData.TestData.LotNumber.Value =   userTestInputDlg.textBoxLotNumber.Text;
                            FDTesterSingleton.Instance.PlotData.TestData.BiMetal.Value = userTestInputDlg.textBoxBiMetal.Text;
                            FDTesterSingleton.Instance.PlotData.TestData.HeatTreat.Value = userTestInputDlg.textBoxHeatTreat.Text;
                            FDTesterSingleton.Instance.PlotData.TestData.ProductName.Value = userTestInputDlg.textBoxProduct.Text;
                            FDTesterSingleton.Instance.PlotData.TestData.DiscNumber.Value = userTestInputDlg.textBoxSampleNum.Text;
                            FDTesterSingleton.Instance.PlotData.TestData.DiscTemps.Value = userTestInputDlg.textBoxDiscTemps.Text;
                            FDTesterSingleton.Instance.PlotData.TestData.ProbeNumber.Value = userTestInputDlg.textBoxProbe.Text;
                            FDTesterSingleton.Instance.PlotData.TestData.Plotter.Value = "MFD1";
                            FDTesterSingleton.Instance.PlotData.TestData.Notes.Value = userTestInputDlg.textBoxNotes.Text;
                        }

                        ChartButtonsToolStrip.Visible = true;
                        EventArgs eventclick = new EventArgs();                       

                       ForceDeflectionPlotControl.ClearChartsForNewTest();
                       //TestStampButton_Click(this, eventclick);
                       establishZeroPointForTestToolStripMenuItem.Enabled = true;
                       
                        
                        



                    }            

        
        }

        private void calibrationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CalibrationInput calDlg = new CalibrationInput();            
            int currLVDT = FDTesterSingleton.Instance.PlotData.lvdtConfig.CurrentTransducerUsed;
            calDlg.textBoxLVDTOffset.Text = FDTesterSingleton.Instance.PlotData.lvdtConfig.CalibrationData[currLVDT].Offset.ToString("F");
            calDlg.textBoxLVDTScale.Text = FDTesterSingleton.Instance.PlotData.lvdtConfig.CalibrationData[currLVDT].Slope.ToString("F");
            
            int currLoadCell = FDTesterSingleton.Instance.PlotData.loadCellConfig.CurrentTransducerUsed;
            calDlg.textBoxLoadCellOffset.Text = FDTesterSingleton.Instance.PlotData.loadCellConfig.CalibrationData[currLoadCell].Offset.ToString("F");
            calDlg.textBoxLoadCellScale.Text = FDTesterSingleton.Instance.PlotData.loadCellConfig.CalibrationData[currLoadCell].Slope.ToString("F");

            if (calDlg.ShowDialog() == DialogResult.OK)
            {
                FDTesterSingleton.Instance.PlotData.lvdtConfig.CalibrationData[currLVDT].Offset = double.Parse(calDlg.textBoxLVDTOffset.Text);
                FDTesterSingleton.Instance.PlotData.lvdtConfig.CalibrationData[currLVDT].Slope = double.Parse(calDlg.textBoxLVDTScale.Text);

                FDTesterSingleton.Instance.PlotData.loadCellConfig.CalibrationData[currLoadCell].Offset = double.Parse(calDlg.textBoxLoadCellOffset.Text);
                FDTesterSingleton.Instance.PlotData.loadCellConfig.CalibrationData[currLoadCell].Slope = double.Parse(calDlg.textBoxLoadCellScale.Text);
            }
        }

        private void axesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AxisScalingInput axisDlg = new AxisScalingInput();
            axisDlg.textBoxDeflectionMax.Text = FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax.ToString("F");
            axisDlg.textBoxDeflectionMin.Text = FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin.ToString("F");
            axisDlg.textBoxForceMax.Text = FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax.ToString("F");
            axisDlg.textBoxForceMin.Text = FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin.ToString("F");

            if (axisDlg.ShowDialog() == DialogResult.OK)
            {
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax = double.Parse(axisDlg.textBoxDeflectionMax.Text);
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin = double.Parse(axisDlg.textBoxDeflectionMin.Text);

                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax = double.Parse(axisDlg.textBoxForceMax.Text);
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin = double.Parse(axisDlg.textBoxForceMin.Text);

                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisInterceptPhysCord = FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin;
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisInterceptPhysCord = FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin;
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTickOrigin = FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin;
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTickSpace = (FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax - FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin) / 100;
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTicksPerMajor = 10;
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTickOrigin = FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin;
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTickSpace = (FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax - FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin) / 100;
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTicksPerMajor = 10;

                //ForceDeflectionPlotControl.ChangeAxesSettings();
                ForceDeflectionPlotControl.InitializeChart();
               // startNewTestToolStripMenuItem.Enabled = true; //this is inside the test tool strip menu
              //  fileToolStripMenuItem.Enabled = true;
                ForceDeflectionPlotControl.ToggleLiveMode();
                axesToolStripMenuItem.Enabled = false;
                configuretestToolStripMenuItem.Enabled = true;
                rescaleAxesToolStripMenuItem.Visible = true;
            }

            
        
        }

       

        private void TestEstablishZeroPoint_Click(object sender, EventArgs e)
        {
            const string message = "Are you sure that you want to Zero Force and Deflection scaled values ? ";
            const string caption = "Force, Deflection Zeroing choice";
            var result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            // If the no button was pressed ...
            if (result == DialogResult.No)
            {
                return;
            }
            else
            {
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisZeroValueScaled = FDTesterSingleton.Instance.PlotData.livePlotData.DisplacementData[(int)PlotPersistentData.ReadingType.enScaled];
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisZeroValueScaled = FDTesterSingleton.Instance.PlotData.livePlotData.ForceData[(int)PlotPersistentData.ReadingType.enScaled];
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveTestResults();
            ForceDeflectionPlotControl.PrintSaveAsFile();
        }

        private void pageSetupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ForceDeflectionPlotControl.PageSetup(sender, e);
        }

        private void printerSetupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ForceDeflectionPlotControl.PrinterSetup(sender, e);

        }

        private void printPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ForceDeflectionPlotControl.PrintPreview(sender, e);
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ForceDeflectionPlotControl.PrintPage(sender, e);

        }

        private void establishZeroPointForTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string message = "Are you sure that you want to Zero Force and Deflection scaled values ? ";
            const string caption = "Force, Deflection Zeroing choice";
            var result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            // If the no button was pressed ...
            if (result == DialogResult.No)
            {
                return;
            }
            else
            {
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisZeroValueScaled +=  FDTesterSingleton.Instance.PlotData.livePlotData.DisplacementData[(int)PlotPersistentData.ReadingType.enScaled];
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisZeroValueScaled += FDTesterSingleton.Instance.PlotData.livePlotData.ForceData[(int)PlotPersistentData.ReadingType.enScaled];
                //ChartButtonsToolStrip.Visible = true;
            }
            establishZeroPointForTestToolStripMenuItem.Enabled = false;            
            startNewTestToolStripMenuItem.Enabled = true; //this is inside the test tool strip menu
            fileToolStripMenuItem.Enabled = true;

        }

        private void rescaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
           
            
            
        }

        private void rescaleAxesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AxisScalingInput axisDlg = new AxisScalingInput();
            axisDlg.textBoxDeflectionMax.Text = FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax.ToString("F");
            axisDlg.textBoxDeflectionMin.Text = FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin.ToString("F");
            axisDlg.textBoxForceMax.Text = FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax.ToString("F");
            axisDlg.textBoxForceMin.Text = FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin.ToString("F");

            if (axisDlg.ShowDialog() == DialogResult.OK)
            {
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax = double.Parse(axisDlg.textBoxDeflectionMax.Text);
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin = double.Parse(axisDlg.textBoxDeflectionMin.Text);

                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax = double.Parse(axisDlg.textBoxForceMax.Text);
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin = double.Parse(axisDlg.textBoxForceMin.Text);

                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisInterceptPhysCord = FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin;
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisInterceptPhysCord = FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin;
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTickOrigin = FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin;
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTickSpace = (FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax - FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin) / 100;
                FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTicksPerMajor = 10;
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTickOrigin = FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin;
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTickSpace = (FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax - FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin) / 100;
                FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTicksPerMajor = 10;

                ForceDeflectionPlotControl.ChangeAxesSettings();
                //ForceDeflectionPlotControl.InitializeChart();
                // startNewTestToolStripMenuItem.Enabled = true; //this is inside the test tool strip menu
                //  fileToolStripMenuItem.Enabled = true;
                //ForceDeflectionPlotControl.ToggleLiveMode();
                //axesToolStripMenuItem.Enabled = false;
                //configuretestToolStripMenuItem.Enabled = true;
            }
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string message = "Are you sure that you want to exit the application ? All drawn plots will be cleared ";
            const string caption = "Force, Deflection Application Exiting choice";
            var result = MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Stop);

            // If the no button was pressed ...
            if (result == DialogResult.No)
            {
                return;
            }
            
            
            Application.Exit();
            
        }

        private void timerBathTempUpdate_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < FDTesterPersistentData.NumbBaths; i++)
            {
                baths[i].ReadPV_SP();
            }

        }

        private void SetpointBath1_Click(object sender, EventArgs e)
        {
            WriteTemperatureSetpointDialog setpointdlg = new WriteTemperatureSetpointDialog();
            setpointdlg.numericUpDownSetpoint.Value = (decimal)FDTesterSingleton.Instance.PlotData.BathsData[0].SetpointRead;
            if (setpointdlg.ShowDialog() == DialogResult.OK)
            {
                double newSetpoint = (double)setpointdlg.numericUpDownSetpoint.Value;
                if (baths[0].WriteSP(newSetpoint) == true)
                {
                    FDTesterSingleton.Instance.PlotData.BathsData[0].Setpoint = newSetpoint;
                    FDTesterSingleton.Instance.PlotData.BathsData[0].SetpointRead = newSetpoint;
                }
            }
        }

        private void SetpointBath2_Click(object sender, EventArgs e)
        {
            WriteTemperatureSetpointDialog setpointdlg = new WriteTemperatureSetpointDialog();
            setpointdlg.numericUpDownSetpoint.Value = (decimal)FDTesterSingleton.Instance.PlotData.BathsData[1].SetpointRead;
            if (setpointdlg.ShowDialog() == DialogResult.OK)
            {
                double newSetpoint = (double)setpointdlg.numericUpDownSetpoint.Value;
                if (baths[1].WriteSP(newSetpoint) == true)
                {
                    FDTesterSingleton.Instance.PlotData.BathsData[1].Setpoint = newSetpoint;
                    FDTesterSingleton.Instance.PlotData.BathsData[1].SetpointRead = newSetpoint;
                }
            }

        }

        private void SetpointBath3_Click(object sender, EventArgs e)
        {
            WriteTemperatureSetpointDialog setpointdlg = new WriteTemperatureSetpointDialog();
            setpointdlg.numericUpDownSetpoint.Value = (decimal)FDTesterSingleton.Instance.PlotData.BathsData[2].SetpointRead;
            if (setpointdlg.ShowDialog() == DialogResult.OK)
            {
                double newSetpoint = (double)setpointdlg.numericUpDownSetpoint.Value;
                if (baths[2].WriteSP(newSetpoint) == true)
                {
                    FDTesterSingleton.Instance.PlotData.BathsData[2].Setpoint = newSetpoint;
                    FDTesterSingleton.Instance.PlotData.BathsData[2].SetpointRead = newSetpoint;
                }
            }

        }

        private void SetpointBath4_Click(object sender, EventArgs e)
        {
            WriteTemperatureSetpointDialog setpointdlg = new WriteTemperatureSetpointDialog();
            setpointdlg.numericUpDownSetpoint.Value = (decimal)FDTesterSingleton.Instance.PlotData.BathsData[3].SetpointRead;
            if (setpointdlg.ShowDialog() == DialogResult.OK)
            {
                double newSetpoint = (double)setpointdlg.numericUpDownSetpoint.Value;
                if (baths[3].WriteSP(newSetpoint) == true)
                {
                    FDTesterSingleton.Instance.PlotData.BathsData[3].Setpoint = newSetpoint;
                    FDTesterSingleton.Instance.PlotData.BathsData[3].SetpointRead = newSetpoint;
                }
            }

        }

        private void SetpointBath5_Click(object sender, EventArgs e)
        {
            WriteTemperatureSetpointDialog setpointdlg = new WriteTemperatureSetpointDialog();
            setpointdlg.numericUpDownSetpoint.Value = (decimal)FDTesterSingleton.Instance.PlotData.BathsData[4].SetpointRead;
            if (setpointdlg.ShowDialog() == DialogResult.OK)
            {
                double newSetpoint = (double)setpointdlg.numericUpDownSetpoint.Value;
                if (baths[4].WriteSP(newSetpoint) == true)
                {
                    FDTesterSingleton.Instance.PlotData.BathsData[4].Setpoint     = newSetpoint;
                    FDTesterSingleton.Instance.PlotData.BathsData[4].SetpointRead = newSetpoint;
                }
            }

        }

        private void buttonLivefromLVDTLoadCell_Click(object sender, EventArgs e)
        {
            ForceDeflectionPlotControl.ToggleLiveMode();
        }

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{

		}
	}
}
