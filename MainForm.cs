/******************************************************************************
*
* Example program:
*   ContAcqVoltageSamples_IntClk
*
* Category:
*   AI
*
* Description:
*   This example demonstrates how to acquire a continuous amount of data using
*   the DAQ device's internal clock.
*
* Instructions for running:
*   1.  Select the physical channel corresponding to where your signal is input
*       on the DAQ device.
*   2.  Enter the minimum and maximum voltage values.Note: For better accuracy,
*       try to match the input range to the expected voltage level of the
*       measured signal.
*   3.  Set the rate of the acquisition.Note: The rate should be at least twice
*       as fast as the maximum frequency component of the signal being acquired.
*       Also, in order to avoid Error -50410 (buffer overflow) it is important
*       to make sure the rate and the number of samples to read per iteration
*       are set such that they don't fill the buffer too quickly. If this error
*       occurs, try reducing the rate or increasing the number of samples to
*       read per iteration.
*
* Steps:
*   1.  Create a new analog input task.
*   2.  Create an analog input voltage channel.
*   3.  Set up the timing for the acquisition. In this example, we use the DAQ
*       device's internal clock to continuously acquire samples.
*   4.  Call AnalogMultiChannelReader.BeginReadWaveform to install a callback
*       and begin the asynchronous read operation.
*   5.  Inside the callback, call AnalogMultiChannelReader.EndReadWaveforme to
*       retrieve the data from the read operation.  
*   6.  Call AnalogMultiChannelReader.BeginMemoryOptimizedReadWaveform
*   7.  Dispose the Task object to clean-up any resources associated with the
*       task.
*   8.  Handle any DaqExceptions, if they occur.
*
*   Note: This example sets SynchronizeCallback to true. If SynchronizeCallback
*   is set to false, then you must give special consideration to safely dispose
*   the task and to update the UI from the callback. If SynchronizeCallback is
*   set to false, the callback executes on the worker thread and not on the main
*   UI thread. You can only update a UI component on the thread on which it was
*   created. Refer to the How to: Safely Dispose Task When Using Asynchronous
*   Callbacks topic in the NI-DAQmx .NET help for more information.
*
* I/O Connections Overview:
*   Make sure your signal input terminal matches the physical channel I/O
*   control. In the default case (differential channel ai0) wire the positive
*   lead for your signal to the ACH0 pin on your DAQ device and wire the
*   negative lead for your signal to the ACH8 pin on you DAQ device.  For more
*   information on the input and output terminals for your device, open the
*   NI-DAQmx Help, and refer to the NI-DAQmx Device Terminals and Device
*   Considerations books in the table of contents.
*
* Microsoft Windows Vista User Account Control
*   Running certain applications on Microsoft Windows Vista requires
*   administrator privileges, 
*   because the application name contains keywords such as setup, update, or
*   install. To avoid this problem, 
*   you must add an additional manifest to the application that specifies the
*   privileges required to run 
*   the application. Some Measurement Studio NI-DAQmx examples for Visual Studio
*   include these keywords. 
*   Therefore, all examples for Visual Studio are shipped with an additional
*   manifest file that you must 
*   embed in the example executable. The manifest file is named
*   [ExampleName].exe.manifest, where [ExampleName] 
*   is the NI-provided example name. For information on how to embed the manifest
*   file, refer to http://msdn2.microsoft.com/en-us/library/bb756929.aspx.Note: 
*   The manifest file is not provided with examples for Visual Studio .NET 2003.
*
******************************************************************************/

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

using NationalInstruments.DAQmx;
using System.Numerics;
using System.Linq;
using MathNet.Numerics.IntegralTransforms;

namespace NationalInstruments.Examples.ContAcqVoltageSamples_IntClk
{
    /// <summary>
    /// Summary description for MainForm.
    /// </summary>
    public class MainForm : System.Windows.Forms.Form
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
        private AsyncCallback analogCallback;

        private AnalogWaveform<double>[] data;
        private DataColumn[] dataColumn = null;
        private DataTable dataTable = null;
        private System.Windows.Forms.GroupBox timingParametersGroupBox;
        private System.Windows.Forms.GroupBox fftGroupBox;

        private System.Windows.Forms.GroupBox acquisitionResultGroupBox;
        private System.Windows.Forms.DataGrid acquisitionDataGrid;
        private System.Windows.Forms.NumericUpDown rateNumeric;
        private System.Windows.Forms.NumericUpDown samplesPerChannelNumeric;
        private TextBox fftresultvalue;
        internal System.Windows.Forms.NumericUpDown minimumValueNumeric;
        internal System.Windows.Forms.NumericUpDown maximumValueNumeric;
        private System.Windows.Forms.ComboBox physicalChannelComboBox;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private double _sampleRate;

        public MainForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            stopButton.Enabled = false;
            dataTable = new DataTable();

            physicalChannelComboBox.Items.AddRange(DaqSystem.Local.GetPhysicalChannels(PhysicalChannelTypes.AI, PhysicalChannelAccess.External));
            if (physicalChannelComboBox.Items.Count > 0)
                physicalChannelComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                if (myTask != null)
                {
                    runningTask = null;
                    myTask.Dispose();
                }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.channelParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.physicalChannelComboBox = new System.Windows.Forms.ComboBox();
            this.minimumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.maximumLabel = new System.Windows.Forms.Label();
            this.minimumLabel = new System.Windows.Forms.Label();
            this.physicalChannelLabel = new System.Windows.Forms.Label();
            this.timingParametersGroupBox = new System.Windows.Forms.GroupBox();
            this.fftGroupBox = new System.Windows.Forms.GroupBox();
            this.rateNumeric = new System.Windows.Forms.NumericUpDown();
            this.samplesLabel = new System.Windows.Forms.Label();
            this.rateLabel = new System.Windows.Forms.Label();
            this.samplesPerChannelNumeric = new System.Windows.Forms.NumericUpDown();
            this.fftresultvalue = new System.Windows.Forms.TextBox();
            this.startButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.acquisitionResultGroupBox = new System.Windows.Forms.GroupBox();
            this.resultLabel = new System.Windows.Forms.Label();
            this.acquisitionDataGrid = new System.Windows.Forms.DataGrid();
            this.channelParametersGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).BeginInit();
            this.timingParametersGroupBox.SuspendLayout();
            this.fftGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.samplesPerChannelNumeric)).BeginInit();
            this.acquisitionResultGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.acquisitionDataGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // channelParametersGroupBox
            // 
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelComboBox);
            this.channelParametersGroupBox.Controls.Add(this.minimumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.maximumValueNumeric);
            this.channelParametersGroupBox.Controls.Add(this.maximumLabel);
            this.channelParametersGroupBox.Controls.Add(this.minimumLabel);
            this.channelParametersGroupBox.Controls.Add(this.physicalChannelLabel);
            this.channelParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.channelParametersGroupBox.Location = new System.Drawing.Point(8, 8);
            this.channelParametersGroupBox.Name = "channelParametersGroupBox";
            this.channelParametersGroupBox.Size = new System.Drawing.Size(224, 120);
            this.channelParametersGroupBox.TabIndex = 2;
            this.channelParametersGroupBox.TabStop = false;
            this.channelParametersGroupBox.Text = "Channel Parameters";
            // 
            // physicalChannelComboBox
            // 
            this.physicalChannelComboBox.Location = new System.Drawing.Point(120, 24);
            this.physicalChannelComboBox.Name = "physicalChannelComboBox";
            this.physicalChannelComboBox.Size = new System.Drawing.Size(96, 21);
            this.physicalChannelComboBox.TabIndex = 1;
            this.physicalChannelComboBox.Text = "Dev1/ai0";
            // 
            // minimumValueNumeric
            // 
            this.minimumValueNumeric.DecimalPlaces = 2;
            this.minimumValueNumeric.Location = new System.Drawing.Point(120, 56);
            this.minimumValueNumeric.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.minimumValueNumeric.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            -2147483648});
            this.minimumValueNumeric.Name = "minimumValueNumeric";
            this.minimumValueNumeric.Size = new System.Drawing.Size(96, 20);
            this.minimumValueNumeric.TabIndex = 3;
            this.minimumValueNumeric.Value = new decimal(new int[] {
            100,
            0,
            0,
            -2147418112});
            // 
            // maximumValueNumeric
            // 
            this.maximumValueNumeric.DecimalPlaces = 2;
            this.maximumValueNumeric.Location = new System.Drawing.Point(120, 88);
            this.maximumValueNumeric.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.maximumValueNumeric.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            -2147483648});
            this.maximumValueNumeric.Name = "maximumValueNumeric";
            this.maximumValueNumeric.Size = new System.Drawing.Size(96, 20);
            this.maximumValueNumeric.TabIndex = 5;
            this.maximumValueNumeric.Value = new decimal(new int[] {
            100,
            0,
            0,
            65536});
            // 
            // maximumLabel
            // 
            this.maximumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.maximumLabel.Location = new System.Drawing.Point(16, 88);
            this.maximumLabel.Name = "maximumLabel";
            this.maximumLabel.Size = new System.Drawing.Size(112, 16);
            this.maximumLabel.TabIndex = 4;
            this.maximumLabel.Text = "Maximum Value (V):";
            // 
            // minimumLabel
            // 
            this.minimumLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.minimumLabel.Location = new System.Drawing.Point(16, 56);
            this.minimumLabel.Name = "minimumLabel";
            this.minimumLabel.Size = new System.Drawing.Size(104, 15);
            this.minimumLabel.TabIndex = 2;
            this.minimumLabel.Text = "Minimum Value (V):";
            // 
            // physicalChannelLabel
            // 
            this.physicalChannelLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.physicalChannelLabel.Location = new System.Drawing.Point(16, 26);
            this.physicalChannelLabel.Name = "physicalChannelLabel";
            this.physicalChannelLabel.Size = new System.Drawing.Size(96, 16);
            this.physicalChannelLabel.TabIndex = 0;
            this.physicalChannelLabel.Text = "Physical Channel:";
            // 
            // timingParametersGroupBox
            // 
            this.timingParametersGroupBox.Controls.Add(this.rateNumeric);
            this.timingParametersGroupBox.Controls.Add(this.samplesLabel);
            this.timingParametersGroupBox.Controls.Add(this.rateLabel);
            this.timingParametersGroupBox.Controls.Add(this.samplesPerChannelNumeric);
            this.timingParametersGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.timingParametersGroupBox.Location = new System.Drawing.Point(8, 136);
            this.timingParametersGroupBox.Name = "timingParametersGroupBox";
            this.timingParametersGroupBox.Size = new System.Drawing.Size(224, 92);
            this.timingParametersGroupBox.TabIndex = 3;
            this.timingParametersGroupBox.TabStop = false;
            this.timingParametersGroupBox.Text = "Timing Parameters";

            this.fftGroupBox.Text = "FFT Result";
            this.fftGroupBox.Controls.Add(this.fftresultvalue);
            this.fftGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.fftGroupBox.Location = new System.Drawing.Point(8, 270);
            this.fftGroupBox.Name = "fftGroupBox";
            this.fftGroupBox.Size = new System.Drawing.Size(224, 46);
            this.fftGroupBox.TabIndex = 3;
            this.fftGroupBox.TabStop = false;

            this.fftresultvalue.ReadOnly = true;
            this.fftresultvalue.Location = new System.Drawing.Point(20, 20);

            // 
            // rateNumeric
            // 
            this.rateNumeric.DecimalPlaces = 2;
            this.rateNumeric.Location = new System.Drawing.Point(120, 56);
            this.rateNumeric.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.rateNumeric.Name = "rateNumeric";
            this.rateNumeric.Size = new System.Drawing.Size(96, 20);
            this.rateNumeric.TabIndex = 3;
            this.rateNumeric.Value = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            // 
            // samplesLabel
            // 
            this.samplesLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.samplesLabel.Location = new System.Drawing.Point(16, 26);
            this.samplesLabel.Name = "samplesLabel";
            this.samplesLabel.Size = new System.Drawing.Size(104, 16);
            this.samplesLabel.TabIndex = 0;
            this.samplesLabel.Text = "Samples/Channel:";
            // 
            // rateLabel
            // 
            this.rateLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.rateLabel.Location = new System.Drawing.Point(16, 58);
            this.rateLabel.Name = "rateLabel";
            this.rateLabel.Size = new System.Drawing.Size(56, 16);
            this.rateLabel.TabIndex = 2;
            this.rateLabel.Text = "Rate (Hz):";
            // 
            // samplesPerChannelNumeric
            // 
            this.samplesPerChannelNumeric.Location = new System.Drawing.Point(120, 24);
            this.samplesPerChannelNumeric.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.samplesPerChannelNumeric.Name = "samplesPerChannelNumeric";
            this.samplesPerChannelNumeric.Size = new System.Drawing.Size(96, 20);
            this.samplesPerChannelNumeric.TabIndex = 1;
            this.samplesPerChannelNumeric.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // startButton
            // 
            this.startButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.startButton.Location = new System.Drawing.Point(24, 240);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(80, 24);
            this.startButton.TabIndex = 0;
            this.startButton.Text = "Start";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // stopButton
            // 
            this.stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.stopButton.Location = new System.Drawing.Point(136, 240);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(80, 24);
            this.stopButton.TabIndex = 1;
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // acquisitionResultGroupBox
            // 
            this.acquisitionResultGroupBox.Controls.Add(this.resultLabel);
            this.acquisitionResultGroupBox.Controls.Add(this.acquisitionDataGrid);
            this.acquisitionResultGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.acquisitionResultGroupBox.Location = new System.Drawing.Point(240, 8);
            this.acquisitionResultGroupBox.Name = "acquisitionResultGroupBox";
            this.acquisitionResultGroupBox.Size = new System.Drawing.Size(304, 256);
            this.acquisitionResultGroupBox.TabIndex = 4;
            this.acquisitionResultGroupBox.TabStop = false;
            this.acquisitionResultGroupBox.Text = "Acquisition Results";
            this.acquisitionResultGroupBox.Enter += new System.EventHandler(this.acquisitionResultGroupBox_Enter);
            // 
            // resultLabel
            // 
            this.resultLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.resultLabel.Location = new System.Drawing.Point(8, 16);
            this.resultLabel.Name = "resultLabel";
            this.resultLabel.Size = new System.Drawing.Size(112, 16);
            this.resultLabel.TabIndex = 0;
            this.resultLabel.Text = "Acquisition Data (V):";
            // 
            // acquisitionDataGrid
            // 
            this.acquisitionDataGrid.AllowSorting = false;
            this.acquisitionDataGrid.DataMember = "";
            this.acquisitionDataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText;
            this.acquisitionDataGrid.Location = new System.Drawing.Point(16, 32);
            this.acquisitionDataGrid.Name = "acquisitionDataGrid";
            this.acquisitionDataGrid.ParentRowsVisible = false;
            this.acquisitionDataGrid.ReadOnly = true;
            this.acquisitionDataGrid.Size = new System.Drawing.Size(280, 216);
            this.acquisitionDataGrid.TabIndex = 1;
            this.acquisitionDataGrid.TabStop = false;
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(554, 330);
            this.Controls.Add(this.acquisitionResultGroupBox);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startButton);
            this.Controls.Add(this.timingParametersGroupBox);
            this.Controls.Add(this.channelParametersGroupBox);
            this.Controls.Add(this.fftGroupBox);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Continuous Acquisition of Voltage Samples - Internal Clock";
            this.channelParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.minimumValueNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maximumValueNumeric)).EndInit();
            this.timingParametersGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.rateNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.samplesPerChannelNumeric)).EndInit();
            this.acquisitionResultGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.acquisitionDataGrid)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.DoEvents();
            Application.Run(new MainForm());
        }

        private void startButton_Click(object sender, System.EventArgs e)
        {
            if (runningTask == null)
            {
                try
                {
                    stopButton.Enabled = true;
                    startButton.Enabled = false;

                    // Create a new task
                    myTask = new Task();

                    // Create a virtual channel
                    myTask.AIChannels.CreateVoltageChannel(physicalChannelComboBox.Text, "",
                        AITerminalConfiguration.Differential, Convert.ToDouble(minimumValueNumeric.Value),
                        Convert.ToDouble(maximumValueNumeric.Value), AIVoltageUnits.Volts);

                    // Configure the timing parameters
                    _sampleRate = Convert.ToDouble(rateNumeric.Value);

                    myTask.Timing.ConfigureSampleClock("", Convert.ToDouble(rateNumeric.Value),
                        SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 1000);

                    // Verify the Task
                    myTask.Control(TaskAction.Verify);

                    // Prepare the table for Data
                    InitializeDataTable(myTask.AIChannels, ref dataTable);
                    acquisitionDataGrid.DataSource = dataTable;

                    runningTask = myTask;
                    analogInReader = new AnalogMultiChannelReader(myTask.Stream);
                    analogCallback = new AsyncCallback(AnalogInCallback);

                    // Use SynchronizeCallbacks to specify that the object 
                    // marshals callbacks across threads appropriately.
                    analogInReader.SynchronizeCallbacks = true;
                    analogInReader.BeginReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value),
                        analogCallback, myTask);
                }
                catch (DaqException exception)
                {
                    // Display Errors
                    MessageBox.Show(exception.Message);
                    runningTask = null;
                    myTask.Dispose();
                    stopButton.Enabled = false;
                    startButton.Enabled = true;
                }
            }
        }

        private void AnalogInCallback(IAsyncResult ar)
        {
            try
            {
                if (runningTask != null && runningTask == ar.AsyncState)
                {
                    // Read the available data from the channels
                    data = analogInReader.EndReadWaveform(ar);

                    // Plot your data here
                    dataToDataTable(data, ref dataTable);

                    analogInReader.BeginMemoryOptimizedReadWaveform(Convert.ToInt32(samplesPerChannelNumeric.Value),
                        analogCallback, myTask, data);
                }
            }
            catch (DaqException exception)
            {
                // Display Errors
                MessageBox.Show(exception.Message);
                runningTask = null;
                myTask.Dispose();
                stopButton.Enabled = false;
                startButton.Enabled = true;
            }
        }

        private void stopButton_Click(object sender, System.EventArgs e)
        {
            if (runningTask != null)
            {
                // Dispose of the task
                runningTask = null;
                myTask.Dispose();
                stopButton.Enabled = false;
                startButton.Enabled = true;
            }
        }

        private void dataToDataTable(AnalogWaveform<double>[] sourceArray, ref DataTable dataTable)
        {
            // Iterate over channels
            int currentLineIndex = 0;
            if (sourceArray.Any())
            {
                var fft = sourceArray[0].Samples.Select(x => new Complex(x.Value, 0)).ToArray();
                // var fft = data.Select(x => new Complex(x, 0)).ToArray();
                Fourier.Forward(fft, FourierOptions.Matlab);
                if (fft.Any())
                {
                    fft = fft.Take(fft.Length / 2).ToArray();
                    var bin =  _sampleRate / (fft.Length * 2.0);
                    var frequencies = Enumerable.Range(0, fft.Length).Select(x => x * bin).ToArray();
                    var amplitudes = fft.Select(x => x.Magnitude).ToArray();

                    var freq = frequencies[MaxIndex(amplitudes)];
                    fftresultvalue.Text = $"{freq} Hz";
                }
            }

            foreach (AnalogWaveform<double> waveform in sourceArray)
            {
                for (int sample = 0; sample < waveform.Samples.Count; ++sample)
                {
                    if (sample == 10)
                        break;

                    var dataValue = waveform.Samples[sample].Value;
                    dataTable.Rows[sample][currentLineIndex] = dataValue;
                }
                currentLineIndex++;
            }
        }

        public void InitializeDataTable(AIChannelCollection channelCollection, ref DataTable data)
        {
            int numOfChannels = channelCollection.Count;
            data.Rows.Clear();
            data.Columns.Clear();
            dataColumn = new DataColumn[numOfChannels];
            int numOfRows = 10;

            for (int currentChannelIndex = 0; currentChannelIndex < numOfChannels; currentChannelIndex++)
            {
                dataColumn[currentChannelIndex] = new DataColumn();
                dataColumn[currentChannelIndex].DataType = typeof(double);
                dataColumn[currentChannelIndex].ColumnName = channelCollection[currentChannelIndex].PhysicalName;
            }

            data.Columns.AddRange(dataColumn);

            for (int currentDataIndex = 0; currentDataIndex < numOfRows; currentDataIndex++)
            {
                object[] rowArr = new object[numOfChannels];
                data.Rows.Add(rowArr);
            }
        }

        /// <summary>
        /// Finds the index of the maximum value in the array
        /// </summary>
        /// <param name="array">the array</param>
        /// <returns>the max index</returns>
        private int MaxIndex(double[] array)
        {
            // Find peak frequency
            int peakIndex = 0;
            double peakValue = 0;
            for (int i = 1; i < array.Length; i++)
            {
                if (array[i] > peakValue)
                {
                    peakValue = array[i];
                    peakIndex = i;
                }
            }

            return peakIndex;
        }

        private void acquisitionResultGroupBox_Enter(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Creates the measured data
        /// </summary>
        /// <param name="samplerate">the selected sample rate</param>
        /// <param name="resolution">the resolution</param>
        /// <param name="targetFrequency">the target frequency of the signal</param>
        /// <param name="targetAmplitude">the target amplite</param>
        /// <param name="noise">The optional noise</param>/// <param name="dcValue">the dc value</param>
        /// <returns>the data</returns>
        private double[] CreateData(
            double samplerate,
            int resolution,
            double targetFrequency,
            double targetAmplitude,
            double dcValue = 0)
        {
            var sinusData = new double[resolution];
            for (var i = 0; i < resolution; i++)
            {
                sinusData[i] = dcValue + targetAmplitude * Math.Sin(2 * Math.PI * (i) * targetFrequency / samplerate);
            }

            return sinusData;
        }
    }
}
