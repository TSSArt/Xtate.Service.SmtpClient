﻿using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public sealed class NamedEventProcessor : IEventProcessor, IDisposable
	{
		private const int         BufferSize         = 4096;
		private const string      PipePrefix         = "#SCXML#_";
		private const PipeOptions DefaultPipeOptions = PipeOptions.WriteThrough | PipeOptions.Asynchronous;

		private static readonly Uri EventProcessorId      = new Uri("http://www.w3.org/TR/scxml/#SCXMLEventProcessor");
		private static readonly Uri EventProcessorAliasId = new Uri(uriString: "scxml", UriKind.Relative);

		private static readonly ConcurrentDictionary<string, IEventConsumer> InProcConsumers = new ConcurrentDictionary<string, IEventConsumer>();

		private readonly Uri            _baseUri;
		private readonly IEventConsumer _eventConsumer;
		private readonly Uri            _loopbackBaseUri;
		private readonly string         _name;
		private readonly string         _pipeName;

		private readonly CancellationTokenSource _stopTokenSource = new CancellationTokenSource();

		public NamedEventProcessor(IEventConsumer eventConsumer, string host, string name)
		{
			if (host == null) throw new ArgumentNullException(nameof(host));

			_eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			_name = name ?? throw new ArgumentNullException(nameof(name));
			_pipeName = PipePrefix + name;
			_baseUri = new Uri("pipe://" + host + "/" + name);
			_loopbackBaseUri = new Uri("pipe:///" + name);

			if (!InProcConsumers.TryAdd(name, eventConsumer))
			{
				throw new StateMachineProcessorException(Res.Format(Resources.Exception_NamedEventProcessor_with_name_already_has_been_registered, name));
			}
		}

	#region Interface IDisposable

		public void Dispose()
		{
			_stopTokenSource.Cancel();

			InProcConsumers.TryRemove(_name, out _);

			_stopTokenSource.Dispose();
		}

	#endregion

	#region Interface IEventProcessor

		Uri IEventProcessor.GetTarget(string sessionId) => GetTarget(sessionId);

		ValueTask IEventProcessor.Dispatch(string sessionId, IOutgoingEvent evt, CancellationToken token) => OutgoingEvent(sessionId, evt, token);

		bool IEventProcessor.CanHandle(Uri? type, Uri? target) =>
				type == null || FullUriComparer.Instance.Equals(type, EventProcessorId) || FullUriComparer.Instance.Equals(type, EventProcessorAliasId);

		Uri IEventProcessor.Id => EventProcessorId;

	#endregion

		private Uri GetTarget(string sessionId, bool isLoopback = false) => new Uri(isLoopback ? _loopbackBaseUri : _baseUri, "#_scxml_" + sessionId);

		private async ValueTask OutgoingEvent(string sessionId, IOutgoingEvent evt, CancellationToken token)
		{
			if (evt.Target == null)
			{
				throw new StateMachineProcessorException(Resources.Exception_Event_Target_did_not_specified);
			}

			var host = evt.Target.Host;
			var isLoopback = evt.Target.IsLoopback || host == _baseUri.Host;
			var name = evt.Target.GetComponents(UriComponents.Path, UriFormat.Unescaped);

			var targetSessionId = ExtractSessionId(evt.Target);
			var eventObject = new EventObject(EventType.External, evt, GetTarget(sessionId, isLoopback), EventProcessorId);

			if (isLoopback && InProcConsumers.TryGetValue(name, out var eventConsumer))
			{
				await eventConsumer.Dispatch(targetSessionId, eventObject, token).ConfigureAwait(false);
			}
			else
			{
				await SendEventToPipe(isLoopback ? "." : host, PipePrefix + name, targetSessionId, eventObject, token).ConfigureAwait(false);
			}
		}

		private static async ValueTask SendEventToPipe(string server, string pipeName, string sessionId, EventObject eventObject, CancellationToken token)
		{
			var pipeStream = new NamedPipeClientStream(server, pipeName, PipeDirection.InOut, DefaultPipeOptions);
			var memoryStream = new MemoryStream();

			await using (pipeStream.ConfigureAwait(false))
			await using (memoryStream.ConfigureAwait(false))
			{
				await pipeStream.ConnectAsync(token).ConfigureAwait(false);
				pipeStream.ReadMode = PipeTransmissionMode.Message;

				var message = new EventMessage(sessionId, eventObject);

				Serialize(message, memoryStream);

				await SendMessage(memoryStream, pipeStream, token).ConfigureAwait(false);

				memoryStream.SetLength(0);

				await ReceiveMessage(pipeStream, memoryStream, token).ConfigureAwait(false);

				memoryStream.Position = 0;
				var responseMessage = Deserialize(memoryStream, b => new ResponseMessage(b));

				if (responseMessage.Exception != null)
				{
					throw new StateMachineProcessorException(Resources.Exception_Error_on_event_consumer_side, responseMessage.Exception);
				}
			}
		}

		public async ValueTask StartListener()
		{
			var pipeStream = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, DefaultPipeOptions);
			var memoryStream = new MemoryStream();
			await using (pipeStream.ConfigureAwait(false))
			await using (memoryStream.ConfigureAwait(false))
			{
				ResponseMessage responseMessage = default;
				try
				{
					await pipeStream.WaitForConnectionAsync(_stopTokenSource.Token).ConfigureAwait(false);

					StartListener().Forget();

					await ReceiveMessage(pipeStream, memoryStream, _stopTokenSource.Token).ConfigureAwait(false);

					memoryStream.Position = 0;
					var message = Deserialize(memoryStream, b => new EventMessage(b));

					if (!string.IsNullOrEmpty(message.SessionId))
					{
						await _eventConsumer.Dispatch(message.SessionId, message.Event, _stopTokenSource.Token).ConfigureAwait(false);
					}
				}
				catch (Exception ex)
				{
					responseMessage = new ResponseMessage(ex);
				}

				memoryStream.SetLength(0);
				Serialize(responseMessage, memoryStream);

				memoryStream.Position = 0;
				await SendMessage(memoryStream, pipeStream, _stopTokenSource.Token).ConfigureAwait(false);
			}
		}

		private static string ExtractSessionId(Uri target)
		{
			var fragment = target.GetComponents(UriComponents.Fragment, UriFormat.Unescaped);

			const string prefix = "_scxml_";

			if (fragment.StartsWith(prefix))
			{
				return fragment.Substring(prefix.Length);
			}

			throw new StateMachineProcessorException(Resources.Exception_Target_wrong_format);
		}

		private static ValueTask SendMessage(MemoryStream memoryStream, PipeStream pipeStream, CancellationToken token)
		{
			memoryStream.TryGetBuffer(out var buffer);

			Infrastructure.Assert(pipeStream.IsConnected);

			return new ValueTask(pipeStream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, token));
		}

		private static async ValueTask ReceiveMessage(PipeStream pipeStream, MemoryStream memoryStream, CancellationToken token)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
			try
			{
				do
				{
					Infrastructure.Assert(pipeStream.IsConnected);

					var count = await pipeStream.ReadAsync(buffer, offset: 0, buffer.Length, token).ConfigureAwait(false);

					// ReSharper disable once MethodHasAsyncOverloadWithCancellation
					memoryStream.Write(buffer, offset: 0, count);
				} while (!pipeStream.IsMessageComplete);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		private static T Deserialize<T>(MemoryStream memoryStream, Func<Bucket, T> creator)
		{
			memoryStream.TryGetBuffer(out var buffer);

			using var inMemoryStorage = new InMemoryStorage(buffer);
			var bucket = new Bucket(inMemoryStorage);

			memoryStream.Seek(offset: 0, SeekOrigin.End);

			return creator(bucket);
		}

		private static void Serialize<T>(T message, MemoryStream memoryStream) where T : IStoreSupport
		{
			using var inMemoryStorage = new InMemoryStorage();
			var bucket = new Bucket(inMemoryStorage);
			message.Store(bucket);

			var size = inMemoryStorage.GetTransactionLogSize();

			if (memoryStream.Length < memoryStream.Position + size)
			{
				memoryStream.SetLength(memoryStream.Position + size);
			}

			memoryStream.TryGetBuffer(out var buffer);
			inMemoryStorage.WriteTransactionLogToSpan(buffer, truncateLog: false);

			memoryStream.Position += size;
		}

		public ValueTask CheckPipeline(CancellationToken token)
		{
			var eventObject = new EventObject(EventType.External, new EventEntity("$"));

			return SendEventToPipe(server: ".", _pipeName, string.Empty, eventObject, token);
		}

		private readonly struct EventMessage : IStoreSupport
		{
			public readonly EventObject Event;
			public readonly string      SessionId;

			public EventMessage(string sessionId, EventObject evt)
			{
				SessionId = sessionId;
				Event = evt;
			}

			public EventMessage(in Bucket bucket)
			{
				if (!bucket.TryGet(Key.TypeInfo, out TypeInfo storedTypeInfo) || storedTypeInfo != TypeInfo.Message)
				{
					throw new ArgumentException(Resources.Exception_Invalid_TypeInfo_value);
				}

				SessionId = bucket.GetString(Key.SessionId) ?? throw new StateMachineProcessorException(Resources.Exception_SessionId_is_empty);
				Event = new EventObject(bucket.Nested(Key.Event));
			}

		#region Interface IStoreSupport

			public void Store(Bucket bucket)
			{
				bucket.Add(Key.TypeInfo, TypeInfo.Message);
				bucket.Add(Key.SessionId, SessionId);
				bucket.AddEntity(Key.Event, Event);
			}

		#endregion
		}

		private readonly struct ResponseMessage : IStoreSupport
		{
			public readonly Exception? Exception;

			public ResponseMessage(Exception exception) => Exception = exception;

			public ResponseMessage(in Bucket bucket)
			{
				if (!bucket.TryGet(Key.TypeInfo, out TypeInfo storedTypeInfo) || storedTypeInfo != TypeInfo.Message)
				{
					throw new ArgumentException(Resources.Exception_Invalid_TypeInfo_value);
				}

				if (bucket.TryGet(Key.Expression, out var mem))
				{
					using var memoryStream = new MemoryStream(mem.ToArray());
					Exception = (Exception) new BinaryFormatter().Deserialize(memoryStream);
				}
				else
				{
					Exception = null;
				}
			}

		#region Interface IStoreSupport

			public void Store(Bucket bucket)
			{
				bucket.Add(Key.TypeInfo, TypeInfo.Message);

				if (Exception != null)
				{
					using var memoryStream = new MemoryStream();
					new BinaryFormatter().Serialize(memoryStream, Exception);
					memoryStream.TryGetBuffer(out var buffer);
					bucket.Add(Key.Exception, buffer);
				}
			}

		#endregion
		}
	}
}