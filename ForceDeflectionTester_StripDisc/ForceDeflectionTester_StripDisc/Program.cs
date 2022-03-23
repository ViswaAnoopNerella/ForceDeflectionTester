using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using com.quinncurtis.chart2dnet;
using com.quinncurtis.rtgraphnet;
using System.Text;

namespace ForceDeflectionTester_StripDisc
{

    [Serializable()]
    public class CalibrationInfo
    {
        public const Int32 MaxCalPoints = 10;
        public double[] CalibrationXData = new double[MaxCalPoints];
        public double[] CalibrationYData = new double[MaxCalPoints];
        public Int32 numberCalibrationPointsUsed;
        public double Slope;
        public double Offset;
        public DateTime CalibrationDateTime;
        public String CalibrationOperator;
        public String CalibrationFileName;
        public String XDataName
        {
            get;
            set;
        }
        public String YDataName
        {
            get;
            set;
        }
        public String TransducerSerialNumber;

        public CalibrationInfo()
        {
            for (int i = 0; i < MaxCalPoints; i++)
            {
                CalibrationXData[i] = new double();
                CalibrationYData[i] = new double();
            }
            ResetCalibrationInfo();
        }
        public void ResetCalibrationInfo()
        {
            for (int i = 0; i < MaxCalPoints; i++)
            {
                CalibrationXData[i] = 0.0;
                CalibrationYData[i] = 0.0;
            }
            numberCalibrationPointsUsed = 0;
            Slope = 1;
            Offset = 0;
        }
    }


    [Serializable()]
    public class AnalogInputConfigurationData
    {
        public const Int32 SamplesPerSecond = 100; //sampling rate
        public const Int32 SamplesToRead = 5; //this number will drive the rate at which the callback function will be called (used in relation with the sampling rate)
        public const Int32 MaxTransducersCalibStorage = 2;
        public const Int32 DisplacementChannel = 0;
        public const Int32 ForceChannel = 1;
        public const String DAQChannelStringStripDisc = "CDAQ1Mod2/ai1,CDAQ1Mod2/ai3";
        public const double analogChMaxInputVolts = 10.0;
        public const double analogChMinInputVolts = -10.0;
        public const Int32 A2DBufferSize = 10000; //buffer to hold continuous data before data is removed from the callback function. 
        public CalibrationInfo[] CalibrationData = new CalibrationInfo[MaxTransducersCalibStorage];
        private Int32 currTransducer;
        public Int32 CurrentTransducerUsed
        {
            get
            {
                return currTransducer;
            }
        }
        public void AddNewTransducer()
        {
            currTransducer++;
            if (currTransducer > (MaxTransducersCalibStorage - 1))
            {
                currTransducer = 0;
            }
        }
        public void ResetTransducersUsedCount()
        {
            currTransducer = 0;
        }

        public AnalogInputConfigurationData(String XdataName, String YdataName)
        {
            ResetTransducersUsedCount();
            for (int i = 0; i < MaxTransducersCalibStorage; i++)
            {
                CalibrationData[i] = new CalibrationInfo();
                CalibrationData[i].XDataName = XdataName;
                CalibrationData[i].YDataName = YdataName;
            }
        }
        public double ScaledValue(double Reading)
        {
            double ScaledValue = CalibrationData[currTransducer].Slope * Reading + CalibrationData[currTransducer].Offset;
            return ScaledValue;
        }
    }


    [Serializable()]
    public class PlotPersistentData
    {
        public const Int32 MaxDataPointsPerPlot = 50000;
        public int NumberOfRecordedDataPoints;
        public double[] DisplacementData = new double[MaxDataPointsPerPlot];
        public double[] ForceData = new double[MaxDataPointsPerPlot];
        public DateTime[] TimestampData = new DateTime[MaxDataPointsPerPlot];
        public double ForceOffset;
        public double Temperature;
        public TimeSpan PlotRecordInterval;
        public enum ReadingType { enRawVolts, enScaled };
        /// <summary>
        /// Allow for clearing out plot data when starting new tests
        /// </summary>
        public void InitializeNewPlot()
        {
            for (int j = 0; j < MaxDataPointsPerPlot; j++)
            {
                DisplacementData[j] = 0.0;
                ForceData[j] = 0.0;
                TimestampData[j] = DateTime.Now;
                ForceOffset = 0.0;
                //Temperature = 0.0;  this is coming from user input
            }
            NumberOfRecordedDataPoints = 0;
        }
        public PlotPersistentData()
        {
            for (int j = 0; j < MaxDataPointsPerPlot; j++)
            {
                DisplacementData[j] = new double();
                ForceData[j] = new double();
                TimestampData[j] = new DateTime();
            }
        }
    }

    [Serializable()]
    public class TestStampElement
    {
        public String Name
        {
            get;
            set;
        }
        public String Value;
        public TestStampElement(String elementName)
        {
            Name = elementName;
        }

    }

    [Serializable()]
    public class TestPersistentData
    {
        public const int MaxPlotsPerTest = 10;
        public int numberOfPlotRecordingStarted;
        public int numberOfPlotRecordingCompleted;
        public int currentPlotNumber;
        public int[] GraphObjChartObjEnable = new int[MaxPlotsPerTest];
        public PlotPersistentData[] recordedPlotData = new PlotPersistentData[MaxPlotsPerTest];
        public DateTime TestStartTime;
        public DateTime TestEndTime;
        public TestStampElement TestName;
        public TestStampElement TestDate;
        public TestStampElement TestNumber;
        public TestStampElement LotNumber;
        public TestStampElement BiMetal;
        public TestStampElement HeatTreat;
        public TestStampElement ProductName;
        public TestStampElement DiscNumber;
        public TestStampElement DiscTemps;
        public TestStampElement XScale;
        public TestStampElement YScale;
        public TestStampElement Plotter;
        public TestStampElement ProbeNumber;
        public TestStampElement Notes;
        public List<TestStampElement> testStampList;
        public double DeflectionAxisZeroValueScaled;
        public double ForceAxisZeroValueScaled;



        #region PlottingProperties

        public double DeflectionAxisMin;
        public double DeflectionAxisMax;
        public double ForceAxisMin;
        public double ForceAxisMax;
        public double DeflectionAxisInterceptPhysCord;
        public double ForceAxisInterceptPhysCord;
        public double DeflectionAxisTickOrigin;
        public double DeflectionAxisTickSpace;
        public int DeflectionAxisTicksPerMajor;
        public double ForceAxisTickOrigin;
        public double ForceAxisTickSpace;
        public int ForceAxisTicksPerMajor;


        #endregion

        public void InitializeNewTest()
        {
            numberOfPlotRecordingStarted = 0;
            numberOfPlotRecordingCompleted = 0;
            currentPlotNumber = 0;
            for (int i = 0; i < MaxPlotsPerTest; i++)
            {
                recordedPlotData[i].InitializeNewPlot();
                GraphObjChartObjEnable[i] = GraphObj.OBJECT_DISABLE;
            }
            
           // DeflectionAxisZeroValueScaled = 0.0;
           // ForceAxisZeroValueScaled = 0.0;

            TestStartTime = DateTime.Now;
            TestEndTime = DateTime.Now;
        }
        public TestPersistentData()
        {
            testStampList = new List<TestStampElement>();
            
            TestName = new TestStampElement("Technician Name");
            testStampList.Add(TestName);

            TestDate = new TestStampElement("Test Date");
            testStampList.Add(TestDate);
            
            TestNumber = new TestStampElement("Test Number");
            testStampList.Add(TestNumber);

            LotNumber = new TestStampElement("Lot Number");
            testStampList.Add(LotNumber);

            BiMetal = new TestStampElement("BiMetal");
            testStampList.Add(BiMetal);

            HeatTreat = new TestStampElement("Heat Treat");
            testStampList.Add(HeatTreat);

            ProductName = new TestStampElement("Product Name");
            testStampList.Add(ProductName);

            DiscNumber = new TestStampElement("Sample Number");
            testStampList.Add(DiscNumber);

            DiscTemps = new TestStampElement("Disc Temps");
            testStampList.Add(DiscTemps);

            XScale = new TestStampElement("XScale");
            testStampList.Add(XScale);

            YScale = new TestStampElement("YScale");
            testStampList.Add(YScale);

            Plotter = new TestStampElement("Equipment");
            testStampList.Add(Plotter);

            ProbeNumber = new TestStampElement("Probe Number");
            testStampList.Add(ProbeNumber);

            Notes = new TestStampElement("Notes");
            testStampList.Add(Notes);

            for (int i = 0; i < MaxPlotsPerTest; i++)
            {
                recordedPlotData[i] = new PlotPersistentData();
                GraphObjChartObjEnable[i] = new int();
            }
            DeflectionAxisMin = -10;
            DeflectionAxisMax = 10;
            ForceAxisMin = -10;
            ForceAxisMax = 10;
            DeflectionAxisInterceptPhysCord = ForceAxisMin;
            ForceAxisInterceptPhysCord = DeflectionAxisMin;
            DeflectionAxisTickOrigin = DeflectionAxisMin;
            DeflectionAxisTickSpace = (DeflectionAxisMax - DeflectionAxisMin) / 100;
            DeflectionAxisTicksPerMajor = 10;
            ForceAxisTickOrigin = ForceAxisMin;
            ForceAxisTickSpace = (ForceAxisMax - ForceAxisMin) / 100;
            ForceAxisTicksPerMajor = 10;
        }
    }
    [Serializable()]
    public class TemperatureBathSerialPortSettings
    {
        public string setting_BaudRate;
        public System.IO.Ports.Parity setting_Parity;
        public System.IO.Ports.StopBits setting_StopBits;
        public string setting_DataBits;
        public string setting_PortName;
        public TemperatureBathSerialPortSettings(String portname)
        {
            setting_BaudRate = "9600";
            setting_Parity = System.IO.Ports.Parity.None;
            setting_StopBits = System.IO.Ports.StopBits.One;
            setting_DataBits = "8";
            setting_PortName = portname;
        }
    }
    public class TemperatureBath
    {
        private ManualResetEventSlim synch_event;
        public enum SerialIOAction { enReadPV, enReadSP, enReadPV_SP, enWriteSP };
        private SerialIOAction BathAction;
        private double newSetpoint;
        private bool bTerminateTaskThread;
        private Int32 bathindex;
        private SerialPort comPort;
        private const Int32 READ_TIMEOUT = 5000; // five seconds timeout for bath to respond

        public TemperatureBath(int index)
        {
            synch_event = new ManualResetEventSlim(false); // initialize as unsignaled
            bTerminateTaskThread = false;
            bathindex = index;
            newSetpoint = 0.0;
            comPort = new SerialPort();
            try
            {
                if (comPort.IsOpen == true) comPort.Close();

                //set the properties of our SerialPort Object
                comPort.BaudRate = int.Parse(FDTesterSingleton.Instance.PlotData.BathsData[bathindex].ioSerial.setting_BaudRate);    //BaudRate
                comPort.DataBits = int.Parse(FDTesterSingleton.Instance.PlotData.BathsData[bathindex].ioSerial.setting_DataBits);    //DataBits
                comPort.StopBits = FDTesterSingleton.Instance.PlotData.BathsData[bathindex].ioSerial.setting_StopBits;               //StopBits
                comPort.Parity = FDTesterSingleton.Instance.PlotData.BathsData[bathindex].ioSerial.setting_Parity;                   //Parity
                comPort.PortName = FDTesterSingleton.Instance.PlotData.BathsData[bathindex].ioSerial.setting_PortName;               //PortName
                comPort.ReadTimeout = READ_TIMEOUT;
                comPort.Handshake = Handshake.None;
                comPort.DtrEnable = true;
                comPort.RtsEnable = true;
                char carriageRet = '\x0d';
                comPort.NewLine = carriageRet.ToString();
                comPort.Open();
            }
            catch (System.Exception ex)
            {
               // MessageBox.Show(ex.Message);
            }

        }
        public void TerminateTaskThread()
        {
            bTerminateTaskThread = true;
        }

        public bool ReadPV_SP()
        {
            if (!IsTaskDone())
            {
                return false;
            }
            BathAction = SerialIOAction.enReadPV_SP;
            TriggerTask();
            return true;
        }
        public bool WriteSP(double setpoint_value)
        {
            if (!IsTaskDone())
            {
                return false;
            }
            BathAction = SerialIOAction.enWriteSP;
            newSetpoint = setpoint_value;
            TriggerTask();
            return true;
        }
        public bool IsTaskDone()
        {
            return !(synch_event.IsSet);
        }
        public void TriggerTask()
        {
            synch_event.Set();
        }
        public void RunAsynchSerialIOThread()
        {
            bTerminateTaskThread = false;
            String InputRead;
            String Value;
            char[] Units = { 'c', 'C', 'k', 'K' };

            var observer = Task.Factory.StartNew(() =>
            {
                while (!bTerminateTaskThread)
                {
                    synch_event.Wait();
                    switch (BathAction)
                    {
                        case SerialIOAction.enReadPV_SP:
                            try
                            {
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].LastAttemptedReadingTime = DateTime.Now;
                                comPort.DiscardInBuffer();
                                comPort.DiscardOutBuffer();
                                comPort.WriteLine("RT ");
                                InputRead = comPort.ReadLine();
                                Value = InputRead.TrimEnd(Units);
                                double Reading = double.Parse(Value);
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].LastValidReading = Reading;
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].LastValidReadingTime = DateTime.Now;
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].LastCommStatus = TemperatureBathPersistentData.LastSerialCommStatus.enCommOK;
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].CurrentReading = Reading;
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].RunningSumReadings += Reading;
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].NumbValidReadings++;
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].AverageTemperature = FDTesterSingleton.Instance.PlotData.BathsData[bathindex].RunningSumReadings / FDTesterSingleton.Instance.PlotData.BathsData[bathindex].NumbValidReadings++;
                                comPort.DiscardInBuffer();
                                comPort.DiscardOutBuffer();
                                comPort.WriteLine("RS ");
                                InputRead = comPort.ReadLine();
                                Value = InputRead.TrimEnd(Units);
                                Reading = double.Parse(Value);
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].SetpointRead = Reading;
                            }
                            catch (System.Exception ex)
                            {
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].LastCommStatus = TemperatureBathPersistentData.LastSerialCommStatus.enCommFail;
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].CurrentReading = -1.0;
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].RunningSumReadings = 0;
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].NumbValidReadings = 0;
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].AverageTemperature = -1.0;
                            }
                            break;
                        case SerialIOAction.enWriteSP:
                            try
                            {
                                comPort.DiscardInBuffer();
                                comPort.DiscardOutBuffer();
                                string sendMsg = "SS " + newSetpoint.ToString();
                                comPort.WriteLine(sendMsg);
                                InputRead = comPort.ReadLine();
                                bool bSucc = (InputRead.Contains("OK") || InputRead.Contains("ok"));
                                if (bSucc == true)
                                {
                                    FDTesterSingleton.Instance.PlotData.BathsData[bathindex].Setpoint = newSetpoint;
                                }
                            }
                            catch (System.Exception ex)
                            {
                                FDTesterSingleton.Instance.PlotData.BathsData[bathindex].LastCommStatus = TemperatureBathPersistentData.LastSerialCommStatus.enCommFail;
                            }

                            break;
                    }
                    synch_event.Reset();

                }
            });

        }
    }
    [Serializable()]
    public class TemperatureBathPersistentData
    {
        public String BathName;
        public Int32 BathIndex;
        public double Setpoint;
        public double SetpointRead;
        public double SetpointTolerance;
        public TimeSpan MinTimeInZoneForStable; //minutes
        public TimeSpan TimeinStableZone;
        public double LastValidReading;
        public DateTime LastValidReadingTime;
        public DateTime LastAttemptedReadingTime;
        public enum LastSerialCommStatus { enCommOK, enCommFail };
        public LastSerialCommStatus LastCommStatus;
        public double AverageTemperature;
        public Int64 NumbValidReadings;
        public double RunningSumReadings;
        public double CurrentReading;
        public String PanelStatus;
        public String comPortName;
        public TemperatureBathSerialPortSettings ioSerial;
        public TemperatureBathPersistentData(String bath, Int32 index, int portNum)
        {
            BathName = bath;
            BathIndex = index;
            Setpoint = 0;
            SetpointRead = 0;
            SetpointTolerance = 1.0;
            MinTimeInZoneForStable = TimeSpan.FromMinutes(15.0);
            LastValidReading = -1.0;
            AverageTemperature = -1.0;
            CurrentReading = 0.0;
            NumbValidReadings = 0;
            RunningSumReadings = 0.0;
            PanelStatus = "No Comms";            
            comPortName = "COM" + portNum.ToString();
            ioSerial = new TemperatureBathSerialPortSettings(comPortName);
        }
    }
    [Serializable()]
    public class FDTesterPersistentData
    {
        public const int NumbBaths = 5;

        public TestPersistentData TestData;
        public PlotPersistentData livePlotData;
        public AnalogInputConfigurationData loadCellConfig;
        public AnalogInputConfigurationData lvdtConfig;
        public TemperatureBathPersistentData[] BathsData = new TemperatureBathPersistentData[NumbBaths];

        public FDTesterPersistentData()
        {
            //test and recorded plots initialization
            TestData = new TestPersistentData();
            TestData.InitializeNewTest();

            //live plot initialization
            livePlotData = new PlotPersistentData();
            livePlotData.InitializeNewPlot();

            // load cell 
            loadCellConfig = new AnalogInputConfigurationData("Volts (V)", "Force (grams)");

            //lvdt 
            lvdtConfig = new AnalogInputConfigurationData("Volts (V)", "Deflection (mil)");
            for (int i = 0; i < NumbBaths; i++)
            {
                string bathname = "Bath# " + i.ToString();
				//switch (i)
				//{
				//    case 0:
				//        BathsData[i] = new TemperatureBathPersistentData(bathname, i,2); //Bath # 1 = COM2
				//        break;
				//    default:
				//        BathsData[i] = new TemperatureBathPersistentData(bathname, i, 6+i); //Bath # 2 = COM7, Bath # 3 = COM8, Bath # 4 = COM9, Bath # 5 = COM10 
				//        break;
				//}
				BathsData[i] = new TemperatureBathPersistentData(bathname, i, 5 + i);
				//Bath # 1 = COM5, Bath # 2 = COM6, Bath # 3 = COM7, Bath # 4 = COM8, Bath # 5 = COM9 

			}

		}
    }
    public static class MathUtilities
    {

        public static Dictionary<string, double> SimpleLinearRegression(List<double> X, List<double> Y)
        {
            ///Variable declarations            
            int num = 0; //use for List count
            double sumX = 0; //summation of x[i]
            double sumY = 0; //summation of y[i]
            double sum2X = 0; // summation of x[i]*x[i]
            double sum2Y = 0; // summation  of y[i]*y[i]
            double sumXY = 0; // summation of x[i] * y[i]  
            double denX = 0;
            double denY = 0;
            double top = 0;
            double corelation = 0; // holds Corelation
            double slope = 0; // holds slope(beta)
            double y_intercept = 0; //holds y-intercept (alpha)

            //Standard error variables
            double sum_res = 0.0;
            double yhat = 0;
            double res = 0;
            double standardError = 0; //
            int n = 0;
            //End standard variable declaration
            Dictionary<string, double> result
                = new Dictionary<string, double>(); //Stores the final result
            //End variable declaration

            #region Computation begins

            num = X.Count;  //Since the X and Y list are of same length, so 
            // we can take the count of any one list 
            sumX = X.Sum();  //Get Sum of X list
            sumY = Y.Sum(); //Get Sum of Y list           
            X.ForEach(i => { sum2X += i * i; }); //Get sum of x[i]*x[i]           
            Y.ForEach(i => { sum2Y += i * i; }); //Get sum of y[i]*y[i]            
            sumXY = Enumerable.Range(0, num).Select(i => X[i] * Y[i]).Sum();//Get Summation of x[i] * y[i]

            //Find denx, deny,top
            denX = num * sum2X - sumX * sumX;
            denY = num * sum2Y - sumY * sumY;
            top = num * sumXY - sumX * sumY;

            //Find corelation, slope and y-intercept
            corelation = top / Math.Sqrt(denX * denY);
            slope = top / denX;
            y_intercept = (sumY - sumX * slope) / num;


            //Implementation of Standard Error
            sum_res = Enumerable.Range(0, num).Aggregate(0.0, (sum, i) =>
            {
                yhat = y_intercept + (slope * X[i]);
                res = yhat - Y[i];
                n++;
                return sum + res * res;
            });

            if (n > 2)
            {
                standardError = sum_res / (1.0 * n - 2.0);
                standardError = Math.Pow(standardError, 0.5);
            }
            else standardError = 0;

            #endregion

            //Add the computed value to the resultant dictionary
            result.Add("Beta", slope);
            result.Add("Alpha", y_intercept);
            result.Add("Corelation", corelation);
            result.Add("StandardError", standardError);
            return result;
        }
    }

    public sealed class FDTesterSingleton
    {
        private static volatile FDTesterSingleton instance;
        private static object syncRoot = new Object();
        private FDTesterPersistentData refPlotData;
        public FDTesterPersistentData PlotData
        {
            get
            {
                return refPlotData;
            }
            set
            {
                refPlotData = value;
            }

        }

        private FDTesterSingleton()
        {
        }

        public static FDTesterSingleton Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new FDTesterSingleton();
                    }
                }

                return instance;
            }
        }
    }

    static class Program
    {
		const string SerializedFileName = @"C:\MSDCommon\Config\StripDiscSavedPlotData.bin";

		static void DeserializeFDTester(ref FDTesterPersistentData FDTesterData)
        {
            if (File.Exists(SerializedFileName))
            {
                Stream TestFileStream = File.OpenRead(SerializedFileName);
                BinaryFormatter deserializer = new BinaryFormatter();
                FDTesterData = (FDTesterPersistentData)deserializer.Deserialize(TestFileStream);
                TestFileStream.Close();
            }
            FDTesterSingleton.Instance.PlotData = FDTesterData;
        }

        static void SerializeFDTester()
        {
            Stream CloseFileStream = File.Create(SerializedFileName);
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(CloseFileStream, FDTesterSingleton.Instance.PlotData);
            CloseFileStream.Close();
        }

        //TestCommit to github

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            FDTesterPersistentData InitPlotData = new FDTesterPersistentData();
            DeserializeFDTester(ref InitPlotData);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

            SerializeFDTester();
        }
    }

}




namespace ReadWriteCsv
{
    /// <summary>
    /// Class to store one CSV row
    /// </summary>
    public class CsvRow : List<string>
    {
        public string LineText { get; set; }
    }

    /// <summary>
    /// Class to write data to a CSV file
    /// </summary>
    public class CsvFileWriter : StreamWriter
    {
        public CsvFileWriter(Stream stream)
            : base(stream)
        {
        }

        public CsvFileWriter(string filename)
            : base(filename)
        {
        }

        /// <summary>
        /// Writes a single row to a CSV file.
        /// </summary>
        /// <param name="row">The row to be written</param>
        public void WriteRow(CsvRow row)
        {
            StringBuilder builder = new StringBuilder();
            bool firstColumn = true;
            foreach (string value in row)
            {
                // Add separator if this isn't the first value
                if (!firstColumn)
                    builder.Append(',');
                // Implement special handling for values that contain comma or quote
                // Enclose in quotes and double up any double quotes
                if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
                    builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                else
                    builder.Append(value);
                firstColumn = false;
            }
            row.LineText = builder.ToString();
            WriteLine(row.LineText);
        }
    }

    /// <summary>
    /// Class to read data from a CSV file
    /// </summary>
    public class CsvFileReader : StreamReader
    {
        public CsvFileReader(Stream stream)
            : base(stream)
        {
        }

        public CsvFileReader(string filename)
            : base(filename)
        {
        }

        /// <summary>
        /// Reads a row of data from a CSV file
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool ReadRow(CsvRow row)
        {
            row.LineText = ReadLine();
            if (String.IsNullOrEmpty(row.LineText))
                return false;

            int pos = 0;
            int rows = 0;

            while (pos < row.LineText.Length)
            {
                string value;

                // Special handling for quoted field
                if (row.LineText[pos] == '"')
                {
                    // Skip initial quote
                    pos++;

                    // Parse quoted value
                    int start = pos;
                    while (pos < row.LineText.Length)
                    {
                        // Test for quote character
                        if (row.LineText[pos] == '"')
                        {
                            // Found one
                            pos++;

                            // If two quotes together, keep one
                            // Otherwise, indicates end of value
                            if (pos >= row.LineText.Length || row.LineText[pos] != '"')
                            {
                                pos--;
                                break;
                            }
                        }
                        pos++;
                    }
                    value = row.LineText.Substring(start, pos - start);
                    value = value.Replace("\"\"", "\"");
                }
                else
                {
                    // Parse unquoted value
                    int start = pos;
                    while (pos < row.LineText.Length && row.LineText[pos] != ',')
                        pos++;
                    value = row.LineText.Substring(start, pos - start);
                }

                // Add field to list
                if (rows < row.Count)
                    row[rows] = value;
                else
                    row.Add(value);
                rows++;

                // Eat up to and including next comma
                while (pos < row.LineText.Length && row.LineText[pos] != ',')
                    pos++;
                if (pos < row.LineText.Length)
                    pos++;
            }
            // Delete any unused items
            while (row.Count > rows)
                row.RemoveAt(rows);

            // Return true if any columns read
            return (row.Count > 0);
        }
    }
}
