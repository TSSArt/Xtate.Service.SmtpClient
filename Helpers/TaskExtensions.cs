using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal static class TaskExtensions
	{
		public static Task WaitAsync(this Task task, CancellationToken token)
		{
			if (task.IsCompleted || !token.CanBeCanceled)
			{
				return task;
			}

			return Task.WhenAny(task, Task.Delay(millisecondsDelay: -1, token));
		}

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

		public static ValueTask WaitAsync(this ValueTask task, CancellationToken token)
		{
			if (task.IsCompleted || !token.CanBeCanceled)
			{
				return task;
			}

			return new ValueTask(Task.WhenAny(task.AsTask(), Task.Delay(millisecondsDelay: -1, token)));
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
