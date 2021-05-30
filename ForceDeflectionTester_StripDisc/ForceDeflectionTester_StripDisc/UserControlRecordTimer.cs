using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using com.quinncurtis.chart2dnet;
using com.quinncurtis.rtgraphnet;
using System.Collections;

namespace ForceDeflectionTester_StripDisc
{
    public partial class UserControlRecordTimer : com.quinncurtis.chart2dnet.ChartView
    {

        RTProcessVar stopWatch;
        long stopWatchStart = 0;
        long stopWatchStop = 0;
        long stopWatchDuration = 0;
        ChartAttribute defaultattrib;
        public Color NextPlotcolor
        {
            get;
            set;
        }     


        Font font8 = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
        Font font10 = new Font("Microsoft Sans Serif", 10, FontStyle.Regular);
        Font font10Bold = new Font("Microsoft Sans Serif", 10, FontStyle.Bold);
        Font font12 = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
        Font font12Bold = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
        Font font12Numeric = new Font("Digital SF", 12, FontStyle.Regular);
        Font font14Numeric = new Font("Digital SF", 14, FontStyle.Regular);
        Font font24Numeric = new Font("Digital SF", 24, FontStyle.Regular);
        Font font32Numeric = new Font("Digital SF", 32, FontStyle.Regular);
        Font font18Numeric = new Font("Digital SF", 18, FontStyle.Regular);
        Font font14 = new Font("Microsoft Sans Serif", 14, FontStyle.Regular);
        Font font24 = new Font("Microsoft Sans Serif", 24, FontStyle.Regular);
        Font font14Bold = new Font("Microsoft Sans Serif", 14, FontStyle.Bold);
        Font font18Bold = new Font("Microsoft Sans Serif", 18, FontStyle.Bold);

        RTControlButton StartButton = new RTControlButton(ChartObj.RT_CONTROL_RADIOBUTTON_SUBTYPE);
        RTControlButton StopButton = new RTControlButton(ChartObj.RT_CONTROL_RADIOBUTTON_SUBTYPE);

        RTFormControlGrid controlgrid1;
        bool StartStopState;

        public UserControlRecordTimer()
        {
            defaultattrib = new ChartAttribute(Color.Green, 1.0, DashStyle.Solid, Color.Green);
            stopWatch = new RTProcessVar("Stop Watch", defaultattrib);
            InitializeComponent();
        }

        public bool StartStopStateValue
        {
            get
            {
                return StartStopState;
            }
            set
            {
                StartStopState = value;
            }

        }

        private ForceDeflectionPlot thePlot;
        public ForceDeflectionPlot refPlot
        {
            set
            {
                thePlot = value;
            }
        }
        public void InitializeStartStopButtonControls()
        {
            Font buttonfont = font14Bold;

            ChartView chartVu = this;

            CartesianCoordinates pTransform1 = new CartesianCoordinates(0.0, 0.0, 1.0, 1.0);
            pTransform1.SetGraphBorderDiagonal(0.05, .05, 0.35, 0.95);
            ChartAttribute attrib1 = new ChartAttribute(Color.White, 5, DashStyle.Solid, Color.IndianRed);

            ArrayList buttonlist1 = new ArrayList();

            StartButton.ButtonUncheckedText = "Start";
            StartButton.Click += new System.EventHandler(this.selector_Button_Click);
            StartButton.ButtonUncheckedTextColor = Color.Black;
            StartButton.ButtonCheckedTextColor = Color.Red;
            StartButton.ButtonFont = buttonfont;
            StartButton.ButtonChecked = false; // Set the state last, after other properties set

            buttonlist1.Add(StartButton);

            StopButton.ButtonUncheckedText = "Stop";
            StopButton.Click += new System.EventHandler(this.selector_Button_Click);
            StopButton.ButtonFont = buttonfont;
            StopButton.ButtonUncheckedColor = Color.Red;
            StopButton.ButtonCheckedColor = Color.White;
            StopButton.Visible = false;
            StopButton.ButtonChecked = true; // Set the state last, after other properties set
            

            buttonlist1.Add(StopButton);

            int numColumns = 2;
            int numRows = 1;

            controlgrid1 = new RTFormControlGrid(pTransform1, null, buttonlist1, numColumns,
                numRows, attrib1);
            controlgrid1.CellRowMargin = 0.1;
            controlgrid1.CellColumnMargin = 0.0;
            controlgrid1.FormControlTemplate.Frame3DEnable = true;
            chartVu.AddChartObject(controlgrid1);
            
        }
        

        public void InitializeStopWatchPanelMeter()
        {
            ChartView chartVu = this;

            CartesianCoordinates pTransform1 = new CartesianCoordinates(0.0, 0.0, 1.0, 1.0);

            pTransform1.SetGraphBorderDiagonal(0.4, 0.01, 0.95, 0.95);

            ChartAttribute panelmeterattrib = new ChartAttribute(Color.SteelBlue, 3, DashStyle.Solid, Color.Black);
            RTElapsedTimePanelMeter panelmeter = new RTElapsedTimePanelMeter(pTransform1, stopWatch, panelmeterattrib);
            panelmeter.PanelMeterPosition = ChartObj.INSIDE_INDICATOR;
            panelmeter.TimeTemplate.TextFont = font32Numeric;
            panelmeter.TimeTemplate.TimeFormat = ChartObj.TIMEDATEFORMAT_24HMS;
            panelmeter.TimeTemplate.DecimalPos = 0;
            panelmeter.AlarmIndicatorColorMode = ChartObj.RT_INDICATOR_COLOR_NO_ALARM_CHANGE;
            chartVu.AddChartObject(panelmeter);

            ChartAttribute panelmetertagattrib = new ChartAttribute(Color.Beige, 0, DashStyle.Solid, Color.Beige);
            RTStringPanelMeter panelmeter3 = new RTStringPanelMeter(pTransform1, stopWatch, panelmetertagattrib, ChartObj.RT_TAG_STRING);
            panelmeter3.StringTemplate.TextFont = font10;
            panelmeter3.PanelMeterPosition = ChartObj.ABOVE_REFERENCED_TEXT;
            panelmeter3.SetPositionReference(panelmeter);
            panelmeter3.TextColor = Color.Black;
            //chartVu.AddChartObject(panelmeter3);

        }
        public void InitializeGraph()
        {
            DateTime time = DateTime.Now;


            stopWatchStart = time.Ticks / 10000; // convert ticks to msecs by dividing by 10000
            stopWatchDuration = 0;


            stopWatch.SetCurrentValue(stopWatchDuration, 0.0); // Stop watch set to 0
            InitializeStopWatchPanelMeter();
            InitializeStartStopButtonControls();

        }

        private void selector_Button_Click(object sender, System.EventArgs e)
        {
            RTControlButton button = (RTControlButton)sender;
            if (button == StartButton)
            {
                if (UserControlTimer.Enabled == false)
                {
                    DateTime time = DateTime.Now;
                    stopWatchStart = time.Ticks / 10000; // convert ticks to msecs by dividing by 10000
                    stopWatchDuration = 0;
                    thePlot.StartNewPlotRecording(NextPlotcolor);
                    StopButton.Visible = true;
                    StartButton.Visible = false;
                    
                }
                UserControlTimer.Enabled = true;
            }
            else if (button == StopButton)
            {
                UserControlTimer.Enabled = false;
                stopWatchDuration = 0;
                stopWatch.SetCurrentValue(stopWatchDuration, 0.0);
                thePlot.EndNewPlotRecording();
                StartStopState = false;
                StopButton.Visible = false;
                StartButton.Visible = true;
            }

            this.UpdateDraw();

        }

        private void timer1_Tick(object sender, EventArgs e)
        {

            DateTime time = DateTime.Now;
            stopWatchStop = time.Ticks / 10000;
            // need to divide by 10000 to convert timer ticks to msecs
            stopWatchDuration = (stopWatchStop - stopWatchStart);

            stopWatch.SetCurrentValue(stopWatchDuration, 0.0); // Stop watch           

            if (stopWatchDuration >= TimeSpan.FromMinutes(5.0).TotalMilliseconds)
            {
                RTControlButton mybutton = new RTControlButton();
                mybutton = StopButton;
                EventArgs myevent = new EventArgs();
                selector_Button_Click(mybutton, myevent);
            }



            this.UpdateDraw();
        }
    }
}
