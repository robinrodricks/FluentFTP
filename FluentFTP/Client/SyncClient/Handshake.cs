using FluentFTP.Exceptions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP {
	public partial class FtpClient {

		/// <summary>
		/// Called during Connect(). Typically extended by FTP proxies.
		/// </summary>
		protected virtual void Handshake() {
			FtpReply reply;
			if (!(reply = GetReply()).Success) {
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
