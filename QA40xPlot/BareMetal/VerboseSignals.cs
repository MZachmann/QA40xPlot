using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.BareMetal
{
	public class Datashow
	{
		public static List<String> _Trackers { get; } = new() 
			{ "SendPacketQueue", "ReadDataQueue", "EnableSend", "OutDataQueue" };
	}

	public class VerboseQueue<T> : ConcurrentQueue<T>
	{
		private string Name { get; set; }
		public VerboseQueue(string name) { Name = name; }
		public new void Enqueue(T item)
		{
			if (Datashow._Trackers.Contains(Name))
				Console.WriteLine($"{Name}.Enqueue()");
			base.Enqueue(item);
		}
		public new bool TryDequeue([MaybeNullWhen(false)] out T result)
		{
			var did = base.TryDequeue(out result);
			if (did && Datashow._Trackers.Contains(Name))
				Console.WriteLine($"{Name}.TryDequeue()");
			return did;
		}
	}

	public class VerboseReset : AsyncManualResetEvent
	{
		private string Name { get; set; }

		public VerboseReset(string name, bool initialState = false) : base(initialState)
		{
			Name = name;
		}

		public new void Set()
		{
			if (Datashow._Trackers.Contains(Name))
				Console.WriteLine($"{Name}.Set() called");
			base.Set();
		}
		public new void Reset()
		{
			if (Datashow._Trackers.Contains(Name))
				Console.WriteLine($"{Name}.Reset() called");
			base.Reset();
		}
	}

}
