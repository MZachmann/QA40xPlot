using QA40xPlot.Data;
using System.Drawing;


namespace QA40xPlot.Actions
{
    public partial class frmMain : Form
    {
        public static Panel MeasurementPanel;

        private static ThdFrequencyData ThdFrequencyData = new();

        private static ThdAmplitudeData ThdAmplitudeData = new();

        private static FrequencyResponseData FrequencyResponseChirpData = new();

        private static BodePlotData FrequencyResponseStepsData = new();

        frmThdAmplitude frmThdAmplitude;

        frmThdFrequency frmThdFrequency;

        frmFrequencyResponse frmFrequencyResponseChirp;

        frmBodePlot frmFrequencyResponseSteps;

        public frmMain()
        {
            InitializeComponent();
            
        }

        public void Init()
        {
            MeasurementPanel = splitContainer1.Panel2;
            HideProgressBar();
            ClearMessage();
            ShowThdFrequencyForm();
        }

        private void setButtonHighlight(System.Windows.Forms.Button toHighlight)
        {
			System.Windows.Forms.Button[] buttonArray = [btnMeasurement_BodePlot, btnMeasurement_ThdFreq, btnMeasurement_ThdAmplitude, btnMeasurement_FrequencyResponse];
            foreach (var button in buttonArray)
            {
                if (button == toHighlight)
                {
                    button.Font = new Font(btnMeasurement_ThdFreq.Font, FontStyle.Bold);
					button.BackColor = System.Drawing.Color.Bisque;
				}
                else
                {
                    button.Font = new Font(btnMeasurement_ThdFreq.Font, FontStyle.Regular);
					button.BackColor = System.Drawing.Color.AliceBlue;
				}
			}
        }

		private void btnMeasurement_ThdFreq_Click(object sender, EventArgs e)
        {
            if (MeasurementPanel.Controls.Count > 0)
            {
                Form frm = (Form)MeasurementPanel.Controls[0];
                if (frm != null && frm is frmThdAmplitude)
                {
                    if (((frmThdAmplitude)frm).MeasurementBusy)
                        return;
                }
                if (frm != null && frm is frmFrequencyResponse)
                {
                    if (((frmFrequencyResponse)frm).MeasurementBusy)
                        return;
                }
            }
            ShowThdFrequencyForm();
            setButtonHighlight(btnMeasurement_ThdFreq);
        }

        private void btnMeasurement_ThdAmplitude_Click(object sender, EventArgs e)
        {
            if (MeasurementPanel.Controls.Count > 0)
            {
                Form frm = (Form)MeasurementPanel.Controls[0];
                if (frm != null && frm is frmThdFrequency)
                {
                    if (((frmThdFrequency)frm).MeasurementBusy)
                        return;
                }
                if (frm != null && frm is frmFrequencyResponse)
                {
                    if (((frmFrequencyResponse)frm).MeasurementBusy)
                        return;
                }
            }
            ShowThdAmplitudeForm();
			setButtonHighlight(btnMeasurement_ThdAmplitude);
        }

        private void btnMeasurement_FrequencyResponse_Click(object sender, EventArgs e)
        {
            if (MeasurementPanel.Controls.Count > 0)
            {
                Form frm = (Form)MeasurementPanel.Controls[0];
                if (frm != null && frm is frmThdAmplitude)
                {
                    if (((frmThdAmplitude)frm).MeasurementBusy)
                        return;
                }
                if (frm != null && frm is frmThdFrequency)
                {
                    if (((frmThdFrequency)frm).MeasurementBusy)
                        return;
                }
            }
            ShowFrequencyResponseChirpForm();
			setButtonHighlight(btnMeasurement_FrequencyResponse);
        }

        private void btnMeasurement_BodePlot_Click(object sender, EventArgs e)
        {
            if (MeasurementPanel.Controls.Count > 0)
            {
                Form frm = (Form)MeasurementPanel.Controls[0];
                if (frm != null && frm is frmBodePlot)
                {
                    if (((frmBodePlot)frm).MeasurementBusy)
                        return;
                }
                if (frm != null && frm is frmBodePlot)
                {
                    if (((frmBodePlot)frm).MeasurementBusy)
                        return;
                }
            }
            ShowFrequencyResponseStepsForm();
			setButtonHighlight(btnMeasurement_BodePlot);
        }


        private void ShowThdFrequencyForm()
        {
            if (frmThdFrequency == null)
            {
                frmThdFrequency = new frmThdFrequency(ref ThdFrequencyData);
                frmThdFrequency.Dock = DockStyle.Fill;
                frmThdFrequency.TopLevel = false;
            }
            MeasurementPanel.Controls.Clear();
            MeasurementPanel.Controls.Add(frmThdFrequency);
            frmThdFrequency.Show();
        }

        private void ShowThdAmplitudeForm()
        {
            if (frmThdAmplitude == null)
            {
                frmThdAmplitude = new frmThdAmplitude(ref ThdAmplitudeData);
                frmThdAmplitude.Dock = DockStyle.Fill;
                frmThdAmplitude.TopLevel = false;    
            }
            MeasurementPanel.Controls.Clear();
            MeasurementPanel.Controls.Add(frmThdAmplitude);
            frmThdAmplitude.Show();
        }

        private void ShowFrequencyResponseChirpForm()
        {
            if (frmFrequencyResponseChirp == null)
            {
                frmFrequencyResponseChirp = new frmFrequencyResponse(ref FrequencyResponseChirpData);
                frmFrequencyResponseChirp.Dock = DockStyle.Fill;
                frmFrequencyResponseChirp.TopLevel = false;
            }
            MeasurementPanel.Controls.Clear();
            MeasurementPanel.Controls.Add(frmFrequencyResponseChirp);
            frmFrequencyResponseChirp.Show();
        }

        private void ShowFrequencyResponseStepsForm()
        {
            if (frmFrequencyResponseSteps == null)
            {
                frmFrequencyResponseSteps = new frmBodePlot(ref FrequencyResponseStepsData);
                frmFrequencyResponseSteps.Dock = DockStyle.Fill;
                frmFrequencyResponseSteps.TopLevel = false;
            }
            MeasurementPanel.Controls.Clear();
            MeasurementPanel.Controls.Add(frmFrequencyResponseSteps);
            frmFrequencyResponseSteps.Show();
        }

        public void SetupProgressBar(int min, int max)
        {
            progressBar1.Minimum = min;
            progressBar1.Maximum = max;
            progressBar1.Value = min;
            progressBar1.Visible = false;
        }

        public void UpdateProgressBar(int value)
        {   
            progressBar1.Value = value;
            progressBar1.Visible = true;
        }

        public void HideProgressBar()
        {
            progressBar1.Visible = false;
        }

        public async Task ShowMessage(string message, int delay = 0)
        {
            lbl_Message.Text = message;
            if (delay > 0) 
                await Task.Delay(delay);
        }

        public void ClearMessage()
        {
            lbl_Message.Text = "";
        }

        
    }
}
