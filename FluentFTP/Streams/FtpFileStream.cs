using System;
using System.IO;
using System.Threading;

#if ASYNC
using System.Threading.Tasks;
#endif

namespace FluentFTP.Streams {
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

#if ASYNC
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
#endif

		/// <summary>
		/// Returns a new stream to upload a file from disk.
		/// If the file fits within the fileSizeLimit, then it is read in a single disk call and stored in memory, and a MemoryStream is returned.
		/// If it is larger than that, then a regular read-only FileStream is returned.
		/// </summary>
		public static Stream GetFileReadStream(string localPath, bool isAsync, long fileSizeLimit, long knownLocalFileSize = 0) {

			// if quick transfer is enabled
			if (fileSizeLimit > 0) {

				// ensure we have the size of the local file
				if (knownLocalFileSize == 0) {
					knownLocalFileSize = GetFileSize(localPath, false);
				}

				// check if quick transfer mode is possible
				if (knownLocalFileSize > 0 && knownLocalFileSize < fileSizeLimit) {

					// read the entire file into memory
					var bytes = File.ReadAllBytes(localPath);

					// create a new memory stream wrapping the bytes and return that
					return new MemoryStream(bytes);
				}

			}

			// normal slow mode, return a FileStream
			return new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, isAsync);
		}

		/// <summary>
		/// Returns a new stream to download a file to disk.
		/// If the file fits within the fileSizeLimit, then a new MemoryStream is returned.
		/// If it is larger than that, then a regular writable FileStream is returned.
		/// </summary>
		public static Stream GetFileWriteStream(string localPath, bool isAsync, long fileSizeLimit, long knownRemoteFileSize = 0, bool isAppend = false, long restartPos = 0) {

			// if quick transfer is enabled
			if (fileSizeLimit > 0) {

				// check if quick transfer mode is possible
				if (!isAppend && restartPos == 0 && knownRemoteFileSize > 0 && knownRemoteFileSize < fileSizeLimit) {

					// create a new memory stream and return that
					return new MemoryStream();
				}
			}

			// normal slow mode, return a FileStream
			if (isAppend) {
				return new FileStream(localPath, FileMode.Append, FileAccess.Write, FileShare.None, 4096, isAsync);
			}
			else {
				return new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, isAsync);
			}
		}

		/// <summary>
		/// If the stream is a MemoryStream, completes the quick download by writing the file to disk.
		/// </summary>
		public static void CompleteQuickFileWrite(Stream fileStream, string localPath) {

			// if quick transfer is enabled
			if (fileStream is MemoryStream) {

				// write the file to disk using a single disk call
				var bytes = ((MemoryStream)fileStream).ToArray();
				File.WriteAllBytes(localPath, bytes);
			}
		}

#if ASYNC
		/// <summary>
		/// If the stream is a MemoryStream, completes the quick download by writing the file to disk.
		/// </summary>
		public static async Task CompleteQuickFileWriteAsync(Stream fileStream, string localPath, CancellationToken token) {

			// if quick transfer is enabled
			if (fileStream is MemoryStream) {

				// write the file to disk using a single disk call
				await Task.Run(() => {
					var bytes = ((MemoryStream)fileStream).ToArray();
					File.WriteAllBytes(localPath, bytes);
				}, token);
			}
			
		}
#endif

	}
}
