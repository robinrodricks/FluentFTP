using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using FluentFTP.Helpers;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Downloads the specified uri and return the raw byte array.
		/// </summary>
		/// <param name="uri">The uri of the item to download</param>
		/// <param name="progress">Provide a callback to track download progress.</param>
		/// <returns>A byte array containing the contents of the downloaded file if successful, otherwise null.</returns>
		public async Task<byte[]> DownloadUriBytes(string uri, IProgress<FtpProgress> progress = null, CancellationToken token = default(CancellationToken)) {
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

			await AutoConnect();

			string remotePath = formalUri.AbsolutePath;

			bool ok;
			var outStream = new MemoryStream();

			ok = await DownloadFileInternalAsync(null, remotePath, outStream, 0, progress, token, new FtpProgress(1, 0), 0, false, 0);

			await Disconnect();

			return ok ? outStream.ToArray() : null;
		}

	}
}
