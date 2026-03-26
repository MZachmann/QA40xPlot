// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// These warnings come from the Microsoft.VisualStudio.Threading.Analyzers package.
// and they are reasonable but temporarily suppressed

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage",
	"VSTHRD001: Await JoinableTaskFactory.SwitchToMainThreadAsync() to switch to the UI thread", Justification = "Not production code.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage",
	"VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks", Justification = "Not production code.")]
//[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage",
//	"VSTHRD003:Avoid awaiting or returning a task out of context", Justification = "Not production code.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage",
	"VSTHRD103:Result synchronously blocks. Use await instead.", Justification = "Not production code.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage",
	"VSTHRD105:Avoid method overloads that assume TaskScheduler.Current.", Justification = "Not production code.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage",
	"VSTHRD110:Observe the awaitable result of this method call by awaiting", Justification = "Not production code.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Usage",
	"VSTHRD200:Use Async suffix in names of methods", Justification = "Not production code.")]

namespace QA40xPlot.Libraries
{
	internal class GlobalSuppressions
	{
	}
}
