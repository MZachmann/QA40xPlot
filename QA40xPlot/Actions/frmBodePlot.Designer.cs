namespace QA40x_AUDIO_ANALYSER
{
    partial class frmBodePlot
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
            this.btnStopThdMeasurement = new System.Windows.Forms.Button();
            this.btnStartThdMeasurement = new System.Windows.Forms.Button();
            this.grpMeasurements_L = new System.Windows.Forms.GroupBox();
            this.lblMeas_Highest_Freq_L = new System.Windows.Forms.Label();
            this.lblMeas_HighestGainFreq = new System.Windows.Forms.Label();
            this.lblMeas_Gain_dB_L = new System.Windows.Forms.Label();
            this.lblMeas_Gain_L = new System.Windows.Forms.Label();
            this.lblMeas_MaxAmplitude_L = new System.Windows.Forms.Label();
            this.lblMeas_BW1_high_L = new System.Windows.Forms.Label();
            this.lblMeas_BW1_low_L = new System.Windows.Forms.Label();
            this.lblMeas_BW1_L = new System.Windows.Forms.Label();
            this.lblMeas_BW3_high_L = new System.Windows.Forms.Label();
            this.lblMeas_BW3_low_L = new System.Windows.Forms.Label();
            this.lblMeas_BW3_L = new System.Windows.Forms.Label();
            this.chk3dBBandWidth = new System.Windows.Forms.CheckBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.label21 = new System.Windows.Forms.Label();
            this.chk1dBBandWidth = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblGeneratorType = new System.Windows.Forms.Label();
            this.cmbGeneratorType = new System.Windows.Forms.ComboBox();
            this.cmbGeneratorVoltageUnit = new System.Windows.Forms.ComboBox();
            this.lblThdFreq_GenVoltage = new System.Windows.Forms.Label();
            this.txtGeneratorVoltage = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtEndFrequency = new System.Windows.Forms.TextBox();
            this.txtStartFrequency = new System.Windows.Forms.TextBox();
            this.udStepsOctave = new System.Windows.Forms.NumericUpDown();
            this.lblStepsPerOctave = new System.Windows.Forms.Label();
            this.lblThdFreq_EndFrequency = new System.Windows.Forms.Label();
            this.lblStartFrequency = new System.Windows.Forms.Label();
            this.scGraphCursors = new System.Windows.Forms.SplitContainer();
            this.scGraphSettings = new System.Windows.Forms.SplitContainer();
            this.phasePlot = new ScottPlot.WinForms.FormsPlot();
            this.freqPlot = new ScottPlot.WinForms.FormsPlot();
            this.grpPhase = new System.Windows.Forms.GroupBox();
            this.btnPhase180 = new System.Windows.Forms.Button();
            this.udPhase_Bottom = new System.Windows.Forms.NumericUpDown();
            this.udPhase_Top = new System.Windows.Forms.NumericUpDown();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.btnPhase_FitGraph = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkGraphShowGain = new System.Windows.Forms.CheckBox();
            this.chkGraphShowPhase = new System.Windows.Forms.CheckBox();
            this.gbFrequencyRange = new System.Windows.Forms.GroupBox();
            this.label8 = new System.Windows.Forms.Label();
            this.cmbGraph_FreqEnd = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cmbGraph_FreqStart = new System.Windows.Forms.ComboBox();
            this.btnFitGraphX = new System.Windows.Forms.Button();
            this.gbDb_Range = new System.Windows.Forms.GroupBox();
            this.ud_Graph_Bottom = new System.Windows.Forms.NumericUpDown();
            this.ud_Graph_Top = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.btnD_FitGraphY = new System.Windows.Forms.Button();
            this.chkShowDataPoints = new System.Windows.Forms.CheckBox();
            this.chkThickLines = new System.Windows.Forms.CheckBox();
            this.pnlCursorsLeft = new System.Windows.Forms.Panel();
            this.lblCursor_Phase = new System.Windows.Forms.Label();
            this.lblCursor_Gain = new System.Windows.Forms.Label();
            this.lblCursorMagnitude = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lblCursor_Frequency = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.scThdVsFreq)).BeginInit();
            this.scThdVsFreq.Panel1.SuspendLayout();
            this.scThdVsFreq.Panel2.SuspendLayout();
            this.scThdVsFreq.SuspendLayout();
            this.grpMeasurements_L.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udStepsOctave)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scGraphCursors)).BeginInit();
            this.scGraphCursors.Panel1.SuspendLayout();
            this.scGraphCursors.Panel2.SuspendLayout();
            this.scGraphCursors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scGraphSettings)).BeginInit();
            this.scGraphSettings.Panel1.SuspendLayout();
            this.scGraphSettings.Panel2.SuspendLayout();
            this.scGraphSettings.SuspendLayout();
            this.grpPhase.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udPhase_Bottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udPhase_Top)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.gbFrequencyRange.SuspendLayout();
            this.gbDb_Range.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ud_Graph_Bottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ud_Graph_Top)).BeginInit();
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
            this.scThdVsFreq.Panel1.Controls.Add(this.btnStopThdMeasurement);
            this.scThdVsFreq.Panel1.Controls.Add(this.btnStartThdMeasurement);
            this.scThdVsFreq.Panel1.Controls.Add(this.grpMeasurements_L);
            this.scThdVsFreq.Panel1.Controls.Add(this.groupBox1);
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
            // btnStopThdMeasurement
            // 
            this.btnStopThdMeasurement.BackColor = System.Drawing.Color.LightCoral;
            this.btnStopThdMeasurement.Enabled = false;
            this.btnStopThdMeasurement.FlatAppearance.BorderColor = System.Drawing.Color.Tomato;
            this.btnStopThdMeasurement.FlatAppearance.BorderSize = 2;
            this.btnStopThdMeasurement.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnStopThdMeasurement.ForeColor = System.Drawing.Color.DimGray;
            this.btnStopThdMeasurement.Location = new System.Drawing.Point(137, 3);
            this.btnStopThdMeasurement.Name = "btnStopThdMeasurement";
            this.btnStopThdMeasurement.Size = new System.Drawing.Size(109, 37);
            this.btnStopThdMeasurement.TabIndex = 58;
            this.btnStopThdMeasurement.Text = "STOP MEASUREMENT";
            this.btnStopThdMeasurement.UseVisualStyleBackColor = false;
            this.btnStopThdMeasurement.Click += new System.EventHandler(this.btnStopThdMeasurement_Click_1);
            // 
            // btnStartThdMeasurement
            // 
            this.btnStartThdMeasurement.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.btnStartThdMeasurement.FlatAppearance.BorderColor = System.Drawing.Color.CornflowerBlue;
            this.btnStartThdMeasurement.FlatAppearance.BorderSize = 2;
            this.btnStartThdMeasurement.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnStartThdMeasurement.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStartThdMeasurement.Location = new System.Drawing.Point(17, 3);
            this.btnStartThdMeasurement.Name = "btnStartThdMeasurement";
            this.btnStartThdMeasurement.Size = new System.Drawing.Size(109, 37);
            this.btnStartThdMeasurement.TabIndex = 57;
            this.btnStartThdMeasurement.Text = "START MEASUREMENT";
            this.btnStartThdMeasurement.UseVisualStyleBackColor = false;
            this.btnStartThdMeasurement.Click += new System.EventHandler(this.btnStartThdMeasurement_Click);
            // 
            // grpMeasurements_L
            // 
            this.grpMeasurements_L.Controls.Add(this.lblMeas_Highest_Freq_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_HighestGainFreq);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_Gain_dB_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_Gain_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_MaxAmplitude_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW1_high_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW1_low_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW1_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW3_high_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW3_low_L);
            this.grpMeasurements_L.Controls.Add(this.lblMeas_BW3_L);
            this.grpMeasurements_L.Controls.Add(this.chk3dBBandWidth);
            this.grpMeasurements_L.Controls.Add(this.label10);
            this.grpMeasurements_L.Controls.Add(this.label11);
            this.grpMeasurements_L.Controls.Add(this.label18);
            this.grpMeasurements_L.Controls.Add(this.label19);
            this.grpMeasurements_L.Controls.Add(this.label20);
            this.grpMeasurements_L.Controls.Add(this.label21);
            this.grpMeasurements_L.Controls.Add(this.chk1dBBandWidth);
            this.grpMeasurements_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpMeasurements_L.Location = new System.Drawing.Point(3, 218);
            this.grpMeasurements_L.Name = "grpMeasurements_L";
            this.grpMeasurements_L.Size = new System.Drawing.Size(254, 151);
            this.grpMeasurements_L.TabIndex = 55;
            this.grpMeasurements_L.TabStop = false;
            this.grpMeasurements_L.Text = "Measurements";
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
            this.lblMeas_HighestGainFreq.Text = "Highest gain freq.";
            // 
            // lblMeas_Gain_dB_L
            // 
            this.lblMeas_Gain_dB_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_Gain_dB_L.Location = new System.Drawing.Point(101, 103);
            this.lblMeas_Gain_dB_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_Gain_dB_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_Gain_dB_L.Name = "lblMeas_Gain_dB_L";
            this.lblMeas_Gain_dB_L.Size = new System.Drawing.Size(75, 15);
            this.lblMeas_Gain_dB_L.TabIndex = 65;
            this.lblMeas_Gain_dB_L.Text = "0.00 dB";
            // 
            // lblMeas_Gain_L
            // 
            this.lblMeas_Gain_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_Gain_L.Location = new System.Drawing.Point(183, 103);
            this.lblMeas_Gain_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblMeas_Gain_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblMeas_Gain_L.Name = "lblMeas_Gain_L";
            this.lblMeas_Gain_L.Size = new System.Drawing.Size(60, 15);
            this.lblMeas_Gain_L.TabIndex = 63;
            this.lblMeas_Gain_L.Text = "0.00 x";
            // 
            // lblMeas_MaxAmplitude_L
            // 
            this.lblMeas_MaxAmplitude_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMeas_MaxAmplitude_L.Location = new System.Drawing.Point(6, 103);
            this.lblMeas_MaxAmplitude_L.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblMeas_MaxAmplitude_L.Name = "lblMeas_MaxAmplitude_L";
            this.lblMeas_MaxAmplitude_L.Size = new System.Drawing.Size(95, 15);
            this.lblMeas_MaxAmplitude_L.TabIndex = 62;
            this.lblMeas_MaxAmplitude_L.Text = "Max. gain";
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
            // chk3dBBandWidth
            // 
            this.chk3dBBandWidth.AutoSize = true;
            this.chk3dBBandWidth.Checked = true;
            this.chk3dBBandWidth.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk3dBBandWidth.Location = new System.Drawing.Point(222, 70);
            this.chk3dBBandWidth.Name = "chk3dBBandWidth";
            this.chk3dBBandWidth.Size = new System.Drawing.Size(15, 14);
            this.chk3dBBandWidth.TabIndex = 52;
            this.chk3dBBandWidth.UseVisualStyleBackColor = true;
            this.chk3dBBandWidth.CheckedChanged += new System.EventHandler(this.chk3dBBandWidth_CheckedChanged);
            // 
            // label10
            // 
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(6, 71);
            this.label10.MinimumSize = new System.Drawing.Size(30, 13);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(42, 13);
            this.label10.TabIndex = 51;
            this.label10.Text = "3 dB";
            // 
            // label11
            // 
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(211, 30);
            this.label11.MinimumSize = new System.Drawing.Size(30, 13);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(42, 13);
            this.label11.TabIndex = 50;
            this.label11.Text = "Show";
            // 
            // label18
            // 
            this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.Location = new System.Drawing.Point(162, 31);
            this.label18.MinimumSize = new System.Drawing.Size(30, 13);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(42, 13);
            this.label18.TabIndex = 49;
            this.label18.Text = "BW";
            // 
            // label19
            // 
            this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.Location = new System.Drawing.Point(101, 31);
            this.label19.MinimumSize = new System.Drawing.Size(30, 13);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(55, 13);
            this.label19.TabIndex = 48;
            this.label19.Text = "f(high)";
            // 
            // label20
            // 
            this.label20.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.Location = new System.Drawing.Point(43, 31);
            this.label20.MinimumSize = new System.Drawing.Size(30, 13);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(42, 13);
            this.label20.TabIndex = 47;
            this.label20.Text = "f(low)";
            // 
            // label21
            // 
            this.label21.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label21.Location = new System.Drawing.Point(6, 51);
            this.label21.MinimumSize = new System.Drawing.Size(30, 13);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(42, 13);
            this.label21.TabIndex = 46;
            this.label21.Text = "1 dB";
            // 
            // chk1dBBandWidth
            // 
            this.chk1dBBandWidth.AutoSize = true;
            this.chk1dBBandWidth.Checked = true;
            this.chk1dBBandWidth.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk1dBBandWidth.Location = new System.Drawing.Point(222, 50);
            this.chk1dBBandWidth.Name = "chk1dBBandWidth";
            this.chk1dBBandWidth.Size = new System.Drawing.Size(15, 14);
            this.chk1dBBandWidth.TabIndex = 43;
            this.chk1dBBandWidth.UseVisualStyleBackColor = true;
            this.chk1dBBandWidth.CheckedChanged += new System.EventHandler(this.chk1dBBandWidth_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblGeneratorType);
            this.groupBox1.Controls.Add(this.cmbGeneratorType);
            this.groupBox1.Controls.Add(this.cmbGeneratorVoltageUnit);
            this.groupBox1.Controls.Add(this.lblThdFreq_GenVoltage);
            this.groupBox1.Controls.Add(this.txtGeneratorVoltage);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtEndFrequency);
            this.groupBox1.Controls.Add(this.txtStartFrequency);
            this.groupBox1.Controls.Add(this.udStepsOctave);
            this.groupBox1.Controls.Add(this.lblStepsPerOctave);
            this.groupBox1.Controls.Add(this.lblThdFreq_EndFrequency);
            this.groupBox1.Controls.Add(this.lblStartFrequency);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(3, 46);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(254, 162);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Measurement Settings";
            // 
            // lblGeneratorType
            // 
            this.lblGeneratorType.AutoSize = true;
            this.lblGeneratorType.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGeneratorType.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblGeneratorType.Location = new System.Drawing.Point(14, 23);
            this.lblGeneratorType.Name = "lblGeneratorType";
            this.lblGeneratorType.Size = new System.Drawing.Size(85, 13);
            this.lblGeneratorType.TabIndex = 57;
            this.lblGeneratorType.Text = "Set generator by";
            // 
            // cmbGeneratorType
            // 
            this.cmbGeneratorType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGeneratorType.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbGeneratorType.FormattingEnabled = true;
            this.cmbGeneratorType.Location = new System.Drawing.Point(128, 19);
            this.cmbGeneratorType.Name = "cmbGeneratorType";
            this.cmbGeneratorType.Size = new System.Drawing.Size(112, 21);
            this.cmbGeneratorType.TabIndex = 56;
            this.cmbGeneratorType.SelectedIndexChanged += new System.EventHandler(this.cmbGeneratorType_SelectedIndexChanged);
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
            this.cmbGeneratorVoltageUnit.Location = new System.Drawing.Point(192, 46);
            this.cmbGeneratorVoltageUnit.Name = "cmbGeneratorVoltageUnit";
            this.cmbGeneratorVoltageUnit.Size = new System.Drawing.Size(48, 21);
            this.cmbGeneratorVoltageUnit.TabIndex = 55;
            this.cmbGeneratorVoltageUnit.SelectedIndexChanged += new System.EventHandler(this.cmbGeneratorVoltageUnit_SelectedIndexChanged);
            // 
            // lblThdFreq_GenVoltage
            // 
            this.lblThdFreq_GenVoltage.AutoSize = true;
            this.lblThdFreq_GenVoltage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblThdFreq_GenVoltage.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblThdFreq_GenVoltage.Location = new System.Drawing.Point(14, 50);
            this.lblThdFreq_GenVoltage.Name = "lblThdFreq_GenVoltage";
            this.lblThdFreq_GenVoltage.Size = new System.Drawing.Size(53, 13);
            this.lblThdFreq_GenVoltage.TabIndex = 54;
            this.lblThdFreq_GenVoltage.Text = "Amplitude";
            // 
            // txtGeneratorVoltage
            // 
            this.txtGeneratorVoltage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtGeneratorVoltage.Location = new System.Drawing.Point(128, 46);
            this.txtGeneratorVoltage.Name = "txtGeneratorVoltage";
            this.txtGeneratorVoltage.Size = new System.Drawing.Size(58, 20);
            this.txtGeneratorVoltage.TabIndex = 53;
            this.txtGeneratorVoltage.TextChanged += new System.EventHandler(this.txtGeneratorVoltage_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label3.Location = new System.Drawing.Point(193, 102);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(20, 13);
            this.label3.TabIndex = 22;
            this.label3.Text = "Hz";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label2.Location = new System.Drawing.Point(193, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(20, 13);
            this.label2.TabIndex = 21;
            this.label2.Text = "Hz";
            // 
            // txtEndFrequency
            // 
            this.txtEndFrequency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtEndFrequency.Location = new System.Drawing.Point(128, 99);
            this.txtEndFrequency.Name = "txtEndFrequency";
            this.txtEndFrequency.Size = new System.Drawing.Size(59, 20);
            this.txtEndFrequency.TabIndex = 20;
            this.txtEndFrequency.Text = "20000";
            this.txtEndFrequency.TextChanged += new System.EventHandler(this.txtEndFreq_TextChanged);
            this.txtEndFrequency.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtEndFreq_KeyPress);
            // 
            // txtStartFrequency
            // 
            this.txtStartFrequency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStartFrequency.Location = new System.Drawing.Point(128, 73);
            this.txtStartFrequency.Name = "txtStartFrequency";
            this.txtStartFrequency.Size = new System.Drawing.Size(59, 20);
            this.txtStartFrequency.TabIndex = 19;
            this.txtStartFrequency.Text = "20";
            this.txtStartFrequency.TextChanged += new System.EventHandler(this.txtStartFreq_TextChanged);
            this.txtStartFrequency.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtStartFreq_KeyPress);
            // 
            // udStepsOctave
            // 
            this.udStepsOctave.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.udStepsOctave.Location = new System.Drawing.Point(128, 126);
            this.udStepsOctave.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.udStepsOctave.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udStepsOctave.Name = "udStepsOctave";
            this.udStepsOctave.Size = new System.Drawing.Size(59, 20);
            this.udStepsOctave.TabIndex = 7;
            this.udStepsOctave.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.udStepsOctave.ValueChanged += new System.EventHandler(this.udStepsOctave_ValueChanged);
            // 
            // lblStepsPerOctave
            // 
            this.lblStepsPerOctave.AutoSize = true;
            this.lblStepsPerOctave.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStepsPerOctave.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblStepsPerOctave.Location = new System.Drawing.Point(13, 128);
            this.lblStepsPerOctave.Name = "lblStepsPerOctave";
            this.lblStepsPerOctave.Size = new System.Drawing.Size(80, 13);
            this.lblStepsPerOctave.TabIndex = 4;
            this.lblStepsPerOctave.Text = "Steps / Octave";
            // 
            // lblThdFreq_EndFrequency
            // 
            this.lblThdFreq_EndFrequency.AutoSize = true;
            this.lblThdFreq_EndFrequency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblThdFreq_EndFrequency.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblThdFreq_EndFrequency.Location = new System.Drawing.Point(13, 102);
            this.lblThdFreq_EndFrequency.Name = "lblThdFreq_EndFrequency";
            this.lblThdFreq_EndFrequency.Size = new System.Drawing.Size(79, 13);
            this.lblThdFreq_EndFrequency.TabIndex = 2;
            this.lblThdFreq_EndFrequency.Text = "End Frequency";
            // 
            // lblStartFrequency
            // 
            this.lblStartFrequency.AutoSize = true;
            this.lblStartFrequency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStartFrequency.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblStartFrequency.Location = new System.Drawing.Point(13, 76);
            this.lblStartFrequency.Name = "lblStartFrequency";
            this.lblStartFrequency.Size = new System.Drawing.Size(82, 13);
            this.lblStartFrequency.TabIndex = 0;
            this.lblStartFrequency.Text = "Start Frequency";
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
            this.scGraphCursors.Panel2.Controls.Add(this.pnlCursorsLeft);
            this.scGraphCursors.Panel2.Controls.Add(this.lblCursorMagnitude);
            this.scGraphCursors.Panel2.Controls.Add(this.label1);
            this.scGraphCursors.Panel2.Controls.Add(this.lblCursor_Frequency);
            this.scGraphCursors.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.scGraphCursors_Panel2_Paint);
            this.scGraphCursors.Size = new System.Drawing.Size(836, 765);
            this.scGraphCursors.SplitterDistance = 725;
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
            this.scGraphSettings.Panel1.Controls.Add(this.phasePlot);
            this.scGraphSettings.Panel1.Controls.Add(this.freqPlot);
            this.scGraphSettings.Panel1.Resize += new System.EventHandler(this.scGraphSettings_Panel1_Resize);
            // 
            // scGraphSettings.Panel2
            // 
            this.scGraphSettings.Panel2.BackColor = System.Drawing.SystemColors.Control;
            this.scGraphSettings.Panel2.Controls.Add(this.grpPhase);
            this.scGraphSettings.Panel2.Controls.Add(this.groupBox2);
            this.scGraphSettings.Panel2.Controls.Add(this.gbFrequencyRange);
            this.scGraphSettings.Panel2.Controls.Add(this.gbDb_Range);
            this.scGraphSettings.Panel2.Controls.Add(this.chkShowDataPoints);
            this.scGraphSettings.Panel2.Controls.Add(this.chkThickLines);
            this.scGraphSettings.Size = new System.Drawing.Size(836, 725);
            this.scGraphSettings.SplitterDistance = 709;
            this.scGraphSettings.TabIndex = 0;
            // 
            // phasePlot
            // 
            this.phasePlot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.phasePlot.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.phasePlot.DisplayScale = 0F;
            this.phasePlot.Location = new System.Drawing.Point(0, 349);
            this.phasePlot.Name = "phasePlot";
            this.phasePlot.Size = new System.Drawing.Size(709, 373);
            this.phasePlot.TabIndex = 3;
            // 
            // freqPlot
            // 
            this.freqPlot.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.freqPlot.DisplayScale = 0F;
            this.freqPlot.Location = new System.Drawing.Point(0, 0);
            this.freqPlot.Name = "freqPlot";
            this.freqPlot.Size = new System.Drawing.Size(709, 343);
            this.freqPlot.TabIndex = 2;
            // 
            // grpPhase
            // 
            this.grpPhase.Controls.Add(this.btnPhase180);
            this.grpPhase.Controls.Add(this.udPhase_Bottom);
            this.grpPhase.Controls.Add(this.udPhase_Top);
            this.grpPhase.Controls.Add(this.label12);
            this.grpPhase.Controls.Add(this.label13);
            this.grpPhase.Controls.Add(this.btnPhase_FitGraph);
            this.grpPhase.Location = new System.Drawing.Point(4, 179);
            this.grpPhase.Name = "grpPhase";
            this.grpPhase.Size = new System.Drawing.Size(113, 168);
            this.grpPhase.TabIndex = 57;
            this.grpPhase.TabStop = false;
            this.grpPhase.Text = "Phase";
            // 
            // btnPhase180
            // 
            this.btnPhase180.Location = new System.Drawing.Point(6, 138);
            this.btnPhase180.Name = "btnPhase180";
            this.btnPhase180.Size = new System.Drawing.Size(94, 23);
            this.btnPhase180.TabIndex = 34;
            this.btnPhase180.Text = "+180 / -180";
            this.btnPhase180.UseVisualStyleBackColor = true;
            this.btnPhase180.Click += new System.EventHandler(this.btnPhase180_Click);
            // 
            // udPhase_Bottom
            // 
            this.udPhase_Bottom.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.udPhase_Bottom.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.udPhase_Bottom.Location = new System.Drawing.Point(8, 76);
            this.udPhase_Bottom.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.udPhase_Bottom.Minimum = new decimal(new int[] {
            200,
            0,
            0,
            -2147483648});
            this.udPhase_Bottom.Name = "udPhase_Bottom";
            this.udPhase_Bottom.Size = new System.Drawing.Size(94, 20);
            this.udPhase_Bottom.TabIndex = 33;
            this.udPhase_Bottom.Value = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
            this.udPhase_Bottom.ValueChanged += new System.EventHandler(this.ud_Graph_Phase_Bottom_ValueChanged);
            // 
            // udPhase_Top
            // 
            this.udPhase_Top.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.udPhase_Top.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.udPhase_Top.Location = new System.Drawing.Point(8, 35);
            this.udPhase_Top.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.udPhase_Top.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
            this.udPhase_Top.Name = "udPhase_Top";
            this.udPhase_Top.Size = new System.Drawing.Size(94, 20);
            this.udPhase_Top.TabIndex = 32;
            this.udPhase_Top.Value = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.udPhase_Top.ValueChanged += new System.EventHandler(this.ud_Graph_Phase_Top_ValueChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label12.Location = new System.Drawing.Point(8, 58);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(40, 13);
            this.label12.TabIndex = 29;
            this.label12.Text = "Bottom";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label13.Location = new System.Drawing.Point(8, 17);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(26, 13);
            this.label13.TabIndex = 27;
            this.label13.Text = "Top";
            // 
            // btnPhase_FitGraph
            // 
            this.btnPhase_FitGraph.Location = new System.Drawing.Point(6, 109);
            this.btnPhase_FitGraph.Name = "btnPhase_FitGraph";
            this.btnPhase_FitGraph.Size = new System.Drawing.Size(94, 23);
            this.btnPhase_FitGraph.TabIndex = 25;
            this.btnPhase_FitGraph.Text = "Autofit";
            this.btnPhase_FitGraph.UseVisualStyleBackColor = true;
            this.btnPhase_FitGraph.Click += new System.EventHandler(this.btnPhase_FitGraph_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chkGraphShowGain);
            this.groupBox2.Controls.Add(this.chkGraphShowPhase);
            this.groupBox2.Location = new System.Drawing.Point(5, 517);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(112, 75);
            this.groupBox2.TabIndex = 56;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Plot";
            // 
            // chkGraphShowGain
            // 
            this.chkGraphShowGain.AutoSize = true;
            this.chkGraphShowGain.Checked = true;
            this.chkGraphShowGain.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkGraphShowGain.Location = new System.Drawing.Point(11, 22);
            this.chkGraphShowGain.Name = "chkGraphShowGain";
            this.chkGraphShowGain.Size = new System.Drawing.Size(48, 17);
            this.chkGraphShowGain.TabIndex = 57;
            this.chkGraphShowGain.Text = "Gain";
            this.chkGraphShowGain.UseVisualStyleBackColor = true;
            this.chkGraphShowGain.CheckedChanged += new System.EventHandler(this.chkGraphShowGain_CheckedChanged);
            // 
            // chkGraphShowPhase
            // 
            this.chkGraphShowPhase.AutoSize = true;
            this.chkGraphShowPhase.Checked = true;
            this.chkGraphShowPhase.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkGraphShowPhase.Location = new System.Drawing.Point(11, 46);
            this.chkGraphShowPhase.Name = "chkGraphShowPhase";
            this.chkGraphShowPhase.Size = new System.Drawing.Size(56, 17);
            this.chkGraphShowPhase.TabIndex = 56;
            this.chkGraphShowPhase.Text = "Phase";
            this.chkGraphShowPhase.UseVisualStyleBackColor = true;
            this.chkGraphShowPhase.CheckedChanged += new System.EventHandler(this.chkGraphShowPhase_CheckedChanged);
            // 
            // gbFrequencyRange
            // 
            this.gbFrequencyRange.Controls.Add(this.label8);
            this.gbFrequencyRange.Controls.Add(this.cmbGraph_FreqEnd);
            this.gbFrequencyRange.Controls.Add(this.label9);
            this.gbFrequencyRange.Controls.Add(this.cmbGraph_FreqStart);
            this.gbFrequencyRange.Controls.Add(this.btnFitGraphX);
            this.gbFrequencyRange.Location = new System.Drawing.Point(5, 353);
            this.gbFrequencyRange.Name = "gbFrequencyRange";
            this.gbFrequencyRange.Size = new System.Drawing.Size(112, 161);
            this.gbFrequencyRange.TabIndex = 51;
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
            this.btnFitGraphX.Click += new System.EventHandler(this.btnFitGraphX_Click_1);
            // 
            // gbDb_Range
            // 
            this.gbDb_Range.Controls.Add(this.ud_Graph_Bottom);
            this.gbDb_Range.Controls.Add(this.ud_Graph_Top);
            this.gbDb_Range.Controls.Add(this.label7);
            this.gbDb_Range.Controls.Add(this.label6);
            this.gbDb_Range.Controls.Add(this.btnD_FitGraphY);
            this.gbDb_Range.Location = new System.Drawing.Point(5, 34);
            this.gbDb_Range.Name = "gbDb_Range";
            this.gbDb_Range.Size = new System.Drawing.Size(113, 143);
            this.gbDb_Range.TabIndex = 50;
            this.gbDb_Range.TabStop = false;
            this.gbDb_Range.Text = "dB range";
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
            this.ud_Graph_Bottom.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
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
            this.ud_Graph_Bottom.ValueChanged += new System.EventHandler(this.ud_Graph_Bottom_ValueChanged);
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
            this.ud_Graph_Top.ValueChanged += new System.EventHandler(this.ud_Graph_Top_ValueChanged);
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
            this.btnD_FitGraphY.Click += new System.EventHandler(this.btnD_FitGraphY_Click);
            // 
            // chkShowDataPoints
            // 
            this.chkShowDataPoints.AutoSize = true;
            this.chkShowDataPoints.Location = new System.Drawing.Point(11, 624);
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
            this.chkThickLines.Location = new System.Drawing.Point(11, 604);
            this.chkThickLines.Name = "chkThickLines";
            this.chkThickLines.Size = new System.Drawing.Size(77, 17);
            this.chkThickLines.TabIndex = 44;
            this.chkThickLines.Text = "Thick lines";
            this.chkThickLines.UseVisualStyleBackColor = true;
            this.chkThickLines.CheckedChanged += new System.EventHandler(this.chkThickLines_CheckedChanged);
            // 
            // pnlCursorsLeft
            // 
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_Phase);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_Gain);
            this.pnlCursorsLeft.Location = new System.Drawing.Point(44, 17);
            this.pnlCursorsLeft.Name = "pnlCursorsLeft";
            this.pnlCursorsLeft.Size = new System.Drawing.Size(790, 20);
            this.pnlCursorsLeft.TabIndex = 41;
            // 
            // lblCursor_Phase
            // 
            this.lblCursor_Phase.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Phase.Location = new System.Drawing.Point(176, 2);
            this.lblCursor_Phase.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Phase.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Phase.Name = "lblCursor_Phase";
            this.lblCursor_Phase.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_Phase.TabIndex = 11;
            this.lblCursor_Phase.Text = "0.00 dB";
            // 
            // lblCursor_Gain
            // 
            this.lblCursor_Gain.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Gain.Location = new System.Drawing.Point(110, 2);
            this.lblCursor_Gain.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Gain.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Gain.Name = "lblCursor_Gain";
            this.lblCursor_Gain.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_Gain.TabIndex = 12;
            this.lblCursor_Gain.Text = "0.00 %";
            // 
            // lblCursorMagnitude
            // 
            this.lblCursorMagnitude.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursorMagnitude.Location = new System.Drawing.Point(219, 2);
            this.lblCursorMagnitude.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblCursorMagnitude.Name = "lblCursorMagnitude";
            this.lblCursorMagnitude.Size = new System.Drawing.Size(70, 13);
            this.lblCursorMagnitude.TabIndex = 22;
            this.lblCursorMagnitude.Text = "Phase";
            this.lblCursorMagnitude.Click += new System.EventHandler(this.label4_Click);
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(153, 2);
            this.label1.MinimumSize = new System.Drawing.Size(50, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 13);
            this.label1.TabIndex = 21;
            this.label1.Text = "Gain";
            // 
            // lblCursor_Frequency
            // 
            this.lblCursor_Frequency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Frequency.Location = new System.Drawing.Point(44, 3);
            this.lblCursor_Frequency.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Frequency.MinimumSize = new System.Drawing.Size(100, 13);
            this.lblCursor_Frequency.Name = "lblCursor_Frequency";
            this.lblCursor_Frequency.Size = new System.Drawing.Size(116, 15);
            this.lblCursor_Frequency.TabIndex = 10;
            this.lblCursor_Frequency.Text = "F: 00.00 Hz";
            // 
            // frmBodePlot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 765);
            this.Controls.Add(this.scThdVsFreq);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MinimumSize = new System.Drawing.Size(1100, 765);
            this.Name = "frmBodePlot";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.scThdVsFreq.Panel1.ResumeLayout(false);
            this.scThdVsFreq.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scThdVsFreq)).EndInit();
            this.scThdVsFreq.ResumeLayout(false);
            this.grpMeasurements_L.ResumeLayout(false);
            this.grpMeasurements_L.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udStepsOctave)).EndInit();
            this.scGraphCursors.Panel1.ResumeLayout(false);
            this.scGraphCursors.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scGraphCursors)).EndInit();
            this.scGraphCursors.ResumeLayout(false);
            this.scGraphSettings.Panel1.ResumeLayout(false);
            this.scGraphSettings.Panel2.ResumeLayout(false);
            this.scGraphSettings.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scGraphSettings)).EndInit();
            this.scGraphSettings.ResumeLayout(false);
            this.grpPhase.ResumeLayout(false);
            this.grpPhase.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udPhase_Bottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.udPhase_Top)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.gbFrequencyRange.ResumeLayout(false);
            this.gbFrequencyRange.PerformLayout();
            this.gbDb_Range.ResumeLayout(false);
            this.gbDb_Range.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ud_Graph_Bottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ud_Graph_Top)).EndInit();
            this.pnlCursorsLeft.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer scThdVsFreq;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtEndFrequency;
        private System.Windows.Forms.TextBox txtStartFrequency;
        private System.Windows.Forms.NumericUpDown udStepsOctave;
        private System.Windows.Forms.Label lblStepsPerOctave;
        private System.Windows.Forms.Label lblThdFreq_EndFrequency;
        private System.Windows.Forms.Label lblStartFrequency;
        private System.Windows.Forms.SplitContainer scGraphCursors;
        private System.Windows.Forms.SplitContainer scGraphSettings;
        private ScottPlot.WinForms.FormsPlot freqPlot;
        private System.Windows.Forms.CheckBox chkShowDataPoints;
        private System.Windows.Forms.CheckBox chkThickLines;
        private System.Windows.Forms.Label lblCursor_Gain;
        private System.Windows.Forms.Label lblCursor_Phase;
        private System.Windows.Forms.Label lblCursor_Frequency;
        private System.Windows.Forms.Label lblCursorMagnitude;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnlCursorsLeft;
        private System.Windows.Forms.GroupBox gbFrequencyRange;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cmbGraph_FreqEnd;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cmbGraph_FreqStart;
        private System.Windows.Forms.Button btnFitGraphX;
        private System.Windows.Forms.GroupBox gbDb_Range;
        private System.Windows.Forms.NumericUpDown ud_Graph_Bottom;
        private System.Windows.Forms.NumericUpDown ud_Graph_Top;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnD_FitGraphY;
        private System.Windows.Forms.GroupBox grpMeasurements_L;
        private System.Windows.Forms.Label lblMeas_Highest_Freq_L;
        private System.Windows.Forms.Label lblMeas_HighestGainFreq;
        private System.Windows.Forms.Label lblMeas_Gain_dB_L;
        private System.Windows.Forms.Label lblMeas_Gain_L;
        private System.Windows.Forms.Label lblMeas_MaxAmplitude_L;
        private System.Windows.Forms.Label lblMeas_BW1_high_L;
        private System.Windows.Forms.Label lblMeas_BW1_low_L;
        private System.Windows.Forms.Label lblMeas_BW1_L;
        private System.Windows.Forms.Label lblMeas_BW3_high_L;
        private System.Windows.Forms.Label lblMeas_BW3_low_L;
        private System.Windows.Forms.Label lblMeas_BW3_L;
        private System.Windows.Forms.CheckBox chk3dBBandWidth;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.CheckBox chk1dBBandWidth;
        private System.Windows.Forms.Label lblGeneratorType;
        private System.Windows.Forms.ComboBox cmbGeneratorType;
        private System.Windows.Forms.ComboBox cmbGeneratorVoltageUnit;
        private System.Windows.Forms.Label lblThdFreq_GenVoltage;
        private System.Windows.Forms.TextBox txtGeneratorVoltage;
        private ScottPlot.WinForms.FormsPlot phasePlot;
        private System.Windows.Forms.Button btnStopThdMeasurement;
        private System.Windows.Forms.Button btnStartThdMeasurement;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox chkGraphShowGain;
        private System.Windows.Forms.CheckBox chkGraphShowPhase;
        private System.Windows.Forms.GroupBox grpPhase;
        private System.Windows.Forms.NumericUpDown udPhase_Bottom;
        private System.Windows.Forms.NumericUpDown udPhase_Top;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button btnPhase_FitGraph;
        private System.Windows.Forms.Button btnPhase180;
    }
}