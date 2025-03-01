namespace QA40x_AUDIO_ANALYSER
{
    partial class frmMain
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
            this.scTopBottom = new System.Windows.Forms.SplitContainer();
            this.btnMeasurement_BodePlot = new System.Windows.Forms.Button();
            this.btnMeasurement_FrequencyResponse = new System.Windows.Forms.Button();
            this.btnMeasurement_ThdAmplitude = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lbl_Message = new System.Windows.Forms.Label();
            this.btnMeasurement_ThdFreq = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.scTopBottom)).BeginInit();
            this.scTopBottom.Panel1.SuspendLayout();
            this.scTopBottom.Panel2.SuspendLayout();
            this.scTopBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // scTopBottom
            // 
            this.scTopBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scTopBottom.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.scTopBottom.IsSplitterFixed = true;
            this.scTopBottom.Location = new System.Drawing.Point(0, 0);
            this.scTopBottom.Name = "scTopBottom";
            this.scTopBottom.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scTopBottom.Panel1
            // 
            this.scTopBottom.Panel1.Controls.Add(this.btnMeasurement_BodePlot);
            this.scTopBottom.Panel1.Controls.Add(this.btnMeasurement_FrequencyResponse);
            this.scTopBottom.Panel1.Controls.Add(this.btnMeasurement_ThdAmplitude);
            this.scTopBottom.Panel1.Controls.Add(this.progressBar1);
            this.scTopBottom.Panel1.Controls.Add(this.lbl_Message);
            this.scTopBottom.Panel1.Controls.Add(this.btnMeasurement_ThdFreq);
            // 
            // scTopBottom.Panel2
            // 
            this.scTopBottom.Panel2.Controls.Add(this.splitContainer1);
            this.scTopBottom.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.scTopBottom.Size = new System.Drawing.Size(1304, 861);
            this.scTopBottom.SplitterDistance = 60;
            this.scTopBottom.TabIndex = 1;
            // 
            // btnMeasurement_BodePlot
            // 
            this.btnMeasurement_BodePlot.BackColor = System.Drawing.Color.AliceBlue;
            this.btnMeasurement_BodePlot.FlatAppearance.BorderColor = System.Drawing.Color.Peru;
            this.btnMeasurement_BodePlot.FlatAppearance.BorderSize = 2;
            this.btnMeasurement_BodePlot.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnMeasurement_BodePlot.Location = new System.Drawing.Point(439, 3);
            this.btnMeasurement_BodePlot.Name = "btnMeasurement_BodePlot";
            this.btnMeasurement_BodePlot.Size = new System.Drawing.Size(128, 54);
            this.btnMeasurement_BodePlot.TabIndex = 12;
            this.btnMeasurement_BodePlot.Text = "Bode Plot";
            this.btnMeasurement_BodePlot.UseVisualStyleBackColor = false;
            this.btnMeasurement_BodePlot.Click += new System.EventHandler(this.btnMeasurement_BodePlot_Click);
            // 
            // btnMeasurement_FrequencyResponse
            // 
            this.btnMeasurement_FrequencyResponse.BackColor = System.Drawing.Color.AliceBlue;
            this.btnMeasurement_FrequencyResponse.FlatAppearance.BorderColor = System.Drawing.Color.Peru;
            this.btnMeasurement_FrequencyResponse.FlatAppearance.BorderSize = 2;
            this.btnMeasurement_FrequencyResponse.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnMeasurement_FrequencyResponse.Location = new System.Drawing.Point(305, 3);
            this.btnMeasurement_FrequencyResponse.Name = "btnMeasurement_FrequencyResponse";
            this.btnMeasurement_FrequencyResponse.Size = new System.Drawing.Size(128, 54);
            this.btnMeasurement_FrequencyResponse.TabIndex = 11;
            this.btnMeasurement_FrequencyResponse.Text = "Frequency Response\r\nChirp";
            this.btnMeasurement_FrequencyResponse.UseVisualStyleBackColor = false;
            this.btnMeasurement_FrequencyResponse.Click += new System.EventHandler(this.btnMeasurement_FrequencyResponse_Click);
            // 
            // btnMeasurement_ThdAmplitude
            // 
            this.btnMeasurement_ThdAmplitude.BackColor = System.Drawing.Color.AliceBlue;
            this.btnMeasurement_ThdAmplitude.FlatAppearance.BorderColor = System.Drawing.Color.Peru;
            this.btnMeasurement_ThdAmplitude.FlatAppearance.BorderSize = 2;
            this.btnMeasurement_ThdAmplitude.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnMeasurement_ThdAmplitude.Location = new System.Drawing.Point(171, 3);
            this.btnMeasurement_ThdAmplitude.Name = "btnMeasurement_ThdAmplitude";
            this.btnMeasurement_ThdAmplitude.Size = new System.Drawing.Size(128, 54);
            this.btnMeasurement_ThdAmplitude.TabIndex = 10;
            this.btnMeasurement_ThdAmplitude.Text = "THD vs Amplitude";
            this.btnMeasurement_ThdAmplitude.UseVisualStyleBackColor = false;
            this.btnMeasurement_ThdAmplitude.Click += new System.EventHandler(this.btnMeasurement_ThdAmplitude_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(1180, 41);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(112, 17);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar1.TabIndex = 9;
            this.progressBar1.Value = 20;
            // 
            // lbl_Message
            // 
            this.lbl_Message.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_Message.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_Message.ForeColor = System.Drawing.Color.IndianRed;
            this.lbl_Message.Location = new System.Drawing.Point(687, 19);
            this.lbl_Message.Name = "lbl_Message";
            this.lbl_Message.Size = new System.Drawing.Size(606, 22);
            this.lbl_Message.TabIndex = 8;
            this.lbl_Message.Text = "Message";
            this.lbl_Message.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // btnMeasurement_ThdFreq
            // 
            this.btnMeasurement_ThdFreq.BackColor = System.Drawing.Color.Bisque;
            this.btnMeasurement_ThdFreq.FlatAppearance.BorderColor = System.Drawing.Color.Peru;
            this.btnMeasurement_ThdFreq.FlatAppearance.BorderSize = 2;
            this.btnMeasurement_ThdFreq.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnMeasurement_ThdFreq.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnMeasurement_ThdFreq.Location = new System.Drawing.Point(36, 3);
            this.btnMeasurement_ThdFreq.Name = "btnMeasurement_ThdFreq";
            this.btnMeasurement_ThdFreq.Size = new System.Drawing.Size(129, 54);
            this.btnMeasurement_ThdFreq.TabIndex = 7;
            this.btnMeasurement_ThdFreq.Text = "THD vs Frequency";
            this.btnMeasurement_ThdFreq.UseVisualStyleBackColor = false;
            this.btnMeasurement_ThdFreq.Click += new System.EventHandler(this.btnMeasurement_ThdFreq_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Size = new System.Drawing.Size(1304, 797);
            this.splitContainer1.SplitterDistance = 25;
            this.splitContainer1.TabIndex = 0;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1304, 861);
            this.Controls.Add(this.scTopBottom);
            this.MinimumSize = new System.Drawing.Size(1320, 900);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "QA40x Audio Analyser - V0.6";
            this.scTopBottom.Panel1.ResumeLayout(false);
            this.scTopBottom.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scTopBottom)).EndInit();
            this.scTopBottom.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer scTopBottom;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lbl_Message;
        private System.Windows.Forms.Button btnMeasurement_ThdFreq;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnMeasurement_ThdAmplitude;
        private System.Windows.Forms.Button btnMeasurement_FrequencyResponse;
        private System.Windows.Forms.Button btnMeasurement_BodePlot;
    }
}