using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace FluentFTP.Client.BaseClient {

	public partial class BaseFtpClient {

		/// <summary>
		/// Sends the NOOP command according to <see cref="FtpConfig.NoopInterval"/> (effectively a no-op if 0).
		/// Please call <see cref="GetReply"/> as needed to read the "OK" command sent by the server and prevent stale data on the socket.
		/// Note that response is not guaranteed by all FTP servers when sent during file transfers.
		/// <param name="ignoreNoopInterval"/>Send the command regardless of NoopInterval
		/// </summary>
		/// <returns>true if NOOP command was sent</returns>
		bool IInternalFtpClient.NoopInternal(bool ignoreNoopInterval = false) {
			if (ignoreNoopInterval || (Config.NoopInterval > 0 && DateTime.UtcNow.Subtract(LastCommandTimestamp).TotalMilliseconds > Config.NoopInterval)) {

				m_sema.Wait();
				try {
					if (IsConnected) {
						Log(FtpTraceLevel.Verbose, "Command:  NOOP (<-Noop)");

						m_stream.WriteLine(m_textEncoding, "NOOP");
						LastCommandTimestamp = DateTime.UtcNow;

						return true;
					}
				}
				finally {
					m_sema.Release();
				}

			}

			return false;
		}

	}
}