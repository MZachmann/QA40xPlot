namespace QA40x_AUDIO_ANALYSER
{
    partial class frmFrequencyResponse
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.scThdVsFreq = new System.Windows.Forms.SplitContainer();
            this.grpMeasurements_R = new System.Windows.Forms.GroupBox();
            this.lblMeas_Highest_Freq_R = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.lblMeas_Amplitude_dBV_R = new System.Windows.Forms.Label();
            this.lblMeas_Amplitude_V_R = new System.Windows.Forms.Label();
            this.label24 = new System.Windows.Forms.Label();
            this.lblMeas_BW1_high_R = new System.Windows.Forms.Label();
            this.lblMeas_BW1_low_R = new System.Windows.Forms.Label();
            this.lblMeas_BW1_R = new System.Windows.Forms.Label();
            this.lblMeas_BW3_high_R = new System.Windows.Forms.Label();
            this.lblMeas_BW3_low_R = new System.Windows.Forms.Label();
            this.lblMeas_BW3_R = new System.Windows.Forms.Label();
            this.chk3dBBandWidth_R = new System.Windows.Forms.CheckBox();
            this.label34 = new System.Windows.Forms.Label();
            this.label35 = new System.Windows.Forms.Label();
            this.label36 = new System.Windows.Forms.Label();
            this.label37 = new System.Windows.Forms.Label();
            this.label38 = new System.Windows.Forms.Label();
            this.label39 = new System.Windows.Forms.Label();
            this.chk1dBBandWidth_R = new System.Windows.Forms.CheckBox();
            this.grpMeasurements_L = new System.Windows.Forms.GroupBox();
            this.lblMeas_Highest_Freq_L = new System.Windows.Forms.Label();
            this.lblMeas_HighestGainFreq = new System.Windows.Forms.Label();
            this.lblMeas_Amplitude_dBV_L = new System.Windows.Forms.Label();
            this.lblMeas_Amplitude_V_L = new System.Windows.Forms.Label();
            this.lblMeas_MaxAmplitude_L = new System.Windows.Forms.Label();
            this.lblMeas_BW1_high_L = new System.Windows.Forms.Label();
            this.lblMeas_BW1_low_L = new System.Windows.Forms.Label();
            this.lblMeas_BW1_L = new System.Windows.Forms.Label();
            this.lblMeas_BW3_high_L = new System.Windows.Forms.Label();
            this.lblMeas_BW3_low_L = new System.Windows.Forms.Label();
            this.lblMeas_BW3_L = new System.Windows.Forms.Label();
            this.chk3dBBandWidth_L = new System.Windows.Forms.CheckBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.chk1dBBandWidth_L = new System.Windows.Forms.CheckBox();
            this.btnSingle = new System.Windows.Forms.Button();
            this.btnStopThdMeasurement = new System.Windows.Forms.Button();
            this.btnRun = new System.Windows.Forms.Button();
            this.grpMeasurmentSettings_Chirp = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbLowFrequencyAccuracy = new System.Windows.Forms.ComboBox();
            this.lblLowFrequencyAccuracy = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.chkRightIsReference = new System.Windows.Forms.CheckBox();
            this.cmbFrequencySpan = new System.Windows.Forms.ComboBox();
            this.lblSmoothing = new System.Windows.Forms.Label();
            this.cmbSmoothing = new System.Windows.Forms.ComboBox();
            this.lblGeneratorType = new System.Windows.Forms.Label();
            this.cmbGeneratorType = new System.Windows.Forms.ComboBox();
            this.chkEnableRightChannel = new System.Windows.Forms.CheckBox();
            this.chkEnableLeftChannel = new System.Windows.Forms.CheckBox();
            this.udAverages = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbGeneratorVoltageUnit = new System.Windows.Forms.ComboBox();
            this.lblThdFreq_GenVoltage = new System.Windows.Forms.Label();
            this.txtGeneratorVoltage = new System.Windows.Forms.TextBox();
            this.lblStartFrequency = new System.Windows.Forms.Label();
            this.scGraphCursors = new System.Windows.Forms.SplitContainer();
            this.scGraphSettings = new System.Windows.Forms.SplitContainer();
            this.freqPlot = new ScottPlot.WinForms.FormsPlot();
            this.gbFrequencyRange = new System.Windows.Forms.GroupBox();
            this.label8 = new System.Windows.Forms.Label();
            this.cmbGraph_FreqEnd = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cmbGraph_FreqStart = new System.Windows.Forms.ComboBox();
            this.btnFitGraphX = new System.Windows.Forms.Button();
            this.chkGraphShowRightChannel = new System.Windows.Forms.CheckBox();
            this.chkGraphShowLeftChannel = new System.Windows.Forms.CheckBox();
            this.chkShowDataPoints = new System.Windows.Forms.CheckBox();
            this.chkThickLines = new System.Windows.Forms.CheckBox();
            this.btnGraph_dBV = new System.Windows.Forms.Button();
            this.btnGraph_Gain = new System.Windows.Forms.Button();
            this.gbDbV_Range = new System.Windows.Forms.GroupBox();
            this.ud_Graph_Bottom = new System.Windows.Forms.NumericUpDown();
            this.ud_Graph_Top = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.btnD_FitGraphY = new System.Windows.Forms.Button();
            this.lblCursorGain = new System.Windows.Forms.Label();
            this.lblCursorAmplitude = new System.Windows.Forms.Label();
            this.lblCursor_Frequency = new System.Windows.Forms.Label();
            this.pnlCursorsRight = new System.Windows.Forms.Panel();
            this.lblCursor_Amplitude_V_R = new System.Windows.Forms.Label();
            this.lblCursor_Amplitude_dBV_R = new System.Windows.Forms.Label();
            this.lblCursor_RightChannel = new System.Windows.Forms.Label();
            this.pnlCursorsLeft = new System.Windows.Forms.Panel();
            this.lblCursor_Gain_dB_L = new System.Windows.Forms.Label();
            this.lblCursor_Gain_times_L = new System.Windows.Forms.Label();
            this.lblCursor_Amplitude_V_L = new System.Windows.Forms.Label();
            this.lblCursor_Amplitude_dBV_L = new System.Windows.Forms.Label();
            this.lblCursor_LeftChannel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.scThdVsFreq)).BeginInit();
            this.scThdVsFreq.Panel1.SuspendLayout();
            this.scThdVsFreq.Panel2.SuspendLayout();
            this.scThdVsFreq.SuspendLayout();
            this.grpMeasurements_R.SuspendLayout();
            this.grpMeasurements_L.SuspendLayout();
            this.grpMeasurmentSettings_Chirp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udAverages)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scGraphCursors)).BeginInit();
            this.scGraphCursors.Panel1.SuspendLayout();
            this.scGraphCursors.Panel2.SuspendLayout();
            this.scGraphCursors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scGraphSettings)).BeginInit();
            this.scGraphSettings.Panel1.SuspendLayout();
            this.scGraphSettings.Panel2.SuspendLayout();
            this.scGraphSettings.SuspendLayout();
            this.gbFrequencyRange.SuspendLayout();
            this.gbDbV_Range.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ud_Graph_Bottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ud_Graph_Top)).BeginInit();
            this.pnlCursorsRight.SuspendLayout();
            this.pnlCursorsLeft.SuspendLayout();
            this.SuspendLayout();
            // 
            // scThdVsFreq
            // 
            this.scThdVsFreq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scThdVsFreq.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.scThdVsFreq.IsSplitterFixed = true;
            this.scThdVsFreq.Location = new System.Drawing.Point(0, 0);
            this.scThdVsFreq.Name = "scThdVsFreq";
            // 
            // scThdVsFreq.Panel1
            // 
            this.scThdVsFreq.Panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.scThdVsFreq.Panel1.Controls.Add(this.grpMeasurements_R);
            this.scThdVsFreq.Panel1.Controls.Add(this.grpMeasurements_L);
            this.scThdVsFreq.Panel1.Controls.Add(this.btnSingle);
            this.scThdVsFreq.Panel1.Controls.Add(this.btnStopThdMeasurement);
            this.scThdVsFreq.Panel1.Controls.Add(this.btnRun);
            this.scThdVsFreq.Panel1.Controls.Add(this.grpMeasurmentSettings_Chirp);
            this.scThdVsFreq.Panel1MinSize = 260;
            // 
            // scThdVsFreq.Panel2
            // 
            this.scThdVsFreq.Panel2.Controls.Add(this.scGraphCursors);
            this.scThdVsFreq.Panel2MinSize = 200;
            this.scThdVsFreq.Size = new System.Drawing.Size(1100, 765);
            this.scThdVsFreq.SplitterDistance = 260;
            this.scThdVsFreq.TabIndex = 1;
            // 
            // grpMeasurements_R
            // 
            this.grpMeasurements_R.Controls.Add(this.lblMeas_Highest_Freq_R);
            this.grpMeasurements_R.Controls.Add(this.label17);
            this.grpMeasurements_R.Controls.Add(this.lblMeas_Amplitude_dBV_R);
            this.grpMeasurements_R.Controls.Add(this.lblMeas_Amplitude_V_R);
            this.grpMeasurements_R.Controls.Add(this.label24);
            this.grpMeasurements_R.Controls.Add(this.lblMeas_BW1_high_R);
            this.grpMeasurements_R.Controls.Add(this.lblMeas_BW1_low_R);
            this.grpMeasurements_R.Controls.Add(this.lblMeas_BW1_R);
            this.grpMeasurements_R.Controls.Add(this.lblMeas_BW3_high_R);
            this.grpMeasurements_R.Controls.Add(this.lblMeas_BW3_low_R);
            this.grpMeasurements_R.Controls.Add(this.lblMeas_BW3_R);
            this.grpMeasurements_R.Controls.Add(this.chk3dBBandWidth_R);
            this.grpMeasurements_R.Controls.Add(this.label34);
            this.grpMeasurements_R.Controls.Add(this.label35);
            this.grpMeasurements_R.Controls.Add(this.label36);
            this.grpMeasurements_R.Controls.Add(this.label37);
            this.grpMeasurements_R.Controls.Add(this.label38);
            this.grpMeasurements_R.Controls.Add(this.label39);
            this.grpMeasurements_R.Controls.Add(this.chk1dBBandWidth_R);
            this.grpMeasurements_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpMeasurements_R.Location = new System.Drawing.Point(4, 455);
            this.grpMeasurements_R.Name = "grpMeasurements_R";
            this.grpMeasurements_R.Size = new System.Drawing.Size(254, 153);
            this.grpMeasurements_R.TabIndex = 54;
            this.grpMeasurements_R.TabStop = false;
            this.grpMeasurements_R.Text = "Measurements right channel";
            // 
            // lblMeas_Highest_Freq_R
            // 
            this.lblMeas_Highest_Freq_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_Highest_Freq_R.Location = new System.Drawing.Point(183, 127);
            this.lblMeas_Highest_Freq_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_Highest_Freq_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_Highest_Freq_R.Name = "lblMeas_Highest_Freq_R";
            this.lblMeas_Highest_Freq_R.Size = new System.Drawing.Size(60, 13);
            this.lblMeas_Highest_Freq_R.TabIndex = 67;
            this.lblMeas_Highest_Freq_R.Text = "0.00 Hz";
            // 
            // label17
            // 
            this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.Location = new System.Drawing.Point(6, 127);
            this.label17.MinimumSize = new System.Drawing.Size(50, 13);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(153, 13);
            this.label17.TabIndex = 66;
            this.label17.Text = "Highest amplitude freq.";
            // 
            // lblMeas_Amplitude_dBV_R
            // 
            this.lblMeas_Amplitude_dBV_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_Amplitude_dBV_R.Location = new System.Drawing.Point(101, 107);
            this.lblMeas_Amplitude_dBV_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_Amplitude_dBV_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_Amplitude_dBV_R.Name = "lblMeas_Amplitude_dBV_R";
            this.lblMeas_Amplitude_dBV_R.Size = new System.Drawing.Size(75, 15);
            this.lblMeas_Amplitude_dBV_R.TabIndex = 65;
            this.lblMeas_Amplitude_dBV_R.Text = "0.00 dBV";
            // 
            // lblMeas_Amplitude_V_R
            // 
            this.lblMeas_Amplitude_V_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_Amplitude_V_R.Location = new System.Drawing.Point(183, 107);
            this.lblMeas_Amplitude_V_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_Amplitude_V_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_Amplitude_V_R.Name = "lblMeas_Amplitude_V_R";
            this.lblMeas_Amplitude_V_R.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_Amplitude_V_R.TabIndex = 63;
            this.lblMeas_Amplitude_V_R.Text = "0.00 V";
            // 
            // label24
            // 
            this.label24.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label24.Location = new System.Drawing.Point(6, 107);
            this.label24.MinimumSize = new System.Drawing.Size(50, 13);
            this.label24.Name = "label24";
            this.label24.Size = new System.Drawing.Size(98, 15);
            this.label24.TabIndex = 62;
            this.label24.Text = "Max. amplitude";
            // 
            // lblMeas_BW1_high_R
            // 
            this.lblMeas_BW1_high_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW1_high_R.Location = new System.Drawing.Point(101, 51);
            this.lblMeas_BW1_high_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW1_high_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW1_high_R.Name = "lblMeas_BW1_high_R";
            this.lblMeas_BW1_high_R.Size = new System.Drawing.Size(60, 13);
            this.lblMeas_BW1_high_R.TabIndex = 60;
            this.lblMeas_BW1_high_R.Text = "0.00 Hz";
            // 
            // lblMeas_BW1_low_R
            // 
            this.lblMeas_BW1_low_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW1_low_R.Location = new System.Drawing.Point(41, 51);
            this.lblMeas_BW1_low_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW1_low_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW1_low_R.Name = "lblMeas_BW1_low_R";
            this.lblMeas_BW1_low_R.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_BW1_low_R.TabIndex = 59;
            this.lblMeas_BW1_low_R.Text = "0.00 Hz";
            // 
            // lblMeas_BW1_R
            // 
            this.lblMeas_BW1_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW1_R.Location = new System.Drawing.Point(162, 51);
            this.lblMeas_BW1_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW1_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW1_R.Name = "lblMeas_BW1_R";
            this.lblMeas_BW1_R.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_BW1_R.TabIndex = 58;
            this.lblMeas_BW1_R.Text = "0.00 Hz";
            // 
            // lblMeas_BW3_high_R
            // 
            this.lblMeas_BW3_high_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW3_high_R.Location = new System.Drawing.Point(101, 71);
            this.lblMeas_BW3_high_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW3_high_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW3_high_R.Name = "lblMeas_BW3_high_R";
            this.lblMeas_BW3_high_R.Size = new System.Drawing.Size(60, 13);
            this.lblMeas_BW3_high_R.TabIndex = 57;
            this.lblMeas_BW3_high_R.Text = "0.00 Hz";
            // 
            // lblMeas_BW3_low_R
            // 
            this.lblMeas_BW3_low_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW3_low_R.Location = new System.Drawing.Point(41, 71);
            this.lblMeas_BW3_low_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW3_low_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW3_low_R.Name = "lblMeas_BW3_low_R";
            this.lblMeas_BW3_low_R.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_BW3_low_R.TabIndex = 55;
            this.lblMeas_BW3_low_R.Text = "0.00 Hz";
            // 
            // lblMeas_BW3_R
            // 
            this.lblMeas_BW3_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW3_R.Location = new System.Drawing.Point(162, 71);
            this.lblMeas_BW3_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW3_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW3_R.Name = "lblMeas_BW3_R";
            this.lblMeas_BW3_R.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_BW3_R.TabIndex = 53;
            this.lblMeas_BW3_R.Text = "0.00 Hz";
            // 
            // chk3dBBandWidth_R
            // 
            this.chk3dBBandWidth_R.AutoSize = true;
            this.chk3dBBandWidth_R.Checked = true;
            this.chk3dBBandWidth_R.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk3dBBandWidth_R.Location = new System.Drawing.Point(222, 70);
            this.chk3dBBandWidth_R.Name = "chk3dBBandWidth_R";
            this.chk3dBBandWidth_R.Size = new System.Drawing.Size(15, 14);
            this.chk3dBBandWidth_R.TabIndex = 52;
            this.chk3dBBandWidth_R.UseVisualStyleBackColor = true;
            this.chk3dBBandWidth_R.CheckedChanged += new System.EventHandler(this.chk3dBBandWidth_R_CheckedChanged);
            // 
            // label34
            // 
            this.label34.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label34.Location = new System.Drawing.Point(6, 71);
            this.label34.MinimumSize = new System.Drawing.Size(30, 13);
            this.label34.Name = "label34";
            this.label34.Size = new System.Drawing.Size(42, 13);
            this.label34.TabIndex = 51;
            this.label34.Text = "3 dB";
            // 
            // label35
            // 
            this.label35.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label35.Location = new System.Drawing.Point(211, 30);
            this.label35.MinimumSize = new System.Drawing.Size(30, 13);
            this.label35.Name = "label35";
            this.label35.Size = new System.Drawing.Size(42, 13);
            this.label35.TabIndex = 50;
            this.label35.Text = "Show";
            // 
            // label36
            // 
            this.label36.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label36.Location = new System.Drawing.Point(162, 31);
            this.label36.MinimumSize = new System.Drawing.Size(30, 13);
            this.label36.Name = "label36";
            this.label36.Size = new System.Drawing.Size(42, 13);
            this.label36.TabIndex = 49;
            this.label36.Text = "BW";
            // 
            // label37
            // 
            this.label37.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label37.Location = new System.Drawing.Point(101, 31);
            this.label37.MinimumSize = new System.Drawing.Size(30, 13);
            this.label37.Name = "label37";
            this.label37.Size = new System.Drawing.Size(45, 13);
            this.label37.TabIndex = 48;
            this.label37.Text = "f(high)";
            // 
            // label38
            // 
            this.label38.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label38.Location = new System.Drawing.Point(43, 31);
            this.label38.MinimumSize = new System.Drawing.Size(30, 13);
            this.label38.Name = "label38";
            this.label38.Size = new System.Drawing.Size(42, 13);
            this.label38.TabIndex = 47;
            this.label38.Text = "f(low)";
            // 
            // label39
            // 
            this.label39.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label39.Location = new System.Drawing.Point(6, 51);
            this.label39.MinimumSize = new System.Drawing.Size(30, 13);
            this.label39.Name = "label39";
            this.label39.Size = new System.Drawing.Size(42, 13);
            this.label39.TabIndex = 46;
            this.label39.Text = "1 dB";
            // 
            // chk1dBBandWidth_R
            // 
            this.chk1dBBandWidth_R.AutoSize = true;
            this.chk1dBBandWidth_R.Checked = true;
            this.chk1dBBandWidth_R.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk1dBBandWidth_R.Location = new System.Drawing.Point(222, 50);
            this.chk1dBBandWidth_R.Name = "chk1dBBandWidth_R";
            this.chk1dBBandWidth_R.Size = new System.Drawing.Size(15, 14);
            this.chk1dBBandWidth_R.TabIndex = 43;
            this.chk1dBBandWidth_R.UseVisualStyleBackColor = true;
            this.chk1dBBandWidth_R.CheckedChanged += new System.EventHandler(this.chk1dBBandWidth_R_CheckedChanged);
            // 
            // grpMeasurements_L
            // 
            this.grpMeasurements_L.Controls.Add(this.lblMeas_Highest_Freq_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_HighestGainFreq);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_Amplitude_dBV_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_Amplitude_V_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_MaxAmplitude_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW1_high_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW1_low_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW1_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW3_high_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW3_low_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW3_L);
            this.grpMeasurements_L.Controls.Add(this.chk3dBBandWidth_L);
            this.grpMeasurements_L.Controls.Add(this.label14);
            this.grpMeasurements_L.Controls.Add(this.label12);
            this.grpMeasurements_L.Controls.Add(this.label11);
            this.grpMeasurements_L.Controls.Add(this.label10);
            this.grpMeasurements_L.Controls.Add(this.label5);
            this.grpMeasurements_L.Controls.Add(this.label19);
            this.grpMeasurements_L.Controls.Add(this.chk1dBBandWidth_L);
            this.grpMeasurements_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpMeasurements_L.Location = new System.Drawing.Point(4, 299);
            this.grpMeasurements_L.Name = "grpMeasurements_L";
            this.grpMeasurements_L.Size = new System.Drawing.Size(254, 151);
            this.grpMeasurements_L.TabIndex = 53;
            this.grpMeasurements_L.TabStop = false;
            this.grpMeasurements_L.Text = "Measurements left channel";
            // 
            // lblMeas_Highest_Freq_L
            // 
            this.lblMeas_Highest_Freq_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_Highest_Freq_L.Location = new System.Drawing.Point(183, 123);
            this.lblMeas_Highest_Freq_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_Highest_Freq_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_Highest_Freq_L.Name = "lblMeas_Highest_Freq_L";
            this.lblMeas_Highest_Freq_L.Size = new System.Drawing.Size(60, 13);
            this.lblMeas_Highest_Freq_L.TabIndex = 67;
            this.lblMeas_Highest_Freq_L.Text = "0.00 Hz";
            // 
            // lblMeas_HighestGainFreq
            // 
            this.lblMeas_HighestGainFreq.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_HighestGainFreq.Location = new System.Drawing.Point(6, 123);
            this.lblMeas_HighestGainFreq.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblMeas_HighestGainFreq.Name = "lblMeas_HighestGainFreq";
            this.lblMeas_HighestGainFreq.Size = new System.Drawing.Size(152, 13);
            this.lblMeas_HighestGainFreq.TabIndex = 66;
            this.lblMeas_HighestGainFreq.Text = "Highest amplitude freq.";
            // 
            // lblMeas_Amplitude_dBV_L
            // 
            this.lblMeas_Amplitude_dBV_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_Amplitude_dBV_L.Location = new System.Drawing.Point(101, 103);
            this.lblMeas_Amplitude_dBV_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_Amplitude_dBV_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_Amplitude_dBV_L.Name = "lblMeas_Amplitude_dBV_L";
            this.lblMeas_Amplitude_dBV_L.Size = new System.Drawing.Size(75, 15);
            this.lblMeas_Amplitude_dBV_L.TabIndex = 65;
            this.lblMeas_Amplitude_dBV_L.Text = "0.00 dBV";
            // 
            // lblMeas_Amplitude_V_L
            // 
            this.lblMeas_Amplitude_V_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_Amplitude_V_L.Location = new System.Drawing.Point(183, 103);
            this.lblMeas_Amplitude_V_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_Amplitude_V_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_Amplitude_V_L.Name = "lblMeas_Amplitude_V_L";
            this.lblMeas_Amplitude_V_L.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_Amplitude_V_L.TabIndex = 63;
            this.lblMeas_Amplitude_V_L.Text = "0.00 V";
            // 
            // lblMeas_MaxAmplitude_L
            // 
            this.lblMeas_MaxAmplitude_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_MaxAmplitude_L.Location = new System.Drawing.Point(6, 103);
            this.lblMeas_MaxAmplitude_L.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblMeas_MaxAmplitude_L.Name = "lblMeas_MaxAmplitude_L";
            this.lblMeas_MaxAmplitude_L.Size = new System.Drawing.Size(95, 15);
            this.lblMeas_MaxAmplitude_L.TabIndex = 62;
            this.lblMeas_MaxAmplitude_L.Text = "Max. amplitude";
            // 
            // lblMeas_BW1_high_L
            // 
            this.lblMeas_BW1_high_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW1_high_L.Location = new System.Drawing.Point(101, 51);
            this.lblMeas_BW1_high_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW1_high_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW1_high_L.Name = "lblMeas_BW1_high_L";
            this.lblMeas_BW1_high_L.Size = new System.Drawing.Size(60, 13);
            this.lblMeas_BW1_high_L.TabIndex = 60;
            this.lblMeas_BW1_high_L.Text = "0.00 Hz";
            // 
            // lblMeas_BW1_low_L
            // 
            this.lblMeas_BW1_low_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW1_low_L.Location = new System.Drawing.Point(41, 51);
            this.lblMeas_BW1_low_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW1_low_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW1_low_L.Name = "lblMeas_BW1_low_L";
            this.lblMeas_BW1_low_L.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_BW1_low_L.TabIndex = 59;
            this.lblMeas_BW1_low_L.Text = "0.00 Hz";
            // 
            // lblMeas_BW1_L
            // 
            this.lblMeas_BW1_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW1_L.Location = new System.Drawing.Point(162, 51);
            this.lblMeas_BW1_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW1_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW1_L.Name = "lblMeas_BW1_L";
            this.lblMeas_BW1_L.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_BW1_L.TabIndex = 58;
            this.lblMeas_BW1_L.Text = "0.00 Hz";
            // 
            // lblMeas_BW3_high_L
            // 
            this.lblMeas_BW3_high_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW3_high_L.Location = new System.Drawing.Point(101, 71);
            this.lblMeas_BW3_high_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW3_high_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW3_high_L.Name = "lblMeas_BW3_high_L";
            this.lblMeas_BW3_high_L.Size = new System.Drawing.Size(60, 13);
            this.lblMeas_BW3_high_L.TabIndex = 57;
            this.lblMeas_BW3_high_L.Text = "0.00 Hz";
            // 
            // lblMeas_BW3_low_L
            // 
            this.lblMeas_BW3_low_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW3_low_L.Location = new System.Drawing.Point(41, 71);
            this.lblMeas_BW3_low_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW3_low_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW3_low_L.Name = "lblMeas_BW3_low_L";
            this.lblMeas_BW3_low_L.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_BW3_low_L.TabIndex = 55;
            this.lblMeas_BW3_low_L.Text = "0.00 Hz";
            // 
            // lblMeas_BW3_L
            // 
            this.lblMeas_BW3_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_BW3_L.Location = new System.Drawing.Point(162, 71);
            this.lblMeas_BW3_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_BW3_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_BW3_L.Name = "lblMeas_BW3_L";
            this.lblMeas_BW3_L.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_BW3_L.TabIndex = 53;
            this.lblMeas_BW3_L.Text = "0.00 Hz";
            // 
            // chk3dBBandWidth_L
            // 
            this.chk3dBBandWidth_L.AutoSize = true;
            this.chk3dBBandWidth_L.Checked = true;
            this.chk3dBBandWidth_L.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk3dBBandWidth_L.Location = new System.Drawing.Point(222, 70);
            this.chk3dBBandWidth_L.Name = "chk3dBBandWidth_L";
            this.chk3dBBandWidth_L.Size = new System.Drawing.Size(15, 14);
            this.chk3dBBandWidth_L.TabIndex = 52;
            this.chk3dBBandWidth_L.UseVisualStyleBackColor = true;
            this.chk3dBBandWidth_L.CheckedChanged += new System.EventHandler(this.chk3dBBandWidth_L_CheckedChanged);
            // 
            // label14
            // 
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(6, 71);
            this.label14.MinimumSize = new System.Drawing.Size(30, 13);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(42, 13);
            this.label14.TabIndex = 51;
            this.label14.Text = "3 dB";
            // 
            // label12
            // 
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(211, 30);
            this.label12.MinimumSize = new System.Drawing.Size(30, 13);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(42, 13);
            this.label12.TabIndex = 50;
            this.label12.Text = "Show";
            // 
            // label11
            // 
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(162, 31);
            this.label11.MinimumSize = new System.Drawing.Size(30, 13);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(42, 13);
            this.label11.TabIndex = 49;
            this.label11.Text = "BW";
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(101, 31);
            this.label10.MinimumSize = new System.Drawing.Size(30, 13);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(45, 13);
            this.label10.TabIndex = 48;
            this.label10.Text = "f(high)";
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(43, 31);
            this.label5.MinimumSize = new System.Drawing.Size(30, 13);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(42, 13);
            this.label5.TabIndex = 47;
            this.label5.Text = "f(low)";
            // 
            // label19
            // 
            this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.Location = new System.Drawing.Point(6, 51);
            this.label19.MinimumSize = new System.Drawing.Size(30, 13);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(42, 13);
            this.label19.TabIndex = 46;
            this.label19.Text = "1 dB";
            // 
            // chk1dBBandWidth_L
            // 
            this.chk1dBBandWidth_L.AutoSize = true;
            this.chk1dBBandWidth_L.Checked = true;
            this.chk1dBBandWidth_L.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk1dBBandWidth_L.Location = new System.Drawing.Point(222, 50);
            this.chk1dBBandWidth_L.Name = "chk1dBBandWidth_L";
            this.chk1dBBandWidth_L.Size = new System.Drawing.Size(15, 14);
            this.chk1dBBandWidth_L.TabIndex = 43;
            this.chk1dBBandWidth_L.UseVisualStyleBackColor = true;
            this.chk1dBBandWidth_L.CheckedChanged += new System.EventHandler(this.chk1dBBandWidth_CheckedChanged);
            // 
            // btnSingle
            // 
            this.btnSingle.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.btnSingle.FlatAppearance.BorderColor = System.Drawing.Color.CornflowerBlue;
            this.btnSingle.FlatAppearance.BorderSize = 2;
            this.btnSingle.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnSingle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSingle.Location = new System.Drawing.Point(97, 3);
            this.btnSingle.Name = "btnSingle";
            this.btnSingle.Size = new System.Drawing.Size(72, 37);
            this.btnSingle.TabIndex = 8;
            this.btnSingle.Text = "SINGLE";
            this.btnSingle.UseVisualStyleBackColor = false;
            this.btnSingle.Click += new System.EventHandler(this.btnSingle_Click);
            // 
            // btnStopThdMeasurement
            // 
            this.btnStopThdMeasurement.BackColor = System.Drawing.Color.LightCoral;
            this.btnStopThdMeasurement.Enabled = false;
            this.btnStopThdMeasurement.FlatAppearance.BorderColor = System.Drawing.Color.Tomato;
            this.btnStopThdMeasurement.FlatAppearance.BorderSize = 2;
            this.btnStopThdMeasurement.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnStopThdMeasurement.ForeColor = System.Drawing.Color.DimGray;
            this.btnStopThdMeasurement.Location = new System.Drawing.Point(179, 3);
            this.btnStopThdMeasurement.Name = "btnStopThdMeasurement";
            this.btnStopThdMeasurement.Size = new System.Drawing.Size(69, 37);
            this.btnStopThdMeasurement.TabIndex = 7;
            this.btnStopThdMeasurement.Text = "STOP";
            this.btnStopThdMeasurement.UseVisualStyleBackColor = false;
            this.btnStopThdMeasurement.Click += new System.EventHandler(this.btnStopThdMeasurement_Click);
            // 
            // btnRun
            // 
            this.btnRun.BackColor = System.Drawing.Color.LimeGreen;
            this.btnRun.FlatAppearance.BorderColor = System.Drawing.Color.CornflowerBlue;
            this.btnRun.FlatAppearance.BorderSize = 2;
            this.btnRun.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnRun.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRun.Location = new System.Drawing.Point(15, 3);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(72, 37);
            this.btnRun.TabIndex = 5;
            this.btnRun.Text = "RUN";
            this.btnRun.UseVisualStyleBackColor = false;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // grpMeasurmentSettings_Chirp
            // 
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.label3);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.cmbLowFrequencyAccuracy);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.lblLowFrequencyAccuracy);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.linkLabel1);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.label1);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.chkRightIsReference);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.cmbFrequencySpan);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.lblSmoothing);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.cmbSmoothing);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.lblGeneratorType);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.cmbGeneratorType);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.chkEnableRightChannel);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.chkEnableLeftChannel);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.udAverages);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.label4);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.label2);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.cmbGeneratorVoltageUnit);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.lblThdFreq_GenVoltage);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.txtGeneratorVoltage);
            this.grpMeasurmentSettings_Chirp.Controls.Add(this.lblStartFrequency);
            this.grpMeasurmentSettings_Chirp.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpMeasurmentSettings_Chirp.Location = new System.Drawing.Point(3, 46);
            this.grpMeasurmentSettings_Chirp.Name = "grpMeasurmentSettings_Chirp";
            this.grpMeasurmentSettings_Chirp.Size = new System.Drawing.Size(254, 250);
            this.grpMeasurmentSettings_Chirp.TabIndex = 0;
            this.grpMeasurmentSettings_Chirp.TabStop = false;
            this.grpMeasurmentSettings_Chirp.Text = "Measurement Settings";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label3.Location = new System.Drawing.Point(191, 111);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(20, 13);
            this.label3.TabIndex = 63;
            this.label3.Text = "Hz";
            // 
            // cmbLowFrequencyAccuracy
            // 
            this.cmbLowFrequencyAccuracy.DisplayMember = "1";
            this.cmbLowFrequencyAccuracy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLowFrequencyAccuracy.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbLowFrequencyAccuracy.FormattingEnabled = true;
            this.cmbLowFrequencyAccuracy.Location = new System.Drawing.Point(126, 106);
            this.cmbLowFrequencyAccuracy.Name = "cmbLowFrequencyAccuracy";
            this.cmbLowFrequencyAccuracy.Size = new System.Drawing.Size(58, 21);
            this.cmbLowFrequencyAccuracy.TabIndex = 62;
            this.cmbLowFrequencyAccuracy.SelectedIndexChanged += new System.EventHandler(this.cmbLowFrequencyAccuracy_SelectedIndexChanged);
            // 
            // lblLowFrequencyAccuracy
            // 
            this.lblLowFrequencyAccuracy.AutoSize = true;
            this.lblLowFrequencyAccuracy.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLowFrequencyAccuracy.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblLowFrequencyAccuracy.Location = new System.Drawing.Point(16, 109);
            this.lblLowFrequencyAccuracy.Name = "lblLowFrequencyAccuracy";
            this.lblLowFrequencyAccuracy.Size = new System.Drawing.Size(105, 13);
            this.lblLowFrequencyAccuracy.TabIndex = 61;
            this.lblLowFrequencyAccuracy.Text = "Frequency resolution";
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.LinkColor = System.Drawing.Color.Blue;
            this.linkLabel1.Location = new System.Drawing.Point(180, 140);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(14, 13);
            this.linkLabel1.TabIndex = 60;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "?";
            this.linkLabel1.VisitedLinkColor = System.Drawing.Color.Blue;
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label1.Location = new System.Drawing.Point(16, 140);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 13);
            this.label1.TabIndex = 59;
            this.label1.Text = "Right channel is reference";
            // 
            // chkRightIsReference
            // 
            this.chkRightIsReference.AutoSize = true;
            this.chkRightIsReference.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkRightIsReference.Location = new System.Drawing.Point(154, 140);
            this.chkRightIsReference.Name = "chkRightIsReference";
            this.chkRightIsReference.Size = new System.Drawing.Size(15, 14);
            this.chkRightIsReference.TabIndex = 58;
            this.chkRightIsReference.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkRightIsReference.UseVisualStyleBackColor = true;
            this.chkRightIsReference.CheckedChanged += new System.EventHandler(this.chkRightIsReference_CheckedChanged);
            // 
            // cmbFrequencySpan
            // 
            this.cmbFrequencySpan.DisplayMember = "1";
            this.cmbFrequencySpan.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFrequencySpan.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbFrequencySpan.FormattingEnabled = true;
            this.cmbFrequencySpan.Items.AddRange(new object[] {
            "mV",
            "V"});
            this.cmbFrequencySpan.Location = new System.Drawing.Point(126, 75);
            this.cmbFrequencySpan.Name = "cmbFrequencySpan";
            this.cmbFrequencySpan.Size = new System.Drawing.Size(58, 21);
            this.cmbFrequencySpan.TabIndex = 57;
            this.cmbFrequencySpan.SelectedIndexChanged += new System.EventHandler(this.cmbFrequencySpan_SelectedIndexChanged);
            // 
            // lblSmoothing
            // 
            this.lblSmoothing.AutoSize = true;
            this.lblSmoothing.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSmoothing.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblSmoothing.Location = new System.Drawing.Point(16, 167);
            this.lblSmoothing.Name = "lblSmoothing";
            this.lblSmoothing.Size = new System.Drawing.Size(57, 13);
            this.lblSmoothing.TabIndex = 54;
            this.lblSmoothing.Text = "Smoothing";
            // 
            // cmbSmoothing
            // 
            this.cmbSmoothing.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSmoothing.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbSmoothing.FormattingEnabled = true;
            this.cmbSmoothing.Items.AddRange(new object[] {
            "Input voltage",
            "Output voltage",
            "Output power"});
            this.cmbSmoothing.Location = new System.Drawing.Point(126, 163);
            this.cmbSmoothing.Name = "cmbSmoothing";
            this.cmbSmoothing.Size = new System.Drawing.Size(85, 21);
            this.cmbSmoothing.TabIndex = 53;
            this.cmbSmoothing.SelectedIndexChanged += new System.EventHandler(this.cmbSmoothing_SelectedIndexChanged);
            // 
            // lblGeneratorType
            // 
            this.lblGeneratorType.AutoSize = true;
            this.lblGeneratorType.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGeneratorType.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblGeneratorType.Location = new System.Drawing.Point(16, 21);
            this.lblGeneratorType.Name = "lblGeneratorType";
            this.lblGeneratorType.Size = new System.Drawing.Size(85, 13);
            this.lblGeneratorType.TabIndex = 52;
            this.lblGeneratorType.Text = "Set generator by";
            // 
            // cmbGeneratorType
            // 
            this.cmbGeneratorType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGeneratorType.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbGeneratorType.FormattingEnabled = true;
            this.cmbGeneratorType.Location = new System.Drawing.Point(126, 17);
            this.cmbGeneratorType.Name = "cmbGeneratorType";
            this.cmbGeneratorType.Size = new System.Drawing.Size(112, 21);
            this.cmbGeneratorType.TabIndex = 51;
            this.cmbGeneratorType.SelectedIndexChanged += new System.EventHandler(this.cmbGeneratorType_SelectedIndexChanged);
            // 
            // chkEnableRightChannel
            // 
            this.chkEnableRightChannel.AutoSize = true;
            this.chkEnableRightChannel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkEnableRightChannel.Location = new System.Drawing.Point(145, 224);
            this.chkEnableRightChannel.Name = "chkEnableRightChannel";
            this.chkEnableRightChannel.Size = new System.Drawing.Size(92, 17);
            this.chkEnableRightChannel.TabIndex = 47;
            this.chkEnableRightChannel.Text = "Right channel";
            this.chkEnableRightChannel.UseVisualStyleBackColor = true;
            this.chkEnableRightChannel.CheckedChanged += new System.EventHandler(this.chkEnableRightChannel_CheckedChanged);
            // 
            // chkEnableLeftChannel
            // 
            this.chkEnableLeftChannel.AutoSize = true;
            this.chkEnableLeftChannel.Checked = true;
            this.chkEnableLeftChannel.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnableLeftChannel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkEnableLeftChannel.Location = new System.Drawing.Point(20, 224);
            this.chkEnableLeftChannel.Name = "chkEnableLeftChannel";
            this.chkEnableLeftChannel.Size = new System.Drawing.Size(85, 17);
            this.chkEnableLeftChannel.TabIndex = 46;
            this.chkEnableLeftChannel.Text = "Left channel";
            this.chkEnableLeftChannel.UseVisualStyleBackColor = true;
            this.chkEnableLeftChannel.CheckedChanged += new System.EventHandler(this.chkEnableLeftChannel_CheckedChanged);
            // 
            // udAverages
            // 
            this.udAverages.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.udAverages.Location = new System.Drawing.Point(127, 195);
            this.udAverages.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.udAverages.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udAverages.Name = "udAverages";
            this.udAverages.Size = new System.Drawing.Size(58, 20);
            this.udAverages.TabIndex = 24;
            this.udAverages.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udAverages.ValueChanged += new System.EventHandler(this.udAverages_ValueChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label4.Location = new System.Drawing.Point(17, 197);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(52, 13);
            this.label4.TabIndex = 23;
            this.label4.Text = "Averages";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label2.Location = new System.Drawing.Point(191, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(20, 13);
            this.label2.TabIndex = 21;
            this.label2.Text = "Hz";
            // 
            // cmbGeneratorVoltageUnit
            // 
            this.cmbGeneratorVoltageUnit.DisplayMember = "1";
            this.cmbGeneratorVoltageUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGeneratorVoltageUnit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbGeneratorVoltageUnit.FormattingEnabled = true;
            this.cmbGeneratorVoltageUnit.Items.AddRange(new object[] {
            "mV",
            "V"});
            this.cmbGeneratorVoltageUnit.Location = new System.Drawing.Point(190, 44);
            this.cmbGeneratorVoltageUnit.Name = "cmbGeneratorVoltageUnit";
            this.cmbGeneratorVoltageUnit.Size = new System.Drawing.Size(48, 21);
            this.cmbGeneratorVoltageUnit.TabIndex = 12;
            this.cmbGeneratorVoltageUnit.SelectedIndexChanged += new System.EventHandler(this.cmbGeneratorAmplitudeUnit_SelectedIndexChanged);
            // 
            // lblThdFreq_GenVoltage
            // 
            this.lblThdFreq_GenVoltage.AutoSize = true;
            this.lblThdFreq_GenVoltage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblThdFreq_GenVoltage.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblThdFreq_GenVoltage.Location = new System.Drawing.Point(16, 50);
            this.lblThdFreq_GenVoltage.Name = "lblThdFreq_GenVoltage";
            this.lblThdFreq_GenVoltage.Size = new System.Drawing.Size(53, 13);
            this.lblThdFreq_GenVoltage.TabIndex = 11;
            this.lblThdFreq_GenVoltage.Text = "Amplitude";
            // 
            // txtGeneratorVoltage
            // 
            this.txtGeneratorVoltage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtGeneratorVoltage.Location = new System.Drawing.Point(126, 44);
            this.txtGeneratorVoltage.Name = "txtGeneratorVoltage";
            this.txtGeneratorVoltage.Size = new System.Drawing.Size(58, 20);
            this.txtGeneratorVoltage.TabIndex = 10;
            this.txtGeneratorVoltage.TextChanged += new System.EventHandler(this.txtGeneratorAmplitude_TextChanged);
            this.txtGeneratorVoltage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtGeneratorAmplitude_KeyPress);
            // 
            // lblStartFrequency
            // 
            this.lblStartFrequency.AutoSize = true;
            this.lblStartFrequency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStartFrequency.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblStartFrequency.Location = new System.Drawing.Point(16, 77);
            this.lblStartFrequency.Name = "lblStartFrequency";
            this.lblStartFrequency.Size = new System.Drawing.Size(83, 13);
            this.lblStartFrequency.TabIndex = 0;
            this.lblStartFrequency.Text = "Frequency span";
            // 
            // scGraphCursors
            // 
            this.scGraphCursors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scGraphCursors.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.scGraphCursors.IsSplitterFixed = true;
            this.scGraphCursors.Location = new System.Drawing.Point(0, 0);
            this.scGraphCursors.Name = "scGraphCursors";
            this.scGraphCursors.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scGraphCursors.Panel1
            // 
            this.scGraphCursors.Panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(239)))), ((int)(((byte)(239)))));
            this.scGraphCursors.Panel1.Controls.Add(this.scGraphSettings);
            this.scGraphCursors.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            // 
            // scGraphCursors.Panel2
            // 
            this.scGraphCursors.Panel2.BackColor = System.Drawing.SystemColors.Control;
            this.scGraphCursors.Panel2.Controls.Add(this.lblCursorGain);
            this.scGraphCursors.Panel2.Controls.Add(this.lblCursorAmplitude);
            this.scGraphCursors.Panel2.Controls.Add(this.lblCursor_Frequency);
            this.scGraphCursors.Panel2.Controls.Add(this.pnlCursorsRight);
            this.scGraphCursors.Panel2.Controls.Add(this.pnlCursorsLeft);
            this.scGraphCursors.Size = new System.Drawing.Size(836, 765);
            this.scGraphCursors.SplitterDistance = 710;
            this.scGraphCursors.TabIndex = 0;
            // 
            // scGraphSettings
            // 
            this.scGraphSettings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scGraphSettings.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.scGraphSettings.IsSplitterFixed = true;
            this.scGraphSettings.Location = new System.Drawing.Point(0, 0);
            this.scGraphSettings.Name = "scGraphSettings";
            // 
            // scGraphSettings.Panel1
            // 
            this.scGraphSettings.Panel1.Controls.Add(this.freqPlot);
            // 
            // scGraphSettings.Panel2
            // 
            this.scGraphSettings.Panel2.BackColor = System.Drawing.SystemColors.Control;
            this.scGraphSettings.Panel2.Controls.Add(this.gbFrequencyRange);
            this.scGraphSettings.Panel2.Controls.Add(this.chkGraphShowRightChannel);
            this.scGraphSettings.Panel2.Controls.Add(this.chkGraphShowLeftChannel);
            this.scGraphSettings.Panel2.Controls.Add(this.chkShowDataPoints);
            this.scGraphSettings.Panel2.Controls.Add(this.chkThickLines);
            this.scGraphSettings.Panel2.Controls.Add(this.btnGraph_dBV);
            this.scGraphSettings.Panel2.Controls.Add(this.btnGraph_Gain);
            this.scGraphSettings.Panel2.Controls.Add(this.gbDbV_Range);
            this.scGraphSettings.Size = new System.Drawing.Size(836, 710);
            this.scGraphSettings.SplitterDistance = 709;
            this.scGraphSettings.TabIndex = 0;
            // 
            // freqPlot
            // 
            this.freqPlot.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.freqPlot.DisplayScale = 0F;
            this.freqPlot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.freqPlot.Location = new System.Drawing.Point(0, 0);
            this.freqPlot.Name = "freqPlot";
            this.freqPlot.Size = new System.Drawing.Size(709, 710);
            this.freqPlot.TabIndex = 2;
            // 
            // gbFrequencyRange
            // 
            this.gbFrequencyRange.Controls.Add(this.label8);
            this.gbFrequencyRange.Controls.Add(this.cmbGraph_FreqEnd);
            this.gbFrequencyRange.Controls.Add(this.label9);
            this.gbFrequencyRange.Controls.Add(this.cmbGraph_FreqStart);
            this.gbFrequencyRange.Controls.Add(this.btnFitGraphX);
            this.gbFrequencyRange.Location = new System.Drawing.Point(5, 193);
            this.gbFrequencyRange.Name = "gbFrequencyRange";
            this.gbFrequencyRange.Size = new System.Drawing.Size(112, 161);
            this.gbFrequencyRange.TabIndex = 49;
            this.gbFrequencyRange.TabStop = false;
            this.gbFrequencyRange.Text = "Frequency range";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label8.Location = new System.Drawing.Point(8, 66);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(20, 13);
            this.label8.TabIndex = 34;
            this.label8.Text = "To";
            // 
            // cmbGraph_FreqEnd
            // 
            this.cmbGraph_FreqEnd.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGraph_FreqEnd.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbGraph_FreqEnd.FormattingEnabled = true;
            this.cmbGraph_FreqEnd.Location = new System.Drawing.Point(6, 82);
            this.cmbGraph_FreqEnd.Name = "cmbGraph_FreqEnd";
            this.cmbGraph_FreqEnd.Size = new System.Drawing.Size(94, 21);
            this.cmbGraph_FreqEnd.TabIndex = 33;
            this.cmbGraph_FreqEnd.SelectedIndexChanged += new System.EventHandler(this.cmbGraph_FreqEnd_SelectedIndexChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label9.Location = new System.Drawing.Point(8, 22);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(30, 13);
            this.label9.TabIndex = 32;
            this.label9.Text = "From";
            // 
            // cmbGraph_FreqStart
            // 
            this.cmbGraph_FreqStart.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGraph_FreqStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbGraph_FreqStart.FormattingEnabled = true;
            this.cmbGraph_FreqStart.Location = new System.Drawing.Point(6, 38);
            this.cmbGraph_FreqStart.Name = "cmbGraph_FreqStart";
            this.cmbGraph_FreqStart.Size = new System.Drawing.Size(94, 21);
            this.cmbGraph_FreqStart.TabIndex = 31;
            this.cmbGraph_FreqStart.SelectedIndexChanged += new System.EventHandler(this.cmbGraph_FreqStart_SelectedIndexChanged);
            // 
            // btnFitGraphX
            // 
            this.btnFitGraphX.Location = new System.Drawing.Point(6, 117);
            this.btnFitGraphX.Name = "btnFitGraphX";
            this.btnFitGraphX.Size = new System.Drawing.Size(94, 23);
            this.btnFitGraphX.TabIndex = 30;
            this.btnFitGraphX.Text = "Autofit";
            this.btnFitGraphX.UseVisualStyleBackColor = true;
            this.btnFitGraphX.Click += new System.EventHandler(this.btnFitGraphX_Click);
            // 
            // chkGraphShowRightChannel
            // 
            this.chkGraphShowRightChannel.AutoSize = true;
            this.chkGraphShowRightChannel.Checked = true;
            this.chkGraphShowRightChannel.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkGraphShowRightChannel.Location = new System.Drawing.Point(9, 442);
            this.chkGraphShowRightChannel.Name = "chkGraphShowRightChannel";
            this.chkGraphShowRightChannel.Size = new System.Drawing.Size(92, 17);
            this.chkGraphShowRightChannel.TabIndex = 47;
            this.chkGraphShowRightChannel.Text = "Right channel";
            this.chkGraphShowRightChannel.UseVisualStyleBackColor = true;
            this.chkGraphShowRightChannel.CheckedChanged += new System.EventHandler(this.chkGraphShowRightChannel_CheckedChanged);
            // 
            // chkGraphShowLeftChannel
            // 
            this.chkGraphShowLeftChannel.AutoSize = true;
            this.chkGraphShowLeftChannel.Checked = true;
            this.chkGraphShowLeftChannel.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkGraphShowLeftChannel.Location = new System.Drawing.Point(9, 421);
            this.chkGraphShowLeftChannel.Name = "chkGraphShowLeftChannel";
            this.chkGraphShowLeftChannel.Size = new System.Drawing.Size(85, 17);
            this.chkGraphShowLeftChannel.TabIndex = 48;
            this.chkGraphShowLeftChannel.Text = "Left channel";
            this.chkGraphShowLeftChannel.UseVisualStyleBackColor = true;
            this.chkGraphShowLeftChannel.CheckedChanged += new System.EventHandler(this.chkGraphShowLeftChannel_CheckedChanged);
            // 
            // chkShowDataPoints
            // 
            this.chkShowDataPoints.AutoSize = true;
            this.chkShowDataPoints.Location = new System.Drawing.Point(9, 383);
            this.chkShowDataPoints.Name = "chkShowDataPoints";
            this.chkShowDataPoints.Size = new System.Drawing.Size(108, 17);
            this.chkShowDataPoints.TabIndex = 45;
            this.chkShowDataPoints.Text = "Show data points";
            this.chkShowDataPoints.UseVisualStyleBackColor = true;
            this.chkShowDataPoints.CheckedChanged += new System.EventHandler(this.chkShowDataPoints_CheckedChanged);
            // 
            // chkThickLines
            // 
            this.chkThickLines.AutoSize = true;
            this.chkThickLines.Checked = true;
            this.chkThickLines.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkThickLines.Location = new System.Drawing.Point(9, 362);
            this.chkThickLines.Name = "chkThickLines";
            this.chkThickLines.Size = new System.Drawing.Size(77, 17);
            this.chkThickLines.TabIndex = 44;
            this.chkThickLines.Text = "Thick lines";
            this.chkThickLines.UseVisualStyleBackColor = true;
            this.chkThickLines.CheckedChanged += new System.EventHandler(this.chkThickLines_CheckedChanged);
            // 
            // btnGraph_dBV
            // 
            this.btnGraph_dBV.BackColor = System.Drawing.Color.Cornsilk;
            this.btnGraph_dBV.FlatAppearance.BorderColor = System.Drawing.Color.CornflowerBlue;
            this.btnGraph_dBV.FlatAppearance.BorderSize = 2;
            this.btnGraph_dBV.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnGraph_dBV.Location = new System.Drawing.Point(8, 483);
            this.btnGraph_dBV.Name = "btnGraph_dBV";
            this.btnGraph_dBV.Size = new System.Drawing.Size(52, 37);
            this.btnGraph_dBV.TabIndex = 28;
            this.btnGraph_dBV.Text = "FR (dBV)";
            this.btnGraph_dBV.UseVisualStyleBackColor = false;
            this.btnGraph_dBV.Click += new System.EventHandler(this.btnGraph_dBV_Click);
            // 
            // btnGraph_Gain
            // 
            this.btnGraph_Gain.BackColor = System.Drawing.Color.WhiteSmoke;
            this.btnGraph_Gain.FlatAppearance.BorderColor = System.Drawing.Color.CornflowerBlue;
            this.btnGraph_Gain.FlatAppearance.BorderSize = 2;
            this.btnGraph_Gain.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnGraph_Gain.Location = new System.Drawing.Point(66, 483);
            this.btnGraph_Gain.Name = "btnGraph_Gain";
            this.btnGraph_Gain.Size = new System.Drawing.Size(49, 37);
            this.btnGraph_Gain.TabIndex = 27;
            this.btnGraph_Gain.Text = "Gain (dB)";
            this.btnGraph_Gain.UseVisualStyleBackColor = false;
            this.btnGraph_Gain.Click += new System.EventHandler(this.btnGraph_Gain_Click);
            // 
            // gbDbV_Range
            // 
            this.gbDbV_Range.Controls.Add(this.ud_Graph_Bottom);
            this.gbDbV_Range.Controls.Add(this.ud_Graph_Top);
            this.gbDbV_Range.Controls.Add(this.label7);
            this.gbDbV_Range.Controls.Add(this.label6);
            this.gbDbV_Range.Controls.Add(this.btnD_FitGraphY);
            this.gbDbV_Range.Location = new System.Drawing.Point(5, 44);
            this.gbDbV_Range.Name = "gbDbV_Range";
            this.gbDbV_Range.Size = new System.Drawing.Size(113, 143);
            this.gbDbV_Range.TabIndex = 25;
            this.gbDbV_Range.TabStop = false;
            this.gbDbV_Range.Text = "dBV range";
            // 
            // ud_Graph_Bottom
            // 
            this.ud_Graph_Bottom.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ud_Graph_Bottom.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.ud_Graph_Bottom.Location = new System.Drawing.Point(8, 76);
            this.ud_Graph_Bottom.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
            this.ud_Graph_Bottom.Name = "ud_Graph_Bottom";
            this.ud_Graph_Bottom.Size = new System.Drawing.Size(94, 20);
            this.ud_Graph_Bottom.TabIndex = 33;
            this.ud_Graph_Bottom.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.ud_Graph_Bottom.ValueChanged += new System.EventHandler(this.ud_dBV_Graph_Bottom_ValueChanged);
            // 
            // ud_Graph_Top
            // 
            this.ud_Graph_Top.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ud_Graph_Top.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.ud_Graph_Top.Location = new System.Drawing.Point(8, 35);
            this.ud_Graph_Top.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
            this.ud_Graph_Top.Name = "ud_Graph_Top";
            this.ud_Graph_Top.Size = new System.Drawing.Size(94, 20);
            this.ud_Graph_Top.TabIndex = 32;
            this.ud_Graph_Top.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.ud_Graph_Top.ValueChanged += new System.EventHandler(this.ud_dBV_Graph_Top_ValueChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label7.Location = new System.Drawing.Point(8, 58);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(40, 13);
            this.label7.TabIndex = 29;
            this.label7.Text = "Bottom";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label6.Location = new System.Drawing.Point(8, 17);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(26, 13);
            this.label6.TabIndex = 27;
            this.label6.Text = "Top";
            // 
            // btnD_FitGraphY
            // 
            this.btnD_FitGraphY.Location = new System.Drawing.Point(6, 109);
            this.btnD_FitGraphY.Name = "btnD_FitGraphY";
            this.btnD_FitGraphY.Size = new System.Drawing.Size(94, 23);
            this.btnD_FitGraphY.TabIndex = 25;
            this.btnD_FitGraphY.Text = "Autofit";
            this.btnD_FitGraphY.UseVisualStyleBackColor = true;
            this.btnD_FitGraphY.Click += new System.EventHandler(this.btnFitGraphY_Click);
            // 
            // lblCursorGain
            // 
            this.lblCursorGain.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursorGain.Location = new System.Drawing.Point(316, 0);
            this.lblCursorGain.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblCursorGain.Name = "lblCursorGain";
            this.lblCursorGain.Size = new System.Drawing.Size(102, 13);
            this.lblCursorGain.TabIndex = 48;
            this.lblCursorGain.Text = "Gain";
            // 
            // lblCursorAmplitude
            // 
            this.lblCursorAmplitude.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursorAmplitude.Location = new System.Drawing.Point(162, 0);
            this.lblCursorAmplitude.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblCursorAmplitude.Name = "lblCursorAmplitude";
            this.lblCursorAmplitude.Size = new System.Drawing.Size(102, 13);
            this.lblCursorAmplitude.TabIndex = 46;
            this.lblCursorAmplitude.Text = "Amplitude";
            // 
            // lblCursor_Frequency
            // 
            this.lblCursor_Frequency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Frequency.Location = new System.Drawing.Point(43, 0);
            this.lblCursor_Frequency.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Frequency.MinimumSize = new System.Drawing.Size(100, 13);
            this.lblCursor_Frequency.Name = "lblCursor_Frequency";
            this.lblCursor_Frequency.Size = new System.Drawing.Size(116, 15);
            this.lblCursor_Frequency.TabIndex = 44;
            this.lblCursor_Frequency.Text = "F: 0.00 Hz";
            // 
            // pnlCursorsRight
            // 
            this.pnlCursorsRight.Controls.Add(this.lblCursor_Amplitude_V_R);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_Amplitude_dBV_R);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_RightChannel);
            this.pnlCursorsRight.Location = new System.Drawing.Point(45, 36);
            this.pnlCursorsRight.Name = "pnlCursorsRight";
            this.pnlCursorsRight.Size = new System.Drawing.Size(790, 16);
            this.pnlCursorsRight.TabIndex = 42;
            // 
            // lblCursor_Amplitude_V_R
            // 
            this.lblCursor_Amplitude_V_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Amplitude_V_R.Location = new System.Drawing.Point(187, 0);
            this.lblCursor_Amplitude_V_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Amplitude_V_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Amplitude_V_R.Name = "lblCursor_Amplitude_V_R";
            this.lblCursor_Amplitude_V_R.Size = new System.Drawing.Size(60, 13);
            this.lblCursor_Amplitude_V_R.TabIndex = 73;
            this.lblCursor_Amplitude_V_R.Text = "0.00 V";
            // 
            // lblCursor_Amplitude_dBV_R
            // 
            this.lblCursor_Amplitude_dBV_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Amplitude_dBV_R.Location = new System.Drawing.Point(117, 0);
            this.lblCursor_Amplitude_dBV_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Amplitude_dBV_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Amplitude_dBV_R.Name = "lblCursor_Amplitude_dBV_R";
            this.lblCursor_Amplitude_dBV_R.Size = new System.Drawing.Size(60, 13);
            this.lblCursor_Amplitude_dBV_R.TabIndex = 72;
            this.lblCursor_Amplitude_dBV_R.Text = "0.00 dBV";
            // 
            // lblCursor_RightChannel
            // 
            this.lblCursor_RightChannel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_RightChannel.Location = new System.Drawing.Point(-1, 0);
            this.lblCursor_RightChannel.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblCursor_RightChannel.Name = "lblCursor_RightChannel";
            this.lblCursor_RightChannel.Size = new System.Drawing.Size(86, 13);
            this.lblCursor_RightChannel.TabIndex = 40;
            this.lblCursor_RightChannel.Text = "Right channel";
            // 
            // pnlCursorsLeft
            // 
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_Gain_dB_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_Gain_times_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_Amplitude_V_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_Amplitude_dBV_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_LeftChannel);
            this.pnlCursorsLeft.Location = new System.Drawing.Point(43, 15);
            this.pnlCursorsLeft.Name = "pnlCursorsLeft";
            this.pnlCursorsLeft.Size = new System.Drawing.Size(790, 20);
            this.pnlCursorsLeft.TabIndex = 43;
            // 
            // lblCursor_Gain_dB_L
            // 
            this.lblCursor_Gain_dB_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Gain_dB_L.Location = new System.Drawing.Point(344, 2);
            this.lblCursor_Gain_dB_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Gain_dB_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Gain_dB_L.Name = "lblCursor_Gain_dB_L";
            this.lblCursor_Gain_dB_L.Size = new System.Drawing.Size(60, 13);
            this.lblCursor_Gain_dB_L.TabIndex = 71;
            this.lblCursor_Gain_dB_L.Text = "0.00 dB";
            // 
            // lblCursor_Gain_times_L
            // 
            this.lblCursor_Gain_times_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Gain_times_L.Location = new System.Drawing.Point(273, 2);
            this.lblCursor_Gain_times_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Gain_times_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Gain_times_L.Name = "lblCursor_Gain_times_L";
            this.lblCursor_Gain_times_L.Size = new System.Drawing.Size(60, 13);
            this.lblCursor_Gain_times_L.TabIndex = 70;
            this.lblCursor_Gain_times_L.Text = "0.00 x";
            // 
            // lblCursor_Amplitude_V_L
            // 
            this.lblCursor_Amplitude_V_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Amplitude_V_L.Location = new System.Drawing.Point(189, 2);
            this.lblCursor_Amplitude_V_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Amplitude_V_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Amplitude_V_L.Name = "lblCursor_Amplitude_V_L";
            this.lblCursor_Amplitude_V_L.Size = new System.Drawing.Size(60, 13);
            this.lblCursor_Amplitude_V_L.TabIndex = 69;
            this.lblCursor_Amplitude_V_L.Text = "0.00 V";
            // 
            // lblCursor_Amplitude_dBV_L
            // 
            this.lblCursor_Amplitude_dBV_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Amplitude_dBV_L.Location = new System.Drawing.Point(119, 2);
            this.lblCursor_Amplitude_dBV_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Amplitude_dBV_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Amplitude_dBV_L.Name = "lblCursor_Amplitude_dBV_L";
            this.lblCursor_Amplitude_dBV_L.Size = new System.Drawing.Size(60, 13);
            this.lblCursor_Amplitude_dBV_L.TabIndex = 68;
            this.lblCursor_Amplitude_dBV_L.Text = "0.00 dBV";
            // 
            // lblCursor_LeftChannel
            // 
            this.lblCursor_LeftChannel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_LeftChannel.Location = new System.Drawing.Point(0, 2);
            this.lblCursor_LeftChannel.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblCursor_LeftChannel.Name = "lblCursor_LeftChannel";
            this.lblCursor_LeftChannel.Size = new System.Drawing.Size(86, 13);
            this.lblCursor_LeftChannel.TabIndex = 39;
            this.lblCursor_LeftChannel.Text = "Left channel";
            // 
            // frmFrequencyResponse
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 765);
            this.Controls.Add(this.scThdVsFreq);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MinimumSize = new System.Drawing.Size(1100, 765);
            this.Name = "frmFrequencyResponse";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "QA40x Audio Analyser - V0.1";
            this.scThdVsFreq.Panel1.ResumeLayout(false);
            this.scThdVsFreq.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scThdVsFreq)).EndInit();
            this.scThdVsFreq.ResumeLayout(false);
            this.grpMeasurements_R.ResumeLayout(false);
            this.grpMeasurements_R.PerformLayout();
            this.grpMeasurements_L.ResumeLayout(false);
            this.grpMeasurements_L.PerformLayout();
            this.grpMeasurmentSettings_Chirp.ResumeLayout(false);
            this.grpMeasurmentSettings_Chirp.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udAverages)).EndInit();
            this.scGraphCursors.Panel1.ResumeLayout(false);
            this.scGraphCursors.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scGraphCursors)).EndInit();
            this.scGraphCursors.ResumeLayout(false);
            this.scGraphSettings.Panel1.ResumeLayout(false);
            this.scGraphSettings.Panel2.ResumeLayout(false);
            this.scGraphSettings.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scGraphSettings)).EndInit();
            this.scGraphSettings.ResumeLayout(false);
            this.gbFrequencyRange.ResumeLayout(false);
            this.gbFrequencyRange.PerformLayout();
            this.gbDbV_Range.ResumeLayout(false);
            this.gbDbV_Range.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ud_Graph_Bottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ud_Graph_Top)).EndInit();
            this.pnlCursorsRight.ResumeLayout(false);
            this.pnlCursorsLeft.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer scThdVsFreq;
        private System.Windows.Forms.Button btnStopThdMeasurement;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.GroupBox grpMeasurmentSettings_Chirp;
        private System.Windows.Forms.NumericUpDown udAverages;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbGeneratorVoltageUnit;
        private System.Windows.Forms.Label lblThdFreq_GenVoltage;
        private System.Windows.Forms.TextBox txtGeneratorVoltage;
        private System.Windows.Forms.Label lblStartFrequency;
        private System.Windows.Forms.SplitContainer scGraphCursors;
        private System.Windows.Forms.SplitContainer scGraphSettings;
        private ScottPlot.WinForms.FormsPlot freqPlot;
        private System.Windows.Forms.CheckBox chkShowDataPoints;
        private System.Windows.Forms.CheckBox chkThickLines;
        private System.Windows.Forms.Button btnGraph_dBV;
        private System.Windows.Forms.Button btnGraph_Gain;
        private System.Windows.Forms.GroupBox gbDbV_Range;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnD_FitGraphY;
        private System.Windows.Forms.CheckBox chkGraphShowRightChannel;
        private System.Windows.Forms.CheckBox chkGraphShowLeftChannel;
        private System.Windows.Forms.CheckBox chkEnableRightChannel;
        private System.Windows.Forms.CheckBox chkEnableLeftChannel;
        private System.Windows.Forms.Panel pnlCursorsRight;
        private System.Windows.Forms.Label lblCursor_RightChannel;
        private System.Windows.Forms.Panel pnlCursorsLeft;
        private System.Windows.Forms.Label lblCursor_LeftChannel;
        private System.Windows.Forms.Label lblCursor_Frequency;
        private System.Windows.Forms.GroupBox gbFrequencyRange;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cmbGraph_FreqEnd;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cmbGraph_FreqStart;
        private System.Windows.Forms.Button btnFitGraphX;
        private System.Windows.Forms.NumericUpDown ud_Graph_Bottom;
        private System.Windows.Forms.NumericUpDown ud_Graph_Top;
        private System.Windows.Forms.Label lblGeneratorType;
        private System.Windows.Forms.ComboBox cmbGeneratorType;
        private System.Windows.Forms.Button btnSingle;
        private System.Windows.Forms.Label lblSmoothing;
        private System.Windows.Forms.ComboBox cmbSmoothing;
        private System.Windows.Forms.Label lblCursorAmplitude;
        private System.Windows.Forms.GroupBox grpMeasurements_L;
        private System.Windows.Forms.CheckBox chk1dBBandWidth_L;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label lblMeas_BW3_L;
        private System.Windows.Forms.CheckBox chk3dBBandWidth_L;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblMeas_MaxAmplitude_L;
        private System.Windows.Forms.Label lblMeas_BW1_high_L;
        private System.Windows.Forms.Label lblMeas_BW1_low_L;
        private System.Windows.Forms.Label lblMeas_BW1_L;
        private System.Windows.Forms.Label lblMeas_BW3_high_L;
        private System.Windows.Forms.Label lblMeas_BW3_low_L;
        private System.Windows.Forms.Label lblMeas_Highest_Freq_L;
        private System.Windows.Forms.Label lblMeas_HighestGainFreq;
        private System.Windows.Forms.Label lblMeas_Amplitude_dBV_L;
        private System.Windows.Forms.Label lblMeas_Amplitude_V_L;
        private System.Windows.Forms.Label lblCursorGain;
        private System.Windows.Forms.GroupBox grpMeasurements_R;
        private System.Windows.Forms.Label lblMeas_Highest_Freq_R;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label lblMeas_Amplitude_dBV_R;
        private System.Windows.Forms.Label lblMeas_Amplitude_V_R;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.Label lblMeas_BW1_high_R;
        private System.Windows.Forms.Label lblMeas_BW1_low_R;
        private System.Windows.Forms.Label lblMeas_BW1_R;
        private System.Windows.Forms.Label lblMeas_BW3_high_R;
        private System.Windows.Forms.Label lblMeas_BW3_low_R;
        private System.Windows.Forms.Label lblMeas_BW3_R;
        private System.Windows.Forms.CheckBox chk3dBBandWidth_R;
        private System.Windows.Forms.Label label34;
        private System.Windows.Forms.Label label35;
        private System.Windows.Forms.Label label36;
        private System.Windows.Forms.Label label37;
        private System.Windows.Forms.Label label38;
        private System.Windows.Forms.Label label39;
        private System.Windows.Forms.CheckBox chk1dBBandWidth_R;
        private System.Windows.Forms.Label lblCursor_Amplitude_V_R;
        private System.Windows.Forms.Label lblCursor_Amplitude_dBV_R;
        private System.Windows.Forms.Label lblCursor_Gain_dB_L;
        private System.Windows.Forms.Label lblCursor_Gain_times_L;
        private System.Windows.Forms.Label lblCursor_Amplitude_V_L;
        private System.Windows.Forms.Label lblCursor_Amplitude_dBV_L;
        private System.Windows.Forms.ComboBox cmbFrequencySpan;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chkRightIsReference;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.ComboBox cmbLowFrequencyAccuracy;
        private System.Windows.Forms.Label lblLowFrequencyAccuracy;
        private System.Windows.Forms.Label label3;
    }
}