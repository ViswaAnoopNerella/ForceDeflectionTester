using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using com.quinncurtis.chart2dnet;
using com.quinncurtis.rtgraphnet;
using NationalInstruments.DAQmx;
using NationalInstruments;
using System.IO;

namespace com.quinncurtis.chart2dnet
{
    class FreePlotText : ChartText
    {
        public FreePlotText(PhysicalCoordinates pTransform, Font font8, String TextString, double XAxisMiddlePhyPos, double YAxisMiddlePhyPos, int GraphObjPosType, int GraphObj_JUSTIFY_TYPE, int GraphObj_JUSTIFY_Type, int TextAngle)
            : base(pTransform, font8, TextString, XAxisMiddlePhyPos, YAxisMiddlePhyPos, GraphObjPosType, GraphObj_JUSTIFY_TYPE, GraphObj_JUSTIFY_Type, TextAngle)
        {

        } 
    }

}

namespace ForceDeflectionTester_StripDisc
{
    public class ExtendedRTSimpleSingleValuePlot : RTSimpleSingleValuePlot
    {
        public ExtendedRTSimpleSingleValuePlot(PhysicalCoordinates transform, SimpleLinePlot linePlot, RTProcessVar processVar)
            : base(transform, linePlot, processVar)
        {

        }
        public int PlotIndex
        {
            get;
            set;
        }
    }
    
    public partial class ForceDeflectionPlot : com.quinncurtis.chart2dnet.ChartView
    {

        private AnalogMultiChannelReader analogInReader;
        private Task myTask;
        private Task runningTask;
        private AsyncCallback analogCallback;
        private AnalogWaveform<double>[] data;
        public static List<ChartObj> UndoStack = new List<ChartObj>();
        public enum PlottingState { enPlotOff, enPlotLive, enPlotRecord }
        PlottingState plotState;
        ChartPrint printobj = null;

        #region font_declaration

        public Font font8 = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
        public Font font10 = new Font("Microsoft Sans Serif", 10, FontStyle.Regular);
        public Font font10Bold = new Font("Microsoft Sans Serif", 10, FontStyle.Bold);
        public Font font12 = new Font("Microsoft Sans Serif", 12, FontStyle.Regular);
        public Font font12Bold = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
        public Font font28Numeric = new Font("Digital SF", 28, FontStyle.Regular);
        public Font font18Numeric = new Font("Digital SF", 18, FontStyle.Regular);
        public Font font16Bold = new Font("Microsoft Sans Serif", 16, FontStyle.Bold);

        #endregion

        #region GraphingConstants

        public const double rLEFT = 0.05;
        public const double rTOP = 0.05;
        public const double rRIGHT = 0.93;
        public const double rBOTTOM = 0.93;
        public const String MainTitleString = " Force Deflection Curve";
        public const double PlotPenLineWidth = 2.0;

        #endregion

        #region CommonPlotObjects

        CartesianCoordinates pTransform;
        Background GraphBackground;
        Background PlotBackground;
        LinearAxis DeflectionAxis;
        LinearAxis ForceAxis;
        NumericAxisLabels DeflectionAxisNumericLabel, ForceAxisNumericLabel;
        AxisTitle DeflectionAxisTitle, ForceAxisTitle;
        Grid DeflectionAxisMinorGrid, DeflectionAxisMajorGrid, ForceAxisMinorGrid, ForceAxisMajorGrid;
        ChartTitle MainTitle;
        ChartZoom zoomObj;
        CustomToolTip stocktooltip;
        CustomZeroToolTip Zerotooltip;
        MoveObj UserMoveObj;

        Font legendFont, legendOffsetFont;
        ChartAttribute legendAttributes;
        StandardLegend legend;
        StandardLegend legendForceOffset;
        Font toolTipFont;
        ChartSymbol toolTipSymbol;
        NumericLabel xValueTemplate;
        NumericLabel yValueTemplate;

        #endregion

        #region IndividualPlotSettings

        private Color[] ChartColorArray = { Color.Blue, Color.Red, Color.DarkSeaGreen, Color.DarkSlateBlue, Color.Brown, Color.DarkViolet, Color.Black, Color.DeepPink, Color.Orange, Color.LightGreen };
        ChartAttribute[] PenColorStyles = new ChartAttribute[TestPersistentData.MaxPlotsPerTest];
        ExtendedRTSimpleSingleValuePlot[] DrawRTPlots = new ExtendedRTSimpleSingleValuePlot[TestPersistentData.MaxPlotsPerTest];
        RTProcessVar[] DrawRTPlotsData = new RTProcessVar[TestPersistentData.MaxPlotsPerTest];
        SimpleLinePlot[] DrawLinePlots = new SimpleLinePlot[TestPersistentData.MaxPlotsPerTest];

        ChartAttribute PenColorStylesLive;
        RTSimpleSingleValuePlot DrawRTLivePlots;
        RTProcessVar DrawRTLiveData;
        SimpleLinePlot DrawRTLiveLinePlots;

        


        #endregion

        /// <summary>
        /// constructor - initialized the axes, plots, data structures
        /// </summary>
        public ForceDeflectionPlot()
        {
            InitializeComponent();
        }

        public void AddLegend()
        {
            int currentPlotIndex = FDTesterSingleton.Instance.PlotData.TestData.currentPlotNumber;
            double plotTemp = FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData[currentPlotIndex].Temperature;
            string legendString = plotTemp.ToString("F") + " C";
            legend.AddLegendItem(legendString, ChartObj.LINE, DrawLinePlots[currentPlotIndex], legendFont);            
            ChartView chartVu = this;
            chartVu.UpdateDraw();
        }
        public void AddLegendForceOffset(int plotNumber)
        {
            double ForceOffsetVal = FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData[plotNumber].ForceOffset;
            string legendString = "F(g)Offset: " + ForceOffsetVal.ToString("F");
            legendForceOffset.AddLegendItem(legendString, ChartObj.LINE, DrawLinePlots[plotNumber], legendOffsetFont);
            //ChartView chartVu = this;
            //chartVu.UpdateDraw();
        }

        private void RedrawPlotSettings()
        {
            pTransform.SetGraphBorderDiagonal(rLEFT, rTOP, rRIGHT, rBOTTOM);
            DeflectionAxis.SetColor(Color.Black);
            DeflectionAxis.SetAxisIntercept(FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisInterceptPhysCord);
            DeflectionAxis.SetAxisTicks(FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTickOrigin, FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTickSpace, FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTicksPerMajor);
            ForceAxis.SetColor(Color.Black);
            ForceAxis.SetAxisIntercept(FDTesterSingleton.Instance.PlotData.TestData.ForceAxisInterceptPhysCord);
            ForceAxis.SetAxisTicks(FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTickOrigin, FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTickSpace, FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTicksPerMajor);
            DeflectionAxisNumericLabel.SetColor(Color.Black);
            ForceAxisNumericLabel.SetColor(Color.Black);

            int CurrentLVDTCalibIndex = FDTesterSingleton.Instance.PlotData.lvdtConfig.CurrentTransducerUsed;
            DeflectionAxisTitle = new AxisTitle(DeflectionAxis, font12Bold, FDTesterSingleton.Instance.PlotData.lvdtConfig.CalibrationData[CurrentLVDTCalibIndex].YDataName);

            int CurrentLoadCellCalibIndex = FDTesterSingleton.Instance.PlotData.loadCellConfig.CurrentTransducerUsed;
            ForceAxisTitle = new AxisTitle(ForceAxis, font12Bold, FDTesterSingleton.Instance.PlotData.loadCellConfig.CalibrationData[CurrentLoadCellCalibIndex].YDataName);

            DeflectionAxisMinorGrid.SetColor(Color.Gray);
            DeflectionAxisMinorGrid.SetLineWidth(1);
            DeflectionAxisMinorGrid.SetLineStyle(DashStyle.Dash);

            DeflectionAxisMajorGrid.SetColor(Color.Gray);
            DeflectionAxisMajorGrid.SetLineWidth(1);
            DeflectionAxisMajorGrid.SetLineStyle(DashStyle.Solid);

            ForceAxisMinorGrid.SetColor(Color.Gray);
            ForceAxisMinorGrid.SetLineWidth(1);
            ForceAxisMinorGrid.SetLineStyle(DashStyle.Dash);

            ForceAxisMajorGrid.SetColor(Color.Gray);
            ForceAxisMajorGrid.SetLineWidth(1);
            ForceAxisMajorGrid.SetLineStyle(DashStyle.Solid);

            legendAttributes.SetFillFlag(false);
            legendAttributes.SetLineFlag(false);

            for (int i = 0; i < TestPersistentData.MaxPlotsPerTest; i++)
            {
                DrawRTPlotsData[i].DatasetEnableUpdate = true;
                DrawLinePlots[i].SetFastClipMode(ChartObj.FASTCLIP_X);

            }

            DrawRTLiveData.DatasetEnableUpdate = true;

            zoomObj.SetButtonMask(MouseButtons.Left);
            zoomObj.SetZoomYEnable(true);
            zoomObj.SetZoomXEnable(true);
            zoomObj.SetZoomXRoundMode(ChartObj.AUTOAXES_EXACT);
            zoomObj.SetZoomYRoundMode(ChartObj.AUTOAXES_EXACT);
            zoomObj.InternalZoomStackProcesssing = true;
            zoomObj.ArCorrectionMode = ChartObj.ZOOM_NO_AR_CORRECTION;
            zoomObj.SetEnable(false);

            stocktooltip.SetDataToolTipFormat(ChartObj.DATA_TOOLTIP_CUSTOM);
            stocktooltip.SetEnable(false);

            toolTipSymbol.SetSymbolSize(5.0);
            Zerotooltip.SetXValueTemplate(xValueTemplate);
            Zerotooltip.SetYValueTemplate(yValueTemplate);
            Zerotooltip.SetDataToolTipFormat(ChartObj.DATA_TOOLTIP_XY_ONELINE);
            Zerotooltip.SetToolTipSymbol(toolTipSymbol);            
            Zerotooltip.SetEnable(false);


            ChartView chartVu = this;
           

            UserMoveObj.SetMoveObjectFilter("FreePlotText"); ////mouselistener.SetMoveObjectFilter("Marker");
            UserMoveObj.SetEnable(false);

            plotState = PlottingState.enPlotOff;


            // Graph Settings

            
            chartVu.AddChartObject(GraphBackground);
            chartVu.AddChartObject(PlotBackground);
            chartVu.AddChartObject(DeflectionAxis);
            chartVu.AddChartObject(ForceAxis);
            chartVu.AddChartObject(DeflectionAxisNumericLabel);
            chartVu.AddChartObject(ForceAxisNumericLabel);
            chartVu.AddChartObject(DeflectionAxisMinorGrid);
            chartVu.AddChartObject(DeflectionAxisMajorGrid);
            chartVu.AddChartObject(ForceAxisMinorGrid);
            chartVu.AddChartObject(ForceAxisMajorGrid);
            chartVu.AddChartObject(DeflectionAxisTitle);
            chartVu.AddChartObject(ForceAxisTitle);
            chartVu.AddChartObject(MainTitle);
            chartVu.AddChartObject(legend);
            chartVu.AddChartObject(legendForceOffset);

            //Recorded Plots
            for (int i = 0; i < TestPersistentData.MaxPlotsPerTest; i++)
            {
                chartVu.AddChartObject(DrawRTPlots[i]);
                DrawRTPlots[i].ChartObjEnable = FDTesterSingleton.Instance.PlotData.TestData.GraphObjChartObjEnable[i];
            }

            //Live Plots
            chartVu.AddChartObject(DrawRTLivePlots);
            DrawRTLivePlots.ChartObjEnable = GraphObj.OBJECT_DISABLE;

            //Mouse listeners

            //////chartVu.AddMouseListener(zoomObj);
            //////chartVu.AddMouseListener(stocktooltip);
            //////chartVu.AddMouseListener(Zerotooltip);
            //////chartVu.AddMouseListener(UserMoveObj);
            
            chartVu.UpdateDraw();


            //StartAnalogDataCollection(); // we have to remove this
            //LiveDataMode(true); //we have to remove this




        }

        public void ClearChartsForNewTest()
        {
            ChartView chartVu = this;
            
            
            for (int i = 0; i <= legend.GetNumLegendItems(); i++)
            {
                legend.DeleteLegendItem(i); 
            }
            for (int i = 0; i <= legendForceOffset.GetNumLegendItems(); i++)
            {
                legendForceOffset.DeleteLegendItem(i);
            }

            legendFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
            legendOffsetFont = new Font("Microsoft Sans Serif", 10, FontStyle.Bold);
            legendAttributes = new ChartAttribute(Color.White, 1, DashStyle.Solid);
            legend = new StandardLegend(0.94, 0.1, 0.7, 0.5, legendAttributes, StandardLegend.VERT_DIR);
            legendForceOffset = new StandardLegend(0.93, 0.6, 0.3, 0.5, legendAttributes, StandardLegend.VERT_DIR);  
            chartVu.GetChartObjectsArrayList().RemoveRange(0, chartVu.GetChartObjectsArrayList().Count);
            chartVu.ResetChartObjectList();          
            chartVu.UpdateDraw();
            RedrawPlotSettings();
            

        }


        // This routine invokes the chart objects PageSetupItem method
        public void PageSetup(object sender, System.EventArgs e)
        {
            ChartView chartVu = this;
            if (chartVu != null)
            {
                if (printobj == null)
                {
                    printobj = new ChartPrint(chartVu, ChartObj.PRT_PROP);
                }
                else
                    printobj.PrintChartView = chartVu;
                printobj.PageSetupItem(sender, e);
            }
        }

        // This routine invokes the chart objects printer setup dialog method
        public void PrinterSetup(object sender, System.EventArgs e)
        {
            ChartView chartVu = this;
            if (chartVu != null)
            {
                if (printobj == null)
                {
                    printobj = new ChartPrint(chartVu, ChartObj.PRT_PROP);
                }
                else
                    printobj.PrintChartView = chartVu;
                printobj.DoPrintDialog();
            }
        }

        // This routine invokes the chart objects PrintPreviewItem method
        public void PrintPreview(object sender, System.EventArgs e)
        {
            ChartView chartVu = this;
            if (chartVu != null)
            {
                if (printobj == null)
                {
                    printobj = new ChartPrint(chartVu,ChartObj.PRT_PROP);
                }
                else
                    printobj.PrintChartView = chartVu;
                printobj.PrintPreviewItem(sender, e);
            }
        }

        // This routine prints a chart by invoking the chart objects DocPrintPage method
        public void PrintPage(object sender, System.EventArgs e)
        {
            ChartView chartVu = this;
            if (chartVu != null)
            {
                if (printobj == null)
                {
                    printobj = new ChartPrint(chartVu, ChartObj.PRT_PROP);
                    printobj.DoPrintDialog();
                }
                else
                    printobj.PrintChartView = chartVu;

                printobj.DocPrintPage(sender, e);
            }
        }




        private void InitializePlotSettings() 
        {
            pTransform = new CartesianCoordinates(FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin, FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin, FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax, FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax);
            
            GraphBackground = new Background(pTransform, ChartObj.GRAPH_BACKGROUND, Color.White);
            PlotBackground = new Background(pTransform, ChartObj.PLOT_BACKGROUND, Color.White);
            DeflectionAxis = new LinearAxis(pTransform, ChartObj.X_AXIS);
            
            ForceAxis = new LinearAxis(pTransform, ChartObj.Y_AXIS);
            
            DeflectionAxisNumericLabel = new NumericAxisLabels(DeflectionAxis);
            

            ForceAxisNumericLabel = new NumericAxisLabels(ForceAxis);
           

            
            DeflectionAxisMinorGrid = new Grid(DeflectionAxis, ForceAxis, ChartObj.X_AXIS, ChartObj.GRID_MINOR);
           
            DeflectionAxisMajorGrid = new Grid(DeflectionAxis, ForceAxis, ChartObj.X_AXIS, ChartObj.GRID_MAJOR);
          
            ForceAxisMinorGrid = new Grid(DeflectionAxis, ForceAxis, ChartObj.Y_AXIS, ChartObj.GRID_MINOR);
           
            ForceAxisMajorGrid = new Grid(DeflectionAxis, ForceAxis, ChartObj.Y_AXIS, ChartObj.GRID_MAJOR);
           
            MainTitle = new ChartTitle(pTransform, font16Bold, "Force Deflection Curve", ChartObj.CHART_HEADER, ChartObj.CENTER_PLOT);


            legendFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
            legendOffsetFont = new Font("Microsoft Sans Serif", 10, FontStyle.Bold);            
            legendAttributes = new ChartAttribute(Color.White, 1, DashStyle.Solid);
            legend = new StandardLegend(0.94, 0.1, 0.7, 0.5, legendAttributes, StandardLegend.VERT_DIR);
            legendForceOffset = new StandardLegend(0.93, 0.6, 0.3, 0.5, legendAttributes, StandardLegend.VERT_DIR);              


            for (int i = 0; i < TestPersistentData.MaxPlotsPerTest; i++)
            {
                PenColorStyles[i] = new ChartAttribute(ChartColorArray[i], PlotPenLineWidth, DashStyle.Solid);

                DrawRTPlotsData[i] = new RTProcessVar();
                DrawRTPlotsData[i].DatasetEnableUpdate = true;
                

                DrawLinePlots[i] = new SimpleLinePlot(pTransform, null, PenColorStyles[i]);
                
            

                DrawRTPlots[i] = new ExtendedRTSimpleSingleValuePlot(pTransform, DrawLinePlots[i], DrawRTPlotsData[i]);

            }

            // for live plot display 
            PenColorStylesLive = new ChartAttribute(Color.Red, 2.0, DashStyle.DashDotDot);
            DrawRTLiveData = new RTProcessVar();
            DrawRTLiveData.DatasetEnableUpdate = true;
           
            DrawRTLiveLinePlots = new SimpleLinePlot(pTransform, null, PenColorStylesLive);
            DrawRTLivePlots = new RTSimpleSingleValuePlot(pTransform, DrawRTLiveLinePlots, DrawRTLiveData);

            // Add the mouse listeners

            //Zoom functionality
            ChartView chartVu = this;
            zoomObj = new ChartZoom(chartVu, pTransform, true);
           
            stocktooltip = new CustomToolTip(this);
           

            toolTipFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);
            Zerotooltip = new CustomZeroToolTip(this);
            xValueTemplate = new NumericLabel(ChartObj.DECIMALFORMAT, 2); // use minimal constructor 
            yValueTemplate = new NumericLabel(ChartObj.DECIMALFORMAT, 2); // use minimal constructor 
            toolTipSymbol = new ChartSymbol(null, ChartObj.SQUARE, new ChartAttribute(Color.Black));      
            
            UserMoveObj = new MoveObj(chartVu);
          

        }

        /// <summary>
        /// Modify the current Axes transformation and axis intercepts, tick mark origins, axis title
        /// </summary>
        public void ChangeAxesSettings()
        {
            pTransform.SetPhysScale(FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin, FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin, FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax, FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax);
            pTransform.SetGraphBorderDiagonal(rLEFT, rTOP, rRIGHT, rBOTTOM);
            GraphBackground = new Background(pTransform, ChartObj.GRAPH_BACKGROUND, Color.White);
            PlotBackground = new Background(pTransform, ChartObj.PLOT_BACKGROUND, Color.White);
           // DeflectionAxis = new LinearAxis(pTransform, ChartObj.X_AXIS);
           // ForceAxis = new LinearAxis(pTransform, ChartObj.Y_AXIS);
            DeflectionAxis.CalcAutoAxis();
            ForceAxis.CalcAutoAxis();                
            DeflectionAxis.SetAxisTicks(FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTickOrigin, FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTickSpace, FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisTicksPerMajor);
            DeflectionAxis.SetAxisIntercept(FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisInterceptPhysCord);
            ForceAxis.SetAxisTicks(FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTickOrigin, FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTickSpace, FDTesterSingleton.Instance.PlotData.TestData.ForceAxisTicksPerMajor);          
            ForceAxis.SetAxisIntercept(FDTesterSingleton.Instance.PlotData.TestData.ForceAxisInterceptPhysCord);
           // DeflectionAxisMinorGrid = new Grid(DeflectionAxis, ForceAxis, ChartObj.X_AXIS, ChartObj.GRID_MINOR);
            //DeflectionAxisMajorGrid = new Grid(DeflectionAxis, ForceAxis, ChartObj.X_AXIS, ChartObj.GRID_MAJOR);
            //ForceAxisMinorGrid = new Grid(DeflectionAxis, ForceAxis, ChartObj.Y_AXIS, ChartObj.GRID_MINOR);
            //ForceAxisMajorGrid = new Grid(DeflectionAxis, ForceAxis, ChartObj.Y_AXIS, ChartObj.GRID_MAJOR);            
            int CurrentLVDTCalibIndex = FDTesterSingleton.Instance.PlotData.lvdtConfig.CurrentTransducerUsed;
            DeflectionAxisTitle.SetAxisTitle(DeflectionAxis, font16Bold, FDTesterSingleton.Instance.PlotData.lvdtConfig.CalibrationData[CurrentLVDTCalibIndex].YDataName);
            int CurrentLoadCellCalibIndex = FDTesterSingleton.Instance.PlotData.loadCellConfig.CurrentTransducerUsed;
            ForceAxisTitle.SetAxisTitle(ForceAxis, font16Bold, FDTesterSingleton.Instance.PlotData.loadCellConfig.CalibrationData[CurrentLoadCellCalibIndex].YDataName);
            ChartView chartVu = this;
            chartVu.UpdateDraw();
        }

        class SelectedPlot : IComparable<SelectedPlot>
        {
            public ChartPlot Plot
            {
                get;
                set;
            }
            public double NearestPointMinDistance
            {
                get;
                set;
            }
            /// <summary>
            /// Compares this Selected Plot's nearest point.GetNearestPointMinDistance to another plot's.
            /// </summary>
            /// <param name="other">The plot to compare this plot to.</param>
            /// <returns>-1 if this is smaller, 
            /// 0 if the are equal, or 
            /// 1 if this plots is greater.</returns>
            int IComparable<SelectedPlot>.CompareTo(SelectedPlot other)
            {
                if (other.NearestPointMinDistance > this.NearestPointMinDistance)
                    return -1;
                else if (other.NearestPointMinDistance == this.NearestPointMinDistance)
                    return 0;
                else
                    return 1;
            }
        }



        class CustomMove : MoveObj
        {
            ForceDeflectionPlot ForceDeflectionPlotObj = null;

            public CustomMove(ForceDeflectionPlot component)
                : base(component)
            {
                ForceDeflectionPlotObj = component;
            }

        }





        class CustomToolTip : DataToolTip
        {

            ForceDeflectionPlot ForceDeflectionPlotObj = null;

            public CustomToolTip(ForceDeflectionPlot component)
                : base(component)
            {
                ForceDeflectionPlotObj = component;
            }

            public override void OnMouseUp(MouseEventArgs mouseevent)
            {
                base.OnMouseUp(mouseevent);
                // Redraws the chart. Since the stockpanel object has not been added to the chart 
                // (using addChartObject) it will not be redrawn when the chart is redrawn        
                ForceDeflectionPlotObj.UpdateDraw();
            }

            public override void OnMouseDown(MouseEventArgs mouseevent)
            {

                //currStockPanel = GetTextTemplate();
                Point2D mousepos = new Point2D();
                mousepos.SetLocation(mouseevent.X, mouseevent.Y);
                base.OnMouseDown(mouseevent);

                ChartPlot selectedPlot = (ChartPlot)GetSelectedPlotObj();
                if (selectedPlot != null)
                {
                    NearestPointData nearestpoint = new NearestPointData();
                    int selectedindex = GetNearestPoint().GetNearestPointIndex();

                    PhysicalCoordinates transform = GetSelectedCoordinateSystem();

                    //currStockPanel.SetTextString("");
                    // Looking to the original arrays, because we just have the selectedindex, 

                    //get data from the point
                    double DistanceValue = selectedPlot.GetDataset().GetXDataValue(selectedindex);
                    double ForceValue = selectedPlot.GetDataset().GetYDataValue(0, selectedindex); //group value is 0 because we only have one Ydata point for each X data point.
                    String ForceObj = ChartSupport.NumToString(ForceValue, ChartObj.DECIMALFORMAT, 2, "");
                    String DistanceObj = ChartSupport.NumToString(DistanceValue, ChartObj.DECIMALFORMAT, 2, "");

                    //draw a text box relative to the data point selected
                    String thestring = "";
                    Font textFont = new Font("Microsoft Sans Serif", 10, FontStyle.Bold);
                    ChartText currStockPanel = new ChartText(transform, textFont, thestring, GetNearestPoint().NearestPoint.GetX(), GetNearestPoint().NearestPoint.GetY(), ChartObj.PHYS_POS);
                    currStockPanel.SetTextString(DistanceObj + " , " + ForceObj);
                    //currStockPanel.SetLocation(mousepos, ChartObj.DEV_POS);
                    Point2D nudgePoint = new Point2D(20, 10);  // distance to move in pixels 
                    currStockPanel.SetTextNudge(nudgePoint);
                    currStockPanel.SetTextBgMode(true);
                    currStockPanel.SetTextBgColor(Color.Yellow);
                    currStockPanel.SetXJust(ChartObj.JUSTIFY_CENTER);
                    currStockPanel.SetYJust(ChartObj.JUSTIFY_CENTER);
                    //currStockPanel.SetChartObjEnable(ChartObj.OBJECT_ENABLE);
                    ForceDeflectionPlotObj.AddChartObject(currStockPanel);                    
                    UndoStack.Add(currStockPanel);

                    //draw a marker at the point selected
                    Marker amarker = new Marker(transform, ChartObj.MARKER_BOX, GetNearestPoint().NearestPoint.GetX(), GetNearestPoint().NearestPoint.GetY(), 10, ChartObj.PHYS_POS);
                    ForceDeflectionPlotObj.AddChartObject(amarker);
                    UndoStack.Add(amarker);


                    // draw an arrow from the point to the text box
                    Arrow arrowshape = new Arrow();
                    arrowshape.ArrowScaleFactor = 1;
                    arrowshape.ArrowShaftLength = 40;
                    // Define the arrow location in ChartObj.PHYS_POS units                
                    // The arrow shape is defined using ChartObj.DEV_POS units, and its position is 
                    // defined using ChartObj.PHYS_POS units
                    ChartShape ArrowObject = new ChartShape(transform, arrowshape.GetArrowShape(), ChartObj.DEV_POS,
                      GetNearestPoint().NearestPoint.GetX(), GetNearestPoint().NearestPoint.GetY(), ChartObj.PHYS_POS, 0);
                    // Rotate arrow clockwise 60 degrees
                    ArrowObject.ShapeRotation = 225; // 90;// 270;
                    // Create a custom color for arrow that is transparent
                    Color arrowColor = Color.Olive;
                    ArrowObject.ChartObjAttributes = new ChartAttribute(arrowColor, 1, DashStyle.Dot, arrowColor);
                  //  ForceDeflectionPlotObj.AddChartObject(ArrowObject);
                    UndoStack.Add(ArrowObject);

                    //stockpanel.Add(currStockPanel);
                    //GetChartObjComponent().AddChartObject(stockpanel[stockpanel.IndexOf(currStockPanel)]);
                    ForceDeflectionPlotObj.UpdateDraw();

                }
            }
        }


        class CustomZeroToolTip : DataToolTip
        {

            ForceDeflectionPlot ForceDeflectionPlotObj = null;

            public CustomZeroToolTip(ForceDeflectionPlot component)
                : base(component)
            {
                ForceDeflectionPlotObj = component;
            }

            public override void OnMouseUp(MouseEventArgs mouseevent)
            {
                base.OnMouseUp(mouseevent);
                // Redraws the chart. Since the stockpanel object has not been added to the chart 
                // (using addChartObject) it will not be redrawn when the chart is redrawn        
                ForceDeflectionPlotObj.UpdateDraw();
            }

            public override void OnMouseDown(MouseEventArgs mouseevent)
            {

                //currStockPanel = GetTextTemplate();
                Point2D mousepos = new Point2D();
                mousepos.SetLocation(mouseevent.X, mouseevent.Y);
                base.OnMouseDown(mouseevent);
                ChartPlot selectedPlot = (ChartPlot)GetSelectedPlotObj();
                if (selectedPlot != null)
                {
                    if (mouseevent.Button == MouseButtons.Left)
                    {
                        const string message = "Are you sure that you would like to zero the force data on this plot ?";
                        const string caption = "Zeroing Force Data Choice";
                        var result = MessageBox.Show(message, caption,MessageBoxButtons.YesNo,MessageBoxIcon.Question);
                        // If the no button was pressed ...
                        if (result == DialogResult.No)
                        {
                            return;
                        }
                        else
                        {                  
                            NearestPointData nearestpoint = new NearestPointData();
                            int selectedindex = GetNearestPoint().GetNearestPointIndex();
                            PhysicalCoordinates transform = GetSelectedCoordinateSystem();
                            
                            // Looking to the original arrays, because we just have the selectedindex, 

                            //get data from the point
                            ExtendedRTSimpleSingleValuePlot selPlot = (ExtendedRTSimpleSingleValuePlot)selectedPlot;
                            int currentSelPlotIndex = selPlot.PlotIndex;

                            
                            double DistanceValue = selectedPlot.GetDataset().GetXDataValue(selectedindex);
                            double ForceValue = selectedPlot.GetDataset().GetYDataValue(0, selectedindex); //group value is 0 because we only have one Ydata point for each X data point.
                            String ForceObj = ChartSupport.NumToString(ForceValue, ChartObj.DECIMALFORMAT, 2, "");
                            String DistanceObj = ChartSupport.NumToString(DistanceValue, ChartObj.DECIMALFORMAT, 2, "");
                             

                            int NumbPoints = FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData[currentSelPlotIndex].NumberOfRecordedDataPoints;
                            // we are not using these averages due to errors
                            

                            ForceDeflectionPlotObj.ClearDrawnRecordedPlotData(currentSelPlotIndex);                            

                            for (int i= 0; i< NumbPoints; i++)
                            {
                                FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData[currentSelPlotIndex].ForceData[i] -= ForceValue;                               
                            }
                            FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData[currentSelPlotIndex].ForceOffset = ForceValue;

                            int numbsetsAvg = NumbPoints / AnalogInputConfigurationData.SamplesToRead;
                            double DisplacementAvg = 0.0;
                            double ForceAvg = 0.0;
                            for (int i=0;i<numbsetsAvg;i++)
                            {
                                DisplacementAvg = 0.0;
                                ForceAvg = 0.0;

                                for (int j = 0; j < AnalogInputConfigurationData.SamplesToRead; j++)
                                {
                                    DisplacementAvg += FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData[currentSelPlotIndex].DisplacementData[i * AnalogInputConfigurationData.SamplesToRead + j];
                                    ForceAvg += FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData[currentSelPlotIndex].ForceData[i * AnalogInputConfigurationData.SamplesToRead + j];
                                }

                                DisplacementAvg /= AnalogInputConfigurationData.SamplesToRead;
                                ForceAvg /= AnalogInputConfigurationData.SamplesToRead;
                                ForceDeflectionPlotObj.AddDrawnPlotDatapoint(currentSelPlotIndex,DisplacementAvg,ForceAvg);
                            }                                                                                                          
                                                 
                            



                            //draw a text box relative to the data point selected
                            String thestring = "";
                            Font textFont = new Font("Microsoft Sans Serif", 10, FontStyle.Bold);
                            ChartText currStockPanel = new ChartText(transform, textFont, thestring, DistanceValue, 0.0, ChartObj.PHYS_POS);
                            currStockPanel.SetTextString(DistanceObj + " , " + ForceObj);
                            //currStockPanel.SetLocation(mousepos, ChartObj.DEV_POS);
                            Point2D nudgePoint = new Point2D(40, -40);  // distance to move in pixels 
                            currStockPanel.SetTextNudge(nudgePoint);
                            currStockPanel.SetTextBgMode(true);
                            currStockPanel.SetTextBgColor(Color.Yellow);
                            currStockPanel.SetXJust(ChartObj.JUSTIFY_CENTER);
                            currStockPanel.SetYJust(ChartObj.JUSTIFY_CENTER);
                            //currStockPanel.SetChartObjEnable(ChartObj.OBJECT_ENABLE);
                            //  GetChartObjComponent().AddChartObject(currStockPanel);
                            //  UndoStack.Add(currStockPanel);

                            //draw a marker at the point selected
                            Marker amarker = new Marker(transform, ChartObj.MARKER_BOX, DistanceValue, 0.0, 10, ChartObj.PHYS_POS);
                            GetChartObjComponent().AddChartObject(amarker);
                            UndoStack.Add(amarker);
                            ForceDeflectionPlotObj.AddLegendForceOffset(currentSelPlotIndex);
                                


                           

                            //stockpanel.Add(currStockPanel);
                            //GetChartObjComponent().AddChartObject(stockpanel[stockpanel.IndexOf(currStockPanel)]);
                                GetChartObjComponent().UpdateDraw();
                        }   
                    }            
                }
            }
        
        
        
        }






        public void AddTextToChart(String TextString, Font userFont, Int32 TextAngle, bool TestStamp = false, double XPos = 0.0, double YPos = 0.0)
        {
            //Send the Text Box to the middle of the plot
            double XAxisMiddlePhyPos; 
            double YAxisMiddlePhyPos;
            if (TestStamp == true)
            {               
                XAxisMiddlePhyPos = XPos;
                YAxisMiddlePhyPos = YPos;
            }
            else
            {
                XAxisMiddlePhyPos = (FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMin + FDTesterSingleton.Instance.PlotData.TestData.DeflectionAxisMax) / 2;
                YAxisMiddlePhyPos = (FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMin + FDTesterSingleton.Instance.PlotData.TestData.ForceAxisMax) / 2;
            }
            FreePlotText UserText = new FreePlotText(pTransform, userFont, TextString, XAxisMiddlePhyPos, YAxisMiddlePhyPos, GraphObj.PHYS_POS, GraphObj.JUSTIFY_MIN, GraphObj.JUSTIFY_CENTER, TextAngle);
            //UserText.SetChartObjEnable(ChartObj.OBJECT_ENABLE);
            ChartView chartVu = this;
            chartVu.AddChartObject(UserText);
            UndoStack.Add(UserText);
            DisableUserInteractivity();            
            chartVu.UpdateDraw();
        }


        public void InitializeChart()
        {
            InitializePlotSettings();
            RedrawPlotSettings();

        }

        public void DisableUserInteractivity()
        {
            LiveDataMode(false);
            zoomObj.SetEnable(false);
            stocktooltip.SetEnable(false);
            UserMoveObj.SetEnable(false);
            Zerotooltip.SetEnable(false);
            ChartView chartVu = this;
            chartVu.ResetMouseListeners();
        }

        public void DisableMouseListeners()
        {
            zoomObj.SetEnable(false);
            stocktooltip.SetEnable(false);
            UserMoveObj.SetEnable(false);
            Zerotooltip.SetEnable(false);
            ChartView chartVu = this;
            chartVu.ResetMouseListeners();

        }
        public void EnableZoom()
        {
            LiveDataMode(false);
            DisableUserInteractivity();
            ChartView chartVu = this;
            zoomObj.SetEnable(true);
            zoomObj.InternalZoomStackProcesssing = true;
            chartVu.SetCurrentMouseListener(zoomObj);
        }

        public void EnableDataPointDraw()
        {
            LiveDataMode(false);
            DisableUserInteractivity();
            ChartView chartVu = this;
            stocktooltip.SetEnable(true);
            chartVu.SetCurrentMouseListener(stocktooltip);
            //DataCursor dataCursorObj = new DataCursor(chartVu, pTransform,
            //    ChartObj.MARKER_HVLINE, 8.0);

            //dataCursorObj.SetEnable(true);
            //chartVu.AddMouseListener(dataCursorObj);
        }

        public void EnableUserMove()
        {
            LiveDataMode(false);
            DisableUserInteractivity();
            ChartView chartVu = this;
            UserMoveObj.SetMoveObjectFilter("ChartText");
            UserMoveObj.SetEnable(true);
            chartVu.SetCurrentMouseListener(UserMoveObj);
        }

        public void EnableUserMoveText()
        {
            LiveDataMode(false);
            DisableUserInteractivity();
            ChartView chartVu = this;
            UserMoveObj.SetEnable(true);
            //UserMoveObj.SetMoveObjectFilter("FreePlotText");
            //UserMoveObj.SetMoveObjectFilter("ChartText");
            UserMoveObj.SetMoveObjectFilter("ChartObj");
            chartVu.SetCurrentMouseListener(UserMoveObj);
        }
        public void EnablePlotForceCoordinateZero()
        {
            LiveDataMode(false);
            DisableUserInteractivity();
            ChartView chartVu = this;
            Zerotooltip.SetEnable(true);
            chartVu.SetCurrentMouseListener(Zerotooltip);
        }

        public void ToggleLiveMode()
        {
            if (this.ShowLiveDataTimer.Enabled == true)
            {
                LiveDataMode(false);
            }
            else
            {
                LiveDataMode(true);
            }
        }

        /// <summary>
        /// Disable Live Plot button
        /// Start Data Collection into recorded plots
        /// </summary>
        public void StartNewPlotRecording(Color NewPlotColor)
        {
            LiveDataMode(false);
            plotState = PlottingState.enPlotRecord;
            int currentPlotIndex = FDTesterSingleton.Instance.PlotData.TestData.currentPlotNumber;
            FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData[currentPlotIndex].InitializeNewPlot();
            DrawRTPlotsData[currentPlotIndex].TruncateProcessVarDataset(0); // clears the data set
            DrawRTPlots[currentPlotIndex].ChartObjEnable = GraphObj.OBJECT_ENABLE;
            DrawRTPlots[currentPlotIndex].PlotIndex = currentPlotIndex;
            DrawLinePlots[currentPlotIndex].LineColor = NewPlotColor;                
            FDTesterSingleton.Instance.PlotData.TestData.numberOfPlotRecordingStarted++;
            this.RecordPlotTimer.Enabled = true;
            DisableMouseListeners();                
            StartAnalogDataCollection();
        }

        private void ClearDrawnRecordedPlotData(int index)
        {
            DrawRTPlotsData[index].TruncateProcessVarDataset(0); // clears the data set

        }
        private void AddDrawnPlotDatapoint(int index, double DisplacementAvg, double ForceAvg)
        {
            DrawRTPlotsData[index].SetCurrentValue(DisplacementAvg, ForceAvg); 
        }

        public void EndNewPlotRecording()
        {
            plotState = PlottingState.enPlotOff;
            FDTesterSingleton.Instance.PlotData.TestData.numberOfPlotRecordingCompleted++;
            this.RecordPlotTimer.Enabled = false;
            StopAnalogDataCollection();
            AddLegend();
        }
        public void PrintSaveAsFile()
        {
            ChartView chartview = this;
            String filename = this.Name;
            SaveFileDialog imagefilechooser = new SaveFileDialog();
            imagefilechooser.Filter =
                "Image Files(*.BMP;*.JPG;*.GIF;*.TIFF;*.PNG)|*.BMP;*.JPG;*.GIF;*.TIFF;*.PNG|All files (*.*)|*.*";
            imagefilechooser.FileName = filename;
            if (imagefilechooser.ShowDialog() == DialogResult.OK)
            {
                filename = imagefilechooser.FileName;
                FileInfo fileinformation = new FileInfo(filename);
                String fileext = fileinformation.Extension;
                fileext = fileext.ToUpper();
                ImageFormat fileimageformat;
                if (fileext == ".BMP")
                    fileimageformat = ImageFormat.Bmp;
                else if ((fileext == ".JPG") || (fileext == ".JPEG"))
                    fileimageformat = ImageFormat.Jpeg;
                else if ((fileext == ".GIF"))
                    fileimageformat = ImageFormat.Gif;
                else if ((fileext == ".TIF") || (fileext == ".TIFF"))
                    fileimageformat = ImageFormat.Tiff;
                else if ((fileext == ".PNG"))
                    fileimageformat = ImageFormat.Png;
                else
                    fileimageformat = ImageFormat.Bmp;

                BufferedImage savegraph = new BufferedImage(chartview, fileimageformat);
                savegraph.Render();
                savegraph.SaveImage(filename);
            }
        }

        public void LiveDataMode(bool Enabled)
        {
            if (Enabled == true)
            {
                plotState = PlottingState.enPlotLive;
                this.ShowLiveDataTimer.Enabled = true;
                DrawRTLivePlots.ChartObjEnable = GraphObj.OBJECT_ENABLE;
                StartAnalogDataCollection();
            }
            else
            {
                plotState = PlottingState.enPlotOff;
                this.ShowLiveDataTimer.Enabled = false;
                DrawRTLivePlots.ChartObjEnable = GraphObj.OBJECT_DISABLE;
                DrawRTLiveData.TruncateProcessVarDataset(0); // clears the data set    
                StopAnalogDataCollection();
            }
        }








        //ChartAttribute attrib3 = new ChartAttribute(Color.LightGray, 1, DashStyle.DashDot);

        //inputChannel3 = new RTProcessVar("Ch #3", new ChartAttribute(Color.Green, 1.0, DashStyle.Solid, Color.Green));
        //inputChannel3.MinimumValue = 0;
        //inputChannel3.MaximumValue = 10;
        //inputChannel3.DefaultMinimumDisplayValue = 0;
        //inputChannel3.DefaultMaximumDisplayValue = 10;
        //   inputChannel3.SetCurrentValue(inputChannelValue1);
        //for (int i = 0; i <= 10; i++)
        //{
        //    inputChannelValue3 = 3 + Math.Abs(2 * Math.Sin(Math.PI * 2 * i * 36 / 180));
        //    inputChannel3.SetCurrentValue(i, inputChannelValue3);
        //} 
        // Important, enables historical data collection for scroll graphs
        //inputChannel3.DatasetEnableUpdate = true;
        //inputChannel3.TruncateProcessVarDataset(10);
        //SimpleLinePlot lineplot3 = new SimpleLinePlot(pTransform3, null, attrib3);
        //lineplot3.SetFastClipMode(ChartObj.FASTCLIP_X);
        //rtPlot3 = new RTSimpleSingleValuePlot(pTransform3, lineplot3, inputChannel3);
        //chartVu.AddChartObject(rtPlot3);


        //for (int i = 0; i < 4; i++)
        //{
        //    ChartText somerandomText = new ChartText();
        //    somerandomText.SetLocation(27 * i, 32 * i, ChartObj.DEV_POS);
        //    somerandomText.SetTextString("What is this?");
        //    somerandomText.SetChartObjEnable(ChartObj.OBJECT_ENABLE);
        //    chartVu.AddChartObject(somerandomText);
        //    //stockpanel.Add(somerandomText);
        //    //chartVu.AddChartObject(stockpanel[stockpanel.Count-1]);
        //}
        //ChartText somerandomText2 = new ChartText(); 
        //for (int i = 0; i < 10; i++)
        //{
        //    somerandomText2.SetLocation(34 * i, 50 * i, ChartObj.DEV_POS);
        //    somerandomText2.SetTextString("What is this?");
        //    somerandomText2.SetChartObjEnable(ChartObj.OBJECT_ENABLE);
        //    stockpanel.Add(somerandomText2);
        //    chartVu.AddChartObject(stockpanel[stockpanel.Count - 1]);
        //}










        //     StartAnalogDataCollection();

        private void ShowLiveDataTimer_Tick(object sender, EventArgs e)
        {
            ChartView chartVu = this;
            if (DrawRTLivePlots.GetDataset().NumberDatapoints > PlotPersistentData.MaxDataPointsPerPlot)
            {
                DrawRTLiveData.TruncateProcessVarDataset(PlotPersistentData.MaxDataPointsPerPlot);
            }
            chartVu.UpdateDraw();
        }

        public void StartAnalogDataCollection()
        {
            if (runningTask == null)
            {
                try
                {
                    // Create a new task
                    myTask = new Task();

                    // Create a virtual channel
                    myTask.AIChannels.CreateVoltageChannel(AnalogInputConfigurationData.DAQChannelStringStripDisc, "",
                        (AITerminalConfiguration)(-1), AnalogInputConfigurationData.analogChMinInputVolts, AnalogInputConfigurationData.analogChMaxInputVolts, AIVoltageUnits.Volts);

                    //  Configure the timing parameters
                    myTask.Timing.ConfigureSampleClock("", AnalogInputConfigurationData.SamplesPerSecond,
                        SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, AnalogInputConfigurationData.A2DBufferSize);

                    // Verify the Task 
                    myTask.Control(TaskAction.Verify);


                    runningTask = myTask;
                    analogInReader = new AnalogMultiChannelReader(myTask.Stream);
                    analogCallback = new AsyncCallback(AnalogInCallback);


                    analogInReader.SynchronizeCallbacks = true;
                    analogInReader.BeginReadWaveform(AnalogInputConfigurationData.SamplesToRead, analogCallback, myTask);

                }
                catch (DaqException exception)
                {
                    // Display Errors
                    MessageBox.Show(exception.Message);
                    runningTask = null;
                    myTask.Dispose();

                }
            }
        }

        public void StopAnalogDataCollection()
        {
            if (runningTask != null)
            {
                // Dispose of the task
                runningTask = null;
                myTask.Dispose();
            }
        }


        private void AnalogInCallback(IAsyncResult ar)
        {
            try
            {
                if (runningTask == ar.AsyncState)
                {

                    // Read the available data from the channels
                    data = analogInReader.EndReadWaveform(ar);
                    int currentRec = FDTesterSingleton.Instance.PlotData.TestData.recordedPlotData[FDTesterSingleton.Instance.PlotData.TestData.currentPlotNumber].NumberOfRecordedDataPoints;
                    int currentPlotNumb = FDTesterSingleton.Instance.PlotData.TestData.currentPlotNumber;
                    double DisplacementSum = 0.0; double DisplacementAvg = 0.0;
                    double ForceSum = 0.0; double ForceAvg = 0.0;
                    double ScaledDisplacementValue = 0.0;
                    double ScaledForceValue = 0.0;
                    double RawDisplacementVolts = 0.0;
                    double RawForceVolts = 0.0;
                    // 1. Scale the values based on scaling set up 
                    // 2. Coordiante Transform the scaled data to the zero point established by the user at the begining of the test
                    // 3. Record the scaled and zeroed values into the Plot Data, but average out for graphical plotting. Not all points in the plot data structure will be rendered on the screen.

                    FDTesterPersistentData PlotData = FDTesterSingleton.Instance.PlotData;
                    for (int i = 0; i < AnalogInputConfigurationData.SamplesToRead; i++)
                    {
                        RawDisplacementVolts += data[AnalogInputConfigurationData.DisplacementChannel].Samples[i].Value;
                        ScaledDisplacementValue = PlotData.lvdtConfig.ScaledValue(data[AnalogInputConfigurationData.DisplacementChannel].Samples[i].Value);
                        ScaledDisplacementValue -= PlotData.TestData.DeflectionAxisZeroValueScaled;
                        DisplacementSum += ScaledDisplacementValue;

                        RawForceVolts += data[AnalogInputConfigurationData.ForceChannel].Samples[i].Value;
                        ScaledForceValue = PlotData.loadCellConfig.ScaledValue(data[AnalogInputConfigurationData.ForceChannel].Samples[i].Value);
                        ScaledForceValue -= PlotData.TestData.ForceAxisZeroValueScaled;
                        ForceSum += ScaledForceValue;

                        switch (plotState)
                        {
                            case PlottingState.enPlotRecord:
                                if ((currentRec + i) < PlotPersistentData.MaxDataPointsPerPlot)
                                {
                                    PlotData.TestData.recordedPlotData[currentPlotNumb].DisplacementData[currentRec + i] = ScaledDisplacementValue;
                                    PlotData.TestData.recordedPlotData[currentPlotNumb].ForceData[currentRec + i] = ScaledForceValue;
                                    PlotData.TestData.recordedPlotData[currentPlotNumb].TimestampData[currentRec + i] = data[AnalogInputConfigurationData.ForceChannel].Samples[i].TimeStamp;
                                    PlotData.TestData.recordedPlotData[currentPlotNumb].NumberOfRecordedDataPoints++;
                                }
                                break;
                            case PlottingState.enPlotOff:
                                break;
                            case PlottingState.enPlotLive:
                                break;
                        }
                    }
                    // 4. Add averaged out data points to the RTPlots for rendering on screen
                    Random randomVal = new Random();
                    DisplacementAvg = DisplacementSum / AnalogInputConfigurationData.SamplesToRead;// +10 * currentPlotNumb * randomVal.NextDouble();// * 10 + 50;
                    ForceAvg = ForceSum / AnalogInputConfigurationData.SamplesToRead;// +10 * currentPlotNumb * randomVal.NextDouble();// * 10 + 50;
                    RawDisplacementVolts /= AnalogInputConfigurationData.SamplesToRead;
                    RawForceVolts /= AnalogInputConfigurationData.SamplesToRead;
                    switch (plotState)
                    {
                        case PlottingState.enPlotRecord:
                            DrawRTPlotsData[currentPlotNumb].SetCurrentValue(DisplacementAvg, ForceAvg);    

                            //use this to update the live numeric values on the display
                       
                            PlotData.livePlotData.DisplacementData[(int)PlotPersistentData.ReadingType.enScaled] = DisplacementAvg;
                            PlotData.livePlotData.ForceData[(int)PlotPersistentData.ReadingType.enScaled] = ForceAvg;
                            PlotData.livePlotData.DisplacementData[(int)PlotPersistentData.ReadingType.enRawVolts] = RawDisplacementVolts;
                            PlotData.livePlotData.ForceData[(int)PlotPersistentData.ReadingType.enRawVolts] = RawForceVolts;

                            break;
                        case PlottingState.enPlotOff:
                            break;
                        case PlottingState.enPlotLive:
                            DrawRTLiveData.SetCurrentValue(DisplacementAvg, ForceAvg);
                            PlotData.livePlotData.DisplacementData[(int)PlotPersistentData.ReadingType.enScaled] = DisplacementAvg;
                            PlotData.livePlotData.ForceData[(int)PlotPersistentData.ReadingType.enScaled] = ForceAvg;
                            PlotData.livePlotData.DisplacementData[(int)PlotPersistentData.ReadingType.enRawVolts] = RawDisplacementVolts;
                            PlotData.livePlotData.ForceData[(int)PlotPersistentData.ReadingType.enRawVolts] = RawForceVolts;
                            break;
                    }
                    analogInReader.BeginMemoryOptimizedReadWaveform(AnalogInputConfigurationData.SamplesToRead, analogCallback, myTask, data);
                }
            }
            catch (DaqException exception)
            {
                // Display Errors
                MessageBox.Show(exception.Message);
                runningTask = null;
                myTask.Dispose();

            }
        }

        private void RecordPlotTimer_Tick(object sender, EventArgs e)
        {
            ChartView chartVu = this;
            chartVu.UpdateDraw();
        }

    }


}
