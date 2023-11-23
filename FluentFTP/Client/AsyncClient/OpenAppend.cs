using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Opens the specified file to be appended asynchronously
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>A stream for appending to the file on the server</returns>
		//[Obsolete("OpenAppendAsync() is obsolete, please use UploadFileAsync() with FtpRemoteExists.Resume or FtpRemoteExists.AddToEnd instead", false)]
		public virtual Task<Stream> OpenAppend(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true, CancellationToken token = default(CancellationToken)) {
			return OpenAppendInternal(path, type, checkIfFileExists ? 0 : -1, true, token);
		}

		/// <summary>
		/// Opens the specified file to be appended asynchronously
		/// </summary>
		/// <param name="path">Full or relative path of the file</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="fileLen">
		/// <para>Pass in a file length if known</para>
		/// <br> -1 => File length is irrelevant, do not attempt to determine it</br>
		/// <br> 0  => File length is unknown, try to determine it</br>
		/// <br> >0 => File length is KNOWN. No need to determine it</br>
		/// </param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>A stream for appending to the file on the server</returns>
		//[Obsolete("OpenAppendAsync() is obsolete, please use UploadFileAsync() with FtpRemoteExists.Resume or FtpRemoteExists.AddToEnd instead", false)]
		public virtual Task<Stream> OpenAppend(string path, FtpDataType type, long fileLen, CancellationToken token = default(CancellationToken)) {
			return OpenAppendInternal(path, type, fileLen, true, token);
		}

		/// <summary>
		/// Internal routine
		/// </summary>
		/// <param name="path"></param>
		/// <param name="type"></param>
		/// <param name="fileLen"></param>
		/// <param name="ignoreStaleData">Normally false. Obsolete API uses true</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>A stream for appending the file on the server</returns>
		public virtual async Task<Stream> OpenAppendInternal(string path, FtpDataType type, long fileLen, bool ignoreStaleData, CancellationToken token = default(CancellationToken)) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();
			LastStreamPath = path;

			LogFunction(nameof(OpenAppend), new object[] {path, type, fileLen, ignoreStaleData });

			var client = this;
			FtpDataStream stream = null;
			long length = 0;

			length = fileLen == 0 ? await client.GetFileSize(path, -1, token) : fileLen;

			await client.SetDataTypeAsync(type, token);
			stream = await client.OpenDataStreamAsync("APPE " + path, 0, token);

			if (length > 0 && stream != null) {
				stream.SetLength(length);
				stream.SetPosition(length);
			}

			Status.IgnoreStaleData = ignoreStaleData;

			return stream;
		}

	}
}
