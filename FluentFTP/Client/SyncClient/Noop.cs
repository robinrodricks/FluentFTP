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
		/// Sends the NOOP command according to <see cref="FtpConfig.NoopInterval"/> (effectively a no-op if 0).
		/// Please call <see cref="GetReply"/> as needed to read the "OK" command sent by the server and prevent stale data on the socket.
		/// Note that response is not guaranteed by all FTP servers when sent during file transfers.
		/// <param name="ignoreNoopInterval"/>Send the command regardless of NoopInterval
		/// </summary>
		/// <returns>true if NOOP command was sent</returns>
		public bool Noop(bool ignoreNoopInterval = false) {
			return ((IInternalFtpClient)this).NoopInternal(ignoreNoopInterval);
		}

	}
}