using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

using NationalInstruments.DAQmx;
using NationalInstruments;

namespace TestPlotAcqVoltage
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Button startButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.GroupBox channelParametersGroupBox;
        private System.Windows.Forms.Label maximumLabel;
        private System.Windows.Forms.Label minimumLabel;
        private System.Windows.Forms.Label physicalChannelLabel;
        private System.Windows.Forms.Label rateLabel;
        private System.Windows.Forms.Label samplesLabel;
        private System.Windows.Forms.Label resultLabel;

        private AnalogMultiChannelReader analogInReader;
        private Task myTask;
        private Task runningTask;

        private AnalogWaveform<double>[] data;

        //private DataColumn[] dataColumn = null;
        //private DataTable dataTable = null;
        private System.Windows.Forms.GroupBox timingParametersGroupBox;
        private System.Windows.Forms.GroupBox acquisitionResultGroupBox;
        private System.Windows.Forms.DataGrid acquisitionDataGrid;
        private System.Windows.Forms.NumericUpDown rateNumeric;
        private System.Windows.Forms.NumericUpDown samplesPerChannelNumeric;
        internal System.Windows.Forms.NumericUpDown minimumValueNumeric;
        internal System.Windows.Forms.NumericUpDown maximumValueNumeric;
        private System.Windows.Forms.ComboBox physicalChannelComboBox;
        private System.Windows.Forms.DataVisualization.Charting.Chart dataChart;

        public Form1()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            setEnabled_graph(false);

            physicalChannelComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (physicalChannelComboBox.Items.Count > 0)
                physicalChannelComboBox.SelectedIndex = 0;

        }
        
        private void setEnabled_graph(bool setEnabled)
        {
            dataChart.Series["Data1"].IsVisibleInLegend = setEnabled;
            stopButton.Enabled = setEnabled;
            startButton.Enabled = !setEnabled;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.DoEvents();
            Application.Run(new Form1());
        }

        private void startButton_Click(object sender, System.EventArgs e)
        {
            if (runningTask == null)
            {
                try
                {
                    setEnabled_graph(true);

                    // Create a new task
                    myTask = new Task();

                    // Create a virtual channel
                    myTask.AIChannels.CreateVoltageChannel(physicalChannelComboBox.Text, "",
                        (AITerminalConfiguration)(-1), Convert.ToDouble(minimumValueNumeric.Value),
                        Convert.ToDouble(maximumValueNumeric.Value), AIVoltageUnits.Volts);

                    // Configure the timing parameters
                    myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(rateNumeric.Value),
                        SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, Convert.ToInt32(samplesPerChannelNumeric.Value) * 10);

                    // Configure the Every N Samples Event
                    myTask.EveryNSamplesReadEventInterval = Convert.ToInt32(samplesPerChannelNumeric.Value);
                    myTask.EveryNSamplesRead += new EveryNSamplesReadEventHandler(myTask_EveryNSamplesRead);
                    
                    runningTask = myTask;
                    analogInReader = new AnalogMultiChannelReader(myTask.Stream);
                    runningTask.SynchronizeCallbacks = true;


                    runningTask.Start();
                }
                catch (DaqException exception)
                {
                    // Display Errors
                    MessageBox.Show(exception.Message);
                    runningTask = null;
                    myTask.Dispose();
                    setEnabled_graph(false);
                }
            }
        }

        void myTask_EveryNSamplesRead(object sender, EveryNSamplesReadEventArgs e)
        {
            try
            {

                // Read the available data from the channels
                data = analogInReader.ReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value));

                // Plot your data here
                dataToDataGraph(data);

            }
            catch (DaqException exception)
            {
                // Display Errors
                MessageBox.Show(exception.Message);
                runningTask = null;
                myTask.Dispose();
                setEnabled_graph(false);
            }
        }

        

        private void stopButton_Click(object sender, System.EventArgs e)
        {
            if (runningTask != null)
            {
                runningTask.Stop();
                // Dispose of the task
                runningTask = null;
                myTask.Dispose();
                setEnabled_graph(false);
            }
        }

        private void dataToDataGraph(AnalogWaveform<double>[] sourceArray)
        {
            // Iterate over channels
            int currentLineIndex = 0;
            foreach (AnalogWaveform<double> waveform in sourceArray)
            {
                dataChart.Series["Data1"].Points.Clear();
                for (int sample = 0; sample < 50; ++sample)
                {                    
                    dataChart.Series["Data1"].Points.AddY(waveform.Samples[sample].Value);
                }
                currentLineIndex++;
            }
        }
    }
}
