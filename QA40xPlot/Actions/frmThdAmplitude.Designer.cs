namespace QA40x_AUDIO_ANALYSER
{
    partial class frmThdAmplitude
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
            this.graphFft = new ScottPlot.WinForms.FormsPlot();
            this.btnStartThdMeasurement = new System.Windows.Forms.Button();
            this.graphTime = new ScottPlot.WinForms.FormsPlot();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkEnableRightChannel = new System.Windows.Forms.CheckBox();
            this.chkEnableLeftChannel = new System.Windows.Forms.CheckBox();
            this.cmbEndVoltageUnit = new System.Windows.Forms.ComboBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtEndVoltage = new System.Windows.Forms.TextBox();
            this.udAverages = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtFrequency = new System.Windows.Forms.TextBox();
            this.lblThdFreq_OutputLoadUnit = new System.Windows.Forms.Label();
            this.lblThdFreq_OutputLoad = new System.Windows.Forms.Label();
            this.txtOutputLoad = new System.Windows.Forms.TextBox();
            this.cmbStartVoltageUnit = new System.Windows.Forms.ComboBox();
            this.lblThdFreq_GenVoltage = new System.Windows.Forms.Label();
            this.txtStartVoltage = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.udStepsOctave = new System.Windows.Forms.NumericUpDown();
            this.lblThdFreq_StepsPerOctave = new System.Windows.Forms.Label();
            this.lblFrequency = new System.Windows.Forms.Label();
            this.scGraphCursors = new System.Windows.Forms.SplitContainer();
            this.scGraphSettings = new System.Windows.Forms.SplitContainer();
            this.thdPlot = new ScottPlot.WinForms.FormsPlot();
            this.chkGraphShowRightChannel = new System.Windows.Forms.CheckBox();
            this.chkGraphShowLeftChannel = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.chkShowDataPoints = new System.Windows.Forms.CheckBox();
            this.chkThickLines = new System.Windows.Forms.CheckBox();
            this.cmbXAxis = new System.Windows.Forms.ComboBox();
            this.gbdB_Range = new System.Windows.Forms.GroupBox();
            this.ud_dB_Graph_Bottom = new System.Windows.Forms.NumericUpDown();
            this.ud_dB_Graph_Top = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.btnDbv_FitGraphY = new System.Windows.Forms.Button();
            this.gbThdFreq_Harmonics = new System.Windows.Forms.GroupBox();
            this.chkShowMagnitude = new System.Windows.Forms.CheckBox();
            this.chkShowNoiseFloor = new System.Windows.Forms.CheckBox();
            this.chkShowD6 = new System.Windows.Forms.CheckBox();
            this.chkShowD5 = new System.Windows.Forms.CheckBox();
            this.chkShowD4 = new System.Windows.Forms.CheckBox();
            this.chkShowD3 = new System.Windows.Forms.CheckBox();
            this.chkShowD2 = new System.Windows.Forms.CheckBox();
            this.chkShowThd = new System.Windows.Forms.CheckBox();
            this.btnGraph_dB = new System.Windows.Forms.Button();
            this.btnGraph_D_Percent = new System.Windows.Forms.Button();
            this.gbXAxisRange = new System.Windows.Forms.GroupBox();
            this.btnAutoFitX = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.cmbGraph_VoltageEnd = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cmbGraph_VoltageStart = new System.Windows.Forms.ComboBox();
            this.gbD_Range = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cmbD_Graph_Bottom = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbD_Graph_Top = new System.Windows.Forms.ComboBox();
            this.btnD_FitGraphY = new System.Windows.Forms.Button();
            this.label17 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.lblCursorMagnitude = new System.Windows.Forms.Label();
            this.label19 = new System.Windows.Forms.Label();
            this.lblCursor_X_Axis = new System.Windows.Forms.Label();
            this.pnlCursorsRight = new System.Windows.Forms.Panel();
            this.label28 = new System.Windows.Forms.Label();
            this.lblCursor_Magnitude_R = new System.Windows.Forms.Label();
            this.lblCursor_NoiseFloor_R = new System.Windows.Forms.Label();
            this.lblCursor_THD_R = new System.Windows.Forms.Label();
            this.lblCursor_Power_R = new System.Windows.Forms.Label();
            this.lblCursor_D2_R = new System.Windows.Forms.Label();
            this.lblCursor_D6_R = new System.Windows.Forms.Label();
            this.lblCursor_D3_R = new System.Windows.Forms.Label();
            this.lblCursor_D5_R = new System.Windows.Forms.Label();
            this.lblCursor_D4_R = new System.Windows.Forms.Label();
            this.pnlCursorsLeft = new System.Windows.Forms.Panel();
            this.lblCursor_D5_L = new System.Windows.Forms.Label();
            this.lblCursor_Magnitude_L = new System.Windows.Forms.Label();
            this.label27 = new System.Windows.Forms.Label();
            this.lblCursor_THD_L = new System.Windows.Forms.Label();
            this.lblCursor_D2_L = new System.Windows.Forms.Label();
            this.lblCursor_D3_L = new System.Windows.Forms.Label();
            this.lblCursor_D4_L = new System.Windows.Forms.Label();
            this.lblCursor_D6_L = new System.Windows.Forms.Label();
            this.lblCursor_Power_L = new System.Windows.Forms.Label();
            this.lblCursor_NoiseFloor_L = new System.Windows.Forms.Label();
            this.lblCursor_Vout_L = new System.Windows.Forms.Label();
            this.lblCursor_Vout_R = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.scThdVsFreq)).BeginInit();
            this.scThdVsFreq.Panel1.SuspendLayout();
            this.scThdVsFreq.Panel2.SuspendLayout();
            this.scThdVsFreq.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udAverages)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.udStepsOctave)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.scGraphCursors)).BeginInit();
            this.scGraphCursors.Panel1.SuspendLayout();
            this.scGraphCursors.Panel2.SuspendLayout();
            this.scGraphCursors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scGraphSettings)).BeginInit();
            this.scGraphSettings.Panel1.SuspendLayout();
            this.scGraphSettings.Panel2.SuspendLayout();
            this.scGraphSettings.SuspendLayout();
            this.gbdB_Range.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ud_dB_Graph_Bottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ud_dB_Graph_Top)).BeginInit();
            this.gbThdFreq_Harmonics.SuspendLayout();
            this.gbXAxisRange.SuspendLayout();
            this.gbD_Range.SuspendLayout();
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
            this.scThdVsFreq.Panel1.Controls.Add(this.btnStopThdMeasurement);
            this.scThdVsFreq.Panel1.Controls.Add(this.graphFft);
            this.scThdVsFreq.Panel1.Controls.Add(this.btnStartThdMeasurement);
            this.scThdVsFreq.Panel1.Controls.Add(this.graphTime);
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
            this.btnStopThdMeasurement.Location = new System.Drawing.Point(135, 3);
            this.btnStopThdMeasurement.Name = "btnStopThdMeasurement";
            this.btnStopThdMeasurement.Size = new System.Drawing.Size(109, 37);
            this.btnStopThdMeasurement.TabIndex = 7;
            this.btnStopThdMeasurement.Text = "STOP MEASUREMENT";
            this.btnStopThdMeasurement.UseVisualStyleBackColor = false;
            this.btnStopThdMeasurement.Click += new System.EventHandler(this.btnStopThdMeasurement_Click);
            // 
            // graphFft
            // 
            this.graphFft.DisplayScale = 0F;
            this.graphFft.Location = new System.Drawing.Point(3, 311);
            this.graphFft.Name = "graphFft";
            this.graphFft.Size = new System.Drawing.Size(254, 160);
            this.graphFft.TabIndex = 6;
            // 
            // btnStartThdMeasurement
            // 
            this.btnStartThdMeasurement.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.btnStartThdMeasurement.FlatAppearance.BorderColor = System.Drawing.Color.CornflowerBlue;
            this.btnStartThdMeasurement.FlatAppearance.BorderSize = 2;
            this.btnStartThdMeasurement.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnStartThdMeasurement.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStartThdMeasurement.Location = new System.Drawing.Point(15, 3);
            this.btnStartThdMeasurement.Name = "btnStartThdMeasurement";
            this.btnStartThdMeasurement.Size = new System.Drawing.Size(109, 37);
            this.btnStartThdMeasurement.TabIndex = 5;
            this.btnStartThdMeasurement.Text = "START MEASUREMENT";
            this.btnStartThdMeasurement.UseVisualStyleBackColor = false;
            this.btnStartThdMeasurement.Click += new System.EventHandler(this.btnStartThdMeasurement_Click);
            // 
            // graphTime
            // 
            this.graphTime.DisplayScale = 0F;
            this.graphTime.Location = new System.Drawing.Point(3, 473);
            this.graphTime.Name = "graphTime";
            this.graphTime.Size = new System.Drawing.Size(254, 160);
            this.graphTime.TabIndex = 3;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkEnableRightChannel);
            this.groupBox1.Controls.Add(this.chkEnableLeftChannel);
            this.groupBox1.Controls.Add(this.cmbEndVoltageUnit);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.txtEndVoltage);
            this.groupBox1.Controls.Add(this.udAverages);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtFrequency);
            this.groupBox1.Controls.Add(this.lblThdFreq_OutputLoadUnit);
            this.groupBox1.Controls.Add(this.lblThdFreq_OutputLoad);
            this.groupBox1.Controls.Add(this.txtOutputLoad);
            this.groupBox1.Controls.Add(this.cmbStartVoltageUnit);
            this.groupBox1.Controls.Add(this.lblThdFreq_GenVoltage);
            this.groupBox1.Controls.Add(this.txtStartVoltage);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.udStepsOctave);
            this.groupBox1.Controls.Add(this.lblThdFreq_StepsPerOctave);
            this.groupBox1.Controls.Add(this.lblFrequency);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(3, 46);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(254, 259);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Measurement Settings";
            // 
            // chkEnableRightChannel
            // 
            this.chkEnableRightChannel.AutoSize = true;
            this.chkEnableRightChannel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkEnableRightChannel.Location = new System.Drawing.Point(144, 237);
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
            this.chkEnableLeftChannel.Location = new System.Drawing.Point(19, 237);
            this.chkEnableLeftChannel.Name = "chkEnableLeftChannel";
            this.chkEnableLeftChannel.Size = new System.Drawing.Size(85, 17);
            this.chkEnableLeftChannel.TabIndex = 46;
            this.chkEnableLeftChannel.Text = "Left channel";
            this.chkEnableLeftChannel.UseVisualStyleBackColor = true;
            this.chkEnableLeftChannel.CheckedChanged += new System.EventHandler(this.chkEnableLeftChannel_CheckedChanged);
            // 
            // cmbEndVoltageUnit
            // 
            this.cmbEndVoltageUnit.DisplayMember = "1";
            this.cmbEndVoltageUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEndVoltageUnit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbEndVoltageUnit.FormattingEnabled = true;
            this.cmbEndVoltageUnit.Items.AddRange(new object[] {
            "mV",
            "V"});
            this.cmbEndVoltageUnit.Location = new System.Drawing.Point(193, 71);
            this.cmbEndVoltageUnit.Name = "cmbEndVoltageUnit";
            this.cmbEndVoltageUnit.Size = new System.Drawing.Size(48, 21);
            this.cmbEndVoltageUnit.TabIndex = 30;
            this.cmbEndVoltageUnit.SelectedIndexChanged += new System.EventHandler(this.cmbEndVoltageUnit_SelectedIndexChanged);
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label12.Location = new System.Drawing.Point(20, 71);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(65, 13);
            this.label12.TabIndex = 29;
            this.label12.Text = "End Voltage";
            // 
            // txtEndVoltage
            // 
            this.txtEndVoltage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtEndVoltage.Location = new System.Drawing.Point(127, 71);
            this.txtEndVoltage.Name = "txtEndVoltage";
            this.txtEndVoltage.Size = new System.Drawing.Size(59, 20);
            this.txtEndVoltage.TabIndex = 28;
            this.txtEndVoltage.TextChanged += new System.EventHandler(this.txtEndVoltage_TextChanged);
            this.txtEndVoltage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtEndVoltage_KeyPress);
            // 
            // udAverages
            // 
            this.udAverages.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.udAverages.Location = new System.Drawing.Point(127, 202);
            this.udAverages.Maximum = new decimal(new int[] {
            20,
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
            this.label4.Location = new System.Drawing.Point(17, 204);
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
            this.label2.Location = new System.Drawing.Point(192, 152);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(20, 13);
            this.label2.TabIndex = 21;
            this.label2.Text = "Hz";
            // 
            // txtFrequency
            // 
            this.txtFrequency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFrequency.Location = new System.Drawing.Point(127, 149);
            this.txtFrequency.Name = "txtFrequency";
            this.txtFrequency.Size = new System.Drawing.Size(59, 20);
            this.txtFrequency.TabIndex = 19;
            this.txtFrequency.Text = "1000";
            this.txtFrequency.TextChanged += new System.EventHandler(this.txtFrequency_TextChanged);
            this.txtFrequency.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtStartFreq_KeyPress);
            // 
            // lblThdFreq_OutputLoadUnit
            // 
            this.lblThdFreq_OutputLoadUnit.AutoSize = true;
            this.lblThdFreq_OutputLoadUnit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblThdFreq_OutputLoadUnit.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblThdFreq_OutputLoadUnit.Location = new System.Drawing.Point(193, 126);
            this.lblThdFreq_OutputLoadUnit.Name = "lblThdFreq_OutputLoadUnit";
            this.lblThdFreq_OutputLoadUnit.Size = new System.Drawing.Size(16, 13);
            this.lblThdFreq_OutputLoadUnit.TabIndex = 15;
            this.lblThdFreq_OutputLoadUnit.Text = "Ω";
            // 
            // lblThdFreq_OutputLoad
            // 
            this.lblThdFreq_OutputLoad.AutoSize = true;
            this.lblThdFreq_OutputLoad.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblThdFreq_OutputLoad.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblThdFreq_OutputLoad.Location = new System.Drawing.Point(17, 126);
            this.lblThdFreq_OutputLoad.Name = "lblThdFreq_OutputLoad";
            this.lblThdFreq_OutputLoad.Size = new System.Drawing.Size(73, 13);
            this.lblThdFreq_OutputLoad.TabIndex = 14;
            this.lblThdFreq_OutputLoad.Text = "Amplifier Load";
            // 
            // txtOutputLoad
            // 
            this.txtOutputLoad.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtOutputLoad.Location = new System.Drawing.Point(127, 123);
            this.txtOutputLoad.Name = "txtOutputLoad";
            this.txtOutputLoad.Size = new System.Drawing.Size(60, 20);
            this.txtOutputLoad.TabIndex = 13;
            this.txtOutputLoad.Text = "8";
            this.txtOutputLoad.TextChanged += new System.EventHandler(this.txtOutputLoad_TextChanged);
            this.txtOutputLoad.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtOutputLoad_KeyPress);
            // 
            // cmbStartVoltageUnit
            // 
            this.cmbStartVoltageUnit.DisplayMember = "1";
            this.cmbStartVoltageUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStartVoltageUnit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbStartVoltageUnit.FormattingEnabled = true;
            this.cmbStartVoltageUnit.Items.AddRange(new object[] {
            "mV",
            "V"});
            this.cmbStartVoltageUnit.Location = new System.Drawing.Point(193, 44);
            this.cmbStartVoltageUnit.Name = "cmbStartVoltageUnit";
            this.cmbStartVoltageUnit.Size = new System.Drawing.Size(48, 21);
            this.cmbStartVoltageUnit.TabIndex = 12;
            this.cmbStartVoltageUnit.SelectedIndexChanged += new System.EventHandler(this.cmbStartVoltageUnit_SelectedIndexChanged);
            // 
            // lblThdFreq_GenVoltage
            // 
            this.lblThdFreq_GenVoltage.AutoSize = true;
            this.lblThdFreq_GenVoltage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblThdFreq_GenVoltage.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblThdFreq_GenVoltage.Location = new System.Drawing.Point(20, 48);
            this.lblThdFreq_GenVoltage.Name = "lblThdFreq_GenVoltage";
            this.lblThdFreq_GenVoltage.Size = new System.Drawing.Size(68, 13);
            this.lblThdFreq_GenVoltage.TabIndex = 11;
            this.lblThdFreq_GenVoltage.Text = "Start Voltage";
            this.lblThdFreq_GenVoltage.Click += new System.EventHandler(this.lblThdFreq_GenVoltage_Click);
            // 
            // txtStartVoltage
            // 
            this.txtStartVoltage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtStartVoltage.Location = new System.Drawing.Point(127, 45);
            this.txtStartVoltage.Name = "txtStartVoltage";
            this.txtStartVoltage.Size = new System.Drawing.Size(60, 20);
            this.txtStartVoltage.TabIndex = 10;
            this.txtStartVoltage.TextChanged += new System.EventHandler(this.txtStartVoltage_TextChanged);
            this.txtStartVoltage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtStartVoltage_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label1.Location = new System.Drawing.Point(95, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Input voltage";
            // 
            // udStepsOctave
            // 
            this.udStepsOctave.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.udStepsOctave.Location = new System.Drawing.Point(127, 175);
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
            // lblThdFreq_StepsPerOctave
            // 
            this.lblThdFreq_StepsPerOctave.AutoSize = true;
            this.lblThdFreq_StepsPerOctave.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblThdFreq_StepsPerOctave.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblThdFreq_StepsPerOctave.Location = new System.Drawing.Point(17, 177);
            this.lblThdFreq_StepsPerOctave.Name = "lblThdFreq_StepsPerOctave";
            this.lblThdFreq_StepsPerOctave.Size = new System.Drawing.Size(80, 13);
            this.lblThdFreq_StepsPerOctave.TabIndex = 4;
            this.lblThdFreq_StepsPerOctave.Text = "Steps / Octave";
            // 
            // lblFrequency
            // 
            this.lblFrequency.AutoSize = true;
            this.lblFrequency.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFrequency.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblFrequency.Location = new System.Drawing.Point(17, 152);
            this.lblFrequency.Name = "lblFrequency";
            this.lblFrequency.Size = new System.Drawing.Size(81, 13);
            this.lblFrequency.TabIndex = 0;
            this.lblFrequency.Text = "Test Frequency";
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
            this.scGraphCursors.Panel2.Controls.Add(this.label17);
            this.scGraphCursors.Panel2.Controls.Add(this.label16);
            this.scGraphCursors.Panel2.Controls.Add(this.label15);
            this.scGraphCursors.Panel2.Controls.Add(this.label14);
            this.scGraphCursors.Panel2.Controls.Add(this.label13);
            this.scGraphCursors.Panel2.Controls.Add(this.label5);
            this.scGraphCursors.Panel2.Controls.Add(this.label18);
            this.scGraphCursors.Panel2.Controls.Add(this.lblCursorMagnitude);
            this.scGraphCursors.Panel2.Controls.Add(this.label19);
            this.scGraphCursors.Panel2.Controls.Add(this.lblCursor_X_Axis);
            this.scGraphCursors.Panel2.Controls.Add(this.pnlCursorsRight);
            this.scGraphCursors.Panel2.Controls.Add(this.pnlCursorsLeft);
            this.scGraphCursors.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.scGraphCursors_Panel2_Paint);
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
            this.scGraphSettings.Panel1.Controls.Add(this.thdPlot);
            // 
            // scGraphSettings.Panel2
            // 
            this.scGraphSettings.Panel2.BackColor = System.Drawing.SystemColors.Control;
            this.scGraphSettings.Panel2.Controls.Add(this.chkGraphShowRightChannel);
            this.scGraphSettings.Panel2.Controls.Add(this.chkGraphShowLeftChannel);
            this.scGraphSettings.Panel2.Controls.Add(this.label3);
            this.scGraphSettings.Panel2.Controls.Add(this.chkShowDataPoints);
            this.scGraphSettings.Panel2.Controls.Add(this.chkThickLines);
            this.scGraphSettings.Panel2.Controls.Add(this.cmbXAxis);
            this.scGraphSettings.Panel2.Controls.Add(this.gbdB_Range);
            this.scGraphSettings.Panel2.Controls.Add(this.gbThdFreq_Harmonics);
            this.scGraphSettings.Panel2.Controls.Add(this.btnGraph_dB);
            this.scGraphSettings.Panel2.Controls.Add(this.btnGraph_D_Percent);
            this.scGraphSettings.Panel2.Controls.Add(this.gbXAxisRange);
            this.scGraphSettings.Panel2.Controls.Add(this.gbD_Range);
            this.scGraphSettings.Size = new System.Drawing.Size(836, 710);
            this.scGraphSettings.SplitterDistance = 709;
            this.scGraphSettings.TabIndex = 0;
            // 
            // thdPlot
            // 
            this.thdPlot.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.thdPlot.DisplayScale = 0F;
            this.thdPlot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.thdPlot.Location = new System.Drawing.Point(0, 0);
            this.thdPlot.Name = "thdPlot";
            this.thdPlot.Size = new System.Drawing.Size(709, 710);
            this.thdPlot.TabIndex = 2;
            // 
            // chkGraphShowRightChannel
            // 
            this.chkGraphShowRightChannel.AutoSize = true;
            this.chkGraphShowRightChannel.Checked = true;
            this.chkGraphShowRightChannel.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkGraphShowRightChannel.Location = new System.Drawing.Point(14, 687);
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
            this.chkGraphShowLeftChannel.Location = new System.Drawing.Point(14, 666);
            this.chkGraphShowLeftChannel.Name = "chkGraphShowLeftChannel";
            this.chkGraphShowLeftChannel.Size = new System.Drawing.Size(85, 17);
            this.chkGraphShowLeftChannel.TabIndex = 48;
            this.chkGraphShowLeftChannel.Text = "Left channel";
            this.chkGraphShowLeftChannel.UseVisualStyleBackColor = true;
            this.chkGraphShowLeftChannel.CheckedChanged += new System.EventHandler(this.chkGraphShowLeftChannel_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label3.Location = new System.Drawing.Point(11, 190);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 36;
            this.label3.Text = "X-axis";
            // 
            // chkShowDataPoints
            // 
            this.chkShowDataPoints.AutoSize = true;
            this.chkShowDataPoints.Location = new System.Drawing.Point(12, 598);
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
            this.chkThickLines.Location = new System.Drawing.Point(12, 577);
            this.chkThickLines.Name = "chkThickLines";
            this.chkThickLines.Size = new System.Drawing.Size(77, 17);
            this.chkThickLines.TabIndex = 44;
            this.chkThickLines.Text = "Thick lines";
            this.chkThickLines.UseVisualStyleBackColor = true;
            this.chkThickLines.CheckedChanged += new System.EventHandler(this.chkThickLines_CheckedChanged);
            // 
            // cmbXAxis
            // 
            this.cmbXAxis.AutoCompleteCustomSource.AddRange(new string[] {
            "Output voltage",
            "Output power",
            "Inpur voltage"});
            this.cmbXAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbXAxis.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbXAxis.FormattingEnabled = true;
            this.cmbXAxis.Items.AddRange(new object[] {
            "Output voltage",
            "Output power",
            "Input voltage"});
            this.cmbXAxis.Location = new System.Drawing.Point(8, 208);
            this.cmbXAxis.Name = "cmbXAxis";
            this.cmbXAxis.Size = new System.Drawing.Size(103, 21);
            this.cmbXAxis.TabIndex = 46;
            this.cmbXAxis.SelectedIndexChanged += new System.EventHandler(this.cmbXAxis_SelectedIndexChanged);
            // 
            // gbdB_Range
            // 
            this.gbdB_Range.Controls.Add(this.ud_dB_Graph_Bottom);
            this.gbdB_Range.Controls.Add(this.ud_dB_Graph_Top);
            this.gbdB_Range.Controls.Add(this.label10);
            this.gbdB_Range.Controls.Add(this.label11);
            this.gbdB_Range.Controls.Add(this.btnDbv_FitGraphY);
            this.gbdB_Range.Location = new System.Drawing.Point(43, 20);
            this.gbdB_Range.Name = "gbdB_Range";
            this.gbdB_Range.Size = new System.Drawing.Size(113, 143);
            this.gbdB_Range.TabIndex = 30;
            this.gbdB_Range.TabStop = false;
            this.gbdB_Range.Text = "dB range";
            // 
            // ud_dB_Graph_Bottom
            // 
            this.ud_dB_Graph_Bottom.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ud_dB_Graph_Bottom.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.ud_dB_Graph_Bottom.Location = new System.Drawing.Point(8, 77);
            this.ud_dB_Graph_Bottom.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.ud_dB_Graph_Bottom.Minimum = new decimal(new int[] {
            200,
            0,
            0,
            -2147483648});
            this.ud_dB_Graph_Bottom.Name = "ud_dB_Graph_Bottom";
            this.ud_dB_Graph_Bottom.Size = new System.Drawing.Size(94, 20);
            this.ud_dB_Graph_Bottom.TabIndex = 31;
            this.ud_dB_Graph_Bottom.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.ud_dB_Graph_Bottom.ValueChanged += new System.EventHandler(this.ud_dB_Graph_Bottom_ValueChanged);
            // 
            // ud_dB_Graph_Top
            // 
            this.ud_dB_Graph_Top.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ud_dB_Graph_Top.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.ud_dB_Graph_Top.Location = new System.Drawing.Point(8, 36);
            this.ud_dB_Graph_Top.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.ud_dB_Graph_Top.Minimum = new decimal(new int[] {
            200,
            0,
            0,
            -2147483648});
            this.ud_dB_Graph_Top.Name = "ud_dB_Graph_Top";
            this.ud_dB_Graph_Top.Size = new System.Drawing.Size(94, 20);
            this.ud_dB_Graph_Top.TabIndex = 30;
            this.ud_dB_Graph_Top.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.ud_dB_Graph_Top.ValueChanged += new System.EventHandler(this.ud_dB_Graph_Top_ValueChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label10.Location = new System.Drawing.Point(8, 58);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(40, 13);
            this.label10.TabIndex = 29;
            this.label10.Text = "Bottom";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label11.Location = new System.Drawing.Point(8, 17);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(26, 13);
            this.label11.TabIndex = 27;
            this.label11.Text = "Top";
            // 
            // btnDbv_FitGraphY
            // 
            this.btnDbv_FitGraphY.Location = new System.Drawing.Point(6, 109);
            this.btnDbv_FitGraphY.Name = "btnDbv_FitGraphY";
            this.btnDbv_FitGraphY.Size = new System.Drawing.Size(94, 23);
            this.btnDbv_FitGraphY.TabIndex = 25;
            this.btnDbv_FitGraphY.Text = "Autofit";
            this.btnDbv_FitGraphY.UseVisualStyleBackColor = true;
            this.btnDbv_FitGraphY.Click += new System.EventHandler(this.btnFitDbGraphY_Click);
            // 
            // gbThdFreq_Harmonics
            // 
            this.gbThdFreq_Harmonics.Controls.Add(this.chkShowMagnitude);
            this.gbThdFreq_Harmonics.Controls.Add(this.chkShowNoiseFloor);
            this.gbThdFreq_Harmonics.Controls.Add(this.chkShowD6);
            this.gbThdFreq_Harmonics.Controls.Add(this.chkShowD5);
            this.gbThdFreq_Harmonics.Controls.Add(this.chkShowD4);
            this.gbThdFreq_Harmonics.Controls.Add(this.chkShowD3);
            this.gbThdFreq_Harmonics.Controls.Add(this.chkShowD2);
            this.gbThdFreq_Harmonics.Controls.Add(this.chkShowThd);
            this.gbThdFreq_Harmonics.Location = new System.Drawing.Point(4, 370);
            this.gbThdFreq_Harmonics.Name = "gbThdFreq_Harmonics";
            this.gbThdFreq_Harmonics.Size = new System.Drawing.Size(111, 203);
            this.gbThdFreq_Harmonics.TabIndex = 29;
            this.gbThdFreq_Harmonics.TabStop = false;
            this.gbThdFreq_Harmonics.Text = "Graph data";
            // 
            // chkShowMagnitude
            // 
            this.chkShowMagnitude.AutoSize = true;
            this.chkShowMagnitude.Checked = true;
            this.chkShowMagnitude.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowMagnitude.Location = new System.Drawing.Point(8, 19);
            this.chkShowMagnitude.Name = "chkShowMagnitude";
            this.chkShowMagnitude.Size = new System.Drawing.Size(76, 17);
            this.chkShowMagnitude.TabIndex = 43;
            this.chkShowMagnitude.Text = "Magnitude";
            this.chkShowMagnitude.UseVisualStyleBackColor = true;
            this.chkShowMagnitude.CheckedChanged += new System.EventHandler(this.chkShowMagnitude_CheckedChanged);
            // 
            // chkShowNoiseFloor
            // 
            this.chkShowNoiseFloor.AutoSize = true;
            this.chkShowNoiseFloor.Checked = true;
            this.chkShowNoiseFloor.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowNoiseFloor.Location = new System.Drawing.Point(8, 180);
            this.chkShowNoiseFloor.Name = "chkShowNoiseFloor";
            this.chkShowNoiseFloor.Size = new System.Drawing.Size(76, 17);
            this.chkShowNoiseFloor.TabIndex = 42;
            this.chkShowNoiseFloor.Text = "Noise floor";
            this.chkShowNoiseFloor.UseVisualStyleBackColor = true;
            this.chkShowNoiseFloor.CheckedChanged += new System.EventHandler(this.chkShowNoiseFloor_CheckedChanged);
            // 
            // chkShowD6
            // 
            this.chkShowD6.AutoSize = true;
            this.chkShowD6.Checked = true;
            this.chkShowD6.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowD6.Location = new System.Drawing.Point(8, 157);
            this.chkShowD6.Name = "chkShowD6";
            this.chkShowD6.Size = new System.Drawing.Size(46, 17);
            this.chkShowD6.TabIndex = 41;
            this.chkShowD6.Text = "D6+";
            this.chkShowD6.UseVisualStyleBackColor = true;
            this.chkShowD6.CheckedChanged += new System.EventHandler(this.chkShowD6_CheckedChanged);
            // 
            // chkShowD5
            // 
            this.chkShowD5.AutoSize = true;
            this.chkShowD5.Checked = true;
            this.chkShowD5.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowD5.Location = new System.Drawing.Point(8, 134);
            this.chkShowD5.Name = "chkShowD5";
            this.chkShowD5.Size = new System.Drawing.Size(40, 17);
            this.chkShowD5.TabIndex = 40;
            this.chkShowD5.Text = "D5";
            this.chkShowD5.UseVisualStyleBackColor = true;
            this.chkShowD5.CheckedChanged += new System.EventHandler(this.chkShowD5_CheckedChanged);
            // 
            // chkShowD4
            // 
            this.chkShowD4.AutoSize = true;
            this.chkShowD4.Checked = true;
            this.chkShowD4.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowD4.Location = new System.Drawing.Point(8, 111);
            this.chkShowD4.Name = "chkShowD4";
            this.chkShowD4.Size = new System.Drawing.Size(40, 17);
            this.chkShowD4.TabIndex = 39;
            this.chkShowD4.Text = "D4";
            this.chkShowD4.UseVisualStyleBackColor = true;
            this.chkShowD4.CheckedChanged += new System.EventHandler(this.chkShowD4_CheckedChanged);
            // 
            // chkShowD3
            // 
            this.chkShowD3.AutoSize = true;
            this.chkShowD3.Checked = true;
            this.chkShowD3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowD3.Location = new System.Drawing.Point(8, 88);
            this.chkShowD3.Name = "chkShowD3";
            this.chkShowD3.Size = new System.Drawing.Size(40, 17);
            this.chkShowD3.TabIndex = 38;
            this.chkShowD3.Text = "D3";
            this.chkShowD3.UseVisualStyleBackColor = true;
            this.chkShowD3.CheckedChanged += new System.EventHandler(this.chkShowD3_CheckedChanged);
            // 
            // chkShowD2
            // 
            this.chkShowD2.AutoSize = true;
            this.chkShowD2.Checked = true;
            this.chkShowD2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowD2.Location = new System.Drawing.Point(8, 65);
            this.chkShowD2.Name = "chkShowD2";
            this.chkShowD2.Size = new System.Drawing.Size(40, 17);
            this.chkShowD2.TabIndex = 37;
            this.chkShowD2.Text = "D2";
            this.chkShowD2.UseVisualStyleBackColor = true;
            this.chkShowD2.CheckedChanged += new System.EventHandler(this.chkShowD2_CheckedChanged);
            // 
            // chkShowThd
            // 
            this.chkShowThd.AutoSize = true;
            this.chkShowThd.Checked = true;
            this.chkShowThd.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkShowThd.Location = new System.Drawing.Point(8, 42);
            this.chkShowThd.Name = "chkShowThd";
            this.chkShowThd.Size = new System.Drawing.Size(49, 17);
            this.chkShowThd.TabIndex = 36;
            this.chkShowThd.Text = "THD";
            this.chkShowThd.UseVisualStyleBackColor = true;
            this.chkShowThd.CheckedChanged += new System.EventHandler(this.chkShowThd_CheckedChanged);
            // 
            // btnGraph_dB
            // 
            this.btnGraph_dB.BackColor = System.Drawing.Color.Cornsilk;
            this.btnGraph_dB.FlatAppearance.BorderColor = System.Drawing.Color.CornflowerBlue;
            this.btnGraph_dB.FlatAppearance.BorderSize = 2;
            this.btnGraph_dB.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnGraph_dB.Location = new System.Drawing.Point(9, 622);
            this.btnGraph_dB.Name = "btnGraph_dB";
            this.btnGraph_dB.Size = new System.Drawing.Size(52, 37);
            this.btnGraph_dB.TabIndex = 28;
            this.btnGraph_dB.Text = "D (dB)";
            this.btnGraph_dB.UseVisualStyleBackColor = false;
            this.btnGraph_dB.Click += new System.EventHandler(this.btnGraph_dB_Click);
            // 
            // btnGraph_D_Percent
            // 
            this.btnGraph_D_Percent.BackColor = System.Drawing.Color.WhiteSmoke;
            this.btnGraph_D_Percent.FlatAppearance.BorderColor = System.Drawing.Color.CornflowerBlue;
            this.btnGraph_D_Percent.FlatAppearance.BorderSize = 2;
            this.btnGraph_D_Percent.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnGraph_D_Percent.Location = new System.Drawing.Point(67, 623);
            this.btnGraph_D_Percent.Name = "btnGraph_D_Percent";
            this.btnGraph_D_Percent.Size = new System.Drawing.Size(49, 37);
            this.btnGraph_D_Percent.TabIndex = 27;
            this.btnGraph_D_Percent.Text = "D (%)";
            this.btnGraph_D_Percent.UseVisualStyleBackColor = false;
            this.btnGraph_D_Percent.Click += new System.EventHandler(this.btnGraph_D_Click);
            // 
            // gbXAxisRange
            // 
            this.gbXAxisRange.Controls.Add(this.btnAutoFitX);
            this.gbXAxisRange.Controls.Add(this.label8);
            this.gbXAxisRange.Controls.Add(this.cmbGraph_VoltageEnd);
            this.gbXAxisRange.Controls.Add(this.label9);
            this.gbXAxisRange.Controls.Add(this.cmbGraph_VoltageStart);
            this.gbXAxisRange.Location = new System.Drawing.Point(3, 235);
            this.gbXAxisRange.Name = "gbXAxisRange";
            this.gbXAxisRange.Size = new System.Drawing.Size(112, 135);
            this.gbXAxisRange.TabIndex = 26;
            this.gbXAxisRange.TabStop = false;
            this.gbXAxisRange.Text = "Voltage range";
            // 
            // btnAutoFitX
            // 
            this.btnAutoFitX.Location = new System.Drawing.Point(6, 106);
            this.btnAutoFitX.Name = "btnAutoFitX";
            this.btnAutoFitX.Size = new System.Drawing.Size(94, 23);
            this.btnAutoFitX.TabIndex = 35;
            this.btnAutoFitX.Text = "Autofit";
            this.btnAutoFitX.UseVisualStyleBackColor = true;
            this.btnAutoFitX.Click += new System.EventHandler(this.btnAutoFitX_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label8.Location = new System.Drawing.Point(8, 61);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(20, 13);
            this.label8.TabIndex = 34;
            this.label8.Text = "To";
            // 
            // cmbGraph_VoltageEnd
            // 
            this.cmbGraph_VoltageEnd.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGraph_VoltageEnd.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbGraph_VoltageEnd.FormattingEnabled = true;
            this.cmbGraph_VoltageEnd.Items.AddRange(new object[] {
            "1",
            "2",
            "5",
            "10",
            "20",
            "50",
            "100",
            "200"});
            this.cmbGraph_VoltageEnd.Location = new System.Drawing.Point(6, 77);
            this.cmbGraph_VoltageEnd.Name = "cmbGraph_VoltageEnd";
            this.cmbGraph_VoltageEnd.Size = new System.Drawing.Size(94, 21);
            this.cmbGraph_VoltageEnd.TabIndex = 33;
            this.cmbGraph_VoltageEnd.SelectedIndexChanged += new System.EventHandler(this.cmbGraph_VoltageEnd_SelectedIndexChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.ForeColor = System.Drawing.SystemColors.ControlText;
            this.label9.Location = new System.Drawing.Point(8, 19);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(30, 13);
            this.label9.TabIndex = 32;
            this.label9.Text = "From";
            // 
            // cmbGraph_VoltageStart
            // 
            this.cmbGraph_VoltageStart.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGraph_VoltageStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbGraph_VoltageStart.FormattingEnabled = true;
            this.cmbGraph_VoltageStart.Items.AddRange(new object[] {
            "0.0001",
            "0.0002",
            "0.0005",
            "0.001",
            "0.002",
            "0.005",
            "0.01",
            "0.02",
            "0.05",
            "0.1",
            "0.2",
            "0.5"});
            this.cmbGraph_VoltageStart.Location = new System.Drawing.Point(6, 35);
            this.cmbGraph_VoltageStart.Name = "cmbGraph_VoltageStart";
            this.cmbGraph_VoltageStart.Size = new System.Drawing.Size(94, 21);
            this.cmbGraph_VoltageStart.TabIndex = 31;
            this.cmbGraph_VoltageStart.SelectedIndexChanged += new System.EventHandler(this.cmbGraph_VoltageStart_SelectedIndexChanged);
            // 
            // gbD_Range
            // 
            this.gbD_Range.Controls.Add(this.label7);
            this.gbD_Range.Controls.Add(this.cmbD_Graph_Bottom);
            this.gbD_Range.Controls.Add(this.label6);
            this.gbD_Range.Controls.Add(this.cmbD_Graph_Top);
            this.gbD_Range.Controls.Add(this.btnD_FitGraphY);
            this.gbD_Range.Location = new System.Drawing.Point(3, 44);
            this.gbD_Range.Name = "gbD_Range";
            this.gbD_Range.Size = new System.Drawing.Size(113, 143);
            this.gbD_Range.TabIndex = 25;
            this.gbD_Range.TabStop = false;
            this.gbD_Range.Text = "D% range";
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
            // cmbD_Graph_Bottom
            // 
            this.cmbD_Graph_Bottom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbD_Graph_Bottom.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbD_Graph_Bottom.FormattingEnabled = true;
            this.cmbD_Graph_Bottom.Items.AddRange(new object[] {
            "0.1",
            "0.01",
            "0.001",
            "0.0001",
            "0.00001",
            "0.000001"});
            this.cmbD_Graph_Bottom.Location = new System.Drawing.Point(6, 74);
            this.cmbD_Graph_Bottom.Name = "cmbD_Graph_Bottom";
            this.cmbD_Graph_Bottom.Size = new System.Drawing.Size(94, 21);
            this.cmbD_Graph_Bottom.TabIndex = 28;
            this.cmbD_Graph_Bottom.SelectedIndexChanged += new System.EventHandler(this.cmbD_Graph_Bottom_SelectedIndexChanged);
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
            // cmbD_Graph_Top
            // 
            this.cmbD_Graph_Top.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbD_Graph_Top.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmbD_Graph_Top.FormattingEnabled = true;
            this.cmbD_Graph_Top.Items.AddRange(new object[] {
            "100",
            "10",
            "1"});
            this.cmbD_Graph_Top.Location = new System.Drawing.Point(6, 33);
            this.cmbD_Graph_Top.Name = "cmbD_Graph_Top";
            this.cmbD_Graph_Top.Size = new System.Drawing.Size(94, 21);
            this.cmbD_Graph_Top.TabIndex = 26;
            this.cmbD_Graph_Top.SelectedIndexChanged += new System.EventHandler(this.cmbD_Graph_Top_SelectedIndexChanged);
            // 
            // btnD_FitGraphY
            // 
            this.btnD_FitGraphY.Location = new System.Drawing.Point(6, 109);
            this.btnD_FitGraphY.Name = "btnD_FitGraphY";
            this.btnD_FitGraphY.Size = new System.Drawing.Size(94, 23);
            this.btnD_FitGraphY.TabIndex = 25;
            this.btnD_FitGraphY.Text = "Autofit";
            this.btnD_FitGraphY.UseVisualStyleBackColor = true;
            this.btnD_FitGraphY.Click += new System.EventHandler(this.btnFitDGraphY_Click);
            // 
            // label17
            // 
            this.label17.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label17.Location = new System.Drawing.Point(760, -1);
            this.label17.MinimumSize = new System.Drawing.Size(50, 13);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(70, 13);
            this.label17.TabIndex = 53;
            this.label17.Text = "Noise floor";
            // 
            // label16
            // 
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(674, -1);
            this.label16.MinimumSize = new System.Drawing.Size(50, 13);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(70, 13);
            this.label16.TabIndex = 52;
            this.label16.Text = "Power";
            // 
            // label15
            // 
            this.label15.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.Location = new System.Drawing.Point(598, -1);
            this.label15.MinimumSize = new System.Drawing.Size(50, 13);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(70, 13);
            this.label15.TabIndex = 51;
            this.label15.Text = "D6+";
            // 
            // label14
            // 
            this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.Location = new System.Drawing.Point(522, -1);
            this.label14.MinimumSize = new System.Drawing.Size(50, 13);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(70, 13);
            this.label14.TabIndex = 50;
            this.label14.Text = "D5";
            // 
            // label13
            // 
            this.label13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.Location = new System.Drawing.Point(446, -1);
            this.label13.MinimumSize = new System.Drawing.Size(50, 13);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(70, 13);
            this.label13.TabIndex = 49;
            this.label13.Text = "D4";
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(370, -1);
            this.label5.MinimumSize = new System.Drawing.Size(50, 13);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(70, 13);
            this.label5.TabIndex = 48;
            this.label5.Text = "D3";
            // 
            // label18
            // 
            this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.Location = new System.Drawing.Point(294, 0);
            this.label18.MinimumSize = new System.Drawing.Size(50, 13);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(70, 13);
            this.label18.TabIndex = 47;
            this.label18.Text = "D2";
            // 
            // lblCursorMagnitude
            // 
            this.lblCursorMagnitude.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursorMagnitude.Location = new System.Drawing.Point(218, -1);
            this.lblCursorMagnitude.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblCursorMagnitude.Name = "lblCursorMagnitude";
            this.lblCursorMagnitude.Size = new System.Drawing.Size(70, 13);
            this.lblCursorMagnitude.TabIndex = 46;
            this.lblCursorMagnitude.Text = "Magnitude";
            // 
            // label19
            // 
            this.label19.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label19.Location = new System.Drawing.Point(152, -1);
            this.label19.MinimumSize = new System.Drawing.Size(50, 13);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(60, 13);
            this.label19.TabIndex = 45;
            this.label19.Text = "THD";
            // 
            // lblCursor_X_Axis
            // 
            this.lblCursor_X_Axis.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_X_Axis.Location = new System.Drawing.Point(80, 0);
            this.lblCursor_X_Axis.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_X_Axis.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblCursor_X_Axis.Name = "lblCursor_X_Axis";
            this.lblCursor_X_Axis.Size = new System.Drawing.Size(56, 17);
            this.lblCursor_X_Axis.TabIndex = 44;
            this.lblCursor_X_Axis.Text = "V(out)";
            // 
            // pnlCursorsRight
            // 
            this.pnlCursorsRight.Controls.Add(this.lblCursor_Vout_R);
            this.pnlCursorsRight.Controls.Add(this.label28);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_Magnitude_R);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_NoiseFloor_R);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_THD_R);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_Power_R);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_D2_R);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_D6_R);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_D3_R);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_D5_R);
            this.pnlCursorsRight.Controls.Add(this.lblCursor_D4_R);
            this.pnlCursorsRight.Location = new System.Drawing.Point(4, 36);
            this.pnlCursorsRight.Name = "pnlCursorsRight";
            this.pnlCursorsRight.Size = new System.Drawing.Size(831, 16);
            this.pnlCursorsRight.TabIndex = 42;
            // 
            // label28
            // 
            this.label28.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label28.Location = new System.Drawing.Point(22, 0);
            this.label28.MinimumSize = new System.Drawing.Size(50, 13);
            this.label28.Name = "label28";
            this.label28.Size = new System.Drawing.Size(53, 13);
            this.label28.TabIndex = 40;
            this.label28.Text = "Right";
            // 
            // lblCursor_Magnitude_R
            // 
            this.lblCursor_Magnitude_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Magnitude_R.Location = new System.Drawing.Point(212, 0);
            this.lblCursor_Magnitude_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Magnitude_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Magnitude_R.Name = "lblCursor_Magnitude_R";
            this.lblCursor_Magnitude_R.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_Magnitude_R.TabIndex = 30;
            this.lblCursor_Magnitude_R.Text = "0.00 dB";
            // 
            // lblCursor_NoiseFloor_R
            // 
            this.lblCursor_NoiseFloor_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_NoiseFloor_R.Location = new System.Drawing.Point(754, 0);
            this.lblCursor_NoiseFloor_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_NoiseFloor_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_NoiseFloor_R.Name = "lblCursor_NoiseFloor_R";
            this.lblCursor_NoiseFloor_R.Size = new System.Drawing.Size(71, 13);
            this.lblCursor_NoiseFloor_R.TabIndex = 38;
            this.lblCursor_NoiseFloor_R.Text = "00.00 dB";
            // 
            // lblCursor_THD_R
            // 
            this.lblCursor_THD_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_THD_R.Location = new System.Drawing.Point(146, 0);
            this.lblCursor_THD_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_THD_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_THD_R.Name = "lblCursor_THD_R";
            this.lblCursor_THD_R.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_THD_R.TabIndex = 31;
            this.lblCursor_THD_R.Text = "0.00 %";
            // 
            // lblCursor_Power_R
            // 
            this.lblCursor_Power_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Power_R.Location = new System.Drawing.Point(668, 0);
            this.lblCursor_Power_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Power_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Power_R.Name = "lblCursor_Power_R";
            this.lblCursor_Power_R.Size = new System.Drawing.Size(82, 13);
            this.lblCursor_Power_R.TabIndex = 37;
            this.lblCursor_Power_R.Text = "0 mW";
            // 
            // lblCursor_D2_R
            // 
            this.lblCursor_D2_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_D2_R.Location = new System.Drawing.Point(288, 0);
            this.lblCursor_D2_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_D2_R.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblCursor_D2_R.Name = "lblCursor_D2_R";
            this.lblCursor_D2_R.Size = new System.Drawing.Size(60, 14);
            this.lblCursor_D2_R.TabIndex = 32;
            this.lblCursor_D2_R.Text = "00.00 %";
            // 
            // lblCursor_D6_R
            // 
            this.lblCursor_D6_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_D6_R.Location = new System.Drawing.Point(592, 0);
            this.lblCursor_D6_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_D6_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_D6_R.Name = "lblCursor_D6_R";
            this.lblCursor_D6_R.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_D6_R.TabIndex = 36;
            this.lblCursor_D6_R.Text = "00.00 %";
            // 
            // lblCursor_D3_R
            // 
            this.lblCursor_D3_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_D3_R.Location = new System.Drawing.Point(364, 0);
            this.lblCursor_D3_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_D3_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_D3_R.Name = "lblCursor_D3_R";
            this.lblCursor_D3_R.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_D3_R.TabIndex = 33;
            this.lblCursor_D3_R.Text = "00.00 %";
            // 
            // lblCursor_D5_R
            // 
            this.lblCursor_D5_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_D5_R.Location = new System.Drawing.Point(516, 0);
            this.lblCursor_D5_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_D5_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_D5_R.Name = "lblCursor_D5_R";
            this.lblCursor_D5_R.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_D5_R.TabIndex = 35;
            this.lblCursor_D5_R.Text = "00.00 %";
            // 
            // lblCursor_D4_R
            // 
            this.lblCursor_D4_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_D4_R.Location = new System.Drawing.Point(440, 0);
            this.lblCursor_D4_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_D4_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_D4_R.Name = "lblCursor_D4_R";
            this.lblCursor_D4_R.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_D4_R.TabIndex = 34;
            this.lblCursor_D4_R.Text = "00.00 %";
            // 
            // pnlCursorsLeft
            // 
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_Vout_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_D5_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_Magnitude_L);
            this.pnlCursorsLeft.Controls.Add(this.label27);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_THD_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_D2_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_D3_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_D4_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_D6_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_Power_L);
            this.pnlCursorsLeft.Controls.Add(this.lblCursor_NoiseFloor_L);
            this.pnlCursorsLeft.Location = new System.Drawing.Point(3, 15);
            this.pnlCursorsLeft.Name = "pnlCursorsLeft";
            this.pnlCursorsLeft.Size = new System.Drawing.Size(830, 20);
            this.pnlCursorsLeft.TabIndex = 43;
            // 
            // lblCursor_D5_L
            // 
            this.lblCursor_D5_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_D5_L.Location = new System.Drawing.Point(518, 2);
            this.lblCursor_D5_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_D5_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_D5_L.Name = "lblCursor_D5_L";
            this.lblCursor_D5_L.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_D5_L.TabIndex = 16;
            this.lblCursor_D5_L.Text = "00.00 %";
            // 
            // lblCursor_Magnitude_L
            // 
            this.lblCursor_Magnitude_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Magnitude_L.Location = new System.Drawing.Point(214, 2);
            this.lblCursor_Magnitude_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Magnitude_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Magnitude_L.Name = "lblCursor_Magnitude_L";
            this.lblCursor_Magnitude_L.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_Magnitude_L.TabIndex = 11;
            this.lblCursor_Magnitude_L.Text = "0.00 dB";
            // 
            // label27
            // 
            this.label27.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label27.Location = new System.Drawing.Point(23, 2);
            this.label27.MinimumSize = new System.Drawing.Size(50, 13);
            this.label27.Name = "label27";
            this.label27.Size = new System.Drawing.Size(53, 13);
            this.label27.TabIndex = 39;
            this.label27.Text = "Left";
            // 
            // lblCursor_THD_L
            // 
            this.lblCursor_THD_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_THD_L.Location = new System.Drawing.Point(148, 2);
            this.lblCursor_THD_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_THD_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_THD_L.Name = "lblCursor_THD_L";
            this.lblCursor_THD_L.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_THD_L.TabIndex = 12;
            this.lblCursor_THD_L.Text = "0.00 %";
            // 
            // lblCursor_D2_L
            // 
            this.lblCursor_D2_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_D2_L.Location = new System.Drawing.Point(290, 2);
            this.lblCursor_D2_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_D2_L.MinimumSize = new System.Drawing.Size(50, 13);
            this.lblCursor_D2_L.Name = "lblCursor_D2_L";
            this.lblCursor_D2_L.Size = new System.Drawing.Size(60, 14);
            this.lblCursor_D2_L.TabIndex = 13;
            this.lblCursor_D2_L.Text = "00.00 %";
            // 
            // lblCursor_D3_L
            // 
            this.lblCursor_D3_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_D3_L.Location = new System.Drawing.Point(366, 2);
            this.lblCursor_D3_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_D3_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_D3_L.Name = "lblCursor_D3_L";
            this.lblCursor_D3_L.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_D3_L.TabIndex = 14;
            this.lblCursor_D3_L.Text = "00.00 %";
            // 
            // lblCursor_D4_L
            // 
            this.lblCursor_D4_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_D4_L.Location = new System.Drawing.Point(442, 2);
            this.lblCursor_D4_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_D4_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_D4_L.Name = "lblCursor_D4_L";
            this.lblCursor_D4_L.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_D4_L.TabIndex = 15;
            this.lblCursor_D4_L.Text = "00.00 %";
            // 
            // lblCursor_D6_L
            // 
            this.lblCursor_D6_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_D6_L.Location = new System.Drawing.Point(594, 2);
            this.lblCursor_D6_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_D6_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_D6_L.Name = "lblCursor_D6_L";
            this.lblCursor_D6_L.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_D6_L.TabIndex = 17;
            this.lblCursor_D6_L.Text = "00.00 %";
            // 
            // lblCursor_Power_L
            // 
            this.lblCursor_Power_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Power_L.Location = new System.Drawing.Point(670, 2);
            this.lblCursor_Power_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Power_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Power_L.Name = "lblCursor_Power_L";
            this.lblCursor_Power_L.Size = new System.Drawing.Size(80, 13);
            this.lblCursor_Power_L.TabIndex = 19;
            this.lblCursor_Power_L.Text = "0 mW";
            // 
            // lblCursor_NoiseFloor_L
            // 
            this.lblCursor_NoiseFloor_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_NoiseFloor_L.Location = new System.Drawing.Point(756, 2);
            this.lblCursor_NoiseFloor_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_NoiseFloor_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_NoiseFloor_L.Name = "lblCursor_NoiseFloor_L";
            this.lblCursor_NoiseFloor_L.Size = new System.Drawing.Size(71, 13);
            this.lblCursor_NoiseFloor_L.TabIndex = 20;
            this.lblCursor_NoiseFloor_L.Text = "00.00 dB";
            // 
            // lblCursor_Vout_L
            // 
            this.lblCursor_Vout_L.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Vout_L.Location = new System.Drawing.Point(80, 2);
            this.lblCursor_Vout_L.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Vout_L.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Vout_L.Name = "lblCursor_Vout_L";
            this.lblCursor_Vout_L.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_Vout_L.TabIndex = 40;
            this.lblCursor_Vout_L.Text = "0.000 V";
            // 
            // lblCursor_Vout_R
            // 
            this.lblCursor_Vout_R.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCursor_Vout_R.Location = new System.Drawing.Point(79, 0);
            this.lblCursor_Vout_R.Margin = new System.Windows.Forms.Padding(0);
            this.lblCursor_Vout_R.MinimumSize = new System.Drawing.Size(60, 13);
            this.lblCursor_Vout_R.Name = "lblCursor_Vout_R";
            this.lblCursor_Vout_R.Size = new System.Drawing.Size(60, 15);
            this.lblCursor_Vout_R.TabIndex = 41;
            this.lblCursor_Vout_R.Text = "0.000 V";
            // 
            // frmThdAmplitude
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1100, 765);
            this.Controls.Add(this.scThdVsFreq);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MinimumSize = new System.Drawing.Size(1100, 765);
            this.Name = "frmThdAmplitude";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "QA40x Audio Analyser - V0.1";
            this.scThdVsFreq.Panel1.ResumeLayout(false);
            this.scThdVsFreq.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scThdVsFreq)).EndInit();
            this.scThdVsFreq.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.udAverages)).EndInit();
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
            this.gbdB_Range.ResumeLayout(false);
            this.gbdB_Range.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ud_dB_Graph_Bottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ud_dB_Graph_Top)).EndInit();
            this.gbThdFreq_Harmonics.ResumeLayout(false);
            this.gbThdFreq_Harmonics.PerformLayout();
            this.gbXAxisRange.ResumeLayout(false);
            this.gbXAxisRange.PerformLayout();
            this.gbD_Range.ResumeLayout(false);
            this.gbD_Range.PerformLayout();
            this.pnlCursorsRight.ResumeLayout(false);
            this.pnlCursorsLeft.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer scThdVsFreq;
        private System.Windows.Forms.Button btnStopThdMeasurement;
        private ScottPlot.WinForms.FormsPlot graphFft;
        private System.Windows.Forms.Button btnStartThdMeasurement;
        private ScottPlot.WinForms.FormsPlot graphTime;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.NumericUpDown udAverages;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtFrequency;
        private System.Windows.Forms.Label lblThdFreq_OutputLoadUnit;
        private System.Windows.Forms.Label lblThdFreq_OutputLoad;
        private System.Windows.Forms.TextBox txtOutputLoad;
        private System.Windows.Forms.ComboBox cmbStartVoltageUnit;
        private System.Windows.Forms.Label lblThdFreq_GenVoltage;
        private System.Windows.Forms.TextBox txtStartVoltage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown udStepsOctave;
        private System.Windows.Forms.Label lblThdFreq_StepsPerOctave;
        private System.Windows.Forms.Label lblFrequency;
        private System.Windows.Forms.SplitContainer scGraphCursors;
        private System.Windows.Forms.SplitContainer scGraphSettings;
        private ScottPlot.WinForms.FormsPlot thdPlot;
        private System.Windows.Forms.CheckBox chkShowDataPoints;
        private System.Windows.Forms.CheckBox chkThickLines;
        private System.Windows.Forms.GroupBox gbdB_Range;
        private System.Windows.Forms.NumericUpDown ud_dB_Graph_Bottom;
        private System.Windows.Forms.NumericUpDown ud_dB_Graph_Top;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button btnDbv_FitGraphY;
        private System.Windows.Forms.GroupBox gbThdFreq_Harmonics;
        private System.Windows.Forms.CheckBox chkShowMagnitude;
        private System.Windows.Forms.CheckBox chkShowNoiseFloor;
        private System.Windows.Forms.CheckBox chkShowD6;
        private System.Windows.Forms.CheckBox chkShowD5;
        private System.Windows.Forms.CheckBox chkShowD4;
        private System.Windows.Forms.CheckBox chkShowD3;
        private System.Windows.Forms.CheckBox chkShowD2;
        private System.Windows.Forms.CheckBox chkShowThd;
        private System.Windows.Forms.Button btnGraph_dB;
        private System.Windows.Forms.Button btnGraph_D_Percent;
        private System.Windows.Forms.GroupBox gbXAxisRange;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cmbGraph_VoltageEnd;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cmbGraph_VoltageStart;
        private System.Windows.Forms.GroupBox gbD_Range;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmbD_Graph_Bottom;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbD_Graph_Top;
        private System.Windows.Forms.Button btnD_FitGraphY;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtEndVoltage;
        private System.Windows.Forms.ComboBox cmbEndVoltageUnit;
        private System.Windows.Forms.Button btnAutoFitX;
        private System.Windows.Forms.ComboBox cmbXAxis;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkGraphShowRightChannel;
        private System.Windows.Forms.CheckBox chkGraphShowLeftChannel;
        private System.Windows.Forms.CheckBox chkEnableRightChannel;
        private System.Windows.Forms.CheckBox chkEnableLeftChannel;
        private System.Windows.Forms.Panel pnlCursorsRight;
        private System.Windows.Forms.Label label28;
        private System.Windows.Forms.Label lblCursor_Magnitude_R;
        private System.Windows.Forms.Label lblCursor_NoiseFloor_R;
        private System.Windows.Forms.Label lblCursor_THD_R;
        private System.Windows.Forms.Label lblCursor_Power_R;
        private System.Windows.Forms.Label lblCursor_D2_R;
        private System.Windows.Forms.Label lblCursor_D6_R;
        private System.Windows.Forms.Label lblCursor_D3_R;
        private System.Windows.Forms.Label lblCursor_D5_R;
        private System.Windows.Forms.Label lblCursor_D4_R;
        private System.Windows.Forms.Panel pnlCursorsLeft;
        private System.Windows.Forms.Label lblCursor_D5_L;
        private System.Windows.Forms.Label lblCursor_Magnitude_L;
        private System.Windows.Forms.Label label27;
        private System.Windows.Forms.Label lblCursor_THD_L;
        private System.Windows.Forms.Label lblCursor_D2_L;
        private System.Windows.Forms.Label lblCursor_D3_L;
        private System.Windows.Forms.Label lblCursor_D4_L;
        private System.Windows.Forms.Label lblCursor_D6_L;
        private System.Windows.Forms.Label lblCursor_Power_L;
        private System.Windows.Forms.Label lblCursor_NoiseFloor_L;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label lblCursorMagnitude;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label lblCursor_X_Axis;
        private System.Windows.Forms.Label lblCursor_Vout_R;
        private System.Windows.Forms.Label lblCursor_Vout_L;
    }
}