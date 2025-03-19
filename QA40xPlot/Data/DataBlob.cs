using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.Data
{
	public class DataBlob
	{
		public List<double> LeftData = new List<double>();	// in dbv?
		public List<double> PhaseData = new List<double>();	// in degrees?
		public List<double> FreqData = new List<double>();	// Hz

		public DataBlob() { }
	}
}
