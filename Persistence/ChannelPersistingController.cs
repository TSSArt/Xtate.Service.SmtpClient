using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	internal sealed class ChannelPersistingController<T> : Channel<T>, IDisposable where T : IEntity
	{
		private readonly TaskCompletionSource<int>          _initializedTcs = new TaskCompletionSource<int>();
		private readonly Channel<T>                         _baseChannel;
		private          Bucket                             _bucket;
		private          int                                _headIndex;
		private          int                                _tailIndex;
		private          SemaphoreSlim                      _storageLock;
		private          Func<CancellationToken, ValueTask> _checkPointAction;

		public ChannelPersistingController(Channel<T> baseChannel)
		{
			_baseChannel = baseChannel;

			Reader = new ChannelReader(this);
			Writer = new ChannelWriter(this);
		}

		public void Initialize(Bucket bucket, Func<Bucket, T> creator, SemaphoreSlim storageLock, Func<CancellationToken, ValueTask> checkPointAction)
		{
			if (creator == null) throw new ArgumentNullException(nameof(creator));

			_bucket = bucket;
			_storageLock = storageLock ?? throw new ArgumentNullException(nameof(storageLock));
			_checkPointAction = checkPointAction ?? throw new ArgumentNullException(nameof(checkPointAction));

			bucket.TryGet(Key.Head, out _headIndex);
			bucket.TryGet(Key.Tail, out _tailIndex);

			for (var i = _headIndex; i < _tailIndex; i ++)
			{
				if (!_baseChannel.Writer.TryWrite(creator(bucket.Nested(i))))
				{
					throw new InvalidOperationException("Channel can't consume previously stored object");
				}
			}

			_initializedTcs.TrySetResult(0);
		}

		private Task InitializationComplete(CancellationToken token)
		{
			var initTask = _initializedTcs.Task;

			if (initTask.IsCompleted || !token.CanBeCanceled)
			{
				return initTask;
			}

			return Task.WhenAny(initTask, Task.Delay(millisecondsDelay: -1, token));
		}

		private class ChannelReader : ChannelReader<T>
		{
			private readonly ChannelPersistingController<T> _parent;

			public ChannelReader(ChannelPersistingController<T> parent) => _parent = parent;

			public override bool TryRead(out T item) => throw new NotSupportedException("Use ReadAsync() instead");

			public override ValueTask<bool> WaitToReadAsync(CancellationToken token = default) => throw new NotSupportedException("Use ReadAsync() instead");

			public override async ValueTask<T> ReadAsync(CancellationToken token = default)
			{
				await _parent.InitializationComplete(token).ConfigureAwait(false);

				await _parent._storageLock.WaitAsync(token).ConfigureAwait(false);
				try
				{
					var item = await _parent._baseChannel.Reader.ReadAsync(token);

					if (_parent._tailIndex > _parent._headIndex)
					{
						_parent._bucket.RemoveSubtree(_parent._headIndex ++);
						_parent._bucket.Add(Key.Head, _parent._headIndex);
					}
					else
					{
						_parent._bucket.RemoveSubtree(Bucket.RootKey);
						_parent._headIndex = _parent._tailIndex = 0;
					}

					await _parent._checkPointAction(token).ConfigureAwait(false);

					return item;
				}
				finally
				{
					_parent._storageLock.Release();
				}
			}
		}

		private class ChannelWriter : ChannelWriter<T>
		{
			private readonly ChannelPersistingController<T> _parent;

			public ChannelWriter(ChannelPersistingController<T> parent) => _parent = parent;

			public override bool TryWrite(T item) => throw new NotSupportedException("Use WriteAsync() instead");

			public override ValueTask<bool> WaitToWriteAsync(CancellationToken token = default) => throw new NotSupportedException("Use WriteAsync() instead");

			public override async ValueTask WriteAsync(T item, CancellationToken token = default)
			{
				await _parent.InitializationComplete(token).ConfigureAwait(false);

				await _parent._storageLock.WaitAsync(token).ConfigureAwait(false);
				try
				{
					await _parent._baseChannel.Writer.WriteAsync(item, token);

					var bucket = _parent._bucket.Nested(_parent._tailIndex ++);
					_parent._bucket.Add(Key.Tail, _parent._tailIndex);
					item.As<IStoreSupport>().Store(bucket);

					await _parent._checkPointAction(token).ConfigureAwait(false);
				}
				finally
				{
					_parent._storageLock.Release();
				}
			}
		}

		public void Dispose()
		{
			_initializedTcs.TrySetException(new ObjectDisposedException(nameof(ChannelPersistingController<T>)));
		}

		private enum Key
		{
			Head,
			Tail
		}
	}
}