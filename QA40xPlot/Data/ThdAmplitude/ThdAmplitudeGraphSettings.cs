namespace QA40xPlot.Data
{
    public class ThdAmplitudeGraphSettings
    {
        public E_ThdAmplitude_GraphType GraphType { get; set; }         
        public bool ShowLeftChannel { get; set; }
        public bool ShowRightChannel { get; set; }
        public bool ShowTHD {  get; set; }
        public bool ShowMagnitude { get; set; }
        public bool ShowD2 { get; set; }
        public bool ShowD3 { get; set; }
        public bool ShowD4 { get; set; }
        public bool ShowD5 { get; set; }
        public bool ShowD6 { get; set; }
        public bool ShowNoiseFloor { get; set; }
        public bool ThickLines { get; set; }
        public bool ShowDataPoints { get; set; }
        public double DbRangeTop { get; set; }
        public double DbRangeBottom { get; set; }
        public double D_PercentTop { get; set; }
        public double D_PercentBottom { get;set; }
        public double VoltageRange_Start { get; set; }
        public double VoltageRange_End { get; set; }

        public E_X_AxisType XAxisType { get; set; }

    }
}           
    