using Microsoft.VisualStudio.Threading;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

// this is simply the standard concurrentqueue and asyncresetevent with ability to 
// show debug info when the state changes
// the _Trackers variable is a list of items that get tracked
//
namespace QA40xPlot.BareMetal
{
	public class Datashow
	{
		public static List<String> _Trackers { get; } = new(); //{ "JobQueue" };
	}

	public class VerboseQueue<T> : ConcurrentQueue<T>
	{
		private string Name { get; set; }
		public VerboseQueue(string name) { Name = name; }
		public new void Enqueue(T item)
		{
			if (Datashow._Trackers.Contains(Name))
				UsbSubs.DebugLine($"{Name}.Enqueue()");
			base.Enqueue(item);
		}
		public new bool TryDequeue([MaybeNullWhen(false)] out T result)
		{
			var did = base.TryDequeue(out result);
			if (did && Datashow._Trackers.Contains(Name))
				UsbSubs.DebugLine($"{Name}.TryDequeue()");
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
				UsbSubs.DebugLine($"{Name}.Set() called");
			base.Set();
		}
		public new void Reset()
		{
			if (Datashow._Trackers.Contains(Name))
				UsbSubs.DebugLine($"{Name}.Reset() called");
			base.Reset();
		}
	}

}
