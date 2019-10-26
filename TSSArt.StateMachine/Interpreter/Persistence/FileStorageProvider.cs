using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TSSArt.StateMachine
{
	public class FileStorageProvider : IStorageProvider
	{
		private static readonly char[]   InvalidFileNameChars   = Path.GetInvalidFileNameChars();
		private static readonly string[] InvalidCharReplacement = GetInvalidCharReplacement();

		private readonly string _extension;
		private readonly string _path;

		public FileStorageProvider(string path, string extension = null)
		{
			_path = path ?? throw new ArgumentNullException(nameof(path));
			_extension = extension;
		}

		public async ValueTask<ITransactionalStorage> GetTransactionalStorage(string sessionId, string name, CancellationToken token)
		{
			if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(sessionId));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

			var dir = Path.Combine(_path, Escape(sessionId));
			var path = Path.Combine(dir, Escape(name) + _extension);
			var fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize: 4096, FileOptions.Asynchronous);
			var streamStorage = await StreamStorage.CreateAsync(fileStream, disposeStream: true, token);

			return streamStorage;
		}

		public ValueTask RemoveTransactionalStorage(string sessionId, string name, CancellationToken token)
		{
			if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(sessionId));
			if (string.IsNullOrEmpty(name)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(name));

			var dir = Path.Combine(_path, Escape(sessionId));
			var path = Path.Combine(dir, Escape(name) + _extension);

			try
			{
				File.Delete(path);
			}
			catch
			{
				// ignored
			}

			return default;
		}

		public ValueTask RemoveAllTransactionalStorage(string sessionId, CancellationToken token)
		{
			if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException(message: "Value cannot be null or empty.", nameof(sessionId));

			var path = Path.Combine(_path, Escape(sessionId));

			try
			{
				Directory.Delete(path, recursive: true);
			}
			catch
			{
				// ignored
			}

			return default;
		}

		private static string[] GetInvalidCharReplacement()
		{
			var list = new string[InvalidFileNameChars.Length];

			for (var i = 0; i < list.Length; i ++)
			{
				list[i] = "_x" + ((int) InvalidFileNameChars[i]).ToString("X") + "_";
			}

			return list;
		}

		private static string Escape(string name)
		{
			if (name.IndexOfAny(InvalidFileNameChars) < 0)
			{
				return name;
			}

			var sb = new StringBuilder();
			foreach (var ch in name)
			{
				var index = Array.IndexOf(InvalidFileNameChars, ch);
				if (index >= 0)
				{
					sb.Append(InvalidCharReplacement[index]);
				}
				else
				{
					sb.Append(ch);
				}
			}

			return sb.ToString();
		}
	}
}