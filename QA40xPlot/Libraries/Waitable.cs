using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA40xPlot.Libraries
{
	public static class Waitable
	{
		/// <summary>
		/// part of the AsyncWaitHandle code below.
		/// This is the callback that will be called when the WaitHandle is signaled or times out.
		/// </summary>
		/// <param name="state">the task completion source</param>
		/// <param name="timedOut">bool if we timed out waiting</param>
		private static void DoStateResult(object state, bool timedOut)
		{
			var tcs = (TaskCompletionSource<bool>)state;
			tcs.TrySetResult(!timedOut);
		}

		public static async Task<int> WaitHandleAsync(this AsyncManualResetEvent handle, int timeOut, CancellationToken token = default)
		{
			try
			{
				var dtsk = Task.Delay(timeOut);
				var wtsk = handle.WaitAsync(token);
				var uou = await Task.WhenAny(wtsk, dtsk).ConfigureAwait(false);
				if (uou.IsCanceled)
					return -1;
				return (wtsk == uou) ? 1 : 0;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			return -1; // if we're here then it popped an exception, assume cancellation
		}

		/// <summary>
		/// Asynchronously waits for a WaitHandle to be signaled.
		/// </summary>
		public static ValueTask<bool> WaitHandleAsync(
			this WaitHandle handle,
			int timeout = Timeout.Infinite,
			CancellationToken cancellationToken = default)
		{
			if (handle == null)
				throw new ArgumentNullException(nameof(handle));

			// Fast path: already signaled
			if (handle.WaitOne(0))
				return ValueTask.FromResult(true);

			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			// Register wait with ThreadPool
			var reg = ThreadPool.RegisterWaitForSingleObject(
				handle,
				static (state, timedOut) =>
				{
					var (src, r) = ((TaskCompletionSource<bool>, RegisteredWaitHandle))state!;
					src.TrySetResult(!timedOut);
				},
				(tcs, default(RegisteredWaitHandle)),
				timeout,
				executeOnlyOnce: true
			);

			// Cancellation support
			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(() =>
				{
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			// Ensure unregistration after completion
			return new ValueTask<bool>(tcs.Task.ContinueWith(result =>
			{
				reg.Unregister(null);
				return result.IsCanceled ? false : result.Result;
			}, TaskScheduler.Default));
		}
	}
}
