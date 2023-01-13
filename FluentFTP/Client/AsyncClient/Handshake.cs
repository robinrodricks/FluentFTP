using FluentFTP.Exceptions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class AsyncFtpClient {

		/// <summary>
		/// Called during <see cref="Connect(CancellationToken)"/>. Typically extended by FTP proxies.
		/// </summary>
		protected virtual async Task HandshakeAsync(CancellationToken token = default(CancellationToken)) {
			FtpReply reply;
			if (!(reply = await GetReply(token)).Success) {
				if (reply.Code == null) {
					throw new IOException("The connection was terminated before a greeting could be read.");
				}
				else {
					throw new FtpCommandException(reply);
				}
			}

			HandshakeReply = reply;
		}

	}
}
