#region Copyright © 2019-2021 Sergii Artemenko

// This file is part of the Xtate project. <https://xtate.net/>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Xtate.Persistence;

namespace Xtate.Test
{
	public interface IStreamCapture
	{
		void Dispose();

		void FlushAsync();

		void ReadAsync(int requestedLength, int returnedLength);

		void WriteAsync(int requestedLength);

		void SetLength(long length);
	}

	public class ProxyMemoryStream : MemoryStream
	{
		private readonly IStreamCapture _capture;

		public ProxyMemoryStream(IStreamCapture capture) => _capture = capture;

		public ProxyMemoryStream(IStreamCapture capture, ProxyMemoryStream copyFrom) : base(copyFrom.ToArray()) => _capture = capture;

		public int FailWriteCountdown { get; set; }

		protected override void Dispose(bool disposing)
		{
			_capture.Dispose();

			base.Dispose(disposing);
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			_capture.FlushAsync();
			return base.FlushAsync(cancellationToken);
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (count == 0)
			{
				throw new ArgumentException("Zero len");
			}

			var result = await base.ReadAsync(buffer, offset, count, cancellationToken);
			_capture.ReadAsync(count, result);
			return result;
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (count == 0)
			{
				throw new ArgumentException("Zero len");
			}

			if (-- FailWriteCountdown == 0)
			{
				throw new ArgumentException("?");
			}

			_capture.WriteAsync(count);
			return base.WriteAsync(buffer, offset, count, cancellationToken);
		}

		public override void SetLength(long value)
		{
			base.SetLength(value);
			_capture.SetLength(value);
		}
	}

	[TestClass]
	public class StreamStorageTest
	{
		private ProxyMemoryStream    _stream             = default!;
		private Mock<IStreamCapture> _streamCaptureMock  = default!;
		private Mock<IStreamCapture> _streamCaptureMock2 = default!;

		[TestInitialize]
		public void Initialize()
		{
			_streamCaptureMock = new Mock<IStreamCapture>();
			_streamCaptureMock2 = new Mock<IStreamCapture>();
			_stream = new ProxyMemoryStream(_streamCaptureMock.Object);
		}

		[TestMethod]
		public async Task BasicStreamStorageTest()
		{
			{
				await using var streamStorage = await StreamStorage.CreateAsync(_stream);
				await streamStorage.Shrink(default);
				await streamStorage.CheckPoint(level: 0, token: default);
				await streamStorage.CheckPoint(level: 1, token: default);
				await streamStorage.Shrink(default);
			}

			_streamCaptureMock.Verify(l => l.ReadAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
			_streamCaptureMock.Verify(l => l.SetLength(It.IsAny<long>()), Times.Never);
			_streamCaptureMock.Verify(l => l.WriteAsync(It.IsAny<int>()), Times.Never);
			_streamCaptureMock.Verify(l => l.Dispose(), Times.Once);
		}

		[TestMethod]
		public async Task CheckPointZeroLevelTest()
		{
			{
				await using var streamStorage = await StreamStorage.CreateAsync(_stream);
				var bucket = new Bucket(streamStorage);
				bucket.Add(key: "k", value: "v");
				await streamStorage.CheckPoint(level: 0, token: default);

				var bytes = _stream.ToArray();
				Assert.AreEqual(expected: 8, bytes.Length);

				await using var streamStorage2 = await StreamStorage.CreateAsync(new ProxyMemoryStream(_streamCaptureMock2.Object, _stream));
				var bucket2 = new Bucket(streamStorage2);
				Assert.AreEqual(expected: "v", bucket2.TryGet(key: "k", out string? value) ? value : null);
			}

			_streamCaptureMock.Verify(l => l.ReadAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
			_streamCaptureMock.Verify(l => l.SetLength(It.IsAny<long>()), Times.Never);
			_streamCaptureMock.Verify(l => l.WriteAsync(It.IsAny<int>()), Times.Once);
			_streamCaptureMock.Verify(l => l.Dispose(), Times.Once);
		}

		[TestMethod]
		public async Task CheckPointFirstLevelTest()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 1, token: default);

			await using var streamStorage2 = await StreamStorage.CreateAsync(new ProxyMemoryStream(_streamCaptureMock2.Object, _stream));
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v1", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task CheckPointFirstLevelWithRollbackTest()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 1, token: default);

			await using var streamStorage2 = await StreamStorage.CreateWithRollbackAsync(new ProxyMemoryStream(_streamCaptureMock2.Object, _stream), rollbackLevel: 0);
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v0", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task ShrinkTest()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 0, token: default);
			await streamStorage.Shrink(token: default);

			await using var streamStorage2 = await StreamStorage.CreateAsync(new ProxyMemoryStream(_streamCaptureMock2.Object, _stream));
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v1", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task ShrinkFailure1Test()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 0, token: default);

			_stream.FailWriteCountdown = 1;

			try
			{
				await streamStorage.Shrink(token: default);
				Assert.Fail();
			}
			catch (ArgumentException) { }

			var proxyMemoryStream = new ProxyMemoryStream(_streamCaptureMock2.Object, _stream);
			Assert.AreEqual(expected: 18, proxyMemoryStream.Length);
			await using var streamStorage2 = await StreamStorage.CreateAsync(proxyMemoryStream);
			Assert.AreEqual(expected: 18, proxyMemoryStream.Length);
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v1", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task ShrinkFailure2Test()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 0, token: default);

			_stream.FailWriteCountdown = 2;

			try
			{
				await streamStorage.Shrink(token: default);
				Assert.Fail();
			}
			catch (ArgumentException) { }

			var proxyMemoryStream = new ProxyMemoryStream(_streamCaptureMock2.Object, _stream);
			Assert.AreEqual(expected: 28, proxyMemoryStream.Length);
			await using var streamStorage2 = await StreamStorage.CreateAsync(proxyMemoryStream);
			Assert.AreEqual(expected: 18, proxyMemoryStream.Length);
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v1", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task ShrinkFailure3Test()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 0, token: default);

			_stream.FailWriteCountdown = 3;

			try
			{
				await streamStorage.Shrink(token: default);
				Assert.Fail();
			}
			catch (ArgumentException) { }

			var proxyMemoryStream = new ProxyMemoryStream(_streamCaptureMock2.Object, _stream);
			Assert.AreEqual(expected: 28, proxyMemoryStream.Length);
			await using var streamStorage2 = await StreamStorage.CreateAsync(proxyMemoryStream);
			Assert.AreEqual(expected: 9, proxyMemoryStream.Length);
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v1", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task ShrinkFailure4Test()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 0, token: default);

			_stream.FailWriteCountdown = 4;

			try
			{
				await streamStorage.Shrink(token: default);
				Assert.Fail();
			}
			catch (ArgumentException) { }

			var proxyMemoryStream = new ProxyMemoryStream(_streamCaptureMock2.Object, _stream);
			Assert.AreEqual(expected: 28, proxyMemoryStream.Length);
			await using var streamStorage2 = await StreamStorage.CreateAsync(proxyMemoryStream);
			Assert.AreEqual(expected: 9, proxyMemoryStream.Length);
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v1", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task ShrinkFailure5Test()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 0, token: default);

			_stream.FailWriteCountdown = 5;

			await streamStorage.Shrink(token: default);

			var proxyMemoryStream = new ProxyMemoryStream(_streamCaptureMock2.Object, _stream);
			Assert.AreEqual(expected: 9, proxyMemoryStream.Length);
			await using var streamStorage2 = await StreamStorage.CreateAsync(proxyMemoryStream);
			Assert.AreEqual(expected: 9, proxyMemoryStream.Length);
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v1", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task BypassMarkTestTest()
		{
			var s = new string(c: '0', count: 113);

			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", s);
			await streamStorage.CheckPoint(level: 0, token: default);

			_stream.FailWriteCountdown = 3;

			try
			{
				await streamStorage.Shrink(token: default);
				Assert.Fail();
			}
			catch (ArgumentException) { }

			var proxyMemoryStream = new ProxyMemoryStream(_streamCaptureMock2.Object, _stream);
			await using var streamStorage2 = await StreamStorage.CreateAsync(proxyMemoryStream);
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(s, bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task ShrinkWithTranTest()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 1, token: default);
			await streamStorage.Shrink(token: default);

			await using var streamStorage2 = await StreamStorage.CreateAsync(new ProxyMemoryStream(_streamCaptureMock2.Object, _stream));
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v1", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task ShrinkNotNeededTest()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 1, token: default);
			await streamStorage.Shrink(token: default);

			await using var streamStorage2 = await StreamStorage.CreateWithRollbackAsync(new ProxyMemoryStream(_streamCaptureMock2.Object, _stream), rollbackLevel: 0);
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v0", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task ShrinkWithTranWithRollbackTest()
		{
			await using var streamStorage = await StreamStorage.CreateAsync(_stream);
			var bucket = new Bucket(streamStorage);
			bucket.Add(key: "k", value: "v0");
			bucket.Add(key: "k", value: "v2");
			await streamStorage.CheckPoint(level: 0, token: default);
			bucket.Add(key: "k", value: "v1");
			await streamStorage.CheckPoint(level: 1, token: default);
			await streamStorage.Shrink(token: default);

			await using var streamStorage2 = await StreamStorage.CreateWithRollbackAsync(new ProxyMemoryStream(_streamCaptureMock2.Object, _stream), rollbackLevel: 0);
			var bucket2 = new Bucket(streamStorage2);
			Assert.AreEqual(expected: "v2", bucket2.TryGet(key: "k", out string? value) ? value : null);
		}

		[TestMethod]
		public async Task CookiesTest()
		{
			var cookie = new DataModelList
						 {
								 ["domain"] = "some.domain",
								 ["path"] = "/",
								 ["name"] = "ID",
								 ["value"] = "TEST",
								 ["httpOnly"] = true,
								 ["secure"] = true,
								 ["expires"] = new DateTime(year: 2020, month: 1, day: 1)
						 };

			var data = new DataModelList { cookie };

			var memoryStream = new MemoryStream();
			var streamStorage = await StreamStorage.CreateAsync(memoryStream, disposeStream: false);
			var bucket = new Bucket(streamStorage).Nested("cookies");

			var tracker = new DataModelReferenceTracker(bucket.Nested(Key.DataReferences));
			bucket.SetDataModelValue(tracker, data);
			await streamStorage.CheckPoint(level: 0, token: default);

			await streamStorage.Shrink(default);

			await streamStorage.DisposeAsync();

			memoryStream.Position = 0;

			var streamStorage2 = await StreamStorage.CreateAsync(memoryStream, disposeStream: false);
			var bucket2 = new Bucket(streamStorage2).Nested("cookies");
			var tracker2 = new DataModelReferenceTracker(bucket2.Nested(Key.DataReferences));
			var _ = bucket2.GetDataModelValue(tracker2, baseValue: default);
		}
	}
}