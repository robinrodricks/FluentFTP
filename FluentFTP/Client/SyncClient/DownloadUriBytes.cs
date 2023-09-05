using System;
using System.IO;

using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Downloads the specified uri and return the raw byte array.
		/// </summary>
		/// <param name="outBytes">The variable that will receive the bytes.</param>
		/// <param name="uri">The uri of the item to download</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <returns>Downloaded byte array</returns>
		public bool DownloadUriBytes(out byte[] outBytes, string uri, Action<FtpProgress> progress = null) {
			// verify args
			if (uri.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", nameof(uri));
			}

			LogFunction(nameof(DownloadUriBytes), new object[] { uri });

			// Example:
			// "ftp[s]://username:password@host:port/path"

			var formalUri = new Uri(uri);

			this.Host = formalUri.DnsSafeHost;
			this.Port = formalUri.Port;
			string[] userInfo = formalUri.UserInfo.Split(':');
			this.Credentials.UserName = userInfo[0];
			this.Credentials.Password = userInfo[1];

			AutoConnect();

			string remotePath = formalUri.AbsolutePath;

			outBytes = null;

			bool ok;
			using (var outStream = new MemoryStream()) {
				ok = DownloadFileInternal(null, remotePath, outStream, 0, progress, new FtpProgress(1, 0), 0, false, 0);
				if (ok) {
					outBytes = outStream.ToArray();
				}
			}

			Disconnect();

			return ok; 
		}

	}
}
