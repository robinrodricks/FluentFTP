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

#if ASYNC
		/// <summary>
		/// Sends the NOOP command according to <see cref="NoopInterval"/> (effectively a no-op if 0).
		/// Please call <see cref="GetReply"/> as needed to read the "OK" command sent by the server and prevent stale data on the socket.
		/// Note that response is not guaranteed by all FTP servers when sent during file transfers.
		/// </summary>
		/// <param name="token"></param>
		/// <returns>true if NOOP command was sent</returns>
		protected async Task<bool> NoopAsync(CancellationToken token) {
			if (Config.NoopInterval > 0 && DateTime.UtcNow.Subtract(LastCommandTimestamp).TotalMilliseconds > Config.NoopInterval) {
				Log(FtpTraceLevel.Verbose, "Command:  NOOP");

				await m_stream.WriteLineAsync(m_textEncoding, "NOOP", token);
				LastCommandTimestamp = DateTime.UtcNow;

				return true;
			}

			return false;
		}
#endif

	}
}