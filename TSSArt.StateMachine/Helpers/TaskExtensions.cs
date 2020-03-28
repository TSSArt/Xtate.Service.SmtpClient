using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TSSArt.StateMachine
{
	[PublicAPI]
	internal static class TaskExtensions
	{
		public static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken token)
		{
			if (task.IsCompleted || !token.CanBeCanceled)
			{
				return await task.ConfigureAwait(false);
			}

			await Task.WhenAny(task, Task.Delay(millisecondsDelay: -1, token)).ConfigureAwait(false);

			token.ThrowIfCancellationRequested();

			return await task.ConfigureAwait(false);
		}

		public static async ValueTask<T> WaitAsync<T>(this ValueTask<T> task, CancellationToken token)
		{
			if (task.IsCompleted || !token.CanBeCanceled)
			{
				return await task.ConfigureAwait(false);
			}

			await Task.WhenAny(task.AsTask(), Task.Delay(millisecondsDelay: -1, token)).ConfigureAwait(false);

			token.ThrowIfCancellationRequested();

			return await task.ConfigureAwait(false);
		}
	}
}