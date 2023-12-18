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
	public partial class FtpClient {

		/// <summary>
		/// Opens the specified file for appending. Please call GetReply() after you have successfully transferred the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">The full or relative path to the file to be opened</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="checkIfFileExists">Only set this to false if you are SURE that the file does not exist. If true, it reads the file size and saves it into the stream length.</param>
		/// <returns>A stream for appending to the file on the server</returns>
		//[Obsolete("OpenAppend() is obsolete, please use UploadFile() with FtpRemoteExists.Resume or FtpRemoteExists.AddToEnd instead", false)]
		public virtual Stream OpenAppend(string path, FtpDataType type = FtpDataType.Binary, bool checkIfFileExists = true) {
			return OpenAppendInternal(path, type, checkIfFileExists ? 0 : -1, true);
		}

		/// <summary>
		/// Opens the specified file for appending. Please call GetReply() after you have successfully transferred the file to read the "OK" command sent by the server and prevent stale data on the socket.
		/// </summary>
		/// <param name="path">The full or relative path to the file to be opened</param>
		/// <param name="type">ASCII/Binary</param>
		/// <param name="fileLen">
		/// <para>Pass in a file length if known</para>
		/// <br> -1 => File length is irrelevant, do not attempt to determine it</br>
		/// <br> 0  => File length is unknown, try to determine it</br>
		/// <br> >0 => File length is KNOWN. No need to determine it</br>
		/// </param>
		/// <returns>A stream for appending to the file on the server</returns>
		//[Obsolete("OpenAppend() is obsolete, please use UploadFile() with FtpRemoteExists.Resume or FtpRemoteExists.AddToEnd instead", false)]
		public virtual Stream OpenAppend(string path, FtpDataType type, long fileLen) {
			return OpenAppendInternal(path, type, fileLen, true);
		}

		/// <summary>
		/// Internal routine
		/// </summary>
		/// <param name="path"></param>
		/// <param name="type"></param>
		/// <param name="fileLen"></param>
		/// <param name="ignoreStaleData">Normally false. Obsolete API uses true</param>
		/// <returns>A stream for appending the file on the server</returns>
		public virtual Stream OpenAppendInternal(string path, FtpDataType type, long fileLen, bool ignoreStaleData) {
			// verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(path));
			}

			path = path.GetFtpPath();
			LastStreamPath = path;

			LogFunction(nameof(OpenAppend), new object[] { path, type, fileLen, ignoreStaleData });

			var client = this;
			FtpDataStream stream = null;
			long length = 0;

			length = fileLen == 0 ? client.GetFileSize(path) : fileLen;

			client.SetDataType(type);
			stream = client.OpenDataStream("APPE " + path, 0);

			if (length > 0 && stream != null) {
				stream.SetLength(length);
				stream.SetPosition(length);
			}

			Status.IgnoreStaleData = ignoreStaleData;

			return stream;
		}

	}
}
