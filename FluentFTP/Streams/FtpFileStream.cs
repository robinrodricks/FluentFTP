using System;
using System.IO;
using System.Threading;
using FluentFTP.Client.BaseClient;
using System.Threading.Tasks;

namespace FluentFTP.Streams {

	/// <summary>
	/// Stream object for the local files
	/// </summary>
	public static class FtpFileStream {

		/// <summary>
		/// Returns the file size using synchronous file I/O.
		/// </summary>
		public static long GetFileSize(string localPath, bool checkExists) {
			if (checkExists) {
				if (!File.Exists(localPath)) {
					return 0;
				}
			}
			return new FileInfo(localPath).Length;
		}

		/// <summary>
		/// Returns the file size using async file I/O.
		/// </summary>
		public static async Task<long> GetFileSizeAsync(string localPath, bool checkExists, CancellationToken token) {
			if (checkExists) {
				if (!(await Task.Run(() => File.Exists(localPath), token))) {
					return 0;
				}
			}
			return (await Task.Run(() => new FileInfo(localPath), token)).Length;
		}

		/// <summary>
		/// Returns the file size using synchronous file I/O.
		/// </summary>
		public static DateTime GetFileDateModifiedUtc(string localPath) {
			return new FileInfo(localPath).LastWriteTimeUtc;
		}

		/// <summary>
		/// Returns the file size using synchronous file I/O.
		/// </summary>
		public static async Task<DateTime> GetFileDateModifiedUtcAsync(string localPath, CancellationToken token) {
			return (await Task.Run(() => new FileInfo(localPath), token)).LastWriteTimeUtc;
		}

		/// <summary>
		/// Returns a new stream to upload a file from disk.
		/// If the file fits within the fileSizeLimit, then it is read in a single disk call and stored in memory, and a MemoryStream is returned.
		/// If it is larger than that, then a regular read-only FileStream is returned.
		/// </summary>
		public static Stream GetFileReadStream(BaseFtpClient client, string localPath, bool isAsync, long fileSizeLimit, long knownLocalFileSize = 0) {

			// normal slow mode, return a FileStream
			var bufferSize = client != null ? client.Config.LocalFileBufferSize : 4096;
			var shareOption = client != null ? client.Config.LocalFileShareOption : FileShare.Read;
			return new FileStream(localPath, FileMode.Open, FileAccess.Read, shareOption, bufferSize, isAsync);
		}

		/// <summary>
		/// Returns a new stream to download a file to disk.
		/// If the file fits within the fileSizeLimit, then a new MemoryStream is returned.
		/// If it is larger than that, then a regular writable FileStream is returned.
		/// </summary>
		public static Stream GetFileWriteStream(BaseFtpClient client, string localPath, bool isAsync, long fileSizeLimit, long knownRemoteFileSize = 0, bool isAppend = false, long restartPos = 0) {

			// normal slow mode, return a FileStream
			var bufferSize = client != null ? client.Config.LocalFileBufferSize : 4096;
			if (isAppend) {
				return new FileStream(localPath, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize, isAsync);
			}
			else {
				return new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, isAsync);
			}
		}


	}
}