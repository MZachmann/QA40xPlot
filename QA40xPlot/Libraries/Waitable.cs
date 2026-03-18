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

		// Awaitable wrapper for AsyncWaitHandle
		public static async Task<bool> WaitHandleAsync(this WaitHandle handle, int timeOut, CancellationToken token = default)
		{
			var rslt = false;
			Debug.Assert(handle != null, "WaitHandle should not be null");

			// Register the WaitHandle with the ThreadPool
			var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			// This callback will be invoked when the handle is signaled
			RegisteredWaitHandle registeredHandle = ThreadPool.RegisterWaitForSingleObject(
				handle,
				(state, timedOut) => DoStateResult(state!, timedOut),
				tcs,
				timeOut,
				executeOnlyOnce: true
			);

			try
			{
				using (token.Register(() => tcs.TrySetCanceled(token)))
				{
					rslt = await tcs.Task.ConfigureAwait(false);
				}
			}
			finally
			{
				// Always unregister to avoid resource leaks
				registeredHandle.Unregister(null);
			}
			return rslt;
		}
	}
}
